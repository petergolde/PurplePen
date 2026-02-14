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
    partial class MapTester
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
            if (disposing) {
                components?.Dispose();
                mapDisplay?.Dispose();
                mapDisplay = null;
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MapTester));
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.panel1 = new System.Windows.Forms.Panel();
            this.mapViewer = new PurplePen.MapView.MapViewer();
            this.horizScroll = new System.Windows.Forms.HScrollBar();
            this.vertScroll = new System.Windows.Forms.VScrollBar();
            this.locationLabel = new System.Windows.Forms.Label();
            this.showGrid = new System.Windows.Forms.CheckBox();
            this.zoomCombo = new System.Windows.Forms.ComboBox();
            this.zoomLabel = new System.Windows.Forms.Label();
            this.buttonScrollIntoView = new System.Windows.Forms.Button();
            this.buttonCreateTest = new System.Windows.Forms.Button();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.antialiasCheckBox = new System.Windows.Forms.CheckBox();
            this.intensityCombo = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.showBoundsCheckBox = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonBrowse
            // 
            this.buttonBrowse.Location = new System.Drawing.Point(13, 12);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(144, 38);
            this.buttonBrowse.TabIndex = 1;
            this.buttonBrowse.Text = "Browse for map file...";
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            // 
            // openFileDialog
            // 
            this.openFileDialog.DefaultExt = "ocd";
            this.openFileDialog.Filter = "OCAD Files|*.ocd|Image Files|*.jpeg;*.jpg;*.tiff;*.tif;*.png;*.bmp;*.gif";
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.mapViewer);
            this.panel1.Controls.Add(this.horizScroll);
            this.panel1.Controls.Add(this.vertScroll);
            this.panel1.Location = new System.Drawing.Point(0, 86);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(778, 517);
            this.panel1.TabIndex = 2;
            // 
            // mapViewer
            // 
            this.mapViewer.BackColor = System.Drawing.Color.White;
            this.mapViewer.CausesValidation = false;
            this.mapViewer.CenterPoint = ((System.Drawing.PointF) (resources.GetObject("mapViewer.CenterPoint")));
            this.mapViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapViewer.ForeColor = System.Drawing.Color.Black;
            this.mapViewer.Location = new System.Drawing.Point(0, 0);
            this.mapViewer.Name = "mapViewer";
            this.mapViewer.ShowGrid = false;
            this.mapViewer.ShowSymbolBounds = false;
            this.mapViewer.Size = new System.Drawing.Size(761, 500);
            this.mapViewer.TabIndex = 2;
            this.mapViewer.Text = "mapViewer1";
            this.mapViewer.Viewport = ((System.Drawing.RectangleF) (resources.GetObject("mapViewer.Viewport")));
            this.mapViewer.ZoomFactor = 1F;
            this.mapViewer.OnViewportChange += new System.EventHandler(this.mapViewer_OnViewportChange);
            this.mapViewer.OnPointerMove += new PurplePen.MapView.MapViewer.PointerEventHandler(this.mapViewer_OnPointerMove);
            this.mapViewer.OnMouseEvent += new PurplePen.MapView.MapViewer.MouseEventHandler(this.mapViewer_OnMouseEvent);
            // 
            // horizScroll
            // 
            this.horizScroll.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.horizScroll.Location = new System.Drawing.Point(0, 500);
            this.horizScroll.Name = "horizScroll";
            this.horizScroll.Size = new System.Drawing.Size(761, 17);
            this.horizScroll.TabIndex = 1;
            this.horizScroll.Scroll += new System.Windows.Forms.ScrollEventHandler(this.horizScroll_Scroll);
            // 
            // vertScroll
            // 
            this.vertScroll.Dock = System.Windows.Forms.DockStyle.Right;
            this.vertScroll.Location = new System.Drawing.Point(761, 0);
            this.vertScroll.Name = "vertScroll";
            this.vertScroll.Size = new System.Drawing.Size(17, 517);
            this.vertScroll.TabIndex = 0;
            this.vertScroll.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vertScroll_Scroll);
            // 
            // locationLabel
            // 
            this.locationLabel.AutoSize = true;
            this.locationLabel.Location = new System.Drawing.Point(13, 52);
            this.locationLabel.Name = "locationLabel";
            this.locationLabel.Size = new System.Drawing.Size(105, 13);
            this.locationLabel.TabIndex = 3;
            this.locationLabel.Text = "X=999.99  Y=999.99";
            // 
            // showGrid
            // 
            this.showGrid.AutoSize = true;
            this.showGrid.Location = new System.Drawing.Point(178, 17);
            this.showGrid.Name = "showGrid";
            this.showGrid.Size = new System.Drawing.Size(75, 17);
            this.showGrid.TabIndex = 4;
            this.showGrid.Text = "Show Grid";
            this.showGrid.CheckedChanged += new System.EventHandler(this.showGrid_CheckedChanged);
            // 
            // zoomCombo
            // 
            this.zoomCombo.FormattingEnabled = true;
            this.zoomCombo.Items.AddRange(new object[] {
            "25%",
            "50%",
            "100%",
            "200%",
            "300%",
            "500%",
            "1000%",
            "2000%",
            "5000%"});
            this.zoomCombo.Location = new System.Drawing.Point(333, 44);
            this.zoomCombo.Name = "zoomCombo";
            this.zoomCombo.Size = new System.Drawing.Size(93, 21);
            this.zoomCombo.TabIndex = 5;
            this.zoomCombo.TextChanged += new System.EventHandler(this.zoomCombo_TextChanged);
            // 
            // zoomLabel
            // 
            this.zoomLabel.AutoSize = true;
            this.zoomLabel.Location = new System.Drawing.Point(175, 52);
            this.zoomLabel.Name = "zoomLabel";
            this.zoomLabel.Size = new System.Drawing.Size(37, 13);
            this.zoomLabel.TabIndex = 6;
            this.zoomLabel.Text = "Zoom:";
            // 
            // buttonScrollIntoView
            // 
            this.buttonScrollIntoView.Location = new System.Drawing.Point(521, 9);
            this.buttonScrollIntoView.Name = "buttonScrollIntoView";
            this.buttonScrollIntoView.Size = new System.Drawing.Size(106, 30);
            this.buttonScrollIntoView.TabIndex = 7;
            this.buttonScrollIntoView.Text = "ScrollIntoView";
            this.buttonScrollIntoView.UseVisualStyleBackColor = true;
            this.buttonScrollIntoView.Click += new System.EventHandler(this.buttonScrollIntoView_Click);
            // 
            // buttonCreateTest
            // 
            this.buttonCreateTest.Location = new System.Drawing.Point(633, 9);
            this.buttonCreateTest.Name = "buttonCreateTest";
            this.buttonCreateTest.Size = new System.Drawing.Size(132, 30);
            this.buttonCreateTest.TabIndex = 8;
            this.buttonCreateTest.Text = "Create Test File";
            this.buttonCreateTest.UseVisualStyleBackColor = true;
            this.buttonCreateTest.Click += new System.EventHandler(this.buttonCreateTest_Click);
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.DefaultExt = "txt";
            // 
            // antialiasCheckBox
            // 
            this.antialiasCheckBox.AutoSize = true;
            this.antialiasCheckBox.Location = new System.Drawing.Point(270, 17);
            this.antialiasCheckBox.Name = "antialiasCheckBox";
            this.antialiasCheckBox.Size = new System.Drawing.Size(65, 17);
            this.antialiasCheckBox.TabIndex = 9;
            this.antialiasCheckBox.Text = "Antialias";
            this.antialiasCheckBox.UseVisualStyleBackColor = true;
            this.antialiasCheckBox.Click += new System.EventHandler(this.antialiasCheckBox_Click);
            // 
            // intensityCombo
            // 
            this.intensityCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.intensityCombo.FormattingEnabled = true;
            this.intensityCombo.Items.AddRange(new object[] {
            "0.1",
            "0.2",
            "0.3",
            "0.4",
            "0.5",
            "0.6",
            "0.7",
            "0.8",
            "0.9",
            "1.0"});
            this.intensityCombo.Location = new System.Drawing.Point(672, 49);
            this.intensityCombo.Name = "intensityCombo";
            this.intensityCombo.Size = new System.Drawing.Size(93, 21);
            this.intensityCombo.TabIndex = 10;
            this.intensityCombo.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(598, 52);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "Intensity:";
            // 
            // showBoundsCheckBox
            // 
            this.showBoundsCheckBox.AutoSize = true;
            this.showBoundsCheckBox.Location = new System.Drawing.Point(357, 17);
            this.showBoundsCheckBox.Name = "showBoundsCheckBox";
            this.showBoundsCheckBox.Size = new System.Drawing.Size(92, 17);
            this.showBoundsCheckBox.TabIndex = 12;
            this.showBoundsCheckBox.Text = "Show Bounds";
            this.showBoundsCheckBox.UseVisualStyleBackColor = true;
            this.showBoundsCheckBox.Click += new System.EventHandler(this.showBoundsCheckBox_Click);
            // 
            // MapTester
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(777, 602);
            this.Controls.Add(this.showBoundsCheckBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.intensityCombo);
            this.Controls.Add(this.antialiasCheckBox);
            this.Controls.Add(this.buttonCreateTest);
            this.Controls.Add(this.buttonScrollIntoView);
            this.Controls.Add(this.zoomLabel);
            this.Controls.Add(this.zoomCombo);
            this.Controls.Add(this.showGrid);
            this.Controls.Add(this.locationLabel);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.buttonBrowse);
            this.Name = "MapTester";
            this.Text = "MapTester";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.VScrollBar vertScroll;
        private System.Windows.Forms.HScrollBar horizScroll;
        private PurplePen.MapView.MapViewer mapViewer;
        private System.Windows.Forms.Label locationLabel;
        private System.Windows.Forms.CheckBox showGrid;
        private System.Windows.Forms.ComboBox zoomCombo;
        private System.Windows.Forms.Label zoomLabel;
        private System.Windows.Forms.Button buttonScrollIntoView;
        private System.Windows.Forms.Button buttonCreateTest;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.CheckBox antialiasCheckBox;
        private System.Windows.Forms.ComboBox intensityCombo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox showBoundsCheckBox;
    }
}
