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
    partial class CreateImageFiles
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateImageFiles));
            this.coursesGroupBox = new System.Windows.Forms.GroupBox();
            this.courseSelector = new PurplePen.CourseSelector();
            this.createButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.outputGroupBox = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.filenamePrefixTextBox = new System.Windows.Forms.TextBox();
            this.fileFormatLabel = new System.Windows.Forms.Label();
            this.fileFormatCombo = new System.Windows.Forms.ComboBox();
            this.fileNamePrefixLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBoxDpi = new System.Windows.Forms.ComboBox();
            this.dpiLabel = new System.Windows.Forms.Label();
            this.comboBoxWorldFile = new System.Windows.Forms.ComboBox();
            this.labelColorModel = new System.Windows.Forms.Label();
            this.comboBoxColorModel = new System.Windows.Forms.ComboBox();
            this.folderGroupBox = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.selectOtherDirectoryButton = new System.Windows.Forms.Button();
            this.coursesDirectory = new System.Windows.Forms.RadioButton();
            this.mapDirectory = new System.Windows.Forms.RadioButton();
            this.otherDirectory = new System.Windows.Forms.RadioButton();
            this.otherDirectoryTextBox = new System.Windows.Forms.TextBox();
            this.coursesGroupBox.SuspendLayout();
            this.outputGroupBox.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.folderGroupBox.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
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
            // createButton
            // 
            resources.ApplyResources(this.createButton, "createButton");
            this.createButton.Name = "createButton";
            this.createButton.UseVisualStyleBackColor = true;
            this.createButton.Click += new System.EventHandler(this.createButton_Click);
            // 
            // cancelButton
            // 
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // folderBrowserDialog
            // 
            resources.ApplyResources(this.folderBrowserDialog, "folderBrowserDialog");
            // 
            // outputGroupBox
            // 
            this.outputGroupBox.Controls.Add(this.tableLayoutPanel1);
            resources.ApplyResources(this.outputGroupBox, "outputGroupBox");
            this.outputGroupBox.Name = "outputGroupBox";
            this.outputGroupBox.TabStop = false;
            this.outputGroupBox.Enter += new System.EventHandler(this.outputGroupBox_Enter);
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.filenamePrefixTextBox, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.fileFormatLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.fileFormatCombo, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.fileNamePrefixLabel, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxDpi, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.dpiLabel, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxWorldFile, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.labelColorModel, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxColorModel, 1, 2);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // filenamePrefixTextBox
            // 
            resources.ApplyResources(this.filenamePrefixTextBox, "filenamePrefixTextBox");
            this.filenamePrefixTextBox.Name = "filenamePrefixTextBox";
            // 
            // fileFormatLabel
            // 
            resources.ApplyResources(this.fileFormatLabel, "fileFormatLabel");
            this.fileFormatLabel.Name = "fileFormatLabel";
            // 
            // fileFormatCombo
            // 
            resources.ApplyResources(this.fileFormatCombo, "fileFormatCombo");
            this.fileFormatCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.fileFormatCombo.FormattingEnabled = true;
            this.fileFormatCombo.Items.AddRange(new object[] {
            resources.GetString("fileFormatCombo.Items"),
            resources.GetString("fileFormatCombo.Items1"),
            resources.GetString("fileFormatCombo.Items2")});
            this.fileFormatCombo.Name = "fileFormatCombo";
            // 
            // fileNamePrefixLabel
            // 
            resources.ApplyResources(this.fileNamePrefixLabel, "fileNamePrefixLabel");
            this.fileNamePrefixLabel.Name = "fileNamePrefixLabel";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // comboBoxDpi
            // 
            resources.ApplyResources(this.comboBoxDpi, "comboBoxDpi");
            this.comboBoxDpi.FormattingEnabled = true;
            this.comboBoxDpi.Items.AddRange(new object[] {
            resources.GetString("comboBoxDpi.Items"),
            resources.GetString("comboBoxDpi.Items1"),
            resources.GetString("comboBoxDpi.Items2"),
            resources.GetString("comboBoxDpi.Items3"),
            resources.GetString("comboBoxDpi.Items4"),
            resources.GetString("comboBoxDpi.Items5"),
            resources.GetString("comboBoxDpi.Items6")});
            this.comboBoxDpi.Name = "comboBoxDpi";
            // 
            // dpiLabel
            // 
            resources.ApplyResources(this.dpiLabel, "dpiLabel");
            this.dpiLabel.Name = "dpiLabel";
            // 
            // comboBoxWorldFile
            // 
            resources.ApplyResources(this.comboBoxWorldFile, "comboBoxWorldFile");
            this.comboBoxWorldFile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxWorldFile.FormattingEnabled = true;
            this.comboBoxWorldFile.Items.AddRange(new object[] {
            resources.GetString("comboBoxWorldFile.Items"),
            resources.GetString("comboBoxWorldFile.Items1")});
            this.comboBoxWorldFile.Name = "comboBoxWorldFile";
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
            resources.GetString("comboBoxColorModel.Items1")});
            this.comboBoxColorModel.Name = "comboBoxColorModel";
            // 
            // folderGroupBox
            // 
            this.folderGroupBox.Controls.Add(this.tableLayoutPanel2);
            resources.ApplyResources(this.folderGroupBox, "folderGroupBox");
            this.folderGroupBox.Name = "folderGroupBox";
            this.folderGroupBox.TabStop = false;
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.selectOtherDirectoryButton, 0, 4);
            this.tableLayoutPanel2.Controls.Add(this.coursesDirectory, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.mapDirectory, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.otherDirectory, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.otherDirectoryTextBox, 0, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // selectOtherDirectoryButton
            // 
            resources.ApplyResources(this.selectOtherDirectoryButton, "selectOtherDirectoryButton");
            this.selectOtherDirectoryButton.Name = "selectOtherDirectoryButton";
            this.selectOtherDirectoryButton.UseVisualStyleBackColor = true;
            this.selectOtherDirectoryButton.Click += new System.EventHandler(this.selectOtherDirectoryButton_Click);
            // 
            // coursesDirectory
            // 
            resources.ApplyResources(this.coursesDirectory, "coursesDirectory");
            this.coursesDirectory.Name = "coursesDirectory";
            this.coursesDirectory.TabStop = true;
            this.coursesDirectory.UseVisualStyleBackColor = true;
            // 
            // mapDirectory
            // 
            resources.ApplyResources(this.mapDirectory, "mapDirectory");
            this.mapDirectory.Name = "mapDirectory";
            this.mapDirectory.TabStop = true;
            this.mapDirectory.UseVisualStyleBackColor = true;
            // 
            // otherDirectory
            // 
            resources.ApplyResources(this.otherDirectory, "otherDirectory");
            this.otherDirectory.Name = "otherDirectory";
            this.otherDirectory.TabStop = true;
            this.otherDirectory.UseVisualStyleBackColor = true;
            this.otherDirectory.CheckedChanged += new System.EventHandler(this.otherDirectory_CheckedChanged);
            // 
            // otherDirectoryTextBox
            // 
            resources.ApplyResources(this.otherDirectoryTextBox, "otherDirectoryTextBox");
            this.otherDirectoryTextBox.Name = "otherDirectoryTextBox";
            this.otherDirectoryTextBox.TextChanged += new System.EventHandler(this.otherDirectoryTextBox_TextChanged);
            // 
            // CreateImageFiles
            // 
            this.AcceptButton = this.createButton;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.cancelButton;
            this.Controls.Add(this.folderGroupBox);
            this.Controls.Add(this.outputGroupBox);
            this.Controls.Add(this.coursesGroupBox);
            this.Controls.Add(this.createButton);
            this.Controls.Add(this.cancelButton);
            this.HelpTopic = "FileCreateImageFiles.htm";
            this.Name = "CreateImageFiles";
            this.coursesGroupBox.ResumeLayout(false);
            this.outputGroupBox.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.folderGroupBox.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox coursesGroupBox;
        private CourseSelector courseSelector;
        private System.Windows.Forms.Button createButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.GroupBox outputGroupBox;
        private System.Windows.Forms.GroupBox folderGroupBox;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label fileNamePrefixLabel;
        private System.Windows.Forms.TextBox filenamePrefixTextBox;
        private System.Windows.Forms.Label fileFormatLabel;
        private System.Windows.Forms.ComboBox fileFormatCombo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBoxDpi;
        private System.Windows.Forms.Label dpiLabel;
        private System.Windows.Forms.ComboBox comboBoxWorldFile;
        private System.Windows.Forms.Label labelColorModel;
        private System.Windows.Forms.ComboBox comboBoxColorModel;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.RadioButton coursesDirectory;
        private System.Windows.Forms.RadioButton mapDirectory;
        private System.Windows.Forms.RadioButton otherDirectory;
        private System.Windows.Forms.TextBox otherDirectoryTextBox;
        private System.Windows.Forms.Button selectOtherDirectoryButton;
    }
}
