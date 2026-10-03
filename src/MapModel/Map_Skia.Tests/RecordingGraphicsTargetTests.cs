using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Map_Skia.Tests
{
    using System.Drawing;
    using NUnit.Framework;
    using PurplePen.Graphics2D;
    using PurplePen.MapModel;
    using SkiaSharp;
    using TestingUtils;

    // Tests for RecordingGraphicsTarget. Rendering tests record drawing, play it back, and compare
    // against the same baselines used by SimpleDrawingTests and RenderingTests.
    [TestFixture]
    public class RecordingGraphicsTargetTests
    {
        [Test]
        public void RecordingSimpleLines()
        {
            RenderingUtil.RecordingRenderingTest(500, RectangleF.FromLTRB(0, 0, 800, 1100), false, TestUtil.GetTestFile("skia_render\\simplelines.png"),
                grTarget => {
                    grTarget.PushAntiAliasing(true);

                    object penKey1 = new object();
                    grTarget.CreatePen(penKey1, CmykColor.FromColor(Color.DarkBlue), 20, LineCapMode.Flat, LineJoinMode.Bevel, 1);
                    grTarget.DrawLine(penKey1, new PointF(100, 100), new PointF(600, 150));

                    object penKey2 = new object();
                    grTarget.CreatePen(penKey2, CmykColor.FromColor(Color.LightBlue), 40, LineCapMode.Round, LineJoinMode.Round, 1);
                    grTarget.DrawLine(penKey2, new PointF(100, 200), new PointF(600, 250));

                    object penKey3 = new object();
                    grTarget.CreatePen(penKey3, CmykColor.FromColor(Color.IndianRed), 80, LineCapMode.Square, LineJoinMode.Miter, 2);
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
        public void RecordingPaths()
        {
            RenderingUtil.RecordingRenderingTest(500, RectangleF.FromLTRB(0, 0, 800, 1100), false, TestUtil.GetTestFile("skia_render\\paths.png"),
               grTarget => {
                   Matrix mat = new Matrix();
                   mat.Translate(425, 250);
                   grTarget.PushTransform(mat);

                   object penKey1 = new object(), pathKey1 = new Object();
                   grTarget.CreatePen(penKey1, CmykColor.FromColor(Color.Red), 7.0F, LineCapMode.Flat, LineJoinMode.Miter, 5F);
                   grTarget.CreatePath(pathKey1, new List<GraphicsPathPart> {
                            new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(-70, -70)}),
                            new GraphicsPathPart(GraphicsPathPartKind.Lines,  new PointF[] { new PointF(-10, 50), new PointF(20, 50), new PointF(70, -90) }),
                            new GraphicsPathPart(GraphicsPathPartKind.Close, new PointF[0]),
                       },
                       AreaFillMode.Alternate);

                   grTarget.DrawPath(penKey1, pathKey1);

                   object penKey2 = new object(), pathKey2 = new Object();
                   grTarget.CreatePen(penKey2, CmykColor.FromColor(Color.Green), 3.0F, LineCapMode.Round, LineJoinMode.Round, 5F);
                   grTarget.CreatePath(pathKey2, new List<GraphicsPathPart> {
                            new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(-70, -70)}),
                            new GraphicsPathPart(GraphicsPathPartKind.Beziers,  new PointF[] { new PointF(-10, 50), new PointF(20, 50), new PointF(70, -90) }),
                            new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(0, 0)}),
                            new GraphicsPathPart(GraphicsPathPartKind.Lines,  new PointF[] { new PointF(-50, 50), new PointF(50, 50) }),
                            new GraphicsPathPart(GraphicsPathPartKind.Close, new PointF[0] )},
                       AreaFillMode.Alternate);
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
                       AreaFillMode.Alternate);
                   grTarget.FillPath(brushKey3, pathKey3);
               }
            );
        }

        [Test]
        public void RecordingClipping()
        {
            RenderingUtil.RecordingRenderingTest(500, RectangleF.FromLTRB(0, 0, 800, 1100), false, TestUtil.GetTestFile("skia_render\\clipping.png"),
                grTarget => {
                    Matrix mat = new Matrix();
                    mat.Translate(425, 250);
                    mat.Scale(2, 2);
                    grTarget.PushTransform(mat);

                    object pathKey = new object(), brushKey = new object(), penKey = new object();

                    grTarget.CreatePath(pathKey, new List<GraphicsPathPart> {
                            new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(-50, -30)}),
                            new GraphicsPathPart(GraphicsPathPartKind.Lines,  new PointF[] {new PointF(0, 80), new PointF(50, -30), new PointF(-50, 50), new PointF(50, 50)})},
                            AreaFillMode.Alternate);

                    grTarget.PushClip(pathKey);

                    grTarget.CreateSolidBrush(brushKey, CmykColor.FromColor(Color.Red));
                    grTarget.FillRectangle(brushKey, new RectangleF(-100, -100, 200, 200));

                    grTarget.CreatePen(penKey, CmykColor.FromColor(Color.Green), 15.0F, LineCapMode.Flat, LineJoinMode.Bevel, 5F);
                    grTarget.DrawEllipse(penKey, new PointF(0, 0), 40, 50);

                    grTarget.PopClip();
                }
            );
        }

        [Test]
        public void RecordingText()
        {
            RenderingUtil.RecordingRenderingTest(500, RectangleF.FromLTRB(0, 0, 800, 1100), false, TestUtil.GetTestFile("skia_render\\text.png"),
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
                    grTarget.CreatePen(penKey2, CmykColor.FromColor(Color.Crimson), 1, LineCapMode.Round, LineJoinMode.Round, 2);
                    grTarget.DrawTextOutline("Hi There", fontKey2, penKey2, new PointF(100, 500));
                }
            );
        }

        [Test]
        public void RecordingBitmap()
        {
            RenderingUtil.RecordingRenderingTest(1000, RectangleF.FromLTRB(0, 0, 800, 1100), false, TestUtil.GetTestFile("skia_render\\bitmap.png"),
                grTarget => {
                    using (Skia_Bitmap penguins = new Skia_Bitmap(SKBitmap.Decode(TestUtil.GetTestFile("pdfrender\\penguins.jpg")))) {
                        grTarget.DrawBitmap(penguins, new RectangleF(100, 100, 600, 400), BitmapScaling.MediumQuality);
                        grTarget.DrawBitmap(penguins, new RectangleF(200, 800, 150, 100), BitmapScaling.MediumQuality);
                    }
                }
            );
        }

        [Test]
        public void RecordingBitmapPart()
        {
            RenderingUtil.RecordingRenderingTest(1000, RectangleF.FromLTRB(0, 0, 800, 1100), false, TestUtil.GetTestFile("skia_render\\bitmappart.png"),
                grTarget => {
                    using (Skia_Bitmap bitmap = new Skia_Bitmap(SKBitmap.Decode(TestUtil.GetTestFile("pdfrender\\penguins.jpg")))) {
                        grTarget.DrawBitmapPart(bitmap, bitmap.PixelWidth * 3 / 10, bitmap.PixelHeight * 2 / 10, bitmap.PixelWidth * 5 / 10, bitmap.PixelHeight * 4 / 10,
                                        new RectangleF(100, 500, 600, 400), BitmapScaling.NearestNeighbor);
                    }
                }
            );
        }

        [Test]
        public void RecordingPatternBrush()
        {
            RenderingUtil.RecordingRenderingTest(800, new RectangleF(-103, -117, 200, 200), false, TestUtil.GetTestFile("skia_render\\patternbrush.png"),
                grTarget => {
                    IBrushTarget brushTarget = grTarget.CreatePatternBrush(new SizeF(30, 20), 0, 60, 60);

                    object pen = new object();
                    brushTarget.CreatePen(pen, CmykColor.FromRgb(1, 0, 0), 1.5F, LineCapMode.Round, LineJoinMode.Round, 5F);
                    brushTarget.DrawLine(pen, new PointF(-15, -10), new PointF(3, 10));
                    pen = new object();
                    brushTarget.CreatePen(pen, CmykColor.FromRgb(0, 1, 0), 3.0F, LineCapMode.Round, LineJoinMode.Round, 5F);
                    brushTarget.DrawLine(pen, new PointF(3, 10), new PointF(15, -10));

                    pen = new object();
                    brushTarget.CreatePen(pen, CmykColor.FromRgb(0, 0, 1), 3F, LineCapMode.Round, LineJoinMode.Round, 5F);
                    brushTarget.DrawEllipse(pen, new PointF(1, -2), 5, 4);

                    object brush = new object();
                    brushTarget.FinishBrush(brush);

                    grTarget.FillPolygon(brush, new PointF[] { new PointF(-50, -60), new PointF(0, 30), new PointF(50, -60), new PointF(-50, 20), new PointF(50, 20) }, AreaFillMode.Winding);

                    object pen2 = new object();
                    grTarget.CreatePen(pen2, CmykColor.FromRgb(0, 0, 0), 0.5F, LineCapMode.Flat, LineJoinMode.Round, 5F);
                    grTarget.DrawLine(pen2, new PointF(-30, -30), new PointF(30, 30));
                    grTarget.DrawLine(pen2, new PointF(30, -30), new PointF(-30, 30));
                });
        }

        [Test]
        public void RecordingMapRender()
        {
            // Area symbols use pattern brushes; also tests the lightened (intensity) variant.
            string fullname = TestUtil.GetTestFile("skia_render\\isomarea.txt");
            bool ok = RenderingUtil.VerifyTestFile(fullname, new RenderOptions(), true, false, true, false, false, 6, 12, 0, true);
            Assert.IsTrue(ok);
        }

        [Test]
        public void PlaybackBeforeEndRecordingThrows()
        {
            using (RecordingGraphicsTarget recorder = new RecordingGraphicsTarget())
            using (SKBitmap bitmap = new SKBitmap(10, 10))
            using (SKCanvas canvas = new SKCanvas(bitmap))
            using (Skia_GraphicsTarget target = new Skia_GraphicsTarget(canvas)) {
                Assert.IsTrue(recorder.IsRecording);
                Assert.Throws<InvalidOperationException>(() => recorder.Playback(target));
            }
        }

        [Test]
        public void DrawingAfterEndRecordingThrows()
        {
            using (RecordingGraphicsTarget recorder = new RecordingGraphicsTarget()) {
                object penKey = new object();
                recorder.CreatePen(penKey, CmykColor.FromCmyk(0, 0, 0, 1), 1, LineCapMode.Flat, LineJoinMode.Miter, 1);
                recorder.EndRecording();

                Assert.IsFalse(recorder.IsRecording);
                Assert.Throws<InvalidOperationException>(() => recorder.DrawLine(penKey, new PointF(0, 0), new PointF(1, 1)));
                Assert.Throws<InvalidOperationException>(() => recorder.CreateSolidBrush(new object(), CmykColor.FromCmyk(0, 0, 0, 1)));
                Assert.Throws<InvalidOperationException>(() => recorder.EndRecording());
            }
        }

        [Test]
        public void PlaybackAfterDisposeThrows()
        {
            RecordingGraphicsTarget recorder = new RecordingGraphicsTarget();
            recorder.EndRecording();
            recorder.Dispose();

            using (SKBitmap bitmap = new SKBitmap(10, 10))
            using (SKCanvas canvas = new SKCanvas(bitmap))
            using (Skia_GraphicsTarget target = new Skia_GraphicsTarget(canvas)) {
                Assert.Throws<ObjectDisposedException>(() => recorder.Playback(target));
            }
        }

        [Test]
        public void HasObjects()
        {
            using (RecordingGraphicsTarget recorder = new RecordingGraphicsTarget()) {
                object pathKey = new object(), brushKey = new object(), penKey = new object(), fontKey = new object();

                Assert.IsFalse(recorder.HasPath(pathKey));
                Assert.IsFalse(recorder.HasBrush(brushKey));
                Assert.IsFalse(recorder.HasPen(penKey));
                Assert.IsFalse(recorder.HasFont(fontKey));

                recorder.CreatePath(pathKey, new List<GraphicsPathPart> {
                    new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[] { new PointF(0, 0) }),
                    new GraphicsPathPart(GraphicsPathPartKind.Lines, new PointF[] { new PointF(1, 1) }) }, AreaFillMode.Winding);
                recorder.CreateSolidBrush(brushKey, CmykColor.FromCmyk(1, 0, 0, 0));
                recorder.CreatePen(penKey, brushKey, 1, LineCapMode.Flat, LineJoinMode.Miter, 1);
                recorder.CreateFont(fontKey, "Arial", 10, TextEffects.Regular);

                Assert.IsTrue(recorder.HasPath(pathKey));
                Assert.IsTrue(recorder.HasBrush(brushKey));
                Assert.IsTrue(recorder.HasPen(penKey));
                Assert.IsTrue(recorder.HasFont(fontKey));

                Assert.Throws<InvalidOperationException>(() => recorder.CreateSolidBrush(brushKey, CmykColor.FromCmyk(1, 0, 0, 0)));

                // Setting intensity destroys pens and brushes, but not paths and fonts.
                recorder.Intensity = 0.5F;
                Assert.AreEqual(0.5F, recorder.Intensity);
                Assert.IsTrue(recorder.HasPath(pathKey));
                Assert.IsFalse(recorder.HasBrush(brushKey));
                Assert.IsFalse(recorder.HasPen(penKey));
                Assert.IsTrue(recorder.HasFont(fontKey));
            }
        }

        [Test]
        public void ChangingInputsDoesNotAffectPlayback()
        {
            // Draw directly as the reference.
            SKBitmap expected = DrawToBitmap(grTarget => DrawMutableInputs(grTarget, false));

            // Record, then mutate all the inputs before playing back.
            using (RecordingGraphicsTarget recorder = new RecordingGraphicsTarget()) {
                DrawMutableInputs(recorder, true);
                recorder.EndRecording();

                SKBitmap actual = DrawToBitmap(grTarget => recorder.Playback(grTarget));
                Assert.AreEqual(expected.Bytes, actual.Bytes);
                actual.Dispose();
            }

            expected.Dispose();
        }

        [Test]
        public void KeysNotReferencedAfterEndRecording()
        {
            using (RecordingGraphicsTarget recorder = new RecordingGraphicsTarget()) {
                WeakReference[] keys = RecordWithKeys(recorder);
                recorder.EndRecording();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                foreach (WeakReference key in keys)
                    Assert.IsFalse(key.IsAlive);

                // Playback still works without the keys.
                DrawToBitmap(grTarget => recorder.Playback(grTarget)).Dispose();
            }
        }

        // Create objects with keys that are only referenced by the recorder. Returns weak references to the keys.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference[] RecordWithKeys(RecordingGraphicsTarget recorder)
        {
            object pathKey = new object(), brushKey = new object(), penKey = new object(), fontKey = new object(), patternKey = new object();

            recorder.CreatePath(pathKey, new List<GraphicsPathPart> {
                new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[] { new PointF(10, 10) }),
                new GraphicsPathPart(GraphicsPathPartKind.Lines, new PointF[] { new PointF(50, 50), new PointF(10, 50) }),
                new GraphicsPathPart(GraphicsPathPartKind.Close, new PointF[0]) }, AreaFillMode.Winding);
            recorder.CreateSolidBrush(brushKey, CmykColor.FromCmyk(1, 0, 0, 0));
            recorder.CreatePen(penKey, brushKey, 2, LineCapMode.Flat, LineJoinMode.Miter, 1);
            recorder.CreateFont(fontKey, "Arial", 10, TextEffects.Regular);

            IBrushTarget brushTarget = recorder.CreatePatternBrush(new SizeF(10, 10), 0, 10, 10);
            object innerBrushKey = new object();
            brushTarget.CreateSolidBrush(innerBrushKey, CmykColor.FromCmyk(0, 1, 0, 0));
            brushTarget.FillRectangle(innerBrushKey, new RectangleF(-2, -2, 4, 4));
            brushTarget.FinishBrush(patternKey);

            recorder.FillPath(patternKey, pathKey);
            recorder.DrawPath(penKey, pathKey);
            recorder.DrawText("A", fontKey, brushKey, new PointF(60, 60));

            return new WeakReference[] {
                new WeakReference(pathKey), new WeakReference(brushKey), new WeakReference(penKey),
                new WeakReference(fontKey), new WeakReference(patternKey), new WeakReference(innerBrushKey) };
        }

        // Draw using inputs that can be mutated after the drawing calls. If mutate is true, all inputs are changed after use.
        private static void DrawMutableInputs(IGraphicsTarget grTarget, bool mutate)
        {
            object penKey = new object(), brushKey = new object();
            grTarget.CreateSolidBrush(brushKey, CmykColor.FromCmyk(0, 1, 1, 0));
            grTarget.CreatePen(penKey, CmykColor.FromCmyk(1, 0, 0, 0), 3, LineCapMode.Flat, LineJoinMode.Miter, 1);

            Matrix matrix = new Matrix();
            matrix.Translate(5, 5);
            grTarget.PushTransform(matrix);

            PointF[] pts = { new PointF(10, 10), new PointF(80, 20), new PointF(40, 80) };
            grTarget.DrawPolyline(penKey, pts);

            List<GraphicsPathPart> parts = new List<GraphicsPathPart> {
                new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[] { new PointF(50, 50) }),
                new GraphicsPathPart(GraphicsPathPartKind.Lines, new PointF[] { new PointF(90, 50), new PointF(90, 90) }),
                new GraphicsPathPart(GraphicsPathPartKind.Close, new PointF[0]) };
            grTarget.FillPath(brushKey, parts, AreaFillMode.Winding);

            RectangleF[] clipRects = { new RectangleF(0, 0, 40, 100) };
            grTarget.PushClip(clipRects);
            grTarget.FillRectangle(brushKey, new RectangleF(0, 0, 100, 100));
            grTarget.PopClip();

            grTarget.PopTransform();

            if (mutate) {
                matrix.Translate(30, 30);
                pts[1] = new PointF(0, 99);
                parts[1].Points[0] = new PointF(0, 0);
                parts.Clear();
                clipRects[0] = new RectangleF(0, 0, 100, 100);
            }
        }

        // Create a bitmap and draw on it with a Skia_GraphicsTarget.
        private static SKBitmap DrawToBitmap(Action<IGraphicsTarget> draw)
        {
            SKBitmap bitmap = new SKBitmap(100, 100);
            using (SKCanvas canvas = new SKCanvas(bitmap)) {
                canvas.Clear(SKColors.White);
                using (Skia_GraphicsTarget grTarget = new Skia_GraphicsTarget(canvas)) {
                    draw(grTarget);
                }
            }
            return bitmap;
        }
    }
}
