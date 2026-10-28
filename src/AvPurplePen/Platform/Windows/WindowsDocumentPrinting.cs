// WindowsDocumentPrinting.cs
//
// Windows implementation of IDocumentPrinting, using the Windows Forms printing API.
// Keeps the current printer and its settings (including the driver-specific DEVMODE
// options set with the printer's Properties button), shared by all printing for the
// session, and lets the user change them with the Windows Forms PrintDialog.
// Ported from the printer handling in the WinForms print dialogs (e.g.
// PurplePen/PrintDescriptions.cs, printerChange_Click).
//
// Only compiled when targeting Windows (see AvPurplePen.csproj).

using Avalonia.Controls;
using Avalonia.Platform;
using PurplePen;
using System;
using System.Drawing.Printing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AvPurplePen.Platform.Windows
{
    /// <summary>
    /// Direct printing on Windows. The printer can be changed (CanChangePrinter is true);
    /// printing goes through <see cref="WindowsPrinter"/>.
    /// </summary>
    public class WindowsDocumentPrinting : IDocumentPrinting
    {
        // The current printer and its settings. Initially the default printer.
        private PrinterSettings printerSettings;

        /// <summary>
        /// Create the Windows printing service, with the system default printer as the current printer.
        /// </summary>
        public WindowsDocumentPrinting()
        {
            // Common dialogs shown through Windows Forms (the print dialog and the printer's
            // Properties dialog) only use the themed common controls if visual styles are enabled.
            System.Windows.Forms.Application.EnableVisualStyles();

            printerSettings = new PrinterSettings();
        }

        /// <summary>The printer can always be changed on Windows.</summary>
        public bool CanChangePrinter => true;

        /// <summary>The name of the current printer.</summary>
        public string CurrentPrinterName => printerSettings.PrinterName;

        /// <summary>
        /// Shows the Windows print dialog, parented to the topmost Avalonia window, to change
        /// the current printer and its settings. The number of copies chosen in the dialog
        /// is ignored; Purple Pen's own print dialogs choose what to print.
        /// Returns true if the user accepted the dialog, false if it was cancelled.
        /// </summary>
        public Task<bool> ChangePrinterDialogAsync()
        {
            using (PrintDialog printDialog = new PrintDialog()) {
                printDialog.UseEXDialog = false;  // EXDialog is buggy, and also clumsier. But mostly just plain buggy.
                printDialog.AllowPrintToFile = false;

                // Edit a copy, so that cancelling leaves the current settings unchanged.
                printDialog.PrinterSettings = (PrinterSettings)printerSettings.Clone();

                if (printDialog.ShowDialog(GetOwnerWindow()) != DialogResult.OK)
                    return Task.FromResult(false);

                printerSettings = printDialog.PrinterSettings;
                printerSettings.Copies = 1; // ignore copies from the print settings dialog.
                return Task.FromResult(true);
            }
        }

        /// <summary>
        /// Gets a printing target that prints to the current printer.
        /// </summary>
        /// <param name="colorModel">The color model used to render the pages.</param>
        public IPrintingTarget GetPrintingTarget(ColorModel colorModel)
        {
            // Give the printer its own copy, so changing printers later doesn't affect it.
            return new WindowsPrinter((PrinterSettings)printerSettings.Clone(), colorModel);
        }

        // Get the topmost Avalonia window, as a Windows Forms owner for a dialog. Returns null
        // if the window has no native handle, in which case Windows Forms uses the active window.
        private static IWin32Window? GetOwnerWindow()
        {
            IPlatformHandle? platformHandle = DialogService.GetActiveOwner().TryGetPlatformHandle();
            if (platformHandle == null)
                return null;
            return new Win32Owner(platformHandle.Handle);
        }

        // Wraps a native window handle as a Windows Forms dialog owner.
        private sealed class Win32Owner : IWin32Window
        {
            public Win32Owner(IntPtr handle)
            {
                Handle = handle;
            }

            public IntPtr Handle { get; }
        }
    }
}
