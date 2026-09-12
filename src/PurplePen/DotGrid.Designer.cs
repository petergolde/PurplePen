namespace PurplePen
{
    partial class DotGrid
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DotGrid));
            this.SuspendLayout();
            // 
            // DotGrid
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.White;
            this.Name = "DotGrid";
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.DotGrid_Paint);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.DotGrid_MouseDown);
            this.Resize += new System.EventHandler(this.DotGrid_Resize);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
