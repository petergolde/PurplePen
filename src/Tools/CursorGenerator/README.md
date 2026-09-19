# CursorGenerator

Generates PurplePen's sizing-cursor family as PNG assets for
`src/AvPurplePen/Assets/Cursors/`.

## Why these assets exist

Avalonia's macOS backend (`native/Avalonia.Native/src/OSX/cursor.mm`) maps all four
corner cursors *and* `SizeAll` to `[NSCursor crosshairCursor]`:

```objc
{ CursorTopLeftCorner,     crossCursor },
{ CursorTopRightCorner,    crossCursor },
{ CursorBottomLeftCorner,  crossCursor },
{ CursorBottomRightCorner, crossCursor },
{ CursorCross,             crossCursor },
{ CursorSizeAll,           crossCursor },
```

So on macOS `SizeNESW`, `SizeNWSE` and `SizeAll` all come out as a plain crosshair,
identical to `Cross`. Windows maps all five correctly (`IDC_SIZENESW` and friends) and
X11 has proper corner cursors, so this only affects macOS — plus `SizeAll` on Linux,
where `XC_sizing` doesn't match the shapes drawn here.

macOS 15 added public API for the diagonals
(`+[NSCursor frameResizeCursorFromPosition:inDirections:]`), but Avalonia doesn't use
it, and there is no public four-headed-arrow cursor on macOS at any version. The
private `_moveCursor` selector still exists but returns `nil`.

`Cursors.UseCustomSizingFamily` decides per platform which of these get used.

## Design notes

All five shapes come from one recipe in `gencursors.m` — same shaft thickness,
arrowhead size, arm length, white halo and drop shadow — so they read as a family.
`SizeAll` is the union of the horizontal and vertical arrows. Adjust the `k*`
constants at the top of the file to retune the whole set at once.

Cursors are authored at their **point** size, not a 2x pixel size. Avalonia's
custom-cursor path discards PNG resolution metadata — `ImmutableBitmap` in
`Avalonia.Skia` hardcodes `Dpi = new Vector(96, 96)` when loading from a stream, and
`Save` re-encodes without a `pHYs` chunk — so macOS treats one pixel as one point.
24x24 matches Apple's own `move` cursor (hotspot 12,12). The 48x48 variants are
generated for future use; nothing references them yet.

## Regenerating

macOS only (uses AppKit for rendering). From this directory:

```sh
clang -fobjc-arc -framework AppKit -o gencursors gencursors.m
./gencursors ../../AvPurplePen/Assets/Cursors
```

The committed PNGs are the build output; there is no build-time dependency on this
tool, so it does not need to run on Windows or Linux.
