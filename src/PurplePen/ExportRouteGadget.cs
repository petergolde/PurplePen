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
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace PurplePen
{
    class ExportRouteGadget
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
            exportBitmap.CreateBitmapAutoDpi(gifFileName, mapArea, ImageFormat.Gif, MAXPIXELWITH, MINDPI, MAXDPI, mapDisplay.CoordinateMapper);
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
