using PurplePen.Graphics2D;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ColorConverter = PurplePen.Graphics2D.ColorConverter;

namespace CanvasTest2.Drawing
{
    public class BrowserCanvasGraphicsTarget : IGraphicsTarget
    {
        private StringWriter outputWriter;
        private BrowserCanvasColorConverter colorConverter;

        private const char argTerminator = ';';
        private const char cmdTerminator = '|';

        private class IdentityComparer<T> : IEqualityComparer<T>
        {
            public bool Equals(T x, T y)
            {
                return ((object)x == (object)y);
            }

            public int GetHashCode(T obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }

        // Remember information about a path.
        private class PathInfo
        {
            public readonly List<GraphicsPathPart> parts;
            public readonly FillMode windingMode;
            public PathInfo(List<GraphicsPathPart> parts, FillMode windingMode)
            {
                this.parts = new List<GraphicsPathPart>(parts.Count);
                this.parts.AddRange(parts);
                this.windingMode = windingMode;
            }
        }

        // Map holding paths.
        private Dictionary<object, PathInfo> pathDict = new Dictionary<object, PathInfo>(new IdentityComparer<object>());

        // Remember information about a pen.
        private class PenInfo
        {
            public readonly CmykColor color;
            public readonly float width;
            public readonly LineCap caps;
            public readonly LineJoin join;
            public readonly float miterLimit;
            public PenInfo(CmykColor color, float width, LineCap caps, LineJoin join, float miterLimit)
            {
                this.color = color;
                this.width = width;
                this.caps = caps;
                this.join = join;
                this.miterLimit = miterLimit;
            }
        }

        // Map holding pens.
        private Dictionary<object, PenInfo> penDict = new Dictionary<object, PenInfo>(new IdentityComparer<object>());

        // Remember information about a brush.
        private class BrushInfo
        {
            public readonly CmykColor color;
            public BrushInfo(CmykColor color)
            {
                this.color = color;
            }
        }

        // Map holding brushes.
        private Dictionary<object, BrushInfo> brushDict = new Dictionary<object, BrushInfo>(new IdentityComparer<object>());

        public BrowserCanvasGraphicsTarget(BrowserCanvasColorConverter colorConverter = null)
        {
            outputWriter = new StringWriter(CultureInfo.InvariantCulture);
            this.colorConverter = colorConverter ?? new BrowserCanvasColorConverter();
        }

        public string GetCommandString()
        {
            return outputWriter.ToString();
        }

        public bool SupportsPatternBrushes => false;

        public void CreateFont(object key, string familyName, float emHeight, TextEffects effects)
        {
            // NOT YET IMPLEMENTED
        }

        public void CreatePath(object key, List<GraphicsPathPart> parts, FillMode windingMode)
        {
            if (pathDict.ContainsKey(key)) {
                throw new ArgumentException("key is already assigned to a path");
            }

            pathDict[key] = new PathInfo(parts, windingMode);
        }

        public IBrushTarget CreatePatternBrush(SizeF size, float angle, int bitmapWidth, int bitmapHeight)
        {
            throw new NotImplementedException();
        }

        public void CreatePen(object key, object brushKey, float width, LineCap caps, LineJoin join, float miterLimit)
        {
            if (penDict.ContainsKey(key)) {
                throw new ArgumentException("key is already assigned to a pen");
            }

            CmykColor color = brushDict[brushKey].color;
            penDict[key] = new PenInfo(color, width, caps, join, miterLimit);
        }

        public void CreatePen(object key, CmykColor color, float width, LineCap caps, LineJoin join, float miterLimit)
        {
            if (penDict.ContainsKey(key)) {
                throw new ArgumentException("key is already assigned to a pen");
            }

            penDict[key] = new PenInfo(color, width, caps, join, miterLimit);
        }

        public void CreateSolidBrush(object key, CmykColor color)
        {
            if (brushDict.ContainsKey(key)) {
                throw new ArgumentException("key is already assigned to a brush");
            }

            brushDict[key] = new BrushInfo(color);
        }

        public void Dispose()
        {
        }

        public void DrawArc(object penKey, PointF center, float radius, float startAngle, float sweepAngle)
        {
            UsePen(penKey);

            BeginCommand('a'); // arc
            FloatArgument(center.X);
            FloatArgument(center.Y);
            FloatArgument(radius);
            FloatArgument((float)(startAngle * Math.PI / 180.0F));
            FloatArgument((float)((startAngle + sweepAngle) * Math.PI / 180.0F));
            EndCommand();

        }

        public void DrawBitmap(IGraphicsBitmap bm, RectangleF rectangle, BitmapScaling scalingMode, float minResolution)
        {
            //throw new NotImplementedException();
        }

        public void DrawBitmapPart(IGraphicsBitmap bm, int x, int y, int width, int height, RectangleF rectange, BitmapScaling scalingMode, float minResolution)
        {
            //throw new NotImplementedException();
        }

        public void DrawEllipse(object penKey, PointF center, float radiusX, float radiusY)
        {
            UsePen(penKey);
            BeginCommand('e'); // drawEllipse
            FloatArgument(center.X - radiusX);
            FloatArgument(center.Y - radiusY);
            FloatArgument(center.X + radiusX);
            FloatArgument(center.Y + radiusY);
            EndCommand();
        }

        public void DrawLine(object penKey, PointF start, PointF finish)
        {
            UsePen(penKey);
            BeginCommand('p'); EndCommand();
            BeginCommand('m'); FloatArgument(start.X); FloatArgument(start.Y); EndCommand();
            BeginCommand('l'); FloatArgument(finish.X); FloatArgument(finish.Y); EndCommand();
            BeginCommand('d'); EndCommand();
        }

        public void DrawPath(object penKey, object pathKey)
        {
            UsePen(penKey);
            UsePath(pathKey);
            BeginCommand('d'); EndCommand();  // draw path
        }

        public void DrawPath(object penKey, List<GraphicsPathPart> parts)
        {
            UsePen(penKey);
            UsePath(parts);
            BeginCommand('d'); EndCommand();  // draw path
        }

        public void DrawPolygon(object penKey, PointF[] pts)
        {
            if (pts.Length == 0)
                return;

            UsePen(penKey);
            BeginCommand('p'); EndCommand(); // beginPath
            BeginCommand('m'); // moveto
            FloatArgument(pts[0].X);
            FloatArgument(pts[0].Y);
            EndCommand();
            for (int i = 1; i < pts.Length; ++i) {
                BeginCommand('l'); // lineto
                FloatArgument(pts[i].X);
                FloatArgument(pts[i].Y);
                EndCommand();
            }
            BeginCommand('c'); EndCommand(); // closePath
            BeginCommand('d'); EndCommand(); // drawPath
        }

        public void DrawPolyline(object penKey, PointF[] pts)
        {
            if (pts.Length == 0)
                return;

            UsePen(penKey);
            BeginCommand('p'); EndCommand(); // beginPath
            BeginCommand('m'); // moveto
            FloatArgument(pts[0].X);
            FloatArgument(pts[0].Y);
            EndCommand();
            for (int i = 1; i < pts.Length; ++i) {
                BeginCommand('l'); // lineto
                FloatArgument(pts[i].X);
                FloatArgument(pts[i].Y);
                EndCommand();
            }
            BeginCommand('d'); EndCommand(); // drawPath
        }

        public void DrawRectangle(object penKey, RectangleF rect)
        {
            UsePen(penKey);
            BeginCommand('r'); // drawRect
            FloatArgument(rect.Left);
            FloatArgument(rect.Top);
            FloatArgument(rect.Right);
            FloatArgument(rect.Bottom);
            EndCommand();
        }

        public void DrawText(string text, object fontKey, object brushKey, PointF upperLeft)
        {
            // Not implemented yet.
        }

        public void DrawTextOutline(string text, object fontKey, object penKey, PointF upperLeft)
        {
            // Not implemented yet.
        }

        public void FillEllipse(object brushKey, PointF center, float radiusX, float radiusY)
        {
            UseBrush(brushKey);
            BeginCommand('E'); // fillEllipse
            FloatArgument(center.X - radiusX);
            FloatArgument(center.Y - radiusY);
            FloatArgument(center.X + radiusX);
            FloatArgument(center.Y + radiusY);
            EndCommand();
        }

        public void FillPath(object brushKey, object pathKey)
        {
            UseBrush(brushKey);
            UsePath(pathKey);
            BeginCommand('f');
            IntArgument((pathDict[pathKey].windingMode == FillMode.Alternate) ? 0 : 1);
            EndCommand();
        }

        public void FillPath(object brushKey, List<GraphicsPathPart> parts, FillMode windingMode)
        {
            UseBrush(brushKey);
            UsePath(parts);
            BeginCommand('f');
            IntArgument((windingMode == FillMode.Alternate) ? 0 : 1);
            EndCommand();
        }

        public void FillPolygon(object brushKey, PointF[] pts, FillMode windingMode)
        {
            if (pts.Length == 0)
                return;

            UseBrush(brushKey);
            BeginCommand('p'); EndCommand(); // beginPath
            BeginCommand('m'); // moveto
            FloatArgument(pts[0].X);
            FloatArgument(pts[0].Y);
            EndCommand();
            for (int i = 1; i < pts.Length; ++i) {
                BeginCommand('l'); // lineto
                FloatArgument(pts[i].X);
                FloatArgument(pts[i].Y);
                EndCommand();
            }

            BeginCommand('f'); // fillPath
            IntArgument((windingMode == FillMode.Alternate) ? 0 : 1);
            EndCommand(); 
        }

        public void FillRectangle(object brushKey, RectangleF rect)
        {
            UseBrush(brushKey);
            BeginCommand('R'); // fillRect
            FloatArgument(rect.Left);
            FloatArgument(rect.Top);
            FloatArgument(rect.Right);
            FloatArgument(rect.Bottom);
            EndCommand();
        }

        public bool HasBrush(object brushKey)
        {
            return brushDict.ContainsKey(brushKey);
        }

        public bool HasFont(object fontKey)
        {
            // Not implemented
            return false;
        }

        public bool HasPath(object pathKey)
        {
            return pathDict.ContainsKey(pathKey);
        }

        public bool HasPen(object penKey)
        {
            return penDict.ContainsKey(penKey);
        }

        public void PopAntiAliasing()
        {
            // Can't change anti-aliasing.
        }

        public void PopBlending()
        {
            // No blending
        }

        public void PopClip()
        {
            BeginCommand('Z'); EndCommand(); // restore
        }

        public void PopTransform()
        {
            BeginCommand('Z'); EndCommand(); // restore
        }

        public void PushAntiAliasing(bool antiAlias)
        {
            // Can't change anti-aliasing.
        }

        public bool PushBlending(BlendMode blendMode)
        {
            // no blending
            return false;
        }

        public void PushClip(object pathKey)
        {
            BeginCommand('z'); EndCommand(); // save
            UsePath(pathKey);
            BeginCommand('C');
            IntArgument((pathDict[pathKey].windingMode == FillMode.Alternate) ? 0 : 1);
            EndCommand(); // clipPath
        }

        public void PushClip(RectangleF rectangle)
        {
            PushClip(new RectangleF[] { rectangle });
        }

        public void PushClip(RectangleF[] rectangles)
        {
            List<GraphicsPathPart> parts = new List<GraphicsPathPart>();
            foreach (RectangleF rectangle in rectangles) {
                parts.Add(new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[] { rectangle.TopLeft() }));
                parts.Add(new GraphicsPathPart(GraphicsPathPartKind.Lines, new PointF[] {
                    rectangle.TopRight(), rectangle.BottomRight(), rectangle.BottomLeft()
                }));
                parts.Add(new GraphicsPathPart(GraphicsPathPartKind.Close, new PointF[0]));
            }

            PushClip(parts, FillMode.Winding);
        }

        public void PushClip(List<GraphicsPathPart> parts, FillMode windingMode)
        {
            BeginCommand('z'); EndCommand(); // save

            UsePath(parts);
            BeginCommand('C');
            IntArgument((windingMode == FillMode.Alternate) ? 0 : 1);
            EndCommand(); // clipPath
        }

        public void PushTransform(Matrix matrix)
        {
            BeginCommand('z'); EndCommand(); // save

            float[] elements = matrix.Elements;
            BeginCommand('x'); // transform
            FloatArgument(elements[0]);
            FloatArgument(elements[1]);
            FloatArgument(elements[2]);
            FloatArgument(elements[3]);
            FloatArgument(elements[4]);
            FloatArgument(elements[5]);
            EndCommand();
        }

        private void BeginCommand(char cmd)
        {
            outputWriter.Write(cmd);
        }

        private void EndCommand()
        {
            outputWriter.Write(cmdTerminator);
        }

        private void IntArgument(int i)
        {
            outputWriter.Write(i);
            outputWriter.Write(argTerminator);
        }

        private void FloatArgument(float f)
        {
            outputWriter.Write(f);
            outputWriter.Write(argTerminator);
        }

        private void StringArgument(string s)
        {
            outputWriter.Write(s);
            outputWriter.Write(argTerminator);
        }

        private void ColorArgument(CmykColor color)
        {
            uint colorValue = colorConverter.ToColor(color);
            outputWriter.Write("#{0:X6}", colorValue);
            outputWriter.Write(argTerminator);
        }

        private void UseBrush(object brushKey)
        {
            BrushInfo brushInfo = brushDict[brushKey];
            BeginCommand('S'); // fillStyle
            ColorArgument(brushInfo.color);
            EndCommand();
        }

        private void UsePen(object penKey)
        {
            PenInfo penInfo = penDict[penKey];

            BeginCommand('s');
            FloatArgument(penInfo.width);
            ColorArgument(penInfo.color);

            switch (penInfo.join) {
                case LineJoin.Miter:
                case LineJoin.MiterClipped:
                    StringArgument("miter"); break;
                case LineJoin.Bevel:
                    StringArgument("bevel"); break;
                case LineJoin.Round:
                    StringArgument("round"); break;
                default:
                    throw new ApplicationException("Unexpected linejoin");
            }

            switch (penInfo.caps) {
                case LineCap.Flat:
                    StringArgument("butt"); break;
                case LineCap.Square:
                    StringArgument("square"); break;
                case LineCap.Round:
                    StringArgument("round"); break;
                default:
                    throw new ApplicationException("Unexpected linecap");
            }

            FloatArgument(penInfo.miterLimit);
            EndCommand();
        }

        private void UsePathPart(GraphicsPathPart part)
        {
            switch (part.Kind) {
                case GraphicsPathPartKind.Start:
                    BeginCommand('m');
                    FloatArgument(part.Points[0].X);
                    FloatArgument(part.Points[0].Y);
                    EndCommand();
                    break;

                case GraphicsPathPartKind.Lines:
                    foreach (PointF pt in part.Points) {
                        BeginCommand('l');
                        FloatArgument(pt.X);
                        FloatArgument(pt.Y);
                        EndCommand();
                    }
                    break;

                case GraphicsPathPartKind.Beziers:
                    for (int i = 0; i < part.Points.Length; i += 3) {
                        BeginCommand('b');
                        FloatArgument(part.Points[i].X);
                        FloatArgument(part.Points[i].Y);
                        FloatArgument(part.Points[i + 1].X);
                        FloatArgument(part.Points[i + 1].Y);
                        FloatArgument(part.Points[i + 2].X);
                        FloatArgument(part.Points[i + 2].Y);
                        EndCommand();
                    }
                    break;

                case GraphicsPathPartKind.Close:
                    BeginCommand('c'); EndCommand();
                    break;

                default:
                    throw new ApplicationException("unexpected part kind");
            }
        }

        private void UsePath(List<GraphicsPathPart> parts)
        {
            BeginCommand('p'); EndCommand();
            foreach (GraphicsPathPart part in parts) {
                UsePathPart(part);
            }
        }

        private void UsePath(object key)
        {
            UsePath(pathDict[key].parts);
        }
    }

    public class BrowserCanvasColorConverter
    {
        public virtual uint ToColor(CmykColor cmykColor)
        {
            System.Drawing.Color sysColor = ColorConverter.ToColor(cmykColor);
            return (uint)(sysColor.ToArgb() & 0xFFFFFF);
        }
    }

}
