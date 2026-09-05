namespace PurplePen.DebugUI
{
    partial class DotGridTester
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
            this.rowsControl = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.colControl = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.dotGrid1 = new PurplePen.DotGrid();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize) (this.rowsControl)).BeginInit();
            ((System.ComponentModel.ISupportInitialize) (this.colControl)).BeginInit();
            this.SuspendLayout();
            // 
            // rowsControl
            // 
            this.rowsControl.Location = new System.Drawing.Point(52, 7);
            this.rowsControl.Maximum = new decimal(new int[] {
            99,
            0,
            0,
            0});
            this.rowsControl.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.rowsControl.Name = "rowsControl";
            this.rowsControl.Size = new System.Drawing.Size(54, 20);
            this.rowsControl.TabIndex = 1;
            this.rowsControl.Value = new decimal(new int[] {
            9,
            0,
            0,
            0});
            this.rowsControl.ValueChanged += new System.EventHandler(this.rowsControl_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Rows:";
            // 
            // colControl
            // 
            this.colControl.Location = new System.Drawing.Point(188, 7);
            this.colControl.Maximum = new decimal(new int[] {
            99,
            0,
            0,
            0});
            this.colControl.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.colControl.Name = "colControl";
            this.colControl.Size = new System.Drawing.Size(54, 20);
            this.colControl.TabIndex = 3;
            this.colControl.Value = new decimal(new int[] {
            9,
            0,
            0,
            0});
            this.colControl.ValueChanged += new System.EventHandler(this.colControl_ValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(152, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(30, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Cols:";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 33);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(103, 23);
            this.button1.TabIndex = 5;
            this.button1.Text = "CheckerAtOnce";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // dotGrid1
            // 
            this.dotGrid1.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dotGrid1.BackColor = System.Drawing.Color.White;
            this.dotGrid1.DotsAcross = 9;
            this.dotGrid1.DotsDown = 9;
            this.dotGrid1.Location = new System.Drawing.Point(12, 95);
            this.dotGrid1.Name = "dotGrid1";
            this.dotGrid1.Size = new System.Drawing.Size(329, 287);
            this.dotGrid1.TabIndex = 0;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(225, 33);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 6;
            this.button2.Text = "Clear";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(121, 33);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(98, 23);
            this.button3.TabIndex = 7;
            this.button3.Text = "Checker";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(12, 62);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(103, 23);
            this.button4.TabIndex = 8;
            this.button4.Text = "ShowAll";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // DotGridTester
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(351, 393);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.colControl);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.rowsControl);
            this.Controls.Add(this.dotGrid1);
            this.Name = "DotGridTester";
            this.Text = "DotGridTester";
            ((System.ComponentModel.ISupportInitialize) (this.rowsControl)).EndInit();
            ((System.ComponentModel.ISupportInitialize) (this.colControl)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DotGrid dotGrid1;
        private System.Windows.Forms.NumericUpDown rowsControl;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown colControl;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
    }
}
