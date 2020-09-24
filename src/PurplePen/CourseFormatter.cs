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
using System.Drawing;
using System.IO;

using PurplePen.MapModel;

namespace PurplePen
{
    using PurplePen.Graphics2D;

    // Macros used in text specials
    static class TextMacros
    {
        public const string EventTitle = "$(EventTitle)";
        public const string CourseName = "$(CourseName)";
        public const string CourseLength = "$(CourseLength)";
        public const string CourseClimb = "$(CourseClimb)";
        public const string ClassList = "$(ClassList)";
        public const string PrintScale = "$(PrintScale)";
        public const string CoursePart = "$(CoursePart)";
        public const string Variation = "$(Variation)";
        public const string RelayTeam = "$(RelayTeam)";
        public const string RelayLeg = "$(RelayLeg)";
        public const string FileName = "$(FileName)";
        public const string MapFileName = "$(MapFileName)";
    }

    class CourseFormatterOptions
    {
        public bool showControlNumbers = true;
    }

    // The course formatter transforms a CourseView into a abstract description of a course, which
    // is a CourseLayout. It does not include the description block itself.
    static class CourseFormatter
    {
        // Format the given CourseView into a bunch of course objects, and add it to the given course Layout
        public static void FormatCourseToLayout(SymbolDB symbolDB, CourseView courseView, CourseAppearance appearance, CourseLayout courseLayout, CourseLayer layer, CourseFormatterOptions options = null)
        {
            EventDB eventDB = courseView.EventDB;
            CourseView.CourseViewKind kind = courseView.Kind;
            ControlLabelKind labelKind = courseView.ControlLabelKind;
            float courseObjRatio = courseView.CourseObjRatio(appearance);
            List<CourseView.ControlView> controlViews = courseView.ControlViews;
            CourseObj courseObj;

            if (options == null)
                options = new CourseFormatterOptions();


            // Go through all the specials in the view and process them to create course objects
            foreach(Id<Special> specialId in courseView.SpecialIds) {
                courseObj = CreateSpecial(eventDB, courseView, courseObjRatio, appearance, specialId, layer);
                if (courseObj != null)
                    courseLayout.AddCourseObject(courseObj);
            }

            // Go through all the descriptions in the view and process them to create course objects
            foreach (CourseView.DescriptionView descriptionView in courseView.DescriptionViews) {
                // The layer depends on "descriptions in purple" setting in the course appearance.
                courseObj = CreateDescriptionSpecial(eventDB, symbolDB, descriptionView, appearance.descriptionsPurple ? layer : CourseLayer.Descriptions);
                if (courseObj != null)
                    courseLayout.AddCourseObject(courseObj);
            }

            // Go through all the controls in the view and process them to create controls and legs.
            for (int controlIndex = 0; controlIndex < controlViews.Count; ++controlIndex) {
                CourseView.ControlView controlView = controlViews[controlIndex];

                if (!controlView.hiddenControl) {

                    // Get the angles of the legs into and out of this control, in radians.
                    double angleOut = ComputeAngleOut(eventDB, courseView, controlIndex);

                    // Get the normal course object associated with this control.
                    courseObj = CreateCourseObject(eventDB, courseView.Kind, courseObjRatio, appearance, courseView.CircleGapScale(appearance), controlView, angleOut);
                    if (courseObj != null) {
                        courseObj.layer = layer;
                        courseLayout.AddCourseObject(courseObj);
                    }

                    // If this course-control indicates custom placement, place the number/code now (so it influences auto-placed numbers).
                    if (options.showControlNumbers && CustomPlaceNumber(eventDB, controlView)) {
                        if (kind == CourseView.CourseViewKind.AllControls)
                            courseObj = CreateCode(eventDB, courseObjRatio, appearance, controlView, courseLayout);
                        else if (kind == CourseView.CourseViewKind.AllVariations)
                            courseObj = CreateControlNumber(eventDB, courseObjRatio, appearance, ControlLabelKind.Code, controlView, courseView, courseLayout);
                        else
                            courseObj = CreateControlNumber(eventDB, courseObjRatio, appearance, labelKind, controlView, courseView, courseLayout);

                        if (courseObj != null) {
                            courseObj.layer = layer;
                            courseLayout.AddCourseObject(courseObj);
                        }
                    }
                }

                if (kind == CourseView.CourseViewKind.Normal || kind == CourseView.CourseViewKind.AllVariations || 
                    (kind == CourseView.CourseViewKind.Score && eventDB.GetControl(controlView.controlId).kind == ControlPointKind.MapIssue))
                {
                    // Get the object(s) associated with the leg(s) to the next control.
                    if (controlView.legTo != null) {
                        for (int leg = 0; leg < controlView.legTo.Length; ++leg) {
                            List<CourseObj> courseObjs = CreateLeg(eventDB, courseView, courseObjRatio, appearance, controlView.courseControlIds[leg], controlView, controlViews[controlView.legTo[leg]], controlView.legId[leg]);
                            if (courseObjs != null) {
                                foreach (CourseObj o in courseObjs) {
                                    o.layer = layer;
                                    courseLayout.AddCourseObject(o);
                                }
                            }
                        }
                    }
                }
            }

            // Add any additional controls
            foreach (Id<CourseControl> extraCourseControl in courseView.ExtraCourseControls) {
                courseLayout.AddCourseObject(CreateCourseObject(eventDB, courseView.Kind, courseObjRatio, appearance, courseView.CircleGapScale(appearance),
                                                                eventDB.GetCourseControl(extraCourseControl).control, extraCourseControl, double.NaN));
            }

            // No go through each control again and add an automatically placed number/code to each. We do this last so that the placement
            // of all fixed-position objects influences the auto-positioned numbers so that they don't interfere.
            if (options.showControlNumbers) {
                for (int controlIndex = 0; controlIndex < controlViews.Count; ++controlIndex) {
                    CourseView.ControlView controlView = controlViews[controlIndex];

                    // Only place numbers WITHOUT custom number placement. Those with custom placement were done previously above.
                    if (!controlView.hiddenControl && !CustomPlaceNumber(eventDB, controlView)) {
                        if (kind == CourseView.CourseViewKind.AllControls)
                            courseObj = CreateCode(eventDB, courseObjRatio, appearance, controlView, courseLayout);
                        else if (kind == CourseView.CourseViewKind.AllVariations)
                            courseObj = CreateControlNumber(eventDB, courseObjRatio, appearance, ControlLabelKind.Code, controlView, courseView, courseLayout);
                        else
                            courseObj = CreateControlNumber(eventDB, courseObjRatio, appearance, labelKind, controlView, courseView, courseLayout);

                        if (courseObj != null) {
                            courseObj.layer = layer;
                            courseLayout.AddCourseObject(courseObj);
                        }
                    }
                }
            }

            // Automatically add cuts to close control circles and crossing legs in the layout.
            if (courseView.Kind != CourseView.CourseViewKind.AllControls) {
                AutoCutCircles(courseLayout, layer);
                AutoCutLegs(eventDB, appearance, courseView.CourseDesignator, courseLayout, layer);
            }
        }

        // Does this control view have a custom number placement?
        private static bool CustomPlaceNumber(EventDB eventDB, CourseView.ControlView controlView)
        {
            return ((controlView.courseControlIds[0].IsNotNone && eventDB.GetCourseControl(controlView.courseControlIds[0]).customNumberPlacement) ||
                        (controlView.courseControlIds[0].IsNone && eventDB.GetControl(controlView.controlId).customCodeLocation));
        }

        // Find all the control view in this courseview that uses the given control id. Used for showing repeated controls in a butterfly course
        // with a nicer view. 
        private static List<CourseView.ControlView> FindControlViewsWithControlId(CourseView courseView, Id<ControlPoint> controlId)
        {
            List<CourseView.ControlView> list = new List<CourseView.ControlView>();

            for (int controlIndex = 0; controlIndex < courseView.ControlViews.Count; ++controlIndex) {
                CourseView.ControlView controlView = courseView.ControlViews[controlIndex];
                if (controlView.controlId == controlId)
                    list.Add(controlView);
            }

            return list;
        }

        // Get the text for a control lable
        private static string GetControlLabelText(EventDB eventDB, ControlLabelKind labelKind, CourseView.ControlView controlView, CourseView courseView) {
            string text = "";

            List<CourseView.ControlView> repeatedControlViews = FindControlViewsWithControlId(courseView, controlView.controlId);

            if (repeatedControlViews.Count > 1 && repeatedControlViews[0] != controlView) {
                // This control is repeated (e.g., like a butterfly course) and is not the first use of this control. Don't put any text.
                return "";
            }

            if (labelKind == ControlLabelKind.Sequence || labelKind == ControlLabelKind.SequenceAndCode || labelKind == ControlLabelKind.SequenceAndScore) {
                text += controlView.ordinal.ToString();

                // Add in numbers for repeated controls.
                for (int i = 1; i < repeatedControlViews.Count; ++i) {
                    text += "/";
                    text += repeatedControlViews[i].ordinal.ToString();
                }
            }
            if (labelKind == ControlLabelKind.SequenceAndCode)
                text += "-";
            if (labelKind == ControlLabelKind.SequenceAndCode || labelKind == ControlLabelKind.Code || labelKind == ControlLabelKind.CodeAndScore) {
                ControlPoint control = eventDB.GetControl(controlView.controlId);
                text += control.code;
            }

            if (labelKind == ControlLabelKind.CodeAndScore || labelKind == ControlLabelKind.SequenceAndScore) {
                int points = eventDB.GetCourseControl(controlView.courseControlIds[0]).points;
                if (points > 0) {
                    text += "(" + points.ToString() + ")";
                }
            }

            return text;
        }

        // Create the control number text object, avoiding existing objects on the map. This can be in the form of a sequence number, code, or both.
        private static CourseObj CreateControlNumber(EventDB eventDB, float courseObjRatio, CourseAppearance appearance, ControlLabelKind labelKind, CourseView.ControlView controlView, CourseView courseView, IEnumerable<CourseObj> existingObjects)
        {
            ControlPoint control = eventDB.GetControl(controlView.controlId);
            CourseControl courseControl = eventDB.GetCourseControl(controlView.courseControlIds[0]);
            PointF controlLocation = control.location;
            PointF textCenterLocation;
            string text;

            if (control.kind == ControlPointKind.Normal) {
                text = GetControlLabelText(eventDB, labelKind, controlView, courseView);

                // Figure out where the control number goes.
                if (courseControl.customNumberPlacement) {
                    textCenterLocation = new PointF(controlLocation.X + courseControl.numberDeltaX, controlLocation.Y + courseControl.numberDeltaY);
                }
                else {
                    FontDesc fontDesc = appearance.numberBold ? NormalCourseAppearance.controlNumberFont : NormalCourseAppearance.controlNumberFontBold;
                    float textDistance = ((appearance.ControlCircleOutsideDiameter / 2F) + (NormalCourseAppearance.controlNumberCircleDistance * appearance.controlCircleSize)) * courseObjRatio;
                    textCenterLocation = GetTextLocation(controlLocation, textDistance, text, fontDesc, courseObjRatio * appearance.numberHeight, existingObjects);
                }

                return new ControlNumberCourseObj(controlView.controlId, controlView.courseControlIds[0], courseObjRatio, appearance, text, textCenterLocation);
            }
            else {
                // Only normal controls have numbers.
                return null;
            }
        }

        // Create the control code text object, avoiding existing objects on the map.
        private static CourseObj CreateCode(EventDB eventDB, float courseObjRatio, CourseAppearance appearance, CourseView.ControlView controlView, IEnumerable<CourseObj> existingObjects)
        {
            ControlPoint control = eventDB.GetControl(controlView.controlId);
            CourseControl courseControl = controlView.courseControlIds[0].IsNotNone ? eventDB.GetCourseControl(controlView.courseControlIds[0]) : null;
            PointF controlLocation = control.location;
            string text;
            PointF textCenterLocation;
            float distanceFromCenter = ((appearance.ControlCircleOutsideDiameter / 2F) + (NormalCourseAppearance.codeCircleDistance * appearance.controlCircleSize)) * courseObjRatio;


            if (control.kind == ControlPointKind.Normal) {
                text = control.code;

                if (courseControl != null && courseControl.customNumberPlacement) {
                    textCenterLocation = new PointF(controlLocation.X + courseControl.numberDeltaX, controlLocation.Y + courseControl.numberDeltaY);
                }
                else if (courseControl == null && control.customCodeLocation) {
                    textCenterLocation = GetRectangleCenter(controlLocation, distanceFromCenter, (float) (control.codeLocationAngle * Math.PI / 180F), GetTextSize(text, NormalCourseAppearance.controlCodeFont, courseObjRatio));
                }
                else {
                    textCenterLocation = GetTextLocation(controlLocation, distanceFromCenter,
                                                                                 text, NormalCourseAppearance.controlCodeFont, courseObjRatio * appearance.numberHeight, existingObjects);
                }

                return new CodeCourseObj(controlView.controlId, controlView.courseControlIds[0], courseObjRatio, appearance, text, textCenterLocation);
            }
            else {
                // Only normal controls get codes.
                return null;
            }
        }

        // Find a location for the control that is furthest possible from surrounding course objects.
#if TEST
        internal
#endif
        static PointF GetTextLocation(PointF controlLocation, float distanceFromCenter, string text, FontDesc font, float fontScaling, IEnumerable<CourseObj> list)
        {
            const double deltaAngle = Math.PI / 16;             // angle to increase by each time when testing an angle.

            // Get a list of all nearby objects that we want to stay away from.
            List<CourseObj> nearbyObjects = GetNearbyObjects(list, controlLocation, distanceFromCenter * 4);

            // Get the size of the text.
            SizeF textSize = GetTextSize(text, font, fontScaling);

            // Try 32 different locations for the number, finding which angle has the largest distance from nearby objects.
            // Start at the default angle, so if all angles are equally good that is the one we pick.
            PointF bestPoint = new PointF();
            double bestDistance = -1;

            for (double angle = NormalCourseAppearance.defaultControlNumberAngle; 
                   angle < NormalCourseAppearance.defaultControlNumberAngle + 2 * Math.PI; 
                   angle += deltaAngle) 
            {
                PointF pt = GetRectangleCenter(controlLocation, distanceFromCenter, angle, textSize);
                double distanceFromNearby = GetMinDistanceFromNearby(pt, nearbyObjects);

                if (distanceFromNearby > bestDistance) {
                    bestPoint = pt;
                    bestDistance = distanceFromNearby;
                }
            }

            return bestPoint; 
        }

        // Get all object that are within a distance of a given point, but not actually a control circle AT that point.
        private static List<CourseObj> GetNearbyObjects(IEnumerable<CourseObj> list, PointF pt, float distance)
        {
            List<CourseObj> result = new List<CourseObj>();
            foreach (CourseObj obj in list) {
                if (obj is ControlCourseObj && ((ControlCourseObj) obj).location == pt)
                    continue;     // ignore the control circle.

                if (obj.DistanceFromPoint(pt) <= distance)
                    result.Add(obj);
            }

            return result;
        }

        // Given a point and list of course objects, determine the minimum distance from that point
        // to an object on that list. Returns 0 if the list is empty.
        private static double GetMinDistanceFromNearby(PointF pt, List<CourseObj> list)
        {
            double minDistance = 1000000;

            if (list.Count == 0)
                return 0;

            foreach (CourseObj courseObj in list) {
                double distance = courseObj.DistanceFromPoint(pt);
                if (distance < minDistance)
                    minDistance = distance;
            }

            return minDistance;
        }

        // Find the size of the given text in the given font/size. Only the size of digits/capital letters is returned.
#if TEST
        internal
#endif
        static SizeF GetTextSize(string text, FontDesc font, float fontScaling)
        {
            Graphics g = Util.GetHiresGraphics();
            using (Font f = font.GetScaledFont(fontScaling)) {
                SizeF size = g.MeasureString(text, f, new PointF(0, 0), StringFormat.GenericTypographic);

                // We really want the size of just the digits/capital letters. So, reduce by the descender size from 
                // bottom and top (no way to get offset from top of box to top of cap letters).
                FontFamily family = f.FontFamily;
                float descender = family.GetCellDescent(f.Style) * font.EmHeight * fontScaling / family.GetEmHeight(f.Style);
                size.Height = size.Height - 2 * descender;

                return size;
            }
        }

        // Given a center point, distance from that point, angle, and rectangle size, find the point that is the center of a 
        // rectangle of the given size, at the given angle from the point, and the edge/corner of the rectangle
        // is the given distance from the point.
#if TEST
        internal
#endif
        static PointF GetRectangleCenter(PointF centerPt, float distanceFromCenter, double angle, SizeF rectSize)
        {
            double w2 = rectSize.Width / 2;              // half the width
            double h2 = rectSize.Height / 2;              // half the height
            double r = distanceFromCenter;              // shorter variable name
            double tangent = Math.Tan(angle);         // tangent of the angle.
            PointF rectCenter;

            if (w2 == 0 && h2 == 0) {
                // Degenerate case: rectangle is empty.
                rectCenter = new PointF((float) (Math.Cos(angle) * r), (float) (Math.Sin(angle) * r));
            }
            else if (w2 > 0 && Math.Abs(tangent) >= (h2 + r) / w2) {
                // Case 1: the top of bottom edge of the rectangle touches the circle.
                rectCenter = new PointF((float) ((h2 + r) / tangent), (float) (h2 + r));
                if (Math.Sin(angle) < 0) {
                    // top edge of rectangle touches the bottom of the circle.
                    rectCenter.X = -rectCenter.X;
                    rectCenter.Y = -rectCenter.Y;
                }
            }
            else if (Math.Abs(tangent) <= h2 / (w2 + r)) {
                // Case 2: the left or right edge of the rectangle touches the circle.
                rectCenter = new PointF((float) (r + w2), (float) ((r + w2) * tangent));
                if (Math.Cos(angle) < 0) {
                    // right edge of rectangle touches left of circle
                    rectCenter.X = -rectCenter.X;
                    rectCenter.Y = -rectCenter.Y;
                }
            }
            else {
                // Case 3: a corner of the rectangle touches the circle
                double normalAngle = Math.Atan2(Math.Abs(Math.Sin(angle)), Math.Abs(Math.Cos(angle)));  // normalize to first quadrant.
                double angleRect = Math.Atan2(h2, w2);
                double radiusRect = Math.Sqrt(h2 * h2 + w2 * w2);
                double alpha = normalAngle - angleRect;
                double beta = Math.Asin(Math.Sin(alpha) / r * radiusRect);
                double angleToTouch = normalAngle + beta;

                rectCenter = new PointF((float) (Math.Cos(angleToTouch) * r + w2), (float) (Math.Sin(angleToTouch) * r + h2));

                if (Math.Cos(angle) < 0)
                    rectCenter.X = - rectCenter.X;
                if (Math.Sin(angle) < 0)
                    rectCenter.Y = - rectCenter.Y;
            }

            return new PointF(rectCenter.X + centerPt.X, rectCenter.Y + centerPt.Y);
        }

        // Expand text
        public static string ExpandText(EventDB eventDB, CourseView courseView, string text)
        {
            if (text.Contains(TextMacros.EventTitle))
                text = text.Replace(TextMacros.EventTitle, QueryEvent.GetEventTitle(eventDB, " "));

            if (text.Contains(TextMacros.CourseName))
                text = text.Replace(TextMacros.CourseName, courseView.CourseName);

            if (text.Contains(TextMacros.CoursePart)) {
                if (courseView.CourseDesignator.IsNotAllControls && !courseView.CourseDesignator.AllParts)
                    text = text.Replace(TextMacros.CoursePart, (courseView.CourseDesignator.Part + 1).ToString());
                else
                    text = text.Replace(TextMacros.CoursePart, "");
            }

            if (text.Contains(TextMacros.Variation)) {
                text = text.Replace(TextMacros.Variation, courseView.VariationName);
            }

            if (text.Contains(TextMacros.RelayTeam)) {
                if (courseView.RelayTeam.HasValue) {
                    text = text.Replace(TextMacros.RelayTeam, courseView.RelayTeam.Value.ToString());
                }
                else {
                    text = text.Replace(TextMacros.RelayTeam, "--");
                }
            }

            if (text.Contains(TextMacros.RelayLeg)) {
                if (courseView.RelayLeg.HasValue) {
                    text = text.Replace(TextMacros.RelayLeg, courseView.RelayLeg.Value.ToString());
                }
                else {
                    text = text.Replace(TextMacros.RelayLeg, "-");
                }
            }

            if (text.Contains(TextMacros.CourseLength))
                text = text.Replace(TextMacros.CourseLength, Util.GetLengthInKm(courseView.MinTotalLength, courseView.MaxTotalLength, 1, false));

            if (text.Contains(TextMacros.CourseClimb)) {
                if (courseView.TotalClimb < 0)
                    text = text.Replace(TextMacros.CourseClimb, "");
                else
                    text = text.Replace(TextMacros.CourseClimb, Convert.ToString(Math.Round(courseView.TotalClimb / 5, MidpointRounding.AwayFromZero) * 5.0));
            }

            if (text.Contains(TextMacros.PrintScale)) {
                text = text.Replace(TextMacros.PrintScale, string.Format("1:{0:N0}", courseView.PrintScale));
            }

            if (text.Contains(TextMacros.ClassList)) {
                string classList = "";
                if (courseView.CourseDesignator.IsNotAllControls) {
                    classList = eventDB.GetCourse(courseView.BaseCourseId).secondaryTitle;
                    if (classList == null)
                        classList = "";
                    else
                        classList = classList.Replace("|", " ");
                }

                text = text.Replace(TextMacros.ClassList, classList);
            }

            if (text.Contains(TextMacros.FileName)) {
                text = text.Replace(TextMacros.FileName, Path.GetFileName(eventDB.PathName));
            }

            if (text.Contains(TextMacros.MapFileName)) {
                Event ev = eventDB.GetEvent();
                string mapFileName = (ev.mapType == MapType.None) ? "" : Path.GetFileName(ev.mapFileName);
                text = text.Replace(TextMacros.MapFileName, mapFileName);
            }

            return text;
        }

        // Create the course objects associated with this special. Assign the given layer to it.
        static CourseObj CreateSpecial(EventDB eventDB, CourseView courseView, float courseObjRatio, CourseAppearance appearance, Id<Special> specialId, CourseLayer normalLayer)
        {
            Special special = eventDB.GetSpecial(specialId);
            CourseObj courseObj = null;

            switch (special.kind) {
            case SpecialKind.FirstAid:
                courseObj = new FirstAidCourseObj(specialId, courseObjRatio, appearance, special.locations[0]); break;
            case SpecialKind.Water:
                courseObj = new WaterCourseObj(specialId, courseObjRatio, appearance, special.locations[0]); break;
            case SpecialKind.OptCrossing:
                courseObj = new CrossingCourseObj(Id<ControlPoint>.None, Id<CourseControl>.None, specialId, courseObjRatio, appearance, special.orientation, special.stretch, special.locations[0]); break;
            case SpecialKind.Forbidden:
                courseObj = new ForbiddenCourseObj(specialId, courseObjRatio, appearance, special.locations[0]); break;
            case SpecialKind.RegMark:
                courseObj = new RegMarkCourseObj(specialId, courseObjRatio, appearance, special.locations[0]); break;
            case SpecialKind.Boundary:
                courseObj = new BoundaryCourseObj(specialId, courseObjRatio, appearance, new SymPath(special.locations)); break;
            case SpecialKind.Rectangle:
                courseObj = new RectSpecialCourseObj(specialId, appearance, false, special.color, special.lineKind, special.lineWidth, special.cornerRadius, special.gapSize, special.dashSize, Geometry.RectFromPoints(special.locations[0], special.locations[1])); break;
            case SpecialKind.Ellipse:
                courseObj = new RectSpecialCourseObj(specialId, appearance, true, special.color, special.lineKind, special.lineWidth, special.cornerRadius, special.gapSize, special.dashSize, Geometry.RectFromPoints(special.locations[0], special.locations[1])); break;
            case SpecialKind.Line:
                courseObj = new LineSpecialCourseObj(specialId, appearance, special.color, special.lineKind, special.lineWidth, special.gapSize, special.dashSize, new SymPath(special.locations)); break;
            case SpecialKind.OOB:
                courseObj = new OOBCourseObj(specialId, courseObjRatio, appearance, special.locations); break;
            case SpecialKind.Dangerous:
                courseObj = new DangerousCourseObj(specialId, courseObjRatio, appearance, special.locations); break;
            case SpecialKind.WhiteOut:
                courseObj = new WhiteOutCourseObj(specialId, courseObjRatio, appearance, special.locations); break;

            case SpecialKind.Image:
                Special imageSpecial = eventDB.GetSpecial(specialId);
                courseObj = new ImageCourseObj(specialId, courseObjRatio, appearance, special.locations, imageSpecial.text, imageSpecial.imageBitmap); 
                break;

            case SpecialKind.Text:
                string text = ExpandText(eventDB, courseView, special.text);
                FontStyle fontStyle = Util.GetFontStyle(special.fontBold, special.fontItalic);
                RectangleF boundingRect = RectangleF.FromLTRB((float)Math.Min(special.locations[0].X, special.locations[1].X), (float)Math.Min(special.locations[0].Y, special.locations[1].Y),
                                                                                              (float)Math.Max(special.locations[0].X, special.locations[1].X), (float)Math.Max(special.locations[0].Y, special.locations[1].Y));
                courseObj = new BasicTextCourseObj(specialId, text, boundingRect, special.fontName, fontStyle, special.color, special.fontHeight);
                break;
            case SpecialKind.Descriptions:
                Debug.Fail("description specials should not be passed to this function");
                return null;
            default:
                Debug.Fail("bad special kind");
                return null;
            }

            courseObj.layer = normalLayer;

            return courseObj;
        }

        // Create the course objects associated with this special. Assign the given layer to it.
        static CourseObj CreateDescriptionSpecial(EventDB eventDB, SymbolDB symbolDB, CourseView.DescriptionView descriptionView, CourseLayer layer)
        {
            Special special = eventDB.GetSpecial(descriptionView.SpecialId);
            Debug.Assert(special.kind == SpecialKind.Descriptions);

            DescriptionKind descKind;
            bool columnHScore;
            DescriptionLine[] description = GetCourseDescription(eventDB, symbolDB, descriptionView.CourseDesignator, out descKind, out columnHScore);
            CourseObj courseObj = new DescriptionCourseObj(descriptionView.SpecialId, special.locations[0], (float)Geometry.Distance(special.locations[0], special.locations[1]), 
                                                          symbolDB, description, descKind, columnHScore, special.numColumns);
            courseObj.layer = layer;
            return courseObj;
        }

        // Return the description and description kind for a given CourseView.
        public static DescriptionLine[] GetCourseDescription(EventDB eventDB, SymbolDB symbolDB, CourseDesignator courseDesignator, out DescriptionKind descKind, out bool columnHScore)
        {
            CourseView courseViewDescription;
            DescriptionLine[] description;
            bool noTextOrSymbols = false;

            // Get the course view for the description we're using.
            courseViewDescription = CourseView.CreateViewingCourseView(eventDB, courseDesignator);

            // Create the description. Note the courseId is None only if we're both in all controls, and there are no courses.
            DescriptionFormatter descFormatter = new DescriptionFormatter(courseViewDescription, symbolDB, DescriptionFormatter.Purpose.ForMap);
            descKind = QueryEvent.GetDefaultDescKind(eventDB, courseDesignator.CourseId);
            columnHScore = (descKind == DescriptionKind.Text && courseViewDescription.ScoreColumn == 7);
            description = descFormatter.CreateDescription(descKind == DescriptionKind.Symbols);
            if (noTextOrSymbols)
                DescriptionFormatter.ClearTextAndSymbols(description);

            return description;
        }

        // Create the object associated with the control/start/finish etc with this control view.
        // AngleOut is the direction IN RADIANs leaving the control.
        static CourseObj CreateCourseObject(EventDB eventDB, CourseView.CourseViewKind courseKind, float courseObjRatio, CourseAppearance appearance, float printScale, CourseView.ControlView controlView, double angleOut)
        {
            return CreateCourseObject(eventDB, courseKind, courseObjRatio, appearance, printScale, controlView.controlId, controlView.courseControlIds[0], angleOut);
        }

        static CourseObj CreateCourseObject(EventDB eventDB, CourseView.CourseViewKind courseKind, float courseObjRatio, CourseAppearance appearance, float scaleForCircleGaps, 
                                            Id<ControlPoint> controlId, Id<CourseControl> courseControlId, double angleOut)
        {
            ControlPoint control = eventDB.GetControl(controlId);
            CircleGap[] gaps = QueryEvent.GetControlGaps(eventDB, controlId, scaleForCircleGaps);
            CourseObj courseObj = null;

            switch (control.kind) {
            case ControlPointKind.MapIssue:
                MapIssueCourseObj.RenderStyle mapIssueRenderStyle;

                if (control.mapIssueKind == MapIssueKind.Beginning && courseKind == CourseView.CourseViewKind.AllControls) {
                    mapIssueRenderStyle = MapIssueCourseObj.RenderStyle.WithTail;
                }
                else if (control.mapIssueKind == MapIssueKind.Beginning) {
                    mapIssueRenderStyle = MapIssueCourseObj.RenderStyle.WithoutTail;
                }
                else {
                    mapIssueRenderStyle = MapIssueCourseObj.RenderStyle.Nothing;
                }

                courseObj = new MapIssueCourseObj(controlId, courseControlId, courseObjRatio, appearance, 
                                                double.IsNaN(angleOut) ? 0 : (float)Geometry.RadiansToDegrees(angleOut), control.location, 
                                                mapIssueRenderStyle);
                break;

            case ControlPointKind.Start:
            case ControlPointKind.MapExchange:
                courseObj = new StartCourseObj(controlId, courseControlId, courseObjRatio, appearance, double.IsNaN(angleOut) ? 0 : (float)Geometry.RadiansToDegrees(angleOut), control.location, CrossHairOptions.HighlightCrossHair);
                break;

            case ControlPointKind.Finish:
                courseObj = new FinishCourseObj(controlId, courseControlId, courseObjRatio, appearance, gaps, control.location, CrossHairOptions.HighlightCrossHair);
                break;

            case ControlPointKind.Normal:
                courseObj = new ControlCourseObj(controlId, courseControlId, courseObjRatio, appearance, gaps, control.location);
                break;

            case ControlPointKind.CrossingPoint:
                courseObj = new CrossingCourseObj(controlId, courseControlId, Id<Special>.None, courseObjRatio, appearance, control.orientation, control.stretch, control.location);
                break;

            default:
                Debug.Fail("bad control kind");
                return null;
            }

            return courseObj;
        }


        // Get all the locations in the course exception controlView.
        private static PointF[] GetOtherLocations(EventDB eventDB, CourseView courseView, CourseView.ControlView controlViewExcept)
        {
            List<PointF> list = new List<PointF>();

            foreach (CourseView.ControlView controlView in courseView.ControlViews) {
                if (controlView != controlViewExcept)
                    list.Add(eventDB.GetControl(controlView.controlId).location);
            }

            return list.ToArray();
        }

        // Create a single object associated with the leg from courseControlId1 to courseControlId2. Does not consider
        // flagging (but does consider bends and gaps.) Used for highlighting on the map. 
        public static CourseObj CreateSimpleLeg(EventDB eventDB, CourseView courseView, float courseObjRatio, CourseAppearance appearance, Id<CourseControl> courseControlId1, Id<CourseControl> courseControlId2)
        {
            Id<ControlPoint> controlId1 = eventDB.GetCourseControl(courseControlId1).control;
            Id<ControlPoint> controlId2 = eventDB.GetCourseControl(courseControlId2).control;
            ControlPoint control1 = eventDB.GetControl(controlId1);
            ControlPoint control2 = eventDB.GetControl(controlId2);
            LegGap[] gaps;

            SymPath legPath = GetLegPath(eventDB, control1.location, control1.kind, controlId1, control2.location, control2.kind, controlId2, ComputeStartAngleOut(eventDB, courseView), courseObjRatio, appearance, out gaps);
            if (legPath == null)
                return null;

            return new LegCourseObj(controlId1, courseControlId1, courseControlId2, courseObjRatio, appearance, legPath, gaps);
        }

        // Create the objects associated with the leg from controlView1 to controlView2. Could be multiple because
        // a leg may be partly flagged, and so forth. Gaps do not create separate course objects.
        private static List<CourseObj> CreateLeg(EventDB eventDB, CourseView courseView, float courseObjRatio, CourseAppearance appearance, Id<CourseControl> courseControlId1, CourseView.ControlView controlView1, CourseView.ControlView controlView2, Id<Leg> legId)
        {
            ControlPoint control1 = eventDB.GetControl(controlView1.controlId);
            ControlPoint control2 = eventDB.GetControl(controlView2.controlId);
            Leg leg = (legId.IsNotNone) ? eventDB.GetLeg(legId) : null;
            List<SymPath> paths = new List<SymPath>();     // paths for each segment of the leg.
            List<LegGap[]> gapsList = new List<LegGap[]>();     // gaps for each segment of the leg.
            List<bool> isFlagged = new List<bool>();             // indicates if each segment is flagged or not.

            LegGap[] gaps;                // What kind of gaps are present? Null array if none 

            // Get the path of the line, and the gaps.
            SymPath legPath = GetLegPath(eventDB, 
                                         control1.location, controlView1.hiddenControl ? ControlPointKind.None : control1.kind, controlView1.controlId, 
                                         control2.location, controlView2.hiddenControl ? ControlPointKind.None : control2.kind, controlView2.controlId,
                                         ComputeStartAngleOut(eventDB, courseView), courseObjRatio, appearance, out gaps);
            if (legPath == null)
                return null;

            // What kind of flagging does this leg have (none/full/begin/end)?
            FlaggingKind flagging = QueryEvent.GetLegFlagging(eventDB, controlView1.controlId, controlView2.controlId, legId);

            // Based on flagging kind, set up the paths/isFlagged lists. Add in gaps as part of it.
            if (flagging == FlaggingKind.Begin || flagging == FlaggingKind.End) {
                // Flagging is partial. We need to split the path into two.
                SymPath beginPath, endPath;
                legPath.Split(leg.flagStartStop, out beginPath, out endPath);

                paths.Add(beginPath);
                gapsList.Add(gaps);
                isFlagged.Add(flagging == FlaggingKind.Begin);

                // Update gaps for the end part.
                if (gaps != null) {
                    gaps = (LegGap[]) gaps.Clone();
                    for (int i = 0; i < gaps.Length; ++i)
                        gaps[i].distanceFromStart -= beginPath.Length;
                }

                paths.Add(endPath);
                gapsList.Add(gaps);
                isFlagged.Add(flagging == FlaggingKind.End);
            }
            else {
                // flagging is not partial. A single path is OK.
                paths.Add(legPath);
                gapsList.Add(gaps);
                isFlagged.Add(flagging == FlaggingKind.All);
            }

            // Create course objects for this leg from the paths/isFlagged lists.
            List<CourseObj> objs = new List<CourseObj>();
            for (int i = 0; i < paths.Count; ++i) {
                if (isFlagged[i]) 
                    objs.Add(new FlaggedLegCourseObj(controlView1.controlId, courseControlId1, controlView2.courseControlIds[0], courseObjRatio, appearance, paths[i], gapsList[i]));
                else
                    objs.Add(new LegCourseObj(controlView1.controlId, courseControlId1, controlView2.courseControlIds[0], courseObjRatio, appearance, paths[i], gapsList[i]));
            }

            // If there is a map issue point along the leg, draw it.
            // We do not associate it with an controlId or courseControlId, because the timed start is associated with those,
            // and weird things happen otherwise.
            if (leg != null && leg.flagging == FlaggingKind.IssuePointMiddle) {
                float angleOfIssuePoint = legPath.TangentAngleAtPoint(leg.flagStartStop) + 90.0F;
                objs.Add(new MapIssueCourseObj(Id<ControlPoint>.None, Id<CourseControl>.None, courseObjRatio, appearance,
                                angleOfIssuePoint, leg.flagStartStop,
                                MapIssueCourseObj.RenderStyle.WithoutTail));
            }

            return objs;
        }

        // Create a path from pt1 to pt2, with a radius aroudn the points correct for the given control kind. If the leg would
        // be of zero length, return null. The controlIds for the start and end points are optional -- if supplied, they are used
        // to deal with bends and gaps. If either is None, then the legs don't use bends or gaps. Returns the gaps to used
        // with the radius subtracted from them.
        public static SymPath GetLegPath(EventDB eventDB, PointF pt1, ControlPointKind kind1, Id<ControlPoint> controlId1, 
                                                          PointF pt2, ControlPointKind kind2, Id<ControlPoint> controlId2, double angleOutStart,
                                                          float courseObjRatio, CourseAppearance appearance, out LegGap[] gaps)
        {
            PointF[] bends = null;
            PointF? dashPoint = null;
            gaps = null;

            // Get bends and gaps if controls were supplied.
            if (controlId1.IsNotNone && controlId2.IsNotNone) {
                Id<Leg> legId = QueryEvent.FindLeg(eventDB, controlId1, controlId2);
                Leg leg = (legId.IsNotNone) ? eventDB.GetLeg(legId) : null;

                // Get the path of the line.
                if (leg != null) {
                    bends = leg.bends;
                    if (leg.flagging == FlaggingKind.IssuePointMiddle) {
                        // The issue point should be a dash point to get the dashes to look right.
                        dashPoint = leg.flagStartStop;
                    }
                    gaps = QueryEvent.GetLegGaps(eventDB, controlId1, controlId2);
                }
            }

            double legRadius1 = GetLegRadius(kind1, courseObjRatio, appearance);
            double legRadius2 = GetLegRadius(kind2, courseObjRatio, appearance);

            if (kind1 == ControlPointKind.CrossingPoint && controlId1.IsNotNone) {
                pt1 = AdjustedCrossingPointLocation(eventDB, controlId1, pt2, courseObjRatio);
            }
            if (kind2 == ControlPointKind.CrossingPoint && controlId2.IsNotNone) {
                pt2 = AdjustedCrossingPointLocation(eventDB, controlId2, pt1, courseObjRatio);
            }

            if (kind2 == ControlPointKind.Start && !double.IsNaN(angleOutStart)) {
                double angleInStart;
                if (bends == null || bends.Length == 0) {
                    angleInStart = Math.Atan2(pt2.Y - pt1.Y, pt2.X - pt1.X);
                }
                else {
                    angleInStart = Math.Atan2(pt2.Y - bends[bends.Length - 1].Y, pt2.X - bends[bends.Length - 1].X);
                }
                legRadius2 *= StartTriangleRadiusAdjustment(angleInStart, angleOutStart);
            }

            return GetLegPath(pt1, legRadius1, pt2, legRadius2, bends, gaps, dashPoint);
        }

        // If a crossing point is stretched, we need to adjust it's location for the purposes of the end 
        // of the leg path. The "ptOtherEnd" is the other end of the leg that tells us which way to adjust it.
        private static PointF AdjustedCrossingPointLocation(EventDB eventDB, Id<ControlPoint> controlId, PointF ptOtherEnd, float courseObjRatio)
        {
            ControlPoint control = eventDB.GetControl(controlId);
            if (control.kind == ControlPointKind.CrossingPoint && control.stretch > 0) {
                // Get the two possible places to put the new endpoint.
                PointF possiblePt1 = Geometry.MoveDistance(control.location, courseObjRatio * (control.stretch / 2), control.orientation + 90);
                PointF possiblePt2 = Geometry.MoveDistance(control.location, courseObjRatio * (control.stretch / 2), control.orientation - 90);

                // Pick the closest one to the other end of the leg.
                if (Geometry.Distance(possiblePt1, ptOtherEnd) < Geometry.Distance(possiblePt2, ptOtherEnd)) {
                    return possiblePt1;
                }
                else {
                    return possiblePt2;
                }
            }
            else {
                // Not stretched.
                return control.location;
            }
        }

        // Create a path from pt1 to pt2, with the given radius around the legs. If the leg would
        // be of zero length, return null. If bends is non-null, then the path should include those bends.
        // If gaps is non-null, updates the gaps by subtracting the radius from them.
        private static SymPath GetLegPath(PointF pt1, double radius1, PointF pt2, double radius2, PointF[] bends, LegGap[] gaps, PointF? dashPoint)
        {
            double legLength = Geometry.Distance(pt1, pt2);

            // Check for no leg.
            if (legLength <= radius1 + radius2)
                return null;

            int bendCount = (bends == null) ? 0 : bends.Length;
            PointF[] coords = new PointF[2 + bendCount];
            PointKind[] kinds = new PointKind[2 + bendCount];

            // Set the end points.
            coords[0] = Geometry.DistanceAlongLine(pt1, (bendCount > 0) ? bends[0] : pt2, radius1);
            coords[coords.Length - 1] = Geometry.DistanceAlongLine(pt2, (bendCount > 0) ? bends[bends.Length - 1] : pt1, radius2);

            // Set the bends.
            if (bendCount > 0)
                Array.Copy(bends, 0, coords, 1, bendCount);

            // Create the path.
            for (int i = 0; i < kinds.Length; ++i)
                kinds[i] = PointKind.Normal;

            // Update the gaps (if any).
            if (gaps != null) {
                for (int i = 0; i < gaps.Length; ++i)
                    gaps[i].distanceFromStart -= (float) radius1;
            }

            // If one is requested to be a dash point, set that.
            if (dashPoint.HasValue) {
                for (int i = 0; i < coords.Length; ++i) {
                    if (coords[i] == dashPoint.Value)
                        kinds[i] = PointKind.Dash;
                }
            }

            return new SymPath(coords, kinds);
        }

        // Get the radius of where the leg should start from a control point of a given kind.
        private static double GetLegRadius(ControlPointKind controlKind, float courseObjRatio, CourseAppearance appearance)
        {
            switch (controlKind) {
            case ControlPointKind.CrossingPoint:
                return courseObjRatio * NormalCourseAppearance.crossingRadius * appearance.controlCircleSize;

            case ControlPointKind.Normal:
                return courseObjRatio * ((appearance.ControlCircleOutsideDiameter / 2F) - (NormalCourseAppearance.lineThickness * appearance.lineWidth / 2F));

            case ControlPointKind.Finish:
                return courseObjRatio * ((appearance.FinishCircleOutsideDiameter / 2F) - (NormalCourseAppearance.lineThickness * appearance.lineWidth / 2F));

            case ControlPointKind.Start:
            case ControlPointKind.MapExchange:
                return courseObjRatio * appearance.StartRadius;

            case ControlPointKind.None:
            case ControlPointKind.MapIssue:
                return 0;

            default:
                Debug.Fail("Bad kind");
                return 0;
            }
        }

        // Get the angle, in radians, that the start angle is 
        public static double ComputeStartAngleOut(EventDB eventDB, CourseView courseView)
        {
            for (int i = 0; i < courseView.ControlViews.Count; ++i) {
                CourseView.ControlView controlView = courseView.ControlViews[i];

                if (eventDB.GetControl(controlView.controlId).kind == ControlPointKind.Start) {
                    return ComputeAngleOut(eventDB, courseView, i);
                }
            }

            return double.NaN;
        }

        // Get the angle, in radians, from the given control index to the next control. 
        public static double ComputeAngleOut(EventDB eventDB, CourseView courseView, int controlIndex)
        {
            PointF pt1 = eventDB.GetControl(courseView.ControlViews[controlIndex].controlId).location;

            // Get index of next control.
            int nextControlIndex = courseView.GetNextControl(controlIndex);
            if (nextControlIndex < 0)
                return Math.PI / 2;

            // By default, the location of the next control is the direction.
            PointF pt2 = eventDB.GetControl(courseView.ControlViews[nextControlIndex].controlId).location;

            // If there is a custom leg, then use the location of the first bend instead. 
            Id<Leg> legId = QueryEvent.FindLeg(eventDB, courseView.ControlViews[controlIndex].controlId, courseView.ControlViews[nextControlIndex].controlId);
            if (legId.IsNotNone) {
                Leg leg = eventDB.GetLeg(legId);
                if (leg.bends != null && leg.bends.Length > 0)
                    pt2 = leg.bends[0];
            }

            return Math.Atan2(pt2.Y - pt1.Y, pt2.X - pt1.X);
        }

        // Get the amount to adjust the radius of a triage for a line going in at angleIn, assuming the triangle
        // is oriented to point to angleOut;
        private static double StartTriangleRadiusAdjustment(double angleIn, double angleOut)
        {
            if (double.IsNaN(angleIn) || double.IsNaN(angleOut))
                return 1;

            double oneThirdCircle = Math.PI * 2.0 / 3.0;
            double netAngle = Math.Abs(Math.IEEERemainder(angleOut - angleIn, oneThirdCircle));

            // Find the intersection between a ray coming from the origin at the given net angle, and a side
            // of a triangle. netAngle has been constrained by symmetry to intersect that side.
            PointF rayEnd = new PointF((float) (2 * Math.Cos(netAngle)), (float)(2 * Math.Sin(netAngle)));
            PointF end1 = new PointF((float) Math.Cos(oneThirdCircle / 2), (float) Math.Sin(oneThirdCircle / 2));
            PointF end2 = new PointF(end1.X, -end1.Y);
            PointF intersectionPoint;
            if (Geometry.LineSegmentsIntersect(end1, end2, new PointF(0, 0), rayEnd, out intersectionPoint)) {
                return Geometry.Distance(new PointF(0, 0), intersectionPoint);
            }
            else {
                return 1;
            }
        }

        // Cut any overlapping control circles in the given layer.
        private static void AutoCutCircles(CourseLayout courseLayout, CourseLayer layer)
        {
            foreach (CourseObj courseObj in courseLayout) {
                if (courseObj.layer == layer && (courseObj is ControlCourseObj || courseObj is FinishCourseObj))
                    AutoCutControl((PointCourseObj) courseObj, courseLayout);
            }
        }

        // Check this control and add cuts to it if needed.
        private static void AutoCutControl(PointCourseObj controlObj, CourseLayout courseLayout)
        {
            foreach (CourseObj courseObj in courseLayout) {
                if (courseObj != controlObj && courseObj.layer == controlObj.layer && courseObj is PointCourseObj)
                    CutControlWithRespectTo(controlObj, (PointCourseObj)courseObj);
            }
        }

        // Cut "controlObj" with respect to "courseObj", if courseObj is close enough to overlap.
        private static void CutControlWithRespectTo(PointCourseObj controlObj, PointCourseObj courseObj)
        {
            float radiusControl = controlObj.ApparentRadius;
            float radiusOther = courseObj.ApparentRadius;
            double distance = Geometry.Distance(controlObj.location, courseObj.location);

            if (distance < (radiusControl + radiusOther) * 0.9F && distance > (radiusControl + radiusOther) * 0.35F) {
                // The other object is close enough to the control to merit cutting, but not too close. (0.9 and 0.35 were just arrived by what looks good.)

                // Law of cosines...
                double arcCos = (radiusControl * radiusControl + distance * distance - radiusOther * radiusOther) / (2 * radiusControl * distance);
                if (Math.Abs(arcCos) < 1) { 
                    float halfAngleGap = (float)(180 * Math.Acos(arcCos) / Math.PI);
                    float angleGap = Geometry.Angle(controlObj.location, courseObj.location);

                    controlObj.gaps = CircleGap.AddGap(controlObj.gaps, angleGap - halfAngleGap, angleGap + halfAngleGap);
                }
            }
        }

        // Cut any overlapping legs in the given layer.
        private static void AutoCutLegs(EventDB eventDB, CourseAppearance appearance, CourseDesignator courseDesignator, CourseLayout courseLayout, CourseLayer layer)
        {
            if (appearance.autoLegGapSize <= 0)
                return;     // No cutting requested.

            foreach (CourseObj courseObj in courseLayout) {
                if (courseObj.layer == layer && (courseObj is LegCourseObj || courseObj is FlaggedLegCourseObj))
                    AutoCutLeg(eventDB, appearance, courseDesignator, (LineCourseObj)courseObj, courseLayout);
            }
        }

        // Check this leg and add cuts to it if needed.
        private static void AutoCutLeg(EventDB eventDB, CourseAppearance appearance, CourseDesignator courseDesignator, LineCourseObj legObj, CourseLayout courseLayout)
        {
            foreach (CourseObj courseObj in courseLayout) {
                if (courseObj != legObj && courseObj.layer == legObj.layer && (courseObj is LegCourseObj || courseObj is FlaggedLegCourseObj))
                    CutLegWithRespectTo(eventDB, appearance, courseDesignator, legObj, (LineCourseObj)courseObj);
                if (courseObj != legObj && courseObj.layer == legObj.layer && courseObj is PointCourseObj)
                    CutLegWithRespectTo(eventDB, appearance, courseDesignator, legObj, (PointCourseObj)courseObj);
            }
        }

        // Cut the leg "legObj" with respect to "otherObj", if they intersect
        private static void CutLegWithRespectTo(EventDB eventDB, CourseAppearance appearance, CourseDesignator courseDesignator, LineCourseObj legObj, LineCourseObj otherObj)
        {
            PointF[] intersectionPoints;

            if (legObj.path.Intersects(otherObj.path, out intersectionPoints) && intersectionPoints != null) {
                // The other line intersections this one. Only the later leg is split.
                if (QueryEvent.DoesCourseControlPrecede(eventDB, courseDesignator, otherObj.courseControlId, legObj.courseControlId)) {
                    foreach (PointF intersectionPoint in intersectionPoints) {
                        float gapRadius = legObj.courseObjRatio * (appearance.autoLegGapSize / 2);
                        CutLegAtPoint(legObj, intersectionPoint, gapRadius);
                    }
                }
            }
        }

        private static void CutLegAtPoint(LineCourseObj legObj, PointF intersectionPoint, float gapRadius)
        {
            float distanceAlongLine = legObj.path.LengthToPoint(intersectionPoint);
            PointF pt1 = legObj.path.PointAtLength(distanceAlongLine - gapRadius);
            PointF pt2 = legObj.path.PointAtLength(distanceAlongLine + gapRadius);
            legObj.gaps = LegGap.AddGap(legObj.path, legObj.gaps, pt1, pt2);
        }

        // Cut the leg "legObj" with respect to "otherObj", if they overlap
        private static void CutLegWithRespectTo(EventDB eventDB, CourseAppearance appearance, CourseDesignator courseDesignator, LineCourseObj legObj, PointCourseObj otherObj)
        {
            if (otherObj.ApparentRadius == 0)
                return;

            float radiusOther = otherObj.ApparentRadius + (appearance.lineWidth * NormalCourseAppearance.lineThickness * 2);
            PointF nearestPointOnLeg;
            float distance = legObj.path.DistanceFromPoint(otherObj.location, out nearestPointOnLeg);

            if (distance < radiusOther && Geometry.Distance(nearestPointOnLeg, legObj.path.FirstPoint) > 0.5 && Geometry.Distance(nearestPointOnLeg, legObj.path.LastPoint) > 0.5) {
                float gapRadius = (float) Math.Sqrt(radiusOther * radiusOther - distance * distance);  // pythagorean theorem.
                gapRadius += appearance.autoLegGapSize / 2;

                CutLegAtPoint(legObj, nearestPointOnLeg, gapRadius);
            }
        }

    }
}
