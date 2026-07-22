#if TEST
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using SkiaSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace PurplePen.Tests
{
    // Utilities for rendering test output through Skia into System.Drawing bitmaps.
    internal static class TestRenderingUtils
    {
        // Create a bitmap and map the given drawing-coordinate rectangle onto the full bitmap.
        // Set yAxisIncreasesUpward for map coordinates, where the bottom of the rectangle maps
        // to the bottom of the bitmap and the top maps to the top of the bitmap.
        public static Bitmap RenderToBitmap(int width, int height, RectangleF rectangle, bool yAxisIncreasesUpward, Action<IGraphicsTarget> draw)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            try {
                SKImageInfo imageInfo = new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
                using (SKSurface surface = SKSurface.Create(imageInfo, bitmapData.Scan0, bitmapData.Stride)) {
                    SKCanvas canvas = surface.Canvas;
                    canvas.Clear(SKColors.White);

                    float horizontalScale = width / rectangle.Width;
                    float verticalScale = height / rectangle.Height;
                    canvas.Scale(horizontalScale, yAxisIncreasesUpward ? -verticalScale : verticalScale);
                    canvas.Translate(-rectangle.Left, yAxisIncreasesUpward ? -rectangle.Bottom : -rectangle.Top);

                    using (Skia_GraphicsTarget graphicsTarget = new Skia_GraphicsTarget(canvas))
                        draw(graphicsTarget);
                }
            }
            finally {
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }

        // Draw onto an existing bitmap, with no coordinate mapping, using Skia_GraphicsTarget.
        public static void RenderToExistingBitmap(Bitmap bitmap, Action<IGraphicsTarget> draw)
        {
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            try {
                SKImageInfo imageInfo = new SKImageInfo(bitmap.Width, bitmap.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
                using (SKSurface surface = SKSurface.Create(imageInfo, bitmapData.Scan0, bitmapData.Stride)) {
                    SKCanvas canvas = surface.Canvas;
                    using (Skia_GraphicsTarget graphicsTarget = new Skia_GraphicsTarget(canvas))
                        draw(graphicsTarget);
                }
            }
            finally {
                bitmap.UnlockBits(bitmapData);
            }
        }

        // Copy an SKBitmap into a new, same-sized System.Drawing bitmap.
        public static Bitmap GdiPlusBitmapFromSkBitmap(SKBitmap skBitmap)
        {
            Bitmap bitmap = new Bitmap(skBitmap.Width, skBitmap.Height, PixelFormat.Format32bppPArgb);
            try {
                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
                try {
                    SKImageInfo imageInfo = new SKImageInfo(bitmap.Width, bitmap.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
                    using (SKPixmap pixmap = skBitmap.PeekPixels()) {
                        if (!pixmap.ReadPixels(imageInfo, bitmapData.Scan0, bitmapData.Stride, 0, 0))
                            throw new InvalidOperationException("Unable to copy the Skia bitmap pixels into a GDI+ bitmap.");
                    }
                }
                finally {
                    bitmap.UnlockBits(bitmapData);
                }

                return bitmap;
            }
            catch {
                bitmap.Dispose();
                throw;
            }
        }
    }
}
#endif
