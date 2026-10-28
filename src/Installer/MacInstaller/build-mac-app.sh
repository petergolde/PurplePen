#!/bin/bash
#
# build-mac-app.sh
#
# Builds a signed and notarized macOS distribution of Purple Pen, producing
# both a .dmg and a .zip in the output/ directory.
#
# The pipeline is:
#
#   1. dotnet publish AvPurplePen (Release, net10.0-macos, osx-arm64). The
#      .NET macOS SDK builds a complete PurplePen.app itself: the native
#      launcher in Contents/MacOS, the runtime, assemblies and native libraries
#      in Contents/MonoBundle, the icon in Contents/Resources, and Info.plist
#      merged from AvPurplePen/Info.plist and the project's properties
#   2. Copy that bundle into build/PurplePen.app, leaving out anything in
#      Contents/MonoBundle matched by publish-exclude.txt
#   3. Sign every Mach-O file in the bundle inside-out, then seal the bundle
#   4. Notarize the app and staple the ticket to it
#   5. Build the .dmg and .zip from the stapled app, then notarize and staple
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
ENTITLEMENTS="$SCRIPT_DIR/PurplePen.entitlements"
DMG_BACKGROUND_SVG="$SCRIPT_DIR/dmg-background.svg"
LAYOUT_READER="$SCRIPT_DIR/read-dmg-layout.py"

BUILD_DIR="$SCRIPT_DIR/build"
APP_BUNDLE="$BUILD_DIR/$APP_NAME.app"
DMG_STAGE_DIR="$BUILD_DIR/dmg"
OUTPUT_DIR="$SCRIPT_DIR/output"
BUILD_INFO_FILE="$OUTPUT_DIR/build-info.sh"

# Where the .NET macOS SDK leaves the bundle it builds. This is the ordinary
# build output directory; with CreatePackage=false, publish adds nothing beside it.
SDK_APP_BUNDLE="$SRC_DIR/AvPurplePen/bin/$CONFIGURATION/$TARGET_FRAMEWORK/$RUNTIME_IDENTIFIER/$APP_NAME.app"
SDK_INTERMEDIATE_DIR="$SRC_DIR/AvPurplePen/obj/$CONFIGURATION/$TARGET_FRAMEWORK/$RUNTIME_IDENTIFIER"

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
  --skip-publish    Reuse the .app the last dotnet publish built instead of
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
[[ -f "$ENTITLEMENTS" ]] || die "Cannot find entitlements at $ENTITLEMENTS"

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

# check_xcode: confirm the .NET macOS workload is installed and that the active
# Xcode is the one that workload was built against.
#
# Building for net10.0-macos needs Xcode: the SDK links the native launcher with
# Xcode's clang and against its macOS SDK. Each release of the workload is built
# against one Xcode, and the SDK refuses to build (error E0191) unless the
# active Xcode's major.minor version matches it exactly -- newer is rejected
# just like older. The SDK would report that itself, but only after restore and
# a good part of the build; checking here fails in the first few seconds, and
# can say how to fix it. See "Xcode and the .NET macOS workload" in README.md.
#
# Not run for --dmg-only or --skip-publish, which do not build anything.
check_xcode() {
    if ! dotnet workload list 2>/dev/null | grep -qE '^macos[[:space:]]'; then
        die "The .NET macOS workload is not installed. Install it with:

    sudo dotnet workload install macos

then re-run. See \"Xcode and the .NET macOS workload\" in README.md."
    fi

    # The workload records the Xcode it needs in a property of its own; asking
    # MSBuild for it means this check can never disagree with the SDK's.
    local props workload required current
    props="$(dotnet msbuild "$PROJECT_FILE" -nologo \
                 -p:Configuration="$CONFIGURATION" -p:RuntimeIdentifier="$RUNTIME_IDENTIFIER" \
                 -getProperty:_ShortPackageVersion -getProperty:_RecommendedXcodeVersion 2>/dev/null)" \
        || die "Could not evaluate $PROJECT_FILE to find which Xcode the .NET macOS workload needs."
    workload="$(printf '%s' "$props" | /usr/bin/python3 -c 'import json,sys; print(json.load(sys.stdin)["Properties"]["_ShortPackageVersion"])' 2>/dev/null || true)"
    required="$(printf '%s' "$props" | /usr/bin/python3 -c 'import json,sys; print(json.load(sys.stdin)["Properties"]["_RecommendedXcodeVersion"])' 2>/dev/null || true)"
    [[ -n "$required" ]] \
        || die "Could not find which Xcode the .NET macOS workload needs. Is AvPurplePen targeting $TARGET_FRAMEWORK?"

    # xcodebuild honours DEVELOPER_DIR, exactly as the SDK does.
    local xcode_dir="${DEVELOPER_DIR:-$(xcode-select -p 2>/dev/null)}"
    current="$(xcodebuild -version 2>/dev/null | awk '/^Xcode/ {print $2}')"
    [[ -n "$current" ]] || die "No Xcode found. Install Xcode $required (the command line tools alone are not enough)."

    local req_mm cur_mm
    req_mm="$(echo "$required" | cut -d. -f1-2)"
    cur_mm="$(echo "$current"  | cut -d. -f1-2)"
    [[ "$req_mm" == *.* ]] || req_mm="$req_mm.0"
    [[ "$cur_mm" == *.* ]] || cur_mm="$cur_mm.0"

    if [[ "$req_mm" != "$cur_mm" ]]; then
        die "The .NET macOS workload ($workload) needs Xcode $required, but the active Xcode is $current
($xcode_dir). The major.minor versions must match exactly.

Either make a matching Xcode the active one:

    sudo xcode-select -s /Applications/Xcode-$required.app

or point just this build at it, leaving the system default alone:

    DEVELOPER_DIR=/Applications/Xcode-$required.app/Contents/Developer ./build-mac-app.sh

If the workload was updated, Xcode $required may need downloading first, from
https://developer.apple.com/download/all/ . See \"Xcode and the .NET macOS
workload\" in README.md."
    fi

    info ".NET macOS workload $workload, Xcode $current ($xcode_dir)"
}

if [[ "$DMG_ONLY" == "0" && "$SKIP_PUBLISH" == "0" ]]; then
    check_xcode
fi

# ---------------------------------------------------------------------------
# Determine the version number
# ---------------------------------------------------------------------------

# read_version: extract the current version from PurplePenCore/VersionNumber.cs
# and derive the version strings the Info.plist and the file names need.
#
# VersionNumber.cs holds a four-part version such as "4.0.0.110". Apple expects
# CFBundleShortVersionString to be at most three integer components, so the
# fourth (the Purple Pen build/prerelease number) is dropped from the
# user-visible version but retained in CFBundleVersion.
#
# That fourth component encodes the release stage rather than a build number:
# 100s alpha, 200s beta, 300s release candidate, 500 stable, with the tens digit
# the sequence number within the stage. It has to reach the distribution file
# names, because dropping it would give a beta and the eventual release of the
# same version the same name -- and they meet in the publishing tree, where the
# release would overwrite the file the beta's manifest entry records a SHA-256
# for, so that every beta user's update then fails verification.
#
# PurplePenCore/VersionNumber.cs is the authority on this encoding.
# LinuxInstaller/build-linux-packages.sh and Installer/GetVersion.cs decode it
# the same way, into the forms their own naming conventions call for.
read_version() {
    [[ -f "$VERSION_FILE" ]] || die "Cannot find $VERSION_FILE"

    FULL_VERSION="$(sed -n 's/.*Current[[:space:]]*=[[:space:]]*"\([0-9.]*\)".*/\1/p' "$VERSION_FILE" | head -1)"
    [[ -n "$FULL_VERSION" ]] || die "Could not parse the version number out of $VERSION_FILE"

    SHORT_VERSION="$(echo "$FULL_VERSION" | cut -d. -f1-3)"
    BUILD_VERSION="$FULL_VERSION"

    local stage seq stage_name
    stage="$(echo "$FULL_VERSION" | cut -d. -f4)"

    # A missing fourth component is a plain release, with nothing to add.
    if [[ -z "$stage" ]]; then
        STAGE_SUFFIX=""
        VERSION_LABEL="release"
        return
    fi

    seq=$(( (stage % 100) / 10 ))
    # A stage of exactly 200 means "beta" with no sequence number, so leave the
    # digit off rather than writing "beta0".
    [[ "$seq" -eq 0 ]] && seq=""

    if   [[ "$stage" -ge 500 ]]; then stage_name=""
    elif [[ "$stage" -ge 300 ]]; then stage_name="rc"
    elif [[ "$stage" -ge 200 ]]; then stage_name="beta"
    elif [[ "$stage" -ge 100 ]]; then stage_name="alpha"
    else                              stage_name="dev"
    fi

    if [[ -z "$stage_name" ]]; then
        STAGE_SUFFIX=""
        VERSION_LABEL="stable release"
    else
        STAGE_SUFFIX="-$stage_name$seq"
        VERSION_LABEL="$stage_name${seq:+ $seq}"
    fi
}

read_version
info "Purple Pen version $FULL_VERSION ($VERSION_LABEL)"

# Base name used for the .dmg and .zip files.
DIST_BASENAME="$APP_NAME-$SHORT_VERSION$STAGE_SUFFIX-$RUNTIME_IDENTIFIER"

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
# preferring explicit environment credentials over the keychain profile, then
# check that Apple accepts them. Populates the NOTARY_ARGS array.
resolve_notary_credentials() {
    NOTARY_ARGS=()

    if [[ -n "$NOTARY_APPLE_ID" ]]; then
        [[ -n "$NOTARY_TEAM_ID" ]] || die "NOTARY_APPLE_ID is set but NOTARY_TEAM_ID is not."
        [[ -n "$NOTARY_PASSWORD" ]] || die "NOTARY_APPLE_ID is set but NOTARY_PASSWORD is not."
        NOTARY_ARGS=(--apple-id "$NOTARY_APPLE_ID" --team-id "$NOTARY_TEAM_ID" --password "$NOTARY_PASSWORD")
        check_notary_credentials "Apple ID $NOTARY_APPLE_ID"
        info "Notarizing as $NOTARY_APPLE_ID (team $NOTARY_TEAM_ID)"
        return
    fi

    NOTARY_ARGS=(--keychain-profile "$NOTARY_KEYCHAIN_PROFILE")
    check_notary_credentials "keychain profile '$NOTARY_KEYCHAIN_PROFILE'"
    info "Notarizing with keychain profile '$NOTARY_KEYCHAIN_PROFILE'"
}

# check_notary_credentials: make one cheap authenticated request to the notary
# service with NOTARY_ARGS, so that a credential or account problem fails the
# build now rather than after the publish, signing and upload. $1 describes the
# credentials, for the error message.
#
# notarytool's own error is always shown. It is the only thing that tells apart
# failures that need quite different fixes -- a missing profile, a revoked
# password, an Apple developer agreement that needs accepting -- and assuming
# the first of those once sent a lapsed agreement off to recreate a perfectly
# good profile.
check_notary_credentials() {
    local description="$1" output advice

    if output="$(xcrun notarytool history "${NOTARY_ARGS[@]}" 2>&1)"; then
        return
    fi

    case "$output" in
        *"No Keychain password item found"*)
            advice="The keychain profile does not exist. Create it once with:

    xcrun notarytool store-credentials \"$NOTARY_KEYCHAIN_PROFILE\" \\
        --apple-id \"your-apple-id@example.com\" \\
        --team-id \"YOURTEAMID\" \\
        --password \"abcd-efgh-ijkl-mnop\"

The password is an app-specific password created at https://appleid.apple.com,
not your Apple ID password. Your team ID is shown at
https://developer.apple.com/account under Membership details." ;;
        *"agreement"*)
            advice="The credentials are fine, but Apple will not notarize until the team's
Account Holder signs in at https://developer.apple.com/account and accepts the
updated Apple Developer Program License Agreement shown there (or renews the
membership, if it has expired). It can take a few minutes to take effect." ;;
        *"401"*|*"nauthorized"*|*"nvalid credentials"*)
            advice="Apple rejected the credentials. App-specific passwords are revoked when the
Apple ID password changes; generate a new one at https://appleid.apple.com and
store it again with xcrun notarytool store-credentials." ;;
        *)
            advice="See the error above. Check the network connection, and that the account
can notarize at https://developer.apple.com/account." ;;
    esac

    printf '%s\n' "$output" | sed 's/^/    /' >&2
    die "The notary service did not accept the $description.

$advice

To build a signed but un-notarized app, use --skip-notarize."
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

# publish_app: run dotnet publish for AvPurplePen, which for net10.0-macos
# builds the complete .app bundle in the project's build output directory.
#
# CreatePackage=false stops publish from going on to wrap the bundle in a .pkg
# installer, which it does for macOS by default. The app is always
# self-contained -- a net10.0-macos app bundles the runtime whether asked to or
# not -- so there is no --self-contained to pass.
publish_app() {
    step "Publishing AvPurplePen ($CONFIGURATION, $TARGET_FRAMEWORK, $RUNTIME_IDENTIFIER)"

    if [[ "$SKIP_PUBLISH" == "1" ]]; then
        [[ -d "$SDK_APP_BUNDLE" ]] || die "--skip-publish was given but $SDK_APP_BUNDLE does not exist."
        info "Skipped; reusing $SDK_APP_BUNDLE"
        return
    fi

    # Remove the stale bundle so that files dropped from the build cannot
    # linger in it. The SDK updates the bundle incrementally otherwise.
    #
    # The intermediate directory has to go with it. The SDK rewrites each
    # native library's install name as it copies it into the bundle, which
    # breaks the library's signature, and then re-signs it -- unless a stamp
    # file under obj/ says it already did. With the bundle deleted and the
    # stamps left behind, it skips the re-signing and leaves every dylib with a
    # broken signature, which the kernel kills the app for on first load.
    rm -rf "$SDK_APP_BUNDLE" "$SDK_INTERMEDIATE_DIR"

    dotnet publish "$PROJECT_FILE" \
        --configuration "$CONFIGURATION" \
        --framework "$TARGET_FRAMEWORK" \
        --runtime "$RUNTIME_IDENTIFIER" \
        -p:CreatePackage=false \
        -p:MacIconFamily="$ICON_FAMILY" \
        -p:PublishReadyToRun="$PUBLISH_READYTORUN" \
        --nologo

    [[ -d "$SDK_APP_BUNDLE" ]] \
        || die "Publish succeeded but $SDK_APP_BUNDLE does not exist. Check APP_NAME and TARGET_FRAMEWORK in config.sh."
    [[ -f "$SDK_APP_BUNDLE/Contents/MacOS/$APP_NAME" ]] \
        || die "The built bundle has no executable named '$APP_NAME'. Check APP_NAME in config.sh against AvPurplePen's <AssemblyName>."

    info "Built $SDK_APP_BUNDLE"
}

# ---------------------------------------------------------------------------
# Step 2: assemble the distributable bundle
# ---------------------------------------------------------------------------

# assemble_bundle: copy the SDK-built bundle into build/PurplePen.app, applying
# publish-exclude.txt to Contents/MonoBundle -- the directory holding the
# runtime, the assemblies, the native libraries and the data files, and so the
# only place exclusions make sense. The patterns are relative to it.
#
# The copy is what gets signed and shipped. Working on a copy rather than in the
# build output keeps the developer's bin/ directory an ordinary, runnable build,
# and means --skip-publish always starts from the untouched SDK output.
assemble_bundle() {
    step "Assembling $APP_NAME.app"

    rm -rf "$APP_BUNDLE"
    mkdir -p "$APP_BUNDLE"

    # Everything outside MonoBundle as it is, except the SDK's ad-hoc
    # signature: the bundle is about to change, which invalidates it, and the
    # signing step writes a real one. archived-expanded-entitlements.xcent is a
    # by-product of that ad-hoc signing, not something the app reads.
    rsync --archive \
          --exclude=/Contents/MonoBundle/ \
          --exclude=/Contents/_CodeSignature/ \
          --exclude=/Contents/Resources/archived-expanded-entitlements.xcent \
          "$SDK_APP_BUNDLE/" "$APP_BUNDLE/"

    local rsync_args=(--archive)
    if [[ -n "$EXCLUDE_FILE" ]]; then
        rsync_args+=(--exclude-from="$EXCLUDE_FILE")
    fi
    rsync "${rsync_args[@]}" "$SDK_APP_BUNDLE/Contents/MonoBundle/" "$APP_BUNDLE/Contents/MonoBundle/"

    [[ -f "$APP_BUNDLE/Contents/MonoBundle/$APP_NAME.dll" ]] \
        || die "The main assembly '$APP_NAME.dll' was excluded from the bundle. Check publish-exclude.txt."

    # PDF map templates are rasterized in-process by the PdfConverter library,
    # which needs the native pdfium library next to the assemblies.
    [[ -f "$APP_BUNDLE/Contents/MonoBundle/libpdfium.dylib" ]] \
        || warn "libpdfium.dylib is not in the bundle; PDF map templates will not work."

    [[ -f "$APP_BUNDLE/Contents/Resources/$APP_NAME.icns" ]] \
        || die "The bundle has no Contents/Resources/$APP_NAME.icns. The BuildMacAppIcon target in AvPurplePen.csproj should have made it."

    check_info_plist

    # The executable bit is lost if the tree ever passes through a Windows
    # filesystem.
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

    local file_count size
    file_count="$(find "$APP_BUNDLE" -type f | wc -l | tr -d ' ')"
    size="$(du -sh "$APP_BUNDLE" | cut -f1 | tr -d ' ')"
    info "Assembled $APP_BUNDLE ($file_count files, $size)"
}

# plist_value: print the value of top-level key $2 in the plist at $1, or
# nothing if it is absent.
plist_value() {
    plutil -extract "$2" raw -o - "$1" 2>/dev/null || true
}

# check_info_plist: confirm the Info.plist the SDK generated says what the rest
# of this build assumes.
#
# The plist is no longer written by this script: the SDK merges
# AvPurplePen/Info.plist with keys it derives from the project (executable,
# identifier, versions, minimum macOS). The versions are derived from
# VersionNumber.cs by AvPurplePen.csproj and here by read_version, so this is
# also the check that those two parsings agree -- if they did not, the file
# names and the update manifest would describe a different version from the
# app inside.
check_info_plist() {
    local plist="$APP_BUNDLE/Contents/Info.plist" key want got

    plutil -lint "$plist" >/dev/null || die "The bundle's Info.plist is not valid."

    for key in CFBundleExecutable:"$APP_NAME" \
               CFBundleShortVersionString:"$SHORT_VERSION" \
               CFBundleVersion:"$BUILD_VERSION"; do
        want="${key#*:}"
        key="${key%%:*}"
        got="$(plist_value "$plist" "$key")"
        [[ "$got" == "$want" ]] \
            || die "Info.plist has $key '$got', but this build expected '$want'. Check AvPurplePen.csproj."
    done

    info "Info.plist: $(plist_value "$plist" CFBundleIdentifier) $(plist_value "$plist" CFBundleShortVersionString) ($(plist_value "$plist" CFBundleVersion)), macOS $(plist_value "$plist" LSMinimumSystemVersion)+"
}

# ---------------------------------------------------------------------------
# Step 3: sign
# ---------------------------------------------------------------------------

# sign_bundle: code sign the app bundle from the inside out.
#
# Every Mach-O file inside the bundle must carry its own signature before the
# bundle itself is sealed: codesign insists for nested code, and notarization
# rejects any unsigned (or ad-hoc signed) Mach-O binary anywhere in the
# submission. Executables other than the main one would be signed with the
# Hardened Runtime and entitlements; libraries get the Hardened Runtime but no
# entitlements, since entitlements only take effect on a process's main
# executable.
#
# Nothing else needs signing individually. The managed assemblies, data files
# and fonts all live in Contents/MonoBundle, which is not one of the bundle's
# nested-code locations, so the bundle seal records them in
# _CodeSignature/CodeResources like any other resource.
sign_bundle() {
    step "Code signing"

    if [[ "$SKIP_SIGN" == "1" ]]; then
        # Apple silicon will not run an unsigned executable at all, and the
        # assembly step dropped the SDK's ad-hoc seal. An ad-hoc signature is
        # enough to run locally, though it means nothing to Gatekeeper.
        codesign --force --sign - "$APP_BUNDLE" \
            || die "Failed to ad-hoc sign the app bundle."
        warn "Skipped; ad-hoc signed only. The app will be blocked by Gatekeeper."
        warn "To run it locally: xattr -dr com.apple.quarantine \"$APP_BUNDLE\""
        return
    fi

    local signed_libs=0 signed_execs=0 file kind output

    # -print0 / read -d '' so that filenames containing spaces are handled.
    while IFS= read -r -d '' file; do
        # Skip the bundle's main executable. Handing codesign the path
        # Contents/MacOS/<CFBundleExecutable> does not sign that file -- codesign
        # resolves it to the enclosing bundle and tries to seal the whole thing,
        # which fails here because the nested libraries are not signed yet. The
        # main executable is signed by the bundle seal at the end of this
        # function, which is where its entitlements come from anyway.
        if [[ "$file" == "$APP_BUNDLE/Contents/MacOS/$APP_NAME" ]]; then
            continue
        fi

        kind="$(file -b "$file")"

        case "$kind" in
            *Mach-O*executable*)
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
                # Managed assemblies (PE, not Mach-O) and data: sealed as
                # resources by the bundle signature below.
                ;;
        esac
    done < <(find "$APP_BUNDLE/Contents" -type f -print0)

    info "Signed $signed_execs nested executables and $signed_libs native libraries"

    # Seal the bundle itself last. This signs the main executable, with the
    # entitlements, and records every other file in CodeResources.
    codesign --force --timestamp --options=runtime \
             --entitlements "$ENTITLEMENTS" \
             --sign "$SIGNING_IDENTITY" "$APP_BUNDLE" \
        || die "Failed to sign the app bundle."

    info "Sealed $APP_NAME.app"

    codesign --verify --deep --strict --verbose=2 "$APP_BUNDLE" 2>&1 | sed 's/^/    /' \
        || die "Signature verification failed."
}

# ---------------------------------------------------------------------------
# Step 4: notarize the app
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
# Step 5: package
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

# hdiutil_attach_plist: run "hdiutil attach" with the given arguments plus
# -plist, and print the plist it reports.
#
# stderr is kept out of the result. macOS 27 deprecated "hdiutil attach" in
# favour of "diskutil image attach" and writes a warning about it to stderr on
# every call; merged into stdout, that made the plist unparseable. hdiutil is
# still used rather than diskutil because diskutil image does not exist before
# macOS 27. On failure, hdiutil's stderr is shown and the exit status passed on.
hdiutil_attach_plist() {
    local err_file out rc=0
    err_file="$(mktemp -t purplepen-hdiutil)"
    out="$(hdiutil attach "$@" -plist 2>"$err_file")" || rc=$?
    if [[ "$rc" != "0" ]]; then
        sed 's/^/    /' "$err_file" >&2
    fi
    rm -f "$err_file"
    printf '%s' "$out"
    return "$rc"
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

    if ! plist="$(hdiutil_attach_plist "$image" -noverify -noautoopen)"; then
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
    if ! plist="$(hdiutil_attach_plist "$image" -readonly -nobrowse -noverify -noautoopen \
                      -mountrandom "$mount_root")"; then
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

    # ditto rather than cp: it reproduces the bundle exactly -- symlinks,
    # permissions and extended attributes included -- so the signature survives
    # the copy. The check just below catches it if it ever does not.
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
    if ! plist="$(hdiutil_attach_plist "$dmg_path" -readonly -nobrowse -noverify -noautoopen \
                      -mountrandom "$mount_root")"; then
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

# write_build_info: record what this run produced, for publish-mac-app.sh to read.
#
# This is the counterpart of the setversion.cmd that create-setup.bat leaves for
# publish-setup.bat on Windows. There, the two scripts share a shell and so share
# their variables; here the publisher runs the build as a separate process, and
# without this would have to re-derive the output names from the version number
# and could not tell whether the build was actually signed and notarized.
#
# It is written only on success, and deleted at the start of every run, so its
# presence means "there is a complete build here" and never describes a build
# that failed halfway.
#
# Values are quoted with printf %q, which produces text that is safe to source.
write_build_info() {
    local dmg_path="" zip_path=""
    [[ "$SKIP_DMG" == "0" ]] && dmg_path="$OUTPUT_DIR/$DIST_BASENAME.dmg"
    [[ "$SKIP_ZIP" == "0" && "$DMG_ONLY" == "0" ]] && zip_path="$OUTPUT_DIR/$DIST_BASENAME.zip"

    {
        echo "# build-info.sh -- what build-mac-app.sh last produced."
        echo "#"
        echo "# Generated; safe to delete. Read by publish-mac-app.sh."
        echo
        printf 'export FULL_VERSION=%q\n'        "$FULL_VERSION"
        printf 'export SHORT_VERSION=%q\n'       "$SHORT_VERSION"
        printf 'export STAGE_SUFFIX=%q\n'        "$STAGE_SUFFIX"
        printf 'export RUNTIME_IDENTIFIER=%q\n'  "$RUNTIME_IDENTIFIER"
        printf 'export APP_BUNDLE=%q\n'          "$APP_BUNDLE"
        printf 'export DIST_BASENAME=%q\n'       "$DIST_BASENAME"
        printf 'export DMG_PATH=%q\n'            "$dmg_path"
        printf 'export ZIP_PATH=%q\n'            "$zip_path"
        printf 'export BUILD_SIGNED=%q\n'        "$([[ "$SKIP_SIGN" == "0" ]] && echo 1 || echo 0)"
        printf 'export BUILD_NOTARIZED=%q\n'     "$([[ "$SKIP_NOTARIZE" == "0" ]] && echo 1 || echo 0)"
    } > "$BUILD_INFO_FILE"
}

# ---------------------------------------------------------------------------
# Run
# ---------------------------------------------------------------------------

mkdir -p "$BUILD_DIR" "$OUTPUT_DIR"

# Any build-info.sh still here describes an earlier run. Remove it now so that a
# build that fails cannot leave one behind for publish-mac-app.sh to believe.
rm -f "$BUILD_INFO_FILE"

if [[ "$DMG_ONLY" == "1" ]]; then
    # Layout iteration: reuse the app bundle from a previous run and go
    # straight to the disk image.
    [[ -d "$APP_BUNDLE" ]] \
        || die "--dmg-only needs an app bundle at $APP_BUNDLE, but there is none.
Run the build without --dmg-only first."
    warn "--dmg-only: reusing the existing $APP_NAME.app without rebuilding it."
else
    publish_app
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

# After the listing, so that the listing shows distribution files only.
write_build_info

if [[ "$SKIP_SIGN" == "1" ]]; then
    warn "This build is UNSIGNED and is not fit for distribution."
elif [[ "$SKIP_NOTARIZE" == "1" ]]; then
    warn "This build is signed but NOT NOTARIZED and is not fit for distribution."
fi

if [[ "$SKIP_DMG" == "0" && "$DMG_STYLE" != "1" ]]; then
    warn "The .dmg window is UNSTYLED: no background picture and no icon layout."
fi
