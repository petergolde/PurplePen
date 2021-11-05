/* Copyright (c) 2021, Peter Golde
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
using System.Text;
using System.Linq;
using System.Xml;
using System.Drawing;
using System.IO;
using System.Globalization;

namespace PurplePen
{
    class ExportKml
    {
        private ExportKmlSettings settings;
        private EventDB eventDB;
        private CoordinateMapper coordinateMapper;
        private XmlWriter xmlWriter;
        private int startSeq, finishSeq;

        public ExportKml(EventDB eventDB, ExportKmlSettings settings, CoordinateMapper coordinateMapper)
        {
            this.eventDB = eventDB;
            this.coordinateMapper = coordinateMapper;
            this.settings = settings;

            if (coordinateMapper == null || !coordinateMapper.HasRealWorldCoords || coordinateMapper.MapProjectionType != MapModel.MapProjectionType.Known)
                throw new Exception("The map file must be georeferenced.");
        }

        public void CreateKmlFiles()
        {
            List<Pair<string, IEnumerable<CourseDesignator>>> fileList = GetFilesToCreate();

            foreach (var pair in fileList) {
                CreateKmlFile(pair.First, pair.Second);
            }
        }

        // Get a list of all files that will be overwritten.
        public List<string> OverwrittenFiles()
        {
            return (from filePair in GetFilesToCreate()
                    let fileName = filePair.First
                    where File.Exists(fileName)
                    select fileName).ToList();
        }

        // Create a single KML file with the given course designators in it.
        void CreateKmlFile(string fileName, IEnumerable<CourseDesignator> courseDesignators)
        {
            // Create the XML writer.
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = new UTF8Encoding(false);
            xmlWriter = XmlWriter.Create(fileName, settings);

            // Write the document start.
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("kml");
            xmlWriter.WriteStartElement("Document");

            // Write each course (which becomes a KML folder)
            foreach (CourseDesignator courseDesignator in courseDesignators) {
                WriteKmlCourse(courseDesignator);
            }

            // Finish the document.
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();

            // And done.
            xmlWriter.Close();
            xmlWriter = null;
        }

        // Write one course into the current KML file.
        private void WriteKmlCourse(CourseDesignator courseDesignator)
        {
            bool useCodes;  // if True, name controls with control codes instead of sequences.

            if (courseDesignator.IsNotAllControls) {
                Course course = eventDB.GetCourse(courseDesignator.CourseId);
                useCodes = (course.kind == CourseKind.Score);
            }
            else {
                // All controls -- always use codes.
                useCodes = true;
            }

            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, courseDesignator);
            startSeq = finishSeq = 1; // Number start/finish starting at 1.

            xmlWriter.WriteStartElement("Folder");
            xmlWriter.WriteElementString("name", courseView.CourseFullName);

            // Go through the control views.
            foreach (CourseView.ControlView controlView in courseView.ControlViews) {
                WriteControl(controlView, useCodes);
            }

            xmlWriter.WriteEndElement();  // Folder
        }

        // Write out a single control.
        private void WriteControl(CourseView.ControlView controlView, bool useCodes)
        {
            ControlPoint control = eventDB.GetControl(controlView.controlId);
            ControlPointKind kind = control.kind;
            string controlName;


            switch (kind) {
                case ControlPointKind.Normal:
                    if (useCodes) {
                        controlName = control.code;
                    }
                    else {
                        controlName = controlView.ordinal.ToString();
                    }
                    break;

                case ControlPointKind.Start:
                    controlName = "S" + (startSeq++).ToString();
                    break;

                case ControlPointKind.Finish:
                    controlName = "F" + (finishSeq++).ToString();
                    break;

                default:
                    return;  // Do nothing for crossing points, issue points, map exchange, etc.
            }

            double lat, lng;
            coordinateMapper.GetLatLong(control.location, out lat, out lng);

            xmlWriter.WriteStartElement("Placemark");
            xmlWriter.WriteElementString("name", controlName);
            xmlWriter.WriteStartElement("Point");
            xmlWriter.WriteElementString("coordinates", string.Format(CultureInfo.InvariantCulture, "{0},{1}", lng, lat));
            xmlWriter.WriteEndElement(); // Point
            xmlWriter.WriteEndElement(); // Placemark
        }

        // Get all the files that will be created, plus the courses in each file.
        List<Pair<string, IEnumerable<CourseDesignator>>> GetFilesToCreate()
        {
            List<Pair<string, IEnumerable<CourseDesignator>>> fileList = new List<Pair<string, IEnumerable<CourseDesignator>>>();

            switch (settings.FileCreation) {
                case ExportKmlSettings.KmlFileCreation.SingleFile:
                    // All pages go into a single file.
                    fileList.Add(new Pair<string, IEnumerable<CourseDesignator>>(CreateOutputFileName(null),
                                 QueryEvent.EnumerateCourseDesignators(eventDB, settings.CourseIds, settings.VariationChoicesPerCourse, false)));
                    break;

                case ExportKmlSettings.KmlFileCreation.FilePerCourse:
                    // Create a file for each course part or variation (or both)
                    foreach (CourseDesignator designator in
                             QueryEvent.EnumerateCourseDesignators(eventDB, settings.CourseIds, settings.VariationChoicesPerCourse, false)) {
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
            string basename = QueryEvent.CreateOutputFileName(eventDB, courseDesignator, settings.filePrefix, "", ".kml");

            return Path.GetFullPath(Path.Combine(settings.outputDirectory, basename));
        }
    }


    // All the information needed to print courses.
    class ExportKmlSettings
    {
        public Id<Course>[] CourseIds;          // Courses to print, None is all controls.
        public bool AllCourses = true;          // If true, overrides CourseIds except for all controls.

        public KmlFileCreation FileCreation = KmlFileCreation.FilePerCourse;

        public bool mapDirectory, fileDirectory;     // directory to place output files in
        public string outputDirectory;               // the output directory if mapDirectory and fileDirectoy are false.
        public string filePrefix;                    // if non-null, non-empty, prefix this and "-" onto the front of files.

        // variation choices for courses with variations.
        public Dictionary<Id<Course>, VariationChoices> VariationChoicesPerCourse = new Dictionary<Id<Course>, VariationChoices>();

        public enum KmlFileCreation { SingleFile, FilePerCourse };

        public ExportKmlSettings Clone()
        {
            ExportKmlSettings n = (ExportKmlSettings)base.MemberwiseClone();
            n.CourseIds = (Id<Course>[])n.CourseIds.Clone();
            return n;
        }
    }

}
