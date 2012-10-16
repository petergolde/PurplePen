namespace TranslateTool
{
    partial class GenerationDialog
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
            this.errorTextBox = new System.Windows.Forms.RichTextBox();
            this.okButton = new System.Windows.Forms.Button();
            this.resultLabel = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.namespaceTextBox = new System.Windows.Forms.TextBox();
            this.assemblyTextBox = new System.Windows.Forms.TextBox();
            this.generateButton = new System.Windows.Forms.Button();
            this.directoryTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // errorTextBox
            // 
            this.errorTextBox.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.errorTextBox.Font = new System.Drawing.Font("Lucida Console", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.errorTextBox.Location = new System.Drawing.Point(12, 131);
            this.errorTextBox.Name = "errorTextBox";
            this.errorTextBox.Size = new System.Drawing.Size(490, 201);
            this.errorTextBox.TabIndex = 0;
            this.errorTextBox.Text = "";
            this.errorTextBox.Visible = false;
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(427, 342);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 1;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Visible = false;
            // 
            // resultLabel
            // 
            this.resultLabel.Location = new System.Drawing.Point(12, 97);
            this.resultLabel.Name = "resultLabel";
            this.resultLabel.Size = new System.Drawing.Size(485, 31);
            this.resultLabel.TabIndex = 2;
            this.resultLabel.Text = "\r\n\r\n";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(67, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Namespace:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 36);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(83, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Assembly name:";
            // 
            // namespaceTextBox
            // 
            this.namespaceTextBox.Location = new System.Drawing.Point(121, 6);
            this.namespaceTextBox.Name = "namespaceTextBox";
            this.namespaceTextBox.Size = new System.Drawing.Size(202, 20);
            this.namespaceTextBox.TabIndex = 5;
            // 
            // assemblyTextBox
            // 
            this.assemblyTextBox.Location = new System.Drawing.Point(121, 33);
            this.assemblyTextBox.Name = "assemblyTextBox";
            this.assemblyTextBox.Size = new System.Drawing.Size(202, 20);
            this.assemblyTextBox.TabIndex = 6;
            // 
            // generateButton
            // 
            this.generateButton.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.generateButton.Location = new System.Drawing.Point(427, 31);
            this.generateButton.Name = "generateButton";
            this.generateButton.Size = new System.Drawing.Size(75, 23);
            this.generateButton.TabIndex = 7;
            this.generateButton.Text = "Generate";
            this.generateButton.UseVisualStyleBackColor = true;
            this.generateButton.Click += new System.EventHandler(this.generateButton_Click);
            // 
            // directoryTextBox
            // 
            this.directoryTextBox.Location = new System.Drawing.Point(121, 62);
            this.directoryTextBox.Name = "directoryTextBox";
            this.directoryTextBox.Size = new System.Drawing.Size(202, 20);
            this.directoryTextBox.TabIndex = 9;
            this.directoryTextBox.Text = "bin\\Release";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 65);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(52, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "Directory:";
            // 
            // GenerationDialog
            // 
            this.AcceptButton = this.generateButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(514, 377);
            this.Controls.Add(this.directoryTextBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.generateButton);
            this.Controls.Add(this.assemblyTextBox);
            this.Controls.Add(this.namespaceTextBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.resultLabel);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.errorTextBox);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GenerationDialog";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Generate Resource DLL";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox errorTextBox;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label resultLabel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox namespaceTextBox;
        private System.Windows.Forms.TextBox assemblyTextBox;
        private System.Windows.Forms.Button generateButton;
        private System.Windows.Forms.TextBox directoryTextBox;
        private System.Windows.Forms.Label label1;
    }
}