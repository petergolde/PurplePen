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

using System;
using System.Windows.Forms;

namespace PurplePen.Livelox
{
    partial class PublishToLiveloxDialog
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
            if (disposing && (components != null))
            {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PublishToLiveloxDialog));
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.learnMoreLink = new System.Windows.Forms.LinkLabel();
            this.informationLabel = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.settingsGroupBox = new System.Windows.Forms.GroupBox();
            this.userPanel = new System.Windows.Forms.Panel();
            this.removeUserLink = new System.Windows.Forms.LinkLabel();
            this.userComboBox = new System.Windows.Forms.ComboBox();
            this.userLabel = new System.Windows.Forms.Label();
            this.resolutionTextBox = new System.Windows.Forms.TextBox();
            this.resolutionLabel = new System.Windows.Forms.Label();
            this.showSettingsCheckBox = new System.Windows.Forms.CheckBox();
            this.existingEventGroupBox = new System.Windows.Forms.GroupBox();
            this.eventLinksPanel = new System.Windows.Forms.Panel();
            this.editEventLink = new System.Windows.Forms.LinkLabel();
            this.showEventLink = new System.Windows.Forms.LinkLabel();
            this.eventTimeIntervalLabel = new System.Windows.Forms.Label();
            this.eventOrganisersLabel = new System.Windows.Forms.Label();
            this.eventNameLabel = new System.Windows.Forms.Label();
            this.buttonPanel = new System.Windows.Forms.Panel();
            this.updateEventButton = new System.Windows.Forms.Button();
            this.publishToOtherEventButtonMarginPanel = new System.Windows.Forms.Panel();
            this.publishToOtherEventButton = new System.Windows.Forms.Button();
            this.publishButtonMarginPanel = new System.Windows.Forms.Panel();
            this.publishButton = new System.Windows.Forms.Button();
            this.cancelButtonMarginPanel = new System.Windows.Forms.Panel();
            this.cancelButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.settingsGroupBox.SuspendLayout();
            this.userPanel.SuspendLayout();
            this.existingEventGroupBox.SuspendLayout();
            this.eventLinksPanel.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // learnMoreLink
            // 
            resources.ApplyResources(this.learnMoreLink, "learnMoreLink");
            this.learnMoreLink.Name = "learnMoreLink";
            this.learnMoreLink.TabStop = true;
            this.learnMoreLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.learnMoreLink_LinkClicked);
            // 
            // informationLabel
            // 
            resources.ApplyResources(this.informationLabel, "informationLabel");
            this.informationLabel.Name = "informationLabel";
            // 
            // pictureBox1
            // 
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.Image = global::PurplePen.Properties.Resources.Livelox64x64;
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // settingsGroupBox
            // 
            resources.ApplyResources(this.settingsGroupBox, "settingsGroupBox");
            this.settingsGroupBox.Controls.Add(this.userPanel);
            this.settingsGroupBox.Controls.Add(this.resolutionTextBox);
            this.settingsGroupBox.Controls.Add(this.resolutionLabel);
            this.settingsGroupBox.Name = "settingsGroupBox";
            this.settingsGroupBox.TabStop = false;
            // 
            // userPanel
            // 
            resources.ApplyResources(this.userPanel, "userPanel");
            this.userPanel.Controls.Add(this.removeUserLink);
            this.userPanel.Controls.Add(this.userComboBox);
            this.userPanel.Controls.Add(this.userLabel);
            this.userPanel.Name = "userPanel";
            // 
            // removeUserLink
            // 
            resources.ApplyResources(this.removeUserLink, "removeUserLink");
            this.removeUserLink.Name = "removeUserLink";
            this.removeUserLink.TabStop = true;
            this.removeUserLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.removeUserLink_LinkClicked);
            // 
            // userComboBox
            // 
            resources.ApplyResources(this.userComboBox, "userComboBox");
            this.userComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.userComboBox.FormattingEnabled = true;
            this.userComboBox.Name = "userComboBox";
            this.userComboBox.SelectedIndexChanged += new System.EventHandler(this.userComboBox_SelectedIndexChanged);
            // 
            // userLabel
            // 
            resources.ApplyResources(this.userLabel, "userLabel");
            this.userLabel.Name = "userLabel";
            // 
            // resolutionTextBox
            // 
            resources.ApplyResources(this.resolutionTextBox, "resolutionTextBox");
            this.resolutionTextBox.Name = "resolutionTextBox";
            // 
            // resolutionLabel
            // 
            resources.ApplyResources(this.resolutionLabel, "resolutionLabel");
            this.resolutionLabel.Name = "resolutionLabel";
            // 
            // showSettingsCheckBox
            // 
            resources.ApplyResources(this.showSettingsCheckBox, "showSettingsCheckBox");
            this.showSettingsCheckBox.Name = "showSettingsCheckBox";
            this.showSettingsCheckBox.CheckedChanged += new System.EventHandler(this.showSettingsCheckBox_CheckedChanged);
            // 
            // existingEventGroupBox
            // 
            resources.ApplyResources(this.existingEventGroupBox, "existingEventGroupBox");
            this.existingEventGroupBox.Controls.Add(this.eventLinksPanel);
            this.existingEventGroupBox.Controls.Add(this.eventTimeIntervalLabel);
            this.existingEventGroupBox.Controls.Add(this.eventOrganisersLabel);
            this.existingEventGroupBox.Controls.Add(this.eventNameLabel);
            this.existingEventGroupBox.Name = "existingEventGroupBox";
            this.existingEventGroupBox.TabStop = false;
            // 
            // eventLinksPanel
            // 
            resources.ApplyResources(this.eventLinksPanel, "eventLinksPanel");
            this.eventLinksPanel.Controls.Add(this.editEventLink);
            this.eventLinksPanel.Controls.Add(this.showEventLink);
            this.eventLinksPanel.Name = "eventLinksPanel";
            // 
            // editEventLink
            // 
            resources.ApplyResources(this.editEventLink, "editEventLink");
            this.editEventLink.Name = "editEventLink";
            this.editEventLink.TabStop = true;
            this.editEventLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.editEventLink_LinkClicked);
            // 
            // showEventLink
            // 
            resources.ApplyResources(this.showEventLink, "showEventLink");
            this.showEventLink.Name = "showEventLink";
            this.showEventLink.TabStop = true;
            this.showEventLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.showEventLink_LinkClicked);
            // 
            // eventTimeIntervalLabel
            // 
            resources.ApplyResources(this.eventTimeIntervalLabel, "eventTimeIntervalLabel");
            this.eventTimeIntervalLabel.Name = "eventTimeIntervalLabel";
            // 
            // eventOrganisersLabel
            // 
            resources.ApplyResources(this.eventOrganisersLabel, "eventOrganisersLabel");
            this.eventOrganisersLabel.Name = "eventOrganisersLabel";
            // 
            // eventNameLabel
            // 
            resources.ApplyResources(this.eventNameLabel, "eventNameLabel");
            this.eventNameLabel.Name = "eventNameLabel";
            // 
            // buttonPanel
            // 
            resources.ApplyResources(this.buttonPanel, "buttonPanel");
            this.buttonPanel.Controls.Add(this.updateEventButton);
            this.buttonPanel.Controls.Add(this.publishToOtherEventButtonMarginPanel);
            this.buttonPanel.Controls.Add(this.publishToOtherEventButton);
            this.buttonPanel.Controls.Add(this.publishButtonMarginPanel);
            this.buttonPanel.Controls.Add(this.publishButton);
            this.buttonPanel.Controls.Add(this.cancelButtonMarginPanel);
            this.buttonPanel.Controls.Add(this.cancelButton);
            this.buttonPanel.Name = "buttonPanel";
            // 
            // updateEventButton
            // 
            resources.ApplyResources(this.updateEventButton, "updateEventButton");
            this.updateEventButton.Name = "updateEventButton";
            this.updateEventButton.UseVisualStyleBackColor = true;
            this.updateEventButton.Click += new System.EventHandler(this.updateEventButton_Click);
            // 
            // publishToOtherEventButtonMarginPanel
            // 
            resources.ApplyResources(this.publishToOtherEventButtonMarginPanel, "publishToOtherEventButtonMarginPanel");
            this.publishToOtherEventButtonMarginPanel.Name = "publishToOtherEventButtonMarginPanel";
            // 
            // publishToOtherEventButton
            // 
            resources.ApplyResources(this.publishToOtherEventButton, "publishToOtherEventButton");
            this.publishToOtherEventButton.Name = "publishToOtherEventButton";
            this.publishToOtherEventButton.UseVisualStyleBackColor = true;
            this.publishToOtherEventButton.Click += new System.EventHandler(this.publishToOtherEventButton_Click);
            // 
            // publishButtonMarginPanel
            // 
            resources.ApplyResources(this.publishButtonMarginPanel, "publishButtonMarginPanel");
            this.publishButtonMarginPanel.Name = "publishButtonMarginPanel";
            // 
            // publishButton
            // 
            resources.ApplyResources(this.publishButton, "publishButton");
            this.publishButton.Name = "publishButton";
            this.publishButton.UseVisualStyleBackColor = true;
            this.publishButton.Click += new System.EventHandler(this.publishButton_Click);
            // 
            // cancelButtonMarginPanel
            // 
            resources.ApplyResources(this.cancelButtonMarginPanel, "cancelButtonMarginPanel");
            this.cancelButtonMarginPanel.Name = "cancelButtonMarginPanel";
            // 
            // cancelButton
            // 
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // PublishToLiveloxDialog
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.buttonPanel);
            this.Controls.Add(this.existingEventGroupBox);
            this.Controls.Add(this.showSettingsCheckBox);
            this.Controls.Add(this.settingsGroupBox);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.informationLabel);
            this.Controls.Add(this.learnMoreLink);
            this.HelpButton = false;
            this.HelpTopic = "";
            this.Name = "PublishToLiveloxDialog";
            this.Shown += new System.EventHandler(this.PublishToLiveloxDialog_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.settingsGroupBox.ResumeLayout(false);
            this.settingsGroupBox.PerformLayout();
            this.userPanel.ResumeLayout(false);
            this.userPanel.PerformLayout();
            this.existingEventGroupBox.ResumeLayout(false);
            this.existingEventGroupBox.PerformLayout();
            this.eventLinksPanel.ResumeLayout(false);
            this.eventLinksPanel.PerformLayout();
            this.buttonPanel.ResumeLayout(false);
            this.buttonPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.LinkLabel learnMoreLink;
        private System.Windows.Forms.Label informationLabel;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.GroupBox settingsGroupBox;
        private System.Windows.Forms.TextBox resolutionTextBox;
        private System.Windows.Forms.Label resolutionLabel;
        private System.Windows.Forms.CheckBox showSettingsCheckBox;
        private System.Windows.Forms.GroupBox existingEventGroupBox;
        private System.Windows.Forms.Label eventTimeIntervalLabel;
        private System.Windows.Forms.Label eventOrganisersLabel;
        private System.Windows.Forms.Label eventNameLabel;
        private System.Windows.Forms.Panel userPanel;
        private System.Windows.Forms.LinkLabel removeUserLink;
        private System.Windows.Forms.ComboBox userComboBox;
        private System.Windows.Forms.Label userLabel;
        private Panel eventLinksPanel;
        private LinkLabel showEventLink;
        private Panel buttonPanel;
        private Button updateEventButton;
        private Panel publishToOtherEventButtonMarginPanel;
        private Button publishToOtherEventButton;
        private Panel publishButtonMarginPanel;
        private Button publishButton;
        private Panel cancelButtonMarginPanel;
        private Button cancelButton;
        private LinkLabel editEventLink;
    }
}
