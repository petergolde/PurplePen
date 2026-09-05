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
