using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    partial class CreateRouteGadgetFiles: BaseDialog
    {
        private RouteGadgetCreationSettings settings;

        // CONSIDER: shouldn't take an eventDB. Should instead take a pair of CourseViewData/name or some such.
        public CreateRouteGadgetFiles(EventDB eventDB)
        {
            InitializeComponent();
        }

        // Get the settings for creating OCAD files.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RouteGadgetCreationSettings RouteGadgetCreationSettings
        {
            get
            {
                UpdateSettings();
                return settings;
            }
            set
            {
                settings = value;
                UpdateDialog();
            }
        }

        // Update the dialog with information from the settings.
        void UpdateDialog()
        {
            // Folder name
            otherDirectoryTextBox.Text = settings.outputDirectory;

            // Filename prefix
            if (string.IsNullOrEmpty(settings.fileBaseName))
                fileNameTextBox.Text = "";
            else
                fileNameTextBox.Text = settings.fileBaseName;

            // Which folder.
            if (settings.mapDirectory) {
                mapDirectory.Checked = true; coursesDirectory.Checked = false; otherDirectory.Checked = false;
            }
            else if (settings.fileDirectory) {
                mapDirectory.Checked = false; coursesDirectory.Checked = true; otherDirectory.Checked = false;
            }
            else {
                mapDirectory.Checked = false; coursesDirectory.Checked = false; otherDirectory.Checked = true;
            }

            // IOF Version.
            if (settings.xmlVersion == 2)
                comboBoxIofXml.SelectedIndex = 0;
            else
                comboBoxIofXml.SelectedIndex = 1;
        }

        // Update the settings with information from the dialog.
        void UpdateSettings()
        {
            // Which folder?
            settings.mapDirectory = mapDirectory.Checked;
            settings.fileDirectory = coursesDirectory.Checked;

            // Folder name
            settings.outputDirectory = otherDirectoryTextBox.Text;

            // Filename prefix
            settings.fileBaseName = fileNameTextBox.Text;

            // IOF Version
            if (comboBoxIofXml.SelectedIndex == 0)
                settings.xmlVersion = 2;
            else
                settings.xmlVersion = 3;
        }

        private void selectOtherDirectoryButton_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = otherDirectoryTextBox.Text;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                otherDirectoryTextBox.Text = folderBrowserDialog.SelectedPath;
        }

        private void createButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void otherDirectory_CheckedChanged(object sender, EventArgs e)
        {
            otherDirectoryTextBox.Visible = otherDirectory.Checked;
            selectOtherDirectoryButton.Visible = otherDirectory.Checked;
        }

        private void learnMoreLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            WindowsUtil.ShowHelpTopic(this, HelpTopic);
        }
    }
}