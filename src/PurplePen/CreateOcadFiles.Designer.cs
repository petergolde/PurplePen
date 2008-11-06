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
    partial class CreateOcadFiles
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateOcadFiles));
            this.coursesGroupBox = new System.Windows.Forms.GroupBox();
            this.courseSelector = new PurplePen.CourseSelector();
            this.createButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.outputGroupBox = new System.Windows.Forms.GroupBox();
            this.filenamePrefixTextBox = new System.Windows.Forms.TextBox();
            this.fileNamePrefixLabel = new System.Windows.Forms.Label();
            this.fileFormatCombo = new System.Windows.Forms.ComboBox();
            this.fileFormatLabel = new System.Windows.Forms.Label();
            this.folderGroupBox = new System.Windows.Forms.GroupBox();
            this.otherDirectoryTextBox = new System.Windows.Forms.TextBox();
            this.selectOtherDirectoryButton = new System.Windows.Forms.Button();
            this.otherDirectory = new System.Windows.Forms.RadioButton();
            this.mapDirectory = new System.Windows.Forms.RadioButton();
            this.coursesDirectory = new System.Windows.Forms.RadioButton();
            this.coursesGroupBox.SuspendLayout();
            this.outputGroupBox.SuspendLayout();
            this.folderGroupBox.SuspendLayout();
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
            resources.ApplyResources(this.courseSelector, "courseSelector");
            this.courseSelector.Name = "courseSelector";
            this.courseSelector.SelectedCourses = new PurplePen.Id<PurplePen.Course>[0];
            this.courseSelector.ShowAllControls = true;
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
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // folderBrowserDialog
            // 
            resources.ApplyResources(this.folderBrowserDialog, "folderBrowserDialog");
            // 
            // outputGroupBox
            // 
            this.outputGroupBox.Controls.Add(this.filenamePrefixTextBox);
            this.outputGroupBox.Controls.Add(this.fileNamePrefixLabel);
            this.outputGroupBox.Controls.Add(this.fileFormatCombo);
            this.outputGroupBox.Controls.Add(this.fileFormatLabel);
            resources.ApplyResources(this.outputGroupBox, "outputGroupBox");
            this.outputGroupBox.Name = "outputGroupBox";
            this.outputGroupBox.TabStop = false;
            // 
            // filenamePrefixTextBox
            // 
            resources.ApplyResources(this.filenamePrefixTextBox, "filenamePrefixTextBox");
            this.filenamePrefixTextBox.Name = "filenamePrefixTextBox";
            // 
            // fileNamePrefixLabel
            // 
            resources.ApplyResources(this.fileNamePrefixLabel, "fileNamePrefixLabel");
            this.fileNamePrefixLabel.Name = "fileNamePrefixLabel";
            // 
            // fileFormatCombo
            // 
            this.fileFormatCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.fileFormatCombo.FormattingEnabled = true;
            this.fileFormatCombo.Items.AddRange(new object[] {
            resources.GetString("fileFormatCombo.Items"),
            resources.GetString("fileFormatCombo.Items1"),
            resources.GetString("fileFormatCombo.Items2"),
            resources.GetString("fileFormatCombo.Items3")});
            resources.ApplyResources(this.fileFormatCombo, "fileFormatCombo");
            this.fileFormatCombo.Name = "fileFormatCombo";
            // 
            // fileFormatLabel
            // 
            resources.ApplyResources(this.fileFormatLabel, "fileFormatLabel");
            this.fileFormatLabel.Name = "fileFormatLabel";
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
            // CreateOcadFiles
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
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.HelpButton = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CreateOcadFiles";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.CreateOcadFiles_HelpButtonClicked);
            this.coursesGroupBox.ResumeLayout(false);
            this.outputGroupBox.ResumeLayout(false);
            this.outputGroupBox.PerformLayout();
            this.folderGroupBox.ResumeLayout(false);
            this.folderGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox coursesGroupBox;
        private CourseSelector courseSelector;
        private System.Windows.Forms.Button createButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.GroupBox outputGroupBox;
        private System.Windows.Forms.ComboBox fileFormatCombo;
        private System.Windows.Forms.Label fileFormatLabel;
        private System.Windows.Forms.GroupBox folderGroupBox;
        private System.Windows.Forms.RadioButton otherDirectory;
        private System.Windows.Forms.RadioButton mapDirectory;
        private System.Windows.Forms.RadioButton coursesDirectory;
        private System.Windows.Forms.Button selectOtherDirectoryButton;
        private System.Windows.Forms.TextBox otherDirectoryTextBox;
        private System.Windows.Forms.Label fileNamePrefixLabel;
        private System.Windows.Forms.TextBox filenamePrefixTextBox;
    }
}
