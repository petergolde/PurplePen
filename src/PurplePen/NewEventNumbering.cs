using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class NewEventNumbering: UserControl, NewEventWizard.IWizardPage
    {
        public NewEventNumbering()
        {
            InitializeComponent();
        }

        public bool CanProceed
        {
            get { return true; } 
        }

        public string Title
        {
            get { return "Control Numbering"; }
        }

    }
}
