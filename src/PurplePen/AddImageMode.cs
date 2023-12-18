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
using System.Diagnostics;

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
        public override Cursor GetMouseCursor(Pane pane, PointF location, float pixelSize)
        {
            if (pane != Pane.Map)
                return Cursors.Arrow;

            return Cursors.Cross;
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

        public override MapViewer.DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return MapViewer.DragAction.None;

            // Begin dragging out the image.
            startLocation = location;
            startingObj = createCourseObj(new RectangleF(location.X, location.Y, 0.1F, 0.1F * aspectRatio));
            handleDragging = location;
            DragTo(location);
            displayUpdateNeeded = true;
            return MapViewer.DragAction.DelayedDrag;  // Also allow a click.
        }

        public override void LeftButtonDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Map);

            DragTo(location);
            displayUpdateNeeded = true;
        }

        public override void LeftButtonClick(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return;

            // User just clicked. Create rectangle of a default size.
            SizeF newSize = aspectRatio < 1 ? new SizeF(60F, 60F * aspectRatio) : new SizeF(60F / aspectRatio, 60F);
            CreateImageSpecial(new RectangleF(location, newSize));
            displayUpdateNeeded = true;
        }

        public override void LeftButtonEndDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Map);

            DragTo(location);

            RectangleF rect = currentObj.rect;
            if (rect.Height < 1 || rect.Width < 1) {
                // Too small. Use the click action.
                LeftButtonClick(pane, location, pixelSize, ref displayUpdateNeeded);
            }
            else {
                CreateImageSpecial(rect);
                displayUpdateNeeded = true;
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
