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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using PurplePen.Livelox.ApiContracts;

namespace PurplePen.Livelox
{
    class PublishManager
    {
        private const string mapFileName = "map.png";
        private const string courseDataFileName = "coursedata.xml";

        public ImportableEvent CreateImportableEvent(Controller controller, SymbolDB symbolDB, double resolution, string temporaryDirectory)
        {
            var eventDB = controller.GetEventDB();

            CreateMapImage(controller.MapDisplay, resolution, temporaryDirectory);
            CreateCourseDataXml(eventDB, controller.MapDisplay, temporaryDirectory);
            CreateCourseImages(controller, eventDB, symbolDB, controller.MapDisplay, temporaryDirectory);

            var importableEvent = CreateImportableEventObject(eventDB, controller.MapDisplay, temporaryDirectory);

            return importableEvent;
        }

        public string CreateTemporaryDirectory()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public void DeleteTemporatyDirectory(string temporaryDirectory)
        {
            try
            {
                Directory.Delete(temporaryDirectory);
            }
            catch
            {
                // just ignore, after all it is temporary
            }
        }

        private static void CreateMapImage(MapDisplay mapDisplay, double resolution, string temporaryDirectory)
        {
            var dpi = (float)(resolution /* in pixels per real-world meter */ * mapDisplay.MapScale * 0.0254);

            if (mapDisplay.MapType == MapType.Bitmap)
            {
                // no need to export in higher resolution than the bitmap's resolution
                dpi = Math.Min(dpi, mapDisplay.Dpi);
            }

            var clonedMapDisplay = mapDisplay.CloneToFullIntensity();
            clonedMapDisplay.AntiAlias = false;
            clonedMapDisplay.SetCourse(null);
            clonedMapDisplay.SetPrintArea(null);
            clonedMapDisplay.ColorModel = ColorModel.CMYK;
            
            var mapExporter = new ExportBitmap(clonedMapDisplay);
            mapExporter.CreateBitmap(
                Path.Combine(temporaryDirectory, mapFileName),
                clonedMapDisplay.Bounds,
                ImageFormat.Png,
                dpi,
                clonedMapDisplay.CoordinateMapper
            );
        }

        private static void CreateCourseDataXml(EventDB eventDB, MapDisplay mapDisplay, string temporaryDirectory)
        {
            var xmlExporter = new ExportXmlVersion3();
            xmlExporter.WriteXml(
                Path.Combine(temporaryDirectory, courseDataFileName),
                eventDB,
                mapDisplay.MapBounds,
                mapDisplay.CoordinateMapper
            );
        }

        private static void CreateCourseImages(Controller controller, EventDB eventDB, SymbolDB symbolDB, MapDisplay mapDisplay, string temporaryDirectory)
        {
            var coursePdfSettings = new CoursePdfSettings
            {
                mapDirectory = false,
                fileDirectory = false,
                outputDirectory = temporaryDirectory,
                ColorModel = ColorModel.CMYK,
                CourseIds = eventDB.AllCourseIds.ToArray(),
                CropLargePrintArea = true,
                FileCreation = CoursePdfSettings.PdfFileCreation.FilePerCourse,
                PrintMapExchangesOnOneMap = true,
                RenderMap = false,
                RenderControlDescriptions = false,
                ShowProgressDialog = false
            };

            var clonedMapDisplay = mapDisplay.Clone();
            clonedMapDisplay.SetMapFile(MapType.None, null);
            var ev = eventDB.GetEvent();
            var courseAppearance = (CourseAppearance)ev.courseAppearance.Clone();
            courseAppearance.renderDescriptions = false;

            var coursePdf = new CoursePdf(eventDB, symbolDB, controller, clonedMapDisplay, coursePdfSettings, courseAppearance);
            coursePdf.CreatePdfs();
        }

        private static ImportableEvent CreateImportableEventObject(EventDB eventDB, MapDisplay mapDisplay, string temporaryDirectory)
        {
            var ev = eventDB.GetEvent();

            Rectangle mapImageRectangle;
            using (var mapImage = Image.FromFile(Path.Combine(temporaryDirectory, mapFileName)))
            {
                mapImageRectangle = new Rectangle(0, 0, mapImage.Width, mapImage.Height);
            }

            var map = new Map()
            {
                Name = Path.GetFileNameWithoutExtension(ev.mapFileName),
                FileName = "map.png",
                MapScale = ev.mapScale,
                // note the order of Top and Bottom; Top has a lower value than Bottom
                BottomLeftCornerPosition = new MapCoordinate() { X = mapDisplay.Bounds.Left / 1000, Y = mapDisplay.Bounds.Top / 1000 }, 
                TopRightCornerPosition = new MapCoordinate() { X = mapDisplay.Bounds.Right / 1000, Y = mapDisplay.Bounds.Bottom / 1000 },
                Georeference = new Georeference()
                {
                    CoordinateMapping = new CoordinateMapping()
                    {
                        Positions = new[]
                        {
                            // note the order of Top and Bottom; Top has a lower value than Bottom
                            GetGeoPosition(mapDisplay.Bounds.Left, mapDisplay.Bounds.Top, mapDisplay.CoordinateMapper),      // bottom left
                            GetGeoPosition(mapDisplay.Bounds.Right, mapDisplay.Bounds.Top, mapDisplay.CoordinateMapper),     // bottom right
                            GetGeoPosition(mapDisplay.Bounds.Right, mapDisplay.Bounds.Bottom, mapDisplay.CoordinateMapper),  // top right
                            GetGeoPosition(mapDisplay.Bounds.Left, mapDisplay.Bounds.Bottom, mapDisplay.CoordinateMapper)    // top left
                        },
                        ImagePositions = new[]
                        {
                            new ImageCoordinate() {X = 0, Y = mapImageRectangle.Height},                        // bottom left
                            new ImageCoordinate() {X = mapImageRectangle.Width, Y = mapImageRectangle.Height},  // bottom right
                            new ImageCoordinate() {X = mapImageRectangle.Width, Y = 0},                         // top right
                            new ImageCoordinate() {X = 0, Y = 0}                                                // top left
                        }
                    }
                }
            };
            if (map.Georeference.CoordinateMapping.Positions.Any(o => o == null))
            {
                // there is no georeference present
                map.Georeference = null;
            }

            var importableEvent = new ImportableEvent()
            {
                Name = ev.title,
                Maps = new[] { map },
                CourseDataFileNames = new[] { "coursedata.xml" },
                CourseImageFileNames = new DirectoryInfo(temporaryDirectory)
                    .GetFiles("*.pdf")
                    .Select(file => file.Name)
                    .ToArray()
            };

            return importableEvent;
        }

        private static GeoCoordinate GetGeoPosition(float x, float y, CoordinateMapper coordinateMapper)
        {
            if (coordinateMapper.GetLatLong(new PointF(x, y), out var latitude, out var longitude))
            {
                return new GeoCoordinate()
                {
                    Latitude = latitude,
                    Longitude = longitude
                };
            }
            return null;
        }
    }
}