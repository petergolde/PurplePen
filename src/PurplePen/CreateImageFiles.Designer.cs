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
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.tableLayoutPanel5 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.coursesGroupBox = new System.Windows.Forms.GroupBox();
            this.courseSelector = new PurplePen.CourseSelector();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.folderGroupBox = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.coursesDirectory = new System.Windows.Forms.RadioButton();
            this.mapDirectory = new System.Windows.Forms.RadioButton();
            this.otherDirectory = new System.Windows.Forms.RadioButton();
            this.otherDirectoryTextBox = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel7 = new System.Windows.Forms.TableLayoutPanel();
            this.selectOtherDirectoryButton = new System.Windows.Forms.Button();
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
            this.tableLayoutPanel6 = new System.Windows.Forms.TableLayoutPanel();
            this.createButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.tableLayoutPanel5.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.coursesGroupBox.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.folderGroupBox.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel7.SuspendLayout();
            this.outputGroupBox.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel6.SuspendLayout();
            this.SuspendLayout();
            // 
            // folderBrowserDialog
            // 
            resources.ApplyResources(this.folderBrowserDialog, "folderBrowserDialog");
            // 
            // tableLayoutPanel5
            // 
            resources.ApplyResources(this.tableLayoutPanel5, "tableLayoutPanel5");
            this.tableLayoutPanel5.Controls.Add(this.tableLayoutPanel3, 0, 0);
            this.tableLayoutPanel5.Controls.Add(this.tableLayoutPanel6, 0, 1);
            this.tableLayoutPanel5.Name = "tableLayoutPanel5";
            // 
            // tableLayoutPanel3
            // 
            resources.ApplyResources(this.tableLayoutPanel3, "tableLayoutPanel3");
            this.tableLayoutPanel3.Controls.Add(this.coursesGroupBox, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.tableLayoutPanel4, 1, 0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            // 
            // coursesGroupBox
            // 
            resources.ApplyResources(this.coursesGroupBox, "coursesGroupBox");
            this.coursesGroupBox.Controls.Add(this.courseSelector);
            this.coursesGroupBox.Name = "coursesGroupBox";
            this.coursesGroupBox.TabStop = false;
            // 
            // courseSelector
            // 
            resources.ApplyResources(this.courseSelector, "courseSelector");
            this.courseSelector.Filter = null;
            this.courseSelector.Name = "courseSelector";
            this.courseSelector.ShowAllControls = true;
            this.courseSelector.ShowCourseParts = false;
            this.courseSelector.ShowVariationChooser = true;
            // 
            // tableLayoutPanel4
            // 
            resources.ApplyResources(this.tableLayoutPanel4, "tableLayoutPanel4");
            this.tableLayoutPanel4.Controls.Add(this.folderGroupBox, 0, 1);
            this.tableLayoutPanel4.Controls.Add(this.outputGroupBox, 0, 0);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            // 
            // folderGroupBox
            // 
            resources.ApplyResources(this.folderGroupBox, "folderGroupBox");
            this.folderGroupBox.Controls.Add(this.tableLayoutPanel2);
            this.folderGroupBox.Name = "folderGroupBox";
            this.folderGroupBox.TabStop = false;
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.coursesDirectory, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.mapDirectory, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.otherDirectory, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.otherDirectoryTextBox, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel7, 0, 4);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
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
            // 
            // tableLayoutPanel7
            // 
            resources.ApplyResources(this.tableLayoutPanel7, "tableLayoutPanel7");
            this.tableLayoutPanel7.Controls.Add(this.selectOtherDirectoryButton, 1, 0);
            this.tableLayoutPanel7.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
            this.tableLayoutPanel7.Name = "tableLayoutPanel7";
            // 
            // selectOtherDirectoryButton
            // 
            resources.ApplyResources(this.selectOtherDirectoryButton, "selectOtherDirectoryButton");
            this.selectOtherDirectoryButton.Name = "selectOtherDirectoryButton";
            this.selectOtherDirectoryButton.UseVisualStyleBackColor = true;
            this.selectOtherDirectoryButton.Click += new System.EventHandler(this.selectOtherDirectoryButton_Click);
            // 
            // outputGroupBox
            // 
            resources.ApplyResources(this.outputGroupBox, "outputGroupBox");
            this.outputGroupBox.Controls.Add(this.tableLayoutPanel1);
            this.outputGroupBox.Name = "outputGroupBox";
            this.outputGroupBox.TabStop = false;
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
            // tableLayoutPanel6
            // 
            resources.ApplyResources(this.tableLayoutPanel6, "tableLayoutPanel6");
            this.tableLayoutPanel6.Controls.Add(this.createButton, 1, 0);
            this.tableLayoutPanel6.Controls.Add(this.cancelButton, 2, 0);
            this.tableLayoutPanel6.Name = "tableLayoutPanel6";
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
            // CreateImageFiles
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.tableLayoutPanel5);
            this.HelpTopic = "FileCreateImageFiles.htm";
            this.Name = "CreateImageFiles";
            this.tableLayoutPanel5.ResumeLayout(false);
            this.tableLayoutPanel5.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.coursesGroupBox.ResumeLayout(false);
            this.coursesGroupBox.PerformLayout();
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel4.PerformLayout();
            this.folderGroupBox.ResumeLayout(false);
            this.folderGroupBox.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel7.ResumeLayout(false);
            this.tableLayoutPanel7.PerformLayout();
            this.outputGroupBox.ResumeLayout(false);
            this.outputGroupBox.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel6.ResumeLayout(false);
            this.tableLayoutPanel6.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel5;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.GroupBox coursesGroupBox;
        private CourseSelector courseSelector;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.GroupBox folderGroupBox;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.RadioButton coursesDirectory;
        private System.Windows.Forms.RadioButton mapDirectory;
        private System.Windows.Forms.RadioButton otherDirectory;
        private System.Windows.Forms.TextBox otherDirectoryTextBox;
        private System.Windows.Forms.GroupBox outputGroupBox;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TextBox filenamePrefixTextBox;
        private System.Windows.Forms.Label fileFormatLabel;
        private System.Windows.Forms.ComboBox fileFormatCombo;
        private System.Windows.Forms.Label fileNamePrefixLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBoxDpi;
        private System.Windows.Forms.Label dpiLabel;
        private System.Windows.Forms.ComboBox comboBoxWorldFile;
        private System.Windows.Forms.Label labelColorModel;
        private System.Windows.Forms.ComboBox comboBoxColorModel;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel6;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel7;
        private System.Windows.Forms.Button selectOtherDirectoryButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button createButton;
    }
}
