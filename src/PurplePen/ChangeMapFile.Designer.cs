namespace PurplePen
{
    partial class ChangeMapFile
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChangeMapFile));
            this.errorDisplayPanel = new System.Windows.Forms.Panel();
            this.errorMessage = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.mapFileDisplay = new System.Windows.Forms.GroupBox();
            this.mapFileNameTextBox = new System.Windows.Forms.TextBox();
            this.buttonChooseFile = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.panelScaleDpi = new System.Windows.Forms.Panel();
            this.labelDpi2 = new System.Windows.Forms.Label();
            this.textBoxDpi = new System.Windows.Forms.TextBox();
            this.labelDpi = new System.Windows.Forms.Label();
            this.textBoxScale = new System.Windows.Forms.TextBox();
            this.labelScale = new System.Windows.Forms.Label();
            this.errorDisplayPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.pictureBox1)).BeginInit();
            this.mapFileDisplay.SuspendLayout();
            this.panelScaleDpi.SuspendLayout();
            this.SuspendLayout();
            // 
            // errorDisplayPanel
            // 
            this.errorDisplayPanel.BackColor = System.Drawing.SystemColors.Control;
            this.errorDisplayPanel.Controls.Add(this.errorMessage);
            this.errorDisplayPanel.Controls.Add(this.pictureBox1);
            resources.ApplyResources(this.errorDisplayPanel, "errorDisplayPanel");
            this.errorDisplayPanel.Name = "errorDisplayPanel";
            this.errorDisplayPanel.UseWaitCursor = true;
            // 
            // errorMessage
            // 
            resources.ApplyResources(this.errorMessage, "errorMessage");
            this.errorMessage.ForeColor = System.Drawing.Color.Red;
            this.errorMessage.Name = "errorMessage";
            this.errorMessage.UseWaitCursor = true;
            // 
            // pictureBox1
            // 
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            this.pictureBox1.UseWaitCursor = true;
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
            // buttonChooseFile
            // 
            resources.ApplyResources(this.buttonChooseFile, "buttonChooseFile");
            this.buttonChooseFile.Name = "buttonChooseFile";
            this.buttonChooseFile.UseVisualStyleBackColor = true;
            this.buttonChooseFile.Click += new System.EventHandler(this.buttonChooseFile_Click);
            // 
            // buttonOK
            // 
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            resources.ApplyResources(this.buttonOK, "buttonOK");
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.UseVisualStyleBackColor = true;
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            resources.ApplyResources(this.buttonCancel, "buttonCancel");
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // openFileDialog
            // 
            this.openFileDialog.DefaultExt = "ocd";
            resources.ApplyResources(this.openFileDialog, "openFileDialog");
            // 
            // panelScaleDpi
            // 
            this.panelScaleDpi.BackColor = System.Drawing.SystemColors.Control;
            this.panelScaleDpi.Controls.Add(this.labelDpi2);
            this.panelScaleDpi.Controls.Add(this.textBoxDpi);
            this.panelScaleDpi.Controls.Add(this.labelDpi);
            this.panelScaleDpi.Controls.Add(this.textBoxScale);
            this.panelScaleDpi.Controls.Add(this.labelScale);
            resources.ApplyResources(this.panelScaleDpi, "panelScaleDpi");
            this.panelScaleDpi.Name = "panelScaleDpi";
            // 
            // labelDpi2
            // 
            resources.ApplyResources(this.labelDpi2, "labelDpi2");
            this.labelDpi2.Name = "labelDpi2";
            // 
            // textBoxDpi
            // 
            resources.ApplyResources(this.textBoxDpi, "textBoxDpi");
            this.textBoxDpi.Name = "textBoxDpi";
            this.textBoxDpi.TextChanged += new System.EventHandler(this.textBoxDpi_TextChanged);
            // 
            // labelDpi
            // 
            resources.ApplyResources(this.labelDpi, "labelDpi");
            this.labelDpi.Name = "labelDpi";
            // 
            // textBoxScale
            // 
            resources.ApplyResources(this.textBoxScale, "textBoxScale");
            this.textBoxScale.Name = "textBoxScale";
            this.textBoxScale.TextChanged += new System.EventHandler(this.textBoxScale_TextChanged);
            // 
            // labelScale
            // 
            resources.ApplyResources(this.labelScale, "labelScale");
            this.labelScale.Name = "labelScale";
            // 
            // ChangeMapFile
            // 
            this.AcceptButton = this.buttonOK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.Controls.Add(this.panelScaleDpi);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.errorDisplayPanel);
            this.Controls.Add(this.mapFileDisplay);
            this.Controls.Add(this.buttonChooseFile);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.HelpButton = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ChangeMapFile";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.ChangeMapFile_HelpButtonClicked);
            this.errorDisplayPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) (this.pictureBox1)).EndInit();
            this.mapFileDisplay.ResumeLayout(false);
            this.mapFileDisplay.PerformLayout();
            this.panelScaleDpi.ResumeLayout(false);
            this.panelScaleDpi.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel errorDisplayPanel;
        private System.Windows.Forms.Label errorMessage;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.GroupBox mapFileDisplay;
        public System.Windows.Forms.TextBox mapFileNameTextBox;
        private System.Windows.Forms.Button buttonChooseFile;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Panel panelScaleDpi;
        private System.Windows.Forms.Label labelDpi2;
        private System.Windows.Forms.TextBox textBoxDpi;
        private System.Windows.Forms.Label labelDpi;
        private System.Windows.Forms.TextBox textBoxScale;
        private System.Windows.Forms.Label labelScale;
    }
}