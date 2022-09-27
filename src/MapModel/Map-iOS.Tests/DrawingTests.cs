
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

using PurplePen.MapModel;
using PurplePen.Graphics2D;

using Foundation;
using UIKit;
using CoreGraphics;

using NUnit.Framework;
using System.Collections.Generic;

namespace MapiOS.Tests
{
    [TestFixture]
    public class DrawingTests
    {
        static readonly CmykColor DarkBlue = CmykColor.FromRgb(0, 0, 0.55F);
        static readonly CmykColor LightBlue = CmykColor.FromRgb((float) 0xAD/0xFF, (float) 0xD8/0xFF, (float) 0xE6/0xFF);
        static readonly CmykColor IndianRed = CmykColor.FromRgb((float) 0xCD/0xFF, (float) 0x5C/0xFF, (float) 0x5C/0xFF);

        public void CheckDrawing(string testName, int width, int height, Action<IGraphicsTarget> drawProc)
        {
            UIImage image = TestUtil.CreateImage(drawProc, width, height);
            TestUtil.CheckAgainstBaseline(testName, image);
        }
        
        public void CheckDrawing(string testName, int width, RectangleF drawingRectangle, Action<IGraphicsTarget> drawProc)
        {
            UIImage image = TestUtil.CreateImage(width, drawingRectangle, drawProc);
            TestUtil.CheckAgainstBaseline(testName, image);
        }

        [Test]
        public void SimpleLines() {
            CheckDrawing("iosdrawing/simplelines", 1000, 1000,
                grTarget => {
                    object penKey1 = new object();
                    grTarget.CreatePen(penKey1, CmykColor.FromRgb(0, 0, 55), 20, LineCap.Flat, LineJoin.Bevel, 1);
                    grTarget.DrawLine(penKey1, new PointF(100, 100), new PointF(600, 150));

                    object brushKey2 = new object();
                    grTarget.CreateSolidBrush(brushKey2, LightBlue);
                    object penKey2 = new object();
                    grTarget.CreatePen(penKey2, brushKey2, 40, LineCap.Round, LineJoin.Round, 1);
                    grTarget.DrawLine(penKey2, new PointF(100, 200), new PointF(600, 250));
                    
                    object penKey3 = new object();
                    grTarget.CreatePen(penKey3, IndianRed, 80, LineCap.Square, LineJoin.Miter, 2);
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
                    
                
                }
            );
        }

        [Test]
        public void ComplexLines()
        {
            CheckDrawing("iosdrawing/compexlines", 1000, 1200,
                        grTarget =>
                      {
                object penKey1 = new object();
                grTarget.CreatePen(penKey1, DarkBlue, 20, LineCap.Round, LineJoin.Round, 1);
                
                grTarget.DrawRectangle(penKey1, RectangleF.FromLTRB(100, 200, 600, 400));
                grTarget.DrawEllipse(penKey1, new PointF(350, 550), 250, 70);
                grTarget.DrawArc(penKey1, new PointF(500, 850), 150, 30, 200);

                object penKey2 = new object();
                grTarget.CreatePen(penKey2, IndianRed, 3, LineCap.Round, LineJoin.Round, 1);
                grTarget.DrawRectangle(penKey2, RectangleF.FromLTRB(100, 800, 600, 1000));
            }
            );
        }
        

        [Test]
        public void SimpleFill()
        {
            CheckDrawing("iosdrawing/simplefill", 1000, 1000,
                        grTarget =>
                        {
                            object brushKey1 = new object();
                            grTarget.CreateSolidBrush(brushKey1, CmykColor.FromRgb(1.0F, 0.3F, 0.0F));
                            
                            grTarget.FillRectangle(brushKey1, RectangleF.FromLTRB(100, 200, 600, 400));
                            grTarget.FillEllipse(brushKey1, new PointF(350, 550), 250, 70);
                            
                            object brushKey2 = new object();
                            grTarget.CreateSolidBrush(brushKey2, CmykColor.FromRgb(0, 0.5F, 0.25F));
                            grTarget.FillPolygon(brushKey2, new PointF[] 
                                                 { new PointF(100,1000), new PointF(200, 800), new PointF(300, 1000), new PointF(100, 850), new PointF(300,850) },
                                                 FillMode.Alternate);
                            
                            grTarget.FillPolygon(brushKey2, new PointF[] 
                                                 { new PointF(500, 1000), new PointF(600, 800), new PointF(700, 1000), new PointF(500, 850), new PointF(700, 850) },
                                                 FillMode.Winding);
                            
                        });

        }

        [Test]
        public void Paths()
        {
            CheckDrawing("iosdrawing/paths", 1000, 1000,
                        grTarget =>
                      {
                Matrix mat = new Matrix();
                mat.Translate(425, 250);
                grTarget.PushTransform(mat);
                
                object penKey1 = new object(), pathKey1 = new Object();
                grTarget.CreatePen(penKey1, CmykColor.FromRgb(1, 0, 0), 7.0F, LineCap.Flat, LineJoin.Miter, 5F);
                    grTarget.CreatePath(pathKey1, new List<GraphicsPathPart>() {
                    new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(-70, -70)}),
                    new GraphicsPathPart(GraphicsPathPartKind.Lines,  new PointF[] { new PointF(-10, 50), new PointF(20, 50), new PointF(70, -90) })},
                FillMode.Alternate);
                grTarget.DrawPath(penKey1, pathKey1);
                
                object penKey2 = new object(), pathKey2 = new Object();
                grTarget.CreatePen(penKey2, CmykColor.FromRgb(0, 1, 0), 3.0F, LineCap.Round, LineJoin.Round, 5F);
                    grTarget.CreatePath(pathKey2, new List<GraphicsPathPart>() {
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
                grTarget.CreateSolidBrush(brushKey3, IndianRed);
                    grTarget.CreatePath(pathKey3, new List<GraphicsPathPart>() {
                    new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(-70, -70)}),
                    new GraphicsPathPart(GraphicsPathPartKind.Beziers,  new PointF[] { new PointF(-10, 50), new PointF(20, 50), new PointF(70, -90) }),
                    new GraphicsPathPart(GraphicsPathPartKind.Close, new PointF[0] )},
                FillMode.Alternate);
                grTarget.FillPath(brushKey3, pathKey3);
                
            }
            );
        }

        [Test]
        public void Paths2()
        {
            CheckDrawing("iosdrawing/paths", 1000, 1000,
                grTarget =>
                {
                    Matrix mat = new Matrix();
                    mat.Translate(425, 250);
                    grTarget.PushTransform(mat);

                    object penKey1 = new object();
                    grTarget.CreatePen(penKey1, CmykColor.FromRgb(1, 0, 0), 7.0F, LineCap.Flat, LineJoin.Miter, 5F);
                    grTarget.DrawPath(penKey1, new List<GraphicsPathPart>() {
                        new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(-70, -70)}),
                        new GraphicsPathPart(GraphicsPathPartKind.Lines,  new PointF[] { new PointF(-10, 50), new PointF(20, 50), new PointF(70, -90) })});
 
                    object penKey2 = new object();
                    grTarget.CreatePen(penKey2, CmykColor.FromRgb(0, 1, 0), 3.0F, LineCap.Round, LineJoin.Round, 5F);
                    grTarget.DrawPath(penKey2, new List<GraphicsPathPart>() {
                        new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(-70, -70)}),
                        new GraphicsPathPart(GraphicsPathPartKind.Beziers,  new PointF[] { new PointF(-10, 50), new PointF(20, 50), new PointF(70, -90) }),
                        new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(0, 0)}),
                        new GraphicsPathPart(GraphicsPathPartKind.Lines,  new PointF[] { new PointF(-50, 50), new PointF(50, 50) }),
                        new GraphicsPathPart(GraphicsPathPartKind.Close, new PointF[0] )});

                    mat = new Matrix();
                    mat.Translate(0, 350);
                    grTarget.PushTransform(mat);

                    object brushKey3 = new object();
                    grTarget.CreateSolidBrush(brushKey3, IndianRed);
                    grTarget.FillPath(brushKey3, new List<GraphicsPathPart>() {
                        new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(-70, -70)}),
                        new GraphicsPathPart(GraphicsPathPartKind.Beziers,  new PointF[] { new PointF(-10, 50), new PointF(20, 50), new PointF(70, -90) }),
                        new GraphicsPathPart(GraphicsPathPartKind.Close, new PointF[0] )},
                        FillMode.Alternate);
                }
            );
        }

        [Test]
        public void Clipping()
        {
            CheckDrawing("iosdrawing/clipping", 1000, 1000,
                        grTarget =>
                      {
                Matrix mat = new Matrix();
                mat.Translate(425, 250);
                mat.Scale(2, 2);
                grTarget.PushTransform(mat);
                
                object pathKey = new object(), brushKey = new object(), penKey = new object();
                
                grTarget.CreatePath(pathKey, new List<GraphicsPathPart>() {
                    new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(-50, -30)}),
                    new GraphicsPathPart(GraphicsPathPartKind.Lines,  new PointF[] {new PointF(0, 80), new PointF(50, -30), new PointF(-50, 50), new PointF(50, 50)})},
                FillMode.Alternate);
                
                grTarget.PushClip(pathKey);
                
                grTarget.CreateSolidBrush(brushKey, CmykColor.FromRgb(1, 0, 0));
                grTarget.FillRectangle(brushKey, new RectangleF(-100, -100, 200, 200));
                
                grTarget.CreatePen(penKey, CmykColor.FromRgb(0, 1, 0), 15.0F, LineCap.Flat, LineJoin.Bevel, 5F);
                grTarget.DrawEllipse(penKey, new PointF(0, 0), 40, 50);
                
                grTarget.PopClip();

                mat = new Matrix();
                mat.Translate(0, 200);
                grTarget.PushTransform(mat);

                object pathKey2 = new object();
                
                grTarget.CreatePath(pathKey2, new List<GraphicsPathPart>() {
                    new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(-50, -30)}),
                    new GraphicsPathPart(GraphicsPathPartKind.Lines,  new PointF[] {new PointF(0, 80), new PointF(50, -30), new PointF(-50, 50), new PointF(50, 50)})},
                FillMode.Winding);
                
                grTarget.PushClip(pathKey2);
                
                grTarget.FillRectangle(brushKey, new RectangleF(-100, -100, 200, 200));
                grTarget.DrawEllipse(penKey, new PointF(0, 0), 40, 50);
                
                grTarget.PopClip();
            }
            );
        }

        [Test]
        public void Clipping2()
        {
            CheckDrawing("iosdrawing/clipping", 1000, 1000,
                grTarget =>
                {
                    Matrix mat = new Matrix();
                    mat.Translate(425, 250);
                    mat.Scale(2, 2);
                    grTarget.PushTransform(mat);

                    object penKey = new object(), brushKey = new object();

                    grTarget.PushClip(new List<GraphicsPathPart>() {
                        new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(-50, -30)}),
                        new GraphicsPathPart(GraphicsPathPartKind.Lines,  new PointF[] {new PointF(0, 80), new PointF(50, -30), new PointF(-50, 50), new PointF(50, 50)})},
                        FillMode.Alternate);


                    grTarget.CreateSolidBrush(brushKey, CmykColor.FromRgb(1, 0, 0));
                    grTarget.FillRectangle(brushKey, new RectangleF(-100, -100, 200, 200));

                    grTarget.CreatePen(penKey, CmykColor.FromRgb(0, 1, 0), 15.0F, LineCap.Flat, LineJoin.Bevel, 5F);
                    grTarget.DrawEllipse(penKey, new PointF(0, 0), 40, 50);

                    grTarget.PopClip();

                    mat = new Matrix();
                    mat.Translate(0, 200);
                    grTarget.PushTransform(mat);


                    grTarget.PushClip(new List<GraphicsPathPart>() {
                        new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(-50, -30)}),
                        new GraphicsPathPart(GraphicsPathPartKind.Lines,  new PointF[] {new PointF(0, 80), new PointF(50, -30), new PointF(-50, 50), new PointF(50, 50)})},
                        FillMode.Winding);

                    grTarget.FillRectangle(brushKey, new RectangleF(-100, -100, 200, 200));
                    grTarget.DrawEllipse(penKey, new PointF(0, 0), 40, 50);

                    grTarget.PopClip();
                }
            );
        }

        [Test]
        public void Clipping3()
        {
            CheckDrawing("iosdrawing/clipping3", 1000, 1000,
                grTarget =>
                {
                    Matrix mat = new Matrix();
                    mat.Translate(425, 250);
                    mat.Scale(2, 2);
                    grTarget.PushTransform(mat);

                    object penKey = new object(), brushKey = new object();

                    grTarget.PushClip(RectangleF.FromLTRB(-50, -30, 50, 30));

                    grTarget.CreateSolidBrush(brushKey, CmykColor.FromRgb(1, 0, 0));
                    grTarget.FillRectangle(brushKey, new RectangleF(-100, -100, 200, 200));

                    grTarget.CreatePen(penKey, CmykColor.FromRgb(0, 1, 0), 15.0F, LineCap.Flat, LineJoin.Bevel, 5F);
                    grTarget.DrawEllipse(penKey, new PointF(0, 0), 40, 50);

                    grTarget.PopClip();

                    mat = new Matrix();
                    mat.Translate(0, 200);
                    grTarget.PushTransform(mat);

                    grTarget.PushClip(RectangleF.FromLTRB(-30, -50, 30, 50));

                    grTarget.FillRectangle(brushKey, new RectangleF(-100, -100, 200, 200));
                    grTarget.DrawEllipse(penKey, new PointF(0, 0), 40, 50);

                    grTarget.PopClip();
                }
            );
        }


        [Test]
        public void Text()
        {
 
            CheckDrawing("iosdrawing/text", 1000, 1000,
                        grTarget =>
                      {
                object fontKey1 = new object(), fontKey2 = new object(), brushKey = new object();
                object penKey = new object();

                grTarget.CreatePen(penKey, IndianRed, 1, LineCap.Flat, LineJoin.Round, 5);

                grTarget.DrawLine(penKey, new PointF(80, 100), new PointF(120, 100));
                grTarget.DrawLine(penKey, new PointF(100, 80), new PointF(100, 120));
                grTarget.CreateFont(fontKey1, "Times New Roman", 90, true, false);
                grTarget.CreateSolidBrush(brushKey, DarkBlue);
                grTarget.DrawText("Hello", fontKey1, brushKey, new PointF(100, 100));
                
                grTarget.CreateFont(fontKey2, "Trebuchet MS", 90, false, false);

                Matrix mat = new Matrix();
                mat.RotateAt(45, new PointF(100, 200));
                grTarget.PushTransform(mat);
                grTarget.DrawLine(penKey, new PointF(80, 200), new PointF(120, 200));
                grTarget.DrawLine(penKey, new PointF(100, 180), new PointF(100, 220));
                grTarget.DrawText("Hello", fontKey2, brushKey, new PointF(100, 200));
                grTarget.PopTransform();
                
                object fontKey3 = new object(), penKey2 = new object();
                grTarget.DrawLine(penKey, new PointF(80, 500), new PointF(120, 500));
                grTarget.DrawLine(penKey, new PointF(100, 480), new PointF(100, 520));
                grTarget.CreateFont(fontKey3, "Times New Roman", 170, false, true);
                grTarget.CreatePen(penKey2, CmykColor.FromRgb(1, 0, 0), 7, LineCap.Round, LineJoin.Round, 2);
                grTarget.DrawTextOutline("Hi There", fontKey3, penKey2, new PointF(100, 500));
                
            }
            );
        }

        [Test]
        public void PatternBrush() {
            CheckDrawing("iosdrawing/patternbrush", 800, new RectangleF(-103, -117, 200, 200),
                        grTarget => 
            {
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
        public void RotatedPatternBrush() {
            CheckDrawing("iosdrawing/rotatedpatternbrush", 800, new RectangleF(-103, -117, 200, 200),
                         grTarget => 
                         {
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
        public void BitmapDrawing()
        {
            CheckDrawing("iosdrawing/bitmap", 1000, 1000,
                        grTarget =>
            {
                IGraphicsBitmap bitmap = TestUtil.LoadInputBitmap("iosdrawing/Jellyfish.jpg");

                grTarget.DrawBitmap(bitmap, new RectangleF(100, 100, 800, 400), BitmapScaling.HighQuality, 0);

                grTarget.DrawBitmapPart(bitmap, bitmap.PixelWidth * 3 / 10, bitmap.PixelHeight * 2 / 10, bitmap.PixelWidth * 5 / 10, bitmap.PixelHeight * 4 / 10, 
                                        new RectangleF(100, 500, 800, 400), BitmapScaling.NearestNeighbor, 0);

                bitmap.Dispose();
            });
        }

        [Test]
        public void TextMetrics()
        {
            ITextMetrics textMetrics = new IOS_TextMetrics();
            
            Assert.IsFalse(textMetrics.TextFaceIsInstalled("Banana"));
            Assert.IsTrue(textMetrics.TextFaceIsInstalled("Times New Roman"));
            Assert.IsTrue(textMetrics.TextFaceIsInstalled("Trebuchet MS"));

            ITextFaceMetrics tnrMetrics = textMetrics.GetTextFaceMetrics("Times New Roman", 25, false, false);
            Assert.AreEqual(25.0F, tnrMetrics.EmHeight, 0.1F);
            Assert.AreEqual(22.28F, tnrMetrics.Ascent, 0.1F);
            Assert.AreEqual(5.41F, tnrMetrics.Descent, 0.1F);
            Assert.AreEqual(16.55F, tnrMetrics.CapHeight, 0.1F);
            Assert.AreEqual(6.25F, tnrMetrics.SpaceWidth, 0.1F);
            Assert.AreEqual(138.87F, tnrMetrics.GetTextWidth("Hello, world  "), 0.1F);
            Assert.AreEqual(27.69F, tnrMetrics.GetTextSize("Hello, world").Height, 0.1F);
            
            tnrMetrics = textMetrics.GetTextFaceMetrics("Trebuchet MS", 50, false, false);
            Assert.AreEqual(50.0F, tnrMetrics.EmHeight, 0.1F);
            Assert.AreEqual(46.95F, tnrMetrics.Ascent, 0.1F);
            Assert.AreEqual(11.11F, tnrMetrics.Descent, 0.1F);
            Assert.AreEqual(36.08F, tnrMetrics.CapHeight, 0.1F);
            Assert.AreEqual(15.06F, tnrMetrics.SpaceWidth, 0.1F);
            Assert.AreEqual(305.93F, tnrMetrics.GetTextWidth("Hello, world  "), 0.1F);
            Assert.AreEqual(58.06F, tnrMetrics.GetTextSize("Hello, world").Height, 0.1F);
        }
    }
}
