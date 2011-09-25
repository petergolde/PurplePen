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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.MapModel.Tests
{
    [TestClass]
    public class Rendering 
    {

        // Write a bitmap to a PNG.
        void WriteBitmap(Bitmap bmp, string filename) {
           bmp.Save(filename, ImageFormat.Png);
        }

        void GDIPlus_RenderingTest(int width, RectangleF drawingRectangle, string pngFileName, Action<IGraphicsTarget> draw)
        {
            int height = (int)Math.Ceiling(width * drawingRectangle.Height / drawingRectangle.Width);

            // Calculate the transform matrix.
            PointF midpoint = new PointF(width / 2.0F, height / 2.0F);
            float scaleFactor = (float)width / drawingRectangle.Width;
            PointF centerPoint = new PointF((drawingRectangle.Left + drawingRectangle.Right) / 2, (drawingRectangle.Top + drawingRectangle.Bottom) / 2);
            Matrix matrix = new Matrix();
            matrix.Translate(midpoint.X, midpoint.Y, MatrixOrder.Prepend);
            matrix.Scale(scaleFactor, -scaleFactor, MatrixOrder.Prepend);  // y scale is negative to get to cartesian orientation.
            matrix.Translate(-centerPoint.X, -centerPoint.Y, MatrixOrder.Prepend);

            // Draw into a new bitmap.
            Bitmap bitmapNew = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(bitmapNew)) {
                g.Clear(Color.White);
                g.Transform = matrix;

                draw(new GDIPlus_GraphicsTarget(g));
            }

            string directoryName = Path.GetDirectoryName(pngFileName);
            string newBitmapName = Path.Combine(directoryName,
                                        Path.GetFileNameWithoutExtension(pngFileName) + "_new.png");
            File.Delete(newBitmapName);

            TestUtil.CompareBitmapBaseline(bitmapNew, pngFileName);
        }


        [TestMethod]
        public void PatternBrush() {
            GDIPlus_RenderingTest(500, new RectangleF(-100, -100, 200, 200), TestUtil.GetTestFile("rendering\\patternbrush"),
                grTarget => {
                    IBrushTarget brushTarget = grTarget.CreatePatternBrush(new SizeF(20, 20), 60, 60);

                    object pen = new object();
                    brushTarget.CreatePen(pen, System.Drawing.Color.Red, 1.5F, LineCap.Round, LineJoin.Round, 5F);
                    brushTarget.DrawLine(pen, new PointF(-7, -7), new PointF(-1, 5));

                    pen = new object();
                    brushTarget.CreatePen(pen, System.Drawing.Color.Blue, 3F, LineCap.Round, LineJoin.Round, 5F);
                    brushTarget.DrawEllipse(pen, new PointF(1, -2), 5, 4);

                    object brush = new object();
                    brushTarget.FinishBrush(brush, 0);

                    grTarget.FillPolygon(brush, new PointF[] { new PointF(-50, -30), new PointF(0, 80), new PointF(50, -30), new PointF(-50, 50), new PointF(50, 50) }, FillMode.Alternate);
                });
        }




        [TestInitialize]
        public void Init()
        {
        }

        static Bitmap RenderBitmap(Map map, Size bitmapSize, RectangleF mapArea, bool usePatternBitmaps)
        {
            // Calculate the transform matrix.
            PointF midpoint = new PointF(bitmapSize.Width / 2.0F, bitmapSize.Height / 2.0F);
            float scaleFactor = (float) bitmapSize.Width / mapArea.Width;
            PointF centerPoint = new PointF((mapArea.Left + mapArea.Right) / 2, (mapArea.Top + mapArea.Bottom) / 2);
            Matrix matrix = new Matrix();
            matrix.Translate(midpoint.X, midpoint.Y, MatrixOrder.Prepend);
            matrix.Scale(scaleFactor, -scaleFactor, MatrixOrder.Prepend);  // y scale is negative to get to cartesian orientation.
            matrix.Translate(-centerPoint.X, -centerPoint.Y, MatrixOrder.Prepend);

            // Draw into a new bitmap.
            Bitmap bitmapNew = new Bitmap(bitmapSize.Width, bitmapSize.Height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(bitmapNew)) {
                RenderOptions renderOpts = new RenderOptions();
                renderOpts.usePatternBitmaps = usePatternBitmaps;
                renderOpts.minResolution = mapArea.Width / (float) bitmapSize.Width;

                g.Clear(Color.White);
                g.Transform = matrix;

                using (map.Read())
                    map.Draw(new GDIPlus_GraphicsTarget(g), mapArea, renderOpts, null);
            }

            return bitmapNew;
        }

        // Verifies a test file. Returns true on success, false on failure. In the failure case, 
        // a difference bitmap is written out.
        static bool VerifyTestFile(string filename, bool usePatternBitmaps, bool testLightenedColor, bool roundtripToOcadFile, int minOcadVersion, int maxOcadVersion)
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
            Map map = new Map(new GDIPlus_TextMetrics());
            InputOutput.ReadFile(mapFileName, map);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Draw into a new bitmap.
            Bitmap bitmapNew = RenderBitmap(map, size, mapArea, usePatternBitmaps);

            sw.Stop();
            Console.WriteLine("Rendered bitmap '{0}' to output '{4}' rect={1} size={2} in {3} ms", mapFileName, mapArea, size, sw.ElapsedMilliseconds, pngFileName);

            TestUtil.CompareBitmapBaseline(bitmapNew, pngFileName);

            if (testLightenedColor) {
                using (map.Write()) {
                    ColorMatrix colorMatrix = new ColorMatrix(new float[][] {
                           new float[] {0.4F,  0,  0,  0, 0},
                           new float[] {0,  0.4F,  0,  0, 0},
                           new float[] {0,  0,  0.4F,  0, 0},
                           new float[] {0,  0,  0,  1, 0},
                           new float[] {0.6F, 0.6F, 0.6F, 0, 1}
                    });
                    map.ColorMatrix = colorMatrix;
                }

                string lightenedPngFileName = Path.Combine(Path.GetDirectoryName(pngFileName), Path.GetFileNameWithoutExtension(pngFileName) + "_light.png");
                Bitmap bitmapLight = RenderBitmap(map, size, mapArea, usePatternBitmaps);
                TestUtil.CompareBitmapBaseline(bitmapLight, lightenedPngFileName);
            }

            if (roundtripToOcadFile) {
                for (int version = minOcadVersion; version <= maxOcadVersion; ++version) {  
                    // Save and load to a temp file name.
                    InputOutput.WriteFile(ocadFileName, map, version);

                    // Create and open the map file.
                    map = new Map(new GDIPlus_TextMetrics());
                    InputOutput.ReadFile(ocadFileName, map);

                    // Draw into a new bitmap.
                    bitmapNew = RenderBitmap(map, size, mapArea, usePatternBitmaps);

                    TestUtil.CompareBitmapBaseline(bitmapNew, pngFileName);

                    File.Delete(ocadFileName);
                }
            }

            return true;
        }

        void TimeMapRender(Map map, Size size, RectangleF mapArea, string name) {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Draw into a new bitmap.
            Bitmap bitmapNew = RenderBitmap(map, size, mapArea, true);

            sw.Stop();
            Console.WriteLine("Rendered bitmap '{0}' in {1} ms", name, sw.ElapsedMilliseconds);
        }


        void CheckTest(string filename, bool testLightenedColor, bool roundtripToOcad, int minOcadVersion, int maxOcadVersion)
        {
            string fullname = TestUtil.GetTestFile("rendering\\" + filename);
            bool ok = VerifyTestFile(fullname, true, testLightenedColor, roundtripToOcad, minOcadVersion, maxOcadVersion);
            Assert.IsTrue(ok, string.Format("Rendering test {0} did not compare correctly.", filename), ok);
        }

        void CheckTestNoPatternBitmaps(string filename, bool testLightenedColor, bool roundtripToOcad, int minOcadVersion, int maxOcadVersion) {
            string fullname = TestUtil.GetTestFile("rendering\\" + filename);
            bool ok = VerifyTestFile(fullname, false, testLightenedColor, roundtripToOcad, minOcadVersion, maxOcadVersion);
            Assert.IsTrue(ok, string.Format("Rendering test {0} did not compare correctly.", filename), ok);
        }

        public void TimeTeanWest() {
            // Create and open the map file.
            Map map = new Map(new GDIPlus_TextMetrics());
            InputOutput.ReadFile(@"C:\Users\Peter\Documents\PurplePen\newmapmodel\src\TestFiles\d2drender\teanwest.ocd", map);

            TimeMapRender(map, new Size(1814, 1022), new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
            TimeMapRender(map, new Size(1814, 1022), new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
            TimeMapRender(map, new Size(1814, 1022), new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
            TimeMapRender(map, new Size(1814, 1022), new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
            TimeMapRender(map, new Size(1814, 1022), new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
        }


        [TestMethod]
        public void TestWest() {
            CheckTest("teanwest.txt", false, false, 9, 10);
            CheckTest("teanwest.txt", false, false, 9, 10);
        }

        [TestMethod]
        public void LineSymbols()
        {
            CheckTest("isomlines.txt", true, true, 6, 10);
            CheckTest("isomlines9.txt", true, true, 6, 10);
        }

        [TestMethod]
        public void Fences()
        {
            CheckTest("fences.txt", false, true, 6, 10);
            CheckTest("fences9.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void FramingLines()
        {
            CheckTest("framingline-test.txt", false, true, 6, 10);
            CheckTest("framingline-test9.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void DashLines()
        {
            CheckTest("dashline.txt", false, true, 6, 10);
            CheckTest("dashline9.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void PointSymbols()
        {
            CheckTest("isompoints.txt", true, true, 6, 10);
            CheckTest("isompoints_9.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void AreaSymbols()
        {
            CheckTest("isomarea.txt", true, true, 6, 10);
            CheckTest("isomarea9.txt", true, true, 6, 10);
        }

        [TestMethod]
        public void AreaHoles()
        {
            CheckTest("holes.txt", false, true, 6, 10);
            CheckTest("holes9.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void CutCircles()
        {
            CheckTest("cutcircles.txt", false, true, 6, 10);
            // CheckTest("cutcircles9.txt", false, false, 6);    OCAD 9 has some strange problems with cut circles...
        }

        [TestMethod]
        public void HiddenSymbols()
        {
            CheckTest("hiddensymbols.txt", false, true, 6, 10);
            CheckTest("hiddensymbols9.txt", false, true, 6, 10);
        }
    

        [TestMethod]
        public void RotatedAreas()
        {
            CheckTest("rotarea-test.txt", false, true, 6, 10);
            CheckTest("rotarea-test9.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void BorderedAreas()
        {
            CheckTest("borderedarea9.txt", false, true, 9, 10);
        }
    

        [TestMethod]
        public void TextSymbols()
        {
            CheckTest("simpletext.txt", true, true, 6, 10);
            CheckTest("simpletext9.txt", true, true, 6, 10);
        }

        [TestMethod]
        public void PunchBox()
        {
            CheckTest("punchbox.txt", false, false, 6, 10);
            CheckTest("punchbox9.txt", false, false, 6, 10);
        }

        [TestMethod]
        public void LakeSammMap()
        {
            CheckTest("lksamm1.txt", false, true, 6, 10);
            // CheckTest("lksamm2.txt", true, true, 6);   // this one has very slight rendering differences each time. odd...
            CheckTest("lksamm3.txt", false, true, 6, 10);
            CheckTest("lksamm4.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void LakeSammMap9()
        {
            CheckTest("lksamm9_1.txt", false, false, 6, 10);
            CheckTest("lksamm9_1.txt", false, false, 6, 10);
            // CheckTest("lksamm9_2.txt", true, false, 6);  // this one has very slight rendering differences each time. odd...
            CheckTest("lksamm9_3.txt", false, false, 6, 10);
            CheckTest("lksamm9_4.txt", false, false, 6, 10);
        }

        [TestMethod]
        public void DeletedItems()
        {
            CheckTest("deleteditems.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void CornersAndEnds()
        {
            CheckTest("corner_ends.txt", false, true, 6, 10);
            CheckTest("corner_ends9.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void GlyphHoles()
        {
            CheckTest("glyphholes.txt", false, true, 6, 10);
            CheckTest("glyphholes9.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void ZeroGlyph()
        {
            CheckTest("zeroglyph9.txt", false, true, 6, 10);
            CheckTest("zeroglyph6.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void OffsetAreaPattern() {
            CheckTest("offsetpattern.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void OffsetAreaPatternNoBitmap() {
            CheckTestNoPatternBitmaps("offsetpattern_nopatbm.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void OffsetAreaPatternRotated() {
            CheckTest("offsetpatternrot.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void OffsetAreaPatternRotatedNoBitmap() {
            CheckTestNoPatternBitmaps("offsetpatternrot_nopatbm.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void ParaSpacing()
        {
            CheckTest("paraspacing.txt", false, true, 6, 10);
            CheckTest("paraspacing9.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void ParaIdent()
        {
            CheckTest("paraindent9.txt", false, true, 6, 10);
            CheckTest("paraindent6.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void NarrowWrap()
        {
            CheckTest("textnarrowwrap.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void CharSpace()
        {
            CheckTest("charspace.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void WordSpace()
        {
            CheckTest("wordspace.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void ComboSpace()
        {
            CheckTest("combospace.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void TopAlignText()
        {
            CheckTest("topaligntext10.txt", false, true, 10, 10);
        }

        [TestMethod]
        public void MidAlignText() {
            CheckTest("midaligntext10.txt", false, true, 10, 10);
        }

        [TestMethod]
        public void CenterPointText() {
            CheckTest("textpoint10.txt", false, true, 10, 10);
        }

        [TestMethod]
        public void Justify()
        {
            CheckTest("justify.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void TabbedText()
        {
            CheckTest("tabbedtext.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void Newlines()
        {
            CheckTest("newlines.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void UnderlineText()
        {
            CheckTest("underlinetext.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void LineText1()
        {
            CheckTest("linetext_6.txt", false, true, 6, 10);
            CheckTest("linetext_9.txt", false, true, 6, 10);   
        }

        [TestMethod]
        public void LineText2()
        {
            CheckTest("linetext2_6.txt", false, true, 6, 10);
            CheckTest("linetext2_9.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void LineTextSpacing()
        {
            CheckTest("linetextspacing.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void AllLineText()
        {
            CheckTest("alllinetext.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void LineTextTop() {
            CheckTest("linetext_top.txt", false, true, 10, 10);
        }

        [TestMethod]
        public void LineTextTop2() {
            CheckTest("linetext2_top.txt", false, true, 10, 10);
        }

        [TestMethod]
        public void LineTextMid() {
            CheckTest("linetext_mid.txt", false, true, 10, 10);
        }

        [TestMethod]
        public void LineTextMid2() {
            CheckTest("linetext2_mid.txt", false, true, 10, 10);
        }

        [TestMethod]
        public void FramingText1()
        {
            CheckTest("frametext1.txt", false, true, 7, 10);
        }

        [TestMethod]
        public void FramingText2()
        {
            CheckTest("frametext2.txt", false, true, 9, 10);
        }

        [TestMethod]
        public void FramingText3()
        {
            CheckTest("frametext3.txt", false, true, 9, 10);
            CheckTest("frametext3.txt", false, true, 6, 7);
        }

        [TestMethod]
        public void Framing_Ocad6()
        {
            // Not supported in OCAD 8!!! (OCAD 8 didn't have font framing or offset framing
            CheckTest("framing_ocad6.txt", false, true, 6, 7);
            CheckTest("framing_ocad6.txt", false, true, 9, 10);
        }

        [TestMethod]
        public void Framing_Ocad7()
        {
            // Not supported in OCAD 6 or 8!!! (OCAD 8 didn't have font framing or offset framing, OCAD 6 didn't have line framing).
            CheckTest("framing_ocad7.txt", false, true, 7, 7);
            CheckTest("framing_ocad7.txt", false, true, 9, 10);
        }

        [TestMethod]
        public void Framing_Ocad8()
        {
            CheckTest("framing_ocad8.txt", false, true, 7, 10);
        }

        [TestMethod]
        public void DoubleLines()
        {
            CheckTest("doublelines9.txt", false, true, 6, 10);
            CheckTest("doublelines6.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void CutDoubleSides()
        {
            CheckTest("cutdoublesides9.txt", false, true, 6, 10);
            CheckTest("cutdoublesides6.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void CutAreaBorder()
        {
            CheckTest("cutareaborder9.txt", false, true, 9, 10);
        }

        [TestMethod]
        public void LineGaps10() {
            CheckTest("linegaps10.txt", false, true, 10, 10);
        }

        [TestMethod]
        public void DashLengths()
        {
            CheckTest("dashlengths9.txt", false, true, 6, 10);
            CheckTest("dashlengths6.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void AngleDashes()
        {
            CheckTest("angledashes9.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void DoubleDashLengths()
        {
            CheckTest("dbldashlengths9.txt", false, true, 7, 10);  // OCAD 6 doesn't support dash points.
            CheckTest("dbldashlengths7.txt", false, true, 7, 10);  // OCAD 6 doesn't support dash points.
        }

        [TestMethod]
        public void SecGapOnly()
        {
            CheckTest("secgaponly9.txt", false, true, 6, 10);
            CheckTest("secgaponly6.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void PointyEnds()
        {
            CheckTest("pointyends9.txt", false, true, 6, 10);
            CheckTest("pointyends6.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void Glyphs()
        {
            CheckTest("glyphs9.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void MainGlyphs()
        {
            CheckTest("mainglyph9.txt", false, true, 7, 10);
            CheckTest("mainglyph7.txt", false, true, 7, 10);
        }

        [TestMethod]
        public void Max2Glyphs()
        {
            CheckTest("max2glyph9.txt", false, true, 7, 10);
            CheckTest("max2glyph7.txt", false, true, 7, 10);
        }

        [TestMethod]
        public void MultiMainGlyphs()
        {
            CheckTest("multimainglyph9.txt", false, true, 7, 10);
            CheckTest("multimainglyph7.txt", false, true, 7, 10);
        }

        [TestMethod]
        public void SecondaryGlyphs()
        {
            CheckTest("secglyph9.txt", false, true, 7, 10);
            CheckTest("secglyph7.txt", false, true, 7, 10);
        }

        [TestMethod]
        public void StartEndGlyph()
        {
            CheckTest("startendglyph9.txt", false, true, 7, 10);
            CheckTest("startendglyph7.txt", false, true, 7, 10);
        }

        [TestMethod]
        public void CornerGlyphs()
        {
            CheckTest("cornerglyphs9.txt", false, true, 6, 10);
            CheckTest("cornerglyphs6.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void DecreaseSymbols()
        {
            CheckTest("decreasesymbols.txt", false, true, 6, 10);
            CheckTest("decreasesymbols6.txt", false, true, 6, 10);
        }

        [TestMethod]
        public void GraphicsObjects()
        {
            CheckTest("graphicobjects9.txt", true, true, 9, 10);
        }

        [TestMethod]
        public void ImageObjects()
        {
            CheckTest("aiimport.txt", true, true, 9, 10);
        }

        [TestMethod]
        public void Clouds()
        {
            CheckTest("Clouds.txt", false, true, 7, 10);
        }

        [TestMethod]
        public void LordHill() {
            CheckTest("LordHill.txt", false, false, 6, 10);
        }
    }

}

#endif //TEST
