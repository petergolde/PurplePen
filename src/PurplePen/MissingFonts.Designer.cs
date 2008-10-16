namespace PurplePen
{
    partial class MissingFonts
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MissingFonts));
            this.labelWarning = new System.Windows.Forms.Label();
            this.listBoxFonts = new System.Windows.Forms.ListBox();
            this.checkBoxDontWarnAgain = new System.Windows.Forms.CheckBox();
            this.okButton = new System.Windows.Forms.Button();
            this.iconPictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize) (this.iconPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // labelWarning
            // 
            resources.ApplyResources(this.labelWarning, "labelWarning");
            this.labelWarning.Name = "labelWarning";
            // 
            // listBoxFonts
            // 
            this.listBoxFonts.FormattingEnabled = true;
            resources.ApplyResources(this.listBoxFonts, "listBoxFonts");
            this.listBoxFonts.Name = "listBoxFonts";
            this.listBoxFonts.Sorted = true;
            // 
            // checkBoxDontWarnAgain
            // 
            resources.ApplyResources(this.checkBoxDontWarnAgain, "checkBoxDontWarnAgain");
            this.checkBoxDontWarnAgain.Name = "checkBoxDontWarnAgain";
            this.checkBoxDontWarnAgain.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.Name = "okButton";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // iconPictureBox
            // 
            resources.ApplyResources(this.iconPictureBox, "iconPictureBox");
            this.iconPictureBox.Name = "iconPictureBox";
            this.iconPictureBox.TabStop = false;
            // 
            // MissingFonts
            // 
            this.AcceptButton = this.okButton;
            this.AllowDrop = true;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.iconPictureBox);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.checkBoxDontWarnAgain);
            this.Controls.Add(this.listBoxFonts);
            this.Controls.Add(this.labelWarning);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.HelpButton = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MissingFonts";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.MissingFonts_HelpButtonClicked);
            ((System.ComponentModel.ISupportInitialize) (this.iconPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelWarning;
        private System.Windows.Forms.ListBox listBoxFonts;
        private System.Windows.Forms.CheckBox checkBoxDontWarnAgain;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.PictureBox iconPictureBox;

    }
}