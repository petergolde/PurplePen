using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PurplePen.Graphics2D
{
    public static class RectangleFExtension
    {
        public static PointF Center(this RectangleF rect)
        {
            return new PointF((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
        }

        public static PointF TopLeft(this RectangleF rect)
        {
            return rect.Location;
        }

        public static PointF TopRight(this RectangleF rect)
        {
            return new PointF(rect.Right, rect.Top);
        }

        public static PointF BottomRight(this RectangleF rect)
        {
            return new PointF(rect.Right, rect.Bottom);
        }

        public static PointF BottomLeft(this RectangleF rect)
        {
            return new PointF(rect.Left, rect.Bottom);
        }

        public static string LTRBString(this RectangleF rect)
        {
            return string.Format("[L={0:F2},T={1:F2},R={2:F2},B={3:F2}]", rect.Left, rect.Top, rect.Right, rect.Bottom);
        }
    }

    public static class MatrixExtensions
    {
        public static PointF Transform(this Matrix matrix, PointF pt)
        {
            PointF[] pts = {pt};
            matrix.TransformPoints(pts);
            return pts[0];
        }
    }
}

