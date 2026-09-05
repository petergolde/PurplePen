#!/bin/bash
#
# publish-linux-repos.sh
#
# Takes the .deb and .rpm packages built by build-linux-packages.sh and files
# them into a signed apt repository and a signed dnf/yum repository, so that
# Linux users can "apt install purplepen" or "dnf install purplepen" and get
# updates automatically.
#
#     ./publish-linux-repos.sh ~/ppdownload /mnt/e/PurplePenSigning
#
# The first argument is a publishing directory laid out like this. Everything
# under root/ is what gets uploaded to the web site; everything under data/ is
# state that must survive between runs but must NOT be published.
#
#     <repository-dir>/
#     |-- root/
#     |   `-- linux/
#     |       |-- purplepen-archive-keyring.asc   public key, for apt and dnf
#     |       |-- purplepen-archive-keyring.gpg   same key, dearmored
#     |       |-- README.md                       generated install instructions
#     |       |-- deb/
#     |       |   |-- pool/<channel>/main/p/purplepen/*.deb
#     |       |   `-- dists/<channel>/...         indexes and signatures
#     |       `-- rpm/
#     |           |-- purplepen.repo              generated dnf configuration
#     |           `-- <channel>/<arch>/*.rpm + repodata/
#     `-- data/
#         |-- README.md
#         |-- apt-ftparchive-<channel>.db         index cache
#         `-- publish.log
#
# The second argument is the directory holding the GPG signing key -- normally
# a removable drive. Nothing is ever written there, and the secret key is never
# copied into the publishing directory.
#
# The pipeline is:
#
#   1. Work out which packages exist and which channel each belongs to
#   2. Import the signing key into a throwaway keyring and take the passphrase
#   3. Publish the public key users need in order to verify any of this
#   4. File the .debs into the pool, rebuild the indexes, sign them
#   5. File the .rpms, sign each one, rebuild the metadata, sign that
#   6. Regenerate the install instructions from the live configuration
#   7. Verify every signature and checksum, and fail on anything wrong
#
# Uploading is deliberately not in scope. This script only prepares the
# directory; getting root/ onto the web site is a separate step.
#
# ---------------------------------------------------------------------------
# Prerequisites
# ---------------------------------------------------------------------------
#
# On Debian or Ubuntu, including WSL, everything needed is one command:
#
#     sudo apt install apt-utils gnupg createrepo-c rpm xz-utils
#
# On Fedora or RHEL the same tools are:
#
#     sudo dnf install apt gnupg2 createrepo_c rpm-sign xz
#
# What each is for:
#
#     apt-ftparchive   apt-utils    Generates the Packages and Release indexes
#                                   that make a directory of .debs an apt
#                                   repository.
#     gpg              gnupg        Signs the apt Release file and the dnf
#                                   repository metadata.
#     createrepo_c     createrepo-c Generates the repodata/ directory that makes
#                                   a directory of .rpms a dnf repository.
#     rpmsign          rpm          Signs each .rpm in place. Unlike Debian,
#                                   RPM signatures live inside the package.
#     dpkg-deb         dpkg         Reads the control fields back out of a .deb.
#     gzip, xz         base, xz-utils  Compress the Packages index.
#
# No pinentry program is required. The passphrase is read by this script and
# handed to gpg through a loopback pinentry, which matters because WSL often
# has no working pinentry at all.
#
# See README.md for the wider picture and config.sh for every setting.
#

set -euo pipefail

# Files written into the publishing directory end up on a public web server, so
# they must be world-readable and must not be world-writable. Forcing the umask
# here means every mkdir and every redirect below gets 0755 / 0644 regardless of
# how the invoking shell was configured.
umask 022

# ---------------------------------------------------------------------------
# Locate ourselves and load configuration
# ---------------------------------------------------------------------------

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# shellcheck source=config.sh
source "$SCRIPT_DIR/config.sh"

# ---------------------------------------------------------------------------
# Output helpers
# ---------------------------------------------------------------------------
#
# Deliberately identical to build-linux-packages.sh. They are duplicated rather
# than factored into a shared file because twenty lines of printf are not worth
# adding a dependency between two scripts that are otherwise independent.

if [[ -t 1 ]]; then
    C_STEP=$'\033[1;35m'; C_INFO=$'\033[0;36m'; C_WARN=$'\033[1;33m'
    C_ERR=$'\033[1;31m';  C_OK=$'\033[1;32m';   C_OFF=$'\033[0m'
else
    C_STEP=""; C_INFO=""; C_WARN=""; C_ERR=""; C_OK=""; C_OFF=""
fi

# step: announce a major phase.
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

REPO_DIR=""
KEY_DIR=""
PACKAGES_DIR="$SCRIPT_DIR/$OUTPUT_SUBDIR"
FORCE_CHANNEL=""
PUBLISH_DEB=true
PUBLISH_RPM=true
DO_SIGN=1
DRY_RUN=0

# usage: print command line help.
usage() {
    cat <<'EOF'
Usage: publish-linux-repos.sh [options] <repository-dir> <signing-key-dir>

Publishes the packages in output/ into an apt repository and a dnf repository
under <repository-dir>, signing both with the GPG key in <signing-key-dir>.

Arguments:
  <repository-dir>    Publishing directory, e.g. ~/ppdownload. Created if it
                      does not exist. Its root/ subdirectory is what gets
                      uploaded to the web site; data/ must not be.
  <signing-key-dir>   Directory holding the GPG signing key, e.g. the USB drive
                      at /mnt/e/PurplePenSigning. Never written to. Not required
                      with --no-sign.

Options:
  --packages-dir DIR  Where to find the .deb and .rpm files.
                      Default: the output/ directory beside this script.
  --channel NAME      Publish everything to this channel instead of choosing
                      per package. Normally the channel follows from the
                      version: a prerelease such as 4.0.0~beta1 goes to beta,
                      anything else to stable.
  --deb-only          Publish only the apt repository.
  --rpm-only          Publish only the dnf repository.
  --no-sign           Do not sign anything. For testing the layout only -- apt
                      and dnf both refuse an unsigned repository by default, so
                      what this produces is not usable.
  --dry-run           Report what would be published and where, then stop
                      without writing anything or asking for the passphrase.
  -h, --help          Show this message.

The passphrase for the signing key is prompted for once per run. Set
SIGNING_PASSPHRASE_FILE to the path of a file containing it to run unattended.

Configuration lives in config.sh; every setting there can also be given as an
environment variable. See README.md for prerequisites.
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --packages-dir) [[ $# -ge 2 ]] || die "--packages-dir needs a directory."
                        PACKAGES_DIR="$2"; shift ;;
        --channel)      [[ $# -ge 2 ]] || die "--channel needs a name."
                        FORCE_CHANNEL="$2"; shift ;;
        --deb-only)     PUBLISH_DEB=true;  PUBLISH_RPM=false ;;
        --rpm-only)     PUBLISH_DEB=false; PUBLISH_RPM=true ;;
        --no-sign)      DO_SIGN=0 ;;
        --dry-run)      DRY_RUN=1 ;;
        -h|--help)      usage; exit 0 ;;
        -*)             usage >&2; die "Unknown option: $1" ;;
        *)              if   [[ -z "$REPO_DIR" ]]; then REPO_DIR="$1"
                        elif [[ -z "$KEY_DIR"  ]]; then KEY_DIR="$1"
                        else usage >&2; die "Unexpected argument: $1"
                        fi ;;
    esac
    shift
done

[[ -n "$REPO_DIR" ]] || { usage >&2; die "No repository directory given."; }

if [[ "$DO_SIGN" == "1" && -z "$KEY_DIR" ]]; then
    usage >&2
    die "No signing key directory given. Pass one, or use --no-sign to build an
unsigned repository for testing."
fi

if [[ -n "$FORCE_CHANNEL" && "$FORCE_CHANNEL" != "$STABLE_CHANNEL" && "$FORCE_CHANNEL" != "$BETA_CHANNEL" ]]; then
    die "--channel must be '$STABLE_CHANNEL' or '$BETA_CHANNEL', not '$FORCE_CHANNEL'."
fi

# ---------------------------------------------------------------------------
# Derived paths
# ---------------------------------------------------------------------------

PUBLISH_ROOT="$REPO_DIR/$PUBLISH_ROOT_SUBDIR"
PUBLISH_DATA="$REPO_DIR/$PUBLISH_DATA_SUBDIR"
LINUX_DIR="$PUBLISH_ROOT/$PUBLISH_LINUX_SUBDIR"
DEB_ROOT="$PUBLISH_ROOT/$DEB_REPO_SUBDIR"
RPM_ROOT="$PUBLISH_ROOT/$RPM_REPO_SUBDIR"

KEYRING_ASC="$LINUX_DIR/$KEYRING_BASENAME.asc"
KEYRING_GPG="$LINUX_DIR/$KEYRING_BASENAME.gpg"

# URLs the generated .repo file and instructions point at. These must agree with
# where the directories above end up once root/ has been uploaded.
LINUX_URL="$PUBLISH_BASE_URL/$PUBLISH_LINUX_SUBDIR"
DEB_URL="$PUBLISH_BASE_URL/$DEB_REPO_SUBDIR"
RPM_URL="$PUBLISH_BASE_URL/$RPM_REPO_SUBDIR"
KEYRING_URL="$LINUX_URL/$KEYRING_BASENAME.asc"

# ---------------------------------------------------------------------------
# Preflight checks
# ---------------------------------------------------------------------------

# require_tool: fail with an install hint unless command $1 exists. $2 is the
# Debian package providing it, $3 the Fedora one.
require_tool() {
    local tool="$1" deb_pkg="$2" rpm_pkg="$3"

    command -v "$tool" >/dev/null 2>&1 && return 0

    die "Required tool not found: $tool

Install it with one of:

    sudo apt install $deb_pkg      # Debian / Ubuntu / WSL
    sudo dnf install $rpm_pkg      # Fedora / RHEL"
}

# check_prerequisites: verify the platform, the tools, and the two directories
# given on the command line, before anything slow or destructive runs.
check_prerequisites() {
    step "Checking prerequisites"

    [[ "$(uname -s)" == "Linux" ]] \
        || die "This script must be run on Linux. On Windows, run it from WSL:

    wsl bash $SCRIPT_DIR/$(basename "${BASH_SOURCE[0]}") <repository-dir> <key-dir>"

    local tool
    for tool in find sed awk sort sha256sum; do
        command -v "$tool" >/dev/null 2>&1 || die "Required tool not found: $tool"
    done

    if [[ "$PUBLISH_DEB" == "true" ]]; then
        require_tool apt-ftparchive apt-utils apt
        require_tool dpkg-deb       dpkg     dpkg
        require_tool gzip           gzip     gzip
        [[ "$DEB_INDEX_XZ" != "true" ]] || require_tool xz xz-utils xz
    fi

    if [[ "$PUBLISH_RPM" == "true" ]]; then
        require_tool createrepo_c createrepo-c createrepo_c
        require_tool rpm          rpm          rpm
        [[ "$DO_SIGN" == "0" ]] || require_tool rpmsign rpm rpm-sign
    fi

    [[ "$DO_SIGN" == "0" ]] || require_tool gpg gnupg gnupg2

    [[ -d "$PACKAGES_DIR" ]] \
        || die "No package directory at $PACKAGES_DIR

Run ./build-linux-packages.sh first, or point --packages-dir somewhere else."

    if [[ "$DO_SIGN" == "1" ]]; then
        [[ -d "$KEY_DIR" ]] \
            || die "No signing key directory at $KEY_DIR

If the key is on removable media, check that it is mounted. Under WSL a USB
drive shows up as /mnt/<letter> only after Windows has mounted it."

        local key_file
        for key_file in "$SIGNING_SUBKEY_FILE" "$SIGNING_PUBKEY_FILE"; do
            [[ -f "$KEY_DIR/$key_file" ]] \
                || die "Cannot find $key_file in $KEY_DIR

Expected the signing key directory to contain:
    $SIGNING_SUBKEY_FILE   the exported secret signing subkey
    $SIGNING_PUBKEY_FILE   the matching public key"
        done
    else
        warn "Signing disabled. The repositories this produces are NOT usable:"
        warn "apt and dnf both reject unsigned repositories by default."
    fi

    # The parent has to exist; the publishing directory itself does not, and is
    # created below. Requiring the parent catches a mistyped path rather than
    # silently building a repository somewhere unexpected.
    local parent
    parent="$(dirname "$REPO_DIR")"
    [[ -d "$parent" ]] || die "Cannot create $REPO_DIR because $parent does not exist."
}

# ---------------------------------------------------------------------------
# Work out what there is to publish
# ---------------------------------------------------------------------------
#
# The packages are interrogated rather than reconstructed from VersionNumber.cs.
# That way whatever is actually sitting in output/ gets published, including
# packages built earlier, built on another machine, or built for a second
# architecture -- and the version recorded in the index can never disagree with
# the version inside the package.

# Parallel arrays: file, package name, version, architecture, channel.
DEB_FILES=(); DEB_NAMES=(); DEB_VERSIONS=(); DEB_ARCHES=(); DEB_CHANNELS=()
RPM_FILES=(); RPM_NAMES=(); RPM_VERSIONS=(); RPM_ARCHES=(); RPM_CHANNELS=()

# channel_for_version: print the channel a package with version $1 belongs to.
#
# read_version in build-linux-packages.sh folds the release stage into the
# upstream version with a tilde -- "4.0.0~beta1", "4.0.0~rc1" -- because that is
# how both dpkg and rpm spell "sorts before the release". So the presence of a
# tilde is exactly the question "is this a prerelease?", and no separate list of
# stage names is needed here.
channel_for_version() {
    if [[ -n "$FORCE_CHANNEL" ]]; then
        printf '%s' "$FORCE_CHANNEL"
    elif [[ "$1" == *"~"* ]]; then
        printf '%s' "$BETA_CHANNEL"
    else
        printf '%s' "$STABLE_CHANNEL"
    fi
}

# discover_packages: fill the arrays above from PACKAGES_DIR.
discover_packages() {
    step "Finding packages in $PACKAGES_DIR"

    local file name version arch release

    if [[ "$PUBLISH_DEB" == "true" ]]; then
        while IFS= read -r file; do
            [[ -n "$file" ]] || continue
            name="$(dpkg-deb --field "$file" Package)"
            version="$(dpkg-deb --field "$file" Version)"
            arch="$(dpkg-deb --field "$file" Architecture)"

            [[ -n "$name" && -n "$version" && -n "$arch" ]] \
                || die "$(basename "$file") has no Package/Version/Architecture field; it is not a valid .deb."

            DEB_FILES+=("$file"); DEB_NAMES+=("$name")
            DEB_VERSIONS+=("$version"); DEB_ARCHES+=("$arch")
            DEB_CHANNELS+=("$(channel_for_version "$version")")
        done < <(find "$PACKAGES_DIR" -maxdepth 1 -type f -name '*.deb' | sort)
    fi

    if [[ "$PUBLISH_RPM" == "true" ]]; then
        while IFS= read -r file; do
            [[ -n "$file" ]] || continue
            # --nosignature stops rpm complaining that it cannot verify a
            # signature by a key that is not in the local rpmdb, which is the
            # normal state of affairs before the package has been signed.
            read -r name version release arch < <(
                rpm -qp --nosignature \
                    --queryformat '%{NAME} %{VERSION} %{RELEASE} %{ARCH}\n' \
                    "$file" 2>/dev/null
            ) || die "rpm could not read $(basename "$file")."

            [[ -n "$name" && -n "$version" && -n "$arch" ]] \
                || die "$(basename "$file") is not a valid .rpm."

            RPM_FILES+=("$file"); RPM_NAMES+=("$name")
            RPM_VERSIONS+=("$version-$release"); RPM_ARCHES+=("$arch")
            RPM_CHANNELS+=("$(channel_for_version "$version")")
        done < <(find "$PACKAGES_DIR" -maxdepth 1 -type f -name '*.rpm' | sort)
    fi

    local total=$(( ${#DEB_FILES[@]} + ${#RPM_FILES[@]} ))
    [[ "$total" -gt 0 ]] \
        || die "No .deb or .rpm files found in $PACKAGES_DIR

Run ./build-linux-packages.sh first, or point --packages-dir somewhere else."

    local i
    for ((i = 0; i < ${#DEB_FILES[@]}; i++)); do
        info "$(printf '%-6s %-10s %-18s %-8s -> %s' \
            deb "${DEB_NAMES[i]}" "${DEB_VERSIONS[i]}" "${DEB_ARCHES[i]}" "${DEB_CHANNELS[i]}")"
    done
    for ((i = 0; i < ${#RPM_FILES[@]}; i++)); do
        info "$(printf '%-6s %-10s %-18s %-8s -> %s' \
            rpm "${RPM_NAMES[i]}" "${RPM_VERSIONS[i]}" "${RPM_ARCHES[i]}" "${RPM_CHANNELS[i]}")"
    done

    if [[ -n "$FORCE_CHANNEL" ]]; then
        warn "--channel $FORCE_CHANNEL overrides the channel each version would normally go to."
    fi
}

# ---------------------------------------------------------------------------
# Signing key
# ---------------------------------------------------------------------------
#
# The key is imported into a throwaway keyring that is destroyed when the script
# exits, so the secret never touches the user's own GnuPG configuration and is
# never left behind in the publishing directory.

GNUPG_TMP=""
GPG_SIGN_OPTS=()
RPM_SIGN_DEFINES=()
PASSPHRASE_FILE=""

# Every key id that a signature of ours may legitimately carry, lower case, one
# per line. See collect_signing_key_ids.
SIGNING_KEY_IDS=""

# cleanup: destroy the throwaway keyring. Runs on every exit path, including
# die.
cleanup() {
    [[ -n "$GNUPG_TMP" && -d "$GNUPG_TMP" ]] || return 0

    # The agent holds the passphrase in its own memory, so it has to go first --
    # and it keeps a socket open in the directory that would otherwise stop the
    # removal.
    GNUPGHOME="$GNUPG_TMP" gpgconf --kill all >/dev/null 2>&1 || true

    if command -v shred >/dev/null 2>&1; then
        find "$GNUPG_TMP" -type f -exec shred -u {} + >/dev/null 2>&1 || true
    fi
    rm -rf "$GNUPG_TMP"
}
trap cleanup EXIT

# setup_signing_key: build the throwaway keyring, import the key, check it is
# the key we expect, and take the passphrase.
setup_signing_key() {
    step "Preparing the signing key"

    if [[ "$DO_SIGN" == "0" ]]; then
        info "Skipped -- signing is disabled."
        return
    fi

    # The keyring must be mode 0700 and it must be on a filesystem that can
    # actually store that. Neither the USB drive nor anything under /mnt/<drive>
    # in WSL can: drvfs reports every file as 0777, and gpg refuses to use a
    # home directory with unsafe permissions. $TMPDIR is a native filesystem.
    GNUPG_TMP="$(mktemp -d "${TMPDIR:-/tmp}/purplepen-gnupg.XXXXXX")" \
        || die "Could not create a temporary keyring directory."
    chmod 0700 "$GNUPG_TMP"
    export GNUPGHOME="$GNUPG_TMP"

    # Loopback pinentry means gpg takes the passphrase from a file descriptor
    # instead of popping up a dialog. It has been the default since GnuPG
    # 2.1.13, but saying so explicitly costs nothing and makes the script work
    # on older versions too.
    printf 'allow-loopback-pinentry\n' > "$GNUPG_TMP/gpg-agent.conf"

    gpg --batch --quiet --import "$KEY_DIR/$SIGNING_PUBKEY_FILE" \
        || die "Could not import $SIGNING_PUBKEY_FILE from $KEY_DIR."
    gpg --batch --quiet --import "$KEY_DIR/$SIGNING_SUBKEY_FILE" \
        || die "Could not import $SIGNING_SUBKEY_FILE from $KEY_DIR."

    # Check we imported the key we meant to. Pointing at the wrong drive, or at
    # a key belonging to someone else, would otherwise produce a repository that
    # every subscriber rejects -- and the failure would surface on users'
    # machines rather than here.
    local imported
    imported="$(gpg --batch --with-colons --list-keys 2>/dev/null \
        | awk -F: '$1 == "fpr" { print $10; exit }')"

    [[ "$imported" == "$SIGNING_KEY_FINGERPRINT" ]] \
        || die "The key in $KEY_DIR is not the expected signing key.

    expected  $SIGNING_KEY_FINGERPRINT
    imported  ${imported:-none}

Either the wrong directory was given, or SIGNING_KEY_FINGERPRINT in config.sh
is out of date."

    # Trust the key ultimately inside this throwaway keyring, so that the
    # verification step at the end reports a clean result instead of burying the
    # answer under a "this key is not certified" warning.
    printf '%s:6:\n' "$SIGNING_KEY_FINGERPRINT" \
        | gpg --batch --quiet --import-ownertrust 2>/dev/null || true

    collect_signing_key_ids

    # The passphrase. Held in a 0600 file inside the 0700 keyring directory, and
    # shredded on exit.
    #
    # A file rather than an interactive prompt at each signature because rpmsign
    # runs gpg with --pinentry-mode=error: it will not prompt, and a protected
    # key would simply fail. Priming the agent's cache first is possible, but
    # depends on the cache timeout and on a working pinentry program, which WSL
    # frequently does not have. This way the passphrase is asked for exactly
    # once and everything afterwards is deterministic.
    PASSPHRASE_FILE="$GNUPG_TMP/passphrase"
    umask 077
    if [[ -n "${SIGNING_PASSPHRASE_FILE:-}" ]]; then
        [[ -f "$SIGNING_PASSPHRASE_FILE" ]] \
            || die "SIGNING_PASSPHRASE_FILE is set but $SIGNING_PASSPHRASE_FILE does not exist."
        head -1 "$SIGNING_PASSPHRASE_FILE" | tr -d '\r\n' > "$PASSPHRASE_FILE"
        info "Passphrase read from $SIGNING_PASSPHRASE_FILE"
    else
        [[ -t 0 ]] \
            || die "No terminal to read the passphrase from.

Set SIGNING_PASSPHRASE_FILE to a file containing it to run unattended."
        local passphrase
        printf '%s' "${C_INFO}    Passphrase for signing key ${SIGNING_KEY_FINGERPRINT: -16}: ${C_OFF}"
        read -rs passphrase
        printf '\n'
        [[ -n "$passphrase" ]] || die "No passphrase given."
        printf '%s' "$passphrase" > "$PASSPHRASE_FILE"
        unset passphrase
    fi
    umask 022

    GPG_SIGN_OPTS=(
        --batch --yes --no-tty --quiet
        --pinentry-mode loopback
        --passphrase-file "$PASSPHRASE_FILE"
        --local-user "$SIGNING_KEY_FINGERPRINT"
        # SHA-1 signatures are rejected outright by current apt and by Fedora's
        # crypto policy, and gpg's own default digest depends on the key. Being
        # explicit removes the question.
        --digest-algo SHA256
    )

    # Prove the passphrase is right now, rather than after several minutes of
    # copying packages around.
    printf 'passphrase check\n' | gpg "${GPG_SIGN_OPTS[@]}" --detach-sign -o /dev/null 2>/dev/null \
        || die "The signing key did not accept that passphrase."

    info "Signing with $SIGNING_KEY_FINGERPRINT"

    detect_rpm_sign_defines
}

# collect_signing_key_ids: record every key id a signature of ours may carry.
#
# RPM identifies the signer by key id, and the key that actually signs is the
# signing SUBKEY -- whose id is nothing like the primary key's fingerprint. So
# "is this package signed with our key?" cannot be answered by comparing against
# SIGNING_KEY_FINGERPRINT; it has to be asked of the keyring.
#
# Field 5 of gpg's colon output is the long key id and field 12 the
# capabilities, so this picks out the primary and every subkey that can sign.
collect_signing_key_ids() {
    SIGNING_KEY_IDS="$(gpg --batch --with-colons --list-keys "$SIGNING_KEY_FINGERPRINT" 2>/dev/null \
        | awk -F: '($1 == "pub" || $1 == "sub") && $12 ~ /s/ { print tolower($5) }')"

    [[ -n "$SIGNING_KEY_IDS" ]] \
        || die "The key $SIGNING_KEY_FINGERPRINT has no signing-capable key in it.

An exported signing subkey should carry the subkey itself, not only a stub of
the primary key. Re-export it with:

    gpg --armor --export-secret-subkeys $SIGNING_KEY_FINGERPRINT"
}

# is_our_signing_key: succeed if key id $1 is one of ours.
is_our_signing_key() {
    local id
    id="$(printf '%s' "$1" | tr '[:upper:]' '[:lower:]')"
    [[ -n "$id" ]] || return 1
    printf '%s\n' "$SIGNING_KEY_IDS" | grep -qx "$id"
}

# detect_rpm_sign_defines: work out how to get a passphrase into rpmsign, and
# record the rpm --define arguments that do it.
#
# rpmsign does not sign anything itself; it expands the %__gpg_sign_cmd macro
# and runs the result. That default command supplies no passphrase at all, and
# on rpm 4.18 it additionally passes --pinentry-mode=error, meaning gpg will not
# even prompt. Either way a protected key fails unless something is injected.
#
# _gpg_sign_cmd_extra_args is the supported injection point, added in rpm 4.15
# and present on everything current -- Ubuntu 22.04 ships 4.17, Debian 12 ships
# 4.18. Rather than assume, probe for it with a sentinel: the macro expands to
# nothing when undefined, so defining it and looking for the value in the
# expansion answers the question exactly.
detect_rpm_sign_defines() {
    [[ "$PUBLISH_RPM" == "true" && "$DO_SIGN" == "1" ]] || return 0

    # Point rpm at the gpg that actually exists.
    #
    # Not paranoia: Ubuntu's rpm package ships %__gpg hardcoded to
    # /usr/bin/gpg2, and Ubuntu's gnupg package installs only /usr/bin/gpg. Out
    # of the box, rpmsign on Ubuntu 22.04 therefore fails with "Could not exec
    # gpg: No such file or directory" -- and it says that after prompting for
    # everything else, so it looks like a signing problem rather than a missing
    # file. Resolving the path here sidesteps whatever each distribution assumed.
    local gpg_path
    gpg_path="$(command -v gpg)"

    RPM_SIGN_DEFINES=(
        --define "__gpg $gpg_path"
        --define "_gpg_name $SIGNING_KEY_FINGERPRINT"
        --define "_gpg_digest_algo $RPM_DIGEST_ALGO"
    )

    if rpm --define "_gpg_sign_cmd_extra_args PURPLEPEN_PROBE" \
           --eval '%__gpg_sign_cmd' 2>/dev/null | grep -q PURPLEPEN_PROBE; then
        RPM_SIGN_DEFINES+=(
            --define "_gpg_sign_cmd_extra_args --pinentry-mode loopback --passphrase-file $PASSPHRASE_FILE"
        )
        return 0
    fi

    # Older rpm: replace the whole command.
    #
    # The shape is rpm's, not a mistake. rpm splits the expansion into words,
    # executes the first as the program and passes the rest as the argument
    # vector -- so the path comes first and the bare "gpg" that follows becomes
    # argv[0]. Dropping either one breaks signing in a thoroughly confusing way.
    warn "This rpm does not support _gpg_sign_cmd_extra_args; overriding its signing command."
    RPM_SIGN_DEFINES+=(
        --define "__gpg_sign_cmd %{__gpg} gpg --batch --no-verbose --no-armor --no-secmem-warning --pinentry-mode loopback --passphrase-file $PASSPHRASE_FILE --digest-algo $RPM_DIGEST_ALGO -u %{_gpg_name} -sbo %{__signature_filename} %{__plaintext_filename}"
    )
}

# gpg_sign_detached: write an armored detached signature of file $1 to $2.
gpg_sign_detached() {
    local source="$1" signature="$2"

    [[ "$DO_SIGN" == "1" ]] || return 0

    rm -f "$signature"
    gpg "${GPG_SIGN_OPTS[@]}" --detach-sign --armor -o "$signature" "$source" \
        || die "Could not sign $(basename "$source")."
    chmod 0644 "$signature"
}

# gpg_sign_inline: write a clear-signed copy of file $1 to $2.
gpg_sign_inline() {
    local source="$1" signature="$2"

    [[ "$DO_SIGN" == "1" ]] || return 0

    rm -f "$signature"
    gpg "${GPG_SIGN_OPTS[@]}" --clearsign -o "$signature" "$source" \
        || die "Could not clear-sign $(basename "$source")."
    chmod 0644 "$signature"
}

# ---------------------------------------------------------------------------
# Directory layout and the published public key
# ---------------------------------------------------------------------------

# ensure_layout: create the publishing directories and the note explaining what
# data/ is.
ensure_layout() {
    step "Preparing $REPO_DIR"

    mkdir -p "$LINUX_DIR" "$PUBLISH_DATA"
    [[ "$PUBLISH_DEB" != "true" ]] || mkdir -p "$DEB_ROOT"
    [[ "$PUBLISH_RPM" != "true" ]] || mkdir -p "$RPM_ROOT"

    cat > "$PUBLISH_DATA/README.md" <<EOF
# Do not publish this directory

Everything in here is state belonging to \`publish-linux-repos.sh\`. It has to
survive between runs, but it must **not** be uploaded to the web site -- only
\`../$PUBLISH_ROOT_SUBDIR/\` goes there.

| File | Purpose |
|---|---|
| \`apt-ftparchive-*.db\` | Cache of each pooled package's size and checksums, so rebuilding the index does not re-hash every package that has not changed. Safe to delete; the next run is just slower. |
| \`publish.log\` | Append-only record of what was published, when, and with which checksum. |

Nothing here is secret, and nothing here is irreplaceable -- the repository can
be rebuilt from the packages in \`../$PUBLISH_ROOT_SUBDIR/\` alone. The signing
key is not here and never is.
EOF

    info "root/ (published) and $PUBLISH_DATA_SUBDIR/ (not published) are ready"
}

# publish_public_key: put the public key where users can fetch it.
#
# Both forms are published because apt changed what it accepts. apt 2.4 and
# later (Debian 12, Ubuntu 22.04) read the armored .asc directly in a signed-by=
# option; older apt needs the dearmored binary form. dnf takes the .asc.
publish_public_key() {
    step "Publishing the public key"

    if [[ "$DO_SIGN" == "0" ]]; then
        info "Skipped -- signing is disabled."
        return
    fi

    install -m 0644 "$KEY_DIR/$SIGNING_PUBKEY_FILE" "$KEYRING_ASC"

    rm -f "$KEYRING_GPG"
    gpg --batch --yes --dearmor -o "$KEYRING_GPG" "$KEYRING_ASC" \
        || die "Could not dearmor the public key."
    chmod 0644 "$KEYRING_GPG"

    info "$KEYRING_BASENAME.asc and .gpg -> $LINUX_DIR"
}

# ---------------------------------------------------------------------------
# The apt repository
# ---------------------------------------------------------------------------
#
# An apt repository is a directory of .deb files plus generated indexes:
#
#   pool/<channel>/main/p/purplepen/*.deb    the packages themselves
#   dists/<channel>/main/binary-<arch>/Packages
#                                            every package's control fields,
#                                            plus its size, SHA256 and path
#   dists/<channel>/Release                  checksums of those index files
#   dists/<channel>/InRelease                Release, clear-signed
#   dists/<channel>/Release.gpg              detached signature of Release
#
# That single signature over Release is the whole trust chain: signature ->
# Release -> Packages checksums -> .deb checksums. The .deb files themselves are
# NOT signed, and apt does not look for a signature on them.
#
# The pool is split per channel rather than shared, which is a small departure
# from Debian's own layout. apt-ftparchive indexes everything underneath the
# directory it is given, so a shared pool would put every package in every
# suite. Paths in Packages only have to resolve relative to the repository root,
# so this is legal -- and the usual reason for a shared pool, storing a file
# once when several suites carry it, never arises here because a beta and a
# release are always different files.

# deb_pool_dir: print the pool directory, relative to the repository root, for
# channel $1 and package name $2.
deb_pool_dir() {
    local channel="$1" name="$2"
    printf 'pool/%s/%s/%s/%s' "$channel" "$DEB_COMPONENT" "${name:0:1}" "$name"
}

# stage_debs: copy each .deb into the pool.
stage_debs() {
    local i channel name pool

    for ((i = 0; i < ${#DEB_FILES[@]}; i++)); do
        channel="${DEB_CHANNELS[i]}"
        name="${DEB_NAMES[i]}"
        pool="$DEB_ROOT/$(deb_pool_dir "$channel" "$name")"

        mkdir -p "$pool"

        # install rather than cp, for the mode. The packages are normally read
        # from a Windows drive mounted into WSL, where every file reports as
        # mode 0777; a plain cp would carry that onto the web server.
        install -m 0644 "${DEB_FILES[i]}" "$pool/"

        info "$(basename "${DEB_FILES[i]}") -> $(deb_pool_dir "$channel" "$name")/"
    done
}

# deb_channels_present: print the channels that have a pool directory.
deb_channels_present() {
    local channel
    for channel in "$STABLE_CHANNEL" "$BETA_CHANNEL"; do
        [[ -d "$DEB_ROOT/pool/$channel" ]] && printf '%s\n' "$channel"
    done
    return 0
}

# deb_arches_in_channel: print the architectures present in channel $1's pool,
# one per line, without duplicates.
#
# Read out of the packages rather than parsed from their file names: the file
# name is conventional, the control field is authoritative.
deb_arches_in_channel() {
    local channel="$1" file
    while IFS= read -r file; do
        [[ -n "$file" ]] && dpkg-deb --field "$file" Architecture
    done < <(find "$DEB_ROOT/pool/$channel" -type f -name '*.deb' 2>/dev/null | sort) \
        | sort -u
}

# write_deb_release_conf: write the apt-ftparchive configuration used to build
# the top-level Release file for channel $1 with architectures $2.
write_deb_release_conf() {
    local channel="$1" arches="$2" path="$3"

    # Suite and Codename are both set to the channel name because apt matches
    # whichever the user wrote in their sources.list line against either one.
    #
    # Valid-Until is deliberately absent. Setting it makes the repository expire
    # on a deadline, and every subscriber's "apt update" starts failing if a new
    # release is not published before then -- a self-inflicted outage for a
    # project that ships a few times a year.
    cat > "$path" <<EOF
APT::FTPArchive::Release {
    Origin "$REPO_ORIGIN";
    Label "$REPO_LABEL";
    Suite "$channel";
    Codename "$channel";
    Architectures "$arches";
    Components "$DEB_COMPONENT";
    Description "$REPO_DESCRIPTION ($channel)";
};
EOF
}

# index_deb_channel: rebuild and sign every index for channel $1.
index_deb_channel() {
    local channel="$1"

    local arches arch_list
    arches="$(deb_arches_in_channel "$channel")"
    [[ -n "$arches" ]] || { warn "No packages in the $channel pool; skipping its indexes."; return 0; }
    arch_list="$(printf '%s' "$arches" | tr '\n' ' ' | sed 's/ *$//')"

    local dist_dir="$DEB_ROOT/dists/$channel"

    # The old signatures and Release must go before the new Release is built.
    # apt-ftparchive checksums every file it finds under dists/<channel>, so
    # leaving them in place would put stale entries -- including Release's own
    # checksum, computed over the previous Release -- into the new file.
    rm -f "$dist_dir/Release" "$dist_dir/Release.gpg" "$dist_dir/InRelease"

    local db="$PUBLISH_DATA/apt-ftparchive-$channel.db"

    local arch bin_dir
    for arch in $arches; do
        bin_dir="$dist_dir/$DEB_COMPONENT/binary-$arch"
        mkdir -p "$bin_dir"

        # The cd is what makes the Filename: fields come out relative to the
        # repository root, which is what apt resolves them against. Running this
        # from anywhere else produces an index full of paths that 404.
        #
        # --db caches each package's size and checksums, so a pool that has
        # accumulated years of releases is not re-hashed on every run.
        ( cd "$DEB_ROOT" && apt-ftparchive --arch "$arch" --db "$db" packages "pool/$channel" ) \
            > "$bin_dir/Packages" \
            || die "apt-ftparchive could not index pool/$channel for $arch.
If the cache is corrupt, delete $db and run again."

        gzip -9 -c "$bin_dir/Packages" > "$bin_dir/Packages.gz"
        if [[ "$DEB_INDEX_XZ" == "true" ]]; then
            xz -9 -c "$bin_dir/Packages" > "$bin_dir/Packages.xz"
        else
            rm -f "$bin_dir/Packages.xz"
        fi
        chmod 0644 "$bin_dir"/Packages*

        # The small per-component Release. Not strictly required, but apt reads
        # it when present and some mirroring tools expect it.
        cat > "$bin_dir/Release" <<EOF
Archive: $channel
Suite: $channel
Component: $DEB_COMPONENT
Architecture: $arch
Origin: $REPO_ORIGIN
Label: $REPO_LABEL
EOF
        chmod 0644 "$bin_dir/Release"

        local count
        count="$(grep -c '^Package: ' "$bin_dir/Packages" || true)"
        info "$channel/$arch: $count package(s) indexed"
    done

    # The top-level Release, built after every Packages file exists -- it
    # records their sizes and checksums, so building it first would bake in
    # stale numbers and apt would report a hash mismatch.
    local conf="$dist_dir/.release.conf"
    write_deb_release_conf "$channel" "$arch_list" "$conf"

    # Generated beside the target and moved into place, so that the scan cannot
    # see a half-written Release and checksum it.
    apt-ftparchive -c "$conf" release "$dist_dir" > "$dist_dir/.Release.new" \
        || die "apt-ftparchive could not build the Release file for $channel."
    mv "$dist_dir/.Release.new" "$dist_dir/Release"
    rm -f "$conf"
    chmod 0644 "$dist_dir/Release"

    gpg_sign_inline   "$dist_dir/Release" "$dist_dir/InRelease"
    gpg_sign_detached "$dist_dir/Release" "$dist_dir/Release.gpg"
}

# publish_deb_repo: stage the packages and rebuild every channel's indexes.
publish_deb_repo() {
    step "Publishing the apt repository"

    if [[ "$PUBLISH_DEB" != "true" ]]; then
        info "Skipped."
        return
    fi

    stage_debs

    local channel
    while IFS= read -r channel; do
        [[ -n "$channel" ]] && index_deb_channel "$channel"
    done < <(deb_channels_present)
}

# ---------------------------------------------------------------------------
# The dnf repository
# ---------------------------------------------------------------------------
#
# A dnf repository inverts Debian's arrangement. Here the .rpm files themselves
# carry a signature, injected into the package header by rpmsign, and dnf checks
# it against a key the user has imported (gpgcheck=1). The generated repodata/
# directory gets a detached signature over its repomd.xml, which dnf checks
# separately (repo_gpgcheck=1).
#
# So an RPM has to be signed after it is built and before it is indexed, and
# signing rewrites the file -- which is why the packages are signed in the
# repository, never in output/.

# rpm_signing_key: print the key id an .rpm is signed with, or nothing if it is
# unsigned.
#
# Both fields are checked because they cover different things: RSAHEADER is the
# signature over the package header, SIGPGP the one over the payload. rpmsign
# --addsign writes both; a package with neither has never been signed.
rpm_signing_key() {
    local file="$1" field sig
    for field in RSAHEADER SIGPGP; do
        sig="$(rpm -qp --nosignature --queryformat "%{${field}:pgpsig}" "$file" 2>/dev/null || true)"
        if [[ -n "$sig" && "$sig" != "(none)" ]]; then
            # The field reads like "RSA/SHA256, Tue 12 Aug 2025, Key ID abc123".
            printf '%s' "${sig##*Key ID }"
            return 0
        fi
    done
    return 0
}

# sign_rpm: add a GPG signature to the .rpm at $1, unless it already carries one
# from our key.
sign_rpm() {
    local file="$1"

    [[ "$DO_SIGN" == "1" ]] || return 0

    local have_key
    have_key="$(rpm_signing_key "$file")"
    if is_our_signing_key "$have_key"; then
        info "$(basename "$file") is already signed"
        return 0
    fi

    # --addsign replaces whatever signature is there, so a package signed with
    # an old key is re-signed rather than skipped. The defines come from
    # detect_rpm_sign_defines, which worked out how this rpm takes a passphrase.
    local sign_output
    sign_output="$(rpmsign --addsign "${RPM_SIGN_DEFINES[@]}" "$file" 2>&1)" || true

    have_key="$(rpm_signing_key "$file")"
    is_our_signing_key "$have_key" \
        || die "Could not sign $(basename "$file") with $SIGNING_KEY_FINGERPRINT.

rpmsign said:
$sign_output"

    info "$(basename "$file") signed by ${have_key}"
}

# rpm_same_build: succeed if the .rpm at $1 and the one at $2 are the same build.
#
# PKGID is a digest of the immutable header and the payload. Signing does not
# change it, so it still matches once the published copy has been signed --
# which is the whole point, because comparing the files byte for byte would
# report a difference the moment a signature was added.
rpm_same_build() {
    local a b
    a="$(rpm -qp --nosignature --queryformat '%{PKGID}' "$1" 2>/dev/null || true)"
    b="$(rpm -qp --nosignature --queryformat '%{PKGID}' "$2" 2>/dev/null || true)"
    [[ -n "$a" && "$a" == "$b" ]]
}

# stage_and_sign_rpms: copy each .rpm into its channel and architecture
# directory, then sign it there.
stage_and_sign_rpms() {
    local i dest dir

    for ((i = 0; i < ${#RPM_FILES[@]}; i++)); do
        dir="$RPM_ROOT/${RPM_CHANNELS[i]}/${RPM_ARCHES[i]}"
        mkdir -p "$dir"

        dest="$dir/$(basename "${RPM_FILES[i]}")"

        # Leave an identical, already-signed copy alone. Every signature carries
        # a timestamp, so re-signing would rewrite forty megabytes that then
        # have to be uploaded again, purely because the script was run twice.
        if [[ -f "$dest" ]] && [[ "$DO_SIGN" == "1" ]] \
           && rpm_same_build "${RPM_FILES[i]}" "$dest" \
           && is_our_signing_key "$(rpm_signing_key "$dest")"; then
            info "$(basename "$dest") is already published and signed"
            continue
        fi

        install -m 0644 "${RPM_FILES[i]}" "$dest"
        info "$(basename "$dest") -> ${RPM_CHANNELS[i]}/${RPM_ARCHES[i]}/"

        # Signed here, on the copy. Signing rewrites the file, and output/ is
        # build product that this script has no business modifying.
        sign_rpm "$dest"
    done
}

# rpm_dirs_present: print every <channel>/<arch> directory that holds packages.
rpm_dirs_present() {
    local channel dir
    for channel in "$STABLE_CHANNEL" "$BETA_CHANNEL"; do
        [[ -d "$RPM_ROOT/$channel" ]] || continue
        while IFS= read -r dir; do
            [[ -n "$dir" ]] || continue
            # Only directories that actually contain an .rpm, so an empty
            # leftover does not get pointless metadata.
            if find "$dir" -maxdepth 1 -name '*.rpm' -print -quit | grep -q .; then
                printf '%s\n' "$dir"
            fi
        done < <(find "$RPM_ROOT/$channel" -mindepth 1 -maxdepth 1 -type d | sort)
    done
}

# index_rpm_dir: build and sign the repodata for one <channel>/<arch> directory.
index_rpm_dir() {
    local dir="$1"

    # --update reuses checksums from the existing metadata for packages that
    # have not changed, which matters once the directory holds several releases.
    createrepo_c --quiet --update --checksum "$RPM_DIGEST_ALGO" "$dir" \
        || die "createrepo_c failed in $dir."

    find "$dir/repodata" -type f -exec chmod 0644 {} +

    # dnf checks this signature before it trusts anything in repodata/, which is
    # what repo_gpgcheck=1 in the generated .repo file turns on.
    gpg_sign_detached "$dir/repodata/repomd.xml" "$dir/repodata/repomd.xml.asc"

    local count
    count="$(find "$dir" -maxdepth 1 -name '*.rpm' | wc -l | tr -d ' ')"
    info "${dir#"$RPM_ROOT"/}: $count package(s) indexed"
}

# write_repo_file: generate the .repo file users install.
#
# The beta section ships disabled. Someone who wants betas enables it
# explicitly, which is how Fedora's own updates-testing works, and means the
# single file can be handed to everyone.
#
# $basearch is a dnf variable, expanded on the user's machine -- so a repository
# that grows an aarch64 directory later needs no change to what users already
# installed. It has to survive into the file literally, hence the escaping.
write_repo_file() {
    local path="$RPM_ROOT/$PACKAGE_NAME.repo"

    local gpgcheck=1 repo_gpgcheck=1
    if [[ "$DO_SIGN" == "0" ]]; then gpgcheck=0; repo_gpgcheck=0; fi

    cat > "$path" <<EOF
# $DISPLAY_NAME package repository for dnf / yum.
#
# Install with:
#     sudo dnf config-manager --add-repo $RPM_URL/$PACKAGE_NAME.repo
#
# Generated by publish-linux-repos.sh -- do not edit by hand.

[$PACKAGE_NAME]
name=$REPO_ORIGIN
baseurl=$RPM_URL/$STABLE_CHANNEL/\$basearch/
enabled=1
gpgcheck=$gpgcheck
repo_gpgcheck=$repo_gpgcheck
gpgkey=$KEYRING_URL
metadata_expire=6h

# Prerelease builds. Enable with:
#     sudo dnf config-manager --set-enabled $PACKAGE_NAME-$BETA_CHANNEL
[$PACKAGE_NAME-$BETA_CHANNEL]
name=$REPO_ORIGIN ($BETA_CHANNEL)
baseurl=$RPM_URL/$BETA_CHANNEL/\$basearch/
enabled=0
gpgcheck=$gpgcheck
repo_gpgcheck=$repo_gpgcheck
gpgkey=$KEYRING_URL
metadata_expire=6h
EOF
    chmod 0644 "$path"

    info "$PACKAGE_NAME.repo written"
}

# publish_rpm_repo: stage, sign and index the RPMs.
publish_rpm_repo() {
    step "Publishing the dnf repository"

    if [[ "$PUBLISH_RPM" != "true" ]]; then
        info "Skipped."
        return
    fi

    stage_and_sign_rpms

    local dir
    while IFS= read -r dir; do
        [[ -n "$dir" ]] && index_rpm_dir "$dir"
    done < <(rpm_dirs_present)

    write_repo_file
}

# ---------------------------------------------------------------------------
# Install instructions
# ---------------------------------------------------------------------------

# write_instructions: generate root/linux/README.md from the live configuration.
#
# Generated rather than hand-written so the URLs, the key fingerprint and the
# list of channels cannot drift away from what the repository actually contains.
write_instructions() {
    step "Writing install instructions"

    local path="$LINUX_DIR/README.md"
    local sources_file="/etc/apt/sources.list.d/$PACKAGE_NAME.sources"
    local keyring_path="/etc/apt/keyrings/$KEYRING_BASENAME.gpg"

    cat > "$path" <<EOF
# Installing $DISPLAY_NAME on Linux

## The short version

Download the \`.deb\` or \`.rpm\` from <$PACKAGE_URL> and install it. That is all
there is to it — the package sets up this repository on its own, so from then on
$DISPLAY_NAME updates along with the rest of your system.

A package built from a prerelease subscribes you to prereleases *and* releases,
so you are offered the stable version as soon as it appears. A package built
from a release subscribes you to releases only.

The file the package writes is
\`$sources_file\` (apt) or
\`/etc/yum.repos.d/$PACKAGE_NAME.repo\` (dnf). Its first line says it is managed
by the package. Delete that line and the package will never touch the file
again, leaving it yours to edit. To keep the file but stop following the
repository, set \`Enabled: no\` or \`enabled=0\` instead.

Everything here is signed with this key:

    $SIGNING_KEY_FINGERPRINT

The rest of this page is for setting the repository up by hand.

## By hand: Debian, Ubuntu, Mint and other apt systems

\`\`\`bash
sudo install -d -m 0755 /etc/apt/keyrings
sudo curl -fsSL $LINUX_URL/$KEYRING_BASENAME.gpg \\
    -o $keyring_path
sudo tee $sources_file <<'SOURCES'
Types: deb
URIs: $DEB_URL
Suites: $STABLE_CHANNEL
Components: $DEB_COMPONENT
Enabled: yes
Signed-By: $keyring_path
SOURCES
sudo apt update
sudo apt install $PACKAGE_NAME
\`\`\`

If \`curl\` is not installed, use \`wget -qO\` in its place.

This is the same path and the same format the package writes, so installing a
$DISPLAY_NAME package later will not leave you with two entries for one
repository. It will not overwrite this file either: what you write here has no
"managed by the package" marker on its first line, so the package treats it as
yours and leaves it alone. The trade-off is that it also will not follow you
onto the beta channel or off it — that stays your job.

The dearmored \`.gpg\` key is used rather than the armored \`.asc\` because apt
older than 2.4 — Debian 11, Ubuntu 20.04 and earlier — cannot read the armored
form. Newer apt accepts either.

### Prereleases

Add \`$BETA_CHANNEL\` to the \`Suites:\` line rather than replacing
\`$STABLE_CHANNEL\`, so the stable release is still offered when it appears:

\`\`\`
Suites: $BETA_CHANNEL $STABLE_CHANNEL
\`\`\`

## By hand: Fedora, RHEL, openSUSE and other dnf systems

\`\`\`bash
sudo dnf config-manager --add-repo $RPM_URL/$PACKAGE_NAME.repo
sudo dnf install $PACKAGE_NAME
\`\`\`

On dnf5 — Fedora 41 and later — the first command is spelled:

\`\`\`bash
sudo dnf config-manager addrepo --from-repofile=$RPM_URL/$PACKAGE_NAME.repo
\`\`\`

The first install asks you to confirm the signing key; check that the
fingerprint it shows matches the one above.

### Prereleases

The beta repository is installed but disabled. Enable it with:

\`\`\`bash
sudo dnf config-manager --set-enabled $PACKAGE_NAME-$BETA_CHANNEL
\`\`\`

## Removing the repository

Uninstalling $DISPLAY_NAME takes the repository configuration with it. To remove
it while keeping the application:

\`\`\`bash
sudo rm $sources_file $keyring_path      # apt
sudo rm /etc/yum.repos.d/$PACKAGE_NAME.repo               # dnf
\`\`\`

## Other formats

$DISPLAY_NAME is also distributed as an AppImage, which needs no repository and
no installation — download it, make it executable and run it. It configures
nothing and updates itself through neither of these repositories; see
<$PACKAGE_URL>.
EOF
    chmod 0644 "$path"

    info "$path"
}

# ---------------------------------------------------------------------------
# Verification
# ---------------------------------------------------------------------------
#
# Everything here checks the finished repository rather than what was intended,
# for the same reason build-linux-packages.sh inspects the finished packages: it
# is the only way to catch something a tool changed, dropped, or never wrote.

# verify_deb_repo: check the signatures and every checksum in every index.
verify_deb_repo() {
    [[ "$PUBLISH_DEB" == "true" ]] || return 0

    local channel dist_dir
    while IFS= read -r channel; do
        [[ -n "$channel" ]] || continue
        dist_dir="$DEB_ROOT/dists/$channel"
        [[ -f "$dist_dir/Release" ]] || continue

        if [[ "$DO_SIGN" == "1" ]]; then
            gpg --batch --quiet --verify "$dist_dir/InRelease" >/dev/null 2>&1 \
                || die "The signature on dists/$channel/InRelease does not verify."
            gpg --batch --quiet --verify "$dist_dir/Release.gpg" "$dist_dir/Release" >/dev/null 2>&1 \
                || die "The signature on dists/$channel/Release does not verify."
        fi

        # apt rejects a Release with no SHA256 section outright, and the failure
        # message it gives is not obvious.
        grep -q '^SHA256:' "$dist_dir/Release" \
            || die "dists/$channel/Release has no SHA256 section; apt would reject it."

        # Every package the index promises must exist, at the size and checksum
        # recorded. This is the check that catches a Filename: path that does
        # not resolve -- the most common way a hand-built repository is broken,
        # and one that only shows up when a user tries to install.
        local packages_file entry filename size sha actual_size actual_sha checked=0
        while IFS= read -r packages_file; do
            [[ -n "$packages_file" ]] || continue
            while read -r filename size sha; do
                [[ -n "$filename" ]] || continue

                [[ -f "$DEB_ROOT/$filename" ]] \
                    || die "dists/$channel indexes $filename, which does not exist."

                actual_size="$(stat -c '%s' "$DEB_ROOT/$filename")"
                [[ "$actual_size" == "$size" ]] \
                    || die "$filename is $actual_size bytes; the index says $size."

                actual_sha="$(sha256sum "$DEB_ROOT/$filename" | cut -d' ' -f1)"
                [[ "$actual_sha" == "$sha" ]] \
                    || die "$filename does not match the SHA256 in the index."

                checked=$((checked + 1))
            done < <(awk '
                /^Filename: / { fn = $2 }
                /^Size: /     { sz = $2 }
                /^SHA256: /   { sha = $2 }
                /^$/          { if (fn != "") { print fn, sz, sha; fn = "" } }
                END           { if (fn != "") print fn, sz, sha }
            ' "$packages_file")
        done < <(find "$dist_dir" -name Packages -type f | sort)

        info "$channel: $checked package reference(s) verified"
    done < <(deb_channels_present)
}

# verify_rpm_repo: check the metadata signature, and check each package's
# signature the way a user's dnf will.
#
# The package check imports the PUBLISHED public key into a scratch rpm database
# and runs rpm --checksig against it. That is a stronger statement than
# comparing key ids: it proves the signature actually validates against the key
# users will be told to trust, so it catches signing with a key whose public
# half never made it into the repository.
verify_rpm_repo() {
    [[ "$PUBLISH_RPM" == "true" ]] || return 0

    local rpmdb=""
    if [[ "$DO_SIGN" == "1" ]]; then
        rpmdb="$GNUPG_TMP/rpmdb"
        mkdir -p "$rpmdb"
        rpm --dbpath "$rpmdb" --initdb >/dev/null 2>&1 \
            || die "Could not create a scratch rpm database for verification."
        rpm --dbpath "$rpmdb" --import "$KEYRING_ASC" >/dev/null 2>&1 \
            || die "rpm would not import the published public key at $KEYRING_ASC."
    fi

    local dir file count checksig
    while IFS= read -r dir; do
        [[ -n "$dir" ]] || continue

        [[ -f "$dir/repodata/repomd.xml" ]] \
            || die "${dir#"$RPM_ROOT"/} has no repodata/repomd.xml."

        if [[ "$DO_SIGN" == "1" ]]; then
            gpg --batch --quiet --verify "$dir/repodata/repomd.xml.asc" \
                "$dir/repodata/repomd.xml" >/dev/null 2>&1 \
                || die "The signature on ${dir#"$RPM_ROOT"/}/repodata/repomd.xml does not verify."
        fi

        count=0
        while IFS= read -r file; do
            [[ -n "$file" ]] || continue
            if [[ "$DO_SIGN" == "1" ]]; then
                checksig="$(rpm --dbpath "$rpmdb" --checksig "$file" 2>&1)" \
                    || die "$(basename "$file") fails signature verification:

$checksig

A user's dnf would refuse to install it."

                # --checksig can exit 0 while reporting NOKEY or NOT OK, so the
                # wording is checked too rather than the exit status alone.
                printf '%s' "$checksig" | grep -qi 'signatures OK' \
                    || die "$(basename "$file") is not signed by the published key:

$checksig"
            fi
            count=$((count + 1))
        done < <(find "$dir" -maxdepth 1 -type f -name '*.rpm' | sort)

        info "${dir#"$RPM_ROOT"/}: $count package(s) verified"
    done < <(rpm_dirs_present)
}

# verify_permissions: nothing published may be group- or world-writable.
#
# The packages are normally read from a Windows drive mounted into WSL, where
# every file reports as mode 0777. Everything above uses install -m or an
# explicit chmod to avoid carrying that across; this is the check that the
# effort actually worked.
verify_permissions() {
    local bad
    bad="$(find "$PUBLISH_ROOT" -perm /022 -print 2>/dev/null | head -10)"

    [[ -z "$bad" ]] || die "Group- or world-writable files under $PUBLISH_ROOT_SUBDIR/:

$bad

These must not go on a web server. This usually means a file was copied from a
filesystem that does not store Unix permissions."
}

# ---------------------------------------------------------------------------
# Publishing log
# ---------------------------------------------------------------------------

# write_log: append what was published to data/publish.log.
#
# The repository can always be rebuilt from the packages themselves, so this is
# not needed for recovery. It is here to answer "when did that version go out,
# and is the file on the web site still the one I built?" long after the fact.
write_log() {
    local log="$PUBLISH_DATA/publish.log"
    local stamp
    stamp="$(date -u +%Y-%m-%dT%H:%M:%SZ)"

    local i sha
    {
        for ((i = 0; i < ${#DEB_FILES[@]}; i++)); do
            sha="$(sha256sum "${DEB_FILES[i]}" | cut -d' ' -f1)"
            printf '%s  %-6s %-6s %-10s %-20s %-8s %s\n' \
                "$stamp" deb "${DEB_CHANNELS[i]}" "${DEB_NAMES[i]}" \
                "${DEB_VERSIONS[i]}" "${DEB_ARCHES[i]}" "$sha"
        done
        for ((i = 0; i < ${#RPM_FILES[@]}; i++)); do
            # Hashed from the source package, before signing rewrote the copy,
            # so this matches what came out of the build.
            sha="$(sha256sum "${RPM_FILES[i]}" | cut -d' ' -f1)"
            printf '%s  %-6s %-6s %-10s %-20s %-8s %s\n' \
                "$stamp" rpm "${RPM_CHANNELS[i]}" "${RPM_NAMES[i]}" \
                "${RPM_VERSIONS[i]}" "${RPM_ARCHES[i]}" "$sha"
        done
    } >> "$log"
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

# show_summary: print where everything went and what a user would now type.
show_summary() {
    step "Done"

    printf '%s' "$C_OK"
    printf '    Publishing directory  %s\n' "$REPO_DIR"
    printf '    Upload                %s/  ->  %s/\n' "$PUBLISH_ROOT" "$PUBLISH_BASE_URL"
    printf '    Do NOT upload         %s/\n' "$PUBLISH_DATA"
    printf '%s\n' "$C_OFF"

    printf '    Users on apt:\n\n'
    printf '        sudo curl -fsSL %s \\\n' "$KEYRING_URL"
    printf '            -o /etc/apt/keyrings/%s.asc\n' "$KEYRING_BASENAME"
    printf '        echo "deb [signed-by=/etc/apt/keyrings/%s.asc] %s %s %s" \\\n' \
        "$KEYRING_BASENAME" "$DEB_URL" "$STABLE_CHANNEL" "$DEB_COMPONENT"
    printf '            | sudo tee /etc/apt/sources.list.d/%s.list\n' "$PACKAGE_NAME"
    printf '        sudo apt update && sudo apt install %s\n\n' "$PACKAGE_NAME"

    printf '    Users on dnf:\n\n'
    printf '        sudo dnf config-manager --add-repo %s/%s.repo\n' "$RPM_URL" "$PACKAGE_NAME"
    printf '        sudo dnf install %s\n\n' "$PACKAGE_NAME"

    printf '    The full instructions were written to %s\n\n' "$LINUX_DIR/README.md"

    if [[ "$DO_SIGN" == "0" ]]; then
        warn "This repository is UNSIGNED and cannot be used as it stands."
    fi
}

# show_dry_run: report what would happen, without touching anything.
show_dry_run() {
    step "Dry run -- nothing has been written"

    local i channel
    printf '%s' "$C_INFO"
    for ((i = 0; i < ${#DEB_FILES[@]}; i++)); do
        channel="${DEB_CHANNELS[i]}"
        printf '    %s\n        -> %s/%s/\n' \
            "${DEB_FILES[i]}" "$DEB_ROOT" "$(deb_pool_dir "$channel" "${DEB_NAMES[i]}")"
    done
    for ((i = 0; i < ${#RPM_FILES[@]}; i++)); do
        printf '    %s\n        -> %s/%s/%s/  (signed in place)\n' \
            "${RPM_FILES[i]}" "$RPM_ROOT" "${RPM_CHANNELS[i]}" "${RPM_ARCHES[i]}"
    done
    printf '%s\n' "$C_OFF"

    printf '    Indexes would be rebuilt for every channel that has packages,\n'
    printf '    signed with %s,\n' "$SIGNING_KEY_FINGERPRINT"
    printf '    and published under %s\n\n' "$PUBLISH_BASE_URL"
}

# ---------------------------------------------------------------------------
# Run
# ---------------------------------------------------------------------------

check_prerequisites
discover_packages

if [[ "$DRY_RUN" == "1" ]]; then
    show_dry_run
    exit 0
fi

setup_signing_key
ensure_layout
publish_public_key
publish_deb_repo
publish_rpm_repo
write_instructions

step "Verifying the repositories"
verify_deb_repo
verify_rpm_repo
verify_permissions

write_log
show_summary
