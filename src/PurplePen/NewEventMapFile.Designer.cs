namespace PurplePen
{
    partial class NewEventMapFile
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewEventMapFile));
            this.newEventMapFileLabel = new System.Windows.Forms.Label();
            this.chooseMapFileButton = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.mapFileDisplay = new System.Windows.Forms.GroupBox();
            this.mapFileNameTextBox = new System.Windows.Forms.TextBox();
            this.errorDisplayPanel = new System.Windows.Forms.Panel();
            this.errorMessage = new System.Windows.Forms.Label();
            this.warningIconPictureBox = new System.Windows.Forms.PictureBox();
            this.infoDisplayPanel = new System.Windows.Forms.Panel();
            this.infoMessage = new System.Windows.Forms.Label();
            this.infoIconPictureBox = new System.Windows.Forms.PictureBox();
            this.mapFileDisplay.SuspendLayout();
            this.errorDisplayPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.warningIconPictureBox)).BeginInit();
            this.infoDisplayPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.infoIconPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // newEventMapFileLabel
            // 
            resources.ApplyResources(this.newEventMapFileLabel, "newEventMapFileLabel");
            this.newEventMapFileLabel.Name = "newEventMapFileLabel";
            // 
            // chooseMapFileButton
            // 
            resources.ApplyResources(this.chooseMapFileButton, "chooseMapFileButton");
            this.chooseMapFileButton.Name = "chooseMapFileButton";
            this.chooseMapFileButton.UseVisualStyleBackColor = true;
            this.chooseMapFileButton.Click += new System.EventHandler(this.button1_Click);
            // 
            // openFileDialog
            // 
            this.openFileDialog.DefaultExt = "ocd";
            resources.ApplyResources(this.openFileDialog, "openFileDialog");
            // 
            // mapFileDisplay
            // 
            this.mapFileDisplay.Controls.Add(this.mapFileNameTextBox);
            resources.ApplyResources(this.mapFileDisplay, "mapFileDisplay");
            this.mapFileDisplay.Name = "mapFileDisplay";
            this.mapFileDisplay.TabStop = false;
            // 
            // mapFileNameTextBox
            // 
            resources.ApplyResources(this.mapFileNameTextBox, "mapFileNameTextBox");
            this.mapFileNameTextBox.Name = "mapFileNameTextBox";
            this.mapFileNameTextBox.ReadOnly = true;
            // 
            // errorDisplayPanel
            // 
            this.errorDisplayPanel.Controls.Add(this.errorMessage);
            this.errorDisplayPanel.Controls.Add(this.warningIconPictureBox);
            resources.ApplyResources(this.errorDisplayPanel, "errorDisplayPanel");
            this.errorDisplayPanel.Name = "errorDisplayPanel";
            // 
            // errorMessage
            // 
            resources.ApplyResources(this.errorMessage, "errorMessage");
            this.errorMessage.ForeColor = System.Drawing.Color.Red;
            this.errorMessage.Name = "errorMessage";
            // 
            // warningIconPictureBox
            // 
            resources.ApplyResources(this.warningIconPictureBox, "warningIconPictureBox");
            this.warningIconPictureBox.Name = "warningIconPictureBox";
            this.warningIconPictureBox.TabStop = false;
            // 
            // infoDisplayPanel
            // 
            this.infoDisplayPanel.Controls.Add(this.infoMessage);
            this.infoDisplayPanel.Controls.Add(this.infoIconPictureBox);
            resources.ApplyResources(this.infoDisplayPanel, "infoDisplayPanel");
            this.infoDisplayPanel.Name = "infoDisplayPanel";
            // 
            // infoMessage
            // 
            resources.ApplyResources(this.infoMessage, "infoMessage");
            this.infoMessage.Name = "infoMessage";
            // 
            // infoIconPictureBox
            // 
            resources.ApplyResources(this.infoIconPictureBox, "infoIconPictureBox");
            this.infoIconPictureBox.Name = "infoIconPictureBox";
            this.infoIconPictureBox.TabStop = false;
            // 
            // NewEventMapFile
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.infoDisplayPanel);
            this.Controls.Add(this.errorDisplayPanel);
            this.Controls.Add(this.mapFileDisplay);
            this.Controls.Add(this.chooseMapFileButton);
            this.Controls.Add(this.newEventMapFileLabel);
            this.Name = "NewEventMapFile";
            this.Load += new System.EventHandler(this.NewEventMapFile_Load);
            this.mapFileDisplay.ResumeLayout(false);
            this.mapFileDisplay.PerformLayout();
            this.errorDisplayPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) (this.warningIconPictureBox)).EndInit();
            this.infoDisplayPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) (this.infoIconPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label newEventMapFileLabel;
        private System.Windows.Forms.Button chooseMapFileButton;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.GroupBox mapFileDisplay;
        private System.Windows.Forms.Panel errorDisplayPanel;
        private System.Windows.Forms.Label errorMessage;
        private System.Windows.Forms.PictureBox warningIconPictureBox;
        public System.Windows.Forms.TextBox mapFileNameTextBox;
        private System.Windows.Forms.Panel infoDisplayPanel;
        private System.Windows.Forms.Label infoMessage;
        private System.Windows.Forms.PictureBox infoIconPictureBox;
    }
}
