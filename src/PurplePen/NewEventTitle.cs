using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace PurplePen
{
    public partial class NewEventTitle: UserControl, NewEventWizard.IWizardPage
    {
        public NewEventTitle()
        {
            InitializeComponent();
        }

        public bool CanProceed
        {
            get { return (titleText.Text.Length > 0); }
        }

        public string Title
        {
            get { return labelTitle.Text; }
        }

        // Given the name of the event, convert to a file name.
        internal string GetEventFileName()
        {
            return Util.FilterInvalidPathChars(titleText.Text) + ".ppen";
        }
    }
}
