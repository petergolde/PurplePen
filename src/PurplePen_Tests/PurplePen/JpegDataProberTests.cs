using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using TestingUtils;

namespace PurplePen.Tests
{
    [TestClass]
    public class JpegDataProberTests: TestFixtureBase
    {
        private const byte MarkerStartOfFrameBaseline = 0xC0;
        private const byte MarkerStartOfFrameProgressive = 0xC2;

        // Builds a JPEG out of the given segments: a start-of-image marker, the segments,
        // and a start-of-scan marker to end the headers. No image data follows, which is
        // all the prober needs and none of what it reads.
        //
        // Parameters:
        //   segments - the marker segments, each complete with its FF byte and length.
        private static byte[] Jpeg(params byte[][] segments)
        {
            List<byte> bytes = new List<byte>() { 0xFF, 0xD8 };

            foreach (byte[] segment in segments)
                bytes.AddRange(segment);

            bytes.AddRange(new byte[] { 0xFF, 0xDA, 0x00, 0x02 });
            return bytes.ToArray();
        }

        // Builds a start-of-frame segment of the given kind, describing a 16 by 16 image
        // with the given number of colour channels.
        //
        // Parameters:
        //   marker - the frame marker, which says how the image is coded.
        //   components - the number of colour channels.
        private static byte[] StartOfFrame(byte marker, int components)
        {
            int length = 8 + 3 * components;
            List<byte> segment = new List<byte>() {
                0xFF, marker, (byte)(length >> 8), (byte)length,
                8, 0, 16, 0, 16, (byte)components
            };

            for (int i = 0; i < components; ++i)
                segment.AddRange(new byte[] { (byte)(i + 1), 0x11, 0 });

            return segment.ToArray();
        }

        // Builds an APP1 segment holding an Exif directory whose only entry is the given
        // orientation.
        //
        // Parameters:
        //   orientation - the value of the orientation tag.
        private static byte[] ExifOrientation(int orientation)
        {
            List<byte> tiff = new List<byte>() {
                (byte)'M', (byte)'M', 0, 42,        // big-endian, and the magic number
                0, 0, 0, 8,                         // the directory follows the header
                0, 1,                               // one entry
                0x01, 0x12,                         // the orientation tag
                0, 3,                               // held as a short
                0, 0, 0, 1,                         // one of them
                (byte)(orientation >> 8), (byte)orientation, 0, 0,
                0, 0, 0, 0                          // no further directory
            };

            List<byte> content = new List<byte>() { (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0, 0 };
            content.AddRange(tiff);

            int length = content.Count + 2;
            List<byte> segment = new List<byte>() { 0xFF, 0xE1, (byte)(length >> 8), (byte)length };
            segment.AddRange(content);
            return segment.ToArray();
        }

        [TestMethod]
        public void BaselineThreeChannels()
        {
            Assert.IsTrue(JpegDataProber.IsPlain(Jpeg(StartOfFrame(MarkerStartOfFrameBaseline, 3))));
        }

        [TestMethod]
        public void FourChannelsIsNotPlain()
        {
            Assert.IsFalse(JpegDataProber.IsPlain(Jpeg(StartOfFrame(MarkerStartOfFrameBaseline, 4))));
        }

        [TestMethod]
        public void ProgressiveIsNotPlain()
        {
            Assert.IsFalse(JpegDataProber.IsPlain(Jpeg(StartOfFrame(MarkerStartOfFrameProgressive, 3))));
        }

        [TestMethod]
        public void UprightOrientationIsPlain()
        {
            Assert.IsTrue(JpegDataProber.IsPlain(
                Jpeg(ExifOrientation(1), StartOfFrame(MarkerStartOfFrameBaseline, 3))));
        }

        [TestMethod]
        public void TurnedOrientationIsNotPlain()
        {
            Assert.IsFalse(JpegDataProber.IsPlain(
                Jpeg(ExifOrientation(6), StartOfFrame(MarkerStartOfFrameBaseline, 3))));
        }

        [TestMethod]
        public void OtherApp1SegmentIsIgnored()
        {
            byte[] notExif = new byte[] { 0xFF, 0xE1, 0x00, 0x08, (byte)'h', (byte)'t', (byte)'t', (byte)'p', 0, 0 };
            Assert.IsTrue(JpegDataProber.IsPlain(
                Jpeg(notExif, StartOfFrame(MarkerStartOfFrameBaseline, 3))));
        }

        [TestMethod]
        public void NothingToReadIsNotPlain()
        {
            Assert.IsFalse(JpegDataProber.IsPlain(null));
            Assert.IsFalse(JpegDataProber.IsPlain(new byte[0]));
            Assert.IsFalse(JpegDataProber.IsPlain(new byte[] { 0xFF, 0xD8 }));
            Assert.IsFalse(JpegDataProber.IsPlain(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' }));
        }

        [TestMethod]
        public void CutShortIsNotPlain()
        {
            byte[] whole = Jpeg(StartOfFrame(MarkerStartOfFrameBaseline, 3));
            byte[] cut = new byte[whole.Length - 6];
            Array.Copy(whole, cut, cut.Length);

            Assert.IsFalse(JpegDataProber.IsPlain(cut));
        }

        [TestMethod]
        public void RealPhotograph()
        {
            byte[] data = File.ReadAllBytes(TestUtil.GetTestFile("coursesymbols\\mrsneeze.jpg"));
            Assert.IsTrue(JpegDataProber.IsPlain(data));
        }
    }
}
