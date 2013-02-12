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
    public class DefaultModeTests: TestFixtureBase
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

        [TestMethod]
        // Should be able to select a control with the mouse and have it highlight. 
        // The corresponding highlight should be highlighted in the description.
        public void SelectControl()
        {
            MapViewer.DragAction dragAction;
            CourseObj[] highlights;

            Setup("modes\\marymoor.coursescribe");
            
            // Select course 3.
            controller.SelectTab(3);       // Course 3.
            CheckHighlightedLines(controller, -1, -1);

            // Click on control 5 (#47).
            dragAction = controller.LeftButtonDown(new PointF(0.9F, 30.5F), 0.1F);

            // Check correct description line highlighted.
            CheckHighlightedLines(controller, 7, 7);

            // Check correct highlights appear.
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.IsInstanceOfType(   highlights[0],   typeof(ControlCourseObj));
            Assert.AreEqual(47, highlights[0].controlId.id);
            Assert.IsInstanceOfType(   highlights[1],   typeof(ControlNumberCourseObj));
            Assert.AreEqual(47, highlights[1].controlId.id);

            // Select all controls.
            controller.SelectTab(0);    
            CheckHighlightedLines(controller, -1, -1);

            // Click on number for control #54.
            dragAction = controller.LeftButtonDown(new PointF(59.3F, 5.5F), 0.1F);

            // Check correct description line highlighted.
            CheckHighlightedLines(controller, 22, 22);

            // Check correct highlights appear.
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.IsInstanceOfType(   highlights[0],   typeof(ControlCourseObj));
            Assert.AreEqual(54, highlights[0].controlId.id);
            Assert.IsInstanceOfType(   highlights[1],   typeof(CodeCourseObj));
            Assert.AreEqual(54, highlights[1].controlId.id);

            // Default mode should not be cancellable
            Assert.IsFalse(controller.CanCancelMode());
        }

        [TestMethod]
        // Should be able to select a point special with the mouse and have it highlight. 
        public void SelectPointSpecial()
        {
            MapViewer.DragAction dragAction;
            CourseObj[] highlights;

            Setup("modes\\marymoor.coursescribe");

            // Select course 3.
            controller.SelectTab(3);       // Course 3.
            CheckHighlightedLines(controller, -1, -1);

            // Click on first aid point
            dragAction = controller.LeftButtonDown(new PointF(15.3F, -42F), 0.1F);

            // Check no description line highlighted.
            CheckHighlightedLines(controller, -1, -1);

            // Check correct highlights appear.
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.IsInstanceOfType(   highlights[0],   typeof(FirstAidCourseObj));
            Assert.AreEqual(1, highlights[0].specialId.id);

            // Select all controls.
            controller.SelectTab(0);
            CheckHighlightedLines(controller, -1, -1);

            // Click on first aid point
            dragAction = controller.LeftButtonDown(new PointF(13.3F, -41F), 0.1F);

            // Check no description line highlighted.
            CheckHighlightedLines(controller, -1, -1);

            // Check correct highlights appear.
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.IsInstanceOfType(   highlights[0],   typeof(FirstAidCourseObj));
            Assert.AreEqual(1, highlights[0].specialId.id);

            // Default mode should not be cancellable
            Assert.IsFalse(controller.CanCancelMode());
        }

        [TestMethod]
        // Should have the move cursor on when moving over a highlighted object.
        // Also, the status text should change appropriately.
        public void MoveCursor()
        {
            Cursor cursor;

            Setup("modes\\marymoor.coursescribe");

            // Select course 3.
            controller.SelectTab(3);       // Course 3.

            // Should all be default cursor.
            cursor = controller.GetMouseCursor(new PointF(0.9F, 30.5F), 0.1F);
            Assert.AreSame(Cursors.Default, cursor);
            cursor = controller.GetMouseCursor(new PointF(-1.7F, 38.6F), 0.1F);
            Assert.AreSame(Cursors.Default, cursor);

            // Should be default status text
            ui.MouseMoved(0.9F, 30.5F, 0.1F);
            Assert.AreEqual(StatusBarText.DefaultStatus, controller.StatusText);

            controller.SelectDescriptionLine(7);     // select control 5 (#47)

            // Look at mouse on control circle
            cursor = controller.GetMouseCursor(new PointF(0.9F, 30.5F), 0.1F);
            Assert.AreSame(Cursors.SizeAll, cursor);
            // Should be move status text
            ui.MouseMoved(0.9F, 30.5F, 0.1F);
            Assert.AreEqual(StatusBarText.DragObject, controller.StatusText);

            // Check control number. It should be movable too.
            cursor = controller.GetMouseCursor(new PointF(-1.7F, 38.6F), 0.1F);
            Assert.AreSame(Cursors.SizeAll, cursor);
            // check status text
            ui.MouseMoved(-1.7F, 38.6F, 0.1F);
            Assert.AreEqual(StatusBarText.DragObject, controller.StatusText);

            // Elsewhere should still be default cursor.
            cursor = controller.GetMouseCursor(new PointF(-2.0F, 11.4F), 0.1F);
            Assert.AreSame(Cursors.Default, cursor);
            cursor = controller.GetMouseCursor(new PointF(-3, 33), 0.1F);
            Assert.AreSame(Cursors.Default, cursor);
            // check status text
            ui.MouseMoved(-3, 33, 0.1F);
            Assert.AreEqual(StatusBarText.DefaultStatus, controller.StatusText);
        }

        [TestMethod]
        // Move a control with the mouse.
        public void MoveControl()
        {
            Setup("modes\\marymoor.coursescribe");

            // Select course 3.
            controller.SelectTab(3);       // Course 3.
            CheckHighlightedLines(controller, -1, -1);

            // Select control 5.
            controller.SelectDescriptionLine(7);
            CheckHighlightedLines(controller, 7, 7);

            // Click on control 5 (#47).
            MapViewer.DragAction dragAction = controller.LeftButtonDown(new PointF(0.9F, 30.5F), 0.1F);
            Assert.AreEqual(MapViewer.DragAction.ImmediateDrag, dragAction);

            // Drag the control
            controller.LeftButtonDrag(new PointF(12.9F, 36.5F), 0.1F);
            ui.MouseMoved(12.9F, 36.5F, 0.1F);

            // Check the highlights
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(3, highlights.Length);
            Assert.AreEqual("Control:        control:47  course-control:306  scale:1  location:(14.51,37.32)  gaps:", 
                                        highlights[0].ToString());
            Assert.AreEqual("Leg:            control:72  scale:1  path:N(0.69,12.76)--N(13.12,34.86)",
                                        highlights[1].ToString());
            Assert.AreEqual("Leg:            control:47  scale:1  path:N(16.74,35.58)--N(37.87,19.14)",
                                        highlights[2].ToString());
            // Check the status text
            Assert.AreEqual(StatusBarText.DraggingObject, controller.StatusText);
            // Drag the control
            controller.LeftButtonDrag(new PointF(22.9F, 33.5F), 0.1F);
            ui.MouseMoved(22.9F, 33.5F, 0.1F);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(3, highlights.Length);
            Assert.AreEqual("Control:        control:47  course-control:306  scale:1  location:(24.51,34.32)  gaps:",
                                        highlights[0].ToString());
            Assert.AreEqual("Leg:            control:72  scale:1  path:N(1.35,12.25)--N(22.46,32.37)",
                                        highlights[1].ToString());
            Assert.AreEqual("Leg:            control:47  scale:1  path:N(26.42,32.24)--N(38.19,19.48)",
                                        highlights[2].ToString());
            // Check the status text
            Assert.AreEqual(StatusBarText.DraggingObject, controller.StatusText);

            // Finish dragging the control
            controller.LeftButtonEndDrag(new PointF(21.9F, 34.5F), 0.1F);
            ui.MouseMoved(21.9F, 34.5F, 0.1F);
            // Check the status text
            Assert.AreEqual(StatusBarText.DragObject, controller.StatusText);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(2, highlights.Length);
            Assert.AreEqual("Control:        control:47  course-control:306  scale:1  location:(23.51,35.32)  gaps:",
                                        highlights[0].ToString());
            Assert.IsInstanceOfType(   highlights[1],   typeof(ControlNumberCourseObj));
            Assert.AreEqual(47, highlights[1].controlId.id);

            // Make sure the control is now moved.
            PointF newLocation = eventDB.GetControl(ControlId(47)).location;
            Assert.AreEqual(23.51F, newLocation.X);
            Assert.AreEqual(35.32F, newLocation.Y);
        }

        // Move a control number with the mouse.
        [TestMethod]
        public void MoveControlNumber()
        {
            Setup("modes\\marymoor.coursescribe");

            // Select course 3.
            controller.SelectTab(3);       // Course 3.
            CheckHighlightedLines(controller, -1, -1);

            // Select control 5.
            controller.SelectDescriptionLine(7);
            CheckHighlightedLines(controller, 7, 7);

            // Click on the control number.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(new PointF(-1.5F, 38.8F), 0.1F);
            Assert.AreEqual(MapViewer.DragAction.ImmediateDrag, dragAction);

            // Drag the number
            controller.LeftButtonDrag(new PointF(7.2F, 24.5F), 0.1F);
            ui.MouseMoved(7.2F, 24.5F, 0.1F);

            // Check the highlights
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"ControlNumber:  control:47  course-control:306  scale:1  text:5  top-left:(6.03,26.42)
                font-name:Arial  font-style:Regular  font-height:5.57", highlights[0].ToString());
            // Check the status text
            Assert.AreEqual(StatusBarText.DraggingObject, controller.StatusText);

            // Finish dragging the Number
            controller.LeftButtonEndDrag(new PointF(8.8F, 31.3F), 0.1F);
            ui.MouseMoved(8.8F, 31.3F, 0.1F);
            // Check the status text
            Assert.AreEqual(StatusBarText.DragObject, controller.StatusText);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(2, highlights.Length);
            Assert.AreEqual(@"ControlNumber:  control:47  course-control:306  scale:1  text:5  top-left:(7.63,33.22)
                font-name:Arial  font-style:Regular  font-height:5.57",
                                        highlights[1].ToString());
            Assert.IsInstanceOfType(   highlights[0],   typeof(ControlCourseObj));
            Assert.AreEqual(47, highlights[0].controlId.id);

            // Make sure the number is now moved.
            CourseControl courseControl = eventDB.GetCourseControl(CourseControlId(306));
            Assert.AreEqual(true, courseControl.customNumberPlacement);
            Assert.AreEqual(6.67F, courseControl.numberDeltaX, 0.01F);
            Assert.AreEqual(-1.22F, courseControl.numberDeltaY, 0.01F);
        }
	

        [TestMethod]
        // Move a special with the mouse.
        public void MoveSpecial()
        {
            Setup("modes\\marymoor.coursescribe");

            // Select course 3.
            controller.SelectTab(3);       // Course 3.
            CheckHighlightedLines(controller, -1, -1);

            // Click on first aid point to select it.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(new PointF(15.3F, -42F), 0.1F);

            // Should have moving mouse cursor
            Cursor cursor = controller.GetMouseCursor(new PointF(15F, -41.5F), 0.1F);
            Assert.AreSame(Cursors.SizeAll, cursor);

            // Click on first aid point to drag it.
            dragAction = controller.LeftButtonDown(new PointF(15F, -41.5F), 0.1F);
            Assert.AreEqual(MapViewer.DragAction.ImmediateDrag, dragAction);

            // Drag the first aid point
            controller.LeftButtonDrag(new PointF(12.9F, 36.5F), 0.1F);
            ui.MouseMoved(12.9F, 36.5F, 0.1F);

            // Check the highlights
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual("FirstAid:       special:1  scale:1  location:(12.4,36.8)",
                                        highlights[0].ToString());
            // Check the status text
            Assert.AreEqual(StatusBarText.DraggingObject, controller.StatusText);


            // Drag the firsr aid point
            controller.LeftButtonDrag(new PointF(22.9F, 33.5F), 0.1F);
            ui.MouseMoved(22.9F, 33.5F, 0.1F);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual("FirstAid:       special:1  scale:1  location:(22.4,33.8)",
                                        highlights[0].ToString());
            // Check the status text
            Assert.AreEqual(StatusBarText.DraggingObject, controller.StatusText);

            // Finish dragging the first aid point
            controller.LeftButtonEndDrag(new PointF(21.9F, 34.5F), 0.1F);
            ui.MouseMoved(21.9F, 34.5F, 0.1F);
            // Check the status text
            Assert.AreEqual(StatusBarText.DragObject, controller.StatusText);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual("FirstAid:       special:1  scale:1  location:(21.4,34.8)",
                                        highlights[0].ToString());
            Assert.AreEqual(1, highlights[0].specialId.id);

            // Make sure the special is now moved.
            PointF newLocation = eventDB.GetSpecial(SpecialId(1)).locations[0];
            Assert.AreEqual(21.4F, newLocation.X);
            Assert.AreEqual(34.8F, newLocation.Y);
        }


        [TestMethod]
        // Move a special with the mouse.
        public void MoveSpecialCorner()
        {
            Setup("modes\\marymoor2.coursescribe");

            // Select course 3.
            controller.SelectTab(3);       // Course 3.
            CheckHighlightedLines(controller, -1, -1);

            // Click on area to select it.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(new PointF(-0.3F, 0.2F), 0.1F);

            // Should have moving mouse cursor
            Cursor cursor = controller.GetMouseCursor(new PointF(-0.3F, 0.2F), 0.1F);
            Assert.AreSame(Cursors.SizeAll, cursor);

            // Over corner should have move corner cursor
            ui.MouseMoved(2.9F, 7.2F, 0.1F);
            cursor = controller.GetMouseCursor(new PointF(2.9F, 7.2F), 0.1F);
            Assert.AreSame(Util.MoveHandleCursor, cursor);

            // And the moving corner text.
            Assert.AreEqual(StatusBarText.DragCorner, controller.StatusText);


            // Click on corner point to drag it.
            dragAction = controller.LeftButtonDown(new PointF(2.9F, 7.2F), 0.1F);
            Assert.AreEqual(MapViewer.DragAction.ImmediateDrag, dragAction);
            Assert.AreEqual(StatusBarText.DraggingCorner, controller.StatusText);

            // Drag the corner
            controller.LeftButtonDrag(new PointF(7.9F, 11.2F), 0.1F);
            ui.MouseMoved(7.9F, 11.2F, 0.1F);

            // Check the highlights
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"OOB:            special:4  scale:1  path:N(8,11)--N(11,2)--N(0,-7)--N(-12,-3)--N(8,11)",
                                        highlights[0].ToString());
            // Check the status text
            Assert.AreEqual(StatusBarText.DraggingCorner, controller.StatusText);
            // Check the cursor
            cursor = controller.GetMouseCursor(new PointF(7.9F, 11.2F), 0.1F);
            Assert.AreSame(Util.MoveHandleCursor, cursor);

            
            // Finish dragging the corner point
            controller.LeftButtonEndDrag(new PointF(9.9F, 8.2F), 0.1F);
            ui.MouseMoved(9.9F, 8.2F, 0.1F);
            // Check the status text
            Assert.AreEqual(StatusBarText.DragCorner, controller.StatusText);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual("OOB:            special:4  scale:1  path:N(10,8)--N(11,2)--N(0,-7)--N(-12,-3)--N(10,8)",
                                        highlights[0].ToString());
            Assert.AreEqual(4, highlights[0].specialId.id);

            // Make sure the special is now moved.
            PointF[] newLocations = eventDB.GetSpecial(SpecialId(4)).locations;
            Assert.AreEqual(new PointF(10,8), newLocations[0]);
            Assert.AreEqual(new PointF(11,2), newLocations[1]);
            Assert.AreEqual(new PointF(0,-7), newLocations[2]);
            Assert.AreEqual(new PointF(-12,-3), newLocations[3]);
            
        }


        [TestMethod]
        // Size a description with the mouse.
        public void SizeDescription()
        {
            Setup("modes\\marymoor2.coursescribe");

            // Select course 3.
            controller.SelectTab(3);       // Course 3.
            CheckHighlightedLines(controller, -1, -1);

            // Click on area to select it.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(new PointF(-24, 12), 0.1F);

            // Should have moving mouse cursor
            Cursor cursor = controller.GetMouseCursor(new PointF(-24, 12), 0.1F);
            Assert.AreSame(Cursors.SizeAll, cursor);

            // Over size handle should have sizing cursor
            ui.MouseMoved(-9.6F, 7.4F, 0.3F);
            cursor = controller.GetMouseCursor(new PointF(-9.6F, 7.4F), 0.3F);
            Assert.AreSame(Cursors.SizeWE, cursor);

            // And the moving description.
            Assert.AreEqual(StatusBarText.SizeRectangle, controller.StatusText);

            // Click on size handle to drag it.
            dragAction = controller.LeftButtonDown(new PointF(-9.6F, 7.4F), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.ImmediateDrag, dragAction);
            Assert.AreEqual(StatusBarText.SizingRectangle, controller.StatusText);

            // Drag the corner
            controller.LeftButtonDrag(new PointF(-2F, -22F), 0.3F);
            ui.MouseMoved(-2F, -22F, 0.1F);

            // Check the highlights
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"Description:    layer:1  special:8  scale:1  rect:{X=-50,Y=-51.54444,Width=48.1,Height=101.5444}",
                                        highlights[0].ToString());
            // Check the status text
            Assert.AreEqual(StatusBarText.SizingRectangle, controller.StatusText);
            // Check the cursor
            cursor = controller.GetMouseCursor(new PointF(-2F, -22F), 0.3F);
            Assert.AreSame(Cursors.SizeWE, cursor);


            // Finish dragging the size point
            controller.LeftButtonEndDrag(new PointF(10F, -20F), 0.3F);
            ui.MouseMoved(-1F, -20F, 0.3F);
            Assert.AreEqual(StatusBarText.DragObject, controller.StatusText);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual("Description:    layer:1  special:8  scale:1  rect:{X=-50,Y=-77.41177,Width=60.35295,Height=127.4118}",
                                        highlights[0].ToString());
            Assert.AreEqual(8, highlights[0].specialId.id);

            // Make sure the description is now sized.
            PointF[] newLocations = eventDB.GetSpecial(SpecialId(8)).locations;
            Assert.AreEqual(new PointF(-50,50), newLocations[0]);
            Assert.AreEqual(-42.549, newLocations[1].X, 0.001);
            Assert.AreEqual(50, newLocations[1].Y);
        }


        [TestMethod]
        // Size a description with the mouse.
        public void SizeDescription2()
        {
            Setup("modes\\marymoor2.coursescribe");

            // Select course 3.
            controller.SelectTab(3);       // Course 3.
            CheckHighlightedLines(controller, -1, -1);

            // Click on area to select it.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(new PointF(-24, 12), 0.1F);

            // Should have moving mouse cursor
            Cursor cursor = controller.GetMouseCursor(new PointF(-24, 12), 0.1F);
            Assert.AreSame(Cursors.SizeAll, cursor);

            // Over size handle should have sizing cursor
            ui.MouseMoved(-9F, 50.0F, 0.3F);
            cursor = controller.GetMouseCursor(new PointF(-9F, 50.0F), 0.3F);
            Assert.AreSame(Cursors.SizeNESW, cursor);

            // And the moving description.
            Assert.AreEqual(StatusBarText.SizeRectangle, controller.StatusText);

            // Click on size handle to drag it.
            dragAction = controller.LeftButtonDown(new PointF(-9F, 50.0F), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.ImmediateDrag, dragAction);
            Assert.AreEqual(StatusBarText.SizingRectangle, controller.StatusText);

            // Drag the corner
            controller.LeftButtonDrag(new PointF(-143F, -3.4F), 0.3F);
            ui.MouseMoved(-143F, -3.4F, 0.1F);

            // Check the highlights
            CourseObj[] highlights = (CourseObj[])controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"Description:    layer:1  special:8  scale:1  rect:{X=-143.5,Y=-35.5,Width=133.1361,Height=32.1} columns:3",
                                        highlights[0].ToString());
            // Check the status text
            Assert.AreEqual(StatusBarText.SizingRectangle, controller.StatusText);
            // Check the cursor
            cursor = controller.GetMouseCursor(new PointF(-143F, -3.4F), 0.3F);
            Assert.AreSame(Cursors.SizeNESW, cursor);

            // Finish dragging the size point
            controller.LeftButtonEndDrag(new PointF(-105F, -47F), 0.3F);
            ui.MouseMoved(-105F, -47F, 0.3F);
            Assert.AreEqual(StatusBarText.SizeRectangle, controller.StatusText);

            // Check the highlights
            highlights = (CourseObj[])controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual("Description:    layer:1  special:8  scale:1  rect:{X=-105,Y=-47.22101,Width=55.91665,Height=13.48188} columns:3",
                                        highlights[0].ToString());
            Assert.AreEqual(8, highlights[0].specialId.id);

            // Make sure the description is now sized.
            PointF[] newLocations = eventDB.GetSpecial(SpecialId(8)).locations;
            Assert.AreEqual(new PointF(-105F, -33.7391319F), newLocations[0]);
            Assert.AreEqual(-102.79F, newLocations[1].X, 0.001);
            Assert.AreEqual(-33.739F, newLocations[1].Y, 0.001);

            // Should be 3 columns.
            Assert.AreEqual(3, eventDB.GetSpecial(SpecialId(8)).numColumns);
        }

        [TestMethod]
        // Move a leg bend with the mouse.
        public void MoveLegBend()
        {
            Setup("modes\\speciallegs.ppen");

            // Select course 1.
            controller.SelectTab(1);       // Course 1.
            CheckHighlightedLines(controller, -1, -1);

            // Click on leg to select it.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(new PointF(18.4F, 30.1F), 0.3F);

            // Over corner should have move corner cursor
            ui.MouseMoved(12.2F, 19.4F, 0.3F);
            Cursor cursor = controller.GetMouseCursor(new PointF(12.2F, 19.4F), 0.3F);
            Assert.AreSame(Util.MoveHandleCursor, cursor);

            // And the moving corner text.
            Assert.AreEqual(StatusBarText.DragCorner, controller.StatusText);


            // Click on corner point to drag it.
            dragAction = controller.LeftButtonDown(new PointF(12.2F, 19.4F), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.ImmediateDrag, dragAction);
            Assert.AreEqual(StatusBarText.DraggingCorner, controller.StatusText);

            // Drag the corner
            controller.LeftButtonDrag(new PointF(7.2F, 9.4F), 0.3F);
            ui.MouseMoved(7.2F, 9.4F, 0.3F);

            // Check the highlights
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"Leg:            control:3  course-control:3  scale:1  course-control2:4  path:N(9.1,10.27)--N(7,10)--N(23.65,38.6)",
                                        highlights[0].ToString());

            
            // Check the status text
            Assert.AreEqual(StatusBarText.DraggingCorner, controller.StatusText);
            // Check the cursor
            cursor = controller.GetMouseCursor(new PointF(7.2F, 9.4F), 0.3F);
            Assert.AreSame(Util.MoveHandleCursor, cursor);

            // Finish dragging the corner point
            controller.LeftButtonEndDrag(new PointF(6.2F, 12.4F), 0.3F);
            ui.MouseMoved(6.2F, 12.4F, 0.3F);
            // Check the status text
            Assert.AreEqual(StatusBarText.DragCorner, controller.StatusText);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual("Leg:            control:3  course-control:3  scale:1  course-control2:4  path:N(7.2,10.16)--N(6,13)--N(23.56,38.67)",
                                        highlights[0].ToString());

            // Make sure the leg is now moved.
            Leg leg = eventDB.GetLeg(QueryEvent.FindLeg(eventDB, ControlId(3), ControlId(4)));
            Assert.AreEqual(1, leg.bends.Length);
            Assert.AreEqual(new PointF(6, 13), leg.bends[0]);
            Assert.AreEqual(new PointF(6, 13), leg.flagStartStop);
        }

        [TestMethod]
        // Move a leg gap with the mouse.
        public void MoveLegGap()
        {
            Setup("modes\\gappedlegs.coursescribe");

            // Select course 1.
            controller.SelectTab(1);       // Course 1.
            CheckHighlightedLines(controller, -1, -1);

            // Click on leg to select it.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(new PointF(71, 0), 0.3F);

            // Over leg gap should have move corner cursor
            ui.MouseMoved(72.5F, 3.5F, 0.3F);
            Cursor cursor = controller.GetMouseCursor(new PointF(72.5F, 3.5F), 0.3F);
            Assert.AreSame(Util.MoveHandleCursor, cursor);

            // And the moving corner text.
            Assert.AreEqual(StatusBarText.DragCorner, controller.StatusText);

            // Click on corner point to drag it.
            dragAction = controller.LeftButtonDown(new PointF(72.5F, 3.5F), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.ImmediateDrag, dragAction);
            Assert.AreEqual(StatusBarText.DraggingCorner, controller.StatusText);

            // Drag the corner
            controller.LeftButtonDrag(new PointF(73.5F, -3.0F), 0.3F);
            ui.MouseMoved(73.5F, -3.0F, 0.3F);

            // Check the highlights
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"Leg:            control:1  course-control:1  scale:1  course-control2:2  path:N(73.43,10.02)--N(68,-22)--N(40.52,-22.36)  gaps: (s:2.96,l:9.74) (s:20.96,l:9)",
                                        highlights[0].ToString());


            // Check the status text
            Assert.AreEqual(StatusBarText.DraggingCorner, controller.StatusText);
            // Check the cursor
            cursor = controller.GetMouseCursor(new PointF(73.5F, -3.0F), 0.3F);
            Assert.AreSame(Util.MoveHandleCursor, cursor);

            // Finish dragging the corner point
            controller.LeftButtonEndDrag(new PointF(76F, -5F), 0.3F);
            ui.MouseMoved(76F, -5F, 0.3F);
            // Check the status text
            Assert.AreEqual(StatusBarText.DefaultStatus, controller.StatusText);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual("Leg:            control:1  course-control:1  scale:1  course-control2:2  path:N(73.43,10.02)--N(68,-22)--N(40.52,-22.36)  gaps: (s:2.96,l:11.3) (s:20.96,l:9)",
                                        highlights[0].ToString());

            // Make sure the gaps are now moved.
            Leg leg = eventDB.GetLeg(QueryEvent.FindLeg(eventDB, ControlId(1), ControlId(2)));
            Assert.AreEqual(2, leg.gaps.Length);
            Assert.AreEqual(7, leg.gaps[0].distanceFromStart, 0.01F);
            Assert.AreEqual(11.3F, leg.gaps[0].length, 0.01F);
            Assert.AreEqual(25, leg.gaps[1].distanceFromStart, 0.01F);
            Assert.AreEqual(9F, leg.gaps[1].length, 0.01F);
        }

        [TestMethod]
        // Move a leg gap with the mouse.
        public void MoveLegGap2()
        {
            Setup("modes\\gappedlegs2.coursescribe");

            // Select course 1.
            controller.SelectTab(1);       // Course 1.
            CheckHighlightedLines(controller, -1, -1);

            // Click on leg to select it.
            MapViewer.DragAction dragAction = controller.LeftButtonDown(new PointF(64, 3.3F), 0.3F);

            // Over leg gap should have move corner cursor
            ui.MouseMoved(67, 6.6F, 0.3F);
            Cursor cursor = controller.GetMouseCursor(new PointF(67, 6.6F), 0.3F);
            Assert.AreSame(Util.MoveHandleCursor, cursor);

            // And the moving corner text.
            Assert.AreEqual(StatusBarText.DragCorner, controller.StatusText);

            // Click on corner point to drag it.
            dragAction = controller.LeftButtonDown(new PointF(67, 6.6F), 0.3F);
            Assert.AreEqual(MapViewer.DragAction.ImmediateDrag, dragAction);
            Assert.AreEqual(StatusBarText.DraggingCorner, controller.StatusText);

            // Drag the corner
            controller.LeftButtonDrag(new PointF(63.4F, 3.1F), 0.3F);
            ui.MouseMoved(63.4F, 3.1F, 0.3F);

            // Check the highlights
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"Leg:            control:1  course-control:1  scale:1  course-control2:2  path:N(71.24,11.14)--N(39.7,-20.4)  gaps: (s:2.96,l:8.52) (s:20.96,l:9)",
                                        highlights[0].ToString());


            // Check the status text
            Assert.AreEqual(StatusBarText.DraggingCorner, controller.StatusText);
            // Check the cursor
            cursor = controller.GetMouseCursor(new PointF(63.4F, 3.1F), 0.3F);
            Assert.AreSame(Util.MoveHandleCursor, cursor);

            // Finish dragging the corner point
            controller.LeftButtonEndDrag(new PointF(55, -8F), 0.3F);
            ui.MouseMoved(55, -8F, 0.3F);
            // Check the status text
            Assert.AreEqual(StatusBarText.DefaultStatus, controller.StatusText);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual("Leg:            control:1  course-control:1  scale:1  course-control2:2  path:N(71.24,11.14)--N(39.7,-20.4)  gaps: (s:2.96,l:27)",
                                        highlights[0].ToString());

            // Make sure the gaps are now moved.
            Leg leg = eventDB.GetLeg(QueryEvent.FindLeg(eventDB, ControlId(1), ControlId(2)));
            Assert.AreEqual(1, leg.gaps.Length);
            Assert.AreEqual(7, leg.gaps[0].distanceFromStart, 0.01F);
            Assert.AreEqual(27F, leg.gaps[0].length, 0.01F);
        }

        [TestMethod]
        // Move a text special with the mouse.
        public void MoveTextSpecial()
        {
            Setup("modes\\marymoor2.coursescribe");

            // Select course 3.
            controller.SelectTab(3);       // Course 3.
            CheckHighlightedLines(controller, -1, -1);

            // Click on text special
            controller.LeftButtonDown(new PointF(62F, 36F), 0.1F);

            // text special should be selected.
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"BasicText:      special:7  scale:1  text:Course 3  top-left:(45,40)
                font-name:Times New Roman  font-style:Bold, Italic  font-height:5.417989  rect:(45,40)-(70,34)",
                                        highlights[0].ToString());
            
            // Drag the special
            MapViewer.DragAction dragAction = controller.LeftButtonDown(new PointF(58F, 35.5F), 0.1F);
            Assert.AreEqual(MapViewer.DragAction.ImmediateDrag, dragAction);

            controller.LeftButtonDrag(new PointF(99F, 48.5F), 0.1F);
            ui.MouseMoved(99F, 48.5F, 0.1F);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"BasicText:      special:7  scale:1  text:Course 3  top-left:(86,53)
                font-name:Times New Roman  font-style:Bold, Italic  font-height:5.417989  rect:(86,53)-(111,47)",
                                        highlights[0].ToString());

            // Check the status text
            Assert.AreEqual(StatusBarText.DraggingObject, controller.StatusText);

            // Finish dragging the special
            controller.LeftButtonEndDrag(new PointF(100F, 47.5F), 0.1F);
            ui.MouseMoved(100F, 47.5F, 0.1F);
            // Check the status text
            Assert.AreEqual(StatusBarText.DragObject, controller.StatusText);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"BasicText:      special:7  scale:1  text:Course 3  top-left:(87,52)
                font-name:Times New Roman  font-style:Bold, Italic  font-height:5.417989  rect:(87,52)-(112,46)",
                                        highlights[0].ToString());

            // Make sure the special is now moved.
            Special newSpecial = eventDB.GetSpecial(SpecialId(7));
            Assert.AreEqual(newSpecial.kind, SpecialKind.Text);
            Assert.AreEqual("$(CourseName)", newSpecial.text);
            Assert.AreEqual("Times New Roman", newSpecial.fontName);
            Assert.IsTrue(newSpecial.fontBold);
            Assert.IsTrue(newSpecial.fontItalic);
            Assert.AreEqual(new PointF(87, 52), newSpecial.locations[0]);
            Assert.AreEqual(new PointF(112, 46), newSpecial.locations[1]);
        }

        [TestMethod]
        // Size a text special with the mouse.
        public void SizeTextSpecialHandle()
        {
            Setup("modes\\marymoor2.coursescribe");

            // Select course 3.
            controller.SelectTab(3);       // Course 3.
            CheckHighlightedLines(controller, -1, -1);

            // Click on text special
            controller.LeftButtonDown(new PointF(62F, 36F), 0.1F);

            // text special should be selected.
            CourseObj[] highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"BasicText:      special:7  scale:1  text:Course 3  top-left:(45,40)
                font-name:Times New Roman  font-style:Bold, Italic  font-height:5.417989  rect:(45,40)-(70,34)",
                                        highlights[0].ToString());

            // Drag the handle
            MapViewer.DragAction dragAction = controller.LeftButtonDown(new PointF(57.5F, 34F), 0.1F);
            Assert.AreEqual(MapViewer.DragAction.ImmediateDrag, dragAction);
            Cursor cursor = controller.GetMouseCursor(new PointF(57.5F, 34F), 0.1F);
            Assert.AreSame(Cursors.SizeNS, cursor);

            controller.LeftButtonDrag(new PointF(60F, 29F), 0.1F);
            ui.MouseMoved(60F, 29F, 0.1F);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"BasicText:      special:7  scale:1  text:Course 3  top-left:(45,40)
                font-name:Times New Roman  font-style:Bold, Italic  font-height:6.765328  rect:(45,40)-(70,29)",
                                        highlights[0].ToString());

            // Check the status text
            Assert.AreEqual(StatusBarText.SizingRectangle, controller.StatusText);

            // Finish dragging the special
            controller.LeftButtonEndDrag(new PointF(66F, 22F), 0.1F);
            ui.MouseMoved(66F, 22F, 0.1F);

            // Check the highlights
            highlights = (CourseObj[]) controller.GetHighlights();
            Assert.AreEqual(1, highlights.Length);
            Assert.AreEqual(@"BasicText:      special:7  scale:1  text:Course 3  top-left:(45,40)
                font-name:Times New Roman  font-style:Bold, Italic  font-height:6.765328  rect:(45,40)-(70,22)",
                                        highlights[0].ToString());


            // Make sure the special is now moved.
            Special newSpecial = eventDB.GetSpecial(SpecialId(7));
            Assert.AreEqual(newSpecial.kind, SpecialKind.Text);
            Assert.AreEqual("$(CourseName)", newSpecial.text);
            Assert.AreEqual("Times New Roman", newSpecial.fontName);
            Assert.IsTrue(newSpecial.fontBold);
            Assert.IsTrue(newSpecial.fontItalic);
            Assert.AreEqual(new PointF(45,40), newSpecial.locations[0]);
            Assert.AreEqual(new PointF(70,22), newSpecial.locations[1]);
 
        }


    }
}

#endif //TEST
