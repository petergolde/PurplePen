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

#define BITMAPPRINTING

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Diagnostics;


namespace PurplePen
{
    using PurplePen.Graphics2D;
    using PurplePen.MapModel;

    // Class to print out courses.
    class CoursePrinting: BasicPrinting
    {
        // Layout of a single page that is being print. Might be all of a course or just part.
        class CoursePage
        {
            public Id<Course> courseId;             // course to print
            public RectangleF mapRectangle;      // rectangle to print in map coordinates
            public RectangleF printRectangle;     // rectangle to print to on page, in hundredth of inch.
            public bool landscape;                       // true if page should be printed in landscape orientation
        }

        // Encapsulate the layout of one dimension of a page layout.
#if TEST
        internal
#endif
        struct DimensionLayout
        {
            public float startMap;                   // start and length in the map in map coords.
            public float lengthMap;
            public float startPage;                 // start and length on the printed page, in hundreths of a inch
            public float lengthPage;

            public DimensionLayout(float startMap, float lengthMap, float startPage, float lengthPage)
            {
                this.startMap = startMap;
                this.lengthMap = lengthMap;
                this.startPage = startPage;
                this.lengthPage = lengthPage;
            }
        }

        // List of all the pages, constructed during layout.
        private List<CoursePage> pages = new List<CoursePage>();

        private CoursePrintSettings coursePrintSettings;
        private EventDB eventDB;
        private SymbolDB symbolDB;
        private Controller controller;
        private MapDisplay mapDisplay;
        private CourseAppearance appearance;

        private RectangleF portraitPrintableArea, landscapePrintableArea;

        // mapDisplay is a MapDisplay that contains the correct map. All other features of the map display need to be customized.
        public CoursePrinting(EventDB eventDB, SymbolDB symbolDB, Controller controller, MapDisplay mapDisplay, CoursePrintSettings coursePrintSettings, CourseAppearance appearance)
            : base(QueryEvent.GetEventTitle(eventDB, " "), coursePrintSettings.PageSettings, coursePrintSettings.PrintingColorModel)
        {
            this.eventDB = eventDB;
            this.symbolDB = symbolDB;
            this.controller = controller;
            this.mapDisplay = mapDisplay;
            this.coursePrintSettings = coursePrintSettings;
            this.appearance = appearance;

            // Set default features for printing.
            mapDisplay.MapIntensity = 1.0F;
            mapDisplay.AntiAlias = false;
            mapDisplay.Printing = true;
        }

        RectangleF GetPrintableArea(PageSettings pageSettings)
        {
            // Get the available page size on the page. Note the PrintableArea member of PageSettings does not automatically does the
            // landscape into account.
            RectangleF printableArea = pageSettings.PrintableArea;
            if (pageSettings.Landscape)
                printableArea = new RectangleF(printableArea.Top, printableArea.Left, printableArea.Height, printableArea.Width);  // reverse rectangle for landscape
            return printableArea;
        }

        // Get the printable area and store them.
        void StorePrintableAreas(PageSettings pageSettings)
        {
            bool saveLandscape = pageSettings.Landscape;
            pageSettings.Landscape = false;
            portraitPrintableArea = GetPrintableArea(pageSettings);
            pageSettings.Landscape = true;
            landscapePrintableArea = GetPrintableArea(pageSettings);
            pageSettings.Landscape = saveLandscape;
        }

        // Layout all the pages, return the total number of pages.
        protected override int LayoutPages(PageSettings pageSettings, SizeF printArea)
        {
            StorePrintableAreas(pageSettings);

            pages.Clear();

            // Go through each course and lay it out, then add to the page list.
            foreach (Id<Course> courseId in coursePrintSettings.CourseIds) {
                // Get the layout for the course.
                List<CoursePage> coursePages = LayoutOptimizedCourse(courseId);

                pageSettings.PrinterSettings.Copies = (short) coursePrintSettings.Count;
                pageSettings.PrinterSettings.Collate = false;      // print all of one course, then all of next, etc.

                pages.AddRange(coursePages);
            }

            return pages.Count;            // total number of pages.
        }

        // Layout a single course onto one or more pages.
        // Optimize onto portrait or landscape.
        List<CoursePage> LayoutOptimizedCourse(Id<Course> courseId)
        {
            List<CoursePage> portraitLayout, landscapeLayout;

            // Layout in both portrait and landscape, and use the one which uses the least pages.
            portraitLayout = LayoutCourse(false, courseId);
            landscapeLayout = LayoutCourse(true, courseId);

            bool useLandscape;

            // Figure out which layout is best. Best layout is the one with the least number of pages. If they have the same
            // number of pages, then the most similar layout.
            if (portraitLayout.Count < landscapeLayout.Count) 
                useLandscape = false;
            else if (portraitLayout.Count > landscapeLayout.Count) {
                useLandscape = true;
            }
            else {
                useLandscape = false;
                if (landscapeLayout.Count > 0 && landscapeLayout[0].printRectangle.Width > landscapeLayout[0].printRectangle.Height) 
                    useLandscape = true;
            }

            // Return the layout that was best.
            if (useLandscape) {
                // Landscape is better.
                return landscapeLayout;
            }
            else {
                // Portrait is better.
                return portraitLayout;
            }
        }


        // Layout a course onto one or more pages.
        List<CoursePage> LayoutCourse(bool landscape, Id<Course> courseId)
        {
            List<CoursePage> pageList = new List<CoursePage>();

            // Get the area of the map we want to print, in map coordinates, and the ratio between print scale and map scale.
            float scaleRatio;
            RectangleF mapArea = GetPrintAreaForCourse(landscape, courseId, out scaleRatio);

            // Get the available page size on the page. 
            RectangleF printableArea = landscape ? landscapePrintableArea : portraitPrintableArea;
            SizeF pageSizeAvailable = printableArea.Size;

            // Layout both page dimensions, iterate through them to get all the pages we have.
            foreach (DimensionLayout verticalLayout in LayoutPageDimension(mapArea.Top, mapArea.Height, printableArea.Top, printableArea.Height, scaleRatio))
                foreach (DimensionLayout horizontalLayout in LayoutPageDimension(mapArea.Left, mapArea.Width, printableArea.Left, printableArea.Width, scaleRatio)) 
                {
                    CoursePage page = new CoursePage();
                    page.courseId = courseId;
                    page.landscape = landscape;
                    page.mapRectangle = new RectangleF(horizontalLayout.startMap, verticalLayout.startMap, horizontalLayout.lengthMap, verticalLayout.lengthMap);
                    page.printRectangle = new RectangleF(horizontalLayout.startPage, verticalLayout.startPage, horizontalLayout.lengthPage, verticalLayout.lengthPage);
                    pageList.Add(page);
                }

            return pageList;
        }

        // Lays out a page in one dimension, determine if it fits on one page or more than one page, and how the mapping goes.
        // Yields an enumeration of the page layouts in that dimensions.
#if TEST
        internal
#endif
        static IEnumerable<DimensionLayout> LayoutPageDimension(float mapStart, float mapLength, float printableAreaStart, float printableAreaLength, float scaleRatio)
        {
            // Map coordinates are in mm, so there are 0.2544 map units per page unit.
            float mmPerPageUnit = (0.254F * scaleRatio);

            // Figure out the length this map part will need on the page, in 1/100 of an inch, given the scale ratio.
            float pageLengthNeeded = mapLength / mmPerPageUnit;

            // If it fits in the printable area, just center it and a single page suffices.
            if (pageLengthNeeded <= printableAreaLength) {
                float borderAmount = (printableAreaLength - pageLengthNeeded) / 2F;
                yield return new DimensionLayout(mapStart, mapLength, printableAreaStart + borderAmount, pageLengthNeeded);
            }
            else {
                // Doesn't fit on one page. How many pages will be needed?

                // The minimum amount of overlap is either 1 inch, or 1/6th of the printable area.
                float minOverlap = Math.Min(100F, printableAreaLength / 6);

                // How many pages?
                int numberOfPages = (int) Math.Ceiling((pageLengthNeeded - minOverlap) / (printableAreaLength - minOverlap));
                Debug.Assert(numberOfPages >= 2);

                // How much overlap will there be between pages (in page units)?
                float overlapPage = (numberOfPages * printableAreaLength - pageLengthNeeded) / (numberOfPages - 1);

                // And create the pages.
                float mapAdvance = (printableAreaLength - overlapPage) * mmPerPageUnit;
                for (int i = 0; i < numberOfPages; ++i) {
                    yield return new DimensionLayout(mapStart + i * mapAdvance, printableAreaLength * mmPerPageUnit, printableAreaStart, printableAreaLength);
                }
            }
        }

        // Get the printable size, scaled to map units(mm) and taking scaleRatio into account. To avoid some round-off problems, the size
        // is reduced by 0.1mm in each direction
        SizeF GetScaledPrintableSizeInMapUnits(RectangleF printableArea, float scaleRatio)
        {
            float mmPerPageUnit = (0.254F * scaleRatio);
            return new SizeF(printableArea.Width * mmPerPageUnit - 0.1F, printableArea.Height * mmPerPageUnit - 0.1F);
        }


        // Get the area of the map we want to print, in map coordinates, and the print scale.
        // if the courseId is None, do all controls.
        // If asked for, crop to a single page size.
        RectangleF GetPrintAreaForCourse(bool landscape, Id<Course> courseId, out float scaleRatio)
        {
            // Get the course view to get the scale ratio.
            CourseView courseView = CourseView.CreatePositioningCourseView(eventDB, new CourseDesignator(courseId));
            scaleRatio = courseView.ScaleRatio;

            RectangleF printArea = controller.GetPrintArea(courseId);

            if (coursePrintSettings.CropLargePrintArea) {
                // Crop the print area to a single page, portrait or landscape.
                // Try to keep CourseObjects in view as much as possible.
                CourseLayout layout = new CourseLayout();
                CourseFormatter.FormatCourseToLayout(symbolDB, courseView, controller.GetCourseAppearance(), layout, 0);
                RectangleF courseObjectsArea = layout.BoundingRect();
                courseObjectsArea.Intersect(printArea);

                // We may need to crop the print area to fit. Try both landscape and portrait.
                float areaCoveredLandscape, areaCoveredPortrait;
                RectangleF portraitPrintArea = CropPrintArea(printArea, courseObjectsArea, GetScaledPrintableSizeInMapUnits(portraitPrintableArea, scaleRatio), out areaCoveredPortrait);
                RectangleF landscapePrintArea = CropPrintArea(printArea, courseObjectsArea, GetScaledPrintableSizeInMapUnits(landscapePrintableArea, scaleRatio), out areaCoveredLandscape);

                // Choose the best one: first look at amount of course covered, then most like the defined print area.
                if (areaCoveredPortrait > areaCoveredLandscape)
                    return portraitPrintArea;
                else if (areaCoveredLandscape > areaCoveredPortrait)
                    return landscapePrintArea;
                else if (printArea.Width < printArea.Height)
                    return portraitPrintArea;
                else
                    return landscapePrintArea;
            }
            else {
                return printArea;
            }
        }

        // Find a crop of printArea that includes as much of courseObjectsArea as possible, that is of the printableSize;
#if TEST
        internal
#endif
        static RectangleF CropPrintArea(RectangleF printArea, RectangleF courseObjectsArea, SizeF printableSize, out float areaCovered)
        {
            float left, top, right, bottom;

            if (printableSize.Width >= printArea.Width) {
                left = printArea.Left; right = printArea.Right;
            }
            else {
                // Center on courseObjectsArea as much as possible, while staying fully inside printArea.
                left = (courseObjectsArea.Left + courseObjectsArea.Right) / 2 - printableSize.Width / 2;
                right = (courseObjectsArea.Left + courseObjectsArea.Right) / 2 + printableSize.Width / 2;
                if (left < printArea.Left) {
                    right += (printArea.Left - left); left = printArea.Left;
                }
                else if (right > printArea.Right) {
                    left -= (right - printArea.Right); right = printArea.Right;
                }
            }

            if (printableSize.Height >= printArea.Height) {
                top = printArea.Top; bottom = printArea.Bottom;
            }
            else {
                // Center on courseObjectsArea as much as possible, while staying fully inside printArea.
                top = (courseObjectsArea.Top + courseObjectsArea.Bottom) / 2 - printableSize.Height / 2;
                bottom = (courseObjectsArea.Top + courseObjectsArea.Bottom) / 2 + printableSize.Height / 2;
                if (top < printArea.Top) {
                    bottom += (printArea.Top - top); top = printArea.Top;
                }
                else if (bottom > printArea.Bottom) {
                    top -= (bottom - printArea.Bottom); bottom = printArea.Bottom;
                }
            }

            // Create the rectangle.
            RectangleF result = RectangleF.FromLTRB(left, top, right, bottom);

            // Calculate the intersected area.
            RectangleF intersect = result;
            intersect.Intersect(courseObjectsArea);
            areaCovered = intersect.Height * intersect.Width;

            return result;
        }

        // Set landscape/portrait and margins for a particular page.
        protected override void ChangePageSettings(int pageNumber, ref bool landscape, Margins margins)
        {
            landscape = pages[pageNumber].landscape;
            margins.Left = margins.Right = margins.Top = margins.Bottom = 0;
        }

        // The core printing routine. The origin of the graphics is the upper-left of the margins,
        // and the printArea in the size to draw into (in hundreths of an inch).
        protected override void DrawPage(IGraphicsTarget graphicsTarget, int pageNumber, SizeF printArea, float dpi)
        {
            CoursePage page = pages[pageNumber];

            // Get the course view for the course we are printing.
            CourseView courseView = CourseView.CreatePrintingCourseView(eventDB, new CourseDesignator(page.courseId));

            // Get the correct purple color to print the course in.
            short ocadId;
            float purpleC, purpleM, purpleY, purpleK;
            FindPurple.GetPurpleColor(mapDisplay, appearance, out ocadId, out purpleC, out purpleM, out purpleY, out purpleK);

            // Create a course layout from the view.
            CourseLayout layout = new CourseLayout();
            layout.SetLayerColor(CourseLayer.Descriptions, NormalCourseAppearance.blackColorOcadId, NormalCourseAppearance.blackColorName, NormalCourseAppearance.blackColorC, NormalCourseAppearance.blackColorM, NormalCourseAppearance.blackColorY, NormalCourseAppearance.blackColorK);
            layout.SetLayerColor(CourseLayer.MainCourse, ocadId, NormalCourseAppearance.courseColorName, purpleC, purpleM, purpleY, purpleK);
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, appearance, layout, CourseLayer.MainCourse);

            // Set the course layout into the map display
            mapDisplay.SetCourse(layout);

            // Sometimes GDI+ gets angry and throws an exception below. I'm hoping collecting garbage might help.
            GC.Collect();

            if (!PrintPreviewInProgress && graphicsTarget is GDIPlus_GraphicsTarget) {
                GDIPlus_GraphicsTarget gdiGraphicsTarget = ((GDIPlus_GraphicsTarget)graphicsTarget);
                Graphics g = gdiGraphicsTarget.Graphics;
                // Save and restore state so we can mess with stuff.
                GraphicsState graphicsState = g.Save();

                // Printing via a bitmap. Works best with some print drivers.
                dpi = AdjustDpi(dpi);

                const long MAX_PIXELS_PER_BAND = 20000000;    // 20M pixels = 60M bytes (3 bytes per pixel).
                List<CoursePage> bands = BandPageToLimitBitmapSize(page, dpi, MAX_PIXELS_PER_BAND);

                // Create the bitmap. Can do this once because each band is the same size.
                int bitmapWidth = (int) Math.Round(bands[0].printRectangle.Width * dpi / 100F);
                int bitmapHeight = (int) Math.Round(bands[0].printRectangle.Height * dpi / 100F);
                Bitmap bitmap = new Bitmap(bitmapWidth, bitmapHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                foreach (CoursePage band in bands) {
                    // Create graphics to draw into the bitmap.
                    Graphics bitmapGraphics = Graphics.FromImage(bitmap);
                    bitmapGraphics.Clear(Color.White);

                    // Set the transform
                    Matrix transform = Geometry.CreateInvertedRectangleTransform(band.mapRectangle, new RectangleF(0, 0, bitmapWidth, bitmapHeight));
                    bitmapGraphics.MultiplyTransform(transform);

                    // Determine the resolution in map coordinates.
                    Matrix inverseTransform = transform.Clone();
                    inverseTransform.Invert();
                    float minResolution = Geometry.TransformDistance(1F, inverseTransform);

                    // And draw.
                    using (IGraphicsTarget gt = new GDIPlus_GraphicsTarget(bitmapGraphics, gdiGraphicsTarget.ColorConverter)) {
                        mapDisplay.Draw(gt, band.mapRectangle, minResolution);
                    }
                    bitmapGraphics.Dispose();

                    // Draw the bitmap on the printer.
                    g.DrawImage(bitmap, band.printRectangle);
                }

                // restore state.
                g.Restore(graphicsState);
                bitmap.Dispose();
            }
            else {
                // Print directly. Works best with print preview.
                // Set the transform, and the clip.
                Matrix transform = Geometry.CreateInvertedRectangleTransform(page.mapRectangle, page.printRectangle);
                graphicsTarget.PushTransform(transform);

                // Determine the resolution in map coordinates.
                Matrix inverseTransform = transform.Clone();
                inverseTransform.Invert();
                float minResolutionPage = 100F / dpi;
                float minResolutionMap = Geometry.TransformDistance(minResolutionPage, inverseTransform);

                // And draw.
                mapDisplay.Draw(graphicsTarget, page.mapRectangle, minResolutionMap);

                graphicsTarget.PopTransform();
            }
        }

        const float MIN_DPI = 400;
        const float MAX_DPI = 1500;

        // Adjust the DPI in case of drivers that report too high or too low. Keep it a multiple/divisor of the reported DPI.
#if TEST
        internal
#endif
        static float AdjustDpi(float dpi)
        {
            while (dpi < MIN_DPI)
                dpi *= 2;

            while (dpi > MAX_DPI)
                dpi /= 2;

            return dpi;
        }


        // Split up a course page into bands, so that each band is
        //    a) exactly the same size
        //    b) no more than maxPixels in size
        //    c) goes top to bottom for portraint, left to right for landscape.
        List<CoursePage> BandPageToLimitBitmapSize(CoursePage page, float dpi, long maxPixels)
        {
            List<CoursePage> list = new List<CoursePage>();
            bool landscape = page.landscape;

            // Figure out how many bands we need to limit the pixel size of each band.
            long pixelsOnPage = (long) Math.Round(page.printRectangle.Width * dpi / 100F) * (long) Math.Round(page.printRectangle.Height * dpi / 100F);
            int numBands = (int) Math.Ceiling((double)pixelsOnPage / (double) maxPixels);

            // Calculate the band size.
            float mapBandSize, printBandSize;
            if (landscape) {
                mapBandSize = page.mapRectangle.Width / numBands;
                printBandSize = page.printRectangle.Width / numBands;
            }
            else {
                mapBandSize = page.mapRectangle.Height / numBands;
                printBandSize = page.printRectangle.Height / numBands;
            }

            // Create the bands.
            for (int i = 0; i < numBands; ++i) {
                CoursePage band = new CoursePage();
                band.landscape = landscape;
                band.courseId = page.courseId;

                if (landscape) {
                    band.mapRectangle = new RectangleF(page.mapRectangle.Left + i * mapBandSize, page.mapRectangle.Top, mapBandSize, page.mapRectangle.Height);
                    band.printRectangle = new RectangleF(page.printRectangle.Left + i * printBandSize, page.printRectangle.Top, printBandSize, page.printRectangle.Height);
                }
                else {
                    band.mapRectangle = new RectangleF(page.mapRectangle.Left, page.mapRectangle.Top + (numBands - 1 - i) * mapBandSize, page.mapRectangle.Width, mapBandSize);
                    band.printRectangle = new RectangleF(page.printRectangle.Left, page.printRectangle.Top + i * printBandSize, page.printRectangle.Width, printBandSize);
                }

                list.Add(band);
            }

            // Return the list of bands.
            return list;
        }

    }

    // All the information needed to print courses.
    class CoursePrintSettings
    {
        private PageSettings pageSettings;

        public PageSettings PageSettings
        {
            get
            {
                if (pageSettings == null) {
                    pageSettings = new PageSettings();
                }

                pageSettings.Landscape = false;                                // Set to not landscape. We change this on a page by page basis later.
                pageSettings.Margins = new Margins(0, 0, 0, 0);        // use no margins. We check the hard margins of the printer during layout.
                return pageSettings;
            }
            set
            {
                pageSettings = value;
            }
        }

        public Id<Course>[] CourseIds;          // Courses to print, None is all controls.

        public int Count = 1;                         // count of copies to print
        public bool CropLargePrintArea = true;       // If true, crop a large print area instead of printing multiple pages 

        public bool UseXpsPrinting = true;          // If true, use XPS printing
        public BasicPrinting.ColorModel PrintingColorModel = BasicPrinting.ColorModel.CMYK;

    }
}
