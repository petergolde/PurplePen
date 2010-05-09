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

        public IGraphicsBrush CreateSolidBrush(Color color)
        {
            return new GDIPlus_Brush(color);
        }

        public IBrushTarget CreatePatternBrush(SizeF size, int bitmapWidth, int bitmapHeight)
        {
                // Create a new bitmap and fill it transparent.
                Bitmap bitmap = new Bitmap(bitmapWidth, bitmapHeight);
                Graphics g = Graphics.FromImage(bitmap);
                //g.CompositingMode = CompositingMode.SourceCopy;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillRectangle(Brushes.Transparent, 0, 0, bitmap.Width, bitmap.Height);
                g.TranslateTransform((float)bitmapWidth / 2F, (float)bitmapHeight / 2F);
                g.ScaleTransform((float)bitmapWidth / size.Width, (float)bitmapHeight / size.Height);

                return new GDIPlus_BrushTarget(g, bitmap, size);
        }

        public IGraphicsPen CreatePen(IGraphicsBrush brush, float width, LineCap caps, LineJoin join, float miterLimit)
        {
            Pen pen = new Pen((brush as GDIPlus_Brush).Brush, width);
            pen.StartCap = pen.EndCap = caps;
            pen.LineJoin = join;
            pen.MiterLimit = miterLimit;
            return new GDIPlus_Pen(pen);
        }

        public IGraphicsPen CreatePen(Color color, float width, LineCap caps, LineJoin join, float miterLimit)
        {
            Pen pen = new Pen(color, width);
            pen.StartCap = pen.EndCap = caps;
            pen.LineJoin = join;
            pen.MiterLimit = miterLimit;
            return new GDIPlus_Pen(pen);
        }

        // Create font
        public IGraphicsFont CreateFont(string familyName, float emHeight, bool bold, bool italic)
        {
            return new GDIPlus_Font(familyName, emHeight, bold, italic);
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
        public void FillPolygon(IGraphicsBrush brush, PointF[] pts, FillMode windingMode)
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

        // Draw text with upper-left corner of text at the given locations.
        public void DrawText(string text, IGraphicsFont font, IGraphicsBrush brush, PointF upperLeft)
        {
            // Occasonal GDI+ throws an exception if the font size is super small.
            try {
                Graphics.DrawString(text, (font as GDIPlus_Font).Font, (brush as GDIPlus_Brush).Brush, upperLeft, stringFormat);
            }
            catch (System.Runtime.InteropServices.ExternalException) {
                // Do nothing
            }
        }

        // Draw text outline with upper-left corner of text at the given locations.
        public void DrawTextOutline(string text, IGraphicsFont font, IGraphicsPen pen, PointF upperLeft)
        {
            Font gdiFont = (font as GDIPlus_Font).Font;
            GraphicsPath grPath = new GraphicsPath(FillMode.Winding);
            Debug.Assert(gdiFont.Unit == GraphicsUnit.World);

            grPath.AddString(text, gdiFont.FontFamily, (int)gdiFont.Style, gdiFont.Size, upperLeft, stringFormat);
            Graphics.DrawPath((pen as GDIPlus_Pen).Pen, grPath);
        }

        public void Dispose()
        { }
    }

    public class GDIPlus_BrushTarget : GDIPlus_GraphicsTarget, IBrushTarget
    {
        private Bitmap bitmap;
        private SizeF size;

        public GDIPlus_BrushTarget(Graphics g, Bitmap bitmap, SizeF size)
        : base(g)
        {
            this.bitmap = bitmap;
            this.size = size;
        }

        public IGraphicsBrush FinishBrush(float angle)
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

    public class GDIPlus_Font : IGraphicsFont
    {
        Font font;
        private float emHeight;

        public GDIPlus_Font(string familyName, float emHeight, bool bold, bool italic)
        {
            FontStyle fontStyle = FontStyle.Regular;
            if (bold)
                fontStyle |= FontStyle.Bold;
            if (italic)
                fontStyle |= FontStyle.Italic;

            this.emHeight = Math.Max(emHeight, 0.01F);            // 0 size fonts cause exception!
            font = new Font(familyName, this.emHeight, fontStyle, GraphicsUnit.World);
        }

        public Font Font
        {
            get { return font; }
        }

        public float EmHeight
        {
            get { return emHeight; }
        }

        public void Dispose()
        {
            font.Dispose();
            font = null;
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

        public bool TextFaceIsInstalled(string familyName)
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
