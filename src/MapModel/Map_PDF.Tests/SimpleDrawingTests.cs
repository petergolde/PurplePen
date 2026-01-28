using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PurplePen.MapModel;
using PurplePen.Graphics2D;
using TestingUtils;
using System.IO;


namespace Map_PDF.Tests
{
    [TestFixture, Parallelizable(ParallelScope.Children)]
    public class SimpleDrawingTests
    {
        void CreatePdf(string testName, bool useCmyk, Action<IGraphicsTarget> draw)
        {
            string pdfFileName = TestUtil.GetTestFile("pdfrender\\" + testName) + "_temp.pdf";
            string pngFileName = TestUtil.GetTestFile("pdfrender\\" + testName) + "_converted.png";
            string baselineFileName = TestUtil.GetTestFile("pdfrender\\" + testName) + ".png";

            PdfCreation.CreatePdfAndPng(pdfFileName, pngFileName, 850, 1100, useCmyk, draw);

            // Load PNG
            using (Bitmap bitmapNew = (Bitmap)(Image.FromFile(pngFileName)))
            {
                TestUtil.CompareBitmapBaseline(bitmapNew, baselineFileName);
            }

            File.Delete(pngFileName);
        }

        void CreatePdfUsingCopiedPage(string testName, string importedPdf, int importedPage, Action<IGraphicsTarget> draw)
        {
            string pdfFileName = TestUtil.GetTestFile("pdfrender\\" + testName) + "_temp.pdf";
            string pngFileName = TestUtil.GetTestFile("pdfrender\\" + testName) + "_converted.png";
            string baselineFileName = TestUtil.GetTestFile("pdfrender\\" + testName) + ".png";

            PdfCreation.CreatePdfAndPngUsingCopiedPage(pdfFileName, pngFileName, importedPdf, importedPage, draw);

            // Load PNG
            using (Bitmap bitmapNew = (Bitmap)(Image.FromFile(pngFileName))) {
                TestUtil.CompareBitmapBaseline(bitmapNew, baselineFileName);
            }

            File.Delete(pngFileName);
        }

        void CreatePdfUsingCopiedPartialPage(string testName, string importedPdf, int importedPage, SizeF paperSize, RectangleF sourceRectangleInInches, RectangleF destRectangleInInches, Action<IGraphicsTarget> draw)
        {
            string pdfFileName = TestUtil.GetTestFile("pdfrender\\" + testName) + "_temp.pdf";
            string pngFileName = TestUtil.GetTestFile("pdfrender\\" + testName) + "_converted.png";
            string baselineFileName = TestUtil.GetTestFile("pdfrender\\" + testName) + ".png";

            PdfCreation.CreatePdfAndPngUsingCopiedPartialPage(pdfFileName, pngFileName, importedPdf, importedPage, 
                                                              paperSize, sourceRectangleInInches, destRectangleInInches, draw);

            // Load PNG
            using (Bitmap bitmapNew = (Bitmap)(Image.FromFile(pngFileName))) {
                TestUtil.CompareBitmapBaseline(bitmapNew, baselineFileName);
            }

            File.Delete(pngFileName);
        }

        private void ConvertPdfToPng(string pdfFileName, string pngFileName)
        {
            string arguments = String.Format(
                @"-dSTRICT -dSAFER -dBATCH -dNOPAUSE -r100  -dTextAlphaBits=4 -dGraphicsAlphaBits=4 -sDEVICE=png16m -sOutputFile={1} {0}", 
                pdfFileName, pngFileName);

            ProcessStartInfo startInfo = new ProcessStartInfo(TestUtil.GetToolFullPath("gswin32c.exe"), arguments);
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Process process = Process.Start(startInfo);
            process.WaitForExit();
        }

        [Test]
        public void SimpleLines() {
            CreatePdf("simplelines", false,
                grTarget => {
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

                
                }
            );
        }

        [Test]
        public void ComplexLines()
        {
            CreatePdf("complexlines", false,
                grTarget =>
                {
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
            CreatePdf("fillareas", false,
                grTarget =>
                {
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
            CreatePdf("paths", false,
               grTarget =>
               {
                   Matrix mat = new Matrix();
                   mat.Translate(425, 250);
                   grTarget.PushTransform(mat);

                   object penKey1 = new object(), pathKey1 = new Object();
                   grTarget.CreatePen(penKey1, CmykColor.FromColor(Color.Red), 7.0F, LineCap.Flat, LineJoin.Miter, 5F);
                   grTarget.CreatePath(pathKey1, new List<GraphicsPathPart> {
                            new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[1] {new PointF(-70, -70)}),
                            new GraphicsPathPart(GraphicsPathPartKind.Lines,  new PointF[] { new PointF(-10, 50), new PointF(20, 50), new PointF(70, -90) })},
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

            CreatePdf("clipping", false,
                grTarget =>
                {
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

            CreatePdf("text", false,
                grTarget =>
                {
                    object fontKey = new object(), brushKey = new object();

                    grTarget.CreateFont(fontKey, "Times New Roman", 90, TextEffects.None);
                    grTarget.CreateSolidBrush(brushKey, CmykColor.FromColor(Color.BlueViolet));
                    grTarget.DrawText("Hello", fontKey, brushKey, new PointF(100, 100));

                    Matrix mat = new Matrix();
                    mat.RotateAt(45, new PointF(100, 200));
                    grTarget.PushTransform(mat);
                    grTarget.DrawText("Hello", fontKey, brushKey, new PointF(100, 200));
                    grTarget.PopTransform();

                    object fontKey2 = new object(), penKey2 = new object();
                    grTarget.CreateFont(fontKey2, "Times New Roman", 170, TextEffects.Bold | TextEffects.Italic);
                    grTarget.CreatePen(penKey2, CmykColor.FromColor(Color.Crimson), 7, LineCap.Round, LineJoin.Round, 2);
                    grTarget.DrawTextOutline("Hi There", fontKey2, penKey2, new PointF(100, 500));

                }
            );
        }

        [Test]
        public void Bitmap()
        {
            CreatePdf("drawbitmap", false,
                grTarget =>
                {
                    GDIPlus_Bitmap penguins = new GDIPlus_Bitmap((Bitmap) Image.FromFile(TestUtil.GetTestFile("pdfrender\\penguins.jpg")));
                    grTarget.DrawBitmap(penguins, new RectangleF(100, 100, 600, 400), BitmapScaling.MediumQuality, 0.01F);
                    grTarget.DrawBitmap(penguins, new RectangleF(200, 800, 150, 100), BitmapScaling.MediumQuality, 0.01F);
                }
            );
        }

        [Test]
        public void BitmapPart()
        {
            CreatePdf("drawbitmappart", false,
                grTarget =>
                {
                    GDIPlus_Bitmap bitmap = new GDIPlus_Bitmap((Bitmap) Image.FromFile(TestUtil.GetTestFile("pdfrender\\penguins.jpg")));
                    grTarget.DrawBitmapPart(bitmap, bitmap.PixelWidth * 3 / 10, bitmap.PixelHeight * 2 / 10, bitmap.PixelWidth * 5 / 10, bitmap.PixelHeight * 4 / 10,
                                    new RectangleF(100, 500, 600, 400), BitmapScaling.NearestNeighbor, 0.01F);
                }
            );
        }

        [Test]
        public void Blending1()
        {
            CreatePdf("blending1", false,
                grTarget => {
                    const int penWidth = 20;
                    CmykColor blue = CmykColor.FromColor(Color.Blue);
                    CmykColor green = CmykColor.FromColor(Color.Green);
                    CmykColor yellow = CmykColor.FromColor(Color.Yellow);
                    CmykColor black = CmykColor.FromColor(Color.Black);
                    CmykColor purple = CmykColor.FromCmyk(0.1F, 0.9F, 0, 0.1F);

                    Matrix transform = new Matrix();
                    transform.Translate(425, 550);
                    transform.Scale(3, -3);
                    grTarget.PushTransform(transform);

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

                    grTarget.PopTransform();
                }
            );
        }

        // Same as blending, but uses CMYK.
        [Test]
        public void Blending2()
        {
            CreatePdf("blending2", true,
                grTarget => {
                    const int penWidth = 20;
                    CmykColor blue = CmykColor.FromColor(Color.Blue);
                    CmykColor green = CmykColor.FromColor(Color.Green);
                    CmykColor yellow = CmykColor.FromColor(Color.Yellow);
                    CmykColor black = CmykColor.FromColor(Color.Black);
                    CmykColor purple = CmykColor.FromCmyk(0.1F, 0.9F, 0, 0.1F);

                    Matrix transform = new Matrix();
                    transform.Translate(425, 550);
                    transform.Scale(3, -3);
                    grTarget.PushTransform(transform);

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

                    grTarget.PopTransform();
                }
            );
        }

        // Doesn't blend (in CMYK).
        [Test]
        public void Blending3()
        {
            CreatePdf("blending3", true,
                grTarget => {
                    const int penWidth = 20;
                    CmykColor blue = CmykColor.FromColor(Color.Blue);
                    CmykColor green = CmykColor.FromColor(Color.Green);
                    CmykColor yellow = CmykColor.FromColor(Color.Yellow);
                    CmykColor black = CmykColor.FromColor(Color.Black);
                    CmykColor purple = CmykColor.FromCmyk(0.1F, 0.9F, 0, 0.1F);

                    Matrix transform = new Matrix();
                    transform.Translate(425, 550);
                    transform.Scale(3, -3);
                    grTarget.PushTransform(transform);

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

                    grTarget.DrawEllipse(penPurple, new PointF(0, 0), 70, 70);

                    grTarget.PopTransform();
                }
            );
        }

        [Test]
        public void CopiedPageRGB()
        {
            CreatePdfUsingCopiedPage("copypagergb",
                TestUtil.GetTestFile("pdfrender\\Lincoln-RGB.pdf"), 0,
                grTarget => {
                    object penKey1 = new object();
                    grTarget.CreatePen(penKey1, CmykColor.FromColor(Color.DarkBlue), 20, LineCap.Flat, LineJoin.Bevel, 1);
                    grTarget.DrawLine(penKey1, new PointF(100, 100), new PointF(600, 150));

                    object penKey2 = new object();
                    grTarget.CreatePen(penKey2, CmykColor.FromColor(Color.LightBlue), 40, LineCap.Round, LineJoin.Round, 1);
                    grTarget.DrawLine(penKey2, new PointF(100, 200), new PointF(600, 250));

                    object penKey3 = new object();
                    grTarget.CreatePen(penKey3, CmykColor.FromColor(Color.IndianRed), 80, LineCap.Square, LineJoin.Miter, 2);
                    grTarget.DrawLine(penKey3, new PointF(100, 300), new PointF(600, 350));

                    grTarget.DrawPolyline(penKey1, new PointF[] { new PointF(150, 400), new PointF(150, 450), new PointF(400, 400), new PointF(300, 500) });

                    grTarget.DrawPolyline(penKey2, new PointF[] { new PointF(150, 500), new PointF(150, 550), new PointF(400, 500), new PointF(300, 600) });

                    grTarget.DrawPolyline(penKey3, new PointF[] { new PointF(120, 700), new PointF(150, 750), new PointF(400, 700), new PointF(300, 800) });

                    grTarget.DrawPolygon(penKey1, new PointF[] { new PointF(500, 400), new PointF(700, 500), new PointF(650, 600), new PointF(535, 520) });

                    grTarget.DrawPolygon(penKey2, new PointF[] { new PointF(500, 600), new PointF(700, 700), new PointF(650, 800), new PointF(535, 720) });

                    grTarget.DrawPolygon(penKey3, new PointF[] { new PointF(500, 800), new PointF(700, 900), new PointF(650, 1000), new PointF(535, 920) });


                }
            );
        }

        [Test]
        public void CopiedPageCMYK()
        {
            CreatePdfUsingCopiedPage("copypagecmyk",
                TestUtil.GetTestFile("pdfrender\\Lincoln-CMYK.pdf"), 0,
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
        public void CopiedPageCMYK2()
        {
            CreatePdfUsingCopiedPage("copypagecmyk2",
                TestUtil.GetTestFile("pdfrender\\tengesdalslia.pdf"), 0,
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

        SizeF letterSize = new SizeF(8.5F, 11.0F);

        [Test]
        public void CopiedPartialPageRGB()
        {
            CreatePdfUsingCopiedPartialPage("copypartialpagergb",
                TestUtil.GetTestFile("pdfrender\\Lincoln-RGB.pdf"), 0,
                letterSize,
                RectangleF.FromLTRB(2, 3, 7, 8),
                new RectangleF(new PointF(0, 0), letterSize),
                grTarget => {
                    object penKey1 = new object();
                    grTarget.CreatePen(penKey1, CmykColor.FromColor(Color.DarkBlue), 20, LineCap.Flat, LineJoin.Bevel, 1);
                    grTarget.DrawLine(penKey1, new PointF(100, 100), new PointF(600, 150));

                    object penKey2 = new object();
                    grTarget.CreatePen(penKey2, CmykColor.FromColor(Color.LightBlue), 40, LineCap.Round, LineJoin.Round, 1);
                    grTarget.DrawLine(penKey2, new PointF(100, 200), new PointF(600, 250));

                    object penKey3 = new object();
                    grTarget.CreatePen(penKey3, CmykColor.FromColor(Color.IndianRed), 80, LineCap.Square, LineJoin.Miter, 2);
                    grTarget.DrawLine(penKey3, new PointF(100, 300), new PointF(600, 350));

                    grTarget.DrawPolyline(penKey1, new PointF[] { new PointF(150, 400), new PointF(150, 450), new PointF(400, 400), new PointF(300, 500) });

                    grTarget.DrawPolyline(penKey2, new PointF[] { new PointF(150, 500), new PointF(150, 550), new PointF(400, 500), new PointF(300, 600) });

                    grTarget.DrawPolyline(penKey3, new PointF[] { new PointF(120, 700), new PointF(150, 750), new PointF(400, 700), new PointF(300, 800) });

                    grTarget.DrawPolygon(penKey1, new PointF[] { new PointF(500, 400), new PointF(700, 500), new PointF(650, 600), new PointF(535, 520) });

                    grTarget.DrawPolygon(penKey2, new PointF[] { new PointF(500, 600), new PointF(700, 700), new PointF(650, 800), new PointF(535, 720) });

                    grTarget.DrawPolygon(penKey3, new PointF[] { new PointF(500, 800), new PointF(700, 900), new PointF(650, 1000), new PointF(535, 920) });


                }
            );
        }


        [Test]
        public void CopiedPartialPageCMYK()
        {
            CreatePdfUsingCopiedPartialPage("copypartialpagecmyk",
                TestUtil.GetTestFile("pdfrender\\Lincoln-CMYK.pdf"), 0,
                letterSize,
                RectangleF.FromLTRB(2, 3, 7, 8),
                new RectangleF(new PointF(0, 0), letterSize),
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
        public void CopiedPartialPage2()
        {
            CreatePdfUsingCopiedPartialPage("copypartialpage2",
                TestUtil.GetTestFile("pdfrender\\CleElum.pdf"), 0,
                letterSize,
                new RectangleF(9.05F, 5.42F, 4.91F, 3.64F),
                new RectangleF(new PointF(0, 0), letterSize),
                grTarget => {
                    object penKey1 = new object();
                    grTarget.CreatePen(penKey1, CmykColor.FromColor(Color.DarkBlue), 20, LineCap.Flat, LineJoin.Bevel, 1);
                    grTarget.DrawLine(penKey1, new PointF(100, 100), new PointF(600, 150));

                    object penKey2 = new object();
                    grTarget.CreatePen(penKey2, CmykColor.FromColor(Color.LightBlue), 40, LineCap.Round, LineJoin.Round, 1);
                    grTarget.DrawLine(penKey2, new PointF(100, 200), new PointF(600, 250));

                    object penKey3 = new object();
                    grTarget.CreatePen(penKey3, CmykColor.FromColor(Color.IndianRed), 80, LineCap.Square, LineJoin.Miter, 2);
                    grTarget.DrawLine(penKey3, new PointF(100, 300), new PointF(600, 350));

                    grTarget.DrawPolyline(penKey1, new PointF[] { new PointF(150, 400), new PointF(150, 450), new PointF(400, 400), new PointF(300, 500) });

                    grTarget.DrawPolyline(penKey2, new PointF[] { new PointF(150, 500), new PointF(150, 550), new PointF(400, 500), new PointF(300, 600) });

                    grTarget.DrawPolyline(penKey3, new PointF[] { new PointF(120, 700), new PointF(150, 750), new PointF(400, 700), new PointF(300, 800) });

                    grTarget.DrawPolygon(penKey1, new PointF[] { new PointF(500, 400), new PointF(700, 500), new PointF(650, 600), new PointF(535, 520) });

                    grTarget.DrawPolygon(penKey2, new PointF[] { new PointF(500, 600), new PointF(700, 700), new PointF(650, 800), new PointF(535, 720) });

                    grTarget.DrawPolygon(penKey3, new PointF[] { new PointF(500, 800), new PointF(700, 900), new PointF(650, 1000), new PointF(535, 920) });


                }
            );
        }

        [Test]
        public void CopiedPartialPage4()
        {
            CreatePdfUsingCopiedPartialPage("copypartialpage4",
                TestUtil.GetTestFile("pdfrender\\CleElum.pdf"), 0,
                letterSize,
                new RectangleF(9.05F, 5.42F, 4.91F, 3.64F),
                new RectangleF(new PointF(1, 2), new SizeF(5, 4.5F)),
                grTarget => {
                    object penKey1 = new object();
                    grTarget.CreatePen(penKey1, CmykColor.FromColor(Color.DarkBlue), 20, LineCap.Flat, LineJoin.Bevel, 1);
                    grTarget.DrawLine(penKey1, new PointF(100, 100), new PointF(600, 150));

                    object penKey2 = new object();
                    grTarget.CreatePen(penKey2, CmykColor.FromColor(Color.LightBlue), 40, LineCap.Round, LineJoin.Round, 1);
                    grTarget.DrawLine(penKey2, new PointF(100, 200), new PointF(600, 250));

                    object penKey3 = new object();
                    grTarget.CreatePen(penKey3, CmykColor.FromColor(Color.IndianRed), 80, LineCap.Square, LineJoin.Miter, 2);
                    grTarget.DrawLine(penKey3, new PointF(100, 300), new PointF(600, 350));

                    grTarget.DrawPolyline(penKey1, new PointF[] { new PointF(150, 400), new PointF(150, 450), new PointF(400, 400), new PointF(300, 500) });

                    grTarget.DrawPolyline(penKey2, new PointF[] { new PointF(150, 500), new PointF(150, 550), new PointF(400, 500), new PointF(300, 600) });

                    grTarget.DrawPolyline(penKey3, new PointF[] { new PointF(120, 700), new PointF(150, 750), new PointF(400, 700), new PointF(300, 800) });

                    grTarget.DrawPolygon(penKey1, new PointF[] { new PointF(500, 400), new PointF(700, 500), new PointF(650, 600), new PointF(535, 520) });

                    grTarget.DrawPolygon(penKey2, new PointF[] { new PointF(500, 600), new PointF(700, 700), new PointF(650, 800), new PointF(535, 720) });

                    grTarget.DrawPolygon(penKey3, new PointF[] { new PointF(500, 800), new PointF(700, 900), new PointF(650, 1000), new PointF(535, 920) });


                }
            );
        }



        [Test]
        public void CopiedPartialPage3()
        {
            CreatePdfUsingCopiedPartialPage("copypartialpage3",
                TestUtil.GetTestFile("pdfrender\\tengesdalslia.pdf"), 0,
                letterSize,
                RectangleF.FromLTRB(2, 3, 7, 8),
                new RectangleF(new PointF(0, 0), letterSize),
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
        public void CopiedPartialPage5()
        {
            CreatePdfUsingCopiedPartialPage("copypartialpage5",
                TestUtil.GetTestFile("pdfrender\\tengesdalslia.pdf"), 0,
                letterSize,
                RectangleF.FromLTRB(2, 3, 7, 8),
                new RectangleF(new PointF(1, 1.5F), new SizeF(7, 8)),
                grTarget => {
                    object penKey1 = new object();
                    grTarget.CreatePen(penKey1, CmykColor.FromColor(Color.DarkBlue), 20, LineCap.Round, LineJoin.Round, 1);

                    grTarget.DrawRectangle(penKey1, RectangleF.FromLTRB(100, 200, 600, 400));
                    grTarget.DrawEllipse(penKey1, new PointF(350, 550), 250, 70);
                    grTarget.DrawArc(penKey1, new PointF(500, 850), 150, 30, 200);

                    object penKey2 = new object();
                    grTarget.CreatePen(penKey2, CmykColor.FromColor(Color.IndianRed), 3, LineCap.Round, LineJoin.Round, 1);
                    grTarget.DrawRectangle(penKey2, RectangleF.FromLTRB(100, 150, 100 + 700, 150 + 800));
                }
            );
        }

    }
}
