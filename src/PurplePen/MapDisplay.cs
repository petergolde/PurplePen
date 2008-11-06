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
using PurplePen.MapView;


namespace PurplePen
{
    // The map display represents everything that is cached in the ViewCache and normally shown
    // on the map. It includes the map proper, as well as the courses and so forth drawn on top.
    // The IMapDisplay interface is the communication channel with the ViewCache and the MapViewer
    // controls -- it simply has to be able to draw itself, and notify when parts change.
    class MapDisplay: IMapDisplay
    {
        MapType mapType;
        string filename;

        Bitmap bitmap;     // the bitmap
        Bitmap dimmedBitmap;  // the dimmed bitmap.
        float bitmapDpi;     // dpi for bitmap

        int mapVersion;       // OCAD version. (OCAD maps only)
        Map map;                // The map to draw. (OCAD maps only)

        CourseLayout course;    // The course to display.
        Map courseMap;              // The courses, rendered into a map.

        double mapIntensity = 1.0;   // Intensity to display the map at.
        bool antialiased = false;        // anti-alias (high quality) the map display?
        bool showBounds = false;       // show symbols bounds (for testing)

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
        public int MapVersion
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

        // Bounds of the map, or empty if no map.
        public RectangleF MapBounds
        {
            get
            {
                switch (mapType) {
                case MapType.Bitmap:
                    return Util.TransformRectangle(new RectangleF(0, 0, bitmap.Width, bitmap.Height), BitmapTransform());

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
            matrix.Translate(0, bitmap.Height);
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
                mapVersion = 0;
                bitmap = null;
            }
            else if (mapType == MapType.OCAD) {
                map = new Map();
                mapVersion = InputOutput.ReadFile(filename, map);
                bitmap = null;
            }
            else if (mapType == MapType.Bitmap) {
                map = null;
                mapVersion = 0;
                bitmap = (Bitmap) Image.FromFile(filename);
                bitmapDpi = bitmap.HorizontalResolution;
            }
            else {
                Debug.Fail("bad maptype");
            }

            UpdateDimmedBitmap();
            RaiseChanged(null);        // redraw everything.
        }


        // Set the courses being displayed.
        public void SetCourse(CourseLayout newCourse)
        {
            if (! object.Equals(course, newCourse)) {
                course = newCourse;
                if (course == null)
                    courseMap = null;
                else
                    courseMap = course.RenderToMap();

                RaiseChanged(null);
            }
        }

        // Update the dimmed bitmap. If the intensity is < 1 and we're using a bitmap, dim it.
        public void UpdateDimmedBitmap()
        {
            if (dimmedBitmap != null)
                dimmedBitmap.Dispose();

            if (mapType == MapType.Bitmap && mapIntensity < 0.99F) {
                dimmedBitmap = new Bitmap(bitmap.Width, bitmap.Height);
                Graphics g = Graphics.FromImage(dimmedBitmap);
                ImageAttributes imageAttributes = new ImageAttributes();
                imageAttributes.SetColorMatrix(ComputeColorMatrix());
                g.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height), 0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, imageAttributes);
                g.Dispose();
            }
            else {
                dimmedBitmap = null;
            }
        }


        // Draw the ocad map part.
        void DrawOcadMap(Graphics g, RectangleF visRect, RenderOptions renderOptions)
        {
            using (map.Write()) {
                map.ColorMatrix = ComputeColorMatrix();
                map.Draw(g, visRect, renderOptions);
                map.ColorMatrix = null;
            }
        }

        // Draw the bitmap map part.
        void DrawBitmapMap(Graphics g, RectangleF visRect)
        {
            GraphicsState savedState = g.Save();

            try {
                // Setup clipping and transform.
                g.IntersectClip(visRect);
                g.MultiplyTransform(BitmapTransform(), MatrixOrder.Prepend);

                // Setup drawing map and intensity.
                g.InterpolationMode = antialiased ? InterpolationMode.HighQualityBilinear : InterpolationMode.NearestNeighbor;

                // Get source bitmap. Use the dimmed bitmap if there is one.
                Bitmap sourceBitmap;
                if (dimmedBitmap != null)
                    sourceBitmap = dimmedBitmap;
                else
                    sourceBitmap = bitmap;

                // Draw it.
                g.DrawImage(sourceBitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height), 0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel);
            }
            finally {
                g.Restore(savedState);
            }
        }

        // Draw the map and course onto a graphics.
        public void Draw(Graphics g, RectangleF visRect, float minResolution)
        {
            RenderOptions renderOptions = new RenderOptions();
            renderOptions.minResolution = (antialiased) ? (minResolution / 2) : (minResolution);
            renderOptions.forceBitmapGlyphs = false;
            renderOptions.showSymbolBounds = showBounds;

            if (antialiased)
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            else
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;


            // First draw the real map.
            switch (mapType) {
            case MapType.OCAD:
                DrawOcadMap(g, visRect, renderOptions);
                break;

            case MapType.Bitmap:
                DrawBitmapMap(g, visRect);
                break;

            case MapType.None:
                g.Clear(Color.White);
                break;
            }

            // Now draw the courseMap on top.
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;   // always anti-alias the course.
            if (courseMap != null) {
                using (courseMap.Read())
                    courseMap.Draw(g, visRect, renderOptions);
            }
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
