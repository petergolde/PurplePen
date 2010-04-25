/* Copyright (c) 2008, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SysDraw = System.Drawing;
using SysDraw2D = System.Drawing.Drawing2D;
#if WPF
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using Matrix = System.Drawing.Drawing2D.Matrix;
using WpfMatrix = System.Windows.Media.Matrix;
using LineJoin = System.Drawing.Drawing2D.LineJoin;
using LineCap = System.Drawing.Drawing2D.LineCap;
#endif
#if WPF
using System.Windows;
using System.Windows.Media;
#else
using System.Drawing;
using System.Drawing.Drawing2D;
#endif

namespace PurplePen.MapModel
{
    public interface IGraphicsBrush : IDisposable
    {
        Brush Brush { get; }
    }

    public interface IGraphicsPen : IDisposable
    {
        Pen Pen { get; }
    }

    public interface IGraphicsPath : IDisposable
    { }
    public interface IGraphicsFont : IDisposable
    { }

    public enum GraphicsPathPartKind { Start, Lines, Beziers, Close };

    public struct GraphicsPathPart {
        public readonly GraphicsPathPartKind Kind;
        public readonly PointF[] Points;

        public GraphicsPathPart(GraphicsPathPartKind kind, PointF[] points)
        {
            this.Kind = kind;
            this.Points = points;
        }
    }

    public interface ITextFaceMetrics : IDisposable
    {
        float EmHeight { get; }
        float Ascent { get; }
        float Descent { get; }
        float CapHeight { get; }
        float SpaceWidth { get; }
        float GetTextWidth(string text);
    }

    public interface ITextMetrics : IDisposable
    {
        ITextFaceMetrics GetTextFaceMetrics(string familyName, float emHeight, bool bold, bool italic);
        bool TextFaceIsInstalled(string familyName);
    }

    public interface IGraphicsTarget_X: IDisposable
    {
        // Prepend a transform to the graphics drawing target.
        void PushTransform(Matrix matrix);
        void PopTransform();

        // Set a clip on the graphics drawing target.
        void PushClip(IGraphicsPath geometry);
        void PopClip();

        // Create paths.
        IGraphicsPath CreatePath(IEnumerable<GraphicsPathPart> parts, SysDraw2D.FillMode windingMode);
        IGraphicsPath CreateMultiPath(IEnumerable<IEnumerable<GraphicsPathPart>> multiParts, SysDraw2D.FillMode windingMode);

        // Create brushes and pens
        IGraphicsBrush CreateSolidBrush(Color color);
        IBrushTarget CreatePatternBrush(SizeF size, int bitmapWidth, int bitmapHeight);
        IGraphicsPen CreatePen(IGraphicsBrush brush, float width, SysDraw2D.LineCap caps, SysDraw2D.LineJoin join, float miterLimit);
        IGraphicsPen CreatePen(Color color, float width, SysDraw2D.LineCap caps, SysDraw2D.LineJoin join, float miterLimit);

        // Create font
        IGraphicsFont CreateFont(string familyName, float emHeight, bool bold, bool italic);

        // Draw an line with a pen.
        void DrawLine(IGraphicsPen pen, PointF start, PointF finish);

        // Draw an ellipse with a pen.
        void DrawEllipse(IGraphicsPen pen, PointF center, float radiusX, float radiusY);

        // Fill an ellipse with a pen.
        void FillEllipse(IGraphicsBrush brush, PointF center, float radiusX, float radiusY);

        // Draw a rectangle with a pen.
        void DrawRectangle(IGraphicsPen pen, RectangleF rect);

        // Fill a rectangle with a brush.
        void FillRectangle(IGraphicsBrush brush, RectangleF rect);

        // Fill a polygon with a brush
        void FillPolygon(IGraphicsBrush brush, PointF[] pts, SysDraw2D.FillMode windingMode);

        // Draw text with upper-left corner of text at the given locations.
        void DrawText(string text, IGraphicsFont font, IGraphicsBrush brush, PointF upperLeft);

        // Draw text with upper-left corner of text at upper-left corner of rectangle, clipped.
        void DrawClippedText(string text, IGraphicsFont font, IGraphicsBrush brush, RectangleF rect);
    }

    public interface IBrushTarget: IGraphicsTarget_X
    {
        IGraphicsBrush FinishBrush();
    }


    // A GraphicsTarget encapsulates either a Graphics (for WinForms) or a DrawingContext (for WPF)
    public class GraphicsTarget
    {
#if WPF
        public DrawingContext DrawingContext;
        private int pushLevel;      // How many pushes have we done?

        public IGraphicsBrush CreateSolidBrush(Color color)
        {
            return new WPF_Brush(color);
        }

        public IGraphicsPen CreatePen(Color color, float width, SysDraw2D.LineCap caps, SysDraw2D.LineJoin join, float miterLimit)
        {
            return CreatePen(CreateSolidBrush(color), width, caps, join, miterLimit);
        }

        public IGraphicsPen CreatePen(IGraphicsBrush brush, float width, SysDraw2D.LineCap caps, SysDraw2D.LineJoin join, float miterLimit)
        {
            Pen pen = new Pen((brush as WPF_Brush).Brush, width);
            
            switch (caps)
	        {
                case System.Drawing.Drawing2D.LineCap.Flat:
                    pen.StartLineCap = pen.EndLineCap = PenLineCap.Flat;
                    break;
                case System.Drawing.Drawing2D.LineCap.Round:
                    pen.StartLineCap = pen.EndLineCap = PenLineCap.Round;
                    break;
                case System.Drawing.Drawing2D.LineCap.Square:
                    pen.StartLineCap = pen.EndLineCap = PenLineCap.Square;
                    break;
                default:
                    throw new ArgumentException("bad line cap", "caps");
	        }

            switch (join)
            {
                case System.Drawing.Drawing2D.LineJoin.Bevel:
                    pen.LineJoin = PenLineJoin.Bevel;
                    break;
                case System.Drawing.Drawing2D.LineJoin.Miter:
                    pen.LineJoin = PenLineJoin.Miter;
                    pen.MiterLimit = miterLimit;
                    break;
                case System.Drawing.Drawing2D.LineJoin.Round:
                    pen.LineJoin = PenLineJoin.Round;
                    break;
                default:
                    throw new ArgumentException("bad line join", "join");
            }

            pen.Freeze();
            return new WPF_Pen(pen);
        }

        public GraphicsBrushTarget CreatePatternBrush(SizeF size, int bitmapWidth, int bitmapHeight)
        {
            // Create a visual with the glyph to tile in it.
            DrawingVisual visual = new DrawingVisual();
            DrawingContext dc = visual.RenderOpen();
            return new GraphicsBrushTarget(dc, visual, size, bitmapWidth, bitmapHeight);
        }

        public GraphicsTarget(DrawingContext dc)
        {
            this.DrawingContext = dc;
            pushLevel = 0;
        }

        // Prepend a transform to the graphics drawing target.
        public void PushTransform(Matrix matrix)
        {
            DrawingContext.PushTransform(new MatrixTransform(GetWpfMatrix(matrix)));
            ++pushLevel;
        }

        public void PopTransform()
        {
            DrawingContext.Pop();
        }

        // Set a clip on the graphics drawing target.
        public void PushClip(SymPathWithHoles path)
        {
            DrawingContext.PushClip(path.Geometry);
            ++pushLevel;
        }

        public void PopClip()
        {
            DrawingContext.Pop();
        }

        // Draw an line with a pen.
        public void DrawLine(IGraphicsPen pen, PointF start, PointF finish)
        {
            DrawingContext.DrawLine((pen as WPF_Pen).Pen, new Point(start.X, start.Y), new Point(finish.X, finish.Y));
        }

        // Draw an ellipse with a pen.
        public void DrawEllipse(IGraphicsPen pen, PointF center, float radiusX, float radiusY)
        {
            DrawingContext.DrawEllipse(null, (pen as WPF_Pen).Pen, new Point(center.X, center.Y), radiusX, radiusY);
        }

        // Fill an ellipse with a pen.
        public void FillEllipse(IGraphicsBrush brush, PointF center, float radiusX, float radiusY)
        {
            DrawingContext.DrawEllipse(brush.Brush, null, new Point(center.X, center.Y), radiusX, radiusY);
        }

        // Draw a rectangle with a pen.
        public void DrawRectangle(IGraphicsPen pen, RectangleF rect)
        {
            DrawingContext.DrawRectangle(null, (pen as WPF_Pen).Pen, new Rect(rect.X, rect.Y, rect.Width, rect.Height));
        }

        // Fill a rectangle with a brush.
        public void FillRectangle(IGraphicsBrush brush, RectangleF rect)
        {
            DrawingContext.DrawRectangle((brush as WPF_Brush).Brush, null, new Rect(rect.X, rect.Y, rect.Width, rect.Height));
        }

        // Fill a polygon with a brush
        public void FillPolygon(Brush brush, PointF[] pts, bool winding)
        {
            Point[] points = new Point[pts.Length];
            for (int i = 0; i < pts.Length; ++i)
                points[i] = new Point(pts[i].X, pts[i].Y);

            PathSegment segment = new PolyLineSegment(points, true);
            PathFigure figure = new PathFigure(points[points.Length - 1], new PathSegment[] { segment }, true);
            PathGeometry geometry = new PathGeometry(new PathFigure[] { figure }, winding ? FillRule.Nonzero : FillRule.EvenOdd, System.Windows.Media.Transform.Identity);
            DrawingContext.DrawGeometry(brush, null, geometry);
        }

        // Fill a polygon with a brush
        public void FillPolygon(IGraphicsBrush brush, PointF[] pts, SysDraw2D.FillMode windingMode)
        {
            Point[] points = new Point[pts.Length];
            for (int i = 0; i < pts.Length; ++i)
                points[i] = new Point(pts[i].X, pts[i].Y);

            PathSegment segment = new PolyLineSegment(points, true);
            PathFigure figure = new PathFigure(points[points.Length - 1], new PathSegment[] { segment }, true);
            PathGeometry geometry = new PathGeometry(new PathFigure[] { figure }, windingMode == SysDraw2D.FillMode.Winding ? FillRule.Nonzero : FillRule.EvenOdd, System.Windows.Media.Transform.Identity);
            DrawingContext.DrawGeometry((brush as WPF_Brush).Brush, null, geometry);
        }

        private static WpfMatrix GetWpfMatrix(Matrix source)
        {
            float[] elements = source.Elements;
            return new WpfMatrix(elements[0], elements[1], elements[2], elements[3], elements[4], elements[5]);
        }

#else
        public Graphics Graphics;

        private Stack<GraphicsState> stateStack;

        public IGraphicsBrush CreateSolidBrush(Color color)
        {
            return new GDIPlus_Brush(color);
        }

        public GraphicsBrushTarget CreatePatternBrush(SizeF size, int bitmapWidth, int bitmapHeight)
        {
                // Create a new bitmap and fill it transparent.
                Bitmap bitmap = new Bitmap(bitmapWidth, bitmapHeight);
                Graphics g = Graphics.FromImage(bitmap);
                //g.CompositingMode = CompositingMode.SourceCopy;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillRectangle(Brushes.Transparent, 0, 0, bitmap.Width, bitmap.Height);
                g.TranslateTransform((float)bitmapWidth / 2F, (float)bitmapHeight / 2F);
                g.ScaleTransform((float)bitmapWidth / size.Width, (float)bitmapHeight / size.Height);

                return new GraphicsBrushTarget(g, bitmap, size);
        }

        public IGraphicsPen CreatePen(IGraphicsBrush brush, float width, SysDraw2D.LineCap caps, SysDraw2D.LineJoin join, float miterLimit)
        {
            Pen pen = new Pen((brush as GDIPlus_Brush).Brush, width);
            pen.StartCap = pen.EndCap = caps;
            pen.LineJoin = join;
            pen.MiterLimit = miterLimit;
            return new GDIPlus_Pen(pen);
        }

        public IGraphicsPen CreatePen(Color color, float width, SysDraw2D.LineCap caps, SysDraw2D.LineJoin join, float miterLimit)
        {
            Pen pen = new Pen(color, width);
            pen.StartCap = pen.EndCap = caps;
            pen.LineJoin = join;
            pen.MiterLimit = miterLimit;
            return new GDIPlus_Pen(pen);
        }

        public GraphicsTarget(Graphics g)
        {
            this.Graphics = g;
            stateStack = new Stack<GraphicsState>();
        }

        // Prepend a transform to the graphics drawing target.
        public void PushTransform(Matrix matrix)
        {
            stateStack.Push(Graphics.Save());
            Graphics.MultiplyTransform(matrix, MatrixOrder.Prepend);
        }

        // Pop the transform
        public void PopTransform()
        {
            Graphics.Restore(stateStack.Pop());
        }

        // Set a clip on the graphics drawing target.
        public void PushClip(SymPathWithHoles path)
        {
            stateStack.Push(Graphics.Save());
            Graphics.IntersectClip(new Region(path.GetPath()));
        }

        // Pop the clip.
        public void PopClip()
        {
            Graphics.Restore(stateStack.Pop());
        }

        // Draw an line with a pen.
        public void DrawLine(IGraphicsPen pen, PointF start, PointF finish)
        {
            Graphics.DrawLine((pen as GDIPlus_Pen).Pen, start, finish);
        }

        // Draw an ellipse with a pen.
        public void DrawEllipse(IGraphicsPen pen, PointF center, float radiusX, float radiusY)
        {
            Graphics.DrawEllipse((pen as GDIPlus_Pen).Pen, center.X - radiusX, center.Y - radiusY, 2 * radiusX, 2 * radiusY);
        }

        // Fill an ellipse with a brush.
        public void FillEllipse(IGraphicsBrush brush, PointF center, float radiusX, float radiusY)
        {
            Graphics.FillEllipse((brush as GDIPlus_Brush).Brush, center.X - radiusX, center.Y - radiusY, 2 * radiusX, 2 * radiusY);
        }

        // Draw a rectangle with a pen.
        public void DrawRectangle(IGraphicsPen pen, RectangleF rect)
        {
            Graphics.DrawRectangle((pen as GDIPlus_Pen).Pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        // Fill a rectangle with a brush.
        public void FillRectangle(IGraphicsBrush brush, RectangleF rect)
        {
            Graphics.FillRectangle((brush as GDIPlus_Brush).Brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        // Fill a polygon with a brush
        public void FillPolygon(IGraphicsBrush brush, PointF[] pts, SysDraw2D.FillMode windingMode)
        {
            Graphics.FillPolygon((brush as GDIPlus_Brush).Brush, pts, windingMode);
        }

#endif
    }

#if WPF
    public class GraphicsBrushTarget : GraphicsTarget
    {
        private DrawingVisual visual;
        private SizeF size;
        private int bitmapWidth, bitmapHeight;

        public GraphicsBrushTarget(DrawingContext dc, DrawingVisual visual, SizeF size, int bitmapWidth, int bitmapHeight)
        : base(dc)
        {
            this.visual = visual;
            this.size = size;
            this.bitmapWidth = bitmapWidth;
            this.bitmapHeight = bitmapHeight;
        }

        public IGraphicsBrush FinishPatternBrush(float rotationAngle)
        {
            DrawingContext.Close();

            // Get a drawing from the drawingvisual
            Drawing drawing = visual.Drawing;
            drawing.Freeze();

            // Create a brush from the drawing.
            DrawingBrush brush = new DrawingBrush(drawing);
            brush.Stretch = Stretch.Fill;
            brush.TileMode = TileMode.Tile;
            brush.ViewboxUnits = BrushMappingMode.Absolute;
            brush.ViewportUnits = BrushMappingMode.Absolute;
            brush.Viewbox = brush.Viewport = new Rect(-size.Width / 2, -size.Height / 2, size.Width, size.Height);
            brush.Transform = new RotateTransform(rotationAngle);

            // Set the minimum and maximum relative sizes for regenerating the tiled brush.
            // The tiled brush will be regenerated when the size is
            //   0.5x, 0.25x (and so forth)
            // and
            //   2x, 4x, 8x (and so forth)
            // of the original size.
            System.Windows.Media.RenderOptions.SetCacheInvalidationThresholdMinimum(brush, 0.5);
            System.Windows.Media.RenderOptions.SetCacheInvalidationThresholdMaximum(brush, 2.0);

            // Set the caching hint option for the brush.
            System.Windows.Media.RenderOptions.SetCachingHint(brush, CachingHint.Cache);

            // Freeze the brush.
            brush.Freeze();
            return new WPF_Brush(brush);
        }
    }
#else
    public class GraphicsBrushTarget : GraphicsTarget
    {
        private Bitmap bitmap;
        private SizeF size;

        public GraphicsBrushTarget(Graphics g, Bitmap bitmap, SizeF size)
        : base(g)
        {
            this.bitmap = bitmap;
            this.size = size;
        }

        public IGraphicsBrush FinishPatternBrush(float angle)
        {
            // Create a TextureBrush on the bitmap.
            TextureBrush brush = new TextureBrush(bitmap);

            // Scale and the texture brush.
            brush.RotateTransform(angle);
            brush.ScaleTransform(size.Width / (float)bitmap.Width, size.Height / (float)bitmap.Height);
            brush.TranslateTransform(-bitmap.Width / 2F, -bitmap.Height / 2F);

            // Dispose of the graphics.
            Graphics.Dispose();
            return new GDIPlus_Brush(brush);
        }
    }
#endif

#if WPF
    public class WPF_Brush : IGraphicsBrush
    {
        private Brush brush;

        public WPF_Brush(Color color)
        {
            brush = new SolidColorBrush(color);
            brush.Freeze();
        }

        public WPF_Brush(Brush brush)
        {
            this.brush = brush;
        }

        public Brush Brush
        {
            get { return brush; }
        }

        public void Dispose()
        {
            brush = null;
        }
    }

    public class WPF_Pen : IGraphicsPen
    {
        private Pen pen;

        public WPF_Pen(Pen pen)
        {
            pen.Freeze();
            this.pen = pen;
        }

        public Pen Pen
        {
            get { return pen; }
        }

        public void Dispose()
        {
            pen = null;
        }
    }

#else
    public class GDIPlus_Brush : IGraphicsBrush
    {
        private Brush brush;

        public Brush Brush
        {
            get { return brush; }
        }

        public GDIPlus_Brush(Color color)
        {
            brush = new SolidBrush(color);
        }

        public GDIPlus_Brush(Brush brush)
        {
            this.brush = brush;
        }

        public void Dispose()
        {
            brush.Dispose();
        }
    }

    public class GDIPlus_Pen : IGraphicsPen
    {
        private Pen pen;

        public Pen Pen
        {
            get { return pen; }
        }

        public GDIPlus_Pen(Pen pen)
        {
            this.pen = pen;
        }

        public void Dispose()
        {
            pen.Dispose();
        }
    }

#endif

    public static class GraphicsUtil
    {
        public static float MITER_LIMIT = 5.0F;

        // Transform points according to a matrix. Does NOT change the input points.
        public static PointF[] TransformPoints(PointF[] pts, Matrix matrix)
        {
            PointF[] xformedPts = (PointF[]) pts.Clone();
            matrix.TransformPoints(xformedPts);
            return xformedPts;
        }

        // Transform one point according to a matrix. 
        public static PointF TransformPoint(PointF pt, Matrix matrix)
        {
            PointF[] xformedPts = new PointF[1] { pt };
            matrix.TransformPoints(xformedPts);
            return xformedPts[0];
        }

#if WPF
        public static WpfMatrix GetWpfMatrix(Matrix source)
        {
            float[] elements = source.Elements;
            return new WpfMatrix(elements[0], elements[1], elements[2], elements[3], elements[4], elements[5]);
        }
#endif

        // Create a solid pen
        public static IGraphicsPen CreateSolidPen(GraphicsTarget g, Color color, float thickness, LineStyle style)
        {
            switch (style)
            {
                case LineStyle.Rounded:
                    return g.CreatePen(color, thickness, LineCap.Round, LineJoin.Round, MITER_LIMIT);
                case LineStyle.Beveled:
                    return g.CreatePen(color, thickness, LineCap.Flat, LineJoin.Bevel, MITER_LIMIT);
                case LineStyle.Mitered:
                    return g.CreatePen(color, thickness, LineCap.Flat, LineJoin.Miter, MITER_LIMIT);
                case LineStyle.FlatRounded:
                    return g.CreatePen(color, thickness, LineCap.Flat, LineJoin.Round, MITER_LIMIT);
                default:
                    throw new ArgumentException();
            }
        }

        // Multiple two matrixes, giving a third
        public static Matrix Multiply(Matrix m1, Matrix m2)
        {
            Matrix result = m1.Clone();
            result.Multiply(m2, System.Drawing.Drawing2D.MatrixOrder.Append);
            return result;
        }

    }
}
