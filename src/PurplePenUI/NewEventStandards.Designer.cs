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
    partial class NewEventStandards
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewEventStandards));
            this.changeLaterLabel = new System.Windows.Forms.Label();
            this.labelTitle = new System.Windows.Forms.Label();
            this.groupBoxMapStandard = new System.Windows.Forms.GroupBox();
            this.radioButtonMap2017 = new System.Windows.Forms.RadioButton();
            this.radioButtonMap2000 = new System.Windows.Forms.RadioButton();
            this.groupBoxDescriptionStandard = new System.Windows.Forms.GroupBox();
            this.radioButtonDescriptions2018 = new System.Windows.Forms.RadioButton();
            this.radioButtonDescriptions2004 = new System.Windows.Forms.RadioButton();
            this.labelStandardsIntro = new System.Windows.Forms.Label();
            this.groupBoxMapStandard.SuspendLayout();
            this.groupBoxDescriptionStandard.SuspendLayout();
            this.SuspendLayout();
            // 
            // changeLaterLabel
            // 
            resources.ApplyResources(this.changeLaterLabel, "changeLaterLabel");
            this.changeLaterLabel.Name = "changeLaterLabel";
            // 
            // labelTitle
            // 
            resources.ApplyResources(this.labelTitle, "labelTitle");
            this.labelTitle.Name = "labelTitle";
            // 
            // groupBoxMapStandard
            // 
            this.groupBoxMapStandard.Controls.Add(this.radioButtonMap2017);
            this.groupBoxMapStandard.Controls.Add(this.radioButtonMap2000);
            resources.ApplyResources(this.groupBoxMapStandard, "groupBoxMapStandard");
            this.groupBoxMapStandard.Name = "groupBoxMapStandard";
            this.groupBoxMapStandard.TabStop = false;
            // 
            // radioButtonMap2017
            // 
            resources.ApplyResources(this.radioButtonMap2017, "radioButtonMap2017");
            this.radioButtonMap2017.Name = "radioButtonMap2017";
            this.radioButtonMap2017.TabStop = true;
            this.radioButtonMap2017.UseVisualStyleBackColor = true;
            // 
            // radioButtonMap2000
            // 
            resources.ApplyResources(this.radioButtonMap2000, "radioButtonMap2000");
            this.radioButtonMap2000.Name = "radioButtonMap2000";
            this.radioButtonMap2000.TabStop = true;
            this.radioButtonMap2000.UseVisualStyleBackColor = true;
            // 
            // groupBoxDescriptionStandard
            // 
            this.groupBoxDescriptionStandard.Controls.Add(this.radioButtonDescriptions2018);
            this.groupBoxDescriptionStandard.Controls.Add(this.radioButtonDescriptions2004);
            resources.ApplyResources(this.groupBoxDescriptionStandard, "groupBoxDescriptionStandard");
            this.groupBoxDescriptionStandard.Name = "groupBoxDescriptionStandard";
            this.groupBoxDescriptionStandard.TabStop = false;
            // 
            // radioButtonDescriptions2018
            // 
            resources.ApplyResources(this.radioButtonDescriptions2018, "radioButtonDescriptions2018");
            this.radioButtonDescriptions2018.Name = "radioButtonDescriptions2018";
            this.radioButtonDescriptions2018.TabStop = true;
            this.radioButtonDescriptions2018.UseVisualStyleBackColor = true;
            // 
            // radioButtonDescriptions2004
            // 
            resources.ApplyResources(this.radioButtonDescriptions2004, "radioButtonDescriptions2004");
            this.radioButtonDescriptions2004.Name = "radioButtonDescriptions2004";
            this.radioButtonDescriptions2004.TabStop = true;
            this.radioButtonDescriptions2004.UseVisualStyleBackColor = true;
            // 
            // labelStandardsIntro
            // 
            resources.ApplyResources(this.labelStandardsIntro, "labelStandardsIntro");
            this.labelStandardsIntro.Name = "labelStandardsIntro";
            // 
            // NewEventStandards
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.labelStandardsIntro);
            this.Controls.Add(this.groupBoxDescriptionStandard);
            this.Controls.Add(this.groupBoxMapStandard);
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.changeLaterLabel);
            this.Name = "NewEventStandards";
            this.groupBoxMapStandard.ResumeLayout(false);
            this.groupBoxMapStandard.PerformLayout();
            this.groupBoxDescriptionStandard.ResumeLayout(false);
            this.groupBoxDescriptionStandard.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label changeLaterLabel;
        private System.Windows.Forms.Label labelTitle;
        private System.Windows.Forms.GroupBox groupBoxMapStandard;
        private System.Windows.Forms.GroupBox groupBoxDescriptionStandard;
        private System.Windows.Forms.Label labelStandardsIntro;
        internal System.Windows.Forms.RadioButton radioButtonMap2017;
        internal System.Windows.Forms.RadioButton radioButtonMap2000;
        internal System.Windows.Forms.RadioButton radioButtonDescriptions2018;
        internal System.Windows.Forms.RadioButton radioButtonDescriptions2004;
    }
}
