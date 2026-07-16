/* Copyright (c) 2026, Peter Golde
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

using System;
using System.IO;
using System.Runtime.InteropServices;
using PurplePen.Graphics2D;
using SkiaSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Quantization;

namespace PurplePen.MapModel
{
    /// <summary>
    /// Holds an SKBitmap along with its file format and resolution (DPI) metadata.
    /// </summary>
    public class BitmapWithResolution
    {
        /// <summary>The bitmap pixel data.</summary>
        public SKBitmap Bitmap { get; set; }

        /// <summary>The bitmap file format (PNG, JPEG, GIF, etc.).</summary>
        public GraphicsBitmapFormat Format { get; set; }

        /// <summary>Horizontal resolution in dots per inch.</summary>
        public double HorizontalResolution { get; set; }

        /// <summary>Vertical resolution in dots per inch.</summary>
        public double VerticalResolution { get; set; }

        /// <summary>
        /// Creates a new BitmapWithResolution.
        /// </summary>
        /// <param name="bitmap">The bitmap pixel data.</param>
        /// <param name="format">The bitmap file format.</param>
        /// <param name="horizontalResolution">Horizontal resolution in DPI.</param>
        /// <param name="verticalResolution">Vertical resolution in DPI.</param>
        public BitmapWithResolution(SKBitmap bitmap, GraphicsBitmapFormat format, double horizontalResolution, double verticalResolution)
        {
            Bitmap = bitmap;
            Format = format;
            HorizontalResolution = horizontalResolution;
            VerticalResolution = verticalResolution;
        }
    }

    /// <summary>
    /// Holds an SKPixmap along with its file format and resolution (DPI) metadata.
    /// </summary>
    public class PixmapWithResolution
    {
        /// <summary>The pixmap pixel data.</summary>
        public SKPixmap Pixmap { get; set; }

        /// <summary>The bitmap file format (PNG, JPEG, GIF, etc.).</summary>
        public GraphicsBitmapFormat Format { get; set; }

        /// <summary>Horizontal resolution in dots per inch.</summary>
        public double HorizontalResolution { get; set; }

        /// <summary>Vertical resolution in dots per inch.</summary>
        public double VerticalResolution { get; set; }

        /// <summary>
        /// Creates a new PixmapWithResolution.
        /// </summary>
        /// <param name="pixmap">The pixmap pixel data.</param>
        /// <param name="format">The bitmap file format.</param>
        /// <param name="horizontalResolution">Horizontal resolution in DPI.</param>
        /// <param name="verticalResolution">Vertical resolution in DPI.</param>
        public PixmapWithResolution(SKPixmap pixmap, GraphicsBitmapFormat format, double horizontalResolution, double verticalResolution)
        {
            Pixmap = pixmap;
            Format = format;
            HorizontalResolution = horizontalResolution;
            VerticalResolution = verticalResolution;
        }
    }

    /// <summary>
    /// Provides bitmap read/write using ImageSharp for features that SkiaSharp lacks:
    /// reading/writing bitmap resolution (DPI) metadata and writing GIF files.
    /// </summary>
    public static class BitmapIO
    {
        /// <summary>
        /// Reads a bitmap from a stream. Uses ImageSharp to extract format and DPI metadata,
        /// and SkiaSharp to decode pixel data into SKImageInfo.PlatformColorType with premultiplied alpha.
        /// </summary>
        /// <param name="stream">The stream to read from (need not be seekable).</param>
        /// <returns>A BitmapWithResolution containing the decoded bitmap and its metadata.</returns>
        public static BitmapWithResolution ReadBitmapFromStream(Stream stream)
        {
            // Buffer the stream since we read it twice: once for metadata, once for pixel decode.
            byte[] data;
            using (MemoryStream ms = new MemoryStream()) {
                stream.CopyTo(ms);
                data = ms.ToArray();
            }

            // Use ImageSharp to identify format and resolution without decoding pixels.
            GraphicsBitmapFormat format;
            double horizontalDpi;
            double verticalDpi;

            using (MemoryStream ms = new MemoryStream(data)) {
                IImageFormat imageFormat = Image.DetectFormat(ms);
                ms.Position = 0;
                IImageInfo imageInfo = Image.Identify(ms);

                format = (imageFormat != null)
                    ? GraphicsBitmapFormatFromImageSharpFormat(imageFormat)
                    : GraphicsBitmapFormat.Unknown;

                if (imageInfo != null) {
                    horizontalDpi = ConvertToDpi(imageInfo.Metadata.HorizontalResolution, imageInfo.Metadata.ResolutionUnits);
                    verticalDpi = ConvertToDpi(imageInfo.Metadata.VerticalResolution, imageInfo.Metadata.ResolutionUnits);
                }
                else {
                    horizontalDpi = 96;
                    verticalDpi = 96;
                }
            }

            // Try SkiaSharp first to decode pixels into PlatformColorType with premultiplied alpha.
            // Fall back to ImageSharp for formats Skia doesn't support (e.g. TIFF).
            using (MemoryStream ms = new MemoryStream(data))
            using (SKData skData = SKData.Create(ms)) {
                SKCodec codec = SKCodec.Create(skData);
                if (codec != null) {
                    using (codec) {
                        SKImageInfo desiredInfo = new SKImageInfo(
                            codec.Info.Width, codec.Info.Height,
                            SKImageInfo.PlatformColorType, SKAlphaType.Premul);
                        SKBitmap bitmap = new SKBitmap(desiredInfo);
                        SKCodecResult result = codec.GetPixels(desiredInfo, bitmap.GetPixels());
                        if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput) {
                            bitmap.Dispose();
                            throw new InvalidOperationException($"Failed to decode bitmap: {result}");
                        }

                        return new BitmapWithResolution(bitmap, format, horizontalDpi, verticalDpi);
                    }
                }
            }

            // Skia couldn't decode the format; use ImageSharp to decode pixels instead.
            return ReadBitmapViaImageSharp(data, format, horizontalDpi, verticalDpi);
        }

        /// <summary>
        /// Fallback decoder using ImageSharp for formats SkiaSharp doesn't support (e.g. TIFF).
        /// Loads the image as Bgra32 pixels and copies them into an SKBitmap with
        /// PlatformColorType and premultiplied alpha.
        /// </summary>
        /// <param name="data">The raw image file bytes.</param>
        /// <param name="format">The detected bitmap format.</param>
        /// <param name="horizontalDpi">Horizontal resolution in DPI.</param>
        /// <param name="verticalDpi">Vertical resolution in DPI.</param>
        /// <returns>A BitmapWithResolution containing the decoded bitmap.</returns>
        private static BitmapWithResolution ReadBitmapViaImageSharp(byte[] data, GraphicsBitmapFormat format,
            double horizontalDpi, double verticalDpi)
        {
            using (Image<Bgra32> image = Image.Load<Bgra32>(data)) {
                int width = image.Width;
                int height = image.Height;

                // Create SKBitmap with Bgra8888/Unpremul to receive ImageSharp's straight-alpha pixels.
                SKImageInfo unpremulInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
                SKBitmap unpremulBitmap = new SKBitmap(unpremulInfo);
                IntPtr destPixels = unpremulBitmap.GetPixels();

                // Copy pixel rows from ImageSharp to the SKBitmap.
                image.ProcessPixelRows(accessor => {
                    for (int y = 0; y < height; y++) {
                        System.Span<Bgra32> row = accessor.GetRowSpan(y);
                        IntPtr destRow = destPixels + y * unpremulInfo.RowBytes;
                        System.Runtime.InteropServices.Marshal.Copy(
                            System.Runtime.InteropServices.MemoryMarshal.AsBytes(row).ToArray(),
                            0, destRow, width * 4);
                    }
                });

                // Convert to PlatformColorType with premultiplied alpha.
                SKImageInfo desiredInfo = new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
                if (unpremulBitmap.Info == desiredInfo) {
                    return new BitmapWithResolution(unpremulBitmap, format, horizontalDpi, verticalDpi);
                }

                SKBitmap finalBitmap = new SKBitmap(desiredInfo);
                using (SKCanvas canvas = new SKCanvas(finalBitmap)) {
                    canvas.DrawBitmap(unpremulBitmap, 0, 0);
                }
                unpremulBitmap.Dispose();

                return new BitmapWithResolution(finalBitmap, format, horizontalDpi, verticalDpi);
            }
        }

        /// <summary>
        /// Writes a bitmap to a stream in the format and resolution specified by the BitmapWithResolution.
        /// Handles any SKBitmap color type and alpha type by converting internally.
        /// </summary>
        /// <param name="bitmap">The bitmap with format and resolution metadata.</param>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="quality">Encoding quality (0-100), used for JPEG and WebP formats.</param>
        public static void WriteBitmapToStream(BitmapWithResolution bitmap, Stream stream, int quality)
        {
            using (SKPixmap pixmap = bitmap.Bitmap.PeekPixels()) {
                WritePixelsToStream(pixmap, bitmap.Format, bitmap.HorizontalResolution, bitmap.VerticalResolution, stream, quality);
            }
        }

        /// <summary>
        /// Writes a pixmap to a stream in the format and resolution specified by the PixmapWithResolution.
        /// Handles any SKPixmap color type and alpha type by converting internally.
        /// </summary>
        /// <param name="pixmap">The pixmap with format and resolution metadata.</param>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="quality">Encoding quality (0-100), used for JPEG and WebP formats.</param>
        public static void WritePixmapToStream(PixmapWithResolution pixmap, Stream stream, int quality)
        {
            WritePixelsToStream(pixmap.Pixmap, pixmap.Format, pixmap.HorizontalResolution, pixmap.VerticalResolution, stream, quality);
        }

        /// <summary>
        /// Shared implementation for writing pixel data from an SKPixmap to a stream.
        /// Converts pixels to Bgra8888 with straight (unpremultiplied) alpha for ImageSharp,
        /// then encodes with the specified format and resolution metadata.
        /// </summary>
        /// <param name="sourcePixmap">The source pixel data.</param>
        /// <param name="format">The target file format.</param>
        /// <param name="horizontalDpi">Horizontal resolution in DPI.</param>
        /// <param name="verticalDpi">Vertical resolution in DPI.</param>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="quality">Encoding quality (0-100).</param>
        private static void WritePixelsToStream(SKPixmap sourcePixmap, GraphicsBitmapFormat format,
            double horizontalDpi, double verticalDpi, Stream stream, int quality)
        {
            int width = sourcePixmap.Width;
            int height = sourcePixmap.Height;

            // Convert pixels to Bgra8888 with straight alpha for ImageSharp.
            SKImageInfo dstInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
            byte[] buffer = new byte[dstInfo.BytesSize];

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try {
                bool success = sourcePixmap.ReadPixels(dstInfo, handle.AddrOfPinnedObject(), dstInfo.RowBytes);
                if (!success)
                    throw new InvalidOperationException("Failed to convert pixel data for encoding.");
            }
            finally {
                handle.Free();
            }

            // Create ImageSharp image from the raw pixel data, set resolution, and encode.
            using (Image<Bgra32> image = Image.LoadPixelData<Bgra32>(buffer, width, height)) {
                image.Metadata.HorizontalResolution = horizontalDpi;
                image.Metadata.VerticalResolution = verticalDpi;
                image.Metadata.ResolutionUnits = PixelResolutionUnit.PixelsPerInch;

                IImageEncoder encoder = CreateEncoder(format, quality);
                image.Save(stream, encoder);
            }
        }

        /// <summary>
        /// Converts a resolution value to DPI based on the source resolution units.
        /// </summary>
        /// <param name="resolution">The resolution value.</param>
        /// <param name="units">The units of the resolution value.</param>
        /// <returns>The resolution in dots per inch.</returns>
        private static double ConvertToDpi(double resolution, PixelResolutionUnit units)
        {
            double convertedResolution;

            switch (units) {
            case PixelResolutionUnit.PixelsPerInch:
                convertedResolution = resolution;
                break;
            case PixelResolutionUnit.PixelsPerCentimeter:
                convertedResolution = resolution * 2.54;
                break;
            case PixelResolutionUnit.PixelsPerMeter:
                convertedResolution = resolution * 0.0254;
                break;
            default:
                // AspectRatio or unknown: no meaningful DPI, default to 96.
                convertedResolution = 96;
                break;
            }

            // PNG resolution is often stored in a way that results in fractional DPI values;
            // round to 1 decimal place for consistency.
            convertedResolution = Math.Round(convertedResolution, 1); 
            return convertedResolution;
        }

        /// <summary>
        /// Creates an ImageSharp encoder for the specified bitmap format and quality level.
        /// </summary>
        /// <param name="format">The target bitmap format.</param>
        /// <param name="quality">Encoding quality (0-100), applied to formats that support it.</param>
        /// <returns>An encoder configured for the specified format.</returns>
        private static IImageEncoder CreateEncoder(GraphicsBitmapFormat format, int quality)
        {
            switch (format) {
            case GraphicsBitmapFormat.PNG:
                return new PngEncoder();
            case GraphicsBitmapFormat.JPEG:
                return new JpegEncoder { Quality = quality };
            case GraphicsBitmapFormat.GIF:
                // Use an Octree quantizer with no dithering. Our bitmaps typically have few
                // distinct colors (map/line art), so a ≤256-entry palette reproduces them
                // exactly; disabling dithering avoids error diffusion and keeps colors crisp.
                return new GifEncoder {
                    Quantizer = new OctreeQuantizer(new QuantizerOptions { Dither = null })
                };
            case GraphicsBitmapFormat.TIFF:
                return new TiffEncoder();
            case GraphicsBitmapFormat.BMP:
                return new BmpEncoder();
            case GraphicsBitmapFormat.WebP:
                return new WebpEncoder { Quality = quality };
            default:
                throw new ArgumentException($"Unsupported bitmap format for writing: {format}", nameof(format));
            }
        }

        /// <summary>
        /// Maps an ImageSharp IImageFormat to a GraphicsBitmapFormat enum value.
        /// </summary>
        /// <param name="imageFormat">The ImageSharp format.</param>
        /// <returns>The corresponding GraphicsBitmapFormat.</returns>
        private static GraphicsBitmapFormat GraphicsBitmapFormatFromImageSharpFormat(IImageFormat imageFormat)
        {
            if (imageFormat is PngFormat)
                return GraphicsBitmapFormat.PNG;
            if (imageFormat is JpegFormat)
                return GraphicsBitmapFormat.JPEG;
            if (imageFormat is GifFormat)
                return GraphicsBitmapFormat.GIF;
            if (imageFormat is BmpFormat)
                return GraphicsBitmapFormat.BMP;
            if (imageFormat is TiffFormat)
                return GraphicsBitmapFormat.TIFF;
            if (imageFormat is WebpFormat)
                return GraphicsBitmapFormat.WebP;

            return GraphicsBitmapFormat.Unknown;
        }
    }
}
