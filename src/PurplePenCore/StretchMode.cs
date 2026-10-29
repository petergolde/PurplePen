using System;
using System.Collections.Generic;
using System.Drawing;

using PurplePen.MapModel;
using PurplePen.Graphics2D;
using System.Threading.Tasks;

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
                return StatusBarText.StretchingObject;
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

            // Create the new start point of the drag
            mouseDown = location;
            return DragAction.ImmediateDrag;
        }

        public override void LeftButtonDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return;

            UpdateStretch(location);
            displayUpdateNeeded = true;
        }

        public override async Task<bool> LeftButtonEndDrag(Pane pane, PointF location, PointF locationStart, float pixelSize)
        {
            if (pane != Pane.Map)
                return false;

            UpdateStretch(location);
            controller.Stretch(courseObj.stretch);
            controller.DefaultCommandMode();
            return true;
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
