using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class NewEventFinal: UserControl, NewEventWizard.IWizardPage
    {
        public NewEventFinal()
        {
            InitializeComponent();
        }

        public bool CanProceed
        {
            get { return ! errorDisplayPanel.Visible; }
        }

        public string Title
        {
            get { return labelTitle.Text; }
        }

    }
}
