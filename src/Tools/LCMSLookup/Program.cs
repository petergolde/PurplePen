using System.Drawing;

namespace LCMSLookup;

internal static class Program
{
    private const int NUMTESTS = 500000;
    private const string ProfileFileName = "GRACoL2013UNC_CRPC3.icc";


    private static int Main()
    {
        string profilePath = Path.Combine(AppContext.BaseDirectory, ProfileFileName);
        using var iccConverter = new CmykToSrgbConverter(profilePath);

        int numSamples = 11; // About 44K lookup table, gives good results.
        var generator = new ColorLookupTableGenerator(iccConverter, numSamples); 

        using (FileStream outputStream = new FileStream("swopsamples.dat", FileMode.Create, FileAccess.Write)) {
            generator.Write(outputStream);
        }

        return 0;
    }

    private static int RunExperiment()
    {
        string profilePath = Path.Combine(AppContext.BaseDirectory, ProfileFileName);

        try
        {
            using var iccConverter = new CmykToSrgbConverter(profilePath);

            for (int numSamples = 5; numSamples <= 17; numSamples++)
            {
                var generator = new ColorLookupTableGenerator(iccConverter, numSamples);
                using var tableStream = new MemoryStream();

                generator.Write(tableStream);
                tableStream.Position = 0;

                var lookupConverter = new LookupTableColorConverter(tableStream);
                Console.WriteLine($"NUMSAMPLES={numSamples}  BYTES={numSamples*numSamples*numSamples*numSamples*3}");
                RunConverterTest(
                    iccConverter,
                    ProfileFileName,
                    lookupConverter,
                    $"Lookup table NUMSAMPLES={numSamples}",
                    NUMTESTS);
            }

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Error: {exception.Message}");
            return 1;
        }
    }

    private static void RunConverterTest(
        IColorConverter firstConverter,
        string firstName,
        IColorConverter secondConverter,
        string secondName,
        int numberOfTests)
    {
        ArgumentNullException.ThrowIfNull(firstConverter);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentNullException.ThrowIfNull(secondConverter);
        ArgumentException.ThrowIfNullOrWhiteSpace(secondName);

        if (numberOfTests <= 0)
            throw new ArgumentOutOfRangeException(nameof(numberOfTests), "The number of tests must be greater than zero.");

        // Restarting with the same seed makes every table configuration use
        // identical CMYK inputs, so differences between runs reflect the table
        // configuration rather than a different random sample.
        var random = new Random(12345);
        var deltaEValues = new double[numberOfTests];

        double largestCyan = 0.0;
        double largestMagenta = 0.0;
        double largestYellow = 0.0;
        double largestBlack = 0.0;
        Color largestFirstColor = default;
        Color largestSecondColor = default;
        double largestDeltaE = double.NegativeInfinity;

        for (int test = 0; test < numberOfTests; test++)
        {
            double cyan = random.NextDouble();
            double magenta = random.NextDouble();
            double yellow = random.NextDouble();
            double black = random.NextDouble();

            Color firstColor = firstConverter.Convert(cyan, magenta, yellow, black);
            Color secondColor = secondConverter.Convert(cyan, magenta, yellow, black);
            double deltaE = CalculateDeltaE76(firstColor, secondColor);
            deltaEValues[test] = deltaE;

            if (deltaE > largestDeltaE)
            {
                largestDeltaE = deltaE;
                largestCyan = cyan;
                largestMagenta = magenta;
                largestYellow = yellow;
                largestBlack = black;
                largestFirstColor = firstColor;
                largestSecondColor = secondColor;
            }
        }

        double[] sortedDeltaEValues = [.. deltaEValues];
        Array.Sort(sortedDeltaEValues);

        Console.WriteLine($"{firstName} vs. {secondName} ({numberOfTests:N0} tests)");
        Console.WriteLine($"Mean ΔE76: {deltaEValues.Average():F3}");
        Console.WriteLine($"Maximum ΔE76: {sortedDeltaEValues[^1]:F3}");
        Console.WriteLine($"Minimum ΔE76: {sortedDeltaEValues[0]:F3}");
        Console.WriteLine($"95th percentile ΔE76: {CalculatePercentile(sortedDeltaEValues, 0.95):F3}");
        Console.WriteLine($"98th percentile ΔE76: {CalculatePercentile(sortedDeltaEValues, 0.98):F3}");
        Console.WriteLine($"99th percentile ΔE76: {CalculatePercentile(sortedDeltaEValues, 0.99):F3}");
        Console.WriteLine($"99.9th percentile ΔE76: {CalculatePercentile(sortedDeltaEValues, 0.999):F3}");
        Console.WriteLine("Largest-ΔE76 sample:");
        Console.WriteLine(
            $"  CMYK ({largestCyan:F6}, {largestMagenta:F6}, {largestYellow:F6}, {largestBlack:F6})");
        Console.WriteLine(
            $"  {firstName}: RGB ({largestFirstColor.R}, {largestFirstColor.G}, {largestFirstColor.B})");
        Console.WriteLine(
            $"  {secondName}: RGB ({largestSecondColor.R}, {largestSecondColor.G}, {largestSecondColor.B})");
        Console.WriteLine($"  ΔE76: {largestDeltaE:F3}");
        Console.WriteLine();
    }

    private static double CalculatePercentile(double[] sortedValues, double percentile)
    {
        if (sortedValues.Length == 0)
            throw new ArgumentException("At least one value is required.", nameof(sortedValues));

        if (percentile < 0.0 || percentile > 1.0)
            throw new ArgumentOutOfRangeException(nameof(percentile), "Percentile must be between zero and one.");

        // Place the requested percentile on the zero-based range [0, count - 1].
        // When it falls between two observations, interpolate linearly between
        // them rather than abruptly selecting either the lower or upper value.
        double position = percentile * (sortedValues.Length - 1);
        int lowerIndex = (int)Math.Floor(position);
        int upperIndex = (int)Math.Ceiling(position);
        double fraction = position - lowerIndex;

        return sortedValues[lowerIndex] +
            (sortedValues[upperIndex] - sortedValues[lowerIndex]) * fraction;
    }

    private static double CalculateDeltaE76(Color first, Color second)
    {
        LabColor firstLab = ConvertSrgbToLab(first);
        LabColor secondLab = ConvertSrgbToLab(second);

        double lightnessDifference = firstLab.Lightness - secondLab.Lightness;
        double aDifference = firstLab.A - secondLab.A;
        double bDifference = firstLab.B - secondLab.B;

        return Math.Sqrt(
            lightnessDifference * lightnessDifference +
            aDifference * aDifference +
            bDifference * bDifference);
    }

    private static LabColor ConvertSrgbToLab(Color color)
    {
        // Decode the nonlinear sRGB channels to linear-light RGB.
        double red = DecodeSrgbChannel(color.R / 255.0);
        double green = DecodeSrgbChannel(color.G / 255.0);
        double blue = DecodeSrgbChannel(color.B / 255.0);

        // Convert linear sRGB to CIE XYZ using the sRGB D65 matrix.
        double x = 0.4124564 * red + 0.3575761 * green + 0.1804375 * blue;
        double y = 0.2126729 * red + 0.7151522 * green + 0.0721750 * blue;
        double z = 0.0193339 * red + 0.1191920 * green + 0.9503041 * blue;

        // Normalize XYZ by the D65 reference white before applying the Lab
        // transfer function. The resulting Lab values share one white point,
        // so their Euclidean distance is the CIE 1976 color difference ΔE76.
        double xFunction = ConvertXyzComponentToLab(x / 0.95047);
        double yFunction = ConvertXyzComponentToLab(y);
        double zFunction = ConvertXyzComponentToLab(z / 1.08883);

        return new LabColor(
            116.0 * yFunction - 16.0,
            500.0 * (xFunction - yFunction),
            200.0 * (yFunction - zFunction));
    }

    private static double DecodeSrgbChannel(double channel) =>
        channel <= 0.04045
            ? channel / 12.92
            : Math.Pow((channel + 0.055) / 1.055, 2.4);

    private static double ConvertXyzComponentToLab(double component)
    {
        const double epsilon = 216.0 / 24389.0;
        const double kappa = 24389.0 / 27.0;

        return component > epsilon
            ? Math.Cbrt(component)
            : (kappa * component + 16.0) / 116.0;
    }

    private readonly record struct LabColor(double Lightness, double A, double B);
}
