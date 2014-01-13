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
using System.Drawing;
using System.Xml;
using System.Diagnostics;
using System.IO;

#pragma warning disable 659


namespace PurplePen
{
    // Utilities to help event DB classes.
    static class EventDBUtil
    {
        public static void WriteDescriptionKindAttribute(System.Xml.XmlTextWriter xmloutput, DescriptionKind descKind)
        {
            string descKindText;
            switch (descKind) {
                case DescriptionKind.Symbols:           descKindText = "symbols"; break;
                case DescriptionKind.Text:              descKindText = "text"; break;
                case DescriptionKind.SymbolsAndText:    descKindText = "symbols-and-text"; break;
                default:                                Debug.Fail("bad desc kind"); descKindText = "none"; break;
            }
            xmloutput.WriteAttributeString("description-kind", descKindText);
        }

        public static DescriptionKind ReadDescriptionKindAttribute(XmlInput xmlinput)
        {
            string descKindText = xmlinput.GetAttributeString("description-kind");
            switch (descKindText) {
            case "symbols":                   return DescriptionKind.Symbols; 
            case "text":                         return DescriptionKind.Text; 
            case "symbols-and-text":    return DescriptionKind.SymbolsAndText;
            default: xmlinput.BadXml("Invalid description-kind '{0}'", descKindText); return DescriptionKind.Symbols;
            }
        }

        public static void WriteScoreColumnAttribute(System.Xml.XmlTextWriter xmloutput, int scoreColumn)
        {
            string scoreColumnText = null;
            switch (scoreColumn) {
                case -1: scoreColumnText = "none"; break;
                case 0: scoreColumnText = "A"; break;
                case 1: scoreColumnText = "B"; break;
                case 7: scoreColumnText = "H"; break;
            }
            if (scoreColumnText != null)
                xmloutput.WriteAttributeString("score-column", scoreColumnText);
        }

        public static int ReadScoreColumnAttribute(XmlInput xmlinput) {
            string scoreColumnText = xmlinput.GetAttributeString("score-column", "").Trim().ToUpper();
            switch (scoreColumnText) {
                case "A": return 0; 
                case "B": return 1; 
                case "H": return 7; 
                case "NONE": return -1; 
                default: return 0; // prior default was column A
            }
        }
    }

    public class PunchPattern: ICloneable
    {
        public int size;              // Size of the punch pattern (e.g., 9 means 9x9).
        public bool[,] dots;       // Dots in the punch pattern. Can not be null.

        // Is the punch pattern empty?
        public bool IsEmpty {
            get {
                for (int i = 0; i < size; ++i)
                    for (int j = 0; j < size; ++j)
                        if (dots[i, j])
                            return false;

                return true;
            }
        }

        public override bool Equals(object obj)
        {
            PunchPattern other = obj as PunchPattern;
            if (other == null)
                return false;

            if (size != other.size)
                return false;

            for (int i = 0; i < size; ++i)
                for (int j = 0; j < size; ++j)
                    if (dots[i, j] != other.dots[i,j])
                        return false;

            return true;
        }

        public object Clone()
        {
            PunchPattern n = new PunchPattern();
            n.size = size;
            n.dots = (bool[,]) dots.Clone();
            return n;
        }
    }

    // The different kinds of control points.
    public enum ControlPointKind
    {
        // The order here determines the order of sorting for all controls course view or score course view
        None,
        Start,                          // A start point
        MapExchange,             // A map exchange (that isn't a control)
        Normal,                         // A normal control point
        Finish,                         // A finish point
        CrossingPoint,                  // A crossing point
    }

    /// <summary>
    /// A control point describes a particular point, not necessarily
    /// any association with a particular course.
    /// </summary>
    public class ControlPoint: StorableObject
    {
        public ControlPointKind kind;   // The kind of control.
        public string code;             // The code for the control.
        public PointF location;         // The location of the control.
        public PunchPattern punches;  // The punch pattern. Null means no punch pattern set yet.
        public float orientation;       // For crossing points only, the orientation in degress
        public string descriptionText;  // null for auto-text, or custom description text
        public string[] symbolIds;      // Array of six symbols ids for column C-H (or one for Finish, CrossingPoint)
        public string columnFText;      // Text for column F, or null for none (/ or | to separate two numbers)
        public Dictionary<int,CircleGap[]> gaps;  // Circle gaps, indexed by scale (rounded to int to prevent rounding problems)
        public string descTextBefore;       // Description text to show before this control (in all courses)
        public string descTextAfter;         // Description text to show after this control (in all courses)
        public bool customCodeLocation;  // If false, default code location in all controls view. If true, use codeLocationAngle.
        public float codeLocationAngle;   // Angle to the code location in the all controls view.

        public ControlPoint()
        {
        }

        public ControlPoint(ControlPointKind kind, string code, PointF location)
        {
            this.kind = kind;
            this.code = code;
            this.location = location;

            if (kind == ControlPointKind.Normal)
                Debug.Assert(code != null);
            else
                Debug.Assert(code == null);         // only normal controls should have codes

            if (kind == ControlPointKind.Normal || kind == ControlPointKind.Start || kind == ControlPointKind.MapExchange)
                symbolIds = new string[6];
            else if (kind == ControlPointKind.Finish || kind == ControlPointKind.CrossingPoint)
                symbolIds = new string[1];
        }

        public void Validate(Id<ControlPoint> id, EventDB.ValidateInfo validateInfo)
        {
            if (kind == ControlPointKind.Normal && code == null)
                throw new ApplicationException(string.Format("Control point '{0}' is missing a code", id));
            if (kind != ControlPointKind.Normal && code != null)
                throw new ApplicationException(string.Format("Control point '{0}' should not have a code", id));

            if ((kind == ControlPointKind.Normal || kind == ControlPointKind.Start || kind == ControlPointKind.MapExchange) && symbolIds.Length != 6)
                throw new ApplicationException(string.Format("Control point '{0}' should have 6 symbols", id));
            if ((kind == ControlPointKind.Finish || kind == ControlPointKind.CrossingPoint) && symbolIds.Length != 1)
                throw new ApplicationException(string.Format("Control point '{0}' should have 1 symbol", id));

            if ((kind != ControlPointKind.Normal && kind != ControlPointKind.Start) && columnFText != null)
                throw new ApplicationException(string.Format("Control point '{0}' should not have column F text", id));

            if (code != null) {
                if (validateInfo.usedCodes.ContainsKey(code))
                    throw new ApplicationException(string.Format("Code '{0}' used by two different controls", code));
                validateInfo.usedCodes[code] = true;
            }
        }


        public override StorableObject Clone()
        {
            ControlPoint n = (ControlPoint) base.Clone();
            n.symbolIds = (string[]) n.symbolIds.Clone();
            if (n.gaps != null) {
                var newDict = new Dictionary<int, CircleGap[]>();
                foreach (KeyValuePair<int, CircleGap[]> pair in n.gaps) {
                    newDict.Add(pair.Key, (CircleGap[])pair.Value.Clone());
                }
                n.gaps = newDict;
            }
            return n;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ControlPoint))
                return false;

            ControlPoint other = (ControlPoint)obj;

            if (other.kind != kind)
                return false;
            if (other.code != code)
                return false;
            if (other.location != location)
                return false;
            if (other.orientation != orientation)
                return false;
            if (other.descriptionText != descriptionText)
                return false;
            if ((other.symbolIds == null || symbolIds == null)) {
                if (other.symbolIds != symbolIds)
                    return false;
            }
            else if (other.symbolIds.Length != symbolIds.Length)
                return false;
            else {
                for (int i = 0; i < symbolIds.Length; ++i)
                    if (other.symbolIds[i] != symbolIds[i])
                        return false;
            }
            if (other.columnFText != columnFText)
                return false;

            if (gaps == null && other.gaps != null)
                return false;
            if (gaps != null && other.gaps == null)
                return false;
            if (gaps != null) {
                if (gaps.Count != other.gaps.Count)
                    return false;
                foreach (int scale in gaps.Keys) {
                    if (!other.gaps.ContainsKey(scale) || ! Util.EqualArrays(other.gaps[scale], gaps[scale]))
                        return false;
                }
            }

            if (! object.Equals(other.punches, punches))
                return false;

            if (other.descTextBefore != descTextBefore)
                return false;
            if (other.descTextAfter != descTextAfter)
                return false;
            if (other.customCodeLocation != customCodeLocation)
                return false;
            if (other.codeLocationAngle != codeLocationAngle)
                return false;

            return true;
        }

        public override void ReadAttributesAndContent(XmlInput xmlinput)
        {
            string kindText = xmlinput.GetAttributeString("kind");
            switch (kindText) {
                case "normal":              kind = ControlPointKind.Normal; break;
                case "start":               kind = ControlPointKind.Start; break;
                case "finish":              kind = ControlPointKind.Finish; break;
                case "crossing-point":      kind = ControlPointKind.CrossingPoint; break;
                case "map-exchange": kind = ControlPointKind.MapExchange; break;
                default:                    xmlinput.BadXml("Invalid control point kind '{0}'", kindText); break;
            }

            if (kind == ControlPointKind.Normal || kind == ControlPointKind.Start || kind == ControlPointKind.MapExchange)
                symbolIds = new string[6];
            else if (kind == ControlPointKind.Finish || kind == ControlPointKind.CrossingPoint)
                symbolIds = new string[1];

            code = null;
            descriptionText = null;
            columnFText = null;

            // Old file format had a single gaps attribute for all scales. Put this in the dictionary with scale of 0, then update after load is finished.
            string gapText = xmlinput.GetAttributeString("gaps", "");
            if (gapText != "") {
                uint gapValue = Convert.ToUInt32(gapText, 2);
                if (gaps == null)
                    gaps = new Dictionary<int, CircleGap[]>();
                gaps[0] = CircleGap.ComputeCircleGaps(gapValue);
            }

            string codeAngle = xmlinput.GetAttributeString("all-controls-code-angle", "");
            if (codeAngle != "") {
                customCodeLocation = true;
                codeLocationAngle = XmlConvert.ToSingle(codeAngle);
            }

            if (kind == ControlPointKind.CrossingPoint)
                orientation = xmlinput.GetAttributeFloat("orientation");

            bool first = true;
            while (xmlinput.FindSubElement(first, "code", "location", "description", "description-text", "gaps", "circle-gaps", "punch-pattern", "description-text-line")) {
                switch (xmlinput.Name) {
                    case "code":
                        if (kind != ControlPointKind.Normal)
                            xmlinput.BadXml("Only normal control points can have a code");
                        code = xmlinput.GetContentString();
                        break;

                    case "location":
                        float x = xmlinput.GetAttributeFloat("x");
                        float y = xmlinput.GetAttributeFloat("y");
                        location = new PointF(x, y);
                        xmlinput.Skip();
                        break;

                    case "punch-pattern":
                        punches = new PunchPattern();
                        punches.size = xmlinput.GetAttributeInt("size");
                        punches.dots = new bool[punches.size, punches.size];

                        string punchPattern = xmlinput.GetContentString();

                        int index = 0;
                        for (int i = 0; i < punches.size; ++i)
                            for (int j = 0; j < punches.size; ++j) {
                                char c;
                                do {
                                    if (index >= punchPattern.Length) {
                                        xmlinput.BadXml("invalid punch pattern");
                                        goto QUITPUNCHPATTERN;
                                    }
                                    c = punchPattern[index++];
                                } while (c != '0' && c != '1');
                                punches.dots[i, j] = (c == '1');
                            }

                    QUITPUNCHPATTERN:
                        break;

                    case "gaps": {
                        int scale = xmlinput.GetAttributeInt("scale", 0);
                        gapText = xmlinput.GetContentString().Trim();

                        if (gapText != "") {
                            if (gaps == null)
                                gaps = new Dictionary<int, CircleGap[]>();
                            if (gapText.Contains(":")) {
                                // For 2.0 beta 1 compatibility only.
                                gaps[scale] = CircleGap.DecodeGaps(gapText);
                            }
                            else if (!gaps.ContainsKey(scale)) {
                                // Only use the old-style if the new-style wasn't found.
                                uint gapValue = Convert.ToUInt32(gapText, 2);
                                gaps[scale] = CircleGap.ComputeCircleGaps(gapValue);
                            }
                        }

                        break;
                    }

                    case "circle-gaps": {
                        int scale = xmlinput.GetAttributeInt("scale", 0);
                        gapText = xmlinput.GetContentString().Trim();

                        if (gapText != "") {
                            if (gaps == null)
                                gaps = new Dictionary<int, CircleGap[]>();
                            if (gapText.Contains(":")) {
                                // This is the new-style; overrides old style if both present.
                                gaps[scale] = CircleGap.DecodeGaps(gapText);
                            }
                        }

                        break;
                    }

                    case "description-text":
                        descriptionText = xmlinput.GetContentString();
                        break;

                    case "description-text-line":
                        xmlinput.CheckElement("description-text-line");
                        string locationText = xmlinput.GetAttributeString("location");
                        if (locationText == "before")
                            descTextBefore = xmlinput.GetContentString();
                        else if (locationText == "after")
                            descTextAfter = xmlinput.GetContentString();
                        else {
                            xmlinput.BadXml("location attribute on description-text-line must be \"before\" or \"after\"");
                            xmlinput.Skip();
                        }
                        break;

                    case "description":
                        string box = xmlinput.GetAttributeString("box");
                        string symbolId = xmlinput.GetAttributeString("iof-2004-ref", null);
                        string text = xmlinput.GetContentString();

                        switch (box) {
                            case "all": symbolIds[0] = symbolId; break;
                            case "C": symbolIds[0] = symbolId; break;
                            case "D": symbolIds[1] = symbolId; break;
                            case "E": symbolIds[2] = symbolId; break;
                            case "F": symbolIds[3] = symbolId; break;
                            case "G": symbolIds[4] = symbolId; break;
                            case "H": symbolIds[5] = symbolId; break;
                            default: xmlinput.BadXml("Invalid box type '{0}'", box); break;
                        }

                        if (box == "F" && !string.IsNullOrEmpty(text))
                            columnFText = text;
                        break;
                }

                first = false;
            }
        }

        public override void WriteAttributesAndContent(System.Xml.XmlTextWriter xmloutput)
        {
            // Write attributes

            string kindText;
            switch (kind) {
                case ControlPointKind.Normal: kindText = "normal"; break;
                case ControlPointKind.Start: kindText = "start"; break;
                case ControlPointKind.Finish: kindText = "finish"; break;
                case ControlPointKind.CrossingPoint: kindText = "crossing-point"; break;
                case ControlPointKind.MapExchange: kindText = "map-exchange"; break;
                default: Debug.Fail("bad kind"); kindText = "none"; break;
            }

            xmloutput.WriteAttributeString("kind", kindText);

            if (kind == ControlPointKind.CrossingPoint)
                xmloutput.WriteAttributeString("orientation", XmlConvert.ToString(orientation));

            if (customCodeLocation) 
                xmloutput.WriteAttributeString("all-controls-code-angle", XmlConvert.ToString(codeLocationAngle));

            // Write sub-elements
            if (code != null)
                xmloutput.WriteElementString("code", code);

            xmloutput.WriteStartElement("location");
            xmloutput.WriteAttributeString("x", XmlConvert.ToString(location.X));
            xmloutput.WriteAttributeString("y", XmlConvert.ToString(location.Y));
            xmloutput.WriteEndElement();

            if (symbolIds != null) {
                for (int i = 0; i < symbolIds.Length; ++i) {
                    string box;

                    if (symbolIds.Length == 1)
                        box = "all";
                    else
                        box = ((char)('C'+i)).ToString();

                    if (symbolIds[i] != null ||
                        ((columnFText != null && (box == "all" || box == "F"))))
                    {
                        xmloutput.WriteStartElement("description");

                        xmloutput.WriteAttributeString("box", box);

                        if (symbolIds[i] != null)
                            xmloutput.WriteAttributeString("iof-2004-ref", symbolIds[i]);

                        if (columnFText != null && (box == "all" || box == "F"))
                            xmloutput.WriteString(columnFText);

                        xmloutput.WriteEndElement();
                    }
                }
            }

            // Write punch pattern.
            if (punches != null && !punches.IsEmpty) {
                xmloutput.WriteStartElement("punch-pattern");
                xmloutput.WriteAttributeString("size", XmlConvert.ToString(punches.size));

                StringBuilder builder = new StringBuilder();
                builder.Append("\r\n");
                for (int row = 0; row < punches.size; ++row) {
                    for (int col = 0; col < punches.size; ++col) {
                        builder.Append(punches.dots[row, col] ? '1' : '0');
                    }
                    builder.Append("\r\n");
                }
                xmloutput.WriteString(builder.ToString());

                xmloutput.WriteEndElement();
            }

            // Write gaps.
            if (gaps != null) {
                foreach (KeyValuePair<int, CircleGap[]> pair in gaps) {
                    // For compatibility with 1.x and 2.0 beta 1, write in the old style of gaps.
                    xmloutput.WriteStartElement("gaps");
                    xmloutput.WriteAttributeString("scale", XmlConvert.ToString(pair.Key));

                    string gapText = Convert.ToString(CircleGap.ComputeApproximateOldStyleGaps(pair.Value), 2);
                    if (gapText.Length < 32)
                        gapText = new string('0', 32 - gapText.Length) + gapText;
                    xmloutput.WriteString(gapText);

                    xmloutput.WriteEndElement();

                    // New-style gap encoding.
                    xmloutput.WriteStartElement("circle-gaps");
                    xmloutput.WriteAttributeString("scale", XmlConvert.ToString(pair.Key));

                    gapText = CircleGap.EncodeGaps(pair.Value);
                    xmloutput.WriteString(gapText);

                    xmloutput.WriteEndElement();
                }
            }

            if (descriptionText != null)
                xmloutput.WriteElementString("description-text", descriptionText);

            if (!string.IsNullOrEmpty(descTextBefore)) {
                xmloutput.WriteStartElement("description-text-line");
                xmloutput.WriteAttributeString("location", "before");
                xmloutput.WriteString(descTextBefore);
                xmloutput.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(descTextAfter)) {
                xmloutput.WriteStartElement("description-text-line");
                xmloutput.WriteAttributeString("location", "after");
                xmloutput.WriteString(descTextAfter);
                xmloutput.WriteEndElement();
            }
        }

        public override string ElementName
        {
            get { return "control"; }
        }
    }

    // The different kinds of courses
    public enum CourseKind
    {
        Normal,                          // A normal course
        Score                            // A score course
    }

    // The diffrent kinds of control labelling
    public enum ControlLabelKind
    {
        Sequence,                         // Control number only
        Code,                             // Control code only
        SequenceAndCode,                  // Control number and code
        SequenceAndScore,                 // Number and score
        CodeAndScore                      // Control code and score
    }

    // The different kinds of control descriptions
    public enum DescriptionKind
    {
        Symbols,                        // Symbolic only (standard)
        Text,                           // Text only
        SymbolsAndText                  // Symbols and text
    }

    public class PartOptions: ICloneable
    {
        public bool ShowFinish;         // Show the course finish of this part if not the last part

        // Default for parts that don't have a part options set.
        public static PartOptions Default = new PartOptions() {
            ShowFinish = false
        };

        public override bool Equals(object obj)
        {
            PartOptions other = obj as PartOptions;
            if (other != null) {
                return other.ShowFinish == this.ShowFinish;
            }

            return false;
        }

        public PartOptions Clone()
        {
            return (PartOptions)base.MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return this.Clone(); ;
        }
    }   

    // Description a main course (not a particular variation, map part, or all controls--
    // those exist only in a particular view).
    public class Course : StorableObject
    {
        public CourseKind kind;         // The kind of course
        public string name;             // Name of the course
        public ControlLabelKind labelKind;// Kind of label for controls on the map
        public int sortOrder;             // Order this course is sorted in. Must be >0, and unique among all courses.
        public string secondaryTitle;   // Secondary title line, or null if none.
        public float printScale;        // Print scale of the course
        public float climb;             // Climb in meters, or negative for no climb.
        public int load;                 // Competitor load, or negative for no load set.
        public int firstControlOrdinal;  // Ordinal number of first control (usually 1 for a normal course.)
        public int scoreColumn;         // column for score, or -1 for none (must be -1 for a non-score course)
        public DescriptionKind descKind;// Kind of description to print
        public RectangleF printArea;  // print area, or empty if no defined print area.
        public Dictionary<int, RectangleF> partPrintAreas; // print area of parts.
        public Dictionary<int, PartOptions> partOptions;  // options of parts.
        public Id<CourseControl> firstCourseControl;  // Id of first course control (None if no controls).

        public Course()
        {
            this.partPrintAreas = new Dictionary<int, RectangleF>();
            this.partOptions = new Dictionary<int, PartOptions>();
        }

        public Course(CourseKind kind, string name, float printScale, int sortOrder): this()
        {
            this.kind = kind;
            this.name = name;
            this.printScale = printScale; 
            this.sortOrder = sortOrder;
            this.labelKind = (kind == CourseKind.Score) ? ControlLabelKind.Code : ControlLabelKind.Sequence;
            this.climb = -1;
            this.load = -1;
            this.firstControlOrdinal = 1;
            this.scoreColumn = -1;
        }

        public void Validate(Id<Course> id, EventDB.ValidateInfo validateInfo)
        {
            Id<CourseControl> nextCourseControl;
            if (name == null)
                throw new ApplicationException(string.Format("Course '{0}' should have a name", id));
            if (sortOrder <= 0)
                throw new ApplicationException(string.Format("Course '{0}' has invalid sort order {1}", id, sortOrder));
            if (firstControlOrdinal <= 0)
                throw new ApplicationException(string.Format("Course '{0}' has invalid first control number {1}", id, firstControlOrdinal));
            if (labelKind != ControlLabelKind.Code && labelKind != ControlLabelKind.Sequence && labelKind != ControlLabelKind.SequenceAndCode && labelKind != ControlLabelKind.CodeAndScore && labelKind != ControlLabelKind.SequenceAndScore)
                throw new ApplicationException(string.Format("Course '{0}' has invalid label kind {1}", id, labelKind));
            if (kind != CourseKind.Score && (labelKind == ControlLabelKind.CodeAndScore || labelKind == ControlLabelKind.SequenceAndScore))
                throw new ApplicationException(string.Format("Course '{0}' has invalid label kind {1} for non-score course", id, labelKind));
            if (kind == 0 && scoreColumn != -1)
                throw new ApplicationException(string.Format("Course '{0}' has invalid score column", id, scoreColumn));

            nextCourseControl = firstCourseControl;

            // Check that no sort order is used more than once.
            if (validateInfo.sortOrders.ContainsKey(sortOrder)) 
                throw new ApplicationException(string.Format("Courses '{0}' and '{1}' both have sort order {2}", id, validateInfo.sortOrders[sortOrder], sortOrder));
            validateInfo.sortOrders.Add(sortOrder, id);

            // Traverse the course control links, to make sure every course control is used once and only once.
            while (nextCourseControl.IsNotNone){
                validateInfo.eventDB.CheckCourseControlId(nextCourseControl);
                if (validateInfo.usedCourseControls.ContainsKey(nextCourseControl))
                    throw new ApplicationException(string.Format("Course control {0} already used by course {1}", nextCourseControl, validateInfo.usedCourseControls[nextCourseControl]));
                validateInfo.usedCourseControls[nextCourseControl] = id;

                CourseControl courseCtl = validateInfo.eventDB.GetCourseControl(nextCourseControl);
                if (courseCtl.split) {
                    Id<CourseControl > idJoin = Id<CourseControl>.None; ;
                    for (int i = 0; i < courseCtl.nextSplitCourseControls.Length; ++i) {
                        Id<CourseControl> idJoinRet = ValidateCourseControlsToJoin(courseCtl.nextSplitCourseControls[i], id, validateInfo);
                        if (idJoinRet.IsNone)
                            throw new ApplicationException("split next course control can't be zero");
                        if (idJoin.IsNotNone && idJoinRet != idJoin)
                            throw new ApplicationException("Split controls don't join correctly");
                        idJoin = idJoinRet;
                    }

                    nextCourseControl = idJoin;
                }
                else {
                    nextCourseControl = courseCtl.nextCourseControl;
                }
            }
        }

        // Validate a string of course control until we get to a join control, which is returned.
        private Id<CourseControl> ValidateCourseControlsToJoin(Id<CourseControl> nextCourseControl, Id<Course> idCourse, EventDB.ValidateInfo validateInfo)
        {
            while (nextCourseControl.IsNotNone) {
                CourseControl courseCtl = validateInfo.eventDB.GetCourseControl(nextCourseControl);
                if (courseCtl.split) {
                    throw new ApplicationException("multiple split points before a join point");
                }
                else if (courseCtl.join) {
                    return nextCourseControl;
                }
                else {
                    validateInfo.eventDB.CheckCourseControlId(nextCourseControl);
                    if (validateInfo.usedCourseControls.ContainsKey(nextCourseControl))
                        throw new ApplicationException(string.Format("Course control {0} already used by course {1}", nextCourseControl, validateInfo.usedCourseControls[nextCourseControl]));
                    validateInfo.usedCourseControls[nextCourseControl] = idCourse;
                }

                nextCourseControl = courseCtl.nextCourseControl;
            }

            throw new ApplicationException("no join point reached");
        }

        public override StorableObject Clone()
        {
            Course n = (Course)base.Clone();

            n.partPrintAreas = Util.CloneDictionary(n.partPrintAreas);
            n.partOptions = Util.CloneDictionary(n.partOptions);

            return n;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Course))
                return false;

            Course other = (Course)obj;

            if (other.kind != kind)
                return false;
            if (other.labelKind != labelKind)
                return false;
            if (other.name != name)
                return false;
            if (other.sortOrder != sortOrder)
                return false;
            if (other.climb != climb)
                return false;
            if (other.load != load)
                return false;
            if (other.printScale != printScale)
                return false;
            if (other.descKind != descKind)
                return false;
            if (other.secondaryTitle != secondaryTitle)
                return false;
            if (other.firstCourseControl != firstCourseControl)
                return false;
            if (other.printArea != printArea)
                return false;
            if (other.firstControlOrdinal != firstControlOrdinal)
                return false;
            if (other.labelKind != labelKind)
                return false;
            if (other.scoreColumn != scoreColumn)
                return false;
            if (other.partPrintAreas.Count != this.partPrintAreas.Count)
                return false;
            foreach (KeyValuePair<int, RectangleF> kvp in this.partPrintAreas) {
                RectangleF rect;
                if (!other.partPrintAreas.TryGetValue(kvp.Key, out rect) || rect != kvp.Value)
                    return false;
            }
            if (other.partOptions.Count != this.partOptions.Count)
                return false;
            foreach (KeyValuePair<int, PartOptions> kvp in this.partOptions) {
                PartOptions partOptions;
                if (!other.partOptions.TryGetValue(kvp.Key, out partOptions) || ! partOptions.Equals(kvp.Value))
                    return false;
            }

            return true;
        }

        public override void ReadAttributesAndContent(XmlInput xmlinput)
        {
            string kindText = xmlinput.GetAttributeString("kind");
            switch (kindText) {
                case "normal":      kind = CourseKind.Normal; break;
                case "score":       kind = CourseKind.Score; break;
                default:            xmlinput.BadXml("Invalid course kind '{0}'", kindText); break;
            }

            sortOrder = xmlinput.GetAttributeInt("order", 0);    // 0 sort orders fixed up later in EventDB.FixCourseSortOrders()

            name = "";
            printScale = 15000;
            descKind = DescriptionKind.Symbols;
            firstCourseControl = Id<CourseControl>.None;
            firstControlOrdinal = 1;
            labelKind = (kind == CourseKind.Score) ? ControlLabelKind.Code : ControlLabelKind.Sequence;
            scoreColumn = (kind == CourseKind.Score) ? 0 : -1;

            bool first = true;
            while (xmlinput.FindSubElement(first, "name", "secondary-title", "first", "print-area", "options", "labels", "part-options")) {
                switch (xmlinput.Name) {
                    case "name":
                        name = xmlinput.GetContentString();
                        break;

                    case "secondary-title":
                        secondaryTitle = xmlinput.GetContentString();
                        break;

                    case "first":
                        firstCourseControl = new Id<CourseControl>(xmlinput.GetAttributeInt("course-control"));
                        firstControlOrdinal = xmlinput.GetAttributeInt("control-number", 1);
                        xmlinput.Skip();
                        break;

                    case "print-area":
                        int part = xmlinput.GetAttributeInt("part", -1);
                        float left = xmlinput.GetAttributeFloat("left");
                        float top = xmlinput.GetAttributeFloat("top");
                        float right = xmlinput.GetAttributeFloat("right");
                        float bottom = xmlinput.GetAttributeFloat("bottom");
                        RectangleF area = RectangleF.FromLTRB(left, bottom, right, top);   // top and bottom reverse due to map orientation.

                        if (part == -1)
                            printArea = area;
                        else
                            partPrintAreas[part] = area;

                        xmlinput.Skip();
                        break;

                    case "options":
                        printScale = xmlinput.GetAttributeFloat("print-scale");
                        climb = xmlinput.GetAttributeFloat("climb", -1F);
                        load = xmlinput.GetAttributeInt("load", -1);
                        if (kind == CourseKind.Score) 
                            scoreColumn = EventDBUtil.ReadScoreColumnAttribute(xmlinput);
                        descKind = EventDBUtil.ReadDescriptionKindAttribute(xmlinput);
                        xmlinput.Skip();
                        break;

                    case "part-options":
                        part = xmlinput.GetAttributeInt("part", -1);
                        bool showFinish = xmlinput.GetAttributeBool("show-finish");

                        if (part != -1)
                            partOptions[part] = new PartOptions() { ShowFinish = showFinish };

                        xmlinput.Skip();
                        break;


                    case "labels":
                        string labelKindText = xmlinput.GetAttributeString("label-kind");
                        switch (labelKindText) {
                            case "sequence":                labelKind = ControlLabelKind.Sequence; break;
                            case "code":                    labelKind = ControlLabelKind.Code; break;
                            case "sequence-and-code":       labelKind = ControlLabelKind.SequenceAndCode; break;
                            case "sequence-and-score":      labelKind = ControlLabelKind.SequenceAndScore; break;
                            case "code-and-score":          labelKind = ControlLabelKind.CodeAndScore; break;
                            default:                        labelKind = ControlLabelKind.Sequence; break;
                        }
                        xmlinput.Skip();
                        break;
                }

                first = false;
            }
        }

        public override void WriteAttributesAndContent(System.Xml.XmlTextWriter xmloutput)
        {
            // Write Attributes
            string kindText;
            switch (kind) {
                case CourseKind.Normal: kindText = "normal"; break;
                case CourseKind.Score: kindText = "score"; break;
                default: Debug.Fail("bad kind"); kindText = "none"; break;
            }

            xmloutput.WriteAttributeString("kind", kindText);
            xmloutput.WriteAttributeString("order", XmlConvert.ToString(sortOrder));

            // Write sub-elements
            xmloutput.WriteElementString("name", name);

            if (secondaryTitle != null)
                xmloutput.WriteElementString("secondary-title", secondaryTitle);

            string labelKindText;
            switch (labelKind) {
                case ControlLabelKind.Sequence: labelKindText = "sequence"; break;
                case ControlLabelKind.Code: labelKindText = "code"; break;
                case ControlLabelKind.SequenceAndCode: labelKindText = "sequence-and-code"; break;
                case ControlLabelKind.SequenceAndScore: labelKindText = "sequence-and-score"; break;
                case ControlLabelKind.CodeAndScore: labelKindText = "code-and-score"; break;
                default: Debug.Fail("bad labelKind"); labelKindText = "none"; break;
            }
            xmloutput.WriteStartElement("labels");
            xmloutput.WriteAttributeString("label-kind", labelKindText);
            xmloutput.WriteEndElement();

            if (firstCourseControl.IsNotNone) {
                xmloutput.WriteStartElement("first");
                xmloutput.WriteAttributeString("course-control", XmlConvert.ToString(firstCourseControl.id));
                if (firstControlOrdinal != 1)
                    xmloutput.WriteAttributeString("control-number", XmlConvert.ToString(firstControlOrdinal));
                xmloutput.WriteEndElement();
            }

            if (!printArea.IsEmpty) {
                xmloutput.WriteStartElement("print-area");
                xmloutput.WriteAttributeString("left", XmlConvert.ToString(printArea.Left));
                xmloutput.WriteAttributeString("top", XmlConvert.ToString(printArea.Bottom));  // rectangle is reversed, so top is really the bottom and vice versa
                xmloutput.WriteAttributeString("right", XmlConvert.ToString(printArea.Right));
                xmloutput.WriteAttributeString("bottom", XmlConvert.ToString(printArea.Top));   // rectangle is reversed, so top is really the bottom and vice versa
                xmloutput.WriteEndElement();
            }

            foreach (KeyValuePair<int, RectangleF> kvp in partPrintAreas) {
                xmloutput.WriteStartElement("print-area");
                xmloutput.WriteAttributeString("part", XmlConvert.ToString(kvp.Key));
                xmloutput.WriteAttributeString("left", XmlConvert.ToString(kvp.Value.Left));
                xmloutput.WriteAttributeString("top", XmlConvert.ToString(kvp.Value.Bottom));  // rectangle is reversed, so top is really the bottom and vice versa
                xmloutput.WriteAttributeString("right", XmlConvert.ToString(kvp.Value.Right));
                xmloutput.WriteAttributeString("bottom", XmlConvert.ToString(kvp.Value.Top));   // rectangle is reversed, so top is really the bottom and vice versa
                xmloutput.WriteEndElement();
            }

            xmloutput.WriteStartElement("options");
            xmloutput.WriteAttributeString("print-scale", XmlConvert.ToString(printScale));
            if (climb >= 0)
                xmloutput.WriteAttributeString("climb", XmlConvert.ToString(climb));
            if (load >= 0)
                xmloutput.WriteAttributeString("load", XmlConvert.ToString(load));

            if (kind == CourseKind.Score) 
                EventDBUtil.WriteScoreColumnAttribute(xmloutput, scoreColumn);

            EventDBUtil.WriteDescriptionKindAttribute(xmloutput, descKind);

            xmloutput.WriteEndElement();

            foreach (KeyValuePair<int, PartOptions> kvp in partOptions) {
                xmloutput.WriteStartElement("part-options");
                xmloutput.WriteAttributeString("part", XmlConvert.ToString(kvp.Key));
                xmloutput.WriteAttributeString("show-finish", XmlConvert.ToString(kvp.Value.ShowFinish));
                xmloutput.WriteEndElement();
            }
        }

        public override string ElementName
        {
            get { return "course"; }
        }
    }

    // Describes a control position on one course
    public class CourseControl: StorableObject
    {
        public Id<ControlPoint> control;             // Id of the control.
        public bool exchange;     // Is this control a map exchange? (must be true for ControlPointKind.MapExchange)
        public bool split;              // Is this the first control before a relay variation split?
        public bool join;               // Is this the control after a relay variation.
        public Id<CourseControl> nextCourseControl;   // Next control, or 0 if this is the last control of the course or split is true.
        public Id<CourseControl>[] nextSplitCourseControls; // null if split is false. Otherwise, the set of next controls in the split (duplicates OK).
        public bool customNumberPlacement;     // If true, the numberDeltaX and numberDeltaY show where to place the code. If false, place in default location.
        public float numberDeltaX, numberDeltaY;       // Where to place the control number relative to the control point location. Only used in customNumberPlacement is true
        public int points;              // Points for score-O
        public string descTextBefore;       // Description text to show before this course control
        public string descTextAfter;         // Description text to show after this course control

        public CourseControl()
        {
        }

        public CourseControl(Id<ControlPoint> controlId, Id<CourseControl> nextCourseControl)
        {
            this.control = controlId;
            this.nextCourseControl = nextCourseControl;
        }

        public void Validate(Id<CourseControl> id, EventDB.ValidateInfo validateInfo)
        {
            if (points < 0)
                throw new ApplicationException(string.Format("Course control '{0}' should not have negative points", id));

            if (control.IsNone)
                throw new ApplicationException(string.Format("Course control '{0}' must have a control", id));
            validateInfo.eventDB.CheckControlId(control);

            if (validateInfo.eventDB.GetControl(control).kind == ControlPointKind.MapExchange && !exchange)
                throw new ApplicationException(string.Format("Course control '{0}' should be marked as a map exchange", id));

            if (exchange && validateInfo.eventDB.GetControl(control).kind != ControlPointKind.MapExchange && validateInfo.eventDB.GetControl(control).kind != ControlPointKind.Normal)
                throw new ApplicationException(string.Format("Course control '{0}' is a exchange, but is not a control or map exchange", id));

            if (split) {
                if (nextSplitCourseControls == null)
                    throw new ApplicationException(string.Format("Course control '{0}' must have next control array", id));
                for (int i = 0; i < nextSplitCourseControls.Length; ++i)
                    validateInfo.eventDB.CheckCourseControlId(nextSplitCourseControls[i]);
            }
            else {
                if (nextSplitCourseControls != null)
                    throw new ApplicationException(string.Format("Course control '{0}' must not have next control array", id));
                if (nextCourseControl.IsNotNone)
                    validateInfo.eventDB.CheckCourseControlId(nextCourseControl);
            }

            if (! validateInfo.usedCourseControls.ContainsKey(id))
                throw new ApplicationException(string.Format("Course control '{0}' not used by any course", id));
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CourseControl))
                return false;

            CourseControl other = (CourseControl)obj;

            if (other.control != control)
                return false;
            if (other.split != split)
                return false;
            if (other.join != join)
                return false;
            if (other.exchange != exchange)
                return false;
            if (other.points != points)
                return false;

            if (split) {
                if (other.nextSplitCourseControls.Length != nextSplitCourseControls.Length)
                    return false;
                for (int i = 0; i < nextSplitCourseControls.Length; ++i)
                    if (other.nextSplitCourseControls[i] != nextSplitCourseControls[i])
                        return false;
            }
            else {
                if (other.nextCourseControl != nextCourseControl)
                    return false;
            }

            if (other.customNumberPlacement != customNumberPlacement)
                return false;
            if (customNumberPlacement) {
                if (other.numberDeltaX != numberDeltaX && other.numberDeltaY != numberDeltaY)
                    return false;
            }

            if (other.descTextBefore != descTextBefore)
                return false;
            if (other.descTextAfter != descTextAfter)
                return false;

            return true;
        }

        public override StorableObject Clone()
        {
            CourseControl n = (CourseControl) base.Clone();
            if (n.nextSplitCourseControls != null)
                n.nextSplitCourseControls = (Id<CourseControl>[])n.nextSplitCourseControls.Clone();
            return n;
        }

        public override void ReadAttributesAndContent(XmlInput xmlinput)
        {
            nextCourseControl = Id<CourseControl>.None;
            nextSplitCourseControls = null;
            control = new Id<ControlPoint>(xmlinput.GetAttributeInt("control"));
            split = xmlinput.GetAttributeBool("relay-split", false);
            join = xmlinput.GetAttributeBool("relay-join", false);
            exchange = xmlinput.GetAttributeBool("map-exchange", false);
            points = xmlinput.GetAttributeInt("points", 0);

            List<Id<CourseControl>> nextCourseControls = new List<Id<CourseControl>>();

            bool first = true;
            while (xmlinput.FindSubElement(first, "next", "number-location", "description-text-line")) {
                switch (xmlinput.Name) {
                case "next":
                    xmlinput.CheckElement("next");
                    nextCourseControls.Add(new Id<CourseControl>(xmlinput.GetAttributeInt("course-control")));
                    xmlinput.Skip();
                    break;

                case "number-location":
                    xmlinput.CheckElement("number-location");
                    customNumberPlacement = true;
                    numberDeltaX = xmlinput.GetAttributeFloat("x");
                    numberDeltaY = xmlinput.GetAttributeFloat("y");
                    xmlinput.Skip();
                    break;

                case "description-text-line":
                    xmlinput.CheckElement("description-text-line");
                    string location = xmlinput.GetAttributeString("location");
                    if (location == "before")
                        descTextBefore = xmlinput.GetContentString();
                    else if (location == "after")
                        descTextAfter = xmlinput.GetContentString();
                    else {
                        xmlinput.BadXml("location attribute on description-text-line must be \"before\" or \"after\"");
                        xmlinput.Skip();
                    }
                    break;
                }
                first = false;
            }

            if (!split && nextCourseControls.Count > 1)
                xmlinput.BadXml("Too many 'next' elements");

            if (split) {
                nextSplitCourseControls = nextCourseControls.ToArray();
            } 
            else if (nextCourseControls.Count > 0) {
                nextCourseControl = nextCourseControls[0];
            }
        }

        public override void WriteAttributesAndContent(XmlTextWriter xmloutput)
        {
            // Write attributes

            xmloutput.WriteAttributeString("control", XmlConvert.ToString(control.id));
            if (split)
                xmloutput.WriteAttributeString("relay-split", XmlConvert.ToString(true));
            if (join)
                xmloutput.WriteAttributeString("relay-join", XmlConvert.ToString(true));
            if (exchange)
                xmloutput.WriteAttributeString("map-exchange", XmlConvert.ToString(true));
            if (points != 0)
                xmloutput.WriteAttributeString("points", XmlConvert.ToString(points));

            // Write elements.
            if (split) {
                foreach (Id<CourseControl> nextCourseControlId in nextSplitCourseControls) {
                    xmloutput.WriteStartElement("next");
                    xmloutput.WriteAttributeString("course-control", XmlConvert.ToString(nextCourseControlId.id));
                    xmloutput.WriteEndElement();
                }
            }
            else {
                if (nextCourseControl.IsNotNone) {
                    xmloutput.WriteStartElement("next");
                    xmloutput.WriteAttributeString("course-control", XmlConvert.ToString(nextCourseControl.id));
                    xmloutput.WriteEndElement();
                }
            }

            if (customNumberPlacement) {
                xmloutput.WriteStartElement("number-location");
                xmloutput.WriteAttributeString("x", XmlConvert.ToString(numberDeltaX));
                xmloutput.WriteAttributeString("y", XmlConvert.ToString(numberDeltaY));
                xmloutput.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(descTextBefore)) {
                xmloutput.WriteStartElement("description-text-line");
                xmloutput.WriteAttributeString("location", "before");
                xmloutput.WriteString(descTextBefore);
                xmloutput.WriteEndElement();
            }

            if (!string.IsNullOrEmpty(descTextAfter)) {
                xmloutput.WriteStartElement("description-text-line");
                xmloutput.WriteAttributeString("location", "after");
                xmloutput.WriteString(descTextAfter);
                xmloutput.WriteEndElement();
            }
        }

        public override string ElementName
        {
            get { return "course-control"; }
        }
    }


    // The different kinds of special objects.
    public enum SpecialKind
    {
        FirstAid,                            // first aid point   (point)
        Water,                              // water point   (point)
        OptCrossing,                    // optional crossing point   (point)
        Forbidden,                       // forbidden route mark   (point)
        RegMark,                          // registration mark
        Boundary,                        // a boundary   (line)
        OOB,                                // out of bounds   (area)
        Dangerous,                      // dangerous area    (area)
        WhiteOut,                        // white out area (area)
        Text,                                // arbitrary text, with replacements   (rectangle)
        Descriptions,                    // control description sheet (rectangle of first square)
    }



    /// <summary>
    /// A special describes a special additional object that isn't a control, and doesn't fit into the 
    /// normal control heirarchy. Special objects are often shared among all the courses.
    /// </summary>
    public class Special: StorableObject
    {
        public SpecialKind kind;            // The kind of special.
        public PointF[] locations;          // The location of the control; might be one or more coordinates (two for a rectangle)
        public float orientation;           // For crossing points only, the orientation in degress
        public bool allCourses;             // If true, special is in all courses.
        public CourseDesignator[] courses;  // If allCourses is false, an array of the course designators this special is in.
        public string text;                 // for text objects, the text.
        public string fontName;             // for text objects, the font name
        public bool fontBold, fontItalic;   // for text objects, the font style
        public int numColumns = 1;          // for description objects, the number of columns.

        public Special()
        {
        }

        public Special(SpecialKind kind, PointF[] locations)
        {
            this.kind = kind;
            this.locations = (PointF[]) locations.Clone();
            this.allCourses = true;
        }

        public void Validate(Id<Special> id, EventDB.ValidateInfo validateInfo)
        {
            switch (kind) {
            case SpecialKind.FirstAid:
            case SpecialKind.Water:
            case SpecialKind.OptCrossing:
            case SpecialKind.Forbidden:
            case SpecialKind.RegMark:
                if (locations.Length != 1)
                    throw new ApplicationException(string.Format("Special point object {0} should have exactly 1 coordinate", id));
                break;

            case SpecialKind.Boundary:
                if (locations.Length < 2)
                    throw new ApplicationException(string.Format("Special line object {0} should have 2 or more coordinates", id));
                break;

            case SpecialKind.OOB:
            case SpecialKind.Dangerous:
            case SpecialKind.WhiteOut:
                if (locations.Length < 2)
                    throw new ApplicationException(string.Format("Special line object {0} should have 3 or more coordinates", id));
                break;

            case SpecialKind.Text:
            case SpecialKind.Descriptions:
                if (locations.Length != 2)
                    throw new ApplicationException(string.Format("Text or descriptions object {0} should have 2 coordinates", id));
                break;

            default:
                throw new ApplicationException("Bad special kind"); 
            }

            if (kind == SpecialKind.Text && text == null)
                throw new ApplicationException(string.Format("Text object {0} should have non-null text", id));

            if (kind == SpecialKind.Text) {
                if (fontName == null || fontName == "")
                    throw new ApplicationException(string.Format("Text object {0} should have non-null font name", id));
            }

            if (kind == SpecialKind.Dangerous) {
                if (numColumns < 1 || numColumns > 100)
                    throw new ApplicationException(string.Format("Description object {0} should have 1-100 columns", id));
            }

            if (allCourses) {
                if (courses != null)
                    throw new ApplicationException(string.Format("Special {0} should have null courses array", id));

                if (kind == SpecialKind.Descriptions) {
                    // All the courses have used a description block
                    foreach (Id<Course> courseId in validateInfo.eventDB.AllCourseIds) {
                        if (validateInfo.usedDescriptionCourses.ContainsKey(new CourseDesignator(courseId)))
                            throw new ApplicationException(string.Format("Course {0} has multiple descriptions (special {1})", courseId, id));
                        validateInfo.usedDescriptionCourses[new CourseDesignator(courseId)] = true;
                    }
                }
            }
            else {
                if (courses == null)  // ok to have zero-length array, if special is in all controls but not on any course.
                    throw new ApplicationException(string.Format("Special {0} should have real courses array", id));
                foreach (CourseDesignator courseDesignator in courses) {
                    if (courseDesignator.IsAllControls) {
                        if (kind != SpecialKind.Descriptions)
                            throw new ApplicationException("Only descriptions should be directly in all controls");
                    }
                    else {
                        validateInfo.eventDB.CheckCourseId(courseDesignator.CourseId);
                    }

                    if (kind == SpecialKind.Descriptions) {
                        // mark off courses that use this description block.
                        if (validateInfo.usedDescriptionCourses.ContainsKey(courseDesignator))
                            throw new ApplicationException(string.Format("{0} has multiple descriptions (special {1})", courseDesignator, id));
                        if (! courseDesignator.AllParts && validateInfo.usedDescriptionCourses.ContainsKey(new CourseDesignator(courseDesignator.CourseId)))
                            throw new ApplicationException(string.Format("{0} has conflict with all parts description (special {1})", courseDesignator, id));
                        validateInfo.usedDescriptionCourses[courseDesignator] = true;
                    }
                }
            }
        }

        public override StorableObject Clone()
        {
            Special n = (Special) base.Clone();
            if (courses != null) {
                n.courses = Util.CloneArrayAndElements(n.courses);
            }
            n.locations = (PointF[]) n.locations.Clone();
            return n;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Special))
                return false;

            Special other = (Special) obj;

            if (other.kind != kind)
                return false;
            if (other.orientation != orientation)
                return false;
            if (other.allCourses != allCourses)
                return false;
            if (other.text != text)
                return false;
            if (other.fontName != fontName)
                return false;
            if (other.fontBold != fontBold)
                return false;
            if (other.fontItalic != fontItalic)
                return false;
            if (other.numColumns != numColumns)
                return false;
            if ((other.courses == null || courses == null)) {
                if (other.courses != courses)
                    return false;
            }
            else if (other.courses.Length != courses.Length)
                return false;
            else {
                for (int i = 0; i < courses.Length; ++i)
                    if (other.courses[i] != courses[i])
                        return false;
            }
            if (other.locations.Length != locations.Length)
                return false;
            else {
                for (int i = 0; i < locations.Length; ++i)
                    if (other.locations[i] != locations[i])
                        return false;
            }

            return true;
        }

        public override void ReadAttributesAndContent(XmlInput xmlinput)
        {
            string kindText = xmlinput.GetAttributeString("kind");
            switch (kindText) {
            case "first-aid": kind = SpecialKind.FirstAid; break;
            case "water": kind = SpecialKind.Water; break;
            case "optional-crossing-point": kind = SpecialKind.OptCrossing; break;
            case "forbidden-route": kind = SpecialKind.Forbidden; break;
            case "registration-mark": kind = SpecialKind.RegMark; break;
            case "boundary": kind = SpecialKind.Boundary; break;
            case "out-of-bounds": kind = SpecialKind.OOB; break;
            case "dangerous-area": kind = SpecialKind.Dangerous; break;
            case "white-out": kind = SpecialKind.WhiteOut; break;
            case "text": kind = SpecialKind.Text; break;
            case "descriptions": kind = SpecialKind.Descriptions; break;
            default: xmlinput.BadXml("Invalid special-object kind '{0}'", kindText); break;
            }

            if (kind == SpecialKind.OptCrossing)
                orientation = xmlinput.GetAttributeFloat("orientation");

            text = null;
            locations = null;
            allCourses = true;
            courses = null;
            List<PointF> locationList = new List<PointF>();

            bool first = true;
            while (xmlinput.FindSubElement(first, "text", "font", "location", "appearance", "courses")) {
                switch (xmlinput.Name) {
                case "text":
                    text = xmlinput.GetContentString();
                    break;

                case "font":
                    fontName = xmlinput.GetAttributeString("name");
                    fontBold = xmlinput.GetAttributeBool("bold");
                    fontItalic = xmlinput.GetAttributeBool("italic");
                    xmlinput.Skip();
                    break;

                case "location":
                    float x = xmlinput.GetAttributeFloat("x");
                    float y = xmlinput.GetAttributeFloat("y");
                    locationList.Add(new PointF(x, y));
                    xmlinput.Skip();
                    break;

                case "appearance":
                    numColumns = xmlinput.GetAttributeInt("columns", 1);
                    xmlinput.Skip();
                    break;

                case "courses":
                    allCourses = xmlinput.GetAttributeBool("all", false);
                    if (!allCourses) {
                        List<CourseDesignator> courseIdList = new List<CourseDesignator>();
                        xmlinput.MoveToContent();

                        bool firstCourse = true;
                        while (xmlinput.FindSubElement(firstCourse, "course")) {
                            int id = xmlinput.GetAttributeInt("course");
                            int part = xmlinput.GetAttributeInt("part", -1);
                            if (part >= 0)
                                courseIdList.Add(new CourseDesignator(new Id<Course>(id), part));
                            else
                                courseIdList.Add(new CourseDesignator(new Id<Course>(id)));
                            xmlinput.Skip();
                            firstCourse = false;
                        }

                        courses = courseIdList.ToArray();
                    }
                    else {
                        xmlinput.Skip();
                    }
                    break;
                }

                first = false;
            }

            if (locationList.Count == 0)
                xmlinput.BadXml("missing 'location' element");
            if ((kind == SpecialKind.Text) && fontName == null)
                xmlinput.BadXml("missing 'font' element");
            locations = locationList.ToArray();
        }

        public override void WriteAttributesAndContent(System.Xml.XmlTextWriter xmloutput)
        {
            // Write attributes

            string kindText;
            switch (kind) {
            case SpecialKind.FirstAid: kindText = "first-aid"; break;
            case SpecialKind.Water: kindText = "water"; break;
            case SpecialKind.OptCrossing: kindText = "optional-crossing-point"; break;
            case SpecialKind.RegMark: kindText = "registration-mark"; break;
            case SpecialKind.Forbidden: kindText = "forbidden-route"; break;
            case SpecialKind.Boundary: kindText = "boundary"; break;
            case SpecialKind.OOB: kindText = "out-of-bounds"; break;
            case SpecialKind.Dangerous: kindText = "dangerous-area"; break;
            case SpecialKind.WhiteOut: kindText = "white-out"; break;
            case SpecialKind.Text: kindText = "text"; break;
            case SpecialKind.Descriptions: kindText = "descriptions"; break;
            default:
                Debug.Fail("bad kind"); kindText = "none";  break;
            }

            xmloutput.WriteAttributeString("kind", kindText);

            if (kind == SpecialKind.OptCrossing)
                xmloutput.WriteAttributeString("orientation", XmlConvert.ToString(orientation));

            // Write sub-elements
            if (text != null) {
                xmloutput.WriteElementString("text", text);
            }

            if (kind == SpecialKind.Descriptions && numColumns > 1) {
                xmloutput.WriteStartElement("appearance");
                xmloutput.WriteAttributeString("columns", XmlConvert.ToString(numColumns));
                xmloutput.WriteEndElement();
            }

            if (kind == SpecialKind.Text) {
                xmloutput.WriteStartElement("font");
                xmloutput.WriteAttributeString("name", fontName);
                xmloutput.WriteAttributeString("bold", XmlConvert.ToString(fontBold));
                xmloutput.WriteAttributeString("italic", XmlConvert.ToString(fontItalic));
                xmloutput.WriteEndElement();
            }

            // Write locations
            foreach (PointF location in locations) {
                xmloutput.WriteStartElement("location");
                xmloutput.WriteAttributeString("x", XmlConvert.ToString(location.X));
                xmloutput.WriteAttributeString("y", XmlConvert.ToString(location.Y));
                xmloutput.WriteEndElement();
            }

            // write courses
            xmloutput.WriteStartElement("courses");
            if (allCourses) {
                xmloutput.WriteAttributeString("all", XmlConvert.ToString(true));
            }
            else {
                foreach (CourseDesignator courseDesignator in courses) {
                    xmloutput.WriteStartElement("course");
                    xmloutput.WriteAttributeString("course", XmlConvert.ToString(courseDesignator.CourseId.id));
                    if (!courseDesignator.AllParts)
                        xmloutput.WriteAttributeString("part", XmlConvert.ToString(courseDesignator.Part));
                    xmloutput.WriteEndElement();
                }
            }

            xmloutput.WriteEndElement();
        }

        public override string ElementName
        {
            get { return "special-object"; }
        }
    }

    public enum FlaggingKind {
        None,               // no flagging
        All,                    // all flagged
        Begin,              // beginning part of the leg is flagged
        End                  // end of the leg is flagged
    };

    // A leg describes the leg between two controls. Not all legs have a leg object; it is 
    // used only if the leg is all or partly flagged, has bends, or has gaps.
    public class Leg: StorableObject
    {
        public Id<ControlPoint> controlId1, controlId2;       // start and end of the leg.
        public FlaggingKind flagging;   // what kind of flagging.
        public PointF flagStartStop;     // start or stop of the flagging, if flagging kind is begin or end. This point is always in the bend array too.
        public PointF[] bends;              // bends in the leg.
        public LegGap[] gaps;              // list of gaps in the leg (null if no gaps)

        public Leg()
        {
        }

        public Leg(Id<ControlPoint> controlId1, Id<ControlPoint> controlId2)
        {
            this.controlId1 = controlId1;
            this.controlId2 = controlId2;
        }

        // Is the leg vacuous (same as no leg object)?
        public bool IsVacuous()
        {
            return (flagging == FlaggingKind.None && bends == null && gaps == null);
        }

        // Validate this leg.
        public void Validate(Id<Leg> id, EventDB.ValidateInfo validateInfo)
        {
            if (controlId1.IsNone || controlId2.IsNone)
                throw new ApplicationException(string.Format("Leg {0} must have valid start/end controls", id));

            validateInfo.eventDB.CheckControlId(controlId1);
            validateInfo.eventDB.CheckControlId(controlId2);

            if (flagging == FlaggingKind.Begin || flagging == FlaggingKind.End) {
                if (bends == null || bends.Length == 0 || Array.IndexOf(bends, flagStartStop) < 0) {
                    throw new ApplicationException(string.Format("Leg {0} has a flagStartStop that isn't in the bends array", id));
                }
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Leg))
                return false;

            Leg other = (Leg) obj;

            if (other.controlId1 != controlId1)
                return false;
            if (other.controlId2 != controlId2)
                return false;
            if (other.flagging != flagging)
                return false;
            if ((flagging == FlaggingKind.Begin || flagging == FlaggingKind.End) && other.flagStartStop != flagStartStop)
                return false;

            if (bends != null) {
                if (other.bends == null)
                    return false;
                if (bends.Length != other.bends.Length)
                    return false;
                for (int i = 0; i < bends.Length; ++i)
                    if (other.bends[i] != bends[i])
                        return false;
            }
            else {
                if (other.bends != null)
                    return false;
            }

            if (gaps != null) {
                if (other.gaps == null)
                    return false;
                if (gaps.Length != other.gaps.Length)
                    return false;
                for (int i = 0; i < gaps.Length; ++i)
                    if (other.gaps[i] != gaps[i])
                        return false;
            }
            else {
                if (other.gaps != null)
                    return false;
            }

            return true;
        }

        public override StorableObject Clone()
        {
            Leg l = (Leg) base.Clone();

            if (l.bends != null)
                l.bends = (PointF[]) l.bends.Clone();
            if (l.gaps != null)
                l.gaps = (LegGap[]) l.gaps.Clone();

            return l;
        }

        public override void ReadAttributesAndContent(XmlInput xmlinput)
        {
            controlId1 = new Id<ControlPoint>(xmlinput.GetAttributeInt("start-control"));
            controlId2 = new Id<ControlPoint>(xmlinput.GetAttributeInt("end-control"));

            bool first = true;
            while (xmlinput.FindSubElement(first, "flagging", "bends", "gaps")) {
                switch (xmlinput.Name) {
                case "flagging":
                    string flagKind = xmlinput.GetAttributeString("kind");
                    switch (flagKind) {
                    case "none": flagging = FlaggingKind.None; break;
                    case "beginning-part": flagging = FlaggingKind.Begin; break;
                    case "end-part": flagging = FlaggingKind.End; break;
                    case "all": flagging = FlaggingKind.All; break;
                    default: xmlinput.BadXml("Invalid flagging kind '{0}'", flagKind); break;
                    }

                    if (flagging == FlaggingKind.Begin || flagging == FlaggingKind.End) {
                        float x = xmlinput.GetAttributeFloat("x");
                        float y = xmlinput.GetAttributeFloat("y");
                        flagStartStop = new PointF(x, y);
                    }

                    xmlinput.Skip();

                    break;

                case "bends":
                    bool firstBend = true;
                    List<PointF> locationList = new List<PointF>();
                    while (xmlinput.FindSubElement(firstBend, "location")) {
                        float x = xmlinput.GetAttributeFloat("x");
                        float y = xmlinput.GetAttributeFloat("y");
                        locationList.Add(new PointF(x, y));
                        xmlinput.Skip();
                        firstBend = false;
                    }

                    bends = locationList.ToArray();

                    break;

                case "gaps":
                    bool firstGap = true;
                    List<LegGap> gapsList = new List<LegGap>();
                    while (xmlinput.FindSubElement(firstGap, "gap")) {
                        float start = xmlinput.GetAttributeFloat("start");
                        float length = xmlinput.GetAttributeFloat("length");
                        gapsList.Add(new LegGap(start, length));
                        xmlinput.Skip();
                        firstGap = false;
                    }

                    gaps = gapsList.ToArray();

                    break;
                }

                first = false;
            }
        }

        public override void WriteAttributesAndContent(XmlTextWriter xmloutput)
        {
            // Write attributes
            xmloutput.WriteAttributeString("start-control", XmlConvert.ToString(controlId1.id));
            xmloutput.WriteAttributeString("end-control", XmlConvert.ToString(controlId2.id));

            // Write elements
            if (flagging != FlaggingKind.None) {
                // Flagging element. -- what kind of flagging and start/stop point.
                string flagKind;
                switch (flagging) {
                case FlaggingKind.Begin: flagKind = "beginning-part"; break;
                case FlaggingKind.End: flagKind = "end-part"; break;
                case FlaggingKind.All: flagKind = "all"; break;
                default: Debug.Fail("bad flagging kind"); flagKind = ""; break;
                }

                xmloutput.WriteStartElement("flagging");
                xmloutput.WriteAttributeString("kind", flagKind);
                if (flagging != FlaggingKind.All) {
                    xmloutput.WriteAttributeString("x", XmlConvert.ToString(flagStartStop.X));
                    xmloutput.WriteAttributeString("y", XmlConvert.ToString(flagStartStop.Y));
                }
                xmloutput.WriteEndElement();
            }

            if (bends != null && bends.Length > 0) {
                // Bends
                xmloutput.WriteStartElement("bends");

                foreach (PointF location in bends) {
                    xmloutput.WriteStartElement("location");
                    xmloutput.WriteAttributeString("x", XmlConvert.ToString(location.X));
                    xmloutput.WriteAttributeString("y", XmlConvert.ToString(location.Y));
                    xmloutput.WriteEndElement();
                }

                xmloutput.WriteEndElement();
            }

            if (gaps != null && gaps.Length > 0) {
                // Gaps
                xmloutput.WriteStartElement("gaps");

                for (int i = 0; i < gaps.Length; ++i) {
                    xmloutput.WriteStartElement("gap");
                    xmloutput.WriteAttributeString("start", XmlConvert.ToString(gaps[i].distanceFromStart));
                    xmloutput.WriteAttributeString("length", XmlConvert.ToString(gaps[i].length));
                    xmloutput.WriteEndElement();
                }

                xmloutput.WriteEndElement();
            }
        }

        public override string ElementName
        {
            get { return "leg"; }
        }

    }

    // The type of map used.
    public enum MapType { None, OCAD, Bitmap, PDF };

    // Describes appearance of the courses.
    public class CourseAppearance
    {
        public float controlCircleSize = 1.0F;            // ratio to apply to control circles and other point features.
        public float lineWidth = 1.0F;                       // ratio to apply to the width of lines
        public float numberHeight = 1.0F;                // ratio to apply to the size of control numbers
        public float centerDotDiameter = 0.0F;            // center dot diameter, or 0 for no center dot.
        public bool numberBold = false;                 // Is the number bolded?
        public float numberOutlineWidth = 0.0F;             // Width of outline

        public bool useDefaultPurple = true;        // if true, use the default purple color (which usually comes from the underlying map)
        public float purpleC, purpleM, purpleY, purpleK;   // CMYK coloir of the purple color to use if "useDefaultPurple" is false

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is CourseAppearance))
                return false;

            CourseAppearance other = (CourseAppearance) obj;

            if (controlCircleSize != other.controlCircleSize)
                return false;
            if (lineWidth != other.lineWidth)
                return false;
            if (centerDotDiameter != other.centerDotDiameter)
                return false;
            if (numberHeight != other.numberHeight)
                return false;
            if (numberBold != other.numberBold)
                return false;
            if (numberOutlineWidth != other.numberOutlineWidth)
                return false;
            if (useDefaultPurple != other.useDefaultPurple)
                return false;
            if (useDefaultPurple == false) {
                // The specific purple colors are not used if useDefaultPurple is false.
                if (purpleC != other.purpleC)
                    return false;
                if (purpleM != other.purpleM)
                    return false;
                if (purpleY != other.purpleY)
                    return false;
                if (purpleK != other.purpleK)
                    return false;
            }

            return true;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
    
    // Describes the entire event. Only one of these should ever be in the event DB at a 
    // time. If none are, a default one is returned.
    public class Event: StorableObject
    {
        public string title;            // title of event. Shows in the title line. Can't be null.
        public string notes;            // notes. May be null.
        public MapType mapType;         // type of name.
        public string mapFileName;      // full file name of map, relativized on save.
        public float mapScale;              // scale of the map (15000 means 1:15000)
        public float mapDpi;                 // dpi of a bitmap map.
        public float allControlsPrintScale; // scale to print all controls at
        public DescriptionKind allControlsDescKind;  // description kind for all controls.
        public RectangleF printArea;    // print area for all controls, or empty if none defined.
        public int firstControlCode;      // initial control code to use (default: 31)
        public bool disallowInvertibleCodes;   // disallow codes that are invertable (e.g., 161).
        public bool ignoreMissingFonts;   // If true, don't warn about missing fonts in the map.
        public PunchcardFormat punchcardFormat = new PunchcardFormat();   // format of punch cards
        public CourseAppearance courseAppearance = new CourseAppearance();   // appearance of courses.
        public string descriptionLangId;   // language id for descriptions.
        public Dictionary<string, List<SymbolText>> customSymbolText = new Dictionary<string, List<SymbolText>>();   // maps symbol IDs to list of custom symbol text.
        public Dictionary<string, bool> customSymbolKey = new Dictionary<string, bool>();   // maps symbol IDs to whether to display key for this custom symbol

        public Event()
        {
            title = "";
            descriptionLangId = "en";
        }

        public void Validate(Id<Event> id, EventDB.ValidateInfo validateInfo)
        {
            if (title == null)
                throw new ApplicationException(string.Format("Event '{0}' should have a title", id));
            if (! Enum.IsDefined(typeof(MapType), mapType))
                throw new ApplicationException(string.Format("Event '{0}' has bad map type", id));
            if (mapType == MapType.Bitmap && mapDpi <= 0)
                throw new ApplicationException(string.Format("Event '{0}' has bad dpi", id));
            if (mapScale <= 0)
                throw new ApplicationException(string.Format("Event '{0}' has bad map scale", id));
            if (allControlsPrintScale <= 0)
                throw new ApplicationException(string.Format("Event '{0}' has bad allControlsPrintScale", id));
            if (string.IsNullOrEmpty(descriptionLangId))
                throw new ApplicationException(string.Format("Event '{0}' has bad description language", id));

            if (customSymbolKey.Count != customSymbolText.Count)
                throw new ApplicationException(string.Format("Event '{0}' has inconsistent custom symbols"));
            foreach (string s in customSymbolText.Keys)
                if (! customSymbolKey.ContainsKey(s))
                    throw new ApplicationException(string.Format("Event '{0}' has inconsistent custom symbols"));
        }

        public override StorableObject Clone()
        {
            Event ev = (Event) base.Clone();

            // Copy custom symbol text and clone the list of texts associated with in.
            if (customSymbolText == null)
                ev.customSymbolText = null;
            else {
                ev.customSymbolText = new Dictionary<string, List<SymbolText>>();
                foreach (var pair in customSymbolText) {
                    List<SymbolText> newList = new List<SymbolText>();
                    foreach (SymbolText text in pair.Value)
                        newList.Add(text.Clone());
                    ev.customSymbolText.Add(pair.Key, newList);
                }
            }

            ev.customSymbolKey = Util.CopyDictionary(customSymbolKey);
            ev.punchcardFormat = (PunchcardFormat) punchcardFormat.Clone();
            ev.courseAppearance = (CourseAppearance) courseAppearance.Clone();
            return ev;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Event))
                return false;

            Event other = (Event)obj;

            if (other.title != title)
                return false;
            if (other.notes != notes)
                return false;
            if (other.mapType != mapType)
                return false;
            if (other.mapScale != mapScale)
                return false;
            if (mapType == MapType.Bitmap && other.mapDpi != mapDpi)
                return false;
            if (other.allControlsPrintScale != allControlsPrintScale)
                return false;
            if (other.allControlsDescKind != allControlsDescKind)
                return false;
            if (! string.Equals(other.mapFileName, mapFileName, StringComparison.OrdinalIgnoreCase))
                return false;
            if (firstControlCode != other.firstControlCode)
                return false;
            if (disallowInvertibleCodes != other.disallowInvertibleCodes)
                return false;
            if (ignoreMissingFonts != other.ignoreMissingFonts)
                return false;
            if (!object.Equals(punchcardFormat, other.punchcardFormat))
                return false;
            if (!object.Equals(courseAppearance, other.courseAppearance))
                return false;
            if (other.printArea != printArea)
                return false;
            if (other.descriptionLangId != descriptionLangId)
                return false;

            if (customSymbolText.Count != other.customSymbolText.Count)
                return false;
            foreach (string s in customSymbolText.Keys) {
                if (!other.customSymbolText.ContainsKey(s))
                    return false;
                List<SymbolText> l1 = other.customSymbolText[s], l2 = customSymbolText[s];
                if (l1.Count != l2.Count)
                    return false;
                for (int i = 0; i < l1.Count; ++i) {
                    if (! l1[i].Equals(l2[i]))
                        return false;
                }
            }

            if (customSymbolKey.Count != other.customSymbolKey.Count)
                return false;
            foreach (string s in customSymbolKey.Keys) {
                if (!other.customSymbolKey.ContainsKey(s) || other.customSymbolKey[s] != customSymbolKey[s])
                    return false;
            }

            return true;
        }

        public override void WriteAttributesAndContent(System.Xml.XmlTextWriter xmloutput)
        {
            // No attributes.

            // Write sub-elements.
            xmloutput.WriteElementString("title", title);

            if (notes != null)
                xmloutput.WriteElementString("notes", notes);

            xmloutput.WriteStartElement("map");

            if (mapType == MapType.None)
                xmloutput.WriteAttributeString("kind", "none");
            else if (mapType == MapType.OCAD)
                xmloutput.WriteAttributeString("kind", "OCAD");
            else if (mapType == MapType.Bitmap)
                xmloutput.WriteAttributeString("kind", "bitmap");
            else if (mapType == MapType.PDF)
                xmloutput.WriteAttributeString("kind", "PDF");
            else
                Debug.Fail("Unknown map kind");

            xmloutput.WriteAttributeString("scale", XmlConvert.ToString(mapScale));

            if (mapType == MapType.Bitmap)
                xmloutput.WriteAttributeString("dpi", XmlConvert.ToString(mapDpi));

            if (mapType == MapType.OCAD)
                xmloutput.WriteAttributeString("ignore-missing-fonts", XmlConvert.ToString(ignoreMissingFonts));

            if (mapType != MapType.None)
                xmloutput.WriteString(Util.GetRelativeFileName(xmloutput, mapFileName));

            xmloutput.WriteEndElement();

            // options for all controls view.
            xmloutput.WriteStartElement("all-controls");
            xmloutput.WriteAttributeString("print-scale", XmlConvert.ToString(allControlsPrintScale));
            EventDBUtil.WriteDescriptionKindAttribute(xmloutput, allControlsDescKind);
            xmloutput.WriteEndElement();

            if (!printArea.IsEmpty) {
                xmloutput.WriteStartElement("print-area");
                xmloutput.WriteAttributeString("left", XmlConvert.ToString(printArea.Left));
                xmloutput.WriteAttributeString("top", XmlConvert.ToString(printArea.Bottom));  // rectangle is reversed, so top is really the bottom and vice versa
                xmloutput.WriteAttributeString("right", XmlConvert.ToString(printArea.Right));
                xmloutput.WriteAttributeString("bottom", XmlConvert.ToString(printArea.Top));   // rectangle is reversed, so top is really the bottom and vice versa
                xmloutput.WriteEndElement();
            }

            xmloutput.WriteStartElement("numbering");
            xmloutput.WriteAttributeString("start", XmlConvert.ToString(firstControlCode));
            xmloutput.WriteAttributeString("disallow-invertible", XmlConvert.ToString(disallowInvertibleCodes));
            xmloutput.WriteEndElement();

            xmloutput.WriteStartElement("punch-card");
            xmloutput.WriteAttributeString("rows", XmlConvert.ToString(punchcardFormat.boxesDown));
            xmloutput.WriteAttributeString("columns", XmlConvert.ToString(punchcardFormat.boxesAcross));
            xmloutput.WriteAttributeString("left-to-right", XmlConvert.ToString(punchcardFormat.leftToRight));
            xmloutput.WriteAttributeString("top-to-bottom", XmlConvert.ToString(punchcardFormat.topToBottom));
            xmloutput.WriteEndElement();

            xmloutput.WriteStartElement("course-appearance");
            if (courseAppearance.controlCircleSize != 1.0F)
                xmloutput.WriteAttributeString("control-circle-size-ratio", XmlConvert.ToString(courseAppearance.controlCircleSize));
            if (courseAppearance.lineWidth != 1.0F)
                xmloutput.WriteAttributeString("line-width-ratio", XmlConvert.ToString(courseAppearance.lineWidth));
            if (courseAppearance.centerDotDiameter != 0.0F) 
                xmloutput.WriteAttributeString("center-dot-diameter", XmlConvert.ToString(courseAppearance.centerDotDiameter));
            if (courseAppearance.numberHeight != 1.0F)
                xmloutput.WriteAttributeString("number-size-ratio", XmlConvert.ToString(courseAppearance.numberHeight));
            if (courseAppearance.numberBold)
                xmloutput.WriteAttributeString("number-bold", XmlConvert.ToString(courseAppearance.numberBold));
            if (courseAppearance.numberOutlineWidth > 0)
                xmloutput.WriteAttributeString("number-outline-width", XmlConvert.ToString(courseAppearance.numberOutlineWidth));
            if (courseAppearance.useDefaultPurple == false) {
                xmloutput.WriteAttributeString("purple-cyan", XmlConvert.ToString(courseAppearance.purpleC));
                xmloutput.WriteAttributeString("purple-magenta", XmlConvert.ToString(courseAppearance.purpleM));
                xmloutput.WriteAttributeString("purple-yellow", XmlConvert.ToString(courseAppearance.purpleY));
                xmloutput.WriteAttributeString("purple-black", XmlConvert.ToString(courseAppearance.purpleK));
            }
            xmloutput.WriteEndElement();

            xmloutput.WriteStartElement("descriptions");
            xmloutput.WriteAttributeString("lang", descriptionLangId);
            xmloutput.WriteEndElement();

            foreach (string iofId in customSymbolText.Keys) {
                xmloutput.WriteStartElement("custom-symbol-text");
                xmloutput.WriteAttributeString("iof-2004-ref", iofId);
                xmloutput.WriteAttributeString("show-key", XmlConvert.ToString(customSymbolKey[iofId]));
                foreach (SymbolText text in customSymbolText[iofId]) 
                    text.WriteXml(xmloutput);
                xmloutput.WriteEndElement();
            }
        }

        public override void ReadAttributesAndContent(XmlInput xmlinput)
        {
            firstControlCode = 31;
            disallowInvertibleCodes = true;

            bool first = true;
            while (xmlinput.FindSubElement(first, "title", "notes", "map", "all-controls", "numbering", "punch-card", "course-appearance", "print-area", "descriptions", "custom-symbol-text")) {
                switch (xmlinput.Name) {
                    case "title":
                        title = xmlinput.GetContentString();
                        break;

                    case "notes":
                        notes = xmlinput.GetContentString();
                        break;

                    case "map":
                        mapScale = xmlinput.GetAttributeFloat("scale");

                        string kindString = xmlinput.GetAttributeString("kind");
                        switch (kindString) {
                            case "none": mapType = MapType.None; break;
                            case "OCAD": mapType = MapType.OCAD; break;
                            case "bitmap": mapType = MapType.Bitmap; break;
                            case "PDF": mapType = MapType.PDF; break;
                            default: xmlinput.BadXml("Invalid map kind '{0}'", kindString); break;
                        }

                        if (mapType == MapType.Bitmap)
                            mapDpi = xmlinput.GetAttributeFloat("dpi");
                        else
                            mapDpi = 0;

                        if (mapType == MapType.OCAD)
                            ignoreMissingFonts = xmlinput.GetAttributeBool("ignore-missing-fonts", false);

                        if (mapType != MapType.None) {
                            mapFileName = xmlinput.GetContentString();
                            mapFileName = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(xmlinput.FileName), mapFileName)); // file name is relative to the XML file
                        }
                        else {
                            mapFileName = null;
                            xmlinput.Skip();
                        }

                        break;

                    case "all-controls":
                        allControlsPrintScale = xmlinput.GetAttributeFloat("print-scale", 0);
                        allControlsDescKind = EventDBUtil.ReadDescriptionKindAttribute(xmlinput);
                        xmlinput.Skip();
                        break;

                    case "numbering":
                        firstControlCode = xmlinput.GetAttributeInt("start", 31);
                        disallowInvertibleCodes = xmlinput.GetAttributeBool("disallow-invertible", true);
                        xmlinput.Skip();
                        break;

                    case "punch-card":
                        punchcardFormat.boxesDown = xmlinput.GetAttributeInt("rows", PunchcardAppearance.defaultBoxesDown);
                        punchcardFormat.boxesAcross = xmlinput.GetAttributeInt("columns", PunchcardAppearance.defaultBoxesAcross);
                        punchcardFormat.leftToRight = xmlinput.GetAttributeBool("left-to-right", PunchcardAppearance.defaultLeftToRight);
                        punchcardFormat.topToBottom = xmlinput.GetAttributeBool("top-to-bottom", PunchcardAppearance.defaultTopToBottom);
                        xmlinput.Skip();
                        break;

                    case "course-appearance":
                        courseAppearance.controlCircleSize = xmlinput.GetAttributeFloat("control-circle-size-ratio", 1.0F);
                        courseAppearance.lineWidth = xmlinput.GetAttributeFloat("line-width-ratio", 1.0F);
                        courseAppearance.centerDotDiameter = xmlinput.GetAttributeFloat("center-dot-diameter", 0.0F);
                        courseAppearance.numberHeight = xmlinput.GetAttributeFloat("number-size-ratio", 1.0F);
                        courseAppearance.numberBold = xmlinput.GetAttributeBool("number-bold", false);
                        courseAppearance.numberOutlineWidth = xmlinput.GetAttributeFloat("number-outline-width", 0.0F);
                        courseAppearance.purpleC = xmlinput.GetAttributeFloat("purple-cyan", -1F);
                        courseAppearance.purpleM = xmlinput.GetAttributeFloat("purple-magenta", -1F);
                        courseAppearance.purpleY = xmlinput.GetAttributeFloat("purple-yellow", -1F);
                        courseAppearance.purpleK = xmlinput.GetAttributeFloat("purple-black", -1F);
                        if (courseAppearance.purpleC < 0 || courseAppearance.purpleM < 0 || courseAppearance.purpleY < 0 || courseAppearance.purpleK < 0) {
                            courseAppearance.useDefaultPurple = true;
                            courseAppearance.purpleC = courseAppearance.purpleM = courseAppearance.purpleY = courseAppearance.purpleK = 1;
                        }
                        else {
                            courseAppearance.useDefaultPurple = false;
                        }

                        xmlinput.Skip();
                        break;

                    case "print-area":
                        float left = xmlinput.GetAttributeFloat("left");
                        float top = xmlinput.GetAttributeFloat("top");
                        float right = xmlinput.GetAttributeFloat("right");
                        float bottom = xmlinput.GetAttributeFloat("bottom");
                        printArea = RectangleF.FromLTRB(left, bottom, right, top);   // top and bottom reverse due to map orientation.
                        xmlinput.Skip();
                        break;

                    case "descriptions":
                        descriptionLangId = xmlinput.GetAttributeString("lang");
                        xmlinput.Skip();
                        break;

                    case "custom-symbol-text":
                        string iof2004id = xmlinput.GetAttributeString("iof-2004-ref");
                        bool key = xmlinput.GetAttributeBool("show-key", false);
                        customSymbolKey[iof2004id] = key;
                        xmlinput.Read();
                        xmlinput.MoveToContent();

                        List<SymbolText> texts = new List<SymbolText>();
                        if (xmlinput.Reader.NodeType == XmlNodeType.Text) {
                            // Reading the old-style custom symbol text.
                            string customText = xmlinput.Reader.ReadString();
                            if (iof2004id.StartsWith("8."))
                                customText += " {0}";     // old style for modifiers didn't have the fill-in placeholder.
                            SymbolText text = new SymbolText();
                            text.Lang = "en";
                            text.Plural = false;
                            text.Gender = "";
                            text.Text = customText;
                            texts.Add(text);
                        }
                        else {
                            // Read the new-style custom symbol text.
                            while (xmlinput.Reader.NodeType == XmlNodeType.Element && xmlinput.Name == "text") {
                                SymbolText text = new SymbolText();
                                text.ReadXml(xmlinput);
                                texts.Add(text);
                                xmlinput.Skip();
                            }
                        }
                        
                        customSymbolText[iof2004id] = texts;
                        xmlinput.Skip();
                        break;
                        
                }

                first = false;
            }

            if (allControlsPrintScale == 0)
                allControlsPrintScale = mapScale;
        }


        public override string ElementName
        {
            get { return "event"; }
        }
    }

    // The event DB controls the entire persistant state of the event. It uses ObjectStore to actual
    // store particular kinds of objects.
    public class EventDB
    {
        const string rootElement = "course-scribe-event";

        // The object stores. If a new one is added, be sure to change ChangeNum,
        // Load, Save, and Validate methods appropriately.
        ObjectStore<ControlPoint> controlPointStore;
        ObjectStore<Course> courseStore;
        ObjectStore<CourseControl> courseControlStore;
        ObjectStore<Event> eventStore;
        ObjectStore<Leg> legStore;
        ObjectStore<Special> specialStore;

        long random;        // A random long number added to the change numbers, so that
                            // different event DBs have different change numbers.

        static Random rand = new Random();

        public EventDB(UndoMgr undomgr)
        {
            controlPointStore = new ObjectStore<ControlPoint>(undomgr);
            courseStore = new ObjectStore<Course>(undomgr);
            courseControlStore = new ObjectStore<CourseControl>(undomgr);
            eventStore = new ObjectStore<Event>(undomgr);
            specialStore = new ObjectStore<Special>(undomgr);
            legStore = new ObjectStore<Leg>(undomgr);

            random = (long) (rand.NextDouble() * long.MaxValue / 2);
        }

        // Returns a change number for the event database. It changes every time a change is 
        // made to the database, so it is a cheap way to detect if any changes have been made.
        public long ChangeNum
        {
            get
            {
                return random + controlPointStore.ChangeNum + courseStore.ChangeNum +
                    courseControlStore.ChangeNum + eventStore.ChangeNum + specialStore.ChangeNum + legStore.ChangeNum;
            }
        }

        public ICollection<ControlPoint> AllControlPoints
        {
            get { return controlPointStore.All; }
        }

        public ICollection<Id<ControlPoint>> AllControlPointIds
        {
            get { return controlPointStore.AllIds; }
        }

        public IEnumerable<KeyValuePair<Id<ControlPoint>,ControlPoint>> AllControlPointPairs
        {
            get { return controlPointStore.AllPairs; }
        }

        public ICollection<Course> AllCourses
        {
            get { return courseStore.All; }
        }

        public ICollection<Id<Course>> AllCourseIds
        {
            get { return courseStore.AllIds; }
        }

        public IEnumerable<KeyValuePair<Id<Course>, Course>> AllCoursePairs
        {
            get { return courseStore.AllPairs; }
        }

        public ICollection<CourseControl> AllCourseControls
        {
            get { return courseControlStore.All; }
        }

        public ICollection<Id<CourseControl>> AllCourseControlIds
        {
            get { return courseControlStore.AllIds; }
        }

        public IEnumerable<KeyValuePair<Id<CourseControl>, CourseControl>> AllCourseControlPairs
        {
            get { return courseControlStore.AllPairs; }
        }

        public ICollection<Special> AllSpecials
        {
            get { return specialStore.All; }
        }

        public ICollection<Id<Special>> AllSpecialIds
        {
            get { return specialStore.AllIds; }
        }

        public IEnumerable<KeyValuePair<Id<Special>, Special>> AllSpecialPairs
        {
            get { return specialStore.AllPairs; }
        }

        public ICollection<Leg> AllLegs
        {
            get { return legStore.All; }
        }

        public ICollection<Id<Leg>> AllLegIds
        {
            get { return legStore.AllIds; }
        }

        public IEnumerable<KeyValuePair<Id<Leg>, Leg>> AllLegPairs
        {
            get { return legStore.AllPairs; }
        }

        // There is always only one Event object, and if not present, it
        // is created automatically with default options.
        public Event GetEvent()
        {
            if (!eventStore.IsPresent(new Id<Event>(1))) {
                return new Event();
            }
            else {
                return eventStore[new Id<Event>(1)];
            }
        }

        public void ChangeEvent(Event e)
        {
            if (!eventStore.IsPresent(new Id<Event>(1))) {
                Id<Event> id = eventStore.Add(e);
                Debug.Assert(id.id == 1);
            }
            else {
                eventStore.Replace(new Id<Event>(1), e);
            }
        }

        public Id<ControlPoint> AddControlPoint(ControlPoint control)
        {
            return controlPointStore.Add(control);
        }

        public Id<Course> AddCourse(Course course)
        {
            return courseStore.Add(course);
        }

        public Id<CourseControl> AddCourseControl(CourseControl courseControl)
        {
            return courseControlStore.Add(courseControl);
        }

        public Id<Special> AddSpecial(Special special)
        {
            return specialStore.Add(special);
        }

        public Id<Leg> AddLeg(Leg leg)
        {
            return legStore.Add(leg);
        }

        public void RemoveControlPoint(Id<ControlPoint> id)
        {
            controlPointStore.Remove(id);
        }

        public void RemoveCourse(Id<Course> id)
        {
            courseStore.Remove(id);
        }

        public void RemoveCourseControl(Id<CourseControl> id)
        {
            courseControlStore.Remove(id);
        }

        public void RemoveSpecial(Id<Special> id)
        {
            specialStore.Remove(id);
        }

        public void RemoveLeg(Id<Leg> id)
        {
            legStore.Remove(id);
        }

        public void ReplaceControlPoint(Id<ControlPoint> id, ControlPoint control)
        {
            controlPointStore.Replace(id, control);
        }

        public void ReplaceCourse(Id<Course> id, Course course)
        {
            courseStore.Replace(id, course);
        }

        public void ReplaceCourseControl(Id<CourseControl> id, CourseControl courseControl)
        {
            courseControlStore.Replace(id, courseControl);
        }

        public void ReplaceSpecial(Id<Special> id, Special special)
        {
            specialStore.Replace(id, special);
        }

        public void ReplaceLeg(Id<Leg> id, Leg leg)
        {
            legStore.Replace(id, leg);
        }

        public ControlPoint GetControl(Id<ControlPoint> controlId)
        {
            return controlPointStore[controlId];
        }

        public Course GetCourse(Id<Course> courseId)
        {
            return courseStore[courseId];
        }

        public CourseControl GetCourseControl(Id<CourseControl> courseControlId)
        {
            return courseControlStore[courseControlId];
        }

        public Special GetSpecial(Id<Special> specialId)
        {
            return specialStore[specialId];
        }

        public Leg GetLeg(Id<Leg> legId)
        {
            return legStore[legId];
        }

        public void CheckControlId(Id<ControlPoint> id)
        {
            controlPointStore.CheckPresent(id);
        }

        public void CheckCourseId(Id<Course> id)
        {
            courseStore.CheckPresent(id);
        }

        public void CheckCourseControlId(Id<CourseControl> id)
        {
            courseControlStore.CheckPresent(id);
        }

        public void CheckSpecialId(Id<Special> id)
        {
            specialStore.CheckPresent(id);
        }

        public void CheckLegId(Id<Leg> id)
        {
            legStore.CheckPresent(id);
        }

        public bool IsControlPresent(Id<ControlPoint> id)
        {
            return controlPointStore.IsPresent(id);
        }

        public bool IsCoursePresent(Id<Course> id)
        {
            return courseStore.IsPresent(id);
        }

        public bool IsCourseControlPresent(Id<CourseControl> id)
        {
            return courseControlStore.IsPresent(id);
        }

        public bool IsSpecialPresent(Id<Special> id)
        {
            return specialStore.IsPresent(id);
        }

        public bool IsLegPresent(Id<Leg> id)
        {
            return legStore.IsPresent(id);
        }

        // Older version of purple pen did not have the sort order on courses. If we load a file with any sort orders missing, we assign
        // sort orders by name.
        void FixCourseSortOrders()
        {
            List<Id<Course>> courseIds = new List<Id<Course>>(AllCourseIds);

            if (courseIds.Exists(delegate(Id<Course> courseId) { return GetCourse(courseId).sortOrder <= 0; })) {
                // Some or all course orders are missing. Sort by name.
                courseIds.Sort(delegate(Id<Course> courseId1, Id<Course> courseId2) {
                    string name1 = GetCourse(courseId1).name;
                    string name2 = GetCourse(courseId2).name;
                    return string.Compare(name1, name2, true);
                });

                // Assign course orders. Note that we are modifying course objects directly, which is normally a BAD thing, because is bypasses the 
                // undo manager. But in this case, it is what we want to do because this is a load-time operation which shouldn't be undoable.
                for (int i = 0; i < courseIds.Capacity; ++i)
                    GetCourse(courseIds[i]).sortOrder = i + 1;
            }
        }

        // Older version of purple pen did not store control gaps on a per-scale basis. These are now loaded with a scale of 0. Get all the scales
        // in the event.
        void FixControlPointGaps()
        {
            List<Id<ControlPoint>> controlIds = new List<Id<ControlPoint>>(AllControlPointIds);

            foreach (Id<ControlPoint> controlId in controlIds) {
                ControlPoint control = GetControl(controlId);
                if (control.gaps != null && control.gaps.ContainsKey(0)) {
                    // Fix up these gaps by adding the gap value in each scale being used (map scale and each course scale)
                    // Note that we are modifying control objects directly, which is normally a BAD thing, because is bypasses the 
                    // undo manager. But in this case, it is what we want to do because this is a load-time operation which shouldn't be undoable.
                    CircleGap[] gaps = control.gaps[0];

                    control.gaps.Remove(0);
                    control.gaps[(int) Math.Round(GetEvent().mapScale)] = gaps;

                    foreach (Course course in AllCourses)
                        control.gaps[(int) Math.Round(course.printScale)] = gaps;
                }
            }
        }

        /// <summary>
        /// Validate that items in the event DB are internally consistent.
        /// </summary>
        public void Validate()
        {
            ValidateInfo validateInfo = new ValidateInfo();
            validateInfo.eventDB = this;

            if (eventStore.IsPresent(new Id<Event>(1))) {
                GetEvent().Validate(new Id<Event>(1), validateInfo);
            }

            foreach (Id<ControlPoint> controlId in AllControlPointIds) 
                GetControl(controlId).Validate(controlId, validateInfo);
            foreach (Id<Course> courseId in validateInfo.eventDB.AllCourseIds) 
                GetCourse(courseId).Validate(courseId, validateInfo);
            foreach (Id<CourseControl> courseControlId in AllCourseControlIds)
                GetCourseControl(courseControlId).Validate(courseControlId, validateInfo);
            foreach (Id<Special> specialId in AllSpecialIds)
                GetSpecial(specialId).Validate(specialId, validateInfo);
            foreach (Id<Leg> legId in AllLegIds)
                GetLeg(legId).Validate(legId, validateInfo);
        }


        /// <summary>
        /// Save the entire state of the event DB to a file.
        /// </summary>
        public void Save(string filename)
        {
            using (XmlTextWriter xmloutput = new XmlTextWriter(filename, Encoding.UTF8)) {
                xmloutput.Formatting = Formatting.Indented;
                xmloutput.Namespaces = false;

                xmloutput.WriteStartElement(rootElement);

                eventStore.Save(xmloutput);
                controlPointStore.Save(xmloutput);
                courseStore.Save(xmloutput);
                courseControlStore.Save(xmloutput);
                legStore.Save(xmloutput);
                specialStore.Save(xmloutput);

                xmloutput.WriteEndElement();
            }
        }

        /// <summary>
        /// Load the entire state of the event DB from a file.
        /// </summary>
        public void Load(string filename)
        {
            using (XmlInput xmlinput = new XmlInput(filename)) {
                xmlinput.CheckElement(rootElement);
                xmlinput.Read();

                eventStore.Load(xmlinput);
                controlPointStore.Load(xmlinput);
                courseStore.Load(xmlinput);
                courseControlStore.Load(xmlinput);
                legStore.Load(xmlinput);
                specialStore.Load(xmlinput);

                // Fix backward compatibility issues.
                FixCourseSortOrders();
                FixControlPointGaps();
            }
        }

        // Holds state information for the validation process.
        public class ValidateInfo
        {
            public EventDB eventDB;

            // Remembers which coursd uses which course controls, to make sure there are no duplicates or dangling course controls.
            public Dictionary<Id<CourseControl>, Id<Course>> usedCourseControls = new Dictionary<Id<CourseControl>, Id<Course>>();

            // Remembers used codes to make sure that no codes are used twice.
            public Dictionary<string, bool> usedCodes = new Dictionary<string, bool>();

            // Remembers used courses to make sure that no course has more than one description 
            public Dictionary<CourseDesignator, bool> usedDescriptionCourses = new Dictionary<CourseDesignator, bool>();

            // Remembers used sort orders to make sure that no sort order is used more than once.
            public Dictionary<int, Id<Course>> sortOrders = new Dictionary<int, Id<Course>>();
        }

    }
}
