using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using PurplePen.MapModel;
using PurplePen.Graphics2D;

using TestingUtils;
using RenderOptions = PurplePen.MapModel.RenderOptions;
using RectangleF = System.Drawing.RectangleF;
using PointF = System.Drawing.PointF;

namespace TestWpfMap
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class RenderingTests
    {
        private const int MAX_PIXEL_DIFFERENCE = 30;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        // Write a bitmap to a PNG.
        static void WritePng(BitmapSource bmp, string filename)
        {
            using (FileStream stream = new FileStream(filename, FileMode.Create)) {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                encoder.Save(stream);
            }
        }

        void WPF_RenderingTest(int width, RectangleF drawingRectangle, string pngFileName, Action<IGraphicsTarget> draw)
        {
            int height = (int)Math.Ceiling(width * drawingRectangle.Height / drawingRectangle.Width);

            // Calculate the transform matrix.
            PointF midpoint = new PointF(width / 2.0F, height / 2.0F);
            float scaleFactor = (float)width / drawingRectangle.Width;
            PointF centerPoint = new PointF((drawingRectangle.Left + drawingRectangle.Right) / 2, (drawingRectangle.Top + drawingRectangle.Bottom) / 2);
            Matrix matrix = Matrix.Identity;
            matrix.TranslatePrepend(midpoint.X, midpoint.Y);
            matrix.ScalePrepend(scaleFactor, -scaleFactor);  // y scale is negative to get to cartesian orientation.
            matrix.TranslatePrepend(-centerPoint.X, -centerPoint.Y);

            // Create a drawing of the map.
            DrawingVisual visual = new DrawingVisual();
            TextOptions.SetTextRenderingMode(visual, TextRenderingMode.Aliased);
            DrawingContext dc = visual.RenderOpen();

            // Clear the bitmap
            dc.DrawRectangle(Brushes.White, null, new Rect(-1, -1, width + 2, height + 2));  // clear background.

            // Transform coords.
            dc.PushTransform(new MatrixTransform(matrix));

            // Draw
            draw(new WPF_GraphicsTarget(dc));
            dc.Close();

            // Draw into a new bitmap.
            RenderTargetBitmap bitmapNew = new RenderTargetBitmap(width, height, 96.0, 96.0, PixelFormats.Pbgra32);
            TextOptions.SetTextRenderingMode(bitmapNew, TextRenderingMode.Aliased);
            TextOptions.SetTextRenderingMode(visual, TextRenderingMode.Aliased);
            bitmapNew.Render(visual);
            bitmapNew.Freeze();

            string directoryName = Path.GetDirectoryName(pngFileName);
            string newBitmapName = Path.Combine(directoryName,
                                        Path.GetFileNameWithoutExtension(pngFileName) + "_new.png");
            File.Delete(newBitmapName);
            WritePng(bitmapNew, newBitmapName);

            TestUtil.CompareBitmapBaseline(newBitmapName, pngFileName, MAX_PIXEL_DIFFERENCE);
        }


        // Render part of a map to a bitmap.
        static BitmapSource RenderBitmap(Map map, int bmWidth, int bmHeight, RectangleF mapArea)
        {
            // Calculate the transform matrix.
            Point midpoint = new Point(bmWidth / 2.0F, bmHeight / 2.0F);
            double scaleFactor = bmWidth / mapArea.Width;
            PointF centerPoint = new PointF((mapArea.Left + mapArea.Right) / 2, (mapArea.Top + mapArea.Bottom) / 2);
            Matrix matrix = Matrix.Identity;
            matrix.TranslatePrepend(midpoint.X, midpoint.Y);
            matrix.ScalePrepend(scaleFactor, -scaleFactor);  // y scale is negative to get to cartesian orientation.
            matrix.TranslatePrepend(-centerPoint.X, -centerPoint.Y);

            // Get the render options.
            RenderOptions renderOpts = new RenderOptions();
            renderOpts.usePatternBitmaps = false;
            renderOpts.renderTemplates = RenderTemplateOption.MapAndTemplates;
            renderOpts.minResolution = (float) (1 / scaleFactor);

            // Create a drawing of the map.
            DrawingVisual visual = new DrawingVisual();
            TextOptions.SetTextRenderingMode(visual, TextRenderingMode.Aliased);
            DrawingContext dc = visual.RenderOpen();

            // Clear the bitmap
            dc.DrawRectangle(Brushes.White, null, new Rect(-1, -1, bmWidth + 2, bmHeight + 2));  // clear background.

            // Transform to map coords.
            dc.PushTransform(new MatrixTransform(matrix));

            // Draw the map.
            using (map.Read())
                map.Draw(new WPF_GraphicsTarget(dc), mapArea, renderOpts, null);
            dc.Close();

            // Draw into a new bitmap.
            RenderTargetBitmap bitmapNew = new RenderTargetBitmap(bmWidth, bmHeight, 96.0, 96.0, PixelFormats.Pbgra32);
            TextOptions.SetTextRenderingMode(bitmapNew, TextRenderingMode.Aliased);
            TextOptions.SetTextRenderingMode(visual, TextRenderingMode.Aliased);
            bitmapNew.Render(visual);
            bitmapNew.Freeze();

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
            string newBitmapName;
            RectangleF mapArea;
            int bmWidth, bmHeight;

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
                bmWidth = int.Parse(coords[0]);
                bmHeight = int.Parse(coords[1]);
            }

            // Convert to absolute paths.
            directoryName = Path.GetDirectoryName(filename);
            mapFileName = Path.Combine(directoryName, mapFileName);
            pngFileName = Path.Combine(directoryName, pngFileName);
            geodeFileName = Path.Combine(directoryName,
                                         Path.GetFileNameWithoutExtension(mapFileName) + "_temp.geode");
            ocadFileName = Path.Combine(directoryName,
                                         Path.GetFileNameWithoutExtension(mapFileName) + "_temp.ocd");
            newBitmapName = Path.Combine(directoryName,
                                        Path.GetFileNameWithoutExtension(pngFileName) + "_new.png");

            File.Delete(geodeFileName);
            File.Delete(ocadFileName);
            File.Delete(newBitmapName);

            // Create and open the map file.
            Map map = new Map(new WPF_TextMetrics(), new GDIPlus_FileLoader(directoryName));
            InputOutput.ReadFile(mapFileName, map);

            // Draw into a new bitmap.
            Stopwatch sw = new Stopwatch();
            sw.Start();

            BitmapSource bitmapNew = RenderBitmap(map, bmWidth, bmHeight, mapArea);

            sw.Stop();
            Console.WriteLine("Rendered bitmap '{0}' rect={1} size=({2},{3}) in {4} ms", mapFileName, mapArea, bmWidth, bmHeight, sw.ElapsedMilliseconds);


            WritePng(bitmapNew, newBitmapName);
            TestUtil.CompareBitmapBaseline(newBitmapName, pngFileName, MAX_PIXEL_DIFFERENCE);

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
                BitmapSource bitmapLight = RenderBitmap(map, bmWidth, bmHeight, mapArea);
                WritePng(bitmapLight, newBitmapName);
                TestUtil.CompareBitmapBaseline(newBitmapName, lightenedPngFileName, MAX_PIXEL_DIFFERENCE);
            }

            if (roundtripToOcadFile) {
                for (int version = minOcadVersion; version <= maxOcadVersion; ++version) {
                    if (version == 13)
                        version = 2018;

                    // Save and load to a temp file name.
                    InputOutput.WriteFile(ocadFileName, map, new MapFileFormat(MapFileFormatKind.OCAD, version));

                    // Create and open the map file.
                    map = new Map(new WPF_TextMetrics(), new GDIPlus_FileLoader(directoryName));
                    InputOutput.ReadFile(ocadFileName, map);

                    // Draw into a new bitmap.
                    bitmapNew = RenderBitmap(map, bmWidth, bmHeight, mapArea);
                    WritePng(bitmapNew, newBitmapName);
                    TestUtil.CompareBitmapBaseline(newBitmapName, pngFileName, MAX_PIXEL_DIFFERENCE);

                    File.Delete(ocadFileName);
                }
            }

            return true;
        }

        void TimeMapRender(Map map, int width, int height, RectangleF mapArea, string name) {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Draw into a new bitmap.
            BitmapSource bitmapNew = RenderBitmap(map, width, height, mapArea);

            sw.Stop();
            Console.WriteLine("Rendered bitmap '{0}' in {1} ms", name, sw.ElapsedMilliseconds);
        }

        public void TimeTeanWest() {
            // Create and open the map file.
            Map map = new Map(new WPF_TextMetrics(), null);
            InputOutput.ReadFile(@"C:\Users\Peter\Documents\PurplePen\newmapmodel\src\TestFiles\d2drender\teanwest.ocd", map);

            TimeMapRender(map, 1814, 1022, new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
            TimeMapRender(map, 1814, 1022, new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
            TimeMapRender(map, 1814, 1022, new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
            TimeMapRender(map, 1814, 1022, new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
            TimeMapRender(map, 1814, 1022, new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
        }

        void CheckTest(string filename, bool testLightenedColor, bool roundtripToOcad, int minOcadVersion, int maxOcadVersion)
        {
            string fullname = TestUtil.GetTestFile("wpfrender\\" + filename);
            bool ok = VerifyTestFile(fullname, true, testLightenedColor, roundtripToOcad, minOcadVersion, maxOcadVersion);
            Assert.IsTrue(ok, string.Format("Rendering test {0} did not compare correctly.", filename));
        }

        void CheckTestNoPatternBitmaps(string filename, bool testLightenedColor, bool roundtripToOcad, int minOcadVersion, int maxOcadVersion)
        {
            string fullname = TestUtil.GetTestFile("wpfrender\\" + filename);
            bool ok = VerifyTestFile(fullname, false, testLightenedColor, roundtripToOcad, minOcadVersion, maxOcadVersion);
            Assert.IsTrue(ok, string.Format("Rendering test {0} did not compare correctly.", filename));
        }

        [TestMethod]
        public void BitmapDrawing()
        {
            WPF_RenderingTest(1000, new RectangleF(0, 0, 1000, 1000), TestUtil.GetTestFile("wpfrender\\bitmapdrawing.png"),
                        grTarget => {
                            IGraphicsBitmap bitmap = new GDIPlus_FileLoader(TestUtil.GetTestFileDirectory()).LoadBitmap(TestUtil.GetTestFile("wpfrender\\Jellyfish.jpg"), false);

                            grTarget.DrawBitmap(bitmap, new RectangleF(100, 100, 800, 400), BitmapScaling.HighQuality, 0.01F);

                            grTarget.DrawBitmapPart(bitmap, bitmap.PixelWidth * 3 / 10, bitmap.PixelHeight * 2 / 10, bitmap.PixelWidth * 5 / 10, bitmap.PixelHeight * 4 / 10,
                                                    new RectangleF(100, 500, 800, 400), BitmapScaling.NearestNeighbor, 0.01F);

                            bitmap.Dispose();
                        });

        }

        [TestMethod]
        public void LineSymbols()
        {
            CheckTest("isomlines.txt", true, true, 6, 9);
            CheckTest("isomlines9.txt", true, true, 6, 9);
        }

        [TestMethod]
        public void Fences()
        {
            CheckTest("fences.txt", false, true, 6, 9);
            CheckTest("fences9.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void FramingLines()
        {
            CheckTest("framingline-test.txt", false, true, 6, 9);
            CheckTest("framingline-test9.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void DashLines()
        {
            CheckTest("dashline.txt", false, true, 6, 9);
            CheckTest("dashline9.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void PointSymbols()
        {
            CheckTest("isompoints.txt", true, true, 6, 9);
            CheckTest("isompoints_9.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void AreaSymbols()
        {
            CheckTest("isomarea.txt", true, true, 6, 9);
            CheckTest("isomarea9.txt", true, true, 6, 9);
        }

        [TestMethod]
        public void OffsetAreaPattern()
        {
            CheckTest("offsetpattern.txt", false, true, 6, 11);
        }

        [TestMethod]
        public void OffsetAreaPatternNoBitmap()
        {
            CheckTestNoPatternBitmaps("offsetpattern_nopatbm.txt", false, true, 6, 11);
        }

        [TestMethod]
        public void OffsetAreaPatternRotated()
        {
            CheckTest("offsetpatternrot.txt", false, true, 6, 11);
        }

        [TestMethod]
        public void OffsetAreaPatternRotated2()
        {
            CheckTest("offsetpatternrot2.txt", false, true, 6, 11);
        }

        [TestMethod]
        public void OffsetAreaPatternRotatedNoBitmap()
        {
            CheckTestNoPatternBitmaps("offsetpatternrot_nopatbm.txt", false, true, 6, 11);
        }

        [TestMethod]
        public void OffsetAreaPatternRotated2NoBitmap()
        {
            CheckTestNoPatternBitmaps("offsetpatternrot2_nopatbm.txt", false, true, 6, 11);
        }


        [TestMethod]
        public void AreaSymbolsBug() {
            CheckTest("isomareabug.txt", false, false, 9, 9);
        }

        [TestMethod]
        public void AreaHoles()
        {
            CheckTest("holes.txt", false, true, 6, 9);
            CheckTest("holes9.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void CutCircles()
        {
            CheckTest("cutcircles.txt", false, true, 6, 9);
            // CheckTest("cutcircles9.txt", false, false, 6);    OCAD 9 has some strange problems with cut circles...
        }

        [TestMethod]
        public void HiddenSymbols()
        {
            CheckTest("hiddensymbols.txt", false, true, 6, 9);
            CheckTest("hiddensymbols9.txt", false, true, 6, 9);
        }


        [TestMethod]
        public void RotatedAreas()
        {
            CheckTest("rotarea-test.txt", false, true, 6, 9);
            CheckTest("rotarea-test9.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void BorderedAreas()
        {
            CheckTest("borderedarea9.txt", false, true, 9, 9);
        }


        [TestMethod]
        public void TextSymbols()
        {
            CheckTest("simpletext.txt", true, true, 6, 9);
            CheckTest("simpletext9.txt", true, true, 6, 9);
        }

        [TestMethod]
        public void PunchBox()
        {
            //CheckTest("punchbox.txt", false, false, 6, 9);
            CheckTest("punchbox9.txt", false, false, 6, 9);
        }

        [TestMethod]
        public void LakeSammMap()
        {
            CheckTest("lksamm1.txt", false, true, 6, 9);
            // CheckTest("lksamm2.txt", true, true, 6);   // this one has very slight rendering differences each time. odd...
            CheckTest("lksamm3.txt", false, true, 6, 9);
            CheckTest("lksamm4.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void LakeSammMap9()
        {
            CheckTest("lksamm9_1.txt", false, false, 6, 9);
            // CheckTest("lksamm9_2.txt", true, false, 6);  // this one has very slight rendering differences each time. odd...
            CheckTest("lksamm9_3.txt", false, false, 6, 9);
            CheckTest("lksamm9_4.txt", false, false, 6, 9);
        }

        [TestMethod]
        public void DeletedItems()
        {
            CheckTest("deleteditems.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void CornersAndEnds()
        {
            CheckTest("corner_ends.txt", false, true, 6, 9);
            CheckTest("corner_ends9.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void GlyphHoles()
        {
            CheckTest("glyphholes.txt", false, true, 6, 9);
            CheckTest("glyphholes9.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void ZeroGlyph()
        {
            CheckTest("zeroglyph9.txt", false, true, 6, 9);
            CheckTest("zeroglyph6.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void ParaSpacing()
        {
            CheckTest("paraspacing.txt", false, true, 6, 9);
            CheckTest("paraspacing9.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void ParaIdent()
        {
            CheckTest("paraindent9.txt", false, true, 6, 9);
            CheckTest("paraindent6.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void NarrowWrap()
        {
            CheckTest("textnarrowwrap.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void CharSpace()
        {
            CheckTest("charspace.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void WordSpace()
        {
            CheckTest("wordspace.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void ComboSpace()
        {
            CheckTest("combospace.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void Justify()
        {
            CheckTest("justify.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void TabbedText()
        {
            CheckTest("tabbedtext.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void UnderlineText()
        {
            CheckTest("underlinetext.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void LineText1()
        {
            CheckTest("linetext_6.txt", false, true, 6, 9);
            CheckTest("linetext_9.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void LineText2()
        {
            CheckTest("linetext2_6.txt", false, true, 6, 9);
            CheckTest("linetext2_9.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void LineTextSpacing()
        {
            CheckTest("linetextspacing.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void AllLineText()
        {
            CheckTest("alllinetext.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void FramingText1()
        {
            CheckTest("frametext1.txt", false, true, 7, 9);
        }

        [TestMethod]
        public void FramingText2()
        {
            CheckTest("frametext2.txt", false, true, 9, 9);
        }

        [TestMethod]
        public void FramingText3()
        {
            CheckTest("frametext3.txt", false, true, 9, 9);
            CheckTest("frametext3.txt", false, true, 6, 7);
        }

        [TestMethod]
        public void Framing_Ocad6()
        {
            // Not supported in OCAD 8!!! (OCAD 8 didn't have font framing or offset framing
            CheckTest("framing_ocad6.txt", false, true, 6, 7);
            CheckTest("framing_ocad6.txt", false, true, 9, 9);
        }

        [TestMethod]
        public void Framing_Ocad7()
        {
            // Not supported in OCAD 6 or 8!!! (OCAD 8 didn't have font framing or offset framing, OCAD 6 didn't have line framing).
            CheckTest("framing_ocad7.txt", false, true, 7, 7);
            CheckTest("framing_ocad7.txt", false, true, 9, 9);
        }

        [TestMethod]
        public void Framing_Ocad8()
        {
            CheckTest("framing_ocad8.txt", false, true, 7, 9);
        }

        [TestMethod]
        public void DoubleLines()
        {
            CheckTest("doublelines9.txt", false, true, 6, 9);
            CheckTest("doublelines6.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void CutDoubleSides()
        {
            CheckTest("cutdoublesides9.txt", false, true, 6, 9);
            CheckTest("cutdoublesides6.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void CutAreaBorder()
        {
            CheckTest("cutareaborder9.txt", false, true, 9, 9);
        }

        [TestMethod]
        public void DashLengths()
        {
            CheckTest("dashlengths9.txt", false, true, 6, 9);
            CheckTest("dashlengths6.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void AngleDashes()
        {
            CheckTest("angledashes9.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void DoubleDashLengths()
        {
            CheckTest("dbldashlengths9.txt", false, true, 7, 9);  // OCAD 6 doesn't support dash points.
            CheckTest("dbldashlengths7.txt", false, true, 7, 9);  // OCAD 6 doesn't support dash points.
        }

        [TestMethod]
        public void SecGapOnly()
        {
            CheckTest("secgaponly9.txt", false, true, 6, 9);
            CheckTest("secgaponly6.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void PointyEnds()
        {
            CheckTest("pointyends9.txt", false, true, 6, 9);
            CheckTest("pointyends6.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void Glyphs()
        {
            CheckTest("glyphs9.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void MainGlyphs()
        {
            CheckTest("mainglyph9.txt", false, true, 7, 9);
            CheckTest("mainglyph7.txt", false, true, 7, 9);
        }

        [TestMethod]
        public void Max2Glyphs()
        {
            CheckTest("max2glyph9.txt", false, true, 7, 9);
            CheckTest("max2glyph7.txt", false, true, 7, 9);
        }

        [TestMethod]
        public void MultiMainGlyphs()
        {
            CheckTest("multimainglyph9.txt", false, true, 7, 9);
            CheckTest("multimainglyph7.txt", false, true, 7, 9);
        }

        [TestMethod]
        public void SecondaryGlyphs()
        {
            CheckTest("secglyph9.txt", false, true, 7, 9);
            CheckTest("secglyph7.txt", false, true, 7, 9);
        }

        [TestMethod]
        public void StartEndGlyph()
        {
            CheckTest("startendglyph9.txt", false, true, 7, 9);
            CheckTest("startendglyph7.txt", false, true, 7, 9);
        }

        [TestMethod]
        public void CornerGlyphs()
        {
            CheckTest("cornerglyphs9.txt", false, true, 6, 9);
            CheckTest("cornerglyphs6.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void DecreaseSymbols()
        {
            CheckTest("decreasesymbols.txt", false, true, 6, 9);
        }

        [TestMethod]
        public void GraphicsObjects()
        {
            CheckTest("graphicobjects9.txt", true, true, 9, 9);
        }

        [TestMethod]
        public void ImageObjects()
        {
            CheckTest("aiimport.txt", true, true, 9, 9);
        }

        [TestMethod]
        public void Ijk()
        {
            CheckTest("ijk.txt", true, false, 9, 9);
        }

        [TestMethod]
        public void Tiling1()
        {
            CheckTest("tiling1.txt", false, false, 9, 9);
        }

        [TestMethod]
        public void Seam()
        {
            CheckTest("seam.txt", false, false, 9, 9);
        }

        [TestMethod]
        public void ArialNarrow()
        {
            CheckTest("arialnarrow.txt", false, false, 9, 9);
        }

        [TestMethod]
        public void LordHill() {
            CheckTest("LordHill.txt", false, false, 6, 10);
        }

        [TestMethod]
        public void LayoutObjects()
        {
            CheckTest("layout_objects11.txt", true, true, 11, 11);
        }

        [TestMethod]
        public void LayoutBitmapObjects()
        {
            // Lightening doesn't work here, because it's done through map conversion.
            CheckTest("layoutbitmap11.txt", false, true, 11, 11);
        }

        [TestMethod]
        public void MarymoorUnderline()
        {
            CheckTest("marymoor_underline.txt", false, false, 9, 9);
        }

        [TestMethod]
        public void ForteText()
        {
            CheckTest("fortetext.txt", false, false, 9, 9);
        }

        [TestMethod]
        public void TemplateRendering()
        {
            CheckTest("template.txt", false, false, 9, 11);
        }

        [TestMethod]
        public void TemplateRendering2()
        {
            CheckTest("template2.txt", false, false, 9, 11);
        }
    }
}
