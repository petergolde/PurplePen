using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace PurplePen.Tests
{
    public class TestFixtureBase
    {
        private TestContext testContextInstance;

        protected CourseAppearance defaultCourseAppearance = new CourseAppearance();          // Use when you the default course appearance.
        protected CourseAppearance std2017CourseAppearance = new CourseAppearance() { mapStandard = "2017" };  // ISOM2017 course appearance.
        protected CourseAppearance stdSpr2019CourseAppearance = new CourseAppearance() { mapStandard = "Spr2019" };  // ISSprOM2019 course appearance.

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }



        internal CourseDesignator Designator(int id)
        {
            return new CourseDesignator(CourseId(id));
        }

        internal CourseDesignator Designator(int id, int part)
        {
            return new CourseDesignator(CourseId(id), part);
        }

        internal Id<Course> CourseId(int id)
        {
            return new Id<Course>(id);
        }

        internal Id<ControlPoint> ControlId(int id)
        {
            return new Id<ControlPoint>(id);
        }

        internal Id<CourseControl> CourseControlId(int id)
        {
            return new Id<CourseControl>(id);
        }

        internal Id<Special> SpecialId(int id)
        {
            return new Id<Special>(id);
        }

        internal Id<Leg> LegId(int id)
        {
            return new Id<Leg>(id);
        }

        internal void CheckHighlightedLines(Controller controller, int expectedStartLine, int exepectedEndLine)
        {
            int first, last;
            controller.GetHighlightedDescriptionLines(out first, out last);
            Assert.AreEqual(expectedStartLine, first);
            Assert.AreEqual(exepectedEndLine, last);
        }
    }
}
