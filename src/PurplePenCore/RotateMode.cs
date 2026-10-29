using System;
using System.Collections.Generic;
using System.Drawing;

using PurplePen.MapModel;
using PurplePen.Graphics2D;

namespace PurplePen
{
    // Mode for rotating an object.
    class RotateMode: BaseMode
    {
        Controller controller;
        CrossingCourseObj courseObj;            // object to modify.

        public RotateMode(Controller controller, CrossingCourseObj courseObj)
        {
            this.controller = controller;
            this.courseObj = (CrossingCourseObj) courseObj.Clone();
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
                return StatusBarText.RotatingObject;
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
            RotateToAngle(location);
            controller.Rotate(courseObj.orientation);
            controller.DefaultCommandMode();
            return DragAction.None;
        }

        public override void MouseMoved(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return;

            RotateToAngle(location);
            displayUpdateNeeded = true;
        }

        // Change the orientation of the crossing point course object to the given angle in degrees.
        private void RotateToAngle(PointF point)
        {
            double angleInRadians = Math.Atan2(point.Y - courseObj.location.Y, point.X - courseObj.location.X);
            float angleInDegrees = (float) Geometry.RadiansToDegrees(angleInRadians);
            courseObj = (CrossingCourseObj) courseObj.Clone();
            courseObj.ChangeOrientation(angleInDegrees);
        }
    }
}
