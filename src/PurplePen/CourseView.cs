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
using System.Linq;

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
            public bool hiddenControl;          // If true, hide this control on map, but not legs to it (used for map exchanges)
        };

        // A description that is being viewed.
        public class DescriptionView
        {
            public readonly Id<Special> SpecialId;
            public readonly CourseDesignator CourseDesignator;

            public DescriptionView(Id<Special> specialId, CourseDesignator courseDesignator)
            {
                this.SpecialId = specialId;
                this.CourseDesignator = courseDesignator;
            }
        }
            
        private EventDB eventDB;
        private string courseName;
        private CourseDesignator courseDesignator;

        private readonly List<ControlView> controlViews = new List<ControlView>();
        private readonly List<Id<Special>> specialIds = new List<Id<Special>>();
        private readonly List<DescriptionView> descriptionViews = new List<DescriptionView>();
        // extra course controls not part of the list of control views. (i.e., finish on first part of multi-part).
        private readonly List<Id<CourseControl>> extraCourseControls = new List<Id<CourseControl>>();

        private int scoreColumn;                // column to put score into.

        private int normalControlCount;         // number of normal controls.
        private float totalPoints;              // total points.
        private float measuredLength;           // measured length
        private float totalLength;              // total length (differs from measured length only if course properies overrides)
        private float partLength;
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

        // All the specials on this course, not include description blocks
        public List<Id<Special>> SpecialIds
        {
            get { return specialIds; }
        }

        public List<DescriptionView> DescriptionViews
        {
            get { return descriptionViews; }
        }

        public List<Id<CourseControl>> ExtraCourseControls
        {
            get { return extraCourseControls; }
        }

        public EventDB EventDB {
            get { return eventDB; }
        }

        public CourseDesignator CourseDesignator
        {
            get { return courseDesignator; }
        }

        // Get the ID of the Course in the event DB. Returns None for an All Controls view.
        public Id<Course> BaseCourseId {
            get { return courseDesignator.CourseId; }
        }

        // Get the kind of the course view.
        public CourseViewKind Kind
        {
            get
            {
                if (courseDesignator.IsAllControls)
                    return CourseViewKind.AllControls;
                else {
                    Course course = eventDB.GetCourse(courseDesignator.CourseId);
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

        // Get the label kind to use.
        public ControlLabelKind ControlLabelKind {
            get {
                if (courseDesignator.IsAllControls)
                    return ControlLabelKind.Code;
                else
                    return eventDB.GetCourse(courseDesignator.CourseId).labelKind;
            }
        }

        public string CourseName
        {
            get { return courseName; }
        }

        // Same as CourseName, but add "-1", "-2", etc. for a part of a multi-part course.
        public string CourseNameWithPart
        {
            get
            {
                if (!courseDesignator.IsAllControls && !courseDesignator.AllParts) {
                    return string.Format("{0}-{1}", courseName, courseDesignator.Part + 1);
                }
                else {
                    return courseName;
                }
            }
        }

        // If multi-part course, length of all parts. If the user
        // specified a course length, this is that length.
        public float TotalLength {
            get { 
                return totalLength;
            }
        }

        // If multi-part course, length of all parts, as calculated.
        // Not affected if the user specified a course length.
        public float MeasuredLength
        {
            get
            {
                return measuredLength;
            }
        }

        // If multi-part course, length of this part only
        public float PartLength
        {
            get
            {
                return partLength;
            }
        }

        public float TotalScore
        {
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

        public int TotalNormalControls
        {
            get
            {
                return normalControlCount;
            }
        }

        public int ScoreColumn {
            get { return scoreColumn; }
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

        private CourseView(EventDB eventDB, CourseDesignator courseDesignator)
        {
            this.eventDB = eventDB;
            this.courseDesignator = courseDesignator;
        }

        // Get the map and print scales.
        private void GetScales()
        {
            mapScale = eventDB.GetEvent().mapScale;
            printScale = QueryEvent.GetPrintScale(eventDB, courseDesignator.CourseId);
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
            partLength = 0;
            Course course = courseDesignator.IsAllControls ? null : eventDB.GetCourse(courseDesignator.CourseId);

            if (courseDesignator.IsAllControls)
                totalClimb = -1;
            else
                totalClimb = course.climb;

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
                    partLength += controlView.legLength[0];
            }

            // Get the total length from another course view, if this is just a partial course.
            if (courseDesignator.AllParts || courseDesignator.IsAllControls)
                measuredLength = partLength;
            else {
                CourseView viewEntireCourse = CourseView.CreateCourseView(eventDB, new CourseDesignator(courseDesignator.CourseId), false, false);
                measuredLength = viewEntireCourse.TotalLength;
            }

            if (course != null && course.overrideCourseLength.HasValue)
                totalLength = course.overrideCourseLength.Value;
            else
                totalLength = measuredLength;
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
        private void AddSpecials(CourseDesignator courseDesignator, bool addNonDescriptionSpecials, bool addDescriptionSpecials)
        {
            bool multiPart = courseDesignator.IsNotAllControls && courseDesignator.AllParts && (QueryEvent.CountCourseParts(eventDB, courseDesignator.CourseId) > 1);

            foreach (Id<Special> specialId in eventDB.AllSpecialIds) {
                SpecialKind specialKind = eventDB.GetSpecial(specialId).kind;

                if (ShouldAddSpecial(specialKind, addNonDescriptionSpecials, addDescriptionSpecials)) {
                    if (specialKind == SpecialKind.Descriptions) {
                        // Descriptions are added differently. It's not entirely clear the best way to handle descriptions
                        // for all-parts of a multi-part course. For now, we don't put any descriptions on.
                        if (!multiPart) {
                            if (QueryEvent.CourseContainsSpecial(eventDB, courseDesignator, specialId))
                                descriptionViews.Add(new DescriptionView(specialId, courseDesignator));
                        }
                    }
                    else {
                        if (QueryEvent.CourseContainsSpecial(eventDB, courseDesignator, specialId))
                            specialIds.Add(specialId);
                    }
                }
            }
        }

        // Should we add the given special?
        private bool ShouldAddSpecial(SpecialKind kind, bool addNonDescriptionSpecials, bool addDescriptionSpecials)
        {
            if (kind == SpecialKind.Descriptions)
                return addDescriptionSpecials;
            else
                return addNonDescriptionSpecials;
        }


        //  -----------  Static methods to create a new CourseView.  -----------------

        // Create a normal course view -- the standard view in order, from start control to finish control. courseId may NOT be None.
        private static CourseView CreateCourseView(EventDB eventDB, CourseDesignator courseDesignator, bool addNonDescriptionSpecials, bool addDescriptionSpecials)
        {
            Debug.Assert(! courseDesignator.IsAllControls);

            Course course = eventDB.GetCourse(courseDesignator.CourseId);
            CourseView courseView;
            if (course.kind == CourseKind.Score) 
                courseView = CreateScoreCourseView(eventDB, courseDesignator);
            else if (course.kind == CourseKind.Normal) {
                courseView = CreateStandardCourseView(eventDB, courseDesignator);
            }
            else {
                Debug.Fail("Bad course kind"); return null;
            }

            courseView.AddSpecials(courseDesignator, addNonDescriptionSpecials, addDescriptionSpecials);

            return courseView;
        }

        // Create the All Controls view -- show all controls, sorted.
        private static CourseView CreateAllControlsView(EventDB eventDB)
        {
            return CreateFilteredAllControlsView(eventDB, null, ControlPointKind.None, true, true);
        }

        // Create an filtered All Controls view -- show controls from the control collection, but only includes some.
        // excludedCourses contains an array of course ids to excluded from the contgrols.
        // kindFilter, if non-null, limits the controls to this kind of controls.
        public static CourseView CreateFilteredAllControlsView(EventDB eventDB, CourseDesignator[] excludedCourses, ControlPointKind kindFilter, bool addSpecials, bool addDescription)
        {
            CourseView courseView = new CourseView(eventDB, CourseDesignator.AllControls);

            courseView.courseName = MiscText.AllControls;
            courseView.scoreColumn = -1;

            // Add every control to the course view, subject to the filters.
            foreach (Id<ControlPoint> controlId in eventDB.AllControlPointIds) {
                ControlPoint control = eventDB.GetControl(controlId);

                // Check if the control is filtered out.

                if (excludedCourses != null) {
                    // Filter excluded courses.
                    foreach (CourseDesignator excludedCourseDesignator in excludedCourses) {
                        if (QueryEvent.CourseUsesControl(eventDB, excludedCourseDesignator, controlId))
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
                // controls only if they appear in all courses (or specifically for the all controls view), and if "addDescription" is true
                foreach (Id<Special> specialId in eventDB.AllSpecialIds) {
                    Special special = eventDB.GetSpecial(specialId);
                    if (special.kind == SpecialKind.Descriptions) {
                        if (addDescription && QueryEvent.CourseContainsSpecial(eventDB, CourseDesignator.AllControls, specialId))
                            courseView.descriptionViews.Add(new DescriptionView(specialId, CourseDesignator.AllControls));
                    }
                    else
                        courseView.specialIds.Add(specialId);
                }
            }

            return courseView;
        }

        // Create a course view for normal viewing.
        public static CourseView CreateViewingCourseView(EventDB eventDB, CourseDesignator courseDesignator)
        {
            if (courseDesignator.IsAllControls)
                return CourseView.CreateAllControlsView(eventDB);
            else
                return CourseView.CreateCourseView(eventDB, courseDesignator, true, true);
        }

        // Create the course view for printing and OCAD export. If CourseId is 0, then the all controls view for printing.
        public static CourseView CreatePrintingCourseView(EventDB eventDB, CourseDesignator courseDesignator)
        {
            if (courseDesignator.IsAllControls)
                return CourseView.CreateFilteredAllControlsView(eventDB, null, ControlPointKind.None, true, true);
            else
                return CourseView.CreateCourseView(eventDB, courseDesignator, true, true);
        }

        // Create the course view for positioning the print area.
        public static CourseView CreatePositioningCourseView(EventDB eventDB, CourseDesignator courseDesignator)
        {
            if (courseDesignator.IsAllControls)
                return CourseView.CreateFilteredAllControlsView(eventDB, null, ControlPointKind.None, false, true);
            else
                return CourseView.CreateCourseView(eventDB, courseDesignator, false, true);
        }

        // Create the course view for positioning the print area for just controls.
        public static CourseView CreateControlsOnlyPositioningCourseView(EventDB eventDB, CourseDesignator courseDesignator)
        {
            if (courseDesignator.IsAllControls)
                return CourseView.CreateFilteredAllControlsView(eventDB, null, ControlPointKind.None, false, false);
            else
                return CourseView.CreateCourseView(eventDB, courseDesignator, false, false);
        }

        // Create the standard view onto a regular course, without variations.
        private static CourseView CreateStandardCourseView(EventDB eventDB, CourseDesignator courseDesignator)
        {
            Course course = eventDB.GetCourse(courseDesignator.CourseId);

            if (QueryEvent.HasVariations(eventDB, courseDesignator.CourseId) && courseDesignator.VariationPath == null)
                throw new ApplicationException("Cannot create course view without specifying which variation");

            // Get sub-part of the course. firstCourseControls is the first control to process, lastCourseControl is the last one to 
            // process, or None if process to the end of the course.
            Id<CourseControl> firstCourseControl, lastCourseControl;
            if (courseDesignator.AllParts) {
                firstCourseControl = course.firstCourseControl;
                lastCourseControl = Id<CourseControl>.None;
            }
            else {
                QueryEvent.GetCoursePartBounds(eventDB, courseDesignator, out firstCourseControl, out lastCourseControl);
            }
            
            CourseView courseView = new CourseView(eventDB, courseDesignator);
            int ordinal;

            courseView.courseName = course.name;
            courseView.scoreColumn = -1;

            ordinal = 1;
            ordinal = course.firstControlOrdinal;

            // To get the ordinals correct, we get the course control ids for all parts.
            List<Id<CourseControl>> courseControls = QueryEvent.EnumCourseControlIds(eventDB, courseDesignator.WithAllParts()).ToList();
            int index = 0;

            // Increase the ordinal value for each normal control before the first one we're considering.
            while (index < courseControls.Count && courseControls[index] != firstCourseControl) { 
                CourseControl courseControl = eventDB.GetCourseControl(courseControls[index]);
                ControlPoint control = eventDB.GetControl(courseControl.control);
                if (control.kind == ControlPointKind.Normal)
                    ++ordinal;
                ++index;
            }

            for (; index < courseControls.Count; ++index) {
                Id<CourseControl> courseControlId = courseControls[index];

                ControlView controlView = new ControlView();
                CourseControl courseControl = eventDB.GetCourseControl(courseControlId);
                ControlPoint control = eventDB.GetControl(courseControl.control);

                controlView.courseControlId = courseControlId;
                controlView.controlId = courseControl.control;

                // Set the ordinal number.
                if (control.kind == ControlPointKind.Normal)
                    controlView.ordinal = ordinal++;
                else if (control.kind == ControlPointKind.Start || control.kind == ControlPointKind.MapExchange)
                    controlView.ordinal = 0;
                else
                    controlView.ordinal = -1;

                // This kind of view doesn't support variations.
                controlView.variation = (char)0;

                // Don't show the map exchange for the next part at the end of this part.
                if (courseControlId == lastCourseControl && !courseDesignator.AllParts && control.kind == ControlPointKind.MapExchange) {
                    controlView.hiddenControl = true;
                }

                // Set the legTo array with the next courseControlID. This is later updated
                // to the indices.
                if (index < courseControls.Count - 1 && courseControlId != lastCourseControl) {
                    Id<CourseControl> nextCourseControl = courseControls[index + 1];
                    controlView.legTo = new int[1] { nextCourseControl.id };   // legTo initially holds course control ids, later changed.
                }
                // Add the controlview.
                courseView.controlViews.Add(controlView);

                if (courseControlId == lastCourseControl)
                    break;
            }

            // If this is a part that should also have the finish on it, and it isn't the last part, then 
            // add the finish.
            if (courseDesignator.IsNotAllControls && !courseDesignator.AllParts && 
                courseDesignator.Part != QueryEvent.CountCourseParts(eventDB, courseDesignator.CourseId) - 1 &&
                QueryEvent.GetPartOptions(eventDB, courseDesignator).ShowFinish) 
            {
                if (QueryEvent.HasFinishControl(eventDB, courseDesignator.CourseId))
                    courseView.extraCourseControls.Add(QueryEvent.LastCourseControl(eventDB, courseDesignator.CourseId, false));
            }

            courseView.Finish();
            return courseView;
        }

        // Create the normal view onto a score course
        private static CourseView CreateScoreCourseView(EventDB eventDB, CourseDesignator courseDesignator)
        {
            Course course = eventDB.GetCourse(courseDesignator.CourseId);
            CourseView courseView = new CourseView(eventDB, courseDesignator);
            Id<CourseControl> courseControlId;

            courseView.courseName = course.name;
            courseView.scoreColumn = course.scoreColumn;

            courseControlId = course.firstCourseControl;

            while (courseControlId.IsNotNone) {
                ControlView controlView = new ControlView();
                CourseControl courseControl = eventDB.GetCourseControl(courseControlId);

                controlView.courseControlId = courseControlId;
                controlView.controlId = courseControl.control;

                // Ordinals assigned after sorting.
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
                int result = Util.CompareCodes(control1.code, control2.code);
                if (result != 0)
                    return result;

                return view1.controlId.id.CompareTo(view2.controlId.id);                
            });

            // Assign ordinals, if applicable. If scores in column A, then no ordinals will be assigned.
            if (courseView.scoreColumn != 0) {
                int ordinal = course.firstControlOrdinal;
                foreach (ControlView control in courseView.controlViews) {
                    if (eventDB.GetControl(control.controlId).kind == ControlPointKind.Normal)
                        control.ordinal = ordinal++;
                }
            }

            courseView.Finish();
            return courseView;
        }
    }

    // A VariationPath indicates a path through the variations of a course.
    public class VariationPath
    {
        // Every place that 'split' is true, indicates which course control is next.
        private Id<CourseControl>[] choices;

        public VariationPath(IEnumerable<Id<CourseControl>> choices)
        {
            if (choices == null)
                choices = new Id<CourseControl>[0];
            else
                this.choices = choices.ToArray();
        }

        public int Count
        {
            get { return choices.Length; }
        }

        public IEnumerable<Id<CourseControl>> Choices
        {
            get { return choices.ToList();  }
        }

        public Id<CourseControl> this[int i] {
            get
            {
                if (i < 0 || i >= Count)
                    throw new ArgumentOutOfRangeException();
                return choices[i];
            }
        }

        public override bool Equals(object obj)
        {
            VariationPath other = obj as VariationPath;
            if (other == null)
                return false;

            return Util.ArrayEquals(this.choices, other.choices);
        }

        public override int GetHashCode()
        {
            return Util.ArrayHashCode(this.choices);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < choices.Length; ++i) {
                if (i != 0)
                    builder.Append("|");
                
                builder.Append(choices[i].id);
            }

            return builder.ToString();
        }
    }

    // A CourseDesignator indicates a course or part of a course for creating a course view.
    // It describes the current view.
    public class CourseDesignator: ICloneable
    {
        private readonly Id<Course> courseId;   // ID of the course, none for all controls.
        private readonly int part;              // Which part of the course. -1 means all parts or not a multi-part course. 0 is first part, 1 is second part, etc.
        private readonly VariationPath variationPath;  // Which path through variations, or null for all or no variations present.

        public override bool Equals(object obj)
        {
            if (!(obj is CourseDesignator))
                return false;
            CourseDesignator other = (CourseDesignator) obj;

            return (courseId == other.courseId && part == other.part && object.Equals(variationPath, other.variationPath));
        }

        public static bool operator ==(CourseDesignator cd1, CourseDesignator cd2)
        {
            if ((object)cd1 == null)
                return ((object)cd2 == null);
            else
                return cd1.Equals(cd2);
        }

        public static bool operator !=(CourseDesignator cd1, CourseDesignator cd2)
        {
            return !(cd1 == cd2);
        }

        public override int GetHashCode()
        {
            return courseId.GetHashCode() ^ part.GetHashCode() ^ (variationPath == null ? 0x34255 : variationPath.GetHashCode()) ;
        }

        public override string ToString()
        {
            string s;
            if (AllParts)
                s = string.Format("Course {0}", courseId.id);
            else
                s = string.Format("Course {0}, Part {1}", courseId.id, part);

            if (variationPath != null)
                s += "Var: " + variationPath.ToString();

            return s;
        }

        public static CourseDesignator AllControls = new CourseDesignator(Id<Course>.None);

        // Create a course designator for all parts
        public CourseDesignator(Id<Course> course)
        {
            this.courseId = course;
            this.part = -1;
        }

        // Create a course designator for a part
        public CourseDesignator(Id<Course> course, int part)
        {
            Debug.Assert(part >= 0);
            Debug.Assert(course.IsNotNone);
            this.courseId = course;
            this.part = part;
        }

        public CourseDesignator(Id<Course> course, VariationPath variationPath)
            :this(course)
        {
            this.variationPath = variationPath;
        }

        public CourseDesignator(Id<Course> course, VariationPath variationPath, int part)
            :this(course, part)
        {
            this.variationPath = variationPath;
        }

        // Accessors.
        public bool IsAllControls
        {
            get { return courseId.IsNone; }
        }

        public bool IsNotAllControls
        {
            get { return !IsAllControls; }
        }

        public Id<Course> CourseId
        {
            get { return courseId; }
        }

        public bool AllParts
        {
            get { return part == -1; }
        }

        public int Part
        {
            get { return part; }
        }

        public VariationPath VariationPath
        {
            get { return variationPath; }
        }

        public bool IsVariation
        {
            get { return variationPath != null && variationPath.Count > 0; }
        }

        public CourseDesignator WithAllVariations()
        {
            if (AllParts)
                return new CourseDesignator(courseId);
            else
                return new CourseDesignator(courseId, part);
        }

        public CourseDesignator WithAllParts()
        {
            return new CourseDesignator(courseId, variationPath);
        }

        public CourseDesignator Clone()
        {
            return (CourseDesignator) base.MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}
