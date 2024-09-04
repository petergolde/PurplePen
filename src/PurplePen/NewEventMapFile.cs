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
using System.IO;

using PurplePen.MapModel;

namespace PurplePen
{
    public partial class NewEventMapFile: UserControl, NewEventWizard.IWizardPage
    {
        NewEventWizard containingWizard;

        public NewEventMapFile()
        {
            InitializeComponent();
        }

        public bool CanProceed
        {
            get { return (mapFileNameTextBox.Text.Length > 0) && !errorDisplayPanel.Visible; }
        }

        public string Title
        {
            get { return labelTitle.Text; }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK) {
                containingWizard.MapFileName = mapFileNameTextBox.Text = openFileDialog.FileName;
                mapFileDisplay.Visible = true;

                string errorMessageText;
                float dpi;  // not used here.
                float mapScale;
                MapType mapType;
                int? lowerPurpleMapLayer;
                Size bitmapSize;
                RectangleF mapBounds;
                if (MapUtil.ValidateMapFile(containingWizard.MapFileName, out mapScale, out dpi, out bitmapSize, out mapBounds, out mapType, out lowerPurpleMapLayer, out errorMessageText)) 
                {
                    // map file is OK.
                    containingWizard.MapScale = mapScale;
                    containingWizard.MapType = mapType;
                    containingWizard.BitmapSize = bitmapSize;
                    containingWizard.mapBounds = mapBounds;
                    containingWizard.LowerPurpleMapLayer = lowerPurpleMapLayer;
                    errorDisplayPanel.Visible = false;
                    infoDisplayPanel.Visible = true;
                    ((Control)ParentForm.AcceptButton).Focus();
                }
                else {
                    // map file is not OK. Show message.
                    errorMessage.Text = errorMessageText;
                    infoDisplayPanel.Visible = false;
                    errorDisplayPanel.Visible = true;
                }
            }
        }


        private void NewEventMapFile_Load(object sender, EventArgs e)
        {
            containingWizard = (NewEventWizard) Parent;
        }
    }
}
