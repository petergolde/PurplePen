using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;

using PurplePen.MapModel;
using PurplePen.Graphics2D;

namespace PurplePen.DebugUI
{
    public partial class DebugTextForm : DpiFixedForm
    {
        public DebugTextForm(string title, string text) {
            InitializeComponent();

            this.labelTitle.Text = title;
            this.textBoxMain.Text = text;
        }
    }
}
