#!/bin/bash
#
# build-linux-packages.sh
#
# Builds .deb and .rpm packages of Purple Pen for Linux, writing them to the
# output/ directory.
#
# The pipeline is:
#
#   1. dotnet publish AvPurplePen (Release, net10.0, linux-x64, self-contained)
#   2. rsync the publish output into build/payload, honouring publish-exclude.txt
#   3. Republish PdfConverter self-contained and overlay it onto the payload
#   4. Normalize permissions, which the publish step cannot be trusted to set
#   5. Assemble build/tree as the filesystem the packages will install
#   6. Build the .deb with dpkg-deb, and the .rpm with rpmbuild, from that tree
#   7. Inspect both finished packages and fail on anything wrong
#
# Both packages install a self-contained build to /opt/purplepen, so no .NET
# runtime is required on the target machine.
#
# Run with --help for options. See README.md for prerequisites.
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
DESKTOP_TEMPLATE="$SCRIPT_DIR/$PACKAGE_NAME.desktop.template"
MIME_TEMPLATE="$SCRIPT_DIR/$PACKAGE_NAME-mime.xml.template"
ICON_DIR="$SRC_DIR/AvPurplePen/Assets/AppIcon"

OUTPUT_DIR="$SCRIPT_DIR/$OUTPUT_SUBDIR"

PUBLISH_DIR="$SRC_DIR/AvPurplePen/bin/$CONFIGURATION/$TARGET_FRAMEWORK/$RUNTIME_IDENTIFIER/publish"

# Icon sizes to install under /usr/share/icons/hicolor/<N>x<N>/apps/.
#
# Every one of these is a directory the hicolor theme defines and that
# AvPurplePen/Assets/AppIcon has a pre-rendered PNG for. The 1024 px file in
# that directory is deliberately not listed: hicolor has no 1024 slot, so it
# would install into a directory no icon lookup ever searches.
ICON_SIZES=(16 24 32 48 64 96 128 256 512)

# Size installed as the legacy /usr/share/pixmaps fallback, used by older
# desktop environments and some window managers that predate the icon theme
# specification. 48 px is the conventional size for that directory.
PIXMAP_SIZE=48

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
SKIP_VERIFY="${SKIP_VERIFY:-0}"
SHOW_DEPS_ONLY=0

# usage: print command line help.
usage() {
    cat <<'EOF'
Usage: build-linux-packages.sh [options]

Builds .deb and .rpm packages of Purple Pen for Linux into output/.

Options:
  --skip-publish    Reuse the existing dotnet publish output instead of
                    rebuilding. Useful when iterating on packaging.
  --deb-only        Build only the .deb.
  --rpm-only        Build only the .rpm.
  --skip-verify     Do not inspect the finished packages. Not recommended.
  --show-deps       Report the shared libraries the payload links against and
                    which installed package provides each, then exit. Use this
                    to check DEB_DEPENDS/RPM_REQUIRES against reality. Note it
                    cannot see libraries opened with dlopen, which is how both
                    ICU/OpenSSL and X11 are loaded; the report says so. Uses
                    the existing publish if there is one.
  -h, --help        Show this message.

Configuration lives in config.sh; every setting there can also be given as an
environment variable. See README.md for prerequisites.
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --skip-publish) SKIP_PUBLISH=1 ;;
        --deb-only)     BUILD_DEB=true;  BUILD_RPM=false ;;
        --rpm-only)     BUILD_DEB=false; BUILD_RPM=true ;;
        --skip-verify)  SKIP_VERIFY=1 ;;
        --show-deps)    SHOW_DEPS_ONLY=1 ;;
        -h|--help)      usage; exit 0 ;;
        *)              usage >&2; die "Unknown option: $1" ;;
    esac
    shift
done

# ---------------------------------------------------------------------------
# Choose a build directory that can actually hold a Unix filesystem
# ---------------------------------------------------------------------------

# dir_preserves_modes: succeed if a file created in directory $1 keeps the
# permission bits it is given.
#
# This is an empirical test rather than a check of the filesystem type, because
# the failure it guards against depends on mount options rather than on the
# filesystem itself. A Windows drive mounted into WSL (9p/drvfs without the
# metadata option) reports every file as mode 0777 and silently ignores chmod,
# so a package staged there would install world-writable files -- including a
# world-writable executable, which is a genuine local privilege escalation.
dir_preserves_modes() {
    local dir="$1" probe mode

    mkdir -p "$dir" 2>/dev/null || return 1
    probe="$(mktemp "$dir/.permprobe.XXXXXX" 2>/dev/null)" || return 1

    chmod 0640 "$probe" 2>/dev/null || { rm -f "$probe"; return 1; }
    mode="$(stat -c '%a' "$probe" 2>/dev/null)" || { rm -f "$probe"; return 1; }
    rm -f "$probe"

    [[ "$mode" == "640" ]]
}

# choose_build_dir: settle on BUILD_DIR, preferring the script directory but
# falling back to a temporary directory when the script lives somewhere that
# cannot store Unix permissions.
choose_build_dir() {
    if [[ -n "$BUILD_DIR" ]]; then
        mkdir -p "$BUILD_DIR" || die "Cannot create BUILD_DIR at $BUILD_DIR"
        dir_preserves_modes "$BUILD_DIR" \
            || die "BUILD_DIR ($BUILD_DIR) does not preserve file permissions.

Packages staged there would install every file as mode 0777. Point BUILD_DIR at
a native Linux filesystem."
        return
    fi

    local preferred="$SCRIPT_DIR/build"
    if dir_preserves_modes "$preferred"; then
        BUILD_DIR="$preferred"
        return
    fi

    # The script directory is on a filesystem that cannot store modes -- almost
    # always a Windows drive mounted into WSL. Stage on a native filesystem
    # instead and copy the finished packages back to output/ at the end.
    BUILD_DIR="${TMPDIR:-/tmp}/$PACKAGE_NAME-linux-build"
    mkdir -p "$BUILD_DIR" || die "Cannot create a build directory at $BUILD_DIR"
    dir_preserves_modes "$BUILD_DIR" \
        || die "Neither $preferred nor $BUILD_DIR preserves file permissions.

Set BUILD_DIR to a directory on a native Linux filesystem."

    rm -rf "${preferred:?}" 2>/dev/null || true
    warn "$preferred cannot store Unix permissions (a Windows drive under WSL?)."
    warn "Staging in $BUILD_DIR instead; the packages still land in $OUTPUT_SUBDIR/."
}

# ---------------------------------------------------------------------------
# Preflight checks
# ---------------------------------------------------------------------------

# check_prerequisites: verify the platform and the tools each requested package
# format needs, before anything slow runs.
check_prerequisites() {
    step "Checking prerequisites"

    [[ "$(uname -s)" == "Linux" ]] \
        || die "This script must be run on Linux. On Windows, run it from WSL:

    wsl bash $SCRIPT_DIR/$(basename "${BASH_SOURCE[0]}")"

    local tool
    for tool in dotnet rsync file find sed awk; do
        command -v "$tool" >/dev/null 2>&1 || die "Required tool not found: $tool"
    done

    if [[ "$BUILD_DEB" == "true" ]]; then
        command -v dpkg-deb >/dev/null 2>&1 \
            || die "dpkg-deb not found, which is needed to build the .deb.

On Debian/Ubuntu it is part of the base system. Elsewhere install the 'dpkg'
package, or build only the .rpm with --rpm-only."
    fi

    if [[ "$BUILD_RPM" == "true" ]]; then
        command -v rpmbuild >/dev/null 2>&1 \
            || die "rpmbuild not found, which is needed to build the .rpm.

Install it with one of:

    sudo apt install rpm          # Debian / Ubuntu
    sudo dnf install rpm-build    # Fedora / RHEL

or build only the .deb with --deb-only."
    fi

    [[ "$BUILD_DEB" == "true" || "$BUILD_RPM" == "true" ]] \
        || die "Both BUILD_DEB and BUILD_RPM are false; there is nothing to build."

    [[ -f "$PROJECT_FILE" ]] || die "Cannot find AvPurplePen project at $PROJECT_FILE"
    [[ -f "$DESKTOP_TEMPLATE" ]] || die "Cannot find the desktop entry template at $DESKTOP_TEMPLATE"
    [[ -d "$ICON_DIR" ]] || die "Cannot find the icon directory at $ICON_DIR"

    if [[ "$REGISTER_MIME_TYPE" == "true" ]]; then
        [[ -f "$MIME_TEMPLATE" ]] \
            || die "Cannot find the MIME type template at $MIME_TEMPLATE
(set REGISTER_MIME_TYPE=false to build without file associations)"
    fi

    if [[ ! -f "$EXCLUDE_FILE" ]]; then
        warn "No publish-exclude.txt found; nothing will be excluded from the payload."
        EXCLUDE_FILE=""
    fi

    info "dotnet SDK $(dotnet --version)"
}

# ---------------------------------------------------------------------------
# Version and architecture
# ---------------------------------------------------------------------------

# read_version: extract the version from PurplePenCore/VersionNumber.cs and
# derive the upstream version string the packages use.
#
# VersionNumber.cs holds a four-part version such as "4.0.0.210" whose last
# component encodes the release stage rather than a build number (100s alpha,
# 200s beta, 300s release candidate, 500 stable), with the tens digit as the
# sequence number within that stage.
#
# Shipping that fourth component verbatim would be wrong in a way that only
# shows up later: both dpkg and rpm would sort "4.0.0.210" AFTER the eventual
# stable "4.0.0", so anyone who installed the beta would never be offered the
# release as an upgrade. Both systems spell "sorts before" with a tilde, so the
# stage is folded into the upstream version as "4.0.0~beta1" instead.
read_version() {
    [[ -f "$VERSION_FILE" ]] || die "Cannot find $VERSION_FILE"

    FULL_VERSION="$(sed -n 's/.*Current[[:space:]]*=[[:space:]]*"\([0-9.]*\)".*/\1/p' "$VERSION_FILE" | head -1)"
    [[ -n "$FULL_VERSION" ]] || die "Could not parse the version number out of $VERSION_FILE"

    local base stage seq stage_name
    base="$(echo "$FULL_VERSION" | cut -d. -f1-3)"
    stage="$(echo "$FULL_VERSION" | cut -d. -f4)"

    if [[ -n "$PACKAGE_VERSION" ]]; then
        UPSTREAM_VERSION="$PACKAGE_VERSION"
        VERSION_LABEL="from PACKAGE_VERSION"
        return
    fi

    # Missing fourth component: treat as a plain release.
    if [[ -z "$stage" ]]; then
        UPSTREAM_VERSION="$base"
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
        UPSTREAM_VERSION="$base"
        VERSION_LABEL="stable release"
    else
        # The tilde is what makes this sort BEFORE $base in both dpkg and rpm.
        UPSTREAM_VERSION="$base~$stage_name$seq"
        VERSION_LABEL="$stage_name $seq"
    fi
}

# resolve_architecture: map the .NET runtime identifier onto the architecture
# names Debian and RPM use, which agree with each other on none of them.
resolve_architecture() {
    case "$RUNTIME_IDENTIFIER" in
        linux-x64)   DEB_ARCH="amd64"; RPM_ARCH="x86_64"  ;;
        linux-arm64) DEB_ARCH="arm64"; RPM_ARCH="aarch64" ;;
        linux-arm)   DEB_ARCH="armhf"; RPM_ARCH="armv7hl" ;;
        linux-x86)   DEB_ARCH="i386";  RPM_ARCH="i686"    ;;
        linux-musl-*)
            die "RUNTIME_IDENTIFIER is $RUNTIME_IDENTIFIER, which targets musl (Alpine).
Alpine uses apk packages, not .deb or .rpm. Use a glibc RID such as linux-x64." ;;
        *)
            die "Unrecognized RUNTIME_IDENTIFIER: $RUNTIME_IDENTIFIER
Add its Debian and RPM architecture names to resolve_architecture." ;;
    esac
}

# ---------------------------------------------------------------------------
# Step 1: publish
# ---------------------------------------------------------------------------

# restore_projects: run an explicit NuGet restore for both projects.
#
# dotnet publish restores implicitly, but PdfConverter is not a ProjectReference
# of AvPurplePen -- AvPurplePen.csproj's BuildPdfConverter target invokes it
# through an MSBuild task with Targets="Build", which does no restore of its
# own. So PdfConverter uses whatever obj/project.assets.json it finds.
#
# That matters because obj/ is shared with builds run on other operating
# systems. A checkout on a Windows drive that WSL also builds from leaves an
# assets file naming Visual Studio's package folder:
#
#     error MSB4018: Unable to find fallback package folder
#     'C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages'
#
# and the publish fails deep inside ResolvePackageAssets, well after it looked
# like it was working. Restoring both projects up front makes the build
# independent of who wrote obj/ last.
restore_projects() {
    info "Restoring NuGet packages"

    dotnet restore "$PROJECT_FILE" --runtime "$RUNTIME_IDENTIFIER" --nologo \
        || die "Failed to restore $PROJECT_FILE."

    local pdf_project="$SRC_DIR/PdfConverter/PdfConverter.csproj"
    if [[ -f "$pdf_project" ]]; then
        # No --runtime here: BuildPdfConverter builds it RID-agnostic.
        dotnet restore "$pdf_project" --nologo \
            || die "Failed to restore $pdf_project."
    fi
}

# publish_app: run dotnet publish for AvPurplePen into its conventional publish
# directory (bin/<Config>/<Framework>/<RID>/publish inside the project).
publish_app() {
    step "Publishing AvPurplePen ($CONFIGURATION, $TARGET_FRAMEWORK, $RUNTIME_IDENTIFIER)"

    if [[ "$SKIP_PUBLISH" == "1" ]]; then
        [[ -d "$PUBLISH_DIR" ]] || die "--skip-publish was given but $PUBLISH_DIR does not exist."
        info "Skipped; reusing $PUBLISH_DIR"
        return
    fi

    restore_projects

    # Remove stale output so files excluded and then re-included cannot linger.
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
# Step 2: stage the payload
# ---------------------------------------------------------------------------

# stage_payload: mirror the publish output into build/payload, dropping
# anything matched by publish-exclude.txt. This is the directory that becomes
# $INSTALL_PREFIX, so it is the place to inspect when tuning the exclusion list.
stage_payload() {
    step "Staging the application payload"

    mkdir -p "$PAYLOAD_DIR"

    local rsync_args=(--archive --delete --delete-excluded)
    if [[ -n "$EXCLUDE_FILE" ]]; then
        rsync_args+=(--exclude-from="$EXCLUDE_FILE")
    fi

    local rid_dir
    while IFS= read -r rid_dir; do
        rsync_args+=(--exclude="/$rid_dir/")
        info "Skipping $rid_dir/ ($(dir_size "$PUBLISH_DIR/$rid_dir") of another platform's build output)"
    done < <(foreign_rid_dirs "$PUBLISH_DIR")

    rsync "${rsync_args[@]}" "$PUBLISH_DIR/" "$PAYLOAD_DIR/"

    [[ -f "$PAYLOAD_DIR/$APP_NAME" ]] \
        || die "The main executable '$APP_NAME' was excluded from the payload. Check publish-exclude.txt."

    # Belt and braces. The exclusions above are computed from the same rule, so
    # this should be unreachable -- but shipping another platform's runtime is
    # invisible in testing (nothing loads it, so nothing breaks) and shows up
    # only as an inexplicably large package, which is exactly the kind of
    # regression worth a hard failure.
    local leftover
    leftover="$(foreign_rid_dirs "$PAYLOAD_DIR" | tr '\n' ' ')"
    [[ -z "$leftover" ]] \
        || die "Another platform's build output survived staging: $leftover
These directories hold binaries this package can never load. Check
foreign_rid_dirs and publish-exclude.txt."

    info "Staged $(count_files "$PAYLOAD_DIR") files ($(dir_size "$PAYLOAD_DIR")) in $PAYLOAD_DIR"
}

# foreign_rid_dirs: print the name of every top-level directory under $1 whose
# name is a .NET runtime identifier -- that is, another platform's build output
# that has no business in this package.
#
# These arrive by a route that is easy to miss. AvPurplePen.csproj's
# CopyPdfConverterToPublishOutput target globs
# PdfConverter/bin/$(Configuration)/net10.0/**/*.* recursively, so it sweeps in
# every RID-specific subdirectory anyone has ever built there -- including from
# a completely different operating system. A checkout that has been published
# for Windows leaves PdfConverter/bin/Release/net10.0/win-x64/, and that whole
# self-contained Windows .NET runtime (98 MB) then lands inside the Linux
# package, where nothing can ever load it.
#
# This is the same recursive-glob hazard INSTALLERS.md 2.4 describes, but
# pointing the other way: there the installer script poisons its own next
# build, here another platform's build poisons this one. The helper published
# by stage_pdf_converter is redirected out of the source tree for exactly that
# reason, which fixes our direction but not Visual Studio's.
#
# Detection is by the OS token a RID always starts with. Satellite resource
# directories can look similar (pt-BR, zh-Hans, nb-NO), but no culture code
# begins with one of these words, so they are never caught by mistake.
foreign_rid_dirs() {
    local dir name
    while IFS= read -r dir; do
        name="$(basename "$dir")"
        if [[ "$name" =~ ^(win|linux|osx|freebsd|illumos|solaris|android|ios|iossimulator|tvos|tvossimulator|maccatalyst|browser|unix)(-|$) ]]; then
            printf '%s\n' "$name"
        fi
    done < <(find "$1" -mindepth 1 -maxdepth 1 -type d | sort)
}

# count_files: print the number of regular files under directory $1.
count_files() { find "$1" -type f | wc -l | tr -d ' '; }

# dir_size: print the human-readable size of directory $1.
dir_size() { du -sh "$1" | cut -f1 | tr -d ' '; }

# ---------------------------------------------------------------------------
# Step 2b: the PdfConverter helper
# ---------------------------------------------------------------------------

# framework_version_of: print the Microsoft.NETCore.App version a
# runtimeconfig.json resolves to, whether it is framework-dependent
# ("framework") or self-contained ("includedFrameworks"). $1 is the path to the
# runtimeconfig.json.
#
# Implemented with sed rather than python3 so the script does not depend on a
# Python interpreter being installed on the build machine.
framework_version_of() {
    tr -d ' \n' < "$1" \
        | sed -n 's/.*"name":"Microsoft.NETCore.App","version":"\([^"]*\)".*/\1/p' \
        | head -1
}

# stage_pdf_converter: republish PdfConverter self-contained for this RID and
# overlay it onto the staged payload.
#
# PurplePenCore/PdfMapFile.cs launches this helper as a separate process to
# rasterize PDF map templates. The copy that AvPurplePen.csproj drops into the
# publish directory is a plain build output and is framework-dependent, so it
# cannot start alongside a self-contained application: its apphost finds the
# app's own libhostfxr.so next to it, resolves ".NET location" to the
# application directory, finds no shared framework there and refuses to start.
#
# A RID-specific self-contained publish fixes that and additionally flattens
# libpdfium.so to the top level, which is what makes excluding the runtimes/
# tree safe -- that tree holds the only other copy of libpdfium.
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
    # copy of the helper plus a self-contained .NET runtime -- into the payload.
    # Left unchecked it roughly doubles the file count on every build.
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
    # app resolved different patch versions of Microsoft.NETCore.App, that would
    # silently downgrade or upgrade the runtime underneath the main app, so
    # refuse rather than ship a subtly broken package.
    local app_fw helper_fw
    app_fw="$(framework_version_of "$PAYLOAD_DIR/$APP_NAME.runtimeconfig.json")"
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
    rsync "${rsync_args[@]}" "$helper_dir/" "$PAYLOAD_DIR/"

    if [[ ! -f "$PAYLOAD_DIR/PdfConverter" ]]; then
        warn "PdfConverter was excluded by publish-exclude.txt; PDF map templates will not work."
        return
    fi
    if [[ ! -f "$PAYLOAD_DIR/libpdfium.so" ]]; then
        warn "libpdfium.so is not in the payload; PDF map templates will not work."
    fi

    info "Payload is now $(dir_size "$PAYLOAD_DIR") with the helper included"
}

# ---------------------------------------------------------------------------
# Step 3: normalize permissions
# ---------------------------------------------------------------------------

# is_elf: succeed if file $1 begins with the ELF magic number.
is_elf() {
    # read -N 4 on the raw bytes, with LC_ALL=C so the high bytes are not
    # interpreted as multibyte characters.
    local magic
    IFS= LC_ALL=C read -r -N 4 magic < "$1" 2>/dev/null || return 1
    [[ "$magic" == $'\x7fELF' ]]
}

# normalize_payload_permissions: give every file in the payload a deliberate
# mode instead of whatever the publish happened to leave behind.
#
# Two independent reasons this cannot be skipped:
#
#   - dotnet publish sets the executable bit on the apphost, but that bit does
#     not survive a tree that has passed through a filesystem which does not
#     store it. A package whose apphost is 0644 installs cleanly and then fails
#     to run with "Permission denied".
#   - Conversely, a Windows drive mounted into WSL reports EVERY file as 0777.
#     Packaging that as-is ships world-writable files, which is a local
#     privilege escalation on any machine that installs it.
#
# Deriving the mode from the file's actual content rather than from its name or
# its current bits is what makes the result identical no matter where the build
# ran.
normalize_payload_permissions() {
    step "Normalizing payload permissions"

    find "$PAYLOAD_DIR" -type d -exec chmod 0755 {} +
    find "$PAYLOAD_DIR" -type f -exec chmod 0644 {} +

    local file exec_count=0
    while IFS= read -r -d '' file; do
        if is_elf "$file"; then
            chmod 0755 "$file"
            exec_count=$((exec_count + 1))
        fi
    done < <(find "$PAYLOAD_DIR" -type f -print0)

    [[ -x "$PAYLOAD_DIR/$APP_NAME" ]] \
        || die "$APP_NAME is not executable after normalization, so it is not an ELF binary.
Check that the publish really produced a native apphost for $RUNTIME_IDENTIFIER."

    info "Marked $exec_count ELF binaries executable; everything else is 0644"
}

# ---------------------------------------------------------------------------
# Step 4: assemble the install tree
# ---------------------------------------------------------------------------

# render_template: copy template $1 to $2, substituting @TOKEN@ placeholders,
# then fail if any placeholder is left over.
#
# Values are passed through sed's replacement text, so a value containing an
# ampersand or a pipe would corrupt the output. None of the configured values
# do today, and the leftover-placeholder check below catches the case where a
# token was simply forgotten.
render_template() {
    local src="$1" dest="$2"

    sed -e "s|@PACKAGE_NAME@|$PACKAGE_NAME|g" \
        -e "s|@APP_NAME@|$APP_NAME|g" \
        -e "s|@DISPLAY_NAME@|$DISPLAY_NAME|g" \
        -e "s|@PACKAGE_SUMMARY@|$PACKAGE_SUMMARY|g" \
        -e "s|@DESKTOP_CATEGORIES@|$DESKTOP_CATEGORIES|g" \
        -e "s|@DESKTOP_KEYWORDS@|$DESKTOP_KEYWORDS|g" \
        -e "s|@MIME_TYPE@|$MIME_TYPE|g" \
        -e "s|@MIME_TYPE_LINE@|$MIME_TYPE_LINE|g" \
        -e "s|@VERSION@|$UPSTREAM_VERSION|g" \
        "$src" > "$dest"

    if grep -q '@[A-Z_]*@' "$dest"; then
        die "$(basename "$dest") still contains unsubstituted placeholders: $(grep -o '@[A-Z_]*@' "$dest" | sort -u | tr '\n' ' ')"
    fi
}

# build_install_tree: lay out build/tree exactly as the packages will install
# it, so that both dpkg-deb and rpmbuild can package the identical filesystem.
build_install_tree() {
    step "Assembling the install tree"

    rm -rf "$TREE_DIR"

    # The payload itself, at $INSTALL_PREFIX.
    local prefix_dir="$TREE_DIR$INSTALL_PREFIX"
    mkdir -p "$(dirname "$prefix_dir")"
    cp -a "$PAYLOAD_DIR" "$prefix_dir"

    # The launcher on PATH.
    #
    # A symlink rather than a wrapper script: .NET's apphost finds its
    # assemblies by resolving /proc/self/exe, which follows symlinks all the
    # way to $INSTALL_PREFIX/$APP_NAME, so the application directory comes out
    # correct with no wrapper needed.
    mkdir -p "$TREE_DIR/usr/bin"
    ln -sf "$INSTALL_PREFIX/$APP_NAME" "$TREE_DIR/usr/bin/$PACKAGE_NAME"

    install_icons
    install_desktop_entry
    install_mime_type
    install_documentation

    # Directories the packages create are 0755; the files placed above are
    # already 0644 except where explicitly made executable.
    find "$TREE_DIR/usr" -type d -exec chmod 0755 {} +
    find "$TREE_DIR/usr" -type f -exec chmod 0644 {} +

    info "Install tree is $(count_files "$TREE_DIR") files ($(dir_size "$TREE_DIR"))"
}

# install_icons: copy the pre-rendered PNGs into the hicolor theme, the SVG
# into its scalable directory, and one PNG into the legacy pixmaps directory.
#
# The PNGs are installed at their native sizes rather than resampled from one
# large source, so any hand-tuning of the small sizes is preserved. Each file's
# real pixel width is checked against the size its name claims, because an icon
# installed into the wrong hicolor directory is picked up at the wrong size and
# looks broken in exactly one place.
install_icons() {
    local size source dest actual

    for size in "${ICON_SIZES[@]}"; do
        source="$ICON_DIR/$ICON_FAMILY.${size}x${size}.png"
        [[ -f "$source" ]] || die "Missing icon: $source

Check ICON_FAMILY in config.sh against the files in $ICON_DIR."

        actual="$(png_width "$source")"
        if [[ -n "$actual" && "$actual" != "$size" ]]; then
            die "Icon $source is ${actual}px wide but its name claims ${size}px."
        fi

        dest="$TREE_DIR/usr/share/icons/hicolor/${size}x${size}/apps"
        mkdir -p "$dest"
        cp "$source" "$dest/$PACKAGE_NAME.png"
    done

    # The scalable SVG, which is what a desktop rendering at an unusual size
    # (a HiDPI dock, say) prefers over any fixed-size PNG.
    source="$ICON_DIR/$ICON_FAMILY.svg"
    if [[ -f "$source" ]]; then
        mkdir -p "$TREE_DIR/usr/share/icons/hicolor/scalable/apps"
        cp "$source" "$TREE_DIR/usr/share/icons/hicolor/scalable/apps/$PACKAGE_NAME.svg"
    else
        warn "No $ICON_FAMILY.svg in $ICON_DIR; installing PNG icons only."
    fi

    # Legacy fallback for desktops that predate the icon theme specification.
    mkdir -p "$TREE_DIR/usr/share/pixmaps"
    cp "$ICON_DIR/$ICON_FAMILY.${PIXMAP_SIZE}x${PIXMAP_SIZE}.png" \
       "$TREE_DIR/usr/share/pixmaps/$PACKAGE_NAME.png"

    info "Installed ${#ICON_SIZES[@]} icon sizes from the $ICON_FAMILY family"
}

# png_width: print the pixel width recorded in the IHDR header of PNG $1, or
# nothing if it cannot be read.
#
# The width is a big-endian 32-bit integer at byte offset 16. od is used rather
# than an image tool so that the check costs no extra dependency.
png_width() {
    od -An -j16 -N4 -tu4 --endian=big "$1" 2>/dev/null | tr -d ' \n'
}

# install_desktop_entry: render the .desktop file and validate it if the
# freedesktop validator is available.
install_desktop_entry() {
    local dest_dir="$TREE_DIR/usr/share/applications"
    local dest="$dest_dir/$PACKAGE_NAME.desktop"

    mkdir -p "$dest_dir"
    render_template "$DESKTOP_TEMPLATE" "$dest"

    if command -v desktop-file-validate >/dev/null 2>&1; then
        desktop-file-validate "$dest" \
            || die "The generated desktop entry is not valid. Check $DESKTOP_TEMPLATE."
        info "Desktop entry validates"
    else
        info "Desktop entry written (desktop-file-validate not installed, so not checked)"
    fi
}

# install_mime_type: register the .ppen MIME type, unless turned off.
install_mime_type() {
    if [[ "$REGISTER_MIME_TYPE" != "true" ]]; then
        info "File associations disabled (REGISTER_MIME_TYPE is not true)"
        return
    fi

    local dest_dir="$TREE_DIR/usr/share/mime/packages"
    mkdir -p "$dest_dir"
    render_template "$MIME_TEMPLATE" "$dest_dir/$PACKAGE_NAME.xml"

    validate_mime_definition "$dest_dir/$PACKAGE_NAME.xml"
    info "Registered $MIME_TYPE for *.ppen"
}

# validate_mime_definition: run the MIME definition at $1 through
# update-mime-database in a throwaway directory, and fail the build if it is
# rejected.
#
# Worth the trouble because the failure is otherwise completely silent from the
# build's point of view: a malformed definition still packages, still installs,
# and only prints its complaint from the shared-mime-info trigger partway
# through someone else's apt output -- after which the file association simply
# does not work, with nothing to connect it back to.
#
# Two details make this less obvious than it looks. update-mime-database exits
# 0 whether or not it rejected the type, so the exit code says nothing and the
# output has to be inspected. And it always emits a note about the directory
# not being in XDG_DATA_DIRS, which is expected here and must not be treated as
# a failure -- hence matching on "Error" specifically rather than on any output.
validate_mime_definition() {
    local source="$1"

    if ! command -v update-mime-database >/dev/null 2>&1; then
        info "shared-mime-info not installed, so the MIME definition was not checked"
        return
    fi

    local probe="$BUILD_DIR/mime-check" output
    rm -rf "$probe"
    mkdir -p "$probe/packages"
    cp "$source" "$probe/packages/"

    output="$(update-mime-database "$probe" 2>&1 || true)"
    rm -rf "$probe"

    if printf '%s\n' "$output" | grep -q '^Error'; then
        die "The generated MIME definition was rejected by update-mime-database:

$(printf '%s\n' "$output" | grep '^Error' | sed 's/^/    /')

The .ppen file association would not work. Check $MIME_TEMPLATE."
    fi
}

# install_documentation: write the copyright file both packaging systems expect
# to find under /usr/share/doc.
install_documentation() {
    local doc_dir="$TREE_DIR/usr/share/doc/$PACKAGE_NAME"
    mkdir -p "$doc_dir"

    cat > "$doc_dir/copyright" <<EOF
$DISPLAY_NAME
$PACKAGE_URL

$COPYRIGHT

Licensed under the $PACKAGE_LICENSE license. The full license text is in the
source distribution.

This package bundles the .NET runtime, which is distributed by Microsoft under
the MIT license, together with its native dependencies (SkiaSharp, HarfBuzz,
PDFium). Their licenses are those of the respective upstream projects.
EOF
}

# ---------------------------------------------------------------------------
# Step 5a: the .deb
# ---------------------------------------------------------------------------

# deb_description: print PACKAGE_DESCRIPTION formatted as a Debian extended
# description -- every line indented by exactly one space, wrapped to fit.
deb_description() {
    printf '%s\n' "$PACKAGE_DESCRIPTION" | fold -s -w 76 | sed -e 's/[[:space:]]*$//' -e 's/^/ /'
}

# write_deb_control: create the DEBIAN control directory inside the install
# tree -- the control file itself plus the maintainer scripts that refresh the
# desktop, MIME and icon caches.
write_deb_control() {
    local debian_dir="$TREE_DIR/DEBIAN"
    mkdir -p "$debian_dir"

    # Installed-Size is in KiB and is what apt shows as "After this operation,
    # N MB of additional disk space will be used".
    local installed_size
    installed_size="$(du -sk --exclude=DEBIAN "$TREE_DIR" | cut -f1)"

    {
        printf 'Package: %s\n'       "$PACKAGE_NAME"
        printf 'Version: %s\n'       "$DEB_VERSION"
        printf 'Section: %s\n'       "$DEB_SECTION"
        printf 'Priority: optional\n'
        printf 'Architecture: %s\n'  "$DEB_ARCH"
        printf 'Depends: %s\n'       "$DEB_DEPENDS"
        [[ -n "$DEB_RECOMMENDS" ]] && printf 'Recommends: %s\n' "$DEB_RECOMMENDS"
        printf 'Installed-Size: %s\n' "$installed_size"
        printf 'Maintainer: %s\n'    "$PACKAGE_MAINTAINER"
        printf 'Homepage: %s\n'      "$PACKAGE_URL"
        printf 'Description: %s\n'   "$PACKAGE_SUMMARY"
        deb_description
    } > "$debian_dir/control"

    write_maintainer_script "$debian_dir/postinst" "configure"
    write_maintainer_script "$debian_dir/postrm"   "remove upgrade purge"
}

# write_maintainer_script: write a Debian maintainer script at $1 that refreshes
# the desktop database, MIME database and icon cache. $2 is the space-separated
# list of first arguments the body should run for.
#
# Every command is guarded by command -v and by "|| true": these caches are a
# convenience, and a package that fails to install because an optional cache
# tool is missing on a minimal system would be far worse than a stale menu.
write_maintainer_script() {
    local path="$1" actions="$2"

    cat > "$path" <<EOF
#!/bin/sh
# Refresh the freedesktop caches so the menu entry, the icon and the .ppen file
# association appear without the user logging out. Generated by
# build-linux-packages.sh -- do not edit.
set -e

case "\$1" in
$(printf '%s' "$actions" | tr ' ' '|'))
    if command -v update-desktop-database >/dev/null 2>&1; then
        update-desktop-database -q /usr/share/applications || true
    fi
    if command -v update-mime-database >/dev/null 2>&1; then
        update-mime-database /usr/share/mime >/dev/null 2>&1 || true
    fi
    if command -v gtk-update-icon-cache >/dev/null 2>&1; then
        gtk-update-icon-cache -q -f /usr/share/icons/hicolor >/dev/null 2>&1 || true
    fi
    ;;
esac

exit 0
EOF

    chmod 0755 "$path"
}

# build_deb: package the install tree with dpkg-deb.
build_deb() {
    step "Building the .deb"

    if [[ "$BUILD_DEB" != "true" ]]; then
        info "Skipped."
        return
    fi

    write_deb_control

    DEB_FILE="$OUTPUT_DIR/${PACKAGE_NAME}_${DEB_VERSION}_${DEB_ARCH}.deb"
    rm -f "$DEB_FILE"

    # --root-owner-group records every file as owned by root:root rather than
    # by whoever ran the build. Without it the package installs files owned by
    # a uid that means nothing on the target machine, and dpkg-deb warns.
    dpkg-deb --root-owner-group --build "$TREE_DIR" "$DEB_FILE" >/dev/null \
        || die "dpkg-deb failed to build the package."

    # DEBIAN/ is a dpkg-only control directory. Remove it now so the same tree
    # can be handed to rpmbuild without it being packaged as installable files.
    rm -rf "$TREE_DIR/DEBIAN"

    info "Wrote $(basename "$DEB_FILE") ($(du -h "$DEB_FILE" | cut -f1 | tr -d ' '))"
}

# ---------------------------------------------------------------------------
# Step 5b: the .rpm
# ---------------------------------------------------------------------------

# rpm_requires_lines: print one "Requires:" line per comma-separated entry in
# $1, which is the form rpm wants.
rpm_requires_lines() {
    local keyword="$1" list="$2" entry
    [[ -n "$list" ]] || return 0
    printf '%s' "$list" | tr ',' '\n' | while IFS= read -r entry; do
        entry="$(printf '%s' "$entry" | sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//')"
        [[ -n "$entry" ]] && printf '%s: %s\n' "$keyword" "$entry"
    done
}

# write_spec: generate the RPM spec file.
#
# The spec deliberately turns off most of rpm's automation, because everything
# it would do is either wrong or already done:
#
#   AutoReqProv: no       rpm would scan the ~90 bundled .so files and generate
#                         both Provides for them (polluting the global soname
#                         namespace with libSkiaSharp.so and friends) and
#                         Requires for every symbol version they reference.
#                         The dependencies are declared explicitly instead.
#   debug_package %{nil}  there are no sources to extract debuginfo from.
#   __os_install_post     the brp-* scripts would strip the bundled binaries,
#     %{nil}              which breaks the .NET runtime's own integrity, and
#                         rewrite shebangs in files that are not scripts.
#   _build_id_links none  rpm >= 4.13 otherwise wants a build-id link for every
#                         ELF file, and refuses when two of them collide.
write_spec() {
    local spec="$1"

    # Precomputed rather than inlined as a conditional inside the heredoc, so
    # that the %files list stays a plain substitution with no command whose
    # exit status could interact with set -e.
    local mime_files_line=""
    if [[ "$REGISTER_MIME_TYPE" == "true" ]]; then
        mime_files_line="/usr/share/mime/packages/$PACKAGE_NAME.xml"
    fi

    cat > "$spec" <<EOF
# $PACKAGE_NAME.spec -- generated by build-linux-packages.sh. Do not edit.
#
# The package contents are not built here; %install copies the tree that the
# build script has already assembled and verified.

%global debug_package %{nil}
%global __os_install_post %{nil}
%global _build_id_links none

Name:           $PACKAGE_NAME
Version:        $RPM_VERSION
Release:        $PACKAGE_RELEASE
Summary:        $PACKAGE_SUMMARY
License:        $PACKAGE_LICENSE
URL:            $PACKAGE_URL
Vendor:         $PACKAGE_VENDOR
Group:          $RPM_GROUP
AutoReqProv:    no
$(rpm_requires_lines "Requires" "$RPM_REQUIRES")
$(rpm_requires_lines "Recommends" "$RPM_RECOMMENDS")

%description
$(printf '%s\n' "$PACKAGE_DESCRIPTION" | fold -s -w 76 | sed 's/[[:space:]]*$//')

%install
rm -rf %{buildroot}
mkdir -p %{buildroot}
cp -a %{_stagetree}/. %{buildroot}/
# Belt and braces: DEBIAN/ is dpkg's control directory and must never be
# installed. build_deb removes it, but a partial run could leave it behind.
rm -rf %{buildroot}/DEBIAN

%files
%defattr(-,root,root,-)
$INSTALL_PREFIX
/usr/bin/$PACKAGE_NAME
/usr/share/applications/$PACKAGE_NAME.desktop
/usr/share/icons/hicolor/*/apps/$PACKAGE_NAME.*
/usr/share/pixmaps/$PACKAGE_NAME.png
$mime_files_line
%dir /usr/share/doc/$PACKAGE_NAME
%license /usr/share/doc/$PACKAGE_NAME/copyright

%post
# Refresh the freedesktop caches. Guarded so a minimal system without them
# still installs cleanly.
if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database -q /usr/share/applications || :
fi
if command -v update-mime-database >/dev/null 2>&1; then
    update-mime-database /usr/share/mime >/dev/null 2>&1 || :
fi
if command -v gtk-update-icon-cache >/dev/null 2>&1; then
    gtk-update-icon-cache -q -f /usr/share/icons/hicolor >/dev/null 2>&1 || :
fi

%postun
if [ \$1 -eq 0 ]; then
    if command -v update-desktop-database >/dev/null 2>&1; then
        update-desktop-database -q /usr/share/applications || :
    fi
    if command -v update-mime-database >/dev/null 2>&1; then
        update-mime-database /usr/share/mime >/dev/null 2>&1 || :
    fi
    if command -v gtk-update-icon-cache >/dev/null 2>&1; then
        gtk-update-icon-cache -q -f /usr/share/icons/hicolor >/dev/null 2>&1 || :
    fi
fi

%changelog
* $(LC_ALL=C date '+%a %b %d %Y') $PACKAGE_MAINTAINER - $RPM_VERSION-$PACKAGE_RELEASE
- Purple Pen $FULL_VERSION
EOF
}

# build_rpm: package the install tree with rpmbuild.
build_rpm() {
    step "Building the .rpm"

    if [[ "$BUILD_RPM" != "true" ]]; then
        info "Skipped."
        return
    fi

    local rpm_top="$BUILD_DIR/rpmbuild"
    local spec="$rpm_top/SPECS/$PACKAGE_NAME.spec"

    rm -rf "$rpm_top"
    mkdir -p "$rpm_top"/{BUILD,BUILDROOT,RPMS,SOURCES,SPECS,SRPMS}

    write_spec "$spec"

    # --target sets the architecture recorded in the package, which matters
    # when cross-building (linux-arm64 on an x86-64 machine). _binary_payload
    # pins xz compression: rpm 4.14+ defaults to zstd, which rpm on older
    # distributions such as RHEL 7/8 cannot read.
    rpmbuild -bb "$spec" \
        --define "_topdir $rpm_top" \
        --define "_stagetree $TREE_DIR" \
        --define "_binary_payload w7.xzdio" \
        --target "$RPM_ARCH" \
        --quiet \
        || die "rpmbuild failed to build the package."

    local built
    built="$(find "$rpm_top/RPMS" -name '*.rpm' -type f | head -1)"
    [[ -n "$built" ]] || die "rpmbuild reported success but produced no .rpm."

    RPM_FILE="$OUTPUT_DIR/$(basename "$built")"
    rm -f "$RPM_FILE"
    cp "$built" "$RPM_FILE"

    info "Wrote $(basename "$RPM_FILE") ($(du -h "$RPM_FILE" | cut -f1 | tr -d ' '))"
}

# ---------------------------------------------------------------------------
# Step 6: verify the finished packages
# ---------------------------------------------------------------------------

# verify_deb: inspect the built .deb and fail on anything that would not work
# on a user's machine.
#
# Checking the finished archive rather than the staging tree is the point: it
# is the only way to catch something that dpkg-deb itself changed or dropped.
verify_deb() {
    [[ "$BUILD_DEB" == "true" ]] || return 0

    local contents
    contents="$(dpkg-deb --contents "$DEB_FILE")" \
        || die "dpkg-deb could not read back the package it just built."

    # The executable must be executable, or the package installs and then fails
    # to launch with "Permission denied".
    printf '%s\n' "$contents" | grep -qE "^-rwxr-xr-x .*\.$INSTALL_PREFIX/$APP_NAME\$" \
        || die "$INSTALL_PREFIX/$APP_NAME is missing or not mode 0755 in the .deb."

    # A group- or world-writable file in a package installed as root is a local
    # privilege escalation. This is what catches a build staged on a filesystem
    # that cannot store permissions.
    #
    # In the ls-style mode string, character 5 is the group write bit and
    # character 8 is the other write bit (counting the type character as 0).
    # The leading [-d] excludes symlinks, whose mode is always lrwxrwxrwx and
    # carries no meaning of its own.
    local writable
    writable="$(printf '%s\n' "$contents" | grep -cE '^[-d](.{4}w|.{7}w)' || true)"
    [[ "$writable" -eq 0 ]] \
        || die "$writable file(s) in the .deb are group- or world-writable.
This usually means the build was staged on a filesystem that does not store
Unix permissions. Set BUILD_DIR to a native Linux filesystem."

    # The launcher must be a symlink pointing into the payload.
    printf '%s\n' "$contents" | grep -q "usr/bin/$PACKAGE_NAME -> $INSTALL_PREFIX/$APP_NAME\$" \
        || die "/usr/bin/$PACKAGE_NAME is not a symlink to $INSTALL_PREFIX/$APP_NAME in the .deb."

    printf '%s\n' "$contents" | grep -q "usr/share/applications/$PACKAGE_NAME.desktop\$" \
        || die "The desktop entry is missing from the .deb."

    # Every file must be owned by root:root, not by the account that ran the
    # build. --root-owner-group is what ensures this.
    local nonroot
    nonroot="$(printf '%s\n' "$contents" | awk '{print $2}' | grep -cv '^root/root$' || true)"
    [[ "$nonroot" -eq 0 ]] || die "$nonroot entries in the .deb are not owned by root:root."

    # The control fields dpkg parsed back out, which proves the control file is
    # syntactically valid rather than merely present.
    local field
    for field in Package Version Architecture Depends Description; do
        dpkg-deb --field "$DEB_FILE" "$field" | grep -q . \
            || die "The .deb control file has no $field field."
    done

    local parsed_version
    parsed_version="$(dpkg-deb --field "$DEB_FILE" Version)"
    [[ "$parsed_version" == "$DEB_VERSION" ]] \
        || die "The .deb reports version '$parsed_version', expected '$DEB_VERSION'."

    info "$(basename "$DEB_FILE"): $(printf '%s\n' "$contents" | wc -l | tr -d ' ') entries, root-owned, apphost executable"

    if command -v lintian >/dev/null 2>&1; then
        # Informational only. A self-contained bundle in /opt trips a number of
        # Debian policy tags by design, so lintian's verdict is not a gate.
        local lintian_out
        lintian_out="$(lintian --no-tag-display-limit --fail-on none "$DEB_FILE" 2>&1 | head -20 || true)"
        if [[ -n "$lintian_out" ]]; then
            info "lintian notes (informational):"
            printf '%s\n' "$lintian_out" | sed 's/^/        /'
        fi
    fi
}

# verify_rpm: inspect the built .rpm the same way.
verify_rpm() {
    [[ "$BUILD_RPM" == "true" ]] || return 0
    command -v rpm >/dev/null 2>&1 || { warn "rpm not available; skipping .rpm verification."; return 0; }

    # -v gives an ls-style long listing, so the same mode string checks used for
    # the .deb apply here rather than having to decode rpm's octal mode field.
    local listing
    listing="$(rpm -qpvl "$RPM_FILE" 2>/dev/null)" \
        || die "rpm could not read back the package it just built."

    printf '%s\n' "$listing" | grep -qE "^-rwxr-xr-x .* $INSTALL_PREFIX/$APP_NAME\$" \
        || die "$INSTALL_PREFIX/$APP_NAME is missing or not mode 0755 in the .rpm."

    # Group- or world-writable files, same reasoning and same character
    # positions as in verify_deb.
    local writable
    writable="$(printf '%s\n' "$listing" | grep -cE '^[-d](.{4}w|.{7}w)' || true)"
    [[ "$writable" -eq 0 ]] \
        || die "$writable file(s) in the .rpm are group- or world-writable."

    # The launcher must have survived as a symlink; rpm records link targets
    # separately from file contents, and a spec that listed it wrongly would
    # produce a regular file holding the target path as text.
    printf '%s\n' "$listing" | grep -q "^lrwxrwxrwx .* /usr/bin/$PACKAGE_NAME -> $INSTALL_PREFIX/$APP_NAME\$" \
        || die "/usr/bin/$PACKAGE_NAME is not a symlink to $INSTALL_PREFIX/$APP_NAME in the .rpm."

    local files
    files="$(rpm -qp --list "$RPM_FILE" 2>/dev/null)"
    printf '%s\n' "$files" | grep -qx "/usr/share/applications/$PACKAGE_NAME.desktop" \
        || die "The desktop entry is missing from the .rpm."

    local parsed_version
    parsed_version="$(rpm -qp --queryformat '%{VERSION}-%{RELEASE}' "$RPM_FILE" 2>/dev/null)"
    [[ "$parsed_version" == "$RPM_VERSION-$PACKAGE_RELEASE" ]] \
        || die "The .rpm reports version '$parsed_version', expected '$RPM_VERSION-$PACKAGE_RELEASE'."

    # AutoReqProv: no should have kept the bundled .so files out of Provides.
    # If that ever regresses, the package would claim to provide libSkiaSharp.so
    # system-wide and could be dragged in to satisfy an unrelated dependency.
    local provides
    provides="$(rpm -qp --provides "$RPM_FILE" 2>/dev/null | grep -c '\.so' || true)"
    [[ "$provides" -eq 0 ]] \
        || die "The .rpm advertises $provides bundled shared libraries in its Provides.
AutoReqProv must be off; check write_spec."

    info "$(basename "$RPM_FILE"): $(printf '%s\n' "$files" | wc -l | tr -d ' ') files, apphost 0755, no bundled Provides"
}

# ---------------------------------------------------------------------------
# --show-deps
# ---------------------------------------------------------------------------

# show_deps: report the external shared libraries the payload links against,
# and which installed package provides each.
#
# The point is to check the hand-maintained DEB_DEPENDS / RPM_REQUIRES lists
# against reality. Three things to understand when reading the output:
#
#   - It reports each binary's DIRECT dependencies (the DT_NEEDED entries),
#     not ldd's full transitive closure. That is what a dependency list should
#     declare: ldd on this payload also names expat, freetype, png, brotli and
#     uuid, but every one of those arrives through libfontconfig1, which
#     already depends on them. Declaring the closure adds noise that then has
#     to be maintained.
#   - Libraries the payload bundles itself are filtered out.
#   - Anything loaded with dlopen is invisible to both DT_NEEDED and ldd, and
#     that covers most of what actually matters here: .NET opens ICU and
#     OpenSSL that way, and Avalonia opens libX11, libICE and libSM that way.
#     None of the five will ever appear in this report, and all five must stay
#     in the dependency lists by hand.
show_deps() {
    step "Shared libraries the payload links against"

    [[ -d "$PAYLOAD_DIR" ]] || die "No staged payload at $PAYLOAD_DIR. Run a build first."

    local reader=""
    if command -v readelf >/dev/null 2>&1; then
        reader="readelf"
    elif command -v objdump >/dev/null 2>&1; then
        reader="objdump"
    else
        warn "Neither readelf nor objdump is installed (both are in binutils),"
        warn "so this falls back to ldd, which reports transitive dependencies too."
    fi

    local bundled needed file
    bundled="$(find "$PAYLOAD_DIR" -name '*.so*' -type f -printf '%f\n' | sort -u)"

    needed=""
    while IFS= read -r -d '' file; do
        is_elf "$file" || continue
        # Every branch ends in "|| true" deliberately. All three tools exit
        # non-zero on a static or otherwise non-dynamic ELF, and because this
        # script runs with pipefail that status propagates through the pipe to
        # the assignment, where set -e would abort the whole report.
        case "$reader" in
            readelf)
                needed+="$(readelf -d "$file" 2>/dev/null | sed -n 's/.*NEEDED.*\[\(.*\)\]/\1/p' || true)"$'\n' ;;
            objdump)
                needed+="$(objdump -p "$file" 2>/dev/null | sed -n 's/^[[:space:]]*NEEDED[[:space:]]*\(.*\)$/\1/p' || true)"$'\n' ;;
            *)
                needed+="$(ldd "$file" 2>/dev/null | sed -n 's/^[[:space:]]*\([^ ]*\.so[^ ]*\).*/\1/p' || true)"$'\n' ;;
        esac
    done < <(find "$PAYLOAD_DIR" -type f -print0)

    local lib provider missing=0
    while IFS= read -r lib; do
        [[ -n "$lib" ]] || continue
        # Skip anything the payload ships itself.
        #
        # Written as a full if rather than "grep -q ... && continue": under
        # set -e that && list returns non-zero for every library that is NOT
        # bundled -- which is the common case -- and aborts the script on the
        # first one, so the report came out empty.
        if printf '%s\n' "$bundled" | grep -qx "$lib"; then
            continue
        fi

        # Same pipefail reasoning as above: both queries exit non-zero when
        # nothing provides the library, which is exactly the case worth
        # reporting rather than aborting on.
        provider=""
        if command -v dpkg-query >/dev/null 2>&1; then
            provider="$(dpkg-query -S "*/$lib" 2>/dev/null | head -1 | cut -d: -f1 || true)"
        elif command -v rpm >/dev/null 2>&1; then
            provider="$(rpm -q --whatprovides "$lib" 2>/dev/null | head -1 || true)"
        fi

        if [[ -z "$provider" ]]; then
            provider="(no installed package provides it)"
            missing=$((missing + 1))
        fi
        printf '    %-42s %s\n' "$lib" "$provider"
    done < <(printf '%s' "$needed" | sort -u)

    printf '\n'
    info "Loaded with dlopen, so absent above and hand-maintained in config.sh:"
    info "  ICU + OpenSSL (.NET globalization and TLS)"
    info "  libX11, libICE, libSM (Avalonia's X11 backend)"
    printf '\n'
    info "Also absent by design: libraries reached only through one already"
    info "listed above. libfontconfig1 pulls in expat, freetype, png, brotli"
    info "and uuid on its own, so none of those need declaring."

    if [[ "$missing" -gt 0 ]]; then
        printf '\n'
        warn "$missing entries have no providing package. Expected ones are the dynamic"
        warn "loader and linux-vdso (kernel-supplied, not files), and liblttng-ust"
        warn "(optional .NET tracing; the runtime works without it)."
    fi
}

# ---------------------------------------------------------------------------
# Run
# ---------------------------------------------------------------------------

check_prerequisites
choose_build_dir
read_version
resolve_architecture

PAYLOAD_DIR="$BUILD_DIR/payload"
TREE_DIR="$BUILD_DIR/tree"

DEB_VERSION="$UPSTREAM_VERSION-$PACKAGE_RELEASE"
RPM_VERSION="$UPSTREAM_VERSION"

# The desktop entry's MimeType line is either present or absent entirely; an
# empty MimeType= would make some validators complain.
if [[ "$REGISTER_MIME_TYPE" == "true" ]]; then
    MIME_TYPE_LINE="MimeType=$MIME_TYPE;"
else
    MIME_TYPE_LINE=""
fi

info "Purple Pen $FULL_VERSION ($VERSION_LABEL) -> $UPSTREAM_VERSION"
info "Target $RUNTIME_IDENTIFIER: Debian $DEB_ARCH, RPM $RPM_ARCH"
info "Build directory $BUILD_DIR"

mkdir -p "$OUTPUT_DIR"

if [[ "$SHOW_DEPS_ONLY" == "1" ]]; then
    if [[ ! -d "$PAYLOAD_DIR" ]]; then
        publish_app
        stage_payload
        stage_pdf_converter
        normalize_payload_permissions
    fi
    show_deps
    exit 0
fi

publish_app
stage_payload
stage_pdf_converter
normalize_payload_permissions
build_install_tree
build_deb
build_rpm

if [[ "$SKIP_VERIFY" == "0" ]]; then
    step "Verifying the packages"
    verify_deb
    verify_rpm
else
    warn "Package verification skipped."
fi

step "Done"
printf '%s' "$C_OK"
ls -1 "$OUTPUT_DIR" 2>/dev/null | sed "s|^|    $OUTPUT_SUBDIR/|"
printf '%s\n' "$C_OFF"
