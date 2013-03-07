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
 * NEGLIGENCE OR OTSpekHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using PurplePen.Graphics2D;

namespace PurplePen
{
    // Describes the formatting of part of the text description.
    public enum TextFormat
    {
        NewLine,              // normal text on a new line.
        SameLine,            // normal text on the same line
        Title,                     // the title line (implies new line) -- bigger than a normal header
        Header                 // a header line (implies new line)
    }

    // A text part is one line of the selection description.
    public struct TextPart
    {
        public TextFormat format;   // format of the text
        public string text;                // the text.

        public TextPart(TextFormat format, string text)
        {
            this.format = format;
            this.text = text;
        }
    }

    // The selection describer creates a description of the selection for display in the description pane.
    class SelectionDescriber
    {
        private enum DescKind { Tooltip, DescPane }

        // Describe the selection, and return an array of TextParts for display in the UI.
        public static TextPart[] DescribeSelection(SymbolDB symbolDB, EventDB eventDB, CourseView activeCourseView, SelectionMgr.SelectionInfo selection)
        {
            if (selection.SelectionKind == SelectionMgr.SelectionKind.Key) {
                return DescribeKey(eventDB);
            }
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.TextLine) {
                return DescribeTextLine(eventDB, selection.SelectedControl, selection.SelectedTextLineKind);
            }
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.Control) {
                return DescribeControlPoint(symbolDB, eventDB, selection.SelectedControl, DescKind.DescPane);
            }
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.Leg) {
                return DescribeLeg(eventDB, selection.SelectedCourseControl, selection.SelectedCourseControl2, DescKind.DescPane);
            }
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.Special) {
                return DescribeSpecial(eventDB, selection.SelectedSpecial, activeCourseView.ScaleRatio, DescKind.DescPane);
            }
            else if (selection.SelectionKind == SelectionMgr.SelectionKind.MapExchangeAtControl) {
                return DescribeMapExchangeAtControl(eventDB, selection.SelectedControl);
            }
            else if (selection.ActiveCourseDesignator.IsNotAllControls) {
                return DescribeCourse(eventDB, activeCourseView);
            }
            else {
                return DescribeAllControls(eventDB, activeCourseView);
            }
        }

        // Describe a course object, and return an array of TextParts for display in the UI. Return null if nothing useful
        // can be said.
        public static TextPart[] DescribeCourseObject(SymbolDB symbolDB, EventDB eventDB, CourseObj courseObj)
        {
            if (courseObj is LineCourseObj && courseObj.courseControlId.IsNotNone && ((LineCourseObj)courseObj).courseControlId2.IsNotNone) {
                return DescribeLeg(eventDB, courseObj.courseControlId, ((LineCourseObj)courseObj).courseControlId2, DescKind.Tooltip);
            }
            else if (courseObj.controlId.IsNotNone) {
                return DescribeControlPoint(symbolDB, eventDB, courseObj.controlId, DescKind.Tooltip);
            }
            else if (courseObj.specialId.IsNotNone) {
                return DescribeSpecial(eventDB, courseObj.specialId, courseObj.scaleRatio, DescKind.Tooltip);
            }
            else {
                return null;
            }
        }

        // Description a control.
        public static TextPart[] DescribeControl(SymbolDB symbolDB, EventDB eventDB, Id<ControlPoint> controlId)
        {
            return DescribeControlPoint(symbolDB, eventDB, controlId, DescKind.Tooltip);
        }

        // Get the text name for a control.
        private static string SpecialName(EventDB eventDB, Id<Special> specialId)
        {
            Special special = eventDB.GetSpecial(specialId);

            // Resources have the name "SpecialKind_xxx" where "xxx" is the enumeration name.
            return SelectionDescriptionText.ResourceManager.GetString("SpecialName_" + special.kind.ToString());
        }


        // Get the text for a list of courses. Returns "None" for no courses. Returns "All courses" for all courses.
        // The list of course names is sorted.
        private static string CourseListText(EventDB eventDB, Id<Course>[] courseIds)
        {
            if (courseIds.Length == 0)
                return SelectionDescriptionText.CourseList_None; ;

            if (courseIds.Length == QueryEvent.CountCourses(eventDB))
                return SelectionDescriptionText.CourseList_AllCourses;

            StringBuilder builder = new StringBuilder();
            string[] courseNames = new string[courseIds.Length];

            for (int i = 0; i < courseIds.Length; ++i) {
                courseNames[i] = eventDB.GetCourse(courseIds[i]).name;
            }
            Array.Sort(courseNames);

            for (int i = 0; i < courseNames.Length; ++i) {
                if (i != 0)
                    builder.Append(", ");
                builder.Append(courseNames[i]);
            }

            return builder.ToString();
        }

        // Get the text for a list of courses. Returns "None" for no courses. Returns "All courses" for all courses.
        // The list of course names is sorted. Ignores part numbers, but doesn't duplicate a course if multiple parts
        // are present.
        private static string CourseListText(EventDB eventDB, CourseDesignator[] courseDesignators)
        {
            HashSet<Id<Course>> courses = new HashSet<Id<Course>>();
            foreach (CourseDesignator designator in courseDesignators) {
                if (designator.IsAllControls)
                    return SelectionDescriptionText.CourseList_AllCourses;
                else
                    courses.Add(designator.CourseId);
            }

            return CourseListText(eventDB, courses.ToArray());
        }


        // Describe the symbol key
        private static TextPart[] DescribeKey(EventDB eventDB)
        {
            List<TextPart> list = new List<TextPart>();
            list.Add(new TextPart(TextFormat.Title, SelectionDescriptionText.CustomizedSymbolDesc));
            return list.ToArray();
        }

        // Describe text line
        private static TextPart[] DescribeTextLine(EventDB eventDB, Id<ControlPoint> controlId, DescriptionLine.TextLineKind textLineKind)
        {
            List<TextPart> list = new List<TextPart>();

            list.Add(new TextPart(TextFormat.Title, SelectionDescriptionText.TextLine));
            list.Add(new TextPart(TextFormat.Header, SelectionDescriptionText.Location));

            string format;
            switch (textLineKind) {
            case DescriptionLine.TextLineKind.BeforeControl:
                format = SelectionDescriptionText.TextLine_AboveAllCourses;
                break;
            case DescriptionLine.TextLineKind.BeforeCourseControl:
                format = SelectionDescriptionText.TextLine_AboveThisCourse;
                break;
            case DescriptionLine.TextLineKind.AfterControl:
                format = SelectionDescriptionText.TextLine_BelowAllCourses;
                break;
            case DescriptionLine.TextLineKind.AfterCourseControl:
                format = SelectionDescriptionText.TextLine_BelowThisCourse;
                break;
            case DescriptionLine.TextLineKind.None:
            default:
                return list.ToArray();
            }

            list.Add(new TextPart(TextFormat.NewLine, string.Format(format, Util.ControlPointName(eventDB, controlId, NameStyle.Long))));

            return list.ToArray();
        }


        // Describe a control point.
        private static TextPart[] DescribeControlPoint(SymbolDB symbolDB, EventDB eventDB, Id<ControlPoint> controlId, DescKind descKind)
        {
            Debug.Assert(descKind == DescKind.DescPane || descKind == DescKind.Tooltip);

            List<TextPart> list = new List<TextPart>();
            ControlPoint control = eventDB.GetControl(controlId);

            // Control name/code.
            list.Add(new TextPart(TextFormat.Title, Util.ControlPointName(eventDB, controlId, NameStyle.Long)));

            // Control location.
            if (descKind == DescKind.DescPane) {
                list.Add(new TextPart(TextFormat.Header, SelectionDescriptionText.Location + "  "));
                list.Add(new TextPart(TextFormat.SameLine, string.Format("({0:##0.0}, {1:##0.0})", control.location.X, control.location.Y)));
            }

            // Which courses is it used in?
            list.Add(new TextPart(TextFormat.Header, (descKind == DescKind.Tooltip ? SelectionDescriptionText.UsedIn : SelectionDescriptionText.UsedInCourses)));
            Id<Course>[] coursesUsingControl = QueryEvent.CoursesUsingControl(eventDB, controlId);
            list.Add(new TextPart(descKind == DescKind.Tooltip ? TextFormat.SameLine : TextFormat.NewLine, CourseListText(eventDB, coursesUsingControl)));

            // What is the competitor load?
            int load = QueryEvent.GetControlLoad(eventDB, controlId);
            if (load >= 0) {
                list.Add(new TextPart(TextFormat.Header, (descKind == DescKind.Tooltip ? SelectionDescriptionText.Load : SelectionDescriptionText.CompetitorLoad)));
                list.Add(new TextPart(TextFormat.SameLine, string.Format("{0}", load)));
            }

            // Text version of the descriptions
            if (descKind == DescKind.DescPane) {
                Textifier textifier = new Textifier(eventDB, symbolDB, QueryEvent.GetDescriptionLanguage(eventDB));
                string descText = textifier.CreateTextForControl(controlId, null);
                list.Add(new TextPart(TextFormat.Header, SelectionDescriptionText.TextDescription));
                list.Add(new TextPart(TextFormat.NewLine, descText));
            }

            return list.ToArray();
        }

        // Describe a course.
        private static TextPart[] DescribeCourse(EventDB eventDB, CourseView activeCourseView)
        {
            List<TextPart> list = new List<TextPart>();

            // UNDONE MAPEXCHANGE: do special something for a part of a course.

            // Course name
            list.Add(new TextPart(TextFormat.Title, string.Format(SelectionDescriptionText.CourseName, activeCourseView.CourseName)));

            if (activeCourseView.Kind == CourseView.CourseViewKind.Normal) {
                // Course length
                list.Add(new TextPart(TextFormat.Header, SelectionDescriptionText.Length));
                list.Add(new TextPart(TextFormat.SameLine,
                    string.Format("{0:0.00} km", Math.Round(activeCourseView.TotalLength / 10.0, MidpointRounding.AwayFromZero) / 100.0)));

                if (activeCourseView.TotalClimb >= 0) {
                    list.Add(new TextPart(TextFormat.Header, SelectionDescriptionText.Climb + "  "));
                    list.Add(new TextPart(TextFormat.SameLine,
                        string.Format("{0:#,###} m", Math.Round(activeCourseView.TotalClimb, MidpointRounding.AwayFromZero))));
                }
            }
            else if (activeCourseView.Kind == CourseView.CourseViewKind.Score) {
                // Total controls
                list.Add(new TextPart(TextFormat.Header, SelectionDescriptionText.TotalControls + "  "));
                list.Add(new TextPart(TextFormat.SameLine,
                    string.Format("{0}", activeCourseView.TotalNormalControls)));

                if (activeCourseView.TotalScore > 0) {
                    list.Add(new TextPart(TextFormat.Header, SelectionDescriptionText.TotalScore + "  "));
                    list.Add(new TextPart(TextFormat.SameLine,
                        string.Format("{0}", activeCourseView.TotalScore)));
                }
            }

            // What is the competitor load?
            int load = QueryEvent.GetCourseLoad(eventDB, activeCourseView.BaseCourseId);
            if (load >= 0) {
                list.Add(new TextPart(TextFormat.Header, SelectionDescriptionText.CompetitorLoad));
                list.Add(new TextPart(TextFormat.SameLine, string.Format("{0}", load)));
            } 

            return list.ToArray();
        }

        // Add a count of objects to a string. Only add non-zero counts, and handle plurals correctly.
        private static string AddCount(string existing, int count, string singular, string plural)
        {
            if (count > 0) {
                if (existing.Length > 0)
                    existing += ", ";

                existing += string.Format("{0} {1}", count, (count == 1) ? singular : plural);
            }

            return existing;
        }


        // Count controls in a course view that match a predicate.
        private static string CountControls(CourseView courseView, Predicate<Id<ControlPoint>> predicate)
        {
            string desc = "";
            int[] count = new int[6];

            foreach (CourseView.ControlView controlView in courseView.ControlViews) {
                if (predicate(controlView.controlId)) {
                    ControlPoint control = courseView.EventDB.GetControl(controlView.controlId);
                    if (control.kind == ControlPointKind.Normal || control.kind == ControlPointKind.Start || 
                        control.kind == ControlPointKind.Finish || control.kind == ControlPointKind.CrossingPoint || control.kind == ControlPointKind.MapExchange)
                    {
                        count[(int) control.kind] += 1;
                    }
                }
            }

            // Add the counts of each control kind to a string.

            if (count[(int) ControlPointKind.Normal] > 0)
                desc = AddCount(desc, count[(int) ControlPointKind.Normal], SelectionDescriptionText.Control_Singular, SelectionDescriptionText.Control_Plural);

            if (count[(int) ControlPointKind.Start] > 0)
                desc = AddCount(desc, count[(int) ControlPointKind.Start], SelectionDescriptionText.Start_Singular, SelectionDescriptionText.Start_Plural);

            if (count[(int) ControlPointKind.Finish] > 0)
                desc = AddCount(desc, count[(int) ControlPointKind.Finish], SelectionDescriptionText.Finish_Singular, SelectionDescriptionText.Finish_Plural);

            if (count[(int) ControlPointKind.CrossingPoint] > 0)
                desc = AddCount(desc, count[(int) ControlPointKind.CrossingPoint], SelectionDescriptionText.MandCrossing_Singular, SelectionDescriptionText.MandCrossing_Plural);

            if (count[(int) ControlPointKind.MapExchange] > 0)
                desc = AddCount(desc, count[(int) ControlPointKind.MapExchange], SelectionDescriptionText.MapExchange_Singular, SelectionDescriptionText.MapExchange_Plural);


            // If we didn't find anthing the count string will still be empty.
            if (desc == "")
                desc = SelectionDescriptionText.None;

            return desc;
        }

        // Describe the all controls.
        private static TextPart[] DescribeAllControls(EventDB eventDB, CourseView activeCourseView)
        {
            List<TextPart> list = new List<TextPart>();

            // Course name
            list.Add(new TextPart(TextFormat.Title, activeCourseView.CourseName));

            // Count controls in use.
            list.Add(new TextPart(TextFormat.Header, SelectionDescriptionText.ControlsInUse));
            list.Add(new TextPart(TextFormat.NewLine, CountControls(activeCourseView, delegate(Id<ControlPoint> controlId) { 
                return QueryEvent.CoursesUsingControl(eventDB, controlId).Length > 0;
            })));

            // Count controls not in use.
            list.Add(new TextPart(TextFormat.Header, SelectionDescriptionText.ControlsNotInUse));
            list.Add(new TextPart(TextFormat.NewLine, CountControls(activeCourseView, delegate(Id<ControlPoint> controlId) { 
                return QueryEvent.CoursesUsingControl(eventDB, controlId).Length == 0;
            })));

            return list.ToArray();
        }

        // Determine the type of flagging
        private static string FlaggingType(EventDB eventDB, Id<ControlPoint> controlId1, Id<ControlPoint> controlId2, Id<Leg> legId)
        {
            string flaggingType = SelectionDescriptionText.None;
            FlaggingKind kind = QueryEvent.GetLegFlagging(eventDB, controlId1, controlId2, legId);

            switch (kind) {
            case FlaggingKind.All: flaggingType = SelectionDescriptionText.EntireLeg; break;
            case FlaggingKind.Begin: flaggingType = SelectionDescriptionText.AwayFromControl; break;
            case FlaggingKind.End: flaggingType = SelectionDescriptionText.IntoControl; break;
            }

            // We use slightly different wording based on the finish control.
            ControlPoint ending = eventDB.GetControl(controlId2);
            if (ending.kind == ControlPointKind.Finish) {
                // finish control can influence flagging!
                if (ending.symbolIds[0] == "14.2" && kind == FlaggingKind.None) 
                    flaggingType = SelectionDescriptionText.FinishFunnel; 
                else if (ending.symbolIds[0] == "14.2" && kind == FlaggingKind.End) 
                    flaggingType = SelectionDescriptionText.IntoFinishFunnel; 
            }

            return flaggingType;
        }

        // Describe a leg.
        private static TextPart[] DescribeLeg(EventDB eventDB, Id<CourseControl> courseControlId1, Id<CourseControl> courseControlId2, DescKind descKind)
        {
            Debug.Assert(descKind == DescKind.Tooltip || descKind == DescKind.DescPane);

            Id<ControlPoint> controlId1 = eventDB.GetCourseControl(courseControlId1).control;
            Id<ControlPoint> controlId2 = eventDB.GetCourseControl(courseControlId2).control;
            Id<Leg> legId = QueryEvent.FindLeg(eventDB, controlId1, controlId2);

            List<TextPart> list = new List<TextPart>();

            // Course name
            list.Add(new TextPart(TextFormat.Title, string.Format("{0} \u2013 {1}", Util.ControlPointName(eventDB, controlId1, NameStyle.Long), Util.ControlPointName(eventDB, controlId2, NameStyle.Long))));

            // Course length
            list.Add(new TextPart(TextFormat.Header, SelectionDescriptionText.Length));
            list.Add(new TextPart(TextFormat.SameLine,
                string.Format("{0:#,###} m", QueryEvent.ComputeLegLength(eventDB, controlId1, controlId2, legId)))); 

            // Which courses
            list.Add(new TextPart(TextFormat.Header, (descKind == DescKind.Tooltip ? SelectionDescriptionText.UsedIn : SelectionDescriptionText.UsedInCourses)));
            Id<Course>[] coursesUsingControl = QueryEvent.CoursesUsingLeg(eventDB, controlId1, controlId2);
            list.Add(new TextPart(descKind == DescKind.Tooltip ? TextFormat.SameLine : TextFormat.NewLine, CourseListText(eventDB, coursesUsingControl)));

            // What is the competitor load?
            int load = QueryEvent.GetLegLoad(eventDB, controlId1, controlId2);
            if (load >= 0) {
                list.Add(new TextPart(TextFormat.Header, (descKind == DescKind.Tooltip ? SelectionDescriptionText.Load : SelectionDescriptionText.CompetitorLoad)));
                list.Add(new TextPart(TextFormat.SameLine, string.Format("{0}", load)));
            }

            if (descKind == DescKind.DescPane) {
                // Flagging
                list.Add(new TextPart(TextFormat.Header, SelectionDescriptionText.Flagging + "  "));
                list.Add(new TextPart(TextFormat.SameLine, FlaggingType(eventDB, controlId1, controlId2, legId)));
            }

            return list.ToArray();
        }

        // Describe a map exchange at control
        private static TextPart[] DescribeMapExchangeAtControl(EventDB eventDB, Id<ControlPoint> controlId)
        {
            List<TextPart> list = new List<TextPart>();

            list.Add(new TextPart(TextFormat.Title, string.Format(SelectionDescriptionText.MapExchangeAtControl, Util.ControlPointName(eventDB, controlId, NameStyle.Long))));

            return list.ToArray();
        }


        // Describe a special.
        private static TextPart[] DescribeSpecial(EventDB eventDB, Id<Special> specialId, float scaleRatio, DescKind descKind)
        {
            Debug.Assert(descKind == DescKind.Tooltip || descKind == DescKind.DescPane);

            List<TextPart> list = new List<TextPart>();
            Special special = eventDB.GetSpecial(specialId);

            // Name of the special.
            list.Add(new TextPart(TextFormat.Title, SpecialName(eventDB, specialId)));

            if (descKind == DescKind.DescPane) {
                // Special location.
                if (special.kind == SpecialKind.FirstAid || special.kind == SpecialKind.Water || special.kind == SpecialKind.Forbidden ||
                    special.kind == SpecialKind.OptCrossing || special.kind == SpecialKind.RegMark || special.kind == SpecialKind.Descriptions) {
                    list.Add(new TextPart(TextFormat.Header, SelectionDescriptionText.Location + "  "));
                    list.Add(new TextPart(TextFormat.SameLine, string.Format("({0:##0.0}, {1:##0.0})", special.locations[0].X, special.locations[0].Y)));
                }
            }

            // Line height for descriptions.
            if (special.kind == SpecialKind.Descriptions) {
                list.Add(new TextPart(TextFormat.Header, SelectionDescriptionText.LineHeight + "  "));
                list.Add(new TextPart(TextFormat.SameLine, string.Format("{0:#0.0} mm", Geometry.Distance(special.locations[0], special.locations[1]) / scaleRatio)));
            }

            // Which courses is it used in?
            list.Add(new TextPart(TextFormat.Header, (descKind == DescKind.Tooltip ? SelectionDescriptionText.UsedIn : SelectionDescriptionText.UsedInCourses)));
            if (special.allCourses)
                list.Add(new TextPart(descKind == DescKind.Tooltip ? TextFormat.SameLine : TextFormat.NewLine, SelectionDescriptionText.CourseList_AllCourses));
            else
                list.Add(new TextPart(descKind == DescKind.Tooltip ? TextFormat.SameLine : TextFormat.NewLine, CourseListText(eventDB, special.courses)));

            return list.ToArray();
        }



    }
}
