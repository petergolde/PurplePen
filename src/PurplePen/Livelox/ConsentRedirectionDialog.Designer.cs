
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
            this.cancelButton = new System.Windows.Forms.Button();
            this.continueButton = new System.Windows.Forms.Button();
            this.rememberConsentCheckBox = new System.Windows.Forms.CheckBox();
            this.informationLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(505, 216);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(100, 34);
            this.cancelButton.TabIndex = 0;
            this.cancelButton.Text = resources.GetString("cancelButton.Text");
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // continueButton
            // 
            this.continueButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.continueButton.Location = new System.Drawing.Point(399, 216);
            this.continueButton.Name = "continueButton";
            this.continueButton.Size = new System.Drawing.Size(100, 34);
            this.continueButton.TabIndex = 1;
            this.continueButton.Text = resources.GetString("continueButton.Text");
            this.continueButton.UseVisualStyleBackColor = true;
            this.continueButton.Click += new System.EventHandler(this.continueButton_Click);
            // 
            // rememberConsentCheckBox
            // 
            this.rememberConsentCheckBox.AutoSize = true;
            this.rememberConsentCheckBox.Checked = true;
            this.rememberConsentCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.rememberConsentCheckBox.Location = new System.Drawing.Point(12, 181);
            this.rememberConsentCheckBox.Name = "rememberConsentCheckBox";
            this.rememberConsentCheckBox.Size = new System.Drawing.Size(236, 19);
            this.rememberConsentCheckBox.TabIndex = 2;
            this.rememberConsentCheckBox.Text = resources.GetString("rememberConsentCheckBox.Text");
            this.rememberConsentCheckBox.UseVisualStyleBackColor = true;
            // 
            // informationLabel
            // 
            this.informationLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.informationLabel.AutoEllipsis = true;
            this.informationLabel.Location = new System.Drawing.Point(12, 16);
            this.informationLabel.Name = "informationLabel";
            this.informationLabel.Size = new System.Drawing.Size(593, 162);
            this.informationLabel.TabIndex = 3;
            this.informationLabel.Text = resources.GetString("informationLabel.Text");
            // 
            // ConsentRedirectionDialog
            // 
            this.AcceptButton = this.continueButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(617, 262);
            this.Controls.Add(this.informationLabel);
            this.Controls.Add(this.rememberConsentCheckBox);
            this.Controls.Add(this.continueButton);
            this.Controls.Add(this.cancelButton);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.HelpButton = false;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "ConsentRedirectionDialog";
            this.Text = resources.GetString("$this.Text");
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button continueButton;
        private System.Windows.Forms.CheckBox rememberConsentCheckBox;
        private System.Windows.Forms.Label informationLabel;
    }
}