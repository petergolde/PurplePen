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
    partial class NewEventFinal
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewEventFinal));
            this.newEventFinalLabel = new System.Windows.Forms.Label();
            this.afterEventCreatedLabel = new System.Windows.Forms.Label();
            this.eventFileName = new System.Windows.Forms.TextBox();
            this.warningIconPictureBox = new System.Windows.Forms.PictureBox();
            this.errorMessage = new System.Windows.Forms.Label();
            this.errorDisplayPanel = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize) (this.warningIconPictureBox)).BeginInit();
            this.errorDisplayPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // newEventFinalLabel
            // 
            resources.ApplyResources(this.newEventFinalLabel, "newEventFinalLabel");
            this.newEventFinalLabel.Name = "newEventFinalLabel";
            // 
            // afterEventCreatedLabel
            // 
            resources.ApplyResources(this.afterEventCreatedLabel, "afterEventCreatedLabel");
            this.afterEventCreatedLabel.Name = "afterEventCreatedLabel";
            // 
            // eventFileName
            // 
            resources.ApplyResources(this.eventFileName, "eventFileName");
            this.eventFileName.Name = "eventFileName";
            this.eventFileName.ReadOnly = true;
            // 
            // warningIconPictureBox
            // 
            resources.ApplyResources(this.warningIconPictureBox, "warningIconPictureBox");
            this.warningIconPictureBox.Name = "warningIconPictureBox";
            this.warningIconPictureBox.TabStop = false;
            // 
            // errorMessage
            // 
            resources.ApplyResources(this.errorMessage, "errorMessage");
            this.errorMessage.ForeColor = System.Drawing.Color.Red;
            this.errorMessage.Name = "errorMessage";
            // 
            // errorDisplayPanel
            // 
            this.errorDisplayPanel.Controls.Add(this.errorMessage);
            this.errorDisplayPanel.Controls.Add(this.warningIconPictureBox);
            resources.ApplyResources(this.errorDisplayPanel, "errorDisplayPanel");
            this.errorDisplayPanel.Name = "errorDisplayPanel";
            // 
            // NewEventFinal
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.errorDisplayPanel);
            this.Controls.Add(this.eventFileName);
            this.Controls.Add(this.afterEventCreatedLabel);
            this.Controls.Add(this.newEventFinalLabel);
            this.Name = "NewEventFinal";
            ((System.ComponentModel.ISupportInitialize) (this.warningIconPictureBox)).EndInit();
            this.errorDisplayPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label newEventFinalLabel;
        private System.Windows.Forms.Label afterEventCreatedLabel;
        public System.Windows.Forms.TextBox eventFileName;
        private System.Windows.Forms.PictureBox warningIconPictureBox;
        public System.Windows.Forms.Label errorMessage;
        public System.Windows.Forms.Panel errorDisplayPanel;
    }
}
