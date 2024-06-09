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
using System.Windows.Media.Imaging;

namespace PurplePen.Tests
{
    [TestClass]
    public class CoursePrintingTests: TestFixtureBase
    {
        TestUI ui;
        Controller controller;

        [TestInitialize]
        public void Setup()
        {
            ui = TestUI.Create();
            controller = ui.controller;
        }

        [TestMethod]
        public void LayoutPageDimension()
        {
            // Should fit on one page
            List<CoursePageLayout.DimensionLayout> result = new List<CoursePageLayout.DimensionLayout>(
                CoursePageLayout.LayoutPageDimension(-10.0F, 240.0F, 50F, 1000F, 1.0F));
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(-10.0F, result[0].startMap);
            Assert.AreEqual(240.0F, result[0].lengthMap);
            Assert.AreEqual(77.56F, result[0].startPage, 0.01F);
            Assert.AreEqual(944.88F, result[0].lengthPage, 0.01F);

            // Fit on two pages
            result = new List<CoursePageLayout.DimensionLayout>(
                CoursePageLayout.LayoutPageDimension(-100.0F, 380.0F, 50F, 1000F, 1.0F));
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(-100.0F, result[0].startMap);
            Assert.AreEqual(254.0F, result[0].lengthMap, 0.01F);
            Assert.AreEqual(50F, result[0].startPage, 0.01F);
            Assert.AreEqual(1000F, result[0].lengthPage, 0.01F);
            Assert.AreEqual(26.0F, result[1].startMap);
            Assert.AreEqual(254.0F, result[1].lengthMap, 0.01F);
            Assert.AreEqual(50F, result[1].startPage, 0.01F);
            Assert.AreEqual(1000F, result[1].lengthPage, 0.01F);

            // Barely fit 3 pages, with minimum 1 inch overlap.
            result = new List<CoursePageLayout.DimensionLayout>(
                CoursePageLayout.LayoutPageDimension(-100.0F, 710.0F, 50F, 1000F, 1.0F));
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(-100.0F, result[0].startMap);
            Assert.AreEqual(254.0F, result[0].lengthMap, 0.01F);
            Assert.AreEqual(50F, result[0].startPage, 0.01F);
            Assert.AreEqual(1000F, result[0].lengthPage, 0.01F);
            Assert.AreEqual(128.0F, result[1].startMap, 0.01);
            Assert.AreEqual(254.0F, result[1].lengthMap, 0.01F);
            Assert.AreEqual(50F, result[1].startPage, 0.01F);
            Assert.AreEqual(1000F, result[1].lengthPage, 0.01F);
            Assert.AreEqual(356.0F, result[2].startMap, 0.01F);
            Assert.AreEqual(254.0F, result[2].lengthMap, 0.01F);
            Assert.AreEqual(50F, result[2].startPage, 0.01F);
            Assert.AreEqual(1000F, result[2].lengthPage, 0.01F);

            // Must go onto 4 pages
            result = new List<CoursePageLayout.DimensionLayout>(
                CoursePageLayout.LayoutPageDimension(-100.0F, 715.0F, 50F, 1000F, 1.0F));
            Assert.AreEqual(4, result.Count);
        }

        [TestMethod]
        public void LayoutPageDimensionScaleRatio()
        {
            // Would fit on one page with 1.0 scale ratio. Now requires 2.
            List<CoursePageLayout.DimensionLayout> result = new List<CoursePageLayout.DimensionLayout>(
                CoursePageLayout.LayoutPageDimension(-10.0F, 200.0F, 50F, 1000F, 0.5F));
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(-10.0F, result[0].startMap, 0.01F);
            Assert.AreEqual(127.0F, result[0].lengthMap, 0.01F);
            Assert.AreEqual(50F, result[0].startPage, 0.01F);
            Assert.AreEqual(1000F, result[0].lengthPage, 0.01F);
            Assert.AreEqual(63.0F, result[1].startMap, 0.01F);
            Assert.AreEqual(127.0F, result[1].lengthMap, 0.01F);
            Assert.AreEqual(50F, result[1].startPage, 0.01F);
            Assert.AreEqual(1000F, result[1].lengthPage, 0.01F);
        }

        // Write a bitmap to a PNG.
        static void WritePng(BitmapSource bmp, string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Create)) {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                encoder.Save(stream);
            }
        }

        private void CoursePrintingTest(string basename, CoursePrintSettings coursePrintSettings, CourseAppearance appearance, float dpi = 200)
        {
            GC.Collect();

            // Get the map display
            MapDisplay mapDisplay = new MapDisplay();
            mapDisplay.MapIntensity = 0.6F;
            mapDisplay.AntiAlias = true;
            mapDisplay.SetMapFile(controller.MapType, controller.MapFileName);
            if (controller.MapType == MapType.Bitmap)
                mapDisplay.Dpi = controller.MapDpi;
            mapDisplay.OcadOverprintEffect = appearance.useOcadOverprint;

            // Get the pages of the printing.
            CoursePrinting coursePrinter = new CoursePrinting(controller.GetEventDB(), ui.symbolDB, controller, mapDisplay.CloneToFullIntensity(), coursePrintSettings, appearance);
            Bitmap[] bitmaps = coursePrinter.PrintBitmaps();
            
            // Check all the pages against the baseline.
            for (int page = 0; page < bitmaps.Length; ++page) {
                Bitmap bm = bitmaps[page];
                string baseFileName = basename + "_page" + (page + 1).ToString();
                TestUtil.CheckBitmapsBase(bm, baseFileName, 5);
                bm.Dispose();
            }

#if XPS_PRINTING
            // Only OCAD maps can be printed in XPS mode.
            if (controller.MapType == MapType.OCAD) {
                // Get the pages of the printing in XPS/WPF mode
                System.Windows.Media.Imaging.BitmapSource[] xpsBitmaps = coursePrinter.PrintXpsBitmaps(dpi);

                // Check all the pages against the baseline.
                for (int page = 0; page < xpsBitmaps.Length; ++page) {
                    var bm = xpsBitmaps[page];
                    string baseFileName = basename + "_xps_page" + (page + 1).ToString();
                    string newFileName = TestUtil.GetTestFile(basename + "_xps_page" + (page + 1).ToString() + "_new");
                    WritePng(bm, newFileName);
                    Bitmap newBitmap = (Bitmap)Image.FromFile(newFileName);

                    // For some reason, the XPS anti-aliasing varies slightly. Allow pixel difference to account.
                    TestUtil.CheckBitmapsBase(newBitmap, baseFileName, 25);
                }
            }
#endif // XPS_PRINTING
        }

        [TestMethod]
        public void PrintCourses1()
        {
            controller.LoadInitialFile(TestUtil.GetTestFile("courseprinting\\marymoor.ppen"), true);
            CoursePrintSettings coursePrintSettings = new CoursePrintSettings();
            coursePrintSettings.CropLargePrintArea = false;
            coursePrintSettings.PrintingColorModel = ColorModel.CMYK;

            coursePrintSettings.CourseIds = new Id<Course>[] { CourseId(1), CourseId(2), CourseId(0) };
            CoursePrintingTest("courseprinting\\test1", coursePrintSettings, new CourseAppearance());
        }

        [TestMethod]
        public void PrintCoursesNoBlend()
        {
            controller.LoadInitialFile(TestUtil.GetTestFile("courseprinting\\marymoor.ppen"), true);
            CoursePrintSettings coursePrintSettings = new CoursePrintSettings();
            coursePrintSettings.CropLargePrintArea = false;
            coursePrintSettings.PrintingColorModel = ColorModel.CMYK;

            CourseAppearance appearance = new CourseAppearance();
            appearance.purpleColorBlend = false;

            coursePrintSettings.CourseIds = new Id<Course>[] { CourseId(1), CourseId(2), CourseId(0) };
            CoursePrintingTest("courseprinting\\noblend", coursePrintSettings, appearance);
        }


        [TestMethod]
        public void PrintCoursesArial()
        {
            controller.LoadInitialFile(TestUtil.GetTestFile("courseprinting\\marymoor.ppen"), true);
            CoursePrintSettings coursePrintSettings = new CoursePrintSettings();
            coursePrintSettings.CropLargePrintArea = false;
            coursePrintSettings.PrintingColorModel = ColorModel.CMYK;

            CourseAppearance appearance = new CourseAppearance();
            appearance.numberRoboto = false;

            coursePrintSettings.CourseIds = new Id<Course>[] { CourseId(1), CourseId(2), CourseId(0) };
            CoursePrintingTest("courseprinting\\arial", coursePrintSettings, appearance);
        }

        [TestMethod]
        public void PrintCourses2()
        {
            CourseAppearance appearance = new CourseAppearance();
            appearance.controlCircleSize = 0.75F;  //smaller circles
            appearance.lineWidth = 3F; // thin lines
            appearance.numberHeight = 0.5F; // small numbers.
            appearance.numberBold = true; // bold numbers.
            appearance.useDefaultPurple = false;
            appearance.autoLegGapSize = 0.0F;
            appearance.purpleC = 0.32F;
            appearance.purpleY = 1.00F;
            appearance.purpleM = 0;
            appearance.purpleK = 0.30F;

            controller.LoadInitialFile(TestUtil.GetTestFile("courseprinting\\marymoor.ppen"), true);
            CoursePrintSettings coursePrintSettings = new CoursePrintSettings();
            coursePrintSettings.CropLargePrintArea = false;
            coursePrintSettings.PrintingColorModel = ColorModel.RGB;

            coursePrintSettings.CourseIds = new Id<Course>[] { CourseId(1), CourseId(2), CourseId(0) };
            CoursePrintingTest("courseprinting\\test2", coursePrintSettings, appearance);
        }

        // Test with crop print area.
        [TestMethod]
        public void PrintCourses3()
        {
            controller.LoadInitialFile(TestUtil.GetTestFile("courseprinting\\marymoor2.ppen"), true);
            CoursePrintSettings coursePrintSettings = new CoursePrintSettings();
            coursePrintSettings.CropLargePrintArea = true;
            coursePrintSettings.PrintingColorModel = ColorModel.RGB;

            coursePrintSettings.CourseIds = new Id<Course>[] { CourseId(1), CourseId(2), CourseId(0) };
            CoursePrintingTest("courseprinting\\test3", coursePrintSettings, new CourseAppearance());
        }

        // Test with graphics things
        [TestMethod]
        public void PrintCourses4()
        {
            controller.LoadInitialFile(TestUtil.GetTestFile("courseprinting\\marymoor_graphics.ppen"), true);
            CoursePrintSettings coursePrintSettings = new CoursePrintSettings();
            coursePrintSettings.CropLargePrintArea = true;
            coursePrintSettings.PrintingColorModel = ColorModel.CMYK;

            coursePrintSettings.CourseIds = new Id<Course>[] {CourseId(2) };
            CoursePrintingTest("courseprinting\\test4", coursePrintSettings, new CourseAppearance());
        }

        [TestMethod]
        public void PrintBitmapBaseMap()
        {
            controller.LoadInitialFile(TestUtil.GetTestFile("courseprinting\\Lincoln Park.ppen"), true);
            CoursePrintSettings coursePrintSettings = new CoursePrintSettings();
            coursePrintSettings.CropLargePrintArea = true;
            coursePrintSettings.PrintingColorModel = ColorModel.CMYK;

            coursePrintSettings.CourseIds = new Id<Course>[] { CourseId(1) };
            CoursePrintingTest("courseprinting\\bitmapbase", coursePrintSettings, new CourseAppearance());
        }

        [TestMethod]
        public void PrintPdfBaseMap()
        {
            controller.LoadInitialFile(TestUtil.GetTestFile("courseprinting\\Lincoln Park PDF.ppen"), true);
            CoursePrintSettings coursePrintSettings = new CoursePrintSettings();
            coursePrintSettings.CropLargePrintArea = true;
            coursePrintSettings.PrintingColorModel = ColorModel.CMYK;

            coursePrintSettings.CourseIds = new Id<Course>[] { CourseId(1) };
            CoursePrintingTest("courseprinting\\pdfbase", coursePrintSettings, new CourseAppearance(), 200);
        }

        [TestMethod]
        public void PrintOverprint()
        {
            controller.LoadInitialFile(TestUtil.GetTestFile("courseprinting\\Overprint test.ppen"), true);
            CoursePrintSettings coursePrintSettings = new CoursePrintSettings();
            coursePrintSettings.CropLargePrintArea = true;
            coursePrintSettings.PrintingColorModel = ColorModel.CMYK;

            coursePrintSettings.CourseIds = new Id<Course>[] { CourseId(1) };

            CourseAppearance appearance = new CourseAppearance();
            appearance.purpleColorBlend = true;
            appearance.useOcadOverprint = true;

            CoursePrintingTest("courseprinting\\overprint", coursePrintSettings, appearance, 200);
        }

        [TestMethod]
        public void PrintTemplatedBaseMap()
        {
            controller.LoadInitialFile(TestUtil.GetTestFile("courseprinting\\Template.ppen"), true);
            CoursePrintSettings coursePrintSettings = new CoursePrintSettings();
            coursePrintSettings.CropLargePrintArea = true;
            coursePrintSettings.PrintingColorModel = ColorModel.CMYK;

            coursePrintSettings.CourseIds = new Id<Course>[] { CourseId(1) };
            CoursePrintingTest("courseprinting\\templatebase", coursePrintSettings, new CourseAppearance(), 200);
        }

        [TestMethod]
        public void PrintingException()
        {
            controller.LoadInitialFile(TestUtil.GetTestFile("courseprinting\\marymoor.ppen"), true);
            CoursePrintSettings coursePrintSettings = new CoursePrintSettings();

            coursePrintSettings.CourseIds = new Id<Course>[] { CourseId(1), CourseId(2), CourseId(3) };
            coursePrintSettings.PageSettings.PrinterSettings.PrinterName = "foobar";

            bool success = controller.PrintCourses(coursePrintSettings, false);

            Assert.IsFalse(success);
            string expected =
@"ERROR: 'Cannot print 'Marymoor WIOL 2' for the following reason:

Settings to access printer 'foobar' are not valid.'
";

            Assert.AreEqual(expected, ui.output.ToString());
        }

        [TestMethod]
        public void PrintAreasAndPageSizes()
        {
            controller.LoadInitialFile(TestUtil.GetTestFile("courseprinting\\Lincoln Park PrintAreas 2.ppen"), true);
            CoursePrintSettings coursePrintSettings = new CoursePrintSettings();
            coursePrintSettings.CropLargePrintArea = false;
            coursePrintSettings.PrintingColorModel = ColorModel.CMYK;

            coursePrintSettings.CourseIds = new Id<Course>[] { CourseId(0), CourseId(1), CourseId(2), CourseId(3), CourseId(4) };
            CoursePrintingTest("courseprinting\\areas", coursePrintSettings, new CourseAppearance());
        }



        [TestMethod]
        public void AdjustDpi()
        {
            float result;

            result = CoursePrinting.AdjustDpi(300);
            Assert.AreEqual(600, result);

            result = CoursePrinting.AdjustDpi(600);
            Assert.AreEqual(600, result);

            result = CoursePrinting.AdjustDpi(720);
            Assert.AreEqual(720, result);

            result = CoursePrinting.AdjustDpi(1800);
            Assert.AreEqual(900, result);

            result = CoursePrinting.AdjustDpi(100);
            Assert.AreEqual(400, result);

            result = CoursePrinting.AdjustDpi(300);
            Assert.AreEqual(600, result);
        }

        [TestMethod]
        public void CropPrintArea()
        {
            RectangleF printArea = RectangleF.FromLTRB(10, 40, 120, 90);
            RectangleF result;
            float area;

            // Printable area bigger than printArea -- should just get printArea back.
            result = CoursePageLayout.CropPrintArea(printArea, RectangleF.FromLTRB(30, 40, 140, 80), new SizeF(110, 100), out area);
            Assert.AreEqual(printArea, result);
            Assert.AreEqual(90 * 40, area);

            // Printable area smaller than print area, larger than courseObjects
            result = CoursePageLayout.CropPrintArea(printArea, RectangleF.FromLTRB(25, 30, 80, 60), new SizeF(70, 40), out area);
            Assert.AreEqual(RectangleF.FromLTRB(17.5F, 40, 87.5F, 80), result);
            Assert.AreEqual(55 * 20, area);

            result = CoursePageLayout.CropPrintArea(printArea, RectangleF.FromLTRB(25, 60, 80, 85), new SizeF(70, 40), out area);
            Assert.AreEqual(RectangleF.FromLTRB(17.5F, 50, 87.5F, 90), result);
            Assert.AreEqual(55 * 25, area);

            // Printable area smaller than  courseObjects
            result = CoursePageLayout.CropPrintArea(printArea, RectangleF.FromLTRB(25, 30, 80, 60), new SizeF(15, 10), out area);
            Assert.AreEqual(RectangleF.FromLTRB(45, 40, 60, 50), result);
            //Assert.AreEqual(55 * 20, area);
        }

    }
}

#endif //TEST
