using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml;

using PurplePen.MapModel;
using DotSpatial.Projections;
using System.Drawing.Drawing2D;
using System.IO;

namespace PurplePen
{
    class GpxFile
    {
        private EventDB eventDB;
        private GpxCreationSettings settings;
        private CoordinateMapper coordinateMapper;
        private XmlWriter xmlWriter;

        // All the waypoints we are writing.
        private Dictionary<Id<ControlPoint>, Waypoint> waypoints = new Dictionary<Id<ControlPoint>, Waypoint>();
        List<Waypoint> waypointList;

        private const string gpxNamespace = "http://www.topografix.com/GPX/1/1";
        private const string xsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";
        private const string gpxSchema = "http://www.topografix.com/GPX/1/1/gpx.xsd";

        public GpxFile(EventDB eventDB, CoordinateMapper coordinateMapper, GpxCreationSettings settings)
        {
            this.eventDB = eventDB;
            this.settings = settings;
            this.coordinateMapper = coordinateMapper;

            if (coordinateMapper == null)
                throw new Exception("The map file must be an OCAD file to use GPX files.");
        }

        public void WriteGpx(string fileName)
        {
            CollectWaypoints();
            WriteGpxXml(fileName);
        }

        private void CollectWaypoints()
        {
            waypointList = new List<Waypoint>();

            CollectWaypointsOfKind(waypointList, ControlPointKind.Start, "STA", WaypointKind.Start);
            CollectWaypointsOfKind(waypointList, ControlPointKind.MapExchange, "XCHG", WaypointKind.Exchange);
            CollectWaypointsOfKind(waypointList, ControlPointKind.Normal, "CTL", WaypointKind.Control);
            CollectWaypointsOfKind(waypointList, ControlPointKind.CrossingPoint, "CROS", WaypointKind.Crossing);
            CollectWaypointsOfKind(waypointList, ControlPointKind.Finish, "FIN", WaypointKind.Finish);
        }

        // Collect waypoints for all the controls of a certain kind.
        // The code prefix is used for controls without a code set (like start/finish)
        private void CollectWaypointsOfKind(List<Waypoint> waypointList, ControlPointKind controlKind, string noCodeName, WaypointKind kind)
        {
            int noCodeSuffix = 1;

            foreach (Id<ControlPoint> controlId in eventDB.AllControlPointIds) {
                ControlPoint control = eventDB.GetControl(controlId);
                if (IncludeControl(controlId) && control.kind == controlKind) {
                    // Get the name.
                    string name = control.code;
                    if (string.IsNullOrEmpty(name))
                        name = noCodeName + (noCodeSuffix++).ToString();
                    name = settings.CodePrefix + name;

                    // Get the latitude and longitude.
                    double latitude, longitude;
                    if (!coordinateMapper.GetLatLong(control.location, out latitude, out longitude) ||
                        double.IsNaN(latitude) || double.IsNaN(longitude)) 
                    {
                        throw new Exception(MiscText.GpxReprojectFailure);
                    }
                    // Create a waypoint.
                    Waypoint wp = new Waypoint() { Name = name, Kind = kind, Latitude = latitude, Longitude = longitude };
                    waypointList.Add(wp);
                    waypoints[controlId] = wp;
                }
            }
        }

        // Determine if we should include this control as a waypoint. We check if "all controls" was selected, or if the
        // control is in any selected course.
        private bool IncludeControl(Id<ControlPoint> controlId)
        {
            if (settings.CourseIds.Contains(Id<Course>.None))
                return true;

            foreach (Id<Course> courseId in settings.CourseIds) {
                if (courseId.IsNotNone) {
                    if (QueryEvent.CourseUsesControl(eventDB, new CourseDesignator(courseId), controlId))
                        return true;
                }
            }

            return false;
        }


        private void WriteGpxXml(string fileName)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = new UTF8Encoding(false);
            using (xmlWriter = XmlWriter.Create(fileName, settings)) {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("gpx", gpxNamespace);
                xmlWriter.WriteAttributeString("creator", "Purple Pen " + Util.PrettyVersionString(VersionNumber.Current));
                xmlWriter.WriteAttributeString("version", "1.1");
                xmlWriter.WriteAttributeString("xsi", "schemaLocation", xsiNamespace, gpxNamespace + " " + gpxSchema);

                WriteMetadata(Path.GetFileName(fileName));

                WriteWaypoints();
                WriteCourseTracks();

                xmlWriter.WriteEndElement();
                xmlWriter.Close();
            }

        }

        private void WriteMetadata(string name)
        {
            xmlWriter.WriteStartElement("metadata", gpxNamespace);
            xmlWriter.WriteElementString("name", gpxNamespace, name);
            xmlWriter.WriteElementString("desc", gpxNamespace, eventDB.GetEvent().title);

            xmlWriter.WriteStartElement("link", gpxNamespace);
            xmlWriter.WriteAttributeString("href", "http://purple-pen.org");
            xmlWriter.WriteElementString("text", gpxNamespace, "Purple Pen");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteElementString("time", gpxNamespace, DateTime.UtcNow.ToString("s") + "Z");

            // Write bounds.
            if (waypointList.Count > 0) {
                double minLat, minLon, maxLat, maxLon;
                GetWaypointBounds(out minLat, out minLon, out maxLat, out maxLon);
                xmlWriter.WriteStartElement("bounds", gpxNamespace);
                xmlWriter.WriteAttributeString("minlat", XmlConvert.ToString(minLat));
                xmlWriter.WriteAttributeString("minlon", XmlConvert.ToString(minLon));
                xmlWriter.WriteAttributeString("maxlat", XmlConvert.ToString(maxLat));
                xmlWriter.WriteAttributeString("maxlon", XmlConvert.ToString(maxLon));
                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndElement();
        }

        private void GetWaypointBounds(out double minLat, out double minLon, out double maxLat, out double maxLon)
        {
            minLat = minLon = double.MaxValue;
            maxLat = maxLon = double.MinValue;

            foreach (var wpt in waypointList) {
                if (wpt.Latitude < minLat) minLat = wpt.Latitude;
                if (wpt.Latitude > maxLat) maxLat = wpt.Latitude;
                if (wpt.Longitude < minLon) minLon = wpt.Longitude;
                if (wpt.Longitude > maxLon) maxLon = wpt.Longitude;
            }
        }

        private void WriteWaypoints()
        {
            // Write waypoints for all controls (or at least the one on requested courses.
            foreach (Waypoint wp in waypointList)
                WriteWaypoint(wp);
        }

        private void WriteWaypoint(Waypoint waypoint)
        {
            string symText;
            switch (waypoint.Kind) {
                case WaypointKind.Start: symText = "Navaid, Green"; break;
                case WaypointKind.Finish: symText = "Navaid, Red"; break;
                case WaypointKind.Exchange: symText = "Navaid, Blue"; break;
                case WaypointKind.Crossing: symText = "Navaid, Orange"; break;
                default: symText = "Navaid, Violet"; break; // normal controls 
            }

            xmlWriter.WriteStartElement("wpt", gpxNamespace);
            xmlWriter.WriteAttributeString("lat", XmlConvert.ToString(waypoint.Latitude));
            xmlWriter.WriteAttributeString("lon", XmlConvert.ToString(waypoint.Longitude));
            xmlWriter.WriteElementString("name", gpxNamespace, waypoint.Name);
            xmlWriter.WriteElementString("sym", gpxNamespace, symText);
            xmlWriter.WriteEndElement();
        }

        private void WriteCourseTracks()
        {
            foreach (Id<Course> courseId in settings.CourseIds) {
                if (courseId.IsNotNone)
                    WriteCourseTrack(courseId);
            }
        }

        private void WriteCourseTrack(Id<Course> courseId)
        {
            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(courseId));
            if (courseView.Kind != CourseView.CourseViewKind.Normal)
                return;  // don't show score courses or variation courses.

            xmlWriter.WriteStartElement("trk", gpxNamespace);
            xmlWriter.WriteElementString("name", gpxNamespace, courseView.CourseName);
            xmlWriter.WriteStartElement("trkseg", gpxNamespace);

            foreach (CourseView.ControlView controlView in courseView.ControlViews) {
                if (controlView.controlId.IsNone)
                    continue;
                if (! waypoints.ContainsKey(controlView.controlId))
                    continue;

                // Write the trackpoint.
                Waypoint wp = waypoints[controlView.controlId];
                WriteTrackPoint(wp);

                // If there are any bends in the next leg, write those.
                if (controlView.legId != null && controlView.legId.Length > 0 && controlView.legId[0].IsNotNone) {
                    Leg leg = eventDB.GetLeg(controlView.legId[0]);
                    if (leg.bends != null && leg.bends.Length > 0) {
                        foreach (PointF bendPoint in leg.bends) {
                            // Get the latitude and longitude.
                            double latitude, longitude;
                            if (!coordinateMapper.GetLatLong(bendPoint, out latitude, out longitude) ||
                                double.IsNaN(latitude) || double.IsNaN(longitude)) 
                            {
                                throw new Exception(MiscText.GpxReprojectFailure);
                            }
                            
                            WriteTrackBendPoint(latitude, longitude);
                        }
                    }
                }
            }

            xmlWriter.WriteEndElement(); // end trkseg
            xmlWriter.WriteEndElement(); // end trk
        }

        private void WriteTrackPoint(Waypoint waypoint)
        {
            xmlWriter.WriteStartElement("trkpt", gpxNamespace);
            xmlWriter.WriteAttributeString("lat", XmlConvert.ToString(waypoint.Latitude));
            xmlWriter.WriteAttributeString("lon", XmlConvert.ToString(waypoint.Longitude));
            xmlWriter.WriteElementString("name", gpxNamespace, waypoint.Name);
            xmlWriter.WriteEndElement();
        }

        private void WriteTrackBendPoint(double latitude, double longitude)
        {
            xmlWriter.WriteStartElement("trkpt", gpxNamespace);
            xmlWriter.WriteAttributeString("lat", XmlConvert.ToString(latitude));
            xmlWriter.WriteAttributeString("lon", XmlConvert.ToString(longitude));
            xmlWriter.WriteEndElement();
        }

        // Return an exception map used to test exported GPX files.
        public static Dictionary<string, string> TestFileExceptionMap()
        {
            Dictionary<string, string> exceptions = new Dictionary<string, string>();
            exceptions[@"^    <time>\d\d\d\d-\d\d-\d\dT\d\d:\d\d:\d\dZ</time>$"] = @"^    <time>\d\d\d\d-\d\d-\d\dT\d\d:\d\d:\d\dZ</time>$";
            return exceptions;
        }


        private class Waypoint
        {
            public string Name;
            public double Longitude, Latitude;
            public WaypointKind Kind;
        }

        private enum WaypointKind { Start, Finish, Exchange, Crossing, Control }
    }

    // Has all the settings for creating OCAD files.
    class GpxCreationSettings
    {
        public Id<Course>[] CourseIds;          // Courses to export. Course.None means all controls.
        public bool AllCourses = true;          // If true, overrides CourseIds except for all controls.
        public string CodePrefix;               // Add this to control codes

        public GpxCreationSettings Clone()
        {
            var clone = (GpxCreationSettings) base.MemberwiseClone();
            clone.CourseIds = (Id<Course>[]) clone.CourseIds.Clone();
            return clone;
        }
    }

    // This class maps between paper coordinates, real world coordinates, and WGS84 lat/long.
    class CoordinateMapper
    {
        double mapScale;
        RealWorldCoords realWorldCoords;
        ProjectionInfo mapProjection, wgs1984Projection;
        MapProjectionType mapProjectionType;
        bool hasRealWorldCoords;

        public CoordinateMapper(Map map)
        {
            if (map == null)
                throw new Exception(MiscText.GpxMustBeOcadMap);

            using (map.Read()) {
                mapScale = map.MapScale;
                realWorldCoords = map.RealWorldCoords;

                if (!realWorldCoords.RealWorldOn && realWorldCoords.RealWorldAngle == 0 && realWorldCoords.RealWorldOffsetX == 0 && realWorldCoords.RealWorldOffsetY == 0) {
                    hasRealWorldCoords = false;
                    mapProjectionType = MapProjectionType.None;
                }
                else {
                    hasRealWorldCoords = true;
                    mapProjectionType = realWorldCoords.ProjectionType;
                    if (mapProjectionType == MapProjectionType.Known) {
                        SetupProjection(realWorldCoords.Proj4String);
                    }
                }

            }
        }

        public bool HasRealWorldCoords
        {
            get { return hasRealWorldCoords; }
        }

        public MapProjectionType MapProjectionType
        {
            get { return mapProjectionType; }
        }

        private void SetupProjection(string proj4String)
        {
            mapProjection = ProjectionInfo.FromProj4String(proj4String);
            wgs1984Projection = ProjectionInfo.FromProj4String("+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs");
        }

        // Get the real world coordinate that matches a paper coordinate.
        public bool GetRealWorld(PointF paperCoord, out double realX, out double realY)
        {
            if (hasRealWorldCoords) {
                double x = paperCoord.X, y = paperCoord.Y;
                x *= (mapScale / 1000.0); y *= (mapScale / 1000.0);
                double ang = (-realWorldCoords.RealWorldAngle * Math.PI) / 180.0;
                realX = x * Math.Cos(ang) - y * Math.Sin(ang);
                realY = x * Math.Sin(ang) + y * Math.Cos(ang);
                realX += realWorldCoords.RealWorldOffsetX;
                realY += realWorldCoords.RealWorldOffsetY;

                realX -= realWorldCoords.RealWorldLocalOffsetX;
                realY -= realWorldCoords.RealWorldLocalOffsetY;
                return true;
            }
            else {
                realX = realY = 0;
                return false;
            }
        }

        public bool GetLatLong(PointF paperCoord, out double latitude, out double longitude)
        {
            latitude = longitude = 0;

            try {
                // Convert paper coord to real world coord. Must use double rather than Matrix to
                // keep precision.
                double realX, realY;
                if (!GetRealWorld(paperCoord, out realX, out realY))
                    return false;

                double[] xy = { realX, realY };
                double[] z = {1};
                Reproject.ReprojectPoints(xy, z, mapProjection, wgs1984Projection, 0, 1);
                longitude = xy[0];
                latitude = xy[1];

            }
            catch (ProjectionException) {
                return false;
            }

            return true;
        }
    }
}
