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
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Printing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Diagnostics;

namespace PurplePen
{
    public enum PrintingCountKind { 
        OneDescription,               // One of each course, on as few pages as possible
        OnePage,                         // One page of each course, as many description on that one page as possible
        DescriptionCount,             // Multiple copies of each description, each packed on pages.
        CopyCount                       // One of each course, on a few pages as possible, then this many copies.
    }

    // Class that prints out several rectangle/grid based things. Used for descriptions and punch cards.
    abstract class RectanglePrinting: BasicPrinting
    {
        const float SPACING = 25;        // spacing of rectangles, in hundreths of an inch.

        private float boxSize;
        private PrintingCountKind countKind;
        private int count;

        private RectanglePositioner positioner;

        // Get all the descriptions we are going to print.
        protected abstract IPrintableRectangle[] GetDescriptionList();

        public RectanglePrinting(string title, PageSettings pageSettings, float boxSize, PrintingCountKind countKind, int count)
            : base(title, pageSettings)
        {
            this.boxSize = boxSize;
            this.countKind = countKind;
            this.count = count;
        }

        // Position all the descriptions with the positioner.
        protected override int LayoutPages(PageSettings pageSettings, SizeF printArea)
        {
            // Get the list of renderers for the descriptions we're printing.
            IPrintableRectangle[] rendererList = GetDescriptionList();

            // Position them with the DescriptionPositioner.
            positioner = new RectanglePositioner(printArea, boxSize / 0.254F, SPACING);

            if (countKind == PrintingCountKind.OneDescription) {
                positioner.LayoutMultipleDescriptions(rendererList);
            }
            else if (countKind == PrintingCountKind.OnePage) {
                foreach (DescriptionRenderer renderer in rendererList)
                    positioner.LayoutOneDescriptionPage(renderer);
            }
            else if (countKind == PrintingCountKind.DescriptionCount) {
                foreach (DescriptionRenderer renderer in rendererList) {
                    // renderer pages until we hit the number of selected controls.
                    int countDescriptions = 0;
                    while (countDescriptions < count) {
                        countDescriptions += positioner.LayoutOneDescriptionPage(renderer);
                    }
                }
            }
            else if (countKind == PrintingCountKind.CopyCount) {
                for (int copy = 0; copy < count; ++copy)
                    positioner.LayoutMultipleDescriptions(rendererList);
            }
            else {
                Debug.Fail("unknown countKind");
            }

            return positioner.PageCount;
        }

        // The core printing routine. The origin of the graphics is the upper-left of the margins,
        // and the printArea in the size to draw into (in hundreths of an inch).
        protected override void DrawPage(Graphics g, int pageNumber, SizeF printArea, int dpi)
        {
            positioner.DrawPage(g, pageNumber);
        }
    }


    // This class encapsulates the layout of rectangles on one or more pages. It can then print a page of the rectangle.
    // All interaction is done via the IPrintableRectangle interface that allows simpler testing.
    public class RectanglePositioner
    {
        private struct PositionedRectangle
        {
            public IPrintableRectangle description;
            public int pageNumber;          // page numbers start at 0
            public PointF location;
            public int startLine;
            public int countLines;
        }

        // The page layout sizes.
        private SizeF pageSize;
        private float boxSize;
        private float spacing;

        // This the list of descriptions that have already been positioned.
        private List<PositionedRectangle> positions = new List<PositionedRectangle>();

        // The following variable track the current position on the page.
        private int currentPage;
        private float currentX;              // X position of left side of current column.
        private float columnWidth;       // if currentY != 0, the width of the current column.
        private float currentY;              // Y position of top of where next description might go.

        // Initialize the description positioner with the size of the pages, the box size of descriptions, and the spacing
        // between descriptions. The physicaly size of the units are immaterial, but are all the same.
        public RectanglePositioner(SizeF pageSize, float boxSize, float spacing)
        {
            this.pageSize = pageSize;
            this.boxSize = boxSize;
            this.spacing = spacing;
        }

        // Get the number of pages used by the positioned descriptions.
        public int PageCount
        {
            get {
                if (positions.Count == 0)
                    return 0;
                else
                    return positions[positions.Count - 1].pageNumber + 1;
            }
        }

        // Layout a single description as many times as you can in one page. Return the number of descriptions
        // laid out. If the description is too large for one page, 1 is returned and the description is laid out on multiple
        // pages, so be sure to check PageCount anyway.
        public int LayoutOneDescriptionPage(IPrintableRectangle rectangle)
        {
            int countRectangles = 0;
            int startLine = 0, countLines;

            if (!PageSizeBigEnough(new IPrintableRectangle[] { rectangle }))
                throw new ApplicationException(MiscText.PageTooSmall);

            StartNewPage();
            int startPage = currentPage;

            // Loop until we go the next page. Finish off a description that's in progress.
            while (currentPage == startPage || startLine != 0) {
                if (FitsInCurrentColumn(rectangle, startLine, out countLines)) {
                    PlaceInCurrentColumn(rectangle, startLine, countLines);
                    startLine = 0;
                    countRectangles += 1;
                }
                else {
                    // It doesn't fit in the current column. 
                    if (CurrentColumnEmpty) {
                        if (countLines == 0) {
                            // the description is too wide for the remaining area.
                            StartNewPage();
                        }
                        else {
                            // The description is too large for one column. Put as much as will fit in one column.
                            PlaceInCurrentColumn(rectangle, startLine, countLines);
                            startLine += countLines;
                        }
                    }
                    else {
                        // The description won't fit in the rest of this column. Start a new one.
                        StartNewColumn();
                    }
                }
            }

            // If the current page isn't empty and we placed more than one description, remove the last description,
            // because it shouldn't have been placed.
            if (! CurrentPageEmpty && countRectangles > 1) {
                RemoveLastRectangle();
                --countRectangles;
            }

            return countRectangles;
        }

        // Layout one copy of each of multiple descriptions on one or more pages.
        public void LayoutMultipleDescriptions(IPrintableRectangle[] rectangles)
        {
            int startLine = 0, countLines;

            if (rectangles.Length == 0)
                return;

            if (!PageSizeBigEnough(rectangles))
                throw new ApplicationException(MiscText.PageTooSmall);

            List<IPrintableRectangle> rectList = new List<IPrintableRectangle>(rectangles);

            // Sort by largest width first, then by largest height.
            rectList.Sort(delegate(IPrintableRectangle d1, IPrintableRectangle d2) {
                Size size1 = d1.Boxes, size2 = d2.Boxes;
                if (size1.Width < size2.Width)
                    return 1;
                else if (size1.Width > size2.Width)
                    return -1;
                else
                    return size2.Height.CompareTo(size1.Height);
            });

            StartNewPage();

            // At each step, we place the largest one that will fit. At a column start, we 
            // place the largest one, period.
            while (rectList.Count > 0) {
                // Select a description to place.
                IPrintableRectangle rectToPlace;
                if (CurrentColumnEmpty)
                    rectToPlace = rectList[0];
                else {
                    rectToPlace = null;
                    foreach (IPrintableRectangle rect in rectList) {
                        if (FitsInCurrentColumn(rect, 0, out countLines)) {
                            rectToPlace = rect;
                            break;
                        }
                    }
                    if (rectToPlace == null) {
                        // Nothing fits. Start a new column.
                        StartNewColumn();
                        continue;
                    }
                }

                // Remove it from the list so we only place it once.
                rectList.Remove(rectToPlace);

                // Place it, in multiple pieces if needed.
                startLine = 0;
                while (!FitsInCurrentColumn(rectToPlace, startLine, out countLines)) {
                    if (countLines == 0)
                        StartNewPage();
                    else {
                        PlaceInCurrentColumn(rectToPlace, startLine, countLines);
                        startLine += countLines;
                    }
                }
                PlaceInCurrentColumn(rectToPlace, startLine, countLines);
            }
        }

        // Draws a given page. 0 is the first page.
        public void DrawPage(Graphics g, int pageNumber)
        {
            foreach (PositionedRectangle positionedRectangle in positions) {
                if (positionedRectangle.pageNumber == pageNumber) {
                    positionedRectangle.description.Draw(g, positionedRectangle.location.X, positionedRectangle.location.Y, positionedRectangle.startLine, positionedRectangle.countLines);
                }
            }
        }

        // Check if page size is big enough to hold any descriptions at all. If not, throw exception.
        private bool PageSizeBigEnough(IPrintableRectangle[] rectangles)
        {
            if (pageSize.Width < boxSize || pageSize.Height < boxSize)
                return false;

            foreach (IPrintableRectangle desc in rectangles) {
                if (desc.Boxes.Width * boxSize > pageSize.Width)
                    return false;
            }

            return true;
        }

        // Determine if the given rectangle (starting at a given line) would fully fit in the current column. 
        // Also returns the number of lines that would fit.
        private bool FitsInCurrentColumn(IPrintableRectangle rectangle, int startLine, out int numberLinesFit)
        {
            numberLinesFit = 0;
            Size sizeInBoxes = rectangle.Boxes;
            SizeF size = new SizeF(sizeInBoxes.Width * boxSize, (sizeInBoxes.Height - startLine) * boxSize);

            // Will width fit?
            if (CurrentColumnEmpty) {
                // A column has not been started. Check that width the description <= remaining space on page.
                if (size.Width > pageSize.Width - currentX)
                    return false;
            }
            else {
                // A column has been started. Check that the width of this description is <= column width
                if (size.Width > columnWidth)
                    return false;
            }

            // Will height fit?
            float remainingHeight = pageSize.Height - currentY;
            if (size.Height <= remainingHeight) {
                // fully fits
                numberLinesFit = rectangle.Boxes.Height;
                return true;
            }
            else {
                // doesn't fully fit. How much does fit?
                numberLinesFit = (int) (remainingHeight / boxSize);
                return false;
            }
        }

        // Place a rectangle in the current column. Caller must ensure that it fits!
        private void PlaceInCurrentColumn(IPrintableRectangle rectangle, int startLine, int countLines)
        {
            countLines = Math.Min(countLines, rectangle.Boxes.Height - startLine);      // if countLines is too big, reduce it.
#if DEBUG
            // Make sure it fits.
            int numberLinesFit;
            bool fits = FitsInCurrentColumn(rectangle, startLine, out numberLinesFit);
            Debug.Assert(fits || countLines <= numberLinesFit);
#endif //DEBUG

            // Add the description to the list of positioned rectangles.
            PositionedRectangle positionedRectangle;
            positionedRectangle.description = rectangle;
            positionedRectangle.pageNumber = currentPage;
            positionedRectangle.location = new PointF(currentX, currentY);
            positionedRectangle.startLine = startLine;
            positionedRectangle.countLines = countLines;
            positions.Add(positionedRectangle);

            // Update the variables.
            if (CurrentColumnEmpty) {
                columnWidth = rectangle.Boxes.Width * boxSize;
            }
            currentY += countLines * boxSize + spacing;
            if (currentY >= pageSize.Height)
                StartNewColumn();
        }

        // Remove the last rectangle placed. If it was split, removes all the parts.
        private void RemoveLastRectangle()
        {
            int lineStart;
            do {
                lineStart = positions[positions.Count - 1].startLine;
                positions.RemoveAt(positions.Count - 1);
            } while (lineStart != 0);

            if (positions.Count == 0) {
                currentPage = 0;
                currentX = 0;
                currentY = 0;
            }
            else {
                PositionedRectangle lastPosition = positions[positions.Count - 1];
                currentPage = lastPosition.pageNumber;
                columnWidth = lastPosition.description.Boxes.Width * boxSize;
                currentX = lastPosition.location.X;
                currentY = lastPosition.location.Y + lastPosition.countLines * boxSize + spacing;
                if (currentY >= pageSize.Height)
                    StartNewColumn();
            }
        }

        // Is the current column empty?
        private bool CurrentColumnEmpty
        {
            get { return currentY == 0; }
        }

        // Is the current page empty?
        private bool CurrentPageEmpty
        {
            get { return currentX == 0 && currentY == 0; }
        }

        // Start a new column. May start a new page. If current column is empty, does nothing.
        private void StartNewColumn()
        {
            if (CurrentColumnEmpty)
                return;         // already at the start of a column!

            currentY = 0;
            currentX += columnWidth + spacing;
            if (currentX >= pageSize.Width)
                StartNewPage();
        }

        // Start a new page. If current page is empty, does nothing.
        private void StartNewPage()
        {
            if (CurrentPageEmpty)
                return;            // already have an empty page!

            currentPage += 1;
            currentX = currentY = columnWidth = 0;
        }
    }

    // The interface that the RectanglePositioner class uses to position and draw pages of rectangles.
    public interface IPrintableRectangle
    {
        // Number of boxes in the description.
        Size Boxes { get; }

        // Draw all or part of the description.
        void Draw(Graphics g, float x, float y, int startLine, int countLines);
    }
}
