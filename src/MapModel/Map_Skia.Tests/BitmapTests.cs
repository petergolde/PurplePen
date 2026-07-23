using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using NUnit.Framework;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using TestingUtils;
using SDSize = System.Drawing.Size;

namespace Map_Skia.Tests
{

    [TestFixture]
    public class BitmapTests
    {
        // Helper: creates a simple test bitmap with known content.
        private SKBitmap CreateTestBitmap(int width, int height)
        {
            SKBitmap bitmap = new SKBitmap(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            using (SKCanvas canvas = new SKCanvas(bitmap)) {
                canvas.Clear(SKColors.Blue);
                using (SKPaint paint = new SKPaint()) {
                    paint.Color = SKColors.Red;
                    canvas.DrawRect(10, 10, width / 2, height / 2, paint);
                }
            }
            return bitmap;
        }



        [Test]
        public void DrawSkiaBitmap()
        {
            string expectedResult = TestUtil.GetTestFile("bitmaps\\DrawSkiaBitmap_baseline.png");

            const string baseBitmapName = "Water lilies.jpg";
            string baseBitmapPath = TestUtil.GetTestFile("bitmaps\\" + baseBitmapName);

            SKBitmap skBitmap = SKBitmap.Decode(baseBitmapPath);
            IGraphicsBitmap skiaBitmap = new Skia_Bitmap(skBitmap);

            int width = skiaBitmap.PixelWidth;
            int height = skiaBitmap.PixelHeight;

            RenderingUtil.RenderingTest(width, new System.Drawing.RectangleF(0, 0, width, height), false, expectedResult,
                grTarget => {
                    grTarget.DrawBitmap(skiaBitmap, new System.Drawing.RectangleF(0, 0, width, height), BitmapScaling.HighQuality);
                }
            );
        }

        [Test]
        public void DrawSkiaBitmapPart()
        {
            string expectedResult = TestUtil.GetTestFile("bitmaps\\DrawSkiaBitmapPart_baseline.png");

            const string baseBitmapName = "Water lilies.jpg";
            string baseBitmapPath = TestUtil.GetTestFile("bitmaps\\" + baseBitmapName);

            SKBitmap skBitmap = SKBitmap.Decode(baseBitmapPath);
            IGraphicsBitmap skiaBitmap = new Skia_Bitmap(skBitmap);

            int width = skiaBitmap.PixelWidth;
            int height = skiaBitmap.PixelHeight;

            RenderingUtil.RenderingTest(width, new System.Drawing.RectangleF(0, 0, width, height), false, expectedResult,
                grTarget => {
                    grTarget.DrawBitmapPart(skiaBitmap, 
                                            100, 130, 400, 200,
                                            new System.Drawing.RectangleF(20, 10, 500, 400), 
                                            BitmapScaling.HighQuality);
                }
            );
        }

        [Test]
        public void CropSkiaBitmap()
        {
            string expectedResult = TestUtil.GetTestFile("bitmaps\\CropSkiaBitmap_baseline.png");

            const string baseBitmapName = "Water lilies.jpg";
            string baseBitmapPath = TestUtil.GetTestFile("bitmaps\\" + baseBitmapName);

            SKBitmap skBitmap = SKBitmap.Decode(baseBitmapPath);
            IGraphicsBitmap skiaBitmap = new Skia_Bitmap(skBitmap);

            IGraphicsBitmap croppedBitmap = skiaBitmap.Crop(100, 130, 450, 250);
            Assert.AreEqual(450, croppedBitmap.PixelWidth);
            Assert.AreEqual(250, croppedBitmap.PixelHeight);

            RenderingUtil.RenderingTest(croppedBitmap.PixelWidth, new System.Drawing.RectangleF(0, 0, croppedBitmap.PixelWidth, croppedBitmap.PixelHeight), false, expectedResult,
                grTarget => {
                    grTarget.DrawBitmap(croppedBitmap,
                                        new System.Drawing.RectangleF(0, 0, croppedBitmap.PixelWidth, croppedBitmap.PixelHeight),
                                        BitmapScaling.HighQuality);
                }
            );
        }

        [Test]
        public void WriteSkiaBitmapToStream()
        {
            string expectedResult = TestUtil.GetTestFile("bitmaps\\WriteSkiaBitmapToStream_baseline.png");

            const string baseBitmapName = "Water lilies.jpg";
            string baseBitmapPath = TestUtil.GetTestFile("bitmaps\\" + baseBitmapName);

            SKBitmap skBitmap = SKBitmap.Decode(baseBitmapPath);
            IGraphicsBitmap skiaBitmap = new Skia_Bitmap(skBitmap);

            MemoryStream memStream = new MemoryStream();
            skiaBitmap.WriteToStream(GraphicsBitmapFormat.PNG, memStream, 100);
            memStream.Seek(0, SeekOrigin.Begin);

            SKBitmap loadedBitmap = SKBitmap.Decode(memStream);

            BitmapTestUtil.CompareBitmapBaseline(loadedBitmap, expectedResult);
        }


        [Test]
        public void DrawSkiaImage()
        {
            string expectedResult = TestUtil.GetTestFile("bitmaps\\DrawSkiaBitmap_baseline.png");

            const string baseBitmapName = "Water lilies.jpg";
            string baseBitmapPath = TestUtil.GetTestFile("bitmaps\\" + baseBitmapName);

            SKBitmap skBitmap = SKBitmap.Decode(baseBitmapPath);
            SKImage skImage = SKImage.FromBitmap(skBitmap);
            IGraphicsBitmap skiaImage = new Skia_Image(skImage);

            int width = skiaImage.PixelWidth;
            int height = skiaImage.PixelHeight;

            Assert.AreEqual(width, skiaImage.PixelWidth);
            Assert.AreEqual(height, skiaImage.PixelHeight);


            RenderingUtil.RenderingTest(width, new System.Drawing.RectangleF(0, 0, width, height), false, expectedResult,
                grTarget => {
                    grTarget.DrawBitmap(skiaImage, new System.Drawing.RectangleF(0, 0, width, height), BitmapScaling.HighQuality);
                }
            );
        }

        [Test]
        public void DrawSkiaImagePart()
        {
            string expectedResult = TestUtil.GetTestFile("bitmaps\\DrawSkiaBitmapPart_baseline.png");

            const string baseBitmapName = "Water lilies.jpg";
            string baseBitmapPath = TestUtil.GetTestFile("bitmaps\\" + baseBitmapName);

            SKBitmap skBitmap = SKBitmap.Decode(baseBitmapPath);
            SKImage skImage = SKImage.FromBitmap(skBitmap);
            IGraphicsBitmap skiaImage = new Skia_Image(skImage);

            int width = skiaImage.PixelWidth;
            int height = skiaImage.PixelHeight;

            RenderingUtil.RenderingTest(width, new System.Drawing.RectangleF(0, 0, width, height), false, expectedResult,
                grTarget => {
                    grTarget.DrawBitmapPart(skiaImage,
                                            100, 130, 400, 200,
                                            new System.Drawing.RectangleF(20, 10, 500, 400),
                                            BitmapScaling.HighQuality);
                }
            );
        }

        [Test]
        public void CropSkiaImage()
        {
            string expectedResult = TestUtil.GetTestFile("bitmaps\\CropSkiaBitmap_baseline.png");

            const string baseBitmapName = "Water lilies.jpg";
            string baseBitmapPath = TestUtil.GetTestFile("bitmaps\\" + baseBitmapName);

            SKBitmap skBitmap = SKBitmap.Decode(baseBitmapPath);
            SKImage skImage = SKImage.FromBitmap(skBitmap);
            IGraphicsBitmap skiaImage = new Skia_Image(skImage);

            IGraphicsBitmap croppedImage = skiaImage.Crop(100, 130, 450, 250);
            Assert.AreEqual(450, croppedImage.PixelWidth);
            Assert.AreEqual(250, croppedImage.PixelHeight);

            RenderingUtil.RenderingTest(croppedImage.PixelWidth, new System.Drawing.RectangleF(0, 0, croppedImage.PixelWidth, croppedImage.PixelHeight), false, expectedResult,
                grTarget => {
                    grTarget.DrawBitmap(croppedImage,
                                        new System.Drawing.RectangleF(0, 0, croppedImage.PixelWidth, croppedImage.PixelHeight),
                                        BitmapScaling.HighQuality);
                }
            );
        }

        [Test]
        public void WriteSkiaImageToStream()
        {
            string expectedResult = TestUtil.GetTestFile("bitmaps\\WriteSkiaBitmapToStream_baseline.png");

            const string baseBitmapName = "Water lilies.jpg";
            string baseBitmapPath = TestUtil.GetTestFile("bitmaps\\" + baseBitmapName);

            SKBitmap skBitmap = SKBitmap.Decode(baseBitmapPath);
            SKImage skImage = SKImage.FromBitmap(skBitmap);
            IGraphicsBitmap skiaImage = new Skia_Image(skImage);

            MemoryStream memStream = new MemoryStream();
            skiaImage.WriteToStream(GraphicsBitmapFormat.PNG, memStream, 100);
            memStream.Seek(0, SeekOrigin.Begin);

            SKBitmap loadedBitmap = SKBitmap.Decode(memStream);

            BitmapTestUtil.CompareBitmapBaseline(loadedBitmap, expectedResult);
        }


        [Test]
        public void DrawSkiaPixmap()
        {
            string expectedResult = TestUtil.GetTestFile("bitmaps\\DrawSkiaBitmap_baseline.png");

            const string baseBitmapName = "Water lilies.jpg";
            string baseBitmapPath = TestUtil.GetTestFile("bitmaps\\" + baseBitmapName);

            SKBitmap skBitmap = SKBitmap.Decode(baseBitmapPath);
            SKPixmap skPixmap = skBitmap.PeekPixels();
            IGraphicsBitmap skiaPixmap = new Skia_Pixmap(skPixmap);

            int width = skPixmap.Width;
            int height = skPixmap.Height;

            RenderingUtil.RenderingTest(width, new System.Drawing.RectangleF(0, 0, width, height), false, expectedResult,
                grTarget => {
                    grTarget.DrawBitmap(skiaPixmap, new System.Drawing.RectangleF(0, 0, width, height), BitmapScaling.HighQuality);
                }
            );
        }

        [Test]
        public void DrawSkiaPixmapPart()
        {
            string expectedResult = TestUtil.GetTestFile("bitmaps\\DrawSkiaBitmapPart_baseline.png");

            const string baseBitmapName = "Water lilies.jpg";
            string baseBitmapPath = TestUtil.GetTestFile("bitmaps\\" + baseBitmapName);

            SKBitmap skBitmap = SKBitmap.Decode(baseBitmapPath);
            SKPixmap skPixmap = skBitmap.PeekPixels();
            IGraphicsBitmap skiaPixmap = new Skia_Pixmap(skPixmap);

            int width = skPixmap.Width;
            int height = skPixmap.Height;

            RenderingUtil.RenderingTest(width, new System.Drawing.RectangleF(0, 0, width, height), false, expectedResult,
                grTarget => {
                    grTarget.DrawBitmapPart(skiaPixmap,
                                            100, 130, 400, 200,
                                            new System.Drawing.RectangleF(20, 10, 500, 400),
                                            BitmapScaling.HighQuality);
                }
            );
        }

        [Test]
        public void CropSkiaPixmap()
        {
            string expectedResult = TestUtil.GetTestFile("bitmaps\\CropSkiaBitmap_baseline.png");

            const string baseBitmapName = "Water lilies.jpg";
            string baseBitmapPath = TestUtil.GetTestFile("bitmaps\\" + baseBitmapName);

            SKBitmap skBitmap = SKBitmap.Decode(baseBitmapPath);
            SKPixmap skPixmap = skBitmap.PeekPixels();
            IGraphicsBitmap skiaPixmap = new Skia_Pixmap(skPixmap);

            IGraphicsBitmap croppedPixmap = skiaPixmap.Crop(100, 130, 450, 250);
            Assert.AreEqual(450, croppedPixmap.PixelWidth);
            Assert.AreEqual(250, croppedPixmap.PixelHeight);

            RenderingUtil.RenderingTest(croppedPixmap.PixelWidth, new System.Drawing.RectangleF(0, 0, croppedPixmap.PixelWidth, croppedPixmap.PixelHeight), false, expectedResult,
                grTarget => {
                    grTarget.DrawBitmap(croppedPixmap,
                                        new System.Drawing.RectangleF(0, 0, croppedPixmap.PixelWidth, croppedPixmap.PixelHeight),
                                        BitmapScaling.HighQuality);
                }
            );
        }

        [Test]
        public void WriteSkiaPixmapToStream()
        {
            string expectedResult = TestUtil.GetTestFile("bitmaps\\WriteSkiaBitmapToStream_baseline.png");

            const string baseBitmapName = "Water lilies.jpg";
            string baseBitmapPath = TestUtil.GetTestFile("bitmaps\\" + baseBitmapName);

            SKBitmap skBitmap = SKBitmap.Decode(baseBitmapPath);
            SKPixmap skPixmap = skBitmap.PeekPixels();
            IGraphicsBitmap skiaPixmap = new Skia_Pixmap(skPixmap);

            MemoryStream memStream = new MemoryStream();
            skiaPixmap.WriteToStream(GraphicsBitmapFormat.PNG, memStream, 100);
            memStream.Seek(0, SeekOrigin.Begin);

            SKBitmap loadedBitmap = SKBitmap.Decode(memStream);

            BitmapTestUtil.CompareBitmapBaseline(loadedBitmap, expectedResult);
        }

        [Test]
        public void SkiaBitmapGraphicsLoader()
        {
            string expectedResult = TestUtil.GetTestFile("bitmaps\\SkiaBitmapLoader_baseline.png");

            const string baseBitmapName = "Water lilies.jpg";
            string baseBitmapPath = TestUtil.GetTestFile("bitmaps\\" + baseBitmapName);
            int width, height;

            IGraphicsBitmap skiaBitmap;
            using (Stream stream = new FileStream(baseBitmapPath, FileMode.Open, FileAccess.Read)) {
                skiaBitmap = new SkiaBitmapGraphicsLoader().ReadBitmapFromStream(stream);
                width = skiaBitmap.PixelWidth;
                height = skiaBitmap.PixelHeight;
            }

            Assert.AreEqual(GraphicsBitmapFormat.JPEG, skiaBitmap.GetOriginalFormat());

            RenderingUtil.RenderingTest(width, new System.Drawing.RectangleF(0, 0, width, height), false, expectedResult,
                grTarget => {
                    grTarget.DrawBitmap(skiaBitmap, new System.Drawing.RectangleF(0, 0, width, height), BitmapScaling.HighQuality);
                }
            );
        }


        [Test]
        public void SkiaBitmapDefaultResolution()
        {
            // A Skia_Bitmap created without explicit resolution should default to 96 DPI.
            SKBitmap skBitmap = new SKBitmap(100, 80, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            Skia_Bitmap skiaBitmap = new Skia_Bitmap(skBitmap);

            Assert.AreEqual(96, skiaBitmap.HorizontalResolution);
            Assert.AreEqual(96, skiaBitmap.VerticalResolution);

            skiaBitmap.Dispose();
        }

        [Test]
        public void SkiaBitmapExplicitResolution()
        {
            // A Skia_Bitmap created with explicit resolution should store those values.
            SKBitmap skBitmap = new SKBitmap(100, 80, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            Skia_Bitmap skiaBitmap = new Skia_Bitmap(skBitmap, GraphicsBitmapFormat.PNG, 300, 150);

            Assert.AreEqual(300, skiaBitmap.HorizontalResolution);
            Assert.AreEqual(150, skiaBitmap.VerticalResolution);

            skiaBitmap.Dispose();
        }

        [Test]
        public void SkiaBitmapLoaderReadsResolutionFromJpeg()
        {
            // Load Waterfall.jpg (230 DPI) through SkiaBitmapGraphicsLoader and verify resolution.
            string jpegPath = TestUtil.GetTestFile("bitmaps\\Waterfall.jpg");

            IGraphicsBitmap loaded;
            using (Stream stream = new FileStream(jpegPath, FileMode.Open, FileAccess.Read)) {
                loaded = new SkiaBitmapGraphicsLoader().ReadBitmapFromStream(stream);
            }

            Assert.AreEqual(GraphicsBitmapFormat.JPEG, loaded.GetOriginalFormat());
            Assert.AreEqual(230, loaded.HorizontalResolution, 0.01);
            Assert.AreEqual(230, loaded.VerticalResolution, 0.01);

            loaded.Dispose();
        }

        [Test]
        public void SkiaBitmapLoaderReadsResolutionFromPng()
        {
            // Load Waterfall.png (230 DPI) through SkiaBitmapGraphicsLoader and verify resolution.
            string pngPath = TestUtil.GetTestFile("bitmaps\\Waterfall.png");

            IGraphicsBitmap loaded;
            using (Stream stream = new FileStream(pngPath, FileMode.Open, FileAccess.Read)) {
                loaded = new SkiaBitmapGraphicsLoader().ReadBitmapFromStream(stream);
            }

            Assert.AreEqual(GraphicsBitmapFormat.PNG, loaded.GetOriginalFormat());
            Assert.AreEqual(230, loaded.HorizontalResolution, 0.01);
            Assert.AreEqual(230, loaded.VerticalResolution, 0.01);

            loaded.Dispose();
        }

        [Test]
        public void SkiaBitmapCropPreservesResolution()
        {
            // Cropping a Skia_Bitmap should preserve the original resolution.
            string jpegPath = TestUtil.GetTestFile("bitmaps\\Waterfall.jpg");

            IGraphicsBitmap loaded;
            using (Stream stream = new FileStream(jpegPath, FileMode.Open, FileAccess.Read)) {
                loaded = new SkiaBitmapGraphicsLoader().ReadBitmapFromStream(stream);
            }

            IGraphicsBitmap cropped = loaded.Crop(10, 10, 50, 50);
            Assert.AreEqual(50, cropped.PixelWidth);
            Assert.AreEqual(50, cropped.PixelHeight);
            Assert.AreEqual(230, cropped.HorizontalResolution, 0.01);
            Assert.AreEqual(230, cropped.VerticalResolution, 0.01);

            cropped.Dispose();
            loaded.Dispose();
        }

        [Test]
        public void SkiaBitmapWriteToStreamPreservesResolution()
        {
            // Writing a Skia_Bitmap to a stream and reading it back should preserve resolution.
            string jpegPath = TestUtil.GetTestFile("bitmaps\\Waterfall.jpg");

            IGraphicsBitmap loaded;
            using (Stream stream = new FileStream(jpegPath, FileMode.Open, FileAccess.Read)) {
                loaded = new SkiaBitmapGraphicsLoader().ReadBitmapFromStream(stream);
            }

            // Write as PNG and read back to verify resolution is embedded.
            MemoryStream ms = new MemoryStream();
            loaded.WriteToStream(GraphicsBitmapFormat.PNG, ms, 100);
            ms.Position = 0;

            BitmapWithResolution readBack = BitmapIO.ReadBitmapFromStream(ms);
            Assert.AreEqual(230, readBack.HorizontalResolution, 0.01);
            Assert.AreEqual(230, readBack.VerticalResolution, 0.01);

            readBack.Bitmap.Dispose();
            loaded.Dispose();
        }

        [Test]
        public void SkiaImageDefaultResolution()
        {
            // A Skia_Image created without explicit resolution should default to 96 DPI.
            SKBitmap skBitmap = new SKBitmap(100, 80, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            SKImage skImage = SKImage.FromBitmap(skBitmap);
            Skia_Image skiaImage = new Skia_Image(skImage);

            Assert.AreEqual(96, skiaImage.HorizontalResolution);
            Assert.AreEqual(96, skiaImage.VerticalResolution);

            skiaImage.Dispose();
            skBitmap.Dispose();
        }

        [Test]
        public void SkiaImageExplicitResolution()
        {
            // A Skia_Image created with explicit resolution should store those values.
            SKBitmap skBitmap = new SKBitmap(100, 80, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            SKImage skImage = SKImage.FromBitmap(skBitmap);
            Skia_Image skiaImage = new Skia_Image(skImage, 200, 300);

            Assert.AreEqual(200, skiaImage.HorizontalResolution);
            Assert.AreEqual(300, skiaImage.VerticalResolution);

            skiaImage.Dispose();
            skBitmap.Dispose();
        }

        [Test]
        public void SkiaImageCropPreservesResolution()
        {
            // Cropping a Skia_Image should preserve the original resolution.
            SKBitmap skBitmap = new SKBitmap(100, 80, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            SKImage skImage = SKImage.FromBitmap(skBitmap);
            Skia_Image skiaImage = new Skia_Image(skImage, 250, 175);

            IGraphicsBitmap cropped = skiaImage.Crop(10, 10, 50, 40);
            Assert.AreEqual(50, cropped.PixelWidth);
            Assert.AreEqual(40, cropped.PixelHeight);
            Assert.AreEqual(250, cropped.HorizontalResolution, 0.01);
            Assert.AreEqual(175, cropped.VerticalResolution, 0.01);

            cropped.Dispose();
            skiaImage.Dispose();
            skBitmap.Dispose();
        }

        [Test]
        public void SkiaImageWriteToStreamPreservesResolution()
        {
            // Writing a Skia_Image to a stream and reading back should preserve resolution.
            SKBitmap skBitmap = new SKBitmap(100, 80, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            SKImage skImage = SKImage.FromBitmap(skBitmap);
            Skia_Image skiaImage = new Skia_Image(skImage, 350, 275);

            MemoryStream ms = new MemoryStream();
            skiaImage.WriteToStream(GraphicsBitmapFormat.PNG, ms, 100);
            ms.Position = 0;

            BitmapWithResolution readBack = BitmapIO.ReadBitmapFromStream(ms);
            Assert.AreEqual(350, readBack.HorizontalResolution, 0.01);
            Assert.AreEqual(275, readBack.VerticalResolution, 0.01);

            readBack.Bitmap.Dispose();
            skiaImage.Dispose();
            skBitmap.Dispose();
        }

        [Test]
        public void SkiaPixmapDefaultResolution()
        {
            // A Skia_Pixmap created without explicit resolution should default to 96 DPI.
            SKBitmap skBitmap = new SKBitmap(100, 80, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            SKPixmap skPixmap = skBitmap.PeekPixels();
            Skia_Pixmap skiaPixmap = new Skia_Pixmap(skPixmap);

            Assert.AreEqual(96, skiaPixmap.HorizontalResolution);
            Assert.AreEqual(96, skiaPixmap.VerticalResolution);

            skiaPixmap.Dispose();
            skBitmap.Dispose();
        }

        [Test]
        public void SkiaPixmapExplicitResolution()
        {
            // A Skia_Pixmap created with explicit resolution should store those values.
            SKBitmap skBitmap = new SKBitmap(100, 80, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            SKPixmap skPixmap = skBitmap.PeekPixels();
            Skia_Pixmap skiaPixmap = new Skia_Pixmap(skPixmap, 400, 350);

            Assert.AreEqual(400, skiaPixmap.HorizontalResolution);
            Assert.AreEqual(350, skiaPixmap.VerticalResolution);

            skiaPixmap.Dispose();
            skBitmap.Dispose();
        }

        [Test]
        public void SkiaPixmapCropPreservesResolution()
        {
            // Cropping a Skia_Pixmap should preserve the original resolution.
            SKBitmap skBitmap = new SKBitmap(100, 80, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            SKPixmap skPixmap = skBitmap.PeekPixels();
            Skia_Pixmap skiaPixmap = new Skia_Pixmap(skPixmap, 180, 220);

            IGraphicsBitmap cropped = skiaPixmap.Crop(5, 5, 60, 40);
            Assert.AreEqual(60, cropped.PixelWidth);
            Assert.AreEqual(40, cropped.PixelHeight);
            Assert.AreEqual(180, cropped.HorizontalResolution, 0.01);
            Assert.AreEqual(220, cropped.VerticalResolution, 0.01);

            cropped.Dispose();
            // Don't dispose skiaPixmap here — it shares memory with skBitmap via PeekPixels.
            skBitmap.Dispose();
        }

        [Test]
        public void SkiaPixmapWriteToStreamPreservesResolution()
        {
            // Writing a Skia_Pixmap to a stream and reading back should preserve resolution.
            SKBitmap skBitmap = new SKBitmap(100, 80, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            using (SKCanvas canvas = new SKCanvas(skBitmap)) {
                canvas.Clear(SKColors.Red);
            }
            SKPixmap skPixmap = skBitmap.PeekPixels();
            Skia_Pixmap skiaPixmap = new Skia_Pixmap(skPixmap, 500, 400);

            MemoryStream ms = new MemoryStream();
            skiaPixmap.WriteToStream(GraphicsBitmapFormat.PNG, ms, 100);
            ms.Position = 0;

            BitmapWithResolution readBack = BitmapIO.ReadBitmapFromStream(ms);
            Assert.AreEqual(500, readBack.HorizontalResolution, 0.01);
            Assert.AreEqual(400, readBack.VerticalResolution, 0.01);

            readBack.Bitmap.Dispose();
            skiaPixmap.Dispose();
            skBitmap.Dispose();
        }

        [Test]
        public void SkiaBitmapCropThenWritePreservesResolution()
        {
            // Full round-trip: load with resolution, crop, write, read back — resolution should survive.
            string pngPath = TestUtil.GetTestFile("bitmaps\\Waterfall.png");

            IGraphicsBitmap loaded;
            using (Stream stream = new FileStream(pngPath, FileMode.Open, FileAccess.Read)) {
                loaded = new SkiaBitmapGraphicsLoader().ReadBitmapFromStream(stream);
            }

            IGraphicsBitmap cropped = loaded.Crop(5, 5, 30, 30);

            MemoryStream ms = new MemoryStream();
            cropped.WriteToStream(GraphicsBitmapFormat.PNG, ms, 100);
            ms.Position = 0;

            BitmapWithResolution readBack = BitmapIO.ReadBitmapFromStream(ms);
            Assert.AreEqual(30, readBack.Bitmap.Width);
            Assert.AreEqual(30, readBack.Bitmap.Height);
            Assert.AreEqual(230, readBack.HorizontalResolution, 0.01);
            Assert.AreEqual(230, readBack.VerticalResolution, 0.01);

            readBack.Bitmap.Dispose();
            cropped.Dispose();
            loaded.Dispose();
        }

        [Test]
        public void ReadBitmapFromJpegStream()
        {
            // Read a JPEG file and verify format, dimensions, and pixel format.
            string jpegPath = TestUtil.GetTestFile("bitmaps\\Water lilies.jpg");

            BitmapWithResolution result;
            using (Stream stream = new FileStream(jpegPath, FileMode.Open, FileAccess.Read)) {
                result = BitmapIO.ReadBitmapFromStream(stream);
            }

            Assert.AreEqual(GraphicsBitmapFormat.JPEG, result.Format);
            Assert.Greater(result.Bitmap.Width, 0);
            Assert.Greater(result.Bitmap.Height, 0);
            Assert.AreEqual(SKImageInfo.PlatformColorType, result.Bitmap.ColorType);
            Assert.AreEqual(SKAlphaType.Premul, result.Bitmap.AlphaType);

            result.Bitmap.Dispose();
        }

        [Test]
        public void ReadBitmapResolution()
        {
            // Read a JPEG and verify that resolution values are reasonable.
            string jpegPath = TestUtil.GetTestFile("bitmaps\\Water lilies.jpg");

            BitmapWithResolution result;
            using (Stream stream = new FileStream(jpegPath, FileMode.Open, FileAccess.Read)) {
                result = BitmapIO.ReadBitmapFromStream(stream);
            }

            // JPEG files typically have 72 or 96 DPI.
            Assert.Greater(result.HorizontalResolution, 0);
            Assert.Greater(result.VerticalResolution, 0);
            Assert.LessOrEqual(result.HorizontalResolution, 1200);
            Assert.LessOrEqual(result.VerticalResolution, 1200);

            result.Bitmap.Dispose();
        }

        [Test]
        public void ReadBitmapFromPngStream()
        {
            // Write a PNG via BitmapIO, then read it back and verify format.
            SKBitmap bitmap = CreateTestBitmap(100, 80);
            BitmapWithResolution bwr = new BitmapWithResolution(bitmap, GraphicsBitmapFormat.PNG, 96, 96);

            MemoryStream ms = new MemoryStream();
            BitmapIO.WriteBitmapToStream(bwr, ms, 100);
            ms.Position = 0;

            BitmapWithResolution result = BitmapIO.ReadBitmapFromStream(ms);

            Assert.AreEqual(GraphicsBitmapFormat.PNG, result.Format);
            Assert.AreEqual(100, result.Bitmap.Width);
            Assert.AreEqual(80, result.Bitmap.Height);
            Assert.AreEqual(SKImageInfo.PlatformColorType, result.Bitmap.ColorType);
            Assert.AreEqual(SKAlphaType.Premul, result.Bitmap.AlphaType);

            result.Bitmap.Dispose();
            bitmap.Dispose();
        }

        [Test]
        public void WriteBitmapToPngStream()
        {
            // Write a bitmap as PNG, read it back with System.Drawing, verify it is valid.
            SKBitmap bitmap = CreateTestBitmap(120, 90);
            BitmapWithResolution bwr = new BitmapWithResolution(bitmap, GraphicsBitmapFormat.PNG, 150, 150);

            MemoryStream ms = new MemoryStream();
            BitmapIO.WriteBitmapToStream(bwr, ms, 100);
            ms.Position = 0;

            SKBitmap loadedBitmap = SKBitmap.Decode(ms);
            Assert.AreEqual(120, loadedBitmap.Width);
            Assert.AreEqual(90, loadedBitmap.Height);

            loadedBitmap.Dispose();
            bitmap.Dispose();
        }

        [Test]
        public void WriteBitmapToJpegStream()
        {
            // Write a bitmap as JPEG, read it back, verify it is valid.
            SKBitmap bitmap = CreateTestBitmap(120, 90);
            BitmapWithResolution bwr = new BitmapWithResolution(bitmap, GraphicsBitmapFormat.JPEG, 200, 200);

            MemoryStream ms = new MemoryStream();
            BitmapIO.WriteBitmapToStream(bwr, ms, 85);
            ms.Position = 0;

            SKBitmap loadedBitmap = SKBitmap.Decode(ms);
            Assert.AreEqual(120, loadedBitmap.Width);
            Assert.AreEqual(90, loadedBitmap.Height);

            loadedBitmap.Dispose();
            bitmap.Dispose();
        }

        [Test]
        public void WriteBitmapToGifStream()
        {
            // Writing GIF is a key use case for BitmapIO (Skia can't do this).
            SKBitmap bitmap = CreateTestBitmap(120, 90);
            BitmapWithResolution bwr = new BitmapWithResolution(bitmap, GraphicsBitmapFormat.GIF, 96, 96);

            MemoryStream ms = new MemoryStream();
            BitmapIO.WriteBitmapToStream(bwr, ms, 100);
            ms.Position = 0;

            // Verify the stream contains a valid GIF by reading it back.
            SKBitmap loadedBitmap;

            using (SKManagedStream skStream = new SKManagedStream(ms, disposeManagedStream: false))
            {
                loadedBitmap = SKBitmap.Decode(skStream);
            }
            Assert.AreEqual(120, loadedBitmap.Width);
            Assert.AreEqual(90, loadedBitmap.Height);

            // Also verify BitmapIO reads it back as GIF format.
            ms.Position = 0;
            BitmapWithResolution readBack = BitmapIO.ReadBitmapFromStream(ms);
            Assert.AreEqual(GraphicsBitmapFormat.GIF, readBack.Format);

            readBack.Bitmap.Dispose();
            loadedBitmap.Dispose();
            bitmap.Dispose();
        }

        [Test]
        public void WriteBitmapToTiffStream()
        {
            // TIFF is another format Skia doesn't handle well.
            SKBitmap bitmap = CreateTestBitmap(100, 80);
            BitmapWithResolution bwr = new BitmapWithResolution(bitmap, GraphicsBitmapFormat.TIFF, 300, 300);

            MemoryStream ms = new MemoryStream();
            BitmapIO.WriteBitmapToStream(bwr, ms, 100);
            ms.Position = 0;

            // Read back and verify format.
            BitmapWithResolution readBack = BitmapIO.ReadBitmapFromStream(ms);
            Assert.AreEqual(GraphicsBitmapFormat.TIFF, readBack.Format);
            Assert.AreEqual(100, readBack.Bitmap.Width);
            Assert.AreEqual(80, readBack.Bitmap.Height);

            readBack.Bitmap.Dispose();
            bitmap.Dispose();
        }

        [Test]
        public void ResolutionPreservedPng()
        {
            // Write a PNG with specific DPI, read it back, verify the DPI is preserved.
            SKBitmap bitmap = CreateTestBitmap(100, 80);
            BitmapWithResolution bwr = new BitmapWithResolution(bitmap, GraphicsBitmapFormat.PNG, 300, 250);

            MemoryStream ms = new MemoryStream();
            BitmapIO.WriteBitmapToStream(bwr, ms, 100);
            ms.Position = 0;

            BitmapWithResolution readBack = BitmapIO.ReadBitmapFromStream(ms);
            Assert.AreEqual(300, readBack.HorizontalResolution, 0.01);
            Assert.AreEqual(250, readBack.VerticalResolution, 0.01);

            readBack.Bitmap.Dispose();
            bitmap.Dispose();
        }

        [Test]
        public void ResolutionPreservedJpeg()
        {
            // Write a JPEG with specific DPI, read it back, verify the DPI is preserved.
            SKBitmap bitmap = CreateTestBitmap(100, 80);
            BitmapWithResolution bwr = new BitmapWithResolution(bitmap, GraphicsBitmapFormat.JPEG, 150, 200);

            MemoryStream ms = new MemoryStream();
            BitmapIO.WriteBitmapToStream(bwr, ms, 90);
            ms.Position = 0;

            BitmapWithResolution readBack = BitmapIO.ReadBitmapFromStream(ms);
            Assert.AreEqual(150, readBack.HorizontalResolution, 0.01);
            Assert.AreEqual(200, readBack.VerticalResolution, 0.01);

            readBack.Bitmap.Dispose();
            bitmap.Dispose();
        }

        [Test]
        public void WritePixmapToStream()
        {
            // Write a pixmap as PNG using WritePixmapToStream, read it back, verify.
            SKBitmap bitmap = CreateTestBitmap(100, 80);
            using (SKPixmap pixmap = bitmap.PeekPixels()) {
                PixmapWithResolution pwr = new PixmapWithResolution(pixmap, GraphicsBitmapFormat.PNG, 200, 200);

                MemoryStream ms = new MemoryStream();
                BitmapIO.WritePixmapToStream(pwr, ms, 100);
                ms.Position = 0;

                BitmapWithResolution readBack = BitmapIO.ReadBitmapFromStream(ms);
                Assert.AreEqual(GraphicsBitmapFormat.PNG, readBack.Format);
                Assert.AreEqual(100, readBack.Bitmap.Width);
                Assert.AreEqual(80, readBack.Bitmap.Height);
                Assert.AreEqual(200, readBack.HorizontalResolution, 0.01);
                Assert.AreEqual(200, readBack.VerticalResolution, 0.01);

                readBack.Bitmap.Dispose();
            }
            bitmap.Dispose();
        }

        [Test]
        public void WritePixmapToGifStream()
        {
            // Write a pixmap as GIF, verify it works.
            SKBitmap bitmap = CreateTestBitmap(80, 60);
            using (SKPixmap pixmap = bitmap.PeekPixels()) {
                PixmapWithResolution pwr = new PixmapWithResolution(pixmap, GraphicsBitmapFormat.GIF, 96, 96);

                MemoryStream ms = new MemoryStream();
                BitmapIO.WritePixmapToStream(pwr, ms, 100);
                ms.Position = 0;

                BitmapWithResolution readBack = BitmapIO.ReadBitmapFromStream(ms);
                Assert.AreEqual(GraphicsBitmapFormat.GIF, readBack.Format);
                Assert.AreEqual(80, readBack.Bitmap.Width);
                Assert.AreEqual(60, readBack.Bitmap.Height);

                readBack.Bitmap.Dispose();
            }
            bitmap.Dispose();
        }

        [Test]
        public void ReadBitmapFromNonSeekableStream()
        {
            // BitmapIO.ReadBitmapFromStream should handle non-seekable streams.
            string jpegPath = TestUtil.GetTestFile("bitmaps\\Water lilies.jpg");
            byte[] data = File.ReadAllBytes(jpegPath);

            using (NonSeekableStream ns = new NonSeekableStream(new MemoryStream(data))) {
                BitmapWithResolution result = BitmapIO.ReadBitmapFromStream(ns);

                Assert.AreEqual(GraphicsBitmapFormat.JPEG, result.Format);
                Assert.Greater(result.Bitmap.Width, 0);
                Assert.Greater(result.Bitmap.Height, 0);

                result.Bitmap.Dispose();
            }
        }

        [Test]
        public void HandlesDifferentBitmapColorTypes()
        {
            // WriteBitmapToStream should handle a bitmap with a non-platform color type.
            SKBitmap bitmap = new SKBitmap(50, 40, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            using (SKCanvas canvas = new SKCanvas(bitmap)) {
                canvas.Clear(SKColors.Green);
            }

            BitmapWithResolution bwr = new BitmapWithResolution(bitmap, GraphicsBitmapFormat.PNG, 96, 96);

            MemoryStream ms = new MemoryStream();
            BitmapIO.WriteBitmapToStream(bwr, ms, 100);
            ms.Position = 0;

            BitmapWithResolution readBack = BitmapIO.ReadBitmapFromStream(ms);
            Assert.AreEqual(50, readBack.Bitmap.Width);
            Assert.AreEqual(40, readBack.Bitmap.Height);

            readBack.Bitmap.Dispose();
            bitmap.Dispose();
        }

        /// <summary>
        /// A stream wrapper that does not support seeking, for testing non-seekable stream handling.
        /// </summary>
        private class NonSeekableStream : Stream
        {
            private Stream inner;

            public NonSeekableStream(Stream inner) { this.inner = inner; }

            public override bool CanRead => inner.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }
            public override void Flush() { inner.Flush(); }
            public override int Read(byte[] buffer, int offset, int count) { return inner.Read(buffer, offset, count); }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
            public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    inner.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
