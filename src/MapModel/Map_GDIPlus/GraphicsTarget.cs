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
using System.Drawing;
using System.Drawing.Drawing2D;

using PurplePen.MapModel;

namespace PurplePen.MapModel
{
    using PurplePen.Graphics2D;
    using System.IO;
    using System.Drawing.Imaging;
using System.Runtime.InteropServices;

    // A GraphicsTarget encapsulates either a Graphics (for WinForms) or a DrawingContext (for WPF)
    public class GDIPlus_GraphicsTarget: IGraphicsTarget
    {
        public Graphics Graphics;
        private GDIPlus_ColorConverter colorConverter;
        private float intensity;
        private ImageAttributes imageAttributes;
        private Stack<GraphicsState> stateStack;
        private Stack<SmoothingMode> smoothingModeStack;
        private StringFormat stringFormat;
        private Dictionary<object, Pen> penMap = new Dictionary<object, Pen>(new IdentityComparer<object>());
        private Dictionary<object, Brush> brushMap = new Dictionary<object, Brush>(new IdentityComparer<object>());
        private Dictionary<object, Brush> brushMapNoDispose = new Dictionary<object, Brush>(new IdentityComparer<object>());
        private Dictionary<object, Font> fontMap = new Dictionary<object, Font>(new IdentityComparer<object>());
        private Dictionary<object, GraphicsPath> pathMap = new Dictionary<object, GraphicsPath>(new IdentityComparer<object>());

        // Bitmaps above this size are split when drawing.
        private const int BITMAP_DRAW_LIMIT = 10000000;

        public GDIPlus_GraphicsTarget(Graphics g, GDIPlus_ColorConverter colorConverter, float intensity)
        {
            this.Graphics = g;
            this.colorConverter = colorConverter ?? new GDIPlus_ColorConverter();
            this.intensity = intensity;
            if (intensity < 1.0F) {
                imageAttributes = new ImageAttributes();
                imageAttributes.SetColorMatrix(ComputeColorMatrix(intensity));
            }

            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            stateStack = new Stack<GraphicsState>();
            smoothingModeStack = new Stack<SmoothingMode>();
            stringFormat = new StringFormat(StringFormat.GenericTypographic);
            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.LineAlignment = StringAlignment.Near;
            stringFormat.FormatFlags |= StringFormatFlags.NoClip;
            stringFormat.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
        }

        public GDIPlus_GraphicsTarget(Graphics g, GDIPlus_ColorConverter colorConverter): this(g, colorConverter, 1.0F)
        {
        }

        public GDIPlus_GraphicsTarget(Graphics g) : this(g, null, 1.0F)
        {
        }

        protected void ChangeGraphics(Graphics newGraphics)
        {
            newGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            newGraphics.IntersectClip(Graphics.Clip);
            newGraphics.SmoothingMode = Graphics.SmoothingMode;
            newGraphics.Transform = Graphics.Transform;
            this.Graphics = newGraphics;
        }

        public GDIPlus_ColorConverter ColorConverter
        {
            get { return colorConverter; }
        }

        public float Intensity
        {
            get { return intensity; }
        }

        private Color ConvertColor(CmykColor cmykColor)
        {
            if (intensity < 1.0F) {
                cmykColor = CmykColor.FromCmyka(cmykColor.Cyan * intensity, cmykColor.Magenta * intensity, cmykColor.Yellow * intensity, cmykColor.Black * intensity, cmykColor.Alpha);
            }

            return colorConverter.ToColor(cmykColor);
        }

        public void CreateGdiPlusBrush(object brushKey, System.Drawing.Brush brush, bool disposeBrush) {
            if (HasBrush(brushKey))
                throw new InvalidOperationException("Key already has a brush created for it");

            if (disposeBrush)
                brushMap.Add(brushKey, brush);
            else
                brushMapNoDispose.Add(brushKey, brush);
        }

        public void CreateSolidBrush(object brushKey, CmykColor color)
        {
            if (HasBrush(brushKey))
                throw new InvalidOperationException("Key already has a brush created for it");

            brushMap.Add(brushKey, new SolidBrush(ConvertColor(color)));
        }

        public bool SupportsPatternBrushes
        {
            get { return true; }
        }

        public IBrushTarget CreatePatternBrush(SizeF size, float angle, int bitmapWidth, int bitmapHeight)
        {
            // Create a new bitmap and fill it transparent.
            Bitmap bitmap = new Bitmap(bitmapWidth, bitmapHeight);
            Graphics g = Graphics.FromImage(bitmap);
            g.CompositingMode = CompositingMode.SourceCopy;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.FillRectangle(Brushes.Transparent, 0, 0, bitmap.Width, bitmap.Height);
            g.TranslateTransform((float)bitmapWidth / 2F, (float)bitmapHeight / 2F);
            g.ScaleTransform((float)bitmapWidth / size.Width, (float)bitmapHeight / size.Height);

            return new GDIPlus_BrushTarget(this, g, bitmap, size, angle);
        }

        public void CreatePen(object penKey, object brushKey, float width, LineCap caps, LineJoin join, float miterLimit)
        {
            if (penMap.ContainsKey(penKey))
                throw new InvalidOperationException("Key already has a pen created for it");

            Brush brush = GetBrush(brushKey);
            Pen pen = new Pen(brush, width);
            pen.StartCap = pen.EndCap = caps;
            pen.LineJoin = join;
            pen.MiterLimit = miterLimit;

            penMap.Add(penKey, pen);
        }

        public void CreatePen(object penKey, CmykColor color, float width, LineCap caps, LineJoin join, float miterLimit)
        {
            if (penMap.ContainsKey(penKey))
                throw new InvalidOperationException("Key already has a pen created for it");

            Pen pen = new Pen(ConvertColor(color), width);
            pen.StartCap = pen.EndCap = caps;
            pen.LineJoin = join;
            pen.MiterLimit = miterLimit;

            penMap.Add(penKey, pen);
        }

        // Create font
        public void CreateFont(object fontKey, string familyName, float emHeight, TextEffects effects)
        {
            if (fontMap.ContainsKey(fontKey))
                throw new InvalidOperationException("Key already has a font created for it");

            FontStyle fontStyle = FontStyle.Regular;
            if ((effects & TextEffects.Bold) != 0)
                fontStyle |= FontStyle.Bold;
            if ((effects & TextEffects.Italic) != 0)
                fontStyle |= FontStyle.Italic;
            if ((effects & TextEffects.Underline) != 0)
                fontStyle |= FontStyle.Underline;

            if (!GDIPlus_TextMetrics.FontFamilyIsInstalled(familyName))
                familyName = "Arial";

            emHeight = Math.Max(emHeight, 0.01F);            // 0 size fonts cause exception!
            Font font = GdiplusFontLoader.CreateFont(familyName, emHeight, fontStyle);

            fontMap.Add(fontKey, font);
        }

        public void CreatePath(object pathKey, List<GraphicsPathPart> parts, FillMode windingMode)
        {
            if (pathMap.ContainsKey(pathKey))
                throw new InvalidOperationException("Key already has a path created for it");

            GraphicsPath path = GetGraphicsPath(parts, windingMode);

            pathMap.Add(pathKey, path);
        }

        private GraphicsPath GetGraphicsPath(List<GraphicsPathPart> parts, FillMode windingMode)
        {
            GraphicsPath path = new GraphicsPath(windingMode);
            PointF startPoint = default(PointF);

            foreach (GraphicsPathPart part in parts) {
                switch (part.Kind) {
                    case GraphicsPathPartKind.Start:
                        Debug.Assert(part.Points.Length == 1);
                        startPoint = part.Points[0];
                        path.StartFigure();
                        break;

                    case GraphicsPathPartKind.Lines: {
                            PointF[] newPoints = new PointF[part.Points.Length + 1];
                            newPoints[0] = startPoint;
                            Array.Copy(part.Points, 0, newPoints, 1, part.Points.Length);
                            path.AddLines(newPoints);
                            startPoint = part.Points[part.Points.Length - 1];
                            break;
                        }

                    case GraphicsPathPartKind.Beziers: {
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

            return path;
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
        public void PushClip(object pathKey)
        {
            stateStack.Push(Graphics.Save());
            using (Region region = new Region(GetGraphicsPath(pathKey)))
                Graphics.IntersectClip(region);
        }

        public void PushClip(List<GraphicsPathPart> parts, FillMode fillMode)
        {
            stateStack.Push(Graphics.Save());

            using (GraphicsPath grPath = GetGraphicsPath(parts, fillMode))
            using (Region region = new Region(grPath)) {
                Graphics.IntersectClip(region);
            }
        }

        public void PushClip(RectangleF rect)
        {
            stateStack.Push(Graphics.Save());

            Graphics.IntersectClip(rect);
        }

        public void PushClip(RectangleF[] rects)
        {
            stateStack.Push(Graphics.Save());

            using (Region region = new Region(rects[0])) {
                for (int i = 1; i < rects.Length; ++i)
                    region.Union(rects[i]);

                Graphics.IntersectClip(region);
            }
        }

        // Pop the clip.
        public void PopClip()
        {
            Graphics.Restore(stateStack.Pop());
        }

        // Push an anti-aliasing mode.
        public void PushAntiAliasing(bool antiAlias)
        {
            smoothingModeStack.Push(Graphics.SmoothingMode);
            Graphics.SmoothingMode = antiAlias? SmoothingMode.AntiAlias : SmoothingMode.HighSpeed;
        }

        // Pop anti-aliases mode.
        public void PopAntiAliasing()
        {
            Graphics.SmoothingMode = smoothingModeStack.Pop();
        }

        // Set blending mode.
        public virtual bool PushBlending(BlendMode blendMode)
        { 
            // Blending not supported.
            return false;  
        }

        public virtual void PopBlending()
        {
            // Blending not supported.
        }


        // Draw an line with a pen.
        public void DrawLine(object penKey, PointF start, PointF finish)
        {
            try {
                Graphics.DrawLine(GetPen(penKey), start, finish);
            }
            catch (Exception) {
                // Do nothing. Very occasionally, GDI+ given an overflow exception or ExternalException or OutOfMemory exception. 
                // Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        // Draw an arc with a pen.
        public void DrawArc(object penKey, PointF center, float radius, float startAngle, float sweepAngle)
        {
            try {
                Graphics.DrawArc(GetPen(penKey), new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2), startAngle, sweepAngle);
            }
            catch (Exception) {
                // Do nothing. Very occasionally, GDI+ given an overflow exception or ExternalException or OutOfMemory exception. 
                // Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        // Draw an ellipse with a pen.
        public void DrawEllipse(object penKey, PointF center, float radiusX, float radiusY)
        {
            try {
                Graphics.DrawEllipse(GetPen(penKey), center.X - radiusX, center.Y - radiusY, 2 * radiusX, 2 * radiusY);
            }
            catch (Exception) {
                // Do nothing. Very occasionally, GDI+ given an overflow exception or ExternalException or OutOfMemory exception. 
                // Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        // Fill an ellipse with a brush.
        public void FillEllipse(object brushKey, PointF center, float radiusX, float radiusY)
        {
            try {
                Graphics.FillEllipse(GetBrush(brushKey), center.X - radiusX, center.Y - radiusY, 2 * radiusX, 2 * radiusY);
            }
            catch (Exception) {
                // Do nothing. Very occasionally, GDI+ given an overflow exception or ExternalException or OutOfMemory exception. 
                // Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        // Draw a rectangle with a pen.
        public void DrawRectangle(object penKey, RectangleF rect)
        {
            try {
                Graphics.DrawRectangle(GetPen(penKey), rect.X, rect.Y, rect.Width, rect.Height);
            }
            catch (Exception) {
                // Do nothing. Very occasionally, GDI+ given an overflow exception or ExternalException or OutOfMemory exception. 
                // Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        // Fill a rectangle with a brush.
        public void FillRectangle(object brushKey, RectangleF rect)
        {
            try {
                Graphics.FillRectangle(GetBrush(brushKey), rect.X, rect.Y, rect.Width, rect.Height);
            }
            catch (Exception) {
                // Do nothing. Very occasionally, GDI+ given an overflow exception or ExternalException or OutOfMemory exception. 
                // Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        // Draw a polygon with a brush
        public void DrawPolygon(object penKey, PointF[] pts)
        {
            try
            {
                Graphics.DrawPolygon(GetPen(penKey), pts);
            }
            catch (Exception) {
                // Do nothing. Very occasionally, GDI+ given an overflow exception or ExternalException or OutOfMemory exception. 
                // Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        // Draw lines with a brush
        public void DrawPolyline(object penKey, PointF[] pts)
        {
            try
            {
                Graphics.DrawLines(GetPen(penKey), pts);
            }
            catch (Exception) {
                // Do nothing. Very occasionally, GDI+ given an overflow exception or ExternalException or OutOfMemory exception. 
                // Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        // Fill a polygon with a brush
        public void FillPolygon(object brushKey, PointF[] pts, FillMode windingMode)
        {
            try {
                Graphics.FillPolygon(GetBrush(brushKey), pts, windingMode);
            }
            catch (Exception) {
                // Do nothing. Very occasionally, GDI+ given an overflow exception or ExternalException or OutOfMemory exception. 
                // Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        // Draw a path with a pen.
        public void DrawPath(object penKey, object pathKey)
        {
            try
            {
                Graphics.DrawPath(GetPen(penKey), GetGraphicsPath(pathKey));
            }
            catch (Exception) {
                // Do nothing. Very occasionally, GDI+ given an overflow exception or ExternalException or OutOfMemory exception. 
                // Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        public void DrawPath(object penKey, List<GraphicsPathPart> parts)
        {
            try {
                using (GraphicsPath grPath = GetGraphicsPath(parts, FillMode.Alternate)) {
                    Graphics.DrawPath(GetPen(penKey), grPath);
                }
            }
            catch (Exception) {
                // Do nothing. Very occasionally, GDI+ given an overflow exception or ExternalException or OutOfMemory exception. 
                // Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        // Fill a path with a brush.
        public void FillPath(object brushKey, object pathKey)
        {
            try {
                Graphics.FillPath(GetBrush(brushKey), GetGraphicsPath(pathKey));
            }
            catch (Exception) {
                // Do nothing. Very occasionally, GDI+ given an overflow exception or ExternalException or OutOfMemory exception. 
                // Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        public void FillPath(object brushKey, List<GraphicsPathPart> parts, FillMode fillMode)
        {
            try {
                using (GraphicsPath grPath = GetGraphicsPath(parts, fillMode)) {
                    Graphics.FillPath(GetBrush(brushKey), grPath);
                }
            }
            catch (Exception) {
                // Do nothing. Very occasionally, GDI+ given an overflow exception or ExternalException or OutOfMemory exception. 
                // Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        // Draw text with upper-left corner of text at the given locations.
        public void DrawText(string text, object fontKey, object brushKey, PointF upperLeft)
        {
            // Occasional GDI+ throws an exception if the font size is super small.
            try {
                Graphics.DrawString(text, GetFont(fontKey), GetBrush(brushKey), upperLeft, stringFormat);
            }
            catch (Exception) {
                // Do nothing. Very occasionally, GDI+ given an overflow exception or ExternalException or OutOfMemory exception. 
                // Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        // Draw text outline with upper-left corner of text at the given locations.
        public void DrawTextOutline(string text, object fontKey, object penKey, PointF upperLeft)
        {
            Font gdiFont = GetFont(fontKey);
            GraphicsPath grPath = new GraphicsPath(FillMode.Winding);
            Debug.Assert(gdiFont.Unit == GraphicsUnit.World);

            grPath.AddString(text, gdiFont.FontFamily, (int)gdiFont.Style, gdiFont.Size, upperLeft, stringFormat);
            try {
                Graphics.DrawPath(GetPen(penKey), grPath);
            }
            catch (Exception) {
                // Do nothing. Very occasionally, GDI+ given an overflow exception or ExternalException or OutOfMemory exception. 
                // Just ignore it; there's nothing else to do. See bug #1997301.
            }
            grPath.Dispose();
        }

        // Draw a bitmap
        public void DrawBitmap(IGraphicsBitmap bm, RectangleF rectangle, BitmapScaling scalingMode, float minResolution)
        {
            // If the transformed rectangle is too large, we can run out of memory.
            RectangleF transformedBounds = Geometry.BoundsOfTransformedRectangle(rectangle, Graphics.Transform);
            if (Math.Abs(transformedBounds.Width * transformedBounds.Height) > 35000000)
                scalingMode = BitmapScaling.NearestNeighbor;

            if (bm.PixelHeight * bm.PixelWidth > BITMAP_DRAW_LIMIT) {
                // Very large bitmaps can't be drawn in one piece.
                DrawBitmapPartSplit(bm, 0, 0, bm.PixelWidth, bm.PixelHeight, rectangle, scalingMode, minResolution);
                return;
            }

            Bitmap gdiBitmap = ((GDIPlus_Bitmap)bm).Bitmap;
            InterpolationMode oldMode = Graphics.InterpolationMode;

            Graphics.InterpolationMode = GetInterpolationMode(scalingMode);
            try {
                if (imageAttributes != null) {
                    Graphics.DrawImage(gdiBitmap,
                        new PointF[3] { new PointF(rectangle.Left, rectangle.Top), new PointF(rectangle.Right, rectangle.Top), new PointF(rectangle.Left, rectangle.Bottom) },
                        new RectangleF(0, 0, gdiBitmap.Width, gdiBitmap.Height),
                        GraphicsUnit.Pixel, imageAttributes);
                }
                else {
                    Graphics.DrawImage(gdiBitmap, rectangle);
                }
            }
            catch (Exception) {
                // Do nothing. Very occasionally, GDI+ given an overflow exception or ExternalException or OutOfMemory exception. 
                // Just ignore it; there's nothing else to do. See bug #1997301.
            }

            Graphics.InterpolationMode = oldMode;
        }

        // Draw part of a bitmap
        public void DrawBitmapPart(IGraphicsBitmap bm, int x, int y, int width, int height, RectangleF rectangle, BitmapScaling scalingMode, float minResolution)
        {
            // If the transformed rectangle is too large, we can run out of memory.
            RectangleF transformedBounds = Geometry.BoundsOfTransformedRectangle(rectangle, Graphics.Transform);
            if (Math.Abs(transformedBounds.Width * transformedBounds.Height) > 35000000)
                scalingMode = BitmapScaling.NearestNeighbor;

            if (width * height > BITMAP_DRAW_LIMIT) {
                // Very large bitmaps can't be drawn in one piece.
                DrawBitmapPartSplit(bm, x, y, width, height, rectangle, scalingMode, minResolution);
                return;
            }

            GDIPlus_Bitmap gdiBitmap = (GDIPlus_Bitmap)bm;
            InterpolationMode oldMode = Graphics.InterpolationMode;

            Graphics.InterpolationMode = GetInterpolationMode(scalingMode);
            try {
                if (imageAttributes != null) {
                    Graphics.DrawImage(gdiBitmap.Bitmap,
                        new PointF[3] { new PointF(rectangle.Left, rectangle.Top), new PointF(rectangle.Right, rectangle.Top), new PointF(rectangle.Left, rectangle.Bottom) },
                        new RectangleF(x, y, width, height),
                        GraphicsUnit.Pixel, imageAttributes);
                }
                else {
                    Graphics.DrawImage(gdiBitmap.Bitmap, rectangle, new RectangleF(x, y, width, height), GraphicsUnit.Pixel);
                }
            }
            catch (Exception) {
                // Do nothing. Very occasionally, GDI+ given an overflow exception or ExternalException or OutOfMemory exception. 
                // Just ignore it; there's nothing else to do. See bug #1997301.
            }

            Graphics.InterpolationMode = oldMode;
        }

        private void DrawBitmapPartSplit(IGraphicsBitmap bm, int x, int y, int width, int height, RectangleF rectangle, BitmapScaling scalingMode, float minResolution)
        {
            int xSrcSplit = x + width / 2, ySrcSplit = y + height / 2;

            float xDestSplit = rectangle.X + rectangle.Width * (xSrcSplit - x) / width;
            float yDestSplit = rectangle.Y + rectangle.Height * (ySrcSplit - y) / height;

            DrawBitmapPart(bm, x, y, xSrcSplit - x, ySrcSplit - y,                                  RectangleF.FromLTRB(rectangle.X, rectangle.Y, xDestSplit, yDestSplit), scalingMode, minResolution);
            DrawBitmapPart(bm, xSrcSplit, y, x + width - xSrcSplit, ySrcSplit - y,                  RectangleF.FromLTRB(xDestSplit, rectangle.Y, rectangle.Right, yDestSplit), scalingMode, minResolution);
            DrawBitmapPart(bm, x, ySrcSplit, xSrcSplit - x, y + height - ySrcSplit,                 RectangleF.FromLTRB(rectangle.X, yDestSplit, xDestSplit, rectangle.Bottom), scalingMode, minResolution);
            DrawBitmapPart(bm, xSrcSplit, ySrcSplit, x + width - xSrcSplit, y + height - ySrcSplit, RectangleF.FromLTRB(xDestSplit, yDestSplit, rectangle.Right, rectangle.Bottom), scalingMode, minResolution);
        }

        private InterpolationMode GetInterpolationMode(BitmapScaling scalingMode)
        {
            switch (scalingMode)
            {
                case BitmapScaling.NearestNeighbor:
                    return InterpolationMode.NearestNeighbor;
                case BitmapScaling.MediumQuality:
                    return InterpolationMode.HighQualityBilinear;
                case BitmapScaling.HighQuality:
                    return InterpolationMode.HighQualityBicubic;
                default:
                    return InterpolationMode.HighQualityBilinear;
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
            return brushMap.ContainsKey(brushKey) || brushMapNoDispose.ContainsKey(brushKey);
        }

        public bool HasFont(object fontKey) {
            return fontMap.ContainsKey(fontKey);
        }

        private Brush GetBrush(object brushKey) {
            Brush brush;
            if (brushMap.TryGetValue(brushKey, out brush))
                return brush;
            else if (brushMapNoDispose.TryGetValue(brushKey, out brush))
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

        private Font GetFont(object fontKey) {
            Font font;
            if (fontMap.TryGetValue(fontKey, out font))
                return font;
            else
                throw new ArgumentException("Given key does not have a font created for it", "fontKey");
        }

        private GraphicsPath GetGraphicsPath(object pathKey) {
            GraphicsPath path;
            if (pathMap.TryGetValue(pathKey, out path))
                return path;
            else
                throw new ArgumentException("Given key does not have a path created for it", "pathKey");
        }

        public virtual void Dispose()
        {
            foreach (Pen pen in penMap.Values)
                pen.Dispose();
            penMap.Clear();

            foreach (Brush brush in brushMap.Values)
                brush.Dispose();
            brushMap.Clear();

            brushMapNoDispose.Clear();  


            foreach (GraphicsPath path in pathMap.Values)
                path.Dispose();
            pathMap.Clear();

            foreach (Font font in fontMap.Values)
                font.Dispose();
            fontMap.Clear();
        }

        private static ColorMatrix ComputeColorMatrix(float intensity)
        {
            float[][] colorMatrixElements = { 
                        new float[] {(float)intensity,  0,  0,  0, 0},
                        new float[] {0,  (float)intensity,  0,  0, 0},
                        new float[] {0,  0,  (float)intensity,  0, 0},
                        new float[] {0,  0,  0,  1, 0},
                        new float[] {(float) (1-intensity), (float) (1-intensity), (float) (1-intensity), 0, 1}};
            ColorMatrix colorMatrix = new ColorMatrix(colorMatrixElements);
            return colorMatrix;
        }


        private class GDIPlus_BrushTarget : GDIPlus_GraphicsTarget, IBrushTarget
        {
            private GDIPlus_GraphicsTarget owningTarget;
            private Bitmap bitmap;
            private SizeF size;
            private float angle;

            public GDIPlus_BrushTarget(GDIPlus_GraphicsTarget owningTarget, Graphics g, Bitmap bitmap, SizeF size, float angle)
                : base(g) {
                this.owningTarget = owningTarget;
                this.bitmap = bitmap;
                this.size = size;
                this.angle = angle;
                this.colorConverter = owningTarget.colorConverter;
                this.intensity = owningTarget.intensity;
            }

            public void FinishBrush(object brushKey) {
                // Dispose of the graphics.
                Graphics.Dispose();

                if (owningTarget.HasBrush(brushKey))
                    throw new InvalidOperationException("Key already has a brush created for it");

                // Create a TextureBrush on the bitmap.
                TextureBrush brush = new TextureBrush(bitmap);

                // Scale and the texture brush.
                brush.RotateTransform(angle);
                brush.ScaleTransform(size.Width / (float)bitmap.Width, size.Height / (float)bitmap.Height);
                brush.TranslateTransform(-bitmap.Width / 2F, -bitmap.Height / 2F);

                owningTarget.brushMap.Add(brushKey, brush);
            }
        }
    }

    // A graphics target that draw onto a bitmap. Also adds support for blending mode Darken.
    public class GDIPlus_BitmapGraphicsTarget : GDIPlus_GraphicsTarget, IBitmapGraphicsTarget
    {
        Stack<Bitmap> bitmapStack = new Stack<Bitmap>();
        Stack<Graphics> graphicsStack = new Stack<Graphics>();
        Stack<BlendMode> blendStack = new Stack<BlendMode>();
        int width, height;

        public GDIPlus_BitmapGraphicsTarget(int pixelWidth, int pixelHeight, bool alpha, CmykColor initialColor, RectangleF rectangle, bool inverted, GDIPlus_ColorConverter colorConverter = null, float intensity = 1.0F)
            :this(GetBitmap(pixelWidth, pixelHeight, alpha), initialColor, rectangle, inverted, colorConverter, intensity)
        {
        }

        public GDIPlus_BitmapGraphicsTarget(Bitmap bitmap, CmykColor initialColor, RectangleF rectangle, bool inverted, GDIPlus_ColorConverter colorConverter = null, float intensity = 1.0F)
            :this(bitmap, initialColor, GetTransform(bitmap, rectangle, inverted), colorConverter, intensity)
        {
        }

        public GDIPlus_BitmapGraphicsTarget(Bitmap bitmap, CmykColor initialColor, Matrix transform, GDIPlus_ColorConverter colorConverter = null, float intensity = 1.0F, Region clipRegion = null)
            : base(GetGraphics(bitmap, initialColor, transform, colorConverter, clipRegion), colorConverter, intensity)
        {
            width = bitmap.Width;
            height = bitmap.Height;

            bitmapStack.Push(bitmap);
        }

        public int PixelWidth { get { return width; } }
        public int PixelHeight { get { return height; } }

        static Graphics GetGraphics(Bitmap bitmap, CmykColor initialColor, Matrix transform, GDIPlus_ColorConverter colorConverter, Region clipRegion)
        {
            Graphics graphics = Graphics.FromImage(bitmap);
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Clip region is in Bitmap coordinates -- before the transform is applied.
            if (clipRegion != null)
                graphics.IntersectClip(clipRegion);

            graphics.Transform = transform;

            if (initialColor != null) {
                colorConverter = colorConverter ?? new GDIPlus_ColorConverter();
                graphics.Clear(colorConverter.ToColor(initialColor));
            }

            return graphics;
        }

        static Matrix GetTransform(Bitmap bitmap, RectangleF rectangle, bool inverted)
        {
            Size bitmapSize = bitmap.Size;
            PointF midpoint = new PointF(bitmapSize.Width / 2.0F, bitmapSize.Height / 2.0F);
            float scaleFactor = (float)bitmapSize.Width / rectangle.Width;
            PointF centerPoint = new PointF((rectangle.Left + rectangle.Right) / 2, (rectangle.Top + rectangle.Bottom) / 2);
            Matrix matrix = new Matrix();
            matrix.Translate(midpoint.X, midpoint.Y);
            matrix.Scale(scaleFactor, inverted ? -scaleFactor : scaleFactor);  // y scale is negative to get to cartesian orientation.
            matrix.Translate(-centerPoint.X, -centerPoint.Y);

            return matrix;
        }

        static Bitmap GetBitmap(int pixelWidth, int pixelHeight, bool alpha)
        {
            PixelFormat format = alpha ? PixelFormat.Format32bppPArgb : PixelFormat.Format24bppRgb;
            return new Bitmap(pixelWidth, pixelHeight, format);
        }

        public Bitmap Bitmap
        {
            get
            {
                Debug.Assert(bitmapStack.Count == 1, "Unbalanced push/pop blending");
                return bitmapStack.Peek();
            }
        }

        public IGraphicsBitmap FinishBitmap()
        {
            return new GDIPlus_Bitmap(Bitmap);
        }

        public override bool PushBlending(BlendMode blendMode)
        {
            blendStack.Push(blendMode);

            if (blendMode == BlendMode.Darken) {
                // Save old graphics.
                graphicsStack.Push(this.Graphics);

                // Create new bitmap to hold the content to blend.
                Bitmap currentBitmap = bitmapStack.Peek();
                Bitmap newBitmap = new Bitmap(currentBitmap.Width, currentBitmap.Height, currentBitmap.PixelFormat);
                Graphics newGraphics = Graphics.FromImage(newBitmap);
                newGraphics.PixelOffsetMode = this.Graphics.PixelOffsetMode;
                newGraphics.Clear(Color.White);
                this.ChangeGraphics(newGraphics);

                bitmapStack.Push(newBitmap);
                return true;
            }
            else {
                return false;
            }
        }

        public override void PopBlending()
        {
            BlendMode blendMode = blendStack.Pop();

            if (blendMode == BlendMode.Darken) {
                // Blend the bitmaps together.
                Bitmap blendFrom = bitmapStack.Pop();
                Bitmap blendTo = bitmapStack.Peek();

                BlendDarken(blendFrom, blendTo);

                Graphics oldGraphics = this.Graphics;
                this.ChangeGraphics(graphicsStack.Pop());
                oldGraphics.Dispose();
            }
        }

        private class Native32
        {
            [DllImport("GDIPlusNative.dll")]
            public static unsafe extern void DarkenBits(byte* bmFrom, int strideFrom, byte* bmTo, int strideTo, int height, int widthInBytes);
        }

        private class Native64
        {
            [DllImport("GDIPlusNative64.dll")]
            public static unsafe extern void DarkenBits(byte* bmFrom, int strideFrom, byte* bmTo, int strideTo, int height, int widthInBytes);
        }

        // Blend bitmapFrom and bitmapTo using darken blend mode (minimum of each R, G, B, (A) component.)
        // Put the result in bitmapTo.
        private unsafe void BlendDarken(Bitmap bitmapFrom, Bitmap bitmapTo)
        {
            Rectangle rect = new Rectangle(0, 0, bitmapFrom.Width, bitmapFrom.Height);
            BitmapData bmdataFrom = bitmapFrom.LockBits(rect, ImageLockMode.ReadOnly, bitmapFrom.PixelFormat);
            BitmapData bmdataTo = bitmapTo.LockBits(rect, ImageLockMode.ReadWrite, bitmapFrom.PixelFormat);
            int pixelSize = Image.GetPixelFormatSize(bitmapFrom.PixelFormat) / 8;
            int bytesPerScan = bmdataFrom.Width * pixelSize;
            
            if (Environment.Is64BitProcess)
                Native64.DarkenBits((byte *)bmdataFrom.Scan0, bmdataFrom.Stride, (byte*)bmdataTo.Scan0, bmdataTo.Stride, bmdataFrom.Height, bytesPerScan);
            else
                Native32.DarkenBits((byte*)bmdataFrom.Scan0, bmdataFrom.Stride, (byte*)bmdataTo.Scan0, bmdataTo.Stride, bmdataFrom.Height, bytesPerScan);

            /*
             * This code is the equivalent in C# unsafe code.
             * This is replaced by calling native function that uses SSE2 instructions.
             
            for (int scan = 0; scan < bmdataFrom.Height; ++scan) {
                byte* pixelFrom = ((byte*)bmdataFrom.Scan0) + scan * bmdataFrom.Stride;
                byte* pixelTo = ((byte*)bmdataTo.Scan0) + scan * bmdataTo.Stride;
                for (int i = 0; i < bytesPerScan; ++i) {
                    byte bFrom = *pixelFrom, bTo = *pixelTo;
                    if (bFrom < bTo)
                        *pixelTo = bFrom;
                    pixelFrom++;
                    pixelTo++;
                }
            }
            */

            bitmapFrom.UnlockBits(bmdataFrom);
            bitmapTo.UnlockBits(bmdataTo);
        }

        public override void Dispose()
        {
            base.Dispose();
            if (Graphics != null)
                Graphics.Dispose();
        }
    }

    public class GDIPlus_TextMetrics : ITextMetrics
    {
        public ITextFaceMetrics GetTextFaceMetrics(string familyName, float emHeight, TextEffects effects)
        {
            if (!TextFaceIsInstalled(familyName))
                familyName = "Arial";          // Map non-existant fonts to "Arial".

            return new GDIPlus_TextFaceMetrics(familyName, emHeight, effects);
        }

        public bool TextFaceIsInstalled(string familyName) {
            return GDIPlus_TextMetrics.FontFamilyIsInstalled(familyName);
        }

        public static bool FontFamilyIsInstalled(string familyName)
        {
            return GdiplusFontLoader.FontFamilyIsInstalled(familyName);
        }

        public void Dispose()
        {
        }
    }

    public class GDIPlus_TextFaceMetrics : ITextFaceMetrics
    {
        private Font font;
        private FontFamily fontFamily;
        private FontStyle fontStyle;
        private StringFormat stringFormat;
        private float emHeight;

        public GDIPlus_TextFaceMetrics(string familyName, float emHeight, TextEffects effects)
        {
            fontStyle = FontStyle.Regular;
            if ((effects & TextEffects.Bold) != 0)
                fontStyle |= FontStyle.Bold;
            if ((effects & TextEffects.Italic) != 0)
                fontStyle |= FontStyle.Italic;
            if ((effects & TextEffects.Underline) != 0)
                fontStyle |= FontStyle.Underline;

            float nominalFontSize = Math.Max(emHeight, 0.01F);            // 0 size fonts cause exception!
            this.emHeight = nominalFontSize;

            font = GdiplusFontLoader.CreateFont(familyName, nominalFontSize, fontStyle);
            fontFamily = font.FontFamily;

            stringFormat = new StringFormat(StringFormat.GenericTypographic);
            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.LineAlignment = StringAlignment.Near;
            stringFormat.FormatFlags |= StringFormatFlags.NoClip;
            stringFormat.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
        }

        public float  EmHeight
        {
            get { return emHeight; }
        }

        private float recommendedLineSpacing = -1;

        public float RecommendedLineSpacing
        {
            get
            {
                if (recommendedLineSpacing < 0) {
                    int nominalEmHeight = fontFamily.GetEmHeight(fontStyle);
                    int nominalLineSpacing = fontFamily.GetLineSpacing(fontStyle);
                    recommendedLineSpacing = (nominalLineSpacing * emHeight) / nominalEmHeight;
                }

                return recommendedLineSpacing;
            }
        }

        private float ascent = -1;

        public float  Ascent
        {
            get {
                if (ascent < 0) {
                    int nominalEmHeight = fontFamily.GetEmHeight(fontStyle);
                    int nominalAscent = fontFamily.GetCellAscent(fontStyle);
                    ascent = (nominalAscent * emHeight) / nominalEmHeight;
                }
                return ascent;
            }
        }

        private float descent = -1;
        public float  Descent
        {
            get {
                if (descent < 0) {
                    int nominalEmHeight = fontFamily.GetEmHeight(fontStyle);
                    int nominalDescent = fontFamily.GetCellDescent(fontStyle);
                    descent = (nominalDescent * emHeight) / nominalEmHeight;
                }
                return descent;
            }
        }

        private float capHeight = -1;
        public float  CapHeight
        {
            get {
                if (capHeight < 0) {
                    GraphicsPath path = new GraphicsPath();
                    path.AddString("W", fontFamily, (int)fontStyle, font.Size, new PointF(0, 0), stringFormat);
                    capHeight = path.GetBounds().Height;
                }
                return capHeight;
            }
        }

        private float spaceWidth = -1;
        public float  SpaceWidth
        {
            get {
                if (spaceWidth < 0) {
                    spaceWidth = GetTextWidth(" ");
                }
                return spaceWidth;
            }
        }

        public float  GetTextWidth(string text)
        {
            return GetHiresGraphics().MeasureString(text, font, new PointF(0, 0), stringFormat).Width;
        }

        public SizeF  GetTextSize(string text)
        {
            return GetHiresGraphics().MeasureString(text, font, new PointF(0, 0), stringFormat);
        }

        [ThreadStatic]
        static Graphics hiResGraphics = null;

        private static Graphics GetHiresGraphics()
        {
            if (hiResGraphics == null) {
                hiResGraphics = Graphics.FromImage(new Bitmap(1, 1));
                hiResGraphics.ScaleTransform(10F, -10F);
            }
            return hiResGraphics;
        }

        public void  Dispose()
        {
            // It is tempting to dispose the FontFamily here, but
            // that actually causes memory corruption! It's a bug.
            //fontFamily.Dispose();
            fontFamily = null;
            font.Dispose();
            font = null;
        }
    }

    public class GDIPlus_Bitmap : IGraphicsBitmap
    {
        Bitmap bitmap;
        Bitmap shrunkBitmap;  // only used if IsExtraLarge==true.

        public Bitmap Bitmap
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

        // Very large bitmaps can cause exceptions when drawing. This property 
        // says if the bitmap is very large.
        public bool IsExtraLarge
        {
            get { return PixelWidth * PixelHeight > 36000000; }
        }

        public Bitmap ShrunkBitmap
        {
            get
            {
                if (IsExtraLarge) {
                    if (shrunkBitmap == null) {
                        double scale = Math.Ceiling(Math.Sqrt((PixelWidth * PixelHeight) / 10000000));
                        Size shrunkSize = new Size((int) Math.Round(PixelWidth / scale), (int) Math.Round(PixelHeight / scale));
                        //shrunkBitmap = new Bitmap(bitmap, shrunkSize);
                        //using (Graphics g = Graphics.FromImage(shrunkBitmap)) {
                        //    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        //    g.DrawImage(bitmap, new Rectangle(new Point(), shrunkSize), 0, 0, PixelWidth, PixelHeight, GraphicsUnit.Pixel);
                        //}
                        shrunkBitmap = (Bitmap) bitmap.GetThumbnailImage(shrunkSize.Width, shrunkSize.Height, () => false, IntPtr.Zero);
                    }

                    return shrunkBitmap;
                }
                else {
                    return bitmap;
                }
            }
        }

        public void Dispose()
        {
            lock (this) {
                if (bitmap != null)
                    bitmap.Dispose();
                bitmap = null;

                if (shrunkBitmap != null)
                    shrunkBitmap.Dispose();
                shrunkBitmap = null;
            }
        }

        public bool Disposed
        {
            get { return bitmap == null; }
        }

        public GDIPlus_Bitmap(Bitmap bitmap)
        {
            this.bitmap = bitmap;
            this.shrunkBitmap = null;
        }
    }

    public class GDIPlus_FileLoader : IFileLoader
    {
        private string basePath;

        public GDIPlus_FileLoader(string basePath)
        {
            this.basePath = basePath;
        }

        public IGraphicsBitmap LoadBitmap(string path, bool isTemplate)
        {
            string filePath = SearchForFile(path);
            if (filePath == null)
                return null;

            using (FileStream src = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                // Copy to a memory stream, so the main bitmap file isn't locked and OCAD can read it.
                MemoryStream memStm = new MemoryStream();
                src.CopyTo(memStm);
                memStm.Seek(0, SeekOrigin.Begin);

                Bitmap bitmap = null;
                try {
                    bitmap = Image.FromStream(memStm) as Bitmap;
                }
                catch (Exception) { }

                if (bitmap == null)
                    return null;

                return new GDIPlus_Bitmap(bitmap);
            }
        }

        public IGraphicsBitmap LoadBitmapFromData(byte[] data)
        {
            MemoryStream memStm = new MemoryStream(data);
            Bitmap bitmap = null;

            try {
                bitmap = Image.FromStream(memStm) as Bitmap;
            }
            catch (Exception) { }

            if (bitmap == null)
                return null;

            return new GDIPlus_Bitmap(bitmap);
        }

        public FileKind CheckFileKind(string path)
        {
            string filePath = SearchForFile(path);
            if (filePath == null)
                return FileKind.DoesntExist;

            try {
                using (Stream s = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    if (InputOutput.IsOcadFile(s) || InputOutput.IsOpenMapperFile(s))
                        return FileKind.OcadFile;
                    else
                        return FileKind.OtherFile;
                }
            }
            catch (IOException) {
                return FileKind.NotReadable;
            }
            catch (UnauthorizedAccessException) {
                return FileKind.NotReadable;
            }
        }

        public Map LoadMap(string path, Map referencingMap)
        {
            string filePath = SearchForFile(path);
            if (filePath == null)
                return null;

            Map newMap = new Map(referencingMap.TextMetricsProvider, new GDIPlus_FileLoader(Path.GetDirectoryName(filePath)));

            InputOutput.ReadFile(filePath, newMap);
            return newMap;
        }

        private string SearchForFile(string path)
        {
            try {
                if (File.Exists(path))
                    return path;

                if (basePath != null) {
                    string baseName = Path.GetFileName(path);
                    string revisedPath = Path.Combine(basePath, baseName);
                    if (File.Exists(revisedPath))
                        return revisedPath;
                }
            }
            catch (ArgumentException) {
                // If the path has invalid characters in it, we get here.
                return null;
            }

            return null;
        }
    }

    public class GDIPlus_ColorConverter
    {
        public virtual Color ToColor(CmykColor cmykColor)
        {
            return ColorConverter.ToColor(cmykColor);
        }
    }
}
