// PdfLoadingUI.cs
//
// Avalonia implementation of IPdfLoadingStatus. Drives the "Reading PDF"
// dialog (PdfConversionInProgressDialog) while the PDFium converter reads a
// PDF map file on a background thread.
//
// The IPdfLoadingStatus contract is awkward for an async UI: ShowLoadingStatus
// is a *synchronous* call that must block (on the UI thread) until the dialog
// is dismissed, while LoadingComplete is raised on the converter's *background*
// thread. Until the interface can be made async, we mirror the WinForms
// approach: show the dialog as an owned/modal window and pump the UI thread via
// IEventDispatcherService.ProcessPendingMessages (the same mechanism the
// progress dialog in MainWindowViewModel_IUserInterface uses) until the dialog
// closes.
//
// Migrated from WinForms PurplePen/PdfLoadingUI.cs.

using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using PurplePen;
using PurplePen.ViewModels;

namespace AvPurplePen
{
    /// <summary>
    /// Shows and manages the "Reading PDF" dialog while a PDF map file is
    /// converted. One instance is used per conversion (registered transient),
    /// so its completion state is not shared across conversions.
    /// </summary>
    public class PdfLoadingUI : IPdfLoadingStatus
    {
        // How often (ms) the UI-thread wait loop yields between pumping the
        // message queue, to avoid a 100%-CPU busy spin while waiting for the
        // conversion to finish or the user to cancel.
        private const int PumpIntervalMs = 15;

        // How often (ms) the background thread polls for the dialog to close
        // after a failure (matching the WinForms implementation).
        private const int PollIntervalMs = 50;

        // Guards the completion state below and the dialog handle, which are
        // touched from both the UI thread (ShowLoadingStatus) and the
        // converter's background thread (LoadingComplete).
        private readonly object lockObj = new object();

        private INonModalDialog<PdfConversionInProgressDialogViewModel>? dialog;
        private bool complete;
        private bool success;
        private string errorMessage = "";

        /// <summary>
        /// Shows the "Reading PDF" dialog and blocks (pumping the UI thread)
        /// until it is dismissed. Returns true if the conversion succeeded
        /// (the dialog was closed programmatically by <see cref="LoadingComplete"/>),
        /// or false if the user cancelled or the conversion failed.
        /// </summary>
        /// <param name="fileName">The PDF file being read (unused; kept for the interface).</param>
        public bool ShowLoadingStatus(string fileName)
        {
            PdfConversionInProgressDialogViewModel vm = new PdfConversionInProgressDialogViewModel();
            INonModalDialog<PdfConversionInProgressDialogViewModel> handle;

            lock (lockObj) {
                // The conversion may have finished before we got here.
                if (complete && success)
                    return true;                      // Nothing to show.
                if (complete && !success)
                    vm.ShowErrorMessage(errorMessage); // Open straight into the error state.

                // Show as a classic modal owned dialog (owner disabled). This
                // returns immediately with a handle; we do the blocking below.
                handle = Services.DialogService.ShowOwnedDialog(vm, disableOwner: true);
                dialog = handle;
            }

            // Block until the dialog closes, pumping the UI so the marquee
            // animates, the Cancel button responds, and LoadingComplete's
            // posted Close()/ShowErrorMessage actions run.
            IEventDispatcherService dispatcher =
                Services.ServiceProvider.GetRequiredService<IEventDispatcherService>();
            while (!handle.ClosedTask.IsCompleted) {
                dispatcher.ProcessPendingMessages();
                if (!handle.ClosedTask.IsCompleted)
                    Thread.Sleep(PumpIntervalMs);
            }

            // Success → we (LoadingComplete) closed it programmatically.
            // Cancel/failure → the user closed it via the Cancel button.
            return handle.ClosedProgrammatically;
        }

        /// <summary>
        /// Called on the converter's background thread when conversion finishes.
        /// On success, closes the dialog (which unblocks <see cref="ShowLoadingStatus"/>).
        /// On failure, switches the dialog into its error state and blocks this
        /// background thread until the user dismisses it.
        /// </summary>
        /// <param name="success">True if the PDF was read successfully.</param>
        /// <param name="errorMessage">The converter's error output (on failure).</param>
        public void LoadingComplete(bool success, string errorMessage)
        {
            INonModalDialog<PdfConversionInProgressDialogViewModel>? handle;

            lock (lockObj) {
                complete = true;
                this.success = success;
                this.errorMessage = errorMessage;
                handle = dialog;
            }

            // Dialog not shown yet (or already closed) — ShowLoadingStatus will
            // observe the completion state we just set above.
            if (handle == null || handle.ClosedTask.IsCompleted)
                return;

            IEventDispatcherService dispatcher =
                Services.ServiceProvider.GetRequiredService<IEventDispatcherService>();

            if (success) {
                // Close on the UI thread; this unblocks ShowLoadingStatus.
                dispatcher.PostMessage(() => handle.Close());
            }
            else {
                // Reveal the failure message on the UI thread, then wait here
                // (on the background thread) for the user to hit Cancel.
                dispatcher.PostMessage(() => handle.ViewModel.ShowErrorMessage(errorMessage));
                while (!handle.ClosedTask.IsCompleted)
                    Thread.Sleep(PollIntervalMs);
            }
        }
    }
}
