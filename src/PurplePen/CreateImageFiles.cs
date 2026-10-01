using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    using PurplePen.MapModel;

    partial class CreateImageFiles: BaseDialog
    {
        private BitmapCreationSettings settings;


        // CONSIDER: shouldn't take an eventDB. Should instead take a pair of CourseViewData/name or some such.
        public CreateImageFiles(EventDB eventDB)
        {
            InitializeComponent();

            courseSelector.EventDB = eventDB;
        }

        // Get the settings for creating OCAD files.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public BitmapCreationSettings BitmapCreationSettings {
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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool WorldFileEnabled {
            get {
                return comboBoxWorldFile.Enabled;
            }
            set {
                comboBoxWorldFile.Enabled = value;
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

            // File format
            switch (settings.ExportedBitmapKind) {
                case BitmapCreationSettings.BitmapKind.Png:
                    fileFormatCombo.SelectedIndex = 0; break;
                case BitmapCreationSettings.BitmapKind.Jpeg:
                    fileFormatCombo.SelectedIndex = 1; break;
                case BitmapCreationSettings.BitmapKind.Gif:
                    fileFormatCombo.SelectedIndex = 2; break;
                default:
                    throw new ApplicationException("Unexpected bitmap kind.");
            }

            // Dpi
            comboBoxDpi.Text = settings.Dpi.ToString();

            // Color model.
            if (settings.ColorModel == ColorModel.CMYK)
                comboBoxColorModel.SelectedIndex = 1;
            else
                comboBoxColorModel.SelectedIndex = 0;

            // World file
            if (settings.WorldFile)
                comboBoxWorldFile.SelectedIndex = 1;
            else
                comboBoxWorldFile.SelectedIndex = 0;

            comboBoxPrintBaseMap.SelectedIndex = settings.DontPrintBaseMap ? 1 : 0;
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

            // File Format.
            switch (fileFormatCombo.SelectedIndex) {
                case 0:
                    settings.ExportedBitmapKind = BitmapCreationSettings.BitmapKind.Png; break;
                case 1:
                    settings.ExportedBitmapKind = BitmapCreationSettings.BitmapKind.Jpeg; break;
                case 2:
                    settings.ExportedBitmapKind = BitmapCreationSettings.BitmapKind.Gif; break;
                default:
                    throw new ApplicationException("Unexpected selected index");
            }

            // Dpi
            float dpi;
            if (float.TryParse(comboBoxDpi.Text, out dpi)) {
                settings.Dpi = dpi;
            }
            else {
                settings.Dpi = 200; // couldn't parse, just use default
            }

            // Color model.
            settings.ColorModel = (comboBoxColorModel.SelectedIndex == 1) ? ColorModel.CMYK : ColorModel.RGB;

            // World file
            settings.WorldFile = (comboBoxWorldFile.SelectedIndex == 1);

            // Print base map.
            settings.DontPrintBaseMap = (comboBoxPrintBaseMap.SelectedIndex == 1);
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

        private void outputGroupBox_Enter(object sender, EventArgs e)
        {

        }

        private void otherDirectoryTextBox_TextChanged(object sender, EventArgs e)
        {

        }
    }
}