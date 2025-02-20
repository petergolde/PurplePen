using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using PurplePen.Graphics2D;
using TestingUtils;

namespace PurplePen.MapModel.Tests
{
    [TestFixture]
    public class SelectionTests
    {
        private const int MAX_PIXEL_DIFF = 20;

        Map ReadMap(string basename)
        {
            string filename = TestUtil.GetTestFile("selection\\" + basename);
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(Path.GetDirectoryName(filename)));
            InputOutput.ReadFile(filename, map);
            return map;
        }

        Symbol GetSymbolWithOcadId(Map map, string ocadID)
        {
            return (from s in map.AllSymbols where s.Definition.SymbolId == ocadID select s).First();
        }

        bool HitsEquals(SymbolHit[] x, SymbolHit[] y)
        {
            if (x.Length != y.Length)
                return false;

            for (int i = 0; i < x.Length; ++i) {
                if (x[i].symbol != y[i].symbol)
                    return false;
                if (Math.Abs(x[i].distance - y[i].distance) > 0.001F)
                    return false;
                if (x[i].layer != y[i].layer)
                    return false;
            }

            return true;
        }

        [Test]
        public void SymbolsWithinRect()
        {
            Map map = ReadMap("hittest1.ocd");
            Symbol lake, rock, sand;
            Symbol road, cliff, stream;
            Symbol control, start, equip, fodder, water, knoll, tower;
            using (map.Read()) {
                lake = GetSymbolWithOcadId(map, "301.0");
                rock = GetSymbolWithOcadId(map, "212.0");
                sand = GetSymbolWithOcadId(map, "211.0");
                control = GetSymbolWithOcadId(map, "702.0");
                start = GetSymbolWithOcadId(map, "701.0");
                equip = GetSymbolWithOcadId(map, "810.2");
                fodder = GetSymbolWithOcadId(map, "538.0");
                water = GetSymbolWithOcadId(map, "314.0");
                knoll = GetSymbolWithOcadId(map, "113.0");
                tower = GetSymbolWithOcadId(map, "535.0");
                road = GetSymbolWithOcadId(map, "502.0");
                cliff = GetSymbolWithOcadId(map, "201.0");
                stream = GetSymbolWithOcadId(map, "308.0");
            }

            Symbol[] result;

            using (map.Read()) {
                result = map.SymbolsWithinRect(Geometry.RectFromPoints(-38, 40, 17, -50));
            }

            Assert.That(result, Is.EquivalentTo(new Symbol[] { }));

            using (map.Read()) {
                result = map.SymbolsWithinRect(Geometry.RectFromPoints(-33, 29, 36, -43));
            }
            Assert.That(result, Is.EquivalentTo(new Symbol[] { sand }));


            using (map.Read()) {
                result = map.SymbolsWithinRect(Geometry.RectFromPoints(50, 43, 92, 2));
            }
            Assert.That(result, Is.EquivalentTo(new Symbol[] { cliff, stream}));

            using (map.Read()) {
                result = map.SymbolsWithinRect(Geometry.RectFromPoints(58,31, 88, -14));
            }
            Assert.That(result, Is.EquivalentTo(new Symbol[] { road, control, start, fodder, water }));
        }

        public static readonly MapHitTestOptions defaultHitTestOptions = new MapHitTestOptions();

        [Test]
        public void HitTestArea()
        {
            SymbolHit[] result, expected;
            Map map = ReadMap("hittest1.ocd");
            Symbol lake, rock, sand;
            using (map.Read()) {
                lake = GetSymbolWithOcadId(map, "301.0");
                rock = GetSymbolWithOcadId(map, "212.0");
                sand = GetSymbolWithOcadId(map, "211.0");
            }

            using (map.Read())
                result = map.HitTest(new System.Drawing.PointF(-16, 23), 1.5F, defaultHitTestOptions);

            expected = new SymbolHit[] {
                new SymbolHit() { symbol = sand, distance = 0, layer = 9},
                new SymbolHit() { symbol = lake, distance = 0, layer = 16},
                new SymbolHit() { symbol = rock, distance = 0, layer = 27},
            };
            Assert.That(result, Is.EqualTo(expected));

            using (map.Read())
                result = map.HitTest(new System.Drawing.PointF(15.3F, 17.3F), 1.5F, defaultHitTestOptions);

            expected = new SymbolHit[] {
                new SymbolHit() { symbol = lake, distance = 0, layer = 16},
                new SymbolHit() { symbol = sand, distance = 0.878367841F, layer = 9},
            };
            Assert.True(HitsEquals(result, expected));

            using (map.Read())
                result = map.HitTest(new System.Drawing.PointF(-42, 15), 1.5F, defaultHitTestOptions);
            expected = new SymbolHit[] {
                new SymbolHit() { symbol = rock, distance = 0.5514185F, layer = 27},
            };
            Assert.True(HitsEquals(result, expected));

            using (map.Read())
                result = map.HitTest(new System.Drawing.PointF(27, 21), 1.5F, defaultHitTestOptions);
            expected = new SymbolHit[] {
                new SymbolHit() { symbol = lake, distance = 0.9172646F, layer = 16},
            };
            Assert.True(HitsEquals(result, expected));

            using (map.Read())
                result = map.HitTest(new System.Drawing.PointF(38, -35), 1.5F, defaultHitTestOptions);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void HitTestLine()
        {
            SymbolHit[] result, expected;
            Map map = ReadMap("hittest1.ocd");
            Symbol lake, road, cliff, stream;
            using (map.Read()) {
                lake = GetSymbolWithOcadId(map, "301.0");
                road = GetSymbolWithOcadId(map, "502.0");
                cliff = GetSymbolWithOcadId(map, "201.0");
                stream = GetSymbolWithOcadId(map, "308.0");
            }

            using (map.Read())
                result = map.HitTest(new System.Drawing.PointF(72.16F, 10.62F), 1.5F, defaultHitTestOptions);

            expected = new SymbolHit[] {
                new SymbolHit() { symbol = cliff, distance = 0, layer = 9},
                new SymbolHit() { symbol = road, distance = 0, layer = 11},
                new SymbolHit() { symbol = stream, distance = 0, layer = 15},
            };
            Assert.That(result, Is.EqualTo(expected));

            using (map.Read())
                result = map.HitTest(new System.Drawing.PointF(85.53F, 24.22F), 1.5F, defaultHitTestOptions);

            expected = new SymbolHit[] {
                new SymbolHit() { symbol = stream, distance = 0.364362061F, layer = 15},
                new SymbolHit() { symbol = cliff, distance = 0.4240952F, layer = 9},
            };
            Assert.That(result, Is.EqualTo(expected));

            using (map.Read())
                result = map.HitTest(new System.Drawing.PointF(61F, 20.4F), 1.5F, defaultHitTestOptions);

            expected = new SymbolHit[] {
                new SymbolHit() { symbol = road, distance = 0, layer = 11},
                new SymbolHit() { symbol = lake, distance = 0, layer = 16},
            };
            Assert.That(result, Is.EqualTo(expected));

            using (map.Read())
                result = map.HitTest(new System.Drawing.PointF(68, 25), 1.5F, defaultHitTestOptions);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void HitTestPoint()
        {
            SymbolHit[] result, expected;
            Map map = ReadMap("hittest1.ocd");
            Symbol lake, control, start, equip, fodder, water, knoll, tower, road;
            using (map.Read()) {
                lake    = GetSymbolWithOcadId(map, "301.0");
                control = GetSymbolWithOcadId(map, "702.0");
                start   = GetSymbolWithOcadId(map, "701.0");
                equip   = GetSymbolWithOcadId(map, "810.2");
                fodder  = GetSymbolWithOcadId(map, "538.0");
                water   = GetSymbolWithOcadId(map, "314.0");
                knoll   = GetSymbolWithOcadId(map, "113.0");
                tower   = GetSymbolWithOcadId(map, "535.0");
                road    = GetSymbolWithOcadId(map, "502.0");
            }

            using (map.Read())
                result = map.HitTest(new System.Drawing.PointF(59.3F, -12.7F), 1.5F, defaultHitTestOptions);

            expected = new SymbolHit[] {
                new SymbolHit() { symbol = knoll, distance = 0.291519523F, layer = 1},
                new SymbolHit() { symbol = lake, distance = 0, layer = 16},
            };
            Assert.IsTrue(HitsEquals(result, expected));

            using (map.Read())
                result = map.HitTest(new System.Drawing.PointF(69.91F, -14.91F), 1.5F, defaultHitTestOptions);

            expected = new SymbolHit[] {
                new SymbolHit() { symbol = tower, distance = 0.13F, layer = 9},
                new SymbolHit() { symbol = equip, distance = 0.3800638F, layer = 2},
            };
            Assert.IsTrue(HitsEquals(result, expected));

            using (map.Read())
                result = map.HitTest(new System.Drawing.PointF(71.12F, -8.48F), 1.5F, defaultHitTestOptions);

            expected = new SymbolHit[] {
                new SymbolHit() { symbol = start, distance = 0.178307831F, layer = 2},
                new SymbolHit() { symbol = fodder, distance = 0.204106F, layer = 9},
                new SymbolHit() { symbol = control, distance = 1.03765488F, layer = 2},
            };
            Assert.IsTrue(HitsEquals(result, expected));

            using (map.Read())
                result = map.HitTest(new System.Drawing.PointF(68.48F, 1.80F), 1.5F, defaultHitTestOptions);

            expected = new SymbolHit[] {
                new SymbolHit() { symbol = road, distance = 0, layer = 11},
                new SymbolHit() { symbol = control, distance = 0.400619984F, layer = 2},
            };
            Assert.IsTrue(HitsEquals(result, expected));

            using (map.Read())
                result = map.HitTest(new System.Drawing.PointF(68.4F, 1.12F), 1.5F, defaultHitTestOptions);

            expected = new SymbolHit[] {
                new SymbolHit() { symbol = control, distance = 0, layer = 2},
                new SymbolHit() { symbol = road, distance = 0, layer = 11},
            };
            Assert.IsTrue(HitsEquals(result, expected));

            using (map.Read())
                result = map.HitTest(new System.Drawing.PointF(68.4F, -3.44F), 3F, defaultHitTestOptions);
            Assert.That(result, Is.Null);
        }


        static Bitmap RenderBitmap(Map map, Size bitmapSize, RectangleF mapArea, Action<IGraphicsTarget, Map, RectangleF, float> draw)
        {
            // Calculate the transform matrix.
            PointF midpoint = new PointF(bitmapSize.Width / 2.0F, bitmapSize.Height / 2.0F);
            float scaleFactor = (float)bitmapSize.Width / mapArea.Width;
            PointF centerPoint = new PointF((mapArea.Left + mapArea.Right) / 2, (mapArea.Top + mapArea.Bottom) / 2);
            Matrix matrix = new Matrix();
            matrix.Translate(midpoint.X, midpoint.Y, MatrixOrder.Prepend);
            matrix.Scale(scaleFactor, -scaleFactor, MatrixOrder.Prepend);  // y scale is negative to get to cartesian orientation.
            matrix.Translate(-centerPoint.X, -centerPoint.Y, MatrixOrder.Prepend);

            // Draw into a new bitmap.
            Bitmap bitmapNew = new Bitmap(bitmapSize.Width, bitmapSize.Height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(bitmapNew)) {
                g.Clear(Color.White);
                g.Transform = matrix;

                float pixelSize = mapArea.Width / (float)bitmapSize.Width;

                draw(new GDIPlus_GraphicsTarget(g), map, mapArea, pixelSize);
            }

            return bitmapNew;
        }

        static void DrawMapAndSelectionHighFidelity(IGraphicsTarget grTarget, Map map, RectangleF rect, float pixelSize)
        {
            DrawMapAndSelection(grTarget, map, rect, pixelSize, HighlightStyle.HighFidelity);
        }
        static void DrawMapAndSelectionLowFidelity(IGraphicsTarget grTarget, Map map, RectangleF rect, float pixelSize)
        {
            DrawMapAndSelection(grTarget, map, rect, pixelSize, HighlightStyle.LowFidelity);
        }


        static void DrawMapAndSelection(IGraphicsTarget grTarget, Map map, RectangleF rect, float pixelSize, HighlightStyle fidelity)
        {
            RenderOptions renderOpts = new RenderOptions();
            renderOpts.usePatternBitmaps = true;
            renderOpts.renderTemplates = RenderTemplateOption.MapAndTemplates;
            renderOpts.minResolution = pixelSize;
            HighlightOptions highlightOpts = new HighlightOptions();
            highlightOpts.minResolution = pixelSize;
            highlightOpts.logicalPixelSize = 2 * pixelSize;
            highlightOpts.style = fidelity;

            object bluePen = new object();
            grTarget.CreatePen(bluePen, CmykColor.FromRgb(0, 0, 1), pixelSize, LineCap.Flat, LineJoin.Miter, 5);

            using (map.Read()) {
                map.Draw(grTarget, rect, renderOpts, null);
                map.DrawHighlightedSymbols(grTarget, map.AllSymbols, rect, highlightOpts, CancellationToken.None);

                foreach (Symbol sym in map.AllSymbols) {
                    grTarget.DrawRectangle(bluePen, sym.HighlightBounds(highlightOpts));
                }
            }
        }

        static void HitTestMap(IGraphicsTarget grTarget, Map map, RectangleF rect, float pixelSize)
        {
            HitTestMapWithOptions(grTarget, map, rect, pixelSize, defaultHitTestOptions);
        }

        static void HitTestMapAreaBordersOnly(IGraphicsTarget grTarget, Map map, RectangleF rect, float pixelSize)
        {
            HitTestMapWithOptions(grTarget, map, rect, pixelSize, new MapHitTestOptions() { AreaBordersOnly = true });
        }

        static void HitTestMapAreaHoleInteriors(IGraphicsTarget grTarget, Map map, RectangleF rect, float pixelSize)
        {
            HitTestMapWithOptions(grTarget, map, rect, pixelSize, new MapHitTestOptions() { AreaIncludeHoleInteriors = true });
        }

        static void HitTestMapWithOptions(IGraphicsTarget grTarget, Map map, RectangleF rect, float pixelSize, MapHitTestOptions hitTestOptions)
        {
            RenderOptions renderOpts = new RenderOptions();
            renderOpts.usePatternBitmaps = true;
            renderOpts.renderTemplates = RenderTemplateOption.MapAndTemplates;
            renderOpts.minResolution = pixelSize;

            object fullRed = new object();
            grTarget.CreateSolidBrush(fullRed, CmykColor.FromRgb(1, 0, 0));

            using (map.Read()) {
                map.Draw(grTarget, rect, renderOpts, null);

                for (float x = rect.Left; x < rect.Right; x += pixelSize * 2) {
                    for (float y = rect.Top; y < rect.Bottom; y += pixelSize * 2) {
                        RectangleF pixel = new RectangleF(x, y, pixelSize, pixelSize);
                        PointF center = pixel.Center();

                        SymbolHit[] hits = map.HitTest(center, pixelSize * 6, hitTestOptions);
                        if (hits != null && hits.Length > 0) {
                            object brush;
                            if (hits[0].distance == 0)
                                brush = fullRed;
                            else {
                                brush = new object();

                                float intensity = 1F - (hits[0].distance / (pixelSize * 6));
                                grTarget.CreateSolidBrush(brush, CmykColor.FromRgba(1F, 0, 0, intensity));
                            }

                            grTarget.FillRectangle(brush, pixel);
                        }
                    }
                }
            }
        }

        // Verifies a test file. Returns true on success, false on failure. In the failure case, 
        // a difference bitmap is written out.
        static bool VerifyTestFile(string filename, Action<IGraphicsTarget, Map, RectangleF, float> draw)
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

            // Draw into a new bitmap.
            Bitmap bitmapNew = RenderBitmap(map, size, mapArea, draw);

            TestUtil.CompareBitmapBaseline(bitmapNew, pngFileName, MAX_PIXEL_DIFF);

            return true;
        }


        void CheckTest(string filename, Action<IGraphicsTarget, Map, RectangleF, float> draw)
        {
            string fullname = TestUtil.GetTestFile("selection\\" + filename);
            bool ok = VerifyTestFile(fullname, draw);
            Assert.IsTrue(ok, string.Format("Rendering test {0} did not compare correctly.", filename), ok);
        }

        [Test]
        public void HighlightPointSymbols()
        {
            CheckTest("selectpointsymbols.txt", DrawMapAndSelectionHighFidelity);
        }

        [Test]
        public void HighlightSmallPointSymbolsLoFi()
        {
            CheckTest("selectsmallpointsymbolslofi.txt", DrawMapAndSelectionLowFidelity);
        }

        [Test]
        public void HighlightSmallPointSymbolsHiFi()
        {
            CheckTest("selectsmallpointsymbols.txt", DrawMapAndSelectionHighFidelity);
        }

        [Test]
        public void HighlightLineSymbolsLoFi()
        {
            CheckTest("selectlinesymbolslofi.txt", DrawMapAndSelectionLowFidelity);
        }

        [Test]
        public void HighlightLineSymbolsHiFi()
        {
            CheckTest("selectlinesymbolshifi.txt", DrawMapAndSelectionHighFidelity);
        }

        [Test]
        public void HighlightAreaSymbols()
        {
            CheckTest("selectareasymbols.txt", DrawMapAndSelectionHighFidelity);
        }

        [Test]
        public void HighlightImageSymbols()
        {
            CheckTest("selectimagesymbols.txt", DrawMapAndSelectionHighFidelity);
        }

        [Test]
        public void HighlightTopAlignTextSymbols()
        {
            CheckTest("selecttopaligntextsymbols.txt", DrawMapAndSelectionHighFidelity);
        }

        [Test]
        public void HighlightMidAlignTextSymbols()
        {
            CheckTest("selectmidaligntextsymbols.txt", DrawMapAndSelectionHighFidelity);
        }

        [Test]
        public void HighlightBottomAlignTextSymbols()
        {
            CheckTest("selectbottomaligntextsymbols.txt", DrawMapAndSelectionHighFidelity);
        }

        [Test]
        public void HighlightJustifiedTextSymbols()
        {
            CheckTest("selectjustifytextsymbols.txt", DrawMapAndSelectionHighFidelity);
        }

        [Test]
        public void HighlightLineTextSymbols()
        {
            CheckTest("selectlinetext.txt", DrawMapAndSelectionHighFidelity);
        }

        [Test]
        public void HighlightLayoutBitmapObjects()
        {
            CheckTest("selectlayoutbitmap.txt", DrawMapAndSelectionHighFidelity);
        }

        [Test]
        public void HighlightLayoutObjects()
        {
            CheckTest("selectlayoutobjects.txt", DrawMapAndSelectionHighFidelity);
        }

        [Test]
        public void HighlightRectangleObjects()
        {
            CheckTest("selectpunchbox.txt", DrawMapAndSelectionHighFidelity);
        }

        [Test]
        public void HitTestPointSymbols()
        {
            CheckTest("hittestpointsymbols.txt", HitTestMap);
        }

        [Test]
        public void HitTestLineSymbols()
        {
            CheckTest("hittestlinesymbols.txt", HitTestMap);
        }

        [Test]
        public void HitTestAreaSymbols()
        {
            CheckTest("hittestareasymbols.txt", HitTestMap);
        }

        [Test]
        public void HitTestAreaSymbolsBorder()
        {
            CheckTest("hittestareasymbolsborder.txt", HitTestMapAreaBordersOnly);
        }

        [Test]
        public void HitTestAreaSymbolsHoleInterior()
        {
            CheckTest("hittestareasymbolsholeinterior.txt", HitTestMapAreaHoleInteriors);
        }

        [Test]
        public void HitTestImageSymbols()
        {
            CheckTest("hittestimagesymbols.txt", HitTestMap);
        }

        [Test]
        public void HitTestTopAlignTextSymbols()
        {
            CheckTest("hittesttopaligntextsymbols.txt", HitTestMap);
        }


        [Test]
        public void HitTestMidAlignTextSymbols()
        {
            CheckTest("hittestmidaligntextsymbols.txt", HitTestMap);
        }

        [Test]
        public void HitTestBottomAlignTextSymbols()
        {
            CheckTest("hittestbottomaligntextsymbols.txt", HitTestMap);
        }

        [Test]
        public void HitTestJustifyTextSymbols()
        {
            CheckTest("hittestjustifytextsymbols.txt", HitTestMap);
        }


        [Test]
        public void HitTestLineTextSymbols()
        {
            CheckTest("hittestlinetext.txt", HitTestMap);
        }


        [Test]
        public void HitTestLineTextTopSymbols()
        {
            CheckTest("hittestlinetexttop.txt", HitTestMap);
        }


        [Test]
        public void HitTestLineTextMidSymbols()
        {
            CheckTest("hittestlinetextmid.txt", HitTestMap);
        }

        [Test]
        public void HitTestLayoutBitmapObjects()
        {
            CheckTest("hittestlayoutbitmap.txt", HitTestMap);
        }


        [Test]
        public void HitTestLayoutObjects()
        {
            CheckTest("hittestlayoutobjects.txt", HitTestMap);
        }

        [Test]
        public void HitTestRectangleObjects()
        {
            CheckTest("hittestpunchbox.txt", HitTestMap);
        }


    }
}
