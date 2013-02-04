/* Copyright (c) 2013, Peter Golde
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
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Drawing2D;

using PurplePen.MapModel;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.Tests
{

    [TestClass]
    public class CircleGapTests
    {
        [TestMethod]
        public void SimplifyGaps()
        {
            CircleGap[] gaps = new CircleGap[] { new CircleGap(125, 134), new CircleGap(290, 270), new CircleGap(125, 140), 
                new CircleGap(-40, -20), new CircleGap(180, 180), new CircleGap(-30, 45), new CircleGap(50, 100), 
                new CircleGap(-140, -120), new CircleGap(55, 65), new CircleGap(75, 102), new CircleGap(129, 130) };
            CircleGap[] expected = new CircleGap[] { new CircleGap(-40, 45), new CircleGap(50, 102), new CircleGap(125, 140), new CircleGap(220, 240) };

            CircleGap[] result = CircleGap.SimplifyGaps(gaps);
            Assert.AreEqual(expected.Length, result.Length);
            for (int i = 0; i < result.Length; ++i)
                Assert.IsTrue(expected[i] == result[i]);
        }

        [TestMethod]
        public void GapStartStopPoints()
        {
            CircleGap[] gaps = new CircleGap[] { new CircleGap(-45, 30), new CircleGap(60, 270), new CircleGap(290, 300) };
            PointF[] result = CircleGap.GapStartStopPoints(new PointF(7F, 13F), 10F, gaps);
            PointF[] expected = new PointF[] { new PointF((float) (10.0 * Math.Sqrt(2)/2 + 7), (float) (10.0 * -Math.Sqrt(2)/2 + 13)),
                                               new PointF((float) (10.0 * Math.Sqrt(3)/2 + 7), (float) (5 + 13)),
                                               new PointF((float) (5 + 7), (float) (10.0 * Math.Sqrt(3)/2 + 13)),
                                               new PointF((float) ( 7), (float) (-10 + 13)),
                                               new PointF((float) (10.0 * Math.Cos(2 * Math.PI * 290 / 360) + 7), (float) (10.0 * Math.Sin(2 * Math.PI * 290 / 360) + 13)),
                                               new PointF((float) (10.0 * Math.Cos(2 * Math.PI * 300 / 360) + 7), (float) (10.0 * Math.Sin(2 * Math.PI * 300 / 360) + 13)),
            };
            TestUtil.TestEnumerableAnyOrder(result, expected);

            result = CircleGap.GapStartStopPoints(new PointF(7F, 13F), 10F, null);
            Assert.IsNull(result);

            result = CircleGap.GapStartStopPoints(new PointF(7F, 13F), 10F, new CircleGap[0]);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void ArcStartSweeps()
        {
            CircleGap[] gaps = new CircleGap[] { new CircleGap(-45, 30), new CircleGap(55, 270), new CircleGap(290, 300) };
            float[] result = CircleGap.ArcStartSweeps(gaps);
            float[] expected = new float[] { 30F, 25F, 270F, 20F, 300F, 15F};
            
            Assert.AreEqual(expected.Length, result.Length);

            for (int i = 0; i < expected.Length; ++i) {
                Assert.AreEqual(expected[i], result[i]);
            }

            result = CircleGap.ArcStartSweeps(null);
            Assert.AreEqual(0F, result[0]);
            Assert.AreEqual(360F, result[1]);
        }

        [TestMethod]
        public void ComputeCircleGaps()
        {
            CircleGap[] result;

            result = CircleGap.ComputeCircleGaps(0xFFFFFFFF);
            Assert.IsNull(result);

            result = CircleGap.ComputeCircleGaps(0);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(new CircleGap(-360, 0), result[0]);

            result = CircleGap.ComputeCircleGaps(0x03FF0060);
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(new CircleGap(-67.5F, 56.25F), result[0]);
            Assert.AreEqual(new CircleGap(78.75F, 180.0F), result[1]);

            result = CircleGap.ComputeCircleGaps(0xF3F1006F);
            Assert.AreEqual(4, result.Length);
            Assert.AreEqual(new CircleGap(45F, 56.25F), result[0]);
            Assert.AreEqual(new CircleGap(78.75F, 180F), result[1]);
            Assert.AreEqual(new CircleGap(191.25F, 225F), result[2]);
            Assert.AreEqual(new CircleGap(292.5F, 315.0F), result[3]);
        
        }

#if false
        [TestMethod]
        public void MoveGapsToNewPath()
        {
            SymPath oldPath = new SymPath(new PointF[] { new PointF(0, 0), new PointF(10, 10) });
            CircleGap[] oldGaps = new CircleGap[] { new CircleGap(1, 2), new CircleGap(7, 0.5F), new CircleGap(12, 2) };
            SymPath newPath = new SymPath(new PointF[] { new PointF(10, 10), new PointF(10, 0), new PointF(0, 0) });
            CircleGap[] expected = new CircleGap[] { new CircleGap(0.1F, 1.414F), new CircleGap(17.88F, 1.414F) };

            CircleGap[] newGaps = CircleGap.MoveGapsToNewPath(oldGaps, oldPath, newPath);

            // Make sure result and expected match.
            Assert.AreEqual(expected.Length, newGaps.Length);
            for (int i = 0; i < newGaps.Length; ++i) {
                Assert.AreEqual(expected[i].distanceFromStart, newGaps[i].distanceFromStart, 0.01F);
                Assert.AreEqual(expected[i].length, newGaps[i].length, 0.01F);
            }
        }

        [TestMethod]
        public void GapStartStopPoints()
        {
            SymPath path = new SymPath(new PointF[] { new PointF(10, 10), new PointF(10, 0), new PointF(0, 0) });
            CircleGap[] gaps = new CircleGap[] { new CircleGap(1, 2), new CircleGap(7, 0.5F), new CircleGap(12, 2) };
            PointF[] expected = new PointF[] { new PointF(10, 9), new PointF(10, 7), new PointF(10, 3), new PointF(10, 2.5F), new PointF(8, 0), new PointF(6, 0) };

            PointF[] result = CircleGap.GapStartStopPoints(path, gaps);
            TestUtil.TestEnumerableAnyOrder(result, expected);
        }

        [TestMethod]
        public void MoveStartStopPoint()
        {
            SymPath path = new SymPath(new PointF[] { new PointF(10, 10), new PointF(10, 0), new PointF(0, 0) });
            CircleGap[] gaps = new CircleGap[] { new CircleGap(1, 2), new CircleGap(7, 0.5F), new CircleGap(12, 2) };

            CircleGap[] result = CircleGap.MoveStartStopPoint(path, gaps, new PointF(8, 0), new PointF(8.5F, 0));
            CircleGap[] expected = new CircleGap[] { new CircleGap(1, 2), new CircleGap(7, 0.5F), new CircleGap(11.5F, 2.5F) };
            TestUtil.TestEnumerableAnyOrder(result, expected);

            result = CircleGap.MoveStartStopPoint(path, gaps, new PointF(10, 2.5F), new PointF(8, 2));
            expected = new CircleGap[] { new CircleGap(1, 2), new CircleGap(7, 1F), new CircleGap(12, 2) };
            TestUtil.TestEnumerableAnyOrder(result, expected);
        }

        [TestMethod]
        public void AddGap1()
        {
            // Add a gap to a null gap array.
            SymPath path = new SymPath(new PointF[] { new PointF(10, 10), new PointF(10, 0), new PointF(0, 0) });
            PointF pt1 = new PointF(7, -2);
            PointF pt2 = new PointF(11, 4);

            CircleGap[] gaps = CircleGap.AddGap(path, null, pt1, pt2);
            Assert.AreEqual(1, gaps.Length);
            Assert.AreEqual(6, gaps[0].distanceFromStart);
            Assert.AreEqual(7, gaps[0].length);

        }

        [TestMethod]
        public void AddGap2()
        {
            // Add a gap to an existing array.
            SymPath path = new SymPath(new PointF[] { new PointF(10, 10), new PointF(10, 0), new PointF(0, 0) });
            CircleGap[] oldGaps = { new CircleGap(6, 7) };
            PointF pt1 = new PointF(8, -2);
            PointF pt2 = new PointF(11, 8.5F);

            CircleGap[] gaps = CircleGap.AddGap(path, oldGaps, pt1, pt2);
            Assert.AreEqual(1, gaps.Length);
            Assert.AreEqual(1.5F, gaps[0].distanceFromStart);
            Assert.AreEqual(11.5F, gaps[0].length);

            pt1 = new PointF(3, -2);
            pt2 = new PointF(1.5F, 1);
            gaps = CircleGap.AddGap(path, oldGaps, pt1, pt2);
            Assert.AreEqual(2, gaps.Length);
            Assert.AreEqual(6, gaps[0].distanceFromStart);
            Assert.AreEqual(7, gaps[0].length);
            Assert.AreEqual(17F, gaps[1].distanceFromStart);
            Assert.AreEqual(1.5F, gaps[1].length);
        }

#endif


    }
}

#endif //TEST
