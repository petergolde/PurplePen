#if TEST


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.Tests
{
    [TestClass]
    public class CreateBitmapTests: TestFixtureBase
    {
        Controller controller;
        TestUI ui;

        const int MAX_PIXEL_DIFF = 30;

        [TestInitialize]
        public void Setup()
        {
            ui = TestUI.Create();
            controller = ui.controller;
        }


        // Create some courses, write them, and check against a dump.
        void CreateBitmapFiles(string file, BitmapCreationSettings settings, CourseAppearance appearance, 
                               string[] expectedBitmapNames, string[] expectedBitmapBaselines, 
                               string[] expectedTextNames = null, string[] expectedTextBaselines = null)
        {
            EventDB eventDB = controller.GetEventDB();

            for (int i = 0; i < expectedBitmapNames.Length; ++i) {
                File.Delete(expectedBitmapNames[i]);
            }

            if (expectedTextNames != null) {
                for (int i = 0; i < expectedTextNames.Length; ++i) {
                    File.Delete(expectedTextNames[i]);
                }
            }

            bool success = controller.LoadInitialFile(file, true);
            Assert.IsTrue(success);

            controller.SetCourseAppearance(appearance);
            if (controller.LowerPurpleMapLayer != null)
                controller.MapDisplay.LowerPurpleMapLayer = controller.LowerPurpleMapLayer;

            for (int i = 0; i < expectedBitmapNames.Length; ++i) {
                File.Delete(expectedBitmapNames[i]);
            }

            if (expectedTextNames != null) {
                for (int i = 0; i < expectedTextNames.Length; ++i) {
                    File.Delete(expectedTextNames[i]);
                }
            }

            success = controller.CreateBitmapFiles(settings);
            Assert.IsTrue(success);

            for (int i = 0; i < expectedBitmapNames.Length; ++i) {
                TestUtil.CompareBitmapBaseline(expectedBitmapNames[i], expectedBitmapBaselines[i], MAX_PIXEL_DIFF);
            }

            if (expectedTextNames != null) {
                for (int i = 0; i < expectedTextNames.Length; ++i) {
                    TestUtil.CompareTextFileBaseline(expectedTextNames[i], expectedTextBaselines[i]);
                }
            }

            for (int i = 0; i < expectedBitmapNames.Length; ++i) {
                File.Delete(expectedBitmapNames[i]);
            }

            if (expectedTextNames != null) {
                for (int i = 0; i < expectedTextNames.Length; ++i) {
                    File.Delete(expectedTextNames[i]);
                }
            }
        }

        [TestMethod]
        public void BitmapCreation1()
        {
            BitmapCreationSettings settings = new BitmapCreationSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("bitmapcreate\\create1");
            settings.CourseIds = new Id<Course>[1] { CourseId(1) };
            settings.Dpi = 200;
            settings.ExportedBitmapKind = BitmapCreationSettings.BitmapKind.Jpeg;
 
            Directory.CreateDirectory(settings.outputDirectory);

            CreateBitmapFiles(TestUtil.GetTestFile("bitmapcreate\\GRC.ppen"), settings, new CourseAppearance(),
                new string[1] { TestUtil.GetTestFile("bitmapcreate\\create1\\Course 1.jpg") },
                new string[1] { TestUtil.GetTestFile("bitmapcreate\\create1\\Course 1_baseline.jpg") });
        }

        [TestMethod]
        public void BitmapCreation2()
        {
            BitmapCreationSettings settings = new BitmapCreationSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("bitmapcreate\\create2");
            settings.CourseIds = new Id<Course>[] { CourseId(0), CourseId(1), CourseId(2) };
            settings.Dpi = 200;
            settings.ExportedBitmapKind = BitmapCreationSettings.BitmapKind.Png;
            settings.ColorModel = ColorModel.RGB;
            settings.WorldFile = true;
            settings.filePrefix = "BM";

            Directory.CreateDirectory(settings.outputDirectory);

            CreateBitmapFiles(TestUtil.GetTestFile("bitmapcreate\\GRC.ppen"), settings, new CourseAppearance(),
                new string[3] { TestUtil.GetTestFile("bitmapcreate\\create2\\BM-All Controls.png"),
                                TestUtil.GetTestFile("bitmapcreate\\create2\\BM-Course 1.png"),
                                TestUtil.GetTestFile("bitmapcreate\\create2\\BM-Course 2.png")},
                new string[3] { TestUtil.GetTestFile("bitmapcreate\\create2\\BM_All Controls_baseline.png"),
                                TestUtil.GetTestFile("bitmapcreate\\create2\\BM_Course 1_baseline.png"),
                                TestUtil.GetTestFile("bitmapcreate\\create2\\BM_Course 2_baseline.png")},
                new string[] { TestUtil.GetTestFile("bitmapcreate\\create2\\BM-All Controls.pgw"),
                               TestUtil.GetTestFile("bitmapcreate\\create2\\BM-Course 1.pgw"),
                               TestUtil.GetTestFile("bitmapcreate\\create2\\BM-Course 2.pgw")},
                new string[] { TestUtil.GetTestFile("bitmapcreate\\create2\\BM-All Controls_baseline.pgw"),
                               TestUtil.GetTestFile("bitmapcreate\\create2\\BM-Course 1_baseline.pgw"),
                               TestUtil.GetTestFile("bitmapcreate\\create2\\BM-Course 2_baseline.pgw")}
                );
        }

        [TestMethod]
        public void BitmapCreation3()
        {
            BitmapCreationSettings settings = new BitmapCreationSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("bitmapcreate\\create3");
            settings.CourseIds = new Id<Course>[] { CourseId(3), CourseId(4) };
            settings.Dpi = 120;
            settings.ExportedBitmapKind = BitmapCreationSettings.BitmapKind.Gif;
            settings.ColorModel = ColorModel.CMYK;
            settings.filePrefix = "";

            Directory.CreateDirectory(settings.outputDirectory);

            CreateBitmapFiles(TestUtil.GetTestFile("bitmapcreate\\GRC.ppen"), settings, new CourseAppearance(),
                new string[] { TestUtil.GetTestFile("bitmapcreate\\create3\\Exchg-1.gif"),
                               TestUtil.GetTestFile("bitmapcreate\\create3\\Exchg-2.gif"),
                               TestUtil.GetTestFile("bitmapcreate\\create3\\Relay AC.gif"),
                               TestUtil.GetTestFile("bitmapcreate\\create3\\Relay AD.gif"),
                               TestUtil.GetTestFile("bitmapcreate\\create3\\Relay BC.gif"),
                               TestUtil.GetTestFile("bitmapcreate\\create3\\Relay BD.gif"),
                },
                new string[] { TestUtil.GetTestFile("bitmapcreate\\create3\\Exchg-1_baseline.png"),
                               TestUtil.GetTestFile("bitmapcreate\\create3\\Exchg-2_baseline.png"),
                               TestUtil.GetTestFile("bitmapcreate\\create3\\Relay AC_baseline.png"),
                               TestUtil.GetTestFile("bitmapcreate\\create3\\Relay AD_baseline.png"),
                               TestUtil.GetTestFile("bitmapcreate\\create3\\Relay BC_baseline.png"),
                               TestUtil.GetTestFile("bitmapcreate\\create3\\Relay BD_baseline.png"),
                });
        }

        [TestMethod]
        public void BitmapCreation4()
        {
            BitmapCreationSettings settings = new BitmapCreationSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("bitmapcreate\\create4");
            settings.CourseIds = new Id<Course>[] { CourseId(1), CourseId(2) };
            settings.Dpi = 300;
            settings.ExportedBitmapKind = BitmapCreationSettings.BitmapKind.Jpeg;

            Directory.CreateDirectory(settings.outputDirectory);

            CreateBitmapFiles(TestUtil.GetTestFile("bitmapcreate\\StEd.ppen"), settings, new CourseAppearance(),
                new string[] { TestUtil.GetTestFile("bitmapcreate\\create4\\Course 1.jpg"),
                               TestUtil.GetTestFile("bitmapcreate\\create4\\Course 2.jpg")},
                new string[] { TestUtil.GetTestFile("bitmapcreate\\create4\\Course 1_baseline.png"),
                               TestUtil.GetTestFile("bitmapcreate\\create4\\Course 2_baseline.png")});
        }

        [TestMethod]
        public void BitmapCreation5()
        {
            BitmapCreationSettings settings = new BitmapCreationSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("bitmapcreate\\create5");
            settings.CourseIds = new Id<Course>[] { CourseId(1), CourseId(2) };
            settings.Dpi = 300;
            settings.ExportedBitmapKind = BitmapCreationSettings.BitmapKind.Jpeg;

            Directory.CreateDirectory(settings.outputDirectory);

            CourseAppearance appearance = new CourseAppearance();
            appearance.numberRoboto = false;

            CreateBitmapFiles(TestUtil.GetTestFile("bitmapcreate\\StEd.ppen"), settings, appearance,
                new string[] { TestUtil.GetTestFile("bitmapcreate\\create5\\Course 1.jpg"),
                               TestUtil.GetTestFile("bitmapcreate\\create5\\Course 2.jpg")},
                new string[] { TestUtil.GetTestFile("bitmapcreate\\create5\\Course 1_baseline.png"),
                               TestUtil.GetTestFile("bitmapcreate\\create5\\Course 2_baseline.png")});
        }

        [TestMethod]
        public void BitmapCreationBlendNone()
        {
            BitmapCreationSettings settings = new BitmapCreationSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("bitmapcreate\\blend\\none");
            settings.CourseIds = new Id<Course>[] { CourseId(8) };
            settings.Dpi = 300;
            settings.ExportedBitmapKind = BitmapCreationSettings.BitmapKind.Png;

            Directory.CreateDirectory(settings.outputDirectory);

            CourseAppearance appearance = new CourseAppearance();
            appearance.numberRoboto = false;
            appearance.purpleColorBlend = PurpleColorBlend.None;
            appearance.itemScaling = ItemScaling.None;

            CreateBitmapFiles(TestUtil.GetTestFile("courseprinting\\Lord Hill Feb 2024 - Final.ppen"), settings, appearance,
                new string[] { TestUtil.GetTestFile("bitmapcreate\\blend\\none\\Course 5.png"),},
                new string[] { TestUtil.GetTestFile("bitmapcreate\\blend\\none\\Course 5_baseline.png")});
        }

        [TestMethod]
        public void BitmapCreationBlend()
        {
            BitmapCreationSettings settings = new BitmapCreationSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("bitmapcreate\\blend\\blend");
            settings.CourseIds = new Id<Course>[] { CourseId(8) };
            settings.Dpi = 300;
            settings.ExportedBitmapKind = BitmapCreationSettings.BitmapKind.Png;

            Directory.CreateDirectory(settings.outputDirectory);

            CourseAppearance appearance = new CourseAppearance();
            appearance.numberRoboto = false;
            appearance.purpleColorBlend = PurpleColorBlend.Blend;
            appearance.itemScaling = ItemScaling.None;

            CreateBitmapFiles(TestUtil.GetTestFile("courseprinting\\Lord Hill Feb 2024 - Final.ppen"), settings, appearance,
                new string[] { TestUtil.GetTestFile("bitmapcreate\\blend\\blend\\Course 5.png"), },
                new string[] { TestUtil.GetTestFile("bitmapcreate\\blend\\blend\\Course 5_baseline.png") });
        }

        [TestMethod]
        public void BitmapCreationBlendLayer()
        {
            BitmapCreationSettings settings = new BitmapCreationSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("bitmapcreate\\blend\\layer");
            settings.CourseIds = new Id<Course>[] { CourseId(8) };
            settings.Dpi = 300;
            settings.ExportedBitmapKind = BitmapCreationSettings.BitmapKind.Png;

            Directory.CreateDirectory(settings.outputDirectory);

            CourseAppearance appearance = new CourseAppearance();
            appearance.numberRoboto = false;
            appearance.purpleColorBlend = PurpleColorBlend.UpperLowerPurple;
            appearance.mapLayerForLowerPurple = 10;
            appearance.itemScaling = ItemScaling.None;

            CreateBitmapFiles(TestUtil.GetTestFile("courseprinting\\Lord Hill Feb 2024 - Final.ppen"), settings, appearance,
                new string[] { TestUtil.GetTestFile("bitmapcreate\\blend\\layer\\Course 5.png"), },
                new string[] { TestUtil.GetTestFile("bitmapcreate\\blend\\layer\\Course 5_baseline.png") });
        }

        [TestMethod]
        public void BitmapCreationBlendLayerSprint()
        {
            BitmapCreationSettings settings = new BitmapCreationSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("bitmapcreate\\blend\\layersprint");
            settings.CourseIds = new Id<Course>[] { CourseId(8) };
            settings.Dpi = 300;
            settings.ExportedBitmapKind = BitmapCreationSettings.BitmapKind.Png;

            Directory.CreateDirectory(settings.outputDirectory);

            CourseAppearance appearance = new CourseAppearance();
            appearance.mapStandard = "Spr2019";
            appearance.numberRoboto = false;
            appearance.purpleColorBlend = PurpleColorBlend.UpperLowerPurple;
            appearance.mapLayerForLowerPurple = 10;
            appearance.itemScaling = ItemScaling.None;

            CreateBitmapFiles(TestUtil.GetTestFile("courseprinting\\Lord Hill Feb 2024 - Final.ppen"), settings, appearance,
                new string[] { TestUtil.GetTestFile("bitmapcreate\\blend\\layersprint\\Course 5.png"), },
                new string[] { TestUtil.GetTestFile("bitmapcreate\\blend\\layersprint\\Course 5_baseline.png") });
        }



    }
}

#endif