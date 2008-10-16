/* Copyright (c) 2006-2007, Peter Golde
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

namespace PurplePen.MapView
{
	public class Util {

		public static double Distance(PointF pt1, PointF pt2) {
			double delta1 = (double)pt2.X - (double)pt1.X;
			double delta2 = (double)pt2.Y - (double)pt1.Y;
			return Math.Sqrt(delta1 * delta1 + delta2 * delta2);
		}

		public static float DistanceF(PointF pt1, PointF pt2) {
			return (float) Distance(pt1, pt2);
		}

		public static double DistanceSquared(PointF pt1, PointF pt2) {
			double delta1 = (double)pt2.X - (double)pt1.X;
			double delta2 = (double)pt2.Y - (double)pt1.Y;
			return delta1 * delta1 + delta2 * delta2;
		}

		public static PointF MidPoint(PointF pt1, PointF pt2) {
			return new PointF((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2);
		}

		public static float Angle(PointF pt1, PointF pt2) {
			if (pt1 == pt2)
				return 0.0F;
			else
				return (float) (Math.Atan2(pt2.Y - pt1.Y, pt2.X - pt1.X) * 360.0 / (Math.PI * 2));
		}

		public static RectangleF TransformRectangle(Matrix m, RectangleF rect) {
			PointF[] pts = { new PointF(rect.Left, rect.Top), new PointF(rect.Right, rect.Bottom) };
			m.TransformPoints(pts);
			return new RectangleF(pts[0], new SizeF(pts[1].X - pts[0].X, pts[1].Y - pts[0].Y));
		}

		// Transform a rectangle with a transform, and return the new rectangle that bounds the corners of the transformed one.
		public static RectangleF BoundsOfTransformedRectangle(RectangleF rect, Matrix transform) {
			PointF[] corners = { new PointF(rect.Left, rect.Top),    new PointF(rect.Right, rect.Top),
							 new PointF(rect.Left, rect.Bottom), new PointF(rect.Right, rect.Bottom) };
			transform.TransformPoints(corners);
			float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
			for (int i = 0; i < corners.Length; ++i) {
				if (corners[i].X < minX)   minX = corners[i].X;
				if (corners[i].X > maxX)   maxX = corners[i].X;
				if (corners[i].Y < minY)   minY = corners[i].Y;
				if (corners[i].Y > maxY)   maxY = corners[i].Y;
			}

			return new RectangleF(minX, minY, maxX - minX, maxY - minY);
		}

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

		public static RectangleF InflateRect(RectangleF rect, float delta) {
			rect.Inflate(delta, delta);
			return rect;
		}
		
		public static Rectangle RectangleFromPoints(Point pt1, Point pt2) {
			int left = Math.Min(pt1.X, pt2.X);
			int right = Math.Max(pt1.X, pt2.X);
			int top = Math.Min(pt1.Y, pt2.Y);
			int bottom = Math.Max(pt1.Y, pt2.Y);
			return Rectangle.FromLTRB(left, top, right, bottom);
		}

		public static Cursor LoadCursor(string name) {
            return new Cursor(typeof(Util).Assembly.GetManifestResourceStream("PurplePen.MapView." + name));
		}
		

	}
}
