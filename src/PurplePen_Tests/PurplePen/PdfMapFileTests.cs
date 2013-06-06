using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TestingUtils;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PurplePen.Tests
{
    [TestClass]
    public class PdfMapFileTests
    {
        [TestMethod]
        public void FindGhostscriptExe()
        {
            string gsExe = new PdfMapFile("foo.pdf").FindGhostscriptExe();
            Assert.IsNotNull(gsExe);
        }

        [TestMethod]
        public void GetCacheFileName()
        {
            string fileName = TestUtil.GetTestFile("pdfmaps\\Potholes.pdf");
            var pdfMap = new PdfMapFile(fileName);
            Assert.AreEqual(Path.Combine(Path.GetTempPath(), @"PurplePen\6E04103F7AFB85896AE188BDAA2855A3E4A75CF3.png"), pdfMap.GetCacheFileName(fileName));
        }
    }
}
