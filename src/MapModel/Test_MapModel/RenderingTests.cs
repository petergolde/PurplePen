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
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;
using NUnit.Framework;
using TestingUtils;

namespace PurplePen.MapModel.Tests
{
    using PurplePen.Graphics2D;

    [TestFixture]
    public class Rendering 
    {
        private const int MAX_PIXEL_DIFF = 30;

        static Rendering()
        {
            Uri uri = new Uri(typeof(Rendering).Assembly.CodeBase);
            string executablePath = Path.GetDirectoryName(uri.LocalPath);
            string fontPath = Path.Combine(executablePath, "fonts");

            GdiplusFontLoader.AddFontFile("Roboto", FontStyle.Regular, Path.Combine(fontPath, "Roboto-Regular.ttf"));
            GdiplusFontLoader.AddFontFile("Roboto", FontStyle.Bold, Path.Combine(fontPath, "Roboto-Bold.ttf"));
            GdiplusFontLoader.AddFontFile("Roboto", FontStyle.Italic, Path.Combine(fontPath, "Roboto-Italic.ttf"));
            GdiplusFontLoader.AddFontFile("Roboto", FontStyle.Bold | FontStyle.Italic, Path.Combine(fontPath, "Roboto-BoldItalic.ttf"));
            GdiplusFontLoader.AddFontFile("Roboto Condensed", FontStyle.Regular, Path.Combine(fontPath, "RobotoCondensed-Regular.ttf"));
            GdiplusFontLoader.AddFontFile("Roboto Condensed", FontStyle.Bold, Path.Combine(fontPath, "RobotoCondensed-Bold.ttf"));
            GdiplusFontLoader.AddFontFile("Roboto Condensed", FontStyle.Italic, Path.Combine(fontPath, "RobotoCondensed-Italic.ttf"));
            GdiplusFontLoader.AddFontFile("Roboto Condensed", FontStyle.Bold | FontStyle.Italic, Path.Combine(fontPath, "RobotoCondensed-BoldItalic.ttf"));
        }

        // Write a bitmap to a PNG.
        void WriteBitmap(Bitmap bmp, string filename) {
           bmp.Save(filename, ImageFormat.Png);
        }

        void GDIPlus_RenderingTest(int width, RectangleF drawingRectangle, string pngFileName, Action<IGraphicsTarget> draw)
        {
            int height = (int)Math.Ceiling(width * drawingRectangle.Height / drawingRectangle.Width);

            Bitmap bitmapNew;
            using (GDIPlus_BitmapGraphicsTarget grTarget = new GDIPlus_BitmapGraphicsTarget(width, height, false, CmykColor.FromCmyk(0, 0, 0, 0), drawingRectangle, true)) {
                draw(grTarget);
                bitmapNew = grTarget.Bitmap;
            }

            string directoryName = Path.GetDirectoryName(pngFileName);
            string newBitmapName = Path.Combine(directoryName,
                                        Path.GetFileNameWithoutExtension(pngFileName) + "_new.png");
            File.Delete(newBitmapName);

            TestUtil.CompareBitmapBaseline(bitmapNew, pngFileName, MAX_PIXEL_DIFF);
        }

        [Test]
        public void Blending1()
        {
            GDIPlus_RenderingTest(800, new RectangleF(-100, -100, 200, 200), TestUtil.GetTestFile("rendering\\blending1_baseline.png"),
                grTarget => {
                    const int penWidth = 20;
                    CmykColor blue = CmykColor.FromColor(Color.Blue);
                    CmykColor green = CmykColor.FromColor(Color.Green);
                    CmykColor yellow = CmykColor.FromColor(Color.Yellow);
                    CmykColor black = CmykColor.FromColor(Color.Black);
                    CmykColor purple = CmykColor.FromCmyk(0.1F, 0.9F, 0, 0.1F);

                    object penBlue = new object(), penGreen = new object(), penYellow = new object(), penBlack = new object(), penPurple = new object();
                    grTarget.CreatePen(penBlue, blue, penWidth, LineCap.Round, LineJoin.Miter, 5);
                    grTarget.CreatePen(penGreen, green, penWidth, LineCap.Round, LineJoin.Miter, 5);
                    grTarget.CreatePen(penYellow, yellow, penWidth, LineCap.Round, LineJoin.Miter, 5);
                    grTarget.CreatePen(penBlack, black, penWidth, LineCap.Round, LineJoin.Miter, 5);
                    grTarget.CreatePen(penPurple, purple, penWidth, LineCap.Round, LineJoin.Miter, 5);

                    grTarget.DrawLine(penBlue, new PointF(-80, 60), new PointF(80, 60));
                    grTarget.DrawLine(penGreen, new PointF(-80, 30), new PointF(80, 30));
                    grTarget.DrawLine(penYellow, new PointF(-80, 0), new PointF(80, 0));
                    grTarget.DrawLine(penBlack, new PointF(-80, -30), new PointF(80, -30));

                    grTarget.PushBlending(BlendMode.Darken);
                    grTarget.DrawEllipse(penPurple, new PointF(0, 0), 70, 70);
                    grTarget.PopBlending();
                });
        }

        [Test]
        public void Blending2()
        {
            GDIPlus_RenderingTest(797, new RectangleF(-100, -100, 200, 200), TestUtil.GetTestFile("rendering\\blending2_baseline.png"),
                grTarget => {
                    const int penWidth = 20;
                    CmykColor blue = CmykColor.FromColor(Color.Blue);
                    CmykColor green = CmykColor.FromColor(Color.Green);
                    CmykColor yellow = CmykColor.FromColor(Color.Yellow);
                    CmykColor black = CmykColor.FromColor(Color.Black);
                    CmykColor purple = CmykColor.FromCmyk(0.1F, 0.9F, 0, 0.1F);

                    object penBlue = new object(), penGreen = new object(), penYellow = new object(), penBlack = new object(), penPurple = new object();
                    grTarget.CreatePen(penBlue, blue, penWidth, LineCap.Round, LineJoin.Miter, 5);
                    grTarget.CreatePen(penGreen, green, penWidth, LineCap.Round, LineJoin.Miter, 5);
                    grTarget.CreatePen(penYellow, yellow, penWidth, LineCap.Round, LineJoin.Miter, 5);
                    grTarget.CreatePen(penBlack, black, penWidth, LineCap.Round, LineJoin.Miter, 5);
                    grTarget.CreatePen(penPurple, purple, penWidth * 2, LineCap.Round, LineJoin.Miter, 5);

                    grTarget.DrawLine(penBlue, new PointF(-180, 60), new PointF(180, 60));
                    grTarget.DrawLine(penGreen, new PointF(-180, 30), new PointF(180, 30));
                    grTarget.DrawLine(penYellow, new PointF(-180, 0), new PointF(180, 0));
                    grTarget.DrawLine(penBlack, new PointF(-180, -30), new PointF(180, -30));

                    grTarget.PushBlending(BlendMode.Darken);
                    grTarget.DrawEllipse(penPurple, new PointF(0, 0), 110, 110);
                    grTarget.PopBlending();
                });
        }

        [Test]
        public void PatternBrush() {
            GDIPlus_RenderingTest(800, new RectangleF(-103, -117, 200, 200), TestUtil.GetTestFile("rendering\\patternbrush_baseline.png"),
                grTarget => {
                    IBrushTarget brushTarget = grTarget.CreatePatternBrush(new SizeF(30, 20), 0, 60, 60);

                    object pen = new object();
                    brushTarget.CreatePen(pen, CmykColor.FromRgb(1, 0, 0), 1.5F, LineCap.Round, LineJoin.Round, 5F);
                    brushTarget.DrawLine(pen, new PointF(-15, -10), new PointF(3, 10));
                    pen = new object();
                    brushTarget.CreatePen(pen, CmykColor.FromRgb(0, 1, 0), 3.0F, LineCap.Round, LineJoin.Round, 5F);
                    brushTarget.DrawLine(pen, new PointF(3, 10), new PointF(15, -10));

                    pen = new object();
                    brushTarget.CreatePen(pen, CmykColor.FromRgb(0, 0, 1), 3F, LineCap.Round, LineJoin.Round, 5F);
                    brushTarget.DrawEllipse(pen, new PointF(1, -2), 5, 4);

                    object brush = new object();
                    brushTarget.FinishBrush(brush);

                    grTarget.FillPolygon(brush, new PointF[] { new PointF(-50, -60), new PointF(0, 30), new PointF(50, -60), new PointF(-50, 20), new PointF(50, 20) }, FillMode.Winding);

                    pen = new object();
                    grTarget.CreatePen(pen, CmykColor.FromRgb(0, 0, 0), 0.5F, LineCap.Flat, LineJoin.Round, 5F);
                    grTarget.DrawLine(pen, new PointF(-30, -30), new PointF(30, 30));
                    grTarget.DrawLine(pen, new PointF(30, -30), new PointF(-30, 30));
                });
        }

        [Test]
        public void RotatedPatternBrush()
        {
            GDIPlus_RenderingTest(800, new RectangleF(-103, -117, 200, 200), TestUtil.GetTestFile("rendering\\rotatedpatternbrush_baseline.png"),
                grTarget => {
                    IBrushTarget brushTarget = grTarget.CreatePatternBrush(new SizeF(30, 20), 147, 60, 60);

                    object pen = new object();
                    brushTarget.CreatePen(pen, CmykColor.FromRgb(1, 0, 0), 1.5F, LineCap.Round, LineJoin.Round, 5F);
                    brushTarget.DrawLine(pen, new PointF(-15, -10), new PointF(3, 10));
                    pen = new object();
                    brushTarget.CreatePen(pen, CmykColor.FromRgb(0, 1, 0), 3.0F, LineCap.Round, LineJoin.Round, 5F);
                    brushTarget.DrawLine(pen, new PointF(3, 10), new PointF(15, -10));

                    pen = new object();
                    brushTarget.CreatePen(pen, CmykColor.FromRgb(0, 0, 1), 3F, LineCap.Round, LineJoin.Round, 5F);
                    brushTarget.DrawEllipse(pen, new PointF(1, -2), 5, 4);

                    object brush = new object();
                    brushTarget.FinishBrush(brush);

                    grTarget.FillPolygon(brush, new PointF[] { new PointF(-50, -60), new PointF(0, 30), new PointF(50, -60), new PointF(-50, 20), new PointF(50, 20) }, FillMode.Winding);

                    pen = new object();
                    grTarget.CreatePen(pen, CmykColor.FromRgb(0, 0, 0), 0.5F, LineCap.Flat, LineJoin.Round, 5F);
                    grTarget.DrawLine(pen, new PointF(-30, -30), new PointF(30, 30));
                    grTarget.DrawLine(pen, new PointF(30, -30), new PointF(-30, 30));
                });
        }

        [Test]
        public void TextMetrics()
        {
            ITextMetrics textMetrics = new GDIPlus_TextMetrics();

            Assert.IsTrue(textMetrics.TextFaceIsInstalled("Times New Roman"));
            Assert.IsTrue(textMetrics.TextFaceIsInstalled("Trebuchet MS"));
            Assert.IsFalse(textMetrics.TextFaceIsInstalled("Banana"));

            ITextFaceMetrics tnrMetrics = textMetrics.GetTextFaceMetrics("Times New Roman", 25, TextEffects.None);
            Assert.AreEqual(25.0F, tnrMetrics.EmHeight, 0.1F);
            Assert.AreEqual(22.28F, tnrMetrics.Ascent, 0.1F);
            Assert.AreEqual(5.41F, tnrMetrics.Descent, 0.1F);
            Assert.AreEqual(16.93F, tnrMetrics.CapHeight, 0.1F);
            Assert.AreEqual(6.25F, tnrMetrics.SpaceWidth, 0.1F);
            Assert.AreEqual(138.87F, tnrMetrics.GetTextWidth("Hello, world  "), 0.1F);
            Assert.AreEqual(27.69F, tnrMetrics.GetTextSize("Hello, world").Height, 0.1F);


            tnrMetrics = textMetrics.GetTextFaceMetrics("Trebuchet MS", 50, TextEffects.None);
            Assert.AreEqual(50.0F, tnrMetrics.EmHeight, 0.1F);
            Assert.AreEqual(46.95F, tnrMetrics.Ascent, 0.1F);
            Assert.AreEqual(11.11F, tnrMetrics.Descent, 0.1F);
            Assert.AreEqual(36.25F, tnrMetrics.CapHeight, 0.1F);
            Assert.AreEqual(15.06F, tnrMetrics.SpaceWidth, 0.1F);
            Assert.AreEqual(305.93F, tnrMetrics.GetTextWidth("Hello, world  "), 0.1F);
            Assert.AreEqual(58.06F, tnrMetrics.GetTextSize("Hello, world").Height, 0.1F);
        }



        public static Bitmap RenderBitmap(Map map, Size bitmapSize, RectangleF mapArea, RenderOptions renderOpts, float intensity)
        {
            var grTarget = new GDIPlus_BitmapGraphicsTarget(bitmapSize.Width, bitmapSize.Height, false, CmykColor.FromCmyk(0, 0, 0, 0), mapArea, true, null, intensity);
            using (grTarget) {
                renderOpts.renderTemplates = RenderTemplateOption.MapAndTemplates;
                renderOpts.minResolution = mapArea.Width / (float)bitmapSize.Width;
                
                using (map.Read())
                    map.Draw(grTarget, mapArea, renderOpts, null);
                
                return grTarget.Bitmap;
            }

        }

        public static Bitmap RenderAntiAliasBitmap(Map map, Size bitmapSize, RectangleF mapArea, RenderOptions renderOpts, float intensity)
        {
            var grTarget = new GDIPlus_BitmapGraphicsTarget(bitmapSize.Width, bitmapSize.Height, false, CmykColor.FromCmyk(0, 0, 0, 0), mapArea, true, null, intensity);
            grTarget.PushAntiAliasing(true);
            using (grTarget) {
                renderOpts.renderTemplates = RenderTemplateOption.MapAndTemplates;
                renderOpts.minResolution = mapArea.Width / (float)bitmapSize.Width;

                using (map.Read())
                    map.Draw(grTarget, mapArea, renderOpts, null);

                return grTarget.Bitmap;
            }

        }
        // Verifies a test file. Returns true on success, false on failure. In the failure case, 
        // a difference bitmap is written out.
        static bool VerifyTestFile(string filename, RenderOptions renderOptions, bool testLightenedColor, bool roundtripToOcadFile, int minOcadVersion, int maxOcadVersion)
        {

            string pngFileName;
            string mapFileName;
            string geodeFileName;
            string ocadFileName;
            string directoryName;
            RectangleF mapArea;
            Size size;

            // Read the test file, and get the other file names and the area.
            using (StreamReader reader = new StreamReader(filename)) {
                mapFileName = reader.ReadLine();
                pngFileName = reader.ReadLine();
                float left, right, top, bottom;
                string area = reader.ReadLine();
                string[] coords = area.Split(',');
                left = float.Parse(coords[0]); bottom = float.Parse(coords[1]); right = float.Parse(coords[2]); top = float.Parse(coords[3]);
                mapArea = new RectangleF(left, top, right - left, bottom - top);
                string sizeLine = reader.ReadLine();
                coords = sizeLine.Split(',');
                size = new Size(int.Parse(coords[0]), int.Parse(coords[1]));
            }

            // Convert to absolute paths.
            directoryName = Path.GetDirectoryName(filename);
            mapFileName = Path.Combine(directoryName, mapFileName);
            pngFileName = Path.Combine(directoryName, pngFileName);
            geodeFileName = Path.Combine(directoryName,
                                         Path.GetFileNameWithoutExtension(mapFileName) + "_temp.geode");
            ocadFileName = Path.Combine(directoryName,
                                         Path.GetFileNameWithoutExtension(mapFileName) + "_temp.ocd");

            File.Delete(geodeFileName);
            File.Delete(ocadFileName);

            // Create and open the map file.
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(directoryName));
            InputOutput.ReadFile(mapFileName, map);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Draw into a new bitmap.
            Bitmap bitmapNew = RenderBitmap(map, size, mapArea, renderOptions, 1.0F);

            sw.Stop();
            Console.WriteLine("Rendered bitmap '{0}' to output '{4}' rect={1} size={2} in {3} ms", mapFileName, mapArea, size, sw.ElapsedMilliseconds, pngFileName);

            TestUtil.CompareBitmapBaseline(bitmapNew, pngFileName, MAX_PIXEL_DIFF);

            if (testLightenedColor) {
                string lightenedPngFileName = Path.Combine(Path.GetDirectoryName(pngFileName), Path.GetFileNameWithoutExtension(pngFileName) + "_light.png");
                Bitmap bitmapLight = RenderBitmap(map, size, mapArea, renderOptions, 0.4F);
                TestUtil.CompareBitmapBaseline(bitmapLight, lightenedPngFileName, MAX_PIXEL_DIFF);
            }

            if (roundtripToOcadFile) {
                for (int version = minOcadVersion; version <= maxOcadVersion; ++version) {
                    if (version == 13)
                        version = 2018;
                      
                    // Save and load to a temp file name.
                    InputOutput.WriteFile(ocadFileName, map, new MapFileFormat(MapFileFormatKind.OCAD, version));

                    // Create and open the map file.
                    map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("rendering")));
                    InputOutput.ReadFile(ocadFileName, map);

                    // Draw into a new bitmap.
                    bitmapNew = RenderBitmap(map, size, mapArea, renderOptions, 1.0F);

                    TestUtil.CompareBitmapBaseline(bitmapNew, pngFileName, MAX_PIXEL_DIFF);

                    File.Delete(ocadFileName);
                }
            }

            return true;
        }

        void TimeMapRender(Map map, Size size, RectangleF mapArea, string name) {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Draw into a new bitmap.
            RenderOptions renderOpts = new RenderOptions();
            renderOpts.usePatternBitmaps = true;
            renderOpts.blendOverprintedColors = false;

            Bitmap bitmapNew = RenderAntiAliasBitmap(map, size, mapArea, renderOpts, 1.0F);

            sw.Stop();
            Console.WriteLine("Rendered bitmap '{0}' in {1} ms", name, sw.ElapsedMilliseconds);
        }


        void CheckTest(string filename, bool testLightenedColor, bool roundtripToOcad, int minOcadVersion, int maxOcadVersion)
        {
            RenderOptions renderOpts = new RenderOptions();
            renderOpts.usePatternBitmaps = true;
            renderOpts.blendOverprintedColors = false;
            string fullname = TestUtil.GetTestFile("rendering\\" + filename);
            bool ok = VerifyTestFile(fullname, renderOpts, testLightenedColor, roundtripToOcad, minOcadVersion, maxOcadVersion);
            Assert.IsTrue(ok, string.Format("Rendering test {0} did not compare correctly.", filename), ok);
        }

        void CheckTestNoPatternBitmaps(string filename, bool testLightenedColor, bool roundtripToOcad, int minOcadVersion, int maxOcadVersion) {
            RenderOptions renderOpts = new RenderOptions();
            renderOpts.usePatternBitmaps = false;
            renderOpts.blendOverprintedColors = false;
            string fullname = TestUtil.GetTestFile("rendering\\" + filename);
            bool ok = VerifyTestFile(fullname, renderOpts, testLightenedColor, roundtripToOcad, minOcadVersion, maxOcadVersion);
            Assert.IsTrue(ok, string.Format("Rendering test {0} did not compare correctly.", filename), ok);
        }

        void CheckTestOverprinting(string filename, bool testLightenedColor, bool roundtripToOcad, int minOcadVersion, int maxOcadVersion)
        {
            RenderOptions renderOpts = new RenderOptions();
            renderOpts.usePatternBitmaps = false;
            renderOpts.blendOverprintedColors = true;
            string fullname = TestUtil.GetTestFile("rendering\\" + filename);
            bool ok = VerifyTestFile(fullname, renderOpts, testLightenedColor, roundtripToOcad, minOcadVersion, maxOcadVersion);
            Assert.IsTrue(ok, string.Format("Rendering test {0} did not compare correctly.", filename), ok);
        }

        void CheckTestLayers(string filename, int? startLayer, int? stopLayer, bool testLightenedColor, bool roundtripToOcad, int minOcadVersion, int maxOcadVersion)
        {
            RenderOptions renderOpts = new RenderOptions();
            renderOpts.usePatternBitmaps = true;
            renderOpts.blendOverprintedColors = false;
            renderOpts.colorBeginDrawExclusive = startLayer;
            renderOpts.colorEndDrawInclusive = stopLayer;

            string fullname = TestUtil.GetTestFile("rendering\\" + filename);
            bool ok = VerifyTestFile(fullname, renderOpts, testLightenedColor, roundtripToOcad, minOcadVersion, maxOcadVersion);
            Assert.IsTrue(ok, string.Format("Rendering test {0} did not compare correctly.", filename), ok);
        }


        [Test]
        public void TimeTeanWest()
        {
            // Create and open the map file.
            Map map = new Map(new GDIPlus_TextMetrics(), null);
            InputOutput.ReadFile(TestUtil.GetTestFile(@"rendering\teanwest.ocd"), map);

            TimeMapRender(map, new Size(1814, 1022), new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
            TimeMapRender(map, new Size(1814, 1022), new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
            TimeMapRender(map, new Size(1814, 1022), new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
            TimeMapRender(map, new Size(1814, 1022), new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
            TimeMapRender(map, new Size(1814, 1022), new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
        }

        [Test]
        public void TimeSalmon()
        {
            // Create and open the map file.
            Map map = new Map(new GDIPlus_TextMetrics(), null);
            InputOutput.ReadFile(TestUtil.GetTestFile(@"rendering\SalmonLaSac.ocd"), map);

            TimeMapRender(map, new Size(1462, 1564), RectangleF.FromLTRB(-115.4F, -93.3F, 114.6F, 152.75F), "Salmon La Sac");
            TimeMapRender(map, new Size(1462, 1564), RectangleF.FromLTRB(-115.4F, -93.3F, 114.6F, 152.75F), "Salmon La Sac");
            TimeMapRender(map, new Size(1462, 1564), RectangleF.FromLTRB(-115.4F, -93.3F, 114.6F, 152.75F), "Salmon La Sac");
        }


        [Test]
        public void TestWest() {
            CheckTest("teanwest.txt", false, true, 9, 2018);
            CheckTest("teanwest2018.txt", false, true, 2018, 2018);
        }

        [Test]
        public void TestWest11()
        {
            CheckTest("teanwest11.txt", false, true, 11, 12);
        }

        [Test]
        public void LineSymbols()
        {
            CheckTest("isomlines.txt", true, true, 6, 12);
            CheckTest("isomlines9.txt", true, true, 6, 12);
            CheckTest("isomlines2018.txt", true, true, 2018, 2018);
        }

        [Test]
        public void Fences()
        {
            CheckTest("fences.txt", false, true, 6, 12);
            CheckTest("fences9.txt", false, true, 6, 12);
        }

        [Test]
        public void ShortFences()
        {
            CheckTest("shortfences.txt", false, false, 9, 9);
        }

        [Test]
        public void FramingLines()
        {
            CheckTest("framingline-test.txt", false, true, 6, 12);
            CheckTest("framingline-test9.txt", false, true, 6, 12);
        }

        [Test]
        public void DashLines()
        {
            CheckTest("dashline.txt", false, true, 6, 12);
            CheckTest("dashline9.txt", false, true, 6, 12);
        }

        [Test]
        public void PointSymbols()
        {
            CheckTest("isompoints.txt", true, true, 6, 12);
            CheckTest("isompoints_9.txt", false, true, 6, 12);
            CheckTest("isompoints_2018.txt", false, true, 2018, 2018);
        }

        [Test]
        public void AreaSymbols()
        {
            CheckTest("isomarea.txt", true, true, 6, 12);
            CheckTest("isomarea9.txt", true, true, 6, 12);
            CheckTest("isomarea2018.txt", true, true, 2018, 2018);
        }

        [Test]
        public void AreaHoles()
        {
            CheckTest("holes.txt", false, true, 6, 12);
            CheckTest("holes9.txt", false, true, 6, 12);
        }

        [Test]
        public void CutCircles()
        {
            CheckTest("cutcircles.txt", false, true, 6, 12);
            // CheckTest("cutcircles9.txt", false, false, 6);    OCAD 9 has some strange problems with cut circles...
        }

        [Test]
        public void HiddenSymbols()
        {
            CheckTest("hiddensymbols.txt", false, true, 6, 12);
            CheckTest("hiddensymbols9.txt", false, true, 6, 12);
        }
    

        [Test]
        public void RotatedAreas()
        {
            CheckTest("rotarea-test.txt", false, true, 6, 12);
            CheckTest("rotarea-test9.txt", false, true, 6, 12);
        }

        [Test]
        public void BorderedAreas()
        {
            CheckTest("borderedarea9.txt", false, true, 9, 12);
        }
    

        [Test]
        public void TextSymbols()
        {
            CheckTest("simpletext.txt", true, true, 6, 12);
            CheckTest("simpletext9.txt", true, true, 6, 12);
        }

        [Test]
        public void PunchBox()
        {
            //CheckTest("punchbox.txt", false, true, 6, 12);
            CheckTest("punchbox9.txt", false, true, 6, 12);
            CheckTest("punchbox11.txt", false, true, 10, 12);
        }

        [Test]
        public void LakeSammMap()
        {
            CheckTest("lksamm1.txt", false, true, 6, 12);
            // CheckTest("lksamm2.txt", true, true, 6);   // this one has very slight rendering differences each time. odd...
            CheckTest("lksamm3.txt", false, true, 6, 12);
            CheckTest("lksamm4.txt", false, true, 6, 12);
        }

        [Test]
        public void LakeSammMap9()
        {
            CheckTest("lksamm9_1.txt", false, false, 6, 12);
            CheckTest("lksamm9_1.txt", false, false, 6, 12);
            // CheckTest("lksamm9_2.txt", true, false, 6);  // this one has very slight rendering differences each time. odd...
            CheckTest("lksamm9_3.txt", false, false, 6, 12);
            CheckTest("lksamm9_4.txt", false, false, 6, 12);
        }

        [Test]
        public void LakeSammMap11()
        {
            CheckTest("lksamm11_1.txt", false, false, 6, 12);
            CheckTest("lksamm11_1.txt", false, false, 6, 12);
            // CheckTest("lksamm11_2.txt", true, false, 6);  // this one has very slight rendering differences each time. odd...
            CheckTest("lksamm11_3.txt", false, false, 6, 12);
            CheckTest("lksamm11_4.txt", false, false, 6, 12);
        }

        [Test]
        public void LakeSammMap12()
        {
            CheckTest("lksamm12_1.txt", false, true, 6, 12);
            CheckTest("lksamm12_1.txt", false, true, 6, 12);
            // CheckTest("lksamm11_2.txt", true, false, 6);  // this one has very slight rendering differences each time. odd...
            CheckTest("lksamm12_3.txt", false, true, 6, 12);
            CheckTest("lksamm12_4.txt", false, true, 6, 12);
        }

        [Test]
        public void DeletedItems()
        {
            CheckTest("deleteditems.txt", false, true, 6, 12);
        }

        [Test]
        public void CornersAndEnds()
        {
            CheckTest("corner_ends.txt", false, true, 6, 12);
            CheckTest("corner_ends9.txt", false, true, 6, 12);
        }

        [Test]
        public void GlyphHoles()
        {
            CheckTest("glyphholes.txt", false, true, 6, 12);
            CheckTest("glyphholes9.txt", false, true, 6, 12);
        }

        [Test]
        public void ZeroGlyph()
        {
            CheckTest("zeroglyph9.txt", false, true, 6, 12);
            CheckTest("zeroglyph6.txt", false, true, 6, 12);
        }

        [Test]
        public void OffsetAreaPattern() {
            CheckTest("offsetpattern.txt", false, true, 6, 12);
        }

        [Test]
        public void OffsetAreaPatternNoBitmap() {
            CheckTestNoPatternBitmaps("offsetpattern_nopatbm.txt", false, true, 6, 12);
        }

        [Test]
        public void OffsetAreaPatternRotated() {
            CheckTest("offsetpatternrot.txt", false, true, 6, 12);
        }

        [Test]
        public void OffsetAreaPatternRotated2() {
            CheckTest("offsetpatternrot2.txt", false, true, 6, 12);
        }

        [Test]
        public void OffsetAreaPatternRotatedNoBitmap() {
            CheckTestNoPatternBitmaps("offsetpatternrot_nopatbm.txt", false, true, 6, 12);
        }

        [Test]
        public void OffsetAreaPatternRotated2NoBitmap() {
            CheckTestNoPatternBitmaps("offsetpatternrot2_nopatbm.txt", false, true, 6, 12);
        }

        [Test]
        public void AreaSymbolsBug() {
            CheckTest("isomareabug.txt", false, false, 9, 9);
        }

        [Test]
        public void AreaSymbolsBugNoBitmap() {
            CheckTestNoPatternBitmaps("isomareabug_nopatbm.txt", false, false,9, 9);
        }

        [Test]
        public void AreaSymbolsNoBitmap() {
            CheckTestNoPatternBitmaps("isomarea_nopatbm.txt", true, true, 6, 9);
        }

        [Test]
        public void ParaSpacing()
        {
            CheckTest("paraspacing.txt", false, true, 6, 12);
            CheckTest("paraspacing9.txt", false, true, 6, 12);
        }

        [Test]
        public void ParaIdent()
        {
            CheckTest("paraindent9.txt", false, true, 6, 12);
            CheckTest("paraindent6.txt", false, true, 6, 12);
        }

        [Test]
        public void NarrowWrap()
        {
            CheckTest("textnarrowwrap.txt", false, true, 6, 12);
        }

        [Test]
        public void CharSpace()
        {
            CheckTest("charspace.txt", false, true, 6, 12);
        }

        [Test]
        public void WordSpace()
        {
            CheckTest("wordspace.txt", false, true, 6, 12);
        }

        [Test]
        public void ComboSpace()
        {
            CheckTest("combospace.txt", false, true, 6, 12);
        }

        [Test]
        public void TopAlignText()
        {
            CheckTest("topaligntext10.txt", false, true, 10, 12);
        }

        [Test]
        public void MidAlignText() {
            CheckTest("midaligntext10.txt", false, true, 10, 12);
        }

        [Test]
        public void CenterPointText() {
            CheckTest("textpoint10.txt", false, true, 10, 12);
        }

        [Test]
        public void Justify()
        {
            CheckTest("justify.txt", false, true, 6, 12);
        }

        [Test]
        public void TabbedText()
        {
            CheckTest("tabbedtext.txt", false, true, 6, 12);
        }

        [Test]
        public void Newlines()
        {
            CheckTest("newlines.txt", false, true, 6, 12);
        }

        [Test]
        public void UnderlineText()
        {
            CheckTest("underlinetext.txt", false, true, 6, 12);
        }

        [Test]
        public void LineText1()
        {
            CheckTest("linetext_6.txt", false, true, 6, 12);
            CheckTest("linetext_9.txt", false, true, 6, 12);   
        }

        [Test]
        public void LineText2()
        {
            CheckTest("linetext2_6.txt", false, true, 6, 12);
            CheckTest("linetext2_9.txt", false, true, 6, 12);
        }

        [Test]
        public void LineTextSpacing()
        {
            CheckTest("linetextspacing.txt", false, true, 6, 12);
        }

        [Test]
        public void AllLineText()
        {
            CheckTest("alllinetext.txt", false, true, 6, 12);
        }

        [Test]
        public void LineTextTop() {
            CheckTest("linetext_top.txt", false, true, 10, 12);
        }

        [Test]
        public void LineTextTop2() {
            CheckTest("linetext2_top.txt", false, true, 10, 12);
        }

        [Test]
        public void LineTextMid() {
            CheckTest("linetext_mid.txt", false, true, 10, 12);
        }

        [Test]
        public void LineTextMid2() {
            CheckTest("linetext2_mid.txt", false, true, 10, 12);
        }

        [Test]
        public void FramingText1()
        {
            CheckTest("frametext1.txt", false, true, 7, 12);
        }

        [Test]
        public void FramingText2()
        {
            CheckTest("frametext2.txt", false, true, 9, 12);
        }

        [Test]
        public void FramingText3()
        {
            CheckTest("frametext3.txt", false, true, 9, 12);
            CheckTest("frametext3.txt", false, true, 6, 7);
        }

        [Test]
        public void Framing_Ocad6()
        {
            // Not supported in OCAD 8!!! (OCAD 8 didn't have font framing or offset framing
            CheckTest("framing_ocad6.txt", false, true, 6, 7);
            CheckTest("framing_ocad6.txt", false, true, 9, 12);
        }

        [Test]
        public void Framing_Ocad7()
        {
            // Not supported in OCAD 6 or 8!!! (OCAD 8 didn't have font framing or offset framing, OCAD 6 didn't have line framing).
            CheckTest("framing_ocad7.txt", false, true, 7, 7);
            CheckTest("framing_ocad7.txt", false, true, 9, 12);
        }

        [Test]
        public void Framing_Ocad8()
        {
            CheckTest("framing_ocad8.txt", false, true, 7, 12);
        }

        [Test]
        public void DoubleLines()
        {
            CheckTest("doublelines9.txt", false, true, 6, 12);
            CheckTest("doublelines6.txt", false, true, 6, 12);
        }

        [Test]
        public void CutDoubleSides()
        {
            CheckTest("cutdoublesides9.txt", false, true, 6, 12);
            CheckTest("cutdoublesides6.txt", false, true, 6, 12);
        }

        [Test]
        public void CutAreaBorder()
        {
            CheckTest("cutareaborder9.txt", false, true, 9, 12);
        }

        [Test]
        public void EuclideanDashLengths11()
        {
            CheckTest("euclid_dash11.txt", false, true, 11, 12);
        }

        [Test]
        public void BizzarroDashLengths11()
        {
            CheckTest("bizzarro_dash11.txt", false, true, 11, 12);
        }

        [Test]
        public void LineGaps10() {
            CheckTest("linegaps10.txt", false, true, 10, 12);
        }

        [Test]
        public void DashLengths()
        {
            CheckTest("dashlengths9.txt", false, true, 6, 12);
            CheckTest("dashlengths6.txt", false, true, 6, 12);
        }

        [Test]
        public void AngleDashes()
        {
            CheckTest("angledashes9.txt", false, true, 6, 12);
        }

        [Test]
        public void DoubleDashLengths()
        {
            CheckTest("dbldashlengths9.txt", false, true, 7, 12);  // OCAD 6 doesn't support dash points.
            CheckTest("dbldashlengths7.txt", false, true, 7, 12);  // OCAD 6 doesn't support dash points.
        }

        [Test]
        public void SecGapOnly()
        {
            CheckTest("secgaponly9.txt", false, true, 6, 12);
            CheckTest("secgaponly6.txt", false, true, 6, 12);
        }

        [Test]
        public void PointyEnds()
        {
            CheckTest("pointyends9.txt", false, true, 6, 12);
            CheckTest("pointyends6.txt", false, true, 6, 12);
        }

        [Test]
        public void BadPointyEnds()
        {
            CheckTest("badpointyends.txt", false, true, 6, 12);
        }

        [Test]
        public void Glyphs()
        {
            CheckTest("glyphs9.txt", false, true, 6, 12);
        }

        [Test]
        public void MainGlyphs()
        {
            CheckTest("mainglyph9.txt", false, true, 7, 12);
            CheckTest("mainglyph7.txt", false, true, 7, 12);
        }

        [Test]
        public void Max2Glyphs()
        {
            CheckTest("max2glyph9.txt", false, true, 7, 12);
            CheckTest("max2glyph7.txt", false, true, 7, 12);
        }

        [Test]
        public void MultiMainGlyphs()
        {
            CheckTest("multimainglyph9.txt", false, true, 7, 12);
            CheckTest("multimainglyph7.txt", false, true, 7, 12);
        }

        [Test]
        public void SecondaryGlyphs()
        {
            CheckTest("secglyph9.txt", false, true, 7, 12);
            CheckTest("secglyph7.txt", false, true, 7, 12);
        }

        [Test]
        public void StartEndGlyph()
        {
            CheckTest("startendglyph9.txt", false, true, 7, 12);
            CheckTest("startendglyph7.txt", false, true, 7, 12);
        }

        [Test]
        public void CornerGlyphs()
        {
            CheckTest("cornerglyphs9.txt", false, true, 6, 12);
            CheckTest("cornerglyphs6.txt", false, true, 6, 12);
        }

        [Test]
        public void DecreaseSymbols()
        {
            CheckTest("decreasesymbols.txt", false, true, 6, 12);
            CheckTest("decreasesymbols6.txt", false, true, 6, 12);
        }

        [Test]
        public void GraphicsObjects()
        {
            CheckTest("graphicobjects9.txt", true, true, 9, 12);
        }

        [Test]
        public void ImageObjects()
        {
            CheckTest("aiimport.txt", true, true, 9, 12);
        }

        [Test]
        public void Clouds()
        {
            CheckTest("Clouds.txt", false, true, 7, 12);
        }

        [Test]
        public void Clouds11()
        {
            CheckTest("Clouds11.txt", false, false, 11, 12);
        }

        [Test]
        public void LordHill() {
            CheckTest("LordHill.txt", false, false, 6, 12);
        }

        [Test]
        public void LordHill11()
        {
            CheckTest("LordHill11.txt", false, false, 11, 12);
        }

        [Test]
        public void MissingColor()
        {
            CheckTest("missingcolor.txt", false, false, 6, 12);
        }

        [Test]
        public void Decrease()
        {
            CheckTest("decrease.txt", false, false, 6, 12);
        }

        [Test]
        public void OddFenceEnds()
        {
            CheckTest("oddfenceends.txt", false, false, 6, 12);
        }

        [Test]
        public void Marymoor()
        {
            CheckTest("marymoor.txt", false, true, 7, 12);
        }

        [Test]
        public void Marymoor11()
        {
            CheckTest("marymoor11.txt", false, false, 11, 12);
        }

        [Test]
        public void LayoutObjects()
        {
            CheckTest("layout_objects11.txt", true, true, 11, 12);
        }

        [Test]
        public void LayoutObjects12()
        {
            CheckTest("layout_objects12.txt", true, false, 11, 12);
        }

        [Test]
        public void LayoutBitmapObjects()
        {
            CheckTest("layoutbitmap11.txt", true, true, 11, 12);
        }

        [Test]
        public void LayoutEmbeddedBitmapObjects()
        {
            CheckTest("layoutbitmap_embed11.txt", true, true, 11, 12);
        }

        [Test]
        public void LayoutHidden()
        {
            CheckTest("hidden_layout.txt", true, true, 11, 12);
        }



        [Test]
        public void TemplateRendering()
        {
            CheckTest("template.txt", true, false, 9, 12);
        }

        [Test]
        public void TemplateRendering2()
        {
            CheckTest("template2.txt", true, false, 9, 12);
        }

        [Test]
        public void RecursiveTemplate()
        {
            CheckTest("recursivetempl.txt", false, false, 9, 12);
        }

        [Test]
        public void HiddenTemplateRendering()
        {
            CheckTest("templatehide.txt", true, false, 9, 12);
        }

        [Test]
        public void TemplateFraction1()
        {
            CheckTest("template_fraction1.txt", false, true, 9, 12);
        }

        [Test]
        public void TemplateFraction2()
        {
            CheckTest("template_fraction2.txt", false, true, 9, 12);
        }

        [Test]
        public void TestLogo()
        {
            CheckTest("testlogo.txt", false, false, 8, 8);
        }

        [Test]
        public void Ocad11Align()
        {
            CheckTest("ocad11templatealign.txt", false, false, 11, 12);
        }

        [Test]
        public void PunchBoxBug()
        {
            CheckTest("punchboxbug.txt", false, true, 9, 12);
        }

        [Test]
        public void SmallRectangleSymbols()
        {
            CheckTest("rectanglesymbols.txt", false, true, 6, 12);
        }

        [Test]
        public void PenistoneHill()
        {
            CheckTest("penistonehill.txt", false, false, 7, 7);
        }

        [Test]
        public void FtCasey()
        {
            CheckTest("FtCasey.txt", false, false, 7, 7);
        }

        [Test]
        public void NorthArrowEnds()
        {
            CheckTest("MagNorthArrowBug.txt", false, false, 11, 12);
        }

        [Test]
        public void DifferentNewlineTypes()
        {
            CheckTest("differentnewlinetypes.txt", false, false, 7, 7);
        }

        [Test]
        public void Overprinting()
        {
            CheckTestOverprinting("ocad11overprinting.txt", true, true, 6, 12);
        }

        [Test]
        public void KernTextOutline()
        {
            CheckTest("kern_text_outline.txt", false, true, 7, 12);
        }

        [Test]
        public void MultiSymOnDash()
        {
            CheckTest("multisymonedash.txt", false, true, 12, 12);
        }

        [Test]
        public void DashMin()
        {
            CheckTest("dashmin.txt", false, true, 8, 12);
        }

        [Test]
        public void WholeStructure1()
        {
            CheckTest("wholestructure.txt", false, true, 12, 12);
        }


        [Test]
        public void WholeStructure2()
        {
            CheckTest("wholestructure2.txt", false, true, 12, 12);
        }
        
        [Test]
        public void WholeStructure3()
        {
            CheckTest("wholestructure3.txt", false, true, 12, 12);
        }


        [Test]
        public void WholeStructure4()
        {
            CheckTest("wholestructure4.txt", false, true, 12, 12);
        }
        
        [Test]
        public void Irregular1()
        {
            CheckTest("irregular1.txt", false, true, 12, 12);
        }

        [Test]
        public void Irregular2()
        {
            CheckTest("irregular2.txt", false, true, 12, 12);
        }


        [Test]
        public void Irregular3()
        {
            CheckTest("irregular3.txt", false, true, 12, 12);
        }


        [Test]
        public void Irregular4()
        {
            CheckTest("irregular4.txt", false, true, 12, 12);
        }

        [Test]
        public void Roboto()
        {
            CheckTest("RobotoTest.txt", false, false, 9, 12);
        }

        [Test]
        public void Marymoor11LowerLayers()
        {
            CheckTestLayers("marymoor11_lowerlayers.txt", null, 7, false, false, 11, 12);
        }

        [Test]
        public void Marymoor11UpperLayers()
        {
            CheckTestLayers("marymoor11_upperlayers.txt", 7, null, false, false, 11, 12);
        }


    }

}

#endif //TEST
