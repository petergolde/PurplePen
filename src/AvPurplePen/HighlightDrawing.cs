using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvUtil;
using PurplePen;
using PurplePen.MapModel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace AvPurplePen
{
    // Class that draws the highlights from the map viewer as an IAvaloniaDrawing, for later display
    // by a PanAndZoom control.
    public class HighlightDrawing : IAvaloniaDrawing
    {
        IMapViewerHighlight[] highlights = new IMapViewerHighlight[0];

        public Rect Bounds {
            get {
                Rect bounds = new Rect(0, 0, 0, 0);
                foreach (var highlight in highlights) {
                    RectangleF highlightBounds = highlight.GetHighlightBounds();
                    bounds = bounds.Union(Conv.ToAvRect(highlightBounds));
                }
                return bounds;
            }
        }

        public event EventHandler? DrawingChanged;

        // Draw all the highlights on an Avalonia DrawingContext. Because we are using Skia, we need to create
        // a writable bitmap and use Skia to draw to that. This is not ideal from a performance perspective, but
        // should be OK. We can optimize later by reducing the size of the bitmap to just one big enough for the 
        // bounds of the highlights.
        public void Draw(DrawingContext drawingContext, Rect rectToDraw, PixelSize pixelSize, Matrix transformWorldToPixel, double layoutScale)
        {
            if (highlights.Length == 0)
                return;

            PurplePen.Graphics2D.Matrix xformWorldToPixel = MatrixFromAvMatrix(transformWorldToPixel);

            // Get the bounds of all the highlights, slightly expanded.
            Rect highlightBounds = Bounds;
            if (highlightBounds.Width == 0 || highlightBounds.Height == 0)
                return;

            // Inflate by 2 pixels on each side, in case of rounding errors and the like.
            // Make sure no bigger than rectToDraw.
            float inflationAmount = PurplePen.Graphics2D.Geometry.TransformDistance(2F, xformWorldToPixel);
            highlightBounds.Inflate(inflationAmount);
            highlightBounds = highlightBounds.Intersect(rectToDraw);

            // For now, create a writable bitmap of the full window size. But we should be able to instead create one
            // that is just big enough for "highlightBounds" of the highlights, and draw that, which would be more efficient.

            WriteableBitmapTracker skiaBitmap = SkiaWriteableBitmapUtil.DrawToBitmap(pixelSize, canvas => {
                using (Skia_GraphicsTarget grTarget = new Skia_GraphicsTarget(canvas)) {
                    // Old Purple Pen was not anti-aliased, but unless it's a performance issue it's nicer to
                    // turn it on. I turn it off specifically for the cross-hair drawing to get that crisp.
                    grTarget.PushAntiAliasing(true); 

                    // This graphics target is set up with pixel coordinates, which is what we want.
                    foreach (IMapViewerHighlight highlight in highlights) {
                        highlight.DrawHighlight(grTarget, xformWorldToPixel, layoutScale);
                    }
                }
            });

            try {
                // Things are flipped vertically between Skia and Avalonia, so we need to flip the bitmap around the horizontal center line before drawing it.
                Matrix flipTransform = Matrix.CreateScale(1, -1) * Matrix.CreateTranslation(0, (rectToDraw.Y * 2) + rectToDraw.Height);
                using (var pushedState = drawingContext.PushTransform(flipTransform)) {
                    drawingContext.DrawImage(skiaBitmap.Bitmap, rectToDraw);
                }
            }
            finally {
                skiaBitmap.Dispose();
            }
        }

        // Convert an Avalonia.Media.Matrix to a PurplePen.Graphics2D.Matrix.
        // Both use the same element ordering: m11, m12, m21, m22, offsetX, offsetY.
        private PurplePen.Graphics2D.Matrix MatrixFromAvMatrix(Matrix avMatrix)
        {
            return new PurplePen.Graphics2D.Matrix(
                (float)avMatrix.M11, (float)avMatrix.M12,
                (float)avMatrix.M21, (float)avMatrix.M22,
                (float)avMatrix.M31, (float)avMatrix.M32);
        }

        public void SetHighlights(IMapViewerHighlight[]? newHighlights)
        {
            if (newHighlights == null)
                newHighlights = new IMapViewerHighlight[0];

            if (highlights.Length == 0 && newHighlights.Length == 0)
                return;

            highlights = newHighlights;
            DrawingChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
