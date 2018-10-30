namespace PurplePen
{
    partial class PdfConversionInProgress
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PdfConversionInProgress));
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.labelReadingPDF = new System.Windows.Forms.Label();
            this.textBoxErrorMessage = new System.Windows.Forms.TextBox();
            this.labelFailure = new System.Windows.Forms.Label();
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
            // progressBar
            // 
            resources.ApplyResources(this.progressBar, "progressBar");
            this.progressBar.Name = "progressBar";
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            // 
            // labelReadingPDF
            // 
            resources.ApplyResources(this.labelReadingPDF, "labelReadingPDF");
            this.labelReadingPDF.Name = "labelReadingPDF";
            // 
            // textBoxErrorMessage
            // 
            resources.ApplyResources(this.textBoxErrorMessage, "textBoxErrorMessage");
            this.textBoxErrorMessage.Name = "textBoxErrorMessage";
            this.textBoxErrorMessage.ReadOnly = true;
            // 
            // labelFailure
            // 
            resources.ApplyResources(this.labelFailure, "labelFailure");
            this.labelFailure.ForeColor = System.Drawing.Color.Red;
            this.labelFailure.Name = "labelFailure";
            // 
            // PdfConversionInProgress
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.labelFailure);
            this.Controls.Add(this.textBoxErrorMessage);
            this.Controls.Add(this.labelReadingPDF);
            this.Controls.Add(this.progressBar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Name = "PdfConversionInProgress";
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.Controls.SetChildIndex(this.progressBar, 0);
            this.Controls.SetChildIndex(this.labelReadingPDF, 0);
            this.Controls.SetChildIndex(this.textBoxErrorMessage, 0);
            this.Controls.SetChildIndex(this.labelFailure, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label labelReadingPDF;
        private System.Windows.Forms.TextBox textBoxErrorMessage;
        private System.Windows.Forms.Label labelFailure;
    }
}
