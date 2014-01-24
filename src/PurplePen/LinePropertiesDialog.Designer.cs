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
            this.label1 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.pictureBoxPreview = new System.Windows.Forms.PictureBox();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownGapSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownDashSize)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).BeginInit();
            this.SuspendLayout();
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(165, 303);
            this.okButton.TabIndex = 2;
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(261, 303);
            this.cancelButton.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(23, 7);
            this.label1.Margin = new System.Windows.Forms.Padding(3, 7, 3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(39, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Color:";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.buttonChangeColor, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxColor, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxStyle, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelWidth, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.labelGapSize, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.labelDashSize, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.upDownWidth, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.upDownGapSize, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.upDownDashSize, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.labelWidthMm, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.labelGapSizeMm, 2, 3);
            this.tableLayoutPanel1.Controls.Add(this.labelDashSizeMm, 2, 4);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(12, 12);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(332, 166);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // buttonChangeColor
            // 
            this.buttonChangeColor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonChangeColor.AutoSize = true;
            this.buttonChangeColor.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonChangeColor.Location = new System.Drawing.Point(227, 3);
            this.buttonChangeColor.Name = "buttonChangeColor";
            this.buttonChangeColor.Size = new System.Drawing.Size(102, 25);
            this.buttonChangeColor.TabIndex = 2;
            this.buttonChangeColor.Text = " Change Color...";
            this.buttonChangeColor.UseVisualStyleBackColor = true;
            // 
            // comboBoxColor
            // 
            this.comboBoxColor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.comboBoxColor, 2);
            this.comboBoxColor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxColor.FormattingEnabled = true;
            this.comboBoxColor.Location = new System.Drawing.Point(68, 3);
            this.comboBoxColor.Margin = new System.Windows.Forms.Padding(3, 3, 15, 3);
            this.comboBoxColor.Name = "comboBoxColor";
            this.comboBoxColor.Size = new System.Drawing.Size(141, 23);
            this.comboBoxColor.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(27, 40);
            this.label2.Margin = new System.Windows.Forms.Padding(3, 7, 3, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 15);
            this.label2.TabIndex = 3;
            this.label2.Text = "Style:";
            // 
            // comboBoxStyle
            // 
            this.comboBoxStyle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.comboBoxStyle, 2);
            this.comboBoxStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxStyle.FormattingEnabled = true;
            this.comboBoxStyle.Items.AddRange(new object[] {
            "Single",
            "Double",
            "Dashed"});
            this.comboBoxStyle.Location = new System.Drawing.Point(68, 36);
            this.comboBoxStyle.Margin = new System.Windows.Forms.Padding(3, 3, 15, 3);
            this.comboBoxStyle.Name = "comboBoxStyle";
            this.comboBoxStyle.Size = new System.Drawing.Size(141, 23);
            this.comboBoxStyle.TabIndex = 4;
            this.comboBoxStyle.SelectedIndexChanged += new System.EventHandler(this.comboBoxStyle_SelectedIndexChanged);
            // 
            // labelWidth
            // 
            this.labelWidth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWidth.AutoSize = true;
            this.labelWidth.Location = new System.Drawing.Point(20, 70);
            this.labelWidth.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this.labelWidth.Name = "labelWidth";
            this.labelWidth.Size = new System.Drawing.Size(42, 15);
            this.labelWidth.TabIndex = 5;
            this.labelWidth.Text = "Width:";
            // 
            // labelGapSize
            // 
            this.labelGapSize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelGapSize.AutoSize = true;
            this.labelGapSize.Location = new System.Drawing.Point(8, 103);
            this.labelGapSize.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this.labelGapSize.Name = "labelGapSize";
            this.labelGapSize.Size = new System.Drawing.Size(54, 15);
            this.labelGapSize.TabIndex = 8;
            this.labelGapSize.Text = "Gap Size:";
            // 
            // labelDashSize
            // 
            this.labelDashSize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelDashSize.AutoSize = true;
            this.labelDashSize.Location = new System.Drawing.Point(3, 136);
            this.labelDashSize.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this.labelDashSize.Name = "labelDashSize";
            this.labelDashSize.Size = new System.Drawing.Size(59, 15);
            this.labelDashSize.TabIndex = 11;
            this.labelDashSize.Text = "Dash Size:";
            // 
            // upDownWidth
            // 
            this.upDownWidth.DecimalPlaces = 2;
            this.upDownWidth.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.upDownWidth.Location = new System.Drawing.Point(68, 69);
            this.upDownWidth.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.upDownWidth.Name = "upDownWidth";
            this.upDownWidth.Size = new System.Drawing.Size(118, 23);
            this.upDownWidth.TabIndex = 6;
            this.upDownWidth.Value = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.upDownWidth.ValueChanged += new System.EventHandler(this.upDown_ValueChanged);
            // 
            // upDownGapSize
            // 
            this.upDownGapSize.DecimalPlaces = 2;
            this.upDownGapSize.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.upDownGapSize.Location = new System.Drawing.Point(68, 102);
            this.upDownGapSize.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.upDownGapSize.Name = "upDownGapSize";
            this.upDownGapSize.Size = new System.Drawing.Size(118, 23);
            this.upDownGapSize.TabIndex = 9;
            this.upDownGapSize.Value = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.upDownGapSize.ValueChanged += new System.EventHandler(this.upDown_ValueChanged);
            // 
            // upDownDashSize
            // 
            this.upDownDashSize.DecimalPlaces = 2;
            this.upDownDashSize.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.upDownDashSize.Location = new System.Drawing.Point(68, 135);
            this.upDownDashSize.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.upDownDashSize.Name = "upDownDashSize";
            this.upDownDashSize.Size = new System.Drawing.Size(118, 23);
            this.upDownDashSize.TabIndex = 12;
            this.upDownDashSize.Value = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.upDownDashSize.ValueChanged += new System.EventHandler(this.upDown_ValueChanged);
            // 
            // labelWidthMm
            // 
            this.labelWidthMm.AutoSize = true;
            this.labelWidthMm.Location = new System.Drawing.Point(192, 70);
            this.labelWidthMm.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this.labelWidthMm.Name = "labelWidthMm";
            this.labelWidthMm.Size = new System.Drawing.Size(29, 15);
            this.labelWidthMm.TabIndex = 7;
            this.labelWidthMm.Text = "mm";
            // 
            // labelGapSizeMm
            // 
            this.labelGapSizeMm.AutoSize = true;
            this.labelGapSizeMm.Location = new System.Drawing.Point(192, 103);
            this.labelGapSizeMm.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this.labelGapSizeMm.Name = "labelGapSizeMm";
            this.labelGapSizeMm.Size = new System.Drawing.Size(29, 15);
            this.labelGapSizeMm.TabIndex = 10;
            this.labelGapSizeMm.Text = "mm";
            // 
            // labelDashSizeMm
            // 
            this.labelDashSizeMm.AutoSize = true;
            this.labelDashSizeMm.Location = new System.Drawing.Point(192, 136);
            this.labelDashSizeMm.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this.labelDashSizeMm.Name = "labelDashSizeMm";
            this.labelDashSizeMm.Size = new System.Drawing.Size(29, 15);
            this.labelDashSizeMm.TabIndex = 13;
            this.labelDashSizeMm.Text = "mm";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.pictureBoxPreview);
            this.groupBox1.Location = new System.Drawing.Point(13, 186);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(331, 111);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Sample";
            // 
            // pictureBoxPreview
            // 
            this.pictureBoxPreview.BackColor = System.Drawing.Color.White;
            this.pictureBoxPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBoxPreview.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.pictureBoxPreview.Location = new System.Drawing.Point(3, 19);
            this.pictureBoxPreview.Name = "pictureBoxPreview";
            this.pictureBoxPreview.Size = new System.Drawing.Size(325, 89);
            this.pictureBoxPreview.TabIndex = 6;
            this.pictureBoxPreview.TabStop = false;
            this.pictureBoxPreview.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBoxPreview_Paint);
            // 
            // LinePropertiesDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(360, 337);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "LinePropertiesDialog";
            this.Text = "Line Properties";
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.Controls.SetChildIndex(this.tableLayoutPanel1, 0);
            this.Controls.SetChildIndex(this.groupBox1, 0);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownGapSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownDashSize)).EndInit();
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
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
    }
}