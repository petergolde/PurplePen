/* Copyright (c) 2006-2007, Peter Golde
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
    public class AddCornerModeTests: TestFixtureBase
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

            bool success = controller.LoadInitialFile(fileName);
            Assert.IsTrue(success);
        }

        // Add a bend to a leg.
        [TestMethod]
        public void AddLegBend()
        {
            Setup("modes\\speciallegs.ppen");

            // Select course 1.
            controller.SelectTab(1);       // Course 1.
            Assert.AreEqual(-1, controller.GetHighlightedDescriptionLine());

            // Click on leg to select it.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(new PointF(18.4F, 30.1F), 0.3F);

            // Begin the add bend mode.
            controller.BeginAddLegBend();

            // Should have crosshair cursor
            ui.MouseMoved(12.2F, 14.4F, 0.3F);
            Cursor cursor = controller.GetMouseCursor(new PointF(12.2F, 14.4F), 0.3F);
            Assert.AreSame(Cursors.Cross, cursor);

            // And the adding bend text.
            Assert.AreEqual(StatusBarText.AddingBend, controller.StatusText);


            // Check the highlights
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"Leg:            control:3  course-control:3  scale:1  course-control2:4  path:N(9.1,10.27)--N(12,20)--N(23.65,38.6)",
                                        highlights[0].ToString());

            // Click to add a bend.
            dragAction = controller.LeftButtonDown(new PointF(12.2F, 14.4F), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.None, dragAction);


            // Check the status text
            Assert.AreEqual(StatusBarText.DragCorner, controller.StatusText);
            // Check the cursor
            cursor = controller.GetMouseCursor(new PointF(12.2F, 14.4F), 0.3F);
            Assert.AreSame(Util.MoveHandleCursor, cursor);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual("Leg:            control:3  course-control:3  scale:1  course-control2:4  path:N(9.7,10.02)--N(12.2,14.4)--N(12,20)--N(23.65,38.6)",
                                        highlights[0].ToString());

            // Make sure the leg has a new bend.
            Leg leg = eventDB.GetLeg(QueryEvent.FindLeg(eventDB, ControlId(3), ControlId(4)));
            Assert.AreEqual(2, leg.bends.Length);
            Assert.AreEqual(new PointF(12.2F, 14.4F), leg.bends[0]);
            Assert.AreEqual(new PointF(12, 20), leg.bends[1]);
            Assert.AreEqual(new PointF(12,20), leg.flagStartStop);
        }

        // Add a corner to an area object.
        [TestMethod]
        public void AddCornerArea()
        {
            Setup("modes\\marymoor2.coursescribe");

            // Select course 3.
            controller.SelectTab(3);       // Course 3.
            Assert.AreEqual(-1, controller.GetHighlightedDescriptionLine());

            // Click on area objects.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(new PointF(1,-2), 0.3F);

            // Begin the add corner mode.
            controller.BeginAddSpecialCorner();

            // Should have crosshair cursor
            ui.MouseMoved(-4,7, 0.3F);
            Cursor cursor = controller.GetMouseCursor(new PointF(-4,7), 0.3F);
            Assert.AreSame(Cursors.Cross, cursor);

            // And the adding corner text.
            Assert.AreEqual(StatusBarText.AddingCorner, controller.StatusText);


            // Check the highlights
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"OOB:            special:4  scale:1  path:N(3,7)--N(11,2)--N(0,-7)--N(-12,-3)--N(3,7)",
                                        highlights[0].ToString());

            // Click to add a corner.
            dragAction = controller.LeftButtonDown(new PointF(-4,7), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.None, dragAction);


            // Check the status text
            Assert.AreEqual(StatusBarText.DragCorner, controller.StatusText);
            // Check the cursor
            cursor = controller.GetMouseCursor(new PointF(-4,7), 0.3F);
            Assert.AreSame(Util.MoveHandleCursor, cursor);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual("OOB:            special:4  scale:1  path:N(3,7)--N(11,2)--N(0,-7)--N(-12,-3)--N(-4,7)--N(3,7)",
                                        highlights[0].ToString());

            // Make sure the special has a new corner.
            Special special = eventDB.GetSpecial(SpecialId(4));
            Assert.AreEqual(5, special.locations.Length);
            Assert.AreEqual(new PointF(3,7), special.locations[0]);
            Assert.AreEqual(new PointF(11,2), special.locations[1]);
            Assert.AreEqual(new PointF(0,-7), special.locations[2]);
            Assert.AreEqual(new PointF(-12,-3), special.locations[3]);
            Assert.AreEqual(new PointF(-4,7), special.locations[4]);
        }

        // Remove a corner from an area object.
        [TestMethod]
        public void RemoveCornerArea()
        {
            Setup("modes\\marymoor2.coursescribe");

            // Select course 3.
            controller.SelectTab(3);       // Course 3.
            Assert.AreEqual(-1, controller.GetHighlightedDescriptionLine());

            // Click on area objects.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(new PointF(1, -2), 0.3F);

            // Begin the remove corner mode.
            controller.BeginRemoveBend();

            // Should have delete corner cursor
            ui.MouseMoved(3.1F, 7.2F, 0.3F);
            Cursor cursor = controller.GetMouseCursor(new PointF(3.1F, 7.2F), 0.3F);
            Assert.AreSame(Util.DeleteHandleCursor, cursor);

            // And the deleting corner text.
            Assert.AreEqual(StatusBarText.DeletingCorner, controller.StatusText);

            // Check the highlights
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"OOB:            special:4  scale:1  path:N(3,7)--N(11,2)--N(0,-7)--N(-12,-3)--N(3,7)",
                                        highlights[0].ToString());

            // Click to delete a corner.
            dragAction = controller.LeftButtonDown(new PointF(3.1F, 7.2F), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.None, dragAction);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual("OOB:            special:4  scale:1  path:N(11,2)--N(0,-7)--N(-12,-3)--N(11,2)",
                                        highlights[0].ToString());

            // Make sure the special has a corner removed.
            Special special = eventDB.GetSpecial(SpecialId(4));
            Assert.AreEqual(3, special.locations.Length);
            Assert.AreEqual(new PointF(11, 2), special.locations[0]);
            Assert.AreEqual(new PointF(0, -7), special.locations[1]);
            Assert.AreEqual(new PointF(-12, -3), special.locations[2]);
        }

        // Remove a bend from a leg.
        [TestMethod]
        public void RemoveLegBend()
        {
            Setup("modes\\speciallegs.ppen");

            // Select course 1.
            controller.SelectTab(1);       // Course 1.
            Assert.AreEqual(-1, controller.GetHighlightedDescriptionLine());

            // Click on leg to select it.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(new PointF(18.4F, 30.1F), 0.3F);

            // Begin the remove bend mode.
            controller.BeginRemoveBend();

            // Should have arrow cursor
            ui.MouseMoved(12.2F, 14.4F, 0.3F);
            Cursor cursor = controller.GetMouseCursor(new PointF(12.2F, 14.4F), 0.3F);
            Assert.AreSame(Cursors.Arrow, cursor);

            // And the adding bend text.
            Assert.AreEqual(StatusBarText.DeletingBend, controller.StatusText);

            // Check the highlights
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"Leg:            control:3  course-control:3  scale:1  course-control2:4  path:N(9.1,10.27)--N(12,20)--N(23.65,38.6)",
                                        highlights[0].ToString());

            // move over an existing bend
            ui.MouseMoved(12.1F, 19.8F, 0.3F);
            cursor = controller.GetMouseCursor(new PointF(12.1F, 19.8F), 0.3F);
            Assert.AreSame(Util.DeleteHandleCursor, cursor);

            // Click to remove the bend.
            dragAction = controller.LeftButtonDown(new PointF(12.1F, 19.8F), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.None, dragAction);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual("Leg:            control:3  course-control:3  scale:1  course-control2:4  path:N(9.57,10.08)--N(23.88,38.48)",
                                        highlights[0].ToString());

            // Make sure the leg has the bend removed
            Leg leg = eventDB.GetLeg(QueryEvent.FindLeg(eventDB, ControlId(3), ControlId(4)));
            Assert.IsNull(leg.bends);
            Assert.AreEqual(FlaggingKind.All, leg.flagging);
        }

    }
}

#endif //TEST
