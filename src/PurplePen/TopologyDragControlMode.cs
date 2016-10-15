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

        private CourseObj dropTargetHighlight;

        public TopologyDragControlMode(Controller controller, EventDB eventDB, SelectionMgr selectionMgr, CourseObj courseObject, PointF location)
        {
            this.controller = controller;
            this.eventDB = eventDB;
            this.selectionMgr = selectionMgr;
            this.courseObjectStart = courseObject;
            this.courseObjectDrag = (CourseObj)(courseObject.Clone());
            this.startDrag = this.currentLocation = location;
            this.dropTargetHighlight = null;
        }

        public override IMapViewerHighlight[] GetHighlights(Pane pane)
        {
            if (pane != Pane.Topology)
                return null;

            if (dropTargetHighlight == null)
                return new CourseObj[] { courseObjectDrag };
            else
                return new CourseObj[] { courseObjectDrag, dropTargetHighlight };
        }

        public override string StatusText
        {
            get
            {
                return StatusBarText.DraggingTopologyObject;
            }
        }

        private TopologyDropTargetCourseObj FindNearbyDropTarget(PointF location)
        {
            const float MAXDISTANCE = 15F;

            CourseLayout layout = selectionMgr.TopologyLayout;
            TopologyDropTargetCourseObj nearest = null;
            float nearestDistance = float.MaxValue;

            // Find nearest drop target that is within MAXDISTANCE of location.
            foreach (CourseObj obj in layout) {
                TopologyDropTargetCourseObj dropTarget = obj as TopologyDropTargetCourseObj;
                if (dropTarget != null) {
                    float distance = Geometry.DistanceF(location, dropTarget.location);
                    if (distance < nearestDistance && distance < MAXDISTANCE) {
                        nearestDistance = distance;
                        nearest = dropTarget;
                    }
                }
            }

            return nearest;
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

            dropTargetHighlight = FindNearbyDropTarget(location);

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

            dropTargetHighlight = null;
            displayUpdateNeeded = true;
            controller.DefaultCommandMode();
        }

        public override void LeftButtonCancelDrag(Pane pane, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Topology);

            // Drag was cancelled. Go back to normal mode.
            dropTargetHighlight = null;
            displayUpdateNeeded = true;

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