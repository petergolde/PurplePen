using System.Windows.Forms;

namespace PurplePen.Livelox
{
    public partial class ConsentRedirectionDialog : OkCancelDialog
    {
        public ConsentRedirectionDialog()
        {
            InitializeComponent();
        }

        public bool RememberConsent => rememberConsentCheckBox.Checked;

        private void continueButton_Click(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void cancelButton_Click(object sender, System.EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
