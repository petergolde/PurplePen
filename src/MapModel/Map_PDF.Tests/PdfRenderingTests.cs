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


using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Diagnostics;
using NUnit.Framework;
using TestingUtils;
using PurplePen.MapModel;
using System.Net;

namespace Map_PDF.Tests
{
    [TestFixture, Parallelizable(ParallelScope.Children)]
    public class Rendering 
    {
        private const int MAX_PIXEL_DIFF = 20;

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


        [SetUp]
        public void Init()
        {
        }

        static Bitmap RenderBitmap(string pdfFileName, Map map, Size bitmapSize, RectangleF mapArea, bool usePatternBitmaps)
        {
            // Get PNG file name
            string directoryName = Path.GetDirectoryName(pdfFileName);
            string pngFileName = Path.Combine(directoryName,
                                         Path.GetFileNameWithoutExtension(pdfFileName) + ".png");

            // Calculate the transform matrix.
            PointF midpoint = new PointF(bitmapSize.Width / 2.0F, bitmapSize.Height / 2.0F);
            float scaleFactor = (float) bitmapSize.Width / mapArea.Width;
            PointF centerPoint = new PointF((mapArea.Left + mapArea.Right) / 2, (mapArea.Top + mapArea.Bottom) / 2);
            Matrix matrix = new Matrix();
            matrix.Translate(midpoint.X, midpoint.Y, MatrixOrder.Prepend);
            matrix.Scale(scaleFactor, -scaleFactor, MatrixOrder.Prepend);  // y scale is negative to get to cartesian orientation.
            matrix.Translate(-centerPoint.X, -centerPoint.Y, MatrixOrder.Prepend);

            RenderOptions renderOpts = new RenderOptions();
            renderOpts.usePatternBitmaps = usePatternBitmaps;
            renderOpts.renderTemplates = RenderTemplateOption.MapAndTemplates;
            renderOpts.minResolution = mapArea.Width / (float)bitmapSize.Width;            // Draw into a new bitmap.

            PdfCreation.CreatePdfAndPng(pdfFileName, pngFileName, bitmapSize.Width, bitmapSize.Height, false,
                grTarget =>
                {
                    grTarget.PushTransform(matrix);
                    using (map.Read())
                        map.Draw(grTarget, mapArea, renderOpts, null);
                });

            return (Bitmap) Image.FromFile(pngFileName);
        }

        // Verifies a test file. Returns true on success, false on failure. In the failure case, 
        // a difference bitmap is written out.
        static bool VerifyTestFile(string filename, bool usePatternBitmaps, bool testLightenedColor, bool roundtripToOcadFile, int minOcadVersion, int maxOcadVersion)
        {
            string pngFileName;
            string mapFileName;
            string tempPdfFileName;
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
            tempPdfFileName = Path.Combine(directoryName,
                                         Path.GetFileNameWithoutExtension(pngFileName) + "_temp.pdf");
            ocadFileName = Path.Combine(directoryName,
                                         Path.GetFileNameWithoutExtension(pngFileName) + "_temp.ocd");

            File.Delete(ocadFileName);

            // Create and open the map file.
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(directoryName));
            InputOutput.ReadFile(mapFileName, map);

            // Draw into a new bitmap.
            Bitmap bitmapNew = RenderBitmap(tempPdfFileName, map, size, mapArea, usePatternBitmaps);

            TestUtil.CompareBitmapBaseline(bitmapNew, pngFileName, MAX_PIXEL_DIFF);
            bitmapNew.Dispose();
            bitmapNew = null;

            if (testLightenedColor) {
                using (map.Write()) {
                    PurplePen.MapModel.ColorMatrix colorMatrix = new PurplePen.MapModel.ColorMatrix(new float[][] {
                           new float[] {0.4F,  0,  0,  0, 0},
                           new float[] {0,  0.4F,  0,  0, 0},
                           new float[] {0,  0,  0.4F,  0, 0},
                           new float[] {0,  0,  0,  1, 0},
                           new float[] {0.6F, 0.6F, 0.6F, 0, 1}
                    });
                    map.ColorMatrix = colorMatrix;
                }

                string lightenedPngFileName = Path.Combine(Path.GetDirectoryName(pngFileName), Path.GetFileNameWithoutExtension(pngFileName) + "_light.png");
                Bitmap bitmapLight = RenderBitmap(tempPdfFileName, map, size, mapArea, usePatternBitmaps);
                TestUtil.CompareBitmapBaseline(bitmapLight, lightenedPngFileName, MAX_PIXEL_DIFF);
                bitmapLight.Dispose();
                bitmapLight = null;
            }

            if (roundtripToOcadFile) {
                for (int version = minOcadVersion; version <= maxOcadVersion; ++version) {  
                    // Save and load to a temp file name.
                    InputOutput.WriteFile(ocadFileName, map, new MapFileFormat(MapFileFormatKind.OCAD, version));

                    // Create and open the map file.
                    map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("pdfrender")));
                    InputOutput.ReadFile(ocadFileName, map);

                    // Draw into a new bitmap.
                    bitmapNew = RenderBitmap(tempPdfFileName, map, size, mapArea, usePatternBitmaps);

                    TestUtil.CompareBitmapBaseline(bitmapNew, pngFileName, MAX_PIXEL_DIFF);

                    File.Delete(ocadFileName);
                }
            }

            return true;
        }


        void CheckTest(string filename, bool testLightenedColor, bool roundtripToOcad, int minOcadVersion, int maxOcadVersion)
        {
            string fullname = TestUtil.GetTestFile("pdfrender\\" + filename);
            bool ok = VerifyTestFile(fullname, true, testLightenedColor, roundtripToOcad, minOcadVersion, maxOcadVersion);
            Assert.IsTrue(ok, string.Format("Rendering test {0} did not compare correctly.", filename), ok);
        }

        void CheckTestNoPatternBitmaps(string filename, bool testLightenedColor, bool roundtripToOcad, int minOcadVersion, int maxOcadVersion) {
            string fullname = TestUtil.GetTestFile("pdfrender\\" + filename);
            bool ok = VerifyTestFile(fullname, false, testLightenedColor, roundtripToOcad, minOcadVersion, maxOcadVersion);
            Assert.IsTrue(ok, string.Format("Rendering test {0} did not compare correctly.", filename), ok);
        }

        [Test]
        public void KernTextOutline()
        {
            CheckTest("kern_text_outline.txt", false, false, 9, 9);
        }

        [Test]
        public void TestWest() {
            CheckTest("teanwest.txt", false, false, 9, 10);
        }

        [Test]
        public void TestWest11()
        {
            CheckTest("teanwest11.txt", false, true, 11, 11);
        }

        [Test]
        public void LineSymbols()
        {
            CheckTest("isomlines.txt", true, true, 6, 10);
            CheckTest("isomlines9.txt", true, true, 6, 10);
        }

        [Test]
        public void Fences()
        {
            CheckTest("fences.txt", false, true, 6, 10);
            CheckTest("fences9.txt", false, true, 6, 10);
        }

        [Test]
        public void FramingLines()
        {
            CheckTest("framingline-test.txt", false, true, 6, 10);
            CheckTest("framingline-test9.txt", false, true, 6, 10);
        }

        [Test]
        public void DashLines()
        {
            CheckTest("dashline.txt", false, true, 6, 10);
            CheckTest("dashline9.txt", false, true, 6, 10);
        }

        [Test]
        public void PointSymbols()
        {
            CheckTest("isompoints.txt", true, true, 6, 10);
            CheckTest("isompoints_9.txt", false, true, 6, 10);
        }

        [Test]
        public void AreaSymbols()
        {
            CheckTest("isomarea.txt", true, true, 6, 10);
            CheckTest("isomarea9.txt", true, true, 6, 10);
        }

        [Test]
        public void AreaHoles()
        {
            CheckTest("holes.txt", false, true, 6, 10);
            CheckTest("holes9.txt", false, true, 6, 10);
        }

        [Test]
        public void CutCircles()
        {
            CheckTest("cutcircles.txt", false, true, 6, 10);
            // CheckTest("cutcircles9.txt", false, false, 6);    OCAD 9 has some strange problems with cut circles...
        }

        [Test]
        public void HiddenSymbols()
        {
            CheckTest("hiddensymbols.txt", false, true, 6, 10);
            CheckTest("hiddensymbols9.txt", false, true, 6, 10);
        }
    

        [Test]
        public void RotatedAreas()
        {
            CheckTest("rotarea-test.txt", false, true, 6, 10);
            CheckTest("rotarea-test9.txt", false, true, 6, 10);
        }

        [Test]
        public void BorderedAreas()
        {
            CheckTest("borderedarea9.txt", false, true, 9, 10);
        }
    

        [Test]
        public void TextSymbols()
        {
            CheckTest("simpletext.txt", true, true, 6, 10);
            CheckTest("simpletext9.txt", true, true, 6, 10);
        }

        [Test]
        public void PunchBox()
        {
            //CheckTest("punchbox.txt", false, false, 6, 10);
            CheckTest("punchbox9.txt", false, false, 6, 10);
        }

        [Test]
        public void LakeSammMap()
        {
            CheckTest("lksamm1.txt", false, true, 6, 10);
            // CheckTest("lksamm2.txt", true, true, 6);   // this one has very slight rendering differences each time. odd...
            CheckTest("lksamm3.txt", false, true, 6, 10);
            CheckTest("lksamm4.txt", false, true, 6, 10);
        }

        [Test]
        public void LakeSammMap9()
        {
            CheckTest("lksamm9_1.txt", false, false, 6, 10);
            CheckTest("lksamm9_1.txt", false, false, 6, 10);
            // CheckTest("lksamm9_2.txt", true, false, 6);  // this one has very slight rendering differences each time. odd...
            CheckTest("lksamm9_3.txt", false, false, 6, 10);
            CheckTest("lksamm9_4.txt", false, false, 6, 10);
        }

        [Test]
        public void DeletedItems()
        {
            CheckTest("deleteditems.txt", false, true, 6, 10);
        }

        [Test]
        public void CornersAndEnds()
        {
            CheckTest("corner_ends.txt", false, true, 6, 10);
            CheckTest("corner_ends9.txt", false, true, 6, 10);
        }

        [Test]
        public void GlyphHoles()
        {
            CheckTest("glyphholes.txt", false, true, 6, 10);
            CheckTest("glyphholes9.txt", false, true, 6, 10);
        }

        [Test]
        public void ZeroGlyph()
        {
            CheckTest("zeroglyph9.txt", false, true, 6, 10);
            CheckTest("zeroglyph6.txt", false, true, 6, 10);
        }

        [Test]
        public void OffsetAreaPattern() {
            CheckTest("offsetpattern.txt", false, true, 6, 10);
        }

        [Test]
        public void OffsetAreaPatternNoBitmap() {
            CheckTestNoPatternBitmaps("offsetpattern_nopatbm.txt", false, true, 6, 10);
        }

        [Test]
        public void OffsetAreaPatternRotated() {
            CheckTest("offsetpatternrot.txt", false, true, 6, 10);
        }

        [Test]
        public void OffsetAreaPatternRotated2() {
            CheckTest("offsetpatternrot2.txt", false, true, 6, 10);
        }

        [Test]
        public void OffsetAreaPatternRotatedNoBitmap() {
            CheckTestNoPatternBitmaps("offsetpatternrot_nopatbm.txt", false, true, 6, 10);
        }

        [Test]
        public void OffsetAreaPatternRotated2NoBitmap() {
            CheckTestNoPatternBitmaps("offsetpatternrot2_nopatbm.txt", false, true, 6, 10);
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
            CheckTest("paraspacing.txt", false, true, 6, 10);
            CheckTest("paraspacing9.txt", false, true, 6, 10);
        }

        [Test]
        public void ParaIdent()
        {
            CheckTest("paraindent9.txt", false, true, 6, 10);
            CheckTest("paraindent6.txt", false, true, 6, 10);
        }

        [Test]
        public void NarrowWrap()
        {
            CheckTest("textnarrowwrap.txt", false, true, 6, 10);
        }

        [Test]
        public void CharSpace()
        {
            CheckTest("charspace.txt", false, true, 6, 10);
        }

        [Test]
        public void WordSpace()
        {
            CheckTest("wordspace.txt", false, true, 6, 10);
        }

        [Test]
        public void ComboSpace()
        {
            CheckTest("combospace.txt", false, true, 6, 10);
        }

        [Test]
        public void TopAlignText()
        {
            CheckTest("topaligntext10.txt", false, true, 10, 10);
        }

        [Test]
        public void MidAlignText() {
            CheckTest("midaligntext10.txt", false, true, 10, 10);
        }

        [Test]
        public void CenterPointText() {
            CheckTest("textpoint10.txt", false, false, 10, 10);
        }

        [Test]
        public void Justify()
        {
            CheckTest("justify.txt", false, true, 6, 10);
        }

        [Test]
        public void TabbedText()
        {
            CheckTest("tabbedtext.txt", false, true, 6, 10);
        }

        [Test]
        public void Newlines()
        {
            CheckTest("newlines.txt", false, true, 6, 10);
        }

        [Test]
        public void UnderlineText()
        {
            CheckTest("underlinetext.txt", false, true, 6, 10);
        }

        [Test]
        public void LineText1()
        {
            CheckTest("linetext_6.txt", false, true, 6, 10);
            CheckTest("linetext_9.txt", false, true, 6, 10);   
        }

        [Test]
        public void LineText2()
        {
            CheckTest("linetext2_6.txt", false, true, 6, 10);
            CheckTest("linetext2_9.txt", false, true, 6, 10);
        }

        [Test]
        public void LineTextSpacing()
        {
            CheckTest("linetextspacing.txt", false, true, 6, 10);
        }

        [Test]
        public void AllLineText()
        {
            CheckTest("alllinetext.txt", false, true, 6, 10);
        }

        [Test]
        public void LineTextTop() {
            CheckTest("linetext_top.txt", false, true, 10, 10);
        }

        [Test]
        public void LineTextTop2() {
            CheckTest("linetext2_top.txt", false, true, 10, 10);
        }

        [Test]
        public void LineTextMid() {
            CheckTest("linetext_mid.txt", false, true, 10, 10);
        }

        [Test]
        public void LineTextMid2() {
            CheckTest("linetext2_mid.txt", false, true, 10, 10);
        }

        [Test]
        public void FramingText1()
        {
            CheckTest("frametext1.txt", false, true, 7, 10);
        }

        [Test]
        public void FramingText2()
        {
            CheckTest("frametext2.txt", false, true, 9, 10);
        }

        [Test]
        public void FramingText3()
        {
            CheckTest("frametext3.txt", false, true, 9, 10);
            CheckTest("frametext3.txt", false, true, 6, 7);
        }

        [Test]
        public void Framing_Ocad6()
        {
            // Not supported in OCAD 8!!! (OCAD 8 didn't have font framing or offset framing
            CheckTest("framing_ocad6.txt", false, true, 6, 7);
            CheckTest("framing_ocad6.txt", false, true, 9, 10);
        }

        [Test]
        public void Framing_Ocad7()
        {
            // Not supported in OCAD 6 or 8!!! (OCAD 8 didn't have font framing or offset framing, OCAD 6 didn't have line framing).
            CheckTest("framing_ocad7.txt", false, true, 7, 7);
            CheckTest("framing_ocad7.txt", false, true, 9, 10);
        }

        [Test]
        public void Framing_Ocad8()
        {
            CheckTest("framing_ocad8.txt", false, true, 7, 10);
        }

        [Test]
        public void DoubleLines()
        {
            CheckTest("doublelines9.txt", false, true, 6, 10);
            CheckTest("doublelines6.txt", false, true, 6, 10);
        }

        [Test]
        public void CutDoubleSides()
        {
            CheckTest("cutdoublesides9.txt", false, true, 6, 10);
            CheckTest("cutdoublesides6.txt", false, true, 6, 10);
        }

        [Test]
        public void CutAreaBorder()
        {
            CheckTest("cutareaborder9.txt", false, true, 9, 10);
        }

        [Test]
        public void LineGaps10() {
            CheckTest("linegaps10.txt", false, true, 10, 10);
        }

        [Test]
        public void DashLengths()
        {
            CheckTest("dashlengths9.txt", false, true, 6, 10);
            CheckTest("dashlengths6.txt", false, true, 6, 10);
        }

        [Test]
        public void AngleDashes()
        {
            CheckTest("angledashes9.txt", false, true, 6, 10);
        }

        [Test]
        public void DoubleDashLengths()
        {
            CheckTest("dbldashlengths9.txt", false, true, 7, 10);  // OCAD 6 doesn't support dash points.
            CheckTest("dbldashlengths7.txt", false, true, 7, 10);  // OCAD 6 doesn't support dash points.
        }

        [Test]
        public void SecGapOnly()
        {
            CheckTest("secgaponly9.txt", false, true, 6, 10);
            CheckTest("secgaponly6.txt", false, true, 6, 10);
        }

        [Test]
        public void PointyEnds()
        {
            CheckTest("pointyends9.txt", false, true, 6, 10);
            CheckTest("pointyends6.txt", false, true, 6, 10);
        }

        [Test]
        public void BadPointyEnds()
        {
            CheckTest("badpointyends.txt", false, true, 6, 11);
        }

        [Test]
        public void Glyphs()
        {
            CheckTest("glyphs9.txt", false, true, 6, 10);
        }

        [Test]
        public void MainGlyphs()
        {
            CheckTest("mainglyph9.txt", false, true, 7, 10);
            CheckTest("mainglyph7.txt", false, true, 7, 10);
        }

        [Test]
        public void Max2Glyphs()
        {
            CheckTest("max2glyph9.txt", false, true, 7, 10);
            CheckTest("max2glyph7.txt", false, true, 7, 10);
        }

        [Test]
        public void MultiMainGlyphs()
        {
            CheckTest("multimainglyph9.txt", false, true, 7, 10);
            CheckTest("multimainglyph7.txt", false, true, 7, 10);
        }

        [Test]
        public void SecondaryGlyphs()
        {
            CheckTest("secglyph9.txt", false, true, 7, 10);
            CheckTest("secglyph7.txt", false, true, 7, 10);
        }

        [Test]
        public void StartEndGlyph()
        {
            CheckTest("startendglyph9.txt", false, true, 7, 10);
            CheckTest("startendglyph7.txt", false, true, 7, 10);
        }

        [Test]
        public void CornerGlyphs()
        {
            CheckTest("cornerglyphs9.txt", false, true, 6, 10);
            CheckTest("cornerglyphs6.txt", false, true, 6, 10);
        }

        [Test]
        public void DecreaseSymbols()
        {
            CheckTest("decreasesymbols.txt", false, true, 6, 10);
            CheckTest("decreasesymbols6.txt", false, true, 6, 10);
        }

        [Test]
        public void GraphicsObjects()
        {
            CheckTest("graphicobjects9.txt", true, true, 9, 10);
        }

        [Test]
        public void ImageObjects()
        {
            CheckTest("aiimport.txt", true, true, 9, 10);
        }

        [Test]
        public void Clouds()
        {
            CheckTest("Clouds.txt", false, true, 7, 10);
        }

        [Test]
        public void Clouds11()
        {
            CheckTest("Clouds11.txt", false, false, 11, 11);
        }

        [Test]
        public void LordHill() {
            CheckTest("LordHill.txt", false, false, 6, 10);
        }

        [Test]
        public void MissingColor()
        {
            CheckTest("missingcolor.txt", false, false, 6, 11);
        }

        [Test]
        public void Decrease()
        {
            CheckTest("decrease.txt", false, false, 6, 11);
        }

        [Test]
        public void OddFenceEnds()
        {
            CheckTest("oddfenceends.txt", false, false, 6, 11);
        }

        [Test]
        public void Marymoor()
        {
            CheckTest("marymoor.txt", false, true, 7, 11);
        }

        [Test]
        public void Marymoor11()
        {
            CheckTest("marymoor11.txt", false, false, 11, 11);
        }

        [Test]
        public void LayoutObjects()
        {
            CheckTest("layout_objects11.txt", true, true, 11, 11);
        }

        [Test]
        public void LayoutBitmapObjects()
        {
            // Lightening doesn't work here, because it's done through map conversion.
            CheckTest("layoutbitmap11.txt", false, true, 11, 11);
        }

        [Test]
        public void PunchBoxBug()
        {
            CheckTest("punchboxbug.txt", false, true, 9, 11);
        }

        [Test]
        public void SmallRectangleSymbols()
        {
            CheckTest("rectanglesymbols.txt", false, true, 6, 11);
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
        public void TemplateRendering()
        {
            CheckTest("template.txt", false, false, 9, 11);
        }

        [Test]
        public void TemplateRendering2()
        {
            CheckTest("template2.txt", false, false, 9, 11); 
        }

        [Test]
        public void Cambria()
        {
            CheckTest("Cambria.txt", false, false, 6, 11);
        }

        [Test]
        public void CambriaBold()
        {
            CheckTest("Cambriabold.txt", false, false, 6, 11);
        }

        [Test]
        public void Roboto()
        {
            CheckTest("RobotoTest.txt", false, false, 9, 12);
        }

    }

}

