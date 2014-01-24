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
            this.okButton.Location = new System.Drawing.Point(148, 184);
            this.okButton.TabIndex = 9;
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(244, 184);
            this.cancelButton.TabIndex = 10;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label5.Location = new System.Drawing.Point(189, 51);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(38, 15);
            this.label5.TabIndex = 6;
            this.label5.Text = "Black:";
            // 
            // upDownBlack
            // 
            this.upDownBlack.DecimalPlaces = 1;
            this.upDownBlack.Location = new System.Drawing.Point(261, 49);
            this.upDownBlack.Name = "upDownBlack";
            this.upDownBlack.Size = new System.Drawing.Size(48, 23);
            this.upDownBlack.TabIndex = 7;
            this.upDownBlack.ValueChanged += new System.EventHandler(this.upDown_ValueChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label6.Location = new System.Drawing.Point(18, 51);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(45, 15);
            this.label6.TabIndex = 4;
            this.label6.Text = "Yellow:";
            // 
            // upDownYellow
            // 
            this.upDownYellow.DecimalPlaces = 1;
            this.upDownYellow.Location = new System.Drawing.Point(90, 49);
            this.upDownYellow.Name = "upDownYellow";
            this.upDownYellow.Size = new System.Drawing.Size(48, 23);
            this.upDownYellow.TabIndex = 5;
            this.upDownYellow.ValueChanged += new System.EventHandler(this.upDown_ValueChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label3.Location = new System.Drawing.Point(189, 23);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(57, 15);
            this.label3.TabIndex = 2;
            this.label3.Text = "Magenta:";
            // 
            // upDownMagenta
            // 
            this.upDownMagenta.DecimalPlaces = 1;
            this.upDownMagenta.Location = new System.Drawing.Point(261, 21);
            this.upDownMagenta.Name = "upDownMagenta";
            this.upDownMagenta.Size = new System.Drawing.Size(48, 23);
            this.upDownMagenta.TabIndex = 3;
            this.upDownMagenta.ValueChanged += new System.EventHandler(this.upDown_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label1.Location = new System.Drawing.Point(18, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Cyan:";
            // 
            // upDownCyan
            // 
            this.upDownCyan.DecimalPlaces = 1;
            this.upDownCyan.Location = new System.Drawing.Point(90, 21);
            this.upDownCyan.Name = "upDownCyan";
            this.upDownCyan.Size = new System.Drawing.Size(48, 23);
            this.upDownCyan.TabIndex = 0;
            this.upDownCyan.ValueChanged += new System.EventHandler(this.upDown_ValueChanged);
            // 
            // groupBoxPreview
            // 
            this.groupBoxPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxPreview.Controls.Add(this.pictureBoxPreview);
            this.groupBoxPreview.Location = new System.Drawing.Point(12, 87);
            this.groupBoxPreview.Name = "groupBoxPreview";
            this.groupBoxPreview.Size = new System.Drawing.Size(319, 78);
            this.groupBoxPreview.TabIndex = 8;
            this.groupBoxPreview.TabStop = false;
            this.groupBoxPreview.Text = "Preview";
            // 
            // pictureBoxPreview
            // 
            this.pictureBoxPreview.BackColor = System.Drawing.Color.White;
            this.pictureBoxPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBoxPreview.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.pictureBoxPreview.Location = new System.Drawing.Point(3, 19);
            this.pictureBoxPreview.Name = "pictureBoxPreview";
            this.pictureBoxPreview.Size = new System.Drawing.Size(313, 56);
            this.pictureBoxPreview.TabIndex = 5;
            this.pictureBoxPreview.TabStop = false;
            this.pictureBoxPreview.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBoxPreview_Paint);
            // 
            // ColorChooserDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(343, 218);
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
            this.Text = "Choose Color";
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
    }
}