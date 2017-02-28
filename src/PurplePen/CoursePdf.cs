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
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using Margins = System.Drawing.Printing.Margins;
using PaperSize = System.Drawing.Printing.PaperSize;
using System.Diagnostics;


namespace PurplePen
{
    using PurplePen.Graphics2D;
    using PurplePen.MapModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using PdfSharp.Pdf;
    using PdfSharp.Drawing;

    // Class to output courses to PDF
    class CoursePdf 
    {
        private CoursePdfSettings coursePdfSettings;
        private EventDB eventDB;
        private SymbolDB symbolDB;
        private Controller controller;
        private MapDisplay mapDisplay;
        private CourseAppearance appearance;
        private RectangleF mapBounds;  // bounds of the map, in map coordinates.
        private string sourcePdfMapFileName;
        private PdfImporter pdfImporter;
        private PdfPage pdfMapPage;
        private int totalPages, currentPage;

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

            mapBounds = mapDisplay.MapBounds;

            if (mapDisplay.MapType == MapType.PDF) {
                // For PDF maps, we remove the PDF map from the MapDisplay and add it in separately.
                sourcePdfMapFileName = mapDisplay.FileName;
            }
        }

        // Is the map a PDF map?
        private bool IsPdfMap
        {
            get { return sourcePdfMapFileName != null; }
        }

        public List<string> OverwrittenFiles()
        {
            return (from filePair in GetFilesToCreate() 
                    let fileName = filePair.First
                    where File.Exists(fileName)
                    select fileName).ToList();
        }

        public void CreatePdfs()
        {
            List<Pair<string, IEnumerable<CourseDesignator>>> fileList = GetFilesToCreate();
            pdfImporter = null;

            if (IsPdfMap) {
                pdfImporter = new PdfImporter(sourcePdfMapFileName);
                try {
                    pdfMapPage = pdfImporter.GetPage(0);
                }
                catch (Exception) {
                    // We couldn't import the page. Fall back to normal map rendering methods.
                    sourcePdfMapFileName = null; // IsPdfMap will now be false.
                }
            }

            totalPages = 0;
            foreach (var pair in fileList) {
                totalPages += LayoutPages(pair.Second).Count;
            }

            controller.ShowProgressDialog(true);

            try {
                currentPage = 0;
                foreach (var pair in fileList) {
                    CreateOnePdfFile(pair.First, pair.Second);
                }
            }
            finally {
                if (pdfImporter != null) {
                    pdfImporter.Dispose();
                    pdfImporter = null;
                }

                controller.EndProgressDialog();
            }
        }

        // Get the files that we should create. along with the corresponding courses on them.
#if TEST
        internal
#endif
        List<Pair<string, IEnumerable<CourseDesignator>>> GetFilesToCreate()
        {
            List<Pair<string, IEnumerable<CourseDesignator>>> fileList = new List<Pair<string, IEnumerable<CourseDesignator>>>();

            switch (coursePdfSettings.FileCreation) {
                case CoursePdfSettings.PdfFileCreation.SingleFile:
                    // All pages go into a single file.
                    fileList.Add(new Pair<string, IEnumerable<CourseDesignator>>(CreateOutputFileName(null),
                                 QueryEvent.EnumerateCourseDesignators(eventDB, coursePdfSettings.CourseIds, coursePdfSettings.VariationChoicesPerCourse, !coursePdfSettings.PrintMapExchangesOnOneMap)));
                    break;

                case CoursePdfSettings.PdfFileCreation.FilePerCourse:
                    // Create a file for each course.
                    foreach (Id<Course> courseId in coursePdfSettings.CourseIds) {
                        fileList.Add(new Pair<string, IEnumerable<CourseDesignator>>(CreateOutputFileName(new CourseDesignator(courseId)),
                                     QueryEvent.EnumerateCourseDesignators(eventDB, new Id<Course>[1] { courseId }, coursePdfSettings.VariationChoicesPerCourse, !coursePdfSettings.PrintMapExchangesOnOneMap)));
                    }
                    break;

                case CoursePdfSettings.PdfFileCreation.FilePerCoursePart:
                    // Create a file for each course part or variation (or both)
                    foreach (CourseDesignator designator in 
                             QueryEvent.EnumerateCourseDesignators(eventDB, coursePdfSettings.CourseIds, 
                                                                   coursePdfSettings.VariationChoicesPerCourse, !coursePdfSettings.PrintMapExchangesOnOneMap)) {
                        fileList.Add(new Pair<string, IEnumerable<CourseDesignator>>(CreateOutputFileName(designator), new[] { designator }));
                    }

                    break;
            }

            return fileList;
        }

        // Get the full output file name. Uses the name of the course, removes bad characters,
        // checks for duplication of the map file name. Puts in the directory given in the creationSettings.
        string CreateOutputFileName(CourseDesignator courseDesignator)
        {
            string basename = QueryEvent.CreateOutputFileName(eventDB, courseDesignator, coursePdfSettings.filePrefix, "", ".pdf");

            return Path.GetFullPath(Path.Combine(coursePdfSettings.outputDirectory, basename));
        }

        // Create a single PDF file
        void CreateOnePdfFile(string fileName, IEnumerable<CourseDesignator> courseDesignators)
        {
            List<CoursePage> pages = LayoutPages(courseDesignators);
            PdfWriter pdfWriter = new PdfWriter(Path.GetFileNameWithoutExtension(fileName), coursePdfSettings.ColorModel == ColorModel.CMYK);

            foreach (CoursePage page in pages) {
                CoursePage pageToDraw = page;

                SizeF paperSize = new SizeF(pageToDraw.paperSize.Width / 100F, pageToDraw.paperSize.Height / 100F);
                if (pageToDraw.landscape)
                    paperSize = new SizeF(paperSize.Height, paperSize.Width);

                if (controller.UpdateProgressDialog(string.Format(MiscText.CreatingFile, Path.GetFileName(fileName)), (double)currentPage / (double)totalPages))
                    throw new Exception(MiscText.CancelledByUser);

                IGraphicsTarget grTarget;

                if (IsPdfMap) {
                    float scaleRatio = CourseView.CreatePrintingCourseView(eventDB, page.courseDesignator).ScaleRatio;
                    if (scaleRatio == 1.0) {
                        // If we're doing a PDF at scale 1, we just copy the page directly.
                        grTarget = pdfWriter.BeginCopiedPage(pdfMapPage);
                        pageToDraw = PdfNonScaledPage(page.courseDesignator);
                    }
                    else {
                        Matrix transform = Geometry.CreateInvertedRectangleTransform(page.printRectangle, page.mapRectangle);
                        RectangleF printedPortionInMapCoords = Geometry.TransformRectangle(transform, new RectangleF(0, 0, paperSize.Width * 100F, paperSize.Height * 100F));
                        RectangleF printedPortionInInches = new RectangleF(
                            Geometry.InchesFromMm(printedPortionInMapCoords.Left),
                            Geometry.InchesFromMm(mapBounds.Height - printedPortionInMapCoords.Bottom),
                            Geometry.InchesFromMm(printedPortionInMapCoords.Width),
                            Geometry.InchesFromMm(printedPortionInMapCoords.Height));

                        grTarget = pdfWriter.BeginCopiedPartialPage(pdfImporter, 0, paperSize, printedPortionInInches);
                    }

                    // Don't draw the map normally.
                    mapDisplay.SetMapFile(MapType.None, null);
                }
                else {
                    grTarget = pdfWriter.BeginPage(paperSize);
                }

                DrawPage(grTarget, pageToDraw);
                pdfWriter.EndPage(grTarget);
                grTarget.Dispose();

                currentPage += 1;
            }

            pdfWriter.Save(fileName);
        }

        // Layout the pages for a set of course designators.
        List<CoursePage> LayoutPages(IEnumerable<CourseDesignator> courseDesignators)
        {
            CoursePageLayout pageLayout = new CoursePageLayout(eventDB, symbolDB, controller, appearance,
                                                                coursePdfSettings.CropLargePrintArea);

            return pageLayout.LayoutPages(courseDesignators);
        }

        private CoursePage PdfNonScaledPage(CourseDesignator designator)
        {
            RectangleF pageArea = new RectangleF(0, 0, Geometry.HundredthsInchesFromMm(mapBounds.Width), Geometry.HundredthsInchesFromMm(mapBounds.Height));
            return new CoursePage() {
                courseDesignator = designator,
                landscape = false,
                mapRectangle = mapBounds,
                printRectangle = pageArea,
                paperSize = new PaperSize("", (int) Math.Round(pageArea.Width), (int) Math.Round(pageArea.Height))
            };
        }

        // The core printing routine. 
        void DrawPage(IGraphicsTarget graphicsTarget, CoursePage page)
        {
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
            mapDisplay.SetPrintArea(null);

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
            graphicsTarget.CreatePath(rectanglePath, new List<GraphicsPathPart> {
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
        public Id<Course>[] CourseIds;          // Courses to print, None is all controls.
        public bool AllCourses = true;          // If true, overrides CourseIds except for all controls.

        public bool CropLargePrintArea = true;       // If true, crop a large print area instead of printing multiple pages 
        public bool PrintMapExchangesOnOneMap = false;
        public PdfFileCreation FileCreation = PdfFileCreation.FilePerCourse; 
        public ColorModel ColorModel = ColorModel.CMYK;

        public bool mapDirectory, fileDirectory;     // directory to place output files in
        public string outputDirectory;               // the output directory if mapDirectory and fileDirectoy are false.
        public string filePrefix;                    // if non-null, non-empty, prefix this an "-" onto the front of files.

        // variation choices for courses with variations.
        public Dictionary<Id<Course>, VariationChoices> VariationChoicesPerCourse = new Dictionary<Id<Course>, VariationChoices>();

        public enum PdfFileCreation { SingleFile, FilePerCourse, FilePerCoursePart };

        public CoursePdfSettings Clone()
        {
            CoursePdfSettings n = (CoursePdfSettings) base.MemberwiseClone();
            return n;
        }
    }
}
