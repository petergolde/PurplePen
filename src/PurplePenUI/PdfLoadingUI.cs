using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PurplePen
{
    class PdfLoadingUI: IPdfLoadingStatus
    {
        private PdfConversionInProgress dialog;
        private bool complete;
        private bool success;
        private string errorMessage;


        public bool ShowLoadingStatus(string fileName)
        {
            dialog = new PdfConversionInProgress();
            if (complete) {
                if (success)
                    return true;
                else
                    dialog.ShowErrorMessage(errorMessage);
            }

            DialogResult result = dialog.ShowDialog();
            return (dialog == null || dialog.DialogResult != DialogResult.Cancel);
        }

        // NOTE: This is called on a different thread from the dialog!
        public void LoadingComplete(bool success, string errorMessage)
        {
            complete = true;
            this.success = success;
            this.errorMessage = errorMessage;

            if (dialog == null || !dialog.IsHandleCreated)
                return;

            if (success) {
                dialog.Invoke((Action)delegate {
                    dialog.Close();
                    dialog.Dispose();
                    dialog = null;
                });
            }
            else {
                dialog.Invoke((Action)delegate {
                    dialog.ShowErrorMessage(errorMessage);
                });

                // Wait for user to hit cancel button.
                while (dialog != null && dialog.Visible) {
                    Thread.Sleep(50);
                }
            }
        }

        private void CloseDialog()
        {
            dialog.Close();
        }
    }
}
