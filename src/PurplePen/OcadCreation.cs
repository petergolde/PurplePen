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
                map.PrintArea = controller.GetPrintArea(courseView.BaseCourseId);

                if (controller.MapType == MapType.OCAD) {
                    // Set OCAD map as template.
                    // OCAD 6 doesn't support another OCAD file as a template.
                    if (creationSettings.version > 6)
                        map.Template = new TemplateInfo(controller.MapFileName, new PointF(0, 0), 0, 0, true);
                }
                else if (controller.MapType == MapType.Bitmap) {
                    // Set bitmap as template.
                    PointF centerPoint = Util.RectCenter(controller.MapDisplay.MapBounds);
                    map.Template = new TemplateInfo(controller.MapFileName, centerPoint, controller.MapDpi, 0, true);
                }
            }

            InputOutput.WriteFile(outputFilename, map, creationSettings.version);
        }

        // Get the full output file name. Uses the name of the course, removes bad characters,
        // checks for duplication of the map file name. Puts in the directory given in the creationSettings.
        string CreateOutputFileName(Id<Course> courseId)
        {
            string basename;

            // Get the course name.
            if (courseId.IsNone)
                basename = MiscText.AllControls;
            else
                basename = eventDB.GetCourse(courseId).name;

            // Add prefix, if requested.
            if (! string.IsNullOrEmpty(creationSettings.filePrefix)) 
                basename = creationSettings.filePrefix + "-" + basename;

            // Remove bad characters.
            basename = Util.FilterInvalidPathChars(basename);
            basename += ".ocd";      // add OCAD extension.

            return Path.GetFullPath(Path.Combine(creationSettings.outputDirectory, basename));
        }

        // Create a single OCAD file. 
        void CreateFile(Id<Course> courseId)
        {
            // Get the file name of the output.
            string outputFilename = CreateOutputFileName(courseId);

            // Create the course view.
            CourseView courseView = CourseView.CreatePrintingCourseView(eventDB, courseId);

            // Write the OCAD file.
            ExportMap(courseView, outputFilename);
        }

        // Create all the OCAD files according to their creation settings. Throws exception on I/O error.
        // The "mapDirectory" and "fileDirectory" fields are not used -- the "outputDirectory" MUST be
        // set to the directory to use.
        public void CreateOcadFiles()
        {
            foreach (Id<Course> courseId in creationSettings.CourseIds) {
                CreateFile(courseId);
            }
        }

        // Determine if any files will be overwritten. Returns a list of file names that will be overwritten.
        public List<string> OverwrittenFiles()
        {
            List<string> overwrittenFiles = new List<string>();

            foreach (Id<Course> courseId in creationSettings.CourseIds) {
                string outputFilename = CreateOutputFileName(courseId);
                if (File.Exists(outputFilename))
                    overwrittenFiles.Add(outputFilename);
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
