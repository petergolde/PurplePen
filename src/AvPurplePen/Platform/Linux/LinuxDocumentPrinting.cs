// LinuxDocumentPrinting.cs
//
// Linux implementation of IDocumentPrinting. Documents are printed by creating a PDF and
// handing it to the XDG desktop portal's Print interface (see LinuxPdfPrinter), so the
// desktop's own print dialog chooses the printer and all the printing options. Purple Pen's
// own print dialogs therefore don't show a printer.
//
// If the print portal isn't available (e.g. on a minimal install or a tiling window manager
// with no portal backend that implements Print), the user is offered to open the PDF in the
// default PDF viewer instead (see LinuxPdfViewer), and print it from there.
//
// Only compiled for the plain net10.0 target framework (see AvPurplePen.csproj), and only
// registered when running on Linux (see Program.cs).

using PurplePen;
using PurplePen.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AvPurplePen.Platform.Linux
{
    /// <summary>
    /// Printing on Linux, always through a PDF (PrintUsingPdf is true). The printer is chosen in the
    /// desktop's print dialog, not through this interface (CanChangePrinter is false).
    /// </summary>
    public class LinuxDocumentPrinting : IDocumentPrinting
    {
        // The directory, under the temporary directory, holding copies of PDFs opened in the PDF viewer.
        private const string ViewerDirectoryName = "PurplePen-Print";

        // Copies of PDFs opened in the PDF viewer are deleted the next time a PDF is opened, once they are this old.
        private static readonly TimeSpan ViewerFileLifetime = TimeSpan.FromDays(1);

        // The settings the print dialog starts with. Always print at 100%: "cups-" keys are passed to CUPS as
        // job options without the prefix, so this becomes the IPP option print-scaling=none.
        private static readonly IReadOnlyDictionary<string, string> InitialPrintSettings = new Dictionary<string, string> {
            ["cups-print-scaling"] = "none"
        };

        /// <summary>The printer is chosen in the desktop's print dialog when printing.</summary>
        public bool CanChangePrinter => false;

        /// <summary>Not used, because CanChangePrinter is false.</summary>
        public string CurrentPrinterName => "";

        /// <summary>
        /// Not supported, because CanChangePrinter is false.
        /// </summary>
        public Task<bool> ChangePrinterDialogAsync()
        {
            throw new NotSupportedException("The printer is chosen in the desktop's print dialog.");
        }

        /// <summary>
        /// Always true: Linux only prints through a PDF.
        /// </summary>
        public bool PrintUsingPdf => true;

        /// <summary>
        /// Not supported, because PrintUsingPdf is true.
        /// </summary>
        /// <param name="colorModel">The color model used to render the pages.</param>
        public IPrintingTarget GetPrintingTarget(ColorModel colorModel)
        {
            throw new NotSupportedException("Linux only supports printing using a PDF.");
        }

        /// <summary>
        /// Prints a PDF file through the desktop's print dialog, attached to the topmost Purple Pen window.
        /// If the print portal isn't available, offers to open the PDF in the default PDF viewer instead.
        /// Completes when the user cancels, the job has been sent to the print system, or the viewer has been
        /// launched. Throws an exception if printing or opening the viewer fails.
        /// </summary>
        /// <param name="pathName">The PDF file to print.</param>
        public async Task PrintPdfAsync(string pathName)
        {
            if (await LinuxPdfPrinter.IsAvailableAsync()) {
                // The label of the print dialog's Print button. Reuses the (already translated) text of the Print button in Purple Pen's print dialogs.
                await LinuxPdfPrinter.PrintPdfAsync(pathName, DialogService.GetActiveOwner(), null,
                    UIText.PrintDescriptions_printButton_Text, InitialPrintSettings);
            }
            else {
                await OfferPdfViewerAsync(pathName);
            }
        }

        // Tell the user that the desktop can't print directly, and offer to open the PDF in the default PDF viewer
        // so they can print from there. Throws an exception if the viewer can't be opened.
        private static async Task OfferPdfViewerAsync(string pathName)
        {
            MessageBoxDialogViewModel vm = new MessageBoxDialogViewModel {
                Message = UIText.LinuxPrinting_PortalUnavailable,
                Buttons = MessageBoxButtons.YesNo,
                DefaultButton = MessageBoxButton.Yes,
                Icon = MessageBoxIcon.Warning
            };
            await Services.DialogService.ShowDialogAsync(vm);
            if (vm.ChosenButton != MessageBoxButton.Yes)
                return;

            // The caller deletes the PDF once this returns, but the viewer needs it for as long as it is open.
            string viewerPath = CopyForViewer(pathName);

            try {
                await LinuxPdfViewer.OpenAsync(viewerPath);
            }
            catch (PdfViewerException e) {
                string message;
                switch (e.Error) {
                    case PdfViewerError.CommandNotFound:
                        message = UIText.LinuxPrinting_XdgUtilsMissing;
                        break;
                    case PdfViewerError.NoDefaultViewer:
                        message = UIText.LinuxPrinting_NoDefaultViewer;
                        break;
                    default:
                        message = string.Format(UIText.LinuxPrinting_ViewerFailed, e.Message);
                        break;
                }
                throw new InvalidOperationException(message, e);
            }
        }

        // Copy a PDF file to its own directory under ViewerDirectoryName, keeping its file name (which the
        // viewer may show), and return the path of the copy. Copies left from earlier prints are deleted once
        // they are old enough that the viewer has very likely been closed.
        private static string CopyForViewer(string pathName)
        {
            string viewerDirectory = Path.Combine(Path.GetTempPath(), ViewerDirectoryName);
            DeleteOldViewerFiles(viewerDirectory);

            string copyDirectory = Path.Combine(viewerDirectory, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(copyDirectory);
            string copyPath = Path.Combine(copyDirectory, Path.GetFileName(pathName));
            File.Copy(pathName, copyPath);
            return copyPath;
        }

        // Delete the subdirectories of viewerDirectory older than ViewerFileLifetime. Failures are ignored:
        // leaving a temporary file behind is harmless.
        private static void DeleteOldViewerFiles(string viewerDirectory)
        {
            try {
                if (!Directory.Exists(viewerDirectory))
                    return;

                DateTime cutoff = DateTime.UtcNow - ViewerFileLifetime;
                foreach (string directory in Directory.GetDirectories(viewerDirectory)) {
                    try {
                        if (Directory.GetLastWriteTimeUtc(directory) < cutoff)
                            Directory.Delete(directory, true);
                    }
                    catch (IOException) {
                    }
                    catch (UnauthorizedAccessException) {
                    }
                }
            }
            catch (IOException) {
            }
            catch (UnauthorizedAccessException) {
            }
        }
    }
}
