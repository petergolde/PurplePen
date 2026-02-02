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
using System.Diagnostics;
using System.Collections.Generic;
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;

namespace PurplePen.Graphics2D
{
	// A struct to encapsulate a single bezier curve and do some calculations on it.
	public struct Bezier
	{
		public PointF end1, end2;   // the end-points of the bezier.
		public PointF control1, control2;  // the control points of the bezier.

		public Bezier(PointF end1, PointF control1, PointF control2, PointF end2)
		{
			this.end1 = end1;
			this.end2 = end2;
			this.control1 = control1;
			this.control2 = control2;
		}

        public RectangleF BoundingBox()
        {
            return Geometry.BoundingBoxOfPoints(new PointF[] { end1, control1, control2, end2 });
        }

		// Is this bezier within "error" of a straight line?
		public bool IsFlat(float error) {
			return (Geometry.DistanceFromLineSegment(end1, end2, control1) < error &&
				    Geometry.DistanceFromLineSegment(end1, end2, control2) < error);
		}

		// Determine if a point is within the quadrilatal of the four defining points. The
		// entier bezier must be in this quad also, so this is a necessary condition for a 
		// point to be on the bezier. If error > 0, also returns true for points outside the
		// quad but within error of the quad.
		public bool IsPointInQuad(PointF point, float error) {
			PointF e1 = end1, e2 = end2;
			PointF c1 = control1, c2 = control2;

			if (Geometry.InsideTriangle(e1, e2, c1, point) ||
				Geometry.InsideTriangle(e1, e2, c2, point) ||
				Geometry.InsideTriangle(e1, c1, c2, point) ||
				Geometry.InsideTriangle(e2, c1, c2, point))
				return true;

			if (error > 0 && (
				Geometry.DistanceFromLineSegment(e1, e2, point) < error ||
				Geometry.DistanceFromLineSegment(e1, c1, point) < error ||
				Geometry.DistanceFromLineSegment(c1, c2, point) < error ||
				Geometry.DistanceFromLineSegment(c1, e2, point) < error ||
				Geometry.DistanceFromLineSegment(c2, e2, point) < error ||
				Geometry.DistanceFromLineSegment(c2, e1, point) < error))
				return true;

			return false;
		}

        // Does this quad intersect the rectangle? This doesn't necessarily mean the bezier
        // intersects the rectangle.
        public bool QuadIntersectsRectangle(RectangleF rect)
        {
            RectangleF boundingBox = BoundingBox();

            // Fastest test -- if the bounding boxes don't intersect, then they don't intersect.
            if (! boundingBox.IntersectsWith(rect))
                return false;

            // 3 possibilities: quad contains rect, rect contains quad, or the boundaries intersect.

            // If the rectangle contains any of the points, they intersect.
            if (rect.Contains(end1) || rect.Contains(end2) || rect.Contains(control1) || rect.Contains(control2))
                return true;

            // If any of the lines intersect the rect, it intersects.
            if (Geometry.LineSegmentIntersectsRect(end1, end2, rect))           return true;
            if (Geometry.LineSegmentIntersectsRect(end1, control1, rect))       return true;
            if (Geometry.LineSegmentIntersectsRect(control1, control2, rect))   return true;
            if (Geometry.LineSegmentIntersectsRect(control1, end2, rect))       return true;
            if (Geometry.LineSegmentIntersectsRect(control2, end2, rect))       return true;
            if (Geometry.LineSegmentIntersectsRect(control2, end1, rect))       return true;

            // Possible the rect is entirely within the quad and fails all the above test. Check any point in the rect.
            if (IsPointInQuad(rect.Location, 0))
                return true;
            
            return false;
        }

		// Find the cooeficient value for a particular point on the bezier. If the point
		// isn't sufficiently close to the bezier, returns NaN. The error value indicates
		// how close the point might be to the bezier.
		public float FindCoefficient(PointF point, float error) {
			if (! IsPointInQuad(point, error))
				return float.NaN;

			float distance;
			return FindCoefficient(point, 0F, 1F, error, out distance);
		}

		// Worker for FindCoefficient. Takes a range of actual coefficients that the bezier
		// handles, and returns an approximate distance of point from the bezier.
		float FindCoefficient(PointF point, float c1, float c2, float error, out float distance) {
			Debug.Assert(IsPointInQuad(point, error));
			Debug.Assert(c2 > c1);

			if (IsFlat(error)) 
			{
				// The point is within the quad and the bezier is sufficiently close to a straight line.
				PointF pointOnLine = Geometry.ClosestPointOnLineSegment(end1, end2, point);
				distance = Geometry.DistanceF(pointOnLine, point);
				float u;
				if (end1 == end2)
					u = 0;
				else if (Math.Abs(end2.X - end1.X) > Math.Abs(end2.Y - end1.Y))
					u = (pointOnLine.X - end1.X) / (end2.X - end1.X);
				else
					u = (pointOnLine.Y - end1.Y) / (end2.Y - end1.Y);

				return c1 + u * (c2 - c1);
			}

			// Split into two beziers, and recurse on them.
			Bezier bez1, bez2;
			this.SplitAtCoefficient(0.5F, out bez1, out bez2);
			float c3 = (c1 + c2) / 2;
			float res1 = float.NaN, res2 = float.NaN;
			float dist1 = float.PositiveInfinity, dist2 = float.PositiveInfinity;

			if (bez1.IsPointInQuad(point, error)) {
				res1 = bez1.FindCoefficient(point, c1, c3, error, out dist1);
			}

			if (bez2.IsPointInQuad(point, error)) {
				res2 = bez2.FindCoefficient(point, c3, c2, error, out dist2);
			}

			if (!float.IsNaN(res1) && (float.IsNaN(res2) || dist1 < dist2)) {
				distance = dist1;
				return res1;
			}
			else {
				distance = dist2;
				return res2;
			}
		}

		// Split a bezier at a particular value of t into two beziers of each half
		public void SplitAtCoefficient(float t, out Bezier bez1, out Bezier bez2) {
			float s = 1 - t;
			PointF f00t, f01t, f11t, f0tt, f1tt, fttt;
			f00t = f01t = f11t = f0tt = f1tt = fttt = new PointF();

			f00t.X = s * end1.X + t * control1.X;
			f00t.Y = s * end1.Y + t * control1.Y;
			f01t.X = s * control1.X + t * control2.X;
			f01t.Y = s * control1.Y + t * control2.Y;
			f11t.X = s * control2.X + t * end2.X;
			f11t.Y = s * control2.Y + t * end2.Y;
			f0tt.X = s * f00t.X + t * f01t.X;
			f0tt.Y = s * f00t.Y + t * f01t.Y;
			f1tt.X = s * f01t.X + t * f11t.X;
			f1tt.Y = s * f01t.Y + t * f11t.Y;
			fttt.X = s * f0tt.X + t * f1tt.X;
			fttt.Y = s * f0tt.Y + t * f1tt.Y;

			bez1 = new Bezier(end1, f00t, f0tt, fttt);
			bez2 = new Bezier(fttt, f1tt, f11t, end2);
		}

		// Split a bezier at a particular point into two beziers. Returns false if the point
		// isn't within error of the bezier.
		public bool SplitAtPoint(PointF point, float error, out Bezier bez1, out Bezier bez2) {
			bez1 = bez2 = new Bezier();
			float t = FindCoefficient(point, error);
			if (float.IsNaN(t))
				return false;

			SplitAtCoefficient(t, out bez1, out bez2);
			return true;
		}

        private void RecursivelyFlatten(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4, double tolerance, List<PointF> list, int level)
        {
            // This method is VERY performance critical!!

            // Check if the bezier is "flat enough".

            double distance = 0;  // Not a real distance -- a combined taxicab metric that works just as well and is fast to compute.
            double t;

            t = x1 + x3 - x2 - x2;
            distance += (t < 0) ? -t : t;
            t = y1 + y3 - y2 - y2;
            distance += (t < 0) ? -t : t;
            t = x2 + x4 - x3 - x3;
            distance += (t < 0) ? -t : t;
            t = y2 + y4 - y3 - y3;
            distance += (t < 0) ? -t : t;
            if (distance <= tolerance || level > 20)
            {
                return;
            }

            // Calculate all the mid-points of the line segments
            //----------------------
            double x12   = (x1 + x2) / 2;
            double y12   = (y1 + y2) / 2;
            double x23   = (x2 + x3) / 2;
            double y23   = (y2 + y3) / 2;
            double x34   = (x3 + x4) / 2;
            double y34   = (y3 + y4) / 2;
            double x123  = (x12 + x23) / 2;
            double y123  = (y12 + y23) / 2;
            double x234  = (x23 + x34) / 2;
            double y234  = (y23 + y34) / 2;
            double x1234 = (x123 + x234) / 2;
            double y1234 = (y123 + y234) / 2;

            // Continue subdivision
            //----------------------
            RecursivelyFlatten(x1, y1, x12, y12, x123, y123, x1234, y1234, tolerance, list, level+1); 
            list.Add(new PointF((float) x1234, (float) y1234));  // CONSIDER: better results using x23,y23?
            RecursivelyFlatten(x1234, y1234, x234, y234, x34, y34, x4, y4, tolerance, list, level+1); 
        }

        // Flatten a bezier into a series of line segments with the given error, and
        // add those segments to a List<PointF>
        // Algorithm from: http://www.antigrain.com/research/adaptive_bezier/index.html
        public void Flatten(float error, List<PointF> list) {
            list.Add(end1);
            RecursivelyFlatten(end1.X, end1.Y, control1.X, control1.Y, control2.X, control2.Y, end2.X, end2.Y, error * 2.5F, list, 1);
            list.Add(end2);
        }
  	}
}
