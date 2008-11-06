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

#if TEST
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PurplePen.MapModel.Tests
{
    [TestClass]
    public class BezierTests
    {
        private Bezier b1, b2;

        [TestInitialize]
        public void Initialize()
        {
            b1 = new Bezier(new PointF(0, 0), new PointF(0, 1), new PointF(1, 2), new PointF(2, 2));
            b2 = new Bezier(new PointF(0, 0), new PointF(1.4F, 0.5F), new PointF(1.0f, 0.2f), new PointF(2, 0));
        }

        [TestMethod]
        public void InQuad()
        {
            bool b;

            b = b1.IsPointInQuad(new PointF(0.9F, 1.1F), 0);
            Assert.IsTrue(b);

            b = b1.IsPointInQuad(new PointF(1.1F, 0.9F), 0);
            Assert.IsTrue(!b);

            b = b1.IsPointInQuad(new PointF(0.1F, 1.2F), 0);
            Assert.IsTrue(!b);

            b = b1.IsPointInQuad(new PointF(0.1F, 1.05F), 0);
            Assert.IsTrue(b);

            b = b1.IsPointInQuad(new PointF(2.1F, 2F), 0);
            Assert.IsTrue(!b);

            b = b1.IsPointInQuad(new PointF(2.1F, 2F), 0.11F);
            Assert.IsTrue(b);

            b = b2.IsPointInQuad(new PointF(1.05F, 0.25F), 0);
            Assert.IsTrue(b);
        }

        [TestMethod]
        public void Split()
        {
            Bezier bez1, bez2;
            b1.SplitAtCoefficient(0.5F, out bez1, out bez2);
            Assert.AreEqual(0F, bez1.end1.X, 0.0001F);
            Assert.AreEqual(0F, bez1.end1.Y, 0.0001F);
            Assert.AreEqual(0F, bez1.control1.X, 0.0001F);
            Assert.AreEqual(1.0F, bez1.control2.Y, 0.0001F);
            Assert.AreEqual(0.25F, bez1.control2.X, 0.0001F);
            Assert.AreEqual(1.0F, bez1.control2.Y, 0.0001F);
            Assert.AreEqual(0.625F, bez1.end2.X, 0.0001F);
            Assert.AreEqual(1.375F, bez1.end2.Y, 0.0001F);

            Assert.AreEqual(0.625F, bez2.end1.X, 0.0001F);
            Assert.AreEqual(1.375F, bez2.end1.Y, 0.0001F);
            Assert.AreEqual(1.0F, bez2.control1.X, 0.0001F);
            Assert.AreEqual(2.0F, bez2.control2.Y, 0.0001F);
            Assert.AreEqual(1.5F, bez2.control2.X, 0.0001F);
            Assert.AreEqual(2.0F, bez2.control2.Y, 0.0001F);
            Assert.AreEqual(2.0F, bez2.end2.X, 0.0001F);
            Assert.AreEqual(2.0F, bez2.end2.Y, 0.0001F);
        }

        [TestMethod]
        public void FindCoefficient()
        {
            Bezier b2, b3;
            PointF point = new PointF(1.26F, 1.83F);
            float t = b1.FindCoefficient(point, 0.01F);
            Assert.AreEqual(0.749F, t, 0.005F);
            b1.SplitAtCoefficient(t, out b2, out b3);
            Assert.AreEqual(b2.end2.X, point.X, 0.01F);
            Assert.AreEqual(b2.end2.Y, point.Y, 0.01F);
            Assert.AreEqual(b3.end1.X, point.X, 0.01F);
            Assert.AreEqual(b3.end1.Y, point.Y, 0.01F);

            point = new PointF(0.05F, 0.37F);
            t = b1.FindCoefficient(point, 0.01F);
            b1.SplitAtCoefficient(t, out b2, out b3);
            Assert.AreEqual(0.124F, t, 0.005F);
            Assert.AreEqual(b2.end2.X, point.X, 0.01F);
            Assert.AreEqual(b2.end2.Y, point.Y, 0.01F);
            Assert.AreEqual(b3.end1.X, point.X, 0.01F);
            Assert.AreEqual(b3.end1.Y, point.Y, 0.01F);
        }
    }

}

#endif //TEST
