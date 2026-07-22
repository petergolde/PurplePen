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
    partial class BitmapCompareDialog
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
            acceptBaselineButton = new System.Windows.Forms.Button();
            failButton = new System.Windows.Forms.Button();
            infoLabel = new System.Windows.Forms.Label();
            tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            label4 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            bitmapViewerNew = new BitmapViewer();
            bitmapViewerBaseline = new BitmapViewer();
            bitmapViewerDiff1 = new BitmapViewer();
            bitmapViewerDiff2 = new BitmapViewer();
            label1 = new System.Windows.Forms.Label();
            tableLayoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // acceptBaselineButton
            // 
            acceptBaselineButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            acceptBaselineButton.Location = new System.Drawing.Point(522, 39);
            acceptBaselineButton.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            acceptBaselineButton.Name = "acceptBaselineButton";
            acceptBaselineButton.Size = new System.Drawing.Size(119, 26);
            acceptBaselineButton.TabIndex = 1;
            acceptBaselineButton.Text = "Accept new baseline";
            acceptBaselineButton.UseVisualStyleBackColor = true;
            acceptBaselineButton.Click += acceptBaselineButton_Click;
            // 
            // failButton
            // 
            failButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            failButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            failButton.Location = new System.Drawing.Point(522, 7);
            failButton.Margin = new System.Windows.Forms.Padding(0);
            failButton.Name = "failButton";
            failButton.Size = new System.Drawing.Size(119, 26);
            failButton.TabIndex = 2;
            failButton.Text = "Fail";
            failButton.UseVisualStyleBackColor = true;
            failButton.Click += failButton_Click;
            // 
            // infoLabel
            // 
            infoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            infoLabel.Location = new System.Drawing.Point(10, 7);
            infoLabel.Margin = new System.Windows.Forms.Padding(48, 0, 48, 0);
            infoLabel.Name = "infoLabel";
            infoLabel.Size = new System.Drawing.Size(346, 80);
            infoLabel.TabIndex = 3;
            infoLabel.Text = "infoLabel";
            // 
            // tableLayoutPanel
            // 
            tableLayoutPanel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tableLayoutPanel.ColumnCount = 2;
            tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            tableLayoutPanel.Controls.Add(label4, 1, 0);
            tableLayoutPanel.Controls.Add(label2, 0, 2);
            tableLayoutPanel.Controls.Add(label3, 1, 2);
            tableLayoutPanel.Controls.Add(bitmapViewerNew, 0, 1);
            tableLayoutPanel.Controls.Add(bitmapViewerBaseline, 1, 1);
            tableLayoutPanel.Controls.Add(bitmapViewerDiff1, 0, 3);
            tableLayoutPanel.Controls.Add(bitmapViewerDiff2, 1, 3);
            tableLayoutPanel.Controls.Add(label1, 0, 0);
            tableLayoutPanel.Location = new System.Drawing.Point(5, 110);
            tableLayoutPanel.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            tableLayoutPanel.Name = "tableLayoutPanel";
            tableLayoutPanel.RowCount = 4;
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 16F));
            tableLayoutPanel.Size = new System.Drawing.Size(636, 404);
            tableLayoutPanel.TabIndex = 4;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            label4.Location = new System.Drawing.Point(318, 0);
            label4.Margin = new System.Windows.Forms.Padding(0);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(101, 13);
            label4.TabIndex = 7;
            label4.Text = "Baseline Bitmap:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            label2.Location = new System.Drawing.Point(0, 202);
            label2.Margin = new System.Windows.Forms.Padding(0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(138, 13);
            label2.TabIndex = 5;
            label2.Text = "Diff (pink background):";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            label3.Location = new System.Drawing.Point(318, 202);
            label3.Margin = new System.Windows.Forms.Padding(0);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(138, 13);
            label3.TabIndex = 6;
            label3.Text = "Diff (blue background):";
            // 
            // bitmapViewerNew
            // 
            bitmapViewerNew.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            bitmapViewerNew.Location = new System.Drawing.Point(0, 24);
            bitmapViewerNew.Margin = new System.Windows.Forms.Padding(0);
            bitmapViewerNew.Name = "bitmapViewerNew";
            bitmapViewerNew.Size = new System.Drawing.Size(318, 178);
            bitmapViewerNew.TabIndex = 8;
            bitmapViewerNew.OnViewportChange += bitmapViewer_OnViewportChange;
            // 
            // bitmapViewerBaseline
            // 
            bitmapViewerBaseline.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            bitmapViewerBaseline.Location = new System.Drawing.Point(318, 24);
            bitmapViewerBaseline.Margin = new System.Windows.Forms.Padding(0);
            bitmapViewerBaseline.Name = "bitmapViewerBaseline";
            bitmapViewerBaseline.Size = new System.Drawing.Size(318, 178);
            bitmapViewerBaseline.TabIndex = 9;
            bitmapViewerBaseline.OnViewportChange += bitmapViewer_OnViewportChange;
            // 
            // bitmapViewerDiff1
            // 
            bitmapViewerDiff1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            bitmapViewerDiff1.Location = new System.Drawing.Point(0, 226);
            bitmapViewerDiff1.Margin = new System.Windows.Forms.Padding(0);
            bitmapViewerDiff1.Name = "bitmapViewerDiff1";
            bitmapViewerDiff1.Size = new System.Drawing.Size(318, 178);
            bitmapViewerDiff1.TabIndex = 10;
            bitmapViewerDiff1.OnViewportChange += bitmapViewer_OnViewportChange;
            // 
            // bitmapViewerDiff2
            // 
            bitmapViewerDiff2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            bitmapViewerDiff2.Location = new System.Drawing.Point(318, 226);
            bitmapViewerDiff2.Margin = new System.Windows.Forms.Padding(0);
            bitmapViewerDiff2.Name = "bitmapViewerDiff2";
            bitmapViewerDiff2.Size = new System.Drawing.Size(318, 178);
            bitmapViewerDiff2.TabIndex = 11;
            bitmapViewerDiff2.OnViewportChange += bitmapViewer_OnViewportChange;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            label1.Location = new System.Drawing.Point(0, 0);
            label1.Margin = new System.Windows.Forms.Padding(0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(78, 13);
            label1.TabIndex = 12;
            label1.Text = "New Bitmap:";
            // 
            // BitmapCompareDialog
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(648, 520);
            Controls.Add(tableLayoutPanel);
            Controls.Add(infoLabel);
            Controls.Add(failButton);
            Controls.Add(acceptBaselineButton);
            Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            Name = "BitmapCompareDialog";
            ShowIcon = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Bitmaps do not match";
            Load += BitmapCompareDialog_Load;
            Shown += BitmapCompareDialog_Shown;
            tableLayoutPanel.ResumeLayout(false);
            tableLayoutPanel.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button acceptBaselineButton;
        private System.Windows.Forms.Button failButton;
        private System.Windows.Forms.Label infoLabel;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private BitmapViewer bitmapViewerNew;
        private BitmapViewer bitmapViewerBaseline;
        private BitmapViewer bitmapViewerDiff1;
        private BitmapViewer bitmapViewerDiff2;
        private System.Windows.Forms.Label label1;
    }
}
