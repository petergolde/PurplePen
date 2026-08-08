# Building Purple Pen Installers

Reference notes for building distributable installers of the Avalonia version of
Purple Pen (`AvPurplePen`). Written while building the macOS installer, then
extended when the Linux `.deb` / `.rpm` packaging was built on top of it.

Everything marked **verified** was actually run and observed. Everything marked
**unverified** is reasoning that has not been tested — treat it as a starting
point, not as fact.

Both installers now exist and have their own detailed documentation:

| Platform | Directory | Status |
|---|---|---|
| macOS | `src/Installer/MacInstaller/` | Builds a signed `.dmg` + `.zip`. Notarization unexercised. |
| Linux | `src/Installer/LinuxInstaller/` | Builds `.deb` + `.rpm` + AppImage. All three installed/run on Ubuntu 22.04. |

This file is the shared background; the per-platform READMEs are the operating
instructions.

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

### 2.5 `PdfConverter.exe` was hard-coded on every platform — now fixed

This used to read `Path.Combine(applicationDirectory, "PdfConverter.exe")`, so
the helper could never be found off Windows and PDF map templates silently
failed. Fixed in commit `1caa2efa`:

```csharp
// PurplePenCore/PdfMapFile.cs:175
string executableFileName = OperatingSystem.IsWindows() ? "PdfConverter.exe" : "PdfConverter";
```

**Verified end to end on Linux**: with the packaging described in §6, running
the installed helper against the test PDF in the repo produces the expected
1240×1754 PNG, and it does so with `DOTNET_ROOT=/nonexistent`, which proves it
is genuinely self-contained rather than quietly using a system .NET.

One caveat remains in that method: it locates itself through
`Assembly.Location`, which returns an empty string under **single-file**
publishing. Neither installer publishes single-file, so this is latent rather
than live. `AppContext.BaseDirectory` is the robust replacement if that ever
changes.

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

## 6. Linux packaging (BUILT — see `src/Installer/LinuxInstaller/README.md`)

`build-linux-packages.sh` produces a `.deb`, an `.rpm` and an AppImage from one
staged tree. This section is the background; that README is the operating
manual.

### 6.1 What was verified

Built in WSL2 (Ubuntu 22.04, .NET SDK 10.0.105) and installed there:

- The publish produces an ELF `PurplePen` apphost and the native `.so` files at
  the output root, and `runtimes/` is orphaned exactly as on macOS.
- **The application launches and runs.** Under WSLg it starts and stays up;
  with `DISPLAY` and `WAYLAND_DISPLAY` unset it exits 1 immediately, which is
  what makes the first result mean something.
- **PDF map templates work** — see §2.5.
- Passing a `.ppen` path as `argv[1]` opens it (`CommandLineOptions.cs`), so
  the file association is real rather than decorative. `gio info` on a sample
  file reports `application/x-purplepen` after install.
- Hard dependencies resolve on Ubuntu 22.04 with nothing else to install.
- The `.deb` and the `.rpm` contain an identical set of paths; the only
  difference is the 32 shared parent directories the RPM correctly does not
  claim.

- **The AppImage runs, including on a host with no ICU at all** — see §6.7.

Sizes, after the exclusions in §6.6: payload **132 MB**, `.deb` 43 MB,
`.rpm` 39 MB, AppImage 63 MB (the extra ~20 MB is bundled ICU).

**Still unverified:** `linux-arm64` and the other RIDs (nothing has been run on
them), and any distribution other than Ubuntu — in particular the RPM
dependency names, which target Fedora/RHEL and were never resolved against a
real dnf.

### 6.2 RIDs

`linux-x64`, `linux-arm64`, and `linux-musl-x64` for Alpine. glibc and musl are
not interchangeable — a musl target needs its own build and its own package.

### 6.3 Runtime dependencies

The lists now in `LinuxInstaller/config.sh` were derived from `ldd` over the
payload plus the two libraries .NET opens with `dlopen`. Rerun the survey with
`./build-linux-packages.sh --show-deps`.

The trap here is that **`ldd` cannot see the two dependencies most likely to
break a machine**: .NET loads ICU (globalization) and OpenSSL (TLS) lazily, so
neither appears in any `ldd` output and both have to be added by hand. Neither
is bundled by a self-contained publish. `InvariantGlobalization=true` would
remove the ICU requirement but break culture-aware behaviour, which is not
acceptable in an application this heavily localized.

Debian's ICU and OpenSSL packages carry the soname in the package name, so it
differs on every distribution release and the dependency has to be spelled as
an alternation (`libicu76 | libicu74 | …`). RPM's `libicu` and `openssl-libs`
are stable by comparison. **Verified on Ubuntu 22.04:** the alternation
resolves against `libicu70` / `libssl3` with nothing left to install.

`xdg-utils` is a Recommends, not a Depends: printing works by generating a PDF
and handing it to the system viewer, so it matters on a desktop but should not
block installation elsewhere.

### 6.4 Packaging mechanics

Settled choices, all now implemented:

- **`dpkg-deb` and `rpmbuild` driven directly**, from one staged tree.
  `dotnet-packaging` is unmaintained; `fpm` needs a Ruby toolchain to save very
  little. `rpmbuild` runs perfectly well on Debian/Ubuntu (`apt install rpm`),
  so no Fedora machine is needed.
- Install to `/opt/purplepen`, with `/usr/bin/purplepen` a **symlink**. A
  wrapper script is unnecessary: the apphost resolves `/proc/self/exe`, which
  follows symlinks, so the application directory comes out right.
- **Set the executable bit deliberately, do not preserve it.** `rsync -a`
  preserving the source mode is not enough when the source is a Windows drive
  under WSL, where every file reads as `0777` and packaging it as-is ships
  world-writable binaries. Deriving the mode from each file's ELF magic makes
  the output identical wherever it was staged.
- **Fold the prerelease stage into the version.** `4.0.0.210` shipped verbatim
  sorts *after* the eventual stable `4.0.0` in both dpkg and rpm, so beta users
  would never be offered the release. Both systems sort `~` before everything,
  so `4.0.0.210` becomes `4.0.0~beta1`. Check with
  `dpkg --compare-versions '4.0.0~beta1' lt '4.0.0'`.
- **Turn off rpm's automatic dependency generation** (`AutoReqProv: no`).
  Left on, it scans the ~90 bundled `.so` files and advertises them in
  `Provides`, so the package can be pulled in to satisfy an unrelated
  dependency on `libSkiaSharp.so`.
- Package signing (`debsign`, `rpm --addsign`) is **not** implemented. Both are
  GPG-based and mainly matter when publishing an apt/yum repository; there is
  no Linux equivalent of notarization.

### 6.5 Fonts

19 Roboto TTFs ship in `fonts/`. `AvaloniaThoughts.txt` notes Arial and Times
New Roman substitutes may be needed on Linux ("Use CrossCore fonts for these"),
and that fonts are believed to be fine on macOS but not on Linux. Expect font
fallback work; it is listed there as low priority because course files usually
name fonts that are present.

### 6.6 Two ways a Windows checkout corrupts a Linux build

Both were hit in WSL, both fail confusingly, and both are handled by the script.

**Another platform's runtime gets packaged.** §2.4 describes
`CopyPdfConverterToPublishOutput`'s recursive glob poisoning the *next* build.
It also works in the other direction: a checkout that has been published for
Windows leaves `PdfConverter/bin/Release/net10.0/win-x64/`, and the glob sweeps
that entire self-contained Windows .NET runtime — **178 MB** — into the Linux
publish, where nothing can ever load it. It is invisible in testing, because
nothing loads it and so nothing breaks; the only symptom is an inexplicably
large package. The script excludes any top-level directory whose name is a RID
and fails if one survives.

**`obj/` is shared across operating systems.** `AvPurplePen.csproj` builds
PdfConverter through an MSBuild task with `Targets="Build"`, which performs no
restore, so it uses whatever `obj/project.assets.json` is present. If Visual
Studio wrote it, the Linux publish dies well into the build with

```
error MSB4018: Unable to find fallback package folder
'C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages'
```

The fix is an explicit `dotnet restore` of both projects before publishing.

### 6.7 The AppImage, and the one dependency that must be bundled

A `.deb` or `.rpm` declares dependencies and lets the package manager satisfy
them. **An AppImage has no dependency resolution at all**, so the bundling
question has to be answered rather than deferred — and the answer is mostly
"bundle nothing".

The AppImage project's
[excludelist](https://github.com/AppImage/pkg2appimage/blob/master/excludelist)
names the libraries that must come from the host because they are tied to its
kernel, display server or font configuration. It covers nearly everything
Purple Pen touches: glibc, libstdc++, libgcc\_s, libX11, libICE, libSM,
fontconfig, freetype, expat, uuid, zlib. Bundling them causes the failures the
list exists to prevent — fontconfig is specifically documented as making
applications hang at startup.

**ICU is the exception.** It is not on the excludelist, and it is the only
dependency whose absence is *fatal rather than degrading*: .NET aborts with
"Couldn't find a valid ICU package installed on the system", and
`InvariantGlobalization` is not acceptable in an application this localized.
`AppRun` prepends a bundled copy to `LD_LIBRARY_PATH`.

**Verified, not assumed.** With the host's ICU masked inside a private mount
namespace, running the payload directly aborts with that error; running the
same host through `AppRun` starts normally. Necessary and sufficient. Cost is
~33 MB uncompressed, ~20 MB in the finished image.

OpenSSL is deliberately not bundled: its absence only breaks the update check,
while a bundled crypto library never gets security updates.

**The host floor does not depend on the build machine.** Purple Pen compiles no
native code — every `.so` comes prebuilt from Microsoft or SkiaSharp — so the
requirement is fixed by those binaries at **glibc 2.27** and **GLIBCXX_3.4.22**
(Ubuntu 18.04 / Debian 10 / RHEL 8 era). The standard AppImage practice of
building inside an ancient distro buys nothing here. Re-measure after a .NET
major upgrade:

```bash
objdump -T payload/*.so | grep -o 'GLIBC_[0-9.]*' | sort -uV | tail -1
```

Two operational notes. appimagetool is pinned by version *and* SHA-256, because
a tag can be moved and a release asset replaced — the hash is the only real
pin. And appimagetool downloads its type-2 runtime on every run, so the
AppImage build needs network access even when the tool itself is cached;
`APPIMAGE_RUNTIME_FILE` makes it offline-capable.

---

## 7. Open items across all platforms

1. **The `CopyPdfConverterToPublishOutput` target** in `AvPurplePen.csproj` is
   the root cause of §2.2, §2.3, §2.4 and both halves of §6.6 — five separate
   workarounds across two installers. Making it publish RID-specific, and
   globbing a single output directory rather than recursing, would fix all of
   them at source and remove the overlay from both installers. It has been left
   alone because it also affects the Windows build. **This is the highest-value
   cleanup available in the packaging work.**
2. **Notarization is still unexercised on macOS.** Code signing now works and is
   verified (full Developer ID chain, secure timestamp, `--verify --deep
   --strict` passes, `spctl` reports the expected `Unnotarized Developer ID`).
   Submitting to Apple has not yet been run. Linux has no equivalent step.
3. **The Linux packages have only been tested on Ubuntu 22.04.** The RPM
   dependency names target Fedora/RHEL and have never been resolved by a real
   dnf; openSUSE and Mageia name several of them differently. Only `linux-x64`
   has been built. The AppImage's claim to run down to glibc 2.27 is derived
   from symbol versions, not from having run it on such a host.
4. **`Assembly.Location` in `PdfMapFile.FindPdfConverterExe`** returns an empty
   string under single-file publishing. Latent today — no installer publishes
   single-file — but `AppContext.BaseDirectory` is the robust form.
5. **AppStream metadata is AppImage-only.** `usr/share/metainfo` is assembled
   for the AppDir but not added to the `.deb`/`.rpm`, where it would also let
   software centres describe the application. Small, self-contained addition.

Resolved since first writing:

- The app icon, which was 64×64 only — see §5.
- The `PdfConverter.exe` lookup — see §2.5, fixed in `1caa2efa` and now
  verified working on Linux.
