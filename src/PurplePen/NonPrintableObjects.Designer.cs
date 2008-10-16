namespace PurplePen
{
    partial class NonPrintableObjects
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NonPrintableObjects));
            this.labelWarning = new System.Windows.Forms.Label();
            this.listBoxBadObjects = new System.Windows.Forms.ListBox();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.iconWarningPictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize) (this.iconWarningPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // labelWarning
            // 
            resources.ApplyResources(this.labelWarning, "labelWarning");
            this.labelWarning.Name = "labelWarning";
            // 
            // listBoxBadObjects
            // 
            this.listBoxBadObjects.FormattingEnabled = true;
            resources.ApplyResources(this.listBoxBadObjects, "listBoxBadObjects");
            this.listBoxBadObjects.Name = "listBoxBadObjects";
            this.listBoxBadObjects.Sorted = true;
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.Name = "okButton";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // iconWarningPictureBox
            // 
            resources.ApplyResources(this.iconWarningPictureBox, "iconWarningPictureBox");
            this.iconWarningPictureBox.Name = "iconWarningPictureBox";
            this.iconWarningPictureBox.TabStop = false;
            // 
            // NonPrintableObjects
            // 
            this.AcceptButton = this.okButton;
            this.AllowDrop = true;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.cancelButton;
            this.Controls.Add(this.iconWarningPictureBox);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.listBoxBadObjects);
            this.Controls.Add(this.labelWarning);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.HelpButton = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NonPrintableObjects";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.NonPrintableObjects_HelpButtonClicked);
            ((System.ComponentModel.ISupportInitialize) (this.iconWarningPictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label labelWarning;
        private System.Windows.Forms.ListBox listBoxBadObjects;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.PictureBox iconWarningPictureBox;

    }
}