// MacPdfPrinter.cs
//
// Prints a PDF file on macOS with PDFKit, showing the standard macOS print panel (which
// chooses the printer, paper and other options) as a sheet on a window. The panel starts
// with the paper size of the PDF's first page.
// Based on the TestPdfKit sample's MacPdfPrinter.
//
// Only compiled when targeting macOS (see AvPurplePen.csproj).

using AppKit;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using PdfKit;
using PurplePen;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AvPurplePen.Platform.Mac
{
    /// <summary>
    /// Prints PDF files through PDFKit and the macOS print panel.
    /// </summary>
    internal static class MacPdfPrinter
    {
        // The page margins, in points (1 inch). See ResetPerDocumentSettings.
        private const float PageMargin = 72;

        // AppKit's value for NSPrintInfo.JobDisposition when the user cancels.
        private const string CancelJobDisposition = "NSPrintCancelJob";

        // The IPP/CUPS job option that controls whether the printer scales pages to fit the paper.
        private const string PrintScalingKey = "print-scaling";

        // Handlers for print sheets currently on screen. AppKit doesn't retain the modal delegate, so without this
        // the handler (and, through its awaiting continuation, the document and operation) could be garbage
        // collected before the sheet closes.
        private static readonly HashSet<PrintCompletionHandler> pendingHandlers = new HashSet<PrintCompletionHandler>();

        /// <summary>
        /// Shows the standard macOS print panel as a sheet attached to the window and prints the PDF.
        /// Completes once the print panel is closed and, if the user chose Print, the job has been sent to the
        /// print system. Must be called on the main (UI) thread.
        /// </summary>
        /// <param name="filePath">The PDF file to print.</param>
        /// <param name="window">The window to attach the print panel to.</param>
        /// <returns>True if the job was printed, false if the user cancelled.</returns>
        /// <exception cref="InvalidOperationException">The PDF couldn't be printed.</exception>
        public static async Task<bool> PrintPdfAsync(string filePath, NSWindow window)
        {
            using (NSUrl url = NSUrl.FromFilename(filePath))
            using (PdfDocument document = OpenDocument(url)) {
                // Start from the app's last-used settings, but not the per-document ones.
                using (NSPrintInfo printInfo = (NSPrintInfo)NSPrintInfo.SharedPrintInfo.Copy()) {
                    SetPaperSizeFromDocument(printInfo, document);
                    ResetPerDocumentSettings(printInfo);

                    using (NSPrintOperation? operation = document.GetPrintOperation(printInfo, PdfPrintScalingMode.None, true)) {  // Automatically rotate pages
                        if (operation == null)
                            throw new InvalidOperationException(MiscText.CannotSendPrintJob);

                        operation.ShowsPrintPanel = true;
                        operation.ShowsProgressPanel = true;

                        PrintCompletionHandler handler = new PrintCompletionHandler();
                        pendingHandlers.Add(handler);
                        bool success;
                        try {
                            operation.RunOperationModal(window, handler, PrintCompletionHandler.DidRunSelector, IntPtr.Zero);
                            success = await handler.Completion;
                        }
                        finally {
                            pendingHandlers.Remove(handler);
                            handler.Dispose();
                        }

                        if (success) {
                            // Remember the user's choices (printer, paper, etc.) for the next print from this app.
                            NSPrintInfo.SharedPrintInfo = operation.PrintInfo;
                            return true;
                        }

                        // AppKit reports cancel and failure the same way; a cancel also sets the job disposition.
                        if (operation.PrintInfo.JobDisposition == CancelJobDisposition)
                            return false;

                        throw new InvalidOperationException(MiscText.CannotSendPrintJob);
                    }
                }
            }
        }

        // Open a PDF file as a PDFKit document. Throws an exception if it can't be opened or printed.
        private static PdfDocument OpenDocument(NSUrl url)
        {
            PdfDocument document;
            try {
                document = new PdfDocument(url);
            }
            catch (Exception) {
                throw new InvalidOperationException(MiscText.CannotSendPrintJob);
            }

            if (document.IsLocked || !document.AllowsPrinting) {
                document.Dispose();
                throw new InvalidOperationException(MiscText.CannotSendPrintJob);
            }

            return document;
        }

        // Set the paper size (and orientation) in printInfo to the size of the first page of the document, so that
        // the print panel starts with the paper chosen when the PDF was created. AppKit matches the size, within a small
        // tolerance, to one of the printer's papers (e.g. "iso-a4"), and picks landscape orientation if the size is wider
        // than it is tall. The user can change the paper in the print panel.
        // If the printer has no matching paper, AppKit would instead make a custom paper of that size, which the printer
        // can't print, and the printing system would then scale the page to fit the paper actually used. So in that case
        // the paper is left unchanged, and PDFKit prints the page centered on it at 100%, clipping what doesn't fit.
        private static void SetPaperSizeFromDocument(NSPrintInfo printInfo, PdfDocument document)
        {
            if (document.PageCount == 0)
                return;

            using (PdfPage? page = document.GetPage(0)) {
                if (page == null)
                    return;

                CGRect bounds = page.GetBoundsForBox(PdfDisplayBox.Media);
                CGSize pageSize = bounds.Size;
                if (page.Rotation % 180 != 0)
                    pageSize = new CGSize(pageSize.Height, pageSize.Width);

                if (pageSize.Width > 0 && pageSize.Height > 0 && PrinterHasPaperSize(printInfo, pageSize))
                    printInfo.PaperSize = pageSize;
            }
        }

        // Check whether the printer in printInfo has a paper matching the given size (in either orientation), by trying
        // the size on a copy of printInfo. AppKit names a matched paper after the printer's paper; an unmatched size gets
        // a custom paper with a generated name that the printer doesn't know.
        private static bool PrinterHasPaperSize(NSPrintInfo printInfo, CGSize paperSize)
        {
            using (NSPrintInfo trialPrintInfo = (NSPrintInfo)printInfo.Copy()) {
                trialPrintInfo.PaperSize = paperSize;
                string? paperName = trialPrintInfo.PaperName;
                if (string.IsNullOrEmpty(paperName))
                    return false;

                CGSize printerPaperSize = trialPrintInfo.Printer.PageSizeForPaper(paperName);
                return printerPaperSize.Width > 0 && printerPaperSize.Height > 0;
            }
        }

        // Reset the settings in printInfo that belong to a single document (page range, copies, scaling), rather
        // than to the printer.
        private static void ResetPerDocumentSettings(NSPrintInfo printInfo)
        {
            NSMutableDictionary settings = printInfo.Dictionary;
            settings["NSPrintAllPages"] = NSNumber.FromBoolean(true);
            settings.Remove(new NSString("NSPrintFirstPage"));
            settings.Remove(new NSString("NSPrintLastPage"));
            settings["NSPrintCopies"] = NSNumber.FromInt32(1);
            printInfo.SelectionOnly = false;

            // Print at exactly 100%, positioned exactly on the paper. PDFKit centers the page within the area that is
            // both inside the margins and printable, so equal margins at least as large as any printer's
            // non-printable edge keep that area centered on the paper. Margins don't clip the page.
            printInfo.ScalingFactor = 1.0f;
            printInfo.HorizontallyCentered = true;
            printInfo.VerticallyCentered = true;
            printInfo.LeftMargin = PageMargin;
            printInfo.RightMargin = PageMargin;
            printInfo.TopMargin = PageMargin;
            printInfo.BottomMargin = PageMargin;

            // Also tell the printer not to scale. If the job's paper isn't what the printer has (e.g. the user picks a
            // paper the printer can't print), many printers shrink the page to fit by default (IPP print-scaling "auto").
            // Print settings keys named like CUPS options are sent to CUPS as job options.
            settings[PrintScalingKey] = new NSString("none");
        }

        // The modal delegate for a print operation: completes a task when the print panel and printing are finished.
        private sealed class PrintCompletionHandler : NSObject
        {
            public static readonly Selector DidRunSelector = new Selector("printOperationDidRun:success:contextInfo:");

            private readonly TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Completes with whether the print operation succeeded.
            public Task<bool> Completion => completion.Task;

            [Export("printOperationDidRun:success:contextInfo:")]
            private void PrintOperationDidRun(NSPrintOperation printOperation, bool success, IntPtr contextInfo)
            {
                completion.TrySetResult(success);
            }
        }
    }
}
