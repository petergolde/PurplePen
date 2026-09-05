/* Copyright 2006-2026 Peter Golde
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 * this list of conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 *
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names of its
 * contributors may be used to endorse or promote products derived from this
 * software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

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
using System.Threading.Tasks;

namespace PurplePen.Tests
{
    [TestClass]
    public class SelectionDescriberTests: TestFixtureBase
    {
        TestUI ui;
        EventDB eventDB;
        SelectionMgr selectionMgr;
        Controller controller;

        [TestInitialize]
        public void Setup()
        {
            ui = TestUI.Create();
            controller = ui.controller;
            selectionMgr = controller.GetSelectionMgr();
            eventDB = controller.GetEventDB();
        }

        [TestMethod]
        public async Task SelectedControl1()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(2));
            selectionMgr.SelectControl(ControlId(70));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Control 70", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Location:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("(21.3, 11.8)", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Course 1, Course 2", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Text description:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("NE end of stream", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedControl1Toolstip()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(2));
            CourseLayout layout = controller.GetCourseLayout();
            CourseObj courseObj = (from co in layout where co.controlId == ControlId(70) && co is ControlCourseObj select co).Single();

            TextPart[] description = SelectionDescriber.DescribeCourseObject(ui.symbolDB, eventDB, courseObj, selectionMgr.ActiveCourseView.ScaleRatio);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Control 70", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Course 1, Course 2", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedControl2()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectControl(ControlId(81));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Control 60", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Location:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("(-12.9, 25.3)", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("None", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Text description:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("E edge of thicket", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedControl2Tooltip()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectControl(ControlId(81));
            CourseLayout layout = controller.GetCourseLayout();
            CourseObj courseObj = (from co in layout where co.controlId == ControlId(81) && co is ControlCourseObj select co).Single();
            TextPart[] description = SelectionDescriber.DescribeCourseObject(ui.symbolDB, eventDB, courseObj, selectionMgr.ActiveCourseView.ScaleRatio);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Control 60", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("None", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedControlWithLoad()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(2));
            selectionMgr.SelectControl(ControlId(53));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Control 53", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Location:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("(25.8, -10.5)", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Course 1, Course 2, Course 3", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Competitor load:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("6", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Text description:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("SW side of lone tree", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedControlWithLoadTooltip()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(2));
            CourseLayout layout = controller.GetCourseLayout();
            CourseObj courseObj = (from co in layout where co.controlId == ControlId(53) && co is ControlCourseObj select co).Single();
            TextPart[] description = SelectionDescriber.DescribeCourseObject(ui.symbolDB, eventDB, courseObj, selectionMgr.ActiveCourseView.ScaleRatio);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Control 53", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Course 1, Course 2, Course 3", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Load:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("6", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedStart()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(2));
            selectionMgr.SelectControl(ControlId(1));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Start", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Location:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("(56.8, -8.7)", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("All courses", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Text description:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Start: clearing", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedStartTooltip()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(2));
            CourseLayout layout = controller.GetCourseLayout();
            CourseObj courseObj = (from co in layout where co.controlId == ControlId(1) && co is StartCourseObj select co).Single();
            TextPart[] description = SelectionDescriber.DescribeCourseObject(ui.symbolDB, eventDB, courseObj, selectionMgr.ActiveCourseView.ScaleRatio);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Start", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("All courses", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedStartWithLoad()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(2));
            selectionMgr.SelectControl(ControlId(1));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Start", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Location:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("(56.8, -8.7)", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Course 1, Course 2, Course 3, Course 4B, Course 4G, Course 5, Score, StartAngle", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Competitor load:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("115", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Text description:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Start: clearing", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedFinish()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(2));
            selectionMgr.SelectControl(ControlId(2));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Finish", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Location:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("(53.2, -2.8)", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("All courses", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Text description:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Navigate  to finish", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedMandatoryCrossingPoint()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(5));
            selectionMgr.SelectControl(ControlId(83));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Mandatory crossing point", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Location:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("(5.0, -12.0)", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Course 4G", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Text description:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Mandatory crossing point", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedSymbolKey()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\sampleevent2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(6));
            selectionMgr.SelectKeyLine(ui.symbolDB["6.1"]);
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Customized symbol description", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);
        }

        [TestMethod]
        public async Task SelectedTextLine()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\desctext.ppen"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(6));
            selectionMgr.SelectTextLine(ControlId(18), CourseControlId(208), DescriptionLine.TextLineKind.BeforeControl);
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Text line", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Location:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Appears above Control 303, in all courses that contain Control 303", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);
        }

        [TestMethod]
        public async Task SelectedCourse()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(3));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Course \"Course 3\"", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Length:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("2.94 km", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Climb:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("37 m", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedCourseCustomLength()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(6));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Course \"Course 5\"", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Length:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("7.60 km", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Calculated Length:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("5.00 km", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedCourseWithLoad()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(3));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Course \"Course 3\"", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Length:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("2.94 km", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Climb:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("37 m", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Competitor load:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("3", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedCourseMapExchange()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\mapexchange1.ppen"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(6, 1));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Course \"Course 5\", Part 2", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Length:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("1.20 km", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Competitor load:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("22", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }


        [TestMethod]
        public async Task ScoreCourse()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(9));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Course \"Score\"", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Total controls:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("13", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Total score:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("135", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedLeg()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(6));
            selectionMgr.SelectLeg(CourseControlId(606), CourseControlId(607), LegInsertionLoc.Normal);
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Control 48 \u2013 Control 50", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Length:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("294 m", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Course 4B, Course 5", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Flagging:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("none", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedLegTooltip()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(6));
            CourseLayout layout = controller.GetCourseLayout();
            CourseObj courseObj = (from co in layout where co.courseControlId == CourseControlId(606) && co is LegCourseObj && ((LegCourseObj)co).courseControlId2 == CourseControlId(607) select co).Single();
            TextPart[] description = SelectionDescriber.DescribeCourseObject(ui.symbolDB, eventDB, courseObj, selectionMgr.ActiveCourseView.ScaleRatio);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Control 48 \u2013 Control 50", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Length:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("294 m", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Course 4B, Course 5", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedLegWithLoad()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(6));
            selectionMgr.SelectLeg(CourseControlId(606), CourseControlId(607), LegInsertionLoc.Normal);
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Control 48 \u2013 Control 50", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Length:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("294 m", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Course 4B, Course 5", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Competitor load:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("9", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Flagging:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("none", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }


        [TestMethod]
        public async Task SelectedLegWithLoadTooltip()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(6));
            CourseLayout layout = controller.GetCourseLayout();
            CourseObj courseObj = (from co in layout where co.courseControlId == CourseControlId(606) && co is LegCourseObj && ((LegCourseObj)co).courseControlId2 == CourseControlId(607) select co).Single();
            TextPart[] description = SelectionDescriber.DescribeCourseObject(ui.symbolDB, eventDB, courseObj, selectionMgr.ActiveCourseView.ScaleRatio);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Control 48 \u2013 Control 50", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Length:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("294 m", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Course 4B, Course 5", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Load:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("9", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }


        [TestMethod]
        public async Task SelectedLegBends()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\SpecialLegs.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(1));
            selectionMgr.SelectLeg(CourseControlId(4), CourseControlId(5), LegInsertionLoc.Normal);
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Control 33 \u2013 Control 34", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Length:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("519 m", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("All courses", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Flagging:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("none", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }


        [TestMethod]
        public async Task SelectedLegFlagging1()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\SpecialLegs.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(1));
            selectionMgr.SelectLeg(CourseControlId(2), CourseControlId(3), LegInsertionLoc.Normal);
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Control 31 \u2013 Control 32", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Length:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("420 m", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("All courses", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Flagging:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("entire leg", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedLegFlagging2()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\SpecialLegs.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(1));
            selectionMgr.SelectLeg(CourseControlId(5), CourseControlId(6), LegInsertionLoc.Normal);
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Control 34 \u2013 Finish", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Length:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("410 m", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("All courses", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Flagging:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("entire leg", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedMapExchangeAtControl()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\mapexchange2.ppen"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(6));
            selectionMgr.SelectMapExchangeAtControl(ControlId(43), CourseControlId(615));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Map exchange at Control 43", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Text description:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Map exchange at the control", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedMapFlipAtControl()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\mapexchange3.ppen"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(6));
            selectionMgr.SelectMapExchangeAtControl(ControlId(43), CourseControlId(615));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Map flip at Control 43", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Text description:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Flip map over", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task OutOfBounds()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(4));
            selectionMgr.SelectSpecial(SpecialId(4));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Out-of-bounds Area", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("All courses", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task OutOfBoundsTooltip()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(4));
            CourseLayout layout = controller.GetCourseLayout();
            CourseObj courseObj = (from co in layout where co.specialId == SpecialId(4) select co).Single();
            TextPart[] description = SelectionDescriber.DescribeCourseObject(ui.symbolDB, eventDB, courseObj, selectionMgr.ActiveCourseView.ScaleRatio);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Out-of-bounds Area", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("All courses", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task OptionalCrossingPoint()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(3));
            selectionMgr.SelectSpecial(SpecialId(2));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Optional Crossing Point", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Location:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("(-4.2, 21.7)", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Course 1, Course 2, Course 3, Score", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task OptionalCrossingPointTooltip()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(3));
            CourseLayout layout = controller.GetCourseLayout();
            CourseObj courseObj = (from co in layout where co.specialId == SpecialId(2) select co).Single();
            TextPart[] description = SelectionDescriber.DescribeCourseObject(ui.symbolDB, eventDB, courseObj, selectionMgr.ActiveCourseView.ScaleRatio);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Optional Crossing Point", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Course 1, Course 2, Course 3, Score", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }


        [TestMethod]
        public async Task Description()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(10));
            selectionMgr.SelectSpecial(SpecialId(8));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Control Descriptions", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Location:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("(-50.0, 50.0)", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Line height:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("5.0 mm", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("All controls, Course 1, Course 3, Course 4B, Course 4G, Score, SingleControl, StartAngle, Xavier", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }


        [TestMethod]
        public async Task DescriptionTooltip()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(10));
            CourseLayout layout = controller.GetCourseLayout();
            CourseObj courseObj = (from co in layout where co.specialId == SpecialId(8) select co).Single();
            TextPart[] description = SelectionDescriber.DescribeCourseObject(ui.symbolDB, eventDB, courseObj, selectionMgr.ActiveCourseView.ScaleRatio);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Control Descriptions", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Line height:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("5.0 mm", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("All controls, Course 1, Course 3, Course 4B, Course 4G, Score, SingleControl, StartAngle, Xavier", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task DescriptionScaled()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(5));
            selectionMgr.SelectSpecial(SpecialId(8));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Control Descriptions", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Location:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("(-50.0, 50.0)", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Line height:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("3.3 mm", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("All controls, Course 1, Course 3, Course 4B, Course 4G, Score, SingleControl, StartAngle, Xavier", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task DescriptionScaledTooltip()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(5));
            CourseLayout layout = controller.GetCourseLayout();
            CourseObj courseObj = (from co in layout where co.specialId == SpecialId(8) select co).Single();
            TextPart[] description = SelectionDescriber.DescribeCourseObject(ui.symbolDB, eventDB, courseObj, selectionMgr.ActiveCourseView.ScaleRatio);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Control Descriptions", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Line height:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("3.3 mm", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("All controls, Course 1, Course 3, Course 4B, Course 4G, Score, SingleControl, StartAngle, Xavier", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task ImageBitmap()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(10));
            selectionMgr.SelectSpecial(SpecialId(9));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Image", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Name:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("testimage.jpg", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("All courses", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }


        [TestMethod]
        public async Task LineSpecial()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(10));
            selectionMgr.SelectSpecial(SpecialId(10));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Line", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Length:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("675 m", textpart.text);
            Assert.AreEqual(TextFormat.SameLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("All courses", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }


        [TestMethod]
        public async Task RectangleSpecial()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(10));
            selectionMgr.SelectSpecial(SpecialId(11));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Rectangle", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("All courses", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task RectangleSpecialAllButOne()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor3.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(10));
            selectionMgr.SelectSpecial(SpecialId(11));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Rectangle", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("All controls, Course 1, Course 2, Course 3, Course 4G, Course 5, Score, SingleControl, StartAngle, Xavier", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }



        [TestMethod]
        public async Task Text()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(10));
            selectionMgr.SelectSpecial(SpecialId(6));
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("Text", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

#if false
            textpart = description[index++];
            Assert.AreEqual("Font:  ", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("12.4 point Arial bold", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);
#endif

            textpart = description[index++];
            Assert.AreEqual("Used in courses:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Xavier", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }

        [TestMethod]
        public async Task SelectedAllControls()
        {
            TextPart textpart;
            int index;

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile("selectiondescriber\\marymoor.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(CourseDesignator.AllControls);
            TextPart[] description = SelectionDescriber.DescribeSelection(ui.symbolDB, eventDB, selectionMgr.ActiveCourseView, selectionMgr.Selection);
            index = 0;

            textpart = description[index++];
            Assert.AreEqual("All controls", textpart.text);
            Assert.AreEqual(TextFormat.Title, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Controls in use:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("36 controls, 1 start, 1 finish, 1 mandatory crossing point", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("Controls not in use:", textpart.text);
            Assert.AreEqual(TextFormat.Header, textpart.format);

            textpart = description[index++];
            Assert.AreEqual("1 control, 1 start", textpart.text);
            Assert.AreEqual(TextFormat.NewLine, textpart.format);

            Assert.AreEqual(index, description.Length);
        }
    }
}


#endif //TEST
