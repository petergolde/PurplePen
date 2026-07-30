// A value converter that returns either the original toolbar image or a 50% grayscale
// version of it, depending on whether the parent button is enabled or disabled.
// This mimics the automatic graying-out of disabled toolbar buttons in WinForms.
//
// Usage in AXAML:
//  <Image Width="16" Height="16"
//         Source="{Binding IsEffectivelyEnabled,
//                  RelativeSource={RelativeSource Self},
//                  Converter={x:Static avutil:DisabledImageConverter.Instance},
//                  ConverterParameter=/Assets/Toolbar/addBend.png}"/>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AvUtil
{
    /// <summary>
    /// Converts a boolean IsEnabled value into a Bitmap, loading from the asset path
    /// given in ConverterParameter. Returns the original image when enabled,
    /// or a cached grayscale version when disabled.
    /// </summary>
    public class DisabledImageConverter : IValueConverter
    {
        /// <summary>
        /// Singleton instance for use in AXAML via x:Static.
        /// </summary>
        public static readonly DisabledImageConverter Instance = new DisabledImageConverter();

        // Cache of loaded bitmaps: asset path -> (original, disabled).
        private readonly Dictionary<string, (Bitmap original, Bitmap disabled)> cache =
            new Dictionary<string, (Bitmap, Bitmap)>();

        /// <summary>
        /// Converts a boolean IsEnabled value to an IImage. The ConverterParameter must
        /// be the asset path string (e.g. "/Assets/Toolbar/addBend.png").
        /// Returns the original bitmap when true (enabled), grayscale when false (disabled).
        /// </summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (parameter is not string assetPath)
                return null;

            bool isEnabled = (value is bool b) && b;

            if (!cache.TryGetValue(assetPath, out (Bitmap original, Bitmap disabled) entry))
            {
                Uri uri = new Uri("avares://PurplePen" + assetPath);
                Bitmap original = new Bitmap(AssetLoader.Open(uri));
                Bitmap disabled = CreateDisabledGrayscale(original);
                entry = (original, disabled);
                cache[assetPath] = entry;
            }

            return isEnabled ? entry.original : entry.disabled;
        }

        /// <summary>
        /// Not supported (one-way converter).
        /// </summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Creates a grayscale copy of the given bitmap using ITU-R BT.601 luminance weights,
        /// and 50% intensity blending toward white.
        /// </summary>
        private static Bitmap CreateDisabledGrayscale(Bitmap original)
        {
            PixelSize size = original.PixelSize;
            WriteableBitmap grayscale = new WriteableBitmap(
                size, original.Dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);

            using (ILockedFramebuffer fb = grayscale.Lock())
            {
                // Copy the original pixels into the writable bitmap.
                original.CopyPixels(new PixelRect(size), fb.Address, fb.RowBytes * size.Height, fb.RowBytes);

                // Read pixels into a managed array for grayscale conversion.
                int byteCount = fb.RowBytes * size.Height;
                byte[] pixels = new byte[byteCount];
                Marshal.Copy(fb.Address, pixels, 0, byteCount);

                // Convert each pixel to grayscale (BGRA byte order, premultiplied alpha).
                for (int i = 0; i < byteCount; i += 4)
                {
                    byte bVal = pixels[i];
                    byte gVal = pixels[i + 1];
                    byte rVal = pixels[i + 2];
                    byte alpha = pixels[i + 3];
                    // ITU-R BT.601 luminance: 0.299*R + 0.587*G + 0.114*B
                    byte gray = (byte)((rVal * 77 + gVal * 150 + bVal * 29) >> 8);
                    // Blend gray toward white at 50% to reduce intensity.
                    // In premultiplied alpha, white is (alpha, alpha, alpha), not (255, 255, 255).
                    gray = (byte)((gray + alpha) >> 1);
                    pixels[i] = gray;
                    pixels[i + 1] = gray;
                    pixels[i + 2] = gray;
                    // Alpha (pixels[i + 3]) is unchanged.
                }

                Marshal.Copy(pixels, 0, fb.Address, byteCount);
            }

            return grayscale;
        }
    }
}
