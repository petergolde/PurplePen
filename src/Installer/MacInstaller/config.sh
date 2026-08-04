#!/bin/bash
#
# config.sh
#
# Settings for build-mac-app.sh. Every value here can be overridden by setting
# the same name as an environment variable before running the build script,
# e.g.:
#
#     SKIP_NOTARIZE=1 ./build-mac-app.sh
#     SIGNING_IDENTITY="Developer ID Application: Someone Else (ABCDE12345)" ./build-mac-app.sh
#
# No secrets belong in this file. The app-specific password used for
# notarization lives in the macOS keychain (see NOTARY_KEYCHAIN_PROFILE below).
#

# ---------------------------------------------------------------------------
# Application identity
# ---------------------------------------------------------------------------

# Reverse-DNS bundle identifier. Must stay stable across releases; changing it
# makes macOS treat the app as a different application.
: "${BUNDLE_ID:=org.purple-pen.PurplePen}"

# Name of the .app bundle and the executable inside it. The executable name
# must match AvPurplePen's <AssemblyName>, which is "PurplePen".
: "${APP_NAME:=PurplePen}"

# CFBundleName is shown in the macOS menu bar and is limited to 15 characters.
: "${BUNDLE_NAME:=Purple Pen}"

# CFBundleDisplayName is shown in Finder and the Dock.
: "${DISPLAY_NAME:=Purple Pen}"

# Copyright string shown in Finder's Get Info panel.
: "${COPYRIGHT:=Copyright © 2006-2026 Peter Golde. All rights reserved.}"

# Which icon family in AvPurplePen/Assets/AppIcon to build the .icns from.
#
# That directory holds pre-rendered PNGs named <family>.<N>x<N>.png. Set this
# to "PurplePen" for release builds and "PurplePenBeta" for beta builds.
#
# The PNGs are used at their native sizes rather than being resampled from a
# single source, so any hand-tuning of the small sizes is preserved.
: "${ICON_FAMILY:=PurplePenBeta}"

# Minimum macOS version the app declares support for. This should track the
# minimum macOS version supported by the .NET runtime being bundled -- if it is
# set too low, the app will launch on an unsupported system and then crash.
: "${MIN_MACOS_VERSION:=13.0}"

# ---------------------------------------------------------------------------
# Build settings
# ---------------------------------------------------------------------------

# Runtime identifier to publish for. Apple Silicon only.
: "${RUNTIME_IDENTIFIER:=osx-arm64}"

# Build configuration and target framework. Note this is net10.0, NOT
# net10.0-macos -- AvPurplePen is a plain Avalonia desktop app and does not use
# the .NET macOS workload.
: "${CONFIGURATION:=Release}"
: "${TARGET_FRAMEWORK:=net10.0}"

# Self-contained means the .NET runtime is bundled into the .app, so users do
# not need to install .NET separately.
: "${SELF_CONTAINED:=true}"

# ReadyToRun ahead-of-time compilation improves startup time at the cost of a
# larger bundle. Off by default; set to true once you have confirmed the basic
# pipeline works end to end.
: "${PUBLISH_READYTORUN:=false}"

# Republish PdfConverter as a self-contained helper and overlay it onto the
# bundle payload.
#
# AvPurplePen.csproj copies PdfConverter's plain *build* output into the
# publish directory, which is framework-dependent: its apphost looks for a
# machine-wide .NET install, finds the self-contained app's own hostfxr next to
# it instead, and refuses to start. Republishing it self-contained for this RID
# makes it share the runtime files already in the bundle, so the helper costs
# only about 7 MB (essentially just libpdfium.dylib).
#
# Set to false to leave whatever the main publish produced untouched.
: "${INCLUDE_PDF_CONVERTER:=true}"

# ---------------------------------------------------------------------------
# Code signing
# ---------------------------------------------------------------------------

# Full name of the "Developer ID Application" certificate to sign with, e.g.
#     Developer ID Application: Peter Golde (ABCDE12345)
# Leave empty to have the script auto-detect it from the keychain. Auto-detect
# fails if zero, or more than one, such certificate is installed.
#
# List what you have with:
#     security find-identity -v -p codesigning
: "${SIGNING_IDENTITY:=}"

# ---------------------------------------------------------------------------
# Notarization
# ---------------------------------------------------------------------------

# Name of the notarytool keychain profile holding your Apple ID credentials.
# Create it once with:
#
#     xcrun notarytool store-credentials "PurplePen-Notary" \
#         --apple-id "peter@golde.org" \
#         --team-id "YOURTEAMID" \
#         --password "abcd-efgh-ijkl-mnop"
#
# The password is an app-specific password generated at appleid.apple.com,
# NOT your Apple ID password.
: "${NOTARY_KEYCHAIN_PROFILE:=PurplePen-Notary}"

# Alternative to the keychain profile: set all three of these in the
# environment (useful on a build server). If NOTARY_APPLE_ID is set, these are
# used instead of the keychain profile.
: "${NOTARY_APPLE_ID:=}"
: "${NOTARY_TEAM_ID:=}"
: "${NOTARY_PASSWORD:=}"

# ---------------------------------------------------------------------------
# Output
# ---------------------------------------------------------------------------

# Volume name shown when the DMG is mounted.
: "${DMG_VOLUME_NAME:=Purple Pen}"

# ---------------------------------------------------------------------------
# Disk image appearance
# ---------------------------------------------------------------------------

# Style the .dmg window: icon view, a background picture, and the app and
# Applications icons positioned side by side for drag-to-install.
#
# Styling drives Finder through AppleScript, so it needs a logged-in desktop
# session and one-time Automation consent (System Settings > Privacy &
# Security > Automation > your terminal > Finder). Set to 0 -- or pass
# --skip-style -- for a plain disk image on a headless build machine. The
# result is functional but looks unfinished, so do not ship it.
: "${DMG_STYLE:=1}"

# Content size of the .dmg window, in points, excluding the title bar.
#
# dmg-background.svg is rasterized to exactly these pixel dimensions at 1x and
# twice them at 2x, so its aspect ratio has to match. The build reads the
# rasterized sizes back and fails loudly if they disagree.
: "${DMG_WINDOW_WIDTH:=640}"
: "${DMG_WINDOW_HEIGHT:=400}"

# Where the window opens, in points from the top-left of the main display.
# If the window does not fit on the build machine's screen Finder silently
# shrinks it and the background stops lining up; the build detects that.
: "${DMG_WINDOW_X:=200}"
: "${DMG_WINDOW_Y:=140}"

# Icon and label sizes in the .dmg window.
: "${DMG_ICON_SIZE:=128}"
: "${DMG_TEXT_SIZE:=13}"

# Centres of the two icons, in points from the top-left of the window content
# area -- the same coordinate system dmg-background.svg is drawn in.
: "${DMG_APP_ICON_X:=170}"
: "${DMG_APP_ICON_Y:=200}"
: "${DMG_APPS_ICON_X:=470}"
: "${DMG_APPS_ICON_Y:=200}"

# Free space, in megabytes, left in the scratch read/write image beyond the
# app itself. Finder needs room for .DS_Store and the background picture, and
# HFS+ needs room for its catalog and journal. This costs almost nothing in
# the finished image because empty space compresses away.
: "${DMG_FREE_SPACE_MB:=64}"

# How long to wait, in seconds, for Finder to flush the window layout to
# .DS_Store before giving up.
: "${DMG_STYLE_TIMEOUT:=60}"
