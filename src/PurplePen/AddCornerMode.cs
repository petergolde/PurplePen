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
        public override Cursor GetMouseCursor(Pane pane, PointF location, float pixelSize)
        {
            if (pane == Pane.Map) {
                return Cursors.Cross;
            }
            else {
                return Cursors.Arrow;
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

        public override MapViewer.DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return MapViewer.DragAction.None;

            // Create the new corner
            controller.AddCorner(location);
            controller.DefaultCommandMode();
            return MapViewer.DragAction.None;
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

        public override Cursor GetMouseCursor(Pane pane, PointF location, float pixelSize)
        {
            PointF handleLocation;

            if (pane == Pane.Map && HitTestHandle(location, pixelSize, out handleLocation)) {
                return Util.DeleteHandleCursor;
            }
            else {
                return Cursors.Arrow;
            }
        }

        public override string StatusText
        {
            get
            {
                return courseObject.specialId.IsNotNone ? StatusBarText.DeletingCorner : StatusBarText.DeletingBend;
            }
        }

        public override MapViewer.DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            PointF handleLocation;

            if (pane == Pane.Map && HitTestHandle(location, pixelSize, out handleLocation)) {
                controller.DeleteCorner(handleLocation);
                controller.DefaultCommandMode();
            }

            return MapViewer.DragAction.None;
        }
    }
}
