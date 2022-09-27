/* Copyright (c) 2006-2008, Peter Golde
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
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using Matrix = System.Drawing.Drawing2D.Matrix;
using LineCap = System.Drawing.Drawing2D.LineCap;
using LineJoin = System.Drawing.Drawing2D.LineJoin;

namespace PurplePen.MapModel
{
    using PurplePen.Graphics2D;

    enum GlyphPartKind { Line, Area, Circle, FilledCircle }

    // A filter that is used to enable whether different parts of a glyph should be drawn.
    delegate bool GlyphPartFilter(PointF[] points, PointF center);

    // A glyph is used to define a point symdef or part of a linesymdef or area symdef.
    public class Glyph {
        internal class GlyphPart {
            public GlyphPartKind kind;
            public SymColor color;
            public float lineWidth;
            public float circleDiam;
            public LineCap lineCap; // for kind==Line
            public LineJoin lineJoin; // for kind == Line
            public SymPath path;      // for kind==Line
            public SymPathWithHoles areaPath;  // for kind==Area
            public PointF point;      // for kind==Circle or FilledCircle

            public GlyphPart CopyToMap(Map map)
            {
                GlyphPart newGlyphPart = (GlyphPart) this.MemberwiseClone();
                newGlyphPart.color = map.SymColorFromSymColor(color);
                return newGlyphPart;
            }

            // Core drawing, used for regular and highlight drawing. If highlightBrush is non-null, use that for drawing
            // else do normal drawing.
            // gaps is used only for Circle parts, and is a set of gaps in the circle in degrees.
            // Extra transform is a transform that is applied to the path, and circle diameters, but not to pen widths.
            // It can be null, for no extra transform.
            public void Draw(IGraphicsTarget g, float[] gaps, Matrix extraTransform, object highlightBrush, GlyphPartFilter filter) {
                float circleRadius;

                switch (kind) {
                    case GlyphPartKind.Line:
                        if (lineWidth > 0 && !path.IsZeroLength) {
                            if (filter != null && !filter(path.Points, path.AreaCentroid())) {
                                // Filter has decided not to draw this.
                                break;
                            }

                            object pen;
                            if (highlightBrush != null) {
                                pen = new object();
                                g.CreatePen(pen, highlightBrush, lineWidth, lineCap, lineJoin, GraphicsUtil.MITER_LIMIT);
                            }
                            else {
                                pen = this;
                                if (!g.HasPen(this)) {
                                    g.CreatePen(this, color.ColorValue, lineWidth, lineCap, lineJoin, GraphicsUtil.MITER_LIMIT);
                                }
                            }

                            if (extraTransform != null)
                                path.DrawTransformed(g, pen, extraTransform);
                            else
                                path.Draw(g, pen);
                        }
                        break;

                    case GlyphPartKind.Area:
                        object areaBrush;

                        if (filter != null && !filter(areaPath.MainPath.Points, areaPath.MainPath.AreaCentroid())) {
                            // Filter has decided not to draw this.
                            break;
                        }

                        if (highlightBrush != null)
                            areaBrush = highlightBrush;
                        else
                            areaBrush = color.GetBrushKey(g);

                        if (extraTransform != null)
                            areaPath.FillTransformed(g, areaBrush, extraTransform);
                        else
                            areaPath.Fill(g, areaBrush);
                        break;

                    case GlyphPartKind.Circle:
                        if (lineWidth > 0 && circleDiam > lineWidth) {
                            if (filter != null && !filter(CircleBorderPoints(), point)) {
                                // Filter has decided not to draw this.
                                break;
                            }

                            PointF centerPoint = TransformPoint(point, extraTransform);

                            object pen;
                            if (highlightBrush != null) {
                                pen = new object();
                                GraphicsUtil.CreateSolidPen(g, pen, highlightBrush, lineWidth, LineStyle.Mitered);
                            }
                            else {
                                pen = this;
                                if (!g.HasPen(this)) {
                                    GraphicsUtil.CreateSolidPen(g, this, color.ColorValue, lineWidth, LineStyle.Mitered);
                                }
                            }

                            circleRadius = (TransformDiameter(circleDiam, extraTransform) - lineWidth) / 2;

                            if (circleRadius < 0) {
                                // only happens with transform -- go to filled circle.
                                circleRadius = TransformDiameter(circleDiam, extraTransform) / 2;
                                g.FillEllipse(highlightBrush != null ? highlightBrush : color.GetBrushKey(g), centerPoint, circleRadius, circleRadius);
                            }
                            else if (gaps == null || gaps.Length == 0) {
                                g.DrawEllipse(pen, centerPoint, circleRadius, circleRadius);
                            }
                            else {
                                // There are gaps in the circle. The arcs to draw are from end of one gap to start of the next.
                                for (int i = 1; i < gaps.Length; i += 2) {
                                    float startArc = gaps[i];
                                    float endArc = (i == gaps.Length - 1) ? gaps[0] : gaps[i + 1];
                                    g.DrawArc(pen, centerPoint, circleRadius, startArc, (float) ((endArc - startArc + 360.0) % 360.0));
                                }
                            }
                        }
                        break;

                    case GlyphPartKind.FilledCircle:
                        if (circleDiam > 0) {
                            if (filter != null && !filter(CircleBorderPoints(), point)) {
                                // Filter has decided not to draw this.
                                break;
                            }

                            PointF centerPoint = TransformPoint(point, extraTransform);

                            circleRadius = TransformDiameter(circleDiam, extraTransform) / 2;
                            g.FillEllipse(highlightBrush != null ? highlightBrush : color.GetBrushKey(g), centerPoint, circleRadius, circleRadius);
                        }
                        break;
                }
            }

            // Get 8 points on the border of the circle, used for filtering.
            private PointF[] CircleBorderPoints()
            {
                Debug.Assert(kind == GlyphPartKind.Circle || kind == GlyphPartKind.FilledCircle);

                float halfsqrt2 = (float)Math.Sqrt(2.0) / 2.0F;
                float radius = circleDiam / 2;
                return new PointF[8] {
                    new PointF(point.X, point.Y + radius),
                    new PointF(point.X, point.Y - radius),
                    new PointF(point.X + radius, point.Y),
                    new PointF(point.X - radius, point.Y),
                    new PointF(point.X + radius * halfsqrt2, point.Y + radius * halfsqrt2),
                    new PointF(point.X - radius * halfsqrt2, point.Y + radius * halfsqrt2),
                    new PointF(point.X + radius * halfsqrt2, point.Y - radius * halfsqrt2),
                    new PointF(point.X - radius * halfsqrt2, point.Y - radius * halfsqrt2)
                };
            }


            private PointF TransformPoint(PointF pt, Matrix extraTransform)
            {
                if (extraTransform == null)
                    return pt;
                else
                    return Geometry.TransformPoint(pt, extraTransform);
            }

            private float TransformDiameter(float diameter, Matrix extraTransform)
            {
                if (extraTransform == null)
                    return diameter;
                else {
                    PointF pt = new PointF(diameter, 0);
                    return Math.Abs(Geometry.TransformPoint(pt, extraTransform).X);
                }
            }

            public float Radius {
                get {
                    switch (kind) {
                    case GlyphPartKind.Line: {
                        float width = lineWidth;
                        if (lineJoin == LineJoin.Miter)
                            width *= path.MaxMiter;

                        return path.FindMaxDistance(new PointF(0, 0)) + width / 2;
                    }

                    case GlyphPartKind.Area:
                        return areaPath.FindMaxDistance(new PointF(0,0));

                    case GlyphPartKind.Circle:
                    case GlyphPartKind.FilledCircle:
                        return (float) ((circleDiam / 2) + Geometry.Distance(point, new PointF(0,0)));
                    }

                    Debug.Assert(false);
                    return 0.0F; // can't get here.
                }
            }

            public RectangleF? BoundingBox {
                get {
                    switch (kind) {
                        case GlyphPartKind.Line:
                            if (lineWidth > 0 && !path.IsZeroLength) {

                                float width = lineWidth;
                                if (lineJoin == LineJoin.Miter)
                                    width *= path.MaxMiter;
                                RectangleF bounding = path.BoundingBox;
                                bounding.Inflate(new SizeF(width / 2, width / 2));
                                return bounding;
                            }
                            else {
                                return null;
                            }

                        case GlyphPartKind.Area:
                            return areaPath.BoundingBox;

                        case GlyphPartKind.Circle:
                            if (lineWidth > 0 && circleDiam > lineWidth) {
                                float circleRadius = circleDiam / 2;
                                return new RectangleF(point.X - circleRadius, point.Y - circleRadius, circleDiam, circleDiam);
                            }
                            else {
                                return null;
                            }
                        case GlyphPartKind.FilledCircle:
                            if (circleDiam > 0) {
                                float circleRadius = circleDiam / 2;
                                return new RectangleF(point.X - circleRadius, point.Y - circleRadius, circleDiam, circleDiam);
                            }
                            else {
                                return null;
                            }
                    }

                    Debug.Assert(false);
                    return new RectangleF();  // Can't get here.
                }
            }

            // Hit test options for glyph area.
            static MapHitTestOptions glyphAreaHitTestOptions = new MapHitTestOptions();

            public bool HitTest(PointF pointTest, float distance, out float actualDistance)
            {
                switch (kind) {
                    case GlyphPartKind.Line:
                        return SymbolHelpers.HitTestLine(path, lineWidth, pointTest, distance, out actualDistance);
                    
                    case GlyphPartKind.Area:
                        int holeIndex; // don't care about this.
                        return SymbolHelpers.HitTestArea(areaPath, pointTest, distance, glyphAreaHitTestOptions, out actualDistance, out holeIndex);
                    
                    case GlyphPartKind.FilledCircle: {
                        float circleRadius = circleDiam / 2;
                        float d = Geometry.DistanceF(point, pointTest);
                        actualDistance = Math.Max(0, d - circleRadius);
                        return actualDistance <= distance;
                    }
                    
                    case GlyphPartKind.Circle: {
                        float circleRadius = circleDiam / 2;
                        float d = Geometry.DistanceF(point, pointTest);
                        if (d >= circleRadius)
                            actualDistance = d - circleRadius;
                        else
                            actualDistance = (circleRadius - lineWidth) - d;
                        actualDistance = Math.Max(0, actualDistance);
                        return actualDistance <= distance;
                    }
                    
                    default:
                        actualDistance = float.MaxValue;
                        return false;
                }
            }

            public void FreeGDIObjects() {
            }
        }

        float radius = 0.0F;    // max distance away from 0,0  
        RectangleF boundingBox;  // bounding box of the 
        GlyphPart[] parts; // a sequence of parts.
        bool simple;	   // true if consist of a single, possibly filled, circle at 0,0.
        bool constructed = false;

        // Returns a clone of the parts array (to prevent modification)
        internal GlyphPart[] GetParts() {
            return (GlyphPart[]) parts.Clone();
        }

        public float Radius {
            get {
                CheckConstructed();
                return radius;
            }
        }

        public RectangleF BoundingBox {
            get {
                CheckConstructed();
                return boundingBox;
            }
        }

        internal bool HasColor(SymColor color) {
            Debug.Assert(constructed);
            Debug.Assert(color != null);

            if (color.IsSpecialLayer)
                return false;

            foreach (GlyphPart part in parts) {
                if (part.color == color)
                    return true;
            }
            return false;
        }

        public bool HitTest(PointF point, float distance, out float actualDistance)
        {
            actualDistance = float.MaxValue;
            CheckConstructed();

            // Check bounding box first.
            RectangleF bounds = boundingBox;
            bounds.Inflate(new SizeF(distance, distance));
            if (!bounds.Contains(point)) {
                return false;
            }

            bool hit = false;
            foreach (GlyphPart part in parts) {
                float d;
                if (part.HitTest(point, distance, out d)) {
                    hit = true;
                    if (d < actualDistance)
                        actualDistance = d;
                }
            }

            return hit;
        }

        // Note that "extraTransform" is applied as a transform only to certain parts -- i.e. paths and locations, but not line widths.
        internal void Draw(IGraphicsTarget g, PointF pt, float angle, Matrix extraTransform, float[] gaps, SymColor color, GlyphPartFilter filter, RenderOptions renderOpts)
        {
            Debug.Assert(filter == null || extraTransform == null, "Code doesn't handle extraTransform and filter both non-null"); // could fix by adding extraTransform in filterWithTransform in DrawCore()

            DrawCore(g, pt, angle, extraTransform, gaps, color, null, filter);
        }

        internal void DrawHighlight(IGraphicsTarget g, PointF pt, float angle, Matrix extraTransform, float[] gaps, object highlightBrush)
        {
            DrawCore(g, pt, angle, extraTransform, gaps, null, highlightBrush, null);
        }

        // If "highlightBrush" is non-null, draw with that brush.
        // Note that "extraTransform" is applied as a transform only to certain parts -- i.e. paths and locations, but not line widths.
        private void DrawCore(IGraphicsTarget g, PointF pt, float angle, Matrix extraTransform, float[] gaps, SymColor color, object highlightBrush, GlyphPartFilter filter)
        {
            Debug.Assert(constructed);

            if (simple && gaps == null && extraTransform == null && filter == null)
            {
                if (highlightBrush != null || color == parts[0].color) 
                    DrawSimple(g, pt, highlightBrush);
            }
            else {
                bool transformApplied = false;
                GlyphPartFilter filterWithTransform = null;

                for (int i = 0; i < parts.Length; ++i) {
                    if (highlightBrush != null || parts[i].color == color) {
                        // Establish transformation matrix.
                        if (!transformApplied) {
                            transformApplied = true;

                            Matrix matrix = new Matrix();
                            matrix.Translate(pt.X, pt.Y);
                            matrix.RotateAt(angle, new PointF(0, 0));

                            g.PushTransform(matrix);

                            if (filter != null) {
                                filterWithTransform = (PointF[] points, PointF center) => {
                                    return filter(Geometry.TransformPoints(points, matrix), Geometry.TransformPoint(center, matrix));
                                };
                            }
                        }
                        parts[i].Draw(g, gaps, extraTransform, highlightBrush, filterWithTransform);						
                    }
                }

                if (transformApplied)
                    g.PopTransform();
            }
        }

        void DrawSimple(IGraphicsTarget g, PointF pt, object highlightBrush) {
            Debug.Assert(parts.Length == 1);
            Debug.Assert(parts[0].kind == GlyphPartKind.Circle || parts[0].kind == GlyphPartKind.FilledCircle);
            Debug.Assert(parts[0].point.X == 0.0F && parts[0].point.Y == 0.0F);
            
            if (parts[0].kind == GlyphPartKind.Circle) {
                if (parts[0].lineWidth > 0 && parts[0].circleDiam > 0) {
                    object pen;
                    if (highlightBrush != null) {
                        pen = new object();
                    }
                    else {
                        pen = parts[0];
                    }

                    object brush = (highlightBrush != null) ? highlightBrush : parts[0].color.GetBrushKey(g);
                    if (! g.HasPen(pen))
                        GraphicsUtil.CreateSolidPen(g, pen, brush, 
                                                    parts[0].lineWidth, LineStyle.Mitered);

                    float circleRadius = (parts[0].circleDiam - parts[0].lineWidth) / 2;
                    if (circleRadius < 0) {
                        circleRadius = parts[0].circleDiam / 2;
                        g.FillEllipse(brush, pt, circleRadius, circleRadius);
                    }
                    else {
                        g.DrawEllipse(pen, pt, circleRadius, circleRadius);
                    }
                }
            }
            else { 
                // filled circle
                if (parts[0].circleDiam > 0) {
                    float circleRadius = parts[0].circleDiam / 2;
                    g.FillEllipse((highlightBrush != null) ? highlightBrush : parts[0].color.GetBrushKey(g), 
                                  pt, circleRadius, circleRadius);
                }
            }
        }

        public void AddLine(SymColor color, SymPath path, float width, LineJoin lineJoin, LineCap lineCap) {
            path.CheckConstructed();
            GlyphPart part = new GlyphPart();
            part.kind = GlyphPartKind.Line;
            part.color = color;
            part.lineWidth = width;
            part.path = path;
            part.lineJoin = lineJoin;
            part.lineCap = lineCap;
            AddGlyphPart(part);
        }

        public void AddArea(SymColor color, SymPathWithHoles path) {
            GlyphPart part = new GlyphPart();
            part.kind = GlyphPartKind.Area;
            part.color = color;
            part.areaPath = path;
            AddGlyphPart(part);
        }

        public void AddCircle(SymColor color, PointF center, float width, float diameter) {
            GlyphPart part = new GlyphPart();
            part.kind = GlyphPartKind.Circle;
            part.color = color;
            part.lineWidth = width;
            part.circleDiam = diameter;
            part.point = center;
            AddGlyphPart(part);
        }

        public void AddFilledCircle(SymColor color, PointF center, float diameter) {
            GlyphPart part = new GlyphPart();
            part.kind = GlyphPartKind.FilledCircle;
            part.color = color;
            part.circleDiam = diameter;
            part.point = center;
            AddGlyphPart(part);
        }

        void AddGlyphPart(GlyphPart part) {
            // Add one additional element to the glyph part array.
            int curLen = (parts == null) ? 0 : parts.Length;
            GlyphPart[] newArray = new GlyphPart[curLen + 1];
            for (int i = 0; i < curLen; ++i)
                newArray[i] = parts[i];
            parts = newArray;
            parts[curLen] = part;

            if (curLen == 0 && (part.kind == GlyphPartKind.Circle || part.kind == GlyphPartKind.FilledCircle) &&
                (part.point.X == 0.0F && part.point.Y == 0.0F))
                simple = true;
            else
                simple = false;
        }

        public void ConstructionComplete() {
            if (constructed)
                throw new MapUsageException("Cannot modify a Glyph after ConstructionComplete() has been called");
            constructed = true;

            if (parts == null)
                parts = new GlyphPart[0];

            // Compute radius-- the max distance of this glyph from 0,0 -- and bounding box.
            radius = 0.0F;
            boundingBox = new RectangleF();
            bool first = true;
            for (int i = 0; i < parts.Length; ++i) {
                float partRadius = parts[i].Radius;
                if (partRadius > radius)
                    radius = partRadius;

                RectangleF? partBoundingBox = parts[i].BoundingBox;
                if (partBoundingBox.HasValue) {
                    if (first)
                        boundingBox = partBoundingBox.Value;
                    else
                        boundingBox = RectangleF.Union(boundingBox, partBoundingBox.Value);
                    first = false;
                }
            }
        }

        internal void CheckConstructed() {
            if (! constructed)
                throw new MapUsageException("ConstructionComplete not called on a Glyph before is it used");
        }

        public Glyph CopyToMap(Map newMap)
        {
            Glyph newGlyph = new Glyph();

            foreach (GlyphPart part in parts) {
                newGlyph.AddGlyphPart(part.CopyToMap(newMap));
            }

            newGlyph.ConstructionComplete();
            return newGlyph;
        }

        internal void CheckColors(Map map) {
            foreach (GlyphPart part in parts) {
                if (part.color.ContainingMap != map)
                    throw new MapUsageException("Glyph contains colors that are not in the containing map");
            }
        }

        public void FreeGdiObjects() {
            foreach (GlyphPart part in parts) 
                part.FreeGDIObjects();
        }
    }


}
