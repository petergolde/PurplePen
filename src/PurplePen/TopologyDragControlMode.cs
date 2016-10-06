using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace PurplePen
{
    using PurplePen.MapView;
    using PurplePen.MapModel;
    using PurplePen.Graphics2D;
    using System.Windows.Forms;

    internal class TopologyDragControlMode: BaseMode
    {
        private Controller controller;
        private EventDB eventDB;
        private SelectionMgr selectionMgr;
        private CourseObj courseObjectStart, courseObjectDrag;
        private PointF startDrag, currentLocation;

        public TopologyDragControlMode(Controller controller, EventDB eventDB, SelectionMgr selectionMgr, CourseObj courseObject, PointF location)
        {
            this.controller = controller;
            this.eventDB = eventDB;
            this.selectionMgr = selectionMgr;
            this.courseObjectStart = courseObject;
            this.courseObjectDrag = (CourseObj)(courseObject.Clone());
            this.startDrag = this.currentLocation = location;
        }

        public override IMapViewerHighlight[] GetHighlights(Pane pane)
        {
            if (pane != Pane.Topology)
                return null;

            return new CourseObj[] { courseObjectDrag };
        }

        public override string StatusText
        {
            get
            {
                return StatusBarText.DraggingTopologyObject;
            }
        }

        public override MapViewer.DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane == Pane.Topology) {
                return MapViewer.DragAction.ImmediateDrag;
            }
            else {
                return MapViewer.DragAction.None;
            }
        }

        public override void LeftButtonDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Topology);

            currentLocation = location;

            // Update the highlight.
            courseObjectDrag = ((CourseObj)courseObjectStart.Clone());
            courseObjectDrag.Offset(location.X - startDrag.X, location.Y - startDrag.Y);

            displayUpdateNeeded = true;
        }

        public override void LeftButtonEndDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Topology);

            float deltaX = (location.X - startDrag.X);
            float deltaY = (location.Y - startDrag.Y);

            /*
                // Move the control to the new location.
                Id<ControlPoint> controlId = courseObjectStart.controlId;
                PointF originalLocation = ((PointCourseObj)courseObjectStart).location;
                PointF newLocation = PointF.Add(originalLocation, new SizeF(deltaX, deltaY));

                controller.MoveControlInCurrentCourse(controlId, newLocation);
            */

            controller.DefaultCommandMode();
        }

        public override void LeftButtonCancelDrag(Pane pane, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Topology);

            // Drag was cancelled. Go back to normal mode.
            controller.DefaultCommandMode();
        }

        public override Cursor GetMouseCursor(Pane pane, PointF location, float pixelSize)
        {
            if (pane == Pane.Topology) {
                return Cursors.SizeAll;
            }
            else {
                return Cursors.Arrow;
            }
        }

    }
}