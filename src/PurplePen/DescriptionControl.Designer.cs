namespace PurplePen
{
    partial class DescriptionControl
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
            if (disposing) {
                components?.Dispose();
                popup?.Dispose();
                popup = null;

                if (selectionBrush != null) {
                    selectionBrush.Dispose();
                    selectionBrush = null;
                }
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DescriptionControl));
            this.descriptionPanel = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // descriptionPanel
            // 
            this.descriptionPanel.BackColor = System.Drawing.Color.White;
            resources.ApplyResources(this.descriptionPanel, "descriptionPanel");
            this.descriptionPanel.Name = "descriptionPanel";
            this.descriptionPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.descriptionPanel_Paint);
            this.descriptionPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.descriptionPanel_MouseDown);
            // 
            // DescriptionControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.descriptionPanel);
            this.Name = "DescriptionControl";
            this.Layout += new System.Windows.Forms.LayoutEventHandler(this.DescriptionControl_Layout);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel descriptionPanel;
    }
}
