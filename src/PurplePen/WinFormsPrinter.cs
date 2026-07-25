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

            if (this.options.ColorModel == ColorModel.OCADCompatible) {
                // OCAD uses CMYK color mode for PostScript, and RGB for other printers. Do similar
                // if OCAD compatible mode is used.
                bool isPostscript = PrinterSupportsPostScript(this.options.PageSettings.PrinterSettings.PrinterName);
                this.options.ColorModel = isPostscript ? ColorModel.CMYK : ColorModel.RGB;
            }
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


            // Wet up the graphics target.
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

            // Update page count.
            ++currentPage;
            e.HasMorePages = (currentPage < pageCount);
#if PORTING
            // Deal with pausing after each page if that option is set. 
#endif
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
            public bool PrintPreview { get; set; }
            public Size? PreviewDialogSize { get; set; }

            public WinFormsPrinterOptions Clone()
            {
                return (WinFormsPrinterOptions)MemberwiseClone();
            }
        }

        #region Postscript detection
        //By Justin Alexander, aka TheLoneCabbage

        static Int32 GETTECHNOLOGY = 20;
        static Int32 QUERYESCSUPPORT = 8;
        static Int32 POSTSCRIPT_PASSTHROUGH = 4115;
        static Int32 ENCAPSULATED_POSTSCRIPT = 4116;
        static Int32 POSTSCRIPT_IDENTIFY = 4117;
        static Int32 POSTSCRIPT_INJECTION = 4118;
        static Int32 POSTSCRIPT_DATA = 37;
        static Int32 POSTSCRIPT_IGNORE = 38;

        static bool PrinterSupportsPostScript(string printername)
        {
            List<Int32> PSChecks = new List<Int32>();
            PSChecks.Add(POSTSCRIPT_PASSTHROUGH);
            PSChecks.Add(ENCAPSULATED_POSTSCRIPT);
            PSChecks.Add(POSTSCRIPT_IDENTIFY);
            PSChecks.Add(POSTSCRIPT_INJECTION);
            PSChecks.Add(POSTSCRIPT_DATA);
            PSChecks.Add(POSTSCRIPT_IGNORE);

            IntPtr hDC = IntPtr.Zero;
            IntPtr BLOB = IntPtr.Zero;

            try {
                hDC = NativeMethods.CreateDC(null, printername, null, IntPtr.Zero);

                int isz = 4;
                BLOB = Marshal.AllocCoTaskMem(isz);
                Marshal.WriteInt32(BLOB, GETTECHNOLOGY);

                int test = NativeMethods.ExtEscape(hDC, QUERYESCSUPPORT, 4, BLOB, 0, IntPtr.Zero);
                if (test == 0) return false; // printer driver does not support GETTECHNOLOGY Checks.

                foreach (Int32 val in PSChecks) {
                    Marshal.WriteInt32(BLOB, val);
                    test = NativeMethods.ExtEscape(hDC, QUERYESCSUPPORT, isz, BLOB, 0, IntPtr.Zero);
                    if (test != 0) return true; // if any of the checks pass, return true
                }
            }
            catch (Exception) {
                return false;
            }
            finally {
                if (hDC != IntPtr.Zero) NativeMethods.DeleteDC(hDC);
                if (BLOB != IntPtr.Zero) Marshal.FreeCoTaskMem(BLOB);
            }

            return false;

        }

        static class NativeMethods
        {
            [DllImport("kernel32.dll")]
            public static extern IntPtr GlobalLock(IntPtr hMem);

            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GlobalUnlock(IntPtr hMem);

            [DllImport("kernel32.dll")]
            public static extern IntPtr GlobalFree(IntPtr hMem);

            [DllImport("kernel32.dll")]
            public static extern UIntPtr GlobalSize(IntPtr hMem);

            [DllImport("gdi32.dll")]
            public static extern int ExtEscape(IntPtr hdc, int nEscape, int cbInput, IntPtr lpszInData, int cbOutput, IntPtr lpszOutData);

            [DllImport("gdi32.dll", BestFitMapping = false, ThrowOnUnmappableChar = true)]
            public static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

            [DllImport("gdi32.dll")]
            public static extern bool DeleteDC(IntPtr hdc);
        }

        #endregion

    }
}
