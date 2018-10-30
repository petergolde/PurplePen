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
using System.Linq;
using System.Xml;
using System.Diagnostics;
using System.Drawing;

namespace PurplePen
{
    abstract class ExportXmlBase
    {
        protected XmlWriter xmlWriter;
        protected EventDB eventDB;
        protected RectangleF mapBounds;
        protected DateTimeOffset modificationDate;
        protected Dictionary<Id<ControlPoint>, string> controlCodeMap;
        protected CoordinateMapper coordinateMapper;

        public void WriteXml(string filename, EventDB eventDB, RectangleF mapBounds, CoordinateMapper coordinateMapper)
        {
            this.eventDB = eventDB;
            this.mapBounds = mapBounds;
            this.coordinateMapper = coordinateMapper;
            this.modificationDate = DateTimeOffset.Now;
            controlCodeMap = new Dictionary<Id<ControlPoint>, string>();

            // Create the XML writer.
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = new UTF8Encoding(false);
            xmlWriter = XmlWriter.Create(filename, settings);

            WriteStart();

            // Write the start point information
            WriteControls(ControlPointKind.Start, "STA");
            WriteControls(ControlPointKind.MapExchange, "XCHG");

            // Write the control information
            WriteControls(ControlPointKind.Normal, "CTL");

            // Write the end point information
            WriteControls(ControlPointKind.Finish, "FIN");

            // Write the crossint point information
            WriteControls(ControlPointKind.CrossingPoint, "CROSS");

            // Write the course information.
            WriteCourses();

            // Write team assignments for relay courses.
            WriteTeamAssignments();

            WriteEnd();

            // And done.
            xmlWriter.Close();
            eventDB = null;
            xmlWriter = null;
        }

        // Writes information about all the controls in the event of a certain kind.
        // The code prefix is used for controls without a code set (like start/finish)
        void WriteControls(ControlPointKind controlKind, string codePrefix)
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

                    WriteControlPoint(controlKind, control, code);
                }
            }
        }

        // Get all the class names associated with this course.
        protected string[] GetClassNames(EventDB eventDB, Id<Course> courseId)
        {
            Course course = eventDB.GetCourse(courseId);
            string secondaryTitle = course.secondaryTitle;

            if (!string.IsNullOrEmpty(secondaryTitle)) {
                // Assumed that classes are separated with commas.
                return (from s in secondaryTitle.Split(new char[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries) select s.Trim()).ToArray();
            }
            else {
                return new string[0];
            }
        }
        void WriteCourses()
        {
            // Sport Software requires that the courses be numbers started at 0.
            int courseNumber = 0;
            foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB)) {
                if (WriteCourse(courseId, courseNumber))
                    ++courseNumber;
            }
        }

        void WriteTeamAssignments()
        {
            foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB)) {
                if (QueryEvent.HasVariations(eventDB, courseId)) {
                    Course course = eventDB.GetCourse(courseId);
                    if (course.relaySettings.relayTeams > 0) {
                        RelayVariations relayVariations = new RelayVariations(eventDB, courseId, course.relaySettings);
                        WriteRelayVariations(courseId, relayVariations);
                    }
                }
            }
        }

        bool WriteCourse(Id<Course> courseId, int courseNumber)
        {
            // A course must have a start and a finish to be output.
            if (!QueryEvent.HasStartControl(eventDB, courseId))
                return false;
            if (!QueryEvent.HasFinishControl(eventDB, courseId))
                return false;

            Course course = eventDB.GetCourse(courseId);
            bool isScore = (course.kind == CourseKind.Score);
            string[] classNames = GetClassNames(eventDB, courseId);
            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(courseId));

            WriteCourseGroupStart(course.name, courseNumber, classNames, isScore);

            WriteCourseVariations(courseId, course.name, courseNumber, classNames, isScore);

            WriteCourseGroupEnd();

            return true;
        }

        void WriteCourseVariations(Id<Course> courseId, string courseName, int courseNumber, string[] classNames, bool isScore)
        {
            if (QueryEvent.HasVariations(eventDB, courseId)) {
                VariationInfo[] variations = QueryEvent.GetAllVariations(eventDB, courseId).ToArray();
                for (int variationNumber = 0; variationNumber < variations.Length; ++variationNumber) {
                    WriteSingleCourseVariation(new CourseDesignator(courseId, variations[variationNumber]), courseName, courseNumber, classNames, isScore, variationNumber, variations[variationNumber]);
                }
            }
            else {
                // No variations.
                WriteSingleCourseVariation(new CourseDesignator(courseId), courseName, courseNumber, classNames, isScore, 0, null);
            }
        }

        void WriteSingleCourseVariation(CourseDesignator courseDesignator, string courseName, int courseNumber, string[] classNames, bool isScore, int variationNumber, VariationInfo variationInfo)
        {
            float distanceThisLeg = 0;
            int sequenceNumber = 1;     // score courses need sequence #'s, even though there is no sequence.

            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, courseDesignator);

            WriteCourseStart(courseView, courseName, courseNumber, classNames, isScore, variationNumber, variationInfo);

            // Go through the control views.
            int controlViewIndex = 0;
            while (controlViewIndex >= 0 && controlViewIndex < courseView.ControlViews.Count) {
                CourseView.ControlView controlView = courseView.ControlViews[controlViewIndex];
                ControlPointKind kind = eventDB.GetControl(controlView.controlId).kind;

                WriteCourseControl(kind, controlView, isScore, ref sequenceNumber, ref distanceThisLeg);

                if (controlView.legLength != null)
                    distanceThisLeg += controlView.legLength[0];

                if (isScore)
                    ++controlViewIndex;
                else
                    controlViewIndex = courseView.GetNextControl(controlViewIndex);
            }

            WriteCourseEnd();
        }



        protected abstract void WriteStart();

        protected abstract void WriteControlPoint(ControlPointKind controlKind, ControlPoint control, string code);

        protected abstract void WriteCourseGroupStart(string courseName, int courseNumber, string[] classNames, bool isScore);

        protected abstract void WriteCourseStart(CourseView courseView, string courseName, int courseNumber, string[] classNames, bool isScore, int variationNumber, VariationInfo variationInfo);

        protected abstract void WriteCourseControl(ControlPointKind kind, CourseView.ControlView controlView, bool isScore, ref int sequenceNumber, ref float distanceThisLeg);

        protected abstract void WriteCourseEnd();

        protected abstract void WriteCourseGroupEnd();

        protected abstract void WriteRelayVariations(Id<Course> courseId, RelayVariations relayVariations);


        protected abstract void WriteEnd();
    }

    class ExportXmlVersion3: ExportXmlBase
    {
        protected override void WriteStart()
        {
            // Write the root
            xmlWriter.WriteStartElement("CourseData", "http://www.orienteering.org/datastandard/3.0");
            xmlWriter.WriteAttributeString("xmlns", "http://www.orienteering.org/datastandard/3.0");
            xmlWriter.WriteAttributeString("iofVersion", "3.0");
            xmlWriter.WriteAttributeString("createTime", XmlConvert.ToString(modificationDate));
            xmlWriter.WriteAttributeString("creator", string.Format("Purple Pen version {0}", Util.PrettyVersionString(VersionNumber.Current)));

            WriteEventInfo();

            xmlWriter.WriteStartElement("RaceCourseData");

            WriteMapInfo();
        }

        protected override void WriteEnd()
        {
            xmlWriter.WriteEndElement();  // RaceCourseData
            xmlWriter.WriteEndElement();  // CourseData
        }


        void WriteEventInfo(){
            xmlWriter.WriteStartElement("Event");
            xmlWriter.WriteElementString("Name", eventDB.GetEvent().title);
            xmlWriter.WriteEndElement();
        }

        // Write the "Map" element and its information.
        void WriteMapInfo()
        {
            xmlWriter.WriteStartElement("Map");

            xmlWriter.WriteElementString("Scale", XmlConvert.ToString(eventDB.GetEvent().mapScale));

            xmlWriter.WriteStartElement("MapPositionTopLeft");
            xmlWriter.WriteAttributeString("x", XmlConvert.ToString(Math.Round(mapBounds.Left, 2)));
            xmlWriter.WriteAttributeString("y", XmlConvert.ToString(Math.Round(mapBounds.Bottom, 2)));
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MapPositionBottomRight");
            xmlWriter.WriteAttributeString("x", XmlConvert.ToString(Math.Round(mapBounds.Right, 2)));
            xmlWriter.WriteAttributeString("y", XmlConvert.ToString(Math.Round(mapBounds.Top, 2)));
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();
        }

        protected override void WriteControlPoint(ControlPointKind controlKind, ControlPoint control, string code)
        {
            string controlType;

            switch (controlKind) {
                case ControlPointKind.Normal:
                    controlType = "Control"; break;
                case ControlPointKind.Start:
                case ControlPointKind.MapExchange:
                    controlType = "Start"; break;
                case ControlPointKind.Finish:
                    controlType = "Finish"; break;
                case ControlPointKind.CrossingPoint:
                    controlType = "CrossingPoint"; break;
                default:
                    return;
            }

            // Write the XML.
            xmlWriter.WriteStartElement("Control");
            xmlWriter.WriteAttributeString("type", controlType);
            xmlWriter.WriteElementString("Id", code);

            if (coordinateMapper != null && coordinateMapper.HasRealWorldCoords && coordinateMapper.MapProjectionType == MapModel.MapProjectionType.Known) {
                double lat, lng;
                coordinateMapper.GetLatLong(control.location, out lat, out lng);
                xmlWriter.WriteStartElement("Position");
                xmlWriter.WriteAttributeString("lng", XmlConvert.ToString(lng));
                xmlWriter.WriteAttributeString("lat", XmlConvert.ToString(lat));
                xmlWriter.WriteEndElement();
            }


            xmlWriter.WriteStartElement("MapPosition");
            xmlWriter.WriteAttributeString("x", XmlConvert.ToString(Math.Round(control.location.X, 2)));
            xmlWriter.WriteAttributeString("y", XmlConvert.ToString(Math.Round(control.location.Y, 2)));
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();
        }

        protected override void WriteCourseGroupStart(string courseName, int courseNumber, string[] classNames, bool isScore)
        {
            // Everything is done in WriteCourseStart. Nothing to do.
        }

        protected override void WriteCourseStart(CourseView courseView, string courseName, int courseNumber, string[] classNames, bool isScore, int variationNumber, VariationInfo variationInfo)
        {
            xmlWriter.WriteStartElement("Course");

            if (variationInfo != null) {
                xmlWriter.WriteElementString("Name", courseName + " " + variationInfo.CodeString);
                xmlWriter.WriteElementString("CourseFamily", courseName);
            }
            else {
                xmlWriter.WriteElementString("Name", courseName);
            }

            if (!isScore) {
                xmlWriter.WriteElementString("Length", XmlConvert.ToString(Math.Round(courseView.MaxTotalLength / 100F) * 100F));   // round to nearest 100m
                if (courseView.TotalClimb > 0)
                    xmlWriter.WriteElementString("Climb", XmlConvert.ToString(Math.Round(courseView.TotalClimb / 5, MidpointRounding.AwayFromZero) * 5.0));  // round to nearest 5m
            }
        }


        protected override void WriteCourseControl(ControlPointKind kind, CourseView.ControlView controlView, bool isScore, ref int sequenceNumber, ref float distanceThisLeg)
        {
            if (kind == ControlPointKind.MapIssue)
                return;

            xmlWriter.WriteStartElement("CourseControl");

            switch (kind) {
                case ControlPointKind.Start:
                    xmlWriter.WriteAttributeString("type", "Start");
                    break;

                case ControlPointKind.Finish:
                    // UNDONE: handle case based on flagging of the leg.
                    xmlWriter.WriteAttributeString("type", "Finish");
                    if (eventDB.GetControl(controlView.controlId).symbolIds?[0] == "14.1")
                        xmlWriter.WriteAttributeString("specialInstruction", "TapedRoute");
                    else if(eventDB.GetControl(controlView.controlId).symbolIds?[0] == "14.2")
                        xmlWriter.WriteAttributeString("specialInstruction", "FunnelTapedRoute");

                    break;

                case ControlPointKind.MapExchange:
                    xmlWriter.WriteAttributeString("type", "Start");
                    break;

                case ControlPointKind.Normal:
                    xmlWriter.WriteAttributeString("type", "Control");
                    break;

                case ControlPointKind.CrossingPoint:
                    xmlWriter.WriteAttributeString("type", "CrossingPoint");
                    if (eventDB.GetControl(controlView.controlId).symbolIds?[0] == "13.4")
                        xmlWriter.WriteAttributeString("specialInstruction", "MandatoryOutOfBoundsAreaPassage");
                    else
                        xmlWriter.WriteAttributeString("specialInstruction", "MandatoryCrossingPoint");
                    break;
            }

            xmlWriter.WriteElementString("Control", controlCodeMap[controlView.controlId]);

            if (!isScore && controlView.ordinal > 0) {
                xmlWriter.WriteElementString("MapText", XmlConvert.ToString(controlView.ordinal));
            }
            if (!isScore && kind != ControlPointKind.Start) {
                xmlWriter.WriteElementString("LegLength", XmlConvert.ToString(Math.Round(distanceThisLeg)));
                distanceThisLeg = 0;
            }
            if (isScore && kind == ControlPointKind.Normal) {
                int points = eventDB.GetCourseControl(controlView.courseControlIds[0]).points;
                if (points > 0)
                    xmlWriter.WriteElementString("Score", XmlConvert.ToString(points));
            }

            xmlWriter.WriteEndElement();  // "CourseControl"
        }

        protected override void WriteRelayVariations(Id<Course> courseId, RelayVariations relayVariations)
        {
            ExportRelayVariations3 exportVariations = new ExportRelayVariations3();
            exportVariations.WriteTeamsPart(xmlWriter, relayVariations, eventDB, courseId);
        }

        protected override void WriteCourseEnd()
        {
            xmlWriter.WriteEndElement();     // "Course"

        }

        protected override void WriteCourseGroupEnd()
        {
            // Nothing to do.
        }

        // Return an exception map used to test exported XML files.
        public static Dictionary<string, string> TestFileExceptionMap() {
            Dictionary<string, string> exceptions = new Dictionary<string, string>();
            exceptions[@"modifyTime=""\d\d\d\d-\d\d-\d\dT\d\d:\d\d:\d\d\.\d\d\d\d\d?\d?\d?-\d\d:00"""] = @"modifyTime=""\d\d\d\d-\d\d-\d\dT\d\d:\d\d:\d\d\.\d\d\d\d\d?\d?\d?-\d\d:00""";
            exceptions[@"createTime=""\d\d\d\d-\d\d-\d\dT\d\d:\d\d:\d\d\.\d\d\d\d\d?\d?\d?-\d\d:00"""] = @"createTime=""\d\d\d\d-\d\d-\d\dT\d\d:\d\d:\d\d\.\d\d\d\d\d?\d?\d?-\d\d:00""";
            return exceptions;
        }

    }


































    class ExportXmlVersion2: ExportXmlBase
    {
        protected override void WriteStart()
        {
            // Write the root
            xmlWriter.WriteStartElement("CourseData");

            // Write the version.
            xmlWriter.WriteStartElement("IOFVersion");
            xmlWriter.WriteAttributeString("version", "2.0.3");
            xmlWriter.WriteEndElement();

            WriteModificationDate(modificationDate);

            WriteMapInfo();
        }

        protected override void WriteEnd()
        {
            xmlWriter.WriteEndElement();
        }


        void WriteModificationDate(DateTimeOffset dateTime)
        {
            xmlWriter.WriteStartElement("ModifyDate");

            // Date
            xmlWriter.WriteStartElement("Date");
            xmlWriter.WriteString(dateTime.ToString("yyyy-MM-dd"));
            xmlWriter.WriteEndElement();

            // Time
            xmlWriter.WriteStartElement("Clock");
            xmlWriter.WriteString(dateTime.ToString("HH:mm"));
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();
        }

        // Write the "Map" element and its information.
        void WriteMapInfo()
        {
            xmlWriter.WriteStartElement("Map");

            xmlWriter.WriteElementString("Scale", XmlConvert.ToString(eventDB.GetEvent().mapScale));

            xmlWriter.WriteStartElement("MapPosition");
            xmlWriter.WriteAttributeString("x", XmlConvert.ToString(Math.Round(mapBounds.Left, 2)));
            xmlWriter.WriteAttributeString("y", XmlConvert.ToString(Math.Round(mapBounds.Bottom, 2)));
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();
        }

        protected override void WriteControlPoint(ControlPointKind controlKind, ControlPoint control, string code)
        {
            string elementName;

            switch (controlKind) {
                case ControlPointKind.Start:
                    elementName = "StartPoint"; break;
                case ControlPointKind.Normal:
                    elementName = "Control"; break;
                case ControlPointKind.Finish:
                    elementName = "FinishPoint"; break;

                case ControlPointKind.MapExchange:  // skip map exchanges.
                default:
                    return;
            }

            // Write the XML.
            xmlWriter.WriteStartElement(elementName);
            xmlWriter.WriteElementString(elementName + "Code", code);

            if (coordinateMapper != null && coordinateMapper.HasRealWorldCoords) {
                double realX, realY;
                coordinateMapper.GetRealWorld(control.location, out realX, out realY);
                xmlWriter.WriteStartElement("ControlPosition");
                xmlWriter.WriteAttributeString("x", XmlConvert.ToString(Math.Round(realX)));
                xmlWriter.WriteAttributeString("y", XmlConvert.ToString(Math.Round(realY)));
                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteStartElement("MapPosition");
            xmlWriter.WriteAttributeString("x", XmlConvert.ToString(Math.Round(control.location.X, 2)));
            xmlWriter.WriteAttributeString("y", XmlConvert.ToString(Math.Round(control.location.Y, 2)));
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndElement();
        }

        protected override void WriteCourseGroupStart(string courseName, int courseNumber, string[] classNames, bool isScore)
        {
            xmlWriter.WriteStartElement("Course");
            xmlWriter.WriteElementString("CourseName", courseName);
            xmlWriter.WriteElementString("CourseId", XmlConvert.ToString(courseNumber));

            foreach (string className in classNames) {
                xmlWriter.WriteElementString("ClassShortName", className);
            }
        }

        protected override void WriteCourseStart(CourseView courseView, string courseName, int courseNumber, string[] classNames, bool isScore, int variationNumber, VariationInfo variationInfo)
        {
            xmlWriter.WriteStartElement("CourseVariation");
            xmlWriter.WriteElementString("CourseVariationId", XmlConvert.ToString(variationNumber));

            if (variationInfo != null)
                xmlWriter.WriteElementString("Name", variationInfo.CodeString);

            if (!isScore) {
                xmlWriter.WriteElementString("CourseLength", XmlConvert.ToString(Math.Round(courseView.MaxTotalLength / 100F) * 100F));   // round to nearest 100m
                if (courseView.TotalClimb > 0)
                    xmlWriter.WriteElementString("CourseClimb", XmlConvert.ToString(Math.Round(courseView.TotalClimb / 5, MidpointRounding.AwayFromZero) * 5.0));  // round to nearest 5m
            }
        }


        protected override void WriteCourseControl(ControlPointKind kind, CourseView.ControlView controlView, bool isScore, ref int sequenceNumber, ref float distanceThisLeg)
        {
            switch (kind) {
                case ControlPointKind.Start:
                    xmlWriter.WriteElementString("StartPointCode", controlCodeMap[controlView.controlId]);
                    break;

                case ControlPointKind.Finish:
                    xmlWriter.WriteElementString("FinishPointCode", controlCodeMap[controlView.controlId]);
                    if (!isScore) {
                        xmlWriter.WriteElementString("DistanceToFinish", XmlConvert.ToString(Math.Round(distanceThisLeg)));
                        distanceThisLeg = 0;
                    }
                    break;

                case ControlPointKind.Normal:
                    xmlWriter.WriteStartElement("CourseControl");

                    // With map exchanges, the sequence can be different than the ordinals. We always use the sequence.
                    xmlWriter.WriteElementString("Sequence", XmlConvert.ToString(sequenceNumber++));

                    xmlWriter.WriteElementString("ControlCode", controlCodeMap[controlView.controlId]);

                    if (!isScore) {
                        xmlWriter.WriteElementString("LegLength", XmlConvert.ToString(Math.Round(distanceThisLeg)));
                        distanceThisLeg = 0;
                    }
                    if (isScore) {
                        int points = eventDB.GetCourseControl(controlView.courseControlIds[0]).points;
                        if (points > 0)
                            xmlWriter.WriteElementString("ScoreOPoints", XmlConvert.ToString(points));
                    }

                    xmlWriter.WriteEndElement();         // "CourseControl"
                    break;

                case ControlPointKind.MapExchange:
                    // Intentionally skip map exchanges.
                case ControlPointKind.CrossingPoint:
                    // Intentionally skip crossing points.
                    break;
            }
        }

        protected override void WriteRelayVariations(Id<Course> courseId, RelayVariations relayVariations)
        {
            // Version 2 does not have ability to write relay variations. Do nothing.
        }

        protected override void WriteCourseEnd()
        {
            xmlWriter.WriteEndElement();     // "CourseVariation"
        }

        protected override void WriteCourseGroupEnd()
        {
            xmlWriter.WriteEndElement();     // "Course"
        }

        // Return an exception map used to test exported XML files.
        public static Dictionary<string, string> TestFileExceptionMap()
        {
            Dictionary<string, string> exceptions = new Dictionary<string, string>();
            exceptions[@"^    <Date>\d\d\d\d-\d\d-\d\d</Date>$"] = @"^    <Date>\d\d\d\d-\d\d-\d\d</Date>$";
            exceptions[@"^    <Clock>\d\d\:\d\d</Clock>$"] = @"^    <Clock>\d\d\:\d\d</Clock>$";
            return exceptions;
        }

    }


    





    class ExportRelayVariations3
    {
        private XmlWriter xmlWriter;
        private DateTimeOffset modificationDate;
        private RelayVariations relayVariations;
        private EventDB eventDB;
        private Id<Course> courseId;
        private string courseName;

        public void WriteFullXml(string filename, RelayVariations relayVariations, EventDB eventDB, Id<Course> courseId)
        {
            this.relayVariations = relayVariations;
            this.eventDB = eventDB;
            this.courseId = courseId;
            this.modificationDate = DateTimeOffset.Now;
            this.courseName = eventDB.GetCourse(courseId).name;

            // Create the XML writer.
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = new UTF8Encoding(false);
            xmlWriter = XmlWriter.Create(filename, settings);

            WriteStart();

            WriteAllTeams();

            WriteEnd();

            // And done.
            xmlWriter.Close();
            eventDB = null;
            xmlWriter = null;
        }

        public void WriteTeamsPart(XmlWriter xmlWriter, RelayVariations relayVariations, EventDB eventDB, Id<Course> courseId)
        {
            this.xmlWriter = xmlWriter;
            this.relayVariations = relayVariations;
            this.eventDB = eventDB;
            this.courseId = courseId;
            this.modificationDate = DateTimeOffset.Now;
            this.courseName = eventDB.GetCourse(courseId).name;

            WriteAllTeams();
        }


        private void WriteStart()
        {
            // Write the root
            xmlWriter.WriteStartElement("CourseData", "http://www.orienteering.org/datastandard/3.0");
            xmlWriter.WriteAttributeString("xmlns", "http://www.orienteering.org/datastandard/3.0");
            xmlWriter.WriteAttributeString("iofVersion", "3.0");
            xmlWriter.WriteAttributeString("createTime", XmlConvert.ToString(modificationDate));
            xmlWriter.WriteAttributeString("creator", string.Format("Purple Pen version {0}", Util.PrettyVersionString(VersionNumber.Current)));

            WriteEventInfo();

            xmlWriter.WriteStartElement("RaceCourseData");
        }

        private void WriteEnd()
        {
            xmlWriter.WriteEndElement();  // RaceCourseData
            xmlWriter.WriteEndElement();  // CourseData
        }

        void WriteEventInfo()
        {
            xmlWriter.WriteStartElement("Event");
            xmlWriter.WriteElementString("Name", eventDB.GetEvent().title);
            xmlWriter.WriteEndElement();
        }

        private void WriteAllTeams()
        {
            for (int teamNumber = 1; teamNumber < relayVariations.NumberOfTeams; ++teamNumber) {
                WriteTeam(teamNumber);
            }
        }

        private void WriteTeam(int teamNumber)
        {
            xmlWriter.WriteStartElement("TeamCourseAssignment");
            xmlWriter.WriteElementString("BibNumber", XmlConvert.ToString(teamNumber));
            xmlWriter.WriteElementString("TeamName", XmlConvert.ToString(teamNumber));

            for (int legNumber = 1; legNumber <= relayVariations.NumberOfLegs; ++legNumber) {
                WriteLeg(teamNumber, legNumber);
            }

            xmlWriter.WriteEndElement(); // </TeamCourseAssignment>
        }

        private void WriteLeg(int teamNumber, int legNumber)
        {
            xmlWriter.WriteStartElement("TeamMemberCourseAssignment");

            string variationString = relayVariations.GetVariation(teamNumber, legNumber).CodeString;

            xmlWriter.WriteElementString("Leg", XmlConvert.ToString(legNumber));
            xmlWriter.WriteElementString("CourseName", courseName + " " + variationString);
            xmlWriter.WriteElementString("CourseFamily", courseName);

            xmlWriter.WriteEndElement(); // </TeamMemberCourseAssignment>
        }
    }

    public enum VariationExportFileType { Xml, Csv };

}
