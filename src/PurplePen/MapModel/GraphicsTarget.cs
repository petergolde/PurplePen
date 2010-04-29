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

    // A GraphicsTarget encapsulates either a Graphics (for WinForms) or a DrawingContext (for WPF)
    public class GraphicsTarget
    {
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

        public IGraphicsPath CreatePath(IEnumerable<GraphicsPathPart> parts, FillMode windingMode)
        {
            GraphicsPath path = new GraphicsPath(windingMode);
            PointF startPoint = default(PointF);

            foreach (GraphicsPathPart part in parts)
            {
                switch (part.Kind)
                {
                    case GraphicsPathPartKind.Start:
                        Debug.Assert(part.Points.Length == 1);
                        startPoint = part.Points[0];
                        path.StartFigure();
                        break;

                    case GraphicsPathPartKind.Lines:
                        {
                            PointF[] newPoints = new PointF[part.Points.Length + 1];
                            newPoints[0] = startPoint;
                            Array.Copy(part.Points, 0, newPoints, 1, part.Points.Length);
                            path.AddLines(newPoints);
                            startPoint = part.Points[part.Points.Length - 1];
                            break;
                        }

                    case GraphicsPathPartKind.Beziers:
                        {
                            PointF[] newPoints = new PointF[part.Points.Length + 1];
                            newPoints[0] = startPoint;
                            Array.Copy(part.Points, 0, newPoints, 1, part.Points.Length);
                            path.AddBeziers(newPoints);
                            startPoint = part.Points[part.Points.Length - 1];
                            break;
                        }

                    case GraphicsPathPartKind.Close:
                        path.CloseFigure();
                        break;
                }
            }

            return new GDIPlus_Path(path);
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
        public void PushClip(IGraphicsPath path)
        {
            stateStack.Push(Graphics.Save());
            Graphics.IntersectClip(new Region((path as GDIPlus_Path).GraphicsPath));
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

        // Draw an arc with a pen.
        public void DrawArc(IGraphicsPen pen, RectangleF boundingRect, float startAngle, float sweepAngle)
        {
            Graphics.DrawArc((pen as GDIPlus_Pen).Pen, boundingRect, startAngle, sweepAngle);
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

        // Draw a polygon with a brush
        public void DrawPolygon(IGraphicsPen pen, PointF[] pts)
        {
            try
            {
                Graphics.DrawPolygon((pen as GDIPlus_Pen).Pen, pts);
            }
            catch (OutOfMemoryException) {
                // Do nothing. Very occasionally, GDI+ given an out of memory exception for very short curves. Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        // Draw lines with a brush
        public void DrawPolyline(IGraphicsPen pen, PointF[] pts)
        {
            try
            {
                Graphics.DrawLines((pen as GDIPlus_Pen).Pen, pts);
            }
            catch (OutOfMemoryException)
            {
                // Do nothing. Very occasionally, GDI+ given an out of memory exception for very short curves. Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        // Fill a polygon with a brush
        public void FillPolygon(IGraphicsBrush brush, PointF[] pts, SysDraw2D.FillMode windingMode)
        {
            Graphics.FillPolygon((brush as GDIPlus_Brush).Brush, pts, windingMode);
        }

        // Draw a path with a pen.
        public void DrawPath(IGraphicsPen pen, IGraphicsPath path)
        {
            try
            {
                Graphics.DrawPath((pen as GDIPlus_Pen).Pen, (path as GDIPlus_Path).GraphicsPath);
            }
            catch (OutOfMemoryException)
            {
                // Do nothing. Very occasionally, GDI+ given an out of memory exception for very short curves. Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        // Fill a path with a brush.
        public void FillPath(IGraphicsBrush brush, IGraphicsPath path)
        {
            Graphics.FillPath((brush as GDIPlus_Brush).Brush, (path as GDIPlus_Path).GraphicsPath);
        }
    }

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

    public class GDIPlus_Path : IGraphicsPath
    {
        private GraphicsPath path;

        public GraphicsPath GraphicsPath
        {
            get { return path; }
        }

        public GDIPlus_Path(GraphicsPath path)
        {
            this.path = path;
        }

        public void Dispose()
        {
            path.Dispose();
        }
    }
}
