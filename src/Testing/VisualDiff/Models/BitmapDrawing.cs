using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvUtil;
using SkiaSharp;
using System;
using System.IO;

namespace VisualDiff.Models
{
    // Adapts a Skia bitmap to the drawing contract used by PanAndZoom.
    internal sealed class BitmapDrawing : IAvaloniaDrawing, IDisposable
    {
        private const double PixelsPerMillimeter = 96.0 / 25.4;
        private readonly Bitmap bitmap;
        private readonly Rect sourceRect;

        public BitmapDrawing(SKBitmap skBitmap)
        {
            using SKImage image = SKImage.FromBitmap(skBitmap);
            using SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            using MemoryStream stream = new MemoryStream(encoded.ToArray(), writable: false);
            bitmap = new Bitmap(stream);

            sourceRect = new Rect(0, 0, skBitmap.Width, skBitmap.Height);
            Bounds = new Rect(0, 0,
                skBitmap.Width / PixelsPerMillimeter,
                skBitmap.Height / PixelsPerMillimeter);
        }

        public Rect Bounds { get; }

        // This immutable drawing never changes after construction.
        public event EventHandler? DrawingChanged {
            add { }
            remove { }
        }

        public void Draw(DrawingContext drawingContext, Rect rectToDraw, PixelSize physicalPixelSize, Matrix transformWorldToPhysicalPixel)
        {
            drawingContext.FillRectangle(Brushes.White, rectToDraw);

            // PanAndZoom uses map coordinates, where Y increases upward. Bitmap rows increase
            // downward, so reverse Y locally while leaving Bounds in normal world coordinates.
            Matrix bitmapToWorld = Matrix.CreateScale(1, -1) *
                                   Matrix.CreateTranslation(0, Bounds.Height);
            using (drawingContext.PushTransform(bitmapToWorld)) {
                drawingContext.DrawImage(bitmap, sourceRect, Bounds);
            }
        }

        public void Dispose()
        {
            bitmap.Dispose();
        }
    }
}
