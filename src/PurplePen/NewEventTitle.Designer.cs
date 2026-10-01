namespace PurplePen
{
    partial class NewEventTitle
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewEventTitle));
            this.newEventTitleLabel = new System.Windows.Forms.Label();
            this.eventTitleLabel = new System.Windows.Forms.Label();
            this.titleText = new System.Windows.Forms.TextBox();
            this.labelTitle = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // newEventTitleLabel
            // 
            resources.ApplyResources(this.newEventTitleLabel, "newEventTitleLabel");
            this.newEventTitleLabel.Name = "newEventTitleLabel";
            // 
            // eventTitleLabel
            // 
            resources.ApplyResources(this.eventTitleLabel, "eventTitleLabel");
            this.eventTitleLabel.Name = "eventTitleLabel";
            // 
            // titleText
            // 
            resources.ApplyResources(this.titleText, "titleText");
            this.titleText.Name = "titleText";
            // 
            // labelTitle
            // 
            resources.ApplyResources(this.labelTitle, "labelTitle");
            this.labelTitle.Name = "labelTitle";
            // 
            // NewEventTitle
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.titleText);
            this.Controls.Add(this.eventTitleLabel);
            this.Controls.Add(this.newEventTitleLabel);
            this.Name = "NewEventTitle";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label newEventTitleLabel;
        private System.Windows.Forms.Label eventTitleLabel;
        public System.Windows.Forms.TextBox titleText;
        private System.Windows.Forms.Label labelTitle;
    }
}
