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
using System.Drawing.Drawing2D;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PurplePen.MapModel.Tests
{
    [TestClass]
    public class MiscTests 
    {
        [TestMethod]
        public void TestCompression()
        {
            byte[] bytes = new byte[] { 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x0, 0x0, 0x0, 0x1B, 0x1E,
								  0x45, 0x32, 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x0, 0x0, 0x0, 0x1B, 0x1E,
								  0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x0, 0x0, 0x0, 0x1B, 0x0};

            byte[] compressed = new byte[bytes.Length];
            byte[] decompressed = new byte[bytes.Length];

            LZWCompression comp = new LZWCompression();
            comp.Compress(bytes, compressed);
            comp.Expand(compressed, decompressed);

            for (int i = 0; i < bytes.Length; ++i) {
                Assert.IsTrue(bytes[i] == decompressed[i]);
            }
        }

        [TestMethod]
        public void SigDigits()
        {
            int dec;
            Assert.AreEqual(45000.0, Util.RoundToSignificant(44768.0, 2, out dec));
            Assert.AreEqual(0, dec);
            Assert.AreEqual(-45000.0, Util.RoundToSignificant(-44768.0, 2, out dec));
            Assert.AreEqual(0, dec);
            Assert.AreEqual(0.0, Util.RoundToSignificant(0, 2, out dec));
            Assert.AreEqual(0, dec);
            Assert.AreEqual(3.14, Util.RoundToSignificant(3.141592, 3, out dec));
            Assert.AreEqual(2, dec);
            Assert.AreEqual(-0.000453, Util.RoundToSignificant(-0.00045287634, 3, out dec));
            Assert.AreEqual(6, dec);
            Assert.AreEqual(-1.456E14, Util.RoundToSignificant(-1.4557893E14, 4, out dec));
            Assert.AreEqual(0, dec);
        }

        [TestMethod]
        public void FormatSuffix()
        {
            Assert.AreEqual("0 m", Util.FormatNumberWithSuffix(0.0, 2, null, "mm", "m", "km", null));
            Assert.AreEqual("560 m", Util.FormatNumberWithSuffix(557.24, 2, null, "mm", "m", "km", null));
            Assert.AreEqual("5.6 m", Util.FormatNumberWithSuffix(5.5724, 2, null, "mm", "m", "km", null));
            Assert.AreEqual("-56 km", Util.FormatNumberWithSuffix(-55724.234, 2, null, "mm", "m", "km", null));
            Assert.AreEqual("560,000 km", Util.FormatNumberWithSuffix(557247123, 2, null, "mm", "m", "km", null));
            Assert.AreEqual("540 mm", Util.FormatNumberWithSuffix(0.54325, 2, null, "mm", "m", "km", null));
            Assert.AreEqual("0.0054 mm", Util.FormatNumberWithSuffix(0.0000054325, 2, null, "mm", "m", "km", null));

            Assert.AreEqual("0 m2", Util.FormatNumberWithSuffix(0.0, 2, "mm2", null, "m2", null, "km2"));
            Assert.AreEqual("990 m2", Util.FormatNumberWithSuffix(987.32, 2, "mm2", null, "m2", null, "km2"));
            Assert.AreEqual("990 km2", Util.FormatNumberWithSuffix(987877123.9, 2, "mm2", null, "m2", null, "km2"));
            Assert.AreEqual("9,880 km2", Util.FormatNumberWithSuffix(9878771243.9, 3, "mm2", null, "m2", null, "km2"));
            Assert.AreEqual("0.034 m2", Util.FormatNumberWithSuffix(0.034114, 2, null, null, "m2", null, "km2"));
            Assert.AreEqual("34,000 mm2", Util.FormatNumberWithSuffix(0.034114, 2, "mm2", null, "m2", null, "km2"));

        }

        [TestMethod]
        public void TransformInvertedRectangle()
        {
            RectangleF source = new RectangleF(3, 4, 7, 8);
            RectangleF dest = new RectangleF(-1, 5, 19, 5.5F);

            Matrix m = Util.TransformInvertedRectangle(source, dest);

            PointF[] pts = new PointF[] { source.Location, new PointF(source.Left, source.Bottom), new PointF(source.Right, source.Top) };
            m.TransformPoints(pts);

            Assert.AreEqual(dest.Left, pts[0].X, 0.0001F);
            Assert.AreEqual(dest.Bottom, pts[0].Y, 0.0001F);
            Assert.AreEqual(dest.Left, pts[1].X, 0.0001F);
            Assert.AreEqual(dest.Top, pts[1].Y, 0.0001F);
            Assert.AreEqual(dest.Right, pts[2].X, 0.0001F);
            Assert.AreEqual(dest.Bottom, pts[2].Y, 0.0001F);
        }
    }

}

#endif //TEST
