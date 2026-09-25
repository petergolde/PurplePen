namespace PurplePen
{
    partial class LicenseForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LicenseForm));
            this.licenseTextBox = new System.Windows.Forms.RichTextBox();
            this.okButton = new System.Windows.Forms.Button();
            this.bsdLicenseLinkLabel = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // licenseTextBox
            // 
            resources.ApplyResources(this.licenseTextBox, "licenseTextBox");
            this.licenseTextBox.BackColor = System.Drawing.SystemColors.Window;
            this.licenseTextBox.Name = "licenseTextBox";
            this.licenseTextBox.ReadOnly = true;
            // 
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Name = "okButton";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // bsdLicenseLinkLabel
            // 
            resources.ApplyResources(this.bsdLicenseLinkLabel, "bsdLicenseLinkLabel");
            this.bsdLicenseLinkLabel.Name = "bsdLicenseLinkLabel";
            this.bsdLicenseLinkLabel.TabStop = true;
            this.bsdLicenseLinkLabel.UseCompatibleTextRendering = true;
            this.bsdLicenseLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // LicenseForm
            // 
            this.AcceptButton = this.okButton;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.bsdLicenseLinkLabel);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.licenseTextBox);
            this.Name = "LicenseForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox licenseTextBox;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.LinkLabel bsdLicenseLinkLabel;
    }
}
