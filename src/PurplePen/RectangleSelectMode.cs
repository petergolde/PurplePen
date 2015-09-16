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

namespace PurplePen
{
    // This mode is for modifying a rectangle object on the map. You can drag the whole rectangle
    // or handles on the edges. The mode must be exited forceable -- it has not UI for exiting itself.
    class RectangleSelectMode: BaseMode
    {
        Controller controller;
        RectangleF originalRectangle;
        SelectingRectangleCourseObj selectingCourseObj;
        IDisposable disposeOnEndMode;        // dispose this object when mode is done.

        // Is dragging the rectangle or a handle on the rectangle in progress?
        bool draggingWhole;       // dragging the whole rectangle?
        bool draggingHandle;      // dragging a handle on the rectangle?
        PointF startDrag;             // location dragging started
        SelectingRectangleCourseObj dragStartCourseObj;  // course object when dragging began
        PointF handleDrag;         // if draggingHandle is true, the exact handle location being dragged.
        Cursor cursorDrag;         // if draggingHandle is true, the cursor used for the drag.

        bool allowDrag = true;    // Allow dragging the entire size.
        bool allowResize = true;  // Allow resized with a handle.

        public RectangleSelectMode(Controller controller, RectangleF rect, IDisposable disposeOnEndMode)
        {
            this.controller = controller;
            originalRectangle = rect;
            this.disposeOnEndMode = disposeOnEndMode;
            selectingCourseObj = new SelectingRectangleCourseObj(rect);
        }

        // The rectangle that is current visible.
        public RectangleF Rectangle
        {
            get { return selectingCourseObj.rect; }
            set {
                selectingCourseObj = (SelectingRectangleCourseObj) selectingCourseObj.Clone();
                selectingCourseObj.rect = value;
            }
        }

        public bool AllowDragging
        {
            get { return allowDrag; }
            set {
                if (allowDrag != value) {
                    allowDrag = value;
                }
            }
        }

        public bool AllowResize
        {
            get { return allowResize; }
            set
            {
                if (allowResize != value) {
                    allowResize = value;
                    selectingCourseObj = (SelectingRectangleCourseObj)selectingCourseObj.Clone();
                    selectingCourseObj.showHandles = allowResize;
                }
            }
        }

        public override bool CanCancel()
        {
            return true;
        }

        public override void EndMode()
        {
            if (disposeOnEndMode != null)
                disposeOnEndMode.Dispose();
        }

        // Get the highlights to display.
        public override IMapViewerHighlight[] GetHighlights()
        {
            return new IMapViewerHighlight[] { selectingCourseObj };
        }

        public override string StatusText
        {
            get
            {
                PointF location, dummy;
                Cursor handleCursor;
                float pixelSize;
                bool onMap = controller.GetCurrentLocation(out location, out pixelSize);

                if (draggingWhole)
                    return StatusBarText.DraggingObject;
                else if (draggingHandle)
                    return StatusBarText.SizingRectangle;
                else if (onMap && HitTestHandle(location, pixelSize, out dummy, out handleCursor)) {
                    return StatusBarText.SizeRectangle;
                }
                else if (onMap && HitTestDraggable(location, pixelSize))
                    return StatusBarText.DragObject;
                else
                    return StatusBarText.DefaultRectangle;
            }
        }

        // Hit test a location to see if it is over a handle. Return the handleLocation and handleCursor.
        bool HitTestHandle(PointF location, float pixelSize, out PointF handleLocation, out Cursor handleCursor)
        {
            PointF[] handles = selectingCourseObj.GetHandles();
            foreach (PointF handle in handles) {
                double distance = Geometry.Distance(location, handle);
                if (distance / pixelSize <= 3.0) {
                    // over a handle.
                    handleLocation = handle;
                    handleCursor = selectingCourseObj.GetHandleCursor(handle);
                    return true;
                }
            }

            // didn't find a handle.
            handleLocation = new PointF();
            handleCursor = null;
            return false;
        }

        // Hit test a location to see if it is over the draggable object.
        bool HitTestDraggable(PointF location, float pixelSize)
        {
            if (allowDrag && selectingCourseObj.DistanceFromPoint(location) < pixelSize * 3)
                return true;
            else
                return false;
        }

        // Mouse cursor looks like a move cursor when hovering over something that is selected.
        public override Cursor GetMouseCursor(PointF location, float pixelSize)
        {
            PointF dummy;
            Cursor handleCursor;

            Debug.WriteLine("allowdrag = {0}", allowDrag);

            if (draggingWhole) {
                Debug.WriteLine("dragging whole");
                return Cursors.SizeAll;
            }
            else if (draggingHandle) {
                Debug.WriteLine("dragging handle");
                return cursorDrag;
            }
            else if (HitTestHandle(location, pixelSize, out dummy, out handleCursor)) {
                Debug.WriteLine("over handle");
                return handleCursor;
            }
            else if (HitTestDraggable(location, pixelSize)) {
                Debug.WriteLine("draggable");
                return Cursors.SizeAll;
            }
            else {
                Debug.WriteLine("Not over anything");
                return Cursors.Default;
            }
        }

        // Left mouse button selects the object clicked on, or drag something already selected.
        public override MapViewer.DragAction LeftButtonDown(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            PointF handleLocation;

            // Area we initiating a drag of a corner/side?
            if (HitTestHandle(location, pixelSize, out handleLocation, out cursorDrag)) {
                // being dragging the corner
                draggingHandle = true;
                startDrag = location;
                handleDrag = handleLocation;
                dragStartCourseObj = (SelectingRectangleCourseObj) selectingCourseObj.Clone();
                displayUpdateNeeded = true;
                return MapViewer.DragAction.ImmediateDrag;
            }

            // Are we initiating a drag of the whole object?
            if (HitTestDraggable(location, pixelSize)) {
                // Begin dragging the clicked object.
                draggingWhole = true;
                startDrag = location;
                dragStartCourseObj = (SelectingRectangleCourseObj) selectingCourseObj.Clone();
                displayUpdateNeeded = true;
                return MapViewer.DragAction.ImmediateDrag;
            }

            return MapViewer.DragAction.None;
        }

        public override void LeftButtonDrag(PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (draggingWhole) {
                // Update the rectangle being dragged.
                selectingCourseObj = (SelectingRectangleCourseObj) dragStartCourseObj.Clone();
                selectingCourseObj.Offset(location.X - startDrag.X, location.Y - startDrag.Y);
            }
            else if (draggingHandle) {
                // Update the rectangle where the handle is being dragged.
                selectingCourseObj = (SelectingRectangleCourseObj) dragStartCourseObj.Clone();
                PointF newHandleLocation = new PointF(handleDrag.X + location.X - startDrag.X, handleDrag.Y + location.Y - startDrag.Y);
                selectingCourseObj.MoveHandle(handleDrag, newHandleLocation);
            }

            displayUpdateNeeded = true;
        }

        public override void LeftButtonEndDrag(PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            LeftButtonDrag(location, locationStart, pixelSize, ref displayUpdateNeeded);
            draggingWhole = draggingHandle = false;
        }
    }

    // CourseObj used to display the selecting rectangle. 
    class SelectingRectangleCourseObj: RectCourseObj
    {
        public bool showHandles = true;               // Should drag handles be shown?

        public SelectingRectangleCourseObj(RectangleF rect) :
            base(Id<ControlPoint>.None, Id<CourseControl>.None, Id<Special>.None, 1.0F, new CourseAppearance(), rect)
        {}

        public override PointF[] GetHandles()
        {
            if (showHandles)
                return base.GetHandles();
            else
                return new PointF[0];
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }

            SelectingRectangleCourseObj other = (SelectingRectangleCourseObj)obj;

            if (showHandles != other.showHandles)
                return false;

            return base.Equals(obj);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            throw new NotSupportedException("The method or operation is not supported.");
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            throw new NotSupportedException ("never needed, so intentionally unimplemented"); 
        }

        protected override void AddToMap(Map map, SymDef symdef)
        {
            throw new NotSupportedException("never needed, so intentionally unimplemented");
        }
    }
/*
    // Mode when an object is being dragged to a new position.
    class DragObjectMode: BaseMode
    {
        Controller controller;
        CourseObj courseObjectStart, courseObjectDrag;
        PointF startDrag, currentLocation;

        public DragObjectMode(Controller controller, CourseObj courseObject, PointF startDrag)
        {
            this.controller = controller;
            this.courseObjectStart = courseObject;
            this.courseObjectDrag = (CourseObj) (courseObject.Clone());
            this.startDrag = this.currentLocation = startDrag;
        }

        public override IMapViewerHighlight[] GetHighlights()
        {
            return new CourseObj[] { courseObjectDrag };
        }

        public override string StatusText
        {
            get
            {
                return StatusStrings.DraggingObject;
            }
        }

        public override MapViewer.DragAction LeftButtonDown(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            return MapViewer.DragAction.ImmediateDrag;
        }

        public override void LeftButtonDrag(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            currentLocation = location;

            // Update the highlight.
            courseObjectDrag = ((CourseObj) courseObjectStart.Clone());
            courseObjectDrag.Offset(location.X - startDrag.X, location.Y - startDrag.Y);

            displayUpdateNeeded = true;
        }

        public override void LeftButtonEndDrag(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            float deltaX = (location.X - startDrag.X);
            float deltaY = (location.Y - startDrag.Y);

            if (courseObjectStart.specialId.IsNotNone) {
                // Moving a special
                Id<Special> specialId = courseObjectStart.specialId;

                controller.MoveSpecialDelta(specialId, deltaX, deltaY);
            }
            else if ((courseObjectStart is ControlNumberCourseObj) || (courseObjectStart is CodeCourseObj)) {
                // Dragging a number around. Update the course control with a new number.
                PointF originalLocation = (courseObjectStart is ControlNumberCourseObj) ? ((ControlNumberCourseObj) courseObjectStart).centerPoint : ((CodeCourseObj) courseObjectStart).centerPoint;
                PointF newLocation = PointF.Add(originalLocation, new SizeF(deltaX, deltaY));

                controller.MoveControlNumber(courseObjectStart.courseControlId, newLocation);
            }
            else {
                // Move the control to the new location.
                Id<ControlPoint> controlId = courseObjectStart.controlId;
                PointF originalLocation = ((PointCourseObj) courseObjectStart).location;
                PointF newLocation = PointF.Add(originalLocation, new SizeF(deltaX, deltaY));

                controller.MoveControl(controlId, newLocation);
            }
            controller.DefaultCommandMode();
        }

        public override void LeftButtonCancelDrag(ref bool displayUpdateNeeded)
        {
            // Drag was cancelled. Go back to normal mode.
            controller.DefaultCommandMode();
        }

        public override Cursor GetMouseCursor(PointF location, float pixelSize)
        {
            return Cursors.SizeAll;
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

        public override IMapViewerHighlight[] GetHighlights()
        {
            return new CourseObj[] { courseObjectDrag };
        }

        public override string StatusText
        {
            get
            {
                if (courseObjectDrag is RectCourseObj)
                    return StatusStrings.SizingRectangle;
                else
                    return StatusStrings.DraggingCorner;
            }
        }

        public override MapViewer.DragAction LeftButtonDown(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            return MapViewer.DragAction.ImmediateDrag;
        }

        public override void LeftButtonDrag(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            currentLocation = location;

            // Update the highlight.
            courseObjectDrag = ((CourseObj) courseObjectStart.Clone());
            PointF newHandleLocation = new PointF(handleLocation.X + location.X - startDrag.X, handleLocation.Y + location.Y - startDrag.Y);
            courseObjectDrag.MoveHandle(handleLocation, newHandleLocation);

            displayUpdateNeeded = true;
        }

        public override void LeftButtonEndDrag(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
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
                    controller.MoveSpecial(specialId, new PointF[2] { new PointF(descObj.rect.Left, descObj.rect.Bottom), new PointF(descObj.rect.Left + descObj.CellSize, descObj.rect.Bottom) });
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
            else {
                Debug.Fail("unknown situation");
            }

            controller.DefaultCommandMode();
        }

        public override void LeftButtonCancelDrag(ref bool displayUpdateNeeded)
        {
            // Drag was cancelled. Go back to normal mode.
            controller.DefaultCommandMode();
        }

        public override Cursor GetMouseCursor(PointF location, float pixelSize)
        {
            return handleCursor;
        }
    }
 */

}
