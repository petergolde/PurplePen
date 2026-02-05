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
            buttonShowDiff = new System.Windows.Forms.Button();
            buttonAcceptBaseline = new System.Windows.Forms.Button();
            buttonFail = new System.Windows.Forms.Button();
            labelInformation = new System.Windows.Forms.Label();
            buttonFixBitness = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // buttonShowDiff
            // 
            buttonShowDiff.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            buttonShowDiff.Location = new System.Drawing.Point(139, 260);
            buttonShowDiff.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            buttonShowDiff.Name = "buttonShowDiff";
            buttonShowDiff.Size = new System.Drawing.Size(113, 28);
            buttonShowDiff.TabIndex = 0;
            buttonShowDiff.Text = "Show Differences";
            buttonShowDiff.UseVisualStyleBackColor = true;
            buttonShowDiff.Click += buttonShowDiff_Click;
            // 
            // buttonAcceptBaseline
            // 
            buttonAcceptBaseline.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            buttonAcceptBaseline.Location = new System.Drawing.Point(14, 260);
            buttonAcceptBaseline.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            buttonAcceptBaseline.Name = "buttonAcceptBaseline";
            buttonAcceptBaseline.Size = new System.Drawing.Size(113, 28);
            buttonAcceptBaseline.TabIndex = 1;
            buttonAcceptBaseline.Text = "Accept As Baseline";
            buttonAcceptBaseline.UseVisualStyleBackColor = true;
            buttonAcceptBaseline.Click += buttonAcceptBaseline_Click;
            // 
            // buttonFail
            // 
            buttonFail.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            buttonFail.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            buttonFail.Location = new System.Drawing.Point(263, 260);
            buttonFail.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            buttonFail.Name = "buttonFail";
            buttonFail.Size = new System.Drawing.Size(113, 28);
            buttonFail.TabIndex = 2;
            buttonFail.Text = "Fail";
            buttonFail.UseVisualStyleBackColor = true;
            // 
            // labelInformation
            // 
            labelInformation.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            labelInformation.Location = new System.Drawing.Point(10, 7);
            labelInformation.Margin = new System.Windows.Forms.Padding(48, 0, 48, 0);
            labelInformation.Name = "labelInformation";
            labelInformation.Size = new System.Drawing.Size(364, 239);
            labelInformation.TabIndex = 3;
            labelInformation.Text = "labelInformation";
            // 
            // buttonFixBitness
            // 
            buttonFixBitness.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            buttonFixBitness.Location = new System.Drawing.Point(139, 289);
            buttonFixBitness.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            buttonFixBitness.Name = "buttonFixBitness";
            buttonFixBitness.Size = new System.Drawing.Size(113, 28);
            buttonFixBitness.TabIndex = 4;
            buttonFixBitness.Text = "Fix Bitness";
            buttonFixBitness.UseVisualStyleBackColor = true;
            buttonFixBitness.Click += buttonFixBitness_Click;
            // 
            // TextFileCompareDialog
            // 
            AcceptButton = buttonFail;
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            CancelButton = buttonFail;
            ClientSize = new System.Drawing.Size(395, 318);
            Controls.Add(buttonFixBitness);
            Controls.Add(labelInformation);
            Controls.Add(buttonFail);
            Controls.Add(buttonAcceptBaseline);
            Controls.Add(buttonShowDiff);
            Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            Name = "TextFileCompareDialog";
            ShowIcon = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Test File Mismatch";
            Shown += TextFileCompareDialog_Shown;
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonShowDiff;
        private System.Windows.Forms.Button buttonAcceptBaseline;
        private System.Windows.Forms.Button buttonFail;
        private System.Windows.Forms.Label labelInformation;
        private System.Windows.Forms.Button buttonFixBitness;
    }
}
