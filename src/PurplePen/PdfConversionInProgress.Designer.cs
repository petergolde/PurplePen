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
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.labelReadingPDF = new System.Windows.Forms.Label();
            this.textBoxErrorMessage = new System.Windows.Forms.TextBox();
            this.labelFailure = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(158, 252);
            this.okButton.Visible = false;
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(254, 252);
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(13, 62);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(328, 23);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 6;
            // 
            // labelReadingPDF
            // 
            this.labelReadingPDF.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelReadingPDF.Location = new System.Drawing.Point(13, 13);
            this.labelReadingPDF.Name = "labelReadingPDF";
            this.labelReadingPDF.Size = new System.Drawing.Size(331, 27);
            this.labelReadingPDF.TabIndex = 7;
            this.labelReadingPDF.Text = "GPL Ghostscript is reading your PDF...";
            // 
            // textBoxErrorMessage
            // 
            this.textBoxErrorMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxErrorMessage.Location = new System.Drawing.Point(13, 134);
            this.textBoxErrorMessage.Multiline = true;
            this.textBoxErrorMessage.Name = "textBoxErrorMessage";
            this.textBoxErrorMessage.ReadOnly = true;
            this.textBoxErrorMessage.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxErrorMessage.Size = new System.Drawing.Size(328, 112);
            this.textBoxErrorMessage.TabIndex = 8;
            this.textBoxErrorMessage.Visible = false;
            // 
            // labelFailure
            // 
            this.labelFailure.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelFailure.ForeColor = System.Drawing.Color.Red;
            this.labelFailure.Location = new System.Drawing.Point(11, 99);
            this.labelFailure.Name = "labelFailure";
            this.labelFailure.Size = new System.Drawing.Size(330, 32);
            this.labelFailure.TabIndex = 9;
            this.labelFailure.Text = "PDF reading has failed. The errors are shown below.";
            this.labelFailure.Visible = false;
            // 
            // PdfConversionInProgress
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.ClientSize = new System.Drawing.Size(353, 286);
            this.Controls.Add(this.labelFailure);
            this.Controls.Add(this.textBoxErrorMessage);
            this.Controls.Add(this.labelReadingPDF);
            this.Controls.Add(this.progressBar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Name = "PdfConversionInProgress";
            this.Text = "Reading PDF";
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
