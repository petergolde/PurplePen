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
    partial class PrintDescriptions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PrintDescriptions));
            this.printerGroup = new System.Windows.Forms.GroupBox();
            this.marginChange = new System.Windows.Forms.Button();
            this.paperSize = new System.Windows.Forms.Label();
            this.paperSizeLabel = new System.Windows.Forms.Label();
            this.margins = new System.Windows.Forms.Label();
            this.marginsLabel = new System.Windows.Forms.Label();
            this.orientation = new System.Windows.Forms.Label();
            this.orientationLabel = new System.Windows.Forms.Label();
            this.printerName = new System.Windows.Forms.Label();
            this.printerChange = new System.Windows.Forms.Button();
            this.printerLabel = new System.Windows.Forms.Label();
            this.layoutGroup = new System.Windows.Forms.GroupBox();
            this.descriptionKindCombo = new System.Windows.Forms.ComboBox();
            this.descriptionTypeLabel = new System.Windows.Forms.Label();
            this.mmSuffixLabel = new System.Windows.Forms.Label();
            this.boxSizeUpDown = new System.Windows.Forms.NumericUpDown();
            this.lineSizeLabel = new System.Windows.Forms.Label();
            this.cancelButton = new System.Windows.Forms.Button();
            this.printButton = new System.Windows.Forms.Button();
            this.previewButton = new System.Windows.Forms.Button();
            this.coursesGroupBox = new System.Windows.Forms.GroupBox();
            this.courseSelector = new PurplePen.CourseSelector();
            this.pageSetupDialog = new System.Windows.Forms.PageSetupDialog();
            this.copiesGroupBox = new System.Windows.Forms.GroupBox();
            this.descriptionsLabel = new System.Windows.Forms.Label();
            this.descriptionsUpDown = new System.Windows.Forms.NumericUpDown();
            this.copiesCombo = new System.Windows.Forms.ComboBox();
            this.whatToPrintLabel = new System.Windows.Forms.Label();
            this.pageSetupDialog1 = new System.Windows.Forms.PageSetupDialog();
            this.printDialog = new System.Windows.Forms.PrintDialog();
            this.printerGroup.SuspendLayout();
            this.layoutGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.boxSizeUpDown)).BeginInit();
            this.coursesGroupBox.SuspendLayout();
            this.copiesGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.descriptionsUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // printerGroup
            // 
            this.printerGroup.Controls.Add(this.marginChange);
            this.printerGroup.Controls.Add(this.paperSize);
            this.printerGroup.Controls.Add(this.paperSizeLabel);
            this.printerGroup.Controls.Add(this.margins);
            this.printerGroup.Controls.Add(this.marginsLabel);
            this.printerGroup.Controls.Add(this.orientation);
            this.printerGroup.Controls.Add(this.orientationLabel);
            this.printerGroup.Controls.Add(this.printerName);
            this.printerGroup.Controls.Add(this.printerChange);
            this.printerGroup.Controls.Add(this.printerLabel);
            resources.ApplyResources(this.printerGroup, "printerGroup");
            this.printerGroup.Name = "printerGroup";
            this.printerGroup.TabStop = false;
            // 
            // marginChange
            // 
            resources.ApplyResources(this.marginChange, "marginChange");
            this.marginChange.Name = "marginChange";
            this.marginChange.UseVisualStyleBackColor = true;
            this.marginChange.Click += new System.EventHandler(this.marginChange_Click);
            // 
            // paperSize
            // 
            resources.ApplyResources(this.paperSize, "paperSize");
            this.paperSize.Name = "paperSize";
            // 
            // paperSizeLabel
            // 
            resources.ApplyResources(this.paperSizeLabel, "paperSizeLabel");
            this.paperSizeLabel.Name = "paperSizeLabel";
            // 
            // margins
            // 
            resources.ApplyResources(this.margins, "margins");
            this.margins.Name = "margins";
            // 
            // marginsLabel
            // 
            resources.ApplyResources(this.marginsLabel, "marginsLabel");
            this.marginsLabel.Name = "marginsLabel";
            // 
            // orientation
            // 
            resources.ApplyResources(this.orientation, "orientation");
            this.orientation.Name = "orientation";
            // 
            // orientationLabel
            // 
            resources.ApplyResources(this.orientationLabel, "orientationLabel");
            this.orientationLabel.Name = "orientationLabel";
            // 
            // printerName
            // 
            resources.ApplyResources(this.printerName, "printerName");
            this.printerName.Name = "printerName";
            // 
            // printerChange
            // 
            resources.ApplyResources(this.printerChange, "printerChange");
            this.printerChange.Name = "printerChange";
            this.printerChange.UseVisualStyleBackColor = true;
            this.printerChange.Click += new System.EventHandler(this.printerChange_Click);
            // 
            // printerLabel
            // 
            resources.ApplyResources(this.printerLabel, "printerLabel");
            this.printerLabel.Name = "printerLabel";
            // 
            // layoutGroup
            // 
            this.layoutGroup.Controls.Add(this.descriptionKindCombo);
            this.layoutGroup.Controls.Add(this.descriptionTypeLabel);
            this.layoutGroup.Controls.Add(this.mmSuffixLabel);
            this.layoutGroup.Controls.Add(this.boxSizeUpDown);
            this.layoutGroup.Controls.Add(this.lineSizeLabel);
            resources.ApplyResources(this.layoutGroup, "layoutGroup");
            this.layoutGroup.Name = "layoutGroup";
            this.layoutGroup.TabStop = false;
            // 
            // descriptionKindCombo
            // 
            this.descriptionKindCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.descriptionKindCombo.FormattingEnabled = true;
            this.descriptionKindCombo.Items.AddRange(new object[] {
            resources.GetString("descriptionKindCombo.Items"),
            resources.GetString("descriptionKindCombo.Items1"),
            resources.GetString("descriptionKindCombo.Items2"),
            resources.GetString("descriptionKindCombo.Items3")});
            resources.ApplyResources(this.descriptionKindCombo, "descriptionKindCombo");
            this.descriptionKindCombo.Name = "descriptionKindCombo";
            this.descriptionKindCombo.SelectedIndexChanged += new System.EventHandler(this.descriptionKindCombo_SelectedIndexChanged);
            // 
            // descriptionTypeLabel
            // 
            resources.ApplyResources(this.descriptionTypeLabel, "descriptionTypeLabel");
            this.descriptionTypeLabel.Name = "descriptionTypeLabel";
            // 
            // mmSuffixLabel
            // 
            resources.ApplyResources(this.mmSuffixLabel, "mmSuffixLabel");
            this.mmSuffixLabel.Name = "mmSuffixLabel";
            // 
            // boxSizeUpDown
            // 
            this.boxSizeUpDown.DecimalPlaces = 1;
            this.boxSizeUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            resources.ApplyResources(this.boxSizeUpDown, "boxSizeUpDown");
            this.boxSizeUpDown.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.boxSizeUpDown.Minimum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.boxSizeUpDown.Name = "boxSizeUpDown";
            this.boxSizeUpDown.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // lineSizeLabel
            // 
            resources.ApplyResources(this.lineSizeLabel, "lineSizeLabel");
            this.lineSizeLabel.Name = "lineSizeLabel";
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // printButton
            // 
            resources.ApplyResources(this.printButton, "printButton");
            this.printButton.Name = "printButton";
            this.printButton.UseVisualStyleBackColor = true;
            this.printButton.Click += new System.EventHandler(this.printButton_Click);
            // 
            // previewButton
            // 
            resources.ApplyResources(this.previewButton, "previewButton");
            this.previewButton.Name = "previewButton";
            this.previewButton.UseVisualStyleBackColor = true;
            this.previewButton.Click += new System.EventHandler(this.previewButton_Click);
            // 
            // coursesGroupBox
            // 
            this.coursesGroupBox.Controls.Add(this.courseSelector);
            resources.ApplyResources(this.coursesGroupBox, "coursesGroupBox");
            this.coursesGroupBox.Name = "coursesGroupBox";
            this.coursesGroupBox.TabStop = false;
            // 
            // courseSelector
            // 
            resources.ApplyResources(this.courseSelector, "courseSelector");
            this.courseSelector.Name = "courseSelector";
            this.courseSelector.SelectedCourses = new PurplePen.Id<PurplePen.Course>[0];
            this.courseSelector.ShowAllControls = true;
            // 
            // copiesGroupBox
            // 
            this.copiesGroupBox.Controls.Add(this.descriptionsLabel);
            this.copiesGroupBox.Controls.Add(this.descriptionsUpDown);
            this.copiesGroupBox.Controls.Add(this.copiesCombo);
            this.copiesGroupBox.Controls.Add(this.whatToPrintLabel);
            resources.ApplyResources(this.copiesGroupBox, "copiesGroupBox");
            this.copiesGroupBox.Name = "copiesGroupBox";
            this.copiesGroupBox.TabStop = false;
            // 
            // descriptionsLabel
            // 
            resources.ApplyResources(this.descriptionsLabel, "descriptionsLabel");
            this.descriptionsLabel.Name = "descriptionsLabel";
            // 
            // descriptionsUpDown
            // 
            resources.ApplyResources(this.descriptionsUpDown, "descriptionsUpDown");
            this.descriptionsUpDown.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.descriptionsUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.descriptionsUpDown.Name = "descriptionsUpDown";
            this.descriptionsUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // copiesCombo
            // 
            this.copiesCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.copiesCombo.FormattingEnabled = true;
            this.copiesCombo.Items.AddRange(new object[] {
            resources.GetString("copiesCombo.Items"),
            resources.GetString("copiesCombo.Items1"),
            resources.GetString("copiesCombo.Items2")});
            resources.ApplyResources(this.copiesCombo, "copiesCombo");
            this.copiesCombo.Name = "copiesCombo";
            this.copiesCombo.SelectedIndexChanged += new System.EventHandler(this.copiesCombo_SelectedIndexChanged);
            // 
            // whatToPrintLabel
            // 
            resources.ApplyResources(this.whatToPrintLabel, "whatToPrintLabel");
            this.whatToPrintLabel.Name = "whatToPrintLabel";
            // 
            // printDialog
            // 
            this.printDialog.UseEXDialog = true;
            // 
            // PrintDescriptions
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.copiesGroupBox);
            this.Controls.Add(this.coursesGroupBox);
            this.Controls.Add(this.previewButton);
            this.Controls.Add(this.printButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.layoutGroup);
            this.Controls.Add(this.printerGroup);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.HelpButton = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PrintDescriptions";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.PrintDescriptions_HelpButtonClicked);
            this.printerGroup.ResumeLayout(false);
            this.printerGroup.PerformLayout();
            this.layoutGroup.ResumeLayout(false);
            this.layoutGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) (this.boxSizeUpDown)).EndInit();
            this.coursesGroupBox.ResumeLayout(false);
            this.copiesGroupBox.ResumeLayout(false);
            this.copiesGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) (this.descriptionsUpDown)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox printerGroup;
        private System.Windows.Forms.Button printerChange;
        private System.Windows.Forms.Label printerLabel;
        private System.Windows.Forms.GroupBox layoutGroup;
        private System.Windows.Forms.Label mmSuffixLabel;
        private System.Windows.Forms.NumericUpDown boxSizeUpDown;
        private System.Windows.Forms.Label lineSizeLabel;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button printButton;
        private System.Windows.Forms.Button previewButton;
        private System.Windows.Forms.ComboBox descriptionKindCombo;
        private System.Windows.Forms.Label descriptionTypeLabel;
        private System.Windows.Forms.Label marginsLabel;
        private System.Windows.Forms.Label orientation;
        private System.Windows.Forms.Label orientationLabel;
        private System.Windows.Forms.Label printerName;
        private System.Windows.Forms.Label margins;
        private System.Windows.Forms.GroupBox coursesGroupBox;
        private System.Windows.Forms.Label paperSizeLabel;
        private System.Windows.Forms.Label paperSize;
        private System.Windows.Forms.PageSetupDialog pageSetupDialog;
        private System.Windows.Forms.GroupBox copiesGroupBox;
        private System.Windows.Forms.ComboBox copiesCombo;
        private System.Windows.Forms.Label whatToPrintLabel;
        private System.Windows.Forms.Label descriptionsLabel;
        private System.Windows.Forms.NumericUpDown descriptionsUpDown;
        private CourseSelector courseSelector;
        private System.Windows.Forms.PageSetupDialog pageSetupDialog1;
        private System.Windows.Forms.Button marginChange;
        private System.Windows.Forms.PrintDialog printDialog;
    }
}
