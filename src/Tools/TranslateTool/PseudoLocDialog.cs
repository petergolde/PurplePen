using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace TranslateTool
{
    public partial class PseudoLocDialog: Form
    {
        public PseudoLocDialog()
        {
            InitializeComponent();
        }

        public bool ExpandText
        {
            get
            {
                return expandTextCheckbox.Checked;
            }
        }
    }
}
