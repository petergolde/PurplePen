using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml;

using PurplePen.MapModel;
using DotSpatial.Projections;
using System.Drawing.Drawing2D;

namespace PurplePen
{
    class GpxFile
    {
        private EventDB eventDB;
        private XmlWriter xmlWriter;
        private string prefix;
        private CoordinateMapper coordinateMapper;

        private const string gpxNamespace = "http://www.topografix.com/GPX/1/1";
        private const string xsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";
        private const string gpxSchema = "http://www.topografix.com/GPX/1/1/gpx.xsd";

        public GpxFile(EventDB eventDB, CoordinateMapper coordinateMapper, string prefix)
        {
            this.eventDB = eventDB;
            this.coordinateMapper = coordinateMapper;
            this.prefix = prefix;
        }

        public void WriteGpx(string fileName)
        {
            List<Waypoint> waypoints = CollectWaypoints();
            WriteGpxXml(fileName, waypoints);
        }

        private List<Waypoint> CollectWaypoints()
        {
            List<Waypoint> waypointList = new List<Waypoint>();

            CollectWaypointsOfKind(waypointList, ControlPointKind.Start, "STA", WaypointKind.Start);
            CollectWaypointsOfKind(waypointList, ControlPointKind.MapExchange, "XCHG", WaypointKind.Exchange);
            CollectWaypointsOfKind(waypointList, ControlPointKind.Normal, "CTL", WaypointKind.Control);
            CollectWaypointsOfKind(waypointList, ControlPointKind.CrossingPoint, "CROS", WaypointKind.Crossing);
            CollectWaypointsOfKind(waypointList, ControlPointKind.Finish, "FIN", WaypointKind.Finish);
            return waypointList;
        }

        // Collect waypoints for all the controls of a certain kind.
        // The code prefix is used for controls without a code set (like start/finish)
        private void CollectWaypointsOfKind(List<Waypoint> waypointList, ControlPointKind controlKind, string noCodeName, WaypointKind kind)
        {
            int noCodeSuffix = 1;

            foreach (Id<ControlPoint> controlId in eventDB.AllControlPointIds) {
                ControlPoint control = eventDB.GetControl(controlId);
                if (control.kind == controlKind) {
                    // Get the name.
                    string name = control.code;
                    if (string.IsNullOrEmpty(name))
                        name = noCodeName + (noCodeSuffix++).ToString();
                    name = prefix + name;

                    // Get the latitude and longitude.
                    double latitude, longitude;
                    if (!coordinateMapper.GetLatLong(control.location, out latitude, out longitude))
                        // UNDONE: move text to localizable
                        throw new Exception("Could not reproject points; either the coordinate system or the real world coordinates in the OCAD map are wrong.");

                    // Create a waypoint.
                    waypointList.Add(new Waypoint() { Name = name, Kind = kind, Latitude = latitude, Longitude = longitude });
                }
            }
        }


        private void WriteGpxXml(string fileName, List<Waypoint> waypoints)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = new UTF8Encoding(false);
            xmlWriter = XmlWriter.Create(fileName, settings);

            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("gpx", gpxNamespace);
            xmlWriter.WriteAttributeString("creator", "Purple Pen");
            xmlWriter.WriteAttributeString("version", "1.1");
            xmlWriter.WriteAttributeString("xsi", "schemaLocation", xsiNamespace, gpxNamespace + " " + gpxSchema);

            WriteMetadata();

            foreach (Waypoint waypoint in waypoints) {
                WriteWaypoint(waypoint);
            }

            xmlWriter.WriteEndElement();
            xmlWriter.Close();

        }

        private void WriteMetadata()
        {
            xmlWriter.WriteStartElement("metadata", gpxNamespace);

            xmlWriter.WriteStartElement("link", gpxNamespace);
            xmlWriter.WriteAttributeString("href", "http://purple-pen.org");
            xmlWriter.WriteElementString("text", gpxNamespace, "Purple Pen");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteElementString("time", gpxNamespace, DateTime.UtcNow.ToString("s") + "Z");
            xmlWriter.WriteEndElement();
        }

        private void WriteWaypoint(Waypoint waypoint)
        {
            string symText;
            switch (waypoint.Kind) {
                case WaypointKind.Start: symText = "Navaid, Red"; break;
                case WaypointKind.Finish: symText = "Navaid, Green"; break;
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

    // This class maps between paper coordinates and WGS84 lat/long.
    class CoordinateMapper
    {
        double mapScale;
        RealWorldCoords realWorldCoords;
        ProjectionInfo mapProjection, wgs1984Projection;
        Matrix paperToRealWorldTransform;

        public CoordinateMapper(Map map)
        {
            if (map == null)
                throw new Exception("The map file must be an OCAD file to use GPX files.");

            using (map.Read()) {
                // UNDONE: move text to localizable.
                realWorldCoords = map.RealWorldCoords;
                if (!realWorldCoords.RealWorldOn)
                    throw new Exception("The OCAD file must have real world coordinates defined to use GPX files.");
                if (realWorldCoords.ProjectionType != MapProjectionType.Known) {
                    if (realWorldCoords.ProjectionType == MapProjectionType.None)
                        throw new Exception("The OCAD file must have a coordinate system defined to use GPX files.");
                    else if (realWorldCoords.ProjectionType == MapProjectionType.Unknown)
                        throw new Exception("The OCAD file uses a coordinate system that is not supported by Purple Pen.");
                }

                mapScale = map.MapScale;
                SetupProjection(realWorldCoords.Proj4String);
            }
        }

        private void SetupProjection(string proj4String)
        {
            mapProjection = ProjectionInfo.FromProj4String(proj4String);
            wgs1984Projection = ProjectionInfo.FromProj4String("+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs");
        }

        private void SetupTransform(double mapScale)
        {
            float scaleFactor = (float) (mapScale / 1000.0);
            paperToRealWorldTransform = new Matrix();
            paperToRealWorldTransform.Scale(scaleFactor, scaleFactor, MatrixOrder.Append);
            paperToRealWorldTransform.Rotate(-(float)realWorldCoords.RealWorldAngle, MatrixOrder.Append);
            paperToRealWorldTransform.Translate((float)realWorldCoords.RealWorldOffsetX, (float)realWorldCoords.RealWorldOffsetY, MatrixOrder.Append);
        }

        public bool GetLatLong(PointF paperCoord, out double latitude, out double longitude)
        {
            latitude = longitude = 0;
            try {
                // Convert paper coord to real world coord. Must use double rather than Matrix to
                // keep precision.
                double x = paperCoord.X, y = paperCoord.Y;
                x *= (mapScale / 1000.0); y *= (mapScale / 1000.0);
                double ang = (-realWorldCoords.RealWorldAngle * Math.PI) / 180.0;
                double realX = x * Math.Cos(ang) - y * Math.Sin(ang);
                double realY = x * Math.Sin(ang) + y * Math.Cos(ang);
                realX += realWorldCoords.RealWorldOffsetX;
                realY += realWorldCoords.RealWorldOffsetY;

                double[] xy = {realX, realY};
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
