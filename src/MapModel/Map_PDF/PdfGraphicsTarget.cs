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

using PdfSharp.Drawing;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Bitmap = System.Drawing.Bitmap;
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using StringAlignment = System.Drawing.StringAlignment;
using StringFormat = System.Drawing.StringFormat;
using StringFormatFlags = System.Drawing.StringFormatFlags;
using SysDraw = System.Drawing;

// TODO: Needs more work to handle CMYK color space!


namespace PurplePen.MapModel
{

    // A GraphicsTarget encapsulates either a Graphics (for WinForms) or a DrawingContext (for WPF)
    public class Pdf_GraphicsTarget: IGraphicsTarget
    {
        private bool cmykMode;   // true=CMYK, false=RGB
        private XGraphics gfx;
        private Stack<XGraphicsState> stateStack;
        private XStringFormat stringFormat;
        private Dictionary<object, XPen> penMap = new Dictionary<object, XPen>(new IdentityComparer<object>());
        private Dictionary<object, XBrush> brushMap = new Dictionary<object, XBrush>(new IdentityComparer<object>());
        private Dictionary<object, XFont> fontMap = new Dictionary<object, XFont>(new IdentityComparer<object>());
        private Dictionary<object, XGraphicsPath> pathMap = new Dictionary<object, XGraphicsPath>(new IdentityComparer<object>());

        // Bitmaps above this size are split when drawing.
        private const int BITMAP_DRAW_LIMIT = 4000000;

        public Pdf_GraphicsTarget(XGraphics gfx, bool cmykMode)
        {
            this.gfx = gfx;
            this.cmykMode = cmykMode;
            stateStack = new Stack<XGraphicsState>();
            stringFormat = new XStringFormat();
            stringFormat.Alignment = XStringAlignment.Near;
            stringFormat.LineAlignment = XLineAlignment.Near;
            stringFormat.FormatFlags = XStringFormatFlags.MeasureTrailingSpaces;
        }

        public XGraphics XGraphics
        {
            get { return gfx; }
        }

        public void CreateSolidBrush(object brushKey, CmykColor color)
        {
            if (brushMap.ContainsKey(brushKey))
                throw new InvalidOperationException("Key already has a brush created for it");

            brushMap.Add(brushKey, new XSolidBrush(ToXColor(color)));
        }


        public bool SupportsPatternBrushes
        {
            get { return false; }
        }

        public IBrushTarget CreatePatternBrush(SizeF size, float angle, int bitmapWidth, int bitmapHeight)
        {
            throw new NotSupportedException();
        }

        public void CreatePen(object penKey, object brushKey, float width, LineCapMode caps, LineJoinMode join, float miterLimit)
        {
            if (penMap.ContainsKey(penKey))
                throw new InvalidOperationException("Key already has a pen created for it");

            XPen pen = new XPen(((XSolidBrush)GetBrush(brushKey)).Color, width);
            pen.LineCap = ToXLineCap(caps);
            pen.LineJoin = ToXLineJoin(join);
            pen.MiterLimit = miterLimit;

            penMap.Add(penKey, pen);
        }

        public void CreatePen(object penKey, CmykColor color, float width, LineCapMode caps, LineJoinMode join, float miterLimit)
        {
            if (penMap.ContainsKey(penKey))
                throw new InvalidOperationException("Key already has a pen created for it");

            XPen pen = new XPen(ToXColor(color), width);
            pen.LineCap = ToXLineCap(caps);
            pen.LineJoin = ToXLineJoin(join);
            pen.MiterLimit = miterLimit;

            penMap.Add(penKey, pen);
        }

        private XLineJoin ToXLineJoin(LineJoinMode linejoin)
        {
            switch (linejoin)
            {
                case LineJoinMode.Bevel:
                    return XLineJoin.Bevel;
                case LineJoinMode.Miter:
                    return XLineJoin.Miter;
                case LineJoinMode.MiterClipped:
                    return XLineJoin.Miter;
                case LineJoinMode.Round:
                    return XLineJoin.Round;
                default:
                    Debug.Fail("unexpected join");
                    throw new NotSupportedException();
            }
        }

        private XLineCap ToXLineCap(LineCapMode linecap)
        {
            switch (linecap)
            {
                case LineCapMode.Flat:
                    return XLineCap.Flat;
                case LineCapMode.Round:
                    return XLineCap.Round;
                case LineCapMode.Square:
                    return XLineCap.Square;
                default:
                    Debug.Fail("unexpected line cap");
                    throw new NotSupportedException();
            }
        }

        private XColor ToXColor(CmykColor color)
        {
            if (cmykMode)
                return XColor.FromCmyk(color.Alpha, color.Cyan, color.Magenta, color.Yellow, color.Black);
            else
                return XColor.FromArgb(ColorConverter.ToColor(color));
        }
        
        // Create font
        public void CreateFont(object fontKey, string familyName, float emHeight, TextEffects effects)
        {
            if (fontMap.ContainsKey(fontKey))
                throw new InvalidOperationException("Key already has a font created for it");

            System.Drawing.FontStyle fontStyle = System.Drawing.FontStyle.Regular;
            if ((effects & TextEffects.Bold) != 0)
                fontStyle |= System.Drawing.FontStyle.Bold;
            if ((effects & TextEffects.Italic) != 0)
                fontStyle |= System.Drawing.FontStyle.Italic;
            if ((effects & TextEffects.Underline) != 0)
                fontStyle |= System.Drawing.FontStyle.Underline;

            if (!GdiplusFontLoader.FontFamilyIsInstalled(familyName))
                familyName = "Arial";

            // Use the GdiplusFontLoader so we get private fonts too.
            emHeight = Math.Max(emHeight, 0.01F);            // 0 size fonts cause problems!
            System.Drawing.Font gdiFont = GdiplusFontLoader.CreateFont(familyName, emHeight, fontStyle);
            XFont font = new XFont(gdiFont, new XPdfFontOptions(PdfSharp.Pdf.PdfFontEncoding.Unicode, PdfSharp.Pdf.PdfFontEmbedding.Always));

            fontMap.Add(fontKey, font);
        }

        public void CreatePath(object pathKey, List<GraphicsPathPart> parts, AreaFillMode windingMode)
        {
            if (pathMap.ContainsKey(pathKey))
                throw new InvalidOperationException("Key already has a path created for it");

            XGraphicsPath path = GetXGraphicsPath(parts, windingMode);
            pathMap.Add(pathKey, path);
        }

        XGraphicsPath GetXGraphicsPath(List<GraphicsPathPart> parts, AreaFillMode windingMode)
        {
            XGraphicsPath path = new XGraphicsPath();
            path.FillMode = ToXFillMode(windingMode);
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
            stateStack.Push(gfx.Save());
            gfx.MultiplyTransform(matrix.ToSysDrawMatrix(), XMatrixOrder.Prepend);
        }

        // Pop the transform
        public void PopTransform()
        {
            gfx.Restore(stateStack.Pop());
        }

        // Set a clip on the graphics drawing target.
        public void PushClip(object pathKey)
        {
            stateStack.Push(gfx.Save());
            gfx.IntersectClip(GetGraphicsPath(pathKey));
        }

        public void PushClip(List<GraphicsPathPart> parts, AreaFillMode windingMode)
        {
            stateStack.Push(gfx.Save());
            gfx.IntersectClip(GetXGraphicsPath(parts, windingMode));
        }

        public void PushClip(RectangleF rect)
        {
            stateStack.Push(gfx.Save());
            gfx.IntersectClip(rect);
        }

        public void PushClip(RectangleF[] rects)
        {
            stateStack.Push(gfx.Save());

            XGraphicsPath path = new XGraphicsPath();
            path.AddRectangles(rects);
            gfx.IntersectClip(path);
        }

        // Pop the clip.
        public void PopClip()
        {
            gfx.Restore(stateStack.Pop());
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

        Stack<string> blendModeStack = new Stack<string>();
        // Set blending mode.
        public virtual bool PushBlending(BlendMode blendMode)
        {
            bool supported = false;
            string newBlendMode = "Normal";
            if (blendMode == BlendMode.Darken) {
                newBlendMode = "Darken";
                supported = true;
            }

            blendModeStack.Push(gfx.BlendMode);
            gfx.BlendMode = newBlendMode;

            return supported;
        }
        
        public virtual void PopBlending()
        {
            gfx.BlendMode = blendModeStack.Pop();
        }

        // Draw an line with a pen.
        public void DrawLine(object penKey, PointF start, PointF finish)
        {
            gfx.DrawLine(GetPen(penKey), start, finish);
        }

        // Draw an arc with a pen.
        public void DrawArc(object penKey, PointF center, float radius, float startAngle, float sweepAngle)
        {
            // Weirdly, using a sweepAngle of 0 causes the PDF code to generate a corrupt PDF.
            if (sweepAngle > 0) {
                gfx.DrawArc(GetPen(penKey), new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2), startAngle, sweepAngle);
            }
        }

        // Draw an ellipse with a pen.
        public void DrawEllipse(object penKey, PointF center, float radiusX, float radiusY)
        {
            gfx.DrawEllipse(GetPen(penKey), center.X - radiusX, center.Y - radiusY, 2 * radiusX, 2 * radiusY);
        }

        // Fill an ellipse with a brush.
        public void FillEllipse(object brushKey, PointF center, float radiusX, float radiusY)
        {
            gfx.DrawEllipse(GetBrush(brushKey), center.X - radiusX, center.Y - radiusY, 2 * radiusX, 2 * radiusY);
        }

        // Draw a rectangle with a pen.
        public void DrawRectangle(object penKey, RectangleF rect)
        {
            gfx.DrawRectangle(GetPen(penKey), rect.X, rect.Y, rect.Width, rect.Height);
        }

        // Fill a rectangle with a brush.
        public void FillRectangle(object brushKey, RectangleF rect)
        {
            gfx.DrawRectangle(GetBrush(brushKey), rect.X, rect.Y, rect.Width, rect.Height);
        }

        // Draw a polygon with a brush
        public void DrawPolygon(object penKey, PointF[] pts)
        {
            gfx.DrawPolygon(GetPen(penKey), pts);
        }

        // Draw lines with a brush
        public void DrawPolyline(object penKey, PointF[] pts)
        {
            gfx.DrawLines(GetPen(penKey), pts);
        }

        // Fill a polygon with a brush
        public void FillPolygon(object brushKey, PointF[] pts, AreaFillMode windingMode)
        {
            gfx.DrawPolygon(GetBrush(brushKey), pts, ToXFillMode(windingMode));
        }

        private XFillMode ToXFillMode(AreaFillMode windingMode)
        {
            switch (windingMode)
            {
                case AreaFillMode.Alternate:
                    return XFillMode.Alternate;
                case AreaFillMode.Winding:
                    return XFillMode.Winding;
                default:
                    return XFillMode.Alternate;
            }
        }

        // Draw a path with a pen.
        public void DrawPath(object penKey, object pathKey)
        {
            gfx.DrawPath(GetPen(penKey), GetGraphicsPath(pathKey));
        }

        public void DrawPath(object penKey, List<GraphicsPathPart> parts)
        {
            XGraphicsPath path = GetXGraphicsPath(parts, AreaFillMode.Alternate);
            gfx.DrawPath(GetPen(penKey), path);
        }

        // Fill a path with a brush.
        public void FillPath(object brushKey, object pathKey)
        {
            gfx.DrawPath(GetBrush(brushKey), GetGraphicsPath(pathKey));
        }

        public void FillPath(object brushKey, List<GraphicsPathPart> parts, AreaFillMode windingMode)
        {
            XGraphicsPath path = GetXGraphicsPath(parts, windingMode);
            gfx.DrawPath(GetBrush(brushKey), path);
        }

        // Draw text with upper-left corner of text at the given locations.
        public void DrawText(string text, object fontKey, object brushKey, PointF upperLeft)
        {
#if false
            gfx.DrawString(text, GetFont(fontKey), GetBrush(brushKey), upperLeft, stringFormat);

#else
            XFont font = GetFont(fontKey);
            XBrush brush = GetBrush(brushKey);

            List<StringGlyph> glyphs = GetGlyphs(text);

            SysDraw.FontStyle fs = default(SysDraw.FontStyle);
            if ((font.Style & XFontStyle.Bold) != 0)
                fs |= SysDraw.FontStyle.Bold;
            if ((font.Style & XFontStyle.Italic) != 0)
                fs |= SysDraw.FontStyle.Italic;
            SysDraw.Font sdFont = GdiplusFontLoader.CreateFont(font.Name, (float)font.Size, fs);

            List<RectangleF> rects = MeasureAllCharacterRanges(text, glyphs, sdFont, upperLeft);

            for (int i = 0; i < glyphs.Count; ++i) {
                gfx.DrawString(glyphs[i].Text, font, brush, rects[i].Location, stringFormat);
            }
#endif
        }

        // Work around for MeasureCharacterRanges only handles 32 ranges at once.
        private List<RectangleF> MeasureAllCharacterRanges(string text, List<StringGlyph> glyphs, SysDraw.Font font, PointF upperLeft)
        {
            const int MAXRANGES = 32;
            List<RectangleF> rects = new List<RectangleF>();

            RectangleF formatRectangle = new RectangleF(upperLeft, new SizeF(1E9F, 1E9F));
            
            StringFormat sf = new StringFormat(StringFormat.GenericTypographic);
            sf.Alignment = StringAlignment.Near;
            sf.LineAlignment = StringAlignment.Near;
            sf.FormatFlags |= StringFormatFlags.NoClip;
            sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

            SysDraw.CharacterRange[] ranges = (from gl in glyphs select new SysDraw.CharacterRange(gl.Index, gl.Length)).ToArray();
            SysDraw.Graphics gr = GetHiresGraphics();

            for (int i = 0; i < glyphs.Count; i += MAXRANGES) {
                int l = Math.Min(MAXRANGES, glyphs.Count - i);
                sf.SetMeasurableCharacterRanges(ranges.Skip(i).Take(l).ToArray());
                SysDraw.Region[] regions = gr.MeasureCharacterRanges(text, font, formatRectangle, sf);
                foreach (SysDraw.Region r in regions) {
                    rects.Add(r.GetBounds(gr));
                    r.Dispose();
                }
            }
            
            return rects;
        }

        private List<StringGlyph> GetGlyphs(string text)
        {
            List<StringGlyph> glyphs = new List<StringGlyph>();

            if (!string.IsNullOrEmpty(text)) {
                System.Globalization.TextElementEnumerator enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(text);
                while (enumerator.MoveNext()) {
                    string grapheme = enumerator.GetTextElement();
                    glyphs.Add(new StringGlyph(enumerator.ElementIndex, grapheme.Length, grapheme));
                }
            }

            return glyphs;
        }

        private static ThreadLocal<System.Drawing.Graphics> hiresGraphics = new ThreadLocal<System.Drawing.Graphics>(() => {
            System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
            g.ScaleTransform(50F, -50F);
            return g;
        });

        // Returns a graphics scaled with negative Y and hi-resolution (50 units/pixel or so).
        // Instances are per-thread, so that tests that use this can run in parallel.
        public static System.Drawing.Graphics GetHiresGraphics()
        {
            return hiresGraphics.Value;
        }

        // Draw text outline with upper-left corner of text at the given locations.
        public void DrawTextOutline(string text, object fontKey, object penKey, PointF upperLeft)
        {
            XFont xFont = GetFont(fontKey);
            XGraphicsPath grPath = new XGraphicsPath();
            grPath.FillMode = XFillMode.Winding;

            grPath.AddString(text, xFont.FontFamily, xFont.Style, xFont.Size, upperLeft, stringFormat);
            gfx.DrawPath(GetPen(penKey), grPath);
        }

        struct StringGlyph
        {
            public int Index;
            public int Length;
            public string Text;
            public StringGlyph(int index, int length, string text)
            {
                this.Index = index; this.Length = length; this.Text = text;
            }
        }

        // PDF Sharp only supposrts bitmaps in some pixel formats.
        private bool SupportedPixelFormat(SysDraw.Imaging.PixelFormat pixelFormat)
        {
            switch (pixelFormat) {
                case SysDraw.Imaging.PixelFormat.Format24bppRgb:
                case SysDraw.Imaging.PixelFormat.Format32bppRgb:
                case SysDraw.Imaging.PixelFormat.Format32bppArgb:
                case SysDraw.Imaging.PixelFormat.Format32bppPArgb:
                case SysDraw.Imaging.PixelFormat.Format8bppIndexed:
                case SysDraw.Imaging.PixelFormat.Format4bppIndexed:
                case SysDraw.Imaging.PixelFormat.Format1bppIndexed:
                    return true;

                default:
                    return false;
            }
        }

        // Bitmap.Clone doesn't work with all source pixel formats. Use this instead.
        private SysDraw.Bitmap CloneToArgb(SysDraw.Bitmap bmSrc, SysDraw.Rectangle rect)
        {
            SysDraw.Bitmap bmDest = new SysDraw.Bitmap(rect.Width, rect.Height, SysDraw.Imaging.PixelFormat.Format32bppArgb);
            using (SysDraw.Graphics graphics = SysDraw.Graphics.FromImage(bmDest)) {
                graphics.DrawImage(bmSrc, new SysDraw.Rectangle(0, 0, rect.Width, rect.Height), rect, SysDraw.GraphicsUnit.Pixel);
            }
            return bmDest;
        }

        // Draw a bitmap
        public void DrawBitmap(IGraphicsBitmap bm, RectangleF rectangle, BitmapScaling scalingMode, float minResolution)
        {
            if (bm.PixelHeight * bm.PixelWidth > BITMAP_DRAW_LIMIT) {
                // Very large bitmaps can't be drawn in one piece.
                DrawBitmapPartSplit(bm, 0, 0, bm.PixelWidth, bm.PixelHeight, rectangle, scalingMode, minResolution);
                return;
            }

            bool dispose = false;
            System.Drawing.Bitmap gdiBitmap = ((GDIPlus_Bitmap)bm).Bitmap;
            if (! SupportedPixelFormat(gdiBitmap.PixelFormat)) {
                // Reformat the bitmap into a different pixel format.
                gdiBitmap = CloneToArgb(gdiBitmap, new SysDraw.Rectangle(0, 0, gdiBitmap.Width, gdiBitmap.Height));
                dispose = true;
            }
            try {
                XImage image = XImage.FromGdiPlusImage(gdiBitmap);

                if (scalingMode == BitmapScaling.NearestNeighbor)
                    image.Interpolate = false;
                else
                    image.Interpolate = true;

                gfx.DrawImage(image, rectangle);
            }
            finally {
                if (dispose)
                    gdiBitmap.Dispose();
            }
        }

        // Draw part of a bitmap
        public void DrawBitmapPart(IGraphicsBitmap bm, int x, int y, int width, int height, RectangleF rectangle, BitmapScaling scalingMode, float minResolution)
        {
            if (width * height > BITMAP_DRAW_LIMIT) {
                // Very large bitmaps can't be drawn in one piece.
                DrawBitmapPartSplit(bm, x, y, width, height, rectangle, scalingMode, minResolution);
                return;
            }

            Bitmap bitmap = ((GDIPlus_Bitmap)bm).Bitmap;

            // Make sure we use a supported pixel format.
            Bitmap bitmapPart;
            SysDraw.Rectangle part = new SysDraw.Rectangle(x, y, width, height);
            if (!SupportedPixelFormat(bitmap.PixelFormat)) {
                bitmapPart = CloneToArgb(bitmap, part);
            }
            else {
                bitmapPart = bitmap.Clone(part, bitmap.PixelFormat);
            }

            try { 
                XImage image = XImage.FromGdiPlusImage(bitmapPart);

                if (scalingMode == BitmapScaling.NearestNeighbor)
                    image.Interpolate = false;
                else
                    image.Interpolate = true;

                gfx.DrawImage(image, rectangle);
            }
            finally {
                bitmapPart.Dispose();
            }
        }

        private void DrawBitmapPartSplit(IGraphicsBitmap bm, int x, int y, int width, int height, RectangleF rectangle, BitmapScaling scalingMode, float minResolution)
        {
            int xSrcSplit = x + width / 2, ySrcSplit = y + height / 2;

            float xDestSplit = rectangle.X + rectangle.Width * (xSrcSplit - x) / width;
            float yDestSplit = rectangle.Y + rectangle.Height * (ySrcSplit - y) / height;

            DrawBitmapPart(bm, x, y, xSrcSplit - x, ySrcSplit - y, RectangleF.FromLTRB(rectangle.X, rectangle.Y, xDestSplit, yDestSplit), scalingMode, minResolution);
            DrawBitmapPart(bm, xSrcSplit, y, x + width - xSrcSplit, ySrcSplit - y, RectangleF.FromLTRB(xDestSplit, rectangle.Y, rectangle.Right, yDestSplit), scalingMode, minResolution);
            DrawBitmapPart(bm, x, ySrcSplit, xSrcSplit - x, y + height - ySrcSplit, RectangleF.FromLTRB(rectangle.X, yDestSplit, xDestSplit, rectangle.Bottom), scalingMode, minResolution);
            DrawBitmapPart(bm, xSrcSplit, ySrcSplit, x + width - xSrcSplit, y + height - ySrcSplit, RectangleF.FromLTRB(xDestSplit, yDestSplit, rectangle.Right, rectangle.Bottom), scalingMode, minResolution);
        }


        public bool HasPath(object pathKey) {
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

        private XBrush GetBrush(object brushKey)
        {
            XBrush brush;
            if (brushMap.TryGetValue(brushKey, out brush))
                return brush;
            else
                throw new ArgumentException("Given key does not have a brush created for it", "brushKey");
        }

        private XPen GetPen(object penKey)
        {
            XPen pen;
            if (penMap.TryGetValue(penKey, out pen))
                return pen;
            else
                throw new ArgumentException("Given key does not have a pen created for it", "penKey");
        }

        private XFont GetFont(object fontKey)
        {
            XFont font;
            if (fontMap.TryGetValue(fontKey, out font))
                return font;
            else
                throw new ArgumentException("Given key does not have a font created for it", "fontKey");
        }

        private XGraphicsPath GetGraphicsPath(object pathKey)
        {
            XGraphicsPath path;
            if (pathMap.TryGetValue(pathKey, out path))
                return path;
            else
                throw new ArgumentException("Given key does not have a path created for it", "pathKey");
        }

        public void Dispose()
        {
            if (gfx != null) {
                gfx.Dispose();
                gfx = null;
            }
        }
    }
}
