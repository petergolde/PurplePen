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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BitmapCompareDialog));
            this.acceptBaselineButton = new System.Windows.Forms.Button();
            this.failButton = new System.Windows.Forms.Button();
            this.infoLabel = new System.Windows.Forms.Label();
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.bitmapViewerNew = new TestingUtils.BitmapViewer();
            this.bitmapViewerBaseline = new TestingUtils.BitmapViewer();
            this.bitmapViewerDiff1 = new TestingUtils.BitmapViewer();
            this.bitmapViewerDiff2 = new TestingUtils.BitmapViewer();
            this.label1 = new System.Windows.Forms.Label();
            this.tableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // acceptBaselineButton
            // 
            this.acceptBaselineButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.acceptBaselineButton.Location = new System.Drawing.Point(522, 39);
            this.acceptBaselineButton.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            this.acceptBaselineButton.Name = "acceptBaselineButton";
            this.acceptBaselineButton.Size = new System.Drawing.Size(119, 26);
            this.acceptBaselineButton.TabIndex = 1;
            this.acceptBaselineButton.Text = "Accept new baseline";
            this.acceptBaselineButton.UseVisualStyleBackColor = true;
            this.acceptBaselineButton.Click += new System.EventHandler(this.acceptBaselineButton_Click);
            // 
            // failButton
            // 
            this.failButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.failButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.failButton.Location = new System.Drawing.Point(522, 7);
            this.failButton.Margin = new System.Windows.Forms.Padding(0);
            this.failButton.Name = "failButton";
            this.failButton.Size = new System.Drawing.Size(119, 26);
            this.failButton.TabIndex = 2;
            this.failButton.Text = "Fail";
            this.failButton.UseVisualStyleBackColor = true;
            this.failButton.Click += new System.EventHandler(this.failButton_Click);
            // 
            // infoLabel
            // 
            this.infoLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.infoLabel.Location = new System.Drawing.Point(10, 7);
            this.infoLabel.Margin = new System.Windows.Forms.Padding(48, 0, 48, 0);
            this.infoLabel.Name = "infoLabel";
            this.infoLabel.Size = new System.Drawing.Size(346, 80);
            this.infoLabel.TabIndex = 3;
            this.infoLabel.Text = "infoLabel";
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel.ColumnCount = 2;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel.Controls.Add(this.label4, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.label2, 0, 2);
            this.tableLayoutPanel.Controls.Add(this.label3, 1, 2);
            this.tableLayoutPanel.Controls.Add(this.bitmapViewerNew, 0, 1);
            this.tableLayoutPanel.Controls.Add(this.bitmapViewerBaseline, 1, 1);
            this.tableLayoutPanel.Controls.Add(this.bitmapViewerDiff1, 0, 3);
            this.tableLayoutPanel.Controls.Add(this.bitmapViewerDiff2, 1, 3);
            this.tableLayoutPanel.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel.Location = new System.Drawing.Point(5, 110);
            this.tableLayoutPanel.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 4;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 16F));
            this.tableLayoutPanel.Size = new System.Drawing.Size(636, 404);
            this.tableLayoutPanel.TabIndex = 4;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.label4.Location = new System.Drawing.Point(318, 0);
            this.label4.Margin = new System.Windows.Forms.Padding(0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(101, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Baseline Bitmap:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.label2.Location = new System.Drawing.Point(0, 202);
            this.label2.Margin = new System.Windows.Forms.Padding(0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(138, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Diff (pink background):";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.label3.Location = new System.Drawing.Point(318, 202);
            this.label3.Margin = new System.Windows.Forms.Padding(0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(138, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Diff (blue background):";
            // 
            // bitmapViewerNew
            // 
            this.bitmapViewerNew.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.bitmapViewerNew.Bitmap = null;
            this.bitmapViewerNew.CenterPoint = ((System.Drawing.PointF) (resources.GetObject("bitmapViewerNew.CenterPoint")));
            this.bitmapViewerNew.Location = new System.Drawing.Point(0, 24);
            this.bitmapViewerNew.Margin = new System.Windows.Forms.Padding(0);
            this.bitmapViewerNew.Name = "bitmapViewerNew";
            this.bitmapViewerNew.Size = new System.Drawing.Size(318, 178);
            this.bitmapViewerNew.TabIndex = 8;
            this.bitmapViewerNew.Viewport = ((System.Drawing.RectangleF) (resources.GetObject("bitmapViewerNew.Viewport")));
            this.bitmapViewerNew.ZoomFactor = 1F;
            this.bitmapViewerNew.OnViewportChange += new System.EventHandler(this.bitmapViewer_OnViewportChange);
            // 
            // bitmapViewerBaseline
            // 
            this.bitmapViewerBaseline.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.bitmapViewerBaseline.Bitmap = null;
            this.bitmapViewerBaseline.CenterPoint = ((System.Drawing.PointF) (resources.GetObject("bitmapViewerBaseline.CenterPoint")));
            this.bitmapViewerBaseline.Location = new System.Drawing.Point(318, 24);
            this.bitmapViewerBaseline.Margin = new System.Windows.Forms.Padding(0);
            this.bitmapViewerBaseline.Name = "bitmapViewerBaseline";
            this.bitmapViewerBaseline.Size = new System.Drawing.Size(318, 178);
            this.bitmapViewerBaseline.TabIndex = 9;
            this.bitmapViewerBaseline.Viewport = ((System.Drawing.RectangleF) (resources.GetObject("bitmapViewerBaseline.Viewport")));
            this.bitmapViewerBaseline.ZoomFactor = 1F;
            this.bitmapViewerBaseline.OnViewportChange += new System.EventHandler(this.bitmapViewer_OnViewportChange);
            // 
            // bitmapViewerDiff1
            // 
            this.bitmapViewerDiff1.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.bitmapViewerDiff1.Bitmap = null;
            this.bitmapViewerDiff1.CenterPoint = ((System.Drawing.PointF) (resources.GetObject("bitmapViewerDiff1.CenterPoint")));
            this.bitmapViewerDiff1.Location = new System.Drawing.Point(0, 226);
            this.bitmapViewerDiff1.Margin = new System.Windows.Forms.Padding(0);
            this.bitmapViewerDiff1.Name = "bitmapViewerDiff1";
            this.bitmapViewerDiff1.Size = new System.Drawing.Size(318, 178);
            this.bitmapViewerDiff1.TabIndex = 10;
            this.bitmapViewerDiff1.Viewport = ((System.Drawing.RectangleF) (resources.GetObject("bitmapViewerDiff1.Viewport")));
            this.bitmapViewerDiff1.ZoomFactor = 1F;
            this.bitmapViewerDiff1.OnViewportChange += new System.EventHandler(this.bitmapViewer_OnViewportChange);
            // 
            // bitmapViewerDiff2
            // 
            this.bitmapViewerDiff2.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.bitmapViewerDiff2.Bitmap = null;
            this.bitmapViewerDiff2.CenterPoint = ((System.Drawing.PointF) (resources.GetObject("bitmapViewerDiff2.CenterPoint")));
            this.bitmapViewerDiff2.Location = new System.Drawing.Point(318, 226);
            this.bitmapViewerDiff2.Margin = new System.Windows.Forms.Padding(0);
            this.bitmapViewerDiff2.Name = "bitmapViewerDiff2";
            this.bitmapViewerDiff2.Size = new System.Drawing.Size(318, 178);
            this.bitmapViewerDiff2.TabIndex = 11;
            this.bitmapViewerDiff2.Viewport = ((System.Drawing.RectangleF) (resources.GetObject("bitmapViewerDiff2.Viewport")));
            this.bitmapViewerDiff2.ZoomFactor = 1F;
            this.bitmapViewerDiff2.OnViewportChange += new System.EventHandler(this.bitmapViewer_OnViewportChange);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Margin = new System.Windows.Forms.Padding(0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(78, 13);
            this.label1.TabIndex = 12;
            this.label1.Text = "New Bitmap:";
            // 
            // BitmapCompareDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(648, 520);
            this.Controls.Add(this.tableLayoutPanel);
            this.Controls.Add(this.infoLabel);
            this.Controls.Add(this.failButton);
            this.Controls.Add(this.acceptBaselineButton);
            this.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            this.Name = "BitmapCompareDialog";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Bitmaps do not match";
            this.Shown += new System.EventHandler(this.BitmapCompareDialog_Shown);
            this.Load += new System.EventHandler(this.BitmapCompareDialog_Load);
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);

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
