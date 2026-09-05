#if TEST
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

using PurplePen.Graphics2D;
using PurplePen.MapModel;

namespace PurplePen.Tests
{
    [TestClass]
    public class PunchesRendererTests: TestFixtureBase
    {
        // Render a description to a bitmap for testing purposes. Hardcoded 70 pixel box size.
        internal static Bitmap RenderToBitmap(EventDB eventDB, CourseView courseView, PunchcardFormat format)
        {
            PunchesRenderer punchesRenderer = new PunchesRenderer(eventDB);
            punchesRenderer.CourseView = courseView;
            punchesRenderer.PunchcardFormat = format;
            punchesRenderer.CellSize = 70;
            punchesRenderer.Margin = 4;

            SizeF size = punchesRenderer.Measure();

            int width = (int) size.Width;
            int height = (int) size.Height;
            RectangleF drawingRectangle = new RectangleF(0, 0, width, height);
            return TestRenderingUtils.RenderToBitmap(width, height, drawingRectangle, false, graphicsTarget => {
                graphicsTarget.PushAntiAliasing(true);
                punchesRenderer.Draw(graphicsTarget, 0, 0, 0, punchesRenderer.Boxes.Height);
            });
        }

        // Get the file name for a bitmap description for testing purposes. CourseID == 0 means all controls. Extra
        // is an extra string to suffix to the base name. Does not end in .png unless specified in extra.
        internal static string GetBitmapFileName(EventDB eventDB, Id<Course> courseId, string extra)
        {
            Course course = null;
            string name;

            if (courseId.IsNotNone)
                course = eventDB.GetCourse(courseId);

            if (course != null)
                name = course.name;
            else
                name = "Allcontrols";

            name = "punchcards\\" + name + extra;

            return name;
        }

        // Render the given course id (0 = all controls) and kind to a bitmap, and compare it to the saved version.
        internal void CheckRenderBitmap(string filename, Id<Course> id, PunchcardFormat format)
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            CourseView courseView;

            eventDB.Load(filename);
            eventDB.Validate();

            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(id));

            Bitmap bmNew = RenderToBitmap(eventDB, courseView, format);
            BitmapTestUtil.CheckBitmapsBase(bmNew, GetBitmapFileName(eventDB, id, ""));
        }

        [TestMethod]
        public void AllControls()
        {
            PunchcardFormat format = new PunchcardFormat();
            format.boxesAcross = 6;
            format.boxesDown = 3;
            format.leftToRight = true;
            format.topToBottom = true;

            CheckRenderBitmap(TestUtil.GetTestFile("punchcards\\sample1.ppen"), CourseId(0), format);
        }

        [TestMethod]
        public void RegularCourse1()
        {
            PunchcardFormat format = new PunchcardFormat();
            format.boxesAcross = 9;
            format.boxesDown = 3;
            format.leftToRight = false;
            format.topToBottom = false;

            CheckRenderBitmap(TestUtil.GetTestFile("punchcards\\sample1.ppen"), CourseId(2), format);
        }

        [TestMethod]
        public void AlternateStart1()
        {
            PunchcardFormat format = new PunchcardFormat();
            format.boxesAcross = 9;
            format.boxesDown = 3;
            format.leftToRight = false;
            format.topToBottom = false;

            CheckRenderBitmap(TestUtil.GetTestFile("punchcards\\sample2.ppen"), CourseId(2), format);
        }

        [TestMethod]
        public void RegularCourse2()
        {
            PunchcardFormat format = new PunchcardFormat();
            format.boxesAcross = 4;
            format.boxesDown = 3;
            format.leftToRight = true;
            format.topToBottom = false;

            CheckRenderBitmap(TestUtil.GetTestFile("punchcards\\sample1.ppen"), CourseId(6), format);
        }

        [TestMethod]
        public void RegularCourse3()
        {
            PunchcardFormat format = new PunchcardFormat();
            format.boxesAcross = 8;
            format.boxesDown = 3;
            format.leftToRight = true;
            format.topToBottom = true;

            CheckRenderBitmap(TestUtil.GetTestFile("punchcards\\sample1.ppen"), CourseId(4), format);
        }

        [TestMethod]
        public void ScoreCourse()
        {
            PunchcardFormat format = new PunchcardFormat();
            format.boxesAcross = 8;
            format.boxesDown = 3;
            format.leftToRight = true;
            format.topToBottom = true;

            CheckRenderBitmap(TestUtil.GetTestFile("punchcards\\sample1.ppen"), CourseId(7), format);
        }

    }
}
#endif
