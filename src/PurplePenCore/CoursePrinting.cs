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
        private EventDB eventDB;
        private SymbolDB symbolDB;
        private MapDisplay mapDisplay;
        private CourseAppearance appearance;

        // mapDisplay is a MapDisplay that contains the correct map. All other features of the map display need to be customized.
        public CoursePrinting(EventDB eventDB, SymbolDB symbolDB, Controller controller, MapDisplay mapDisplay, CoursePrintSettings coursePrintSettings, CourseAppearance appearance)
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
            mapDisplay.ColorModel = coursePrintSettings.PrintingColorModel;
        }

        // Layout all the pages, return the total number of pages.
        public int LayoutPages(PrintingPaperSizeWithMargins defaultPaperSizeWithMargins)
        {
            CoursePageLayout pageLayout = new CoursePageLayout(eventDB, symbolDB, controller, appearance, coursePrintSettings.CropLargePrintArea);
            IEnumerable<CourseDesignator> courseDesignators = QueryEvent.EnumerateCourseDesignators(eventDB, coursePrintSettings.CourseIds, coursePrintSettings.VariationChoicesPerCourse, !coursePrintSettings.PrintMapExchangesOnOneMap);
            List<CoursePage> layoutPages = pageLayout.LayoutPages(courseDesignators);

            // Copies are made here, rather than by the printer, uncollated: all the copies of
            // one page, then all the copies of the next, etc.
            int copies = Math.Max(1, coursePrintSettings.Count);
            pages = new List<CoursePage>();
            foreach (CoursePage page in layoutPages) {
                for (int copy = 0; copy < copies; ++copy)
                    pages.Add(page);
            }

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
        // in hundreths of an inch. The page is drawn directly onto grTarget; purple blending is
        // correct only if grTarget supports blending, so printing targets that don't (e.g., GDI+
        // printing) should render the page into bitmap bands first.
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

            // Set the transform, and the clip.
            Matrix transform = Geometry.CreateInvertedRectangleTransform(page.mapRectangle, page.printRectangle);
            grTarget.PushClip(page.printRectangle);
            grTarget.PushTransform(transform);

            // Determine the resolution in map coordinates: one printer dot.
            Matrix inverseTransform = transform.Clone();
            inverseTransform.Invert();
            float minResolutionPage = 100F / dpi;
            float minResolutionMap = Geometry.TransformDistance(minResolutionPage, inverseTransform);

            // And draw.
            mapDisplay.Draw(grTarget, page.mapRectangle, minResolutionMap, null);

            grTarget.PopTransform();
            grTarget.PopClip();
        }

        public void PrintingComplete()
        {
        }
    }
}
