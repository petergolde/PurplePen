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

namespace PurplePen
{
    partial class PunchcardLayoutDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PunchcardLayoutDialog));
            this.boxOrderGroupBox = new System.Windows.Forms.GroupBox();
            this.orderRLTB = new System.Windows.Forms.RadioButton();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.pictureBox4 = new System.Windows.Forms.PictureBox();
            this.orderRLBT = new System.Windows.Forms.RadioButton();
            this.orderLRTB = new System.Windows.Forms.RadioButton();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.orderLRBT = new System.Windows.Forms.RadioButton();
            this.sizeGroupBox = new System.Windows.Forms.GroupBox();
            this.colsUpDown = new System.Windows.Forms.NumericUpDown();
            this.rowsUpDown = new System.Windows.Forms.NumericUpDown();
            this.columnsLabel = new System.Windows.Forms.Label();
            this.rowsLabel = new System.Windows.Forms.Label();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.boxOrderGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.pictureBox3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.pictureBox4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.pictureBox1)).BeginInit();
            this.sizeGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.colsUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.rowsUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // boxOrderGroupBox
            // 
            this.boxOrderGroupBox.Controls.Add(this.orderRLTB);
            this.boxOrderGroupBox.Controls.Add(this.pictureBox3);
            this.boxOrderGroupBox.Controls.Add(this.pictureBox4);
            this.boxOrderGroupBox.Controls.Add(this.orderRLBT);
            this.boxOrderGroupBox.Controls.Add(this.orderLRTB);
            this.boxOrderGroupBox.Controls.Add(this.pictureBox2);
            this.boxOrderGroupBox.Controls.Add(this.pictureBox1);
            this.boxOrderGroupBox.Controls.Add(this.orderLRBT);
            resources.ApplyResources(this.boxOrderGroupBox, "boxOrderGroupBox");
            this.boxOrderGroupBox.Name = "boxOrderGroupBox";
            this.boxOrderGroupBox.TabStop = false;
            // 
            // orderRLTB
            // 
            resources.ApplyResources(this.orderRLTB, "orderRLTB");
            this.orderRLTB.Name = "orderRLTB";
            this.orderRLTB.TabStop = true;
            this.orderRLTB.UseVisualStyleBackColor = true;
            // 
            // pictureBox3
            // 
            resources.ApplyResources(this.pictureBox3, "pictureBox3");
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.TabStop = false;
            // 
            // pictureBox4
            // 
            resources.ApplyResources(this.pictureBox4, "pictureBox4");
            this.pictureBox4.Name = "pictureBox4";
            this.pictureBox4.TabStop = false;
            // 
            // orderRLBT
            // 
            resources.ApplyResources(this.orderRLBT, "orderRLBT");
            this.orderRLBT.Name = "orderRLBT";
            this.orderRLBT.TabStop = true;
            this.orderRLBT.UseVisualStyleBackColor = true;
            // 
            // orderLRTB
            // 
            resources.ApplyResources(this.orderLRTB, "orderLRTB");
            this.orderLRTB.Name = "orderLRTB";
            this.orderLRTB.TabStop = true;
            this.orderLRTB.UseVisualStyleBackColor = true;
            // 
            // pictureBox2
            // 
            resources.ApplyResources(this.pictureBox2, "pictureBox2");
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.TabStop = false;
            // 
            // pictureBox1
            // 
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // orderLRBT
            // 
            resources.ApplyResources(this.orderLRBT, "orderLRBT");
            this.orderLRBT.Name = "orderLRBT";
            this.orderLRBT.TabStop = true;
            this.orderLRBT.UseVisualStyleBackColor = true;
            // 
            // sizeGroupBox
            // 
            this.sizeGroupBox.Controls.Add(this.colsUpDown);
            this.sizeGroupBox.Controls.Add(this.rowsUpDown);
            this.sizeGroupBox.Controls.Add(this.columnsLabel);
            this.sizeGroupBox.Controls.Add(this.rowsLabel);
            resources.ApplyResources(this.sizeGroupBox, "sizeGroupBox");
            this.sizeGroupBox.Name = "sizeGroupBox";
            this.sizeGroupBox.TabStop = false;
            // 
            // colsUpDown
            // 
            resources.ApplyResources(this.colsUpDown, "colsUpDown");
            this.colsUpDown.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.colsUpDown.Minimum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.colsUpDown.Name = "colsUpDown";
            this.colsUpDown.Value = new decimal(new int[] {
            8,
            0,
            0,
            0});
            // 
            // rowsUpDown
            // 
            resources.ApplyResources(this.rowsUpDown, "rowsUpDown");
            this.rowsUpDown.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.rowsUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.rowsUpDown.Name = "rowsUpDown";
            this.rowsUpDown.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // columnsLabel
            // 
            resources.ApplyResources(this.columnsLabel, "columnsLabel");
            this.columnsLabel.Name = "columnsLabel";
            // 
            // rowsLabel
            // 
            resources.ApplyResources(this.rowsLabel, "rowsLabel");
            this.rowsLabel.Name = "rowsLabel";
            // 
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Name = "okButton";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // PunchcardLayoutDialog
            // 
            this.AcceptButton = this.okButton;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.cancelButton;
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.sizeGroupBox);
            this.Controls.Add(this.boxOrderGroupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.HelpButton = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PunchcardLayoutDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.PunchcardLayoutDialog_HelpButtonClicked);
            this.boxOrderGroupBox.ResumeLayout(false);
            this.boxOrderGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) (this.pictureBox3)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.pictureBox4)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.pictureBox1)).EndInit();
            this.sizeGroupBox.ResumeLayout(false);
            this.sizeGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) (this.colsUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.rowsUpDown)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox boxOrderGroupBox;
        private System.Windows.Forms.GroupBox sizeGroupBox;
        private System.Windows.Forms.NumericUpDown colsUpDown;
        private System.Windows.Forms.NumericUpDown rowsUpDown;
        private System.Windows.Forms.Label columnsLabel;
        private System.Windows.Forms.Label rowsLabel;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.RadioButton orderLRBT;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.RadioButton orderRLTB;
        private System.Windows.Forms.PictureBox pictureBox3;
        private System.Windows.Forms.PictureBox pictureBox4;
        private System.Windows.Forms.RadioButton orderRLBT;
        private System.Windows.Forms.RadioButton orderLRTB;
        private System.Windows.Forms.PictureBox pictureBox2;
    }
}
