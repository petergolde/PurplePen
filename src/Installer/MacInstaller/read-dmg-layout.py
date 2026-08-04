#!/usr/bin/env python3
"""
read-dmg-layout.py

Read the Finder window settings out of a disk image's .DS_Store and report
them, so build-mac-app.sh can prove the disk image it just built will actually
open styled rather than having to take Finder's word for it.

Usage:  read-dmg-layout.py <path to .DS_Store>

Prints a one-line summary and exits 0 when the layout is present and usable.
Exits 1 with a message on stderr when it is not, which the build treats as a
failure -- an unstyled disk image should never ship silently.

Why parse the file rather than just check that it exists: macOS creates
skeleton .DS_Store files routinely. One can exist, be several kilobytes long,
and contain no view settings at all, so its presence proves nothing.

Format: records are stored as a UTF-16BE filename, a four-character structure
id, a four-character type, then the payload. The ids needed here are:

    icvp    icon view options, a binary plist (background, icon size, ...)
    bwsp    browser window settings, a binary plist (the window bounds)
    Iloc    an icon position, 16 bytes of which the first two big-endian
            uint32s are x and y

The ids are plain ASCII in the file, and a payload is far smaller than the
4 KB B-tree nodes records live in, so scanning for them directly is safe.
"""

import plistlib
import struct
import sys


def find_blob(data, tag):
    """Return the payload of the first <tag>blob record, or None."""
    marker = tag + b"blob"
    i = data.find(marker)
    if i < 0:
        return None
    start = i + len(marker)
    (length,) = struct.unpack(">I", data[start:start + 4])
    return data[start + 4:start + 4 + length]


def find_positions(data):
    """Return every icon position recorded in the file, as (x, y) pairs."""
    positions = []
    marker = b"Ilocblob" + struct.pack(">I", 16)
    i = 0
    while True:
        i = data.find(marker, i)
        if i < 0:
            return positions
        x, y = struct.unpack(">II", data[i + len(marker):i + len(marker) + 8])
        positions.append((x, y))
        i += 1


def main():
    if len(sys.argv) != 2:
        print(__doc__.strip(), file=sys.stderr)
        return 2

    try:
        with open(sys.argv[1], "rb") as handle:
            data = handle.read()
    except OSError as exc:
        print("could not read .DS_Store: %s" % exc, file=sys.stderr)
        return 1

    icvp = find_blob(data, b"icvp")
    if icvp is None:
        print("no icon view settings (icvp) recorded", file=sys.stderr)
        return 1

    try:
        opts = plistlib.loads(icvp)
    except Exception as exc:
        print("icon view settings could not be decoded: %s" % exc, file=sys.stderr)
        return 1

    # backgroundType 2 means "picture". Without the alias the picture is
    # recorded but unresolvable, which shows up as a blank window.
    if opts.get("backgroundType") != 2:
        print("no background picture set (backgroundType=%r)"
              % opts.get("backgroundType"), file=sys.stderr)
        return 1
    if "backgroundImageAlias" not in opts:
        print("background picture has no alias, so Finder cannot resolve it",
              file=sys.stderr)
        return 1

    positions = find_positions(data)
    if len(positions) < 2:
        print("expected at least two positioned icons, found %d" % len(positions),
              file=sys.stderr)
        return 1

    # The window size is advisory: report it, but do not fail on it. Finder
    # clamps the window when it does not fit the screen, and the build already
    # warns about that separately.
    size = "unknown"
    bwsp = find_blob(data, b"bwsp")
    if bwsp is not None:
        try:
            bounds = plistlib.loads(bwsp).get("WindowBounds")
            if bounds:
                size = str(bounds)
        except Exception:
            pass

    print("background set, %spt icons, window %s, icons at %s"
          % (opts.get("iconSize", "?"), size,
             " ".join("(%d,%d)" % p for p in positions)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
