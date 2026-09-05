using System;
using System.Collections.Generic;
using System.Drawing;

using PurplePen.MapModel;
using System.Diagnostics;
using PurplePen.Graphics2D;
using System.Threading.Tasks;

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

        public override DragAction LeftButtonDown(Pane pane, PointF location, float pixelSize, ref bool displayUpdateNeeded)
        {
            if (pane != Pane.Map)
                return DragAction.None;

            // Begin dragging out the description block.
            startLocation = location;
            startingObj = new BasicTextCourseObj(Id<Special>.None, displayText, new RectangleF(location, new SizeF(0.001F, 0.001F)), fontName, Util.GetTextEffects(fontBold, fontItalic), fontColor, fontHeight);
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

            // If text is empty, use a non-empty text
            string measureText = string.IsNullOrEmpty(displayText) ? "000000" : displayText;

            // User just clicked. Create text of a default size.
            ITextMetrics textMetrics = Services.TextMetricsProvider;
            ITextFaceMetrics textFaceMetrics = textMetrics.GetTextFaceMetrics(NormalCourseAppearance.fontNameTextSpecial, NormalCourseAppearance.emHeightDefaultTextSpecial, NormalCourseAppearance.fontEffectsTextSpecial); 
            SizeF size = textFaceMetrics.GetTextSize(measureText);

            RectangleF boundingRect = new RectangleF(new PointF(location.X, location.Y - size.Height), size);
            boundingRect = currentObj.AdjustBoundingRect(boundingRect);
            CreateTextSpecial(boundingRect);
            return true;
        }

        public override async Task<bool> LeftButtonEndDrag(Pane pane, PointF location, PointF locationStart, float pixelSize)
        {
            Debug.Assert(pane == Pane.Map);

            DragTo(location);

            RectangleF rect = currentObj.GetHighlightBounds();
            if (rect.Height < 1 || rect.Width < 1) {
                // Too small. Use the click action.
                return await LeftButtonClick(pane, location, pixelSize);
            }
            else {
                rect = currentObj.AdjustBoundingRect(rect);
                CreateTextSpecial(rect);
                return true;
            }
        }

        void CreateTextSpecial(RectangleF boundingRect)
        {
            undoMgr.BeginCommand(1551, CommandNameText.AddObject);

            Id<Special> specialId = ChangeEvent.AddTextSpecial(eventDB, boundingRect, text, currentObj.fontName, (currentObj.textEffects & TextEffects.Bold) != 0, (currentObj.textEffects & TextEffects.Italic) != 0, currentObj.fontColor, currentObj.fontDigitHeight);
            undoMgr.EndCommand(1551);

            selectionMgr.SelectSpecial(specialId);

            controller.DefaultCommandMode();
        }
    }

}
