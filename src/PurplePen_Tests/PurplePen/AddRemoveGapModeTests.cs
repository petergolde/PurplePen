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
using System.Drawing;
using System.Windows.Forms;

using PurplePen.MapView;

using TestingUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace PurplePen.Tests
{
    [TestClass]
    public class AddGapModeTests: TestFixtureBase
    {
        TestUI ui;
        Controller controller;
        EventDB eventDB;

        public void Setup(string filename)
        {
            ui = TestUI.Create();
            controller = ui.controller;
            eventDB = controller.GetEventDB();

            string fileName = TestUtil.GetTestFile(filename);

            bool success = controller.LoadInitialFile(fileName, true);
            Assert.IsTrue(success);
        }

        // Add a gap to a control via single click
        [TestMethod]
        public void AddControlGap1()
        {
            Setup("modes\\speciallegs.ppen");

            // Select course 1.
            controller.SelectTab(1);       // Course 1.
            CheckHighlightedLines(controller, -1, -1);

            // Click on control to select it.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(Pane.Map, new PointF(27F, 41F), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.DelayedDrag, dragAction);
            controller.LeftButtonClick(Pane.Map, new PointF(27F, 41F), 0.3F);

            // Begin the add bend mode.
            controller.BeginAddGap();

            // Should have crosshair cursor
            ui.MouseMoved(26.1F, 31.5F, 0.3F);
            Cursor cursor = controller.GetMouseCursor(Pane.Map, new PointF(26.1F, 31.5F), 0.3F);
            Assert.AreSame(Cursors.Cross, cursor);

            // And the adding bend text.
            Assert.AreEqual(StatusBarText.AddingControlGap, controller.StatusText);

            // Check the highlights
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights(Pane.Map);
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"Control:        control:4  course-control:4  scale:1  location:(25.15,41)  gaps:",
                                        highlights[0].ToString());

            // Click to add a gap.
            dragAction = controller.LeftButtonDown(Pane.Map, new PointF(26.1F, 31.5F), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.DelayedDrag, dragAction);
            controller.LeftButtonClick(Pane.Map, new PointF(26.1F, 31.5F), 0.3F);

            // Check the status text
            Assert.AreEqual(StatusBarText.DefaultStatus, controller.StatusText);
            // Check the cursor
            cursor = controller.GetMouseCursor(Pane.Map, new PointF(26.1F, 31.5F), 0.3F);
            Assert.AreSame(Cursors.Default, cursor);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights(Pane.Map);
            Assert.AreEqual(2, highlights.Length);
            Assert.AreEqual(@"Control:        control:4  course-control:4  scale:1  location:(25.15,41)  gaps:260.71:290.71",
                                        highlights[0].ToString());
            Assert.AreEqual(@"ControlNumber:  control:4  course-control:4  scale:1  text:3  top-left:(21.31,51.06)
                font-name:Roboto  font-style:Regular  font-height:5.57",
                                        highlights[1].ToString());

            // Make sure the control has a new gap.
            ControlPoint control = eventDB.GetControl(ControlId(4));
            CollectionAssert.AreEqual(new CircleGap[] {new CircleGap(260.707031F, 290.707031F)}, control.gaps[10000]);
        }

        // Add a gap to a control via drag
        [TestMethod]
        public void AddControlGap2()
        {
            Setup("modes\\speciallegs.ppen");

            // Select course 1.
            controller.SelectTab(1);       // Course 1.
            CheckHighlightedLines(controller, -1, -1);

            // Click on control to select it.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(Pane.Map, new PointF(27F, 41F), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.DelayedDrag, dragAction);
            controller.LeftButtonClick(Pane.Map, new PointF(27F, 41F), 0.3F);

            // Begin the add bend mode.
            controller.BeginAddGap();

            // Should have crosshair cursor
            ui.MouseMoved(26.1F, 31.5F, 0.3F);
            Cursor cursor = controller.GetMouseCursor(Pane.Map, new PointF(26.1F, 31.5F), 0.3F);
            Assert.AreSame(Cursors.Cross, cursor);

            // And the adding gap text
            Assert.AreEqual(StatusBarText.AddingControlGap, controller.StatusText);

            // Check the highlights
            CourseObj[] highlights = (CourseObj[])controller.GetHighlights(Pane.Map);
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"Control:        control:4  course-control:4  scale:1  location:(25.15,41)  gaps:",
                                        highlights[0].ToString());

            // Click to add a gap.
            dragAction = controller.LeftButtonDown(Pane.Map, new PointF(35.0F, 31.5F), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.DelayedDrag, dragAction);
            controller.LeftButtonEndDrag(Pane.Map, new PointF(12, -1), new PointF(35.0F, 31.5F), 0.3F);

            // Check the status text
            Assert.AreEqual(StatusBarText.DefaultStatus, controller.StatusText);
            // Check the cursor
            cursor = controller.GetMouseCursor(Pane.Map, new PointF(26.1F, 31.5F), 0.3F);
            Assert.AreSame(Cursors.Default, cursor);

            // Check the highlights
            highlights = (CourseObj[])controller.GetHighlights(Pane.Map);
            Assert.AreEqual(2, highlights.Length);
            Assert.AreEqual(@"Control:        control:4  course-control:4  scale:1  location:(25.15,41)  gaps:252.61:316.04",
                                        highlights[0].ToString());
            Assert.AreEqual(@"ControlNumber:  control:4  course-control:4  scale:1  text:3  top-left:(21.31,51.06)
                font-name:Roboto  font-style:Regular  font-height:5.57",
                                        highlights[1].ToString());

            // Make sure the control has a new gap.
            ControlPoint control = eventDB.GetControl(ControlId(4));
            CollectionAssert.AreEqual(new CircleGap[] { new CircleGap(252.6131F, 316.040161F) }, control.gaps[10000]);
        }

        // Remove a gap from a control.
        [TestMethod]
        public void RemoveControlGap()
        {
            Setup("modes\\speciallegs.ppen");

            // Select course 1.
            controller.SelectTab(1);       // Course 1.
            CheckHighlightedLines(controller, -1, -1);

            // Click on control 1 to select it.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(Pane.Map, new PointF(38.6F, -21F), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.DelayedDrag, dragAction);
            controller.LeftButtonClick(Pane.Map, new PointF(38.6F, -21F), 0.3F);

            // Begin the remove gap mode.
            controller.BeginRemoveGap();

            // Should have crosshair cursor
            ui.MouseMoved(39.0F, -29F, 0.3F);
            Cursor cursor = controller.GetMouseCursor(Pane.Map, new PointF(39.0F, -29F), 0.3F);
            Assert.AreSame(Cursors.Cross, cursor);

            // And the adding bend text.
            Assert.AreEqual(StatusBarText.RemovingControlGap, controller.StatusText);

            // Check the highlights
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights(Pane.Map);
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"Control:        control:2  course-control:2  scale:1  location:(37.72,-22.42)  gaps:112.5:146.25,258.75:303.75",
                                        highlights[0].ToString());

            // Click to add a gap.
            dragAction = controller.LeftButtonDown(Pane.Map, new PointF(42.0F, -32F), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.SuppressClick, dragAction);

            // Check the status text
            ui.MouseMoved(42.0F, -32F, 0.1F);
            Assert.AreEqual(StatusBarText.DefaultStatus, controller.StatusText);
            // Check the cursor
            cursor = controller.GetMouseCursor(Pane.Map, new PointF(42.0F, -32F), 0.3F);
            Assert.AreSame(Cursors.Default, cursor);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights(Pane.Map);
            Assert.AreEqual(2, highlights.Length);
            Assert.AreEqual(@"Control:        control:2  course-control:2  scale:1  location:(37.72,-22.42)  gaps:112.5:146.25",
                                        highlights[0].ToString());
            Assert.AreEqual(@"ControlNumber:  control:2  course-control:2  scale:1  text:1  top-left:(35.72,-25.85)
                font-name:Roboto  font-style:Regular  font-height:5.57",
                                        highlights[1].ToString());

            // Make sure the control has a new gap.
            ControlPoint control = eventDB.GetControl(ControlId(2));
            CollectionAssert.AreEqual(CircleGap.ComputeCircleGaps(0xFFFFE3FF), control.gaps[10000]);
        }

        // Remove a gap from a leg.
        [TestMethod]
        public void RemoveLegGap()
        {
            Setup("modes\\gappedlegs.coursescribe");

            // Select course 1.
            controller.SelectTab(1);       // Course 1.
            CheckHighlightedLines(controller, -1, -1);

            // Click on leg to select it.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(Pane.Map, new PointF(71, 0), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.DelayedDrag, dragAction);
            controller.LeftButtonClick(Pane.Map, new PointF(71, 0), 0.3F);

            // Begin the remove gap mode.
            controller.BeginRemoveGap();

            // Should have crosshair cursor
            ui.MouseMoved(70, -13, 0.3F);
            Cursor cursor = controller.GetMouseCursor(Pane.Map, new PointF(70, -13), 0.3F);
            Assert.AreSame(Cursors.Cross, cursor);

            // And the adding bend text.
            Assert.AreEqual(StatusBarText.RemovingLegGap, controller.StatusText);

            // Check the highlights
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights(Pane.Map);
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"Leg:            control:1  course-control:1  scale:1  course-control2:2  path:N(73.43,10.02)--N(68,-22)--N(40.52,-22.36)  gaps: (s:2.96,l:3.5) (s:20.96,l:9)",
                                        highlights[0].ToString());

            // Click to remove the gap.
            dragAction = controller.LeftButtonDown(Pane.Map, new PointF(70, -13), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.SuppressClick, dragAction);

            // Check the status text
            ui.MouseMoved(70, -13, 0.1F);
            Assert.AreEqual(StatusBarText.DefaultStatus, controller.StatusText);
            // Check the cursor
            cursor = controller.GetMouseCursor(Pane.Map, new PointF(42.0F, -32F), 0.3F);
            Assert.AreSame(Cursors.Default, cursor);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights(Pane.Map);
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"Leg:            control:1  course-control:1  scale:1  course-control2:2  path:N(73.43,10.02)--N(68,-22)--N(40.52,-22.36)  gaps: (s:2.96,l:3.5)",
                                        highlights[0].ToString());

            // Make sure the leg gap is removed.
            Leg leg = eventDB.GetLeg(QueryEvent.FindLeg(eventDB, ControlId(1), ControlId(2)));
            Assert.AreEqual(1, leg.gaps.Length);
            Assert.AreEqual(7, leg.gaps[0].distanceFromStart, 0.01F);
            Assert.AreEqual(3.5F, leg.gaps[0].length, 0.01F);
        }

        [TestMethod]
        public void AddLegGap1()
        {
            Setup("modes\\gappedlegs.coursescribe");

            // Select course 1.
            controller.SelectTab(1);       // Course 1.
            CheckHighlightedLines(controller, -1, -1);

            // Click on leg to select it.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(Pane.Map, new PointF(20, -5), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.DelayedDrag, dragAction);
            controller.LeftButtonClick(Pane.Map, new PointF(20, -5), 0.3F);

            // Begin the add gap mode.
            controller.BeginAddGap();

            // Should have crosshair cursor
            ui.MouseMoved(32, -5, 0.3F);
            Cursor cursor = controller.GetMouseCursor(Pane.Map, new PointF(32, -5), 0.3F);
            Assert.AreSame(Cursors.Cross, cursor);

            // And the adding leg gap text.
            Assert.AreEqual(StatusBarText.AddingLegGap, controller.StatusText);

            // Check the highlights
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights(Pane.Map);
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"Leg:            control:2  course-control:2  scale:1  course-control2:3  path:N(35.72,-20.39)--N(10.18,5.49)",
                                        highlights[0].ToString());

            // Click and drag add a gap.
            dragAction = controller.LeftButtonDown(Pane.Map, new PointF(30, -11), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.DelayedDrag, dragAction);
            controller.LeftButtonEndDrag(Pane.Map, new PointF(12, -1), new PointF(30, -11), 0.3F);

            // Check the status text
            ui.MouseMoved(12, -1, 0.1F);
            Assert.AreEqual(StatusBarText.DefaultStatus, controller.StatusText);
            // Check the cursor
            cursor = controller.GetMouseCursor(Pane.Map, new PointF(12, -1), 0.3F);
            Assert.AreSame(Cursors.Default, cursor);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights(Pane.Map);
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"Leg:            control:2  course-control:2  scale:1  course-control2:3  path:N(35.72,-20.39)--N(10.18,5.49)  gaps: (s:10.7,l:19.76)",
                                        highlights[0].ToString());

            // Make sure the leg gap is added.
            Leg leg = eventDB.GetLeg(QueryEvent.FindLeg(eventDB, ControlId(2), ControlId(3)));
            Assert.AreEqual(1, leg.gaps.Length);
            Assert.AreEqual(13.52F, leg.gaps[0].distanceFromStart, 0.01F);
            Assert.AreEqual(19.76F, leg.gaps[0].length, 0.01F);
        }

        // Add a gap to a leg that has two gaps -- forming 1 gap.
        [TestMethod]
        public void AddLegGap2()
        {
            Setup("modes\\gappedlegs.coursescribe");

            // Select course 1.
            controller.SelectTab(1);       // Course 1.
            CheckHighlightedLines(controller, -1, -1);

            // Click on leg to select it.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(Pane.Map, new PointF(71, 0), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.DelayedDrag, dragAction);
            controller.LeftButtonClick(Pane.Map, new PointF(71, 0), 0.3F);

            // Begin the add gap mode.
            controller.BeginAddGap();

            // Should have crosshair cursor
            ui.MouseMoved(72, 10, 0.3F);
            Cursor cursor = controller.GetMouseCursor(Pane.Map, new PointF(72, 10), 0.3F);
            Assert.AreSame(Cursors.Cross, cursor);

            // And the adding leg gap text.
            Assert.AreEqual(StatusBarText.AddingLegGap, controller.StatusText);

            // Check the highlights
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights(Pane.Map);
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"Leg:            control:1  course-control:1  scale:1  course-control2:2  path:N(73.43,10.02)--N(68,-22)--N(40.52,-22.36)  gaps: (s:2.96,l:3.5) (s:20.96,l:9)",
                                        highlights[0].ToString());

            // Click and drag add a gap.
            dragAction = controller.LeftButtonDown(Pane.Map, new PointF(72, 10), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.DelayedDrag, dragAction);
            controller.LeftButtonEndDrag(Pane.Map, new PointF(50, -25), new PointF(72, 10), 0.3F);

            // Check the status text
            ui.MouseMoved(12, -1, 0.1F);
            Assert.AreEqual(StatusBarText.DefaultStatus, controller.StatusText);
            // Check the cursor
            cursor = controller.GetMouseCursor(Pane.Map, new PointF(12, -1), 0.3F);
            Assert.AreSame(Cursors.Default, cursor);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights(Pane.Map);
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"Leg:            control:1  course-control:1  scale:1  course-control2:2  path:N(73.43,10.02)--N(68,-22)--N(40.52,-22.36)  gaps: (s:0.25,l:50.26)",
                                        highlights[0].ToString());

            // Make sure the leg gap is added.
            Leg leg = eventDB.GetLeg(QueryEvent.FindLeg(eventDB, ControlId(1), ControlId(2)));
            Assert.AreEqual(1, leg.gaps.Length);
            Assert.AreEqual(4.29F, leg.gaps[0].distanceFromStart, 0.01F);
            Assert.AreEqual(50.26F, leg.gaps[0].length, 0.01F);
        }

        // Create gap by single click (2mm gap).
        [TestMethod]
        public void AddLegGap3()
        {
            Setup("modes\\gappedlegs.coursescribe");

            // Select course 1.
            controller.SelectTab(1);       // Course 1.
            CheckHighlightedLines(controller, -1, -1);

            // Click on leg to select it.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(Pane.Map, new PointF(20, -5), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.DelayedDrag, dragAction);
            controller.LeftButtonClick(Pane.Map, new PointF(20, -5), 0.3F);

            // Begin the add gap mode.
            controller.BeginAddGap();

            // Should have crosshair cursor
            ui.MouseMoved(32, -5, 0.3F);
            Cursor cursor = controller.GetMouseCursor(Pane.Map, new PointF(32, -5), 0.3F);
            Assert.AreSame(Cursors.Cross, cursor);

            // And the adding leg gap text.
            Assert.AreEqual(StatusBarText.AddingLegGap, controller.StatusText);

            // Check the highlights
            CourseObj[] highlights = (CourseObj[])controller.GetHighlights(Pane.Map);
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"Leg:            control:2  course-control:2  scale:1  course-control2:3  path:N(35.72,-20.39)--N(10.18,5.49)",
                                        highlights[0].ToString());

            // Click to add a gap.
            dragAction = controller.LeftButtonDown(Pane.Map, new PointF(30, -11), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.DelayedDrag, dragAction);
            controller.LeftButtonClick(Pane.Map, new PointF(30, -11), 0.3F);

            // Check the status text
            ui.MouseMoved(12, -1, 0.1F);
            Assert.AreEqual(StatusBarText.DefaultStatus, controller.StatusText);
            // Check the cursor
            cursor = controller.GetMouseCursor(Pane.Map, new PointF(12, -1), 0.3F);
            Assert.AreSame(Cursors.Default, cursor);

            // Check the highlights
            highlights = (CourseObj[])controller.GetHighlights(Pane.Map);
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"Leg:            control:2  course-control:2  scale:1  course-control2:3  path:N(35.72,-20.39)--N(10.18,5.49)  gaps: (s:9.7,l:2)",
                                        highlights[0].ToString());

            // Make sure the leg gap is added.
            Leg leg = eventDB.GetLeg(QueryEvent.FindLeg(eventDB, ControlId(2), ControlId(3)));
            Assert.AreEqual(1, leg.gaps.Length);
            Assert.AreEqual(12.52F, leg.gaps[0].distanceFromStart, 0.01F);
            Assert.AreEqual(2.0F, leg.gaps[0].length, 0.01F);
        }

    }
}

#endif //TEST
