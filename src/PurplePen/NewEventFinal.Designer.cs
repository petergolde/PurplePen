namespace PurplePen
{
    partial class NewEventFinal
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewEventFinal));
            this.newEventFinalLabel = new System.Windows.Forms.Label();
            this.afterEventCreatedLabel = new System.Windows.Forms.Label();
            this.eventFileName = new System.Windows.Forms.TextBox();
            this.warningIconPictureBox = new System.Windows.Forms.PictureBox();
            this.errorMessage = new System.Windows.Forms.Label();
            this.errorDisplayPanel = new System.Windows.Forms.Panel();
            this.labelTitle = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.warningIconPictureBox)).BeginInit();
            this.errorDisplayPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // newEventFinalLabel
            // 
            resources.ApplyResources(this.newEventFinalLabel, "newEventFinalLabel");
            this.newEventFinalLabel.Name = "newEventFinalLabel";
            // 
            // afterEventCreatedLabel
            // 
            resources.ApplyResources(this.afterEventCreatedLabel, "afterEventCreatedLabel");
            this.afterEventCreatedLabel.Name = "afterEventCreatedLabel";
            // 
            // eventFileName
            // 
            resources.ApplyResources(this.eventFileName, "eventFileName");
            this.eventFileName.Name = "eventFileName";
            this.eventFileName.ReadOnly = true;
            // 
            // warningIconPictureBox
            // 
            resources.ApplyResources(this.warningIconPictureBox, "warningIconPictureBox");
            this.warningIconPictureBox.Name = "warningIconPictureBox";
            this.warningIconPictureBox.TabStop = false;
            // 
            // errorMessage
            // 
            resources.ApplyResources(this.errorMessage, "errorMessage");
            this.errorMessage.ForeColor = System.Drawing.Color.Red;
            this.errorMessage.Name = "errorMessage";
            // 
            // errorDisplayPanel
            // 
            this.errorDisplayPanel.Controls.Add(this.errorMessage);
            this.errorDisplayPanel.Controls.Add(this.warningIconPictureBox);
            resources.ApplyResources(this.errorDisplayPanel, "errorDisplayPanel");
            this.errorDisplayPanel.Name = "errorDisplayPanel";
            // 
            // labelTitle
            // 
            resources.ApplyResources(this.labelTitle, "labelTitle");
            this.labelTitle.Name = "labelTitle";
            // 
            // NewEventFinal
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.errorDisplayPanel);
            this.Controls.Add(this.eventFileName);
            this.Controls.Add(this.afterEventCreatedLabel);
            this.Controls.Add(this.newEventFinalLabel);
            this.Name = "NewEventFinal";
            ((System.ComponentModel.ISupportInitialize)(this.warningIconPictureBox)).EndInit();
            this.errorDisplayPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label newEventFinalLabel;
        private System.Windows.Forms.Label afterEventCreatedLabel;
        public System.Windows.Forms.TextBox eventFileName;
        private System.Windows.Forms.PictureBox warningIconPictureBox;
        public System.Windows.Forms.Label errorMessage;
        public System.Windows.Forms.Panel errorDisplayPanel;
        private System.Windows.Forms.Label labelTitle;
    }
}
