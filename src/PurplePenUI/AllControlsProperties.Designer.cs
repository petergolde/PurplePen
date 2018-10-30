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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.descriptionAppearanceLabel = new System.Windows.Forms.Label();
            this.printingScaleLabel = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.oneToPrefix = new System.Windows.Forms.Label();
            this.scaleCombo = new System.Windows.Forms.ComboBox();
            this.descKindCombo = new System.Windows.Forms.ComboBox();
            this.allControlsPropertiesLabel = new System.Windows.Forms.Label();
            this.appearanceGroupBox.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
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
            // appearanceGroupBox
            // 
            this.appearanceGroupBox.Controls.Add(this.tableLayoutPanel1);
            resources.ApplyResources(this.appearanceGroupBox, "appearanceGroupBox");
            this.appearanceGroupBox.Name = "appearanceGroupBox";
            this.appearanceGroupBox.TabStop = false;
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.descriptionAppearanceLabel, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.printingScaleLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.descKindCombo, 1, 1);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
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
            // flowLayoutPanel1
            // 
            resources.ApplyResources(this.flowLayoutPanel1, "flowLayoutPanel1");
            this.flowLayoutPanel1.Controls.Add(this.oneToPrefix);
            this.flowLayoutPanel1.Controls.Add(this.scaleCombo);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            // 
            // oneToPrefix
            // 
            resources.ApplyResources(this.oneToPrefix, "oneToPrefix");
            this.oneToPrefix.Name = "oneToPrefix";
            // 
            // scaleCombo
            // 
            this.scaleCombo.FormattingEnabled = true;
            resources.ApplyResources(this.scaleCombo, "scaleCombo");
            this.scaleCombo.Name = "scaleCombo";
            // 
            // descKindCombo
            // 
            resources.ApplyResources(this.descKindCombo, "descKindCombo");
            this.descKindCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.descKindCombo.FormattingEnabled = true;
            this.descKindCombo.Items.AddRange(new object[] {
            resources.GetString("descKindCombo.Items"),
            resources.GetString("descKindCombo.Items1"),
            resources.GetString("descKindCombo.Items2")});
            this.descKindCombo.Name = "descKindCombo";
            // 
            // allControlsPropertiesLabel
            // 
            resources.ApplyResources(this.allControlsPropertiesLabel, "allControlsPropertiesLabel");
            this.allControlsPropertiesLabel.Name = "allControlsPropertiesLabel";
            // 
            // AllControlsProperties
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.allControlsPropertiesLabel);
            this.Controls.Add(this.appearanceGroupBox);
            this.HelpTopic = "AllControlsProperties.htm";
            this.Name = "AllControlsProperties";
            this.Controls.SetChildIndex(this.appearanceGroupBox, 0);
            this.Controls.SetChildIndex(this.allControlsPropertiesLabel, 0);
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.appearanceGroupBox.ResumeLayout(false);
            this.appearanceGroupBox.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox appearanceGroupBox;
        private System.Windows.Forms.Label allControlsPropertiesLabel;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label descriptionAppearanceLabel;
        private System.Windows.Forms.Label printingScaleLabel;
        private System.Windows.Forms.ComboBox descKindCombo;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Label oneToPrefix;
        private System.Windows.Forms.ComboBox scaleCombo;
    }
}