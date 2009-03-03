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
using System.Diagnostics;
using System.Globalization;
using System.Drawing;

namespace PurplePen
{
    /// <summary>
    /// A course view is a static view of all or part of a course. It is a static snapshot,
    /// and doesn't change if the underlying course changes. It also handles subsetting for
    /// map exchanges, relay variations, the all controls view, etc. It is the basis for
    /// control descriptions and the course drawing.
    /// </summary>
    class CourseView
    {
        public enum CourseViewKind {
            Normal,
            AllControls,
            Score
        };

        public class ControlView
        {
            public int ordinal;                 // Ordinal number (number in the description sheet).
                                                // 0 = start, -1 = N/A (finish, crossing, flagged route, etc).
            public char variation;              // If this is a variation control, the character A-Z of the variation.
            public Id<ControlPoint> controlId;               // ID of control in the event DB
            public Id<CourseControl> courseControlId;         // ID of course control in the event DB
            public int[] legTo;                 // Indices in the list of the control a leg should be drawn to
            public Id<Leg>[] legId;                 // If special leg information, the ID in the eventDB.
            public float[] legLength;           // Length of the leg
        };
            

        private EventDB eventDB;
        private string courseName;
        private Id<Course> courseId;

        private readonly List<ControlView> controlViews = new List<ControlView>();
        private readonly List<Id<Special>> specialIds = new List<Id<Special>>();

        private int normalControlCount;         // number of normal controls.
        private float totalPoints;              // total points.
        private float totalLength;              // total length.
        private float totalClimb;

        private float mapScale;                // scale of map
        private float printScale;               // scale to print

        public List<ControlView> ControlViews
        {
            get
            {
                return controlViews;
            }
        }

        // All the specials on this course.
        public List<Id<Special>> SpecialIds
        {
            get { return specialIds; }
        }

        public EventDB EventDB {
            get { return eventDB; }
        }

        // Get the ID of the Course in the event DB. Returns None for an All Controls view.
        public Id<Course> BaseCourseId {
            get { return courseId; }
        }

        // Get the kind of the course view.
        public CourseViewKind Kind
        {
            get
            {
                if (courseId.IsNone)
                    return CourseViewKind.AllControls;
                else {
                    Course course = eventDB.GetCourse(courseId);
                    if (course.kind == CourseKind.Score)
                        return CourseViewKind.Score;
                    else if (course.kind == CourseKind.Normal)
                        return CourseViewKind.Normal;
                    else {
                        Debug.Fail("Bad course kind"); return CourseViewKind.Normal;
                    }
                }
            }
        }

        public string CourseName
        {
            get { return courseName; }
        }

        public float TotalLength {
            get { 
                return totalLength;
            }
        }

        public float TotalScore {
            get
            {
                return totalPoints;
            }
        }

        public float TotalClimb
        {
            get
            {
                return totalClimb;
            }
        }

        public float TotalNormalControls
        {
            get
            {
                return normalControlCount;
            }
        }

        public float MapScale
        {
            get { return mapScale; }
        }

        public float PrintScale
        {
            get { return printScale; }
        }

        public float ScaleRatio
        {
            get { return printScale / mapScale; }
        }

        // Get the index of the next control. If this is a splitting control, just takes the first.
        // If no next control, then returns -1.
        public int GetNextControl(int controlIndex)
        {
            ControlView controlView = controlViews[controlIndex];

            if (controlView.legTo != null && controlView.legTo.Length > 0)
                return controlView.legTo[0];
            else
                return -1;
        }

        // Get the index of the previous control. If this is a joining control, just takes one of the controls previous.
        // If no next control, then returns -1.
        public int GetPrevControl(int controlIndex)
        {
            for (int i = controlIndex - 1; i >= 0; --i) {
                ControlView controlView = controlViews[i];
                if (controlView.legTo != null) {
                    for (int leg = 0; leg < controlView.legTo.Length; ++leg) {
                        if (controlView.legTo[leg] == controlIndex)
                            return i;
                    }
                }
            }

            return -1;
        }

        // Get the bounds of the course view. Uses a 10mm boundary around the controls.
        public RectangleF GetViewBounds()
        {
            const float BORDER = 10;      // amount of border around the controls.

            RectangleF bounds = new RectangleF();

            foreach (ControlView controlView in controlViews) {
                if (controlView.controlId.IsNotNone) {
                    PointF controlLocation = eventDB.GetControl(controlView.controlId).location;
                    RectangleF controlBounds = new RectangleF(controlLocation.X - BORDER, controlLocation.Y - BORDER, BORDER * 2, BORDER * 2);
                    if (bounds.IsEmpty)
                        bounds = controlBounds;
                    else
                        bounds = RectangleF.Union(bounds, controlBounds);
                }
            }

            return bounds;
        }

        private CourseView(EventDB eventDB, Id<Course> courseId)
        {
            this.eventDB = eventDB;
            this.courseId = courseId;
        }

        // Get the map and print scales.
        private void GetScales()
        {
            mapScale = eventDB.GetEvent().mapScale;
            printScale = QueryEvent.GetPrintScale(eventDB, courseId);
        }


        // The legTo array currently has courseControlId values in it.
        // Convert them to indices into the controlView list. Also, fills
        // in the legId array and the length lengths.
        private void UpdateLegToIndices()
        {
            for (int i = 0; i < controlViews.Count; ++i) {
                if (controlViews[i].legTo == null)
                    continue;

                controlViews[i].legId = new Id<Leg>[controlViews[i].legTo.Length];
                controlViews[i].legLength = new float[controlViews[i].legTo.Length];

                for (int legIndex = 0; legIndex < controlViews[i].legTo.Length; ++legIndex) {
                    Id<CourseControl> courseControlId = new Id<CourseControl>(controlViews[i].legTo[legIndex]);

                    if (courseControlId.IsNotNone) {
                        int j;
                        for (j = i + 1; j < controlViews.Count; ++j) {
                            if (courseControlId == controlViews[j].courseControlId) {
                                controlViews[i].legTo[legIndex] = j;
                                controlViews[i].legId[legIndex] = QueryEvent.FindLeg(eventDB, controlViews[i].controlId, controlViews[j].controlId);
                                break;
                            }
                        }

                        Debug.Assert(j < controlViews.Count);  // make sure we found it.
                    }

                    controlViews[i].legLength[legIndex] = QueryEvent.ComputeLegLength(eventDB, 
                        controlViews[i].controlId,
                        controlViews[controlViews[i].legTo[legIndex]].controlId,
                        controlViews[i].legId[legIndex]);
                }
            }
        }

        // Compute stats like total length, total score, number of controls, etc.
        private void ComputeStatistics()
        {
            totalPoints = 0;
            normalControlCount = 0;
            totalLength = 0;

            if (courseId.IsNone)
                totalClimb = -1;
            else
                totalClimb = eventDB.GetCourse(courseId).climb;

            for (int i = 0; i < controlViews.Count; ++i) {
                ControlView controlView = controlViews[i];

                if (controlView.controlId.IsNotNone) {
                    ControlPoint control = eventDB.GetControl(controlView.controlId);
                    if (control.kind == ControlPointKind.Normal)
                        ++normalControlCount;
                }

                if (controlView.courseControlId.IsNotNone) {
                    ControlPoint control = eventDB.GetControl(controlView.controlId);
                    CourseControl courseControl = eventDB.GetCourseControl(controlView.courseControlId);
                    if (control.kind == ControlPointKind.Normal && courseControl.points > 0)
                        totalPoints += courseControl.points;
                }

                // Always use the first leg for split controls.
                if (controlView.legTo != null && controlView.legTo.Length > 0)
                    totalLength += controlView.legLength[0];
            }
        }

        // Finalize the course view
        private void Finish()
        {
            GetScales();
            UpdateLegToIndices();
            ComputeStatistics();
        }

        // Add the appropriate specials for the given course to the course view.
        // If descriptionSpecialOnly is true, then only description sheet specials are added.
        private void AddSpecials(Id<Course> courseId, bool descriptionSpecialsOnly)
        {
            foreach (Id<Special> specialId in eventDB.AllSpecialIds) {
                if (!descriptionSpecialsOnly || eventDB.GetSpecial(specialId).kind == SpecialKind.Descriptions) {
                    if (QueryEvent.CourseContainsSpecial(eventDB, courseId, specialId))
                        specialIds.Add(specialId);
                }
            }
        }


        //  -----------  Static methods to create a new CourseView.  -----------------

        // Create a normal course view -- the standard view in order.
        public static CourseView CreateCourseView(EventDB eventDB, Id<Course> courseId, bool descriptionSpecialsOnly)
        {
            Course course = eventDB.GetCourse(courseId);
            CourseView courseView;
            if (course.kind == CourseKind.Score) 
                courseView = CreateScoreCourseView(eventDB, courseId);
            else if (course.kind == CourseKind.Normal)
                courseView = CreateStandardCourseView(eventDB, courseId);
            else {
                Debug.Fail("Bad course kind"); return null;
            }

            courseView.AddSpecials(courseId, descriptionSpecialsOnly);

            return courseView;
        }

        // Create the All Controls view -- show all controls, sorted.
        public static CourseView CreateAllControlsView(EventDB eventDB)
        {
            return CreateFilteredAllControlsView(eventDB, null, ControlPointKind.None, true, true);
        }

        // Create an filtered All Controls view -- show controls from the control collection, but only includes some.
        // excludedCourses contains an array of course ids to excluded from the contgrols.
        // kindFilter, if non-null, limits the controls to this kind of controls.
        public static CourseView CreateFilteredAllControlsView(EventDB eventDB, Id<Course>[] excludedCourses, ControlPointKind kindFilter, bool addSpecials, bool addDescription)
        {
            CourseView courseView = new CourseView(eventDB, Id<Course>.None);

            courseView.courseName = MiscText.AllControls;

            // Add every control to the course view, subject to the filters.
            foreach (Id<ControlPoint> controlId in eventDB.AllControlPointIds) {
                ControlPoint control = eventDB.GetControl(controlId);

                // Check if the control is filtered out.

                if (excludedCourses != null) {
                    // Filter excluded courses.
                    foreach (Id<Course> excludedCourseId in excludedCourses) {
                        if (QueryEvent.CourseUsesControl(eventDB, excludedCourseId, controlId))
                            goto SKIP;
                    }
                }

                if (kindFilter != ControlPointKind.None) {
                    // Filter on control type.
                    if (control.kind != kindFilter)
                        goto SKIP;
                }

                // We are going to include this control in the collection.

                ControlView controlView = new ControlView();

                controlView.courseControlId = Id<CourseControl>.None;
                controlView.controlId = controlId;

                // All controls doesn't have ordinals or variations.
                controlView.ordinal = -1;
                controlView.variation = (char)0;

                courseView.controlViews.Add(controlView);
 
       SKIP:        ;
            }

            // Sort the control views: first by kind, then by code.
            courseView.controlViews.Sort((view1, view2) => QueryEvent.CompareControlIds(eventDB, view1.controlId, view2.controlId));

            courseView.Finish();

            if (addSpecials) {
                // Add every special, regardless of courses it is on, except for descriptions. Descriptions are added to all
                // controls only if they appear in all courses (otherwise we'd see a ton of descriptions on all controls), and if "addDescription" is true
                foreach (Id<Special> specialId in eventDB.AllSpecialIds) {
                    Special special = eventDB.GetSpecial(specialId);
                    if (special.kind == SpecialKind.Descriptions) {
                        if (addDescription && special.allCourses)
                            courseView.specialIds.Add(specialId);
                    }
                    else
                        courseView.specialIds.Add(specialId);
                }
            }

            return courseView;
        }

        // Create the course view for printing and OCAD export. If CourseId is 0, then the all controls view for printing.
        public static CourseView CreatePrintingCourseView(EventDB eventDB, Id<Course> courseId)
        {
            if (courseId.IsNone)
                return CourseView.CreateFilteredAllControlsView(eventDB, null, ControlPointKind.None, true, false);
            else
                return CourseView.CreateCourseView(eventDB, courseId, false);
        }

        // Create the course view for positioning the print area.
        public static CourseView CreatePositioningCourseView(EventDB eventDB, Id<Course> courseId)
        {
            if (courseId.IsNone)
                return CourseView.CreateFilteredAllControlsView(eventDB, null, ControlPointKind.None, false, false);
            else
                return CourseView.CreateCourseView(eventDB, courseId, true);
        }

        // Create the standard view onto a regular course, without variations.
        private static CourseView CreateStandardCourseView(EventDB eventDB, Id<Course> courseId)
        {
            Course course = eventDB.GetCourse(courseId);
            CourseView courseView = new CourseView(eventDB, courseId);
            Id<CourseControl> courseControlId;
            int ordinal;

            courseView.courseName = course.name;

            courseControlId = course.firstCourseControl;
            ordinal = 1;

            while (courseControlId.IsNotNone) {
                ControlView controlView = new ControlView();
                CourseControl courseControl = eventDB.GetCourseControl(courseControlId);
                ControlPoint control = eventDB.GetControl(courseControl.control);

                controlView.courseControlId = courseControlId;
                controlView.controlId = courseControl.control;

                // Set the ordinal number.
                if (control.kind == ControlPointKind.Normal)
                    controlView.ordinal = ordinal++;
                else if (control.kind == ControlPointKind.Start)
                    controlView.ordinal = 0;
                else
                    controlView.ordinal = -1;

                // This kind of view doesn't support variations.
                controlView.variation = (char)0;

                // Set the legTo array with the next courseControlID. This is later updated
                // to the indices.
                if (courseControl.nextCourseControl.IsNotNone) {
                    controlView.legTo = new int[1] { courseControl.nextCourseControl.id };   // legTo initially holds course control ids, later changed.
                }

                // Move to the next control.
                courseView.controlViews.Add(controlView);
                courseControlId = courseControl.nextCourseControl;
            }

            courseView.Finish();
            return courseView;
        }

        // Create the normal view onto a score course
        private static CourseView CreateScoreCourseView(EventDB eventDB, Id<Course> courseId)
        {
            Course course = eventDB.GetCourse(courseId);
            CourseView courseView = new CourseView(eventDB, courseId);
            Id<CourseControl> courseControlId;

            courseView.courseName = course.name;

            courseControlId = course.firstCourseControl;

            while (courseControlId.IsNotNone) {
                ControlView controlView = new ControlView();
                CourseControl courseControl = eventDB.GetCourseControl(courseControlId);

                controlView.courseControlId = courseControlId;
                controlView.controlId = courseControl.control;

                // No ordinals in a score course.
                controlView.ordinal = -1;
                controlView.variation = (char)0;

                // Move to the next control.
                courseView.controlViews.Add(controlView);
                courseControlId = courseControl.nextCourseControl;
            }

            // Sort the control views: first by kind, then by score, then by code.
            courseView.controlViews.Sort(delegate(ControlView view1, ControlView view2) {
                ControlPoint control1 = eventDB.GetControl(view1.controlId);
                ControlPoint control2 = eventDB.GetControl(view2.controlId);
                CourseControl courseControl1 = eventDB.GetCourseControl(view1.courseControlId);
                CourseControl courseControl2 = eventDB.GetCourseControl(view2.courseControlId);

                if (control1.kind < control2.kind)
                    return -1;
                else if (control1.kind > control2.kind)
                    return 1;
                
                if (courseControl1.points != courseControl2.points)
                    return courseControl1.points.CompareTo(courseControl2.points);
                else
                    return Util.CompareCodes(control1.code, control2.code);
            });

            courseView.Finish();
            return courseView;
        }
    }

}
