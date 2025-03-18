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



namespace PurplePen.MapModel
{
    using PurplePen.Graphics2D;
    using SkiaSharp;

    // A GraphicsTarget encapsulates an SKCanvas
    public class Skia_GraphicsTarget: IGraphicsTarget
    {
        private SKCanvas canvas;
        private SkiaColorConverter colorConverter;
        private int pushLevel;      // How many pushes have we done?
        private Dictionary<object, SKPaint> penMap = new Dictionary<object, SKPaint>(new IdentityComparer<object>());
        private Dictionary<object, SKPaint> brushMap = new Dictionary<object, SKPaint>(new IdentityComparer<object>());
        private Dictionary<object, SkiaFont> fontMap = new Dictionary<object, SkiaFont>(new IdentityComparer<object>());
        private Dictionary<object, SKPath> pathMap = new Dictionary<object, SKPath>(new IdentityComparer<object>());
        private Stack<bool> antiAliasStack = new Stack<bool>();
        private bool antiAlias;

        public Skia_GraphicsTarget(SKCanvas canvas, SkiaColorConverter colorConverter, float intensity = 1.0F)
        {
            this.canvas = canvas;
            pushLevel = 0;
            this.colorConverter = colorConverter ?? new SkiaColorConverter();
            this.antiAlias = false;

            // TODO: handle intensity
        }

        public Skia_GraphicsTarget(SKCanvas canvas) : this(canvas, null)
        {
        }

        //public WPF_ColorConverter ColorConverter
        //{
        //    get { return colorConverter; }
        //}

        public SKCanvas Canvas
        {
            get { return canvas; }
        }

        public void CreateSolidBrush(object brushKey, CmykColor color)
        {
            if (brushMap.ContainsKey(brushKey))
                throw new InvalidOperationException("Key already has a brush created for it");

            SKPaint paint = new SKPaint();
            paint.Color = colorConverter.ToColor(color);
            paint.IsStroke = false;
            brushMap.Add(brushKey, paint);
        }

        private void CreatePenCore(object penKey, SKPaint basePaint, float width, SysDraw2D.LineCap caps, SysDraw2D.LineJoin join, float miterLimit)
        {
            if (penMap.ContainsKey(penKey))
                throw new InvalidOperationException("Key already has a pen created for it");

            SKPaint paint = basePaint.Clone();
            paint.IsStroke = true;
            paint.StrokeWidth = width;

            switch (caps) {
                case System.Drawing.Drawing2D.LineCap.Flat:
                    paint.StrokeCap = SKStrokeCap.Butt;
                    break;
                case System.Drawing.Drawing2D.LineCap.Round:
                    paint.StrokeCap = SKStrokeCap.Round;
                    break;
                case System.Drawing.Drawing2D.LineCap.Square:
                    paint.StrokeCap = SKStrokeCap.Square;
                    break;
                default:
                    throw new ArgumentException("bad line cap", "caps");
            }

            switch (join) {
                case System.Drawing.Drawing2D.LineJoin.Bevel:
                    paint.StrokeJoin = SKStrokeJoin.Bevel;
                    break;
                case System.Drawing.Drawing2D.LineJoin.Miter:
                    paint.StrokeJoin = SKStrokeJoin.Miter;
                    paint.StrokeMiter = miterLimit;
                    break;
                case System.Drawing.Drawing2D.LineJoin.Round:
                    paint.StrokeJoin = SKStrokeJoin.Round;
                    break;
                default:
                    throw new ArgumentException("bad line join", "join");
            }

            penMap.Add(penKey, paint);
        }

        public void CreatePen(object penKey, CmykColor color, float width, SysDraw2D.LineCap caps, SysDraw2D.LineJoin join, float miterLimit)
        {
            SKPaint paint = new SKPaint();
            paint.Color = colorConverter.ToColor(color);
            CreatePenCore(penKey, paint, width, caps, join, miterLimit);
        }

        public void CreatePen(object penKey, object brushKey, float width, SysDraw2D.LineCap caps, SysDraw2D.LineJoin join, float miterLimit)
        {
            SKPaint brushPaint = GetBrushPaint(brushKey);
            CreatePenCore(penKey, brushPaint, width, caps, join, miterLimit);
        }

        public bool SupportsPatternBrushes
        {
            get { return true; }
        }

        public IBrushTarget CreatePatternBrush(SizeF size, float angle, int bitmapWidth, int bitmapHeight)
        {
            // Create a new bitmap and fill it transparent.
            SKBitmap bitmap = new SKBitmap(bitmapWidth, bitmapHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            bitmap.Erase(SKColors.Transparent);

            IntPtr length; // not needed.
            SKSurface surface = SKSurface.Create(new SKImageInfo(bitmapWidth, bitmapHeight, SKImageInfo.PlatformColorType, SKAlphaType.Premul), bitmap.GetPixels(out length), bitmap.RowBytes);
            SKCanvas newCanvas = surface.Canvas;
            newCanvas.Translate((float)bitmapWidth / 2F, (float)bitmapHeight / 2F);
            newCanvas.Scale((float)bitmapWidth / size.Width, (float)bitmapHeight / size.Height);

            return new Skia_BrushTarget(this, newCanvas, surface, bitmap, size, angle);
        }

        // Create font
        public void CreateFont(object fontKey, string familyName, float emHeight, TextEffects effects)
        {
            if (fontMap.ContainsKey(fontKey))
                throw new InvalidOperationException("Key already has a font created for it");

            SkiaFont font = new SkiaFont(familyName, emHeight, effects);
            fontMap.Add(fontKey, font);
        }

        public void CreatePath(object pathKey, List<GraphicsPathPart> parts, FillMode windingMode)
        {
            if (pathMap.ContainsKey(pathKey))
                throw new InvalidOperationException("Key already has a path created for it");

            SKPath path = GetPath(parts, windingMode);
            pathMap.Add(pathKey, path);
        }

        private SKPath GetPath(List<GraphicsPathPart> parts, FillMode windingMode)
        {
            SKPath path = new SKPath();
            
            path.FillType = (windingMode == FillMode.Alternate) ? SKPathFillType.EvenOdd : SKPathFillType.Winding;

            int count = parts.Count;
            for (int partIndex = 0; partIndex < count; ++partIndex) {
                GraphicsPathPart part = parts[partIndex];

                switch (part.Kind) {
                    case GraphicsPathPartKind.Start:
                        Debug.Assert(part.Points.Length == 1);
                        path.MoveTo(part.Points[0].X, part.Points[0].Y);
                        break;

                    case GraphicsPathPartKind.Lines: 
                        foreach (PointF pt in part.Points) {
                            path.LineTo(pt.X, pt.Y);
                        }
                        break;

                    case GraphicsPathPartKind.Beziers: 
                        for (int i = 0; i < part.Points.Length; i += 3) {
                            path.CubicTo(part.Points[i].X, part.Points[i].Y, part.Points[i+1].X, part.Points[i+1].Y, part.Points[i+2].X, part.Points[i+2].Y);
                        }
                        break;

                    case GraphicsPathPartKind.Close:
                        path.Close();
                        break;
                }
            }

            return path;
        }

        // Prepend a transform to the graphics drawing target.
        public void PushTransform(Matrix matrix)
        {
            canvas.Save();
            ++pushLevel;

            SKMatrix mat = GetSkMatrix(matrix);
            canvas.Concat(ref mat);
        }

        public void PopTransform()
        {
            Debug.Assert(pushLevel > 0);
            --pushLevel;
            canvas.Restore();
        }

        private void PushClip(SKPath path)
        {
            canvas.Save();
            ++pushLevel;

            canvas.ClipPath(path);
            ++pushLevel;
        }

        // Set a clip on the graphics drawing target.
        public void PushClip(object pathKey)
        {
            PushClip(GetSkPath(pathKey));
        }

        public void PushClip(List<GraphicsPathPart> parts, FillMode windingMode)
        {
            using (SKPath path = GetPath(parts, windingMode)) {
                PushClip(path);
            }
        }

        public void PushClip(RectangleF rect)
        {
            canvas.Save();
            ++pushLevel;

            canvas.ClipRect(GetSKRect(rect));
            ++pushLevel;
        }

        public void PushClip(RectangleF[] rects)
        {
            using (SKPath path = new SKPath()) {
                foreach (RectangleF rect in rects) {
                    path.AddRect(GetSKRect(rect), SKPathDirection.Clockwise);
                }

                PushClip(path);
            }
        }

        public void PopClip()
        {
            Debug.Assert(pushLevel > 0);
            --pushLevel;
            canvas.Restore();
        }

        // Push an anti-aliasing mode.
        public void PushAntiAliasing(bool newAntiAlias)
        {
            antiAliasStack.Push(antiAlias);
            antiAlias = newAntiAlias;
        }

        // Pop anti-aliases mode.
        public void PopAntiAliasing()
        {
            antiAlias = antiAliasStack.Pop();
        }

        SKPaint UpdateAntialias(SKPaint paint)
        {
            paint.IsAntialias = antiAlias;
            return paint;
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
            canvas.DrawLine(start.X, start.Y, finish.X, finish.Y, GetPenPaint(penKey));
        }

        // Draw an arc with a pen.
        public void DrawArc(object penKey, PointF center, float radius, float startAngle, float sweepAngle)
        {
            SKRect oval = GetSKRect(Geometry.RectangleFromCenterSize(center, new SizeF(radius * 2, radius * 2)));
            using (SKPath path = new SKPath()) {
                path.AddArc(oval, startAngle, sweepAngle);
                canvas.DrawPath(path, GetPenPaint(penKey));
            }
        }

        // Draw an ellipse with a pen.
        public void DrawEllipse(object penKey, PointF center, float radiusX, float radiusY)
        {
            canvas.DrawOval(GetSKRect(Geometry.RectangleFromCenterSize(center, new SizeF(radiusX * 2, radiusY * 2))), GetPenPaint(penKey));
        }

        // Fill an ellipse with a pen.
        public void FillEllipse(object brushKey, PointF center, float radiusX, float radiusY)
        {
            canvas.DrawOval(GetSKRect(Geometry.RectangleFromCenterSize(center, new SizeF(radiusX * 2, radiusY * 2))), GetBrushPaint(brushKey));
        }

        // Draw a rectangle with a pen.
        public void DrawRectangle(object penKey, RectangleF rect)
        {
            canvas.DrawRect(GetSKRect(rect), GetPenPaint(penKey));
        }

        // Fill a rectangle with a brush.
        public void FillRectangle(object brushKey, RectangleF rect)
        {
            canvas.DrawRect(GetSKRect(rect), GetBrushPaint(brushKey));
        }

        // Fill a polygon with a brush
        public void DrawPolygon(object penKey, PointF[] pts)
        {
            using (SKPath path = new SKPath()) {
                path.MoveTo(pts[0].X, pts[0].Y);
                for (int i = 1; i < pts.Length; ++i)
                    path.LineTo(pts[i].X, pts[i].Y);
                path.Close();
                DrawSKPath(penKey, path);
            }
        }

        // Fill a polygon with a brush
        public void DrawPolyline(object penKey, PointF[] pts)
        {
            using (SKPath path = new SKPath()) {
                path.MoveTo(pts[0].X, pts[0].Y);
                for (int i = 1; i < pts.Length; ++i)
                    path.LineTo(pts[i].X, pts[i].Y);
                DrawSKPath(penKey, path);
            }
        }

        // Fill a polygon with a brush
        public void FillPolygon(object brushKey, PointF[] pts, SysDraw2D.FillMode windingMode)
        {
            using (SKPath path = new SKPath()) {
		        path.FillType = (windingMode == FillMode.Alternate) ? SKPathFillType.EvenOdd : SKPathFillType.Winding;

                path.MoveTo(pts[0].X, pts[0].Y);
                for (int i = 1; i < pts.Length; ++i)
                    path.LineTo(pts[i].X, pts[i].Y);
                path.Close();
                FillSKPath(brushKey, path);
            }
        }

        private void DrawSKPath(object penKey, SKPath path)
        {
            canvas.DrawPath(path, GetPenPaint(penKey));
        }

        // Draw a path with a pen.
        public void DrawPath(object penKey, object pathKey)
        {
            DrawSKPath(penKey, GetSkPath(pathKey));
        }

        public void DrawPath(object penKey, List<GraphicsPathPart> parts)
        {
            DrawSKPath(penKey, GetPath(parts, FillMode.Winding));
        }

        private void FillSKPath(object brushKey, SKPath path)
        {
            canvas.DrawPath(path, GetBrushPaint(brushKey));
        }

        // Fill a path with a brush.
        public void FillPath(object brushKey, object pathKey)
        {
            FillSKPath(brushKey, GetSkPath(pathKey));
        }

        public void FillPath(object brushKey, List<GraphicsPathPart> parts, FillMode windingMode)
        {
            FillSKPath(brushKey, GetPath(parts, windingMode));
        }

        // Draw text with upper-left corner of text at the given locations.
        public void DrawText(string text, object fontKey, object brushKey, PointF upperLeft)
        {
            SkiaFont font = GetFont(fontKey);
            SKPaint brushPaint = GetBrushPaint(brushKey);
            using (SKPaint paint = new SKPaint()) {
                paint.Color = brushPaint.Color;
                paint.Shader = brushPaint.Shader;
                paint.Typeface = font.Typeface;
                paint.TextSize = font.EmHeight;
                paint.TextAlign = SKTextAlign.Left;
                paint.IsAntialias = antiAlias;
                // paint.UnderlineText = font.Underline;  // TODO: Underline not yet supported.
                canvas.DrawText(text, upperLeft.X, upperLeft.Y + font.Ascent, paint);
            }
            float emHeight = font.EmHeight;
        }

        // Draw text outline with upper-left corner of text at the given locations.
        public void DrawTextOutline(string text, object fontKey, object penKey, PointF upperLeft)
        {
            SkiaFont font = GetFont(fontKey);
            SKPaint penPaint = GetPenPaint(penKey);
            
            using (SKPaint paint = new SKPaint()) {
                paint.Typeface = font.Typeface;
                paint.TextSize = font.EmHeight;
                paint.TextAlign = SKTextAlign.Left;
                paint.IsAntialias = antiAlias;
                paint.IsStroke = true;
                paint.Color = penPaint.Color;
                paint.Shader = penPaint.Shader;
                paint.StrokeWidth = penPaint.StrokeWidth;
                paint.StrokeJoin = penPaint.StrokeJoin;
                paint.StrokeCap = penPaint.StrokeCap;
                paint.StrokeMiter = penPaint.StrokeMiter;
                canvas.DrawText(text, upperLeft.X, upperLeft.Y + font.Ascent, paint);
            }
        }

        // Draw a bitmap
        public void DrawBitmap(IGraphicsBitmap bm, RectangleF rectangle, BitmapScaling scalingMode, float minResolution)
        {
            DrawBitmapPart(bm, 0, 0, bm.PixelWidth, bm.PixelHeight, rectangle, scalingMode, minResolution);
        }

        // Draw part of a bitmap
        public void DrawBitmapPart(IGraphicsBitmap bm, int x, int y, int width, int height, RectangleF rectangle, BitmapScaling scalingMode, float minResolution)
        {
            using (SKPaint paint = new SKPaint()) {
                SKFilterQuality filterQuality;
                switch (scalingMode) {
                    default:
                    case BitmapScaling.NearestNeighbor: filterQuality = SKFilterQuality.None; break;
                    case BitmapScaling.MediumQuality: filterQuality = SKFilterQuality.Medium; break;
                    case BitmapScaling.HighQuality: filterQuality = SKFilterQuality.High; break;
                }
                paint.FilterQuality = filterQuality;
                paint.IsAntialias = true;

                if (bm is Skia_Image) {
                    SKImage image = ((Skia_Image)bm).Image;
                    canvas.DrawImage(image, GetSKRect(new RectangleF(x, y, width, height)), GetSKRect(rectangle), paint);
                }
                else if (bm is Skia_Bitmap) {
                    SKBitmap bitmap = ((Skia_Bitmap)bm).Bitmap;
                    canvas.DrawBitmap(bitmap, GetSKRect(new RectangleF(x, y, width, height)), GetSKRect(rectangle), paint);
                }
            }
        }

        public bool HasPath(object pathKey)
        {
            return pathMap.ContainsKey(pathKey);
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

        private SKPaint GetBrushPaint(object brushKey) {
            SKPaint paint;
            if (brushMap.TryGetValue(brushKey, out paint)) {
                Debug.Assert(!paint.IsStroke);
                return UpdateAntialias(paint);
            }
            else {
                Debug.Fail("Given key does not have a brush created for it");
                throw new ArgumentException("Given key does not have a brush created for it", "brushKey");
            }
        }

        private SKPaint GetPenPaint(object penKey)
        {
            SKPaint paint;
            if (penMap.TryGetValue(penKey, out paint)) {
                Debug.Assert(paint.IsStroke);
                return UpdateAntialias(paint);
            }
            else {
                Debug.Fail("Given key does not have a pen created for it");
                throw new ArgumentException("Given key does not have a pen created for it", "penKey");
            }
        }

        private SkiaFont GetFont(object fontKey)
        {
            SkiaFont font;
            if (fontMap.TryGetValue(fontKey, out font)) {
                return font;
            }
            else {
                Debug.Fail("Given key does not have a font created for it");
                throw new ArgumentException("Given key does not have a font created for it", "fontKey");
            }
        }

        private SKPath GetSkPath(object pathKey)
        {
            SKPath path;
            if (pathMap.TryGetValue(pathKey, out path)) {
                return path;
            }
            else {
                Debug.Fail("Given key does not have a path created for it");
                throw new ArgumentException("Given key does not have a path created for it", "pathKey");
            }
        }

        public virtual void Dispose() {
            if (canvas != null) {
                canvas.Dispose();
                canvas = null;
            }

            foreach (SKPaint paint in penMap.Values)
                paint.Dispose();
            penMap.Clear();

            foreach (SKPaint paint in brushMap.Values)
                paint.Dispose();
            brushMap.Clear();

            foreach (SKPath path in pathMap.Values)
                path.Dispose();
            pathMap.Clear();

            foreach (SkiaFont font in fontMap.Values)
                font.Dispose();

            fontMap.Clear();
        }

        internal static SKMatrix GetSkMatrix(Matrix source)
        {
            float[] elements = source.Elements;

            SKMatrix mat = SKMatrix.CreateIdentity();
            mat.ScaleX = elements[0];
            mat.SkewY = elements[1];
            mat.SkewX = elements[2];
            mat.ScaleY = elements[3];
            mat.TransX = elements[4];
            mat.TransY = elements[5];

            return mat;
        }

        private static SKRect GetSKRect(RectangleF rect)
        {
            return new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        private class Skia_BrushTarget: Skia_GraphicsTarget, IBrushTarget
        {
            private Skia_GraphicsTarget owningTarget;
            private SKSurface surface;
            private SKBitmap bitmap;
            private SizeF size;
            private float angle;

            public Skia_BrushTarget(Skia_GraphicsTarget owningTarget, SKCanvas canvas, SKSurface surface, SKBitmap bitmap, SizeF size, float angle)
                : base(canvas)
            {
                this.owningTarget = owningTarget;
                this.surface = surface;
                this.size = size;
                this.angle = angle;
                this.bitmap = bitmap;
                this.colorConverter = owningTarget.colorConverter;
                // TODO: Copy intensity
            }

            public void FinishBrush(object brushKey)
            {
                // Dispose of the canvas.
                canvas.Dispose();
                canvas = null;

                // Dispose of the surface.
                surface.Dispose();
                surface = null;

                if (owningTarget.HasBrush(brushKey))
                    throw new InvalidOperationException("Key already has a brush created for it");

                // Scale and rotate.
                Matrix transform = new Matrix();
                transform.Rotate(angle);
                transform.Scale(size.Width / (float)bitmap.Width, size.Height / (float)bitmap.Height);
                transform.Translate(-bitmap.Width / 2F, -bitmap.Height / 2F);

                // Create an SKShader around this texture.
                using (SKShader shader = SKShader.CreateBitmap(bitmap, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat, GetSkMatrix(transform))) {
                    // Create an SKPaint with that shader.
                    SKPaint paint = new SKPaint();
                    paint.Shader = shader;

                    owningTarget.brushMap.Add(brushKey, paint);
                }
            }
        }

    }

    public class SkiaFont: ITextFaceMetrics
    {
		private SKTypeface typeface;
		private float emHeight;
        private SKFontMetrics fontMetrics;
        private bool fontMetricsObtained;
        private bool underline;
        private float spaceWidth = -1, capHeight = -1;

		public SkiaFont(string familyName, float emHeight, TextEffects effects)
		{
			this.emHeight = emHeight;
            this.typeface = SKTypeface.FromFamilyName(familyName, GetSKFontStyleWeight(effects), SKFontStyleWidth.Normal, GetSKFontStyleSlant(effects));
            this.underline = ((effects & TextEffects.Underline) != 0);
		}

		public SKTypeface Typeface
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

        public float RecommendedLineSpacing
        {
            get
            {
                LoadFontMetrics();
                return (-fontMetrics.Ascent + fontMetrics.Descent + fontMetrics.Leading);
            }
        }

        public float Ascent
        {
            get
            {
                LoadFontMetrics();
                return - fontMetrics.Ascent;
            }
        }

        public float Descent
        {
            get
            {
                LoadFontMetrics();
                return fontMetrics.Descent;
            }
        }

        public float CapHeight
        {
            get
            {
                if (capHeight < 0) {
                    using (SKPaint paint = new SKPaint()) {
                        paint.IsAntialias = true;
                        paint.Typeface = typeface;
                        paint.TextSize = emHeight * 100;
                        using (SKPath path = paint.GetTextPath("W", 0, 0)) {
                            SKRect rect = path.TightBounds;
                            capHeight = rect.Height / 100F;
                        }
                    }
                }

                return capHeight;
            }
        }

        public float SpaceWidth
        {
            get
            {
                if (spaceWidth < 0) {
                    spaceWidth = GetTextWidth(" ");
                }

                return spaceWidth;
            }
        }

        public void Dispose()
		{
            if (typeface != null) {
                typeface.Dispose();
                typeface = null; 
            }
		}

        public static SKFontStyleWeight GetSKFontStyleWeight(TextEffects effects)
        {
            bool bold = (effects & TextEffects.Bold) != 0;

            if (bold) {
                return SKFontStyleWeight.Bold;
            }
            else {
                return SKFontStyleWeight.Normal;
            }
        }

        public static SKFontStyleSlant GetSKFontStyleSlant(TextEffects effects)
        {
            bool italic = (effects & TextEffects.Italic) != 0;

            if (italic) {
                return SKFontStyleSlant.Italic;
            }
            else {
                return SKFontStyleSlant.Upright;
            }
        }


        public float GetTextWidth(string text)
        {
            using (SKPaint paint = new SKPaint()) {
                paint.IsAntialias = true;
                paint.Typeface = typeface;
                paint.TextSize = emHeight;
                paint.TextEncoding = SKTextEncoding.Utf16;
                return paint.MeasureText(text);
            }
        }

        public SizeF GetTextSize(string text)
        {
            SKRect rect = new SKRect();

            using (SKPaint paint = new SKPaint()) {
                paint.IsAntialias = true;
                paint.Typeface = typeface;
                paint.TextSize = emHeight * 100;
                paint.TextEncoding = SKTextEncoding.Utf16;
                paint.MeasureText(text, ref rect);
                return new SizeF((rect.Right - rect.Left) / 100F, Math.Max((rect.Bottom - rect.Top) / 100, Ascent + Descent));
            }
        }

        void LoadFontMetrics()
        {
            if (!fontMetricsObtained) {
                using (SKPaint paint = new SKPaint()) {
                    paint.IsAntialias = true;
                    paint.Typeface = typeface;
                    paint.TextSize = emHeight;
                    fontMetrics = paint.FontMetrics;
                }

                fontMetricsObtained = true;
            }
        }
    }

    public class Skia_TextMetrics: ITextMetrics
    {
        public static ITextFaceMetrics GetMetrics(string familyName, float emHeight, TextEffects effects)
        {
            if (!IsTextFaceInstalled(familyName))
                familyName = "Arial";          // Map non-existant fonts to "Arial".

            return new SkiaFont(familyName, emHeight, effects);
        }

        public static bool IsTextFaceInstalled(string familyName)
        {
            // Get the glyphTypeface to see if the font exists.
            SKTypeface typeface = SKTypeface.FromFamilyName(familyName);

            if (typeface == null)
                return false;
            typeface.Dispose();
            typeface = null;
            return true;
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

    // A graphics target that draw onto a bitmap. Also adds support for blending mode Darken.
    public class Skia_BitmapGraphicsTarget: Skia_GraphicsTarget, IBitmapGraphicsTarget
    {
        int width, height;
        SKBitmap bitmap;
        SKSurface surface;

        public Skia_BitmapGraphicsTarget(int pixelWidth, int pixelHeight, bool alpha, CmykColor initialColor, RectangleF rectangle, bool inverted, SkiaColorConverter colorConverter = null, float intensity = 1.0F)
            : this(GetBitmap(pixelWidth, pixelHeight, alpha), initialColor, rectangle, inverted, colorConverter, intensity)
        {
        }

        public Skia_BitmapGraphicsTarget(SKBitmap bitmap, CmykColor initialColor, RectangleF rectangle, bool inverted, SkiaColorConverter colorConverter = null, float intensity = 1.0F)
            : this(bitmap, initialColor, GetTransform(bitmap, rectangle, inverted), colorConverter, intensity)
        {
        }

        public Skia_BitmapGraphicsTarget(SKBitmap bitmap, CmykColor initialColor, Matrix transform, SkiaColorConverter colorConverter = null, float intensity = 1.0F, SKPath clipPath = null)
            : this(GetSurface(bitmap), bitmap, initialColor, transform, colorConverter, intensity, clipPath)
        {
        }

        private Skia_BitmapGraphicsTarget(SKSurface surface, SKBitmap bitmap, CmykColor initialColor, Matrix transform, SkiaColorConverter colorConverter = null, float intensity = 1.0F, SKPath clipPath = null)
            : base(GetCanvas(surface, initialColor, transform, colorConverter, clipPath), colorConverter, intensity)
        {
            this.surface = surface;
            this.bitmap = bitmap;
            this.width = bitmap.Width;
            this.height = bitmap.Height;
        }
        public int PixelWidth { get { return width; } }
        public int PixelHeight { get { return height; } }

        static SKCanvas GetCanvas(SKSurface surface, CmykColor initialColor, Matrix transform, SkiaColorConverter colorConverter, SKPath clipPath)
        {
            SKCanvas canvas = surface.Canvas;

            // ClipPath region is in Bitmap coordinates -- before the transform is applied.
            if (clipPath != null)
                canvas.ClipPath(clipPath);

            SKMatrix matrix = Skia_GraphicsTarget.GetSkMatrix(transform);
            canvas.Concat(ref matrix);

            if (initialColor != null) {
                colorConverter = colorConverter ?? new SkiaColorConverter();
                canvas.Clear(colorConverter.ToColor(initialColor));
            }

            return canvas;
        }

        static Matrix GetTransform(SKBitmap bitmap, RectangleF rectangle, bool inverted)
        {
            SizeF bitmapSize = new SizeF(bitmap.Width, bitmap.Height);
            PointF midpoint = new PointF(bitmapSize.Width / 2.0F, bitmapSize.Height / 2.0F);
            float scaleFactor = (float)bitmapSize.Width / rectangle.Width;
            PointF centerPoint = new PointF((rectangle.Left + rectangle.Right) / 2, (rectangle.Top + rectangle.Bottom) / 2);
            Matrix matrix = new Matrix();
            matrix.Translate(midpoint.X, midpoint.Y);
            matrix.Scale(scaleFactor, inverted ? -scaleFactor : scaleFactor);  // y scale is negative to get to cartesian orientation.
            matrix.Translate(-centerPoint.X, -centerPoint.Y);

            return matrix;
        }

        static SKBitmap GetBitmap(int pixelWidth, int pixelHeight, bool alpha)
        {
            return new SKBitmap(pixelWidth, pixelHeight, !alpha);
        }

        static SKSurface GetSurface(SKBitmap bitmap)
        {
            IntPtr length;
            return SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height, bitmap.ColorType, bitmap.AlphaType), bitmap.GetPixels(out length), bitmap.RowBytes);
        }

        public IGraphicsBitmap FinishBitmap()
        {
            Skia_Bitmap skBitmap = new Skia_Bitmap(bitmap);
            bitmap = null;  // Now owned by the Skia_Bitmap.
            return skBitmap;
        }

        public override void Dispose()
        {
            base.Dispose();

            if (Canvas != null)
                Canvas.Dispose();

            if (surface != null) {
                surface.Dispose();
                surface = null;
            }

            if (bitmap != null) {
                bitmap.Dispose();
                bitmap = null;
            }
        }
    }

    public class Skia_Image: IGraphicsBitmap
    {
        SKImage image;

        public SKImage Image
        {
            get { return image; }
        }

        public int PixelWidth
        {
            get { return image != null ? image.Width : 0; }
        }

        public int PixelHeight
        {
            get { return image != null ? image.Height : 0; }
        }

        public void Dispose()
        {
            lock (this) {
                if (image != null)
                    image.Dispose();
                image = null;
            }
        }

        public bool Disposed
        {
            get { return image == null; }
        }

        public Skia_Image(SKImage image)
        {
            this.image = image;
        }
    }

    public class Skia_Bitmap: IGraphicsBitmap
    {
        SKBitmap bitmap;

        public SKBitmap Bitmap
        {
            get { return bitmap; }
        }

        public int PixelWidth
        {
            get { return bitmap != null ? bitmap.Width : 0; }
        }

        public int PixelHeight
        {
            get { return bitmap != null ? bitmap.Height : 0; }
        }

        public void Dispose()
        {
            lock (this) {
                if (bitmap != null)
                    bitmap.Dispose();
                bitmap = null;
            }
        }

        public bool Disposed
        {
            get { return bitmap == null; }
        }

        public Skia_Bitmap(SKBitmap bitmap)
        {
            this.bitmap = bitmap;
        }
    }


    public class SkiaColorConverter
    {
        public virtual SKColor ToColor(CmykColor cmykColor)
        {
            SysDraw.Color sysColor = ColorConverter.ToColor(cmykColor);
            return new SKColor(sysColor.R, sysColor.G, sysColor.B, sysColor.A);
        }
    }
}
