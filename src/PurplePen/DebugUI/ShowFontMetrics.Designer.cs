namespace PurplePen.DebugUI
{
    partial class ShowFontMetrics
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
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
        private void InitializeComponent() {
            this.label1 = new System.Windows.Forms.Label();
            this.fontNameTextBox = new System.Windows.Forms.TextBox();
            this.boldCheckBox = new System.Windows.Forms.CheckBox();
            this.italicCheckBox = new System.Windows.Forms.CheckBox();
            this.outputText = new System.Windows.Forms.TextBox();
            this.showButton = new System.Windows.Forms.Button();
            this.fontSizeTextBox = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.fontSizeTextBox)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(62, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Font Name:";
            // 
            // fontNameTextBox
            // 
            this.fontNameTextBox.Location = new System.Drawing.Point(84, 18);
            this.fontNameTextBox.Name = "fontNameTextBox";
            this.fontNameTextBox.Size = new System.Drawing.Size(188, 20);
            this.fontNameTextBox.TabIndex = 1;
            // 
            // boldCheckBox
            // 
            this.boldCheckBox.AutoSize = true;
            this.boldCheckBox.Location = new System.Drawing.Point(138, 47);
            this.boldCheckBox.Name = "boldCheckBox";
            this.boldCheckBox.Size = new System.Drawing.Size(47, 17);
            this.boldCheckBox.TabIndex = 4;
            this.boldCheckBox.Text = "Bold";
            this.boldCheckBox.UseVisualStyleBackColor = true;
            // 
            // italicCheckBox
            // 
            this.italicCheckBox.AutoSize = true;
            this.italicCheckBox.Location = new System.Drawing.Point(191, 47);
            this.italicCheckBox.Name = "italicCheckBox";
            this.italicCheckBox.Size = new System.Drawing.Size(48, 17);
            this.italicCheckBox.TabIndex = 5;
            this.italicCheckBox.Text = "Italic";
            this.italicCheckBox.UseVisualStyleBackColor = true;
            // 
            // outputText
            // 
            this.outputText.BackColor = System.Drawing.SystemColors.Window;
            this.outputText.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.outputText.Location = new System.Drawing.Point(12, 97);
            this.outputText.Multiline = true;
            this.outputText.Name = "outputText";
            this.outputText.ReadOnly = true;
            this.outputText.Size = new System.Drawing.Size(368, 153);
            this.outputText.TabIndex = 7;
            // 
            // showButton
            // 
            this.showButton.Location = new System.Drawing.Point(278, 21);
            this.showButton.Name = "showButton";
            this.showButton.Size = new System.Drawing.Size(108, 40);
            this.showButton.TabIndex = 6;
            this.showButton.Text = "Show Metrics";
            this.showButton.UseVisualStyleBackColor = true;
            this.showButton.Click += new System.EventHandler(this.showButton_Click);
            // 
            // fontSizeTextBox
            // 
            this.fontSizeTextBox.DecimalPlaces = 1;
            this.fontSizeTextBox.Location = new System.Drawing.Point(52, 46);
            this.fontSizeTextBox.Name = "fontSizeTextBox";
            this.fontSizeTextBox.Size = new System.Drawing.Size(67, 20);
            this.fontSizeTextBox.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 48);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(30, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Size:";
            // 
            // ShowFontMetrics
            // 
            this.AcceptButton = this.showButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(392, 262);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.fontSizeTextBox);
            this.Controls.Add(this.showButton);
            this.Controls.Add(this.outputText);
            this.Controls.Add(this.italicCheckBox);
            this.Controls.Add(this.boldCheckBox);
            this.Controls.Add(this.fontNameTextBox);
            this.Controls.Add(this.label1);
            this.Name = "ShowFontMetrics";
            this.Text = "Show Font Metrics";
            ((System.ComponentModel.ISupportInitialize)(this.fontSizeTextBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox fontNameTextBox;
        private System.Windows.Forms.CheckBox boldCheckBox;
        private System.Windows.Forms.CheckBox italicCheckBox;
        private System.Windows.Forms.TextBox outputText;
        private System.Windows.Forms.Button showButton;
        private System.Windows.Forms.NumericUpDown fontSizeTextBox;
        private System.Windows.Forms.Label label2;
    }
}