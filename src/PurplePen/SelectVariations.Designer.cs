namespace PurplePen
{
    partial class SelectVariations
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SelectVariations));
            this.comboBoxVariations = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.labelAllVariations = new System.Windows.Forms.Label();
            this.labelSeparateVariations = new System.Windows.Forms.Label();
            this.labelByLeg = new System.Windows.Forms.Label();
            this.labelByLegNotAvailable = new System.Windows.Forms.Label();
            this.panelByVariation = new System.Windows.Forms.Panel();
            this.checkedListBoxVariations = new System.Windows.Forms.CheckedListBox();
            this.checkBoxSelectIndividualVariations = new System.Windows.Forms.CheckBox();
            this.panelByTeam = new System.Windows.Forms.Panel();
            this.labelNumberOfTeams = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.upDownLastTeam = new System.Windows.Forms.NumericUpDown();
            this.upDownFirstTeam = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.panelByVariation.SuspendLayout();
            this.panelByTeam.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownLastTeam)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownFirstTeam)).BeginInit();
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
            // comboBoxVariations
            // 
            resources.ApplyResources(this.comboBoxVariations, "comboBoxVariations");
            this.comboBoxVariations.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxVariations.FormattingEnabled = true;
            this.comboBoxVariations.Items.AddRange(new object[] {
            resources.GetString("comboBoxVariations.Items"),
            resources.GetString("comboBoxVariations.Items1"),
            resources.GetString("comboBoxVariations.Items2")});
            this.comboBoxVariations.Name = "comboBoxVariations";
            this.comboBoxVariations.SelectedIndexChanged += new System.EventHandler(this.comboBoxVariations_SelectedIndexChanged);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // labelAllVariations
            // 
            resources.ApplyResources(this.labelAllVariations, "labelAllVariations");
            this.labelAllVariations.Name = "labelAllVariations";
            // 
            // labelSeparateVariations
            // 
            resources.ApplyResources(this.labelSeparateVariations, "labelSeparateVariations");
            this.labelSeparateVariations.Name = "labelSeparateVariations";
            // 
            // labelByLeg
            // 
            resources.ApplyResources(this.labelByLeg, "labelByLeg");
            this.labelByLeg.Name = "labelByLeg";
            // 
            // labelByLegNotAvailable
            // 
            resources.ApplyResources(this.labelByLegNotAvailable, "labelByLegNotAvailable");
            this.labelByLegNotAvailable.Name = "labelByLegNotAvailable";
            // 
            // panelByVariation
            // 
            resources.ApplyResources(this.panelByVariation, "panelByVariation");
            this.panelByVariation.Controls.Add(this.checkedListBoxVariations);
            this.panelByVariation.Controls.Add(this.checkBoxSelectIndividualVariations);
            this.panelByVariation.Name = "panelByVariation";
            // 
            // checkedListBoxVariations
            // 
            resources.ApplyResources(this.checkedListBoxVariations, "checkedListBoxVariations");
            this.checkedListBoxVariations.CheckOnClick = true;
            this.checkedListBoxVariations.FormattingEnabled = true;
            this.checkedListBoxVariations.Name = "checkedListBoxVariations";
            // 
            // checkBoxSelectIndividualVariations
            // 
            resources.ApplyResources(this.checkBoxSelectIndividualVariations, "checkBoxSelectIndividualVariations");
            this.checkBoxSelectIndividualVariations.Name = "checkBoxSelectIndividualVariations";
            this.checkBoxSelectIndividualVariations.UseVisualStyleBackColor = true;
            this.checkBoxSelectIndividualVariations.CheckedChanged += new System.EventHandler(this.checkBoxSelectIndividualVariations_CheckedChanged);
            // 
            // panelByTeam
            // 
            resources.ApplyResources(this.panelByTeam, "panelByTeam");
            this.panelByTeam.Controls.Add(this.labelNumberOfTeams);
            this.panelByTeam.Controls.Add(this.label3);
            this.panelByTeam.Controls.Add(this.upDownLastTeam);
            this.panelByTeam.Controls.Add(this.upDownFirstTeam);
            this.panelByTeam.Controls.Add(this.label2);
            this.panelByTeam.Name = "panelByTeam";
            // 
            // labelNumberOfTeams
            // 
            resources.ApplyResources(this.labelNumberOfTeams, "labelNumberOfTeams");
            this.labelNumberOfTeams.Name = "labelNumberOfTeams";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // upDownLastTeam
            // 
            resources.ApplyResources(this.upDownLastTeam, "upDownLastTeam");
            this.upDownLastTeam.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.upDownLastTeam.Name = "upDownLastTeam";
            this.upDownLastTeam.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.upDownLastTeam.ValueChanged += new System.EventHandler(this.upDownTeam_ValueChanged);
            // 
            // upDownFirstTeam
            // 
            resources.ApplyResources(this.upDownFirstTeam, "upDownFirstTeam");
            this.upDownFirstTeam.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.upDownFirstTeam.Name = "upDownFirstTeam";
            this.upDownFirstTeam.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.upDownFirstTeam.ValueChanged += new System.EventHandler(this.upDownTeam_ValueChanged);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // SelectVariations
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.panelByTeam);
            this.Controls.Add(this.panelByVariation);
            this.Controls.Add(this.labelByLegNotAvailable);
            this.Controls.Add(this.labelByLeg);
            this.Controls.Add(this.labelSeparateVariations);
            this.Controls.Add(this.labelAllVariations);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBoxVariations);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Name = "SelectVariations";
            this.Controls.SetChildIndex(this.comboBoxVariations, 0);
            this.Controls.SetChildIndex(this.label1, 0);
            this.Controls.SetChildIndex(this.labelAllVariations, 0);
            this.Controls.SetChildIndex(this.labelSeparateVariations, 0);
            this.Controls.SetChildIndex(this.labelByLeg, 0);
            this.Controls.SetChildIndex(this.labelByLegNotAvailable, 0);
            this.Controls.SetChildIndex(this.panelByVariation, 0);
            this.Controls.SetChildIndex(this.panelByTeam, 0);
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.panelByVariation.ResumeLayout(false);
            this.panelByVariation.PerformLayout();
            this.panelByTeam.ResumeLayout(false);
            this.panelByTeam.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownLastTeam)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownFirstTeam)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxVariations;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelAllVariations;
        private System.Windows.Forms.Label labelSeparateVariations;
        private System.Windows.Forms.Label labelByLeg;
        private System.Windows.Forms.Label labelByLegNotAvailable;
        private System.Windows.Forms.Panel panelByVariation;
        private System.Windows.Forms.Panel panelByTeam;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown upDownLastTeam;
        private System.Windows.Forms.NumericUpDown upDownFirstTeam;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckedListBox checkedListBoxVariations;
        private System.Windows.Forms.CheckBox checkBoxSelectIndividualVariations;
        private System.Windows.Forms.Label labelNumberOfTeams;
    }
}
