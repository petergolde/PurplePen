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

namespace PurplePen.DebugUI
{
    partial class SymbolBrowser
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
            this.pictureSymbol = new System.Windows.Forms.PictureBox();
            this.buttonCreateImage = new System.Windows.Forms.Button();
            this.buttonPrint = new System.Windows.Forms.Button();
            this.printDocument = new System.Drawing.Printing.PrintDocument();
            this.listBoxSymbols = new System.Windows.Forms.ListBox();
            this.labelName = new System.Windows.Forms.Label();
            this.labelText = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.labelType = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize) (this.pictureSymbol)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureSymbol
            // 
            this.pictureSymbol.BackColor = System.Drawing.Color.White;
            this.pictureSymbol.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureSymbol.Location = new System.Drawing.Point(13, 142);
            this.pictureSymbol.Name = "pictureSymbol";
            this.pictureSymbol.Size = new System.Drawing.Size(480, 443);
            this.pictureSymbol.TabIndex = 1;
            this.pictureSymbol.TabStop = false;
            this.pictureSymbol.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureSymbol_Paint);
            // 
            // buttonCreateImage
            // 
            this.buttonCreateImage.Location = new System.Drawing.Point(307, 6);
            this.buttonCreateImage.Name = "buttonCreateImage";
            this.buttonCreateImage.Size = new System.Drawing.Size(154, 42);
            this.buttonCreateImage.TabIndex = 2;
            this.buttonCreateImage.Text = "Create Image Bitmap";
            this.buttonCreateImage.Click += new System.EventHandler(this.buttonCreateImage_Click);
            // 
            // buttonPrint
            // 
            this.buttonPrint.Location = new System.Drawing.Point(468, 6);
            this.buttonPrint.Name = "buttonPrint";
            this.buttonPrint.Size = new System.Drawing.Size(154, 42);
            this.buttonPrint.TabIndex = 3;
            this.buttonPrint.Text = "Print All";
            this.buttonPrint.Click += new System.EventHandler(this.buttonPrint_Click);
            // 
            // printDocument
            // 
            this.printDocument.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(this.printDocument_PrintPage);
            // 
            // listBoxSymbols
            // 
            this.listBoxSymbols.FormattingEnabled = true;
            this.listBoxSymbols.Location = new System.Drawing.Point(13, 6);
            this.listBoxSymbols.Name = "listBoxSymbols";
            this.listBoxSymbols.Size = new System.Drawing.Size(274, 108);
            this.listBoxSymbols.TabIndex = 4;
            this.listBoxSymbols.SelectedIndexChanged += new System.EventHandler(this.listBoxSymbol_SelectedIndexChanged);
            // 
            // labelName
            // 
            this.labelName.AutoSize = true;
            this.labelName.Location = new System.Drawing.Point(468, 76);
            this.labelName.Name = "labelName";
            this.labelName.Size = new System.Drawing.Size(57, 13);
            this.labelName.TabIndex = 5;
            this.labelName.Text = "labelName";
            // 
            // labelText
            // 
            this.labelText.AutoSize = true;
            this.labelText.Location = new System.Drawing.Point(468, 93);
            this.labelText.Name = "labelText";
            this.labelText.Size = new System.Drawing.Size(50, 13);
            this.labelText.TabIndex = 6;
            this.labelText.Text = "labelText";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(310, 76);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "Name:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(310, 91);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(31, 13);
            this.label2.TabIndex = 9;
            this.label2.Text = "Text:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(310, 60);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(34, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "Type:";
            // 
            // labelType
            // 
            this.labelType.AutoSize = true;
            this.labelType.Location = new System.Drawing.Point(470, 60);
            this.labelType.Name = "labelType";
            this.labelType.Size = new System.Drawing.Size(53, 13);
            this.labelType.TabIndex = 12;
            this.labelType.Text = "labelType";
            // 
            // SymbolBrowser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(653, 597);
            this.Controls.Add(this.labelType);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.labelText);
            this.Controls.Add(this.labelName);
            this.Controls.Add(this.listBoxSymbols);
            this.Controls.Add(this.buttonPrint);
            this.Controls.Add(this.buttonCreateImage);
            this.Controls.Add(this.pictureSymbol);
            this.Name = "SymbolBrowser";
            this.Text = "Symbol Browser";
            ((System.ComponentModel.ISupportInitialize) (this.pictureSymbol)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureSymbol;
        private System.Windows.Forms.Button buttonCreateImage;
        private System.Windows.Forms.Button buttonPrint;
        private System.Drawing.Printing.PrintDocument printDocument;
        private System.Windows.Forms.ListBox listBoxSymbols;
        private System.Windows.Forms.Label labelName;
        private System.Windows.Forms.Label labelText;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelType;
    }
}
