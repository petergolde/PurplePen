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
using System.Diagnostics;



namespace TestingUtils
{
    public partial class TextFileCompareDialog: Form
    {
        public string BaselineFilename;
        public string NewFilename;

        public TextFileCompareDialog()
        {
            InitializeComponent();
        }

        private void TextFileCompareDialog_Shown(object sender, EventArgs e)
        {
            if (File.Exists(BaselineFilename)) {
                labelInformation.Text = string.Format(
                    "File '{0}' does not compare with baseline '{1}'", NewFilename, BaselineFilename);
            }
            else {
                labelInformation.Text = string.Format(
                    "Baseline file '{0}' does not exist", BaselineFilename);
                buttonShowDiff.Text = "Show File";
            }
        }

        private void buttonShowDiff_Click(object sender, EventArgs e)
        {
            if (File.Exists(BaselineFilename)) {
                Process.Start("windiff.exe", string.Format("\"{0}\" \"{1}\"", BaselineFilename, NewFilename));
            }
            else {
                Process.Start("notepad.exe", string.Format("\"{0}\"", NewFilename));
            }
        }

        private void buttonAcceptBaseline_Click(object sender, EventArgs e)
        {
            File.Copy(NewFilename, BaselineFilename, true);
            DialogResult = DialogResult.OK;
        }
    }
}