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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;



namespace PurplePen.MapModel
{
    using Map_SkiaStd;
    using PurplePen.Graphics2D;
    using SkiaSharp;
    using System.Collections.Concurrent;
    using System.Drawing;
    using System.Drawing.Imaging;

    // A GraphicsTarget encapsulates an SKCanvas
    public class Skia_GraphicsTarget: IGraphicsTarget
    {
        protected SKCanvas canvas;
        private IColorConverter colorConverter;
        private float intensity;    // color intensity level, 1.0F is full intensity (no lightening)
        private int pushLevel;      // How many pushes have we done?
        private Dictionary<object, SKPaint> penMap = new Dictionary<object, SKPaint>(new IdentityComparer<object>());
        private Dictionary<object, SKPaint> brushMap = new Dictionary<object, SKPaint>(new IdentityComparer<object>());
        private Dictionary<object, SkiaFont> fontMap = new Dictionary<object, SkiaFont>(new IdentityComparer<object>());
        private Dictionary<object, SKPath> pathMap = new Dictionary<object, SKPath>(new IdentityComparer<object>());
        private Stack<bool> antiAliasStack = new Stack<bool>();
        private bool antiAlias;

        public Skia_GraphicsTarget(SKCanvas canvas, IColorConverter colorConverter, float intensity = 1.0F)
        {
            this.canvas = canvas;
            pushLevel = 0;
            this.colorConverter = colorConverter ?? new SkiaColorConverter();
            this.antiAlias = false;
            this.intensity = intensity;
        }

        public Skia_GraphicsTarget(SKCanvas canvas) : this(canvas, null)
        {
        }

        public float Intensity {
            get { return intensity; }
            set {
                // Pens and brushes have colors that were based on the intensity, so
                // they must be destroyed.
                foreach (SKPaint paint in penMap.Values)
                    paint.Dispose();
                penMap.Clear();

                foreach (SKPaint paint in brushMap.Values)
                    paint.Dispose();
                brushMap.Clear();

                intensity = value;
            }
        }


        public SKCanvas Canvas
        {
            get { return canvas; }
        }


        private SKColor ConvertColor(CmykColor cmykColor)
        {
            if (intensity < 1.0F) {
                cmykColor = CmykColor.FromCmyka(cmykColor.Cyan * intensity, cmykColor.Magenta * intensity, cmykColor.Yellow * intensity, cmykColor.Black * intensity, cmykColor.Alpha);
            }

            Color sysColor = colorConverter.ToColor(cmykColor);
            return new SKColor(sysColor.R, sysColor.G, sysColor.B, sysColor.A);
        }



        public void CreateSolidBrush(object brushKey, CmykColor color)
        {
            if (brushMap.ContainsKey(brushKey))
                throw new InvalidOperationException("Key already has a brush created for it");

            SKPaint paint = new SKPaint();
            paint.Color = ConvertColor(color);
            paint.IsStroke = false;
            brushMap.Add(brushKey, paint);
        }

        private void CreatePenCore(object penKey, SKPaint basePaint, float width, LineCapMode caps, LineJoinMode join, float miterLimit)
        {
            if (penMap.ContainsKey(penKey))
                throw new InvalidOperationException("Key already has a pen created for it");

            SKPaint paint = basePaint.Clone();
            paint.IsStroke = true;
            paint.StrokeWidth = width;

            switch (caps) {
                case LineCapMode.Flat:
                    paint.StrokeCap = SKStrokeCap.Butt;
                    break;
                case LineCapMode.Round:
                    paint.StrokeCap = SKStrokeCap.Round;
                    break;
                case LineCapMode.Square:
                    paint.StrokeCap = SKStrokeCap.Square;
                    break;
                default:
                    throw new ArgumentException("bad line cap", "caps");
            }

            switch (join) {
                case LineJoinMode.Bevel:
                    paint.StrokeJoin = SKStrokeJoin.Bevel;
                    break;
                case LineJoinMode.Miter:
                    paint.StrokeJoin = SKStrokeJoin.Miter;
                    paint.StrokeMiter = miterLimit;
                    break;
                case LineJoinMode.Round:
                    paint.StrokeJoin = SKStrokeJoin.Round;
                    break;
                default:
                    throw new ArgumentException("bad line join", "join");
            }

            penMap.Add(penKey, paint);
        }

        public void CreatePen(object penKey, CmykColor color, float width, LineCapMode caps, LineJoinMode join, float miterLimit)
        {
            SKPaint paint = new SKPaint();
            paint.Color = ConvertColor(color);
            CreatePenCore(penKey, paint, width, caps, join, miterLimit);
        }

        public void CreatePen(object penKey, object brushKey, float width, LineCapMode caps, LineJoinMode join, float miterLimit)
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

        public void CreatePath(object pathKey, List<GraphicsPathPart> parts, AreaFillMode windingMode)
        {
            if (pathMap.ContainsKey(pathKey))
                throw new InvalidOperationException("Key already has a path created for it");

            SKPath path = GetPath(parts, windingMode);
            pathMap.Add(pathKey, path);
        }

        private SKPath GetPath(List<GraphicsPathPart> parts, AreaFillMode windingMode)
        {
            SKPath path = new SKPath();
            
            path.FillType = (windingMode == AreaFillMode.Alternate) ? SKPathFillType.EvenOdd : SKPathFillType.Winding;

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
            canvas.Concat(mat);
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

        public void PushClip(List<GraphicsPathPart> parts, AreaFillMode windingMode)
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
        public void FillPolygon(object brushKey, PointF[] pts, AreaFillMode windingMode)
        {
            using (SKPath path = new SKPath()) {
		        path.FillType = (windingMode == AreaFillMode.Alternate) ? SKPathFillType.EvenOdd : SKPathFillType.Winding;

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
            DrawSKPath(penKey, GetPath(parts, AreaFillMode.Winding));
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

        public void FillPath(object brushKey, List<GraphicsPathPart> parts, AreaFillMode windingMode)
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
                paint.IsAntialias = antiAlias;

                // paint.UnderlineText = font.Underline;  // TODO: Underline not yet supported.
                font.EnhancedTypeface.DrawText(canvas, text, new SKPoint(upperLeft.X, upperLeft.Y), font.EmHeight, paint);
            }
        }

        // Draw text outline with upper-left corner of text at the given locations.
        public void DrawTextOutline(string text, object fontKey, object penKey, PointF upperLeft)
        {
            SkiaFont font = GetFont(fontKey);
            SKPaint penPaint = GetPenPaint(penKey);
            
            using (SKPaint paint = new SKPaint()) {
                paint.IsAntialias = antiAlias;
                paint.IsStroke = true;
                paint.Color = penPaint.Color;
                paint.Shader = penPaint.Shader;
                paint.StrokeWidth = penPaint.StrokeWidth;
                paint.StrokeJoin = penPaint.StrokeJoin;
                paint.StrokeCap = penPaint.StrokeCap;
                paint.StrokeMiter = penPaint.StrokeMiter;

                font.EnhancedTypeface.DrawText(canvas, text, new SKPoint(upperLeft.X, upperLeft.Y), font.EmHeight, paint);
            }
        }

        // Draw a bitmap
        public void DrawBitmap(IGraphicsBitmap bm, RectangleF rectangle, BitmapScaling scalingMode)
        {
            DrawBitmapPart(bm, 0, 0, bm.PixelWidth, bm.PixelHeight, rectangle, scalingMode);
        }

        // Draw part of a bitmap
        public void DrawBitmapPart(IGraphicsBitmap bm, int x, int y, int width, int height, RectangleF rectangle, BitmapScaling scalingMode)
        {
            using (SKPaint paint = new SKPaint()) {
                SKSamplingOptions samplingOptions;
                switch (scalingMode) {
                    default:
                    case BitmapScaling.NearestNeighbor: samplingOptions = new SKSamplingOptions(SKFilterMode.Nearest); break;
                    case BitmapScaling.MediumQuality: samplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Nearest); break;
                    case BitmapScaling.HighQuality: samplingOptions = new SKSamplingOptions(new SKCubicResampler(1 / 3.0f, 1 / 3.0f)); break;
                }
                paint.IsAntialias = true;


                if (intensity < 1.0F) {
                    paint.ColorFilter = SKColorFilter.CreateLighting((SKColor) new SKColorF(intensity, intensity, intensity), (SKColor) new SKColorF(1.0F - intensity, 1.0F - intensity, 1.0F - intensity));
                }

                if (bm is Skia_Image) {
                    SKImage image = ((Skia_Image)bm).Image;
                    canvas.DrawImage(image, GetSKRect(new RectangleF(x, y, width, height)), GetSKRect(rectangle), samplingOptions, paint);
                }
                else if (bm is Skia_Bitmap) {
                    // Canvas.DrawBitmap doesn't support sampling options, so we have to create an SKImage to draw with sampling options.
                    SKBitmap bitmap = ((Skia_Bitmap)bm).Bitmap;
                    using (SKImage image = SKImage.FromBitmap(bitmap)) {
                        canvas.DrawImage(image, GetSKRect(new RectangleF(x, y, width, height)), GetSKRect(rectangle), samplingOptions, paint);
                    }
                }
                else if (bm is Skia_Pixmap) {
                    using (SKImage image = SKImage.FromPixels(((Skia_Pixmap)bm).Pixmap)) {
                        canvas.DrawImage(image, GetSKRect(new RectangleF(x, y, width, height)), GetSKRect(rectangle), samplingOptions, paint);
                    }
                }
                else {
                    Debug.Fail("Unexpected IGraphicsBitmap implementation");
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
                this.intensity = owningTarget.intensity;
                this.antiAlias = true;
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

                // Create an SKShader around this texture, using high quality sampling.
                SKImage image = SKImage.FromBitmap(bitmap);
                SKShader shader = SKShader.CreateImage(image, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat, new SKSamplingOptions(new SKCubicResampler(1 / 3.0f, 1 / 3.0f)), GetSkMatrix(transform));

                // Create an SKPaint with that shader.
                SKPaint paint = new SKPaint();
                paint.Shader = shader;
                paint.IsAntialias = true;

                owningTarget.brushMap.Add(brushKey, paint);
            }
        }

    }

    public class SkiaFont: ITextFaceMetrics
    {
        //private SKTypeface typeface;
        //private SKShaper shaper;
        private ShapedTypeface shapedTypeface;
        private EnhancedTypeface enhancedTypeface;
		private float emHeight;
        private SKFontMetrics fontMetrics;
        private bool fontMetricsObtained;
        private bool underline;
        private float spaceWidth = -1, capHeight = -1;  

        // These properties mostly duplicates how OCAD renders text.
        private readonly Dictionary<string, int> harfBuzzProperties = new Dictionary<string, int>() {
            { "kern", 1 },  // enable kerning
            { "liga", 0 },  // disable standard ligatures
            { "clig", 0 },  // disable contextual ligatures
            { "dlig", 0 },  // disable discretionary ligatures
            { "hlig", 0 },  // disable optional ligatures
            { "calt", 0 },  // disable contextual alternates
        };

        private readonly string[] fallbackFontsWindows = new string[] {
            "Segoe UI",              // Latin, Cyrillic, Greek, Arabic, Hebrew
            "Tahoma",                // Broad Latin/Arabic backup
            "Nirmala UI",            // Indic scripts (Devanagari, Bengali, Tamil, Telugu, etc.)
            "Leelawadee UI",         // Thai, Lao, Khmer
            "Yu Gothic UI",          // Japanese
            "Microsoft YaHei UI",    // Chinese Simplified
            "Microsoft JhengHei UI", // Chinese Traditional
            "Malgun Gothic",         // Korean
            "Ebrima",                // Ethiopic, N'Ko, Tifinagh, Vai, Osmanya
            "Gadugi",                // Cherokee, Canadian Aboriginal Syllabics
            "Sylfaen",               // Georgian, Armenian
            "Myanmar Text",          // Myanmar
            "Microsoft Himalaya",    // Tibetan
            "Mongolian Baiti",       // Mongolian
            "Segoe UI Symbol",       // Miscellaneous symbols, math, Braille
            "Segoe UI Emoji",        // Emoji
            "Segoe UI Historic",     // Miscellaneous letters, like runic
        };

        private ConcurrentDictionary<TextEffects, ShapedTypeface[]> fallbackTypefaceCache = new ConcurrentDictionary<TextEffects, ShapedTypeface[]>();

        public SkiaFont(string familyName, float emHeight, TextEffects effects)
        {
            ShapedTypeface[] fallbackTypefaces = fallbackTypefaceCache.GetOrAdd(effects, (te) => {
                List<ShapedTypeface> list = new List<ShapedTypeface>();
                foreach (string fallbackFamily in fallbackFontsWindows) {
                    ShapedTypeface shapedTypeface = ShapedTypeface.Get(fallbackFamily, GetSKFontStyleWeight(effects), SKFontStyleWidth.Normal, GetSKFontStyleSlant(effects));
                    if (shapedTypeface != null)
                        list.Add(shapedTypeface);
                }
                return list.ToArray();
            });

            this.emHeight = emHeight;
            this.shapedTypeface = ShapedTypeface.Get(familyName, GetSKFontStyleWeight(effects), SKFontStyleWidth.Normal, GetSKFontStyleSlant(effects));
            this.enhancedTypeface = new EnhancedTypeface(this.shapedTypeface, fallbackTypefaces, harfBuzzProperties);
            this.underline = ((effects & TextEffects.Underline) != 0);
        }

        public EnhancedTypeface EnhancedTypeface { 
            get { return enhancedTypeface; } 
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
                    using (SKFont font = new SKFont(shapedTypeface.Typeface, emHeight * 100))
                    using (SKPath path = font.GetTextPath("W")) {
                        SKRect rect = path.TightBounds;
                        capHeight = rect.Height / 100F;
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
            if (shapedTypeface != null) {
                shapedTypeface.Dispose();
                shapedTypeface = null; 
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
            // We need to use the shaper, to take kerning into account.
            float width = enhancedTypeface.MeasureTextAdvanceWidth(text, emHeight * 100);

            return width / 100;
        }

        public SizeF GetTextSize(string text)
        {
            // We need to use the shaper, to take kerning into account.
            float width = enhancedTypeface.MeasureTextAdvanceWidth(text, emHeight * 100);
            SKRect bounds = enhancedTypeface.MeasureTextBounds(text, emHeight * 100);

            return new SizeF(width / 100, Math.Max((bounds.Bottom - bounds.Top) / 100, Ascent + Descent));
        }

        public RectangleF GetTightBoundingBox(PointF startpoint, string text)
        {
            SKPath path = enhancedTypeface.GetTextPath(text, new SKPoint(startpoint.X, startpoint.Y), emHeight);
            SKRect bounds = path.TightBounds;
            return new RectangleF(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
        }

        void LoadFontMetrics()
        {
            if (!fontMetricsObtained) {
                using (SKFont font = new SKFont(shapedTypeface.Typeface, emHeight)) {
                    fontMetrics = font.Metrics;
                }

                fontMetricsObtained = true;
            }
        }
    }

    public class Skia_TextMetrics: ITextMetrics
    {
        public ITextFaceMetrics GetTextFaceMetrics(string familyName, float emHeight, TextEffects effects)
        {
            return new SkiaFont(familyName, emHeight, effects);
        }

        public bool TextFaceIsInstalled(string familyName)
        {
            return SkiaFontManager.FontFamilyIsInstalled(familyName);
        }

        public void Dispose()
        {
        }
    }

    public class SkiaFontLoader : IFontLoader
    {
        public static SkiaFontLoader Instance { get { return instance; } }
        private static SkiaFontLoader instance = new SkiaFontLoader();

        public void AddFontFile(string familyName, TextEffects textEffects, string fontFilePath)
        {
            SKFontStyleWeight weight = SkiaFont.GetSKFontStyleWeight(textEffects);
            SKFontStyleSlant slant = SkiaFont.GetSKFontStyleSlant(textEffects);
            SKFontStyleWidth width = SKFontStyleWidth.Normal;
            SkiaFontManager.AddFontFile(familyName, weight, width, slant, fontFilePath);
        }

        public bool FontFamilyIsInstalled(string familyName)
        {
            return SkiaFontManager.FontFamilyIsInstalled(familyName);
        }

        // Returns an array of all available font family names, combining both
        // private registered fonts and system fonts. Delegates to SkiaFontManager.
        public string[] GetFontFamilies()
        {
            return SkiaFontManager.GetFontFamilies();
        }
    }

    // A graphics target that draw onto a bitmap. Also adds support for blending mode Darken.
    public class Skia_BitmapGraphicsTarget: Skia_GraphicsTarget, IBitmapGraphicsTarget
    {
        int width, height;
        SKBitmap bitmap;
        SKSurface surface;

        Stack<SKBitmap> bitmapStack = new Stack<SKBitmap>();
        Stack<SKSurface> surfaceStack = new Stack<SKSurface>();
        Stack<SKCanvas> canvasStack = new Stack<SKCanvas>();
        Stack<BlendMode> blendStack = new Stack<BlendMode>();

        public Skia_BitmapGraphicsTarget(int pixelWidth, int pixelHeight, bool alpha, CmykColor initialColor, RectangleF rectangle, bool inverted, IColorConverter colorConverter = null, float intensity = 1.0F)
            : this(GetBitmap(pixelWidth, pixelHeight, alpha), initialColor, rectangle, inverted, colorConverter, intensity)
        {
        }

        public Skia_BitmapGraphicsTarget(SKBitmap bitmap, CmykColor initialColor, RectangleF rectangle, bool inverted, IColorConverter colorConverter = null, float intensity = 1.0F)
            : this(bitmap, initialColor, GetTransform(bitmap, rectangle, inverted), colorConverter, intensity)
        {
        }

        public Skia_BitmapGraphicsTarget(SKBitmap bitmap, CmykColor initialColor, Matrix transform, IColorConverter colorConverter = null, float intensity = 1.0F, SKPath clipPath = null)
            : this(GetSurface(bitmap), bitmap, initialColor, transform, colorConverter, intensity, clipPath)
        {
        }

        private Skia_BitmapGraphicsTarget(SKSurface surface, SKBitmap bitmap, CmykColor initialColor, Matrix transform, IColorConverter colorConverter = null, float intensity = 1.0F, SKPath clipPath = null)
            : base(GetCanvas(surface, initialColor, transform, colorConverter, clipPath), colorConverter, intensity)
        {
            this.surface = surface;
            this.bitmap = bitmap;
            this.width = bitmap.Width;
            this.height = bitmap.Height;
        }
        public int PixelWidth { get { return width; } }
        public int PixelHeight { get { return height; } }

        static SKCanvas GetCanvas(SKSurface surface, CmykColor initialColor, Matrix transform, IColorConverter colorConverter, SKPath clipPath)
        {
            SKCanvas canvas = surface.Canvas;

            // ClipPath region is in Bitmap coordinates -- before the transform is applied.
            if (clipPath != null)
                canvas.ClipPath(clipPath);

            SKMatrix matrix = Skia_GraphicsTarget.GetSkMatrix(transform);
            canvas.Concat(matrix);

            if (initialColor != null) {
                colorConverter = colorConverter ?? new SkiaColorConverter();
                Color sysColor = colorConverter.ToColor(initialColor);
                canvas.Clear(new SKColor(sysColor.R, sysColor.G, sysColor.B, sysColor.A));
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

        // Push a blending mode. For Darken mode, creates a new offscreen bitmap with a white background
        // to draw onto. When PopBlending is called, the offscreen content is composited back using
        // Skia's native Darken blend mode (minimum of each R, G, B component).
        public override bool PushBlending(BlendMode blendMode)
        {
            blendStack.Push(blendMode);

            if (blendMode == BlendMode.Darken) {
                // Save current canvas and surface.
                canvasStack.Push(this.canvas);
                surfaceStack.Push(this.surface);
                bitmapStack.Push(this.bitmap);

                // Create new bitmap to hold the content to blend.
                SKBitmap currentBitmap = this.bitmap;
                SKBitmap newBitmap = new SKBitmap(currentBitmap.Width, currentBitmap.Height, currentBitmap.ColorType, currentBitmap.AlphaType);
                SKSurface newSurface = GetSurface(newBitmap);
                SKCanvas newCanvas = newSurface.Canvas;

                // Copy the transform and clip from the current canvas.
                newCanvas.SetMatrix(this.canvas.TotalMatrix);

                // Clear to white -- darken blend takes the minimum of each component,
                // so white (255,255,255) acts as the identity for undrawn areas.
                newCanvas.Clear(SKColors.White);

                this.canvas = newCanvas;
                this.surface = newSurface;
                this.bitmap = newBitmap;

                return true;
            }
            else {
                return false;
            }
        }

        // Pop the blending mode. For Darken mode, composites the offscreen bitmap back onto the
        // underlying bitmap using Skia's native SKBlendMode.Darken.
        public override void PopBlending()
        {
            BlendMode blendMode = blendStack.Pop();

            if (blendMode == BlendMode.Darken) {
                // Get the bitmap we drew on and flush its surface.
                SKBitmap blendFrom = this.bitmap;
                SKSurface blendSurface = this.surface;
                SKCanvas blendCanvas = this.canvas;
                blendSurface.Flush();

                // Restore the previous canvas, surface, and bitmap.
                this.canvas = canvasStack.Pop();
                this.surface = surfaceStack.Pop();
                this.bitmap = bitmapStack.Pop();

                // Draw the offscreen bitmap onto the destination using Darken blend mode.
                // Skia's Darken blend computes: result = min(src, dst) per component.
                using (SKPaint blendPaint = new SKPaint()) {
                    blendPaint.BlendMode = SKBlendMode.Darken;

                    // Draw in bitmap coordinates (identity transform), since both bitmaps have the same dimensions.
                    this.canvas.Save();
                    this.canvas.SetMatrix(SKMatrix.Identity);
                    this.canvas.DrawBitmap(blendFrom, 0, 0, blendPaint);
                    this.canvas.Restore();
                }

                // Dispose the offscreen resources.
                blendCanvas.Dispose();
                blendSurface.Dispose();
                blendFrom.Dispose();
            }
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
        GraphicsBitmapFormat originalFormat = GraphicsBitmapFormat.None;
        double horizontalResolution = 96;
        double verticalResolution = 96;


        public SKImage Image
        {
            get { return image; }
        }

        /// <summary>Horizontal resolution in dots per inch.</summary>
        public double HorizontalResolution
        {
            get { return horizontalResolution; }
            set { horizontalResolution = value; }
        }

        /// <summary>Vertical resolution in dots per inch.</summary>
        public double VerticalResolution
        {
            get { return verticalResolution; }
            set { verticalResolution = value; }
        }

        public int PixelWidth
        {
            get { return image != null ? image.Width : 0; }
        }

        public int PixelHeight
        {
            get { return image != null ? image.Height : 0; }
        }

        public bool MustCopyBitsForGraphicsTarget => true;


        public GraphicsBitmapFormat GetOriginalFormat()
        {
            return originalFormat;
        }

        public Color GetPixel(int x, int y)
        {
            SKPixmap pixmap = image.PeekPixels();
            SKColor color;

            if (pixmap != null) {

                color = pixmap.GetPixelColor(x, y);
            }
            else {
                // Can't get a pixmap from the image.
                // Crop the image to a single pixel to reduce copying, then create a 
                // bitmap if needed.
                using (SKImage crop = image.Subset(new SKRectI(x, y, x + 1, y + 1))) {
                    pixmap = crop.PeekPixels();
                    if (pixmap != null) {
                        color = pixmap.GetPixelColor(0, 0);
                    }
                    else {
                        using (SKBitmap bitmap = SKBitmap.FromImage(crop)) {
                            color = bitmap.GetPixel(0, 0);
                        }
                    }
                }
            }

            if (pixmap != null)
                pixmap.Dispose();

            return Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
        }



        public IGraphicsBitmap Crop(int x, int y, int width, int height)
        {
            SKRectI cropRect = new SKRectI(x, y, x + width, y + height);
            SKPixmap pixmap = image.PeekPixels();

            if (pixmap != null) {
                SKPixmap subsetPixmap = pixmap.ExtractSubset(cropRect);
                return new Skia_Pixmap(subsetPixmap, horizontalResolution, verticalResolution);
            }
            else {
                SKImage croppedImage = image.Subset(cropRect);
                return new Skia_Image(croppedImage, horizontalResolution, verticalResolution);
            }
        }

        public bool WriteToStream(GraphicsBitmapFormat format, Stream stream)
        {
            SKPixmap pixmap = image.PeekPixels();
            if (pixmap != null) {
                // PeekPixels available: write directly from the pixmap.
                PixmapWithResolution pwr = new PixmapWithResolution(pixmap, format, horizontalResolution, verticalResolution);
                BitmapIO.WritePixmapToStream(pwr, stream, 100);
                pixmap.Dispose();
                return true;
            }
            else {
                // PeekPixels not available (e.g. GPU-backed image): convert to bitmap first.
                using (SKBitmap bmp = SKBitmap.FromImage(image)) {
                    BitmapWithResolution bwr = new BitmapWithResolution(bmp, format, horizontalResolution, verticalResolution);
                    BitmapIO.WriteBitmapToStream(bwr, stream, 100);
                    return true;
                }
            }
        }

        public IBitmapGraphicsTarget GetGraphicsTarget(bool copyBits, IColorConverter colorConverter = null)
        {
            if (!copyBits) {
                throw new ArgumentException("Pixmap must be copied for graphics target", "copyBits");
            }

            SKBitmap newBitmap = SKBitmap.FromImage(image);

            // Return the new Skia_Bitmap that wraps it.
            Skia_Bitmap skia_bitmap = new Skia_Bitmap(newBitmap, originalFormat, horizontalResolution, verticalResolution);
            return new Skia_BitmapGraphicsTarget(newBitmap, null, new RectangleF(0, 0, newBitmap.Width, newBitmap.Height), false, colorConverter);
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

        public Skia_Image(SKImage image, double horizontalResolution, double verticalResolution)
        {
            this.image = image;
            this.horizontalResolution = horizontalResolution;
            this.verticalResolution = verticalResolution;
        }
    }

    public class Skia_Bitmap: IGraphicsBitmap
    {
        SKBitmap bitmap;
        GraphicsBitmapFormat originalFormat = GraphicsBitmapFormat.None;
        double horizontalResolution = 96;
        double verticalResolution = 96;

        public SKBitmap Bitmap
        {
            get { return bitmap; }
        }

        /// <summary>Horizontal resolution in dots per inch.</summary>
        public double HorizontalResolution
        {
            get { return horizontalResolution; }
            set { horizontalResolution = value; }
        }

        /// <summary>Vertical resolution in dots per inch.</summary>
        public double VerticalResolution
        {
            get { return verticalResolution; }
            set { verticalResolution = value; }
        }

        public int PixelWidth
        {
            get { return bitmap != null ? bitmap.Width : 0; }
        }

        public int PixelHeight
        {
            get { return bitmap != null ? bitmap.Height : 0; }
        }

        public bool MustCopyBitsForGraphicsTarget => false;

        public GraphicsBitmapFormat GetOriginalFormat()
        {
            return originalFormat;
        }

        public Color GetPixel(int x, int y)
        {
            SKColor color = bitmap.GetPixel(x, y);
            return Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
        }


        public IGraphicsBitmap Crop(int x, int y, int width, int height)
        {
            SKRectI cropRect = new SKRectI(x, y, x + width, y + height);
            SKPixmap pixmap = bitmap.PeekPixels();
            SKPixmap subsetPixmap = pixmap.ExtractSubset(cropRect);
            return new Skia_Pixmap(subsetPixmap, horizontalResolution, verticalResolution);
        }


        public bool WriteToStream(GraphicsBitmapFormat format, Stream stream)
        {
            BitmapWithResolution bwr = new BitmapWithResolution(bitmap, format, horizontalResolution, verticalResolution);
            BitmapIO.WriteBitmapToStream(bwr, stream, 100);
            return true;
        }

        public IBitmapGraphicsTarget GetGraphicsTarget(bool copyBits, IColorConverter colorConverter = null)
        {
            SKBitmap newBitmap;
            if (copyBits) {
                // Create a new bitmap to draw on, and copy the pixmap content to it.
                newBitmap = new SKBitmap(bitmap.Info);
                SKPixmap pixmap = bitmap.PeekPixels();
                pixmap.ReadPixels(newBitmap.Info, newBitmap.GetPixels(), newBitmap.RowBytes, 0, 0);
            }
            else {
                newBitmap = bitmap;
            }

            // Return the new Skia_Bitmap that wraps it.
            Skia_Bitmap skia_bitmap = new Skia_Bitmap(bitmap, originalFormat, horizontalResolution, verticalResolution);
            return new Skia_BitmapGraphicsTarget(bitmap, null, new RectangleF(0, 0, bitmap.Width, bitmap.Height), false, colorConverter);
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

        internal static SKEncodedImageFormat? ImageFormatFromGraphicsBitmapFormat(GraphicsBitmapFormat format)
        {
            switch (format) {
            case GraphicsBitmapFormat.GIF:
                return SKEncodedImageFormat.Gif;
            case GraphicsBitmapFormat.PNG:
                return SKEncodedImageFormat.Png;
            case GraphicsBitmapFormat.JPEG:
                return SKEncodedImageFormat.Jpeg;
            case GraphicsBitmapFormat.WebP:
                return SKEncodedImageFormat.Webp;
            case GraphicsBitmapFormat.BMP:
                return SKEncodedImageFormat.Bmp;
            }

            return null;
        }

        internal static GraphicsBitmapFormat GraphicsBitmapFormatFromImageFormat(SKEncodedImageFormat skFormat)
        {
            switch (skFormat) {
            case SKEncodedImageFormat.Gif:
                return GraphicsBitmapFormat.GIF;
            case SKEncodedImageFormat.Png:
                return GraphicsBitmapFormat.PNG;
            case SKEncodedImageFormat.Jpeg:
                return GraphicsBitmapFormat.JPEG;
            case SKEncodedImageFormat.Webp:
                return GraphicsBitmapFormat.WebP;
            case SKEncodedImageFormat.Bmp:
                return GraphicsBitmapFormat.BMP;
            }

            return GraphicsBitmapFormat.Other;
        }


        public Skia_Bitmap(SKBitmap bitmap)
        {
            this.bitmap = bitmap;
            this.originalFormat = GraphicsBitmapFormat.Unknown;
        }

        public Skia_Bitmap(SKBitmap bitmap, GraphicsBitmapFormat originalFormat)
        {
            this.bitmap = bitmap;
            this.originalFormat = originalFormat;
        }

        public Skia_Bitmap(SKBitmap bitmap, GraphicsBitmapFormat originalFormat, double horizontalResolution, double verticalResolution)
        {
            this.bitmap = bitmap;
            this.originalFormat = originalFormat;
            this.horizontalResolution = horizontalResolution;
            this.verticalResolution = verticalResolution;
        }
    }

    public class Skia_Pixmap : IGraphicsBitmap
    {
        SKPixmap pixmap;
        GraphicsBitmapFormat originalFormat = GraphicsBitmapFormat.None;
        double horizontalResolution = 96;
        double verticalResolution = 96;


        public SKPixmap Pixmap {
            get { return pixmap; }
        }

        /// <summary>Horizontal resolution in dots per inch.</summary>
        public double HorizontalResolution {
            get { return horizontalResolution; }
            set {  horizontalResolution = value; }
        }

        /// <summary>Vertical resolution in dots per inch.</summary>
        public double VerticalResolution {
            get { return verticalResolution; }
            set { verticalResolution = value; }
        }

        public int PixelWidth {
            get { return pixmap != null ? pixmap.Width : 0; }
        }

        public int PixelHeight {
            get { return pixmap != null ? pixmap.Height : 0; }
        }

        public bool MustCopyBitsForGraphicsTarget => true;

        public GraphicsBitmapFormat GetOriginalFormat()
        {
            return originalFormat;
        }

        public Color GetPixel(int x, int y)
        {
            SKColor color = pixmap.GetPixelColor(x, y);
            return Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
        }

        public IGraphicsBitmap Crop(int x, int y, int width, int height)
        {
            SKRectI cropRect = new SKRectI(x, y, x + width, y + height);
            SKPixmap subsetPixmap = pixmap.ExtractSubset(cropRect);
            return new Skia_Pixmap(subsetPixmap, horizontalResolution, verticalResolution);
        }


        public bool WriteToStream(GraphicsBitmapFormat format, Stream stream)
        {
            PixmapWithResolution pwr = new PixmapWithResolution(pixmap, format, horizontalResolution, verticalResolution);
            BitmapIO.WritePixmapToStream(pwr, stream, 100);
            return true;
        }

        public IBitmapGraphicsTarget GetGraphicsTarget(bool copyBits, IColorConverter colorConverter = null)
        {
            if (!copyBits) {
                throw new ArgumentException("Pixmap must be copied for graphics target", "copyBits");
            }

            // Create a new bitmap to draw on, and copy the pixmap content to it.
            SKBitmap bitmap = new SKBitmap(pixmap.Info);
            pixmap.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0);


            // Return the new Skia_Bitmap that wraps it.
            Skia_Bitmap skia_bitmap = new Skia_Bitmap(bitmap, originalFormat, horizontalResolution, verticalResolution);
            return new Skia_BitmapGraphicsTarget(bitmap, null, new RectangleF(0, 0, bitmap.Width, bitmap.Height), false, colorConverter);

        }

        public void Dispose()
        {
            lock (this) {
                if (pixmap != null)
                    pixmap.Dispose();
                pixmap = null;
            }
        }

        public bool Disposed {
            get { return pixmap == null; }
        }

        public Skia_Pixmap(SKPixmap pixmap)
        {
            this.pixmap = pixmap;
        }

        public Skia_Pixmap(SKPixmap pixmap, double horizontalResolution, double verticalResolution)
        {
            this.pixmap = pixmap;
            this.horizontalResolution = horizontalResolution;
            this.verticalResolution = verticalResolution;
        }
    }



    public class SkiaColorConverter: IColorConverter
    {
        public virtual Color ToColor(CmykColor cmykColor)
        {
            return PurplePen.Graphics2D.ColorConverter.ToColor(cmykColor);
        }    
    }

    public class SkiaBitmapGraphicsLoader : IGraphicsBitmapLoader
    {
        public void Dispose()
        {
        }

        public IGraphicsBitmap CreateEmptyBitmap(int width, int height, System.Drawing.Color? color)
        {
            SKBitmap bitmap = new SKBitmap(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

            if (color.HasValue) {
                SKColor skColor = new SKColor(color.Value.R, color.Value.G, color.Value.B, color.Value.A);
                using (SKCanvas canvas = new SKCanvas(bitmap)) {
                    canvas.Clear(skColor);
                }
            }

            return new Skia_Bitmap(bitmap);
        }

        public IGraphicsBitmap ReadBitmapFromStream(Stream stream)
        {
            BitmapWithResolution bwr = BitmapIO.ReadBitmapFromStream(stream);
            return new Skia_Bitmap(bwr.Bitmap, bwr.Format, bwr.HorizontalResolution, bwr.VerticalResolution);
        }
    }

    public class SkiaBitmapGraphicsTargetProvider : IBitmapGraphicsTargetProvider
    {
        public IBitmapGraphicsTarget CreateBitmapGraphicsTarget(int width, int height, CmykColor initialColor, IColorConverter colorConverter)
        {
            return new Skia_BitmapGraphicsTarget(width, height, true, initialColor, RectangleF.FromLTRB(0, 0, width, height), false, colorConverter);
        }

        public void Dispose()
        {
        }
    }

    public class SkiaFileLoaderProvider : IFileLoaderProvider
    {
        public IFileLoader GetFileLoaderForDirectory(string path)
        {
            return new Skia_FileLoader(path);
        }
    }

}
