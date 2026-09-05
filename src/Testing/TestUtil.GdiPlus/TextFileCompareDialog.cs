using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;


namespace TestingUtils
{
    public partial class TextFileCompareDialog: Form
    {
        public string BaselineFilename;
        public string NewFilename;

        public TextFileCompareDialog()
        {
            InitializeComponent();
        }

        private void TextFileCompareDialog_Shown(object sender, EventArgs e)
        {
            if (File.Exists(BaselineFilename)) {
                labelInformation.Text = string.Format(
                    "File '{0}' does not compare with baseline '{1}'", NewFilename, BaselineFilename);
            }
            else {
                labelInformation.Text = string.Format(
                    "Baseline file '{0}' does not exist", BaselineFilename);
                buttonShowDiff.Text = "Show File";
            }
        }

        private void buttonShowDiff_Click(object sender, EventArgs e)
        {
            if (File.Exists(BaselineFilename)) {
                Process.Start("kdiff3.exe", string.Format("\"{0}\" \"{1}\"", BaselineFilename, NewFilename));
            }
            else {
                Process.Start("notepad.exe", string.Format("\"{0}\"", NewFilename));
            }
        }

        private void buttonAcceptBaseline_Click(object sender, EventArgs e)
        {
            File.Copy(NewFilename, BaselineFilename, true);
            DialogResult = DialogResult.OK;
        }

        private void buttonFixBitness_Click(object sender, EventArgs e)
        {
            if (TestUtil.HasBitnessSuffix(BaselineFilename)) {
                MessageBox.Show("Already bitness specific.");
                return;
            }

            (string filenameNewSave, string filenameBaselineSave) = TestingUtils.TestUtil.AddBitnessSuffix(BaselineFilename);

            File.Move(BaselineFilename, filenameBaselineSave);
            File.Copy(NewFilename, filenameNewSave, true);

            DialogResult = DialogResult.OK;
        }

        private void buttonFixFramework_Click(object sender, EventArgs e)
        {
            if (TestUtil.HasFrameworkSuffix(BaselineFilename)) {
                MessageBox.Show("Already framework specific.");
                return;
            }

            (string filenameNewSave, string filenameBaselineSave) = TestingUtils.TestUtil.AddFrameworkSuffix(BaselineFilename);

            File.Move(BaselineFilename, filenameBaselineSave);
            File.Copy(NewFilename, filenameNewSave, true);

            DialogResult = DialogResult.OK;
        }
    }
}