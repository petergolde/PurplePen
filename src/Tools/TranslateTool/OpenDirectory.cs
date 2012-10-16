using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.IO;

namespace TranslateTool
{
    public partial class OpenDirectory: Form
    {
        public OpenDirectory()
        {
            InitializeComponent();

            Uri uri = new Uri(typeof(OpenDirectory).Assembly.CodeBase);
            textBoxDirectory.Text = Path.GetDirectoryName(uri.LocalPath);
        }

        public string Directory
        {
            get
            {
                return textBoxDirectory.Text;
            }
        }

        public CultureInfo Culture
        {
            get
            {
                return (CultureInfo) comboBoxLanguage.SelectedItem;
            }
        }

        private void OpenDirectory_Load(object sender, EventArgs e)
        {
            foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
                if (culture.IsNeutralCulture && culture != CultureInfo.InvariantCulture)
                    comboBoxLanguage.Items.Add(culture);

            comboBoxLanguage.SelectedIndex = 0;
        }

        private void buttonSelectDirectory_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = textBoxDirectory.Text;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                textBoxDirectory.Text = folderBrowserDialog.SelectedPath;
        }
    }
}
