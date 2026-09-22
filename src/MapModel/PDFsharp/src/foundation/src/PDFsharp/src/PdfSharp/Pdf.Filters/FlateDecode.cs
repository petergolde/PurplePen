// PDFsharp - A .NET library for processing PDF
// See the LICENSE file in the solution root for more information.

using System.IO.Compression;

namespace PdfSharp.Pdf.Filters
{
    /// <summary>
    /// Implements the PDF FlateDecode filter using zlib-wrapped DEFLATE streams.
    /// </summary>
    public class FlateDecode : Filter
    {
        // Reference: 3.3.3  LZWDecode and FlateDecode Filters / Page 71

        /// <summary>
        /// Encodes the specified data as a complete zlib stream using the default compression mode.
        /// </summary>
        public override byte[] Encode(byte[] data)
        {
            return Encode(data, PdfFlateEncodeMode.Default);
        }

        /// <summary>
        /// Encodes the specified data with the requested compression mode, including the zlib header and Adler-32 trailer.
        /// </summary>
        public byte[] Encode(byte[] data, PdfFlateEncodeMode mode)
        {
            byte zlibFlags = mode == PdfFlateEncodeMode.BestSpeed ? (byte)0x01 :
                             mode == PdfFlateEncodeMode.BestCompression ? (byte)0xDA : (byte)0x9C;
            // .NET compression streams can emit nothing for a zero-length write.
            // Match SharpZipLib by emitting a final empty DEFLATE block and Adler-32 = 1.
            if (data.Length == 0)
                return new byte[] { 0x78, zlibFlags, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01 };

            using MemoryStream ms = new MemoryStream();

            CompressionLevel level;
            switch (mode)
            {
                case PdfFlateEncodeMode.BestCompression:
#if NET462 || NETSTANDARD2_0
                    level = CompressionLevel.Optimal;
#else
                    level = CompressionLevel.SmallestSize;
#endif
                    break;
                case PdfFlateEncodeMode.BestSpeed:
                    level = CompressionLevel.Fastest;
                    break;
                default:
                    level = CompressionLevel.Optimal;
                    break;
            }

#if NET462 || NETSTANDARD2_0
            // These targets have no ZLibStream. Supply both parts of the RFC 1950
            // wrapper around raw DEFLATE, just as the old SharpZipLib encoder did.
            ms.WriteByte(0x78);
            ms.WriteByte(zlibFlags);
            using (DeflateStream zip = new DeflateStream(ms, level, true))
            {
                zip.Write(data, 0, data.Length);
            }
            uint checksum = CalculateAdler32(data);
            ms.WriteByte((byte)(checksum >> 24));
            ms.WriteByte((byte)(checksum >> 16));
            ms.WriteByte((byte)(checksum >> 8));
            ms.WriteByte((byte)checksum);
#else
            // Purple Pen 3.5.5 used SharpZipLib with its zlib wrapper enabled.
            // ZLibStream likewise writes both the header and the Adler-32 trailer;
            // DeflateStream alone omits them. Finish before reading the buffer.
            using (ZLibStream zip = new ZLibStream(ms, level, true))
            {
                zip.Write(data, 0, data.Length);
            }
#endif
            return ms.ToArray();
        }

#if NET462 || NETSTANDARD2_0
        /// <summary>
        /// Calculates the RFC 1950 Adler-32 checksum of the uncompressed data for targets without ZLibStream.
        /// </summary>
        static uint CalculateAdler32(byte[] data)
        {
            const uint modulus = 65521;
            uint s1 = 1;
            uint s2 = 0;
            int offset = 0;
            while (offset < data.Length)
            {
                // At most 5552 bytes can be accumulated without overflowing either sum.
                int end = offset + Math.Min(5552, data.Length - offset);
                while (offset < end)
                {
                    s1 += data[offset++];
                    s2 += s1;
                }
                s1 %= modulus;
                s2 %= modulus;
            }
            return (s2 << 16) | s1;
        }
#endif

        /// <summary>
        /// Decodes the specified data.
        /// </summary>
        public override byte[] Decode(byte[] data, FilterParms? parms)
        {
            using var msInput = new MemoryStream(data);
            using var msOutput = new MemoryStream();

            // ReSharper disable once RedundantAssignment
            var header = new byte[]
            {
                (byte)msInput.ReadByte(), // CMF (Compression Method and flags)
                (byte)msInput.ReadByte()  // Flags
            };
#if true
            Debug.Assert((header[0] & 0xF) == 0x8); // Compression method must be deflate.
            Debug.Assert((header[1] & 0x20) == 0);  // DeflateStream does not support Adler32.
#endif

            using var zip = new DeflateStream(msInput, CompressionMode.Decompress, true);
            zip.CopyTo(msOutput);
            msOutput.Flush();

            if (msOutput.Length >= 0)
            {
                msOutput.Capacity = (int)msOutput.Length;
                if (parms?.DecodeParms != null)
                    return StreamDecoder.Decode(msOutput.GetBuffer(), parms.DecodeParms);
            }

            return msOutput.GetBuffer();
        }
    }
}
