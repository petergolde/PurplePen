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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;

namespace InteractiveTestApp.MapView
{
	public static class Util {

		public static Rectangle RectFromRectF(RectangleF rectf) {
			int newLeft = (int) Math.Floor(rectf.Left);
			int newTop = (int) Math.Floor(rectf.Top);
			int newRight = (int) Math.Ceiling(rectf.Right);
			int newBottom = (int) Math.Ceiling(rectf.Bottom);
			return new Rectangle(newLeft, newTop, newRight - newLeft, newBottom - newTop);
		}

		public static Point PointFromPointF(PointF pointf) {
			return new Point((int) Math.Round(pointf.X), (int) Math.Round(pointf.Y));
		}

	
		public static Rectangle RectangleFromPoints(Point pt1, Point pt2) {
			int left = Math.Min(pt1.X, pt2.X);
			int right = Math.Max(pt1.X, pt2.X);
			int top = Math.Min(pt1.Y, pt2.Y);
			int bottom = Math.Max(pt1.Y, pt2.Y);
			return Rectangle.FromLTRB(left, top, right, bottom);
		}

        // Round a rectangle. Returns a sane hittest of rounding each coordinate. Rectangle.Round doesn't do that!
        public static Rectangle Round(RectangleF rect)
        {
            return Rectangle.FromLTRB((int)Math.Round(rect.Left), (int)Math.Round(rect.Top), (int)Math.Round(rect.Right), (int)Math.Round(rect.Bottom));
        }

		public static Cursor LoadCursor(string name) {
            return new Cursor(typeof(Util).Assembly.GetManifestResourceStream("InteractiveTestApp.MapView." + name));
		}
		

	}
}
