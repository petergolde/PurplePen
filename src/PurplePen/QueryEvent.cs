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
        // Number of meters a control must move to get a warning.
        const float MOVE_THRESHOLD = 50;

        // Determine if a code is in use in the database.
        public static bool IsCodeInUse(EventDB eventDB, string code)
        {
            return FindCode(eventDB, code).IsNotNone;
        }

        // Find the control with a code, and return its ID. Else return None.
        public static Id<ControlPoint> FindCode(EventDB eventDB, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Id<ControlPoint>.None;

            foreach (Id<ControlPoint> controlId in eventDB.AllControlPointIds) {
                if (eventDB.GetControl(controlId).code == code)
                    return controlId;
            }

            return Id<ControlPoint>.None;
        }

        // Enumerate all the course controls ids for a particular course.
        public static IEnumerable<Id<CourseControl>> EnumCourseControlIds(EventDB eventDB, CourseDesignator courseDesignator)
        {
            return EnumCourseControlIdsWithPercent(eventDB, courseDesignator).Select(ccwp => ccwp.courseControlId);
        }

        // Enumerate all the course control ids for a particular course. If the course is "all variations" for a course
        // with variations, also compute the fraction of the variations for which a particular course control is visited.
        private static IEnumerable<CourseControlWithPercent> EnumCourseControlIdsWithPercent(EventDB eventDB, CourseDesignator courseDesignator)
        {
            Debug.Assert(courseDesignator.IsNotAllControls);

            Id<Course> courseId = courseDesignator.CourseId;
            int part = courseDesignator.Part;
            Id<CourseControl> firstCourseControlId = eventDB.GetCourse(courseId).firstCourseControl;
            int currentPart = 0;

            IEnumerable<Id<CourseControl>> variationChoices;
            if (! courseDesignator.IsVariation)
                variationChoices = null;
            else
                variationChoices = courseDesignator.VariationInfo.Path.Choices;

            return EnumCourseControlsToJoin(eventDB, courseDesignator, firstCourseControlId, Id<CourseControl>.None, variationChoices, false, currentPart, 1.0);
        }

        struct CourseControlWithPercent
        {
            public Id<CourseControl> courseControlId;
            public double visitFraction;  // 1.0 if always visition, 0.5 is visit on half of loops, etc.

            public CourseControlWithPercent(Id<CourseControl> courseControlId, double visitFraction)
            {
                this.courseControlId = courseControlId;
                this.visitFraction = visitFraction;
            }
        }

        private static List<CourseControlWithPercent> EnumCourseControlsToJoin(EventDB eventDB, CourseDesignator courseDesignator, Id<CourseControl> start, Id<CourseControl> join,
                                                                        IEnumerable<Id<CourseControl>> variationChoices, bool ignoreFirstSplit, int currentPart, double currentFraction)
        {
            List<CourseControlWithPercent> result = new List<CourseControlWithPercent>();

            int part = courseDesignator.Part;
            Id<CourseControl> nextCourseControlId = start;
            bool first = true;

            // Traverse the course control links.
            while (nextCourseControlId.IsNotNone && nextCourseControlId != join) {
                CourseControl courseCtl = eventDB.GetCourseControl(nextCourseControlId);

                if (courseCtl.split && !(first && ignoreFirstSplit)) {
                    if (variationChoices != null && variationChoices.Any()) {
                        // Follow the path given by the variantChoices. May be the same as this control.
                        Id<CourseControl> choice = variationChoices.First();
                        Debug.Assert(courseCtl.splitCourseControls.Contains(choice));

                        variationChoices = variationChoices.Skip(1);

                        nextCourseControlId = choice;
                        courseCtl = eventDB.GetCourseControl(nextCourseControlId);
                    }
                    else {
                        double newFraction;
                        if (courseCtl.loop) 
                            newFraction = currentFraction;// loops traverse all parts at some time.
                        else
                            newFraction = currentFraction / courseCtl.splitCourseControls.Length;

                        // This could be simplified without the if and the AddRange after, but I'm trying to keep
                        // things in the exact order they were before.
                        for (int i = 0; i < courseCtl.splitCourseControls.Length; ++i) {
                            if (courseCtl.splitCourseControls[i] != nextCourseControlId) {
                                result.AddRange(EnumCourseControlsToJoin(eventDB, courseDesignator, courseCtl.splitCourseControls[i],
                                                courseCtl.splitEnd, variationChoices, true, currentPart, newFraction));
                            }
                        }

                        if (!courseCtl.loop) {
                            result.AddRange(EnumCourseControlsToJoin(eventDB, courseDesignator, nextCourseControlId,
                                            courseCtl.splitEnd, variationChoices, true, currentPart, newFraction));

                            nextCourseControlId = courseCtl.splitEnd;
                            if (nextCourseControlId.IsNone || nextCourseControlId == join)
                                break;
                            continue;
                        }
                    }
                }

                if (courseDesignator.AllParts || currentPart == part)
                    result.Add(new CourseControlWithPercent(nextCourseControlId, currentFraction));

                if (courseCtl.exchange) {
                    ++currentPart;
                    if (!courseDesignator.AllParts && currentPart == part)
                        result.Add(new CourseControlWithPercent(nextCourseControlId, currentFraction));
                }

                nextCourseControlId = courseCtl.nextCourseControl;
                first = false;
            }

            return result;
        }

        struct CourseControlAndSplitStart
        {
            public readonly Id<CourseControl> courseControlId;
            public readonly Id<CourseControl> splitStart;

            public CourseControlAndSplitStart(Id<CourseControl> courseControlId, Id<CourseControl> splitStart)
            {
                this.courseControlId = courseControlId;
                this.splitStart = splitStart;
            }
        }

        // Enumerate all the course controls id and corresponding split starts for a particular course.
        private static List<CourseControlAndSplitStart> EnumCourseControlsAndSplitStarts(EventDB eventDB, Id<Course> courseId)
        {
            Debug.Assert(courseId.IsNotNone);

            Id<CourseControl> firstCourseControlId = eventDB.GetCourse(courseId).firstCourseControl;

            return EnumCourseControlsAndSplitStartsToJoin(eventDB, courseId, firstCourseControlId, Id<CourseControl>.None, Id<CourseControl>.None);
        }

        private static List<CourseControlAndSplitStart> EnumCourseControlsAndSplitStartsToJoin(EventDB eventDB, Id<Course> courseId, Id<CourseControl> begin, Id<CourseControl> join, Id<CourseControl> splitStart)
        {
            List<CourseControlAndSplitStart> result = new List<CourseControlAndSplitStart>();

            Id<CourseControl> nextCourseControlId = begin;

            // Traverse the course control links.
            while (nextCourseControlId.IsNotNone && nextCourseControlId != join) {
                CourseControl courseCtl = eventDB.GetCourseControl(nextCourseControlId);

                if (courseCtl.split) {
                    // Follow all of the alternate paths 
                    for (int i = 0; i < courseCtl.splitCourseControls.Length; ++i) {
                        if (! (courseCtl.loop && i == 0)) {
                            Id<CourseControl> forkStart = courseCtl.splitCourseControls[i];
                            result.Add(new CourseControlAndSplitStart(forkStart, forkStart));
                            var splitControls = EnumCourseControlsAndSplitStartsToJoin(eventDB, courseId, eventDB.GetCourseControl(forkStart).nextCourseControl, courseCtl.splitEnd, forkStart);
                            result.AddRange(splitControls);
                        }
                    }

                    if (!courseCtl.loop) {
                        nextCourseControlId = courseCtl.splitEnd;
                    }
                    else {
                        nextCourseControlId = courseCtl.nextCourseControl;
                    }
                }
                else {
                    result.Add(new CourseControlAndSplitStart(nextCourseControlId, splitStart));
                    nextCourseControlId = courseCtl.nextCourseControl;
                }
            }

            return result;
        }

        // Get the fork start control that started the fork that courseControlId is one, or None if none.
        public static Id<CourseControl> GetForkStart(EventDB eventDB, Id<Course> courseId, Id<CourseControl> courseControlId)
        {
            List<CourseControlAndSplitStart> allSplitStarts = EnumCourseControlsAndSplitStarts(eventDB, courseId);
            foreach (CourseControlAndSplitStart ccAndSs in allSplitStarts) {
                if (ccAndSs.courseControlId == courseControlId)
                    return ccAndSs.splitStart;
            }

            return Id<CourseControl>.None;
        }

        // Get the next course control 
        private static Id<CourseControl> GetNextControl(EventDB eventDB, CourseDesignator courseDesignator, Id<CourseControl> courseControlId)
        {
            Id<CourseControl> next = eventDB.GetCourseControl(courseControlId).nextCourseControl;

            return next;
            /*
            // Simple case, the next control is not starting a split.
            if (next.IsNone || !eventDB.GetCourseControl(next).split)
                return next;

            
            // If it does, then we have to do the complex thing.
            List<Id<CourseControl>> allCourseControls = EnumCourseControlIds(eventDB, courseDesignator).ToList();
            for (int i = 0; i < allCourseControls.Count; ++i) {
                if (allCourseControls[i] == courseControlId) {
                    if (i < allCourseControls.Count - 1)
                        return allCourseControls[i + 1];
                    else
                        return Id<CourseControl>.None;
                }
            }

            throw new Exception("Course does not contain give course control");
            */
        }

        // Describes the information about a leg.
        public struct LegInfo
        {
            public Id<CourseControl> courseControlId1;
            public Id<CourseControl> courseControlId2;
            public double visitFraction;

            public LegInfo(Id<CourseControl> courseControlId1, Id<CourseControl> courseControlId2)
            {
                this.courseControlId1 = courseControlId1;
                this.courseControlId2 = courseControlId2;
                this.visitFraction = 1.0;
            }

            public LegInfo(Id<CourseControl> courseControlId1, Id<CourseControl> courseControlId2, double visitFraction)
            {
                this.courseControlId1 = courseControlId1;
                this.courseControlId2 = courseControlId2;
                this.visitFraction = visitFraction;
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
            foreach (CourseControlWithPercent ccwp in EnumCourseControlIdsWithPercent(eventDB, courseDesignator)) {
                Id<CourseControl> courseControlId = ccwp.courseControlId;
                CourseControl courseControl = eventDB.GetCourseControl(courseControlId);
                if (first || courseDesignator.AllParts || !courseControl.exchange) {
                    Id<CourseControl> nextCourseControlId = GetNextControl(eventDB, courseDesignator, courseControlId);
                    if (nextCourseControlId.IsNotNone) {
                        yield return new LegInfo(courseControlId, nextCourseControlId, ccwp.visitFraction);
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

        public static bool CourseUsesLeg(EventDB eventDB, CourseDesignator courseDesignator, Id<ControlPoint> control1, Id<ControlPoint> control2)
        {
            foreach (LegInfo leg in EnumLegs(eventDB, courseDesignator)) {
                if (eventDB.GetCourseControl(leg.courseControlId1).control == control1 &&
                    eventDB.GetCourseControl(leg.courseControlId2).control == control2) {
                    return true;
                }
            }

            return false;
        }

        // Determine if you should warn about moving a shared course control. If a normal control is being 
        // moved more than 75 meters, and is in other courses, then warn. 
        // Returns null to not warn, or array of other courses to warn.
        public static Id<Course>[] ShouldWarnAboutMovingControl(EventDB eventDB, Id<Course> courseId, Id<CourseControl> courseControlId, PointF newLocation)
        {
            Id<ControlPoint> controlId = eventDB.GetCourseControl(courseControlId).control;

            Debug.Assert(CourseUsesControl(eventDB, new CourseDesignator(courseId), controlId));

            if (eventDB.GetControl(controlId).kind != ControlPointKind.Normal)
                return null;

            float distance = DistanceBetweenPointsInMeters(eventDB, eventDB.GetControl(controlId).location, newLocation);
            if (distance < MOVE_THRESHOLD)
                return null;

            List<Id<Course>> list = new List<Id<Course>>();

            foreach (Id<Course> containingCourseId in SortedCourseIds(eventDB)) {
                if (containingCourseId != courseId &&
                    CourseUsesControl(eventDB, new CourseDesignator(containingCourseId), controlId)) {
                    list.Add(containingCourseId);
                }
            }

            if (list.Count == 0)
                return null;
            else
                return list.ToArray();
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
                        eventDB.GetCourseControl(leg.courseControlId2).control == control2) {
                        list.Add(courseId);
                        break;
                    }
                }
            }

            return list.ToArray();
        }

        public static bool CourseIsForked(EventDB eventDB, CourseDesignator courseDesignator)
        {
            foreach (Id<CourseControl> courseControlId in EnumCourseControlIds(eventDB, courseDesignator)) {
                if (eventDB.GetCourseControl(courseControlId).split)
                    return true;
            }

            return false;
        }

        // Get the number of parts that this course has.  A course with no map exchanges has 1 part, with one
        // map exchange has 2 parts, etc.
        public static int CountCourseParts(EventDB eventDB, Id<Course> courseId)
        {
            return CountCourseParts(eventDB, new CourseDesignator(courseId));
        }

        // Does this course have any map exchanges. This can return true event if the course has
        // variations, which don't allow access parts of all variations.
        public static bool HasAnyMapExchanges(EventDB eventDB, Id<Course> courseId)
        {
            foreach (Id<CourseControl> courseControlId in EnumCourseControlIds(eventDB, new CourseDesignator(courseId))) {
                if (eventDB.GetCourseControl(courseControlId).exchange)
                    return true;
            }

            return false;
        }

        // Get the number of parts that this course has.  A course with no map exchanges has 1 part, with one
        // map exchange has 2 parts, etc.
        public static int CountCourseParts(EventDB eventDB, CourseDesignator courseDesignator)
        {
            if (courseDesignator.IsAllControls)
                return 1;

            courseDesignator = courseDesignator.WithAllParts();

            // The All Variations course designator does not have "parts" as such.
            if (HasVariations(eventDB, courseDesignator.CourseId) && !courseDesignator.IsVariation)
                return 1;

            int currentPart = 0;

            foreach (Id<CourseControl> courseControlId in EnumCourseControlIds(eventDB, courseDesignator)) {
                if (eventDB.GetCourseControl(courseControlId).exchange)
                    ++currentPart;
            }

            return currentPart + 1;
        }

        // Enumerator all course designators in a list of course ids, possibly enumerating parts separately.
        public static IEnumerable<CourseDesignator> EnumerateCourseDesignators(EventDB eventDB, Id<Course>[] courseIds, bool enumeratePartsSeparately)
        {
            foreach (Id<Course> courseId in courseIds) {
                if (courseId.IsNotNone && enumeratePartsSeparately && QueryEvent.CountCourseParts(eventDB, courseId) > 1) {
                    // Create files for each part.
                    for (int part = 0; part < QueryEvent.CountCourseParts(eventDB, courseId); ++part) {
                        yield return new CourseDesignator(courseId, part);
                    }
                }
                else {
                    yield return new CourseDesignator(courseId);
                }
            }
        }

        // Enumerator all course designators in a list of course ids, possibly enumerating parts separately.
        public static IEnumerable<CourseDesignator> EnumerateCourseDesignators(EventDB eventDB, Id<Course>[] courseIds, 
                      Dictionary<Id<Course>, VariationChoices> variationChoicesPerCourse, bool enumeratePartsSeparately)
        {
            foreach (Id<Course> courseId in courseIds) {
                if (QueryEvent.HasVariations(eventDB, courseId)) {
                    VariationChoices variationChoices = variationChoicesPerCourse.ContainsKey(courseId) ? variationChoicesPerCourse[courseId] : new VariationChoices();
                    foreach (CourseDesignator courseDesignator in QueryEvent.GetDesignatorsFromVariationChoices(eventDB, courseId, variationChoices)) {
                        if (enumeratePartsSeparately && QueryEvent.CountCourseParts(eventDB, courseDesignator) > 1) {
                            // Create files for each part.
                            for (int part = 0; part < QueryEvent.CountCourseParts(eventDB, courseDesignator); ++part) {
                                yield return new CourseDesignator(courseDesignator.CourseId, courseDesignator.VariationInfo, part);
                            }
                        }
                        else {
                            yield return courseDesignator;
                        }

                    }
                }
                else {
                    // No variation.
                    int numberOfParts = QueryEvent.CountCourseParts(eventDB, new CourseDesignator(courseId));
                    if (courseId.IsNotNone && enumeratePartsSeparately && numberOfParts > 1) {
                        // Create files for each part.
                        for (int part = 0; part < numberOfParts; ++part) {
                            yield return new CourseDesignator(courseId, part);
                        }
                    }
                    else {
                        yield return new CourseDesignator(courseId);
                    }
                }
            }
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

        // Get a textual version of a list of courses.
        public static string CourseList(EventDB eventDB, IEnumerable<Id<Course>> courses)
        {
            StringBuilder courseNames = new StringBuilder();
            bool first = true;

            foreach (Id<Course> courseId in courses) {
                if (!first)
                    courseNames.Append(", ");
                courseNames.Append(eventDB.GetCourse(courseId).name);
                first = false;
            }

            return courseNames.ToString();
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
        private static int ComputeLoad(EventDB eventDB, Id<ControlPoint> controlId, Id<Course>[] courses)
        {
            bool anyLoadFound = false;
            int totalLoad = 0;

            foreach (Id<Course> courseId in courses) {
                int load = eventDB.GetCourse(courseId).load;
                if (load >= 0) {
                    anyLoadFound = true;
                    if (HasVariations(eventDB, courseId)) {
                        // If this course is a relay, then computer what percent of variations use this control.
                        double variationPercent = ComputeLoadFraction(eventDB, courseId, controlId);
                        totalLoad += (int)Math.Ceiling(variationPercent * load);
                    }
                    else {
                        totalLoad += load;
                    }
                }
            }

            if (anyLoadFound)
                return totalLoad;
            else
                return -1;
        }

        // Given an array of courses, compute the control visit. Return -1 if no control load set for any containing courses, or array is empty.
        // Just like ComputeLoad, but counts visits with multiplicity.
        private static int ComputeVisits(EventDB eventDB, Id<ControlPoint> controlId, Id<Course>[] courses)
        {
            bool anyLoadFound = false;
            int totalLoad = 0;

            foreach (Id<Course> courseId in courses) {
                int load = eventDB.GetCourse(courseId).load;
                if (load >= 0) {
                    anyLoadFound = true;
                    double variationPercent = ComputeVisitFraction(eventDB, courseId, controlId);
                    totalLoad += (int)Math.Ceiling(variationPercent * load);
                }
            }

            if (anyLoadFound)
                return totalLoad;
            else
                return -1;
        }

        // Given an array of courses, compute the control load. Return -1 if no control load set for any containing courses, or array is empty.
        private static int ComputeLoad(EventDB eventDB, Id<ControlPoint> controlId1, Id<ControlPoint> controlId2, Id<Course>[] courses)
        {
            bool anyLoadFound = false;
            int totalLoad = 0;

            foreach (Id<Course> courseId in courses) {
                int load = eventDB.GetCourse(courseId).load;
                if (load >= 0) {
                    anyLoadFound = true;
                    if (HasVariations(eventDB, courseId)) {
                        // If this course is a relay, then computer what percent of variations use this control.
                        double variationPercent = ComputeLoadFraction(eventDB, courseId, controlId1, controlId2);
                        totalLoad += (int)Math.Ceiling(variationPercent * load);
                    }
                    else {
                        totalLoad += load;
                    }
                }
            }

            if (anyLoadFound)
                return totalLoad;
            else
                return -1;
        }

        // Determine what fract of the variations of a course use the given controlId. Returns 1 if all variations use the control, 
        // 0.5 if half the variations use the control, etc.
        private static double ComputeLoadFraction(EventDB eventDB, Id<Course> courseId, Id<ControlPoint> controlId)
        {
            return (from c in EnumCourseControlIdsWithPercent(eventDB, new CourseDesignator(courseId))
                    where eventDB.GetCourseControl(c.courseControlId).control == controlId
                    let loadFraction = LoadFraction(eventDB, c)
                    select (double?) loadFraction).Max() ?? 0;
        }

        // Determine what fract of the variations of a course use the given controlId, counting multiple visits with multiplicity. 
        //Returns 1 if all variations use the control, 0.5 if half the variations use the control, 2 if all variations use the control twice
        private static double ComputeVisitFraction(EventDB eventDB, Id<Course> courseId, Id<ControlPoint> controlId)
        {
            var temp = EnumCourseControlIdsWithPercent(eventDB, new CourseDesignator(courseId));

            return (from c in EnumCourseControlIdsWithPercent(eventDB, new CourseDesignator(courseId))
                    where eventDB.GetCourseControl(c.courseControlId).control == controlId
                    select (double?)c.visitFraction).Sum() ?? 0;
        }

        // For non-loop split controls, multiply the visit fraction times the number of forks because of the way that
        // multiple course controls are used for the split.
        private static double LoadFraction(EventDB eventDB, CourseControlWithPercent c)
        {
            CourseControl cc = eventDB.GetCourseControl(c.courseControlId);
            if (cc.split && !cc.loop)
                return c.visitFraction * cc.splitCourseControls.Length;
            else
                return c.visitFraction;
        }

        // Determine what fract of the variations of a course use the given leg. Returns 1 if all variations use the control, 
        // 0.5 if half the variations use the control, etc.
        private static double ComputeLoadFraction(EventDB eventDB, Id<Course> courseId, Id<ControlPoint> control1, Id<ControlPoint> control2)
        {
            return (from l in EnumLegs(eventDB, new CourseDesignator(courseId))
                    where eventDB.GetCourseControl(l.courseControlId1).control == control1 &&
                          eventDB.GetCourseControl(l.courseControlId2).control == control2
                    let loadFraction = l.visitFraction
                    select (double?)loadFraction).Max() ?? 0;
        }

        // Does courseControl1 precede courseControl2 in the given course.
        public static bool DoesCourseControlPrecede(EventDB eventDB, CourseDesignator courseDesignator, Id<CourseControl> courseControl1, Id<CourseControl> courseControl2)
        {
            bool sawFirst = false, sawSecond = false;

            foreach (Id<CourseControl> id in EnumCourseControlIds(eventDB, courseDesignator)) {
                if (id == courseControl1)
                    sawFirst = true;
                else if (sawFirst && id == courseControl2)
                    sawSecond = true;
            }

            return (sawFirst && sawSecond);
        }

        // What is the control load for this control. Return -1 if not used in any courses that have a load set for them.
        public static int GetControlLoad(EventDB eventDB, Id<ControlPoint> controlId)
        {
            Id<Course>[] courses = CoursesUsingControl(eventDB, controlId);
            return ComputeLoad(eventDB, controlId, courses);
        }

        // What is the visit load for this control. Return -1 if not used in any courses that have a load set for them.
        // Counts multiple visits with multiplicity.
        public static int GetControlVisitLoad(EventDB eventDB, Id<ControlPoint> controlId)
        {
            Id<Course>[] courses = CoursesUsingControl(eventDB, controlId);
            return ComputeVisits(eventDB, controlId, courses);
        }

        // What is the load for this leg. Return -1 if not used in any courses that have a load set for them.
        public static int GetLegLoad(EventDB eventDB, Id<ControlPoint> control1, Id<ControlPoint> control2)
        {
            Id<Course>[] courses = CoursesUsingLeg(eventDB, control1, control2);
            return ComputeLoad(eventDB, control1, control2, courses);
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
            if (code == null || code.Length < 1) {
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

            courseDesignator = courseDesignator.WithAllVariations();

            if (courseDesignator.AllParts && special.kind != SpecialKind.Descriptions)
                return special.courses.Any(cd => cd.CourseId == courseDesignator.CourseId);
            else
                return special.courses.Contains(courseDesignator) || special.courses.Contains(new CourseDesignator(courseDesignator.CourseId));
        }


        // Get the gaps in a control for a given scale.
        // Returns null if no gaps defined for that scale.
        public static CircleGap[] GetControlGaps(EventDB eventDB, Id<ControlPoint> controlId, float scale)
        {
            int scaleInt = (int)Math.Round(scale);

            ControlPoint control = eventDB.GetControl(controlId);
            if (control.gaps == null)
                return null;
            else if (!control.gaps.ContainsKey(scaleInt))
                return null;
            else {
                return control.gaps[scaleInt];
            }
        }



        // Finds where a new regular control would be inserted into an existing course. courseControl1 and courseControl2 can either or both be none, to identify
        // a leg to insert into, a control to insert after, or no information about where to insert. Updates courseControl1 and courseControl2 to identify exactly
        // where on the course the control should be inserted as follows:
        //     If inserting between two course controls -- these are denoted by courseControl1 and courseControl2
        //     If inserting as last course control -- courseControl1 is the current last control and courseControl2 is None  (only occurs when there is no finish)
        //     If inserting as first course control -- courseControl2 is None and courseControl2 is current first control (only occurs when there is no start)
        //     If inserting as only course control -- both are none (only occurs if course is currently empty)
        public static void FindControlInsertionPoint(EventDB eventDB, CourseDesignator courseDesignator, 
                                                     ref Id<CourseControl> courseControl1, ref Id<CourseControl> courseControl2,
                                                     ref LegInsertionLoc legInsertionLoc)
        {
            Id<Course> courseId = courseDesignator.CourseId;

            if (courseControl1.IsNotNone && courseControl2.IsNotNone) {
                CourseControl cc1 = eventDB.GetCourseControl(courseControl1);
                CourseControl cc2 = eventDB.GetCourseControl(courseControl2);

                if (cc1.nextCourseControl != courseControl2) {
                    Debug.Assert(cc2.split && cc2.splitCourseControls.Contains(courseControl2));
                    courseControl2 = cc1.nextCourseControl;
                }

                return;
            }
            else {
                // Adding after courseControl1. If none, or a finish control, add at end, before the finish control if any.
                if (courseControl1.IsNone || eventDB.GetControl(eventDB.GetCourseControl(courseControl1).control).kind == ControlPointKind.Finish)
                    courseControl1 = QueryEvent.LastCourseControl(eventDB, courseId, true);

                if (courseControl1.IsNone) {
                    // Empty course or adding at start.
                    courseControl2 = eventDB.GetCourse(courseId).firstCourseControl;
                    legInsertionLoc = LegInsertionLoc.Normal;
                    return;
                }
                else {
                    // Adding after courseControl1.
                    CourseControl before = (CourseControl)eventDB.GetCourseControl(courseControl1);
                    courseControl2 = before.nextCourseControl;
                    if (before.split && !before.loop)
                        legInsertionLoc = LegInsertionLoc.PreSplit;
                    else
                        legInsertionLoc = LegInsertionLoc.Normal;
                    return;
                }
            }
        }

        // Get the real world distance, in meters, between two points.
        public static float DistanceBetweenPointsInMeters(EventDB eventDB, PointF pt1, PointF pt2)
        {
            return (float)((eventDB.GetEvent().mapScale * Geometry.Distance(pt1, pt2)) / 1000.0);
        }

        // Compute the length of a leg between two controls, in meters. The indicated leg id, if non-zero, is used
        // to get bend information. The event map scale converts between the map scale, in mm, which is used
        // for the coordinate information, to meters in the world scale.
        public static float ComputeLegLength(EventDB eventDB, Id<ControlPoint> controlId1, Id<ControlPoint> controlId2, Id<Leg> legId)
        {
            PointF location1 = eventDB.GetControl(controlId1).location;
            PointF location2 = eventDB.GetControl(controlId2).location;

            SymPath path = GetLegPath(eventDB, controlId1, controlId2, legId);
            return (float)((eventDB.GetEvent().mapScale * path.Length) / 1000.0);
        }

        // Compute the distance between two control points, in meters. The controls need not be part of a leg, and if they are, bends
        // in the leg are NOT taken into account.
        public static float ComputeStraightLineControlDistance(EventDB eventDB, Id<ControlPoint> controlId1, Id<ControlPoint> controlId2)
        {
            PointF location1 = eventDB.GetControl(controlId1).location;
            PointF location2 = eventDB.GetControl(controlId2).location;

            return DistanceBetweenPointsInMeters(eventDB, location1, location2);
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
                return (float)((eventDB.GetEvent().mapScale * Geometry.Distance(location1, location2)) / 1000.0);
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

                return (float)((eventDB.GetEvent().mapScale * dist) / 1000.0);
            }
        }

        // Get Length of a special, in meters.
        public static float ComputeSpecialLength(EventDB eventDB, Id<Special> specialId)
        {
            Special special = eventDB.GetSpecial(specialId);
            SymPath path = new SymPath(special.locations);
            return (float)((eventDB.GetEvent().mapScale * path.Length) / 1000.0);
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
                return (leg.gaps == null) ? null : (LegGap[])leg.gaps.Clone();
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

                // Also on all controls.
                list.Add(CourseDesignator.AllControls);

                return list.ToArray();
            }
            else {
                return (CourseDesignator[])Util.CloneArrayAndElements(special.courses);       // clone so that changes don't affect it.
            }
        }

        // Get the number of courses.
        public static int CountCourses(EventDB eventDB)
        {
            return eventDB.AllCourses.Count;
        }

        public static bool AnyMultipartCourses(EventDB eventDB)
        {
            foreach (CourseControl courseControl in eventDB.AllCourseControls) {
                if (courseControl.exchange)
                    return true;
            }

            return false;
        }

        // Get all course IDs, in the correct sorted order.
        public static Id<Course>[] SortedCourseIds(EventDB eventDB)
        {
            List<Id<Course>> allCourseIds = new List<Id<Course>>();
            foreach (Id<Course> courseId in eventDB.AllCourseIds) {
                allCourseIds.Add(courseId);
            }

            // Sort by sortOrder field on the Course objects.
            allCourseIds.Sort(delegate (Id<Course> courseId1, Id<Course> courseId2) {
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
                        pattern = (PunchPattern)control.punches.Clone();
                    result.Add(control.code, pattern);
                }
            }

            return result;
        }

        // Get the print area for a course, or for all controls if CourseId is none.
        public static PrintArea GetPrintArea(EventDB eventDB, CourseDesignator courseDesignator)
        {
            PrintArea printArea;

            if (courseDesignator.IsAllControls)
                printArea = eventDB.GetEvent().printArea;
            else {
                Course course = eventDB.GetCourse(courseDesignator.CourseId);
                printArea = course.printArea;
                if (!courseDesignator.AllParts && course.partPrintAreas.ContainsKey(courseDesignator.Part))
                    printArea = course.partPrintAreas[courseDesignator.Part];
            }

            return printArea;
        }

        // Returns true if the course designator is a specific part and that part has a custom print area
        // just for that part.
        public static bool HasPartSpecificPrintArea(EventDB eventDB, CourseDesignator courseDesignator)
        {
            if (courseDesignator.IsAllControls)
                return false;
            else {
                return (!courseDesignator.AllParts && eventDB.GetCourse(courseDesignator.CourseId).partPrintAreas.ContainsKey(courseDesignator.Part));
            }
        }

        // Get the part options for a specific course part. Returns null for all controls.
        public static PartOptions GetPartOptions(EventDB eventDB, CourseDesignator courseDesignator)
        {
            if (courseDesignator.IsAllControls)
                return null;

            Course course = eventDB.GetCourse(courseDesignator.CourseId);
            PartOptions partOptions;

            if (!course.partOptions.TryGetValue(courseDesignator.Part, out partOptions)) {
                partOptions = PartOptions.Default;
            }

            // Show Finish is always true for the last part of the course.
            if (courseDesignator.Part == QueryEvent.CountCourseParts(eventDB, courseDesignator) - 1) {
                partOptions = partOptions.Clone();
                partOptions.ShowFinish = true;
            }

            return partOptions;
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

        public static int GetDescriptionColumns(EventDB eventDB, Id<Special> specialId)
        {
            Special special = eventDB.GetSpecial(specialId);
            Debug.Assert(special.kind == SpecialKind.Descriptions);
            return special.numColumns;
        }

        // Get the event title, with particular string for newlines.
        public static string GetEventTitle(EventDB eventDB, string lineSep)
        {
            Event ev = eventDB.GetEvent();
            return ev.title.Replace("|", lineSep);            // In internal storage, | is used as line seperator.
        }

        // Get the full output file name. Uses the name of the course, removes bad characters,
        // checks for duplication of the map file name. 
        // If courseDesignator is null, uses the event title insteand.
        public static string CreateOutputFileName(EventDB eventDB, CourseDesignator courseDesignator, string filePrefix, string fileSuffix, string extension)
        {
            string basename;

            // Get the course name.
            if (courseDesignator == null)
                basename = GetEventTitle(eventDB, " ");
            else if (courseDesignator.IsAllControls)
                basename = MiscText.AllControls;
            else
                basename = eventDB.GetCourse(courseDesignator.CourseId).name;

            // Add prefix, if requested.
            if (!string.IsNullOrEmpty(filePrefix))
                basename = filePrefix + "-" + basename;

            // Add variation.
            if (courseDesignator != null && courseDesignator.IsVariation) {
                basename = basename + " " + courseDesignator.VariationInfo.Name;
            }

            // Add part.
            if (courseDesignator != null && !courseDesignator.AllParts) {
                basename = basename + "-" + (courseDesignator.Part + 1).ToString();
            }

            if (!string.IsNullOrEmpty(fileSuffix))
                basename = basename + fileSuffix;

            // Remove bad characters.
            basename = Util.FilterInvalidPathChars(basename);
            basename += extension;      // add OCAD extension.

            return basename;
        }

        // Is the given image name in use?
        public static bool IsImageNameUsed(EventDB eventDB, string imageName)
        {
            foreach (Special special in eventDB.AllSpecials) {
                if (special.kind == SpecialKind.Image && string.Equals(special.text, imageName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }

        // Get a unique image name.
        public static string UniqueImageName(EventDB eventDB, string imageName)
        {
            if (!IsImageNameUsed(eventDB, imageName))
                return imageName;

            for (int i = 1; i < 999999; ++i) {
                int lastDot = imageName.LastIndexOf('.');
                if (lastDot < 0)
                    lastDot = imageName.Length;
                string newName = imageName.Substring(0, lastDot) + "(" + i.ToString() + ")" + imageName.Substring(lastDot);
                if (!IsImageNameUsed(eventDB, newName))
                    return newName;
            }

            return imageName;
        }

        // Get all fonts used by text specials.
        public static IEnumerable<string> GetTextSpecialFonts(EventDB eventDB)
        {
            HashSet<string> fonts = new HashSet<string>();
            foreach (Special special in eventDB.AllSpecials) {
                if (special.kind == SpecialKind.Text)
                    fonts.Add(special.fontName);
            }

            return fonts;
        }

        // See if course has any variations.
        public static bool HasVariations(EventDB eventDB, Id<Course> courseId)
        {
            if (courseId.IsNone)
                return false;  // All Control has no variations.
            Course course = eventDB.GetCourse(courseId);
            if (course.kind == CourseKind.Score)
                return false;  // Score courses don't have variations.

            return EnumCourseControlIds(eventDB, new CourseDesignator(courseId)).Any(courseControlId => eventDB.GetCourseControl(courseControlId).split);
        }

        public static bool AnyCourseHasVariations(EventDB eventDB)
        {
            foreach (Id<Course> courseId in eventDB.AllCourseIds) {
                if (HasVariations(eventDB, courseId))
                    return true;
            }
            return false;
        }

        // If a course control is the beginning of a variation split, return all course controls assigned to that variation split.
        // Otherwise, return just that course control.
        public static IEnumerable<Id<CourseControl>> AllVariationsOfCourseControl(EventDB eventDB, Id<CourseControl> courseControlId)
        {
            CourseControl courseControl = eventDB.GetCourseControl(courseControlId);
            if (courseControl.split) {
                return courseControl.splitCourseControls;
            }
            else {
                return new[] { courseControlId };
            }
        }

        // Get the mapping from split course control to letter.
        public static Dictionary<Id<CourseControl>, char> GetVariantCodeMapping(EventDB eventDB, CourseDesignator courseDesignator)
        {
            Debug.Assert(!courseDesignator.IsVariation);

            char nextLetter = 'A';
            Dictionary<Id<CourseControl>, char> result = new Dictionary<Id<CourseControl>, char>();


            foreach (Id<CourseControl> courseControlId in EnumCourseControlIds(eventDB, courseDesignator)) {
                CourseControl courseControl = eventDB.GetCourseControl(courseControlId);
                if (courseControl.split) {
                    foreach (Id<CourseControl> splitId in courseControl.splitCourseControls) {
                        // The loop escape path doesn't get a letter.
                        if (!(courseControl.loop && courseControl.splitCourseControls[0] == splitId)) {
                            if (!result.ContainsKey(splitId)) {
                                result.Add(splitId, nextLetter);
                                if (nextLetter == 'Z')
                                    nextLetter = 'a';
                                else
                                    ++nextLetter;
                            }
                        }
                    }
                }
            }

            return result;
        }

        private static string GetVariationString(EventDB eventDB, IEnumerable<Id<CourseControl>> choices, Dictionary<Id<CourseControl>, char> variationMapper)
        {
            StringBuilder builder = new StringBuilder();

            foreach (Id<CourseControl> choice in choices) {
                if (variationMapper.ContainsKey(choice))
                    builder.Append(variationMapper[choice]);
            }

            return builder.ToString();
        }

        private static string GetVariationString(EventDB eventDB, Id<Course> courseId, VariationInfo.VariationPath variationPath, Dictionary<Id<CourseControl>, char> variationMapper)
        {
            return GetVariationString(eventDB, variationPath.Choices, variationMapper);
        }

        public static string GetVariationString(EventDB eventDB, Id<Course> courseId, VariationInfo.VariationPath variationPath)
        {
            Dictionary<Id<CourseControl>, char> variationMapper = GetVariantCodeMapping(eventDB, new CourseDesignator(courseId));
            return GetVariationString(eventDB, courseId, variationPath, variationMapper);
        }

        // Get all the possible variations for a given course, based on the loops/forks. Returns a list of VariationInfo, 
        // giving the code string, VariationPath, and name. Sorted by code string.
        public static IEnumerable<VariationInfo> GetAllVariations(EventDB eventDB, Id<Course> courseId)
        {
            HashSet<Id<CourseControl>> alreadyVisited = new HashSet<PurplePen.Id<PurplePen.CourseControl>>();

            CourseDesignator courseDesignator = new CourseDesignator(courseId);
            Dictionary<Id<CourseControl>, char> variationMapper = GetVariantCodeMapping(eventDB, courseDesignator);
            List<List<Id<CourseControl>>> variations = GetVariations(eventDB, courseDesignator, eventDB.GetCourse(courseId).firstCourseControl, alreadyVisited);

            List<VariationInfo> result = new List<VariationInfo>();

            // Check for no variations.
            if (variations.Count == 1 && variations[0].Count == 0)
                return result;

            foreach (var choices in variations) {
                string variationString = GetVariationString(eventDB, choices, variationMapper);
                VariationInfo.VariationPath variationPath = new VariationInfo.VariationPath(choices);
                result.Add(new VariationInfo(variationString, variationPath));
            }
            result.Sort((vi1, vi2) => string.Compare(vi1.CodeString, vi2.CodeString, StringComparison.OrdinalIgnoreCase));

            return result;
        }

        private static List<List<Id<CourseControl>>> GetVariations(EventDB eventDB, CourseDesignator courseDesignator, Id<CourseControl> start, HashSet<Id<CourseControl>> alreadyVisited)
        {
            List<List<Id<CourseControl>>> result = new List<List<Id<CourseControl>>>();
            Id<CourseControl> nextCourseControlId = start;

            // Traverse the course control links.
            while (nextCourseControlId.IsNotNone) {
                CourseControl courseCtl = eventDB.GetCourseControl(nextCourseControlId);

                if (courseCtl.split) {

                    // If its a loop, we can only continue on the loop skipping path if all other loop
                    // paths have been visited.
                    bool allLoopsVisited = true; // true if all loops in this loop are visited, or its a fork.
                    if (courseCtl.loop) {
                        for (int i = 1; i < courseCtl.splitCourseControls.Length; ++i) {
                            if (!alreadyVisited.Contains(courseCtl.splitCourseControls[i]))
                                allLoopsVisited = false;
                        }
                    }

                    for (int i = (allLoopsVisited ? 0 : 1); i < courseCtl.splitCourseControls.Length; ++i) {
                        Id<CourseControl> split = courseCtl.splitCourseControls[i];
                        Id<CourseControl> afterSplit = eventDB.GetCourseControl(split).nextCourseControl;

                        if (afterSplit.IsNotNone && !alreadyVisited.Contains(split)) {
                            // Mark this path as visited so if its part of a loop, we don't visit it again.
                            alreadyVisited.Add(split);
                            List<List<Id<CourseControl>>> tailVariants = GetVariations(eventDB, courseDesignator, afterSplit, alreadyVisited);
                            alreadyVisited.Remove(split);

                            foreach (List<Id<CourseControl>> v in tailVariants) {
                                List<Id<CourseControl>> l = new List<PurplePen.Id<PurplePen.CourseControl>>(v.Count + 1);
                                l.Add(courseCtl.splitCourseControls[i]);
                                l.AddRange(v);
                                result.Add(l);
                            }
                        }
                    }

                    break;
                }

                nextCourseControlId = courseCtl.nextCourseControl;
            }

            // If no variations found, there is one way to go.
            if (result.Count == 0)
                result.Add(new List<Id<CourseControl>>());

            return result;
        }

        // Given a VariationChoices, select all the course designators that match.
        public static IEnumerable<CourseDesignator> GetDesignatorsFromVariationChoices(EventDB eventDB, Id<Course> courseId, VariationChoices variationChoices)
        {
            switch (variationChoices.Kind) {
                case VariationChoices.VariationChoicesKind.Combined:
                    return new[] { new CourseDesignator(courseId) };

                case VariationChoices.VariationChoicesKind.AllVariations:
                    return (from vi in GetAllVariations(eventDB, courseId)
                            select new PurplePen.CourseDesignator(courseId, vi)).ToArray();

                case VariationChoices.VariationChoicesKind.ChosenVariations:
                    return (from vi in GetAllVariations(eventDB, courseId)
                            where variationChoices.ChosenVariations.Contains(vi.CodeString)
                            select new PurplePen.CourseDesignator(courseId, vi)).ToArray();

                case VariationChoices.VariationChoicesKind.ChosenTeams:
                    Course course = eventDB.GetCourse(courseId);
                    RelayVariations relayVariations = new RelayVariations(eventDB, courseId, course.relaySettings);

                    List<CourseDesignator> result = new List<CourseDesignator>();
                    for (int team = variationChoices.FirstTeam; team <= variationChoices.LastTeam; ++team) {
                        for (int leg = 1; leg <= relayVariations.NumberOfLegs; ++leg) {
                            result.Add(new CourseDesignator(courseId, relayVariations.GetVariation(team, leg)));
                        }
                    }

                    return result;

                default:
                    Debug.Fail("Bad variation choices kind");
                    return null;
            }
        }

        public enum AddVariationResult
        {
            OK,
            CantAddInAllControls,
            CantAddInScoreCourse,
            NoControlSelected,
            CantAddToLastControl,
            CantAddToFinishControl,
            VariationAlreadyExists
        }

        public static AddVariationResult CanAddVariation(EventDB eventDB, CourseDesignator courseDesignator, Id<CourseControl> courseControlId)
        {
            // Can't be all controls or score course.
            if (courseDesignator.IsAllControls)
                return AddVariationResult.CantAddInAllControls;
            Id<Course> courseId = courseDesignator.CourseId;
            if (eventDB.GetCourse(courseId).kind == CourseKind.Score)
                return AddVariationResult.CantAddInScoreCourse;

            if (courseControlId.IsNone)
                return AddVariationResult.NoControlSelected;

            CourseControl courseControl = eventDB.GetCourseControl(courseControlId);
            ControlPoint control = eventDB.GetControl(courseControl.control);

            if (control.kind == ControlPointKind.Finish)
                return AddVariationResult.CantAddToFinishControl;       // Can't add to finish control.

            // Must not be the last control in the course.
            if (QueryEvent.LastCourseControl(eventDB, courseId, false) == courseControlId)
                return AddVariationResult.CantAddToFinishControl;

            // Can't already have a variation there.
            if (courseControl.split)
                return AddVariationResult.VariationAlreadyExists;

            return AddVariationResult.OK;
        }
    }


}
