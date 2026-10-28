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
| `config.sh` | Settings — icon family, signing identity, notarization profile, publishing tree, disk image layout. Every value can be overridden by an environment variable of the same name. |
| `publish-exclude.txt` | rsync exclusion list controlling which files in the bundle's `Contents/MonoBundle` ship. |
| `PurplePen.entitlements` | Hardened Runtime entitlements required to run .NET under notarization. |

The bundle's metadata is not here. Since AvPurplePen targets `net10.0-macos`
on a Mac, the .NET macOS SDK builds the `.app` itself, for every build:

| File | Purpose |
|---|---|
| `AvPurplePen/AvPurplePen.csproj` | Bundle identifier (`ApplicationId`), minimum macOS (`SupportedOSPlatformVersion`), the icon (`BuildMacAppIcon` target), and the overrides described under [macOS SDK behavior](#macos-sdk-behavior). |
| `AvPurplePen/Info.plist` | Static `Info.plist` keys: menu-bar and Finder names, copyright, the `.ppen` document type. The SDK merges in the executable name, identifier, versions and minimum macOS. |

`build/` (the assembled, signed `.app`) and `output/` are generated and
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

### 4. Install the .NET macOS workload and the matching Xcode

```bash
sudo dotnet workload install macos
```

then install the Xcode version that workload needs — see the next section. The
build checks both before doing anything else.

## Xcode and the .NET macOS workload

AvPurplePen targets `net10.0-macos` on a Mac so that it can call AppKit and
other macOS APIs directly through the `Microsoft.macOS` bindings (inside
`#if MACOS`, which the SDK defines for that target framework). That target
framework comes from the .NET **macOS workload**, and building with it needs
**Xcode** — not just the command line tools. The SDK uses Xcode's `clang` and
macOS SDK to link the app's native launcher, its `install_name_tool` on the
bundled native libraries, and its `codesign`.

**Each workload release is built against exactly one Xcode, and the build
refuses any other.** Only the major.minor version is compared, but it has to
match exactly — a *newer* Xcode is rejected just like an older one:

```
error E0191: This version of .NET for macOS (26.5.10315) requires Xcode 26.6.
The current version of Xcode is 27.1.
```

`build-mac-app.sh` checks this in its first few seconds and says what to do.
To see where you stand by hand:

```bash
dotnet workload list
xcodebuild -version
dotnet msbuild ../../AvPurplePen/AvPurplePen.csproj -getProperty:_RecommendedXcodeVersion
```

The last command prints the Xcode version the installed workload wants.

**Keep several Xcodes side by side** and choose between them, rather than
letting the App Store update the one you build with. Download specific
versions from [developer.apple.com/download/all](https://developer.apple.com/download/all/),
and rename each as it is unpacked (`Xcode-26.6.app`, ...). Then either make
one the system default:

```bash
sudo xcode-select -s /Applications/Xcode-26.6.app
```

or select one for a single command, leaving the default alone — the SDK and
`xcodebuild` both honour `DEVELOPER_DIR`:

```bash
DEVELOPER_DIR=/Applications/Xcode-26.6.app/Contents/Developer ./build-mac-app.sh
```

**When the two get out of step.** The usual ways:

- *Xcode updated itself* (or a newer one became the default). Switch back to
  the matching one as above; nothing needs rebuilding.
- *The .NET SDK or workload was updated.* `dotnet workload update` (and some
  SDK updates) move to a new workload that may want a newer Xcode. Run the
  `-getProperty` command above, install that Xcode, and select it.
- *A brand new macOS.* A new major macOS usually needs a new Xcode, which needs
  a new workload. Microsoft ships a workload for each Xcode release, generally
  within days to weeks of Apple; until then, build with the previous Xcode
  (they run fine on the newer macOS in practice).

As a last resort, `-p:ValidateXcodeVersion=false` skips the check. The build
may then work or fail in obscure ways; do not ship from it.

Purple Pen's `LSMinimumSystemVersion` (13.0) is independent of all this: it is
set by `SupportedOSPlatformVersion` in AvPurplePen.csproj, and a build made
with the newest Xcode still runs on older macOS.

Recorded at the time of the switch: .NET SDK 10.0.300, macOS workload
26.5.10315, Xcode 26.6, on macOS 27.0.1.

## What the script does

1. **Publish** — `dotnet publish` of `AvPurplePen.csproj` in Release for
   `net10.0-macos` / `osx-arm64`, with `CreatePackage=false` (otherwise
   publish goes on to make a `.pkg`). The SDK builds the complete bundle at
   `AvPurplePen/bin/Release/net10.0-macos/osx-arm64/PurplePen.app`:

   | | |
   |---|---|
   | `Contents/MacOS/PurplePen` | the native launcher, linked by the SDK |
   | `Contents/MonoBundle/` | the .NET runtime, all managed assemblies, the native libraries (`libSkiaSharp`, `libpdfium`, ...), satellite resource directories, and the fonts, `symbols.xml` and sample event |
   | `Contents/Resources/PurplePen.icns` | built from the `ICON_FAMILY` PNGs |
   | `Contents/Info.plist` | merged from `AvPurplePen/Info.plist` and the project |

2. **Assemble** — copies that bundle to `build/PurplePen.app`, applying
   `publish-exclude.txt` to `Contents/MonoBundle`, and checks that the
   `Info.plist` versions match `VersionNumber.cs`. This is where you control
   the bundle's contents.
3. **Sign** — signs every Mach-O file inside the bundle from the inside out,
   then seals the bundle. Nested executables would get the Hardened Runtime and
   entitlements; dynamic libraries get the Hardened Runtime alone. With
   `--skip-sign` the bundle is ad-hoc signed instead, since Apple silicon will
   not run an unsigned executable at all.
4. **Notarize** — zips the bundle with `ditto`, submits it, waits for the
   result, staples the ticket into the `.app`, and confirms with `spctl`.
5. **Package** — builds the `.zip`, then the `.dmg`: a scratch read/write image
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
| `--skip-publish` | Reuse the `.app` the last publish built. |
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

To run a `--skip-sign` build somewhere other than where it was built, clear its
quarantine flag first:

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
preserved. That happens in the `BuildMacAppIcon` target in AvPurplePen.csproj,
which the script drives with `-p:MacIconFamily=$ICON_FAMILY`; ordinary
development builds get the beta icon. `iconutil` refuses an iconset with a
wrongly sized image, so a mislabelled PNG fails the build.

macOS needs 16, 32, 64, 128, 256, 512 and 1024. The 24, 48 and 96 files in that
directory are unused here but map onto the freedesktop hicolor sizes Linux
packaging wants.

**PDF map templates are rasterized in-process.** `PdfConverter` is a plain
library referenced by PurplePenCore, so its native `libpdfium.dylib` arrives
through the normal build and lands in `Contents/MonoBundle` like
`libSkiaSharp.dylib`. The assembly step warns if it is missing. It used to be a
separate helper executable that needed its own self-contained republish and
overlay; that is gone.

The `runtimes/` line in `publish-exclude.txt` dates from when AvPurplePen.csproj
copied PdfConverter's entire RID-agnostic build output into the publish
directory, which made the bundle 533 MB, 399 MB of it native binaries for other
platforms. That copy no longer happens; the exclusion stays as a safety net.

**Only the Mach-O files are signed individually.** Everything else — the
managed `.dll`s, fonts, `symbols.xml` — is in `Contents/MonoBundle`, which is
not one of the bundle's nested-code locations, so the final bundle seal records
it in `_CodeSignature/CodeResources` as an ordinary resource. (Under the old
`net10.0` layout all of it sat in `Contents/MacOS`, where `codesign` treats
every file as nested code and refused to seal the bundle until each `.xml` and
`.dll` had a signature of its own.) Every native library *is* signed:
notarization rejects any Mach-O binary anywhere in the bundle that lacks a
Developer ID signature with the Hardened Runtime and a secure timestamp.

**Never pass the main executable's path to `codesign` directly.** Given
`Contents/MacOS/PurplePen`, `codesign` resolves it to the *enclosing bundle*
(`Format=app bundle`) rather than the file, and tries to seal the whole thing
prematurely. The per-file loop skips it; its signature comes from the final
bundle seal, which is also where its entitlements are applied.

**`LSMinimumSystemVersion` is set to 13.0.** This should match the minimum
macOS version supported by the .NET 10 runtime you are bundling. If it is set
too low, the app launches on an unsupported system and then crashes; verify it
against the current .NET 10 support matrix and adjust `SupportedOSPlatformVersion`
in AvPurplePen.csproj — and the matching `SupportedOSPlatform("macos13.0")` in
`AvPurplePen/Properties/AssemblyInfo.cs`.

**`.ppen` files are associated with the app.** `AvPurplePen/Info.plist` exports
the `org.purple-pen.ppen` type and claims it in `CFBundleDocumentTypes`. Finder
does not pass the file on the command line: it sends an "open documents" event,
which `App.axaml.cs` handles through Avalonia's `IActivatableLifetime`, both at
launch and while the app is running. To check a freshly built bundle without
installing it, register it with Launch Services and `open` a `.ppen` file
(`lsregister` lives in
`/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/`;
`lsregister -f build/PurplePen.app` registers, `-u` unregisters).

**Apple Silicon only.** `RUNTIME_IDENTIFIER` is `osx-arm64`; the result will
not run on Intel Macs at all. To add Intel support later, either build a second
`osx-x64` distribution or merge both into a universal binary with `lipo`.

**Versions come from `PurplePenCore/VersionNumber.cs`.** The four-part version
(`4.0.0.110`) becomes `CFBundleVersion`; its first three components
(`4.0.0`) become the user-visible `CFBundleShortVersionString`. Apple requires
`CFBundleVersion` to increase with every release you submit. AvPurplePen.csproj
reads the file to set them, and the script, which parses it independently for
the file names, fails if the two disagree.

**The bundle is `PurplePen.app`, not `Purple Pen.app`.** In .NET 10 the SDK
names the bundle after `ApplicationTitle` when one is set, which is why the
project sets `CFBundleDisplayName` in `Info.plist` instead. Existing installs
are `PurplePen.app`; renaming would leave users who drag-install from the
`.dmg` with two copies side by side.

## macOS SDK behavior

Things about the `net10.0-macos` build that are not obvious, all found the hard
way while switching to it.

**Release builds get phone-app runtime defaults, two of which are overridden.**
The macOS SDK sets a batch of size-saving feature switches meant for iOS. Most
are harmless here (no EventSource, no metrics, no startup hooks, ...). Two are
not, and AvPurplePen.csproj turns them back off:

- `UseSystemResourceKeys` replaces .NET's own exception messages with bare
  resource keys. Purple Pen shows those messages in error dialogs and crash
  reports.
- `HybridGlobalization` takes culture data, casing and sorting from Apple's
  Foundation APIs instead of ICU, with documented differences and some
  `CompareOptions` that throw. Off, the Mac build uses the system ICU
  (`libicucore`) exactly as the earlier `net10.0` build and the Linux build do.
  The SDK sets it unconditionally after everything a project can set, so it is
  overridden in a target (`DisableMacHybridGlobalization`).

Check what a build actually got with
`strings PurplePen.app/Contents/MonoBundle/runtimeconfig.bin`.

Trimming stays off (`TrimMode=copy`); "Optimizing assemblies for size" in the
build output is the SDK's linker running its own steps, not removing anything.
The bundle is about 36 MB larger than the `net10.0` one, all of it
`Microsoft.macOS.dll`, the AppKit bindings.

**Content files need a `PublishFolderType`.** The SDK only knows where
assemblies, native libraries and bundle resources go; any other
`CopyToOutputDirectory` file is dropped with a warning that it "does not specify
a 'PublishFolderType'". The `PlaceMacContentFiles` target sends the fonts,
`symbols.xml` and the sample event to `Contents/MonoBundle`, beside the
assemblies, where `Util.GetFileInAppDirectory` looks. A new data file of some
other type needs adding there. `AppContext.BaseDirectory` and assembly
locations are `Contents/MonoBundle/`; `Environment.ProcessPath` is the launcher
in `Contents/MacOS/`.

**Never delete the built `.app` without its `obj/` directory.** The SDK
rewrites each native library's install name as it copies it into the bundle,
which invalidates the library's signature, and then re-signs it — unless a stamp
under `obj/<Config>/net10.0-macos/<RID>/codesign/` says it already did. Delete
just the `.app` and the next build skips the re-signing, and the kernel kills
the app on launch (`CODE SIGNING: ... rejecting invalid page`, exit status 137).
`build-mac-app.sh` deletes both. By hand, delete both too, or do a full
`dotnet clean`.

**`install_name_tool exited with code 72`** with screens of "Requested but did
not find extension point" from `xcodebuild` is `xcrun` failing to find the tool,
typically just after switching Xcode. It is transient: run
`xcrun --find install_name_tool` once and build again.

**`hdiutil attach` is deprecated as of macOS 27** in favour of
`diskutil image attach`, and prints a warning on every call. The script still
uses `hdiutil`, since `diskutil image` does not exist on earlier macOS, but
keeps its stderr out of the plist it parses — mixed together, they broke the
`.dmg` step.
