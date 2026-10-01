using PurplePen.MapModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class AboutForm: BaseDialog
    {
        public AboutForm()
        {
            InitializeComponent();

            this.versionLabel.Text = string.Format(MiscText.VersionLabel, Util.PrettyVersionString(VersionNumber.Current));
            this.bitnessLabel.Text = Environment.Is64BitProcess ? "64-bit" : "32-bit";
#if MSSTORE
            this.bitnessLabel.Text += " (Windows Store)";
#else
            this.bitnessLabel.Text += " (Standalone Setup)";
#endif
        }

        private void licenseButton_Click(object sender, EventArgs e)
        {
            new LicenseForm().ShowDialog();
        }

        private void logoPanel_Paint(object sender, PaintEventArgs e)
        {
            LogoDrawing.DrawPurplePenLogo(new GDIPlus_GraphicsTarget(e.Graphics), logoPanel.ClientRectangle);
        }

        private void creditsButton_Click(object sender, EventArgs e)
        {
            WindowsUtil.ShowHelpTopic(this, "Credits.htm");
        }

        private void copyrightLabel_Click(object sender, EventArgs e)
        {

        }

        private void freeLabel_Click(object sender, EventArgs e)
        {

        }

        private void bitnessLabel_Click(object sender, EventArgs e)
        {

        }
    }
}