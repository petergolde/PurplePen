using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;

using SkiaSharp;

namespace Map_Skia.Tests
{
    using PurplePen.Graphics2D;
    using PurplePen.MapModel;
    using TestingUtils;
    using Map_Skia;
    using System.Diagnostics;


    public static class RenderingUtil
    {

        public static void RenderingTest(int width, RectangleF drawingRectangle, bool inverted, string pngFileName, Action<IGraphicsTarget> draw)
        {
            int height = (int)Math.Ceiling(width * drawingRectangle.Height / drawingRectangle.Width);

            using (SKBitmap bitmapNew = new SKBitmap(width, height))
            using (SKCanvas skcanvas = new SKCanvas(bitmapNew)) {
                skcanvas.Clear(SKColors.White);

                using (Skia_GraphicsTarget grTarget = new Skia_GraphicsTarget(skcanvas)) {
                    grTarget.PushTransform(GetTransform(width, height, drawingRectangle, inverted));
                    draw(grTarget);
                }

                string directoryName = Path.GetDirectoryName(pngFileName);
                string newBitmapName = Path.Combine(directoryName,
                                            Path.GetFileNameWithoutExtension(pngFileName) + "_new.png");
                File.Delete(newBitmapName);

                BitmapTestUtil.CompareBitmapBaseline(bitmapNew, pngFileName);
            }
        }

        static Skia_Bitmap RenderBitmap(Map map, Size bitmapSize, RectangleF mapArea, RenderOptions renderOptions, bool usePatternBitmaps, bool useOverprinting, bool antiAlias, float intensity)
        {
            var grTarget = new Skia_BitmapGraphicsTarget(bitmapSize.Width, bitmapSize.Height, false, CmykColor.FromCmyk(0, 0, 0, 0), mapArea, true, null, intensity);
            using (grTarget) {
                grTarget.PushAntiAliasing(antiAlias);

                RenderOptions renderOpts = renderOptions;
                renderOpts.usePatternBitmaps = usePatternBitmaps;
                renderOpts.renderTemplates = RenderTemplateOption.MapAndTemplates;
                renderOpts.blendOverprintedColors = useOverprinting;
                renderOpts.minResolution = mapArea.Width / (float)bitmapSize.Width;

                using (map.Read())
                    map.Draw(grTarget, mapArea, renderOpts, null);

                return ((Skia_Bitmap)grTarget.FinishBitmap());
            }

        }

        static void CompareBitmapBaseline(Skia_Bitmap skiaBitmapNew, string baselineFileName, int maxPixelDiff)
        {
            SKBitmap skBitmap = skiaBitmapNew.Bitmap;
            BitmapTestUtil.CompareBitmapBaseline(skBitmap, baselineFileName, maxPixelDiff);
        }

        // Verifies a test file. Returns true on success, false on failure. In the failure case, 
        // a difference bitmap is written out.
        public static bool VerifyTestFile(string filename, RenderOptions renderOptions, bool usePatternBitmaps, bool useOverprinting, bool testLightenedColor, bool roundtripToOcadFile, bool antiAlias, int minOcadVersion, int maxOcadVersion, int maxPixelDiff)
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
            Map map = new Map(new Skia_TextMetrics(), new Skia_FileLoader(directoryName));
            InputOutput.ReadFile(mapFileName, map);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Draw into a new bitmap.
            Skia_Bitmap bitmapNew = RenderBitmap(map, size, mapArea, renderOptions, usePatternBitmaps, useOverprinting, antiAlias, 1.0F);
            sw.Stop();
            //Console.WriteLine("Rendered bitmap '{0}' to output '{4}' rect={1} size={2} in {3} ms", mapFileName, mapArea, size, sw.ElapsedMilliseconds, pngFileName);

            CompareBitmapBaseline(bitmapNew, pngFileName, maxPixelDiff);

            if (testLightenedColor) {
                string lightenedPngFileName = Path.Combine(Path.GetDirectoryName(pngFileName), Path.GetFileNameWithoutExtension(pngFileName) + "_light.png");
                Skia_Bitmap bitmapLight = RenderBitmap(map, size, mapArea, renderOptions, usePatternBitmaps, useOverprinting, antiAlias, 0.4F);
                CompareBitmapBaseline(bitmapLight, lightenedPngFileName, maxPixelDiff);
                bitmapLight.Dispose();
            }

            if (roundtripToOcadFile) {
                for (int version = minOcadVersion; version <= maxOcadVersion; ++version) {
                    // Save and load to a temp file name.
                    InputOutput.WriteFile(ocadFileName, map, new MapFileFormat(MapFileFormatKind.OCAD, version));

                    // Create and open the map file.
                    map = new Map(new Skia_TextMetrics(), new Skia_FileLoader(TestUtil.GetTestFile("rendering")));
                    InputOutput.ReadFile(ocadFileName, map);

                    // Draw into a new bitmap.
                    bitmapNew = RenderBitmap(map, size, mapArea, renderOptions, usePatternBitmaps, useOverprinting, antiAlias, 1.0F);

                    CompareBitmapBaseline(bitmapNew, pngFileName, maxPixelDiff);

                    File.Delete(ocadFileName);
                }
            }

            bitmapNew.Dispose();

            return true;
        }

        public static void TimeMapRender(Map map, Size size, RectangleF mapArea, string name)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Draw into a new bitmap.
            Skia_Bitmap bitmapNew = RenderBitmap(map, size, mapArea, new RenderOptions(), true, false, true, 1.0F);

            sw.Stop();
            //Console.WriteLine("Rendered bitmap '{0}' in {1} ms", name, sw.ElapsedMilliseconds);
        }

        static Matrix GetTransform(int bitmapWidth, int bitmapHeight, RectangleF rectangle, bool inverted)
        {
            PointF midpoint = new PointF(bitmapWidth / 2.0F, bitmapHeight / 2.0F);
            float scaleFactor = (float)bitmapWidth / rectangle.Width;
            PointF centerPoint = new PointF((rectangle.Left + rectangle.Right) / 2, (rectangle.Top + rectangle.Bottom) / 2);
            Matrix matrix = new Matrix();
            matrix.Translate(midpoint.X, midpoint.Y);
            matrix.Scale(scaleFactor, inverted ? -scaleFactor : scaleFactor);  // y scale is negative to get to cartesian orientation.
            matrix.Translate(-centerPoint.X, -centerPoint.Y);

            return matrix;
        }
    }
}
