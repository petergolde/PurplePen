using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using PurplePen.Graphics2D;

namespace PurplePen
{
    public class ExportRouteGadget
    {
        SymbolDB symbolDB;
        EventDB eventDB;
        Controller controller;
        MapDisplay mapDisplay;

        const int MAXPIXELWITH = 2000;
        const float MINDPI = 140;
        const float MAXDPI = 200;

        public ExportRouteGadget(SymbolDB symbolDB, EventDB eventDB, Controller controller, MapDisplay mapDisplay)
        {
            this.symbolDB = symbolDB;
            this.eventDB = eventDB;
            this.controller = controller;
            this.mapDisplay = mapDisplay.CloneToFullIntensity();
            this.mapDisplay.SetCourse(null);
            this.mapDisplay.SetPrintArea(null);
            this.mapDisplay.ColorModel = ColorModel.CMYK;
        }

        public void ExportXml(string xmlFileName, int version)
        {
            // Get the area to export.
            RectangleF mapArea = GetAllPrintAreas();

            // Export the XML file.
            ExportXmlBase exportXml;
            if (version == 2)
                exportXml = new ExportXmlVersion2();
            else if (version == 3)
                exportXml = new ExportXmlVersion3();
            else
                throw new ApplicationException("Unknown XML version " + version.ToString());

            exportXml.WriteXml(xmlFileName, eventDB, mapArea, mapDisplay.CoordinateMapper);
        }

        public void ExportGif(string gifFileName)
        {
            // Get the area to export.
            RectangleF mapArea = GetAllPrintAreas();

            // Export the GIF file.
            ExportBitmap exportBitmap = new ExportBitmap(mapDisplay);
            exportBitmap.CreateBitmapAutoDpi(gifFileName, mapArea, GraphicsBitmapFormat.GIF, MAXPIXELWITH, MINDPI, MAXDPI, mapDisplay.CoordinateMapper);
        }

        // Get the union of all the print areas in the event.
        private RectangleF GetAllPrintAreas()
        {
            RectangleF mergedRect = new RectangleF();
            RectangleF mapBounds = mapDisplay.MapBounds;

            bool first = true;
            foreach (Id<Course> courseId in eventDB.AllCourseIds) {
                RectangleF courseArea = GetPrintArea(courseId);
                if (first)
                    mergedRect = courseArea;
                else
                    mergedRect = RectangleF.Union(mergedRect, courseArea);
                first = false;
            }

            // If there were no courses, then use the map bounds, otherwise intersect the map bounds with the merged courses.
            if (first)
                return mapBounds;
            else
                return RectangleF.Intersect(mergedRect, mapBounds);
        }
            
        // Get the print area that encloses the given courseId.
        private RectangleF GetPrintArea(Id<Course> courseId)
        {
            return controller.GetCurrentPrintAreaRectangle(new CourseDesignator(courseId));
        }
    }
}
