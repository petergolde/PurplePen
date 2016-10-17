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
using System.Diagnostics;

using PurplePen.MapView;
using PurplePen.MapModel;
using PurplePen.Graphics2D;
using System.Text;
using System.Linq;

namespace PurplePen
{
    // The base mode is a base class with default behavior.
    class BaseMode: ICommandMode
    {
        public virtual void BeginMode()
        { }

        public virtual void EndMode()
        { }

        public virtual bool CanCancel()
        {
            return true;
        }

        // Get the highlights to display.
        public virtual IMapViewerHighlight[] GetHighlights(Pane pane)
        {
            return null;
        }

        // Mouse cursor looks like a move cursor when hovering over something that is selected.
        public virtual Cursor GetMouseCursor(Pane pane, PointF location, float pixelSize)
        {
            return Cursors.Default;
        }

        public virtual string StatusText
        {
            get { return ""; }
        }

        public virtual void MouseMoved(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        { }

        public virtual MapViewer.DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            return MapViewer.DragAction.None;
        }

        // By default, the right mouse button drags the map.
        public virtual MapViewer.DragAction RightButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane == Pane.Map) {
                return MapViewer.DragAction.MapDrag;
            }
            else {
                return MapViewer.DragAction.None;
            }
        }

        public virtual void LeftButtonUp(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        { }

        public virtual void RightButtonUp(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        { }

        public virtual void LeftButtonClick(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        { }

        public virtual void RightButtonClick(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        { }

        public virtual void LeftButtonDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        { }

        public virtual void RightButtonDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        { }

        public virtual void LeftButtonEndDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        { }

        public virtual void RightButtonEndDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        { }

        public virtual void LeftButtonCancelDrag(Pane pane, ref bool displayUpdateNeeded)
        { }

        public virtual void RightButtonCancelDrag(Pane pane, ref bool displayUpdateNeeded)
        { }

        public virtual bool GetToolTip(Pane pane, PointF location, float pixelSize, out string tipText, out string titleText)
        {
            tipText = titleText = "";
            return false;
        }

        protected void ConvertTextPartsToToolTip(TextPart[] textParts, out string tipText, out string tipTitle)
        {
            StringBuilder tipBuilder = new StringBuilder();
            StringBuilder titleBuilder = new StringBuilder();

            foreach (TextPart part in textParts) {
                switch (part.format) {
                    case TextFormat.NewLine:
                        if (tipBuilder.Length > 0)
                            tipBuilder.AppendLine();
                        tipBuilder.Append(part.text);
                        break;

                    case TextFormat.SameLine:
                        tipBuilder.Append(part.text);
                        break;

                    case TextFormat.Title:
                        titleBuilder.Append(part.text);
                        break;

                    case TextFormat.Header:
                        if (tipBuilder.Length > 0)
                            tipBuilder.AppendLine();
                        tipBuilder.Append(part.text);
                        tipBuilder.Append(" ");
                        break;

                    default:
                        Debug.Fail("Unexpected part format");
                        break;
                }
            }

            tipText = tipBuilder.ToString();
            tipTitle = titleBuilder.ToString();
        }

    }


    // The default mode is the standard default behavior.
    // Clicking an object selects it. Hovering over a selected objects
    // gives a move cursor if it is moveable. MouseDown over a selected 
    // draggable objects initiated a drag to move it.
    class DefaultMode: BaseMode
    {
        Controller controller;
        EventDB eventDB;
        SymbolDB symbolDB;
        SelectionMgr selectionMgr;

        public DefaultMode(Controller controller, EventDB eventDB, SymbolDB symbolDB, SelectionMgr selectionMgr)
        {
            this.controller = controller;
            this.eventDB = eventDB;
            this.symbolDB = symbolDB;
            this.selectionMgr = selectionMgr;
        }

        public override bool CanCancel()
        {
            // The default mode is not cancelable.
 	         return false;
        }

        // Get the highlights to display.
        public override IMapViewerHighlight[] GetHighlights(Pane pane)
        {
            // No command in progress. Just return the active selected objects.
            CourseObj[] selectedObjects;

            if (pane == Pane.Map)
                selectedObjects = selectionMgr.SelectedCourseObjects;
            else
                selectedObjects = selectionMgr.SelectedTopologyObjects;

            if (selectedObjects == null)
                return null;

            return (IMapViewerHighlight[])selectedObjects;
        }

        public override string StatusText
        {
            get
            {
                PointF location, dummy;
                Cursor handleCursor;
                float pixelSize;
                bool onMap = controller.GetCurrentLocation(out location, out pixelSize);
                CourseObj courseObj;

                if (onMap && (courseObj = HitTestHandle(location, pixelSize, out dummy, out handleCursor)) != null) {
                    if (courseObj is RectCourseObj || courseObj is BasicTextCourseObj)
                        return StatusBarText.SizeRectangle;
                    else
                        return StatusBarText.DragCorner;
                }
                else if (onMap && HitTestDraggable(location, pixelSize) != null)
                    return StatusBarText.DragObject;
                else
                    return StatusBarText.DefaultStatus;
            }
        }

        // Is this course object draggable?
        bool DraggableObject(CourseObj courseObject)
        {
            // Legs are not draggable.
            if (courseObject == null || (courseObject is LegCourseObj) || (courseObject is FlaggedLegCourseObj))
                return false;

            // Everything else is draggable.
            return true;
        }

        // Is this course object selectable?
        bool SelectableObject(CourseObj courseObject)
        {
            // All course objects are selectable.
            return courseObject != null;
        }

        // Hit test a location to see if it is over a handle of a selected object.
        CourseObj HitTestHandle(PointF location, float pixelSize, out PointF handleLocation, out Cursor handleCursor)
        {
            CourseObj[] selectedObjects = selectionMgr.SelectedCourseObjects;

            if (selectedObjects != null) {
                foreach (CourseObj courseObj in selectionMgr.SelectedCourseObjects) {
                    PointF[] handles = courseObj.GetHandles();
                    if (handles != null) {
                        foreach (PointF handle in handles) {
                            double distance = Geometry.Distance(location, handle);
                            if (distance / pixelSize <= 3.0) {
                                // over a handle.
                                handleLocation = handle;
                                handleCursor = courseObj.GetHandleCursor(handle);
                                return courseObj;
                            }
                        }
                    }
                }
            }

            // didn't find a handle.
            handleLocation = new PointF();
            handleCursor = null;
            return null;
        }

        // Hit test a location to see if it is over a selected, draggable objects. If so,
        // return that course object, otherwise, return null.
        CourseObj HitTestDraggable(PointF location, float pixelSize)
        {
            CourseObj[] selectedObjects = selectionMgr.SelectedCourseObjects;

            if (selectedObjects != null) {
                // If the cursor is above a selected control, start, finish that is a moveable object.
                CourseObj hitObject = CourseLayout.HitTestCollection(selectedObjects, location, pixelSize, CourseLayer.All, null);
                if (hitObject != null && DraggableObject(hitObject))
                    return hitObject;
            }

            return null;
        }

        // Mouse cursor looks like a move cursor when hovering over something that is selected.
        public override Cursor GetMouseCursor(Pane pane, PointF location, float pixelSize)
        {
            if (pane == Pane.Map) {
                PointF dummy;
                Cursor handleCursor;

                if (HitTestHandle(location, pixelSize, out dummy, out handleCursor) != null)
                    return handleCursor;
                if (HitTestDraggable(location, pixelSize) != null)
                    return Cursors.SizeAll;
                else
                    return Cursors.Default;
            }
            else {
                return Cursors.Default;
            }
        }

        public override void LeftButtonClick(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            CourseObj clickedObject = HitTest(pane, location, pixelSize);
            if (clickedObject != null) {
                selectionMgr.SelectCourseObject(clickedObject);
            }
            else {
                if (pane == Pane.Map) {
                    // clicked on nothing. Clear selection.
                    controller.ClearSelection();
                }
            }
        }

        public override void LeftButtonDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane == Pane.Map) {
                // If we dragged an object or corner, we would have entered a new mode. So this must be a delayed drag that should
                // become map dragging.
                controller.InitiateMapDragging(locationStart, System.Windows.Forms.MouseButtons.Left);
            }
            else if (pane == Pane.Topology) {
                CourseObj clickedObject = HitTest(pane, locationStart, pixelSize);
                TopologyDragControlMode commandMode = new TopologyDragControlMode(controller, eventDB, selectionMgr, clickedObject, locationStart, location);
                controller.SetCommandMode(commandMode);
            }
        }

        // Left mouse button selects the object clicked on, or drag something already selected.
        public override MapViewer.DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane == Pane.Map) {
                CourseLayout activeCourse = controller.GetCourseLayout();
                CourseObj clickedObject;
                PointF handleLocation;
                Cursor handleCursor;

                // Area we initiating a drag of a corner?
                clickedObject = HitTestHandle(location, pixelSize, out handleLocation, out handleCursor);
                if (clickedObject != null) {
                    // being dragging the corner
                    DragHandleMode commandMode = new DragHandleMode(controller, clickedObject, handleLocation, location);
                    controller.SetCommandMode(commandMode);
                    displayUpdateNeeded = true;
                    return MapViewer.DragAction.ImmediateDrag;
                }

                // Are we initiating a drag of an object?
                clickedObject = HitTestDraggable(location, pixelSize);
                if (clickedObject != null) {
                    // Begin dragging the clicked object.
                    DragObjectMode commandMode = new DragObjectMode(controller, eventDB, selectionMgr, clickedObject, location);
                    controller.SetCommandMode(commandMode);
                    displayUpdateNeeded = true;
                    return MapViewer.DragAction.ImmediateDrag;
                }

                return MapViewer.DragAction.DelayedDrag;
            }
            else if (pane == Pane.Topology) {
                CourseObj clickedObject = HitTest(pane, location, pixelSize);
                if (clickedObject is ControlNumberCourseObj || clickedObject is CrossingCourseObj ||
                    (clickedObject is StartCourseObj && (eventDB.GetControl(((StartCourseObj)clickedObject).controlId).kind == ControlPointKind.MapExchange))) 
                {
                    // Can drag control numbers, crossing points, or map exchanges.
                    selectionMgr.SelectCourseObject(clickedObject);
                    displayUpdateNeeded = true;
                    return MapViewer.DragAction.DelayedDrag;
                }
            }

            return MapViewer.DragAction.None;
        }

        private CourseObj HitTest(Pane pane, PointF location, float pixelSize)
        {
            CourseLayout activeCourse = (pane == Pane.Map) ? controller.GetCourseLayout() : controller.GetTopologyLayout();
            CourseObj clickedObject;

            clickedObject = activeCourse.HitTest(location, pixelSize, CourseLayer.MainCourse, null);
            if (clickedObject == null)
                clickedObject = activeCourse.HitTest(location, pixelSize, CourseLayer.Descriptions, null);

            return clickedObject;
        }

        public override bool GetToolTip(Pane pane, PointF location, float pixelSize, out string tipText, out string titleText)
        {
            CourseLayout activeCourse;
            CourseView courseView = selectionMgr.ActiveCourseView;

            if (pane == Pane.Map) {
                activeCourse = controller.GetCourseLayout();
            }
            else {
                activeCourse = controller.GetTopologyLayout();
            }

            CourseObj touchedObject = activeCourse.HitTest(location, pixelSize, CourseLayer.MainCourse, null);

            if (touchedObject == null)
                touchedObject = activeCourse.HitTest(location, pixelSize, CourseLayer.Descriptions, null);

            if (touchedObject != null) {
                TextPart[] textParts = SelectionDescriber.DescribeCourseObject(symbolDB, eventDB, touchedObject, courseView.ScaleRatio);
                ConvertTextPartsToToolTip(textParts, out tipText, out titleText);
                return true;
            }
            else {
                tipText = titleText = "";
                return false;
            }
        }
    }

    // Mode when an object is being dragged to a new position.
    class DragObjectMode: BaseMode
    {
        Controller controller;
        EventDB eventDB;
        SelectionMgr selectionMgr;
        CourseObj courseObjectStart, courseObjectDrag;
        PointF startDrag, currentLocation;

        CourseObj[] additionalHighlights;  // additional highlights to show also, for legs to/from control.

        public DragObjectMode(Controller controller, EventDB eventDB, SelectionMgr selectionMgr, CourseObj courseObject, PointF startDrag)
        {
            this.controller = controller;
            this.eventDB = eventDB;
            this.selectionMgr = selectionMgr;
            this.courseObjectStart = courseObject;
            this.courseObjectDrag = (CourseObj) (courseObject.Clone());
            this.startDrag = this.currentLocation = startDrag;
        }

        public override IMapViewerHighlight[] GetHighlights(Pane pane)
        {
            if (pane != Pane.Map)
                return null;

            if (additionalHighlights != null && additionalHighlights.Length > 0) {
                CourseObj[] highlights = new CourseObj[additionalHighlights.Length + 1];
                highlights[0] = courseObjectDrag;
                Array.Copy(additionalHighlights, 0, highlights, 1, additionalHighlights.Length);
                return highlights;
            }
            else {
                return new CourseObj[] { courseObjectDrag };
            }
        }

        public override string StatusText
        {
            get
            {
                return StatusBarText.DraggingObject;
            }
        }

        public override MapViewer.DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane == Pane.Map) {
                return MapViewer.DragAction.ImmediateDrag;
            }
            else {
                return MapViewer.DragAction.None;
            }
        }

        public override void LeftButtonDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Map);

            currentLocation = location;

            // Update the highlight.
            courseObjectDrag = ((CourseObj) courseObjectStart.Clone());
            courseObjectDrag.Offset(location.X - startDrag.X, location.Y - startDrag.Y);
            
            // If we're dragging a control in a course (not all controls) then add additional highlights for the leg(s) to/from the control.
            if (AreDraggingControlPoint() && courseObjectStart.courseControlId.IsNotNone) {
                ControlPoint control = eventDB.GetControl(courseObjectStart.controlId);
                CourseView courseView = selectionMgr.ActiveCourseView;

                // Find index of this course control in the course view.
                int index;
                for (index = 0; index < courseView.ControlViews.Count; ++index) {
                    if (courseView.ControlViews[index].courseControlIds.Contains(courseObjectStart.courseControlId))
                        break;
                }

                if (index < courseView.ControlViews.Count) {
                    // Get previous and next controls.
                    int prevIndex = courseView.GetPrevControl(index), nextIndex = courseView.GetNextControl(index);
                    Id<CourseControl> prevCourseControl = (prevIndex >= 0) ? courseView.ControlViews[prevIndex].courseControlIds[0] : Id<CourseControl>.None;
                    Id<CourseControl> nextCourseControl = (nextIndex >= 0) ? courseView.ControlViews[nextIndex].courseControlIds[0] : Id<CourseControl>.None;

                    // Get additional highlights to and from those controls.
                    additionalHighlights = AddControlMode.CreateLegHighlights(eventDB, ((PointCourseObj) courseObjectDrag).location, courseObjectDrag.controlId, control.kind, prevCourseControl, nextCourseControl, courseView.ScaleRatio, courseObjectStart.appearance);

                    // If we're dragging the start, update the angle of the start appropriately.
                    if ((control.kind == ControlPointKind.Start || control.kind == ControlPointKind.MapExchange) && additionalHighlights.Length > 0) {
                        SymPath pathFromStart = ((LineCourseObj) additionalHighlights[additionalHighlights.Length - 1]).path;
                        PointF[] pts = pathFromStart.FlattenedPoints;
                        double angleOut = Math.Atan2(pts[1].Y - pts[0].Y, pts[1].X - pts[0].X);
                        if (!double.IsNaN(angleOut)) 
                            ((StartCourseObj) courseObjectDrag).orientation = (float) Geometry.RadiansToDegrees(angleOut);
                    }
                }                                                             
            }
            
            displayUpdateNeeded = true;
        }

        // Are we dragging a control point?
        bool AreDraggingControlPoint()
        {
            return courseObjectStart.specialId.IsNone && !((courseObjectStart is ControlNumberCourseObj) || (courseObjectStart is CodeCourseObj));
        }

        public override void LeftButtonEndDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Map);

            float deltaX = (location.X - startDrag.X);
            float deltaY = (location.Y - startDrag.Y);

            if (courseObjectStart.specialId.IsNotNone) {
                // Moving a special
                Id<Special> specialId = courseObjectStart.specialId;

                controller.MoveSpecialDelta(specialId, deltaX, deltaY);
            }
            else if ((courseObjectStart is ControlNumberCourseObj) || (courseObjectStart is CodeCourseObj)) {
                // Dragging a number around. Update the course control with a new number.
                PointF originalLocation = (courseObjectStart is ControlNumberCourseObj) ? ((ControlNumberCourseObj)courseObjectStart).centerPoint : ((CodeCourseObj)courseObjectStart).centerPoint;
                PointF newLocation = PointF.Add(originalLocation, new SizeF(deltaX, deltaY));

                controller.MoveControlNumber(courseObjectStart.controlId, courseObjectStart.courseControlId, newLocation);
            }
            else {
                // Move the control to the new location.
                Id<ControlPoint> controlId = courseObjectStart.controlId;
                PointF originalLocation = ((PointCourseObj) courseObjectStart).location;
                PointF newLocation = PointF.Add(originalLocation, new SizeF(deltaX,deltaY));

                controller.MoveControlInCurrentCourse(controlId, newLocation);
            }
            controller.DefaultCommandMode();
        }

        public override void LeftButtonCancelDrag(Pane pane, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Map);

            // Drag was cancelled. Go back to normal mode.
            controller.DefaultCommandMode();
        }

        public override Cursor GetMouseCursor(Pane pane, PointF location, float pixelSize)
        {
            if (pane == Pane.Map) {
                return Cursors.SizeAll;
            }
            else {
                return Cursors.Arrow;
            }
        }
    }


    // Mode when an handle on a line/area/leg is being dragged to a new position.
    class DragHandleMode: BaseMode
    {
        Controller controller;
        CourseObj courseObjectStart, courseObjectDrag;
        PointF handleLocation, startDrag, currentLocation;
        Cursor handleCursor;

        public DragHandleMode(Controller controller, CourseObj courseObject, PointF handleLocation, PointF startDrag)
        {
            this.controller = controller;
            this.courseObjectStart = courseObject;
            this.courseObjectDrag = (CourseObj) (courseObject.Clone());
            this.handleLocation = handleLocation;
            this.handleCursor = courseObject.GetHandleCursor(handleLocation);
            this.startDrag = this.currentLocation = startDrag;
        }

        public override IMapViewerHighlight[] GetHighlights(Pane pane)
        {
            Debug.Assert(pane == Pane.Map);

            return new CourseObj[] { courseObjectDrag };
        }

        public override string StatusText
        {
            get
            {
                if (courseObjectDrag is RectCourseObj || courseObjectDrag is BasicTextCourseObj)
                    return StatusBarText.SizingRectangle;
                else
                    return StatusBarText.DraggingCorner;
            }
        }

        public override MapViewer.DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Map);

            return MapViewer.DragAction.ImmediateDrag;
        }

        public override void LeftButtonDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Map);

            currentLocation = location;

            // Update the highlight.
            courseObjectDrag = ((CourseObj) courseObjectStart.Clone());
            PointF newHandleLocation = new PointF(handleLocation.X + location.X - startDrag.X, handleLocation.Y + location.Y - startDrag.Y);
            courseObjectDrag.MoveHandle(handleLocation, newHandleLocation);

            displayUpdateNeeded = true;
        }

        public override void LeftButtonEndDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Map);

            float deltaX = (location.X - startDrag.X);
            float deltaY = (location.Y - startDrag.Y);
            PointF newHandleLocation = new PointF(handleLocation.X + deltaX, handleLocation.Y + deltaY);

            if (courseObjectStart.specialId.IsNotNone) {
                // Moving a corner of a special
                Id<Special> specialId = courseObjectStart.specialId;

                if (courseObjectStart is DescriptionCourseObj) {
                    // Moving a description. Descriptions are rather special in the way their locations are used.
                    DescriptionCourseObj descObj = (DescriptionCourseObj) courseObjectStart.Clone();
                    descObj.MoveHandle(handleLocation, location);
                    controller.MoveSpecial(specialId, new PointF[2] { new PointF(descObj.rect.Left, descObj.rect.Bottom), new PointF(descObj.rect.Left + descObj.CellSize, descObj.rect.Bottom) }, descObj.NumberOfColumns);
                }
                else if (courseObjectStart is RectCourseObj) {
                    // Moving rectangle handles is sort of special too.
                    RectCourseObj rectObj = (RectCourseObj)courseObjectStart.Clone();
                    rectObj.MoveHandle(handleLocation, location);
                    RectangleF rect = rectObj.GetHighlightBounds();
                    controller.MoveSpecial(specialId, new PointF[2] { new PointF(rect.Left, rect.Bottom), new PointF(rect.Right, rect.Top) });
                }
                else if (courseObjectStart is BasicTextCourseObj) {
                    // Moving text handles is sort of special too.
                    BasicTextCourseObj textObj = (BasicTextCourseObj)courseObjectStart.Clone();
                    textObj.MoveHandle(handleLocation, location);
                    RectangleF rect = textObj.GetHighlightBounds();
                    controller.MoveSpecial(specialId, new PointF[2] { new PointF(rect.Left, rect.Bottom), new PointF(rect.Right, rect.Top) });
                }
                else {
                    controller.MoveSpecialPoint(specialId, handleLocation, newHandleLocation);
                }
            }
            else if ((courseObjectStart is LegCourseObj) || (courseObjectStart is FlaggedLegCourseObj)) {
                // Moving a leg bend.
                LineCourseObj lineCourseObj = (LineCourseObj) courseObjectStart;

                controller.MoveLegBendOrGap(lineCourseObj.courseControlId, lineCourseObj.courseControlId2, handleLocation, newHandleLocation);
            }
            else if ((courseObjectStart is ControlCourseObj) || (courseObjectStart is FinishCourseObj)) {
                PointCourseObj pointObj = (PointCourseObj)courseObjectStart.Clone();
                pointObj.MoveHandle(handleLocation, location);
                controller.MoveControlGap(courseObjectStart.controlId, pointObj.movableGaps);
            }
            else {
                Debug.Fail("unknown situation");
            }

            controller.DefaultCommandMode();
        }

        public override void LeftButtonCancelDrag(Pane pane, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Map);

            // Drag was cancelled. Go back to normal mode.
            controller.DefaultCommandMode();
        }

        public override Cursor GetMouseCursor(Pane pane, PointF location, float pixelSize)
        {
            if (pane == Pane.Map) {
                return handleCursor;
            }
            else {
                return Cursors.Arrow;
            }
        }
    }

}
