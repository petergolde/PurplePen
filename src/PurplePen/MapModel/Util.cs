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
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
#if WPF
using System.Windows.Media;
#else
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
#endif

namespace PurplePen.MapModel
{
	public class Util {
		public const float kappa = 0.5522847498F;  // constant used to create near-circle with a bezier.

		public static string ReadDelphiString(BinaryReader reader, int nBytes) {
			int length = reader.ReadByte();
			char[] chars = reader.ReadChars(nBytes);
			return new string(chars, 0, Math.Min(length, nBytes));
		}

		public static void WriteDelphiString(BinaryWriter writer, string s, int nBytes) {
			int length;
			char[] a;
			if (s == null) {
				length = 0;
				a = null;
			}
			else {
				length = Math.Min(s.Length, nBytes);
				a = new char[length];
				s.CopyTo(0, a, 0, length);
			}

			writer.Write((byte)length);
			if (a != null)
				writer.Write(a);
			for (int i = 0; i < nBytes - length; ++i)
				writer.Write((byte) 0);
		}


		public static byte[] ReadByteArray(BinaryReader reader, int nBytes) {
			byte[] bytes = new byte[nBytes];
			for (int i = 0; i < nBytes; ++i) {
				bytes[i] = reader.ReadByte();
			}
			return bytes;
		}

		public static bool FontExists(string fontname) {
#if WPF
            // Get the glyphTypeface to see if the font exists.
            GlyphTypeface glyphTypeface;
            Typeface typeface = new Typeface(fontname);
            return typeface.TryGetGlyphTypeface(out glyphTypeface);
#else
			// Doesn't seem to be an easy way to determine if a font exists.
			try {
				FontFamily family = new FontFamily(fontname);
				family.Dispose();
				return true;
			}
			catch {
				return false;
			}
#endif
		}

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

		public static PointF MoveDistance(PointF pt, float distance, float angle) {
			pt.X = (float) (pt.X + distance * Math.Cos(angle / 360.0 * (Math.PI * 2)));
			pt.Y = (float) (pt.Y + distance * Math.Sin(angle / 360.0 * (Math.PI * 2)));
			return pt;
		}

		public static RectangleF TransformRectangle(Matrix m, RectangleF rect) {
			PointF[] pts = { new PointF(rect.Left, rect.Top), new PointF(rect.Right, rect.Bottom) };
            pts = GraphicsUtil.TransformPoints(pts, m);
			return new RectangleF(pts[0], new SizeF(pts[1].X - pts[0].X, pts[1].Y - pts[0].Y));
		}

		// Transform a rectangle with a transform, and return the new rectangle that bounds the corners of the transformed one.
		public static RectangleF BoundsOfTransformedRectangle(RectangleF rect, Matrix transform) {
			PointF[] corners = { new PointF(rect.Left, rect.Top),    new PointF(rect.Right, rect.Top),
							 new PointF(rect.Left, rect.Bottom), new PointF(rect.Right, rect.Bottom) };
            corners = GraphicsUtil.TransformPoints(corners, transform);
			float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
			for (int i = 0; i < corners.Length; ++i) {
				if (corners[i].X < minX)   minX = corners[i].X;
				if (corners[i].X > maxX)   maxX = corners[i].X;
				if (corners[i].Y < minY)   minY = corners[i].Y;
				if (corners[i].Y > maxY)   maxY = corners[i].Y;
			}

			return new RectangleF(minX, minY, maxX - minX, maxY - minY);
		}


		// Rotate a rectangle around a point, and return the new rectangle that bounds the corners of the rotated one.
		public static RectangleF BoundsOfRotatedRectangle(RectangleF rect, PointF rotateAt, float angle) {
            Matrix m = GraphicsUtil.RotationMatrix(angle, rotateAt);
			return BoundsOfTransformedRectangle(rect, m);
		}

		// Find the transformation matrix that transform a rectangle (with positive Y downward) to a different
		// rectangle (with positive Y upward)
		public static Matrix TransformInvertedRectangle(RectangleF source, RectangleF dest) {
			SizeF sourceSize = source.Size, destSize = dest.Size;

            Matrix m = GraphicsUtil.TranslationMatrix(-source.Left, -source.Top);
            m = GraphicsUtil.Multiply(m, GraphicsUtil.ScalingMatrix(destSize.Width / sourceSize.Width, -destSize.Height / sourceSize.Height));
            m = GraphicsUtil.Multiply(m, GraphicsUtil.TranslationMatrix(dest.Left, dest.Bottom));
            return m;
		}

		public static RectangleF InflateRect(RectangleF rect, float delta) {
			rect.Inflate(delta, delta);
			return rect;
		}
		
		// Find two control points around p2.
		private static void FindControlPoints(PointF p1, PointF p2, PointF p3, out PointF c1, out PointF c2) {
			if (p1 == p3) {
				c1 = c2 = p2;
			}
			else {
				float angle = Angle(p1, p3);
				c1 = MoveDistance(p2, DistanceF(p2, p1) / 3, angle + 180);
				c2 = MoveDistance(p2, DistanceF(p2, p3) / 3, angle);
			}
		}

		// Find an end control point nearest p1 on the way to c2 and p2.
		private static PointF FindEndControlPoint(PointF p1, PointF p2, PointF c2) {
			if (p1 == p2 || p2 == c2)
				return p1;

			float angleControl = Angle(p2, c2);
			float anglePoint = Angle(p2, p1);
			return MoveDistance(p1, DistanceF(p1, p2) / 3, anglePoint - (angleControl - anglePoint) + 180);
		}

		// Create a SymPath that is a Bezier curver matching some points.
		public static SymPath BezierFromPoints(PointF[] points) {
			int length = points.Length;
			PointF[] newpts;
			PointKind[] kinds;
			if (length <= 2) {
				newpts = points;
				kinds = new PointKind[length];
				for (int i = 0; i < length; ++i)
					kinds[i] = PointKind.Normal;
			}
			else {
				newpts = new PointF[length + (length - 1) * 2];
				kinds = new PointKind[length + (length - 1) * 2];

				// find control points for all but the end.
				for (int i = 1; i < points.Length - 1; ++i) {
					newpts[i * 3] = points[i];
					kinds[i * 3] = PointKind.Normal;
					FindControlPoints(points[i-1], points[i], points[i+1], out newpts[i * 3 - 1], out newpts[i * 3 + 1]);
					kinds[i * 3 - 1] = PointKind.BezierControl;
					kinds[i * 3 + 1] = PointKind.BezierControl;
				}

				// 
				newpts[0] = points[0];
				newpts[(length-1) * 3] = points[length - 1];
				kinds[0] = PointKind.Normal;
				kinds[(length-1) * 3] = PointKind.Normal;

				if (points[0] == points[length - 1]) {
					// closed curve
					FindControlPoints(points[length - 2], points[0], points[1], out newpts[(length-1) * 3 - 1], out newpts[1]);
				}
				else {
					// open curve
					newpts[1] = FindEndControlPoint(points[0], points[1], newpts[2]);
					newpts[(length-1) * 3 - 1] = FindEndControlPoint(points[length - 1], points[length - 2], newpts[(length-1) * 3 - 2]);
				}
				kinds[(length-1) * 3 - 1] = PointKind.BezierControl;
				kinds[1] = PointKind.BezierControl;
			}

			kinds[kinds.Length - 1] = PointKind.Normal;
			
			return new SymPath(newpts, kinds);
		}

		// Create a SymPath that is an ellipse inside the given rectange.
		public static SymPath CreateEllipsePath(RectangleF boundingRect) {
			float midX = (boundingRect.Left + boundingRect.Right) / 2;
			float midY = (boundingRect.Top + boundingRect.Bottom) / 2;
			float alphaX = kappa / 2.0F * boundingRect.Width;
			float alphaY = kappa / 2.0F * boundingRect.Height;
			PointF[] pts = {
							   new PointF(midX, boundingRect.Top),
							   new PointF(midX - alphaX, boundingRect.Top),
							   new PointF(boundingRect.Left, midY - alphaY),
							   new PointF(boundingRect.Left, midY),
							   new PointF(boundingRect.Left, midY + alphaY),
							   new PointF(midX - alphaX, boundingRect.Bottom),
							   new PointF(midX, boundingRect.Bottom),
							   new PointF(midX + alphaX, boundingRect.Bottom),
							   new PointF(boundingRect.Right, midY + alphaY),
							   new PointF(boundingRect.Right, midY),
							   new PointF(boundingRect.Right, midY - alphaY),
							   new PointF(midX + alphaX, boundingRect.Top),
							   new PointF(midX, boundingRect.Top) };
			PointKind[] kinds = {   PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
									PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
									PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
									PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
									PointKind.Normal };
			return new SymPath(pts, kinds);
		}

		// Create a SymPath that is a rectangle.
		public static SymPath CreateRectanglePath(RectangleF rect) {
			PointF[] pts = {	new PointF(rect.Left, rect.Top), new PointF(rect.Right, rect.Top), new PointF(rect.Right, rect.Bottom), 
							   new PointF(rect.Left, rect.Bottom), new PointF(rect.Left, rect.Top) };
			PointKind[] kinds = {PointKind.Corner, PointKind.Corner, PointKind.Corner, PointKind.Corner, PointKind.Corner};

			return new SymPath(pts, kinds);
		}
							   

		// Return point on a line segment closest to another point.
		public static PointF ClosestPointOnLineSegment(PointF start, PointF end, PointF point) {
			double u;

			if (end.X == start.X && end.Y == start.Y) {
				// special case: line segment is a point.
				return start;
			}

			// computer parameterized point on the full line nearest to point.
			u = ((point.X - start.X)*(end.X - start.X) + (point.Y - start.Y)*(end.Y - start.Y)) / ((end.X - start.X)*(end.X - start.X) + (end.Y - start.Y)*(end.Y - start.Y));

			if (u < 0) {
				return start;
			}
			else if (u > 1) {
				return end;
			}
			else {
				PointF closest = new PointF(
				           (float) (start.X + u * (end.X - start.X)),
						   (float) (start.Y + u * (end.Y - start.Y)));
				return closest;
			}
		}

		// Determine if a point is on a line segment.
		public static bool IsPointOnLineSegment(PointF start, PointF end, PointF point) {
			PointF closest = ClosestPointOnLineSegment(start, end, point);
			if (Math.Abs(closest.X - point.X) < 0.01 &&
				Math.Abs(closest.Y - point.Y) < 0.01)
				return true;
			else
				return false;
		}

		public static float DistanceFromLineSegment(PointF start, PointF end, PointF point) {
			return DistanceF(ClosestPointOnLineSegment(start, end, point), point);
		}
			
		// Determine if two points are on the same side of a line (defined by 2 points)
		public static bool SameSideOfLine(PointF line1, PointF line2, PointF point1, PointF point2) {
			float u = (point1.X - line1.X) * (line1.Y - line2.Y) + (point1.Y - line1.Y) * (line2.X - line1.X);
			float v = (point2.X - line1.X) * (line1.Y - line2.Y) + (point2.Y - line1.Y) * (line2.X - line1.X);
			return (u == 0 || v == 0 || Math.Sign(u) == Math.Sign(v));
		}

		// Determine if a point is inside a triangle
		public static bool InsideTriangle(PointF t1, PointF t2, PointF t3, PointF point) {
			return SameSideOfLine(t1, t2, t3, point) &&
				SameSideOfLine(t2, t3, t1, point) &&
				SameSideOfLine(t1, t3, t2, point);
		}

        // Determine the miter amount for an subtended angle
        public static float MiterFactor(float subtendedAngle)
        {
            subtendedAngle = Math.Abs(subtendedAngle);
            if (subtendedAngle > 0 && subtendedAngle < 180.0F)
                return (float) Math.Abs(1.0F / Math.Sin(subtendedAngle * Math.PI / 360.0));
            else
                return 1.0F;
        }

#if !WPF
		private static Graphics hiresGraphics;

		// Returns a graphics scaled with negative Y and hi-resolution (50 units/pixel or so).
		public static Graphics GetHiresGraphics() {
            if (hiresGraphics == null) {
                hiresGraphics = Graphics.FromHwnd(IntPtr.Zero);
                hiresGraphics.ScaleTransform(50F, -50F);
            }

			return hiresGraphics;
		}
#endif

		// Split a string with newlines in it into lines.
		public static string[] SplitLines(string s) {
			int startLine = 0;
			List<string> a = new List<string>();

			for (int i = 0; i < s.Length; ++i) {
				if (s[i] == '\r' || s[i] == '\n') {
					a.Add(s.Substring(startLine,i - startLine));
					if (s[i] == '\r' && i < s.Length - 1 && s[i+1] == '\n') 
						++i;
					startLine = i+1;
				}
			}

			if (startLine < s.Length)
				a.Add(s.Substring(startLine));

			return a.ToArray();
		}

		// Determine the distance between two colors. squareroot of sum of squares of distance.
		public static double ColorDistance(Color col1, Color col2) {
			double dist = 0;
			dist += (col1.R - col2.R) * (col1.R - col2.R);
			dist += (col1.G - col2.G) * (col1.G - col2.G);
			dist += (col1.B - col2.B) * (col1.B - col2.B);
			return Math.Sqrt(dist);
		}


		// Round a number to a certain number of significant digits.
		public static double RoundToSignificant(double number, int sigDigits, out int decimalPlaces) {
			if (number == 0) {
				decimalPlaces = 0;
				return number;
			}

			// Calculate number of digits before the decimal point.
			int digits = (int) Math.Floor(Math.Log10(Math.Abs(number))) + 1;
			decimalPlaces = sigDigits - digits;
			if (decimalPlaces >= 0 && decimalPlaces <= 15)
				return Math.Round(number, decimalPlaces);
			else {
				double scale = Math.Pow(10.0, - decimalPlaces);
				if (decimalPlaces < 0)
					decimalPlaces = 0;
				return Math.Round(number / scale) * scale;
			}
		}
		
		// Format a number with a certain number of significant digits, chosing the correct suffix based on the magnitude of the number.
		public static string FormatNumberWithSuffix(double number, int sigDigits, string micro, string milli, string unit, string kilo, string mega) {
			string suffix;
			Debug.Assert(unit != null);

			if (number == 0) {
				suffix = unit;
			}
			else if ((Math.Abs(number) < 1e-3 || (Math.Abs(number) < 1 && milli == null)) && micro != null) {
				suffix = micro;
				number *= 1E6;
			}
			else if (Math.Abs(number) < 1 && milli != null) {
				suffix = milli;
				number *= 1E3;
			}
			else if (Math.Abs(number) < 1000 || (kilo == null && mega == null)) {
				suffix = unit;
			}
			else if ((Math.Abs(number) < 1E6 || mega == null) && kilo != null) {
				suffix = kilo;
				number /= 1E3;
			}
			else if (mega != null) {
				suffix = mega;
				number /= 1e6; 
			}
			else {
				Debug.Fail("Can't get here");
				return "";
			}


			int decimals;
			number = RoundToSignificant(number, sigDigits, out decimals);
			string format = "{0:N" + decimals + "} {1}";
			return string.Format(format, number, suffix);
		}

        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        static extern bool PathRelativePathTo(
             [Out] StringBuilder pszPath,
             [In] string pszFrom,
             [In] uint dwAttrFrom,
             [In] string pszTo,
             [In] uint dwAttrTo
        );
        const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
        const uint FILE_ATTRIBUTE_NORMAL = 0x0;
        const int MAX_PATH = 260;

        // Get the relative name, if possible, of one file relative to another.
        public static string GetRelativeFileName(string relativeTo, string file)
        {
            StringBuilder result = new StringBuilder(MAX_PATH);
            bool ret = PathRelativePathTo(result, relativeTo, FILE_ATTRIBUTE_NORMAL, file, FILE_ATTRIBUTE_NORMAL);
            if (ret == false)
                return file;        // no relative path.
            else {
                // If the hittest starts with .\, remove that.
                if (result.Length > 2 && result[0] == '.' && result[1] == '\\')
                    result.Remove(0, 2);
                return result.ToString();
            }
        }

    }
}
