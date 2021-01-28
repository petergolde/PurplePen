
namespace PurplePen.Livelox
{
    partial class ConsentRedirectionDialog
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
            if (disposing && (components != null))
            {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConsentRedirectionDialog));
            this.rememberConsentCheckBox = new System.Windows.Forms.CheckBox();
            this.informationLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.Click += new System.EventHandler(this.continueButton_Click);
            // 
            // cancelButton
            // 
            resources.ApplyResources(this.cancelButton, "cancelButton");
            // 
            // rememberConsentCheckBox
            // 
            resources.ApplyResources(this.rememberConsentCheckBox, "rememberConsentCheckBox");
            this.rememberConsentCheckBox.Checked = true;
            this.rememberConsentCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.rememberConsentCheckBox.Name = "rememberConsentCheckBox";
            this.rememberConsentCheckBox.UseVisualStyleBackColor = true;
            // 
            // informationLabel
            // 
            resources.ApplyResources(this.informationLabel, "informationLabel");
            this.informationLabel.AutoEllipsis = true;
            this.informationLabel.Name = "informationLabel";
            // 
            // ConsentRedirectionDialog
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.informationLabel);
            this.Controls.Add(this.rememberConsentCheckBox);
            this.HelpButton = false;
            this.Name = "ConsentRedirectionDialog";
            this.Controls.SetChildIndex(this.rememberConsentCheckBox, 0);
            this.Controls.SetChildIndex(this.informationLabel, 0);
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.CheckBox rememberConsentCheckBox;
        private System.Windows.Forms.Label informationLabel;
    }
}