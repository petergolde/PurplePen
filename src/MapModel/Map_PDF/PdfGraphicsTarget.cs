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
        private bool cmykMode;   // true=CMYK, false=RGB
        private XGraphics gfx;
        private Stack<XGraphicsState> stateStack;
        private XStringFormat stringFormat;
        private Dictionary<object, XPen> penMap = new Dictionary<object, XPen>(new IdentityComparer<object>());
        private Dictionary<object, XBrush> brushMap = new Dictionary<object, XBrush>(new IdentityComparer<object>());
        private Dictionary<object, SkiaFont> fontMap = new Dictionary<object, SkiaFont>(new IdentityComparer<object>());
        private Dictionary<object, XGraphicsPath> pathMap = new Dictionary<object, XGraphicsPath>(new IdentityComparer<object>());

        public Pdf_GraphicsTarget(XGraphics gfx, bool cmykMode)
        {
            this.gfx = gfx;
            this.cmykMode = cmykMode;
            stateStack = new Stack<XGraphicsState>();
            stringFormat = new XStringFormat();
            stringFormat.Alignment = XStringAlignment.Near;
            stringFormat.LineAlignment = XLineAlignment.Near;
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
                gfx.DrawString(glyph.GlyphText, xfont, brush, new XPoint(glyph.Position.X, glyph.Position.Y - skiaFont.Ascent), stringFormat);
            }

        }



        // Draw text outline with upper-left corner of text at the given locations.
        public void DrawTextOutline(string text, object fontKey, object penKey, PointF upperLeft)
        {
            SkiaFont skiaFont = GetFont(fontKey);

            GlyphPosition[] glyphs = skiaFont.EnhancedTypeface.GetGlyphPositions(text, new SKPoint(upperLeft.X, upperLeft.Y), (float)skiaFont.EmHeight);

            XGraphicsPath grPath = new XGraphicsPath();
            grPath.FillMode = XFillMode.Winding;

            foreach (GlyphPosition glyph in glyphs) {
                AddTextOutlineToPath(grPath, glyph.GlyphText, glyph.Typeface, skiaFont.EmHeight, glyph.Position.X, glyph.Position.Y);
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

        private static void AddTextOutlineToPath(XGraphicsPath pdfPath, string text, SKTypeface typeFace, float fontSize, float x, float y)
        {
            // 1. Iterate of the path of the text.
            using (SKFont font = new SKFont(typeFace, fontSize))
            using (SKPath skPath = font.GetTextPath(text, new SKPoint(x, y)))
            using (SKPath.Iterator iterator = skPath.CreateIterator(false)) {

                // 3. Map each SKPath verb to the corresponding PDF path command.
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
            using (MemoryStream memStream = new MemoryStream()) {
                IGraphicsBitmap croppedBitmap = bm.Crop(x, y, width, height);
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

    // This is the FontResolver that we use. Because isBold and isItalic are not enough, we want to really encode
    // Skia information of weight, width, and slant. So we encode that information in the family name, and ignore the isBold and isItalic parameters.
    // The familyName looks like family^weight^width^slant.
    class PdfFontResolver : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            return new FontResolverInfo(familyName, false, false);
        }

        public byte[] GetFont(string faceName)
        {
            (string familyName, SKFontStyleWeight weight, SKFontStyleWidth width, SKFontStyleSlant slant) = DecodeFamilyName(faceName);
            ShapedTypeface shapedTypeface = ShapedTypeface.Get(familyName, weight, width, slant);
            return shapedTypeface.GetFontData();
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
