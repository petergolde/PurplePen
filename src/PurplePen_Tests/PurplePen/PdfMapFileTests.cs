using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TestingUtils;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Drawing;

namespace PurplePen.Tests
{
    [TestClass]
    public class PdfMapFileTests
    {


        [TestMethod]
        public void GetCacheFileName()
        {
            string fileName = TestUtil.GetTestFile("pdfmaps\\Potholes.pdf");
            var pdfMap = new PdfMapFile(fileName);
            Assert.AreEqual(Path.Combine(Path.GetTempPath(), @"PurplePen\8704103F7AFB85896AE188BDAA2855A3E4A75CF3.png"), pdfMap.GetCacheFileName(fileName));
        }

        [TestMethod] 
        public void Potholes()
        {
            string fileName = TestUtil.GetTestFile("pdfmaps\\Potholes.pdf");
            var pdfMap = new PdfMapFile(fileName);

            pdfMap.BeginConversion(); 
            while (pdfMap.Status == PdfMapFile.ConversionStatus.Working)
                System.Threading.Thread.Sleep(10);
            Assert.AreEqual(PdfMapFile.ConversionStatus.Success, pdfMap.Status);

            using (Bitmap bmNew = (Bitmap)Image.FromFile(pdfMap.PngFileName)) {
                TestUtil.CompareBitmapBaseline(bmNew, TestUtil.GetTestFile("pdfmaps\\Potholes_baseline.png"));
            }

        }
    }
}
