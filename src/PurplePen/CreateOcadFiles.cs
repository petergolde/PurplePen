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
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    using PurplePen.MapModel;

    partial class CreateOcadFiles: BaseDialog
    {
        private OcadCreationSettings settings;

        // Strings to put into drop-down list.
        private string[] fileFormatStrings = {
            MiscText.OCAD + " 6",
            MiscText.OCAD + " 7",
            MiscText.OCAD + " 8",
            MiscText.OCAD + " 9",
            MiscText.OCAD + " 10",
            MiscText.OCAD + " 11",
            MiscText.OCAD + " 12",
            MiscText.OCAD + " 2018",
            MiscText.OpenOrienteeringMapper + " 0.7 (.omap)",
            MiscText.OpenOrienteeringMapper + " 0.7 (.xmap)",
            MiscText.OpenOrienteeringMapper + " 0.8 (.omap)",
            MiscText.OpenOrienteeringMapper + " 0.8 (.xmap)",
            MiscText.OpenOrienteeringMapper + " 0.9 (.omap)",
            MiscText.OpenOrienteeringMapper + " 0.9 (.xmap)",
        };

        // These must match strings above in order.
        private MapFileFormat[] fileFormatDescriptors = {
            new MapFileFormat(MapFileFormatKind.OCAD, 6),
            new MapFileFormat(MapFileFormatKind.OCAD, 7),
            new MapFileFormat(MapFileFormatKind.OCAD, 8),
            new MapFileFormat(MapFileFormatKind.OCAD, 9),
            new MapFileFormat(MapFileFormatKind.OCAD, 10),
            new MapFileFormat(MapFileFormatKind.OCAD, 11),
            new MapFileFormat(MapFileFormatKind.OCAD, 12),
            new MapFileFormat(MapFileFormatKind.OCAD, 2018),
            new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.OMap, 6),
            new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.XMap, 6),
            new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.OMap, 7),
            new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.XMap, 7),
            new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.OMap, 9),
            new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.XMap, 9),
        };

        // CONSIDER: shouldn't take an eventDB. Should instead take a pair of CourseViewData/name or some such.
        public CreateOcadFiles(EventDB eventDB, MapFileFormatKind restrictToFormat, string title)
        {
            InitializeComponent();

            this.Text = title;
            courseSelector.EventDB = eventDB;

            // Initialize the items list in the file format. Only put in items matching "restrictToFormat".
            fileFormatCombo.Items.Clear();

            for (int i = 0; i < fileFormatDescriptors.Length; ++i) {
                if (restrictToFormat == MapFileFormatKind.None || restrictToFormat == fileFormatDescriptors[i].kind) {
                    fileFormatCombo.Items.Add(fileFormatStrings[i]);
                }
            }
        }

        // Get the settings for creating OCAD files.
        public OcadCreationSettings OcadCreationSettings
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

            // Format.
            for (int i = 0; i < fileFormatDescriptors.Length; ++i) {
                if (settings.fileFormat.Equals(fileFormatDescriptors[i])) {
                    fileFormatCombo.SelectedItem = fileFormatStrings[i];
                }
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
            for (int i = 0; i < fileFormatDescriptors.Length; ++i) {
                if ((string)fileFormatCombo.SelectedItem == fileFormatStrings[i]) {
                    settings.fileFormat = fileFormatDescriptors[i];
                }
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