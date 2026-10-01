using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    // Dialog used to get the settings for printing description. The dialog is used to fill out a DescriptionPrintSettings
    // class which contains the settings.
    partial class PrintCourses: OkCancelDialog
    {
        CoursePrintSettings settings = new CoursePrintSettings();
        PageSettings pageSettings = new PageSettings();

        internal Controller controller;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CoursePrintSettings PrintSettings
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

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PageSettings PageSettings {
            get {
                UpdateSettings();
                return pageSettings;
            }
            set {
                pageSettings = value;
                UpdateDialog();
            }
        }

        public PrintCourses(EventDB eventDB, bool enableMultipart)
        {
            InitializeComponent();
            courseSelector.EventDB = eventDB;

            checkBoxMergeParts.Visible = enableMultipart;
        }

#if XPS_PRINTING
        public bool EnableRasterizeChoice {
            get { return checkBoxRasterPrinting.Enabled; }
            set { checkBoxRasterPrinting.Enabled = value;  }
        }
#endif // XPS_PRINTING

        // Update the dialog with information from the settings.
        void UpdateDialog()
        {
            PrinterSettings printerSettings = pageSettings.PrinterSettings;

            // Courses
            if (settings.CourseIds != null)
                courseSelector.SelectedCourses = settings.CourseIds;
            if (settings.AllCourses)
                courseSelector.AllCoursesSelected = true;
            courseSelector.VariationChoicesPerCourse = settings.VariationChoicesPerCourse;

            // Output section.
            printerName.Text = printerSettings.PrinterName;

            copiesUpDown.Value = settings.Count;
            checkBoxPausePrinting.Checked = settings.PauseAfterCourseOrPart;

            comboBoxMultiPage.SelectedIndex = settings.CropLargePrintArea ? 0 : 1;
            comboBoxColorModel.SelectedIndex = (int)settings.PrintingColorModel;
            checkBoxMergeParts.Checked = settings.PrintMapExchangesOnOneMap;
#if XPS_PRINTING
            checkBoxRasterPrinting.Checked = !settings.UseXpsPrinting;
#endif // XPS_PRINTING
        }

        // Update the settings with information from the dialog.
        void UpdateSettings()
        {
            // Courses.
            settings.CourseIds = courseSelector.SelectedCourses;
            settings.AllCourses = courseSelector.AllCoursesSelected;
            settings.VariationChoicesPerCourse = courseSelector.VariationChoicesPerCourse;

            // Copies section.
            settings.Count = (int) copiesUpDown.Value;
            settings.PauseAfterCourseOrPart = checkBoxPausePrinting.Checked;

            // Appearance 
            settings.CropLargePrintArea = (comboBoxMultiPage.SelectedIndex == 0);
#if XPS_PRINTING
            settings.UseXpsPrinting = ! checkBoxRasterPrinting.Checked;
#endif // XPS_PRINTING
            settings.PrintMapExchangesOnOneMap = checkBoxMergeParts.Checked;
            settings.PrintingColorModel = (ColorModel)comboBoxColorModel.SelectedIndex;
        }

        private void printerChange_Click(object sender, EventArgs e)
        {
            controller.HandleExceptions(
                delegate {
                    UpdateSettings();
                    printDialog.PrinterSettings = pageSettings.PrinterSettings;
                    printDialog.PrinterSettings.DefaultPageSettings.Landscape = pageSettings.Landscape;
                    printDialog.PrinterSettings.DefaultPageSettings.PaperSize = pageSettings.PaperSize;
                    printDialog.PrinterSettings.DefaultPageSettings.PaperSource = pageSettings.PaperSource;
                    DialogResult result = printDialog.ShowDialog(this);
                    if (result == DialogResult.OK) {
                        pageSettings.PaperSize = printDialog.PrinterSettings.DefaultPageSettings.PaperSize;
                        pageSettings.PaperSource = printDialog.PrinterSettings.DefaultPageSettings.PaperSource;
                        pageSettings.PrinterSettings = printDialog.PrinterSettings;
                        pageSettings.PrinterSettings.Copies = 1; // ignore copies from the print settings dialog.
                        UpdateDialog();
                    }
                }
            );
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

        private void previewButton_Click(object sender, EventArgs e)
        {
            if (SomeCoursesSelected()) {
                controller.PrintCourses(WindowsUtil.GetWinFormsPrintTarget(PageSettings, this.Owner, true),
                                        PrintSettings,
                                        WindowsUtil.PrintingPaperSizeWithMarginsFromPageSettings(PageSettings));

            }
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