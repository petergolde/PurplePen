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

#pragma warning disable 659

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using Matrix = System.Drawing.Drawing2D.Matrix;
using LineJoin = System.Drawing.Drawing2D.LineJoin;
using LineCap = System.Drawing.Drawing2D.LineCap;
using Color = System.Drawing.Color;

namespace PurplePen.MapModel
{
    using PurplePen.Graphics2D;
    using System.Drawing.Drawing2D;

    // A color matrix
    public class ColorMatrix: ICloneable
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

#if WINDOWS
        public static implicit operator System.Drawing.Imaging.ColorMatrix(ColorMatrix matrix) {
            return new System.Drawing.Imaging.ColorMatrix(matrix.entries);
        }
#endif

        public object Clone()
        {
            return new ColorMatrix((float[][]) entries.Clone());
        }

        public static bool Equal(ColorMatrix mat1, ColorMatrix mat2)
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
        public int layer;   // what layer (0 is top)

        public override string ToString()
        {
            return string.Format("SymbolHit[symid={0}, dist={1:R}, layer={2}]", symbol.Definition.SymbolId, distance, layer);
        }

        internal Map.SymbolHitOrder HitOrder
        {
            get {
                return symbol.HitOrder;
            }
        }
    }

    // Information about a template.
    public class TemplateInfo: ICloneable
    {
        public readonly string absoluteFileName;                    // absolute file name to template. Relativized on write/made absolute when read.
        public readonly PointF centerPoint;                         // center point of the template.
        public readonly float dpi;                                  // dpi of the template (if a bitmap)
        public readonly float angle;                                // angle in degrees of the template.
        public readonly float scaleX = 1.0F, scaleY = 1.0F;         // scaling in X/Y direction
        public readonly float shearAngle;                           // shear angle
        public readonly bool visible;                               // is the template currently visible?
        public readonly bool drawAboveMap;                          // Indicates that it is drawn above the map.
        public readonly IFileLoader fileLoader;                     // if non-null, overrides the file loaded in the map; if null, use file loader from map.

        public TemplateInfo(string absoluteFileName, PointF centerPoint, float dpi, float angle, bool visible)
        {
            this.absoluteFileName = absoluteFileName;
            this.centerPoint = centerPoint;
            this.dpi = dpi;
            this.angle = this.shearAngle = angle;
            this.visible = visible;
        }

        public TemplateInfo(string absoluteFileName, PointF centerPoint, float dpi, float angle, bool visible, IFileLoader fileLoader)
        {
            this.absoluteFileName = absoluteFileName;
            this.centerPoint = centerPoint;
            this.dpi = dpi;
            this.angle = this.shearAngle = angle;
            this.visible = visible;
            this.fileLoader = fileLoader;
        }

        public TemplateInfo(string absoluteFileName, PointF centerPoint, float dpi, float angle, float scaleX, float scaleY, float shearAngle, bool visible)
        {
            this.absoluteFileName = absoluteFileName;
            this.centerPoint = centerPoint;
            this.dpi = dpi;
            this.angle = angle;
            this.scaleX = scaleX;
            this.scaleY = scaleY;
            this.shearAngle = shearAngle;
            this.visible = visible;
        }

        public TemplateInfo(string absoluteFileName, PointF centerPoint, float dpi, float angle, float scaleX, float scaleY, float shearAngle, bool visible, IFileLoader fileLoader)
        {
            this.absoluteFileName = absoluteFileName;
            this.centerPoint = centerPoint;
            this.dpi = dpi;
            this.angle = angle;
            this.scaleX = scaleX;
            this.scaleY = scaleY;
            this.shearAngle = shearAngle;
            this.visible = visible;
            this.fileLoader = fileLoader;
        }

        public TemplateInfo(string absoluteFileName, PointF centerPoint, float dpi, float angle, float scaleX, float scaleY, float shearAngle, bool visible, IFileLoader fileLoader, bool drawAboveMap)
        {
            this.absoluteFileName = absoluteFileName;
            this.centerPoint = centerPoint;
            this.dpi = dpi;
            this.angle = angle;
            this.scaleX = scaleX;
            this.scaleY = scaleY;
            this.shearAngle = shearAngle;
            this.visible = visible;
            this.fileLoader = fileLoader;
            this.drawAboveMap = drawAboveMap;
        }

        public TemplateInfo(string absoluteFileName, float[] transformElements, bool visible, IFileLoader fileLoader, bool drawAboveMap)
        {
            float angle, skew, scaleX, scaleY;
            PointF translate;

            GetTransformParameters(transformElements, out angle, out skew, out scaleX, out scaleY, out translate);

            this.absoluteFileName = absoluteFileName;
            this.centerPoint = translate;
            this.dpi = 25.4F / scaleX;
            this.angle = angle;
            this.scaleX = 1; // uses DPI for scaling instead.
            this.scaleY = - scaleY / scaleX;  // negative because that's how world file parameters work.
            this.shearAngle = skew;
            this.visible = visible;
            this.fileLoader = fileLoader;
            this.drawAboveMap = drawAboveMap;
        }

        public TemplateInfo UpdateFileName(string newFileName)
        {
            return new TemplateInfo(newFileName, centerPoint, dpi, angle, scaleX, scaleY, shearAngle, visible, fileLoader, drawAboveMap);
        }

        public TemplateInfo UpdateVisible(bool newVisible)
        {
            return new TemplateInfo(absoluteFileName, centerPoint, dpi, angle, scaleX, scaleY, shearAngle, newVisible, fileLoader, drawAboveMap);
        }

        // Calculate the transform from bitmap coordinate to map coordinates.
        internal Matrix CalculateTransform()
        {
            Matrix translate = new Matrix();
            translate.Translate(centerPoint.X, centerPoint.Y);

            Matrix rotate = new Matrix();
            rotate.Rotate(angle);

            Matrix scale = new Matrix();
            scale.Scale(scaleX * 25.4F / dpi, - scaleY * 25.4F / dpi);

            Matrix shear = new Matrix();
            double theta = (angle - shearAngle) * Math.PI / 180;
            shear.Scale(1, (float)Math.Cos(theta));
            shear.Shear((float)Math.Sin(theta), 0);

            Matrix result = new Matrix();
            result.Multiply(translate, System.Drawing.Drawing2D.MatrixOrder.Prepend);
            result.Multiply(rotate, System.Drawing.Drawing2D.MatrixOrder.Prepend);
            result.Multiply(shear, System.Drawing.Drawing2D.MatrixOrder.Prepend);
            result.Multiply(scale, System.Drawing.Drawing2D.MatrixOrder.Prepend);
            return result;
        }

        // Given six parameters for a matrix transform, such as from a world file, get the angle, skew, scale, and translation
        // parameters. Basically the reverse of CalculateTransform.
        static void GetTransformParameters(float[] elements, out float angle, out float skew, out float scaleX, out float scaleY, out PointF translate)
        {
            angle = (float)(Math.Atan2(elements[1], elements[0]) / (Math.PI / 180));
            double denom = elements[0] * elements[0] + elements[1] * elements[1];
            scaleX = (float)Math.Sqrt(denom);
            scaleY = (elements[0] * elements[3] - elements[2] * elements[1]) / scaleX;
            double theta = (Math.Atan2((elements[0] * elements[2] + elements[1] * elements[3]) * (scaleX / scaleY), denom));
            scaleY *= (float)(1 / Math.Cos(theta));  // compensate for the weird way OCAD does skew.
            skew = angle - (float)(theta / (Math.PI / 180));
            translate = new PointF(elements[4], elements[5]);
        }


        public object Clone()
        {
            return base.MemberwiseClone();
        }

        public override bool Equals(object obj)
        {
            TemplateInfo other = obj as TemplateInfo;
            if (other == null)
                return false;

            return (other.absoluteFileName == this.absoluteFileName &&
                other.angle == this.angle &&
                other.centerPoint == this.centerPoint &&
                other.dpi == this.dpi &&
                other.fileLoader == this.fileLoader &&
                other.scaleX == this.scaleX &&
                other.scaleY == this.scaleY &&
                other.shearAngle == this.shearAngle &&
                other.visible == this.visible);
        }
    }

    // Errors that can occur when loading a map file or a template.
    public enum LoadingError {
        None,   // No error.
        FileDoesntExist,  // File doesn't exist
        FileNotReadable,  // I/O error opening or reading file
        ImageFileFormat,  // File format of image file no good
        ImageTooLarge     // Image file too large
    }

    // Information about an template known only after attempting to load it.
    public class TemplateLoadInfo {
        public readonly LoadingError LoadingError;
        public readonly int PixelWidth, PixelHeight;

        public TemplateLoadInfo(LoadingError loadingError, int pixelWidth = 0, int pixelHeight = 0) {
            this.LoadingError = loadingError;
            this.PixelWidth = pixelWidth;
            this.PixelHeight = pixelHeight;
        }
    }

    // Represents a template where the corresponding OCAD or bitmap file is loaded. Only loaded on demand 
    // (when a drawing operation that draws the templates is executed).
    abstract class LoadedTemplate: IDisposable
    {
        protected Map owningMap;
        private bool drawAboveMap;

        public LoadedTemplate(Map owningMap)
        {
            this.owningMap = owningMap;
        }

        public bool DrawAboveMap { get { return drawAboveMap; } }

        public abstract RectangleF GetBounds(int templateRecursionCount);
        public abstract void Draw(IGraphicsTarget g, RectangleF rect, RenderOptions renderOpts, Operation throwOnCancel, int templateRecursionCount);
        public abstract void Dispose();
        public abstract TemplateLoadInfo GetTemplateLoadInfo();

        public static LoadedTemplate Create(Map owningMap, TemplateInfo templateInfo)
        {
            IFileLoader fileLoader = (templateInfo.fileLoader == null) ? owningMap.FileLoader : templateInfo.fileLoader;
            LoadedTemplate result;

            switch (fileLoader.CheckFileKind(templateInfo.absoluteFileName)) {
                case FileKind.OtherFile:
                    result = new BitmapTemplate(owningMap, templateInfo); break;
                case FileKind.OcadFile:
                    result = new OcadFileTemplate(owningMap, templateInfo); break;
                case FileKind.DoesntExist:
                    result = new UnloadableTemplate(owningMap, LoadingError.FileDoesntExist); break;
                case FileKind.NotReadable: 
                default:
                    result = new UnloadableTemplate(owningMap, LoadingError.FileNotReadable); break;
            }

            result.drawAboveMap = templateInfo.drawAboveMap;
            return result;
        }
    }

    // Represents a template that couldn't be loaded. Keeps us from keeping trying loading again and again,
    // and holds onto an error code for us.
    class UnloadableTemplate: LoadedTemplate
    {
        LoadingError loadingError;

        public UnloadableTemplate(Map owningMap, LoadingError loadingError)
            : base(owningMap)
        {
            this.loadingError = loadingError;
        }

        public override TemplateLoadInfo GetTemplateLoadInfo()
        {
            return new TemplateLoadInfo(loadingError);
        }

        public override void Draw(IGraphicsTarget g, RectangleF rect, RenderOptions renderOpts, Operation throwOnCancel, int templateRecursionCount)
        {}

        public override void Dispose()
        {}

        public override RectangleF GetBounds(int templateRecursionCount)
        {
            return new RectangleF();
        }
    }

    class BitmapTemplate : LoadedTemplate
    {
        IGraphicsBitmap bitmap;
        Matrix transform, inverseTransform;
        SizeF size;     // size in pixels (before transformation);

        public BitmapTemplate(Map owningMap, TemplateInfo templateInfo)
            : base(owningMap)
        {
            IFileLoader fileLoader = (templateInfo.fileLoader == null) ? owningMap.FileLoader : templateInfo.fileLoader;

            // TODO: How to deal with case that bitmap is bad format?
            this.bitmap = fileLoader.LoadBitmap(templateInfo.absoluteFileName, true);
            if (bitmap == null)
                size = new SizeF();
            else
                size = new SizeF(bitmap.PixelWidth, bitmap.PixelHeight);

            this.transform = templateInfo.CalculateTransform();
            this.inverseTransform = this.transform.Clone();
            this.inverseTransform.Invert();
        }

        public override TemplateLoadInfo GetTemplateLoadInfo()
        {
            if (bitmap == null)
                return new TemplateLoadInfo(LoadingError.ImageFileFormat);
            else
                return new TemplateLoadInfo(LoadingError.None, bitmap.PixelWidth, bitmap.PixelHeight);
        }

        public override RectangleF GetBounds(int templateRecursionCount)
        {
            return Geometry.BoundsOfTransformedRectangle(new RectangleF(- size.Width / 2, -size.Height / 2, size.Width, size.Height), transform);
        }

        public override void Draw(IGraphicsTarget g, RectangleF rect, RenderOptions renderOpts, Operation throwOnCancel, int templateRecursionCount)
        {
            RectangleF clippedBitmap, clippedRect;

            if (bitmap == null)
                return;

            bool canClip = ComputeClippedBitmap(rect, out clippedBitmap, out clippedRect);
            if (canClip && (clippedBitmap.Width <= 0 || clippedBitmap.Height <= 0))
                return; // Nothing to draw.

            g.PushTransform(transform);
            float transformedMinResolution = Geometry.TransformDistance(renderOpts.minResolution, inverseTransform);

            try {
                if (canClip) {
                    g.DrawBitmapPart(bitmap, (int)clippedBitmap.Left, (int)clippedBitmap.Top, (int)clippedBitmap.Width, (int)clippedBitmap.Height,
                        clippedRect, BitmapScaling.MediumQuality, transformedMinResolution);
                }
                else {
                    RectangleF rectangle = new RectangleF(- size.Width / 2, -size.Height / 2, size.Width, size.Height);
                    g.DrawBitmap(bitmap, rectangle, BitmapScaling.MediumQuality, transformedMinResolution); 
                }
            }
            finally {
                g.PopTransform();
            }
        }

        // Determine if it we can only draw some of the bitmap, and if so, what the source and destination rectangles
        // should be.
        bool ComputeClippedBitmap(RectangleF destRect, out RectangleF clippedBitmap, out RectangleF clippedRect)
        {
            // The matrix "transform" maps from a rectangle with origin at the center of the bitmap to the destination in paper coordinates.
            // This transform may involve rotation and shearing.
            // The input "destRect" is also in paper coordinates.

            // First, get the transform and inverse transform from bitmap pixel coordinates to paper.
            Matrix pixelToPaper = transform.Clone();
            pixelToPaper.Translate(-size.Width / 2, -size.Height / 2, MatrixOrder.Prepend);
            Matrix paperToPixel = pixelToPaper.Clone();
            paperToPixel.Invert();

            // Get the rectangle in bitmap coordinates that maps to destRect.
            RectangleF sourceRect = Geometry.BoundsOfTransformedRectangle(destRect, paperToPixel);
            if (sourceRect.Left <= 0 && sourceRect.Right >= size.Width &&
                sourceRect.Top <= 0 && sourceRect.Bottom >= size.Height) 
            {
                // The source rectangle is bigger than the bitmap, no clipping.
                clippedBitmap = new RectangleF(0, 0, size.Width, size.Height);
                clippedRect = new RectangleF(-size.Width / 2, -size.Height / 2, size.Width, size.Height);
                return false;
            }
            else {
                // Restrict the source rectangle to be no larger than the bitmap, rounding out to 
                // pixel coordinates.
                float left, top, right, bottom;
                left = (float) Math.Max(0, Math.Floor(sourceRect.Left));
                top = (float) Math.Max(0, Math.Floor(sourceRect.Top));
                right = (float) Math.Min(size.Width, Math.Ceiling(sourceRect.Right));
                bottom = (float) Math.Min(size.Height, Math.Ceiling(sourceRect.Bottom));
                clippedBitmap = RectangleF.FromLTRB(left, top, right, bottom);

                // The destination is always just offset by half of width and height.
                clippedRect = clippedBitmap;
                clippedRect.Offset(-size.Width / 2, -size.Height / 2);
                return true;
            }
        }

        public override void Dispose()
        {
            if (bitmap != null) {
                bitmap.Dispose();
                bitmap = null;
            }
        }
    }

    class OcadFileTemplate : LoadedTemplate
    {
        Map map;
        Matrix transform;

        public OcadFileTemplate(Map owningMap, TemplateInfo templateInfo)
            : base(owningMap)
        {
            IFileLoader fileLoader = (templateInfo.fileLoader == null) ? owningMap.FileLoader : templateInfo.fileLoader;

            // TODO: How to deal with file load errors gracefully?
            this.map = fileLoader.LoadMap(templateInfo.absoluteFileName, owningMap);
            this.transform = new Matrix();  // identity for now, may change in CheckRealWorldCoords().

            CheckRealWorldCoords();
        }

        public override TemplateLoadInfo GetTemplateLoadInfo()
        {
            return new TemplateLoadInfo(LoadingError.None);
        }

        // Check to see if real world coords dictate a different map alignment.
        private void CheckRealWorldCoords()
        {
            // UNDONE: check OCAD version of owning map?
            RealWorldCoords realCoordsThisMap, realCoordsOwningMap;
            float scaleFactorThis, scaleFactorOwning;

            using (map.Read()) {
                realCoordsThisMap = map.RealWorldCoords;
                scaleFactorThis = (float) map.RealWorldCoords.GridScaleFactor * map.MapScale / 1000F; // factor between OCAD units and meters in the real world.
            }
            using (owningMap.Read()) {
                realCoordsOwningMap = owningMap.RealWorldCoords;
                scaleFactorOwning = (float) owningMap.RealWorldCoords.GridScaleFactor * owningMap.MapScale / 1000F; // factor between OCAD units and meters in the real world.
            }

            if (realCoordsThisMap.RealWorldOn) {
                // Create transformation to align to map's real world coords to the owning maps real world coords.
                transform = new Matrix();
                transform.Scale(scaleFactorThis, scaleFactorThis, MatrixOrder.Append);
                transform.Rotate(-(float)realCoordsThisMap.RealWorldAngle, MatrixOrder.Append);
                transform.Translate((float)realCoordsThisMap.RealWorldOffsetX, (float)realCoordsThisMap.RealWorldOffsetY, MatrixOrder.Append);
                transform.Translate((float)-realCoordsOwningMap.RealWorldOffsetX, (float)-realCoordsOwningMap.RealWorldOffsetY, MatrixOrder.Append);
                transform.Rotate((float)(realCoordsOwningMap.RealWorldAngle), MatrixOrder.Append);
                transform.Scale(1F / scaleFactorOwning, 1F / scaleFactorOwning, MatrixOrder.Append);  // scale to real world.
            }
        }

        public override RectangleF GetBounds(int templateRecursionCount)
        {
            using (map.Read()) {
                RectangleF mapBounds;
                if (templateRecursionCount > 15)
                    mapBounds = map.Bounds;
                else
                    mapBounds = map.GetBoundsIncludingTemplates(templateRecursionCount + 1);
                
                return Geometry.BoundsOfTransformedRectangle(mapBounds, transform);
            }
        }

        public override void Draw(IGraphicsTarget g, RectangleF rect, RenderOptions renderOpts, Operation throwOnCancel, int templateRecursionCount)
        {
            // Don't recurse more than 15 levels. Prevents unbounded recursion when template directly or indirectly has itself as a template.
            if (templateRecursionCount > 15)
                return;
            templateRecursionCount += 1;

            // Determine bounding rectangle in drawn map coordinates.
            Matrix inverse = transform.Clone();
            inverse.Invert();
            RectangleF boundingRect = Geometry.BoundsOfTransformedRectangle(rect, inverse);

            // Draw the template map. The transform is usually identity, but maybe not if real world coords dictate
            // a different alignment.
            using (map.Read()) {
                g.PushTransform(transform);
                map.Draw(g, boundingRect, renderOpts, throwOnCancel, templateRecursionCount);
                g.PopTransform();
            }
        }

        public override void Dispose()
        {
            if (map != null) {
                map.Dispose();
                map = null;
            }
        }
    }

    // Information about real world coordinates
    public class RealWorldCoords: ICloneable
    {
        public bool RealWorldOn;
        public double RealWorldOffsetX, RealWorldOffsetY;
        public double RealWorldLocalOffsetX, RealWorldLocalOffsetY;
        public double RealWorldAngle;
        public double RealWorldGridDistance;
        public double PaperGridDistance;
        public double GridScaleFactor = 1.0;  // Typically 1.0 (1 unit = 1m), but can be used to scale. Useful for Pseudo-Mercator EPSG 3857.
        private int realWorldGridAndZone;  // OCAD Grid and Zone ID, or 0 if none
        private int epsg;                  // EPSG id, or 0 if none.
        private string proj4;              // proj4 code (may be mapped from epsg or ocad, or may be set separately

        public RealWorldCoords Clone()
        {
            return (RealWorldCoords) base.MemberwiseClone();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public override bool Equals(object obj)
        {
            RealWorldCoords other = obj as RealWorldCoords;
            if (other == null)
                return false;

            return (other.RealWorldOn == this.RealWorldOn &&
                other.RealWorldOffsetX == this.RealWorldOffsetX &&
                other.RealWorldOffsetY == this.RealWorldOffsetY &&
                other.RealWorldLocalOffsetX == this.RealWorldLocalOffsetX &&
                other.RealWorldLocalOffsetY == this.RealWorldLocalOffsetY &&
                other.RealWorldAngle == this.RealWorldAngle &&
                other.RealWorldGridDistance == this.RealWorldGridDistance &&
                other.PaperGridDistance == this.PaperGridDistance &&
                other.GridScaleFactor == this.GridScaleFactor && 
                other.realWorldGridAndZone == this.realWorldGridAndZone &&
                other.epsg == this.epsg &&
                other.proj4 == this.proj4);
        }

        public RealWorldCoords()
        {
            RealWorldGridDistance = 100;
            PaperGridDistance = 10;
            GridScaleFactor = 1.0;
        }

        public MapProjectionType ProjectionType
        {
            get
            {
                if (!string.IsNullOrEmpty(proj4)) {
                    return MapProjectionType.Known;
                }

                if (realWorldGridAndZone == 0 || realWorldGridAndZone == 1000)
                    return MapProjectionType.None;
                else
                    return MapProjectionType.Unknown;
            }
        }

        public int RealWorldGridAndZone {
            get { return realWorldGridAndZone; }
            set {
                realWorldGridAndZone = value;
                proj4 = OcadToProj4Projection.Proj4StringFromOcadProjectionId(RealWorldGridAndZone);
            }
        }

        public int Epsg {
            get { return epsg; }
            set {
                realWorldGridAndZone = 0;
                epsg = value;
                proj4 = MapModel.Projections.AuthorityCodeHandler.Instance["EPSG:" + value.ToString()];
            }
        }

        // The string that the Proj4 libraries used to identify a projection.
        public string Proj4String
        {
            get
            {
                return proj4;
            }
            set {
                realWorldGridAndZone = 0;
                epsg = 0;
                proj4 = value;
            }
        }

    }

    public class GpsReferencePoint: ICloneable
    {
        string name;
        PointF mapCoord;
        double longitude, latitude;
        bool active;

        public string Name
        {
            get { return name; }
        }

        public PointF MapCoord
        {
            get { return mapCoord; }
        }

        public double Longitude
        {
            get { return longitude; }
        }

        public double Latitude
        {
            get { return latitude; }
        }

        public bool Active
        {
            get { return active; }
        }

        public GpsReferencePoint(string name, PointF mapCoord, double longitude, double latitude, bool active)
        {
            this.name = name;
            this.mapCoord = mapCoord;
            this.longitude = longitude;
            this.latitude = latitude;
            this.active = active;
        }

        public object Clone()
        {
            return base.MemberwiseClone();
        }
    }

    public class GpsReferenceInfo: ICloneable
    {
        bool gpsReferenceOn;
        float angle;
        IList<GpsReferencePoint> referencePoints;

        public GpsReferenceInfo(bool gpsReferenceOn, float angle, IList<GpsReferencePoint> referencePoints)
        {
            this.gpsReferenceOn = gpsReferenceOn;
            this.angle = angle;

            if (referencePoints == null)
                this.referencePoints = new List<GpsReferencePoint>(0);
            else
                this.referencePoints = Util.DeepCloneList(referencePoints);
        }

        public bool GpsReferenceOn
        {
            get { return gpsReferenceOn; }
        }

        public float Angle
        {
            get { return angle; }
        }

        public IList<GpsReferencePoint> ReferencePoints
        {
            get { return referencePoints; }
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public GpsReferenceInfo Clone()
        {
            GpsReferenceInfo other = (GpsReferenceInfo) base.MemberwiseClone();
            other.referencePoints = Util.DeepCloneList(other.referencePoints);
            return other;
        }

    }

    public enum MapProjectionType { None, Unknown, Known }

    public class MapHitTestOptions: ICloneable
    {
        // Hit test against the borders of area symbols only.
        public bool AreaBordersOnly = false;

        // Hit test against area holes also.
        public bool AreaIncludeHoles = false;

        // Hit test against the interiors of area holes also.
        public bool AreaIncludeHoleInteriors = false;

        public object Clone()
        {
            return base.MemberwiseClone();
        }
    }

    public enum FileKind { DoesntExist, NotReadable, OcadFile, OtherFile };

    public interface IFileLoader
    {
        // Determine file existance and kind.
        FileKind CheckFileKind(string path);

        // Load a bitmap from the given path. 
        IGraphicsBitmap LoadBitmap(string path, bool isTemplate);

        // Load a bitmap from bitmap data.
        IGraphicsBitmap LoadBitmapFromData(byte[] data);

        // Load a map from the given path.
        Map LoadMap(string path, Map referencingMap);

    }

    public class MapChangedEventArgs: EventArgs
    {
        public readonly bool AllChanged;        // If true, must assume everything may have change; otherwise check changedArea
        public readonly RectSet ChangedArea;    // The area of the map that changed, if allChanged is false.

        public MapChangedEventArgs() {
            AllChanged = true;
            ChangedArea = null;
        }

        public MapChangedEventArgs(RectSet changedArea)
        {
            AllChanged = false;
            this.ChangedArea = changedArea;
        }
    }

    public class Map: IDisposable
    {
        internal List<SymColor> colors = new List<SymColor>();
        List<SymDef> symdefs = new List<SymDef>();
        List<Symbol> symbols = new List<Symbol>();
        Dictionary<SymDef, bool> hiddenSymbols = new Dictionary<SymDef, bool>();
        ColorMatrix colorMatrix;               // if non-null, transforms colors when rendering the map.
        string fileInformation;
        float mapScale;
        float printScale;
        RectangleF printArea;                  // print area, if 0,0,0,0, then print whole map.
        List<TemplateInfo> templates;          // if non-null, information about the templates (top to bottom) associated with the map (templates are NOT rendered).
        List<LoadedTemplate> loadedTemplates;  // Corresponding loaded template, loaded on demand.
        bool templatesHidden = false;          // true to hide all templates.
        RealWorldCoords realWorldCoords = new RealWorldCoords();
        GpsReferenceInfo gpsReferenceInfo = new GpsReferenceInfo(false, 0, null);
        bool useEuclideanMetric = false;       // true -- use Euclidean metric (OCAD 11)
        UndoMgr undoMgr;                       // optional undoMgr. If non-null, methods that change map will be registered with it.

        ReaderWriterLockSlim maplock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        CancellationTokenSource maplockCancellationSource;

        bool mapDirty = false;
        bool allDirty = false;  // All of the map must be considered dirty.
        RectSet dirtyArea; // If mapDirty is true and allDirty is false, the area that is dirty.
        bool symdefsDirty = false;
        bool boundsAccurate = false;          // Are the map bounds accurate?
        RectangleF mapBounds;                        // If boundsAccurate is true, the bounds are accurate.

        object boundsPenKey = new object();      // pen for drawing symbol bounds.

        readonly ITextMetrics textMetricsProvider;
        readonly IFileLoader fileLoader;

        public readonly SymColor LayoutColor = new SymColor(SymLayer.Layout);
        public readonly SymColor ImageColor = new SymColor(SymLayer.Image);

        ImageSymDef layoutSymDef, imageSymDef; // special symdefs for image/layout objects.

        // Cache objects for highlighting.
        readonly Dictionary<Pair<float, Pair<LineJoin, LineCap>>, object> highlightPens = new Dictionary<Pair<float, Pair<LineJoin, LineCap>>, object>();
        readonly Dictionary<float, object> holeHighlightPens = new Dictionary<float,object>();
        readonly object highlightBrush = new object();
        readonly object highlightDimBrush = new object();
        readonly object holeHighlightBrush = new object();
        readonly object holeHighlightDimBrush = new object();

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
        internal List<string> nonRenderableObjects = new List<string>();      
        public string[] NotRenderableObjects {
            get
            {
                return nonRenderableObjects.ToArray();
            }
        }

        // A list of fonts name that are in the map, but not installed.
        internal List<string> missingFonts = new List<string>();
        public string[] MissingFonts
        {
            get
            {
                return missingFonts.ToArray();
            }
        }

        public delegate void MapChangedHandler(Map sender, MapChangedEventArgs e);
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

        private void AcquireWriteLock()
        {
            if (! maplock.TryEnterWriteLock(0)) {
                // We can't enter the write lock, because there is another reader or writer.
                // If there is a maplockCancellationSource, then perhaps a cancellable reader
                // can be cancelled so we can enter faster. In any case, wait to get the write lock.
                CancellationTokenSource cancelSource = maplockCancellationSource;
                if (cancelSource != null) {
                    cancelSource.Cancel();
                }

                maplock.EnterWriteLock();
            }
            else {
            }

            // We have entered the write lock.
            // We know there are no readers now, so get rid of the cancellationSource
            // so new readers aren't cancelled once we exit the write lock.
            maplockCancellationSource = null;
        }

        public WriteReleaser Write()
        {
            Debug.Assert(!mapDirty);

            AcquireWriteLock();

            return new WriteReleaser(this); // the writeReleaser calls FinishWrite when it is Disposed.
        }

        private void FinishWrite() {
            bool dirty = mapDirty;
            bool wasAllDirty = allDirty || symdefsDirty; // If symdefs changed, anything could change.
            RectSet wasDirtyArea = dirtyArea;
            bool defsDirty = symdefsDirty;

            mapDirty = false;
            allDirty = false;
            dirtyArea = null;
            symdefsDirty = false;

            if (wasAllDirty)
                boundsAccurate = false;

            maplock.ExitWriteLock();

            if (dirty || defsDirty) {
                MapChanged(wasAllDirty, wasDirtyArea);
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
            if (!maplock.TryEnterReadLock(0)) {
                maplock.EnterReadLock();
            }
            else {
            }

            Debug.Assert(!mapDirty && !symdefsDirty);
            return new ReadReleaser(this); // the ReadReleaser calls FinishRead when it is Disposed.
        }

        private void FinishRead()
        {
            Debug.Assert(!mapDirty && !symdefsDirty);
            maplock.ExitReadLock();
        }

        // Enters a Read(), but provides a cancellation token that will be signals if someone
        // wants to write. This allows readers that are OK to be cancelled to allow writers faster
        // access. For example, background drawing code can be cancelled.
        public ReadReleaser CancellableRead(out CancellationToken cancellationToken)
        {
            if (!maplock.TryEnterReadLock(0)) {
                maplock.EnterReadLock();
            }
            else {
            }

            // We know there are no writers now.
            // If necessary, create a cancellation source that could be used to cancel our read.
            if (maplockCancellationSource == null)
                Interlocked.CompareExchange(ref maplockCancellationSource, new CancellationTokenSource(), null);
            cancellationToken = maplockCancellationSource.Token;

            Debug.Assert(!mapDirty && !symdefsDirty);
            return new ReadReleaser(this); // the ReadReleaser calls FinishRead when it is Disposed.
        }

        public struct UndoableWriteReleaser : IDisposable
        {
            private Map map;
            private int cookie;

            public UndoableWriteReleaser(Map map, int cookie) { this.map = map; this.cookie = cookie; }

            public void Dispose()
            {
                map.FinishUndoableWrite(cookie);
            }
        }

        int undoCookie = 332;
        public UndoableWriteReleaser UndoableWrite(string commandName)
        {
            int cookie = undoCookie++;
            Debug.Assert(!mapDirty);

            AcquireWriteLock();

            undoMgr.BeginCommand(cookie, commandName);
            return new UndoableWriteReleaser(this, cookie); // the writeReleaser calls FinishWrite when it is Disposed.
        }

        private void FinishUndoableWrite(int cookie)
        {
            undoMgr.EndCommand(cookie);
            FinishWrite();
        }

        // Set the entire map as potentially dirty.
        internal void SetDirty()
        {
            mapDirty = true;
            allDirty = true;
            dirtyArea = null;
        }

        // Add a rectangle to the dirty area.
        internal void SetDirty(RectangleF dirtyRect)
        {
            mapDirty = true;

            if (! allDirty) {
                if (dirtyArea == null)
                    dirtyArea = new RectSet(dirtyRect);
                else
                    dirtyArea.Add(dirtyRect);
            }

            if (boundsAccurate && !allDirty) {
                // Check to see if this change could have grown or shrunk the bounds.
                // If bounds of new rect strictly inside old bounds, then now.
                if (dirtyRect.Left <= mapBounds.Left || dirtyRect.Top <= mapBounds.Top || dirtyRect.Right >= mapBounds.Right || dirtyRect.Bottom >= mapBounds.Bottom)
                    boundsAccurate = false;
            }
        }

        public Map(ITextMetrics textMetricsProvider, IFileLoader fileLoader)
        {
            this.textMetricsProvider = textMetricsProvider;
            this.fileLoader = fileLoader;

            using (Write()) {
                Clear();
            }
        }

        public UndoMgr UndoMgr
        {
            get { return undoMgr; }
            set { undoMgr = value; }
        }

        // Options for the clone operation
        public class CloneOptions
        {
            public bool CloneColors;
        }

        // Clone the map, copying only things specified in the clone options.
        // Currently, just have the option to clone the color table.
        public Map Clone(CloneOptions options)
        {
            CheckReadable();

            Map newMap = new Map(textMetricsProvider, fileLoader);

            using (newMap.Write()) {
                newMap.mapScale = mapScale;
                newMap.printScale = printScale;
                newMap.printArea = printArea;
                newMap.realWorldCoords = realWorldCoords.Clone();

                if (options.CloneColors) {
                    for (int i = 0; i < colors.Count; ++i) {
                        float c, m, y, k;
                        colors[i].GetCMYK(out c, out m, out y, out k);
                        newMap.AddColor(colors[i].Name, colors[i].OcadId, c, m, y, k, colors[i].OverPrint);
                    }
                }
            }

            return newMap;
        }

        private void TraceLine(string format, params object[] args)
        {
            Debug.WriteLine(string.Format(format, args), "Map");
        }

        internal void CheckWritable()
        {
            if (! maplock.IsWriteLockHeld) 
                throw new MapUsageException("Cannot modify map without calling Map.Write");
        }

        void CheckReadable()
        {
            if (! maplock.IsReadLockHeld && ! maplock.IsWriteLockHeld) 
                throw new MapUsageException("Cannot read map without calling Map.Read or Map.Write");
        }

        // Clear everything from the map, INCLUDING the undo mgr. Thus, this is not an undoable action.
        public void Clear()
        {
            CheckWritable();
            colors.Clear();
            symdefs.Clear();
            symbols.Clear();
            SetDirty();
            undoMgr = null;
            symdefsDirty = true;
        }

        void MapChanged(bool allDirty, RectSet dirtyArea) {
            var handler = OnMapChanged;

            if (handler != null) {
                MapChangedEventArgs e;
                if (allDirty)
                    e = new MapChangedEventArgs();
                else
                    e = new MapChangedEventArgs(dirtyArea);

                handler(this, e);
            }
        }

        public ITextMetrics TextMetricsProvider
        {
            get
            {
                return textMetricsProvider;
            }
        }

        public IFileLoader FileLoader
        {
            get { return fileLoader; }
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

        public string FileInformation
        {
            get
            {
                CheckReadable();
                return fileInformation;
            }
            set
            {
                CheckWritable();

                if (fileInformation != value) {
                    if (undoMgr != null) {
                        undoMgr.RecordAction(new ChangeFileInformationAction(this, fileInformation, value));
                    }

                    fileInformation = value;
                    // Doesn't affect rendering, so no need to call SetDirty();
                }
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

                if (mapScale != value) {
                    if (undoMgr != null) {
                        undoMgr.RecordAction(new ChangeMapScaleAction(this, mapScale, value));
                    }

                    mapScale = value;
                    SetDirty();
                }
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
                if (printScale != value) {
                    if (undoMgr != null) {
                        undoMgr.RecordAction(new ChangePrintScaleAction(this, printScale, value));
                    }

                    printScale = value;
                }
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

                if (!printArea.Equals(value)) {
                    if (undoMgr != null) {
                        undoMgr.RecordAction(new ChangePrintAreaAction(this, printArea, value));
                    }

                    printArea = value;
                }
            }
        }

        // Get the one and only image symdef for this map. Used for iamge objects -- created with OCAD image import command.
        public ImageSymDef GetImageSymDef()
        {
            CheckWritable();

            if (imageSymDef == null) {
                imageSymDef = new ImageSymDef(SymLayer.Image);
                this.AddSymdef(imageSymDef);
            }

            return imageSymDef;
        }


        // Get the one and only layout symdef for this map. Used for layout objects -- created with OCAD 11+ layout objects.
        public ImageSymDef GetLayoutSymDef()
        {
            CheckWritable();

            if (layoutSymDef == null) {
                layoutSymDef = new ImageSymDef(SymLayer.Layout);
                this.AddSymdef(layoutSymDef);
            }

            return layoutSymDef;
        }

        public int GetHighestSortOrder(SymDef symDef)
        {
            Debug.Assert(symDef.SortSymbolsForDrawing);

            ICollection<Symbol> allSymbols = symDef.Symbols;

            if (allSymbols.Any())
                return allSymbols.Max(sym => sym.SortOrder);
            else
                return 0;
        }


        // If true, use Euclidan metrics for line symbols (OCAD 11 without OCAD 10 compability flag)
        public bool UseEuclideanMetric
        {
            get
            {
                CheckReadable();
                return useEuclideanMetric;
            }

            set
            {
                CheckWritable();
                if (useEuclideanMetric != value) {
                    if (undoMgr != null) {
                        undoMgr.RecordAction(new ChangeMetricAction(this, useEuclideanMetric, value));
                    }

                    useEuclideanMetric = value;
                    SetDirty();
                }
            }
        }

        // Return a distance metric based on the UseEuclideanMetric property.
        internal SymPath.DistanceMetric MapDistanceMetric
        {
            get { return UseEuclideanMetric ? (SymPath.DistanceMetric)SymPath.EuclidDistance : (SymPath.DistanceMetric) SymPath.BizzarroDistance; }
        }

        // Get information about the templates associated with the map, top to bottom
        public IList<TemplateInfo> Templates
        {
            get
            {
                CheckReadable();
                if (templates == null)
                    return new List<TemplateInfo>(0);
                else 
                    return templates.AsReadOnly();
            }
            set
            {
                CheckWritable();

                if (undoMgr != null) {
                    undoMgr.RecordAction(new ChangeTemplatesAction(this, templates, value));
                }

                if (value != null && value.Count > 0)
                    templates = new List<TemplateInfo>(value);
                else
                    templates = null;

                FreeLoadedTemplates();  // Existing loaded templates are probably bunk.
                SetDirty();
            }
        }

        public bool HideTemplates
        {
            get
            {
                CheckReadable();
                return templatesHidden;
            }
            set
            {
                CheckWritable();
                if (templatesHidden != value) {
                    if (undoMgr != null)
                        undoMgr.RecordAction(new ChangeHideTemplatesAction(this, templatesHidden, value));

                    templatesHidden = value;
                    if (templatesHidden)
                        FreeLoadedTemplates();  // If we are hiding all templates, unload loaded templates to save memory.
                    SetDirty();
                }
            }
        }



        // Are there any visible templates in the map?
        public bool AnyVisibleTemplates
        {
            get
            {
                if (templates == null)
                    return false;

                if (templatesHidden)
                    return false;

                foreach (var t in templates) {
                    if (t.visible)
                        return true;
                }

                return false;
            }
        }

        // Test load a template, and return TemplateLoadInfo about it. This doesn't change the
        // map at all.
        public TemplateLoadInfo TryLoadTemplate(TemplateInfo template)
        {
            LoadedTemplate loadedTemplate = LoadedTemplate.Create(this, template);
            TemplateLoadInfo loadInfo = loadedTemplate.GetTemplateLoadInfo();
            loadedTemplate.Dispose();
            return loadInfo;
        }

        // Even if templates aren't visible, try to load each template and give information
        // about each one. This may require expensively loading files, etc.
        public IList<TemplateLoadInfo> GetTemplateLoadInfo()
        {
            CheckReadable();

            List<TemplateLoadInfo> loadInfos = new List<TemplateLoadInfo>();

            if (templates != null) {
                // Use loaded templates, if we have them, otherwise load then discard.
                for (int i = 0; i < templates.Count; ++i) {
                    LoadedTemplate loadedTemplate;
                    bool dispose;
                    if (loadedTemplates != null && loadedTemplates[i] != null) {
                        loadedTemplate = loadedTemplates[i];
                        dispose = false;
                    }
                    else {
                        loadedTemplate = LoadedTemplate.Create(this, templates[i]);
                        dispose = true;
                    }

                    loadInfos.Add(loadedTemplate.GetTemplateLoadInfo());

                    if (dispose)
                        loadedTemplate.Dispose();
                }
            }

            return loadInfos.AsReadOnly();
        }

        // Load all visible templates. Done in preperation of drawing.
        private void LoadTemplates()
        {
            if (this.loadedTemplates != null)
                return;     // already loaded.

            if (! AnyVisibleTemplates)
                return;

            LoadedTemplate[] templatesLoaded = new LoadedTemplate[templates.Count];

            for (int i = 0; i < templates.Count; ++i) {
                if (templates[i].visible) {
                    templatesLoaded[i] = LoadedTemplate.Create(this, templates[i]);
                }
            }

            this.loadedTemplates = new List<LoadedTemplate>(templatesLoaded);
        }

        private void FreeLoadedTemplates()
        {
            if (loadedTemplates != null) {
                foreach (var templ in loadedTemplates) {
                    if (templ != null)
                        templ.Dispose();
                }
                loadedTemplates = null;
            }
        }

        public RealWorldCoords RealWorldCoords
        {
            get {
                CheckReadable();
                return realWorldCoords.Clone(); 
            }
            set {
                CheckWritable();

                if (!realWorldCoords.Equals(value)) {
                    if (undoMgr != null)
                        undoMgr.RecordAction(new ChangeRealWorldCoordsAction(this, realWorldCoords, value));

                    realWorldCoords = value.Clone();
                }
            }
        }


        public Matrix MapToRealWorldTransform()
        {
            float scaleFactorThis = (float)(realWorldCoords.GridScaleFactor * this.MapScale / 1000.0); // factor between OCAD units and meters in the real world.

            Matrix transform = new Matrix();
            transform.Scale(scaleFactorThis, scaleFactorThis, MatrixOrder.Append);
            transform.Rotate(-(float)realWorldCoords.RealWorldAngle, MatrixOrder.Append);
            transform.Translate((float)realWorldCoords.RealWorldOffsetX, (float)realWorldCoords.RealWorldOffsetY, MatrixOrder.Append);
            return transform;
        }

        public Matrix RealWorldToMapTransform()
        {
            Matrix transform = this.MapToRealWorldTransform();
            transform.Invert();
            return transform;
        }

        // Get information about the templates associated with the map, top to bottom
        public GpsReferenceInfo GpsReferenceInfo
        {
            get
            {
                CheckReadable();

                return gpsReferenceInfo.Clone();
            }
            set
            {
                CheckWritable();

                if (undoMgr != null) {
                    undoMgr.RecordAction(new ChangeGpsReferenceInfoAction(this, gpsReferenceInfo, value));
                }

                gpsReferenceInfo = value.Clone();

                SetDirty();
            }
        }



        // CONSIDER: why doesn't AddColor just take a SymColor? This seems inconsistent with the way the
        // other methods work.

        // The ColorMatrix transforms the colors before rendering. This is useful, for example, to draw
        // in a lighted fashion or similar.
        public ColorMatrix ColorMatrix {
            get {
                return colorMatrix;
            }

            set {
                CheckWritable();

                if (ColorMatrix.Equal(colorMatrix, value))
                    return;         // no change in matrix.

                if (undoMgr != null)
                    undoMgr.RecordAction(new ChangeColorMatrixAction(this, colorMatrix, value));

                this.colorMatrix = value;

                // Free GDI objects that might be using the old colors. They are recreated on demand.
                foreach (SymColor symColor in colors) 
                    symColor.FreeGdiObjects();
                foreach (SymDef symdef in symdefs)
                    symdef.FreeGdiObjects();
            }
        }

        // Transform a given color value by the ColorMatrix, if any. 
        public CmykColor TransformColor(CmykColor colorIn)
        {
            if (colorMatrix == null)
                return colorIn;
            else {
                float redIn = colorIn.Red;
                float greenIn = colorIn.Green;
                float blueIn = colorIn.Blue;

                float redOut = redIn * colorMatrix[0, 0] + greenIn * colorMatrix[1, 0] + blueIn * colorMatrix[2,0] + colorMatrix[4,0];
                float greenOut = redIn * colorMatrix[0, 1] + greenIn * colorMatrix[1, 1] + blueIn * colorMatrix[2, 1] + colorMatrix[4, 1];
                float blueOut = redIn * colorMatrix[0, 2] + greenIn * colorMatrix[1, 2] + blueIn * colorMatrix[2, 2] + colorMatrix[4, 2];

                return CmykColor.FromRgb(redOut, greenOut, blueOut);
            }
        }

        public SymColor AddColorAtIndex(int index, string name, short ocadId, float cyan, float magenta, float yellow, float black, bool overprint)
        {
            CheckWritable();

            SymColor color = new SymColor(SymLayer.Normal);
            color.Name = name;
            color.OcadId = ocadId;
            color.SetCMYK(cyan, magenta, yellow, black);
            color.OverPrint = overprint;

            if (undoMgr != null)
                undoMgr.RecordAction(new ChangeColorTableAction(this, index, null, color));

            colors.Insert(index, color);
            color.SetMap(this);
            symdefsDirty = true;
            return color;
        }

        public SymColor AddColor(string name, short ocadId, float cyan, float magenta, float yellow, float black, bool overprint)
        {
            return AddColorAtIndex(colors.Count, name, ocadId, cyan, magenta, yellow, black, overprint);
        }

        public SymColor AddColorBottom(string name, short ocadId, float cyan, float magenta, float yellow, float black, bool overprint)
        {
            return AddColorAtIndex(0, name, ocadId, cyan, magenta, yellow, black, overprint);
        }

        // Remove the color at a given index. You cannot remove a color that is in use.
        // Use SymdefsUsingColor and GraphicsSymbolsUsingColor to determine if a color is in use.
        public SymColor RemoveColorAtIndex(int index)
        {
            CheckWritable();

            SymColor color = colors[index];
#if DEBUG
            SymDef[] symdefsUsingColor = SymdefsUsingColor(color);
            Symbol[] symbolsUsingColor = GraphicsSymbolsUsingColor(color);
            Debug.Assert(symdefsUsingColor == null || symdefsUsingColor.Length == 0, "Color still in use");
            Debug.Assert(symbolsUsingColor == null || symbolsUsingColor.Length == 0, "Color still in use");
#endif
            if (undoMgr != null)
                undoMgr.RecordAction(new ChangeColorTableAction(this, index, color, null));

            colors.RemoveAt(index);
            color.SetMap(null);

            return color;
        }

        // Changes the color at the given index. It is allow to change a color
        // that is in use.
        public void ChangeColorAtIndex(int index, string name, short ocadId, float cyan, float magenta, float yellow, float black, bool overprint)
        {
            CheckWritable();

            SymColor color = colors[index];
            SymColor oldColor = new SymColor(SymLayer.Normal);
            oldColor.CopyFrom(color);
            color.Update(name, ocadId, cyan, magenta, yellow, black, overprint);

            if (undoMgr != null)
                undoMgr.RecordAction(new ChangeColorTableAction(this, index, oldColor, color));

            symdefsDirty = true;
            SetDirty();

            // Free GDI objects that might be using the old colors. They are recreated on demand.
            foreach (SymDef symdef in symdefs)
                symdef.FreeGdiObjects();
        }

        public void AddSymbol(Symbol sym) {
            CheckWritable();

            if (undoMgr != null) {
                undoMgr.RecordAction(new SymbolChangeAction(this, null, sym));
            }

            if (sym is HoleSymbol) {
                HoleSymbol hole = (HoleSymbol)sym;
                Debug.Assert(!hole.IsAttached, "Cannot add attached hole to map");
                hole.SetMap(this);
                hole.SymbolWithHole.AddHole(hole);
                SetDirty(hole.SymbolWithHole.BoundingBox);
            }
            else {
                symbols.Add(sym);
                sym.SetMap(this);
                sym.Definition.AddSymbol(sym);
                SetDirty(sym.BoundingBox);
            }
        }

        // Note: changing the symbol after it is removed will mess up Undo. Don't do that
        // if you are using UndoMgr support.
        public void RemoveSymbol(Symbol sym) {
            CheckWritable();

            RectangleF boundingBox = sym.BoundingBox;

            if (undoMgr != null) {
                undoMgr.RecordAction(new SymbolChangeAction(this, sym, null));
            }

            if (sym is HoleSymbol) {
                HoleSymbol hole = (HoleSymbol)sym;
                Debug.Assert(hole.IsAttached, "Cannot remove unattached hole from map");
                SetDirty(hole.SymbolWithHole.BoundingBox);
                hole.SymbolWithHole.RemoveHole(hole);
                hole.SetMap(null);
            }
            else {
                Debug.Assert(symbols.Contains(sym));
                symbols.Remove(sym);
                sym.Definition.RemoveSymbol(sym);
                sym.SetMap(null);
                SetDirty(boundingBox);
            }
        }

        public void AddSymdef(SymDef symdef) {
            CheckWritable();

            if (undoMgr != null) {
                undoMgr.RecordAction(new SymdefChangeAction(this, null, symdef));
            }

            symdefs.Add(symdef);
            symdef.SetMap(this);

            if (symdef.DependsOnSymdef != null) {
                Debug.Assert(symdef.DependsOnSymdef.ContainingMap == this);
                symdef.DependsOnSymdef.AddDependentSymdef(symdef);
            }

            symdefsDirty = true;
        }

        // Remove a symdef from the map. The symdef must not be in use by any symbol.
        //
        // Bad things can happen with Undo if the symdef is changed after being removed
        // from the map. So don't do that!
        public void RemoveSymdef(SymDef symdef) {
            CheckWritable();

            Debug.Assert(symdef.symbols.Count == 0); // No symbols may be used this symdef.
            Debug.Assert(symdef.dependentSymdefs.Count == 0); // No symdefs may be dependent on this symdef.

            if (undoMgr != null) {
                undoMgr.RecordAction(new SymdefChangeAction(this, symdef, null));
            }


            if (symdef.DependsOnSymdef != null)
                symdef.DependsOnSymdef.RemoveDependentSymdef(symdef);

            symdefs.Remove(symdef);
            symdef.SetMap(null);
            symdefsDirty = true;
        }

        // Find the symdef with the given ID, or return null if none found.
        public SymDef SymdefFromSymbolId(string symbolId) {
            CheckReadable();

            foreach (SymDef symdef in symdefs) {
                if (symdef.SymbolId == symbolId)
                    return symdef;
            }

            return null;
        }

        // Get a free symdef OCAD ID, starting the search with a given integer part.
        // Always creates an ID that is OCAD 6/7/8 compatible.
        public string GetFreeSymbolId(int integerPart)
        {
            int fracPart = 0;
            string idString;

            for (;;) {
                idString = string.Format("{0}.{1}", integerPart, fracPart);
                if (SymdefFromSymbolId(idString) == null)
                    break;

                ++fracPart;
                if (fracPart >= 10) {
                    integerPart += 1;
                    fracPart = 0;
                }
            }

            return idString;
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

        // Return a symbol in this match that exactly matches a symcolor from another map.
        public SymColor SymColorFromSymColor(SymColor otherColor)
        {
            CheckReadable();

            if (otherColor == null)
                return otherColor;

            foreach (SymColor color in colors) {
                if (color.OcadId == otherColor.OcadId && color.Name == otherColor.Name && color.RawColorValue.Equals(otherColor.RawColorValue))
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

        // Determine which special symbols (not using color from symdef) use a color.
        public Symbol[] GraphicsSymbolsUsingColor(SymColor color)
        {
            CheckReadable();
            List<Symbol> list = new List<Symbol>();

            foreach (Symbol sym in symbols) {
                IGraphicsSymbol graphicsSymbol = sym as IGraphicsSymbol;
                if (graphicsSymbol != null && graphicsSymbol.HasColor(color))
                    list.Add(sym);
            }

            return list.ToArray();
        }

        // Set if a symdef is visible.
        public void SetSymdefVisible(SymDef symdef, bool isVisible)
        {
            CheckWritable();
            Debug.Assert(symdefs.Contains(symdef));

            bool wasVisible = IsSymdefVisible(symdef);

            if (isVisible != wasVisible) {
                if (undoMgr != null) {
                    undoMgr.RecordAction(new SymdefVisibleAction(this, symdef, wasVisible, isVisible));
                }

                hiddenSymbols[symdef] = isVisible;
                boundsAccurate = false;
            }
        }

        // Decide if a symdef is visible.
        public bool IsSymdefVisible(SymDef symdef)
        {
            bool isVisible;

            CheckReadable();

            if (symdef is HoleSymDef)
                return true;
            
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
                    // Only use visible symbols.
                    bool first = true;
                    foreach (Symbol sym in AllSymbols) {
                        if (this.IsSymdefVisible(sym.Definition)) {
                            if (first)
                                mapBounds = sym.BoundingBox;
                            else
                                mapBounds = RectangleF.Union(mapBounds, sym.BoundingBox);

                            first = false;
                        }
                    }

                    if (first)
                        mapBounds = new RectangleF();       // no symbols: empty rectangle.

                    boundsAccurate = true;
                }

                return mapBounds;
            }
        }

        // Includes the size of VISIBLE templates in the bounds.
        public RectangleF GetBoundsIncludingTemplates()
        {
            return GetBoundsIncludingTemplates(0);
        }

        internal RectangleF GetBoundsIncludingTemplates(int templateRecursionCount)
        {
            RectangleF bounds = this.Bounds;

            if (AnyVisibleTemplates) {
                LoadTemplates();

                if (loadedTemplates != null) {
                    for (int i = loadedTemplates.Count - 1; i >= 0; --i) {
                        if (loadedTemplates[i] != null)
                            bounds = RectangleF.Union(bounds, loadedTemplates[i].GetBounds(templateRecursionCount));
                    }
                }
            }

            return bounds;
        }

        public void DrawHighlightedSymbols(IGraphicsTarget g, IEnumerable<Symbol> symbols, RectangleF rect, HighlightOptions options, CancellationToken cancelToken)
        {
            CheckReadable();

            cancelToken.ThrowIfCancellationRequested();

            foreach (Symbol sym in symbols) {
                if (sym.ContainingMap == this && IsSymdefVisible(sym.Definition) && sym.IsVisible) {
                    // Only draw the symbol if it may intersect. Check
                    // the bounding box first as it's faster exclusion than MayIntersectRect.
                    RectangleF bounds = sym.BoundingBox;
                    if (bounds.IntersectsWith(rect) && sym.MayIntersectRect(rect)) {
                        sym.DrawHighlight(g, options);

                        cancelToken.ThrowIfCancellationRequested();
                    }
                }
            }
        }

        internal CmykColor HighlightColor = CmykColor.FromRgba(1, 0, 0, 1F);
        internal CmykColor HighlightDimColor = CmykColor.FromRgba(1, 0, 0, 0.25F);
        internal CmykColor HoleHighlightColor = CmykColor.FromRgba(1, 0.3333F, 0, 1F);
        internal CmykColor HoleHighlightDimColor = CmykColor.FromRgba(1, 0.3333F, 0, 0.33F);

        // Get a cached reference to a pen for highlighting of the given width.
        internal object GetHighlightPen(IGraphicsTarget g, float width, LineJoin lineJoin, LineCap lineCap)
        {
            var key = new Pair<float, Pair<LineJoin, LineCap>>(width, new Pair<LineJoin, LineCap>(lineJoin, lineCap));

            if (!highlightPens.ContainsKey(key)) {
                lock (highlightPens) {
                    highlightPens[key] = new object();
                }
            }
            object pen = highlightPens[key];

            if (!g.HasPen(pen)) {
                g.CreatePen(pen, HighlightColor, width, lineCap, lineJoin, GraphicsUtil.MITER_LIMIT);
            }

            return pen;
        }

        // Get a cached reference to a pen for highlighting holes of the given width.
        internal object GetHoleHighlightPen(IGraphicsTarget g, float width)
        {
            if (!holeHighlightPens.ContainsKey(width)) {
                lock (holeHighlightPens) {
                    holeHighlightPens[width] = new object();
                }
            }
            object pen = holeHighlightPens[width];

            if (!g.HasPen(pen)) {
                GraphicsUtil.CreateSolidPen(g, pen, HoleHighlightColor, width, LineStyle.Mitered);
            }

            return pen;
        }

        internal object GetHighlightBrush(IGraphicsTarget g)
        {
            if (!g.HasBrush(highlightBrush)) {
                g.CreateSolidBrush(highlightBrush, HighlightColor);
            }
            return highlightBrush;
        }

        internal object GetDimHighlightBrush(IGraphicsTarget g)
        {
            if (!g.HasBrush(highlightDimBrush)) {
                g.CreateSolidBrush(highlightDimBrush, HighlightDimColor);
            }
            return highlightDimBrush;
        }

        internal object GetHoleHighlightBrush(IGraphicsTarget g)
        {
            if (!g.HasBrush(holeHighlightBrush)) {
                g.CreateSolidBrush(holeHighlightBrush, HoleHighlightColor);
            }
            return holeHighlightBrush;
        }

        internal object GetDimHoleHighlightBrush(IGraphicsTarget g)
        {
            if (!g.HasBrush(holeHighlightDimBrush)) {
                g.CreateSolidBrush(holeHighlightDimBrush, HoleHighlightDimColor);
            }
            return holeHighlightDimBrush;
        }

        public void Draw(IGraphicsTarget g, RectangleF rect, RenderOptions renderOpts, Operation throwOnCancel)
        {
            Draw(g, rect, renderOpts, throwOnCancel, 0);
        }

        internal void Draw(IGraphicsTarget g, RectangleF rect, RenderOptions renderOpts, Operation throwOnCancel, int templateRecursionCount)
        {
            CheckReadable();

            //TraceLine("Begin drawing rectangle ({0},{1})-({2},{3})", rect.Left, rect.Top, rect.Right, rect.Bottom);
            Debug.Indent();

            // Templates below the map (usual OCAD case)
            if ((renderOpts.renderTemplates == RenderTemplateOption.MapAndTemplates || renderOpts.renderTemplates == RenderTemplateOption.TemplatesOnly) &&
                this.AnyVisibleTemplates) {
                DrawTemplates(g, rect, renderOpts, throwOnCancel, false, templateRecursionCount);
            }

            if (renderOpts.renderTemplates == RenderTemplateOption.MapAndTemplates || renderOpts.renderTemplates == RenderTemplateOption.MapOnly) { 
                if (renderOpts.showSymbolBounds && !g.HasPen(boundsPenKey))
                    GraphicsUtil.CreateSolidPen(g, boundsPenKey, CmykColor.FromRgba(1.0F, 0, 0, 0.392157F), 0.01F, LineStyle.Mitered);

                // Draw the image layer.
                DrawColor(g, this.ImageColor, rect, renderOpts, throwOnCancel);
                if (throwOnCancel != null)
                    throwOnCancel();

                // Draw each color separately, to get correct layering.
                foreach (SymColor curColor in colors) {
                    DrawColor(g, curColor, rect, renderOpts, throwOnCancel);
                    if (throwOnCancel != null)
                        throwOnCancel();
                }

                // Draw the layout layer.
                DrawColor(g, this.LayoutColor, rect, renderOpts, throwOnCancel);
                if (throwOnCancel != null)
                    throwOnCancel();
            }

            // Templates above the map (OOM case)
            if ((renderOpts.renderTemplates == RenderTemplateOption.MapAndTemplates || renderOpts.renderTemplates == RenderTemplateOption.TemplatesOnly) &&
                this.AnyVisibleTemplates) {
                DrawTemplates(g, rect, renderOpts, throwOnCancel, true, templateRecursionCount);
            }

            Debug.Unindent();
        }

        private int SymbolComparison(Symbol s1, Symbol s2)
        {
            return s1.SortOrder.CompareTo(s2.SortOrder);
        }

        // Draw a particular color layer. If curColor is ImageColor or LayoutColor, draw the image layer or Layout layer 
        private void DrawColor(IGraphicsTarget g, SymColor curColor, RectangleF rect, RenderOptions renderOpts, Operation throwOnCancel)
        {
            int symbolsDrawn = 0;
            bool anySymbolsDrawn = false;
            bool mustPopBlending = false;

            //TraceLine("Drawing color {0}", curColor.Name ?? "special layer");

            foreach (SymDef symdef in symdefs) {
                if (IsSymdefVisible(symdef) && symdef.HasColor(curColor)) {
                    List<Symbol> symbolsForThisDef = symdef.symbols;

                    if (symdef.SortSymbolsForDrawing) {
                        symbolsForThisDef = new List<Symbol>(symbolsForThisDef);
                        symbolsForThisDef.Sort(SymbolComparison);
                    }

                    foreach (Symbol curSym in symbolsForThisDef) {
                        if (curSym.IsVisible) {
                            // Only draw the symbol if it may intersect. Check
                            // the bounding box first as it's faster exclusion than MayIntersectRect.
                            RectangleF bounds = curSym.BoundingBox;
                            if (bounds.IntersectsWith(rect) &&
                                curSym.MayIntersectRect(rect)) 
                            {
                                if (renderOpts.blendOverprintedColors && curColor.OverPrint && !anySymbolsDrawn) {
                                    // We need to blend this color.
                                    g.PushBlending(BlendMode.Darken);
                                    mustPopBlending = true;
                                }
                                
                                //TraceLine("Drawing symbol with definition '{0}' and bounds ({1},{2})-({3},{4})", symdef.Name, bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
                                curSym.Draw(g, curColor, renderOpts);
                                ++symbolsDrawn;
                                anySymbolsDrawn = true;

                                //TraceLine("Done drawing symbol");

                                if (renderOpts.showSymbolBounds)
                                    g.DrawRectangle(boundsPenKey, bounds);

                                //if (symbolsDrawn % 100 == 0)
                                //    TraceLine("Drawn {0} symbols", symbolsDrawn);
                                if (throwOnCancel != null && symbolsDrawn % 64 == 0)
                                    throwOnCancel();
                            }
                        }
                    }
                }
            }

            if (mustPopBlending)
                g.PopBlending();

            //TraceLine("Drawing color {0}: drew {1} symbols.", curColor.Name ?? "special layer", symbolsDrawn);
        }

        // Draw all the templates; either the aboveMap ones or the below map ones.
        private void DrawTemplates(IGraphicsTarget g, RectangleF rect, RenderOptions renderOpts, Operation throwOnCancel, bool aboveMap, int templateRecursionCount)
        {
            LoadTemplates();

            if (loadedTemplates != null) {
                // Draw from bottom to top.
                for (int i = loadedTemplates.Count - 1; i >= 0; --i) {
                    if (loadedTemplates[i] != null && loadedTemplates[i].DrawAboveMap == aboveMap)
                        loadedTemplates[i].Draw(g, rect, renderOpts, throwOnCancel, templateRecursionCount);

                    if (throwOnCancel != null)
                        throwOnCancel();
                }
            }
        }

        // Final all symbols entirely withing a given rectangle. Does not include holes.
        public Symbol[] SymbolsWithinRect(RectangleF rect)
        {
            CheckReadable();

            List<Symbol> list = new List<Symbol>();

            foreach (Symbol curSym in symbols) {
                if (IsSymdefVisible(curSym.Definition) &&
                    curSym.IsVisible &&
                    rect.Contains(curSym.BoundingBox)) 
                {
                    list.Add(curSym);
                }
            }

            return list.ToArray();
        }

        // Find all symbols that are hit within the given distance of point. 
        public SymbolHit[] HitTest(PointF point, float distance, MapHitTestOptions options)
        {
            CheckReadable();

            RectangleF testBox = new RectangleF(point.X - distance, point.Y - distance, distance * 2, distance * 2);
            List<SymbolHit> list = new List<SymbolHit>();
            float actualDistance;

            foreach (Symbol curSym in symbols) {
                if (IsSymdefVisible(curSym.Definition) &&
                    curSym.IsVisible && 
                    curSym.BoundingBox.IntersectsWith(testBox) && 
                    curSym.MayIntersectRect(testBox)) 
                {
                    int holeIndex;
                    if (curSym.HitTest(point, distance, options, out actualDistance, out holeIndex)) {
                        SymbolHit hit;
                        hit.symbol = (holeIndex >= 0) ? ((AreaLikeSymbol)curSym).GetHole(holeIndex) : curSym;
                        hit.distance = actualDistance;
                        hit.layer = SymbolLayer(curSym);
                        if (hit.layer >= 0)
                            list.Add(hit);
                    }
                }
            }

            if (list.Count == 0) {
                return null;
            }
            else {
                list.Sort((hit1, hit2) => CompareSymbolHits(hit1, hit2, distance));
                return list.ToArray();
            }
        }

        // Order in which symbols should be returned, based on type of symbols. The value indicates percentage bonus of hit distance.
        internal enum SymbolHitOrder { 
            Point = 80, 
            Line = 60, 
            Text = 20, 
            Bitmap = 15, 
            Area = 10,
            AreaHole = 0
        }

        // Symbol hit tests are ordered by:
        //    type of symbol (point/line/text/area) and distance...
        //    then by layer
        private int CompareSymbolHits(SymbolHit sh1, SymbolHit sh2, float distance)
        {
            // The adjusted distance takes into account that some symbols (area/text) are easier to tap
            // on than others (line and point). Otherwise it would be hard to tap on a point that is on
            // top of an area.
            float adjustedDistance1 = sh1.distance - ((int) sh1.HitOrder / 100F) * distance;
            float adjustedDistance2 = sh2.distance - ((int) sh2.HitOrder / 100F) * distance;

            if (adjustedDistance1 < adjustedDistance2)
                return -1;
            else if (adjustedDistance1 > adjustedDistance2)
                return 1;

            if (sh1.layer < sh2.layer)
                return -1;
            else if (sh1.layer > sh2.layer)
                return 1;

            return 0;
        }

        // Get the layer that a symbol is in. 0 is the layout layer, 1 through N are color layers (top to bottom), and N + 1 is the image layer. -1 is no layer (can this happen?)
        int SymbolLayer(Symbol sym)
        {
            SymDef def = sym.Definition;
            int layer = 0;

            if (def.HasColor(this.LayoutColor))
                return layer;

            ++layer;
            for (int iColor = colors.Count - 1; iColor >= 0; --iColor) {
                if (def.HasColor(colors[iColor]))
                    return layer;
                ++layer;
            }

            if (def.HasColor(this.ImageColor))
                return layer;

            return -1;
        }

        public void Dispose()
        {
            FreeLoadedTemplates();
        }

        #region Undo actions

        abstract class ChangeMapAction<T>: UndoableAction
        {
            protected readonly Map map;
            T before, after;

            protected ChangeMapAction(Map map, T before, T after)
            {
                this.map = map;
                this.before = before;
                this.after = after;
            }

            protected abstract void Modify(T from, T to);

            public override void Undo()
            {
                Modify(after, before);
            }

            public override void Redo()
            {
                Modify(before, after);
            }
        }


        abstract class ChangeMapCloneableAction<T> : UndoableAction
            where T: ICloneable
        {
            protected readonly Map map;
            T before, after;

            #pragma warning disable RECS0017 // Possible compare of value type with 'null'
            protected ChangeMapCloneableAction(Map map, T before, T after)
            {
                this.map = map;
                this.before = ((before == null) ? before : (T) before.Clone());
                this.after = ((after == null) ? after : (T) after.Clone());
            }
            #pragma warning restore RECS0017 // Possible compare of value type with 'null'

            protected abstract void Modify(T from, T to);

#pragma warning disable RECS0017 // Possible compare of value type with 'null'
            public override void Undo()
            {
                Modify(after, ((before == null) ? before : (T)before.Clone()));
            }

            public override void Redo()
            {
                Modify(before, ((after == null) ? after : (T)after.Clone()));
            }
#pragma warning restore RECS0017 // Possible compare of value type with 'null'
        }

        class ChangeFileInformationAction: ChangeMapAction<string>
        {
            public ChangeFileInformationAction(Map map, string before, string after)
                : base(map, before, after)
            { }

            protected override void Modify(string from, string to)
            {
                Debug.Assert(map.FileInformation == from);
                map.FileInformation = to;
            }
        }

        class ChangeMapScaleAction : ChangeMapAction<float>
        {
            public ChangeMapScaleAction(Map map, float before, float after)
                : base(map, before, after)
            {
            }

            protected override void Modify(float from, float to)
            {
                Debug.Assert(map.MapScale == from);
                map.MapScale = to;
            }
        }

        class ChangePrintScaleAction : ChangeMapAction<float>
        {
            public ChangePrintScaleAction(Map map, float before, float after)
                : base(map, before, after)
            {
            }

            protected override void Modify(float from, float to)
            {
                Debug.Assert(map.PrintScale == from);
                map.PrintScale = to;
            }
        }

        class ChangePrintAreaAction : ChangeMapAction<RectangleF>
        {
            public ChangePrintAreaAction(Map map, RectangleF before, RectangleF after)
                : base(map, before, after)
            {
            }

            protected override void Modify(RectangleF from, RectangleF to)
            {
                Debug.Assert(map.PrintArea == from);
                map.PrintArea = to;
            }
        }

        class ChangeTemplatesAction : ChangeMapAction<IList<TemplateInfo>>
        {
            public ChangeTemplatesAction(Map map, IList<TemplateInfo> before, IList<TemplateInfo> after)
                : base(map, Util.DeepCloneList(before), Util.DeepCloneList(after))
            { }

            protected override void Modify(IList<TemplateInfo> from, IList<TemplateInfo> to)
            {
                map.Templates = to;
            }
        }

        class ChangeHideTemplatesAction : ChangeMapAction<bool>
        {
            public ChangeHideTemplatesAction(Map map, bool before, bool after)
                : base(map, before, after)
            {
            }

            protected override void Modify(bool from, bool to)
            {
                Debug.Assert(map.HideTemplates == from);
                map.HideTemplates = to;
            }
        }

        class ChangeGpsReferenceInfoAction: ChangeMapAction<GpsReferenceInfo>
        {
            public ChangeGpsReferenceInfoAction(Map map, GpsReferenceInfo before, GpsReferenceInfo after)
                : base(map, before.Clone(), after.Clone())
            { }

            protected override void Modify(GpsReferenceInfo from, GpsReferenceInfo to)
            {
                map.GpsReferenceInfo = to;
            }
        }

        class ChangeMetricAction : ChangeMapAction<bool>
        {
            public ChangeMetricAction(Map map, bool before, bool after)
                : base(map, before, after)
            {
            }

            protected override void Modify(bool from, bool to)
            {
                Debug.Assert(map.UseEuclideanMetric == from);
                map.UseEuclideanMetric = to;
            }
        }

        class ChangeRealWorldCoordsAction : ChangeMapCloneableAction<RealWorldCoords>
        {
            public ChangeRealWorldCoordsAction(Map map, RealWorldCoords before, RealWorldCoords after)
                : base(map, before, after)
            {
            }

            protected override void Modify(RealWorldCoords from, RealWorldCoords to)
            {
                Debug.Assert(map.RealWorldCoords.Equals(from));
                map.RealWorldCoords = to;
            }
        }

        class ChangeColorMatrixAction : ChangeMapCloneableAction<ColorMatrix>
        {
            public ChangeColorMatrixAction(Map map, ColorMatrix before, ColorMatrix after)
                : base(map, before, after)
            {
            }

            protected override void Modify(ColorMatrix from, ColorMatrix to)
            {
                Debug.Assert(ColorMatrix.Equal(map.ColorMatrix, from));
                map.ColorMatrix = to;
            }
        }

        class ChangeColorTableAction: ChangeMapAction<SymColor>
        {
            int index;

            public ChangeColorTableAction(Map map, int index, SymColor from, SymColor to)
                :base(map, 
                      (from == null ? null : new SymColor(from.Layer).CopyFrom(from)), 
                      (to == null ? null : new SymColor(to.Layer).CopyFrom(to)))
            {
                this.index = index;
            }

            protected override void Modify(SymColor from, SymColor to)
            {
#if DEBUG
                if (from != null) {
                    SymColor existing = map.AllColors.ElementAt(index);
                    Debug.Assert(existing.Name == from.Name);
                    Debug.Assert(existing.OcadId == from.OcadId);
                    Debug.Assert(existing.CmykColor.Equals(from.CmykColor));
                    Debug.Assert(existing.OverPrint == from.OverPrint);
                }
#endif
                if (from == null) {
                    // Inserting at index.
                    map.AddColorAtIndex(index, to.Name, to.OcadId, to.CmykColor.Cyan, to.CmykColor.Magenta, to.CmykColor.Yellow, to.CmykColor.Black, to.OverPrint);
                }
                else if (to == null) {
                    // Deleting at index
                    map.RemoveColorAtIndex(index);
                }
                else {
                    // Changing color
                    map.ChangeColorAtIndex(index, to.Name, to.OcadId, to.CmykColor.Cyan, to.CmykColor.Magenta, to.CmykColor.Yellow, to.CmykColor.Black, to.OverPrint);
                }
            }
        }

        class SymbolChangeAction: ChangeMapAction<Symbol>
        {
            public SymbolChangeAction(Map map, Symbol before, Symbol after) 
                : base (map, before, after)
            {
                Debug.Assert(before == null || after == null);
            }

            protected override void Modify(Symbol from, Symbol to)
            {
                if (from == null) {
                    // Add symbol.
                    Debug.Assert(to.ContainingMap == null);
                    map.AddSymbol(to);
                }
                else if (to == null) {
                    // Remove symbol.
                    Debug.Assert(from.ContainingMap == map);
                    map.RemoveSymbol(from);
                }
            }
        }

        class SymdefChangeAction : ChangeMapAction<SymDef>
        {
            public SymdefChangeAction(Map map, SymDef before, SymDef after)
                : base(map, before, after)
            {
                Debug.Assert(before == null || after == null);
            }

            protected override void Modify(SymDef from, SymDef to)
            {
                if (from == null) {
                    // Add SymDef.
                    Debug.Assert(to.ContainingMap == null);
                    map.AddSymdef(to);
                }
                else if (to == null) {
                    // Remove SymDef.
                    Debug.Assert(from.ContainingMap == map);
                    map.RemoveSymdef(from);
                }
            }
        }

        class SymdefVisibleAction : ChangeMapAction<bool>
        {
            SymDef symdef;

            public SymdefVisibleAction(Map map, SymDef symdef, bool before, bool after)
                : base(map, before, after)
            {
                this.symdef = symdef;
            }

            protected override void Modify(bool from, bool to)
            {
                Debug.Assert(map.IsSymdefVisible(symdef) == from);
                map.SetSymdefVisible(symdef, to);
            }
        }



        #endregion
    }
    
    public enum RenderTemplateOption { MapOnly, TemplatesOnly, MapAndTemplates};

    // This class allows controlling rendering options.
    public class RenderOptions
    {
        public float minResolution;  // minimum size of a feature to render (typically the pixel size)
        public bool usePatternBitmaps; // if true, always use bitmap glyphs to render area patterns, if false, use direct drawing and clipping
        public RenderTemplateOption renderTemplates; // how templates (either OCAD or bitmap) will be displayed. Recursively applies to OCAD templates.
        public bool blendOverprintedColors; // if true, then use BlendMode.Darken to blend colors that have Overprint flag set to true.
                                            // if false, then Overprint flag is ignored.

        // debug options.
        public bool showSymbolBounds;      // Show the bounds of symbols.
    }

    // This class describes highlighting options.
    public class HighlightOptions
    {
        public double minResolution; // size of phyiscal pixels.
        public double logicalPixelSize; // size of logical pixels.
        public PointF pixelOrigin;  // origin of pixels, for positioning pixels

        public HighlightStyle style;  // highlight style 

        public HighlightOptions Clone()
        {
            return (HighlightOptions) base.MemberwiseClone();
        }
    }

    public enum HighlightStyle {
        HighFidelity,  // Fully matches normal drawing.
        LowFidelity     // Reduced fidelity, mostly for use with large amounts of multi-select present.
    }
}
