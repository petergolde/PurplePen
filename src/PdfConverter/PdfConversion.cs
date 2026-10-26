using System;
using System.Buffers;
using System.IO;
using PDFiumCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata;
using SixLabors.ImageSharp.PixelFormats;

namespace PurplePen.PdfConverter
{
    // Converts PDF files into PNG bitmaps using PDFium.
    //
    // PDFium keeps process-wide state and is not thread-safe, so all conversions are serialized
    // through a single lock, and the library is initialized once on first use and never torn down.
    // Neither PDFiumCore nor the native pdfium library is loaded until the first conversion.
    //
    // PDFium renders straight into the pixel buffer of an ImageSharp image, which is then saved
    // as a PNG with the resolution recorded in it.
    public static class PdfConversion
    {
        private const int FPDFBitmap_BGRA = 4;      // PDFium bitmap format: 4 bytes per pixel, in the order blue, green, red, alpha.
        private const int BytesPerPixel = 4;

        private static readonly object pdfiumLock = new object();
        private static bool pdfiumInitialized;

        // ImageSharp configuration that asks for each image's pixels in one contiguous buffer, so
        // that PDFium can normally render a page in a single pass.
        private static readonly Configuration imageConfiguration = CreateImageConfiguration();

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
                imageConfiguration.MemoryAllocator.ReleaseRetainedResources();
            }
        }

        // Create the ImageSharp configuration used for rendered pages.
        private static Configuration CreateImageConfiguration()
        {
            Configuration configuration = Configuration.Default.Clone();
            configuration.PreferContiguousImageBuffers = true;
            return configuration;
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
                using (Image<Bgra32> image = new Image<Bgra32>(imageConfiguration, pixelWidth, pixelHeight)) {
                    RenderPage(page, image);

                    image.Metadata.ResolutionUnits = PixelResolutionUnit.PixelsPerInch;
                    image.Metadata.HorizontalResolution = dpi;
                    image.Metadata.VerticalResolution = dpi;

                    // The page is rendered onto an opaque white background, so there is no alpha to save.
                    PngEncoder encoder = new PngEncoder() {
                        ColorType = PngColorType.Rgb,
                        BitDepth = PngBitDepth.Bit8
                    };
                    image.SaveAsPng(destFileName, encoder);
                }
            }
            finally {
                fpdfview.FPDF_ClosePage(page);
            }
        }

        // Render a page into an image, filling the whole image. Must be called with pdfiumLock held.
        //
        // PDFium renders into the image's own pixel memory, one memory segment at a time. ImageSharp
        // keeps whole rows within a segment, so each segment is a horizontal band of the page, which
        // PDFium is given as a bitmap with the page offset upward to that band. With imageConfiguration
        // there is normally a single segment, so the page is rendered once.
        private static unsafe void RenderPage(FpdfPageT page, Image<Bgra32> image)
        {
            int width = image.Width;
            int height = image.Height;
            int stride = width * BytesPerPixel;
            int firstRow = 0;

            foreach (Memory<Bgra32> segment in image.GetPixelMemoryGroup()) {
                int rows = Math.Min(segment.Length / width, height - firstRow);
                if (rows <= 0)
                    break;

                using (MemoryHandle pinnedSegment = segment.Pin()) {
                    FpdfBitmapT bitmap = fpdfview.FPDFBitmapCreateEx(width, rows, FPDFBitmap_BGRA, (IntPtr)pinnedSegment.Pointer, stride);
                    if (bitmap == null) {
                        throw new Exception("Failed to create bitmap");
                    }

                    try {
                        // Fill background with white (otherwise it defaults to transparent/black)
                        fpdfview.FPDFBitmapFillRect(bitmap, 0, 0, width, rows, 0xFFFFFFFF);

                        // Parameters: (bitmap_handle, page, start_x, start_y, size_x, size_y, rotation, flags)
                        // A negative start_y shifts the page up so this band shows rows firstRow onward.
                        fpdfview.FPDF_RenderPageBitmap(bitmap, page, 0, -firstRow, width, height, 0, 0);
                    }
                    finally {
                        fpdfview.FPDFBitmapDestroy(bitmap);
                    }
                }

                firstRow += rows;
            }
        }
    }
}
