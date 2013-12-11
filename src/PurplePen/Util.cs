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
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using System.Windows.Forms;
using PurplePen.MapModel;
using PurplePen.Graphics2D;

namespace PurplePen
{

    // Name style for ControlPointName()
    public enum NameStyle { Long, Medium, Short };


    /// <summary>
    /// A whole bunch of static utility functions.
    /// </summary>
    static class Util
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        static extern bool PathRelativePathTo(
             [Out] StringBuilder pszPath,
             [In] string pszFrom,
             [In] uint dwAttrFrom,
             [In] string pszTo,
             [In] uint dwAttrTo
        );
        const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
        const uint FILE_ATTRIBUTE_NORMAL = 0x0;
        const int MAX_PATH = 260;

        // Get the relative name, if possible, of one file relative to another.
        public static string GetRelativeFileName(string relativeTo, string file)
        {
            StringBuilder result = new StringBuilder(MAX_PATH);
            bool ret = PathRelativePathTo(result, relativeTo, FILE_ATTRIBUTE_NORMAL, file, FILE_ATTRIBUTE_NORMAL);
            if (ret == false)
                return file;        // no relative path.
            else {
                // If the hittest starts with .\, remove that.
                if (result.Length > 2 && result[0] == '.' && result[1] == '\\')
                    result.Remove(0, 2);
                return result.ToString();
            }
        }

        // Get the relative name, if possible, of one file relative to the output file name
        // of an xmltextwriter.
        public static string GetRelativeFileName(XmlTextWriter xmlwriter, string file)
        {
            Stream stream = xmlwriter.BaseStream;
            if (stream == null)
                return file;
            FileStream filestream = stream as FileStream;
            if (filestream == null)
                return file;
            string xmlFileName = filestream.Name;
            if (xmlFileName == null)
                return file;

            return GetRelativeFileName(xmlFileName, file);
        }

        // Filters out invalid path characters in a string, replacing them with underscores.
        public static string FilterInvalidPathChars(string path)
        {
            List<char> invalidChars = new List<char>();
            invalidChars.AddRange(Path.GetInvalidFileNameChars());
            invalidChars.AddRange(Path.GetInvalidPathChars());

            StringBuilder builder = new StringBuilder();
            foreach (char c in path) {
                if (invalidChars.Contains(c))
                    builder.Append('_');
                else
                    builder.Append(c);
            }

            return builder.ToString();
        }


        // Given the name of a file that resides in the .EXE directory, return the
        // full path to that file.
        public static string GetFileInAppDirectory(string filename)
        {
            // Using Application.StartupPath would be
            // simpler and probably faster, but doesn't work with NUnit.
            string codebase = typeof(Controller).Assembly.CodeBase;
            Uri uri = new Uri(codebase);
            string appPath = Path.GetDirectoryName(uri.LocalPath);

            // Create the core objects needed for the application to run.
            return Path.Combine(appPath, filename);
        }


        // Remove the "&" prefix in menu names
        public static string RemoveHotkeyPrefix(string s)
        {
            return s.Replace("&", "");
        }

        // Remove a "m" or " m" suffix from a string. If none, return the string itself.
        public static string RemoveMeterSuffix(string s)
        {
            if (s == null)
                return s;

            string sTrim = s.Trim();

            if (sTrim.EndsWith("m"))
                return sTrim.Substring(0, sTrim.Length - 1).Trim();
            else
                return s;
        }

        // Get a list of print scales from a map scale.
        // Current algorithm: use 4000, 5000, 7500, 10000, 15000, plus the map scale itself.
        public static float[] PrintScaleList(float mapScale)
        {
            List<float> result = new List<float>(new float[] { 4000, 5000, 7500, 10000, 15000 });
            if (!result.Contains(mapScale))
                result.Add(mapScale);
            result.Sort();
            return result.ToArray();
        }

        // Are two arrays equal
        public static bool ArrayEquals<T>(T[] a1, T[] a2)
        {
            if (a1 == null)
                return a2 == null;
            if (a2 == null)
                return a1 == null;

            if (a1.Length != a2.Length)
                return false;

            for (int i = 0; i < a1.Length; ++i)
                if (!object.Equals(a1[i], a2[i]))
                    return false;

            return true;
        }

        // Get hash code of array.
        public static int ArrayHashCode<T>(T[] a)
        {
            if (a == null)
                return 98112;
            else {
                int hash = 991134;
                for (int i = 0; i < a.Length; ++i)
                    hash ^= a[i].GetHashCode();
                return hash;
            }
        }

        // Clone an array and its elemenets.
        public static T[] CloneArrayAndElements<T>(T[] a)
            where T : ICloneable
        {
            if (a == null)
                return null;

            T[] newArray = new T[a.Length];
            for (int i = 0; i < a.Length; ++i) {
                newArray[i] = (T) a[i].Clone();
            }

            return newArray;
        }

        // Clone a dictionary and its elements.
        public static Dictionary<K, V> CloneDictionary<K, V>(Dictionary<K, V> dict)
        {
            Dictionary<K, V> newDict = new Dictionary<K, V>(dict.Count);

            foreach (KeyValuePair<K, V> pair in dict) {
                K key = pair.Key;
                V value = pair.Value;

                ICloneable cloneableKey = key as ICloneable;
                if (cloneableKey != null) {
                    key = (K) cloneableKey.Clone();
                }

                ICloneable cloneableValue = value as ICloneable;
                if (cloneableValue != null) {
                    value = (V) cloneableValue.Clone();
                }

                newDict.Add(key, value);
            }

            return newDict;
        }

        // Round a rectangle. Returns a sane hittest of rounding each coordinate. Rectangle.Round doesn't do that!
        public static Rectangle Round(RectangleF rect)
        {
            return Rectangle.FromLTRB((int)Math.Round(rect.Left), (int)Math.Round(rect.Top), (int)Math.Round(rect.Right), (int)Math.Round(rect.Bottom));
        }

        private static Graphics hiresGraphics;

        // Returns a graphics scaled with negative Y and hi-resolution (50 units/pixel or so).
        public static Graphics GetHiresGraphics()
        {
            if (hiresGraphics == null) {
                hiresGraphics = Graphics.FromHwnd(IntPtr.Zero);
                hiresGraphics.ScaleTransform(50F, -50F);
                hiresGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            }

            return hiresGraphics;
        }

        // Go to a given web page.
        public static void GoToWebPage(string url)
        {
            System.Diagnostics.Process.Start(url);
        }

        // Show a given page of help.
        public static void ShowHelpTopic(Form form, string pageName)
        {
            Help.ShowHelp(form, "file:" + GetFileInAppDirectory("Purple Pen Help.chm"), HelpNavigator.Topic, pageName);
        }

        public static bool IsInteger(string s)
        {
            int result;
            return int.TryParse(s, NumberStyles.None, null, out result);
        }

        // Compare two codes and sort them. Integers sort in integer
        // order before strings in string order.
        public static int CompareCodes(string code1, string code2)
        {
            // Null sorts first.
            if (code1 == null && code2 == null)
                return 0;
            else if (code1 == null)
                return -1;
            else if (code2 == null)
                return 1;

            bool isInt1, isInt2;

            isInt1 = IsInteger(code1);
            isInt2 = IsInteger(code2);

            if (isInt1 && !isInt2)
                return -1;
            else if (!isInt1 && isInt2)
                return 1;
            else if (isInt1 && isInt2)
                return int.Parse(code1).CompareTo(int.Parse(code2));
            else if (!isInt1 && !isInt2)
                return string.Compare(code1, code2);

            return 0;  // can't get here.
        }

        private static Cursor moveHandleCursor;
        private static Cursor deleteHandleCursor;

        // Load the MoveHandle cursor.
        public static Cursor MoveHandleCursor
        {
            get
            {
                if (moveHandleCursor == null) {
                    moveHandleCursor = new Cursor(typeof(Util).Assembly.GetManifestResourceStream("PurplePen.Images.MoveHandle.cur"));
                }
                return moveHandleCursor;
            }
        }

        // Load the DeleteHandle cursor.
        public static Cursor DeleteHandleCursor
        {
            get
            {
                if (deleteHandleCursor == null) {
                    deleteHandleCursor = new Cursor(typeof(Util).Assembly.GetManifestResourceStream("PurplePen.Images.DeleteHandle.cur"));
                }
                return deleteHandleCursor;
            }
        }

        // Given an array of points that define a path, add a new bend into it at the "right" place where it fits.
        // The oldPoints array may be null or empty.
        public static PointF[] AddPointToArray(PointF[] oldPoints, PointF newPoint)
        {
            if (oldPoints == null || oldPoints.Length == 0) {
                // Simple case -- no old path.
                return new PointF[1] { newPoint };
            }
            else {
                // Complex case. We need to figure out where the newPoint goes by finding the closest point
                // on the path.
                PointF closestPoint;
                SymPath path = new SymPath(oldPoints);
                path.DistanceFromPoint(newPoint, out closestPoint);

                // On which segment does the closest point lie?
                int segmentStart, segmentEnd;
                path.FindSegmentWithPoint(closestPoint, 0.01F, out segmentStart, out segmentEnd);

                // Insert the point in that segment.
                List<PointF> list = new List<PointF>(oldPoints);
                list.Insert(segmentEnd, newPoint);
                return list.ToArray();
            }
        }

        // Given an array of points and a point in it, remove the given point from the array.
        public static PointF[] RemovePointFromArray(PointF[] points, PointF pointToRemove)
        {
            List<PointF> list = new List<PointF>(points);

            return list.FindAll(delegate(PointF pt) { return pt != pointToRemove; }).ToArray();
        }

        // Get a bit from a uint. The bit number is interpreted mod 32.
        public static bool GetBit(uint u, int bitNumber)
        {
            return (u & (1 << (bitNumber & 0x1F))) != 0;
        }

        // Set a bit from a uint. The bit number is interpreted mod 32.
        public static uint SetBit(uint u, int bitNumber, bool newValue)
        {
            if (newValue)
                return u | (1U << (bitNumber & 0x1F));
            else
                return u & ~(1U << (bitNumber & 0x1F));
        }

        public static bool IsPrerelease(string version)
        {
            Version v = new Version(version);
            return (v.Revision < VersionNumber.Stable);
        }

        // Compare version strings. If s1 < s2, return -1; if s1 > s2, return 1, else return 0.
        // Returns 0 if one or both didn't parse.
        public static int CompareVersionStrings(string s1, string s2)
        {
            Version v1, v2;
            if (Version.TryParse(s1, out v1) && Version.TryParse(s2, out v2))
                return v1.CompareTo(v2);
            else
                return 0;
        }

        // Compare version strings. Return true if all exception last component is same.
        // Return false if one or both didn't parse.
        public static bool SameExceptRevision(string s1, string s2)
        {
            Version v1, v2;
            if (Version.TryParse(s1, out v1) && Version.TryParse(s2, out v2))
                return (v1.Major == v2.Major && v1.Minor == v2.Minor && v1.Build == v2.Build);
            else
                return false;
        }

        // Pretty-ize the version string. 
        public static string PrettyVersionString(string verString)
        {
            Version v;

            if (Version.TryParse(verString, out v)) {
                string modifier;

                if (v.Revision >= VersionNumber.Stable)
                    modifier = "";
                else if (v.Revision >= VersionNumber.RC)
                    modifier = " " + string.Format(MiscText.Version_RC, (v.Revision - VersionNumber.RC) / 10.0);
                else if (v.Revision >= VersionNumber.Beta)
                    modifier = " " + string.Format(MiscText.Version_Beta, (v.Revision - VersionNumber.Beta) / 10.0);
                else if (v.Revision >= VersionNumber.Alpha)
                    modifier = " " + string.Format(MiscText.Version_Alpha, (v.Revision - VersionNumber.Alpha) / 10.0);
                else
                    modifier = string.Format(" ({0})", v.Revision);

                return string.Format("{0}.{1}.{2}{3}", v.Major, v.Minor, v.Build, modifier);
            }
            else {
                return verString;
            }
        }

        // Get text describing a distance. The input is in hundreths of an inch.
        public static string GetDistanceText(int distance, bool addUnits = true)
        {
            string result;
            if (RegionInfo.CurrentRegion.IsMetric) {
                result = (distance * 25.4 / 100.0).ToString("0");
                if (addUnits)
                    result += "mm";
            }
            else {
                result = (distance / 100.0).ToString("0.##");
                if (addUnits)
                    result += "\"";
            }

            return result;
        }

        // Get decimal for a distance.
        public static decimal GetDistanceValue(int distance)
        {
            if (RegionInfo.CurrentRegion.IsMetric) {
                return ((decimal) distance * 25.4M / 100.0M);
            }
            else {
                return ((decimal)distance / 100.0M);
            }
        }

        // Get distance in hundredth of an inch from a decimal.
        public static int GetDistanceFromValue(decimal value)
        {
            if (RegionInfo.CurrentRegion.IsMetric) {
                return (int) Math.Round(value * 100.0M / 25.4M);
            }
            else {
                return (int) Math.Round(value * 100.0M);
            }
        }

        // Get text describing a paper size.
        public static string GetPaperSizeText(PaperSize paperSize)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(paperSize.PaperName);
            builder.AppendFormat(" ({0} x {1})", GetDistanceText(paperSize.Width), GetDistanceText(paperSize.Height));
            return builder.ToString();
        }

        // Get text describing margins.
        public static string GetMarginsText(Margins margins)
        {
            if (margins.Left == margins.Right && margins.Left == margins.Top && margins.Left == margins.Bottom && margins.Left == margins.Right) {
                // All margins all the same. Simplify the text.
                return string.Format(MiscText.Margins_All, GetDistanceText(margins.Left));
            }
            else {
                return string.Format(MiscText.Margins_LRTB, GetDistanceText(margins.Left), GetDistanceText(margins.Right), GetDistanceText(margins.Top), GetDistanceText(margins.Bottom));
            }
        }

        // Get the text name for a control. THe Name Style controls how the control points appear:
        // Long:  "Control 32", "Start", "Finish", "Mandatory crossing point".
        // Medium: "32", "Start", "Finish", "Crossing"
        // Short: "32", "S", "F", "C"
        public static string ControlPointName(EventDB eventDB, Id<ControlPoint> controlId, NameStyle style)
        {
            ControlPoint control = eventDB.GetControl(controlId);

            // Control name/code.
            switch (control.kind) {
            case ControlPointKind.Normal:
                if (style == NameStyle.Long)
                    return string.Format(MiscText.Control_Code, control.code);
                else
                    return string.Format("{0}", control.code);

            case ControlPointKind.Start:
                if (style == NameStyle.Short)
                    return MiscText.Start_Short;
                else
                    return MiscText.Start;

            case ControlPointKind.Finish:
                if (style == NameStyle.Short)
                    return MiscText.Finish_Short;
                else
                    return MiscText.Finish;

            case ControlPointKind.CrossingPoint:
                if (style == NameStyle.Long)
                    return MiscText.MandCrossing_Long;
                else if (style == NameStyle.Medium)
                    return MiscText.MandCrossing_Medium;
                else
                    return MiscText.MandCrossing_Short;

            case ControlPointKind.MapExchange:
                if (style == NameStyle.Long)
                    return MiscText.MapExchange_Long;
                else if (style == NameStyle.Medium)
                    return MiscText.MapExchange_Medium;
                else
                    return MiscText.MapExchange_Short;

            default:
                Debug.Fail("bad control kind");
                return "";
            }
        }

        // Copy a dictionary, so changes to the source no longer affect the result.
        public static Dictionary<K, V> CopyDictionary<K, V>(Dictionary<K, V> source)
        {
            if (source == null)
                return null;

            Dictionary<K,V> result = new Dictionary<K,V>();

            foreach (KeyValuePair<K, V> pair in source) {
                result.Add(pair.Key, pair.Value);
            }

            return result;
        }

        public static string CurrentLangName()
        {
            CultureInfo culture = System.Threading.Thread.CurrentThread.CurrentUICulture;
            return culture.TwoLetterISOLanguageName;
        }

        public static bool EqualArrays<T>(T[] a1, T[] a2)
        {
            if (a1 == null)
                return (a2 == null);
            else if (a2 == null)
                return (a1 == null);
            else {
                if (a1.Length != a2.Length)
                    return false;

                for (int i = 0; i < a1.Length; ++i) {
                    if (!a1[i].Equals(a2[i]))
                        return false;
                }
            }

            return true;
        }

        public static Point PointFromPointF(PointF pointf)
        {
            return new Point((int)Math.Round(pointf.X), (int)Math.Round(pointf.Y));
        }


    }

}
