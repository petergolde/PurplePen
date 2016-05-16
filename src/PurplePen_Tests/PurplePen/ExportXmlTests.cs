/* Copyright (c) 2006-2008, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

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

// Things to test:
// -- score course


namespace PurplePen.Tests
{
    [TestClass]
    public class ExportXmlTests
    {
        TestUI ui;
        Controller controller;

        public void Setup(string filename)
        {
            ui = TestUI.Create();
            controller = ui.controller;
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile(filename), true);
            Assert.IsTrue(success);
        }


        [TestMethod]
        public void ExportXmlTestV2()
        {
            Dictionary<string, string> exceptions = ExportXmlVersion2.TestFileExceptionMap();

            string outputFile = TestUtil.GetTestFile("exportxml\\marymoor_actual.xml");
            string expectedFile = TestUtil.GetTestFile("exportxml\\marymoor_expected.xml");

            Setup("exportxml\\marymoor.ppen");

            controller.ExportXml(outputFile, RectangleF.FromLTRB(-29.5F, -113.1F, 232.9F, 86.7F), 2);

            TestUtil.CompareTextFileBaseline(outputFile, expectedFile, exceptions);
        }

        [TestMethod]
        public void ExportXmlTestV3()
        {
            Dictionary<string, string> exceptions = ExportXmlVersion3.TestFileExceptionMap();

            string outputFile = TestUtil.GetTestFile("exportxml\\marymoor_actual_v3.xml");
            string expectedFile = TestUtil.GetTestFile("exportxml\\marymoor_expected_v3.xml");

            Setup("exportxml\\marymoor.ppen");

            controller.ExportXml(outputFile, RectangleF.FromLTRB(-29.5F, -113.1F, 232.9F, 86.7F), 3);

            TestUtil.CompareTextFileBaseline(outputFile, expectedFile, exceptions);
        }

        [TestMethod]
        public void ExportXmlTestOtherLocaleV2()
        {
            CultureInfo cultureUISave = Thread.CurrentThread.CurrentUICulture;
            CultureInfo cultureSave = Thread.CurrentThread.CurrentCulture;

            try {
                Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr");

                Dictionary<string, string> exceptions = ExportXmlVersion2.TestFileExceptionMap();

                string outputFile = TestUtil.GetTestFile("exportxml\\marymoor_actual.xml");
                string expectedFile = TestUtil.GetTestFile("exportxml\\marymoor_expected.xml");

                Setup("exportxml\\marymoor.ppen");

                controller.ExportXml(outputFile, RectangleF.FromLTRB(-29.5F, -113.1F, 232.9F, 86.7F), 2);

                TestUtil.CompareTextFileBaseline(outputFile, expectedFile, exceptions);
            }
            finally {
                Thread.CurrentThread.CurrentCulture = cultureSave;
                Thread.CurrentThread.CurrentUICulture = cultureUISave;
            }
        }

        [TestMethod]
        public void ExportXmlTestOtherLocaleV3()
        {
            CultureInfo cultureUISave = Thread.CurrentThread.CurrentUICulture;
            CultureInfo cultureSave = Thread.CurrentThread.CurrentCulture;

            try {
                Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr");

                Dictionary<string, string> exceptions = ExportXmlVersion3.TestFileExceptionMap();

                string outputFile = TestUtil.GetTestFile("exportxml\\marymoor_actual_v3.xml");
                string expectedFile = TestUtil.GetTestFile("exportxml\\marymoor_expected_v3.xml");

                Setup("exportxml\\marymoor.ppen");

                controller.ExportXml(outputFile, RectangleF.FromLTRB(-29.5F, -113.1F, 232.9F, 86.7F), 3);

                TestUtil.CompareTextFileBaseline(outputFile, expectedFile, exceptions);
            }
            finally {
                Thread.CurrentThread.CurrentCulture = cultureSave;
                Thread.CurrentThread.CurrentUICulture = cultureUISave;
            }
        }

        [TestMethod]
        public void ExportXmlTestMapExchangeV2()
        {
            Dictionary<string, string> exceptions = ExportXmlVersion2.TestFileExceptionMap();

            string outputFile = TestUtil.GetTestFile("exportxml\\mapexchange1_actual.xml");
            string expectedFile = TestUtil.GetTestFile("exportxml\\mapexchange1_expected.xml");

            Setup("exportxml\\mapexchange1.ppen");

            controller.ExportXml(outputFile, RectangleF.FromLTRB(-29.5F, -113.1F, 232.9F, 86.7F), 2);

            TestUtil.CompareTextFileBaseline(outputFile, expectedFile, exceptions);
        }

        [TestMethod]
        public void ExportXmlTestMapExchangeV3()
        {
            Dictionary<string, string> exceptions = ExportXmlVersion3.TestFileExceptionMap();

            string outputFile = TestUtil.GetTestFile("exportxml\\mapexchange1_actual_v3.xml");
            string expectedFile = TestUtil.GetTestFile("exportxml\\mapexchange1_expected_v3.xml");

            Setup("exportxml\\mapexchange1.ppen");

            controller.ExportXml(outputFile, RectangleF.FromLTRB(-29.5F, -113.1F, 232.9F, 86.7F), 3);

            TestUtil.CompareTextFileBaseline(outputFile, expectedFile, exceptions);
        }

    }
}

#endif //TEST
