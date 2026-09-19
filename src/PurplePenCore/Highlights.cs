using System;
using System.Collections.Generic;
using System.Drawing;

using PurplePen.Graphics2D;

namespace PurplePen
{

    // A highlight is as object overlayed on the current map that shows the current selection.
    // It is designed to draw/erase quickly, so it must be able to erase itself given a brush with the
    // bitmap to erase with. The highlight draws in pixel coords, but it is passed a transform it can
    // used. It has to not apply that transform to the Graphics, however, so that the textures look OK 
    // which drawing and erasing.
    public interface IMapViewerHighlight
    {
        // Get the bounding rectangle.
        RectangleF GetHighlightBounds();

        // Get extra border, in pixels, around GetHighlightBounds
        int GetBorderPixels();

        // Draw onto the (pixel coordinates) graphics, using the given world-to-pixel transformation.
        void DrawHighlight(IGraphicsTarget g, Matrix xformWorldToPixel);

    }


    public class RectangleHighlight: IMapViewerHighlight
    {
        const float penWidth = 3F;

        RectangleF rect;
        object redPenKey = new object();
        object blueBrushKey = new object();

        public RectangleHighlight(RectangleF rect)
        {
            this.rect = rect;
        }

        public void DrawHighlight(IGraphicsTarget g, Matrix xformWorldToPixel)
        {
            if (! g.HasPen(redPenKey)) {
                g.CreatePen(redPenKey, CmykColor.FromColor(Color.Red), penWidth, LineCapMode.Flat, LineJoinMode.Miter, 0);
            }

            if (! g.HasBrush(blueBrushKey)) {
                g.CreateSolidBrush(blueBrushKey, CmykColor.FromColor(Color.FromArgb(64, Color.DarkBlue)));
            }

            PointF[] pts = { new PointF(rect.Left, rect.Bottom), new PointF(rect.Right, rect.Top) };
            xformWorldToPixel.TransformPoints(pts);
            RectangleF rectPixel = RectangleF.FromLTRB(pts[0].X, pts[0].Y, pts[1].X, pts[1].Y);

            g.FillRectangle(blueBrushKey, new RectangleF(rectPixel.X, rectPixel.Y, rectPixel.Width, rectPixel.Height));
            g.DrawRectangle(redPenKey, new RectangleF(rectPixel.X, rectPixel.Y, rectPixel.Width, rectPixel.Height));
        }

        public void EraseHighlight(IGraphicsTarget g, Matrix xformWorldToPixel, object eraseBrushKey)
        {
            PointF[] pts = { new PointF(rect.Left, rect.Bottom), new PointF(rect.Right, rect.Top) };
            xformWorldToPixel.TransformPoints(pts);
            RectangleF rectPixel = RectangleF.FromLTRB(pts[0].X, pts[0].Y, pts[1].X, pts[1].Y);

            rectPixel.Inflate(penWidth / 2F, penWidth / 2F);
            Rectangle r = Geometry.RoundRectangle(rectPixel);

            g.FillRectangle(eraseBrushKey, r);
        }

        public RectangleF GetHighlightBounds()
        {
            return rect;
        }

        public int GetBorderPixels()
        {
            return (int)Math.Ceiling(penWidth / 2);
        }
    }

    // Describes the content of a tooltip shown in the map viewer: a bold header and a body.
    public record ToolTipDescription(string header, string body);


}
