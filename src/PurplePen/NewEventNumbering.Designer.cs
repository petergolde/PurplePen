namespace PurplePen
{
    partial class NewEventNumbering
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewEventNumbering));
            this.newEventNumberingLabel = new System.Windows.Forms.Label();
            this.disallowInvertibleCheckBox = new System.Windows.Forms.CheckBox();
            this.startingCodeLabel = new System.Windows.Forms.Label();
            this.startingCodeNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.changeLaterLabel = new System.Windows.Forms.Label();
            this.labelTitle = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.startingCodeNumericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // newEventNumberingLabel
            // 
            resources.ApplyResources(this.newEventNumberingLabel, "newEventNumberingLabel");
            this.newEventNumberingLabel.Name = "newEventNumberingLabel";
            // 
            // disallowInvertibleCheckBox
            // 
            resources.ApplyResources(this.disallowInvertibleCheckBox, "disallowInvertibleCheckBox");
            this.disallowInvertibleCheckBox.Name = "disallowInvertibleCheckBox";
            this.disallowInvertibleCheckBox.UseVisualStyleBackColor = true;
            // 
            // startingCodeLabel
            // 
            resources.ApplyResources(this.startingCodeLabel, "startingCodeLabel");
            this.startingCodeLabel.Name = "startingCodeLabel";
            // 
            // startingCodeNumericUpDown
            // 
            resources.ApplyResources(this.startingCodeNumericUpDown, "startingCodeNumericUpDown");
            this.startingCodeNumericUpDown.Maximum = new decimal(new int[] {
            999,
            0,
            0,
            0});
            this.startingCodeNumericUpDown.Minimum = new decimal(new int[] {
            31,
            0,
            0,
            0});
            this.startingCodeNumericUpDown.Name = "startingCodeNumericUpDown";
            this.startingCodeNumericUpDown.Value = new decimal(new int[] {
            31,
            0,
            0,
            0});
            // 
            // changeLaterLabel
            // 
            resources.ApplyResources(this.changeLaterLabel, "changeLaterLabel");
            this.changeLaterLabel.Name = "changeLaterLabel";
            // 
            // labelTitle
            // 
            resources.ApplyResources(this.labelTitle, "labelTitle");
            this.labelTitle.Name = "labelTitle";
            // 
            // NewEventNumbering
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.changeLaterLabel);
            this.Controls.Add(this.newEventNumberingLabel);
            this.Controls.Add(this.disallowInvertibleCheckBox);
            this.Controls.Add(this.startingCodeLabel);
            this.Controls.Add(this.startingCodeNumericUpDown);
            this.Name = "NewEventNumbering";
            ((System.ComponentModel.ISupportInitialize)(this.startingCodeNumericUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label newEventNumberingLabel;
        private System.Windows.Forms.Label startingCodeLabel;
        private System.Windows.Forms.Label changeLaterLabel;
        public System.Windows.Forms.CheckBox disallowInvertibleCheckBox;
        public System.Windows.Forms.NumericUpDown startingCodeNumericUpDown;
        private System.Windows.Forms.Label labelTitle;
    }
}
