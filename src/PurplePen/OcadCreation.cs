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
using System.IO;
using System.Drawing;
using PurplePen.MapModel;
using PurplePen.Graphics2D;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace PurplePen
{
    class OcadCreation
    {
        SymbolDB symbolDB;
        EventDB eventDB;
        Controller controller;
        CourseAppearance courseAppearance;
        OcadCreationSettings creationSettings;

        public OcadCreation(SymbolDB symbolDB, EventDB eventDB, Controller controller, CourseAppearance courseAppearance, OcadCreationSettings creationSettings)
        {
            this.symbolDB = symbolDB;
            this.eventDB = eventDB;
            this.controller = controller;
            this.courseAppearance = courseAppearance;
            this.creationSettings = creationSettings;
        }

        // Write a map to the given file name.
        void ExportMap(CourseView courseView, string outputFilename)
        {
            // Create the CourseLayout.
            CourseLayout courseLayout = new CourseLayout();
            courseLayout.SetLayerColor(CourseLayer.Descriptions, NormalCourseAppearance.blackColorOcadId, NormalCourseAppearance.blackColorName, NormalCourseAppearance.blackColorC, NormalCourseAppearance.blackColorM, NormalCourseAppearance.blackColorY, NormalCourseAppearance.blackColorK);
            courseLayout.SetLayerColor(CourseLayer.MainCourse, NormalCourseAppearance.courseOcadId, NormalCourseAppearance.courseColorName,
                creationSettings.cyan, creationSettings.magenta, creationSettings.yellow, creationSettings.black);
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, courseAppearance, courseLayout, CourseLayer.MainCourse);

            // Create the map and write it out.
            Map map = courseLayout.RenderToMap();
            using (map.Write()) {
                map.MapScale = courseView.MapScale;
                map.PrintScale = courseView.PrintScale;
                map.PrintArea = controller.GetPrintArea(courseView.CourseDesignator);

                switch (controller.MapType) {
                    case MapType.OCAD:
                        // Set OCAD map as template.
                        // OCAD 6 doesn't support another OCAD file as a template.
                        if (creationSettings.version > 6)
                            map.Templates = new TemplateInfo[] {new TemplateInfo(controller.MapFileName, new PointF(0, 0), 0, 0, true)};

                        // Use same real world coordinates as underlying map (nicer, but also works around bug in OCAD 11
                        // where background maps with real world coordinates aren't displayed if the map map doesn't have same real
                        // world coordinates).
                        map.RealWorldCoords = controller.MapRealWorldCoords;
                        break;

                    case MapType.Bitmap:
                    case MapType.PDF:
                        // Set bitmap as template.
                        PointF centerPoint = Geometry.RectCenter(controller.MapDisplay.MapBounds);

                        ImageFormat imageFormat;
                        string mapFileName;
                        float dpi;
                        if (CreateBitmapFile()) {
                            // Write a copy of the bitmap map.
                            mapFileName = CreateBitmapFileName(out imageFormat);
                            controller.MapDisplay.WriteBitmapMap(mapFileName, imageFormat, out dpi);
                        }
                        else {
                            // Use existing map file.
                            mapFileName = controller.MapFileName;
                            dpi = controller.MapDpi;
                        }

                        map.Templates = new TemplateInfo[] {new TemplateInfo(mapFileName, centerPoint, dpi, 0, true)};
                        break;

                    case MapType.None:
                        break;
                    
                    default:
                        Debug.Fail("Unexpected map type");
                        break;
                }
            }

            InputOutput.WriteFile(outputFilename, map, creationSettings.version);
        }

        // Get the full output file name. Uses the name of the course, removes bad characters,
        // checks for duplication of the map file name. Puts in the directory given in the creationSettings.
        string CreateOutputFileName(CourseDesignator courseDesignator)
        {
            string basename = QueryEvent.CreateOutputFileName(eventDB, courseDesignator, creationSettings.filePrefix, ".ocd");

            return Path.GetFullPath(Path.Combine(creationSettings.outputDirectory, basename));
        }

        // Do we need to create a bitmap copy of the map file?
        // Always with PDF, only if the current format is incompatible with bitmaps.
        bool CreateBitmapFile()
        {
            if (controller.MapDisplay.MapType == MapType.PDF)
                return true;
            else if (controller.MapDisplay.MapType == MapType.Bitmap) {
                ImageFormat imageFormat = controller.MapDisplay.BitmapImageFormat;

                if (creationSettings.version <= 7)
                    return (imageFormat.Guid != ImageFormat.Bmp.Guid);
                else if (creationSettings.version <= 10) {
                    return (imageFormat.Guid != ImageFormat.Bmp.Guid && imageFormat.Guid != ImageFormat.Tiff.Guid && imageFormat.Guid != ImageFormat.Jpeg.Guid && imageFormat.Guid != ImageFormat.Gif.Guid);
                }
                else {
                    return (imageFormat.Guid != ImageFormat.Bmp.Guid && imageFormat.Guid != ImageFormat.Tiff.Guid && imageFormat.Guid != ImageFormat.Jpeg.Guid && imageFormat.Guid != ImageFormat.Gif.Guid && imageFormat.Guid != ImageFormat.Png.Guid);
                }
            }

            return false;
        }

        // PDF files need to have a bitmap saved with the OCAD file(s). Return the file name and format of the bitmap file.
        // The format used depends on the OCAD version we are targeting.
        string CreateBitmapFileName(out ImageFormat imageFormat)
        {
            string extension;

            Debug.Assert(controller.MapDisplay.MapType == MapType.PDF || controller.MapDisplay.MapType == MapType.Bitmap);

            if (creationSettings.version <= 7) {
                extension = ".bmp";
                imageFormat = ImageFormat.Bmp;
            }
            else if (creationSettings.version <= 10) {
                extension = ".gif";
                imageFormat = ImageFormat.Gif;
            }
            else {
                extension = ".png";
                imageFormat = ImageFormat.Png;
            }

            string basePdfName = Path.GetFileName(controller.MapFileName);
            return Path.GetFullPath(Path.Combine(creationSettings.outputDirectory, Path.ChangeExtension(basePdfName, extension)));
        }

        // Create a single OCAD file. 
        void CreateFile(CourseDesignator courseDesignator)
        {
            // Get the file name of the output.
            string outputFilename = CreateOutputFileName(courseDesignator);

            // Create the course view.
            CourseView courseView = CourseView.CreatePrintingCourseView(eventDB, courseDesignator);

            // Write the OCAD file.
            ExportMap(courseView, outputFilename);
        }

        // Enumerator all course designators to create.
        private IEnumerable<CourseDesignator> EnumerateCourseDesignators()
        {
            return QueryEvent.EnumerateCourseDesignators(eventDB, creationSettings.CourseIds, true);
        }

        // Create all the OCAD files according to their creation settings. Throws exception on I/O error.
        // The "mapDirectory" and "fileDirectory" fields are not used -- the "outputDirectory" MUST be
        // set to the directory to use.
        public void CreateOcadFiles()
        {
            foreach (CourseDesignator courseDesignator in EnumerateCourseDesignators()) {
                CreateFile(courseDesignator);
            }
        }

        // Determine if any files will be overwritten. Returns a list of file names that will be overwritten.
        public List<string> OverwrittenFiles()
        {
            List<string> overwrittenFiles = new List<string>();

            foreach (CourseDesignator courseDesignator in EnumerateCourseDesignators()) {
                string outputFilename = CreateOutputFileName(courseDesignator);
                if (File.Exists(outputFilename))
                    overwrittenFiles.Add(outputFilename);
            }

            if (CreateBitmapFile()) {
                ImageFormat imageFormat;
                string pdfBitmap = CreateBitmapFileName(out imageFormat);
                if (File.Exists(pdfBitmap))
                    overwrittenFiles.Add(pdfBitmap);
            }

            return overwrittenFiles;
        }
    }

    // Has all the settings for creating OCAD files.
    class OcadCreationSettings
    {
        public Id<Course>[] CourseIds;          // Courses to print. Course.None means all controls.
        public int version;                                // OCAD version to use (6,7,8,9)
        public bool mapDirectory, fileDirectory;   // directory to place output files in
        public string outputDirectory;              // the output directory if mapDirectory and fileDirectoy are false.
        public string filePrefix;                      // if non-null, non-empty, prefix this an "-" onto the front of files.
        public short colorOcadId;                         // ocadID for the purple stuff.
        public float cyan, magenta, yellow, black;   // color to use for the "Purple" stuff.

        public OcadCreationSettings Clone()
        {
            return (OcadCreationSettings) base.MemberwiseClone();
        }
    }
}
