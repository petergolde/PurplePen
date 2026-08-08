# Purple Pen Linux Installer

Builds `.deb`, `.rpm` and AppImage distributions of Purple Pen for Linux.

```bash
./build-linux-packages.sh
```

Output lands in `output/`:

```
output/purplepen_4.0.0~beta1-1_amd64.deb
output/purplepen-4.0.0~beta1-1.x86_64.rpm
output/PurplePen-4.0.0~beta1-x86_64.AppImage
```

All three install a **self-contained** build, so the target machine does not
need .NET installed. All three are built from one staged tree, so they ship
byte-identical application content and a bug cannot appear in one format but
not the others.

## Files

| File | Purpose |
|---|---|
| `build-linux-packages.sh` | The build script. Run this. |
| `config.sh` | Settings — package identity, dependencies, architecture, versioning, AppImage options. Every value can be overridden by an environment variable of the same name. |
| `publish-exclude.txt` | rsync exclusion list controlling exactly which published files go into the packages. |
| `purplepen.desktop.template` | Desktop menu entry. |
| `purplepen-mime.xml.template` | shared-mime-info definition registering `.ppen` files. |
| `AppRun.template` | The AppImage entry point, which sets up the bundled-library path. |
| `purplepen.appdata.xml.template` | AppStream metadata, so software centres and AppImage managers can describe the application. |

Templates use at-sign-delimited tokens that the script substitutes; it fails
the build if any are left over.

`build/` (staging) and `output/` are generated and git-ignored.

## Prerequisites

| Tool | Needed for | Install |
|---|---|---|
| .NET 10 SDK | always | [dotnet install docs](https://learn.microsoft.com/dotnet/core/install/linux) |
| `rsync`, `dpkg-deb` | the `.deb` | base system on Debian/Ubuntu; `dpkg` package elsewhere |
| `rpmbuild` | the `.rpm` | `sudo apt install rpm` / `sudo dnf install rpm-build` |
| `curl`, `ldconfig` | the AppImage | base system |
| `desktop-file-validate` | recommended | `desktop-file-utils` — the build validates both menu entries when present |
| `lintian` | optional | reports Debian policy notes, informational only |

You do **not** need a Fedora machine to build the `.rpm`; `rpmbuild` runs fine
on Debian and Ubuntu.

`appimagetool` is fetched automatically on first use and cached in
`~/.cache/purplepen-linuxinstaller`. The version and a per-architecture SHA-256
are pinned in `config.sh`, and the download is refused if the hash does not
match — a git tag can be moved and a release asset can be replaced, so the hash
is the only thing that actually pins what gets executed. Set `APPIMAGETOOL` to
use a copy you already have, or `--skip-appimage` to build without it.

**The AppImage build needs network access even when appimagetool is cached.**
appimagetool downloads its type-2 runtime from the AppImage project on every
run. Pre-fetch it and set `APPIMAGE_RUNTIME_FILE` to build offline — see that
setting in `config.sh`.

## Building from Windows via WSL

The script must run inside WSL, not from PowerShell:

```bash
wsl bash ./build-linux-packages.sh
```

Two things about a Windows checkout are worth knowing, because both were hit
while developing this and neither fails in an obvious way.

**Permissions cannot be stored on a Windows drive.** `/mnt/d` is mounted 9p
without the `metadata` option, so every file reports mode `0777` and `chmod` is
silently ignored. Staging a package there produces one that installs
world-writable files — including a world-writable executable, which is a local
privilege escalation on every machine that installs it. The script detects this
by creating a probe file, chmod-ing it and reading the mode back; when the check
fails it stages under `$TMPDIR` instead and says so. The finished packages are
still copied to `output/` either way. `BUILD_DIR` overrides the choice.

The build also sets every payload file's mode explicitly, from its *content*
rather than its name or current bits: ELF binaries get `0755`, everything else
`0644`. That makes the result identical no matter which filesystem it was
staged on, and the finished packages are re-inspected for stray writable files
before the build is allowed to succeed.

**`obj/` is shared with the Windows build.** `AvPurplePen.csproj` builds
PdfConverter through an MSBuild task with `Targets="Build"`, which performs no
NuGet restore, so it uses whatever `obj/project.assets.json` happens to be
there. If Visual Studio wrote it, the file names a fallback package folder that
does not exist in WSL and the publish dies well into the build:

```
error MSB4018: Unable to find fallback package folder
'C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages'
```

The script restores both projects explicitly up front so this cannot happen.
The cost is that alternating between a Windows build and a WSL build makes each
one re-restore; that is a few seconds, not a failure.

## What the script does

1. **Restore + publish** — `dotnet publish` of `AvPurplePen.csproj` in Release
   for `net10.0` / `linux-x64`, self-contained, into the project's usual
   publish directory.
2. **Stage** — `rsync --archive --delete --delete-excluded
   --exclude-from=publish-exclude.txt` into `build/payload`. This is where you
   control the package's contents.
3. **PDF helper** — republishes `PdfConverter` self-contained for the target
   RID and overlays it, after checking that both resolve the same
   `Microsoft.NETCore.App` version.
4. **Normalize permissions** — ELF binaries to `0755`, everything else `0644`,
   decided by reading each file's magic number.
5. **Assemble** — builds `build/tree` as the exact filesystem to be installed:
   the payload at `/opt/purplepen`, a `/usr/bin/purplepen` symlink, icons,
   the desktop entry, the MIME definition and a copyright file.
6. **Package** — `dpkg-deb --root-owner-group --build` for the `.deb`,
   `rpmbuild` against a generated spec for the `.rpm`, and `appimagetool` over
   an AppDir for the AppImage, all from that one tree.
7. **Verify** — reads all three finished artifacts back and fails on a
   non-executable apphost, a writable file, a non-root owner, a missing symlink
   or desktop entry, a version mismatch, bundled libraries leaking into RPM
   `Provides`, a bad AppImage magic number, or missing AppImage desktop
   integration.

## Iterating

```bash
./build-linux-packages.sh --skip-publish --deb-only
```

| Option | Effect |
|---|---|
| `--skip-publish` | Reuse the existing publish output. |
| `--deb-only` | Build only the `.deb`. |
| `--rpm-only` | Build only the `.rpm`. |
| `--appimage-only` | Build only the AppImage. |
| `--skip-appimage` | Build the `.deb` and `.rpm` only. Useful when offline. |
| `--skip-verify` | Do not inspect the finished artifacts. Not recommended. |
| `--show-deps` | List the shared libraries the payload links against and which package provides each, then exit. |

## Installing and removing

```bash
sudo apt install ./output/purplepen_4.0.0~beta1-1_amd64.deb
sudo dnf install ./output/purplepen-4.0.0~beta1-1.x86_64.rpm
```

Use `apt install ./file.deb` rather than `dpkg -i`: `dpkg` does not resolve
dependencies, so it leaves the package unconfigured if anything is missing.

```bash
sudo apt remove purplepen
sudo dnf remove purplepen
```

After installing, `purplepen` is on `PATH`, Purple Pen appears in the
applications menu, and double-clicking a `.ppen` file opens it.

## The AppImage

A single executable file. No installation, no root, no package manager:

```bash
chmod +x PurplePen-4.0.0~beta1-x86_64.AppImage
./PurplePen-4.0.0~beta1-x86_64.AppImage
./PurplePen-4.0.0~beta1-x86_64.AppImage ~/events/national.ppen
```

### What it carries for desktop integration

An AppImage does not install anything by itself. But if the user runs an
AppImage manager — [AppImageLauncher](https://github.com/TheAssassin/AppImageLauncher),
`appimaged`, Gearlever — that tool extracts this metadata and installs it, and
then Purple Pen appears in the menu with its icon and `.ppen` files open in it.
All of it is inside the image:

| Path in the AppImage | What it gives you |
|---|---|
| `purplepen.desktop` (root) | The entry the AppImage spec requires and managers read |
| `usr/share/applications/purplepen.desktop` | Where a manager copies it from |
| `purplepen.png` (root) + `.DirIcon` | The icon managers install, and what a file manager shows for the file itself |
| `usr/share/icons/hicolor/<N>x<N>/apps/` | All 9 sizes plus the scalable SVG |
| `usr/share/mime/packages/purplepen.xml` | The `.ppen` MIME type, which is what makes the file association possible |
| `usr/share/metainfo/purplepen.appdata.xml` | AppStream data — name, summary, description, categories, homepage |

The desktop entry also carries `X-AppImage-Version`, which managers display.
This key is added *only* to the AppImage's copy; the `.deb` and `.rpm` entries
must not claim to be AppImages, and the build keeps them separate.

Note the AppStream file is named after the *desktop entry*, not after the
component id, because that is what appimagetool looks for — it reports metadata
as missing otherwise, however correct the file inside is. The id stays
reverse-DNS (`org.purple-pen.PurplePen`, the same identity as the macOS bundle)
and `<launchable>` ties it back to the desktop entry.

### What is bundled, and why that is the whole design problem

This is the one place the AppImage genuinely differs from the `.deb`/`.rpm`.
Those declare dependencies and let the package manager satisfy them. **An
AppImage has no dependency resolution at all** — anything not inside it must
already exist on the host.

The AppImage project publishes an
[excludelist](https://github.com/AppImage/pkg2appimage/blob/master/excludelist)
of libraries that must *not* be bundled because they are tied to the host's
kernel, display server or font configuration. It covers nearly everything
Purple Pen touches — glibc, libstdc++, libgcc\_s, libX11, libICE, libSM,
fontconfig, freetype, expat, uuid, zlib. Bundling those causes the failures it
is meant to prevent; fontconfig in particular is documented as making
applications hang at startup.

**ICU is the exception, and it is bundled.** It is not on the excludelist, and
it is the one dependency whose absence is *fatal rather than degrading*:

```
Couldn't find a valid ICU package installed on the system.
```

.NET aborts at startup. Purple Pen is heavily localized, so
`InvariantGlobalization` is not an acceptable escape. `AppRun` prepends the
bundled copy to `LD_LIBRARY_PATH`, which is a floor rather than an override —
if the host has its own ICU, .NET's version probing may use that instead, and
either works.

This was verified rather than assumed. Masking the host's ICU inside a private
mount namespace and running both ways:

| | Result |
|---|---|
| Payload run directly, bundled ICU not on the path | aborts with the error above |
| Same host, run through `AppRun` | starts and runs normally |

So the bundle is both necessary and sufficient. It costs ~33 MB uncompressed
(28 MB of that is `libicudata`'s tables), which squashfs compresses to about
20 MB of the finished file.

**OpenSSL is deliberately *not* bundled** (`BUNDLE_OPENSSL=true` to change
that). Missing OpenSSL only degrades TLS — the application still starts and
every offline feature works; what breaks is the update check. Against that, a
bundled crypto library never receives security updates and can conflict with a
host's crypto policy, and every mainstream distribution ships OpenSSL.

### Which hosts it runs on

Because Purple Pen compiles **no native code** — every `.so` in the payload
comes prebuilt from Microsoft or SkiaSharp — the host requirement is fixed by
those binaries and **does not depend on the machine that built the AppImage**.
The usual AppImage advice to build inside an ancient distro does not apply.

Measured from the payload's own symbol versions:

| Requirement | Floor | First met by |
|---|---|---|
| glibc | 2.27 | Ubuntu 18.04, Debian 10, RHEL 8 |
| libstdc++ | GLIBCXX\_3.4.22 | GCC 6.1, comfortably older |

Re-measure after a .NET major upgrade with:

```bash
objdump -T build/payload/*.so | grep -o 'GLIBC_[0-9.]*' | sort -uV | tail -1
```

An AppImage mounts itself with FUSE to run. The widely repeated advice that
this needs `libfuse2` applies to the *old* AppImageKit runtime, not the type-2
runtime embedded here — **verified**: this AppImage self-mounts and runs on a
system with only `libfuse3` installed and no `libfuse.so.2` present at all.

Where FUSE genuinely is unavailable — many containers and CI runners — either
of these works without it:

```bash
./PurplePen-4.0.0~beta1-x86_64.AppImage --appimage-extract-and-run
APPIMAGE_EXTRACT_AND_RUN=1 ./PurplePen-4.0.0~beta1-x86_64.AppImage
```

The build handles the same problem for itself: appimagetool is an AppImage too,
so the script probes whether it can run and falls back to extract-and-run mode
automatically rather than failing with a confusing libfuse error.

## Install layout

```
/opt/purplepen/                                    the self-contained payload
/usr/bin/purplepen                    -> /opt/purplepen/PurplePen
/usr/share/applications/purplepen.desktop
/usr/share/icons/hicolor/<N>x<N>/apps/purplepen.png    9 sizes, 16 to 512
/usr/share/icons/hicolor/scalable/apps/purplepen.svg
/usr/share/pixmaps/purplepen.png                       48px legacy fallback
/usr/share/mime/packages/purplepen.xml
/usr/share/doc/purplepen/copyright
```

`/opt` is the conventional home for third-party bundles that ship their own
runtime; it keeps ~145 MB of .NET out of `/usr/lib`, which is meant for
distribution-managed libraries.

`/usr/bin/purplepen` is a **symlink**, not a wrapper script. .NET's apphost
finds its assemblies by resolving `/proc/self/exe`, which follows symlinks, so
the application directory comes out correct with nothing in between.

## Things to be aware of

**Switch between the beta and release icon with `ICON_FAMILY`.**
`AvPurplePen/Assets/AppIcon` holds two families of pre-rendered PNGs. It
defaults to `PurplePenBeta`; set it to `PurplePen` for a release build:

```bash
ICON_FAMILY=PurplePen ./build-linux-packages.sh
```

Icons are installed at their native pixel sizes rather than resampled from one
large source, so hand-tuning of the small sizes is preserved. The build checks
each PNG's real width against the size its name claims, because an icon
installed into the wrong hicolor directory looks broken in exactly one place
and nowhere else. The 1024px file is deliberately skipped: hicolor has no 1024
slot, so it would land in a directory no icon lookup searches.

**The prerelease stage is folded into the version, and this matters.**
`VersionNumber.cs` holds `4.0.0.210`, whose last component encodes the release
stage rather than a build number (100s alpha, 200s beta, 300s RC, 500 stable).
Shipping that verbatim would be wrong in a way that only surfaces much later:
both dpkg and rpm sort `4.0.0.210` *after* the eventual stable `4.0.0`, so
anyone who installed the beta would never be offered the release as an upgrade.
Both systems spell "sorts before" with a tilde, so the script produces
`4.0.0~beta1` instead, and `4.0.0` once the stage reaches 500.

Verify the ordering yourself with:

```bash
dpkg --compare-versions '4.0.0~beta1' lt '4.0.0' && echo correct
```

`PACKAGE_VERSION` overrides the derived value; `PACKAGE_RELEASE` is the
packaging revision, to bump when the packaging changes but the application does
not.

**The payload is ~145 MB, down from ~721 MB.** `AvPurplePen.csproj`'s
`CopyPdfConverterToPublishOutput` target copies PdfConverter's entire build
output into the publish directory, including its RID-agnostic `runtimes/` tree
— native pdfium and libSkiaSharp binaries for Windows, macOS, musl and every
Linux architecture, none of which this package loads. `publish-exclude.txt`
drops the whole tree.

That is only safe together with the PdfConverter overlay: `libpdfium.so` has no
copy at the publish root and exists *only* inside `runtimes/`. The overlay is
what puts it at the top level. Excluding `runtimes/` with
`INCLUDE_PDF_CONVERTER=false` silently removes PDF map template support.

**The `PdfConverter` helper needs a separate self-contained publish.** The copy
that arrives from the main publish is a plain *build* output, so it is
framework-dependent: its apphost finds the self-contained app's own
`libhostfxr.so` sitting next to it, resolves ".NET location" to the application
directory, finds no shared framework there and refuses to start — even on a
machine that does have .NET installed. The script republishes it self-contained
for the target RID and overlays it, which costs about 7 MB.

Two consequences:

- Do **not** add `PdfConverter*` or `libpdfium.so` to `publish-exclude.txt`;
  the list is applied to the overlay too, so that would delete the working
  helper. Use `INCLUDE_PDF_CONVERTER=false` instead.
- The script aborts if the app and the helper resolve different
  `Microsoft.NETCore.App` patch versions, since the overlay would otherwise
  swap the runtime out from under the main app.

**Dependencies are hand-maintained, and most of them are invisible to `ldd`.**
The payload's binaries record only four external `DT_NEEDED` dependencies —
`libc6`, `libgcc-s1`, `libstdc++6` and `libfontconfig1`. Everything else that
matters is opened with `dlopen` and so appears in no linker metadata at all:

| Loaded lazily | By | Consequence if missing |
|---|---|---|
| ICU | .NET globalization | fails at startup |
| OpenSSL | .NET TLS | update check fails |
| libX11, libICE, libSM | Avalonia's X11 backend | installs, then no window |

So the X11 entries in `DEB_DEPENDS` are *not* redundant even though nothing
links against them. Conversely expat, freetype, png, brotli, uuid and zlib are
deliberately **not** listed: they show up in `ldd` output only because
`libfontconfig1` depends on them, and it pulls them in by itself.

`--show-deps` reports the direct set with the providing package for each, and
lists the dlopen'd libraries separately as a reminder.

The Debian list spells the versioned libraries as alternatives
(`libicu76 | libicu74 | ...`) because those packages carry their soname in the
package name, so it differs on every distribution release. Extend the list as
new releases appear rather than reaching for an unversioned name, which does
not exist. RPM's `libicu` and `openssl-libs` are stable by comparison.

**The RPM dependency names target Fedora/RHEL.** openSUSE and Mageia name
several of these differently (`libX11-6`, `libopenssl3`), so a package built
here needs `RPM_REQUIRES` adjusted for those distributions.

**The spec turns off most of rpm's automation, deliberately.** `AutoReqProv: no`
stops rpm scanning the ~90 bundled `.so` files and generating both `Provides`
for them — which would advertise `libSkiaSharp.so` system-wide and let this
package be dragged in to satisfy an unrelated dependency — and `Requires` for
every symbol version they reference. `__os_install_post` is emptied because the
`brp-*` scripts would strip the bundled binaries. The build fails if bundled
libraries ever start leaking into `Provides` again.

**Only `linux-x64` has been built and tested.** `RUNTIME_IDENTIFIER` also
understands `linux-arm64`, `linux-arm` and `linux-x86`, and the architecture
names map through to all three formats, but nothing has been run on those. For
the AppImage they additionally need an appimagetool SHA-256 recorded for that
architecture in `config.sh`; the build refuses to download an unverified
binary rather than proceeding. musl targets are rejected outright — Alpine uses
apk, not deb or rpm.

**Package signing is not implemented.** `dpkg-sig`/`debsign` and
`rpm --addsign` are both GPG-based and mainly matter if you publish an apt or
yum repository; a directly downloaded package does not need one. There is no
Linux equivalent of macOS notarization.

**Wayland and printing.** Avalonia uses its X11 backend, so the packages depend
on `libx11-6`/`libX11` and run under XWayland on Wayland desktops. Printing
works by generating a PDF and handing it to the system viewer, which is why
`xdg-utils` is a Recommends.
