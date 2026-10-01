using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
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
    public class BitmapCreation
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
        private GraphicsBitmapFormat GetImageFormat()
        {
            switch (bitmapCreationSettings.ExportedBitmapKind) {
                case BitmapCreationSettings.BitmapKind.Gif:
                    return GraphicsBitmapFormat.GIF;
                case BitmapCreationSettings.BitmapKind.Png:
                    return GraphicsBitmapFormat.PNG;
                case BitmapCreationSettings.BitmapKind.Jpeg:
                    return GraphicsBitmapFormat.JPEG;
                default:
                    throw new ApplicationException("Unknown bitmap kind");
            }
        }

        // Create a single PDF file
        void CreateOneBitmap(string fileName, CourseDesignator courseDesignator)
        {
            MapDisplay currentMapDisplay = mapDisplay.Clone();

            RectangleF mapRectangle = controller.GetCurrentPrintAreaRectangle(courseDesignator);

            // Get the course view for the course we are printing.
            CourseView courseView = CourseView.CreatePrintingCourseView(eventDB, courseDesignator);

            // Get the correct purple color to print the course in.
            short ocadId;
            float purpleC, purpleM, purpleY, purpleK;
            bool purpleOverprint;
            FindPurple.GetPurpleColor(currentMapDisplay, appearance, out ocadId, out purpleC, out purpleM, out purpleY, out purpleK, out purpleOverprint);

            // Create a course layout from the view.
            CourseLayout layout = new CourseLayout();
            layout.SetLayerColor(CourseLayer.Descriptions, NormalCourseAppearance.blackColorOcadId, NormalCourseAppearance.blackColorName, NormalCourseAppearance.blackColorC, NormalCourseAppearance.blackColorM, NormalCourseAppearance.blackColorY, NormalCourseAppearance.blackColorK, false);
            layout.SetLayerColor(CourseLayer.MainCourse, ocadId, NormalCourseAppearance.courseColorName, purpleC, purpleM, purpleY, purpleK, purpleOverprint);
            layout.SetLowerLayerColor(CourseLayer.MainCourse, NormalCourseAppearance.lowerPurpleOcadId, NormalCourseAppearance.lowerPurpleColorName, purpleC, purpleM, purpleY, purpleK, purpleOverprint);
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, appearance, layout, CourseLayer.MainCourse);

            // Set the course layout into the map display
            currentMapDisplay.SetCourse(layout);
            currentMapDisplay.SetPrintArea(null);

            CoordinateMapper coordinateMapper = bitmapCreationSettings.WorldFile ? currentMapDisplay.CoordinateMapper : null;

            if (bitmapCreationSettings.DontPrintBaseMap) {
                // Remove the base map.
                currentMapDisplay.SetMapFile(MapType.None, null);
            }

            ExportBitmap exportBitmap = new ExportBitmap(currentMapDisplay);
            exportBitmap.CreateBitmap(fileName, mapRectangle, GetImageFormat(), bitmapCreationSettings.Dpi, coordinateMapper);
        }
    }
}
