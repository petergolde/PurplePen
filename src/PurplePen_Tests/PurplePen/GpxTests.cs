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

// Things to test:
// -- score course


namespace PurplePen.Tests
{
    [TestClass]
    public class GpxTests
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
        public void ExportGpx()
        {
            string outputFile = TestUtil.GetTestFile("gpx\\lickcreek_actual.gpx");
            string expectedFile = TestUtil.GetTestFile("gpx\\lickcreek_expected.gpx");

            Setup("gpx\\Lick Creek 2014.ppen");

            controller.ExportGpx(outputFile, new GpxCreationSettings() {
                CourseIds = new Id<Course>[] { Id<Course>.None, new Id<Course>(3), new Id<Course>(4), new Id<Course>(2) },
                CodePrefix = "PR"
            });

            TestUtil.CompareTextFileBaseline(outputFile, expectedFile, GpxFile.TestFileExceptionMap());
        }

        [TestMethod]
        public void ExportGpx2()
        {
            string outputFile = TestUtil.GetTestFile("gpx\\lickcreek2_actual.gpx");
            string expectedFile = TestUtil.GetTestFile("gpx\\lickcreek2_expected.gpx");

            Setup("gpx\\Lick Creek 2014.ppen");

            controller.ExportGpx(outputFile, new GpxCreationSettings() {
                CourseIds = new Id<Course>[] { new Id<Course>(5) },
                CodePrefix = "S"
            });

            TestUtil.CompareTextFileBaseline(outputFile, expectedFile, GpxFile.TestFileExceptionMap());
        }

        [TestMethod]
        public void ExportGpxProj1()
        {
            string outputFile = TestUtil.GetTestFile("gpx\\testproj1_actual.gpx");
            string expectedFile = TestUtil.GetTestFile("gpx\\testproj1_expected.gpx");

            Setup("gpx\\testproj1.ppen");

            controller.ExportGpx(outputFile, new GpxCreationSettings() {
                CourseIds = new Id<Course>[] { Id<Course>.None },
                CodePrefix = ""
            });

            TestUtil.CompareTextFileBaseline(outputFile, expectedFile, GpxFile.TestFileExceptionMap());
        }


        [TestMethod]
        public void ExportGpxProj2()
        {
            string outputFile = TestUtil.GetTestFile("gpx\\testproj2_actual.gpx");
            string expectedFile = TestUtil.GetTestFile("gpx\\testproj2_expected.gpx");

            Setup("gpx\\testproj2.ppen");

            controller.ExportGpx(outputFile, new GpxCreationSettings() {
                CourseIds = new Id<Course>[] { Id<Course>.None },
                CodePrefix = ""
            });

            TestUtil.CompareTextFileBaseline(outputFile, expectedFile, GpxFile.TestFileExceptionMap());
        }

        [TestMethod]
        public void ExportGpxProj3()
        {
            string outputFile = TestUtil.GetTestFile("gpx\\testproj3_actual.gpx");
            string expectedFile = TestUtil.GetTestFile("gpx\\testproj3_expected.gpx");

            Setup("gpx\\testproj3.ppen");

            controller.ExportGpx(outputFile, new GpxCreationSettings() {
                CourseIds = new Id<Course>[] { Id<Course>.None },
                CodePrefix = ""
            });

            TestUtil.CompareTextFileBaseline(outputFile, expectedFile, GpxFile.TestFileExceptionMap());
        }

        [TestMethod]
        public void ExportGpxProj4()
        {
            string outputFile = TestUtil.GetTestFile("gpx\\testproj4_actual.gpx");
            string expectedFile = TestUtil.GetTestFile("gpx\\testproj4_expected.gpx");

            Setup("gpx\\testproj4.ppen");

            controller.ExportGpx(outputFile, new GpxCreationSettings() {
                CourseIds = new Id<Course>[] { Id<Course>.None },
                CodePrefix = ""
            });

            TestUtil.CompareTextFileBaseline(outputFile, expectedFile, GpxFile.TestFileExceptionMap());
        }



    }
}

#endif //TEST
