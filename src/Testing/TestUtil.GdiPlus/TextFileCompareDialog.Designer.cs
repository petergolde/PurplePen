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
            this.buttonFixBitness = new System.Windows.Forms.Button();
            this.buttonFixFramework = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonShowDiff
            // 
            this.buttonShowDiff.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonShowDiff.Location = new System.Drawing.Point(174, 325);
            this.buttonShowDiff.Margin = new System.Windows.Forms.Padding(60, 28, 60, 28);
            this.buttonShowDiff.Name = "buttonShowDiff";
            this.buttonShowDiff.Size = new System.Drawing.Size(141, 35);
            this.buttonShowDiff.TabIndex = 0;
            this.buttonShowDiff.Text = "Show Differences";
            this.buttonShowDiff.UseVisualStyleBackColor = true;
            this.buttonShowDiff.Click += new System.EventHandler(this.buttonShowDiff_Click);
            // 
            // buttonAcceptBaseline
            // 
            this.buttonAcceptBaseline.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonAcceptBaseline.Location = new System.Drawing.Point(18, 325);
            this.buttonAcceptBaseline.Margin = new System.Windows.Forms.Padding(60, 28, 60, 28);
            this.buttonAcceptBaseline.Name = "buttonAcceptBaseline";
            this.buttonAcceptBaseline.Size = new System.Drawing.Size(141, 35);
            this.buttonAcceptBaseline.TabIndex = 1;
            this.buttonAcceptBaseline.Text = "Accept As Baseline";
            this.buttonAcceptBaseline.UseVisualStyleBackColor = true;
            this.buttonAcceptBaseline.Click += new System.EventHandler(this.buttonAcceptBaseline_Click);
            // 
            // buttonFail
            // 
            this.buttonFail.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonFail.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonFail.Location = new System.Drawing.Point(329, 325);
            this.buttonFail.Margin = new System.Windows.Forms.Padding(60, 28, 60, 28);
            this.buttonFail.Name = "buttonFail";
            this.buttonFail.Size = new System.Drawing.Size(141, 35);
            this.buttonFail.TabIndex = 2;
            this.buttonFail.Text = "Fail";
            this.buttonFail.UseVisualStyleBackColor = true;
            // 
            // labelInformation
            // 
            this.labelInformation.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelInformation.Location = new System.Drawing.Point(12, 9);
            this.labelInformation.Margin = new System.Windows.Forms.Padding(60, 0, 60, 0);
            this.labelInformation.Name = "labelInformation";
            this.labelInformation.Size = new System.Drawing.Size(455, 299);
            this.labelInformation.TabIndex = 3;
            this.labelInformation.Text = "labelInformation";
            // 
            // buttonFixBitness
            // 
            this.buttonFixBitness.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonFixBitness.Location = new System.Drawing.Point(18, 361);
            this.buttonFixBitness.Margin = new System.Windows.Forms.Padding(60, 28, 60, 28);
            this.buttonFixBitness.Name = "buttonFixBitness";
            this.buttonFixBitness.Size = new System.Drawing.Size(226, 35);
            this.buttonFixBitness.TabIndex = 4;
            this.buttonFixBitness.Text = "Make Bitness Specific";
            this.buttonFixBitness.UseVisualStyleBackColor = true;
            // 
            // buttonFixFramework
            // 
            this.buttonFixFramework.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonFixFramework.Location = new System.Drawing.Point(260, 361);
            this.buttonFixFramework.Name = "buttonFixFramework";
            this.buttonFixFramework.Size = new System.Drawing.Size(210, 35);
            this.buttonFixFramework.TabIndex = 5;
            this.buttonFixFramework.Text = "Make Framework Specific";
            this.buttonFixFramework.UseVisualStyleBackColor = true;
            this.buttonFixFramework.Click += new System.EventHandler(this.buttonFixFramework_Click);
            // 
            // TextFileCompareDialog
            // 
            this.AcceptButton = this.buttonFail;
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.buttonFail;
            this.ClientSize = new System.Drawing.Size(494, 398);
            this.Controls.Add(this.buttonFixFramework);
            this.Controls.Add(this.buttonFixBitness);
            this.Controls.Add(this.labelInformation);
            this.Controls.Add(this.buttonFail);
            this.Controls.Add(this.buttonAcceptBaseline);
            this.Controls.Add(this.buttonShowDiff);
            this.Margin = new System.Windows.Forms.Padding(60, 28, 60, 28);
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
        private System.Windows.Forms.Button buttonFixBitness;
        private System.Windows.Forms.Button buttonFixFramework;
    }
}
