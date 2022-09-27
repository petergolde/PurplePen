using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CanvasTest2.Models;
using CanvasTest2.Drawing;
using PurplePen.Graphics2D;
using System.Drawing;
using System.Drawing.Drawing2D;
using PurplePen.MapModel;
using System.IO;

namespace CanvasTest2.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        private string DrawMap(Map map, Size canvasSize, RectangleF mapArea)
        {
            // Calculate the transform matrix.
            PointF midpoint = new PointF(canvasSize.Width / 2.0F, canvasSize.Height / 2.0F);
            float scaleFactor = Math.Min((float)canvasSize.Width / mapArea.Width, (float)canvasSize.Height / mapArea.Height);
            PointF centerPoint = new PointF((mapArea.Left + mapArea.Right) / 2, (mapArea.Top + mapArea.Bottom) / 2);
            Matrix matrix = new Matrix();
            matrix.Translate(midpoint.X, midpoint.Y, MatrixOrder.Prepend);
            matrix.Scale(scaleFactor, -scaleFactor, MatrixOrder.Prepend);  // y scale is negative to get to cartesian orientation.
            matrix.Translate(-centerPoint.X, -centerPoint.Y, MatrixOrder.Prepend);

            string commandString;

            RenderOptions renderOpts = new RenderOptions();
            renderOpts.usePatternBitmaps = false;
            renderOpts.renderTemplates = RenderTemplateOption.MapAndTemplates;
            renderOpts.minResolution = 1.0F/scaleFactor;            

            using (BrowserCanvasGraphicsTarget grTarget = new BrowserCanvasGraphicsTarget()) {
                grTarget.PushTransform(matrix);
                using (map.Read())
                    map.Draw(grTarget, mapArea, renderOpts, null);

                commandString = grTarget.GetCommandString();
            }

            return commandString;
        }

        private string TestMap(string mapTestFileName, int width, int height)
        {
            string pngFileName;
            string mapFileName;
            RectangleF mapArea;
            Size size;

            // Read the test file, and get the other file names and the area.
            using (StreamReader reader = new StreamReader(mapTestFileName)) {
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

            string directoryName = Path.GetDirectoryName(mapTestFileName);
            mapFileName = Path.Combine(directoryName, mapFileName);

            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(directoryName));
            InputOutput.ReadFile(mapFileName, map);
            return DrawMap(map, new Size(width, height), mapArea);
        }

        private string TestDrawing(Action<IGraphicsTarget> drawFunction)
        {
            BrowserCanvasGraphicsTarget grTarget = new BrowserCanvasGraphicsTarget();
            drawFunction(grTarget);
            string commandString = grTarget.GetCommandString();
            grTarget.Dispose();
            return commandString;
        }

        public string TestDrawMap()
        {
            string directory = @"C:\Users\peter\Documents\Programs\MapModel\src\TestFiles\rendering";
            string mapFileName = Path.Combine(directory, @"teanwest11.txt");
            return TestMap(mapFileName, 3200, 2400);
        }

        public string TestDraw1()
        {
            return TestDrawing(grTarget => {
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
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
