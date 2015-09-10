/* Copyright (c) 2006-2008, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

using PurplePen.MapView;

namespace PurplePen
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
                PointF[] pts = { new PointF(rect.Left, rect.Bottom), new PointF(rect.Right, rect.Top) };
                xformWorldToPixel.TransformPoints(pts);
                RectangleF rectPixel = RectangleF.FromLTRB(pts[0].X, pts[0].Y, pts[1].X, pts[1].Y);
                g.FillRectangle(blueBrush, rectPixel.X, rectPixel.Y, rectPixel.Width, rectPixel.Height);
                g.DrawRectangle(redPen, rectPixel.X, rectPixel.Y, rectPixel.Width, rectPixel.Height);
            }
        }

        public void EraseHighlight(Graphics g, Matrix xformWorldToPixel, Brush eraseBrush)
        {
            PointF[] pts = { new PointF(rect.Left, rect.Bottom), new PointF(rect.Right, rect.Top) };
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
