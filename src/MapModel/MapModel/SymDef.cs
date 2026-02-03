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
using System.Text;
using System.Drawing;

namespace PurplePen.MapModel
{
    using System.Linq;
    using PurplePen.Graphics2D;

    public abstract class SymDef
    {
        string name;
        public string Name { get { return name; } }

        internal List<Symbol> symbols = new List<Symbol>();
        public ICollection<Symbol> Symbols
        {
            get
            {
                return symbols;
            }
        }

        // Symdefs that depends on this symdef (e.g., area symdefs this use this symdef as a bounding line).
        internal List<SymDef> dependentSymdefs = new List<SymDef>();
        public ICollection<SymDef> DependentSymdefs
        {
            get
            {
                return dependentSymdefs;
            }
        }

        protected SymDef(string name, string symbolId)
        {
            this.name = name;
            this.symbolId = symbolId;
        }

        // The containing map
        protected Map map;
        public Map ContainingMap { get { return map; } }

        private ToolboxIcon toolboxIcon;

        public ToolboxIcon ToolboxImage
        {
            get
            {
                if (toolboxIcon == null)
                    toolboxIcon = new ToolboxIcon();
                return toolboxIcon;
            }
            set
            {
                CheckModifiable();
                toolboxIcon = value;
            }
        }

        private string symbolId;

        public string SymbolId
        {
            get { return symbolId; }
        }

        public virtual void SetMap(Map newMap)
        {
            if (map != null && newMap != null && map != newMap)
                throw new MapUsageException("Cannot add SyMDef to a map; it is already part of another map.");
            map = newMap;
        }

        // A new symbol with this symdef is added to the map. Add to the list of symbols with this SymDef.
        internal void AddSymbol(Symbol sym)
        {
            Debug.Assert(sym.Definition == this);

            symbols.Add(sym);
        }

        // A symbol with the symdef is removed from the map. Remove from the list of symbosl with this symdef.
        internal void RemoveSymbol(Symbol sym)
        {
            Debug.Assert(sym.Definition == this);
            symbols.Remove(sym);
        }

        // A new dependent symdef is added.
        internal void AddDependentSymdef(SymDef symdef)
        {
            Debug.Assert(symdef.DependsOnSymdef == this);

            dependentSymdefs.Add(symdef);
        }

        // A dependent symdef is removed
        internal void RemoveDependentSymdef(SymDef symdef)
        {
            Debug.Assert(symdef.DependsOnSymdef == this);
            dependentSymdefs.Remove(symdef);
        }

        protected void CheckModifiable()
        {
            if (map != null)
                throw new MapUsageException("Cannot modify a SymDef after it has been added to a map");
        }

        protected void CheckColor(SymColor color)
        {
            if (color != null && color.ContainingMap != map)
                throw new MapUsageException("Color in SymDef is not part of the containing map");
        }

        // Does this symbol definition draw the given color. Used to determine
        // if this symbol definition draws into the current layer being draw.
        // If color is null, return if draws in the image layer (below all color layers).
        public abstract bool HasColor(SymColor color);


        // Does this symdef depend on another? null means no.
        public virtual SymDef DependsOnSymdef
        {
            get
            {
                return null;
            }
        }

        // Remove any caches brushes/pens/fonts.
        public abstract void FreeGdiObjects();

        // Do the symbols need to be sorted before drawing them?
        public virtual bool SortSymbolsForDrawing
        {
            get { return false; }
        }

        public abstract SymDef CopyToMap(Map map);
    }

    public class PointSymDef: SymDef
    {
        private readonly Glyph glyph;

        public Glyph Glyph { get { return glyph; } }

        readonly bool allowRotation;  // Should this glyph rotate when the feature/map is rotates, or always remain in the same orientation.
        public bool AllowRotation { get { return allowRotation; } }

        public float Radius { get { return glyph.Radius; } }

        public PointSymDef(string name, string symbolId, Glyph glyph, bool allowRotation)
            : base(name, symbolId)
        {
            if (glyph == null)
                throw new ArgumentNullException(nameof(glyph));
            this.glyph = glyph;
            this.allowRotation = allowRotation;
        }

        public override void SetMap(Map newMap)
        {
            base.SetMap(newMap);

            if (newMap != null) {
                glyph.CheckColors(newMap);
            }
        }

        public override SymDef CopyToMap(Map map)
        {
            var newSymDef = new PointSymDef(Name, SymbolId, glyph.CopyToMap(map), allowRotation);
            map.AddSymdef(newSymDef);
            return newSymDef;
        }

        // Does this symbol definition draw the given color. Used to determine
        // if this symbol definition draws into the current layer being draw.
        public override bool HasColor(SymColor color)
        {
            Debug.Assert(color != null);
            if (color.IsSpecialLayer)
                return false;
            return glyph.HasColor(color);
        }

        // Draw this point symbol at point pt with angle ang in this graphics (given color only).
        internal void Draw(IGraphicsTarget g, PointF pt, float angle, float[] gaps, SymColor color, RenderOptions renderOpts)
        {
            glyph.Draw(g, pt, angle, null, gaps, color, null, renderOpts);
        }

        internal void DrawHighlight(IGraphicsTarget g, PointF location, float rotation, float[] gaps, HighlightOptions options)
        {
            if (options.style == HighlightStyle.HighFidelity && glyph.Radius < 4 * options.logicalPixelSize) {
                // Draw bubble.
                float radius = (float) (6 * options.logicalPixelSize);
                g.FillEllipse(map.GetDimHighlightBrush(g), location, radius, radius);
            }

            glyph.DrawHighlight(g, location, rotation, null, gaps, map.GetHighlightBrush(g));
        }

        internal RectangleF HighlightBounds(PointF location, HighlightOptions options)
        {
            float radius = glyph.Radius;
            if (options.style == HighlightStyle.HighFidelity && radius < 4 * options.logicalPixelSize) {
                // Draw bubble.
                radius = (float)(6 * options.logicalPixelSize);
            }

            return new RectangleF(location.X - radius, location.Y - radius, radius * 2, radius * 2);
        }

        // Calculate the bounding box
        internal RectangleF CalcBounds(PointF pt, float angle)
        {
            RectangleF box1 = glyph.BoundingBox;
            if (angle != 0)
                box1 = Geometry.BoundsOfRotatedRectangle(box1, new PointF(), angle);
            box1.Offset(pt);

            float radius = glyph.Radius;
            RectangleF box2 = new RectangleF(pt.X - radius, pt.Y - radius, radius * 2, radius * 2);
            box1.Intersect(box2);
            return box1;
        }

        public override void FreeGdiObjects()
        {
            glyph.FreeGdiObjects();
        }
    }

    public abstract class LineLikeSymDef: SymDef
    {
        public LineLikeSymDef(string name, string symbolId)
            :base(name, symbolId)
        {
        }

        public abstract float MaxThickness { get; } 
        public abstract float HighlightThickness { get; }

        // Used during OpenMapper import for how to import dash points.
        internal abstract bool HasDashes { get; }

        internal abstract RectangleF CalcBounds(SymPath path);

        internal abstract void Draw(IGraphicsTarget g, SymPath path, SymColor color, RenderOptions renderOpts);

        internal abstract void DrawHighlight(IGraphicsTarget g, SymPath path, HighlightOptions highlightOptions);
    }

    // A combination of multiple line symdefs. Acts like a line symdef. For OpenMapper support.
    public class LineComboSymDef: LineLikeSymDef
    {
        public LineSymDef[] components;

        public LineComboSymDef(string name, string symbolId, IEnumerable<LineSymDef> components)
            :base(name, symbolId)
        {
            this.components = components.ToArray();

        }

        public IEnumerable<LineSymDef> Components
        {
            get
            {
                return components.ToList().AsReadOnly();
            }
        }

        public override float HighlightThickness
        {
            get
            {
                float max = 0;
                foreach (LineSymDef component in components) {
                    max = Math.Max(max, component.HighlightThickness);
                }
                return max;
            }
        }

        public override float MaxThickness
        {
            get
            {
                float max = 0;
                foreach (LineSymDef component in components) {
                    max = Math.Max(max, component.MaxThickness);
                }
                return max;
            }
        }

        internal override bool HasDashes
        {
            get
            {
                foreach (LineSymDef component in components) {
                    if (component.HasDashes)
                        return true;
                }

                return false;
            }
        }

        public override SymDef CopyToMap(Map map)
        {
            throw new NotImplementedException();
        }

        public override void FreeGdiObjects()
        {
        }

        public override bool HasColor(SymColor color)
        {
            foreach (LineSymDef component in components) {
                if (component.HasColor(color))
                    return true;
            }

            return false;
        }

        internal override RectangleF CalcBounds(SymPath path)
        {
            RectangleF bounds = new RectangleF();
            bool first = true;
            foreach (LineSymDef component in components) {
                RectangleF componentBounds = component.CalcBounds(path);

                if (first)
                    bounds = componentBounds;
                else
                    bounds = RectangleF.Union(bounds, componentBounds);

                first = false;
            }

            return bounds;
        }

        internal override void Draw(IGraphicsTarget g, SymPath path, SymColor color, RenderOptions renderOpts)
        {
            foreach (LineSymDef component in components) {
                component.Draw(g, path, color, renderOpts);
            }
        }

        internal override void DrawHighlight(IGraphicsTarget g, SymPath path, HighlightOptions highlightOptions)
        {
            foreach (LineSymDef component in components) {
                component.DrawHighlight(g, path, highlightOptions);
            }
        }
    }

    public class LineSymDef: LineLikeSymDef
    {
        public enum SpacingMethod { OCAD, OpenMapperDashes, OpenMapperMidSymbols}
        // describes dash information
        public struct DashInfo
        {
            public float dashLength;      // length of the dashes (solid part)
            public float firstDashLength; // length of the first dash 
            public float lastDashLength;  // length of the last dash
            public bool halfEndDashLengthWhenClosed; // make first/last dash half length on a closed curve (for OpenMapper compatibility).
            public float gapLength;       // length of the gaps (undrawn part)
            public int minGaps;           // minimum number of GAPS in the line
            public int secondaryMiddleGaps;     // number of secondary gaps in the main dash (0 for none)
            public float secondaryMiddleLength; // length of the secondary gaps
            public int secondaryEndGaps;     // number of secondary gaps in the first and last dashes (0 for none)
            public float secondaryEndLength; // length of the first and last secondary gaps
            public SpacingMethod spacingMethod;    // method of computing dash spacing.
        }

        // describes double line info
        public struct DoubleLineInfo
        {
            public SymColor doubleFillColor; // color between double lines (or null for none)
            public SymColor doubleLeftColor; // color of left double line
            public SymColor doubleRightColor; // color of right double lines
            public float doubleThick;		// distance between the double lines, and thickness of fill
            public float doubleLeftWidth; // thickness of left double line
            public float doubleRightWidth; // thickness of right double line
            public bool doubleFillDashed;          // Dash the fill part?
            public bool doubleLeftDashed;        // Dash the left part?
            public bool doubleRightDashed;      // Dash the right part?
            public DashInfo doubleDashes;  // Info on the dashes.

        }

        // describes shorting of the line ends
        public struct ShortenInfo
        {
            public float shortenBeginning;	// amount to shorten beginning of line
            public float shortenEnd;		// amount to shorten end of line
            public bool pointyEnds;			// draw points to regular end
        }

        // Describes where a glyph is located.
        public enum GlyphLocation { 
            Start,        // at the start of the path (without shortening)
            End,         // at the end of the path (without shortening)
            Corners,   // at corner points
            CornersIgnoreEnds,   // at corner points, but never at the ends
            DashPoints,     // at the explicit dash points
            DashPointsIgnoreEnds,  // at the explicit dash points, but never at ends
            DashCenters,           // at the center of the main dashes in the dash information
            GapCenters,     // at the gaps between main dashes
            Spaced,            // spaced at distances given
            SpacedOffset,       // spaced as distances given, then offset by a certain amount, and the last one removed.
            SpacedDecrease,       // spaced as distances given, but with decreasing size and spacing
        }

        public struct GlyphInfo
        {
            public Glyph glyph;           // the glyph
            public GlyphLocation location;// where on line to draw it
            public float distance;        // for GlyphLocation.Spaced
            public float firstDistance;   // for GlyphLocation.Spaced
            public float lastDistance;    // for GlyphLocation.Spaced
            public int minimum;           // for GlyphLocation.Spaced, GapCenters -- minimum number of symbols. For GapCenters, the Max of this and the minimum number of gaps is used.
            public int number;            // number of symbols at each location
            public float spacing;         // spacing of symbols if number > 1
            public float offset;            // offset for GlyphLocation.SpacedOffset
            public float decreaseLimit;  // value between 1 and 0 with decrease final amount (SpacedDecrease only)
            public bool decreaseBothEnds;  // If true, decrease to both ends, else decrease to one end (SpacedDecrease only)
            public bool noScaleAtCorners;  // If true, glyph is NOT scaled when its a corner glyph.
            public SpacingMethod spacingMethod; // Way to compute spacing
        }

        private class LineSymDefPens
        {
            public object mainPen = new object();
            public object secondPen = new object();
            public object pointyEndsBrush = new object();
            public object doubleFillPen = new object();
            public object doubleLeftPen = new object();
            public object doubleRightPen = new object();
        }

        const float CORNER_GLYPH_STRETCH_LIMIT = 3.0F;

        SymColor lineColor;
        float thickness;
        LineCapMode lineCap;
        LineJoinMode lineJoin;

        SymColor secondLineColor;
        float secondThickness;
        LineJoinMode secondLineJoin;
        LineCapMode secondLineCap;

        DashInfo dashInfo;
        bool isDashed;

        bool isDoubleLine;
        DoubleLineInfo doubleLines;

        ShortenInfo shortenInfo;

        GlyphInfo[] glyphs;

        LineSymDefPens drawingPens = new LineSymDefPens();      // Pens for regular drawing
        LineSymDefPens highlightPens = new LineSymDefPens();    // Pens for drawing the highlight

        float maxThickness, maxMiteredThickness, highlightThickness;

        public SymColor LineColor { get { return lineColor; } }
        public float LineThickness { get { return thickness; } }
        public LineCapMode MainLineCap { get { return lineCap; } }
        public LineJoinMode MainLineJoin { get { return lineJoin; } }
        public bool HasSecondLine { get { return secondLineColor != null && secondThickness > 0; } }
        public SymColor SecondLineColor { get { return secondLineColor; } }
        public float SecondThickness { get { return secondThickness; } }
        public LineJoinMode SecondLineJoin { get { return secondLineJoin; } }
        public LineCapMode SecondLineCap { get { return secondLineCap; } }
        public bool IsDashed { get { return isDashed; } }
        public DashInfo Dashes { get { return dashInfo; } }
        public bool IsDoubleLine { get { return isDoubleLine; } }
        public DoubleLineInfo DoubleLines { get { return doubleLines; } }
        public GlyphInfo[] Glyphs { get { return (glyphs == null) ? null : (GlyphInfo[]) glyphs.Clone(); } }
        public override float MaxThickness { get { return maxThickness; } }
        public override float HighlightThickness { get { return highlightThickness; } }
        public ShortenInfo Shortening { get { return shortenInfo; } }
        public bool IsShortened { get { return (shortenInfo.shortenBeginning > 0 || shortenInfo.shortenEnd > 0); } }

        public override void SetMap(Map newMap)
        {
            base.SetMap(newMap);

            if (newMap != null) {
                CheckColor(lineColor);
                CheckColor(secondLineColor);
                CheckColor(doubleLines.doubleFillColor);
                CheckColor(doubleLines.doubleLeftColor);
                CheckColor(doubleLines.doubleRightColor);

                // compute the max thickness of this line.
                maxThickness = 0.0F;
                if (lineColor != null && thickness > maxThickness)
                    maxThickness = thickness;
                if (lineColor != null && lineJoin == LineJoinMode.Miter && thickness > maxMiteredThickness)
                    maxMiteredThickness = thickness;

                if (secondLineColor != null && secondThickness > maxThickness)
                    maxThickness = secondThickness;
                if (secondLineColor != null && secondLineJoin == LineJoinMode.Miter && secondThickness > maxMiteredThickness)
                    maxMiteredThickness = secondThickness;

                if (isDoubleLine) {
                    float doubleThickness = (2 * Math.Max(doubleLines.doubleLeftWidth, doubleLines.doubleRightWidth) + doubleLines.doubleThick);
                    if (doubleThickness > maxThickness)
                        maxThickness = doubleThickness;
                    if (doubleThickness > maxMiteredThickness)
                        maxMiteredThickness = doubleThickness;
                }

                highlightThickness = maxThickness;  // don't include glyph size in the highlight thickness.

                if (glyphs != null) {
                    foreach (GlyphInfo glyphInfo in glyphs) {
                        glyphInfo.glyph.CheckColors(newMap);

                        float glyphRadius = glyphInfo.glyph.Radius;

                        if (glyphRadius * 2 > maxThickness)
                            maxThickness = glyphRadius * 2;
                        if (glyphInfo.location == GlyphLocation.Corners && glyphRadius * 2 > maxMiteredThickness)
                            maxMiteredThickness = glyphRadius * 2;
                    }
                }

                // If a line symbol has no thickness other than glyphs, then include glyphs (e.g., narrow marsh).
                if (highlightThickness == 0)
                    highlightThickness = maxThickness;
            }
        }

        public LineSymDef(string name, string symbolId, SymColor color, float thick, LineJoinMode lineJoin, LineCapMode lineCap)
            : base(name, symbolId)
        {
            lineColor = color;
            thickness = thick;
            this.lineJoin = lineJoin;
            this.lineCap = lineCap;
        }

        public void SetSecondLine(SymColor secondLineColor, float secondThickness, LineJoinMode lineJoin, LineCapMode lineCap)
        {
            CheckModifiable();
            this.secondLineColor = secondLineColor;
            this.secondThickness = secondThickness;
            this.secondLineJoin = lineJoin;
            this.secondLineCap = lineCap;
        }

        public void SetDashInfo(DashInfo dashInfo)
        {
            CheckModifiable();
            this.dashInfo = dashInfo;
            isDashed = (dashInfo.gapLength > 0 || dashInfo.secondaryMiddleLength > 0 || dashInfo.secondaryEndLength > 0);
        }

        public void SetGlyphs(GlyphInfo[] glyphs)
        {
            CheckModifiable();
            this.glyphs = glyphs;
        }

        public void SetDoubleLines(DoubleLineInfo doubleLineInfo)
        {
            CheckModifiable();
            isDoubleLine = true;
            this.doubleLines = doubleLineInfo;
        }

        public void SetShortening(ShortenInfo shortenInfo)
        {
            CheckModifiable();
            this.shortenInfo = shortenInfo;
        }

        internal override bool HasDashes
        {
            get
            {
                if (isDashed)
                    return true;
                if (isDoubleLine && (doubleLines.doubleFillDashed || doubleLines.doubleLeftDashed || doubleLines.doubleRightDashed))
                    return true;

                return false;
            }
        }

        public override SymDef CopyToMap(Map map)
        {
            var newSymDef = new LineSymDef(Name, SymbolId, map.SymColorFromSymColor(lineColor), thickness, lineJoin, lineCap);
            if (secondLineColor != null)
                newSymDef.SetSecondLine(map.SymColorFromSymColor(secondLineColor), secondThickness, secondLineJoin, secondLineCap);
            if (isDashed)
                newSymDef.SetDashInfo(dashInfo);
            if (isDoubleLine) {
                DoubleLineInfo doubleLineInfo = doubleLines;
                doubleLineInfo.doubleFillColor = map.SymColorFromSymColor(doubleLineInfo.doubleFillColor);
                doubleLineInfo.doubleLeftColor = map.SymColorFromSymColor(doubleLineInfo.doubleLeftColor);
                doubleLineInfo.doubleRightColor = map.SymColorFromSymColor(doubleLineInfo.doubleRightColor);
                newSymDef.SetDoubleLines(doubleLineInfo);
            }
            if (glyphs != null) {
                GlyphInfo[] newGlyphs = new GlyphInfo[glyphs.Length];
                for (int i = 0; i < glyphs.Length; ++i) {
                    newGlyphs[i] = glyphs[i];
                    newGlyphs[i].glyph = newGlyphs[i].glyph.CopyToMap(map);
                }
                newSymDef.SetGlyphs(newGlyphs);
            }
            if (IsShortened)
                newSymDef.SetShortening(shortenInfo);

            map.AddSymdef(newSymDef);
            return newSymDef;
        }

        void CreatePens(IGraphicsTarget g, LineSymDefPens pens, bool highlight)
        {
            if (thickness > 0.0F && lineColor != null) {
                CmykColor mainColorValue = highlight ? map.HighlightColor : lineColor.ColorValue;

                if (!g.HasPen(pens.mainPen)) {
                    if (shortenInfo.shortenBeginning > 0 && shortenInfo.pointyEnds) {
                        // Always use round line cap with pointy ends.
                        g.CreatePen(pens.mainPen, mainColorValue, thickness, LineCapMode.Round, lineJoin, GraphicsUtil.MITER_LIMIT);
                    }
                    else {
                        g.CreatePen(pens.mainPen, mainColorValue, thickness, lineCap, lineJoin, GraphicsUtil.MITER_LIMIT);
                    }
                }

                if (shortenInfo.pointyEnds) {
                    if (!g.HasBrush(pens.pointyEndsBrush)) {
                        g.CreateSolidBrush(pens.pointyEndsBrush, mainColorValue);
                    }
                }
            }

            if (secondLineColor != null && secondThickness > 0) {
                CmykColor secondLineColorValue = highlight ? map.HighlightColor : secondLineColor.ColorValue;

                if (!g.HasPen(pens.secondPen)) {
                    g.CreatePen(pens.secondPen, secondLineColorValue, secondThickness, secondLineCap, secondLineJoin, GraphicsUtil.MITER_LIMIT);
                }
            }

            if (isDoubleLine) {
                if (doubleLines.doubleFillColor != null) {
                    CmykColor doubleFillColorValue = highlight ? map.HighlightColor : doubleLines.doubleFillColor.ColorValue;

                    if (!g.HasPen(pens.doubleFillPen)) {
                        GraphicsUtil.CreateSolidPen(g, pens.doubleFillPen, doubleFillColorValue, doubleLines.doubleThick, LineStyle.Mitered);
                    }
                }
                if (doubleLines.doubleLeftColor != null && doubleLines.doubleLeftWidth > 0.0F) {
                    CmykColor doubleLeftColorValue = highlight ? map.HighlightColor : doubleLines.doubleLeftColor.ColorValue;

                    if (!g.HasPen(pens.doubleLeftPen)) {
                        GraphicsUtil.CreateSolidPen(g, pens.doubleLeftPen, doubleLeftColorValue, doubleLines.doubleLeftWidth, LineStyle.FlatRounded);
                    }
                }
                if (doubleLines.doubleRightColor != null && doubleLines.doubleRightWidth > 0.0F) {
                    CmykColor doubleRightColorValue = highlight ? map.HighlightColor : doubleLines.doubleRightColor.ColorValue;
                    if (!g.HasPen(pens.doubleRightPen)) {
                        GraphicsUtil.CreateSolidPen(g, pens.doubleRightPen, doubleRightColorValue, doubleLines.doubleRightWidth, LineStyle.FlatRounded);
                    }
                }
            }
        }

        public override void FreeGdiObjects()
        {
        }


        // Does this symbol definition draw the given color. Used to determine
        // if this symbol definition draws into the current layer being draw.
        public override bool HasColor(SymColor color)
        {
            Debug.Assert(color != null);
            if (color.IsSpecialLayer)
                return false;

            if ((lineColor != null && color == lineColor && thickness > 0.0F) ||
                (secondLineColor != null && color == secondLineColor && secondThickness > 0.0F) ||
                (isDoubleLine && doubleLines.doubleFillColor != null && doubleLines.doubleFillColor == color) ||
                (isDoubleLine && doubleLines.doubleLeftWidth > 0.0F && doubleLines.doubleLeftColor != null && doubleLines.doubleLeftColor == color) ||
                (isDoubleLine && doubleLines.doubleRightWidth > 0.0F && doubleLines.doubleRightColor != null && doubleLines.doubleRightColor == color)) {
                return true;
            }

            if (glyphs != null) {
                foreach (GlyphInfo glyphInfo in glyphs) {
                    if (glyphInfo.glyph.HasColor(color))
                        return true;
                }
            }

            return false;
        }

        internal override void Draw(IGraphicsTarget g, SymPath path, SymColor color, RenderOptions renderOpts)
        {
            DrawCore(g, path, color, renderOpts, highlight: false);
        }

        internal override void DrawHighlight(IGraphicsTarget g, SymPath path, HighlightOptions highlightOptions)
        {
            if (highlightOptions.style == HighlightStyle.LowFidelity || HighlightThickness < SymbolHelpers.MinimumLineWidth(highlightOptions)) {
                SymbolHelpers.DrawLineHighlight(g, map, path, HighlightThickness, MainLineJoin, MainLineCap, highlightOptions);
            }
            else {
                RenderOptions renderOpts = new RenderOptions();
                renderOpts.minResolution = (float)highlightOptions.minResolution;
                DrawCore(g, path, null, renderOpts, true);
            }
        }

        // Draw this line symbol in the graphics along the path provided, with
        // the given color only.
        private void DrawCore(IGraphicsTarget g, SymPath path, SymColor color, RenderOptions renderOpts, bool highlight)
        {
            Debug.Assert(map != null);
            Debug.Assert(highlight || color != null);

            LineSymDefPens pens = highlight ? highlightPens : drawingPens;

            if (path.IsZeroLength)
                return;             // Don't draw anything for a zero-length path.

            CreatePens(g, pens, highlight);

            SymPath[] pathsWithShortening, pathsWithoutShortening;
            GetPathParts(path, out pathsWithShortening, out pathsWithoutShortening);

            if (lineColor != null && (highlight || color == lineColor) && thickness > 0.0F) {
                if (!isDashed) {
                    // simple drawing.
                    foreach (SymPath p in pathsWithShortening)
                        p.Draw(g, pens.mainPen);
                }
                else {
                    // Draw the dashed line.
                    foreach (SymPath p in pathsWithShortening)
                        DrawDashed(g, p, pens.mainPen, dashInfo, renderOpts, map.MapDistanceMetric);
                }
            }

            // Draw the pointy ends of the line. If shortening has removed the whole line, this is all the line!
            if (lineColor != null && (highlight || color == lineColor) && shortenInfo.pointyEnds && thickness > 0.0F && (shortenInfo.shortenBeginning > 0.0F || shortenInfo.shortenEnd > 0.0F)) 
                DrawPointyEnds(g, path, shortenInfo.shortenBeginning, shortenInfo.shortenEnd, thickness, pens.pointyEndsBrush);

            if (secondLineColor != null && (highlight || color == secondLineColor) && secondThickness > 0.0F && path != null) {
                // note that shortened path not used for secondary line, the full length path is.
                foreach (SymPath p in pathsWithoutShortening)
                    p.Draw(g, pens.secondPen);
            }

            // Double lines don't use the shortened path, but the full-length path.
            if (isDoubleLine) {
                if (doubleLines.doubleFillColor != null && (highlight || doubleLines.doubleFillColor == color)) {
                    if (doubleLines.doubleFillDashed) {
                        foreach (SymPath p in pathsWithoutShortening)
                            DrawDashed(g, p, pens.doubleFillPen, doubleLines.doubleDashes, renderOpts, map.MapDistanceMetric);
                    }
                    else {
                        foreach (SymPath p in pathsWithoutShortening)
                            p.Draw(g, pens.doubleFillPen);
                    }
                }

                if (doubleLines.doubleLeftColor != null && (highlight || doubleLines.doubleLeftColor == color) && doubleLines.doubleLeftWidth > 0.0F) {
                    foreach (SymPath subpath in path.GetSubpaths(SymPath.DOUBLE_LEFT_STARTSTOPFLAG)) {
                        float offsetRight = -(doubleLines.doubleThick + doubleLines.doubleLeftWidth) / 2F;
                        if (doubleLines.doubleLeftDashed) {
                            DrawDashedWithOffset(g, subpath, pens.doubleLeftPen, doubleLines.doubleDashes, offsetRight, GraphicsUtil.MITER_LIMIT, renderOpts, map.MapDistanceMetric);
                        }
                        else {  
                            SymPath leftPath = subpath.OffsetRight(offsetRight, GraphicsUtil.MITER_LIMIT);
                            leftPath.Draw(g, pens.doubleLeftPen);
                        }
                    }
                }

                if (doubleLines.doubleRightColor != null && (highlight || doubleLines.doubleRightColor == color) && doubleLines.doubleRightWidth > 0.0F) {
                    foreach (SymPath subpath in path.GetSubpaths(SymPath.DOUBLE_RIGHT_STARTSTOPFLAG)) {
                        float offsetRight = (doubleLines.doubleThick + doubleLines.doubleRightWidth) / 2F;
                        if (doubleLines.doubleRightDashed) {
                            DrawDashedWithOffset(g, subpath, pens.doubleRightPen, doubleLines.doubleDashes, offsetRight, GraphicsUtil.MITER_LIMIT, renderOpts, map.MapDistanceMetric);
                        }
                        else {  
                            SymPath rightPath = subpath.OffsetRight(offsetRight, GraphicsUtil.MITER_LIMIT);
                            rightPath.Draw(g, pens.doubleRightPen);
                        }
                    }
                }
            }

            if (glyphs != null) {
                foreach (GlyphInfo glyphInfo in glyphs) {
                    if (highlight || glyphInfo.glyph.HasColor(color)) {
                        GlyphInfo gi = glyphInfo;

                        if ((gi.location == GlyphLocation.Start || gi.location == GlyphLocation.End) && pathsWithShortening.Length > 0) {
                            // start and end only apply to the full path, ignoring cut parts and shortening.
                            DrawGlyphs(g, gi, path, path, color, renderOpts, highlight);
                        }
                        else {
                            for (int i = 0; i < pathsWithShortening.Length; ++i) {
                                SymPath shortPath = pathsWithShortening[i];
                                SymPath longPath = (i < pathsWithoutShortening.Length ? pathsWithoutShortening[i] : path);
                                DrawGlyphs(g, gi, shortPath, longPath, color, renderOpts, highlight);
                            }
                        }
                    }
                }
            }
        }

        // Apply the shortening and the start/stop flags to get an array of the paths to draw. This might be an empty array.
        private void GetPathParts(SymPath path, out SymPath[] withShortening, out SymPath[] withoutShortening) {
            withoutShortening = path.GetSubpaths(SymPath.MAIN_STARTSTOPFLAG);

            if (shortenInfo.shortenBeginning > 0.0F || shortenInfo.shortenEnd > 0.0F) {
                SymPath shortenedPath = path.Shorten(shortenInfo.shortenBeginning, shortenInfo.shortenEnd, map.MapDistanceMetric);
                if (shortenedPath == null)
                    withShortening = new SymPath[0] { };
                else
                    withShortening = shortenedPath.GetSubpaths(SymPath.MAIN_STARTSTOPFLAG);
            }
            else {
                withShortening = withoutShortening;
            }
        }

        // Unbelievably, it appears as though OCAD actually uses Bizarro distance metric (see SymPath) to do layout of dashes. This is so completely insane that I can't
        // even fathom it. 

        // Compute the distances along the line between dashes on the line. The distance are all deltas from the previous
        // distance. Can compute the start and end of the dashes/gaps (i.e., the lengths of the dashes and gaps), the distances
        // to the centers of the dashes (possibly excluded the first/last dash), or the distances to the centers of the gaps.
        // When computing the dash/gap lengths, the array returned is always of odd length, as follows:
        //    one element -- indicates that it should be fully drawn -- it's all dash.
        //    three elements -- lengths of the dash, gap, and dash
        //    five elements -- dash, gap, dash, gap, dash length, etc.
        //   it's possible that dashes may be of length zero, indicating that they shouldn't be drawn. For example, 
        //   if a very short line has a minimum # of gaps > 0, the line may be "all gap", which would
        //   be returns as a three-element array: 0, full-length, 0
        //
        //  Note: parameter "offset" is only used for MiddleDashCentersOffset! It does not otherwise affect the dashes.
        enum LocationKind { 
            DashAndGapLengths,      // gives lengths of both the dashes and the gaps
            DashCenters,                   // gives locations (as delta distances) of dash centers
            GapCenters,                     // gives locations of the gap centers
            GapCentersOffset,   // gives locations of gap centers, but offset forward by "offset", and missing the last 
            GapCentersDecrease,    // gives locations of the gap centers, but with decreasing distances based on final decrease value and a decrease toward both ends boolean
        }

        private class DecreaseRanges
        {
            public int startSmallToLarge, countSmallToLarge;
            public int startLargeToSmall, countLargeToSmall;
            public int startBoth, countBoth;
        }

        private static float[] ComputeDashDistances(SymPath path, LocationKind kind, SpacingMethod spacingMethod, 
                                                    float dashLength, float firstDashLength, float lastDashLength, float gapLength,
                                                    bool halfEndDashLengthWhenClosed, int minGaps, float offset, 
                                                    int numEndSecGaps, float lengthEndSecGaps, int numMiddleSecGaps, float lengthMiddleSecGaps, 
                                                    float decreaseLimit, bool decreaseBothEnds, DecreaseRanges decreaseRanges, SymPath.DistanceMetric distanceMetric)
        {
            LocationKind originalKind = kind;   // save original kind.

            // Get length of each segment, deliniated by corner points.
            PointKind[] pointkinds;
            float[] segmentLengths = path.GetCornerAndDashPointDistances(distanceMetric, out pointkinds);
            float[] lengthAtEnd = new float[segmentLengths.Length];
            float[][] dashDistances = new float[segmentLengths.Length][];
            int segmentSmallToLarge = -1, segmentLargeToSmall = -1, segmentBothEnds = -1;  // segment numbers with different decrease kinds.

            // For dash centers, first compute the dashes, then find the centers.
            if (kind == LocationKind.DashCenters)
                kind = LocationKind.DashAndGapLengths;  

            // Respect the halfEndDashLengthWhenClosed flag.
            if (path.IsClosedCurve && halfEndDashLengthWhenClosed) {
                firstDashLength /= 2.0F;
                lastDashLength /= 2.0F;
            }

            // Determine longest segment. This is the one that minGaps applies to.
            int segmentLongest = 0;
            float longestSegmentLength = 0;
            for (int i = 0; i < dashDistances.Length; ++i) {
                if (segmentLengths[i] >= longestSegmentLength) {
                    longestSegmentLength = segmentLengths[i];
                    segmentLongest = i;
                }
            }

            // Compute dash distances for each segment of the path.
            int totalDistances = 0;
            int minGapsLeft = minGaps;
            for (int i = 0; i < dashDistances.Length; ++i) {
                bool firstPointIsDashPoint = (i != 0 && pointkinds[i] == PointKind.Dash);
                bool lastPointIsDashPoint = (i < dashDistances.Length - 1 && pointkinds[i + 1] == PointKind.Dash);

                // Open mapper doesn't half dash points if there are secondary gaps.
                if (spacingMethod != SpacingMethod.OCAD && numMiddleSecGaps > 0) {
                    firstPointIsDashPoint = lastPointIsDashPoint = false;
                }

                // Compute first and last lengths of the segment in question.
                float firstLength, lastLength;
                if (firstPointIsDashPoint)
                    firstLength = dashLength / 2;
                else
                    firstLength = firstDashLength;

                if (lastPointIsDashPoint)
                    lastLength = dashLength / 2;
                else
                    lastLength = lastDashLength;

                // Note that minGaps is only required on the LONGEST segment. This is because that the minGaps requirement is for
                // all segments together, not each segment. Except for Open Mapper mid symbols.
                int numGaps;

                // In a multi-segment line, set decreasing for first/last segment only.
                DecreaseKind decreaseKind = DecreaseKind.None;
                if (kind == LocationKind.GapCentersDecrease) {
                    if (decreaseBothEnds) {
                        if (dashDistances.Length == 1) {
                            decreaseKind = DecreaseKind.BothEnds;
                            segmentBothEnds = i;
                        }
                        else if (i == 0) {
                            decreaseKind = DecreaseKind.SmallToLarge;
                            segmentSmallToLarge = i;
                        }
                        else if (i == dashDistances.Length - 1) {
                            decreaseKind = DecreaseKind.LargeToSmall;
                            segmentLargeToSmall = i;
                        }
                    }
                    else {
                        if (i == dashDistances.Length - 1) {
                            decreaseKind = DecreaseKind.LargeToSmall;
                            segmentLargeToSmall = i;
                        }
                    }
                }

                int minGapsThisSegment = 0;
                if (i == segmentLongest || segmentLengths[i] >= segmentLengths[segmentLongest] - 0.1 || spacingMethod == SpacingMethod.OpenMapperMidSymbols)
                    minGapsThisSegment = minGaps;

                dashDistances[i] = ComputeDashDistances(segmentLengths[i], kind, spacingMethod, dashLength, firstLength, lastLength, gapLength,
                                                        minGapsThisSegment, offset, 
                                                        numEndSecGaps, lengthEndSecGaps, numMiddleSecGaps, lengthMiddleSecGaps, 
                                                        decreaseLimit, decreaseKind, 
                                                        out lengthAtEnd[i], out numGaps);

                totalDistances += dashDistances[i].Length;

                // The last dash and first dash of adjacent parts merge together.
                if (kind == LocationKind.DashAndGapLengths && i > 0)
                    --totalDistances;
            }

            float[] distances;

            if (dashDistances.Length == 1) {
                if (segmentSmallToLarge == 0) {
                    decreaseRanges.startSmallToLarge = 0; decreaseRanges.countSmallToLarge = dashDistances[0].Length;
                }
                else if (segmentLargeToSmall == 0) {
                    decreaseRanges.startLargeToSmall = 0; decreaseRanges.countLargeToSmall = dashDistances[0].Length;
                }
                else if (segmentBothEnds == 0) {
                    decreaseRanges.startBoth = 0; decreaseRanges.countBoth = dashDistances[0].Length;
                }

                distances = dashDistances[0];
            }
            else {
                // Combine the distances from each segment into a single array. For dash and gap lengths, combine the 
                // dash lengths around segment boundaries.
                distances = new float[totalDistances];

                int index = 0;
                for (int i = 0; i < dashDistances.Length; ++i) {
                    Array.Copy(dashDistances[i], 0, distances, index, dashDistances[i].Length);

                    if (decreaseRanges != null) {
                        if (segmentSmallToLarge == i) {
                            decreaseRanges.startSmallToLarge = index; decreaseRanges.countSmallToLarge = dashDistances[i].Length;
                        }
                        else if (segmentLargeToSmall == i) {
                            decreaseRanges.startLargeToSmall = index; decreaseRanges.countLargeToSmall = dashDistances[i].Length;
                        }
                        else if (segmentBothEnds == i) {
                            decreaseRanges.startBoth = index; decreaseRanges.countBoth = dashDistances[i].Length;
                        }
                    }

                    if (i > 0 && dashDistances[i].Length > 0) {
                        distances[index] += lengthAtEnd[i - 1];
                    }
                    if (i > 0 && (dashDistances[i].Length == 0 || (kind == LocationKind.DashAndGapLengths && dashDistances[i].Length == 1)))
                        lengthAtEnd[i] += lengthAtEnd[i - 1];

                    index += dashDistances[i].Length;
                    if (kind == LocationKind.DashAndGapLengths)
                        --index;
                }
            }

            if (kind == LocationKind.DashAndGapLengths)
                distances = RemoveZeroGaps(distances);

            if (originalKind == LocationKind.DashCenters) {
                distances = ComputeDashCenters(distances, path.IsClosedCurve);
            }

            return distances;
        }

        private enum DecreaseKind { None, SmallToLarge, LargeToSmall, BothEnds }

        // Computes the dash distances for a single segment. Also returns the distance from the last location to the end of the segment.
        // If LocationKind is DashesAndGapLengths, this is a duplicate of the last array element.
        private static float[] ComputeDashDistances(float pathLength, LocationKind kind, SpacingMethod spacingMethod, 
                                                    float dashLength, float firstDashLength, float lastDashLength, float gapLength, int minGaps, float offset, 
                                                    int numEndSecGaps, float lengthEndSecGaps, int numMiddleSecGaps, float lengthMiddleSecGaps, float decreaseLimit, DecreaseKind decreaseKind, 
                                                    out float lengthAtEnd, out int actualGaps)
        {
            int numGaps;		         // actual number of gaps in the line
            float actualDashLength;      // actual length of each dash
            float actualFirstDashLength; // actual length of first dash
            float actualLastDashLength;  // actual last dash length.

            if (decreaseKind == DecreaseKind.None && kind == LocationKind.GapCentersDecrease)
                kind = LocationKind.GapCenters;

            if (kind == LocationKind.GapCentersDecrease) {
                // Computer number of dashes based on average dash length.
                dashLength = (dashLength + (dashLength * decreaseLimit)) / 2;
            }

            // DashCenters should be done at a higher level.
            if (kind == LocationKind.DashCenters)
                throw new ArgumentException("Should not pass DashCenters to this function");

            // Computer the number of gaps
            // The number of gaps is adjusted so that the gap lengths are always preserved exactly, and the dash lengths are as close as possible to the actual dash lengths.
            if (dashLength + gapLength > 0) {
                if (spacingMethod == SpacingMethod.OCAD) {
                    if (pathLength - firstDashLength - lastDashLength <= gapLength) {
                        numGaps = (minGaps >= 1 || pathLength >= firstDashLength / 2 + lastDashLength / 2 + gapLength) ? 1 : 0;
                    }
                    else {
                        numGaps = (int)Math.Round((pathLength + dashLength - firstDashLength - lastDashLength) / (dashLength + gapLength));
                    }
                    //if (firstDashLength == 0 && lastDashLength == 0 && pathLength < dashLength / 2) {
                    //    numGaps = 0;
                    //}
                }
                else if (spacingMethod == SpacingMethod.OpenMapperMidSymbols) {
                    // Taken roughly from LineSymbol::createMidSymbolRenderables in OpenMapper. 

                    int mid_symbol_num_gaps = 0; // mid_symbols_per_spot - 1;
                    double segment_length_f = dashLength;
                    double end_length_f = (firstDashLength + lastDashLength) / 2; 
                    double end_length_twice_f = firstDashLength + lastDashLength;
                    double mid_symbol_distance_f = 0;   // 0.001 * mid_symbol_distance;
                    double mid_symbols_length = mid_symbol_num_gaps * mid_symbol_distance_f;


                    // The total length of the current continuous part
                    double length = pathLength;
                    // The length which is available for placing mid symbols
                    double segmented_length = Math.Max(0.0, length - end_length_twice_f) - mid_symbols_length;
                    // The number of segments to be created by mid symbols
                    double segment_count_raw = Math.Max((end_length_f == 0) ? 1.0 : 0.0, (segmented_length / (segment_length_f + mid_symbols_length)));
                    int lower_segment_count = (int)Math.Floor(segment_count_raw);
                    int higher_segment_count = (int)Math.Ceiling(segment_count_raw);
                    int segment_count;

                    if (end_length_f > 0) {
                        double lower_abs_deviation = Math.Abs(length - lower_segment_count * segment_length_f - (lower_segment_count + 1) * mid_symbols_length - end_length_twice_f);
                        double higher_abs_deviation = Math.Abs(length - higher_segment_count * segment_length_f - (higher_segment_count + 1) * mid_symbols_length - end_length_twice_f);
                        segment_count = (lower_abs_deviation >= higher_abs_deviation) ? higher_segment_count : lower_segment_count;

                        if (higher_segment_count > 0 || length > end_length_twice_f - 0.5 * (segment_length_f + mid_symbols_length))
                            numGaps = segment_count + 1;
                        else
                            numGaps = 0;
                    }
                    else {
                        // end_length == 0
                        double lower_segment_deviation = Math.Abs(length - lower_segment_count * segment_length_f - (lower_segment_count + 1) * mid_symbols_length) / lower_segment_count;
                        double higher_segment_deviation = Math.Abs(length - higher_segment_count * segment_length_f - (higher_segment_count + 1) * mid_symbols_length) / higher_segment_count;
                        segment_count = (lower_segment_deviation > higher_segment_deviation) ? higher_segment_count : lower_segment_count;
                        numGaps = segment_count + 1;
                    }

                }
                else {
                    // Taken roughly from LineSymbol::createDashGroups in OpenMapper. 
                    int numShortDash = ((firstDashLength < 0.6F * dashLength) ? 1 : 0) + ((lastDashLength < 0.6F * dashLength) ? 1 : 0);
                    double numberGaps = (pathLength + dashLength - firstDashLength - lastDashLength + gapLength * numShortDash) / (dashLength + gapLength);
                    double floorNumberGaps = Math.Floor(numberGaps);
                    double ceilNumberGaps = Math.Ceiling(numberGaps);
                    double floorDeviation = (pathLength + gapLength - (floorNumberGaps + 1) * (dashLength + gapLength)) / (floorNumberGaps + 1);
                    double ceilDeviation = - (pathLength + gapLength - (ceilNumberGaps + 1) * (dashLength + gapLength)) / (ceilNumberGaps + 1);
                    numGaps = (int) ((floorDeviation > ceilDeviation) ? ceilNumberGaps : floorNumberGaps);

                    double minimumOptimumLength = (2 * (dashLength + gapLength));
                    double minimumOptimumNumDashes = ((numMiddleSecGaps + 1) * 2.0 - numShortDash * 0.5);
                    double switchDeviation = 0.2 * (dashLength + gapLength) / (numMiddleSecGaps + 1);
                    if (floorNumberGaps == 0 && pathLength <  minimumOptimumLength - minimumOptimumNumDashes * switchDeviation) {
                        numGaps = 0;
                    }

                    if (numGaps < minGaps)
                        numGaps = minGaps;
                }
            }
            else
                numGaps = 0;

            // Enforce minimum gaps.
            if (numGaps < minGaps)
                numGaps = minGaps;

            // Given the number of gaps, compute the lengths of the dashes. Gaps are always at their nominal length.
            if (numGaps == 0)
                actualDashLength = actualFirstDashLength = actualLastDashLength = pathLength;
            else {
                if (numGaps == 1 && firstDashLength == 0 && lastDashLength == 0)
                    actualDashLength = actualFirstDashLength = actualLastDashLength = 0;
                else {
                    actualDashLength = (pathLength - gapLength * numGaps) / (numGaps - 1 + firstDashLength / dashLength + lastDashLength / dashLength);
                    actualFirstDashLength = actualDashLength * firstDashLength / dashLength;
                    actualLastDashLength = actualDashLength * lastDashLength / dashLength;
                }
            }
            actualGaps = numGaps;

            if (actualDashLength <= 0) {
                // Special case: the path is "all gap".
                if (kind == LocationKind.GapCenters) {
                    if (numGaps > 1) {
                        // multiple gaps. Put at either end.
                        lengthAtEnd = 0;
                        return new float[2] { 0, pathLength }; // in middle of single gap
                    }
                    //else if (firstDashLength == 0 && lastDashLength == 0) {
                    //    lengthAtEnd = pathLength;
                    //    return new float[1] { 0 };
                    //}
                    else {
                        lengthAtEnd = pathLength / 2;
                        return new float[1] { pathLength / 2 }; // in middle of single gap
                    }
                }
                else if (kind == LocationKind.DashCenters || kind == LocationKind.GapCentersOffset) {
                    lengthAtEnd = pathLength;
                    return new float[0];
                }
                else {
                    lengthAtEnd = 0.0F;
                    return new float[3] { 0.0F, pathLength, 0.0F };
                }
            }

            int index = 0;
            float[] locations;

            if (kind == LocationKind.DashCenters) {
                locations = new float[numGaps + 1];

                locations[index++] = actualFirstDashLength / 2;
                lengthAtEnd = actualLastDashLength / 2;
                if (numGaps == 0)
                    return locations;
                else if (numGaps == 1)
                    locations[index++] = actualLastDashLength + gapLength;
                else {
                    locations[index++] = actualFirstDashLength / 2 + gapLength + actualDashLength / 2;
                    for (int i = 2; i < numGaps; ++i)
                        locations[index++] = actualDashLength + gapLength;
                    locations[index++] = actualLastDashLength / 2 + gapLength + actualDashLength / 2;
                }
            }
            else if (kind == LocationKind.GapCenters) {
                locations = new float[numGaps];

                if (numGaps == 0)
                    lengthAtEnd = pathLength;
                else {
                    lengthAtEnd = actualLastDashLength + gapLength / 2;
                    locations[index++] = actualFirstDashLength + gapLength / 2;
                    for (int i = 1; i < numGaps; ++i)
                        locations[index++] = actualDashLength + gapLength;
                }
            }
            else if (kind == LocationKind.GapCentersDecrease) {
                locations = new float[numGaps];

                if (numGaps == 0)
                    lengthAtEnd = pathLength;
                else {
                    lengthAtEnd = actualLastDashLength + gapLength / 2;
                    locations[index++] = actualFirstDashLength + gapLength / 2;

                    if (numGaps > 1) {
                        if (decreaseKind == DecreaseKind.BothEnds) {
                            float currentDashLength = 2 * actualDashLength / (1 + decreaseLimit);
                            if (numGaps % 2 == 1) {
                                float dashDelta = (currentDashLength - decreaseLimit * currentDashLength) / (numGaps - 1);
                                currentDashLength = decreaseLimit * currentDashLength;

                                for (int i = 1; i < numGaps; ++i) {
                                    if (i == (numGaps + 1) / 2)
                                        dashDelta = -dashDelta;

                                    currentDashLength += dashDelta;
                                    locations[index++] = currentDashLength + gapLength;
                                    currentDashLength += dashDelta;
                                }
                            }
                            else {
                                float dashDelta;
                                if (numGaps == 2) 
                                    dashDelta = 0;
                                else {
                                    dashDelta = currentDashLength * (((1.0F - (1.0F - decreaseLimit) / numGaps) - decreaseLimit) / (numGaps - 2));
                                    currentDashLength = decreaseLimit * currentDashLength;
                                }

                                for (int i = 1; i < numGaps; ++i) {
                                    if (i != numGaps / 2)
                                        currentDashLength += dashDelta;
                                    locations[index++] = currentDashLength + gapLength;

                                    if (i == numGaps / 2)
                                        dashDelta = -dashDelta;
                                    else
                                        currentDashLength += dashDelta;
                                }
                            }
                        }
                        else if (decreaseKind == DecreaseKind.LargeToSmall) {
                            float currentDashLength = 2 * actualDashLength / (1 + decreaseLimit);
                            float dashDelta = -(currentDashLength - decreaseLimit * currentDashLength) / ((numGaps-1) * 2);

                            for (int i = 1; i < numGaps; ++i) {
                                currentDashLength += dashDelta;
                                locations[index++] = currentDashLength + gapLength;
                                currentDashLength += dashDelta;
                            }
                        }
                        else if (decreaseKind == DecreaseKind.SmallToLarge) {
                            float currentDashLength = 2 * actualDashLength / (1 + decreaseLimit);
                            float dashDelta = (currentDashLength - decreaseLimit * currentDashLength) / ((numGaps - 1) * 2);
                            currentDashLength -= (currentDashLength - decreaseLimit * currentDashLength);

                            for (int i = 1; i < numGaps; ++i) {
                                currentDashLength += dashDelta;
                                locations[index++] = currentDashLength + gapLength;
                                currentDashLength += dashDelta;
                            }
                        }
                    }
                }
            }
            else if (kind == LocationKind.GapCentersOffset) {
                if (numGaps <= 1) {
                    locations = new float[0];
                    lengthAtEnd = pathLength;
                }
                else {
                    locations = new float[numGaps - 1];
                    lengthAtEnd = actualLastDashLength + gapLength / 2 + actualDashLength + gapLength - offset;
                    locations[index++] = actualFirstDashLength + gapLength / 2 + offset;
                    for (int i = 1; i < numGaps - 1; ++i)
                        locations[index++] = actualDashLength + gapLength;
                }
            }
            else {
                Debug.Assert(kind == LocationKind.DashAndGapLengths);

                List<float> locationList = new List<float>(numGaps * 2 + 1);

                if (numGaps > 0) {
                    // Add first dash (with secondary gaps as appropriate).
                    if (numEndSecGaps > 0 && (actualFirstDashLength >= firstDashLength / 2))
                        locationList.AddRange(AddSecondaryGaps(actualFirstDashLength, numEndSecGaps, lengthEndSecGaps));  // add with secondary gaps.
                    else
                        locationList.Add(actualFirstDashLength); // no secondary gaps

                    // Add middle dashes and gaps to them (with secondary gaps as appropriate)
                    for (int i = 1; i < numGaps; ++i) {
                        locationList.Add(gapLength);

                        if (numMiddleSecGaps > 0 && (actualDashLength >= dashLength / 2))
                            locationList.AddRange(AddSecondaryGaps(actualDashLength, numMiddleSecGaps, lengthMiddleSecGaps));  // add with secondary gaps
                        else
                            locationList.Add(actualDashLength);
                    }

                    // Add gap to last dash.
                    locationList.Add(gapLength);
                }

                // Add last dash (with secondary gaps as appropriate).
                if (numEndSecGaps > 0 && (actualLastDashLength >= lastDashLength / 2) && numGaps > 0)
                    locationList.AddRange(AddSecondaryGaps(actualLastDashLength, numEndSecGaps, lengthEndSecGaps));   // add with secondary gaps.
                else
                    locationList.Add(actualLastDashLength);

                // Convert to array.
                locations = locationList.ToArray();
                lengthAtEnd = locationList[locationList.Count - 1];
            }

            return locations;
        }

        // Adds secondary gaps to a dash of length dashLength, and returns an array of {dash-gap-dash} lengths. Could be array of length 1 if gaps aren't added.
        internal static float[] AddSecondaryGaps(float dashLength, int countGaps, float gapLength)
        {
            if (dashLength > countGaps * gapLength) {
                // we can create the secondary gaps.
                float[] newDistances = new float[countGaps * 2 + 1];
                float newDashLength = (dashLength - countGaps * gapLength) / (countGaps + 1);
                newDistances[0] = newDashLength;
                for (int k = 0; k < countGaps; ++k) {
                    newDistances[k * 2 + 1] = gapLength;
                    newDistances[k * 2 + 2] = newDashLength;
                }
                return newDistances;
            }
            else {
                // not room for the new gaps.
                return new float[1] { dashLength };
            }
        }

        // Given an array of dash lengths and gap lengths, remove zero-length gaps. Returns the same parameter if none to remove.
        static float[] RemoveZeroGaps(float[] distancesAndGaps)
        {
            bool hasZeroLengthGaps = false;

            for (int i = 1; i < distancesAndGaps.Length - 2; i += 2)
                if (distancesAndGaps[i] == 0)
                    hasZeroLengthGaps = true;

            if (!hasZeroLengthGaps)
                return distancesAndGaps;       // no gaps to remove.

            // Remove gaps and coellesce distances.
            List<float> list = new List<float>(distancesAndGaps);
            for (int i = distancesAndGaps.Length - 2; i >= 1; i -= 2) {
                if (list[i] == 0) {
                    list[i - 1] = list[i - 1] + list[i + 1];
                    list.RemoveAt(i + 1);
                    list.RemoveAt(i);
                }
            }

            return list.ToArray();
        }

        // Given an array of dash lengths and gap lengths, compute an array of spacing of the dash centers.
        // If "closed" is true, it is assume that first and last dashes are combined and uses the combined centers.
        static float[] ComputeDashCenters(float[] distancesAndGaps, bool closed)
        {
            // Special case -- one dash.
            if (distancesAndGaps.Length == 1) {
                return new float[1] { distancesAndGaps[0] / 2 };
            }

            List<float> dashCenters = new List<float>(distancesAndGaps.Length / 2);
            float len = 0;

            // First dash is special.
            float? firstPos;
            if (closed) {
                if (distancesAndGaps[0] >= distancesAndGaps[distancesAndGaps.Length - 1])
                    firstPos = distancesAndGaps[0] - (distancesAndGaps[0] + distancesAndGaps[distancesAndGaps.Length - 1])  / 2;
                else
                    firstPos = null;
            }
            else {
                firstPos = distancesAndGaps[0] / 2;
            }

            if (firstPos.HasValue)
                dashCenters.Add(firstPos.Value);
            len = distancesAndGaps[0] - (firstPos ?? 0);

            // Do the other dashes. "len" is the amount of the previous dash still to be used.
            for (int i = 1; i < (distancesAndGaps.Length + 1) / 2; ++i) {
                int gapIndex = i * 2 - 1;
                float dashLength = distancesAndGaps[gapIndex + 1];
                float gapLength = distancesAndGaps[gapIndex];
                if (dashLength > 0) {
                    if (closed && i == ((distancesAndGaps.Length - 1) / 2)) {
                        if (distancesAndGaps[0] < distancesAndGaps[distancesAndGaps.Length - 1])
                            dashCenters.Add(len + gapLength + (distancesAndGaps[0] + distancesAndGaps[distancesAndGaps.Length - 1])  / 2);
                    }
                    else {
                        dashCenters.Add(len + gapLength + dashLength / 2);
                        len = dashLength / 2;
                    }
                }
                else {
                    len += dashLength + gapLength;
                }
            }

            return dashCenters.ToArray();
        }

        private static void DrawDashed(IGraphicsTarget g, SymPath path, object penKey, DashInfo dashes, RenderOptions renderOpts, SymPath.DistanceMetric distanceMetric)
        {
            DrawDashedWithOffset(g, path, penKey, dashes, 0, 1, renderOpts, distanceMetric);
        }

        private static void DrawDashedWithOffset(IGraphicsTarget g, SymPath path, object penKey, DashInfo dashes, float offsetRight, float miterLimit, RenderOptions renderOpts, SymPath.DistanceMetric distanceMetric)
        {
            float[] distances;

            distances = ComputeDashDistances(path, LocationKind.DashAndGapLengths, dashes.spacingMethod, dashes.dashLength, dashes.firstDashLength, dashes.lastDashLength, dashes.gapLength, dashes.halfEndDashLengthWhenClosed, dashes.minGaps, 0, dashes.secondaryEndGaps, dashes.secondaryEndLength, dashes.secondaryMiddleGaps, dashes.secondaryMiddleLength, 1.0F, false, null, distanceMetric);

            if (distances.Length == 0 || (dashes.gapLength < renderOpts.minResolution && (dashes.secondaryMiddleGaps == 0 || dashes.secondaryMiddleLength < renderOpts.minResolution) && (dashes.secondaryEndGaps == 0 || dashes.secondaryEndLength < renderOpts.minResolution))) {
                // No dashes, or the dashes are too small to be visible. Draw solid.
                if (offsetRight != 0) {
                    SymPath offsetPath = path.OffsetRight(offsetRight, miterLimit);
                    offsetPath.Draw(g, penKey);
                }
                else
                    path.Draw(g, penKey);
            }
            else {
                path.DrawDashedOffset(g, penKey, distances, 0, offsetRight, miterLimit, distanceMetric);
            }
        }

        // Helper for drawing or highlighting a glyph.
        private void DrawGlyph(IGraphicsTarget g, Glyph glyph, PointF location, float angle, Matrix extraTransform, SymColor color, RenderOptions renderOpts, bool highlight)
        {
            if (highlight) {
                glyph.DrawHighlight(g, location, angle, extraTransform, null, map.GetHighlightBrush(g));
            }
            else {
                glyph.Draw(g, location, angle, extraTransform, null, color, null, renderOpts);
            }
        }

        // Draw the glyphs along the path. "longPath" is the same as path unless shortening of the ends has occurred, in which case
        // path is the shortened path (used for all glyphs except start and end), and longPath is used for the start and end.
        private void DrawGlyphs(IGraphicsTarget g, GlyphInfo glyphInfo, SymPath path, SymPath longPath, SymColor color, RenderOptions renderOpts, bool highlight)
        {
            float[] distances;
            PointF[] points;
            float[] perpAngles, subtendedAngles;
            float firstDistance;
            bool ignoreDashCornersAtEnds = false;

            Debug.Assert(highlight || color != null);

            // Figure out the distances of the glyphs along the line.
            switch (glyphInfo.location) {
            case GlyphLocation.CornersIgnoreEnds:
            case GlyphLocation.DashPointsIgnoreEnds:
                ignoreDashCornersAtEnds = true;
                goto case GlyphLocation.Corners;

            case GlyphLocation.Corners:
            case GlyphLocation.DashPoints:
                // Corner points/dash points are done somewhat differently. Only can have 1 symbol.
                // There is an interesting feature in OCAD where the dimensions of corner glyphs are stretched a certain amount at
                // very acute angles. This is so that power line crossbars always extend beyond the power lines themselves.
                // This is handled by stretching the glyph based on the subtended angle at the corner.
                points = path.FindCornerOrDashPoints((glyphInfo.location == GlyphLocation.DashPoints || glyphInfo.location == GlyphLocation.DashPointsIgnoreEnds) ? PointKind.Dash : PointKind.Corner,
                                                     ignoreDashCornersAtEnds, out perpAngles, out subtendedAngles);

                if (points != null) {
                    for (int i = 0; i < points.Length; ++i) {
                        float subtendedAngle = subtendedAngles[i];
                        float stretch;
                        if (subtendedAngle != 0 && !glyphInfo.noScaleAtCorners && (glyphInfo.location == GlyphLocation.Corners || glyphInfo.location == GlyphLocation.CornersIgnoreEnds))
                            stretch = Geometry.MiterFactor(subtendedAngle);   
                        else
                            stretch = 1.0F;
                        stretch = Math.Min(stretch, CORNER_GLYPH_STRETCH_LIMIT);

                        Matrix stretchMatrix = new Matrix();
                        stretchMatrix.Scale(1.0F, stretch);

                        DrawGlyph(g, glyphInfo.glyph, points[i], perpAngles[i] + 90.0F, stretchMatrix, color, renderOpts, highlight);
                    }
                }
                return;

            case GlyphLocation.Spaced:
                distances = ComputeDashDistances(path, LocationKind.GapCenters, glyphInfo.spacingMethod, glyphInfo.distance, glyphInfo.firstDistance, glyphInfo.lastDistance, 0, false, glyphInfo.minimum, 0, 0, 0, 0, 0, 1.0F, false, null, map.MapDistanceMetric);
                break;
            case GlyphLocation.SpacedOffset:
                distances = ComputeDashDistances(path, LocationKind.GapCentersOffset, glyphInfo.spacingMethod, glyphInfo.distance, glyphInfo.firstDistance, glyphInfo.lastDistance, 0, false, glyphInfo.minimum, glyphInfo.offset, 0, 0, 0, 0, 1.0F, false, null, map.MapDistanceMetric);
                break;
            case GlyphLocation.SpacedDecrease:
                DecreaseRanges decreaseRanges = new DecreaseRanges();
                distances = ComputeDashDistances(path, LocationKind.GapCentersDecrease, glyphInfo.spacingMethod, glyphInfo.distance, glyphInfo.firstDistance, glyphInfo.lastDistance, 0, false, glyphInfo.minimum, 0, 0, 0, 0, 0, glyphInfo.decreaseLimit, glyphInfo.decreaseBothEnds, decreaseRanges, map.MapDistanceMetric);

                if (distances != null && distances.Length > 0) {
                    firstDistance = distances[0];

                    for (int n = 0; n < glyphInfo.number; ++n) {
                        distances[0] = firstDistance - ((glyphInfo.number - 1 - n * 2) * (glyphInfo.spacing / 2.0F));

                        points = path.FindPointsAlongLine(distances, out perpAngles, map.MapDistanceMetric);

                        for (int i = 0; i < points.Length; ++i) {
                            float decreaseFactor = ComputeDecreaseFactor(i, decreaseRanges, glyphInfo.decreaseLimit);

                            if (decreaseFactor >= 0.001F) {
                                Matrix matrixTransform = new Matrix();
                                matrixTransform.Scale(decreaseFactor, decreaseFactor);
                                DrawGlyph(g, glyphInfo.glyph, points[i], perpAngles[i], matrixTransform, color, renderOpts, highlight);
                            }
                        }
                    }
                }
                
                return;
            case GlyphLocation.DashCenters:
                distances = ComputeDashDistances(path, LocationKind.DashCenters, dashInfo.spacingMethod, dashInfo.dashLength, dashInfo.firstDashLength, dashInfo.lastDashLength, dashInfo.gapLength, dashInfo.halfEndDashLengthWhenClosed, dashInfo.minGaps, 0, 0, 0, 0, 0, 1.0F, false, null, map.MapDistanceMetric);
                break;
            case GlyphLocation.GapCenters:
                // OCAD doesn't respect the "0 minimum gaps" for the symbols, although it does for the gaps. Always have at least one symbol. This is handled on import by having glyphInfo.minimum be 1.
                distances = ComputeDashDistances(path, LocationKind.GapCenters, dashInfo.spacingMethod, dashInfo.dashLength, dashInfo.firstDashLength, dashInfo.lastDashLength, dashInfo.gapLength, dashInfo.halfEndDashLengthWhenClosed, Math.Max(glyphInfo.minimum, dashInfo.minGaps), 0, 0, 0, 0, 0, 1.0F, false, null, map.MapDistanceMetric);
                break;
            case GlyphLocation.Start:
                distances = new float[1] { 0 };
                break;
            case GlyphLocation.End:
                distances = new float[1] { map.UseEuclideanMetric ? longPath.Length : longPath.BizzarroLength };
                break;
            default:
                Debug.Fail("bad glyph location");
                return;
            }

            if (distances == null || distances.Length == 0)
                return;
            firstDistance = distances[0];

            for (int n = 0; n < glyphInfo.number; ++n) {
                distances[0] = firstDistance - ((glyphInfo.number - 1 - n * 2) * (glyphInfo.spacing / 2.0F));

                if (glyphInfo.location != GlyphLocation.Start && glyphInfo.location != GlyphLocation.End) {
                    if (longPath.FirstPoint != path.FirstPoint && shortenInfo.shortenBeginning > 0) {
                        // This is a shortened path, convert distances back into the long path.
                        distances[0] += shortenInfo.shortenBeginning;
                        points = longPath.FindPointsAlongLine(distances, out perpAngles, map.MapDistanceMetric);
                    }
                    else {
                        points = path.FindPointsAlongLine(distances, out perpAngles, map.MapDistanceMetric);
                    }
                }
                else {
                    points = longPath.FindPointsAlongLine(distances, out perpAngles, map.MapDistanceMetric);
                }


                for (int i = 0; i < points.Length; ++i) {
                    DrawGlyph(g, glyphInfo.glyph, points[i], perpAngles[i], null, color, renderOpts, highlight);
                }
            }
        }

        // Compute the decrease factor to apply to a glyph at the given index, given the ranges of decreasing symbols and the decrease limit.
        private float ComputeDecreaseFactor(int index, DecreaseRanges decreaseRanges, float decreaseLimit)
        {
            int i;
            if (index >= decreaseRanges.startBoth && index - decreaseRanges.startBoth < decreaseRanges.countBoth) {
                i = index - decreaseRanges.startBoth;
                if (decreaseRanges.countBoth <= 2)
                    return decreaseLimit;
                else
                    return 1.0F - (Math.Abs(i - ((decreaseRanges.countBoth - 1) / 2F)) * (1 - decreaseLimit) / ((decreaseRanges.countBoth - 1) / 2F));
            }
            else if (index >= decreaseRanges.startLargeToSmall && index - decreaseRanges.startLargeToSmall < decreaseRanges.countLargeToSmall) {
                i = index - decreaseRanges.startLargeToSmall;
                if (i == 0)
                    return 1.0F;
                else
                    return 1.0F - (i * (1 - decreaseLimit) / (decreaseRanges.countLargeToSmall - 1));
            }
            else if (index >= decreaseRanges.startSmallToLarge && index - decreaseRanges.startSmallToLarge < decreaseRanges.countSmallToLarge) {
                i = index - decreaseRanges.startSmallToLarge;
                i = decreaseRanges.countSmallToLarge - 1 - i;
                if (i == 0)
                    return 1.0F;
                else
                    return 1.0F - (i * (1 - decreaseLimit) / (decreaseRanges.countSmallToLarge - 1));
            }
            else {
                return 1.0F;
            }
        }

        // Draw the pointy ends on a line.
        void DrawPointyEnds(IGraphicsTarget g, SymPath longPath, float pointyLengthStart, float pointyLengthEnd, float lineWidth, object brush)
        {
            // Get locations of points at the tip, half-way, and base of the pointy tips.
            float length = map.UseEuclideanMetric ? longPath.Length : longPath.BizzarroLength;
            float[] distances, angles;
            if (length >= pointyLengthStart + pointyLengthEnd) {
                distances = new float[6] { 0, pointyLengthStart / 2, pointyLengthStart / 2, length - pointyLengthEnd - pointyLengthStart, pointyLengthEnd / 2, pointyLengthEnd / 2 };
            }
            else {
                float scaleFactor = length / (pointyLengthStart + pointyLengthEnd);
                distances = new float[6] { 0, (pointyLengthStart / 2) * scaleFactor, (pointyLengthStart / 2) * scaleFactor, 0, (pointyLengthEnd / 2) * scaleFactor, (pointyLengthEnd / 2) * scaleFactor };
            }
            PointF[] pointsAlongPath = longPath.FindPointsAlongLine(distances, out angles, map.MapDistanceMetric);

            // Each pointy tip is composed of a polygon of 5 points.
            PointF[] tipCorners = new PointF[5];
            float midpointWidth = lineWidth * 0.666F;  // Makes a sort of curvy tip.

            if (pointyLengthStart > 0) {
                // Draw point at beginning.
                tipCorners[0] = pointsAlongPath[0];
                tipCorners[1] = Geometry.MoveDistance(pointsAlongPath[1], midpointWidth / 2, angles[1] - 90.0F);
                tipCorners[4] = Geometry.MoveDistance(pointsAlongPath[1], midpointWidth / 2, angles[1] + 90.0F);
                tipCorners[2] = Geometry.MoveDistance(pointsAlongPath[2], lineWidth / 2, angles[2] - 90.0F);
                tipCorners[3] = Geometry.MoveDistance(pointsAlongPath[2], lineWidth / 2, angles[2] + 90.0F);
                g.FillPolygon(brush, tipCorners, AreaFillMode.Winding);
            }

            if (pointyLengthEnd > 0) {
                // Draw point at end.
                tipCorners[0] = pointsAlongPath[5];
                tipCorners[1] = Geometry.MoveDistance(pointsAlongPath[4], midpointWidth / 2, angles[4] - 90.0F);
                tipCorners[4] = Geometry.MoveDistance(pointsAlongPath[4], midpointWidth / 2, angles[4] + 90.0F);
                tipCorners[2] = Geometry.MoveDistance(pointsAlongPath[3], lineWidth / 2, angles[3] - 90.0F);
                tipCorners[3] = Geometry.MoveDistance(pointsAlongPath[3], lineWidth / 2, angles[3] + 90.0F);
                g.FillPolygon(brush, tipCorners, AreaFillMode.Winding);
            }
        }

        // Calculate the bounding box
        internal override RectangleF CalcBounds(SymPath path)
        {
            float lineThickness = maxThickness;

            // If the path has sharp mitered corners, the thickness is increased.
            if (maxMiteredThickness > 0) {
                float miterThickness = Math.Min(path.MaxMiter, GraphicsUtil.MITER_LIMIT) * maxMiteredThickness;
                if (miterThickness > lineThickness)
                    lineThickness = miterThickness;
            }

            RectangleF box = path.BoundingBox;
            box.Inflate(lineThickness / 2, lineThickness / 2);
            return box;
        }
    }

    public abstract class AreaLikeSymDef: SymDef
    {
        public AreaLikeSymDef(string name, string symbolId)
            :base(name, symbolId)
        { }

        internal abstract float BorderHighlightThickness { get; }

        internal abstract bool HasDashes { get; }

        internal abstract RectangleF CalcBounds(SymPathWithHoles path);

        internal abstract void Draw(IGraphicsTarget g, SymPathWithHoles path, SymColor color, float angle, PointF rotationCenter, RenderOptions renderOpts);

        internal abstract void DrawHighlight(IGraphicsTarget g, SymPathWithHoles path, HighlightOptions options);

        internal abstract RectangleF HighlightBounds(SymPathWithHoles path, RectangleF boundingBox, HighlightOptions options);


    }

    public class AreaComboSymDef: AreaLikeSymDef
    {
        SymDef[] components;

        public AreaComboSymDef(string name, string symbolId, IEnumerable<SymDef> components)
            :base(name, symbolId)
        {
            this.components = components.ToArray();
        }

        public IEnumerable<SymDef> Components
        {
            get
            {
                return components.ToList().AsReadOnly();
            }
        }

        internal override float BorderHighlightThickness
        {
            get
            {
                float max = 0;
                foreach (SymDef component in components) {
                    LineLikeSymDef lineComponent = component as LineLikeSymDef;
                    max = Math.Max(max, lineComponent.HighlightThickness);
                }

                return max;
            }
        }

        internal override bool HasDashes
        {
            get
            {
                foreach (SymDef component in components) {
                    LineLikeSymDef lineComponent = component as LineLikeSymDef;
                    if (lineComponent != null && lineComponent.HasDashes)
                        return true;
                }

                return false;
            }
        }

        public override SymDef CopyToMap(Map map)
        {
            throw new NotImplementedException();
        }

        public override void FreeGdiObjects()
        {
        }

        public override bool HasColor(SymColor color)
        {
            foreach (SymDef component in components) {
                if (component.HasColor(color))
                    return true;
            }

            return false;
        }

        internal override RectangleF CalcBounds(SymPathWithHoles path)
        {
            RectangleF bounds = new RectangleF();
            bool first = true;
            foreach (SymDef component in components) {
                RectangleF componentBounds;

                if (component is LineLikeSymDef)
                    componentBounds = ((LineLikeSymDef)component).CalcBounds(path.MainPath);
                else
                    componentBounds = ((AreaLikeSymDef)component).CalcBounds(path);

                if (first)
                    bounds = componentBounds;
                else
                    bounds = RectangleF.Union(bounds, componentBounds);

                first = false;
            }

            return bounds;
        }

        internal override void Draw(IGraphicsTarget g, SymPathWithHoles path, SymColor color, float angle, PointF rotationCenter, RenderOptions renderOpts)
        {
            foreach (SymDef component in components) {
                if (component is LineLikeSymDef) {
                    LineLikeSymDef lineSymDef = (LineLikeSymDef)component;
                    // Draw main part of border.
                    lineSymDef.Draw(g, path.MainPath, color, renderOpts);

                    // Draw the holes.
                    if (path.Holes != null)
                        foreach (SymPath hole in path.Holes)
                            lineSymDef.Draw(g, hole, color, renderOpts);
                }
                else {
                    ((AreaLikeSymDef)component).Draw(g, path, color, angle, rotationCenter, renderOpts);
                }
            }
        }

        internal override void DrawHighlight(IGraphicsTarget g, SymPathWithHoles path, HighlightOptions options)
        {
            if (options.style == HighlightStyle.HighFidelity) {
                foreach (SymDef component in components) {
                    bool areaDrawn = false;

                    if (component is LineLikeSymDef) {
                        LineLikeSymDef lineSymDef = (LineLikeSymDef)component;
                        // Draw main part of border.
                        lineSymDef.DrawHighlight(g, path.MainPath, options);

                        // Draw the holes.
                        if (path.Holes != null)
                            foreach (SymPath hole in path.Holes)
                                lineSymDef.DrawHighlight(g, hole, options);
                    }
                    else if (!areaDrawn) {
                        ((AreaLikeSymDef)component).DrawHighlight(g, path, options);
                        areaDrawn = true;  // Only draw area once when highlighting.
                    }
                }
            }
            else {
                SymbolHelpers.DrawAreaHighlight(g, map, path, options);
            }
        }

        internal override RectangleF HighlightBounds(SymPathWithHoles path, RectangleF boundingBox, HighlightOptions options)
        {
            if (options.style == HighlightStyle.HighFidelity && this.BorderHighlightThickness > SymbolHelpers.MinimumLineWidth(options)) {
                return boundingBox;
            }
            else {
                return SymbolHelpers.AreaHighlightBounds(path, options);
            }
        }
    }

    public class AreaSymDef: AreaLikeSymDef
    {
        public enum RotateMode {
            Always,             // Always rotate the pattern when rotating map/object.
            ManualOnly,         // Only allow manual setting of pattern direction, but don't rotate pattern when rotating object/map
            Never               // Never allow pattern direction to influence rendering of pattern (even if already set).
        };

        public enum PatternFillMode
        {
            Clip = 0,               // Clip at boundary of area (only option until OCAD 12)
            CompletelyInside = 1,   // Draw structure elements completely inside area
            CenterInside = 2,       // Draw structure elements with center inside area
            PartiallyInside = 3,    // Draw structure elements with any part inside area
        }

        public struct HatchInfo
        {
            public SymColor hatchColor;  // color of hatch lines
            public float hatchWidth;    // width of hatch lines
            public float hatchSpacing;  // spacing of hatch lines
            public float hatchOffset;   // offset of hatch lines from zero (for OpenMapper compatibility)
            public float hatchAngle;    // angle of hatching
            public RotateMode hatchRotateMode; // rotation mode.
        }

        public struct PatternInfo
        {
            public bool offsetRows;     // offset alternate rows by 1/2
            public float patternWidth;
            public float patternHeight; // size of pattern element
            public float patternAngle;  // angle of pattern
            public float patternOffsetX, patternOffsetY;  // offset patterns by this.
            public Glyph patternGlyph;  // pattern element
            public RotateMode patternRotateMode; // rotation mode.
            public PatternFillMode patternFillMode;  // fill mode for pattern
            public bool irregular;         // true: irregular pattern
            public float irregularVarX;    // 0-1: fraction of irregularity in X
            public float irregularVarY;    // 0-1: fraction of irregularity in Y
            public float irregularMinDist; // min distance between objects.
        }

        // Solid fill information
        SymColor fillColor;  // color to fill with, null to not fill.

        // Border information
        LineSymDef borderSymdef;          // if non-null, the border symbol to use.

        // Hatching information.
        List<HatchInfo> hatchings = new List<HatchInfo>();
        object[] hatchPens;        // pens for hatching.

        // Pattern information
        List<PatternInfo> patterns = new List<PatternInfo>();

        // Cached brushes for faster pattern drawing.
        Dictionary<Pair<int, SymColor>, object> patternBrushes = new Dictionary<Pair<int, SymColor>, object>(2); // pattern brushes, indexed by pattern index and color

        public override void SetMap(Map newMap)
        {
            base.SetMap(newMap);

            if (newMap != null) {
                CheckColor(fillColor);
                foreach (HatchInfo hatchInfo in hatchings)
                    CheckColor(hatchInfo.hatchColor);
                foreach (PatternInfo patternInfo in patterns)
                    patternInfo.patternGlyph.CheckColors(map);
            }
        }

        public AreaSymDef(string name, string symbolId, SymColor color, LineSymDef borderSymdef)
            : base(name, symbolId)
        {
            fillColor = color;
            this.borderSymdef = borderSymdef;
        }

        public SymColor FillColor { get { return fillColor; } }

        public LineSymDef BorderSymdef { get { return borderSymdef; } }

        public override SymDef DependsOnSymdef
        {
            get
            {
                return borderSymdef;
            }
        }

        internal override float BorderHighlightThickness
        {
            get
            {
                if (borderSymdef != null)
                    return borderSymdef.HighlightThickness;
                else
                    return 0;
            }
        }

        public void AddHatching(HatchInfo hatchInfo)
        {
            CheckModifiable();
            hatchings.Add(hatchInfo);
        }

        public bool HasHatching { get { return hatchings.Count > 0; }}

        public ICollection<HatchInfo> GetHatchings()
        {
            return hatchings.AsReadOnly();
        }

        public void AddPattern(PatternInfo patternInfo)
        {
            CheckModifiable();
            patterns.Add(patternInfo);
        }

        public bool HasPattern { get { return patterns.Count > 0; }}

        internal override bool HasDashes
        {
            get
            {
                if (borderSymdef != null && borderSymdef.HasDashes)
                    return true;
                else
                    return false;
            }
        }

        public ICollection<PatternInfo> GetPatterns()
        {
            return patterns.AsReadOnly();
        }

        public override SymDef CopyToMap(Map map)
        {
            var newBorderSymDef = borderSymdef == null ? null : (LineSymDef) map.SymdefFromSymbolId(borderSymdef.SymbolId);
            AreaSymDef newSymDef = new AreaSymDef(Name, SymbolId, map.SymColorFromSymColor(fillColor), newBorderSymDef);

            foreach (HatchInfo hatchInfo in hatchings) {
                HatchInfo newHatchInfo = hatchInfo;
                newHatchInfo.hatchColor = map.SymColorFromSymColor(hatchInfo.hatchColor);
                newSymDef.AddHatching(newHatchInfo);
            }

            foreach (PatternInfo patternInfo in patterns) {
                PatternInfo newPatternInfo = patternInfo;
                newPatternInfo.patternGlyph = patternInfo.patternGlyph.CopyToMap(map);
            }

            map.AddSymdef(newSymDef);
            return newSymDef;
        }

        void CreatePensAndBrushes(IGraphicsTarget g)
        {
            if (HasHatching) {
                if (hatchPens == null || hatchPens.Length != hatchings.Count)
                    hatchPens = new object[hatchings.Count];
                for (int i = 0; i < hatchings.Count; ++i) {
                    if (hatchPens[i] == null) {
                        hatchPens[i] = new object();
                    }
                    if (!g.HasPen(hatchPens[i])) {
                        g.CreatePen(hatchPens[i], hatchings[i].hatchColor.GetBrushKey(g), hatchings[i].hatchWidth, LineCapMode.Flat, LineJoinMode.Miter, GraphicsUtil.MITER_LIMIT);
                    }
                }
            }
        }

        public override void FreeGdiObjects()
        {
        }



        // Does this symbol definition draw the given color. Used to determine
        // if this symbol definition draws into the current layer being draw.
        public override bool HasColor(SymColor color)
        {
            Debug.Assert(color != null);
            if (color.IsSpecialLayer)
                return false;

            if (color == fillColor)
                return true;

            if (borderSymdef != null && borderSymdef.HasColor(color))
                return true;
            
            foreach (HatchInfo hatchInfo in hatchings) {
                if (color == hatchInfo.hatchColor)
                    return true;
            }

            foreach (PatternInfo patternInfo in patterns) {
                if (patternInfo.patternGlyph.HasColor(color))
                    return true;
            }

            return false;
        }

        // Highlight the symbol.
        internal override void DrawHighlight(IGraphicsTarget g, SymPathWithHoles path, HighlightOptions options)
        {
            if (options.style == HighlightStyle.HighFidelity && BorderSymdef != null &&
                BorderSymdef.HighlightThickness > SymbolHelpers.MinimumLineWidth(options)) {
                // Just fill the interior, and highlight the border.
                SymbolHelpers.FillAreaHighlight(g, map, path, options);

                BorderSymdef.DrawHighlight(g, path.MainPath, options);
                if (path.Holes != null) {
                    foreach (SymPath holePath in path.Holes)
                        BorderSymdef.DrawHighlight(g, holePath, options);
                }
            }
            else {
                SymbolHelpers.DrawAreaHighlight(g, map, path, options);
            }
        }

        internal override RectangleF HighlightBounds(SymPathWithHoles path, RectangleF boundingBox, HighlightOptions options)
        {
            if (options.style == HighlightStyle.HighFidelity && BorderSymdef != null &&
                BorderSymdef.HighlightThickness > SymbolHelpers.MinimumLineWidth(options)) {
                return boundingBox;
            }
            else {
                return SymbolHelpers.AreaHighlightBounds(path, options);
            }
        }

        // Draw this area symbol in the graphics inside/around the path provided, with
        // the given color only.
        internal override void Draw(IGraphicsTarget g, SymPathWithHoles path, SymColor color, float angle, PointF rotationCenter, RenderOptions renderOpts)
        {
            Debug.Assert(color != null);

            CreatePensAndBrushes(g);

            if (color == fillColor) {
                path.Fill(g, color.GetBrushKey(g));
            }

            if (HasHatching) {
                DrawHatchings(g, path, angle, rotationCenter, renderOpts, color);
            }

            if (HasPattern) {
                DrawPatterns(g, path, color, angle, rotationCenter, renderOpts);
            }

            // Draw the border. The Draw routine on LineSymDef automatically takes into account the subpaths defined by start/stop flags along the paths.
            if (borderSymdef != null && borderSymdef.HasColor(color)) {
                // Draw main part of border.
                borderSymdef.Draw(g, path.MainPath, color, renderOpts);

                // Draw the holes.
                if (path.Holes != null)
                    foreach (SymPath hole in path.Holes)
                        borderSymdef.Draw(g, hole, color, renderOpts);
            }
        }

        // Draw the hatching into the interior of the SymPath.
        void DrawHatchings(IGraphicsTarget g, SymPathWithHoles path, float angle, PointF rotationCenter, RenderOptions renderOpts, SymColor color)
        {
            // Precheck if any hatchings have the color before doing the expensive clip operation.
            bool hasColor = false;
            for (int i = 0; i < hatchings.Count; ++i) {
                if (hatchings[i].hatchColor == color)
                    hasColor = true;
            }

            if (!hasColor)
                return;

            // Set the clipping region to draw only inside the area.
            g.PushClip(path.GetPathKey(g));

            try {
                for (int i = 0; i < hatchings.Count; ++i) {
                    if (hatchings[i].hatchColor == color)
                        DrawOneHatching(g, path, angle, rotationCenter, renderOpts, hatchings[i], hatchPens[i]);
                }
            }
            finally {
                g.PopClip();
            }
        }

        void DrawOneHatching(IGraphicsTarget g, SymPathWithHoles path, float angle, PointF rotationCenter, RenderOptions renderOpts, HatchInfo hatchInfo, object hatchPen)
        {
            if (hatchInfo.hatchRotateMode == RotateMode.Never) {
                angle = 0;
                rotationCenter = new PointF();
            }

            // use a transform to rotate and then draw hatching.
            Matrix matrix = new Matrix();
            matrix.Translate(rotationCenter.X, rotationCenter.Y);
            matrix.Rotate(angle);
            matrix.Rotate(hatchInfo.hatchAngle);
            g.PushTransform(matrix);

            try {
                // Get the correct bounding rect.
                matrix.Invert();
                RectangleF bounding = Geometry.BoundsOfTransformedRectangle(path.BoundingBox, matrix);

                SymbolHelpers.DrawHatchLines(g, hatchPen, hatchInfo.hatchSpacing, hatchInfo.hatchOffset, bounding);
            }
            finally {
                // restore the transform
                g.PopTransform();
            }
        }

        // Draw all patterns
        void DrawPatterns(IGraphicsTarget g, SymPathWithHoles path, SymColor color, float angle, PointF rotationCenter, RenderOptions renderOpts)
        {
            for (int i = 0; i < patterns.Count; ++i) {
                PatternInfo patternInfo = patterns[i];
                bool ignoreRotation = (patternInfo.patternRotateMode == RotateMode.Never);

                if (patternInfo.patternGlyph.HasColor(color)) {
                    // Faster to draw the pattern with a texture brush that has a bitmap
                    // of the pattern in it. Better quality to do it all with glyph drawing.
                    // Choose based on the renderOptions.
                    if (renderOpts.usePatternBitmaps && g.SupportsPatternBrushes && 
                        patternInfo.patternFillMode == PatternFillMode.Clip && !patternInfo.irregular &&
                        !PatternBrushTooPixelated(patternInfo, renderOpts.minResolution)) 
                    {
                        CreatePatternBrushes(i, renderOpts.minResolution, g);
                        DrawPatternWithTexBrush(i, g, path, ignoreRotation ? 0 : angle, ignoreRotation ? new PointF() : rotationCenter, color, renderOpts);
                    }
                    else
                        DrawPattern(patternInfo, g, path, ignoreRotation ? 0: angle, ignoreRotation ? new PointF() : rotationCenter, color, renderOpts);

                }
            }
        }

        // Draw the pattern using the texture brush.
        void DrawPatternWithTexBrush(int iPattern, IGraphicsTarget g, SymPathWithHoles path, float angle, PointF rotationCenter, SymColor color, RenderOptions renderOpts)
        {
            Debug.Assert(color != null);

            object brush = patternBrushes[new Pair<int, SymColor>(iPattern, color)];
            Debug.Assert(brush != null);

            if (angle != 0.0F) {
                // Set the clipping region to draw only inside the area.
                g.PushClip(path.GetPathKey(g));

                // use a transform to rotate.
                Matrix matrix = new Matrix();
                matrix.Translate(rotationCenter.X, rotationCenter.Y);
                matrix.Rotate(angle);
                g.PushTransform(matrix);

                try
                {
                    // Get the correct bounding rect.
                    matrix.Invert();
                    RectangleF bounding = Geometry.BoundsOfTransformedRectangle(path.BoundingBox, matrix);

                    g.FillRectangle(brush, bounding);
                }
                finally {
                    // restore the clip region and the transform
                    g.PopTransform();
                    g.PopClip();
                }
            }
            else {
                path.Fill(g, brush);
            }
        }

#if false
        void CreatePatternBrush() 
        {
            patternBrushes = new Dictionary<SymColor, Brush>(2);

            foreach (SymColor color in map.colors) {
                if (!patternGlyph.HasColor(color))
                    continue;

                // Create a visual with the glyph to tile in it.
                DrawingVisual visual = new DrawingVisual();
                DrawingContext dc = visual.RenderOpen();
                GraphicsTarget grTarget = new GraphicsTarget(dc);

                RenderOptions renderOpts = new RenderOptions();
                renderOpts.minResolution = 0.01F;
                renderOpts.usePatternBitmaps = false;

                patternGlyph.Draw(grTarget, new PointF(0F, 0F), -patternAngle, null, null, color, renderOpts);
                if (offsetRows) {
                    patternGlyph.Draw(grTarget, new PointF(patternWidth / 2, patternHeight), -patternAngle, null, null, color, renderOpts);
                    patternGlyph.Draw(grTarget, new PointF(-patternWidth / 2, patternHeight), -patternAngle, null, null, color, renderOpts);
                }

                dc.Close();

                // Get a drawing from the drawingvisual
                Drawing drawing = visual.Drawing;
                drawing.Freeze();

                // Create a brush from the drawing.
                DrawingBrush brush = new DrawingBrush(drawing);
                brush.Stretch = Stretch.Fill;
                brush.TileMode = TileMode.Tile;
                brush.ViewboxUnits = BrushMappingMode.Absolute;
                brush.ViewportUnits = BrushMappingMode.Absolute;
                if (offsetRows)
                    brush.Viewbox = brush.Viewport = new Rect(-patternWidth / 2, -patternHeight / 2, patternWidth, patternHeight * 2);
                else
                    brush.Viewbox = brush.Viewport = new Rect(-patternWidth / 2, -patternHeight / 2, patternWidth, patternHeight);
                brush.Transform = new RotateTransform(patternAngle);

                // Set the minimum and maximum relative sizes for regenerating the tiled brush.
                // The tiled brush will be regenerated when the size is
                //   0.5x, 0.25x (and so forth)
                // and
                //   2x, 4x, 8x (and so forth)
                // of the original size.
                System.Windows.Media.RenderOptions.SetCacheInvalidationThresholdMinimum(brush, 0.5);
                System.Windows.Media.RenderOptions.SetCacheInvalidationThresholdMaximum(brush, 2.0);

                // Set the caching hint option for the brush.
                System.Windows.Media.RenderOptions.SetCachingHint(brush, CachingHint.Cache);

                // Freeze the brush.
                brush.Freeze();

                // Add it to the collection of brushes.
                patternBrushes.Add(color, brush);
            }
        }
#endif
        const float MAX_PATTERN_SIZE = 80;
        const float MIN_PATTERN_SIZE = 10;
        
        // return true if a patten brush would be too pixelated at this size.
        bool PatternBrushTooPixelated(PatternInfo patternInfo, float pixelSize)
        {
            return (pixelSize < patternInfo.patternWidth / MAX_PATTERN_SIZE) || (pixelSize < patternInfo.patternHeight / MAX_PATTERN_SIZE);
        }

        void CreatePatternBrushes(int iPattern, float pixelSize, IGraphicsTarget gt)
        {
            PatternInfo patternInfo = patterns[iPattern];
            //  Determine adjusted pixel size of the brush to create. 
            if (pixelSize < patternInfo.patternWidth / MAX_PATTERN_SIZE)
                pixelSize = patternInfo.patternWidth / MAX_PATTERN_SIZE;
            if (pixelSize < patternInfo.patternHeight / MAX_PATTERN_SIZE)
                pixelSize = patternInfo.patternHeight / MAX_PATTERN_SIZE;
            if (pixelSize > patternInfo.patternWidth / MIN_PATTERN_SIZE)
                pixelSize = patternInfo.patternWidth / MIN_PATTERN_SIZE;
            if (pixelSize > patternInfo.patternHeight / MIN_PATTERN_SIZE)
                pixelSize = patternInfo.patternHeight / MIN_PATTERN_SIZE;

            // Get size of bitmap to create with the image of the pattern.
            float width = (float) Math.Round(patternInfo.patternWidth / pixelSize);
            float height = (float) Math.Round(patternInfo.patternHeight / pixelSize);

            int bitmapWidth = (int) width;
            int bitmapHeight = (int) (patternInfo.offsetRows ? height * 2 : height);

            RenderOptions renderOpts = new RenderOptions();
            renderOpts.minResolution = pixelSize;
            renderOpts.usePatternBitmaps = false;

            foreach (SymColor color in map.colors) {
                if (!patternInfo.patternGlyph.HasColor(color))
                    continue;

                Pair<int, SymColor> brushKey = new Pair<int, SymColor>(iPattern, color);

                // Create a new pattern brush.
                if (!patternBrushes.ContainsKey(brushKey)) {
                    lock (patternBrushes) {
                        if (!patternBrushes.ContainsKey(brushKey))
                            patternBrushes.Add(brushKey, new object());
                    }
                }

                if (gt.HasBrush(patternBrushes[brushKey]))
                    continue;

                // Calculate the brush box in rendering coordinates.
                RectangleF patternBox = new RectangleF(-patternInfo.patternWidth / 2, -patternInfo.patternHeight / 2, patternInfo.patternWidth, patternInfo.patternHeight);

                // Create the brush, with coordinates set up with (0,0) in center of the brush.
                IBrushTarget brushTarget = gt.CreatePatternBrush(new SizeF(patternInfo.patternWidth, (patternInfo.offsetRows ? patternInfo.patternHeight * 2 : patternInfo.patternHeight)), patternInfo.patternAngle, bitmapWidth, bitmapHeight);

                // If the glyphPattern bounding box extend outside the brush box, then we need to render glyphPattern more then once. This
                // is calculated in both the vertical and horizontal directions.
                RectangleF patternGlyphBounds = patternInfo.patternGlyph.BoundingBox;
                Matrix m = new Matrix();
                m.Translate(patternInfo.patternOffsetX, patternInfo.patternOffsetY);
                m.Rotate(-patternInfo.patternAngle);
                patternGlyphBounds = Geometry.BoundsOfTransformedRectangle(patternGlyphBounds, m);

                int rowStart = 0, rowEnd = 0, colStart = 0, colEnd = 0;
                if (patternBox.Top > patternGlyphBounds.Top)
                    rowStart = -(int)Math.Ceiling((patternBox.Top - patternGlyphBounds.Top) / patternInfo.patternHeight);
                if (patternBox.Bottom < patternGlyphBounds.Bottom)
                    rowEnd = (int)Math.Ceiling((patternGlyphBounds.Bottom - patternBox.Bottom) / patternInfo.patternHeight);
                if (patternBox.Left > patternGlyphBounds.Left)
                    colStart = -(int)Math.Ceiling((patternBox.Left - patternGlyphBounds.Left) / patternInfo.patternWidth);
                if (patternBox.Right < patternGlyphBounds.Right)
                    colEnd = (int)Math.Ceiling((patternGlyphBounds.Right - patternBox.Right) / patternInfo.patternWidth);

                if (patternInfo.offsetRows) {
                    rowStart -= 1;
                    rowEnd += 1;
                    colStart -= 1;
                }

                // Draw the pattern into the bitmap
                Matrix matrix = new Matrix();

                for (int col = colStart; col <= colEnd; ++col) {
                    for (int row = rowStart; row <= rowEnd; ++row) {
                        matrix = new Matrix();
                        matrix.Translate(-col * patternInfo.patternWidth, -row * patternInfo.patternHeight);
                        matrix.Translate(patternInfo.patternOffsetX, patternInfo.patternOffsetY);
                        if (patternInfo.offsetRows && (row % 2) != 0)
                            matrix.Translate(-patternInfo.patternWidth / 2, 0);
                        matrix.Rotate(-patternInfo.patternAngle);
                        brushTarget.PushTransform(matrix);

                        patternInfo.patternGlyph.Draw(brushTarget, new PointF(0F, 0F), 0, null, null, color, null, renderOpts);

                        brushTarget.PopTransform();
                    }
                }

                // Get the brush
                brushTarget.FinishBrush(patternBrushes[brushKey]);
            }
        }

        // Draw the pattern (at the given angle) inside the path.
        void DrawPattern(PatternInfo patternInfo, IGraphicsTarget g, SymPathWithHoles path, float angle, PointF rotationCenter, SymColor color, RenderOptions renderOpts)
        {
            // Set the clipping region to draw only inside the area.
            if (patternInfo.patternFillMode == PatternFillMode.Clip) {
                g.PushClip(path.GetPathKey(g));
            }

            // use a transform to rotate 
            Matrix matrix = new Matrix();
            matrix.Translate(rotationCenter.X, rotationCenter.Y);
            matrix.Rotate(angle);
            matrix.Rotate(patternInfo.patternAngle);
            matrix.Translate(patternInfo.patternOffsetX, patternInfo.patternOffsetY);
            g.PushTransform(matrix);

            try
            {
                // Get the correct bounding rect.
                matrix.Invert();
                RectangleF bounding = Geometry.BoundsOfTransformedRectangle(path.BoundingBox, matrix);

                DrawPatternRows(patternInfo, path.Transform(matrix), g, bounding, color, renderOpts);
            }
            finally {
                // restore the clip region and the transform
                g.PopTransform();
                if (patternInfo.patternFillMode == PatternFillMode.Clip) {
                    g.PopClip();
                }
            }
        }

        // Draw a set of rows of the pattern with the given rectangle
        void DrawPatternRows(PatternInfo patternInfo, SymPathWithHoles transformedPath, IGraphicsTarget g, RectangleF boundingRect, SymColor color, RenderOptions renderOpts)
        {
            RectangleF patternGlyphBounds = Geometry.BoundsOfRotatedRectangle(patternInfo.patternGlyph.BoundingBox, new PointF(0, 0), -patternInfo.patternAngle);
            RectangleF patternBounds = new RectangleF(-patternInfo.patternWidth / 2, -patternInfo.patternHeight / 2, patternInfo.patternWidth, patternInfo.patternHeight);

            long topLineCount = (long)Math.Round((boundingRect.Top - patternGlyphBounds.Bottom + patternBounds.Bottom) / patternInfo.patternHeight);
            double topLine = topLineCount * patternInfo.patternHeight;
            double bottomLine = (Math.Round((boundingRect.Bottom - patternGlyphBounds.Top + patternBounds.Top) / patternInfo.patternHeight) + 0.5) * patternInfo.patternHeight;
            double leftLine = Math.Round((boundingRect.Left - patternGlyphBounds.Right + patternBounds.Right) / patternInfo.patternWidth) * patternInfo.patternWidth;
            double rightLine = (Math.Round((boundingRect.Right - patternGlyphBounds.Left + patternBounds.Left) / patternInfo.patternWidth) + 0.5) * patternInfo.patternWidth;
            double offsetLeftLine = leftLine - (patternInfo.patternWidth / 2);
            double offsetRightLine = rightLine + (patternInfo.patternWidth / 2);
            bool firstLineOffset = (topLineCount & 1) != 0;

            bool offsetThisLine = patternInfo.offsetRows && firstLineOffset;

            GlyphPartFilter filter;
            switch (patternInfo.patternFillMode) {
                case PatternFillMode.Clip:
                    filter = null;
                    break;
                case PatternFillMode.CompletelyInside:
                    filter = (PointF[] points, PointF center) => points.All(pt => transformedPath.IsInside(pt));
                    break;
                case PatternFillMode.CenterInside:
                    filter = (PointF[] points, PointF center) => transformedPath.IsInside(center);
                    break;
                case PatternFillMode.PartiallyInside:
                    filter = (PointF[] points, PointF center) => points.Any(pt => transformedPath.IsInside(pt));
                    break;
                default:
                    Debug.Fail("Unexpected PatternFillMode");
                    filter = null;
                    break;
            }

            for (double y = topLine; y <= bottomLine; y += patternInfo.patternHeight) {
                if (offsetThisLine) {
                    for (double x = offsetLeftLine; x <= offsetRightLine; x += patternInfo.patternWidth) {
                        PointF location = HandleIrregularity(patternInfo, x, y);
                        patternInfo.patternGlyph.Draw(g, location, -patternInfo.patternAngle, null, null, color, filter, renderOpts);
                    }
                }
                else {
                    for (double x = leftLine; x <= rightLine; x += patternInfo.patternWidth) {
                        PointF location = HandleIrregularity(patternInfo, x, y);
                        patternInfo.patternGlyph.Draw(g, location, -patternInfo.patternAngle, null, null, color, filter, renderOpts);
                    }
                }

                if (patternInfo.offsetRows)
                    offsetThisLine = !offsetThisLine;
            }
        }

        // Handle irregularity by offseting x and y by random amounts if irregularity was asked for.
        private PointF HandleIrregularity(PatternInfo patternInfo, double x, double y)
        {
            if (!patternInfo.irregular) {
                return new PointF((float)x, (float)y);
            }

            // Irregularity is desired. Figure out the maximum displacement (either positive or negative) in each direction.

            // First get max displacements based on percentage.
            double maxXDisplacement = patternInfo.patternWidth / 2.0F * patternInfo.irregularVarX;
            double maxYDisplacement = patternInfo.patternHeight / 2.0F * patternInfo.irregularVarY;

            // Then, if the max displacement based on radius of pattern is smaller, use that.
            maxXDisplacement = Math.Min(maxXDisplacement, patternInfo.patternWidth / 2.0F - patternInfo.patternGlyph.Radius);
            maxYDisplacement = Math.Min(maxYDisplacement, patternInfo.patternHeight / 2.0F - patternInfo.patternGlyph.Radius);

            // Then, if the max displacement based on nearest distance is smaller, use that.
            maxXDisplacement = Math.Min(maxXDisplacement, maxXDisplacement - patternInfo.irregularMinDist * 0.5F);
            if (patternInfo.offsetRows)
                maxYDisplacement = Math.Min(maxYDisplacement, (patternInfo.patternHeight - (patternInfo.patternGlyph.Radius * 2 + patternInfo.irregularMinDist) * 0.85F) * 0.5F);  
            else
                maxYDisplacement = Math.Min(maxYDisplacement, maxYDisplacement - patternInfo.irregularMinDist * 0.5F);

            // But max displacement can't be smaller than zero.
            maxXDisplacement = Math.Max(0, maxXDisplacement);
            maxYDisplacement = Math.Max(0, maxYDisplacement);

            // Now get random displacement in X and Y from the max displacement.
            // The randomness actually needs to be deterministic according to the x and y location, so that
            // if different areas overlap they achieve the same randomness. We create a seed based on x and y.
            int seed = 17;
            seed = seed * 23 + Math.Round(x, 2).GetHashCode();
            seed = seed * 23 + Math.Round(y, 2).GetHashCode();
            Random rand = new Random(seed);
            double xDisplacement = (rand.NextDouble() * 2.0 - 1.0) * maxXDisplacement;
            double yDisplacement = (rand.NextDouble() * 2.0 - 1.0) * maxYDisplacement;

            return new PointF((float)(x + xDisplacement), (float)(y + yDisplacement));
        }

        // Calculate the bounding box
        internal override RectangleF CalcBounds(SymPathWithHoles path)
        {
            if (borderSymdef != null)
                return borderSymdef.CalcBounds(path.MainPath);
            else
                return path.BoundingBox;
        }
    }

    // The horizontal alignment of text symbols.
    public enum TextSymDefHorizAlignment: byte
    {
        Default,       // Default as set in SymDef.
        Left,
        Right,
        Center,
        Justified
    }

    // The vertical alignment of text symbols.
    public enum TextSymDefVertAlignment: byte
    {
        Default,        // Default as set in SymDef.
        TopAscent,      // At the top of the ascent line
        Midpoint,       // Midpoint between ascent and baseline of first line
        Baseline,       // At the baseline of the first line
        BaselineLast,   // At the baseline of the last line
        MidpointAllLines, // Midpoint between ascent of top line and baseline of last line.
        Bottom,         // Baseline of last line.
    }

    // A coordinate (line/column) in text.
    public struct TextCoord
    {
        public readonly int Line;  // 0-based line.
        public readonly int Col;  // 0-base column.

        public override bool Equals(object obj)
        {
            if (obj is TextCoord) {
                TextCoord other = (TextCoord)obj;
                return (this.Line == other.Line && this.Col == other.Col);
            }
            else {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Line.GetHashCode() + 1737 * Col.GetHashCode();
        }

        public TextCoord(int line, int col)
        { 
            Line = line;
            Col = col;
        }
    }

    // This class contains data that remembers the mapping between unwrapped and wrapped text coordinates.
    public class TextCoordMapper
    {
        // For each wrapped line, the unwrapped coordinates of the start of that line.
        List<TextCoord> unwrappedCoordOfWrappedLine = new List<TextCoord>();

        internal void AddUnwrappedCoord(TextCoord unwrappedTextCoord)
        {
            unwrappedCoordOfWrappedLine.Add(unwrappedTextCoord);
        }

        // Add all the coordinates from another coord mapper, applying a line offset.
        internal void AddCoordMapper(int lineOffset, TextCoordMapper coordMapperToAdd)
        {
            foreach (TextCoord unwrappedCoord in coordMapperToAdd.unwrappedCoordOfWrappedLine) {
                AddUnwrappedCoord(new TextCoord(unwrappedCoord.Line + lineOffset, unwrappedCoord.Col));
            }
        }

        public TextCoord UnwrappedFromWrapped(TextCoord wrappedTextCoord, string[] unwrappedText, string[] wrappedText)
        {
            Debug.Assert(wrappedText.Length == unwrappedCoordOfWrappedLine.Count);
            Debug.Assert(wrappedTextCoord.Line >= 0 && wrappedTextCoord.Line < unwrappedCoordOfWrappedLine.Count);
            Debug.Assert(wrappedTextCoord.Col >= 0 && wrappedTextCoord.Col <= wrappedText[wrappedTextCoord.Line].Length);

            int unwrappedLine = unwrappedCoordOfWrappedLine[wrappedTextCoord.Line].Line;
            int unwrappedCol = unwrappedCoordOfWrappedLine[wrappedTextCoord.Line].Col + wrappedTextCoord.Col;

            return new TextCoord(unwrappedLine, unwrappedCol);
        }

        public TextCoord WrappedFromUnwrapped(TextCoord unwrappedTextCoord, string[] unwrappedText, string[] wrappedText)
        {
            Debug.Assert(wrappedText.Length == unwrappedCoordOfWrappedLine.Count);
            Debug.Assert(unwrappedTextCoord.Line >= 0 && unwrappedTextCoord.Line < unwrappedText.Length);
            Debug.Assert(unwrappedTextCoord.Col >= 0 && unwrappedTextCoord.Col <= unwrappedText[unwrappedTextCoord.Line].Length);

            int wrappedLine = 0, wrappedCol = 0;

            for (int i = 0; i < unwrappedCoordOfWrappedLine.Count; ++i) {
                TextCoord unwrappedCoordAtLineStart = unwrappedCoordOfWrappedLine[i];
                if (unwrappedCoordAtLineStart.Line == unwrappedTextCoord.Line && unwrappedCoordAtLineStart.Col <= unwrappedTextCoord.Col) {
                    wrappedLine = i;
                    wrappedCol = unwrappedTextCoord.Col - unwrappedCoordAtLineStart.Col;

                    if (i < unwrappedCoordOfWrappedLine.Count - 1) {
                        TextCoord unwrappedCoordAtNextLineStart = unwrappedCoordOfWrappedLine[i];
                        if (unwrappedCoordAtLineStart.Line != unwrappedTextCoord.Line ||
                            unwrappedCoordAtLineStart.Col > unwrappedTextCoord.Col) {
                            break;
                        }
                    }
                    else {
                        break;
                    }
                }
            }

            return new TextCoord(wrappedLine, wrappedCol);
        }

    }

    public class TextSymDef: SymDef
    {
        public enum FramingStyle {None, Line, Shadow, Rectangle };

        public enum PreferredSymbolKind { NormalText, LineText, None }

        public struct Framing {
            public FramingStyle framingStyle;
            public SymColor framingColor;
            public float lineWidth;
            public LineStyle lineStyle;
            public float shadowX, shadowY;
            public float rectBorderLeft, rectBorderTop, rectBorderRight, rectBorderBottom;
        }

        public class Underlining: ICloneable
        {
            public bool underlineOn;
            public SymColor underlineColor;
            public float underlineWidth;
            public float underlineDistance;

            public object Clone()
            {
                return this.MemberwiseClone();
            }
        }

        // Struct for static methods involving wrapping text.
        public struct WrapTextProperties
        {
            public ITextFaceMetrics TextFaceMetrics;
            public float[] Tabs;
            public float CharSpacing;
            public float WordSpacing;
            public TextSymDefHorizAlignment FontAlign;
            public float FirstIndent;
            public float RestIndent;
            public bool AddParagraphMarks;
        }

        PreferredSymbolKind preferredSymbolKind;
        SymColor fontColor;
        float fontSize;
        string fontName;
        TextEffects effects;
        TextSymDefHorizAlignment defaultHorizAlign;
        TextSymDefVertAlignment defaultVertAlign;
        float lineSpacing;
        float paraSpacing;   
        float charSpacing, wordSpacing;
        float firstIndent, restIndent;
        float[] tabs;
        Framing framing;
        Underlining underline = new Underlining();
        PointSymDef centerPointSymdef;          // if non-null, the center point symdef to use.

        // GDI+ object correspoding to the above attributes.
        List<object> framingPens = new List<object>();
        object font = new object();

        // How to draw underlining font effect.
        const float fontEffectUnderlineWidth = 0.05F;
        const float fontEffectUnderlineDistance = 0.02F;

        bool fontMetricsCreated;
        ITextFaceMetrics textFaceMetrics;


        public const string ParagraphMark = "\x2029";  // string denoted a paragraph boundary (Unicode paragraph mark).

        public TextSymDef(string name, string symbolId, PreferredSymbolKind preferredSymbolKind, PointSymDef centerPointSymdef)
            : base(name, symbolId)
        {
            this.preferredSymbolKind = preferredSymbolKind;
            this.centerPointSymdef = centerPointSymdef;
        }

        public override void SetMap(Map newMap)
        {
            base.SetMap(newMap);
        }

        public PointSymDef CenterPointSymdef { get { return centerPointSymdef; } }

        public PreferredSymbolKind SymbolKind { get { return preferredSymbolKind; } }

        public override SymDef DependsOnSymdef {
            get {
                return centerPointSymdef;
            }
        }

        private void CreateFontMetrics()
        {
            Debug.Assert(!fontMetricsCreated);
            textFaceMetrics = map.TextMetricsProvider.GetTextFaceMetrics(fontName, fontSize, effects);
            fontMetricsCreated = true;
        }

        private void CreateObjects(IGraphicsTarget g)
        {
            if (!fontMetricsCreated)
                CreateFontMetrics();

            if (!g.HasFont(font))
                g.CreateFont(font, fontName, fontSize, (effects & ~TextEffects.Underline)); // Underlining done otherwise.

            if (framing.framingStyle == FramingStyle.Line /* && framing.framingColor != null */) {
                // We use multiple pens to avoid weird artifacts with overlapping parts.
                int iPen = 0;
                for (float width = fontSize * 0.33F; width < framing.lineWidth * 2; width += fontSize * 0.33F) {
                    if (iPen >= framingPens.Count) {
                        lock (framingPens)
                            framingPens.Add(new object());
                    }

                    if (! g.HasPen(framingPens[iPen]))
                        GraphicsUtil.CreateSolidPen(g, framingPens[iPen], framing.framingColor.ColorValue, width, LineStyle.Beveled);
                    ++iPen;
                }

                if (iPen >= framingPens.Count) {
                    lock (framingPens)
                        framingPens.Add(new object());
                }

                if (!g.HasPen(framingPens[iPen]))
                    GraphicsUtil.CreateSolidPen(g, framingPens[iPen], framing.framingColor.ColorValue, framing.lineWidth * 2, framing.lineStyle);
            }
        }

        public override void FreeGdiObjects()
        {
            if (textFaceMetrics != null) {
                textFaceMetrics.Dispose();
                textFaceMetrics = null;
            }

            fontMetricsCreated = false;
        }


        // Does this symbol definition draw the given color. Used to determine
        // if this symbol definition draws into the current layer being draw.
        public override bool HasColor(SymColor color)
        {
            Debug.Assert(color != null);
            if (color.IsSpecialLayer)
                return false;

            return color == fontColor || 
                   (framing.framingStyle != FramingStyle.None && color == framing.framingColor) || 
                   (underline.underlineOn && color == underline.underlineColor) ||
                   (centerPointSymdef != null && centerPointSymdef.HasColor(color));
        }

        public void SetFont(string fontName, float fontSize, TextEffects effects, SymColor fontColor, float lineSpacing, float paraSpacing, 
                            float firstIndent, float restIndent, float[] tabs, float charSpacing, float wordSpacing, 
                            TextSymDefHorizAlignment defaulHorizAlign, TextSymDefVertAlignment defaultVertAlign)
        {
            CheckModifiable();
            this.fontName = fontName;
            this.fontSize = fontSize;
            this.effects = effects;
            this.fontColor = fontColor;
            this.lineSpacing = lineSpacing;
            this.paraSpacing = paraSpacing;
            this.firstIndent = firstIndent;
            this.restIndent = restIndent;
            this.charSpacing = charSpacing;
            this.wordSpacing = wordSpacing;
            this.defaultHorizAlign = defaulHorizAlign;
            this.defaultVertAlign = defaultVertAlign;
            this.tabs = tabs;
        }

        // Set tab stops to be the width of a particular string.
        public void SetRepeatingTabs(Map map, string tabTextWidth)
        {
            CheckModifiable();
            float tabWidth = map.TextMetricsProvider.GetTextFaceMetrics(fontName, fontSize, effects).GetTextWidth(tabTextWidth);
            this.tabs = new float[1] { tabWidth };
        }

        public void SetFraming(Framing framing)
        {
            CheckModifiable();
            this.framing = framing;
        }

        public void SetUnderline(Underlining underline)
        {
            CheckModifiable();
            this.underline = (Underlining) underline.Clone();
        }

        public override SymDef CopyToMap(Map map)
        {
            var newCenterPointSymdef = centerPointSymdef == null ? null : (PointSymDef) map.SymdefFromSymbolId(centerPointSymdef.SymbolId);
            var newSymDef = new TextSymDef(Name, SymbolId, preferredSymbolKind, newCenterPointSymdef);

            float[] newTabs = (tabs == null) ? null : (float[])tabs.Clone();
            newSymDef.SetFont(fontName, fontSize, effects, map.SymColorFromSymColor(fontColor), lineSpacing, paraSpacing, 
                              firstIndent, restIndent, newTabs, charSpacing, wordSpacing, defaultHorizAlign, defaultVertAlign);

            if (framing.framingStyle != FramingStyle.None) {
                Framing newFraming = framing;
                newFraming.framingColor = map.SymColorFromSymColor(newFraming.framingColor);
                newSymDef.SetFraming(newFraming);
            }

            if (underline.underlineOn) {
                Underlining newUnderlining = (Underlining) underline.Clone();
                newUnderlining.underlineColor = map.SymColorFromSymColor(newUnderlining.underlineColor);
                newSymDef.SetUnderline(newUnderlining);
            }

            map.AddSymdef(newSymDef);
            return newSymDef;
        }

        public TextSymDefHorizAlignment FontAlignment { get { return defaultHorizAlign; } }
        public TextSymDefVertAlignment VertAlignment { get { return defaultVertAlign; } }
        public string FontName { get { return fontName; } }
        public TextEffects TextEffects { get { return effects; } }
        public SymColor FontColor { get { return fontColor; } }
        public float LineSpacing { get { return lineSpacing; } }
        public float ParaSpacing { get { return paraSpacing; } }
        public float FirstIndent { get { return firstIndent; } }
        public float RestIndent { get { return restIndent; } }
        public float CharSpacing { get { return charSpacing; } }
        public float WordSpacing { get { return wordSpacing; } }
        public float FontEmHeight { get { return fontSize; } }
        public float[] Tabs { get { return tabs; } }
        public Framing FramingInfo { get { return framing; } }
        public Underlining Underline { get { return underline; } }

        public float FontAscent
        {
            get
            {
                if (!fontMetricsCreated)
                    CreateFontMetrics();

                return textFaceMetrics.Ascent;
            }
        }

        // Get height of the "W" character.
        public float WHeight
        {
            get
            {
                if (!fontMetricsCreated)
                    CreateFontMetrics();

                return textFaceMetrics.CapHeight; 
            }
        }

        public float FontDescent
        {
            get
            {
                if (!fontMetricsCreated)
                    CreateFontMetrics();

                return textFaceMetrics.Descent;
            }
        }

        // Return the offset from the top of the ascent of various vertical alignment options.
        internal float VertOffset(TextSymDefVertAlignment vertAlignment, string[] text)
        {
            TextSymDefVertAlignment vertAlign = GetVertAlignment(vertAlignment);

            switch (vertAlign) {
                case TextSymDefVertAlignment.TopAscent:
                    return 0;
                case TextSymDefVertAlignment.Baseline:
                    return FontAscent;
                case TextSymDefVertAlignment.Bottom:
                    return FontAscent + FontDescent;
                case TextSymDefVertAlignment.Midpoint:
                    return FontAscent / 2.0F;
                case TextSymDefVertAlignment.MidpointAllLines:
                    return (TextHeight(text) - FontDescent) / 2.0F;
                case TextSymDefVertAlignment.BaselineLast:
                    return (TextHeight(text) - FontDescent);
                default:
                    throw new ArgumentException("Bad vertical alignment");
            }
        }

        // Return the offset from the top of the ascent of various vertical alignment options for line text.
        internal float LineTextVertOffset(TextSymDefVertAlignment vertAlignment)
        {
            TextSymDefVertAlignment vertAlign = GetVertAlignment(vertAlignment);

            switch (vertAlign) {
                case TextSymDefVertAlignment.TopAscent:
                    return FontAscent - WHeight;

                case TextSymDefVertAlignment.Baseline:
                    return FontAscent;

                case TextSymDefVertAlignment.Bottom:
                    return FontAscent + FontDescent;

                case TextSymDefVertAlignment.Midpoint:
                case TextSymDefVertAlignment.MidpointAllLines:
                    return FontAscent - WHeight / 2.0F;

                default:
                    throw new ArgumentException("Bad vertical alignment");
            }
        }

        // Draw a single line of text at the given point with the given brush.
        private void DrawSingleLineString(IGraphicsTarget g, string text, object brushKey, PointF pt)
        {
            g.DrawText(text, font, brushKey, pt);
        }

        // Get the horizontal alignment to use, taking into account both the object and symdef alignment. 
        // Never returns default.
        internal TextSymDefHorizAlignment GetHorizAlignment(TextSymDefHorizAlignment objectHorizAlignment)
        {
            TextSymDefHorizAlignment fontAlign = objectHorizAlignment;
            if (fontAlign == TextSymDefHorizAlignment.Default)
                fontAlign = defaultHorizAlign;
            if (fontAlign == TextSymDefHorizAlignment.Default)
                fontAlign = TextSymDefHorizAlignment.Left;

            return fontAlign;
        }

        internal TextSymDefVertAlignment GetVertAlignment(TextSymDefVertAlignment objectVertAlignment)
        {
            TextSymDefVertAlignment vertAlign = objectVertAlignment;
            if (vertAlign == TextSymDefVertAlignment.Default)
                vertAlign = defaultVertAlign;
            if (vertAlign == TextSymDefVertAlignment.Default)
                vertAlign = TextSymDefVertAlignment.TopAscent;

            return vertAlign;
        }

        // Get properties needed to wrap text for this symdef.
        private WrapTextProperties GetWrapProperties(TextSymDefHorizAlignment objectHorizAlignment)
        {
            if (!fontMetricsCreated)
                CreateFontMetrics();

            return new WrapTextProperties() { 
                TextFaceMetrics = textFaceMetrics,
                Tabs = tabs,
                CharSpacing = charSpacing,
                WordSpacing = wordSpacing,
                FontAlign = GetHorizAlignment(objectHorizAlignment),
                FirstIndent = firstIndent,
                RestIndent = restIndent,
                AddParagraphMarks = true
            };
        }

        // Measure the width of a single line of text.
        private float MeasureStringWidth(string text, TextSymDefHorizAlignment objectHorizAlignment)
        {
            return MeasureStringWidth(GetWrapProperties(objectHorizAlignment), text);
        }

        // Measure the width of a single line of text.
        private static float MeasureStringWidth(WrapTextProperties wrapProperties, string text)
        {
            return wrapProperties.TextFaceMetrics.GetTextWidth(text);
        }

        // Draw a string with shadow or line framing effects, if specified. The font from this symdef is used.
        private void DrawStringWithEffects(IGraphicsTarget g, SymColor color, string text, PointF pt)
        {
            Debug.Assert(color != null);

            if (fontColor != null && color == fontColor) {
                DrawSingleLineString(g, text, fontColor.GetBrushKey(g), pt);
            }

            if (framing.framingStyle != FramingStyle.None && framing.framingColor != null && color == framing.framingColor) {
                if (framing.framingStyle == FramingStyle.Line) {
                    DrawSingleLineString(g, text, framing.framingColor.GetBrushKey(g), pt);
                    foreach (object p in framingPens)
                        g.DrawTextOutline(text, font, p, pt);
                }
                else if (framing.framingStyle == FramingStyle.Shadow) {
                    DrawSingleLineString(g, text, framing.framingColor.GetBrushKey(g), new PointF(pt.X + framing.shadowX, pt.Y - framing.shadowY));
                }
            }
        }

        // Draw the framing rectangle around some text. The top of the text is at 0, and the bottom baseline of text is at "bottomOfText".
        private void DrawFramingRectangle(IGraphicsTarget g, float[] lineWidths, float fullWidth, SymColor color, float topOfText, float bottomOfText, TextSymDefHorizAlignment objectHorizAlignment)
        {
            if (framing.framingStyle == FramingStyle.Rectangle && color == framing.framingColor) {
                RectangleF textRect = CalcTextRectangle(lineWidths, fullWidth, topOfText, bottomOfText, objectHorizAlignment);

                // Add padding.
                float t = textRect.Top - framing.rectBorderTop;
                float b = textRect.Bottom + framing.rectBorderBottom;
                float l = textRect.Left - framing.rectBorderLeft;
                float r = textRect.Right + framing.rectBorderRight;

                // Draw the rectangle
                g.FillRectangle(color.GetBrushKey(g), new RectangleF(l, t, r - l, b - t));
            }
        }

        // Calculate the text rectangle of a piece of text, as for framing.
        private RectangleF CalcTextRectangle(float[] lineWidths, float fullWidth, float topOfText, float bottomOfText, TextSymDefHorizAlignment objectHorizAlignment) {
            float l, t, r, b;  // The left, top, right, bottom of the rectangle.
            TextSymDefHorizAlignment fontAlign = GetHorizAlignment(objectHorizAlignment);

            // First, figure out the width of the rectangle. If fullWidth is zero, used the maximum line width.
            fullWidth = CalcFullWidth(lineWidths, fullWidth, fontAlign);

            // Next, figure out the rectangle, not counting padding.

            if (fontAlign == TextSymDefHorizAlignment.Right)
                l = -fullWidth;
            else if (fontAlign == TextSymDefHorizAlignment.Center)
                l = -(fullWidth / 2F);
            else
                l = 0;
            r = l + fullWidth;
            t = topOfText + FontAscent - WHeight;
            b = bottomOfText;
            return RectangleF.FromLTRB(l, t, r, b);
        }

        private float CalcFullWidth(float[] lineWidths, float fullWidth, TextSymDefHorizAlignment objectHorizAlignment)
        {
            TextSymDefHorizAlignment fontAlign = GetHorizAlignment(objectHorizAlignment);

            if (fullWidth == 0) {
                foreach (float w in lineWidths) {
                    if (w > fullWidth)
                        fullWidth = w;
                }
                if (fontAlign == TextSymDefHorizAlignment.Justified || fontAlign == TextSymDefHorizAlignment.Left)
                    fullWidth += firstIndent;   // if fullWidth is zero, this is unformatted text, and only the firstIndent is used.
            }
            return fullWidth;
        }

        // Helper for GetNextTextElement to handle empty/null case.
        static string GetNextTextElement(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            else
                return StringInfo.GetNextTextElement(s);
        }

        // Draw the center point symbol of the text, if applicable.
        private void DrawCenterPoint(IGraphicsTarget g, float[] lineWidths, float fullWidth, SymColor color, float topOfText, float bottomOfText, TextSymDefHorizAlignment objectHorizAlignment, RenderOptions renderOpts) 
        {
            TextSymDefHorizAlignment fontAlign = GetHorizAlignment(objectHorizAlignment);

            // OCAD only draws center point for unformatted text (fullWidth == 0).
            if (centerPointSymdef != null && centerPointSymdef.HasColor(color) && fullWidth == 0) {
                float l, r, t, b;  // bounding rectangle for calculating center point.

                // First, figure out the width of the rectangle. If fullWidth is zero, used the maximum line width.
                fullWidth = CalcFullWidth(lineWidths, fullWidth, fontAlign);

                // Next, figure out the rectangle, not counting padding.
                if (fontAlign == TextSymDefHorizAlignment.Right)
                    l = -fullWidth;
                else if (fontAlign == TextSymDefHorizAlignment.Center)
                    l = -(fullWidth / 2F);
                else
                    l = 0;
                r = l + fullWidth;
                t = topOfText - (FontAscent + FontDescent) + FontEmHeight;  // strange OCAD adjustment.
                b = bottomOfText + FontDescent;  // include the descender on the text.

                PointF centerPoint = new PointF((r + l) / 2, (t + b) / 2);

                // We're currently in a reversed coordinate space;  re-reverse it.
                Matrix matrix = new Matrix();
                matrix.Translate(centerPoint.X, centerPoint.Y);
                matrix.Scale(1, -1);      // Reverse Y so text is correct way aroun
                g.PushTransform(matrix);

                try {
                    centerPointSymdef.Draw(g, new PointF(0, 0), 0, null, color, renderOpts);
                }
                finally {
                    g.PopTransform();  // restore transform
                }
            }
        }

        // Draw an underline under the text, if applicable.
        private void DrawUnderline(IGraphicsTarget g, Underlining underlining, SymColor color, float baseline, float width, float indent, TextSymDefHorizAlignment objectHorizAlignment)
        {
            if (underlining.underlineOn && color != null && color == underlining.underlineColor) {
                TextSymDefHorizAlignment fontAlign = GetHorizAlignment(objectHorizAlignment);

                // Figure out the left and right sides of the underline.
                float l, r;
                if (fontAlign == TextSymDefHorizAlignment.Right)
                    l = -width;
                else if (fontAlign == TextSymDefHorizAlignment.Center)
                    l = -(width / 2F);
                else
                    l = indent;
                r = l + width;

                // figure out y coordinate of line.
                float y = baseline + underlining.underlineDistance + underlining.underlineWidth / 2;

                // Create pen (use the Underlining object for the pen)
                if (!g.HasPen(underlining))
                    GraphicsUtil.CreateSolidPen(g, underlining, underlining.underlineColor.ColorValue, underlining.underlineWidth, LineStyle.Mitered);

                // draw the line.
                g.DrawLine(underlining, new PointF(l, y), new PointF(r, y));
            }
        }


        // Draw this text symbol at point pt with angle ang in this graphics (given color only). 
        internal void Draw(IGraphicsTarget g, string[] text, float[] lineWidths, PointF location, float angle,  float fullWidth, SymColor color, 
                           TextSymDefHorizAlignment objectHorizAlignment, TextSymDefVertAlignment objectVertAlignment, RenderOptions renderOpts)
        {
            // WARNING: changes to this likely mean changes to FindInsertionPoint! Keep them synchronized.

            TextSymDefHorizAlignment fontAlign = GetHorizAlignment(objectHorizAlignment);
            TextSymDefVertAlignment vertAlign = GetVertAlignment(objectVertAlignment);

            Debug.Assert(color != null);

            if (fontColor != null && color != fontColor && 
                (framing.framingStyle == FramingStyle.None || color != framing.framingColor) && 
                (!underline.underlineOn || color != underline.underlineColor) &&
                (centerPointSymdef == null || !centerPointSymdef.HasColor(color)))
                return;

            CreateObjects(g);

            Underlining underliningFontEffect = new Underlining();
            if ((effects & TextEffects.Underline) != 0) {
                underliningFontEffect.underlineOn = true;
                underliningFontEffect.underlineColor = fontColor;
                underliningFontEffect.underlineDistance = FontEmHeight * fontEffectUnderlineDistance;
                underliningFontEffect.underlineWidth = FontEmHeight * fontEffectUnderlineWidth;
            }
            
            // Move location to draw at to the origin.
            Matrix matrix = new Matrix();
            matrix.Translate(location.X, location.Y);
            matrix.Scale(1, -1); // Reverse Y direction so text is correct way around.
            if (angle != 0)
                matrix.RotateAt(-angle, new PointF(0,0));
            g.PushTransform(matrix);

            try {
                // Draw all the lines of text.
                PointF pt = new PointF(0F, 0F);
                float baselineOfLine = 0;          // y coordinate of baseline of line.
                bool firstLineOfPara = true, lastLineOfPara;

                pt.Y -= VertOffset(vertAlign, text);

                float topOfFirstLine = pt.Y;

                for (int lineIndex = 0; lineIndex < text.Length; ++lineIndex) {
                    string line = text[lineIndex];
                    float lineWidth = lineWidths[lineIndex];

                    lastLineOfPara = (lineIndex == text.Length - 1 || text[lineIndex + 1] == ParagraphMark);        // are we on the last line of a paragraph?

                    if (line == ParagraphMark) {
                        pt.Y += paraSpacing;
                        firstLineOfPara = true;
                    }
                    else {
                        float indent = 0;
                        float leftEdge;          // tabs are relative to this X position.
                        if (fontAlign == TextSymDefHorizAlignment.Right)
                            pt.X = leftEdge = -lineWidth;
                        else if (fontAlign == TextSymDefHorizAlignment.Center)
                            pt.X = leftEdge = -(lineWidth / 2F);
                        else {
                            leftEdge = 0;
                            pt.X = indent = firstLineOfPara ? firstIndent : restIndent;     // indents only used for left align or justified
                        }

                        // Get the size of spaces. Justification is done by adjusting this.
                        float sizeOfSpace = wordSpacing * textFaceMetrics.SpaceWidth;            // basic width of spaces as set by the symdef
                        if (fontAlign == TextSymDefHorizAlignment.Justified && !lastLineOfPara && fullWidth > 0)
                            sizeOfSpace += JustifyText(line, lineWidth, fullWidth - indent);

                        // Draw all the text segments in the line. (A text segment is a word, unless charSpacing>0, in which case it is graphemes).
                        int index = 0;
                        for (; ; ) {
                            string textSegment;

                            if (charSpacing > 0)
                                textSegment = GetNextTextElement(line.Substring(index));
                            else
                                textSegment = GetNextTextSegment(line.Substring(index));
                            if (string.IsNullOrEmpty(textSegment))
                                break;

                            if (textSegment == " ")
                                pt.X += sizeOfSpace;
                            else if (textSegment == "\t")
                                pt.X += WidthOfTextSegment("\t", pt.X - leftEdge, fontAlign);
                            else {
                                DrawStringWithEffects(g, color, textSegment, pt);
                                pt.X += MeasureStringWidth(textSegment, fontAlign);

                                if (charSpacing > 0)
                                    pt.X += charSpacing * textFaceMetrics.SpaceWidth;
                            }

                            index += textSegment.Length;
                        }

                        baselineOfLine = pt.Y + FontAscent;        // Set the bottom of the text.

                        // Draw underlining. For the font underlining effect, we draw it line this instead of setting the effect
                        // in the font so that it works right with extra character and space widths.
                        DrawUnderline(g, underliningFontEffect, color, baselineOfLine, lineWidth, indent, fontAlign);
                        if (lastLineOfPara)
                            DrawUnderline(g, this.underline, color, baselineOfLine, Math.Max(fullWidth, lineWidth), (fullWidth == 0) ? indent : 0, fontAlign);

                        pt.Y += lineSpacing;
                        firstLineOfPara = false;
                    }
                }

                // Draw the center point, if any.
                DrawCenterPoint(g, lineWidths, fullWidth, color, topOfFirstLine, baselineOfLine, fontAlign, renderOpts);

                // Draw the framing rectangle, if any.
                if (underline.underlineOn)
                    baselineOfLine += underline.underlineDistance + underline.underlineWidth;
                DrawFramingRectangle(g, lineWidths, fullWidth, color, topOfFirstLine, baselineOfLine, fontAlign);
            }
            finally {
                g.PopTransform();
            }
        }

        public class InsertionPointLocation
        {
            public PointF Baseline, Ascent, Descent;
        }

        // Draw this text symbol at point pt with angle ang in this graphics (given color only). 
        internal InsertionPointLocation FindInsertionPoint(TextCoord textCoord, string[] text, float[] lineWidths, PointF location, float angle, float fullWidth,
                                                           TextSymDefHorizAlignment objectHorizAlignment, TextSymDefVertAlignment objectVertAlignment)
        {
            // WARNING: This function must be kept synchronized to changed to the Draw() function!
            TextSymDefHorizAlignment fontAlign = GetHorizAlignment(objectHorizAlignment);
            TextSymDefVertAlignment vertAlign = GetVertAlignment(objectVertAlignment);

            if (textCoord.Line < 0 || textCoord.Line >= text.Length || textCoord.Col < 0 || textCoord.Col > text[textCoord.Line].Length) {
                return null;
            }

            // Move location to draw at to the origin.
            Matrix matrix = new Matrix();
            matrix.Translate(location.X, location.Y);
            matrix.Scale(1, -1); // Reverse Y direction so text is correct way around.
            if (angle != 0)
                matrix.RotateAt(-angle, new PointF(0, 0));

            // Draw all the lines of text.
            PointF pt = new PointF(0F, 0F);
            bool firstLineOfPara = true, lastLineOfPara;

            pt.Y -= VertOffset(vertAlign, text);

            float topOfFirstLine = pt.Y;

            for (int lineIndex = 0; lineIndex < text.Length; ++lineIndex) {
                string line = text[lineIndex];
                float lineWidth = lineWidths[lineIndex];

                lastLineOfPara = (lineIndex == text.Length - 1 || text[lineIndex + 1] == ParagraphMark);        // are we on the last line of a paragraph?

                if (line == ParagraphMark) {
                    pt.Y += paraSpacing;
                    firstLineOfPara = true;
                }
                else {
                    if (lineIndex == textCoord.Line) {
                        float indent = 0;
                        float leftEdge;          // tabs are relative to this X position.
                        if (fontAlign == TextSymDefHorizAlignment.Right)
                            pt.X = leftEdge = -lineWidth;
                        else if (fontAlign == TextSymDefHorizAlignment.Center)
                            pt.X = leftEdge = -(lineWidth / 2F);
                        else {
                            leftEdge = 0;
                            pt.X = indent = firstLineOfPara ? firstIndent : restIndent;     // indents only used for left align or justified
                        }

                        // Get the size of spaces. Justification is done by adjusting this.
                        float sizeOfSpace = wordSpacing * textFaceMetrics.SpaceWidth;            // basic width of spaces as set by the symdef
                        if (fontAlign == TextSymDefHorizAlignment.Justified && !lastLineOfPara && fullWidth > 0)
                            sizeOfSpace += JustifyText(line, lineWidth, fullWidth - indent);

                        // Draw all the text segments in the line. (A text segment is a word, unless charSpacing>0, in which case it is graphemes).
                        int index = 0;
                        bool indexByGraphemes = charSpacing > 0;  // May change later.
                        for (; ; ) {
                            string textSegment;

                            if (index >= textCoord.Col) {
                                // Found the place!
                                InsertionPointLocation insertionPoint = new InsertionPointLocation();

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

                            if (textSegment == " ")
                                pt.X += sizeOfSpace;
                            else if (textSegment == "\t")
                                pt.X += WidthOfTextSegment("\t", pt.X - leftEdge, fontAlign);
                            else {
                                pt.X += MeasureStringWidth(textSegment, fontAlign);

                                if (charSpacing > 0)
                                    pt.X += charSpacing * textFaceMetrics.SpaceWidth;
                            }

                            index += textSegment.Length;
                        }
                    }

                    pt.Y += lineSpacing;
                    firstLineOfPara = false;
                }
            }

            // Not sure how this happens!
            Debug.Fail("Couldn't find insertion point for this text coord.");
            return null;
        }

        
        // Draw this text symbol at point pt with angle ang in this graphics (given color only). 
        internal bool HitTest(string[] text, float[] lineWidths, PointF location, float angle, float fullWidth,
                              TextSymDefHorizAlignment objectHorizAlignment, TextSymDefVertAlignment objectVertAlignment, 
                              PointF pointTest, float distanceTest, out float actualDistance)
        {
            if (!fontMetricsCreated)
                CreateFontMetrics();

            TextSymDefHorizAlignment fontAlign = GetHorizAlignment(objectHorizAlignment);
            TextSymDefVertAlignment vertAlign = GetVertAlignment(objectVertAlignment);

            // Move location to draw at to the origin.
            Matrix matrix = new Matrix();
            matrix.Translate(location.X, location.Y);
            matrix.Scale(1, -1); // Reverse Y direction so text is correct way around.
            if (angle != 0)
                matrix.RotateAt(-angle, new PointF(0, 0));
            matrix.Invert();  // Invert to translate dest point to source coordinates.
            pointTest = matrix.Transform(pointTest);

            // Check all the lines of text.
            PointF pt = new PointF(0F, 0F);
            bool firstLineOfPara = true, lastLineOfPara;

            pt.Y -= VertOffset(vertAlign, text);

            float topOfFirstLine = pt.Y;

            float minDistanceFromText = float.MaxValue;

            // For each line of text, hit test against that line of text.

            for (int lineIndex = 0; lineIndex < text.Length; ++lineIndex) {
                string line = text[lineIndex];
                float lineWidth = lineWidths[lineIndex];

                lastLineOfPara = (lineIndex == text.Length - 1 || text[lineIndex + 1] == ParagraphMark);        // are we on the last line of a paragraph?

                if (line == ParagraphMark) {
                    pt.Y += paraSpacing;
                    firstLineOfPara = true;
                }
                else {
                    float indent = 0;
                    if (fontAlign == TextSymDefHorizAlignment.Right)
                        pt.X = -lineWidth;
                    else if (fontAlign == TextSymDefHorizAlignment.Center)
                        pt.X = -(lineWidth / 2F);
                    else {
                        pt.X = indent = firstLineOfPara ? firstIndent : restIndent;     // indents only used for left align or justified
                    }

                    // Adjust width for justification.
                    if (fontAlign == TextSymDefHorizAlignment.Justified && !lastLineOfPara && fullWidth > 0)
                        lineWidth = fullWidth - indent;

                    // Hit test against this line.
                    RectangleF textRectangle = new RectangleF(pt.X, pt.Y, lineWidth, lineSpacing);
                    float distanceFromText = Geometry.DistanceFromRectangle(textRectangle, pointTest);
                    if (distanceFromText < minDistanceFromText)
                        minDistanceFromText = distanceFromText;

                    pt.Y += lineSpacing;
                    firstLineOfPara = false;
                }
            }

            // Return minimum of distance from each line of text.
            actualDistance = minDistanceFromText;
            return (minDistanceFromText <= distanceTest);
        }

        // Calculate the additional width to add to spaces to justify a line of text. textWidth is the width of text without
        // adjustment. fullWidth is the size we want the text to be.
        private float JustifyText(string text, float textWidth, float fullWidth)
        {
            int spaceCount = 0;
            for (int i = 0; i < text.Length; ++i)
                if (text[i] == ' ')
                    ++spaceCount;

            if (spaceCount > 0)
                return (fullWidth - textWidth) / spaceCount;
            else
                return 0;
        }

        internal float TextHeight(string[] text)
        {
            // count number of lines, number of new paragraphs.
            int lineCount, newParaCount = 0;
            for (int i = 0; i < text.Length; ++i)
                if (text[i] == ParagraphMark)
                    ++newParaCount;
            lineCount = text.Length - newParaCount;

            // Calculate height of text.
            return (FontAscent + FontDescent + ((lineCount - 1) * lineSpacing) + (newParaCount * paraSpacing));
        }

        // Calculate the bounding box. 
        public RectangleF CalcBounds(string[] text, float[] lineWidths, PointF location, float angle, float fullWidth,
                                     TextSymDefHorizAlignment objectHorizAlignment, TextSymDefVertAlignment objectVertAlignment,
                                     out SizeF size)
        {
            if (!fontMetricsCreated)
                CreateFontMetrics();

            TextSymDefHorizAlignment fontAlign = GetHorizAlignment(objectHorizAlignment);
            TextSymDefVertAlignment vertAlign = GetVertAlignment(objectVertAlignment);

            // Calculate height of text.
            float height = TextHeight(text);

            // Calculate full width of text.
            fullWidth = CalcFullWidth(lineWidths, fullWidth, fontAlign);

            // Get the size.
            size = new SizeF(fullWidth, height);

            // The rectangle, unrotated.
            RectangleF rect;
            if (fontAlign == TextSymDefHorizAlignment.Left || fontAlign == TextSymDefHorizAlignment.Justified)
                rect = new RectangleF(location.X, location.Y - size.Height, size.Width, size.Height);  // indents only used for left aligned and justified text.
            else if (fontAlign == TextSymDefHorizAlignment.Right)
                rect = new RectangleF(location.X - size.Width, location.Y - size.Height, size.Width, size.Height);
            else {
                Debug.Assert(fontAlign == TextSymDefHorizAlignment.Center);
                rect = new RectangleF(location.X - size.Width / 2, location.Y - size.Height, size.Width, size.Height);
            }

            // Expand for framing and underlining.
            if (underline.underlineOn)
                rect = RectangleF.FromLTRB(rect.Left, rect.Top - (underline.underlineDistance + underline.underlineWidth), rect.Right, rect.Bottom);

            if (framing.framingStyle == FramingStyle.Line)
                rect.Inflate(framing.lineWidth, framing.lineWidth);
            else if (framing.framingStyle == FramingStyle.Shadow) {
                RectangleF shadow = rect;
                shadow.Offset(framing.shadowX, framing.shadowY);
                rect = RectangleF.Union(rect, shadow);
            }
            else if (framing.framingStyle == FramingStyle.Rectangle) 
                rect = RectangleF.FromLTRB(rect.Left - framing.rectBorderLeft, rect.Top - framing.rectBorderBottom, rect.Right + framing.rectBorderRight, rect.Bottom + framing.rectBorderTop);

            // Adjust for vertical alignment
            rect.Offset(0, VertOffset(vertAlign, text));

            // Rotate the rectangle.
            if (angle != 0)
                rect = Geometry.BoundsOfRotatedRectangle(rect, location, angle);

            return rect;
        }

        internal SymPath GetHighlightPath(string[] text, float[] lineWidths, PointF location,
                                          TextSymDefHorizAlignment objectHorizAlignment, TextSymDefVertAlignment objectVertAlignment,
                                          float rotation, float width)
        {
            if (!fontMetricsCreated)
                CreateFontMetrics();

            TextSymDefHorizAlignment fontAlign = GetHorizAlignment(objectHorizAlignment);
            TextSymDefVertAlignment vertAlign = GetVertAlignment(objectVertAlignment);

            // count number of lines, number of new paragraphs.
            int lineCount, newParaCount = 0;
            for (int i = 0; i < text.Length; ++i)
                if (text[i] == ParagraphMark)
                    ++newParaCount;
            lineCount = text.Length - newParaCount;

            // Calculate height of text.
            float height = FontAscent + FontDescent + ((lineCount - 1) * lineSpacing) + (newParaCount * paraSpacing);

            // Calculate full width of text.
            float fullWidth = CalcFullWidth(lineWidths, width, fontAlign);

            SizeF size = new SizeF(fullWidth, height);

            // The rectangle, unrotated. First adjust horizonal alignment.
            RectangleF rect;
            if (fontAlign == TextSymDefHorizAlignment.Left || fontAlign == TextSymDefHorizAlignment.Justified)
                rect = new RectangleF(location.X, location.Y - size.Height, size.Width, size.Height);  // indents only used for left aligned and justified text.
            else if (fontAlign == TextSymDefHorizAlignment.Right)
                rect = new RectangleF(location.X - size.Width, location.Y - size.Height, size.Width, size.Height);
            else {
                Debug.Assert(fontAlign == TextSymDefHorizAlignment.Center);
                rect = new RectangleF(location.X - size.Width / 2, location.Y - size.Height, size.Width, size.Height);
            }

            // Adjust for vertical alignment
            rect.Offset(0, VertOffset(vertAlign, text));

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

        // Breaks unwrapped text into paragraphs. This just means adding a paragraph mark
        // between each line. The lineWidths array has the width of each line.
        //
        // Also retursn a TextCoordMapper object to remember the mapping.
        public string[] BreakUnwrappedLines(string[] text, TextSymDefHorizAlignment objectHorizAlignment, out TextCoordMapper coordMapper, out float[] lineWidths)
        {
            return BreakUnwrappedLines(GetWrapProperties(objectHorizAlignment), text, out coordMapper, out lineWidths);
        }

        // Breaks unwrapped text into paragraphs. This just means adding a paragraph mark
        // between each line. The lineWidths array has the width of each line.
        //
        // Also retursn a TextCoordMapper object to remember the mapping.
        internal static string[] BreakUnwrappedLines(WrapTextProperties wrapProperties, string[] text, out TextCoordMapper coordMapper, out float[] lineWidths)
        {
            coordMapper = new TextCoordMapper();

            int firstLine = 0;

            if (text.Length == firstLine) {
                lineWidths = new float[0];
                return new string[0];
            }

            if (wrapProperties.AddParagraphMarks) {
                string[] newLines = new string[(text.Length - firstLine) * 2 - 1];
                lineWidths = new float[newLines.Length];

                for (int i = 0; i < text.Length - firstLine; ++i) {
                    newLines[i * 2] = text[i + firstLine];
                    lineWidths[i * 2] = LineWidth(wrapProperties, text[i + firstLine]);
                    coordMapper.AddUnwrappedCoord(new TextCoord(i + firstLine, 0));

                    if (i + firstLine != text.Length - 1) {
                        newLines[i * 2 + 1] = ParagraphMark;
                        lineWidths[i * 2 + 1] = 0;
                        coordMapper.AddUnwrappedCoord(new TextCoord(-1, -1));  // Paragraph marks don't correspond to any unwrapped text.
                    }
                }

                return newLines;
            }
            else {
                string[] newLines = new string[(text.Length - firstLine)];
                lineWidths = new float[newLines.Length];

                for (int i = 0; i < text.Length - firstLine; ++i) {
                    newLines[i] = text[i + firstLine];
                    lineWidths[i] = LineWidth(wrapProperties, text[i + firstLine]);
                    coordMapper.AddUnwrappedCoord(new TextCoord(i + firstLine, 0));
                }

                return newLines;
            }
        }

        // Breaks the text into lines based on the given width. Returns a new string array with
        // line breaks made into it. A line break that already exists is turned into a paragraph break,
        // which is a line with just a unicode paragraph separator in it. The line widths array has the 
        // width of each line.
        internal string[] BreakLines(string[] text, float width, TextSymDefHorizAlignment objectHorizAlignment, out TextCoordMapper coordMapper, out float[] lineWidths, out bool didWrapAtLeastOneLine)
        {
            return BreakLines(GetWrapProperties(objectHorizAlignment), text, width, out coordMapper, out lineWidths, out didWrapAtLeastOneLine);
        }
        
        // Breaks the text into lines based on the given width. Returns a new string array with
        // line breaks made into it. A line break that already exists is turned into a paragraph break,
        // which is a line with just a unicode paragraph separator in it. (This can be turned off in WrapTextProperties).
        // The line widths array has the width of each line.
        //
        // Also retursn a TextCoordMapper object to remember the mapping.
        internal static string[] BreakLines(WrapTextProperties wrapProperties, string[] text, float width, out TextCoordMapper coordMapper, out float[] lineWidths, out bool didWrapAtLeastOneLine)
        {
            List<String> lineList = new List<String>();
            List<float> widthList = new List<float>();
            coordMapper = new TextCoordMapper();
            didWrapAtLeastOneLine = false;

            float widthFirstLine, widthRemainingLines;
            if (wrapProperties.FontAlign == TextSymDefHorizAlignment.Left || wrapProperties.FontAlign == TextSymDefHorizAlignment.Justified) {
                widthFirstLine = Math.Max(0F, width - wrapProperties.FirstIndent);
                widthRemainingLines = Math.Max(0F, width - wrapProperties.RestIndent);
            }
            else {
                // indents only used for left-aligned and justified text.
                widthFirstLine = widthRemainingLines = width;
            }

            int firstLine = 0;

            for (int i = firstLine; i < text.Length; ++i) {
                TextCoordMapper coordMapperForThisLine;

                int nLines = WrapParagraph(wrapProperties, text[i], widthFirstLine, widthRemainingLines, lineList, widthList, out coordMapperForThisLine);
                if (nLines > 1)
                    didWrapAtLeastOneLine = true;

                coordMapper.AddCoordMapper(i, coordMapperForThisLine);
                
                if (i < text.Length - 1 && wrapProperties.AddParagraphMarks) {
                    lineList.Add(ParagraphMark);
                    widthList.Add(0);
                    coordMapper.AddUnwrappedCoord(new TextCoord(-1, -1));  // Paragraph marks don't correspond to any unwrapped line.
                }
            }

            lineWidths = widthList.ToArray();
            return lineList.ToArray();
        }

        // Split text into lines of length width or less, and add those lines to the given ArrayList (and widths of the lines to the width list).
        // Return number of lines the paragraphs was wrapped onto. 
        // Creates a TextCoordMapper object to remember how the text was wrapped.
        private static int WrapParagraph(WrapTextProperties wrapProperties, string text, float widthFirstLine, float widthRemainingLines, List<String> lineList, List<float> widthList, out TextCoordMapper coordMapper)
        {
            int unwrappedCol = 0;
            int translateLine = 0;
            coordMapper = new TextCoordMapper();
            
            bool firstLine = true;
            while (text != null) {
                float lineWidth;
                int textLengthBeforeWrap = text.Length;

                string line = WrapOneLine(wrapProperties, ref text, firstLine ? widthFirstLine : widthRemainingLines, out lineWidth);
                lineList.Add(line);
                widthList.Add(lineWidth);
                firstLine = false;
                coordMapper.AddUnwrappedCoord(new TextCoord(0, unwrappedCol));

                unwrappedCol += (textLengthBeforeWrap - ((text == null) ? 0 : text.Length));
                translateLine += 1;
            }

            return translateLine;
        }

        // Figure out how much of the line will fit and return that. line is modified
        // to be the remaining text to fit on subsequent lines, or null if nothing left. The amount of width
        // actually consumed is returned in actualLineWidth.
        private static string WrapOneLine(WrapTextProperties wrapProperties, ref string line, float lineWidth, out float actualLineWidth)
        {
            StringBuilder lineSoFar = new StringBuilder();
            float widthUsed = 0F;
            bool useSingleLetters = false;

            while (!string.IsNullOrEmpty(line)) {
                // Get next segment of text to add.
                string nextSegment;
                if (useSingleLetters)
                    nextSegment = GetNextTextElement(line);
                else
                    nextSegment = GetNextTextSegment(line);
                if (string.IsNullOrEmpty(nextSegment))
                    break;

                // See if this segment will fit on the line.
                float segmentWidth = WidthOfTextSegment(wrapProperties, nextSegment, widthUsed);
                if (segmentWidth + widthUsed > lineWidth) {
                    // The segment won't fit. If we haven't placed any segments yet, we need to try placing single letters.
                    if (widthUsed == 0) {
                        if (!useSingleLetters) {
                            useSingleLetters = true;
                            continue;
                        }
                    }
                    else
                        break;  // we're done.
                }

                // Add nextSegment to the line, and remove from the line under consideration.
                lineSoFar.Append(nextSegment);
                line = line.Substring(nextSegment.Length);
                widthUsed += segmentWidth;
            }

            // If we're wrapping, the new line replaces spaces.
            if (line.Length > 0) {
                // Remove trailing spaces from current line.
                while (lineSoFar.Length > 0 && lineSoFar[lineSoFar.Length - 1] == ' ') {
                    lineSoFar.Remove(lineSoFar.Length - 1, 1);
                    widthUsed -= WidthOfTextSegment(wrapProperties, " ", widthUsed);
                }

                // Remove initial spaces from next line.
                int startNextLine = 0;
                while (startNextLine < line.Length && line[startNextLine] == ' ')
                    ++startNextLine;
                if (startNextLine > 0)
                    line = line.Substring(startNextLine);
            }

            if (line == "")
                line = null;

            actualLineWidth = widthUsed;
            return lineSoFar.ToString();
        }

        // Calculate width of one line, not limited by wrapping.
        private float LineWidth(string text, TextSymDefHorizAlignment objectHorizAlignment)
        {
            return LineWidth(GetWrapProperties(objectHorizAlignment), text);
        }
            
        // Calculate width of one line, not limited by wrapping.
        private static float LineWidth(WrapTextProperties wrapProperties, string text)
        {
            string nextSegment;
            float width = 0;

            while ((nextSegment = GetNextTextSegment(text)) != null) {
                width += WidthOfTextSegment(wrapProperties, nextSegment, width);
                text = text.Substring(nextSegment.Length);
            }

            return width;
        }

        // Get the next text segment needed. If empty, returns null.
        // Is the next is a space or tab, that is it.
        // Otherwise, the whole word to the next space or tab.
        private static string GetNextTextSegment(string text)
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

                // Get the width of a text segment. Handles tabs, spaces between characters, and space widths.
        private float WidthOfTextSegment(string text, float widthSoFar, TextSymDefHorizAlignment objectHorizAlignment)
        {
            return WidthOfTextSegment(GetWrapProperties(objectHorizAlignment), text, widthSoFar);
        }

        // Get the width of a text segment. Handles tabs, spaces between characters, and space widths.
        private static float WidthOfTextSegment(WrapTextProperties wrapProperties, string text, float widthSoFar)
        {
            if (text == " ") {
                return wrapProperties.TextFaceMetrics.SpaceWidth * wrapProperties.WordSpacing;
            }
            else if (text == "\t") {
                float[] tabs = wrapProperties.Tabs;

                if (tabs == null || tabs.Length == 0)
                    return 0;          // no tabs.
                else {
                    // Find first tab stop beyond the current location.
                    for (int i = 0; i < tabs.Length; ++i) {
                        if (tabs[i] > widthSoFar)
                            return tabs[i] - widthSoFar;
                    }

                    // Find repeating tabs and first tab stop beyond current location.
                    float tabStop, tabInc;
                    if (tabs.Length > 2 && tabs[tabs.Length - 1] > tabs[tabs.Length - 2]) {
                        tabStop = tabs[tabs.Length - 1];
                        tabInc = tabs[tabs.Length - 1] - tabs[tabs.Length - 2];
                    }
                    else {
                        tabStop = tabInc = tabs[tabs.Length - 1];
                    }

                    for (; ; ) {
                        if (tabStop > widthSoFar)
                            return tabStop - widthSoFar;
                        tabStop += tabInc;
                    }
                }
            }
            else if (wrapProperties.CharSpacing > 0) {
                float width = 0;
                TextElementEnumerator enumTextElements = StringInfo.GetTextElementEnumerator(text);
                while (enumTextElements.MoveNext()) 
                    width += MeasureStringWidth(wrapProperties, enumTextElements.GetTextElement()) + 
                             (wrapProperties.CharSpacing * wrapProperties.TextFaceMetrics.SpaceWidth);
                return width;
            }
            else {
                return MeasureStringWidth(wrapProperties, text);
            }
        }




        // The remaining functions all have to do with text along a path ("line text").

        // Structure to show where one character (which might be multiple Unicode codepoints), hence Grapheme, is placed
        // when laying out line text.
        struct GraphemePlacement
        {
            public string grapheme;       // The grapheme to draw.
            public int indexInText;       // Index of the grapheme in the text.
            public float width;                // The width of this grapheme.
            public PointF pointStart;      // The location the grapheme starts at.
            public float angle;                // The angle of the grapheme.

            public GraphemePlacement(string grapheme, int indexInText, float width, PointF pointStart, float angle)
            {
                this.grapheme = grapheme;
                this.indexInText = indexInText;
                this.width = width;
                this.pointStart = pointStart;
                this.angle = angle;
            }
        }

        // Draw this text symbol along a path.
        internal void DrawTextOnPath(IGraphicsTarget g, SymPath path, string text, SymColor color,
                                     TextSymDefHorizAlignment objectHorizAlignment, TextSymDefVertAlignment objectVertAlignment,
                                     RenderOptions renderOpts)
        {
            TextSymDefHorizAlignment fontAlign = GetHorizAlignment(objectHorizAlignment);
            TextSymDefVertAlignment vertAlign = GetVertAlignment(objectVertAlignment);

            if (color == null)
                return;
            if (color != fontColor && (framing.framingStyle == FramingStyle.None || color != framing.framingColor))
                return;

            CreateObjects(g);

            // Get the location of each grapheme to print.
            List<GraphemePlacement> graphemeList = GetLineTextGraphemePlacement(path, text, fontAlign);

            // Get location to draw to, relative to the line we're drawing along.
            PointF topAscentPoint = new PointF(0, -LineTextVertOffset(vertAlign));

            foreach (GraphemePlacement grapheme in graphemeList) {
                // Move location to draw at to the origin, set angle for drawing text.
                Matrix matrix = new Matrix();
                matrix.Translate(grapheme.pointStart.X, grapheme.pointStart.Y);
                matrix.Scale(1, -1);      // Reverse Y so text is correct way aroun
                matrix.RotateAt(-grapheme.angle, new PointF(0,0));
                g.PushTransform(matrix);

                try
                {
                    DrawStringWithEffects(g, color, grapheme.grapheme, topAscentPoint);
                }
                finally {
                    g.PopTransform();  // restore transform
                }
            }
        }

        internal InsertionPointLocation FindInsertionPointOnPath(SymPath path, string text, int textIndex,
                                        TextSymDefHorizAlignment objectHorizAlignment, TextSymDefVertAlignment objectVertAlignment)
        {
            TextSymDefHorizAlignment fontAlign = GetHorizAlignment(objectHorizAlignment);
            TextSymDefVertAlignment vertAlign = GetVertAlignment(objectVertAlignment);

            // Get the location of each grapheme to print.
            List<GraphemePlacement> graphemeList = GetLineTextGraphemePlacement(path, text, fontAlign);

            // Add one more zero-width space at the end to handle the last character.
            if (graphemeList.Count > 0) {
                GraphemePlacement lastGrapheme = graphemeList[graphemeList.Count - 1];
                graphemeList.Add(new GraphemePlacement(" ", text.Length, 0, Geometry.MoveDistance(lastGrapheme.pointStart, lastGrapheme.width, lastGrapheme.angle), lastGrapheme.angle));
            }
            else {
                PointF nearPoint = path.PointAtLength(0.01F, map.MapDistanceMetric);
                graphemeList.Add(new GraphemePlacement(" ", text.Length, 0, path.FirstPoint, (float) (Math.Atan2(nearPoint.Y - path.FirstPoint.Y, nearPoint.X - path.FirstPoint.X) * 180 / Math.PI)));
            }

            // Get location to draw to, relative to the line we're drawing along.
            PointF topAscentPoint;
            topAscentPoint = new PointF(0, -LineTextVertOffset(vertAlign));

            for (int i = 0; i < graphemeList.Count; ++i) {
                GraphemePlacement grapheme = graphemeList[i];

                // If we are at the desired location, or at the last grapheme (our zero-width space), use this one.
                // The text can be truncated, so we can easily go off the end.
                if ((textIndex >= grapheme.indexInText && textIndex < grapheme.indexInText + grapheme.grapheme.Length) ||
                    i == graphemeList.Count - 1) 
                {
                    // Move location to draw at to the origin, set angle for drawing text.
                    Matrix matrix = new Matrix();
                    matrix.Translate(grapheme.pointStart.X, grapheme.pointStart.Y);
                    matrix.Scale(1, -1);      // Reverse Y so text is correct way aroun
                    matrix.RotateAt(-grapheme.angle, new PointF(0, 0));

                    // Found the place!
                    InsertionPointLocation insertionPoint = new InsertionPointLocation();

                    // Find all three points along the insertion point, as transformed.
                    PointF insertPt = topAscentPoint;
                    insertionPoint.Ascent = Geometry.TransformPoint(insertPt, matrix);
                    insertPt.Y += FontAscent;
                    insertionPoint.Baseline = Geometry.TransformPoint(insertPt, matrix);
                    insertPt.Y += FontDescent;
                    insertionPoint.Descent = Geometry.TransformPoint(insertPt, matrix);
                    return insertionPoint;
                }
            }

            return null;
        }


        // Draw this text symbol along a path.
        internal bool HitTestTextOnPath(SymPath path, string text, PointF pointTest, float distanceTest,
                                        TextSymDefHorizAlignment objectHorizAlignment, TextSymDefVertAlignment objectVertAlignment,
                                        out float actualDistance)
        {
            if (!fontMetricsCreated)
                CreateFontMetrics();

            TextSymDefHorizAlignment fontAlign = GetHorizAlignment(objectHorizAlignment);
            TextSymDefVertAlignment vertAlign = GetVertAlignment(objectVertAlignment);

            // Much quicker test to rule out points -- test distance from the path.
            float distanceFromPath = path.DistanceFromPoint(pointTest);
            if (distanceFromPath > distanceTest + WHeight + FontDescent) {
                actualDistance = float.MaxValue;
                return false;
            }

            // Get the location of each grapheme to print.
            List<GraphemePlacement> graphemeList = GetLineTextGraphemePlacement(path, text, fontAlign);

            // Get location to draw to, relative to the line we're drawing along.
            PointF topAscentPoint = new PointF(0, -LineTextVertOffset(vertAlign));
            topAscentPoint.Y -= (WHeight - FontAscent);

            float minDistanceFromText = float.MaxValue;

            foreach (GraphemePlacement grapheme in graphemeList) {
                // Move location to draw at to the origin, set angle for drawing text.
                Matrix matrix = new Matrix();
                matrix.Translate(grapheme.pointStart.X, grapheme.pointStart.Y);
                matrix.Scale(1, -1);      // Reverse Y so text is correct way aroun
                matrix.RotateAt(-grapheme.angle, new PointF(0, 0));
                matrix.Invert();  // Because we're transforming a test point backward.
                PointF transformedTestPoint = matrix.Transform(pointTest);

                RectangleF textRectangle = new RectangleF(topAscentPoint.X, topAscentPoint.Y, grapheme.width, WHeight + FontDescent);
                float distanceFromText = Geometry.DistanceFromRectangle(textRectangle, transformedTestPoint);
                if (distanceFromText < minDistanceFromText)
                    minDistanceFromText = distanceFromText;
            }

            // Return minimum of distance from each line of text.
            actualDistance = minDistanceFromText;
            return (minDistanceFromText <= distanceTest);
        }


        // Calculate bounds of text along a path with this symbol.
        internal RectangleF CalcBounds(SymPath path, string text)
        {
            // This doesn't take into account the text at all -- just the line. It is probably good enought. We could do better 
            // with a bunch of work by calling GetLineTextGraphemePlacement() and unioning the bounds of each grapheme.
            // But not that necessary.

            RectangleF rect = path.BoundingBox;
            rect.Inflate(FontAscent + FontDescent, FontAscent + FontDescent);

            if (framing.framingStyle == FramingStyle.Line)
                rect.Inflate(framing.lineWidth, framing.lineWidth);
            else if (framing.framingStyle == FramingStyle.Shadow)
                rect.Inflate(Math.Max(framing.shadowX, framing.shadowY), Math.Max(framing.shadowX, framing.shadowY));

            return rect;
        }

        // Determine where each grapheme (essentially character) will go when drawing text along a path.
        // The algorithm for how text is laid out along a path makes sense, with one MAJOR weirdness.
        // The basic algorithm is that the width of each character to draw is determined. Starting where the last
        // character ends, the path is followed for that character width (around corners if needed), and the point along 
        // the path at that point is connected to the start point of the character with a line. The character is drawn along that
        // baseline, starting at the start point. The end point is used as the start of the next character. If there are bends, 
        // of course, the character won't end right at that end point, but we ignore that.
        //
        // THe weirdness is this: instead of measuring distance along the path correctly with the Pythagorian formula, a
        // strange alternate metric of dx + 1/2 dy is used, where dx is the larger ordinate delta and dy is the smaller ordinate
        // delta. Thus, text along diagonals is squished together more than it should be. I have no explanation as to why
        // this might work but it reproduces what OCAD does. See the function "BizzarroDistance" in SymPath.
        private List<GraphemePlacement> GetLineTextGraphemePlacement(SymPath path, string text, TextSymDefHorizAlignment objectHorizAlignment)
        {
            TextSymDefHorizAlignment fontAlign = GetHorizAlignment(objectHorizAlignment);
            float totalWidth = 0;
            List<GraphemePlacement> graphemeList = new List<GraphemePlacement>();
            float pathLength = path.BizzarroLength;
            if (pathLength == 0)
                return graphemeList;            // nothing to draw.

            // First, determine all the graphemes and their width
            if (!string.IsNullOrEmpty(text)) {
                TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(text);
                while (enumerator.MoveNext()) {
                    string grapheme = enumerator.GetTextElement();
                    float graphemeWidth;

                    if (grapheme == " ")
                        graphemeWidth = wordSpacing * textFaceMetrics.SpaceWidth;
                    else {
                        float width = MeasureStringWidth(grapheme, fontAlign);
                        graphemeWidth = width + charSpacing * textFaceMetrics.SpaceWidth;
                    }

                    graphemeList.Add(new GraphemePlacement(grapheme, enumerator.ElementIndex, graphemeWidth, new PointF(), 0));
                    totalWidth += graphemeWidth;
                    if (totalWidth + 0.01F >= pathLength && fontAlign != TextSymDefHorizAlignment.Justified)
                        break;          // We don't have any room for more characters. (0.01 prevents a very small tail at the end.)
                }
            }

            // For OCAD compatibility, truncate right aligned text if too big to fit so the whole
            // string fits. (Note that left-aligned text will typically show one more character than this.)
            if (pathLength < totalWidth && fontAlign != TextSymDefHorizAlignment.Left && fontAlign != TextSymDefHorizAlignment.Justified) {
                totalWidth -= graphemeList[graphemeList.Count - 1].width;
                if (fontAlign == TextSymDefHorizAlignment.Right)
                    graphemeList.RemoveAt(graphemeList.Count - 1);
            }

            // Where does the text begin?
            float startingDistance = 0;
            if (fontAlign == TextSymDefHorizAlignment.Left || fontAlign == TextSymDefHorizAlignment.Justified)
                startingDistance = 0;
            else if (fontAlign == TextSymDefHorizAlignment.Right)
                startingDistance = pathLength - totalWidth;
            else if (fontAlign == TextSymDefHorizAlignment.Center)
                startingDistance = (pathLength - totalWidth) / 2;

            // For justified (all-line) text, adjust the widths of each character so they all fit.
            if (fontAlign == TextSymDefHorizAlignment.Justified && graphemeList.Count > 1) {
                if (charSpacing > 0) {
                    // last character doesn't have space added.
                    GraphemePlacement graphemePlacement = graphemeList[graphemeList.Count - 1];
                    graphemePlacement.width -= charSpacing * textFaceMetrics.SpaceWidth;
                    totalWidth -= charSpacing * textFaceMetrics.SpaceWidth;
                    graphemeList[graphemeList.Count - 1] = graphemePlacement;
                }

                float adjustment = (pathLength - totalWidth) / (graphemeList.Count - 1);
                for (int i = 0; i < graphemeList.Count - 1; ++i) {
                    GraphemePlacement graphemePlacement = graphemeList[i];
                    graphemePlacement.width += adjustment;
                    graphemeList[i] = graphemePlacement;
                }
            }

            // Find points along the path that are the start/end of each grapheme.
            PointF[] points = new PointF[graphemeList.Count + 1];
            float curDistance = startingDistance;
            for (int i = 0; i < graphemeList.Count; ++i) {
                points[i] = path.PointAtLengthBizzarro(curDistance);
                curDistance += graphemeList[i].width;
            }
            points[graphemeList.Count] = path.PointAtLengthBizzarro(Math.Min(curDistance, pathLength));

            // Fill in graphemeList with start points and angles
            for (int i = 0; i < graphemeList.Count; ++i) {
                GraphemePlacement graphemePlacement = graphemeList[i];
                graphemePlacement.pointStart = points[i];
                float distX = points[i + 1].X - points[i].X;
                float distY = points[i + 1].Y - points[i].Y;
                graphemePlacement.angle = (float) (Math.Atan2(distY, distX) * 360.0 / (Math.PI * 2));
                graphemeList[i] = graphemePlacement;
            }

            return graphemeList;
        }

        string[] emptyText = new string[1] { " " };

        // OCAD aligns text vertically a little differently than the map rendering. This function calculated
        // the OCAD vertical adjustment amount, for formatted or unformatted text.
        internal float GetOcadTopAdjustment(bool formatted, int version) {
            float topAdjust = 0;

            if (formatted) {
                // OCAD adds an extra internal leading (incorrectly).
                topAdjust = this.FontEmHeight - (this.FontAscent + this.FontDescent);

                // OCAD always aligns formatted text by the top, no matter when the alignment is.
                topAdjust -= VertOffset(this.VertAlignment, emptyText);
            }
            else {
                if (version >= 10) {
                    // OCAD version 10 and up support vertical alignment for unformatted text.
                    if (this.VertAlignment == TextSymDefVertAlignment.TopAscent) {
                        topAdjust = (this.FontAscent - this.WHeight);
                    }
                    else if (this.VertAlignment == TextSymDefVertAlignment.Midpoint) {
                        topAdjust = (this.FontAscent - this.WHeight) / 2;
                    }
                }
                else {
                    // OCAD version 9 and below do not support vertical alignment, but always align by baseline.
                    topAdjust = this.FontAscent - VertOffset(this.VertAlignment, emptyText);
                }
            }

            return topAdjust;
        }

        // OCAD alway stores location of formatted text by left edge, so this adjusts it to the correct
        // horizontal location.
        internal float GetOcadFormattedHorizAdjustment(float width)
        {
            if (this.FontAlignment == TextSymDefHorizAlignment.Right)
                return width;
            else if (this.FontAlignment == TextSymDefHorizAlignment.Center)
                return width / 2;
            else
                return 0;
        }
    }

    // The HoleSymDef is a dummy symdef used for holes -- There is a singleton HoleSymDef that
    // is used as the symdef for all of these objects, if there are any. It is never added to a map.
    public class HoleSymDef: SymDef
    {
        private HoleSymDef()
            : base("Hole", "hole")
        {
        }

        public static readonly HoleSymDef Singleton = new HoleSymDef();

        public override SymDef CopyToMap(Map map)
        {
            return this;
        }

        // Does this symbol definition draw the given color. Used to determine
        // if this symbol definition draws into the current layer being draw.
        public override bool HasColor(SymColor color)
        {
            return false;
        }

        public override void FreeGdiObjects()
        {
        }

    }



    // The GraphicsSymDef is a sort of dummy symdef used for graphics objects -- objects
    // created by doing a "To Graphics" operation in OCAD. These objects define their own color and shape,
    // so there really isn't any state in the symdef that is useful. There is a singleton GraphicsSymDef that
    // is used as the symdef for all of these objects, if there are any.
    public class GraphicsSymDef: SymDef
    {
        public GraphicsSymDef()
            : base("Graphics object", "-2")
        {
        }

        public override SymDef CopyToMap(Map map)
        {
            var newSymDef = new GraphicsSymDef();
            map.AddSymdef(newSymDef);
            return newSymDef;
        }

        // Does this symbol definition draw the given color. Used to determine
        // if this symbol definition draws into the current layer being draw.
        public override bool HasColor(SymColor color)
        {
            // Always return true, because graphics objects could be any color! (except image layer)
            if (color.IsSpecialLayer)
                return false;
            else
                return true;
        }

        public override void FreeGdiObjects()
        {
        }

    }

    // The ImageSymDef is a sort of dummy symdef used for image objects and layout objects -- 
    // created by doing a import image operation in OCAD, or layout object. These objects live in a layer below
    // all others or above all others. There is a singleton ImageSymDef that
    // is used as the symdef for all of these objects, if there are any.
    public class ImageSymDef: SymDef
    {
        private SymLayer layer;

        public ImageSymDef(SymLayer layer)
            : base(layer == SymLayer.Image ? "Image object" : "Layout object", 
                   layer == SymLayer.Image ? "-3" : "-4")
        {
            Debug.Assert(layer != SymLayer.Normal);
            this.layer = layer;
        }

        public override SymDef CopyToMap(Map map)
        {
            var newSymDef = new ImageSymDef(layer);
            map.AddSymdef(newSymDef);
            return newSymDef;
        }

        // Does this symbol definition draw the given color. Used to determine
        // if this symbol definition draws into the current layer being draw.
        public override bool HasColor(SymColor color)
        {
            return (color.Layer == layer);  // only draw in the layer we are associated with.
        }

        public override void FreeGdiObjects()
        {
        }

        public SymLayer Layer
        {
            get { return layer; }
        }

        // Image and layout objects have a sort order that needs to be preserved.
        public override bool SortSymbolsForDrawing
        {
            get { return true; }
        }

        // OCAD aligns text vertically a little differently than the map rendering. This function calculated
        // the OCAD vertical adjustment amount, for formatted or unformatted text.
        internal float GetOcadTopAdjustment(string fontName, float fontSize, bool formatted)
        {
            using (ITextFaceMetrics textFaceMetrics = map.TextMetricsProvider.GetTextFaceMetrics(fontName, fontSize, TextEffects.None)) {
                if (formatted) {
                    // OCAD adds an extra internal leading (incorrectly).
                    return textFaceMetrics.EmHeight - (textFaceMetrics.Ascent + textFaceMetrics.Descent);
                }
                else {
                    return textFaceMetrics.Ascent;
                }
            }
        }
    }

    public class RectangleSymDef : SymDef
    {
        const float gridWidth = 0.15F;
        const string fontName = "Arial";

        SymColor lineColor;
        float thickness;
        float cornerRadius;
        ushort gridFlags;
        float cellWidth, cellHeight;
        float textSize;
        int unnumberedCells;   // number of unnumbered cells
        string unnumberedText;  // text of unnumbered calls

        object mainPen = new object();
        object gridPen = new object();
        object font = new object();
        object brush = new object();
        float fontAscent;

        public SymColor LineColor { get { return lineColor; } }
        public float LineThickness { get { return thickness; } }

        public ushort GridFlags { get { return gridFlags; } }
        public float CellWidth { get { return cellWidth; }}
        public float CellHeight { get {return cellHeight; }}

        public float CornerRadius { get { return cornerRadius; } }

        public int UnnumberedCells { get { return unnumberedCells; }}

        public string UnnnumberedText { get { return unnumberedText; }}

        public float TextSize { get { return textSize; }}

        public RectangleSymDef(string name, string symbolId, SymColor lineColor, float thickness, float cornerRadius, ushort gridFlags, float cellWidth, float cellHeight, float textSize, int unnumberedCells, string unnumberedText)
            : base(name, symbolId)
        {
            this.lineColor = lineColor;
            this.thickness = thickness;
            this.cornerRadius = cornerRadius;
            this.gridFlags = gridFlags;
            this.cellWidth = cellWidth;
            this.cellHeight = cellHeight;
            this.textSize = textSize;
            this.unnumberedCells = unnumberedCells;
            this.unnumberedText = unnumberedText;
        }

        public override void SetMap(Map newMap)
        {
            base.SetMap(newMap);

            if (newMap != null) {
                // check colors.
                CheckColor(lineColor);
            }
        }

        public override SymDef CopyToMap(Map map)
        {
            var newSymDef = new RectangleSymDef(Name, SymbolId, map.SymColorFromSymColor(lineColor), thickness, cornerRadius, gridFlags, cellWidth, cellHeight, textSize, unnumberedCells, unnumberedText);
            map.AddSymdef(newSymDef);
            return newSymDef;
        }

        void CreateObjects(IGraphicsTarget g)
        {
            if (thickness > 0.0F && lineColor != null) {
                if (! g.HasPen(mainPen)) {
                    GraphicsUtil.CreateSolidPen(g, mainPen, lineColor.ColorValue, thickness, ((cornerRadius > 0) ? LineStyle.Rounded : LineStyle.Mitered));
                }
            }

            if (lineColor != null && (gridFlags & 1) != 0) {
                if (! g.HasPen(gridPen))
                    GraphicsUtil.CreateSolidPen(g, gridPen, lineColor.ColorValue, gridWidth, LineStyle.Beveled);
            }

            if (lineColor != null && (gridFlags & 1) != 0 && (gridFlags & 2) != 0) {
                if (!g.HasFont(font)) {
                    g.CreateFont(font, fontName, textSize, TextEffects.Bold);
                    using (ITextFaceMetrics textFaceMetrics = map.TextMetricsProvider.GetTextFaceMetrics(fontName, textSize, TextEffects.Bold))
                        fontAscent = textFaceMetrics.Ascent;

                }
                if (!g.HasBrush(brush)) {
                    g.CreateSolidBrush(brush, lineColor.ColorValue);
                }
            }
        }

        // Does this symbol definition draw the given color. Used to determine
        // if this symbol definition draws into the current layer being draw.
        public override bool HasColor(SymColor color)
        {
            Debug.Assert(color != null);
            if (color.IsSpecialLayer)
                return false;
            return (lineColor == color);
        }

        // Draw this point symbol at point pt with angle ang in this graphics (given color only).
        internal void Draw(IGraphicsTarget g, PointF location, SizeF size, float rotation, SymColor color, RenderOptions renderOpts)
        {
            if (size.Width == 0&& size.Height == 0)
                return;

            CreateObjects(g);

            // Draw the outside.
            if (lineColor != null && color == lineColor && thickness > 0.0F) {
                GetPath(location, size, rotation).Draw(g, mainPen);
            }

            if ((gridFlags & 1) != 0) {
                // We have a grid.
                Matrix matrix = new Matrix();
                matrix.Translate(location.X, location.Y);
                matrix.Rotate(rotation);

                g.PushTransform(matrix);
                try {
                    DrawGrid(g, size, color, renderOpts);
                }
                finally {
                    g.PopTransform();
                }
            }
        }

        void DrawGrid(IGraphicsTarget g, SizeF size, SymColor color, RenderOptions renderOpts)
        {
            if (lineColor != null && color == lineColor && size.Width > 0 && size.Height > 0) {
                int cxCells = (int)Math.Round(size.Width / cellWidth);
                if (cxCells < 1)
                    cxCells = 1;
                int cyCells = (int)Math.Round(size.Height / cellHeight);
                if (cyCells < 1)
                    cyCells = 1;

                float width = size.Width / cxCells;
                float height = size.Height / cyCells;

                DrawGridLines(g, size, cxCells, cyCells, width, height);
                if ((gridFlags & 2) != 0)
                    DrawGridText(g, size, cxCells, cyCells, width, height);
            }
        }

        private void DrawGridLines(IGraphicsTarget g, SizeF size, int cxCells, int cyCells, float width, float height)
        {
            for (int x = 1; x < cxCells; ++x) {
                PointF pt1 = new PointF(x * width, 0);
                PointF pt2 = new PointF(x * width, size.Height);
                g.DrawLine(gridPen, pt1, pt2);
            }

            for (int y = 1; y < cyCells; ++y) {
                PointF pt1 = new PointF(0, y * height);
                PointF pt2 = new PointF(size.Width, y * height);
                g.DrawLine(gridPen, pt1, pt2);
            }
        }

        private void DrawGridText(IGraphicsTarget g, SizeF size, int cxCells, int cyCells, float width, float height)
        {
            for (int y = 0; y < cyCells; ++y)
                for (int x = 0; x < cxCells; ++x) {
                    int cellNum;
                    string cellText;

                    if ((gridFlags & 4) != 0) // number from bottom
                        cellNum = y * cxCells + x + 1;
                    else
                        cellNum = (cyCells - 1 - y) * cxCells + x + 1;

                    if (cellNum > cxCells * cyCells - unnumberedCells)
                        cellText = unnumberedText;
                    else
                        cellText = cellNum.ToString();

                    PointF pt = new PointF(x * width, (y + 1) * height);
                    pt.Y -= textSize * 0.17F;
                    pt.X += textSize * 0.28F;

                    Matrix flipMatrix = new Matrix();
                    flipMatrix.Translate(pt.X, pt.Y);
                    flipMatrix.Scale(1, -1); // Reverse Y direction so text is correct way around.
                    g.PushTransform(flipMatrix);

                    g.DrawText(cellText, font, brush, new PointF());

                    g.PopTransform();
                }
        }

        // Get the path of the outer rectangle (including corners)
        internal SymPath GetPath(PointF location, SizeF size, float rotation)
        {
            SymPath path = SymPath.CreateRoundedRectangle(new RectangleF(location, size), cornerRadius);
            if (rotation != 0) {
                Matrix mat = new Matrix();
                mat.RotateAt(rotation, location);
                path = path.Transform(mat);
            }

            return path;
        }

        // Calculate the bounding box
        internal RectangleF CalcBounds(PointF location, SizeF size, float rotation)
        {
            RectangleF box = new RectangleF(location, size);
            box.Inflate(thickness / 2, thickness / 2);
            if (rotation != 0)
                box = Geometry.BoundsOfRotatedRectangle(box, location, rotation);

            return box;
        }

        public override void FreeGdiObjects()
        {
            lineColor.FreeGdiObjects();
        }
    }


    // Enum for the different kinds of line
    public enum LineStyle { 
        Rounded,                 // round ends, corners
        Mitered,                   // flat ends, mitered corners
        Beveled,                   // flat ends, bevel corners
        FlatRounded         // flat ends, round corners -- not used directly for symbols, but for parts of symbols.
    };
}

