using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Drawing.Imaging;

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

        // Write a bitmap to a PNG.
        public static void WriteBitmap(Bitmap bmp, string filename)
        {
            bmp.Save(filename, ImageFormat.Png);
        }



        public static void RenderingTest(int width, RectangleF drawingRectangle, bool inverted, string pngFileName, Action<IGraphicsTarget> draw)
        {
            int height = (int)Math.Ceiling(width * drawingRectangle.Height / drawingRectangle.Width);

            Bitmap bitmapNew = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            BitmapData data = bitmapNew.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bitmapNew.PixelFormat);

            using (var surface = SKSurface.Create(new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul), data.Scan0, data.Stride)) {
                var skcanvas = surface.Canvas;
                skcanvas.Clear(SKColors.White);

                using (Skia_GraphicsTarget grTarget = new Skia_GraphicsTarget(skcanvas)) {
                    grTarget.PushTransform(GetTransform(bitmapNew, drawingRectangle, inverted));
                    draw(grTarget);
                }
            }
            bitmapNew.UnlockBits(data);

            string directoryName = Path.GetDirectoryName(pngFileName);
            string newBitmapName = Path.Combine(directoryName,
                                        Path.GetFileNameWithoutExtension(pngFileName) + "_new.png");
            File.Delete(newBitmapName);

            TestUtil.CompareBitmapBaseline(bitmapNew, pngFileName);
        }

        static Skia_Bitmap RenderBitmap(Map map, Size bitmapSize, RectangleF mapArea, bool usePatternBitmaps, bool useOverprinting, bool antiAlias, float intensity)
        {
            var grTarget = new Skia_BitmapGraphicsTarget(bitmapSize.Width, bitmapSize.Height, false, CmykColor.FromCmyk(0, 0, 0, 0), mapArea, true, null, intensity);
            using (grTarget) {
                grTarget.PushAntiAliasing(antiAlias);

                RenderOptions renderOpts = new RenderOptions();
                renderOpts.usePatternBitmaps = usePatternBitmaps;
                renderOpts.renderTemplates = RenderTemplateOption.MapAndTemplates;
                renderOpts.blendOverprintedColors = useOverprinting;
                renderOpts.minResolution = mapArea.Width / (float)bitmapSize.Width;

                using (map.Read())
                    map.Draw(grTarget, mapArea, renderOpts, null);

                return ((Skia_Bitmap)grTarget.FinishBitmap());
            }

        }

        static Bitmap BitmapFromLockedSkiaBitmap(SKBitmap bitmap)
        {
            IntPtr length;
            IntPtr pixels = bitmap.GetPixels(out length);
            return new Bitmap(bitmap.Width, bitmap.Height, bitmap.RowBytes, bitmap.AlphaType == SKAlphaType.Opaque ? PixelFormat.Format32bppRgb : PixelFormat.Format32bppPArgb, pixels);
        }

        static void CompareBitmapBaseline(Skia_Bitmap skiaBitmapNew, string baselineFileName, int maxPixelDiff)
        {
            SKBitmap skBitmap = skiaBitmapNew.Bitmap;
            Bitmap bitmapNew = BitmapFromLockedSkiaBitmap(skBitmap);
            TestUtil.CompareBitmapBaseline(bitmapNew, baselineFileName, maxPixelDiff);
            bitmapNew.Dispose();
        }

        // Verifies a test file. Returns true on success, false on failure. In the failure case, 
        // a difference bitmap is written out.
        public static bool VerifyTestFile(string filename, bool usePatternBitmaps, bool useOverprinting, bool testLightenedColor, bool roundtripToOcadFile, bool antiAlias, int minOcadVersion, int maxOcadVersion, int maxPixelDiff)
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
            Skia_Bitmap bitmapNew = RenderBitmap(map, size, mapArea, usePatternBitmaps, useOverprinting, antiAlias, 1.0F);
            sw.Stop();
            Console.WriteLine("Rendered bitmap '{0}' to output '{4}' rect={1} size={2} in {3} ms", mapFileName, mapArea, size, sw.ElapsedMilliseconds, pngFileName);

            CompareBitmapBaseline(bitmapNew, pngFileName, maxPixelDiff);

            if (testLightenedColor) {
                string lightenedPngFileName = Path.Combine(Path.GetDirectoryName(pngFileName), Path.GetFileNameWithoutExtension(pngFileName) + "_light.png");
                Skia_Bitmap bitmapLight = RenderBitmap(map, size, mapArea, usePatternBitmaps, useOverprinting, antiAlias, 0.4F);
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
                    bitmapNew = RenderBitmap(map, size, mapArea, usePatternBitmaps, useOverprinting, antiAlias, 1.0F);

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
            Skia_Bitmap bitmapNew = RenderBitmap(map, size, mapArea, true, false, true, 1.0F);

            sw.Stop();
            Console.WriteLine("Rendered bitmap '{0}' in {1} ms", name, sw.ElapsedMilliseconds);
        }

        static Matrix GetTransform(Bitmap bitmap, RectangleF rectangle, bool inverted)
        {
            Size bitmapSize = bitmap.Size;
            PointF midpoint = new PointF(bitmapSize.Width / 2.0F, bitmapSize.Height / 2.0F);
            float scaleFactor = (float)bitmapSize.Width / rectangle.Width;
            PointF centerPoint = new PointF((rectangle.Left + rectangle.Right) / 2, (rectangle.Top + rectangle.Bottom) / 2);
            Matrix matrix = new Matrix();
            matrix.Translate(midpoint.X, midpoint.Y);
            matrix.Scale(scaleFactor, inverted ? -scaleFactor : scaleFactor);  // y scale is negative to get to cartesian orientation.
            matrix.Translate(-centerPoint.X, -centerPoint.Y);

            return matrix;
        }
    }
}
