using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using Matrix = System.Drawing.Drawing2D.Matrix;
using LineJoin = System.Drawing.Drawing2D.LineJoin;
using LineCap = System.Drawing.Drawing2D.LineCap;
using Color = System.Drawing.Color;

namespace PurplePen.MapModel
{
    public static class GraphicsUtil
    {
        public static float MITER_LIMIT = 5.0F;

        // Transform points according to a matrix. Does NOT change the input points.
        public static PointF[] TransformPoints(PointF[] pts, Matrix matrix)
        {
            PointF[] xformedPts = (PointF[])pts.Clone();
            matrix.TransformPoints(xformedPts);
            return xformedPts;
        }

        // Transform one point according to a matrix. 
        public static PointF TransformPoint(PointF pt, Matrix matrix)
        {
            PointF[] xformedPts = new PointF[1] { pt };
            matrix.TransformPoints(xformedPts);
            return xformedPts[0];
        }

        // Create a solid pen
        public static IGraphicsPen CreateSolidPen(IGraphicsTarget g, Color color, float thickness, LineStyle style)
        {
            switch (style)
            {
                case LineStyle.Rounded:
                    return g.CreatePen(color, thickness, LineCap.Round, LineJoin.Round, MITER_LIMIT);
                case LineStyle.Beveled:
                    return g.CreatePen(color, thickness, LineCap.Flat, LineJoin.Bevel, MITER_LIMIT);
                case LineStyle.Mitered:
                    return g.CreatePen(color, thickness, LineCap.Flat, LineJoin.Miter, MITER_LIMIT);
                case LineStyle.FlatRounded:
                    return g.CreatePen(color, thickness, LineCap.Flat, LineJoin.Round, MITER_LIMIT);
                default:
                    throw new ArgumentException();
            }
        }

        // Multiple two matrixes, giving a third
        public static Matrix Multiply(Matrix m1, Matrix m2)
        {
            Matrix result = m1.Clone();
            result.Multiply(m2, System.Drawing.Drawing2D.MatrixOrder.Append);
            return result;
        }

    }
}
