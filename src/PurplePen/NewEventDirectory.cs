/* Copyright (c) 2006-2007, Peter Golde
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
            get { return "Event File Location"; } 
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
