using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

namespace PurplePen
{
    // Dialog used to get the settings for printing description. The dialog is used to fill out a DescriptionPrintSettings
    // class which contains the settings.
    partial class PrintPunches: BaseDialog
    {
        CorePunchPrintSettings settings = new CorePunchPrintSettings();
        PageSettings printerPageSettings = new PageSettings();
        internal Controller controller;
        private readonly bool isPdfCreation = false;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CorePunchPrintSettings PrintSettings
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
        public PageSettings PrinterPageSettings {
            get {
                UpdateSettings();
                return printerPageSettings;
            }
            set {
                printerPageSettings = value;
                UpdateDialog();
            }
        }

        // CONSIDER: shouldn't take an eventDB. Should instead take a pair of CourseViewData/name or some such.
        public PrintPunches(EventDB eventDB, bool isPdfCreation)
        {
            this.isPdfCreation = isPdfCreation;
            InitializeComponent();
            courseSelector.EventDB = eventDB;

            if (isPdfCreation) {
                printerLabel.Visible = printerName.Visible = printerChange.Visible = false;
                printButton.Text = MiscText.CreatePdf;
                this.Text = MiscText.CreatePdf;
                this.HelpTopic = "FileCreatePdfPunchCards.htm";

                foreach (Control c in outputPanel.Controls) {
                    outputPanel.SetRow(c, outputPanel.GetRow(c) - 1);
                }
            }
        }

        // Update the dialog with information from the settings.
        void UpdateDialog()
        {
            PageSettings pageSettings = printerPageSettings;
            PrinterSettings printerSettings = pageSettings.PrinterSettings;

            // Courses
            if (settings.CourseIds != null)
                courseSelector.SelectedCourses = settings.CourseIds;
            if (settings.AllCourses)
                courseSelector.AllCoursesSelected = true;

            courseSelector.VariationChoicesPerCourse = settings.VariationChoicesPerCourse;

            // Output section.
            printerName.Text = printerSettings.PrinterName;
            if (printerSettings.IsValid) {
                paperSize.Text = WindowsUtil.GetPaperSizeText(pageSettings.PaperSize);
                orientation.Text = (pageSettings.Landscape) ? MiscText.Landscape : MiscText.Portrait;
                margins.Text = WindowsUtil.GetMarginsText(pageSettings.Margins);
            }
            else {
                paperSize.Text = orientation.Text = margins.Text = "";
            }

            descriptionsUpDown.Value = settings.Count;

            // Appearance section
            boxSizeUpDown.Value = (decimal) settings.BoxSize;
        }

        // Update the settings with information from the dialog.
        void UpdateSettings()
        {
            // Courses.
            settings.CourseIds = courseSelector.SelectedCourses;
            settings.AllCourses = courseSelector.AllCoursesSelected;
            settings.VariationChoicesPerCourse = courseSelector.VariationChoicesPerCourse;

            // Copies section.
            settings.Count = (int) descriptionsUpDown.Value;

            // Appearance section
            settings.BoxSize = (float) boxSizeUpDown.Value;
        }

        private void printerChange_Click(object sender, EventArgs e)
        {
            controller.HandleExceptions(
                delegate {
                    UpdateSettings();
                    printDialog.PrinterSettings = printerPageSettings.PrinterSettings;
                    printDialog.PrinterSettings.DefaultPageSettings.Landscape = printerPageSettings.Landscape;
                    printDialog.PrinterSettings.DefaultPageSettings.Margins = printerPageSettings.Margins;
                    printDialog.PrinterSettings.DefaultPageSettings.PaperSize = printerPageSettings.PaperSize;
                    printDialog.PrinterSettings.DefaultPageSettings.PaperSource = printerPageSettings.PaperSource;

                    DialogResult result = printDialog.ShowDialog(this);

                    if (result == DialogResult.OK) {
                        printerPageSettings.Margins = printDialog.PrinterSettings.DefaultPageSettings.Margins;
                        printerPageSettings.PaperSize = printDialog.PrinterSettings.DefaultPageSettings.PaperSize;
                        printerPageSettings.PaperSource = printDialog.PrinterSettings.DefaultPageSettings.PaperSource;
                        printerPageSettings.PrinterSettings = printDialog.PrinterSettings;
                        printerPageSettings.PrinterSettings.Copies = 1; // ignore copies from the print settings dialog.
                        UpdateDialog();
                    }
                }
            );
        }

        private void marginChange_Click(object sender, EventArgs e)
        {
            controller.HandleExceptions(
                delegate {
                    UpdateSettings();
                    Margins originalMargins = printerPageSettings.Margins;
                    if (Util.IsCurrentCultureMetric())     // work around bug
                        printerPageSettings.Margins = PrinterUnitConvert.Convert(printerPageSettings.Margins, PrinterUnit.Display, PrinterUnit.TenthsOfAMillimeter);

                    pageSetupDialog.PageSettings = printerPageSettings;
                    pageSetupDialog.PrinterSettings = printerPageSettings.PrinterSettings;

                    DialogResult result = pageSetupDialog.ShowDialog(this);
                    if (result == DialogResult.OK) {
                        printerPageSettings = pageSetupDialog.PageSettings;
                        UpdateDialog();
                    }
                    else {
                        printerPageSettings.Margins = originalMargins;
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

        private void printButton_Click(object sender, EventArgs e)
        {
            if (SomeCoursesSelected())
                DialogResult = DialogResult.OK;
        }

        private void previewButton_Click(object sender, EventArgs e)
        {
            if (SomeCoursesSelected())
                controller.PrintPunches(WindowsUtil.GetWinFormsPrintTarget(PrinterPageSettings, this.Owner, true),
                                        PrintSettings,
                                        WindowsUtil.PrintingPaperSizeWithMarginsFromPageSettings(PrinterPageSettings));
        }

        private void punchCardLayoutButton_Click(object sender, EventArgs e)
        {
            PunchcardLayoutDialog dialog = new PunchcardLayoutDialog();
            PunchcardFormat format = controller.GetPunchcardFormat();

            dialog.PunchcardFormat = format;
            DialogResult result = dialog.ShowDialog();

            if (result == DialogResult.OK && !format.Equals(dialog.PunchcardFormat))
                controller.SetPunchcardFormat(dialog.PunchcardFormat);

            dialog.Dispose();
        }

        // Show an error message.
        async void ErrorMessage(string message)
        {
            await ((MainFrame)Owner).ErrorMessage(message);
        }

    }
}