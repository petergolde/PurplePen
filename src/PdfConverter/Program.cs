using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



#if NETFRAMEWORK
using System.Drawing;
using System.Drawing.Imaging;
using PdfiumViewer;
#else
using System.Runtime.InteropServices;
using PDFiumCore;
using SkiaSharp;
#endif

namespace PurplePen.PdfConverter
{
    public class Program
    {
        static float dpi;
        static string sourcePdfFileName;
        static string destinationPngFileName;

        public static int Main(string[] args)
        {
            try {
                if (!ParseArguments(args))
                    return 2;

                DoConversion();
                return 0;
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                return 1;
            }
        }

        static bool ParseArguments(string[] args)
        {
            if (args.Length != 3) {
                Console.WriteLine("usage: PdfConverter <dpi> <source_pdf> <destination_png>");
                return false;
            }

            if (!float.TryParse(args[0], out dpi) || dpi < 1 || dpi > 100000) {
                Console.WriteLine("Invalid value for dpi: {0}", args[0]);
                return false;
            }

            sourcePdfFileName = args[1];
            destinationPngFileName = args[2];

            if (!File.Exists(sourcePdfFileName)) {
                Console.WriteLine("Source PDF '{0}' does not exists.", sourcePdfFileName);
                return false;
            }

            return true;
        }

        static void DoConversion()
        {
#if NETFRAMEWORK
            using (PdfDocument document = PdfDocument.Load(sourcePdfFileName)) {
                if (destinationPngFileName.Contains("%d")) {
                    // Multi-page convert.
                    int numPages = document.PageCount;
                    for (int pageNumber = 0; pageNumber < numPages; ++pageNumber) {
                        string destFileName = destinationPngFileName.Replace("%d", (pageNumber+1).ToString());
                        SavePng(document, pageNumber, destFileName);
                    }
                }
                else {
                    SavePng(document, 0, destinationPngFileName);
                }
            }
#else
            fpdfview.FPDF_InitLibrary();
            try {
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
                            SavePng(document, pageNumber, destFileName);
                        }
                    }
                    else {
                        SavePng(document, 0, destinationPngFileName);
                    }
                }
                finally {
                    fpdfview.FPDF_CloseDocument(document);
                }
            }
            finally {
                fpdfview.FPDF_DestroyLibrary();
            }
#endif
        }

#if NETFRAMEWORK
        private static void SavePng(PdfDocument document, int pageNumber, string destFileName)
        {
            SizeF sizeInPoints = document.PageSizes[pageNumber];
            int widthInPixels = (int)Math.Round(sizeInPoints.Width * (float)dpi / 72F);
            int heightInPixels = (int)Math.Round(sizeInPoints.Height * (float)dpi / 72F);
            using (Image image = document.Render(pageNumber, widthInPixels, heightInPixels, dpi, dpi, true)) {
                image.Save(destFileName, ImageFormat.Png);
            }
        }
#else
        private static void SavePng(FpdfDocumentT document, int pageNumber, string destFileName)
        {
            double widthInPoints = 0, heightInPoints = 0;
            fpdfview.FPDF_GetPageSizeByIndex(document, pageNumber, ref widthInPoints, ref heightInPoints);

            int pixelWidth = (int)Math.Round(widthInPoints * dpi / 72.0);
            int pixelHeight = (int)Math.Round(heightInPoints * dpi / 72.0);

            FpdfPageT page = fpdfview.FPDF_LoadPage(document, pageNumber);
            if (page == null) {
                throw new Exception($"Failed to load page {pageNumber}");
            }

            // Create bitmap (BGRA format, 4 bytes per pixel)
            using SKBitmap bitmap = new SKBitmap(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
            if (bitmap == null) {
                throw new Exception("Failed to create bitmap");
            }

            // 5. Render PDF page to the Bitmap's memory buffer
            IntPtr scan0 = bitmap.GetPixels(); 
            try {
                // FPDF_RenderPageBitmap renders the page into the provided memory buffer
                // Parameters: (bitmap_handle, page, start_x, start_y, size_x, size_y, rotation, flags)
                // Note: We create a temporary FPDF_BITMAP to bridge to Skia's memory
                var fpdfBitmap = fpdfview.FPDFBitmapCreateEx(pixelWidth, pixelHeight, 4, scan0, pixelWidth * 4);

                // Fill background with white (otherwise it defaults to transparent/black)
                fpdfview.FPDFBitmapFillRect(fpdfBitmap, 0, 0, pixelWidth, pixelHeight, 0xFFFFFFFF);

                fpdfview.FPDF_RenderPageBitmap(fpdfBitmap, page, 0, 0, pixelWidth, pixelHeight, 0, 0);

                // 6. Save the SkiaSharp Bitmap to a file
                using (var image = SKImage.FromBitmap(bitmap))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (var stream = File.OpenWrite(destFileName)) {
                    data.SaveTo(stream);
                }

                // Cleanup Fpdf objects
                fpdfview.FPDFBitmapDestroy(fpdfBitmap);
            }
            finally {
                fpdfview.FPDF_ClosePage(page);
            }
        }
#endif
    }
}
