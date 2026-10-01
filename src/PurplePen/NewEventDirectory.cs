using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace PurplePen
{
    public partial class NewEventDirectory: UserControl, NewEventWizard.IWizardPage
    {
        public NewEventDirectory()
        {
            InitializeComponent();
        }

        public bool CanProceed
        {
            get { return (!useOtherFolder.Checked || (directoryName.Text.Length > 0)); } 
        }

        public string Title
        {
            get { return labelTitle.Text; } 
        }

        private void useOtherFolder_CheckedChanged(object sender, EventArgs e)
        {
            chooseFolder.Enabled = useOtherFolder.Checked;
            directoryDisplay.Visible = (useOtherFolder.Checked && directoryName.Text.Length > 0);
        }

        private void chooseFolder_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK) {
                directoryName.Text = folderBrowserDialog.SelectedPath;
                directoryDisplay.Visible = true;
            }
        }

        // Get the directory of the event, based on the selected items.
        public string GetEventDirectory(string mapName)
        {
            if (useMapDirectory.Checked) {
                return Path.GetDirectoryName(mapName);
            }
            else {
                return directoryName.Text;
            }
        }

    }
}
