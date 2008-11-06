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
    public class LegGapTests
    {

        [TestMethod]
        public void SplitPathWithGaps()
        {
            PointF[] points = new PointF[] {
				new PointF(0,0), new PointF(10,10), new PointF(20,40), new PointF(-10, 10),
				new PointF(20,20), new PointF(25, 30), new PointF(25,30), new PointF(40,25) };
            PointKind[] kinds = new PointKind[] {
				PointKind.Normal, PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
				PointKind.Normal, PointKind.Normal, PointKind.Normal, PointKind.Normal };
            SymPath p = new SymPath(points, kinds);

            LegGap[] gapStartStops = new LegGap[] { new LegGap(-2, 3), new LegGap(15, 7), new LegGap(25, 1.5F), new LegGap(300, 5) };

            SymPath[] results = LegGap.SplitPathWithGaps(p, gapStartStops);

            foreach (SymPath path in results)
                Console.WriteLine(path);

            Assert.AreEqual(@"N(0.71,0.71)--N(10,10)--B(10.09,10.28)--B(10.18,10.56)--N(10.27,10.83)", results[0].ToString());
            Assert.AreEqual(@"N(11.95,17.61)--B(12.12,18.8)--B(12.17,19.79)--N(12.12,20.6)", results[1].ToString());
            Assert.AreEqual(@"N(11.88,22.07)--B(9.81,28.87)--B(-3.49,12.17)--N(20,20)--N(25,30)--N(25,30)--N(40,25)", results[2].ToString());
        }

        [TestMethod]
        public void SimplifyGaps()
        {
            LegGap[] gaps = new LegGap[] { new LegGap(7, 8), new LegGap(5, 0), new LegGap(25, 5), new LegGap(-7, 2), new LegGap(-1, 3), new LegGap(8.1F, 2.2F), new LegGap(100, 100), new LegGap(8, 1.4F), new LegGap(14, 8.5F), new LegGap(3, 0.5F) };
            LegGap[] expected = new LegGap[] { new LegGap(0, 2), new LegGap(3, 0.5F), new LegGap(7, 15.5F), new LegGap(25, 2) };

            LegGap[] result = LegGap.SimplifyGaps(gaps, 27);
            Assert.AreEqual(expected.Length, result.Length);
            for (int i = 0; i < result.Length; ++i)
                Assert.IsTrue(expected[i] == result[i]);
        }

        [TestMethod]
        public void MoveGapsToNewPath()
        {
            SymPath oldPath = new SymPath(new PointF[] { new PointF(0, 0), new PointF(10, 10) });
            LegGap[] oldGaps = new LegGap[] { new LegGap(1, 2), new LegGap(7, 0.5F), new LegGap(12, 2) };
            SymPath newPath = new SymPath(new PointF[] { new PointF(10, 10), new PointF(10, 0), new PointF(0, 0) });
            LegGap[] expected = new LegGap[] { new LegGap(0.1F, 1.414F), new LegGap(17.88F, 1.414F) };

            LegGap[] newGaps = LegGap.MoveGapsToNewPath(oldGaps, oldPath, newPath);

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
            LegGap[] gaps = new LegGap[] { new LegGap(1, 2), new LegGap(7, 0.5F), new LegGap(12, 2) };
            PointF[] expected = new PointF[] { new PointF(10, 9), new PointF(10, 7), new PointF(10, 3), new PointF(10, 2.5F), new PointF(8, 0), new PointF(6, 0) };

            PointF[] result = LegGap.GapStartStopPoints(path, gaps);
            TestUtil.TestEnumerableAnyOrder(result, expected);
        }

        [TestMethod]
        public void MoveStartStopPoint()
        {
            SymPath path = new SymPath(new PointF[] { new PointF(10, 10), new PointF(10, 0), new PointF(0, 0) });
            LegGap[] gaps = new LegGap[] { new LegGap(1, 2), new LegGap(7, 0.5F), new LegGap(12, 2) };

            LegGap[] result = LegGap.MoveStartStopPoint(path, gaps, new PointF(8, 0), new PointF(8.5F, 0));
            LegGap[] expected = new LegGap[] { new LegGap(1, 2), new LegGap(7, 0.5F), new LegGap(11.5F, 2.5F) };
            TestUtil.TestEnumerableAnyOrder(result, expected);

            result = LegGap.MoveStartStopPoint(path, gaps, new PointF(10, 2.5F), new PointF(8,2));
            expected = new LegGap[] { new LegGap(1, 2), new LegGap(7, 1F), new LegGap(12, 2) };
            TestUtil.TestEnumerableAnyOrder(result, expected);
        }

        [TestMethod]
        public void AddGap1()
        {
            // Add a gap to a null gap array.
            SymPath path = new SymPath(new PointF[] { new PointF(10, 10), new PointF(10, 0), new PointF(0, 0) });
            PointF pt1 = new PointF(7, -2);
            PointF pt2 = new PointF(11, 4);

            LegGap[] gaps = LegGap.AddGap(path, null, pt1, pt2);
            Assert.AreEqual(1, gaps.Length);
            Assert.AreEqual(6, gaps[0].distanceFromStart);
            Assert.AreEqual(7, gaps[0].length);
            
        }

        [TestMethod]
        public void AddGap2()
        {
            // Add a gap to an existing array.
            SymPath path = new SymPath(new PointF[] { new PointF(10, 10), new PointF(10, 0), new PointF(0, 0) });
            LegGap[] oldGaps = { new LegGap(6, 7) };
            PointF pt1 = new PointF(8, -2);
            PointF pt2 = new PointF(11, 8.5F);

            LegGap[] gaps = LegGap.AddGap(path, oldGaps, pt1, pt2);
            Assert.AreEqual(1, gaps.Length);
            Assert.AreEqual(1.5F, gaps[0].distanceFromStart);
            Assert.AreEqual(11.5F, gaps[0].length);

            pt1 = new PointF(3, -2);
            pt2 = new PointF(1.5F, 1);
            gaps = LegGap.AddGap(path, oldGaps, pt1, pt2);
            Assert.AreEqual(2, gaps.Length);
            Assert.AreEqual(6, gaps[0].distanceFromStart);
            Assert.AreEqual(7, gaps[0].length);
            Assert.AreEqual(17F, gaps[1].distanceFromStart);
            Assert.AreEqual(1.5F, gaps[1].length);
        }
	
	

    }
}

#endif //TEST
