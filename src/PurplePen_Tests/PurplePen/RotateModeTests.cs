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
    public class RotateModeTests: TestFixtureBase
    {
        TestUI ui;
        Controller controller;
        EventDB eventDB;

        [TestInitialize]
        public void Setup()
        {
            ui = TestUI.Create();
            controller = ui.controller;
            eventDB = controller.GetEventDB();

            string fileName = TestUtil.GetTestFile("modes\\crossings.ppen");

            bool success = controller.LoadInitialFile(fileName, true);
            Assert.IsTrue(success);
        }

        // Rotate a mandatory crossing point.
        [TestMethod]
        public void RotateMandatoryCrossing()
        {
            CourseObj[] highlights;

            // Select Course 3
            controller.SelectTab(3);

            // Select mandatory crossing point.
            controller.LeftButtonDown(new PointF(25.4F, 25.5F), 0.2F);
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(82, highlights[0].controlId.id);

            // Begin rotating mode.
            controller.BeginRotate();
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            CrossingCourseObj obj = (CrossingCourseObj) highlights[0];
            Assert.AreEqual(new PointF(25.9F, 26.4F), obj.location);

            // Should have correct status text.
            Assert.AreEqual(StatusBarText.RotatingObject, controller.StatusText);

            // Move the mouse somewhere (mouse buttons are up).
            ui.MouseMoved(31, -11, 0.1F);

            // The highlight should be in the same place, but rotated.
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            obj = (CrossingCourseObj) highlights[0];
            Assert.AreEqual(new PointF(25.9F, 26.4F), obj.location);
            Assert.AreEqual(187.7F, obj.orientation, 0.1F);

            // Mouse down somewhere.
            MapViewer.DragAction action = controller.LeftButtonDown(new PointF(44, 29), 0.1F);

            // The highlight should be in the same place, but rotated again.
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            obj = (CrossingCourseObj) highlights[0];
            Assert.AreEqual(new PointF(25.9F, 26.4F), obj.location);
            Assert.AreEqual(278.2F, obj.orientation, 0.1F);

            // The control should have its orientation changed, but not its position.
            ControlPoint control = eventDB.GetControl(ControlId(82));
            Assert.AreEqual(new PointF(25.9F, 26.4F), control.location);
            Assert.AreEqual(278.2F, control.orientation, 0.1F);
        }

        // Rotate a optional crossing point.
        [TestMethod]
        public void RotateOptionalCrossing()
        {
            CourseObj[] highlights;

            // Select Course 3
            controller.SelectTab(3);

            // Select mandatory crossing point.
            controller.LeftButtonDown(new PointF(76, -5F), 0.2F);
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(2, highlights[0].specialId.id);

            // Begin rotating mode.
            controller.BeginRotate();
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            CrossingCourseObj obj = (CrossingCourseObj) highlights[0];
            Assert.AreEqual(new PointF(76, -4.8F), obj.location);

            // Should have correct status text.
            Assert.AreEqual(StatusBarText.RotatingObject, controller.StatusText);

            // Move the mouse somewhere (mouse buttons are up).
            ui.MouseMoved(31, -11, 0.1F);

            // The highlight should be in the same place, but rotated.
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            obj = (CrossingCourseObj) highlights[0];
            Assert.AreEqual(new PointF(76, -4.8F), obj.location);
            Assert.AreEqual(97.8F, obj.orientation, 0.1F);

            // Mouse down somewhere.
            MapViewer.DragAction action = controller.LeftButtonDown(new PointF(44, 29), 0.1F);

            // The highlight should be in the same place, but rotated again.
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            obj = (CrossingCourseObj) highlights[0];
            Assert.AreEqual(new PointF(76, -4.8F), obj.location);
            Assert.AreEqual(43.4F, obj.orientation, 0.1F);

            // The control should have its orientation changed, but not its position.
            Special special = eventDB.GetSpecial(SpecialId(2));
            Assert.AreEqual(new PointF(76, -4.8F), special.locations[0]);
            Assert.AreEqual(43.4F, special.orientation, 0.1F);
        }
    }


}

#endif //TEST
