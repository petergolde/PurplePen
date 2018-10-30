namespace PurplePen
{
    partial class LinePropertiesDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LinePropertiesDialog));
            this.label1 = new System.Windows.Forms.Label();
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.buttonChangeColor = new System.Windows.Forms.Button();
            this.comboBoxColor = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBoxStyle = new System.Windows.Forms.ComboBox();
            this.labelWidth = new System.Windows.Forms.Label();
            this.labelGapSize = new System.Windows.Forms.Label();
            this.labelDashSize = new System.Windows.Forms.Label();
            this.upDownWidth = new System.Windows.Forms.NumericUpDown();
            this.upDownGapSize = new System.Windows.Forms.NumericUpDown();
            this.upDownDashSize = new System.Windows.Forms.NumericUpDown();
            this.labelWidthMm = new System.Windows.Forms.Label();
            this.labelGapSizeMm = new System.Windows.Forms.Label();
            this.labelDashSizeMm = new System.Windows.Forms.Label();
            this.labelCornerRadius = new System.Windows.Forms.Label();
            this.upDownRadius = new System.Windows.Forms.NumericUpDown();
            this.labelRadiusMm = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.pictureBoxPreview = new System.Windows.Forms.PictureBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.usageLabel = new System.Windows.Forms.Label();
            this.tableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownGapSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownDashSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownRadius)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).BeginInit();
            this.groupBox2.SuspendLayout();
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
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // tableLayoutPanel
            // 
            resources.ApplyResources(this.tableLayoutPanel, "tableLayoutPanel");
            this.tableLayoutPanel.Controls.Add(this.buttonChangeColor, 3, 0);
            this.tableLayoutPanel.Controls.Add(this.comboBoxColor, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel.Controls.Add(this.comboBoxStyle, 1, 1);
            this.tableLayoutPanel.Controls.Add(this.labelWidth, 0, 3);
            this.tableLayoutPanel.Controls.Add(this.labelGapSize, 0, 4);
            this.tableLayoutPanel.Controls.Add(this.labelDashSize, 0, 5);
            this.tableLayoutPanel.Controls.Add(this.upDownWidth, 1, 3);
            this.tableLayoutPanel.Controls.Add(this.upDownGapSize, 1, 4);
            this.tableLayoutPanel.Controls.Add(this.upDownDashSize, 1, 5);
            this.tableLayoutPanel.Controls.Add(this.labelWidthMm, 2, 3);
            this.tableLayoutPanel.Controls.Add(this.labelGapSizeMm, 2, 4);
            this.tableLayoutPanel.Controls.Add(this.labelDashSizeMm, 2, 5);
            this.tableLayoutPanel.Controls.Add(this.labelCornerRadius, 0, 2);
            this.tableLayoutPanel.Controls.Add(this.upDownRadius, 1, 2);
            this.tableLayoutPanel.Controls.Add(this.labelRadiusMm, 2, 2);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            // 
            // buttonChangeColor
            // 
            resources.ApplyResources(this.buttonChangeColor, "buttonChangeColor");
            this.buttonChangeColor.Name = "buttonChangeColor";
            this.buttonChangeColor.UseVisualStyleBackColor = true;
            // 
            // comboBoxColor
            // 
            resources.ApplyResources(this.comboBoxColor, "comboBoxColor");
            this.tableLayoutPanel.SetColumnSpan(this.comboBoxColor, 2);
            this.comboBoxColor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxColor.FormattingEnabled = true;
            this.comboBoxColor.Name = "comboBoxColor";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // comboBoxStyle
            // 
            resources.ApplyResources(this.comboBoxStyle, "comboBoxStyle");
            this.tableLayoutPanel.SetColumnSpan(this.comboBoxStyle, 2);
            this.comboBoxStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxStyle.FormattingEnabled = true;
            this.comboBoxStyle.Items.AddRange(new object[] {
            resources.GetString("comboBoxStyle.Items"),
            resources.GetString("comboBoxStyle.Items1"),
            resources.GetString("comboBoxStyle.Items2")});
            this.comboBoxStyle.Name = "comboBoxStyle";
            this.comboBoxStyle.SelectedIndexChanged += new System.EventHandler(this.comboBoxStyle_SelectedIndexChanged);
            // 
            // labelWidth
            // 
            resources.ApplyResources(this.labelWidth, "labelWidth");
            this.labelWidth.Name = "labelWidth";
            // 
            // labelGapSize
            // 
            resources.ApplyResources(this.labelGapSize, "labelGapSize");
            this.labelGapSize.Name = "labelGapSize";
            // 
            // labelDashSize
            // 
            resources.ApplyResources(this.labelDashSize, "labelDashSize");
            this.labelDashSize.Name = "labelDashSize";
            // 
            // upDownWidth
            // 
            resources.ApplyResources(this.upDownWidth, "upDownWidth");
            this.upDownWidth.DecimalPlaces = 2;
            this.upDownWidth.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.upDownWidth.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.upDownWidth.Name = "upDownWidth";
            this.upDownWidth.Value = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.upDownWidth.ValueChanged += new System.EventHandler(this.upDown_ValueChanged);
            // 
            // upDownGapSize
            // 
            resources.ApplyResources(this.upDownGapSize, "upDownGapSize");
            this.upDownGapSize.DecimalPlaces = 2;
            this.upDownGapSize.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.upDownGapSize.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.upDownGapSize.Name = "upDownGapSize";
            this.upDownGapSize.Value = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.upDownGapSize.ValueChanged += new System.EventHandler(this.upDown_ValueChanged);
            // 
            // upDownDashSize
            // 
            resources.ApplyResources(this.upDownDashSize, "upDownDashSize");
            this.upDownDashSize.DecimalPlaces = 2;
            this.upDownDashSize.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.upDownDashSize.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.upDownDashSize.Name = "upDownDashSize";
            this.upDownDashSize.Value = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.upDownDashSize.ValueChanged += new System.EventHandler(this.upDown_ValueChanged);
            // 
            // labelWidthMm
            // 
            resources.ApplyResources(this.labelWidthMm, "labelWidthMm");
            this.labelWidthMm.Name = "labelWidthMm";
            // 
            // labelGapSizeMm
            // 
            resources.ApplyResources(this.labelGapSizeMm, "labelGapSizeMm");
            this.labelGapSizeMm.Name = "labelGapSizeMm";
            // 
            // labelDashSizeMm
            // 
            resources.ApplyResources(this.labelDashSizeMm, "labelDashSizeMm");
            this.labelDashSizeMm.Name = "labelDashSizeMm";
            // 
            // labelCornerRadius
            // 
            resources.ApplyResources(this.labelCornerRadius, "labelCornerRadius");
            this.labelCornerRadius.Name = "labelCornerRadius";
            // 
            // upDownRadius
            // 
            resources.ApplyResources(this.upDownRadius, "upDownRadius");
            this.upDownRadius.DecimalPlaces = 2;
            this.upDownRadius.Increment = new decimal(new int[] {
            10,
            0,
            0,
            131072});
            this.upDownRadius.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.upDownRadius.Name = "upDownRadius";
            this.upDownRadius.ValueChanged += new System.EventHandler(this.upDown_ValueChanged);
            // 
            // labelRadiusMm
            // 
            resources.ApplyResources(this.labelRadiusMm, "labelRadiusMm");
            this.labelRadiusMm.Name = "labelRadiusMm";
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.pictureBoxPreview);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // pictureBoxPreview
            // 
            this.pictureBoxPreview.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.pictureBoxPreview, "pictureBoxPreview");
            this.pictureBoxPreview.Name = "pictureBoxPreview";
            this.pictureBoxPreview.TabStop = false;
            this.pictureBoxPreview.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBoxPreview_Paint);
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.tableLayoutPanel);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // usageLabel
            // 
            resources.ApplyResources(this.usageLabel, "usageLabel");
            this.usageLabel.Name = "usageLabel";
            // 
            // LinePropertiesDialog
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.usageLabel);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "LinePropertiesDialog";
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.Controls.SetChildIndex(this.groupBox1, 0);
            this.Controls.SetChildIndex(this.groupBox2, 0);
            this.Controls.SetChildIndex(this.usageLabel, 0);
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownGapSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownDashSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownRadius)).EndInit();
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.Button buttonChangeColor;
        private System.Windows.Forms.ComboBox comboBoxColor;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBoxStyle;
        private System.Windows.Forms.Label labelWidth;
        private System.Windows.Forms.Label labelGapSize;
        private System.Windows.Forms.Label labelDashSize;
        private System.Windows.Forms.NumericUpDown upDownWidth;
        private System.Windows.Forms.NumericUpDown upDownGapSize;
        private System.Windows.Forms.NumericUpDown upDownDashSize;
        private System.Windows.Forms.Label labelWidthMm;
        private System.Windows.Forms.Label labelGapSizeMm;
        private System.Windows.Forms.Label labelDashSizeMm;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.PictureBox pictureBoxPreview;
        private System.Windows.Forms.Label labelCornerRadius;
        private System.Windows.Forms.NumericUpDown upDownRadius;
        private System.Windows.Forms.Label labelRadiusMm;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label usageLabel;
    }
}