namespace TestingUtils
{
    partial class BitmapCompareDialog2
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
                if (bmNew != null) {
                    bmNew.Dispose();
                    bmNew = null;
                }
                if (bmBaseline != null) {
                    bmBaseline.Dispose();
                    bmBaseline = null;
                }
                if (bmDiff != null) {
                    bmDiff.Dispose();
                    bmDiff = null;
                }
                if (bmWhite != null) {
                    bmWhite.Dispose();
                    bmWhite = null;
                }
                if (components != null) {
                    components.Dispose();
                    components = null;
                }
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
            this.components = new System.ComponentModel.Container();
            this.panel1 = new System.Windows.Forms.Panel();
            this.buttonFixFramework = new System.Windows.Forms.Button();
            this.buttonFixBitness = new System.Windows.Forms.Button();
            this.labelNowShowing = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.checkBoxRed = new System.Windows.Forms.CheckBox();
            this.checkBoxBlink = new System.Windows.Forms.CheckBox();
            this.infoText = new System.Windows.Forms.Label();
            this.buttonAccept = new System.Windows.Forms.Button();
            this.buttonFail = new System.Windows.Forms.Button();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.bitmapViewer = new TestingUtils.BitmapViewer();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.buttonFixFramework);
            this.panel1.Controls.Add(this.buttonFixBitness);
            this.panel1.Controls.Add(this.labelNowShowing);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.checkBoxRed);
            this.panel1.Controls.Add(this.checkBoxBlink);
            this.panel1.Controls.Add(this.infoText);
            this.panel1.Controls.Add(this.buttonAccept);
            this.panel1.Controls.Add(this.buttonFail);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1046, 70);
            this.panel1.TabIndex = 1;
            // 
            // buttonFixFramework
            // 
            this.buttonFixFramework.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonFixFramework.Location = new System.Drawing.Point(844, 38);
            this.buttonFixFramework.Name = "buttonFixFramework";
            this.buttonFixFramework.Size = new System.Drawing.Size(186, 29);
            this.buttonFixFramework.TabIndex = 8;
            this.buttonFixFramework.Text = "Make Framework Specific";
            this.buttonFixFramework.UseVisualStyleBackColor = true;
            this.buttonFixFramework.Click += new System.EventHandler(this.buttonFixFramework_Click);
            // 
            // buttonFixBitness
            // 
            this.buttonFixBitness.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonFixBitness.Location = new System.Drawing.Point(663, 38);
            this.buttonFixBitness.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.buttonFixBitness.Name = "buttonFixBitness";
            this.buttonFixBitness.Size = new System.Drawing.Size(182, 29);
            this.buttonFixBitness.TabIndex = 7;
            this.buttonFixBitness.Text = "Make Bitness Specific";
            this.buttonFixBitness.UseVisualStyleBackColor = true;
            this.buttonFixBitness.Click += new System.EventHandler(this.buttonFixBitness_Click);
            // 
            // labelNowShowing
            // 
            this.labelNowShowing.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelNowShowing.Location = new System.Drawing.Point(517, 45);
            this.labelNowShowing.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.labelNowShowing.Name = "labelNowShowing";
            this.labelNowShowing.Size = new System.Drawing.Size(262, 21);
            this.labelNowShowing.TabIndex = 6;
            this.labelNowShowing.Text = "now showing";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(418, 45);
            this.label1.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(89, 16);
            this.label1.TabIndex = 5;
            this.label1.Text = "Now showing:";
            // 
            // checkBoxRed
            // 
            this.checkBoxRed.AutoSize = true;
            this.checkBoxRed.Location = new System.Drawing.Point(576, 10);
            this.checkBoxRed.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.checkBoxRed.Name = "checkBoxRed";
            this.checkBoxRed.Size = new System.Drawing.Size(160, 20);
            this.checkBoxRed.TabIndex = 4;
            this.checkBoxRed.Text = "Show difference in red";
            this.checkBoxRed.UseVisualStyleBackColor = true;
            this.checkBoxRed.CheckedChanged += new System.EventHandler(this.checkBoxRed_CheckedChanged);
            // 
            // checkBoxBlink
            // 
            this.checkBoxBlink.AutoSize = true;
            this.checkBoxBlink.Location = new System.Drawing.Point(423, 10);
            this.checkBoxBlink.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.checkBoxBlink.Name = "checkBoxBlink";
            this.checkBoxBlink.Size = new System.Drawing.Size(126, 20);
            this.checkBoxBlink.TabIndex = 3;
            this.checkBoxBlink.Text = "Continuous blink";
            this.checkBoxBlink.UseVisualStyleBackColor = true;
            // 
            // infoText
            // 
            this.infoText.Location = new System.Drawing.Point(14, 11);
            this.infoText.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.infoText.Name = "infoText";
            this.infoText.Size = new System.Drawing.Size(367, 49);
            this.infoText.TabIndex = 2;
            this.infoText.Text = "label1";
            // 
            // buttonAccept
            // 
            this.buttonAccept.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAccept.Location = new System.Drawing.Point(791, 9);
            this.buttonAccept.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.buttonAccept.Name = "buttonAccept";
            this.buttonAccept.Size = new System.Drawing.Size(130, 29);
            this.buttonAccept.TabIndex = 1;
            this.buttonAccept.Text = "Accept Baseline";
            this.buttonAccept.UseVisualStyleBackColor = true;
            this.buttonAccept.Click += new System.EventHandler(this.buttonAccept_Click);
            // 
            // buttonFail
            // 
            this.buttonFail.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonFail.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonFail.Location = new System.Drawing.Point(929, 9);
            this.buttonFail.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.buttonFail.Name = "buttonFail";
            this.buttonFail.Size = new System.Drawing.Size(101, 29);
            this.buttonFail.TabIndex = 0;
            this.buttonFail.Text = "Fail";
            this.buttonFail.UseVisualStyleBackColor = true;
            this.buttonFail.Click += new System.EventHandler(this.buttonFail_Click);
            // 
            // timer
            // 
            this.timer.Enabled = true;
            this.timer.Interval = 350;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // bitmapViewer
            // 
            this.bitmapViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bitmapViewer.Location = new System.Drawing.Point(0, 70);
            this.bitmapViewer.Margin = new System.Windows.Forms.Padding(64, 27, 64, 27);
            this.bitmapViewer.Name = "bitmapViewer";
            this.bitmapViewer.Size = new System.Drawing.Size(1046, 871);
            this.bitmapViewer.TabIndex = 0;
            this.bitmapViewer.MouseDown += new System.Windows.Forms.MouseEventHandler(this.bitmapViewer_MouseDown);
            // 
            // BitmapCompareDialog2
            // 
            this.AcceptButton = this.buttonFail;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonFail;
            this.ClientSize = new System.Drawing.Size(1046, 941);
            this.Controls.Add(this.bitmapViewer);
            this.Controls.Add(this.panel1);
            this.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.Name = "BitmapCompareDialog2";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Bitmaps do not match";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.BitmapCompareDialog2_FormClosed);
            this.Shown += new System.EventHandler(this.BitmapCompareDialog2_Shown);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private BitmapViewer bitmapViewer;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label infoText;
        private System.Windows.Forms.Button buttonAccept;
        private System.Windows.Forms.Button buttonFail;
        private System.Windows.Forms.Label labelNowShowing;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox checkBoxRed;
        private System.Windows.Forms.CheckBox checkBoxBlink;
        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.Button buttonFixBitness;
        private System.Windows.Forms.Button buttonFixFramework;
    }
}