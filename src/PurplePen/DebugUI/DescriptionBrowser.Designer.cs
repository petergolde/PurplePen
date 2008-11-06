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

namespace PurplePen.DebugUI
{
    partial class DescriptionBrowser
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.pictureBoxDescription = new System.Windows.Forms.Panel();
            this.buttonPrint = new System.Windows.Forms.Button();
            this.printDocument = new System.Drawing.Printing.PrintDocument();
            this.listBoxCourses = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxMm = new System.Windows.Forms.TextBox();
            this.comboBoxKind = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxPixels = new System.Windows.Forms.TextBox();
            this.labelPopupResults = new System.Windows.Forms.Label();
            this.buttonSaveBitmap = new System.Windows.Forms.Button();
            this.labelHitTestKind = new System.Windows.Forms.Label();
            this.labelHitTestLine = new System.Windows.Forms.Label();
            this.labelHitTestCol = new System.Windows.Forms.Label();
            this.labelHitTestRect = new System.Windows.Forms.Label();
            this.labelPoint = new System.Windows.Forms.Label();
            this.customKeyCheckBox = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.AutoScroll = true;
            this.panel1.Controls.Add(this.pictureBoxDescription);
            this.panel1.Location = new System.Drawing.Point(196, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(383, 570);
            this.panel1.TabIndex = 4;
            // 
            // pictureBoxDescription
            // 
            this.pictureBoxDescription.BackColor = System.Drawing.Color.White;
            this.pictureBoxDescription.Location = new System.Drawing.Point(0, 0);
            this.pictureBoxDescription.Name = "pictureBoxDescription";
            this.pictureBoxDescription.Size = new System.Drawing.Size(350, 545);
            this.pictureBoxDescription.TabIndex = 0;
            this.pictureBoxDescription.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pictureBoxDescription_MouseDown);
            this.pictureBoxDescription.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBoxDescription_Paint);
            // 
            // buttonPrint
            // 
            this.buttonPrint.Location = new System.Drawing.Point(17, 362);
            this.buttonPrint.Name = "buttonPrint";
            this.buttonPrint.Size = new System.Drawing.Size(166, 35);
            this.buttonPrint.TabIndex = 5;
            this.buttonPrint.Text = "Print";
            this.buttonPrint.Click += new System.EventHandler(this.buttonPrint_Click);
            // 
            // printDocument
            // 
            this.printDocument.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(this.printDocument_PrintPage);
            // 
            // listBoxCourses
            // 
            this.listBoxCourses.FormattingEnabled = true;
            this.listBoxCourses.Location = new System.Drawing.Point(17, 12);
            this.listBoxCourses.Name = "listBoxCourses";
            this.listBoxCourses.Size = new System.Drawing.Size(166, 199);
            this.listBoxCourses.TabIndex = 6;
            this.listBoxCourses.SelectedIndexChanged += new System.EventHandler(this.listBoxCourses_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 413);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(103, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Print Box Size (mm): ";
            // 
            // textBoxMm
            // 
            this.textBoxMm.Location = new System.Drawing.Point(129, 410);
            this.textBoxMm.Name = "textBoxMm";
            this.textBoxMm.Size = new System.Drawing.Size(54, 20);
            this.textBoxMm.TabIndex = 8;
            this.textBoxMm.Text = "6.0";
            // 
            // comboBoxKind
            // 
            this.comboBoxKind.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxKind.FormattingEnabled = true;
            this.comboBoxKind.Items.AddRange(new object[] {
            "Symbols",
            "Text",
            "SymbolsAndText"});
            this.comboBoxKind.Location = new System.Drawing.Point(17, 278);
            this.comboBoxKind.Name = "comboBoxKind";
            this.comboBoxKind.Size = new System.Drawing.Size(166, 21);
            this.comboBoxKind.TabIndex = 9;
            this.comboBoxKind.SelectedIndexChanged += new System.EventHandler(this.comboBoxKind_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 244);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(89, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "Box Size (pixels): ";
            // 
            // textBoxPixels
            // 
            this.textBoxPixels.Location = new System.Drawing.Point(129, 242);
            this.textBoxPixels.Name = "textBoxPixels";
            this.textBoxPixels.Size = new System.Drawing.Size(54, 20);
            this.textBoxPixels.TabIndex = 11;
            this.textBoxPixels.Text = "40";
            this.textBoxPixels.TextChanged += new System.EventHandler(this.textBoxPixels_TextChanged);
            // 
            // labelPopupResults
            // 
            this.labelPopupResults.AutoSize = true;
            this.labelPopupResults.Location = new System.Drawing.Point(14, 492);
            this.labelPopupResults.Name = "labelPopupResults";
            this.labelPopupResults.Size = new System.Drawing.Size(0, 13);
            this.labelPopupResults.TabIndex = 12;
            // 
            // buttonSaveBitmap
            // 
            this.buttonSaveBitmap.Location = new System.Drawing.Point(17, 551);
            this.buttonSaveBitmap.Name = "buttonSaveBitmap";
            this.buttonSaveBitmap.Size = new System.Drawing.Size(163, 30);
            this.buttonSaveBitmap.TabIndex = 14;
            this.buttonSaveBitmap.Text = "Save Bitmap";
            this.buttonSaveBitmap.Click += new System.EventHandler(this.buttonSaveBitmap_Click);
            // 
            // labelHitTestKind
            // 
            this.labelHitTestKind.AutoSize = true;
            this.labelHitTestKind.Location = new System.Drawing.Point(16, 454);
            this.labelHitTestKind.Name = "labelHitTestKind";
            this.labelHitTestKind.Size = new System.Drawing.Size(35, 13);
            this.labelHitTestKind.TabIndex = 15;
            this.labelHitTestKind.Text = "label3";
            // 
            // labelHitTestLine
            // 
            this.labelHitTestLine.AutoSize = true;
            this.labelHitTestLine.Location = new System.Drawing.Point(16, 466);
            this.labelHitTestLine.Name = "labelHitTestLine";
            this.labelHitTestLine.Size = new System.Drawing.Size(35, 13);
            this.labelHitTestLine.TabIndex = 16;
            this.labelHitTestLine.Text = "label3";
            // 
            // labelHitTestCol
            // 
            this.labelHitTestCol.AutoSize = true;
            this.labelHitTestCol.Location = new System.Drawing.Point(16, 479);
            this.labelHitTestCol.Name = "labelHitTestCol";
            this.labelHitTestCol.Size = new System.Drawing.Size(35, 13);
            this.labelHitTestCol.TabIndex = 17;
            this.labelHitTestCol.Text = "label3";
            // 
            // labelHitTestRect
            // 
            this.labelHitTestRect.AutoSize = true;
            this.labelHitTestRect.Location = new System.Drawing.Point(16, 492);
            this.labelHitTestRect.Name = "labelHitTestRect";
            this.labelHitTestRect.Size = new System.Drawing.Size(86, 13);
            this.labelHitTestRect.TabIndex = 18;
            this.labelHitTestRect.Text = "labelHitTestRect";
            // 
            // labelPoint
            // 
            this.labelPoint.AutoSize = true;
            this.labelPoint.Location = new System.Drawing.Point(16, 441);
            this.labelPoint.Name = "labelPoint";
            this.labelPoint.Size = new System.Drawing.Size(35, 13);
            this.labelPoint.TabIndex = 19;
            this.labelPoint.Text = "label3";
            // 
            // customKeyCheckBox
            // 
            this.customKeyCheckBox.AutoSize = true;
            this.customKeyCheckBox.Location = new System.Drawing.Point(16, 334);
            this.customKeyCheckBox.Name = "customKeyCheckBox";
            this.customKeyCheckBox.Size = new System.Drawing.Size(119, 17);
            this.customKeyCheckBox.TabIndex = 20;
            this.customKeyCheckBox.Text = "Custom Symbol Key";
            this.customKeyCheckBox.UseVisualStyleBackColor = true;
            this.customKeyCheckBox.CheckedChanged += new System.EventHandler(this.customKeyCheckBox_CheckedChanged);
            // 
            // DescriptionBrowser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(592, 594);
            this.Controls.Add(this.customKeyCheckBox);
            this.Controls.Add(this.labelPoint);
            this.Controls.Add(this.labelHitTestRect);
            this.Controls.Add(this.labelHitTestCol);
            this.Controls.Add(this.labelHitTestLine);
            this.Controls.Add(this.labelHitTestKind);
            this.Controls.Add(this.buttonSaveBitmap);
            this.Controls.Add(this.labelPopupResults);
            this.Controls.Add(this.textBoxPixels);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.comboBoxKind);
            this.Controls.Add(this.textBoxMm);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listBoxCourses);
            this.Controls.Add(this.buttonPrint);
            this.Controls.Add(this.panel1);
            this.Name = "DescriptionBrowser";
            this.Text = "DescriptionBrowser";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button buttonPrint;
        private System.Drawing.Printing.PrintDocument printDocument;
        private System.Windows.Forms.ListBox listBoxCourses;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxMm;
        private System.Windows.Forms.ComboBox comboBoxKind;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxPixels;
        private System.Windows.Forms.Label labelPopupResults;
        private System.Windows.Forms.Button buttonSaveBitmap;
        private System.Windows.Forms.Label labelHitTestKind;
        private System.Windows.Forms.Label labelHitTestLine;
        private System.Windows.Forms.Label labelHitTestCol;
        private System.Windows.Forms.Label labelHitTestRect;
        private System.Windows.Forms.Panel pictureBoxDescription;
        private System.Windows.Forms.Label labelPoint;
        private System.Windows.Forms.CheckBox customKeyCheckBox;


    }
}
