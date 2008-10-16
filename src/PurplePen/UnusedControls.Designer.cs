namespace PurplePen
{
    partial class UnusedControls
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UnusedControls));
            this.unusedControlsLabel = new System.Windows.Forms.Label();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.codeListBox = new System.Windows.Forms.CheckedListBox();
            this.SuspendLayout();
            // 
            // unusedControlsLabel
            // 
            resources.ApplyResources(this.unusedControlsLabel, "unusedControlsLabel");
            this.unusedControlsLabel.Name = "unusedControlsLabel";
            // 
            // cancelButton
            // 
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Name = "okButton";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // codeListBox
            // 
            resources.ApplyResources(this.codeListBox, "codeListBox");
            this.codeListBox.CheckOnClick = true;
            this.codeListBox.FormattingEnabled = true;
            this.codeListBox.Name = "codeListBox";
            // 
            // UnusedControls
            // 
            this.AcceptButton = this.okButton;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.cancelButton;
            this.Controls.Add(this.codeListBox);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.unusedControlsLabel);
            this.HelpButton = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UnusedControls";
            this.ShowIcon = false;
            this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.UnusedControls_HelpButtonClicked);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label unusedControlsLabel;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.CheckedListBox codeListBox;
    }
}