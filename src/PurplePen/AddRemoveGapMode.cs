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
    // Mode for adding a gap to a control.
    class AddControlGapMode: BaseMode
    {
        Controller controller;
        PointCourseObj courseObj;            // object to modify.

        public AddControlGapMode(Controller controller, PointCourseObj courseObj)
        {
            this.controller = controller;
            this.courseObj = (PointCourseObj) courseObj.Clone();
        }

        // Mouse cursor looks like a crosshair
        public override Cursor GetMouseCursor(PointF location, float pixelSize)
        {
            return Cursors.Cross;
        }

        public override string StatusText
        {
            get
            {
                return StatusBarText.AddingControlGap;
            }
        }

        public override IMapViewerHighlight[] GetHighlights()
        {
            return new CourseObj[1] { courseObj };
        }

        public override MapViewer.DragAction LeftButtonDown(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            // Create the new corner
            controller.AddControlGap(location);
            controller.DefaultCommandMode();
            return MapViewer.DragAction.None;
        }
    }

    // Mode for remove a gap from a control.
    class RemoveControlGapMode: BaseMode
    {
        Controller controller;
        PointCourseObj courseObj;            // object to modify.

        public RemoveControlGapMode(Controller controller, PointCourseObj courseObj)
        {
            this.controller = controller;
            this.courseObj = (PointCourseObj) courseObj.Clone();
        }

        // Mouse cursor looks like a crosshair
        public override Cursor GetMouseCursor(PointF location, float pixelSize)
        {
            return Cursors.Cross;
        }

        public override string StatusText
        {
            get
            {
                return StatusBarText.RemovingControlGap;
            }
        }

        public override IMapViewerHighlight[] GetHighlights()
        {
            return new CourseObj[1] { courseObj };
        }

        public override MapViewer.DragAction LeftButtonDown(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            // Create the new corner
            controller.RemoveControlGap(location);
            controller.DefaultCommandMode();
            return MapViewer.DragAction.None;
        }
    }

    // Mode for remove a gap from a leg.
    class RemoveLegGapMode: BaseMode
    {
        Controller controller;
        LineCourseObj courseObj;            // object to modify.

        public RemoveLegGapMode(Controller controller, LineCourseObj courseObj)
        {
            this.controller = controller;
            this.courseObj = (LineCourseObj) courseObj.Clone();
        }

        // Mouse cursor looks like a crosshair
        public override Cursor GetMouseCursor(PointF location, float pixelSize)
        {
            return Cursors.Cross;
        }

        public override string StatusText
        {
            get
            {
                return StatusBarText.RemovingLegGap;
            }
        }

        public override IMapViewerHighlight[] GetHighlights()
        {
            return new CourseObj[1] { courseObj };
        }

        public override MapViewer.DragAction LeftButtonDown(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            // Remove the gap
            controller.RemoveLegGap(location);
            controller.DefaultCommandMode();
            return MapViewer.DragAction.None;
        }
    }

    // Mode when an handle on a line/area/leg is being dragged to a new position.
    class AddLegGapMode: BaseMode
    {
        Controller controller;
        LegCourseObj courseObjStart;            // object to modify.
        LegCourseObj courseObjDrag;            // current highlight, possibly with gap being dragged out.

        PointF startDrag;

        public AddLegGapMode(Controller controller, LegCourseObj courseObject)
        {
            this.controller = controller;
            this.courseObjStart = courseObject;
            this.courseObjDrag = (LegCourseObj) courseObject.Clone();
        }

        public override IMapViewerHighlight[] GetHighlights()
        {
            return new CourseObj[] { courseObjDrag };
        }

        // Mouse cursor looks like a crosshair
        public override Cursor GetMouseCursor(PointF location, float pixelSize)
        {
            return Cursors.Cross;
        }

        public override string StatusText
        {
            get
            {
                return StatusBarText.AddingLegGap;
            }
        }

        public override MapViewer.DragAction LeftButtonDown(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            startDrag = location;
            return MapViewer.DragAction.ImmediateDrag;
        }

        public override void LeftButtonDrag(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            // Get the new set of gaps.
            LegGap[] newGaps = LegGap.AddGap(courseObjStart.path, courseObjStart.gaps, startDrag, location);

            // Put the new gaps into the highlight.
            courseObjDrag = new LegCourseObj(courseObjStart.controlId, courseObjStart.courseControlId, courseObjStart.courseControlId2,
                courseObjStart.scaleRatio, courseObjStart.appearance, courseObjStart.path, newGaps);

            displayUpdateNeeded = true;
        }

        public override void LeftButtonEndDrag(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            controller.AddLegGap(startDrag, location);     // implicitly uses the current selected to determine which leg gets the gap.

            controller.DefaultCommandMode();
            displayUpdateNeeded = true;
        }

        public override void LeftButtonCancelDrag(ref bool displayUpdateNeeded)
        {
            // Drag was cancelled. Go back to normal mode.
            controller.DefaultCommandMode();
            displayUpdateNeeded = true;
        }
    }
}
