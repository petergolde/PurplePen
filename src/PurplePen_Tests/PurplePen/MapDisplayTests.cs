#if TEST
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using TestingUtils;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using PurplePen.Graphics2D;
using PurplePen.MapModel;
using SkiaSharp;

namespace PurplePen.Tests
{
    [TestClass]
    public sealed class MapDisplayTests: TestFixtureBase, IDisposable
    {
        Matrix transform;
        SKBitmap bitmap;

        void SetupBitmap()
        {
            bitmap = new SKBitmap(400, 400);
            transform = new Matrix();
            transform.Translate(0, bitmap.Height);
            transform.Scale(8F, -8F);
            transform.Translate(-50F, -170F);
        }

        void Dispose(bool disposing)
        {
            if (disposing) {
                bitmap?.Dispose();
                bitmap = null;
                transform?.Dispose();
                transform = null;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
        }

        Bitmap GetBitmap()
        {
            return TestRenderingUtils.GdiPlusBitmapFromSkBitmap(bitmap);
        }


        void DrawToBitmap(MapDisplay mapdisplay, RectangleF clip)
        {
            Matrix inverse = transform.Clone();
            inverse.Invert();
            clip = Geometry.TransformRectangle(inverse, clip);
            mapdisplay.Draw(new Skia_Bitmap(bitmap), transform, clip);
       }

        [TestMethod]
        public void OcadBoundsScale()
        {
            MapDisplay mapDisplay = new MapDisplay() ;

            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.ocd"));
            Assert.AreEqual(15000, mapDisplay.MapScale);
            TestUtil.AssertEqualRect(RectangleF.FromLTRB(-0.22F, 0.01F, 199.85F, 267.8F), mapDisplay.MapBounds, 0.01F, "map bounds");
        }

        [TestMethod]
        public void BitmapBounds()
        {
            MapDisplay mapDisplay = new MapDisplay();

            mapDisplay.SetMapFile(MapType.Bitmap, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.jpg"));
            mapDisplay.Dpi = 300;
            TestUtil.AssertEqualRect(RectangleF.FromLTRB(0F, 0F, 201.08F, 269.16F), mapDisplay.MapBounds, 0.01F, "map bounds");
        }

        [TestMethod]
        public void BasicOcadMap()
        {
            SetupBitmap();
            MapDisplay mapDisplay = new MapDisplay();

            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.ocd"));
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 1.0F;

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);
            DrawToBitmap(mapDisplay, drawRect);

            using (Bitmap bm = GetBitmap()) {
                BitmapTestUtil.CompareBitmapBaseline(bm, TestUtil.GetTestFile(@"mapdisplay\BasicOcadMap.png"));
            }
        }

        [TestMethod]
        public void BasicBitmapMap()
        {
            SetupBitmap();
            MapDisplay mapDisplay = new MapDisplay();

            mapDisplay.SetMapFile(MapType.Bitmap, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.jpg"));
            mapDisplay.Dpi = 300;
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 1.0F;

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);
            DrawToBitmap(mapDisplay, drawRect);

            using (Bitmap bm = GetBitmap()) {
                BitmapTestUtil.CompareBitmapBaseline(bm, TestUtil.GetTestFile(@"mapdisplay\BasicBitmapMap.png"));
            }
        }

        [TestMethod]
        public void AntialiasOcadMap()
        {
            SetupBitmap();
            MapDisplay mapDisplay = new MapDisplay();


            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.ocd"));
            mapDisplay.AntiAlias = true;
            mapDisplay.MapIntensity = 1.0F;

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);
            DrawToBitmap(mapDisplay, drawRect);

            using (Bitmap bm = GetBitmap()) {
                BitmapTestUtil.CompareBitmapBaseline(bm, TestUtil.GetTestFile(@"mapdisplay\AntialiasOcadMap.png"));
            }
        }

        [TestMethod]
        public void AntialiasBitmapMap()
        {
            SetupBitmap();
            MapDisplay mapDisplay = new MapDisplay();


            mapDisplay.SetMapFile(MapType.Bitmap, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.jpg"));
            mapDisplay.Dpi = 300;
            mapDisplay.AntiAlias = true;
            mapDisplay.MapIntensity = 1.0F;

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);
            DrawToBitmap(mapDisplay, drawRect);

            using (Bitmap bm = GetBitmap()) {
                BitmapTestUtil.CompareBitmapBaseline(bm, TestUtil.GetTestFile(@"mapdisplay\AntialiasBitmapMap.png"));
            }
        }

        [TestMethod]
        public void IntensityOcadMap()
        {
            SetupBitmap();
            MapDisplay mapDisplay = new MapDisplay();


            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.ocd"));
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 0.3F;

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);
            DrawToBitmap(mapDisplay, drawRect);

            using (Bitmap bm = GetBitmap()) {
                BitmapTestUtil.CompareBitmapBaseline(bm, TestUtil.GetTestFile(@"mapdisplay\IntensityOcadMap.png"));
            }
        }

        [TestMethod]
        public void IntensityBitmapMap()
        {
            SetupBitmap();
            MapDisplay mapDisplay = new MapDisplay();


            mapDisplay.SetMapFile(MapType.Bitmap, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.jpg"));
            mapDisplay.Dpi = 300;
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 0.3F;

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);
            DrawToBitmap(mapDisplay, drawRect);

            using (Bitmap bm = GetBitmap()) {
                BitmapTestUtil.CompareBitmapBaseline(bm, TestUtil.GetTestFile(@"mapdisplay\IntensityBitmapMap.png"));
            }
        }

        [TestMethod]
        public void CloneOcad()
        {
            MapDisplay mapDisplay = new MapDisplay();

            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.ocd"));
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 0.3F;

            MapDisplay cloned = mapDisplay.Clone();

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);

            SetupBitmap();
            cloned.AntiAlias = true;
            cloned.MapIntensity = 1.0F;
            DrawToBitmap(cloned, drawRect);

            using (Bitmap bm = GetBitmap()) {
                BitmapTestUtil.CompareBitmapBaseline(bm, TestUtil.GetTestFile(@"mapdisplay\ClonedOcadMap.png"));
            }

            SetupBitmap();
            DrawToBitmap(mapDisplay, drawRect);

            using (Bitmap bm = GetBitmap()) {
                BitmapTestUtil.CompareBitmapBaseline(bm, TestUtil.GetTestFile(@"mapdisplay\NonclonedOcadMap.png"));
            }
        }

        [TestMethod]
        public void CloneBitmap()
        {
            MapDisplay mapDisplay = new MapDisplay();

            mapDisplay.SetMapFile(MapType.Bitmap, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.jpg"));
            mapDisplay.Dpi = 300;
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 0.3F;

            MapDisplay cloned = mapDisplay.Clone();

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);

            SetupBitmap();
            cloned.AntiAlias = true;
            cloned.MapIntensity = 1.0F;
            DrawToBitmap(cloned, drawRect);

            using (Bitmap bm = GetBitmap()) {
                BitmapTestUtil.CompareBitmapBaseline(bm, TestUtil.GetTestFile(@"mapdisplay\ClonedBitmapMap.png"));
            }

            SetupBitmap();
            DrawToBitmap(mapDisplay, drawRect);

            using (Bitmap bm = GetBitmap()) {
                BitmapTestUtil.CompareBitmapBaseline(bm, TestUtil.GetTestFile(@"mapdisplay\NonclonedBitmapMap.png"));
            }
        }

        [TestMethod]
        public void OverprintOcadMap()
        {
            SetupBitmap();
            MapDisplay mapDisplay = new MapDisplay();


            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"mapdisplay\overprint.ocd"));
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 1F;
            mapDisplay.OcadOverprintEffect = true;

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);
            DrawToBitmap(mapDisplay, drawRect);

            using (Bitmap bm = GetBitmap()) {
                BitmapTestUtil.CompareBitmapBaseline(bm, TestUtil.GetTestFile(@"mapdisplay\OverprintOcadMap.png"));
            }
        }


        [TestMethod]
        public void NoOverprintOcadMap()
        {
            SetupBitmap();
            MapDisplay mapDisplay = new MapDisplay();


            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"mapdisplay\overprint.ocd"));
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 1F;
            mapDisplay.OcadOverprintEffect = false;

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);
            DrawToBitmap(mapDisplay, drawRect);

            using (Bitmap bm = GetBitmap()) {
                BitmapTestUtil.CompareBitmapBaseline(bm, TestUtil.GetTestFile(@"mapdisplay\NoOverprintOcadMap.png"));
            }
        }


    }
}

#endif //TEST
