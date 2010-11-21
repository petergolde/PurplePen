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
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

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

            Bitmap bm = new Bitmap((int) size.Width, (int) size.Height);
            Graphics g = Graphics.FromImage(bm);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            g.Clear(Color.White);
            punchesRenderer.Draw(g, 0, 0, 0, punchesRenderer.Boxes.Height);

            g.Dispose();

            return bm;
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

            if (id.IsNone)
                courseView = CourseView.CreateAllControlsView(eventDB);
            else
                courseView = CourseView.CreateCourseView(eventDB, id, false);

            Bitmap bmNew = RenderToBitmap(eventDB, courseView, format);
            TestUtil.CheckBitmapsBase(bmNew, GetBitmapFileName(eventDB, id, ""));
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
