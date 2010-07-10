using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using D2D = Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

using PurplePen.MapModel;

using Color = System.Drawing.Color;
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using Matrix = System.Drawing.Drawing2D.Matrix;
using FillMode = System.Drawing.Drawing2D.FillMode;
using LineJoin = System.Drawing.Drawing2D.LineJoin;
using LineCap = System.Drawing.Drawing2D.LineCap;

namespace Map_D2D
{
    public class D2D_GraphicsTarget: IGraphicsTarget
    {
        protected D2DFactory factory;
        protected RenderTarget renderTarget;
        private Stack<Matrix3x2F> transformStack = new Stack<Matrix3x2F>();
        private Stack<Layer> layerStack = new Stack<Layer>();
        private Dictionary<object, D2D_Pen> penMap = new Dictionary<object, D2D_Pen>();
        private Dictionary<object, Brush> brushMap = new Dictionary<object, Brush>();
        private Dictionary<object, D2D_Font> fontMap = new Dictionary<object, D2D_Font>();
        private Dictionary<object, Geometry> geometryMap = new Dictionary<object, Geometry>();

        public D2D_GraphicsTarget(D2DFactory factory, RenderTarget renderTarget)
        {
            this.factory = factory;
            this.renderTarget = renderTarget;
        }

        public void PushTransform(System.Drawing.Drawing2D.Matrix matrix)
        {
            Matrix3x2F d2dMatrix = D2DUtil.GetD2DMatrix(matrix);
            transformStack.Push(renderTarget.Transform);
            renderTarget.Transform = D2DUtil.Multiply(d2dMatrix, renderTarget.Transform);
        }

        public void PopTransform()
        {
            renderTarget.Transform = transformStack.Pop();
        }

        public void PushClip(object pathKey)
        {
            Layer layer = renderTarget.CreateLayer(new D2D.SizeF(0,0));
            renderTarget.PushLayer(new LayerParameters(new D2D.RectF(float.NegativeInfinity, float.NegativeInfinity, float.PositiveInfinity, float.PositiveInfinity),
                                                                          GetGeometry(pathKey),
                                                                          AntialiasMode.PerPrimitive,
                                                                          Matrix3x2F.Identity,
                                                                          1.0F,
                                                                          null,
                                                                          LayerOptions.None),
                                             layer);
            layerStack.Push(layer);
        }

        public void PopClip()
        {
            renderTarget.PopLayer();
            layerStack.Pop().Dispose();
        }

        public void CreatePath(object pathKey, IEnumerable<GraphicsPathPart> parts, System.Drawing.Drawing2D.FillMode windingMode)
        {
            if (geometryMap.ContainsKey(pathKey))
                throw new InvalidOperationException("Key already has a path created for it");

            PathGeometry geo = factory.CreatePathGeometry();
            using (GeometrySink sink = geo.Open()) {

                sink.SetFillMode((windingMode == FillMode.Alternate) ? D2D.FillMode.Alternate : D2D.FillMode.Alternate);

                GraphicsPathPart[] partArray = parts.ToArray();

                bool closed = true;
                for (int partIndex = 0; partIndex < partArray.Length; ++partIndex) {
                    GraphicsPathPart part = partArray[partIndex];

                    switch (part.Kind) {
                        case GraphicsPathPartKind.Start:
                            Debug.Assert(part.Points.Length == 1);
                            if (!closed)
                                sink.EndFigure(FigureEnd.Open);
                            sink.BeginFigure(D2DUtil.Point(part.Points[0]), FigureBegin.Filled);
                            closed = false;
                            break;

                        case GraphicsPathPartKind.Lines: {
                                sink.AddLines(part.Points);
                                break;
                            }

                        case GraphicsPathPartKind.Beziers: {
                                BezierSegment[] segments = new BezierSegment[part.Points.Length / 3];
                                for (int iPoint = 0, iSegment = 0; iPoint < part.Points.Length; iPoint += 3, iSegment += 1) 
                                    segments[iSegment] = new BezierSegment(D2DUtil.Point(part.Points[iPoint]), D2DUtil.Point(part.Points[iPoint + 1]), D2DUtil.Point(part.Points[iPoint + 2])); 
                                sink.AddBeziers(segments);
                                break;
                            }

                        case GraphicsPathPartKind.Close:
                            sink.EndFigure(FigureEnd.Closed);
                            closed = true;
                            break;
                    }
                }

                if (!closed)
                    sink.EndFigure(FigureEnd.Open);
                sink.Close();
            }

            geometryMap.Add(pathKey, geo);
        }

        public void CreateSolidBrush(object brushKey, System.Drawing.Color color)
        {
            if (brushMap.ContainsKey(brushKey))
                throw new InvalidOperationException("Key already has a brush created for it");

            D2D.Brush brush = renderTarget.CreateSolidColorBrush(D2DUtil.ToD2DColor(color));
            brushMap.Add(brushKey, brush);
        }

        public IBrushTarget CreatePatternBrush(System.Drawing.SizeF size, int bitmapWidth, int bitmapHeight)
        {
            // Create a renderTarget to render the pattern brush to.
            BitmapRenderTarget brushRenderTarget = renderTarget.CreateCompatibleRenderTarget(CompatibleRenderTargetOptions.None, new D2D.SizeU((uint)bitmapWidth, (uint)bitmapHeight));

            Matrix m = new Matrix();
            m.Translate((float)bitmapWidth / 2F, (float)bitmapHeight / 2F);
            m.Scale((float)bitmapWidth / size.Width, (float)bitmapHeight / size.Height);
            Matrix3x2F d2dMatrix = D2DUtil.GetD2DMatrix(m);

            brushRenderTarget.BeginDraw();
            brushRenderTarget.Clear(new ColorF(0, 0, 0, 0));
            brushRenderTarget.Transform = D2DUtil.Multiply(d2dMatrix, brushRenderTarget.Transform);

            return new D2D_BrushTarget(this, factory, brushRenderTarget, renderTarget, size, bitmapWidth, bitmapHeight);
        }

        public void CreatePen(object penKey, object brushKey, float width, System.Drawing.Drawing2D.LineCap caps, System.Drawing.Drawing2D.LineJoin join, float miterLimit)
        {
            if (penMap.ContainsKey(penKey))
                throw new InvalidOperationException("Key already has a pen created for it");

            D2D_Pen pen = new D2D_Pen(GetBrush(brushKey), false, width, D2DUtil.CreateStrokeStyle(factory, caps, join, miterLimit));
            penMap.Add(penKey, pen);
        }

        public void CreatePen(object penKey, System.Drawing.Color color, float width, System.Drawing.Drawing2D.LineCap caps, System.Drawing.Drawing2D.LineJoin join, float miterLimit)
        {
            if (penMap.ContainsKey(penKey))
                throw new InvalidOperationException("Key already has a pen created for it");

            D2D.Brush brush = renderTarget.CreateSolidColorBrush(D2DUtil.ToD2DColor(color));
            D2D_Pen pen = new D2D_Pen(brush, true, width, D2DUtil.CreateStrokeStyle(factory, caps, join, miterLimit));
            penMap.Add(penKey, pen);
        }

        public void CreateFont(object fontKey, string familyName, float emHeight, bool bold, bool italic)
        {
            //throw new NotImplementedException();
            fontMap.Add(fontKey, new D2D_Font());
        }

        public void DrawLine(object penKey, System.Drawing.PointF start, System.Drawing.PointF finish)
        {
            D2D_Pen realPen = GetPen(penKey);
            renderTarget.DrawLine(D2DUtil.Point(start), D2DUtil.Point(finish), realPen.Brush, realPen.Width, realPen.StrokeStyle);
        }

        public void DrawArc(object penKey, System.Drawing.RectangleF boundingRect, float startAngle, float sweepAngle)
        {
            float endAngle = startAngle + sweepAngle;
            PointF centerPoint = new PointF((boundingRect.Left + boundingRect.Right) / 2, (boundingRect.Top + boundingRect.Bottom) / 2);
            float radiusX = boundingRect.Right - centerPoint.X, radiusY = boundingRect.Bottom - centerPoint.Y;
            PointF ptStart = new PointF(centerPoint.X + (float)Math.Cos(startAngle * Math.PI / 180.0) * radiusX, centerPoint.Y + (float)Math.Sin(startAngle * Math.PI / 180.0) * radiusY);
            PointF ptEnd = new PointF(centerPoint.X + (float)Math.Cos(endAngle * Math.PI / 180.0) * radiusX, centerPoint.Y + (float)Math.Sin(endAngle * Math.PI / 180.0) * radiusY);
            ArcSegment segment = new ArcSegment(D2DUtil.Point(ptEnd), new D2D.SizeF(radiusX, radiusY), 0, SweepDirection.Clockwise, (sweepAngle > 180.0F) ? ArcSize.Large : ArcSize.Small);

            D2D_Pen realPen = GetPen(penKey);

            using (PathGeometry geo = factory.CreatePathGeometry()) {
                using (GeometrySink sink = geo.Open()) {
                    sink.BeginFigure(D2DUtil.Point(ptStart), FigureBegin.Hollow);
                    sink.AddArc(segment);
                    sink.EndFigure(FigureEnd.Open);
                    sink.Close();
                }

                renderTarget.DrawGeometry(geo, realPen.Brush, realPen.Width, realPen.StrokeStyle);
            }
        }

        public void DrawEllipse(object penKey, System.Drawing.PointF center, float radiusX, float radiusY)
        {
            D2D_Pen realPen = GetPen(penKey);
            renderTarget.DrawEllipse(new D2D.Ellipse(D2DUtil.Point(center), radiusX, radiusY), realPen.Brush, realPen.Width, realPen.StrokeStyle);
        }

        public void FillEllipse(object brushKey, System.Drawing.PointF center, float radiusX, float radiusY)
        {
            renderTarget.FillEllipse(new D2D.Ellipse(D2DUtil.Point(center), radiusX, radiusY), GetBrush(brushKey));
        }

        public void DrawRectangle(object penKey, System.Drawing.RectangleF rect)
        {
            D2D_Pen realPen = GetPen(penKey);
            renderTarget.DrawRectangle(D2DUtil.Rectangle(rect), realPen.Brush, realPen.Width, realPen.StrokeStyle);
        }

        public void FillRectangle(object brushKey, System.Drawing.RectangleF rect)
        {
            renderTarget.FillRectangle(D2DUtil.Rectangle(rect), GetBrush(brushKey));
        }

        public void DrawPolygon(object penKey, System.Drawing.PointF[] pts)
        {
            D2D_Pen realPen = GetPen(penKey);

            using (PathGeometry geo = factory.CreatePathGeometry()) {
                using (GeometrySink sink = geo.Open()) {
                    sink.BeginFigure(D2DUtil.Point(pts[0]), FigureBegin.Hollow);
                    PointF[] pointsAfterFirst = new PointF[pts.Length - 1];
                    Array.Copy(pts, 1, pointsAfterFirst, 0, pts.Length - 1);
                    sink.AddLines(pointsAfterFirst);
                    sink.EndFigure(FigureEnd.Closed);
                    sink.Close();
                }

                renderTarget.DrawGeometry(geo, realPen.Brush, realPen.Width, realPen.StrokeStyle);
            }
        }

        public void DrawPolyline(object penKey, System.Drawing.PointF[] pts)
        {
            D2D_Pen realPen = GetPen(penKey);

            using (PathGeometry geo = factory.CreatePathGeometry()) {
                using (GeometrySink sink = geo.Open()) {
                    sink.BeginFigure(D2DUtil.Point(pts[0]), FigureBegin.Hollow);
                    PointF[] pointsAfterFirst = new PointF[pts.Length - 1];
                    Array.Copy(pts, 1, pointsAfterFirst, 0, pts.Length - 1);
                    sink.AddLines(pointsAfterFirst);
                    sink.EndFigure(FigureEnd.Open);
                    sink.Close();
                }

                renderTarget.DrawGeometry(geo, realPen.Brush, realPen.Width, realPen.StrokeStyle);
            }
        }

        public void FillPolygon(object brushKey, System.Drawing.PointF[] pts, System.Drawing.Drawing2D.FillMode windingMode)
        {
            using (PathGeometry geo = factory.CreatePathGeometry()) {
                using (GeometrySink sink = geo.Open()) {
                    sink.SetFillMode(windingMode == FillMode.Alternate ? D2D.FillMode.Alternate : D2D.FillMode.Winding);
                    sink.BeginFigure(D2DUtil.Point(pts[0]), FigureBegin.Filled);
                    PointF[] pointsAfterFirst = new PointF[pts.Length - 1];
                    Array.Copy(pts, 1, pointsAfterFirst, 0, pts.Length - 1);
                    sink.AddLines(pointsAfterFirst);
                    sink.EndFigure(FigureEnd.Closed);
                    sink.Close();
                }

                renderTarget.FillGeometry(geo, GetBrush(brushKey));
            }
        }

        public void DrawPath(object penKey, object pathKey)
        {
            D2D_Pen realPen = GetPen(penKey);

            renderTarget.DrawGeometry(GetGeometry(pathKey), realPen.Brush, realPen.Width, realPen.StrokeStyle);
        }

        public void FillPath(object brushKey, object pathKey)
        {
            renderTarget.FillGeometry(GetGeometry(pathKey), GetBrush(brushKey));
        }

        public void DrawText(string text, object fontKey, object brushKey, System.Drawing.PointF upperLeft)
        {
            //throw new NotImplementedException();
        }

        public void DrawTextOutline(string text, object fontKey, object penKey, System.Drawing.PointF upperLeft)
        {
            //throw new NotImplementedException();
        }

        public bool HasPath(object pathKey) {
            return geometryMap.ContainsKey(pathKey);
        }

        public bool HasPen(object penKey) {
            return penMap.ContainsKey(penKey);
        }

        public bool HasBrush(object brushKey) {
            return brushMap.ContainsKey(brushKey);
        }

        public bool HasFont(object fontKey) {
            return fontMap.ContainsKey(fontKey);
        }

        private Brush GetBrush(object brushKey) {
            Brush brush;
            if (brushMap.TryGetValue(brushKey, out brush))
                return brush;
            else
                throw new ArgumentException("Given key does not have a brush created for it", "brushKey");
        }

        private D2D_Pen GetPen(object penKey) {
            D2D_Pen pen;
            if (penMap.TryGetValue(penKey, out pen))
                return pen;
            else
                throw new ArgumentException("Given key does not have a pen created for it", "penKey");
        }

        private D2D_Font GetFont(object fontKey) {
            D2D_Font font;
            if (fontMap.TryGetValue(fontKey, out font))
                return font;
            else
                throw new ArgumentException("Given key does not have a font created for it", "fontKey");
        }

        private Geometry GetGeometry(object pathKey) {
            Geometry geo;
            if (geometryMap.TryGetValue(pathKey, out geo))
                return geo;
            else
                throw new ArgumentException("Given key does not have a path created for it", "pathKey");
        }

        public void Dispose()
        {
            foreach (D2D_Pen pen in penMap.Values)
                pen.Dispose();
            penMap.Clear();

            foreach (Brush brush in brushMap.Values)
                brush.Dispose();
            brushMap.Clear();

            foreach (Geometry geo in geometryMap.Values)
                geo.Dispose();
            geometryMap.Clear();

            foreach (D2D_Font font in fontMap.Values)
                font.Dispose();
            fontMap.Clear();

            renderTarget.Dispose();
        }

        public class D2D_BrushTarget : D2D_GraphicsTarget, IBrushTarget
        {
            private D2D_GraphicsTarget owningTarget;
            private SizeF size;
            private new BitmapRenderTarget renderTarget;
            private RenderTarget originalRenderTarget;
            private int bitmapWidth, bitmapHeight;

            public D2D_BrushTarget(D2D_GraphicsTarget owningTarget, D2DFactory factory, BitmapRenderTarget renderTarget, RenderTarget originalRenderTarget, SizeF size, int bitmapWidth, int bitmapHeight)
                : base(factory, renderTarget) {
                this.owningTarget = owningTarget;
                this.renderTarget = renderTarget;
                this.originalRenderTarget = originalRenderTarget;
                this.size = size;
                this.bitmapWidth = bitmapWidth;
                this.bitmapHeight = bitmapHeight;
            }

            public void FinishBrush(object brushKey, float rotationAngle) {
                renderTarget.EndDraw();

                if (owningTarget.brushMap.ContainsKey(brushKey))
                    throw new InvalidOperationException("Key already has a brush created for it");

                // Get the bitmap.
                D2D.D2DBitmap bitmap = renderTarget.GetBitmap();

                Matrix matrix = new Matrix();
                matrix.Rotate(rotationAngle);
                matrix.Scale(size.Width / (float)bitmapWidth, size.Height / (float)bitmapHeight);
                matrix.Translate(-bitmapWidth / 2F, -bitmapHeight / 2F);

                D2D.BitmapBrush brush = originalRenderTarget.CreateBitmapBrush(bitmap,
                    new BitmapBrushProperties(ExtendMode.Wrap, ExtendMode.Wrap, BitmapInterpolationMode.Linear),
                    new BrushProperties(1.0F, D2DUtil.GetD2DMatrix(matrix)));

                //D2D.BitmapBrush brush = originalRenderTarget.CreateBitmapBrush(bitmap);
                bitmap.Dispose();
                Dispose();

                owningTarget.brushMap.Add(brushKey, brush);
            }
        }
    }


    public class D2D_Pen 
    {
        private D2D.Brush brush;
        float width;
        bool brushIsOwned;
        StrokeStyle strokeStyle;

        public D2D.Brush Brush { get { return brush; } }
        public float Width { get { return width; } }
        public StrokeStyle StrokeStyle { get { return strokeStyle; } }

        public void Dispose()
        {
            if (brushIsOwned && brush != null) 
                brush.Dispose();
            brush = null;

            if (strokeStyle != null)
                strokeStyle.Dispose();
            strokeStyle = null;
        }

        public D2D_Pen(D2D.Brush brush, bool brushIsOwned, float width, StrokeStyle strokeStyle)
        {
            this.brush = brush;
            this.brushIsOwned = brushIsOwned;
            this.width = width;
            this.strokeStyle = strokeStyle;
        }
    }

    public class D2D_Font 
    {
        public void Dispose() {
        }
    }

    public static class D2DUtil
    {
        public static Matrix3x2F GetD2DMatrix(Matrix source)
        {
            float[] elements = source.Elements;
            return new Matrix3x2F(elements[0], elements[1], elements[2], elements[3], elements[4], elements[5]);
        }

        public static Matrix3x2F Multiply(Matrix3x2F mat1, Matrix3x2F mat2)
        {
            float m_11 = mat1.M11 * mat2.M11 + mat1.M12 * mat2.M21;
            float m_12 = mat1.M11 * mat2.M12 + mat1.M12 * mat2.M22;
            float m_21 = mat1.M21 * mat2.M11 + mat1.M22 * mat2.M21;
            float m_22 = mat1.M21 * mat2.M12 + mat1.M22 * mat2.M22;
            float m_31 = mat1.M31 * mat2.M11 + mat1.M32 * mat2.M21 + mat2.M31;
            float m_32 = mat1.M31 * mat2.M12 + mat1.M32 * mat2.M22 + mat2.M32;

            return new Matrix3x2F(m_11, m_12, m_21, m_22, m_31, m_32);
        }

        public static D2D.StrokeStyle CreateStrokeStyle(D2DFactory factory, LineCap lineCap, LineJoin lineJoin, float miterLimit) {
            D2D.CapStyle capStyle;
            switch (lineCap) {
                case LineCap.Flat:
                    capStyle = CapStyle.Flat; break;
                case LineCap.Square:
                    capStyle = CapStyle.Square; break;
                case LineCap.Round:
                    capStyle = CapStyle.Round; break;
                default:
                    throw new ApplicationException();
            }

            D2D.LineJoin lineStyle;
            switch (lineJoin) {
                case LineJoin.Bevel:
                    lineStyle = D2D.LineJoin.Bevel; break;
                case LineJoin.Miter:
                    lineStyle = D2D.LineJoin.Miter; break;
                case LineJoin.Round:
                    lineStyle = D2D.LineJoin.Round; break;
                case LineJoin.MiterClipped:
                    lineStyle = D2D.LineJoin.MiterOrBevel; break;
                default:
                    throw new ApplicationException();
            }

            return factory.CreateStrokeStyle(new StrokeStyleProperties(capStyle, capStyle, CapStyle.Flat, lineStyle, miterLimit, DashStyle.Solid, 0));
        }

        public static ColorF ToD2DColor(Color color)
        {
            return new ColorF((float)(color.R)/255F, (float)(color.G)/255F, (float)(color.B)/255F, (float)(color.A)/255F);
        }

        public static Point2F Point(PointF pt) {
            return new Point2F(pt.X, pt.Y);
        }

        public static D2D.RectF Rectangle(RectangleF rect) {
            return new D2D.RectF(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }
    }
}
