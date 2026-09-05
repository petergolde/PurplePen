using System;
using System.Collections.Generic;
using System.Drawing;

using PurplePen.MapModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PurplePen
{
    // Mode for adding an image or rectangleSpecial to a course.
    class AddRectangleMode : BaseMode
    {
        Controller controller;
        SelectionMgr selectionMgr;
        UndoMgr undoMgr;
        EventDB eventDB;
        CourseObj startingObj;           // base object being dragged out -- used to create current obj being dragged.
        RectCourseObj currentObj;           // current object being dragged out.
        PointF startLocation;                               // location where dragging started.
        PointF handleDragging;

        // Aspect ratio 
        float aspectRatio;

        Func<RectangleF, CourseObj> createCourseObj;
        Func<RectangleF, Id<Special>> createSpecial;


        public AddRectangleMode(Controller controller, UndoMgr undoMgr, SelectionMgr selectionMgr, EventDB eventDB, float aspectRatio, Func<RectangleF, CourseObj> createCourseObj, Func<RectangleF, Id<Special>> createSpecial)
        {
            this.controller = controller;
            this.undoMgr = undoMgr;
            this.selectionMgr = selectionMgr;
            this.eventDB = eventDB;
            this.aspectRatio = aspectRatio;
            this.createCourseObj = createCourseObj;
            this.createSpecial = createSpecial;
        }

        // Mouse cursor looks like a crosshair
        public override MousePointerShape GetMouseCursor(Pane pane, PointF location, float pixelSize)
        {
            if (pane != Pane.Map)
                return MousePointerShape.Arrow;

            return MousePointerShape.Cross;
        }

        public override string StatusText
        {
            get
            {
                return StatusBarText.AddingRectangle;
            }
        }

        public override IMapViewerHighlight[] GetHighlights(Pane pane)
        {
            if (pane == Pane.Map && currentObj != null)
                return new CourseObj[] { currentObj };
            else
                return null;
        }

        // Update currentObj to reflect dragging to the given location.
        void DragTo(PointF location)
        {
            currentObj = (RectCourseObj)startingObj.Clone();
            currentObj.MoveHandle(handleDragging, location);
        }

        public override DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return DragAction.None;

            // Begin dragging out the image.
            startLocation = location;
            startingObj = createCourseObj(new RectangleF(location.X, location.Y, 0.1F, 0.1F * aspectRatio));
            handleDragging = location;
            DragTo(location);
            displayUpdateNeeded = true;
            return DragAction.DelayedDrag;  // Also allow a click.
        }

        public override void LeftButtonDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Map);

            DragTo(location);
            displayUpdateNeeded = true;
        }

        public override async Task<bool> LeftButtonClick(Pane pane, PointF location, float pixelSize)
        {
            if (pane != Pane.Map)
                return false;

            // User just clicked. Create rectangle of a default size.
            SizeF newSize = aspectRatio < 1 ? new SizeF(60F, 60F * aspectRatio) : new SizeF(60F / aspectRatio, 60F);
            CreateImageSpecial(new RectangleF(location, newSize));
            return true;
        }

        public override async Task<bool> LeftButtonEndDrag(Pane pane, PointF location, PointF locationStart, float pixelSize)
        {
            Debug.Assert(pane == Pane.Map);

            DragTo(location);

            RectangleF rect = currentObj.rect;
            if (rect.Height < 1 || rect.Width < 1) {
                // Too small. Use the click action.
                return await LeftButtonClick(pane, location, pixelSize);
            }
            else {
                CreateImageSpecial(rect);
                return true;
            }
        }

        void CreateImageSpecial(RectangleF boundingRect)
        {
            undoMgr.BeginCommand(1851, CommandNameText.AddObject);
            Id<Special> specialId = createSpecial(boundingRect);
            undoMgr.EndCommand(1851);

            selectionMgr.SelectSpecial(specialId);

            controller.DefaultCommandMode();
        }
    }
}
