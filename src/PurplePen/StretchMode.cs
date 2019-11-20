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
using PurplePen.Graphics2D;

namespace PurplePen
{
    // Mode for rotating an object.
    class StretchMode: BaseMode
    {
        Controller controller;
        CrossingCourseObj courseObj;            // object to modify.
        float originalStretch;
        PointF mouseDown;

        public StretchMode(Controller controller, CrossingCourseObj courseObj)
        {
            this.controller = controller;
            this.courseObj = (CrossingCourseObj) courseObj.Clone();
            this.originalStretch = courseObj.stretch;
        }

        // Mouse cursor looks like a crosshair
        public override Cursor GetMouseCursor(Pane pane, PointF location, float pixelSize)
        {
            if (pane == Pane.Map)
                return Cursors.Cross;
            else
                return Cursors.Arrow;
        }

        public override string StatusText
        {
            get
            {
                return StatusBarText.StretchingObject;
            }
        }

        public override IMapViewerHighlight[] GetHighlights(Pane pane)
        {
            if (pane != Pane.Map)
                return null;

            return new CourseObj[1] { courseObj };
        }

        public override MapViewer.DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return MapViewer.DragAction.None;

            // Create the new start point of the drag
            mouseDown = location;
            return MapViewer.DragAction.ImmediateDrag;
        }

        public override void LeftButtonDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return;

            UpdateStretch(location);
            displayUpdateNeeded = true;
        }

        public override void LeftButtonEndDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return;

            UpdateStretch(location);
            controller.Stretch(courseObj.stretch);
            controller.DefaultCommandMode();
            displayUpdateNeeded = true;
        }

        // Change the stretch of the crossing point course object to the given distance from start drag.
        private void UpdateStretch(PointF point)
        {
            float distFromCenterOfObj = Geometry.DistanceF(courseObj.location, point);
            float distDragStartFromCenterOfObj = Geometry.DistanceF(courseObj.location, mouseDown);

            float newStretchDistance = Math.Max(0, originalStretch + (distFromCenterOfObj - distDragStartFromCenterOfObj));

            courseObj = new CrossingCourseObj(courseObj.controlId, courseObj.courseControlId, courseObj.specialId,
                                              courseObj.courseObjRatio, courseObj.appearance, courseObj.orientation,
                                              newStretchDistance, courseObj.location);
        }
    }
}
