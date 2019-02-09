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
using System.Linq;

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
            course.SetLayerColor(CourseLayer.Descriptions, 0, "Black", 0, 0, 0, 1F, false);
            course.SetLayerColor(CourseLayer.MainCourse, 11, "Purple", 0.2F, 1.0F, 0.0F, 0.07F, false);
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, appearance, course, CourseLayer.MainCourse);

            // Add all controls if requested.
            if (addAllControls && courseDesignator.IsNotAllControls) {
                courseView = CourseView.CreateFilteredAllControlsView(eventDB, new CourseDesignator[] { courseDesignator }, ControlPointKind.None,
                    new CourseViewOptions() { showNonDescriptionSpecials = false, showDescriptionSpecials = true });
                course.SetLayerColor(CourseLayer.AllControls, 12, "LightPurple", 0.1F, 0.5F, 0.0F, 0.0F, false);
                CourseFormatter.FormatCourseToLayout(symbolDB, courseView, appearance, course, CourseLayer.AllControls);
            }

            // Render to a map
            Map map = course.RenderToMap(new CourseLayout.MapRenderOptions());

            // Render map to the graphics.
            Bitmap bm = new Bitmap(1000,1000);
            using (Graphics g = Graphics.FromImage(bm)) {
                RenderOptions options = new RenderOptions();

                options.usePatternBitmaps = true;
                options.minResolution = (float) (rect.Width / bm.Width);
                options.renderTemplates = RenderTemplateOption.MapAndTemplates;

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
            specialAppearance.numberOutlineWidth = 0.13F;
            specialAppearance.autoLegGapSize = 0.0F;
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
        public void ScoreCourseSequenceAndScore()
        {
            CheckCourseBothAppearances("courserenderer\\marymoor7.coursescribe", Designator(9), false, "scoreseqscore", new RectangleF(-20, -60, 150, 140));
            CheckCourseBothAppearances("courserenderer\\marymoor7.coursescribe", Designator(9), true, "scoreseqscore_plusall", new RectangleF(-20, -60, 150, 140));
        }

        [TestMethod]
        public void ScoreCourseCodeAndScore()
        {
            CheckCourseBothAppearances("courserenderer\\marymoor8.coursescribe", Designator(9), false, "scorecodescore", new RectangleF(-20, -60, 150, 140));
            CheckCourseBothAppearances("courserenderer\\marymoor8.coursescribe", Designator(9), true, "scorescodescore_plusall", new RectangleF(-20, -60, 150, 140));
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
        public void MulticolDescription()
        {
            CheckCourseBothAppearances("courserenderer\\marymoor2.coursescribe", Designator(4), false, "multicol", new RectangleF(-51, -36, 150, 150));
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
        public void TimedStart()
        {
            CheckCourseBothAppearances("courserenderer\\MapIssueTest.ppen", Designator(1), false, "timedstart1", new RectangleF(0, -50, 150, 150));
            CheckCourseBothAppearances("courserenderer\\MapIssueTest.ppen", Designator(2), false, "timedstart2", new RectangleF(0, -50, 150, 150));
            CheckCourseBothAppearances("courserenderer\\MapIssueTest.ppen", Designator(3), false, "timedstart3", new RectangleF(0, -50, 150, 150));
            CheckCourseBothAppearances("courserenderer\\MapIssueTest.ppen", Designator(4), false, "timedstart4", new RectangleF(-120, 0, 150, 150));
            CheckCourseBothAppearances("courserenderer\\MapIssueTest.ppen", Designator(5), false, "timedstart5", new RectangleF(-120, 0, 150, 150));
            CheckCourseBothAppearances("courserenderer\\MapIssueTest.ppen", Designator(6), false, "timedstart6", new RectangleF(-120, -50, 150, 150));

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

        [TestMethod]
        public void Butterfly1()
        {
            CheckCourseBothAppearances("courserenderer\\butterfly.ppen", Designator(7), false, "butterfly1", new RectangleF(-45, -60, 190, 190));
        }

        [TestMethod]
        public void Butterfly2()
        {
            CheckCourseBothAppearances("courserenderer\\butterfly.ppen", new CourseDesignator(CourseId(7), 0), false, "butterfly2", new RectangleF(-45, -60, 190, 190));
        }

        [TestMethod]
        public void Butterfly3()
        {
            CheckCourseBothAppearances("courserenderer\\butterfly.ppen", new CourseDesignator(CourseId(7), 1), false, "butterfly3", new RectangleF(-45, -60, 190, 190));
        }

        [TestMethod]
        public void ButterflyCode()
        {
            CheckCourseBothAppearances("courserenderer\\butterfly2.ppen", Designator(9), false, "butterflycode", new RectangleF(-45, -60, 190, 190));
        }

        [TestMethod]
        public void ButterflyNumberAndCode()
        {
            CheckCourseBothAppearances("courserenderer\\butterfly2.ppen", Designator(8), false, "butterflynumandcode", new RectangleF(-45, -60, 190, 190));
        }

        [TestMethod]
        public void RelayVariations()
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);

            eventDB.Load(TestUtil.GetTestFile("courserenderer\\Fake Relay.ppen"));
            eventDB.Validate();

            VariationInfo[] variationInfos = QueryEvent.GetAllVariations(eventDB, CourseId(1)).ToArray();

            CourseDesignator courseDesignator = new CourseDesignator(CourseId(1), variationInfos.First(vi => vi.CodeString == "BDGHK"));
            CheckCourseBothAppearances("courserenderer\\Fake Relay.ppen", courseDesignator, false, "relayvariations_1", RectangleF.FromLTRB(-80, -110, 110, 80));
            courseDesignator = new CourseDesignator(CourseId(1), variationInfos.First(vi => vi.CodeString == "ADFHJ"));
            CheckCourseBothAppearances("courserenderer\\Fake Relay.ppen", courseDesignator, false, "relayvariations_2", RectangleF.FromLTRB(-80, -110, 110, 80));
            courseDesignator = new CourseDesignator(CourseId(1), variationInfos.First(vi => vi.CodeString == "CEGIL"));
            CheckCourseBothAppearances("courserenderer\\Fake Relay.ppen", courseDesignator, false, "relayvariations_3", RectangleF.FromLTRB(-80, -110, 110, 80));
        }



        [TestMethod]
        public void SingleVariation()
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);

            eventDB.Load(TestUtil.GetTestFile("queryevent\\variations.ppen"));
            eventDB.Validate();

            VariationInfo.VariationPath variationPath = new VariationInfo.VariationPath(new[] {
                CourseControlId(2),
                CourseControlId(27),
                CourseControlId(30),
                CourseControlId(26),
                CourseControlId(25),
                CourseControlId(4),
                CourseControlId(28),
            });
            VariationInfo variationInfo = new VariationInfo("AEFDCI", variationPath);

            CourseDesignator courseDesignator = new CourseDesignator(CourseId(1), variationInfo);

            CheckCourseBothAppearances("queryevent\\variations.ppen", courseDesignator, false, "singlevariation", new RectangleF(-15, -100, 230, 230));
        }

        [TestMethod]
        public void AllVariations()
        {
            CourseDesignator courseDesignator = new CourseDesignator(CourseId(1));

            CheckCourseBothAppearances("queryevent\\variations.ppen", courseDesignator, false, "allvariations", new RectangleF(-15, -100, 230, 230));

        }

        [TestMethod]
        public void ScalingMethods()
        {
            CourseAppearance appearance = (CourseAppearance) defaultCourseAppearance.Clone();

            CourseDesignator courseDesignator = new CourseDesignator(CourseId(4));
            appearance.itemScaling = ItemScaling.None;
            CheckCourse("courserenderer\\scaling.ppen", courseDesignator, false, "scaling_noscale", new RectangleF(-22, -33, 160, 160), appearance);
            appearance.itemScaling = ItemScaling.RelativeToMap;
            CheckCourse("courserenderer\\scaling.ppen", courseDesignator, false, "scaling_maprelative", new RectangleF(-22, -33, 160, 160), appearance);
            appearance.itemScaling = ItemScaling.RelativeTo15000;
            CheckCourse("courserenderer\\scaling.ppen", courseDesignator, false, "scaling_15krelative", new RectangleF(-22, -33, 160, 160), appearance);

        }

    }
}

#endif //TEST
