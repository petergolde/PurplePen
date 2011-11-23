using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class BaseDialog: Form
    {
        public BaseDialog()
        {
            // Initialize the font by default.
            Font = SystemFonts.MessageBoxFont;
            InitializeComponent();
        }

        // The HelpTopic to display when the question mark icon in the title bar is clicked.
        [Localizable(false)]
        public string HelpTopic { get; set; }

        private void BaseDialog_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            if (! string.IsNullOrEmpty(HelpTopic))
                Util.ShowHelpTopic(this, HelpTopic);
            e.Cancel = true;
        }
    }
}
