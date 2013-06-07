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

namespace PurplePen
{
    partial class NewEventBitmapScale
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewEventBitmapScale));
            this.bitmapScaleLabel = new System.Windows.Forms.Label();
            this.dpiTextBox = new System.Windows.Forms.TextBox();
            this.resolutionLabel = new System.Windows.Forms.Label();
            this.dpiLabel = new System.Windows.Forms.Label();
            this.oneToPrefixLabel = new System.Windows.Forms.Label();
            this.mapScaleLabel = new System.Windows.Forms.Label();
            this.scaleTextBox = new System.Windows.Forms.TextBox();
            this.labelTitle = new System.Windows.Forms.Label();
            this.pdfScaleLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // bitmapScaleLabel
            // 
            resources.ApplyResources(this.bitmapScaleLabel, "bitmapScaleLabel");
            this.bitmapScaleLabel.Name = "bitmapScaleLabel";
            // 
            // dpiTextBox
            // 
            resources.ApplyResources(this.dpiTextBox, "dpiTextBox");
            this.dpiTextBox.Name = "dpiTextBox";
            // 
            // resolutionLabel
            // 
            resources.ApplyResources(this.resolutionLabel, "resolutionLabel");
            this.resolutionLabel.Name = "resolutionLabel";
            // 
            // dpiLabel
            // 
            resources.ApplyResources(this.dpiLabel, "dpiLabel");
            this.dpiLabel.Name = "dpiLabel";
            // 
            // oneToPrefixLabel
            // 
            resources.ApplyResources(this.oneToPrefixLabel, "oneToPrefixLabel");
            this.oneToPrefixLabel.Name = "oneToPrefixLabel";
            // 
            // mapScaleLabel
            // 
            resources.ApplyResources(this.mapScaleLabel, "mapScaleLabel");
            this.mapScaleLabel.Name = "mapScaleLabel";
            // 
            // scaleTextBox
            // 
            resources.ApplyResources(this.scaleTextBox, "scaleTextBox");
            this.scaleTextBox.Name = "scaleTextBox";
            // 
            // labelTitle
            // 
            resources.ApplyResources(this.labelTitle, "labelTitle");
            this.labelTitle.Name = "labelTitle";
            // 
            // pdfScaleLabel
            // 
            resources.ApplyResources(this.pdfScaleLabel, "pdfScaleLabel");
            this.pdfScaleLabel.Name = "pdfScaleLabel";
            // 
            // NewEventBitmapScale
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.pdfScaleLabel);
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.oneToPrefixLabel);
            this.Controls.Add(this.mapScaleLabel);
            this.Controls.Add(this.scaleTextBox);
            this.Controls.Add(this.dpiLabel);
            this.Controls.Add(this.resolutionLabel);
            this.Controls.Add(this.dpiTextBox);
            this.Controls.Add(this.bitmapScaleLabel);
            this.Name = "NewEventBitmapScale";
            this.Load += new System.EventHandler(this.NewEventBitmapScale_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label bitmapScaleLabel;
        private System.Windows.Forms.TextBox dpiTextBox;
        private System.Windows.Forms.Label resolutionLabel;
        private System.Windows.Forms.Label dpiLabel;
        private System.Windows.Forms.Label oneToPrefixLabel;
        private System.Windows.Forms.Label mapScaleLabel;
        private System.Windows.Forms.TextBox scaleTextBox;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.Label pdfScaleLabel;
    }
}
