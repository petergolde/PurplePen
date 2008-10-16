/* Copyright (c) 2006-2007, Peter Golde
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
#if WPF
using System.Windows.Media;
#else
using System.Drawing;
using System.Drawing.Drawing2D;
#endif

namespace PurplePen.MapModel
{
    public abstract class Symbol
    {
        public abstract SymDef Definition {get;}

        // The containing map
        protected Map map;
        public Map ContainingMap { get {return map;} }

        internal void SetMap(Map newMap)
        {
            if (map != null && newMap != null && map != newMap)
                throw new MapUsageException("Cannot add symbol to a map; it is already part of another map.");
            map = newMap;
        }

        protected void CheckModifiable()
        {
            if (map != null)
                throw new MapUsageException("Cannot modify a symbol after it has been added to a map");
        }

        // A box guaranteed to bound this symbol. May be bigger than needed.
        protected RectangleF boundingBox;
        public RectangleF BoundingBox {get {return boundingBox; }}

        // Determine accurately if this point is within distance of a this symbol.
        public abstract bool HitTest(PointF point, float distance, out float actualDistance);

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public abstract bool MayIntersectRect(RectangleF rect);

        // Draw this color, if used, of this symbol.
        public abstract void Draw(GraphicsTarget g, SymColor color, RenderOptions renderOpts);
    }

    public class PointSymbol: Symbol
    {
        PointSymDef def;
        public override SymDef Definition { get { return def; }}

        PointF location;
        public PointF Location {get { return location; }}

        float rotation;  // angle in dgrees symbol is rotated.
        public float Rotation {get { return rotation; }}

        float[] gaps;     // sorted array of start/end angles for gaps in circles; null for none.
        public float[] Gaps { get { return gaps; } }

        public PointSymbol(PointSymDef def, PointF location, float angle, float[] gaps)
        {
            this.def = def; this.location = location;
            this.rotation = angle;
            this.gaps = gaps;
            boundingBox = def.CalcBounds(location, rotation);
        }

        // Determine accurately if this point is within distance of this symbol.
        public override bool HitTest(PointF point, float distance, out float actualDistance)
        {
            float distFromCenter = Util.DistanceF(point, location);
            if (distFromCenter <= distance) 
            {
                actualDistance = distFromCenter;
                return true;
            }
            else 
            {
                actualDistance = 0;
                return false;
            }
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect)
        {
            // TODO: implement hit testing
            return true;
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(GraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            def.Draw(g, location, rotation, gaps, color, renderOpts);
        }
    }

    public class LineSymbol: Symbol
    {
        LineSymDef def;
        public override SymDef Definition { get { return def; }}

        SymPath path;
        public SymPath Path { get { return path; }}

        public LineSymbol(LineSymDef def, SymPath path) 
        {
            path.CheckConstructed();
            this.def = def; this.path = path;
            boundingBox = def.CalcBounds(path);
        }


        // Determine accurately if this point is within distance of a this symbol.
        public override bool HitTest(PointF point, float distance, out float actualDistance)
        {
            PointF temp;

            actualDistance = path.DistanceFromPoint(point, out temp);
            return (actualDistance <= distance);
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect)
        {
            // TODO: implement hit testing
            return true;
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(GraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            def.Draw(g, path, color, renderOpts);
        }
    }

    public class LineTextSymbol: Symbol
    {
        TextSymDef def;
        public override SymDef Definition { get { return def; } }

        SymPath path;
        public SymPath Path { get { return path; } }

        string text;
        public string Text { get { return text; } }

        public LineTextSymbol(TextSymDef def, SymPath path, string text)
        {
            path.CheckConstructed();
            this.def = def; this.path = path; this.text = text;

            boundingBox = def.CalcBounds(path, text);
        }

        
        // Determine accurately if this point is within distance of a this symbol.
        public override bool HitTest(PointF point, float distance, out float actualDistance)
        {
            PointF temp;

            actualDistance = path.DistanceFromPoint(point, out temp);
            return (actualDistance <= distance);
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect)
        {
            // TODO: implement hit testing
            return true;
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(GraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            def.DrawTextOnPath(g, path, text, color, renderOpts);
        }
    }

    public class AreaSymbol: Symbol
    {
        AreaSymDef def;
        public override SymDef Definition { get { return def; }}

        SymPathWithHoles path;
        public SymPathWithHoles Path { get { return path; }}

        float angle;
        public float Angle { get { return angle; }}

        public AreaSymbol(AreaSymDef def, SymPathWithHoles path, float angle) 
        {
            this.def = def; this.path = path; this.angle = angle;
            boundingBox = def.CalcBounds(path);
        }

        // Determine accurately if this point is within distance of a this symbol.
        public override bool HitTest(PointF point, float distance, out float actualDistance)
        {
#if WPF
            throw new NotImplementedException();
#else
            actualDistance = 0;

            using (GraphicsPath grpath = path.GetPath()) 
                return grpath.IsVisible(point, Util.GetHiresGraphics());
#endif
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect)
        {
            // TODO: implement hit testing
            return true;
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(GraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            def.Draw(g, path, color, angle, renderOpts);
        }
    }

    public class TextSymbol: Symbol {
        TextSymDef def;
        public override SymDef Definition { get { return def; }}

        string[] text;
        public string[] Text { get { return text; }}

        string[] wrappedText; // text after being word wrapped.
        public string[] WrappedText { get { return wrappedText; } }

        float[] wrappedLineWidths;  // widths of each line in the wrapped line text.

        PointF location;
        public PointF Location {get { return location; }}
        PointF adjustedLocation;

        SizeF size; // size of actual text.
        public SizeF TextSize { get { return size; }}

        float rotation;  // angle in dgrees symbol is rotated.
        public float Rotation {get { return rotation; }}

        float width;
        public float Width { get { return width; }}

        public TextSymbol(TextSymDef def, string[] text, PointF location, float angle, float width) {
            this.def = def; this.location = location;
            this.rotation = angle; this.width = width;
            this.text = text;

            // For text that is wrapped and center or right aligned, the adjusted location
            // is the location that the text is aligned on.
            adjustedLocation = location;
            if (width > 0) {
                if (def.FontAlignment == TextSymDefAlignment.Right)
                    adjustedLocation = Util.MoveDistance(adjustedLocation, width, angle);
                else if (def.FontAlignment == TextSymDefAlignment.Center)
                    adjustedLocation = Util.MoveDistance(adjustedLocation, width / 2, angle);
            }

            if (width > 0)
                wrappedText = def.BreakLines(text, width, out wrappedLineWidths);
            else
                wrappedText = def.BreakUnwrappedLines(text, out wrappedLineWidths); // no wrapping.

            boundingBox = def.CalcBounds(wrappedText, wrappedLineWidths, adjustedLocation, rotation, width, out size);
        }

        // Determine accurately if this point is within distance of this symbol.
        public override bool HitTest(PointF point, float distance, out float actualDistance) {
            // CONSIDER: handle rotated text better.
            actualDistance = 0;
            if (boundingBox.Contains(point)) 
                return true;
            else
                return false;
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect) {
            // TODO: implement hit testing
            return true;
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(GraphicsTarget g, SymColor color, RenderOptions renderOpts) {
            def.Draw(g, wrappedText, wrappedLineWidths, adjustedLocation, rotation, width, color, renderOpts);
        }

        // Get four points that define the bounds of the text, and the size, and the baseline point.
        public PointF[] GetCornerPoints(out SizeF sizeText, out PointF baselinePoint) {
            PointF[] pts = new PointF[4];
            TextSymDefAlignment fontAlign = def.FontAlignment;

            if (fontAlign == TextSymDefAlignment.Left) {
                pts[0] = adjustedLocation;
                pts[1].X = pts[0].X + size.Width; pts[1].Y = pts[0].Y;
                pts[2].X = pts[0].X + size.Width; pts[2].Y = pts[0].Y - size.Height;
                pts[3].X = pts[0].X; pts[3].Y = pts[0].Y - size.Height;
            }
            else if (fontAlign == TextSymDefAlignment.Right) {
                pts[0] = adjustedLocation;
                pts[1].X = pts[0].X - size.Width; pts[1].Y = pts[0].Y;
                pts[2].X = pts[0].X - size.Width; pts[2].Y = pts[0].Y - size.Height;
                pts[3].X = pts[0].X; pts[3].Y = pts[0].Y - size.Height;
            }
            else {
                Debug.Assert(fontAlign == TextSymDefAlignment.Center);
                pts[0].X = adjustedLocation.X - size.Width / 2; pts[0].Y = adjustedLocation.Y;
                pts[1].X = pts[0].X + size.Width; pts[1].Y = pts[0].Y;
                pts[2].X = pts[0].X + size.Width; pts[2].Y = pts[0].Y - size.Height;
                pts[3].X = pts[0].X; pts[3].Y = pts[0].Y - size.Height;
            }

            
            baselinePoint = new PointF(pts[0].X, pts[0].Y - def.FontAscent);
            sizeText = size;

            if (rotation != 0) {
                Matrix mat = GraphicsUtil.RotationMatrix(rotation, adjustedLocation);
                pts = GraphicsUtil.TransformPoints(pts, mat);
                baselinePoint = GraphicsUtil.TransformPoint(baselinePoint, mat);
            }

            return pts;
        }
    }

    // This is an area object creating by a "ToGraphics" operation -- it defines its own color.
    public class GraphicsAreaSymbol: Symbol
    {
        GraphicsSymDef def;
        public override SymDef Definition { get { return def; } }

        SymPathWithHoles path;
        public SymPathWithHoles Path { get { return path; } }

        SymColor fillColor;
        public SymColor FillColor { get { return fillColor; } }

        public GraphicsAreaSymbol(GraphicsSymDef def, SymPathWithHoles path, SymColor fillColor)
        {
            this.def = def; this.path = path; this.fillColor = fillColor;
            boundingBox = path.BoundingBox;
        }

        // Determine accurately if this point is within distance of a this symbol.
        public override bool HitTest(PointF point, float distance, out float actualDistance)
        {
#if WPF
            throw new NotImplementedException();
#else
            actualDistance = 0;

            using (GraphicsPath grpath = path.GetPath())
                return grpath.IsVisible(point, Util.GetHiresGraphics());
#endif
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect)
        {
            // TODO: implement hit testing
            return true;
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(GraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            if (color == fillColor) {
                path.Fill(g, fillColor.Brush);
            }
        }
    }

    // This is an area object creating by a "image import" operation -- it draws to the image layer below all colors.
    public class ImageAreaSymbol: Symbol
    {
        ImageSymDef def;
        public override SymDef Definition { get { return def; } }

        SymPathWithHoles path;
        public SymPathWithHoles Path { get { return path; } }

        Color fillColor;          // Note: not a SymColor, but a real RGC color!
        public Color FillColor { get { return fillColor; } }

        public ImageAreaSymbol(ImageSymDef def, SymPathWithHoles path, Color fillColor)
        {
            this.def = def; this.path = path; this.fillColor = fillColor;
            boundingBox = path.BoundingBox;
        }

        // Determine accurately if this point is within distance of a this symbol.
        public override bool HitTest(PointF point, float distance, out float actualDistance)
        {
#if WPF
            throw new NotImplementedException();
#else
            actualDistance = 0;

            using (GraphicsPath grpath = path.GetPath())
                return grpath.IsVisible(point, Util.GetHiresGraphics());
#endif
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect)
        {
            // TODO: implement hit testing
            return true;
        }

        // Draw this symbol.
        public override void Draw(GraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            if (color == null) {
                Brush brush = GraphicsUtil.CreateSolidBrush(map.TransformColor(fillColor));
                path.Fill(g, brush);
                GraphicsUtil.DisposeBrush(brush);
            }
        }
    }

    // This is an line object creating by a "ToGraphics" operation -- it defines its own color, line end/join.
    public class GraphicsLineSymbol: Symbol
    {
        GraphicsSymDef def;
        public override SymDef Definition { get { return def; } }

        SymPath path;
        public SymPath Path { get { return path; } }

        SymColor lineColor;
        public SymColor LineColor { get { return lineColor; } }

        float thickness;
        public float Thickness { get { return thickness; } }

        LineStyle lineStyle;
        public LineStyle LineStyle { get { return lineStyle; } }

        public GraphicsLineSymbol(GraphicsSymDef def, SymPath path, SymColor lineColor, float thickness, LineStyle lineStyle)
        {
            this.def = def; this.path = path;
            this.lineColor = lineColor; this.thickness = thickness; this.lineStyle = lineStyle;

            boundingBox = path.BoundingBox;
            boundingBox.Inflate(thickness / 2, thickness / 2);
        }

        // Determine accurately if this point is within distance of a this symbol.
        public override bool HitTest(PointF point, float distance, out float actualDistance)
        {
            PointF temp;

            actualDistance = path.DistanceFromPoint(point, out temp);
            return (actualDistance <= distance);
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect)
        {
            // TODO: implement hit testing
            return true;
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(GraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            if (color == lineColor) {
                Pen pen = GraphicsUtil.CreateSolidPen(lineColor.ColorValue, thickness, lineStyle);
                path.Draw(g, pen);
                GraphicsUtil.DisposePen(pen);
            }
        }
    }

    // This is an line object creating by a import graphcs  -- it draws into the image layer, below all colors.
    public class ImageLineSymbol: Symbol
    {
        ImageSymDef def;
        public override SymDef Definition { get { return def; } }

        SymPath path;
        public SymPath Path { get { return path; } }

        Color lineColor;            // not a SymColor, but a real RGB color!
        public Color LineColor { get { return lineColor; } }

        float thickness;
        public float Thickness { get { return thickness; } }

        LineStyle lineStyle;
        public LineStyle LineStyle { get { return lineStyle; } }

        public ImageLineSymbol(ImageSymDef def, SymPath path, Color lineColor, float thickness, LineStyle lineStyle)
        {
            this.def = def; this.path = path;
            this.lineColor = lineColor; this.thickness = thickness; this.lineStyle = lineStyle;

            boundingBox = path.BoundingBox;
            boundingBox.Inflate(thickness / 2, thickness / 2);
        }

        // Determine accurately if this point is within distance of a this symbol.
        public override bool HitTest(PointF point, float distance, out float actualDistance)
        {
            PointF temp;

            actualDistance = path.DistanceFromPoint(point, out temp);
            return (actualDistance <= distance);
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect)
        {
            // TODO: implement hit testing
            return true;
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(GraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            if (color == null) {
                Pen pen = GraphicsUtil.CreateSolidPen(map.TransformColor(lineColor), thickness, lineStyle);
                path.Draw(g, pen);
                GraphicsUtil.DisposePen(pen);
            }
        }
    }

}
