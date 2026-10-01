using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    // Dialog used to get the settings for printing description. The dialog is used to fill out a DescriptionPrintSettings
    // class which contains the settings.
    partial class CreatePdfCourses: OkCancelDialog
    {
        CoursePdfSettings settings;
        internal Controller controller;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CoursePdfSettings PdfSettings
        {
            get {
                UpdateSettings();
                return settings; 
            }
            set
            {
                settings = value;
                UpdateDialog();
            }
        }

        // CONSDER: shouldn't take an eventDB. Should instead take a pair of CourseViewData/name or some such.
        public CreatePdfCourses(EventDB eventDB, bool enableMultipart)
        {
            InitializeComponent();
            courseSelector.EventDB = eventDB;

            checkBoxMergeParts.Visible = enableMultipart;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool EnableChangeCropping
        {
            get { return comboBoxMultiPage.Enabled; }
            set { comboBoxMultiPage.Enabled = value; }
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

            comboBoxPrintBaseMap.SelectedIndex = settings.DontPrintBaseMap ? 1 : 0;
            comboBoxMultiPage.SelectedIndex = settings.CropLargePrintArea ? 0 : 1;
            comboBoxColorModel.SelectedIndex = (int)settings.ColorModel - 1;
            checkBoxMergeParts.Checked = settings.PrintMapExchangesOnOneMap;

            int fileFormatIndex = (int)settings.FileCreation;
            comboBoxFileFormat.SelectedIndex = fileFormatIndex;

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

            // Folder name
            otherDirectoryTextBox.Text = settings.outputDirectory;

            // Filename prefix
            if (string.IsNullOrEmpty(settings.filePrefix))
                filenamePrefixTextBox.Text = "";
            else
                filenamePrefixTextBox.Text = settings.filePrefix;
        }

        // Update the settings with information from the dialog.
        void UpdateSettings()
        {
            // Courses.
            settings.CourseIds = courseSelector.SelectedCourses;
            settings.AllCourses = courseSelector.AllCoursesSelected;
            settings.VariationChoicesPerCourse = courseSelector.VariationChoicesPerCourse;

            // Appearance 
            settings.DontPrintBaseMap = (comboBoxPrintBaseMap.SelectedIndex == 1);
            settings.CropLargePrintArea = (comboBoxMultiPage.SelectedIndex == 0);
            settings.PrintMapExchangesOnOneMap = checkBoxMergeParts.Checked;
            settings.ColorModel = (ColorModel)(comboBoxColorModel.SelectedIndex + 1);

            // Which folder?
            settings.mapDirectory = mapDirectory.Checked;
            settings.fileDirectory = coursesDirectory.Checked;

            // Folder name
            settings.outputDirectory = otherDirectoryTextBox.Text;

            // Filenames
            settings.filePrefix = filenamePrefixTextBox.Text;
            settings.FileCreation = (CoursePdfSettings.PdfFileCreation)comboBoxFileFormat.SelectedIndex;

            // Paper size and margins handled in marginChange_Click.
        }

        private void marginChange_Click(object sender, EventArgs e)
        {
            UpdateSettings();

            PrinterMargins printerMarginsDialog = new PrinterMargins();
            printerMarginsDialog.EnableOrientation = false;

            DialogResult result = printerMarginsDialog.ShowDialog(this);
            if (result == DialogResult.OK) {
                UpdateDialog();
            }
        }

        // If at least one course is selected, return true. Otherwise, show an error message an 
        // return false;
        private bool SomeCoursesSelected()
        {
            if (courseSelector.SelectedCourses.Length > 0)
                return true;
            else {
                ErrorMessage(MiscText.NoCoursesSelected);
                return false;
            }
        }

        private void otherDirectory_CheckedChanged(object sender, EventArgs e)
        {
            otherDirectoryTextBox.Visible = otherDirectory.Checked;
            selectOtherDirectoryButton.Visible = otherDirectory.Checked;
        }

        private void selectOtherDirectoryButton_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = otherDirectoryTextBox.Text;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                otherDirectoryTextBox.Text = folderBrowserDialog.SelectedPath;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (SomeCoursesSelected())
                DialogResult = DialogResult.OK;
            else
                DialogResult = DialogResult.None;
        }

        // Show an error message.
        async void ErrorMessage(string message)
        {
            await ((MainFrame)Owner).ErrorMessage(message);
        }

    }
}