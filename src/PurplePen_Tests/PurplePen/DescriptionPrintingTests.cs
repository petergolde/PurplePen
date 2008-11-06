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
using System.Drawing;
using System.Drawing.Printing;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.Tests
{
    [TestClass]
    public class DescriptionPrintingTests: TestFixtureBase
    {
        TestUI ui;
        Controller controller;

        [TestInitialize]
        public void Setup()
        {
            ui = TestUI.Create();
            controller = ui.controller;
        }

        private void DescriptionPrintingTest(string basename, DescriptionPrintSettings descPrintSettings)
        {
            // Get the pages of the printing.
            DescriptionPrinting descPrinter = new DescriptionPrinting(controller.GetEventDB(), ui.symbolDB, descPrintSettings);
            Bitmap[] bitmaps = descPrinter.PrintBitmaps();
            descPrinter.Dispose();

            // Check all the pages against the baseline.
            for (int page = 0; page < bitmaps.Length; ++page) {
                Bitmap bm = bitmaps[page];
                string baseFileName = basename + "_page" + (page + 1).ToString();
                TestUtil.CheckBitmapsBase(bm, baseFileName);
            }
        }

        [TestMethod]
        public void PrintDescriptions1()
        {
            controller.LoadInitialFile(TestUtil.GetTestFile("printdesc\\marymoor.ppen"));
            DescriptionPrintSettings descPrintSettings = new DescriptionPrintSettings();

            descPrintSettings.CourseIds = new Id<Course>[] { CourseId(1), CourseId(2), CourseId(3) };
            DescriptionPrintingTest("printdesc\\desc1", descPrintSettings);
        }

        [TestMethod]
        public void PrintDescriptions2()
        {
            controller.LoadInitialFile(TestUtil.GetTestFile("printdesc\\marymoor.ppen"));
            DescriptionPrintSettings descPrintSettings = new DescriptionPrintSettings();
            descPrintSettings.PageSettings.Landscape = true;
            descPrintSettings.PageSettings.Margins = new Margins(50, 50, 200, 200);

            descPrintSettings.CourseIds = new Id<Course>[] { CourseId(0), CourseId(1), CourseId(2), CourseId(3) };
            DescriptionPrintingTest("printdesc\\desc2", descPrintSettings);
        }

        // Should be symbols and text for all controls.
        [TestMethod]
        public void PrintDescriptions3()
        {
            controller.LoadInitialFile(TestUtil.GetTestFile("printdesc\\marymoor2.ppen"));
            DescriptionPrintSettings descPrintSettings = new DescriptionPrintSettings();
            descPrintSettings.PageSettings.Landscape = true;
            descPrintSettings.PageSettings.Margins = new Margins(50, 50, 200, 200);

            descPrintSettings.CourseIds = new Id<Course>[] { CourseId(0) };
            DescriptionPrintingTest("printdesc\\desc3", descPrintSettings);
        }

        [TestMethod]
        public void PrintingException()
        {
            controller.LoadInitialFile(TestUtil.GetTestFile("printdesc\\marymoor.ppen"));
            DescriptionPrintSettings descPrintSettings = new DescriptionPrintSettings();

            descPrintSettings.CourseIds = new Id<Course>[] { CourseId(1), CourseId(2), CourseId(3) };
            descPrintSettings.PageSettings.PrinterSettings.PrinterName = "foobar";

            bool success = controller.PrintDescriptions(descPrintSettings, false);

            Assert.IsFalse(success);
            string expected =
@"ERROR: 'Cannot print 'Marymoor WIOL 2' for the following reason:

Settings to access printer 'foobar' are not valid.'
";

            Assert.AreEqual(expected, ui.output.ToString());
        }
	
	
    }


    [TestClass]
    public class DescriptionPositionerTests: TestFixtureBase
    {
        class MockDescription: IPrintableRectangle
        {
            string id;
            TextWriter writer;
            Size boxes;

            public MockDescription(string id, TextWriter writer, int boxWidth, int boxHeight)
            {
                this.id = id;
                this.writer = writer;
                this.boxes = new Size(boxWidth, boxHeight);
            }

            public Size Boxes
            {
                get { return boxes; }
            }

            public void Draw(Graphics g, float x, float y, int startLine, int countLines)
            {
                if (startLine != 0 || countLines < boxes.Height)
                    writer.WriteLine("@ ({1},{2}) partial description '{0}' [start:{3} count:{4}]", id, x, y, startLine, countLines, x, y);
                else
                    writer.WriteLine("@ ({1},{2}) description '{0}'", id, x, y);
            }
        }

        [TestMethod]
        public void LayoutOneDescription1()
        {
            // Simple description, fits 3 times across and 2 times down.
            StringWriter writer = new StringWriter();
            SizeF paperSize = new SizeF(300, 500);
            float boxSize = 10;
            float spacing = 15;
            MockDescription description = new MockDescription("desc1", writer, 8, 17);
            RectanglePositioner positioner = new RectanglePositioner(paperSize, boxSize, spacing);

            int count = positioner.LayoutOneDescriptionPage(description);
            Assert.AreEqual(1, positioner.PageCount);
            Assert.AreEqual(6, count);
            positioner.DrawPage(null, 0);
            Assert.AreEqual(
@"@ (0,0) description 'desc1'
@ (0,185) description 'desc1'
@ (95,0) description 'desc1'
@ (95,185) description 'desc1'
@ (190,0) description 'desc1'
@ (190,185) description 'desc1'
", writer.ToString());
        }

        [TestMethod]
        public void LayoutOneDescription2()
        {
            // Simple description, fits 3 times across and exactly 1 times down.
            StringWriter writer = new StringWriter();
            SizeF paperSize = new SizeF(300, 170);
            float boxSize = 10;
            float spacing = 15;
            MockDescription description = new MockDescription("desc1", writer, 8, 17);
            RectanglePositioner positioner = new RectanglePositioner(paperSize, boxSize, spacing);

            int count = positioner.LayoutOneDescriptionPage(description);
            Assert.AreEqual(1, positioner.PageCount);
            Assert.AreEqual(3, count);
            positioner.DrawPage(null, 0);
            Assert.AreEqual(
@"@ (0,0) description 'desc1'
@ (95,0) description 'desc1'
@ (190,0) description 'desc1'
", writer.ToString());
        }

        [TestMethod]
        public void LayoutOneDescription3()
        {
            // Simple description, fits 1 times across and 4 times down.
            StringWriter writer = new StringWriter();
            SizeF paperSize = new SizeF(90, 730);
            float boxSize = 10;
            float spacing = 15;
            MockDescription description = new MockDescription("desc1", writer, 8, 17);
            RectanglePositioner positioner = new RectanglePositioner(paperSize, boxSize, spacing);

            int count = positioner.LayoutOneDescriptionPage(description);
            Assert.AreEqual(1, positioner.PageCount);
            Assert.AreEqual(4, count);
            positioner.DrawPage(null, 0);
            Assert.AreEqual(
@"@ (0,0) description 'desc1'
@ (0,185) description 'desc1'
@ (0,370) description 'desc1'
@ (0,555) description 'desc1'
", writer.ToString());
        }

        [TestMethod]
        public void LayoutOneDescription4()
        {
            // Large description, split into 5 columns on 2 pages.
            StringWriter writer = new StringWriter();
            SizeF paperSize = new SizeF(300, 100);
            float boxSize = 10;
            float spacing = 15;
            MockDescription description = new MockDescription("desc1", writer, 8, 46);
            RectanglePositioner positioner = new RectanglePositioner(paperSize, boxSize, spacing);

            int count = positioner.LayoutOneDescriptionPage(description);
            Assert.AreEqual(2, positioner.PageCount);
            Assert.AreEqual(1, count);
            positioner.DrawPage(null, 0);
            positioner.DrawPage(null, 1);
            Assert.AreEqual(
@"@ (0,0) partial description 'desc1' [start:0 count:10]
@ (95,0) partial description 'desc1' [start:10 count:10]
@ (190,0) partial description 'desc1' [start:20 count:10]
@ (0,0) partial description 'desc1' [start:30 count:10]
@ (95,0) partial description 'desc1' [start:40 count:6]
", writer.ToString());
        }


        [TestMethod]
        public void LayoutOneDescription5()
        {
            // Large description, split into 2 columns on 1 page.
            StringWriter writer = new StringWriter();
            SizeF paperSize = new SizeF(300, 100);
            float boxSize = 10;
            float spacing = 15;
            MockDescription description = new MockDescription("desc1", Console.Out, 8, 17);
            RectanglePositioner positioner = new RectanglePositioner(paperSize, boxSize, spacing);

            int count = positioner.LayoutOneDescriptionPage(description);
            Assert.AreEqual(1, positioner.PageCount);
            Assert.AreEqual(1, count);
            positioner.DrawPage(null, 0);
            Assert.AreEqual(
@"", writer.ToString());
        }


        [TestMethod]
        public void PageTooSmall()
        {
            // Large description, split into 2 columns on 1 page.
            StringWriter writer = new StringWriter();
            SizeF paperSize = new SizeF(75, 100);
            float boxSize = 10;
            float spacing = 15;
            MockDescription description = new MockDescription("desc1", Console.Out, 8, 17);
            RectanglePositioner positioner = new RectanglePositioner(paperSize, boxSize, spacing);

            try {
                int count = positioner.LayoutOneDescriptionPage(description);
                Assert.Fail("shouldn't get here");
            }
            catch (Exception e) {
                Assert.AreEqual(MiscText.PageTooSmall, e.Message);
            }
        }


        [TestMethod]
        public void LayoutMultipleDescriptions1()
        {
            // 7 descriptions.
            StringWriter writer = new StringWriter();
            SizeF paperSize = new SizeF(400, 500);
            float boxSize = 10;
            float spacing = 15;
            MockDescription desc1 = new MockDescription("desc1", writer, 8, 17);
            MockDescription desc2 = new MockDescription("desc2", writer, 8, 15);
            MockDescription desc3 = new MockDescription("desc3", writer, 12, 22);
            MockDescription desc4 = new MockDescription("desc4", writer, 12, 24);
            MockDescription desc5 = new MockDescription("desc5", writer, 8, 11);
            MockDescription desc6 = new MockDescription("desc6", writer, 8, 16);
            MockDescription desc7 = new MockDescription("desc7", writer, 8, 8);
            RectanglePositioner positioner = new RectanglePositioner(paperSize, boxSize, spacing);

            positioner.LayoutMultipleDescriptions(new MockDescription[] { desc1, desc2, desc3, desc4, desc5, desc6, desc7 });
            //Assert.AreEqual(1, positioner.PageCount);
            positioner.DrawPage(null, 0);
            Assert.AreEqual(
@"@ (0,0) description 'desc4'
@ (0,255) description 'desc3'
@ (135,0) description 'desc1'
@ (135,185) description 'desc6'
@ (135,360) description 'desc5'
@ (230,0) description 'desc2'
@ (230,165) description 'desc7'
", writer.ToString());
        }


        [TestMethod]
        public void LayoutMultipleDescriptions2()
        {
            // test descriptions breaking across columns.
            StringWriter writer = new StringWriter();
            SizeF paperSize = new SizeF(400, 250);
            float boxSize = 10;
            float spacing = 15;
            MockDescription desc1 = new MockDescription("desc1", writer, 12, 22);
            MockDescription desc2 = new MockDescription("desc2", writer, 8, 36);
            MockDescription desc3 = new MockDescription("desc3", writer, 8, 6);
            RectanglePositioner positioner = new RectanglePositioner(paperSize, boxSize, spacing);

            positioner.LayoutMultipleDescriptions(new MockDescription[] { desc1, desc2, desc3 });
            //Assert.AreEqual(1, positioner.PageCount);
            positioner.DrawPage(null, 0);
            Assert.AreEqual(
@"@ (0,0) description 'desc1'
@ (135,0) partial description 'desc2' [start:0 count:25]
@ (230,0) partial description 'desc2' [start:25 count:11]
@ (230,125) description 'desc3'
", writer.ToString());
        }

    }
}

#endif //TEST
