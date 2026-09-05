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
using System.Threading.Tasks;

// Things to test:
// -- score course


namespace PurplePen.Tests
{
    [TestClass]
    public class GpxTests: TestFixtureBase
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
        public async Task ExportGpx()
        {
            string outputFile = TestUtil.GetTestFile("gpx\\lickcreek_actual.gpx");
            string expectedFile = TestUtil.GetTestFile("gpx\\lickcreek_expected.gpx");

            await Setup("gpx\\Lick Creek 2014.ppen");

            controller.ExportGpx(outputFile, new GpxCreationSettings() {
                CourseIds = new Id<Course>[] { Id<Course>.None, new Id<Course>(3), new Id<Course>(4), new Id<Course>(2) },
                CodePrefix = "PR"
            });

            TextFileTestUtil.CompareTextFileBaseline(outputFile, expectedFile, GpxFile.TestFileExceptionMap());
            Assert.AreEqual("", ui.output.ToString());
        }

        [TestMethod]
        public async Task ExportGpx2()
        {
            string outputFile = TestUtil.GetTestFile("gpx\\lickcreek2_actual.gpx");
            string expectedFile = TestUtil.GetTestFile("gpx\\lickcreek2_expected.gpx");

            await Setup("gpx\\Lick Creek 2014.ppen");

            controller.ExportGpx(outputFile, new GpxCreationSettings() {
                CourseIds = new Id<Course>[] { new Id<Course>(5) },
                CodePrefix = "S"
            });

            TextFileTestUtil.CompareTextFileBaseline(outputFile, expectedFile, GpxFile.TestFileExceptionMap());
            Assert.AreEqual("", ui.output.ToString());
        }

        [TestMethod]
        public async Task ExportGpxProj1()
        {
            string outputFile = TestUtil.GetTestFile("gpx\\testproj1_actual.gpx");
            string expectedFile = TestUtil.GetTestFile("gpx\\testproj1_expected.gpx");

            await Setup("gpx\\testproj1.ppen");

            controller.ExportGpx(outputFile, new GpxCreationSettings() {
                CourseIds = new Id<Course>[] { Id<Course>.None },
                CodePrefix = ""
            });

            TextFileTestUtil.CompareTextFileBaseline(outputFile, expectedFile, GpxFile.TestFileExceptionMap());
            Assert.AreEqual("", ui.output.ToString());
        }


        [TestMethod]
        public async Task ExportGpxProj2()
        {
            string outputFile = TestUtil.GetTestFile("gpx\\testproj2_actual.gpx");
            string expectedFile = TestUtil.GetTestFile("gpx\\testproj2_expected.gpx");

            await Setup("gpx\\testproj2.ppen");

            controller.ExportGpx(outputFile, new GpxCreationSettings() {
                CourseIds = new Id<Course>[] { Id<Course>.None },
                CodePrefix = ""
            });

            TextFileTestUtil.CompareTextFileBaseline(outputFile, expectedFile, GpxFile.TestFileExceptionMap());
            Assert.AreEqual("", ui.output.ToString());
        }

        [TestMethod]
        public async Task ExportGpxProj3()
        {
            string outputFile = TestUtil.GetTestFile("gpx\\testproj3_actual.gpx");
            string expectedFile = TestUtil.GetTestFile("gpx\\testproj3_expected.gpx");

            await Setup("gpx\\testproj3.ppen");

            controller.ExportGpx(outputFile, new GpxCreationSettings() {
                CourseIds = new Id<Course>[] { Id<Course>.None },
                CodePrefix = ""
            });

            TextFileTestUtil.CompareTextFileBaseline(outputFile, expectedFile, GpxFile.TestFileExceptionMap());
            Assert.AreEqual("", ui.output.ToString());
        }

        [TestMethod]
        public async Task ExportGpxProj4()
        {
            string outputFile = TestUtil.GetTestFile("gpx\\testproj4_actual.gpx");
            string expectedFile = TestUtil.GetTestFile("gpx\\testproj4_expected.gpx");

            await Setup("gpx\\testproj4.ppen");

            controller.ExportGpx(outputFile, new GpxCreationSettings() {
                CourseIds = new Id<Course>[] { Id<Course>.None },
                CodePrefix = ""
            });

            TextFileTestUtil.CompareTextFileBaseline(outputFile, expectedFile, GpxFile.TestFileExceptionMap());
            Assert.AreEqual("", ui.output.ToString());
        }


        [TestMethod]
        public async Task ExportGpxNotOcad()
        {
            string outputFile = TestUtil.GetTestFile("gpx\\lincoln_actual.gpx");
            string expectedFile = TestUtil.GetTestFile("gpx\\lincoln_expected.gpx");

            await Setup("gpx\\Lincoln Park PDF.ppen");

            controller.ExportGpx(outputFile, new GpxCreationSettings() {
                CourseIds = new Id<Course>[] { Id<Course>.None, },
                CodePrefix = ""
            });

            Assert.AreEqual("ERROR: 'Cannot create '" + outputFile + "' for the following reason:\r\n\r\nThe map file must be an OCAD file to use GPX files.'\r\n", ui.output.ToString());
            Assert.IsFalse(File.Exists(outputFile));
        }

        [TestMethod]
        public async Task ExportGpxNoRealWorld()
        {
            string outputFile = TestUtil.GetTestFile("gpx\\lickcreeknorealworld_actual.gpx");
            string expectedFile = TestUtil.GetTestFile("gpx\\lickcreeknorealworld_expected.gpx");

            await Setup("gpx\\Lick Creek 2014 NoRealWorld.ppen");

            controller.ExportGpx(outputFile, new GpxCreationSettings() {
                CourseIds = new Id<Course>[] { Id<Course>.None, new Id<Course>(3), new Id<Course>(4), new Id<Course>(2) },
                CodePrefix = ""
            });

            Assert.AreEqual("ERROR: 'Cannot create '" + outputFile +"' for the following reason:\r\n\r\nThe OCAD file must have real world coordinates defined to use GPX files.'\r\n", 
                            ui.output.ToString());
            Assert.IsFalse(File.Exists(outputFile));
        }

        [TestMethod]
        public async Task ExportGpxNoCoordSystem()
        {
            string outputFile = TestUtil.GetTestFile("gpx\\lickcreeknocoord_actual.gpx");
            string expectedFile = TestUtil.GetTestFile("gpx\\lickcreeknocoord_expected.gpx");

            await Setup("gpx\\Lick Creek 2014 NoCoordSystem.ppen");

            controller.ExportGpx(outputFile, new GpxCreationSettings() {
                CourseIds = new Id<Course>[] { Id<Course>.None, new Id<Course>(3), new Id<Course>(4), new Id<Course>(2) },
                CodePrefix = ""
            });

            Assert.AreEqual("ERROR: 'Cannot create '" + outputFile + "' for the following reason:\r\n\r\nThe OCAD file must have a coordinate system defined to use GPX files.'\r\n", ui.output.ToString());
            Assert.IsFalse(File.Exists(outputFile));
        }

        [TestMethod]
        public async Task ExportGpxUnsupCoordSystem()
        {
            string outputFile = TestUtil.GetTestFile("gpx\\lickcreekunsupcoord_actual.gpx");
            string expectedFile = TestUtil.GetTestFile("gpx\\lickcreekunsupcoord_expected.gpx");

            await Setup("gpx\\Lick Creek 2014 UnsupCoordSystem.ppen");

            controller.ExportGpx(outputFile, new GpxCreationSettings() {
                CourseIds = new Id<Course>[] { Id<Course>.None, new Id<Course>(3), new Id<Course>(4), new Id<Course>(2) },
                CodePrefix = ""
            });

            Assert.AreEqual("ERROR: 'Cannot create '" + outputFile + "' for the following reason:\r\n\r\nThe OCAD file uses a coordinate system that is not supported by Purple Pen.'\r\n", ui.output.ToString());
            Assert.IsFalse(File.Exists(outputFile));
        }




    }
}

#endif //TEST
