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

namespace TestingUtils
{
    partial class TextFileCompareDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonShowDiff = new System.Windows.Forms.Button();
            this.buttonAcceptBaseline = new System.Windows.Forms.Button();
            this.buttonFail = new System.Windows.Forms.Button();
            this.labelInformation = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // buttonShowDiff
            // 
            this.buttonShowDiff.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonShowDiff.Location = new System.Drawing.Point(139, 234);
            this.buttonShowDiff.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            this.buttonShowDiff.Name = "buttonShowDiff";
            this.buttonShowDiff.Size = new System.Drawing.Size(113, 28);
            this.buttonShowDiff.TabIndex = 0;
            this.buttonShowDiff.Text = "Show Differences";
            this.buttonShowDiff.UseVisualStyleBackColor = true;
            this.buttonShowDiff.Click += new System.EventHandler(this.buttonShowDiff_Click);
            // 
            // buttonAcceptBaseline
            // 
            this.buttonAcceptBaseline.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonAcceptBaseline.Location = new System.Drawing.Point(14, 234);
            this.buttonAcceptBaseline.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            this.buttonAcceptBaseline.Name = "buttonAcceptBaseline";
            this.buttonAcceptBaseline.Size = new System.Drawing.Size(113, 28);
            this.buttonAcceptBaseline.TabIndex = 1;
            this.buttonAcceptBaseline.Text = "Accept As Baseline";
            this.buttonAcceptBaseline.UseVisualStyleBackColor = true;
            this.buttonAcceptBaseline.Click += new System.EventHandler(this.buttonAcceptBaseline_Click);
            // 
            // buttonFail
            // 
            this.buttonFail.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonFail.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonFail.Location = new System.Drawing.Point(263, 234);
            this.buttonFail.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            this.buttonFail.Name = "buttonFail";
            this.buttonFail.Size = new System.Drawing.Size(113, 28);
            this.buttonFail.TabIndex = 2;
            this.buttonFail.Text = "Fail";
            this.buttonFail.UseVisualStyleBackColor = true;
            // 
            // labelInformation
            // 
            this.labelInformation.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.labelInformation.Location = new System.Drawing.Point(10, 7);
            this.labelInformation.Margin = new System.Windows.Forms.Padding(48, 0, 48, 0);
            this.labelInformation.Name = "labelInformation";
            this.labelInformation.Size = new System.Drawing.Size(363, 212);
            this.labelInformation.TabIndex = 3;
            this.labelInformation.Text = "labelInformation";
            // 
            // TextFileCompareDialog
            // 
            this.AcceptButton = this.buttonFail;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.buttonFail;
            this.ClientSize = new System.Drawing.Size(394, 278);
            this.Controls.Add(this.labelInformation);
            this.Controls.Add(this.buttonFail);
            this.Controls.Add(this.buttonAcceptBaseline);
            this.Controls.Add(this.buttonShowDiff);
            this.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            this.Name = "TextFileCompareDialog";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Test File Mismatch";
            this.Shown += new System.EventHandler(this.TextFileCompareDialog_Shown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonShowDiff;
        private System.Windows.Forms.Button buttonAcceptBaseline;
        private System.Windows.Forms.Button buttonFail;
        private System.Windows.Forms.Label labelInformation;
    }
}
