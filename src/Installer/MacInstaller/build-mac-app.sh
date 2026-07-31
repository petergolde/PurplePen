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
SOURCE_ICON="$SRC_DIR/AvPurplePen/Assets/PurplePenIcon.png"

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
# Command line
# ---------------------------------------------------------------------------

SKIP_PUBLISH="${SKIP_PUBLISH:-0}"
SKIP_SIGN="${SKIP_SIGN:-0}"
SKIP_NOTARIZE="${SKIP_NOTARIZE:-0}"
SKIP_DMG="${SKIP_DMG:-0}"
SKIP_ZIP="${SKIP_ZIP:-0}"

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
[[ -f "$SOURCE_ICON" ]] || die "Cannot find source icon at $SOURCE_ICON"

if [[ ! -f "$EXCLUDE_FILE" ]]; then
    warn "No publish-exclude.txt found; nothing will be excluded from the bundle."
    EXCLUDE_FILE=""
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

# build_icon: produce an .icns from the source PNG.
#
# The source in the repository is only 64x64, so the larger sizes here are
# upscaled and will look soft. Replace $SOURCE_ICON with a 1024x1024 PNG to fix
# that -- no change to this script is needed.
build_icon() {
    step "Building application icon"

    local iconset="$BUILD_DIR/$APP_NAME.iconset"
    local icns="$BUILD_DIR/$APP_NAME.icns"
    local source_width

    rm -rf "$iconset"
    mkdir -p "$iconset"

    source_width="$(sips -g pixelWidth "$SOURCE_ICON" | awk '/pixelWidth/ {print $2}')"
    if [[ -n "$source_width" && "$source_width" -lt 1024 ]]; then
        warn "Source icon is only ${source_width}x${source_width}; larger icon sizes will be upscaled and look blurry."
        warn "Replace $SOURCE_ICON with a 1024x1024 PNG for a crisp icon."
    fi

    # macOS expects each logical size at both 1x and 2x pixel density.
    local size
    for size in 16 32 128 256 512; do
        sips -z "$size" "$size" "$SOURCE_ICON" \
             --out "$iconset/icon_${size}x${size}.png" >/dev/null
        sips -z "$((size * 2))" "$((size * 2))" "$SOURCE_ICON" \
             --out "$iconset/icon_${size}x${size}@2x.png" >/dev/null
    done

    iconutil --convert icns "$iconset" --output "$icns"
    rm -rf "$iconset"

    ICNS_FILE="$icns"
    info "Built $(basename "$icns")"
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

# build_dmg: produce a compressed disk image containing the app and a symlink
# to /Applications, so the user can drag one onto the other to install. The
# image is signed and notarized in its own right.
build_dmg() {
    step "Building .dmg"

    if [[ "$SKIP_DMG" == "1" ]]; then
        info "Skipped."
        return
    fi

    local dmg_path="$OUTPUT_DIR/$DIST_BASENAME.dmg"
    rm -f "$dmg_path"

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

    info "Wrote $dmg_path ($(du -h "$dmg_path" | cut -f1 | tr -d ' '))"
}

# ---------------------------------------------------------------------------
# Run
# ---------------------------------------------------------------------------

mkdir -p "$BUILD_DIR" "$OUTPUT_DIR"

publish_app
stage_payload
stage_pdf_converter
build_icon
assemble_bundle
sign_bundle
notarize_app
build_zip
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
