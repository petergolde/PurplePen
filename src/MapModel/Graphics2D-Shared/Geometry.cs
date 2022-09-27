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
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using Matrix = System.Drawing.Drawing2D.Matrix;
using Color = System.Drawing.Color;

namespace PurplePen.Graphics2D
{
    public delegate void Operation();

    public static class Geometry
    {
        // Converts an angle in radians, where 0 is to the right, to an angle in degrees, where 0 is up.
        public static double RadiansToDegrees(double radians)
        {
            radians -= Math.PI / 2;      // make zero straight up.
            if (radians < 0)
                radians += (Math.PI * 2F);
            return radians * (180.0 / Math.PI);
        }

        public static double DegreesToRadians(double degrees)
        {
            double radians = degrees * Math.PI / 180.0;
            radians += Math.PI / 2;
            return radians;
        }

        public static double Distance(PointF pt1, PointF pt2)
        {
            double delta1 = (double)pt2.X - (double)pt1.X;
            double delta2 = (double)pt2.Y - (double)pt1.Y;
            return Math.Sqrt(delta1 * delta1 + delta2 * delta2);
        }

        public static float DistanceF(PointF pt1, PointF pt2)
        {
            return (float)Distance(pt1, pt2);
        }

        public static double DistanceSquared(PointF pt1, PointF pt2)
        {
            double delta1 = (double)pt2.X - (double)pt1.X;
            double delta2 = (double)pt2.Y - (double)pt1.Y;
            return delta1 * delta1 + delta2 * delta2;
        }

        public static PointF MidPoint(PointF pt1, PointF pt2)
        {
            return new PointF((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2);
        }

        // Return a point a given distance from pt1 towards pt2.
        public static PointF DistanceAlongLine(PointF pt1, PointF pt2, double distance)
        {
            double ratio = distance / Geometry.Distance(pt1, pt2);
            return new PointF((float)(pt1.X + (pt2.X - pt1.X) * ratio), (float)(pt1.Y + (pt2.Y - pt1.Y) * ratio));
        }
        
        public static float Angle(PointF pt1, PointF pt2)
        {
            if (pt1 == pt2)
                return 0.0F;
            else
                return (float)(Math.Atan2(pt2.Y - pt1.Y, pt2.X - pt1.X) * 360.0 / (Math.PI * 2));
        }

        // Get the angle at point2 between pt1 and pt3. 
        public static float Angle(PointF pt1, PointF pt2, PointF pt3)
        {
            if (pt1 == pt2 || pt2 == pt3)
                return 0.0F;

            double angle = Math.Abs(Math.Atan2(pt1.Y - pt2.Y, pt1.X - pt2.X) - Math.Atan2(pt3.Y - pt2.Y, pt3.X - pt2.X));
            if (angle > Math.PI)
                angle = (Math.PI * 2) - angle;     // convert to < a line.

            return (float)(angle * 360.0 / (Math.PI * 2));   // convert to degrees.
        }
        
        public static PointF MoveDistance(PointF pt, float distance, float angle)
        {
            pt.X = (float)(pt.X + distance * Math.Cos(angle / 360.0 * (Math.PI * 2)));
            pt.Y = (float)(pt.Y + distance * Math.Sin(angle / 360.0 * (Math.PI * 2)));
            return pt;
        }

        // Given two directions, in radians, determine the different between them as an 
        // angle between 0 and PI.
        public static double AngleAbsoluteDifferenceRadians(double angle1, double angle2)
        {
            return Math.Abs(Math.IEEERemainder(angle1 - angle2, Math.PI));
        }

        // Multiple two matrixes, giving a third
        public static Matrix Multiply(Matrix m1, Matrix m2)
        {
            Matrix result = m1.Clone();
            result.Multiply(m2, System.Drawing.Drawing2D.MatrixOrder.Append);
            return result;
        }

        // Transform one point according to a matrix. 
        public static PointF TransformPoint(PointF pt, Matrix matrix)
        {
            PointF[] xformedPts = { pt };
            matrix.TransformPoints(xformedPts);
            return xformedPts[0];
        }

        // Transform points according to a matrix. Does NOT change the input points.
        public static PointF[] TransformPoints(PointF[] pts, Matrix matrix)
        {
            PointF[] xformedPts = (PointF[])pts.Clone();
            matrix.TransformPoints(xformedPts);
            return xformedPts;
        }

        // Transform a distance via a matrix
        public static float TransformDistance(float src, Matrix mat)
        {
            PointF[] pts = { new PointF(0, 0), new PointF(src, 0) };
            mat.TransformPoints(pts);
            return (float)Geometry.Distance(pts[0], pts[1]);
        }

        // Transform an angle via a matrix.
        public static float TransformAngle(float angleInDegrees, Matrix transform)
        {
            // Quick check if transform has no rotation in it.
            float[] elements = transform.Elements;
            if (elements[1] == 0 && elements[2] == 0)
                return angleInDegrees;

            double angleInRadians = (float) Geometry.DegreesToRadians(angleInDegrees);
            PointF origin = new PointF();
            PointF pt = new PointF((float) Math.Cos(angleInRadians), (float) Math.Sin(angleInRadians));
            PointF transformedOrigin = Geometry.TransformPoint(origin, transform);
            PointF transformedPt = Geometry.TransformPoint(pt, transform);
            double transformedAngle = Math.Atan2(transformedPt.Y - transformedOrigin.Y, transformedPt.X - transformedOrigin.X);

            return (float) RadiansToDegrees(transformedAngle);
        }

        // Transform a rectangle via a matrix. Must not rotate!
        public static RectangleF TransformRectangle(Matrix mat, RectangleF src)
        {
            PointF[] pts = { src.Location, new PointF(src.Right, src.Bottom) };
            mat.TransformPoints(pts);
            return RectFromPoints(pts[0].X, pts[0].Y, pts[1].X, pts[1].Y);
        }

        // Transform a rectangle with a transform, and return the new rectangle that bounds the corners of the transformed one.
        public static RectangleF BoundsOfTransformedRectangle(RectangleF rect, Matrix transform)
        {
            PointF[] corners = { new PointF(rect.Left, rect.Top),    new PointF(rect.Right, rect.Top),
							 new PointF(rect.Left, rect.Bottom), new PointF(rect.Right, rect.Bottom) };
            corners = TransformPoints(corners, transform);
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < corners.Length; ++i)
            {
                if (corners[i].X < minX) minX = corners[i].X;
                if (corners[i].X > maxX) maxX = corners[i].X;
                if (corners[i].Y < minY) minY = corners[i].Y;
                if (corners[i].Y > maxY) maxY = corners[i].Y;
            }

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }


        // Rotate a rectangle around a point, and return the new rectangle that bounds the corners of the rotated one.
        public static RectangleF BoundsOfRotatedRectangle(RectangleF rect, PointF rotateAt, float angle)
        {
            if (angle == 0)
            {
                return rect;
            }
            else
            {
                Matrix m = new Matrix();
                m.RotateAt(angle, rotateAt);
                return BoundsOfTransformedRectangle(rect, m);
            }
        }

        // Find the transformation matrix that transform a rectangle (with positive Y upward) to a different
        // rectangle (with positive Y upward)
        public static Matrix CreateRectangleTransform(RectangleF source, RectangleF dest)
        {
            SizeF sourceSize = source.Size, destSize = dest.Size;

            Matrix m = new Matrix();
            m.Translate(-source.Left, -source.Top);
            m.Scale(destSize.Width / sourceSize.Width, destSize.Height / sourceSize.Height, System.Drawing.Drawing2D.MatrixOrder.Append);
            m.Translate(dest.Left, dest.Top, System.Drawing.Drawing2D.MatrixOrder.Append);
            return m;
        }
        
        // Find the transformation matrix that transform a rectangle (with positive Y downward) to a different
        // rectangle (with positive Y upward)
        public static Matrix CreateInvertedRectangleTransform(RectangleF source, RectangleF dest)
        {
            SizeF sourceSize = source.Size, destSize = dest.Size;

            Matrix m = new Matrix();
            m.Translate(-source.Left, -source.Top);
            m.Scale(destSize.Width / sourceSize.Width, -destSize.Height / sourceSize.Height, System.Drawing.Drawing2D.MatrixOrder.Append);
            m.Translate(dest.Left, dest.Bottom, System.Drawing.Drawing2D.MatrixOrder.Append);
            return m;
        }

        public static RectangleF InflateRect(RectangleF rect, float delta)
        {
            rect.Inflate(delta, delta);
            return rect;
        }

        public static RectangleF ScaleRect(RectangleF rect, float scale)
        {
            return new RectangleF(rect.Left * scale, rect.Bottom * scale, rect.Width * scale, rect.Height * scale);
        }

        // Get the center point of a rectangle.
        public static PointF RectCenter(RectangleF rect)
        {
            return new PointF((rect.Left + rect.Right) / 2, (rect.Top + rect.Bottom) / 2);
        }

        // Get a rectangle from any two opposite corners.
        public static RectangleF RectFromPoints(float x1, float y1, float x2, float y2)
        {
            float left = Math.Min(x1, x2), right = Math.Max(x1, x2);
            float top = Math.Min(y1, y2), bottom = Math.Max(y1, y2);
            return new RectangleF(left, top, right - left, bottom - top);
        }

        public static RectangleF RectFromPoints(PointF pt1, PointF pt2)
        {
            return RectFromPoints(pt1.X, pt1.Y, pt2.X, pt2.Y);
        }


        public static RectangleF RectangleFromCenterSize(PointF center, SizeF size)
        {
            return new RectangleF(center.X - size.Width / 2, center.Y - size.Height / 2, size.Width, size.Height);
        }

        // Determine distance of a point from a rectange. If in the interior of the rectangle, distance is zero.
        public static float DistanceFromRectangle(RectangleF rect, PointF point)
        {
            float distX = 0, distY = 0;
            if (point.X < rect.Left)
                distX = rect.Left - point.X;
            else if (point.X > rect.Right)
                distX = point.X - rect.Right;
            if (point.Y < rect.Top)
                distY = rect.Top - point.Y;
            else if (point.Y > rect.Bottom)
                distY = point.Y - rect.Bottom;

            if (distX == 0)
                return distY;
            else if (distY == 0)
                return distX;
            else
                return (float) Math.Sqrt(distX * distX + distY * distY);
        }

        // Return a centered location for of a rectangle relative to another.
        public static RectangleF CenteredRectangle(SizeF size, RectangleF centerOn)
        {
            PointF center = new PointF((centerOn.Left + centerOn.Right) / 2F, (centerOn.Top + centerOn.Bottom) / 2F);
            PointF upperLeft = new PointF(center.X - size.Width / 2, center.Y - size.Height / 2);
            return new RectangleF(upperLeft, size);
        }

        // Return true if "container" is strictly outside "contained" in at least one dimensions.
        public static bool IsLargerRect(RectangleF container, RectangleF contained)
        {
            return (container.Left < contained.Left || container.Right > contained.Right || container.Top < contained.Top || container.Bottom > contained.Bottom);
        }

        // Increase the given rectangle so that all coordinates an exact multiple of "pixelSize" from the given origin.
        public static RectangleF InflateToPixelBoundaries(RectangleF rect, double pixelSize, PointF origin)
        {
            return RectangleF.FromLTRB(
                DecreaseToIntegralMultiple(rect.Left, pixelSize, origin.X),
                DecreaseToIntegralMultiple(rect.Top, pixelSize, origin.Y),
                IncreaseToIntegralMultiple(rect.Right, pixelSize, origin.X),
                IncreaseToIntegralMultiple(rect.Bottom, pixelSize, origin.Y));
        }

        private static float IncreaseToIntegralMultiple(float x, double pixelSize, float origin)
        {
            return (float) (Math.Ceiling((x - origin) / pixelSize) * pixelSize + origin);
        }

        private static float DecreaseToIntegralMultiple(float x, double pixelSize, float origin)
        {
            return (float) (Math.Floor((x - origin) / pixelSize) * pixelSize + origin);
        }

        // Find two control points around p2.
        public static void FindControlPoints(PointF p1, PointF p2, PointF p3, out PointF c1, out PointF c2)
        {
            if (p1 == p3)
            {
                c1 = c2 = p2;
            }
            else
            {
                float angle = Angle(p1, p3);
                c1 = MoveDistance(p2, DistanceF(p2, p1) / 3, angle + 180);
                c2 = MoveDistance(p2, DistanceF(p2, p3) / 3, angle);
            }
        }

        // Find an end control point nearest p1 on the way to c2 and p2.
        public static PointF FindEndControlPoint(PointF p1, PointF p2, PointF c2)
        {
            if (p1 == p2 || p2 == c2)
                return p1;

            float angleControl = Angle(p2, c2);
            float anglePoint = Angle(p2, p1);
            return MoveDistance(p1, DistanceF(p1, p2) / 3, anglePoint - (angleControl - anglePoint) + 180);
        }

        // Return point on a infinite line closest to another point.
        public static PointF ClosestPointOnLine(PointF start, PointF end, PointF point)
        {
            double u;

            if (end.X == start.X && end.Y == start.Y)
            {
                // special case: line segment is a point.
                return new PointF(float.NaN, float.NaN);
            }

            // computer parameterized point on the full line nearest to point.
            u = ((point.X - start.X) * (end.X - start.X) + (point.Y - start.Y) * (end.Y - start.Y)) / ((end.X - start.X) * (end.X - start.X) + (end.Y - start.Y) * (end.Y - start.Y));

            PointF closest = new PointF(
                (float)(start.X + u * (end.X - start.X)),
                (float)(start.Y + u * (end.Y - start.Y)));
            return closest;
        }

        // Return distance from infinite line
        public static float DistanceFromLine(PointF start, PointF end, PointF point)
        {
            return DistanceF(ClosestPointOnLine(start, end, point), point);
        }

        // Return point on a line segment closest to another point.
        public static PointF ClosestPointOnLineSegment(PointF start, PointF end, PointF point)
        {
            double u;

            if (end.X == start.X && end.Y == start.Y)
            {
                // special case: line segment is a point.
                return start;
            }

            // computer parameterized point on the full line nearest to point.
            u = (((double)point.X - (double)start.X) * ((double)end.X - (double)start.X) + ((double)point.Y - (double)start.Y) * ((double)end.Y - (double)start.Y)) / (((double)end.X - (double)start.X) * ((double)end.X - (double)start.X) + ((double)end.Y - (double)start.Y) * ((double)end.Y - (double)start.Y));

            if (u < 0)
            {
                return start;
            }
            else if (u > 1)
            {
                return end;
            }
            else
            {
                PointF closest = new PointF(
                           (float)(start.X + u * ((double)end.X - (double)start.X)),
                           (float)(start.Y + u * ((double)end.Y - (double)start.Y)));
                return closest;
            }
        }

        // Determine if a point is on a line segment.
        public static bool IsPointOnLineSegment(PointF start, PointF end, PointF point, float error)
        {
            PointF closest = ClosestPointOnLineSegment(start, end, point);
            if (Distance(closest, point) < error)
                return true;
            else
                return false;
        }

        public static float DistanceFromLineSegment(PointF start, PointF end, PointF point)
        {
            return DistanceF(ClosestPointOnLineSegment(start, end, point), point);
        }

        // Determines if the lines AB and CD intersect, and where the intersection point is (if possible).
        public static bool LineSegmentsIntersect(PointF A, PointF B, PointF C, PointF D, out PointF intersectionPoint)
        {
            intersectionPoint = new PointF(float.NaN, float.NaN);

            PointF CmP = new PointF(C.X - A.X, C.Y - A.Y);
            PointF r = new PointF(B.X - A.X, B.Y - A.Y);
            PointF s = new PointF(D.X - C.X, D.Y - C.Y);

            float CmPxr = CmP.X * r.Y - CmP.Y * r.X;
            float CmPxs = CmP.X * s.Y - CmP.Y * s.X;
            float rxs = r.X * s.Y - r.Y * s.X;

            if (CmPxr == 0f) {
                // Lines are collinear, and so intersect if they have any overlap
                // No intersection point.

                return ((C.X - A.X < 0f) != (C.X - B.X < 0f))
                    || ((C.Y - A.Y < 0f) != (C.Y - B.Y < 0f));
            }

            if (rxs == 0f)
                return false; // Lines are parallel.

            float rxsr = 1f / rxs;
            float t = CmPxs * rxsr;
            float u = CmPxr * rxsr;

            bool intersects = (t >= 0f) && (t <= 1f) && (u >= 0f) && (u <= 1f);
            if (intersects) {
                intersectionPoint = new PointF(A.X + r.X * t, A.Y + r.Y * t);
            }
            return intersects;
        }

        public static bool LineSegmentsIntersect(PointF A, PointF B, PointF C, PointF D)
        {
            PointF intersectionPoint;
            return LineSegmentsIntersect(A, B, C, D, out intersectionPoint);
        }

        public static bool LineSegmentIntersectsRect(PointF pt1, PointF pt2, RectangleF rect)
        {
            // First some fast tests.
            if (rect.Contains(pt1))
                return true;
            if (rect.Contains(pt2))
                return true;
            if (!rect.IntersectsWith(RectFromPoints(pt1, pt2)))
                return false;

            if (LineSegmentsIntersect(pt1, pt2, new PointF(rect.Left, rect.Top), new PointF(rect.Right, rect.Top)))
                return true;
            if (LineSegmentsIntersect(pt1, pt2, new PointF(rect.Right, rect.Top), new PointF(rect.Right, rect.Bottom)))
                return true;
            if (LineSegmentsIntersect(pt1, pt2, new PointF(rect.Right, rect.Bottom), new PointF(rect.Left, rect.Bottom)))
                return true;
            if (LineSegmentsIntersect(pt1, pt2, new PointF(rect.Left, rect.Bottom), new PointF(rect.Left, rect.Top)))
                return true;

            return false;
        }
    

        // Determine if two points are on the same side of a line (defined by 2 points)
        public static bool SameSideOfLine(PointF line1, PointF line2, PointF point1, PointF point2)
        {
            float u = (point1.X - line1.X) * (line1.Y - line2.Y) + (point1.Y - line1.Y) * (line2.X - line1.X);
            float v = (point2.X - line1.X) * (line1.Y - line2.Y) + (point2.Y - line1.Y) * (line2.X - line1.X);
            return (u == 0 || v == 0 || Math.Sign(u) == Math.Sign(v));
        }

        // Determine if a point is inside a triangle
        public static bool InsideTriangle(PointF t1, PointF t2, PointF t3, PointF point)
        {
            return SameSideOfLine(t1, t2, t3, point) &&
                SameSideOfLine(t2, t3, t1, point) &&
                SameSideOfLine(t1, t3, t2, point);
        }

        // Determine the miter amount for an subtended angle
        public static float MiterFactor(float subtendedAngle)
        {
            subtendedAngle = Math.Abs(subtendedAngle);
            if (subtendedAngle > 0 && subtendedAngle < 180.0F)
                return (float)Math.Abs(1.0F / Math.Sin(subtendedAngle * Math.PI / 360.0));
            else
                return 1.0F;
        }

        public static float InchesFromMm(float mm)
        {
            return mm / 25.4F;
        }

        public static float HundredthsInchesFromMm(float mm)
        {
            return mm / 25.4F * 100F;
        }

        public static float PointsFromMm(float mm)
        {
            return mm / 25.4F * 72F;
        }

        public static float MmFromInches(float inches)
        {
            return inches * 25.4F;
        }

        public static float MmFromHundredthInches(float hundredthInches)
        {
            return hundredthInches / 100F * 25.4F;
        }

        public static float MmFromPoints(float points)
        {
            return points / 72F * 25.4F;
        }

        // Create a bounding box around a bunch of points.
        public static RectangleF BoundingBoxOfPoints(IEnumerable<PointF> pts)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;

            foreach (PointF p in pts) {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            return RectangleF.FromLTRB(minX, minY, maxX, maxY);
        }
    }
}
