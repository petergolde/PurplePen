/* Copyright (c), Peter Golde
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

using PurplePen.MapView;
using PurplePen.MapModel;
using System.Diagnostics;
using static PurplePen.Controller;
using PurplePen.Graphics2D;
using System.Data;

namespace PurplePen
{
    class SelectControlToMoveMode: BaseMode
    {
        const int PIXELOFFSETX = -7;         // offset in pixels from the mouse cursor to the cross-hairs of the control to place.
        const int PIXELOFFSETY = 7;

        Controller controller;
        SelectionMgr selectionMgr;
        EventDB eventDB;
        SymbolDB symbolDB;
        PointF? blockLocation;
        float courseObjRatio;
        CourseAppearance appearance;
        MoveAllControlSelected controlSelected;

        PointCourseObj highlight;    // the highlight of the control we are creating.

        public SelectControlToMoveMode(Controller controller, SelectionMgr selectionMgr, EventDB eventDB, SymbolDB symbolDB, PointF? blockLocation, MoveAllControlSelected controlSelected)
        {
            this.controller = controller;
            this.selectionMgr = selectionMgr;
            this.eventDB = eventDB;
            this.symbolDB = symbolDB;
            this.blockLocation = blockLocation;
            this.controlSelected = controlSelected;
            this.appearance = controller.GetCourseAppearance();
            this.courseObjRatio = selectionMgr.ActiveCourseView.CourseObjRatio(appearance);
        }

        public override void BeginMode()
        {
            // Create the initial highlight.
            PointF location;
            float pixelSize;

            if (controller.GetCurrentLocation(out location, out pixelSize)) {
                PointF highlightLocation;
                bool temp = false;
                HitTestPoint(location, pixelSize, out highlightLocation);
                SetHighlightLocation(highlightLocation, pixelSize, ref temp);
            }
        }

        public override void EndMode()
        {
        }

        public override bool CanCancel()
        {
            return false;
        }

        public override string StatusText
        {
            get {
                return StatusBarText.ControlForMoveAllControls;
            }
        }

        // Hit test a point to see if it is over an existing control, or will create a new control.
        PointCourseObj HitTestPoint(PointF mouseLocation, float pixelSize, out PointF highlightLocation)
        {
            // Are we over a control we might add?
            CourseLayout layout = controller.GetCourseLayout();
            PointCourseObj courseObj = layout.HitTest(mouseLocation, pixelSize, CourseLayer.MainCourse, (co => co is PointCourseObj)) as PointCourseObj;


            if (courseObj != null && NotNear(courseObj.location, blockLocation)) {
                if (courseObj.controlId.IsNotNone) {
                    ControlPointKind controlPointKind = eventDB.GetControl(courseObj.controlId).kind;
                    if (controlPointKind == ControlPointKind.Start || controlPointKind == ControlPointKind.Finish || controlPointKind == ControlPointKind.Normal || controlPointKind == ControlPointKind.MapExchange) {
                        highlightLocation = courseObj.location;
                        return courseObj;
                    }
                }
                else if (courseObj.specialId.IsNotNone) {
                    SpecialKind specialKind = eventDB.GetSpecial(courseObj.specialId).kind;
                    if (specialKind == SpecialKind.RegMark) {
                        highlightLocation = courseObj.location;
                        return courseObj;
                    }
                }
            }

            highlightLocation = new PointF();
            return null;
        }

        private bool NotNear(PointF location, PointF? blockLocation)
        {
            if (blockLocation == null)
                return true;

            if (Geometry.Distance(location, blockLocation.Value) > 1) {
                return true;
            }

            return false;
        }

        public override void MouseMoved(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane == Pane.Map) {
                SetHighlightLocation(location, pixelSize, ref displayUpdateNeeded);
            }
        }

        public override IMapViewerHighlight[] GetHighlights(Pane pane)
        {
            if (pane == Pane.Map) {
                if (highlight != null) {
                    return new CourseObj[] { highlight };
                }
            }

            return null;
        }


        public override MapViewer.DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane == Pane.Map) {
                // Delay to see if click or drag.
                return MapViewer.DragAction.DelayedDrag;
            }
            else {
                return MapViewer.DragAction.None;
            }
        }

        public override void LeftButtonDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Map);

            // Drag is move map.
            controller.InitiateMapDragging(locationStart, System.Windows.Forms.MouseButtons.Left);
        }

        public override void LeftButtonClick(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return;

            // Did we click on a control/special?
            PointF highlightLocation;
            PointCourseObj courseObj = HitTestPoint(location, pixelSize, out highlightLocation);
            if (courseObj != null) {
                controlSelected(courseObj.controlId, courseObj.specialId, highlightLocation);
            }
        }

        // Create the highlight, and put it at the given location.
        // Set displayUpdateNeeded to true if the highlight was just created or was moved.
        void SetHighlightLocation(PointF highlightLocation, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (highlight != null && highlight.location == highlightLocation)
                return;

            PointF unused;
            PointCourseObj existingObject = HitTestPoint(highlightLocation, pixelSize, out unused);

            if (existingObject != null) {
                if (highlight == existingObject)
                    return;
                highlight = existingObject;
                displayUpdateNeeded = true;
            }
            else {
                if (highlight == null) {
                    return;
                }

                highlight = null;
                displayUpdateNeeded = true;
            }
        }


        public override bool GetToolTip(Pane pane, PointF location, float pixelSize, out string tipText, out string titleText)
        {
            if (pane == Pane.Map) {
                PointF highlightLocation;
                PointCourseObj courseObj = HitTestPoint(location, pixelSize, out highlightLocation);

                if (courseObj is ControlCourseObj && ((ControlCourseObj)courseObj).controlId.IsNotNone) {
                    TextPart[] textParts = SelectionDescriber.DescribeControl(symbolDB, eventDB, ((ControlCourseObj)courseObj).controlId);
                    base.ConvertTextPartsToToolTip(textParts, out tipText, out titleText);
                    return true;
                }
                else {
                    tipText = titleText = "";
                    return false;
                }
            }
            else {
                return base.GetToolTip(pane, location, pixelSize, out tipText, out titleText);
            }
        }
    }

    class SelectNewControlLocationMode : BaseMode
    {
        const int PIXELOFFSETX = -7;         // offset in pixels from the mouse cursor to the cross-hairs of the special to place.
        const int PIXELOFFSETY = 7;

        Controller controller;
        SelectionMgr selectionMgr;
        EventDB eventDB;
        ControlPointKind controlPointKind;  // Kind of control we are moving.
        SpecialKind specialKind;            // Kind of special we are moving, if controlPointKind==ControlPointKind.None.
        PointF initialLocation;
        PointF otherLocation;
        MoveAllControlsAction action;
        float courseObjRatio;
        CourseAppearance appearance;
        MoveAllLocationSelected locationSelected;

        PointCourseObj highlight;    // the highlight we are creating.

        public SelectNewControlLocationMode(Controller controller, SelectionMgr selectionMgr, EventDB eventDB, ControlPointKind controlPointKind, SpecialKind specialKind, PointF initialLocation, PointF otherLocation, MoveAllControlsAction action, MoveAllLocationSelected locationSelected)
        { 
            this.controller = controller;
            this.selectionMgr = selectionMgr;
            this.eventDB = eventDB;
            this.controlPointKind = controlPointKind;
            this.specialKind = specialKind;
            this.initialLocation = initialLocation;
            this.otherLocation = otherLocation;
            this.action = action;
            this.locationSelected = locationSelected;
            this.appearance = controller.GetCourseAppearance();
            this.courseObjRatio = selectionMgr.ActiveCourseView.CourseObjRatio(appearance);
        }

        public override void BeginMode()
        {
            // Create the initial highlight.
            PointF location;
            float pixelSize;

            if (controller.GetCurrentLocation(out location, out pixelSize)) {
                bool temp = false;
                PointF highlightLocation = new PointF(location.X + PIXELOFFSETX * pixelSize, location.Y + PIXELOFFSETY * pixelSize);
                SetHighlightLocation(highlightLocation, pixelSize, ref temp);
            }
        }

        public override bool CanCancel()
        {
            return false;
        }

        public override string StatusText
        {
            get {
                return StatusBarText.NewLocationForMoveAllControls;
            }
        }

        public override void MouseMoved(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return;

            PointF highlightLocation = new PointF(location.X + PIXELOFFSETX * pixelSize, location.Y + PIXELOFFSETY * pixelSize);
            highlightLocation = ConstrainLocation(highlightLocation);
            SetHighlightLocation(highlightLocation, pixelSize, ref displayUpdateNeeded);
            locationSelected(highlightLocation, false);
        }

        // Constrain the location to be on a line or on a circle. 
        private PointF ConstrainLocation(PointF highlightLocation)
        {
            if (action == MoveAllControlsAction.MoveScale) {
                // Constrain to be on a half-line.
                PointF constrain1 = new PointF(otherLocation.X + (initialLocation.X - otherLocation.X) * 0.01F, otherLocation.Y + (initialLocation.Y - otherLocation.Y) * 0.01F);
                PointF constrain2 = new PointF(otherLocation.X + (initialLocation.X - otherLocation.X) * 10000F, otherLocation.Y + (initialLocation.Y - otherLocation.Y) * 10000F);
                return Geometry.ClosestPointOnLineSegment(constrain1, constrain2, highlightLocation);
            }
            else if (action == MoveAllControlsAction.MoveRotate) {
                // Constrain to be on a circle.
                double radius = Geometry.Distance(initialLocation, otherLocation);
                double angle = Math.Atan2(highlightLocation.Y - otherLocation.Y, highlightLocation.X - otherLocation.X);
                return new PointF((float)(radius * Math.Cos(angle)) + otherLocation.X, (float)(radius * Math.Sin(angle)) + otherLocation.Y);
            }
            else {
                // No constraining
                return highlightLocation;
            }
        }

        public override IMapViewerHighlight[] GetHighlights(Pane pane)
        {
            if (pane != Pane.Map)
                return null;

            if (highlight != null)
                return new CourseObj[] { highlight };
            else
                return null;
        }

        public override MapViewer.DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane == Pane.Map) {
                // Delay to see if click or drag.
                return MapViewer.DragAction.DelayedDrag;
            }
            else {
                return MapViewer.DragAction.None;
            }
        }

        public override void LeftButtonDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Map);

            // Drag is move map.
            controller.InitiateMapDragging(locationStart, System.Windows.Forms.MouseButtons.Left);
        }

        public override void LeftButtonClick(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return;

            PointF highlightLocation = new PointF(location.X + PIXELOFFSETX * pixelSize, location.Y + PIXELOFFSETY * pixelSize);
            highlightLocation = ConstrainLocation(highlightLocation);
            locationSelected(highlightLocation, true);
        }

        // Create the highlight, and put it at the given location.
        // Set displayUpdateNeeded to true if the highlight was just created or was moved.
        void SetHighlightLocation(PointF highlightLocation, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (highlight != null && highlight.location == highlightLocation)
                return;


            switch (controlPointKind) {
                case ControlPointKind.Normal:
                    highlight = new ControlCourseObj(Id<ControlPoint>.None, Id<CourseControl>.None, courseObjRatio, appearance, null, highlightLocation);
                    break;

                case ControlPointKind.Start:
                    highlight = new StartCourseObj(Id<ControlPoint>.None, Id<CourseControl>.None, courseObjRatio, appearance, 0, highlightLocation, CrossHairOptions.HighlightCrossHair);
                    break;

                case ControlPointKind.MapExchange:
                    highlight = new StartCourseObj(Id<ControlPoint>.None, Id<CourseControl>.None, courseObjRatio, appearance, 0, highlightLocation, CrossHairOptions.HighlightCrossHair);
                    break;

                case ControlPointKind.Finish:
                    highlight = new FinishCourseObj(Id<ControlPoint>.None, Id<CourseControl>.None, courseObjRatio, appearance, null, highlightLocation, CrossHairOptions.HighlightCrossHair);
                    break;

                case ControlPointKind.None:
                    switch (specialKind) {
                        case SpecialKind.RegMark:
                            highlight = new RegMarkCourseObj(Id<Special>.None, courseObjRatio, appearance, highlightLocation);
                            break;
                        default:
                            throw new Exception("bad special kind");
                    }
                    break;

                default:
                    throw new Exception("bad control kind");
            }

            highlight.location = highlightLocation;
            displayUpdateNeeded = true;
        }
    }

    class ConfirmAllControlsMoveMode : BaseMode
    {
        public override MapViewer.DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane == Pane.Map) {
                return MapViewer.DragAction.MapDrag;
            }
            else {
                return MapViewer.DragAction.None;
            }
        }

        public override bool CanCancel()
        {
            return false;
        }
    }
}
