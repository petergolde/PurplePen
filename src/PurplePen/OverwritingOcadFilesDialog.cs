using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class OverwritingOcadFilesDialog: Form
    {
        public OverwritingOcadFilesDialog()
        {
            InitializeComponent();
        }

        public List<string> Filenames
        {
            set
            {
                foreach (string s in value)
                    listBoxFiles.Items.Add(s);
            }
        }
    }
}
