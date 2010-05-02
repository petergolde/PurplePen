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

//#define DRAWBOUNDS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using Matrix = System.Drawing.Drawing2D.Matrix;
using LineJoin = System.Drawing.Drawing2D.LineJoin;
using LineCap = System.Drawing.Drawing2D.LineCap;
using Color = System.Drawing.Color;

namespace PurplePen.MapModel
{
    // A color matrix
    public class ColorMatrix
    {
        private float[][] entries;

        public ColorMatrix()
        {
            entries = new float[5][];
            for (int i = 0; i < 5; ++i)
                entries[i] = new float[5];
        }

        public ColorMatrix(float[][] entries)
        {
            this.entries = entries;
        }

        public float this[int i, int j]
        {
            get
            {
                return entries[i][j];
            }
            set
            {
                entries[i][j] = value;
            }
        }
    }

    class MapUsageException : Exception
    {
        public MapUsageException(string message) : base(message)
        {}
    }

    public struct SymbolHit
    {
        public Symbol symbol;
        public float distance;
    }

    // Information about a template.
    public class TemplateInfo
    {
        public string absoluteFileName;           // absolute file name to template. Relativized on write/made absolute when read.
        public PointF centerPoint;                    // center point of the template.
        public float dpi;                                    // dpi of the template (if a bitmap)
        public float angle;                                // angle in degrees of the template.
        public bool visible;                               // is the template currently visible?

        public TemplateInfo(string absoluteFileName, PointF centerPoint, float dpi, float angle, bool visible)
        {
            this.absoluteFileName = absoluteFileName;
            this.centerPoint = centerPoint;
            this.dpi = dpi;
            this.angle = angle;
            this.visible = visible;
        }
    }

    public class Map
    {
        internal List<SymColor> colors = new List<SymColor>();
        List<SymDef> symdefs = new List<SymDef>();
        List<Symbol> symbols = new List<Symbol>();
        Dictionary<SymDef, bool> hiddenSymbols = new Dictionary<SymDef, bool>();
        ColorMatrix colorMatrix;               // if non-null, transforms colors when rendering the map.
        float mapScale;
        float printScale;
        RectangleF printArea;                  // print area, if 0,0,0,0, then print whole map.
        TemplateInfo template;               // if non-null, information about the template associated with the map (templates are NOT rendered).
        ReaderWriterLock maplock = new ReaderWriterLock();
        bool mapDirty = false;
        bool symdefsDirty = false;
        bool boundsAccurate = false;          // Are the map bounds accurate?
        RectangleF bounds;                        // If boundsAccurate is true, the bounds are accurate.

        IGraphicsPen boundsPen;                          // pen for drawing symbol bounds.

        ITextMetrics textMetricsProvider;


        OcadSetup ocadSetupStructure;    // An OCAD setup structure to preserve.
        internal OcadSetup OcadSetupStructure {
            get { 
                if (ocadSetupStructure != null)
                    return ocadSetupStructure.Clone();
                else 
                    return null;
            }
            set {
                ocadSetupStructure = value;
            }
        }

        OcadSymbolHeader ocadSymbolHeader;    
        internal OcadSymbolHeader OcadSymbolHeaderStructure {
            get { 
                if (ocadSymbolHeader != null)
                    return ocadSymbolHeader.Clone();
                else
                    return null;
            }
            set {
                ocadSymbolHeader = value;
            }
        }

        // A list of messages describing objects that won't render correctly.
        internal List<string> nonRenderableObjects;      
        public string[] NotRenderableObjects {
            get
            {
                return nonRenderableObjects.ToArray();
            }
        }

        // A list of fonts name that are in the map, but not installed.
        internal List<string> missingFonts;
        public string[] MissingFonts
        {
            get
            {
                return missingFonts.ToArray();
            }
        }

        public delegate void MapChangedHandler(Map sender);
        public event MapChangedHandler OnMapChanged;
        public delegate void SymdefsChangedHandler(Map sender);
        public event SymdefsChangedHandler OnSymdefsChanged;

        public struct WriteReleaser: IDisposable
        {
            private Map map;

            public WriteReleaser(Map map) { this.map = map; }

            public void Dispose()
            {
                map.FinishWrite();
            }
        }

        public WriteReleaser Write()
        {
            Debug.Assert(!mapDirty);
            maplock.AcquireWriterLock(-1);
            return new WriteReleaser(this); // the writeReleaser calls FinishWrite when it is Disposed.
        }

        private void FinishWrite() {
            bool dirty = mapDirty;
            bool defsDirty = symdefsDirty;

            mapDirty = false;
            symdefsDirty = false;
            maplock.ReleaseWriterLock();
            if (dirty) {
                boundsAccurate = false;
                MapChanged();
            }
            if (defsDirty && OnSymdefsChanged != null)
                OnSymdefsChanged(this);
        }

        public struct ReadReleaser: IDisposable
        {
            private Map map;

            public ReadReleaser(Map map) { this.map = map; }

            public void Dispose()
            {
                map.FinishRead();
            }
        }

        public ReadReleaser Read()
        {
            Debug.Assert(!mapDirty && !symdefsDirty);
            maplock.AcquireReaderLock(-1);
            return new ReadReleaser(this); // the writeReleaser calls FinishWrite when it is Disposed.
        }

        private void FinishRead()
        {
            Debug.Assert(!mapDirty && !symdefsDirty);
            maplock.ReleaseReaderLock();
        }

        internal void SetDirty()
        {
            mapDirty = true;
        }

        public Map(ITextMetrics textMetricsProvider)
        {
            this.textMetricsProvider = textMetricsProvider;

            using (Write()) {
                Clear();
            }
        }

        private void TraceLine(string format, params object[] args)
        {
            Trace.WriteLine(string.Format(format, args), "Map");
        }

        internal void CheckWritable()
        {
            if (! maplock.IsWriterLockHeld) 
                throw new MapUsageException("Cannot modify map without calling Map.Write");
        }

        void CheckReadable()
        {
            // CONSIDER: is allowing reading while holding the writer lock too lenient?
            if (! maplock.IsReaderLockHeld && ! maplock.IsWriterLockHeld) 
                throw new MapUsageException("Cannot read map without calling Map.Read or Map.Write");
        }

        public void Clear()
        {
            CheckWritable();
            colors.Clear();
            symdefs.Clear();
            symbols.Clear();
            SetDirty();
            symdefsDirty = true;
        }

        void MapChanged() {
            if (OnMapChanged != null)
                OnMapChanged(this);
        }

        internal ITextMetrics TextMetricsProvider
        {
            get
            {
                return textMetricsProvider;
            }
        }

        public ICollection<SymDef> AllSymdefs {
            get { 
            CheckReadable(); 
            return symdefs; 
            }
        }

        public ICollection<Symbol> AllSymbols {
            get { 
                CheckReadable(); 
                return symbols; 
            }
        }

        public ICollection<SymColor> AllColors {
            get {
                CheckReadable();
                return colors;
            }
        }

        public float MapScale
        {
            get
            {
                CheckReadable();
                return mapScale;
            }
            set
            {
                CheckWritable();
                mapScale = value;
            }
        }

        public float PrintScale
        {
            get
            {
                CheckReadable();
                return printScale;
            }
            set
            {
                CheckWritable();
                printScale = value;
            }
        }

        // The print area. A 0,0,0,0 rectangle is used to indicated no defined area, which generally means print the whole map.
        public RectangleF PrintArea
        {
            get
            {
                CheckReadable();
                return printArea;
            }
            set
            {
                CheckWritable();
                printArea = value;
            }
        }

        // Get information about the template associated with the map. Only one template 
        // is currently supported.
        public TemplateInfo Template
        {
            get
            {
                return template;
            }
            set
            {
                template = value;
            }
        }

        // CONSIDER: why doesn't AddColor just take a SymColor? This seems inconsistent with the way the
        // other methods work.

        private bool EqualColorMatrix(ColorMatrix mat1, ColorMatrix mat2)
        {
            if (mat1 == null)
                return (mat2 == null);
            else if (mat2 == null)
                return (mat1 == null);
            else {
                for (int i = 0; i < 5; ++i)
                    for (int j = 0; j < 5; ++j)
                        if (mat1[i, j] != mat2[i, j])
                            return false;

                return true;
            }
        }

        // The ColorMatrix transforms the colors before rendering. This is useful, for example, to draw
        // in a lighted fashion or similar.
        public ColorMatrix ColorMatrix {
            get {
                return colorMatrix;
            }

            set {
                CheckWritable();

                if (EqualColorMatrix(colorMatrix, value))
                    return;         // no change in matrix.

                this.colorMatrix = value;

                // Free GDI objects that might be using the old colors. They are recreated on demand.
                foreach (SymColor symColor in colors) 
                    symColor.FreeGdiObjects();
                foreach (SymDef symdef in symdefs)
                    symdef.FreeGdiObjects();
            }
        }

        // Transform a given color value by the ColorMatrix, if any. 
        public Color TransformColor(Color colorIn)
        {
            if (colorMatrix == null)
                return colorIn;
            else {
                float redIn = colorIn.R / 255.0F;
                float greenIn = colorIn.G / 255.0F;
                float blueIn = colorIn.B / 255.0F;

                float redOut = redIn * colorMatrix[0, 0] + greenIn * colorMatrix[1, 0] + blueIn * colorMatrix[2,0] + colorMatrix[4,0];
                float greenOut = redIn * colorMatrix[0, 1] + greenIn * colorMatrix[1, 1] + blueIn * colorMatrix[2, 1] + colorMatrix[4, 1];
                float blueOut = redIn * colorMatrix[0, 2] + greenIn * colorMatrix[1, 2] + blueIn * colorMatrix[2, 2] + colorMatrix[4, 2];

                int redByte = (int) Math.Round(redOut * 255.0);
                int greenByte = (int) Math.Round(greenOut * 255.0);
                int blueByte = (int) Math.Round(blueOut * 255.0);

                return Color.FromArgb(255, (byte)redByte, (byte)greenByte, (byte)blueByte);
            }
        }

        public SymColor AddColor(string name, short ocadId, float cyan, float magenta, float yellow, float black)
        {
            CheckWritable();
            SymColor color = new SymColor();
            color.Name = name;
            color.OcadId = ocadId;
            color.SetCMYK(cyan, magenta, yellow, black);
            colors.Add(color);
            color.SetMap(this);
            symdefsDirty = true;
            return color;
        }

        public SymColor AddColorBottom(string name, short ocadId, float cyan, float magenta, float yellow, float black)
        {
            CheckWritable();
            SymColor color = new SymColor();
            color.Name = name;
            color.OcadId = ocadId;
            color.SetCMYK(cyan, magenta, yellow, black);
            colors.Insert(0, color);
            color.SetMap(this);
            symdefsDirty = true;
            return color;
        }

        public SymColor AddColor(string name, short ocadId, float red, float green, float blue)
        {
            CheckWritable();
            SymColor color = new SymColor();
            color.Name = name;
            color.OcadId = ocadId;
            color.SetRGB(red, green, blue);
            colors.Add(color);
            color.SetMap(this);
            symdefsDirty = true;
            return color;
        }


        public void AddSymbol(Symbol sym) {
            // TODO: should invalidate smaller region of map.
            CheckWritable();
            symbols.Add(sym);
            sym.SetMap(this);
            sym.Definition.AddSymbol(sym);
            SetDirty();
        }

        public void RemoveSymbol(Symbol sym) {
            // TODO: should invalidate smaller region of map.
            CheckWritable();
            Debug.Assert(symbols.Contains(sym));
            symbols.Remove(sym);
            sym.Definition.RemoveSymbol(sym);
            sym.SetMap(null);
            SetDirty();
        }

        public void AddSymdef(SymDef symdef) {
            CheckWritable();
            symdefs.Add(symdef);
            symdef.SetMap(this);

            if (symdef.DependsOnSymdef != null) {
                Debug.Assert(symdef.DependsOnSymdef.ContainingMap == this);
                symdef.DependsOnSymdef.AddDependentSymdef(symdef);
            }

            symdefsDirty = true;
        }

        // Remove a symdef from the map. The symdef must not be in use by any symbol.
        public void RemoveSymdef(SymDef symdef) {
            CheckWritable();
            Debug.Assert(symdef.symbols.Count == 0); // No symbols may be used this symdef.
            Debug.Assert(symdef.dependentSymdefs.Count == 0); // No symdefs may be dependent on this symdef.

            if (symdef.DependsOnSymdef != null)
                symdef.DependsOnSymdef.RemoveDependentSymdef(symdef);

            symdefs.Remove(symdef);
            symdefsDirty = true;
        }

        // Find the symdef with the given ID, or return null if none found.
        public SymDef SymdefFromOcadID(int ocadID) {
            CheckReadable();

            foreach (SymDef symdef in symdefs) {
                if (symdef.OcadID == ocadID)
                    return symdef;
            }

            return null;
        }

        // Get a free symdef OCAD ID, starting the search with a given integer part.
        // Always creates an ID that is OCAD 6/7/8 compatible.
        public int GetFreeSymdefOcadId(int integerPart)
        {
            int ocadId = integerPart * 1000;

            while (SymdefFromOcadID(ocadId) != null) {
                ++ocadId;
                if (ocadId % 1000 >= 10)
                    ocadId = ((ocadId / 1000) + 1) * 1000;
            }

            return ocadId;
        }

        // Find the color with the given ID, or return null if none found.
        public SymColor SymColorFromOcadID(int ocadID) {
            CheckReadable();

            foreach (SymColor color in colors) {
                if (color.OcadId == ocadID)
                    return color;
            }

            return null;
        }

        // Determine which symdefs use a color.
        public SymDef[] SymdefsUsingColor(SymColor color) {
            CheckReadable();
            List<SymDef> list = new List<SymDef>();

            foreach (SymDef symdef in symdefs) {
                if (symdef.HasColor(color)) 
                    list.Add(symdef);
            }

            if (list.Count > 0) 
                return list.ToArray();
            else
                return null;
        }

        // Set if a symdef is visible.
        public void SetSymdefVisible(SymDef symdef, bool isVisible)
        {
            CheckWritable();
            Debug.Assert(symdefs.Contains(symdef));

            hiddenSymbols[symdef] = isVisible;
        }

        // Decide if a symdef is visible.
        public bool IsSymdefVisible(SymDef symdef)
        {
            bool isVisible;

            CheckReadable();
            Debug.Assert(symdefs.Contains(symdef));

            // Not present in the hiddenSymbols dictionary means it is visible.
            if (hiddenSymbols.TryGetValue(symdef, out isVisible) && !isVisible)
                return false;
            else
                return true;
        }

        // Get the bounds of all symbols on the map.
        public RectangleF Bounds
        {
            get
            {
                CheckReadable();

                if (!boundsAccurate) {
                    // Recalculate bounds by unioning all the bounding boxes of every symbol.
                    bool first = true;
                    foreach (Symbol sym in AllSymbols) {
                        if (first)
                            bounds = sym.BoundingBox;
                        else
                            bounds = RectangleF.Union(bounds, sym.BoundingBox);

                        first = false;
                    }

                    if (first)
                        bounds = new RectangleF();       // no symbols: empty rectangle.

                    boundsAccurate = true;
                }

                return bounds;
            }
        }

#if WPF
        public void Draw(System.Windows.Media.DrawingContext dc, RectangleF rect, RenderOptions renderOpts)
        {
            CheckReadable();

            TraceLine("Begin drawing rectangle ({0},{1})-({2},{3})", rect.Left, rect.Top, rect.Right, rect.Bottom);
            Trace.Indent();

            GraphicsTarget grTarget = new GraphicsTarget(dc);

            if (renderOpts.showSymbolBounds)
                boundsPen = GraphicsUtil.CreateSolidPen(grTarget, Color.FromArgb(100, 255, 0, 0), 0.01F, LineStyle.Mitered);

            // Draw the image layer.
            DrawColor(grTarget, null, rect, true, renderOpts);

            // Draw each color separately, to get correct layering.
            foreach (SymColor curColor in colors) {
                DrawColor(grTarget, curColor, rect, true, renderOpts);
            }

            if (renderOpts.showSymbolBounds)
                boundsPen.Dispose();

            Trace.Unindent();
        }

#else

        // Draw the elements of the map that lie within the rectange rect
        // into the Graphics g, which already has its world coordinates set up
        // correctly and the clipping region.
        public void Draw(System.Drawing.Graphics g, RectangleF rect, RenderOptions renderOpts)
        {
            CheckReadable();

            TraceLine("Begin drawing rectangle ({0},{1})-({2},{3})", rect.Left, rect.Top, rect.Right, rect.Bottom);
            Trace.Indent();

            System.Drawing.Drawing2D.GraphicsState graphicsState = g.Save();
            GraphicsTarget grTarget = new GraphicsTarget(g);
            try {
                // Get clipping region and bounding rectangle. If the clipping region isn't a rectangle,
                // then it is worth doing extra work to eliminate symbols.
                // UNDONE: clipRegionIsRectangle is given false on full repaint. Need to fix this.
                System.Drawing.Region clipRegion;
                rect.Intersect(g.ClipBounds);
                clipRegion = g.Clip.Clone();
                clipRegion.Xor(rect);
                bool clipRegionIsRectangle = clipRegion.IsEmpty(g);
                clipRegion.Dispose();

                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                if (renderOpts.showSymbolBounds)
                    boundsPen = grTarget.CreatePen(Color.FromArgb(100, Color.Red), 0, LineCap.Flat, LineJoin.Miter, 4);

                // Draw the image layer.
                DrawColor(grTarget, null, rect, clipRegionIsRectangle, renderOpts);

                // Draw each color separately, to get correct layering.
                foreach (SymColor curColor in colors) {
                    DrawColor(grTarget, curColor, rect, clipRegionIsRectangle, renderOpts);
                }

                if (renderOpts.showSymbolBounds)
                    boundsPen.Dispose();

                Trace.Unindent();
            }
            finally {
                g.Restore(graphicsState);
            }
        }

#endif

        // Draw a particular color layer. If curColor is null, draw the image layer. 
        private void DrawColor(GraphicsTarget g, SymColor curColor, RectangleF rect, bool clipRegionIsRectangle, RenderOptions renderOpts)
        {
            foreach (SymDef symdef in symdefs) {
                if (IsSymdefVisible(symdef) && symdef.HasColor(curColor)) {
                    foreach (Symbol curSym in symdef.symbols) {
                        // Only draw the symbol if it may intersect. Check
                        // the bounding box first as it's faster exclusion than MayIntersectRect.
                        RectangleF bounds = curSym.BoundingBox;
                        if (bounds.IntersectsWith(rect) &&
#if !WPF
                            (clipRegionIsRectangle || g.Graphics.IsVisible(Util.InflateRect(bounds, renderOpts.minResolution))) &&
#endif
                            curSym.MayIntersectRect(rect)) 
                        {
                            curSym.Draw(g, curColor, renderOpts);

                            if (renderOpts.showSymbolBounds)
                                g.DrawRectangle(boundsPen, bounds);
                        }
                    }
                }
            }

            //TraceLine("Drawing color {0}: drew {1} of {2} symbols.", curColor, cDrawn, cSymbols);
        }

        // Find all symbols that are hit within the given distance of point. 
        public SymbolHit[] HitTest(PointF point, float distance)
        {
            CheckReadable();

            RectangleF testBox = new RectangleF(point.X - distance, point.Y - distance, distance * 2, distance * 2);
            List<SymbolHit> list = new List<SymbolHit>();
            float actualDistance;

            foreach (Symbol curSym in symbols) {
                if (curSym.BoundingBox.IntersectsWith(testBox) && 
                    curSym.MayIntersectRect(testBox)) 
                {
                    if (curSym.HitTest(point, distance, out actualDistance)) {
                        SymbolHit hit;
                        hit.symbol = curSym;
                        hit.distance = actualDistance;
                        list.Add(hit);
                    }
                }
            }

            if (list.Count == 0)
                return null;
            else 
                return list.ToArray();
        }
    }

    // This class allows controlling rendering options.
    public class RenderOptions
    {
        public float minResolution;  // minimum size of a feature to render (typically the pixel size)
        public bool usePatternBitmaps; // if true, always use bitmap glyphs to render area patterns, if false, use direct drawing and clipping

        // debug options.
        public bool showSymbolBounds;      // Show the bounds of symbols.
    }
}
