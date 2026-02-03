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
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Drawing;

using PurplePen.MapModel;
using System.Globalization;
using PurplePen.Graphics2D;

namespace PurplePen
{
    // What did we hit?
    enum HitTestKind
    {
        None,               // didn't hit anything
        Title,              // hit a title (box always 0)
        SecondaryTitle,              // hit a secondary title (box always 0)
        Header,             // hit the header (name, length, climb, or total controls -- box is 0, 1, 2).
        NormalBox,          // hit a normal box (box indicates A-H)
        NormalText,         // hit the text part of a normal box line (box is not valid)
        Directive,          // hit a directive (box always 0)
        DirectiveText,      // hit the text part of a directive (box is not valid)
        OtherTextLine,              // hit a text line (box is not valid)
        Key                      // hit the key for custom special items at the bottom (box always 0)
    }

    // Indicates the hittest of a hit test operation.
    struct HitTestResult
    {
        public HitTestKind kind;   // What did we hit?
        public int firstLine, lastLine;          // What line(s) of the description?
        public int box;           // Which box in the description?
        public RectangleF rect;    // What are the bounds of that box?
    }

    /// <summary>
    /// Renders a CourseView onto a Graphics.
    /// </summary>
    class DescriptionRenderer: IPrintableRectangle, ICloneable
    {
        private SymbolDB symbolDB;

        const int NUM_FONTS = 9;
        const int TITLE_FONT = 0;
        const int COLUMNA_FONT = 1;
        const int COLUMNB_FONT = 2;
        const int COLUMNF_FONT = 3;
        const int COLUMNF_DOUBLE_FONT = 4;
        const int DIRECTIVE_FONT = 5;
        const int TEXT_FONT = 6;
        const int KEY_FONT = 7;
        const int TEXTLINE_FONT = 8;

        private const float columnGap = 0.60F;  // size of gap between columns as fraction of cell size.

        private FontDesc[] fontDescs = new FontDesc[NUM_FONTS];
        private StringAlignment[] fontAlignments = new StringAlignment[NUM_FONTS];
        private object[] fonts = new object[NUM_FONTS];
        private object thickPen, thinPen;

        // Note: If you add new state -- be sure to update Clone().
        private float margin = 3;           
        private float cellSize = 30;
        private int numColumns = 1;
        DescriptionLine[] description;
        DescriptionKind descriptionKind;
        bool columnHScore; // If true, show score in column H for text-only descriptions.

        private bool replaceMultiplySign;         // If true, replace 'x' in column F with multiply sign.


        public DescriptionRenderer(SymbolDB symbolDB)
        {
            this.symbolDB = symbolDB;
        }

        public object Clone()
        {
            DescriptionRenderer n = new DescriptionRenderer(symbolDB);
            n.margin = this.margin;
            n.cellSize = this.cellSize;
            n.numColumns = this.numColumns;
            n.description = this.description;
            n.columnHScore = this.columnHScore;
            n.descriptionKind = this.descriptionKind;
            n.replaceMultiplySign = this.replaceMultiplySign;
            return n;
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

        public int NumberOfColumns
        {
            get { return numColumns; }
            set { numColumns = value; }
        }

        // The description to render.
        public DescriptionLine[] Description
        {
            get { return description; }
            set { description = value; }
        }

        // The kind of description to render (symbols, text, both).
        public DescriptionKind DescriptionKind
        {
            get { return descriptionKind; }
            set { descriptionKind = value; }
        }

        // Reserve column H for score, even in text-only option.
        public bool ColumnHScore {
            get { return columnHScore; }
            set { columnHScore = value; }
        }

        public float ColumnWidth
        {
            get { return cellSize * WidthInCells(); }
        }

        public float ColumnGap
        {
            get { return cellSize * columnGap; }
        }

        // Measure the size of the description. Includes the margins and multi-column splitting.
        public SizeF Measure()
        {
            SizeF size = new SizeF(ColumnWidth * numColumns + ColumnGap * (numColumns - 1) + margin * 2, cellSize * ColumnLengthInCells + margin * 2);
            return size;
        }

        // Measure the size of the descriptions in boxes. Does not take column splitting into account for multi-column description.
        public Size Boxes
        {
            get
            {
                return new Size(WidthInCells(), description.Length);
            }
        }

        // Measure the bounds of one line. Does not include margings or line widths.
        // If multicolumns, then firstline must equal lastLine.
        public RectangleF LineBounds(int firstLine, int lastLine)
        {
            Debug.Assert(firstLine == lastLine || numColumns == 1);

            int row, col;
            ColumnAndRow(firstLine, out row, out col);
            RectangleF rect = new RectangleF((WidthInCells() + columnGap) * cellSize * col, row * cellSize, WidthInCells() * cellSize, cellSize * (lastLine - firstLine + 1));
            rect.Offset(margin, margin);
            return rect;
        }

        // Measure the bounds of one box.
        public RectangleF BoxBounds(int firstLine, int lastLine, int startBox, int endBox)
        {
            Debug.Assert(firstLine == lastLine || numColumns == 1);

            RectangleF rect = new RectangleF(startBox * cellSize, firstLine * cellSize, cellSize * (endBox - startBox + 1), cellSize * (lastLine - firstLine + 1));
            rect.Offset(margin, margin);
            return rect;
         }

         // Render the description onto the given graphics at (0,0). Only draw the parts that lie within
         // the clip rect.
        public void RenderToGraphics(Graphics g, RectangleF clipRect)
        {
            IRenderer renderer = new GraphicsTargetRenderer(new GDIPlus_GraphicsTarget(g), new GDIPlus_TextMetrics(), CmykColor.FromCmyk(0, 0, 0, 1));
            replaceMultiplySign = true;
            Render(renderer, clipRect, 0, description.Length);
        }

        void IPrintableRectangle.Draw(IGraphicsTarget grTarget, float x, float y, int startLine, int countLines)
        {
            IRenderer renderer = new GraphicsTargetRenderer(grTarget, new GDIPlus_TextMetrics(), CmykColor.FromCmyk(0, 0, 0, 1));

            Matrix transform = new Matrix();
            transform.Translate(x, y);

            grTarget.PushTransform(transform);
            replaceMultiplySign = true;
            Render(renderer, new RectangleF(-100000, -100000, 200000, 200000), startLine, countLines);

            grTarget.PopTransform();
        }

        // Render the description into the given map at the given location in the given color.
        public void RenderToMap(Map map, SymColor color, PointF point, Dictionary<object, SymDef> dict)
        {
            IRenderer renderer = new MapRenderer(map, color, dict);

            // Set the correct transform.
            Matrix mat = new Matrix();
            mat.Scale(1, -1);      // flip Y axis.
            mat.Translate(point.X, -point.Y);
            renderer.PushTransform(mat);
            
            // White out the background.
            SizeF size = Measure();
            PointKind[] kinds = { PointKind.Normal, PointKind.Normal, PointKind.Normal, PointKind.Normal, PointKind.Normal};
            PointF[] pts = {Geometry.TransformPoint(new PointF(0, 0), mat), Geometry.TransformPoint(new PointF(size.Width, 0), mat),Geometry.TransformPoint(new PointF(size.Width, size.Height), mat),
                Geometry.TransformPoint(new PointF(0, size.Height), mat),Geometry.TransformPoint(new PointF(0, 0), mat) };
            SymPath path = new SymPath(pts, kinds);
            AreaSymbol whiteout = new AreaSymbol((AreaSymDef) dict[CourseLayout.KeyWhiteOut], new SymPathWithHoles(path, null), 0, new PointF());
            map.AddSymbol(whiteout);

            replaceMultiplySign = false;   // OCAD doesn't handle the multiple character well.
            Render(renderer, new RectangleF(-100000, -100000, 200000, 200000), 0, description.Length);
        }

        // Render the description onto the given renderer at (0,0). Only draw the parts that lie within
        // the clip rect.
        void Render(IRenderer renderer, RectangleF clipRect, int startLine, int countLines)
        {
            int thickLineCounter = 0;
            PointF upperLeft;   // upper left of the current line.

            // Thickness of a thick line, in device units.
            float thickLineWidth = DescriptionAppearance.thickDescriptionLine * (float)cellSize / 100.0F;

            CreateObjects(renderer, cellSize);

            try {
                for (int line = 0; line < description.Length; ++line) {
                    if (line >= startLine && line < startLine + countLines) {
                        // Figure out if this line needs to be drawn.
                        RectangleF bounds = LineBounds(line, line);
                        bounds.Inflate(thickLineWidth, thickLineWidth);

                        if (bounds.IntersectsWith(clipRect)) {
                            Matrix matrixNew;
                            int row, col;
                            int nextRow, nextCol;

                            ColumnAndRow(line - startLine, out row, out col);
                            ColumnAndRow(line - startLine + 1, out nextRow, out nextCol);
                            upperLeft = new PointF(margin + (col * (WidthInCells() + columnGap) * cellSize), margin + row * cellSize);
                            if (col > 0 && row == 0)
                                thickLineCounter = 0;

                            // Set transform so the each cell is 100x100, and the origin of the line is at (0,0).
                            matrixNew = new Matrix();
                            matrixNew.Translate(upperLeft.X, upperLeft.Y);
                            matrixNew.Scale(cellSize / 100.0F, cellSize / 100.0F);
                            renderer.PushTransform(matrixNew);

                            // Transform the clip rectangle into world coordinate relative to the line.
                            RectangleF clipWorld = new RectangleF((clipRect.X - upperLeft.X) * 100.0F / cellSize,
                                                                  (clipRect.Y - upperLeft.Y) * 100.0F / cellSize,
                                                                  clipRect.Width * 100.0F / cellSize,
                                                                  clipRect.Height * 100.0F / cellSize);

                            // Draw the line.
                            // Don't draw a top line if it's a title/secondary and so was the previous.
                            bool lastLine = (line == description.Length - 1 || line == startLine + countLines - 1 || nextCol != col);
                            bool drawThickLine = (thickLineCounter == 0 || line == startLine);
                            bool noTopLine = (line != 0) && (DescriptionLine.NoBoundaryBetween(description[line], description[line - 1]));
                            RenderLine(renderer, description[line], descriptionKind, lastLine,  drawThickLine, noTopLine, clipWorld);

                            renderer.PopTransform();
                        }
                    }

                    UpdateThickLineCounter(description[line], DescriptionKind, ref thickLineCounter);
                }
            }
            finally {
                DisposeObjects();
            }
        }

        // Find line bounds of lines of a given kind.
        void LineBounds(int line, out int firstLine, out int lastLine)
        {
            DescriptionLine descLine = description[line];

            firstLine = line;
            while (firstLine > 0 && DescriptionLine.NoBoundaryBetween(description[firstLine - 1], descLine))
                --firstLine;

            lastLine = line;
            while (lastLine < description.Length - 1 && DescriptionLine.NoBoundaryBetween(description[lastLine + 1], descLine))
                ++lastLine;
        }

        // Hit test a point (in device coordinates) so we know what was clicked on. 
        // Currently only supports single column.
        public HitTestResult HitTest(PointF point)
        {
            Debug.Assert(numColumns == 1);

            HitTestResult result = new HitTestResult();

            // Compensate for the margin.
            point.X -= margin;
            point.Y -= margin;

            // Get total size, not including margin.
            int width = (int) (WidthInCells() * cellSize);
            int height = (int) (description.Length * cellSize);

            if (point.X < 0 || point.X >= width || point.Y < 0 || point.Y >= height) {
                // Outside the bounds of the description.
                result.kind = HitTestKind.None;
                result.firstLine = result.lastLine = -1;
                result.box = -1;
                result.rect = new RectangleF();
                return result;
            }

            // What line, column are we on?
            int iLine = (int) (point.Y / cellSize);
            Debug.Assert(iLine >= 0 && iLine < description.Length);
            int iCol = (int)(point.X / cellSize);
            Debug.Assert(iCol >= 0 && iCol < WidthInCells());
            result.firstLine = result.lastLine = iLine;
            DescriptionLineKind lineKind = description[iLine].kind;

            switch (lineKind) {
                case DescriptionLineKind.Title:
                    result.kind = HitTestKind.Title;
                    result.box = 0;
                    LineBounds(iLine, out result.firstLine, out result.lastLine);
                    result.rect = new RectangleF(0, result.firstLine * cellSize, width, cellSize * (result.lastLine - result.firstLine + 1));
                    break;

                case DescriptionLineKind.SecondaryTitle:
                    result.kind = HitTestKind.SecondaryTitle;
                    result.box = 0;
                    LineBounds(iLine, out result.firstLine, out result.lastLine);
                    result.rect = new RectangleF(0, result.firstLine * cellSize, width, cellSize * (result.lastLine - result.firstLine + 1));
                    break;

                case DescriptionLineKind.Header2Box:
                    if (iCol < 3) {
                        result.kind = HitTestKind.Header;
                        result.box = 0;
                        result.rect = new RectangleF(0, iLine * cellSize, 3 * cellSize, cellSize);
                    }
                    else if (iCol < 8) {
                        result.kind = HitTestKind.Header;
                        result.box = 1;
                        result.rect = new RectangleF(3 * cellSize, iLine * cellSize, 5 * cellSize, cellSize);
                    }
                    else {
                        result.kind = HitTestKind.None;
                        result.box = -1;
                        result.rect = new RectangleF(8 * cellSize, iLine * cellSize, 5 * cellSize, cellSize);
                    }
                    break;

                case DescriptionLineKind.Header3Box:
                    if (iCol < 3) {
                        result.kind = HitTestKind.Header;
                        result.box = 0;
                        result.rect = new RectangleF(0, iLine * cellSize, 3 * cellSize, cellSize);
                    }
                    else if (iCol < 6) {
                        result.kind = HitTestKind.Header;
                        result.box = 1;
                        result.rect = new RectangleF(3 * cellSize, iLine * cellSize, 3 * cellSize, cellSize);
                    }
                    else if (iCol < 8) {
                        result.kind = HitTestKind.Header;
                        result.box = 2;
                        result.rect = new RectangleF(6 * cellSize, iLine * cellSize, 2 * cellSize, cellSize);
                    }
                    else {
                        result.kind = HitTestKind.None;
                        result.box = -1;
                        result.rect = new RectangleF(8 * cellSize, iLine * cellSize, 5 * cellSize, cellSize);
                    }
                    break;

                case DescriptionLineKind.Normal:
                    if (descriptionKind == DescriptionKind.SymbolsAndText && iCol >= 8) {
                        result.kind = HitTestKind.NormalText;
                        result.box = -1;
                        result.rect = new RectangleF(8 * cellSize, iLine * cellSize, 5 * cellSize, cellSize);
                    }
                    else if (descriptionKind == DescriptionKind.Text && iCol >= 2 && !(columnHScore && iCol == 7)) {
                        result.kind = HitTestKind.NormalText;
                        result.box = -1;
                        result.rect = new RectangleF(2 * cellSize, iLine * cellSize, (columnHScore ? 5 : 6) * cellSize, cellSize);
                    }
                    else {
                        result.kind = HitTestKind.NormalBox;
                        result.box = iCol;
                        result.rect = new RectangleF(iCol * cellSize, iLine * cellSize, cellSize, cellSize);
                    }
                    break;

                case DescriptionLineKind.Directive:
                    if (descriptionKind == DescriptionKind.SymbolsAndText && iCol >= 8) {
                        result.kind = HitTestKind.DirectiveText;
                        result.box = -1;
                        result.rect = new RectangleF(8 * cellSize, iLine * cellSize, 5 * cellSize, cellSize);
                    }
                    else if (descriptionKind == DescriptionKind.Text) {
                        result.kind = HitTestKind.DirectiveText;
                        result.box = -1;
                        result.rect = new RectangleF(0, iLine * cellSize, 8 * cellSize, cellSize);
                    }
                    else {
                        result.kind = HitTestKind.Directive;
                        result.box = 0;
                        result.rect = new RectangleF(0, iLine * cellSize, 8 * cellSize, cellSize);
                    }
                    break;

                case DescriptionLineKind.Text:
                    result.kind = HitTestKind.OtherTextLine;
                    result.box = 0;
                    LineBounds(iLine, out result.firstLine, out result.lastLine);
                    result.rect = new RectangleF(0, result.firstLine * cellSize, width, cellSize * (result.lastLine - result.firstLine + 1));
                    break;

                case DescriptionLineKind.Key:
                    result.kind = HitTestKind.Key;
                    result.box = 0;
                    result.rect = new RectangleF(0, iLine * cellSize, width, cellSize);
                    break;

                default:
                    Debug.Fail("bad line kind");
                    return result;
            }

            result.rect.Offset(margin, margin);     // recompensate for the margin.
            return result;
        }

        // Get the width in cells -- either 8 or 13.
        int WidthInCells()
        {
            return (descriptionKind == DescriptionKind.SymbolsAndText) ? 13 : 8;
        }

        // Number of boxes down a full column.
        public int ColumnLengthInCells
        {
            get
            {
                return (description.Length + (numColumns - 1)) / numColumns;
            }
        }

        // Taking multi-columns into account, figure out the column and row
        private void ColumnAndRow(int line, out int row, out int column)
        {
            if (numColumns == 1) {
                row = line;
                column = 0;
            }
            else {
                column = 0;
                int numberOfLongColumns = description.Length % numColumns;
                if (numberOfLongColumns == 0)
                    numberOfLongColumns = numColumns;
                int firstColumnLength = ColumnLengthInCells;
                while (line >= ((column < numberOfLongColumns) ? firstColumnLength : firstColumnLength - 1)) {
                    line -= (column < numberOfLongColumns) ? firstColumnLength : firstColumnLength - 1;
                    ++column;
                }
                row = line;
            }
        }

        // Create the drawing objects we need.
        void CreateObjects(IRenderer renderer, float cellSize)
        {
            Matrix matrixNew;

            // Set transform so that the each cell is 100x100. The objects must be created
            // with the same transform (except translations) that they are drawn in.
            matrixNew = new Matrix();
            matrixNew.Scale(cellSize / 100.0F, cellSize / 100.0F);
            renderer.PushTransform(matrixNew);

            thickPen = renderer.CreatePen(DescriptionAppearance.thickDescriptionLine, LineJoinMode.Miter, LineCapMode.Flat);
            thinPen = renderer.CreatePen(DescriptionAppearance.thinDescriptionLine, LineJoinMode.Miter, LineCapMode.Flat);

            fontDescs[TITLE_FONT] = DescriptionAppearance.titleFont;                                        fontAlignments[TITLE_FONT] = StringAlignment.Center;
            fontDescs[COLUMNA_FONT] = DescriptionAppearance.columnAFont;                          fontAlignments[COLUMNA_FONT] = StringAlignment.Center;
            fontDescs[COLUMNB_FONT] = DescriptionAppearance.columnBFont;                          fontAlignments[COLUMNB_FONT] = StringAlignment.Center;
            fontDescs[COLUMNF_FONT] = DescriptionAppearance.columnFFont;                          fontAlignments[COLUMNF_FONT] = StringAlignment.Center;
            fontDescs[COLUMNF_DOUBLE_FONT] = DescriptionAppearance.columnFSmallFont;   fontAlignments[COLUMNF_DOUBLE_FONT] = StringAlignment.Center;
            fontDescs[DIRECTIVE_FONT] = DescriptionAppearance.directiveFont;                        fontAlignments[DIRECTIVE_FONT] = StringAlignment.Center;
            fontDescs[TEXT_FONT] = DescriptionAppearance.textFont;                                        fontAlignments[TEXT_FONT] = StringAlignment.Near;
            fontDescs[KEY_FONT] = DescriptionAppearance.keyFont;                                           fontAlignments[KEY_FONT] = StringAlignment.Near;
            fontDescs[TEXTLINE_FONT] = DescriptionAppearance.textLineFont;                          fontAlignments[TEXTLINE_FONT] = StringAlignment.Near;

            for (int i = 0; i < NUM_FONTS; ++i)
                fonts[i] = renderer.CreateFont(fontDescs[i].Name, fontDescs[i].EmHeight, fontDescs[i].Bold, fontDescs[i].Italic, fontAlignments[i]);

            renderer.PopTransform();
        }

        // Dispose one object.
        void DisposeObject(object obj)
        {
            IDisposable dispose = obj as IDisposable;
            if (dispose != null)
                dispose.Dispose();
        }

        // Dispose the drawing objects.
        void DisposeObjects()
        {
            DisposeObject(thickPen); thickPen = null;
            DisposeObject(thinPen); thinPen = null;
            for (int i = 0; i < NUM_FONTS; ++i) {
                DisposeObject(fonts[i]);
                fonts[i] = null;
            }
        }

        // Render some text with the given font inside the given rectangle. Either center or left flush.
        // If the rectange doesn't intersect the clip rectangle, do nothing. Render a single line of text
        private void RenderSingleLineText(IRenderer renderer, int fontNumber, StringAlignment alignment, string s, float left, float top, float right, float bottom, RectangleF clipRect)
        {
            if (s == null || s == "")
                return;

            RectangleF rect = new RectangleF(left, top, right - left, bottom - top);
            if (!rect.IntersectsWith(clipRect))
                return;

            // Measure to make sure that the text fits.
            float width = renderer.MeasureSingleLineText(fonts[fontNumber], s, rect, alignment);

            if (width <= rect.Width) {
                // Text fits OK.
                renderer.DrawSingleLineText(fonts[fontNumber], s, rect, alignment);
            }
            else {
                // Text is too big with normal font. Scale it down.
                float scaleFactor = (rect.Width / width) * 0.95F;  // scale factor so that it fits.
                object scaledFont = renderer.CreateFont(fontDescs[fontNumber].Name, fontDescs[fontNumber].EmHeight * scaleFactor, fontDescs[fontNumber].Bold, fontDescs[fontNumber].Italic, fontAlignments[fontNumber]);
                renderer.DrawSingleLineText(scaledFont, s, rect, alignment);
                DisposeObject(scaledFont);
            }
        }

        // Render some text with the given font inside the given rectangle. Either center or left flush.
        // If the rectange doesn't intersect the clip rectangle, do nothing. Word wrap if needed.
        private void RenderWrappedText(IRenderer renderer, int fontIndex, StringAlignment alignment, string s, float left, float top, float right, float bottom, RectangleF clipRect)
        {
            if (s == null || s == "")
                return;

            RectangleF rect = new RectangleF(left, top, right - left, bottom - top);
            if (!rect.IntersectsWith(clipRect))
                return;

            object currentFont = fonts[fontIndex];
            float currentHeight = fontDescs[fontIndex].EmHeight;

            // Keep shrinking the font by 5% until it fits.
            bool fits;
            do {
                fits = renderer.WrappedTextFits(currentFont, s, rect, alignment);
                if (!fits) {
                    currentHeight *= 0.95F;
                    currentFont = renderer.CreateFont(fontDescs[fontIndex].Name, currentHeight, fontDescs[fontIndex].Bold, fontDescs[fontIndex].Italic, fontAlignments[fontIndex]);
                }
            } while (!fits);

            renderer.DrawWrappedText(currentFont, s, rect, alignment);
        }

        // Render a symbol inside the given rectangle.
        private void RenderSymbol(IRenderer renderer, Symbol symbol, float left, float top, float right, float bottom, RectangleF clipRect)
        {
            if (symbol == null)
                return;

            RectangleF rect = new RectangleF(left, top, right - left, bottom - top);

            if (!rect.IntersectsWith(clipRect))
                return;

            renderer.DrawSymbol(symbol, rect);
        }

        // Render some text inside a rectangle. Use the special column F formatting characters (/, |).
        private void RenderColumnFText(IRenderer renderer, string s, float left, float top, float right, float bottom, RectangleF clipRect)
        {
            int index;
            bool split = false;
            bool drawDiagonalLine = false;
            string first = null, second = null;
            RectangleF rectFirst = new RectangleF(), rectSecond = new RectangleF();

            if (s == null)
                return;

            RectangleF rect = new RectangleF(left, top, right - left, bottom - top);
            if (!rect.IntersectsWith(clipRect))
                return;

            if (replaceMultiplySign)
                s = s.Replace('x', '\u00D7');           // U+00D7 is multiplication sign

            index = s.IndexOf('|');
            if (index >= 0) {
                split = true;
                drawDiagonalLine = false;
                first = s.Substring(0, index);
                second = s.Substring(index + 1);
                rectFirst = new RectangleF(left, top, right - left, (bottom - top) / 2);
                rectSecond = new RectangleF(left, top + (bottom - top) / 2, right - left, (bottom - top) / 2);
            }
            else {
                index = s.IndexOf('/');
                if (index >= 0) {
                    split = true;
                    drawDiagonalLine = true;
                    first = s.Substring(0, index);
                    second = s.Substring(index + 1);
                    rectFirst = new RectangleF(left, top, (right - left) * 0.63F, (bottom - top) / 2);
                    rectSecond = new RectangleF(left + (right - left) * 0.37F, top + (bottom - top) / 2, (right - left) * 0.63F, (bottom - top) / 2);
                }
            }

            if (split) {
                if (drawDiagonalLine)
                    renderer.DrawLine(thinPen, right, top, left, bottom);

                RenderSingleLineText(renderer, COLUMNF_DOUBLE_FONT, StringAlignment.Center, first, rectFirst.Left, rectFirst.Top, rectFirst.Right, rectFirst.Bottom, clipRect);
                RenderSingleLineText(renderer, COLUMNF_DOUBLE_FONT, StringAlignment.Center, second, rectSecond.Left, rectSecond.Top, rectSecond.Right, rectSecond.Bottom, clipRect);
            }
            else {
                RenderSingleLineText(renderer, COLUMNF_FONT, StringAlignment.Center, s, left, top, right, bottom, clipRect);
            }
        }

        // Render a single line of the description. "lastLine" is true if this is the last line (draws the bottom line). The "thickLineCounter"
        // is used to decide when to draw the thick lines.
        // clipRect is the clipping rectangle in world coordinates. Only need to draw things that intersect it.
        private void RenderLine(IRenderer renderer, DescriptionLine descriptionLine, DescriptionKind descriptionKind, bool lastLine, bool drawThickLine, bool noTopLine, RectangleF clipRect)
        {
            float fullWidth = WidthInCells() * 100;

            // Draw top line.
            if (!noTopLine) {
                if (descriptionLine.kind != DescriptionLineKind.Normal || drawThickLine) {
                    renderer.DrawLine(thickPen, 0, 0, fullWidth, 0);
                }
                else {
                    renderer.DrawLine(thinPen, 0, 0, fullWidth, 0);
                }
            }

            // Draw bottom line, if requested
            if (lastLine)
                renderer.DrawLine(thickPen, 0, 100, fullWidth, 100);

            // Draw side lines.
            float lineTop = -DescriptionAppearance.thickDescriptionLine / 2;
            float lineBottom = 100 + DescriptionAppearance.thickDescriptionLine / 2;
            renderer.DrawLine(thickPen, 0, lineTop, 0, lineBottom);
            if (! (descriptionKind == DescriptionKind.SymbolsAndText && (descriptionLine.kind == DescriptionLineKind.Title || descriptionLine.kind == DescriptionLineKind.SecondaryTitle || descriptionLine.kind == DescriptionLineKind.Text)))
                renderer.DrawLine(thickPen, 800, lineTop, 800, lineBottom);
            if (descriptionKind == DescriptionKind.SymbolsAndText)
                renderer.DrawLine(thickPen, 1300, lineTop, 1300, lineBottom);

            switch (descriptionLine.kind) {
                case DescriptionLineKind.Title:
                    RenderSingleLineText(renderer, TITLE_FONT, StringAlignment.Center, (string) (descriptionLine.boxes[0]), 0, 0, fullWidth, 100, clipRect);
                    break;

                case DescriptionLineKind.SecondaryTitle:
                    RenderSingleLineText(renderer, TITLE_FONT, StringAlignment.Center, (string) (descriptionLine.boxes[0]), 0, 0, fullWidth, 100, clipRect);
                    break;

                case DescriptionLineKind.Header2Box:
                    renderer.DrawLine(thickPen, 300, lineTop, 300, lineBottom);
                    RenderSingleLineText(renderer, TITLE_FONT, StringAlignment.Center, (string) (descriptionLine.boxes[0]), 0, 0, 300, 100, clipRect);
                    RenderSingleLineText(renderer, TITLE_FONT, StringAlignment.Center, (string) (descriptionLine.boxes[1]), 300, 0, 800, 100, clipRect);
                    break;

                case DescriptionLineKind.Header3Box:
                    renderer.DrawLine(thickPen, 300, lineTop, 300, lineBottom);
                    renderer.DrawLine(thickPen, 600, lineTop, 600, lineBottom);
                    RenderSingleLineText(renderer, TITLE_FONT, StringAlignment.Center, (string) (descriptionLine.boxes[0]), 0, 0, 300, 100, clipRect);
                    RenderSingleLineText(renderer, TITLE_FONT, StringAlignment.Center, (string) (descriptionLine.boxes[1]), 300, 0, 600, 100, clipRect);
                    RenderSingleLineText(renderer, TITLE_FONT, StringAlignment.Center, (string) (descriptionLine.boxes[2]), 600, 0, 800, 100, clipRect);
                    break;

                case DescriptionLineKind.Directive:
                    if (descriptionKind == DescriptionKind.Text) {
                        RenderWrappedText(renderer, TEXT_FONT, StringAlignment.Near, descriptionLine.textual, 15, 0, 785, 100, clipRect);
                    }
                    else {
                        RenderSymbol(renderer, (Symbol)descriptionLine.boxes[0], 0, 0, 800, 100, clipRect);
                        RenderSingleLineText(renderer, DIRECTIVE_FONT, StringAlignment.Center, (string) (descriptionLine.boxes[1]), 300, 0, 500, 100, clipRect);
                        if (descriptionKind == DescriptionKind.SymbolsAndText)
                            RenderWrappedText(renderer, TEXT_FONT, StringAlignment.Near, descriptionLine.textual, 815, 0, 1285, 100, clipRect);
                    }

                    break;

                case DescriptionLineKind.Normal:
                    Func<int, bool> boxesToShow = (i => true);  // which boxes to show?

                    if (descriptionKind == DescriptionKind.Text) {
                        renderer.DrawLine(thinPen, 100, lineTop, 100, lineBottom);
                        renderer.DrawLine(thickPen, 200, lineTop, 200, lineBottom);
                        if (columnHScore) {
                            renderer.DrawLine(thickPen, 700, lineTop, 700, lineBottom);
                        }
                        RenderWrappedText(renderer, TEXT_FONT, StringAlignment.Near, descriptionLine.textual, 215, 0, (columnHScore ? 685 : 785), 100, clipRect);

                        if (columnHScore) {
                            boxesToShow = (i => (i < 2 || i == 7));
                        }
                        else {
                            boxesToShow = (i => (i < 2));
                        }
                    }
                    else {
                        renderer.DrawLine(thinPen, 100, lineTop, 100, lineBottom);
                        renderer.DrawLine(thinPen, 200, lineTop, 200, lineBottom);
                        renderer.DrawLine(thickPen, 300, lineTop, 300, lineBottom);
                        renderer.DrawLine(thinPen, 400, lineTop, 400, lineBottom);
                        renderer.DrawLine(thinPen, 500, lineTop, 500, lineBottom);
                        renderer.DrawLine(thickPen, 600, lineTop, 600, lineBottom);
                        renderer.DrawLine(thinPen, 700, lineTop, 700, lineBottom);

                        if (descriptionKind == DescriptionKind.SymbolsAndText) {
                            renderer.DrawLine(thickPen, 1300, lineTop, 1300, lineBottom);
                            RenderWrappedText(renderer, TEXT_FONT, StringAlignment.Near, descriptionLine.textual, 815, 0, 1285, 100, clipRect);
                        }
                    }

                    for (int i = 0; i < 8; ++i) {
                        if (boxesToShow(i)) {
                            if (descriptionLine.boxes[i] is Symbol) {
                                RenderSymbol(renderer, (Symbol)descriptionLine.boxes[i], i * 100, 0, i * 100 + 100, 100, clipRect);
                            }
                            else if (descriptionLine.boxes[i] is String) {
                                if (i == 5)
                                    RenderColumnFText(renderer, (string)descriptionLine.boxes[i], i * 100, 0, i * 100 + 100, 100, clipRect);
                                else if (i == 0)
                                    RenderSingleLineText(renderer, COLUMNA_FONT, StringAlignment.Center, (string)descriptionLine.boxes[i], i * 100, 0, i * 100 + 100, 100, clipRect);
                                else
                                    RenderSingleLineText(renderer, COLUMNB_FONT, StringAlignment.Center, (string)descriptionLine.boxes[i], i * 100, 0, i * 100 + 100, 100, clipRect);
                            }
                        }
                    }

                    break;

                case DescriptionLineKind.Key:
                    RenderSymbol(renderer, (Symbol) descriptionLine.boxes[0], 100, 0, 200, 100, clipRect);
                    RenderSingleLineText(renderer, KEY_FONT, StringAlignment.Near, "= " + (string) (descriptionLine.boxes[1]), 200, 0, 800, 100, clipRect);
                    break;

                case DescriptionLineKind.Text:
                    RenderWrappedText(renderer, TEXTLINE_FONT, StringAlignment.Near, (string) (descriptionLine.boxes[0]), 20, 0, fullWidth, 100, clipRect);
                    break;

                default:
                    Debug.Fail("unknown description line kind");
                    break;
            }
        }

        // Update the thick line counter to indicate when a thick line should be drawn (whever counter is 0)
        private void UpdateThickLineCounter(DescriptionLine descriptionLine, DescriptionKind descriptionKind, ref int thickLineCounter)
        {
            if (descriptionLine.kind == DescriptionLineKind.Normal) {
                if (descriptionLine.boxes[0] != null && descriptionLine.boxes[0] is Symbol)
                    thickLineCounter = 0;   // after start, put a thick line also.
                else {
                    // put a thick line after every three normal lines.
                    thickLineCounter += 1;
                    if (thickLineCounter == 3)
                        thickLineCounter = 0;
                }
            }
            else {
                thickLineCounter = 0;
            }
        }
    }

    // This is the interface to the renderer which abstracts the difference between drawing to 
    // a Graphics and putting objects in a Map.
    interface IRenderer
    {
        // Push a new transform.
        void PushTransform(Matrix m);

        // Pop the transform.
        void PopTransform();

        // Create a pen used for drawing lines.
        object CreatePen(float thickness, LineJoinMode lineJoin, LineCapMode lineCap);

        // Draw a line with a pen.
        void DrawLine(object pen, float x1, float y1, float x2, float y2);

        // Create a font used for drawing text.
        object CreateFont(string fontName, float emHeight, bool bold, bool italic, StringAlignment alignment);

        // Draw a single line of text.
        void DrawSingleLineText(object font, string text, RectangleF rect, StringAlignment horizAlignment);

        // Measure a single line of text, return the width
        float MeasureSingleLineText(object font, string text, RectangleF rect, StringAlignment horizAlignment);

        // Draw some text, wrapping to multiple lines
        void DrawWrappedText(object font, string text, RectangleF rect, StringAlignment horizAlignment);

        // Determine if wrapped lines will fit in the rectangle.
        bool WrappedTextFits(object font, string text, RectangleF rect, StringAlignment horizAlignment);

        // Draw a symbol.
        void DrawSymbol(Symbol symbol, RectangleF rect);
    }

    
    // The Graphics renderer handles rendering to a GraphicsTarget. The passed
    // in GraphicsTarget may have a non-standard transform.
    class GraphicsTargetRenderer : IRenderer
    {
        private IGraphicsTarget grTarget;
        private ITextMetrics textMetrics;
        private CmykColor color;

        // Create a new rendered around the given graphics.
        public GraphicsTargetRenderer(IGraphicsTarget grTarget, ITextMetrics textMetrics, CmykColor color)
        {
            this.grTarget = grTarget;
            this.textMetrics = textMetrics;
            this.color = color;
            grTarget.CreateSolidBrush(color, color);
        }

        public void PushTransform(Matrix m)
        {
            grTarget.PushTransform(m);
        }

        public void PopTransform()
        {
            grTarget.PopTransform();
        }

        public object CreatePen(float thickness, LineJoinMode lineJoin, LineCapMode lineCap)
        {
            object pen = new object();
            grTarget.CreatePen(pen, color, thickness, lineCap, lineJoin, 5);
            return pen;
        }

        public object CreateFont(string fontName, float emHeight, bool bold, bool italic, StringAlignment alignment)
        {
            TextEffects textEffects = TextEffects.None;

            if (bold)
                textEffects |= TextEffects.Bold;
            if (italic)
                textEffects |= TextEffects.Italic;

            FontInfo font = new FontInfo(fontName, emHeight, textEffects, textMetrics);
            grTarget.CreateFont(font, fontName, emHeight, textEffects);

            return font;
        }

        public void DrawLine(object pen, float x1, float y1, float x2, float y2)
        {
            grTarget.DrawLine(pen, new PointF(x1, y1), new PointF(x2, y2));
        }

        public float MeasureSingleLineText(object font, string text, RectangleF rect, StringAlignment horizAlignment)
        {
            FontInfo fontInfo = (FontInfo)font;
            return fontInfo.Metrics.GetTextWidth(text);
        }

        public void DrawSingleLineText(object font, string text, RectangleF rect, StringAlignment horizAlignment)
        {
            FontInfo fontInfo = (FontInfo)font;
            SizeF size = fontInfo.Metrics.GetTextSize(text);

            float x, y;
            y = rect.Center().Y - (fontInfo.Metrics.Ascent + fontInfo.Metrics.Descent) / 2F;
            switch (horizAlignment) {
                case StringAlignment.Near:
                    x = rect.Left; break;
                case StringAlignment.Center:
                    x = rect.Center().X - (size.Width / 2F); break;
                case StringAlignment.Far:
                    x = rect.Right - size.Width; break;
                default:
                    throw new ArgumentException("unknown value for horizAlignment");
            }

            grTarget.DrawText(text, font, color, new PointF(x, y));
        }

        public bool WrappedTextFits(object font, string text, RectangleF rect, StringAlignment horizAlignment)
        {
            FontInfo fontInfo = (FontInfo)font;
            List<string> wrapped = WrapText(fontInfo, text, rect.Width);
            int numLines = wrapped.Count;

            float height = fontInfo.Metrics.Ascent + fontInfo.Metrics.Descent + (numLines - 1) * fontInfo.Metrics.RecommendedLineSpacing;

            return height <= rect.Height;
        }

        public void DrawWrappedText(object font, string text, RectangleF rect, StringAlignment horizAlignment)
        {
            FontInfo fontInfo = (FontInfo)font;
            List<string> wrapped = WrapText(fontInfo, text, rect.Width);
            int numLines = wrapped.Count;

            float height = fontInfo.Metrics.Ascent + fontInfo.Metrics.Descent + (numLines - 1) * fontInfo.Metrics.RecommendedLineSpacing;

            float y = rect.Center().Y - height / 2F;
            float lineHeight = fontInfo.Metrics.Ascent + fontInfo.Metrics.Descent;

            for (int i = 0; i < wrapped.Count; ++i) {
                RectangleF lineRect = new RectangleF(rect.X, y, rect.Width, lineHeight);
                DrawSingleLineText(font, wrapped[i], lineRect, horizAlignment);
                y += fontInfo.Metrics.RecommendedLineSpacing;
            }
        }

        public void DrawSymbol(Symbol sym, RectangleF rect)
        {
            sym.Draw(grTarget, color, rect);
        }


        // Wrap text into lines of length width or less, and return a list of all the lines.
        private List<string> WrapText(FontInfo fontInfo, string text, float width)
        {
            List<string> lineList = new List<string>();

            while (text != null) {
                float lineWidth;
                string line = WrapOneLine(fontInfo, ref text, width, out lineWidth);
                lineList.Add(line);
            }

            return lineList;
        }

        // Figure out how much of the line will fit and return that. line is modified
        // to be the remaining text to fit on subsequent lines, or null if nothing left. The amount of width
        // actually consumed is returned in actualLineWidth.
        private string WrapOneLine(FontInfo fontInfo, ref string line, float lineWidth, out float actualLineWidth)
        {
            StringBuilder lineSoFar = new StringBuilder();
            float widthUsed = 0F;
            bool useSingleLetters = false;

            while (!string.IsNullOrEmpty(line)) {
                // Get next segment of text to add.
                string nextSegment;
                if (useSingleLetters)
                    nextSegment = GetNextTextElement(line);
                else
                    nextSegment = GetNextTextSegment(line);
                if (string.IsNullOrEmpty(nextSegment))
                    break;

                // See if this segment will fit on the line.
                float segmentWidth = WidthOfTextSegment(fontInfo, nextSegment, widthUsed);
                if (segmentWidth + widthUsed > lineWidth) {
                    // The segment won't fit. If we haven't placed any segments yet, we need to try placing single letters.
                    if (widthUsed == 0) {
                        if (!useSingleLetters) {
                            useSingleLetters = true;
                            continue;
                        }
                    }
                    else
                        break;  // we're done.
                }

                // Add nextSegment to the line, and remove from the line under consideration.
                lineSoFar.Append(nextSegment);
                line = line.Substring(nextSegment.Length);
                widthUsed += segmentWidth;
            }

            // If we're wrapping, the new line replaces spaces.
            if (line.Length > 0) {
                // Remove trailing spaces from current line.
                while (lineSoFar.Length > 0 && lineSoFar[lineSoFar.Length - 1] == ' ') {
                    lineSoFar.Remove(lineSoFar.Length - 1, 1);
                    widthUsed -= WidthOfTextSegment(fontInfo, " ", widthUsed);
                }

                // Remove initial spaces from next line.
                int startNextLine = 0;
                while (startNextLine < line.Length && line[startNextLine] == ' ')
                    ++startNextLine;
                if (startNextLine > 0)
                    line = line.Substring(startNextLine);
            }

            if (line == "")
                line = null;

            actualLineWidth = widthUsed;
            return lineSoFar.ToString();
        }

        // Calculate width of one line, not limited by wrapping.
        private float LineWidth(FontInfo fontInfo, string text)
        {
            string nextSegment;
            float width = 0;

            while ((nextSegment = GetNextTextSegment(text)) != null) {
                width += WidthOfTextSegment(fontInfo, nextSegment, width);
                text = text.Substring(nextSegment.Length);
            }

            return width;
        }

        // Get the next text segment needed. If empty, returns null.
        // Is the next is a space or tab, that is it.
        // Otherwise, the whole word to the next space or tab.
        private string GetNextTextSegment(string text)
        {
            if (String.IsNullOrEmpty(text))
                return null;

            if (text[0] == ' ')
                return " ";
            if (text[0] == '\t')
                return "\t";

            int i = 0;
            while (i < text.Length && text[i] != ' ' && text[i] != '\t')
                ++i;

            return text.Substring(0, i);
        }

        // Get the next "character" in a string (a length of unicode characters that appear as a single character).
        private string GetNextTextElement(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            else
                return StringInfo.GetNextTextElement(s);
        }


        // Get the width of a text segment. Handles tabs, spaces between characters, and space widths.
        private float WidthOfTextSegment(FontInfo fontInfo, string text, float widthSoFar)
        {
            // "widthSoFar" is useful for tabs, but we aren't dealing with those.
            return fontInfo.Metrics.GetTextWidth(text);
        }


        // Object returns from CreateFont, stores font information.
        private class FontInfo
        {
            public readonly string FamilyName;
            public readonly float EmHeight;
            public readonly TextEffects TextEffects;
            public readonly ITextFaceMetrics Metrics;

            public FontInfo(string familyName, float emHeight, TextEffects textEffects, ITextMetrics metricsProvider)
            {
                this.FamilyName = familyName;
                this.EmHeight = emHeight;
                this.TextEffects = textEffects;
                this.Metrics = metricsProvider.GetTextFaceMetrics(familyName, emHeight, textEffects);
            }
        }
    }
    
    // The MapRenderer handles rendering to a map, creating symbols as needed.
    // The SymColor to use must be already created a passed in.
    class MapRenderer : IRenderer
    {
        private Map map;
        private SymColor color;
        private Matrix currentTransform, inverseTransform;
        private Stack<Matrix> transformStack = new Stack<Matrix>();
        private Dictionary<object, SymDef> dict;

        public MapRenderer(Map map, SymColor color, Dictionary<object, SymDef> dict)
        {
            this.map = map;
            this.color = color;
            this.dict = dict;
            currentTransform = new Matrix();
        }

        private Matrix Transform
        {
            get
            {
                return currentTransform;
            }
            set
            {
                currentTransform = value;
                inverseTransform = currentTransform.Clone();
                inverseTransform.Invert();
            }
        }

        public void PushTransform(Matrix m)
        {
            transformStack.Push(currentTransform);

            Matrix newTransform = currentTransform.Clone();
            newTransform.Multiply(m, MatrixOrder.Prepend);
            Transform = newTransform;
        }

        public void PopTransform()
        {
            Transform = transformStack.Pop();
        }

        // Get a free OCAD id number
        string GetOcadId()
        {
            return map.GetFreeSymbolId(810);
        }

        public object CreatePen(float thickness, LineJoinMode lineJoin, LineCapMode lineCap)
        {
            LineSymDef symdef = new LineSymDef("Description: line", GetOcadId(), color, Geometry.TransformDistance(thickness, currentTransform), lineJoin, lineCap);
            symdef.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.DescLine_OcadToolbox);
            map.AddSymdef(symdef);
            return symdef;
        }

        public void DrawLine(object pen, float x1, float y1, float x2, float y2)
        {
            LineSymDef symdef = (LineSymDef) pen;
            PointKind[] kinds = {PointKind.Normal, PointKind.Normal};
            PointF[] points = { Geometry.TransformPoint(new PointF(x1, y1), currentTransform), Geometry.TransformPoint(new PointF(x2, y2), currentTransform) };
            SymPath path = new SymPath(points, kinds);
            LineSymbol symbol = new LineSymbol(symdef, path);
            map.AddSymbol(symbol);
        }

        public object CreateFont(string fontName, float emHeight, bool bold, bool italic, StringAlignment alignment)
        {
            TextSymDefHorizAlignment fontAlign;
            TextSymDef symdef = new TextSymDef("Description: text", GetOcadId(), TextSymDef.PreferredSymbolKind.NormalText, null);

            if (alignment == StringAlignment.Far)
                fontAlign = TextSymDefHorizAlignment.Right;
            else if (alignment == StringAlignment.Center)
                fontAlign = TextSymDefHorizAlignment.Center;
            else
                fontAlign = TextSymDefHorizAlignment.Left;

            symdef.SetFont(fontName, Geometry.TransformDistance(emHeight, currentTransform), Util.GetTextEffects(bold, italic), color, Geometry.TransformDistance(emHeight * 1.1F, currentTransform), 0, 0, 0, null, 0, 1F, fontAlign, TextSymDefVertAlignment.TopAscent);
            symdef.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.DescText_OcadToolbox);
            map.AddSymdef(symdef);
            return symdef;
        }

        public float MeasureSingleLineText(object font, string text, RectangleF rect, StringAlignment horizAlignment)
        {
            TextSymDef symdef = (TextSymDef) font;
            PointF baseLocation;       // base location of the text -- top of the character.

            SizeF size;

            // Place the top of the character at 1/2 the height of the character above the vertical mid-line of the rectangle.
            float x = rect.Left;
            float y = (rect.Top + rect.Bottom) / 2;    // mid-point.
            baseLocation = Geometry.TransformPoint(new PointF(x, y), currentTransform);
            baseLocation.Y += (symdef.FontAscent + symdef.FontDescent) / 2;      // half the character height.

            // Calc size of the text. 
            float[] wrappedWidths;
            TextCoordMapper coordMapper;
            string[] wrappedText = symdef.BreakUnwrappedLines(new string[1] {text}, TextSymDefHorizAlignment.Default, out coordMapper, out wrappedWidths); // no wrapping.
            symdef.CalcBounds(wrappedText, wrappedWidths, baseLocation, 0, 0, TextSymDefHorizAlignment.Default, TextSymDefVertAlignment.Default, out size);

            return Geometry.TransformDistance(size.Width, inverseTransform);
        }

        public void DrawSingleLineText(object font, string text, RectangleF rect, StringAlignment horizAlignment)
        {
            TextSymDef symdef = (TextSymDef) font;
            PointF baseLocation;       // base location of the text -- top of the character.

            // Place the top of the character at 1/2 the height of the character above the vertical mid-line of the rectangle.
            float x = rect.Left;
            float y = (rect.Top + rect.Bottom) / 2;    // mid-point.
            float width = Geometry.TransformDistance(rect.Width, currentTransform);
            baseLocation = Geometry.TransformPoint(new PointF(x, y), currentTransform);
            baseLocation.Y += (symdef.FontAscent + symdef.FontDescent) / 2;      // half the character height.
            if (horizAlignment == StringAlignment.Center)
                baseLocation.X += width / 2;
            else if (horizAlignment == StringAlignment.Far)
                baseLocation.X += width;

            TextSymbol symbol = new TextSymbol(symdef, new string[1] { text }, baseLocation, 0, width, TextSymDefHorizAlignment.Default, TextSymDefVertAlignment.Default);
            map.AddSymbol(symbol);
        }

        public bool WrappedTextFits(object font, string text, RectangleF rect, StringAlignment horizAlignment)
        {
            TextSymDef symdef = (TextSymDef) font;

            PointF baseLocation;
            float x = rect.Left;
            float y = rect.Top;    
            baseLocation = Geometry.TransformPoint(new PointF(x, y), currentTransform);

            // Need to create the symbol to get it's height.
            TextSymbol symbol = new TextSymbol(symdef, new string[1] { text }, baseLocation, 0, Geometry.TransformDistance(rect.Width, currentTransform), TextSymDefHorizAlignment.Default, TextSymDefVertAlignment.Default);
            return symbol.TextSize.Height <= Geometry.TransformDistance(rect.Height, currentTransform);
        }

        public void DrawWrappedText(object font, string text, RectangleF rect, StringAlignment horizAlignment)
        {
            TextSymDef symdef = (TextSymDef) font;
            PointF baseLocation;       // base location of the text -- top of the character.

            // Place the top of the character at 1/2 the height of the text above the vertical med-line of the rectangle.
            float x = rect.Left;
            float y = (rect.Top + rect.Bottom) / 2;    // mid-point.
            float width = Geometry.TransformDistance(rect.Width, currentTransform);
            baseLocation = Geometry.TransformPoint(new PointF(x, y), currentTransform);

            // Need to create the symbol to get it's height, which is needed to correctly place it. So we create the symbol twice.
            TextSymbol symbol = new TextSymbol(symdef, new string[1] { text }, baseLocation, 0, width, TextSymDefHorizAlignment.Default, TextSymDefVertAlignment.Default);
            baseLocation.Y += symbol.TextSize.Height / 2;

            if (horizAlignment == StringAlignment.Center)
                baseLocation.X += width / 2;
            else if (horizAlignment == StringAlignment.Far)
                baseLocation.X += width;

            symbol = new TextSymbol(symdef, new string[1] { text }, baseLocation, 0, width, TextSymDefHorizAlignment.Default, TextSymDefVertAlignment.Default);
            map.AddSymbol(symbol);
        }

        public void DrawSymbol(Symbol symbol, RectangleF rect)
        {
            PointSymDef symdef;

            Pair<object, Symbol> key = new Pair<object, Symbol>(this, symbol);

            // The dictionary is used to contains symdefs for each symbol.
            if (dict.ContainsKey(key)) {
                symdef = (PointSymDef) dict[key];
            }
            else {
                symdef = symbol.CreateSymdef(map, color, Geometry.TransformDistance(rect.Height, currentTransform));
                dict[key] = symdef;
            }

            PointSymbol sym = new PointSymbol(symdef, Geometry.TransformPoint(new PointF(rect.X + rect.Width / 2, rect.Y + rect.Height / 2), currentTransform), 0, null);
            map.AddSymbol(sym);
        }
    }
}
