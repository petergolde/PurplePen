/* Copyright (c) 2006-2008, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

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
    partial class PrintDescriptions: Form
    {
        DescriptionPrintSettings settings;
        internal Controller controller;

        public DescriptionPrintSettings PrintSettings
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

        // CONSIDER: shouldn't take an eventDB. Should instead take a pair of CourseViewData/name or some such.
        public PrintDescriptions(EventDB eventDB)
        {
            InitializeComponent();
            courseSelector.EventDB = eventDB;
        }

        // Update the dialog with information from the settings.
        void UpdateDialog()
        {
            PageSettings pageSettings = settings.PageSettings;
            PrinterSettings printerSettings = pageSettings.PrinterSettings;

            // CONSIDER: Currently we always select courses, because the courses in the print settings could be out of date.

            // Output section.
            printerName.Text = printerSettings.PrinterName;
            if (printerSettings.IsValid) {
                paperSize.Text = Util.GetPaperSizeText(pageSettings.PaperSize);
                orientation.Text = (pageSettings.Landscape) ? MiscText.Landscape : MiscText.Portrait;
                margins.Text = Util.GetMarginsText(pageSettings.Margins);
            }
            else {
                paperSize.Text = orientation.Text = margins.Text = "";
            }

            // Copies section.
            if (settings.CountKind == PrintingCountKind.DescriptionCount) {
                copiesCombo.SelectedIndex = 2;
                descriptionsUpDown.Enabled = true;
                descriptionsLabel.Enabled = true;
                descriptionsUpDown.Value = settings.Count;
            }
            else {
                descriptionsUpDown.Enabled = false;
                descriptionsLabel.Enabled = false;
                if (settings.CountKind == PrintingCountKind.OneDescription)
                    copiesCombo.SelectedIndex = 0;
                else
                    copiesCombo.SelectedIndex = 1;
            }

            // Appearance section
            boxSizeUpDown.Value = (decimal) settings.BoxSize;
            if (settings.UseCourseDefault) {
                descriptionKindCombo.SelectedIndex = 0;
            }
            else if (settings.DescKind == DescriptionKind.Symbols) {
                descriptionKindCombo.SelectedIndex = 1;
            }
            else if (settings.DescKind == DescriptionKind.Text) {
                descriptionKindCombo.SelectedIndex = 2;
            }
            else if (settings.DescKind == DescriptionKind.SymbolsAndText) {
                descriptionKindCombo.SelectedIndex = 3;
            }
        }

        // Update the settings with information from the dialog.
        void UpdateSettings()
        {
            // Courses.
            settings.CourseIds = courseSelector.SelectedCourses;

            // Copies section.
            if (copiesCombo.SelectedIndex == 0) {
                settings.CountKind = PrintingCountKind.OneDescription;
            }
            else if (copiesCombo.SelectedIndex == 1) {
                settings.CountKind = PrintingCountKind.OnePage;
            }
            else if (copiesCombo.SelectedIndex == 2) {
                settings.CountKind = PrintingCountKind.DescriptionCount;
                settings.Count = (int) descriptionsUpDown.Value;
            }

            // Appearance section
            settings.BoxSize = (float) boxSizeUpDown.Value;
            switch (descriptionKindCombo.SelectedIndex) {
            case 0: settings.UseCourseDefault = true; break;
            case 1: settings.UseCourseDefault = false; settings.DescKind = DescriptionKind.Symbols; break;
            case 2: settings.UseCourseDefault = false; settings.DescKind = DescriptionKind.Text; break;
            case 3: settings.UseCourseDefault = false; settings.DescKind = DescriptionKind.SymbolsAndText; break;
            }
        }

        private void printerChange_Click(object sender, EventArgs e)
        {
            controller.HandleExceptions(
                delegate {
                    UpdateSettings();
                    printDialog.PrinterSettings = settings.PageSettings.PrinterSettings;
                    printDialog.PrinterSettings.DefaultPageSettings.Landscape = settings.PageSettings.Landscape;
                    printDialog.PrinterSettings.DefaultPageSettings.Margins = settings.PageSettings.Margins;
                    printDialog.PrinterSettings.DefaultPageSettings.PaperSize = settings.PageSettings.PaperSize;
                    printDialog.PrinterSettings.DefaultPageSettings.PaperSource = settings.PageSettings.PaperSource;
                    DialogResult result = printDialog.ShowDialog(this);
                    if (result == DialogResult.OK) {
                        settings.PageSettings.Margins = printDialog.PrinterSettings.DefaultPageSettings.Margins;
                        settings.PageSettings.PaperSize = printDialog.PrinterSettings.DefaultPageSettings.PaperSize;
                        settings.PageSettings.PaperSource = printDialog.PrinterSettings.DefaultPageSettings.PaperSource;
                        settings.PageSettings.PrinterSettings = printDialog.PrinterSettings;
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
                    pageSetupDialog.PageSettings = settings.PageSettings;
                    pageSetupDialog.PrinterSettings = settings.PageSettings.PrinterSettings;
                    DialogResult result = pageSetupDialog.ShowDialog(this);
                    if (result == DialogResult.OK) {
                        settings.PageSettings = pageSetupDialog.PageSettings;
                        UpdateDialog();
                    }
                }
            );
        }

        private void printButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void previewButton_Click(object sender, EventArgs e)
        {
            controller.PrintDescriptions(PrintSettings, true);
        }

        private void copiesCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool enableCopyCount = (copiesCombo.SelectedIndex == 2);

            descriptionsUpDown.Enabled = enableCopyCount;
            descriptionsLabel.Enabled = enableCopyCount;
        }

        private void descriptionKindCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void PrintDescriptions_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            Util.ShowHelpTopic(this, "FilePrintDescriptions.htm");
            e.Cancel = true;
        }

    }
}