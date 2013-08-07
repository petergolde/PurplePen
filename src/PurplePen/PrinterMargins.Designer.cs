namespace PurplePen
{
    partial class PrinterMargins
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PrinterMargins));
            this.groupBoxPaperSize = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.labelPaper = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBoxPaperSize = new System.Windows.Forms.ComboBox();
            this.upDownHeight = new System.Windows.Forms.NumericUpDown();
            this.upDownWidth = new System.Windows.Forms.NumericUpDown();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.radioButtonLandscape = new System.Windows.Forms.RadioButton();
            this.radioButtonPortrait = new System.Windows.Forms.RadioButton();
            this.groupBoxMargins = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.upDownLeft = new System.Windows.Forms.NumericUpDown();
            this.upDownRight = new System.Windows.Forms.NumericUpDown();
            this.upDownTop = new System.Windows.Forms.NumericUpDown();
            this.upDownBottom = new System.Windows.Forms.NumericUpDown();
            this.groupBoxPaperSize.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownWidth)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBoxMargins.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownLeft)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownRight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownTop)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownBottom)).BeginInit();
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
            // groupBoxPaperSize
            // 
            resources.ApplyResources(this.groupBoxPaperSize, "groupBoxPaperSize");
            this.groupBoxPaperSize.Controls.Add(this.tableLayoutPanel1);
            this.groupBoxPaperSize.Name = "groupBoxPaperSize";
            this.groupBoxPaperSize.TabStop = false;
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.labelPaper, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label3, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.comboBoxPaperSize, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.upDownHeight, 3, 1);
            this.tableLayoutPanel1.Controls.Add(this.upDownWidth, 1, 1);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // labelPaper
            // 
            resources.ApplyResources(this.labelPaper, "labelPaper");
            this.labelPaper.Name = "labelPaper";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // comboBoxPaperSize
            // 
            resources.ApplyResources(this.comboBoxPaperSize, "comboBoxPaperSize");
            this.tableLayoutPanel1.SetColumnSpan(this.comboBoxPaperSize, 3);
            this.comboBoxPaperSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPaperSize.FormattingEnabled = true;
            this.comboBoxPaperSize.Name = "comboBoxPaperSize";
            this.comboBoxPaperSize.SelectedIndexChanged += new System.EventHandler(this.comboBoxPaperSize_SelectedIndexChanged);
            // 
            // upDownHeight
            // 
            resources.ApplyResources(this.upDownHeight, "upDownHeight");
            this.upDownHeight.Name = "upDownHeight";
            // 
            // upDownWidth
            // 
            resources.ApplyResources(this.upDownWidth, "upDownWidth");
            this.upDownWidth.Name = "upDownWidth";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioButtonLandscape);
            this.groupBox1.Controls.Add(this.radioButtonPortrait);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // radioButtonLandscape
            // 
            resources.ApplyResources(this.radioButtonLandscape, "radioButtonLandscape");
            this.radioButtonLandscape.Name = "radioButtonLandscape";
            this.radioButtonLandscape.TabStop = true;
            this.radioButtonLandscape.UseVisualStyleBackColor = true;
            // 
            // radioButtonPortrait
            // 
            resources.ApplyResources(this.radioButtonPortrait, "radioButtonPortrait");
            this.radioButtonPortrait.Name = "radioButtonPortrait";
            this.radioButtonPortrait.TabStop = true;
            this.radioButtonPortrait.UseVisualStyleBackColor = true;
            // 
            // groupBoxMargins
            // 
            resources.ApplyResources(this.groupBoxMargins, "groupBoxMargins");
            this.groupBoxMargins.Controls.Add(this.tableLayoutPanel2);
            this.groupBoxMargins.Name = "groupBoxMargins";
            this.groupBoxMargins.TabStop = false;
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.label4, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.label5, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.label6, 2, 1);
            this.tableLayoutPanel2.Controls.Add(this.upDownLeft, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.upDownRight, 3, 0);
            this.tableLayoutPanel2.Controls.Add(this.upDownTop, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.upDownBottom, 3, 1);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // upDownLeft
            // 
            resources.ApplyResources(this.upDownLeft, "upDownLeft");
            this.upDownLeft.Name = "upDownLeft";
            // 
            // upDownRight
            // 
            resources.ApplyResources(this.upDownRight, "upDownRight");
            this.upDownRight.Name = "upDownRight";
            // 
            // upDownTop
            // 
            resources.ApplyResources(this.upDownTop, "upDownTop");
            this.upDownTop.Name = "upDownTop";
            // 
            // upDownBottom
            // 
            resources.ApplyResources(this.upDownBottom, "upDownBottom");
            this.upDownBottom.Name = "upDownBottom";
            // 
            // PrinterMargins
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.groupBoxMargins);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBoxPaperSize);
            this.Name = "PrinterMargins";
            this.Shown += new System.EventHandler(this.PrinterMargins_Shown);
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.Controls.SetChildIndex(this.groupBoxPaperSize, 0);
            this.Controls.SetChildIndex(this.groupBox1, 0);
            this.Controls.SetChildIndex(this.groupBoxMargins, 0);
            this.groupBoxPaperSize.ResumeLayout(false);
            this.groupBoxPaperSize.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownWidth)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBoxMargins.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownLeft)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownRight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownTop)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownBottom)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxPaperSize;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label labelPaper;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboBoxPaperSize;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton radioButtonLandscape;
        private System.Windows.Forms.RadioButton radioButtonPortrait;
        private System.Windows.Forms.GroupBox groupBoxMargins;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown upDownHeight;
        private System.Windows.Forms.NumericUpDown upDownWidth;
        private System.Windows.Forms.NumericUpDown upDownLeft;
        private System.Windows.Forms.NumericUpDown upDownRight;
        private System.Windows.Forms.NumericUpDown upDownTop;
        private System.Windows.Forms.NumericUpDown upDownBottom;
    }
}
