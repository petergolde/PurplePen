namespace PurplePen
{
    partial class TeamVariationsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TeamVariationsForm));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.buttonClose = new System.Windows.Forms.Button();
            this.buttonExport = new System.Windows.Forms.Button();
            this.buttonPrint = new System.Windows.Forms.Button();
            this.buttonPrintPreview = new System.Windows.Forms.Button();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.upDownNumberOfLegs = new System.Windows.Forms.NumericUpDown();
            this.buttonCalculate = new System.Windows.Forms.Button();
            this.upDownNumberOfTeams = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.webBrowser = new System.Windows.Forms.WebBrowser();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownNumberOfLegs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownNumberOfTeams)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.buttonClose, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonExport, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonPrint, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonPrintPreview, 2, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // buttonClose
            // 
            resources.ApplyResources(this.buttonClose, "buttonClose");
            this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.UseVisualStyleBackColor = true;
            // 
            // buttonExport
            // 
            resources.ApplyResources(this.buttonExport, "buttonExport");
            this.buttonExport.Name = "buttonExport";
            this.buttonExport.UseVisualStyleBackColor = true;
            // 
            // buttonPrint
            // 
            resources.ApplyResources(this.buttonPrint, "buttonPrint");
            this.buttonPrint.Name = "buttonPrint";
            this.buttonPrint.UseVisualStyleBackColor = true;
            this.buttonPrint.Click += new System.EventHandler(this.buttonPrint_Click);
            // 
            // buttonPrintPreview
            // 
            resources.ApplyResources(this.buttonPrintPreview, "buttonPrintPreview");
            this.buttonPrintPreview.Name = "buttonPrintPreview";
            this.buttonPrintPreview.UseVisualStyleBackColor = true;
            this.buttonPrintPreview.Click += new System.EventHandler(this.buttonPrintPreview_Click);
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.label2, 3, 0);
            this.tableLayoutPanel2.Controls.Add(this.upDownNumberOfLegs, 4, 0);
            this.tableLayoutPanel2.Controls.Add(this.buttonCalculate, 5, 0);
            this.tableLayoutPanel2.Controls.Add(this.upDownNumberOfTeams, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // upDownNumberOfLegs
            // 
            resources.ApplyResources(this.upDownNumberOfLegs, "upDownNumberOfLegs");
            this.upDownNumberOfLegs.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.upDownNumberOfLegs.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.upDownNumberOfLegs.Name = "upDownNumberOfLegs";
            this.upDownNumberOfLegs.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // buttonCalculate
            // 
            resources.ApplyResources(this.buttonCalculate, "buttonCalculate");
            this.buttonCalculate.Name = "buttonCalculate";
            this.buttonCalculate.UseVisualStyleBackColor = true;
            this.buttonCalculate.Click += new System.EventHandler(this.buttonCalculate_Click);
            // 
            // upDownNumberOfTeams
            // 
            resources.ApplyResources(this.upDownNumberOfTeams, "upDownNumberOfTeams");
            this.upDownNumberOfTeams.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.upDownNumberOfTeams.Name = "upDownNumberOfTeams";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // webBrowser
            // 
            this.webBrowser.AllowWebBrowserDrop = false;
            resources.ApplyResources(this.webBrowser, "webBrowser");
            this.webBrowser.Name = "webBrowser";
            this.webBrowser.Url = new System.Uri("http://purple-pen.org", System.UriKind.Absolute);
            // 
            // TeamVariationsForm
            // 
            this.AcceptButton = this.buttonCalculate;
            resources.ApplyResources(this, "$this");
            this.CancelButton = this.buttonClose;
            this.Controls.Add(this.webBrowser);
            this.Controls.Add(this.tableLayoutPanel2);
            this.Controls.Add(this.tableLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Name = "TeamVariationsForm";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.upDownNumberOfLegs)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.upDownNumberOfTeams)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown upDownNumberOfLegs;
        private System.Windows.Forms.Button buttonCalculate;
        private System.Windows.Forms.NumericUpDown upDownNumberOfTeams;
        private System.Windows.Forms.WebBrowser webBrowser;
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.Button buttonExport;
        private System.Windows.Forms.Button buttonPrint;
        private System.Windows.Forms.Button buttonPrintPreview;
    }
}
