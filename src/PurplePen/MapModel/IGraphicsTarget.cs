using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using Matrix = System.Drawing.Drawing2D.Matrix;
using FillMode = System.Drawing.Drawing2D.FillMode;
using LineJoin = System.Drawing.Drawing2D.LineJoin;
using LineCap = System.Drawing.Drawing2D.LineCap;
using Color = System.Drawing.Color;

namespace PurplePen.MapModel
{
    public interface IGraphicsBrush : IDisposable
    {
    }

    public interface IGraphicsPen : IDisposable
    {
    }

    public interface IGraphicsPath : IDisposable
    { }

    public interface IGraphicsFont : IDisposable
    { }

    public enum GraphicsPathPartKind { 
        Start,    // 1 point
        Lines,    // n points, n >= 1
        Beziers,   // 3n points n >= 1
        Close     // 0 points
    };

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
        float GetTextWidth(string text);
        SizeF GetTextSize(string text);
    }

    public interface ITextMetrics : IDisposable
    {
        ITextFaceMetrics GetTextFaceMetrics(string familyName, float emHeight, bool bold, bool italic);
        bool TextFaceIsInstalled(string familyName);
    }

    public interface IGraphicsTarget : IDisposable
    {
        // Prepend a transform to the graphics drawing target.
        void PushTransform(Matrix matrix);
        void PopTransform();

        // Set a clip on the graphics drawing target.
        void PushClip(IGraphicsPath geometry);
        void PopClip();

        // Create paths.
        IGraphicsPath CreatePath(IEnumerable<GraphicsPathPart> parts, FillMode windingMode);

        // Create brushes and pens
        IGraphicsBrush CreateSolidBrush(Color color);
        IBrushTarget CreatePatternBrush(SizeF size, int bitmapWidth, int bitmapHeight);
        IGraphicsPen CreatePen(IGraphicsBrush brush, float width, LineCap caps, LineJoin join, float miterLimit);
        IGraphicsPen CreatePen(Color color, float width, LineCap caps, LineJoin join, float miterLimit);

        // Create font
        IGraphicsFont CreateFont(string familyName, float emHeight, bool bold, bool italic);

        // Draw an line with a pen.
        void DrawLine(IGraphicsPen pen, PointF start, PointF finish);

        // Draw an arc with a pen.
        void DrawArc(IGraphicsPen pen, RectangleF boundingRect, float startAngle, float sweepAngle);

        // Draw an ellipse with a pen.
        void DrawEllipse(IGraphicsPen pen, PointF center, float radiusX, float radiusY);

        // Fill an ellipse with a pen.
        void FillEllipse(IGraphicsBrush brush, PointF center, float radiusX, float radiusY);

        // Draw a rectangle with a pen.
        void DrawRectangle(IGraphicsPen pen, RectangleF rect);

        // Fill a rectangle with a brush.
        void FillRectangle(IGraphicsBrush brush, RectangleF rect);

        // Draw a polygon with a pen
        void DrawPolygon(IGraphicsPen pen, PointF[] pts);

        // Fill a polygon with a brush
        void DrawPolyline(IGraphicsPen pen, PointF[] pts);

        // Fill a polygon with a brush
        void FillPolygon(IGraphicsBrush brush, PointF[] pts, FillMode windingMode);

        // Draw a path with a pen.
        void DrawPath(IGraphicsPen pen, IGraphicsPath path);

        // Fill a path with a brush.
        void FillPath(IGraphicsBrush brush, IGraphicsPath path);

        // Draw text with upper-left corner of text at the given locations.
        void DrawText(string text, IGraphicsFont font, IGraphicsBrush brush, PointF upperLeft);

        // Draw text outline with upper-left corner of text at the given locations.
        void DrawTextOutline(string text, IGraphicsFont font, IGraphicsPen pen, PointF upperLeft);

    }

    public interface IBrushTarget : IGraphicsTarget
    {
        IGraphicsBrush FinishBrush(float rotationAngle);
    }

}
