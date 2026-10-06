using System;
using System.Collections.Generic;
using System.Drawing;

using PurplePen.MapModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PurplePen
{
    // Mode for adding a gap to a control.
    class AddControlGapMode: BaseMode
    {
        Controller controller;
        PointCourseObj courseObjStart;            // object to modify.
        PointCourseObj courseObjDrag;             // object being dragged on.

        PointF startDrag;

        public AddControlGapMode(Controller controller, PointCourseObj courseObj)
        {
            this.controller = controller;
            this.courseObjStart = (PointCourseObj) courseObj.Clone();
            this.courseObjDrag = (PointCourseObj) courseObj.Clone();
        }

        // Mouse cursor looks like a crosshair
        public override MousePointerShape GetMouseCursor(Pane pane, PointF location, float pixelSize)
        {
            if (pane == Pane.Map)
                return MousePointerShape.Cross;
            else
                return MousePointerShape.Arrow;
        }

        public override string StatusText
        {
            get
            {
                return StatusBarText.AddingControlGap;
            }
        }

        public override IMapViewerHighlight[] GetHighlights(Pane pane)
        {
            if (pane != Pane.Map)
                return null;

            return new CourseObj[1] { courseObjDrag };
        }

        public override DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return DragAction.None;

            startDrag = location;
            return DragAction.DelayedDrag;
        }

        public override void LeftButtonDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Map);

            // Get the new set of gaps.
            CircleGap[] newGaps = CircleGap.AddGap(courseObjStart.location, courseObjStart.gaps, startDrag, location);
            CircleGap[] newMovableGaps = CircleGap.AddGap(courseObjStart.location, courseObjStart.movableGaps, startDrag, location);

            // Put the new gaps into the highlight.
            courseObjDrag = (PointCourseObj) courseObjStart.Clone();
            courseObjDrag.gaps = newGaps;
            courseObjDrag.movableGaps = newMovableGaps;

            displayUpdateNeeded = true;
        }

        public override async Task<bool> LeftButtonEndDrag(Pane pane, PointF location, PointF locationStart, float pixelSize)
        {
            Debug.Assert(pane == Pane.Map);

            controller.AddControlGap(startDrag, location);

            controller.DefaultCommandMode();
            return true;
        }

        public override async Task<bool> LeftButtonClick(Pane pane, PointF location, float pixelSize)
        {
            if (pane != Pane.Map)
                return false;

            // Create the new gap
            controller.AddControlGap(location);
            controller.DefaultCommandMode();
            return true;
        }

        public override void LeftButtonCancelDrag(Pane pane, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Map);

            // Drag was cancelled. Go back to normal mode.
            controller.DefaultCommandMode();
            displayUpdateNeeded = true;
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
        public override MousePointerShape GetMouseCursor(Pane pane, PointF location, float pixelSize)
        {
            if (pane == Pane.Map)
                return MousePointerShape.Cross;
            else
                return MousePointerShape.Arrow;
        }

        public override string StatusText
        {
            get
            {
                return StatusBarText.RemovingControlGap;
            }
        }

        public override IMapViewerHighlight[] GetHighlights(Pane pane)
        {
            if (pane != Pane.Map)
                return null;

            return new CourseObj[1] { courseObj };
        }

        public override DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return DragAction.None;

            // Create the new corner
            controller.RemoveControlGap(location);
            controller.DefaultCommandMode();
            return DragAction.SuppressClick;
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
        public override MousePointerShape GetMouseCursor(Pane pane, PointF location, float pixelSize)
        {
            if (pane == Pane.Map)
                return MousePointerShape.Cross;
            else
                return MousePointerShape.Arrow;
        }

        public override string StatusText
        {
            get
            {
                return StatusBarText.RemovingLegGap;
            }
        }

        public override IMapViewerHighlight[] GetHighlights(Pane pane)
        {
            if (pane != Pane.Map)
                return null;

            return new CourseObj[1] { courseObj };
        }

        public override DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return DragAction.None;

            // Remove the gap
            controller.RemoveLegGap(location);
            controller.DefaultCommandMode();
            return DragAction.SuppressClick;
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

        public override IMapViewerHighlight[] GetHighlights(Pane pane)
        {
            if (pane != Pane.Map)
                return null;

            return new CourseObj[] { courseObjDrag };
        }

        // Mouse cursor looks like a crosshair
        public override MousePointerShape GetMouseCursor(Pane pane, PointF location, float pixelSize)
        {
            return MousePointerShape.Cross;
        }

        public override string StatusText
        {
            get
            {
                return StatusBarText.AddingLegGap;
            }
        }

        public override DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return DragAction.None;

            startDrag = location;
            return DragAction.DelayedDrag;
        }

        public override void LeftButtonDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Map);

            // Get the new set of gaps.
            LegGap[] newGaps = LegGap.AddGap(courseObjStart.path, courseObjStart.gaps, startDrag, location);

            // Put the new gaps into the highlight.
            courseObjDrag = new LegCourseObj(courseObjStart.controlId, courseObjStart.courseControlId, courseObjStart.courseControlId2,
                courseObjStart.courseObjRatio, courseObjStart.appearance, courseObjStart.path, newGaps);

            displayUpdateNeeded = true;
        }

        public override async Task<bool> LeftButtonEndDrag(Pane pane, PointF location, PointF locationStart, float pixelSize)
        {
            Debug.Assert(pane == Pane.Map);

            controller.AddLegGap(startDrag, location);     // implicitly uses the current selected to determine which leg gets the gap.

            controller.DefaultCommandMode();
            return true;
        }

        public override async Task<bool> LeftButtonClick(Pane pane, PointF location, float pixelSize)
        {
            if (pane != Pane.Map)
                return false;

            controller.AddLegGap(location);     // implicitly uses the current selected to determine which leg gets the gap.

            controller.DefaultCommandMode();
            return true;
        }

        public override void LeftButtonCancelDrag(Pane pane, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Map);

            // Drag was cancelled. Go back to normal mode.
            controller.DefaultCommandMode();
            displayUpdateNeeded = true;
        }
    }
}
