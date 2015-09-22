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
        // List of all the pages, constructed during layout.
        private List<CoursePage> pages = new List<CoursePage>();

        private CoursePrintSettings coursePrintSettings;
        private EventDB eventDB;
        private SymbolDB symbolDB;
        private MapDisplay mapDisplay;
        private CourseAppearance appearance;

        private RectangleF portraitPrintableArea, landscapePrintableArea;

        // mapDisplay is a MapDisplay that contains the correct map. All other features of the map display need to be customized.
        public CoursePrinting(EventDB eventDB, SymbolDB symbolDB, Controller controller, MapDisplay mapDisplay, CoursePrintSettings coursePrintSettings, CourseAppearance appearance)
            : base(QueryEvent.GetEventTitle(eventDB, " "), controller, coursePrintSettings.PageSettings, coursePrintSettings.PrintingColorModel)
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
            mapDisplay.ColorModel = base.colorModel;
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

            pageSettings.PrinterSettings.Copies = (short)coursePrintSettings.Count;
            pageSettings.PrinterSettings.Collate = false;      // print all of one course, then all of next, etc.

            CoursePageLayout pageLayout = new CoursePageLayout(eventDB, symbolDB, controller, appearance, coursePrintSettings.CropLargePrintArea, portraitPrintableArea, landscapePrintableArea);
            IEnumerable<CourseDesignator> courseDesignators = QueryEvent.EnumerateCourseDesignators(eventDB, coursePrintSettings.CourseIds, !coursePrintSettings.PrintMapExchangesOnOneMap);
            pages = pageLayout.LayoutPages(courseDesignators);

            return pages.Count;            // total number of pages.
        }

        // Set landscape/portrait and margins for a particular page.
        protected override void ChangePageSettings(int pageNumber, ref bool landscape, ref PaperSize paperSize, Margins margins)
        {
            landscape = pages[pageNumber].landscape;
            paperSize = pages[pageNumber].paperSize;
            margins.Left = margins.Right = margins.Top = margins.Bottom = 0;
        }

        protected override bool PausePrintingAfterPage(int pageNumber, out string pauseMessage)
        {
            if (coursePrintSettings.PauseAfterCourseOrPart && pages[pageNumber].lastPageOfCourseOrPart && pageNumber + 1 < pages.Count) {
                pauseMessage = PausePrintingMessage(pageNumber);
                return true;
            }
            else {
                pauseMessage = null;
                return false;
            }
        }

        private string PausePrintingMessage(int pageNumber)
        {
            return string.Format(MiscText.PausePrinting, pages[pageNumber].description, pages[pageNumber + 1].description);
        }

        // The core printing routine. The origin of the graphics is the upper-left of the margins,
        // and the printArea in the size to draw into (in hundreths of an inch).
        protected override void DrawPage(IGraphicsTarget graphicsTarget, int pageNumber, SizeF printArea, float dpi)
        {
            CoursePage page = pages[pageNumber];

            // Get the course view for the course we are printing.
            CourseView courseView = CourseView.CreatePrintingCourseView(eventDB, page.courseDesignator);

            // Get the correct purple color to print the course in.
            short ocadId;
            float purpleC, purpleM, purpleY, purpleK;
            bool purpleOverprint;
            FindPurple.GetPurpleColor(mapDisplay, appearance, out ocadId, out purpleC, out purpleM, out purpleY, out purpleK, out purpleOverprint);

            // Create a course layout from the view.
            CourseLayout layout = new CourseLayout();
            layout.SetLayerColor(CourseLayer.Descriptions, NormalCourseAppearance.blackColorOcadId, NormalCourseAppearance.blackColorName, NormalCourseAppearance.blackColorC, NormalCourseAppearance.blackColorM, NormalCourseAppearance.blackColorY, NormalCourseAppearance.blackColorK, false);
            layout.SetLayerColor(CourseLayer.MainCourse, ocadId, NormalCourseAppearance.courseColorName, purpleC, purpleM, purpleY, purpleK, purpleOverprint);
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, appearance, layout, CourseLayer.MainCourse);

            // Set the course layout into the map display
            mapDisplay.SetCourse(layout);
            this.mapDisplay.SetPrintArea(null);

            // Collecting garbage should make out of memory less common.
            GC.Collect();

            if (graphicsTarget is GDIPlus_GraphicsTarget) {
                // We print to intermediate bands of bitmaps. This is the only way to get purple blending correct.
                // Other code ensure that if purple blending is on, we always take this code path.

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
                    // Set the transform
                    Matrix transform = Geometry.CreateInvertedRectangleTransform(band.mapRectangle, new RectangleF(0, 0, bitmapWidth, bitmapHeight));
                    mapDisplay.Draw(bitmap, transform);

                    // Draw the bitmap on the printer.
                    g.DrawImage(bitmap, band.printRectangle);
                }

                // restore state.
                g.Restore(graphicsState);
                bitmap.Dispose();
            }
            else {
                // Print directly. Used only when prerasterization is off.
                // Set the transform, and the clip.
                Matrix transform = Geometry.CreateInvertedRectangleTransform(page.mapRectangle, page.printRectangle);
                PushRectangleClip(graphicsTarget, page.printRectangle);
                graphicsTarget.PushTransform(transform);
                // Determine the resolution in map coordinates.
                Matrix inverseTransform = transform.Clone();
                inverseTransform.Invert();
                float minResolutionPage = 100F / dpi;
                float minResolutionMap = Geometry.TransformDistance(minResolutionPage, inverseTransform);

                // And draw.
                mapDisplay.Draw(graphicsTarget, page.mapRectangle, minResolutionMap);

                graphicsTarget.PopTransform();
                graphicsTarget.PopClip();
            }
        }

        private void PushRectangleClip(IGraphicsTarget graphicsTarget, RectangleF rect)
        {
            object rectanglePath = new object();
            graphicsTarget.CreatePath(rectanglePath, new List<GraphicsPathPart>() {
                new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[] { rect.Location }),
                new GraphicsPathPart(GraphicsPathPartKind.Lines, new PointF[] { new PointF(rect.Right, rect.Top), new PointF(rect.Right, rect.Bottom), new PointF(rect.Left, rect.Bottom), new PointF(rect.Left, rect.Top)}),
                new GraphicsPathPart(GraphicsPathPartKind.Close, new PointF[0])
            }, FillMode.Winding);
            graphicsTarget.PushClip(rectanglePath);
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
                band.courseDesignator = page.courseDesignator;

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
        public bool AllCourses = true;          // If true, overrides the course ids in CourseIds except for "all controls".

        public int Count = 1;                         // count of copies to print
        public bool CropLargePrintArea = true;       // If true, crop a large print area instead of printing multiple pages 
        public bool PrintMapExchangesOnOneMap = false;
        public bool UseXpsPrinting = false;          // If true, use XPS printing; default to not.
        public bool PauseAfterCourseOrPart = false;  // If true, printing pauses after each course or part of course printed.
        public ColorModel PrintingColorModel = ColorModel.CMYK;

    }
}
