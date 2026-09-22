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

using Map_SkiaStd;
using PdfSharp.Drawing;
using PdfSharp.Events;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;


namespace PurplePen.MapModel
{

    // A GraphicsTarget encapsulates either a Graphics (for WinForms) or a DrawingContext (for WPF)
    public class Pdf_GraphicsTarget: IGraphicsTarget
    {
        // Bitmaps larger than this many pixels are drawn as a set of smaller tiles rather than
        // in one piece. A single very large image forces everything downstream -- our own PNG
        // encoder, PdfSharp's importer, and ultimately the RIP that prints the file -- to hold
        // the whole decoded image, plus its mask, in memory at once. Splitting keeps each piece
        // to a size that has always worked.
        private const int BITMAP_DRAW_LIMIT = 4000000;

        private bool cmykMode;   // true=CMYK, false=RGB
        private XGraphics gfx;
        private PdfGlyphSubstituter glyphSubstituter;   // supplies shaped glyphs to PDFsharp
        private Stack<XGraphicsState> stateStack;
        private XStringFormat stringFormat;
        private Dictionary<object, XPen> penMap = new Dictionary<object, XPen>(new IdentityComparer<object>());
        private Dictionary<object, XBrush> brushMap = new Dictionary<object, XBrush>(new IdentityComparer<object>());
        private Dictionary<object, SkiaFont> fontMap = new Dictionary<object, SkiaFont>(new IdentityComparer<object>());
        private Dictionary<object, XGraphicsPath> pathMap = new Dictionary<object, XGraphicsPath>(new IdentityComparer<object>());

        // Create a graphics target that draws into the given XGraphics. Text drawn through this
        // target has its glyphs resolved by PDFsharp from the characters, which loses the shaping
        // done by HarfBuzz. PdfDocumentWriter uses the overload below instead.
        //
        // Parameters:
        //   gfx - the PDFsharp graphics object to draw into.
        //   cmykMode - true for CMYK output, false for RGB.
        public Pdf_GraphicsTarget(XGraphics gfx, bool cmykMode)
            : this(gfx, cmykMode, null)
        {
        }

        // Create a graphics target that draws into the given XGraphics, handing PDFsharp the
        // glyphs that were actually shaped.
        //
        // Parameters:
        //   gfx - the PDFsharp graphics object to draw into.
        //   cmykMode - true for CMYK output, false for RGB.
        //   glyphSubstituter - the substituter hooked to the owning document's RenderTextEvent,
        //     used to hand PDFsharp the shaped glyph for each character drawn. May be null, in
        //     which case PDFsharp maps characters to glyphs itself.
        internal Pdf_GraphicsTarget(XGraphics gfx, bool cmykMode, PdfGlyphSubstituter glyphSubstituter)
        {
            this.gfx = gfx;
            this.cmykMode = cmykMode;
            this.glyphSubstituter = glyphSubstituter;
            stateStack = new Stack<XGraphicsState>();
            stringFormat = new XStringFormat();
            stringFormat.Alignment = XStringAlignment.Near;

            // Position text by its baseline, not its top. The glyph positions we get from
            // EnhancedTypeface are already baseline-relative, so we can pass them straight
            // through. With XLineAlignment.Near, PdfSharp would instead add its own
            // XFont.CellAscent to our Y, which is a different quantity computed from the font
            // tables by PdfSharp's OpenTypeDescriptor -- notably it folds sTypoLineGap into
            // the ascender for USE_TYPO_METRICS fonts, which DirectWrite and GDI+ do not.
            // Using the baseline keeps DrawText and DrawTextOutline consistent and keeps all
            // vertical positioning decisions in FontVerticalMetrics.
            stringFormat.LineAlignment = XLineAlignment.BaseLine;
        }

        public float Intensity {
            get { return 1.0F; }
            set {
                if (value != 1.0F) {
                    throw new ArgumentException("Only intensities of 1.0 are supported", "value");
                }
            }
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

        private XPoint ToXPoint(PointF pt)
        {
            return new XPoint(pt.X, pt.Y);
        }

        private XRect ToXRect(RectangleF rect)
        {
            return new XRect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        private XColor ToXColor(CmykColor color)
        {
            if (cmykMode)
                return XColor.FromCmyk(color.Alpha, color.Cyan, color.Magenta, color.Yellow, color.Black);
            else {
                System.Drawing.Color sysDrawColor = PurplePen.Graphics2D.ColorConverter.ToColor(color);
                return XColor.FromArgb(sysDrawColor.A, sysDrawColor.R, sysDrawColor.G, sysDrawColor.B);
            }
        }

        private XFontStyleEx ToXFontStyleEx(TextEffects effects)
        {
            XFontStyleEx style = XFontStyleEx.Regular;
            if ((effects & TextEffects.Bold) != 0)
                style |= XFontStyleEx.Bold;
            if ((effects & TextEffects.Italic) != 0)
                style |= XFontStyleEx.Italic;
            if ((effects & TextEffects.Underline) != 0)
                style |= XFontStyleEx.Underline;
            return style;
        }

        private XMatrix ToXMatrix(Matrix mat)
        {
            float[] elements = mat.Elements;
            XMatrix xMatrix = new XMatrix(elements[0], elements[1], elements[2], elements[3], elements[4], elements[5]);
            return xMatrix;
        }

        // Create font. We use the same SkiaFont class for PDF as for Skia, since it just encapsulates the font information
        // we need. We later use the SkiaFont to determine specific font information we need to draw with.
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
                        XPoint[] newPoints = new XPoint[part.Points.Length + 1];
                        newPoints[0] = new XPoint(startPoint.X, startPoint.Y);
                        for (int i = 0; i < part.Points.Length; ++i) {
                            newPoints[i + 1] = new XPoint(part.Points[i].X, part.Points[i].Y);
                        }
                        path.AddLines(newPoints);
                        startPoint = part.Points[part.Points.Length - 1];
                        break;
                    }

                    case GraphicsPathPartKind.Beziers: {
                        XPoint[] newPoints = new XPoint[part.Points.Length + 1];
                        newPoints[0] = new XPoint(startPoint.X, startPoint.Y);
                        for (int i = 0; i < part.Points.Length; ++i) {
                            newPoints[i + 1] = new XPoint(part.Points[i].X, part.Points[i].Y);
                        }
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
            gfx.MultiplyTransform(ToXMatrix(matrix), XMatrixOrder.Prepend);
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
            gfx.IntersectClip(ToXRect(rect));
        }

        public void PushClip(RectangleF[] rects)
        {
            stateStack.Push(gfx.Save());

            XGraphicsPath path = new XGraphicsPath();
            foreach (RectangleF rect in rects) {
                path.AddRectangle(ToXRect(rect));
            }

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

        Stack<XBlendMode> blendModeStack = new Stack<XBlendMode>();
        // Set blending mode.
        public virtual bool PushBlending(BlendMode blendMode)
        {
            bool supported = false;
            XBlendMode newBlendMode = XBlendMode.Normal;
            if (blendMode == BlendMode.Darken) {
                newBlendMode = XBlendMode.Darken;
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
            gfx.DrawLine(GetPen(penKey), ToXPoint(start), ToXPoint(finish));
        }

        // Draw an arc with a pen.
        public void DrawArc(object penKey, PointF center, float radius, float startAngle, float sweepAngle)
        {
            // Weirdly, using a sweepAngle of 0 causes the PDF code to generate a corrupt PDF.
            if (sweepAngle > 0) {
                gfx.DrawArc(GetPen(penKey), new XRect(center.X - radius, center.Y - radius, radius * 2, radius * 2), startAngle, sweepAngle);
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
            XPoint[] xPts = new XPoint[pts.Length];
            for (int i = 0; i < pts.Length; i++) {
                xPts[i] = ToXPoint(pts[i]);
            }

            gfx.DrawPolygon(GetPen(penKey), xPts);
        }

        // Draw lines with a brush
        public void DrawPolyline(object penKey, PointF[] pts)
        {
            XPoint[] xPts = new XPoint[pts.Length];
            for (int i = 0; i < pts.Length; i++) {
                xPts[i] = ToXPoint(pts[i]);
            }
            gfx.DrawLines(GetPen(penKey), xPts);
        }

        // Fill a polygon with a brush
        public void FillPolygon(object brushKey, PointF[] pts, AreaFillMode windingMode)
        {
            XPoint[] xPts = new XPoint[pts.Length];
            for (int i = 0; i < pts.Length; i++) {
                xPts[i] = ToXPoint(pts[i]);
            }

            gfx.DrawPolygon(GetBrush(brushKey), xPts, ToXFillMode(windingMode));
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
            SkiaFont skiaFont = GetFont(fontKey);
            XBrush brush = GetBrush(brushKey);

            GlyphPosition[] glyphs = skiaFont.EnhancedTypeface.GetGlyphPositions(text, new SKPoint(upperLeft.X, upperLeft.Y), (float)skiaFont.EmHeight);

            foreach (GlyphPosition glyph in glyphs) {
                XFont xfont = XFontFromTypeface(glyph.Typeface, skiaFont.EmHeight);

                // Tell PDFsharp which glyph HarfBuzz picked for this cluster. Without this it
                // would map glyph.GlyphText through the font's character map itself and lose the
                // shaping. The value is set for exactly one DrawString call, because PDFsharp
                // raises the same event when measuring text.
                if (glyphSubstituter != null)
                    glyphSubstituter.PendingGlyphId = (ushort) glyph.GlyphId;

                try {
                    // glyph.Position is already on the baseline, and stringFormat uses
                    // XLineAlignment.BaseLine, so it is passed through unadjusted.
                    gfx.DrawString(glyph.GlyphText, xfont, brush, new XPoint(glyph.Position.X, glyph.Position.Y), stringFormat);
                }
                finally {
                    if (glyphSubstituter != null)
                        glyphSubstituter.PendingGlyphId = null;
                }
            }

        }



        // Draw text outline with upper-left corner of text at the given locations.
        public void DrawTextOutline(string text, object fontKey, object penKey, PointF upperLeft)
        {
            SkiaFont skiaFont = GetFont(fontKey);

            XGraphicsPath grPath = new XGraphicsPath();
            grPath.FillMode = XFillMode.Winding;

            // EnhancedTypeface.GetTextPath outlines the glyphs that HarfBuzz shaped, looking each
            // one up by glyph id. Going back to the characters here instead -- as this used to do,
            // via SKFont.GetTextPath -- would map them through the font's character map again and
            // throw the shaping away, which is the same mistake DrawText used to make. It also
            // keeps the outlined text identical to what the Skia target draws on screen, since
            // that uses this same method.
            using (SKPath skPath = skiaFont.EnhancedTypeface.GetTextPath(text, new SKPoint(upperLeft.X, upperLeft.Y), (float) skiaFont.EmHeight)) {
                AddSkiaPathToPdfPath(grPath, skPath);
            }

            gfx.DrawPath(GetPen(penKey), grPath);
        }

        // Given a Skia typeface and height, create an XFont that we can use to draw with. We encode the Skia typeface information into the family name,
        // and then use our PdfFontResolver to get the font data when needed.
        private XFont XFontFromTypeface(SKTypeface typeFace, float height)
        {
            string encodedFamilyName = PdfFontResolver.GetEncodedFamilyName(typeFace.FamilyName, (SKFontStyleWeight) typeFace.FontWeight, (SKFontStyleWidth) typeFace.FontWidth, typeFace.FontSlant);
            return new XFont(encodedFamilyName, height, XFontStyleEx.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode, PdfFontEmbedding.TryComputeSubset));
        }

        // Convert a Skia path into a PDF path, appending to whatever is already there.
        //
        // Parameters:
        //   pdfPath - the path to append to.
        //   skPath - the Skia path to convert.
        private static void AddSkiaPathToPdfPath(XGraphicsPath pdfPath, SKPath skPath)
        {
            using (SKPath.Iterator iterator = skPath.CreateIterator(false)) {

                // Map each SKPath verb to the corresponding PDF path command.
                SKPathVerb verb;
                SKPoint[] pts = new SKPoint[4];

                while ((verb = iterator.Next(pts)) != SKPathVerb.Done) {
                    switch (verb) {
                    case SKPathVerb.Move:
                        // Start a new independent figure (e.g., a new letter or a hole inside a letter)
                        pdfPath.StartFigure();
                        break;

                    case SKPathVerb.Line:
                        // Draw a straight line
                        pdfPath.AddLine(pts[0].X, pts[0].Y, pts[1].X, pts[1].Y);
                        break;

                    case SKPathVerb.Cubic:
                        // Draw a cubic bezier curve directly
                        pdfPath.AddBezier(
                            pts[0].X, pts[0].Y,
                            pts[1].X, pts[1].Y,
                            pts[2].X, pts[2].Y,
                            pts[3].X, pts[3].Y);
                        break;

                    case SKPathVerb.Quad:
                        // PDFsharp only supports Cubic Beziers, but TrueType fonts use Quadratic. 
                        // We convert Quadratic to Cubic using standard math:
                        double cp1X = pts[0].X + (2.0 / 3.0) * (pts[1].X - pts[0].X);
                        double cp1Y = pts[0].Y + (2.0 / 3.0) * (pts[1].Y - pts[0].Y);
                        double cp2X = pts[2].X + (2.0 / 3.0) * (pts[1].X - pts[2].X);
                        double cp2Y = pts[2].Y + (2.0 / 3.0) * (pts[1].Y - pts[2].Y);

                        pdfPath.AddBezier(
                            pts[0].X, pts[0].Y,
                            cp1X, cp1Y,
                            cp2X, cp2Y,
                            pts[2].X, pts[2].Y);
                        break;

                    case SKPathVerb.Close:
                        pdfPath.CloseFigure();
                        break;
                    }
                }
            }
        }

        // Draw a bitmap
        public void DrawBitmap(IGraphicsBitmap bm, RectangleF rectangle, BitmapScaling scalingMode)
        {
            if (bm.PixelWidth * (long) bm.PixelHeight > BITMAP_DRAW_LIMIT) {
                // Very large bitmaps aren't drawn in one piece.
                DrawBitmapPartSplit(bm, 0, 0, bm.PixelWidth, bm.PixelHeight, rectangle, scalingMode);
                return;
            }

            using (MemoryStream memStream = new MemoryStream()) {
                if (bm.WriteToStream(GraphicsBitmapFormat.PNG, memStream, 100)) {
                    using (XImage image = XImage.FromStream(memStream)) {
                        if (scalingMode == BitmapScaling.NearestNeighbor)
                            image.Interpolate = false;
                        else
                            image.Interpolate = true;

                        gfx.DrawImage(image, ToXRect(rectangle));
                    }
                }
            }
        }

        // Draw part of a bitmap
        public void DrawBitmapPart(IGraphicsBitmap bm, int x, int y, int width, int height, RectangleF rectangle, BitmapScaling scalingMode)
        {
            if (width * (long) height > BITMAP_DRAW_LIMIT) {
                // Very large bitmaps aren't drawn in one piece.
                DrawBitmapPartSplit(bm, x, y, width, height, rectangle, scalingMode);
                return;
            }

            using (MemoryStream memStream = new MemoryStream())
            using (IGraphicsBitmap croppedBitmap = bm.Crop(x, y, width, height)) {
                if (croppedBitmap.WriteToStream(GraphicsBitmapFormat.PNG, memStream, 100)) {
                    using (XImage image = XImage.FromStream(memStream)) {
                        if (scalingMode == BitmapScaling.NearestNeighbor)
                            image.Interpolate = false;
                        else
                            image.Interpolate = true;

                        gfx.DrawImage(image, ToXRect(rectangle));
                    }
                }
            }
        }

        // Draw part of a bitmap that is too large to draw in one piece, by splitting it into
        // quarters and drawing each separately. Each quarter goes back through DrawBitmapPart,
        // so a bitmap that is still too big after one split is split again.
        private void DrawBitmapPartSplit(IGraphicsBitmap bm, int x, int y, int width, int height, RectangleF rectangle, BitmapScaling scalingMode)
        {
            int xSrcSplit = x + width / 2, ySrcSplit = y + height / 2;

            float xDestSplit = rectangle.X + rectangle.Width * (xSrcSplit - x) / width;
            float yDestSplit = rectangle.Y + rectangle.Height * (ySrcSplit - y) / height;

            DrawBitmapPart(bm, x, y, xSrcSplit - x, ySrcSplit - y,
                           RectangleF.FromLTRB(rectangle.X, rectangle.Y, xDestSplit, yDestSplit), scalingMode);
            DrawBitmapPart(bm, xSrcSplit, y, x + width - xSrcSplit, ySrcSplit - y,
                           RectangleF.FromLTRB(xDestSplit, rectangle.Y, rectangle.Right, yDestSplit), scalingMode);
            DrawBitmapPart(bm, x, ySrcSplit, xSrcSplit - x, y + height - ySrcSplit,
                           RectangleF.FromLTRB(rectangle.X, yDestSplit, xDestSplit, rectangle.Bottom), scalingMode);
            DrawBitmapPart(bm, xSrcSplit, ySrcSplit, x + width - xSrcSplit, y + height - ySrcSplit,
                           RectangleF.FromLTRB(xDestSplit, yDestSplit, rectangle.Right, rectangle.Bottom), scalingMode);
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

        private SkiaFont GetFont(object fontKey)
        {
            SkiaFont font;
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

    // Supplies PDFsharp with the glyph that HarfBuzz actually chose, in place of the one
    // PDFsharp would look up for itself.
    //
    // Pdf_GraphicsTarget shapes a string with HarfBuzz and then draws it one glyph at a time.
    // XGraphics.DrawString takes characters rather than glyphs, so on its own PDFsharp would map
    // each cluster back through the font's character map and discard the shaping: ligatures come
    // apart, combining marks are placed as separate characters, and any character the font cannot
    // map becomes .notdef, which prints as a hollow box. PDFsharp raises RenderTextEvent after it
    // has resolved glyph indices and before it writes them out, which is where the shaped glyph
    // can be put back.
    //
    // One instance belongs to each PdfDocumentWriter and is shared by every graphics target that
    // draws into that document. It holds the glyph for the draw currently in progress, so it is
    // no more thread safe than the rest of PDF generation.
    internal class PdfGlyphSubstituter
    {
        // The glyph to write for the DrawString call currently in progress, or null when no
        // substitution applies. PDFsharp also raises RenderTextEvent while measuring, so a null
        // here means "leave whatever PDFsharp resolved alone".
        public ushort? PendingGlyphId { get; set; }

        // Handler for PdfDocument.RenderEvents.RenderTextEvent.
        public void OnRenderText(object sender, RenderTextEventArgs e)
        {
            if (PendingGlyphId == null)
                return;

            CodePointGlyphIndexPair[] pairs = e.CodePointGlyphIndexPairs;

            // One glyph replaces the whole cluster. Keep the cluster's first code point so the
            // /ToUnicode map can still say what this glyph stands for; a cluster covering several
            // code points can only record the first of them.
            int codePoint = pairs.Length > 0 ? pairs[0].CodePoint : 0;

            e.CodePointGlyphIndexPairs = new CodePointGlyphIndexPair[] {
                new CodePointGlyphIndexPair(codePoint, PendingGlyphId.Value)
            };
        }
    }

    // This is the FontResolver that we use. Because isBold and isItalic are not enough, we want to really encode
    // Skia information of weight, width, and slant. So we encode that information in the family name, and ignore the isBold and isItalic parameters.
    // The familyName looks like family^weight^width^slant.
    class PdfFontResolver : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // The collection number tells PDFsharp which face to read out of the data GetFont
            // returns. It is only non-zero for a font that lives in a TrueType collection, where
            // the data is the whole .ttc. Without it PDFsharp reads face 0, which is a different
            // font from the one the text was shaped with -- for instance Nirmala UI Bold would be
            // embedded as Nirmala UI Regular.
            return new FontResolverInfo(familyName, false, false, LookupTypeface(familyName).FontDataCollectionIndex);
        }

        public byte[] GetFont(string faceName)
        {
            return LookupTypeface(faceName).GetFontData();
        }

        // Find the ShapedTypeface for an encoded face name. Both of the methods above go through
        // here, so the font data and the collection number that selects a face within it are
        // always taken from the same typeface.
        //
        // Parameters:
        //   faceName - an encoded name as produced by GetEncodedFamilyName.
        private static ShapedTypeface LookupTypeface(string faceName)
        {
            (string familyName, SKFontStyleWeight weight, SKFontStyleWidth width, SKFontStyleSlant slant) = DecodeFamilyName(faceName);

            // Prefer the cached face. The name was encoded from a typeface that was already
            // resolved while the text was being laid out, and every such typeface is in the
            // ShapedTypeface cache under exactly this family name and style -- including the
            // ones that came from platform font fallback, which ShapedTypeface.GetOrAdd caches
            // under their own name for this reason. Resolving the name again would go back
            // through SKTypeface.FromFamilyName, which does not reliably return the same face
            // for a fallback family (fontconfig aliases in particular) and silently substitutes
            // the default font when it finds nothing, embedding the wrong glyphs in the PDF.
            if (!ShapedTypeface.TryGetCached(familyName, weight, width, slant, out ShapedTypeface shapedTypeface))
                shapedTypeface = ShapedTypeface.Get(familyName, weight, width, slant);

            return shapedTypeface;
        }

        public static string GetEncodedFamilyName(string familyName, SKFontStyleWeight weight, SKFontStyleWidth width, SKFontStyleSlant slant)
        {
            return $"{familyName}^{(int) weight}^{(int) width}^{(int) slant}";
        }

        public static (string, SKFontStyleWeight, SKFontStyleWidth, SKFontStyleSlant) DecodeFamilyName(string encodedFamilyName)
        {
            string[] parts = encodedFamilyName.Split('^');
            if (parts.Length != 4)
                throw new ArgumentException("Invalid encoded family name", "encodedFamilyName");
            string familyName = parts[0];
            SKFontStyleWeight weight = (SKFontStyleWeight) int.Parse(parts[1]);
            SKFontStyleWidth width = (SKFontStyleWidth) int.Parse(parts[2]);
            SKFontStyleSlant slant = (SKFontStyleSlant) int.Parse(parts[3]);
            return (familyName, weight, width, slant);
        }
    }
}
