namespace TranslateTool
{
    partial class SynchronizePOs
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
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxResXDirectory = new System.Windows.Forms.TextBox();
            this.buttonSelectResXDirectory = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxPODirectory = new System.Windows.Forms.TextBox();
            this.buttonSelectPODirectory = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(12, 21);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(259, 39);
            this.label2.TabIndex = 9;
            this.label2.Text = "Select a folder to translate, and a language to translate to.";
            // 
            // textBoxResXDirectory
            // 
            this.textBoxResXDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxResXDirectory.Location = new System.Drawing.Point(12, 102);
            this.textBoxResXDirectory.Name = "textBoxResXDirectory";
            this.textBoxResXDirectory.ReadOnly = true;
            this.textBoxResXDirectory.Size = new System.Drawing.Size(413, 20);
            this.textBoxResXDirectory.TabIndex = 8;
            // 
            // buttonSelectResXDirectory
            // 
            this.buttonSelectResXDirectory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSelectResXDirectory.AutoSize = true;
            this.buttonSelectResXDirectory.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonSelectResXDirectory.Location = new System.Drawing.Point(433, 100);
            this.buttonSelectResXDirectory.Name = "buttonSelectResXDirectory";
            this.buttonSelectResXDirectory.Size = new System.Drawing.Size(88, 23);
            this.buttonSelectResXDirectory.TabIndex = 7;
            this.buttonSelectResXDirectory.Text = "Select Folder...";
            this.buttonSelectResXDirectory.UseVisualStyleBackColor = true;
            this.buttonSelectResXDirectory.Click += new System.EventHandler(this.buttonSelectResXDirectory_Click);
            // 
            // okButton
            // 
            this.okButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(365, 227);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 11;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(446, 227);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 10;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // folderBrowserDialog
            // 
            this.folderBrowserDialog.Description = "Select the folder which contains the files to translate.";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 83);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 13);
            this.label1.TabIndex = 12;
            this.label1.Text = "ResX folder:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 146);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(54, 13);
            this.label3.TabIndex = 15;
            this.label3.Text = "PO folder:";
            // 
            // textBoxPODirectory
            // 
            this.textBoxPODirectory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxPODirectory.Location = new System.Drawing.Point(12, 165);
            this.textBoxPODirectory.Name = "textBoxPODirectory";
            this.textBoxPODirectory.ReadOnly = true;
            this.textBoxPODirectory.Size = new System.Drawing.Size(413, 20);
            this.textBoxPODirectory.TabIndex = 14;
            // 
            // buttonSelectPODirectory
            // 
            this.buttonSelectPODirectory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSelectPODirectory.AutoSize = true;
            this.buttonSelectPODirectory.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonSelectPODirectory.Location = new System.Drawing.Point(433, 163);
            this.buttonSelectPODirectory.Name = "buttonSelectPODirectory";
            this.buttonSelectPODirectory.Size = new System.Drawing.Size(88, 23);
            this.buttonSelectPODirectory.TabIndex = 13;
            this.buttonSelectPODirectory.Text = "Select Folder...";
            this.buttonSelectPODirectory.UseVisualStyleBackColor = true;
            this.buttonSelectPODirectory.Click += new System.EventHandler(this.buttonSelectPODirectory_Click);
            // 
            // SynchronizePOs
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(531, 262);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxPODirectory);
            this.Controls.Add(this.buttonSelectPODirectory);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxResXDirectory);
            this.Controls.Add(this.buttonSelectResXDirectory);
            this.Name = "SynchronizePOs";
            this.Text = "SynchronizePOs";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxResXDirectory;
        private System.Windows.Forms.Button buttonSelectResXDirectory;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxPODirectory;
        private System.Windows.Forms.Button buttonSelectPODirectory;
    }
}