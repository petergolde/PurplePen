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
using PurplePen.DebugUI;

using PurplePen.MapModel;

namespace PurplePen.Tests
{
    [TestClass]
    public class DescriptionRendererTests: TestFixtureBase
    {
        // Render the given course id (0 = all controls) and kind to a bitmap, and compare it to the saved version.
        internal void CheckRenderBitmap(string filename, Id<Course> id, DescriptionKind kind)
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

            DescriptionLine[] description = DescriptionFormatter.CreateDescription(courseView, symbolDB, kind == DescriptionKind.Symbols);

            Bitmap bmNew = DescriptionBrowser.RenderToBitmap(symbolDB, description, kind);
            TestUtil.CheckBitmapsBase(bmNew, DescriptionBrowser.GetBitmapFileName(eventDB, id, "", kind));
        }

        // Render a description to a bitmap for testing purposes. Does one pixel at a time to test clip rectangle.
        internal static Bitmap RenderToBitmapPixelAtATime(SymbolDB symbolDB, DescriptionLine[] description, DescriptionKind kind)
        {
            DescriptionRenderer descriptionRenderer = new DescriptionRenderer(symbolDB);
            descriptionRenderer.Description = description;
            descriptionRenderer.DescriptionKind = kind;
            descriptionRenderer.CellSize = 40;
            descriptionRenderer.Margin = 4;

            SizeF size = descriptionRenderer.Measure();

            Bitmap bm = new Bitmap((int)size.Width, (int)size.Height);
            Graphics g = Graphics.FromImage(bm);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            g.Clear(Color.White);

            for (int x = 0; x < size.Width; ++x) {
                for (int y = 0; y < size.Height; ++y) {
                    Rectangle clip = new Rectangle(x, y, 1, 1);
                    g.SetClip(clip);
                    descriptionRenderer.RenderToGraphics(g, clip);
                }
            }

            g.Dispose();

            return bm;
        }

        // Render the given course id (0 = all controls) and kind to a bitmap, and compare it to the saved version.
        internal void CheckRenderBitmapPixelAtATime(Id<Course> id, DescriptionKind kind)
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            CourseView courseView;

            eventDB.Load(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"));
            eventDB.Validate();

            if (id.IsNone)
                courseView = CourseView.CreateAllControlsView(eventDB);
            else
                courseView = CourseView.CreateCourseView(eventDB, id, false);

            DescriptionLine[] description = DescriptionFormatter.CreateDescription(courseView, symbolDB, false);

            Bitmap bmNew = RenderToBitmapPixelAtATime(symbolDB, description, kind);
            TestUtil.CheckBitmapsBase(bmNew, DescriptionBrowser.GetBitmapFileName(eventDB, id, "", kind));
        }

        [TestMethod]
        public void AllControlsSymbols()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(0), DescriptionKind.Symbols);
        }

        [TestMethod]
        public void AllControlsText()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(0), DescriptionKind.Text);
        }

        [TestMethod]
        public void AllControlsSymbolsAndText()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(0), DescriptionKind.SymbolsAndText);
        }

        [TestMethod]
        public void RegularSymbols()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(4), DescriptionKind.Symbols);
        }

        [TestMethod]
        public void RegularText()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(4), DescriptionKind.Text);
        }

        [TestMethod]
        public void RegularSymbolsAndText()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(4), DescriptionKind.SymbolsAndText);
        }

        [TestMethod]
        public void ScoreSymbols()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(5), DescriptionKind.Symbols);
        }

        [TestMethod]
        public void ScoreText()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(3), DescriptionKind.Text);
        }

        [TestMethod]
        public void ScoreSymbolsAndText()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(5), DescriptionKind.SymbolsAndText);
        }

        [TestMethod]
        public void RegularSymbols2()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(6), DescriptionKind.Symbols);
        }

        [TestMethod]
        public void EmptySymbols()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(2), DescriptionKind.Symbols);
        }

        [TestMethod]
        public void ShrinkText()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent2.ppen"), CourseId(1), DescriptionKind.Symbols);
        }

        [TestMethod]
        public void CustomSymbolKey()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent3.ppen"), CourseId(6), DescriptionKind.Symbols);
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent3.ppen"), CourseId(6), DescriptionKind.SymbolsAndText);
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent3.ppen"), CourseId(6), DescriptionKind.Text);
        }

        [TestMethod]
        public void TextLines()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\desctext.ppen"), CourseId(6), DescriptionKind.Symbols);
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\desctext.ppen"), CourseId(6), DescriptionKind.SymbolsAndText);
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\desctext.ppen"), CourseId(6), DescriptionKind.Text);
        }

        [TestMethod] 
        public void MultiLineTitle()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent6.coursescribe"), CourseId(1), DescriptionKind.Symbols);
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent6.coursescribe"), CourseId(1), DescriptionKind.SymbolsAndText);
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent6.coursescribe"), CourseId(1), DescriptionKind.Text);
        }

        [TestMethod]
        public void LongDescriptions()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent7.ppen"), CourseId(3), DescriptionKind.SymbolsAndText);
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent7.ppen"), CourseId(3), DescriptionKind.Text);
        }
	

        // Render a description to a map, then to a bitmap for testing purposes. Hardcoded 6 mm box size.
        internal static Bitmap RenderToMapThenToBitmap(SymbolDB symbolDB, DescriptionLine[] description, DescriptionKind kind)
        {
            DescriptionRenderer descriptionRenderer = new DescriptionRenderer(symbolDB);
            descriptionRenderer.Description = description;
            descriptionRenderer.DescriptionKind = kind;
            descriptionRenderer.CellSize = 6.0F;
            descriptionRenderer.Margin = 0.7F;
            PointF location = new PointF(30, -100);

            SizeF size = descriptionRenderer.Measure();

            Bitmap bm = new Bitmap((int) size.Width * 8, (int) size.Height * 8);
            Graphics g = Graphics.FromImage(bm);
            g.ScaleTransform(bm.Width / size.Width, -bm.Height / size.Height);
            g.TranslateTransform(-location.X, -location.Y);

            g.Clear(Color.White);

            Map map = new Map();
            using (map.Write()) {
                SymColor color = map.AddColor("Purple", 11, 0.045F, 0.59F, 0, 0.255F);
                descriptionRenderer.RenderToMap(map, color, location, new Dictionary<object,SymDef>());
            }

            InputOutput.WriteFile(TestUtil.GetTestFile("descriptions\\desc_temp.ocd"), map, 8);

            using (map.Read()) {
                RenderOptions renderOpts = new RenderOptions();
                renderOpts.usePatternBitmaps = true;
                renderOpts.minResolution = 0.1F;
                map.Draw(g, new RectangleF(location.X, location.Y - size.Height, size.Width, size.Height), renderOpts);
            }

            g.Dispose();

            return bm;
        }

        // Render the given course id (0 = all controls) and kind to a map, and compare it to the saved version.
        internal void CheckRenderMap(string filename, Id<Course> id, DescriptionKind kind)
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

            DescriptionLine[] description = DescriptionFormatter.CreateDescription(courseView, symbolDB, kind == DescriptionKind.Symbols);

            Bitmap bmNew = RenderToMapThenToBitmap(symbolDB, description, kind);
            TestUtil.CheckBitmapsBase(bmNew, DescriptionBrowser.GetBitmapFileName(eventDB, id, "_ocad", kind));
        }

        [TestMethod]
        public void AllControlsSymbolsToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(0), DescriptionKind.Symbols);
        }

        [TestMethod]
        public void AllControlsTextToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(0), DescriptionKind.Text);
        }

        [TestMethod]
        public void AllControlsSymbolsAndTextToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(0), DescriptionKind.SymbolsAndText);
        }

        [TestMethod]
        public void RegularSymbolsToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(4), DescriptionKind.Symbols);
        }

        [TestMethod]
        public void RegularTextToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(4), DescriptionKind.Text);
        }

        [TestMethod]
        public void RegularSymbolsAndTextToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(4), DescriptionKind.SymbolsAndText);
        }

        [TestMethod]
        public void ScoreSymbolsToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(5), DescriptionKind.Symbols);
        }

        [TestMethod]
        public void ScoreTextToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(3), DescriptionKind.Text);
        }

        [TestMethod]
        public void ScoreSymbolsAndTextToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(5), DescriptionKind.SymbolsAndText);
        }

        [TestMethod]
        public void RegularSymbols2ToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(6), DescriptionKind.Symbols);
        }

        [TestMethod]
        public void EmptySymbolsToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(2), DescriptionKind.Symbols);
        }

        [TestMethod]
        public void ShrinkTextToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent2.ppen"), CourseId(1), DescriptionKind.Symbols);
        }

        [TestMethod]
        public void CustomSymbolKeyToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent3.ppen"), CourseId(6), DescriptionKind.Symbols);
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent3.ppen"), CourseId(6), DescriptionKind.SymbolsAndText);
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent3.ppen"), CourseId(6), DescriptionKind.Text);
        }

        [TestMethod]
        public void TextLinesToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\desctext.ppen"), CourseId(6), DescriptionKind.Symbols);
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\desctext.ppen"), CourseId(6), DescriptionKind.SymbolsAndText);
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\desctext.ppen"), CourseId(6), DescriptionKind.Text);
        }

        [TestMethod]
        public void MultiLineTitleToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent6.coursescribe"), CourseId(1), DescriptionKind.Symbols);
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent6.coursescribe"), CourseId(1), DescriptionKind.SymbolsAndText);
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent6.coursescribe"), CourseId(1), DescriptionKind.Text);
        }

        [TestMethod]
        public void LongDescriptionsToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent7.ppen"), CourseId(3), DescriptionKind.SymbolsAndText);
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent7.ppen"), CourseId(3), DescriptionKind.Text);
        }
	

#if false  // These tests are too slow to run normally.

        [TestMethod]
        public void AllControlsSymbolsPixelAtATime()
        {
            CheckRenderBitmapPixelAtATime(0, DescriptionKind.Symbols);
        }

        [TestMethod]
        public void AllControlsTextPixelAtATime()
        {
            CheckRenderBitmapPixelAtATime(0, DescriptionKind.Text);
        }

        [TestMethod]
        public void AllControlsSymbolsAndTextPixelAtATime()
        {
            CheckRenderBitmapPixelAtATime(0, DescriptionKind.SymbolsAndText);
        }

        [TestMethod]
        public void RegularSymbolsPixelAtATime()
        {
            CheckRenderBitmapPixelAtATime(4, DescriptionKind.Symbols);
        }

        [TestMethod]
        public void RegularTextPixelAtATime()
        {
            CheckRenderBitmapPixelAtATime(4, DescriptionKind.Text);
        }

        [TestMethod]
        public void RegularSymbolsAndTextPixelAtATime()
        {
            CheckRenderBitmapPixelAtATime(4, DescriptionKind.SymbolsAndText);
        }

        [TestMethod]
        public void ScoreSymbolsPixelAtATime()
        {
            CheckRenderBitmapPixelAtATime(5, DescriptionKind.Symbols);
        }

        [TestMethod]
        public void ScoreTextPixelAtATime()
        {
            CheckRenderBitmapPixelAtATime(3, DescriptionKind.Text);
        }

        [TestMethod]
        public void ScoreSymbolsAndTextPixelAtATime()
        {
            CheckRenderBitmapPixelAtATime(5, DescriptionKind.SymbolsAndText);
        }

        [TestMethod]
        public void RegularSymbols2PixelAtATime()
        {
            CheckRenderBitmapPixelAtATime(6, DescriptionKind.Symbols);
        }

        [TestMethod]
        public void EmptySymbolsPixelAtATime()
        {
            CheckRenderBitmapPixelAtATime(2, DescriptionKind.Symbols);
        }

#endif 

        void CheckHitTest(DescriptionRenderer renderer, Point pt, HitTestKind expectedKind, int expectedFirstLine, int expectedLastLine, int expectedBox, RectangleF expectedRect)
        {
            HitTestResult result;
            result = renderer.HitTest(pt);

            Assert.AreEqual(expectedKind, result.kind);
            Assert.AreEqual(expectedFirstLine, result.firstLine);
            Assert.AreEqual(expectedLastLine, result.lastLine);
            Assert.AreEqual(expectedBox, result.box);
            Assert.AreEqual(expectedRect, result.rect);
        }

        [TestMethod]
        public void HitTestAllControls()
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            eventDB.Load(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"));
            eventDB.Validate();
            CourseView courseView = CourseView.CreateAllControlsView(eventDB);
            DescriptionLine[] description = DescriptionFormatter.CreateDescription(courseView, symbolDB, false);
            DescriptionRenderer descriptionRenderer = new DescriptionRenderer(symbolDB);
            descriptionRenderer.Description = description;
            descriptionRenderer.CellSize = 40;
            descriptionRenderer.Margin = 5;

            descriptionRenderer.DescriptionKind = DescriptionKind.Symbols;

            CheckHitTest(descriptionRenderer, new Point(143, 22), HitTestKind.Title, 0, 0, 0, new RectangleF(5, 5, 320, 40));
            CheckHitTest(descriptionRenderer, new Point(116, 53), HitTestKind.Header, 1, 1, 0, new RectangleF(5, 45, 120, 40));
            CheckHitTest(descriptionRenderer, new Point(259,78), HitTestKind.Header, 1, 1, 1, new RectangleF(125, 45, 200, 40));
            CheckHitTest(descriptionRenderer, new Point(38,97), HitTestKind.NormalBox, 2, 2, 0, new RectangleF(5,85, 40, 40));
            CheckHitTest(descriptionRenderer, new Point(226,260), HitTestKind.NormalBox, 6, 6, 5, new RectangleF(205,245, 40, 40));
            CheckHitTest(descriptionRenderer, new Point(68,999), HitTestKind.Directive, 24, 24, 0, new RectangleF(5, 965, 320, 40));
            CheckHitTest(descriptionRenderer, new Point(3, 184), HitTestKind.None, -1, -1, -1, new RectangleF(0, 0, 0, 0));
            CheckHitTest(descriptionRenderer, new Point(262,745), HitTestKind.OtherTextLine, 18, 18,0, new RectangleF(5, 725, 320, 40));

            descriptionRenderer.DescriptionKind = DescriptionKind.Text;

            CheckHitTest(descriptionRenderer, new Point(311,12), HitTestKind.Title, 0, 0, 0, new RectangleF(5, 5, 320, 40));
            CheckHitTest(descriptionRenderer, new Point(16,82), HitTestKind.Header, 1, 1, 0, new RectangleF(5, 45, 120, 40));
            CheckHitTest(descriptionRenderer, new Point(178,76), HitTestKind.Header, 1, 1, 1, new RectangleF(125, 45, 200, 40));
            CheckHitTest(descriptionRenderer, new Point(38, 97), HitTestKind.NormalBox, 2, 2, 0, new RectangleF(5, 85, 40, 40));
            CheckHitTest(descriptionRenderer, new Point(182,234), HitTestKind.NormalText, 5, 5, -1, new RectangleF(85,205, 240, 40));
            CheckHitTest(descriptionRenderer, new Point(60,942), HitTestKind.DirectiveText, 23, 23,-1, new RectangleF(5,925, 320, 40));
            CheckHitTest(descriptionRenderer, new Point(3, 184), HitTestKind.None, -1, -1, -1, new RectangleF(0, 0, 0, 0));
            CheckHitTest(descriptionRenderer, new Point(262,745), HitTestKind.OtherTextLine, 18, 18,0, new RectangleF(5, 725, 320, 40));

            descriptionRenderer.DescriptionKind = DescriptionKind.SymbolsAndText;

            CheckHitTest(descriptionRenderer, new Point(143, 22), HitTestKind.Title, 0, 0, 0, new RectangleF(5, 5, 520, 40));
            CheckHitTest(descriptionRenderer, new Point(434, 25), HitTestKind.Title, 0, 0, 0, new RectangleF(5, 5, 520, 40));
            CheckHitTest(descriptionRenderer, new Point(116, 53), HitTestKind.Header, 1, 1, 0, new RectangleF(5, 45, 120, 40));
            CheckHitTest(descriptionRenderer, new Point(259, 78), HitTestKind.Header, 1, 1, 1, new RectangleF(125, 45, 200, 40));
            CheckHitTest(descriptionRenderer, new Point(380, 76), HitTestKind.None, 1, 1, -1, new RectangleF(325,45, 200, 40));
            CheckHitTest(descriptionRenderer, new Point(38, 97), HitTestKind.NormalBox, 2, 2, 0, new RectangleF(5, 85, 40, 40));
            CheckHitTest(descriptionRenderer, new Point(226, 260), HitTestKind.NormalBox, 6, 6, 5, new RectangleF(205, 245, 40, 40));
            CheckHitTest(descriptionRenderer, new Point(68, 999), HitTestKind.Directive, 24, 24, 0, new RectangleF(5, 965, 320, 40));
            CheckHitTest(descriptionRenderer, new Point(398,554), HitTestKind.NormalText, 13, 13, -1, new RectangleF(325,525, 200, 40));
            CheckHitTest(descriptionRenderer, new Point(401, 934), HitTestKind.DirectiveText, 23, 23, -1, new RectangleF(325, 925, 200, 40));
            CheckHitTest(descriptionRenderer, new Point(3, 184), HitTestKind.None, -1, -1, -1, new RectangleF(0, 0, 0, 0));
            CheckHitTest(descriptionRenderer, new Point(262,745), HitTestKind.OtherTextLine, 18, 18,0, new RectangleF(5, 725, 520, 40));
        }

        [TestMethod]
        public void HitTestRegular()
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            eventDB.Load(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"));
            eventDB.Validate();
            CourseView courseView = CourseView.CreateCourseView(eventDB, CourseId(4), false);
            DescriptionLine[] description = DescriptionFormatter.CreateDescription(courseView, symbolDB, false);
            DescriptionRenderer descriptionRenderer = new DescriptionRenderer(symbolDB);
            descriptionRenderer.Description = description;
            descriptionRenderer.CellSize = 40;
            descriptionRenderer.Margin = 5;

            descriptionRenderer.DescriptionKind = DescriptionKind.Symbols;

            CheckHitTest(descriptionRenderer, new Point(201,36), HitTestKind.Title, 0, 0, 0, new RectangleF(5, 5, 320, 40));
            CheckHitTest(descriptionRenderer, new Point(79,50), HitTestKind.Header, 1, 1, 0, new RectangleF(5, 45, 120, 40));
            CheckHitTest(descriptionRenderer, new Point(145,80), HitTestKind.Header, 1, 1, 1, new RectangleF(125, 45, 120, 40));
            CheckHitTest(descriptionRenderer, new Point(289, 60), HitTestKind.Header, 1, 1, 2, new RectangleF(245, 45, 80, 40));
            CheckHitTest(descriptionRenderer, new Point(175,216), HitTestKind.NormalBox, 5, 5,4, new RectangleF(165,205, 40, 40));
            CheckHitTest(descriptionRenderer, new Point(285,365), HitTestKind.NormalBox, 9, 9,7, new RectangleF(285, 365, 40, 40));
            CheckHitTest(descriptionRenderer, new Point(81,193), HitTestKind.Directive, 4, 4,0, new RectangleF(5,165, 320, 40));
            CheckHitTest(descriptionRenderer, new Point(328,147), HitTestKind.None, -1, -1, -1, new RectangleF(0, 0, 0, 0));
            CheckHitTest(descriptionRenderer, new Point(255, 427), HitTestKind.OtherTextLine, 10, 10, 0, new RectangleF(5, 405, 320, 40));

            descriptionRenderer.DescriptionKind = DescriptionKind.Text;
            CheckHitTest(descriptionRenderer, new Point(201, 36), HitTestKind.Title, 0, 0, 0, new RectangleF(5, 5, 320, 40));
            CheckHitTest(descriptionRenderer, new Point(79, 50), HitTestKind.Header, 1, 1, 0, new RectangleF(5, 45, 120, 40));
            CheckHitTest(descriptionRenderer, new Point(145, 80), HitTestKind.Header, 1, 1, 1, new RectangleF(125, 45, 120, 40));
            CheckHitTest(descriptionRenderer, new Point(289, 60), HitTestKind.Header, 1, 1, 2, new RectangleF(245, 45, 80, 40));
            CheckHitTest(descriptionRenderer, new Point(175, 216), HitTestKind.NormalText, 5, 5, -1, new RectangleF(85, 205, 240, 40));
            CheckHitTest(descriptionRenderer, new Point(285, 365), HitTestKind.NormalText, 9, 9, -1, new RectangleF(85, 365, 240, 40));
            CheckHitTest(descriptionRenderer, new Point(59, 302), HitTestKind.NormalBox, 7, 7, 1, new RectangleF(45,285, 40, 40));
            CheckHitTest(descriptionRenderer, new Point(81, 193), HitTestKind.DirectiveText, 4, 4, -1, new RectangleF(5, 165, 320, 40));
            CheckHitTest(descriptionRenderer, new Point(328, 147), HitTestKind.None, -1, -1, -1, new RectangleF(0, 0, 0, 0));
            CheckHitTest(descriptionRenderer, new Point(255, 427), HitTestKind.OtherTextLine, 10, 10, 0, new RectangleF(5, 405, 320, 40));


            descriptionRenderer.DescriptionKind = DescriptionKind.SymbolsAndText;
            CheckHitTest(descriptionRenderer, new Point(201, 36), HitTestKind.Title, 0, 0, 0, new RectangleF(5, 5, 520, 40));
            CheckHitTest(descriptionRenderer, new Point(79, 50), HitTestKind.Header, 1, 1, 0, new RectangleF(5, 45, 120, 40));
            CheckHitTest(descriptionRenderer, new Point(145, 80), HitTestKind.Header, 1, 1, 1, new RectangleF(125, 45, 120, 40));
            CheckHitTest(descriptionRenderer, new Point(289, 60), HitTestKind.Header, 1, 1, 2, new RectangleF(245, 45, 80, 40));
            CheckHitTest(descriptionRenderer, new Point(175, 216), HitTestKind.NormalBox, 5, 5, 4, new RectangleF(165, 205, 40, 40));
            CheckHitTest(descriptionRenderer, new Point(285, 365), HitTestKind.NormalBox, 9, 9, 7, new RectangleF(285, 365, 40, 40));
            CheckHitTest(descriptionRenderer, new Point(81, 193), HitTestKind.Directive, 4, 4, 0, new RectangleF(5, 165, 320, 40));
            CheckHitTest(descriptionRenderer, new Point(431, 56), HitTestKind.None, 1, 1, -1, new RectangleF(325, 45, 200, 40));
            CheckHitTest(descriptionRenderer, new Point(333, 131), HitTestKind.NormalText, 3, 3, -1, new RectangleF(325, 125, 200, 40));
            CheckHitTest(descriptionRenderer, new Point(491,252), HitTestKind.DirectiveText, 6, 6, -1, new RectangleF(325, 245, 200, 40));
            CheckHitTest(descriptionRenderer, new Point(527,433), HitTestKind.None, -1, -1, -1, new RectangleF(0, 0, 0, 0));
            CheckHitTest(descriptionRenderer, new Point(255, 427), HitTestKind.OtherTextLine, 10, 10, 0, new RectangleF(5, 405, 520, 40));


        }

        [TestMethod]
        public void HitTestScore()
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            eventDB.Load(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"));
            eventDB.Validate();
            CourseView courseView = CourseView.CreateCourseView(eventDB, CourseId(5), false);
            DescriptionLine[] description = DescriptionFormatter.CreateDescription(courseView, symbolDB, false);
            DescriptionRenderer descriptionRenderer = new DescriptionRenderer(symbolDB);
            descriptionRenderer.Description = description;
            descriptionRenderer.CellSize = 40;
            descriptionRenderer.Margin = 5;

            descriptionRenderer.DescriptionKind = DescriptionKind.Symbols;

            CheckHitTest(descriptionRenderer, new Point(143, 22), HitTestKind.Title, 0, 0, 0, new RectangleF(5, 5, 320, 40));
            CheckHitTest(descriptionRenderer, new Point(178,51), HitTestKind.SecondaryTitle, 1, 1, 0, new RectangleF(5, 45, 320, 40));
            CheckHitTest(descriptionRenderer, new Point(116, 93), HitTestKind.Header, 2, 2, 0, new RectangleF(5, 85, 120, 40));
            CheckHitTest(descriptionRenderer, new Point(259, 118), HitTestKind.Header, 2, 2, 1, new RectangleF(125, 85, 200, 40));
            CheckHitTest(descriptionRenderer, new Point(38, 137), HitTestKind.NormalBox, 3, 3, 0, new RectangleF(5, 125, 40, 40));
            CheckHitTest(descriptionRenderer, new Point(226, 260), HitTestKind.NormalBox, 6, 6, 5, new RectangleF(205, 245, 40, 40));
            CheckHitTest(descriptionRenderer, new Point(3, 184), HitTestKind.None, -1, -1, -1, new RectangleF(0, 0, 0, 0));
            CheckHitTest(descriptionRenderer, new Point(228,534), HitTestKind.OtherTextLine, 13, 13,0, new RectangleF(5, 525, 320, 40));

            descriptionRenderer.DescriptionKind = DescriptionKind.Text;

            CheckHitTest(descriptionRenderer, new Point(311, 12), HitTestKind.Title, 0, 0, 0, new RectangleF(5, 5, 320, 40));
            CheckHitTest(descriptionRenderer, new Point(178, 51), HitTestKind.SecondaryTitle, 1, 1, 0, new RectangleF(5, 45, 320, 40));
            CheckHitTest(descriptionRenderer, new Point(16, 112), HitTestKind.Header, 2, 2, 0, new RectangleF(5, 85, 120, 40));
            CheckHitTest(descriptionRenderer, new Point(178, 116), HitTestKind.Header, 2, 2, 1, new RectangleF(125, 85, 200, 40));
            CheckHitTest(descriptionRenderer, new Point(38, 137), HitTestKind.NormalBox, 3, 3, 0, new RectangleF(5, 125, 40, 40));
            CheckHitTest(descriptionRenderer, new Point(182, 234), HitTestKind.NormalText, 5, 5, -1, new RectangleF(85, 205, 240, 40));
            CheckHitTest(descriptionRenderer, new Point(3, 184), HitTestKind.None, -1, -1, -1, new RectangleF(0, 0, 0, 0));
            CheckHitTest(descriptionRenderer, new Point(228, 534), HitTestKind.OtherTextLine, 13, 13, 0, new RectangleF(5, 525, 320, 40));

            descriptionRenderer.DescriptionKind = DescriptionKind.SymbolsAndText;

            CheckHitTest(descriptionRenderer, new Point(143, 22), HitTestKind.Title, 0, 0, 0, new RectangleF(5, 5, 520, 40));
            CheckHitTest(descriptionRenderer, new Point(178, 51), HitTestKind.SecondaryTitle, 1, 1, 0, new RectangleF(5, 45, 520, 40));
            CheckHitTest(descriptionRenderer, new Point(434, 25), HitTestKind.Title, 0, 0, 0, new RectangleF(5, 5, 520, 40));
            CheckHitTest(descriptionRenderer, new Point(116, 93), HitTestKind.Header, 2, 2, 0, new RectangleF(5, 85, 120, 40));
            CheckHitTest(descriptionRenderer, new Point(259, 118), HitTestKind.Header, 2, 2, 1, new RectangleF(125, 85, 200, 40));
            CheckHitTest(descriptionRenderer, new Point(380, 116), HitTestKind.None, 2, 2, -1, new RectangleF(325, 85, 200, 40));
            CheckHitTest(descriptionRenderer, new Point(38, 137), HitTestKind.NormalBox, 3, 3, 0, new RectangleF(5, 125, 40, 40));
            CheckHitTest(descriptionRenderer, new Point(226, 260), HitTestKind.NormalBox, 6, 6, 5, new RectangleF(205, 245, 40, 40));
            CheckHitTest(descriptionRenderer, new Point(398, 594), HitTestKind.NormalText, 14, 14, -1, new RectangleF(325, 565, 200, 40));
            CheckHitTest(descriptionRenderer, new Point(3, 184), HitTestKind.None, -1, -1, -1, new RectangleF(0, 0, 0, 0));
            CheckHitTest(descriptionRenderer, new Point(451, 534), HitTestKind.OtherTextLine, 13, 13, 0, new RectangleF(5, 525, 520, 40));
        }


        [TestMethod]
        public void HitTestMultiLine()
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            eventDB.Load(TestUtil.GetTestFile("descriptions\\sampleevent6.coursescribe"));
            eventDB.Validate();
            CourseView courseView = CourseView.CreateCourseView(eventDB, CourseId(1), false);
            DescriptionLine[] description = DescriptionFormatter.CreateDescription(courseView, symbolDB, false);
            DescriptionRenderer descriptionRenderer = new DescriptionRenderer(symbolDB);
            descriptionRenderer.Description = description;
            descriptionRenderer.CellSize = 40;
            descriptionRenderer.Margin = 5;

            descriptionRenderer.DescriptionKind = DescriptionKind.Symbols;

            CheckHitTest(descriptionRenderer, new Point(143, 22), HitTestKind.Title, 0, 1, 0, new RectangleF(5, 5, 320, 80));
            CheckHitTest(descriptionRenderer, new Point(13, 51), HitTestKind.Title, 0, 1, 0, new RectangleF(5, 5, 320, 80));
            CheckHitTest(descriptionRenderer, new Point(178, 101), HitTestKind.SecondaryTitle, 2, 4, 0, new RectangleF(5, 85, 320, 120));
            CheckHitTest(descriptionRenderer, new Point(178, 141), HitTestKind.SecondaryTitle, 2, 4, 0, new RectangleF(5, 85, 320, 120));
            CheckHitTest(descriptionRenderer, new Point(178, 181), HitTestKind.SecondaryTitle, 2, 4, 0, new RectangleF(5, 85, 320, 120));
            CheckHitTest(descriptionRenderer, new Point(116, 213), HitTestKind.Header, 5, 5, 0, new RectangleF(5, 205, 120, 40));

            descriptionRenderer.DescriptionKind = DescriptionKind.Text;
            
            CheckHitTest(descriptionRenderer, new Point(143, 22), HitTestKind.Title, 0, 1, 0, new RectangleF(5, 5, 320, 80));
            CheckHitTest(descriptionRenderer, new Point(13, 51), HitTestKind.Title, 0, 1, 0, new RectangleF(5, 5, 320, 80));
            CheckHitTest(descriptionRenderer, new Point(178, 101), HitTestKind.SecondaryTitle, 2, 4, 0, new RectangleF(5, 85, 320, 120));
            CheckHitTest(descriptionRenderer, new Point(178, 141), HitTestKind.SecondaryTitle, 2, 4, 0, new RectangleF(5, 85, 320, 120));
            CheckHitTest(descriptionRenderer, new Point(178, 181), HitTestKind.SecondaryTitle, 2, 4, 0, new RectangleF(5, 85, 320, 120));
            CheckHitTest(descriptionRenderer, new Point(116, 213), HitTestKind.Header, 5, 5, 0, new RectangleF(5, 205, 120, 40));

            descriptionRenderer.DescriptionKind = DescriptionKind.SymbolsAndText;

            CheckHitTest(descriptionRenderer, new Point(143, 22), HitTestKind.Title, 0, 1, 0, new RectangleF(5, 5, 520, 80));
            CheckHitTest(descriptionRenderer, new Point(13, 51), HitTestKind.Title, 0, 1, 0, new RectangleF(5, 5, 520, 80));
            CheckHitTest(descriptionRenderer, new Point(178, 101), HitTestKind.SecondaryTitle, 2, 4, 0, new RectangleF(5, 85, 520, 120));
            CheckHitTest(descriptionRenderer, new Point(178, 141), HitTestKind.SecondaryTitle, 2, 4, 0, new RectangleF(5, 85, 520, 120));
            CheckHitTest(descriptionRenderer, new Point(178, 181), HitTestKind.SecondaryTitle, 2, 4, 0, new RectangleF(5, 85, 520, 120));
            CheckHitTest(descriptionRenderer, new Point(116, 213), HitTestKind.Header, 5, 5, 0, new RectangleF(5, 205, 120, 40));
        }

    }
}

#endif //TEST
