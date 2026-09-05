#if TEST
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using TestingUtils;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Threading;

namespace PurplePen.Tests
{
    [TestClass]
    public class MapUtilTests : TestFixtureBase
    {

        [TestMethod]
        public void ValidateMapFileOCAD()
        {
            float scale, dpi;
            Size bitmapSize;
            RectangleF mapBounds;
            MapType mapType;
            string errorMessageText;
            int? lowerPurpleLayer;
            bool result;

            result = CoreMapUtil.ValidateMapFile(TestUtil.GetTestFile("mapdisplay\\SampleEvent.ocd"), out scale, out dpi, out bitmapSize, out mapBounds, out mapType, out lowerPurpleLayer, out errorMessageText);
            Assert.IsTrue(result);
            Assert.AreEqual(MapType.OCAD, mapType);
            Assert.AreEqual(15000, scale);
            Assert.AreEqual(-0.22F, mapBounds.Left, 0.01F);
            Assert.AreEqual(0.01F, mapBounds.Top, 0.01F);
            Assert.AreEqual(200.07F, mapBounds.Width, 0.01F);
            Assert.AreEqual(267.79F, mapBounds.Height, 0.01F);
            Assert.IsNull(lowerPurpleLayer);

            result = CoreMapUtil.ValidateMapFile(TestUtil.GetTestFile("mapdisplay\\overprint.ocd"), out scale, out dpi, out bitmapSize, out mapBounds, out mapType, out lowerPurpleLayer, out errorMessageText);
            Assert.IsTrue(result);
            Assert.AreEqual(MapType.OCAD, mapType);
            Assert.AreEqual(10000, scale);
            Assert.AreEqual(36.75F, mapBounds.Left, 0.01F);
            Assert.AreEqual(169.43F, mapBounds.Top, 0.01F);
            Assert.AreEqual(112.77F, mapBounds.Right, 0.01F);
            Assert.AreEqual(214.96F, mapBounds.Bottom, 0.01F);
            Assert.IsNull(lowerPurpleLayer);

            result = CoreMapUtil.ValidateMapFile(TestUtil.GetTestFile("courseprinting\\LordHill_ver16_2024Jan_scaled.omap"), out scale, out dpi, out bitmapSize, out mapBounds, out mapType, out lowerPurpleLayer, out errorMessageText);
            Assert.IsTrue(result);
            Assert.AreEqual(MapType.OCAD, mapType);
            Assert.AreEqual(10000, scale);
            Assert.AreEqual(-303.74F, mapBounds.Left, 0.01F);
            Assert.AreEqual(-406.05F, mapBounds.Top, 0.01F);
            Assert.AreEqual(395.76F, mapBounds.Right, 0.01F);
            Assert.AreEqual(133.40F, mapBounds.Bottom, 0.01F);
            Assert.AreEqual(10, lowerPurpleLayer);

        }

        [TestMethod]
        public void ValidateMapFileBitmap()
        {
            float scale, dpi;
            Size bitmapSize;
            RectangleF mapBounds;
            MapType mapType;
            string errorMessageText;
            int? lowerPurpleLayer;
            bool result;

            result = CoreMapUtil.ValidateMapFile(TestUtil.GetTestFile("mapdisplay\\SampleEvent.jpg"), out scale, out dpi, out bitmapSize, out mapBounds, out mapType, out lowerPurpleLayer, out errorMessageText);
            Assert.IsTrue(result);
            Assert.AreEqual(MapType.Bitmap, mapType);
            Assert.AreEqual(96, dpi, 0.1F);
            Assert.AreEqual(0F, mapBounds.Left, 0.01F);
            Assert.AreEqual(0F, mapBounds.Top, 0.01F);
            Assert.AreEqual(628.39F, mapBounds.Width, 0.01F);
            Assert.AreEqual(841.11F, mapBounds.Height, 0.01F);
            Assert.IsNull(lowerPurpleLayer);
        }


        [TestMethod]
        public void ValidateMapFilePDF()
        {
            float scale, dpi;
            Size bitmapSize;
            RectangleF mapBounds;
            MapType mapType;
            string errorMessageText;
            int? lowerPurpleLayer;
            bool result;

            result = CoreMapUtil.ValidateMapFile(TestUtil.GetTestFile("pdfmaps\\Potholes.pdf"), out scale, out dpi, out bitmapSize, out mapBounds, out mapType, out lowerPurpleLayer, out errorMessageText);
            Assert.IsTrue(result);
            Assert.AreEqual(MapType.PDF, mapType);
            Assert.AreEqual(600, dpi, 0.1F);
            Assert.AreEqual(0F, mapBounds.Left, 0.01F);
            Assert.AreEqual(0F, mapBounds.Top, 0.01F);
            Assert.AreEqual(215.9F, mapBounds.Width, 0.01F);
            Assert.AreEqual(279.4F, mapBounds.Height, 0.01F);
            Assert.IsNull(lowerPurpleLayer);
        }

        [TestMethod]
        public void GetDefaultPageSizeMetric()
        {
            int pageWidth, pageHeight, pageMargins;
            bool landscape;
            CultureInfo currentCulture;

            currentCulture = Thread.CurrentThread.CurrentCulture;
            try {
                CultureInfo.CurrentCulture.ClearCachedData();
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

                Assert.IsTrue(Util.IsCurrentCultureMetric());

                CoreMapUtil.GetDefaultPageSize(new RectangleF(30, 50, 350, 210), 1.0F, out pageWidth, out pageHeight, out pageMargins, out landscape);
                Assert.AreEqual(1169, pageWidth);
                Assert.AreEqual(1654, pageHeight);
                Assert.AreEqual(28, pageMargins);
                Assert.IsTrue(landscape);

                CoreMapUtil.GetDefaultPageSize(new RectangleF(30, 50, 290, 210), 1.0F, out pageWidth, out pageHeight, out pageMargins, out landscape);
                Assert.AreEqual(827, pageWidth);
                Assert.AreEqual(1169, pageHeight);
                Assert.AreEqual(0, pageMargins);
                Assert.IsTrue(landscape);

                CoreMapUtil.GetDefaultPageSize(new RectangleF(30, 50, 190, 270), 1.0F, out pageWidth, out pageHeight, out pageMargins, out landscape);
                Assert.AreEqual(827, pageWidth);
                Assert.AreEqual(1169, pageHeight);
                Assert.AreEqual(28, pageMargins);
                Assert.IsFalse(landscape);

                CoreMapUtil.GetDefaultPageSize(new RectangleF(30, 50, 1350, 2210), 1.0F, out pageWidth, out pageHeight, out pageMargins, out landscape);
                Assert.AreEqual(827, pageWidth);
                Assert.AreEqual(1169, pageHeight);
                Assert.AreEqual(0, pageMargins);
                Assert.IsFalse(landscape);

                CoreMapUtil.GetDefaultPageSize(new RectangleF(30, 50, 210, 296), 0.5F, out pageWidth, out pageHeight, out pageMargins, out landscape);
                Assert.AreEqual(1654, pageWidth);
                Assert.AreEqual(2339, pageHeight);
                Assert.AreEqual(0, pageMargins);
                Assert.IsFalse(landscape);



            }
            finally {
                CultureInfo.CurrentCulture.ClearCachedData();
                Thread.CurrentThread.CurrentCulture = currentCulture;
            }
        }

        [TestMethod]
        public void GetDefaultPageSizeEnglish()
        {
            int pageWidth, pageHeight, pageMargins;
            bool landscape;
            CultureInfo currentCulture;

            currentCulture = Thread.CurrentThread.CurrentCulture;
            try {
                CultureInfo.CurrentCulture.ClearCachedData();
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                Assert.IsFalse(Util.IsCurrentCultureMetric());

                CoreMapUtil.GetDefaultPageSize(new RectangleF(30, 50, 350, 210), 1.0F, out pageWidth, out pageHeight, out pageMargins, out landscape);
                Assert.AreEqual(850, pageWidth);
                Assert.AreEqual(1400, pageHeight);
                Assert.AreEqual(0, pageMargins);
                Assert.IsTrue(landscape);

                CoreMapUtil.GetDefaultPageSize(new RectangleF(30, 50, 260, 190), 1.0F, out pageWidth, out pageHeight, out pageMargins, out landscape);
                Assert.AreEqual(850, pageWidth);
                Assert.AreEqual(1100, pageHeight);
                Assert.AreEqual(25, pageMargins);
                Assert.IsTrue(landscape);

                CoreMapUtil.GetDefaultPageSize(new RectangleF(30, 50, 200, 270), 1.0F, out pageWidth, out pageHeight, out pageMargins, out landscape);
                Assert.AreEqual(850, pageWidth);
                Assert.AreEqual(1100, pageHeight);
                Assert.AreEqual(0, pageMargins);
                Assert.IsFalse(landscape);

                CoreMapUtil.GetDefaultPageSize(new RectangleF(30, 50, 1350, 2210), 1.0F, out pageWidth, out pageHeight, out pageMargins, out landscape);
                Assert.AreEqual(850, pageWidth);
                Assert.AreEqual(1100, pageHeight);
                Assert.AreEqual(0, pageMargins);
                Assert.IsFalse(landscape);

                CoreMapUtil.GetDefaultPageSize(new RectangleF(30, 50, 125, 200), 0.5F, out pageWidth, out pageHeight, out pageMargins, out landscape);
                Assert.AreEqual(1100, pageWidth);
                Assert.AreEqual(1700, pageHeight);
                Assert.AreEqual(25, pageMargins);
                Assert.IsFalse(landscape);



            }
            finally {
                Thread.CurrentThread.CurrentCulture = currentCulture;
            }
        }

    }
}

#endif //TEST
