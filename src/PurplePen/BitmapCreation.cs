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
    using System.Drawing.Imaging;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    // Class to output courses to bitmaps
    class BitmapCreation
    {
        private BitmapCreationSettings bitmapCreationSettings;
        private EventDB eventDB;
        private SymbolDB symbolDB;
        private Controller controller;
        private MapDisplay mapDisplay;
        private CourseAppearance appearance;

        // mapDisplay is a MapDisplay that contains the correct map. All other features of the map display need to be customized.
        public BitmapCreation(EventDB eventDB, SymbolDB symbolDB, Controller controller, MapDisplay mapDisplay,
                              BitmapCreationSettings bitmapCreationSettings, CourseAppearance appearance)
        {
            this.eventDB = eventDB;
            this.symbolDB = symbolDB;
            this.controller = controller;
            this.mapDisplay = mapDisplay.CloneToFullIntensity();
            this.bitmapCreationSettings = bitmapCreationSettings;
            this.appearance = appearance;

            // Set default features for creating bitmaps.
            this.mapDisplay.MapIntensity = 1.0F;
            this.mapDisplay.AntiAlias = true;
            this.mapDisplay.ColorModel = bitmapCreationSettings.ColorModel;
        }

        public List<string> OverwrittenFiles()
        {
            return (from filePair in GetFilesToCreate()
                    let fileName = filePair.First
                    where File.Exists(fileName)
                    select fileName).ToList();
        }

        public void CreateBitmaps()
        {
            List<Pair<string, CourseDesignator>> fileList = GetFilesToCreate();

            foreach (var pair in fileList) {
                CreateOneBitmap(pair.First, pair.Second);
            }
        }

        // Get the files that we should create. along with the corresponding courses on them.
#if TEST
        internal
#endif
        List<Pair<string, CourseDesignator>> GetFilesToCreate()
        {
            List<Pair<string, CourseDesignator>> fileList = new List<Pair<string, CourseDesignator>>();

            // Create a file for each course part or variation (or both)
            foreach (CourseDesignator designator in
                     QueryEvent.EnumerateCourseDesignators(eventDB, bitmapCreationSettings.CourseIds,
                                                           bitmapCreationSettings.VariationChoicesPerCourse, !bitmapCreationSettings.PrintMapExchangesOnOneMap)) {
                fileList.Add(new Pair<string, CourseDesignator>(CreateOutputFileName(designator), designator));
            }

            return fileList;
        }

        // Get the full output file name. Uses the name of the course, removes bad characters,
        // checks for duplication of the map file name. Puts in the directory given in the creationSettings.
        string CreateOutputFileName(CourseDesignator courseDesignator)
        {
            string basename = QueryEvent.CreateOutputFileName(eventDB, courseDesignator, bitmapCreationSettings.filePrefix, "", GetFileExtension());

            return Path.GetFullPath(Path.Combine(bitmapCreationSettings.outputDirectory, basename));
        }

        // Get the file extensions for the type of bitmap file we are creating.
        private string GetFileExtension()
        {
            switch (bitmapCreationSettings.ExportedBitmapKind) {
                case BitmapCreationSettings.BitmapKind.Gif:
                    return ".gif";
                case BitmapCreationSettings.BitmapKind.Png:
                    return ".png";
                case BitmapCreationSettings.BitmapKind.Jpeg:
                    return ".jpg";
                default:
                    throw new ApplicationException("Unknown bitmap kind");
            }
        }

        // Get the image format for the type of bitmap file we are creating.
        private ImageFormat GetImageFormat()
        {
            switch (bitmapCreationSettings.ExportedBitmapKind) {
                case BitmapCreationSettings.BitmapKind.Gif:
                    return ImageFormat.Gif;
                case BitmapCreationSettings.BitmapKind.Png:
                    return ImageFormat.Png;
                case BitmapCreationSettings.BitmapKind.Jpeg:
                    return ImageFormat.Jpeg;
                default:
                    throw new ApplicationException("Unknown bitmap kind");
            }
        }

        // Create a single PDF file
        void CreateOneBitmap(string fileName, CourseDesignator courseDesignator)
        {
            RectangleF mapRectangle = controller.GetCurrentPrintAreaRectangle(courseDesignator);

            // Get the course view for the course we are printing.
            CourseView courseView = CourseView.CreatePrintingCourseView(eventDB, courseDesignator);

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
            mapDisplay.SetPrintArea(null);

            ExportBitmap exportBitmap = new ExportBitmap(mapDisplay);
            exportBitmap.CreateBitmap(fileName, mapRectangle, GetImageFormat(), bitmapCreationSettings.Dpi,
                                      bitmapCreationSettings.WorldFile ? mapDisplay.CoordinateMapper : null);
        }
    }

    // All the information needed to create bitmaps.
    class BitmapCreationSettings
    {
        public Id<Course>[] CourseIds;          // Courses to print, None is all controls.
        public bool AllCourses = true;          // If true, overrides CourseIds except for all controls.

        public bool PrintMapExchangesOnOneMap = false;
        public BitmapKind ExportedBitmapKind = BitmapCreationSettings.BitmapKind.Png;
        public float Dpi;
        public bool WorldFile;                      // Create a world file?
        public ColorModel ColorModel = ColorModel.CMYK;

        public bool mapDirectory, fileDirectory;     // directory to place output files in
        public string outputDirectory;               // the output directory if mapDirectory and fileDirectoy are false.
        public string filePrefix;                    // if non-null, non-empty, prefix this an "-" onto the front of files.

        // variation choices for courses with variations.
        public Dictionary<Id<Course>, VariationChoices> VariationChoicesPerCourse = new Dictionary<Id<Course>, VariationChoices>();

        public enum BitmapKind { Gif, Png, Jpeg };

        public BitmapCreationSettings Clone()
        {
            BitmapCreationSettings n = (BitmapCreationSettings)base.MemberwiseClone();
            return n;
        }
    }
}
