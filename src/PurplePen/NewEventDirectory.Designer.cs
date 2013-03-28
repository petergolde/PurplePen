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
    partial class NewEventDirectory
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewEventDirectory));
            this.newEventDirectoryLabel = new System.Windows.Forms.Label();
            this.useMapDirectory = new System.Windows.Forms.RadioButton();
            this.useOtherFolder = new System.Windows.Forms.RadioButton();
            this.chooseFolder = new System.Windows.Forms.Button();
            this.folderName = new System.Windows.Forms.Label();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.directoryDisplay = new System.Windows.Forms.GroupBox();
            this.directoryName = new System.Windows.Forms.TextBox();
            this.labelTitle = new System.Windows.Forms.Label();
            this.directoryDisplay.SuspendLayout();
            this.SuspendLayout();
            // 
            // newEventDirectoryLabel
            // 
            resources.ApplyResources(this.newEventDirectoryLabel, "newEventDirectoryLabel");
            this.newEventDirectoryLabel.Name = "newEventDirectoryLabel";
            // 
            // useMapDirectory
            // 
            resources.ApplyResources(this.useMapDirectory, "useMapDirectory");
            this.useMapDirectory.Name = "useMapDirectory";
            this.useMapDirectory.TabStop = true;
            this.useMapDirectory.UseVisualStyleBackColor = true;
            // 
            // useOtherFolder
            // 
            resources.ApplyResources(this.useOtherFolder, "useOtherFolder");
            this.useOtherFolder.Name = "useOtherFolder";
            this.useOtherFolder.TabStop = true;
            this.useOtherFolder.UseVisualStyleBackColor = true;
            this.useOtherFolder.CheckedChanged += new System.EventHandler(this.useOtherFolder_CheckedChanged);
            // 
            // chooseFolder
            // 
            resources.ApplyResources(this.chooseFolder, "chooseFolder");
            this.chooseFolder.Name = "chooseFolder";
            this.chooseFolder.UseVisualStyleBackColor = true;
            this.chooseFolder.Click += new System.EventHandler(this.chooseFolder_Click);
            // 
            // folderName
            // 
            resources.ApplyResources(this.folderName, "folderName");
            this.folderName.Name = "folderName";
            // 
            // folderBrowserDialog
            // 
            resources.ApplyResources(this.folderBrowserDialog, "folderBrowserDialog");
            // 
            // directoryDisplay
            // 
            this.directoryDisplay.Controls.Add(this.directoryName);
            resources.ApplyResources(this.directoryDisplay, "directoryDisplay");
            this.directoryDisplay.Name = "directoryDisplay";
            this.directoryDisplay.TabStop = false;
            // 
            // directoryName
            // 
            resources.ApplyResources(this.directoryName, "directoryName");
            this.directoryName.Name = "directoryName";
            this.directoryName.ReadOnly = true;
            // 
            // labelTitle
            // 
            resources.ApplyResources(this.labelTitle, "labelTitle");
            this.labelTitle.Name = "labelTitle";
            // 
            // NewEventDirectory
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.directoryDisplay);
            this.Controls.Add(this.folderName);
            this.Controls.Add(this.chooseFolder);
            this.Controls.Add(this.useOtherFolder);
            this.Controls.Add(this.useMapDirectory);
            this.Controls.Add(this.newEventDirectoryLabel);
            this.Name = "NewEventDirectory";
            this.directoryDisplay.ResumeLayout(false);
            this.directoryDisplay.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label newEventDirectoryLabel;
        private System.Windows.Forms.RadioButton useMapDirectory;
        private System.Windows.Forms.RadioButton useOtherFolder;
        private System.Windows.Forms.Button chooseFolder;
        private System.Windows.Forms.Label folderName;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.GroupBox directoryDisplay;
        private System.Windows.Forms.TextBox directoryName;
        private System.Windows.Forms.Label labelTitle;
    }
}
