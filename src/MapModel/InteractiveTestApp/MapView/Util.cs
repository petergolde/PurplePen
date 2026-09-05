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
