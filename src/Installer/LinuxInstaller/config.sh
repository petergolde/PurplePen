#!/bin/bash
#
# config.sh
#
# Settings for build-linux-packages.sh. Every value here can be overridden by
# setting the same name as an environment variable before running the build
# script, e.g.:
#
#     ICON_FAMILY=PurplePen ./build-linux-packages.sh
#     RUNTIME_IDENTIFIER=linux-arm64 ./build-linux-packages.sh
#
# No secrets belong in this file.
#

# ---------------------------------------------------------------------------
# Package identity
# ---------------------------------------------------------------------------

# Package name. Lower case, no spaces -- this is the name apt/dnf know the
# package by, the directory under /opt, the command in /usr/bin, and the icon
# name referenced by the desktop entry. Changing it after release makes package
# managers treat it as a different package.
: "${PACKAGE_NAME:=purplepen}"

# Name of the executable produced by the build. Must match AvPurplePen's
# <AssemblyName>, which is "PurplePen" -- note this differs in case from
# PACKAGE_NAME.
: "${APP_NAME:=PurplePen}"

# Human-readable application name, shown in menus and package descriptions.
: "${DISPLAY_NAME:=Purple Pen}"

# One-line package summary. Debian wants this under about 60 characters.
: "${PACKAGE_SUMMARY:=Course setting program for orienteering}"

# Longer description. Blank lines are not allowed; the script re-wraps this
# into the continuation format each package type wants.
: "${PACKAGE_DESCRIPTION:=Purple Pen is a course setting program for orienteering races. It \
allows you to design courses on top of an orienteering map, manage control \
points, produce course descriptions using IOF control description symbols, \
and export the results for printing and race management.}"

# Maintainer, in the RFC822 form Debian requires ("Name <email>").
: "${PACKAGE_MAINTAINER:=Peter Golde <peter@golde.org>}"

# Vendor / publisher, used by RPM.
: "${PACKAGE_VENDOR:=Purple Pen Software}"

# Project home page.
: "${PACKAGE_URL:=http://purple-pen.org}"

# License, in the short identifier form each packaging system expects. Purple
# Pen is BSD 3-clause (see the header of PurplePenCore/VersionNumber.cs).
: "${PACKAGE_LICENSE:=BSD-3-Clause}"

# Copyright line for /usr/share/doc/<package>/copyright.
: "${COPYRIGHT:=Copyright (c) 2006-2026 Peter Golde. All rights reserved.}"

# Debian section and RPM group. "graphics" is the closest fit for a drawing /
# map program; "science" would also be defensible.
: "${DEB_SECTION:=graphics}"
: "${RPM_GROUP:=Applications/Productivity}"

# Freedesktop menu categories for the .desktop entry. Must end with a
# semicolon. "Graphics" gives the main menu placement; the others are
# additional categories that some menu editors show.
: "${DESKTOP_CATEGORIES:=Graphics;Education;Sports;}"

# Keywords for desktop search. Must end with a semicolon.
: "${DESKTOP_KEYWORDS:=orienteering;course;map;control;o-map;}"

# Which icon family in AvPurplePen/Assets/AppIcon to install.
#
# That directory holds pre-rendered PNGs named <family>.<N>x<N>.png plus a
# <family>.svg. Set this to "PurplePen" for release builds and "PurplePenBeta"
# for beta builds. The PNGs are installed at their native sizes rather than
# being resampled, so any hand-tuning of the small sizes is preserved.
: "${ICON_FAMILY:=PurplePenBeta}"

# ---------------------------------------------------------------------------
# Install layout
# ---------------------------------------------------------------------------

# Where the self-contained application payload is installed.
#
# /opt is the conventional home for third-party bundles that ship their own
# runtime; it keeps ~140 MB of .NET out of /usr/lib, which is meant for
# distribution-managed libraries. A symlink at /usr/bin/$PACKAGE_NAME puts the
# program on PATH.
#
# .NET's apphost locates its assemblies through /proc/self/exe, which resolves
# symlinks, so a symlink works here and no wrapper script is needed.
: "${INSTALL_PREFIX:=/opt/$PACKAGE_NAME}"

# ---------------------------------------------------------------------------
# Build settings
# ---------------------------------------------------------------------------

# Runtime identifier to publish for.
#
#   linux-x64        glibc, x86-64   -> Debian amd64  / RPM x86_64
#   linux-arm64      glibc, ARM64    -> Debian arm64  / RPM aarch64
#   linux-musl-x64   musl (Alpine)   -> neither .deb nor .rpm; Alpine uses apk
#
# glibc and musl are not interchangeable, so a musl target needs its own build.
: "${RUNTIME_IDENTIFIER:=linux-x64}"

# Build configuration and target framework. Note this is net10.0 -- AvPurplePen
# is a plain Avalonia desktop app and does not use any platform workload.
: "${CONFIGURATION:=Release}"
: "${TARGET_FRAMEWORK:=net10.0}"

# Self-contained means the .NET runtime is bundled into the package, so users
# do not need to install .NET separately. This is what makes a single package
# work across distributions.
: "${SELF_CONTAINED:=true}"

# ReadyToRun ahead-of-time compilation improves startup time at the cost of a
# larger package. Off by default; turn on once the basic pipeline is proven.
: "${PUBLISH_READYTORUN:=false}"

# Republish PdfConverter as a self-contained helper and overlay it onto the
# payload.
#
# AvPurplePen.csproj copies PdfConverter's plain *build* output into the
# publish directory, which is framework-dependent: its apphost looks for a
# machine-wide .NET install, finds the self-contained app's own hostfxr next to
# it instead, and refuses to start. Republishing it self-contained for this RID
# makes it share the runtime files already present, so the helper costs only
# about 7 MB (essentially just libpdfium.so).
#
# Set to false to leave whatever the main publish produced untouched.
: "${INCLUDE_PDF_CONVERTER:=true}"

# ---------------------------------------------------------------------------
# Which packages to build
# ---------------------------------------------------------------------------

: "${BUILD_DEB:=true}"
: "${BUILD_RPM:=true}"

# ---------------------------------------------------------------------------
# Versioning
# ---------------------------------------------------------------------------
#
# PurplePenCore/VersionNumber.cs holds a four-part version such as "4.0.0.210".
# The last component encodes the release stage rather than a build number:
#
#     100-199   Alpha     110 = Alpha 1, 120 = Alpha 2, ...
#     200-299   Beta      210 = Beta 1,  220 = Beta 2,  ...
#     300-399   Release candidate
#     500       Stable
#
# A bare "4.0.0.210" would sort *after* the stable "4.0.0" in both dpkg and
# rpm, so a user on the beta would never be offered the release. Both systems
# spell "sorts before" with a tilde, so the stage is folded into the upstream
# version instead:
#
#     4.0.0.210  ->  4.0.0~beta1     (sorts before 4.0.0)
#     4.0.0.500  ->  4.0.0
#
# Set PACKAGE_VERSION to override the derived value entirely.
: "${PACKAGE_VERSION:=}"

# Packaging revision. Bump this when the packaging changes but the application
# does not (a corrected dependency, say). Resets to 1 on a new upstream
# version. Debian appends it as "-N"; RPM uses it as the Release field.
: "${PACKAGE_RELEASE:=1}"

# ---------------------------------------------------------------------------
# Runtime dependencies
# ---------------------------------------------------------------------------
#
# A self-contained publish bundles the .NET runtime but NOT the system
# libraries that runtime and Avalonia use.
#
# Most of what matters here is invisible to ldd. The payload's binaries record
# only four external DT_NEEDED dependencies -- libc, libgcc_s, libstdc++ and
# libfontconfig -- because everything else is opened with dlopen at runtime:
#
#     ICU, OpenSSL          .NET globalization and TLS
#     libX11, libICE, libSM Avalonia's X11 backend
#
# So the X11 entries below are NOT redundant even though nothing links against
# them; without them the application installs and then fails to open a window.
#
# Conversely, expat, freetype, png, brotli, uuid and friends are deliberately
# NOT listed: they appear in ldd output only because libfontconfig1 depends on
# them, and it will pull them in by itself.
#
# Re-run the survey at any time with --show-deps.

# Debian/Ubuntu package names.
#
# Alternatives ("a | b") matter for the versioned libraries: libicu and libssl
# carry their soname in the package name, so it differs on every release. The
# list covers Debian 11/12/13 and Ubuntu 20.04 through 25.04; extend it as new
# releases appear rather than dropping to an unversioned name, which does not
# exist.
: "${DEB_DEPENDS:=libc6, libgcc-s1, libstdc++6, \
libx11-6, libice6, libsm6, libfontconfig1, \
libicu76 | libicu74 | libicu72 | libicu71 | libicu70 | libicu69 | libicu67 | libicu66, \
libssl3t64 | libssl3 | libssl1.1}"

# Packages that improve the experience but are not required to start.
#
# xdg-utils provides xdg-open, which is how printing works on Linux: Purple Pen
# generates a PDF and hands it to the system viewer.
: "${DEB_RECOMMENDS:=xdg-utils}"

# RPM package names. These follow Fedora/RHEL conventions; openSUSE and Mageia
# name several of these differently, so a package built here may need its
# dependency list adjusted for those. Unlike Debian, the ICU and OpenSSL
# packages have stable unversioned names.
: "${RPM_REQUIRES:=glibc, libgcc, libstdc++, \
libX11, libICE, libSM, fontconfig, \
libicu, openssl-libs}"

: "${RPM_RECOMMENDS:=xdg-utils}"

# ---------------------------------------------------------------------------
# File associations
# ---------------------------------------------------------------------------

# Register a MIME type for .ppen course files and declare the application as
# its handler, so a double-clicked course file opens in Purple Pen.
#
# AvPurplePen accepts a file name as its first command-line argument (see
# CommandLineOptions.cs), so this works today.
: "${REGISTER_MIME_TYPE:=true}"

# The MIME type itself. "x-" marks it as unregistered with IANA.
: "${MIME_TYPE:=application/x-purplepen}"

# ---------------------------------------------------------------------------
# Build and output directories
# ---------------------------------------------------------------------------

# Scratch directory for the publish staging, the assembled install tree, and
# the RPM build root.
#
# Left empty, the script picks one: normally $SCRIPT_DIR/build, but a directory
# under $TMPDIR when the script lives on a filesystem that cannot store Unix
# permissions -- a Windows drive mounted into WSL, most importantly. Building a
# package there would silently produce one whose files are all mode 0777.
: "${BUILD_DIR:=}"

# Where the finished .deb and .rpm are written. Always under the script
# directory, so the packages end up beside the script that built them even when
# BUILD_DIR was redirected elsewhere.
: "${OUTPUT_SUBDIR:=output}"
