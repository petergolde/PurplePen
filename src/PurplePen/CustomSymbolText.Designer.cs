/* Copyright (c) 2007, Peter Golde
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
    partial class CustomSymbolText
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CustomSymbolText));
            this.comboBoxLanguage = new System.Windows.Forms.ComboBox();
            this.labelChooseLanguage = new System.Windows.Forms.Label();
            this.textBoxSymbolName = new System.Windows.Forms.TextBox();
            this.labelSymbolName = new System.Windows.Forms.Label();
            this.labelCustomizedText = new System.Windows.Forms.Label();
            this.buttonChangeText = new System.Windows.Forms.Button();
            this.buttonDefault = new System.Windows.Forms.Button();
            this.textBoxCurrent = new System.Windows.Forms.TextBox();
            this.labelStandardText = new System.Windows.Forms.Label();
            this.checkBoxShowKey = new System.Windows.Forms.CheckBox();
            this.listBoxSymbols = new System.Windows.Forms.ListBox();
            this.labelCustomizeText = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            // 
            // cancelButton
            // 
            resources.ApplyResources(this.cancelButton, "cancelButton");
            // 
            // comboBoxLanguage
            // 
            this.comboBoxLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLanguage.FormattingEnabled = true;
            resources.ApplyResources(this.comboBoxLanguage, "comboBoxLanguage");
            this.comboBoxLanguage.Name = "comboBoxLanguage";
            this.comboBoxLanguage.SelectedIndexChanged += new System.EventHandler(this.comboBoxLanguage_SelectedIndexChanged);
            // 
            // labelChooseLanguage
            // 
            resources.ApplyResources(this.labelChooseLanguage, "labelChooseLanguage");
            this.labelChooseLanguage.Name = "labelChooseLanguage";
            // 
            // textBoxSymbolName
            // 
            resources.ApplyResources(this.textBoxSymbolName, "textBoxSymbolName");
            this.textBoxSymbolName.Name = "textBoxSymbolName";
            // 
            // labelSymbolName
            // 
            resources.ApplyResources(this.labelSymbolName, "labelSymbolName");
            this.labelSymbolName.Name = "labelSymbolName";
            // 
            // labelCustomizedText
            // 
            resources.ApplyResources(this.labelCustomizedText, "labelCustomizedText");
            this.labelCustomizedText.Name = "labelCustomizedText";
            // 
            // buttonChangeText
            // 
            resources.ApplyResources(this.buttonChangeText, "buttonChangeText");
            this.buttonChangeText.Name = "buttonChangeText";
            this.buttonChangeText.UseVisualStyleBackColor = true;
            this.buttonChangeText.Click += new System.EventHandler(this.buttonChangeText_Click);
            // 
            // buttonDefault
            // 
            resources.ApplyResources(this.buttonDefault, "buttonDefault");
            this.buttonDefault.Name = "buttonDefault";
            this.buttonDefault.UseVisualStyleBackColor = true;
            this.buttonDefault.Click += new System.EventHandler(this.buttonDefault_Click);
            // 
            // textBoxCurrent
            // 
            resources.ApplyResources(this.textBoxCurrent, "textBoxCurrent");
            this.textBoxCurrent.Name = "textBoxCurrent";
            this.textBoxCurrent.ReadOnly = true;
            // 
            // labelStandardText
            // 
            resources.ApplyResources(this.labelStandardText, "labelStandardText");
            this.labelStandardText.Name = "labelStandardText";
            // 
            // checkBoxShowKey
            // 
            resources.ApplyResources(this.checkBoxShowKey, "checkBoxShowKey");
            this.checkBoxShowKey.Name = "checkBoxShowKey";
            this.checkBoxShowKey.UseVisualStyleBackColor = true;
            // 
            // listBoxSymbols
            // 
            resources.ApplyResources(this.listBoxSymbols, "listBoxSymbols");
            this.listBoxSymbols.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.listBoxSymbols.FormattingEnabled = true;
            this.listBoxSymbols.Name = "listBoxSymbols";
            this.listBoxSymbols.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.listBoxSymbols_DrawItem);
            this.listBoxSymbols.SelectedIndexChanged += new System.EventHandler(this.listBoxSymbols_SelectedIndexChanged);
            // 
            // labelCustomizeText
            // 
            resources.ApplyResources(this.labelCustomizeText, "labelCustomizeText");
            this.labelCustomizeText.Name = "labelCustomizeText";
            // 
            // CustomSymbolText
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.textBoxCurrent);
            this.Controls.Add(this.labelCustomizedText);
            this.Controls.Add(this.textBoxSymbolName);
            this.Controls.Add(this.labelSymbolName);
            this.Controls.Add(this.buttonChangeText);
            this.Controls.Add(this.checkBoxShowKey);
            this.Controls.Add(this.listBoxSymbols);
            this.Controls.Add(this.labelStandardText);
            this.Controls.Add(this.labelCustomizeText);
            this.Controls.Add(this.comboBoxLanguage);
            this.Controls.Add(this.buttonDefault);
            this.Controls.Add(this.labelChooseLanguage);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.HelpTopic = "ControlsCustomizeDescriptionText.htm";
            this.Name = "CustomSymbolText";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Controls.SetChildIndex(this.labelChooseLanguage, 0);
            this.Controls.SetChildIndex(this.buttonDefault, 0);
            this.Controls.SetChildIndex(this.comboBoxLanguage, 0);
            this.Controls.SetChildIndex(this.labelCustomizeText, 0);
            this.Controls.SetChildIndex(this.labelStandardText, 0);
            this.Controls.SetChildIndex(this.listBoxSymbols, 0);
            this.Controls.SetChildIndex(this.checkBoxShowKey, 0);
            this.Controls.SetChildIndex(this.buttonChangeText, 0);
            this.Controls.SetChildIndex(this.labelSymbolName, 0);
            this.Controls.SetChildIndex(this.textBoxSymbolName, 0);
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.Controls.SetChildIndex(this.labelCustomizedText, 0);
            this.Controls.SetChildIndex(this.textBoxCurrent, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxLanguage;
        private System.Windows.Forms.Label labelChooseLanguage;
        private System.Windows.Forms.TextBox textBoxSymbolName;
        private System.Windows.Forms.Label labelSymbolName;
        private System.Windows.Forms.Label labelCustomizedText;
        private System.Windows.Forms.Button buttonChangeText;
        private System.Windows.Forms.Button buttonDefault;
        private System.Windows.Forms.TextBox textBoxCurrent;
        private System.Windows.Forms.Label labelStandardText;
        private System.Windows.Forms.CheckBox checkBoxShowKey;
        private System.Windows.Forms.ListBox listBoxSymbols;
        private System.Windows.Forms.Label labelCustomizeText;
    }
}