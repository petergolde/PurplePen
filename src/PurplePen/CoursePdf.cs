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

    // Class to output courses to PDF
    class CoursePdf 
    {
        // List of all the pages, constructed during layout.
        private List<CoursePage> pages = new List<CoursePage>();

        private CoursePdfSettings coursePdfSettings;
        private EventDB eventDB;
        private SymbolDB symbolDB;
        private Controller controller;
        private MapDisplay mapDisplay;
        private CourseAppearance appearance;

        private RectangleF portraitPrintableArea, landscapePrintableArea;

        // mapDisplay is a MapDisplay that contains the correct map. All other features of the map display need to be customized.
        public CoursePdf(EventDB eventDB, SymbolDB symbolDB, Controller controller, MapDisplay mapDisplay, 
                         CoursePdfSettings coursePdfSettings, CourseAppearance appearance)
        {
            this.eventDB = eventDB;
            this.symbolDB = symbolDB;
            this.controller = controller;
            this.mapDisplay = mapDisplay;
            this.coursePdfSettings = coursePdfSettings;
            this.appearance = appearance;

            // Set default features for printing.
            mapDisplay.MapIntensity = 1.0F;
            mapDisplay.AntiAlias = false;
            mapDisplay.Printing = true;
            mapDisplay.ColorModel = coursePdfSettings.ColorModel;

            StorePrintableAreas();
        }

        // Get the printable area and store them.
        void StorePrintableAreas()
        {
            int height = coursePdfSettings.PageSettings.PaperSize.Height;
            int width = coursePdfSettings.PageSettings.PaperSize.Width;
            Margins margins = coursePdfSettings.PageSettings.Margins;

            portraitPrintableArea = new RectangleF(margins.Left, margins.Top, width - margins.Left - margins.Right, height - margins.Top - margins.Bottom);
            landscapePrintableArea = new RectangleF(margins.Top, margins.Left, height - margins.Top - margins.Bottom, width - margins.Left - margins.Right);
        }

        // Layout all the pages, return the total number of pages.
        int LayoutPages()
        {
            CoursePageLayout pageLayout = new CoursePageLayout(eventDB, symbolDB, controller, appearance, coursePdfSettings.CropLargePrintArea, portraitPrintableArea, landscapePrintableArea);
            pages = pageLayout.LayoutPages(coursePdfSettings.CourseIds, coursePdfSettings.PrintMapExchangesOnOneMap);

            return pages.Count;            // total number of pages.
        }

        // Create a single PDF file
        void CreateOnePdfFile(string fileName, IEnumerable<CourseDesignator> courseDesignators)
        {

        }

        // The core printing routine. 
        void DrawPage(IGraphicsTarget graphicsTarget, int pageNumber)
        {
            CoursePage page = pages[pageNumber];

            // Get the course view for the course we are printing.
            CourseView courseView = CourseView.CreatePrintingCourseView(eventDB, page.courseDesignator);

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

            // Set the transform, and the clip.
            Matrix transform = Geometry.CreateInvertedRectangleTransform(page.mapRectangle, page.printRectangle);
            PushRectangleClip(graphicsTarget, page.printRectangle);
            graphicsTarget.PushTransform(transform);
            // Determine the resolution in map coordinates.
            Matrix inverseTransform = transform.Clone();
            inverseTransform.Invert();
            float minResolutionPage = 100F / 2400F;  // Assume 2400 DPI as the base resolution, to get very accurate print.
            float minResolutionMap = Geometry.TransformDistance(minResolutionPage, inverseTransform);

            // And draw.
            mapDisplay.Draw(graphicsTarget, page.mapRectangle, minResolutionMap);

            graphicsTarget.PopTransform();
            graphicsTarget.PopClip();
        }

        private void PushRectangleClip(IGraphicsTarget graphicsTarget, RectangleF rect)
        {
            object rectanglePath = new object();
            graphicsTarget.CreatePath(rectanglePath, new GraphicsPathPart[] {
                new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[] { rect.Location }),
                new GraphicsPathPart(GraphicsPathPartKind.Lines, new PointF[] { new PointF(rect.Right, rect.Top), new PointF(rect.Right, rect.Bottom), new PointF(rect.Left, rect.Bottom), new PointF(rect.Left, rect.Top)}),
                new GraphicsPathPart(GraphicsPathPartKind.Close, new PointF[0])
            }, FillMode.Winding);
            graphicsTarget.PushClip(rectanglePath);
        }
    }

    // All the information needed to print courses.
    class CoursePdfSettings
    {
        private PageSettings pageSettings;

        public PageSettings PageSettings
        {
            get
            {
                if (pageSettings == null) {
                    pageSettings = new PageSettings();
                    pageSettings.Margins = new Margins(25, 25, 25, 25);    // Default is 1/4" margins
                }

                pageSettings.Landscape = false;                        // Set to not landscape. We change this on a page by page basis later.
                return pageSettings;
            }
            set
            {
                pageSettings = value;
            }
        }

        public Id<Course>[] CourseIds;          // Courses to print, None is all controls.

        public bool CropLargePrintArea = true;       // If true, crop a large print area instead of printing multiple pages 
        public bool PrintMapExchangesOnOneMap = false;
        public PdfFileCreation FileCreation = PdfFileCreation.FilePerCourse; 
        public ColorModel ColorModel = ColorModel.CMYK;

        public bool mapDirectory, fileDirectory;     // directory to place output files in
        public string outputDirectory;               // the output directory if mapDirectory and fileDirectoy are false.
        public string filePrefix;                    // if non-null, non-empty, prefix this an "-" onto the front of files.

        public enum PdfFileCreation { SingleFile, FilePerCourse, FilePerCoursePart };

        public CoursePdfSettings Clone()
        {
            CoursePdfSettings n = (CoursePdfSettings) base.MemberwiseClone();
            return n;
        }
    }
}
