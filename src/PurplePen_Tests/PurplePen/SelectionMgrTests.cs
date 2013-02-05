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
using System.Diagnostics;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.Tests
{
    [TestClass]
    public class SelectionMgrTests: TestFixtureBase
    {
        TestUI ui;
        SelectionMgr selectionMgr;
        Controller controller;

        [TestInitialize]
        public void Setup()
        {
            ui = TestUI.Create();
            controller = ui.controller;
            selectionMgr = controller.GetSelectionMgr();
        }


        [TestMethod]
        public void TabList()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            Assert.AreEqual(7, selectionMgr.TabCount);

            string[] expected = { "All controls", "Green Y", "Rambo", "SampleCourse4", "Score 4", "White", "Yellow" };

            for (int i = 0; i < expected.Length; ++i) {
                Assert.AreEqual(expected[i], selectionMgr.TabName(i));
            }
        }

        [TestMethod]
        public void EmptyTabList()
        {
            Assert.AreEqual(1, selectionMgr.TabCount);
            Assert.AreEqual(0, selectionMgr.ActiveTab);

            string[] expected = { "All controls" };

            for (int i = 0; i < expected.Length; ++i) {
                Assert.AreEqual(expected[i], selectionMgr.TabName(i));
            }
        }

        [TestMethod]
        public void AddRemoveTabs()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            Assert.AreEqual(7, selectionMgr.TabCount);
            Assert.AreEqual(0, selectionMgr.ActiveTab);

            string[] expected = { "All controls", "Green Y", "Rambo", "SampleCourse4", "Score 4", "White", "Yellow" };

            for (int i = 0; i < expected.Length; ++i) {
                Assert.AreEqual(expected[i], selectionMgr.TabName(i));
            }


            UndoMgr undoMgr = controller.GetUndoMgr();
            EventDB eventDB = controller.GetEventDB();

            undoMgr.BeginCommand(197, "Add course");
            eventDB.AddCourse(new Course(CourseKind.Normal, "AAA", 15000, 10));
            undoMgr.EndCommand(197);
            undoMgr.BeginCommand(198, "Remove courses");
            eventDB.RemoveCourse(CourseId(1));
            eventDB.RemoveCourse(CourseId(4));
            undoMgr.EndCommand(198);

            expected = new string[] { "All controls", "Green Y", "Rambo", "Score 4", "Yellow", "AAA"};

            Assert.AreEqual(6, selectionMgr.TabCount);
            Assert.AreEqual(0, selectionMgr.ActiveTab);
            for (int i = 0; i < expected.Length; ++i) {
                Assert.AreEqual(expected[i], selectionMgr.TabName(i));
            }

        }

        [TestMethod]
        public void ActiveTab()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            Assert.AreEqual(7, selectionMgr.TabCount);
            Assert.AreEqual(0, selectionMgr.ActiveTab);

            selectionMgr.ActiveTab = 5;
            Assert.AreEqual(5, selectionMgr.ActiveTab);

            UndoMgr undoMgr = controller.GetUndoMgr();
            EventDB eventDB = controller.GetEventDB();

            undoMgr.BeginCommand(197, "Add course");
            eventDB.AddCourse(new Course(CourseKind.Normal, "AAA", 15000, 1));
            undoMgr.EndCommand(197);

            Assert.AreEqual(6, selectionMgr.ActiveTab);

            undoMgr.BeginCommand(198, "Remove courses");
            eventDB.RemoveCourse(CourseId(1));
            undoMgr.EndCommand(198);

            Assert.AreEqual(0, selectionMgr.ActiveTab);
        }

        [TestMethod]
        public void ActiveDescription()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.ActiveTab = 3;

            Assert.AreEqual("SampleCourse4", selectionMgr.ActiveCourseView.CourseName);

            StringWriter writer = new StringWriter();
            string actual, expected;

            DescriptionFormatter.DumpDescription(ui.symbolDB, selectionMgr.ActiveDescription, writer);
            actual = writer.ToString();
            expected =
@"      | Sample Event 1                                |   [Sample Event 1]
      |SampleCourse4    |4.7 km           |175 m      |   [Length 4.7 km, climb 175 m]
(  1) |start|     |     |  2.8|  8.5|     |     |     |   [Start: open bare rock]
( 11) |    1|  191|     |  5.2|  5.2| 10.1|11.15|     |   [Between path crossings]
( 22) |                 13.4:                         |   [Mandatory passage]
(  4) |    2|   32|     |  3.7|     |     |     | 12.1|   [very marshy spot]
( 15) |                 13.3:                         |   [Mandatory crossing point]
(  5) |    3|   GO| 0.1N|  5.5|  5.2| 10.1|11.1N| 12.3|   [N side of N power line and path crossing (radio)]
( 18) |    4|  303|     |  1.3|     |    4|     |     |   [Reentrant, 4m deep]
(  6) |                 14.2: 1420 m                  |   [Navigate 1420 m to finish funnel]
";
            Assert.AreEqual(expected, actual);

            selectionMgr.ActiveTab = 0;

            Assert.AreEqual("All controls", selectionMgr.ActiveCourseView.CourseName);

            writer = new StringWriter();
            DescriptionFormatter.DumpDescription(ui.symbolDB, selectionMgr.ActiveDescription, writer);
            actual = writer.ToString();
            expected =
@"      | Sample Event 1                                |   [Sample Event 1]
      |All controls     |17 controls                  |   [17 controls]
(  1) |start|     |     |  2.8|  8.5|     |     |     |   [Start: open bare rock]
( 23) |start|     |0.2NW|  1.7|     |  2.5|     |     |   [Start: NW gully, 2.5m deep]
(  2) |     |   31|  0.3|  2.4|     |   2m|     |     |   [Upper boulder, 2m high]
(  4) |     |   32|     |  3.7|     |     |     | 12.1|   [very marshy spot]
( 12) |     |   74|     |  2.4|     |0.5/2.5|11.15|     |   [Between boulders, 0.5m to 2.5m high]
(  7) |     |  189|     |  1.8|  1.8| 10.2|     |     |   [Small gully junction]
( 10) |     |  190|     |  5.1|  5.5| 10.1|11.1N|     |   [N side of road and power line crossing]
( 11) |     |  191|     |  5.2|  5.2| 10.1|11.15|     |   [Between path crossings]
(  8) |     |  210|     | 5.11| 5.20|     |11.15|     |   [Between building and statue]
(  9) |     |  211|     |  3.7|  3.7|4x4|5x6|11.15|     |   [Between marshes, 4m by 4m and 5m by 6m]
( 13) |     |  290| 0.1N|  5.2|     | 10.2|     |     |   [N path junction]
( 14) |     |  291|     | 1.10|  2.4|     |     |     |   [Knoll and boulder]
( 16) |     |  301|     | 1.14|  8.4|  3.0|     |     |   [Overgrown pit, 3m deep]
( 17) |     |  302|     | 1.14|     |     |11.1S|     |   [S side of pit]
( 18) |     |  303|     |  1.3|     |    4|     |     |   [Reentrant, 4m deep]
( 19) |     |  304| 0.1S|     |  8.5|     |     |     |   [S ]
( 20) |     |  305|     |  2.7|     |  6x7|     | 12.4|   [Stony ground, 6m by 7m (manned)]
( 21) |     |  306|     |     |     |     |     |     |   []
(  5) |     |   GO| 0.1N|  5.5|  5.2| 10.1|11.1N| 12.3|   [N side of N power line and path crossing (radio)]
(  6) |                 14.2:                         |   [Navigate to finish funnel]
( 24) |                 14.1:                         |   [Follow tapes to finish]
(  3) |                 13.3:                         |   [Mandatory crossing point]
( 15) |                 13.3:                         |   [Mandatory crossing point]
( 22) |                 13.4:                         |   [Mandatory passage]
";
            Assert.AreEqual(expected, actual);

            writer = new StringWriter();
        }

        void RoundtripSelectedLines(string testFileName, bool singleLineOnly)
        {

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile(testFileName), true);
            Assert.IsTrue(success);

            // Make sure every line in every tab can be selected.
            for (int tab = 0; tab < selectionMgr.TabCount; ++tab) {
                selectionMgr.ActiveTab = tab;
                for (int line = -1; line < selectionMgr.ActiveDescription.Length; ++line) {
                    selectionMgr.SelectDescriptionLine(line); 
                    Assert.AreEqual(tab, selectionMgr.ActiveTab);
                    int firstLine, lastLine;
                    selectionMgr.GetSelectedLines(out firstLine, out lastLine);
                    if (singleLineOnly) {
                        Assert.AreEqual(line, firstLine);
                        Assert.AreEqual(line, lastLine);
                    }
                    else {
                        Assert.IsTrue(line >= firstLine);
                        Assert.IsTrue(line <= lastLine);
                    }
                }
            }
        }

        [TestMethod]
        public void RoundtripSelectedLine()
        {
            RoundtripSelectedLines("selectionmgr\\sampleevent1.coursescribe", true);
        }

        [TestMethod]
        public void RoundtripSelectedLine2()
        {
            RoundtripSelectedLines("selectionmgr\\speciallegs.coursescribe", true);
        }

        [TestMethod]
        public void RoundtripSelectedLine3()
        {
            RoundtripSelectedLines("selectionmgr\\sampleevent5.ppen", true);
        }

        [TestMethod]
        public void RoundtripSelectedLine4()
        {
            RoundtripSelectedLines("selectionmgr\\desctext.ppen", true);
        }

        [TestMethod]
        public void RoundtripSelectedLine5()
        {
            RoundtripSelectedLines("selectionmgr\\sampleevent6.coursescribe", false);
        }

        [TestMethod]
        public void RoundtripSelectedLine6()
        {
            RoundtripSelectedLines("selectionmgr\\mapexchange2.ppen", false);
        }

        void CheckSelectedLines(int expectedFirstLine, int expectedLastLine)
        {
            int firstLine, lastLine;
            selectionMgr.GetSelectedLines(out firstLine, out lastLine);
            Assert.AreEqual(expectedFirstLine, firstLine);
            Assert.AreEqual(expectedLastLine, lastLine);
        }

        [TestMethod]
        public void ClearSelection()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.ActiveTab = 0;
            selectionMgr.SelectDescriptionLine(4);
            CheckSelectedLines(4, 4);

            selectionMgr.ClearSelection();
            CheckSelectedLines(-1, -1);

            selectionMgr.ActiveTab = 3;
            selectionMgr.SelectDescriptionLine(5);
            CheckSelectedLines(5, 5);

            selectionMgr.ClearSelection();
            CheckSelectedLines(-1, -1);
        }

        [TestMethod]
        public void SelectCourseView()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(3));
            Assert.AreEqual(2, selectionMgr.ActiveTab);
            selectionMgr.SelectCourseView(Designator(0));
            Assert.AreEqual(0, selectionMgr.ActiveTab);
        }

        [TestMethod]
        public void SetSelection()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(4));

            selectionMgr.SelectHeader();
            CheckSelectedLines(1,1);

            selectionMgr.SelectTitle();
            CheckSelectedLines(0, 0);

            selectionMgr.SelectCourseControl(CourseControlId(11));
            CheckSelectedLines(2, 2);

            selectionMgr.SelectCourseControl(CourseControlId(12));
            CheckSelectedLines(3, 3);

            selectionMgr.SelectCourseControl(CourseControlId(13));
            CheckSelectedLines(4, 4);

            selectionMgr.SelectCourseControl(CourseControlId(16));
            CheckSelectedLines(6, 6);

            selectionMgr.SelectControl(ControlId(6));
            CheckSelectedLines(9, 9);

            selectionMgr.SelectCourseView(Designator(0));
            selectionMgr.SelectControl(ControlId(8));
            CheckSelectedLines(10, 10);

            selectionMgr.SelectCourseView(Designator(5));
            selectionMgr.SelectSecondaryTitle();
            CheckSelectedLines(1, 1);
        }

        [TestMethod]
        public void SelectMultilineTitle()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\sampleevent6.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(1));
            selectionMgr.SelectDescriptionLine(1);
            CheckSelectedLines(0, 1);
            selectionMgr.SelectDescriptionLine(3);
            CheckSelectedLines(2, 4);
            selectionMgr.SelectDescriptionLine(0);
            CheckSelectedLines(0, 1);
            selectionMgr.SelectDescriptionLine(4);
            CheckSelectedLines(2, 4);
            selectionMgr.SelectDescriptionLine(1);
            CheckSelectedLines(0, 1);
            selectionMgr.SelectDescriptionLine(2);
            CheckSelectedLines(2, 4);

            selectionMgr.SelectTitle();
            CheckSelectedLines(0, 1);
            selectionMgr.SelectSecondaryTitle();
            CheckSelectedLines(2, 4);
        }

        [TestMethod]
        public void SelectLeg()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\sampleevent4.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(4));
            selectionMgr.SelectDescriptionLine(5);
            selectionMgr.SelectLeg(CourseControlId(13), CourseControlId(14));

            CheckSelectedLines(-1, -1);
            SelectionMgr.SelectionInfo selectionInfo = selectionMgr.Selection;
            Assert.AreEqual(SelectionMgr.SelectionKind.Leg, selectionInfo.SelectionKind);
            Assert.AreEqual(22, selectionInfo.SelectedControl.id);
            Assert.AreEqual(13, selectionInfo.SelectedCourseControl.id);
            Assert.AreEqual(14, selectionInfo.SelectedCourseControl2.id);
            Assert.AreEqual(0, selectionInfo.SelectedSpecial.id);

            CourseObj[] selectedObjects = selectionMgr.SelectedCourseObjects;
            Assert.AreEqual(1, selectedObjects.Length);
            Assert.AreEqual(@"Leg:            control:22  course-control:13  scale:1  course-control2:14  path:N(17.24,5.42)--N(13.76,-5.42)", selectedObjects[0].ToString());
        }

        [TestMethod]
        public void SelectLeg2()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\speciallegs.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(1));
            selectionMgr.SelectLeg(CourseControlId(2), CourseControlId(3));

            CheckSelectedLines(4, 4);
            SelectionMgr.SelectionInfo selectionInfo = selectionMgr.Selection;
            Assert.AreEqual(SelectionMgr.SelectionKind.Leg, selectionInfo.SelectionKind);
            Assert.AreEqual(2, selectionInfo.SelectedControl.id);
            Assert.AreEqual(2, selectionInfo.SelectedCourseControl.id);
            Assert.AreEqual(3, selectionInfo.SelectedCourseControl2.id);
            Assert.AreEqual(0, selectionInfo.SelectedSpecial.id);

            selectionMgr.SelectLeg(CourseControlId(1), CourseControlId(2));
            CheckSelectedLines(-1, -1);
        }

        [TestMethod]
        public void SelectLegTestObj()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\sampleevent4.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(4));
            CourseLayout course = selectionMgr.CourseLayout;
            foreach (CourseObj courseobj in course) {
                if (courseobj is LegCourseObj && courseobj.courseControlId == CourseControlId(13) &&
                    ((LegCourseObj) courseobj).courseControlId2 == CourseControlId(14)) {
                    selectionMgr.SelectCourseObject(courseobj);
                    break;
                }
            }

            SelectionMgr.SelectionInfo selectionInfo = selectionMgr.Selection;
            Assert.AreEqual(SelectionMgr.SelectionKind.Leg, selectionInfo.SelectionKind);
            Assert.AreEqual(22, selectionInfo.SelectedControl.id);
            Assert.AreEqual(13, selectionInfo.SelectedCourseControl.id);
            Assert.AreEqual(14, selectionInfo.SelectedCourseControl2.id);
            Assert.AreEqual(0, selectionInfo.SelectedSpecial.id);

            CourseObj[] selectedObjects = selectionMgr.SelectedCourseObjects;
            Assert.AreEqual(1, selectedObjects.Length);
            Assert.AreEqual(@"Leg:            control:22  course-control:13  scale:1  course-control2:14  path:N(17.24,5.42)--N(13.76,-5.42)", selectedObjects[0].ToString());
        }
	
        [TestMethod]
        public void SelectSpecial()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\sampleevent4.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(4));
            selectionMgr.SelectCourseControl(CourseControlId(11));
            selectionMgr.SelectSpecial(SpecialId(5));

            CheckSelectedLines(-1, -1);
            SelectionMgr.SelectionInfo selectionInfo = selectionMgr.Selection;
            Assert.AreEqual(SelectionMgr.SelectionKind.Special, selectionInfo.SelectionKind);
            Assert.AreEqual(0, selectionInfo.SelectedControl.id);
            Assert.AreEqual(0, selectionInfo.SelectedCourseControl.id);
            Assert.AreEqual(5, selectionInfo.SelectedSpecial.id);

            CourseObj[] selectedObjects = selectionMgr.SelectedCourseObjects;
            Assert.AreEqual(1, selectedObjects.Length);
            Assert.AreEqual(@"BasicText:      special:5  scale:1  text:Banana Apple  top-left:(13,17)
                font-name:Times New Roman  font-style:Bold  font-height:9.530931  rect:(13,17)-(71,1)", selectedObjects[0].ToString());
        }

        [TestMethod]
        public void SelectTextLine()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\desctext.ppen"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(6));

            selectionMgr.SelectTextLine(ControlId(18), CourseControlId(208), DescriptionLine.TextLineKind.BeforeCourseControl);            
            SelectionMgr.SelectionInfo selectionInfo = selectionMgr.Selection;
            Assert.AreEqual(SelectionMgr.SelectionKind.TextLine, selectionInfo.SelectionKind);
            Assert.AreEqual(18, selectionInfo.SelectedControl.id);
            Assert.AreEqual(208, selectionInfo.SelectedCourseControl.id);
            Assert.AreEqual(DescriptionLine.TextLineKind.BeforeCourseControl, selectionInfo.SelectedTextLineKind);

            selectionMgr.SelectTextLine(ControlId(18), CourseControlId(208), DescriptionLine.TextLineKind.AfterCourseControl);
            selectionInfo = selectionMgr.Selection;
            Assert.AreEqual(SelectionMgr.SelectionKind.TextLine, selectionInfo.SelectionKind);
            Assert.AreEqual(18, selectionInfo.SelectedControl.id);
            Assert.AreEqual(208, selectionInfo.SelectedCourseControl.id);
            Assert.AreEqual(DescriptionLine.TextLineKind.AfterCourseControl, selectionInfo.SelectedTextLineKind);

            selectionMgr.SelectTextLine(ControlId(18), CourseControlId(208), DescriptionLine.TextLineKind.BeforeControl);
            selectionInfo = selectionMgr.Selection;
            Assert.AreEqual(SelectionMgr.SelectionKind.TextLine, selectionInfo.SelectionKind);
            Assert.AreEqual(18, selectionInfo.SelectedControl.id);
            Assert.AreEqual(208, selectionInfo.SelectedCourseControl.id);
            Assert.AreEqual(DescriptionLine.TextLineKind.BeforeControl, selectionInfo.SelectedTextLineKind);

            selectionMgr.SelectTextLine(ControlId(18), CourseControlId(208), DescriptionLine.TextLineKind.AfterControl);
            selectionInfo = selectionMgr.Selection;
            Assert.AreEqual(SelectionMgr.SelectionKind.TextLine, selectionInfo.SelectionKind);
            Assert.AreEqual(18, selectionInfo.SelectedControl.id);
            Assert.AreEqual(208, selectionInfo.SelectedCourseControl.id);
            Assert.AreEqual(DescriptionLine.TextLineKind.AfterControl, selectionInfo.SelectedTextLineKind);
        }

        [TestMethod]
        public void SelectKeyLine()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\sampleevent5.ppen"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(5));

            selectionMgr.SelectKeyLine(ui.symbolDB["5.6"]);            // light pole
            SelectionMgr.SelectionInfo selectionInfo = selectionMgr.Selection;
            Assert.AreEqual(SelectionMgr.SelectionKind.Key, selectionInfo.SelectionKind);
            Assert.AreEqual("5.6", selectionInfo.SelectedKeySymbol.Id);
            CheckSelectedLines(16, 16);

            selectionMgr.SelectDescriptionLine(17);
            selectionInfo = selectionMgr.Selection;
            Assert.AreEqual(SelectionMgr.SelectionKind.Key, selectionInfo.SelectionKind);
            Assert.AreEqual("12.1", selectionInfo.SelectedKeySymbol.Id);
        }
	

        [TestMethod]
        public void SelectSpecialCourseObj()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\sampleevent4.coursescribe"), true);
            Assert.IsTrue(success);

            // Select a special in all controls.
            selectionMgr.SelectCourseView(Designator(0));
            CourseLayout course = selectionMgr.CourseLayout;
            CourseObj courseobject = course[3];
            Assert.IsTrue(courseobject.specialId.id == 4);
            selectionMgr.SelectCourseObject(courseobject);

            CheckSelectedLines(-1, -1);
            SelectionMgr.SelectionInfo selectionInfo = selectionMgr.Selection;
            Assert.AreEqual(SelectionMgr.SelectionKind.Special, selectionInfo.SelectionKind);
            Assert.AreEqual(0, selectionInfo.SelectedControl.id);
            Assert.AreEqual(0, selectionInfo.SelectedCourseControl.id);
            Assert.AreEqual(4, selectionInfo.SelectedSpecial.id);

            CourseObj[] selectedObjects = selectionMgr.SelectedCourseObjects;
            Assert.AreEqual(1, selectedObjects.Length);
            Assert.AreEqual(@"OOB:            special:4  scale:1  path:N(3,7)--N(11,2)--N(0,-7)--N(-12,-3)--N(3,7)", selectedObjects[0].ToString());

            // Select a special in a course view
            selectionMgr.SelectCourseView(Designator(3));
            course = selectionMgr.CourseLayout;
            courseobject = course[2];
            Assert.IsTrue(courseobject.specialId.id == 3);
            selectionMgr.SelectCourseObject(courseobject);

            CheckSelectedLines(-1, -1);
            selectionInfo = selectionMgr.Selection;
            Assert.AreEqual(SelectionMgr.SelectionKind.Special, selectionInfo.SelectionKind);
            Assert.AreEqual(0, selectionInfo.SelectedControl.id);
            Assert.AreEqual(0, selectionInfo.SelectedCourseControl.id);
            Assert.AreEqual(3, selectionInfo.SelectedSpecial.id);

            selectedObjects = selectionMgr.SelectedCourseObjects;
            Assert.AreEqual(1, selectedObjects.Length);
            Assert.AreEqual(@"Boundary:       special:3  scale:0.6666667  path:N(11,2)--N(0,-7)--N(-12,-3)", selectedObjects[0].ToString());
        }
	

        [TestMethod]
        public void SelectedCourseObjects()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            StringWriter writer = new StringWriter();

            selectionMgr.SelectCourseView(Designator(4));
            selectionMgr.SelectCourseControl(CourseControlId(11));
            CourseObj[] selectedObjects = selectionMgr.SelectedCourseObjects;
            foreach (CourseObj courseObject in selectedObjects) 
                writer.WriteLine(courseObject);
            string dump = writer.ToString();

            string expected = @"Start:          control:1  course-control:11  scale:1  location:(5,0)  orientation:82.91
";
            Assert.AreEqual(expected, dump);

            writer = new StringWriter();
            selectionMgr.SelectCourseView(Designator(0));
            selectionMgr.SelectControl(ControlId(9));
            selectedObjects = selectionMgr.SelectedCourseObjects;
            foreach (CourseObj courseObject in selectedObjects)
                writer.WriteLine(courseObject);
            dump = writer.ToString();
            expected = @"Control:        control:9  scale:1  location:(39.9,-1.43)  gaps:
Code:           control:9  scale:1  text:211  top-left:(36.72,-3.88)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
";
            Assert.AreEqual(expected, dump);

        }

        [TestMethod]
        public void SelectCourseObjectAllControls()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\marymoor1.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(0));
            CourseLayout course = selectionMgr.CourseLayout;

            CourseObj courseobject = course[4];
            Assert.IsTrue(courseobject.controlId.id == 38);
            selectionMgr.SelectCourseObject(courseobject);
            CheckSelectedLines(6, 6);

            courseobject = course[0];
            Assert.IsTrue(courseobject.controlId.id == 1);
            selectionMgr.SelectCourseObject(courseobject);
            CheckSelectedLines(2, 2);

            courseobject = course[37];
            Assert.IsTrue(courseobject.controlId.id == 2);
            selectionMgr.SelectCourseObject(courseobject);
            CheckSelectedLines(39, 39);

            courseobject = course[41];
            Assert.IsTrue(courseobject.controlId.id == 38);
            selectionMgr.SelectCourseObject(courseobject);
            CheckSelectedLines(6, 6);
        }

        [TestMethod]
        public void SelectCourseObjectNormalCourse()
        {
            CourseObj courseobject;
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\marymoor1.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(3));
            CourseLayout course = selectionMgr.CourseLayout;

            courseobject = course[0];
            Assert.IsTrue(courseobject.controlId.id == 1);
            selectionMgr.SelectCourseObject(courseobject);
            CheckSelectedLines(2, 2);

            courseobject = course[6];
            Assert.IsTrue(courseobject.controlId.id == 41);
            selectionMgr.SelectCourseObject(courseobject);
            CheckSelectedLines(5, 5);

            courseobject = course[30];
            Assert.IsTrue(courseobject.controlId.id == 53);
            selectionMgr.SelectCourseObject(courseobject);
            CheckSelectedLines(4, 4);
        }

        [TestMethod]
        public void GetSelectionInfo()
        {
            SelectionMgr.SelectionInfo info;

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(0));
            info = selectionMgr.Selection;
            Assert.AreEqual(0, info.ActiveCourseDesignator.CourseId.id);
            Assert.AreEqual(SelectionMgr.SelectionKind.None, info.SelectionKind);

            selectionMgr.SelectControl(ControlId(5));
            info = selectionMgr.Selection;
            Assert.AreEqual(0, info.ActiveCourseDesignator.CourseId.id);
            Assert.AreEqual(SelectionMgr.SelectionKind.Control, info.SelectionKind);
            Assert.AreEqual(5, info.SelectedControl.id);
            Assert.AreEqual(0, info.SelectedCourseControl.id);

            selectionMgr.SelectCourseView(Designator(6));
            info = selectionMgr.Selection;
            Assert.AreEqual(6, info.ActiveCourseDesignator.CourseId.id);
            Assert.AreEqual(SelectionMgr.SelectionKind.None, info.SelectionKind);

            selectionMgr.SelectCourseControl(CourseControlId(204));
            info = selectionMgr.Selection;
            Assert.AreEqual(6, info.ActiveCourseDesignator.CourseId.id);
            Assert.AreEqual(SelectionMgr.SelectionKind.Control, info.SelectionKind);
            Assert.AreEqual(8, info.SelectedControl.id);
            Assert.AreEqual(204, info.SelectedCourseControl.id);

            selectionMgr.SelectTitle();
            info = selectionMgr.Selection;
            Assert.AreEqual(6, info.ActiveCourseDesignator.CourseId.id);
            Assert.AreEqual(SelectionMgr.SelectionKind.Title, info.SelectionKind);
            Assert.AreEqual(0, info.SelectedControl.id);
            Assert.AreEqual(0, info.SelectedCourseControl.id);

            selectionMgr.SelectCourseView(Designator(5));
            selectionMgr.SelectSecondaryTitle();
            info = selectionMgr.Selection;
            Assert.AreEqual(5, info.ActiveCourseDesignator.CourseId.id);
            Assert.AreEqual(SelectionMgr.SelectionKind.SecondaryTitle, info.SelectionKind);
            Assert.AreEqual(0, info.SelectedControl.id);
            Assert.AreEqual(0, info.SelectedCourseControl.id);
        }

        [TestMethod]
        public void SetAllControlsDisplay()
        {
            StringWriter writer = new StringWriter();
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\sampleevent3.coursescribe"), true);
            Assert.IsTrue(success);

            selectionMgr.SelectCourseView(Designator(1));
            CourseLayout course = selectionMgr.CourseLayout;

            course.Dump(writer);
            Assert.AreEqual(
@"
Start:          control:1  course-control:1  scale:1  location:(5,0)  orientation:333.43
Leg:            control:1  course-control:1  scale:1  course-control2:2  path:N(6.81,3.61)--N(8.74,7.47)
Control:        control:2  course-control:2  scale:1  location:(10,10)  gaps:
Leg:            control:2  course-control:2  scale:1  course-control2:3  path:N(11.36,7.52)--N(28.7,-24.19)
Finish:         control:6  course-control:3  scale:1  location:(30.3,-27.11)  gaps:
ControlNumber:  control:2  course-control:2  scale:1  text:1  top-left:(7.56,19.87)
                font-name:Arial  font-style:Regular  font-height:5.57
", writer.ToString());

            selectionMgr.SetAllControlsDisplay(true, ControlPointKind.None);

            writer = new StringWriter();
            course = selectionMgr.CourseLayout;
            course.Dump(writer);
            Assert.AreEqual(
@"
Start:          control:1  course-control:1  scale:1  location:(5,0)  orientation:333.43
Leg:            control:1  course-control:1  scale:1  course-control2:2  path:N(6.81,3.61)--N(8.74,7.47)
Control:        control:2  course-control:2  scale:1  location:(10,10)  gaps:
Leg:            control:2  course-control:2  scale:1  course-control2:3  path:N(11.36,7.52)--N(28.7,-24.19)
Finish:         control:6  course-control:3  scale:1  location:(30.3,-27.11)  gaps:
ControlNumber:  control:2  course-control:2  scale:1  text:1  top-left:(7.56,19.87)
                font-name:Arial  font-style:Regular  font-height:5.57
Start:          layer:2  control:7  scale:1  location:(0,5)  orientation:0
Control:        layer:2  control:3  scale:1  location:(20,-10.5)  gaps:56.25:67.5
Control:        layer:2  control:4  scale:1  location:(35.4,-22.5)  gaps:
Code:           layer:2  control:3  scale:1  text:32  top-left:(13.15,-10.97)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:2  control:4  scale:1  text:GO  top-left:(38.27,-16.92)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
", writer.ToString());

            selectionMgr.SetAllControlsDisplay(true, ControlPointKind.Start);

            writer = new StringWriter();
            course = selectionMgr.CourseLayout;
            course.Dump(writer);
            Assert.AreEqual(
@"
Start:          control:1  course-control:1  scale:1  location:(5,0)  orientation:333.43
Leg:            control:1  course-control:1  scale:1  course-control2:2  path:N(6.81,3.61)--N(8.74,7.47)
Control:        control:2  course-control:2  scale:1  location:(10,10)  gaps:
Leg:            control:2  course-control:2  scale:1  course-control2:3  path:N(11.36,7.52)--N(28.7,-24.19)
Finish:         control:6  course-control:3  scale:1  location:(30.3,-27.11)  gaps:
ControlNumber:  control:2  course-control:2  scale:1  text:1  top-left:(7.56,19.87)
                font-name:Arial  font-style:Regular  font-height:5.57
Start:          layer:2  control:7  scale:1  location:(0,5)  orientation:0
", writer.ToString());


            selectionMgr.SetAllControlsDisplay(false, ControlPointKind.None);

            writer = new StringWriter();
            course = selectionMgr.CourseLayout;
            course.Dump(writer);
            Assert.AreEqual(
@"
Start:          control:1  course-control:1  scale:1  location:(5,0)  orientation:333.43
Leg:            control:1  course-control:1  scale:1  course-control2:2  path:N(6.81,3.61)--N(8.74,7.47)
Control:        control:2  course-control:2  scale:1  location:(10,10)  gaps:
Leg:            control:2  course-control:2  scale:1  course-control2:3  path:N(11.36,7.52)--N(28.7,-24.19)
Finish:         control:6  course-control:3  scale:1  location:(30.3,-27.11)  gaps:
ControlNumber:  control:2  course-control:2  scale:1  text:1  top-left:(7.56,19.87)
                font-name:Arial  font-style:Regular  font-height:5.57
", writer.ToString());

        }

        [TestMethod]
        public void MapExchangeSelectionChanges()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("selectionmgr\\mapexchange2.ppen"), true);
            Assert.IsTrue(success);

            UndoMgr undoMgr = controller.GetUndoMgr();
            EventDB eventDB = controller.GetEventDB();

            selectionMgr.ActiveTab = selectionMgr.TabCount - 1;  // last tab
            selectionMgr.SelectCourseControl(CourseControlId(602));
            Assert.IsTrue(selectionMgr.Selection.SelectedCourseControl.id == 602);
            selectionMgr.SelectCourseView(new CourseDesignator(selectionMgr.Selection.ActiveCourseDesignator.CourseId, 0));
            Assert.IsTrue(selectionMgr.Selection.SelectedCourseControl.id == 602);
            selectionMgr.SelectCourseView(new CourseDesignator(selectionMgr.Selection.ActiveCourseDesignator.CourseId, 1));
            Assert.IsTrue(selectionMgr.Selection.SelectedCourseControl.IsNone);

            selectionMgr.SelectLeg(CourseControlId(611), CourseControlId(612));
            Assert.IsTrue(selectionMgr.Selection.SelectedCourseControl.id == 611);
            Assert.IsTrue(selectionMgr.Selection.SelectedCourseControl2.id == 612);
            selectionMgr.SelectCourseView(new CourseDesignator(selectionMgr.Selection.ActiveCourseDesignator.CourseId, 0));
            Assert.IsTrue(selectionMgr.Selection.SelectedCourseControl.IsNone);
            Assert.IsTrue(selectionMgr.Selection.SelectedCourseControl2.IsNone);

            selectionMgr.SelectCourseView(new CourseDesignator(selectionMgr.Selection.ActiveCourseDesignator.CourseId, 3));
            Assert.IsTrue(selectionMgr.Selection.ActiveCourseDesignator.Part == 3);
            undoMgr.BeginCommand(912, "Remove Course Control");
            ChangeEvent.RemoveCourseControl(eventDB, selectionMgr.Selection.ActiveCourseDesignator.CourseId, CourseControlId(615));
            undoMgr.EndCommand(912);
            Assert.AreEqual(2, selectionMgr.Selection.ActiveCourseDesignator.Part);

            undoMgr.BeginCommand(915, "Remove Course Controls");
            ChangeEvent.RemoveCourseControl(eventDB, selectionMgr.Selection.ActiveCourseDesignator.CourseId, CourseControlId(611));
            ChangeEvent.RemoveCourseControl(eventDB, selectionMgr.Selection.ActiveCourseDesignator.CourseId, CourseControlId(616));
            undoMgr.EndCommand(915);
            Assert.IsTrue(selectionMgr.Selection.ActiveCourseDesignator.AllParts);
        }
	
    }
}

#endif //TEST
