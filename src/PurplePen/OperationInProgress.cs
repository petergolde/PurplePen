using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class OperationInProgress : PurplePen.BaseDialog
    {
        bool cancelPressed = false;
        public EventHandler<EventArgs> OnCancelPressed;

        public OperationInProgress()
        {
            InitializeComponent();
        }

        public bool CancelPressed
        {
            get { return cancelPressed; }
        }

        public string StatusText
        {
            get
            {
                return informationLabel.Text;
            }
            set
            {
                informationLabel.Text = value;
            }
        }

        public bool IndefiniteDuration {
            get { 
                return progressBar.Style != ProgressBarStyle.Blocks; 
            }
            set { 
                progressBar.Style = value ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks; 
            }
        }

        public void SetProgress(double progressAmount)
        {
            if (progressAmount < 0)
                progressAmount = 0;
            if (progressAmount > 1)
                progressAmount = 1;

            progressBar.Value = (int)Math.Round(progressAmount * progressBar.Maximum);
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            cancelPressed = true;
            OnCancelPressed?.Invoke(this, new EventArgs());
        }
    }
}
