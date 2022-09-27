/* Copyright (c) 2011, Peter Golde
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
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Drawing.Drawing2D;

namespace Graphics2D.Tests
{
    using PurplePen.Graphics2D;

    [TestFixture]
    public class GeometryTests
    {
        [Test]
        public void TransformRectangle()
        {
            RectangleF from = new RectangleF(7.8F, 9.1F, 11.1F, 22.1F);
            RectangleF to = new RectangleF(72.8F, 901.1F, 234.1F, 441F);

            Matrix result = Geometry.CreateRectangleTransform(from, to);

            PointF pt = Geometry.TransformPoint(from.Location, result);
            Assert.AreEqual(to.Left, pt.X, 0.001F);
            Assert.AreEqual(to.Top, pt.Y, 0.001F);

            pt = Geometry.TransformPoint(new PointF(from.Left, from.Bottom), result);
            Assert.AreEqual(to.Left, pt.X, 0.001F);
            Assert.AreEqual(to.Bottom, pt.Y, 0.001F);

            pt = Geometry.TransformPoint(new PointF(from.Right, from.Top), result);
            Assert.AreEqual(to.Right, pt.X, 0.001F);
            Assert.AreEqual(to.Top, pt.Y, 0.001F);

            pt = Geometry.TransformPoint(new PointF(from.Right, from.Bottom), result);
            Assert.AreEqual(to.Right, pt.X, 0.001F);
            Assert.AreEqual(to.Bottom, pt.Y, 0.001F);
        }

        [Test]
        public void TransformInvertedRectangle()
        {
            RectangleF source = new RectangleF(3, 4, 7, 8);
            RectangleF dest = new RectangleF(-1, 5, 19, 5.5F);

            Matrix m = Geometry.CreateInvertedRectangleTransform(source, dest);

            PointF[] pts = new PointF[] { source.Location, new PointF(source.Left, source.Bottom), new PointF(source.Right, source.Top) };
            m.TransformPoints(pts);

            Assert.AreEqual(dest.Left, pts[0].X, 0.0001F);
            Assert.AreEqual(dest.Bottom, pts[0].Y, 0.0001F);
            Assert.AreEqual(dest.Left, pts[1].X, 0.0001F);
            Assert.AreEqual(dest.Top, pts[1].Y, 0.0001F);
            Assert.AreEqual(dest.Right, pts[2].X, 0.0001F);
            Assert.AreEqual(dest.Bottom, pts[2].Y, 0.0001F);
        }

        [Test]
        public void TransformInvertedRectangle2()
        {
            RectangleF from = new RectangleF(7.8F, 9.1F, 11.1F, 22.1F);
            RectangleF to = new RectangleF(72.8F, 901.1F, 234.1F, 441F);

            Matrix result = Geometry.CreateInvertedRectangleTransform(from, to);

            PointF pt = Geometry.TransformPoint(from.Location, result);
            Assert.AreEqual(to.Left, pt.X, 0.001F);
            Assert.AreEqual(to.Bottom, pt.Y, 0.001F);

            pt = Geometry.TransformPoint(new PointF(from.Left, from.Bottom), result);
            Assert.AreEqual(to.Left, pt.X, 0.001F);
            Assert.AreEqual(to.Top, pt.Y, 0.001F);

            pt = Geometry.TransformPoint(new PointF(from.Right, from.Top), result);
            Assert.AreEqual(to.Right, pt.X, 0.001F);
            Assert.AreEqual(to.Bottom, pt.Y, 0.001F);

            pt = Geometry.TransformPoint(new PointF(from.Right, from.Bottom), result);
            Assert.AreEqual(to.Right, pt.X, 0.001F);
            Assert.AreEqual(to.Top, pt.Y, 0.001F);
        }

        [Test]
        public void TransformRect()
        {
            Matrix m = new Matrix();
            m.Translate(30F, 50F);
            RectangleF rect = Geometry.TransformRectangle(m, new RectangleF(7, 9, 20, 30));
            Assert.AreEqual(new RectangleF(37F, 59F, 20F, 30F), rect);

            m = new Matrix();
            m.Scale(1, -1);
            rect = Geometry.TransformRectangle(m, new RectangleF(7, 9, 20, 30));
            Assert.AreEqual(new RectangleF(7, -39F, 20F, 30F), rect);
        }

        [Test]
        public void RectFromPoints()
        {
            RectangleF rect = Geometry.RectFromPoints(5, 7, 12, 15);
            Assert.AreEqual(5, rect.Left);
            Assert.AreEqual(7, rect.Top);
            Assert.AreEqual(12, rect.Right);
            Assert.AreEqual(15, rect.Bottom);

            rect = Geometry.RectFromPoints(12, 7, 5, 15);
            Assert.AreEqual(5, rect.Left);
            Assert.AreEqual(7, rect.Top);
            Assert.AreEqual(12, rect.Right);
            Assert.AreEqual(15, rect.Bottom);

            rect = Geometry.RectFromPoints(12, 15, 5, 7);
            Assert.AreEqual(5, rect.Left);
            Assert.AreEqual(7, rect.Top);
            Assert.AreEqual(12, rect.Right);
            Assert.AreEqual(15, rect.Bottom);
        }

        [Test]
        public void InflateToPixelBoundaries()
        {
            RectangleF rect = RectangleF.FromLTRB(-3.3F, 2F, 5.2F, 7.7F);
            RectangleF result = Geometry.InflateToPixelBoundaries(rect, 0.2F, new PointF(-0.1F, 0.05F));
            Assert.AreEqual(RectangleF.FromLTRB(-3.3F, 1.85F, 5.3F, 7.85F), result);
        }

        [Test]
        public void Angle()
        {
            float result = Geometry.Angle(new PointF(1, 2), new PointF(4, 1), new PointF(3.5F, 7));
            Assert.AreEqual(66.8F, result, 0.01F);

            result = Geometry.Angle(new PointF(3.5F, 7), new PointF(4, 1), new PointF(1, 2));
            Assert.AreEqual(66.8F, result, 0.01F);

            result = Geometry.Angle(new PointF(-1, 7), new PointF(0, 0), new PointF(1, 7));
            Assert.AreEqual(16.26F, result, 0.01F);

            result = Geometry.Angle(new PointF(1, 7), new PointF(0, 0), new PointF(-1, 7));
            Assert.AreEqual(16.26F, result, 0.01F);

            result = Geometry.Angle(new PointF(7, 1), new PointF(0, 0), new PointF(-7, 1));
            Assert.AreEqual(163.74F, result, 0.01F);

            result = Geometry.Angle(new PointF(-7, 1), new PointF(0, 0), new PointF(7, 1));
            Assert.AreEqual(163.74F, result, 0.01F);

            result = Geometry.Angle(new PointF(-7, 1), new PointF(-7, 1), new PointF(7, 1));
            Assert.AreEqual(0.0F, result, 0.01F);

            result = Geometry.Angle(new PointF(-7, 1), new PointF(7, 1), new PointF(7, 1));
            Assert.AreEqual(0.0F, result, 0.01F);
        }

        [Test]
        public void TransformPoint()
        {
            Matrix m = new Matrix();
            m.Translate(30F, 50F);
            PointF pt = Geometry.TransformPoint(new PointF(7, 13), m);
            Assert.AreEqual(new PointF(37F, 63F), pt);
        }


        [Test]
        public void TransformDistance()
        {
            Matrix m = new Matrix();
            m.Translate(30F, 50F);
            m.Scale(3, 3);
            m.RotateAt(67, new PointF(12, -47));

            Assert.AreEqual(30F, Geometry.TransformDistance(10, m), 0.0001F);
        }
        
        [Test]
        public void RadiansToDegrees()
        {
            double radians;

            radians = Math.Atan2(5, 0);
            Assert.AreEqual(0, Geometry.RadiansToDegrees(radians));

            radians = Math.Atan2(5, -5);
            Assert.AreEqual(45, Geometry.RadiansToDegrees(radians));

            radians = Math.Atan2(0, -5);
            Assert.AreEqual(90, Geometry.RadiansToDegrees(radians));

            radians = Math.Atan2(-5, -5);
            Assert.AreEqual(135, Geometry.RadiansToDegrees(radians));

            radians = Math.Atan2(-5, 0);
            Assert.AreEqual(180, Geometry.RadiansToDegrees(radians));

            radians = Math.Atan2(-5, 5);
            Assert.AreEqual(225, Geometry.RadiansToDegrees(radians));

            radians = Math.Atan2(0, 5);
            Assert.AreEqual(270, Geometry.RadiansToDegrees(radians));

            radians = Math.Atan2(5, 5);
            Assert.AreEqual(315, Geometry.RadiansToDegrees(radians));
        }

        [Test]
        public void CenteredRectangle()
        {
            RectangleF centerOn = new RectangleF(1, 3, 4, 7);   // center point: 3, 6.5
            SizeF size = new SizeF(3, 2.5F);

            RectangleF rect = Geometry.CenteredRectangle(size, centerOn);

            Assert.AreEqual(1.5F, rect.Left, 0.0001F);
            Assert.AreEqual(6.5F - 1.25F, rect.Top, 0.0001F);
            Assert.AreEqual(4.5F, rect.Right, 0.0001F);
            Assert.AreEqual(6.5F + 1.25F, rect.Bottom, 0.0001F);
        }

        [Test]
        public void LineSegmentsIntersect()
        {
            PointF intsect;
            bool result;

            // no intersect
            result = Geometry.LineSegmentsIntersect(new PointF(0, 0), new PointF(2, 8), new PointF(8, 0), new PointF(0, 20), out intsect);
            Assert.False(result);
            Assert.AreEqual(float.NaN, intsect.X);
            Assert.AreEqual(float.NaN, intsect.Y);

            // intersect
            result = Geometry.LineSegmentsIntersect(new PointF(0, 10), new PointF(2, 0), new PointF(10, 0), new PointF(0, 5), out intsect);
            Assert.True(result);
            Assert.AreEqual(1.111111F, intsect.X, 0.00001F);
            Assert.AreEqual(4.444444F, intsect.Y, 0.00001F);

            // parallel, vertical
            result = Geometry.LineSegmentsIntersect(new PointF(0, 0), new PointF(0, 10), new PointF(2, 0), new PointF(2, 10), out intsect);
            Assert.False(result);
            Assert.AreEqual(float.NaN, intsect.X);
            Assert.AreEqual(float.NaN, intsect.Y);

            // parallel, diagonal
            result = Geometry.LineSegmentsIntersect(new PointF(0, 0), new PointF(5, 5), new PointF(2, 0), new PointF(7, 5), out intsect);
            Assert.False(result);
            Assert.AreEqual(float.NaN, intsect.X);
            Assert.AreEqual(float.NaN, intsect.Y);

            // collinear, overlap
            result = Geometry.LineSegmentsIntersect(new PointF(0, 0), new PointF(5, 5), new PointF(2, 2), new PointF(7, 7), out intsect);
            Assert.True(result);
            Assert.AreEqual(float.NaN, intsect.X);
            Assert.AreEqual(float.NaN, intsect.Y);

            // collinear, no overlap
            result = Geometry.LineSegmentsIntersect(new PointF(0, 0), new PointF(5, 5), new PointF(7, 7), new PointF(10, 10), out intsect);
            Assert.False(result);
            Assert.AreEqual(float.NaN, intsect.X);
            Assert.AreEqual(float.NaN, intsect.Y);
        }

        [Test]
        public void LineSegmentRectIntersection()
        {
            bool result;
            RectangleF rect = new RectangleF(1, 1, 4, 9);

            result = Geometry.LineSegmentIntersectsRect(new PointF(2, 2), new PointF(3, 3), rect);
            Assert.True(result);

            result = Geometry.LineSegmentIntersectsRect(new PointF(2, 2), new PointF(23, 3), rect);
            Assert.True(result);

            result = Geometry.LineSegmentIntersectsRect(new PointF(22, 2), new PointF(3, 3), rect);
            Assert.True(result);

            result = Geometry.LineSegmentIntersectsRect(new PointF(-1, 5), new PointF(3, 11), rect);
            Assert.True(result);

            result = Geometry.LineSegmentIntersectsRect(new PointF(3, -1), new PointF(6, 5), rect);
            Assert.True(result);

            result = Geometry.LineSegmentIntersectsRect(new PointF(3, -1), new PointF(7, -2), rect);
            Assert.False(result);

            result = Geometry.LineSegmentIntersectsRect(new PointF(-1, 11), new PointF(10, 11), rect);
            Assert.False(result);
        }
    }
}
