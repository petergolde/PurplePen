using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class NewEventPaperSize : UserControl, NewEventWizard.IWizardPage
    {
        public NewEventPaperSize()
        {
            InitializeComponent();
        }

        public bool CanProceed
        {
            get
            {
                return true;
            }
        }

        public string Title
        {
            get
            {
                return labelTitle.Text;
            }
        }
    }
}
