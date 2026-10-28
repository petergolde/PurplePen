// WindowsPrinter.cs
//
// Prints to a Windows printer through the Windows Forms printing API (PrintDocument),
// as an IPrintingTarget. Ported from the WinForms PurplePen/WinFormsPrinter.cs.
//
// Unlike the WinForms version, pages are drawn with Skia rather than GDI+, because
// everything else in the Avalonia app (bitmaps, fonts, font fallback, text metrics) is
// Skia-based. Each page is rendered with Skia into horizontal bands of a bitmap at (up
// to) the printer's resolution, and the bands are drawn onto the printer's Graphics.
// The output is therefore always raster.
//
// Only compiled when targeting Windows (see AvPurplePen.csproj).

using PurplePen;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Runtime.InteropServices;

namespace AvPurplePen.Platform.Windows
{
    /// <summary>
    /// Abstracts the Windows Forms printing API as an IPrintingTarget, rasterizing each page with Skia.
    /// </summary>
    internal class WindowsPrinter : IPrintingTarget
    {
        // Pages are rasterized at the printer resolution, divided by the smallest whole number
        // that brings it to at most this, so each raster pixel covers a whole number of printer dots.
        private const float MaxRasterDpi = 720F;

        // The resolution to assume if the printer can't be queried.
        private const float DefaultRasterDpi = 600F;

        // The maximum size of a band bitmap, in bytes. Larger pages are rendered in several bands.
        private const long MaxBandBytes = 48L * 1024 * 1024;

        private readonly PrinterSettings printerSettings;
        private readonly ColorModel colorModel;
        private string documentTitle = "";
        private int currentPage;
        private readonly List<DocumentPage> documentPages = new List<DocumentPage>();

        // A page to print: its paper size, and the function that draws it.
        private sealed class DocumentPage
        {
            public DocumentPage(PrintingPaperSize paperSize, Action<IGraphicsTarget> drawPage)
            {
                PaperSize = paperSize;
                DrawPage = drawPage;
            }

            public PrintingPaperSize PaperSize { get; }
            public Action<IGraphicsTarget> DrawPage { get; }
        }

        /// <summary>
        /// Create a printing target for a printer.
        /// </summary>
        /// <param name="printerSettings">The printer and its settings. Not copied, so the caller must not change it later.</param>
        /// <param name="colorModel">The color model used to render the pages.</param>
        public WindowsPrinter(PrinterSettings printerSettings, ColorModel colorModel)
        {
            this.printerSettings = printerSettings;
            this.colorModel = colorModel;
        }

        // The WinForms printing model uses a PrintDocument that raises events to perform the printing.
        // In order to fit this into the IPrintingTarget model, we basically just save information
        // for each page, and then do all the printing when EndPrinting is called. Each page is saved
        // as a DocumentPage object. Note that we aren't saving the actual images or page content, but just
        // delegates to functions that will draw the page when called.

        /// <summary>
        /// Called at the beginning of printing.
        /// </summary>
        /// <param name="documentTitle">The document name shown in the print queue.</param>
        /// <param name="pageCount">The number of pages that will be printed.</param>
        public void StartPrinting(string documentTitle, int pageCount)
        {
            this.documentTitle = documentTitle;
            currentPage = 0;
            documentPages.Clear();
        }

        /// <summary>
        /// Gets the resolution pages are rendered at: the rasterization resolution for this printer.
        /// </summary>
        public float GetPrinterDpi()
        {
            // Always check if the printer is valid before querying
            if (printerSettings.IsValid) {
                using (Graphics g = printerSettings.CreateMeasurementGraphics()) {
                    return RasterDpi(Math.Max(g.DpiX, g.DpiY));
                }
            }
            else {
                return DefaultRasterDpi;
            }
        }

        /// <summary>
        /// Saves a page to print when EndPrinting is called.
        /// </summary>
        /// <param name="pageNumber">The page number, starting at 1. Pages must be printed in order.</param>
        /// <param name="paperSize">The paper size (and orientation) of the page.</param>
        /// <param name="drawPage">Draws the page, in hundredths of an inch from the top-left corner of the paper.</param>
        public void PrintPage(int pageNumber, PrintingPaperSize paperSize, Action<IGraphicsTarget> drawPage)
        {
            Debug.Assert(pageNumber == documentPages.Count + 1, "Pages must be printed in order, and page numbers must be sequential starting at 1.");
            documentPages.Add(new DocumentPage(paperSize, drawPage));
        }

        /// <summary>
        /// Does the actual printing: creates a PrintDocument, sets up the event handlers, and
        /// calls Print on it. Exceptions from the printing propagate to the caller.
        /// </summary>
        public void EndPrinting()
        {
            try {
                if (documentPages.Count == 0)
                    return;

                using (PrintDocument printDocument = new PrintDocument()) {
                    printDocument.DocumentName = documentTitle;
                    printDocument.PrinterSettings = printerSettings;

                    printDocument.QueryPageSettings += PrintDocument_QueryPageSettings;
                    printDocument.PrintPage += PrintDocument_PrintPage;

                    currentPage = 0;
                    printDocument.Print();
                }
            }
            finally {
                documentPages.Clear();
            }
        }

        // Event called from the PrintDocument when it needs to know the page settings for a page.
        private void PrintDocument_QueryPageSettings(object sender, QueryPageSettingsEventArgs e)
        {
            PrintingPaperSize paperSize = documentPages[currentPage].PaperSize;
            e.PageSettings.PaperSize = WinformsPaperSize(paperSize, e.PageSettings.PrinterSettings.PaperSizes);
            e.PageSettings.Landscape = paperSize.Landscape;
        }

        // Event called from the PrintDocument when it needs to print a page.
        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics!;
            DocumentPage page = documentPages[currentPage];

            // The printer's graphics context is offset by the hard margin of the printer; translate so that
            // (0,0) is the top-left corner of the paper. The units are hundredths of an inch.
            g.TranslateTransform(-e.PageSettings.HardMarginX, -e.PageSettings.HardMarginY);

            float dpi = RasterDpi(Math.Max(g.DpiX, g.DpiY));
            DrawRasterizedPage(g, page, dpi);

            // Update page count.
            ++currentPage;
            e.HasMorePages = (currentPage < documentPages.Count);
#if PORTING
            // Deal with pausing after each page if that option is set (WinForms StopAfterEachPage).
            // Maybe this goes into IPrintable in some way?
#endif
        }

        // Render a page with Skia, a horizontal band at a time, and draw the bands onto
        // the printer's graphics. The page is drawn once for each band.
        // g: the printer graphics, transformed so (0,0) is the top-left of the paper, in hundredths of an inch.
        // page: the page to print; it draws in hundredths of an inch from the top-left of the paper.
        // dpi: the resolution to render at.
        private void DrawRasterizedPage(Graphics g, DocumentPage page, float dpi)
        {
            float pixelsPerHundredth = dpi / 100F;
            int pixelWidth = Math.Max(1, (int)Math.Ceiling(page.PaperSize.SizeInHundreths.Width * pixelsPerHundredth));
            int pixelHeight = Math.Max(1, (int)Math.Ceiling(page.PaperSize.SizeInHundreths.Height * pixelsPerHundredth));
            int bandHeight = (int)Math.Clamp(MaxBandBytes / (pixelWidth * 4L), 1, pixelHeight);

            IColorConverter? colorConverter = (colorModel == ColorModel.CMYK) ? SwopColorConverter.Instance : null;

            // Each raster pixel covers a whole number of printer dots, so nearest-neighbor scaling
            // is exact and keeps the band edges from blending into each other.
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            // Bgra8888 has the same memory layout as GDI+ 32bpp formats, so GDI+ can use the pixels directly.
            SKImageInfo bandInfo = new SKImageInfo(pixelWidth, bandHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
            using (SKBitmap bandBitmap = new SKBitmap(bandInfo))
            using (SKCanvas canvas = new SKCanvas(bandBitmap)) {
                for (int bandTop = 0; bandTop < pixelHeight; bandTop += bandHeight) {
                    int bandRows = Math.Min(bandHeight, pixelHeight - bandTop);

                    // Render the band: page coordinates are hundredths of an inch, with the band's top at the top of the bitmap.
                    canvas.ResetMatrix();
                    canvas.Clear(SKColors.White);
                    canvas.Scale(pixelsPerHundredth);
                    canvas.Translate(0, -bandTop / pixelsPerHundredth);
                    using (Skia_GraphicsTarget grTarget = new Skia_GraphicsTarget(canvas, false, colorConverter)) {
                        grTarget.PushAntiAliasing(true);
                        page.DrawPage(grTarget);
                        grTarget.PopAntiAliasing();
                    }
                    canvas.Flush();

                    // Draw the band onto the printer. The band is opaque, so its alpha is ignored. The GDI+ bitmap
                    // uses the Skia pixels without copying, so it must be disposed before the band is reused.
                    using (Bitmap gdiBand = new Bitmap(pixelWidth, bandRows, bandBitmap.RowBytes, PixelFormat.Format32bppRgb, bandBitmap.GetPixels())) {
                        RectangleF destRect = new RectangleF(0, bandTop / pixelsPerHundredth, pixelWidth / pixelsPerHundredth, bandRows / pixelsPerHundredth);
                        g.DrawImage(gdiBand, destRect, new RectangleF(0, 0, pixelWidth, bandRows), GraphicsUnit.Pixel);
                    }
                }
            }
        }

        // The resolution to rasterize pages at for a printer resolution: the printer resolution
        // divided by the smallest whole number that brings it to at most MaxRasterDpi.
        private static float RasterDpi(float printerDpi)
        {
            if (printerDpi <= 0)
                return DefaultRasterDpi;

            int divisor = Math.Max(1, (int)Math.Ceiling(printerDpi / MaxRasterDpi));
            return printerDpi / divisor;
        }

        // Get the WinForms PaperSize object corresponding to the given PrintingPaperSize.
        // Note that we can't just create a new PaperSize object, but instead need to find the
        // one in the printer's PaperSizes collection that matches the given size, because
        // that's how the WinForms printing API works.
        private PaperSize WinformsPaperSize(PrintingPaperSize paperSize, PrinterSettings.PaperSizeCollection paperSizes)
        {
            // The papersize we want, in portait orientation (since the printer's PaperSizes are in portrait orientation).
            // We will set the Landscape property of the PageSettings to true if we want landscape.
            PrintingPaperSize portraitTargetSize = new PrintingPaperSize(false, paperSize);
            int targetWidth = (int)Math.Round(portraitTargetSize.SizeInHundreths.Width);
            int targetHeight = (int)Math.Round(portraitTargetSize.SizeInHundreths.Height);
            string targetName = portraitTargetSize.Name;

            // First try for exact match on name and dimensions
            foreach (PaperSize supportedSize in paperSizes) {
                if (string.Equals(supportedSize.Kind.ToString(), targetName, StringComparison.InvariantCultureIgnoreCase) &&
                    supportedSize.Width == targetWidth && supportedSize.Height == targetHeight) {
                    return supportedSize;
                }
            }

            // Next for exact match on dimensions only
            foreach (PaperSize supportedSize in paperSizes) {
                if (supportedSize.Width == targetWidth && supportedSize.Height == targetHeight) {
                    return supportedSize;
                }
            }

            // Create a custom size.
            return new PaperSize("Custom", targetWidth, targetHeight);
        }
    }
}
