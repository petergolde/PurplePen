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
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace PurplePen
{
    partial class DescriptionControl : UserControl
    {
        // What was changed?
        public enum ChangeKind
        {
            None,               
            Title,              // Primary title
            SecondaryTitle,     // Secondary title
            CourseName,         // Course name
            Climb,              // Course climb box
            Length,             // Course length box
            Score,              // Score box for a control (column A on score course)
            Code,               // Code box for a control (column B)
            DescriptionBox,     // Other box (C-H) on a control
            Directive,           // A directive
            Key,                      // A symbol key line
            TextLine,              // A text line
        }

        SymbolDB symbolDB;
        DescriptionRenderer renderer;
        CourseView.CourseViewKind courseViewKind;
        bool isCoursePart, hasCustomLength;
        SymbolPopup popup;
        int scoreColumn;

        int firstSelectedLine = -1;                  // first selected line, or -1 for no selection.
        int lastSelectedLine = -1;                  // last selected line, or -1 for no selection.
        Brush selectionBrush = Brushes.Yellow;  // brush to draw the selection in.

        int scrollWidth;                 // width of a vertical scroll bar.

        // If a popup is active, the below indicate what.
        ChangeKind popupKind;
        int popupLine;
        int popupBox;

        const int margin = 3;            // margin size in pixels
        const int popupBoxSize = 28;     // box size in the popup menu
        const float minBoxSize = 20;     // minimum box size of the description panel; 

        public delegate void DescriptionChangedHandler(DescriptionControl sender, ChangeKind kind, int line, int box, object newValue);

        // Via a popup-menu, the user requested a change to what is in a box in the description.
        public event DescriptionChangedHandler Change;

        // Via a mouse, the selected was changed. Does not fire if the selected in changed via
        // the property.
        public event EventHandler SelectedIndexChange;      

        public DescriptionControl()
        {
            ResizeRedraw = false;

            InitializeComponent();

            // Get the size of a vertical scroll bar.
            VScrollBar scrollBar = new VScrollBar();
            scrollWidth = scrollBar.GetPreferredSize(new Size(200,200)).Width;
            scrollBar.Dispose();
        }



        // The SymbolDB should be set immediately after creation.
        public SymbolDB SymbolDB
        {
            get
            {
                return symbolDB;
            }

            set
            {
                if (value != null) {
                    Debug.Assert(symbolDB == null, "Symbol database cannot be set more than once");
                    symbolDB = value;

                    // Create the renderer.
                    renderer = new DescriptionRenderer(symbolDB);
                    renderer.Margin = margin;
                    renderer.DescriptionKind = DescriptionKind.Symbols;     // control always shows symbols.

                    // Create the popup for displaying.
                    popup = new SymbolPopup(symbolDB, popupBoxSize);
                    popup.Selected += new SymbolPopupEventHandler(popup_Selected);
                    popup.Canceled += new EventHandler(popup_Canceled);
                }
            }
        }

        // Dictionary to map custom symbol names (never changes)
        public Dictionary<string, string> CustomSymbolText
        {
            get
            {
                if (popup != null)
                    return popup.customSymbolText;
                else
                    return null;
            }
            set
            {
                if (popup != null)
                    popup.customSymbolText = value;
            }
        }

        // language id for symbol names.
        [System.ComponentModel.DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), System.ComponentModel.Browsable(false)]
        public string LangId
        {
            get
            {
                return popup.LangId;
            }
            set
            {
                popup.LangId = value;
            }
        }

        // Popup the control code popup on the selected line.
        public void PopupControlCode()
        {
            if (firstSelectedLine >= 0) {
                HitTestResult hittest = new HitTestResult();
                hittest.kind = HitTestKind.NormalBox;
                hittest.firstLine = firstSelectedLine;
                hittest.lastLine = lastSelectedLine;
                hittest.box = 1;
                hittest.rect = renderer.BoxBounds(firstSelectedLine, lastSelectedLine, 1, 1);
                PopupMenu(hittest);
            }
        }

        // Make sure the given line is in view.
        public void ScrollLineIntoView(int line)
        {
            Rectangle lineRect = Util.Round(renderer.LineBounds(line, line));
            Point currentScrollPosition = AutoScrollPosition;
            lineRect.Offset(currentScrollPosition);
            Rectangle client = ClientRectangle;

            if (lineRect.Top >= client.Top && lineRect.Bottom <= client.Bottom)
                return;             // no scrolling needed.
            else if (lineRect.Top < client.Top) {
                // the line is off the top. Scroll it into view.
                int adjustment = client.Top - lineRect.Top + lineRect.Height;
                AutoScrollPosition = new Point(-currentScrollPosition.X, -(currentScrollPosition.Y + adjustment));
            }
            else if (lineRect.Bottom > client.Bottom) {
                // the line is off the bottom. Scroll it into view.
                int adjustment = lineRect.Bottom - client.Bottom + lineRect.Height;
                AutoScrollPosition = new Point(-currentScrollPosition.X, -(currentScrollPosition.Y - adjustment));
            }
        }

        public void GetSelection(out int firstLine, out int lastLine)
        {
            firstLine = firstSelectedLine;
            lastLine = lastSelectedLine;
        }

        public void SetSelection(int firstLine, int lastLine)
        {
            if (firstLine < -1 || firstLine >= ((Description == null) ? 0 : Description.Length))
                throw new ArgumentOutOfRangeException("firstLine");
            if (lastLine < -1 || lastLine >= ((Description == null) ? 0 : Description.Length))
                throw new ArgumentOutOfRangeException("lastLine");

            int oldFirstLine = firstSelectedLine, oldLastLine = lastSelectedLine;
            firstSelectedLine = firstLine;
            lastSelectedLine = lastLine;

            // Invalidate parts based on the line.
            if (oldFirstLine != firstSelectedLine || oldLastLine != lastSelectedLine) {
                if (firstSelectedLine != -1)
                    ScrollLineIntoView(firstSelectedLine);
                if (oldFirstLine >= 0) {
                    for (int l = oldFirstLine; l <= oldLastLine; ++l)
                        InvalidateLine(l);
                }
                if (firstSelectedLine >= 0) {
                    for (int l = firstSelectedLine; l <= lastSelectedLine; ++l)
                        InvalidateLine(l);
                }
            }
        }

        // Set the description to display.
        public DescriptionLine[] Description
        {
            get
            {
                if (renderer == null)
                    return null;
                else
                    return renderer.Description;
            }

            set
            {
                if (renderer == null) {
                    Debug.Assert(value == null);
                    return;
                }

                Debug.Assert(symbolDB != null, "Must set SymbolDB first!");
                DescriptionLine[] old = renderer.Description;
                renderer.Description = value;

                if (old == null || old.Length != value.Length)
                    UpdatePanelSize();

                // Is selection is out of range, remove it.
                if (firstSelectedLine >= value.Length || lastSelectedLine >= value.Length)
                    firstSelectedLine = lastSelectedLine = -1;

                // Eventually, check if the description changed only a little and
                // just invalidate the line(s) that changed.
                InvalidateChangedLines(old, value);
            }
        }

        public void CloseAnyPopup()
        {
            if (popup != null)
                popup.ClosePopup();
        }

        // Invalidate any lines that have changed between two descriptions.
        void InvalidateChangedLines(DescriptionLine[] old, DescriptionLine[] current)
        {
            if (old == null || current == null) {
                descriptionPanel.Invalidate();
                return;
            }

            int lineCount = Math.Max(old.Length, current.Length);
            for (int i = 0; i < lineCount; ++i) {
                bool invalidate = false;

                if (i >= old.Length || i >= current.Length)
                    invalidate = true;
                else if (!old[i].Equals(current[i]))
                    invalidate = true;

                if (invalidate) {
                    RectangleF bounds = renderer.LineBounds(i, i);
                    descriptionPanel.Invalidate(Util.Round(bounds));
                }
            }
        }


        // Set the course kind being displayed. This affects the popups to some
        // extent (e.g., score courses have a popup to set the score in column A, but
        // other courses don't.
        public CourseView.CourseViewKind CourseKind
        {
            get
            {
                return courseViewKind;
            }
            set
            {
                CourseView.CourseViewKind old = courseViewKind;
                courseViewKind = value;

                // Note that the course kind affects the click behavior, but not the rendering,
                // so we don't need to invalidate the panel here.
            }
        }

        public bool IsCoursePart {
            get
            {
                return isCoursePart;
            }

            set
            {
                if (isCoursePart != value) {
                    isCoursePart = value;
                    descriptionPanel.Invalidate();
                }
            }
        }

        public bool HasCustomLength
        {
            get { return hasCustomLength; }
            set
            {
                hasCustomLength = value;
                // Only affects click behavior, so no need to invalidate.
            }

        }

        // lColumn that displays the score, or -1 if none.
        [System.ComponentModel.DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), System.ComponentModel.Browsable(false)]
        public int ScoreColumn {
            get {
                return scoreColumn;
            }
            set {
                scoreColumn = value;
            }
        }

        // Update the size of the panel, because the description length changed or the size of the control changed.
        void UpdatePanelSize()
        {
            if (renderer == null || renderer.Description == null) {
                // No description. The panel is invisible.
                descriptionPanel.Size = new Size(0, 0);
                return;
            }

            Size oldSize = descriptionPanel.Size;

            // First, figure out the new box size based on the width of the control.
            // There are two cases -- if it fits without any scroll bars, or not.

            int panelWidth = Size.Width - scrollWidth;
            float boxSize;
            Size newSize;

            // Try first without scroll bars, decreasing the width until a scrollbar width is reached.
            int width = Size.Width;
            for (;;) {
                newSize = FitDescriptionToWidth(width, out boxSize);
                if (newSize.Width <= Size.Width && newSize.Height <= Size.Height) 
                    break;  // Fits!
                if (width > 1 && width > Size.Width - scrollWidth) {
                    --width;  // decrease width by one and try again.
                }
                else {
                    break; // we're done, we'll use scroll bars.
                }
            };

            renderer.CellSize = boxSize;

            VerticalScroll.SmallChange = (int) Math.Round(boxSize);

            if (oldSize != newSize)
                descriptionPanel.Size = newSize;
            if (oldSize.Width != newSize.Width)
                descriptionPanel.Invalidate();
        }

        // Try to fit the current description into the given width. Return the full size of the description
        // and the box size used.
        Size FitDescriptionToWidth(float width, out float boxSize)
        {
            boxSize = (width - (margin * 2)) / 8.0F;
            if (boxSize < minBoxSize)
                boxSize = minBoxSize;

            renderer.CellSize = boxSize;

            // Next, measure the size of the description and set the size of the panel to match.
            return Size.Round(renderer.Measure());
        }

        // Invalidate the given line of the description.
        void InvalidateLine(int line)
        {
            RectangleF rect = renderer.LineBounds(line, line);
            descriptionPanel.Invalidate(Util.Round(rect));
        }

        // Given a hit test, determine the location where the upper-left of the popup menu should be.
        // We position it to not obscure the symbol much, but still overlap the cell a bit.
        Point GetPopupMenuLocation(HitTestResult hitTest)
        {
            return new Point((int)Math.Round(hitTest.rect.Left + renderer.CellSize * 0.5F),
                             (int)Math.Round(hitTest.rect.Top + renderer.CellSize * 0.75F));
        }

        // Combine text from several lines.
        string CombineBoxTexts(int firstLine, int lastLine, int boxNumber, string combineWith)
        {
            string result = "";
            for (int l = firstLine; l <= lastLine; ++l) {
                if (result != "")
                    result += combineWith;
                result += (string) renderer.Description[l].boxes[boxNumber];
            }

            return result;
        }

        // Show the correct popup menu, give the box the user clicked.
        void PopupMenu(HitTestResult hitTest)
        {
            string text;

            // Get location for menu to appear.
            Point location = GetPopupMenuLocation(hitTest);

            // Save the line/box we are possibly changing
            popupKind = ChangeKind.None;     // will change this below if we actual pop something up!
            popupLine = hitTest.firstLine;
            popupBox = hitTest.box;

            switch (hitTest.kind) {
                case HitTestKind.NormalBox:
                    if (scoreColumn >= 0 && hitTest.box == scoreColumn) {
                        if (!(renderer.Description[hitTest.firstLine].boxes[0] is Symbol)) {
                            // In score courses, the score is in column A, so allow in-place editing of it, unless its the start triange.
                            popupKind = ChangeKind.Score;
                            popup.ShowPopup(8, (char)0, (char)0, false, MiscText.EnterScore, (string)renderer.Description[hitTest.firstLine].boxes[hitTest.box], 2, descriptionPanel, location);
                        }
                    }
                    else if (hitTest.box == 0) {
                        // Column A:
                        // We don't allow changing the sequence number, so no popup allowed
                    }
                    else if (hitTest.box == 1) {
                        // Column B
                        if (!(renderer.Description[hitTest.firstLine].boxes[0] is Symbol)) {
                            popupKind = ChangeKind.Code;
                            popup.ShowPopup(8, (char) 0, (char) 0, false, MiscText.EnterCode, (string) renderer.Description[hitTest.firstLine].boxes[1], 2, descriptionPanel, location);
                        }
                    }
                    else if (hitTest.box == 4) {
                        // Column E
                        popupKind = ChangeKind.DescriptionBox;
                        popup.ShowPopup(8, 'E', 'D', true, null, null, 0, descriptionPanel, location);
                    }
                    else if (hitTest.box == 5) {
                        // Column F
                        string initialText = "";
                        if (renderer.Description[hitTest.firstLine].boxes[5] is string && renderer.Description[hitTest.firstLine].boxes[5] != null)
                            initialText = (string) renderer.Description[hitTest.firstLine].boxes[5];
                        popupKind = ChangeKind.DescriptionBox;
                        popup.ShowPopup(8, 'F', (char) 0, true, MiscText.EnterDimensions, initialText, 4, descriptionPanel, location);
                    }
                    else {
                        // Column C, D, G, H
                        popupKind = ChangeKind.DescriptionBox;
                        popup.ShowPopup(8, (char)(hitTest.box + 'A'), (char)0, true, null, null, 0, descriptionPanel, location);
                    }
                    break;

                case HitTestKind.Directive:
                    Symbol current = renderer.Description[hitTest.firstLine].boxes[0] as Symbol;
                    if (current != null) {
                        char kind = current.Kind;       // Allow changing in the existing kind only.

                        // Only allow changing the crossing point or finish symbols.
                        if (kind == 'X' || kind == 'Z') {
                            popupKind = ChangeKind.Directive;
                            popup.ShowPopup(1, kind, (char)0, false, null, null, 0, descriptionPanel, location);
                        }
                    }
                    break;

                case HitTestKind.Title:
                    text = MiscText.EnterEventTitle;
                    popupKind = ChangeKind.Title;

                    popup.ShowPopup(8, (char)0, (char)0, false, text, CombineBoxTexts(hitTest.firstLine, hitTest.lastLine, 0, "|"), 8, descriptionPanel, location);
                    break;

                case HitTestKind.SecondaryTitle:
                    text = MiscText.EnterSecondaryTitle;
                    popupKind = ChangeKind.SecondaryTitle;

                    popup.ShowPopup(8, (char) 0, (char) 0, false, text, CombineBoxTexts(hitTest.firstLine, hitTest.lastLine, 0, "|"), 8, descriptionPanel, location);
                    break;

                case HitTestKind.Header:
                    if (hitTest.box == 0 && courseViewKind != CourseView.CourseViewKind.AllControls) {
                        // the course name. Can't change the "All Controls" name.
                        popupKind = ChangeKind.CourseName;
                        string courseName = (string) renderer.Description[hitTest.firstLine].boxes[0];
                        if (isCoursePart && courseName.Length > 2) {
                            // Remove the "-3" etc with the part number.
                            courseName = courseName.Substring(0, courseName.LastIndexOf('-'));
                        }
                        popup.ShowPopup(8, (char) 0, (char) 0, false, MiscText.EnterCourseName, courseName, 6, descriptionPanel, location);
                    }
                    else if (hitTest.box == 1  && courseViewKind == CourseView.CourseViewKind.Normal) {
                        // the length
                        string lengthText;
                        if (hasCustomLength)
                            lengthText = Util.RemoveSuffix((string)renderer.Description[hitTest.firstLine].boxes[1], "km");
                        else 
                            lengthText = "";  // automatically calculated length.

                        popupKind = ChangeKind.Length;
                        popup.ShowPopup(8, (char)0, (char)0, false, MiscText.EnterLength, lengthText, 4, descriptionPanel, location);
                    }
                    else if (hitTest.box == 2) {
                        // the climb
                        popupKind = ChangeKind.Climb;
                        popup.ShowPopup(8, (char) 0, (char) 0, false, MiscText.EnterClimb, Util.RemoveMeterSuffix((string) renderer.Description[hitTest.firstLine].boxes[2]), 4, descriptionPanel, location);
                    }
                    break;

                case HitTestKind.Key:
                    popupKind = ChangeKind.Key;
                    popup.ShowPopup(8, (char) 0, (char) 0, false, MiscText.EnterSymbolText, (string) renderer.Description[hitTest.firstLine].boxes[1], 8, descriptionPanel, location);
                    break;

                case HitTestKind.OtherTextLine:
                    popupKind = ChangeKind.TextLine;
                    popup.ShowPopup(8, (char)0, (char)0, false, MiscText.EnterTextLine, CombineBoxTexts(hitTest.firstLine, hitTest.lastLine, 0, "|"), 8, descriptionPanel, location);
                    break;

                default: Debug.Fail("bad hit test kind"); break;
            }
        }

        // Draw the highlight rectangle to show selection for a given line.
        private void DrawSelection(Graphics g, int firstLine, int lastLine, Rectangle clip)
        {
            if (firstLine >= 0 && lastLine >= 0) {
                Rectangle selectedRect = Util.Round(renderer.LineBounds(firstLine, lastLine));
                if (selectedRect.IntersectsWith(clip))
                    g.FillRectangle(selectionBrush, selectedRect);
            }
        }

        // Redraw the description.
        private void descriptionPanel_Paint(object sender, PaintEventArgs e)
        {
            if (renderer != null) {
                Graphics g = e.Graphics;

                // Draw the selected line.
                DrawSelection(g, firstSelectedLine, lastSelectedLine, e.ClipRectangle);

                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                renderer.RenderToGraphics(g, e.ClipRectangle);
            }
        }

        // The control is being re-layed out. Size the panel appropriately.
        private void DescriptionControl_Layout(object sender, LayoutEventArgs e)
        {
            UpdatePanelSize();
        }

        // The mouse is clicked on the panel. Change the selection.
        // Show the popup menu if appropriate.
        private void descriptionPanel_MouseDown(object sender, MouseEventArgs e)
        {
            HitTestResult hitTest = renderer.HitTest(e.Location);
            if (hitTest.firstLine < 0)
                return;             // clicked outside the description.

            bool alreadySelected = (hitTest.firstLine == firstSelectedLine);

            if (!alreadySelected) {
                // Move the selected line.
                SetSelection(hitTest.firstLine, hitTest.lastLine);
                if (SelectedIndexChange != null)
                    SelectedIndexChange(this, EventArgs.Empty);
            }

            // If the left-click the selected line, or right-click anywhere, then possible show a popup menu.
            if ((e.Button == MouseButtons.Right || alreadySelected) && hitTest.kind != HitTestKind.None) 
            {
                // Clicked on the selected line, in a potentially interesting place. Show a menu (maybe).
                PopupMenu(hitTest);
            }
        }

        // The user selected something in the popup menu. Fire event indicating the change.
        void popup_Selected(object sender, SymbolPopupEventArgs eventArgs)
        {
            Debug.Assert(popupKind != ChangeKind.None);

            if (Change != null) {
                object newValue = null;
                if (eventArgs.TextSelected != null)
                    newValue = eventArgs.TextSelected;
                else if (eventArgs.SymbolSelected != null)
                    newValue = eventArgs.SymbolSelected;

                Change(this, popupKind, popupLine, popupBox, newValue);
            }
        }

        // The popup was canceled.
        void popup_Canceled(object sender, EventArgs e)
        {
            popupKind = ChangeKind.None;
        }

    }
}
