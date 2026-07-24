using System.Drawing;
using System.Runtime.InteropServices;
using lcmsNET;

namespace LCMSLookup;

/// <summary>
/// Converts normalized CMYK values through an ICC profile to 8-bit sRGB.
/// </summary>
public sealed class CmykToSrgbConverter : IColorConverter, IDisposable
{
    private readonly Profile cmykProfile;
    private readonly Profile srgbProfile;
    private readonly Transform transform;
    private bool disposed;

    public CmykToSrgbConverter(string colorProfilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(colorProfilePath);

        string fullProfilePath = Path.GetFullPath(colorProfilePath);
        if (!File.Exists(fullProfilePath))
            throw new FileNotFoundException("The CMYK ICC color profile was not found.", fullProfilePath);

        Profile? openedCmykProfile = null;
        Profile? createdSrgbProfile = null;

        try
        {
            openedCmykProfile = Profile.Open(fullProfilePath, "r");
            if (openedCmykProfile.ColorSpace != ColorSpaceSignature.CmykData)
                throw new ArgumentException("The supplied ICC profile is not a CMYK profile.", nameof(colorProfilePath));

            createdSrgbProfile = Profile.Create_sRGB(null!);
            transform = Transform.Create(
                openedCmykProfile,
                Cms.TYPE_CMYK_DBL,
                createdSrgbProfile,
                Cms.TYPE_RGB_8,
                Intent.RelativeColorimetric,
                CmsFlags.BlackPointCompensation);

            cmykProfile = openedCmykProfile;
            srgbProfile = createdSrgbProfile;
        }
        catch
        {
            createdSrgbProfile?.Dispose();
            openedCmykProfile?.Dispose();
            throw;
        }
    }

    public Color Convert(float cyan, float magenta, float yellow, float black) =>
        Convert((double)cyan, magenta, yellow, black);

    public Color Convert(double cyan, double magenta, double yellow, double black)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        ValidateChannel(cyan, nameof(cyan));
        ValidateChannel(magenta, nameof(magenta));
        ValidateChannel(yellow, nameof(yellow));
        ValidateChannel(black, nameof(black));

        // LCMS represents floating-point CMYK channels as percentages, even though
        // this class deliberately exposes the more convenient normalized 0..1 range.
        Span<double> cmyk = stackalloc double[]
        {
            cyan * 100.0,
            magenta * 100.0,
            yellow * 100.0,
            black * 100.0
        };
        Span<byte> rgb = stackalloc byte[3];

        transform.DoTransform(MemoryMarshal.AsBytes(cmyk), rgb, 1);
        return Color.FromArgb(rgb[0], rgb[1], rgb[2]);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        transform.Dispose();
        srgbProfile.Dispose();
        cmykProfile.Dispose();
        disposed = true;
    }

    private static void ValidateChannel(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0.0 || value > 1.0)
            throw new ArgumentOutOfRangeException(parameterName, value, "CMYK channels must be between 0 and 1.");
    }
}
