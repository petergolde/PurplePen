namespace PurplePen
{
    partial class SetUILanguage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetUILanguage));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.languageListBox = new System.Windows.Forms.ListBox();
            this.introLabel = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
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
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.languageListBox, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.introLabel, 0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // languageListBox
            // 
            resources.ApplyResources(this.languageListBox, "languageListBox");
            this.languageListBox.FormattingEnabled = true;
            this.languageListBox.Name = "languageListBox";
            this.languageListBox.Sorted = true;
            this.languageListBox.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.languageListBox_Format);
            // 
            // introLabel
            // 
            resources.ApplyResources(this.introLabel, "introLabel");
            this.introLabel.Name = "introLabel";
            // 
            // SetUILanguage
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.HelpTopic = "FileSetProgramLanguage.htm";
            this.Name = "SetUILanguage";
            this.Controls.SetChildIndex(this.tableLayoutPanel1, 0);
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ListBox languageListBox;
        private System.Windows.Forms.Label introLabel;

    }
}