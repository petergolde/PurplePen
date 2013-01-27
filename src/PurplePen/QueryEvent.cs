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
using System.Diagnostics;
using System.Linq;

using PurplePen.MapModel;
using PurplePen.Graphics2D;

namespace PurplePen
{
    // The class queries the event database in useful ways.
    static class QueryEvent
    {
        // Determine if a code is in use in the database.
        public static bool IsCodeInUse(EventDB eventDB, string code)
        {
            return FindCode(eventDB, code).IsNotNone;
        }

        // Find the control with a code, and return its ID. Else return None.
        public static Id<ControlPoint> FindCode(EventDB eventDB, string code)
        {
            foreach (Id<ControlPoint> controlId in eventDB.AllControlPointIds) {
                if (eventDB.GetControl(controlId).code == code)
                    return controlId;
            }

            return Id<ControlPoint>.None;
        }

        // Enumerate all the course controls ids for a particular course.
        public static IEnumerable<Id<CourseControl>> EnumCourseControlIds(EventDB eventDB, CourseDesignator courseDesignator)
        {
            Debug.Assert(courseDesignator.IsNotAllControls);

            Id<Course> courseId = courseDesignator.CourseId;
            int part = courseDesignator.Part;
            Id<CourseControl> nextCourseControlId = eventDB.GetCourse(courseId).firstCourseControl;

            // Traverse the course control links.
            int currentPart = 0;
            while (nextCourseControlId.IsNotNone) {
                if (courseDesignator.AllParts || currentPart == part)
                    yield return nextCourseControlId;

                CourseControl courseCtl = eventDB.GetCourseControl(nextCourseControlId);

                if (courseCtl.exchange) {
                    ++currentPart;
                    if (!courseDesignator.AllParts && currentPart == part)
                        yield return nextCourseControlId;
                }

                if (courseCtl.split) {
                    Id<CourseControl> joinId = Id<CourseControl>.None;

                    foreach (Id<CourseControl> splitCourseControlId in courseCtl.nextSplitCourseControls) {
                        Id<CourseControl> nextCourseControlInSplit = splitCourseControlId;
                        while (nextCourseControlInSplit.IsNotNone) {
                            if (eventDB.GetCourseControl(nextCourseControlInSplit).join) {
                                Debug.Assert(joinId.IsNone || joinId == nextCourseControlInSplit);
                                joinId = nextCourseControlInSplit;
                                break;
                            }
                            else {
                                if (courseDesignator.AllParts || currentPart == part)
                                    yield return nextCourseControlInSplit;
                                nextCourseControlInSplit = eventDB.GetCourseControl(nextCourseControlInSplit).nextCourseControl;
                            }
                        }
                    }

                    nextCourseControlId = joinId;
                }
                else {
                    nextCourseControlId = courseCtl.nextCourseControl;
                }
            }
        }

        // Describes the information about a leg.
        public struct LegInfo
        {
            public Id<CourseControl> courseControlId1;
            public Id<CourseControl> courseControlId2;

            public LegInfo(Id<CourseControl> courseControlId1, Id<CourseControl> courseControlId2)
            {
                this.courseControlId1 = courseControlId1;
                this.courseControlId2 = courseControlId2;
            }
        }

        public static IEnumerable<LegInfo> EnumLegs(EventDB eventDB, CourseDesignator courseDesignator)
        {
            if (courseDesignator.IsAllControls)
                yield break;

            Id<Course> courseId = courseDesignator.CourseId;

            // Score courses, by definition, have no legs.
            if (eventDB.GetCourse(courseId).kind == CourseKind.Score)
                yield break;

            bool first = true;
            foreach (Id<CourseControl> courseControlId in EnumCourseControlIds(eventDB, courseDesignator)) {
                CourseControl courseControl = eventDB.GetCourseControl(courseControlId);
                if (first || courseDesignator.AllParts || !courseControl.exchange) {
                    if (courseControl.split) {
                        foreach (Id<CourseControl> courseControlIdTo in courseControl.nextSplitCourseControls)
                            yield return new LegInfo(courseControlId, courseControlIdTo);
                    }
                    else if (courseControl.nextCourseControl.IsNotNone) {
                        yield return new LegInfo(courseControlId, courseControl.nextCourseControl);
                    }
                }
                first = false;
            }
        }

        // Find the closest leg to a given point on a course. The leg might be None/None if the course has no legs.
        public static LegInfo FindClosestLeg(EventDB eventDB, CourseDesignator courseDesignator, PointF pt)
        {
            LegInfo closestLegSoFar = new LegInfo();
            float closestSoFar = 1E10F;

            foreach (LegInfo leg in EnumLegs(eventDB, courseDesignator)) {
                PointF temp;
                SymPath legPath = GetLegPath(eventDB, eventDB.GetCourseControl(leg.courseControlId1).control, eventDB.GetCourseControl(leg.courseControlId2).control);
                float distance = legPath.DistanceFromPoint(pt, out temp);
                if (distance < closestSoFar) {
                    closestSoFar = distance;
                    closestLegSoFar = leg;
                }
                else if (distance == closestSoFar) {
                    // Distances are equal. Use leg with the largest angle between the end points.
                    SymPath closestLegPath = GetLegPath(eventDB, eventDB.GetCourseControl(closestLegSoFar.courseControlId1).control, eventDB.GetCourseControl(closestLegSoFar.courseControlId2).control);
                    if (Geometry.Angle(legPath.FirstPoint, pt, legPath.LastPoint) >  Geometry.Angle(closestLegPath.FirstPoint, pt, closestLegPath.LastPoint)) {
                        closestSoFar = distance;
                        closestLegSoFar = leg;
                    }
                }
            }

            return closestLegSoFar;
        }

        // Find all the course controls for a particular control in a particular course. If the course
        // doesn't contain the given controlId, an empty array is returned.
        public static Id<CourseControl>[] GetCourseControlsInCourse(EventDB eventDB, CourseDesignator courseDesignator, Id<ControlPoint> controlId)
        {
            List<Id<CourseControl>> list = new List<Id<CourseControl>>();

            foreach (Id<CourseControl> courseControlId in EnumCourseControlIds(eventDB, courseDesignator)) {
                if (eventDB.GetCourseControl(courseControlId).control == controlId)
                    list.Add(courseControlId);
            }

            return list.ToArray();
        }

        // Return if a give course uses a given control.
        public static bool CourseUsesControl(EventDB eventDB, CourseDesignator courseDesignator, Id<ControlPoint> controlId)
        {
            eventDB.CheckControlId(controlId);

            foreach (Id<CourseControl> courseControlId in EnumCourseControlIds(eventDB, courseDesignator)) {
                if (eventDB.GetCourseControl(courseControlId).control == controlId)
                    return true;
            }

            return false;
        }

        // Find which courses are using a particular control. If none, return an 
        // empty array.
        public static Id<Course>[] CoursesUsingControl(EventDB eventDB, Id<ControlPoint> controlId)
        {
            List<Id<Course>> list = new List<Id<Course>>();

            foreach (Id<Course> courseId in SortedCourseIds(eventDB)) {
                if (CourseUsesControl(eventDB, new CourseDesignator(courseId), controlId))
                    list.Add(courseId);
            }

            return list.ToArray();
        }

        // Find which courses are using a particular leg. If none, return an 
        // empty array.
        public static Id<Course>[] CoursesUsingLeg(EventDB eventDB, Id<ControlPoint> control1, Id<ControlPoint> control2)
        {
            List<Id<Course>> list = new List<Id<Course>>();

            foreach (Id<Course> courseId in SortedCourseIds(eventDB)) {
                foreach (LegInfo leg in EnumLegs(eventDB, new CourseDesignator(courseId))) {
                    if (eventDB.GetCourseControl(leg.courseControlId1).control == control1 &&
                        eventDB.GetCourseControl(leg.courseControlId2).control == control2) 
                    {
                        list.Add(courseId);
                        break;
                    }
                }
            }

            return list.ToArray();
        }

        // Get the number of parts that this course has.  A course with no map exchanges has 1 part, with one
        // map exchange has 2 parts, etc.
        public static int CountCourseParts(EventDB eventDB, Id<Course> courseId)
        {
            int currentPart = 0;

            foreach (Id<CourseControl> courseControlId in EnumCourseControlIds(eventDB, new CourseDesignator(courseId))) {
                if (eventDB.GetCourseControl(courseControlId).exchange)
                    ++currentPart;
            }

            return currentPart + 1;
        }


        // Get the start and end coursecontrols (inclusive on both ends) for a particular part of a course (parts separated by map exchanges).
        // If the given part doesn't exist, return false. If there are no map exchanges, then part 0 is the entire course.
        public static bool GetCoursePartBounds(EventDB eventDB, CourseDesignator courseDesignator, out Id<CourseControl> startCourseControlId, out Id<CourseControl> endCourseControlId)
        {
            Debug.Assert(courseDesignator.IsNotAllControls);

            startCourseControlId = endCourseControlId = Id<CourseControl>.None;

            bool startFound = false;      // did we find the beginning part of the course part?

            foreach (Id<CourseControl> courseControlId in EnumCourseControlIds(eventDB, courseDesignator)) {
                if (!startFound) {
                    startFound = true;
                    startCourseControlId = courseControlId;
                }
                endCourseControlId = courseControlId;
            }

            return startFound;
        }

        // Determine if the given course control is in the given part.
        public static bool IsCourseControlInPart(EventDB eventDB, CourseDesignator courseDesignator, Id<CourseControl> courseControlId)
        {
            foreach (Id<CourseControl> courseControlInPart in EnumCourseControlIds(eventDB, courseDesignator)) {
                if (courseControlInPart == courseControlId)
                    return true;
            }

            return false;
        }

        // Given an array of courses, compute the control load. Return -1 if no control load set for any containing courses, or array is empty.
        private static int ComputeLoad(EventDB eventDB, Id<Course>[] courses)
        {
            bool anyLoadFound = false;
            int totalLoad = 0;

            foreach (Id<Course> courseId in courses) {
                int load = eventDB.GetCourse(courseId).load;
                if (load >= 0) {
                    anyLoadFound = true;
                    totalLoad += load;
                }
            }

            if (anyLoadFound)
                return totalLoad;
            else
                return -1;
        }

        // What is the control load for this control. Return -1 if not used in any courses that have a load set for them.
        public static int GetControlLoad(EventDB eventDB, Id<ControlPoint> controlId)
        {
            Id<Course>[] courses = CoursesUsingControl(eventDB, controlId);
            return ComputeLoad(eventDB, courses);
        }

        // What is the load for this leg. Return -1 if not used in any courses that have a load set for them.
        public static int GetLegLoad(EventDB eventDB, Id<ControlPoint> control1, Id<ControlPoint> control2)
        {
            Id<Course>[] courses = CoursesUsingLeg(eventDB, control1, control2);
            return ComputeLoad(eventDB, courses);
        }

        // What is the load for this course. Return -1 if not set.
        public static int GetCourseLoad(EventDB eventDB, Id<Course> courseId)
        {
            int load = eventDB.GetCourse(courseId).load;
            if (load >= 0)
                return load;
            else
                return -1;
        }

        // Figure out all unused controls.
        public static List<Id<ControlPoint>> ControlsUnusedInCourses(EventDB eventDB)
        {
            List<Id<ControlPoint>> unusedControls = new List<Id<ControlPoint>>();

            foreach (Id<ControlPoint> controlId in eventDB.AllControlPointIds) {
                if (CoursesUsingControl(eventDB, controlId).Length == 0)
                    unusedControls.Add(controlId);
            }

            return unusedControls;
        }


        // What is the sort order for this course. Order is integer > 0.
        public static int GetCourseSortOrder(EventDB eventDB, Id<Course> courseId)
        {
            return eventDB.GetCourse(courseId).sortOrder;
        }

        // What is the print scale for this course? Can be None, which gets All Controls print scale.
        public static float GetPrintScale(EventDB eventDB, Id<Course> courseId)
        {
            if (courseId.IsNone)
                return eventDB.GetEvent().allControlsPrintScale;
            else
                return eventDB.GetCourse(courseId).printScale;
        }

        // What is the default description kind for this course. Can be None, which gets
        // All Controls print scale.
        public static DescriptionKind GetDefaultDescKind(EventDB eventDB, Id<Course> courseId)
        {
            if (courseId.IsNone)
                return eventDB.GetEvent().allControlsDescKind;
            else
                return eventDB.GetCourse(courseId).descKind;
        }

        // Compare control ID by kind and code.
        public static int CompareControlIds(EventDB eventDB, Id<ControlPoint> controlId1, Id<ControlPoint> controlId2)
        {
            ControlPoint control1 = eventDB.GetControl(controlId1);
            ControlPoint control2 = eventDB.GetControl(controlId2);
            if (control1.kind < control2.kind)
                return -1;
            else if (control1.kind > control2.kind)
                return 1;

            int result = Util.CompareCodes(control1.code, control2.code);
            if (result != 0)
                return result;

            return controlId1.id.CompareTo(controlId2.id);
        }

        // Do all courses have loads set?
        public static bool AllCoursesHaveLoads(EventDB eventDB)
        {
            foreach (Course course in eventDB.AllCourses) {
                if (course.load < 0)
                    return false;
            }

            return true;
        }

        // Do any courses have loads set?
        public static bool AnyCoursesHaveLoads(EventDB eventDB)
        {
            foreach (Course course in eventDB.AllCourses) {
                if (course.load >= 0)
                    return true;
            }

            return false;
        }


        // Numbers that shouldn't be control codes because they look different upside down.
        static readonly int[] badUpsideDownNumbers = {                                           61,  66,   68,             81,  86,           89,                   98,  99, 
                                                                       106, 108, 109,         116, 118, 119, 161, 166, 168, 169,         186, 188, 189, 191, 196, 198, 199,
                                                               601, 606, 608,         611, 616, 618,         661, 666, 668, 669, 681, 686, 688,         691, 696, 698, 699,
                                                               801, 806,         809, 811, 816,         819, 861, 866, 868, 869, 881, 886,         889, 891, 896, 898, 899,
                                                               901,         908, 909, 911,         918, 919, 961, 966, 968, 969, 981,         988, 989, 991, 996, 998, 999};

        // Determine if a given control code is valid.
        public static bool IsLegalControlCode(string code, out string reason)
        {
            if (code.Length < 1) {
                reason = MiscText.CodeBadLength;
                return false;
            }
            if (code.Length > 3) {
                reason = MiscText.CodeBadLength;
                return false;
            }
            if (code.Contains(" ")) {
                reason = MiscText.CodeContainsSpace;
                return false;
            }

            reason = null;
            return true;
        }

        // Determine if a code is legal and preferred. Some legal codes like "20" are never preferred. Invertible codes are
        // preferred only if the event is set to disallow such codes.
        // Returns true if the code is legal, false if illegal. 
        // If legal but not preferred, returns true, but reason is set to a non-null value.
        // Legal but not preferred includes: under 31, leading zero, invertable (and event disallows those).
        public static bool IsPreferredControlCode(EventDB eventDB, string code, out string reason)
        {
            int codeNumber;

            bool disallowInvertibleCodes = eventDB.GetEvent().disallowInvertibleCodes;

            if (!IsLegalControlCode(code, out reason))
                return false;

            if (int.TryParse(code, System.Globalization.NumberStyles.None, null, out codeNumber)) {
                if (codeNumber < 31) {
                    reason = MiscText.CodeUnder31;
                    return true;  // legal but not preferred.
                }
                if (code[0] == '0') {
                    reason = MiscText.CodeBeginsWithZero;
                    return true;  // legal but not preferred.
                }
                if (disallowInvertibleCodes && Array.IndexOf(badUpsideDownNumbers, codeNumber) >= 0) {
                    reason = MiscText.CodeCouldBeUpsideDown;
                    return true;  // legal but not preferred.
                }

                // It's OK.
                reason = null;
                return true;
            }
            else {
                // Non-numerics are OK.
                reason = null;
                return true;
            }
        }

        // Get the lowest numeric code. Returns int.MaxValue if no number code in the event.
        private static int LowestNumericCode(EventDB eventDB)
        {
            int lowest = int.MaxValue;

            foreach (ControlPoint control in eventDB.AllControlPoints) {
                string code = control.code;
                int codeNumber;
                if (code != null && int.TryParse(code, System.Globalization.NumberStyles.None, null, out codeNumber)) {
                    if (codeNumber < lowest)
                        lowest = codeNumber;
                }
            }

            return lowest;
        }


        // Get the next unused control code. The next unused control code is determined by first taking
        // the initial code for this event. The next legal numeric control code
        // that isn't in use after this is returned, taking into account the invertibility setting for this event also.
        public static string NextUnusedControlCode(EventDB eventDB)
        {
            Event ev = eventDB.GetEvent();

            int lowestCode = ev.firstControlCode;

            // Loop to find a legal, unused control code.
            bool wrapped = false;
            int codeNumber = lowestCode;
            for (;;) {
                string code = codeNumber.ToString();
                string reason;

                if (FindCode(eventDB, code).IsNone && IsPreferredControlCode(eventDB, code, out reason) && reason == null)
                    return code;

                ++codeNumber;
                if (codeNumber == 1000) {
                    // Hit 1000. Wrap around to find another place.
                    if (wrapped)
                        return "XX";  // all the numbers are used! Unlikely, but we don't want to hang!
                    wrapped = true;
                    codeNumber = 31;
                }
            }
        }

        // Get the last course control in a course. In dontReturnFinish is true, will never return a finish control, but
        // always the control before the finish. Returns None if the course has no controls (or only a finish control.)
        public static Id<CourseControl> LastCourseControl(EventDB eventDB, Id<Course> courseId, bool dontReturnFinish)
        {
            Id<CourseControl> last = Id<CourseControl>.None;

            foreach (Id<CourseControl> courseControlId in EnumCourseControlIds(eventDB, new CourseDesignator(courseId))) {
                if (!dontReturnFinish || eventDB.GetControl(eventDB.GetCourseControl(courseControlId).control).kind != ControlPointKind.Finish)
                    last = courseControlId;
            }

            return last;
        }

        // Does the course have a finish control?
        public static bool HasFinishControl(EventDB eventDB, Id<Course> courseId)
        {
            Id<CourseControl> lastId = QueryEvent.LastCourseControl(eventDB, courseId, false);

            if (lastId.IsNone || eventDB.GetControl(eventDB.GetCourseControl(lastId).control).kind != ControlPointKind.Finish)
                return false;
            else
                return true;
       }

        // Does the course have a start control?
        public static bool HasStartControl(EventDB eventDB, Id<Course> courseId)
        {
            Id<CourseControl> firstId = eventDB.GetCourse(courseId).firstCourseControl;
            if (firstId.IsNone || eventDB.GetControl(eventDB.GetCourseControl(firstId).control).kind != ControlPointKind.Start)
                return false;
            else
                return true;
        }

        // Does the given course (or all controls) contain the given special? 
        public static bool CourseContainsSpecial(EventDB eventDB, CourseDesignator courseDesignator, Id<Special> specialId)
        {
            Special special = eventDB.GetSpecial(specialId);

            if (special.allCourses)
                return true;

            if (courseDesignator.AllParts)
                return special.courses.Any(cd => cd.CourseId == courseDesignator.CourseId);
            else
                return special.courses.Contains(courseDesignator) || special.courses.Contains(new CourseDesignator(courseDesignator.CourseId));
        }

        // Get the gaps in a control for a given scale.
        // Returns 0xFFFFFFFF if no gaps defined for that scale.
        public static uint GetControlGaps(EventDB eventDB, Id<ControlPoint> controlId, float scale)
        {
            int scaleInt = (int) Math.Round(scale);

            ControlPoint control = eventDB.GetControl(controlId);
            if (control.gaps == null)
                return 0xFFFFFFFF;
            else if (!control.gaps.ContainsKey(scaleInt))
                return 0xFFFFFFFF;
            else
                return control.gaps[scaleInt];
        }

        // Finds where a new regular control would be inserted into an existing course. courseControl1 and courseControl2 can either or both be none, to identify
        // a leg to insert into, a control to insert after, or no information about where to insert. Updates courseControl1 and courseControl2 to identify exactly
        // where on the course the control should be inserted as follows:
        //     If inserting between two course controls -- these are denoted by courseControl1 and courseControl2
        //     If inserting as last course control -- courseControl1 is the current last control and courseControl2 is None  (only occurs when there is no finish)
        //     If inserting as first course control -- courseControl2 is None and courseControl2 is current first control (only occurs when there is no start)
        //     If inserting as only course control -- both are none (only occurs if course is currently empty)
        public static void FindControlInsertionPoint(EventDB eventDB, CourseDesignator courseDesignator, ref Id<CourseControl> courseControl1, ref Id<CourseControl> courseControl2)
        {
            Id<Course> courseId = courseDesignator.CourseId;
            if (courseControl1.IsNotNone && courseControl2.IsNotNone) {
                // A leg was specified already. Nothing to do.
                return;
            }
            else {
                // Adding after courseControl1. If none, or a finish control, add at end, before the finish control if any.
                if (courseControl1.IsNone || eventDB.GetControl(eventDB.GetCourseControl(courseControl1).control).kind == ControlPointKind.Finish)
                    courseControl1 = QueryEvent.LastCourseControl(eventDB, courseId, true);

                if (courseControl1.IsNone) {
                    // Empty course or adding at start.
                    courseControl2 = eventDB.GetCourse(courseId).firstCourseControl;
                    return;
                }
                else {
                    // Adding after courseControl1.
                    CourseControl before = (CourseControl) eventDB.GetCourseControl(courseControl1);
                    if (before.split) {
                        throw new NotImplementedException("Not yet implemented.");    // UNDONE: not yet implemented
                    }
                    else {
                        // Not a split.
                        courseControl2 = before.nextCourseControl;
                        return;
                    }
                }
            }
        }

        // Compute the length of a leg between two controls, in meters. The indicated leg id, if non-zero, is used
        // to get bend information. The event map scale converts between the map scale, in mm, which is used
        // for the coordinate information, to meters in the world scale.
        public static float ComputeLegLength(EventDB eventDB, Id<ControlPoint> controlId1, Id<ControlPoint> controlId2, Id<Leg> legId)
        {
            PointF location1 = eventDB.GetControl(controlId1).location;
            PointF location2 = eventDB.GetControl(controlId2).location;

            SymPath path = GetLegPath(eventDB, controlId1, controlId2, legId);
            return (float) ((eventDB.GetEvent().mapScale * path.Length) / 1000.0);
        }

        // Compute the distance between two control points, in meters. The controls need not be part of a leg, and if they are, bends
        // in the leg are NOT taken into account.
        public static float ComputeStraightLineControlDistance(EventDB eventDB, Id<ControlPoint> controlId1, Id<ControlPoint> controlId2)
        {
            PointF location1 = eventDB.GetControl(controlId1).location;
            PointF location2 = eventDB.GetControl(controlId2).location;

            return (float) ((eventDB.GetEvent().mapScale * Geometry.Distance(location1, location2)) / 1000.0);
        }

        // Similar to ComputeLegLength. However, if the leg is flagged partially, only the length of the flagged portion is returned.
        // Note: if the flagging is NONE, this still returns the length of the whole leg!
        public static float ComputeFlaggedLegLength(EventDB eventDB, Id<ControlPoint> controlId1, Id<ControlPoint> controlId2, Id<Leg> legId)
        {
            PointF location1 = eventDB.GetControl(controlId1).location;
            PointF location2 = eventDB.GetControl(controlId2).location;
            PointF[] bends = null;
            Leg leg = null;

            if (legId.IsNotNone) {
                leg = eventDB.GetLeg(legId);
                Debug.Assert(leg.controlId1 == controlId1 && leg.controlId2 == controlId2);
                bends = leg.bends;
            }

            if (bends == null) {
                return (float) ((eventDB.GetEvent().mapScale * Geometry.Distance(location1, location2)) / 1000.0);
            }
            else {
                List<PointF> points = new List<PointF>();
                int bendIndexStart, bendIndexStop;

                points.Add(location1);
                points.AddRange(bends);
                points.Add(location2);

                // Which part is flagged?
                if (leg.flagging == FlaggingKind.Begin) {
                    bendIndexStart = 0; bendIndexStop = points.IndexOf(leg.flagStartStop);
                }
                else if (leg.flagging == FlaggingKind.End) {
                    bendIndexStart = points.IndexOf(leg.flagStartStop); bendIndexStop = points.Count - 1;
                }
                else {
                    bendIndexStart = 0; bendIndexStop = points.Count - 1;
                }

                double dist = 0;

                for (int i = bendIndexStart + 1; i <= bendIndexStop; ++i)
                    dist += Geometry.Distance(points[i - 1], points[i]);

                return (float) ((eventDB.GetEvent().mapScale * dist) / 1000.0);
            }
        }

        // Find a leg object, if one exists, between the two controls.
        public static Id<Leg> FindLeg(EventDB eventDB, Id<ControlPoint> controlId1, Id<ControlPoint> controlId2)
        {
            // Go through all the legs to find one that matches.
            foreach (Id<Leg> legId in eventDB.AllLegIds) {
                Leg leg = eventDB.GetLeg(legId);
                if (leg.controlId1 == controlId1 && leg.controlId2 == controlId2) {
                    return legId;
                }
            }

            // Didn't find it.
            return Id<Leg>.None;
        }

        // Find the kind of flagging for the leg from controlId1 to controlIs2. The legId, if not none, must be the correct leg id.
        public static FlaggingKind GetLegFlagging(EventDB eventDB, Id<ControlPoint> controlId1, Id<ControlPoint> controlId2, Id<Leg> legId)
        {
            FlaggingKind flagging = FlaggingKind.None;
            if (legId.IsNotNone) {
                Leg leg = eventDB.GetLeg(legId);
                Debug.Assert(leg.controlId1 == controlId1 && leg.controlId2 == controlId2);
                flagging = leg.flagging;
            }

            ControlPoint control2 = eventDB.GetControl(controlId2);
            if (control2.kind == ControlPointKind.Finish && control2.symbolIds[0] == "14.1")
                flagging = FlaggingKind.All;
            if (control2.kind == ControlPointKind.MapExchange)
                flagging = FlaggingKind.All;

            return flagging;
        }

        // Find the kind of flagging for the leg from controlId1 to controlId2.
        public static FlaggingKind GetLegFlagging(EventDB eventDB, Id<ControlPoint> controlId1, Id<ControlPoint> controlId2)
        {
            return GetLegFlagging(eventDB, controlId1, controlId2, QueryEvent.FindLeg(eventDB, controlId1, controlId2));
        }

        // Find the start/stop gaps fort he leg from controlId1 to controlsId2. The legId, if not none, must be the correct leg id.
        public static LegGap[] GetLegGaps(EventDB eventDB, Id<ControlPoint> controlId1, Id<ControlPoint> controlId2, Id<Leg> legId)
        {
            if (legId.IsNotNone) {
                Leg leg = eventDB.GetLeg(legId);
                Debug.Assert(leg.controlId1 == controlId1 && leg.controlId2 == controlId2);
                return (leg.gaps == null) ? null : (LegGap[]) leg.gaps.Clone();
            }
            else {
                return null;
            }
        }

        // Find the start/stop gaps fort he leg from controlId1 to controlsId2. The legId, if not none, must be the correct leg id.
        public static LegGap[] GetLegGaps(EventDB eventDB, Id<ControlPoint> controlId1, Id<ControlPoint> controlId2)
        {
            return GetLegGaps(eventDB, controlId1, controlId2, QueryEvent.FindLeg(eventDB, controlId1, controlId2));
        }

        // Find the SymPath of the path between controls, taking any bends into account. If no bends, the path is just the
        // simple path between the controls.
        public static SymPath GetLegPath(EventDB eventDB, Id<ControlPoint> controlId1, Id<ControlPoint> controlId2, Id<Leg> legId)
        {
            PointF location1 = eventDB.GetControl(controlId1).location;
            PointF location2 = eventDB.GetControl(controlId2).location;

            if (legId.IsNotNone) {
                Leg leg = eventDB.GetLeg(legId);
                Debug.Assert(leg.controlId1 == controlId1 && leg.controlId2 == controlId2);

                if (leg.bends != null) {
                    List<PointF> points = new List<PointF>();
                    points.Add(location1);
                    points.AddRange(leg.bends);
                    points.Add(location2);
                    return new SymPath(points.ToArray());
                }
            }

            // No bends.
            return new SymPath(new PointF[] { location1, location2 });
        }

        // Find the SymPath of the path between controls, taking any bends into account. If no bends, the path is just the
        // simple path between the controls.
        public static SymPath GetLegPath(EventDB eventDB, Id<ControlPoint> controlId1, Id<ControlPoint> controlId2)
        {
            return GetLegPath(eventDB, controlId1, controlId2, QueryEvent.FindLeg(eventDB, controlId1, controlId2));
        }

        // Get the set of courses that a special is displayed on.
        public static CourseDesignator[] GetSpecialDisplayedCourses(EventDB eventDB, Id<Special> specialId)
        {
            Special special = eventDB.GetSpecial(specialId);

            if (special.allCourses) {
                // special is on all courses. Return an array with all courses in it.
                List<CourseDesignator> list = new List<CourseDesignator>();
                foreach (Id<Course> courseId in SortedCourseIds(eventDB)) {
                    list.Add(new CourseDesignator(courseId));
                }
                if (special.kind == SpecialKind.Descriptions) {
                    // Descriptions also are on all controls separatedly.
                    list.Add(CourseDesignator.AllControls);
                }
                return list.ToArray();
            }
            else {
                return (CourseDesignator[]) Util.CloneArrayAndElements(special.courses);       // clone so that changes don't affect it.
            }
        }

        // Get the number of courses.
        public static int CountCourses(EventDB eventDB)
        {
            return eventDB.AllCourses.Count;
        }

        public static bool AnyMultipartCourses(EventDB eventDB)
        {
            bool anyMultipart = false;
            foreach (Id<Course> courseId in eventDB.AllCourseIds) {
                anyMultipart |= (CountCourseParts(eventDB, courseId) > 1);
            }

            return anyMultipart;
        }

        // Get all course IDs, in the correct sorted order.
        public static Id<Course>[] SortedCourseIds(EventDB eventDB)
        {
            List<Id<Course>> allCourseIds = new List<Id<Course>>();
            foreach (Id<Course> courseId in eventDB.AllCourseIds) {
                allCourseIds.Add(courseId);
            }

            // Sort by sortOrder field on the Course objects.
            allCourseIds.Sort(delegate(Id<Course> courseId1, Id<Course> courseId2) {
                return eventDB.GetCourse(courseId1).sortOrder.CompareTo(eventDB.GetCourse(courseId2).sortOrder);
            });

            return allCourseIds.ToArray();
        }

        // Get auto numbering values.
        public static void GetAutoNumbering(EventDB eventDB, out int firstCode, out bool disallowInvertibleCodes)
        {
            Event ev = eventDB.GetEvent();
            firstCode = ev.firstControlCode;
            disallowInvertibleCodes = ev.disallowInvertibleCodes;
        }

        // Get all the punch patterns in the whole event into a dictionary, indexed by code.
        public static Dictionary<string, PunchPattern> GetAllPunchPatterns(EventDB eventDB)
        {
            Dictionary<string, PunchPattern> result = new Dictionary<string, PunchPattern>();

            foreach (ControlPoint control in eventDB.AllControlPoints) {
                if (control.code != null) {
                    PunchPattern pattern = control.punches;
                    if (pattern != null)
                        pattern = (PunchPattern) control.punches.Clone();
                    result.Add(control.code, pattern);
                }
            }

            return result;
        }

        // Get the print area for a course, or for all controls if CourseId is none.
        // If none is defined, returns the default one.
        public static RectangleF GetPrintArea(EventDB eventDB, CourseDesignator courseDesignator, RectangleF defaultPrintArea)
        {
            RectangleF printArea;

            if (courseDesignator.IsAllControls)
                printArea = eventDB.GetEvent().printArea;
            else {
                Course course = eventDB.GetCourse(courseDesignator.CourseId);
                printArea = course.printArea;
                if (! courseDesignator.AllParts && course.partPrintAreas.ContainsKey(courseDesignator.Part))
                    printArea = course.partPrintAreas[courseDesignator.Part];
            }

            if (printArea.IsEmpty)
                return defaultPrintArea;
            else
                return printArea;
        }

        // Gets the custom symbol text/key dictionary. Makes a copy of them, so that changes don't cause weird effects.
        public static void GetCustomSymbolText(EventDB eventDB, out Dictionary<string, List<SymbolText>> customSymbolText, out Dictionary<string, bool> customSymbolKey)
        {
            Event ev = eventDB.GetEvent();

            customSymbolText = Util.CopyDictionary(ev.customSymbolText);
            customSymbolKey = Util.CopyDictionary(ev.customSymbolKey);
        }

        // Get the description language.
        public static string GetDescriptionLanguage(EventDB eventDB)
        {
            Event ev = eventDB.GetEvent();

            return ev.descriptionLangId;
        }

        // Get the event title, with particular string for newlines.
        public static string GetEventTitle(EventDB eventDB, string lineSep)
        {
            Event ev = eventDB.GetEvent();
            return ev.title.Replace("|", lineSep);            // In internal storage, | is used as line seperator.
        }
    }
}
