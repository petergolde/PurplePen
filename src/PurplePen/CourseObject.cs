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
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using PurplePen.MapModel;
using PurplePen.MapView;
using PurplePen.Graphics2D;
using System.Runtime.InteropServices;

namespace PurplePen
{
    // A CourseObj defines a single object on the rendered course.
    abstract class CourseObj: IMapViewerHighlight, ICloneable
    {
        // NOTE: if you add new fields, update the Equals override!
        public CourseLayer layer;                            // layer in the map
                                                            // The layer number is set when the objects are added to a course layout.

        public Id<ControlPoint> controlId;                        // Id of associated control (control/start/finish/crossing)
        public Id<CourseControl> courseControlId;             // Id of associated course control (control/start/finish/crossing)
        public Id<Special> specialId;                                // Id of special (water/dangerous/etc)
        public float scaleRatio;                   // scale to display in (1.0 is normal scale).
        public CourseAppearance appearance;       // customize course appearance

        static Brush highlightBrush;             // brush used to draw highlights.

        public const int HANDLESIZE = 5;          // side of a square handle (should be odd).

        protected CourseObj(Id<ControlPoint> controlId, Id<CourseControl> courseControlId, Id<Special> specialId, float scaleRatio, CourseAppearance appearance)
        {
            this.controlId = controlId;
            this.courseControlId = courseControlId;
            this.specialId = specialId;
            this.scaleRatio = scaleRatio;
            this.appearance = appearance;
        }

        // Add the given course object to the map, creating a SymDef if needed. The passed dictionary
        // should have the same lifetime as the map and is used to store symdefs.
        public virtual void AddToMap(Map map, SymColor symColor, Dictionary<object, SymDef> dict)
        {
            object key = new Pair<short,object>(symColor.OcadId, SymDefKey());

            if (! dict.ContainsKey(key))
                dict[key] = CreateSymDef(map, symColor);

            AddToMap(map, dict[key]);
        }

        // Scale an array of coords by the scale factor and courseappearance control circle factors.
        protected PointF[] ScaleCoords(PointF[] coords)
        {
            for (int i = 0; i < coords.Length; ++i) {
                coords[i].X *= scaleRatio * appearance.controlCircleSize;
                coords[i].Y *= scaleRatio * appearance.controlCircleSize;
            }

            return coords;
        }

        // Offset an array of coords by an amount.
        protected PointF[] OffsetCoords(PointF[] coords, float dx, float dy)
        {
            for (int i = 0; i < coords.Length; ++i) {
                coords[i].X += dx;
                coords[i].Y += dy;
            }

            return coords;
        }

        // Rotate an array of coords by an angle in degrees.
        protected PointF[] RotateCoords(PointF[] coords, float angle)
        {
            Matrix m = new Matrix();
            m.Rotate(angle);
            m.TransformPoints(coords);
            return coords;
        }


        // Transform X-distance via a transform. 
        protected float TransformDistance(float distance, Matrix xform)
        {
            PointF[] vectors = { new PointF(distance, 0) };
            xform.TransformVectors(vectors);
            return (float) Math.Sqrt(vectors[0].X * vectors[0].X + vectors[0].Y * vectors[0].Y);
        }

        // Overrides...

        // Get a key that corresponding 1-1 with needed symdefs
        protected virtual object SymDefKey()
        {
            return this.GetType();
        }

        // Create the SymDef for this symbol kind. Only called once for each "key"
        protected abstract SymDef CreateSymDef(Map map, SymColor symColor);

        // If returns non-null, indicates the color of the object. Null means use default for the layer it is in.
        public virtual SpecialColor CustomColor
        {
            get { return null; }
        }

        // Add a symbol to the map.
        protected abstract void AddToMap(Map map, SymDef symdef);

        // Determine the distance of this object from the given point, or 0 if the object overlaps the point.
        public abstract double DistanceFromPoint(PointF pt);

        // Draw or erase the highlight, given a brush.
        public abstract void Highlight(Graphics g, Matrix xformWorldToPixel, Brush brush, bool erasing);

        // Offset this course object by the given amount.
        public abstract void Offset(float dx, float dy); 

        // Move a handle on the object.
        public virtual void MoveHandle(PointF oldHandle, PointF newHandle)
        {
        }

        // Get the bounds of the highlight.
        public abstract RectangleF GetHighlightBounds();

        // Get the set of handles that should be drawn with the objects.
        public virtual PointF[] GetHandles()
        {
            return null;
        }

        // Get the cursor that should be used for a given handle.
        public virtual Cursor GetHandleCursor(PointF handlePoint)
        {
            return Util.MoveHandleCursor;
        }

        // Draw a highlight for this course object.    
        public void DrawHighlight(Graphics g, Matrix xformWorldToPixel)
        {
            if (highlightBrush == null) {
                // Using a SolidBrush causes slight differences in drawing single pixel
                // wide lines. This must be due to some optimizations in GDI+. So we fake it by using
                // a single pixel texture brush.
                Bitmap bm = new Bitmap(1, 1);
                bm.SetPixel(0, 0, NormalCourseAppearance.highlightColor);
                highlightBrush = new TextureBrush(bm);
            }
            Highlight(g, xformWorldToPixel, highlightBrush, false);

            // Draw any handles we have.
            PointF[] handles = GetHandles();
            if (handles != null) {
                foreach (PointF handleLocation in handles)
                    DrawHandle(handleLocation, g, xformWorldToPixel);
            }
        }

        // Erase a highlight for this course object.
        public void EraseHighlight(Graphics g, Matrix xformWorldToPixel, Brush eraseBrush)
        {
            Highlight(g, xformWorldToPixel, eraseBrush, true);

            // Erase any handles we have.
            PointF[] handles = GetHandles();
            if (handles != null) {
                foreach (PointF handleLocation in handles)
                    EraseHandle(handleLocation, g, xformWorldToPixel, eraseBrush);
            }
        }

        // Draw a handle at a given location.
        private void DrawHandle(PointF handleLocation, Graphics g, Matrix xformWorldToPixel)
        {
            const int HIGHLIGHTSIZE = 5;
            Point pixelLocation = Point.Round(Geometry.TransformPoint(handleLocation, xformWorldToPixel));

            Rectangle rect = new Rectangle(pixelLocation.X - (HIGHLIGHTSIZE - 1) / 2, pixelLocation.Y - (HIGHLIGHTSIZE - 1) / 2, HIGHLIGHTSIZE, HIGHLIGHTSIZE);
            g.FillRectangle(Brushes.Blue, rect);
        }

        // Erase a handle at a given location.
        private void EraseHandle(PointF handleLocation, Graphics g, Matrix xformWorldToPixel, Brush eraseBrush)
        {
            Point pixelLocation = Point.Round(Geometry.TransformPoint(handleLocation, xformWorldToPixel));

            Rectangle rect = new Rectangle(pixelLocation.X - (HANDLESIZE - 1) / 2, pixelLocation.Y - (HANDLESIZE - 1) / 2, HANDLESIZE, HANDLESIZE);
            g.FillRectangle(eraseBrush, rect);
        }

        // Get a string with the state of this course object.
        public override string ToString()
        {
            string result = "";

            string typeName = GetType().Name;
            if (typeName.EndsWith("CourseObj", StringComparison.InvariantCulture))
                result += string.Format("{0,-16}", GetType().Name.Substring(0, typeName.Length - "CourseObj".Length) + ":");

            if (layer != 0)
                result += string.Format("layer:{0}  ", (int)layer);
            if (controlId.IsNotNone)
                result += string.Format("control:{0}  ", controlId);
            if (courseControlId.IsNotNone)
                result += string.Format("course-control:{0}  ", courseControlId);
            if (specialId.IsNotNone)
                result += string.Format("special:{0}  ", specialId);
            result += string.Format("scale:{0}  ", scaleRatio);

            return result;
        }


        // override object.Equals
        public override bool Equals(object obj)
        {
            if ((object) obj == (object) this)
                return true;

            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }

            CourseObj other = (CourseObj) obj;
            if (other.layer != layer || other.controlId != controlId || other.courseControlId != courseControlId || 
                other.specialId != specialId || other.scaleRatio != scaleRatio || ! other.appearance.Equals(appearance))
                return false;

            return true;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            throw new NotSupportedException("The method or operation is not supported.");
        }

        public virtual object Clone()
        {
            return base.MemberwiseClone();
        }
}

    // A type of course object that exists at a single point.
    abstract class PointCourseObj: CourseObj
    {
        // NOTE: if new fields are added, update Equals implementation.
        public CircleGap[] gaps;                 // gaps if its a control or finish circle
        public CircleGap[] movableGaps;          // gaps if its a control or finish circle that can be moved via handles
        public float orientation;                // orientation in degrees (start/crossing).
        public PointF location;                  // location of the object
        float radius;                            // radius of the object (for hit-testing) -- unscaled.


        protected PointCourseObj(Id<ControlPoint> controlId, Id<CourseControl> courseControlId, Id<Special> specialId, float scaleRatio, CourseAppearance appearance, CircleGap[] gaps, float orientation, float radius, PointF location) :
           base(controlId, courseControlId, specialId, scaleRatio, appearance)
       {
            this.gaps = gaps;
            this.movableGaps = gaps;
            this.orientation = orientation;
            this.location = location;
            this.radius = radius;
       }

        // Get the full radius of this point object. 
        public float FullRadius
        {
            get { return radius * scaleRatio * appearance.controlCircleSize; }
        }

        // Get the radius that handles are placed on. Compensates for the line width. Used for cutting adjacent circles, and positioning handles
        public float ApparentRadius
        {
            get {return FullRadius - ((appearance.lineWidth * NormalCourseAppearance.lineThickness * scaleRatio) / 2.0F); }
        }

        protected override void AddToMap(Map map, SymDef symdef)
        {
            PointSymbol sym = new PointSymbol((PointSymDef)symdef, location, orientation, CircleGap.StartsAndStops(gaps));
            map.AddSymbol(sym);
        }

        // Get the distance of a point from this object, or 0 if the point is covered by the object.
        public override double DistanceFromPoint(PointF pt)
        {
            double dist = Geometry.Distance(pt, location) - FullRadius;
            return Math.Max(0, dist);
        }

        public override string ToString()
        {
            string result = base.ToString();
            result += string.Format("location:({0},{1})", location.X, location.Y);
            return result;
        }

        // Draw a cross-hair at the location.
        protected void HighlightCrossHair(Graphics g, Matrix xformWorldToPixel, Brush brush)
        {
            // Cross hair is 1.5mm in each direction.
            float crossHairLength = 1.5F * scaleRatio;

            // Get the points of the cross-hair.
            PointF[] pts = { new PointF(location.X - crossHairLength, location.Y), new PointF(location.X + crossHairLength, location.Y),
                                      new PointF(location.X, location.Y - crossHairLength), new PointF(location.X, location.Y + crossHairLength)};
            xformWorldToPixel.TransformPoints(pts);

            // Draw the cross-hair.
            using (Pen pen = new Pen(brush, 0)) {
                g.DrawLine(pen, (float) Math.Round(pts[0].X), (float) Math.Round(pts[0].Y), (float) Math.Round(pts[1].X), (float)Math.Round(pts[1].Y));
                g.DrawLine(pen, (float)Math.Round(pts[2].X), (float)Math.Round(pts[2].Y), (float)Math.Round(pts[3].X), (float)Math.Round(pts[3].Y));
            }
        }

        // Get the bounds of the highlight.
        public override RectangleF GetHighlightBounds()
        {
            return new RectangleF(location.X - FullRadius, location.Y - FullRadius, FullRadius * 2, FullRadius * 2);
        }

        public override PointF[] GetHandles()
        {
            if (gaps == null)
                return null;
            else 
                return CircleGap.GapStartStopPoints(location, ApparentRadius, movableGaps);
        }

        // Move a handle on the line.
        public override void MoveHandle(PointF oldHandle, PointF newHandle)
        {
            movableGaps = CircleGap.MoveStartStopPoint(location, ApparentRadius, movableGaps, oldHandle, newHandle);
            gaps = CircleGap.MoveStartStopPoint(location, ApparentRadius, gaps, oldHandle, newHandle);
        }

        // Offset the object by a given amount
        public override void Offset(float dx, float dy)
        {
            location.X += dx;
            location.Y += dy;
        }

        // Are we equal?
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }

            PointCourseObj other = (PointCourseObj) obj;

            if (other.gaps != gaps || other.orientation != orientation || other.location != location || other.radius != radius)
                return false;

            return base.Equals(obj);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            throw new NotSupportedException("The method or operation is not supported.");
        }
    }

    // A type of course object that is a series of line segments.
    abstract class LineCourseObj: CourseObj
    {
        // NOTE: if new fields are added, update Equals implementation.
        public Id<CourseControl> courseControlId2;            // Id of second associated course control (normal leg/flagged leg)
        public SymPath path;                      // Path of the line   
        public LegGap[] gaps;                     // Gaps (can be null)
        public LegGap[] movableGaps;              // Gaps that can be moved with a handle (can be null)
        float thickness;                               // thickness of the line  (unscaled)        

        protected LineCourseObj(Id<ControlPoint> controlId, Id<CourseControl> courseControlId, Id<CourseControl> courseControlId2, Id<Special> specialId, float scaleRatio, CourseAppearance appearance, float thickness, SymPath path, LegGap[] gaps) :
           base(controlId, courseControlId, specialId, scaleRatio, appearance)
       {
           this.courseControlId2 = courseControlId2;
           this.thickness = thickness;

           this.path = path;
           this.gaps = this.movableGaps = gaps;
       }

        // Should the ends of the line have handles?
        public virtual bool HandlesOnEnds
        {
            get { return true; }
        }

       public override PointF[] GetHandles()
       {
           List<PointF> handleList = new List<PointF>();

           // Add handles for the bends, and possibly the end points.
           handleList.AddRange(path.Points);
           if (! HandlesOnEnds) {
               // Remove handles from the ends.
               handleList.RemoveAt(0);
               handleList.RemoveAt(handleList.Count - 1);
           }

           // Add handles for the gaps. (Only the moveable ones)
           if (movableGaps != null) {
               handleList.AddRange(LegGap.GapStartStopPoints(path, movableGaps));
           }

           // Return the handles as an array.
           if (handleList.Count > 0)
               return handleList.ToArray();
           else
               return null;
       }

       protected override void AddToMap(Map map, SymDef symdef)
       {
            SymPath[] gappedPaths = LegGap.SplitPathWithGaps(path, gaps);

            foreach (SymPath p in gappedPaths) {
                LineSymbol sym = new LineSymbol((LineSymDef) symdef, p);
                map.AddSymbol(sym);
            }
       }

       // Get the distance of a point from this object, or 0 if the point is covered by the object.
       public override double DistanceFromPoint(PointF pt)
       {
           PointF closestPoint;
           double dist = path.DistanceFromPoint(pt, out closestPoint) - (thickness / 2.0 * scaleRatio);
           return Math.Max(0, dist);
       }

       public override string ToString()
       {
           string result = base.ToString();

           if (courseControlId2.IsNotNone)
                result += string.Format("course-control2:{0}  ", courseControlId2);

           result += string.Format("path:{0}", path);

           if (gaps != null) {
               result += "  gaps:";
               foreach (LegGap gap in gaps)
                   result += string.Format(" (s:{0:0.##},l:{1:0.##})", gap.distanceFromStart, gap.length);
           }

           return result;
       }

       // Draw the highlight. Everything must be drawn in pixel coords so fast erase works correctly.
       public override void Highlight(Graphics g, Matrix xformWorldToPixel, Brush brush, bool erasing)
       {
           GDIPlus_GraphicsTarget grTarget = new GDIPlus_GraphicsTarget(g);

           object brushKey = new object();
           grTarget.CreateGdiPlusBrush(brushKey, brush, false);

           // Get thickness of line.
           float pixelThickness = TransformDistance(thickness * scaleRatio, xformWorldToPixel);

           SymPath[] gappedPaths = LegGap.SplitPathWithGaps(path, gaps);

           // Draw it.
           object penKey = new object();
           grTarget.CreatePen(penKey, brushKey, pixelThickness, LineCap.Flat, LineJoin.Miter, 5);

           try {
               foreach (SymPath p in gappedPaths) {
                   p.DrawTransformed(grTarget, penKey, xformWorldToPixel);
               }
           }
           catch (ExternalException) {
               // Ignore this exeption. Not sure what causes it.
           }

           grTarget.Dispose();
       }

       // Get the bounds of the highlight.
        public override RectangleF GetHighlightBounds()
        {
            return path.BoundingBox;
        }

       // Offset the object by a given amount
       public override void Offset(float dx, float dy)
       {
           Matrix m = new Matrix();
           m.Translate(dx, dy);
           path = path.Transform(m);
       }

       // Move a handle on the line.
       public override void MoveHandle(PointF oldHandle, PointF newHandle)
       {
           SymPath oldPath = path;
           PointF[] points = (PointF[]) path.Points.Clone();
           PointKind[] kinds = path.PointKinds;
           bool foundPoint = false;

           // Check if handle being moved is an path handle.
           if (HandlesOnEnds) {
               for (int i = 0; i < points.Length; ++i) {
                   if (!foundPoint && points[i] == oldHandle) {
                       points[i] = newHandle;
                       foundPoint = true;
                   }
               }
           }
           else {
               for (int i = 1; i < points.Length - 1; ++i) {
                   if (!foundPoint && points[i] == oldHandle) {
                       points[i] = newHandle;
                       foundPoint = true;
                   }
               }
           }

           if (foundPoint) {
               // Create new path.
               path = new SymPath(points, kinds);

               // Update gaps for the new path.
               if (gaps != null)
                   gaps = LegGap.MoveGapsToNewPath(gaps, oldPath, path);
           }
           else {
               // Handle may be on the gaps. Update those.
                if (gaps != null) 
                    gaps = LegGap.MoveStartStopPoint(path, gaps, oldHandle, newHandle);
                if (movableGaps != null)
                    movableGaps = LegGap.MoveStartStopPoint(path, movableGaps, oldHandle, newHandle);
           }
       }


       // Are we equal?
       public override bool Equals(object obj)
       {
           if (obj == null || GetType() != obj.GetType()) {
               return false;
           }

           LineCourseObj other = (LineCourseObj) obj;

           if (other.courseControlId2 != courseControlId2 || other.thickness != thickness || !(path.Equals(other.path)))
               return false;

           if (gaps != null) {
               if (other.gaps == null)
                   return false;
               if (gaps.Length != other.gaps.Length)
                   return false;
               for (int i = 0; i < gaps.Length; ++i)
                   if (other.gaps[i] != gaps[i])
                       return false;
           }
           else {
               if (other.gaps != null)
                   return false;
           }

           return base.Equals(obj);
       }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            throw new NotSupportedException("The method or operation is not supported.");
        }
   }

    // A type of course object that spans an area.
    abstract class AreaCourseObj: CourseObj
    {
        // NOTE: if new fields are added, update Equals implementation.
        SymPathWithHoles path;                // closed path with the area to fill

        protected AreaCourseObj(Id<ControlPoint> controlId, Id<CourseControl> courseControlId, Id<Special> specialId, float scaleRatio, CourseAppearance appearance, PointF[] pts) :
           base(controlId, courseControlId, specialId, scaleRatio, appearance)
       {
            bool lastPtSynthesized = false;

            if (pts[pts.Length - 1] != pts[0]) {
                // If needed, synthesize a final point to close the path.
                PointF[] newPts = new PointF[pts.Length + 1];
                Array.Copy(pts, newPts, pts.Length);
                newPts[pts.Length] = pts[0];
                pts = newPts;
                lastPtSynthesized = true;
            }

           PointKind[] kinds = new PointKind[pts.Length];
           for (int i = 0; i < kinds.Length; ++i)
               kinds[i] = PointKind.Normal;

           this.path = new SymPathWithHoles(new SymPath(pts, kinds, null, lastPtSynthesized), null);
       }

        public override PointF[] GetHandles()
        {
            // First and last point are duplicates, so return all except the last point.
            PointF[] points = path.MainPath.Points;
            PointF[] handles = new PointF[points.Length - 1];
            Array.Copy(points, handles, points.Length - 1);
            return handles;
        }

        protected override void AddToMap(Map map, SymDef symdef)
        {
            AreaSymbol sym = new AreaSymbol((AreaSymDef)symdef, path, 0, new PointF());
            map.AddSymbol(sym);
        }

        // Get the distance of a point from this object, or 0 if the point is covered by the object.
        public override double DistanceFromPoint(PointF pt)
        {
            // Is the point contained inside?
            if (path.IsInside(pt))
                return 0.0;

           // Not inside: use the distance from the path.
           PointF closestPoint;
           return path.MainPath.DistanceFromPoint(pt, out closestPoint);
        }

        public override string ToString()
        {
            string result = base.ToString();
            result += string.Format("path:{0}", path);
            return result;
        }

        // Draw the highlight. Everything must be draw in pixel coords so fast erase works correctly.
        public override void Highlight(Graphics g, Matrix xformWorldToPixel, Brush brush, bool erasing)
        {
            GDIPlus_GraphicsTarget grTarget = new GDIPlus_GraphicsTarget(g);

            object brushKey = new object();
            grTarget.CreateGdiPlusBrush(brushKey, brush, false);

            // Draw the boundary.
            object penKey = new object();
            grTarget.CreatePen(penKey, brushKey, 2, LineCap.Round, LineJoin.Round, 5);
            path.DrawTransformed(grTarget, penKey, xformWorldToPixel);

            // Get a brush to fill the interior with.
            object fillBrushKey;

            if (erasing)
                fillBrushKey = brushKey;
            else {
                fillBrushKey = new object();
                grTarget.CreateGdiPlusBrush(fillBrushKey, NormalCourseAppearance.areaHighlight, false);
            }

            // Draw the interior
            path.FillTransformed(grTarget, fillBrushKey, xformWorldToPixel);

            grTarget.Dispose();
        }

        // Get the bounds of the highlight.
        public override RectangleF GetHighlightBounds()
        {
            return path.BoundingBox;
        }

        // Offset the object by a given amount
        public override void Offset(float dx, float dy)
        {
            Matrix m = new Matrix();
            m.Translate(dx, dy);
            path = path.Transform(m);
        }

        // Move a handle on the area.
        public override void MoveHandle(PointF oldHandle, PointF newHandle)
        {
            PointF[] points = (PointF[]) path.MainPath.Points.Clone();
            PointKind[] kinds = path.MainPath.PointKinds;

            for (int i = 0; i < points.Length; ++i) {
                if (points[i] == oldHandle)
                    points[i] = newHandle;
            }

            path = new SymPathWithHoles(new SymPath(points, kinds), null);
        }

        // Are we equal?
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }

            AreaCourseObj other = (AreaCourseObj) obj;

            if (!(path.Equals(other.path)))
                return false;

            return base.Equals(obj);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            throw new NotSupportedException("The method or operation is not supported.");
        }
    }

    // A type of course object that spans an rectangular area.
    class RectCourseObj: CourseObj
    {
        // NOTE: if new fields are added, update Equals implementation.
        public RectangleF rect;                // rectangle with the area.

        public RectCourseObj(Id<ControlPoint> controlId, Id<CourseControl> courseControlId, Id<Special> specialId, float scaleRatio, CourseAppearance appearance, RectangleF rect)
            :
           base(controlId, courseControlId, specialId, scaleRatio, appearance)
        {
            this.rect = rect;
        }

        public override PointF[] GetHandles()
        {
            // Handles on sides and corners. Handle 0 is at bottom-left (which corresponds to rect.Left,rect.Top, since rect is inverted). Goes counter-clockwise
            // from there.
            float middleWidth = (rect.Left + rect.Right) / 2;
            float middleHeight = (rect.Top + rect.Bottom) / 2;
            PointF[] handles = { new PointF(rect.Left, rect.Top), new PointF(middleWidth, rect.Top), new PointF(rect.Right, rect.Top),
                                            new PointF(rect.Left, middleHeight), new PointF(rect.Right, middleHeight),
                                            new PointF(rect.Left, rect.Bottom), new PointF(middleWidth, rect.Bottom), new PointF(rect.Right, rect.Bottom)};
            return handles;
        }

        public override Cursor GetHandleCursor(PointF handlePoint)
        {
            // Get the correct sizing cursors for each point given above. 
            int index = Array.IndexOf(GetHandles(), handlePoint);

            switch (index) {
            case 0: case 7: return Cursors.SizeNESW;
            case 1: case 6: return Cursors.SizeNS;
            case 2: case 5: return Cursors.SizeNWSE;
            case 3: case 4: return Cursors.SizeWE;
            default: return Util.MoveHandleCursor;
            }
        }

        // Get the distance of a point from this object, or 0 if the point is covered by the object.
        public override double DistanceFromPoint(PointF pt)
        {
            PointF closestPoint;

            // Is the point contained inside?
            if (rect.Contains(pt))
                return 0.0;

            SymPath path = new SymPath(new PointF[5] { new PointF(rect.Left, rect.Top), new PointF(rect.Right, rect.Top),
                                             new PointF(rect.Right, rect.Bottom), new PointF(rect.Left, rect.Bottom), new PointF(rect.Left, rect.Top)});
            return path.DistanceFromPoint(pt, out closestPoint);
        }

        public override string ToString()
        {
            string result = base.ToString();
            result += string.Format("rect:{0}", rect);
            return result;
        }

        protected static void DrawBorderedRectangle(Graphics g, Matrix xformWorldToPixel, RectangleF rectToDraw, Brush brush, bool erasing)
        {
            RectangleF xformedRect = Geometry.TransformRectangle(xformWorldToPixel, rectToDraw);

            // Get a brush to fill the interior with.
            Brush fillBrush;

            if (erasing)
                fillBrush = brush;
            else
                fillBrush = NormalCourseAppearance.areaHighlight;

            // Draw the interior
            g.FillRectangle(fillBrush, xformedRect);

            // Draw the boundary.
            using (Pen pen = new Pen(brush, 2)) {
                g.DrawRectangle(pen, xformedRect.Left, xformedRect.Top, xformedRect.Width, xformedRect.Height);
            }
        }

        // Draw the highlight. Everything must be draw in pixel coords so fast erase works correctly.
        public override void Highlight(Graphics g, Matrix xformWorldToPixel, Brush brush, bool erasing)
        {
            DrawBorderedRectangle(g, xformWorldToPixel, rect, brush, erasing);
        }

        // Get the bounds of the highlight.
        public override RectangleF GetHighlightBounds()
        {
            return rect;
        }

        // Offset the object by a given amount
        public override void Offset(float dx, float dy)
        {
            RectangleF newRect = rect;
            newRect.Offset(dx, dy);

            RectangleUpdating(ref newRect, true, false, false, false, false);

            rect = newRect;
        }

        // Move a handle on the rectangle.
        public override void MoveHandle(PointF oldHandle, PointF newHandle)
        {
            PointF[] handles = GetHandles();
            int handleIndex = Array.IndexOf(handles, oldHandle);

            // Existing coordinates of the rectangle.
            float left = rect.Left, top = rect.Top, right = rect.Right, bottom = rect.Bottom;

            // Figure out which coord(s) moving this handle changes.
            bool changeLeft = false, changeTop = false, changeRight = false, changeBottom = false;
            switch (handleIndex) {
            case 0: changeLeft = true; changeTop = true; break;
            case 1: changeTop = true; break;
            case 2: changeRight = true; changeTop = true; break;
            case 3: changeLeft = true; break;
            case 4: changeRight = true; break;
            case 5: changeLeft = true; changeBottom = true; break;
            case 6: changeBottom = true; break;
            case 7: changeRight = true; changeBottom = true; break;
            default:
                Debug.Fail("bad handle"); break;
            }

            // Update the coordinates based on movement.
            if (changeLeft)         left = newHandle.X;
            if (changeTop)          top = newHandle.Y;
            if (changeRight)       right = newHandle.X;
            if (changeBottom)    bottom = newHandle.Y;

            RectangleF newRect = Geometry.RectFromPoints(left, top, right, bottom);
           
            // Update the rectangle.
            RectangleUpdating(ref newRect, false, changeLeft, changeTop, changeRight, changeBottom);
            rect = newRect;
        }

        // Rectangle is about to be updated by MoveHandle. This method can update the rectangle to something new, if desired.
        // The boolean params indicate how the rectangle changed.
        public virtual void RectangleUpdating(ref RectangleF newRect, bool dragAll, bool dragLeft, bool dragTop, bool dragRight, bool dragBottom)
        {
        }

        // Are we equal?
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }

            RectCourseObj other = (RectCourseObj) obj;

            if (!(rect.Equals(other.rect)))
                return false;

            return base.Equals(obj);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            throw new NotSupportedException("The method or operation is not supported.");
        }

        protected override void AddToMap(Map map, SymDef symdef)
        {
            throw new NotImplementedException("Must be overridden");
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            throw new NotImplementedException("Must be overridden");
        }
    }

    // A rectangle that preserves aspect when resized.
    abstract class AspectPreservingRectCourseObj: RectCourseObj
    {
        protected float aspect;                    // aspect to maintain: width / height

        public AspectPreservingRectCourseObj(Id<ControlPoint> controlId, Id<CourseControl> courseControlId, Id<Special> specialId, float scaleRatio, CourseAppearance appearance, RectangleF rect)
            : base (controlId, courseControlId, specialId, scaleRatio, appearance, rect)
        {
            if (rect.Height != 0)
                aspect = rect.Width / rect.Height;
            else
                aspect = 1;
        }

        // Adjust the new rectangle to preserve the aspect ratio, depending on how the rectangle was modified.
        public override void RectangleUpdating(ref RectangleF newRect, bool dragAll, bool dragLeft, bool dragTop, bool dragRight, bool dragBottom)
        {
            float left = newRect.Left, right = newRect.Right, top = newRect.Top, bottom = newRect.Bottom;
            bool aspectAdjustWidth = false, aspectAdjustHeight = false;

            if (!dragAll) {
                if (!dragTop && !dragBottom)
                    aspectAdjustHeight = true;
                else if (!dragLeft && !dragRight)
                    aspectAdjustWidth = true;

                // Update the coordinates to preserve aspect.
                float newAspect = (bottom != top) ? Math.Abs(right - left) / Math.Abs(bottom - top) : 1;
                if (!aspectAdjustWidth && !aspectAdjustHeight) {
                    // Determine if width or height aspect should be adjusted.
                    if (newAspect < aspect)
                        aspectAdjustWidth = true;
                    else if (newAspect > aspect)
                        aspectAdjustHeight = true;
                }

                if (aspectAdjustHeight && aspect != 0) {
                    // Adjust the height to match the width.
                    float newHeight = Math.Abs(right - left) / aspect;
                    if (dragBottom) {
                        if (bottom > top)
                            bottom = top + newHeight;
                        else
                            bottom = top - newHeight;
                    }
                    else {
                        if (top < bottom)
                            top = bottom - newHeight;
                        else
                            top = bottom + newHeight;
                    }
                }
                else if (aspectAdjustWidth) {
                    // Adjust the width to match the height
                    float newWidth = Math.Abs(bottom - top) * aspect;
                    if (dragLeft) {
                        if (left < right)
                            left = right - newWidth;
                        else
                            left = right + newWidth;
                    }
                    else {
                        if (right > left)
                            right = left + newWidth;
                        else
                            right = left - newWidth;
                    }
                }

                newRect = Geometry.RectFromPoints(left, top, right, bottom);
            }
        }
    }



    // A type of course object that is text.
    abstract class TextCourseObj: CourseObj
    {
        // NOTE: if new fields are added, update Equals implementation.
        public string text;                             // text for a Text object
        public PointF topLeft;                      // top-left of the text.
        public string fontName;                  // font name
        public FontStyle fontStyle;              // font style
        public SpecialColor fontColor;           // font color
        private float emHeight;                     // em height of the font.
        private float outlineWidth;                 // width of white outline (0 for none)

        protected SizeF size;                       // size of the text.

        // NOTE: scale ratio is not used for this type of object!
        public TextCourseObj(Id<ControlPoint> controlId, Id<CourseControl> courseControlId, Id<Special> specialId, string text, PointF topLeft, string fontName, FontStyle fontStyle, SpecialColor fontColor, float emHeight, float outlineWidth)
            :
           base(controlId, courseControlId, specialId, 1.0F, new CourseAppearance())
       {
            this.text = text;
            this.topLeft = topLeft;
            this.fontName = fontName;
            this.fontStyle = fontStyle;
            this.fontColor = fontColor;
            this.emHeight = emHeight;
            this.outlineWidth = outlineWidth;
            this.size = MeasureText();
       }

        public float EmHeight
        {
            get { return emHeight; }
            set
            {
                emHeight = value;
                this.size = MeasureText();
            }
        }

        // Get the name for the text symdef created.
        protected abstract string SymDefName {get;} 

        // Get the ID for the text symdef created.
        protected abstract int OcadIdIntegerPart { get;}

        // A struct synthesizes Equals/GetHashCode automatically.
        // CONSIDER: use FontDesc instead!
        struct MySymdefKey
        {
            public string fontName;
            public FontStyle fontStyle;
            public SpecialColor fontColor;
            public float emHeight;
            public float outineWidth;
        }

        protected override object SymDefKey()
        {
            MySymdefKey key = new MySymdefKey();
            key.fontName = fontName;
            key.fontStyle = fontStyle;
            key.fontColor = fontColor;
            key.emHeight = emHeight;
            key.outineWidth = outlineWidth;

            return key;
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            throw new NotImplementedException("Should not be called.");
        }

        protected virtual SymDef CreateSymDef(Map map, SymColor symColor, SymColor whiteColor)
        {
            // Find a free id.
            string symbolId = map.GetFreeSymbolId(OcadIdIntegerPart);

            TextSymDef symdef = new TextSymDef(SymDefName, symbolId, TextSymDef.PreferredSymbolKind.NormalText, null);
            symdef.SetFont(fontName, emHeight, Util.GetTextEffects(fontStyle), symColor, emHeight, 0, 0, 0, null, 0, 1F, TextSymDefHorizAlignment.Left, TextSymDefVertAlignment.TopAscent);
            if (outlineWidth > 0) {
                TextSymDef.Framing framing = new TextSymDef.Framing() {
                    framingColor = whiteColor,
                    framingStyle = TextSymDef.FramingStyle.Line,
                    lineStyle = LineStyle.Rounded,
                    lineWidth = outlineWidth
                };
                symdef.SetFraming(framing);
            }

            symdef.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.Number_OcadToolbox);
            map.AddSymdef(symdef);
            return symdef;
        }

        public override void AddToMap(Map map, SymColor symColor, Dictionary<object, SymDef> dict)
        {
            object key = new Pair<short, object>(symColor.OcadId, SymDefKey());

            if (!dict.ContainsKey(key)) {
                SymColor whiteColor = ((AreaSymDef)dict[CourseLayout.KeyWhiteOut]).FillColor;
                dict[key] = CreateSymDef(map, symColor, whiteColor);
            }

            AddToMap(map, dict[key]);
        }

        public override SpecialColor CustomColor
        {
            get
            {
                return fontColor;
            }
        }

       protected override void AddToMap(Map map, SymDef symdef)
       {
           TextSymbol sym = new TextSymbol((TextSymDef) symdef, new string[1] { text }, topLeft, 0, 0, TextSymDefHorizAlignment.Default, TextSymDefVertAlignment.Default);

           /*Show size of text
            * PointF[] pts = { topLeft, new PointF(topLeft.X, topLeft.Y - size.Height), new PointF(topLeft.X + size.Width, topLeft.Y - size.Height), new PointF(topLeft.X + size.Width, topLeft.Y), topLeft };
           PointKind[] kinds = { PointKind.Normal, PointKind.Normal, PointKind.Normal, PointKind.Normal, PointKind.Normal };
           SymPathWithHoles path = new SymPathWithHoles(new SymPath(pts, kinds), null);
           AreaSymbol sym = new AreaSymbol((AreaSymDef) symdef, path, 0); */

           map.AddSymbol(sym);
       }

        public override double DistanceFromPoint(PointF pt)
        {
            // Is point within the rectangle?
            RectangleF rect = new RectangleF(new PointF(topLeft.X, topLeft.Y - size.Height), size);
            if (rect.Contains(pt))
                return 0;

            // Return distance to the border of the rectangle.
            PointF closestPoint;
            SymPath path = new SymPath(new PointF[] {new PointF(rect.Left, rect.Top), new PointF(rect.Left, rect.Bottom), new PointF(rect.Right, rect.Bottom), new PointF(rect.Right, rect.Top), new PointF(rect.Left, rect.Top) },
                new PointKind[] { PointKind.Normal, PointKind.Normal, PointKind.Normal, PointKind.Normal, PointKind.Normal} );
            return path.DistanceFromPoint(pt, out closestPoint);
        }

        // Measure the text.
        private SizeF MeasureText()
        {
            if (emHeight == 0)
                return new SizeF(0, 0);

            Graphics g = Util.GetHiresGraphics();
            using (Font f = new Font(fontName, emHeight, fontStyle, GraphicsUnit.World))
                return g.MeasureString(text, f, topLeft, StringFormat.GenericTypographic);
        }

        public override string ToString()
        {
            string result = base.ToString();
            result += string.Format("text:{0}  top-left:({1:0.##},{2:0.##})\r\n                font-name:{3}  font-style:{4}  font-height:{5}", text, topLeft.X, topLeft.Y, fontName, fontStyle, emHeight);
            return result;
        }

        // Draw the highlight. Everything must be draw in pixel coords so fast erase works correctly.
        public override void Highlight(Graphics g, Matrix xformWorldToPixel, Brush brush, bool erasing)
        {
            // Get height of the text.
            float pixelEmHight = TransformDistance(emHeight, xformWorldToPixel);

            // Get top-left corner of text.
            PointF[] topLeftPixel = { topLeft };
            xformWorldToPixel.TransformPoints(topLeftPixel);

            // Draw it.
            using (FontFamily fontFam = new FontFamily(fontName)) {
                StringFormat format = new StringFormat(StringFormat.GenericTypographic);
                format.Alignment = StringAlignment.Near;
                format.LineAlignment = StringAlignment.Near;
                format.FormatFlags |= StringFormatFlags.NoClip;
                GraphicsPath path = new GraphicsPath();
                path.AddString(text, fontFam, (int)fontStyle, pixelEmHight, topLeftPixel[0], format);
                path.CloseAllFigures();
                g.FillPath(brush, path);
                path.Dispose();

                // The above is similar to this, but produces results slightly more like the anti-aliased text.
                //using (Font font = new Font(fontFam, pixelEmHight))
                    //g.DrawString(text, font, brush, topLeftPixel[0], format);
            }
        }

        // Get the bounds of the highlight
        public override RectangleF GetHighlightBounds()
        {
            // CONSIDER: this is sometimes a little bit too small.
            return new RectangleF(topLeft.X, topLeft.Y - size.Height, size.Width, size.Height);
        }

        // Offset the object by a given amount
        public override void Offset(float dx, float dy)
        {
            topLeft.X += dx;
            topLeft.Y += dy;
        }

        // Are we equal?
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }

            TextCourseObj other = (TextCourseObj) obj;

            if (text != other.text || topLeft != other.topLeft || fontName != other.fontName || fontStyle != other.fontStyle || !fontColor.Equals(other.fontColor) || emHeight != other.emHeight)
                return false;

            return base.Equals(obj);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            throw new NotSupportedException("The method or operation is not supported.");
        }
    }

    // A control circle
    class ControlCourseObj : PointCourseObj
    {
        public const float diameter = NormalCourseAppearance.controlOutsideDiameter;

        public ControlCourseObj(Id<ControlPoint> controlId, Id<CourseControl> courseControlId, float scaleRatio, CourseAppearance appearance, CircleGap[] gaps, PointF location)
            : base(controlId, courseControlId, Id<Special>.None, scaleRatio, appearance, gaps, 0, 3.0F, location)
        {
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            Glyph glyph = new Glyph();
            glyph.AddCircle(symColor, new PointF(0.0F, 0.0F), NormalCourseAppearance.lineThickness * scaleRatio * appearance.lineWidth, diameter * scaleRatio * appearance.controlCircleSize);
            if (appearance.centerDotDiameter > 0.0F) {
                glyph.AddFilledCircle(symColor, new PointF(0.0F, 0.0F), appearance.centerDotDiameter * scaleRatio);
            }
            glyph.ConstructionComplete();

            PointSymDef symdef = new PointSymDef("Control point", "702", glyph, false);
            symdef.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.Control_OcadToolbox);
            map.AddSymdef(symdef);
            return symdef;
        }

        public override string ToString()
        {
            string result = base.ToString();
            result += string.Format("  gaps:{0}", CircleGap.EncodeGaps(gaps));
            return result;
        }

        // Draw the highlight. Everything must be drawn in pixel coords so fast erase works correctly.
        public override void Highlight(Graphics g, Matrix xformWorldToPixel, Brush brush, bool erasing)
        {
            // Transform the thickness to pixel coords.
            float thickness = TransformDistance(NormalCourseAppearance.lineThickness * appearance.lineWidth * scaleRatio, xformWorldToPixel);

            // Transform the ellipse to pixel coords. Points array is 0=location, 1=upper-left corner, 2 = lower-right corner
            float radius = ((diameter * appearance.controlCircleSize - NormalCourseAppearance.lineThickness * appearance.lineWidth) * scaleRatio) / 2F;
            PointF[] pts = { location, new PointF(location.X - radius, location.Y - radius), new PointF(location.X + radius, location.Y + radius) };
            xformWorldToPixel.TransformPoints(pts);

            // Draw the control circle.
            using (Pen pen = new Pen(brush, thickness)) {
                RectangleF rect = RectangleF.FromLTRB(pts[1].X, pts[2].Y, pts[2].X, pts[1].Y);
                CircleGap[] gapsToDraw = CircleGap.SimplifyGaps(gaps);

                try {
                    if (gapsToDraw == null)
                        g.DrawEllipse(pen, rect);
                    else {
                        float[] arcStartSweeps = CircleGap.ArcStartSweeps(gapsToDraw);
                        for (int i = 0; i < arcStartSweeps.Length; i += 2) {
                            float startArc = arcStartSweeps[i];
                            float sweepArc = arcStartSweeps[i + 1];
                            g.DrawArc(pen, rect, startArc, sweepArc);
                        }
                    }

                    // No center dot for highlighting (crosshair instead)
                }
                catch (ExternalException) {
                    // Ignore this exeption. Not sure what causes it.
                }
                catch (OutOfMemoryException) {
                    // Similar.
                }
            }

            // Draw the cross-hair.
            HighlightCrossHair(g, xformWorldToPixel, brush);
        }
    }

    // Start triangle
    class StartCourseObj : PointCourseObj
    {
        // Coordinates of the triangle.
        static readonly PointF[] coords = { new PointF(0F, 4.041F), new PointF(3.5F, -2.021F), new PointF(-3.5F, -2.021F), new PointF(0F, 4.041F) };

        public StartCourseObj(Id<ControlPoint> controlId, Id<CourseControl> courseControlId, float scaleRatio, CourseAppearance appearance, float orientation, PointF location)
            : base(controlId, courseControlId, Id<Special>.None, scaleRatio, appearance, null, orientation, 4.041F, location)
        {
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            PointKind[] kinds = { PointKind.Normal, PointKind.Normal, PointKind.Normal, PointKind.Normal };
            PointF[] pts = ScaleCoords((PointF[]) coords.Clone());
            SymPath path = new SymPath(pts, kinds);

            Glyph glyph = new Glyph();
            glyph.AddLine(symColor, path, NormalCourseAppearance.lineThickness * scaleRatio * appearance.lineWidth, LineJoin.Miter, LineCap.Flat);
            glyph.ConstructionComplete();

            PointSymDef symdef = new PointSymDef("Start", "701", glyph, true);
            symdef.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.Start_OcadToolbox);
            map.AddSymdef(symdef);
            return symdef;
        }

        public override string ToString()
        {
            string result = base.ToString();
            result += string.Format("  orientation:{0:0.##}", orientation);
            return result;
        }

        // Draw the highlight. Everything must be draw in pixel coords so fast erase works correctly.
        public override void Highlight(Graphics g, Matrix xformWorldToPixel, Brush brush, bool erasing)
        {
            // Transform the thickness to pixel coords.
            float thickness = TransformDistance(NormalCourseAppearance.lineThickness * scaleRatio * appearance.lineWidth, xformWorldToPixel);

            // Get coordinates of the triangle and transform to pixel coords.
            PointF[] pts = OffsetCoords(ScaleCoords(RotateCoords((PointF[]) coords.Clone(), orientation)), location.X, location.Y);
            xformWorldToPixel.TransformPoints(pts);

            // Draw the triangle.
            using (Pen pen = new Pen(brush, thickness)) {
                g.DrawPolygon(pen, pts);
            }

            // Draw the cross-hair.
            HighlightCrossHair(g, xformWorldToPixel, brush);
        }
    }

    // Finish circle
    class FinishCourseObj : PointCourseObj
    {
        public FinishCourseObj(Id<ControlPoint> controlId, Id<CourseControl> courseControlId, float scaleRatio, CourseAppearance appearance, CircleGap[] gaps, PointF location)
            : base(controlId, courseControlId, Id<Special>.None, scaleRatio, appearance, gaps, 0, 3.5F, location)
        {
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            Glyph glyph = new Glyph();
            glyph.AddCircle(symColor, new PointF(0.0F, 0.0F), NormalCourseAppearance.lineThickness * scaleRatio * appearance.lineWidth, (5.35F * scaleRatio * appearance.controlCircleSize - NormalCourseAppearance.lineThickness * scaleRatio * appearance.lineWidth));
            glyph.AddCircle(symColor, new PointF(0.0F, 0.0F), NormalCourseAppearance.lineThickness * scaleRatio * appearance.lineWidth, 7.0F * scaleRatio * appearance.controlCircleSize);
            glyph.ConstructionComplete();

            PointSymDef symdef = new PointSymDef("Finish", "706", glyph, false);
            symdef.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.Finish_OcadToolbox);
            map.AddSymdef(symdef);
            return symdef;
        }

        public override string ToString()
        {
            string result = base.ToString();
            result += string.Format("  gaps:{0}", CircleGap.EncodeGaps(gaps));
            return result;
        }

        // Draw the highlight. Everything must be draw in pixel coords so fast erase works correctly.
        public override void Highlight(Graphics g, Matrix xformWorldToPixel, Brush brush, bool erasing)
        {
            // Transform the thickness to pixel coords.
            float thickness = TransformDistance(NormalCourseAppearance.lineThickness * scaleRatio * appearance.lineWidth, xformWorldToPixel);

            // Transform the ellipse to pixel coords. Points array is 0=location, 1=upper-left corner inner, 2 = lower-right corner inner, 3 = upper-left outer, 4=lower-right outer
            float radiusOuter = ((7.0F * appearance.controlCircleSize - NormalCourseAppearance.lineThickness * appearance.lineWidth) * scaleRatio) / 2F;
            float radiusInner = ((5.35F * appearance.controlCircleSize - 2 * NormalCourseAppearance.lineThickness * appearance.lineWidth) * scaleRatio) / 2F;
            PointF[] pts = { location, new PointF(location.X - radiusInner, location.Y - radiusInner), new PointF(location.X + radiusInner, location.Y + radiusInner),
                                                                         new PointF(location.X - radiusOuter, location.Y - radiusOuter), new PointF(location.X + radiusOuter, location.Y + radiusOuter)};
            xformWorldToPixel.TransformPoints(pts);

            // Draw the inner and outer circle.
            using (Pen pen = new Pen(brush, thickness)) {
                RectangleF rect1 = RectangleF.FromLTRB(pts[1].X, pts[2].Y, pts[2].X, pts[1].Y);
                RectangleF rect2 = RectangleF.FromLTRB(pts[3].X, pts[4].Y, pts[4].X, pts[3].Y);

                try {
                    if (gaps == null) {
                        g.DrawEllipse(pen, rect1);
                        g.DrawEllipse(pen, rect2);
                    }
                    else {
                        float[] arcStartSweeps = CircleGap.ArcStartSweeps(gaps);
                        for (int i = 0; i < arcStartSweeps.Length; i += 2) {
                            float startArc = arcStartSweeps[i];
                            float sweepArc = arcStartSweeps[i + 1];
                            g.DrawArc(pen, rect1, startArc, sweepArc);
                            g.DrawArc(pen, rect2, startArc, sweepArc);
                        }
                    }
                }
                catch (ExternalException) {
                    // Ignore this exeption. Not sure what causes it.
                }
                catch (OutOfMemoryException) {
                    // Similar.
                }

            }

            // Draw the cross-hair.
            HighlightCrossHair(g, xformWorldToPixel, brush);
        }
   }

    // A first aid point
    class FirstAidCourseObj : PointCourseObj
    {
        // outline of the first aid symbol.
        static readonly PointF[] outlineCoords = { 
                new PointF(-0.5F, 1.5F), new PointF(0.5F, 1.5F), new PointF(0.5F,  0.5F), new PointF(1.5F, 0.5F), 
                new PointF(1.5F, -0.5F), new PointF(0.5F, -0.5F), new PointF(0.5F,  -1.5F), new PointF(-0.5F, -1.5F), 
                new PointF(-0.5F, -0.5F), new PointF(-1.5F, -0.5F), new PointF(-1.5F,  0.5F), new PointF(-0.5F, 0.5F), new PointF(-0.5F, 1.5F) 
            };

        public FirstAidCourseObj(Id<Special> specialId, float scaleRatio, CourseAppearance appearance, PointF location)
            : base(Id<ControlPoint>.None, Id<CourseControl>.None, specialId, scaleRatio, appearance, null, 0, 1.5F, location)
        {
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            PointKind[] kinds = { 
                PointKind.Normal, PointKind.Normal, PointKind.Normal, PointKind.Normal,
                PointKind.Normal, PointKind.Normal, PointKind.Normal, PointKind.Normal,
                PointKind.Normal, PointKind.Normal, PointKind.Normal, PointKind.Normal, PointKind.Normal
            };
            PointF[] coords = ScaleCoords((PointF[]) outlineCoords.Clone());
            SymPath path = new SymPath(coords, kinds);

            Glyph glyph = new Glyph();
            glyph.AddArea(symColor, new SymPathWithHoles(path, null));
            glyph.ConstructionComplete();

            PointSymDef symdef = new PointSymDef("First aid post", "712", glyph, false);
            symdef.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.FirstAid_OcadToolbox);
            map.AddSymdef(symdef);
            return symdef;
        }


        // Draw the highlight. Everything must be draw in pixel coords so fast erase works correctly.
        public override void Highlight(Graphics g, Matrix xformWorldToPixel, Brush brush, bool erasing)
        {
            // Get the world coordinates of the object.
            PointF[] coords = OffsetCoords(ScaleCoords((PointF[]) outlineCoords.Clone()), location.X, location.Y);

            // Transform to pixel coordinates.
            xformWorldToPixel.TransformPoints(coords);

            // Draw the object.
            g.FillPolygon(brush, coords);
        }

    }

    // A water point
    class WaterCourseObj : PointCourseObj
    {
        PointKind[] kinds1 = { 
                PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl, 
                PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl, 
                PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl, 
                PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl, PointKind.Normal
            };
        PointF[] coords1 =  { 
                new PointF(1.5F, 1.375F), new PointF(1.5F, 1.5825F), new PointF(0.8275F, 1.75F), 
                new PointF(0F, 1.75F), new PointF(-0.8275F, 1.75F), new PointF(-1.5F, 1.5825F), 
                new PointF(-1.5F, 1.375F), new PointF(-1.5F, 1.1675F), new PointF(-0.8275F, 1.0F), 
                new PointF(0F, 1.0F), new PointF(0.8275F, 1.0F), new PointF(1.5F, 1.1675F), new PointF(1.5F, 1.375F) 
            };
        PointKind[]  kinds2 =  { 
                PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl, 
                PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl, PointKind.Normal
            };
        PointF[] coords2 =  { 
                new PointF(1.0F, -1.5F), new PointF(1.0F, -1.6375F), new PointF(0.551F, -1.75F), 
                new PointF(0F, -1.75F), new PointF(-0.551F, -1.75F), new PointF(-1.0F, -1.6375F), new PointF(-1.0F, -1.5F) 
            };
        PointKind[] kinds3 =  { 
                PointKind.Normal, PointKind.Normal, 
            };
        PointF[] coords3 =  { 
                new PointF(1.5F, 1.375F), new PointF(1.0F, -1.5F),
            };
        PointKind[] kinds4 =  { 
                PointKind.Normal, PointKind.Normal, 
            };
        PointF[] coords4 =  { 
                new PointF(-1.5F, 1.375F), new PointF(-1.0F, -1.5F),
            };

        public WaterCourseObj(Id<Special> specialId, float scaleRatio, CourseAppearance appearance, PointF location)
            : base(Id<ControlPoint>.None, Id<CourseControl>.None, specialId, scaleRatio, appearance, null, 0, 2.0F, location)
        {
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            Glyph glyph = new Glyph();

            SymPath path = new SymPath(ScaleCoords((PointF[]) coords1.Clone()), kinds1);
            glyph.AddLine(symColor, path, NormalCourseAppearance.lineThickness * scaleRatio * appearance.lineWidth, LineJoin.Round, LineCap.Round);

            path = new SymPath(ScaleCoords((PointF[]) coords2.Clone()), kinds2);
            glyph.AddLine(symColor, path, NormalCourseAppearance.lineThickness * scaleRatio * appearance.lineWidth, LineJoin.Round, LineCap.Round);

            path = new SymPath(ScaleCoords((PointF[]) coords3.Clone()), kinds3);
            glyph.AddLine(symColor, path, NormalCourseAppearance.lineThickness * scaleRatio * appearance.lineWidth, LineJoin.Round, LineCap.Round);

            path = new SymPath(ScaleCoords((PointF[]) coords4.Clone()), kinds4);
            glyph.AddLine(symColor, path, NormalCourseAppearance.lineThickness * scaleRatio * appearance.lineWidth, LineJoin.Round, LineCap.Round);

            glyph.ConstructionComplete();

            PointSymDef symdef = new PointSymDef("Refreshment point", "713", glyph, false);
            symdef.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.Water_OcadToolbox);
            map.AddSymdef(symdef);
            return symdef;
        }


        public override void Highlight(Graphics g, Matrix xformWorldToPixel, Brush brush, bool erasing)
        {
            SymPath path1, path2, path3, path4;
            float thickness;

            GDIPlus_GraphicsTarget grTarget = new GDIPlus_GraphicsTarget(g);

            object brushKey = new object();
            grTarget.CreateGdiPlusBrush(brushKey, brush, false);

            // Get line thickness.
            thickness = TransformDistance(NormalCourseAppearance.lineThickness * scaleRatio * appearance.lineWidth, xformWorldToPixel);

            // Get the paths.
            path1 = new SymPath(OffsetCoords(ScaleCoords((PointF[]) coords1.Clone()), location.X, location.Y), kinds1);
            path2 = new SymPath(OffsetCoords(ScaleCoords((PointF[]) coords2.Clone()), location.X, location.Y), kinds2);
            path3 = new SymPath(OffsetCoords(ScaleCoords((PointF[]) coords3.Clone()), location.X, location.Y), kinds3);
            path4 = new SymPath(OffsetCoords(ScaleCoords((PointF[]) coords4.Clone()), location.X, location.Y), kinds4);

            object penKey = new object();
            grTarget.CreatePen(penKey, brushKey, thickness, LineCap.Round, LineJoin.Miter, 5);

            // Draw the paths
            path1.DrawTransformed(grTarget, penKey, xformWorldToPixel);
            path2.DrawTransformed(grTarget, penKey, xformWorldToPixel);
            path3.DrawTransformed(grTarget, penKey, xformWorldToPixel);
            path4.DrawTransformed(grTarget, penKey, xformWorldToPixel);

            grTarget.Dispose();
        }

    }

    // A crossing point (could be associated with a control or a special, depending on whether it is mandatory or optional)
    class CrossingCourseObj : PointCourseObj
    {
        static readonly PointF[] coords1 = { new PointF(-0.85F, -1.5F), new PointF(-0.35F, -0.65F), new PointF(-0.35F, 0.65F), new PointF(-0.85F, 1.5F) };
        static readonly PointF[] coords2 = { new PointF(0.85F, -1.5F), new PointF(0.35F, -0.65F), new PointF(0.35F, 0.65F), new PointF(0.85F, 1.5F) };

        public CrossingCourseObj(Id<ControlPoint> controlId, Id<CourseControl> courseControlId, Id<Special> specialId, float scaleRatio, CourseAppearance appearance, float orientation, PointF location)
            : base(controlId, courseControlId, specialId, scaleRatio, appearance, null, orientation, 1.72F, location)
        {
        }

        // Change the orientation of this crossing point.
        public void ChangeOrientation(float newOrientation)
        {
            orientation = newOrientation;
        }

        void GetPaths(out SymPath path1, out SymPath path2)
        {
            PointKind[] kinds = { PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl, PointKind.Normal };
            PointF[] pts = ScaleCoords((PointF[]) coords1.Clone());
            path1 = new SymPath(pts, kinds);

            kinds = new PointKind[4] { PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl, PointKind.Normal };
            pts = ScaleCoords((PointF[]) coords2.Clone());
            path2 = new SymPath(pts, kinds);
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            Glyph glyph = new Glyph();
            SymPath path1, path2;
            
            GetPaths(out path1, out path2);
            glyph.AddLine(symColor, path1, NormalCourseAppearance.lineThickness * scaleRatio * appearance.lineWidth, LineJoin.Miter, LineCap.Flat);
            glyph.AddLine(symColor, path2, NormalCourseAppearance.lineThickness * scaleRatio * appearance.lineWidth, LineJoin.Miter, LineCap.Flat);

            glyph.ConstructionComplete();

            PointSymDef symdef = new PointSymDef("Crossing point", "708", glyph, true);
            symdef.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.Crossing_OcadToolbox);
            map.AddSymdef(symdef);
            return symdef;
        }

        public override void Highlight(Graphics g, Matrix xformWorldToPixel, Brush brush, bool erasing)
        {
            SymPath path1, path2;
            float thickness;

            GDIPlus_GraphicsTarget grTarget = new GDIPlus_GraphicsTarget(g);

            object brushKey = new object();
            grTarget.CreateGdiPlusBrush(brushKey, brush, false);

            // Get line thickness.
            thickness = TransformDistance(NormalCourseAppearance.lineThickness * scaleRatio * appearance.lineWidth, xformWorldToPixel);

            // Get the paths.
            GetPaths(out path1, out path2);

            // Move and rotate the paths to the correct position.
            Matrix moveAndRotate = new Matrix();
            moveAndRotate.Rotate(orientation);
            moveAndRotate.Translate(location.X, location.Y, MatrixOrder.Append);
            path1 = path1.Transform(moveAndRotate);
            path2 = path2.Transform(moveAndRotate);

            object penKey = new object();
            grTarget.CreatePen(penKey, brushKey, thickness, LineCap.Flat, LineJoin.Miter, 5);

            // Draw it.
            path1.DrawTransformed(grTarget, penKey, xformWorldToPixel);
            path2.DrawTransformed(grTarget, penKey, xformWorldToPixel);

            grTarget.Dispose();
        }

        public override string ToString()
        {
            string result = base.ToString();
            result += string.Format("  orientation:{0:0.##}", orientation);
            return result;
        }
    }

    // A registration mark
    class RegMarkCourseObj : PointCourseObj
    {
        const float lineThickness = 0.1F;
        PointKind[] kinds1 = { PointKind.Normal, PointKind.Normal };
        PointF[] coords1 = { new PointF(-2F, 0F), new PointF(2F, 0F) };
        PointKind[] kinds2 =  { PointKind.Normal, PointKind.Normal };
        PointF[] coords2 = { new PointF(0F, -2F), new PointF(0F, 2F) };

        public RegMarkCourseObj(Id<Special> specialId, float scaleRatio, CourseAppearance appearance, PointF location)
            : base(Id<ControlPoint>.None, Id<CourseControl>.None, specialId, scaleRatio, appearance, null, 0, 2.0F, location)
        {
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            Glyph glyph = new Glyph();

            SymPath path = new SymPath(ScaleCoords((PointF[]) coords1.Clone()), kinds1);
            glyph.AddLine(symColor, path, lineThickness * scaleRatio * appearance.lineWidth, LineJoin.Miter, LineCap.Flat);

            path = new SymPath(ScaleCoords((PointF[]) coords2.Clone()), kinds2);
            glyph.AddLine(symColor, path, lineThickness * scaleRatio * appearance.lineWidth, LineJoin.Miter, LineCap.Flat);

            glyph.ConstructionComplete();

            PointSymDef symdef = new PointSymDef("Registration mark", "714", glyph, false);
            symdef.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.Registration_OcadToolbox);
            map.AddSymdef(symdef);
            return symdef;
        }

        public override void Highlight(Graphics g, Matrix xformWorldToPixel, Brush brush, bool erasing)
        {
            SymPath path1, path2;
            float thickness;

            GDIPlus_GraphicsTarget grTarget = new GDIPlus_GraphicsTarget(g);

            object brushKey = new object();
            grTarget.CreateGdiPlusBrush(brushKey, brush, false);

            // Get line thickness.
            thickness = TransformDistance(lineThickness * scaleRatio * appearance.lineWidth, xformWorldToPixel);

            // Get the paths.
            path1 = new SymPath(OffsetCoords(ScaleCoords((PointF[]) coords1.Clone()), location.X, location.Y), kinds1);
            path2 = new SymPath(OffsetCoords(ScaleCoords((PointF[]) coords2.Clone()), location.X, location.Y), kinds2);

            object penKey = new object();
            grTarget.CreatePen(penKey, brushKey, thickness, LineCap.Flat, LineJoin.Miter, 5);

            // Draw the paths
            path1.DrawTransformed(grTarget, penKey, xformWorldToPixel);
            path2.DrawTransformed(grTarget, penKey, xformWorldToPixel);

            grTarget.Dispose();
        }
    }

    // A forbidden cross
    class ForbiddenCourseObj: PointCourseObj
    {
        PointKind[] kinds1 = { PointKind.Normal, PointKind.Normal };
        PointF[] coords1 = { new PointF(-1.06F, -1.06F), new PointF(1.06F, 1.06F) };
        PointKind[] kinds2 =  { PointKind.Normal, PointKind.Normal };
        PointF[] coords2 = { new PointF(1.06F, -1.06F), new PointF(-1.06F, 1.06F) };

        public ForbiddenCourseObj(Id<Special> specialId, float scaleRatio, CourseAppearance appearance, PointF location)
            : base(Id<ControlPoint>.None, Id<CourseControl>.None, specialId, scaleRatio, appearance, null, 0, 1.5F, location)
        {
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            Glyph glyph = new Glyph();

            // Note: the line thickness of forbidden marks do NOT scale with the Line Thickness in the Course Appearance. This is by design,
            // otherwise it would look kind of weird. The scale with the control circle size instead to maintain the ratio.

            SymPath path = new SymPath(ScaleCoords((PointF[]) coords1.Clone()), kinds1);
            glyph.AddLine(symColor, path, 0.35F * scaleRatio * appearance.controlCircleSize, LineJoin.Miter, LineCap.Flat);

            path = new SymPath(ScaleCoords((PointF[]) coords2.Clone()), kinds2);
            glyph.AddLine(symColor, path, 0.35F * scaleRatio * appearance.controlCircleSize, LineJoin.Miter, LineCap.Flat);

            glyph.ConstructionComplete();

            PointSymDef symdef = new PointSymDef("Forbidden route", "710", glyph, false);
            symdef.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.Forbidden_OcadToolbox);
            map.AddSymdef(symdef);
            return symdef;
        }

        public override void Highlight(Graphics g, Matrix xformWorldToPixel, Brush brush, bool erasing)
        {
            SymPath path1, path2;
            float thickness;

            GDIPlus_GraphicsTarget grTarget = new GDIPlus_GraphicsTarget(g);

            object brushKey = new object();
            grTarget.CreateGdiPlusBrush(brushKey, brush, false);

            // Get line thickness.
            thickness = TransformDistance(NormalCourseAppearance.lineThickness * scaleRatio * appearance.controlCircleSize, xformWorldToPixel);

            // Get the paths.
            path1 = new SymPath(OffsetCoords(ScaleCoords((PointF[]) coords1.Clone()), location.X, location.Y), kinds1);
            path2 = new SymPath(OffsetCoords(ScaleCoords((PointF[]) coords2.Clone()), location.X, location.Y), kinds2);

            // Draw the paths
            object penKey = new object();
            grTarget.CreatePen(penKey, brushKey, thickness, LineCap.Flat, LineJoin.Miter, 5);

            path1.DrawTransformed(grTarget, penKey, xformWorldToPixel);
            path2.DrawTransformed(grTarget, penKey, xformWorldToPixel);

            grTarget.Dispose();
        }
    }

    // A normal leg
    class LegCourseObj : LineCourseObj
    {
        public LegCourseObj(Id<ControlPoint> controlId, Id<CourseControl> courseControlId, Id<CourseControl> courseControlId2, float scaleRatio, CourseAppearance appearance, SymPath path, LegGap[] gaps)
            : base(controlId, courseControlId, courseControlId2, Id<Special>.None, scaleRatio, appearance, NormalCourseAppearance.lineThickness * appearance.lineWidth, path, gaps)
        {
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            LineSymDef symdef = new LineSymDef("Line", "704", symColor, NormalCourseAppearance.lineThickness * scaleRatio * appearance.lineWidth, LineJoin.Bevel, LineCap.Flat);
            symdef.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.Line_OcadToolbox);
            map.AddSymdef(symdef);
            return symdef;
        }

        public override bool HandlesOnEnds
        {
            get { return false; }
        }
    }

    // A flagged leg
    class FlaggedLegCourseObj : LineCourseObj
    {
        public FlaggedLegCourseObj(Id<ControlPoint> controlId, Id<CourseControl> courseControlId, Id<CourseControl> courseControlId2, float scaleRatio, CourseAppearance appearance, SymPath path, LegGap[] gaps)
            : base(controlId, courseControlId, courseControlId2, Id<Special>.None, scaleRatio, appearance, NormalCourseAppearance.lineThickness * appearance.lineWidth, path, gaps)
        {
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            LineSymDef symdef = new LineSymDef("Marked route", "705", symColor, NormalCourseAppearance.lineThickness * scaleRatio * appearance.lineWidth, LineJoin.Bevel, LineCap.Flat);

            LineSymDef.DashInfo dashes = new LineSymDef.DashInfo();
            dashes.dashLength = dashes.firstDashLength = dashes.lastDashLength = 2.0F * scaleRatio;
            dashes.gapLength = 0.5F * scaleRatio;
            dashes.minGaps = 1;
            symdef.SetDashInfo(dashes);

            symdef.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.DashedLine_OcadToolbox);   
            map.AddSymdef(symdef);
            return symdef;
        }

        public override bool HandlesOnEnds
        {
            get { return false; }
        }
    }

    // A boundary
    class BoundaryCourseObj : LineCourseObj
    {
        public BoundaryCourseObj(Id<Special> specialId, float scaleRatio, CourseAppearance appearance, SymPath path)
            : base(Id<ControlPoint>.None, Id<CourseControl>.None, Id<CourseControl>.None, specialId, scaleRatio, appearance, 0.7F * appearance.lineWidth, path, null)
        {
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            LineSymDef symdef = new LineSymDef("Uncrossable boundary", "707", symColor, 0.7F * scaleRatio * appearance.lineWidth, LineJoin.Bevel, LineCap.Flat);
            symdef.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.Line_OcadToolbox);
            map.AddSymdef(symdef);
            return symdef;
        }
    }

    // An arbitrary line.
    class LineSpecialCourseObj: LineCourseObj
    {
        public readonly SpecialColor color;
        public readonly LineKind lineKind;
        public readonly float lineWidth;
        public readonly float gapSize;
        public readonly float dashSize;

        public LineSpecialCourseObj(Id<Special> specialId, CourseAppearance appearance, SpecialColor color, LineKind lineKind, float lineWidth, float gapSize, float dashSize, SymPath path)
            : base(Id<ControlPoint>.None, Id<CourseControl>.None, Id<CourseControl>.None, specialId, 1.0F, appearance, 
                   (lineKind == LineKind.Double) ? (lineWidth * 2 + gapSize) : lineWidth, path, null)
        {
            this.color = color;
            this.lineKind = lineKind;
            this.lineWidth = lineWidth;
            this.gapSize = gapSize;
            this.dashSize = dashSize;
        }

        // A struct synthesizes Equals/GetHashCode automatically.
        // CONSIDER: use FontDesc instead!
        struct MySymdefKey
        {
            public SpecialColor color; 
            public LineKind lineKind; 
            public float lineWidth;
            public float gapSize;
            public float dashSize;
        }

        protected override object SymDefKey()
        {
            MySymdefKey key = new MySymdefKey();
            key.color = color; 
            key.lineKind = lineKind; 
            key.lineWidth = lineWidth;
            key.gapSize = gapSize;
            key.dashSize = dashSize;

            return key;
        }

        public override bool Equals(object obj)
        {
            LineSpecialCourseObj other = obj as LineSpecialCourseObj;
            if (other == null)
                return false;
            if (!other.color.Equals(color))
                return false;
            if (other.lineKind != lineKind)
                return false;
            if (other.lineWidth != lineWidth)
                return false;
            if (other.gapSize != gapSize)
                return false;
            if (other.dashSize != dashSize)
                return false;

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() + color.GetHashCode() + lineKind.GetHashCode() + lineWidth.GetHashCode() + gapSize.GetHashCode() + dashSize.GetHashCode() ;
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            return CreateLineSpecialSymDef(map, symColor, lineKind, lineWidth, gapSize, dashSize, LineJoin.Bevel, LineCap.Flat);
        }

        // This is used by both line and rectangle specials.
        public static SymDef CreateLineSpecialSymDef(Map map, SymColor symColor, LineKind lineKind, float lineWidth, float gapSize, float dashSize, LineJoin lineJoin, LineCap lineCap)
        {
            string symbolId = map.GetFreeSymbolId(901);

            LineSymDef symdef;
            switch (lineKind) {
                case LineKind.Single:
                    symdef = new LineSymDef("Line", symbolId, symColor, lineWidth, lineJoin, lineCap);
                    break;

                case LineKind.Double:
                    LineSymDef.DoubleLineInfo doubleInfo = new LineSymDef.DoubleLineInfo();
                    doubleInfo.doubleLeftColor = doubleInfo.doubleRightColor = symColor;
                    doubleInfo.doubleThick = gapSize;
                    doubleInfo.doubleLeftWidth = doubleInfo.doubleRightWidth = lineWidth;
                    symdef = new LineSymDef("Line", symbolId, null, 0, lineJoin, lineCap);
                    symdef.SetDoubleLines(doubleInfo);
                    break;

                case LineKind.Dashed:
                    LineSymDef.DashInfo dashInfo = new LineSymDef.DashInfo();
                    dashInfo.dashLength = dashInfo.firstDashLength = dashInfo.lastDashLength = dashSize;
                    dashInfo.gapLength = gapSize;
                    symdef = new LineSymDef("Line", symbolId, symColor, lineWidth, lineJoin, lineCap);
                    symdef.SetDashInfo(dashInfo);
                    break;

                default: throw new ApplicationException("Unexpected line kind");
            }

            symdef.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.LineSpecial_OcadToolbox);
            map.AddSymdef(symdef);
            return symdef;
        }

        public override SpecialColor CustomColor
        {
            get {
                return color;
            }
        }

        public override string ToString()
        {
            string result = base.ToString();
            result += string.Format("  color:{0}  lineKind:{1}  lineWidth:{2}  gapSize:{3}  dashSize:{4}", color, lineKind, lineWidth, gapSize, dashSize);
            return result;
        }
    }

    // An arbitrary rectangle
    class RectSpecialCourseObj : RectCourseObj
    {
        public readonly SpecialColor color;
        public readonly LineKind lineKind;
        public readonly float lineWidth;
        public readonly float cornerRadius;
        public readonly float gapSize;
        public readonly float dashSize;

        public RectSpecialCourseObj(Id<Special> specialId, CourseAppearance appearance, SpecialColor color, LineKind lineKind, float lineWidth, float cornerRadius, float gapSize, float dashSize, RectangleF rect)
            : base(Id<ControlPoint>.None, Id<CourseControl>.None, specialId, 1.0F, appearance, rect)
        {
            this.color = color;
            this.lineKind = lineKind;
            this.lineWidth = lineWidth;
            this.cornerRadius = cornerRadius;
            this.gapSize = gapSize;
            this.dashSize = dashSize;
        }

        private SymPath CreateSymPath()
        {
            return SymPath.CreateRoundedRectangle(rect, cornerRadius);
        }

        // A struct synthesizes Equals/GetHashCode automatically.
        // CONSIDER: use FontDesc instead!
        struct MySymdefKey
        {
            public SpecialColor color;
            public LineKind lineKind;
            public float lineWidth;
            public float gapSize;
            public float dashSize;
        }

        protected override object SymDefKey()
        {
            MySymdefKey key = new MySymdefKey();
            key.color = color;
            key.lineKind = lineKind;
            key.lineWidth = lineWidth;
            key.gapSize = gapSize;
            key.dashSize = dashSize;

            return key;
        }
        public override bool Equals(object obj)
        {
            RectSpecialCourseObj other = obj as RectSpecialCourseObj;
            if (other == null)
                return false;
            if (!other.color.Equals(color))
                return false;
            if (other.lineKind != lineKind)
                return false;
            if (other.lineWidth != lineWidth)
                return false;
            if (other.gapSize != gapSize)
                return false;
            if (other.dashSize != dashSize)
                return false;
            if (other.cornerRadius != cornerRadius)
                return false;

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() + color.GetHashCode() + lineKind.GetHashCode() + lineWidth.GetHashCode() + gapSize.GetHashCode() + dashSize.GetHashCode() + cornerRadius.GetHashCode();
        }

        // The full width of the line, even if a double line.
        private float FullWidth
        {
            get
            {
                if (lineKind == LineKind.Double)
                    return lineWidth * 2 + gapSize;
                else
                    return lineWidth;
            }
        }

        // Get the distance of a point from this object, or 0 if the point is covered by the object.
        public override double DistanceFromPoint(PointF pt)
        {
            PointF closestPoint;

            SymPath path = CreateSymPath();
            return Math.Max(0, path.DistanceFromPoint(pt, out closestPoint) - (FullWidth/2));
        }

        public override void Highlight(Graphics g, Matrix xformWorldToPixel, Brush brush, bool erasing)
        {
            object brushKey = new object();
            object penKey = new object();

            using (GDIPlus_GraphicsTarget graphicsTarget = new GDIPlus_GraphicsTarget(g)) {
                graphicsTarget.CreateGdiPlusBrush(brushKey, brush, false);
                graphicsTarget.CreatePen(penKey, brushKey, Geometry.TransformDistance(FullWidth, xformWorldToPixel), LineCap.Flat, LineJoin.Miter, 10);
                SymPath path = CreateSymPath();
                path = path.Transform(xformWorldToPixel);
                path.Draw(graphicsTarget, penKey);
            }
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            return LineSpecialCourseObj.CreateLineSpecialSymDef(map, symColor, lineKind, lineWidth, gapSize, dashSize, LineJoin.Miter, LineCap.Flat);
        }

        protected override void AddToMap(Map map, SymDef symdef)
        {
            SymPath symPath = CreateSymPath();

            LineSymbol sym = new LineSymbol((LineSymDef)symdef, symPath);
            map.AddSymbol(sym);
        }

        public override SpecialColor CustomColor
        {
            get
            {
                return color;
            }
        }

        public override string ToString()
        {
            string result = base.ToString();
            result += string.Format("  color:{0}  lineKind:{1}  lineWidth:{2}  cornerRadius:{3}  gapSize:{4}  dashSize:{5}", color, lineKind, cornerRadius, lineWidth, gapSize, dashSize);
            return result;
        }
    }

    // An out of bounds area
    class OOBCourseObj : AreaCourseObj
    {
        public OOBCourseObj(Id<Special> specialId, float scaleRatio, CourseAppearance appearance, PointF[] pts)
            : base(Id<ControlPoint>.None, Id<CourseControl>.None, specialId, scaleRatio, appearance, pts)
        {
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            AreaSymDef symdef = new AreaSymDef("Out-of-bounds area", "709", null, null);
            AreaSymDef.HatchInfo hatchInfo = new AreaSymDef.HatchInfo();
            hatchInfo.hatchColor = symColor;
            hatchInfo.hatchWidth = 0.25F * scaleRatio;
            hatchInfo.hatchSpacing = 0.6F * scaleRatio;
            hatchInfo.hatchAngle = 90;
            symdef.AddHatching(hatchInfo);
            symdef.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.OOB_OcadToolbox);
            map.AddSymdef(symdef);
            return symdef;
        }
    }

    // A dangerous area
    class DangerousCourseObj : AreaCourseObj
    {
        public DangerousCourseObj(Id<Special> specialId, float scaleRatio, CourseAppearance appearance, PointF[] pts)
            : base(Id<ControlPoint>.None, Id<CourseControl>.None, specialId, scaleRatio, appearance, pts)
        {
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            AreaSymDef symdef = new AreaSymDef("Dangerous area", "710", null, null);
            AreaSymDef.HatchInfo hatchInfo = new AreaSymDef.HatchInfo();
            hatchInfo.hatchColor = symColor;
            hatchInfo.hatchWidth = 0.25F * scaleRatio;
            hatchInfo.hatchSpacing = 0.6F * scaleRatio;
            hatchInfo.hatchAngle = 45;
            symdef.AddHatching(hatchInfo);
            hatchInfo.hatchAngle = 135;
            symdef.AddHatching(hatchInfo);
            symdef.ToolboxImage = MapUtil.CreateToolboxIcon(Properties.Resources.OOB_OcadToolbox);
            map.AddSymdef(symdef);
            return symdef;
        }
  }

    // A dangerous area
    class WhiteOutCourseObj: AreaCourseObj
    {
        public WhiteOutCourseObj(Id<Special> specialId, float scaleRatio, CourseAppearance appearance, PointF[] pts)
            : base(Id<ControlPoint>.None, Id<CourseControl>.None, specialId, scaleRatio, appearance, pts)
        {
        }
        
        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            Debug.Fail("should never be called");
            return null;
        }

        public override void AddToMap(Map map, SymColor symColor, Dictionary<object, SymDef> dict)
        {
            AddToMap(map, dict[CourseLayout.KeyWhiteOut]);
        }
    }

    // CONSIDER: merge ControlNumberCourseObj and CodeCourseObj since they are so similar!

    // A control number
    class ControlNumberCourseObj : TextCourseObj
    {
        public PointF centerPoint;

        public ControlNumberCourseObj(Id<ControlPoint> controlId, Id<CourseControl> courseControlId, float scaleRatio, CourseAppearance appearance, string text, PointF centerPoint)
            : base(controlId, courseControlId, Id<Special>.None, text, centerPoint, NormalCourseAppearance.controlNumberFont.Name,
                   appearance.numberBold ? NormalCourseAppearance.controlNumberFontBold.Style : NormalCourseAppearance.controlNumberFont.Style, SpecialColor.Purple,
                   NormalCourseAppearance.controlNumberFont.EmHeight * scaleRatio * appearance.numberHeight, scaleRatio * appearance.numberOutlineWidth)
        {
            // Update the top left coord so the text is centered on centerPoint.
            this.centerPoint = centerPoint;
            topLeft = new PointF(centerPoint.X - size.Width / 2, centerPoint.Y + size.Height / 2);
        }

        protected override string SymDefName
        {
            get { return "Control number"; }
        }

        protected override int OcadIdIntegerPart
        {
            get { return 703; }
        }
    }

    // A control code
    class CodeCourseObj : TextCourseObj
    {
        public PointF centerPoint;

        public CodeCourseObj(Id<ControlPoint> controlId, Id<CourseControl> courseControlId, float scaleRatio, CourseAppearance appearance, string text, PointF centerPoint)
            : base(controlId, courseControlId, Id<Special>.None, text, centerPoint, NormalCourseAppearance.controlCodeFont.Name, NormalCourseAppearance.controlCodeFont.Style, SpecialColor.Purple,
            NormalCourseAppearance.controlCodeFont.EmHeight * scaleRatio * appearance.numberHeight, scaleRatio * appearance.numberOutlineWidth)
        {
            // Update the top left coord so the text is centered on centerPoint.
            this.centerPoint = centerPoint;
            topLeft = new PointF(centerPoint.X - size.Width / 2, centerPoint.Y + size.Height / 2);
        }

        protected override string SymDefName
        {
            get { return "Control code"; }
        }

        protected override int OcadIdIntegerPart
        {
            get { return 720; }
        }
   }

   // Arbitrary text, set withing a bounding rectangle. The text is sized to fit inside the bounding rectangle.
   class BasicTextCourseObj: TextCourseObj
   {
       private RectangleF rectBounding;

       public BasicTextCourseObj(Id<Special> specialId, string text, RectangleF rectBounding, string fontName, FontStyle fontStyle, SpecialColor color)
           : base(Id<ControlPoint>.None, Id<CourseControl>.None, specialId, text, new PointF(rectBounding.Left, rectBounding.Bottom), fontName, fontStyle, color, CalculateEmHeight(text, fontName, fontStyle, rectBounding.Size), 0.0F)
       {
           this.rectBounding = rectBounding;
       }

       // Given some text in a font and a bounding rectangle, figure out the correct em-height so that the text fits in the rectangle.
       static private float CalculateEmHeight(string text, string fontName, FontStyle fontStyle, SizeF desiredSize)
       {
           if (String.IsNullOrEmpty(text) || desiredSize.Width == 0)
               return desiredSize.Height;
           if (desiredSize.Height == 0)
               return 0;

           // Measure with a font size of 1, then scale appropriately.
           Graphics g = Util.GetHiresGraphics();
           SizeF size;
           using (Font f = new Font(fontName, 1F, fontStyle, GraphicsUnit.World))
               size = g.MeasureString(text, f, new PointF(0, 0), StringFormat.GenericTypographic);

           if (size.Width * desiredSize.Height > size.Height * desiredSize.Width) {
               // width is the deciding factor.
               return desiredSize.Width / size.Width;
           }
           else {
               // height is the deciding factor.
               return desiredSize.Height / size.Height;
           }
       }

       protected override string SymDefName
       {
           get { return "Text"; }
       }

       protected override int OcadIdIntegerPart
       {
           get { return 730; }
       }

       public override PointF[] GetHandles()
       {
           // Handles on sides and corners. Handle 0 is at bottom-left (which corresponds to rectBounding.Left,rectBounding.Top, since rectBounding is inverted). Goes counter-clockwise
           // from there.
           float middleWidth = (rectBounding.Left + rectBounding.Right) / 2;
           float middleHeight = (rectBounding.Top + rectBounding.Bottom) / 2;
           PointF[] handles = { new PointF(rectBounding.Left, rectBounding.Top), new PointF(middleWidth, rectBounding.Top), new PointF(rectBounding.Right, rectBounding.Top),
                                             new PointF(rectBounding.Left, middleHeight), new PointF(rectBounding.Right, middleHeight),
                                             new PointF(rectBounding.Left, rectBounding.Bottom), new PointF(middleWidth, rectBounding.Bottom), new PointF(rectBounding.Right, rectBounding.Bottom)};
           return handles;
       }

       public override Cursor GetHandleCursor(PointF handlePoint)
       {
           // Get the correct sizing cursors for each point given above. 
           int index = Array.IndexOf(GetHandles(), handlePoint);

           switch (index) {
           case 0:
           case 7: return Cursors.SizeNESW;
           case 1:
           case 6: return Cursors.SizeNS;
           case 2:
           case 5: return Cursors.SizeNWSE;
           case 3:
           case 4: return Cursors.SizeWE;
           default: return Util.MoveHandleCursor;
           }
       }

       public override void Highlight(Graphics g, Matrix xformWorldToPixel, Brush brush, bool erasing)
       {
           // Draw the text.
           base.Highlight(g, xformWorldToPixel, brush, erasing);

           PointF[] corners = { new PointF(rectBounding.Left, rectBounding.Bottom), new PointF(rectBounding.Right, rectBounding.Top) };
           xformWorldToPixel.TransformPoints(corners);

           // Draw an outline.
           using (Pen p = new Pen(brush, 0)) {
               g.DrawRectangle(p, corners[0].X, corners[0].Y, corners[1].X - corners[0].X, corners[1].Y - corners[0].Y);
           }
       }

       // Get the bounds of the highlight
       public override RectangleF GetHighlightBounds()
       {
           return rectBounding;
       }

       public override void Offset(float dx, float dy)
       {
           base.Offset(dx, dy);
           rectBounding.Offset(dx, dy);
       }

       // Move a handle on the rectangle.
       public override void MoveHandle(PointF oldHandle, PointF newHandle)
       {
           PointF[] handles = GetHandles();
           int handleIndex = Array.IndexOf(handles, oldHandle);

           // Existing coordinates of the rectangle.
           float left = rectBounding.Left, top = rectBounding.Top, right = rectBounding.Right, bottom = rectBounding.Bottom;

           // Figure out which coord(s) moving this handle changes.
           bool changeLeft = false, changeTop = false, changeRight = false, changeBottom = false;
           switch (handleIndex) {
           case 0: changeLeft = true; changeTop = true; break;
           case 1: changeTop = true; break;
           case 2: changeRight = true; changeTop = true; break;
           case 3: changeLeft = true; break;
           case 4: changeRight = true; break;
           case 5: changeLeft = true; changeBottom = true; break;
           case 6: changeBottom = true; break;
           case 7: changeRight = true; changeBottom = true; break;
           default:
               Debug.Fail("bad handle"); break;
           }

           // Update the coordinates based on movement.
           if (changeLeft) left = newHandle.X;
           if (changeTop) top = newHandle.Y;
           if (changeRight) right = newHandle.X;
           if (changeBottom) bottom = newHandle.Y;

           RectangleF newRect = Geometry.RectFromPoints(left, top, right, bottom);

           // Update the rectangle.
           base.EmHeight = CalculateEmHeight(text, fontName, fontStyle, newRect.Size);
           base.topLeft = new PointF(newRect.Left, newRect.Bottom);
           rectBounding = newRect;
       }

       public override string ToString()
       {
           return base.ToString() + string.Format("  rect:({0},{1})-({2},{3})", rectBounding.Left, rectBounding.Bottom, rectBounding.Right, rectBounding.Top);
       }
    }

    // This course object is a description sheet block.
    class DescriptionCourseObj: AspectPreservingRectCourseObj
    {
        DescriptionRenderer renderer;        // The description renderer that holds the description.
        float[] aspectAnglesByColumns;       // array describing the angles that are closest for each number of columns.

        // Create a new description course object.
        public DescriptionCourseObj(Id<Special> specialId, PointF topLeft, float cellSize, SymbolDB symbolDB, DescriptionLine[] description, DescriptionKind kind, int numColumns)
            : base(Id<ControlPoint>.None, Id<CourseControl>.None, specialId, 1, new CourseAppearance(), GetRect(topLeft, cellSize, symbolDB, description, kind, numColumns))
        {
            // Create the renderer.
            renderer = new DescriptionRenderer(symbolDB);
            renderer.Description = description;
            renderer.DescriptionKind = kind;
            renderer.Margin = cellSize / 20;   // about the thickness of the thick lines.
            renderer.CellSize = cellSize;
            renderer.NumberOfColumns = numColumns;
            aspectAnglesByColumns = ComputeAspectAngles();
        }

        // Get the rectangle used by the description.
        static RectangleF GetRect(PointF topLeft, float cellSize, SymbolDB symbolDB, DescriptionLine[] description, DescriptionKind kind, int numColumns)
        {
            // Create the renderer.
            DescriptionRenderer renderer = new DescriptionRenderer(symbolDB);
            renderer.Description = description;
            renderer.DescriptionKind = kind;
            renderer.Margin = cellSize / 20;   // about the thickness of the thick lines.
            renderer.CellSize = cellSize;
            renderer.NumberOfColumns = numColumns;

            SizeF size = renderer.Measure();
            return new RectangleF(topLeft.X, topLeft.Y - size.Height, size.Width, size.Height);
        }

        public override object Clone()
        {
            DescriptionCourseObj c = (DescriptionCourseObj)(base.Clone());
            c.renderer = (DescriptionRenderer) this.renderer.Clone();
            return c;
        }

        // The user has updated the rectangle. Update the cell size to match.
        public override void RectangleUpdating(ref RectangleF newRect, bool dragAll, bool dragLeft, bool dragTop, bool dragRight, bool dragBottom)
        {
            int bestNumberOfColumns = BestNumberOfColumns(newRect.Size);

            if (bestNumberOfColumns != renderer.NumberOfColumns) {
                renderer.NumberOfColumns = bestNumberOfColumns;
                SizeF size = renderer.Measure();
                aspect = size.Width / size.Height;
            }

            base.RectangleUpdating(ref newRect, dragAll, dragLeft, dragTop, dragRight, dragBottom);

            renderer.CellSize = newRect.Height / renderer.ColumnLengthInCells;
        }

        // Get the cell size.
        public float CellSize
        {
            get
            {
                return renderer.CellSize;
            }
        }

        // Get the number of columns
        public int NumberOfColumns
        {
            get { return renderer.NumberOfColumns; }
        }

        // Add the description to the map. Uses the map rendering functionality in the renderer.
        public override void AddToMap(Map map, SymColor symColor, Dictionary<object, SymDef> dict)
        {
            renderer.RenderToMap(map, symColor, new PointF(rect.Left, rect.Bottom), dict);
        }

        // This override is not needed because we are not using the base implemention of AddToMap.
        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            throw new NotSupportedException("not supported");
        }

        // This override is not needed because we are not using the base implemention of AddToMap.
        protected override void AddToMap(Map map, SymDef symdef)
        {
            throw new NotSupportedException("not supported");
        }

        // Draw the highlight. Everything must be draw in pixel coords so fast erase works correctly.
        public override void Highlight(Graphics g, Matrix xformWorldToPixel, Brush brush, bool erasing)
        {
            if (NumberOfColumns == 1)
                base.Highlight(g, xformWorldToPixel, brush, erasing);
            else {
                RectangleF currentColumnRect = rect;
                currentColumnRect.Width = renderer.ColumnWidth;
                for (int i = 0; i < renderer.NumberOfColumns; ++i) {
                    DrawBorderedRectangle(g, xformWorldToPixel, currentColumnRect, brush, erasing);
                    currentColumnRect.X += renderer.ColumnWidth + renderer.ColumnGap;
                }
            }
        }

        private float[] ComputeAspectAngles()
        {
            int numColumns = renderer.NumberOfColumns;
            int maxColumns = Math.Max(1, (renderer.Description.Length - 1) / 4);  // maximum number of columns.
            float[] aspectAnglesByColumns = new float[maxColumns + 1];
            for (int i = 1; i <= maxColumns; ++i) {
                renderer.NumberOfColumns = i;
                SizeF size = renderer.Measure();
                aspectAnglesByColumns[i] = (float)Math.Atan2(size.Height, size.Width);
            }

            renderer.NumberOfColumns = numColumns;
            return aspectAnglesByColumns;
        }

        private int BestNumberOfColumns(SizeF currentSize)
        {
            float aspectAngle = (float) Math.Atan2(currentSize.Height, currentSize.Width);

            float bestAngleDiff = Math.Abs(aspectAnglesByColumns[1] - aspectAngle);
            int bestColumns = 1;
            for (int i = 2; i < aspectAnglesByColumns.Length; ++i) {
                float angleDiff = Math.Abs(aspectAnglesByColumns[i] - aspectAngle);
                if (angleDiff < bestAngleDiff) {
                    bestAngleDiff = angleDiff;
                    bestColumns = i;
                }
            }

            return bestColumns;
        }

        // Are we equal?
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }

            DescriptionCourseObj other = (DescriptionCourseObj) obj;

            // Check description kind
            if (renderer.DescriptionKind != other.renderer.DescriptionKind)
                return false;
            if (renderer.NumberOfColumns != other.renderer.NumberOfColumns)
                return false;

            // Check description 
            DescriptionLine[] myDesc = renderer.Description;
            DescriptionLine[] otherDesc = other.renderer.Description;
            if (myDesc.Length != otherDesc.Length)
                return false;
            for (int i = 0; i < myDesc.Length; ++i) {
                if (! myDesc[i].Equals(otherDesc[i]))
                    return false;
            }

            // Check id and bounding rect.
            return base.Equals(obj);
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            throw new NotSupportedException("The method or operation is not supported.");
        }

        public override string ToString()
        {
            string text = base.ToString();
            if (NumberOfColumns > 1)
                text += string.Format(" columns:{0}", NumberOfColumns);
            return text;
        }
    }

    
    class ImageCourseObj: AspectPreservingRectCourseObj
    {
        public readonly string imageName;
        public readonly Bitmap imageBitmap;
        private ImageLoader imageLoader;

        public ImageCourseObj(Id<Special> specialId, float scaleRatio, CourseAppearance appearance, PointF[] locations, string imageName, Bitmap imageBitmap)
            :base(Id<ControlPoint>.None, Id<CourseControl>.None, specialId, scaleRatio, appearance, Geometry.RectFromPoints(locations[0].X, locations[0].Y, locations[1].X, locations[1].Y))
        {
            this.imageName = imageName;
            this.imageBitmap = imageBitmap;
            this.imageLoader = new ImageLoader(imageName, imageBitmap);
        }

        public override void AddToMap(Map map, SymColor symColor, Dictionary<object, SymDef> dict)
        {
            IList<TemplateInfo> currentTemplates = map.Templates;
            
            PointF center = Geometry.RectCenter(rect);
            float dpi = (25.4F * imageBitmap.Width) / rect.Width;
            TemplateInfo newTemplate = new TemplateInfo(imageName, center, dpi, 0, true, imageLoader);

            List<TemplateInfo> newTemplates = new List<TemplateInfo>(currentTemplates.Count + 1);
            newTemplates.Add(newTemplate);
            newTemplates.AddRange(currentTemplates);
            map.Templates = newTemplates;

            /* The following code creates the image as a ImageSymDef instead of a template. We use templates because
             * they are compatible with OCAD 8,9,10, while ImageSymDef only works for OCAD 11+.
             
            ImageSymDef layoutSymDef = (ImageSymDef) dict[CourseLayout.KeyLayout];

            PointF center = Geometry.RectCenter(rect);
            ImageBitmapSymbol symbol = new ImageBitmapSymbol(layoutSymDef, imageName, center, rect.Width / imageBitmap.Width, rect.Height / imageBitmap.Height, true, specialId.id, imageLoader);
            map.AddSymbol(symbol);
             */
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }
            if (((ImageCourseObj)obj).imageName != imageName)
                return false;
            if (((ImageCourseObj)obj).imageBitmap != imageBitmap)
                return false;

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ imageName.GetHashCode();
        }

        protected override void AddToMap(Map map, SymDef symdef)
        {
            // Shouldn't be called.
            throw new NotImplementedException();
        }

        protected override SymDef CreateSymDef(Map map, SymColor symColor)
        {
            // Shouldn't be called.
            throw new NotImplementedException();
        }

        // The ImageLoader handles providing images to the map based on the image name.
        private class ImageLoader: IFileLoader
        {
            private string imageName;
            private Bitmap imageBitmap;

            public ImageLoader(string imageName, Bitmap imageBitmap)
            {
                this.imageName = imageName;
                this.imageBitmap = imageBitmap;
            }

            public FileKind CheckFileKind(string path)
            {
                if (string.Equals(path, imageName, StringComparison.InvariantCultureIgnoreCase))
                    return FileKind.OtherFile;
                else
                    return FileKind.DoesntExist;
            }

            public IGraphicsBitmap LoadBitmap(string path, bool isTemplate)
            {
                if (string.Equals(path, imageName, StringComparison.InvariantCultureIgnoreCase))
                    return new GDIPlus_Bitmap(imageBitmap);
                else
                    return null;
            }

            public Map LoadMap(string path, Map referencingMap)
            {
                return null;
            }

        }
    }
    
}
