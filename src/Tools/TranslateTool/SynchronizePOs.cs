using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TranslateTool
{
    public partial class SynchronizePOs : Form
    {
        public SynchronizePOs() {
            InitializeComponent();
        }

        public string ResXDirectory {
            get {
                return textBoxResXDirectory.Text;
            }
        }

        public string PODirectory {
            get {
                return textBoxPODirectory.Text;
            }
        }

        private void buttonSelectResXDirectory_Click(object sender, EventArgs e) {
            folderBrowserDialog.SelectedPath = textBoxResXDirectory.Text;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                textBoxResXDirectory.Text = folderBrowserDialog.SelectedPath;
        }

        private void buttonSelectPODirectory_Click(object sender, EventArgs e) {
            folderBrowserDialog.SelectedPath = textBoxPODirectory.Text;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                textBoxPODirectory.Text = folderBrowserDialog.SelectedPath;
        }
    }
}
