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
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.panelScaleDpi = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.labelDpi = new System.Windows.Forms.Label();
            this.labelScale = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.labelOneTo = new System.Windows.Forms.Label();
            this.textBoxScale = new System.Windows.Forms.TextBox();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.textBoxDpi = new System.Windows.Forms.TextBox();
            this.labelDpi2 = new System.Windows.Forms.Label();
            this.errorDisplayPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.pictureBox1)).BeginInit();
            this.mapFileDisplay.SuspendLayout();
            this.panelScaleDpi.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
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
            // errorDisplayPanel
            // 
            this.errorDisplayPanel.BackColor = System.Drawing.SystemColors.Control;
            this.errorDisplayPanel.Controls.Add(this.errorMessage);
            this.errorDisplayPanel.Controls.Add(this.pictureBox1);
            this.errorDisplayPanel.Cursor = System.Windows.Forms.Cursors.Default;
            resources.ApplyResources(this.errorDisplayPanel, "errorDisplayPanel");
            this.errorDisplayPanel.Name = "errorDisplayPanel";
            // 
            // errorMessage
            // 
            this.errorMessage.ForeColor = System.Drawing.Color.Red;
            resources.ApplyResources(this.errorMessage, "errorMessage");
            this.errorMessage.Name = "errorMessage";
            // 
            // pictureBox1
            // 
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
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
            // openFileDialog
            // 
            this.openFileDialog.DefaultExt = "ocd";
            resources.ApplyResources(this.openFileDialog, "openFileDialog");
            // 
            // panelScaleDpi
            // 
            this.panelScaleDpi.BackColor = System.Drawing.SystemColors.Control;
            this.panelScaleDpi.Controls.Add(this.tableLayoutPanel1);
            resources.ApplyResources(this.panelScaleDpi, "panelScaleDpi");
            this.panelScaleDpi.Name = "panelScaleDpi";
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.labelDpi, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelScale, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel2, 1, 1);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // labelDpi
            // 
            resources.ApplyResources(this.labelDpi, "labelDpi");
            this.labelDpi.Name = "labelDpi";
            // 
            // labelScale
            // 
            resources.ApplyResources(this.labelScale, "labelScale");
            this.labelScale.Name = "labelScale";
            // 
            // flowLayoutPanel1
            // 
            resources.ApplyResources(this.flowLayoutPanel1, "flowLayoutPanel1");
            this.flowLayoutPanel1.Controls.Add(this.labelOneTo);
            this.flowLayoutPanel1.Controls.Add(this.textBoxScale);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            // 
            // labelOneTo
            // 
            resources.ApplyResources(this.labelOneTo, "labelOneTo");
            this.labelOneTo.Name = "labelOneTo";
            // 
            // textBoxScale
            // 
            resources.ApplyResources(this.textBoxScale, "textBoxScale");
            this.textBoxScale.Name = "textBoxScale";
            this.textBoxScale.TextChanged += new System.EventHandler(this.textBoxScale_TextChanged);
            // 
            // flowLayoutPanel2
            // 
            resources.ApplyResources(this.flowLayoutPanel2, "flowLayoutPanel2");
            this.flowLayoutPanel2.Controls.Add(this.textBoxDpi);
            this.flowLayoutPanel2.Controls.Add(this.labelDpi2);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            // 
            // textBoxDpi
            // 
            resources.ApplyResources(this.textBoxDpi, "textBoxDpi");
            this.textBoxDpi.Name = "textBoxDpi";
            this.textBoxDpi.TextChanged += new System.EventHandler(this.textBoxDpi_TextChanged);
            // 
            // labelDpi2
            // 
            resources.ApplyResources(this.labelDpi2, "labelDpi2");
            this.labelDpi2.Name = "labelDpi2";
            // 
            // ChangeMapFile
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.panelScaleDpi);
            this.Controls.Add(this.errorDisplayPanel);
            this.Controls.Add(this.mapFileDisplay);
            this.Controls.Add(this.buttonChooseFile);
            this.HelpTopic = "EventMapFile.htm";
            this.Name = "ChangeMapFile";
            this.Controls.SetChildIndex(this.buttonChooseFile, 0);
            this.Controls.SetChildIndex(this.mapFileDisplay, 0);
            this.Controls.SetChildIndex(this.errorDisplayPanel, 0);
            this.Controls.SetChildIndex(this.panelScaleDpi, 0);
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.errorDisplayPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) (this.pictureBox1)).EndInit();
            this.mapFileDisplay.ResumeLayout(false);
            this.mapFileDisplay.PerformLayout();
            this.panelScaleDpi.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel errorDisplayPanel;
        private System.Windows.Forms.Label errorMessage;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.GroupBox mapFileDisplay;
        public System.Windows.Forms.TextBox mapFileNameTextBox;
        private System.Windows.Forms.Button buttonChooseFile;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Panel panelScaleDpi;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label labelDpi;
        private System.Windows.Forms.Label labelScale;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Label labelOneTo;
        private System.Windows.Forms.TextBox textBoxScale;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.TextBox textBoxDpi;
        private System.Windows.Forms.Label labelDpi2;
    }
}