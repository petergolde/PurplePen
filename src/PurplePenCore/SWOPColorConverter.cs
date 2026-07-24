using PurplePen.Graphics2D;
using PurplePen.MapModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;

namespace PurplePen
{
    // Converts CMYK to RGB using the SWOP color profile. This is a singleton class, and the instance is available via SwopColorConverter.Instance.
    // It uses a dictionary to make it fast with repeated colors, because that happens many times.
    public class SwopColorConverter: IColorConverter
    {
        public static SwopColorConverter Instance { get { return instance; } }
        private static SwopColorConverter instance = new SwopColorConverter();

        private Dictionary<CmykColor, Color> cmykToColor = new Dictionary<CmykColor, Color>();
        private LookupTableColorConverter lookupConverter;

        public SwopColorConverter()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream("PurplePen.Resources.swopsamples.dat")) {
                if (stream == null) {
                    throw new InvalidOperationException("Could not find the embedded resource.");
                }

                lookupConverter = new LookupTableColorConverter(stream);
            }
        }

        public Color CmykToRgbColor(CmykColor cmykColor)
        {
            Color result;

            if (cmykColor.Cyan == 0 && cmykColor.Magenta == 0 && cmykColor.Yellow == 0 && cmykColor.Black == 0) {
                // Map to pure white, just in case the lookup mapper doesn't.
                if (cmykColor.Alpha == 1)
                    return Color.White;
                else
                    return Color.FromArgb((byte) Math.Round(cmykColor.Alpha * 255), Color.White);
            }

            if (cmykColor.Black > 0.999F) {
                // The default mapping doesn't quite map black to pure black. It only maps all components 1 to pure black,
                // but I want anything with Black==1 to be pure black.
                if (cmykColor.Alpha == 1)
                    return Color.Black;
                else
                    return Color.FromArgb((byte)Math.Round(cmykColor.Alpha * 255), Color.Black);
            }

            if (!cmykToColor.TryGetValue(cmykColor, out result)) {
                Color rgb = lookupConverter.Convert(cmykColor.Cyan, cmykColor.Magenta, cmykColor.Yellow, cmykColor.Black);
                result = Color.FromArgb((byte) Math.Round(cmykColor.Alpha * 255), rgb.R, rgb.G, rgb.B);

                lock (cmykToColor) {
                    cmykToColor[cmykColor] = result;
                }
            }

            return result;
        }

        public Color ToColor(CmykColor cmykColor)
        {
            return CmykToRgbColor(cmykColor);
        }
    }

    /// <summary>
    /// Converts CMYK to RGB from a serialized lookup table without using LCMS.
    /// See LCMSLookup in the tools directory for a tool to generate the lookup table from an ICC profile.
    /// </summary>
    public sealed class LookupTableColorConverter 
    {
        private readonly byte[] rgbValues;

        public LookupTableColorConverter(Stream input)
        {
            ArgumentNullException.ThrowIfNull(input);
            if (!input.CanRead)
                throw new ArgumentException("The input stream must be readable.", nameof(input));

            using var reader = new BinaryReader(input, Encoding.UTF8, leaveOpen: true);

            byte[] magic = reader.ReadBytes(ColorLookupTableFormat.Magic.Length);
            if (!magic.AsSpan().SequenceEqual(ColorLookupTableFormat.Magic))
                throw new InvalidDataException("The stream is not a CMYK-to-RGB lookup table.");

            int version = reader.ReadInt32();
            if (version != ColorLookupTableFormat.Version)
                throw new InvalidDataException($"Unsupported lookup-table version {version}.");

            NumSamples = reader.ReadInt32();

            try {
                ColorLookupTableFormat.ValidateNumSamples(NumSamples);
            }
            catch (ArgumentOutOfRangeException exception) {
                throw new InvalidDataException("The lookup-table header contains invalid parameters.", exception);
            }

            long expectedEntryCount = ColorLookupTableFormat.GetEntryCount(NumSamples);
            long storedEntryCount = reader.ReadInt64();
            if (storedEntryCount != expectedEntryCount)
                throw new InvalidDataException("The lookup-table entry count does not match its dimensions.");

            int byteCount = checked((int)(expectedEntryCount * 3));
            rgbValues = reader.ReadBytes(byteCount);
            if (rgbValues.Length != byteCount)
                throw new EndOfStreamException("The lookup table ended before all RGB samples were read.");
        }

        public int NumSamples { get; }

        public Color Convert(double cyan, double magenta, double yellow, double black)
        {
            ValidateChannel(cyan, nameof(cyan));
            ValidateChannel(magenta, nameof(magenta));
            ValidateChannel(yellow, nameof(yellow));
            ValidateChannel(black, nameof(black));

            GetAxisPosition(cyan, out int cyanLower, out double cyanFraction);
            GetAxisPosition(magenta, out int magentaLower, out double magentaFraction);
            GetAxisPosition(yellow, out int yellowLower, out double yellowFraction);
            GetAxisPosition(black, out int blackLower, out double blackFraction);

            double red = 0.0;
            double green = 0.0;
            double blue = 0.0;

            // Four-dimensional multilinear interpolation has 2^4 = 16 corners.
            // Each corner's contribution is the product of its interpolation weight
            // along the C, M, Y, and K axes.
            for (int cyanCorner = 0; cyanCorner <= 1; cyanCorner++) {
                double cyanWeight = GetCornerWeight(cyanFraction, cyanCorner);

                for (int magentaCorner = 0; magentaCorner <= 1; magentaCorner++) {
                    double magentaWeight = GetCornerWeight(magentaFraction, magentaCorner);

                    for (int yellowCorner = 0; yellowCorner <= 1; yellowCorner++) {
                        double yellowWeight = GetCornerWeight(yellowFraction, yellowCorner);

                        for (int blackCorner = 0; blackCorner <= 1; blackCorner++) {
                            double blackWeight = GetCornerWeight(blackFraction, blackCorner);
                            double weight = cyanWeight * magentaWeight * yellowWeight * blackWeight;

                            int byteIndex = GetByteIndex(
                                cyanLower + cyanCorner,
                                magentaLower + magentaCorner,
                                yellowLower + yellowCorner,
                                blackLower + blackCorner);

                            red += rgbValues[byteIndex] * weight;
                            green += rgbValues[byteIndex + 1] * weight;
                            blue += rgbValues[byteIndex + 2] * weight;
                        }
                    }
                }
            }

            return Color.FromArgb(RoundToByte(red), RoundToByte(green), RoundToByte(blue));
        }

        private void GetAxisPosition(double channel, out int lowerIndex, out double fraction)
        {
            double tablePosition = channel * (NumSamples - 1);

            lowerIndex = (int)Math.Floor(tablePosition);
            if (lowerIndex >= NumSamples - 1) {
                // At channel == 1, use the last interval with full weight on its
                // upper endpoint rather than indexing one element beyond the table.
                lowerIndex = NumSamples - 2;
                fraction = 1.0;
            }
            else {
                fraction = tablePosition - lowerIndex;
            }
        }

        private int GetByteIndex(int cyan, int magenta, int yellow, int black)
        {
            int entryIndex =
                (((cyan * NumSamples) + magenta) * NumSamples + yellow) * NumSamples + black;

            return entryIndex * 3;
        }

        private static double GetCornerWeight(double fraction, int corner) =>
            corner == 0 ? 1.0 - fraction : fraction;

        private static int RoundToByte(double value) =>
            Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 0, 255);

        private static void ValidateChannel(double value, string parameterName)
        {
            if (!double.IsFinite(value) || value < 0.0 || value > 1.0)
                throw new ArgumentOutOfRangeException(parameterName, value, "CMYK channels must be between 0 and 1.");
        }
    }

    /// <summary>
    /// Constants and validation shared by the table writer and reader.
    /// </summary>
    internal static class ColorLookupTableFormat
    {
        // "CMYKRGB" makes an invalid or unrelated stream easy to recognize.
        public static ReadOnlySpan<byte> Magic => "CMYKRGB"u8;

        public const int Version = 2;

        public static void ValidateNumSamples(int numSamples)
        {
            if (numSamples < 2)
                throw new ArgumentOutOfRangeException(nameof(numSamples), numSamples, "At least two samples per axis are required.");

            // The reader stores three bytes for each point in one managed array.
            // Reject table dimensions that cannot be represented by that array.
            long entryCount = GetEntryCount(numSamples);
            if (entryCount > int.MaxValue / 3)
                throw new ArgumentOutOfRangeException(nameof(numSamples), numSamples, "The resulting lookup table is too large.");
        }

        public static long GetEntryCount(int numSamples)
        {
            try {
                return checked((long)numSamples * numSamples * numSamples * numSamples);
            }
            catch (OverflowException) {
                throw new ArgumentOutOfRangeException(
                    nameof(numSamples),
                    numSamples,
                    "The resulting lookup table is too large.");
            }
        }
    }



}
