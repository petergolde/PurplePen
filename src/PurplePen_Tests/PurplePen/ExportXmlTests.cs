#if TEST
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Drawing;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

// Things to test:
// -- score course


namespace PurplePen.Tests
{
    [TestClass]
    public class ExportXmlTests
    {
        TestUI ui;
        Controller controller;

        public async Task Setup(string filename)
        {
            ui = TestUI.Create();
            controller = ui.controller;
            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile(filename), true);
            Assert.IsTrue(success);
        }


        [TestMethod]
        public async Task ExportXmlTestV2()
        {
            Dictionary<string, string> exceptions = ExportXmlVersion2.TestFileExceptionMap();

            string outputFile = TestUtil.GetTestFile("exportxml\\marymoor_actual.xml");
            string expectedFile = TestUtil.GetTestFile("exportxml\\marymoor_expected.xml");

            await Setup("exportxml\\marymoor.ppen");

            controller.ExportXml(outputFile, RectangleF.FromLTRB(-29.5F, -113.1F, 232.9F, 86.7F), 2);

            TextFileTestUtil.CompareTextFileBaseline(outputFile, expectedFile, exceptions);
            File.Delete(outputFile);
        }

        [TestMethod]
        public async Task ExportXmlTestV3()
        {
            Dictionary<string, string> exceptions = ExportXmlVersion3.TestFileExceptionMap();

            string outputFile = TestUtil.GetTestFile("exportxml\\marymoor_actual_v3.xml");
            string expectedFile = TestUtil.GetTestFile("exportxml\\marymoor_expected_v3.xml");

            await Setup("exportxml\\marymoor.ppen");

            controller.ExportXml(outputFile, RectangleF.FromLTRB(-29.5F, -113.1F, 232.9F, 86.7F), 3);

            TextFileTestUtil.CompareTextFileBaseline(outputFile, expectedFile, exceptions);
            File.Delete(outputFile);
        }

        [TestMethod]
        public async Task ExportXmlTestOtherLocaleV2()
        {
            CultureInfo cultureUISave = Thread.CurrentThread.CurrentUICulture;
            CultureInfo cultureSave = Thread.CurrentThread.CurrentCulture;

            try {
                Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr");

                Dictionary<string, string> exceptions = ExportXmlVersion2.TestFileExceptionMap();

                string outputFile = TestUtil.GetTestFile("exportxml\\marymoor_actual.xml");
                string expectedFile = TestUtil.GetTestFile("exportxml\\marymoor_expected.xml");

                await Setup("exportxml\\marymoor.ppen");

                controller.ExportXml(outputFile, RectangleF.FromLTRB(-29.5F, -113.1F, 232.9F, 86.7F), 2);

                TextFileTestUtil.CompareTextFileBaseline(outputFile, expectedFile, exceptions);
                File.Delete(outputFile);
            }
            finally {
                Thread.CurrentThread.CurrentCulture = cultureSave;
                Thread.CurrentThread.CurrentUICulture = cultureUISave;
            }
        }

        [TestMethod]
        public async Task ExportXmlTestOtherLocaleV3()
        {
            CultureInfo cultureUISave = Thread.CurrentThread.CurrentUICulture;
            CultureInfo cultureSave = Thread.CurrentThread.CurrentCulture;

            try {
                Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr");

                Dictionary<string, string> exceptions = ExportXmlVersion3.TestFileExceptionMap();

                string outputFile = TestUtil.GetTestFile("exportxml\\marymoor_actual_v3.xml");
                string expectedFile = TestUtil.GetTestFile("exportxml\\marymoor_expected_v3.xml");

                await Setup("exportxml\\marymoor.ppen");

                controller.ExportXml(outputFile, RectangleF.FromLTRB(-29.5F, -113.1F, 232.9F, 86.7F), 3);

                TextFileTestUtil.CompareTextFileBaseline(outputFile, expectedFile, exceptions);
                File.Delete(outputFile);
            }
            finally {
                Thread.CurrentThread.CurrentCulture = cultureSave;
                Thread.CurrentThread.CurrentUICulture = cultureUISave;
            }
        }

        [TestMethod]
        public async Task ExportXmlTestMapExchangeV2()
        {
            Dictionary<string, string> exceptions = ExportXmlVersion2.TestFileExceptionMap();

            string outputFile = TestUtil.GetTestFile("exportxml\\mapexchange1_actual.xml");
            string expectedFile = TestUtil.GetTestFile("exportxml\\mapexchange1_expected.xml");

            await Setup("exportxml\\mapexchange1.ppen");

            controller.ExportXml(outputFile, RectangleF.FromLTRB(-29.5F, -113.1F, 232.9F, 86.7F), 2);

            TextFileTestUtil.CompareTextFileBaseline(outputFile, expectedFile, exceptions);
            File.Delete(outputFile);
        }

        [TestMethod]
        public async Task ExportXmlTestMapExchangeV3()
        {
            Dictionary<string, string> exceptions = ExportXmlVersion3.TestFileExceptionMap();

            string outputFile = TestUtil.GetTestFile("exportxml\\mapexchange1_actual_v3.xml");
            string expectedFile = TestUtil.GetTestFile("exportxml\\mapexchange1_expected_v3.xml");

            await Setup("exportxml\\mapexchange1.ppen");

            controller.ExportXml(outputFile, RectangleF.FromLTRB(-29.5F, -113.1F, 232.9F, 86.7F), 3);

            TextFileTestUtil.CompareTextFileBaseline(outputFile, expectedFile, exceptions);
            File.Delete(outputFile);
        }

        [TestMethod]
        public async Task ExportGeoreferencedXmlTestV2()
        {
            Dictionary<string, string> exceptions = ExportXmlVersion2.TestFileExceptionMap();

            string outputFile = TestUtil.GetTestFile("exportxml\\teanaway_actual.xml");
            string expectedFile = TestUtil.GetTestFile("exportxml\\teanaway_expected.xml");

            await Setup("exportxml\\teanawayxml.ppen");

            controller.ExportXml(outputFile, RectangleF.FromLTRB(-22F, -270F, 257F, -54F), 2);

            TextFileTestUtil.CompareTextFileBaseline(outputFile, expectedFile, exceptions);
            File.Delete(outputFile);
        }

        [TestMethod]
        public async Task ExportGeoreferencedXmlTestV3()
        {
            Dictionary<string, string> exceptions = ExportXmlVersion3.TestFileExceptionMap();

            string outputFile = TestUtil.GetTestFile("exportxml\\teanaway_actual_v3.xml");
            string expectedFile = TestUtil.GetTestFile("exportxml\\teanaway_expected_v3.xml");

            await Setup("exportxml\\teanawayxml.ppen");

            controller.ExportXml(outputFile, RectangleF.FromLTRB(-22F, -270F, 257F, -54F), 3);

            TextFileTestUtil.CompareTextFileBaseline(outputFile, expectedFile, exceptions);
            File.Delete(outputFile);
        }

        [TestMethod]
        public async Task ExportRelayV2()
        {
            Dictionary<string, string> exceptions = ExportXmlVersion2.TestFileExceptionMap();

            string outputFile = TestUtil.GetTestFile("exportxml\\relay_actual.xml");
            string expectedFile = TestUtil.GetTestFile("exportxml\\relay_expected.xml");

            await Setup("exportxml\\relay.ppen");

            controller.ExportXml(outputFile, RectangleF.FromLTRB(-22F, -270F, 257F, -54F), 2);

            TextFileTestUtil.CompareTextFileBaseline(outputFile, expectedFile, exceptions);
            File.Delete(outputFile);
        }

        [TestMethod]
        public async Task ExportRelayV3()
        {
            Dictionary<string, string> exceptions = ExportXmlVersion3.TestFileExceptionMap();

            string outputFile = TestUtil.GetTestFile("exportxml\\relay_actual_v3.xml");
            string expectedFile = TestUtil.GetTestFile("exportxml\\relay_expected_v3.xml");

            await Setup("exportxml\\relay.ppen");

            controller.ExportXml(outputFile, RectangleF.FromLTRB(-22F, -270F, 257F, -54F), 3);

            TextFileTestUtil.CompareTextFileBaseline(outputFile, expectedFile, exceptions);
            File.Delete(outputFile);
        }




    }
}

#endif //TEST
