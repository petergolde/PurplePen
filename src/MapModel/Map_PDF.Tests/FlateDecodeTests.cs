using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Filters;

namespace Map_PDF.Tests
{
    // Checks the complete zlib format consumed by PDF readers, independently of PDFsharp's tolerant decoder.
    [TestFixture]
    public class FlateDecodeTests
    {
        // Supplies each compression mode with empty, short, binary, and large inputs and known Adler-32 values.
        public static IEnumerable<TestCaseData> CompressionCases()
        {
            byte[] patterned = new byte[262144];
            for (int i = 0; i < patterned.Length; ++i)
                patterned[i] = (byte)((i * 73 + i / 251) & 255);

            byte[] large = new byte[300000];
            Array.Fill(large, (byte)255);

            // Expected checksums were independently calculated with Python's zlib.adler32.
            foreach (PdfFlateEncodeMode mode in Enum.GetValues<PdfFlateEncodeMode>()) {
                yield return new TestCaseData(mode, Array.Empty<byte>(), 0x00000001U).SetName($"ZlibStream_{mode}_Empty");
                yield return new TestCaseData(mode, Encoding.ASCII.GetBytes("Wikipedia"), 0x11E60398U).SetName($"ZlibStream_{mode}_Text");
                yield return new TestCaseData(mode, new byte[] { 255 }, 0x01000100U).SetName($"ZlibStream_{mode}_SingleByte");
                yield return new TestCaseData(mode, patterned, 0xF0281BFDU).SetName($"ZlibStream_{mode}_Binary");
                yield return new TestCaseData(mode, large, 0x00A29082U).SetName($"ZlibStream_{mode}_Large");
            }
        }

        // Verifies the header, finalized checksum, and standard zlib decoding for the given mode and input.
        [TestCaseSource(nameof(CompressionCases))]
        public void EncodeCompleteZlibStream(PdfFlateEncodeMode mode, byte[] input, uint expectedChecksum)
        {
            byte[] encoded = new FlateDecode().Encode(input, mode);
            Assert.That(encoded.Length, Is.GreaterThanOrEqualTo(8), "A complete empty zlib stream needs a header, DEFLATE block, and checksum.");
            Assert.That(encoded[0], Is.EqualTo(0x78), "DEFLATE with a 32 KB window.");
            int expectedFlags = mode == PdfFlateEncodeMode.BestSpeed ? 0x01 :
                                mode == PdfFlateEncodeMode.BestCompression ? 0xDA : 0x9C;
            Assert.That(encoded[1], Is.EqualTo(expectedFlags), "The header must describe the requested compression level without a preset dictionary.");

            int offset = encoded.Length - 4;
            uint checksum = ((uint)encoded[offset] << 24) | ((uint)encoded[offset + 1] << 16) |
                            ((uint)encoded[offset + 2] << 8) | encoded[offset + 3];
            Assert.That(checksum, Is.EqualTo(expectedChecksum), "The final four bytes must be the big-endian Adler-32 of the uncompressed input.");

            using (MemoryStream compressed = new MemoryStream(encoded))
            using (ZLibStream decoder = new ZLibStream(compressed, CompressionMode.Decompress))
            using (MemoryStream decoded = new MemoryStream()) {
                decoder.CopyTo(decoded);
                Assert.That(decoded.ToArray(), Is.EqualTo(input));
            }

            // The existing reader must still accept the complete streams produced by the writer.
            Assert.That(new FlateDecode().Decode(encoded), Is.EqualTo(input));
        }

        // Ensures callers using Encode(data) receive the same complete default stream as the explicit overload.
        [Test]
        public void DefaultOverloadUsesCompleteZlibStream()
        {
            byte[] input = Encoding.ASCII.GetBytes("Wikipedia");
            FlateDecode filter = new FlateDecode();
            Assert.That(filter.Encode(input), Is.EqualTo(filter.Encode(input, PdfFlateEncodeMode.Default)));
        }
    }
}
