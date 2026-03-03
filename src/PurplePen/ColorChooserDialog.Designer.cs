namespace PurplePen
{
    partial class ColorChooserDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ColorChooserDialog));
            this.label5 = new System.Windows.Forms.Label();
            this.upDownBlack = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.upDownYellow = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.upDownMagenta = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.upDownCyan = new System.Windows.Forms.NumericUpDown();
            this.groupBoxPreview = new System.Windows.Forms.GroupBox();
            this.pictureBoxPreview = new System.Windows.Forms.PictureBox();
            this.checkBoxOverprint = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.upDownBlack)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownYellow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownMagenta)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownCyan)).BeginInit();
            this.groupBoxPreview.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).BeginInit();
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
            // checkBoxOverprint
            // 
            resources.ApplyResources(this.checkBoxOverprint, "checkBoxOverprint");
            this.checkBoxOverprint.Name = "checkBoxOverprint";
            this.checkBoxOverprint.UseVisualStyleBackColor = true;
            // 
            // ColorChooserDialog
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.checkBoxOverprint);
            this.Controls.Add(this.groupBoxPreview);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.upDownBlack);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.upDownYellow);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.upDownMagenta);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.upDownCyan);
            this.Name = "ColorChooserDialog";
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.Controls.SetChildIndex(this.upDownCyan, 0);
            this.Controls.SetChildIndex(this.label1, 0);
            this.Controls.SetChildIndex(this.upDownMagenta, 0);
            this.Controls.SetChildIndex(this.label3, 0);
            this.Controls.SetChildIndex(this.upDownYellow, 0);
            this.Controls.SetChildIndex(this.label6, 0);
            this.Controls.SetChildIndex(this.upDownBlack, 0);
            this.Controls.SetChildIndex(this.label5, 0);
            this.Controls.SetChildIndex(this.groupBoxPreview, 0);
            this.Controls.SetChildIndex(this.checkBoxOverprint, 0);
            ((System.ComponentModel.ISupportInitialize)(this.upDownBlack)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownYellow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownMagenta)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownCyan)).EndInit();
            this.groupBoxPreview.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown upDownBlack;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown upDownYellow;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown upDownMagenta;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown upDownCyan;
        private System.Windows.Forms.GroupBox groupBoxPreview;
        private System.Windows.Forms.PictureBox pictureBoxPreview;
        private System.Windows.Forms.CheckBox checkBoxOverprint;
    }
}