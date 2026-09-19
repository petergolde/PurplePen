//  gencursors.m
//  Generates the PurplePen sizing-cursor family as PNG assets.
//
//  PurplePen needs its own sizing cursors because Avalonia's macOS backend maps all four
//  corner cursors (and SizeAll) to [NSCursor crosshairCursor], so SizeNESW/SizeNWSE/SizeAll
//  are indistinguishable from Cross. See src/Tools/CursorGenerator/README.md.
//
//  The five cursors share one drawing recipe -- same shaft thickness, head size, arm length,
//  white halo and drop shadow -- so they read as a single family. SizeAll is the union of the
//  horizontal and vertical arrows.
//
//  Cursors are authored at their *point* size: Avalonia's macOS custom-cursor path discards
//  PNG DPI metadata (Skia's ImmutableBitmap hardcodes Dpi = 96), so 1 px == 1 pt on screen.
//  24x24 matches Apple's own "move" cursor.
//
//  Build and run:
//      clang -fobjc-arc -framework AppKit -o gencursors gencursors.m
//      ./gencursors ../../AvPurplePen/Assets/Cursors
//
#import <AppKit/AppKit.h>

// Geometry of the arrow family, expressed in a 24-unit design box.
//
// kHeadHalf and kHeadLen are constrained by SizeAll, not by the two-headed arrows: in the
// four-way union the neighbouring heads' base corners sit at (kMargin + kHeadLen, 12 - kHeadHalf)
// and its mirror, so they collide once kHeadHalf grows toward 12 - kMargin - kHeadLen. The halo
// widens each shape by kHalo on top of that. Keep the diagonal gap between those two corners
// comfortably above 2 * kHalo or the four arms fuse into a solid diamond.
static const CGFloat kBox       = 24.0;   // design box size
static const CGFloat kMargin    = 1.5;    // gap from box edge to arrow tip
static const CGFloat kHeadLen   = 4.0;    // arrowhead length
static const CGFloat kHeadHalf  = 3.3;    // arrowhead half-width
static const CGFloat kShaftHalf = 1.10;   // shaft half-thickness
static const CGFloat kHalo      = 1.00;   // white outline thickness
static const CGFloat kSuper     = 8.0;    // supersample factor

// Build a double-headed arrow centred in the design box, rotated by `degrees`.
// The arrow runs tip-to-tip along the rotated axis; both ends get a head.
static NSBezierPath *DoubleArrow(CGFloat degrees)
{
    CGFloat c = kBox / 2.0;
    CGFloat a = kMargin;          // near tip
    CGFloat b = kBox - kMargin;   // far tip

    NSBezierPath *p = [NSBezierPath bezierPath];
    [p moveToPoint:   NSMakePoint(a,             c)];
    [p lineToPoint:   NSMakePoint(a + kHeadLen,  c - kHeadHalf)];
    [p lineToPoint:   NSMakePoint(a + kHeadLen,  c - kShaftHalf)];
    [p lineToPoint:   NSMakePoint(b - kHeadLen,  c - kShaftHalf)];
    [p lineToPoint:   NSMakePoint(b - kHeadLen,  c - kHeadHalf)];
    [p lineToPoint:   NSMakePoint(b,             c)];
    [p lineToPoint:   NSMakePoint(b - kHeadLen,  c + kHeadHalf)];
    [p lineToPoint:   NSMakePoint(b - kHeadLen,  c + kShaftHalf)];
    [p lineToPoint:   NSMakePoint(a + kHeadLen,  c + kShaftHalf)];
    [p lineToPoint:   NSMakePoint(a + kHeadLen,  c + kHeadHalf)];
    [p closePath];

    if (degrees == 0) { return p; }

    NSAffineTransform *t = [NSAffineTransform transform];
    [t translateXBy:c yBy:c];
    [t rotateByDegrees:degrees];
    [t translateXBy:-c yBy:-c];
    return [t transformBezierPath:p];
}

// The four-headed arrow: union of the horizontal and vertical arrows. Drawn as two subpaths;
// the halo pass paints the whole silhouette white, so the internal seam never shows.
static NSBezierPath *FourWayArrow(void)
{
    NSBezierPath *p = [NSBezierPath bezierPath];
    [p appendBezierPath:DoubleArrow(0)];
    [p appendBezierPath:DoubleArrow(90)];
    return p;
}

// Render `path` (in design-box units) to a PNG of `size` x `size` pixels.
// Two passes: a white halo carrying the drop shadow, then the black body on top.
static BOOL WriteCursor(NSBezierPath *path, CGFloat size, NSString *outPath)
{
    CGFloat px = size * kSuper;
    NSBitmapImageRep *rep =
        [[NSBitmapImageRep alloc] initWithBitmapDataPlanes:NULL
                                                pixelsWide:(NSInteger)px
                                                pixelsHigh:(NSInteger)px
                                             bitsPerSample:8
                                           samplesPerPixel:4
                                                  hasAlpha:YES
                                                  isPlanar:NO
                                            colorSpaceName:NSDeviceRGBColorSpace
                                               bytesPerRow:0
                                              bitsPerPixel:0];

    NSGraphicsContext *ctx = [NSGraphicsContext graphicsContextWithBitmapImageRep:rep];
    [NSGraphicsContext saveGraphicsState];
    [NSGraphicsContext setCurrentContext:ctx];
    [ctx setShouldAntialias:YES];

    // Scale the 24-unit design box up to the supersampled pixel grid.
    CGFloat s = px / kBox;
    NSAffineTransform *scale = [NSAffineTransform transform];
    [scale scaleBy:s];
    NSBezierPath *sp = [scale transformBezierPath:path];

    // Pass 1: white halo + drop shadow. Stroking with 2*kHalo widens the silhouette
    // outward by kHalo; round joins keep the arrow tips from growing spikes.
    NSShadow *shadow = [[NSShadow alloc] init];
    [shadow setShadowColor:[NSColor colorWithCalibratedWhite:0.0 alpha:0.38]];
    [shadow setShadowOffset:NSMakeSize(0, -0.75 * s)];
    [shadow setShadowBlurRadius:1.35 * s];

    [NSGraphicsContext saveGraphicsState];
    [shadow set];
    [[NSColor whiteColor] setFill];
    [[NSColor whiteColor] setStroke];
    [sp setLineWidth:2.0 * kHalo * s];
    [sp setLineJoinStyle:NSLineJoinStyleRound];
    [sp setLineCapStyle:NSLineCapStyleRound];
    [sp setWindingRule:NSWindingRuleNonZero];
    [sp stroke];
    [sp fill];
    [NSGraphicsContext restoreGraphicsState];

    // Pass 2: black body.
    [[NSColor blackColor] setFill];
    [sp setWindingRule:NSWindingRuleNonZero];
    [sp fill];

    [NSGraphicsContext restoreGraphicsState];

    // Downsample to the final size.
    NSImage *big = [[NSImage alloc] initWithSize:NSMakeSize(px, px)];
    [big addRepresentation:rep];

    NSBitmapImageRep *out =
        [[NSBitmapImageRep alloc] initWithBitmapDataPlanes:NULL
                                                pixelsWide:(NSInteger)size
                                                pixelsHigh:(NSInteger)size
                                             bitsPerSample:8
                                           samplesPerPixel:4
                                                  hasAlpha:YES
                                                  isPlanar:NO
                                            colorSpaceName:NSDeviceRGBColorSpace
                                               bytesPerRow:0
                                              bitsPerPixel:0];
    NSGraphicsContext *octx = [NSGraphicsContext graphicsContextWithBitmapImageRep:out];
    [NSGraphicsContext saveGraphicsState];
    [NSGraphicsContext setCurrentContext:octx];
    [octx setImageInterpolation:NSImageInterpolationHigh];
    [big drawInRect:NSMakeRect(0, 0, size, size)
           fromRect:NSZeroRect
          operation:NSCompositingOperationSourceOver
           fraction:1.0];
    [NSGraphicsContext restoreGraphicsState];

    NSData *png = [out representationUsingType:NSBitmapImageFileTypePNG properties:@{}];
    return [png writeToFile:outPath atomically:YES];
}

int main(int argc, const char **argv)
{
    @autoreleasepool {
        [NSApplication sharedApplication];

        NSString *dir = (argc > 1) ? [NSString stringWithUTF8String:argv[1]] : @".";
        CGFloat sizes[] = { 24.0, 48.0 };

        // SizeNESW points bottom-left <-> top-right; in AppKit's y-up space that is +45 deg.
        // SizeNWSE points top-left <-> bottom-right, i.e. -45 deg.
        NSDictionary<NSString *, NSBezierPath *> *family = @{
            @"SizeAll"  : FourWayArrow(),
            @"SizeWE"   : DoubleArrow(0),
            @"SizeNS"   : DoubleArrow(90),
            @"SizeNESW" : DoubleArrow(45),
            @"SizeNWSE" : DoubleArrow(-45),
        };

        for (NSString *name in [[family allKeys] sortedArrayUsingSelector:@selector(compare:)]) {
            for (unsigned i = 0; i < sizeof(sizes)/sizeof(sizes[0]); i++) {
                int n = (int)sizes[i];
                NSString *file = [NSString stringWithFormat:@"%@/%@Cursor%dx%d.png", dir, name, n, n];
                BOOL ok = WriteCursor(family[name], sizes[i], file);
                printf("%-8s %2dx%-2d  %s\n", [name UTF8String], n, n,
                       ok ? [[file lastPathComponent] UTF8String] : "FAILED");
                if (!ok) { return 1; }
            }
        }
    }
    return 0;
}
