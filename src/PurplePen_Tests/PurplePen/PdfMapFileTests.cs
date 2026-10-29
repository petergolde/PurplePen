using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using PurplePen.Graphics2D;
using TestingUtils;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PurplePen.Tests
{
    [TestClass]
    public class PdfMapFileTests
    {
        // Run an uncached conversion of pdfFile to a temporary PNG and wait for it to finish.
        // Returns the PdfMapFile, and the PNG file name in pngFile (the caller deletes it).
        private PdfMapFile ConvertAndWait(string pdfFile, int resolution, out string pngFile)
        {
            pngFile = Path.Combine(Path.GetTempPath(), $"PdfMapFileTests_{Guid.NewGuid():N}.png");

            PdfMapFile mapFile = new PdfMapFile(pdfFile);
            using (ManualResetEventSlim completed = new ManualResetEventSlim()) {
                mapFile.ConversionCompleted += delegate { completed.Set(); };

                PdfMapFile.ConversionStatus status = mapFile.BeginUncachedConversion(pngFile, resolution);
                Assert.AreEqual(PdfMapFile.ConversionStatus.Working, status);
                Assert.IsTrue(completed.Wait(TimeSpan.FromSeconds(60)), "ConversionCompleted was not raised.");
            }

            return mapFile;
        }

        [TestMethod]
        public void ConversionSucceeds()
        {
            PdfMapFile mapFile = ConvertAndWait(TestUtil.GetTestFile("pdfmaps\\Potholes.pdf"), 100, out string pngFile);

            try {
                Assert.AreEqual(PdfMapFile.ConversionStatus.Success, mapFile.Status);
                Assert.AreEqual(pngFile, mapFile.PngFileName);

                using (Stream stream = new FileStream(pngFile, FileMode.Open, FileAccess.Read)) {
                    IGraphicsBitmap bitmap = Services.BitmapLoader.ReadBitmapFromStream(stream);
                    Assert.AreEqual(100, Math.Round(bitmap.HorizontalResolution));
                    Assert.IsTrue(bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0);
                    bitmap.Dispose();
                }
            }
            finally {
                File.Delete(pngFile);
            }
        }

        [TestMethod]
        public void ConversionFailsOnCorruptPdf()
        {
            string corruptPdf = Path.Combine(Path.GetTempPath(), $"PdfMapFileTests_{Guid.NewGuid():N}.pdf");
            File.WriteAllText(corruptPdf, "This is not a PDF file.");

            try {
                PdfMapFile mapFile = ConvertAndWait(corruptPdf, 100, out string pngFile);

                Assert.AreEqual(PdfMapFile.ConversionStatus.Failure, mapFile.Status);
                Assert.IsFalse(string.IsNullOrWhiteSpace(mapFile.ConversionOutput));
                Assert.IsFalse(File.Exists(pngFile));
            }
            finally {
                File.Delete(corruptPdf);
            }
        }

        [TestMethod]
        public void GetCacheFileName()
        {
            string expectedHash = "B77F103F7AFB85896AE188BDAA2855A3E4A75CF3";

            string fileName = TestUtil.GetTestFile("pdfmaps\\Potholes.pdf");
            var pdfMap = new PdfMapFile(fileName);
            string pdfHash = pdfMap.GetCacheFileName(fileName);
            Assert.AreEqual(Path.Combine(Path.GetTempPath(), $"PurplePen\\{expectedHash}.png"), pdfMap.GetCacheFileName(fileName));
        }
    }
}
