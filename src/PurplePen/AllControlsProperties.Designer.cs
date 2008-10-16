namespace PurplePen
{
    partial class AllControlsProperties
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AllControlsProperties));
            this.appearanceGroupBox = new System.Windows.Forms.GroupBox();
            this.oneToPrefix = new System.Windows.Forms.Label();
            this.descriptionAppearanceLabel = new System.Windows.Forms.Label();
            this.printingScaleLabel = new System.Windows.Forms.Label();
            this.descKindCombo = new System.Windows.Forms.ComboBox();
            this.scaleCombo = new System.Windows.Forms.ComboBox();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.allControlsPropertiesLabel = new System.Windows.Forms.Label();
            this.appearanceGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // appearanceGroupBox
            // 
            this.appearanceGroupBox.Controls.Add(this.oneToPrefix);
            this.appearanceGroupBox.Controls.Add(this.descriptionAppearanceLabel);
            this.appearanceGroupBox.Controls.Add(this.printingScaleLabel);
            this.appearanceGroupBox.Controls.Add(this.descKindCombo);
            this.appearanceGroupBox.Controls.Add(this.scaleCombo);
            resources.ApplyResources(this.appearanceGroupBox, "appearanceGroupBox");
            this.appearanceGroupBox.Name = "appearanceGroupBox";
            this.appearanceGroupBox.TabStop = false;
            // 
            // oneToPrefix
            // 
            resources.ApplyResources(this.oneToPrefix, "oneToPrefix");
            this.oneToPrefix.Name = "oneToPrefix";
            // 
            // descriptionAppearanceLabel
            // 
            resources.ApplyResources(this.descriptionAppearanceLabel, "descriptionAppearanceLabel");
            this.descriptionAppearanceLabel.Name = "descriptionAppearanceLabel";
            // 
            // printingScaleLabel
            // 
            resources.ApplyResources(this.printingScaleLabel, "printingScaleLabel");
            this.printingScaleLabel.Name = "printingScaleLabel";
            // 
            // descKindCombo
            // 
            this.descKindCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.descKindCombo.FormattingEnabled = true;
            this.descKindCombo.Items.AddRange(new object[] {
            resources.GetString("descKindCombo.Items"),
            resources.GetString("descKindCombo.Items1"),
            resources.GetString("descKindCombo.Items2")});
            resources.ApplyResources(this.descKindCombo, "descKindCombo");
            this.descKindCombo.Name = "descKindCombo";
            // 
            // scaleCombo
            // 
            this.scaleCombo.FormattingEnabled = true;
            resources.ApplyResources(this.scaleCombo, "scaleCombo");
            this.scaleCombo.Name = "scaleCombo";
            // 
            // okButton
            // 
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
            // allControlsPropertiesLabel
            // 
            resources.ApplyResources(this.allControlsPropertiesLabel, "allControlsPropertiesLabel");
            this.allControlsPropertiesLabel.Name = "allControlsPropertiesLabel";
            // 
            // AllControlsProperties
            // 
            this.AcceptButton = this.okButton;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.cancelButton;
            this.Controls.Add(this.allControlsPropertiesLabel);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.appearanceGroupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.HelpButton = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AllControlsProperties";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.AllControlsProperties_HelpButtonClicked);
            this.appearanceGroupBox.ResumeLayout(false);
            this.appearanceGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox appearanceGroupBox;
        private System.Windows.Forms.Label oneToPrefix;
        private System.Windows.Forms.Label descriptionAppearanceLabel;
        private System.Windows.Forms.Label printingScaleLabel;
        private System.Windows.Forms.ComboBox descKindCombo;
        private System.Windows.Forms.ComboBox scaleCombo;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label allControlsPropertiesLabel;
    }
}