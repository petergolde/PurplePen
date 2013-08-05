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
using System.Diagnostics;
using System.IO;

namespace PurplePen
{
    // The kinds of lines in a control description sheet.
    public enum DescriptionLineKind
    {
        Title,                                      // A title line. Uses 1 box for the title text.
        SecondaryTitle,                     // A secondary title line. Uses 1 box for the title text.
        Header3Box,                                 // A normal 3-box header line (name, length, climb)
        Header2Box,                                 // A 2-box header line (name, total point or total # controls)
        Normal,                                     // A normal 8-box line
        Directive,                                  // A finish or other directive. 2 boxes -- the finish symbol is box 0, the text (if any) is box 1.
        Text,                                         // An arbitrary text line. Uses 1 box for the text. Text is in box 0. 
        Key                                           // A key line, giving the meaning of a special symbol. Symbol is box 0, meaning (in text) is box 1.
    }

    /// <summary>
    /// Describes one line in a abstract view of a control description sheet.
    /// </summary>
    public class DescriptionLine
    {
        public enum TextLineKind { None, BeforeControl, BeforeCourseControl, AfterControl, AfterCourseControl };

        public DescriptionLineKind kind;            // The kind of description line
        public object[] boxes;                      // The boxes. The number of boxes depends on the kind. Each box is
                                                                // either a Symbol objet or a string object or null.
        public string textual;                      // Textual version of the description line.
        public bool isLeg;                                       // If true, this line describes a leg, else it describes a ControlPoint of some kind (start/finish/crossing/control).
        public TextLineKind textLineKind;        // If a Text line, then which kind?
        public Id<CourseControl> courseControlId;                 // The course control ID, just for editing/selection purposes.
        public Id<CourseControl> courseControlId2;                 // If isLeg is true, the course control Id of the end of the leg, for editing/selection purposes.
        public Id<ControlPoint> controlId;                       // The control ID, just for editing/selection purposes.

        // Determine if this description line is the same as another, primarily so we don't have to repaint it.
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is DescriptionLine))
                return false;

            DescriptionLine other = (DescriptionLine)obj;

            if (other.kind != kind)
                return false;
            if (other.textual != textual)
                return false;
            if (other.isLeg != isLeg)
                return false;
            if (other.courseControlId != courseControlId)
                return false;
            if (isLeg && (other.courseControlId2 != courseControlId2))
                return false;
            if (other.controlId != controlId)
                return false;
            if (other.boxes.Length != boxes.Length)
                return false;
            for (int i = 0; i < boxes.Length; ++i)
                if (!object.Equals(boxes[i], other.boxes[i]))
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            throw new NotSupportedException("The method or operation is not implemented.");
        }
    }

    /// <summary>
    /// The description formatter takes a CourseView, and transforms it into a generic
    /// description sheet -- an array of DescriptionLine objects.
    /// </summary>
    class DescriptionFormatter
    {
        private EventDB eventDB;
        private SymbolDB symbolDB;
        private CourseView courseView;
        private string language;

        // Create a description formatter to format the given courseView.
        // Currently, the language for the text descriptions is taken from the language set for the event. It would be easy to make this
        // a parameter to the constructor (e.g., to allow printing in a different language), but this currently isn't required.
        public DescriptionFormatter(CourseView courseView, SymbolDB symbolDB)
        {
            this.courseView = courseView;
            this.eventDB = courseView.EventDB;
            this.symbolDB = symbolDB;
            this.language = QueryEvent.GetDescriptionLanguage(eventDB);
        }

        // Get the text of the firse (main) title line.
        private string GetTitleLine1()
        {
            return QueryEvent.GetEventTitle(eventDB, "|");
        }

        // Get the text of the second title line, or null if there is no such title line.
        private string GetTitleLine2()
        {
            Id<Course> id = courseView.BaseCourseId;

            if (id.IsNotNone) {
                Course course = eventDB.GetCourse(id);
                return course.secondaryTitle;
            }
            else
                return null;  // Probably all controls. No second line is correct for that.
        }

        // Given the text, create descriptions line for a title line with that text. Lines are split by vertical bars.
        private DescriptionLine[] GetTitleLineFromText(DescriptionLineKind kind, string text)
        {
            string[] texts = text.Split(new char[] { '|' });
            int lineCount = texts.Length;

            DescriptionLine[] lines = new DescriptionLine[lineCount];
            for (int index = 0; index < lineCount; ++index) {
                DescriptionLine line = new DescriptionLine();

                line.kind = kind;
                line.boxes = new object[1];
                line.boxes[0] = texts[index];
                line.textual = texts[index];

                lines[index] = line;
            }

            return lines;
        }

        // Create a description line for a normal header line: name, length, climb
        private DescriptionLine GetNormalHeaderLine()
        {
            DescriptionLine line = new DescriptionLine();

            line.kind = DescriptionLineKind.Header3Box;
            line.boxes = new object[3];
            line.boxes[0] = courseView.CourseNameWithPart;
            line.boxes[1] = string.Format("{0:0.0} km", Math.Round(courseView.TotalLength / 100, MidpointRounding.AwayFromZero) / 10.0);
            if (courseView.TotalClimb < 0) {
                line.boxes[2] = null;
                line.textual = string.Format(symbolDB["course_length"].GetText(language), line.boxes[1]);
            }
            else {
                line.boxes[2] = Convert.ToString(Math.Round(courseView.TotalClimb / 5, MidpointRounding.AwayFromZero) * 5.0) + " m";
                line.textual = string.Format(symbolDB["course_length_climb"].GetText(language), line.boxes[1], line.boxes[2]);
            }

            return line;
        }

        // Get the header line for the All Controls listing.
        private DescriptionLine GetAllControlsHeaderLine()
        {
            DescriptionLine line = new DescriptionLine();

            line.kind = DescriptionLineKind.Header2Box;
            line.boxes = new object[2];
            line.boxes[0] = symbolDB["all_controls"].GetText(language);
            line.boxes[1] = string.Format(symbolDB["number_controls"].GetText(language), courseView.TotalNormalControls);
            line.textual = (string) line.boxes[1];

            return line;
        }

        // Get a description line for the header line for a score course, which contains the total points
        private DescriptionLine GetScoreHeaderLine()
        {
            DescriptionLine line = new DescriptionLine();

            line.kind = DescriptionLineKind.Header2Box;
            line.boxes = new object[2];
            line.boxes[0] = courseView.CourseNameWithPart;

            // If there is scoring, display the total score, else display the total number of controls (e.g.,
            // for a score course with no points for each control.
            if (courseView.TotalScore > 0)
                line.boxes[1] = string.Format(symbolDB["number_points"].GetText(language), courseView.TotalScore);
            else
                line.boxes[1] = string.Format(symbolDB["number_controls"].GetText(language), courseView.TotalNormalControls);

            line.textual = (string)line.boxes[1];

            return line;
        }

        // Given the text, create a text line for that text.
        private DescriptionLine GetTextLineFromText(string text, Id<CourseControl> courseControlId, Id<ControlPoint> controlId, DescriptionLine.TextLineKind textLineKind)
        {
            DescriptionLine line = new DescriptionLine();

            line.kind = DescriptionLineKind.Text;
            line.boxes = new object[1];
            line.boxes[0] = text;
            line.textual = text;
            line.courseControlId = courseControlId;
            line.controlId = controlId;
            line.textLineKind = textLineKind;

            return line;
        }

        // Given some text, a text line for it to a list if the text is non-empty.
        private void AddTextLine(List<DescriptionLine> list, string text, Id<CourseControl> courseControlId, Id<ControlPoint> controlId, DescriptionLine.TextLineKind textLineKind)
        {
            if (!string.IsNullOrEmpty(text)) {
                list.Add(GetTextLineFromText(text, courseControlId, controlId, textLineKind));
            }
        }

        // Get a regular 8-box line for a start or regular control.
        private DescriptionLine GetRegularLine(CourseView.CourseViewKind kind, int scoreColumn, CourseView.ControlView controlView, Dictionary<string, string> descriptionKey)
        {
            Event ev = eventDB.GetEvent();
            ControlPoint control = eventDB.GetControl(controlView.controlId);
            CourseControl courseControl;

            if (controlView.courseControlId.IsNone)
                courseControl = null;
            else
                courseControl = eventDB.GetCourseControl(controlView.courseControlId);

            Debug.Assert(control.kind == ControlPointKind.Normal || control.kind == ControlPointKind.Start || control.kind == ControlPointKind.MapExchange);

            DescriptionLine line = new DescriptionLine();
            line.kind = DescriptionLineKind.Normal;
            line.boxes = new object[8];
            
            // Box A: ordinal or start triangle or points.
            if (control.kind == ControlPointKind.Start || control.kind == ControlPointKind.MapExchange)
                line.boxes[0] = symbolDB["start"];
            else if (kind != CourseView.CourseViewKind.AllControls && controlView.ordinal > 0)
                line.boxes[0] = Convert.ToString(controlView.ordinal);
            else 
                line.boxes[0] = null;

            // Box B: code of the control
            if (control.kind == ControlPointKind.Normal)
                line.boxes[1] = Convert.ToString(control.code);

            // Boxes C-H, from the symbols
            for (int i = 2; i < 8; ++i) {
                String symbolID = control.symbolIds[i - 2];
                if (symbolID != null) {
                    line.boxes[i] = symbolDB[control.symbolIds[i - 2]];

                    // See if we need to add this to the key.
                    bool addToKey;
                    if (ev.customSymbolKey.TryGetValue(symbolID, out addToKey) && addToKey && Symbol.ContainsLanguage(ev.customSymbolText[symbolID], language)) {
                        descriptionKey[symbolID] = Symbol.GetBestSymbolText(ev.customSymbolText[symbolID], language, false, "");
                    }
                }
            }

            // Box F -- may be text instead of a symbol.
            if (control.columnFText != null) {
                Debug.Assert(line.boxes[5] == null);
                line.boxes[5] = control.columnFText;
            }

            // Put points in the score column, for a score course.
            if (control.kind == ControlPointKind.Normal && scoreColumn >= 0 && courseControl != null) {
                int points = courseControl.points;
                if (points > 0)
                    line.boxes[scoreColumn] = Convert.ToString(courseControl.points);
                else
                    line.boxes[scoreColumn] = null;
            }

            // Get the text version of the control using the Textifier.
            Textifier textifier = new Textifier(eventDB, symbolDB, language);
            line.textual = textifier.CreateTextForControl(controlView.controlId, ""); 

            // The course control ID, for use in coordinating the selection
            line.controlId = controlView.controlId;
            line.courseControlId = controlView.courseControlId;

            return line;
        }

        // Get a directive line for a finish or crossingpoint.
        private DescriptionLine GetDirectiveLine(CourseView.CourseViewKind kind, CourseView.ControlView controlView, CourseView.ControlView controlViewPrev)
        {
            ControlPoint control = eventDB.GetControl(controlView.controlId);
            CourseControl courseControl;

            if (controlView.courseControlId.IsNone)
                courseControl = null;
            else
                courseControl = eventDB.GetCourseControl(controlView.courseControlId);

            Debug.Assert(control.kind == ControlPointKind.Finish || control.kind == ControlPointKind.CrossingPoint);

            DescriptionLine line = new DescriptionLine();
            line.kind = DescriptionLineKind.Directive;
            line.boxes = new object[2];

            // Figure out the distance in the directive, rounded to nearest 10m.
            string distanceText;
            if (controlViewPrev != null && controlViewPrev.legLength != null) {
                float distance = controlViewPrev.legLength[0];
                distance = (float)(Math.Round(distance / 10.0) * 10.0);      // round to nearest 10 m.
                distanceText = string.Format("{0} m", distance);
            }
            else
                distanceText = "";

            // Box 1: directive graphics.
            line.boxes[0] = symbolDB[control.symbolIds[0]];

            // Box 2: distance for the control, if any.
            if (control.kind == ControlPointKind.Finish)
                line.boxes[1] = distanceText;

            // Get the text version of the control using the Textifier.
            Textifier textifier = new Textifier(eventDB, symbolDB, language);
            line.textual = textifier.CreateTextForControl(controlView.controlId, distanceText); 

            // The course control ID, for use in coordinating the selection
            line.controlId = controlView.controlId;
            line.courseControlId = controlView.courseControlId;

            return line;
        }

        // Get a directive line for a marked route (not to the finish). The legId must be valid, because a marked route only occurs
        // with a real leg id.
        private DescriptionLine GetMarkedRouteLine(CourseView.ControlView controlViewFrom, CourseView.ControlView controlViewTo, Id<Leg> legId)
        {
            Leg leg = eventDB.GetLeg(legId);

            Debug.Assert(leg.flagging != FlaggingKind.None && leg.flagging != FlaggingKind.End);

            DescriptionLine line = new DescriptionLine();
            line.kind = DescriptionLineKind.Directive;
            line.boxes = new object[2];

            // Figure out the distance in the directive, rounded to nearest 10m.
            string distanceText;
            float distance = QueryEvent.ComputeFlaggedLegLength(eventDB, leg.controlId1, leg.controlId2, legId);
            distance = (float) (Math.Round(distance / 10.0) * 10.0);      // round to nearest 10 m.
            distanceText = string.Format("{0} m", distance);

            // Box 1: directive graphics.
            string symbolId = (leg.flagging == FlaggingKind.Begin) ? "13.1" : "13.2";
            line.boxes[0] = symbolDB[symbolId];

            // Box 2: distance of the flagging
            line.boxes[1] = distanceText;

            // Get the text version of the control using the Textifier.
            Textifier textifier = new Textifier(eventDB, symbolDB, language);
            line.textual = textifier.CreateTextForDirective(symbolId, distanceText);

            // The course control IDs, for use in coordinating the selection
            line.isLeg = true;
            line.controlId = controlViewFrom.controlId;
            line.courseControlId = controlViewFrom.courseControlId;
            line.courseControlId2 = controlViewTo.courseControlId;

            return line;
        }

        // Get a directive line for a flagged route to map exchange (not to the finish). The distance between the controls is calculated and used for the distance
        // in the direction. 
        private DescriptionLine GetMapExchangeLine(CourseView.ControlView controlViewFrom, CourseView.ControlView controlViewTo)
        {
            DescriptionLine line = new DescriptionLine();
            line.kind = DescriptionLineKind.Directive;
            line.boxes = new object[2];

            // Figure out the distance in the directive, rounded to nearest 10m.
            float distance;       // default distance is zero.
            string distanceText;
            distance = controlViewFrom.legLength[0];
            distance = (float) (Math.Round(distance / 10.0) * 10.0);      // round to nearest 10 m.
            distanceText = string.Format("{0} m", distance);

            // Box 1: directive graphics.
            string symbolId = "13.5";
            line.boxes[0] = symbolDB[symbolId];

            // Box 2: distance of the flagging
            line.boxes[1] = distanceText;

            // Get the text version of the control using the Textifier.
            Textifier textifier = new Textifier(eventDB, symbolDB, language);
            line.textual = textifier.CreateTextForDirective(symbolId, distanceText);

            // The course control IDs, for use in coordinating the selection
            line.isLeg = true;
            line.controlId = controlViewFrom.controlId;
            line.courseControlId = controlViewFrom.courseControlId;
            line.courseControlId2 = controlViewTo.courseControlId;

            return line;
        }

        // Get a directive line for a map exchange at a control (not to the finish). 
        private DescriptionLine GetMapExchangeAtControlLine(CourseView.ControlView controlWithExchange)
        {
            DescriptionLine line = new DescriptionLine();
            line.kind = DescriptionLineKind.Directive;
            line.boxes = new object[2];

            // Distance is 0m at the control!
            string distanceText = string.Format("{0} m", 0);

            // Box 1: directive graphics.
            string symbolId = "13.5control";
            line.boxes[0] = symbolDB[symbolId];

            // Box 2: distance of the flagging
            line.boxes[1] = distanceText;

            // Get the text version of the control using the Textifier.
            Textifier textifier = new Textifier(eventDB, symbolDB, language);
            line.textual = textifier.CreateTextForDirective(symbolId, distanceText);

            // The course control IDs, for use in coordinating the selection
            line.controlId = controlWithExchange.controlId;
            line.courseControlId = controlWithExchange.courseControlId;

            return line;
        }

        // Return true if this control should be included, false if not.
        private bool FilterControl(CourseView.CourseViewKind kind, ControlPoint control, ControlPoint controlPrev, ControlPoint controlNext)
        {
            switch (kind) {
                case CourseView.CourseViewKind.AllControls:
                    // All controls list shows all kinds.
                    return true;

                case CourseView.CourseViewKind.Normal:
                    // Normal list shows all control kinds. 

                    // filter out duplicate crossing points.
                    if (control.kind == ControlPointKind.CrossingPoint && controlPrev != null && controlPrev.kind == ControlPointKind.CrossingPoint)
                        return false;

                    // Don't show map exchange that is the last control being shown.
                    if (control.kind == ControlPointKind.MapExchange && controlNext == null)
                        return false;

                    return true;

                case CourseView.CourseViewKind.Score:
                    // Score course shows start, normal controls.
                    return (control.kind == ControlPointKind.Normal || control.kind == ControlPointKind.Start);

                default:
                    Debug.Fail("bad course view kind");
                    return false;
            }
        }

        // Return true if this leg should be included, false if not.
        private bool FilterLeg(CourseView.CourseViewKind kind, ControlPoint from, ControlPoint to, Leg leg)
        {
            if (leg == null)
                return false;

            if (leg.flagging == FlaggingKind.None || leg.flagging == FlaggingKind.End)
                return false;

            // Flagged legs that end at the finish or a map exchange are 
            // included in the finish control.
            if (to.kind == ControlPointKind.Finish || to.kind == ControlPointKind.MapExchange)
                return false;

            return true;
        }

        // Create a set of description lines for a course. If "createKey" is true, then lines for a key are created based on any symbols
        // that have custom text. This is typically done only if text description are not already being printed.
        public DescriptionLine[] CreateDescription(bool createKey)
        {
            EventDB eventDB = courseView.EventDB;
            CourseView.CourseViewKind kind = courseView.Kind;
            int scoreColumn = courseView.ScoreColumn;
            List<DescriptionLine> list = new List<DescriptionLine>(courseView.ControlViews.Count + 4);
            string text;
            DescriptionLine line;
            DescriptionLine[] lines;
            Dictionary<string, string> descriptionKey = new Dictionary<string, string>(); // dictionary for any symbols encountered with custom text.

            // Get the first title line.
            text = GetTitleLine1();
            Debug.Assert(text != null);
            lines = GetTitleLineFromText(DescriptionLineKind.Title, text);
            list.AddRange(lines);

            // Get the second title line.
            text = GetTitleLine2();
            if (text != null) {
                lines = GetTitleLineFromText(DescriptionLineKind.SecondaryTitle, text);
                list.AddRange(lines);
            }

            // Get the header line, depending on the kind of course.
            switch (kind) {
                case CourseView.CourseViewKind.Normal:
                line = GetNormalHeaderLine(); break;

                case CourseView.CourseViewKind.AllControls:
                    line = GetAllControlsHeaderLine(); break;

                case CourseView.CourseViewKind.Score:
                    line = GetScoreHeaderLine(); break;

                default:
                    Debug.Fail("unknown CourseViewKind"); line = null;  break;
            }

            if (line != null)
                list.Add(line);

            // Do all the normal lines
            for (int iLine = 0; iLine < courseView.ControlViews.Count; ++iLine) {
                CourseView.ControlView controlView = courseView.ControlViews[iLine];
                ControlPoint control = eventDB.GetControl(controlView.controlId);
                CourseControl courseControl = controlView.courseControlId.IsNone ? null : eventDB.GetCourseControl(controlView.courseControlId);

                // CONSIDER: this might need to be updated for relay or split controls.
                ControlPoint controlPrev = (iLine > 0) ? eventDB.GetControl(courseView.ControlViews[iLine - 1].controlId) : null;
                ControlPoint controlNext = (iLine < courseView.ControlViews.Count - 1) ? eventDB.GetControl(courseView.ControlViews[iLine + 1].controlId) : null;
                //Id<CourseControl> courseControlIdNext = (iLine < courseView.ControlViews.Count - 1) ? courseView.ControlViews[iLine + 1].courseControlId : Id<CourseControl>.None;
                //CourseControl courseControlNext = courseControlIdNext.IsNotNone ? eventDB.GetCourseControl(coruseControlIdNext) : null;

                // Do the control.control
                if (FilterControl(kind, control, controlPrev, controlNext)) 
                {
                    // Text associated with the course or course control (before)
                    AddTextLine(list, control.descTextBefore, controlView.courseControlId, controlView.controlId, DescriptionLine.TextLineKind.BeforeControl);
                    if (courseControl != null)
                        AddTextLine(list, courseControl.descTextBefore, controlView.courseControlId, controlView.controlId, DescriptionLine.TextLineKind.BeforeCourseControl);

                    // The control itself.
                    if (control.kind == ControlPointKind.Finish ||
                        control.kind == ControlPointKind.CrossingPoint) 
                    {
                        line = GetDirectiveLine(kind, controlView, iLine > 0 ? courseView.ControlViews[iLine - 1] : null);
                    }
                    else {
                        line = GetRegularLine(kind, scoreColumn, controlView, descriptionKey);
                    }
                    Debug.Assert(line != null);
                    list.Add(line);

                    // Text associated with the course or course control (after)
                    if (courseControl != null)
                        AddTextLine(list, courseControl.descTextAfter, controlView.courseControlId, controlView.controlId, DescriptionLine.TextLineKind.AfterCourseControl);
                    AddTextLine(list, control.descTextAfter, controlView.courseControlId, controlView.controlId, DescriptionLine.TextLineKind.AfterControl);
                }

                // Add any map exchange lines.
                if (courseView.Kind == CourseView.CourseViewKind.Normal) {
                    if (controlNext != null && controlNext.kind == ControlPointKind.MapExchange) {
                        line = GetMapExchangeLine(controlView, courseView.ControlViews[controlView.legTo[0]]);
                        list.Add(line);
                    }
                    else if (courseControl != null && courseControl.exchange && control.kind != ControlPointKind.MapExchange && controlPrev != null) {
                        line = GetMapExchangeAtControlLine(controlView);
                        list.Add(line);
                    }
                }

                // Do the leg (if any).
                if (controlView.legTo != null && controlView.legTo.Length > 0) {
                    Id<Leg> legId = controlView.legId[0];
                    Leg leg = (legId.IsNotNone) ? eventDB.GetLeg(legId) : null;
                    if (FilterLeg(kind, control, controlNext, leg)) {
                        line = GetMarkedRouteLine(controlView, courseView.ControlViews[controlView.legTo[0]], legId);
                        Debug.Assert(line != null);
                        list.Add(line);
                    }
                }

            }

            // Add the key if desired.
            if (createKey) {
                foreach (string symbolId in descriptionKey.Keys) {
                    line = new DescriptionLine();
                    line.kind = DescriptionLineKind.Key;
                    line.boxes = new object[2];
                    line.boxes[0] = symbolDB[symbolId];
                    line.boxes[1] = descriptionKey[symbolId];

                    list.Add(line);
                }
            }

            // And we're done!
            return list.ToArray();
        }

        // Dump a description line to a text writer.
        static void DumpDescriptionLine(SymbolDB symbolDB, DescriptionLine line, TextWriter writer)
        {
            if (line.controlId.IsNotNone)
                writer.Write("({0,3}) |", line.controlId);
            else
                writer.Write("      |");

            switch (line.kind) {
                case DescriptionLineKind.Title:
                case DescriptionLineKind.SecondaryTitle:
                case DescriptionLineKind.Text:
                    writer.Write(" {0,-46}|", line.boxes[0]);
                    break;

                case DescriptionLineKind.Normal:
                    for (int i = 0; i < 8; ++i) {
                        string text;
                        if (line.boxes[i] == null)
                            text = "";
                        else if (line.boxes[i] is Symbol)
                            text = ((Symbol)(line.boxes[i])).Id;
                        else
                            text = (string)(line.boxes[i]);

                        writer.Write("{0,5}|", line.boxes[i] is Symbol ? ((Symbol)(line.boxes[i])).Id : (string)(line.boxes[i]));
                    }
                    break;

                case DescriptionLineKind.Header3Box:
                    writer.Write("{0,-17}|", line.boxes[0] == null ? "" : (string)(line.boxes[0]));
                    writer.Write("{0,-17}|", line.boxes[1] == null ? "" : (string)(line.boxes[1]));
                    writer.Write("{0,-11}|", line.boxes[2] == null ? "" : (string)(line.boxes[2]));
                    break;

                case DescriptionLineKind.Header2Box:
                    writer.Write("{0,-17}|", line.boxes[0] == null ? "" : (string) (line.boxes[0]));
                    writer.Write("{0,-29}|", line.boxes[1] == null ? "" : (string) (line.boxes[1]));
                    break;

                case DescriptionLineKind.Key:
                    writer.Write("{0,-17}|", line.boxes[0] == null ? "" : ((Symbol) (line.boxes[0])).Id);
                    writer.Write("{0,-29}|", line.boxes[1] == null ? "" : (string) (line.boxes[1]));
                    break;

                case DescriptionLineKind.Directive:
                    writer.Write("     {0,16}: {1,-24}|", line.boxes[0] != null ? ((Symbol)(line.boxes[0])).Id : "", line.boxes[1] == null ? "" : (string)(line.boxes[1]));
                    break;
            }

            if (line.textual != null)
                writer.Write("   [{0}]", line.textual);

            writer.WriteLine();
        }

        public static void DumpDescription(SymbolDB symbolDB, DescriptionLine[] lines, TextWriter writer)
        {
            foreach (DescriptionLine line in lines)
                DumpDescriptionLine(symbolDB, line, writer);
        }

        // Remove all text and symbol from a description line, leaving only the "grid" when rendered.
        public static void ClearTextAndSymbols(DescriptionLine[] lines)
        {
            foreach (DescriptionLine line in lines) {
                line.textual = null;
                for (int i = 0; i < line.boxes.Length; ++i)
                    line.boxes[i] = null;
            }
        }
    }
}
