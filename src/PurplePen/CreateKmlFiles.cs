using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    using PurplePen.MapModel;

    partial class CreateKmlFiles: BaseDialog
    {
        private ExportKmlSettings settings;


        // CONSIDER: shouldn't take an eventDB. Should instead take a pair of CourseViewData/name or some such.
        public CreateKmlFiles(EventDB eventDB)
        {
            InitializeComponent();

            courseSelector.EventDB = eventDB;
        }

        // Get the settings for creating OCAD files.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ExportKmlSettings ExportKmlSettings
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
            // Courses
            if (settings.CourseIds != null)
                courseSelector.SelectedCourses = settings.CourseIds;
            if (settings.AllCourses)
                courseSelector.AllCoursesSelected = true;

            courseSelector.VariationChoicesPerCourse = settings.VariationChoicesPerCourse;

            // Folder name
            otherDirectoryTextBox.Text = settings.outputDirectory;

            // Filename prefix
            if (string.IsNullOrEmpty(settings.filePrefix))
                filenamePrefixTextBox.Text = "";
            else
                filenamePrefixTextBox.Text = settings.filePrefix;

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

            // Files.
            switch (settings.FileCreation) {
                case ExportKmlSettings.KmlFileCreation.SingleFile:
                    filesCombo.SelectedIndex = 0; break;
                case ExportKmlSettings.KmlFileCreation.FilePerCourse:
                    filesCombo.SelectedIndex = 1; break;
            }
        }

        // Update the settings with information from the dialog.
        void UpdateSettings()
        {
            // Courses.
            settings.CourseIds = courseSelector.SelectedCourses;
            settings.AllCourses = courseSelector.AllCoursesSelected;
            settings.VariationChoicesPerCourse = courseSelector.VariationChoicesPerCourse;

            // Which folder?
            settings.mapDirectory = mapDirectory.Checked;
            settings.fileDirectory = coursesDirectory.Checked;

            // Folder name
            settings.outputDirectory = otherDirectoryTextBox.Text;

            // Filename prefix
            settings.filePrefix = filenamePrefixTextBox.Text;

            // Format.
            if (filesCombo.SelectedIndex == 0) {
                settings.FileCreation = ExportKmlSettings.KmlFileCreation.SingleFile;
            }
            else if (filesCombo.SelectedIndex == 1) {
                settings.FileCreation = ExportKmlSettings.KmlFileCreation.FilePerCourse;
            }
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
    }
}