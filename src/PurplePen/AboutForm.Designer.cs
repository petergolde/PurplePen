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
    partial class AboutForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            this.logoPanel = new System.Windows.Forms.Panel();
            this.copyrightLabel = new System.Windows.Forms.Label();
            this.freeLabel = new System.Windows.Forms.Label();
            this.disclaimerLabel = new System.Windows.Forms.Label();
            this.versionLabel = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.okButton = new System.Windows.Forms.Button();
            this.creditsButton = new System.Windows.Forms.Button();
            this.licenseButton = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // logoPanel
            // 
            this.logoPanel.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.logoPanel, "logoPanel");
            this.logoPanel.Name = "logoPanel";
            this.logoPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.logoPanel_Paint);
            // 
            // copyrightLabel
            // 
            resources.ApplyResources(this.copyrightLabel, "copyrightLabel");
            this.copyrightLabel.Name = "copyrightLabel";
            this.copyrightLabel.Click += new System.EventHandler(this.copyrightLabel_Click);
            // 
            // freeLabel
            // 
            resources.ApplyResources(this.freeLabel, "freeLabel");
            this.freeLabel.Name = "freeLabel";
            // 
            // disclaimerLabel
            // 
            resources.ApplyResources(this.disclaimerLabel, "disclaimerLabel");
            this.disclaimerLabel.Name = "disclaimerLabel";
            // 
            // versionLabel
            // 
            resources.ApplyResources(this.versionLabel, "versionLabel");
            this.versionLabel.Name = "versionLabel";
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.okButton, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.creditsButton, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.licenseButton, 0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Name = "okButton";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // creditsButton
            // 
            resources.ApplyResources(this.creditsButton, "creditsButton");
            this.creditsButton.Name = "creditsButton";
            this.creditsButton.UseVisualStyleBackColor = true;
            this.creditsButton.Click += new System.EventHandler(this.creditsButton_Click);
            // 
            // licenseButton
            // 
            resources.ApplyResources(this.licenseButton, "licenseButton");
            this.licenseButton.Name = "licenseButton";
            this.licenseButton.UseVisualStyleBackColor = true;
            this.licenseButton.Click += new System.EventHandler(this.licenseButton_Click);
            // 
            // AboutForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.versionLabel);
            this.Controls.Add(this.disclaimerLabel);
            this.Controls.Add(this.freeLabel);
            this.Controls.Add(this.copyrightLabel);
            this.Controls.Add(this.logoPanel);
            this.HelpTopic = "HelpAbout.htm";
            this.Name = "AboutForm";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel logoPanel;
        private System.Windows.Forms.Label copyrightLabel;
        private System.Windows.Forms.Label freeLabel;
        private System.Windows.Forms.Label disclaimerLabel;
        private System.Windows.Forms.Label versionLabel;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button creditsButton;
        private System.Windows.Forms.Button licenseButton;
    }
}
