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
    partial class CreatePdfCourses
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreatePdfCourses));
            this.coursesGroupBox = new System.Windows.Forms.GroupBox();
            this.courseSelector = new PurplePen.CourseSelector();
            this.groupBoxAppearance = new System.Windows.Forms.GroupBox();
            this.checkBoxMergeParts = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.labelColorModel = new System.Windows.Forms.Label();
            this.comboBoxColorModel = new System.Windows.Forms.ComboBox();
            this.comboBoxMultiPage = new System.Windows.Forms.ComboBox();
            this.labelAppearanceInfo = new System.Windows.Forms.Label();
            this.folderGroupBox = new System.Windows.Forms.GroupBox();
            this.otherDirectoryTextBox = new System.Windows.Forms.TextBox();
            this.selectOtherDirectoryButton = new System.Windows.Forms.Button();
            this.otherDirectory = new System.Windows.Forms.RadioButton();
            this.mapDirectory = new System.Windows.Forms.RadioButton();
            this.coursesDirectory = new System.Windows.Forms.RadioButton();
            this.outputGroupBox = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.fileNamePrefixLabel = new System.Windows.Forms.Label();
            this.filenamePrefixTextBox = new System.Windows.Forms.TextBox();
            this.filesLabel = new System.Windows.Forms.Label();
            this.comboBoxFileFormat = new System.Windows.Forms.ComboBox();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.coursesGroupBox.SuspendLayout();
            this.groupBoxAppearance.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.folderGroupBox.SuspendLayout();
            this.outputGroupBox.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
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
            this.courseSelector.ShowAllControls = true;
            this.courseSelector.ShowCourseParts = false;
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
            resources.GetString("comboBoxColorModel.Items1")});
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
            // folderGroupBox
            // 
            this.folderGroupBox.Controls.Add(this.otherDirectoryTextBox);
            this.folderGroupBox.Controls.Add(this.selectOtherDirectoryButton);
            this.folderGroupBox.Controls.Add(this.otherDirectory);
            this.folderGroupBox.Controls.Add(this.mapDirectory);
            this.folderGroupBox.Controls.Add(this.coursesDirectory);
            resources.ApplyResources(this.folderGroupBox, "folderGroupBox");
            this.folderGroupBox.Name = "folderGroupBox";
            this.folderGroupBox.TabStop = false;
            // 
            // otherDirectoryTextBox
            // 
            resources.ApplyResources(this.otherDirectoryTextBox, "otherDirectoryTextBox");
            this.otherDirectoryTextBox.Name = "otherDirectoryTextBox";
            // 
            // selectOtherDirectoryButton
            // 
            resources.ApplyResources(this.selectOtherDirectoryButton, "selectOtherDirectoryButton");
            this.selectOtherDirectoryButton.Name = "selectOtherDirectoryButton";
            this.selectOtherDirectoryButton.UseVisualStyleBackColor = true;
            this.selectOtherDirectoryButton.Click += new System.EventHandler(this.selectOtherDirectoryButton_Click);
            // 
            // otherDirectory
            // 
            resources.ApplyResources(this.otherDirectory, "otherDirectory");
            this.otherDirectory.Name = "otherDirectory";
            this.otherDirectory.TabStop = true;
            this.otherDirectory.UseVisualStyleBackColor = true;
            this.otherDirectory.CheckedChanged += new System.EventHandler(this.otherDirectory_CheckedChanged);
            // 
            // mapDirectory
            // 
            resources.ApplyResources(this.mapDirectory, "mapDirectory");
            this.mapDirectory.Name = "mapDirectory";
            this.mapDirectory.TabStop = true;
            this.mapDirectory.UseVisualStyleBackColor = true;
            // 
            // coursesDirectory
            // 
            resources.ApplyResources(this.coursesDirectory, "coursesDirectory");
            this.coursesDirectory.Name = "coursesDirectory";
            this.coursesDirectory.TabStop = true;
            this.coursesDirectory.UseVisualStyleBackColor = true;
            // 
            // outputGroupBox
            // 
            this.outputGroupBox.Controls.Add(this.tableLayoutPanel2);
            resources.ApplyResources(this.outputGroupBox, "outputGroupBox");
            this.outputGroupBox.Name = "outputGroupBox";
            this.outputGroupBox.TabStop = false;
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.fileNamePrefixLabel, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.filenamePrefixTextBox, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.filesLabel, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.comboBoxFileFormat, 1, 1);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // fileNamePrefixLabel
            // 
            resources.ApplyResources(this.fileNamePrefixLabel, "fileNamePrefixLabel");
            this.fileNamePrefixLabel.Name = "fileNamePrefixLabel";
            // 
            // filenamePrefixTextBox
            // 
            resources.ApplyResources(this.filenamePrefixTextBox, "filenamePrefixTextBox");
            this.filenamePrefixTextBox.Name = "filenamePrefixTextBox";
            // 
            // filesLabel
            // 
            resources.ApplyResources(this.filesLabel, "filesLabel");
            this.filesLabel.Name = "filesLabel";
            // 
            // comboBoxFileFormat
            // 
            resources.ApplyResources(this.comboBoxFileFormat, "comboBoxFileFormat");
            this.comboBoxFileFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxFileFormat.FormattingEnabled = true;
            this.comboBoxFileFormat.Items.AddRange(new object[] {
            resources.GetString("comboBoxFileFormat.Items"),
            resources.GetString("comboBoxFileFormat.Items1"),
            resources.GetString("comboBoxFileFormat.Items2")});
            this.comboBoxFileFormat.Name = "comboBoxFileFormat";
            // 
            // folderBrowserDialog
            // 
            resources.ApplyResources(this.folderBrowserDialog, "folderBrowserDialog");
            // 
            // CreatePdfCourses
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.outputGroupBox);
            this.Controls.Add(this.folderGroupBox);
            this.Controls.Add(this.groupBoxAppearance);
            this.Controls.Add(this.coursesGroupBox);
            this.HelpTopic = "FileCreatePdfFiles.htm";
            this.Name = "CreatePdfCourses";
            this.Controls.SetChildIndex(this.coursesGroupBox, 0);
            this.Controls.SetChildIndex(this.groupBoxAppearance, 0);
            this.Controls.SetChildIndex(this.folderGroupBox, 0);
            this.Controls.SetChildIndex(this.outputGroupBox, 0);
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.coursesGroupBox.ResumeLayout(false);
            this.groupBoxAppearance.ResumeLayout(false);
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.folderGroupBox.ResumeLayout(false);
            this.folderGroupBox.PerformLayout();
            this.outputGroupBox.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.GroupBox coursesGroupBox;
        private CourseSelector courseSelector;
        private System.Windows.Forms.GroupBox groupBoxAppearance;
        private System.Windows.Forms.Label labelAppearanceInfo;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.ComboBox comboBoxColorModel;
        private System.Windows.Forms.Label labelColorModel;
        private System.Windows.Forms.ComboBox comboBoxMultiPage;
        private System.Windows.Forms.CheckBox checkBoxMergeParts;
        private System.Windows.Forms.GroupBox folderGroupBox;
        private System.Windows.Forms.TextBox otherDirectoryTextBox;
        private System.Windows.Forms.Button selectOtherDirectoryButton;
        private System.Windows.Forms.RadioButton otherDirectory;
        private System.Windows.Forms.RadioButton mapDirectory;
        private System.Windows.Forms.RadioButton coursesDirectory;
        private System.Windows.Forms.GroupBox outputGroupBox;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label fileNamePrefixLabel;
        private System.Windows.Forms.TextBox filenamePrefixTextBox;
        private System.Windows.Forms.Label filesLabel;
        private System.Windows.Forms.ComboBox comboBoxFileFormat;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
    }
}
