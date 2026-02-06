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
using NUnit.Framework;

namespace PurplePen.MapModel.Tests
{
    using System.IO;
    using PurplePen.Graphics2D;

    [TestFixture]
    public class MiscTests
    {
        static MiscTests()
        {
            // Make sure that the code page providers are loaded.
            Encoding encoding = Util.GetDefaultOcadEncoding();
        }

        [Test]
        public void TestCompression()
        {
            byte[] bytes = { 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x0, 0x0, 0x0, 0x1B, 0x1E,
								  0x45, 0x32, 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x0, 0x0, 0x0, 0x1B, 0x1E,
								  0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x45, 0x0, 0x0, 0x0, 0x1B, 0x0};

            byte[] compressed = new byte[bytes.Length];
            byte[] decompressed = new byte[bytes.Length];

            LZWCompression comp = new LZWCompression();
            comp.Compress(bytes, compressed);
            comp.Expand(compressed, decompressed);

            for (int i = 0; i < bytes.Length; ++i)
            {
                Assert.IsTrue(bytes[i] == decompressed[i]);
            }
        }

        [Test]
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

        [Test]
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

        [Test]
        public void TextCoordMapper1()
        {
            TextCoordMapper mapper = new TextCoordMapper();
            string[] text = { "hi there", "hello mom" };
            string[] wrappedText = { "hi ", "there", "hello ", "mom" };

            mapper.AddUnwrappedCoord(new TextCoord(0, 0));
            mapper.AddUnwrappedCoord(new TextCoord(0, 3));
            mapper.AddUnwrappedCoord(new TextCoord(1, 0));
            mapper.AddUnwrappedCoord(new TextCoord(1, 6));

            TextCoord result;
            result = mapper.WrappedFromUnwrapped(new TextCoord(0, 0), text, wrappedText);
            Assert.AreEqual(new TextCoord(0, 0), result);

            result = mapper.WrappedFromUnwrapped(new TextCoord(0, 2), text, wrappedText);
            Assert.AreEqual(new TextCoord(0, 2), result);

            result = mapper.WrappedFromUnwrapped(new TextCoord(0, 3), text, wrappedText);
            Assert.AreEqual(new TextCoord(1, 0), result);

            result = mapper.WrappedFromUnwrapped(new TextCoord(0, 5), text, wrappedText);
            Assert.AreEqual(new TextCoord(1, 2), result);

            result = mapper.WrappedFromUnwrapped(new TextCoord(1, 0), text, wrappedText);
            Assert.AreEqual(new TextCoord(2, 0), result);

            result = mapper.WrappedFromUnwrapped(new TextCoord(1, 6), text, wrappedText);
            Assert.AreEqual(new TextCoord(3, 0), result);

            result = mapper.WrappedFromUnwrapped(new TextCoord(1, 9), text, wrappedText);
            Assert.AreEqual(new TextCoord(3, 3), result);


            result = mapper.UnwrappedFromWrapped(new TextCoord(0, 0), text, wrappedText);
            Assert.AreEqual(new TextCoord(0, 0), result);

            result = mapper.UnwrappedFromWrapped(new TextCoord(0, 2), text, wrappedText);
            Assert.AreEqual(new TextCoord(0, 2), result);

            result = mapper.UnwrappedFromWrapped(new TextCoord(1, 0), text, wrappedText);
            Assert.AreEqual(new TextCoord(0, 3), result);

            result = mapper.UnwrappedFromWrapped(new TextCoord(1, 2), text, wrappedText);
            Assert.AreEqual(new TextCoord(0, 5), result);

            result = mapper.UnwrappedFromWrapped(new TextCoord(2, 0), text, wrappedText);
            Assert.AreEqual(new TextCoord(1, 0), result);

            result = mapper.UnwrappedFromWrapped(new TextCoord(3, 0), text, wrappedText);
            Assert.AreEqual(new TextCoord(1, 6), result);

            result = mapper.UnwrappedFromWrapped(new TextCoord(3, 3), text, wrappedText);
            Assert.AreEqual(new TextCoord(1, 9), result);

        }

        public void CheckWriteDelphiString(int codepage, string s, int n)
        {
            Encoding encoding = Encoding.GetEncoding(codepage);
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, encoding);

            writer.Write((byte)37);
            writer.Write((byte)213);
            Util.WriteDelphiString(writer, s, n);
            writer.Write((byte)89);
            writer.Write((byte)243);
            
            writer.Flush();
            byte[] bytes = stream.GetBuffer();
            Assert.AreEqual(stream.Position, n + 5);

            Assert.AreEqual((byte)37, bytes[0]);
            Assert.AreEqual((byte)213, bytes[1]);
            Assert.AreEqual((byte)89, bytes[3+n]);
            Assert.AreEqual((byte)243, bytes[4+n]);

            byte[] bytesFromEncoding = new byte[0];
            string sub = "";
            for (int c = s.Length; c >= 0; --c) {
                sub = s.Substring(0, c);
                bytesFromEncoding = encoding.GetBytes(sub);
                if (bytesFromEncoding.Length <= n)
                    break;
            }

            Assert.AreEqual((byte)bytesFromEncoding.Length, bytes[2]);
            for (int i = 0; i < bytesFromEncoding.Length; ++i) {
                Assert.AreEqual(bytesFromEncoding[i], bytes[i + 3]);
            }
            for (int i = bytesFromEncoding.Length; i < n; ++i) {
                Assert.AreEqual(0, bytes[i + 3]);
            }

            FastBinaryReader fbr = new FastBinaryReader(bytes, encoding);
            fbr.Seek(2, SeekOrigin.Begin);
            string result = Util.ReadDelphiString(fbr, n);
            Assert.AreEqual(n + 3, fbr.Seek(0, SeekOrigin.Current));
            Assert.AreEqual(sub, result);
        }


        [Test]
        public void WriteDelphiString()
        {
            // Make sure that DBCS and SBCS code pages work.

            CheckWriteDelphiString(1252, "hello", 7);
            CheckWriteDelphiString(1252, "hello", 6);
            CheckWriteDelphiString(1252, "hello", 5);
            CheckWriteDelphiString(1252, "hello", 4);
            CheckWriteDelphiString(1252, "hello", 3);
            CheckWriteDelphiString(1252, "hello", 2);
            CheckWriteDelphiString(1252, "hello", 1);

            string msMincho = "\uFF2D\uFF33 \u30B4\u30b7\u30c3\u30af";
            CheckWriteDelphiString(932, msMincho, 16);
            CheckWriteDelphiString(932, msMincho, 15);
            CheckWriteDelphiString(932, msMincho, 14);
            CheckWriteDelphiString(932, msMincho, 13);
            CheckWriteDelphiString(932, msMincho, 12);
            CheckWriteDelphiString(932, msMincho, 11);
            CheckWriteDelphiString(932, msMincho, 10);
            CheckWriteDelphiString(932, msMincho, 9);
            CheckWriteDelphiString(932, msMincho, 8);
            CheckWriteDelphiString(932, msMincho, 7);
            CheckWriteDelphiString(932, msMincho, 6);
            CheckWriteDelphiString(932, msMincho, 5);
            CheckWriteDelphiString(932, msMincho, 4);
            CheckWriteDelphiString(932, msMincho, 3);
            CheckWriteDelphiString(932, msMincho, 2);
            CheckWriteDelphiString(932, msMincho, 1);

        }
    }
}

#endif //TEST
