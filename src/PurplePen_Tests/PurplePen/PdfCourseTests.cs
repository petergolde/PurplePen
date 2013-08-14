#if TEST
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;


namespace PurplePen.Tests
{
    [TestClass]
    public class PdfCourseTests: TestFixtureBase
    {
        TestUI ui;
        Controller controller;

        [TestInitialize]
        public void Setup()
        {
            ui = TestUI.Create();
            controller = ui.controller;
        }

        // Create some courses, write them as PDF, and check against a PNG snapshot of that PDF.
        void CreatePdfFiles(string file, CoursePdfSettings settings, CourseAppearance appearance, string[] expectedFiles, string[] expectedDumps)
        {
            EventDB eventDB = controller.GetEventDB();

            bool success = controller.LoadInitialFile(file, true);
            Assert.IsTrue(success);

            controller.SetCourseAppearance(appearance);

            for (int i = 0; i < expectedFiles.Length; ++i) {
                File.Delete(expectedFiles[i]);
            }

            success = controller.CreateCoursePdfs(settings);
            Assert.IsTrue(success);

            for (int i = 0; i < expectedFiles.Length; ++i) {
                CheckPdfDump(expectedFiles[i], expectedDumps[i]);
                File.Delete(expectedFiles[i]);
            }
        }

        private void CheckPdfDump(string pdfFile, string expectedPng)
        {
            PdfMapFile mapFile = new PdfMapFile(pdfFile);
            string pngFile = Path.Combine(Path.GetDirectoryName(pdfFile), Path.GetFileNameWithoutExtension(pdfFile) + "_page%d_temp.png");
            mapFile.BeginUncachedConversion(pngFile, 200); // Convert 200 DPI.
            while (mapFile.Status == PdfMapFile.ConversionStatus.Working)
                System.Threading.Thread.Sleep(10);
            Assert.AreEqual(PdfMapFile.ConversionStatus.Success, mapFile.Status);

            int pageNum = 1;
            for (; ; ) {
                string pngExpectedPage = expectedPng.Replace("%d", pageNum.ToString());
                bool expectedPageExists = File.Exists(pngExpectedPage);
                string pngActualPage = pngFile.Replace("%d", pageNum.ToString());
                bool actualPageExists = File.Exists(pngActualPage);
                    
                Assert.AreEqual(expectedPageExists, actualPageExists);
                if (expectedPageExists) {
                    using (Bitmap bmNew = (Bitmap)Image.FromFile(pngActualPage)) {
                        TestUtil.CompareBitmapBaseline(bmNew, pngExpectedPage);
                    }
                }
                else {
                    break;
                }

                if (!expectedPng.Contains("%d"))
                    break;

                pageNum++;
            }
        }

        [TestMethod]
        public void Files_OnePerCourse()
        {
            EventDB eventDB = controller.GetEventDB();
            SymbolDB symbolDB = ui.symbolDB;

            CoursePdfSettings settings = new CoursePdfSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("controller\\pdf_create1");
            settings.CourseIds = new Id<Course>[] { CourseId(1), CourseId(6), Id<Course>.None };
            settings.PaperSize = new System.Drawing.Printing.PaperSize("Letter", 850, 1100);
            settings.ColorModel = ColorModel.CMYK;
            settings.CropLargePrintArea = true;
            settings.FileCreation = CoursePdfSettings.PdfFileCreation.FilePerCourse;
            settings.PrintMapExchangesOnOneMap = false;
            settings.Margins = new System.Drawing.Printing.Margins(100, 25, 50, 120);

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\mapexchange1.ppen"), true);
            Assert.IsTrue(success);

            var coursePdf = new CoursePdf(eventDB, symbolDB, controller, controller.MapDisplay, settings, new CourseAppearance());
            var filesToCreate = coursePdf.GetFilesToCreate();

            Assert.AreEqual(3, filesToCreate.Count);
            Assert.AreEqual(TestUtil.GetTestFile("controller\\pdf_create1\\Course 1.pdf"), filesToCreate[0].First);
            CollectionAssert.AreEqual(new CourseDesignator[] { Designator(1) }, filesToCreate[0].Second.ToList());
            Assert.AreEqual(TestUtil.GetTestFile("controller\\pdf_create1\\Course 5.pdf"), filesToCreate[1].First);
            CollectionAssert.AreEqual(new CourseDesignator[] { new CourseDesignator(CourseId(6), 0), new CourseDesignator(CourseId(6), 1), new CourseDesignator(CourseId(6), 2), new CourseDesignator(CourseId(6), 3) },
                                      filesToCreate[1].Second.ToList());
            Assert.AreEqual(TestUtil.GetTestFile("controller\\pdf_create1\\All controls.pdf"), filesToCreate[2].First);
            CollectionAssert.AreEqual(new CourseDesignator[] { CourseDesignator.AllControls }, filesToCreate[2].Second.ToList());

            settings.PrintMapExchangesOnOneMap = true;

            coursePdf = new CoursePdf(eventDB, symbolDB, controller, controller.MapDisplay, settings, new CourseAppearance());
            filesToCreate = coursePdf.GetFilesToCreate();

            Assert.AreEqual(3, filesToCreate.Count);
            Assert.AreEqual(TestUtil.GetTestFile("controller\\pdf_create1\\Course 1.pdf"), filesToCreate[0].First);
            CollectionAssert.AreEqual(new CourseDesignator[] { Designator(1) }, filesToCreate[0].Second.ToList());
            Assert.AreEqual(TestUtil.GetTestFile("controller\\pdf_create1\\Course 5.pdf"), filesToCreate[1].First);
            CollectionAssert.AreEqual(new CourseDesignator[] { Designator(6) },
                                      filesToCreate[1].Second.ToList());
            Assert.AreEqual(TestUtil.GetTestFile("controller\\pdf_create1\\All controls.pdf"), filesToCreate[2].First);
            CollectionAssert.AreEqual(new CourseDesignator[] { CourseDesignator.AllControls }, filesToCreate[2].Second.ToList());

        }

        [TestMethod]
        public void Files_OnePerCoursePart()
        {
            EventDB eventDB = controller.GetEventDB();
            SymbolDB symbolDB = ui.symbolDB;

            CoursePdfSettings settings = new CoursePdfSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("controller\\pdf_create1");
            settings.CourseIds = new Id<Course>[] { CourseId(1), CourseId(6), Id<Course>.None };
            settings.PaperSize = new System.Drawing.Printing.PaperSize("Letter", 850, 1100);
            settings.ColorModel = ColorModel.CMYK;
            settings.CropLargePrintArea = true;
            settings.FileCreation = CoursePdfSettings.PdfFileCreation.FilePerCoursePart;
            settings.PrintMapExchangesOnOneMap = false;
            settings.Margins = new System.Drawing.Printing.Margins(100, 25, 50, 120);

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\mapexchange1.ppen"), true);
            Assert.IsTrue(success);

            var coursePdf = new CoursePdf(eventDB, symbolDB, controller, controller.MapDisplay, settings, new CourseAppearance());
            var filesToCreate = coursePdf.GetFilesToCreate();

            Assert.AreEqual(6, filesToCreate.Count);
            Assert.AreEqual(TestUtil.GetTestFile("controller\\pdf_create1\\Course 1.pdf"), filesToCreate[0].First);
            CollectionAssert.AreEqual(new CourseDesignator[] { Designator(1) }, filesToCreate[0].Second.ToList());
            Assert.AreEqual(TestUtil.GetTestFile("controller\\pdf_create1\\Course 5-1.pdf"), filesToCreate[1].First);
            CollectionAssert.AreEqual(new CourseDesignator[] { new CourseDesignator(CourseId(6), 0)}, filesToCreate[1].Second.ToList());
            Assert.AreEqual(TestUtil.GetTestFile("controller\\pdf_create1\\Course 5-2.pdf"), filesToCreate[2].First);
            CollectionAssert.AreEqual(new CourseDesignator[] { new CourseDesignator(CourseId(6), 1) }, filesToCreate[2].Second.ToList());
            Assert.AreEqual(TestUtil.GetTestFile("controller\\pdf_create1\\Course 5-3.pdf"), filesToCreate[3].First);
            CollectionAssert.AreEqual(new CourseDesignator[] { new CourseDesignator(CourseId(6), 2) }, filesToCreate[3].Second.ToList());
            Assert.AreEqual(TestUtil.GetTestFile("controller\\pdf_create1\\Course 5-4.pdf"), filesToCreate[4].First);
            CollectionAssert.AreEqual(new CourseDesignator[] { new CourseDesignator(CourseId(6), 3) }, filesToCreate[4].Second.ToList());
            Assert.AreEqual(TestUtil.GetTestFile("controller\\pdf_create1\\All controls.pdf"), filesToCreate[5].First);
            CollectionAssert.AreEqual(new CourseDesignator[] { CourseDesignator.AllControls }, filesToCreate[5].Second.ToList());

            settings.PrintMapExchangesOnOneMap = true;

            coursePdf = new CoursePdf(eventDB, symbolDB, controller, controller.MapDisplay, settings, new CourseAppearance());
            filesToCreate = coursePdf.GetFilesToCreate();

            Assert.AreEqual(3, filesToCreate.Count);
            Assert.AreEqual(TestUtil.GetTestFile("controller\\pdf_create1\\Course 1.pdf"), filesToCreate[0].First);
            CollectionAssert.AreEqual(new CourseDesignator[] { Designator(1) }, filesToCreate[0].Second.ToList());
            Assert.AreEqual(TestUtil.GetTestFile("controller\\pdf_create1\\Course 5.pdf"), filesToCreate[1].First);
            CollectionAssert.AreEqual(new CourseDesignator[] { Designator(6) }, filesToCreate[1].Second.ToList());
            Assert.AreEqual(TestUtil.GetTestFile("controller\\pdf_create1\\All controls.pdf"), filesToCreate[2].First);
            CollectionAssert.AreEqual(new CourseDesignator[] { CourseDesignator.AllControls }, filesToCreate[2].Second.ToList());

        }

        [TestMethod]
        public void Files_SingleFile()
        {
            EventDB eventDB = controller.GetEventDB();
            SymbolDB symbolDB = ui.symbolDB;

            CoursePdfSettings settings = new CoursePdfSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("controller\\pdf_create1");
            settings.CourseIds = new Id<Course>[] { CourseId(1), CourseId(6), Id<Course>.None };
            settings.PaperSize = new System.Drawing.Printing.PaperSize("Letter", 850, 1100);
            settings.ColorModel = ColorModel.CMYK;
            settings.CropLargePrintArea = true;
            settings.FileCreation = CoursePdfSettings.PdfFileCreation.SingleFile;
            settings.PrintMapExchangesOnOneMap = false;
            settings.Margins = new System.Drawing.Printing.Margins(100, 25, 50, 120);

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\mapexchange1.ppen"), true);
            Assert.IsTrue(success);

            var coursePdf = new CoursePdf(eventDB, symbolDB, controller, controller.MapDisplay, settings, new CourseAppearance());
            var filesToCreate = coursePdf.GetFilesToCreate();

            Assert.AreEqual(1, filesToCreate.Count);
            Assert.AreEqual(TestUtil.GetTestFile("controller\\pdf_create1\\Marymoor WIOL 2.pdf"), filesToCreate[0].First);
            CollectionAssert.AreEqual(new CourseDesignator[] { Designator(1), 
                                                               new CourseDesignator(CourseId(6), 0),  new CourseDesignator(CourseId(6), 1),  new CourseDesignator(CourseId(6), 2),  new CourseDesignator(CourseId(6), 3),
                                                               CourseDesignator.AllControls },
                                      filesToCreate[0].Second.ToList());

            settings.PrintMapExchangesOnOneMap = true;

            coursePdf = new CoursePdf(eventDB, symbolDB, controller, controller.MapDisplay, settings, new CourseAppearance());
            filesToCreate = coursePdf.GetFilesToCreate();

            Assert.AreEqual(1, filesToCreate.Count);
            Assert.AreEqual(TestUtil.GetTestFile("controller\\pdf_create1\\Marymoor WIOL 2.pdf"), filesToCreate[0].First);
            CollectionAssert.AreEqual(new CourseDesignator[] { Designator(1), 
                                                               Designator(6),
                                                               CourseDesignator.AllControls },
                                      filesToCreate[0].Second.ToList());
        }

        [TestMethod]
        public void PdfCreation1()
        {
            CoursePdfSettings settings = new CoursePdfSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("controller\\pdf_create1");
            settings.CourseIds = new Id<Course>[] { CourseId(1), CourseId(2), CourseId(0) };
            settings.PaperSize = new System.Drawing.Printing.PaperSize("Letter", 850, 1100);
            settings.ColorModel = ColorModel.CMYK;
            settings.CropLargePrintArea = false;
            settings.FileCreation = CoursePdfSettings.PdfFileCreation.SingleFile;
            settings.PrintMapExchangesOnOneMap = false;
            settings.Margins = new System.Drawing.Printing.Margins(15, 15, 15, 15);

            CreatePdfFiles(TestUtil.GetTestFile("courseprinting\\marymoor.ppen"), settings, new CourseAppearance(),
                new string[1] { TestUtil.GetTestFile("controller\\pdf_create1\\Marymoor WIOL 2.pdf") },
                new string[1] { TestUtil.GetTestFile("controller\\pdf_create1\\test1_page%d_baseline.png") });
        }


        [TestMethod]
        public void PdfCreation4()
        {
            CoursePdfSettings settings = new CoursePdfSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("controller\\pdf_create4");
            settings.CourseIds = new Id<Course>[1] { CourseId(2) };
            settings.PaperSize = new System.Drawing.Printing.PaperSize("Letter", 850, 1100);
            settings.ColorModel = ColorModel.CMYK;
            settings.CropLargePrintArea = true;
            settings.FileCreation = CoursePdfSettings.PdfFileCreation.FilePerCourse;
            settings.Margins = new System.Drawing.Printing.Margins(100, 25, 50, 120);

            Directory.CreateDirectory(settings.outputDirectory);

            CreatePdfFiles(TestUtil.GetTestFile("controller\\marymoor4.ppen"), settings, new CourseAppearance(),
                new string[1] { TestUtil.GetTestFile("controller\\pdf_create4\\Course 2.pdf") },
                new string[1] { TestUtil.GetTestFile("controller\\pdf_create4\\Course 2_expected.png") });
        }

    }
}
#endif
