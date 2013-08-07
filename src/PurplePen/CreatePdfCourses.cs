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

        // Update the dialog with information from the settings.
        void UpdateDialog()
        {
            // CONSIDER: Currently we always select courses, because the courses in the print settings could be out of date.

            // Output section.
            paperSize.Text = Util.GetPaperSizeText(settings.PaperSize);
            marginsLabel.Text = Util.GetMarginsText(settings.Margins);

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

            // Appearance 
            settings.CropLargePrintArea = (comboBoxMultiPage.SelectedIndex == 0);
            settings.PrintMapExchangesOnOneMap = checkBoxMergeParts.Checked;
            settings.ColorModel = (ColorModel)(comboBoxColorModel.SelectedIndex + 1);

            // Which folder?
            settings.mapDirectory = mapDirectory.Checked;
            settings.fileDirectory = coursesDirectory.Checked;

            // Folder name
            settings.outputDirectory = otherDirectoryTextBox.Text;

            // Filename prefix
            settings.filePrefix = filenamePrefixTextBox.Text;
        }

        private void marginChange_Click(object sender, EventArgs e)
        {
            UpdateSettings();

            PrinterMargins printerMarginsDialog = new PrinterMargins();
            printerMarginsDialog.EnableOrientation = false;
            printerMarginsDialog.PaperSize = settings.PaperSize;
            printerMarginsDialog.Margins = settings.Margins;

            DialogResult result = printerMarginsDialog.ShowDialog(this);
            if (result == DialogResult.OK) {
                settings.PaperSize = printerMarginsDialog.PaperSize;
                settings.Margins = printerMarginsDialog.Margins;
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
                ((MainFrame) Owner).ErrorMessage(MiscText.NoCoursesSelected);
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
    }
}