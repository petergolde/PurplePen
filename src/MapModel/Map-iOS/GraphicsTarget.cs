using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;

using Foundation;
using UIKit;
using CoreGraphics;
using CoreText;

using PurplePen.MapModel;
using System.Collections.Concurrent;
using CoreImage;
using System.Threading;

namespace PurplePen.MapModel
{
    using PurplePen.Graphics2D;
    using System.IO;

    // A GraphicsTarget encapsulates a CGContext
    public class IOS_GraphicsTarget: IGraphicsTarget
    {
        public CGContext Context;
        private float intensity;
        private Dictionary<object, Pen> penMap = new Dictionary<object, Pen>(new IdentityComparer<object>());
        private Dictionary<object, Brush> brushMap = new Dictionary<object, Brush>(new IdentityComparer<object>());
        private Dictionary<object, IOS_Font> fontMap = new Dictionary<object, IOS_Font>(new IdentityComparer<object>());
        private Dictionary<object, Path> pathMap = new Dictionary<object, Path>(new IdentityComparer<object>());

        static CGColorSpace cmykColorSpace = CGColorSpace.CreateDeviceCMYK();

        public IOS_GraphicsTarget(CGContext context, float intensity)
        {
            this.Context = context;
            this.intensity = intensity;

            Context.TextMatrix = CGAffineTransform.MakeScale(1F, -1F);  // draw text normal way around.
        }

        public IOS_GraphicsTarget(CGContext context) : this(context, 1.0F)
        {
        }

        public float Intensity
        {
            get { return intensity; }
        }

        private float CurrentPixelSize {
            get { 
                CGAffineTransform transform = Context.GetCTM(); 
                return 1.0F / (float) Math.Sqrt((transform.xx * transform.xx) + (transform.xy * transform.xy));
            }
        }

        private CGColor ConvertColor(CmykColor cmykColor)
        {
            if (intensity < 1.0F) {
                cmykColor = CmykColor.FromCmyka(cmykColor.Cyan * intensity, cmykColor.Magenta * intensity, cmykColor.Yellow * intensity, cmykColor.Black * intensity, cmykColor.Alpha);
            }

            return ToCGColor(cmykColor);
        }

        public static CGColor ToCGColor(CmykColor cmykColor)
        {
            return new CGColor(cmykColorSpace, new nfloat[5] { cmykColor.Cyan, cmykColor.Magenta, cmykColor.Yellow, cmykColor.Black, cmykColor.Alpha} );
        }

        public void CreateSolidBrush(object brushKey, CmykColor color)
        {
            if (HasBrush(brushKey))
                throw new InvalidOperationException("Key already has a brush created for it");

            brushMap.Add(brushKey, new SolidBrush(ConvertColor(color)));
        }

        public bool SupportsPatternBrushes
        {
            get { return true; }
        }

        public IBrushTarget CreatePatternBrush(SizeF size, float angle, int bitmapWidth, int bitmapHeight)
        {
            return IOS_BrushTarget.Create(this, size, angle, bitmapWidth, bitmapHeight);
        }

        public void CreatePen(object penKey, object brushKey, float width, LineCap cap, LineJoin join, float miterLimit)
        {
            if (penMap.ContainsKey(penKey))
                throw new InvalidOperationException("Key already has a pen created for it");

            Brush brush = GetBrush(brushKey);
            if (! brush.CanBeStroke)
                throw new NotSupportedException("Pattern brushes cannot be used for a pen on iOS");
            
            Pen pen = new Pen(brush, width, join, cap, miterLimit);
            
            penMap.Add(penKey, pen);
        }

        public void CreatePen(object penKey, CmykColor color, float width, LineCap cap, LineJoin join, float miterLimit)
        {
            if (penMap.ContainsKey(penKey))
                throw new InvalidOperationException("Key already has a pen created for it");

            Pen pen = new Pen(new SolidBrush(ConvertColor(color)), width, join, cap, miterLimit);

            penMap.Add(penKey, pen);
        }

        // Create font
        public void CreateFont(object fontKey, string familyName, float emHeight, bool bold, bool italic)
        {
            if (fontMap.ContainsKey(fontKey))
                throw new InvalidOperationException("Key already has a font created for it");

            IOS_Font font = new IOS_Font(familyName, emHeight, bold, italic);

            fontMap.Add(fontKey, font);
        }

        public void CreatePath(object pathKey, List<GraphicsPathPart> parts, FillMode windingMode)
        {
            // NOTE: This should be maintained together with AddToCurrentPath, as
            // NOTE: they are almost the same.

            if (pathMap.ContainsKey(pathKey))
                throw new InvalidOperationException("Key already has a path created for it");

            float bezierError = 0.9F * CurrentPixelSize;  // Looks good enough.

            CGPath path = new CGPath();
            List<PointF> points = new List<PointF>();

            for (int iPart = 0; iPart < parts.Count; ++iPart)
            {
                GraphicsPathPart part = parts[iPart];

                switch (part.Kind)
                {
                    case GraphicsPathPartKind.Start:
                        Debug.Assert(part.Points.Length == 1);

                        if (points.Count >= 2) {
                            path.AddLines(CGPointArray(points));
                        }
                        points.Clear();

                        points.Add(part.Points[0]);
                        break;

                    case GraphicsPathPartKind.Lines:
                        points.AddRange(part.Points);

                        break;

                    case GraphicsPathPartKind.Beziers:
                        PointF lastPoint = points[points.Count - 1];
                        points.RemoveAt(points.Count - 1);

                        for (int i = 0; i < part.Points.Length; i += 3) {
                            Bezier bezier = new Bezier(lastPoint, part.Points[i], part.Points[i + 1], part.Points[i + 2]);
                            bezier.Flatten(bezierError, points);
                            lastPoint = part.Points[i + 2];
                        }

                        break;

                    case GraphicsPathPartKind.Close:
                        if (points.Count >= 2) {
                            path.AddLines(CGPointArray(points));
                        }
                        points.Clear();
                        path.CloseSubpath();
                        break;
                }
            }

            if (points.Count >= 2) {
                path.AddLines(CGPointArray(points));
            }
            points.Clear();   

            pathMap.Add(pathKey, new Path(path, windingMode));
        }

        private CGPoint[] CGPointArray(List<PointF> points)
        {
            CGPoint[] cgPoints = new CGPoint[points.Count];
            for (int i = 0; i < points.Count; ++i)
                cgPoints[i] = points[i];
            return cgPoints;
        }

        private CGPoint[] CGPointArray(PointF[] points)
        {
            CGPoint[] cgPoints = new CGPoint[points.Length];
            for (int i = 0; i < points.Length; ++i)
                cgPoints[i] = points[i];
            return cgPoints;
        }

        private void AddToCurrentPath(List<GraphicsPathPart> parts)
        {
            // NOTE: This should be maintained together with CreatePath, as
            // NOTE: they are almost the same.

            float bezierError = 0.9F * CurrentPixelSize;  // Looks good enough.

            List<PointF> points = new List<PointF>();

            for (int iPart = 0; iPart < parts.Count; ++iPart)
            {
                GraphicsPathPart part = parts[iPart];

                switch (part.Kind)
                {
                    case GraphicsPathPartKind.Start:
                        Debug.Assert(part.Points.Length == 1);

                        if (points.Count >= 2)
                            Context.AddLines(CGPointArray(points));
                        points.Clear();

                        points.Add(part.Points[0]);
                        break;

                    case GraphicsPathPartKind.Lines:
                        points.AddRange(part.Points);

                        break;

                    case GraphicsPathPartKind.Beziers:
                        PointF lastPoint = points[points.Count - 1];
                        points.RemoveAt(points.Count - 1);

                        for (int i = 0; i < part.Points.Length; i += 3) {
                            Bezier bezier = new Bezier(lastPoint, part.Points[i], part.Points[i + 1], part.Points[i + 2]);
                            bezier.Flatten(bezierError, points);
                            lastPoint = part.Points[i + 2];
                        }

                        break;

                    case GraphicsPathPartKind.Close:
                        if (points.Count >= 2)
                            Context.AddLines(CGPointArray(points));
                        points.Clear();
                        Context.ClosePath();
                        break;
                }
            }

            if (points.Count >= 2)
                Context.AddLines(CGPointArray(points));
            points.Clear();   
        }

        // Prepend a transform to the graphics drawing target.
        public void PushTransform(Matrix matrix)
        {
            Context.SaveState();
            Context.ConcatCTM(ToCGAffineTransform(matrix));
        }

        // Pop the transform
        public void PopTransform()
        {
            Context.RestoreState();
        }

        // Set a clip on the graphics drawing target.
        public void PushClip(object pathKey)
        {
            Path path = GetPath(pathKey);

            Context.SaveState();
            Context.BeginPath();
            Context.AddPath(path.CGPath);
            if (path.FillMode == FillMode.Alternate)
                Context.EOClip();
            else
                Context.Clip();
        }

        public void PushClip(RectangleF clipRect)
        {
            Context.SaveState();
            Context.ClipToRect(clipRect);
        }

        public void PushClip(RectangleF[] clipRects)
        {
            CGRect[] cgRects = new CGRect[clipRects.Length];
            for (int i = 0; i < clipRects.Length; ++i)
                cgRects[i] = clipRects[i];

            Context.SaveState();
            Context.ClipToRects(cgRects);
        }

        public void PushClip(List<GraphicsPathPart> parts, FillMode fillMode)
        {
            Context.SaveState();

            Context.BeginPath();
            AddToCurrentPath(parts);
            if (fillMode == FillMode.Alternate)
                Context.EOClip();
            else
                Context.Clip();
        }

        // Pop the clip.
        public void PopClip()
        {
            Context.RestoreState();
        }

        // Push an anti-aliasing mode.
        public void PushAntiAliasing(bool antiAlias)
        {
            Context.SaveState();
            Context.SetShouldAntialias(antiAlias);
        }

        // Pop anti-aliases mode.
        public void PopAntiAliasing()
        {
            Context.RestoreState();
        }

        // Draw an line with a pen.
        public void DrawLine(object penKey, PointF start, PointF finish)
        {
            Pen pen = GetPen(penKey);
            pen.SetAsStroke(Context);
            Context.StrokeLineSegments(new CGPoint[2] {start, finish});
        }

        // Blending isn't supported right now.
        public bool PushBlending(BlendMode blendMode)
        { return false; }

        public void PopBlending()
        {}

        // Draw an arc with a pen.
        public void DrawArc(object penKey, PointF center, float radius, float startAngle, float sweepAngle)
        {
            float startRadians = (float) (startAngle * Math.PI / 180.0);
            float sweepRadians = (float) (sweepAngle * Math.PI / 180.0);
            float endRadians = startRadians + sweepRadians;

            Pen pen = GetPen(penKey);
            pen.SetAsStroke(Context);
            Context.BeginPath();
            Context.AddArc(center.X, center.Y, radius, startRadians, endRadians, sweepAngle < 0);
            Context.StrokePath();
        }

        // Draw an ellipse with a pen.
        public void DrawEllipse(object penKey, PointF center, float radiusX, float radiusY)
        {
            Pen pen = GetPen(penKey);
            pen.SetAsStroke(Context);
            Context.StrokeEllipseInRect(new RectangleF(center.X - radiusX, center.Y - radiusY, 2 * radiusX, 2 * radiusY));
        }

        // Fill an ellipse with a brush.
        public void FillEllipse(object brushKey, PointF center, float radiusX, float radiusY)
        {
            Brush brush = GetBrush(brushKey);

            if (brush.CanBeFill) {
                brush.SetAsFill(Context);
                Context.FillEllipseInRect(new RectangleF(center.X - radiusX, center.Y - radiusY, 2 * radiusX, 2 * radiusY));
            }
            else {
                Context.BeginPath();
                Context.AddEllipseInRect(new RectangleF(center.X - radiusX, center.Y - radiusY, 2 * radiusX, 2 * radiusY));
                brush.FillCurrentPath(Context, false, this.CurrentPixelSize);
            }
        }

        // Draw a rectangle with a pen.
        public void DrawRectangle(object penKey, RectangleF rect)
        {
            Pen pen = GetPen(penKey);
            pen.SetAsStroke(Context);
            Context.StrokeRect(rect);
        }

        // Fill a rectangle with a brush.
        public void FillRectangle(object brushKey, RectangleF rect)
        {
            Brush brush = GetBrush(brushKey);

            if (brush.CanBeFill) {
                brush.SetAsFill(Context);
                Context.FillRect(rect);
            }
            else {
                Context.BeginPath();
                Context.AddRect(rect);
                brush.FillCurrentPath(Context, false, this.CurrentPixelSize);
            }
        }

        // Draw a polygon with a brush
        public void DrawPolygon(object penKey, PointF[] pts)
        {
            Pen pen = GetPen(penKey);
            pen.SetAsStroke(Context);
            Context.BeginPath();
            Context.AddLines(CGPointArray(pts));
            Context.ClosePath();
            Context.StrokePath();
        }

        // Draw lines with a brush
        public void DrawPolyline(object penKey, PointF[] pts)
        {
            Pen pen = GetPen(penKey);
            pen.SetAsStroke(Context);

            Context.BeginPath();
            Context.AddLines(CGPointArray(pts));
            Context.StrokePath();
        }

        // Fill a polygon with a brush
        public void FillPolygon(object brushKey, PointF[] pts, FillMode windingMode)
        {
            Brush brush = GetBrush(brushKey);

            Context.BeginPath();
            Context.AddLines(CGPointArray(pts));
            brush.FillCurrentPath(Context, windingMode == FillMode.Alternate, this.CurrentPixelSize);
        }

        // Draw a path with a pen.
        public void DrawPath(object penKey, object pathKey)
        {
            Path path = GetPath(pathKey);
            Pen pen = GetPen(penKey);

            pen.SetAsStroke(Context);
            Context.BeginPath();
            Context.AddPath(path.CGPath);
            Context.StrokePath();
        }

        public void DrawPath(object penKey, List<GraphicsPathPart> parts)
        {
            Pen pen = GetPen(penKey);

            pen.SetAsStroke(Context);
            Context.BeginPath();
            AddToCurrentPath(parts);
            Context.StrokePath();
        }

        // Fill a path with a brush.
        public void FillPath(object brushKey, object pathKey)
        {
            Path path = GetPath(pathKey);
            Brush brush = GetBrush(brushKey);

            Context.BeginPath();
            Context.AddPath(path.CGPath);
            brush.FillCurrentPath(Context, path.FillMode == FillMode.Alternate, this.CurrentPixelSize);
        }

        public void FillPath(object brushKey, List<GraphicsPathPart> parts, FillMode fillMode)
        {
            Brush brush = GetBrush(brushKey);

            Context.BeginPath();
            AddToCurrentPath(parts);
            brush.FillCurrentPath(Context, fillMode == FillMode.Alternate, this.CurrentPixelSize);
        }

        // Draw text with upper-left corner of text at the given locations.
        public void DrawText(string text, object fontKey, object brushKey, PointF upperLeft)
        {
            IOS_Font font = GetFont(fontKey);
            Brush brush = GetBrush(brushKey);

            Context.SetTextDrawingMode(CGTextDrawingMode.Fill);
            brush.SetAsFill(Context);
            Context.TextPosition = new PointF(upperLeft.X, upperLeft.Y + font.Ascent);

            using (CTLine line = font.GetCTLine(text))
                line.Draw(Context);
        }

        // Draw text outline with upper-left corner of text at the given locations.
        public void DrawTextOutline(string text, object fontKey, object penKey, PointF upperLeft)
        {
            IOS_Font font = GetFont(fontKey);
            Pen pen = GetPen(penKey);
            
            Context.SetTextDrawingMode(CGTextDrawingMode.Stroke);
            pen.SetAsStroke(Context);
            Context.TextPosition = new PointF(upperLeft.X, upperLeft.Y + font.Ascent);
            
            using (CTLine line = font.GetCTLine(text))
                line.Draw(Context);
        }

        // Draw a bitmap
        public void DrawBitmap(IGraphicsBitmap bm, RectangleF rectangle, BitmapScaling scalingMode, float minResolution)
        {
            if (bm is IOS_LargeBitmap) {
                DrawBitmapPart(bm, 0, 0, bm.PixelWidth, bm.PixelHeight, rectangle, scalingMode, minResolution);
            }
            else {
                lock (bm) {
                    IOS_Bitmap bitmap = (IOS_Bitmap)bm;
                    CGImage image = bitmap.CGImage;

                    if (image != null) {
                        Context.SaveState(); // Save interpolationQuality and transform matrix (CTM)

                        Context.InterpolationQuality = ToCGInterpolationQuality(scalingMode);

                        // Flip coordinate system.
                        PointF center = rectangle.Center();
                        Context.TranslateCTM(center.X, center.Y);
                        Context.ScaleCTM(1, -1);
                        Context.TranslateCTM(-center.X, -center.Y);

                        Context.DrawImage(rectangle, image);

                        Context.RestoreState();
                    }
                }
            }
        }

        // Draw part of a bitmap
        public void DrawBitmapPart(IGraphicsBitmap bm, int x, int y, int width, int height, RectangleF rectangle, BitmapScaling scalingMode, float minResolution)
        {
            lock (bm) {
                CGImage partImage = null;
                bool shouldClip = false;
                RectangleF clipRectangle = new RectangleF();

                if (bm is IOS_Bitmap) {
                    IOS_Bitmap bitmap = (IOS_Bitmap)bm;

                    CGImage fullImage = bitmap.CGImage;
                    if (fullImage != null)
                        partImage = fullImage.WithImageInRect(new CGRect(x, y, width, height));
                }
                else if (bm is IOS_LargeBitmap) {
                    IOS_LargeBitmap bitmap = (IOS_LargeBitmap)bm;
                    RectangleF actualRectangle;
                    long maxScale;

                    if (minResolution == 0)
                        maxScale = 1;
                    else {
                        maxScale = (long)Math.Ceiling(Math.Max(width / (rectangle.Width / minResolution), height / (rectangle.Height / minResolution)));
                        if (maxScale < 1)
                            maxScale = 1;
                    }

                    partImage = bitmap.GetPart(x, y, width, height, maxScale, out actualRectangle);
                    if (actualRectangle.X != x || actualRectangle.Y != y || actualRectangle.Width != width || actualRectangle.Height != height) {
                        // Due to scaling, we need to slightly expand the rectangle we are drawing into, but clip back to the actual
                        // rectangle.
                        shouldClip = true;
                        clipRectangle = rectangle;
                        RectangleF srcRect = new RectangleF(x, y, width, height);
                        Matrix transform = Geometry.CreateRectangleTransform(srcRect, rectangle);
                        rectangle = Geometry.TransformRectangle(transform, actualRectangle);
                    }
                    else {
                        shouldClip = true;
                        clipRectangle = rectangle;
                    }
                }
                else {
                    Debug.Fail("Unexpected bitmap class");
                }

                if (partImage != null) {
                    Context.SaveState(); // Save interpolationQuality and transform matrix (CTM)
                    try {

                        Context.InterpolationQuality = ToCGInterpolationQuality(scalingMode);

                        if (shouldClip) {
                            Context.ClipToRect(clipRectangle);
                        }
                    
                        // Flip coordinate system before drawing bitmap.
                        PointF center = rectangle.Center();

                        Context.TranslateCTM(center.X, center.Y);
                        Context.ScaleCTM(1, -1);
                        Context.TranslateCTM(-center.X, -center.Y);

                        Context.DrawImage(rectangle, partImage);
                    }
                    finally {
                        Context.RestoreState();
                        partImage.Dispose();
                    }
                }
            }
        }

        public bool HasPath(object pathKey)
        {
            return pathMap.ContainsKey(pathKey);
        }

        public bool HasPen(object penKey) {
             return penMap.ContainsKey(penKey);
        }

        public bool HasBrush(object brushKey) {
            return brushMap.ContainsKey(brushKey);
        }

        public bool HasFont(object fontKey) {
            return fontMap.ContainsKey(fontKey);
        }

        private Brush GetBrush(object brushKey) {
            Brush brush;
            if (brushMap.TryGetValue(brushKey, out brush))
                return brush;
            else
                throw new ArgumentException("Given key does not have a brush created for it", "brushKey");
        }

        private Pen GetPen(object penKey) {
            Pen pen;
            if (penMap.TryGetValue(penKey, out pen))
                return pen;
            else
                throw new ArgumentException("Given key does not have a pen created for it", "penKey");
        }

        private IOS_Font GetFont(object fontKey) {
            IOS_Font font;
            if (fontMap.TryGetValue(fontKey, out font))
                return font;
            else
                throw new ArgumentException("Given key does not have a font created for it", "fontKey");
        }

        private Path GetPath(object pathKey) {
            Path path;
            if (pathMap.TryGetValue(pathKey, out path))
                return path;
            else
                throw new ArgumentException("Given key does not have a path created for it", "pathKey");
        }

        private static CGLineJoin ToCGLineJoin(LineJoin join)
        {
            switch (join) {
                case LineJoin.Bevel: return CGLineJoin.Bevel;
                case LineJoin.Miter: return CGLineJoin.Miter;
                case LineJoin.MiterClipped: return CGLineJoin.Miter;
                case LineJoin.Round: return CGLineJoin.Round;
                default: return CGLineJoin.Round;
            }
        }

        private static CGLineCap ToCGLineCap(LineCap cap)
        {
            switch (cap) {
                case LineCap.Flat: return CGLineCap.Butt;
                case LineCap.Square: return CGLineCap.Square;
                case LineCap.Round: return CGLineCap.Round;
                default: return CGLineCap.Butt;
            }
        }

        private CGInterpolationQuality ToCGInterpolationQuality(BitmapScaling scalingMode)
        {
            switch (scalingMode)
            {
                case BitmapScaling.NearestNeighbor:
                    return CGInterpolationQuality.None;
                case BitmapScaling.MediumQuality:
                    return CGInterpolationQuality.Low;
                case BitmapScaling.HighQuality:
                    return CGInterpolationQuality.High;
                default:
                    return CGInterpolationQuality.Low;
            }
        }

        public static CGAffineTransform ToCGAffineTransform(Matrix matrix)
        {
            float[] elements = matrix.Elements;
            return new CGAffineTransform(elements[0], elements[1], elements[2], elements[3], elements[4], elements[5]);
        }

        public void Dispose()
        {
            foreach (Pen pen in penMap.Values)
                pen.Dispose();
            penMap.Clear();

            foreach (Brush brush in brushMap.Values)
                brush.Dispose();
            brushMap.Clear();

            foreach (Path path in pathMap.Values)
                path.Dispose();
            pathMap.Clear();

            foreach (IOS_Font font in fontMap.Values)
                font.Dispose();
            fontMap.Clear();
        }

        private class Pen: IDisposable
        {
            private Brush brush;
            private float width;
            private LineJoin join;
            private LineCap cap;
            private float miterLimit;

            public Pen(Brush brush, float width, LineJoin join, LineCap cap, float miterLimit)
            {
                this.brush = brush;
                this.width = width;
                this.join = join;
                this.cap = cap;
                this.miterLimit = miterLimit;
            }

            public void SetAsStroke(CGContext context)
            {
                brush.SetAsStroke(context);
                context.SetLineWidth(width);
                context.SetLineJoin(ToCGLineJoin(join));
                context.SetLineCap(ToCGLineCap(cap));
                context.SetMiterLimit(miterLimit);
            }

            public void Dispose()
            {
                brush.Dispose();
            }
        }

        private class Path: IDisposable
        {
            public CGPath CGPath;
            public FillMode FillMode;
            
            public Path(CGPath path, FillMode fillMode)
            {
                this.CGPath = path;
                this.FillMode = fillMode;
            }
            
            public void Dispose()
            {
                CGPath.Dispose();
            }
        }
        
        private abstract class Brush: IDisposable
        {
            public abstract void SetAsFill(CGContext context);

            public abstract void SetAsStroke(CGContext context);

            public abstract void FillCurrentPath(CGContext context, bool evenOddFill, float contextPixelSize);

            public abstract bool CanBeStroke {get; }
            public abstract bool CanBeFill {get; }

            public virtual void Dispose()
            {
            }
        }

        private class SolidBrush: Brush
        {
            private CGColor color;

            public SolidBrush(CGColor color)
            {
                this.color = color;
            }

            public override bool CanBeFill
            {
                get
                {
                    return true;
                }
            }

            public override bool CanBeStroke
            {
                get
                {
                    return true;
                }
            }

            public override void SetAsFill(CGContext context)
            {
                context.SetFillColor(color);
            }

            public override void SetAsStroke(CGContext context)
            {
                context.SetStrokeColor(color);
            }

            public override void FillCurrentPath(CGContext context, bool evenOddFill, float contextPixelSize)
            {
                context.SetFillColor(color);
                if (evenOddFill)
                    context.EOFillPath();
                else
                    context.FillPath();
            }

            public override void Dispose()
            {
                color.Dispose();
            }
        }

        private class PatternBrush: Brush
        {
            private CGImage bitmap;
            private RectangleF rect;
            private float angle;
            private float pixelSize;

            public PatternBrush(UIImage bitmap, SizeF size, float angle, float pixelSize)
            {
                this.bitmap = bitmap.CGImage;
                this.rect = new RectangleF(new PointF(-size.Width / 2, -size.Height / 2), size);
                this.angle = angle;
                this.pixelSize = pixelSize;
            }

            public override bool CanBeFill
            {
                get
                {
                    return false;
                }
            }
            
            public override bool CanBeStroke
            {
                get
                {
                    return false;
                }
            }

            public override void SetAsFill(CGContext context)
            {
                throw new NotSupportedException("Pattern brush cannot be fill on iOS");
            }

            public override void SetAsStroke(CGContext context)
            {
                throw new NotSupportedException("Pattern brush cannot be stroke on iOS");
            }

            public override void FillCurrentPath(CGContext context, bool evenOddFill, float contextPixelSize)
            {
                // I tested using CGPattern and FillPath, and it wasn't any faster than DrawTiledImage, looked worse,
                // and also had similar rounding issues.

                context.SaveState();
                if (evenOddFill)
                    context.EOClip();
                else
                    context.Clip();

                CGRect bounding = context.GetClipBoundingBox();

                if (!bounding.IsEmpty) {
                    if (pixelSize < contextPixelSize / 3)
                        context.InterpolationQuality = CGInterpolationQuality.Medium;  // prevent Moire effects.
                    else
                        context.InterpolationQuality = CGInterpolationQuality.None;  // fastest when zoomed in enough that Moire effects aren't a problem.

                    if (angle != 0)
                        context.RotateCTM((float)(angle * Math.PI / 180F));
                       
                    // This code is the equivalent of DrawTiledImage. It is slowed, but DrawTiledImage
                    // optimized the 0/90/180/270 degree case too much, and rounding errors. We only use
                    // DrawTiledImage in the non-right angle case.
                    if (Math.Abs(angle % 90) < 1) {

                        int minx = (int)Math.Floor((bounding.GetMinX() - rect.X) / rect.Width);
                        int miny = (int)Math.Floor((bounding.GetMinY() - rect.Y) / rect.Height); 
                        int maxx = (int)Math.Ceiling((bounding.GetMaxX() - rect.X) / rect.Width);
                        int maxy = (int)Math.Ceiling((bounding.GetMaxY() - rect.Y) / rect.Height);
                       Debug.Assert(maxy > miny && maxx > minx);
                        
                        for (int x = minx; x < maxx; ++x) {
                            for (int y = miny; y < maxy; ++y) {
                                context.DrawImage(new RectangleF(x * rect.Width + rect.X, y * rect.Height + rect.Y, rect.Width, rect.Height), bitmap);
                            }
                        }
                    }
                    else {
                        context.DrawTiledImage(rect, bitmap);
                    }
                }

                context.RestoreState();
            }

            public override void Dispose()
            {
                if (bitmap != null) {
                    bitmap.Dispose();
                    bitmap = null;
                }
            }
        }

        private class IOS_BrushTarget : IOS_GraphicsTarget, IBrushTarget
        {
            private IOS_GraphicsTarget owningTarget;
            private SizeF size;
            private float angle;
            private float pixelSize;

            public static IOS_BrushTarget Create(IOS_GraphicsTarget owningTarget, SizeF size, float angle, int bitmapWidth, int bitmapHeight)
            {
                UIGraphics.BeginImageContextWithOptions(new SizeF(bitmapWidth, bitmapHeight), false, 1.0F);
                CGContext context = UIGraphics.GetCurrentContext();
                context.TranslateCTM((float)bitmapWidth / 2F, (float)bitmapHeight / 2F);
                context.ScaleCTM((float)bitmapWidth / size.Width, -(float)bitmapHeight / size.Height);
                return new IOS_BrushTarget(owningTarget, size, angle, size.Width / bitmapWidth);
            }

            private IOS_BrushTarget(IOS_GraphicsTarget owningTarget, SizeF size, float angle, float pixelSize)
            : base(UIGraphics.GetCurrentContext()) 
            {
                this.owningTarget = owningTarget;
                this.size = size;
                this.angle = angle;
                this.pixelSize = pixelSize;
                this.intensity = owningTarget.intensity;
            }

            public void FinishBrush(object brushKey) {
                UIImage bitmap = UIGraphics.GetImageFromCurrentImageContext();
                UIGraphics.EndImageContext();
                Context.Dispose();

                if (owningTarget.HasBrush(brushKey))
                    throw new InvalidOperationException("Key already has a brush created for it");

                PatternBrush brush = new PatternBrush(bitmap, size, angle, pixelSize);

                owningTarget.brushMap.Add(brushKey, brush);
            }
        }
    }


    public class IOS_BitmapTarget: IOS_GraphicsTarget, IBitmapGraphicsTarget, IDisposable
    {
        bool needsDisposal = false;
        int width, height;

        public IOS_BitmapTarget(int width, int height): base(GetGraphicsContextForBitmap(width, height))
        {
            needsDisposal = true;
            this.width = width;
            this.height = height;
        }

        public int PixelWidth { get { return width; }}
        public int PixelHeight { get { return height; }}

        private static CGContext GetGraphicsContextForBitmap(int width, int height)
        {
            // Profiling shows this is significantly faster than UIGraphics.BeginImageContextWithOptions
            CGContext context = new CGBitmapContext(null, width, height, 8, width * 4, CGColorSpace.CreateDeviceRGB(), CGBitmapFlags.ByteOrder32Little | CGBitmapFlags.PremultipliedFirst);
            context.TranslateCTM(0, height);
            context.ScaleCTM(1, -1);
            return context;
        }

        public IGraphicsBitmap FinishBitmap()
        {
            CGImage cgImage = ((CGBitmapContext)Context).ToImage();
            Context.Dispose();
            needsDisposal = false;
            return new IOS_Bitmap(cgImage);
        }

        public new void Dispose()
        {
            if (needsDisposal) {
                UIGraphics.EndImageContext();
                Context.Dispose();
                needsDisposal = false;
            }

            base.Dispose();
        }
    }

    public class IOS_BitmapGraphicsTargetProvider: IBitmapGraphicsTargetProvider
    {
        #region IBitmapGraphicsTargetProvider implementation

        public IBitmapGraphicsTarget CreateBitmapGraphicsTarget(int width, int height)
        {
            return new IOS_BitmapTarget(width, height);
        }

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
        }

        #endregion
    }


    // Using family names other than PostScript names is slow on iOS -- this class
    // caches the font name lookups so we use PostScript names on all subsequent 
    // font creation.
    static class FontCreator
    {
        static ConcurrentDictionary<string, string> fontNames;

        static FontCreator()
        {
            FlushFontNameCache();
        }

        public static void FlushFontNameCache()
        {
            // Pre-initialize the font mappings for the built-in IOS fonts.
            fontNames = new ConcurrentDictionary<string, string>();
            FontMappings.AddFontMappings(fontNames);
        }
            
        // Create a font, return null if couldn't be created.
        public static CTFont CreateFont(string familyName, float emHeight)
        {
            string psName;

            if (fontNames.TryGetValue(familyName, out psName)) {
                // We've asked for this family name before. If "" is present, the font doesn't
                // exist. Otherwise, psName is the PostScript name to use when creating the font.
                if (psName.Length == 0)
                    return null;
                else
                    return new CTFont(psName, emHeight);
            }
            else {
                CTFont font = new CTFont(familyName, emHeight);

                // Record PostScript name for next time.
                fontNames.TryAdd(familyName, (font == null) ? "" : font.PostScriptName);

                return font;
            }
        }
    }

    class IOS_Font: IDisposable, ITextFaceMetrics
    {
        private CTFont font;
        private CTStringAttributes stringAttributes;
        private float emHeight;
        private float ascent = -1F, descent = -1F, capHeight = -1F, spaceWidth = -1F;
        
        public IOS_Font(string familyName, float emHeight, bool bold, bool italic)
        {
            this.emHeight = emHeight;
            font = FontCreator.CreateFont(familyName, emHeight);
            if (font == null)
                throw new ArgumentException(string.Format("Font '{0}' does not exist", familyName));
            
            if (bold || italic) {
                // Create new font with new symbolic traits.
                CTFontSymbolicTraits traits = 0;
                if (bold)
                    traits |= CTFontSymbolicTraits.Bold;
                if (italic)
                    traits |= CTFontSymbolicTraits.Italic;
                CTFont newFont = font.WithSymbolicTraits(emHeight, traits, CTFontSymbolicTraits.Bold | CTFontSymbolicTraits.Italic);
                if (newFont != null && newFont != font) {
                    font.Dispose();
                    font = newFont;
                }
            }

            stringAttributes = new CTStringAttributes() { Font = font, ForegroundColorFromContext = true };
        }
        
        public CTFont CTFont {
            get {
                return font;
            }
        }

        public float EmHeight {
            get {
                return emHeight;
            }
        }
        
        public float Ascent {
            get {
                if (ascent < 0) {
                    ascent = (float) font.AscentMetric;
                }
                
                return ascent;
            }
        }
       
        public float Descent
        {
            get {
                if (descent < 0) {
                    descent = (float) font.DescentMetric;
                }

                return descent;
            }
        }
        
        public float  CapHeight
        {
            get {
                if (capHeight < 0) {
                    capHeight = (float) font.CapHeightMetric;
                }

                return capHeight;
            }
        }
        
        //private float spaceWidth = -1;
        public float  SpaceWidth
        {
            get {
                if (spaceWidth < 0) {
                    spaceWidth = GetTextWidth(" ");
                }

                return spaceWidth;
            }
        }
        
        public float GetTextWidth(string text)
        {
            using (CTLine line = GetCTLine(text)) {
                return (float) line.GetTypographicBounds();
            }
        }
        
        public SizeF  GetTextSize(string text)
        {
            return new SizeF(GetTextWidth(text), Ascent + Descent);
        }

        public CTLine GetCTLine(string text)
        {
            NSAttributedString attributedString = new NSAttributedString(text, stringAttributes);
            return new CTLine(attributedString);
        }

        #region IDisposable implementation
        public void Dispose()
        {
            if (font != null) {
                font.Dispose();
                font = null;
            }
        }
        #endregion
    }


    public class IOS_TextMetrics : ITextMetrics
    {
        public ITextFaceMetrics GetTextFaceMetrics(string familyName, float emHeight, bool bold, bool italic)
        {
            if (!TextFaceIsInstalled(familyName))
                familyName = "Arial";          // Map non-existant fonts to "Arial".

            return new IOS_Font(familyName, emHeight, bold, italic);
        }

        public bool TextFaceIsInstalled(string familyName) {
            CTFont font = FontCreator.CreateFont(familyName, 12.0F);
            if (font == null)
                return false;
            if (font.FamilyName == "Helvetica" && familyName != "Helvetica")
                return false;

            return true;
        }

        // Flush the font name cache if new fonts are registered or de-registered.
        public static void FlushFontNameCache()
        {
            FontCreator.FlushFontNameCache();
        }

        public void Dispose()
        {
        }
    }

    public class IOS_Bitmap : IGraphicsBitmap
    {
        UIImage uiImage;
        CGImage cgImage;

        // Warning: Do not use the CGImage property on a UIImage without being very careful.
        //
        // EVERY TIME you obtain that property, a new CGImage object is created, which has
        // a ref-counted reference to the image. So, for example, doing:
        //   UIImage img = ...
        //   if (image.CGImage != null)
        //      image.CGImage.foo();
        // Will cause two new CGImage objects to be created. Each of these objects must be
        // disposed. To make this easier, just use the CGImage property on IOS_Bitmap, which
        // will be disposed properly.

        public UIImage UIImage
        {
            get { return uiImage; }
        }

        public CGImage CGImage
        {
            get { return cgImage; }
        }

        public int PixelWidth
        {
            get { return (int) Math.Round(uiImage.Size.Width); }
        }

        public int PixelHeight
        {
            get { return (int) Math.Round(uiImage.Size.Height); }
        }

        public bool Disposed
        {
            get { return uiImage == null; }
        }

        public void Dispose()
        {
            lock (this) {
                if (uiImage != null) {
                    uiImage.Dispose();
                    uiImage = null;
                }
                if (cgImage != null) {
                    cgImage.Dispose();
                    cgImage = null;
                }
            }
        }

        public IOS_Bitmap(UIImage image)
        {
            this.uiImage = image;
            this.cgImage = uiImage.CGImage;
        }

        public IOS_Bitmap(CGImage cgImage)
        {
            this.cgImage = cgImage;
            this.uiImage = new UIImage(cgImage);
        }
    }

    // A different representation of bitmaps using CoreImage that support extremely
    // large bitmaps, and extracting scaled parts of them. As long as we only extract
    // reasonably small parts, we won't blow our memory budget. We constrain the number of
    // pixels in any extraction by scaling if needed, and cache a few scaled versions
    // for efficiency.
    public class IOS_LargeBitmap: IGraphicsBitmap
    {
        // The maximum number of pixels in a returned or retained bitmap. We may retain
        // up to two bitmaps of this pixel size.
        const long MAXPIXELS = 3500000;  // big enough for one iPad screen at full resolution.

        NSUrl url; 
        bool disposed = false;
        int pixelWidth, pixelHeight;
        volatile Slice entireSlice;
        volatile Slice cachedSlice;

        public IOS_LargeBitmap(NSUrl url)
        {
            this.url = url;
            CIImage sourceImage = new CIImage(url);
            if (sourceImage != null) {
                pixelWidth = (int) sourceImage.Extent.Width;
                pixelHeight = (int) sourceImage.Extent.Height;
                sourceImage.Dispose();
            }
        }

        public bool Disposed { get { return disposed; }}

        public void Dispose()
        {
            disposed = true;

            if (entireSlice != null) {
                entireSlice.Dispose();
                entireSlice = null;
            }

//            if (sourceImage != null) {
//                sourceImage.Dispose();
//                sourceImage = null;
//            }
        }

        public int PixelWidth {
            get {
                return pixelWidth;
            }
        }

        public int PixelHeight {
            get {
                return pixelHeight;
            }
        }

        public bool IsEmpty {
            get { return pixelWidth == 0 || pixelHeight == 0; }
        }

        // Extract a part of the image, with the suggested maximum scale. The actual scale returned
        // may be smaller or, if the result will exceed the pixel size limit, larger.
        //
        // For scaled images, if the coordinates request don't fall on scale boundaries,
        // we expand them to scale boundaries. "actualRect" has the expanded coordinates (always integers). Note that
        // ActualRect could even exceed original bitmap boundaries by up to scale - 1.
        public CGImage GetPart(int x, int y, int width, int height, long maximumScale, out RectangleF actualRect)
        {
            //Debug.WriteLine("Part requested at x={0}, y={1}, width={2}, height={3}, maxScale={4}", x, y, width, height, maximumScale);

            if (IsEmpty) {
                actualRect = new RectangleF(x, y, width, height);
                return null;
            }

            // Round up maximumScale to power of two.
            ulong newMaxScale = UpperPowerOfTwo((ulong)maximumScale);
            if (newMaxScale > (1 << 30))
                maximumScale = 1 << 30;
            else
                maximumScale = (int)newMaxScale;

            // If we can't satisfy the maximum scale, then set the maximum scale to best attainable.
            int bestPossibleScale = GetSmallestScaleWithinLimit(width, height, MAXPIXELS);
            if (maximumScale < bestPossibleScale)
                maximumScale = bestPossibleScale;

            // First see if the slice that encompasses the whole image can work.
            CreateEntireSlice();
            if (entireSlice.Contains(x, y, width, height, maximumScale)) {
                //Debug.WriteLine("Returning part from entire slice");
                return entireSlice.GetPart(x, y, width, height, out actualRect);
            }

            // See if we have a caches slice that can work.
            Slice cachedSlice = this.cachedSlice;
            if (cachedSlice != null) {
                lock (cachedSlice) {                    
                    if (cachedSlice.Contains(x, y, width, height, (int)maximumScale)) {
                        //Debug.WriteLine("Returning part from cached slice");
                        return cachedSlice.GetPart(x, y, width, height, out actualRect);
                    }
                }
            }

            // Create a new slice and cache it.
            //Debug.WriteLine("Creating new slice for the part");
            cachedSlice = CreateEnclosingSlice(x, y, width, height, (int)maximumScale);
            Slice oldSlice = Interlocked.Exchange(ref this.cachedSlice, cachedSlice);
            if (oldSlice != null) {
                lock (oldSlice)
                    oldSlice.Dispose();
            }
            return cachedSlice.GetPart(x, y, width, height, out actualRect);
        }

        ulong UpperPowerOfTwo(ulong v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v |= v >> 32;
            v++;
            return v;
        }

        // Create a slice that covers the entire source image, at the smallest scale
        // that fits the pixel limit.
        void CreateEntireSlice()
        {
            if (entireSlice == null) {
                // We can use up to 2x the pixel budget if we create at scale=1, because that would mean
                // we never create any other slices.
                int scale = GetSmallestScaleWithinLimit(PixelWidth, PixelHeight, MAXPIXELS * 2);
                if (scale != 1)
                    scale = GetSmallestScaleWithinLimit(PixelWidth, PixelHeight, MAXPIXELS);
                
                Slice slice = CreateSlice(0, 0, PixelWidth, PixelHeight, scale);
                //Debug.WriteLine("EntireSlice created with width={0}, height={1}, scale={2}", PixelWidth, PixelHeight, scale);
                if (Interlocked.CompareExchange(ref entireSlice, slice, null) != null)
                    slice.Dispose();
            }
        }

        // Create a slice at the given scale level that encloses the given rectangled,
        // and return it.
        Slice CreateEnclosingSlice(int x, int y, int width, int height, int scale)
        {
            int sliceX = x, sliceY = y;
            int sliceWidth, sliceHeight;

            // Determine the size of the slice we are returning.
            GetMaxSliceSize(scale, (double)width / (double)height, out sliceWidth, out sliceHeight);
            if (sliceWidth < width)
                sliceWidth = width;
            if (sliceHeight < height)
                sliceHeight = height;

            // Determine the position of the slice we are returning.
            PositionSlice(ref sliceX, ref sliceWidth, (x + width / 2), PixelWidth, scale);
            PositionSlice(ref sliceY, ref sliceHeight, (y + height / 2), PixelHeight, scale);

            //Debug.WriteLine("Wanting slice at  x={0}, y={1}, width={2}, height={3}, scale={4}", x, y, width, height, scale);
            //Debug.WriteLine("Creating slice at x={0}, y={1}, width={2}, height={3}, scale={4}", sliceX, sliceY, sliceWidth, sliceHeight, scale);

            // Create the slice.
            return CreateSlice(sliceX, sliceY, sliceWidth, sliceHeight, scale);
        }

        // Position a slice of size sliceExtent, centered as best possible on center, within the range (0..maxExtent).
        // Return the slice through sliceStart, sliceExtent. The start and extent should be multiple of scale, unless bumping
        // against the end.
        void PositionSlice(ref int sliceStart, ref int sliceExtent, int center, int maxExtent, int scale)
        {
            // Position centered on center.
            sliceStart = center - sliceExtent / 2;

            if (sliceExtent % scale != 0)
                sliceExtent += (scale - sliceExtent % scale);

            // Move negative direction if goes past maxExtent.
            if (sliceStart + sliceExtent > maxExtent)
                sliceStart -= ((sliceStart + sliceExtent) - maxExtent);
            // Move positive direction if goes before 0.
            if (sliceStart < 0) {
                sliceStart = 0;
                if (sliceExtent > maxExtent)
                    sliceExtent = maxExtent;
            }
            else {
                if (sliceStart % scale != 0) {
                    sliceStart -= sliceStart % scale;
                    sliceExtent += scale;
                    if (sliceStart + sliceExtent > maxExtent)
                        sliceExtent -= ((sliceStart + sliceExtent) - maxExtent);
                }
            }
        }

        int GetSmallestScaleWithinLimit(int width, int height, long maxPixels)
        {
            long scale = 1;
            while (((long)width * (long)height) / ((long)scale * scale) > maxPixels)
                scale *= 2;
            if (scale > (1 << 30))
                return (1 << 30);
            else 
                return (int)scale;
        }

        // Get the maximum slice size at the given aspect ratio using the given scale.
        void GetMaxSliceSize(int scale, double ratio, out int width, out int height)
        {
            height = (int) Math.Floor(Math.Sqrt((double)MAXPIXELS * (double)scale * (double)scale / ratio));
            width = (int)Math.Floor(height * ratio);
        }

        Slice CreateSlice(int x, int y, int width, int height, int scale)
        {
            Debug.Assert(x % scale == 0);
            Debug.Assert(y % scale == 0);

            int finalWidth, finalHeight;  // Changed if we scale.
            List<CIImage> imagesToDispose = new List<CIImage>(4);

            using (CIContext imageContext = CIContext.FromOptions(null)) {
                CIImage outputImage = new CIImage(url);
                imagesToDispose.Add(outputImage);

                CGAffineTransform transform = CGAffineTransform.MakeIdentity();

                // Crop.
                if (x != 0 || y != 0 || height != PixelHeight || width != PixelWidth) {
                    int flippedY = PixelHeight - height - y;
                    outputImage = outputImage.ImageByCroppingToRect(new CGRect(x, flippedY, width, height));
                    imagesToDispose.Add(outputImage);

                    if (x != 0 || flippedY != 0) {
                        transform.Translate(-x, -flippedY);
                   }
                }

                // Scale.
                if (scale > 1) {
                    // If the width/height isn't an exactly multiple of scale, these scale factors make be slightly off an exact scale so that
                    // the result bitmap has integral size.
                    // We try to make sure this doesn't happen, but it can. The tiny difference shouldn't be noticable.
                    finalWidth = (int)Math.Ceiling(width / (double)scale);
                    finalHeight = (int)Math.Ceiling(height / (double)scale);
                    transform.Scale((nfloat)finalWidth / (nfloat)width, (nfloat)finalHeight / (nfloat)height);
                }
                else {
                    finalWidth = width;
                    finalHeight = height;
                }

                if (!transform.IsIdentity) {
                    outputImage = outputImage.ImageByApplyingTransform(transform);
                    imagesToDispose.Add(outputImage);
                }

                CGImage cgImage = imageContext.CreateCGImage(outputImage, new CGRect(0, 0, finalWidth, finalHeight));

                // For debugging, this can be very useful to monitor what is going on and if the images are being created correction.
                //SaveCGImage(cgImage, string.Format("{0}_{1},{2}-{3},{4}-sc{5}.png", fileName, x, y, width, height, scale));

                // Dispose the CIImages created along the way.
                foreach (CIImage imageToDispose in imagesToDispose)
                    imageToDispose.Dispose();
                
                return new Slice(cgImage, x, y, width, height, scale);
            }
        }

        #if false // Useful debugging code.
        void SaveCGImage(CGImage image, string name)
        {
            string directory = GetDirectory("LargeImageSlices");
            string pathName = Path.Combine(directory, name);
            using (UIImage uiImage = new UIImage(image)) {
                using (NSData data = new UIImage(image).AsPNG()) {
                    data.Save(pathName, true);
                }
            }
            Debug.WriteLine("Wrote image to: {0}", (object)pathName);
        }

        string GetDirectory(string subdirName)
        {
            NSError error;
            NSUrl appSupport = NSFileManager.DefaultManager.GetUrl(NSSearchPathDirectory.ApplicationSupportDirectory, NSSearchPathDomain.User, null, true, out error);
            if (appSupport == null)
                return null;

            string containerDirectory = Path.Combine(appSupport.Path, subdirName);

            try {
                if (!Directory.Exists(containerDirectory))
                    Directory.CreateDirectory(containerDirectory);  // TODO: handle failure?
            }
            catch (IOException) {
                return null;
            }

            return containerDirectory;

        }
        #endif

        // A slice produced from a large bitmap. It contains a part of the original bitmap,
        // possibly scaled.
        private class Slice: IDisposable
        {
            public CGImage cgImage;
            public readonly int x, y;           // Upper-left of the original image. Always a multiple of scale.
            public readonly int width, height;  // Size in the original image. May not be a multiple of scale, but only if it bumped up to an edge.
            public readonly int scale;          // Amount scaled. I.e., 4 means that the width of the cgImage is 1/4 of "width", and each pixel is scalled from 4x4 pixel block in original image.

            public Slice(CGImage cgImage, int x, int y, int width, int height, int scale)
            {
                this.cgImage = cgImage;
                this.x = x;
                this.y = y;
                this.width = width;
                this.height = height;
                this.scale = scale;
            }

            // Can this slice support the following request?
            public bool Contains(int partX, int partY, int partWidth, int partHeight, double maxScale)
            {
                return (partX >= x && partY >= y && (partX + partWidth) <= (x + width) && (partY + partHeight) <= (y + height) && scale <= maxScale);
            }

            // Get a part of the image. For scaled images, if the coordinates request don't fall on scale boundaries,
            // we expand them to scale boundaries. ActualRect has the expanded coordinates (always integers). Note that
            // ActualRect could even exceed original bitmap boundaries by up to scale - 1.
            public CGImage GetPart(int partX, int partY, int partWidth, int partHeight, out RectangleF actualRect)
            {
                // For scaled images, this will produce something off, because the pixel boundaries will
                // have to be rounded. To get around this, we could return the actually pixel boundaries, and 
                // then have any code that wants to deal with that handle clipping. 
                int left = (int)Math.Floor((partX - x) / (double)scale);
                int top = (int)Math.Floor((partY - y) / (double)scale);
                int right = (int)Math.Ceiling((partX + partWidth - x) / (double)scale);
                int bottom = (int)Math.Ceiling((partY + partHeight - y) / (double)scale);
                actualRect = RectangleF.FromLTRB(left * scale + x, top * scale + y, right * scale + x, bottom * scale + y);
                return cgImage.WithImageInRect(CGRect.FromLTRB(left, top, right, bottom));
            }

            public void Dispose()
            {
                if (cgImage != null) {
                    cgImage.Dispose();
                    cgImage = null;
                }
            }
        }
    }


    public class IOS_FileLoader : IFileLoader
    {
        private string basePath;

        public IOS_FileLoader(string basePath)
        {
            this.basePath = basePath;
        }

        public FileKind CheckFileKind(string path)
        {
            string filePath = SearchForFileAnySlash(path);
            if (filePath == null)
                return FileKind.DoesntExist;

            try {
                using (Stream s = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    if (InputOutput.IsOcadFile(s))
                        return FileKind.OcadFile;
                    else
                        return FileKind.OtherFile;
                }
            }
            catch (IOException) {
                return FileKind.NotReadable;
            }
            catch (UnauthorizedAccessException) {
                return FileKind.NotReadable;
            }
        }

        public IGraphicsBitmap LoadBitmap(string path, bool isTemplate)
        {
            // UNDONE: Use File Coordination? Needed if we support ICloud.

            string filePath = SearchForFileAnySlash(path);
            if (filePath == null)
                return null;

            return new IOS_LargeBitmap(NSUrl.FromFilename(filePath));
        }
            
        public Map LoadMap(string path, Map referencingMap)
        {
            // UNDONE: Use File Coordination? Needed if we support ICloud.

            string filePath = SearchForFileAnySlash(path);
            if (filePath == null)
                return null;

            Map newMap = new Map(referencingMap.TextMetricsProvider, new IOS_FileLoader(Path.GetDirectoryName(filePath)));

            InputOutput.ReadFile(filePath, newMap);
            return newMap;
        }

        // Find the file that would be loaded, and delete it. Used to delete background maps that aren't needed anymore.
        public bool DeleteFile(string path)
        {
            string filePath = SearchForFileAnySlash(path);
            if (filePath == null)
                return false;
            else {
                File.Delete(path);
                return true;
            }
        }

        // Try searching for file using either slash or backslash as separator.
        private string SearchForFileAnySlash(string path)
        {
            string result = SearchForFile(path);
            if (result != null)
                return result;

            path = path.Replace('\\', '/');
            return SearchForFile(path);
        }

        private string SearchForFile(string path)
        {
            if (File.Exists(path))
                return path;

            if (basePath != null) {
                string revisedPath;

                if (!Path.IsPathRooted(path))
                {
                    revisedPath = Path.Combine(basePath, path);
                    if (File.Exists(revisedPath))
                        return revisedPath;
                }

                string baseName = Path.GetFileName(path);
                revisedPath = Path.Combine(basePath, baseName);
                if (File.Exists(revisedPath))
                    return revisedPath;
            }

            return null;
        }
    }
}
