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

    // A GraphicsTarget encapsulates either a Graphics (for WinForms) or a DrawingContext (for WPF)
    public class GDIPlus_GraphicsTarget: IGraphicsTarget
    {
        public Graphics Graphics;
        private Stack<GraphicsState> stateStack;
        private StringFormat stringFormat;
        private Dictionary<object, Pen> penMap = new Dictionary<object, Pen>(new IdentityComparer<object>());
        private Dictionary<object, Brush> brushMap = new Dictionary<object, Brush>(new IdentityComparer<object>());
        private Dictionary<object, Font> fontMap = new Dictionary<object, Font>(new IdentityComparer<object>());
        private Dictionary<object, GraphicsPath> pathMap = new Dictionary<object, GraphicsPath>(new IdentityComparer<object>());

        public GDIPlus_GraphicsTarget(Graphics g)
        {
            this.Graphics = g;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            stateStack = new Stack<GraphicsState>();
            stringFormat = new StringFormat(StringFormat.GenericTypographic);
            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.LineAlignment = StringAlignment.Near;
            stringFormat.FormatFlags |= StringFormatFlags.NoClip;
            stringFormat.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
        }

        public void CreateGdiPlusBrush(object brushKey, System.Drawing.Brush brush) {
            if (brushMap.ContainsKey(brushKey))
                throw new InvalidOperationException("Key already has a brush created for it");

            brushMap.Add(brushKey, brush);
        }

        public void CreateSolidBrush(object brushKey, Color color)
        {
            if (brushMap.ContainsKey(brushKey))
                throw new InvalidOperationException("Key already has a brush created for it");

            brushMap.Add(brushKey, new SolidBrush(color));
        }

        public IBrushTarget CreatePatternBrush(SizeF size, int bitmapWidth, int bitmapHeight)
        {
            // Create a new bitmap and fill it transparent.
            Bitmap bitmap = new Bitmap(bitmapWidth, bitmapHeight);
            Graphics g = Graphics.FromImage(bitmap);
            g.CompositingMode = CompositingMode.SourceCopy;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.FillRectangle(Brushes.Transparent, 0, 0, bitmap.Width, bitmap.Height);
            g.TranslateTransform((float)bitmapWidth / 2F, (float)bitmapHeight / 2F);
            g.ScaleTransform((float)bitmapWidth / size.Width, (float)bitmapHeight / size.Height);

            return new GDIPlus_BrushTarget(this, g, bitmap, size);
        }

        public void CreatePen(object penKey, object brushKey, float width, LineCap caps, LineJoin join, float miterLimit)
        {
            if (penMap.ContainsKey(penKey))
                throw new InvalidOperationException("Key already has a pen created for it");

            Pen pen = new Pen(GetBrush(brushKey), width);
            pen.StartCap = pen.EndCap = caps;
            pen.LineJoin = join;
            pen.MiterLimit = miterLimit;

            penMap.Add(penKey, pen);
        }

        public void CreatePen(object penKey, Color color, float width, LineCap caps, LineJoin join, float miterLimit)
        {
            if (penMap.ContainsKey(penKey))
                throw new InvalidOperationException("Key already has a pen created for it");

            Pen pen = new Pen(color, width);
            pen.StartCap = pen.EndCap = caps;
            pen.LineJoin = join;
            pen.MiterLimit = miterLimit;

            penMap.Add(penKey, pen);
        }

        // Create font
        public void CreateFont(object fontKey, string familyName, float emHeight, bool bold, bool italic)
        {
            if (fontMap.ContainsKey(fontKey))
                throw new InvalidOperationException("Key already has a font created for it");

            FontStyle fontStyle = FontStyle.Regular;
            if (bold)
                fontStyle |= FontStyle.Bold;
            if (italic)
                fontStyle |= FontStyle.Italic;

            if (!GDIPlus_TextMetrics.FontFamilyIsInstalled(familyName))
                familyName = "Arial";

            emHeight = Math.Max(emHeight, 0.01F);            // 0 size fonts cause exception!
            Font font = new Font(familyName, emHeight, fontStyle, GraphicsUnit.World);

            fontMap.Add(fontKey, font);
        }

        public void CreatePath(object pathKey, IEnumerable<GraphicsPathPart> parts, FillMode windingMode)
        {
            if (pathMap.ContainsKey(pathKey))
                throw new InvalidOperationException("Key already has a path created for it");

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

            pathMap.Add(pathKey, path);
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

        // Pop the clip.
        public void PopClip()
        {
            Graphics.Restore(stateStack.Pop());
        }

        // Draw an line with a pen.
        public void DrawLine(object penKey, PointF start, PointF finish)
        {
            Graphics.DrawLine(GetPen(penKey), start, finish);
        }

        // Draw an arc with a pen.
        public void DrawArc(object penKey, RectangleF boundingRect, float startAngle, float sweepAngle)
        {
            Graphics.DrawArc(GetPen(penKey), boundingRect, startAngle, sweepAngle);
        }

        // Draw an ellipse with a pen.
        public void DrawEllipse(object penKey, PointF center, float radiusX, float radiusY)
        {
            Graphics.DrawEllipse(GetPen(penKey), center.X - radiusX, center.Y - radiusY, 2 * radiusX, 2 * radiusY);
        }

        // Fill an ellipse with a brush.
        public void FillEllipse(object brushKey, PointF center, float radiusX, float radiusY)
        {
            Graphics.FillEllipse(GetBrush(brushKey), center.X - radiusX, center.Y - radiusY, 2 * radiusX, 2 * radiusY);
        }

        // Draw a rectangle with a pen.
        public void DrawRectangle(object penKey, RectangleF rect)
        {
            Graphics.DrawRectangle(GetPen(penKey), rect.X, rect.Y, rect.Width, rect.Height);
        }

        // Fill a rectangle with a brush.
        public void FillRectangle(object brushKey, RectangleF rect)
        {
            Graphics.FillRectangle(GetBrush(brushKey), rect.X, rect.Y, rect.Width, rect.Height);
        }

        // Draw a polygon with a brush
        public void DrawPolygon(object penKey, PointF[] pts)
        {
            try
            {
                Graphics.DrawPolygon(GetPen(penKey), pts);
            }
            catch (OutOfMemoryException) {
                // Do nothing. Very occasionally, GDI+ given an out of memory exception for very short curves. Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        // Draw lines with a brush
        public void DrawPolyline(object penKey, PointF[] pts)
        {
            try
            {
                Graphics.DrawLines(GetPen(penKey), pts);
            }
            catch (OutOfMemoryException)
            {
                // Do nothing. Very occasionally, GDI+ given an out of memory exception for very short curves. Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        // Fill a polygon with a brush
        public void FillPolygon(object brushKey, PointF[] pts, FillMode windingMode)
        {
            Graphics.FillPolygon(GetBrush(brushKey), pts, windingMode);
        }

        // Draw a path with a pen.
        public void DrawPath(object penKey, object pathKey)
        {
            try
            {
                Graphics.DrawPath(GetPen(penKey), GetGraphicsPath(pathKey));
            }
            catch (OutOfMemoryException)
            {
                // Do nothing. Very occasionally, GDI+ given an out of memory exception for very short curves. Just ignore it; there's nothing else to do. See bug #1997301.
            }
        }

        // Fill a path with a brush.
        public void FillPath(object brushKey, object pathKey)
        {
            Graphics.FillPath(GetBrush(brushKey), GetGraphicsPath(pathKey));
        }

        // Draw text with upper-left corner of text at the given locations.
        public void DrawText(string text, object fontKey, object brushKey, PointF upperLeft)
        {
            // Occasonal GDI+ throws an exception if the font size is super small.
            try {
                Graphics.DrawString(text, GetFont(fontKey), GetBrush(brushKey), upperLeft, stringFormat);
            }
            catch (System.Runtime.InteropServices.ExternalException) {
                // Do nothing
            }
        }

        // Draw text outline with upper-left corner of text at the given locations.
        public void DrawTextOutline(string text, object fontKey, object penKey, PointF upperLeft)
        {
            Font gdiFont = GetFont(fontKey);
            GraphicsPath grPath = new GraphicsPath(FillMode.Winding);
            Debug.Assert(gdiFont.Unit == GraphicsUnit.World);

            grPath.AddString(text, gdiFont.FontFamily, (int)gdiFont.Style, gdiFont.Size, upperLeft, stringFormat);
            Graphics.DrawPath(GetPen(penKey), grPath);
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

        public void Dispose()
        {
            foreach (Pen pen in penMap.Values)
                pen.Dispose();
            penMap.Clear();

            foreach (Brush brush in brushMap.Values)
                brush.Dispose();
            brushMap.Clear();

            foreach (GraphicsPath path in pathMap.Values)
                path.Dispose();
            pathMap.Clear();

            foreach (Font font in fontMap.Values)
                font.Dispose();
            fontMap.Clear();
        }

        private class GDIPlus_BrushTarget : GDIPlus_GraphicsTarget, IBrushTarget
        {
            private GDIPlus_GraphicsTarget owningTarget;
            private Bitmap bitmap;
            private SizeF size;

            public GDIPlus_BrushTarget(GDIPlus_GraphicsTarget owningTarget, Graphics g, Bitmap bitmap, SizeF size)
                : base(g) {
                this.owningTarget = owningTarget;
                this.bitmap = bitmap;
                this.size = size;
            }

            public void FinishBrush(object brushKey, float angle) {
                // Dispose of the graphics.
                Graphics.Dispose();

                if (owningTarget.brushMap.ContainsKey(brushKey))
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



    public class GDIPlus_TextMetrics : ITextMetrics
    {
        public ITextFaceMetrics GetTextFaceMetrics(string familyName, float emHeight, bool bold, bool italic)
        {
            if (!TextFaceIsInstalled(familyName))
                familyName = "Arial";          // Map non-existant fonts to "Arial".

            return new GDIPlus_TextFaceMetrics(familyName, emHeight, bold, italic);
        }

        public bool TextFaceIsInstalled(string familyName) {
            return GDIPlus_TextMetrics.FontFamilyIsInstalled(familyName);
        }

        public static bool FontFamilyIsInstalled(string familyName)
        {
            // Doesn't seem to be an easy way to determine if a font exists.
            try {
                FontFamily family = new FontFamily(familyName);
                family.Dispose();
                return true;
            }
            catch {
                return false;
            }
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

        public GDIPlus_TextFaceMetrics(string familyName, float emHeight, bool bold, bool italic)
        {
            fontStyle = FontStyle.Regular;
            if (bold)
                fontStyle |= FontStyle.Bold;
            if (italic)
                fontStyle |= FontStyle.Italic;

            float nominalFontSize = Math.Max(emHeight, 0.01F);            // 0 size fonts cause exception!
            this.emHeight = nominalFontSize;

            font = new Font(familyName, nominalFontSize, fontStyle, GraphicsUnit.World);
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
                    return path.GetBounds().Height;
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
                hiResGraphics = Graphics.FromHwnd(IntPtr.Zero);
                hiResGraphics.ScaleTransform(50F, -50F);
            }
            return hiResGraphics;
        }

        public void  Dispose()
        {
            fontFamily.Dispose();
            fontFamily = null;
            font.Dispose();
            font = null;
        }
    }
}
