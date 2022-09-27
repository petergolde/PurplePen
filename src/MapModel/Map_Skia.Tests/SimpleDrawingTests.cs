extern alias Graphics2DStd;
using System;
using System.Collections.Generic;

namespace Map_Skia.Tests
{
    using System.Diagnostics;
    using System.Drawing;
    using Graphics2DStd::System.Drawing.Drawing2D;
    using System.Text;
    using NUnit.Framework;
    using Graphics2DStd::PurplePen.Graphics2D;
    using PurplePen.MapModel;
    using SkiaSharp;
    using TestingUtils;

    [TestFixture]
    public class SimpleDrawingTests
    {
        [Test]
        public void SimpleLines()
        {
            RenderingUtil.RenderingTest(500, RectangleF.FromLTRB(0, 0, 800, 1100), false, TestUtil.GetTestFile("skia_render\\simplelines.png"), 
                grTarget => {
                    grTarget.PushAntiAliasing(true);

                    object penKey1 = new object();
                    grTarget.CreatePen(penKey1, CmykColor.FromColor(Color.DarkBlue), 20, LineCap.Flat, LineJoin.Bevel, 1);
                    grTarget.DrawLine(penKey1, new PointF(100, 100), new PointF(600, 150));

                    object penKey2 = new object();
                    grTarget.CreatePen(penKey2, CmykColor.FromColor(Color.LightBlue), 40, LineCap.Round, LineJoin.Round, 1);
                    grTarget.DrawLine(penKey2, new PointF(100, 200), new PointF(600, 250));

                    object penKey3 = new object();
                    grTarget.CreatePen(penKey3, CmykColor.FromColor(Color.IndianRed), 80, LineCap.Square, LineJoin.Miter, 2);
                    grTarget.DrawLine(penKey3, new PointF(100, 300), new PointF(600, 350));

                    grTarget.DrawPolyline(penKey1, new PointF[]
                         { new PointF(150, 400), new PointF(150, 450), new PointF(400, 400), new PointF(300, 500) });

                    grTarget.DrawPolyline(penKey2, new PointF[]
                        { new PointF(150, 500), new PointF(150, 550), new PointF(400, 500), new PointF(300, 600) });

                    grTarget.DrawPolyline(penKey3, new PointF[]
                        { new PointF(120, 700), new PointF(150, 750), new PointF(400, 700), new PointF(300, 800) });

                    grTarget.DrawPolygon(penKey1, new PointF[]
                        { new PointF(500, 400), new PointF(700, 500), new PointF(650, 600), new PointF(535, 520) });

                    grTarget.DrawPolygon(penKey2, new PointF[]
                        { new PointF(500, 600), new PointF(700, 700), new PointF(650, 800), new PointF(535, 720) });

                    grTarget.DrawPolygon(penKey3, new PointF[]
                        { new PointF(500, 800), new PointF(700, 900), new PointF(650, 1000), new PointF(535, 920) });

                    grTarget.PopAntiAliasing();
                }
            );
        }

        [Test]
        public void ComplexLines()
        {
            RenderingUtil.RenderingTest(500, RectangleF.FromLTRB(0, 0, 800, 1100), false, TestUtil.GetTestFile("skia_render\\complexlines.png"),
                grTarget => {
                    object penKey1 = new object();
                    grTarget.CreatePen(penKey1, CmykColor.FromColor(Color.DarkBlue), 20, LineCap.Round, LineJoin.Round, 1);

                    grTarget.DrawRectangle(penKey1, RectangleF.FromLTRB(100, 200, 600, 400));
                    grTarget.DrawEllipse(penKey1, new PointF(350, 550), 250, 70);
                    grTarget.DrawArc(penKey1, new PointF(500, 850), 150, 30, 200);

                    object penKey2 = new object();
                    grTarget.CreatePen(penKey2, CmykColor.FromColor(Color.IndianRed), 3, LineCap.Round, LineJoin.Round, 1);
                    grTarget.DrawRectangle(penKey2, RectangleF.FromLTRB(100, 800, 600, 1000));
                }
            );
        }

        [Test]
        public void FillAreas()
        {
            RenderingUtil.RenderingTest(500, RectangleF.FromLTRB(0, 0, 800, 1100), false, TestUtil.GetTestFile("skia_render\\fillareas.png"),
                grTarget => {
                    object brushKey1 = new object();
                    grTarget.CreateSolidBrush(brushKey1, CmykColor.FromRgb(0, 0, 1));

                    grTarget.FillRectangle(brushKey1, RectangleF.FromLTRB(100, 200, 600, 400));
                    grTarget.FillEllipse(brushKey1, new PointF(350, 550), 250, 70);

                    object brushKey2 = new object();
                    grTarget.CreateSolidBrush(brushKey2, CmykColor.FromRgb(0.5F, 0, 0));
                    grTarget.FillPolygon(brushKey2, new PointF[]
                        { new PointF(100,1000), new PointF(200, 800), new PointF(300, 1000), new PointF(100, 850), new PointF(300,850) },
                        FillMode.Alternate);

                    grTarget.FillPolygon(brushKey2, new PointF[]
                    { new PointF(500, 1000), new PointF(600, 800), new PointF(700, 1000), new PointF(500, 850), new PointF(700, 850) },
                        FillMode.Winding);

                }
            );
        }

        [Test]
        public void Paths()
        {
            RenderingUtil.RenderingTest(500, RectangleF.FromLTRB(0, 0, 800, 1100), false, TestUtil.GetTestFile("skia_render\\paths.png"),
               grTarget => {
                   Matrix mat = new Matrix();
                   mat.Translate(425, 250);
                   grTarget.PushTransform(mat);

                   object penKey1 = new object(), pathKey1 = new Object();
                   grTarget.CreatePen(penKey1, CmykColor.FromColor(Color.Red), 7.0F, LineCap.Flat, LineJoin.Miter, 5F);
                   grTarget.CreatePath(pathKey1, new List<GraphicsPathPart> {
                            new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(-70, -70)}),
                            new GraphicsPathPart(GraphicsPathPartKind.Lines,  new PointF[] { new PointF(-10, 50), new PointF(20, 50), new PointF(70, -90) }),
                            new GraphicsPathPart(GraphicsPathPartKind.Close, new PointF[0]),
                       },
                       FillMode.Alternate);

                   grTarget.DrawPath(penKey1, pathKey1);

                   object penKey2 = new object(), pathKey2 = new Object();
                   grTarget.CreatePen(penKey2, CmykColor.FromColor(Color.Green), 3.0F, LineCap.Round, LineJoin.Round, 5F);
                   grTarget.CreatePath(pathKey2, new List<GraphicsPathPart> {
                            new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(-70, -70)}),
                            new GraphicsPathPart(GraphicsPathPartKind.Beziers,  new PointF[] { new PointF(-10, 50), new PointF(20, 50), new PointF(70, -90) }),
                            new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(0, 0)}),
                            new GraphicsPathPart(GraphicsPathPartKind.Lines,  new PointF[] { new PointF(-50, 50), new PointF(50, 50) }),
                            new GraphicsPathPart(GraphicsPathPartKind.Close, new PointF[0] )},
                       FillMode.Alternate);
                   grTarget.DrawPath(penKey2, pathKey2);

                   mat = new Matrix();
                   mat.Translate(0, 350);
                   grTarget.PushTransform(mat);

                   object brushKey3 = new object(), pathKey3 = new Object();
                   grTarget.CreateSolidBrush(brushKey3, CmykColor.FromColor(Color.IndianRed));
                   grTarget.CreatePath(pathKey3, new List<GraphicsPathPart> {
                            new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(-70, -70)}),
                            new GraphicsPathPart(GraphicsPathPartKind.Beziers,  new PointF[] { new PointF(-10, 50), new PointF(20, 50), new PointF(70, -90) }),
                            new GraphicsPathPart(GraphicsPathPartKind.Close, new PointF[0] )},
                       FillMode.Alternate);
                   grTarget.FillPath(brushKey3, pathKey3);


                }
               );
        }

        [Test]
        public void Clipping()
        {

            RenderingUtil.RenderingTest(500, RectangleF.FromLTRB(0, 0, 800, 1100), false, TestUtil.GetTestFile("skia_render\\clipping.png"),
                grTarget => {
                    Matrix mat = new Matrix();
                    mat.Translate(425, 250);
                    mat.Scale(2, 2);
                    grTarget.PushTransform(mat);

                    object pathKey = new object(), brushKey = new object(), penKey = new object();

                    grTarget.CreatePath(pathKey, new List<GraphicsPathPart> {
                            new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(-50, -30)}),
                            new GraphicsPathPart(GraphicsPathPartKind.Lines,  new PointF[] {new PointF(0, 80), new PointF(50, -30), new PointF(-50, 50), new PointF(50, 50)})},
                            FillMode.Alternate);

                    grTarget.PushClip(pathKey);

                    grTarget.CreateSolidBrush(brushKey, CmykColor.FromColor(Color.Red));
                    grTarget.FillRectangle(brushKey, new RectangleF(-100, -100, 200, 200));

                    grTarget.CreatePen(penKey, CmykColor.FromColor(Color.Green), 15.0F, LineCap.Flat, LineJoin.Bevel, 5F);
                    grTarget.DrawEllipse(penKey, new PointF(0, 0), 40, 50);

                    grTarget.PopClip();
                }
            );
        }

        [Test]
        public void Text()
        {

            RenderingUtil.RenderingTest(500, RectangleF.FromLTRB(0, 0, 800, 1100), false, TestUtil.GetTestFile("skia_render\\text.png"),
                grTarget => {
                    grTarget.PushAntiAliasing(true);

                    object fontKey = new object(), brushKey = new object();

                    grTarget.CreateFont(fontKey, "Cambria", 90, TextEffects.Italic);
                    grTarget.CreateSolidBrush(brushKey, CmykColor.FromColor(Color.BlueViolet));
                    grTarget.DrawText("Hello", fontKey, brushKey, new PointF(100, 100));

                    Matrix mat = new Matrix();
                    mat.RotateAt(45, new PointF(100, 200));
                    grTarget.PushTransform(mat);
                    grTarget.DrawText("Hello", fontKey, brushKey, new PointF(100, 200));
                    grTarget.PopTransform();

                    object fontKey2 = new object(), penKey2 = new object();
                    grTarget.CreateFont(fontKey2, "Times New Roman", 170, TextEffects.Bold | TextEffects.Italic);
                    grTarget.CreatePen(penKey2, CmykColor.FromColor(Color.Crimson), 1, LineCap.Round, LineJoin.Round, 2);
                    grTarget.DrawTextOutline("Hi There", fontKey2, penKey2, new PointF(100, 500));

                }
            );
        }

        [Test]
        public unsafe void TextOutline()
        {

            RenderingUtil.RenderingTest(500, RectangleF.FromLTRB(0, 0, 800, 1100), false, TestUtil.GetTestFile("skia_render\\text_outline.png"),
                grTarget => {
                    object fontKey = new object(), brushKey = new object();

                    grTarget.CreateFont(fontKey, "Cambria", 200, TextEffects.Italic);
                    grTarget.CreateSolidBrush(brushKey, CmykColor.FromColor(Color.BlueViolet));
                    grTarget.DrawText("Hell\u00F6", fontKey, brushKey, new PointF(100, 100));

                    SKCanvas canvas = ((Skia_GraphicsTarget)grTarget).Canvas;
                    SKPaint paint = new SKPaint();
                    paint.Color = SKColors.Chartreuse;
                    paint.TextEncoding = SKTextEncoding.Utf8;
                    paint.Typeface = SKTypeface.FromFamilyName("Cambria", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic);
                    paint.TextSize = 200;
                    byte[] bytes = Encoding.UTF8.GetBytes("Hell\u00F6");
                    SKPath path;
                    fixed(byte *p = bytes)
                    {
                        path = paint.GetTextPath((IntPtr)p, (IntPtr) bytes.Length, 100, 100 - paint.FontMetrics.Ascent);
                    }
                    paint.StrokeWidth = 3;
                    paint.IsStroke = true;
                    canvas.DrawPath(path, paint);

                }
            );
        }

        [Test]
        public unsafe void TextPosOutline()
        {

            RenderingUtil.RenderingTest(500, RectangleF.FromLTRB(0, 0, 800, 1100), false, TestUtil.GetTestFile("skia_render\\text_pos_outline.png"),
                grTarget => {
                    SKCanvas canvas = ((Skia_GraphicsTarget)grTarget).Canvas;
                    SKPaint paint = new SKPaint();
                    SKPoint[] points = { new SKPoint(125, 600), new SKPoint(189, 214), new SKPoint(278, 303), new SKPoint(397, 245), new SKPoint(545, 299) };
                    paint.Color = SKColors.Red;
                    paint.TextEncoding = SKTextEncoding.Utf8;
                    paint.Typeface = SKTypeface.FromFamilyName("Cambria", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
                    paint.TextSize = 300;
                    paint.IsStroke = false;
                    for (int i = 0; i < points.Length; ++i) {
                        canvas.DrawText("Hell\u00F6".Substring(i, 1), points[i], paint);
                    }

                    SKPath path = paint.GetTextPath("Hell\u00F6", points);
                    paint.IsStroke = true;
                    paint.Color = SKColors.DarkBlue;
                    paint.StrokeWidth = 15;
                    canvas.DrawPath(path, paint);

                    byte[] bytes = Encoding.UTF8.GetBytes("Hell\u00F6");
                    fixed (byte* p = bytes)
                    {
                        path = paint.GetTextPath((IntPtr)p, (IntPtr)bytes.Length, points);
                    }
                    paint.IsStroke = true;
                    paint.Color = SKColors.Yellow;
                    paint.StrokeWidth = 5;
                    canvas.DrawPath(path, paint);

                }
            );
        }

        [Test]
        public void Bitmap()
        {
            RenderingUtil.RenderingTest(1000, RectangleF.FromLTRB(0, 0, 800, 1100), false, TestUtil.GetTestFile("skia_render\\bitmap.png"),
                grTarget => {
                    Skia_Bitmap penguins = new Skia_Bitmap(SKBitmap.Decode(TestUtil.GetTestFile("pdfrender\\penguins.jpg")));
                    grTarget.DrawBitmap(penguins, new RectangleF(100, 100, 600, 400), BitmapScaling.MediumQuality, 0.01F);
                    grTarget.DrawBitmap(penguins, new RectangleF(200, 800, 150, 100), BitmapScaling.MediumQuality, 0.01F);
                }
            );
        }

        [Test]
        public void BitmapPart()
        {
            RenderingUtil.RenderingTest(1000, RectangleF.FromLTRB(0, 0, 800, 1100), false, TestUtil.GetTestFile("skia_render\\bitmappart.png"),
                grTarget => {
                    Skia_Bitmap bitmap = new Skia_Bitmap(SKBitmap.Decode(TestUtil.GetTestFile("pdfrender\\penguins.jpg")));
                    grTarget.DrawBitmapPart(bitmap, bitmap.PixelWidth * 3 / 10, bitmap.PixelHeight * 2 / 10, bitmap.PixelWidth * 5 / 10, bitmap.PixelHeight * 4 / 10,
                                    new RectangleF(100, 500, 600, 400), BitmapScaling.NearestNeighbor, 0.01F);
                }
            );
        }

        [Test]
        public void PatternBrush()
        {
            RenderingUtil.RenderingTest(800, new RectangleF(-103, -117, 200, 200), false, TestUtil.GetTestFile("skia_render\\patternbrush.png"),
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

                    object pen2 = new object();
                    grTarget.CreatePen(pen2, CmykColor.FromRgb(0, 0, 0), 0.5F, LineCap.Flat, LineJoin.Round, 5F);
                    grTarget.DrawLine(pen2, new PointF(-30, -30), new PointF(30, 30));
                    grTarget.DrawLine(pen2, new PointF(30, -30), new PointF(-30, 30));
                });
        }

        [Test]
        public void RotatedPatternBrush()
        {
            RenderingUtil.RenderingTest(800, new RectangleF(-103, -117, 200, 200), false, TestUtil.GetTestFile("skia_render\\rotatedpatternbrush.png"),
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
        public void SkiaTimeTeanWest()
        {
            // Create and open the map file.
            Map map = new Map(new Skia_TextMetrics(), null);
            InputOutput.ReadFile(TestUtil.GetTestFile(@"rendering\teanwest.ocd"), map);

            RenderingUtil.TimeMapRender(map, new Size(1814, 1022), new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
            RenderingUtil.TimeMapRender(map, new Size(1814, 1022), new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
            RenderingUtil.TimeMapRender(map, new Size(1814, 1022), new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
            RenderingUtil.TimeMapRender(map, new Size(1814, 1022), new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
            RenderingUtil.TimeMapRender(map, new Size(1814, 1022), new RectangleF(-347.3F, -275F, 408.3F, 230F), "TeanWest full");
        }

        [Test]
        public void SkiaTimeSalmon()
        {
            // Create and open the map file.
            Map map = new Map(new Skia_TextMetrics(), null);
            InputOutput.ReadFile(TestUtil.GetTestFile(@"rendering\SalmonLaSac.ocd"), map);

            RenderingUtil.TimeMapRender(map, new Size(1462, 1564), RectangleF.FromLTRB(-115.4F, -93.3F, 114.6F, 152.75F), "Salmon La Sac");
            RenderingUtil.TimeMapRender(map, new Size(1462, 1564), RectangleF.FromLTRB(-115.4F, -93.3F, 114.6F, 152.75F), "Salmon La Sac");
            RenderingUtil.TimeMapRender(map, new Size(1462, 1564), RectangleF.FromLTRB(-115.4F, -93.3F, 114.6F, 152.75F), "Salmon La Sac");
        }


    }
}
