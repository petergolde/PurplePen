#!/bin/bash
#
# build-mac-app.sh
#
# Builds a signed and notarized macOS distribution of Purple Pen, producing
# both a .dmg and a .zip in the output/ directory.
#
# The pipeline is:
#
#   1. dotnet publish AvPurplePen (Release, net10.0, osx-arm64, self-contained)
#   2. rsync the publish output into build/staging, honouring publish-exclude.txt
#   3. Republish PdfConverter self-contained and overlay it onto the payload
#   4. Assemble build/PurplePen.app from the staging directory
#   5. Sign every file in Contents/MacOS inside-out, then seal the bundle
#   6. Notarize the app and staple the ticket to it
#   7. Build the .dmg and .zip from the stapled app, then notarize and staple
#      the .dmg as well
#
# Run with --help for options. See README.md for one-time setup.
#

set -euo pipefail

# ---------------------------------------------------------------------------
# Locate ourselves and load configuration
# ---------------------------------------------------------------------------

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

# shellcheck source=config.sh
source "$SCRIPT_DIR/config.sh"

PROJECT_FILE="$SRC_DIR/AvPurplePen/AvPurplePen.csproj"
VERSION_FILE="$SRC_DIR/PurplePenCore/VersionNumber.cs"
EXCLUDE_FILE="$SCRIPT_DIR/publish-exclude.txt"
PLIST_TEMPLATE="$SCRIPT_DIR/Info.plist.template"
ENTITLEMENTS="$SCRIPT_DIR/PurplePen.entitlements"
ICON_DIR="$SRC_DIR/AvPurplePen/Assets/AppIcon"
DMG_BACKGROUND_SVG="$SCRIPT_DIR/dmg-background.svg"
LAYOUT_READER="$SCRIPT_DIR/read-dmg-layout.py"

BUILD_DIR="$SCRIPT_DIR/build"
STAGING_DIR="$BUILD_DIR/staging"
APP_BUNDLE="$BUILD_DIR/$APP_NAME.app"
DMG_STAGE_DIR="$BUILD_DIR/dmg"
OUTPUT_DIR="$SCRIPT_DIR/output"

PUBLISH_DIR="$SRC_DIR/AvPurplePen/bin/$CONFIGURATION/$TARGET_FRAMEWORK/$RUNTIME_IDENTIFIER/publish"

# ---------------------------------------------------------------------------
# Output helpers
# ---------------------------------------------------------------------------

# Terminal colours, suppressed when not writing to a terminal.
if [[ -t 1 ]]; then
    C_STEP=$'\033[1;35m'; C_INFO=$'\033[0;36m'; C_WARN=$'\033[1;33m'
    C_ERR=$'\033[1;31m';  C_OK=$'\033[1;32m';   C_OFF=$'\033[0m'
else
    C_STEP=""; C_INFO=""; C_WARN=""; C_ERR=""; C_OK=""; C_OFF=""
fi

# step: announce a major phase of the build.
step() { printf '\n%s==> %s%s\n' "$C_STEP" "$*" "$C_OFF"; }

# info: report progress within a phase.
info() { printf '%s    %s%s\n' "$C_INFO" "$*" "$C_OFF"; }

# warn: report a non-fatal problem.
warn() { printf '%s    WARNING: %s%s\n' "$C_WARN" "$*" "$C_OFF" >&2; }

# die: report a fatal problem and exit.
die() { printf '\n%sERROR: %s%s\n' "$C_ERR" "$*" "$C_OFF" >&2; exit 1; }

# ---------------------------------------------------------------------------
# Disk image attach/detach bookkeeping
# ---------------------------------------------------------------------------
#
# Styling the disk image means having it mounted while Finder is told what to
# do with it. Under "set -e" any failure in between would otherwise leave a
# mounted volume and a live device node behind, which then breaks the *next*
# run (see the volume-name collision check in the preflight section). These
# globals let the EXIT trap clean up whatever state we were in.

DMG_ATTACH_IMAGE=""
DMG_ATTACH_DEV=""
DMG_ATTACH_MOUNT=""

# dev_for_image: print the whole-disk device node currently backing the disk
# image at $1, or nothing if it is not attached. Used by the cleanup trap to
# recover when a failure happened between attaching and recording the device.
dev_for_image() {
    [[ -n "$1" ]] || return 0
    hdiutil info -plist 2>/dev/null | /usr/bin/python3 -c '
import plistlib, sys
try:
    want = sys.argv[1]
    d = plistlib.loads(sys.stdin.buffer.read())
    for img in d.get("images", []):
        if img.get("image-path") == want:
            # The shortest dev-entry is the whole-disk node (/dev/diskN rather
            # than /dev/diskNs1), which is what detach wants.
            devs = sorted((e["dev-entry"] for e in img.get("system-entities", [])
                           if e.get("dev-entry")), key=len)
            if devs:
                print(devs[0])
            break
except Exception:
    pass
' "$1" 2>/dev/null || true
}

# detach_dmg: unmount the device node $1, retrying because Finder, Spotlight or
# a stray process can hold the volume busy for a few seconds after styling.
detach_dmg() {
    local dev="$1" i
    [[ -n "$dev" ]] || return 0
    sync
    for i in 1 2 3 4 5; do
        hdiutil detach "$dev" -quiet 2>/dev/null && return 0
        sleep 2
    done
    hdiutil detach "$dev" -force -quiet 2>/dev/null && return 0
    return 1
}

# on_exit: EXIT trap. Detaches any disk image still attached when the script
# stops, whether it stopped normally, via die, or via a set -e abort.
#
# Every command here is guarded: an unguarded failure inside an EXIT trap would
# silently skip the rest of the cleanup. This function must never call die,
# which would re-enter exit from within the trap.
on_exit() {
    if [[ -n "$DMG_ATTACH_IMAGE" || -n "$DMG_ATTACH_DEV" ]]; then
        if [[ -z "$DMG_ATTACH_DEV" ]]; then
            DMG_ATTACH_DEV="$(dev_for_image "$DMG_ATTACH_IMAGE")"
        fi
        if [[ -n "$DMG_ATTACH_DEV" ]]; then
            warn "Detaching the scratch disk image left behind by a failed build."
            detach_dmg "$DMG_ATTACH_DEV" \
                || warn "Could not detach $DMG_ATTACH_DEV. Run: hdiutil detach $DMG_ATTACH_DEV -force"
        fi
    fi
}

trap on_exit EXIT
# Bash does not reliably run the EXIT trap when killed by an untrapped signal;
# turning these into an exit makes Ctrl-C clean up after itself.
trap 'exit 130' INT
trap 'exit 143' TERM

# ---------------------------------------------------------------------------
# Command line
# ---------------------------------------------------------------------------

SKIP_PUBLISH="${SKIP_PUBLISH:-0}"
SKIP_SIGN="${SKIP_SIGN:-0}"
SKIP_NOTARIZE="${SKIP_NOTARIZE:-0}"
SKIP_DMG="${SKIP_DMG:-0}"
SKIP_ZIP="${SKIP_ZIP:-0}"
DMG_ONLY=0

# usage: print command line help.
usage() {
    cat <<'EOF'
Usage: build-mac-app.sh [options]

Builds a signed, notarized Purple Pen .app, .dmg and .zip for macOS.

Options:
  --skip-publish    Reuse the existing dotnet publish output instead of
                    rebuilding. Useful when iterating on publish-exclude.txt.
  --skip-sign       Do not code sign. Implies --skip-notarize. The resulting
                    app will only run locally after clearing its quarantine
                    attribute. Useful for testing bundle layout.
  --skip-notarize   Sign, but do not submit to Apple for notarization. Much
                    faster; the app will run on this machine but will be
                    blocked by Gatekeeper elsewhere.
  --skip-dmg        Do not produce a .dmg.
  --skip-zip        Do not produce a distribution .zip.
  --skip-style      Build a plain, unstyled .dmg: no background picture and no
                    icon positioning. Use on a build machine with no desktop
                    session, where Finder cannot be scripted. The result looks
                    unfinished, so do not ship it.
  --dmg-only        Skip straight to building the .dmg from the .app already in
                    build/. Intended for iterating on the window layout; takes
                    seconds instead of minutes.
  -h, --help        Show this message.

Configuration lives in config.sh; every setting there can also be given as an
environment variable. See README.md for one-time signing and notarization setup.
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --skip-publish)  SKIP_PUBLISH=1 ;;
        --skip-sign)     SKIP_SIGN=1; SKIP_NOTARIZE=1 ;;
        --skip-notarize) SKIP_NOTARIZE=1 ;;
        --skip-dmg)      SKIP_DMG=1 ;;
        --skip-zip)      SKIP_ZIP=1 ;;
        --skip-style)    DMG_STYLE=0 ;;
        --dmg-only)      DMG_ONLY=1 ;;
        -h|--help)       usage; exit 0 ;;
        *)               usage >&2; die "Unknown option: $1" ;;
    esac
    shift
done

# ---------------------------------------------------------------------------
# Preflight checks
# ---------------------------------------------------------------------------

step "Checking prerequisites"

[[ "$(uname -s)" == "Darwin" ]] || die "This script must be run on macOS."

for tool in dotnet rsync sips iconutil hdiutil ditto plutil; do
    command -v "$tool" >/dev/null 2>&1 || die "Required tool not found: $tool"
done

if [[ "$SKIP_SIGN" == "0" ]]; then
    command -v codesign >/dev/null 2>&1 || die "codesign not found. Install the Xcode command line tools."
fi

if [[ "$SKIP_NOTARIZE" == "0" ]]; then
    xcrun notarytool --version >/dev/null 2>&1 \
        || die "xcrun notarytool is unavailable. Install Xcode 13 or later, or run with --skip-notarize."
fi

[[ -f "$PROJECT_FILE" ]] || die "Cannot find AvPurplePen project at $PROJECT_FILE"
[[ -f "$PLIST_TEMPLATE" ]] || die "Cannot find Info.plist template at $PLIST_TEMPLATE"
[[ -f "$ENTITLEMENTS" ]] || die "Cannot find entitlements at $ENTITLEMENTS"
[[ -d "$ICON_DIR" ]] || die "Cannot find the icon directory at $ICON_DIR"

if [[ ! -f "$EXCLUDE_FILE" ]]; then
    warn "No publish-exclude.txt found; nothing will be excluded from the bundle."
    EXCLUDE_FILE=""
fi

# Checks that only matter when a styled disk image is actually going to be
# built. These run here, at the very start, rather than inside build_dmg --
# which runs last, after notarization has already spent several minutes at
# Apple. Failing at second five instead of minute twelve is the whole point.
if [[ "$SKIP_DMG" == "0" && "$DMG_STYLE" == "1" ]]; then
    for tool in osascript tiffutil; do
        command -v "$tool" >/dev/null 2>&1 \
            || die "Required tool not found: $tool (needed to style the disk image; --skip-style builds without it)"
    done

    [[ -f "$DMG_BACKGROUND_SVG" ]] \
        || die "Cannot find the disk image background at $DMG_BACKGROUND_SVG"
    [[ -f "$LAYOUT_READER" ]] \
        || die "Cannot find the layout reader at $LAYOUT_READER"

    # A volume of the same name already being mounted is the nastiest failure
    # mode here: macOS would mount ours as "<name> 1", and the AppleScript,
    # which addresses the disk by name, would style the OTHER volume and report
    # success -- shipping an unstyled disk image.
    if [[ -e "/Volumes/$DMG_VOLUME_NAME" ]]; then
        die "A volume named '$DMG_VOLUME_NAME' is already mounted.

That would make the styling step operate on the wrong volume and silently
produce an unstyled disk image. Eject it first:

    hdiutil detach \"/Volumes/$DMG_VOLUME_NAME\""
    fi

    # Front-load the one-time Automation consent dialog, and fail clearly on a
    # machine with no desktop session.
    if ! osascript -e 'tell application "Finder" to get name of startup disk' >/dev/null 2>&1; then
        die "Cannot drive Finder through AppleScript, which is needed to style the disk image.

If macOS showed an Automation consent dialog, approve it and re-run. Otherwise
grant it manually in System Settings > Privacy & Security > Automation, by
ticking Finder under this terminal application.

On a build machine with no logged-in desktop session, use --skip-style."
    fi
fi

info "dotnet SDK $(dotnet --version)"

# ---------------------------------------------------------------------------
# Determine the version number
# ---------------------------------------------------------------------------

# read_version: extract the current version from PurplePenCore/VersionNumber.cs
# and derive the two version strings the Info.plist needs.
#
# VersionNumber.cs holds a four-part version such as "4.0.0.110". Apple expects
# CFBundleShortVersionString to be at most three integer components, so the
# fourth (the Purple Pen build/prerelease number) is dropped from the
# user-visible version but retained in CFBundleVersion.
read_version() {
    [[ -f "$VERSION_FILE" ]] || die "Cannot find $VERSION_FILE"

    FULL_VERSION="$(sed -n 's/.*Current[[:space:]]*=[[:space:]]*"\([0-9.]*\)".*/\1/p' "$VERSION_FILE" | head -1)"
    [[ -n "$FULL_VERSION" ]] || die "Could not parse the version number out of $VERSION_FILE"

    SHORT_VERSION="$(echo "$FULL_VERSION" | cut -d. -f1-3)"
    BUILD_VERSION="$FULL_VERSION"
}

read_version
info "Purple Pen version $FULL_VERSION (short version $SHORT_VERSION)"

# Base name used for the .dmg and .zip files.
DIST_BASENAME="$APP_NAME-$SHORT_VERSION-$RUNTIME_IDENTIFIER"

# ---------------------------------------------------------------------------
# Resolve the signing identity
# ---------------------------------------------------------------------------

# resolve_signing_identity: fill in SIGNING_IDENTITY if the user left it empty,
# by finding the single "Developer ID Application" certificate in the keychain.
# Fails with an explanatory message if there is not exactly one.
resolve_signing_identity() {
    if [[ -n "$SIGNING_IDENTITY" ]]; then
        info "Signing identity: $SIGNING_IDENTITY (from config)"
        return
    fi

    local identities count
    identities="$(security find-identity -v -p codesigning 2>/dev/null \
                  | sed -n 's/.*"\(Developer ID Application:[^"]*\)".*/\1/p')"
    count="$(printf '%s' "$identities" | grep -c . || true)"

    if [[ "$count" -eq 0 ]]; then
        die "No 'Developer ID Application' certificate found in the keychain.

Create one at https://developer.apple.com/account/resources/certificates
(choose 'Developer ID Application'), download it, and double-click to install.
Then re-run this script.

To build an unsigned bundle for local testing, use --skip-sign."
    elif [[ "$count" -gt 1 ]]; then
        die "Found $count 'Developer ID Application' certificates:

$identities

Set SIGNING_IDENTITY in config.sh to the one you want to use."
    fi

    SIGNING_IDENTITY="$identities"
    info "Signing identity: $SIGNING_IDENTITY (auto-detected)"
}

# resolve_notary_credentials: build the notarytool authentication arguments,
# preferring explicit environment credentials over the keychain profile.
# Populates the NOTARY_ARGS array.
resolve_notary_credentials() {
    NOTARY_ARGS=()

    if [[ -n "$NOTARY_APPLE_ID" ]]; then
        [[ -n "$NOTARY_TEAM_ID" ]] || die "NOTARY_APPLE_ID is set but NOTARY_TEAM_ID is not."
        [[ -n "$NOTARY_PASSWORD" ]] || die "NOTARY_APPLE_ID is set but NOTARY_PASSWORD is not."
        NOTARY_ARGS=(--apple-id "$NOTARY_APPLE_ID" --team-id "$NOTARY_TEAM_ID" --password "$NOTARY_PASSWORD")
        info "Notarizing as $NOTARY_APPLE_ID (team $NOTARY_TEAM_ID)"
        return
    fi

    if ! xcrun notarytool history --keychain-profile "$NOTARY_KEYCHAIN_PROFILE" >/dev/null 2>&1; then
        die "The notarytool keychain profile '$NOTARY_KEYCHAIN_PROFILE' does not exist or is invalid.

Create it once with:

    xcrun notarytool store-credentials \"$NOTARY_KEYCHAIN_PROFILE\" \\
        --apple-id \"your-apple-id@example.com\" \\
        --team-id \"YOURTEAMID\" \\
        --password \"abcd-efgh-ijkl-mnop\"

The password is an app-specific password created at https://appleid.apple.com,
not your Apple ID password. Your team ID is shown at
https://developer.apple.com/account under Membership details.

To build a signed but un-notarized app, use --skip-notarize."
    fi

    NOTARY_ARGS=(--keychain-profile "$NOTARY_KEYCHAIN_PROFILE")
    info "Notarizing with keychain profile '$NOTARY_KEYCHAIN_PROFILE'"
}

if [[ "$SKIP_SIGN" == "0" ]]; then
    resolve_signing_identity
fi

if [[ "$SKIP_NOTARIZE" == "0" ]]; then
    resolve_notary_credentials
fi

# ---------------------------------------------------------------------------
# Step 1: publish
# ---------------------------------------------------------------------------

# publish_app: run dotnet publish for AvPurplePen into its default publish
# directory. No -o is passed, so the output lands in the conventional
# bin/<Config>/<Framework>/<RID>/publish path inside the project.
publish_app() {
    step "Publishing AvPurplePen ($CONFIGURATION, $TARGET_FRAMEWORK, $RUNTIME_IDENTIFIER)"

    if [[ "$SKIP_PUBLISH" == "1" ]]; then
        [[ -d "$PUBLISH_DIR" ]] || die "--skip-publish was given but $PUBLISH_DIR does not exist."
        info "Skipped; reusing $PUBLISH_DIR"
        return
    fi

    # Remove stale output so excluded-then-reincluded files cannot linger.
    rm -rf "$PUBLISH_DIR"

    dotnet publish "$PROJECT_FILE" \
        --configuration "$CONFIGURATION" \
        --framework "$TARGET_FRAMEWORK" \
        --runtime "$RUNTIME_IDENTIFIER" \
        --self-contained "$SELF_CONTAINED" \
        -p:PublishReadyToRun="$PUBLISH_READYTORUN" \
        -p:UseAppHost=true \
        -p:DebugType=none \
        --nologo

    [[ -d "$PUBLISH_DIR" ]] || die "Publish succeeded but $PUBLISH_DIR does not exist. Check the paths in config.sh."
    [[ -f "$PUBLISH_DIR/$APP_NAME" ]] \
        || die "Publish output does not contain an executable named '$APP_NAME'. Check APP_NAME in config.sh against AvPurplePen's <AssemblyName>."

    info "Published to $PUBLISH_DIR"
}

# ---------------------------------------------------------------------------
# Step 2: stage
# ---------------------------------------------------------------------------

# stage_payload: mirror the publish output into build/staging, dropping
# anything matched by publish-exclude.txt. This is the directory whose contents
# become Contents/MacOS in the app bundle, so it is the place to inspect when
# tuning the exclusion list.
stage_payload() {
    step "Staging bundle payload"

    mkdir -p "$STAGING_DIR"

    local rsync_args=(--archive --delete --delete-excluded)
    if [[ -n "$EXCLUDE_FILE" ]]; then
        rsync_args+=(--exclude-from="$EXCLUDE_FILE")
    fi

    rsync "${rsync_args[@]}" "$PUBLISH_DIR/" "$STAGING_DIR/"

    [[ -f "$STAGING_DIR/$APP_NAME" ]] \
        || die "The main executable '$APP_NAME' was excluded from the payload. Check publish-exclude.txt."

    local file_count size
    file_count="$(find "$STAGING_DIR" -type f | wc -l | tr -d ' ')"
    size="$(du -sh "$STAGING_DIR" | cut -f1 | tr -d ' ')"
    info "Staged $file_count files ($size) in $STAGING_DIR"
}

# ---------------------------------------------------------------------------
# Step 2b: the PdfConverter helper
# ---------------------------------------------------------------------------

# framework_version_of: print the Microsoft.NETCore.App version a
# runtimeconfig.json resolves to, whether it is framework-dependent
# ("framework") or self-contained ("includedFrameworks"). $1 is the path to the
# runtimeconfig.json.
framework_version_of() {
    /usr/bin/python3 - "$1" <<'PY'
import json, sys
with open(sys.argv[1]) as f:
    opts = json.load(f)["runtimeOptions"]
frameworks = opts.get("includedFrameworks") or ([opts["framework"]] if "framework" in opts else [])
for fw in frameworks:
    if fw.get("name") == "Microsoft.NETCore.App":
        print(fw.get("version", ""))
        break
PY
}

# stage_pdf_converter: republish PdfConverter self-contained for this RID and
# overlay it onto the staged payload.
#
# PurplePenCore/PdfMapFile.cs launches this helper as a separate process to
# rasterize PDF map templates. The copy that AvPurplePen.csproj drops into the
# publish directory is a plain build output and is framework-dependent, so it
# cannot start inside a self-contained bundle. A RID-specific self-contained
# publish fixes that and additionally flattens libpdfium.dylib to the top
# level, replacing the cross-platform runtimes/ tree it would otherwise need.
#
# Because the helper ends up in the same directory as the app, it shares every
# framework assembly already present and adds only its own code plus libpdfium.
stage_pdf_converter() {
    step "Staging the PdfConverter helper"

    if [[ "$INCLUDE_PDF_CONVERTER" != "true" ]]; then
        info "Skipped (INCLUDE_PDF_CONVERTER is not true)."
        return
    fi

    local project="$SRC_DIR/PdfConverter/PdfConverter.csproj"
    if [[ ! -f "$project" ]]; then
        warn "PdfConverter project not found at $project; skipping the helper."
        return
    fi

    local helper_dir="$BUILD_DIR/pdfconverter"
    rm -rf "$helper_dir"

    # BaseOutputPath redirects PdfConverter's bin/ into our build directory.
    #
    # This is not cosmetic. AvPurplePen.csproj's CopyPdfConverterToPublishOutput
    # target globs PdfConverter/bin/$(Configuration)/net10.0/**/*.* recursively.
    # Publishing here with a RID would otherwise create
    # PdfConverter/bin/Release/net10.0/<rid>/ inside the source tree, and the
    # NEXT full run of this script would sweep that entire directory -- a second
    # copy of the helper plus a self-contained .NET runtime -- into the app
    # payload. Left unchecked it roughly doubles the file count on every build.
    dotnet publish "$project" \
        --configuration "$CONFIGURATION" \
        --framework "$TARGET_FRAMEWORK" \
        --runtime "$RUNTIME_IDENTIFIER" \
        --self-contained true \
        -p:PublishReadyToRun="$PUBLISH_READYTORUN" \
        -p:UseAppHost=true \
        -p:DebugType=none \
        -p:BaseOutputPath="$BUILD_DIR/pdfconverter-bin/" \
        --output "$helper_dir" \
        --nologo \
        || die "Failed to publish PdfConverter."

    # The overlay overwrites shared framework assemblies. If the helper and the
    # app resolved different patch versions of Microsoft.NETCore.App, that
    # would silently downgrade or upgrade the runtime underneath the main app,
    # so refuse rather than ship a subtly broken bundle.
    local app_fw helper_fw
    app_fw="$(framework_version_of "$STAGING_DIR/$APP_NAME.runtimeconfig.json")"
    helper_fw="$(framework_version_of "$helper_dir/PdfConverter.runtimeconfig.json")"

    if [[ -n "$app_fw" && -n "$helper_fw" && "$app_fw" != "$helper_fw" ]]; then
        die "Runtime version mismatch between the app and the PdfConverter helper.

    $APP_NAME     targets Microsoft.NETCore.App $app_fw
    PdfConverter targets Microsoft.NETCore.App $helper_fw

Overlaying the helper would replace the app's framework assemblies with a
different version. Rebuild both with the same SDK, or set
INCLUDE_PDF_CONVERTER=false to leave the helper out."
    fi
    info "Both target Microsoft.NETCore.App $app_fw"

    # No --delete here: this is an overlay onto the existing payload. The same
    # exclusion list is applied so the helper obeys the same rules.
    local rsync_args=(--archive)
    if [[ -n "$EXCLUDE_FILE" ]]; then
        rsync_args+=(--exclude-from="$EXCLUDE_FILE")
    fi
    rsync "${rsync_args[@]}" "$helper_dir/" "$STAGING_DIR/"

    if [[ ! -f "$STAGING_DIR/PdfConverter" ]]; then
        warn "PdfConverter was excluded by publish-exclude.txt; PDF map templates will not work."
        return
    fi

    local size
    size="$(du -sh "$STAGING_DIR" | cut -f1 | tr -d ' ')"
    info "Payload is now $size with the helper included"
}

# ---------------------------------------------------------------------------
# Step 3: build the icon and assemble the bundle
# ---------------------------------------------------------------------------

# build_icon: assemble an .icns from the pre-rendered PNGs in $ICON_DIR.
#
# The icons are taken at their native pixel sizes rather than resampled from a
# single large source, so whatever tuning was done to the small sizes survives
# into the bundle.
#
# An .iconset maps a logical point size and a pixel density to a file, so each
# logical size needs two files: icon_32x32.png is 32 pixels, icon_32x32@2x.png
# is 64. That means 256 and 512 each appear twice under different names, which
# is expected.
build_icon() {
    step "Building application icon"

    local iconset="$BUILD_DIR/$APP_NAME.iconset"
    local icns="$BUILD_DIR/$APP_NAME.icns"

    rm -rf "$iconset"
    mkdir -p "$iconset"

    # "<iconset name>:<pixel size>"
    local entries=(
        "icon_16x16.png:16"       "icon_16x16@2x.png:32"
        "icon_32x32.png:32"       "icon_32x32@2x.png:64"
        "icon_128x128.png:128"    "icon_128x128@2x.png:256"
        "icon_256x256.png:256"    "icon_256x256@2x.png:512"
        "icon_512x512.png:512"    "icon_512x512@2x.png:1024"
    )

    local entry name size source actual
    for entry in "${entries[@]}"; do
        name="${entry%%:*}"
        size="${entry##*:}"
        source="$ICON_DIR/$ICON_FAMILY.${size}x${size}.png"

        [[ -f "$source" ]] || die "Missing icon: $source

The .icns needs the $ICON_FAMILY family at sizes 16, 32, 64, 128, 256, 512 and
1024. Check ICON_FAMILY in config.sh against the files in $ICON_DIR."

        # Trust the pixel dimensions, not the file name.
        actual="$(sips -g pixelWidth "$source" 2>/dev/null | awk '/pixelWidth/ {print $2}')"
        if [[ "$actual" != "$size" ]]; then
            die "Icon $source is ${actual}px wide but its name claims ${size}px."
        fi

        cp "$source" "$iconset/$name"
    done

    iconutil --convert icns "$iconset" --output "$icns" \
        || die "iconutil failed to build the .icns from $iconset"
    rm -rf "$iconset"

    ICNS_FILE="$icns"
    info "Built $(basename "$icns") from the $ICON_FAMILY icons (${#entries[@]} sizes)"
}

# assemble_bundle: lay out PurplePen.app from the staged payload, the generated
# icon, and the Info.plist template.
assemble_bundle() {
    step "Assembling $APP_NAME.app"

    rm -rf "$APP_BUNDLE"
    mkdir -p "$APP_BUNDLE/Contents/MacOS"
    mkdir -p "$APP_BUNDLE/Contents/Resources"

    # -a preserves permissions and symlinks; the trailing /. copies the
    # directory contents rather than the directory itself.
    cp -a "$STAGING_DIR/." "$APP_BUNDLE/Contents/MacOS/"

    cp "$ICNS_FILE" "$APP_BUNDLE/Contents/Resources/$APP_NAME.icns"

    write_info_plist

    # The apphost must be executable. dotnet publish normally sets this, but
    # the bit is lost if the tree ever passes through a Windows filesystem.
    chmod +x "$APP_BUNDLE/Contents/MacOS/$APP_NAME"

    # Finder metadata files get sealed into the signature, and Finder may
    # rewrite them later, which invalidates it. Remove them before signing.
    local ds_count
    ds_count="$(find "$APP_BUNDLE" -name '.DS_Store' -type f -print -delete | wc -l | tr -d ' ')"
    if [[ "$ds_count" -gt 0 ]]; then
        info "Removed $ds_count .DS_Store file(s) from the bundle"
    fi

    # Extended attributes (notably com.apple.quarantine and resource forks)
    # make codesign fail with "resource fork, Finder information, or similar
    # detritus not allowed".
    xattr -cr "$APP_BUNDLE"

    local size
    size="$(du -sh "$APP_BUNDLE" | cut -f1 | tr -d ' ')"
    info "Assembled $APP_BUNDLE ($size)"
}

# write_info_plist: substitute the @PLACEHOLDER@ tokens in the template and
# write Contents/Info.plist, then validate that the result parses.
write_info_plist() {
    local plist="$APP_BUNDLE/Contents/Info.plist"

    sed -e "s|@EXECUTABLE@|$APP_NAME|g" \
        -e "s|@BUNDLE_NAME@|$BUNDLE_NAME|g" \
        -e "s|@DISPLAY_NAME@|$DISPLAY_NAME|g" \
        -e "s|@BUNDLE_ID@|$BUNDLE_ID|g" \
        -e "s|@ICON_NAME@|$APP_NAME.icns|g" \
        -e "s|@SHORT_VERSION@|$SHORT_VERSION|g" \
        -e "s|@BUILD_VERSION@|$BUILD_VERSION|g" \
        -e "s|@COPYRIGHT@|$COPYRIGHT|g" \
        -e "s|@MIN_MACOS_VERSION@|$MIN_MACOS_VERSION|g" \
        "$PLIST_TEMPLATE" > "$plist"

    plutil -lint "$plist" >/dev/null || die "The generated Info.plist is not valid. Check Info.plist.template."

    if grep -q '@[A-Z_]*@' "$plist"; then
        die "The generated Info.plist still contains unsubstituted placeholders: $(grep -o '@[A-Z_]*@' "$plist" | sort -u | tr '\n' ' ')"
    fi
}

# ---------------------------------------------------------------------------
# Step 4: sign
# ---------------------------------------------------------------------------

# sign_bundle: code sign the app bundle from the inside out.
#
# Every Mach-O file inside the bundle must carry its own signature before the
# bundle itself is sealed, otherwise codesign rejects the outer signature.
# Nested executables are signed with the Hardened Runtime and entitlements;
# plain dynamic libraries get the Hardened Runtime but no entitlements, since
# entitlements only take effect on the process's main executable.
sign_bundle() {
    step "Code signing"

    if [[ "$SKIP_SIGN" == "1" ]]; then
        warn "Skipped. The resulting app is unsigned and will be blocked by Gatekeeper."
        warn "To run it locally: xattr -dr com.apple.quarantine \"$APP_BUNDLE\""
        return
    fi

    local signed_libs=0 signed_execs=0 signed_managed=0 file kind output

    # -print0 / read -d '' so that filenames containing spaces are handled.
    while IFS= read -r -d '' file; do
        # Skip the bundle's main executable. Handing codesign the path
        # Contents/MacOS/<CFBundleExecutable> does not sign that file -- codesign
        # resolves it to the enclosing bundle and tries to seal the whole thing,
        # which fails here because the nested libraries are not signed yet and
        # because sealing walks the hundreds of managed PE .dll files alongside
        # it. The main executable is signed by the bundle seal at the end of
        # this function, which is where its entitlements come from anyway.
        if [[ "$file" == "$APP_BUNDLE/Contents/MacOS/$APP_NAME" ]]; then
            continue
        fi

        kind="$(file -b "$file")"

        case "$kind" in
            *Mach-O*executable*)
                # Entitlements only take effect on a process's main
                # executable, so they are applied here but not to libraries.
                if ! output="$(codesign --force --timestamp --options=runtime \
                                        --entitlements "$ENTITLEMENTS" \
                                        --sign "$SIGNING_IDENTITY" "$file" 2>&1)"; then
                    echo "$output" | sed 's/^/    /' >&2
                    die "Failed to sign executable: $file"
                fi
                signed_execs=$((signed_execs + 1))
                ;;
            *Mach-O*)
                # Shared libraries, bundles and dylibs.
                if ! output="$(codesign --force --timestamp --options=runtime \
                                        --sign "$SIGNING_IDENTITY" "$file" 2>&1)"; then
                    echo "$output" | sed 's/^/    /' >&2
                    die "Failed to sign library: $file"
                fi
                signed_libs=$((signed_libs + 1))
                ;;
            *)
                # Everything else in Contents/MacOS: managed .NET assemblies
                # (PE, not Mach-O), and plain data such as the runtimeconfig
                # JSON, XML doc files, fonts and sample course files.
                #
                # All of it must be signed. Contents/MacOS is the bundle's
                # executables directory, so codesign treats every file in it as
                # nested code and refuses to seal the bundle while any one of
                # them lacks a signature -- it fails on a .xml doc file just as
                # readily as on a .dll. Non-Mach-O files are signed as
                # "generic" code. Data files belong in Contents/Resources by
                # Apple convention, but .NET requires them beside the apphost.
                if ! output="$(codesign --force --timestamp --options=runtime \
                                        --sign "$SIGNING_IDENTITY" "$file" 2>&1)"; then
                    echo "$output" | sed 's/^/    /' >&2
                    die "Failed to sign file: $file"
                fi
                signed_managed=$((signed_managed + 1))
                ;;
        esac
    done < <(find "$APP_BUNDLE/Contents/MacOS" -type f -print0)

    info "Signed $signed_execs executables, $signed_libs native libraries, $signed_managed other files"

    # Seal the bundle itself last.
    codesign --force --timestamp --options=runtime \
             --entitlements "$ENTITLEMENTS" \
             --sign "$SIGNING_IDENTITY" "$APP_BUNDLE" \
        || die "Failed to sign the app bundle."

    info "Sealed $APP_NAME.app"

    codesign --verify --deep --strict --verbose=2 "$APP_BUNDLE" 2>&1 | sed 's/^/    /' \
        || die "Signature verification failed."
}

# ---------------------------------------------------------------------------
# Step 5: notarize the app
# ---------------------------------------------------------------------------

# notarize: submit a file to Apple's notary service and wait for the result.
# $1 is the path to submit (a .zip or .dmg), $2 a human-readable description.
notarize() {
    local path="$1" description="$2"

    info "Submitting $description to Apple; this usually takes a few minutes..."

    # notarytool exits non-zero if the submission is rejected, but the log is
    # only reachable through the submission id, so capture the output.
    local output submission_id
    if ! output="$(xcrun notarytool submit "$path" "${NOTARY_ARGS[@]}" --wait 2>&1)"; then
        echo "$output" | sed 's/^/    /' >&2
        submission_id="$(echo "$output" | sed -n 's/.*id: \([0-9a-f-]\{36\}\).*/\1/p' | head -1)"
        if [[ -n "$submission_id" ]]; then
            printf '\n%sNotarization failed. Detailed log:%s\n' "$C_ERR" "$C_OFF" >&2
            xcrun notarytool log "$submission_id" "${NOTARY_ARGS[@]}" >&2 || true
        fi
        die "Notarization of $description was rejected."
    fi

    echo "$output" | sed 's/^/    /'

    if ! echo "$output" | grep -q "status: Accepted"; then
        submission_id="$(echo "$output" | sed -n 's/.*id: \([0-9a-f-]\{36\}\).*/\1/p' | head -1)"
        if [[ -n "$submission_id" ]]; then
            xcrun notarytool log "$submission_id" "${NOTARY_ARGS[@]}" >&2 || true
        fi
        die "Notarization of $description did not reach status Accepted."
    fi
}

# notarize_app: zip the bundle, notarize it, and staple the resulting ticket
# into the .app itself.
#
# Stapling the .app (rather than only the .dmg) means the app validates even
# after being copied out of an archive on a machine that is offline.
notarize_app() {
    step "Notarizing $APP_NAME.app"

    if [[ "$SKIP_NOTARIZE" == "1" ]]; then
        warn "Skipped. The app will run on this machine but Gatekeeper will block it elsewhere."
        return
    fi

    local notarize_zip="$BUILD_DIR/$APP_NAME-notarize.zip"
    rm -f "$notarize_zip"

    # ditto, not zip: it preserves symlinks, extended attributes and the
    # bundle's signature. Plain zip corrupts the signature.
    ditto -c -k --sequesterRsrc --keepParent "$APP_BUNDLE" "$notarize_zip"

    notarize "$notarize_zip" "$APP_NAME.app"
    rm -f "$notarize_zip"

    xcrun stapler staple "$APP_BUNDLE" || die "Failed to staple the notarization ticket to the app."
    xcrun stapler validate "$APP_BUNDLE" || die "Stapled ticket did not validate."

    # The definitive check: this is what Gatekeeper does on a user's machine.
    info "Gatekeeper assessment:"
    spctl --assess --type execute --verbose=4 "$APP_BUNDLE" 2>&1 | sed 's/^/    /' \
        || die "Gatekeeper rejected the app."
}

# ---------------------------------------------------------------------------
# Step 6: package
# ---------------------------------------------------------------------------

# build_zip: produce the distribution .zip from the (stapled) app bundle.
build_zip() {
    step "Building .zip"

    if [[ "$SKIP_ZIP" == "1" ]]; then
        info "Skipped."
        return
    fi

    local zip_path="$OUTPUT_DIR/$DIST_BASENAME.zip"
    rm -f "$zip_path"

    ditto -c -k --sequesterRsrc --keepParent "$APP_BUNDLE" "$zip_path"

    info "Wrote $zip_path ($(du -h "$zip_path" | cut -f1 | tr -d ' '))"
}

# build_background: rasterize dmg-background.svg into the multi-resolution TIFF
# that Finder uses as the disk image window's background picture.
#
# Finder draws a background picture unscaled at its natural point size, and
# takes that size from the image's representations. A plain 640x400 PNG is
# therefore soft on a Retina display, and a plain 1280x800 PNG would be drawn
# at 1280x800 points -- showing only its top-left quarter. A two-page TIFF with
# the second page tagged as the 2x representation is the way to get both right.
#
# Prints the path of the TIFF.
build_background() {
    local bgdir="$BUILD_DIR/dmg-background"
    local w2=$(( DMG_WINDOW_WIDTH * 2 ))
    local h2=$(( DMG_WINDOW_HEIGHT * 2 ))

    rm -rf "$bgdir"
    mkdir -p "$bgdir"

    # --resampleWidth takes a single argument and preserves aspect ratio.
    # Do not reach for -z (which takes height *before* width) or -Z (which
    # takes a single maximum dimension).
    sips -s format png --resampleWidth "$DMG_WINDOW_WIDTH" \
         "$DMG_BACKGROUND_SVG" --out "$bgdir/background.png" >/dev/null 2>&1 \
        || die "sips failed to rasterize $DMG_BACKGROUND_SVG"
    sips -s format png --resampleWidth "$w2" \
         "$DMG_BACKGROUND_SVG" --out "$bgdir/background@2x.png" >/dev/null 2>&1 \
        || die "sips failed to rasterize $DMG_BACKGROUND_SVG at 2x"

    # sips exits 0 on some partial failures, so trust the pixels, not the exit
    # code. This is also what catches an SVG whose aspect ratio no longer
    # matches DMG_WINDOW_WIDTH/HEIGHT.
    check_png_size "$bgdir/background.png"    "$DMG_WINDOW_WIDTH" "$DMG_WINDOW_HEIGHT"
    check_png_size "$bgdir/background@2x.png" "$w2"               "$h2"

    # -cathidpicheck tags the second image as the 2x representation, and
    # verifies that it really is exactly double the first.
    rm -f "$bgdir/background.tiff"
    tiffutil -cathidpicheck "$bgdir/background.png" "$bgdir/background@2x.png" \
             -out "$bgdir/background.tiff" >/dev/null 2>&1 \
        || die "tiffutil failed to combine the background images into a multi-resolution TIFF."

    printf '%s\n' "$bgdir/background.tiff"
}

# check_png_size: die unless the PNG at $1 is exactly $2 x $3 pixels.
check_png_size() {
    local file="$1" want_w="$2" want_h="$3" got_w got_h
    got_w="$(sips -g pixelWidth "$file" 2>/dev/null | awk '/pixelWidth/ {print $2}')"
    got_h="$(sips -g pixelHeight "$file" 2>/dev/null | awk '/pixelHeight/ {print $2}')"

    if [[ "$got_w" != "$want_w" || "$got_h" != "$want_h" ]]; then
        die "$(basename "$file") came out ${got_w}x${got_h}, expected ${want_w}x${want_h}.

The background art's aspect ratio must match DMG_WINDOW_WIDTH x
DMG_WINDOW_HEIGHT (${DMG_WINDOW_WIDTH}x${DMG_WINDOW_HEIGHT}). Check the viewBox in
$DMG_BACKGROUND_SVG."
    fi
}

# attach_dmg: attach the disk image at $1, recording the device node and mount
# point in DMG_ATTACH_DEV / DMG_ATTACH_MOUNT for the caller and for the EXIT
# trap.
#
# Note there is no -nobrowse here, tempting though it is for a build script:
# the styling works by asking Finder about the volume, so Finder has to be
# willing to show it.
attach_dmg() {
    local image="$1" plist parsed

    # Record the image path before anything else can fail, so the EXIT trap can
    # still find the device if the parsing below goes wrong.
    DMG_ATTACH_IMAGE="$image"

    if ! plist="$(hdiutil attach "$image" -noverify -noautoopen -plist 2>&1)"; then
        printf '%s\n' "$plist" | sed 's/^/    /' >&2
        die "hdiutil failed to attach $image"
    fi

    if ! parsed="$(printf '%s' "$plist" | /usr/bin/python3 -c '
import plistlib, sys
ents = plistlib.loads(sys.stdin.buffer.read())["system-entities"]
devs = sorted((e["dev-entry"] for e in ents if e.get("dev-entry")), key=len)
mounts = [e["mount-point"] for e in ents if e.get("mount-point")]
print(devs[0] if devs else "")
print(mounts[0] if mounts else "")
')"; then
        die "Could not parse the output of hdiutil attach."
    fi

    DMG_ATTACH_DEV="$(printf '%s\n' "$parsed" | sed -n 1p)"
    DMG_ATTACH_MOUNT="$(printf '%s\n' "$parsed" | sed -n 2p)"

    [[ -n "$DMG_ATTACH_DEV" ]] || die "hdiutil attach reported no device node."
    [[ -d "$DMG_ATTACH_MOUNT" ]] || die "hdiutil attach reported no usable mount point."
}

# style_dmg_window: drive Finder to lay out the mounted volume's window.
#
# $1 is the mount point. The volume's Finder name is derived from the mount
# point rather than from DMG_VOLUME_NAME, because if macOS had to disambiguate
# the name at mount time the two differ -- and addressing the wrong disk here
# would style someone else's window and silently produce an unstyled image.
style_dmg_window() {
    local mount="$1"
    local disk_name; disk_name="$(basename "$mount")"
    local wx="$DMG_WINDOW_X" wy="$DMG_WINDOW_Y"
    local wx2=$(( DMG_WINDOW_X + DMG_WINDOW_WIDTH ))
    local wy2=$(( DMG_WINDOW_Y + DMG_WINDOW_HEIGHT ))
    local out

    if [[ "$disk_name" != "$DMG_VOLUME_NAME" ]]; then
        die "The disk image mounted as '$disk_name' rather than '$DMG_VOLUME_NAME', which
means something else of that name was already mounted. Styling would apply to
the wrong volume, so the build has been stopped."
    fi

    # The AppleScript is written out to a file rather than piped in inline.
    # /bin/bash on macOS is still 3.2, which mis-parses a here-document nested
    # inside $( ) as soon as the body contains an apostrophe -- and the body
    # below has one. Keeping it on disk also makes it possible to run by hand
    # when debugging a layout problem.
    local script_file="$BUILD_DIR/dmg-style.applescript"
    cat > "$script_file" <<'APPLESCRIPT'
on run argv
	set diskName to item 1 of argv
	set appName to item 2 of argv
	set appBaseName to item 3 of argv
	set wx to (item 4 of argv) as integer
	set wy to (item 5 of argv) as integer
	set wx2 to (item 6 of argv) as integer
	set wy2 to (item 7 of argv) as integer
	set iconSize to (item 8 of argv) as integer
	set textSize to (item 9 of argv) as integer
	set appX to (item 10 of argv) as integer
	set appY to (item 11 of argv) as integer
	set appsX to (item 12 of argv) as integer
	set appsY to (item 13 of argv) as integer

	tell application "Finder"
		tell disk diskName
			open

			tell container window
				set current view to icon view
				set toolbar visible to false
				set statusbar visible to false
			end tell

			-- Setting bounds can be ignored while the window is still
			-- animating open, so set and read back until it sticks rather
			-- than sprinkling arbitrary delays around.
			repeat 40 times
				tell container window
					set the bounds to {wx, wy, wx2, wy2}
				end tell
				if (the bounds of container window) is {wx, wy, wx2, wy2} then exit repeat
				delay 0.25
			end repeat

			set opts to the icon view options of container window
			tell opts
				set icon size to iconSize
				set text size to textSize
				set arrangement to not arranged
			end tell
			set background picture of opts to file ".background:background.tiff"

			-- Finder reports the name without the extension if the item's
			-- extension-hidden bit is set, so accept either spelling.
			set appItem to first item of container window whose name is appName or name is appBaseName
			set position of appItem to {appX, appY}
			set position of item "Applications" of container window to {appsX, appsY}

			set finalBounds to the bounds of container window

			-- Closing the window is what makes Finder flush .DS_Store.
			close
		end tell
	end tell

	return (item 1 of finalBounds as text) & "," & (item 2 of finalBounds as text) & "," & ¬
		(item 3 of finalBounds as text) & "," & (item 4 of finalBounds as text)
end run
APPLESCRIPT

    # All geometry goes through argv, so there is no template substitution to
    # get wrong. Every value arrives as a string and has to be coerced.
    if ! out="$(osascript "$script_file" "$disk_name" "$APP_NAME.app" "$APP_NAME" \
                    "$wx" "$wy" "$wx2" "$wy2" \
                    "$DMG_ICON_SIZE" "$DMG_TEXT_SIZE" \
                    "$DMG_APP_ICON_X" "$DMG_APP_ICON_Y" \
                    "$DMG_APPS_ICON_X" "$DMG_APPS_ICON_Y" 2>&1)"; then
        printf '%s\n' "$out" | sed 's/^/    /' >&2
        explain_osascript_error "$out"
    fi

    # Finder clamps the window if it does not fit the screen, which would bake
    # a different size into .DS_Store and leave the artwork misaligned.
    local want="$wx,$wy,$wx2,$wy2"
    if [[ "$out" != "$want" ]]; then
        warn "Finder settled on window bounds $out rather than $want."
        warn "If the artwork looks misaligned, the window probably did not fit the screen."
    fi
}

# explain_osascript_error: turn a raw Apple event error into something
# actionable, then exit. $1 is the captured osascript output.
explain_osascript_error() {
    local out="$1"

    case "$out" in
        *-1743*)
            die "Not permitted to control Finder, so the disk image cannot be styled.

Grant it in System Settings > Privacy & Security > Automation: find this
terminal application and tick Finder. Then re-run." ;;
        *-600*|*-609*)
            die "No Finder session available to style the disk image.

This usually means the build is running without a logged-in desktop session
(over SSH, or from a CI agent). Use --skip-style there." ;;
        *-1728*)
            die "Finder could not find an item to position inside the disk image.

Expected '$APP_NAME.app' at the top level of the volume. Check APP_NAME in
config.sh." ;;
        *)
            die "Failed to style the disk image window through Finder." ;;
    esac
}

# check_image_layout: attach the disk image at $1 read-only and confirm Finder
# recorded the window layout in it. Prints a one-line summary.
#
# This has to run after the image has been detached at least once. Finder does
# not write .DS_Store when the window is closed -- it writes it as part of
# ejecting the volume, so before the first detach the file is an empty 6 KB
# skeleton containing no view settings at all.
check_image_layout() {
    local image="$1" mount_root="$BUILD_DIR/layout-check" plist mount summary rc=0

    rm -rf "$mount_root"
    mkdir -p "$mount_root"

    # -nobrowse is correct here: this pass does not involve Finder, and
    # mounting outside /Volumes keeps it clear of the volume-name collision
    # that the preflight check guards against.
    DMG_ATTACH_IMAGE="$image"
    if ! plist="$(hdiutil attach "$image" -readonly -nobrowse -noverify -noautoopen \
                      -mountrandom "$mount_root" -plist 2>&1)"; then
        printf '%s\n' "$plist" | sed 's/^/    /' >&2
        die "Could not attach $image to check its window layout."
    fi

    DMG_ATTACH_DEV="$(printf '%s' "$plist" | /usr/bin/python3 -c '
import plistlib, sys
e = plistlib.loads(sys.stdin.buffer.read())["system-entities"]
d = sorted((x["dev-entry"] for x in e if x.get("dev-entry")), key=len)
print(d[0] if d else "")
')"
    mount="$(printf '%s' "$plist" | /usr/bin/python3 -c '
import plistlib, sys
e = plistlib.loads(sys.stdin.buffer.read())["system-entities"]
m = [x["mount-point"] for x in e if x.get("mount-point")]
print(m[0] if m else "")
')"
    DMG_ATTACH_MOUNT="$mount"

    summary="$(/usr/bin/python3 "$LAYOUT_READER" "$mount/.DS_Store" 2>&1)" || rc=$?

    detach_dmg "$DMG_ATTACH_DEV" || warn "Could not detach the layout check mount."
    DMG_ATTACH_IMAGE=""; DMG_ATTACH_DEV=""; DMG_ATTACH_MOUNT=""
    rm -rf "$mount_root"

    if [[ "$rc" != "0" ]]; then
        die "Finder did not record the window layout: $summary

The disk image would open unstyled, so the build has been stopped rather than
shipping something that looks unfinished."
    fi

    printf '%s\n' "$summary"
}

# build_styled_dmg: create the disk image at $1 with a laid-out Finder window.
build_styled_dmg() {
    local dmg_path="$1"
    local rw_dmg="$BUILD_DIR/$DIST_BASENAME-rw.dmg"
    local background size_kb size_mb mount

    background="$(build_background)"
    info "Rasterized the background at ${DMG_WINDOW_WIDTH}x${DMG_WINDOW_HEIGHT} and 2x"

    # Size the scratch image from the payload plus headroom. HFS+ needs room
    # for its catalog and journal beyond the files themselves, and Finder needs
    # somewhere to put .DS_Store and the background picture. Being generous is
    # nearly free: the slack is empty and compresses away in the final image.
    size_kb="$(du -sk "$APP_BUNDLE" | awk '{print $1}')"
    size_mb=$(( size_kb / 1024 * 110 / 100 + DMG_FREE_SPACE_MB ))

    rm -f "$rw_dmg"
    hdiutil create \
        -size "${size_mb}m" \
        -fs HFS+ \
        -volname "$DMG_VOLUME_NAME" \
        -nospotlight \
        -ov -quiet \
        "$rw_dmg" \
        || die "hdiutil failed to create the scratch disk image."

    attach_dmg "$rw_dmg"
    mount="$DMG_ATTACH_MOUNT"

    # ditto rather than cp: the managed .NET assemblies in Contents/MacOS carry
    # their code signatures in extended attributes, and a copy that drops those
    # would unsign them. The failure would not surface until notarization, as a
    # complaint about an apparently unrelated file.
    ditto "$APP_BUNDLE" "$mount/$APP_NAME.app" \
        || die "Failed to copy the app into the disk image."

    ln -s /Applications "$mount/Applications"
    mkdir "$mount/.background"
    cp "$background" "$mount/.background/background.tiff"

    # Catch a signature-destroying copy in seconds rather than after a round
    # trip to Apple.
    if [[ "$SKIP_SIGN" == "0" ]]; then
        codesign --verify --strict "$mount/$APP_NAME.app" \
            || die "The copy of $APP_NAME.app inside the disk image failed signature verification.
The copy step must preserve extended attributes."
    fi

    style_dmg_window "$mount"

    # Finder and Spotlight leave working files behind that should not ship.
    # Do not touch .DS_Store: Finder is about to write the layout into it.
    rm -rf "$mount/.fseventsd" "$mount/.Trashes" "$mount/.TemporaryItems" 2>/dev/null || true

    # Give Finder a moment to settle before ejecting. It writes the layout as
    # part of the eject, so this is the last chance for it to have caught up
    # with the changes made above.
    sleep 1

    detach_dmg "$DMG_ATTACH_DEV" || die "Could not detach the scratch disk image."
    DMG_ATTACH_IMAGE=""; DMG_ATTACH_DEV=""; DMG_ATTACH_MOUNT=""

    # Only now is there anything to check: .DS_Store is written during the
    # eject above, not when the window was closed. Confirm the layout landed
    # before spending time compressing 140 MB.
    info "Layout: $(check_image_layout "$rw_dmg")"

    hdiutil convert "$rw_dmg" -format UDZO -o "$dmg_path" -ov -quiet \
        || die "hdiutil failed to compress the disk image."
    rm -f "$rw_dmg"
}

# build_plain_dmg: create an unstyled disk image at $1, for build machines with
# no desktop session where Finder cannot be scripted.
build_plain_dmg() {
    local dmg_path="$1"

    warn "Building without window styling."

    rm -rf "$DMG_STAGE_DIR"
    mkdir -p "$DMG_STAGE_DIR"
    cp -a "$APP_BUNDLE" "$DMG_STAGE_DIR/"
    ln -s /Applications "$DMG_STAGE_DIR/Applications"

    hdiutil create \
        -volname "$DMG_VOLUME_NAME" \
        -srcfolder "$DMG_STAGE_DIR" \
        -format UDZO \
        -fs HFS+ \
        -ov \
        -quiet \
        "$dmg_path" \
        || die "hdiutil failed to create the disk image."

    rm -rf "$DMG_STAGE_DIR"
}

# verify_dmg: attach the finished disk image read-only and confirm that what
# shipped is what was intended.
#
# This inspects the final compressed image rather than the scratch one, so it
# catches anything lost in conversion. The window settings are read straight
# out of .DS_Store: the records are self-describing, so the values Finder
# actually stored can be compared against what was asked for.
verify_dmg() {
    local dmg_path="$1"
    local mount_root="$BUILD_DIR/verify" mount plist summary

    rm -rf "$mount_root"
    mkdir -p "$mount_root"

    # -nobrowse is right here: this pass does not involve Finder, and mounting
    # outside /Volumes keeps it clear of the volume-name collision check.
    DMG_ATTACH_IMAGE="$dmg_path"
    if ! plist="$(hdiutil attach "$dmg_path" -readonly -nobrowse -noverify -noautoopen \
                      -mountrandom "$mount_root" -plist 2>&1)"; then
        printf '%s\n' "$plist" | sed 's/^/    /' >&2
        die "Could not attach the finished disk image to verify it."
    fi

    DMG_ATTACH_DEV="$(printf '%s' "$plist" | /usr/bin/python3 -c '
import plistlib, sys
ents = plistlib.loads(sys.stdin.buffer.read())["system-entities"]
devs = sorted((e["dev-entry"] for e in ents if e.get("dev-entry")), key=len)
print(devs[0] if devs else "")
')"
    mount="$(printf '%s' "$plist" | /usr/bin/python3 -c '
import plistlib, sys
ents = plistlib.loads(sys.stdin.buffer.read())["system-entities"]
m = [e["mount-point"] for e in ents if e.get("mount-point")]
print(m[0] if m else "")
')"
    DMG_ATTACH_MOUNT="$mount"
    [[ -d "$mount" ]] || die "The finished disk image did not mount for verification."

    # Contents
    [[ -d "$mount/$APP_NAME.app" ]] || die "$APP_NAME.app is missing from the disk image."
    [[ -L "$mount/Applications" ]] || die "The Applications symlink is missing from the disk image."

    if [[ "$DMG_STYLE" == "1" ]]; then
        [[ -f "$mount/.background/background.tiff" ]] \
            || die "The background picture is missing from the disk image."

        if ! summary="$(/usr/bin/python3 "$LAYOUT_READER" "$mount/.DS_Store")"; then
            die "The disk image window layout is missing or wrong. It would open unstyled."
        fi
        info "Shipped image: $summary"
    fi

    # The truest end-to-end check available: this is what Gatekeeper does when
    # the user drags the app out of the disk image.
    if [[ "$SKIP_SIGN" == "0" ]]; then
        codesign --verify --strict "$mount/$APP_NAME.app" \
            || die "The app inside the finished disk image failed signature verification."
    fi
    if [[ "$SKIP_NOTARIZE" == "0" ]]; then
        spctl --assess --type execute "$mount/$APP_NAME.app" 2>/dev/null \
            || die "Gatekeeper rejected the app inside the finished disk image."
        info "Gatekeeper accepts the app inside the disk image"
    fi

    detach_dmg "$DMG_ATTACH_DEV" || warn "Could not detach the verification mount."
    DMG_ATTACH_IMAGE=""; DMG_ATTACH_DEV=""; DMG_ATTACH_MOUNT=""
    rm -rf "$mount_root"
}

# build_dmg: produce a compressed disk image containing the app and a symlink
# to /Applications, with the window laid out for drag-to-install. The image is
# signed and notarized in its own right.
#
# Styling requires the image to be writable and mounted while Finder is told
# what to do with it, so this builds a blank read/write image, fills it, styles
# it, then converts to the compressed read-only format that actually ships.
build_dmg() {
    step "Building .dmg"

    if [[ "$SKIP_DMG" == "1" ]]; then
        info "Skipped."
        return
    fi

    local dmg_path="$OUTPUT_DIR/$DIST_BASENAME.dmg"
    rm -f "$dmg_path"

    # Check the source bundle before copying it, so that a problem here is not
    # later mistaken for the copy having damaged it. This matters most with
    # --dmg-only, which reuses whatever bundle a previous run happened to
    # leave behind -- possibly an unsigned one.
    if [[ "$SKIP_SIGN" == "0" ]]; then
        codesign --verify --strict "$APP_BUNDLE" 2>/dev/null \
            || die "$APP_NAME.app is not validly signed, so the disk image would ship unsigned.

If this was --dmg-only, the bundle left in build/ came from an earlier
--skip-sign run. Rebuild it with signing enabled:

    ./build-mac-app.sh --skip-publish --skip-notarize

or add --skip-sign here to build an unsigned disk image deliberately."
    fi

    if [[ "$DMG_STYLE" == "1" ]]; then
        build_styled_dmg "$dmg_path"
    else
        build_plain_dmg "$dmg_path"
    fi

    if [[ "$SKIP_SIGN" == "0" ]]; then
        # A disk image is signed as a flat file: no Hardened Runtime, no
        # entitlements. Those apply to the app inside it, which is already
        # signed and stapled.
        codesign --force --timestamp --sign "$SIGNING_IDENTITY" "$dmg_path" \
            || die "Failed to sign the disk image."
        info "Signed the disk image"
    fi

    if [[ "$SKIP_NOTARIZE" == "0" ]]; then
        notarize "$dmg_path" "$(basename "$dmg_path")"
        xcrun stapler staple "$dmg_path" || die "Failed to staple the notarization ticket to the disk image."
        xcrun stapler validate "$dmg_path" || die "Stapled disk image ticket did not validate."
    fi

    verify_dmg "$dmg_path"

    info "Wrote $dmg_path ($(du -h "$dmg_path" | cut -f1 | tr -d ' '))"
}

# ---------------------------------------------------------------------------
# Run
# ---------------------------------------------------------------------------

mkdir -p "$BUILD_DIR" "$OUTPUT_DIR"

if [[ "$DMG_ONLY" == "1" ]]; then
    # Layout iteration: reuse the app bundle from a previous run and go
    # straight to the disk image.
    [[ -d "$APP_BUNDLE" ]] \
        || die "--dmg-only needs an app bundle at $APP_BUNDLE, but there is none.
Run the build without --dmg-only first."
    warn "--dmg-only: reusing the existing $APP_NAME.app without rebuilding it."
else
    publish_app
    stage_payload
    stage_pdf_converter
    build_icon
    assemble_bundle
    sign_bundle
    notarize_app
    build_zip
fi

build_dmg

step "Done"
printf '%s' "$C_OK"
ls -1 "$OUTPUT_DIR" 2>/dev/null | sed "s|^|    $(basename "$OUTPUT_DIR")/|"
printf '%s\n' "$C_OFF"

if [[ "$SKIP_SIGN" == "1" ]]; then
    warn "This build is UNSIGNED and is not fit for distribution."
elif [[ "$SKIP_NOTARIZE" == "1" ]]; then
    warn "This build is signed but NOT NOTARIZED and is not fit for distribution."
fi

if [[ "$SKIP_DMG" == "0" && "$DMG_STYLE" != "1" ]]; then
    warn "The .dmg window is UNSTYLED: no background picture and no icon layout."
fi
