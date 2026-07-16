using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;

namespace PurplePen.Graphics2D
{
    public enum TextAlignment { Left, Center, Right }
    public enum VerticalTextAlignment { Top, Center, Bottom }

    public enum BlendMode { Darken }

    [Flags]
    public enum TextEffects { Regular = 0, Bold = 0x1, Italic = 0x2, Underline = 0x4, Strikeout = 0x8}

    public enum GraphicsPathPartKind { 
        Start,    // 1 point
        Lines,    // n points, n >= 1
        Beziers,   // 3n points n >= 1
        Close     // 0 points
    }

    public struct GraphicsPathPart
    {
        public readonly GraphicsPathPartKind Kind;
        public readonly PointF[] Points;

        public GraphicsPathPart(GraphicsPathPartKind kind, PointF[] points)
        {
            this.Kind = kind;
            this.Points = points;
        }
    }

    public interface ITextFaceMetrics : IDisposable
    {
        float EmHeight { get; }
        float Ascent { get; }
        float Descent { get; }
        float CapHeight { get; }
        float SpaceWidth { get; }
        float RecommendedLineSpacing { get; }
        float GetTextWidth(string text);
        SizeF GetTextSize(string text);
        RectangleF GetTightBoundingBox(PointF startPoint, string text); // Gets a tight bounding box for the text; based on the actual character shapes. So if there are no descenders, the bounding box won't include any space for descenders. 
    }

    public interface ITextMetrics : IDisposable
    {
        ITextFaceMetrics GetTextFaceMetrics(string familyName, float emHeight, TextEffects effects);
        bool TextFaceIsInstalled(string familyName);
    }

    // Represents different formats that bitmaps can be saved/loaded from.
    // Implementations don't necessary support all of these.
    public enum GraphicsBitmapFormat
    {
        None,
        PNG,
        JPEG,
        GIF,
        TIFF,
        BMP,
        WebP,
        Other,
        Unknown
    }
    
    // Encapsulates the ability to read bitmap from file or stream.
    public interface IGraphicsBitmapLoader : IDisposable
    {
        IGraphicsBitmap ReadBitmapFromStream(Stream stream);
        IGraphicsBitmap CreateEmptyBitmap(int width, int height, System.Drawing.Color? initialColor = null);
    }

    // If an IGraphicsBitmap is locked, it won't be disposed by another thread while locked.
    // e.g., Dispose() will take a lock.
    public interface IGraphicsBitmap : IDisposable
    {
        bool Disposed {get;}
        int PixelWidth { get; }
        int PixelHeight { get; }

        // If true, you must pass true to the copyBits arguement on 
        // GetGraphicsTarget.
        bool MustCopyBitsForGraphicsTarget { get; }

        // Resolution in dots per inch. Defaults to 96 if unknown.
        double HorizontalResolution { get; set; }
        double VerticalResolution { get; set; }

        // Get the format this bitmap was loaded from, if read from stream/file.
        // Otherwise, return None. Return Other if format not in the enum, or Unknown
        // if it was read from file but no way to know the format.
        GraphicsBitmapFormat GetOriginalFormat();

        // Get a single pixel. This is probably not that fast, so don't use repeatedly
        // too much in performance critical code.
        System.Drawing.Color GetPixel(int x, int y);

        IGraphicsBitmap Crop(int x, int y, int width, int height);
        bool WriteToStream(GraphicsBitmapFormat format, Stream stream, int quality);

        // If copyBits is false, the graphics target will draw directly to the existing bitmap. If copyBits is true,
        // the existing bitmap will be copied and the graphics target will draw to the copy. The existing bitmap will
        // not be changed, and the only way to get access to the copy is by calling FinishBitmap() on the graphics target. 
        // If MustCopyBitsForGraphicsTarget is true, then copyBits must be true (otherwise an ArgumentException is thrown).
        IBitmapGraphicsTarget GetGraphicsTarget(bool copyBits, IColorConverter colorConverter = null);
    }

    public interface IBitmapGraphicsTargetProvider: IDisposable
    {
        IBitmapGraphicsTarget CreateBitmapGraphicsTarget(int width, int height, CmykColor initialColor, IColorConverter colorConverter);
    }

    public interface IGraphicsTarget : IDisposable
    {
        // Get/set the intensity. Note that setting the intensity will cause
        // all brushes and pens to be destroyed.
        float Intensity { get; set; }

        // Prepend a transform to the graphics drawing target.
        void PushTransform(Matrix matrix);
        void PopTransform();

        // Set a clip on the graphics drawing target.
        void PushClip(object pathKey);
        void PushClip(RectangleF rectangle);
        void PushClip(RectangleF[] rectangles);
        void PushClip(List<GraphicsPathPart> parts, AreaFillMode windingMode);
        void PopClip();

        // Set aliasing mode
        void PushAntiAliasing(bool antiAlias);
        void PopAntiAliasing();

        // Set blending mode.
        bool PushBlending(BlendMode blendMode);

        void PopBlending();

        // Create paths.
        void CreatePath(object key, List<GraphicsPathPart> parts, AreaFillMode windingMode);

        // Create brushes and pens
        void CreateSolidBrush(object key, CmykColor color);
        bool SupportsPatternBrushes { get; }
        IBrushTarget CreatePatternBrush(SizeF size, float angle, int bitmapWidth, int bitmapHeight);
        void CreatePen(object key, object brushKey, float width, LineCapMode caps, LineJoinMode join, float miterLimit);
        void CreatePen(object key, CmykColor color, float width, LineCapMode caps, LineJoinMode join, float miterLimit);

        // Create font
        void CreateFont(object key, string familyName, float emHeight, TextEffects effects);

        // Determine if objects exist.
        bool HasPath(object pathKey);
        bool HasBrush(object brushKey);
        bool HasPen(object penKey);
        bool HasFont(object fontKey);

        // Draw an line with a pen.
        void DrawLine(object penKey, PointF start, PointF finish);

        // Draw an arc with a pen.
        void DrawArc(object penKey, PointF center, float radius, float startAngle, float sweepAngle);

        // Draw an ellipse with a pen.
        void DrawEllipse(object penKey, PointF center, float radiusX, float radiusY);

        // Fill an ellipse with a pen.
        void FillEllipse(object brushKey, PointF center, float radiusX, float radiusY);

        // Draw a rectangle with a pen.
        void DrawRectangle(object penKey, RectangleF rect);

        // Fill a rectangle with a brush.
        void FillRectangle(object brushKey, RectangleF rect);

        // Draw a polygon with a pen
        void DrawPolygon(object penKey, PointF[] pts);

        // Fill a polygon with a brush
        void DrawPolyline(object penKey, PointF[] pts);

        // Fill a polygon with a brush
        void FillPolygon(object brushKey, PointF[] pts, AreaFillMode windingMode);

        // Draw a path with a pen.
        void DrawPath(object penKey, object pathKey);
        void DrawPath(object penKey, List<GraphicsPathPart> parts);

        // Fill a path with a brush.
        void FillPath(object brushKey, object pathKey);
        void FillPath(object brushKey, List<GraphicsPathPart> parts, AreaFillMode windingMode);

        // Draw text with upper-left corner of text at the given locations.
        void DrawText(string text, object fontKey, object brushKey, PointF upperLeft);

        // Draw text outline with upper-left corner of text at the given locations.
        void DrawTextOutline(string text, object fontKey, object penKey, PointF upperLeft);

        // Draw a bitmap. minResolution how big in paper coords a pixel in the destination is. Used to set scaling for large bitmaps.
        void DrawBitmap(IGraphicsBitmap bm, RectangleF rectangle, BitmapScaling scalingMode);

        // Draw part of a bitmap
        // minResolution how big in paper coords a pixel in the destination is. Used to set scaling for large bitmaps.
        void DrawBitmapPart(IGraphicsBitmap bm, int x, int y, int width, int height, RectangleF rectange, BitmapScaling scalingMode);
    }

    public enum BitmapScaling { NearestNeighbor, MediumQuality, HighQuality }

    public interface IBrushTarget : IGraphicsTarget
    {
        void FinishBrush(object brushKey);
    }

    public interface IBitmapGraphicsTarget: IGraphicsTarget
    {
        int PixelWidth {get;}
        int PixelHeight {get;}
        IGraphicsBitmap FinishBitmap();
    }

    public interface IColorConverter
    {
        System.Drawing.Color ToColor(CmykColor cmykColor);
    }   

    public interface IFontLoader
    {
        bool FontFamilyIsInstalled(string familyName);
        void AddFontFile(string familyName, TextEffects textEffects, string fontFilePath);

        // Returns an array of all available font family names, combining both
        // private registered fonts and system fonts. 
        string[] GetFontFamilies();
    }
}
