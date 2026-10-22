using AvUtil;
using PurplePen;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using SkiaSharp;
using System;
using System.Threading;

namespace AvPurplePen
{
    // Draws a page recorded by a RecordingPrintTarget as an IThreadsafeSkiaDrawing, for display in a
    // PanAndZoom control (wrapped in a CachedDrawing). The page is shown as a sheet of white paper with
    // a drop shadow, on a gray background that surrounds the paper by PaperMarginMm.
    //
    // World coordinates are millimeters with Y increasing upward (as PanAndZoom expects), so a zoom factor of 1
    // shows the page at actual size. The top-left corner of the paper is at (0,0), so the paper occupies
    // X from 0 to the paper width and Y from minus the paper height to 0.
    public class RecordedPageDrawing : IThreadsafeSkiaDrawing
    {
        // Millimeters per hundredth of an inch (the units that pages are printed in).
        public const float MmPerHundredth = 0.254F;

        // The amount of gray background around each side of the paper, in millimeters.
        public const float PaperMarginMm = 8F;

        // Offset of the drop shadow from the paper, in millimeters.
        private const float ShadowOffsetMm = 1.2F;

        // Colors of the background around the paper, the drop shadow, and the paper outline.
        private static readonly SKColor BackgroundColor = new SKColor(0xC8, 0xC8, 0xC8);
        private static readonly SKColor ShadowColor = new SKColor(0x00, 0x00, 0x00, 0x50);
        private static readonly SKColor OutlineColor = new SKColor(0x90, 0x90, 0x90);

        private readonly RecordedPrintPage page;
        private readonly IColorConverter? colorConverter;

        // Create a drawing of a recorded page.
        // page: the page to draw.
        // colorConverter: converts CMYK colors to display colors; null uses the Skia default conversion.
        public RecordedPageDrawing(RecordedPrintPage page, IColorConverter? colorConverter)
        {
            this.page = page;
            this.colorConverter = colorConverter;
        }

        // The page being drawn.
        public RecordedPrintPage Page => page;

        // The drawing never changes.
        public event EventHandler? DrawingChanged {
            add { }
            remove { }
        }

        // The bounds of the drawing: the paper plus the surrounding margin, in world coordinates.
        public SKRect Bounds {
            get {
                SKRect bounds = GetPaperRect(page.PaperSize);
                bounds.Inflate(PaperMarginMm, PaperMarginMm);
                return bounds;
            }
        }

        // Get the rectangle occupied by the paper, in world coordinates (millimeters, Y upward,
        // top-left of the paper at the origin).
        // paperSize: the size of the paper.
        public static SKRect GetPaperRect(PrintingPaperSize paperSize)
        {
            float width = paperSize.SizeInHundreths.Width * MmPerHundredth;
            float height = paperSize.SizeInHundreths.Height * MmPerHundredth;
            return new SKRect(0, -height, width, 0);
        }

        // Draw the background, paper, and page contents. Called on a background thread by CachedDrawing.
        // canvas: the canvas, already transformed so that world coordinates are used.
        // rectToDraw: the area being drawn, in world coordinates.
        // pixelSize: the size of the area being drawn, in pixels.
        // cancelToken: cancels the drawing.
        public void ThreadsafeDraw(SKCanvas canvas, SKRect rectToDraw, SKSizeI pixelSize, CancellationToken cancelToken)
        {
            SKRect paperRect = GetPaperRect(page.PaperSize);
            float worldPerPixel = (pixelSize.Width > 0) ? rectToDraw.Width / pixelSize.Width : 0;

            canvas.Clear(BackgroundColor);

            // Drop shadow, offset to the lower right (Y is upward in world coordinates).
            SKRect shadowRect = paperRect;
            shadowRect.Offset(ShadowOffsetMm, -ShadowOffsetMm);
            using (SKPaint shadowPaint = new SKPaint { Color = ShadowColor, Style = SKPaintStyle.Fill }) {
                canvas.DrawRect(shadowRect, shadowPaint);
            }

            // The paper, and its contents. Page coordinates are hundredths of an inch with Y downward,
            // so flip the Y axis.
            int saveCount = canvas.Save();
            try {
                canvas.Scale(MmPerHundredth, -MmPerHundredth);
                DrawPageContents(canvas, page, colorConverter, cancelToken);
            }
            finally {
                canvas.RestoreToCount(saveCount);
            }

            // A thin (one pixel) outline around the paper.
            using (SKPaint outlinePaint = new SKPaint { Color = OutlineColor, Style = SKPaintStyle.Stroke, StrokeWidth = worldPerPixel }) {
                canvas.DrawRect(paperRect, outlinePaint);
            }
        }

        // Draw a white sheet of paper and the contents of a page onto a canvas, clipped to the paper. The canvas must
        // already be transformed so that the page coordinate system is used: hundredths of an inch, with the origin at the
        // top-left of the paper and Y downward. The canvas state is restored afterward, even if the drawing is canceled.
        // canvas: the canvas to draw on.
        // page: the page to draw.
        // colorConverter: converts CMYK colors to display colors; null uses the Skia default conversion.
        // cancelToken: cancels the drawing (an OperationCanceledException is thrown).
        public static void DrawPageContents(SKCanvas canvas, RecordedPrintPage page, IColorConverter? colorConverter, CancellationToken cancelToken)
        {
            SKRect paperRect = new SKRect(0, 0, page.PaperSize.SizeInHundreths.Width, page.PaperSize.SizeInHundreths.Height);

            int saveCount = canvas.Save();
            try {
                using (SKPaint paperPaint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill }) {
                    canvas.DrawRect(paperRect, paperPaint);
                }

                canvas.ClipRect(paperRect);

                using (Skia_GraphicsTarget grTarget = new Skia_GraphicsTarget(canvas, false, colorConverter)) {
                    grTarget.PushAntiAliasing(true);
                    page.Draw(grTarget, cancelToken);
                    grTarget.PopAntiAliasing();
                }
            }
            finally {
                canvas.RestoreToCount(saveCount);
            }
        }
    }
}
