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
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Drawing;

namespace PurplePen
{
    class ExportXml
    {
        private XmlWriter xmlWriter;
        private EventDB eventDB;
        private RectangleF mapBounds;
        private Dictionary<Id<ControlPoint>, string> controlCodeMap;

        // Write the event out as a course data IOF XML format file to the given file.
        public void WriteXml(string filename, EventDB eventDB, RectangleF mapBounds) 
        {
            this.eventDB = eventDB;
            this.mapBounds = mapBounds;
            controlCodeMap = new Dictionary<Id<ControlPoint>, string>();

            // Create the XML writer.
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            xmlWriter = XmlWriter.Create(filename, settings);

            // Write the root
            xmlWriter.WriteStartElement("CourseData");

            // Write the version.
            xmlWriter.WriteStartElement("IOFVersion");
            xmlWriter.WriteAttributeString("version", "2.0.3");
            xmlWriter.WriteEndElement();

            // Write the map information.
            WriteMapInfo();

            // Write the start point information
            WriteControls(ControlPointKind.Start, "StartPoint", "STA");

            // Write the control information
            WriteControls(ControlPointKind.Normal, "Control", "CTL");

            // Write the end point information
            WriteControls(ControlPointKind.Finish, "FinishPoint", "FIN");

            // Write the course information.
            WriteCourses();

            // And done.
            xmlWriter.WriteEndElement();
            xmlWriter.Close();
            eventDB = null;
            xmlWriter = null;
        }

        // Write the "Map" element and its information.
        private void WriteMapInfo()
        {
            xmlWriter.WriteStartElement("Map");

            xmlWriter.WriteElementString("Scale", XmlConvert.ToString(eventDB.GetEvent().mapScale));

            xmlWriter.WriteStartElement("MapPosition");
            xmlWriter.WriteAttributeString("x", Convert.ToString(Math.Round(mapBounds.Left, 2)));
            xmlWriter.WriteAttributeString("y", Convert.ToString(Math.Round(mapBounds.Bottom, 2)));
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();
        }

        // Writes information about all the controls in the event of a certain kind.
        // The code prefix is used for controls without a code set (like start/finish)
        private void WriteControls(ControlPointKind controlKind, string elementName, string codePrefix)
        {
            int emptyCodeSuffix = 1;

            foreach (Id<ControlPoint> controlId in eventDB.AllControlPointIds) {
                ControlPoint control = eventDB.GetControl(controlId);
                if (control.kind == controlKind) {
                    // Get the code. Synthesize a unique code if necessary.
                    string code = control.code;
                    if (string.IsNullOrEmpty(code))
                        code = codePrefix + (emptyCodeSuffix++).ToString();

                    // Record the controlid->code mapping for later use.
                    controlCodeMap[controlId] = code;

                    // Write the XML.
                    xmlWriter.WriteStartElement(elementName);
                    xmlWriter.WriteElementString(elementName + "Code", code);
                    xmlWriter.WriteStartElement("MapPosition");
                    xmlWriter.WriteAttributeString("x", Convert.ToString(Math.Round(control.location.X, 2)));
                    xmlWriter.WriteAttributeString("y", Convert.ToString(Math.Round(control.location.Y, 2)));
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndElement();
                }
            }
        }

        private void WriteCourses()
        {
            foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB)) {
                WriteCourse(courseId);
            }
        }

        private void WriteCourse(Id<Course> courseId)
        {
            // A course must have a start and a finish to be output.
            if (!QueryEvent.HasStartControl(eventDB, courseId))
                return;
            if (!QueryEvent.HasFinishControl(eventDB, courseId))
                return;

            Course course = eventDB.GetCourse(courseId);
            bool isScore = (course.kind == CourseKind.Score);
            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(courseId));
            float distanceThisLeg = 0;
            int scoreSequence = 1;     // score courses need sequence #'s, even though there is no sequence.

            xmlWriter.WriteStartElement("Course");
            xmlWriter.WriteElementString("CourseName", course.name);
            xmlWriter.WriteElementString("CourseId", XmlConvert.ToString(courseId.id));
            xmlWriter.WriteStartElement("CourseVariation");
            xmlWriter.WriteElementString("CourseVariationId", XmlConvert.ToString(0));
            if (!isScore) {
                xmlWriter.WriteElementString("CourseLength", XmlConvert.ToString(Math.Round(courseView.TotalLength / 100F) * 100F));   // round to nearest 100m
                if (courseView.TotalClimb > 0)
                    xmlWriter.WriteElementString("CourseClimb", XmlConvert.ToString(Math.Round(courseView.TotalClimb / 5, MidpointRounding.AwayFromZero) * 5.0));  // round to nearest 5m
            }

            // Go through the control views.
            int controlViewIndex = 0;
            while (controlViewIndex >= 0 && controlViewIndex < courseView.ControlViews.Count) {
                CourseView.ControlView controlView = courseView.ControlViews[controlViewIndex];
                ControlPointKind kind = eventDB.GetControl(controlView.controlId).kind;

                switch (kind) {
                case ControlPointKind.Start:
                    xmlWriter.WriteElementString("StartPointCode", controlCodeMap[controlView.controlId]);
                    break;

                case ControlPointKind.Finish:
                    xmlWriter.WriteElementString("FinishPointCode", controlCodeMap[controlView.controlId]);
                    if (!isScore)
                        xmlWriter.WriteElementString("DistanceToFinish", XmlConvert.ToString(Math.Round(distanceThisLeg)));
                    distanceThisLeg = 0;
                    break;

                case ControlPointKind.MapExchange:
                    Debug.Fail("UNDONE MAPEXCHANGE");
                    break;

                case ControlPointKind.Normal:
                    xmlWriter.WriteStartElement("CourseControl");

                    if (!isScore)
                        xmlWriter.WriteElementString("Sequence", XmlConvert.ToString(controlView.ordinal));
                    else
                        xmlWriter.WriteElementString("Sequence", XmlConvert.ToString(scoreSequence++));

                    xmlWriter.WriteElementString("ControlCode", controlCodeMap[controlView.controlId]);

                    if (!isScore) {
                        xmlWriter.WriteElementString("LegLength", XmlConvert.ToString(Math.Round(distanceThisLeg)));
                        distanceThisLeg = 0;
                    }
                    if (isScore) {
                        int points = eventDB.GetCourseControl(controlView.courseControlId).points;
                        if (points > 0)
                            xmlWriter.WriteElementString("ScoreOPoints", XmlConvert.ToString(points));
                    }
                        
                    xmlWriter.WriteEndElement();         // "CourseControl"
                    break;

                // Intentionally skip crossing points.
                }


                if (controlView.legLength != null)
                    distanceThisLeg += controlView.legLength[0];

                if (isScore)
                    ++controlViewIndex;
                else
                    controlViewIndex = courseView.GetNextControl(controlViewIndex);
            }

            xmlWriter.WriteEndElement();     // "CourseVariation"
            xmlWriter.WriteEndElement();     // "Course"
        }

    }
}
