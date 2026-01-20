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
    partial class PrintCourses
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PrintCourses));
            this.printerGroup = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.printerChange = new System.Windows.Forms.Button();
            this.printerName = new System.Windows.Forms.Label();
            this.printerLabel = new System.Windows.Forms.Label();
            this.previewButton = new System.Windows.Forms.Button();
            this.coursesGroupBox = new System.Windows.Forms.GroupBox();
            this.courseSelector = new PurplePen.CourseSelector();
            this.copiesGroupBox = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.copiesUpDown = new System.Windows.Forms.NumericUpDown();
            this.copiesLabel = new System.Windows.Forms.Label();
            this.checkBoxPausePrinting = new System.Windows.Forms.CheckBox();
            this.printDialog = new System.Windows.Forms.PrintDialog();
            this.groupBoxAppearance = new System.Windows.Forms.GroupBox();
            this.checkBoxMergeParts = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.labelColorModel = new System.Windows.Forms.Label();
            this.comboBoxColorModel = new System.Windows.Forms.ComboBox();
            this.comboBoxMultiPage = new System.Windows.Forms.ComboBox();
            this.labelAppearanceInfo = new System.Windows.Forms.Label();
            this.printerGroup.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.coursesGroupBox.SuspendLayout();
            this.copiesGroupBox.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.copiesUpDown)).BeginInit();
            this.groupBoxAppearance.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.SuspendLayout();
            // 
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            resources.ApplyResources(this.cancelButton, "cancelButton");
            // 
            // printerGroup
            // 
            this.printerGroup.Controls.Add(this.tableLayoutPanel1);
            resources.ApplyResources(this.printerGroup, "printerGroup");
            this.printerGroup.Name = "printerGroup";
            this.printerGroup.TabStop = false;
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.printerChange, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.printerName, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.printerLabel, 0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // printerChange
            // 
            resources.ApplyResources(this.printerChange, "printerChange");
            this.printerChange.Name = "printerChange";
            this.printerChange.UseVisualStyleBackColor = true;
            this.printerChange.Click += new System.EventHandler(this.printerChange_Click);
            // 
            // printerName
            // 
            resources.ApplyResources(this.printerName, "printerName");
            this.printerName.Name = "printerName";
            // 
            // printerLabel
            // 
            resources.ApplyResources(this.printerLabel, "printerLabel");
            this.printerLabel.Name = "printerLabel";
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
            this.courseSelector.Filter = null;
            resources.ApplyResources(this.courseSelector, "courseSelector");
            this.courseSelector.Name = "courseSelector";
            this.courseSelector.ShowAllControls = true;
            this.courseSelector.ShowCourseParts = false;
            this.courseSelector.ShowVariationChooser = true;
            // 
            // copiesGroupBox
            // 
            this.copiesGroupBox.Controls.Add(this.tableLayoutPanel2);
            resources.ApplyResources(this.copiesGroupBox, "copiesGroupBox");
            this.copiesGroupBox.Name = "copiesGroupBox";
            this.copiesGroupBox.TabStop = false;
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.copiesUpDown, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.copiesLabel, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.checkBoxPausePrinting, 0, 1);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // copiesUpDown
            // 
            resources.ApplyResources(this.copiesUpDown, "copiesUpDown");
            this.copiesUpDown.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.copiesUpDown.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.copiesUpDown.Name = "copiesUpDown";
            this.copiesUpDown.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // copiesLabel
            // 
            resources.ApplyResources(this.copiesLabel, "copiesLabel");
            this.copiesLabel.Name = "copiesLabel";
            // 
            // checkBoxPausePrinting
            // 
            this.tableLayoutPanel2.SetColumnSpan(this.checkBoxPausePrinting, 3);
            resources.ApplyResources(this.checkBoxPausePrinting, "checkBoxPausePrinting");
            this.checkBoxPausePrinting.Name = "checkBoxPausePrinting";
            this.checkBoxPausePrinting.UseVisualStyleBackColor = true;
            // 
            // printDialog
            // 
            this.printDialog.AllowPrintToFile = false;
            // 
            // groupBoxAppearance
            // 
            this.groupBoxAppearance.Controls.Add(this.checkBoxMergeParts);
            this.groupBoxAppearance.Controls.Add(this.tableLayoutPanel4);
            this.groupBoxAppearance.Controls.Add(this.comboBoxMultiPage);
            this.groupBoxAppearance.Controls.Add(this.labelAppearanceInfo);
            resources.ApplyResources(this.groupBoxAppearance, "groupBoxAppearance");
            this.groupBoxAppearance.Name = "groupBoxAppearance";
            this.groupBoxAppearance.TabStop = false;
            // 
            // checkBoxMergeParts
            // 
            resources.ApplyResources(this.checkBoxMergeParts, "checkBoxMergeParts");
            this.checkBoxMergeParts.Name = "checkBoxMergeParts";
            this.checkBoxMergeParts.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel4
            // 
            resources.ApplyResources(this.tableLayoutPanel4, "tableLayoutPanel4");
            this.tableLayoutPanel4.Controls.Add(this.labelColorModel, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.comboBoxColorModel, 1, 0);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            // 
            // labelColorModel
            // 
            resources.ApplyResources(this.labelColorModel, "labelColorModel");
            this.labelColorModel.Name = "labelColorModel";
            // 
            // comboBoxColorModel
            // 
            resources.ApplyResources(this.comboBoxColorModel, "comboBoxColorModel");
            this.comboBoxColorModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxColorModel.FormattingEnabled = true;
            this.comboBoxColorModel.Items.AddRange(new object[] {
            resources.GetString("comboBoxColorModel.Items"),
            resources.GetString("comboBoxColorModel.Items1"),
            resources.GetString("comboBoxColorModel.Items2")});
            this.comboBoxColorModel.Name = "comboBoxColorModel";
            // 
            // comboBoxMultiPage
            // 
            this.comboBoxMultiPage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxMultiPage.FormattingEnabled = true;
            this.comboBoxMultiPage.Items.AddRange(new object[] {
            resources.GetString("comboBoxMultiPage.Items"),
            resources.GetString("comboBoxMultiPage.Items1")});
            resources.ApplyResources(this.comboBoxMultiPage, "comboBoxMultiPage");
            this.comboBoxMultiPage.Name = "comboBoxMultiPage";
            // 
            // labelAppearanceInfo
            // 
            resources.ApplyResources(this.labelAppearanceInfo, "labelAppearanceInfo");
            this.labelAppearanceInfo.Name = "labelAppearanceInfo";
            // 
            // PrintCourses
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.groupBoxAppearance);
            this.Controls.Add(this.coursesGroupBox);
            this.Controls.Add(this.previewButton);
            this.Controls.Add(this.copiesGroupBox);
            this.Controls.Add(this.printerGroup);
            this.HelpTopic = "FilePrintCourses.htm";
            this.Name = "PrintCourses";
            this.Controls.SetChildIndex(this.printerGroup, 0);
            this.Controls.SetChildIndex(this.copiesGroupBox, 0);
            this.Controls.SetChildIndex(this.previewButton, 0);
            this.Controls.SetChildIndex(this.coursesGroupBox, 0);
            this.Controls.SetChildIndex(this.groupBoxAppearance, 0);
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.printerGroup.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.coursesGroupBox.ResumeLayout(false);
            this.copiesGroupBox.ResumeLayout(false);
            this.copiesGroupBox.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.copiesUpDown)).EndInit();
            this.groupBoxAppearance.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox printerGroup;
        private System.Windows.Forms.Button previewButton;
        private System.Windows.Forms.GroupBox coursesGroupBox;
        private System.Windows.Forms.GroupBox copiesGroupBox;
        private CourseSelector courseSelector;
        private System.Windows.Forms.PrintDialog printDialog;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button printerChange;
        private System.Windows.Forms.Label printerName;
        private System.Windows.Forms.Label printerLabel;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.NumericUpDown copiesUpDown;
        private System.Windows.Forms.Label copiesLabel;
        private System.Windows.Forms.GroupBox groupBoxAppearance;
        private System.Windows.Forms.Label labelAppearanceInfo;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.ComboBox comboBoxColorModel;
        private System.Windows.Forms.Label labelColorModel;
        private System.Windows.Forms.ComboBox comboBoxMultiPage;
        private System.Windows.Forms.CheckBox checkBoxMergeParts;
        private System.Windows.Forms.CheckBox checkBoxPausePrinting;
    }
}
