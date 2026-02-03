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
using System.Globalization;
using System.Drawing;

namespace PurplePen.MapModel
{
    using PurplePen.Graphics2D;
    using System.Linq;
    public abstract class Symbol
    {
        public abstract SymDef Definition {get;}

        // The containing map
        protected Map map;
        public Map ContainingMap { get {return map;} }

        internal virtual void SetMap(Map newMap)
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

        // Create a clone of this symbol, not attached to the map. Possibly change the symdef
        // or the location.
        public abstract Symbol CloneDetached(SymDef newSymdef, Matrix transform);

        public Symbol CloneDetached()
        {
            return CloneDetached(this.Definition, new Matrix());
        }

        public Symbol CloneDetached(SymDef newSymdef)
        {
            return CloneDetached(newSymdef, new Matrix());
        }

        public Symbol CloneDetached(Matrix transform)
        {
            return CloneDetached(this.Definition, transform);
        }

        // Determine accurately if this point is within distance of a this symbol.
        public abstract bool HitTest(PointF pointTest, float distanceTest, MapHitTestOptions options, out float actualDistance, out int holeIndex);

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public abstract bool MayIntersectRect(RectangleF rect);

        // Draw this color, if used, of this symbol.
        public abstract void Draw(IGraphicsTarget g, SymColor color, RenderOptions renderOpts);

        // Highlight this symbols.
        public abstract void DrawHighlight(IGraphicsTarget g, HighlightOptions options);

        // Get bounding box of the highlight that would be drawn by DrawHighlight.
        public abstract RectangleF HighlightBounds(HighlightOptions options);

        // Ordering for symbols hit testing (points before lines before areas, etc.)
        internal abstract Map.SymbolHitOrder HitOrder { get;  }

        // Is this symbol visible at all?
        public virtual bool IsVisible
        {
            get { return true; }
        }

        // If its a sortable symdef, then sort order of the symbols.
        public virtual int SortOrder
        {
            get { return 0; }
        }
    }

    public abstract class AreaLikeSymbol: Symbol
    {
        protected readonly SymPath mainPath;
        List<HoleSymbol> holes;

        public AreaLikeSymbol(SymPathWithHoles path)
        {
            mainPath = path.MainPath;
            if (path.Holes != null && path.Holes.Length > 0) {
                holes = new List<HoleSymbol>(path.Holes.Length);
                for (int i = 0; i < path.Holes.Length; ++i) {
                    SymPath holePath = path.Holes[i];
                    HoleSymbol hole = new HoleSymbol(this, holePath);
                    holes.Add(hole);
                }
            }
        }

        abstract protected void HolesChanged();

        internal override void SetMap(Map newMap)
        {
            base.SetMap(newMap);
            
            lock (this) {
                if (holes != null) {
                    foreach (HoleSymbol hole in holes)
                        hole.SetMap(newMap);
                }
            }
        }

        public SymPathWithHoles Path {
            get
            {
                if (holes == null) {
                    return new SymPathWithHoles(mainPath, null);
                }
                else {
                    lock (this) {
                        SymPath[] paths = new SymPath[holes.Count];

                        for (int i = 0; i < holes.Count; ++i) {
                            paths[i] = holes[i].HolePath;
                        }

                        return new SymPathWithHoles(mainPath, paths);
                    }
                }
            }
        }

        public bool IsAttached(HoleSymbol hole)
        {
            lock (this) {
                Debug.Assert(hole.SymbolWithHole == this);
                return (holes != null && holes.Contains(hole));
            }
        }

        public int HoleCount
        {
            get
            {
                lock(this) {
                    if (holes == null)
                        return 0;
                    else
                        return holes.Count;
                }
            }
        }

        public HoleSymbol GetHole(int index)
        {
            lock (this) {
                if (holes == null || index < 0 || index >= holes.Count)
                    return null;
                else
                    return holes[index];
            }
        }

        internal void AddHole(HoleSymbol hole)
        {
            lock (this) {
                if (holes == null)
                    holes = new List<HoleSymbol>();
                Debug.Assert(hole.SymbolWithHole == this);
                Debug.Assert(hole.ContainingMap == this.ContainingMap);
                holes.Add(hole);
                HolesChanged();
            }
        }

        internal void RemoveHole(HoleSymbol hole)
        {
            lock (this) {
                Debug.Assert(holes.Contains(hole));
                Debug.Assert(hole.SymbolWithHole == this);
                holes.Remove(hole);
                HolesChanged();
            }
        }

        public abstract AreaLikeSymbol CloneDetached(SymPathWithHoles newPath);
    }

    // Base class for all symbols that are like a line.
    public abstract class LineLikeSymbol: Symbol
    {
        public abstract SymPath Path { get; }

        public abstract LineLikeSymbol CloneDetached(SymPath newPath);
    }

    // Base class for all symbols that are like a rectangle.
    public abstract class RectLikeSymbol: Symbol
    {
        // Must always be a 5-point rectangle path, as created by Path.CreateRectanglePath.
        public abstract SymPath Path { get; }

        public abstract bool CanRotate { get; }

        public abstract RectLikeSymbol CloneDetached(SymPath newPath);
    }

    // Interface because some text symbols are also rect-like symbols.
    public interface ITextLikeSymbol
    {
        string[] Text {get; }

        TextSymDef.InsertionPointLocation FindInsertionPoint(TextCoord textCoord);

        TextCoord FindClosestInsertionPoint(PointF point);

        ITextLikeSymbol CloneDetached(string[] newText);
    }

    public interface IGraphicsSymbol
    {
        bool HasColor(SymColor color);
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

        public override Symbol CloneDetached(SymDef newSymdef, Matrix transform)
        {
            float newRotation = rotation;
            if (def.AllowRotation)
                newRotation = Geometry.TransformAngle(rotation, transform);

            return new PointSymbol((PointSymDef)newSymdef, Geometry.TransformPoint(location, transform), newRotation, gaps);
        }

        // Determine accurately if this point is within distance of this symbol.
        public override bool HitTest(PointF pointTest, float distanceTest, MapHitTestOptions options, out float actualDistance, out int holeIndex)
        {
            holeIndex = -1;

            PointF glyphRelativePoint = new PointF(pointTest.X - location.X, pointTest.Y - location.Y);

            if (rotation != 0) {
                Matrix transform = new Matrix();
                transform.RotateAt(- rotation, new PointF(0,0));
                glyphRelativePoint = Geometry.TransformPoint(glyphRelativePoint, transform);
            }

            return def.Glyph.HitTest(glyphRelativePoint, distanceTest, out actualDistance);
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect)
        {
            // TODO: implement hit testing
            return true;
        }

        internal override Map.SymbolHitOrder HitOrder
        {
            get { return Map.SymbolHitOrder.Point; }
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(IGraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            def.Draw(g, location, rotation, gaps, color, renderOpts);
        }

        // Highlight the symbol.
        public override void DrawHighlight(IGraphicsTarget g, HighlightOptions options)
        {
            def.DrawHighlight(g, location, rotation, gaps, options);
        }

        public override RectangleF HighlightBounds(HighlightOptions options)
        {
            return def.HighlightBounds(location, options);
        }
    }

    public class LineSymbol: LineLikeSymbol
    {
        LineLikeSymDef def;
        public override SymDef Definition { get { return def; }}

        SymPath path;
        public override SymPath Path { get { return path; }}

        public LineSymbol(LineLikeSymDef def, SymPath path) 
        {
            path.CheckConstructed();
            this.def = def; this.path = path;
            boundingBox = def.CalcBounds(path);
        }

        public override Symbol CloneDetached(SymDef newSymdef, Matrix transform)
        {
            return new LineSymbol((LineSymDef)newSymdef, path.Transform(transform));
        }

        public override LineLikeSymbol CloneDetached(SymPath newPath)
        {
            return new LineSymbol(def, newPath);
        }

        // Determine accurately if this point is within distance of a this symbol.
        public override bool HitTest(PointF pointTest, float distanceTest, MapHitTestOptions options, out float actualDistance, out int holeIndex)
        {
            holeIndex = -1;

            return SymbolHelpers.HitTestLine(path, def.HighlightThickness, pointTest, distanceTest, out actualDistance);
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate. Always called after bounding
        // box test, so no need to test that again.
        public override bool MayIntersectRect(RectangleF rect)
        {
            return SymbolHelpers.LineMayIntersectRect(path, def.MaxThickness, rect);
        }

        internal override Map.SymbolHitOrder HitOrder
        {
            get { return Map.SymbolHitOrder.Line; }
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(IGraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            def.Draw(g, path, color, renderOpts);
        }

        // Highlight the symbol.
        public override void DrawHighlight(IGraphicsTarget g, HighlightOptions options)
        {
            def.DrawHighlight(g, path, options);
        }

        public override RectangleF HighlightBounds(HighlightOptions options)
        {
            if (options.style == HighlightStyle.LowFidelity || def.HighlightThickness < SymbolHelpers.MinimumLineWidth(options))
                return SymbolHelpers.LineHighlightBounds(path, def.HighlightThickness, options);
            else
                return this.BoundingBox;
        }

    }

    public class LineTextSymbol: LineLikeSymbol, ITextLikeSymbol
    {
        readonly TextSymDef def;
        public override SymDef Definition { get { return def; } }

        readonly SymPath path;
        public override SymPath Path { get { return path; } }

        readonly string text;
        public string Text { get { return text; } }
        string[] ITextLikeSymbol.Text {get { return new string[1] {text}; }}

        readonly TextSymDefHorizAlignment horizAlignment = TextSymDefHorizAlignment.Default;
        public TextSymDefHorizAlignment HorizontalAlignment { get { return horizAlignment; } }

        readonly TextSymDefVertAlignment vertAlignment = TextSymDefVertAlignment.Default;
        public TextSymDefVertAlignment VerticalAlignment { get { return vertAlignment; } }


        public LineTextSymbol(TextSymDef def, SymPath path, string text, TextSymDefHorizAlignment horizAlignment, TextSymDefVertAlignment vertAlignment)
        {
            path.CheckConstructed();
            this.def = def; this.path = path; this.text = text;
            this.horizAlignment = horizAlignment;
            this.vertAlignment = vertAlignment;

            boundingBox = def.CalcBounds(path, text);
        }

        public override Symbol CloneDetached(SymDef newSymdef, Matrix transform)
        {
            return new LineTextSymbol((TextSymDef)newSymdef, path.Transform(transform), text, horizAlignment, vertAlignment);
        }

        public override LineLikeSymbol CloneDetached(SymPath newPath)
        {
            return new LineTextSymbol(def, newPath, text, horizAlignment, vertAlignment);
        }

        public ITextLikeSymbol CloneDetached(string[] newText)
        {
            return new LineTextSymbol(def, path, newText[0], horizAlignment, vertAlignment);
        }

        // Determine accurately if this point is within distance of a this symbol.
        public override bool HitTest(PointF pointTest, float distanceTest, MapHitTestOptions options, out float actualDistance, out int holeIndex)
        {
            holeIndex = -1;

            return def.HitTestTextOnPath(path, text, pointTest, distanceTest, horizAlignment, vertAlignment, out actualDistance);
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect)
        {
            return true;
        }

        internal override Map.SymbolHitOrder HitOrder
        {
            get { return Map.SymbolHitOrder.Text; }
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(IGraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            def.DrawTextOnPath(g, path, text, color, horizAlignment, vertAlignment, renderOpts);
        }

        // Highlight the symbol.
        public override void DrawHighlight(IGraphicsTarget g, HighlightOptions options)
        {
            SymbolHelpers.DrawLineHighlight(g, map, path, (float)(options.logicalPixelSize * 3), LineJoinMode.Miter, LineCapMode.Flat, options);
        }

        public override RectangleF HighlightBounds(HighlightOptions options)
        {
            return SymbolHelpers.LineHighlightBounds(path, (float)(options.logicalPixelSize * 3), options);
        }

        public TextSymDef.InsertionPointLocation FindInsertionPoint(TextCoord textCoord)
        {
            if (textCoord.Line != 0)
                return null;

            return def.FindInsertionPointOnPath(path, text, textCoord.Col, horizAlignment, vertAlignment);
        }

        public TextCoord FindClosestInsertionPoint(PointF point)
        {
            return SymbolHelpers.FindClosestInsertionPoint(this, point);
        }
    }

    public class AreaSymbol: AreaLikeSymbol
    {
        readonly AreaLikeSymDef def;
        public override SymDef Definition { get { return def; }}

        readonly float angle;
        public float Angle { get { return angle; }}

        readonly PointF rotationCenter;
        public PointF RotationCenter { get { return rotationCenter; } }

        public AreaSymbol(AreaLikeSymDef def, SymPathWithHoles path, float angle, PointF rotationCenter) :
            base(path)
        {
            this.def = def; this.angle = angle; this.rotationCenter = rotationCenter;
            boundingBox = def.CalcBounds(path);
        }

        protected override void HolesChanged()
        {
            boundingBox = def.CalcBounds(Path);
        }

        public override Symbol CloneDetached(SymDef newSymdef, Matrix transform)
        {
            // Do not transform the pattern angle on rotation.
            return new AreaSymbol((AreaSymDef)newSymdef, Path.Transform(transform), angle, rotationCenter);
        }

        public override AreaLikeSymbol CloneDetached(SymPathWithHoles newPath)
        {
            return new AreaSymbol(def, newPath, angle, rotationCenter);
        }

        // Determine accurately if this point is within distance of this symbol.
        public override bool HitTest(PointF pointTest, float distanceTest, MapHitTestOptions options, out float actualDistance, out int holeIndex)
        {
            bool hitArea = SymbolHelpers.HitTestArea(Path, pointTest, distanceTest, options, out actualDistance, out holeIndex);
            float borderHighlightThickness = def.BorderHighlightThickness;

            if (borderHighlightThickness != 0) {
                // Hit test on the border also.
                float borderDistance;
                bool hitBorder = SymbolHelpers.HitTestLine(Path.MainPath, borderHighlightThickness, pointTest, distanceTest, out borderDistance);
                if (hitBorder && !hitArea) {
                    actualDistance = borderDistance;
                    return true;
                }
                else if (hitArea && !hitBorder) {
                    return true;
                }
                else {
                    actualDistance = Math.Min(actualDistance, borderDistance);
                    return hitArea || hitBorder;
                }
            }
            else {
                return hitArea;
            }
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect)
        {
            return true;
        }

        internal override Map.SymbolHitOrder HitOrder
        {
            get { return Map.SymbolHitOrder.Area; }
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(IGraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            def.Draw(g, Path, color, angle, rotationCenter, renderOpts);
        }

        // Highlight the symbol.
        public override void DrawHighlight(IGraphicsTarget g, HighlightOptions options)
        {
            def.DrawHighlight(g, Path, options);
        }

        public override RectangleF HighlightBounds(HighlightOptions options)
        {
            return def.HighlightBounds(Path, boundingBox, options);
        }

    }

    public class TextSymbol: RectLikeSymbol, ITextLikeSymbol {
        TextSymDef def;
        public override SymDef Definition { get { return def; }}

        string[] text;
        public string[] Text { get { return text; }}

        string[] wrappedText; // text after being word wrapped.
        public string[] WrappedText { get { return wrappedText; } }

        float[] wrappedLineWidths;  // widths of each line in the wrapped line text.

        TextCoordMapper coordMapper;  // The mapping between text and wrappedText.

        bool didWrapAtLeastOneLine; // at least one line was word wrapped.
        public bool DidWrapAtLeastOneLine { get { return didWrapAtLeastOneLine; }}

        PointF location;
        public PointF Location {get { return location; }}

        SizeF size; // size of actual text.
        public SizeF TextSize { get { return size; }}

        float rotation;  // angle in dgrees symbol is rotated.
        public float Rotation {get { return rotation; }}

        float width;
        public float Width { get { return width; }}

        readonly TextSymDefHorizAlignment horizAlignment = TextSymDefHorizAlignment.Default;
        public TextSymDefHorizAlignment HorizontalAlignment { get { return horizAlignment; } }

        readonly TextSymDefVertAlignment vertAlignment = TextSymDefVertAlignment.Default;
        public TextSymDefVertAlignment VerticalAlignment { get { return vertAlignment; } }

        public TextSymbol(TextSymDef def, string[] text, PointF location, float angle, float width, TextSymDefHorizAlignment horizAlignment, TextSymDefVertAlignment vertAlignment) {
            this.def = def; this.location = location;
            this.rotation = angle; this.width = width;
            this.text = text;
            this.horizAlignment = horizAlignment;
            this.vertAlignment = vertAlignment;

            // We break the text into lines seperated by paragraph marks. We also ignore an initial
            // newline for OCAD compatibility. We can't just remove that on import, or else roundtripping
            // an object with two initial newlines wouldn't work.
            if (width > 0) {
                wrappedText = def.BreakLines(text, width, horizAlignment, out coordMapper, out wrappedLineWidths, out didWrapAtLeastOneLine);
            }
            else {
                wrappedText = def.BreakUnwrappedLines(text, horizAlignment, out coordMapper, out wrappedLineWidths); // no wrapping.
                didWrapAtLeastOneLine = false;
            }

            boundingBox = def.CalcBounds(wrappedText, wrappedLineWidths, location, rotation, width, horizAlignment, vertAlignment, out size);
        }

        public override SymPath Path
        {
            get {
                // Location is the top-left. But in our coordinate system, rectangles are reversed along the Y access.
                return GetHighlightPath();
            }
        }

        public override bool CanRotate
        {
            get {
                return true;
            }
        }

        public override Symbol CloneDetached(SymDef newSymdef, Matrix transform)
        {
            return new TextSymbol((TextSymDef)newSymdef, text, 
                Geometry.TransformPoint(location, transform), Geometry.TransformAngle(rotation, transform), Geometry.TransformDistance(width, transform),
                horizAlignment, vertAlignment);
        }

        public ITextLikeSymbol CloneDetached(string[] newText)
        {
            return new TextSymbol(def, newText, location, rotation, width, horizAlignment, vertAlignment);
        }

        public override RectLikeSymbol CloneDetached(SymPath newPath)
        {
            // Provisionally create with the current location and formatted to the desired width.
            TextSymbol newSymbol = CloneDetachedFormatted(newPath);
            if (! newSymbol.DidWrapAtLeastOneLine) {
                // If wrapping didn't occur, go unformatted instead.
                newSymbol = CloneDetachedUnformatted(newPath);
            }

            return newSymbol;
        }

        TextSymbol CloneDetachedFormatted(SymPath newPath)
        {
            PointF newLocation = newPath.Points[3];
            float newWidth = Geometry.DistanceF(newPath.Points[0], newPath.Points[1]);
            float newRotation = Geometry.Angle(newPath.Points[0], newPath.Points[1]);

            // If we are changing from unformatted to formatted, add a little slop due to rounding errors
            // that occur doing formatted. Otherwise a line will change from unformatted to formatted even if
            // the width didn't change.
            if (width == 0)
                newWidth += 0.01F;

            newLocation = Geometry.MoveDistance(newLocation, def.VertOffset(this.VerticalAlignment, this.wrappedText), newRotation - 90);

            return new TextSymbol(def, text, newLocation, newRotation, newWidth, horizAlignment, vertAlignment);
        }

        TextSymbol CloneDetachedUnformatted(SymPath newPath)
        {
            PointF newLocation;

            if (def.FontAlignment == TextSymDefHorizAlignment.Right)
                newLocation = newPath.Points[2];
            else if (def.FontAlignment == TextSymDefHorizAlignment.Center)
                newLocation = Geometry.MidPoint(newPath.Points[2], newPath.Points[3]);
            else
                newLocation = newPath.Points[3];

            float newRotation = Geometry.Angle(newPath.Points[0], newPath.Points[1]);

            newLocation = Geometry.MoveDistance(newLocation, def.VertOffset(this.VerticalAlignment, this.wrappedText), newRotation - 90);

            return new TextSymbol(def, text, newLocation, newRotation, 0, horizAlignment, vertAlignment);
        }

        // Determine accurately if this point is within distance of this symbol.
        public override bool HitTest(PointF pointTest, float distanceTest, MapHitTestOptions options, out float actualDistance, out int holeIndex)
        {
            holeIndex = -1;

            return def.HitTest(wrappedText, wrappedLineWidths, location, rotation, width, horizAlignment, vertAlignment, pointTest, distanceTest, out actualDistance);
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect) {
            // TODO: implement hit testing
            return true;
        }

        internal override Map.SymbolHitOrder HitOrder
        {
            get { return Map.SymbolHitOrder.Text; }
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(IGraphicsTarget g, SymColor color, RenderOptions renderOpts) {
            def.Draw(g, wrappedText, wrappedLineWidths, location, rotation, width, color, horizAlignment, vertAlignment, renderOpts);
        }

        // Highlight the symbol.
        public override void DrawHighlight(IGraphicsTarget g, HighlightOptions options)
        {
            SymPath path = def.GetHighlightPath(wrappedText, wrappedLineWidths, location, horizAlignment, vertAlignment, rotation, width);
            SymbolHelpers.DrawAreaHighlight(g, map, new SymPathWithHoles(path, null), options);
        }

        SymPath GetHighlightPath()
        {
            return def.GetHighlightPath(wrappedText, wrappedLineWidths, location, horizAlignment, vertAlignment, rotation, width);
        }

        public override RectangleF HighlightBounds(HighlightOptions options)
        {
            SymPath path = GetHighlightPath();
            return SymbolHelpers.AreaHighlightBounds(new SymPathWithHoles(path, null), options);
        }

        // Given coordinates in the unwrapped text, get the points corresponding to three points on the insertion line
        // for the given location in the text (given by textCoord).
        public TextSymDef.InsertionPointLocation FindInsertionPoint(TextCoord textCoord)
        {
            if (textCoord.Line < 0 || textCoord.Line >= text.Length || textCoord.Col < 0 || textCoord.Col > text[textCoord.Line].Length) {
                return null;
            }

            // First, convert textCoord to wrapped text coordinates.
            TextCoord wrappedTextCoord = coordMapper.WrappedFromUnwrapped(textCoord, text, this.wrappedText);

            // Need at least one string.
            float[] wrappedLineWidths = this.wrappedLineWidths;
            string[] wrappedText = this.wrappedText;
            if (wrappedText == null || wrappedText.Length == 0) {
                wrappedText = new string[1] { "" };
                wrappedLineWidths = new float[1] { 0 };
            }

            return def.FindInsertionPoint(wrappedTextCoord, wrappedText, wrappedLineWidths, location, rotation, width, horizAlignment, vertAlignment);
        }

        // Find where in the text is closest to a point.
        // Note: currently this implementation is very very inefficient. It could be made much more
        // efficient, but it is unclear if it needs to be.
        public TextCoord FindClosestInsertionPoint(PointF point)
        {
            return SymbolHelpers.FindClosestInsertionPoint(this, point);

        }

        // Get four points that define the bounds of the text, and the size, and the baseline point.
        public PointF[] GetCornerPoints(out SizeF sizeText, out PointF baselinePoint) {
            PointF[] pts = new PointF[4];
            TextSymDefHorizAlignment fontAlign = def.FontAlignment;

            if (fontAlign == TextSymDefHorizAlignment.Left || fontAlign == TextSymDefHorizAlignment.Justified) {
                pts[0] = location;
                pts[1].X = pts[0].X + size.Width; pts[1].Y = pts[0].Y;
                pts[2].X = pts[0].X + size.Width; pts[2].Y = pts[0].Y - size.Height;
                pts[3].X = pts[0].X; pts[3].Y = pts[0].Y - size.Height;
            }
            else if (fontAlign == TextSymDefHorizAlignment.Right) {
                pts[0] = location;
                pts[1].X = pts[0].X - size.Width; pts[1].Y = pts[0].Y;
                pts[2].X = pts[0].X - size.Width; pts[2].Y = pts[0].Y - size.Height;
                pts[3].X = pts[0].X; pts[3].Y = pts[0].Y - size.Height;
            }
            else {
                Debug.Assert(fontAlign == TextSymDefHorizAlignment.Center);
                pts[0].X = location.X - size.Width / 2; pts[0].Y = location.Y;
                pts[1].X = pts[0].X + size.Width; pts[1].Y = pts[0].Y;
                pts[2].X = pts[0].X + size.Width; pts[2].Y = pts[0].Y - size.Height;
                pts[3].X = pts[0].X; pts[3].Y = pts[0].Y - size.Height;
            }

            
            baselinePoint = new PointF(pts[0].X, pts[0].Y - def.FontAscent);
            sizeText = size;

            if (rotation != 0) {
                Matrix mat = new Matrix();
                mat.RotateAt(rotation, location);
                pts = Geometry.TransformPoints(pts, mat);
                baselinePoint = Geometry.TransformPoint(baselinePoint, mat);
            }

            return pts;
        }
    }

    // This is an area object creating by a "ToGraphics" operation -- it defines its own color.
    public class GraphicsAreaSymbol: AreaLikeSymbol, IGraphicsSymbol
    {
        readonly GraphicsSymDef def;
        public override SymDef Definition { get { return def; } }

        readonly SymColor fillColor;
        public SymColor FillColor { get { return fillColor; } }

        public GraphicsAreaSymbol(GraphicsSymDef def, SymPathWithHoles path, SymColor fillColor)
            :base(path)
        {
            this.def = def; this.fillColor = fillColor;
            boundingBox = path.BoundingBox;
        }
        protected override void HolesChanged()
        {
            boundingBox = Path.BoundingBox;
        }


        public override Symbol CloneDetached(SymDef newSymdef, Matrix transform)
        {
            return new GraphicsAreaSymbol((GraphicsSymDef)newSymdef, Path.Transform(transform), this.fillColor);
        }

        public override AreaLikeSymbol CloneDetached(SymPathWithHoles newPath)
        {
            return new GraphicsAreaSymbol(def, newPath, fillColor);
        }

        // Determine accurately if this point is within distance of a this symbol.
        public override bool HitTest(PointF pointTest, float distanceTest, MapHitTestOptions options, out float actualDistance, out int holeIndex)
        {
            return SymbolHelpers.HitTestArea(Path, pointTest, distanceTest, options, out actualDistance, out holeIndex);
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect)
        {
            // TODO: implement hit testing
            return true;
        }

        internal override Map.SymbolHitOrder HitOrder
        {
            get { return Map.SymbolHitOrder.Area; }
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(IGraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            if (fillColor != null && color == fillColor) {
                Path.Fill(g, fillColor.GetBrushKey(g));
            }
        }


        // Highlight the symbol.
        public override void DrawHighlight(IGraphicsTarget g, HighlightOptions options)
        {
            SymbolHelpers.DrawAreaHighlight(g, map, Path, options);
        }

        public override RectangleF HighlightBounds(HighlightOptions options)
        {
            return SymbolHelpers.AreaHighlightBounds(Path, options);
        }


        public bool HasColor(SymColor color)
        {
            return fillColor != null && color == fillColor;
        }
    }

    // This is an area object creating by a "image import" operation -- it draws to the image layer below all colors.
    public class ImageAreaSymbol: AreaLikeSymbol
    {
        ImageSymDef def;
        public override SymDef Definition { get { return def; } }

        readonly CmykColor fillColor;          // Note: not a SymColor, but a real color!
        public CmykColor FillColor { get { return fillColor; } }

        private readonly bool isVisible;
        public override bool IsVisible
        {
            get { return isVisible; }
        }

        private readonly int sortOrder;
        public override int SortOrder
        {
            get { return sortOrder; }
        }

        public ImageAreaSymbol(ImageSymDef def, SymPathWithHoles path, CmykColor fillColor, bool isVisible, int sortOrder)
            :base(path)
        {
            this.def = def; this.fillColor = fillColor;
            this.isVisible = isVisible; this.sortOrder = sortOrder;
            boundingBox = path.BoundingBox;
        }

        protected override void HolesChanged()
        {
            boundingBox = Path.BoundingBox;
        }

        public override Symbol CloneDetached(SymDef newSymdef, Matrix transform)
        {
            return new ImageAreaSymbol((ImageSymDef)newSymdef, Path.Transform(transform), fillColor, isVisible, sortOrder);
        }

        public override AreaLikeSymbol CloneDetached(SymPathWithHoles newPath)
        {
            return new ImageAreaSymbol(def, newPath, fillColor, isVisible, sortOrder);
        }

        // Determine accurately if this point is within distance of a this symbol.
        public override bool HitTest(PointF pointTest, float distanceTest, MapHitTestOptions options, out float actualDistance, out int holeIndex)
        {
            return SymbolHelpers.HitTestArea(Path, pointTest, distanceTest, options, out actualDistance, out holeIndex);
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate. Always called after test on bounding rectangle,
        // so no need to reproduce that.
        public override bool MayIntersectRect(RectangleF rect)
        {
            return true;
        }

        internal override Map.SymbolHitOrder HitOrder
        {
            get { return Map.SymbolHitOrder.Area; }
        }

        // Draw this symbol.
        public override void Draw(IGraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            if (def.HasColor(color)) {
                if (!g.HasBrush(this))
                    g.CreateSolidBrush(this, map.TransformColor(fillColor));

                Path.Fill(g, this);
            }
        }


        // Highlight the symbol.
        public override void DrawHighlight(IGraphicsTarget g, HighlightOptions options)
        {
            SymbolHelpers.DrawAreaHighlight(g, map, Path, options);
        }

       public override RectangleF HighlightBounds(HighlightOptions options)
        {
            return SymbolHelpers.AreaHighlightBounds(Path, options);
        }

    }

    // This is an line object creating by a "ToGraphics" operation -- it defines its own color, line end/join.
    public class GraphicsLineSymbol: LineLikeSymbol, IGraphicsSymbol
    {
        GraphicsSymDef def;
        public override SymDef Definition { get { return def; } }

        SymPath path;
        public override SymPath Path { get { return path; } }

        SymColor lineColor;
        public SymColor LineColor { get { return lineColor; } }

        float thickness;
        public float Thickness { get { return thickness; } }

        LineJoinMode lineJoin;
        public LineJoinMode LineJoinMode { get { return lineJoin; } }

        LineCapMode lineCap;
        public LineCapMode LineCapMode { get { return lineCap; } }

        public GraphicsLineSymbol(GraphicsSymDef def, SymPath path, SymColor lineColor, float thickness, LineJoinMode lineJoin, LineCapMode lineCap)
        {
            this.def = def; this.path = path;
            this.lineColor = lineColor; this.thickness = thickness; this.lineJoin = lineJoin; this.lineCap = lineCap;

            boundingBox = path.BoundingBox;
            boundingBox.Inflate(thickness / 2, thickness / 2);
        }

        public override Symbol CloneDetached(SymDef newSymdef, Matrix transform)
        {
            return new GraphicsLineSymbol((GraphicsSymDef)newSymdef, path.Transform(transform), lineColor, thickness, lineJoin, lineCap);
        }

        public override LineLikeSymbol CloneDetached(SymPath newPath)
        {
            return new GraphicsLineSymbol(def, newPath, lineColor, thickness, lineJoin, lineCap);
        }

        // Determine accurately if this point is within distance of a this symbol.
        public override bool HitTest(PointF pointTest, float distanceTest, MapHitTestOptions options, out float actualDistance, out int holeIndex)
        {
            holeIndex = -1;

            return SymbolHelpers.HitTestLine(path, thickness, pointTest, distanceTest, out actualDistance);
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect)
        {
            return SymbolHelpers.LineMayIntersectRect(path, thickness, rect);
        }

        internal override Map.SymbolHitOrder HitOrder
        {
            get { return Map.SymbolHitOrder.Line; }
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(IGraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            if (lineColor != null && color == lineColor) {
                if (! g.HasPen(this))
                    g.CreatePen(this, lineColor.ColorValue, thickness, lineCap, lineJoin, GraphicsUtil.MITER_LIMIT);

                path.Draw(g, this);
            }
        }

        // Highlight the symbol.
        public override void DrawHighlight(IGraphicsTarget g, HighlightOptions options)
        {
            SymbolHelpers.DrawLineHighlight(g, map, path, thickness, lineJoin, lineCap, options);
        }

        public override RectangleF HighlightBounds(HighlightOptions options)
        {
            return SymbolHelpers.LineHighlightBounds(path, thickness, options);
        }

        public bool HasColor(SymColor color)
        {
            return (lineColor != null && color == lineColor);
        }
    }

    // This is an line object creating by a import graphcs  -- it draws into the image layer, below all colors.
    public class ImageLineSymbol: LineLikeSymbol
    {
        ImageSymDef def;
        public override SymDef Definition { get { return def; } }

        private readonly bool isVisible;
        public override bool IsVisible
        {
            get { return isVisible; }
        }

        private readonly int sortOrder;
        public override int SortOrder
        {
            get { return sortOrder; }
        }

        SymPath path;
        public override SymPath Path { get { return path; } }

        CmykColor lineColor;            // not a SymColor, but a real RGB color!
        public CmykColor LineColor { get { return lineColor; } }

        float thickness;
        public float Thickness { get { return thickness; } }

        LineJoinMode lineJoin;
        public LineJoinMode LineJoinMode { get { return lineJoin; } }

        LineCapMode lineCap;
        public LineCapMode LineCapMode { get { return lineCap; } }

        public ImageLineSymbol(ImageSymDef def, SymPath path, CmykColor lineColor, float thickness, LineJoinMode lineJoin, LineCapMode lineCap, bool isVisible, int sortOrder)
        {
            this.def = def; this.path = path;
            this.lineColor = lineColor; this.thickness = thickness; this.lineJoin = lineJoin; this.lineCap = lineCap;
            this.isVisible = isVisible; this.sortOrder = sortOrder;

            boundingBox = path.BoundingBox;
            boundingBox.Inflate(thickness / 2, thickness / 2);
        }

        public override Symbol CloneDetached(SymDef newSymdef, Matrix transform)
        {
            return new ImageLineSymbol((ImageSymDef)newSymdef, path.Transform(transform), lineColor, thickness, lineJoin, lineCap, isVisible, sortOrder);
        }

        public override LineLikeSymbol CloneDetached(SymPath newPath)
        {
            return new ImageLineSymbol(def, newPath, lineColor, thickness, lineJoin, lineCap, isVisible, sortOrder);
        }

        // Determine accurately if this point is within distance of a this symbol.
        public override bool HitTest(PointF pointTest, float distanceTest, MapHitTestOptions options, out float actualDistance, out int holeIndex)
        {
            holeIndex = -1;

            return SymbolHelpers.HitTestLine(path, thickness, pointTest, distanceTest, out actualDistance);
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect)
        {
            return SymbolHelpers.LineMayIntersectRect(path, thickness, rect);
        }

        internal override Map.SymbolHitOrder HitOrder
        {
            get { return Map.SymbolHitOrder.Line; }
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(IGraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            if (def.HasColor(color)) {
                if (! g.HasPen(this))
                    g.CreatePen(this, map.TransformColor(lineColor), thickness, lineCap, lineJoin, GraphicsUtil.MITER_LIMIT);

                path.Draw(g, this);
            }
        }

        // Highlight the symbol.
        public override void DrawHighlight(IGraphicsTarget g, HighlightOptions options)
        {
            SymbolHelpers.DrawLineHighlight(g, map, path, thickness, lineJoin, lineCap, options);
        }

        public override RectangleF HighlightBounds(HighlightOptions options)
        {
            return SymbolHelpers.LineHighlightBounds(path, thickness, options);
        }
    }

    // This is an text object in the layout layer.
    public class ImageTextSymbol : RectLikeSymbol, ITextLikeSymbol
    {
        ImageSymDef def;
        public override SymDef Definition { get { return def; } }

        string[] unwrappedText;
        public string[] Text { get { return unwrappedText; } }

        string[] wrappedText;
        public string[] WrappedText { get { return wrappedText; } }

        float[] lineWidths;  // widths of each line in the wrapped line text.

        TextCoordMapper coordMapper;  // The mapping between text and wrappedText.

        CmykColor textColor;            // not a SymColor, but a real RGB color!
        public CmykColor TextColor { get { return textColor; } }

        PointF location;
        public PointF Location { get { return location; } }

        SizeF size; // size of actual text.
        public SizeF TextSize { get { return size; } }

        float rotation;
        public float Rotation { get { return rotation; } }

        float width;
        public float Width { get { return width;  } }

        private readonly bool isVisible;
        public override bool IsVisible
        {
            get { return isVisible; }
        }

        private readonly int sortOrder;
        public override int SortOrder
        {
            get { return sortOrder; }
        }

        private readonly string fontName;
        public string FontName
        {
            get { return fontName; }
        }

        private bool didWrapAtLeastOneLine;
        public bool DidWrapAtLeastOneLine { get { return didWrapAtLeastOneLine; } }


        public readonly float FontAscent, FontDescent, FontSize;
        private readonly float lineSpacing;

        public ImageTextSymbol(ImageSymDef def, string[] text, CmykColor textColor, string fontName, float fontSize, PointF location, float angle, float width, bool isVisible, int sortOrder)
        {
            this.def = def;
            this.FontSize = fontSize;
            this.fontName = fontName;
            this.textColor = textColor;
            this.unwrappedText = text; this.location = location; this.rotation = angle; this.width = width;
            this.isVisible = isVisible; this.sortOrder = sortOrder;

            using (ITextFaceMetrics textFaceMetrics = def.ContainingMap.TextMetricsProvider.GetTextFaceMetrics(fontName, fontSize, TextEffects.None)) {
                FontAscent = textFaceMetrics.Ascent;
                FontDescent = textFaceMetrics.Descent;
                lineSpacing = fontSize * 1.2F;

                TextSymDef.WrapTextProperties wrapProperties = new TextSymDef.WrapTextProperties() {
                    TextFaceMetrics = textFaceMetrics,
                    Tabs = null,
                    CharSpacing = 0,
                    WordSpacing = 1,
                    FontAlign = TextSymDefHorizAlignment.Left,
                    FirstIndent = 0,
                    RestIndent = 0,
                    AddParagraphMarks = false
                };

                if (width > 0) {
                    wrappedText = TextSymDef.BreakLines(wrapProperties, text, width, out coordMapper, out lineWidths, out didWrapAtLeastOneLine);
                }
                else {
                    wrappedText = TextSymDef.BreakUnwrappedLines(wrapProperties, text, out coordMapper, out lineWidths);
                    didWrapAtLeastOneLine = false;
                }

                CalcBounds();
            }
        }

        public override SymPath Path
        {
            get {
                // Location is the top-left. But in our coordinate system, rectangles are reversed along the Y access.
                return GetHighlightPath();
            }
        }

        public override bool CanRotate
        {
            get {
                return true;
            }
        }

        public override Symbol CloneDetached(SymDef newSymdef, Matrix transform)
        {
            return new ImageTextSymbol((ImageSymDef)newSymdef, unwrappedText, textColor, fontName, FontSize, 
                Geometry.TransformPoint(location, transform), Geometry.TransformAngle(rotation, transform), Geometry.TransformDistance(width, transform),
                isVisible, sortOrder);
        }

        public override RectLikeSymbol CloneDetached(SymPath newPath)
        {
            // Provisionally create with the current location and formatted to the desired width.
            ImageTextSymbol newSymbol = CloneDetachedFormatted(newPath);
            if (!newSymbol.DidWrapAtLeastOneLine) {
                // If wrapping didn't occur, go unformatted instead.
                newSymbol = CloneDetachedUnformatted(newPath);
            }

            return newSymbol;
        }

        ImageTextSymbol CloneDetachedFormatted(SymPath newPath)
        {
            PointF newLocation = newPath.Points[3];
            float newWidth = Geometry.DistanceF(newPath.Points[0], newPath.Points[1]);
            float newRotation = Geometry.Angle(newPath.Points[0], newPath.Points[1]);

            return new ImageTextSymbol(def, unwrappedText, textColor, fontName, FontSize, newLocation, newRotation, newWidth, isVisible, sortOrder);
        }

        ImageTextSymbol CloneDetachedUnformatted(SymPath newPath)
        {
            PointF newLocation;

            newLocation = newPath.Points[3];

            float newRotation = Geometry.Angle(newPath.Points[0], newPath.Points[1]);

            return new ImageTextSymbol(def, unwrappedText, textColor, fontName, FontSize, newLocation, newRotation, 0, isVisible, sortOrder);
        }

        public ITextLikeSymbol CloneDetached(string[] newText)
        {
            return new ImageTextSymbol(def, newText, textColor, fontName, FontSize, location, rotation, width, isVisible, sortOrder);
        }

        private void CalcBounds()
        {
            int lineCount = wrappedText.Length;

            // Calculate height of text.
            float height = FontAscent + FontDescent + ((lineCount - 1) * lineSpacing);

            // Calculate full width of text.
            float fullWidth = 0;
            foreach (float w in lineWidths) {
                if (w > fullWidth)
                    fullWidth = w;
            }

            // Get the size.
            size = new SizeF(fullWidth, height);

            // The rectangle, unrotated.
            RectangleF rect = new RectangleF(location.X, location.Y - size.Height, size.Width, size.Height);  // indents only used for left aligned and justified text.

            // Rotate the rectangle.
            if (rotation != 0)
                rect = Geometry.BoundsOfRotatedRectangle(rect, location, rotation);

            boundingBox = rect;
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect)
        {
            // TODO: implement hit testing
            return true;
        }

        internal override Map.SymbolHitOrder HitOrder
        {
            get { return Map.SymbolHitOrder.Text; }
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(IGraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            // Warning: keep changes synchonized with FindInsertionPoint!

            if (def.HasColor(color)) {
                Matrix matrix = new Matrix();
                matrix.Translate(location.X, location.Y);
                matrix.Scale(1, -1); // Reverse Y direction so text is correct way around.
                if (rotation != 0)
                    matrix.RotateAt(-rotation, new PointF(0, 0));
                g.PushTransform(matrix);

                if (!g.HasBrush(this))
                    g.CreateSolidBrush(this, map.TransformColor(textColor));

                if (!g.HasFont(this))
                    g.CreateFont(this, fontName, FontSize, TextEffects.None);

                try {
                    // Draw all the lines of text.
                    PointF pt = new PointF(0F, 0F);

                    for (int lineIndex = 0; lineIndex < wrappedText.Length; ++lineIndex) {
                        g.DrawText(wrappedText[lineIndex], this, this, pt);
                        pt.Y += lineSpacing;
                    }
                }
                finally {
                    g.PopTransform();
                }
            }
        }

        public TextSymDef.InsertionPointLocation FindInsertionPoint(TextCoord textCoord)
        {
            // Map text coord to wrapped coordinates.
            textCoord = coordMapper.WrappedFromUnwrapped(textCoord, unwrappedText, wrappedText);

            if (textCoord.Line < 0 || textCoord.Line >= wrappedText.Length || textCoord.Col < 0 || textCoord.Col > wrappedText[textCoord.Line].Length) {
                return null;
            }

            using (ITextFaceMetrics textFaceMetrics = def.ContainingMap.TextMetricsProvider.GetTextFaceMetrics(fontName, FontSize, TextEffects.None)) {

                Matrix matrix = new Matrix();
                matrix.Translate(location.X, location.Y);
                matrix.Scale(1, -1); // Reverse Y direction so text is correct way around.
                if (rotation != 0)
                    matrix.RotateAt(-rotation, new PointF(0, 0));

                // Draw all the lines of text.
                PointF pt = new PointF(0F, 0F);

                for (int lineIndex = 0; lineIndex < wrappedText.Length; ++lineIndex) {
                    if (lineIndex == textCoord.Line) {
                        string line = wrappedText[lineIndex];

                        // Draw all the text segments in the line. (A text segment is a word, unless charSpacing>0, in which case it is graphemes).
                        int index = 0;
                        bool indexByGraphemes = false;  // May change later.
                        for (; ;) {
                            string textSegment;

                            if (index >= textCoord.Col) {
                                // Found the place!
                                TextSymDef.InsertionPointLocation insertionPoint = new TextSymDef.InsertionPointLocation();

                                // Find all three points along the insertion point, as transformed.
                                PointF insertPt = pt;
                                insertionPoint.Ascent = Geometry.TransformPoint(insertPt, matrix);
                                insertPt.Y += FontAscent;
                                insertionPoint.Baseline = Geometry.TransformPoint(insertPt, matrix);
                                insertPt.Y += FontDescent;
                                insertionPoint.Descent = Geometry.TransformPoint(insertPt, matrix);
                                return insertionPoint;
                            }

                            if (indexByGraphemes) {
                                textSegment = GetNextTextElement(line.Substring(index));
                            }
                            else {
                                textSegment = GetNextTextSegment(line.Substring(index));
                                if (!string.IsNullOrEmpty(textSegment) && textSegment.Length > textCoord.Col - index) {
                                    // The textCoord we are looking for in withing the next text segment. Start going by
                                    // graphemes instead.
                                    indexByGraphemes = true;
                                    textSegment = GetNextTextElement(line.Substring(index));
                                }
                            }

                            if (string.IsNullOrEmpty(textSegment))
                                break;

                            pt.X += textFaceMetrics.GetTextWidth(textSegment);

                            index += textSegment.Length;
                        }
                    }

                    pt.Y += lineSpacing;
                }
            }

            // Not sure how this happens!
            Debug.Fail("Couldn't find insertion point for this text coord.");
            return null;
        }

        // Helper for GetNextTextElement to handle empty/null case.
        static string GetNextTextElement(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            else
                return StringInfo.GetNextTextElement(s);
        }

        // Get the next text segment needed. If empty, returns null.
        // Is the next is a space or tab, that is it.
        // Otherwise, the whole word to the next space or tab.
        private string GetNextTextSegment(string text)
        {
            if (String.IsNullOrEmpty(text))
                return null;

            if (text[0] == ' ')
                return " ";
            if (text[0] == '\t')
                return "\t";

            int i = 0;
            while (i < text.Length && text[i] != ' ' && text[i] != '\t')
                ++i;

            return text.Substring(0, i);
        }

        // Find where in the text is closest to a point.
        // Note: currently this implementation is very very inefficient. It could be made much more
        // efficient, but it is unclear if it needs to be.
        public TextCoord FindClosestInsertionPoint(PointF point)
        {
            return SymbolHelpers.FindClosestInsertionPoint(this, point);
        }

        // Draw this color, if used, of this symbol.
        // Determine accurately if this point is within distance of this symbol.
        public override bool HitTest(PointF pointTest, float distanceTest, MapHitTestOptions options, out float actualDistance, out int holeIndex)
        {
            holeIndex = -1;

            Matrix matrix = new Matrix();
            matrix.Translate(location.X, location.Y);
            matrix.Scale(1, -1); // Reverse Y direction so text is correct way around.
            if (rotation != 0)
                matrix.RotateAt(-rotation, new PointF(0, 0));
            matrix.Invert();  // Invert to translate dest point to source coordinates.
            pointTest = matrix.Transform(pointTest);

            float minDistanceFromText = float.MaxValue;
            PointF pt = new PointF(0F, 0F);

            // For each line of text, hit test against that line of text.
            for (int lineIndex = 0; lineIndex < wrappedText.Length; ++lineIndex) {
                float lineWidth = lineWidths[lineIndex];
                RectangleF textRectangle = new RectangleF(pt.X, pt.Y, lineWidth, FontAscent + FontDescent);
                float distanceFromText = Geometry.DistanceFromRectangle(textRectangle, pointTest);
                if (distanceFromText < minDistanceFromText)
                    minDistanceFromText = distanceFromText;
                pt.Y += lineSpacing;
            }

            // Return minimum of distance from each line of text.
            actualDistance = minDistanceFromText;
            return (minDistanceFromText <= distanceTest);
        }


        internal SymPath GetHighlightPath()
        {
            int lineCount = wrappedText.Length;

            // Calculate height of text.
            float height = FontAscent + FontDescent + ((lineCount - 1) * lineSpacing);

            // Calculate full width of text.
            float fullWidth = 0;
            foreach (float w in lineWidths) {
                if (w > fullWidth)
                    fullWidth = w;
            }

            // Get the size.
            SizeF rectSize = new SizeF(fullWidth, height);

            // The rectangle, unrotated.
            RectangleF rect = new RectangleF(location.X, location.Y - rectSize.Height, rectSize.Width, rectSize.Height);  

            // Create path from rectangle.
            SymPath path = SymPath.CreateRectanglePath(rect);

            // Rotate the rectangle.
            if (rotation != 0) {
                Matrix matrix = new Matrix();
                matrix.RotateAt(rotation, location);  // Correct for centered/right aligned text?
                path = path.Transform(matrix);
            }

            return path;
        }


        // Highlight the symbol.
        public override void DrawHighlight(IGraphicsTarget g, HighlightOptions options)
        {
            SymPath path = GetHighlightPath();
            SymbolHelpers.DrawAreaHighlight(g, map, new SymPathWithHoles(path, null), options);
        }

        public override RectangleF HighlightBounds(HighlightOptions options)
        {
            SymPath path = GetHighlightPath();
            return SymbolHelpers.AreaHighlightBounds(new SymPathWithHoles(path, null), options);
        }


        // Get four points that define the bounds of the text, and the size, and the baseline point.
        public PointF[] GetCornerPoints(out SizeF sizeText, out PointF baselinePoint)
        {
            PointF[] pts = new PointF[4];

            pts[0] = location;
            pts[1].X = pts[0].X + size.Width; pts[1].Y = pts[0].Y;
            pts[2].X = pts[0].X + size.Width; pts[2].Y = pts[0].Y - size.Height;
            pts[3].X = pts[0].X; pts[3].Y = pts[0].Y - size.Height;

            baselinePoint = new PointF(pts[0].X, pts[0].Y - FontAscent);
            sizeText = size;

            if (rotation != 0) {
                Matrix mat = new Matrix();
                mat.RotateAt(rotation, location);
                pts = Geometry.TransformPoints(pts, mat);
                baselinePoint = Geometry.TransformPoint(baselinePoint, mat);
            }

            return pts;
        }
    }

    // This is an bitmap object in the layout layer.
    public class ImageBitmapSymbol : RectLikeSymbol
    {
        ImageSymDef def;
        public override SymDef Definition { get { return def; } }

        PointF location;
        public PointF Location { get { return location; } }

        // If non-null, this is used as the bitmap data instead of loading from the file name.
        private byte[] embeddedData; 
        public byte[] EmbeddedData { get { return embeddedData; } } 

        private IFileLoader fileLoader;

        private readonly string fileName;
        public string FileName
        {
            get { return fileName; }
        }

        private readonly float mmPerPixX;
        public float MmPerPixX
        {
            get { return mmPerPixX; }
        }

        private readonly float mmPerPixY;
        public float MmPerPixY
        {
            get { return mmPerPixY; }
        }

        // UNDONE: How does Dispose get called on this bitmap?
        IGraphicsBitmap bitmap;

        private readonly bool isVisible;
        public override bool IsVisible
        {
            get { return isVisible; }
        }

        private readonly int sortOrder;
        public override int SortOrder
        {
            get { return sortOrder; }
        }

        public ImageBitmapSymbol(ImageSymDef def, string fileName, PointF location, float mmPerPixX, float mmPerPixY, byte[] embeddedData, bool isVisible, int sortOrder, IFileLoader fileLoader)
        {
            this.def = def;
            this.isVisible = isVisible; this.sortOrder = sortOrder;
            this.fileName = fileName;
            this.location = location;
            this.mmPerPixX = mmPerPixX; this.mmPerPixY = mmPerPixY;
            this.embeddedData = embeddedData;
            this.fileLoader = fileLoader;

            if (isVisible) {
                // Try to load the bitmap.

                if (embeddedData != null && fileLoader != null) {
                    bitmap = fileLoader.LoadBitmapFromData(embeddedData);
                }
                else if (fileLoader != null) {
                    bitmap = fileLoader.LoadBitmap(fileName, false);
                }

                if (bitmap != null) {
                    double width = bitmap.PixelWidth * mmPerPixX, height = bitmap.PixelHeight * mmPerPixY;
                    boundingBox = new RectangleF((float)(location.X - width / 2), (float)(location.Y - height / 2), (float)width, (float)height);
                }
                else {
                    this.isVisible = false;
                }
            }
        }

        public override SymPath Path
        {
            get {
                if (isVisible) {
                    return SymPath.CreateRectanglePath(boundingBox);
                }
                else {
                    return SymPath.CreateRectanglePath(new RectangleF(location, new SizeF(0, 0)));
                }
            }
        }

        public override bool CanRotate
        {
            get {
                return false;
            }
        }


        public override Symbol CloneDetached(SymDef newSymdef, Matrix transform)
        {
            return new ImageBitmapSymbol((ImageSymDef)newSymdef, fileName, Geometry.TransformPoint(location, transform), 
                Geometry.TransformDistance(mmPerPixX, transform), Geometry.TransformDistance(mmPerPixY, transform),
                embeddedData, isVisible, sortOrder, fileLoader);
        }

        public override RectLikeSymbol CloneDetached(SymPath newPath)
        {
            if (!isVisible)
                return (RectLikeSymbol)CloneDetached();
            else {
                PointF newLocation = Geometry.MidPoint(newPath.Points[0], newPath.Points[2]);
                float width = Geometry.DistanceF(newPath.Points[0], newPath.Points[1]);
                float height = Geometry.DistanceF(newPath.Points[1], newPath.Points[2]);
                float newMmPerPixelWidth = width / bitmap.PixelWidth;
                float newMmPerPixelHeight = height / bitmap.PixelHeight;

                return new ImageBitmapSymbol(def, fileName, newLocation, 
                    newMmPerPixelWidth, newMmPerPixelHeight,
                    embeddedData, isVisible, sortOrder, fileLoader);
            }
        }

        // Determine accurately if this point is within distance of this symbol.
        public override bool HitTest(PointF pointTest, float distanceTest, MapHitTestOptions options, out float actualDistance, out int holeIndex)
        {
            SymPath symPath = SymPath.CreateRectanglePath(boundingBox);
            return SymbolHelpers.HitTestArea(new SymPathWithHoles(symPath, null), pointTest, distanceTest, options, out actualDistance, out holeIndex);
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect)
        {
            return true;
        }

        internal override Map.SymbolHitOrder HitOrder
        {
            get { return Map.SymbolHitOrder.Bitmap; }
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(IGraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            if (isVisible && def.HasColor(color)) {
                // Create matrix to flip Y so bitmap is right way around.
                Matrix matrix = new Matrix();
                matrix.Translate(location.X, location.Y);
                matrix.Scale(1, -1); 
                matrix.Translate(-location.X, -location.Y);

                g.PushTransform(matrix);
                g.DrawBitmap(bitmap, boundingBox, BitmapScaling.HighQuality, renderOpts.minResolution);  // UNDONE: should bitmap scaling mode be customizable?
                g.PopTransform();
            }
        }


        // Highlight the symbol.
        public override void DrawHighlight(IGraphicsTarget g, HighlightOptions options)
        {
            SymPath path = SymPath.CreateRectanglePath(boundingBox);
            SymbolHelpers.DrawAreaHighlight(g, map, new SymPathWithHoles(path, null), options);
        }

        public override RectangleF HighlightBounds(HighlightOptions options)
        {
            SymPath path = SymPath.CreateRectanglePath(boundingBox);
            return SymbolHelpers.AreaHighlightBounds(new SymPathWithHoles(path, null), options);
        }

    }

    public class RectangleSymbol : RectLikeSymbol
    {
        RectangleSymDef def;
        public override SymDef Definition { get { return def; } }

        PointF location;
        SizeF size;
        float rotation;

        public PointF Location { get { return location; } }
        public SizeF Size { get { return size; } }
        public float Rotation { get { return rotation;  } }

        SymPath path; // the path of the outside, include corner rounding.
        
        public RectangleSymbol(RectangleSymDef def, PointF location, SizeF size, float rotation)
        {
            this.def = def; this.location = location; this.size = size; this.rotation = rotation;
            boundingBox = def.CalcBounds(location, size, rotation);
            path = def.GetPath(location, size, rotation);
        }

        public override Symbol CloneDetached(SymDef newSymdef, Matrix transform)
        {
            return new RectangleSymbol((RectangleSymDef)newSymdef, 
                Geometry.TransformPoint(location, transform), 
                new SizeF(Geometry.TransformDistance(size.Width, transform), Geometry.TransformDistance(size.Height, transform)),
                Geometry.TransformAngle(rotation, transform));
        }

        public override RectLikeSymbol CloneDetached(SymPath newPath)
        {
            PointF newLocation = newPath.Points[0];
            SizeF newSize = new SizeF(Geometry.DistanceF(newPath.Points[0], newPath.Points[1]), Geometry.DistanceF(newPath.Points[1], newPath.Points[2]));
            float newRotation = Geometry.Angle(newPath.Points[0], newPath.Points[1]);

            return new RectangleSymbol(def, newLocation, newSize, newRotation);
        }

        public override bool CanRotate
        {
            get { return true; }
        }

        public override SymPath Path
        {
            get
            {
                // Can't use this.path, because this should be without corner rounding.
                SymPath pathWithoutCornerRounding = SymPath.CreateRectanglePath(new RectangleF(location, size));
                if (rotation != 0) {
                    Matrix mat = new Matrix();
                    mat.RotateAt(rotation, location);
                    pathWithoutCornerRounding = pathWithoutCornerRounding.Transform(mat);
                }

                return pathWithoutCornerRounding;
            }
        }

        SymPath GetPath()
        {
            return def.GetPath(location, size, rotation);
        }

        // Determine accurately if this point is within distance of a this symbol.
        public override bool HitTest(PointF pointTest, float distanceTest, MapHitTestOptions options, out float actualDistance, out int holeIndex)
        {
            holeIndex = -1;

            return SymbolHelpers.HitTestLine(path, def.LineThickness, pointTest, distanceTest, out actualDistance);
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate. Always called after bounding
        // box test, so no need to test that again.
        public override bool MayIntersectRect(RectangleF rect)
        {
            return true;
        }

        internal override Map.SymbolHitOrder HitOrder
        {
            get { return Map.SymbolHitOrder.Line; }
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(IGraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
            def.Draw(g, location, size, rotation, color, renderOpts);
        }

        // Highlight the symbol.
        public override void DrawHighlight(IGraphicsTarget g, HighlightOptions options)
        {
            SymbolHelpers.DrawLineHighlight(g, map, path, def.LineThickness, LineJoinMode.Miter, LineCapMode.Flat, options);
        }

        public override RectangleF HighlightBounds(HighlightOptions options)
        {
            return SymbolHelpers.LineHighlightBounds(path, def.LineThickness, options);
        }

    }

    // This is an symbol that defines a hole inside an Area-like symbol. It is itself an area like symbol, 
    // but holes cannot have holes.
    public class HoleSymbol: AreaLikeSymbol
    {
        public override SymDef Definition { get { return HoleSymDef.Singleton; } }

        readonly AreaLikeSymbol symbolWithHole;
        public AreaLikeSymbol SymbolWithHole
        {
            get { return symbolWithHole; }
        }

        public SymPath HolePath { get { return mainPath; } }

        public HoleSymbol(AreaLikeSymbol symbolWithHole, SymPath path)
            : base(new SymPathWithHoles(path, null))
        {
            this.symbolWithHole = symbolWithHole;
            boundingBox = path.BoundingBox;

            Debug.Assert(!(symbolWithHole is HoleSymbol), "can't have a hole in a hole");
            // Hole is currently detached from it's corresponding symbol, until it is actually added.
        }

        protected override void HolesChanged()
        {
            // Nothing to do.
        }

        public new bool IsAttached
        {
            get { return symbolWithHole.IsAttached(this); }
        }

        public override Symbol CloneDetached(SymDef newSymdef, Matrix transform)
        {
            return new HoleSymbol(symbolWithHole, HolePath.Transform(transform));
        }

        public override AreaLikeSymbol CloneDetached(SymPathWithHoles newPath)
        {
            Debug.Assert(newPath.Holes == null, "A hole can't have holes in it.");
            return new HoleSymbol(symbolWithHole, newPath.MainPath);
        }

        // Determine accurately if this point is within distance of a this symbol.
        public override bool HitTest(PointF pointTest, float distanceTest, MapHitTestOptions options, out float actualDistance, out int holeIndex)
        {
            if (options.AreaIncludeHoleInteriors && HolePath.IsInside(pointTest)) {
                // Point is inside the hole.
                actualDistance = 0;
                holeIndex = -1;
                return true;
            }

            actualDistance = float.MaxValue;
            holeIndex = -1;
            return false;
        }

        // Determine if this symbol might draw somewhere within this rect. If false is
        // returned, must be accurate; true can be inaccurate.
        public override bool MayIntersectRect(RectangleF rect)
        {
            return true;
        }

        internal override Map.SymbolHitOrder HitOrder
        {
            get { return Map.SymbolHitOrder.AreaHole; }
        }

        // Draw this color, if used, of this symbol.
        public override void Draw(IGraphicsTarget g, SymColor color, RenderOptions renderOpts)
        {
        }


        // Highlight the symbol.
        public override void DrawHighlight(IGraphicsTarget g, HighlightOptions options)
        {
            SymbolHelpers.DrawAreaHighlightedHole(g, map, HolePath, options);
        }

        public override RectangleF HighlightBounds(HighlightOptions options)
        {
            return SymbolHelpers.AreaHighlightBounds(Path, options);
        }


        public bool HasColor(SymColor color)
        {
            return false;
        }
    }



    internal static class SymbolHelpers
    {
        public static bool HitTestArea(SymPathWithHoles path, PointF point, float distance, MapHitTestOptions options, out float actualDistance, out int holeIndex)
        {
            if (!options.AreaBordersOnly)
            {
                if (path.IsInside(point)) {
                    // Point is inside the area.
                    actualDistance = 0;
                    holeIndex = -1;
                    return true;
                }

                if (options.AreaIncludeHoleInteriors && path.Holes != null && path.Holes.Length > 0) {
                    for (int hole = 0; hole < path.Holes.Length; ++hole) {
                        if (path.Holes[hole].IsInside(point)) {
                            // Point is inside a hole.
                            actualDistance = 0;
                            holeIndex = hole;
                            return true;
                        }
                    }
                }
            }

            // Determine distance from a point to the boundary of this path.
            float minDistance = path.MainPath.DistanceFromPoint(point);
            int minHoleIndex = -1;

            if (path.Holes != null) {
                for (int iHole = 0; iHole < path.Holes.Length; ++iHole) {
                    float d = path.Holes[iHole].DistanceFromPoint(point);
                    if (d < minDistance) {
                        minDistance = d;
                        if (options.AreaIncludeHoleInteriors)
                            minHoleIndex = iHole;
                        else
                            minHoleIndex = -1;
                    }
                }
            }

            if (minDistance <= distance) {
                actualDistance = minDistance;
                holeIndex = minHoleIndex;
                return true;
            }

            // Not in or near.
            actualDistance = float.MaxValue;
            holeIndex = -1;
            return false;
        }

        public static bool HitTestLine(SymPath path, float width, PointF point, float distance, out float actualDistance)
        {
            actualDistance = Math.Max(0, path.DistanceFromPoint(point) - (width / 2));
            return (actualDistance <= distance);
        }

        public static bool LineMayIntersectRect(SymPath path, float pathWidth, RectangleF rect)
        {
            rect.Inflate(new SizeF(pathWidth, pathWidth));
            return path.MayIntersectRect(rect);
        }

        public static float MinimumLineWidth(HighlightOptions options)
        {
            return (float) (1.0 * options.logicalPixelSize);  // minimum thickness of 1 logical pixels.
        }

        private static float HighlightWidth(float lineWidth, HighlightOptions options)
        {
            return Math.Max(lineWidth, MinimumLineWidth(options)); 
        }

        public static void DrawLineHighlight(IGraphicsTarget g, Map map, SymPath path, float lineWidth, LineJoinMode lineJoin, LineCapMode lineCap, HighlightOptions options)
        {
            float width = HighlightWidth(lineWidth, options);
            object pen = map.GetHighlightPen(g, width, lineJoin, lineCap);
            path.Draw(g, pen);
        }

        public static RectangleF LineHighlightBounds(SymPath path, float lineWidth, HighlightOptions options)
        {
            float width = HighlightWidth(lineWidth, options);
            RectangleF box = path.BoundingBox;
            box.Inflate(width / 2, width / 2);
            return box;
        }

        public static void DrawAreaHighlight(IGraphicsTarget g, Map map, SymPathWithHoles path, HighlightOptions options)
        {
            object boundaryPen = map.GetHighlightPen(g, (float) (options.logicalPixelSize * 1.5F), LineJoinMode.Miter, LineCapMode.Flat);
            object fillBrush = map.GetDimHighlightBrush(g);
            path.Fill(g, fillBrush);
            path.Draw(g, boundaryPen);
        }

        // Only fills the interior, doesn't draw boundary.
        public static void FillAreaHighlight(IGraphicsTarget g, Map map, SymPathWithHoles path, HighlightOptions options)
        {
            object fillBrush = map.GetDimHighlightBrush(g);
            path.Fill(g, fillBrush);
        }


        public static void DrawAreaHighlightedHole(IGraphicsTarget g, Map map, SymPath holePath, HighlightOptions options)
        {
            // UNDONE: Should the highlighting for an area hole be somehow distinguished. It feels like it.
            object boundaryPen = map.GetHoleHighlightPen(g, (float) (options.logicalPixelSize * 1.5F));
            object fillBrush = map.GetDimHoleHighlightBrush(g);
            holePath.Fill(g, fillBrush);
            holePath.Draw(g, boundaryPen);
        }

        public static RectangleF AreaHighlightBounds(SymPathWithHoles path, HighlightOptions options)
        {
            RectangleF box = path.BoundingBox;
            box.Inflate((float)(options.logicalPixelSize), (float)(options.logicalPixelSize));
            return box;
        }

        // Draw cross hatching into the interior of the SymPath.
        static void DrawCrossHatching(IGraphicsTarget g, SymPathWithHoles path, object pen, float spacing)
        {
            // Set the clipping region to draw only inside the area.
            g.PushClip(path.GetPathKey(g));

            // use a transform to rotate and then draw hatching.
            Matrix matrix = new Matrix();
            matrix.RotateAt(45, new PointF(0, 0));
            g.PushTransform(matrix);

            try {
                // Get the correct bounding rect.
                RectangleF bounding = Geometry.BoundsOfRotatedRectangle(path.BoundingBox, new PointF(), -45);

                DrawHatchLines(g, pen, spacing, 0, bounding);

                // and again for the second bound of hatching
                // Get the correct bounding rect.
                bounding = Geometry.BoundsOfRotatedRectangle(path.BoundingBox, new PointF(), 45);

                matrix = new Matrix();
                matrix.RotateAt(-90, new PointF(0, 0));
                g.PushTransform(matrix);
                try {
                    DrawHatchLines(g, pen, spacing, 0, bounding);
                }
                finally {
                    g.PopTransform();
                }
            }
            finally {
                // restore the clip region and the transform
                g.PopTransform();
                g.PopClip();
            }
        }

        // Draw a set of horizontal hatching lines at the given spacing with
        // the given pen. A line should be centered on the zero y coordinate.
        public static void DrawHatchLines(IGraphicsTarget g, object penKey, float spacing, float offset, RectangleF boundingRect)
        {
            double firstLine = Math.Round((boundingRect.Top - offset) / spacing) * spacing + offset;
            double lastLine = (Math.Round((boundingRect.Bottom - offset) / spacing) + 0.5) * spacing + offset;

            for (double y = firstLine; y <= lastLine; y += spacing) {
                g.DrawLine(penKey, new PointF(boundingRect.Left, (float)y), new PointF(boundingRect.Right, (float)y));
            }
        }

        // Find where in the text is closest to a point.
        // Note: currently this implementation is very very inefficient. It could be made much more
        // efficient, but it is unclear if it needs to be.
        public static TextCoord FindClosestInsertionPoint(ITextLikeSymbol symbol, PointF point)
        {
            double closestDistance = 1E20;
            TextCoord closestCoord = new TextCoord();
            string[] text = symbol.Text;

            for (int line = 0; line < text.Length; ++line) {
                for (int col = 0; col <= text[line].Length; ++col) {
                    // Note that col can be just beyond end of line.
                    var insertionPoint = symbol.FindInsertionPoint(new TextCoord(line, col));
                    double distance = Geometry.DistanceFromLineSegment(insertionPoint.Ascent, insertionPoint.Descent, point);
                    if (distance < closestDistance) {
                        closestDistance = distance;
                        closestCoord = new TextCoord(line, col);
                    }
                }
            }

            return closestCoord;
        }

    }


}
