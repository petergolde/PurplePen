namespace TestingUtils
{
    partial class BitmapViewerTest
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
            this.bitmapViewer1 = new TestingUtils.BitmapViewer();
            this.SuspendLayout();
            // 
            // bitmapViewer1
            // 
            this.bitmapViewer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bitmapViewer1.Location = new System.Drawing.Point(0, 0);
            this.bitmapViewer1.Margin = new System.Windows.Forms.Padding(1200, 258, 1200, 258);
            this.bitmapViewer1.Name = "bitmapViewer1";
            this.bitmapViewer1.Size = new System.Drawing.Size(1330, 1045);
            this.bitmapViewer1.TabIndex = 0;
            // 
            // BitmapViewerTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1330, 1045);
            this.Controls.Add(this.bitmapViewer1);
            this.Margin = new System.Windows.Forms.Padding(60, 28, 60, 28);
            this.Name = "BitmapViewerTest";
            this.Text = "BitmapViewerTest";
            this.ResumeLayout(false);

        }

        #endregion

        private TestingUtils.BitmapViewer bitmapViewer1;
    }
}
