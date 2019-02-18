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
            this.specialTextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.eventTitleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.courseNameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.coursePartMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.variationMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.courseLengthMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.courseClimbMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.classListMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.printScaleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.relayTeamMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.relayLegMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.usageLabel = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.buttonChangeColor = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.checkBoxItalic = new System.Windows.Forms.CheckBox();
            this.comboBoxColor = new System.Windows.Forms.ComboBox();
            this.checkBoxBold = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.listBoxFonts = new System.Windows.Forms.ListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.upDownFontSize = new System.Windows.Forms.NumericUpDown();
            this.labelFontSizeMm = new System.Windows.Forms.Label();
            this.checkBoxAutoFontSize = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.pictureBoxPreview = new System.Windows.Forms.PictureBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.insertSpecialButton = new System.Windows.Forms.Button();
            this.textBoxMain = new System.Windows.Forms.TextBox();
            this.fileNameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mapFileNameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.specialTextMenu.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownFontSize)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).BeginInit();
            this.groupBox3.SuspendLayout();
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
            // specialTextMenu
            // 
            this.specialTextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.eventTitleMenuItem,
            this.courseNameMenuItem,
            this.coursePartMenuItem,
            this.variationMenuItem,
            this.courseLengthMenuItem,
            this.courseClimbMenuItem,
            this.classListMenuItem,
            this.printScaleMenuItem,
            this.relayTeamMenuItem,
            this.relayLegMenuItem,
            this.fileNameMenuItem,
            this.mapFileNameMenuItem});
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
            // coursePartMenuItem
            // 
            this.coursePartMenuItem.Name = "coursePartMenuItem";
            resources.ApplyResources(this.coursePartMenuItem, "coursePartMenuItem");
            this.coursePartMenuItem.Click += new System.EventHandler(this.coursePartMenuItem_Click);
            // 
            // variationMenuItem
            // 
            this.variationMenuItem.Name = "variationMenuItem";
            resources.ApplyResources(this.variationMenuItem, "variationMenuItem");
            this.variationMenuItem.Click += new System.EventHandler(this.variationMenuItem_Click);
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
            // printScaleMenuItem
            // 
            this.printScaleMenuItem.Name = "printScaleMenuItem";
            resources.ApplyResources(this.printScaleMenuItem, "printScaleMenuItem");
            this.printScaleMenuItem.Click += new System.EventHandler(this.printScaleMenuItem_Click);
            // 
            // relayTeamMenuItem
            // 
            this.relayTeamMenuItem.Name = "relayTeamMenuItem";
            resources.ApplyResources(this.relayTeamMenuItem, "relayTeamMenuItem");
            this.relayTeamMenuItem.Click += new System.EventHandler(this.relayTeamMenuItem_Click);
            // 
            // relayLegMenuItem
            // 
            this.relayLegMenuItem.Name = "relayLegMenuItem";
            resources.ApplyResources(this.relayLegMenuItem, "relayLegMenuItem");
            this.relayLegMenuItem.Click += new System.EventHandler(this.relayLegMenuItem_Click);
            // 
            // usageLabel
            // 
            resources.ApplyResources(this.usageLabel, "usageLabel");
            this.usageLabel.Name = "usageLabel";
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.tableLayoutPanel1);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.buttonChangeColor, 3, 2);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxItalic, 3, 1);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxColor, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxBold, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.listBoxFonts, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.upDownFontSize, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.labelFontSizeMm, 2, 3);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxAutoFontSize, 3, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // buttonChangeColor
            // 
            resources.ApplyResources(this.buttonChangeColor, "buttonChangeColor");
            this.buttonChangeColor.Name = "buttonChangeColor";
            this.buttonChangeColor.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // checkBoxItalic
            // 
            resources.ApplyResources(this.checkBoxItalic, "checkBoxItalic");
            this.checkBoxItalic.Name = "checkBoxItalic";
            this.checkBoxItalic.UseVisualStyleBackColor = true;
            this.checkBoxItalic.CheckedChanged += new System.EventHandler(this.checkBoxItalic_CheckedChanged);
            // 
            // comboBoxColor
            // 
            resources.ApplyResources(this.comboBoxColor, "comboBoxColor");
            this.tableLayoutPanel1.SetColumnSpan(this.comboBoxColor, 2);
            this.comboBoxColor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxColor.FormattingEnabled = true;
            this.comboBoxColor.Name = "comboBoxColor";
            // 
            // checkBoxBold
            // 
            resources.ApplyResources(this.checkBoxBold, "checkBoxBold");
            this.checkBoxBold.Name = "checkBoxBold";
            this.checkBoxBold.UseVisualStyleBackColor = true;
            this.checkBoxBold.CheckedChanged += new System.EventHandler(this.checkBoxBold_CheckedChanged);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // listBoxFonts
            // 
            resources.ApplyResources(this.listBoxFonts, "listBoxFonts");
            this.tableLayoutPanel1.SetColumnSpan(this.listBoxFonts, 2);
            this.listBoxFonts.FormattingEnabled = true;
            this.listBoxFonts.Name = "listBoxFonts";
            this.tableLayoutPanel1.SetRowSpan(this.listBoxFonts, 2);
            this.listBoxFonts.Sorted = true;
            this.listBoxFonts.SelectedIndexChanged += new System.EventHandler(this.listBoxFonts_SelectedIndexChanged);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // upDownFontSize
            // 
            resources.ApplyResources(this.upDownFontSize, "upDownFontSize");
            this.upDownFontSize.DecimalPlaces = 1;
            this.upDownFontSize.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.upDownFontSize.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.upDownFontSize.Name = "upDownFontSize";
            this.upDownFontSize.Value = new decimal(new int[] {
            50,
            0,
            0,
            65536});
            this.upDownFontSize.ValueChanged += new System.EventHandler(this.upDownFontSize_ValueChanged);
            // 
            // labelFontSizeMm
            // 
            resources.ApplyResources(this.labelFontSizeMm, "labelFontSizeMm");
            this.labelFontSizeMm.Name = "labelFontSizeMm";
            // 
            // checkBoxAutoFontSize
            // 
            resources.ApplyResources(this.checkBoxAutoFontSize, "checkBoxAutoFontSize");
            this.checkBoxAutoFontSize.Name = "checkBoxAutoFontSize";
            this.checkBoxAutoFontSize.UseVisualStyleBackColor = true;
            this.checkBoxAutoFontSize.CheckedChanged += new System.EventHandler(this.checkBoxAutoFontSize_CheckedChanged);
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.pictureBoxPreview);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // pictureBoxPreview
            // 
            this.pictureBoxPreview.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.pictureBoxPreview, "pictureBoxPreview");
            this.pictureBoxPreview.Name = "pictureBoxPreview";
            this.pictureBoxPreview.TabStop = false;
            this.pictureBoxPreview.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBoxPreview_Paint);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.insertSpecialButton);
            this.groupBox3.Controls.Add(this.textBoxMain);
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // insertSpecialButton
            // 
            resources.ApplyResources(this.insertSpecialButton, "insertSpecialButton");
            this.insertSpecialButton.Image = global::PurplePen.Properties.Resources.MenuDown;
            this.insertSpecialButton.Name = "insertSpecialButton";
            this.insertSpecialButton.UseVisualStyleBackColor = true;
            this.insertSpecialButton.Click += new System.EventHandler(this.insertSpecialButton_Click);
            // 
            // textBoxMain
            // 
            resources.ApplyResources(this.textBoxMain, "textBoxMain");
            this.textBoxMain.Name = "textBoxMain";
            this.textBoxMain.TextChanged += new System.EventHandler(this.textBoxMain_TextChanged);
            // 
            // fileNameMenuItem
            // 
            this.fileNameMenuItem.Name = "fileNameMenuItem";
            resources.ApplyResources(this.fileNameMenuItem, "fileNameMenuItem");
            this.fileNameMenuItem.Click += new System.EventHandler(this.fileNameMenuItem_Click);
            // 
            // mapFileNameMenuItem
            // 
            this.mapFileNameMenuItem.Name = "mapFileNameMenuItem";
            resources.ApplyResources(this.mapFileNameMenuItem, "mapFileNameMenuItem");
            this.mapFileNameMenuItem.Click += new System.EventHandler(this.mapFileNameMenuItem_Click);
            // 
            // ChangeText
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.usageLabel);
            this.Name = "ChangeText";
            this.Controls.SetChildIndex(this.usageLabel, 0);
            this.Controls.SetChildIndex(this.groupBox1, 0);
            this.Controls.SetChildIndex(this.groupBox2, 0);
            this.Controls.SetChildIndex(this.groupBox3, 0);
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.specialTextMenu.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownFontSize)).EndInit();
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip specialTextMenu;
        private System.Windows.Forms.ToolStripMenuItem eventTitleMenuItem;
        private System.Windows.Forms.ToolStripMenuItem courseNameMenuItem;
        private System.Windows.Forms.ToolStripMenuItem courseLengthMenuItem;
        private System.Windows.Forms.ToolStripMenuItem courseClimbMenuItem;
        private System.Windows.Forms.ToolStripMenuItem classListMenuItem;
        private System.Windows.Forms.Label usageLabel;
        private System.Windows.Forms.ToolStripMenuItem printScaleMenuItem;
        private System.Windows.Forms.ToolStripMenuItem coursePartMenuItem;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.PictureBox pictureBoxPreview;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button buttonChangeColor;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox checkBoxItalic;
        private System.Windows.Forms.ComboBox comboBoxColor;
        private System.Windows.Forms.CheckBox checkBoxBold;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox listBoxFonts;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Button insertSpecialButton;
        private System.Windows.Forms.TextBox textBoxMain;
        private System.Windows.Forms.ToolStripMenuItem variationMenuItem;
        private System.Windows.Forms.ToolStripMenuItem relayTeamMenuItem;
        private System.Windows.Forms.ToolStripMenuItem relayLegMenuItem;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown upDownFontSize;
        private System.Windows.Forms.Label labelFontSizeMm;
        private System.Windows.Forms.CheckBox checkBoxAutoFontSize;
        private System.Windows.Forms.ToolStripMenuItem fileNameMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mapFileNameMenuItem;
    }
}