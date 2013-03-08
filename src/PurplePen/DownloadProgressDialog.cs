using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class DownloadProgressDialog : PurplePen.OkCancelDialog
    {
        public DownloadProgressDialog()
        {
            InitializeComponent();
        }

        public void SetProgress(int percent)
        {
            progressBar.Value = percent;
        }
    }
}
