using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace InteractiveTestApp.MapView
{
    public class RectangleHighlight: IMapViewerHighlight
    {
        const float penWidth = 3F;

        RectangleF rect;

        public RectangleHighlight(RectangleF rect)
        {
            this.rect = rect;
        }

        public void DrawHighlight(Graphics g, Matrix xformWorldToPixel)
        {
            using (Pen redPen = new Pen(Color.Red, penWidth))
            using (Brush blueBrush = new HatchBrush(HatchStyle.Percent25, Color.DarkBlue, Color.Transparent)) {
                PointF[] pts = new PointF[] { new PointF(rect.Left, rect.Bottom), new PointF(rect.Right, rect.Top) };
                xformWorldToPixel.TransformPoints(pts);
                RectangleF rectPixel = RectangleF.FromLTRB(pts[0].X, pts[0].Y, pts[1].X, pts[1].Y);
                g.FillRectangle(blueBrush, rectPixel.X, rectPixel.Y, rectPixel.Width, rectPixel.Height);
                g.DrawRectangle(redPen, rectPixel.X, rectPixel.Y, rectPixel.Width, rectPixel.Height);
            }
        }

        public void EraseHighlight(Graphics g, Matrix xformWorldToPixel, Brush eraseBrush)
        {
            PointF[] pts = new PointF[] { new PointF(rect.Left, rect.Bottom), new PointF(rect.Right, rect.Top) };
            xformWorldToPixel.TransformPoints(pts);
            RectangleF rectPixel = RectangleF.FromLTRB(pts[0].X, pts[0].Y, pts[1].X, pts[1].Y);

            rectPixel.Inflate(penWidth / 2F, penWidth / 2F);
            Rectangle r = Util.Round(rectPixel);
            g.FillRectangle(eraseBrush, r);
        }

        public RectangleF GetHighlightBounds()
        {
            return rect;
        }
    }
}
