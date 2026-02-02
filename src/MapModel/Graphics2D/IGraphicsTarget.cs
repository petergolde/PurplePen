using System;
using System.Collections.Generic;
using System.Text;

using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;

namespace PurplePen.Graphics2D
{
    public enum BlendMode { Darken }

    [Flags]
    public enum TextEffects { None = 0, Bold = 0x1, Italic = 0x2, Underline = 0x4}

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
    }

    public interface ITextMetrics : IDisposable
    {
        ITextFaceMetrics GetTextFaceMetrics(string familyName, float emHeight, TextEffects effects);
        bool TextFaceIsInstalled(string familyName);
    }

    // If an IGraphicsBitmap is locked, it won't be disposed by another thread while locked.
    // e.g., Dispose() will take a lock.
    public interface IGraphicsBitmap : IDisposable
    {
        bool Disposed {get;}
        int PixelWidth { get; }
        int PixelHeight { get; }
    }

    public interface IBitmapGraphicsTargetProvider: IDisposable
    {
        IBitmapGraphicsTarget CreateBitmapGraphicsTarget(int width, int height);
    }

    public interface IGraphicsTarget : IDisposable
    {
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
        void DrawBitmap(IGraphicsBitmap bm, RectangleF rectangle, BitmapScaling scalingMode, float minResolution);

        // Draw part of a bitmap
        // minResolution how big in paper coords a pixel in the destination is. Used to set scaling for large bitmaps.
        void DrawBitmapPart(IGraphicsBitmap bm, int x, int y, int width, int height, RectangleF rectange, BitmapScaling scalingMode, float minResolution);
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

}
