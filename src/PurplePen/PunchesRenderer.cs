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
using PurplePen.Graphics2D;
using PurplePen.MapModel;

namespace PurplePen
{
    // Renders a course view into the punch card with all the punches. Also allows checking to see if
    // the punches are missing.
    class PunchesRenderer: IPrintableRectangle, IDisposable
    {
        private float margin = 3;           
        private float cellSize = 30;
        EventDB eventDB;
        private CourseView courseView;
        PunchcardFormat punchcardFormat;

        object blackBrush;
        object thinPen, thickPen;
        ITextMetrics textMetrics;

        bool disposed;

        public PunchesRenderer(EventDB eventDB)
        {
            this.eventDB = eventDB;
        }

        // The margin around the side, in device units.
        public float Margin
        {
            get { return margin; }
            set { margin = value; }
        }

        // The side of a single box in the description, in device units.
        public float CellSize
        {
            get { return cellSize; }
            set { cellSize = value; }
        }

        // The course to render.
        public CourseView CourseView
        {
            get { return courseView; }
            set { courseView = value; }
        }

        // How to render the punch cards.
        public PunchcardFormat PunchcardFormat
        {
            get { return punchcardFormat; }
            set { punchcardFormat = value; }
        }

        // Measure the size of the description. Includes the margins.
        public SizeF Measure()
        {
            Size sizeInBoxes = Boxes;
            SizeF size = new SizeF(cellSize * sizeInBoxes.Width + margin * 2, cellSize * sizeInBoxes.Height + margin * 2);
            return size;
        }

        // Measure the size of the descriptions in boxes. Includes the title line
        public Size Boxes
        {
            get
            {
                return new Size(punchcardFormat.boxesAcross, NumberOfLines(GetAllBoxes()) + 1);        // +1 for the title line
            }
        }

        // Get number of lines that will be on the punch card, not including the title line.
        int NumberOfLines(List<CourseView.ControlView> boxes)
        {
            // Number of punch lines on the punch card may be increased if needed, but will always be at least punchcardFormat.boxesDown.
            int linesNeeded = (boxes.Count + punchcardFormat.boxesAcross - 1) / punchcardFormat.boxesAcross;
            int lineCount = Math.Max(punchcardFormat.boxesDown, linesNeeded);

            return lineCount;
        }

        // Create pens and fonts we use.
        void CreateObjects(IGraphicsTarget g)
        {
            CmykColor black = CmykColor.FromCmyk(0, 0, 0, 1);
            blackBrush = new object();
            g.CreateSolidBrush(blackBrush, black);
            thinPen = new object();
            g.CreatePen(thinPen, black, PunchcardAppearance.thinLine, LineCapMode.Flat, LineJoinMode.Miter, 5F);
            thickPen = new object();
            g.CreatePen(thickPen, black, PunchcardAppearance.thickLine, LineCapMode.Flat, LineJoinMode.Miter, 5F);

            textMetrics = new GDIPlus_TextMetrics();        }

        // Dispose the pens and fonts we use.
        void DisposeObjects()
        {
            blackBrush = null;
            thinPen = null;
            thickPen = null;
            if (textMetrics != null) {
                textMetrics.Dispose();
                textMetrics = null;
            }
        }

        // Get all the boxes we are going to fill into an array.
        // This is just all the regular controls from the course view.
        List<CourseView.ControlView> GetAllBoxes()
        {
            return
                courseView.ControlViews.FindAll(delegate(CourseView.ControlView ctlView) 
                { 
                    return ctlView.controlId.IsNotNone && eventDB.GetControl(ctlView.controlId).kind == ControlPointKind.Normal;
                });
        }


        // Render the punchcard onto the given graphics at (0,0). Only draw the parts that lie within
        // the clip rect.
        void Render(IGraphicsTarget g, int startLine, int countLines)
        {
            PointF upperLeft = new PointF(margin, margin);   // upper left of the current line.

            List<CourseView.ControlView> boxes = GetAllBoxes();           // mapping from box number to control views.
            int lineCount = NumberOfLines(boxes);

            CreateObjects(g);

            try {
                for (int line = 0; line <= lineCount; ++line) {
                    if (line >= startLine && line < startLine + countLines) {
                        Matrix matrixNew;

                        // Set transform so the each cell is 100x100, and the origin of the line is at (0,0).
                        matrixNew = new Matrix();
                        matrixNew.Translate(upperLeft.X, upperLeft.Y);
                        matrixNew.Scale(cellSize / 100.0F, cellSize / 100.0F);
                        g.PushTransform(matrixNew);

                        // Draw the line.
                        RenderLine(g, boxes, line, (line == lineCount || line == startLine + countLines - 1));

                        g.PopTransform();

                        upperLeft.Y += cellSize;
                    }
                }
            }
            finally {
                DisposeObjects();
            }
        }

        // Render one punch box
        private void RenderPunchBox(IGraphicsTarget g, CourseView.ControlView controlView, RectangleF rect)
        {
            // Draw the ordinal number, if there is one.
            if (controlView.ordinal > 0) {
                DrawSingleLineText(g, controlView.ordinal.ToString(), PunchcardAppearance.controlNumberFont, new PointF(rect.Left + 6, rect.Top + 3), StringAlignment.Near, StringAlignment.Near);
            }

            // If it's a score course, and a score has been defined, then put the score.
            if (courseView.Kind == CourseView.CourseViewKind.Score) {
                int points = 0;
                if (controlView.courseControlIds[0].IsNotNone)
                    points = eventDB.GetCourseControl(controlView.courseControlIds[0]).points;

                if (points > 0) {
                    DrawSingleLineText(g, points.ToString(), PunchcardAppearance.scoreFont, new PointF((rect.Left + rect.Right) / 2, rect.Top + 3), StringAlignment.Center, StringAlignment.Near);
                }
            }

            // Draw the code.
            string code = string.Format("({0})", eventDB.GetControl(controlView.controlId).code);
            DrawSingleLineText(g, code, PunchcardAppearance.codeFont, new PointF(rect.Right - 5F, rect.Top + 3), StringAlignment.Far, StringAlignment.Near);

            // Draw the punch pattern.
            RectangleF punchRect = RectangleF.FromLTRB(rect.Left + 20F, rect.Top + 27.5F, rect.Right - 20F, rect.Bottom - 12.5F);
            
            PunchPattern pattern = eventDB.GetControl(controlView.controlId).punches;
            if (pattern != null) 
                DrawPattern(g, pattern, punchRect);
        }

        // Draw a pattern of dots. The center of the edge dots are on the edges of the given rectangle, so the dots
        // protrude out a dot radius from the rectangle.
        private void DrawPattern(IGraphicsTarget g, PunchPattern pattern, RectangleF punchRect)
        {
            float dxPerDot = (pattern.size > 1) ? punchRect.Width / (pattern.size - 1) : 0;
            float dyPerDot = (pattern.size > 1) ? punchRect.Height / (pattern.size - 1) : 0;
            float r = PunchcardAppearance.dotRadius;

            for (int row = 0; row < pattern.size; ++row) {
                for (int col = 0; col < pattern.size; ++col) {
                    if (pattern.dots[row, col]) {
                        float xCenter = punchRect.Left + dxPerDot * col;
                        float yCenter = punchRect.Top + dyPerDot * row;
                        g.FillEllipse(blackBrush, new PointF(xCenter, yCenter), r, r);
                    }
                }
            }
        }

        // Render a single line of the punchcard. "lastLine" is true if this is the last line (draws the bottom line). 
        private void RenderLine(IGraphicsTarget g, List<CourseView.ControlView> boxes, int line, bool lastLine)
        {
            int lineCount = NumberOfLines(boxes);

            // Draw top line.
            float fullWidth = punchcardFormat.boxesAcross * 100;
            if (line == 0 || line == 1) {
                g.DrawLine(thickPen, new PointF(0, 0), new PointF(fullWidth, 0));
            }
            else {
                g.DrawLine(thinPen, new PointF(0, 0), new PointF(fullWidth, 0));
            }

            // Draw bottom line, if requested
            if (lastLine)
                g.DrawLine(thickPen, new PointF(0, 100), new PointF(fullWidth, 100));

            // Draw side lines.
            float lineTop = -PunchcardAppearance.thickLine / 2;
            float lineBottom = 100 + PunchcardAppearance.thickLine / 2;
            g.DrawLine(thickPen, new PointF(0, lineTop), new PointF(0, lineBottom));
            g.DrawLine(thickPen, new PointF(fullWidth, lineTop), new PointF(fullWidth, lineBottom));

            if (line == 0) {
                // Draw title
                RectangleF rect = new RectangleF(0, 0, fullWidth, 100);
                StringFormat stringFormat = new StringFormat(StringFormat.GenericDefault);
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;
                stringFormat.FormatFlags = StringFormatFlags.NoWrap;

                DrawSingleLineText(g, courseView.CourseFullName, PunchcardAppearance.titleFont, rect.Center(), StringAlignment.Center, StringAlignment.Center);
            }
            else {
                // Draw grid lines and the boxes.
                for (int col = 0; col < punchcardFormat.boxesAcross; ++col) {
                    if (col != 0)
                        g.DrawLine(thinPen, new PointF(100 * col, lineTop), new PointF(100 * col, lineBottom));

                    RectangleF boxRect = new RectangleF(100 * col, 0, 100, 100);

                    // Figure out the box number (0 based!)
                    int lineStart, boxWithinLine, boxNumber;
                    if (punchcardFormat.topToBottom)
                        lineStart = (line - 1) * punchcardFormat.boxesAcross;
                    else
                        lineStart = (lineCount - line) * punchcardFormat.boxesAcross;
                    if (punchcardFormat.leftToRight)
                        boxWithinLine = col;
                    else
                        boxWithinLine = (punchcardFormat.boxesAcross - col - 1);
                    boxNumber = lineStart + boxWithinLine;

                    if (boxNumber < boxes.Count)
                        RenderPunchBox(g, boxes[boxNumber], boxRect);
                }
            }
        }

        private void DrawSingleLineText(IGraphicsTarget g, string text, FontDesc fontDesc, PointF pt, StringAlignment horizAlignment, StringAlignment vertAlignment)
        {
            object font = new object();
            g.CreateFont(font, fontDesc.Name, fontDesc.EmHeight, fontDesc.TextEffects);

            ITextFaceMetrics fontMetrics = textMetrics.GetTextFaceMetrics(fontDesc.Name, fontDesc.EmHeight, fontDesc.TextEffects);
            SizeF size = fontMetrics.GetTextSize(text);

            switch (horizAlignment) {
                case StringAlignment.Near:
                    break;
                case StringAlignment.Center:
                    pt.X = pt.X - size.Width / 2F; break;
                case StringAlignment.Far:
                    pt.X = pt.X - size.Width; break;
            }

            switch (vertAlignment) {
                case StringAlignment.Near:
                    break;
                case StringAlignment.Center:
                    pt.Y = pt.Y - size.Height / 2F; break;
                case StringAlignment.Far:
                    pt.Y = pt.Y - size.Height; break;
            }

            g.DrawText(text, font, blackBrush, pt);

            fontMetrics.Dispose();
        }

        public void Draw(IGraphicsTarget grTarget, float x, float y, int startLine, int countLines)
        {
            Matrix transform = new Matrix();
            transform.Translate(x, y);
            grTarget.PushTransform(transform);
            Render(grTarget, startLine, countLines);
            grTarget.PopTransform();
        }


        // Dispose pattern - release any managed resources we may still be holding.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing) {
                // Dispose managed resources
                DisposeObjects();
            }

            disposed = true;
        }
    }

    // The format of a punch card.
    public class PunchcardFormat: ICloneable
    {
        public bool leftToRight;             // left-to-right or righ-to-left
        public bool topToBottom;         // top-to-bottom or bottom-to-top
        public int boxesAcross;               // number of boxes across
        public int boxesDown;                 // number of boxes down;

        // Set default format.
        public PunchcardFormat()
        {
            leftToRight = PunchcardAppearance.defaultLeftToRight;
            topToBottom = PunchcardAppearance.defaultTopToBottom;
            boxesAcross = PunchcardAppearance.defaultBoxesAcross;
            boxesDown = PunchcardAppearance.defaultBoxesDown;
        }

        public override bool Equals(object obj)
        {
            if (! (obj is PunchcardFormat))
                return false;
            PunchcardFormat other = (PunchcardFormat) obj;

            if (other.leftToRight != leftToRight)
                return false;
            if (other.topToBottom != topToBottom)
                return false;
            if (other.boxesAcross != boxesAcross)
                return false;
            if (other.boxesDown != boxesDown)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return leftToRight.GetHashCode() ^ topToBottom.GetHashCode() ^ boxesAcross.GetHashCode() ^ boxesDown.GetHashCode();
        }
   
        public object Clone()
        {
            PunchcardFormat other = new PunchcardFormat();
            other.boxesAcross = boxesAcross;
            other.boxesDown = boxesDown;
            other.leftToRight = leftToRight;
            other.topToBottom = topToBottom;
            return other;
        }
    }
}
