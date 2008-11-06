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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class AutoNumbering: Form
    {
        public int FirstCode {
            get {
                return (int) startingCodeNumericUpDown.Value;
            }
            set {
                startingCodeNumericUpDown.Value = value;
            }
        }

        public bool DisallowInvertibleCodes
        {
            get
            {
                return this.disallowInvertibleCheckBox.Checked;
            }
            set
            {
                this.disallowInvertibleCheckBox.Checked = value;
            }
        }

        public bool RenumberExisting {
            get
            {
                return renumberExistingRadioButton.Checked;
            }
            set
            {
                renumberExistingRadioButton.Checked = value;
                newControlsOnlyRadioButton.Checked = !value;
            }
        }

        public AutoNumbering()
        {
            InitializeComponent();
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AutoNumbering));
            this.startingCodeNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.startingCodeLabel = new System.Windows.Forms.Label();
            this.disallowInvertibleCheckBox = new System.Windows.Forms.CheckBox();
            this.existingControlsGroupBox = new System.Windows.Forms.GroupBox();
            this.renumberExistingRadioButton = new System.Windows.Forms.RadioButton();
            this.newControlsOnlyRadioButton = new System.Windows.Forms.RadioButton();
            this.automaticNumberingLabel = new System.Windows.Forms.Label();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize) (this.startingCodeNumericUpDown)).BeginInit();
            this.existingControlsGroupBox.SuspendLayout();
            this.SuspendLayout();
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
            // startingCodeLabel
            // 
            resources.ApplyResources(this.startingCodeLabel, "startingCodeLabel");
            this.startingCodeLabel.Name = "startingCodeLabel";
            // 
            // disallowInvertibleCheckBox
            // 
            resources.ApplyResources(this.disallowInvertibleCheckBox, "disallowInvertibleCheckBox");
            this.disallowInvertibleCheckBox.Name = "disallowInvertibleCheckBox";
            this.disallowInvertibleCheckBox.UseVisualStyleBackColor = true;
            // 
            // existingControlsGroupBox
            // 
            this.existingControlsGroupBox.Controls.Add(this.renumberExistingRadioButton);
            this.existingControlsGroupBox.Controls.Add(this.newControlsOnlyRadioButton);
            resources.ApplyResources(this.existingControlsGroupBox, "existingControlsGroupBox");
            this.existingControlsGroupBox.Name = "existingControlsGroupBox";
            this.existingControlsGroupBox.TabStop = false;
            // 
            // renumberExistingRadioButton
            // 
            resources.ApplyResources(this.renumberExistingRadioButton, "renumberExistingRadioButton");
            this.renumberExistingRadioButton.Name = "renumberExistingRadioButton";
            this.renumberExistingRadioButton.TabStop = true;
            this.renumberExistingRadioButton.UseVisualStyleBackColor = true;
            // 
            // newControlsOnlyRadioButton
            // 
            resources.ApplyResources(this.newControlsOnlyRadioButton, "newControlsOnlyRadioButton");
            this.newControlsOnlyRadioButton.Name = "newControlsOnlyRadioButton";
            this.newControlsOnlyRadioButton.TabStop = true;
            this.newControlsOnlyRadioButton.UseVisualStyleBackColor = true;
            // 
            // automaticNumberingLabel
            // 
            resources.ApplyResources(this.automaticNumberingLabel, "automaticNumberingLabel");
            this.automaticNumberingLabel.Name = "automaticNumberingLabel";
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
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.Name = "okButton";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // AutoNumbering
            // 
            this.AcceptButton = this.okButton;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.cancelButton;
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.automaticNumberingLabel);
            this.Controls.Add(this.existingControlsGroupBox);
            this.Controls.Add(this.disallowInvertibleCheckBox);
            this.Controls.Add(this.startingCodeLabel);
            this.Controls.Add(this.startingCodeNumericUpDown);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.HelpButton = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AutoNumbering";
            this.ShowInTaskbar = false;
            this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.AutoNumbering_HelpButtonClicked);
            ((System.ComponentModel.ISupportInitialize) (this.startingCodeNumericUpDown)).EndInit();
            this.existingControlsGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private NumericUpDown startingCodeNumericUpDown;
        private Label startingCodeLabel;
        private CheckBox disallowInvertibleCheckBox;
        private GroupBox existingControlsGroupBox;
        private RadioButton renumberExistingRadioButton;
        private RadioButton newControlsOnlyRadioButton;
        private Label automaticNumberingLabel;
        private Button cancelButton;
        private Button okButton;       

        #endregion

        private void AutoNumbering_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            Util.ShowHelpTopic(this, "ControlsAutomaticNumbering.htm");
            e.Cancel = true;
        }
    }
}
