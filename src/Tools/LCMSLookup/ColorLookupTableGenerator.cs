using System.Drawing;
using System.Text;

namespace LCMSLookup;

/// <summary>
/// Samples an <see cref="IColorConverter"/> onto a uniformly spaced
/// four-dimensional CMYK lattice and serializes the resulting RGB lookup table.
/// </summary>
public sealed class ColorLookupTableGenerator
{
    private readonly IColorConverter sourceConverter;

    public ColorLookupTableGenerator(IColorConverter sourceConverter, int numSamples)
    {
        ArgumentNullException.ThrowIfNull(sourceConverter);
        ColorLookupTableFormat.ValidateNumSamples(numSamples);

        this.sourceConverter = sourceConverter;
        NumSamples = numSamples;
    }

    /// <summary>
    /// Gets the number of samples along each of the C, M, Y, and K axes.
    /// The complete table contains <c>NumSamples^4</c> RGB entries.
    /// </summary>
    public int NumSamples { get; }

    /// <summary>
    /// Writes the lookup table at the stream's current position.
    /// The stream remains open after this method returns.
    /// </summary>
    public void Write(Stream output)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (!output.CanWrite)
            throw new ArgumentException("The output stream must be writable.", nameof(output));

        long entryCount = ColorLookupTableFormat.GetEntryCount(NumSamples);
        double[] axisValues = CreateAxisValues();

        // Binary layout:
        //   magic bytes, format version, samples per axis, entry count,
        //   then one R/G/B byte triplet per lattice point.
        //
        // K changes fastest, followed by Y, M, and C. LookupTableColorConverter
        // uses the same ordering when flattening four indices into the byte array.
        using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);
        writer.Write(ColorLookupTableFormat.Magic);
        writer.Write(ColorLookupTableFormat.Version);
        writer.Write(NumSamples);
        writer.Write(entryCount);

        for (int cyanIndex = 0; cyanIndex < NumSamples; cyanIndex++)
        {
            for (int magentaIndex = 0; magentaIndex < NumSamples; magentaIndex++)
            {
                for (int yellowIndex = 0; yellowIndex < NumSamples; yellowIndex++)
                {
                    for (int blackIndex = 0; blackIndex < NumSamples; blackIndex++)
                    {
                        Color color = sourceConverter.Convert(
                            axisValues[cyanIndex],
                            axisValues[magentaIndex],
                            axisValues[yellowIndex],
                            axisValues[blackIndex]);

                        writer.Write(color.R);
                        writer.Write(color.G);
                        writer.Write(color.B);
                    }
                }
            }
        }
    }

    private double[] CreateAxisValues()
    {
        var values = new double[NumSamples];

        for (int index = 0; index < values.Length; index++)
            values[index] = (double)index / (NumSamples - 1);

        return values;
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
        try
        {
            return checked((long)numSamples * numSamples * numSamples * numSamples);
        }
        catch (OverflowException)
        {
            throw new ArgumentOutOfRangeException(
                nameof(numSamples),
                numSamples,
                "The resulting lookup table is too large.");
        }
    }
}
