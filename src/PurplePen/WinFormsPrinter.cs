using PurplePen.Graphics2D;
using PurplePen.MapModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PurplePen
{
    // Abstracts the Windows Form printing API as an IPrintingTarget.  
    internal class WinFormsPrinter : IPrintingTarget
    {
        private readonly WinFormsPrinterOptions options;
        private string documentTitle;
        private int currentPage, pageCount;
        private List<DocumentPage> documentPages = new List<DocumentPage>();

        private class DocumentPage
        {
            public int PageNumber;
            public PrintingPaperSize PaperSize;
            public Action<IGraphicsTarget> DrawPage;
        }


        public WinFormsPrinter(WinFormsPrinter.WinFormsPrinterOptions options)
        {
            this.options = options.Clone();
        }

        // The WinForms printing model uses a PrintDocument that raises events to perform the printing.
        // In order to fit this into the IPrintingTarget model, we basically just save information
        // for each page, and then do all the printing when EndPrinting is called. Each page is saved into
        // as DocumentPage object. Note that we aren't saving the actual images or page content, but just
        // delegates to functions that will draw the page when called.


        public void StartPrinting(string documentTitle, int pageCount)
        {
            this.pageCount = pageCount;
            this.documentTitle = documentTitle;
            currentPage = 0;
            documentPages.Clear();
        }

        public float GetPrinterDpi()
        {
            PrinterSettings settings = options.PageSettings.PrinterSettings;

            // Always check if the printer is valid before querying
            if (settings.IsValid) {
                using (Graphics g = settings.CreateMeasurementGraphics()) {
                    return Math.Max(g.DpiX, g.DpiY);
                }
            }
            else {
                return 600F;  // reasonable default, I guess.
            }
        }

        public void PrintPage(int pageNumber, PrintingPaperSize paperSize, Action<IGraphicsTarget> drawPage)
        {
            Debug.Assert(pageNumber == documentPages.Count + 1, "Pages must be printed in order, and page numbers must be sequential starting at 1.");
            documentPages.Add(new DocumentPage { PageNumber = pageNumber, PaperSize = paperSize, DrawPage = drawPage });
        }


        // In EndPrinting , we create a PrintDocument, set up the event handlers, and then call Print on it to do the actual printing.
        public void EndPrinting()
        {
            using (PrintDocument printDocument = new System.Drawing.Printing.PrintDocument()) {
                printDocument.DocumentName = documentTitle;
                printDocument.PrinterSettings = options.PageSettings.PrinterSettings;
                printDocument.DefaultPageSettings = options.PageSettings;

                printDocument.BeginPrint += this.PrintDocument_BeginPrint;
                printDocument.QueryPageSettings += this.PrintDocument_QueryPageSettings;
                printDocument.PrintPage += this.PrintDocument_PrintPage;
                printDocument.EndPrint += this.PrintDocument_EndPrint;

                if (options.PrintPreview) {
                    PrintPreviewDialog dialog = new PrintPreviewDialog();
                    dialog.UseAntiAlias = true;
                    dialog.Document = printDocument;
                    dialog.StartPosition = FormStartPosition.CenterParent;
                    if (options.PreviewDialogSize.HasValue) {
                        dialog.Size = options.PreviewDialogSize.Value;
                    }
                    dialog.SizeGripStyle = SizeGripStyle.Show;
                    dialog.ShowIcon = false;

                    // Remove the "print" button.
                    foreach (Control ctl in dialog.Controls) {
                        ToolStrip strip = ctl as ToolStrip;
                        if (strip != null) {
                            var button = strip.Items[0];
                            if (button.Name == "printToolStripButton")
                                strip.Items.Remove(button);
                        }
                    }

                    dialog.ShowDialog();
                    dialog.Dispose();
                }
                else {
                    printDocument.Print();
                }
            }

            documentPages.Clear();
        }


        // Event called from the PrintDocument when it starts printing.
        private void PrintDocument_BeginPrint(object sender, PrintEventArgs e)
        {
            // nothing to do.
        }

        // Event called from the PrintDocument when it finishes printing.
        private void PrintDocument_EndPrint(object sender, PrintEventArgs e)
        {
            // nothing to do.
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
            Graphics g = e.Graphics;

            // Deal with the hard margin of the printer, based on printPreview or not.
            if (!options.PrintPreview) {
                // When printing, we need to translate the graphics by the hard margin of the printer, since the printer's graphics context is offset by the hard margin.
                g.TranslateTransform(-e.PageSettings.HardMarginX, -e.PageSettings.HardMarginY);
            }
            // Get the dpi of the printer.
            float dpi = Math.Max((int)g.DpiX, (int)g.DpiY);

            if (options.RasterizeInBands) {
                // Draw the page into bitmap bands, then draw the bands onto the printer.
                DocumentPage page = documentPages[currentPage];
                using (GDIPlus_GraphicsTarget printerTarget = new GDIPlus_GraphicsTarget(g)) {
                    DrawPageInBands(printerTarget, page.PaperSize, page.DrawPage, options.ColorModel, AdjustDpi(dpi));
                }
            }
            else {
                // Set up the graphics target.
                IGraphicsTarget graphicsTarget;
                if (options.ColorModel == ColorModel.RGB)
                    graphicsTarget = new GDIPlus_GraphicsTarget(g);
                else if (options.ColorModel == ColorModel.CMYK)
                    graphicsTarget = new GDIPlus_GraphicsTarget(g, SwopColorConverter.Instance);
                else
                    throw new NotImplementedException();

                // Draw the page
                using (graphicsTarget) {
                    documentPages[currentPage].DrawPage(graphicsTarget);
                }
            }

            // Update page count.
            ++currentPage;
            e.HasMorePages = (currentPage < pageCount);
#if PORTING
            // Deal with pausing after each page if that option is set. 
#endif
        }

        // The maximum number of pixels in a band bitmap. Larger pages are drawn in several bands.
        private const long MaxPixelsPerBand = 20000000;    // 20M pixels = 80M bytes (4 bytes per pixel).

        // Draw a page into horizontal bands of a bitmap, and draw each band onto the page target. Bitmap
        // graphics targets support blending, which the printer's graphics target does not, so this is
        // the only way to get purple blending correct. The page is drawn once for each band. The band
        // bitmaps are created with Services.BitmapLoader, which must match the page target's backend.
        // pageTarget: the target to draw the bands onto, with (0,0) the top-left of the paper, in hundredths of an inch.
        // paperSize: the paper size of the page.
        // drawPage: draws the page, in hundredths of an inch from the top-left of the paper.
        // colorModel: the color model used to render the bands.
        // dpi: the resolution of the band bitmaps.
        internal static void DrawPageInBands(IGraphicsTarget pageTarget, PrintingPaperSize paperSize, Action<IGraphicsTarget> drawPage, ColorModel colorModel, float dpi)
        {
            float pixelsPerHundredth = dpi / 100F;
            SizeF pageSize = paperSize.SizeInHundreths;
            int pixelWidth = Math.Max(1, (int)Math.Ceiling(pageSize.Width * pixelsPerHundredth));
            int pixelHeight = Math.Max(1, (int)Math.Ceiling(pageSize.Height * pixelsPerHundredth));
            int bandHeight = (int)Math.Max(1, Math.Min(MaxPixelsPerBand / pixelWidth, pixelHeight));

            IColorConverter colorConverter;
            if (colorModel == ColorModel.RGB)
                colorConverter = DefaultColorConverter.Instance;
            else if (colorModel == ColorModel.CMYK)
                colorConverter = SwopColorConverter.Instance;
            else
                throw new NotImplementedException();

            // Create the bitmap once, because each band is the same size.
            using (IGraphicsBitmap bandBitmap = Services.BitmapLoader.CreateEmptyBitmap(pixelWidth, bandHeight, null)) {
                for (int bandTop = 0; bandTop < pixelHeight; bandTop += bandHeight) {
                    float bandTopInHundredths = bandTop / pixelsPerHundredth;

                    using (IBitmapGraphicsTarget bandTarget = bandBitmap.GetGraphicsTarget(false, colorConverter)) {
                        // Clear the band with white.
                        object whiteBrush = new object();
                        bandTarget.CreateSolidBrush(whiteBrush, CmykColor.FromCmyk(0, 0, 0, 0));
                        bandTarget.FillRectangle(whiteBrush, RectangleF.FromLTRB(-10, -10, pixelWidth + 10, bandHeight + 10));

                        // Draw the page, in hundredths of an inch, with the band's top at the top of the bitmap.
                        Matrix transform = new Matrix();
                        transform.Scale(pixelsPerHundredth, pixelsPerHundredth);
                        transform.Translate(0, -bandTopInHundredths);
                        bandTarget.PushTransform(transform);
                        drawPage(bandTarget);
                        bandTarget.PopTransform();

                        bandTarget.FinishBitmap();
                    }

                    // Draw the band on the page. The last band may extend past the bottom of the paper.
                    RectangleF bandRect = new RectangleF(0, bandTopInHundredths, pixelWidth / pixelsPerHundredth, bandHeight / pixelsPerHundredth);
                    pageTarget.DrawBitmap(bandBitmap, bandRect, BitmapScaling.HighQuality);
                }
            }
        }

        const float MIN_DPI = 400;
        const float MAX_DPI = 1500;

        // Adjust the DPI in case of drivers that report too high or too low. Keep it a multiple/divisor of the reported DPI.
#if TEST
        internal
#endif
        static float AdjustDpi(float dpi)
        {
            while (dpi < MIN_DPI)
                dpi *= 2;

            while (dpi > MAX_DPI)
                dpi /= 2;

            return dpi;
        }

        private bool PausePrintingAfterPage(int pageNumber, out string pauseMessage)
        {
#if PORTING
            // Need to implement the pausing functionality. Maybe this goes into
            // IPrintable in some way?
#endif
            pauseMessage = null;
            return false;
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

        public class WinFormsPrinterOptions
        {
            public PageSettings PageSettings;
            public ColorModel ColorModel;
            public bool StopAfterEachPage;

            // If true, each page is drawn into bitmap bands, which are then drawn onto the printer.
            // Needed for course printing, so that purple blending is correct; not needed for
            // descriptions or punch cards.
            public bool RasterizeInBands;
            public bool PrintPreview { get; set; }
            public Size? PreviewDialogSize { get; set; }

            public WinFormsPrinterOptions Clone()
            {
                return (WinFormsPrinterOptions)MemberwiseClone();
            }
        }

    }
}
