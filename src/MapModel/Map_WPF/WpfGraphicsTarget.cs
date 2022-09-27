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
using Bitmap = System.Drawing.Bitmap;
using WpfMatrix = System.Windows.Media.Matrix;

namespace PurplePen.MapModel
{
    using PurplePen.Graphics2D;
    using Geometry = System.Windows.Media.Geometry;
    using System.Windows.Media.Imaging;

    // A GraphicsTarget encapsulates either a Graphics (for WinForms) or a DrawingContext (for WPF)
    public class WPF_GraphicsTarget: IGraphicsTarget
    {
        public DrawingContext DrawingContext;
        private WPF_ColorConverter colorConverter;
        private int pushLevel;      // How many pushes have we done?
        private Dictionary<object, Pen> penMap = new Dictionary<object, Pen>(new IdentityComparer<object>());
        private Dictionary<object, Brush> brushMap = new Dictionary<object, Brush>(new IdentityComparer<object>());
        private Dictionary<object, WPF_Font> fontMap = new Dictionary<object, WPF_Font>(new IdentityComparer<object>());
        private Dictionary<object, Geometry> geometryMap = new Dictionary<object, Geometry>(new IdentityComparer<object>());

        public WPF_GraphicsTarget(DrawingContext dc, WPF_ColorConverter colorConverter)
        {
            this.DrawingContext = dc;
            this.colorConverter = colorConverter ?? new WPF_ColorConverter();
            pushLevel = 0;
        }

        public WPF_GraphicsTarget(DrawingContext dc) : this(dc, null)
        {
        }

        public WPF_ColorConverter ColorConverter
        {
            get { return colorConverter; }
        }
        
        public void CreateSolidBrush(object brushKey, CmykColor color)
        {
            if (brushMap.ContainsKey(brushKey))
                throw new InvalidOperationException("Key already has a brush created for it");

            Brush brush = new SolidColorBrush(colorConverter.ToColor(color));
            brush.Freeze();
            brushMap.Add(brushKey, brush);
        }

        public void CreatePen(object penKey, CmykColor color, float width, SysDraw2D.LineCap caps, SysDraw2D.LineJoin join, float miterLimit)
        {
            object brushKey = new object();
            CreateSolidBrush(brushKey, color);
            CreatePen(penKey, brushKey, width, caps, join, miterLimit);
        }

        public void CreatePen(object penKey, object brushKey, float width, SysDraw2D.LineCap caps, SysDraw2D.LineJoin join, float miterLimit)
        {
            if (penMap.ContainsKey(penKey))
                throw new InvalidOperationException("Key already has a pen created for it");

            Pen pen = new Pen(GetBrush(brushKey), width);
            
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
            penMap.Add(penKey, pen);
        }

        public bool SupportsPatternBrushes
        {
            get { return true; }
        }

        public IBrushTarget CreatePatternBrush(SizeF size, float angle, int bitmapWidth, int bitmapHeight)
        {
            // Create a visual with the glyph to tile in it.
            DrawingVisual visual = new DrawingVisual();
            DrawingContext dc = visual.RenderOpen();
            return new WPF_BrushTarget(this, dc, visual, size, angle, bitmapWidth, bitmapHeight);
        }

        // Create font
        public void CreateFont(object fontKey, string familyName, float emHeight, TextEffects effects)
        {
            if (fontMap.ContainsKey(fontKey))
                throw new InvalidOperationException("Key already has a font created for it");

            WPF_Font font = new WPF_Font(familyName, emHeight, effects);
            fontMap.Add(fontKey, font);
        }

        public void CreatePath(object pathKey, List<GraphicsPathPart> parts, FillMode windingMode)
        {
            if (geometryMap.ContainsKey(pathKey))
                throw new InvalidOperationException("Key already has a path created for it");

            StreamGeometry geo = GetGeometry(parts, windingMode);
            geometryMap.Add(pathKey, geo);
        }

        private StreamGeometry GetGeometry(List<GraphicsPathPart> parts, FillMode windingMode)
        {
            StreamGeometry geo = new StreamGeometry();
            geo.FillRule = (windingMode == FillMode.Alternate) ? FillRule.EvenOdd : FillRule.Nonzero;
            StreamGeometryContext geoContext = geo.Open();

            GraphicsPathPart[] partArray = parts.ToArray();

            for (int partIndex = 0; partIndex < partArray.Length; ++partIndex) {
                GraphicsPathPart part = partArray[partIndex];

                switch (part.Kind) {
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

                    case GraphicsPathPartKind.Lines: {
                            geoContext.PolyLineTo(Array.ConvertAll<PointF, Point>(part.Points, pt => new Point(pt.X, pt.Y)), true, false);
                            break;
                        }

                    case GraphicsPathPartKind.Beziers: {
                            geoContext.PolyBezierTo(Array.ConvertAll<PointF, Point>(part.Points, pt => new Point(pt.X, pt.Y)), true, false);
                            break;
                        }

                    case GraphicsPathPartKind.Close:
                        break;
                }
            }

            geoContext.Close();
            geo.Freeze();
            return geo;
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
        public void PushClip(object pathKey)
        {
            DrawingContext.PushClip(GetGeometry(pathKey));
            ++pushLevel;
        }

        public void PushClip(List<GraphicsPathPart> parts, FillMode windingMode)
        {
            StreamGeometry geo = GetGeometry(parts, windingMode);
            DrawingContext.PushClip(geo);
            ++pushLevel;
        }

        public void PushClip(RectangleF rect)
        {
            DrawingContext.PushClip(new RectangleGeometry(new Rect(rect.X, rect.Y, rect.Width, rect.Height)));
            ++pushLevel;
        }

        public void PushClip(RectangleF[] rects)
        {
            GeometryCollection collection = new GeometryCollection(from r in rects select new RectangleGeometry(new Rect(r.X, r.Y, r.Width, r.Height)));
            GeometryGroup group = new GeometryGroup() { Children = collection, FillRule = FillRule.Nonzero };

            DrawingContext.PushClip(group);
            ++pushLevel;
        }

        public void PopClip()
        {
            DrawingContext.Pop();
        }

        // Push an anti-aliasing mode.
        public void PushAntiAliasing(bool antiAlias)
        {
            // not supported.
        }

        // Pop anti-aliases mode.
        public void PopAntiAliasing()
        {
            // not supported
        }

        // Set blending mode.
        public virtual bool PushBlending(BlendMode blendMode)
        {
            // Blending not supported.
            return false;
        }

        public virtual void PopBlending()
        {}

        // Draw an line with a pen.
        public void DrawLine(object penKey, PointF start, PointF finish)
        {
            DrawingContext.DrawLine(GetPen(penKey), new Point(start.X, start.Y), new Point(finish.X, finish.Y));
        }

        // Draw an arc with a pen.
        public void DrawArc(object penKey, PointF center, float radius, float startAngle, float sweepAngle)
        {
            float endAngle = startAngle + sweepAngle;
            Point ptStart = new Point(center.X + Math.Cos(startAngle * Math.PI / 180.0) * radius, center.Y +  Math.Sin(startAngle * Math.PI / 180.0) * radius);
            Point ptEnd = new Point(center.X + Math.Cos(endAngle * Math.PI / 180.0) * radius, center.Y + Math.Sin(endAngle * Math.PI / 180.0) * radius);
            ArcSegment segment = new ArcSegment(ptEnd, new Size(radius, radius), 0, sweepAngle > 180.0F, SweepDirection.Clockwise, true);
            PathFigure figure = new PathFigure(ptStart, new PathSegment[] { segment }, false);
            PathGeometry geometry = new PathGeometry(new PathFigure[] { figure });
            DrawingContext.DrawGeometry(null, GetPen(penKey), geometry);
        }

        // Draw an ellipse with a pen.
        public void DrawEllipse(object penKey, PointF center, float radiusX, float radiusY)
        {
            DrawingContext.DrawEllipse(null, GetPen(penKey), new Point(center.X, center.Y), radiusX, radiusY);
        }

        // Fill an ellipse with a pen.
        public void FillEllipse(object brushKey, PointF center, float radiusX, float radiusY)
        {
            DrawingContext.DrawEllipse(GetBrush(brushKey), null, new Point(center.X, center.Y), radiusX, radiusY);
        }

        // Draw a rectangle with a pen.
        public void DrawRectangle(object penKey, RectangleF rect)
        {
            DrawingContext.DrawRectangle(null, GetPen(penKey), new Rect(rect.X, rect.Y, rect.Width, rect.Height));
        }

        // Fill a rectangle with a brush.
        public void FillRectangle(object brushKey, RectangleF rect)
        {
            DrawingContext.DrawRectangle(GetBrush(brushKey), null, new Rect(rect.X, rect.Y, rect.Width, rect.Height));
        }

        // Fill a polygon with a brush
        public void DrawPolygon(object penKey, PointF[] pts)
        {
            Point[] points = new Point[pts.Length - 1];
            for (int i = 1; i < pts.Length; ++i)
                points[i - 1] = new Point(pts[i].X, pts[i].Y);
            Point startPoint = new Point(pts[0].X, pts[0].Y);

            PathSegment segment = new PolyLineSegment(points, true);
            PathFigure figure = new PathFigure(startPoint, new PathSegment[] { segment }, true);
            PathGeometry geometry = new PathGeometry(new PathFigure[] { figure }, FillRule.EvenOdd, System.Windows.Media.Transform.Identity);
            DrawingContext.DrawGeometry(null, GetPen(penKey), geometry);
        }

        // Fill a polygon with a brush
        public void DrawPolyline(object penKey, PointF[] pts)
        {
            Point[] points = new Point[pts.Length - 1];
            for (int i = 1; i < pts.Length; ++i)
                points[i - 1] = new Point(pts[i].X, pts[i].Y);
            Point startPoint = new Point(pts[0].X, pts[0].Y);

            PathSegment segment = new PolyLineSegment(points, true);
            PathFigure figure = new PathFigure(startPoint, new PathSegment[] { segment }, false);
            PathGeometry geometry = new PathGeometry(new PathFigure[] { figure }, FillRule.EvenOdd, System.Windows.Media.Transform.Identity);
            DrawingContext.DrawGeometry(null, GetPen(penKey), geometry);
        }

        // Fill a polygon with a brush
        public void FillPolygon(object brushKey, PointF[] pts, SysDraw2D.FillMode windingMode)
        {
            Point[] points = new Point[pts.Length];
            for (int i = 0; i < pts.Length; ++i)
                points[i] = new Point(pts[i].X, pts[i].Y);

            PathSegment segment = new PolyLineSegment(points, true);
            PathFigure figure = new PathFigure(points[points.Length - 1], new PathSegment[] { segment }, true);
            PathGeometry geometry = new PathGeometry(new PathFigure[] { figure }, windingMode == SysDraw2D.FillMode.Winding ? FillRule.Nonzero : FillRule.EvenOdd, System.Windows.Media.Transform.Identity);
            DrawingContext.DrawGeometry(GetBrush(brushKey), null, geometry);
        }

        // Draw a path with a pen.
        public void DrawPath(object penKey, object pathKey)
        {
            DrawingContext.DrawGeometry(null, GetPen(penKey), GetGeometry(pathKey));
        }

        public void DrawPath(object penKey, List<GraphicsPathPart> parts)
        {
            StreamGeometry geo = GetGeometry(parts, FillMode.Alternate);
            DrawingContext.DrawGeometry(null, GetPen(penKey), geo);
        }

        // Fill a path with a brush.
        public void FillPath(object brushKey, object pathKey)
        {
            DrawingContext.DrawGeometry(GetBrush(brushKey), null, GetGeometry(pathKey));
        }

        public void FillPath(object brushKey, List<GraphicsPathPart> parts, FillMode windingMode)
        {
            StreamGeometry geo = GetGeometry(parts, windingMode);
            DrawingContext.DrawGeometry(GetBrush(brushKey), null, geo);
        }

        // Draw text with upper-left corner of text at the given locations.
        public void DrawText(string text, object fontKey, object brushKey, PointF upperLeft)
        {
            WPF_Font font = GetFont(fontKey);
            Typeface typeface = font.Typeface;
            float emHeight = font.EmHeight;
            FormattedText formattedText = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, emHeight, GetBrush(brushKey));
            if (font.Underline)
                formattedText.SetTextDecorations(TextDecorations.Underline);
            DrawingContext.DrawText(formattedText, new Point(upperLeft.X, upperLeft.Y + font.VerticalDisplacement));
        }

        // Draw text outline with upper-left corner of text at the given locations.
        public void DrawTextOutline(string text, object fontKey, object penKey, PointF upperLeft)
        {
            WPF_Font font = GetFont(fontKey);
            Typeface typeface = (font as WPF_Font).Typeface;
            float emHeight = (font as WPF_Font).EmHeight;
            FormattedText formattedText = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, emHeight, Brushes.Black);
            Geometry geometry = formattedText.BuildGeometry(new Point(upperLeft.X, upperLeft.Y + font.VerticalDisplacement));
            DrawingContext.DrawGeometry(null, GetPen(penKey), geometry);
        }

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        // Draw a bitmap
        public void DrawBitmap(IGraphicsBitmap bm, RectangleF rectangle, BitmapScaling scalingMode, float minResolution)
        {
            DrawBitmapPart(bm, 0, 0, bm.PixelWidth, bm.PixelHeight, rectangle, scalingMode, minResolution);
        }

        // Draw part of a bitmap
        public void DrawBitmapPart(IGraphicsBitmap bm, int x, int y, int width, int height, RectangleF rectangle, BitmapScaling scalingMode, float minResolution)
        {
            GDIPlus_Bitmap gdiBitmap = (GDIPlus_Bitmap)bm;
            var hBitmap = gdiBitmap.Bitmap.GetHbitmap();

            try {
                BitmapSource bitSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    new Int32Rect(0, 0, gdiBitmap.PixelWidth, gdiBitmap.PixelHeight),
                    BitmapSizeOptions.FromEmptyOptions());

                if (x != 0 || y != 0 || width != bm.PixelWidth || height != bm.PixelHeight) {
                    bitSrc = new CroppedBitmap(bitSrc, new Int32Rect(x, y, width, height));
                }

                DrawingContext.DrawImage(new CachedBitmap(bitSrc, BitmapCreateOptions.None, BitmapCacheOption.Default), 
                                         new Rect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height));
            }
            finally {
                DeleteObject(hBitmap);
            }
        }
        
        public bool HasPath(object pathKey)
        {
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

        private Pen GetPen(object penKey) {
            Pen pen;
            if (penMap.TryGetValue(penKey, out pen))
                return pen;
            else
                throw new ArgumentException("Given key does not have a pen created for it", "penKey");
        }

        private WPF_Font GetFont(object fontKey) {
            WPF_Font font;
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

        public void Dispose() {
            penMap.Clear();
            brushMap.Clear();
            geometryMap.Clear();
            fontMap.Clear();
        }

        private static WpfMatrix GetWpfMatrix(Matrix source)
        {
            float[] elements = source.Elements;
            return new WpfMatrix(elements[0], elements[1], elements[2], elements[3], elements[4], elements[5]);
        }

        public class WPF_BrushTarget : WPF_GraphicsTarget, IBrushTarget
        {
            private WPF_GraphicsTarget owningTarget;
            private DrawingVisual visual;
            private SizeF size;
            private float angle;
            private int bitmapWidth, bitmapHeight;

            public WPF_BrushTarget(WPF_GraphicsTarget owningTarget, DrawingContext dc, DrawingVisual visual, SizeF size, float angle, int bitmapWidth, int bitmapHeight)
                : base(dc) {
                this.owningTarget = owningTarget;
                this.visual = visual;
                this.size = size;
                this.angle = angle;
                this.bitmapWidth = bitmapWidth;
                this.bitmapHeight = bitmapHeight;
            }

            public void FinishBrush(object brushKey) {
                DrawingContext.Close();

                if (owningTarget.brushMap.ContainsKey(brushKey))
                    throw new InvalidOperationException("Key already has a brush created for it");

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
                brush.Transform = new RotateTransform(angle);

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
                owningTarget.brushMap.Add(brushKey, brush);
            }
        }

    }

    public class WPF_Font
    {
        private Typeface typeface;
        private float emHeight;
        private bool underline;
        private float verticalDisplacement;

        public WPF_Font(string familyName, float emHeight, TextEffects effects)
        {
            this.emHeight = emHeight;
            var metrics = WPF_TextMetrics.GetMetrics(familyName, emHeight, effects);
            this.typeface = metrics.Typeface;
            this.underline = metrics.Underline;
            this.verticalDisplacement = metrics.VerticalDisplacement;
        }

        public Typeface Typeface
        {
            get { return typeface; }
        }

        public float EmHeight 
        {
            get { return emHeight; }
        }

        public bool Underline
        {
            get { return underline; }
        }

        // WPF sometimes need to offset vertically some fonts to achieve the same drawing 
        // as GDI+ does.
        internal float VerticalDisplacement
        {
            get { return verticalDisplacement; }
        }

        public void Dispose()
        {
        }
    }

    public class WPF_TextMetrics : ITextMetrics
    {
        public static WPF_TextFaceMetrics GetMetrics(string familyName, float emHeight, TextEffects effects)
        {
            if (!IsTextFaceInstalled(familyName))
                familyName = "Arial";          // Map non-existant fonts to "Arial".

            return new WPF_TextFaceMetrics(familyName, emHeight, effects);
        }

        public static bool IsTextFaceInstalled(string familyName)
        {
            // Get the glyphTypeface to see if the font exists.
            GlyphTypeface glyphTypeface;
            Typeface typeface = new Typeface(familyName);
            return typeface.TryGetGlyphTypeface(out glyphTypeface);
        }

        public ITextFaceMetrics GetTextFaceMetrics(string familyName, float emHeight, TextEffects effects)
        {
            return GetMetrics(familyName, emHeight, effects);
        }

        public bool TextFaceIsInstalled(string familyName)
        {
            return IsTextFaceInstalled(familyName);
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
        private bool underline;

        public WPF_TextFaceMetrics(string familyName, float emHeight, TextEffects effects)
        {
            this.emHeight = emHeight;
            this.underline = (effects & TextEffects.Underline) != 0;
            typeface = WpfUtil.CreateTypeface(familyName, effects);
            family = typeface.FontFamily;
            glyphTypeface = null;
            typeface.TryGetGlyphTypeface(out glyphTypeface);
        }

        public float EmHeight
        {
            get { return emHeight; }
        }

        public float RecommendedLineSpacing
        {
            get
            {
                return (float)(family.LineSpacing * emHeight);
            }
        }

        public float Ascent
        {
            get { return (float)(glyphTypeface.Baseline * emHeight); }
        }

        public float Descent
        {
            get { return (float)((glyphTypeface.Height - glyphTypeface.Baseline) * emHeight); }
        }

        public float CapHeight
        {
            get { return (float)(glyphTypeface.CapsHeight * emHeight); }
        }

        internal float VerticalDisplacement
        {
            get { return (float) ((glyphTypeface.Baseline - family.Baseline) * emHeight); }
        }

        internal Typeface Typeface
        {
            get { return typeface; }
        }

        internal bool Underline
        {
            get { return underline; }
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
        public static Typeface CreateTypeface(string fontName, TextEffects effects)
        {
            if (!FontExists(fontName))
                fontName = "Arial";          // Map non-existant fonts to "Arial".

            bool bold = (effects & TextEffects.Bold) != 0;
            bool italic = (effects & TextEffects.Italic) != 0;
            bool underline = (effects & TextEffects.Underline) != 0;

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

    public class WPF_ColorConverter
    {
        public virtual Color ToColor(CmykColor cmykColor)
        {
            SysDraw.Color sysColor = ColorConverter.ToColor(cmykColor);
            return Color.FromArgb(sysColor.A, sysColor.R, sysColor.G, sysColor.B);
        }
    }
}
