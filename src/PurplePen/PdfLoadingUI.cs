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

        public bool DownloadAndInstall(string downloadFrom, string fileName)
        {
            DialogResult result = MessageBox.Show(null, MiscText.DownloadGhostscript, MiscText.AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (result != DialogResult.Yes)
                return false;

            return Updater.DownloadAndInstall(new Uri(downloadFrom), fileName, false);
        }

        public bool ShowLoadingStatus(string fileName)
        {
            dialog = new PdfConversionInProgress();
            DialogResult result = dialog.ShowDialog();
            return (dialog.DialogResult != DialogResult.Cancel);
        }

        // NOTE: This is called on a different thread from the dialog!
        public void LoadingComplete(bool success, string errorMessage)
        {
            if (success) {
                dialog.Invoke((Action)delegate {
                    dialog.Close();
                    dialog.Dispose();
                    dialog = null;
                });
            }
            else {
                if (dialog != null) {
                    dialog.Invoke((Action)delegate {
                        dialog.ShowErrorMessage(errorMessage);
                    });

                    // Wait for user to hit cancel button.
                    while (dialog != null && dialog.Visible) {
                        Thread.Sleep(50);
                    }
                }
            }
        }

        private void CloseDialog()
        {
            dialog.Close();
        }
    }
}
