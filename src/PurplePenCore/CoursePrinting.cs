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
using System.Diagnostics;


namespace PurplePen
{
    using PurplePen.Graphics2D;
    using PurplePen.MapModel;

    // Class to print out courses.
    class CoursePrinting: IPrintable
    {
        // List of all the pages, constructed during layout.
        private List<CoursePage> pages = new List<CoursePage>();

        private Controller controller;
        private CoursePrintSettings coursePrintSettings;
        private PrintingPaperSizeWithMargins paperSizeWithMargins;
        private EventDB eventDB;
        private SymbolDB symbolDB;
        private MapDisplay mapDisplay;
        private CourseAppearance appearance;

        // mapDisplay is a MapDisplay that contains the correct map. All other features of the map display need to be customized.
        public CoursePrinting(EventDB eventDB, SymbolDB symbolDB, Controller controller, MapDisplay mapDisplay, CoursePrintSettings coursePrintSettings, PrintingPaperSizeWithMargins paperSizeWithMargins, CourseAppearance appearance)
        {
            this.eventDB = eventDB;
            this.symbolDB = symbolDB;
            this.controller = controller;
            this.mapDisplay = mapDisplay;
            this.coursePrintSettings = coursePrintSettings;
            this.paperSizeWithMargins = paperSizeWithMargins;
            this.appearance = appearance;

            // Set default features for printing.
            mapDisplay.MapIntensity = 1.0F;
            mapDisplay.AntiAlias = false;
            mapDisplay.Printing = true;
            mapDisplay.ColorModel = coursePrintSettings.PrintingColorModel;
        }

        // Layout all the pages, return the total number of pages.
        public int LayoutPages(PrintingPaperSizeWithMargins defaultPaperSizeWithMargins) 
        {
#if !PORTING
            // We need to deal with Copies and Collating manually, instead of relying on the Windows print system.
            pageSettings.PrinterSettings.Copies = (short)coursePrintSettings.Count;
            pageSettings.PrinterSettings.Collate = false;      // print all of one course, then all of next, etc.

#endif

            CoursePageLayout pageLayout = new CoursePageLayout(eventDB, symbolDB, controller, appearance, coursePrintSettings.CropLargePrintArea);
            IEnumerable<CourseDesignator> courseDesignators = QueryEvent.EnumerateCourseDesignators(eventDB, coursePrintSettings.CourseIds, coursePrintSettings.VariationChoicesPerCourse, !coursePrintSettings.PrintMapExchangesOnOneMap);
            pages = pageLayout.LayoutPages(courseDesignators);

            return pages.Count;            // total number of pages.
        }

        // Set page size and landscape/portrait for a particular page.
        public PrintingPaperSize GetPagePaperSize(int pageNumber)
        {
            PrintingPaperSize paperSize = pages[pageNumber - 1].paperSize;
            bool landscape = pages[pageNumber - 1].landscape;
            return new PrintingPaperSize(landscape, paperSize);
        }

#if !PORTING
        // Need to implement the pausing functionality still, maybe. Not sure how this 
        // integrates into IPrintable.

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
#endif

        // The core printing routine. The origin of the graphics is the upper-left of the margins,
        // and the printArea in the size to draw into (in hundreths of an inch).
        //protected override void DrawPage(IGraphicsTarget graphicsTarget, int pageNumber, SizeF printArea, float dpi)
        public void DrawPage(IGraphicsTarget grTarget, int pageNumber, float dpi)
        {
            CoursePage page = pages[pageNumber - 1];

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
            layout.SetLowerLayerColor(CourseLayer.MainCourse, NormalCourseAppearance.lowerPurpleOcadId, NormalCourseAppearance.lowerPurpleColorName, purpleC, purpleM, purpleY, purpleK, purpleOverprint);

            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, appearance, layout, CourseLayer.MainCourse);

            // Set the course layout into the map display
            mapDisplay.SetCourse(layout);
            this.mapDisplay.SetPrintArea(null);

            // Collecting garbage should make out of memory less common.
            GC.Collect();

            // We print to intermediate bands of bitmaps. This is the only way to get purple blending correct.
            // Is also insures that we get the most accurate print.

            //GDIPlus_GraphicsTarget gdiGraphicsTarget = ((GDIPlus_GraphicsTarget)graphicsTarget);
            //Graphics g = gdiGraphicsTarget.Graphics;
            //// Save and restore state so we can mess with stuff.
            //GraphicsState graphicsState = g.Save();

            // Adjust the DPI so it isn't too high or too low. So printer drivers are bad.
            dpi = AdjustDpi(dpi);

            const long MAX_PIXELS_PER_BAND = 20000000;    // 20M pixels = 60M bytes (3 bytes per pixel).
            List<CoursePage> bands = BandPageToLimitBitmapSize(page, dpi, MAX_PIXELS_PER_BAND);

            // Create the bitmap. Can do this once because each band is the same size.
            int bitmapWidth = (int) Math.Round(bands[0].printRectangle.Width * dpi / 100F);
            int bitmapHeight = (int) Math.Round(bands[0].printRectangle.Height * dpi / 100F);
            IGraphicsBitmap bitmap = Services.BitmapLoader.CreateEmptyBitmap(bitmapWidth, bitmapHeight, null);

            foreach (CoursePage band in bands) {
                // Set the transform
                Matrix transform = Geometry.CreateInvertedRectangleTransform(band.mapRectangle, new RectangleF(0, 0, bitmapWidth, bitmapHeight));
                mapDisplay.Draw(bitmap, transform);

                // Draw the bitmap on the printer.
                grTarget.DrawBitmap(bitmap, band.printRectangle, BitmapScaling.HighQuality);
            }

            // And we are done with the bitmap.
            bitmap.Dispose();
        }

        public void PrintingComplete()
        {
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
}
