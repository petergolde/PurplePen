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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Diagnostics;
using PurplePen.MapModel;
using InteractiveTestApp.MapView;
using ColorMatrix = PurplePen.MapModel.ColorMatrix;
using PurplePen.Graphics2D;
using System.IO;


namespace InteractiveTestApp.MapDisplayer
{
    public enum MapType { None, Bitmap, OCAD };

    // The map display represents everything that is cached in the ViewCache and normally shown
    // on the map. It includes the map proper, as well as the courses and so forth drawn on top.
    // The IMapDisplay interface is the communication channel with the ViewCache and the MapViewer
    // controls -- it simply has to be able to draw itself, and notify when parts change.
    class MapDisplay: IMapDisplay
    {
        MapType mapType;
        string filename;

        GDIPlus_Bitmap bitmap;     // the bitmap
        GDIPlus_Bitmap dimmedBitmap;  // the dimmed bitmap.
        float bitmapDpi;     // dpi for bitmap

        MapFileFormat mapVersion;       // OCAD version. (OCAD/OpenMapper maps only)
        Map map;                    // The map to draw. (OCAD/OpenMapper maps only)

        double mapIntensity = 1.0;   // Intensity to display the map at.
        bool antialiased = false;        // anti-alias (high quality) the map display?
        bool showBounds = false;       // show symbols bounds (for testing)
        bool showTemplates = false;    // show templates.
        bool overprinting = false;     // overprinting effect

        // If Line >= 0 show insertion point at this coordinate, or -1 for dont show.
        TextCoord insertionPointToShow = new TextCoord(-1, 0); 

        // Clones this map display.
        public MapDisplay Clone()
        {
            MapDisplay newMapDisplay = (MapDisplay) MemberwiseClone();

            newMapDisplay.dimmedBitmap = null;         // clones should not share dimmed bitmaps.
            newMapDisplay.UpdateDimmedBitmap();

            return newMapDisplay;
        }

        // Map type we're drawing.
        public MapType MapType
        {
            get
            {
                return mapType;
            }
        }

        // File name of the map.
        public string FileName
        {
            get
            {
                return filename;
            }
        }

        // OCAD version of the map.
        public MapFileFormat MapVersion
        {
            get
            {
                return mapVersion;
            }
        }

        // Scale of the map
        public float MapScale
        {
            get
            {
                if (map != null) {
                    using (map.Read())
                        return map.MapScale;
                }
                else
                    return 0;
            }
        }

        // Dpi of the bitmap
        public float Dpi
        {
            get
            {
                if (mapType == MapType.Bitmap)
                    return bitmapDpi;
                else
                    return 0;
            }

            set
            {
                bitmapDpi = value;
                RaiseChanged(null);        // redraw everything.
            }
        }

        public TextCoord InsertionPointToShow
        {
            get { return insertionPointToShow; }
            set
            {
                insertionPointToShow = value;
                RaiseChanged(null); // redraw.
            }
        }

        // Bounds of the map, or empty if no map.
        public RectangleF MapBounds
        {
            get
            {
                switch (mapType) {
                case MapType.Bitmap:
                    return Geometry.TransformRectangle(BitmapTransform(), new RectangleF(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));

                case MapType.OCAD:
                    if (map != null) {
                        using (map.Read())
                            return map.Bounds;
                    }
                    else
                        return new RectangleF();

                case MapType.None:
                    return new RectangleF();

                default:
                    Debug.Fail("bad maptype");
                    return new RectangleF();
                }
            }
        }


        // Colors in the map, or empty list if no map or bitmap map.
        public List<SymColor> GetMapColors()
        {
            if (mapType == MapType.OCAD && map != null) {
                using (map.Read())
                    return new List<SymColor>(map.AllColors);
            }
            else
                return new List<SymColor>();
        }

        // Intensity to draw the map at.
        public double MapIntensity
        {
            get
            {
                return mapIntensity;
            }

            set
            {
                if (MapIntensity != value) {
                    mapIntensity = value;
                    UpdateDimmedBitmap();
                    RaiseChanged(null);
                }
            }
        }

        // Missing fonts?
        public string[] MissingFonts()
        {
            if (mapType == MapType.OCAD)
                return map.MissingFonts;
            else
                return null;
        }

        // Nonrenderable objects?
        public string[] NonRenderableObjects()
        {
            if (mapType == MapType.OCAD)
                return map.NotRenderableObjects;
            else
                return null;
        }

        // Anti-alias the map display?
        public bool AntiAlias
        {
            get
            {
                return antialiased;
            }
            set
            {
                if (antialiased != value) {
                    antialiased = value;
                    RaiseChanged(null);
                }
            }
        }

        // Are we printing?
        public bool Printing { get; set; }

        // Anti-alias the map display?
        public bool ShowSymbolBounds
        {
            get
            {
                return showBounds;
            }
            set
            {
                if (showBounds != value) {
                    showBounds = value;
                    RaiseChanged(null);
                }
            }
        }

        public bool ShowTemplates
        {
            get
            {
                return showTemplates;
            }
            set
            {
                if (showTemplates != value) {
                    showTemplates = value;
                    RaiseChanged(null);
                }
            }
        }

        public bool Overprinting
        {
            get
            {
                return overprinting;
            }
            set
            {
                if (overprinting != value) {
                    overprinting = value;
                    RaiseChanged(null);
                }
            }
        }

        // Get a color matrix from the current map intensity value. Can return null if intensity is 1.0.
        ColorMatrix ComputeColorMatrix()
        {
            if (mapIntensity < 0.99F) {
                float[][] colorMatrixElements = { 
                           new float[] {(float)mapIntensity,  0,  0,  0, 0},
                           new float[] {0,  (float)mapIntensity,  0,  0, 0},
                           new float[] {0,  0,  (float)mapIntensity,  0, 0},
                           new float[] {0,  0,  0,  1, 0},
                           new float[] {(float) (1-mapIntensity), (float) (1-mapIntensity), (float) (1-mapIntensity), 0, 1}};
                ColorMatrix colorMatrix = new ColorMatrix(colorMatrixElements);
                return colorMatrix;
            }
            else
                return null;            // full intensity.
        }

        // Computer the transform from coordinates of the bitmap to world coordinates
        Matrix BitmapTransform()
        {
            // (worldcoord in mm) / 25.4F * dpi = pixels
            float scaleFactor = bitmapDpi / 25.4F;

            Matrix matrix = new Matrix();
            matrix.Translate(0, bitmap.PixelHeight);
            matrix.Scale(scaleFactor, -scaleFactor);
            matrix.Invert();
            return matrix;
        }

        // Set the map file used to draw. 
        public void SetMapFile(MapType mapType, string filename)
        {
            this.mapType = mapType;
            this.filename = filename;
            this.bitmap = null;
            this.map = null;

            if (mapType == MapType.None) {
                map = null;
                mapVersion = new MapFileFormat(MapFileFormatKind.None, 0);
                bitmap = null;
            }
            else if (mapType == MapType.OCAD) {
                map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(Path.GetDirectoryName(filename)));
                mapVersion = InputOutput.ReadFile(filename, map);
                bitmap = null;
            }
            else if (mapType == MapType.Bitmap) {
                map = null;
                mapVersion = new MapFileFormat(MapFileFormatKind.None, 0);
                Bitmap bm = (Bitmap)Image.FromFile(filename);
                bitmap = new GDIPlus_Bitmap(bm);
                bitmapDpi = bm.HorizontalResolution;
            }
            else {
                Debug.Fail("bad maptype");
                mapVersion = new MapFileFormat(MapFileFormatKind.None, 0);
                mapType = MapType.None;
            }

            UpdateDimmedBitmap();
            RaiseChanged(null);        // redraw everything.
        }

        // Update the dimmed bitmap. If the intensity is < 1 and we're using a bitmap, dim it.
        public void UpdateDimmedBitmap()
        {
            if (dimmedBitmap != null) {
                dimmedBitmap.Dispose();
                dimmedBitmap = null;
            }

            if (mapType == MapType.Bitmap && mapIntensity < 0.99F) {
                Bitmap dimmed = new Bitmap(bitmap.PixelWidth, bitmap.PixelHeight);
                Graphics g = Graphics.FromImage(dimmed);
                ImageAttributes imageAttributes = new ImageAttributes();
                imageAttributes.SetColorMatrix(ComputeColorMatrix());
                g.DrawImage(bitmap.Bitmap, new Rectangle(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), 0, 0, bitmap.PixelWidth, bitmap.PixelHeight, GraphicsUnit.Pixel, imageAttributes);
                g.Dispose();

                dimmedBitmap = new GDIPlus_Bitmap(dimmed);
            }
            else {
                dimmedBitmap = null;
            }
        }

        // Draw the ocad map part.
        void DrawOcadMap(IGraphicsTarget grTarget, RectangleF visRect, RenderOptions renderOptions)
        {
            using (map.Write()) {
                map.ColorMatrix = ComputeColorMatrix();
                map.Draw(grTarget, visRect, renderOptions, null);

                if (insertionPointToShow.Line >= 0)
                    DrawOcadMapInsertionPoints(grTarget, visRect, renderOptions);
            }
        }

        private void DrawOcadMapInsertionPoints(IGraphicsTarget grTarget, RectangleF visRect, RenderOptions renderOptions)
        {
            object pen = new object();
            grTarget.CreatePen(pen, CmykColor.FromRgb(1, 0, 0), renderOptions.minResolution * 2, LineCap.Flat, LineJoin.Round, 5);

            foreach (Symbol sym in map.AllSymbols) {
                if (sym is TextSymbol) {
                    TextSymbol textSym = (TextSymbol)sym;

                    TextSymDef.InsertionPointLocation loc = textSym.FindInsertionPoint(insertionPointToShow);
                    if (loc != null)
                        grTarget.DrawLine(pen, loc.Ascent, loc.Descent);
                }
            }
        }

        // Draw the bitmap map part.
        void DrawBitmapMap(IGraphicsTarget grTarget, RectangleF visRect)
        {
            // Setup transform.
            grTarget.PushTransform(BitmapTransform());

            // Setup drawing map and intensity.
            BitmapScaling scalingMode = antialiased ? BitmapScaling.MediumQuality : BitmapScaling.NearestNeighbor;

            // Get source bitmap. Use the dimmed bitmap if there is one.
            IGraphicsBitmap sourceBitmap;
            if (dimmedBitmap != null)
                sourceBitmap = dimmedBitmap;
            else
                sourceBitmap = bitmap;

            // Draw it.
            grTarget.DrawBitmap(sourceBitmap, new RectangleF(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), scalingMode, 0.01F);

            // Pop transform
            grTarget.PopTransform();
        }


        // Draw the map and course onto a graphics.
        public void Draw(IGraphicsTarget grTarget, RectangleF visRect, float minResolution)
        {
            RenderOptions renderOptions = new RenderOptions();
            renderOptions.minResolution = minResolution;

            if (Printing)
                renderOptions.usePatternBitmaps = false;   // don't use pattern bitmaps when printing, they cause some problems in some printer drivers and we want best quality.
            else if (antialiased && minResolution < 0.007F)  // use pattern bitmaps unless high quality and zoomed in very far
                renderOptions.usePatternBitmaps = false;
            else
                renderOptions.usePatternBitmaps = true;

            renderOptions.showSymbolBounds = showBounds;
            renderOptions.renderTemplates = showTemplates ? RenderTemplateOption.MapAndTemplates : RenderTemplateOption.MapOnly;
            renderOptions.blendOverprintedColors = overprinting;

            if (Printing)
                grTarget.PushAntiAliasing(false);       // don't anti-alias on printer
            else
                grTarget.PushAntiAliasing(antialiased);
            

            // First draw the real map.
            switch (mapType) {
            case MapType.OCAD:
                DrawOcadMap(grTarget, visRect, renderOptions);
                break;

            case MapType.Bitmap:
                DrawBitmapMap(grTarget, visRect);
                break;

            case MapType.None:
                object brushKey = new object();
                grTarget.CreateSolidBrush(brushKey, CmykColor.FromRgb(1, 1, 1));
                grTarget.FillRectangle(brushKey, visRect);
                break;
            }

            grTarget.PopAntiAliasing();
        }

        public event MapDisplayChanged Changed;

        // Raise the changed event.
        private void RaiseChanged(Region region)
        {
            if (Changed != null)
                Changed(region);
        }
    }
}
