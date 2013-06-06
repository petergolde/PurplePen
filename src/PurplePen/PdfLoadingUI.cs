using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    class PdfLoadingUI: IPdfLoadingStatus
    {
        public bool DownloadAndInstall(string downloadFrom, string fileName)
        {
            DialogResult result = MessageBox.Show(null, MiscText.DownloadGhostscript, MiscText.AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (result != DialogResult.Yes)
                return false;

            return Updater.DownloadAndInstall(new Uri(downloadFrom), fileName, false);
        }

        public bool ShowLoadingStatus(string fileName)
        {
            throw new NotImplementedException();
        }

        public void LoadingComplete(bool success, string errorMessage)
        {
            throw new NotImplementedException();
        }
    }
}
