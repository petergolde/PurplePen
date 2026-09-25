#if TEST
using System;
using System.Collections.Generic;
using System.Drawing;
using TestingUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using PurplePen.Graphics2D;
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
            course.SetLayerColor(CourseLayer.InvisibleObjects, 2, "DropTargets", 1F, 1F, 0, 0, false);
            TopologyFormatter formatter = new TopologyFormatter();
            RectangleF rect = formatter.FormatCourseToLayout(symbolDB, courseViewAllVariations, courseView, course, Id<CourseControl>.None, Id<CourseControl>.None, CourseLayer.AllVariations, CourseLayer.MainCourse);

            // Render to a map
            Map map = course.RenderToMap(new CourseLayout.MapRenderOptions());

            // Make drop targets visible.
            using (map.Write()) {
                foreach (SymDef symdef in map.AllSymdefs) {
                    if (symdef.SymbolId == "781")
                        map.SetSymdefVisible(symdef, true);
                }
            }

            // Render map to the graphics.
            int width = (int)(1000 * rect.Width / rect.Height);
            int height = 1000;
            RenderOptions options = new RenderOptions();

            options.usePatternBitmaps = true;
            options.minResolution = rect.Width / width;
            options.renderTemplates = RenderTemplateOption.MapAndTemplates;

            Bitmap bm = TestRenderingUtils.RenderToBitmap(width, height, rect, true, graphicsTarget => {
                using (map.Read())
                    map.Draw(graphicsTarget, rect, options, null);
            });

            BitmapTestUtil.CheckBitmapsBase(bm, "topologyformatter\\" + testName);
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
            VariationInfo variationInfo = new VariationInfo("AEFDCI", variationPath);

            CourseDesignator courseDesignator = new CourseDesignator(CourseId(1), variationInfo);
            CheckCourse("topologyformatter\\variations.ppen", courseDesignator, "complexvariations_onepath");
        }

    }
}

#endif //TEST
