using System;

namespace PurplePen
{
    // Reads the headers in JPEG data.
    public static class JpegDataProber
    {
        private const int TagOrientation = 0x0112;
        private const int TypeShort = 3;
        private const int OrientationUpright = 1;

        // Returns true if the data is a baseline JPEG of three colour channels that
        // declares no orientation other than the upright one. Four channels mean CMYK
        // or YCCK; the other frame types are progressive, lossless or arithmetically
        // coded. Data that cannot be read as a JPEG at all, or that is cut short,
        // returns false.
        //
        // Parameters:
        //   data - the encoded image, or null.
        public static bool IsPlain(byte[] data)
        {
            if (data == null || data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8)
                return false;

            bool baselineWithThreeChannels = false;
            int offset = 2;

            while (offset + 3 < data.Length) {
                if (data[offset] != 0xFF)
                    return false;

                int marker = data[offset + 1];

                // Segments may be separated by any number of fill bytes.
                if (marker == 0xFF) {
                    offset += 1;
                    continue;
                }

                // The scan has begun, or the image has ended; nothing further describes
                // the image as a whole.
                if (marker == 0xDA || marker == 0xD9)
                    return baselineWithThreeChannels;

                int length = (data[offset + 2] << 8) | data[offset + 3];
                if (length < 2 || offset + 2 + length > data.Length)
                    return false;

                int contentOffset = offset + 4;
                int contentLength = length - 2;

                if (marker == 0xC0) {
                    // Start of frame, baseline. Precision, height and width fill the
                    // first five bytes; the number of components follows them.
                    if (contentLength < 6)
                        return false;
                    baselineWithThreeChannels = (data[contentOffset + 5] == 3);
                }
                else if (IsStartOfFrame(marker)) {
                    // Extended, progressive, lossless or arithmetically coded.
                    return false;
                }
                else if (marker == 0xE1 && !IsUpright(data, contentOffset, contentLength)) {
                    return false;
                }

                offset += 2 + length;
            }

            return false;
        }

        // Returns true if the marker begins a frame. The run from C0 to CF is frame
        // headers apart from three markers that mean something else entirely.
        //
        // Parameters:
        //   marker - the byte following FF.
        private static bool IsStartOfFrame(int marker)
        {
            if (marker < 0xC0 || marker > 0xCF)
                return false;

            return marker != 0xC4 && marker != 0xC8 && marker != 0xCC;
        }

        // Returns true if the given APP1 segment leaves the image upright: either it is
        // not an Exif segment, or it carries no orientation, or the orientation it
        // carries is the upright one. A segment that cannot be read returns false.
        //
        // Parameters:
        //   data - the whole file.
        //   offset - where the segment's content begins.
        //   length - the length of that content.
        private static bool IsUpright(byte[] data, int offset, int length)
        {
            byte[] exifHeader = new byte[] { (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0, 0 };
            if (length < exifHeader.Length)
                return true;

            for (int i = 0; i < exifHeader.Length; ++i) {
                if (data[offset + i] != exifHeader[i])
                    return true;
            }

            // The Exif header is followed by a TIFF file, whose own header names the
            // byte order the rest of it is written in.
            int tiff = offset + exifHeader.Length;
            int tiffLength = length - exifHeader.Length;
            if (tiffLength < 8)
                return false;

            bool bigEndian;
            if (data[tiff] == 'I' && data[tiff + 1] == 'I')
                bigEndian = false;
            else if (data[tiff] == 'M' && data[tiff + 1] == 'M')
                bigEndian = true;
            else
                return false;

            long directory = ReadUInt32(data, tiff + 4, bigEndian);
            if (directory < 8 || directory + 2 > tiffLength)
                return false;

            int entryCount = ReadUInt16(data, tiff + (int)directory, bigEndian);
            for (int i = 0; i < entryCount; ++i) {
                long entry = directory + 2 + 12 * i;
                if (entry + 12 > tiffLength)
                    return false;

                int at = tiff + (int)entry;
                if (ReadUInt16(data, at, bigEndian) != TagOrientation)
                    continue;

                if (ReadUInt16(data, at + 2, bigEndian) != TypeShort)
                    return false;

                return ReadUInt16(data, at + 8, bigEndian) == OrientationUpright;
            }

            return true;
        }

        // Reads a 16-bit value in the given byte order.
        private static int ReadUInt16(byte[] data, int offset, bool bigEndian)
        {
            return bigEndian ? (data[offset] << 8) | data[offset + 1]
                             : (data[offset + 1] << 8) | data[offset];
        }

        // Reads a 32-bit value in the given byte order.
        private static long ReadUInt32(byte[] data, int offset, bool bigEndian)
        {
            if (bigEndian) {
                return ((long)data[offset] << 24) | ((long)data[offset + 1] << 16) |
                       ((long)data[offset + 2] << 8) | data[offset + 3];
            }

            return ((long)data[offset + 3] << 24) | ((long)data[offset + 2] << 16) |
                   ((long)data[offset + 1] << 8) | data[offset];
        }
    }
}
