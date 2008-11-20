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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using PurplePen.MapView;
using PurplePen.MapModel;

namespace PurplePen
{
    class AddControlMode: BaseMode
    {
        const int PIXELOFFSETX = -7;         // offset in pixels from the mouse cursor to the cross-hairs of the control to place.
        const int PIXELOFFSETY = 7;

        Controller controller;
        SelectionMgr selectionMgr;
        UndoMgr undoMgr;
        EventDB eventDB;
        bool allControls;                  // Are we in All Controls (true), or adding to a course (false)
        ControlPointKind controlKind;      // Kind of control we are adding.
        float scaleRatio;
        CourseAppearance appearance;

        PointCourseObj highlight;    // the highlight of the control we are creating.
        CourseObj[] additionalHighlights;  // additional highlights to show also. 

        public AddControlMode(Controller controller, SelectionMgr selectionMgr, UndoMgr undoMgr, EventDB eventDB, bool allControls, ControlPointKind controlKind)
        {
            this.controller = controller;
            this.selectionMgr = selectionMgr;
            this.undoMgr = undoMgr;
            this.eventDB = eventDB;
            this.allControls = allControls;
            this.controlKind = controlKind;
            this.scaleRatio = selectionMgr.ActiveCourseView.ScaleRatio;
            this.appearance = controller.GetCourseAppearance();
        }

        public override void BeginMode()
        {
            if (!allControls) {
                // Show all the existing controls we could add (not already in the course).
                controller.SetTemporaryControlView(true, controlKind);
            }

            // Create the initial highlight.
            PointF location; 
            float pixelSize;

            if (controller.GetCurrentLocation(out location, out pixelSize)) {
                PointF highlightLocation;
                bool temp = false;
                HitTestPoint(location, pixelSize, out highlightLocation);
                SetHighlightLocation(highlightLocation, ref temp);
            }
        }

        public override void EndMode()
        {
            // Don't view any other controls any more.
            controller.SetTemporaryControlView(false, ControlPointKind.None);
        }

        public override string StatusText
        {
            get
            {
                Id<ControlPoint> existingControl = Id<ControlPoint>.None;
                PointF location;
                float pixelSize;
                if (controller.GetCurrentLocation(out location, out pixelSize)) {
                    PointF highlightLocation;
                    existingControl = HitTestPoint(location, pixelSize, out highlightLocation);
                }

                switch (controlKind) {
                case ControlPointKind.Start:
                    return (existingControl.IsNone) ? StatusBarText.AddingStart : StatusBarText.AddingExistingStart;
                case ControlPointKind.Finish:
                    return (existingControl.IsNone) ? StatusBarText.AddingFinish : StatusBarText.AddingExistingFinish;
                case ControlPointKind.CrossingPoint:
                    return (existingControl.IsNone) ? StatusBarText.AddingCrossingPoint : StatusBarText.AddingExistingCrossingPoint;
                case ControlPointKind.Normal:
                    return (existingControl.IsNone) ? StatusBarText.AddingControl : string.Format(StatusBarText.AddingExistingControl, eventDB.GetControl(existingControl).code);
                default:
                    return "";
                }
            }
        }

        // Hit test a point to see if it is over an existing control, or will create a new control.
        Id<ControlPoint> HitTestPoint(PointF mouseLocation, float pixelSize, out PointF highlightLocation)
        {
            if (allControls) {
                // If all controls, always new control.
                highlightLocation = new PointF(mouseLocation.X + PIXELOFFSETX * pixelSize, mouseLocation.Y + PIXELOFFSETY * pixelSize);
                return Id<ControlPoint>.None;
            }
            else {
                // Are we over a control we might add?
                CourseLayout layout = controller.GetCourseLayout();
                PointCourseObj courseObj = layout.HitTest(mouseLocation, pixelSize, CourseLayer.AllControls, typeof(PointCourseObj)) as PointCourseObj;
                if (courseObj != null) {
                    highlightLocation = courseObj.location;
                    return courseObj.controlId;
                }
                else {
                    // Allow selecting a control in the current course for a butterfly course. But -- it must be a normal control or crossing point, and not adjacent to the control being inserted.
                    courseObj = layout.HitTest(mouseLocation, pixelSize, CourseLayer.MainCourse, typeof(PointCourseObj)) as PointCourseObj;
                    if (courseObj != null && courseObj.controlId.IsNotNone && eventDB.GetControl(courseObj.controlId).kind == controlKind && (controlKind == ControlPointKind.Normal || controlKind == ControlPointKind.CrossingPoint)) {
                        Id<CourseControl> courseControl1, courseControl2;
                        Id<Course> courseId;
                        GetControlInsertionPoint(courseObj.location, out courseId, out courseControl1, out courseControl2);
                        if (eventDB.GetCourse(courseId).kind != CourseKind.Score && courseObj.courseControlId != courseControl1 && courseObj.courseControlId != courseControl2) {
                            highlightLocation = courseObj.location;
                            return courseObj.controlId;
                        }
                    }

                    highlightLocation = new PointF(mouseLocation.X + PIXELOFFSETX * pixelSize, mouseLocation.Y + PIXELOFFSETY * pixelSize);
                    return Id<ControlPoint>.None;
                }
            }
        }

        public override void MouseMoved(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            PointF highlightLocation;
            Id<ControlPoint> controlId = HitTestPoint(location, pixelSize, out highlightLocation);
            SetHighlightLocation(highlightLocation, ref displayUpdateNeeded);
        }

        public override IMapViewerHighlight[] GetHighlights()
        {
            if (highlight != null) {
                if (additionalHighlights != null && additionalHighlights.Length > 0) {
                    CourseObj[] highlights = new CourseObj[additionalHighlights.Length + 1];
                    highlights[0] = highlight;
                    Array.Copy(additionalHighlights, 0, highlights, 1, additionalHighlights.Length);
                    return highlights;
                }
                else {
                    return new CourseObj[] { highlight };
                }
            }
            else
                return null;
        }

        // Get the controls the define where to insert the new control point.
        private void GetControlInsertionPoint(PointF pt, out Id<Course> courseId, out Id<CourseControl> courseControlId1, out Id<CourseControl> courseControlId2)
        {
            SelectionMgr.SelectionInfo selection = selectionMgr.Selection;
            courseId = selection.ActiveCourseId;
            courseControlId1 = Id<CourseControl>.None;
            courseControlId2 = Id<CourseControl>.None;

            if (selection.SelectionKind == SelectionMgr.SelectionKind.Control)
                courseControlId1 = selection.SelectedCourseControl;
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.Leg) {
                courseControlId1 = selection.SelectedCourseControl;
                courseControlId2 = selection.SelectedCourseControl2;
            }
            else if (courseId.IsNotNone) {
                // Not all control, and neight control or leg is selected. Use the closest leg.
                QueryEvent.LegInfo leg = QueryEvent.FindClosestLeg(eventDB, courseId, pt);
                courseControlId1 = leg.courseControlId1;
                courseControlId2 = leg.courseControlId2;
            }

            if (courseId.IsNotNone)
                QueryEvent.FindControlInsertionPoint(eventDB, courseId, ref courseControlId1, ref courseControlId2);
        }

        public override MapViewer.DragAction LeftButtonDown(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            // Create the new control!

            // Are we creating a new control point, or using existing one?
            PointF highlightLocation;
            Id<ControlPoint> controlId = HitTestPoint(location, pixelSize, out highlightLocation);
            bool createNewControl = controlId.IsNone;
            string commandString;

            switch (controlKind) {
            case ControlPointKind.Start: commandString = CommandNameText.AddStart; break;
            case ControlPointKind.Finish: commandString = CommandNameText.AddFinish; break;
            case ControlPointKind.CrossingPoint: commandString = CommandNameText.AddCrossingPoint; break;
            default: commandString = CommandNameText.AddControl; break;
            }

            undoMgr.BeginCommand(1321, commandString);

            if (createNewControl) {
                // Creating a new control point.
                string newCode = null;
                if (controlKind == ControlPointKind.Normal)
                    newCode = QueryEvent.NextUnusedControlCode(eventDB);
                controlId = ChangeEvent.AddControlPoint(eventDB, controlKind, newCode, highlightLocation, 0);
                if (controlKind == ControlPointKind.Finish)
                    ChangeEvent.ChangeDescriptionSymbol(eventDB, controlId, 0, "14.3");   // set finish to "navigate to finish".
                else if (controlKind == ControlPointKind.CrossingPoint)
                    ChangeEvent.ChangeDescriptionSymbol(eventDB, controlId, 0, "13.3");   // set to mandatory crossing point.
            }

            if (allControls) {
                // select the new control.
                selectionMgr.SelectControl(controlId);
            }
            else {
                // Add the control to the current course.

                // Get where to add the control.
                Id<Course> courseId;
                Id<CourseControl> courseControl1, courseControl2;
                GetControlInsertionPoint(highlightLocation, out courseId, out courseControl1, out courseControl2);

                // And add it.
                Id<CourseControl> courseControlId;
                if (controlKind == ControlPointKind.Start)
                    courseControlId = ChangeEvent.AddStartToCourse(eventDB, controlId, courseId, true);
                else if (controlKind == ControlPointKind.Finish)
                    courseControlId = ChangeEvent.AddFinishToCourse(eventDB, controlId, courseId, true);
                else
                    courseControlId = ChangeEvent.AddCourseControl(eventDB, controlId, courseId, courseControl1, courseControl2);

                // select the new control.
                selectionMgr.SelectCourseControl(courseControlId);
            }

            undoMgr.EndCommand(1321);

            controller.DefaultCommandMode();

            return MapViewer.DragAction.None;
        }

        // Create the highlight, and put it at the given location.
        // Set displayUpdateNeeded to true if the highlight was just created or was moved.
        void SetHighlightLocation(PointF highlightLocation, ref bool displayUpdateNeeded)
        {
            if (highlight != null && highlight.location == highlightLocation)
                return;

            // Get where the control is being inserted.
            Id<Course> courseId;
            Id<CourseControl> courseControl1, courseControl2;
            GetControlInsertionPoint(highlightLocation, out courseId, out courseControl1, out courseControl2);

            // Note, we cannot changed this existing highlight because it is needed for erasing.

            switch (controlKind) {
            case ControlPointKind.Normal:
                highlight = new ControlCourseObj(Id<ControlPoint>.None, Id<CourseControl>.None, scaleRatio, appearance, 0xFFFFFFFF, highlightLocation);

                if (courseId.IsNotNone && eventDB.GetCourse(courseId).kind != CourseKind.Score) {
                    // Show the legs to and from the control also as additional highlights.
                    additionalHighlights = CreateLegHighlights(eventDB, highlightLocation, Id<ControlPoint>.None, controlKind, courseControl1, courseControl2, scaleRatio, appearance);
                }
                break;

            case ControlPointKind.Start:
                highlight = new StartCourseObj(Id<ControlPoint>.None, Id<CourseControl>.None, scaleRatio, appearance, 0, highlightLocation);
                break;
            case ControlPointKind.Finish:
                highlight = new FinishCourseObj(Id<ControlPoint>.None, Id<CourseControl>.None, scaleRatio, appearance, 0xFFFFFFFF, highlightLocation);
                break;
            case ControlPointKind.CrossingPoint:
                highlight = new CrossingCourseObj(Id<ControlPoint>.None, Id<CourseControl>.None, Id<Special>.None, scaleRatio, appearance, 0, highlightLocation);

                if (courseId.IsNotNone && eventDB.GetCourse(courseId).kind != CourseKind.Score) {
                    // Show the legs to and from the control also as additional highlights.
                    additionalHighlights = CreateLegHighlights(eventDB, highlightLocation, Id<ControlPoint>.None, controlKind, courseControl1, courseControl2, scaleRatio, appearance);
                }
                break;
            default:
                throw new Exception("bad control kind");
            }

            highlight.location = highlightLocation;
            displayUpdateNeeded = true;
        }

        // Create a leg object from one point to another. Might return null. The controlIds can be None, but if they are supplied, then
        // they are used to handle bends. If either is null, the leg object is just straight. Gaps are never displayed.
        private static LegCourseObj CreateLegHighlight(EventDB eventDB, PointF pt1, ControlPointKind kind1, Id<ControlPoint> controlId1, PointF pt2, ControlPointKind kind2, Id<ControlPoint> controlId2, float scaleRatio, CourseAppearance appearance)
        {
            LegGap[] gaps;

            SymPath path = CourseFormatter.GetLegPath(eventDB, pt1, kind1, controlId1, pt2, kind2, controlId2, scaleRatio, appearance, out gaps);
            if (path != null)
                return new LegCourseObj(controlId1, Id<CourseControl>.None, Id<CourseControl>.None, scaleRatio, appearance, path, null);     // We never display the gaps, because it looks dumb.
            else
                return null;
        }

        // Create highlights to and from a point to course controls. If controlDrag is set (optional), it is 
        // used to get the correct bends for legs.
        // Static because it is used from DragControlMode also.
        public static CourseObj[] CreateLegHighlights(EventDB eventDB, PointF newPoint, Id<ControlPoint>controlDrag, ControlPointKind controlKind, Id<CourseControl> courseControlId1, Id<CourseControl> courseControlId2, float scaleRatio, CourseAppearance appearance)
        {
            List<CourseObj> highlights = new List<CourseObj>();

            if (courseControlId1.IsNotNone) {
                Id<ControlPoint> controlId1 = eventDB.GetCourseControl(courseControlId1).control;
                ControlPoint control1 = eventDB.GetControl(controlId1);
                LegCourseObj highlight = CreateLegHighlight(eventDB, control1.location, control1.kind, controlId1, newPoint, controlKind, controlDrag, scaleRatio, appearance);
                if (highlight != null)
                    highlights.Add(highlight);
            }

            if (courseControlId2.IsNotNone) {
                Id<ControlPoint> controlId2 = eventDB.GetCourseControl(courseControlId2).control;
                ControlPoint control2 = eventDB.GetControl(controlId2);
                LegCourseObj highlight = CreateLegHighlight(eventDB, newPoint, controlKind, controlDrag, control2.location, control2.kind, controlId2, scaleRatio, appearance);
                if (highlight != null)
                    highlights.Add(highlight);
            }

            return highlights.ToArray();
        }
    }

    class AddPointSpecialMode: BaseMode
    {
        const int PIXELOFFSETX = -7;         // offset in pixels from the mouse cursor to the cross-hairs of the special to place.
        const int PIXELOFFSETY = 7;

        Controller controller;
        SelectionMgr selectionMgr;
        UndoMgr undoMgr;
        EventDB eventDB;
        SpecialKind specialKind;      // Kind of special we are adding.
        float scaleRatio;
        CourseAppearance appearance;

        PointCourseObj highlight;    // the highlight we are creating.

        public AddPointSpecialMode(Controller controller, SelectionMgr selectionMgr, UndoMgr undoMgr, EventDB eventDB, SpecialKind specialKind)
        {
            this.controller = controller;
            this.selectionMgr = selectionMgr;
            this.undoMgr = undoMgr;
            this.eventDB = eventDB;
            this.specialKind = specialKind;
            this.scaleRatio = selectionMgr.ActiveCourseView.ScaleRatio;
            this.appearance = controller.GetCourseAppearance();
        }

        public override void BeginMode()
        {
            // Create the initial highlight.
            PointF location;
            float pixelSize;

            if (controller.GetCurrentLocation(out location, out pixelSize)) {
                bool temp = false;
                PointF highlightLocation = new PointF(location.X + PIXELOFFSETX * pixelSize, location.Y + PIXELOFFSETY * pixelSize);
                SetHighlightLocation(highlightLocation, ref temp);
            }
        }

        public override string StatusText
        {
            get
            {
                return StatusBarText.AddingObject;
            }
        }

        public override void MouseMoved(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            PointF highlightLocation = new PointF(location.X + PIXELOFFSETX * pixelSize, location.Y + PIXELOFFSETY * pixelSize);
            SetHighlightLocation(highlightLocation, ref displayUpdateNeeded);
        }

        public override IMapViewerHighlight[] GetHighlights()
        {
            if (highlight != null)
                return new CourseObj[] { highlight };
            else
                return null;
        }

        public override MapViewer.DragAction LeftButtonDown(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            // Create the new special!

            PointF highlightLocation = new PointF(location.X + PIXELOFFSETX * pixelSize, location.Y + PIXELOFFSETY * pixelSize);

            undoMgr.BeginCommand(1322, CommandNameText.AddObject);

            // Creat the special
            Id<Special> specialId = ChangeEvent.AddPointSpecial(eventDB, specialKind, highlightLocation, 0);

            // select the new special.
            selectionMgr.SelectSpecial(specialId);
            undoMgr.EndCommand(1322);

            controller.DefaultCommandMode();
            return MapViewer.DragAction.None;
        }

        // Create the highlight, and put it at the given location.
        // Set displayUpdateNeeded to true if the highlight was just created or was moved.
        void SetHighlightLocation(PointF highlightLocation, ref bool displayUpdateNeeded)
        {
            if (highlight != null && highlight.location == highlightLocation)
                return;

            // Note, we cannot change this existing highlight because it is needed for erasing.
            switch (specialKind) {
            case SpecialKind.FirstAid:
                highlight = new FirstAidCourseObj(Id<Special>.None, scaleRatio, appearance, highlightLocation);
                break;
            case SpecialKind.Water:
                highlight = new WaterCourseObj(Id<Special>.None, scaleRatio, appearance, highlightLocation);
                break;
            case SpecialKind.OptCrossing:
                highlight = new CrossingCourseObj(Id<ControlPoint>.None, Id<CourseControl>.None, Id<Special>.None, scaleRatio, appearance, 0, highlightLocation);
                break;
            case SpecialKind.Forbidden:
                highlight = new ForbiddenCourseObj(Id<Special>.None, scaleRatio, appearance, highlightLocation);
                break;
            case SpecialKind.RegMark:
                highlight = new RegMarkCourseObj(Id<Special>.None, scaleRatio, appearance, highlightLocation);
                break;
            default:
                throw new Exception("bad special kind");
            }

            highlight.location = highlightLocation;
            displayUpdateNeeded = true;
        }
    }

    // Mode to add a line or area special.
    class AddLineAreaSpecialMode: BaseMode
    {
        const float CLOSEDISTANCE = 5F;          // pixel distance to consider closing the polygon.

        Controller controller;
        SelectionMgr selectionMgr;
        UndoMgr undoMgr;
        EventDB eventDB;
        SpecialKind specialKind;      // Kind of special we are adding.
        bool isArea;                     // is it an area special?
        float scaleRatio;
        CourseAppearance appearance;

        List<PointF> points = new List<PointF>();      // the list of coordinates in the path we are creating.
        int numberFixedPoints = 0;                          // number of coordinates now fixed in place. 
                                                                            // The last coordinate in points may or may not be fixed depending on this value vs. points.Count.
        BoundaryCourseObj highlight;    // the highlight of the path we are creating.

        public AddLineAreaSpecialMode(Controller controller, SelectionMgr selectionMgr, UndoMgr undoMgr, EventDB eventDB, SpecialKind specialKind, bool isArea)
        {
            this.controller = controller;
            this.selectionMgr = selectionMgr;
            this.undoMgr = undoMgr;
            this.eventDB = eventDB;
            this.specialKind = specialKind;
            this.isArea = isArea;
            this.scaleRatio = selectionMgr.ActiveCourseView.ScaleRatio;
            this.appearance = controller.GetCourseAppearance();
        }

        public override string StatusText
        {
            get
            {
                return StatusBarText.AddingLineArea;
            }
        }

        public override Cursor GetMouseCursor(PointF location, float pixelSize)
        {
            return Cursors.Cross;
        }

        public override IMapViewerHighlight[] GetHighlights()
        {
            if (highlight != null)
                return new CourseObj[] { highlight };
            else
                return null;
        }

        public override MapViewer.DragAction LeftButtonDown(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (numberFixedPoints == 0) {
                // The first point. Fix it at the location.
                AddFixedPoint(location);
            }

            displayUpdateNeeded = true;
            return MapViewer.DragAction.DelayedDrag;
        }

        public override void LeftButtonDrag(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            // In the middle of dragging. Current location isn't fixed yet.
            AddUnfixedPoint(location);
            displayUpdateNeeded = true;
        }

        public override void LeftButtonEndDrag(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            // If we ended near to the first point, we've create a polygon and creation is done.
            if (numberFixedPoints >= 3 && Util.Distance(location, points[0]) < pixelSize * CLOSEDISTANCE) {
                if (!isArea)
                    AddFixedPoint(points[0]);  // area symbols close automatically.

                CreateObject();

                controller.DefaultCommandMode();
                displayUpdateNeeded = true;
            }
            else {
                // Ended dragging. Current location the next location.
                AddFixedPoint(location);
                displayUpdateNeeded = true;
            }
        }

        public override void LeftButtonClick(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            // Left button clicked. Ends creating the item and we're done.
            CreateObject();

            controller.DefaultCommandMode();
            displayUpdateNeeded = true;
        }

        // Should there be a left button click here?

        // Add a new fixed point that never gets changed.
        private void AddFixedPoint(PointF newPoint)
        {
            AddUnfixedPoint(newPoint);
            ++numberFixedPoints;
        }

        // Add or change the final unfixed point.
        private void AddUnfixedPoint(PointF newPoint)
        {
            if (numberFixedPoints > points.Count - 1)
                points.Add(newPoint);
            else
                points[numberFixedPoints] = newPoint;

            if (points.Count >= 2)
                highlight = new BoundaryCourseObj(Id<Special>.None, scaleRatio, appearance, new SymPath(points.ToArray()));
        }

        // Create the object with the number of fixed points there are, if there are enough. Returns true if object was created, false
        // if no enough points.
        bool CreateObject()
        {
            // Line objects need at least 2 points, area objects need at least 3.
            if (numberFixedPoints < 2 || (isArea && numberFixedPoints < 3))
                return false;

            undoMgr.BeginCommand(1327, CommandNameText.AddObject);

            // Create the special
            Id<Special> specialId = ChangeEvent.AddLineAreaSpecial(eventDB, specialKind, points.GetRange(0, numberFixedPoints).ToArray());

            // select the new special.
            selectionMgr.SelectSpecial(specialId);
            undoMgr.EndCommand(1327);
            return true;
        }
    }
}
