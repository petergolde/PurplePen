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
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

using SysDraw = System.Drawing;
using SysDraw2D = System.Drawing.Drawing2D;
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using Matrix = System.Drawing.Drawing2D.Matrix;
using FillMode = System.Drawing.Drawing2D.FillMode;
using LineJoin = System.Drawing.Drawing2D.LineJoin;
using LineCap = System.Drawing.Drawing2D.LineCap;
using WpfMatrix = System.Windows.Media.Matrix;

namespace PurplePen.MapModel
{

    // A GraphicsTarget encapsulates either a Graphics (for WinForms) or a DrawingContext (for WPF)
    public class WPF_GraphicsTarget: IGraphicsTarget
    {
        public DrawingContext DrawingContext;
        private int pushLevel;      // How many pushes have we done?

        public WPF_GraphicsTarget(DrawingContext dc)
        {
            this.DrawingContext = dc;
            pushLevel = 0;
        }

        public IGraphicsBrush CreateSolidBrush(SysDraw.Color color)
        {
            return new WPF_Brush(WpfUtil.ToWpfColor(color));
        }

        public IGraphicsPen CreatePen(SysDraw.Color color, float width, SysDraw2D.LineCap caps, SysDraw2D.LineJoin join, float miterLimit)
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

        public IBrushTarget CreatePatternBrush(SizeF size, int bitmapWidth, int bitmapHeight)
        {
            // Create a visual with the glyph to tile in it.
            DrawingVisual visual = new DrawingVisual();
            DrawingContext dc = visual.RenderOpen();
            return new WPF_BrushTarget(dc, visual, size, bitmapWidth, bitmapHeight);
        }

        // Create font
        public IGraphicsFont CreateFont(string familyName, float emHeight, bool bold, bool italic)
        {
            return new WPF_Font(familyName, emHeight, bold, italic);
        }

        public IGraphicsPath CreatePath(IEnumerable<GraphicsPathPart> parts, FillMode windingMode)
        {
            StreamGeometry geo = new StreamGeometry();
            geo.FillRule = (windingMode == FillMode.Alternate) ? FillRule.EvenOdd : FillRule.Nonzero;
            StreamGeometryContext geoContext = geo.Open();

            GraphicsPathPart[] partArray = parts.ToArray();

            for (int partIndex = 0; partIndex < partArray.Length; ++partIndex)
            {
                GraphicsPathPart part = partArray[partIndex];

                switch (part.Kind)
                {
                    case GraphicsPathPartKind.Start:
                        bool isClosed = false;
                        for (int j = partIndex + 1; j < partArray.Length; ++j) {
                            if (partArray[j].Kind == GraphicsPathPartKind.Close) {
                                isClosed = true;
                                break;
                            }
                            else if (partArray[j].Kind == GraphicsPathPartKind.Start) {
                                break;
                            }
                        }
                                
                        Debug.Assert(part.Points.Length == 1);
                        geoContext.BeginFigure(new Point(part.Points[0].X, part.Points[0].Y), isClosed, isClosed);
                        break;

                    case GraphicsPathPartKind.Lines:
                        {
                            geoContext.PolyLineTo(Array.ConvertAll<PointF, Point>(part.Points, pt => new Point(pt.X, pt.Y)), true, false);
                            break;
                        }

                    case GraphicsPathPartKind.Beziers:
                        {
                            geoContext.PolyBezierTo(Array.ConvertAll<PointF, Point>(part.Points, pt => new Point(pt.X, pt.Y)), true, false);
                            break;
                        }

                    case GraphicsPathPartKind.Close:
                        break;
                }
            }

            geoContext.Close();
            geo.Freeze();
            return new WPF_Path(geo);
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
        public void PushClip(IGraphicsPath path)
        {
            DrawingContext.PushClip((path as WPF_Path).Geometry);
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

        // Draw an arc with a pen.
        public void DrawArc(IGraphicsPen pen, RectangleF boundingRect, float startAngle, float sweepAngle)
        {
            float endAngle = startAngle + sweepAngle;
            PointF centerPoint = new PointF((boundingRect.Left + boundingRect.Right) / 2, (boundingRect.Top + boundingRect.Bottom) / 2);
            float radiusX = boundingRect.Right - centerPoint.X, radiusY = boundingRect.Bottom - centerPoint.Y;
            Point ptStart = new Point(centerPoint.X + Math.Cos(startAngle * Math.PI / 180.0) * radiusX, centerPoint.Y +  Math.Sin(startAngle * Math.PI / 180.0) * radiusY);
            Point ptEnd = new Point(centerPoint.X + Math.Cos(endAngle * Math.PI / 180.0) * radiusX, centerPoint.Y + Math.Sin(endAngle * Math.PI / 180.0) * radiusY);
            ArcSegment segment = new ArcSegment(ptEnd, new Size(radiusX, radiusY), 0, sweepAngle > 180.0F, SweepDirection.Clockwise, true);
            PathFigure figure = new PathFigure(ptStart, new PathSegment[] { segment }, false);
            PathGeometry geometry = new PathGeometry(new PathFigure[] { figure });
            DrawingContext.DrawGeometry(null, (pen as WPF_Pen).Pen, geometry);
        }

        // Draw an ellipse with a pen.
        public void DrawEllipse(IGraphicsPen pen, PointF center, float radiusX, float radiusY)
        {
            DrawingContext.DrawEllipse(null, (pen as WPF_Pen).Pen, new Point(center.X, center.Y), radiusX, radiusY);
        }

        // Fill an ellipse with a pen.
        public void FillEllipse(IGraphicsBrush brush, PointF center, float radiusX, float radiusY)
        {
            DrawingContext.DrawEllipse((brush as WPF_Brush).Brush, null, new Point(center.X, center.Y), radiusX, radiusY);
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
        public void DrawPolygon(IGraphicsPen pen, PointF[] pts)
        {
            Point[] points = new Point[pts.Length - 1];
            for (int i = 1; i < pts.Length; ++i)
                points[i - 1] = new Point(pts[i].X, pts[i].Y);
            Point startPoint = new Point(pts[0].X, pts[0].Y);

            PathSegment segment = new PolyLineSegment(points, true);
            PathFigure figure = new PathFigure(startPoint, new PathSegment[] { segment }, true);
            PathGeometry geometry = new PathGeometry(new PathFigure[] { figure }, FillRule.EvenOdd, System.Windows.Media.Transform.Identity);
            DrawingContext.DrawGeometry(null, (pen as WPF_Pen).Pen, geometry);
        }

        // Fill a polygon with a brush
        public void DrawPolyline(IGraphicsPen pen, PointF[] pts)
        {
            Point[] points = new Point[pts.Length - 1];
            for (int i = 1; i < pts.Length; ++i)
                points[i - 1] = new Point(pts[i].X, pts[i].Y);
            Point startPoint = new Point(pts[0].X, pts[0].Y);

            PathSegment segment = new PolyLineSegment(points, true);
            PathFigure figure = new PathFigure(startPoint, new PathSegment[] { segment }, false);
            PathGeometry geometry = new PathGeometry(new PathFigure[] { figure }, FillRule.EvenOdd, System.Windows.Media.Transform.Identity);
            DrawingContext.DrawGeometry(null, (pen as WPF_Pen).Pen, geometry);
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

        // Draw a path with a pen.
        public void DrawPath(IGraphicsPen pen, IGraphicsPath path)
        {
            DrawingContext.DrawGeometry(null, (pen as WPF_Pen).Pen, (path as WPF_Path).Geometry);
        }

        // Fill a path with a brush.
        public void FillPath(IGraphicsBrush brush, IGraphicsPath path)
        {
            DrawingContext.DrawGeometry((brush as WPF_Brush).Brush, null, (path as WPF_Path).Geometry);
        }

        // Draw text with upper-left corner of text at the given locations.
        public void DrawText(string text, IGraphicsFont font, IGraphicsBrush brush, PointF upperLeft)
        {
            Typeface typeface = (font as WPF_Font).Typeface;
            float emHeight = (font as WPF_Font).EmHeight;
            FormattedText formattedText = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, emHeight, (brush as WPF_Brush).Brush);
            DrawingContext.DrawText(formattedText, new Point(upperLeft.X, upperLeft.Y));
        }

        // Draw text outline with upper-left corner of text at the given locations.
        public void DrawTextOutline(string text, IGraphicsFont font, IGraphicsPen pen, PointF upperLeft)
        {
            Typeface typeface = (font as WPF_Font).Typeface;
            float emHeight = (font as WPF_Font).EmHeight;
            FormattedText formattedText = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, emHeight, Brushes.Black);
            Geometry geometry = formattedText.BuildGeometry(new Point(upperLeft.X, upperLeft.Y));
            DrawingContext.DrawGeometry(null, (pen as WPF_Pen).Pen, geometry);
        }

        public void Dispose()
        { }

        private static WpfMatrix GetWpfMatrix(Matrix source)
        {
            float[] elements = source.Elements;
            return new WpfMatrix(elements[0], elements[1], elements[2], elements[3], elements[4], elements[5]);
        }

    }

    public class WPF_BrushTarget : WPF_GraphicsTarget, IBrushTarget
    {
        private DrawingVisual visual;
        private SizeF size;
        private int bitmapWidth, bitmapHeight;

        public WPF_BrushTarget(DrawingContext dc, DrawingVisual visual, SizeF size, int bitmapWidth, int bitmapHeight)
        : base(dc)
        {
            this.visual = visual;
            this.size = size;
            this.bitmapWidth = bitmapWidth;
            this.bitmapHeight = bitmapHeight;
        }

        public IGraphicsBrush FinishBrush(float rotationAngle)
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

    public class WPF_Path : IGraphicsPath
    {
        private Geometry geometry;

        public Geometry Geometry
        {
            get { return geometry; }
        }

        public WPF_Path(Geometry geometry)
        {
            this.geometry = geometry;
        }

        public void Dispose()
        {
            geometry = null;
        }
    }

    public class WPF_Font : IGraphicsFont
    {
        private Typeface typeface;
        private float emHeight;

        public WPF_Font(string familyName, float emHeight, bool bold, bool italic)
        {
            this.emHeight = emHeight;
            this.typeface = WpfUtil.CreateTypeface(familyName, bold, italic);
        }

        public Typeface Typeface
        {
            get { return typeface; }
        }

        public float EmHeight 
        {
            get { return emHeight; }
        }

        public void Dispose()
        {
        }
    }

    public class WPF_TextMetrics : ITextMetrics
    {
        public ITextFaceMetrics GetTextFaceMetrics(string familyName, float emHeight, bool bold, bool italic)
        {
            if (!TextFaceIsInstalled(familyName))
                familyName = "Arial";          // Map non-existant fonts to "Arial".

            return new WPF_TextFaceMetrics(familyName, emHeight, bold, italic);
        }

        public bool TextFaceIsInstalled(string familyName)
        {
            // Get the glyphTypeface to see if the font exists.
            GlyphTypeface glyphTypeface;
            Typeface typeface = new Typeface(familyName);
            return typeface.TryGetGlyphTypeface(out glyphTypeface);
        }

        public void Dispose()
        {
        }
    }

    public class WPF_TextFaceMetrics: ITextFaceMetrics
    {
        private Typeface typeface;
        private FontFamily family;
        private GlyphTypeface glyphTypeface;
        private float emHeight;

        public WPF_TextFaceMetrics(string familyName, float emHeight, bool bold, bool italic)
        {
            this.emHeight = emHeight;
            typeface = WpfUtil.CreateTypeface(familyName, bold, italic);
            family = typeface.FontFamily;
            glyphTypeface = null;
            typeface.TryGetGlyphTypeface(out glyphTypeface);
        }

        public float EmHeight
        {
            get { return emHeight; }
        }

        public float Ascent
        {
            get { return (float)(family.Baseline * emHeight); }
        }

        public float Descent
        {
            get { return (float)((glyphTypeface.Height - family.Baseline) * emHeight); }
        }

        public float CapHeight
        {
            get { return (float)(typeface.CapsHeight * emHeight); }
        }

        float spaceWidth = -1;
        public float SpaceWidth
        {
            get {
                if (spaceWidth < 0) {
                    spaceWidth = GetTextWidth(" ");
                }
                return spaceWidth;
            }
        }

        public float GetTextWidth(string text)
        {
            FormattedText formattedText = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, emHeight, Brushes.Black);
            return (float) formattedText.WidthIncludingTrailingWhitespace;
        }

        public SizeF GetTextSize(string text)
        {
            FormattedText formattedText = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, emHeight, Brushes.Black);
            return new SizeF((float)formattedText.WidthIncludingTrailingWhitespace, (float)formattedText.Height);
        }

        public void Dispose()
        {
        }
    }

    public static class WpfUtil
    {
        public static WpfMatrix GetWpfMatrix(Matrix source)
        {
            float[] elements = source.Elements;
            return new WpfMatrix(elements[0], elements[1], elements[2], elements[3], elements[4], elements[5]);
        }

        public static Color ToWpfColor(SysDraw.Color color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static bool FontExists(string fontName)
        {
            // Get the glyphTypeface to see if the font exists.
            GlyphTypeface glyphTypeface;
            Typeface typeface = new Typeface(fontName);
            return typeface.TryGetGlyphTypeface(out glyphTypeface);
        }

        [DllImport("shell32.dll")]
        private static extern bool SHGetSpecialFolderPath(IntPtr hwndOwner,
           [Out] StringBuilder lpszPath, int nFolder, bool fCreate);
        const int CSIDL_FONTS = 0x14;  // Font folder

        // Create a Typeface, taking into account a nasty WPF bug regarding Arial Narrow.
        public static Typeface CreateTypeface(string fontName, bool bold, bool italic)
        {
            if (!FontExists(fontName))
                fontName = "Arial";          // Map non-existant fonts to "Arial".

            if (fontName == "Arial Narrow") {
                // Arial Narrow doesn't work right in WPF. We can work around by going directly to the font file.
                string fontfileName;
                if (!bold && !italic)
                    fontfileName = "arialn.ttf";
                else if (bold && !italic)
                    fontfileName = "arialnb.ttf";
                else if (!bold && italic)
                    fontfileName = "arialni.ttf";
                else
                    fontfileName = "arialnbi.ttf";

                // Get font folder
                StringBuilder fontPath = new StringBuilder(260);
                if (SHGetSpecialFolderPath(IntPtr.Zero, fontPath, CSIDL_FONTS, false)) {
                    // Get path to font name.
                    string fontfile = Path.Combine(fontPath.ToString(), fontfileName);
                    UriBuilder fontfileUriBuilder = new UriBuilder(new Uri(fontfile));
                    fontfileUriBuilder.Fragment = "Arial";
                    Typeface typeface = new Typeface(new FontFamily(fontfileUriBuilder.Uri.ToString()), italic ? FontStyles.Italic : FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Condensed);
                    GlyphTypeface gtf;
                    if (typeface.TryGetGlyphTypeface(out gtf))
                        return typeface;           // Make sure that the font file really exists, by getting the glyph typeface.
                }
            }

            return new Typeface(new FontFamily(fontName), italic ? FontStyles.Italic : FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
        }

    }
}
