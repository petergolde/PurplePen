namespace PurplePen
{
    partial class MissingFonts
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MissingFonts));
            this.checkBoxDontWarnAgain = new System.Windows.Forms.CheckBox();
            this.okButton = new System.Windows.Forms.Button();
            this.iconPictureBox = new System.Windows.Forms.PictureBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.listBoxFonts = new System.Windows.Forms.ListBox();
            this.labelWarning = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize) (this.iconPictureBox)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // checkBoxDontWarnAgain
            // 
            resources.ApplyResources(this.checkBoxDontWarnAgain, "checkBoxDontWarnAgain");
            this.checkBoxDontWarnAgain.Name = "checkBoxDontWarnAgain";
            this.checkBoxDontWarnAgain.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Name = "okButton";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // iconPictureBox
            // 
            resources.ApplyResources(this.iconPictureBox, "iconPictureBox");
            this.iconPictureBox.Name = "iconPictureBox";
            this.iconPictureBox.TabStop = false;
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.listBoxFonts, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelWarning, 0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // listBoxFonts
            // 
            resources.ApplyResources(this.listBoxFonts, "listBoxFonts");
            this.listBoxFonts.FormattingEnabled = true;
            this.listBoxFonts.Name = "listBoxFonts";
            this.listBoxFonts.Sorted = true;
            // 
            // labelWarning
            // 
            resources.ApplyResources(this.labelWarning, "labelWarning");
            this.labelWarning.MinimumSize = new System.Drawing.Size(375, 60);
            this.labelWarning.Name = "labelWarning";
            // 
            // MissingFonts
            // 
            this.AcceptButton = this.okButton;
            this.AllowDrop = true;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.iconPictureBox);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.checkBoxDontWarnAgain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.HelpTopic = "MissingFontsDialog.htm";
            this.Name = "MissingFonts";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            ((System.ComponentModel.ISupportInitialize) (this.iconPictureBox)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBoxDontWarnAgain;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.PictureBox iconPictureBox;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ListBox listBoxFonts;
        private System.Windows.Forms.Label labelWarning;

    }
}