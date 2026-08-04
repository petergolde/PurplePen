# Building Purple Pen Installers

Reference notes for building distributable installers of the Avalonia version of
Purple Pen (`AvPurplePen`). Written while building the macOS installer; the
cross-platform sections apply directly to Linux `.deb` / `.rpm` packaging, which
has not been built yet.

Everything marked **verified** was actually run and observed. Everything marked
**unverified** is reasoning that has not been tested — treat it as a starting
point, not as fact.

---

## 1. Project facts

| Thing | Value |
|---|---|
| App project | `src/AvPurplePen/AvPurplePen.csproj` |
| Assembly / executable name | `PurplePen` (`<AssemblyName>`, not `AvPurplePen`) |
| Root namespace | `AvPurplePen` |
| Target framework | `net10.0` — **not** `net10.0-macos`; it's a plain Avalonia desktop app and does not use the .NET macOS workload |
| Version source | `src/PurplePenCore/VersionNumber.cs` → `public const string Current = "4.0.0.110"` |
| Helper executable | `src/PdfConverter/PdfConverter.csproj` — rasterizes PDF map templates |
| Bundled fonts | 19 Roboto TTFs in `fonts/` in the publish output |

Parse the version out of `VersionNumber.cs` rather than hard-coding it:

```bash
sed -n 's/.*Current[[:space:]]*=[[:space:]]*"\([0-9.]*\)".*/\1/p' src/PurplePenCore/VersionNumber.cs
```

It is a four-part version. macOS needs at most three for the user-visible
string; Debian and RPM versions have their own rules (see §6).

---

## 2. The five cross-platform gotchas

These were all found on macOS and then **verified to reproduce identically on a
`linux-x64` publish**. Anyone building a Linux package will hit all five.

### 2.1 A RID-specific publish flattens native libraries; a RID-agnostic one does not

This is the mechanism underlying most of what follows. NuGet packages ship
native assets under `runtimes/<rid>/native/`. What the SDK does with them
depends entirely on whether it knows the target RID at build time:

| | RID-agnostic (no `-r`) | RID-specific (`-r linux-x64`) |
|---|---|---|
| `deps.json` section | `runtimeTargets`, each tagged with its RID | `native` |
| On disk | the whole `runtimes/` tree | the single matching file, at the output **root** |
| Who chooses | the host, at startup, via the RID graph | the SDK, at build time |

**Verified:** it is `-r`, not `--self-contained`, that causes flattening — a
RID-specific *framework-dependent* publish flattens identically.

**Consequence:** in a RID-specific publish, `libSkiaSharp.so`, `libpdfium.so`
etc. live at the top level and the `runtimes/` tree is not consulted at all.

### 2.2 The `runtimes/` tree is ~399 MB of dead weight

`AvPurplePen.csproj` has a `CopyPdfConverterToPublishOutput` target that copies
PdfConverter's entire **build** output into the publish directory. That build is
deliberately RID-agnostic (`RuntimeIdentifier=` is passed blank), so it drags in
`pdfium` and `libSkiaSharp` for *every* platform:

```
win-x86 99M   win-x64 98M   win-arm64 97M
linux-musl-x64/-arm64/-x86 50M   linux-x64/-x86/-arm/-arm64 28M
osx 15M   osx-x64 7.2M   osx-arm64 6.8M
```

**Verified sizes:** macOS publish 533 MB, Linux publish **721 MB**. Excluding
`runtimes/` on macOS gave a 141 MB bundle that runs correctly.

**How to prove it is unused** — the definitive check, works on any platform:

```bash
python3 -c "
import json; d=json.load(open('PurplePen.deps.json'))
print(sum(1 for t in d['targets'].values() for i in t.values() if i.get('runtimeTargets')))
"
```

`runtimeTargets` is the *only* mechanism by which the host probes
`runtimes/<rid>/native/`. Both `PurplePen.deps.json` and (after the fix in §2.3)
`PdfConverter.deps.json` report **0**, so nothing ever looks in that directory.
The tree is orphaned, not merely redundant.

**Caveat that bites:** `libpdfium` has **no** copy at the publish root, on either
platform — it exists *only* inside `runtimes/`. It is only safe to delete
`runtimes/` if you also apply the fix in §2.3, which puts `libpdfium` at the
root. Deleting `runtimes/` *without* that fix silently removes PDF support.

### 2.3 PdfConverter is framework-dependent and cannot start

`CopyPdfConverterToPublishOutput` copies a plain **build** output, so
`PdfConverter.runtimeconfig.json` says `"framework": {...}` while the app's says
`"includedFrameworks"`. The helper's apphost then finds the self-contained app's
own `libhostfxr` sitting next to it, resolves ".NET location" to the *app
directory*, finds no shared framework there, and refuses to start:

```
You must install or update .NET to run this application.
Framework: 'Microsoft.NETCore.App', version '10.0.0'
.NET location: .../publish/
No frameworks were found.
```

**Verified:** this fails even on a machine that *does* have .NET 10 installed —
it never consults the system install. **Verified identical on `linux-x64`.**

**The fix:** republish PdfConverter self-contained *for the target RID* and
overlay it onto the payload. Because it lands in the same directory as the app,
it shares every framework assembly already present:

```bash
dotnet publish src/PdfConverter/PdfConverter.csproj \
    -c Release -f net10.0 -r <RID> --self-contained true -o <tmp>
rsync -a <tmp>/ <payload>/          # NO --delete: this is an overlay
```

This does two things at once: it makes the helper startable, and (per §2.1) it
flattens `libpdfium` to the root, which is what makes deleting `runtimes/` safe.

**Verified cost: ~7 MB**, almost entirely `libpdfium` itself.

**Verified on Linux:** `-r linux-x64 --self-contained` produces an ELF apphost,
`libpdfium.so` at the root, and no `runtimes/` directory.

**Guard you must implement:** the overlay overwrites shared framework
assemblies. If the app and helper ever resolve different
`Microsoft.NETCore.App` patch versions, this silently swaps the runtime out from
under the main app. Compare both `runtimeconfig.json` files and abort on
mismatch. (Both currently report `10.0.8`.)

### 2.4 The PdfConverter republish will poison the *next* build unless redirected

`CopyPdfConverterToPublishOutput` globs
`PdfConverter/bin/$(Configuration)/net10.0/**/*.*` **recursively**. The §2.3 fix
runs `dotnet publish PdfConverter -r <RID>`, which by default writes its build
output to `PdfConverter/bin/Release/net10.0/<RID>/` — *inside* that glob.

So build N creates the directory, and build N+1 sweeps a second copy of the
helper plus a whole self-contained .NET runtime into the app payload. Observed
on macOS: the payload jumped from 362 files / 133 MB to **755 files / 329 MB**,
and it compounds.

Redirect the helper's output out of the source tree:

```bash
dotnet publish ... -p:BaseOutputPath="<your build dir>/pdfconverter-bin/" -o <tmp>
```

**Verified:** with this, two consecutive full builds produce byte-identical
payload counts. Any Linux packaging script doing the same republish needs the
same redirect.

### 2.5 `PdfConverter.exe` is hard-coded on every platform

```csharp
// PurplePenCore/PdfMapFile.cs:176
return Path.Combine(applicationDirectory, "PdfConverter.exe");
```

The helper can never be found off Windows, so PDF map templates silently fail
with "PdfConverter.exe not found." **This is an unfixed bug in the app**, listed
in `doc/devdocs/AvaloniaThoughts.txt` as "Fix running PdfConverter with the .exe
extension. Need to test this on Mac and Linux."

Also in that method: `Assembly.Location` returns an empty string under
single-file publishing. `AppContext.BaseDirectory` is more robust.

Until this is fixed, shipping the helper accomplishes nothing at runtime — but
the packaging work should still include it so it works the moment the lookup is
fixed.

---

## 3. Useful diagnostic techniques

Platform-independent, and they were what actually settled each question:

**Simulate a machine with no .NET installed** (catches framework-dependent
binaries that "work on my machine"):

```bash
env DOTNET_ROOT=/nonexistent PATH=/usr/bin:/bin ./SomeExecutable
```

**Find every native binary in a payload** (for signing, or for `strip`):

```bash
find <payload> -type f -print0 | while IFS= read -r -d '' f; do
    case "$(file -b "$f")" in
        *ELF*executable*)    echo "EXEC: $f" ;;   # Mach-O on macOS
        *ELF*shared\ object*) echo "LIB:  $f" ;;
    esac
done
```

**Check whether a file under `runtimes/` duplicates one at the root:**

```bash
cmp -s runtimes/osx/native/libSkiaSharp.dylib ./libSkiaSharp.dylib && echo identical
```

**Real end-to-end test of the PDF helper** (there is a test PDF in the repo):

```bash
./PdfConverter 150 "src/TestFiles/pdfcourse/All controls.pdf" /tmp/out.png
# expect a 1240x1754 PNG of an orienteering map
```

---

## 4. The macOS installer (built, mostly verified)

Lives in `src/Installer/MacInstaller/` — see its `README.md` for full detail.
`build-mac-app.sh` does: publish → rsync-stage with an exclusion file →
self-contained PdfConverter overlay → `.icns` → assemble `.app` → sign
inside-out → notarize + staple → `.dmg` + `.zip` → notarize + staple the `.dmg`.

Structural decisions worth copying to Linux:

- **An `rsync --exclude-from` staging step** between publish and packaging.
  Being able to tune exactly what ships without touching the build was the
  single most useful part of the design.
- **Skip flags** (`--skip-publish`, `--skip-sign`, `--skip-notarize`) so
  iteration doesn't pay for the slow steps.
- **A config file where every value is env-overridable** (`: "${VAR:=default}"`),
  so CI can override without editing files.

**Verified:** publish, staging, exclusions, bundle assembly, the PdfConverter
overlay, DMG/ZIP creation, and the app launching and running from the built
bundle. **Unverified:** signing and notarization — no Developer ID certificate
is installed on the dev machine yet.

macOS-only concerns that have **no Linux equivalent**: code signing, hardened
runtime entitlements, notarization/stapling, `.icns`, `Info.plist`. Don't waste
time looking for analogues. The nearest Linux counterpart is GPG-signing the
package (§6.4), which is a much weaker and more optional mechanism.

---

## 5. Things that are genuinely macOS-specific but hint at Linux equivalents

| macOS | Linux equivalent |
|---|---|
| `Info.plist` `CFBundleDocumentTypes` for `.ppen` | `shared-mime-info` XML + `MimeType=` in the `.desktop` file |
| `.icns` (all sizes in one file) | separate PNGs in `/usr/share/icons/hicolor/<N>x<N>/apps/` |
| `LSMinimumSystemVersion` | package `Depends:` / `Requires:` on glibc etc. |
| `.app` bundle | `/opt/purplepen` + a launcher symlink |
| Notarization | (none) — GPG repo signing is the closest thing |
| Styled `.dmg` window (background, icon positions, drag arrow) | (none) — a `.deb`/`.rpm` has no presentation layer; the desktop entry and icon are what the user sees |

**Icons are already available at every size Linux needs.**
`src/AvPurplePen/Assets/AppIcon` holds two families — `PurplePen.*` (release)
and `PurplePenBeta.*` (beta) — as PNGs at **16, 24, 32, 48, 64, 96, 128, 256,
512 and 1024**, plus a `.svg` of each. Alpha channels are intact.

That maps directly onto freedesktop hicolor: install
`<family>.<N>x<N>.png` as `/usr/share/icons/hicolor/<N>x<N>/apps/purplepen.png`
for each size, and the `.svg` as
`/usr/share/icons/hicolor/scalable/apps/purplepen.svg`. Every hicolor size is
covered; 1024 has no hicolor slot and is macOS-only.

Whatever packaging script gets written should select the family the same way
the macOS one does (`ICON_FAMILY`, defaulting to `PurplePenBeta`) so beta and
release builds are visually distinguishable.

**Rasterizing the SVG is possible but unnecessary.** `sips` (built into macOS)
reads SVG and genuinely vector-rasterizes at the requested size — **verified**
by rendering a 16px-declared SVG to 1024px and getting a crisp result, against
a deliberately upscaled control that was a blurry mess. It needs `-s format
png` for SVG input; a bare `sips -z` fails. `iconutil` accepts PNG only.
Since pre-rendered PNGs now exist at every needed size, none of this is
required — use the PNGs, which preserve hand-tuning of the small sizes.
On Linux, `rsvg-convert` or `inkscape` would be the equivalent tools if you
ever do need to rasterize.

---

## 6. Linux packaging notes (UNVERIFIED — no Linux machine was available)

The publish itself is verified; everything below it is not.

### 6.1 What was verified

- `dotnet publish AvPurplePen -c Release -f net10.0 -r linux-x64 --self-contained true`
  **succeeds**, producing an ELF `PurplePen` apphost and 16 native `.so` files
  at the output root (`libSkiaSharp.so`, `libHarfBuzzSharp.so`,
  `libSystem.Native.so`, `libclrjit.so`, …).
- Output is **721 MB**, of which 399 MB is the `runtimes/` tree from §2.2.
- `PurplePen.deps.json` has **0** `runtimeTargets` entries, so `runtimes/` is
  orphaned exactly as on macOS.
- `PdfConverter.runtimeconfig.json` in that output is **framework-dependent**,
  i.e. the §2.3 bug is present.
- The §2.3 fix works: `-r linux-x64 --self-contained` gives an ELF apphost and a
  flattened `libpdfium.so`.

Nothing has ever been *run* on Linux. Assume the app has never been launched
there and budget time for first-run problems.

### 6.2 RIDs

`linux-x64`, `linux-arm64`, and `linux-musl-x64` for Alpine. glibc and musl are
not interchangeable — a musl target needs its own build and its own package.

### 6.3 Runtime dependencies — determine these on the target, do not guess

Run `ldd` on the apphost and on every `.so` at the publish root, on the oldest
distro you intend to support, and derive `Depends:` / `Requires:` from that.

Likely needed, but **each must be confirmed**:

- **ICU** (`libicu`) — no `InvariantGlobalization` property is set anywhere in
  the repo, so .NET will expect ICU at runtime. Self-contained publishing does
  **not** bundle it. Either add a dependency, or set
  `InvariantGlobalization=true` (which would break culture-aware behaviour —
  Purple Pen is heavily localized, so this is probably not acceptable), or
  bundle `Microsoft.ICU.ICU4C.Runtime`.
- **OpenSSL** for `libSystem.Security.Cryptography.Native.OpenSsl.so`
- **fontconfig / freetype** for Skia text rendering
- **X11 / libICE / libSM** for Avalonia's X11 backend
- **zlib**

Also decide about Wayland vs X11 and whether to depend on `xdg-utils` —
`doc/devdocs/AvaloniaThoughts.txt` notes that printing on Linux is expected to
work by generating a PDF and launching a viewer, and suggests looking at
`xdg-desktop-portal`.

### 6.4 Packaging mechanics

- `dotnet-packaging` (the old `dotnet deb` / `dotnet rpm` tooling) is
  effectively unmaintained. Prefer driving `dpkg-deb` / `rpmbuild` directly, or
  use `fpm` to generate both from one staged tree — which fits the staging-
  directory design in §4 well.
- Install to `/opt/purplepen`, symlink the launcher into `/usr/bin`.
- **Preserve the executable bit** on `PurplePen` and `PdfConverter`. `rsync -a`
  and `cp -a` preserve it; some archive round-trips do not.
- Ship a `.desktop` file in `/usr/share/applications` with `Categories=`,
  `Icon=purplepen`, and `MimeType=` once a `.ppen` MIME type is registered.
- Debian versions don't accept a bare four-part version cleanly in all
  contexts; map `4.0.0.110` to something like `4.0.0.110-1`. RPM wants separate
  `Version:` and `Release:` fields.
- Package signing: `dpkg-sig` / `debsign` for `.deb`, `rpm --addsign` for
  `.rpm`, both GPG-based. Unlike notarization this is optional and mainly
  matters if you publish an apt/yum repository.

### 6.5 Fonts

19 Roboto TTFs ship in `fonts/`. `AvaloniaThoughts.txt` notes Arial and Times
New Roman substitutes may be needed on Linux ("Use CrossCore fonts for these"),
and that fonts are believed to be fine on macOS but not on Linux. Expect font
fallback work; it is listed there as low priority because course files usually
name fonts that are present.

---

## 7. Open items across all platforms

1. **`PdfConverter.exe` lookup** (§2.5) — blocks PDF templates on macOS *and*
   Linux. Fix in `PurplePenCore/PdfMapFile.cs`.
2. **The `CopyPdfConverterToPublishOutput` target** in `AvPurplePen.csproj` is
   the root cause of §2.2, §2.3 and §2.4. Making it publish RID-specific on
   non-Windows platforms would fix all three at source and remove the need for
   the overlay in each installer. It was left alone because it also affects the
   Windows build.
3. **Notarization is still unexercised on macOS.** Code signing now works and is
   verified (full Developer ID chain, secure timestamp, `--verify --deep
   --strict` passes, `spctl` reports the expected `Unnotarized Developer ID`).
   Submitting to Apple has not yet been run. Linux has no equivalent step.

Resolved since first writing: the app icon, which was 64×64 only — see §5.
