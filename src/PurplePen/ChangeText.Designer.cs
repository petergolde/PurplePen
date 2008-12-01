namespace PurplePen
{
    partial class ChangeText
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChangeText));
            this.textBoxMain = new System.Windows.Forms.TextBox();
            this.insertSpecialButton = new System.Windows.Forms.Button();
            this.specialTextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.eventTitleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.courseNameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.courseLengthMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.courseClimbMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.classListMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.usageLabel = new System.Windows.Forms.Label();
            this.specialTextMenu.SuspendLayout();
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
            // textBoxMain
            // 
            resources.ApplyResources(this.textBoxMain, "textBoxMain");
            this.textBoxMain.Name = "textBoxMain";
            this.textBoxMain.TextChanged += new System.EventHandler(this.textBoxMain_TextChanged);
            // 
            // insertSpecialButton
            // 
            resources.ApplyResources(this.insertSpecialButton, "insertSpecialButton");
            this.insertSpecialButton.Name = "insertSpecialButton";
            this.insertSpecialButton.UseVisualStyleBackColor = true;
            this.insertSpecialButton.Click += new System.EventHandler(this.insertSpecialButton_Click);
            // 
            // specialTextMenu
            // 
            this.specialTextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.eventTitleMenuItem,
            this.courseNameMenuItem,
            this.courseLengthMenuItem,
            this.courseClimbMenuItem,
            this.classListMenuItem});
            this.specialTextMenu.Name = "specialTextMenu";
            this.specialTextMenu.ShowImageMargin = false;
            resources.ApplyResources(this.specialTextMenu, "specialTextMenu");
            // 
            // eventTitleMenuItem
            // 
            this.eventTitleMenuItem.Name = "eventTitleMenuItem";
            resources.ApplyResources(this.eventTitleMenuItem, "eventTitleMenuItem");
            this.eventTitleMenuItem.Click += new System.EventHandler(this.eventTitleMenuItem_Click);
            // 
            // courseNameMenuItem
            // 
            this.courseNameMenuItem.Name = "courseNameMenuItem";
            resources.ApplyResources(this.courseNameMenuItem, "courseNameMenuItem");
            this.courseNameMenuItem.Click += new System.EventHandler(this.courseNameMenuItem_Click);
            // 
            // courseLengthMenuItem
            // 
            this.courseLengthMenuItem.Name = "courseLengthMenuItem";
            resources.ApplyResources(this.courseLengthMenuItem, "courseLengthMenuItem");
            this.courseLengthMenuItem.Click += new System.EventHandler(this.courseLengthMenuItem_Click);
            // 
            // courseClimbMenuItem
            // 
            this.courseClimbMenuItem.Name = "courseClimbMenuItem";
            resources.ApplyResources(this.courseClimbMenuItem, "courseClimbMenuItem");
            this.courseClimbMenuItem.Click += new System.EventHandler(this.courseClimbMenuItem_Click);
            // 
            // classListMenuItem
            // 
            this.classListMenuItem.Name = "classListMenuItem";
            resources.ApplyResources(this.classListMenuItem, "classListMenuItem");
            this.classListMenuItem.Click += new System.EventHandler(this.classListMenuItem_Click);
            // 
            // usageLabel
            // 
            resources.ApplyResources(this.usageLabel, "usageLabel");
            this.usageLabel.Name = "usageLabel";
            // 
            // ChangeText
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.usageLabel);
            this.Controls.Add(this.insertSpecialButton);
            this.Controls.Add(this.textBoxMain);
            this.Name = "ChangeText";
            this.Controls.SetChildIndex(this.textBoxMain, 0);
            this.Controls.SetChildIndex(this.insertSpecialButton, 0);
            this.Controls.SetChildIndex(this.usageLabel, 0);
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.specialTextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxMain;
        private System.Windows.Forms.Button insertSpecialButton;
        private System.Windows.Forms.ContextMenuStrip specialTextMenu;
        private System.Windows.Forms.ToolStripMenuItem eventTitleMenuItem;
        private System.Windows.Forms.ToolStripMenuItem courseNameMenuItem;
        private System.Windows.Forms.ToolStripMenuItem courseLengthMenuItem;
        private System.Windows.Forms.ToolStripMenuItem courseClimbMenuItem;
        private System.Windows.Forms.ToolStripMenuItem classListMenuItem;
        private System.Windows.Forms.Label usageLabel;
    }
}