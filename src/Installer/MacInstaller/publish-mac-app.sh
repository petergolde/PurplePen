#!/bin/bash
#
# publish-mac-app.sh
#
# Builds the macOS distribution and files it into the publishing tree: the
# directory whose contents are uploaded to the download site, and whose layout
# therefore has to match the URLs recorded in the update manifest.
#
# Three steps: build-mac-app.sh builds the .app, .dmg and .zip and records what
# it produced in output/build-info.sh, the disk image is copied into the tree,
# and UpdateManifest records it in manifest.json so that running copies of
# Purple Pen are offered the update.
#
# The Windows counterpart is Innosetup/publish-setup.bat, which publishes into
# the same tree; the linux packages publish into it too, and their half of it is
# configured in Installer/LinuxInstaller/config.sh.
#
# Settings live in config.sh, like everything else here; every one of them can
# be overridden from the environment. Run with --help for options.

set -euo pipefail

# ---------------------------------------------------------------------------
# Locate ourselves and load configuration
# ---------------------------------------------------------------------------

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

# shellcheck source=config.sh
source "$SCRIPT_DIR/config.sh"

BUILD_SCRIPT="$SCRIPT_DIR/build-mac-app.sh"
BUILD_DIR="$SCRIPT_DIR/build"
OUTPUT_DIR="$SCRIPT_DIR/output"
BUILD_INFO_FILE="$OUTPUT_DIR/build-info.sh"

GETVERSION_SOURCE="$SRC_DIR/Installer/GetVersion.cs"
VERSION_SCRIPT="$BUILD_DIR/setversion.sh"
UPDATEMANIFEST_PROJECT="$SRC_DIR/Tools/UpdateManifest/UpdateManifest.csproj"

# Where the disk image goes inside the tree, and the address it will be served
# from. Both come from the one PUBLISH_SUBDIR setting so that they cannot drift
# apart -- a manifest that names a file which is not where it says it is fails
# every download, and does so only for the users, never for whoever published it.
PUBLISH_DIR="$PUBLISH_TREE/$PUBLISH_SUBDIR"
PUBLISH_URL_BASE="$PUBLISH_URL_ROOT/$PUBLISH_SUBDIR"
MANIFEST_FILE="$PUBLISH_TREE/manifest.json"

# ---------------------------------------------------------------------------
# Output helpers
# ---------------------------------------------------------------------------

# Terminal colours, suppressed when not writing to a terminal.
if [[ -t 1 ]]; then
    C_STEP=$'\033[1;35m'; C_INFO=$'\033[0;36m'
    C_ERR=$'\033[1;31m';  C_OFF=$'\033[0m'
else
    C_STEP=""; C_INFO=""; C_ERR=""; C_OFF=""
fi

# step: announce a stage of the process.
step() { printf '\n%s==> %s%s\n' "$C_STEP" "$1" "$C_OFF"; }

# info: report progress within a stage.
info() { printf '%s    %s%s\n' "$C_INFO" "$1" "$C_OFF"; }

# die: report why publishing stopped, and stop.
die() { printf '\n%sERROR: %s%s\n' "$C_ERR" "$1" "$C_OFF" >&2; exit 1; }

# ---------------------------------------------------------------------------
# Command line
# ---------------------------------------------------------------------------

# usage: print command line help.
usage() {
    cat <<'EOF'
Usage: publish-mac-app.sh [build options]

Builds Purple Pen for macOS and publishes the resulting disk image: copies it
into the publishing tree and records it in the tree's manifest.json, so that
running copies of Purple Pen are offered the update.

Options:
  -h, --help        Show this message.

Every other argument is passed straight through to build-mac-app.sh; run
"./build-mac-app.sh --help" to see them. Note that a build made with
--skip-sign or --skip-notarize is not fit for distribution and will not be
published.

Publishing settings -- PUBLISH_TREE, PUBLISH_URL_ROOT and PUBLISH_SUBDIR --
live in config.sh and can be overridden from the environment:

    PUBLISH_TREE=/tmp/testtree ./publish-mac-app.sh
EOF
}

for argument in "$@"; do
    case "$argument" in
        -h|--help) usage; exit 0 ;;
    esac
done

# ---------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------

step "Building"

[[ -x "$BUILD_SCRIPT" ]] || die "Cannot find $BUILD_SCRIPT. Nothing was published."

# set -e stops us here if the build fails, which is what should happen: there is
# nothing to publish and build-mac-app.sh has already said why.
"$BUILD_SCRIPT" "$@"

# ---------------------------------------------------------------------------
# Find out what was built
# ---------------------------------------------------------------------------

step "Checking the build"

[[ -f "$BUILD_INFO_FILE" ]] \
    || die "$BUILD_INFO_FILE was not written, so there is no way to tell what was
built. Nothing was published."

# shellcheck source=/dev/null
source "$BUILD_INFO_FILE"

# A build that Gatekeeper will refuse must not reach the tree. This tests what
# the build recorded about itself rather than scanning the command line, because
# SKIP_SIGN and SKIP_NOTARIZE can equally well come from the environment -- which
# is the whole point of how config.sh is written, and so is easy to leave set.
[[ "${BUILD_SIGNED:-0}" == "1" ]] \
    || die "That build is UNSIGNED and is not fit for distribution. Nothing was published."
[[ "${BUILD_NOTARIZED:-0}" == "1" ]] \
    || die "That build is NOT NOTARIZED and is not fit for distribution. Nothing was published."

[[ -n "${DMG_PATH:-}" ]] \
    || die "That build produced no disk image, which is the file this publishes.
Nothing was published."
[[ -f "$DMG_PATH" ]] \
    || die "\"$DMG_PATH\" does not exist. Nothing was published."

info "Publishing $(basename "$DMG_PATH")"

# ---------------------------------------------------------------------------
# Read the version out of the binary that was built
# ---------------------------------------------------------------------------

step "Reading the version number"

# GetVersion.cs reads the version out of a compiled assembly and writes a shell
# script setting VERSION_STRING, VERSION_PRERELEASE and PROGRAM_TITLE, among
# others. The Windows publish uses the same program, so the title recorded in the
# manifest reads the same way on both platforms; composing it here in bash
# instead would be one more place for the two to drift apart.
#
# It is read from the assembly inside the bundle that was just built, not from
# VersionNumber.cs, so that what goes in the manifest describes the thing being
# published rather than the source tree as it stands now.
VERSION_DLL="$APP_BUNDLE/Contents/MacOS/PurplePenCore.dll"

[[ -f "$VERSION_DLL" ]] || die "Cannot find \"$VERSION_DLL\". Nothing was published."

dotnet run --file "$GETVERSION_SOURCE" -- bash "$VERSION_DLL" > "$VERSION_SCRIPT" \
    || die "Could not read the version number from \"$VERSION_DLL\". Nothing was published."

# shellcheck source=/dev/null
source "$VERSION_SCRIPT"

[[ -n "${PROGRAM_TITLE:-}" ]] \
    || die "\"$VERSION_SCRIPT\" did not set the version variables. Nothing was published."

# The two ways of arriving at the version have to agree. They will not if the
# build reused an app bundle from an earlier run -- with --dmg-only, say -- after
# VersionNumber.cs changed, in which case the manifest would offer a version the
# disk image does not contain.
[[ "$VERSION_STRING" == "$FULL_VERSION" ]] \
    || die "The bundle contains version $VERSION_STRING but the build reports $FULL_VERSION.
Rebuild without --dmg-only. Nothing was published."

# A prerelease goes on the beta channel, a final release on the main channel.
# These are the names UpdateManager.GetChannels asks for: a build filed under any
# other name is one no copy of Purple Pen will ever be offered.
if [[ "$VERSION_PRERELEASE" == "1" ]]; then
    PUBLISH_CHANNEL="beta"
else
    PUBLISH_CHANNEL="main"
fi

info "$PROGRAM_TITLE -- version $VERSION_STRING on the $PUBLISH_CHANNEL channel"

# ---------------------------------------------------------------------------
# Copy into the publishing tree
# ---------------------------------------------------------------------------

step "Copying into the publishing tree"

# The tree itself has to exist already. Creating it would be the wrong thing to
# do when PUBLISH_TREE points into cloud storage that has not mounted yet: the
# build would appear to publish, into a directory nobody uploads.
[[ -d "$PUBLISH_TREE" ]] \
    || die "The publishing tree \"$PUBLISH_TREE\" does not exist. Nothing was published."

mkdir -p "$PUBLISH_DIR" || die "Could not create \"$PUBLISH_DIR\". Nothing was published."

cp "$DMG_PATH" "$PUBLISH_DIR/" \
    || die "Could not copy \"$DMG_PATH\" into \"$PUBLISH_DIR\". Nothing was published."

PUBLISHED_FILE="$PUBLISH_DIR/$(basename "$DMG_PATH")"

info "Copied to $PUBLISHED_FILE"

# Only the disk image is published. To publish the .zip alongside it -- for a
# download page that offers both -- add:
#
#     cp "$ZIP_PATH" "$PUBLISH_DIR/"
#
# To have Purple Pen update itself from the .zip rather than the .dmg, point the
# --file argument below at the copied .zip instead. The two behave quite
# differently once downloaded (see PurplePenCore/UpdateInstallerScript.cs): a
# .zip is expanded over the installed bundle and the application relaunches
# itself, while a .dmg is only opened in Finder for the user to drag across by
# hand. Whichever file --file names is the one users actually download; any other
# file in the tree is there for the web page alone.

# ---------------------------------------------------------------------------
# Record it in the manifest
# ---------------------------------------------------------------------------

step "Updating the manifest"

# --file is the copy in the publishing tree rather than the one in the output
# directory, so that the hash written into the manifest is the hash of the file
# that will actually be served.
#
# --platform is the runtime identifier the build was made for, which is the same
# name UpdateManager.GetPlatformName composes at run time; taking it from the
# build rather than writing it out again keeps the two in step.
dotnet run --project "$UPDATEMANIFEST_PROJECT" --configuration Release -- \
    --manifest "$MANIFEST_FILE" \
    --title "$PROGRAM_TITLE" \
    --version "$VERSION_STRING" \
    --platform "$RUNTIME_IDENTIFIER" \
    --channel "$PUBLISH_CHANNEL" \
    --file "$PUBLISHED_FILE" \
    --url-base "$PUBLISH_URL_BASE" \
    || die "UpdateManifest failed. The disk image was copied into the tree, but
\"$MANIFEST_FILE\" does not describe it, so no one will be offered it."

step "Done publishing $PROGRAM_TITLE"
