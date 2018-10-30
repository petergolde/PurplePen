using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class PdfConversionInProgress : PurplePen.OkCancelDialog
    {
        public PdfConversionInProgress()
        {
            InitializeComponent();
        }

        public void ShowErrorMessage(string message)
        {
            this.labelFailure.Visible = true;
            this.textBoxErrorMessage.Text = message;
            this.textBoxErrorMessage.Visible = true;
            this.progressBar.Value = this.progressBar.Maximum;
            this.progressBar.Style = ProgressBarStyle.Blocks;
        }
    }
}
