using System;
using System.Collections.Generic;
using System.Text;


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
                    g.CreatePen(penKey, color, thickness, LineCapMode.Round, LineJoinMode.Round, MITER_LIMIT); break;
                case LineStyle.Beveled:
                    g.CreatePen(penKey, color, thickness, LineCapMode.Flat, LineJoinMode.Bevel, MITER_LIMIT); break;
                case LineStyle.Mitered:
                    g.CreatePen(penKey, color, thickness, LineCapMode.Flat, LineJoinMode.Miter, MITER_LIMIT); break;
                case LineStyle.FlatRounded:
                    g.CreatePen(penKey, color, thickness, LineCapMode.Flat, LineJoinMode.Round, MITER_LIMIT); break;
                default:
                    throw new ArgumentException();
            }
        }

        // Create a solid pen
        public static void CreateSolidPen(IGraphicsTarget g, object penKey, object brushKey, float thickness, LineStyle style)
        {
            switch (style) {
                case LineStyle.Rounded:
                    g.CreatePen(penKey, brushKey, thickness, LineCapMode.Round, LineJoinMode.Round, MITER_LIMIT); break;
                case LineStyle.Beveled:
                    g.CreatePen(penKey, brushKey, thickness, LineCapMode.Flat, LineJoinMode.Bevel, MITER_LIMIT); break;
                case LineStyle.Mitered:
                    g.CreatePen(penKey, brushKey, thickness, LineCapMode.Flat, LineJoinMode.Miter, MITER_LIMIT); break;
                case LineStyle.FlatRounded:
                    g.CreatePen(penKey, brushKey, thickness, LineCapMode.Flat, LineJoinMode.Round, MITER_LIMIT); break;
                default:
                    throw new ArgumentException();
            }
        }

    }
}
