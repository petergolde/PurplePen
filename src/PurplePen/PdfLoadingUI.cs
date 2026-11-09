using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PurplePen
{
    // Shows the "Reading PDF" dialog while a PDF map file is converted on a background thread.
    // Holds per-conversion state, so it must be registered transient (one instance per conversion).
    class PdfLoadingUI: IPdfLoadingStatus, IDisposable
    {
        // Guards the fields below, which are touched both by the UI thread (ShowLoadingStatus, Dialog_Load)
        // and by the converter's background thread (LoadingComplete).
        private readonly object lockObj = new object();

        private PdfConversionInProgress dialog;    // The dialog while it is being shown, else null.
        private bool complete;
        private bool success;
        private string errorMessage;

        // Show the dialog until the conversion finishes or the user cancels.
        // Returns true if the conversion succeeded, false if the user cancelled (including after a failure).
        // fileName: the PDF file being read (unused).
        public bool ShowLoadingStatus(string fileName)
        {
            PdfConversionInProgress newDialog;

            lock (lockObj) {
                // The conversion may have finished before we got here.
                if (complete && success)
                    return true;

                newDialog = new PdfConversionInProgress();
                if (complete)
                    newDialog.ShowErrorMessage(errorMessage);

                // The conversion can finish after the check above but before the dialog has a window handle,
                // when LoadingComplete can't reach it; the Load handler catches that case.
                newDialog.Load += Dialog_Load;
                dialog = newDialog;
            }

            DialogResult result;
            try {
                result = newDialog.ShowDialog();
            }
            finally {
                lock (lockObj) {
                    dialog = null;
                }
                newDialog.Dispose();
            }

            return (result != DialogResult.Cancel);
        }

        // Called on the UI thread when the dialog's window has been created. Handles a conversion that
        // completed before the window existed.
        private void Dialog_Load(object sender, EventArgs e)
        {
            PdfConversionInProgress loadedDialog = (PdfConversionInProgress) sender;

            lock (lockObj) {
                if (!complete)
                    return;     // LoadingComplete will reach the dialog through its window handle.

                if (success)
                    loadedDialog.BeginInvoke((Action) delegate { loadedDialog.DialogResult = DialogResult.OK; });
                else
                    loadedDialog.ShowErrorMessage(errorMessage);
            }
        }

        // Called when the conversion finishes.
        // NOTE: This is called on a different thread from the dialog!
        // success: true if the PDF was converted.
        // errorMessage: the converter's error output, on failure.
        public void LoadingComplete(bool success, string errorMessage)
        {
            lock (lockObj) {
                complete = true;
                this.success = success;
                this.errorMessage = errorMessage;

                // No dialog yet: ShowLoadingStatus sees "complete". No window yet: Dialog_Load handles it.
                if (dialog == null || !dialog.IsHandleCreated)
                    return;

                // BeginInvoke rather than Invoke: the UI thread may be waiting for this lock in Dialog_Load.
                // Holding the lock keeps ShowLoadingStatus from disposing the dialog under us.
                PdfConversionInProgress currentDialog = dialog;
                try {
                    if (success)
                        currentDialog.BeginInvoke((Action) delegate { currentDialog.DialogResult = DialogResult.OK; });
                    else
                        currentDialog.BeginInvoke((Action) delegate { currentDialog.ShowErrorMessage(errorMessage); });
                }
                catch (InvalidOperationException) {
                    // The user closed the dialog and its window was destroyed just now; nothing left to update.
                }
            }
        }

        // Dispose managed resources.
        public void Dispose()
        {
            lock (lockObj) {
                dialog?.Dispose();
                dialog = null;
            }
        }
    }
}
