namespace PurplePen
{
    partial class CourseAppearanceDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CourseAppearanceDialog));
            this.groupBoxSizes = new System.Windows.Forms.GroupBox();
            this.labelControlNumber = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.upDownNumberHeight = new System.Windows.Forms.NumericUpDown();
            this.labelLineWidth = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.upDownLineWidth = new System.Windows.Forms.NumericUpDown();
            this.labelControlCircle = new System.Windows.Forms.Label();
            this.labelMM1 = new System.Windows.Forms.Label();
            this.upDownControlCircle = new System.Windows.Forms.NumericUpDown();
            this.checkBoxStandardSizes = new System.Windows.Forms.CheckBox();
            this.groupBoxPurple = new System.Windows.Forms.GroupBox();
            this.buttonColorChoosers = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.upDownBlack = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.upDownYellow = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.upDownMagenta = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.upDownCyan = new System.Windows.Forms.NumericUpDown();
            this.checkBoxDefaultPurple = new System.Windows.Forms.CheckBox();
            this.colorDialog = new System.Windows.Forms.ColorDialog();
            this.groupBoxPreview = new System.Windows.Forms.GroupBox();
            this.pictureBoxPreview = new System.Windows.Forms.PictureBox();
            this.groupBoxSizes.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.upDownNumberHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.upDownLineWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.upDownControlCircle)).BeginInit();
            this.groupBoxPurple.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.upDownBlack)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.upDownYellow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.upDownMagenta)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.upDownCyan)).BeginInit();
            this.groupBoxPreview.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) (this.pictureBoxPreview)).BeginInit();
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
            // groupBoxSizes
            // 
            resources.ApplyResources(this.groupBoxSizes, "groupBoxSizes");
            this.groupBoxSizes.Controls.Add(this.labelControlNumber);
            this.groupBoxSizes.Controls.Add(this.label4);
            this.groupBoxSizes.Controls.Add(this.upDownNumberHeight);
            this.groupBoxSizes.Controls.Add(this.labelLineWidth);
            this.groupBoxSizes.Controls.Add(this.label2);
            this.groupBoxSizes.Controls.Add(this.upDownLineWidth);
            this.groupBoxSizes.Controls.Add(this.labelControlCircle);
            this.groupBoxSizes.Controls.Add(this.labelMM1);
            this.groupBoxSizes.Controls.Add(this.upDownControlCircle);
            this.groupBoxSizes.Controls.Add(this.checkBoxStandardSizes);
            this.groupBoxSizes.Name = "groupBoxSizes";
            this.groupBoxSizes.TabStop = false;
            // 
            // labelControlNumber
            // 
            resources.ApplyResources(this.labelControlNumber, "labelControlNumber");
            this.labelControlNumber.Name = "labelControlNumber";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // upDownNumberHeight
            // 
            this.upDownNumberHeight.DecimalPlaces = 2;
            this.upDownNumberHeight.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            resources.ApplyResources(this.upDownNumberHeight, "upDownNumberHeight");
            this.upDownNumberHeight.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            65536});
            this.upDownNumberHeight.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            65536});
            this.upDownNumberHeight.Name = "upDownNumberHeight";
            this.upDownNumberHeight.Value = new decimal(new int[] {
            20,
            0,
            0,
            65536});
            this.upDownNumberHeight.ValueChanged += new System.EventHandler(this.upDown_ValueChanged);
            // 
            // labelLineWidth
            // 
            resources.ApplyResources(this.labelLineWidth, "labelLineWidth");
            this.labelLineWidth.Name = "labelLineWidth";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // upDownLineWidth
            // 
            this.upDownLineWidth.DecimalPlaces = 2;
            this.upDownLineWidth.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            resources.ApplyResources(this.upDownLineWidth, "upDownLineWidth");
            this.upDownLineWidth.Maximum = new decimal(new int[] {
            100,
            0,
            0,
            65536});
            this.upDownLineWidth.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.upDownLineWidth.Name = "upDownLineWidth";
            this.upDownLineWidth.Value = new decimal(new int[] {
            20,
            0,
            0,
            65536});
            this.upDownLineWidth.ValueChanged += new System.EventHandler(this.upDown_ValueChanged);
            // 
            // labelControlCircle
            // 
            resources.ApplyResources(this.labelControlCircle, "labelControlCircle");
            this.labelControlCircle.Name = "labelControlCircle";
            // 
            // labelMM1
            // 
            resources.ApplyResources(this.labelMM1, "labelMM1");
            this.labelMM1.Name = "labelMM1";
            // 
            // upDownControlCircle
            // 
            this.upDownControlCircle.DecimalPlaces = 2;
            this.upDownControlCircle.Increment = new decimal(new int[] {
            10,
            0,
            0,
            131072});
            resources.ApplyResources(this.upDownControlCircle, "upDownControlCircle");
            this.upDownControlCircle.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            65536});
            this.upDownControlCircle.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            65536});
            this.upDownControlCircle.Name = "upDownControlCircle";
            this.upDownControlCircle.Value = new decimal(new int[] {
            20,
            0,
            0,
            65536});
            this.upDownControlCircle.ValueChanged += new System.EventHandler(this.upDown_ValueChanged);
            // 
            // checkBoxStandardSizes
            // 
            resources.ApplyResources(this.checkBoxStandardSizes, "checkBoxStandardSizes");
            this.checkBoxStandardSizes.Name = "checkBoxStandardSizes";
            this.checkBoxStandardSizes.UseVisualStyleBackColor = true;
            this.checkBoxStandardSizes.CheckedChanged += new System.EventHandler(this.checkBoxStandardSizes_CheckedChanged);
            // 
            // groupBoxPurple
            // 
            resources.ApplyResources(this.groupBoxPurple, "groupBoxPurple");
            this.groupBoxPurple.Controls.Add(this.buttonColorChoosers);
            this.groupBoxPurple.Controls.Add(this.label5);
            this.groupBoxPurple.Controls.Add(this.upDownBlack);
            this.groupBoxPurple.Controls.Add(this.label6);
            this.groupBoxPurple.Controls.Add(this.upDownYellow);
            this.groupBoxPurple.Controls.Add(this.label3);
            this.groupBoxPurple.Controls.Add(this.upDownMagenta);
            this.groupBoxPurple.Controls.Add(this.label1);
            this.groupBoxPurple.Controls.Add(this.upDownCyan);
            this.groupBoxPurple.Controls.Add(this.checkBoxDefaultPurple);
            this.groupBoxPurple.Name = "groupBoxPurple";
            this.groupBoxPurple.TabStop = false;
            // 
            // buttonColorChoosers
            // 
            resources.ApplyResources(this.buttonColorChoosers, "buttonColorChoosers");
            this.buttonColorChoosers.Name = "buttonColorChoosers";
            this.buttonColorChoosers.UseVisualStyleBackColor = true;
            this.buttonColorChoosers.Click += new System.EventHandler(this.buttonColorChoosers_Click);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // upDownBlack
            // 
            this.upDownBlack.DecimalPlaces = 1;
            resources.ApplyResources(this.upDownBlack, "upDownBlack");
            this.upDownBlack.Name = "upDownBlack";
            this.upDownBlack.ValueChanged += new System.EventHandler(this.upDown_ValueChanged);
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // upDownYellow
            // 
            this.upDownYellow.DecimalPlaces = 1;
            resources.ApplyResources(this.upDownYellow, "upDownYellow");
            this.upDownYellow.Name = "upDownYellow";
            this.upDownYellow.ValueChanged += new System.EventHandler(this.upDown_ValueChanged);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // upDownMagenta
            // 
            this.upDownMagenta.DecimalPlaces = 1;
            resources.ApplyResources(this.upDownMagenta, "upDownMagenta");
            this.upDownMagenta.Name = "upDownMagenta";
            this.upDownMagenta.ValueChanged += new System.EventHandler(this.upDown_ValueChanged);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // upDownCyan
            // 
            this.upDownCyan.DecimalPlaces = 1;
            resources.ApplyResources(this.upDownCyan, "upDownCyan");
            this.upDownCyan.Name = "upDownCyan";
            this.upDownCyan.ValueChanged += new System.EventHandler(this.upDown_ValueChanged);
            // 
            // checkBoxDefaultPurple
            // 
            resources.ApplyResources(this.checkBoxDefaultPurple, "checkBoxDefaultPurple");
            this.checkBoxDefaultPurple.Name = "checkBoxDefaultPurple";
            this.checkBoxDefaultPurple.UseVisualStyleBackColor = true;
            this.checkBoxDefaultPurple.CheckedChanged += new System.EventHandler(this.checkBoxDefaultPurple_CheckedChanged);
            // 
            // colorDialog
            // 
            this.colorDialog.AnyColor = true;
            this.colorDialog.FullOpen = true;
            // 
            // groupBoxPreview
            // 
            resources.ApplyResources(this.groupBoxPreview, "groupBoxPreview");
            this.groupBoxPreview.Controls.Add(this.pictureBoxPreview);
            this.groupBoxPreview.Name = "groupBoxPreview";
            this.groupBoxPreview.TabStop = false;
            // 
            // pictureBoxPreview
            // 
            this.pictureBoxPreview.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.pictureBoxPreview, "pictureBoxPreview");
            this.pictureBoxPreview.Name = "pictureBoxPreview";
            this.pictureBoxPreview.TabStop = false;
            this.pictureBoxPreview.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBoxPreview_Paint);
            // 
            // CourseAppearanceDialog
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.groupBoxPreview);
            this.Controls.Add(this.groupBoxPurple);
            this.Controls.Add(this.groupBoxSizes);
            this.HelpTopic = "EventCustomizeCourseAppearance.htm";
            this.Name = "CourseAppearanceDialog";
            this.Controls.SetChildIndex(this.groupBoxSizes, 0);
            this.Controls.SetChildIndex(this.groupBoxPurple, 0);
            this.Controls.SetChildIndex(this.groupBoxPreview, 0);
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.groupBoxSizes.ResumeLayout(false);
            this.groupBoxSizes.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) (this.upDownNumberHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.upDownLineWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.upDownControlCircle)).EndInit();
            this.groupBoxPurple.ResumeLayout(false);
            this.groupBoxPurple.PerformLayout();
            ((System.ComponentModel.ISupportInitialize) (this.upDownBlack)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.upDownYellow)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.upDownMagenta)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.upDownCyan)).EndInit();
            this.groupBoxPreview.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) (this.pictureBoxPreview)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxSizes;
        private System.Windows.Forms.Label labelMM1;
        private System.Windows.Forms.NumericUpDown upDownControlCircle;
        private System.Windows.Forms.CheckBox checkBoxStandardSizes;
        private System.Windows.Forms.GroupBox groupBoxPurple;
        private System.Windows.Forms.Label labelControlNumber;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown upDownNumberHeight;
        private System.Windows.Forms.Label labelLineWidth;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown upDownLineWidth;
        private System.Windows.Forms.Label labelControlCircle;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown upDownCyan;
        private System.Windows.Forms.CheckBox checkBoxDefaultPurple;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown upDownBlack;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown upDownYellow;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown upDownMagenta;
        private System.Windows.Forms.Button buttonColorChoosers;
        private System.Windows.Forms.ColorDialog colorDialog;
        private System.Windows.Forms.GroupBox groupBoxPreview;
        private System.Windows.Forms.PictureBox pictureBoxPreview;
    }
}