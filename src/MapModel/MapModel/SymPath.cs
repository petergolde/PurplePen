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
using System.Text;
using System.Drawing;

namespace PurplePen.MapModel
{
    using PurplePen.Graphics2D;

    // PointKind.Interpolated is only for use within the "flatpoints" array, and indicates 
    // a point on a bezier that was interpolated.
    public enum PointKind: byte { Normal, BezierControl, Corner, Dash, Interpolated };

    public class SymPath 
    {
        RectangleF boundingBox;
        PointF[] points;   // points in this path
        PointKind[] kinds; // the point kinds for each point, always the same length as points.
        byte[] startStopFlags; // flags that indicate start/stop points, which can be used to get sub-paths. Each bit can control a different set. Can be null.

        // These are volatile, to support multi-threaded access and correct synchronization
        // in GetFlattenedPoints().
        volatile PointF[] flatpoints;   // points in the flattened path (no beziers)
        volatile PointKind[] flatkinds; // points kinds in the flattened path
        volatile byte[] flatStartStop; // start/stop points in the flattened path.

        bool lastPointSynthesized; // If true, the last point was added to close the curve when imported. Used for re-exporting to get round-trip.
        bool anyBeziers;    // Are there any beziers in this path
        // bool allBeziers;    // Are there all beziers in this path
        bool isClosedCurve; // true if end point is the same as start point (closed curve)
        bool isZeroLength;  // true if all points are the same.
        volatile float pathLength = -1;  // -1 means not computed yet.
        volatile float bizzarroLength = -1;  // -1 means not computed yet.
        volatile float maxMiter = 0;  // 0 means not computed yet.

        const float FLATTENAMOUNT = 0.01F;

        // Constants for the start/stop flags.
        public const byte DOUBLE_LEFT_STARTSTOPFLAG = 0x1;
        public const byte DOUBLE_RIGHT_STARTSTOPFLAG = 0x2;
        public const byte MAIN_STARTSTOPFLAG = 0x4;

        // A distance metric that computes the distance between two points.
        internal delegate double DistanceMetric(PointF pt1, PointF pt2);    

        // Create a path from points, point kinds, and start stop flags.
        // The start/stop flags works like this - each bit position is seperate (so up to 8 different sets).
        // A 1 bits in startStopFlags[k] means the segment from points[k] to points[k+1] is NOT present.
        public SymPath(PointF[] points, PointKind[] kinds, byte[] startStopFlags)
        {
            InitFromPoints(points, kinds, startStopFlags);
        }

        // Create a path from points and the kinds of those points.
        public SymPath(PointF[] points, PointKind[] kinds) {
            InitFromPoints(points, kinds, null);
        }

        // Create a path from normal points (no beziers)
        public SymPath(PointF[] points)
        {
            PointKind[] pointKinds = new PointKind[points.Length];
            for (int i = 0; i < pointKinds.Length; ++i)
                pointKinds[i] = PointKind.Normal;
            InitFromPoints(points, pointKinds, null);
        }

        // The last parameter true simply indicates that the last point in the point array was made the same
        // as the first to close the path.
        public SymPath(PointF[] points, PointKind[] kinds, byte[] startStopFlags, bool lastPointSynthesized) {
            InitFromPoints(points, kinds, startStopFlags);
            if (lastPointSynthesized) {
                this.lastPointSynthesized = true;
                Debug.Assert(isClosedCurve);
            }
        }

        public bool IsClosedCurve { get { return isClosedCurve; }}

        public bool LastPointSynthesized { get { return lastPointSynthesized; }}

        void InitFromPoints(PointF[] points, PointKind[] kinds, byte[] startStopFlags)
        {
            if (points.Length < 2)
                throw new MapUsageException("Points array must have at least two elements");
            if (points.Length != kinds.Length)
                throw new MapUsageException("points and kinds must have same length");
            if (startStopFlags != null && startStopFlags.Length != points.Length - 1)
                throw new MapUsageException("start/stop flags must be null or one less in length than points");

            this.points = (PointF[]) points.Clone();
            this.kinds = (PointKind[]) kinds.Clone();

            // Only copy start/stop flags if at least one is non-zero.
            this.startStopFlags = null;
            if (startStopFlags != null) {
                for (int i = 0; i < startStopFlags.Length; ++i) {
                    if (startStopFlags[i] != 0) {
                        this.startStopFlags = (byte[]) startStopFlags.Clone();
                        break;
                    }
                }
            }

            float boundingLeft = float.MaxValue;
            float boundingRight = float.MinValue;
            float boundingTop = float.MaxValue;
            float boundingBottom = float.MinValue;

            //allBeziers = true;
            isZeroLength = true;
            for (int i = 0; i < kinds.Length; ++i) 
            {
                PointF pt = points[i];
                float x = pt.X, y = pt.Y;
                if (x < boundingLeft)			boundingLeft = x;
                if (x > boundingRight)			boundingRight = x;
                if (y < boundingTop)			boundingTop = y;
                if (y > boundingBottom)		    boundingBottom = y;

                if (isZeroLength && pt != points[0])
                    isZeroLength = false;

                if (kinds[i] == PointKind.BezierControl) {
                    anyBeziers = true;
                    if (kinds[i - 1] == PointKind.BezierControl && kinds[i + 1] == PointKind.BezierControl)
                        throw new MapUsageException("exactly two bezier control points must be in sequence");
                    if (kinds[i - 1] != PointKind.BezierControl && kinds[i + 1] != PointKind.BezierControl)
                        throw new MapUsageException("exactly two bezier control points must be in sequence");
                }
                else {
                    if (i > 0 && kinds[i - 1] != PointKind.BezierControl) {
                        //allBeziers = false;            // 2 non-bezier control in a row.
                    }
                }
            }

            if (points[0] == points[points.Length - 1])
                isClosedCurve = true;

            boundingBox = new RectangleF(boundingLeft, boundingTop, boundingRight - boundingLeft, boundingBottom - boundingTop);
        }

        public PointF[] Points 
        {
            get { return points; }
        }

        public PointF[] FlattenedPoints
        {
            get
            {
                GetFlattenedPoints();
                return flatpoints;
            }
        }

        public PointKind[] PointKinds 
        {
            get { return kinds; }
        }

        public byte[] StartStopFlags
        {
            get { return startStopFlags; }
        }

        public RectangleF BoundingBox 
        {
            get { return boundingBox; }
        }

        // Get the first point on the path.
        public PointF FirstPoint
        {
            get { return points[0]; }
        }

        // Get the last point on the path.
        public PointF LastPoint
        {
            get { return points[points.Length - 1]; }
        }


        // Create a new path that is the current path transformed by a matrix.
        public SymPath Transform(Matrix mat) {
            if (mat.IsIdentity)
                return this;

            PointF[] pointsXform = Geometry.TransformPoints(points, mat);
            return new SymPath(pointsXform, kinds, startStopFlags, lastPointSynthesized);
        }

        // Returns true if the path might intersect the given rectangle. Due to beziers,
        // it may return true even if it doesn't intersect. (Exact result is returned for 
        // paths consisting only of line segments).
        public bool MayIntersectRect(RectangleF rect)
        {
            int i = 0;

            if (!boundingBox.IntersectsWith(rect))
                return false;

            while (i < points.Length - 1) {
                Debug.Assert(kinds[i] != PointKind.BezierControl);

                if (kinds[i + 1] != PointKind.BezierControl) {
                    // Segment from i to i+1 is a line.
                    if (Geometry.LineSegmentIntersectsRect(points[i], points[i + 1], rect))
                        return true;

                    i += 1;
                }
                else {
                    // Segment from i to i+3 is a bezier.
                    Debug.Assert(kinds[i + 2] == PointKind.BezierControl);
                    Debug.Assert(kinds[i + 3] != PointKind.BezierControl);
                    Bezier bez = new Bezier(points[i], points[i + 1], points[i + 2], points[i + 3]);
                    if (bez.QuadIntersectsRectangle(rect))
                        return true;

                    i += 3;
                }
            }

            return false;
        }

        public void Draw(IGraphicsTarget g, object penKey)
        {
            DrawCore(g, penKey, points);
        }

        public void DrawTransformed(IGraphicsTarget g, object penKey, Matrix transform)
        {
            PointF[] pointsXform = Geometry.TransformPoints(points, transform);
            DrawCore(g, penKey, pointsXform);
        }

        void DrawCore(IGraphicsTarget g, object penKey, PointF[] points) 
        {
            if (!anyBeziers)
            {
                if (isClosedCurve)
                    g.DrawPolygon(penKey, points);
                else
                    g.DrawPolyline(penKey, points);
            }
            else
            {
                var pathPartList = GetPathPartListCore(g, points, null, null);
                g.DrawPath(penKey, pathPartList);
            }
        }

        public void Fill(IGraphicsTarget g, object brushKey)
        {
            FillCore(g, brushKey, points, null, null);
        }

        public void FillTransformed(IGraphicsTarget g, object brushKey, Matrix transform)
        {
            FillTransformedWithHoles(g, brushKey, transform, null);
        }

        public void FillWithHoles(IGraphicsTarget g, object brushKey, SymPath[] holes) {
            PointF[][] holePoints = null;
            if (holes != null) {
                holePoints = new PointF[holes.Length][];
                for (int i = 0; i < holes.Length; ++i)
                    holePoints[i] = holes[i].points;
            }

            FillCore(g, brushKey, points, holes, holePoints);
        }

        public void FillTransformedWithHoles(IGraphicsTarget g, object brushKey, Matrix transform, SymPath[] holes) {
            PointF[] pointsXform = Geometry.TransformPoints(points, transform);

            PointF[][] holePoints = null;
            if (holes != null) {
                holePoints = new PointF[holes.Length][];
                for (int i = 0; i < holes.Length; ++i) {
                    holePoints[i] = Geometry.TransformPoints(holes[i].points, transform); 
                }
            }

            FillCore(g, brushKey, pointsXform, holes, holePoints);
        }

        void FillCore(IGraphicsTarget g, object brushKey, PointF[] points, SymPath[] holes, PointF[][] holePoints) 
        {
            if (anyBeziers || holes != null) 
            {
                // Use a path.
                var pathPartList = GetPathPartListCore(g, points, holes, holePoints);
                g.FillPath(brushKey, pathPartList, AreaFillMode.Alternate);
            }
            else 
            {
                // One simple line. Fill it in directly.
                g.FillPolygon(brushKey, points, AreaFillMode.Alternate);
            }
        }

        public object GetPathKey(IGraphicsTarget g)
        {
            return GetPathKeyCore(g, this, points, null, null);
        }

        public object GetPathKeyWithHoles(IGraphicsTarget g, object pathKey, SymPath[] holes)
        {
            if (holes == null) {
                return GetPathKeyCore(g, pathKey, points, null, null);
            }
            else
            {
                PointF[][] holePoints = null;
                if (holes != null)
                {
                    holePoints = new PointF[holes.Length][];
                    for (int i = 0; i < holes.Length; ++i)
                        holePoints[i] = holes[i].points;
                }
                return GetPathKeyCore(g, pathKey, points, holes, holePoints);
            }
        }

        static void AddToPathPartList(List<GraphicsPathPart> partList, SymPath path, PointF[] points, bool alwaysClose)
        {
            // Add the start point.
            partList.Add(new GraphicsPathPart(GraphicsPathPartKind.Start, new PointF[] { points[0] }));

            if (!path.anyBeziers)
            {
                PointF[] newArray = new PointF[points.Length - 1];
                Array.Copy(points, 1, newArray, 0, points.Length - 1);
                partList.Add(new GraphicsPathPart(GraphicsPathPartKind.Lines, newArray));
            }
            else
            {
                int iStart = 1, iEnd;
                while (iStart < points.Length)
                {
                    // First, scan ahead to find the number of points we can draw in a 
                    // single call. 
                    bool scanningBezier = (path.kinds[iStart] == PointKind.BezierControl);
                    for (iEnd = iStart; iEnd < points.Length; ++iEnd)
                    {
                        PointKind kind = path.kinds[iEnd];

                        // Check for switch between bezier/non-bezier.
                        if (!scanningBezier && kind == PointKind.BezierControl)
                            break;
                        if (scanningBezier && (iEnd - iStart + 1) % 3 != 0 && kind != PointKind.BezierControl)
                            break;
                    }

                    // Now we can add a component from iStart to iEnd, where iEnd is just beyond the last point.
                    if (iEnd > iStart)
                    {
                        PointF[] ptArray = new PointF[iEnd - iStart];
                        Array.Copy(points, iStart, ptArray, 0, ptArray.Length);
                        if (scanningBezier)
                            partList.Add(new GraphicsPathPart(GraphicsPathPartKind.Beziers, ptArray));
                        else
                            partList.Add(new GraphicsPathPart(GraphicsPathPartKind.Lines, ptArray));
                    }

                    // Advance iStart for the next component.
                    iStart = iEnd;
                }
            }

            if (alwaysClose || path.IsClosedCurve)
                partList.Add(new GraphicsPathPart(GraphicsPathPartKind.Close, null));
        }

        object GetPathKeyCore(IGraphicsTarget g, object pathKey, PointF[] points, SymPath[] holes, PointF[][] holePoints)
        {
            if (g.HasPath(pathKey))
                return pathKey;

            List<GraphicsPathPart> partList = GetPathPartListCore(g, points, holes, holePoints);

            g.CreatePath(pathKey, partList, AreaFillMode.Alternate);
            return pathKey;
        }

        List<GraphicsPathPart> GetPathPartListCore(IGraphicsTarget g, PointF[] points, SymPath[] holes, PointF[][] holePoints)
        {
            List<GraphicsPathPart> partList = new List<GraphicsPathPart>();

            AddToPathPartList(partList, this, points, holes != null);

            if (holes != null)
            {
                for (int i = 0; i < holes.Length; ++i)
                {
                    AddToPathPartList(partList, holes[i], holePoints[i], true);
                }
            }

            return partList;
        }


        // Draw a dashed lines. The dashLengths array contains the dash/gap lengths for the whole line. If we reach 
        // the end of this array, we wrap around. The variable offsetToStart indicates how far into the array to 
        // start.
        internal void DrawDashed(IGraphicsTarget g, object penKey, float[] dashLengths, float offsetToStart, DistanceMetric metric)
        {
            DrawDashedCore(g, penKey, dashLengths, offsetToStart, metric, 0, 1);
        }

        public void DrawDashed(IGraphicsTarget g, object penKey, float[] dashLengths, float offsetToStart)
        {
            DrawDashedCore(g, penKey, dashLengths, offsetToStart, EuclidDistance, 0, 1);
        }

        public void DrawDashedBizzarro(IGraphicsTarget g, object penKey, float[] dashLengths, float offsetToStart)
        {
            DrawDashedCore(g, penKey, dashLengths, offsetToStart, BizzarroDistance, 0, 1);
        }

        internal void DrawDashedOffset(IGraphicsTarget g, object penKey, float[] dashLengths, float offsetToStart, float offsetRight, float miterLimit, DistanceMetric metric)
        {
            DrawDashedCore(g, penKey, dashLengths, offsetToStart, metric, offsetRight, miterLimit);
        }


        public void DrawDashedOffset(IGraphicsTarget g, object penKey, float[] dashLengths, float offsetToStart, float offsetRight, float miterLimit)
        {
            DrawDashedCore(g, penKey, dashLengths, offsetToStart, EuclidDistance, offsetRight, miterLimit);
        }

        public void DrawDashedOffsetBizzarro(IGraphicsTarget g, object penKey, float[] dashLengths, float offsetToStart, float offsetRight, float miterLimit)
        {
            DrawDashedCore(g, penKey, dashLengths, offsetToStart, BizzarroDistance, offsetRight, miterLimit);
        }

        private void DrawDashedCore(IGraphicsTarget g, object penKey, float[] dashLengths, float offsetToStart, DistanceMetric metric, float offsetRight, float miterLimit)
        {
            double nextLength = dashLengths[0];  // distance to next dash beginning/end
            int curDash = 0;			// current index into dashLengths array.
            int i;									// index of current point being processed
            PointF prevPoint;				// previous point being processed
            bool inDrawnPart = true;		// true=in a drawn part, false=in a gap part
            PointF lastDashEnd;			// point last the was beginning/end of a dash
            int lastDashEndIndex;			// index into flatpoints just beyond that.

            // Consume offsetToStart.
            while (offsetToStart >= nextLength && offsetToStart > 0) {
                offsetToStart -= (float)nextLength;
                curDash++;
                if (curDash >= dashLengths.Length)
                    curDash = 0;
                nextLength = dashLengths[curDash];
                inDrawnPart = !inDrawnPart;
            }
            nextLength -= offsetToStart;

            // Set up the loop.
            GetFlattenedPoints();
            if (flatpoints.Length == 0)
                return;
            lastDashEnd = prevPoint = flatpoints[0];
            lastDashEndIndex = 1;
            i = 1;

            while (i < flatpoints.Length) 
            {
                // compute distance from prevPoint to nextPoint
                PointF nextPoint = flatpoints[i];
                double distX = nextPoint.X - prevPoint.X;
                double distY = nextPoint.Y - prevPoint.Y;
                double dist = metric(nextPoint, prevPoint);

                if (nextLength < dist) 
                {
                    // desired point is at or beyond prevPoint and before nextPoint.
                    double fraction = nextLength / dist; // percent between prevPoint and desired point.
                    PointF pt = new PointF((float) (prevPoint.X + distX * fraction),
                        (float) (prevPoint.Y + distY * fraction));

                    // pt is the beginning or end of a dash.
                    if (inDrawnPart) {
                        // Draw the dash. The start point is lastDashEnd, the end point is pt and
                        // the intermediate points are indexed lastDashEndIndex ... i - 1
                        PointF[] pts = new PointF[2 + (i - lastDashEndIndex)];
                        pts[0] = lastDashEnd;
                        pts[pts.Length - 1] = pt;
                        if (i > lastDashEndIndex) 
                            Array.Copy(flatpoints, lastDashEndIndex, pts, 1, i - lastDashEndIndex);
                        if (offsetRight != 0)
                            pts = OffsetPointsRight(pts, offsetRight, miterLimit);
                        g.DrawPolyline(penKey, pts);
                    }

                    // update lastDashEnd with the current point
                    lastDashEnd = pt;
                    lastDashEndIndex = i;
                    inDrawnPart = !inDrawnPart;

                    // find distant to next dash beginning/end
                    ++curDash;
                    if (curDash >= dashLengths.Length)
                        curDash = 0;
                    nextLength += dashLengths[curDash];
                }
                else 
                {
                    // desired point is at or beyond nextPoint
                    prevPoint = nextPoint;
                    nextLength -= dist;
                    ++i;
                }
            }

            if (inDrawnPart) {
                // Draw the last dash. 
                i = flatpoints.Length;
                PointF[] pts = new PointF[1 + (flatpoints.Length - lastDashEndIndex)];
                pts[0] = lastDashEnd;
                if (flatpoints.Length > lastDashEndIndex) 
                    Array.Copy(flatpoints, lastDashEndIndex, pts, 1, flatpoints.Length - lastDashEndIndex);
                if (offsetRight != 0)
                    pts = OffsetPointsRight(pts, offsetRight, miterLimit);
                g.DrawPolyline(penKey, pts);
            }
        }

        // Fill in the flatpoints and flatkinds fields with the flattened path. A 
        // flattened path simply has no Beziers.
        void GetFlattenedPoints()
        {
            if (flatpoints == null) {
                if (!anyBeziers) {
                    flatkinds = kinds;
                    flatStartStop = startStopFlags;
                    flatpoints = points;  // Must be last for correct threading syncronization!
                }
                else {
                    List<PointF> newPoints = new List<PointF>();
                    List<PointKind> newKinds = new List<PointKind>();
                    List<byte> newStartStop = null;
                    if (startStopFlags != null)
                        newStartStop = new List<byte>();

                    int iStart = 0;
                    byte startStop = 0;
                    while (iStart < points.Length) {
                        newPoints.Add(points[iStart]);
                        newKinds.Add(kinds[iStart]);
                        if (iStart != points.Length - 1 && newStartStop != null) {
                            startStop = startStopFlags[iStart];
                            newStartStop.Add(startStop);
                        }

                        if (iStart + 1 < points.Length && kinds[iStart + 1] == PointKind.BezierControl) {
                            // Get a bezier and convert it to lines.
                            newPoints.RemoveAt(newPoints.Count - 1);
                            Bezier bez = new Bezier(points[iStart], points[iStart + 1], points[iStart + 2], points[iStart + 3]);
                            bez.Flatten(FLATTENAMOUNT, newPoints);
                            newPoints.RemoveAt(newPoints.Count - 1);

                            while (newKinds.Count < newPoints.Count) {
                                newKinds.Add(PointKind.Interpolated);
                                if (newStartStop != null)
                                    newStartStop.Add(startStop);
                            }

                            iStart += 3;
                        }
                        else {
                            ++iStart;
                        }
                    }

                    flatkinds = newKinds.ToArray();
                    if (newStartStop != null)
                        flatStartStop = newStartStop.ToArray();
                    else
                        flatStartStop = null;
                    flatpoints = newPoints.ToArray(); // Must be last for correct thread synchronization!
                }
            }
        }

        // Normal Euclidean distance between points.
        internal static double EuclidDistance(PointF pt1, PointF pt2) {
            double delta1 = (double)pt2.X - (double)pt1.X;
            double delta2 = (double)pt2.Y - (double)pt1.Y;
            return Math.Sqrt(delta1 * delta1 + delta2 * delta2);
        }

        // Determines the Bizzarro distance between two points. This should be just the Pythagorian formula, but
        // for unknown reasons OCAD uses this metric for text along a path, and for dashes along a path. It makes no sense AT ALL.
        internal static double BizzarroDistance(PointF point1, PointF point2)
        {
            double deltaX = Math.Abs(point2.X - point1.X);
            double deltaY = Math.Abs(point2.Y - point1.Y);
            double bigDelta = Math.Max(deltaX, deltaY);
            double littleDelta = Math.Min(deltaX, deltaY);

            return bigDelta + littleDelta / 2;
        }


        // Computer the length of the path with a given metric.
        float ComputeLength(DistanceMetric metric)
        {
            // Get the points in the flattened path.
            double accumulate = 0.0;
            GetFlattenedPoints();

            // Traverse the flatpoints, computing the length.
            if (flatpoints.Length <= 1)
                return 0.0F;
            else {
                PointF lastPoint = flatpoints[0];
                int i = 1;

                while (i < flatpoints.Length) {
                    accumulate += metric(lastPoint, flatpoints[i]);
                    lastPoint = flatpoints[i];
                    ++i;
                }

                return (float) accumulate;
            }
        }

        public bool IsZeroLength
        {
            get { return isZeroLength; }
        }

        // Get the length of the path. Cached because it is used often.
        public float Length 
        {
            get 
            {
                if (pathLength < 0) 
                    pathLength = ComputeLength(EuclidDistance);

                return pathLength;
            }
        }

        // Get the length of the path with the Bizzarro metric. Cached because it is used often.
        public float BizzarroLength
        {
            get
            {
                if (bizzarroLength < 0)
                    bizzarroLength = ComputeLength(BizzarroDistance);

                return bizzarroLength;
            }
        }

        public float FindMaxDistance(PointF point)
        {
            double maxDist = 0.0;
            for (int i = 0; i < points.Length; ++i) 
            {
                double dist = Geometry.Distance(points[i], point);
                if (dist > maxDist)
                    maxDist = dist;
            }
            return (float) maxDist;
        }

        // Determine if a given point is within the path.
        public bool IsInside(PointF point) {
            if (!BoundingBox.Contains(point))
                return false;

            GetFlattenedPoints();

            bool inside = false;
            for (int i = 0, j = flatpoints.Length - 1; i < flatpoints.Length; j = i++) {
                if (((flatpoints[i].Y > point.Y) != (flatpoints[j].Y > point.Y)) &&
                    (point.X < (flatpoints[j].X - flatpoints[i].X) * (point.Y - flatpoints[i].Y) / (flatpoints[j].Y - flatpoints[i].Y) + flatpoints[i].X))
                    inside = !inside;
            }
            return inside;
        }


        // Find the point that is a given length along the path. If the distance is greater than
        // the length, returns the last point.
        public PointF PointAtLength(float length)
        {
            return PointAtLength(length, EuclidDistance);
        }

        public PointF PointAtLengthBizzarro(float length)
        {
            return PointAtLength(length, BizzarroDistance);
        }

        internal PointF PointAtLength(float length, DistanceMetric metric)
        {
            if (length <= 0)
                return points[0];

            // Set up the loop.
            GetFlattenedPoints();

            PointF prevPoint;			// previous point being processed
            prevPoint = flatpoints[0];
            int i = 1;
            double accumulate = 0.0;

            // Determine where in the (flattened) path the distance lies.
            while (i < flatpoints.Length) {
                // compute distance from prevPoint to nextPoint
                PointF nextPoint = flatpoints[i];
                double distX = nextPoint.X - prevPoint.X;
                double distY = nextPoint.Y - prevPoint.Y;
                double dist = metric(nextPoint, prevPoint);

                if (accumulate < length && accumulate + dist >= length) {
                    double fraction = (length - accumulate) / dist; // percent between last point and current point.
                    return new PointF((float) (prevPoint.X + distX * fraction),
                        (float) (prevPoint.Y + distY * fraction));
                }

                prevPoint = nextPoint;
                accumulate += dist;
                ++i;
            }

            // Went beyond the end.
            return flatpoints[flatpoints.Length - 1];
        }

        // Return a new path which is shorter at the beginning and end by the indicated
        // amounts. null may be returned if the old path was too short -- check for this!
        public SymPath Shorten(float startDistance, float endDistance)
        {
            return Shorten(startDistance, endDistance, EuclidDistance);
        }

        public SymPath ShortenBizzarro(float startDistance, float endDistance)
        {
            return Shorten(startDistance, endDistance, BizzarroDistance);
        }

        internal SymPath Shorten(float startDistance, float endDistance, DistanceMetric metric)
        {
            if (startDistance <= 0 && endDistance <= 0)
                return this;  // no shortening.

            // endDistance becomes the distance from the beginning!
            endDistance = ComputeLength(metric) - endDistance;
            if (endDistance <= startDistance) {
                // No line left!
                return null;
            }

            SymPath newPath, temp;

            // Split off the first part.
            if (startDistance > 0) {
                PointF splitPoint = PointAtLength(startDistance, metric);
                Split(splitPoint, out temp, out newPath);
                endDistance -= startDistance;
            }
            else {
                newPath = this;
            }

            // Split off the last part.
            SymPath result;
            if (endDistance > 0) {
                PointF splitPoint = newPath.PointAtLength(endDistance, metric);
                newPath.Split(splitPoint, out result, out temp);
            }
            else {
                result = newPath;
            }

            return result;
        }

 
        // Find points at particular distances along a path, and return the coordinates of the 
        // points and their angles. Points before beginning or after end of line are placed at the beginning/end of line.
        public PointF[] FindPointsAlongLine(float[] dashLengths, out float[] angles)
        {
            return FindPointsAlongLine(dashLengths, out angles, EuclidDistance);
        }

        public PointF[] FindPointsAlongLineBizzarro(float[] dashLengths, out float[] angles)
        {
            return FindPointsAlongLine(dashLengths, out angles, BizzarroDistance);
        }

        internal PointF[] FindPointsAlongLine(float[] dashLengths, out float[] angles, DistanceMetric metric)
        {
            double nextLength = dashLengths[0];  // distance to next dash beginning/end
            int curDash = 0;			// current index into dashLengths array.
            int i;						// index of current point being processed
            PointF prevPoint;			// previous point being processed
            PointF[] outputPoints;
            float[] outputAngles;
            int indexOutput;

            outputPoints = new PointF[dashLengths.Length];
            outputAngles = new float[dashLengths.Length];
            indexOutput = 0;

            // Set up the loop.
            GetFlattenedPoints();
            if (flatpoints.Length == 0) {
                angles = null;
                return null;
            }
            prevPoint = flatpoints[0];
            i = 1;

            while (i < flatpoints.Length) 
            {
                // compute distance from prevPoint to nextPoint
                PointF nextPoint = flatpoints[i];
                double distX = nextPoint.X - prevPoint.X;
                double distY = nextPoint.Y - prevPoint.Y;
                double dist = metric(prevPoint, nextPoint);

                if (nextLength < dist) 
                {
                    // desired point is at or beyond prevPoint and before nextPoint.
                    double fraction = nextLength / dist; // percent between prevPoint and desired point.
                    if (fraction < 0.0)      fraction = 0.0;
                    if (fraction > 1.0)      fraction = 1.0;

                    outputPoints[indexOutput] = new PointF((float) (prevPoint.X + distX * fraction),
                        (float) (prevPoint.Y + distY * fraction));
                    outputAngles[indexOutput] = (float) (Math.Atan2(distY, distX) * 360.0 / (Math.PI * 2));
                    ++indexOutput;

                    // find distant to next dash beginning/end
                    ++curDash;
                    if (curDash >= dashLengths.Length) {
                        break;
                    }
                    nextLength += dashLengths[curDash];
                }
                else 
                {
                    // desired point is at or beyond nextPoint
                    prevPoint = nextPoint;
                    nextLength -= dist;
                    ++i;
                }
            }

            while (indexOutput < dashLengths.Length) {
                // Missed last point(s). Put at end.
                outputPoints[indexOutput] = flatpoints[flatpoints.Length - 1];
                i = flatpoints.Length - 1;
                outputAngles[indexOutput] = (float) (Math.Atan2(flatpoints[i].Y - flatpoints[i-1].Y, flatpoints[i].X - flatpoints[i-1].X) * 360.0 / (Math.PI * 2));
                ++indexOutput;
            }

            angles = outputAngles;
            return outputPoints;
        }

        // Get the tangent angle at the given point.
        public float TangentAngleAtPoint(PointF pt)
        {
            PointF closestPoint;
            DistanceFromPoint(pt, out closestPoint);

            GetFlattenedPoints();
            int index = Array.IndexOf(flatpoints, closestPoint);
            if (index >= 0) {
                return TangentAngleAtPoint(flatpoints, isClosedCurve, index);
            }
            else {
                for (int i = 0; i < flatpoints.Length - 1; ++i) {
                    if (Geometry.ClosestPointOnLineSegment(flatpoints[i], flatpoints[i+1], closestPoint) == closestPoint) {
                        PointF[] threePoints = new PointF[] { flatpoints[i], closestPoint, flatpoints[i + 1] };
                        return TangentAngleAtPoint(threePoints, false, 1);
                    }
                }
            }

            return 0.0F; // shouldn't get here.
        }

        private static float TangentAngleAtPoint(PointF[] flattened, bool isClosed, int i)
        {
            float anglePrev = AngleIntoPoint(flattened, i, isClosed);
            float angleNext = AngleOutOfPoint(flattened, i, isClosed);
            if (float.IsNaN(anglePrev)) {
                if (float.IsNaN(angleNext))
                    return 0.0F;
                else
                    return angleNext;
            }
            else if (float.IsNaN(angleNext)) {
                return anglePrev;
            }
            else {
                // Make sure anglePrev and angleNext are within 180 degrees of each other
                if (anglePrev - 180F > angleNext)
                    anglePrev -= 360F;
                else if (angleNext - 180F > anglePrev)
                    angleNext -= 360F;

                return ((anglePrev + angleNext) / 2.0F);
            }
        }

        // Get perpendicular and subtended angle at a point. If isClosed, then the array is consider a closed
        // objects with first and last points connected.
        static void GetAnglesAtPoint(PointF[] points, int i, bool isClosed, out float perpAngle, out float subtendedAngle)
        {
            float anglePrev = AngleIntoPoint(points, i, isClosed);
            float angleNext = AngleOutOfPoint(points, i, isClosed);
            if (float.IsNaN(anglePrev)) {
                if (float.IsNaN(angleNext))
                    perpAngle = 0.0F;
                else
                    perpAngle = angleNext - 90.0F;
                subtendedAngle = 180.0F;
            }
            else if (float.IsNaN(angleNext)) {
                perpAngle = anglePrev - 90.0F;
                subtendedAngle = 180.0F;
            }
            else {
                // Make sure anglePrev and angleNext are within 180 degrees of each other
                if (anglePrev - 180F > angleNext)
                    anglePrev -= 360F;
                else if (angleNext - 180F > anglePrev)
                    angleNext -= 360F;

                perpAngle = ((anglePrev + angleNext) / 2.0F) - 90F;
                subtendedAngle = 180F - Math.Abs(anglePrev - angleNext);
            }
        }

        // Get the location of corner or dash points, and the perpendicular angles (on the right side) at those points, and the angle the path makes at those corners.
        // 0 degress is positive X (to the right), and 90 degrees is positive Y (up).
        // if "ignoreEnds" is true, never return an ends point.
        public PointF[] FindCornerOrDashPoints(PointKind pointKind, bool ignoreEnds, out float[] perpAngles, out float[] subtendedAngles) {
            int first = 0, end = points.Length;

            if (ignoreEnds) {
                first += 1;
                end -= 1;
            }

            // First, count the number of corner points.
            int numCorners = 0;
            for (int i = first; i < end; ++i) {
                if (kinds[i] == pointKind)
                    ++numCorners;
            }

            if (numCorners == 0) {
                perpAngles = subtendedAngles = null;
                return null;
            }

            PointF[] pts = new PointF[numCorners];
            perpAngles = new float[numCorners];
            subtendedAngles = new float[numCorners];
            int iCorner = 0;

            for (int i = first; i < end; ++i) {
                if (kinds[i] == pointKind) {
                    GetAnglesAtPoint(points, i, isClosedCurve, out perpAngles[iCorner], out subtendedAngles[iCorner]);
                    pts[iCorner] = points[i];
                    ++iCorner;
                }
            }

            return pts;
        }

        // Determine the maximum miter amount at a corner on the path.
        private float FindMaxMiter()
        {
            float[] temp, subtendedAngles;

            // Find maximum miter amount.
            FindCornerOrDashPoints(PointKind.Corner, true, out temp, out subtendedAngles);
            if (subtendedAngles != null && subtendedAngles.Length > 0) {
                float currentMaxMiter = 1.0F;
                for (int i = 0; i < subtendedAngles.Length; ++i) {
                    float m = Geometry.MiterFactor(subtendedAngles[i]);
                    if (m > currentMaxMiter)
                        currentMaxMiter = m;
                }
                return currentMaxMiter;
            }
            else
                return 1.0F;
        }

        // Get the maximum miter amount at a corner on the path. Cached for efficiency.
        public float MaxMiter
        {
            get
            {
                if (maxMiter == 0)
                    maxMiter = FindMaxMiter();
                return maxMiter;
            }
        }


        // Determine the angle the line makes as it enters point i, or NaN if that point
        // is at the beginning. If isClosed is true, then wraps around and only returns NaN if all points
        // are the same.
        static float AngleIntoPoint(PointF[] points, int i, bool isClosed) {
            int j = i - 1;

            while (j != i) {
                if (j < 0) {
                    if (isClosed && i != points.Length - 1)
                        j = points.Length - 1;
                    else
                        break;
                }

                if (points[j].X != points[i].X || points[j].Y != points[i].Y)
                    return (float) (Math.Atan2(points[i].Y - points[j].Y, points[i].X - points[j].X) * 360.0 / (Math.PI * 2));
                --j;
            }

            return float.NaN;
        }

        // Determine the angle the line makes as it exits point i, or NaN if that point
        // is at the end. If isClosed is true, then wraps around and only returns NaN if all points
        // are the same.
        static float AngleOutOfPoint(PointF[] points, int i, bool isClosed) {
            int j = i + 1;

            while (j != i) {
                if (j == points.Length) {
                    if (isClosed && i != 0)
                        j = 0;
                    else
                        break;
                }

                if (points[j].X != points[i].X || points[j].Y != points[i].Y)
                    return (float) (Math.Atan2(points[j].Y - points[i].Y, points[j].X - points[i].X) * 360.0 / (Math.PI * 2));
                ++j;
            }

            return float.NaN;
        }

        // Determine the distance of a point from this path (by Euclidean metric always!), 
        // The closest point on it (this is metric indipendent)
        // and the distance along the path to that point (via specified metric).
        private float DistanceFromPoint(PointF testPoint, DistanceMetric metric, out PointF closestPoint, out float distanceAlongPath) {
            float currentDistance = 0;
            double minDistSq = double.PositiveInfinity;
            closestPoint = new PointF();
            distanceAlongPath = 0;

            GetFlattenedPoints();

            for (int i = 0; i < flatpoints.Length - 1; ++i) {
                PointF closest = Geometry.ClosestPointOnLineSegment(flatpoints[i], flatpoints[i+1], testPoint);
                double distSq = Geometry.DistanceSquared(testPoint, closest);
                if (distSq < minDistSq) {
                    minDistSq = distSq;
                    closestPoint = closest;
                    distanceAlongPath = currentDistance + (float) metric(flatpoints[i], closest);
                }

                currentDistance += (float) metric(flatpoints[i], flatpoints[i + 1]);
            }

            return (float) Math.Sqrt(minDistSq);
        }

        // Determine the distance of a point from this path, and the closest point on it.
        public float DistanceFromPoint(PointF testPoint, out PointF closestPoint)
        {
            float temp;
            return DistanceFromPoint(testPoint, EuclidDistance, out closestPoint, out temp);
        }

        public float DistanceFromPoint(PointF testPoint)
        {
            PointF dummy;
            return DistanceFromPoint(testPoint, out dummy);
        }

        // Determine the length along the bezier to a given point on (or near) the path. Actually
        // determines the distance to the nearest point of the path.
        public float LengthToPoint(PointF point)
        {
            return LengthToPoint(point, EuclidDistance);
        }

        public float LengthToPointBizzarro(PointF point)
        {
            return LengthToPoint(point, BizzarroDistance);
        }

        internal float LengthToPoint(PointF point, DistanceMetric metric)
        {
            PointF temp;
            float distanceAlongPath;
            DistanceFromPoint(point, metric, out temp, out distanceAlongPath);
            return distanceAlongPath;
        }


        // Determine which straight-line or bezier segment contains
        // a point by returning the first and last points of that segment.
        // CONSIDER: the way we are doing this is fairly bogus. Need a better way!
        public void FindSegmentWithPoint(PointF point, float error, out int indexFirst, out int indexSecond) {
            GetFlattenedPoints();

            int iFlat = 0, iReg = 0;
            indexFirst = indexSecond = -1;
            do {
                if (flatkinds[iFlat] != PointKind.Interpolated) {
                    do {
                        ++iReg;
                    } while (kinds[iReg] == PointKind.BezierControl);
                }

                if (iFlat < flatpoints.Length - 1 && Geometry.IsPointOnLineSegment(flatpoints[iFlat], flatpoints[iFlat + 1], point, error)) {
                    indexSecond = iReg;
                    break;
                }

                ++iFlat;
            } while (iFlat < flatpoints.Length);

            if (indexSecond > 0) {
                if (kinds[indexSecond - 1] == PointKind.BezierControl)
                    indexFirst = indexSecond - 3;
                else
                    indexFirst = indexSecond - 1;
            }
        }

        // Determine if this sympath intersections another, and returns the intersection points.
        // Could be VERY SLOW if either symbol has beziers or a lot of line segments. Could be made faster
        // for these cases, but I'm currently only using it for very simple paths (<5 segements each).
        public bool Intersects(SymPath other, out PointF[] intersectionPoints)
        {
            bool intersects = false;
            intersectionPoints = null;

            RectangleF otherBoundingBox = other.BoundingBox;
            List<PointF> intersectionPointList = null;

            if (!this.BoundingBox.IntersectsWith(otherBoundingBox))
                return false;

            GetFlattenedPoints();
            PointF[] otherFlattened = other.FlattenedPoints;

            // Compare each line segment to each other line segment.
            for (int i = 0; i < flatpoints.Length - 1; ++i) {
                PointF pt1 = flatpoints[i], pt2 = flatpoints[i + 1];  // current line segment.
                if (!Geometry.LineSegmentIntersectsRect(pt1, pt2, otherBoundingBox))
                    continue;

                for (int j = 0; j < otherFlattened.Length - 1; ++j) {
                    PointF pt3 = otherFlattened[j], pt4 = otherFlattened[j + 1];  // current line segment.
                    PointF intersectionPoint;
                    if (Geometry.LineSegmentsIntersect(pt1, pt2, pt3, pt4, out intersectionPoint)) {
                        intersects = true;
                        if (!float.IsNaN(intersectionPoint.X) && !float.IsNaN(intersectionPoint.Y)) {
                            if (intersectionPointList == null)
                                intersectionPointList = new List<PointF>();
                            intersectionPointList.Add(intersectionPoint);
                        }
                    }
                }
            }

            if (intersectionPointList != null)
                intersectionPoints = intersectionPointList.ToArray();

            return intersects;
        }

        // Split a path into two paths at the given point. This point should have been
        // obtained by a call to DistanceFromPoint, so that it is known that this point
        // is on the path.
        public void Split(PointF point, out SymPath path1, out SymPath path2) {
#if DEBUG
            // Make sure the point is on the path.
            PointF closest;
            DistanceFromPoint(point, out closest);
            Debug.Assert(Geometry.Distance(point, closest) < FLATTENAMOUNT);
#endif
            // Find the segment (either a line segment or a single bezier) that
            // contains the given point
            int indexFirst, indexSecond;
            FindSegmentWithPoint(point, FLATTENAMOUNT, out indexFirst, out indexSecond);
            Debug.Assert(indexFirst >= 0 && indexSecond >= 0 && indexSecond > indexFirst);

            // Create the new points and kinds arrays for both new paths.
            int length = points.Length;
            PointF[] pts1, pts2;
            PointKind[] kinds1, kinds2;
            byte[] startStopFlags1 = null, startStopFlags2 = null;

            if (points[indexFirst] == point) {
                // We're splitting at the point indexFirst.
                pts1 = new PointF[indexFirst + 1];
                kinds1 = new PointKind[indexFirst + 1];
                Array.Copy(points, 0, pts1, 0, indexFirst + 1);
                Array.Copy(kinds, 0, kinds1, 0, indexFirst + 1);
                if (startStopFlags != null) {
                    startStopFlags1 = new byte[indexFirst];
                    Array.Copy(startStopFlags, 0, startStopFlags1, 0, indexFirst);
                }

                pts2 = new PointF[length - indexFirst];
                kinds2 = new PointKind[length - indexFirst];
                Array.Copy(points, indexFirst, pts2, 0, length - indexFirst);
                Array.Copy(kinds, indexFirst, kinds2, 0, length - indexFirst);
                if (startStopFlags != null) {
                    startStopFlags2 = new byte[length - indexFirst - 1];
                    Array.Copy(startStopFlags, indexFirst, startStopFlags2, 0, length - indexFirst - 1);
                }
            }
            else if (points[indexSecond] == point) {
                // We're splitting at the point indexSecond.
                pts1 = new PointF[indexSecond + 1];
                kinds1 = new PointKind[indexSecond + 1];
                Array.Copy(points, 0, pts1, 0, indexSecond + 1);
                Array.Copy(kinds, 0, kinds1, 0, indexSecond + 1);
                if (startStopFlags != null) {
                    startStopFlags1 = new byte[indexSecond];
                    Array.Copy(startStopFlags, 0, startStopFlags1, 0, indexSecond);
                }

                pts2 = new PointF[length - indexSecond];
                kinds2 = new PointKind[length - indexSecond];
                Array.Copy(points, indexSecond, pts2, 0, length - indexSecond);
                Array.Copy(kinds, indexSecond, kinds2, 0, length - indexSecond);
                if (startStopFlags != null) {
                    startStopFlags2 = new byte[length - indexSecond - 1];
                    Array.Copy(startStopFlags, indexSecond, startStopFlags2, 0, length - indexSecond - 1);
                }
            }
            else if (indexFirst + 1 == indexSecond) {
                // We're splitting inside a line segment.
                pts1 = new PointF[indexFirst + 2];
                kinds1 = new PointKind[indexFirst + 2];
                Array.Copy(points, 0, pts1, 0, indexFirst + 1);
                Array.Copy(kinds, 0, kinds1, 0, indexFirst + 1);
                pts1[indexFirst + 1] = point;
                kinds1[indexFirst + 1] = PointKind.Normal;
                if (startStopFlags != null) {
                    startStopFlags1 = new byte[indexFirst + 1];
                    Array.Copy(startStopFlags, 0, startStopFlags1, 0, indexFirst + 1);
                }

                pts2 = new PointF[length - indexSecond + 1];
                kinds2 = new PointKind[length - indexSecond + 1];
                Array.Copy(points, indexSecond, pts2, 1, length - indexSecond);
                Array.Copy(kinds, indexSecond, kinds2, 1, length - indexSecond);
                pts2[0] = point;
                kinds2[0] = PointKind.Normal;
                if (startStopFlags != null) {
                    startStopFlags2 = new byte[length - indexSecond];
                    Array.Copy(startStopFlags, indexSecond - 1, startStopFlags2, 0, length - indexSecond);  // last flag of startStopFlags1 is same as first flag of startStopFlags2, which is right.
                }
            }
            else {
                // We're splitting inside a bezier segment.
                Debug.Assert(indexFirst + 3 == indexSecond);
                Debug.Assert(kinds[indexFirst + 1] == PointKind.BezierControl && 
                    kinds[indexFirst + 2] == PointKind.BezierControl);

                Bezier bez = new Bezier(points[indexFirst], points[indexFirst+1], points[indexFirst+2], points[indexFirst+3]);
                Bezier bez1, bez2;
                bool success;
                success = bez.SplitAtPoint(point, FLATTENAMOUNT, out bez1, out bez2);
                Debug.Assert(success, "Point that should have been on bezier wasn't found");

                pts1 = new PointF[indexFirst + 4];
                kinds1 = new PointKind[indexFirst + 4];
                Array.Copy(points, 0, pts1, 0, indexFirst + 1);
                Array.Copy(kinds, 0, kinds1, 0, indexFirst + 1);
                pts1[indexFirst + 1] = bez1.control1;
                kinds1[indexFirst + 1] = PointKind.BezierControl;
                pts1[indexFirst + 2] = bez1.control2;
                kinds1[indexFirst + 2] = PointKind.BezierControl;
                pts1[indexFirst + 3] = bez1.end2;
                kinds1[indexFirst + 3] = PointKind.Normal;
                if (startStopFlags != null) {
                    startStopFlags1 = new byte[indexFirst + 3];
                    Array.Copy(startStopFlags, 0, startStopFlags1, 0, indexFirst + 1); // bezier control points always have 0 startStopFlags.
                }

                pts2 = new PointF[length - indexSecond + 3];
                kinds2 = new PointKind[length - indexSecond + 3];
                Array.Copy(points, indexSecond, pts2, 3, length - indexSecond);
                Array.Copy(kinds, indexSecond, kinds2, 3, length - indexSecond);
                pts2[0] = bez2.end1;
                kinds2[0] = PointKind.Normal;
                pts2[1] = bez2.control1;
                kinds2[1] = PointKind.BezierControl;
                pts2[2] = bez2.control2;
                kinds2[2] = PointKind.BezierControl;
                if (startStopFlags != null) {
                    startStopFlags2 = new byte[length - indexSecond + 2];
                    Array.Copy(startStopFlags, indexSecond, startStopFlags2, 3, length - indexSecond - 1);
                    startStopFlags2[0] = startStopFlags[indexFirst];
                }
            }

            // Create the two new paths.
            path1 = pts1.Length >= 2 ? new SymPath(pts1, kinds1, startStopFlags1, false) : null;
            path2 = pts2.Length >= 2 ? new SymPath(pts2, kinds2, startStopFlags2, false) : null;
        }

        // Return a path that is the segment between two points, both of which must lie on 
        // the path. First point must be before the second, or null is returned.
        public SymPath Segment(PointF pt1, PointF pt2)
        {
            PointF closest;

            // Points must be on the bezier.
            Debug.Assert(DistanceFromPoint(pt1, out closest) < FLATTENAMOUNT);
            Debug.Assert(DistanceFromPoint(pt2, out closest) < FLATTENAMOUNT);

            // Split at point 2.
            SymPath beginning, end, dummy;
            Split(pt2, out beginning, out end);

            if (beginning == null)
                return null;

            // If pt1 not on the bezier, the points must be in opposite order.
            if (beginning.DistanceFromPoint(pt1, out closest) >= FLATTENAMOUNT)
                return null;

            beginning.Split(pt1, out dummy, out end);
            return end;
        }

        // Join two paths. The last point of the first path must be the first point
        // of the second path. This is the join point. The new path has the join point
        // in the middle, with the given kind.
        public static SymPath Join(SymPath path1, SymPath path2, PointKind kind) {
            Debug.Assert(path1.points[path1.Points.Length - 1] == path2.points[0]);

            PointF[] points = new PointF[path1.points.Length + path2.points.Length - 1];
            PointKind[] kinds = new PointKind[path1.points.Length + path2.points.Length - 1];
            byte[] startStopFlags = null;

            int length = path1.points.Length;
            Array.Copy(path1.points, 0, points, 0, length);
            Array.Copy(path1.kinds,  0, kinds,  0, length);
            Array.Copy(path2.points, 1, points, length, path2.points.Length - 1);
            Array.Copy(path2.kinds,  1, kinds,  length, path2.kinds.Length  - 1);
            kinds[length - 1] = kind;

            if (path1.startStopFlags != null || path2.startStopFlags != null) {
                startStopFlags = new byte[path1.points.Length + path2.points.Length - 2];
                if (path1.startStopFlags != null)
                    Array.Copy(path1.startStopFlags, 0, startStopFlags, 0, path1.startStopFlags.Length);
                if (path2.startStopFlags != null)
                    Array.Copy(path2.startStopFlags, 0, startStopFlags, path1.points.Length - 1, path2.startStopFlags.Length);
            }

            return new SymPath(points, kinds, startStopFlags, false);
        }

        // Get the SymPath produced by offseting this path to the right. The offset amount can be negative to offset to the left.
        // It will always have only straight segments, since the input path is converted to straight segments first.
        // The miter limit controls how far an acute angle will stretch.
        public SymPath OffsetRight(float offsetAmount, float miterLimit)
        {
            if (offsetAmount == 0)
                return this;

            GetFlattenedPoints();
            PointF[] offsetPts = OffsetPointsRight(flatpoints, offsetAmount, miterLimit);
            return new SymPath(offsetPts, flatkinds, flatStartStop);
        }

        // Offset a set of points in a certain direction. 
        private static PointF[] OffsetPointsRight(PointF[] pts, float offsetAmount, float miterLimit)
        {
            PointF[] offsetPoints = new PointF[pts.Length];
            bool isClosed = (pts[0] == pts[pts.Length - 1]);

            // Do interior points. Offset point based on the angles in and out.
            for (int i = 0; i < pts.Length; ++i) {
                float perpAngle, subtendedAngle;

                GetAnglesAtPoint(pts, i, isClosed, out perpAngle, out subtendedAngle);
                float multiplier = Math.Min(miterLimit, Geometry.MiterFactor(subtendedAngle));
                float distance = multiplier * offsetAmount;
                float dx = (float) (Math.Cos(perpAngle * Math.PI / 180.0) * distance);
                float dy = (float) (Math.Sin(perpAngle * Math.PI / 180.0) * distance);
                offsetPoints[i] = new PointF(pts[i].X + dx, pts[i].Y + dy);
            }

            return offsetPoints;
        }



        // Get the distances of each segment where segments are deliniated by corner points.
        // The kinds[] array tells has one more element that the returned distances arrays and
        // tells the kind of point at each location, where start and end points are returned as 
        // corner points.
        public float[] GetCornerAndDashPointDistances(out PointKind[] pointkinds) 
        {
            return GetCornerAndDashPointDistances(EuclidDistance, out pointkinds);
        }

        public float[] GetCornerAndDashPointDistancesBizzarro(out PointKind[] pointkinds)
        {
            return GetCornerAndDashPointDistances(BizzarroDistance, out pointkinds);
        }

        internal float[] GetCornerAndDashPointDistances(DistanceMetric metric, out PointKind[] pointkinds) 
        {
            // First, count the number of corner points, plus 1. Ignore first and last point.
            int numCorners = 1;
            int i;
            for (i = 1; i < kinds.Length - 1; ++i) {
                PointKind kind = kinds[i];
                if (kind == PointKind.Corner || kind == PointKind.Dash)
                    ++numCorners;
            }
            
            float[] distances = new float[numCorners];
            pointkinds = new PointKind[numCorners + 1];
            pointkinds[0] = PointKind.Corner;
            int iDistance = 0;

            GetFlattenedPoints();
            double accumulate = 0.0;
            PointF lastPoint = flatpoints[0];
            i = 0;

            while (i < flatpoints.Length) 
            {
                if (i > 0) 
                {
                    accumulate += metric(lastPoint, flatpoints[i]);
                }
                if ((i != 0 && flatkinds[i] == PointKind.Corner) || i == flatpoints.Length - 1) 
                {
                    distances[iDistance++] = (float) accumulate;
                    accumulate = 0.0;
                    pointkinds[iDistance] = PointKind.Corner;
                }
                else if (i != 0 && flatkinds[i] == PointKind.Dash) 
                {
                    distances[iDistance++] = (float) accumulate;
                    accumulate = 0.0;
                    pointkinds[iDistance] = PointKind.Dash;
                }
                lastPoint = flatpoints[i];
                ++i;
            }

            return distances;
        }

        // Get subsegments for a path based on the start/stop points. If no start/stop points, just returns the current path.
        // If true subpath are returns, they have no start/stop points.
        public SymPath[] GetSubpaths(byte flagPosition)
        {
            Debug.Assert(flagPosition != 0 && (flagPosition & (flagPosition - 1)) == 0);         // must be a single bit.

            if (startStopFlags == null)
                return new SymPath[] { this };

            // Count the number of subpaths, and check if the only subpath is the whole path.

            int subPathCount = 0;
            bool anySetBits = false;
            bool previousBit = true;

            for (int i = 0; i < startStopFlags.Length; ++i) {
                if (kinds[i] == PointKind.BezierControl)
                    continue;     // skip bezier points.

                bool currentBit = ((startStopFlags[i] & flagPosition) != 0);

                if (currentBit)
                    anySetBits = true;
                else if (previousBit) 
                    ++subPathCount;            // This is the beginning of a subpath.

                previousBit = currentBit;
            }

            if (!anySetBits) {
                // The only sub-path is the whole path.
                Debug.Assert(subPathCount == 1);
                return new SymPath[] { this };
            }

            if (subPathCount == 0)
                return new SymPath[0];          // No subpath (all bits were set).

            // All the simple cases are handled. We have at least one non-trivial subpath.
            SymPath[] result = new SymPath[subPathCount];

            int index = 0;  // index of subpath we're working on.
            int startLocation = 0;         // start of current subpath we're in.
            previousBit = true;
            for (int i = 0; i < points.Length; ++i) {
                if (kinds[i] == PointKind.BezierControl)
                    continue;     // skip bezier points.

                bool currentBit = (i == startStopFlags.Length) || ((startStopFlags[i] & flagPosition) != 0);

                if (previousBit && !currentBit) {
                    // This is the beginning of a subpath.
                    startLocation = i;
                }
                else if (currentBit && !previousBit) {
                    // This is the end of a subpath.
                    int size = i - startLocation + 1;
                    PointF[] newPts = new PointF[size];
                    Array.Copy(points, startLocation, newPts, 0, size);
                    PointKind[] newKinds = new PointKind[size];
                    Array.Copy(kinds, startLocation, newKinds, 0, size);
                    result[index] = new SymPath(newPts, newKinds);
                    ++index;
                }

                previousBit = currentBit;
            }

            return result;
        }


        // Calculate the area of a closed path
        public float Area() {
            double sum = 0;

            Debug.Assert(IsClosedCurve);
            GetFlattenedPoints();

            for (int i = 0; i < flatpoints.Length - 1; ++i) {
                sum += ((double) flatpoints[i].X * (double) flatpoints[i+1].Y) - ((double) flatpoints[i].Y * (double) flatpoints[i+1].X);
            }

            return (float) (Math.Abs(sum) / 2.0);
        }

        // Calculate the centroid of the area bounded by a closed path
        // From: https://stackoverflow.com/questions/2792443
        public PointF AreaCentroid()
        {
            double sum = 0;
            double centroidX = 0, centroidY = 0;

            GetFlattenedPoints();

            int limit = IsClosedCurve ? flatpoints.Length - 1 : flatpoints.Length;

            for (int i = 0; i < limit; ++i) {
                double a;
                double x0 = flatpoints[i].X, x1 = flatpoints[(i + 1) % flatpoints.Length].X;
                double y0 = flatpoints[i].Y, y1 = flatpoints[(i + 1) % flatpoints.Length].Y;
                a = (x0 * y1) - (y0 * x1);
                sum += a;
                centroidX += (x0 + x1) * a;
                centroidY += (y0 + y1) * a;

            }

            sum /= 2.0;
            centroidX /= (sum * 6.0);
            centroidY /= (sum * 6.0);
            return new PointF((float) centroidX, (float) centroidY);
        }

        // Create a SymPath that is an ellipse inside the given rectange.
        public static SymPath CreateEllipsePath(RectangleF boundingRect) {
            float midX = (boundingRect.Left + boundingRect.Right) / 2;
            float midY = (boundingRect.Top + boundingRect.Bottom) / 2;
            float alphaX = Util.kappa / 2.0F * boundingRect.Width;
            float alphaY = Util.kappa / 2.0F * boundingRect.Height;
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

        // Create a SymPath that is a part of a circular arc. startAngle and sweepAngle are in degrees.
        public static SymPath CreateArcPath(PointF center, float radius, double startAngle, double sweepAngle)
        {
            if (startAngle < 0 || startAngle >= 360)
                throw new ArgumentException("startAngle must be >=0 and <360");
            if (sweepAngle < 0 || sweepAngle > 360)
                throw new ArgumentException("sweepAngle must be >=0 and <=360");

            float centerX = center.X, centerY = center.Y;
            float alpha = (float) (Util.kappa * radius);

            // Create a Bezier path that start at angle 0, then wraps a circle TWICE.
            PointF[] pts = {
                new PointF(centerX + radius, centerY),
                new PointF(centerX + radius, centerY + alpha),
                new PointF(centerX + alpha, centerY + radius),
                new PointF(centerX, centerY + radius),
                new PointF(centerX - alpha, centerY + radius),
                new PointF(centerX - radius, centerY + alpha),
                new PointF(centerX - radius, centerY),
                new PointF(centerX - radius, centerY - alpha),
                new PointF(centerX - alpha, centerY - radius),
                new PointF(centerX, centerY - radius),
                new PointF(centerX + alpha, centerY - radius),
                new PointF(centerX + radius, centerY - alpha),
                new PointF(centerX + radius, centerY),
                new PointF(centerX + radius, centerY + alpha),
                new PointF(centerX + alpha, centerY + radius),
                new PointF(centerX, centerY + radius),
                new PointF(centerX - alpha, centerY + radius),
                new PointF(centerX - radius, centerY + alpha),
                new PointF(centerX - radius, centerY),
                new PointF(centerX - radius, centerY - alpha),
                new PointF(centerX - alpha, centerY - radius),
                new PointF(centerX, centerY - radius),
                new PointF(centerX + alpha, centerY - radius),
                new PointF(centerX + radius, centerY - alpha),
                new PointF(centerX + radius, centerY),
            };

            PointKind[] kinds = {   PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
                PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
                PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
                PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
                PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
                PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
                PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
                PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
                PointKind.Normal };

            SymPath doubleCircle = new SymPath(pts, kinds);

            // Split the doubleCircle with the given start/sweep values.
            double length = doubleCircle.Length;
            double lengthToFirst = (startAngle / 720.0F) * length;
            double lengthToSecond = ((startAngle + sweepAngle) / 720.0F) * length;
            PointF firstPoint = doubleCircle.PointAtLength((float) lengthToFirst);
            PointF secondPoint = doubleCircle.PointAtLength((float)lengthToSecond);
            SymPath unneeded1, unneeded2, splitAfterFirst, result;
            doubleCircle.Split(firstPoint, out unneeded1, out splitAfterFirst);
            splitAfterFirst.Split(secondPoint, out result, out unneeded2);

            return result;
        }

        // Create a SymPath that is a rectangle.
        public static SymPath CreateRectanglePath(RectangleF rect) {
            PointF[] pts = {    new PointF(rect.Left, rect.Top), new PointF(rect.Right, rect.Top), new PointF(rect.Right, rect.Bottom), 
                new PointF(rect.Left, rect.Bottom), new PointF(rect.Left, rect.Top) };
            PointKind[] kinds = {PointKind.Corner, PointKind.Corner, PointKind.Corner, PointKind.Corner, PointKind.Corner};

            return new SymPath(pts, kinds);
        }

        // Create a rectangle path for the rectangle given rotated by the angle (in degrees) around the point (rect.Left, rect.Bottom).
        public static SymPath CreateRotatedRectanglePath(RectangleF rect, float angle)
        {
            PointF[] pts = {    new PointF(rect.Left, rect.Top), new PointF(rect.Right, rect.Top), new PointF(rect.Right, rect.Bottom), 
                new PointF(rect.Left, rect.Bottom), new PointF(rect.Left, rect.Top) };
            PointKind[] kinds = {PointKind.Corner, PointKind.Corner, PointKind.Corner, PointKind.Corner, PointKind.Corner};

            Matrix transform = new Matrix();
            transform.RotateAt(angle, new PointF(rect.Left, rect.Bottom));
            pts = Geometry.TransformPoints(pts, transform);

            return new SymPath(pts, kinds);
        }

        // Create a SymPath for a rectangle or a rounded rectangle
        public static SymPath CreateRoundedRectangle(RectangleF rect, float cornerRadius)
        {
            SizeF size = rect.Size;

            // The corner radius cannot be greater than half the width or height.
            if (size.Width < cornerRadius * 2)
                cornerRadius = size.Width / 2;
            if (size.Height < cornerRadius * 2)
                cornerRadius = size.Height / 2;

            // Create points and kinds for rectangle of given size at (0,0).
            PointKind[] kinds;
            PointF[] pathpts;
            if (size.Width == 0) {
                kinds = new PointKind[] { PointKind.Corner, PointKind.Corner };
                pathpts = new PointF[] { new PointF(0, 0), new PointF(0, size.Height) };

            }
            else if (size.Height == 0) {
                kinds = new PointKind[] { PointKind.Corner, PointKind.Corner };
                pathpts = new PointF[] { new PointF(0, 0), new PointF(size.Width, 0) };

            }
            else if (cornerRadius == 0) {
                kinds = new PointKind[] { PointKind.Corner, PointKind.Corner, PointKind.Corner, PointKind.Corner, PointKind.Corner };
                pathpts = new PointF[] { new PointF(0,0), new PointF(0, size.Height), new PointF(size.Width, size.Height), 
                                           new PointF(size.Width, 0), new PointF(0,0)};
            }
            else {
                kinds = new PointKind[] {PointKind.Normal, PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
                                            PointKind.Normal, PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
                                            PointKind.Normal, PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
                                            PointKind.Normal, PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
                                            PointKind.Normal};
                pathpts = new PointF[] { new PointF(cornerRadius, 0),
                                           new PointF(size.Width - cornerRadius, 0),
                                           new PointF(size.Width - (1-Util.kappa) * cornerRadius, 0),
                                           new PointF(size.Width, (1-Util.kappa) * cornerRadius),
                                           new PointF(size.Width, cornerRadius),
                                           new PointF(size.Width, size.Height - cornerRadius),
                                           new PointF(size.Width, size.Height - (1-Util.kappa) * cornerRadius),
                                           new PointF(size.Width - (1-Util.kappa) * cornerRadius, size.Height),
                                           new PointF(size.Width - cornerRadius, size.Height),
                                           new PointF(cornerRadius, size.Height),
                                           new PointF((1-Util.kappa) * cornerRadius, size.Height),
                                           new PointF(0, size.Height - (1-Util.kappa) * cornerRadius),
                                           new PointF(0, size.Height - cornerRadius),
                                           new PointF(0, cornerRadius),
                                           new PointF(0, (1-Util.kappa) * cornerRadius),
                                           new PointF((1-Util.kappa) * cornerRadius, 0),
                                           new PointF(cornerRadius, 0)};
            }

            // Offset points.
            for (int i = 0; i < pathpts.Length; ++i) {
                pathpts[i].X += rect.X;
                pathpts[i].Y += rect.Y;
            }

            return new SymPath(pathpts, kinds);
        }

        // override object.Equals to test two paths for equality.
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }

            SymPath other = (SymPath) obj;

            if ((object)other == (object)this)
                return true;

            if (other.points.Length != points.Length || other.lastPointSynthesized != lastPointSynthesized)
                return false;
            if ((other.startStopFlags == null && this.startStopFlags != null) || (other.startStopFlags != null && this.startStopFlags == null))
                return false;

            for (int i = 0; i < points.Length; i++) {
                if (other.points[i] != points[i] || other.kinds[i] != kinds[i])
                    return false;
                if (startStopFlags != null && i < startStopFlags.Length && other.startStopFlags[i] != this.startStopFlags[i])
                    return false;
            }

            return true;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            throw new NotSupportedException("GetHashCode not supported.");
        }

        // Change negative zero to positive zero for output in ToString.
        static float SanitizeZero(float f)
        {
            if (f >= -1E-5 && f <= 0.0F)
                return 0.0F;
            else
                return f;
        }

        // Create a string representation with the kinds and points of the path, with kinds represented by single letters.
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < kinds.Length; i++) {
                if (i != 0)
                    builder.Append("--");          // seperate points with two dashes.

                switch (kinds[i]) {
                case PointKind.Normal: builder.Append('N'); break;
                case PointKind.BezierControl: builder.Append('B'); break;
                case PointKind.Corner: builder.Append('C'); break;
                case PointKind.Dash: builder.Append('D'); break;
                case PointKind.Interpolated: builder.Append('I'); break;
                default:
                    Debug.Fail("bad point kind"); break;
                }

                // sanitize zeros so we don't get negative zeros.
                builder.AppendFormat("({0:0.##},{1:0.##})", SanitizeZero(points[i].X), SanitizeZero(points[i].Y));

                if (startStopFlags != null && i < startStopFlags.Length && startStopFlags[i] != 0)
                    builder.AppendFormat("/{0:X}", startStopFlags[i]);
            }

            return builder.ToString();
        }

        internal void CheckConstructed() {}
    }

    public class SymPathWithHoles {
        SymPath mainPath;
        public SymPath MainPath { get { return mainPath; }}
        SymPath[] holes;
        public SymPath[] Holes { get { return holes; }}
        RectangleF boundingBox;
        public RectangleF BoundingBox { get { return boundingBox; }}

        // Create a sympath from a path and various holes in it. If there are holes, the path
        // and all the holes must be closed.
        public SymPathWithHoles(SymPath path, SymPath[] holes) {
            Construct(path, holes);
        }


        private void Construct(SymPath path, SymPath[] holes) {
            Debug.Assert(path != null);
            path.CheckConstructed();

            if (holes != null && holes.Length == 0)
                holes = null;

            this.mainPath = path;
            this.holes = holes;

            // Check for closed.
            if (holes != null) {
                Debug.Assert(mainPath.IsClosedCurve);
                foreach (SymPath hole in holes) 
                    Debug.Assert(hole.IsClosedCurve);
            }

            // Compute bounding box, including the holes.
            RectangleF box = path.BoundingBox;
            if (holes != null) {
                foreach (SymPath hole in holes) 
                    box = RectangleF.Union(box, hole.BoundingBox);
            }
            boundingBox = box;
        }

        public bool IsInside(PointF point) {
            bool inside = mainPath.IsInside(point);
            if (holes != null) {
                foreach (SymPath hole in holes) {
                    if (hole.IsInside(point))
                        inside = !inside;
                }
            }

            return inside;
        }

        public void Draw(IGraphicsTarget g, object penKey) {
            MainPath.Draw(g, penKey);
            if (Holes != null) {
                foreach (SymPath hole in Holes)
                    hole.Draw(g, penKey);
            }
        }

        public void DrawTransformed(IGraphicsTarget g, object penKey, Matrix matrix)
        {
            MainPath.DrawTransformed(g, penKey, matrix);
            if (Holes != null) {
                foreach (SymPath hole in Holes)
                    hole.DrawTransformed(g, penKey, matrix);
            }
        }

        public void Fill(IGraphicsTarget g, object brushKey) {
            MainPath.FillWithHoles(g, brushKey, Holes);
        }

        public void FillTransformed(IGraphicsTarget g, object brushKey, Matrix matrix) {
            MainPath.FillTransformedWithHoles(g, brushKey, matrix, Holes);
        }

        public object GetPathKey(IGraphicsTarget g)
        {
            return MainPath.GetPathKeyWithHoles(g, this, Holes);
        }

        public SymPathWithHoles Transform(Matrix mat) {
            if (mat.IsIdentity)
                return this;

            SymPath newMainPath = MainPath.Transform(mat);

            SymPath[] newHoles;
            if (Holes == null)
                newHoles = null;
            else {
                newHoles = (SymPath[]) Holes.Clone();
                for (int i = 0; i < newHoles.Length; ++i)
                    newHoles[i] = newHoles[i].Transform(mat);
            }

            return new SymPathWithHoles(newMainPath, newHoles);
        }

        // Always returns a copy, even if no holes are present. Can be modified without risk.
        public PointF[] GetPoints() {
            if (holes == null) {
                return (PointF[]) mainPath.Points.Clone();
            }
            else {
                int length = mainPath.Points.Length;
                foreach (SymPath hole in holes)
                    length += hole.Points.Length;

                PointF[] points = new PointF[length];
                Array.Copy(mainPath.Points, 0, points, 0, mainPath.Points.Length);
                int index = mainPath.Points.Length;

                foreach (SymPath hole in holes) {
                    Array.Copy(hole.Points, 0, points, index, hole.Points.Length);
                    index += hole.Points.Length;
                }

                return points;
            }
        }

        // Always returns a copy, even if no holes are present. Can be modified without risk.
        public PointKind[] GetPointKinds() {
            if (holes == null) {
                return (PointKind[]) mainPath.PointKinds.Clone();
            }
            else {
                int length = mainPath.PointKinds.Length;
                foreach (SymPath hole in holes)
                    length += hole.PointKinds.Length;

                PointKind[] points = new PointKind[length];
                Array.Copy(mainPath.PointKinds, 0, points, 0, mainPath.PointKinds.Length);
                int index = mainPath.PointKinds.Length;

                foreach (SymPath hole in holes) {
                    Array.Copy(hole.PointKinds, 0, points, index, hole.PointKinds.Length);
                    index += hole.PointKinds.Length;
                }

                return points;
            }
        }

        // Calculate area, by using the main minus the holes. If the holes are not fully contained in the 
        // main area, the result is incorrect.
        public float Area() {
            float area = mainPath.Area();

            if (holes != null) {
                foreach (SymPath hole in holes) {
                    area -= hole.Area();
                }
            }

            if (area < 0)
                area = 0;

            return area;
        }

        public float FindMaxDistance(PointF point)
        {
            double maxDist = mainPath.FindMaxDistance(point);

            if (holes != null) {
                foreach (SymPath hole in holes) {
                    double dist = hole.FindMaxDistance(point);
                    if (dist > maxDist)
                        maxDist = dist;
                }
            }

            return (float) maxDist;
        }

        // override object.Equals to test two paths for equality.
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }

            SymPathWithHoles other = (SymPathWithHoles) obj;

            if (! mainPath.Equals(other.mainPath))
                return false;

            if ((other.holes == null && holes != null) || (other.holes != null && holes == null))
                return false;

            if (holes != null) {
                if (other.holes.Length != holes.Length)
                    return false;
                for (int i = 0; i < holes.Length; ++i)
                    if (!holes[i].Equals(other.holes[i]))
                        return false;
            }

            return true;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            throw new NotSupportedException("GetHashCode not supported.");
        }

        // Convert to a string showing the main sympath and its holes.
        public override string ToString()
        {
            // Start with the main path.
            string result = mainPath.ToString();

            // Add in the holes, if any.
            if (holes != null) {
                for (int i = 0; i < holes.Length; i++) {
                    result += string.Format("  HOLE {0}: {1}", i, holes[i]);
                }
            }

            return result;
        }
    }
}
