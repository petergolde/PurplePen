namespace PurplePen
{
    partial class AddForkDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddForkDialog));
            this.labelWhichType = new System.Windows.Forms.Label();
            this.radioButtonFork = new System.Windows.Forms.RadioButton();
            this.radioButtonLoop = new System.Windows.Forms.RadioButton();
            this.labelNumberBranches = new System.Windows.Forms.Label();
            this.comboBoxNumberBranches = new System.Windows.Forms.ComboBox();
            this.labelNumberLoops = new System.Windows.Forms.Label();
            this.labelSummary = new System.Windows.Forms.Label();
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
            // labelWhichType
            // 
            resources.ApplyResources(this.labelWhichType, "labelWhichType");
            this.labelWhichType.Name = "labelWhichType";
            // 
            // radioButtonFork
            // 
            resources.ApplyResources(this.radioButtonFork, "radioButtonFork");
            this.radioButtonFork.Checked = true;
            this.radioButtonFork.Name = "radioButtonFork";
            this.radioButtonFork.TabStop = true;
            this.radioButtonFork.UseVisualStyleBackColor = true;
            this.radioButtonFork.CheckedChanged += new System.EventHandler(this.radioButtonFork_CheckedChanged);
            // 
            // radioButtonLoop
            // 
            resources.ApplyResources(this.radioButtonLoop, "radioButtonLoop");
            this.radioButtonLoop.Name = "radioButtonLoop";
            this.radioButtonLoop.UseVisualStyleBackColor = true;
            // 
            // labelNumberBranches
            // 
            resources.ApplyResources(this.labelNumberBranches, "labelNumberBranches");
            this.labelNumberBranches.Name = "labelNumberBranches";
            // 
            // comboBoxNumberBranches
            // 
            this.comboBoxNumberBranches.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxNumberBranches.FormattingEnabled = true;
            this.comboBoxNumberBranches.Items.AddRange(new object[] {
            resources.GetString("comboBoxNumberBranches.Items"),
            resources.GetString("comboBoxNumberBranches.Items1"),
            resources.GetString("comboBoxNumberBranches.Items2"),
            resources.GetString("comboBoxNumberBranches.Items3"),
            resources.GetString("comboBoxNumberBranches.Items4"),
            resources.GetString("comboBoxNumberBranches.Items5"),
            resources.GetString("comboBoxNumberBranches.Items6"),
            resources.GetString("comboBoxNumberBranches.Items7"),
            resources.GetString("comboBoxNumberBranches.Items8")});
            resources.ApplyResources(this.comboBoxNumberBranches, "comboBoxNumberBranches");
            this.comboBoxNumberBranches.Name = "comboBoxNumberBranches";
            this.comboBoxNumberBranches.SelectedIndexChanged += new System.EventHandler(this.comboBoxNumberBranches_SelectedIndexChanged);
            // 
            // labelNumberLoops
            // 
            resources.ApplyResources(this.labelNumberLoops, "labelNumberLoops");
            this.labelNumberLoops.Name = "labelNumberLoops";
            // 
            // labelSummary
            // 
            resources.ApplyResources(this.labelSummary, "labelSummary");
            this.labelSummary.Name = "labelSummary";
            // 
            // AddForkDialog
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.labelSummary);
            this.Controls.Add(this.labelNumberLoops);
            this.Controls.Add(this.comboBoxNumberBranches);
            this.Controls.Add(this.labelNumberBranches);
            this.Controls.Add(this.radioButtonLoop);
            this.Controls.Add(this.radioButtonFork);
            this.Controls.Add(this.labelWhichType);
            this.HelpTopic = "ItemAddVariation.htm";
            this.Name = "AddForkDialog";
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.Controls.SetChildIndex(this.labelWhichType, 0);
            this.Controls.SetChildIndex(this.radioButtonFork, 0);
            this.Controls.SetChildIndex(this.radioButtonLoop, 0);
            this.Controls.SetChildIndex(this.labelNumberBranches, 0);
            this.Controls.SetChildIndex(this.comboBoxNumberBranches, 0);
            this.Controls.SetChildIndex(this.labelNumberLoops, 0);
            this.Controls.SetChildIndex(this.labelSummary, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelWhichType;
        private System.Windows.Forms.RadioButton radioButtonFork;
        private System.Windows.Forms.RadioButton radioButtonLoop;
        private System.Windows.Forms.Label labelNumberBranches;
        private System.Windows.Forms.ComboBox comboBoxNumberBranches;
        private System.Windows.Forms.Label labelNumberLoops;
        private System.Windows.Forms.Label labelSummary;
    }
}