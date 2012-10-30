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

#if TEST
using System;
using System.Collections.Generic;
using System.Drawing;
using TestingUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using PurplePen.MapModel;

namespace PurplePen.Tests
{
    [TestClass]
    public class CourseRendererTests: TestFixtureBase
    {
        void CheckCourse(string filename, CourseDesignator courseDesignator, bool addAllControls, string testName, RectangleF rect, CourseAppearance appearance)
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            CourseView courseView;
            CourseLayout course;

            eventDB.Load(TestUtil.GetTestFile(filename));
            eventDB.Validate();

            // Create the course
            courseView = CourseView.CreateViewingCourseView(eventDB, courseDesignator);
            course = new CourseLayout();
            course.SetLayerColor(CourseLayer.Descriptions, 0, "Black", 0, 0, 0, 1F);
            course.SetLayerColor(CourseLayer.MainCourse, 11, "Purple", 0.2F, 1.0F, 0.0F, 0.07F);
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, appearance, course, CourseLayer.MainCourse);

            // Add all controls if requested.
            if (addAllControls && courseDesignator.IsNotAllControls) {
                courseView = CourseView.CreateFilteredAllControlsView(eventDB, new CourseDesignator[] { courseDesignator }, ControlPointKind.None, false, true);
                course.SetLayerColor(CourseLayer.AllControls, 12, "LightPurple", 0.1F, 0.5F, 0.0F, 0.0F);
                CourseFormatter.FormatCourseToLayout(symbolDB, courseView, appearance, course, CourseLayer.AllControls);
            }

            // Render to a map
            Map map = course.RenderToMap();

            // Render map to the graphics.
            Bitmap bm = new Bitmap(1000,1000);
            using (Graphics g = Graphics.FromImage(bm)) {
                RenderOptions options = new RenderOptions();

                options.usePatternBitmaps = true;
                options.minResolution = (float) (rect.Width / bm.Width);

                g.ScaleTransform((float) (bm.Width / rect.Width), - (float) (bm.Height / rect.Height));
                g.TranslateTransform(-rect.Left, -rect.Top-rect.Height);

                g.Clear(Color.White);
                using (map.Read())
                    map.Draw(new GDIPlus_GraphicsTarget(g), rect, options, null);
            }

            TestUtil.CheckBitmapsBase(bm, "courserenderer\\" + testName);
        }

        // Do CheckCourse for normal and special appearances.
        void CheckCourseBothAppearances(string filename, CourseDesignator courseDesignator, bool addAllControls, string testName, RectangleF rect)
        {
            CourseAppearance specialAppearance;

            // Special appearance to test the usage of CourseAppearance.
            specialAppearance = new CourseAppearance();
            specialAppearance.controlCircleSize = 1.6F;  //big control circle
            specialAppearance.lineWidth = 0.2F; // thin lines
            specialAppearance.numberHeight = 2F; // really big numbers.
            specialAppearance.numberBold = true; // bold numbers
            specialAppearance.useDefaultPurple = false;
            specialAppearance.purpleC = 0.32F;
            specialAppearance.purpleY = 1.00F;
            specialAppearance.purpleM = 0;
            specialAppearance.purpleK = 0.30F;

            CheckCourse(filename, courseDesignator, addAllControls, testName, rect, defaultCourseAppearance);
            CheckCourse(filename, courseDesignator, addAllControls, testName + "_special", rect, specialAppearance);
        }


        [TestMethod]
        public void StartTriangle()
        {
            CheckCourseBothAppearances("courserenderer\\marymoor1.coursescribe", Designator(8), false, "start_triangle", new RectangleF(5, -20, 60, 60));    
        }


        [TestMethod]
        public void DefaultNumberLocation()
        {
            CheckCourseBothAppearances("courserenderer\\marymoor1.coursescribe", Designator(7), false, "default_number_loc", new RectangleF(120, -5, 20, 20));
        }

        [TestMethod]
        public void RegularCourse()
        {
            CheckCourseBothAppearances("courserenderer\\marymoor1.coursescribe", Designator(3), false, "regular", new RectangleF(-10, -40, 120, 120));
            CheckCourseBothAppearances("courserenderer\\marymoor1.coursescribe", Designator(3), true, "regular_plusall", new RectangleF(-10, -40, 120, 120));
        }

        [TestMethod]
        public void RegularCourseWithInitialNumber()
        {
            CheckCourseBothAppearances("courserenderer\\marymoor4.coursescribe", Designator(3), false, "initnum", new RectangleF(-10, -40, 120, 120));
            CheckCourseBothAppearances("courserenderer\\marymoor4.coursescribe", Designator(3), true, "initnum_plusall", new RectangleF(-10, -40, 120, 120));
        }

        [TestMethod]
        public void ScoreCourse()
        {
            CheckCourseBothAppearances("courserenderer\\marymoor1.coursescribe", Designator(9), false, "score", new RectangleF(-10, -40, 120, 120));
            CheckCourseBothAppearances("courserenderer\\marymoor1.coursescribe", Designator(9), true, "score_plusall", new RectangleF(-10, -40, 120, 120));
        }

        [TestMethod]
        public void ScoreCourseSequence() {
            CheckCourseBothAppearances("courserenderer\\marymoor5.coursescribe", Designator(9), false, "scoreseq", new RectangleF(-10, -40, 120, 120));
            CheckCourseBothAppearances("courserenderer\\marymoor5.coursescribe", Designator(9), true, "scoreseq_plusall", new RectangleF(-10, -40, 120, 120));
        }

        [TestMethod]
        public void ScoreCourseSequenceAndCode() {
            CheckCourseBothAppearances("courserenderer\\marymoor6.coursescribe", Designator(9), false, "scoreseqcode", new RectangleF(-10, -40, 120, 120));
            CheckCourseBothAppearances("courserenderer\\marymoor6.coursescribe", Designator(9), true, "scoreseqcode_plusall", new RectangleF(-10, -40, 120, 120));
        }

        [TestMethod]
        public void AllControls()
        {
            CheckCourseBothAppearances("courserenderer\\marymoor1.coursescribe", CourseDesignator.AllControls, false, "all_controls", new RectangleF(-20, -50, 160, 160));
        }

        [TestMethod]
        public void CrossingPoints()
        {
            CheckCourseBothAppearances("courserenderer\\marymoor3.coursescribe", Designator(2), false, "crossing", new RectangleF(-10, -40, 90, 90));
            CheckCourseBothAppearances("courserenderer\\marymoor3.coursescribe", Designator(2), true, "crossing_plusall", new RectangleF(-10, -40, 90, 90));
        }

        [TestMethod]
        public void Specials1()
        {
            CheckCourseBothAppearances("courserenderer\\marymoor2.coursescribe", Designator(3), false, "specials1", new RectangleF(-51, -36, 150, 150));
        }

        [TestMethod]
        public void Specials2()
        {
            CheckCourseBothAppearances("courserenderer\\marymoor2.coursescribe", Designator(10), false, "specials2", new RectangleF(-51, -36, 150, 150));
        }

        [TestMethod]
        public void AllControlsDescriptions()
        {
            CheckCourseBothAppearances("courserenderer\\marymoor2.coursescribe", CourseDesignator.AllControls, false, "allcontrolsdesc", new RectangleF(-51, -76, 150, 150));
        }

        [TestMethod]
        public void NoCoursesDescriptions()
        {
            CheckCourseBothAppearances("courserenderer\\nocourses.ppen", CourseDesignator.AllControls, false, "nocoursesdesc", new RectangleF(-51, -76, 150, 150));
        }

        [TestMethod]
        public void SpecialLegs()
        {
            CheckCourseBothAppearances("courserenderer\\speciallegs.coursescribe", Designator(1), false, "speciallegs", new RectangleF(0, -35, 110, 110));
        }

        [TestMethod]
        public void GappedLegs()
        {
            CheckCourseBothAppearances("courserenderer\\gappedlegs.coursescribe", Designator(1), false, "gappedlegs", new RectangleF(0, -35, 110, 110));
        }

        [TestMethod]
        public void AllControlsDifferentScale()
        {
            CheckCourseBothAppearances("courserenderer\\allcontrolsscale.ppen", CourseDesignator.AllControls, false, "allcontrolsscale", new RectangleF(-20, -50, 160, 160));
        }

        [TestMethod]
        public void AllControlsCodePos()
        {
            CheckCourseBothAppearances("courserenderer\\allcontrolscodepos.ppen", CourseDesignator.AllControls, false, "allcontrolscodepos", new RectangleF(-20, -50, 160, 160));
        }

        [TestMethod]
        public void OverlappedAllControls()
        {
            CheckCourseBothAppearances("courserenderer\\Overlapper.ppen", CourseDesignator.AllControls, false, "overlapped_all", new RectangleF(55, -25, 75, 75));
        }

        [TestMethod]
        public void Overlapped1()
        {
            CheckCourseBothAppearances("courserenderer\\Overlapper.ppen", Designator(1), false, "overlapped_1", new RectangleF(55, -25, 75, 75));
        }

        [TestMethod]
        public void Overlapped2()
        {
            CheckCourseBothAppearances("courserenderer\\Overlapper.ppen", Designator(2), false, "overlapped_2", new RectangleF(55, -25, 75, 75));
        }

        [TestMethod]
        public void Overlapped2WithAll()
        {
            CheckCourseBothAppearances("courserenderer\\Overlapper.ppen", Designator(2), true, "overlapped_2_all", new RectangleF(55, -25, 75, 75));
        }

        [TestMethod]
        public void OverlappedScore()
        {
            CheckCourseBothAppearances("courserenderer\\Overlapper.ppen", Designator(3), false, "overlapped_score", new RectangleF(55, -25, 75, 75));
        }

        [TestMethod]
        public void MapExchange()
        {
            CheckCourseBothAppearances("courserenderer\\mapexchange2.ppen", Designator(6), false, "exch_allparts", new RectangleF(-45, -60, 190, 190));
            CheckCourseBothAppearances("courserenderer\\mapexchange2.ppen", new CourseDesignator(CourseId(6), 0), false, "exch_part1", new RectangleF(-45, -60, 190, 190));
            CheckCourseBothAppearances("courserenderer\\mapexchange2.ppen", new CourseDesignator(CourseId(6), 1), false, "exch_part2", new RectangleF(-45, -60, 190, 190));
            CheckCourseBothAppearances("courserenderer\\mapexchange2.ppen", new CourseDesignator(CourseId(6), 2), false, "exch_part3", new RectangleF(-45, -60, 190, 190));
            CheckCourseBothAppearances("courserenderer\\mapexchange2.ppen", new CourseDesignator(CourseId(6), 3), false, "exch_part4", new RectangleF(-45, -60, 190, 190));
        }
    
    }
}

#endif //TEST
