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
        public override Cursor GetMouseCursor(PointF location, float pixelSize)
        {
            return Cursors.Cross;
        }

        public override string StatusText
        {
            get
            {
                return StatusBarText.RotatingObject;
            }
        }

        public override IMapViewerHighlight[] GetHighlights()
        {
            return new CourseObj[1] { courseObj };
        }

        public override MapViewer.DragAction LeftButtonDown(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            // Create the new corner
            RotateToAngle(location);
            controller.Rotate(courseObj.orientation);
            controller.DefaultCommandMode();
            return MapViewer.DragAction.None;
        }

        public override void MouseMoved(PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            RotateToAngle(location);
            displayUpdateNeeded = true;
        }

        // Change the orientation of the crossing point course object to the given angle in degrees.
        private void RotateToAngle(PointF point)
        {
            double angleInRadians = Math.Atan2(point.Y - courseObj.location.Y, point.X - courseObj.location.X);
            float angleInDegrees = (float) Util.RadiansToDegrees(angleInRadians);
            courseObj = (CrossingCourseObj) courseObj.Clone();
            courseObj.ChangeOrientation(angleInDegrees);
        }
    }
}
