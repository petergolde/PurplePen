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
    partial class NewEventNumbering
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewEventNumbering));
            this.newEventNumberingLabel = new System.Windows.Forms.Label();
            this.disallowInvertibleCheckBox = new System.Windows.Forms.CheckBox();
            this.startingCodeLabel = new System.Windows.Forms.Label();
            this.startingCodeNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.changeLaterLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize) (this.startingCodeNumericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // newEventNumberingLabel
            // 
            resources.ApplyResources(this.newEventNumberingLabel, "newEventNumberingLabel");
            this.newEventNumberingLabel.Name = "newEventNumberingLabel";
            // 
            // disallowInvertibleCheckBox
            // 
            resources.ApplyResources(this.disallowInvertibleCheckBox, "disallowInvertibleCheckBox");
            this.disallowInvertibleCheckBox.Name = "disallowInvertibleCheckBox";
            this.disallowInvertibleCheckBox.UseVisualStyleBackColor = true;
            // 
            // startingCodeLabel
            // 
            resources.ApplyResources(this.startingCodeLabel, "startingCodeLabel");
            this.startingCodeLabel.Name = "startingCodeLabel";
            // 
            // startingCodeNumericUpDown
            // 
            resources.ApplyResources(this.startingCodeNumericUpDown, "startingCodeNumericUpDown");
            this.startingCodeNumericUpDown.Maximum = new decimal(new int[] {
            999,
            0,
            0,
            0});
            this.startingCodeNumericUpDown.Minimum = new decimal(new int[] {
            31,
            0,
            0,
            0});
            this.startingCodeNumericUpDown.Name = "startingCodeNumericUpDown";
            this.startingCodeNumericUpDown.Value = new decimal(new int[] {
            31,
            0,
            0,
            0});
            // 
            // changeLaterLabel
            // 
            resources.ApplyResources(this.changeLaterLabel, "changeLaterLabel");
            this.changeLaterLabel.Name = "changeLaterLabel";
            // 
            // NewEventNumbering
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.changeLaterLabel);
            this.Controls.Add(this.newEventNumberingLabel);
            this.Controls.Add(this.disallowInvertibleCheckBox);
            this.Controls.Add(this.startingCodeLabel);
            this.Controls.Add(this.startingCodeNumericUpDown);
            this.Name = "NewEventNumbering";
            ((System.ComponentModel.ISupportInitialize) (this.startingCodeNumericUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label newEventNumberingLabel;
        private System.Windows.Forms.Label startingCodeLabel;
        private System.Windows.Forms.Label changeLaterLabel;
        public System.Windows.Forms.CheckBox disallowInvertibleCheckBox;
        public System.Windows.Forms.NumericUpDown startingCodeNumericUpDown;
    }
}
