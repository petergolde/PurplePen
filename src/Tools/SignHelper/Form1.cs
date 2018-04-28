using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SignHelper.Properties;

namespace SignHelper
{
    public partial class Form1 : Form
    {
        string fileToSign;

        public Form1(string fileToSign)
        {
            this.fileToSign = fileToSign;

            InitializeComponent();
            labelFileToSign.Text = fileToSign;
            LoadSettings();

        }

        private void buttonSignNow_Click(object sender, EventArgs e)
        {
            SaveSettings();
            int result = Signer.Sign(textBoxSignTool.Text, textBoxCertificate.Text, textBoxPassword.Text, fileToSign);
            Close();
            Environment.Exit(result);
        }


        private void SaveSettings()
        {
            Settings.Default.SignTool = textBoxSignTool.Text;
            Settings.Default.Certificate = textBoxCertificate.Text;
            Settings.Default.Save();
        }

        private void LoadSettings()
        {
            textBoxSignTool.Text = Settings.Default.SignTool;
            textBoxCertificate.Text = Settings.Default.Certificate;
        }

        private void browseSignTool_Click(object sender, EventArgs e)
        {
            openFileDialogSignTool.FileName = textBoxSignTool.Text;

            if (openFileDialogSignTool.ShowDialog() == DialogResult.OK) {
                textBoxSignTool.Text = openFileDialogSignTool.FileName;
            }
        }

        private void browseCertificate_Click(object sender, EventArgs e)
        {
            openFileDialogCertificate.FileName = textBoxCertificate.Text;

            if (openFileDialogCertificate.ShowDialog() == DialogResult.OK) {
                textBoxCertificate.Text = openFileDialogCertificate.FileName;
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            textBoxPassword.Focus();
        }
    }
}
