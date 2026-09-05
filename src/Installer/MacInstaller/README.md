# Purple Pen macOS Installer

Builds a signed and notarized macOS distribution of Purple Pen — a `.dmg` for
normal installation and a `.zip` for automated deployment.

```bash
./build-mac-app.sh
```

Output lands in `output/`:

```
output/PurplePen-4.0.0-beta1-osx-arm64.dmg
output/PurplePen-4.0.0-beta1-osx-arm64.zip
```

The name carries the release stage taken from the fourth component of the
version in `PurplePenCore/VersionNumber.cs` — `-beta1` here, `-rc2` for a
release candidate, nothing at all for a stable release. Without it a beta and
the eventual release of the same version would share a file name, and would
collide in the publishing tree.

To build and publish in one step, see [Publishing](#publishing) below.

## Files

| File | Purpose |
|---|---|
| `build-mac-app.sh` | The build script. Run this. |
| `publish-mac-app.sh` | Builds, then files the result into the download tree and records it in the update manifest. |
| `config.sh` | Settings — bundle id, versions, signing identity, notarization profile, publishing tree. Every value can be overridden by an environment variable of the same name. |
| `publish-exclude.txt` | rsync exclusion list controlling exactly which published files go into the app bundle. Currently empty; customize as you experiment. |
| `Info.plist.template` | Bundle metadata, with `@PLACEHOLDER@` tokens filled in by the script. |
| `PurplePen.entitlements` | Hardened Runtime entitlements required to run .NET under notarization. |

`build/` (staging area, assembled `.app`) and `output/` are generated and
git-ignored. `output/build-info.sh` is written by a successful build to say what
it produced — version, file names, whether it was signed and notarized — and is
what `publish-mac-app.sh` reads.

## One-time setup

### 1. Install a Developer ID certificate

Notarized distribution outside the Mac App Store requires a **Developer ID
Application** certificate, which is separate from the "Apple Development"
certificate Xcode creates for you.

1. Go to [Certificates, Identifiers & Profiles](https://developer.apple.com/account/resources/certificates).
2. Create a certificate of type **Developer ID Application**.
3. Download it and double-click to install into your login keychain.

Verify:

```bash
security find-identity -v -p codesigning
```

You should see a line containing `Developer ID Application: Your Name (TEAMID)`.
The script auto-detects this if there is exactly one; otherwise set
`SIGNING_IDENTITY` in `config.sh`.

> As of this writing, that command reports `0 valid identities found` on this
> machine, so this step has not been done yet.

### 2. Create an app-specific password

Notarization authenticates with an app-specific password, not your Apple ID
password.

1. Sign in at [appleid.apple.com](https://appleid.apple.com).
2. Under **Sign-In and Security → App-Specific Passwords**, generate one.

### 3. Store notarization credentials in the keychain

```bash
xcrun notarytool store-credentials "PurplePen-Notary" \
    --apple-id "peter@golde.org" \
    --team-id "YOURTEAMID" \
    --password "abcd-efgh-ijkl-mnop"
```

Your team ID appears at
[developer.apple.com/account](https://developer.apple.com/account) under
Membership details. The password is the app-specific one from step 2. This is
stored once; the script never handles it again.

## What the script does

1. **Publish** — `dotnet publish` of `AvPurplePen.csproj` in Release for
   `net10.0` / `osx-arm64`, self-contained, into the project's usual publish
   directory (`AvPurplePen/bin/Release/net10.0/osx-arm64/publish`).
2. **Stage** — `rsync --archive --delete --delete-excluded
   --exclude-from=publish-exclude.txt` from the publish directory into
   `build/staging`. This is where you control the bundle's contents.
3. **PDF helper** — republishes `PdfConverter` self-contained for the target
   RID and overlays it onto the payload, after checking that both resolve the
   same `Microsoft.NETCore.App` version. See the notes below for why this is
   necessary.
4. **Icon** — assembles `PurplePen.icns` from the pre-rendered PNGs in
   `AvPurplePen/Assets/AppIcon`, using the family named by `ICON_FAMILY`.
5. **Assemble** — builds `build/PurplePen.app` with `Contents/MacOS` (the
   staged payload), `Contents/Resources` (the icon) and a generated
   `Contents/Info.plist`, then strips extended attributes.
6. **Sign** — signs every Mach-O file inside the bundle from the inside out,
   then seals the bundle. Nested executables get the Hardened Runtime and
   entitlements; dynamic libraries get the Hardened Runtime alone.
7. **Notarize** — zips the bundle with `ditto`, submits it, waits for the
   result, staples the ticket into the `.app`, and confirms with `spctl`.
8. **Package** — builds the `.zip`, then the `.dmg`: a scratch read/write image
   is filled, its window laid out through Finder, detached, and compressed. The
   result is signed, notarized, stapled and then re-attached to verify.

Both the `.app` and the `.dmg` are stapled, so the app validates even when
extracted from the `.zip` on a machine with no network connection.

## Iterating

Notarization is the slow part (a few minutes per submission, twice). While
experimenting with `publish-exclude.txt`, skip it:

```bash
./build-mac-app.sh --skip-publish --skip-notarize
```

| Option | Effect |
|---|---|
| `--skip-publish` | Reuse the existing publish output. |
| `--skip-sign` | Build an unsigned bundle. Implies `--skip-notarize`. |
| `--skip-notarize` | Sign, but do not submit to Apple. |
| `--skip-dmg` | Do not build the `.dmg`. |
| `--skip-zip` | Do not build the `.zip`. |
| `--skip-style` | Build a plain, unstyled `.dmg` (no background, no icon layout). |
| `--dmg-only` | Rebuild only the `.dmg` from the existing `build/PurplePen.app`. |

`--dmg-only` is the one to use when adjusting the window layout — it takes
seconds instead of minutes:

```bash
./build-mac-app.sh --dmg-only --skip-sign
```

To run an unsigned build locally, clear its quarantine flag first:

```bash
xattr -dr com.apple.quarantine build/PurplePen.app
```

## Publishing

```bash
./publish-mac-app.sh
```

Builds, then files the result into the publishing tree — the directory whose
contents are uploaded to the download site — and records it in that tree's
`manifest.json`, which is what running copies of Purple Pen read to find out
that an update exists.

Three settings in `config.sh` control where it all goes:

| Setting | Default |
|---|---|
| `PUBLISH_TREE` | `~/Library/CloudStorage/OneDrive-Personal/Purple Pen/Downloads/root` |
| `PUBLISH_URL_ROOT` | `https://downloads.purple-pen.org` |
| `PUBLISH_SUBDIR` | `mac/arm64` |

`PUBLISH_TREE` is the same `root` the Windows and Linux builds publish into
(`Innosetup/publish-setup.bat` and `Installer/LinuxInstaller/config.sh`), and it
maps onto `PUBLISH_URL_ROOT` once uploaded. Both the directory copied into and
the URL recorded are derived from `PUBLISH_SUBDIR`, so they cannot drift apart.

To try it without touching the real tree:

```bash
PUBLISH_TREE=/tmp/testtree ./publish-mac-app.sh
```

Any other argument is passed straight through to `build-mac-app.sh`. A build
made with `--skip-sign` or `--skip-notarize` is refused: it would be offered to
users as an update that Gatekeeper then blocks. The check reads what the build
recorded about itself, so it catches `SKIP_NOTARIZE=1` left in the environment
just as well as the command-line flag.

The manifest entry's channel follows the version: a prerelease is published to
`beta`, a stable release to `main`. Its title comes from `Installer/GetVersion.cs`
reading the assembly inside the bundle that was just built — the same program the
Windows publish uses, so both platforms' entries are titled the same way.

Only the `.dmg` is published. The `.zip` is left in `output/`; the comment above
the manifest step in `publish-mac-app.sh` says what to change to publish it too,
and why you might want the update to download the `.zip` instead — a `.zip` is
expanded over the installed bundle and the application relaunches itself, while
a `.dmg` is only opened in Finder for the user to drag across by hand.

## The disk image window

The `.dmg` opens as a 640×400 window with the Purple Pen icon on the left, the
Applications folder on the right, and an arrow between them — the drag-to-install
idiom Mac users expect.

The artwork is [`dmg-background.svg`](dmg-background.svg), authored at 2× over a
`0 0 640 400` viewBox so its coordinates are the same numbers as the icon
positions in `config.sh`. The build rasterizes it with `sips` to 640×400 and
1280×800 and combines them with `tiffutil -cathidpicheck` into a
multi-resolution TIFF. That last step matters: Finder draws a background
picture unscaled at its natural point size, so a plain 640×400 PNG is soft on
Retina and a plain 1280×800 PNG would show only its top-left quarter.

Geometry lives in `config.sh` (`DMG_WINDOW_WIDTH`/`HEIGHT`, `DMG_ICON_SIZE`,
`DMG_APP_ICON_X`/`Y`, `DMG_APPS_ICON_X`/`Y`). The SVG's aspect ratio must match
the window size; the build reads the rasterized dimensions back and fails if
they disagree.

To edit the layout, change both the config numbers and the SVG together, then
`./build-mac-app.sh --dmg-only --skip-sign` and open the result.

### Finder Automation permission

Styling works by telling Finder how to lay out the window, so the first run
shows a **"Terminal wants to control Finder"** dialog. Approve it once and it
never asks again. The build probes this during preflight rather than at the
end, so it fails in the first few seconds rather than after notarization has
already spent minutes at Apple.

A build machine with no logged-in desktop session cannot script Finder at all.
Use `--skip-style` there — the disk image still works, it just looks
unfinished, and the build prints a warning saying so.

### Two things that would otherwise waste an afternoon

**Finder writes `.DS_Store` when the volume is ejected, not when the window is
closed.** Before the first detach the file is a 6 KB skeleton containing no
view settings at all, so there is nothing to verify while the image is still
mounted. The build therefore styles, detaches, and only then re-attaches to
confirm the layout landed.

**A volume of the same name already being mounted silently breaks styling.**
macOS would mount ours as `Purple Pen 1`, and the AppleScript — which addresses
the disk by name — would style the *other* volume and report success, shipping
an unstyled image. Preflight refuses to start if `/Volumes/Purple Pen` exists;
eject it with `hdiutil detach "/Volumes/Purple Pen"`. This is easy to trigger
by opening a previously built `.dmg` to compare.

### Verification

`read-dmg-layout.py` decodes the `.DS_Store` in the finished image and the
build fails if the background picture or icon positions are missing — an
unstyled disk image can never ship silently. Every build logs what it found:

```
Layout: background set, 128.0pt icons, window {{200, 703}, {640, 400}}, icons at (470,200) (170,200)
```

Checking that the file merely exists would prove nothing, which is why it is
decoded: macOS creates empty skeleton `.DS_Store` files routinely.

## Things to be aware of

**Switch between the beta and release icon with `ICON_FAMILY`.**
`AvPurplePen/Assets/AppIcon` holds two families of pre-rendered PNGs,
`PurplePen.<N>x<N>.png` and `PurplePenBeta.<N>x<N>.png`. `ICON_FAMILY` in
`config.sh` selects one; it defaults to **`PurplePenBeta`**. Set it to
`PurplePen` for a release build:

```bash
ICON_FAMILY=PurplePen ./build-mac-app.sh
```

The `.icns` is assembled from those PNGs at their **native** pixel sizes rather
than resampled from one large source, so any hand-tuning of the small sizes is
preserved. The script verifies each file's actual pixel width against its name
and aborts if they disagree.

macOS needs 16, 32, 64, 128, 256, 512 and 1024. The 24, 48 and 96 files in that
directory are unused here but map onto the freedesktop hicolor sizes Linux
packaging wants.

**The bundle is 533 MB, and 399 MB of that is waste.** The
`CopyPdfConverterToPublishOutput` target in `AvPurplePen.csproj` copies
PdfConverter's entire build output into the publish directory, including its
RID-agnostic `runtimes/` tree — native `pdfium` and `libSkiaSharp` binaries for
Windows, Linux and musl, none of which macOS ever loads. Adding a single
`runtimes/` line to `publish-exclude.txt` takes the bundle to **141 MB** with
the PDF helper still working. The commented block in that file has the measured
breakdown.

**The `PdfConverter` helper needs a separate self-contained publish.** The copy
that arrives from the main publish is a plain *build* output, so it is
framework-dependent: its apphost looks for a machine-wide .NET install, finds
the self-contained app's own `libhostfxr.dylib` sitting next to it, resolves
".NET location" to the app directory, finds no shared framework there and
refuses to start — even on a machine that does have .NET installed.

The script therefore republishes it self-contained for the target RID and
overlays it onto the payload (`INCLUDE_PDF_CONVERTER` in `config.sh`). Because
the helper lands in the same directory as the app, it shares every framework
assembly already present, so it costs about 7 MB — almost entirely
`libpdfium.dylib`, which a RID-specific publish flattens to the top level in
place of the cross-platform `runtimes/` tree.

Two consequences:

- Do **not** add `PdfConverter*` or `libpdfium.dylib` to `publish-exclude.txt`;
  the list is applied to the overlay too, so that would delete the working
  helper. Use `INCLUDE_PDF_CONVERTER=false` instead.
- The script aborts if the app and the helper resolve different
  `Microsoft.NETCore.App` patch versions, since the overlay would otherwise
  swap the runtime out from under the main app.

This only becomes useful once the lookup in
[PdfMapFile.cs:176](../../PurplePenCore/PdfMapFile.cs) stops hard-coding the
`.exe` extension — it currently returns `PdfConverter.exe` on every platform,
so the helper is never found on macOS. `AppContext.BaseDirectory` is also a
more robust way to locate it than `Assembly.Location`, which returns an empty
string under single-file publishing.

**Every file in `Contents/MacOS` must be signed, not just the Mach-O ones.**
That directory is the bundle's executables directory, so `codesign` treats
everything in it as nested code and refuses to seal the bundle while any single
file lacks a signature. It fails on a `.xml` documentation file just as readily
as on a `.dll`. Non-Mach-O files get signed as `Format=generic`. Signing only
the Mach-O binaries produces:

```
PurplePen.app: code object is not signed at all
In subcomponent: .../Contents/MacOS/System.Xml.Linq.dll
```

**Never pass the main executable's path to `codesign` directly.** Given
`Contents/MacOS/PurplePen`, `codesign` resolves it to the *enclosing bundle*
(`Format=app bundle`) rather than the file, and tries to seal the whole thing
prematurely. The per-file loop skips it; its signature comes from the final
bundle seal, which is also where its entitlements are applied.

**`LSMinimumSystemVersion` is set to 13.0.** This should match the minimum
macOS version supported by the .NET 10 runtime you are bundling. If it is set
too low, the app launches on an unsupported system and then crashes; verify it
against the current .NET 10 support matrix and adjust `MIN_MACOS_VERSION` in
`config.sh`.

**File associations are not enabled.** `Info.plist.template` contains a
commented-out `CFBundleDocumentTypes` block for `.ppen` files. Enabling it
makes Finder route double-clicked course files to Purple Pen, but AvPurplePen
does not yet handle the macOS "open document" event, so the app would launch
without opening the file. Uncomment once that is wired up.

**Apple Silicon only.** `RUNTIME_IDENTIFIER` is `osx-arm64`; the result will
not run on Intel Macs at all. To add Intel support later, either build a second
`osx-x64` distribution or merge both into a universal binary with `lipo`.

**Versions come from `PurplePenCore/VersionNumber.cs`.** The four-part version
(`4.0.0.110`) becomes `CFBundleVersion`; its first three components
(`4.0.0`) become the user-visible `CFBundleShortVersionString`. Apple requires
`CFBundleVersion` to increase with every release you submit.
