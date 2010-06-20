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
        void PushClip(object pathKey);
        void PopClip();

        // Create paths.
        void CreatePath(object key, IEnumerable<GraphicsPathPart> parts, FillMode windingMode);

        // Create brushes and pens
        void CreateSolidBrush(object key, Color color);
        IBrushTarget CreatePatternBrush(SizeF size, int bitmapWidth, int bitmapHeight);
        void CreatePen(object key, object brushKey, float width, LineCap caps, LineJoin join, float miterLimit);
        void CreatePen(object key, Color color, float width, LineCap caps, LineJoin join, float miterLimit);

        // Create font
        void CreateFont(object key, string familyName, float emHeight, bool bold, bool italic);

        // Determine if objects exist.
        bool HasPath(object pathKey);
        bool HasBrush(object brushKey);
        bool HasPen(object penKey);
        bool HasFont(object fontKey);

        // Draw an line with a pen.
        void DrawLine(object penKey, PointF start, PointF finish);

        // Draw an arc with a pen.
        void DrawArc(object penKey, RectangleF boundingRect, float startAngle, float sweepAngle);

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
        void FillPolygon(object brushKey, PointF[] pts, FillMode windingMode);

        // Draw a path with a pen.
        void DrawPath(object penKey, object pathKey);

        // Fill a path with a brush.
        void FillPath(object brushKey, object pathKey);

        // Draw text with upper-left corner of text at the given locations.
        void DrawText(string text, object fontKey, object brushKey, PointF upperLeft);

        // Draw text outline with upper-left corner of text at the given locations.
        void DrawTextOutline(string text, object fontKey, object penKey, PointF upperLeft);

    }

    public interface IBrushTarget : IGraphicsTarget
    {
        void FinishBrush(object brushKey, float rotationAngle);
    }

}
