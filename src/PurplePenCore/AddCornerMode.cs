using System;
using System.Collections.Generic;
using System.Drawing;

using PurplePen.MapModel;
using PurplePen.Graphics2D;

namespace PurplePen
{
    // Mode for adding a corner to a special or a leg.
    class AddCornerMode: BaseMode
    {
        Controller controller;
        IMapViewerHighlight[] highlights;            // highlights to display.
        bool isLeg;                                               // adding to a leg?

        public AddCornerMode(Controller controller, bool isLeg, IMapViewerHighlight[] highlights)
        {
            this.controller = controller;
            this.highlights = highlights;
            this.isLeg = isLeg;
        }

        // Mouse cursor looks like a crosshair
        public override MousePointerShape GetMouseCursor(Pane pane, PointF location, float pixelSize)
        {
            if (pane == Pane.Map) {
                return MousePointerShape.Cross;
            }
            else {
                return MousePointerShape.Arrow;
            }
        }

        public override string StatusText
        {
            get
            {
                return isLeg ? StatusBarText.AddingBend : StatusBarText.AddingCorner;
            }
        }

        public override IMapViewerHighlight[]  GetHighlights(Pane pane)
        {
            if (pane != Pane.Map)
                return null;
 	        return highlights;
        }

        public override DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return DragAction.None;

            // Create the new corner
            controller.AddCorner(location);
            controller.DefaultCommandMode();
            return DragAction.SuppressClick;
        }
    }

    // Mode when an corner is being deleted from a leg or a special
    class DeleteCornerMode: BaseMode
    {
        Controller controller;
        CourseObj courseObject;

        public DeleteCornerMode(Controller controller, CourseObj courseObject)
        {
            this.controller = controller;
            this.courseObject = courseObject;
        }

        // Hit test a location to see if it is over a handle.
        bool HitTestHandle(PointF location, float pixelSize, out PointF handleLocation)
        {
            PointF[] handles = courseObject.GetHandles();
            if (handles != null) {
                foreach (PointF handle in handles) {
                    double distance = Geometry.Distance(location, handle);
                    if (distance / pixelSize <= 3.0) {
                        // over a handle.
                        handleLocation = handle;
                        return true;
                    }
                }
            }

            // didn't find a handle.
            handleLocation = new PointF();
            return false;
        }

        public override IMapViewerHighlight[] GetHighlights(Pane pane)
        {
            if (pane != Pane.Map)
                return null;

            return new CourseObj[] { courseObject };
        }

        public override MousePointerShape GetMouseCursor(Pane pane, PointF location, float pixelSize)
        {
            PointF handleLocation;

            if (pane == Pane.Map && HitTestHandle(location, pixelSize, out handleLocation)) {
                return MousePointerShape.DeleteHandle;
            }
            else {
                return MousePointerShape.Arrow;
            }
        }

        public override string StatusText
        {
            get
            {
                return courseObject.specialId.IsNotNone ? StatusBarText.DeletingCorner : StatusBarText.DeletingBend;
            }
        }

        public override DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            PointF handleLocation;

            if (pane == Pane.Map && HitTestHandle(location, pixelSize, out handleLocation)) {
                controller.DeleteCorner(handleLocation);
                controller.DefaultCommandMode();
            }

            return DragAction.SuppressClick;
        }
    }
}
