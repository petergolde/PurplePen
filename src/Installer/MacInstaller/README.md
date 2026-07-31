# Purple Pen macOS Installer

Builds a signed and notarized macOS distribution of Purple Pen — a `.dmg` for
normal installation and a `.zip` for automated deployment.

```bash
./build-mac-app.sh
```

Output lands in `output/`:

```
output/PurplePen-4.0.0-osx-arm64.dmg
output/PurplePen-4.0.0-osx-arm64.zip
```

## Files

| File | Purpose |
|---|---|
| `build-mac-app.sh` | The build script. Run this. |
| `config.sh` | Settings — bundle id, versions, signing identity, notarization profile. Every value can be overridden by an environment variable of the same name. |
| `publish-exclude.txt` | rsync exclusion list controlling exactly which published files go into the app bundle. Currently empty; customize as you experiment. |
| `Info.plist.template` | Bundle metadata, with `@PLACEHOLDER@` tokens filled in by the script. |
| `PurplePen.entitlements` | Hardened Runtime entitlements required to run .NET under notarization. |

`build/` (staging area, assembled `.app`) and `output/` are generated and
git-ignored.

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
4. **Icon** — generates `PurplePen.icns` at all required sizes from
   `AvPurplePen/Assets/PurplePenIcon.png`.
5. **Assemble** — builds `build/PurplePen.app` with `Contents/MacOS` (the
   staged payload), `Contents/Resources` (the icon) and a generated
   `Contents/Info.plist`, then strips extended attributes.
6. **Sign** — signs every Mach-O file inside the bundle from the inside out,
   then seals the bundle. Nested executables get the Hardened Runtime and
   entitlements; dynamic libraries get the Hardened Runtime alone.
7. **Notarize** — zips the bundle with `ditto`, submits it, waits for the
   result, staples the ticket into the `.app`, and confirms with `spctl`.
8. **Package** — builds the `.zip` and a `.dmg` (containing the app and an
   `/Applications` symlink) from the stapled app, then signs, notarizes and
   staples the `.dmg` as well.

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

To run an unsigned build locally, clear its quarantine flag first:

```bash
xattr -dr com.apple.quarantine build/PurplePen.app
```

## Things to be aware of

**The icon is low resolution.** The only icon in the repository is
`AvPurplePen/Assets/PurplePenIcon.png` at 64×64, so every larger size in the
`.icns` is upscaled and will look soft. Replacing that PNG with a 1024×1024
version is the only change needed — the script picks it up automatically and
warns until you do.

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
