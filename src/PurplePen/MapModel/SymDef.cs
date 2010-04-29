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
using System.IO;
using SysDraw = System.Drawing;
#if WPF
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using Matrix = System.Drawing.Drawing2D.Matrix;
using LineJoin = System.Drawing.Drawing2D.LineJoin;
using LineCap = System.Drawing.Drawing2D.LineCap;
#endif

#if WPF
using System.Windows;
using System.Windows.Media;
using System.Runtime.InteropServices;
#else
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
#endif

namespace PurplePen.MapModel
{
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

        protected SymDef(string name, int ocadID)
        {
            this.name = name; this.ocadID = ocadID;
        }

        // The containing map
        protected Map map;
        public Map ContainingMap { get { return map; } }

#if !WPF
        private Image toolboxImage;
        public Image ToolboxImage
        {
            get
            {
                if (toolboxImage == null) {
                    Bitmap bitmap = new Bitmap(24, 24);
                    using (Graphics g = Graphics.FromImage(bitmap))
                        g.Clear(Color.Transparent);
                    toolboxImage = bitmap;
                }

                return toolboxImage;
            }
            set { CheckModifiable(); toolboxImage = value; }
        }
#endif

        private int ocadID;
        public int OcadID { get { return ocadID; } }

        public virtual void SetMap(Map newMap)
        {
            Debug.Assert(newMap != null);

            if (map != null && map != newMap)
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
    }

    public class PointSymDef: SymDef
    {
        Glyph glyph;

        public Glyph Glyph { get { return glyph; } }

        bool allowRotation;  // Should this glyph rotate when the feature/map is rotates, or always remain in the same orientation.
        public bool AllowRotation { get { return allowRotation; } }

        public float Radius { get { return glyph.Radius; } }

        public PointSymDef(string name, int ocadID, Glyph glyph, bool allowRotation)
            : base(name, ocadID)
        {
            if (glyph == null)
                throw new ArgumentNullException("glyph");
            this.glyph = glyph;
            this.allowRotation = allowRotation;
        }

        public override void SetMap(Map newMap)
        {
            base.SetMap(newMap);
            glyph.CheckColors(newMap);
        }

        // Does this symbol definition draw the given color. Used to determine
        // if this symbol definition draws into the current layer being draw.
        public override bool HasColor(SymColor color)
        {
            if (color == null)
                return false;
            return glyph.HasColor(color);
        }

        // Draw this point symbol at point pt with angle ang in this graphics (given color only).
        internal void Draw(GraphicsTarget g, PointF pt, float angle, float[] gaps, SymColor color, RenderOptions renderOpts)
        {
            glyph.Draw(g, pt, angle, null, gaps, color, renderOpts);
        }

        // Calculate the bounding box
        internal RectangleF CalcBounds(PointF pt, float angle)
        {
            float radius = glyph.Radius;
            return new RectangleF(pt.X - radius, pt.Y - radius, radius * 2, radius * 2);
        }

        public override void FreeGdiObjects()
        {
            glyph.FreeGdiObjects();
        }

    }

    public class LineSymDef: SymDef
    {
        // describes dash information
        public struct DashInfo
        {
            public float dashLength;      // length of the dashes (solid part)
            public float firstDashLength; // length of the first dash 
            public float lastDashLength;  // length of the last dash
            public float gapLength;       // length of the gaps (undrawn part)
            public int minGaps;           // minimum number of GAPS in the line
            public int secondaryMiddleGaps;     // number of secondary gaps in the main dash (0 for none)
            public float secondaryMiddleLength; // length of the secondary gaps
            public int secondaryEndGaps;     // number of secondary gaps in the first and last dashes (0 for none)
            public float secondaryEndLength; // length of the first and last secondary gaps
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
            DashCenters,           // at the center of the main dashes in the dash information
            MiddleDashCenters,     // at the center of the main dashes, not counting first and last dash
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
        }

        const float CORNER_GLYPH_STRETCH_LIMIT = 3.0F;

        SymColor lineColor;
        float thickness;
        LineStyle lineStyle;

        SymColor secondLineColor;
        float secondThickness;
        LineStyle secondLineStyle;

        DashInfo dashInfo;
        bool isDashed;

        bool isDoubleLine;
        DoubleLineInfo doubleLines;

        ShortenInfo shortenInfo;

        GlyphInfo[] glyphs;

        bool pensCreated = false;
        IGraphicsPen mainPen;
        IGraphicsPen secondPen;
        IGraphicsBrush pointyEndsBrush;
        IGraphicsPen doubleFillPen;
        IGraphicsPen doubleLeftPen;
        IGraphicsPen doubleRightPen;

        float maxThickness, maxMiteredThickness;

        public SymColor LineColor { get { return lineColor; } }
        public float LineThickness { get { return thickness; } }
        public LineStyle MainLineStyle { get { return lineStyle; } }
        public bool HasSecondLine { get { return secondLineColor != null && secondThickness > 0; } }
        public SymColor SecondLineColor { get { return secondLineColor; } }
        public float SecondThickness { get { return secondThickness; } }
        public LineStyle SecondLineStyle { get { return secondLineStyle; } }
        public bool IsDashed { get { return isDashed; } }
        public DashInfo Dashes { get { return dashInfo; } }
        public bool IsDoubleLine { get { return isDoubleLine; } }
        public DoubleLineInfo DoubleLines { get { return doubleLines; } }
        public GlyphInfo[] Glyphs { get { return (glyphs == null) ? null : (GlyphInfo[]) glyphs.Clone(); } }
        public float MaxThickness { get { return maxThickness; } }
        public ShortenInfo Shortening { get { return shortenInfo; } }
        public bool IsShortened { get { return (shortenInfo.shortenBeginning > 0 || shortenInfo.shortenEnd > 0); } }

        public override void SetMap(Map newMap)
        {
            base.SetMap(newMap);
            CheckColor(lineColor);
            CheckColor(secondLineColor);
            CheckColor(doubleLines.doubleFillColor);
            CheckColor(doubleLines.doubleLeftColor);
            CheckColor(doubleLines.doubleRightColor);

            // compute the max thickness of this line.
            maxThickness = 0.0F;
            if (lineColor != null && thickness > maxThickness)
                maxThickness = thickness;
            if (lineColor != null && lineStyle == LineStyle.Mitered && thickness > maxMiteredThickness)
                maxMiteredThickness = thickness;

            if (secondLineColor != null && secondThickness > maxThickness)
                maxThickness = secondThickness;
            if (secondLineColor != null && secondLineStyle == LineStyle.Mitered && secondThickness > maxMiteredThickness)
                maxMiteredThickness = secondThickness;

            if (isDoubleLine) {
                float doubleThickness = (2 * Math.Max(doubleLines.doubleLeftWidth, doubleLines.doubleRightWidth) + doubleLines.doubleThick);
                if (doubleThickness > maxThickness)
                    maxThickness = doubleThickness;
                if (doubleThickness > maxMiteredThickness)
                    maxMiteredThickness = doubleThickness;
            }

            if (glyphs != null) {
                foreach (GlyphInfo glyphInfo in glyphs) {
                    float glyphRadius = glyphInfo.glyph.Radius;

                    if (glyphRadius * 2 > maxThickness)
                        maxThickness = glyphRadius * 2;
                    if (glyphInfo.location == GlyphLocation.Corners && glyphRadius * 2 > maxMiteredThickness)
                        maxMiteredThickness = glyphRadius * 2;
                }
            }
        }

        public LineSymDef(string name, int ocadID, SymColor color, float thick, LineStyle lineStyle)
            : base(name, ocadID)
        {
            lineColor = color;
            thickness = thick;
            this.lineStyle = lineStyle;
        }

        public void SetSecondLine(SymColor secondLineColor, float secondThickness, LineStyle secondLineStyle)
        {
            CheckModifiable();
            this.secondLineColor = secondLineColor;
            this.secondThickness = secondThickness;
            this.secondLineStyle = secondLineStyle;
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

        void CreatePens(GraphicsTarget g)
        {
            Debug.Assert(!pensCreated && mainPen == null);

            if (thickness > 0.0F && lineColor != null) {

                if (shortenInfo.shortenBeginning > 0 && shortenInfo.pointyEnds) {
                    // Always use round line cap with pointy ends.
                    if (lineStyle == LineStyle.Rounded || lineStyle == LineStyle.FlatRounded) {
                        mainPen = g.CreatePen(lineColor.ColorValue, thickness, LineCap.Round, LineJoin.Round, GraphicsUtil.MITER_LIMIT);
                    }
                    else if (lineStyle == LineStyle.Beveled) {
                        mainPen = g.CreatePen(lineColor.ColorValue, thickness, LineCap.Round, LineJoin.Bevel, GraphicsUtil.MITER_LIMIT);
                    }
                    else if (lineStyle == LineStyle.Mitered) {
                        mainPen = g.CreatePen(lineColor.ColorValue, thickness, LineCap.Round, LineJoin.Miter, GraphicsUtil.MITER_LIMIT);
                    }
                }
                else {
                    mainPen = GraphicsUtil.CreateSolidPen(g, lineColor.ColorValue, thickness, lineStyle);
                }

                if (shortenInfo.pointyEnds)
                    pointyEndsBrush = g.CreateSolidBrush(lineColor.ColorValue);
            }

            if (secondLineColor != null && secondThickness > 0) {
                secondPen = GraphicsUtil.CreateSolidPen(g, secondLineColor.ColorValue, secondThickness, secondLineStyle);
            }

            if (isDoubleLine) {
                if (doubleLines.doubleFillColor != null) {
                    doubleFillPen = GraphicsUtil.CreateSolidPen(g, doubleLines.doubleFillColor.ColorValue, doubleLines.doubleThick, LineStyle.Mitered);
                }
                if (doubleLines.doubleLeftWidth > 0.0F) {
                    doubleLeftPen = GraphicsUtil.CreateSolidPen(g, doubleLines.doubleLeftColor.ColorValue, doubleLines.doubleLeftWidth, LineStyle.FlatRounded);
                }
                if (doubleLines.doubleRightWidth > 0.0F) {
                    doubleRightPen = GraphicsUtil.CreateSolidPen(g, doubleLines.doubleRightColor.ColorValue, doubleLines.doubleRightWidth, LineStyle.FlatRounded);
                }
            }

            pensCreated = true;
        }

        public override void FreeGdiObjects()
        {
            if (mainPen != null) {
                mainPen.Dispose();
                mainPen = null;
            }
            if (pointyEndsBrush != null) {
                pointyEndsBrush.Dispose();
                pointyEndsBrush = null;
            }
            if (secondPen != null) {
                secondPen.Dispose();
                secondPen = null;
            }
            if (doubleFillPen != null) {
                doubleFillPen.Dispose();
                doubleFillPen = null;
            }
            if (doubleLeftPen != null) {
                doubleLeftPen.Dispose();
                doubleLeftPen = null;
            }
            if (doubleRightPen != null) {
                doubleRightPen.Dispose();
                doubleRightPen = null;
            }

            if (glyphs != null) {
                foreach (GlyphInfo glyphInfo in glyphs) {
                    if (glyphInfo.glyph != null)
                        glyphInfo.glyph.FreeGdiObjects();
                }
            }

            pensCreated = false;
        }


        // Does this symbol definition draw the given color. Used to determine
        // if this symbol definition draws into the current layer being draw.
        public override bool HasColor(SymColor color)
        {
            if (color == null)
                return false;

            if ((color == lineColor && thickness > 0.0F) ||
                (color == secondLineColor && secondThickness > 0.0F) ||
                (isDoubleLine && doubleLines.doubleFillColor == color) ||
                (isDoubleLine && doubleLines.doubleLeftWidth > 0.0F && doubleLines.doubleLeftColor == color) ||
                (isDoubleLine && doubleLines.doubleRightWidth > 0.0F && doubleLines.doubleRightColor == color)) {
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

        // Draw this line symbol in the graphics along the path provided, with
        // the given color only.
        internal void Draw(GraphicsTarget g, SymPath path, SymColor color, RenderOptions renderOpts)
        {
            Debug.Assert(map != null);

            if (path.Length == 0)
                return;             // Don't draw anything for a zero-length path.

            if (!pensCreated)
                CreatePens(g);

            SymPath mainPath = path;  // the path for the main part of the line (might be shortened).
            if (shortenInfo.shortenBeginning > 0.0F || shortenInfo.shortenEnd > 0.0F) {
                mainPath = path.ShortenBizzarro(shortenInfo.shortenBeginning, shortenInfo.shortenEnd);
                // NOTE: mainPath can be NULL below here!!!
            }

            if (color == lineColor && thickness > 0.0F && mainPath != null) {
                if (!isDashed) {
                    // simple drawing.
                    mainPath.Draw(g, mainPen);
                }
                else {
                    // Draw the dashed line.
                    DrawDashed(g, mainPath, mainPen, dashInfo, renderOpts);
                }
            }

            // Draw the pointy ends of the line. If mainPath is null, this is all the line!
            if (color == lineColor && shortenInfo.pointyEnds && thickness > 0.0F && (shortenInfo.shortenBeginning > 0.0F || shortenInfo.shortenEnd > 0.0F))
                DrawPointyEnds(g, path, shortenInfo.shortenBeginning, shortenInfo.shortenEnd, thickness);

            if (color == secondLineColor && secondThickness > 0.0F && path != null) {
                // note that shortened path not used for secondary line, the full length path is.
                path.Draw(g, secondPen);
            }

            // Double lines don't use the shortened path, but the full-length path.
            if (isDoubleLine) {
                if (doubleLines.doubleFillColor == color) {
                    if (doubleLines.doubleFillDashed)
                        DrawDashed(g, path, doubleFillPen, doubleLines.doubleDashes, renderOpts);
                    else
                        path.Draw(g, doubleFillPen);
                }

                if (doubleLines.doubleLeftColor == color && doubleLines.doubleLeftWidth > 0.0F) {
                    foreach (SymPath subpath in path.GetSubpaths(SymPath.DOUBLE_LEFT_STARTSTOPFLAG)) {
                        float offsetRight = -(doubleLines.doubleThick + doubleLines.doubleLeftWidth) / 2F;
                        if (doubleLines.doubleLeftDashed) {
                            DrawDashedWithOffset(g, subpath, doubleLeftPen, doubleLines.doubleDashes, offsetRight, GraphicsUtil.MITER_LIMIT, renderOpts);
                        }
                        else {  
                            SymPath leftPath = subpath.OffsetRight(offsetRight, GraphicsUtil.MITER_LIMIT);
                            leftPath.Draw(g, doubleLeftPen);
                        }
                    }
                }

                if (doubleLines.doubleRightColor == color && doubleLines.doubleRightWidth > 0.0F) {
                    foreach (SymPath subpath in path.GetSubpaths(SymPath.DOUBLE_RIGHT_STARTSTOPFLAG)) {
                        float offsetRight = (doubleLines.doubleThick + doubleLines.doubleRightWidth) / 2F;
                        if (doubleLines.doubleRightDashed) {
                            DrawDashedWithOffset(g, subpath, doubleRightPen, doubleLines.doubleDashes, offsetRight, GraphicsUtil.MITER_LIMIT, renderOpts);
                        }
                        else {  
                            SymPath rightPath = subpath.OffsetRight(offsetRight, GraphicsUtil.MITER_LIMIT);
                            rightPath.Draw(g, doubleRightPen);
                        }
                    }
                }
            }

            if (glyphs != null && mainPath != null) {
                foreach (GlyphInfo glyphInfo in glyphs) {
                    if (glyphInfo.glyph.HasColor(color))
                        DrawGlyphs(g, glyphInfo, mainPath, path, color, renderOpts);
                }
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
            MiddleDashCenters,         // gives locations of dash centers, but doesn't include the first and last
            GapCenters,                     // gives locations of the gap centers
            GapCentersOffset,   // gives locations of gap centers, but offset forward by "offset", and missing the last 
            GapCentersDecrease,    // gives locations of the gap centers, but with decreasing distances based on final decrease value and a decrease toward both ends boolean
        }

        private static float[] ComputeDashDistances(SymPath path, LocationKind kind, float dashLength, float firstDashLength, float lastDashLength, float gapLength, int minGaps, float offset, 
                                                                               int numEndSecGaps, float lengthEndSecGaps, int numMiddleSecGaps, float lengthMiddleSecGaps, float decreaseLimit, bool decreaseBothEnds)
        {
            // Get length of each segment, deliniated by corner points.
            PointKind[] pointkinds;
            float[] segmentLengths = path.GetCornerAndDashPointDistancesBizzarro(out pointkinds);
            float[] lengthAtEnd = new float[segmentLengths.Length];
            float[][] dashDistances = new float[segmentLengths.Length][];

            // Compute dash distances for each segment of the path.
            int totalDistances = 0;
            for (int i = 0; i < dashDistances.Length; ++i) {
                // Compute first and last lengths of the segment in question.
                float firstLength, lastLength;
                if (i != 0 && pointkinds[i] == PointKind.Dash)
                    firstLength = dashLength / 2;
                else
                    firstLength = firstDashLength;

                if (i < dashDistances.Length - 1 && pointkinds[i + 1] == PointKind.Dash)
                    lastLength = dashLength / 2;
                else
                    lastLength = lastDashLength;

                // Note that minGaps is only required on the LAST segment. This is because that the minGaps requirement is for
                // all segments together, not each segment.
                int numGaps;
                dashDistances[i] = ComputeDashDistances(segmentLengths[i], kind, dashLength, firstLength, lastLength, gapLength,
                                                        (i == dashDistances.Length - 1) ? minGaps : 0, offset, numEndSecGaps, lengthEndSecGaps, numMiddleSecGaps, lengthMiddleSecGaps, decreaseLimit, decreaseBothEnds, out lengthAtEnd[i], out numGaps);
                minGaps = Math.Max(minGaps - numGaps, 0);
                totalDistances += dashDistances[i].Length;

                // The last dash and first dash of adjacent parts merge together.
                if (kind == LocationKind.DashAndGapLengths && i > 0)
                    --totalDistances;
            }

            if (dashDistances.Length == 1) {
                return dashDistances[0];
            }

            // Combine the distances from each segment into a single array. For dash and gap lengths, combine the 
            // dash lengths around segment boundaries.
            float[] distances = new float[totalDistances];

            int index = 0;
            for (int i = 0; i < dashDistances.Length; ++i) {
                Array.Copy(dashDistances[i], 0, distances, index, dashDistances[i].Length);
                if (i > 0 && dashDistances[i].Length > 0) {
                    distances[index] += lengthAtEnd[i - 1];
                }
                if (i > 0 && (dashDistances[i].Length == 0 || (kind == LocationKind.DashAndGapLengths && dashDistances[i].Length == 1)))
                    lengthAtEnd[i] += lengthAtEnd[i - 1];

                index += dashDistances[i].Length;
                if (kind == LocationKind.DashAndGapLengths)
                    --index;
            }

            if (kind == LocationKind.DashAndGapLengths)
                distances = RemoveZeroGaps(distances);

            return distances;
        }

        // Computes the dash distances for a single segment. Also returns the distance from the last location to the end of the segment.
        // If LocationKind is DashesAndGapLengths, this is a duplicate of the last array element.
        private static float[] ComputeDashDistances(float pathLength, LocationKind kind, float dashLength, float firstDashLength, float lastDashLength, float gapLength, int minGaps, float offset, 
                                                                            int numEndSecGaps, float lengthEndSecGaps, int numMiddleSecGaps, float lengthMiddleSecGaps, float decreaseLimit, bool decreaseBothEnds, 
                                                                            out float lengthAtEnd, out int actualGaps)
        {
            int numGaps;		         // actual number of gaps in the line
            float actualDashLength;      // actual length of each dash
            float actualFirstDashLength; // actual length of first dash
            float actualLastDashLength;  // actual last dash length.

            if (kind == LocationKind.GapCentersDecrease) {
                // Computer number of dashes based on average dash length.
                dashLength = (dashLength + (dashLength * decreaseLimit)) / 2;
            }

            // Computer the number of gaps
            // The number of gaps is adjusted so that the gap lengths are always preserved exactly, and the dash lengths are as close as possible to the actual dash lengths.
            if (pathLength - firstDashLength - lastDashLength <= gapLength)
                numGaps = (minGaps >= 1 || pathLength >= firstDashLength / 2 + lastDashLength / 2 + gapLength) ? 1 : 0;
            else
                numGaps = (int) Math.Round((pathLength + dashLength - firstDashLength - lastDashLength) / (dashLength + gapLength));

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
                    else {
                        lengthAtEnd = pathLength / 2;
                        return new float[1] { pathLength / 2 }; // in middle of single gap
                    }
                }
                else if (kind == LocationKind.DashCenters || kind == LocationKind.MiddleDashCenters|| kind == LocationKind.GapCentersOffset) {
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
            else if (kind == LocationKind.MiddleDashCenters) {
                if (numGaps >= 2) {
                    locations = new float[numGaps - 1];

                    locations[index++] = actualFirstDashLength + gapLength + actualDashLength / 2;
                    lengthAtEnd = actualLastDashLength + gapLength + actualDashLength / 2;
                    for (int i = 1; i < numGaps - 1; ++i)
                        locations[index++] = actualDashLength + gapLength;
                }
                else {
                    locations = new float[0];
                    lengthAtEnd = pathLength;
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
                        if (decreaseBothEnds) {
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
                        else {
                            float currentDashLength = 2 * actualDashLength / (1 + decreaseLimit);
                            float dashDelta = -(currentDashLength - decreaseLimit * currentDashLength) / ((numGaps-1) * 2);

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
                locations = new float[numGaps - 1];

                if (numGaps <= 1)
                    lengthAtEnd = pathLength;
                else {
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

        private static void DrawDashed(GraphicsTarget g, SymPath path, IGraphicsPen pen, DashInfo dashes, RenderOptions renderOpts)
        {
            DrawDashedWithOffset(g, path, pen, dashes, 0, 1, renderOpts);
        }

        private static void DrawDashedWithOffset(GraphicsTarget g, SymPath path, IGraphicsPen pen, DashInfo dashes, float offsetRight, float miterLimit, RenderOptions renderOpts)
        {
            float[] distances;

            distances = ComputeDashDistances(path, LocationKind.DashAndGapLengths, dashes.dashLength, dashes.firstDashLength, dashes.lastDashLength, dashes.gapLength, dashes.minGaps, 0, dashes.secondaryEndGaps, dashes.secondaryEndLength, dashes.secondaryMiddleGaps, dashes.secondaryMiddleLength, 1.0F, false);

            if (distances.Length == 0 || (dashes.gapLength < renderOpts.minResolution && (dashes.secondaryMiddleGaps == 0 || dashes.secondaryMiddleLength < renderOpts.minResolution) && (dashes.secondaryEndGaps == 0 || dashes.secondaryEndLength < renderOpts.minResolution))) {
                // No dashes, or the dashes are too small to be visible. Draw solid.
                if (offsetRight != 0) {
                    SymPath offsetPath = path.OffsetRight(offsetRight, miterLimit);
                    offsetPath.Draw(g, pen);
                }
                else
                    path.Draw(g, pen);
            }
            else {
                path.DrawDashedOffsetBizzarro(g, pen, distances, 0, offsetRight, miterLimit);
            }
        }

        // Draw the glyphs along the path. "longPath" is the same as path unless shortening of the ends has occurred, in which case
        // path is the shortened path (used for all glyphs except start and end), and longPath is used for the start and end.
        private void DrawGlyphs(GraphicsTarget g, GlyphInfo glyphInfo, SymPath path, SymPath longPath, SymColor color, RenderOptions renderOpts)
        {
            float[] distances;
            PointF[] points;
            float[] perpAngles, subtendedAngles;
            float firstDistance;

            // Figure out the distances of the glyphs along the line.
            switch (glyphInfo.location) {
            case GlyphLocation.Corners:
                // Corner points are done somewhat differently. Only can have 1 symbol.
                // There is an interesting feature in OCAD where the dimensions of corner glyphs are stretched a certain amount at
                // very acute angles. This is so that power line crossbars always extend beyond the power lines themselves.
                // This is handled by stretching the glyph based on the subtended angle at the corner.
                points = path.FindCornerPoints(out perpAngles, out subtendedAngles);
                if (points != null) {
                    for (int i = 0; i < points.Length; ++i) {
                        float subtendedAngle = subtendedAngles[i];
                        float stretch;
                        if (subtendedAngle != 0)
                            stretch = Util.MiterFactor(subtendedAngle);   
                        else
                            stretch = 1.0F;
                        stretch = Math.Min(stretch, CORNER_GLYPH_STRETCH_LIMIT);

                        Matrix stretchMatrix = new Matrix();
                        stretchMatrix.Scale(1.0F, stretch);

                        glyphInfo.glyph.Draw(g, points[i], perpAngles[i] + 90.0F, stretchMatrix, null, color, renderOpts);
                    }
                }
                return;

            case GlyphLocation.Spaced:
                distances = ComputeDashDistances(path, LocationKind.GapCenters, glyphInfo.distance, glyphInfo.firstDistance, glyphInfo.lastDistance, 0, glyphInfo.minimum, 0, 0, 0, 0, 0, 1.0F, false);
                break;
            case GlyphLocation.SpacedOffset:
                distances = ComputeDashDistances(path, LocationKind.GapCentersOffset, glyphInfo.distance, glyphInfo.firstDistance, glyphInfo.lastDistance, 0, glyphInfo.minimum, glyphInfo.offset, 0, 0, 0, 0, 1.0F, false);
                break;
            case GlyphLocation.SpacedDecrease:
                distances = ComputeDashDistances(path, LocationKind.GapCentersDecrease, glyphInfo.distance, glyphInfo.firstDistance, glyphInfo.lastDistance, 0, glyphInfo.minimum, 0, 0, 0, 0, 0, glyphInfo.decreaseLimit, glyphInfo.decreaseBothEnds);

                if (distances != null && distances.Length > 0) {
                    firstDistance = distances[0];

                    for (int n = 0; n < glyphInfo.number; ++n) {
                        distances[0] = Math.Max(0.0F, firstDistance - ((glyphInfo.number - 1 - n * 2) * (glyphInfo.spacing / 2.0F)));

                        points = path.FindPointsAlongLineBizzarro(distances, out perpAngles);

                        for (int i = 0; i < points.Length; ++i) {
                            float decreaseFactor;
                            if (glyphInfo.decreaseBothEnds) {
                                if (points.Length <= 2)
                                    decreaseFactor = glyphInfo.decreaseLimit;
                                else
                                    decreaseFactor = 1.0F - (Math.Abs(i - ((points.Length-1) / 2F)) * (1 - glyphInfo.decreaseLimit) / ((points.Length-1) / 2F));
                            }
                            else {
                                if (i == 0)
                                    decreaseFactor = 1.0F;
                                else
                                    decreaseFactor = 1.0F - (i * (1 - glyphInfo.decreaseLimit) / (points.Length - 1));
                            }
                            Matrix matrixTransform = new Matrix();
                            matrixTransform.Scale(decreaseFactor, decreaseFactor);
                            glyphInfo.glyph.Draw(g, points[i], perpAngles[i], matrixTransform, null, color, renderOpts);
                        }
                    }
                }
                
                return;
            case GlyphLocation.DashCenters:
                distances = ComputeDashDistances(path, LocationKind.DashCenters, dashInfo.dashLength, dashInfo.firstDashLength, dashInfo.lastDashLength, dashInfo.gapLength, dashInfo.minGaps, 0, 0, 0, 0, 0, 1.0F, false);
                break;
            case GlyphLocation.MiddleDashCenters:
                distances = ComputeDashDistances(path, LocationKind.MiddleDashCenters, dashInfo.dashLength, dashInfo.firstDashLength, dashInfo.lastDashLength, dashInfo.gapLength, dashInfo.minGaps, 0, 0, 0, 0, 0, 1.0F, false);
                break;
            case GlyphLocation.GapCenters:
                // OCAD doesn't respect the "0 minimum gaps" for the symbols, although it does for the gaps. Always have at least one symbol. This is handled on import by having glyphInfo.minimum be 1.
                distances = ComputeDashDistances(path, LocationKind.GapCenters, dashInfo.dashLength, dashInfo.firstDashLength, dashInfo.lastDashLength, dashInfo.gapLength, Math.Max(glyphInfo.minimum, dashInfo.minGaps), 0, 0, 0, 0, 0, 1.0F, false);
                break;
            case GlyphLocation.Start:
                distances = new float[1] { 0 };
                break;
            case GlyphLocation.End:
                distances = new float[1] { longPath.BizzarroLength };
                break;
            default:
                Debug.Fail("bad glyph location");
                return;
            }

            if (distances == null || distances.Length == 0)
                return;
            firstDistance = distances[0];

            for (int n = 0; n < glyphInfo.number; ++n) {
                distances[0] = Math.Max(0.0F, firstDistance - ((glyphInfo.number - 1 - n * 2) * (glyphInfo.spacing / 2.0F)));

                if (glyphInfo.location == GlyphLocation.Start || glyphInfo.location == GlyphLocation.End)
                    points = longPath.FindPointsAlongLineBizzarro(distances, out perpAngles);
                else
                    points = path.FindPointsAlongLineBizzarro(distances, out perpAngles);

                for (int i = 0; i < points.Length; ++i) {
                    glyphInfo.glyph.Draw(g, points[i], perpAngles[i], null, null, color, renderOpts);
                }
            }
        }

        // Draw the pointy ends on a line.
        void DrawPointyEnds(GraphicsTarget g, SymPath longPath, float pointyLengthStart, float pointyLengthEnd, float lineWidth)
        {
            // Get locations of points at the tip, half-way, and base of the pointy tips.
            float length = longPath.BizzarroLength;
            float[] distances, angles;
            if (length >= pointyLengthStart + pointyLengthEnd) {
                distances = new float[6] { 0, pointyLengthStart / 2, pointyLengthStart / 2, length - pointyLengthEnd - pointyLengthStart, pointyLengthEnd / 2, pointyLengthEnd / 2 };
            }
            else {
                float scaleFactor = length / (pointyLengthStart + pointyLengthEnd);
                distances = new float[6] { 0, (pointyLengthStart / 2) * scaleFactor, (pointyLengthStart / 2) * scaleFactor, 0, (pointyLengthEnd / 2) * scaleFactor, (pointyLengthEnd / 2) * scaleFactor };
            }
            PointF[] pointsAlongPath = longPath.FindPointsAlongLineBizzarro(distances, out angles);

            // Each pointy tip is composed of a polygon of 5 points.
            PointF[] tipCorners = new PointF[5];
            float midpointWidth = lineWidth * 0.666F;  // Makes a sort of curvy tip.

            if (pointyLengthStart > 0) {
                // Draw point at beginning.
                tipCorners[0] = pointsAlongPath[0];
                tipCorners[1] = Util.MoveDistance(pointsAlongPath[1], midpointWidth / 2, angles[1] - 90.0F);
                tipCorners[4] = Util.MoveDistance(pointsAlongPath[1], midpointWidth / 2, angles[1] + 90.0F);
                tipCorners[2] = Util.MoveDistance(pointsAlongPath[2], lineWidth / 2, angles[2] - 90.0F);
                tipCorners[3] = Util.MoveDistance(pointsAlongPath[2], lineWidth / 2, angles[2] + 90.0F);
                g.FillPolygon(pointyEndsBrush, tipCorners, SysDraw.Drawing2D.FillMode.Winding);
            }

            if (pointyLengthEnd > 0) {
                // Draw point at end.
                tipCorners[0] = pointsAlongPath[5];
                tipCorners[1] = Util.MoveDistance(pointsAlongPath[4], midpointWidth / 2, angles[4] - 90.0F);
                tipCorners[4] = Util.MoveDistance(pointsAlongPath[4], midpointWidth / 2, angles[4] + 90.0F);
                tipCorners[2] = Util.MoveDistance(pointsAlongPath[3], lineWidth / 2, angles[3] - 90.0F);
                tipCorners[3] = Util.MoveDistance(pointsAlongPath[3], lineWidth / 2, angles[3] + 90.0F);
                g.FillPolygon(pointyEndsBrush, tipCorners, SysDraw.Drawing2D.FillMode.Winding);
            }
        }

        // Calculate the bounding box
        internal RectangleF CalcBounds(SymPath path)
        {
            float thickness = maxThickness;

            // If the path has sharp mitered corners, the thickness is increased.
            if (maxMiteredThickness > 0) {
                float miterThickness = Math.Min(path.MaxMiter, GraphicsUtil.MITER_LIMIT) * maxMiteredThickness;
                if (miterThickness > thickness)
                    thickness = miterThickness;
            }

            RectangleF box = path.BoundingBox;
            box.Inflate(thickness / 2, thickness / 2);
            return box;
        }
    }

    public class AreaSymDef: SymDef
    {
        // Solid fill information
        SymColor fillColor;  // color to fill with, null to not fill.

        // Border information
        LineSymDef borderSymdef;          // if non-null, the border symbol to use.

        // Hatching information.
        int hatchMode;       // 0 for no hatch, 1 for single hatch, 2 for double hatch.
        SymColor hatchColor; // color for hatching
        float hatchWidth;    // width of hatch lines
        float hatchSpacing;  // spacing of hatch lines
        float hatchAngle1;   // angle of hatching
        float hatchAngle2;   // angle of 2nd hatching (for double hatching only)
        IGraphicsPen hatchPen;        // pen for hatching.

        // Pattern information
        bool drawPattern;    // are we drawing a pattern?
        bool offsetRows;     // offset alternate rows by 1/2
        float patternWidth;
        float patternHeight; // size of pattern element
        float patternAngle;  // angle of pattern
        Glyph patternGlyph;  // pattern element

        // Cached brushes for faster pattern drawing.
        float pixelSizeCached;                          // pixel size in mm that the patternBrushes are created for. (WPF brushes are resolution independent).
        Dictionary<SymColor, IGraphicsBrush> patternBrushes; // pattern brushes, indexed by color

        bool pensAndBrushesCreated; // are pens and brushed created.

        public override void SetMap(Map newMap)
        {
            base.SetMap(newMap);
            CheckColor(fillColor);
            if (hatchMode != 0)
                CheckColor(hatchColor);
            if (drawPattern)
                patternGlyph.CheckColors(map);
        }

        public AreaSymDef(string name, int ocadID, SymColor color, LineSymDef borderSymdef)
            : base(name, ocadID)
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

        public void SetHatching(int hatchMode, SymColor hatchColor, float hatchWidth, float hatchSpacing, float angle1, float angle2)
        {
            CheckModifiable();
            if (hatchMode < 0 || hatchMode > 2)
                throw new ArgumentOutOfRangeException("hatchMode", "hatching mode must be 0, 1, 2");

            this.hatchMode = hatchMode;
            this.hatchColor = hatchColor;
            this.hatchWidth = hatchWidth;
            this.hatchSpacing = hatchSpacing;
            this.hatchAngle1 = angle1;
            this.hatchAngle2 = angle2;
        }

        public void GetHatching(out int hatchMode, out SymColor hatchColor, out float hatchWidth, out float hatchSpacing, out float angle1, out float angle2)
        {
            hatchMode = this.hatchMode;
            hatchColor = this.hatchColor;
            hatchWidth = this.hatchWidth;
            hatchSpacing = this.hatchSpacing;
            angle1 = this.hatchAngle1;
            angle2 = this.hatchAngle2;
        }


        public void SetPattern(bool drawPattern, bool offsetRows, float width, float height, float angle, Glyph glyph)
        {
            CheckModifiable();

            this.drawPattern = drawPattern;
            this.offsetRows = offsetRows;
            this.patternWidth = width;
            this.patternHeight = height;
            this.patternAngle = angle;
            this.patternGlyph = glyph;
        }

        public void GetPattern(out bool drawPattern, out bool offsetRows, out float width, out float height, out float angle, out Glyph glyph)
        {
            drawPattern = this.drawPattern;
            offsetRows = this.offsetRows;
            width = this.patternWidth;
            height = this.patternHeight;
            angle = this.patternAngle;
            glyph = this.patternGlyph;
        }


        void CreatePensAndBrushes(GraphicsTarget g)
        {
            Debug.Assert(!pensAndBrushesCreated && hatchPen == null && patternBrushes == null);

            if (hatchMode != 0) {
                hatchPen = g.CreatePen(hatchColor.GetBrush(g), hatchWidth, LineCap.Flat, LineJoin.Miter, GraphicsUtil.MITER_LIMIT);
            }

            pensAndBrushesCreated = true;
        }

        public override void FreeGdiObjects()
        {
            if (hatchPen != null) {
                hatchPen.Dispose();
                hatchPen = null;
            }
            if (patternBrushes != null) {
                foreach (IGraphicsBrush brush in patternBrushes.Values)
                    brush.Dispose();
                patternBrushes.Clear();
                patternBrushes = null;
            }
            if (patternGlyph != null)
                patternGlyph.FreeGdiObjects();

            pensAndBrushesCreated = false;
        }



        // Does this symbol definition draw the given color. Used to determine
        // if this symbol definition draws into the current layer being draw.
        public override bool HasColor(SymColor color)
        {
            if (color == null)
                return false;

            return (color == fillColor) ||
                (hatchMode != 0 && hatchColor == color) ||
                (drawPattern && patternGlyph.HasColor(color)) ||
                (borderSymdef != null && borderSymdef.HasColor(color));
        }

        // Draw this area symbol in the graphics inside/around the path provided, with
        // the given color only.
        internal void Draw(GraphicsTarget g, SymPathWithHoles path, SymColor color, float angle, RenderOptions renderOpts)
        {
            if (!pensAndBrushesCreated)
                CreatePensAndBrushes(g);

            if (color == fillColor) {
                path.Fill(g, color.GetBrush(g));
            }

            if (hatchMode != 0 && hatchColor == color) {
                DrawHatching(g, path, angle, renderOpts);
            }

            if (drawPattern && patternGlyph.HasColor(color)) {
                // Faster to draw the pattern with a texture brush that has a bitmap
                // of the pattern in it. Better quality to do it all with glyph drawing.
                // Choose based on the renderOptions.
#if false
                DrawPatternWithTexBrush(g, path, angle, color, renderOpts);
#else
                if (renderOpts.usePatternBitmaps) {
                    CreatePatternBrush(renderOpts.minResolution, g);
                    DrawPatternWithTexBrush(g, path, angle, color, renderOpts);
                }
                else
                    DrawPattern(g, path, angle, color, renderOpts);
#endif
            }

            // Draw the border. Take into account the subpaths defined by start/stop flags along the paths.
            if (borderSymdef != null && borderSymdef.HasColor(color)) {
                // Draw main part of border.
                foreach (SymPath subpath in path.MainPath.GetSubpaths(SymPath.AREA_BOUNDARY_STARTSTOPFLAG))
                    borderSymdef.Draw(g, subpath, color, renderOpts);

                // Draw the holes.
                if (path.Holes != null)
                    foreach (SymPath hole in path.Holes)
                        foreach (SymPath subpath in hole.GetSubpaths(SymPath.AREA_BOUNDARY_STARTSTOPFLAG))
                            borderSymdef.Draw(g, subpath, color, renderOpts);
            }
        }

        // Draw the hatching into the interior of the SymPath.
        void DrawHatching(GraphicsTarget g, SymPathWithHoles path, float angle, RenderOptions renderOpts)
        {
            // Set the clipping region to draw only inside the area.
            g.PushClip(path.GetIGraphicsPath(g));

            // use a transform to rotate and then draw hatching.
            Matrix matrix = new Matrix();
            matrix.RotateAt(hatchAngle1 + angle, new PointF(0, 0));
            g.PushTransform(matrix);

            try
            {
                // Get the correct bounding rect.
                RectangleF bounding = Util.BoundsOfRotatedRectangle(path.BoundingBox, new PointF(), -(hatchAngle1 + angle));

                DrawHatchLines(g, hatchPen, hatchSpacing, bounding, renderOpts);

                // and again for the second bound of hatching
                if (hatchMode == 2) {
                    // Get the correct bounding rect.
                    bounding = Util.BoundsOfRotatedRectangle(path.BoundingBox, new PointF(), -(hatchAngle2 + angle));

                    matrix = new Matrix();
                    matrix.RotateAt(hatchAngle2 - hatchAngle1, new PointF(0, 0));
                    g.PushTransform(matrix);
                    try
                    {
                        DrawHatchLines(g, hatchPen, hatchSpacing, bounding, renderOpts);
                    }
                    finally
                    {
                        g.PopTransform();
                    }
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
        void DrawHatchLines(GraphicsTarget g, IGraphicsPen pen, float spacing, RectangleF boundingRect, RenderOptions renderOpts)
        {
            double firstLine = Math.Round(boundingRect.Top / spacing) * spacing;
            double lastLine = (Math.Round(boundingRect.Bottom / spacing) + 0.5) * spacing;

            for (double y = firstLine; y <= lastLine; y += spacing) {
                g.DrawLine(pen, new PointF(boundingRect.Left, (float) y), new PointF(boundingRect.Right, (float) y));
            }
        }

        // Draw the pattern using the texture brush.
        void DrawPatternWithTexBrush(GraphicsTarget g, SymPathWithHoles path, float angle, SymColor color, RenderOptions renderOpts)
        {
            IGraphicsBrush brush = patternBrushes[color];
            Debug.Assert(brush != null);

            if (angle != 0.0F) {
                // Set the clipping region to draw only inside the area.
                g.PushClip(path.GetIGraphicsPath(g));

                // use a transform to rotate.
                Matrix matrix = new Matrix();
                matrix.RotateAt(angle, new PointF(0, 0));
                g.PushTransform(matrix);

                try
                {
                    // Get the correct bounding rect.
                    RectangleF bounding = Util.BoundsOfRotatedRectangle(path.BoundingBox, new PointF(), -angle);

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
        void CreatePatternBrush(float pixelSize, GraphicsTarget gt)
        {
            //  Determine adjusted pixel size of the brush to create. 
            const float MAX_PATTERN_SIZE = 80;
            const float MIN_PATTERN_SIZE = 10;
            if (pixelSize < patternWidth / MAX_PATTERN_SIZE)
                pixelSize = patternWidth / MAX_PATTERN_SIZE;
            if (pixelSize < patternHeight / MAX_PATTERN_SIZE)
                pixelSize = patternHeight / MAX_PATTERN_SIZE;
            if (pixelSize > patternWidth / MIN_PATTERN_SIZE)
                pixelSize = patternWidth / MIN_PATTERN_SIZE;
            if (pixelSize > patternHeight / MIN_PATTERN_SIZE)
                pixelSize = patternHeight / MIN_PATTERN_SIZE;

            if (patternBrushes != null && Math.Abs(pixelSize - pixelSizeCached) / pixelSize < 0.01)
                return;         // the pattern brush is already OK size.

            // Get size of bitmap to create with the image of the pattern.
            float width = (float) Math.Round(patternWidth / pixelSize);
            float height = (float) Math.Round(patternHeight / pixelSize);

            int bitmapWidth = (int) width;
            int bitmapHeight = (int) (offsetRows ? height * 2 : height);

            // Create dictionary to hold brushes for each color.
            patternBrushes = new Dictionary<SymColor, IGraphicsBrush>(2);
            pixelSizeCached = pixelSize;

            RenderOptions renderOpts = new RenderOptions();
            renderOpts.minResolution = pixelSize;
            renderOpts.usePatternBitmaps = false;

            foreach (SymColor color in map.colors) {
                if (!patternGlyph.HasColor(color))
                    continue;

                // Create a new pattern brush.
                GraphicsBrushTarget brushTarget = gt.CreatePatternBrush(new SizeF(patternWidth, (offsetRows ? patternHeight * 2 : patternHeight)), bitmapWidth, bitmapHeight);

                // Draw the pattern into the bitmap.
                if (offsetRows)
                {
                    patternGlyph.Draw(brushTarget, new PointF(0F, 0), -patternAngle, null, null, color, renderOpts);
                    patternGlyph.Draw(brushTarget, new PointF(patternWidth / 2, patternHeight), -patternAngle, null, null, color, renderOpts);
                    patternGlyph.Draw(brushTarget, new PointF(-patternWidth / 2, patternHeight), -patternAngle, null, null, color, renderOpts);
                    patternGlyph.Draw(brushTarget, new PointF(patternWidth / 2, -patternHeight), -patternAngle, null, null, color, renderOpts);
                    patternGlyph.Draw(brushTarget, new PointF(-patternWidth / 2, -patternHeight), -patternAngle, null, null, color, renderOpts);
                }
                else
                {
                    patternGlyph.Draw(brushTarget, new PointF(0F, 0F), -patternAngle, null, null, color, renderOpts);
                }

                // Get the brush
                IGraphicsBrush brush = brushTarget.FinishPatternBrush(patternAngle);
                
                // Add it to the collection of brushes.
                patternBrushes.Add(color, brush);
            }
        }

        // Draw the pattern (at the given angle) inside the path.
        void DrawPattern(GraphicsTarget g, SymPathWithHoles path, float angle, SymColor color, RenderOptions renderOpts)
        {
            // Set the clipping region to draw only inside the area.
            g.PushClip(path.GetIGraphicsPath(g));

            // use a transform to rotate 
            Matrix matrix = new Matrix();
            matrix.RotateAt(patternAngle + angle, new PointF(0, 0));
            g.PushTransform(matrix);

            try
            {
                // Get the correct bounding rect.
                RectangleF bounding = Util.BoundsOfRotatedRectangle(path.BoundingBox, new PointF(), -(patternAngle + angle));

                DrawPatternRows(g, bounding, color, renderOpts);
            }
            finally {
                // restore the clip region and the transform
                g.PopTransform();
                g.PopClip();
            }
        }

        // Draw a set of rows of the pattern with the given rectangle
        void DrawPatternRows(GraphicsTarget g, RectangleF boundingRect, SymColor color, RenderOptions renderOpts)
        {
            double topLine = Math.Round(boundingRect.Top / patternHeight) * patternHeight;
            double bottomLine = (Math.Round(boundingRect.Bottom / patternHeight) + 0.5) * patternHeight;
            double leftLine = Math.Round(boundingRect.Left / patternWidth) * patternWidth;
            double rightLine = (Math.Round(boundingRect.Right / patternWidth) + 0.5) * patternWidth;
            double offsetLeftLine = leftLine - (patternWidth / 2);
            double offsetRightLine = rightLine + (patternWidth / 2);
            bool firstLineOffset = ((long) Math.Round(boundingRect.Top / patternHeight) & 1) != 0;

            bool offsetThisLine = offsetRows && firstLineOffset;
            for (double y = topLine; y <= bottomLine; y += patternHeight) {
                if (offsetThisLine) {
                    for (double x = offsetLeftLine; x <= offsetRightLine; x += patternWidth) {
                        patternGlyph.Draw(g, new PointF((float) x, (float) y), -patternAngle, null, null, color, renderOpts);
                    }
                }
                else {
                    for (double x = leftLine; x <= rightLine; x += patternWidth) {
                        patternGlyph.Draw(g, new PointF((float) x, (float) y), -patternAngle, null, null, color, renderOpts);
                    }
                }

                if (offsetRows)
                    offsetThisLine = !offsetThisLine;
            }
        }


        // Calculate the bounding box
        internal RectangleF CalcBounds(SymPathWithHoles path)
        {
            if (borderSymdef != null)
                return borderSymdef.CalcBounds(path.MainPath);
            else
                return path.BoundingBox;
        }
    }

    // The alignment of text symbols.
    public enum TextSymDefAlignment
    {
        Left,
        Right,
        Center,
        Justified
    }

    public class TextSymDef: SymDef
    {
        public enum FramingStyle {None, Line, Shadow, Rectangle };
        public struct Framing {
            public FramingStyle framingStyle;
            public SymColor framingColor;
            public float lineWidth;
            public LineStyle lineStyle;
            public float shadowX, shadowY;
            public float rectBorderLeft, rectBorderTop, rectBorderRight, rectBorderBottom;
        }

        public struct Underlining
        {
            public bool underlineOn;
            public SymColor underlineColor;
            public float underlineWidth;
            public float underlineDistance;
        }

        SymColor fontColor;
        float fontSize;
        string fontName;
        bool bold, italic;
        TextSymDefAlignment fontAlign;
        float lineSpacing;
        float paraSpacing;   
        float charSpacing, wordSpacing;
        float firstIndent, restIndent;
        float[] tabs;
        Framing framing;
        Underlining underline;

        // GDI+ object correspoding to the above attributes.
        bool objectsCreated;
        List<IGraphicsPen> framingPens;
        IGraphicsPen underlinePen;

        bool fontsCreated;
#if WPF
        Typeface typeface;
        float ascent, descent, capHeight;
#else
        Font font;
        StringFormat stringFormat;
#endif

        float spaceWidth;     // width of one space.

        const string ParagraphMark = "\x2029";  // string denoted a paragraph boundary (Unicode paragraph mark).

        public TextSymDef(string name, int ocadID)
            : base(name, ocadID)
        {
        }

        public override void SetMap(Map newMap)
        {
            base.SetMap(newMap);
        }

#if WPF
        [DllImport("shell32.dll")]
        private static extern bool SHGetSpecialFolderPath(IntPtr hwndOwner,
           [Out] StringBuilder lpszPath, int nFolder, bool fCreate);
        const int CSIDL_FONTS = 0x14;  // Font folder

        // Create a Typeface, taking into account a nasty WPF bugs regarding Arial Narrow.
        private Typeface CreateTypeface(string fontName, bool bold, bool italic)
        {
            if (!Util.FontExists(fontName))
                fontName = "Arial";          // Map non-existant fonts to "Arial".

            if (fontName == "Arial Narrow") {
                // Arial Narrow doesn't work right in WPF. We can work around by going directly to the font file.
                string fontfileName;
                if (!bold && !italic)
                    fontfileName = "arialn.ttf";
                else if (bold && !italic)
                    fontfileName = "arialnb.ttf";
                else if (!bold && italic)
                    fontfileName = "arialni.ttf";
                else
                    fontfileName = "arialnbi.ttf";

                // Get font folder
                StringBuilder fontPath = new StringBuilder(260);
                if (SHGetSpecialFolderPath(IntPtr.Zero, fontPath, CSIDL_FONTS, false)) {
                    // Get path to font name.
                    string fontfile = Path.Combine(fontPath.ToString(), fontfileName);
                    UriBuilder fontfileUriBuilder = new UriBuilder(new Uri(fontfile));
                    fontfileUriBuilder.Fragment = "Arial";
                    Typeface typeface = new Typeface(new FontFamily(fontfileUriBuilder.Uri.ToString()), italic ? FontStyles.Italic : FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Condensed);
                    GlyphTypeface gtf;
                    if (typeface.TryGetGlyphTypeface(out gtf))
                        return typeface;           // Make sure that the font file really exists, by getting the glyph typeface.
                }
            }

            return new Typeface(new FontFamily(fontName), italic ? FontStyles.Italic : FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal);
        }
#endif

        private void CreateFonts()
        {
            Debug.Assert(!fontsCreated);

#if WPF
            // Get the typeface.
            typeface = CreateTypeface(fontName, bold, italic);
            FontFamily family = typeface.FontFamily;
            GlyphTypeface glyphTypeface = null;
            typeface.TryGetGlyphTypeface(out glyphTypeface);

            // Get the ascent, descent, capheight values.
            ascent = (float)(family.Baseline * fontSize);
            capHeight = (float)(typeface.CapsHeight * fontSize);
            if (glyphTypeface != null)
            {
                descent = (float)((glyphTypeface.Height - glyphTypeface.Baseline) * fontSize);
                spaceWidth = (float)(glyphTypeface.AdvanceWidths[glyphTypeface.CharacterToGlyphMap[32]] * fontSize);
            }
            else
            {
                // We can try to measure characters instead.
                throw new NotImplementedException();
            }

#else
            FontStyle fontStyle = FontStyle.Regular;
            if (bold)
                fontStyle |= FontStyle.Bold;
            if (italic)
                fontStyle |= FontStyle.Italic;

            float nominalFontSize = Math.Max(fontSize, 0.01F);            // 0 size fonts cause exception!

            if (Util.FontExists(fontName))
                font = new Font(fontName, nominalFontSize, fontStyle, GraphicsUnit.World);
            else
                font = new Font(new FontFamily(GenericFontFamilies.SansSerif), nominalFontSize, fontStyle, GraphicsUnit.World);

            stringFormat = new StringFormat(StringFormat.GenericTypographic);
            stringFormat.Alignment = StringAlignment.Near;
            stringFormat.LineAlignment = StringAlignment.Near;
            stringFormat.FormatFlags |= StringFormatFlags.NoClip;
            stringFormat.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

            spaceWidth = Util.GetHiresGraphics().MeasureString(" ", font, 1000000, stringFormat).Width;
#endif

            fontsCreated = true;
        }

        private void CreateObjects(GraphicsTarget g)
        {
            Debug.Assert(!objectsCreated);

            if (!fontsCreated)
                CreateFonts();

            if (framing.framingStyle == FramingStyle.Line) {
                framingPens = new List<IGraphicsPen>();

                // We use multiple pens to avoid weird artifacts with overlapping parts.
                IGraphicsPen p;
                for (float width = 0; width < framing.lineWidth * 2; width += fontSize * 0.33F) {
                    p = GraphicsUtil.CreateSolidPen(g, framing.framingColor.ColorValue, width, LineStyle.Beveled);
                    framingPens.Add(p);
                }
                p = GraphicsUtil.CreateSolidPen(g, framing.framingColor.ColorValue, framing.lineWidth * 2, framing.lineStyle);
                framingPens.Add(p);
            }

            if (underline.underlineOn) {
                underlinePen = GraphicsUtil.CreateSolidPen(g, underline.underlineColor.ColorValue, underline.underlineWidth, LineStyle.Mitered);
            }

            objectsCreated = true;
        }

        public override void FreeGdiObjects()
        {
#if !WPF
            if (font != null) {
                font.Dispose();
                font = null;
            }
#endif

            if (framingPens != null) {
                foreach (IGraphicsPen p in framingPens)
                    p.Dispose();
                framingPens = null;
            }

            if (underlinePen != null) {
                underlinePen.Dispose();
                underlinePen = null;
            }

            objectsCreated = false;
            fontsCreated = false;
        }


        // Does this symbol definition draw the given color. Used to determine
        // if this symbol definition draws into the current layer being draw.
        public override bool HasColor(SymColor color)
        {
            if (color == null)
                return false;

            return color == fontColor || (framing.framingStyle != FramingStyle.None && color == framing.framingColor) || (underline.underlineOn && color == underline.underlineColor);
        }

        public void SetFont(string fontName, float fontSize, bool bold, bool italic, SymColor fontColor, float lineSpacing, float paraSpacing, float firstIndent, float restIndent, float[] tabs, float charSpacing, float wordSpacing, TextSymDefAlignment fontAlign)
        {
            CheckModifiable();
            this.fontName = fontName;
            this.fontSize = fontSize;
            this.bold = bold;
            this.italic = italic;
            this.fontColor = fontColor;
            this.lineSpacing = lineSpacing;
            this.paraSpacing = paraSpacing;
            this.firstIndent = firstIndent;
            this.restIndent = restIndent;
            this.charSpacing = charSpacing;
            this.wordSpacing = wordSpacing;
            this.fontAlign = fontAlign;
            this.tabs = tabs;
        }

        public void SetFraming(Framing framing)
        {
            CheckModifiable();
            this.framing = framing;
        }

        public void SetUnderline(Underlining underline)
        {
            CheckModifiable();
            this.underline = underline;
        }

        public TextSymDefAlignment FontAlignment { get { return fontAlign; } }
        public string FontName { get { return fontName; } }
        public bool Bold { get { return bold; } }
        public bool Italic { get { return italic; } }
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
                if (!fontsCreated)
                    CreateFonts();

#if WPF
                return ascent;
#else
                FontFamily fam = font.FontFamily;
                FontStyle fontStyle = font.Style;
                int emHeight = fam.GetEmHeight(fontStyle);
                int ascent = fam.GetCellAscent(fontStyle);
                return (ascent * fontSize) / emHeight;
#endif
            }
        }

        // Get height of the "W" character.
        public float WHeight
        {
            get
            {
                if (!fontsCreated)
                    CreateFonts();

#if WPF
                return capHeight;
#else
                GraphicsPath path = new GraphicsPath();
                path.AddString("W", font.FontFamily, (int)font.Style, font.Size, new PointF(0, 0), stringFormat);
                return path.GetBounds().Height;
#endif
            }
        }

        public float FontDescent
        {
            get
            {
                if (!fontsCreated)
                    CreateFonts();

#if WPF
                return descent;
#else
                FontFamily fam = font.FontFamily;
                FontStyle fontStyle = font.Style;
                int emHeight = fam.GetEmHeight(fontStyle);
                int descent = fam.GetCellDescent(fontStyle);
                return (descent * fontSize) / emHeight;
#endif
            }
        }

        // Draw a single line of text at the given point with the given brush.
        private void DrawSingleLineString(GraphicsTarget g, string text, IGraphicsBrush brush, PointF pt)
        {
#if WPF
            FormattedText formattedText = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, fontSize, (brush as WPF_Brush).Brush);
            g.DrawingContext.DrawText(formattedText, new Point(pt.X, pt.Y));
#else
            // Occasonal GDI+ throws an exception if the font size is super small.
            try {
                g.Graphics.DrawString(text, font, (brush as GDIPlus_Brush).Brush, pt, stringFormat);
            }
            catch (System.Runtime.InteropServices.ExternalException) {
                // Do nothing
            }
#endif
        }

        // Measure the width of a single line of text.
        private float MeasureStringWidth(string text)
        {
#if WPF
            FormattedText formattedText = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
            return (float) formattedText.Width;
#else
            return Util.GetHiresGraphics().MeasureString(text, font, new PointF(0, 0), stringFormat).Width;
#endif
        }

        // Draw a string with shadow or line framing effects, if specified. The font from this symdef is used.
        private void DrawStringWithEffects(GraphicsTarget g, SymColor color, string text, PointF pt)
        {
            if (color == fontColor) {
                DrawSingleLineString(g, text, fontColor.GetBrush(g), pt);
            }

            if (framing.framingStyle != FramingStyle.None && color == framing.framingColor) {
                if (framing.framingStyle == FramingStyle.Line) {
#if WPF
                    FormattedText formattedText = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
                    Geometry geometry = formattedText.BuildGeometry(new Point(pt.X, pt.Y));
                    foreach (IGraphicsPen p in framingPens)
                        g.DrawingContext.DrawGeometry(null, (p as WPF_Pen).Pen, geometry);
#else
                    GraphicsPath grPath = new GraphicsPath(FillMode.Winding);
                    Debug.Assert(font.Unit == GraphicsUnit.World);
                    grPath.AddString(text, font.FontFamily, (int) font.Style, font.Size, pt, stringFormat);

                    foreach (IGraphicsPen p in framingPens)
                        g.Graphics.DrawPath((p as GDIPlus_Pen).Pen, grPath);
#endif
                }
                else if (framing.framingStyle == FramingStyle.Shadow) {
                    DrawSingleLineString(g, text, framing.framingColor.GetBrush(g), new PointF(pt.X + framing.shadowX, pt.Y - framing.shadowY));
                }
            }
        }

        // Draw the framing rectangle around some text. The top of the text is at 0, and the bottom baseline of text is at "bottomOfText".
        private void DrawFramingRectangle(GraphicsTarget g, float[] lineWidths, float fullWidth, SymColor color, float bottomOfText)
        {
            if (framing.framingStyle == FramingStyle.Rectangle && color == framing.framingColor) {
                // First, figure out the width of the rectangle. If fullWidth is zero, used the maximum line width.
                fullWidth = CalcFullWidth(lineWidths, fullWidth);

                // Next, figure out the rectangle, not counting padding.
                float l, t, r, b;
                if (fontAlign == TextSymDefAlignment.Right)
                    l = -fullWidth;
                else if (fontAlign == TextSymDefAlignment.Center)
                    l = -(fullWidth / 2F);
                else
                    l = 0;
                r = l + fullWidth;
                t = FontAscent - WHeight;           // Place the top of the rectangle at top of letter "W", not top of accents.
                b = bottomOfText;

                // Add padding.
                t -= framing.rectBorderTop;
                b += framing.rectBorderBottom;
                l -= framing.rectBorderLeft;
                r += framing.rectBorderRight;

                // Draw the rectangle
                g.FillRectangle(color.GetBrush(g), new RectangleF(l, t, r - l, b - t));
            }
        }

        private float CalcFullWidth(float[] lineWidths, float fullWidth)
        {
            if (fullWidth == 0) {
                foreach (float w in lineWidths) {
                    if (w > fullWidth)
                        fullWidth = w;
                }
                if (fontAlign == TextSymDefAlignment.Justified || fontAlign == TextSymDefAlignment.Left)
                    fullWidth += firstIndent;   // if fullWidth is zero, this is unformatted text, and only the firstIndent is used.
            }
            return fullWidth;
        }

        // Draw an underline under the text, if applicable.
        private void DrawUnderline(GraphicsTarget g, SymColor color, float baseline, float width, float indent)
        {
            if (underline.underlineOn && color == underline.underlineColor) {
                // Figure out the left and right sides of the underline.
                float l, r;
                if (fontAlign == TextSymDefAlignment.Right)
                    l = -width;
                else if (fontAlign == TextSymDefAlignment.Center)
                    l = -(width / 2F);
                else
                    l = indent;
                r = l + width;

                // figure out y coordinate of line.
                float y = baseline + underline.underlineDistance + underline.underlineWidth / 2;

                // draw the line.
                g.DrawLine(underlinePen, new PointF(l, y), new PointF(r, y));
            }
        }


        // Draw this text symbol at point pt with angle ang in this graphics (given color only). 
        internal void Draw(GraphicsTarget g, string[] text, float[] lineWidths, PointF location, float angle,  float fullWidth, SymColor color, RenderOptions renderOpts)
        {
            if (color == null)
                return;
            if (color != fontColor && (framing.framingStyle == FramingStyle.None || color != framing.framingColor) && (!underline.underlineOn || color != underline.underlineColor))
                return;

            if (!objectsCreated)
                CreateObjects(g);

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
                        if (fontAlign == TextSymDefAlignment.Right)
                            pt.X = leftEdge = -lineWidth;
                        else if (fontAlign == TextSymDefAlignment.Center)
                            pt.X = leftEdge = -(lineWidth / 2F);
                        else {
                            leftEdge = 0;
                            pt.X = indent = firstLineOfPara ? firstIndent : restIndent;     // indents only used for left align or justified
                        }

                        // Get the size of spaces. Justification is done by adjusting this.
                        float sizeOfSpace = wordSpacing * spaceWidth;            // basic width of spaces as set by the symdef
                        if (fontAlign == TextSymDefAlignment.Justified && !lastLineOfPara && fullWidth > 0)
                            sizeOfSpace += JustifyText(line, lineWidth, fullWidth - indent);

                        // Draw all the text segments in the line. (A text segment is a word, unless charSpacing>0, in which case it is graphemes).
                        int index = 0;
                        for (; ; ) {
                            string textSegment;

                            if (charSpacing > 0)
                                textSegment = StringInfo.GetNextTextElement(line.Substring(index));
                            else
                                textSegment = GetNextTextSegment(line.Substring(index));
                            if (string.IsNullOrEmpty(textSegment))
                                break;

                            if (textSegment == " ")
                                pt.X += sizeOfSpace;
                            else if (textSegment == "\t")
                                pt.X += WidthOfTextSegment("\t", pt.X - leftEdge);
                            else {
                                DrawStringWithEffects(g, color, textSegment, pt);
                                pt.X += MeasureStringWidth(textSegment);

                                if (charSpacing > 0)
                                    pt.X += charSpacing * spaceWidth;
                            }

                            index += textSegment.Length;
                        }

                        baselineOfLine = pt.Y + FontAscent;        // Set the bottom of the text.

                        if (lastLineOfPara)
                            DrawUnderline(g, color, baselineOfLine, Math.Max(fullWidth, lineWidth), (fullWidth == 0) ? indent : 0);

                        pt.Y += lineSpacing;
                        firstLineOfPara = false;
                    }
                }

                // Draw the framing rectangle, if any.
                if (underline.underlineOn)
                    baselineOfLine += underline.underlineDistance + underline.underlineWidth;
                DrawFramingRectangle(g, lineWidths, fullWidth, color, baselineOfLine);
            }
            finally {
                g.PopTransform();
            }
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

        // Calculate the bounding box. 
        public RectangleF CalcBounds(string[] text, float[] lineWidths, PointF location, float angle, float fullWidth, out SizeF size)
        {
            if (!fontsCreated)
                CreateFonts();

            // count number of lines, number of new paragraphs.
            int lineCount, newParaCount = 0;
            for (int i = 0; i < text.Length; ++i)
                if (text[i] == ParagraphMark)
                    ++newParaCount;
            lineCount = text.Length - newParaCount;

            // Calculate height of text.
            float height = FontAscent + FontDescent + ((lineCount - 1) * lineSpacing) + (newParaCount * paraSpacing);

            // Calculate full width of text.
            fullWidth = CalcFullWidth(lineWidths, fullWidth);

            // Get the size.
            size = new SizeF(fullWidth, height);

            // The rectangle, unrotated.
            RectangleF rect;
            if (fontAlign == TextSymDefAlignment.Left || fontAlign == TextSymDefAlignment.Justified)
                rect = new RectangleF(location.X, location.Y - size.Height, size.Width, size.Height);  // indents only used for left aligned and justified text.
            else if (fontAlign == TextSymDefAlignment.Right)
                rect = new RectangleF(location.X - size.Width, location.Y - size.Height, size.Width, size.Height);
            else {
                Debug.Assert(fontAlign == TextSymDefAlignment.Center);
                rect = new RectangleF(location.X - size.Width / 2, location.Y - size.Height, size.Width, size.Height);
            }

            // Expand for framing and underlining.
            if (underline.underlineOn)
                rect = RectangleF.FromLTRB(rect.Left, rect.Top - (underline.underlineDistance + underline.underlineWidth), rect.Right, rect.Bottom);

            RectangleF rectFrame = rect;
            if (framing.framingStyle == FramingStyle.Line)
                rect.Inflate(framing.lineWidth, framing.lineWidth);
            else if (framing.framingStyle == FramingStyle.Shadow) {
                RectangleF shadow = rect;
                shadow.Offset(framing.shadowX, framing.shadowY);
                rect = RectangleF.Union(rect, shadow);
            }
            else if (framing.framingStyle == FramingStyle.Rectangle) 
                rect = RectangleF.FromLTRB(rect.Left - framing.rectBorderLeft, rect.Top - framing.rectBorderBottom, rect.Right + framing.rectBorderRight, rect.Bottom + framing.rectBorderTop);

            // Rotate the rectangle.
            if (angle != 0)
                rect = Util.BoundsOfRotatedRectangle(rect, location, angle);

            return rect;
        }

        // Breaks unwrapped text into paragraphs. This just means adding a paragraph mark
        // between each line. The lineWidths array has the width of each line.
        internal string[] BreakUnwrappedLines(string[] text, out float[] lineWidths)
        {
            if (!fontsCreated)
                CreateFonts();

            // We ignore ONE initial blank line for OCAD compatibility.
            int firstLine = (text.Length > 0 && text[0] == "") ? 1 : 0;

            if (text.Length == firstLine) {
                lineWidths = new float[0];
                return new string[0];
            }

            string[] newLines = new string[(text.Length - firstLine) * 2 - 1];
            lineWidths = new float[newLines.Length];

            for (int i = 0; i < text.Length - firstLine; ++i) {
                newLines[i * 2] = text[i + firstLine];
                lineWidths[i * 2] = LineWidth(text[i + firstLine]);
                if (i + firstLine != text.Length - 1) {
                    newLines[i * 2 + 1] = ParagraphMark;
                    lineWidths[i * 2 + 1] = 0;
                }
            }

            return newLines;
        }

        // Breaks the text into lines based on the given width. Returns a new string array with
        // line breaks made into it. A line break that already exists is turned into a paragraph break,
        // which is a line with just a unicode paragraph separator in it. The line widths array has the 
        // width of each line.
        internal string[] BreakLines(string[] text, float width, out float[] lineWidths)
        {
            if (!fontsCreated)
                CreateFonts();

            List<String> lineList = new List<String>();
            List<float> widthList = new List<float>();

            float widthFirstLine, widthRemainingLines;
            if (fontAlign == TextSymDefAlignment.Left || fontAlign == TextSymDefAlignment.Justified) {
                widthFirstLine = Math.Max(0F, width - firstIndent);
                widthRemainingLines = Math.Max(0F, width - restIndent);
            }
            else {
                // indents only used for left-aligned and justified text.
                widthFirstLine = widthRemainingLines = width;
            }

            // We ignore ONE initial blank line for OCAD compatibility.
            int firstLine = (text.Length > 0 && text[0] == "") ? 1 : 0;

            for (int i = firstLine; i < text.Length; ++i) {
                WrapParagraph(text[i], widthFirstLine, widthRemainingLines, lineList, widthList);
                if (i < text.Length - 1) {
                    lineList.Add(ParagraphMark);
                    widthList.Add(0);
                }
            }

            lineWidths = widthList.ToArray();
            return lineList.ToArray();
        }

        // Split text into lines of length width or less, and add those lines to the given ArrayList (and widths of the lines to the width list).
        private void WrapParagraph(string text, float widthFirstLine, float widthRemainingLines, List<String> lineList, List<float> widthList)
        {
            bool firstLine = true;
            while (text != null) {
                float lineWidth;
                string line = WrapOneLine(ref text, firstLine ? widthFirstLine : widthRemainingLines, out lineWidth);
                lineList.Add(line);
                widthList.Add(lineWidth);
                firstLine = false;
            }
        }

        // Figure out how much of the line will fit and return that. line is modified
        // to be the remaining text to fit on subsequent lines, or null if nothing left. The amount of width
        // actually consumed is returned in actualLineWidth.
        private string WrapOneLine(ref string line, float lineWidth, out float actualLineWidth)
        {
            StringBuilder lineSoFar = new StringBuilder();
            float widthUsed = 0F;
            bool useSingleLetters = false;

            for (; ; ) {
                // Get next segment of text to add.
                string nextSegment;
                if (useSingleLetters)
                    nextSegment = StringInfo.GetNextTextElement(line);
                else
                    nextSegment = GetNextTextSegment(line);
                if (string.IsNullOrEmpty(nextSegment))
                    break;

                // See if this segment will fit on the line.
                float segmentWidth = WidthOfTextSegment(nextSegment, widthUsed);
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
                    widthUsed -= WidthOfTextSegment(" ", widthUsed);
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
        private float LineWidth(string text)
        {
            string nextSegment;
            float width = 0;

            while ((nextSegment = GetNextTextSegment(text)) != null) {
                width += WidthOfTextSegment(nextSegment, width);
                text = text.Substring(nextSegment.Length);
            }

            return width;
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

        // Get the width of a text segment. Handles tabs, spaces between characters, and space widths.
        private float WidthOfTextSegment(string text, float widthSoFar)
        {
            if (text == " ") {
                return spaceWidth * wordSpacing;
            }
            else if (text == "\t") {
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
            else if (charSpacing > 0) {
                float width = 0;
                TextElementEnumerator enumTextElements = StringInfo.GetTextElementEnumerator(text);
                while (enumTextElements.MoveNext()) 
                    width += MeasureStringWidth(enumTextElements.GetTextElement()) + (charSpacing * spaceWidth);
                return width;
            }
            else {
                return MeasureStringWidth(text);
            }
        }




        // The remaining functions all have to do with text along a path ("line text").

        // Structure to show where one character (which might be multiple Unicode codepoints), hence Grapheme, is placed
        // when laying out line text.
        struct GraphemePlacement
        {
            public string grapheme;       // The grapheme to draw.
            public float width;                // The width of this grapheme.
            public PointF pointStart;      // The location the grapheme starts at.
            public float angle;                // The angle of the grapheme.

            public GraphemePlacement(string grapheme, float width, PointF pointStart, float angle)
            {
                this.grapheme = grapheme;
                this.width = width;
                this.pointStart = pointStart;
                this.angle = angle;
            }
        }

        // Draw this text symbol along a path.
        internal void DrawTextOnPath(GraphicsTarget g, SymPath path, string text, SymColor color, RenderOptions renderOpts)
        {
            if (color == null)
                return;
            if (color != fontColor && (framing.framingStyle == FramingStyle.None || color != framing.framingColor))
                return;

            if (!objectsCreated)
                CreateObjects(g);

            // Get the location of each grapheme to print.
            List<GraphemePlacement> graphemeList = GetLineTextGraphemePlacement(path, text);
            PointF topAscentPoint = new PointF(0, -FontAscent);    // Drawing is relative to top of char, we want to draw at baseline.

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

        // Calculate bounds of text along a path with this symbol.
        internal RectangleF CalcBounds(SymPath path, string text)
        {
            // This doesn't take into account the text at all -- just the line. It is probably good enought. We could do better 
            // with a bunch of work by calling GetLineTextGraphemePlacement() and unioning the bounds of each grapheme.
            // But not that necessary.

            RectangleF rect = path.BoundingBox;
            rect.Inflate(FontAscent, FontAscent);

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
        private List<GraphemePlacement> GetLineTextGraphemePlacement(SymPath path, string text)
        {
            float totalWidth = 0;
            List<GraphemePlacement> graphemeList = new List<GraphemePlacement>();
            float pathLength = path.BizzarroLength;
            if (pathLength == 0)
                return graphemeList;            // nothing to draw.

            // First, determine all the graphemes and their width
            TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(text);
            while (enumerator.MoveNext()) {
                string grapheme = enumerator.GetTextElement();
                float graphemeWidth;

                if (grapheme == " ")
                    graphemeWidth = wordSpacing * spaceWidth;
                else {
                    float width = MeasureStringWidth(grapheme);
                    graphemeWidth = width + charSpacing * spaceWidth;
                }

                graphemeList.Add(new GraphemePlacement(grapheme, graphemeWidth, new PointF(), 0));
                totalWidth += graphemeWidth;
                if (totalWidth + 0.01F >= pathLength && fontAlign != TextSymDefAlignment.Justified)
                    break;          // We don't have any room for more characters. (0.01 prevents a very small tail at the end.)
            }

            // For OCAD compatibility, truncate right aligned text if too big to fit so the whole
            // string fits. (Note that left-aligned text will typically show one more character than this.)
            if (pathLength < totalWidth && fontAlign != TextSymDefAlignment.Left && fontAlign != TextSymDefAlignment.Justified) {
                totalWidth -= graphemeList[graphemeList.Count - 1].width;
                if (fontAlign == TextSymDefAlignment.Right)
                    graphemeList.RemoveAt(graphemeList.Count - 1);
            }

            // Where does the text begin?
            float startingDistance = 0;
            if (fontAlign == TextSymDefAlignment.Left || fontAlign == TextSymDefAlignment.Justified)
                startingDistance = 0;
            else if (fontAlign == TextSymDefAlignment.Right)
                startingDistance = pathLength - totalWidth;
            else if (fontAlign == TextSymDefAlignment.Center)
                startingDistance = (pathLength - totalWidth) / 2;

            // For justified (all-line) text, adjust the widths of each character so they all fit.
            if (fontAlign == TextSymDefAlignment.Justified && graphemeList.Count > 1) {
                if (charSpacing > 0) {
                    // last character doesn't have space added.
                    GraphemePlacement graphemePlacement = graphemeList[graphemeList.Count - 1];
                    graphemePlacement.width -= charSpacing * spaceWidth;
                    totalWidth -= charSpacing * spaceWidth;
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
    }


    // The GraphicsSymDef is a sort of dummy symdef used for graphics objects -- objects
    // created by doing a "To Graphics" operation in OCAD. These objects define their own color and shape,
    // so there really isn't any state in the symdef that is useful. There is a singleton GraphicsSymDef that
    // is used as the symdef for all of these objects, if there are any.
    public class GraphicsSymDef: SymDef
    {
        public GraphicsSymDef()
            : base("Graphics object", -2)
        {
        }

        // Does this symbol definition draw the given color. Used to determine
        // if this symbol definition draws into the current layer being draw.
        public override bool HasColor(SymColor color)
        {
            // Always return true, because graphics objects could be any color! (except image layer)
            if (color == null)
                return false;
            else
                return true;
        }

        public override void FreeGdiObjects()
        {
        }

    }

    // The ImageSymDef is a sort of dummy symdef used for images objects -- objects
    // created by doing a import image operation in OCAD. These objects live in a layer below
    // all others. There is a singleton ImageSymDef that
    // is used as the symdef for all of these objects, if there are any.
    public class ImageSymDef: SymDef
    {
        public ImageSymDef()
            : base("Image object", -3)
        {
        }

        // Does this symbol definition draw the given color. Used to determine
        // if this symbol definition draws into the current layer being draw.
        public override bool HasColor(SymColor color)
        {
            return (color == null);  // only draw in the image layer.
        }

        public override void FreeGdiObjects()
        {
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

