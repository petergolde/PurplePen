namespace TranslateTool
{
    partial class OpenDirectory
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
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.buttonSelectDirectory = new System.Windows.Forms.Button();
            this.comboBoxLanguage = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxDirectory = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.SuspendLayout();
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(316, 229);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 0;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(235, 229);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 1;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // buttonSelectDirectory
            // 
            this.buttonSelectDirectory.AutoSize = true;
            this.buttonSelectDirectory.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonSelectDirectory.Location = new System.Drawing.Point(13, 64);
            this.buttonSelectDirectory.Name = "buttonSelectDirectory";
            this.buttonSelectDirectory.Size = new System.Drawing.Size(88, 23);
            this.buttonSelectDirectory.TabIndex = 2;
            this.buttonSelectDirectory.Text = "Select Folder...";
            this.buttonSelectDirectory.UseVisualStyleBackColor = true;
            this.buttonSelectDirectory.Click += new System.EventHandler(this.buttonSelectDirectory_Click);
            // 
            // comboBoxLanguage
            // 
            this.comboBoxLanguage.Anchor = ((System.Windows.Forms.AnchorStyles) (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxLanguage.DisplayMember = "DisplayName";
            this.comboBoxLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLanguage.FormattingEnabled = true;
            this.comboBoxLanguage.Location = new System.Drawing.Point(86, 149);
            this.comboBoxLanguage.Name = "comboBoxLanguage";
            this.comboBoxLanguage.Size = new System.Drawing.Size(186, 21);
            this.comboBoxLanguage.Sorted = true;
            this.comboBoxLanguage.TabIndex = 3;
            this.comboBoxLanguage.ValueMember = "DisplayName";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 152);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Language:";
            // 
            // textBoxDirectory
            // 
            this.textBoxDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxDirectory.Location = new System.Drawing.Point(13, 94);
            this.textBoxDirectory.Name = "textBoxDirectory";
            this.textBoxDirectory.ReadOnly = true;
            this.textBoxDirectory.Size = new System.Drawing.Size(378, 20);
            this.textBoxDirectory.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(13, 13);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(259, 39);
            this.label2.TabIndex = 6;
            this.label2.Text = "Select a folder to translate, and a language to translate to.";
            // 
            // folderBrowserDialog
            // 
            this.folderBrowserDialog.Description = "Select the folder which contains the files to translate.";
            // 
            // OpenDirectory
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(403, 264);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxDirectory);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBoxLanguage);
            this.Controls.Add(this.buttonSelectDirectory);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OpenDirectory";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Open Folder for Translation";
            this.Load += new System.EventHandler(this.OpenDirectory_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button buttonSelectDirectory;
        private System.Windows.Forms.ComboBox comboBoxLanguage;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxDirectory;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
    }
}