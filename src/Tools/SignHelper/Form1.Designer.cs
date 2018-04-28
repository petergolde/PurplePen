namespace SignHelper
{
    partial class Form1
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
            this.textBoxSignTool = new System.Windows.Forms.TextBox();
            this.browseSignTool = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.browseCertificate = new System.Windows.Forms.Button();
            this.textBoxCertificate = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxPassword = new System.Windows.Forms.TextBox();
            this.buttonSignNow = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.openFileDialogSignTool = new System.Windows.Forms.OpenFileDialog();
            this.openFileDialogCertificate = new System.Windows.Forms.OpenFileDialog();
            this.label4 = new System.Windows.Forms.Label();
            this.labelFileToSign = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // textBoxSignTool
            // 
            this.textBoxSignTool.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSignTool.Location = new System.Drawing.Point(239, 152);
            this.textBoxSignTool.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.textBoxSignTool.Name = "textBoxSignTool";
            this.textBoxSignTool.Size = new System.Drawing.Size(900, 46);
            this.textBoxSignTool.TabIndex = 0;
            // 
            // browseSignTool
            // 
            this.browseSignTool.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browseSignTool.Location = new System.Drawing.Point(1171, 152);
            this.browseSignTool.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.browseSignTool.Name = "browseSignTool";
            this.browseSignTool.Size = new System.Drawing.Size(181, 50);
            this.browseSignTool.TabIndex = 1;
            this.browseSignTool.Text = "Browse...";
            this.browseSignTool.UseVisualStyleBackColor = true;
            this.browseSignTool.Click += new System.EventHandler(this.browseSignTool_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 160);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(180, 40);
            this.label1.TabIndex = 2;
            this.label1.Text = "SignTool.exe:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 274);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(203, 40);
            this.label2.TabIndex = 5;
            this.label2.Text = "Certificate File:";
            // 
            // browseCertificate
            // 
            this.browseCertificate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.browseCertificate.Location = new System.Drawing.Point(1169, 266);
            this.browseCertificate.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.browseCertificate.Name = "browseCertificate";
            this.browseCertificate.Size = new System.Drawing.Size(181, 50);
            this.browseCertificate.TabIndex = 4;
            this.browseCertificate.Text = "Browse...";
            this.browseCertificate.UseVisualStyleBackColor = true;
            this.browseCertificate.Click += new System.EventHandler(this.browseCertificate_Click);
            // 
            // textBoxCertificate
            // 
            this.textBoxCertificate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxCertificate.Location = new System.Drawing.Point(237, 266);
            this.textBoxCertificate.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.textBoxCertificate.Name = "textBoxCertificate";
            this.textBoxCertificate.Size = new System.Drawing.Size(900, 46);
            this.textBoxCertificate.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(17, 383);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(142, 40);
            this.label3.TabIndex = 7;
            this.label3.Text = "Password:";
            // 
            // textBoxPassword
            // 
            this.textBoxPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxPassword.Location = new System.Drawing.Point(239, 375);
            this.textBoxPassword.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.textBoxPassword.Name = "textBoxPassword";
            this.textBoxPassword.Size = new System.Drawing.Size(900, 46);
            this.textBoxPassword.TabIndex = 6;
            this.textBoxPassword.UseSystemPasswordChar = true;
            // 
            // buttonSignNow
            // 
            this.buttonSignNow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSignNow.Location = new System.Drawing.Point(1126, 518);
            this.buttonSignNow.Name = "buttonSignNow";
            this.buttonSignNow.Size = new System.Drawing.Size(226, 70);
            this.buttonSignNow.TabIndex = 8;
            this.buttonSignNow.Text = "Sign";
            this.buttonSignNow.UseVisualStyleBackColor = true;
            this.buttonSignNow.Click += new System.EventHandler(this.buttonSignNow_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.Location = new System.Drawing.Point(841, 518);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(252, 70);
            this.buttonCancel.TabIndex = 9;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // openFileDialogSignTool
            // 
            this.openFileDialogSignTool.DefaultExt = "exe";
            this.openFileDialogSignTool.Filter = "Executable Files|*.exe";
            this.openFileDialogSignTool.Title = "Select SignTool.exe Location";
            // 
            // openFileDialogCertificate
            // 
            this.openFileDialogCertificate.DefaultExt = "pfx";
            this.openFileDialogCertificate.Filter = "Certificates|*.pfx;*.p12";
            this.openFileDialogCertificate.Title = "Select Certificate";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(24, 44);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(118, 40);
            this.label4.TabIndex = 10;
            this.label4.Text = "Signing:";
            // 
            // labelFileToSign
            // 
            this.labelFileToSign.AutoSize = true;
            this.labelFileToSign.Font = new System.Drawing.Font("Segoe UI", 10.875F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelFileToSign.Location = new System.Drawing.Point(232, 44);
            this.labelFileToSign.Name = "labelFileToSign";
            this.labelFileToSign.Size = new System.Drawing.Size(0, 40);
            this.labelFileToSign.TabIndex = 11;
            // 
            // Form1
            // 
            this.AcceptButton = this.buttonSignNow;
            this.AutoScaleDimensions = new System.Drawing.SizeF(192F, 192F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1380, 619);
            this.Controls.Add(this.labelFileToSign);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonSignNow);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxPassword);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.browseCertificate);
            this.Controls.Add(this.textBoxCertificate);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.browseSignTool);
            this.Controls.Add(this.textBoxSignTool);
            this.Font = new System.Drawing.Font("Segoe UI", 10.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "Form1";
            this.Text = "Sign Helper";
            this.Shown += new System.EventHandler(this.Form1_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxSignTool;
        private System.Windows.Forms.Button browseSignTool;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button browseCertificate;
        private System.Windows.Forms.TextBox textBoxCertificate;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxPassword;
        private System.Windows.Forms.Button buttonSignNow;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.OpenFileDialog openFileDialogSignTool;
        private System.Windows.Forms.OpenFileDialog openFileDialogCertificate;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelFileToSign;
    }
}

