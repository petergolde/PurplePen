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
    public class TopologyFormatterTests: TestFixtureBase
    {
        void CheckCourse(string filename, CourseDesignator courseDesignator, string testName)
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            CourseView courseView, courseViewAllVariations;
            CourseLayout course;

            eventDB.Load(TestUtil.GetTestFile(filename));
            eventDB.Validate();

            // Create the course
            courseView = CourseView.CreateViewingCourseView(eventDB, courseDesignator);
            if (courseDesignator.IsVariation)
                courseViewAllVariations = CourseView.CreateViewingCourseView(eventDB, courseDesignator.WithAllVariations());
            else
                courseViewAllVariations = courseView;

            course = new CourseLayout();
            course.SetLayerColor(CourseLayer.AllVariations, 1, "Gray", 0, 0, 0, 0.4F, false);
            course.SetLayerColor(CourseLayer.MainCourse, 0, "Black", 0, 0, 0, 1F, false);
            TopologyFormatter formatter = new TopologyFormatter();
            RectangleF rect = formatter.FormatCourseToLayout(symbolDB, courseViewAllVariations, courseView, course, CourseLayer.AllVariations, CourseLayer.MainCourse);

            // Render to a map
            Map map = course.RenderToMap();

            // Make drop targets visible.
            using (map.Write()) {
                foreach (SymDef symdef in map.AllSymdefs) {
                    if (symdef.SymbolId == "781")
                        map.SetSymdefVisible(symdef, true);
                }
            }

            // Render map to the graphics.
            Bitmap bm = new Bitmap((int)(1000 * rect.Width / rect.Height), 1000);
            using (Graphics g = Graphics.FromImage(bm)) {
                RenderOptions options = new RenderOptions();

                options.usePatternBitmaps = true;
                options.minResolution = (float)(rect.Width / bm.Width);
                options.renderTemplates = RenderTemplateOption.MapAndTemplates;

                g.ScaleTransform((float)(bm.Width / rect.Width), -(float)(bm.Height / rect.Height));
                g.TranslateTransform(-rect.Left, -rect.Top - rect.Height);

                g.Clear(Color.White);
                using (map.Read())
                    map.Draw(new GDIPlus_GraphicsTarget(g), rect, options, null);
            }

            TestUtil.CheckBitmapsBase(bm, "topologyformatter\\" + testName);
        }

        [TestMethod]
        public void CheckSimple()
        {
            CheckCourse("topologyformatter\\marymoor1.coursescribe", Designator(3), "simple");
        }

        [TestMethod]
        public void OneFork()
        {
            CheckCourse("topologyformatter\\variations.ppen", Designator(4), "onefork");
        }

        [TestMethod]
        public void EmptyFork()
        {
            CheckCourse("topologyformatter\\variations.ppen", Designator(5), "emptyfork");
        }

        [TestMethod]
        public void BothForksEmpty()
        {
            CheckCourse("topologyformatter\\variations.ppen", Designator(6), "bothemptyfork");
        }

        [TestMethod]
        public void NestedFork()
        {
            CheckCourse("topologyformatter\\variations.ppen", Designator(7), "nestedfork");
        }

        [TestMethod]
        public void SimpleLoop()
        {
            CheckCourse("topologyformatter\\variations.ppen", Designator(8), "simpleloop");
        }

        [TestMethod]
        public void ComplexVariations()
        {
            CheckCourse("topologyformatter\\variations.ppen", Designator(1), "complexvariations");
        }

        [TestMethod]
        public void ComplexVariationsOnePath()
        {
            VariationInfo.VariationPath variationPath = new VariationInfo.VariationPath(new[] {
                CourseControlId(2),
                CourseControlId(27),
                CourseControlId(30),
                CourseControlId(26),
                CourseControlId(25),
                CourseControlId(4),
                CourseControlId(28),
            });


            CourseDesignator courseDesignator = new CourseDesignator(CourseId(1), variationPath);
            CheckCourse("topologyformatter\\variations.ppen", courseDesignator, "complexvariations_onepath");
        }

    }
}

#endif //TEST
