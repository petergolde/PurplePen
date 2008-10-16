/* Copyright (c) 2006-2007, Peter Golde
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
    partial class AddCourse
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddCourse));
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.courseNameLabel = new System.Windows.Forms.Label();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.courseKindCombo = new System.Windows.Forms.ComboBox();
            this.courseTypeLabel = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.oneToPrefixLabel = new System.Windows.Forms.Label();
            this.descriptionAppearanceLabel = new System.Windows.Forms.Label();
            this.mapScaleLabel = new System.Windows.Forms.Label();
            this.descKindCombo = new System.Windows.Forms.ComboBox();
            this.scaleCombo = new System.Windows.Forms.ComboBox();
            this.secondaryTitleGroup = new System.Windows.Forms.GroupBox();
            this.secondaryTitleTextBox = new System.Windows.Forms.TextBox();
            this.secondaryTitleDescription = new System.Windows.Forms.Label();
            this.climbLabel = new System.Windows.Forms.Label();
            this.climbTextBox = new System.Windows.Forms.TextBox();
            this.metersSuffix = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.secondaryTitleGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.Name = "okButton";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // courseNameLabel
            // 
            resources.ApplyResources(this.courseNameLabel, "courseNameLabel");
            this.courseNameLabel.Name = "courseNameLabel";
            // 
            // nameTextBox
            // 
            resources.ApplyResources(this.nameTextBox, "nameTextBox");
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.TextChanged += new System.EventHandler(this.nameTextBox_TextChanged);
            // 
            // courseKindCombo
            // 
            this.courseKindCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.courseKindCombo.FormattingEnabled = true;
            this.courseKindCombo.Items.AddRange(new object[] {
            resources.GetString("courseKindCombo.Items"),
            resources.GetString("courseKindCombo.Items1")});
            resources.ApplyResources(this.courseKindCombo, "courseKindCombo");
            this.courseKindCombo.Name = "courseKindCombo";
            // 
            // courseTypeLabel
            // 
            resources.ApplyResources(this.courseTypeLabel, "courseTypeLabel");
            this.courseTypeLabel.Name = "courseTypeLabel";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.oneToPrefixLabel);
            this.groupBox1.Controls.Add(this.descriptionAppearanceLabel);
            this.groupBox1.Controls.Add(this.mapScaleLabel);
            this.groupBox1.Controls.Add(this.descKindCombo);
            this.groupBox1.Controls.Add(this.scaleCombo);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // oneToPrefixLabel
            // 
            resources.ApplyResources(this.oneToPrefixLabel, "oneToPrefixLabel");
            this.oneToPrefixLabel.Name = "oneToPrefixLabel";
            // 
            // descriptionAppearanceLabel
            // 
            resources.ApplyResources(this.descriptionAppearanceLabel, "descriptionAppearanceLabel");
            this.descriptionAppearanceLabel.Name = "descriptionAppearanceLabel";
            // 
            // mapScaleLabel
            // 
            resources.ApplyResources(this.mapScaleLabel, "mapScaleLabel");
            this.mapScaleLabel.Name = "mapScaleLabel";
            // 
            // descKindCombo
            // 
            this.descKindCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.descKindCombo.FormattingEnabled = true;
            this.descKindCombo.Items.AddRange(new object[] {
            resources.GetString("descKindCombo.Items"),
            resources.GetString("descKindCombo.Items1"),
            resources.GetString("descKindCombo.Items2")});
            resources.ApplyResources(this.descKindCombo, "descKindCombo");
            this.descKindCombo.Name = "descKindCombo";
            // 
            // scaleCombo
            // 
            this.scaleCombo.FormattingEnabled = true;
            resources.ApplyResources(this.scaleCombo, "scaleCombo");
            this.scaleCombo.Name = "scaleCombo";
            // 
            // secondaryTitleGroup
            // 
            this.secondaryTitleGroup.Controls.Add(this.secondaryTitleTextBox);
            this.secondaryTitleGroup.Controls.Add(this.secondaryTitleDescription);
            resources.ApplyResources(this.secondaryTitleGroup, "secondaryTitleGroup");
            this.secondaryTitleGroup.Name = "secondaryTitleGroup";
            this.secondaryTitleGroup.TabStop = false;
            // 
            // secondaryTitleTextBox
            // 
            resources.ApplyResources(this.secondaryTitleTextBox, "secondaryTitleTextBox");
            this.secondaryTitleTextBox.Name = "secondaryTitleTextBox";
            // 
            // secondaryTitleDescription
            // 
            resources.ApplyResources(this.secondaryTitleDescription, "secondaryTitleDescription");
            this.secondaryTitleDescription.Name = "secondaryTitleDescription";
            // 
            // climbLabel
            // 
            resources.ApplyResources(this.climbLabel, "climbLabel");
            this.climbLabel.Name = "climbLabel";
            // 
            // climbTextBox
            // 
            resources.ApplyResources(this.climbTextBox, "climbTextBox");
            this.climbTextBox.Name = "climbTextBox";
            // 
            // metersSuffix
            // 
            resources.ApplyResources(this.metersSuffix, "metersSuffix");
            this.metersSuffix.Name = "metersSuffix";
            // 
            // AddCourse
            // 
            this.AcceptButton = this.okButton;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.cancelButton;
            this.Controls.Add(this.metersSuffix);
            this.Controls.Add(this.climbTextBox);
            this.Controls.Add(this.climbLabel);
            this.Controls.Add(this.secondaryTitleGroup);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.courseTypeLabel);
            this.Controls.Add(this.courseKindCombo);
            this.Controls.Add(this.nameTextBox);
            this.Controls.Add(this.courseNameLabel);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.HelpButton = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddCourse";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.AddCourse_HelpButtonClicked);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.secondaryTitleGroup.ResumeLayout(false);
            this.secondaryTitleGroup.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label courseNameLabel;
        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.ComboBox courseKindCombo;
        private System.Windows.Forms.Label courseTypeLabel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox secondaryTitleGroup;
        private System.Windows.Forms.Label secondaryTitleDescription;
        private System.Windows.Forms.Label oneToPrefixLabel;
        private System.Windows.Forms.Label descriptionAppearanceLabel;
        private System.Windows.Forms.Label mapScaleLabel;
        private System.Windows.Forms.ComboBox descKindCombo;
        private System.Windows.Forms.ComboBox scaleCombo;
        private System.Windows.Forms.TextBox secondaryTitleTextBox;
        private System.Windows.Forms.Label climbLabel;
        private System.Windows.Forms.TextBox climbTextBox;
        private System.Windows.Forms.Label metersSuffix;
    }
}
