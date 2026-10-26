using System;
using System.IO;
using PDFiumCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata;
using SkiaSharp;

namespace PurplePen.PdfConverter
{
    // Converts PDF files into PNG bitmaps using PDFium.
    //
    // PDFium keeps process-wide state and is not thread-safe, so all conversions are serialized
    // through a single lock, and the library is initialized once on first use and never torn down.
    // Neither PDFiumCore nor the native pdfium library is loaded until the first conversion.
    public static class PdfConversion
    {
        private static readonly object pdfiumLock = new object();
        private static bool pdfiumInitialized;

        // Convert a PDF file into a PNG file at the given resolution.
        //   dpi: resolution of the resulting bitmap, in dots per inch (1 to 100000).
        //   sourcePdfFileName: the PDF file to convert.
        //   destinationPngFileName: the PNG file to create. If it contains "%d", every page is converted,
        //       with "%d" replaced by the 1-based page number; otherwise only the first page is converted.
        // Throws an exception describing the problem if the conversion fails.
        public static void Convert(float dpi, string sourcePdfFileName, string destinationPngFileName)
        {
            if (!(dpi >= 1 && dpi <= 100000))
                throw new ArgumentOutOfRangeException(nameof(dpi), dpi, $"Invalid value for dpi: {dpi}");

            if (!File.Exists(sourcePdfFileName))
                throw new FileNotFoundException($"Source PDF '{sourcePdfFileName}' does not exist.", sourcePdfFileName);

            try {
                lock (pdfiumLock) {
                    if (!pdfiumInitialized) {
                        fpdfview.FPDF_InitLibrary();
                        pdfiumInitialized = true;
                    }

                    ConvertDocument(dpi, sourcePdfFileName, destinationPngFileName);
                }
            }
            finally {
                // A 600 dpi page leaves hundreds of megabytes of pooled buffers in ImageSharp's
                // allocator. Give them back, since conversions are rare.
                Configuration.Default.MemoryAllocator.ReleaseRetainedResources();
            }
        }

        // Convert one or all pages of a PDF document. Must be called with pdfiumLock held.
        private static void ConvertDocument(float dpi, string sourcePdfFileName, string destinationPngFileName)
        {
            FpdfDocumentT document = fpdfview.FPDF_LoadDocument(sourcePdfFileName, null);
            if (document == null) {
                throw new Exception($"Failed to load PDF document: {sourcePdfFileName}");
            }

            try {
                if (destinationPngFileName.Contains("%d")) {
                    // Multi-page convert.
                    int numPages = fpdfview.FPDF_GetPageCount(document);
                    for (int pageNumber = 0; pageNumber < numPages; ++pageNumber) {
                        string destFileName = destinationPngFileName.Replace("%d", (pageNumber + 1).ToString());
                        SavePng(dpi, document, pageNumber, destFileName);
                    }
                }
                else {
                    SavePng(dpi, document, 0, destinationPngFileName);
                }
            }
            finally {
                fpdfview.FPDF_CloseDocument(document);
            }
        }

        // Render one page of a PDF document and save it as a PNG file with the resolution recorded in it.
        // Must be called with pdfiumLock held.
        private static void SavePng(float dpi, FpdfDocumentT document, int pageNumber, string destFileName)
        {
            double widthInPoints = 0, heightInPoints = 0;
            fpdfview.FPDF_GetPageSizeByIndex(document, pageNumber, ref widthInPoints, ref heightInPoints);

            int pixelWidth = (int)Math.Round(widthInPoints * dpi / 72.0);
            int pixelHeight = (int)Math.Round(heightInPoints * dpi / 72.0);

            FpdfPageT page = fpdfview.FPDF_LoadPage(document, pageNumber);
            if (page == null) {
                throw new Exception($"Failed to load page {pageNumber}");
            }

            try {
                // Create bitmap (BGRA format, 4 bytes per pixel)
                using SKBitmap bitmap = new SKBitmap(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
                if (bitmap == null) {
                    throw new Exception("Failed to create bitmap");
                }

                // Render PDF page to the Bitmap's memory buffer.
                // We create a temporary FPDF_BITMAP to bridge to Skia's memory.
                IntPtr scan0 = bitmap.GetPixels();
                FpdfBitmapT fpdfBitmap = fpdfview.FPDFBitmapCreateEx(pixelWidth, pixelHeight, 4, scan0, pixelWidth * 4);
                try {
                    // Fill background with white (otherwise it defaults to transparent/black)
                    fpdfview.FPDFBitmapFillRect(fpdfBitmap, 0, 0, pixelWidth, pixelHeight, 0xFFFFFFFF);

                    // Parameters: (bitmap_handle, page, start_x, start_y, size_x, size_y, rotation, flags)
                    fpdfview.FPDF_RenderPageBitmap(fpdfBitmap, page, 0, 0, pixelWidth, pixelHeight, 0, 0);
                }
                finally {
                    fpdfview.FPDFBitmapDestroy(fpdfBitmap);
                }

                // Save the SkiaSharp Bitmap to a file
                using (SKImage image = SKImage.FromBitmap(bitmap))
                using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (Stream stream = data.AsStream())
                using (Image imageSharpImage = Image.Load(stream)) {
                    // We need to use SixLabors.ImageSharp to set the DPI metadata, since SkiaSharp doesn't support that.
                    imageSharpImage.Metadata.ResolutionUnits = PixelResolutionUnit.PixelsPerInch;
                    imageSharpImage.Metadata.HorizontalResolution = dpi;
                    imageSharpImage.Metadata.VerticalResolution = dpi;
                    imageSharpImage.SaveAsPng(destFileName);
                }
            }
            finally {
                fpdfview.FPDF_ClosePage(page);
            }
        }
    }
}
