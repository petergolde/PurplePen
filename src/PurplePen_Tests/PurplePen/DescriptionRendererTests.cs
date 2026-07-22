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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using SkiaSharp;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using TestingUtils;

namespace PurplePen.Tests
{
    [TestClass, DoNotParallelize]
    public class DescriptionRendererTests: TestFixtureBase
    {
        // Render the given course id (0 = all controls) and kind to a bitmap, and compare it to the saved version.
        internal void CheckRenderBitmap(string filename, Id<Course> id, DescriptionKind kind, int numColumns = 1, string standard = "2004")
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"), standard);
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            CourseView courseView;

            eventDB.Load(filename);
            eventDB.Validate();

            courseView = CourseView.CreateViewingCourseView(eventDB, DesignatorFromCourseId(eventDB, id));

            DescriptionFormatter descFormatter = new DescriptionFormatter(courseView, symbolDB, DescriptionFormatter.Purpose.ForPrinting);
            DescriptionLine[] description = descFormatter.CreateDescription(kind == DescriptionKind.Symbols);

            Bitmap bmNew = RenderToBitmap(symbolDB, description, kind, numColumns);
            if (numColumns > 1)
                BitmapTestUtil.CheckBitmapsBase(bmNew, GetBitmapFileName(eventDB, id, "_" + numColumns + "col", kind));
            else
                BitmapTestUtil.CheckBitmapsBase(bmNew, GetBitmapFileName(eventDB, id, "", kind));
        }

        // Render a description to a bitmap for testing purposes. Hardcoded 40 pixel box size.
        public static Bitmap RenderToBitmap(SymbolDB symbolDB, DescriptionLine[] description, DescriptionKind kind, int numColumns)
        {
            DescriptionRenderer descriptionRenderer = new DescriptionRenderer(symbolDB);
            descriptionRenderer.Description = description;
            descriptionRenderer.DescriptionKind = kind;
            descriptionRenderer.CellSize = 40;
            descriptionRenderer.Margin = 4;
            descriptionRenderer.NumberOfColumns = numColumns;

            SizeF size = descriptionRenderer.Measure();

            int bmWidth = (int)size.Width;
            int bmHeight = (int)size.Height;
            Bitmap bm = new Bitmap(bmWidth, bmHeight);
            BitmapData bitmapData = bm.LockBits(new Rectangle(0, 0, bmWidth, bmHeight), ImageLockMode.ReadWrite, bm.PixelFormat);
            try {
                SKImageInfo imageInfo = new SKImageInfo(bmWidth, bmHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
                using (SKSurface surface = SKSurface.Create(imageInfo, bitmapData.Scan0, bitmapData.Stride)) {
                    SKCanvas canvas = surface.Canvas;
                    canvas.Clear(SKColors.White);

                    using (Skia_GraphicsTarget grTarget = new Skia_GraphicsTarget(canvas)) {
                        grTarget.PushAntiAliasing(true);
                        descriptionRenderer.RenderToGraphics(grTarget, new RectangleF(0, 0, bmWidth, bmHeight));
                    }
                }
            }
            finally {
                bm.UnlockBits(bitmapData);
            }

            return bm;
        }

        // Get the file name for a bitmap description for testing purposes. CourseID == 0 means all controls. Extra
        // is an extra string to suffix to the base name. Does not end in .png unless specified in extra.
        public static string GetBitmapFileName(EventDB eventDB, Id<Course> courseId, string extra, DescriptionKind kind)
        {
            Course course = null;
            string name;

            if (courseId.IsNotNone)
                course = eventDB.GetCourse(courseId);


            if (course != null)
                name = course.name;
            else
                name = "Allcontrols";

            name = "descriptions\\" + name + "_" + kind.ToString() + extra;

            return name;
        }

        public CourseDesignator DesignatorFromCourseId(EventDB eventDB, Id<Course> courseId)
        {
            CourseDesignator designator;
            if (QueryEvent.HasVariations(eventDB, courseId)) {
                var variationInfo = QueryEvent.GetAllVariations(eventDB, courseId).First();
                designator = new CourseDesignator(courseId, variationInfo);
            }
            else {
                designator = new CourseDesignator(courseId);
            }
            return designator;
        }

        [TestMethod]
        public void AllControlsSymbols()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(0), DescriptionKind.Symbols);
        }

        [TestMethod]
        public void AllControlsSymbols2Col()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(0), DescriptionKind.Symbols, 2);
        }

        [TestMethod]
        public void AllControlsSymbols3Col()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(0), DescriptionKind.Symbols, 3);
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
        public void ScoreSymbolsColumnB() {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent4.coursescribe"), CourseId(7), DescriptionKind.Symbols);
        }

        [TestMethod]
        public void ScoreSymbolsColumnH() {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent5.coursescribe"), CourseId(7), DescriptionKind.Symbols);
        }

        [TestMethod]
        public void ScoreSymbolsNoColumn() {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent8.coursescribe"), CourseId(7), DescriptionKind.Symbols);
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
        public void MultiLineTextLines()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\desctextmultiline.ppen"), CourseId(6), DescriptionKind.Symbols);
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\desctextmultiline.ppen"), CourseId(6), DescriptionKind.SymbolsAndText);
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\desctextmultiline.ppen"), CourseId(6), DescriptionKind.Text);
        }

        [TestMethod] 
        public void MultiLineTitle()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent6.coursescribe"), CourseId(1), DescriptionKind.Symbols);
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent6.coursescribe"), CourseId(1), DescriptionKind.SymbolsAndText);
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent6.coursescribe"), CourseId(1), DescriptionKind.Text);
        }

        [TestMethod]
        public void Exchanges()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent8.ppen"), CourseId(3), DescriptionKind.Symbols, 1, "2018");
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent8.ppen"), CourseId(3), DescriptionKind.SymbolsAndText, 1, "2018");
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent8.ppen"), CourseId(3), DescriptionKind.Text, 1, "2018");
        }

        [TestMethod]
        public void LongDescriptions()
        {
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent7.ppen"), CourseId(3), DescriptionKind.SymbolsAndText);
            CheckRenderBitmap(TestUtil.GetTestFile("descriptions\\sampleevent7.ppen"), CourseId(3), DescriptionKind.Text);
        }
	

        // Render a description to a map, then to a bitmap for testing purposes. Hardcoded 6 mm box size.
        internal static Bitmap RenderToMapThenToBitmap(SymbolDB symbolDB, DescriptionLine[] description, DescriptionKind kind, int numColumns)
        {
            DescriptionRenderer descriptionRenderer = new DescriptionRenderer(symbolDB);
            descriptionRenderer.Description = description;
            descriptionRenderer.DescriptionKind = kind;
            descriptionRenderer.CellSize = 6.0F;
            descriptionRenderer.Margin = 0.7F;
            descriptionRenderer.NumberOfColumns = numColumns;
            PointF location = new PointF(30, -100);

            SizeF size = descriptionRenderer.Measure();

            int bmWidth = (int)size.Width * 8, bmHeight = (int)size.Height * 8;

            Map map = new Map(new Skia_TextMetrics(), null);
            using (map.Write()) {
                Dictionary<object, SymDef> dict = new Dictionary<object, SymDef>();

                // Create white color and white-out symdef.
                SymColor white = map.AddColorBottom("White", 44, 0, 0, 0, 0, false);
                AreaSymDef whiteArea = new AreaSymDef("White out", "890", white, null);
                whiteArea.ToolboxImage = CoreMapUtil.CreateToolboxIcon(ImageResources.WhiteOut_OcadToolbox);
                map.AddSymdef(whiteArea);
                dict[CourseLayout.KeyWhiteOut] = whiteArea;

                SymColor color = map.AddColor("Purple", 11, 0.045F, 0.59F, 0, 0.255F, false);
                descriptionRenderer.RenderToMap(map, color, location, dict);
            }

            InputOutput.WriteFile(TestUtil.GetTestFile("descriptions\\desc_temp.ocd"), map, new MapFileFormat(MapFileFormatKind.OCAD, 8));

            RenderOptions renderOpts = new RenderOptions();
            renderOpts.usePatternBitmaps = true;
            renderOpts.minResolution = 0.1F;
            renderOpts.renderTemplates = RenderTemplateOption.MapAndTemplates;

            RectangleF mapRectangle = new RectangleF(location.X, location.Y - size.Height, size.Width, size.Height);
            return TestRenderingUtils.RenderToBitmap(bmWidth, bmHeight, mapRectangle, true, graphicsTarget => {
                graphicsTarget.PushAntiAliasing(true);
                using (map.Read())
                    map.Draw(graphicsTarget, mapRectangle, renderOpts, null);
            });
        }

        // Render the given course id (0 = all controls) and kind to a map, and compare it to the saved version.
        internal void CheckRenderMap(string filename, Id<Course> id, DescriptionKind kind, int numColumns = 1, string standard = "2004")
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"), standard);
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            CourseView courseView;

            eventDB.Load(filename);
            eventDB.Validate();
            symbolDB.Standard = eventDB.GetEvent().descriptionStandard;

            courseView = CourseView.CreateViewingCourseView(eventDB, DesignatorFromCourseId(eventDB, id));

            DescriptionFormatter descFormatter = new DescriptionFormatter(courseView, symbolDB, DescriptionFormatter.Purpose.ForPrinting);
            DescriptionLine[] description = descFormatter.CreateDescription(kind == DescriptionKind.Symbols);

            Bitmap bmNew = RenderToMapThenToBitmap(symbolDB, description, kind, numColumns);
            if (numColumns > 1)
                BitmapTestUtil.CheckBitmapsBase(bmNew, GetBitmapFileName(eventDB, id, "_ocad_" + numColumns + "col", kind));
            else
                BitmapTestUtil.CheckBitmapsBase(bmNew, GetBitmapFileName(eventDB, id, "_ocad", kind));
        }

        // Render the given course id (0 = all controls) and kind to a map, and compare it to the saved version.
        internal void CheckRenderMapStandardChange(string filename, Id<Course> id, DescriptionKind kind, string newDescStandard)
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            CourseView courseView;

            eventDB.Load(filename);
            symbolDB.Standard = eventDB.GetEvent().descriptionStandard;
            eventDB.Validate();

            courseView = CourseView.CreateViewingCourseView(eventDB, DesignatorFromCourseId(eventDB, id));

            DescriptionFormatter descFormatter = new DescriptionFormatter(courseView, symbolDB, DescriptionFormatter.Purpose.ForPrinting);
            DescriptionLine[] description = descFormatter.CreateDescription(kind == DescriptionKind.Symbols);

            Bitmap bmNew = RenderToMapThenToBitmap(symbolDB, description, kind, 1);
            BitmapTestUtil.CheckBitmapsBase(bmNew, GetBitmapFileName(eventDB, id, "_std_default", kind));

            undomgr.BeginCommand(71231, "change standard");
            symbolDB.Standard = newDescStandard;
            ChangeEvent.UpdateDescriptionToMatchStandard(eventDB, symbolDB);
            undomgr.EndCommand(71231);
            description = descFormatter.CreateDescription(kind == DescriptionKind.Symbols);

            bmNew = RenderToMapThenToBitmap(symbolDB, description, kind, 1);
            BitmapTestUtil.CheckBitmapsBase(bmNew, GetBitmapFileName(eventDB, id, "_std_" + newDescStandard, kind));
        }

        [TestMethod]
        public void AllControlsSymbolsToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(0), DescriptionKind.Symbols);
        }

        [TestMethod]
        public void AllControlsSymbolsToMap2Col()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(0), DescriptionKind.Symbols, 2);
        }

        [TestMethod]
        public void AllControlsSymbolsToMap3Col()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(0), DescriptionKind.Symbols, 3);
        }

        [TestMethod]
        public void AllControlsSymbolsToMap4Col()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(0), DescriptionKind.Symbols, 4);
        }

        [TestMethod]
        public void AllControlsTextToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(0), DescriptionKind.Text);
        }

        [TestMethod]
        public void AllControlsTextToMap3Col()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(0), DescriptionKind.Text, 3);
        }

        [TestMethod]
        public void AllControlsSymbolsAndTextToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(0), DescriptionKind.SymbolsAndText);
        }

        [TestMethod]
        public void AllControlsSymbolsAndTextToMap3Col()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(0), DescriptionKind.SymbolsAndText, 3);
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
        public void ScoreSymbolsToMap2Col()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(5), DescriptionKind.Symbols, 2);
        }

        [TestMethod]
        public void ScoreSymbolsToMap3Col()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(5), DescriptionKind.Symbols, 3);
        }

        [TestMethod]
        public void ScoreSymbolsToMap5Col()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent1.coursescribe"), CourseId(5), DescriptionKind.Symbols, 5);
        }

        [TestMethod]
        public void ScoreSymbolsColumnBToMap() {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent4.coursescribe"), CourseId(7), DescriptionKind.Symbols);
        }

        [TestMethod]
        public void ScoreSymbolsColumnHToMap() {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent5.coursescribe"), CourseId(7), DescriptionKind.Symbols);
        }

        [TestMethod]
        public void ScoreSymbolsNoColumnToMap() {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent8.coursescribe"), CourseId(7), DescriptionKind.Symbols);
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
        public void MultiLineTextLinesToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\desctextmultiline.ppen"), CourseId(6), DescriptionKind.Symbols);
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\desctextmultiline.ppen"), CourseId(6), DescriptionKind.SymbolsAndText);
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\desctextmultiline.ppen"), CourseId(6), DescriptionKind.Text);
        }

        [TestMethod]
        public void MultiLineTitleToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent6.coursescribe"), CourseId(1), DescriptionKind.Symbols);
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent6.coursescribe"), CourseId(1), DescriptionKind.SymbolsAndText);
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent6.coursescribe"), CourseId(1), DescriptionKind.Text);
        }

        [TestMethod]
        public void ExchangesToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent8.ppen"), CourseId(3), DescriptionKind.Symbols, 1, "2018");
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent8.ppen"), CourseId(3), DescriptionKind.SymbolsAndText, 1, "2018");
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent8.ppen"), CourseId(3), DescriptionKind.Text, 1, "2018");
        }



        [TestMethod]
        public void LongDescriptionsToMap()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent7.ppen"), CourseId(3), DescriptionKind.SymbolsAndText);
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\sampleevent7.ppen"), CourseId(3), DescriptionKind.Text);
        }

        [TestMethod]
        public void NewSymbols2018and2024()
        {
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\newsymbols.ppen"), CourseId(1), DescriptionKind.Symbols);
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\newsymbols.ppen"), CourseId(1), DescriptionKind.SymbolsAndText);
            CheckRenderMap(TestUtil.GetTestFile("descriptions\\newsymbols.ppen"), CourseId(1), DescriptionKind.Text);
        }



        [TestMethod]
        public void MultiStandard1()
        {
            CheckRenderMapStandardChange(TestUtil.GetTestFile("descriptions\\standards1.ppen"), CourseId(1), DescriptionKind.SymbolsAndText, "2018");
        }

        [TestMethod]
        public void MultiStandard2()
        {
            CheckRenderMapStandardChange(TestUtil.GetTestFile("descriptions\\standards2.ppen"), CourseId(1), DescriptionKind.SymbolsAndText, "2004");
        }


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
            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, CourseDesignator.AllControls);
            DescriptionFormatter descFormatter = new DescriptionFormatter(courseView, symbolDB, DescriptionFormatter.Purpose.ForPrinting);
            DescriptionLine[] description = descFormatter.CreateDescription(false);
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
            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(4)));
            DescriptionFormatter descFormatter = new DescriptionFormatter(courseView, symbolDB, DescriptionFormatter.Purpose.ForPrinting);
            DescriptionLine[] description = descFormatter.CreateDescription(false);
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
            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(5)));
            DescriptionFormatter descFormatter = new DescriptionFormatter(courseView, symbolDB, DescriptionFormatter.Purpose.ForPrinting);
            DescriptionLine[] description = descFormatter.CreateDescription(false);
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
            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, DesignatorFromCourseId(eventDB, CourseId(1)));
            DescriptionFormatter descFormatter = new DescriptionFormatter(courseView, symbolDB, DescriptionFormatter.Purpose.ForPrinting);
            DescriptionLine[] description = descFormatter.CreateDescription(false);
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
