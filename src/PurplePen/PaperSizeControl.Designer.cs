namespace PurplePen
{
    partial class PaperSizeControl
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
            if (disposing && (components != null))
            {
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.checkBoxPortrait = new System.Windows.Forms.CheckBox();
            this.checkBoxLandscape = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.labelPaper = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBoxPaperSize = new System.Windows.Forms.ComboBox();
            this.upDownHeight = new System.Windows.Forms.NumericUpDown();
            this.upDownWidth = new System.Windows.Forms.NumericUpDown();
            this.upDownMargin = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.labelUnitsWidth = new System.Windows.Forms.Label();
            this.labelUnitsHeight = new System.Windows.Forms.Label();
            this.labelUnitsMargin = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownMargin)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel1.ColumnCount = 6;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.checkBoxPortrait, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.checkBoxLandscape, 3, 3);
            this.tableLayoutPanel1.Controls.Add(this.label7, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.labelPaper, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxPaperSize, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.upDownHeight, 4, 1);
            this.tableLayoutPanel1.Controls.Add(this.upDownWidth, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.upDownMargin, 4, 2);
            this.tableLayoutPanel1.Controls.Add(this.label3, 3, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelUnitsWidth, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelUnitsHeight, 5, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelUnitsMargin, 5, 2);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 4;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(254, 149);
            this.tableLayoutPanel1.TabIndex = 4;
            // 
            // checkBoxPortrait
            // 
            this.checkBoxPortrait.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxPortrait.Appearance = System.Windows.Forms.Appearance.Button;
            this.tableLayoutPanel1.SetColumnSpan(this.checkBoxPortrait, 2);
            this.checkBoxPortrait.Image = global::PurplePen.Properties.Resources.Portrait;
            this.checkBoxPortrait.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.checkBoxPortrait.Location = new System.Drawing.Point(47, 86);
            this.checkBoxPortrait.Name = "checkBoxPortrait";
            this.checkBoxPortrait.Size = new System.Drawing.Size(80, 60);
            this.checkBoxPortrait.TabIndex = 10;
            this.checkBoxPortrait.Text = "Portrait";
            this.checkBoxPortrait.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.checkBoxPortrait.UseVisualStyleBackColor = true;
            this.checkBoxPortrait.CheckedChanged += new System.EventHandler(this.checkBoxPortrait_CheckedChanged);
            // 
            // checkBoxLandscape
            // 
            this.checkBoxLandscape.Appearance = System.Windows.Forms.Appearance.Button;
            this.tableLayoutPanel1.SetColumnSpan(this.checkBoxLandscape, 2);
            this.checkBoxLandscape.Image = global::PurplePen.Properties.Resources.Landscape;
            this.checkBoxLandscape.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.checkBoxLandscape.Location = new System.Drawing.Point(133, 86);
            this.checkBoxLandscape.Name = "checkBoxLandscape";
            this.checkBoxLandscape.Size = new System.Drawing.Size(80, 60);
            this.checkBoxLandscape.TabIndex = 9;
            this.checkBoxLandscape.Text = "Landscape";
            this.checkBoxLandscape.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.checkBoxLandscape.UseVisualStyleBackColor = true;
            this.checkBoxLandscape.CheckedChanged += new System.EventHandler(this.checkBoxLandscape_CheckedChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.label7, 4);
            this.label7.Dock = System.Windows.Forms.DockStyle.Right;
            this.label7.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label7.Location = new System.Drawing.Point(86, 61);
            this.label7.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(88, 22);
            this.label7.TabIndex = 7;
            this.label7.Text = "Margin (all sides):";
            // 
            // labelPaper
            // 
            this.labelPaper.AutoSize = true;
            this.labelPaper.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.labelPaper.Location = new System.Drawing.Point(3, 6);
            this.labelPaper.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
            this.labelPaper.Name = "labelPaper";
            this.labelPaper.Size = new System.Drawing.Size(30, 13);
            this.labelPaper.TabIndex = 0;
            this.labelPaper.Text = "Size:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label2.Location = new System.Drawing.Point(3, 33);
            this.label2.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Width:";
            // 
            // comboBoxPaperSize
            // 
            this.comboBoxPaperSize.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.SetColumnSpan(this.comboBoxPaperSize, 5);
            this.comboBoxPaperSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPaperSize.FormattingEnabled = true;
            this.comboBoxPaperSize.Location = new System.Drawing.Point(47, 3);
            this.comboBoxPaperSize.Name = "comboBoxPaperSize";
            this.comboBoxPaperSize.Size = new System.Drawing.Size(204, 21);
            this.comboBoxPaperSize.TabIndex = 1;
            this.comboBoxPaperSize.SelectedIndexChanged += new System.EventHandler(this.comboBoxPaperSize_SelectedIndexChanged);
            // 
            // upDownHeight
            // 
            this.upDownHeight.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.upDownHeight.Location = new System.Drawing.Point(180, 32);
            this.upDownHeight.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.upDownHeight.Name = "upDownHeight";
            this.upDownHeight.Size = new System.Drawing.Size(50, 20);
            this.upDownHeight.TabIndex = 5;
            // 
            // upDownWidth
            // 
            this.upDownWidth.Location = new System.Drawing.Point(47, 32);
            this.upDownWidth.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.upDownWidth.Name = "upDownWidth";
            this.upDownWidth.Size = new System.Drawing.Size(50, 20);
            this.upDownWidth.TabIndex = 3;
            // 
            // upDownMargin
            // 
            this.upDownMargin.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.upDownMargin.Location = new System.Drawing.Point(180, 60);
            this.upDownMargin.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.upDownMargin.Name = "upDownMargin";
            this.upDownMargin.Size = new System.Drawing.Size(50, 20);
            this.upDownMargin.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label3.Location = new System.Drawing.Point(133, 33);
            this.label3.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(41, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Height:";
            // 
            // labelUnitsWidth
            // 
            this.labelUnitsWidth.AutoSize = true;
            this.labelUnitsWidth.Location = new System.Drawing.Point(103, 33);
            this.labelUnitsWidth.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
            this.labelUnitsWidth.Name = "labelUnitsWidth";
            this.labelUnitsWidth.Size = new System.Drawing.Size(15, 13);
            this.labelUnitsWidth.TabIndex = 11;
            this.labelUnitsWidth.Text = "in";
            // 
            // labelUnitsHeight
            // 
            this.labelUnitsHeight.AutoSize = true;
            this.labelUnitsHeight.Location = new System.Drawing.Point(236, 33);
            this.labelUnitsHeight.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
            this.labelUnitsHeight.Name = "labelUnitsHeight";
            this.labelUnitsHeight.Size = new System.Drawing.Size(15, 13);
            this.labelUnitsHeight.TabIndex = 12;
            this.labelUnitsHeight.Text = "in";
            // 
            // labelUnitsMargin
            // 
            this.labelUnitsMargin.AutoSize = true;
            this.labelUnitsMargin.Location = new System.Drawing.Point(236, 61);
            this.labelUnitsMargin.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
            this.labelUnitsMargin.Name = "labelUnitsMargin";
            this.labelUnitsMargin.Size = new System.Drawing.Size(15, 13);
            this.labelUnitsMargin.TabIndex = 13;
            this.labelUnitsMargin.Text = "in";
            // 
            // PaperSizeControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "PaperSizeControl";
            this.Size = new System.Drawing.Size(260, 155);
            this.Load += new System.EventHandler(this.PaperSizeControl_Loaded);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownMargin)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.CheckBox checkBoxPortrait;
        private System.Windows.Forms.CheckBox checkBoxLandscape;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label labelPaper;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBoxPaperSize;
        private System.Windows.Forms.NumericUpDown upDownHeight;
        private System.Windows.Forms.NumericUpDown upDownWidth;
        private System.Windows.Forms.NumericUpDown upDownMargin;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label labelUnitsWidth;
        private System.Windows.Forms.Label labelUnitsHeight;
        private System.Windows.Forms.Label labelUnitsMargin;
    }
}
