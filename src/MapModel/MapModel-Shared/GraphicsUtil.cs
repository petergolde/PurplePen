using System;
using System.Collections.Generic;
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
    using PurplePen.Graphics2D;

    public static class GraphicsUtil
    {
        public static float MITER_LIMIT = 5.0F;

        // Create a solid pen
        public static void CreateSolidPen(IGraphicsTarget g, object penKey, CmykColor color, float thickness, LineStyle style)
        {
            switch (style)
            {
                case LineStyle.Rounded:
                    g.CreatePen(penKey, color, thickness, LineCap.Round, LineJoin.Round, MITER_LIMIT); break;
                case LineStyle.Beveled:
                    g.CreatePen(penKey, color, thickness, LineCap.Flat, LineJoin.Bevel, MITER_LIMIT); break;
                case LineStyle.Mitered:
                    g.CreatePen(penKey, color, thickness, LineCap.Flat, LineJoin.Miter, MITER_LIMIT); break;
                case LineStyle.FlatRounded:
                    g.CreatePen(penKey, color, thickness, LineCap.Flat, LineJoin.Round, MITER_LIMIT); break;
                default:
                    throw new ArgumentException();
            }
        }

        // Create a solid pen
        public static void CreateSolidPen(IGraphicsTarget g, object penKey, object brushKey, float thickness, LineStyle style)
        {
            switch (style) {
                case LineStyle.Rounded:
                    g.CreatePen(penKey, brushKey, thickness, LineCap.Round, LineJoin.Round, MITER_LIMIT); break;
                case LineStyle.Beveled:
                    g.CreatePen(penKey, brushKey, thickness, LineCap.Flat, LineJoin.Bevel, MITER_LIMIT); break;
                case LineStyle.Mitered:
                    g.CreatePen(penKey, brushKey, thickness, LineCap.Flat, LineJoin.Miter, MITER_LIMIT); break;
                case LineStyle.FlatRounded:
                    g.CreatePen(penKey, brushKey, thickness, LineCap.Flat, LineJoin.Round, MITER_LIMIT); break;
                default:
                    throw new ArgumentException();
            }
        }

    }
}
