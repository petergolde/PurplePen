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
    partial class CreateRouteGadgetFiles
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateRouteGadgetFiles));
            this.createButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.folderGroupBox = new System.Windows.Forms.GroupBox();
            this.otherDirectoryTextBox = new System.Windows.Forms.TextBox();
            this.selectOtherDirectoryButton = new System.Windows.Forms.Button();
            this.otherDirectory = new System.Windows.Forms.RadioButton();
            this.mapDirectory = new System.Windows.Forms.RadioButton();
            this.coursesDirectory = new System.Windows.Forms.RadioButton();
            this.nameGroupBox = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.fileNameTextBox = new System.Windows.Forms.TextBox();
            this.learnMoreLink = new System.Windows.Forms.LinkLabel();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.comboBoxIofXml = new System.Windows.Forms.ComboBox();
            this.labelIofXml = new System.Windows.Forms.Label();
            this.folderGroupBox.SuspendLayout();
            this.nameGroupBox.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
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
            // nameGroupBox
            // 
            this.nameGroupBox.Controls.Add(this.label1);
            this.nameGroupBox.Controls.Add(this.fileNameTextBox);
            resources.ApplyResources(this.nameGroupBox, "nameGroupBox");
            this.nameGroupBox.Name = "nameGroupBox";
            this.nameGroupBox.TabStop = false;
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // fileNameTextBox
            // 
            resources.ApplyResources(this.fileNameTextBox, "fileNameTextBox");
            this.fileNameTextBox.Name = "fileNameTextBox";
            // 
            // learnMoreLink
            // 
            resources.ApplyResources(this.learnMoreLink, "learnMoreLink");
            this.learnMoreLink.Name = "learnMoreLink";
            this.learnMoreLink.TabStop = true;
            this.learnMoreLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.learnMoreLink_LinkClicked);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.comboBoxIofXml);
            this.groupBox1.Controls.Add(this.labelIofXml);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // comboBoxIofXml
            // 
            this.comboBoxIofXml.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxIofXml.FormattingEnabled = true;
            this.comboBoxIofXml.Items.AddRange(new object[] {
            resources.GetString("comboBoxIofXml.Items"),
            resources.GetString("comboBoxIofXml.Items1")});
            resources.ApplyResources(this.comboBoxIofXml, "comboBoxIofXml");
            this.comboBoxIofXml.Name = "comboBoxIofXml";
            // 
            // labelIofXml
            // 
            resources.ApplyResources(this.labelIofXml, "labelIofXml");
            this.labelIofXml.Name = "labelIofXml";
            // 
            // CreateRouteGadgetFiles
            // 
            this.AcceptButton = this.createButton;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.cancelButton;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.learnMoreLink);
            this.Controls.Add(this.nameGroupBox);
            this.Controls.Add(this.folderGroupBox);
            this.Controls.Add(this.createButton);
            this.Controls.Add(this.cancelButton);
            this.HelpTopic = "FileCreateRouteGadget.htm";
            this.Name = "CreateRouteGadgetFiles";
            this.folderGroupBox.ResumeLayout(false);
            this.folderGroupBox.PerformLayout();
            this.nameGroupBox.ResumeLayout(false);
            this.nameGroupBox.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button createButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.GroupBox folderGroupBox;
        private System.Windows.Forms.RadioButton otherDirectory;
        private System.Windows.Forms.RadioButton mapDirectory;
        private System.Windows.Forms.RadioButton coursesDirectory;
        private System.Windows.Forms.Button selectOtherDirectoryButton;
        private System.Windows.Forms.TextBox otherDirectoryTextBox;
        private System.Windows.Forms.GroupBox nameGroupBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox fileNameTextBox;
        private System.Windows.Forms.LinkLabel learnMoreLink;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox comboBoxIofXml;
        private System.Windows.Forms.Label labelIofXml;
    }
}
