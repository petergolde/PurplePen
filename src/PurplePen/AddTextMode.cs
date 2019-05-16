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
    // Mode for adding a description to a course.
    class AddTextMode: BaseMode
    {
        Controller controller;
        SelectionMgr selectionMgr;
        UndoMgr undoMgr;
        EventDB eventDB;
        BasicTextCourseObj startingObj;           // base object being dragged out -- used to create current obj being dragged.
        BasicTextCourseObj currentObj;           // current object being dragged out.
        PointF startLocation;                               // location where dragging started.
        PointF handleDragging;

        // The text being added.
        string text;

        // The text to display
        string displayText;

        // The text properties
        string fontName;
        bool fontBold, fontItalic;
        SpecialColor fontColor;
        float fontHeight;

        public AddTextMode(Controller controller, UndoMgr undoMgr, SelectionMgr selectionMgr, EventDB eventDB, string text, string fontName, bool fontBold, bool fontItalic, SpecialColor fontColor, float fontHeight)
        {
            this.controller = controller;
            this.undoMgr = undoMgr;
            this.selectionMgr = selectionMgr;
            this.eventDB = eventDB;
            this.text = text;
            this.fontName = fontName;
            this.fontBold = fontBold;
            this.fontItalic = fontItalic;
            this.fontColor = fontColor;
            this.fontHeight = fontHeight;
            this.displayText = CourseFormatter.ExpandText(eventDB, selectionMgr.ActiveCourseView, text);
        }

        // Mouse cursor looks like a crosshair
        public override Cursor GetMouseCursor(Pane pane, PointF location, float pixelSize)
        {
            if (pane == Pane.Map)
                return Cursors.Cross;
            else
                return Cursors.Arrow;
        }

        public override string StatusText
        {
            get
            {
                return StatusBarText.AddingText;
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
            currentObj = (BasicTextCourseObj) startingObj.Clone();
            currentObj.MoveHandle(handleDragging, location);
        }

        public override MapViewer.DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return MapViewer.DragAction.None;

            // Begin dragging out the description block.
            startLocation = location;
            startingObj = new BasicTextCourseObj(Id<Special>.None, displayText, new RectangleF(location, new SizeF(0.001F, 0.001F)), fontName, Util.GetFontStyle(fontBold, fontItalic), fontColor, fontHeight);
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

            // If text is empty, use a non-empty text
            string measureText = string.IsNullOrEmpty(displayText) ? "000000" : displayText;

            // User just clicked. Create text of a default size.
            SizeF size;
            Graphics g = Util.GetHiresGraphics();
            using (Font f = GdiplusFontLoader.CreateFont(NormalCourseAppearance.fontNameTextSpecial, NormalCourseAppearance.emHeightDefaultTextSpecial, NormalCourseAppearance.fontStyleTextSpecial))
                size = g.MeasureString(measureText, f, new PointF(0,0), StringFormat.GenericTypographic);

            RectangleF boundingRect = new RectangleF(new PointF(location.X, location.Y - size.Height), size);
            boundingRect = currentObj.AdjustBoundingRect(boundingRect);
            CreateTextSpecial(boundingRect);
            displayUpdateNeeded = true;
        }

        public override void LeftButtonEndDrag(Pane pane, PointF location, PointF locationStart, float pixelSize, ref bool displayUpdateNeeded)
        {
            Debug.Assert(pane == Pane.Map);

            DragTo(location);

            RectangleF rect = currentObj.GetHighlightBounds();
            if (rect.Height < 1 || rect.Width < 1) {
                // Too small. Use the click action.
                LeftButtonClick(pane, location, pixelSize, ref displayUpdateNeeded);
            }
            else {
                rect = currentObj.AdjustBoundingRect(rect);
                CreateTextSpecial(rect);
                displayUpdateNeeded = true;
            }
        }

        void CreateTextSpecial(RectangleF boundingRect)
        {
            undoMgr.BeginCommand(1551, CommandNameText.AddObject);

            Id<Special> specialId = ChangeEvent.AddTextSpecial(eventDB, boundingRect, text, currentObj.fontName, (currentObj.fontStyle & FontStyle.Bold) != 0, (currentObj.fontStyle & FontStyle.Italic) != 0, currentObj.fontColor, currentObj.fontDigitHeight);
            undoMgr.EndCommand(1551);

            selectionMgr.SelectSpecial(specialId);

            controller.DefaultCommandMode();
        }
    }

}
