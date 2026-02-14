/* Copyright (c) 2006-2008, Peter Golde
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
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Drawing.Imaging;
using Draw2D = System.Drawing.Drawing2D;
using System.Windows.Forms;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

namespace PurplePen
{
    // Basic class to handle printing / print preview.
    // Must override LayoutPages and DrawPage.

    public enum ColorModel { OCADCompatible, RGB, CMYK };

    abstract class BasicPrinting
    {
        private PageSettings pageSettings;
        private string documentTitle;
        protected Controller controller;
        protected ColorModel colorModel;
        private int currentPage, totalPages;
        private bool printingToBitmaps = false;
        private bool printPreviewInProgress = false;

        public BasicPrinting(string title, Controller controller, PageSettings pageSettings, ColorModel colorModel)
        {
            this.controller = controller;
            this.pageSettings = pageSettings;
            this.documentTitle = title;

            if (colorModel == ColorModel.OCADCompatible) {
                // OCAD uses CMYK color mode for PostScript, and RGB for other printers. Do similar
                // if OCAD compatible mode is used.
                bool isPostscript = PrinterSupportsPostScript(pageSettings.PrinterSettings.PrinterName);
                colorModel = isPostscript ? ColorModel.CMYK : ColorModel.RGB;
            }

            this.colorModel = colorModel;
        }

        public bool PrintPreviewInProgress {
            get { return printPreviewInProgress; }
        }

        // Print normally (not using XPS).
        public void Print()
        {
            // Set up and position everything.
            SetupPrinting();
            printPreviewInProgress = false;

            do {
                using (PrintDocument printDocument = CreatePrintDocument()) {
                    printDocument.Print();
                }

                // If we didn't print all the pages, then we must have been doing a pause
                // between pages.
                if (currentPage < totalPages) {
                    string pauseMessage;
                    bool pause = PausePrintingAfterPage(currentPage - 1, out pauseMessage);
                    Debug.Assert(pause);
                    if (!controller.OkCancelMessage(pauseMessage, true))
                        break;
                }
            } while (currentPage < totalPages);
        }

        // Do a print preview of the descriptions.
        public void PrintPreview(Size dialogSize)
        {
            // Set up and position everything.
            SetupPrinting();
            printPreviewInProgress = true;

            using (PrintDocument printDocument = CreatePrintDocument()) {
                PrintPreviewDialog dialog = new PrintPreviewDialog();
                dialog.UseAntiAlias = true;
                dialog.Document = printDocument;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.Size = dialogSize;
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
        }

        // Get the printing area from a pageSettings.
        private SizeF GetPrintArea(PageSettings pageSettings)
        {
            return new SizeF(pageSettings.Bounds.Width - pageSettings.Margins.Left - pageSettings.Margins.Right,
                             pageSettings.Bounds.Height - pageSettings.Margins.Top - pageSettings.Margins.Bottom);
        }

        // Layout all pages, get the total number of pages, and get ready to print.
        private void SetupPrinting()
        {
            totalPages = LayoutPages(pageSettings, GetPrintArea(pageSettings));
            currentPage = 0;
        }

        // Setup the print document for printing. Should be called after SetupPrinting().
        private PrintDocument CreatePrintDocument()
        {
            PrintDocument printDocument = new System.Drawing.Printing.PrintDocument();

            printDocument.DocumentName = documentTitle;
            printDocument.PrinterSettings = pageSettings.PrinterSettings;
            printDocument.DefaultPageSettings = pageSettings;

            printDocument.PrintPage += this.PrintPage;
            printDocument.QueryPageSettings += this.QueryPageSettings;
            printDocument.EndPrint += this.EndPrint;
            printDocument.BeginPrint += this.BeginPrint;

            return printDocument;
        }

        // Do printing to a set of bitmaps. This is used for testing support.
        public Bitmap[] PrintBitmaps()
        {
            // Set up and position everything.
            printingToBitmaps = true;
            printPreviewInProgress = false;
            SetupPrinting();

            if (totalPages <= 0)
                return new Bitmap[0];

            PrintEventArgs printArgs = new PrintEventArgs();
            PrintPageEventArgs printPageArgs;
            List<Bitmap> bitmapList = new List<Bitmap>();

            BeginPrint(this, printArgs);

            do {
                // Set the page settings.
                QueryPageSettingsEventArgs queryPageSettingsArgs = new QueryPageSettingsEventArgs(pageSettings);
                QueryPageSettings(this, queryPageSettingsArgs);

                Size pageSize = pageSettings.Bounds.Size;

                Bitmap bm = new Bitmap(pageSize.Width * 2, pageSize.Height * 2, GDIPlus_GraphicsTarget.NonAlphaPixelFormat);
                bm.SetResolution(200, 200);           // using 200 dpi.

                using (Graphics g = Graphics.FromImage(bm)) {
                    g.Clear(Color.White);
                    g.SmoothingMode = Draw2D.SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    g.InterpolationMode = Draw2D.InterpolationMode.HighQualityBicubic;
                    g.ScaleTransform(2, 2);
                    Rectangle pageBounds = pageSettings.Bounds;
                    Rectangle marginBounds = Rectangle.FromLTRB(pageBounds.Left + pageSettings.Margins.Left, pageBounds.Top + pageSettings.Margins.Top, pageBounds.Right - pageSettings.Margins.Right, pageBounds.Bottom - pageSettings.Margins.Bottom);
                    printPageArgs = new PrintPageEventArgs(g, marginBounds, pageBounds, pageSettings);
                    PrintPage(this, printPageArgs);
                }

                bitmapList.Add(bm);
            } while (printPageArgs.HasMorePages);

            EndPrint(this, printArgs);

            return bitmapList.ToArray();
        }

#if XPS_PRINTING
        // Do printing to a set of bitmaps using the XPS/WPF path. This is used for testing support.
        public System.Windows.Media.Imaging.BitmapSource[] PrintXpsBitmaps(float dpi)
        {
            // Set up and position everything.
            printingToBitmaps = true;
            printPreviewInProgress = false;
            SetupPrinting();

            if (totalPages <= 0)
                return new System.Windows.Media.Imaging.BitmapSource[0];

            PrintEventArgs printArgs = new PrintEventArgs();
            List<System.Windows.Media.Imaging.BitmapSource> bitmapList = new List<System.Windows.Media.Imaging.BitmapSource>();

            BeginPrint(this, printArgs);

            float paperWidth = pageSettings.PaperSize.Width, paperHeight = pageSettings.PaperSize.Height;
            if (pageSettings.Landscape) {
                float temp = paperWidth; paperWidth = paperHeight; paperHeight = temp;
            }

            var paginator = new Paginator(this, 0, new SizeF(paperWidth, paperHeight), pageSettings.Margins, dpi, false);

            for (int pageNumber = 0; pageNumber < paginator.PageCount; ++pageNumber) {
                DocumentPage docPage = paginator.GetPage(pageNumber);

                paperWidth = (float)PointsToHundreths(docPage.Size.Width);
                paperHeight = (float)PointsToHundreths(docPage.Size.Height);

                var bitmapNew = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int) Math.Round(paperWidth * dpi / 100F), 
                    (int) Math.Round(paperHeight * dpi / 100F), 
                    dpi, dpi, System.Windows.Media.PixelFormats.Pbgra32);
                bitmapNew.Render(docPage.Visual);
                bitmapNew.Freeze();
                bitmapList.Add(bitmapNew);
            }

            EndPrint(this, printArgs);

            return bitmapList.ToArray();
        }
#endif // XPS_PRINTING

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            if (currentPage < totalPages) {
                // Get the graphics and origin relative to the page edge.
                Graphics g = e.Graphics;
                PointF origin;
                if (!printPreviewInProgress && !printingToBitmaps)
                    origin = new PointF(e.PageSettings.HardMarginX, e.PageSettings.HardMarginY);
                else
                    origin = new PointF();

                // Get the dpi of the printer.
                float dpi = Math.Max((int) g.DpiX, (int) g.DpiY);

                // Move the origin of the graphics to the margin boundaries.
                g.TranslateTransform(e.MarginBounds.Left - origin.X, e.MarginBounds.Top - origin.Y);
                SizeF size = new SizeF(e.MarginBounds.Width, e.MarginBounds.Height);

                // Draw the page.
                IGraphicsTarget graphicsTarget;
                if (colorModel == ColorModel.RGB)
                    graphicsTarget = new GDIPlus_GraphicsTarget(g);
                else if (colorModel == ColorModel.CMYK)
                    graphicsTarget = new GDIPlus_GraphicsTarget(g, new SwopColorConverter());
                else
                    throw new NotImplementedException();

                using (graphicsTarget) {
                    DrawPage(graphicsTarget, currentPage, size, dpi);
                }
                graphicsTarget = null;

                // Update page count.
                ++currentPage;
            }

            e.HasMorePages = (currentPage < totalPages) && !StopDocumentAfterPage(currentPage - 1);
        }

        // Should we pause printing after the given page.
        private bool StopDocumentAfterPage(int pageNumber)
        {
            string pauseMessage; // not used.
            return (!printPreviewInProgress && !printingToBitmaps && pageNumber < totalPages - 1 && PausePrintingAfterPage(pageNumber, out pauseMessage));
        }

        // These can be overridden in the derived class if needed. Most useful is to override QueryPageSettings
        // so that page settings can be set for each page.

        protected virtual void BeginPrint(object sender, PrintEventArgs e)
        {
        }

        protected virtual void EndPrint(object sender, PrintEventArgs e)
        {
        }

        protected virtual void QueryPageSettings(object sender, QueryPageSettingsEventArgs e)
        {
            if (currentPage < totalPages) {
                bool landscape = e.PageSettings.Landscape;
                string pausePrintingMessage = null;
                PaperSize paperSize = e.PageSettings.PaperSize;
                ChangePageSettings(currentPage, ref landscape, ref paperSize, e.PageSettings.Margins);
                if (!printPreviewInProgress && pausePrintingMessage != null) {
                    if (!controller.OkCancelMessage(pausePrintingMessage, true))
                        e.Cancel = true;
                }
                e.PageSettings.Landscape = landscape;
                e.PageSettings.PaperSize = paperSize;
            }
        }

#region Pdf Printing

        public void PrintToPdf(string pathName, bool cmykMode)
        {
            PdfWriter pdfWriter = new PdfWriter(Path.GetFileNameWithoutExtension(pathName), cmykMode);

            // Set up and position everything.
            printPreviewInProgress = false;
            printingToBitmaps = false;
            SetupPrinting();

            PrintEventArgs printArgs = new PrintEventArgs();
            BeginPrint(this, printArgs);

            while (currentPage < totalPages) {
                // Set the page settings.
                QueryPageSettingsEventArgs queryPageSettingsArgs = new QueryPageSettingsEventArgs(pageSettings);
                QueryPageSettings(this, queryPageSettingsArgs);

                Size pageSize = pageSettings.Bounds.Size;
                SizeF paperSizeInInches = new SizeF(pageSize.Width / 100F, pageSize.Height / 100F);

                Rectangle pageBounds = pageSettings.Bounds;
                Rectangle marginBounds = Rectangle.FromLTRB(pageBounds.Left + pageSettings.Margins.Left, pageBounds.Top + pageSettings.Margins.Top, pageBounds.Right - pageSettings.Margins.Right, pageBounds.Bottom - pageSettings.Margins.Bottom);
                float dpi = 1200;  // Make a PDF high resolution, although this is unlikely to matter much.

                // create and print a page.
                using (IGraphicsTarget grTarget = pdfWriter.BeginPage(paperSizeInInches)) {
                    // Move the origin of the graphics to the margin boundaries.
                    Matrix translateTransform = new Matrix();
                    translateTransform.Translate(marginBounds.Left, marginBounds.Top);
                    grTarget.PushTransform(translateTransform);
                    SizeF size = new SizeF(marginBounds.Width, marginBounds.Height);

                    DrawPage(grTarget, currentPage, size, dpi);

                    grTarget.PopTransform();

                }

                ++currentPage;
            } 

            EndPrint(this, printArgs);

            pdfWriter.Save(pathName);
        }

        #endregion

#region Xps Printing Support
#if XPS_PRINTING
        public void PrintUsingXps(bool showProgressDialog)
        {
            if (showProgressDialog)
                controller.ShowProgressDialog(true);

            try {
                // Set up and position everything.
                SetupPrinting();

                PrintQueue printQueue = GetPrintQueue(pageSettings.PrinterSettings.PrinterName);

                do {
                    PrintTicket printTicket = GetPrintTicket(printQueue, pageSettings);
                    Margins margins = pageSettings.Margins;

                    BeginPrint(this, new PrintEventArgs());

                    printQueue.CurrentJobSettings.Description = documentTitle;
                    XpsDocumentWriter documentWriter = PrintQueue.CreateXpsDocumentWriter(printQueue);
                    Paginator paginator = new Paginator(this, currentPage, new SizeF(pageSettings.PaperSize.Width, pageSettings.PaperSize.Height), margins, GetDPI(printTicket), showProgressDialog);
                    documentWriter.Write(paginator, printTicket);
                    currentPage += paginator.PageCount;

                    EndPrint(this, new PrintEventArgs());

                    // If we didn't print all the pages, then we must have been doing a pause
                    // between pages.
                    if (currentPage < totalPages) {
                        string pauseMessage;
                        bool pause = PausePrintingAfterPage(currentPage - 1, out pauseMessage);
                        Debug.Assert(pause);
                        if (!controller.OkCancelMessage(pauseMessage, true))
                            break;
                    }
                } while (currentPage < totalPages);
            }
            finally {
                if (showProgressDialog)
                    controller.EndProgressDialog();
            }
        }

        private PrintTicket GetPrintTicket(PrintQueue printQueue, System.Drawing.Printing.PageSettings pageSettings)
        {
            PrintTicketConverter printTicketConverter = new PrintTicketConverter(printQueue.FullName, printQueue.ClientPrintSchemaVersion);
            IntPtr devmodeHandle = pageSettings.PrinterSettings.GetHdevmode(pageSettings);
            int size = (int)GlobalSize(devmodeHandle);
            IntPtr devmodePtr = GlobalLock(devmodeHandle);
            byte[] devMode = new byte[size];
            Marshal.Copy(devmodePtr, devMode, 0, size);
            GlobalUnlock(devmodeHandle);
            GlobalFree(devmodeHandle);
            return printTicketConverter.ConvertDevModeToPrintTicket(devMode);
        }

        private PrintQueue GetPrintQueue(string printerName)
        {
            PrintServer server = null;

            if (printerName.StartsWith(@"\\", StringComparison.InvariantCulture)) {
                int indexOfSecondSlash = printerName.IndexOf('\\', 2);
                if (indexOfSecondSlash > 2) {
                    string serverName = printerName.Substring(0, indexOfSecondSlash);
                    printerName = printerName.Substring(indexOfSecondSlash + 1);
                    server = new PrintServer(serverName);
                }
            }

            if (server == null) {
                server = new LocalPrintServer();
            }

            return server.GetPrintQueue(printerName);
        }

        private float GetDPI(PrintTicket printTicket)
        {
            float xResolution = (printTicket.PageResolution == null) ? 1000 : (printTicket.PageResolution.X ?? 1000);
            float yResolution = (printTicket.PageResolution == null) ? 1000 : (printTicket.PageResolution.Y ?? 1000);

            float dpi = Math.Max(xResolution, yResolution);
            return dpi;
        }

        // Convert 1/100 of an inch to 1/96 of an inch.
        private static double HundrethsToPoints(double hundreths)
        {
            return hundreths / 100.0 * 96.0;
        }

        // Convert 1/96 of an inch to 1/100 of an inch.
        private static double PointsToHundreths(double points)
        {
            return points / 96.0 * 100.0;
        }



        private class Paginator : DocumentPaginator
        {
            private BasicPrinting outer;
            private int startingPage, pageCount;
            private System.Windows.Size pageSize;  // page size in 1/96 of an inch.
            private Margins margins;  // margins in 1/100 of an inch.
            private float dpi;
            private bool showProgressDialog;

            public Paginator(BasicPrinting outer, int startingPage, SizeF pageSize, Margins margins, float dpi, bool showProgressDialog)
            {
                this.outer = outer;
                this.startingPage = startingPage;
                this.margins = margins;
                this.dpi = dpi;
                this.pageSize = new System.Windows.Size(HundrethsToPoints(pageSize.Width), HundrethsToPoints(pageSize.Height));
                this.showProgressDialog = showProgressDialog;

                pageCount = CountPages();
            }

            public override DocumentPage GetPage(int pageNumber)
            {
                // This is called starting at zero, but we are really starting at the given page.
                pageNumber += startingPage;

                if (showProgressDialog) {
                    if (outer.controller.UpdateProgressDialog(string.Format(MiscText.PrintingPage, pageNumber + 1, outer.totalPages), (double)pageNumber / (double)outer.totalPages))
                        throw new Exception(MiscText.CancelledByUser);
                }
                
                Margins margins = new Margins(this.margins.Left, this.margins.Right, this.margins.Top, this.margins.Bottom);
                bool landscape = outer.pageSettings.Landscape;
                PaperSize paperSize = outer.pageSettings.PaperSize;
                bool rotate;
                outer.ChangePageSettings(pageNumber, ref landscape, ref paperSize, margins);
                rotate = (landscape != outer.pageSettings.Landscape);
                this.pageSize = new System.Windows.Size(HundrethsToPoints(paperSize.Width), HundrethsToPoints(paperSize.Height));
                if (outer.pageSettings.Landscape) {
                    this.pageSize = new System.Windows.Size(this.pageSize.Height, this.pageSize.Width);
                }

                // Get margins in terms of normal page orientation, in points.
                double leftMargin = HundrethsToPoints(rotate ? margins.Bottom : margins.Left);
                double rightMargin = HundrethsToPoints(rotate ? margins.Top : margins.Right);
                double topMargin = HundrethsToPoints(rotate ? margins.Left : margins.Top);
                double bottomMargin = HundrethsToPoints(rotate ? margins.Right : margins.Bottom);
                System.Windows.Rect contentRect = new System.Windows.Rect(leftMargin, topMargin, pageSize.Width - leftMargin - rightMargin, pageSize.Height - topMargin - bottomMargin);
                System.Windows.Rect boundingRect = new System.Windows.Rect(0, 0, pageSize.Width - leftMargin - rightMargin, pageSize.Height - topMargin - bottomMargin);
                System.Windows.Media.DrawingVisual visual = new System.Windows.Media.DrawingVisual();

                using (System.Windows.Media.DrawingContext dc = visual.RenderOpen()) {
                    if (outer.printingToBitmaps) {
                        // This is kind of hacky way to get the printing to bitmaps white, but much easier than the alternative.
                        dc.DrawRectangle(System.Windows.Media.Brushes.White, null, new System.Windows.Rect(-1, -1, pageSize.Width + 2, pageSize.Height + 2));
                    }

                    // Clip to the bounding rect within margins.
                    dc.PushClip(new System.Windows.Media.RectangleGeometry(boundingRect));

                    if (rotate) {
                        // Rotate and translate to handle landscape mode.
                        dc.PushTransform(new System.Windows.Media.TranslateTransform(0, boundingRect.Height));
                        dc.PushTransform(new System.Windows.Media.RotateTransform(-90));
                    }

                    // Scale to hundreths of an inch instead of points (1/96 of inch).
                    dc.PushTransform(new System.Windows.Media.ScaleTransform(96.0 / 100.0, 96.0 / 100.0));

                    IGraphicsTarget graphicsTarget;
                    if (outer.colorModel == ColorModel.RGB)
                        graphicsTarget = new WPF_GraphicsTarget(dc);
                    else if (outer.colorModel == ColorModel.CMYK)
                        graphicsTarget = new WPF_GraphicsTarget(dc, new WPFSwopColorConverter());
                    else
                        throw new NotImplementedException();

                    using (graphicsTarget) {
                        outer.DrawPage(graphicsTarget, pageNumber, new SizeF((float)contentRect.Width, (float)contentRect.Height), dpi);
                    }
                    graphicsTarget = null;
                }

                return new DocumentPage(visual, pageSize, contentRect, contentRect);
            }

            public override bool IsPageCountValid
            {
                get { return true; }
            }

            public override int PageCount
            {
                get { return pageCount; }
            }

            // Count the number of pages we are going to print, taking into account stopping
            // after some pages.
            int CountPages()
            {
                int p = startingPage, count = 0;
                do {
                    count += 1;
                    p += 1;
                } while (p < outer.totalPages && !outer.StopDocumentAfterPage(p - 1));

                return count;
            }

            public override System.Windows.Size PageSize
            {
                get { return pageSize; }
#pragma warning disable RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter
                set { return; }
#pragma warning restore RECS0029 // Warns about property or indexer setters and event adders or removers that do not use the value parameter
            }

            public override IDocumentPaginatorSource Source
            {
                get { return null; }
            }
        }
#endif // XPS_PRINTING
#endregion Xps Printing Support

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
#endregion

        // Routine to layout all the pages. Must return the number of pages to print. "printArea" is the size
        // without the margins as set in the pageSettings. 
        protected abstract int LayoutPages(PageSettings pageSettings, SizeF printArea);

        // The core printing routine. The origin of the graphics is the upper-left of the margins,
        // and the printArea is the size to draw into (in hundreths of an inch), within the margins.
        // dpi is the resolution of the printing in dots per inch.
        protected abstract void DrawPage(IGraphicsTarget graphicsTarget, int pageNumber, SizeF printArea, float dpi);

        // Routine to change page settings for a particular page.
        protected virtual void ChangePageSettings(int pageNumber, ref bool landscape, ref PaperSize paperSize, Margins margins)
        {
        }

        // Should we pause printing after this page?
        protected virtual bool PausePrintingAfterPage(int pageNumber, out string pauseMessage)
        {
            pauseMessage = null;
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
    }
}

