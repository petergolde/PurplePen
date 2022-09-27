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

namespace InteractiveTestApp
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MapTester));
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.panel1 = new System.Windows.Forms.Panel();
            this.mapViewer = new InteractiveTestApp.MapView.MapViewer();
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.dumpOCADFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.compareOcadFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showTemplatesCheckBox = new System.Windows.Forms.CheckBox();
            this.overprintCheckBox = new System.Windows.Forms.CheckBox();
            this.checkBoxInsertionPoint = new System.Windows.Forms.CheckBox();
            this.numericUpDownLine = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownCol = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownLine)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownCol)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonBrowse
            // 
            this.buttonBrowse.Location = new System.Drawing.Point(13, 52);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(144, 38);
            this.buttonBrowse.TabIndex = 1;
            this.buttonBrowse.Text = "Browse for map file...";
            this.buttonBrowse.Click += new System.EventHandler(this.buttonBrowse_Click);
            // 
            // openFileDialog
            // 
            this.openFileDialog.DefaultExt = "ocd";
            this.openFileDialog.Filter = "OCAD Files|*.ocd|Open Mapper Files|*.xmap;*.omap|Image Files|*.jpeg;*.jpg;*.tiff;" +
    "*.tif;*.png;*.bmp;*.gif";
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.mapViewer);
            this.panel1.Controls.Add(this.horizScroll);
            this.panel1.Controls.Add(this.vertScroll);
            this.panel1.Location = new System.Drawing.Point(0, 155);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(778, 448);
            this.panel1.TabIndex = 2;
            // 
            // mapViewer
            // 
            this.mapViewer.BackColor = System.Drawing.Color.White;
            this.mapViewer.CausesValidation = false;
            this.mapViewer.CenterPoint = ((System.Drawing.PointF)(resources.GetObject("mapViewer.CenterPoint")));
            this.mapViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapViewer.ForeColor = System.Drawing.Color.Black;
            this.mapViewer.Location = new System.Drawing.Point(0, 0);
            this.mapViewer.Name = "mapViewer";
            this.mapViewer.ShowGrid = false;
            this.mapViewer.ShowSymbolBounds = false;
            this.mapViewer.Size = new System.Drawing.Size(761, 431);
            this.mapViewer.TabIndex = 2;
            this.mapViewer.Text = "mapViewer1";
            this.mapViewer.Viewport = ((System.Drawing.RectangleF)(resources.GetObject("mapViewer.Viewport")));
            this.mapViewer.ZoomFactor = 1F;
            this.mapViewer.OnViewportChange += new System.EventHandler(this.mapViewer_OnViewportChange);
            this.mapViewer.OnPointerMove += new InteractiveTestApp.MapView.MapViewer.PointerEventHandler(this.mapViewer_OnPointerMove);
            this.mapViewer.OnMouseEvent += new InteractiveTestApp.MapView.MapViewer.MouseEventHandler(this.mapViewer_OnMouseEvent);
            // 
            // horizScroll
            // 
            this.horizScroll.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.horizScroll.Location = new System.Drawing.Point(0, 431);
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
            this.vertScroll.Size = new System.Drawing.Size(17, 448);
            this.vertScroll.TabIndex = 0;
            this.vertScroll.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vertScroll_Scroll);
            // 
            // locationLabel
            // 
            this.locationLabel.AutoSize = true;
            this.locationLabel.Location = new System.Drawing.Point(13, 92);
            this.locationLabel.Name = "locationLabel";
            this.locationLabel.Size = new System.Drawing.Size(105, 13);
            this.locationLabel.TabIndex = 3;
            this.locationLabel.Text = "X=999.99  Y=999.99";
            // 
            // showGrid
            // 
            this.showGrid.AutoSize = true;
            this.showGrid.Location = new System.Drawing.Point(178, 43);
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
            this.zoomCombo.Location = new System.Drawing.Point(333, 84);
            this.zoomCombo.Name = "zoomCombo";
            this.zoomCombo.Size = new System.Drawing.Size(93, 21);
            this.zoomCombo.TabIndex = 5;
            this.zoomCombo.TextChanged += new System.EventHandler(this.zoomCombo_TextChanged);
            // 
            // zoomLabel
            // 
            this.zoomLabel.AutoSize = true;
            this.zoomLabel.Location = new System.Drawing.Point(175, 92);
            this.zoomLabel.Name = "zoomLabel";
            this.zoomLabel.Size = new System.Drawing.Size(37, 13);
            this.zoomLabel.TabIndex = 6;
            this.zoomLabel.Text = "Zoom:";
            // 
            // buttonScrollIntoView
            // 
            this.buttonScrollIntoView.Location = new System.Drawing.Point(521, 49);
            this.buttonScrollIntoView.Name = "buttonScrollIntoView";
            this.buttonScrollIntoView.Size = new System.Drawing.Size(106, 30);
            this.buttonScrollIntoView.TabIndex = 7;
            this.buttonScrollIntoView.Text = "ScrollIntoView";
            this.buttonScrollIntoView.UseVisualStyleBackColor = true;
            this.buttonScrollIntoView.Click += new System.EventHandler(this.buttonScrollIntoView_Click);
            // 
            // buttonCreateTest
            // 
            this.buttonCreateTest.Location = new System.Drawing.Point(633, 49);
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
            this.antialiasCheckBox.Location = new System.Drawing.Point(257, 43);
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
            this.intensityCombo.Location = new System.Drawing.Point(672, 89);
            this.intensityCombo.Name = "intensityCombo";
            this.intensityCombo.Size = new System.Drawing.Size(93, 21);
            this.intensityCombo.TabIndex = 10;
            this.intensityCombo.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(598, 92);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 13);
            this.label1.TabIndex = 11;
            this.label1.Text = "Intensity:";
            // 
            // showBoundsCheckBox
            // 
            this.showBoundsCheckBox.AutoSize = true;
            this.showBoundsCheckBox.Location = new System.Drawing.Point(333, 66);
            this.showBoundsCheckBox.Name = "showBoundsCheckBox";
            this.showBoundsCheckBox.Size = new System.Drawing.Size(62, 17);
            this.showBoundsCheckBox.TabIndex = 12;
            this.showBoundsCheckBox.Text = "Bounds";
            this.showBoundsCheckBox.UseVisualStyleBackColor = true;
            this.showBoundsCheckBox.Click += new System.EventHandler(this.showBoundsCheckBox_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(777, 24);
            this.menuStrip1.TabIndex = 13;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dumpOCADFileToolStripMenuItem,
            this.compareOcadFilesToolStripMenuItem});
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(59, 20);
            this.toolStripMenuItem1.Text = "Actions";
            // 
            // dumpOCADFileToolStripMenuItem
            // 
            this.dumpOCADFileToolStripMenuItem.Name = "dumpOCADFileToolStripMenuItem";
            this.dumpOCADFileToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.dumpOCADFileToolStripMenuItem.Text = "Dump OCAD File...";
            this.dumpOCADFileToolStripMenuItem.Click += new System.EventHandler(this.dumpOCADFileToolStripMenuItem_Click);
            // 
            // compareOcadFilesToolStripMenuItem
            // 
            this.compareOcadFilesToolStripMenuItem.Name = "compareOcadFilesToolStripMenuItem";
            this.compareOcadFilesToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.compareOcadFilesToolStripMenuItem.Text = "Compare Ocad Files...";
            this.compareOcadFilesToolStripMenuItem.Click += new System.EventHandler(this.compareOcadFilesToolStripMenuItem_Click);
            // 
            // showTemplatesCheckBox
            // 
            this.showTemplatesCheckBox.AutoSize = true;
            this.showTemplatesCheckBox.Location = new System.Drawing.Point(332, 43);
            this.showTemplatesCheckBox.Name = "showTemplatesCheckBox";
            this.showTemplatesCheckBox.Size = new System.Drawing.Size(75, 17);
            this.showTemplatesCheckBox.TabIndex = 14;
            this.showTemplatesCheckBox.Text = "Templates";
            this.showTemplatesCheckBox.UseVisualStyleBackColor = true;
            this.showTemplatesCheckBox.CheckedChanged += new System.EventHandler(this.showTemplatesCheckBox_CheckedChanged);
            // 
            // overprintCheckBox
            // 
            this.overprintCheckBox.AutoSize = true;
            this.overprintCheckBox.Location = new System.Drawing.Point(178, 66);
            this.overprintCheckBox.Name = "overprintCheckBox";
            this.overprintCheckBox.Size = new System.Drawing.Size(83, 17);
            this.overprintCheckBox.TabIndex = 15;
            this.overprintCheckBox.Text = "Overprinting";
            this.overprintCheckBox.UseVisualStyleBackColor = true;
            this.overprintCheckBox.CheckedChanged += new System.EventHandler(this.overprintCheckBox_CheckedChanged);
            // 
            // checkBoxInsertionPoint
            // 
            this.checkBoxInsertionPoint.AutoSize = true;
            this.checkBoxInsertionPoint.Location = new System.Drawing.Point(178, 123);
            this.checkBoxInsertionPoint.Name = "checkBoxInsertionPoint";
            this.checkBoxInsertionPoint.Size = new System.Drawing.Size(93, 17);
            this.checkBoxInsertionPoint.TabIndex = 16;
            this.checkBoxInsertionPoint.Text = "Insertion Point";
            this.checkBoxInsertionPoint.UseVisualStyleBackColor = true;
            this.checkBoxInsertionPoint.CheckedChanged += new System.EventHandler(this.InsertionPointChanged);
            // 
            // numericUpDownLine
            // 
            this.numericUpDownLine.Location = new System.Drawing.Point(329, 121);
            this.numericUpDownLine.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDownLine.Name = "numericUpDownLine";
            this.numericUpDownLine.Size = new System.Drawing.Size(52, 20);
            this.numericUpDownLine.TabIndex = 17;
            this.numericUpDownLine.ValueChanged += new System.EventHandler(this.InsertionPointChanged);
            // 
            // numericUpDownCol
            // 
            this.numericUpDownCol.Location = new System.Drawing.Point(432, 121);
            this.numericUpDownCol.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDownCol.Name = "numericUpDownCol";
            this.numericUpDownCol.Size = new System.Drawing.Size(57, 20);
            this.numericUpDownCol.TabIndex = 18;
            this.numericUpDownCol.ValueChanged += new System.EventHandler(this.InsertionPointChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(293, 123);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(30, 13);
            this.label2.TabIndex = 19;
            this.label2.Text = "Line:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(401, 125);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(25, 13);
            this.label3.TabIndex = 20;
            this.label3.Text = "Col:";
            // 
            // MapTester
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(777, 602);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.numericUpDownCol);
            this.Controls.Add(this.numericUpDownLine);
            this.Controls.Add(this.checkBoxInsertionPoint);
            this.Controls.Add(this.overprintCheckBox);
            this.Controls.Add(this.showTemplatesCheckBox);
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
            this.Controls.Add(this.menuStrip1);
            this.Name = "MapTester";
            this.Text = "MapTester";
            this.Load += new System.EventHandler(this.MapTester_Load);
            this.panel1.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownLine)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownCol)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.VScrollBar vertScroll;
        private System.Windows.Forms.HScrollBar horizScroll;
        private InteractiveTestApp.MapView.MapViewer mapViewer;
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
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem dumpOCADFileToolStripMenuItem;
        private System.Windows.Forms.CheckBox showTemplatesCheckBox;
        private System.Windows.Forms.CheckBox overprintCheckBox;
        private System.Windows.Forms.CheckBox checkBoxInsertionPoint;
        private System.Windows.Forms.NumericUpDown numericUpDownLine;
        private System.Windows.Forms.NumericUpDown numericUpDownCol;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ToolStripMenuItem compareOcadFilesToolStripMenuItem;
    }
}
