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
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using System.Printing;
using System.Printing.Interop;
using System.Runtime.InteropServices;
using System.Windows.Xps;
using System.Windows.Documents;

namespace PurplePen
{
    // Basic class to handle printing / print preview.
    // Must override LayoutPages and DrawPage.

    abstract class BasicPrinting: Component
    {
        private PageSettings pageSettings;
        protected PrintDocument printDocument;
        private int currentPage, totalPages;
        private bool printingToBitmaps = false;
        private bool useXpsPrinting;

        // These are used only for XPS printing:
        private PrintQueue printQueue;
        private PrintTicket printTicket;

        public BasicPrinting(string title, PageSettings pageSettings, bool useXpsPrinting)
        {
            InitializeComponent();

            this.pageSettings = pageSettings;
            this.useXpsPrinting = useXpsPrinting;

            printDocument.DocumentName = title;
            printDocument.PrinterSettings = pageSettings.PrinterSettings;
            printDocument.DefaultPageSettings = pageSettings;
        }

        private void InitializeComponent()
        {
            this.printDocument = new System.Drawing.Printing.PrintDocument();
            // 
            // printDocument
            // 
            this.printDocument.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(this.PrintPage);
            this.printDocument.QueryPageSettings += new System.Drawing.Printing.QueryPageSettingsEventHandler(this.QueryPageSettings);
            this.printDocument.EndPrint += new System.Drawing.Printing.PrintEventHandler(this.EndPrint);
            this.printDocument.BeginPrint += new System.Drawing.Printing.PrintEventHandler(this.BeginPrint);

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                printDocument.Dispose();
            base.Dispose(disposing);
        }

        // Print the descriptions.
        public void Print()
        {
            // Set up and position everything.
            SetupPrinting();

            if (useXpsPrinting) {
                PrintUsingXps();
            }
            else {
                printDocument.Print();
            }
        }

        // Do a print preview of the descriptions.
        public void PrintPreview(Size dialogSize)
        {
            // Set up and position everything.
            SetupPrinting();

            PrintPreviewDialog dialog = new PrintPreviewDialog();
            dialog.UseAntiAlias = true;
            dialog.Document = printDocument;
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.Size = dialogSize;
            dialog.SizeGripStyle = SizeGripStyle.Show;
            dialog.ShowIcon = false;

            dialog.ShowDialog();
            dialog.Dispose();
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
        }

        // Do printing to a set of bitmaps. This is used for testing support.
        public Bitmap[] PrintBitmaps()
        {
            // Set up and position everything.
            printingToBitmaps = true;
            SetupPrinting();

            if (totalPages <= 0)
                return new Bitmap[0];

            PrintEventArgs printArgs = new PrintEventArgs();
            PrintPageEventArgs printPageArgs;
            List<Bitmap> bitmapList = new List<Bitmap>();

            printDocument.PrintController = new StandardPrintController(); //new PreviewPrintController();
            printDocument.PrinterSettings = pageSettings.PrinterSettings;
            BeginPrint(this, printArgs);

            do {
                // Set the page settings.
                QueryPageSettingsEventArgs queryPageSettingsArgs = new QueryPageSettingsEventArgs(pageSettings);
                QueryPageSettings(this, queryPageSettingsArgs);

                Size pageSize = pageSettings.Bounds.Size;        

                Bitmap bm = new Bitmap(pageSize.Width * 2, pageSize.Height * 2, PixelFormat.Format24bppRgb);
                bm.SetResolution(200, 200);           // using 200 dpi.

                using (Graphics g = Graphics.FromImage(bm)) {
                    g.Clear(Color.White);
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
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

        private void PrintPage(object sender, PrintPageEventArgs e)
        {
            if (currentPage < totalPages) {
                // Get the graphics and origin relative to the page edge.
                Graphics g = e.Graphics;
                PointF origin;
                if (!printDocument.PrintController.IsPreview && !printingToBitmaps)
                    origin = new PointF(e.PageSettings.HardMarginX, e.PageSettings.HardMarginY);
                else
                    origin = new PointF();

                // Get the dpi of the printer.
                float dpi = Math.Max((int) g.DpiX, (int) g.DpiY);

                // Move the origin of the graphics to the margin boundaries.
                g.TranslateTransform(e.MarginBounds.Left - origin.X, e.MarginBounds.Top - origin.Y);
                SizeF size = new SizeF(e.MarginBounds.Width, e.MarginBounds.Height);

                // Draw the page.
                using (IGraphicsTarget graphicsTarget = new GDIPlus_GraphicsTarget(g)) {
                    DrawPage(graphicsTarget, currentPage, size, dpi);
                }

                // Update page count.
                ++currentPage;
            }

            e.HasMorePages = (currentPage < totalPages);
        }

        // These can be overridden in the derived class if needed. Most useful is to override QueryPageSettings
        // so that page settings can be set for each page.

        protected virtual void BeginPrint(object sender, PrintEventArgs e)
        {
            currentPage = 0;
        }

        protected virtual void EndPrint(object sender, PrintEventArgs e)
        {
        }

        protected virtual void QueryPageSettings(object sender, QueryPageSettingsEventArgs e)
        {
            if (currentPage < totalPages) {
                bool landscape = e.PageSettings.Landscape;
                ChangePageSettings(currentPage, ref landscape, e.PageSettings.Margins);
                e.PageSettings.Landscape = landscape;
            }
        }

        // Routine to change page settings for a particular page.
        protected virtual void ChangePageSettings(int pageNumber, ref bool landscape, Margins margins)
        {
        }

        #region Xps Printing Support

        private void PrintUsingXps()
        {
            printQueue = GetPrintQueue(pageSettings.PrinterSettings.PrinterName);
            printTicket = GetPrintTicket(printQueue, pageSettings);
            Margins margins = pageSettings.Margins;

            BeginPrint(this, new PrintEventArgs());

            XpsDocumentWriter documentWriter = PrintQueue.CreateXpsDocumentWriter(printQueue);
            documentWriter.Write(new Paginator(this, margins), printTicket);

            EndPrint(this, new PrintEventArgs());
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

            if (printerName.StartsWith(@"\\")) {
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

        private class Paginator : DocumentPaginator
        {
            private BasicPrinting outer;
            private System.Windows.Size pageSize;
            private Margins margins;  // margins in 1/100 of an inch.

            public Paginator(BasicPrinting outer, Margins margins)
            {
                this.outer = outer;
                this.margins = margins;
            }

            public override DocumentPage GetPage(int pageNumber)
            {
                System.Windows.Media.DrawingVisual visual = new System.Windows.Media.DrawingVisual();
                System.Windows.Rect contentRect = new System.Windows.Rect(margins.Left, margins.Top, pageSize.Width - margins.Left - margins.Right, pageSize.Height - margins.Top - margins.Bottom);
                using (System.Windows.Media.DrawingContext dc = visual.RenderOpen()) {
                    IGraphicsTarget graphicsTarget = new WPF_GraphicsTarget(dc);
                    // UNDONE: DPI.
                    outer.DrawPage(graphicsTarget, pageNumber, new SizeF((float)contentRect.Width, (float)contentRect.Height), 600F);
                }

                return new DocumentPage(visual, pageSize, contentRect, contentRect);
            }

            public override bool IsPageCountValid
            {
                get { return true; }
            }

            public override int PageCount
            {
                get { return outer.totalPages; }
            }

            public override System.Windows.Size PageSize
            {
                get { return pageSize; }
                set { pageSize = value; }
            }

            public override IDocumentPaginatorSource Source
            {
                get { return null; }
            }
        }

        #endregion Xps Printing Support

        // Routine to layout all the pages. Must return the number of pages to print. "printArea" is the size
        // without the margins as set in the pageSettings. 
        protected abstract int LayoutPages(PageSettings pageSettings, SizeF printArea);

        // The core printing routine. The origin of the graphics is the upper-left of the margins,
        // and the printArea is the size to draw into (in hundreths of an inch), within the margins.
        // dpi is the resolution of the printing in dots per inch.
        protected abstract void DrawPage(IGraphicsTarget graphicsTarget, int pageNumber, SizeF printArea, float dpi);

        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        static extern IntPtr GlobalFree(IntPtr hMem);

        [DllImport("kernel32.dll")]
        static extern UIntPtr GlobalSize(IntPtr hMem);
    }
}

