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
using System.Drawing;

using PurplePen.Graphics2D;
using PurplePen.MapModel;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.Tests
{

    [TestClass]
    public class MoveAllComputationsTests
    {
        [TestMethod]
        public void Move()
        {
            MoveAllComputations computations = new MoveAllComputations(MoveAllControlsAction.Move, new PointF[] { new PointF(3, 4), new PointF(2, 2) });
            Assert.AreEqual(-1, computations.XOffset);
            Assert.AreEqual(-2, computations.YOffset);
            Assert.AreEqual(0, computations.Rotation);
            Assert.AreEqual(1.0, computations.Scale);

            Matrix matrix = computations.Matrix;
            PointF[] points = new PointF[] { new PointF(3, 4), new PointF(0, 0), new PointF(-4, -4) };
            matrix.TransformPoints(points);
            Assert.AreEqual(new PointF(2, 2), points[0]);
            Assert.AreEqual(new PointF(-1, -2), points[1]);
            Assert.AreEqual(new PointF(-5, -6), points[2]);
        }

        [TestMethod]
        public void MoveAndScale()
        {
            MoveAllComputations computations = new MoveAllComputations(MoveAllControlsAction.MoveScale, new PointF[] { new PointF(3, 4), new PointF(2, 2), new PointF(4, 8), new PointF(5, 11) });
            Assert.AreEqual(-1, computations.XOffset);
            Assert.AreEqual(-2, computations.YOffset);
            Assert.AreEqual(0, computations.Rotation);
            Assert.AreEqual(1.5, computations.Scale);

            Matrix matrix = computations.Matrix;
            PointF[] points = new PointF[] { new PointF(3, 4), new PointF(0, 0), new PointF(-4, -4), new PointF(5, 10) };
            matrix.TransformPoints(points);
            Assert.AreEqual(new PointF(2, 2), points[0]);
            Assert.AreEqual(new PointF(-2.5F, -4), points[1]);
            Assert.AreEqual(new PointF(-8.5F, -10), points[2]);
            Assert.AreEqual(new PointF(5, 11), points[3]);
        }

        [TestMethod]
        public void MoveAndRotate()
        {
            MoveAllComputations computations = new MoveAllComputations(MoveAllControlsAction.MoveRotate, new PointF[] { new PointF(3, 4), new PointF(2, 2), new PointF(4, 8), new PointF(8, 5) });
            Assert.AreEqual(-1, computations.XOffset);
            Assert.AreEqual(-2, computations.YOffset);
            Assert.AreEqual(-45, computations.Rotation, 0.001);
            Assert.AreEqual(1.0, computations.Scale);

            Matrix matrix = computations.Matrix;
            PointF[] points = new PointF[] { new PointF(3, 4), new PointF(0, 0), new PointF(-4, -4) };
            matrix.TransformPoints(points);
            Assert.AreEqual(2, points[0].X, 0.001);
            Assert.AreEqual(2, points[0].Y, 0.001);
            Assert.AreEqual(-2.9497F, points[1].X, 0.001);
            Assert.AreEqual(1.2929F, points[1].Y, 0.001);
            Assert.AreEqual(-8.6066, points[2].X, 0.001);
            Assert.AreEqual(1.2929F, points[2].Y, 0.001);
        }

        [TestMethod]
        public void MoveRotateScale()
        {
            MoveAllComputations computations = new MoveAllComputations(MoveAllControlsAction.MoveRotateScale, new PointF[] { new PointF(3, 4), new PointF(2, 2), new PointF(4, 8), new PointF(10, 9) });
            Assert.AreEqual(-1, computations.XOffset);
            Assert.AreEqual(-2, computations.YOffset);
            Assert.AreEqual(-30.3791, computations.Rotation, 0.001);
            Assert.AreEqual(1.68077, computations.Scale, 0.001);

            Matrix matrix = computations.Matrix;
            PointF[] points = new PointF[] { new PointF(3, 4), new PointF(0, 0), new PointF(-4, -4) };
            matrix.TransformPoints(points);
            Assert.AreEqual(2, points[0].X, 0.001);
            Assert.AreEqual(2, points[0].Y, 0.001);
            Assert.AreEqual(-5.75F, points[1].X, 0.001);
            Assert.AreEqual(-1.25F, points[1].Y, 0.001);
            Assert.AreEqual(-14.95, points[2].X, 0.001);
            Assert.AreEqual(-3.65F, points[2].Y, 0.001);
        }

        [TestMethod]
        public void BadScale()
        {
            MoveAllComputations computations = new MoveAllComputations(MoveAllControlsAction.MoveScale, new PointF[] { new PointF(3, 4), new PointF(2, 2), new PointF(2, 2), new PointF(5, 11) });
            Assert.AreEqual(-1, computations.XOffset);
            Assert.AreEqual(-2, computations.YOffset);
            Assert.AreEqual(0, computations.Rotation);
            Assert.AreEqual(1.0, computations.Scale);

            Matrix matrix = computations.Matrix;
            PointF[] points = new PointF[] { new PointF(3, 4), new PointF(0, 0), new PointF(-4, -4) };
            matrix.TransformPoints(points);
            Assert.AreEqual(new PointF(2, 2), points[0]);
            Assert.AreEqual(new PointF(-1, -2), points[1]);
            Assert.AreEqual(new PointF(-5, -6), points[2]);

            computations = new MoveAllComputations(MoveAllControlsAction.MoveScale, new PointF[] { new PointF(3, 4), new PointF(2, 2), new PointF(5, 11), new PointF(2, 2) });
            Assert.AreEqual(-1, computations.XOffset);
            Assert.AreEqual(-2, computations.YOffset);
            Assert.AreEqual(0, computations.Rotation);
            Assert.AreEqual(1.0, computations.Scale);

            matrix = computations.Matrix;
            points = new PointF[] { new PointF(3, 4), new PointF(0, 0), new PointF(-4, -4) };
            matrix.TransformPoints(points);
            Assert.AreEqual(new PointF(2, 2), points[0]);
            Assert.AreEqual(new PointF(-1, -2), points[1]);
            Assert.AreEqual(new PointF(-5, -6), points[2]);

        }

        [TestMethod]
        public void BadRotation()
        {
            MoveAllComputations computations = new MoveAllComputations(MoveAllControlsAction.MoveRotate, new PointF[] { new PointF(3, 4), new PointF(2, 2), new PointF(2, 2), new PointF(5, 11) });
            Assert.AreEqual(-1, computations.XOffset);
            Assert.AreEqual(-2, computations.YOffset);
            Assert.AreEqual(0, computations.Rotation);
            Assert.AreEqual(1.0, computations.Scale);

            Matrix matrix = computations.Matrix;
            PointF[] points = new PointF[] { new PointF(3, 4), new PointF(0, 0), new PointF(-4, -4) };
            matrix.TransformPoints(points);
            Assert.AreEqual(new PointF(2, 2), points[0]);
            Assert.AreEqual(new PointF(-1, -2), points[1]);
            Assert.AreEqual(new PointF(-5, -6), points[2]);

            computations = new MoveAllComputations(MoveAllControlsAction.MoveRotate, new PointF[] { new PointF(3, 4), new PointF(2, 2), new PointF(5, 11), new PointF(2, 2) });
            Assert.AreEqual(-1, computations.XOffset);
            Assert.AreEqual(-2, computations.YOffset);
            Assert.AreEqual(0, computations.Rotation);
            Assert.AreEqual(1.0, computations.Scale);

            matrix = computations.Matrix;
            points = new PointF[] { new PointF(3, 4), new PointF(0, 0), new PointF(-4, -4) };
            matrix.TransformPoints(points);
            Assert.AreEqual(new PointF(2, 2), points[0]);
            Assert.AreEqual(new PointF(-1, -2), points[1]);
            Assert.AreEqual(new PointF(-5, -6), points[2]);

        }

        [TestMethod]
        public void BadRotationScale()
        {
            MoveAllComputations computations = new MoveAllComputations(MoveAllControlsAction.MoveRotateScale, new PointF[] { new PointF(3, 4), new PointF(2, 2), new PointF(2, 2), new PointF(5, 11) });
            Assert.AreEqual(-1, computations.XOffset);
            Assert.AreEqual(-2, computations.YOffset);
            Assert.AreEqual(0, computations.Rotation);
            Assert.AreEqual(1.0, computations.Scale);

            Matrix matrix = computations.Matrix;
            PointF[] points = new PointF[] { new PointF(3, 4), new PointF(0, 0), new PointF(-4, -4) };
            matrix.TransformPoints(points);
            Assert.AreEqual(new PointF(2, 2), points[0]);
            Assert.AreEqual(new PointF(-1, -2), points[1]);
            Assert.AreEqual(new PointF(-5, -6), points[2]);

            computations = new MoveAllComputations(MoveAllControlsAction.MoveRotateScale, new PointF[] { new PointF(3, 4), new PointF(2, 2), new PointF(5, 11), new PointF(2, 2) });
            Assert.AreEqual(-1, computations.XOffset);
            Assert.AreEqual(-2, computations.YOffset);
            Assert.AreEqual(0, computations.Rotation);
            Assert.AreEqual(1.0, computations.Scale);

            matrix = computations.Matrix;
            points = new PointF[] { new PointF(3, 4), new PointF(0, 0), new PointF(-4, -4) };
            matrix.TransformPoints(points);
            Assert.AreEqual(new PointF(2, 2), points[0]);
            Assert.AreEqual(new PointF(-1, -2), points[1]);
            Assert.AreEqual(new PointF(-5, -6), points[2]);

        }


    }
}
#endif //TEST
