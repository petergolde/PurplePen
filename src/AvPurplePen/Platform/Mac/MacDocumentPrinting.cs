// MacDocumentPrinting.cs
//
// macOS implementation of IDocumentPrinting. Documents are printed by creating a PDF and
// printing it with PDFKit (see MacPdfPrinter), so the standard macOS print panel chooses
// the printer and all the printing options. Purple Pen's own print dialogs therefore don't
// show a printer.
//
// Only compiled when targeting macOS (see AvPurplePen.csproj).

using AppKit;
using Avalonia.Controls;
using Avalonia.Platform;
using ObjCRuntime;
using PurplePen;
using System;
using System.Threading.Tasks;

namespace AvPurplePen.Platform.Mac
{
    /// <summary>
    /// Printing on macOS, always through a PDF (PrintUsingPdf is true). The printer is chosen in the
    /// macOS print panel, not through this interface (CanChangePrinter is false).
    /// </summary>
    public class MacDocumentPrinting : IDocumentPrinting
    {
        /// <summary>The printer is chosen in the macOS print panel when printing.</summary>
        public bool CanChangePrinter => false;

        /// <summary>Not used, because CanChangePrinter is false.</summary>
        public string CurrentPrinterName => "";

        /// <summary>
        /// Not supported, because CanChangePrinter is false.
        /// </summary>
        public Task<bool> ChangePrinterDialogAsync()
        {
            throw new NotSupportedException("The printer is chosen in the macOS print panel.");
        }

        /// <summary>
        /// Always true: macOS only prints through a PDF.
        /// </summary>
        public bool PrintUsingPdf => true;

        /// <summary>
        /// Not supported, because PrintUsingPdf is true.
        /// </summary>
        /// <param name="colorModel">The color model used to render the pages.</param>
        public IPrintingTarget GetPrintingTarget(ColorModel colorModel)
        {
            throw new NotSupportedException("macOS only supports printing using a PDF.");
        }

        /// <summary>
        /// Prints a PDF file, showing the macOS print panel as a sheet on the topmost Purple Pen window.
        /// Completes when the user cancels, or the job has been sent to the print system.
        /// Throws an exception if printing fails.
        /// </summary>
        /// <param name="pathName">The PDF file to print.</param>
        public async Task PrintPdfAsync(string pathName)
        {
            await MacPdfPrinter.PrintPdfAsync(pathName, GetOwnerWindow());
        }

        // Get the native window of the topmost Avalonia window, to attach the print panel to.
        private static NSWindow GetOwnerWindow()
        {
            if (DialogService.GetActiveOwner().TryGetPlatformHandle() is IMacOSTopLevelPlatformHandle handle) {
                NSWindow? window = Runtime.GetNSObject<NSWindow>(handle.NSWindow);
                if (window != null)
                    return window;
            }

            throw new InvalidOperationException("Could not get the native macOS window.");
        }
    }
}
