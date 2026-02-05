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
            components = new System.ComponentModel.Container();
            panel1 = new System.Windows.Forms.Panel();
            buttonFixBitness = new System.Windows.Forms.Button();
            labelNowShowing = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            checkBoxRed = new System.Windows.Forms.CheckBox();
            checkBoxBlink = new System.Windows.Forms.CheckBox();
            infoText = new System.Windows.Forms.Label();
            buttonAccept = new System.Windows.Forms.Button();
            buttonFail = new System.Windows.Forms.Button();
            timer = new System.Windows.Forms.Timer(components);
            bitmapViewer = new BitmapViewer();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Controls.Add(buttonFixBitness);
            panel1.Controls.Add(labelNowShowing);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(checkBoxRed);
            panel1.Controls.Add(checkBoxBlink);
            panel1.Controls.Add(infoText);
            panel1.Controls.Add(buttonAccept);
            panel1.Controls.Add(buttonFail);
            panel1.Dock = System.Windows.Forms.DockStyle.Top;
            panel1.Location = new System.Drawing.Point(0, 0);
            panel1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(915, 66);
            panel1.TabIndex = 1;
            // 
            // buttonFixBitness
            // 
            buttonFixBitness.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            buttonFixBitness.Location = new System.Drawing.Point(648, 36);
            buttonFixBitness.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            buttonFixBitness.Name = "buttonFixBitness";
            buttonFixBitness.Size = new System.Drawing.Size(159, 27);
            buttonFixBitness.TabIndex = 7;
            buttonFixBitness.Text = "Make Bitness Specific";
            buttonFixBitness.UseVisualStyleBackColor = true;
            buttonFixBitness.Click += buttonFixBitness_Click;
            // 
            // labelNowShowing
            // 
            labelNowShowing.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            labelNowShowing.Location = new System.Drawing.Point(460, 42);
            labelNowShowing.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            labelNowShowing.Name = "labelNowShowing";
            labelNowShowing.Size = new System.Drawing.Size(229, 20);
            labelNowShowing.TabIndex = 6;
            labelNowShowing.Text = "now showing";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(366, 42);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(83, 15);
            label1.TabIndex = 5;
            label1.Text = "Now showing:";
            // 
            // checkBoxRed
            // 
            checkBoxRed.AutoSize = true;
            checkBoxRed.Location = new System.Drawing.Point(504, 9);
            checkBoxRed.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            checkBoxRed.Name = "checkBoxRed";
            checkBoxRed.Size = new System.Drawing.Size(144, 19);
            checkBoxRed.TabIndex = 4;
            checkBoxRed.Text = "Show difference in red";
            checkBoxRed.UseVisualStyleBackColor = true;
            checkBoxRed.CheckedChanged += checkBoxRed_CheckedChanged;
            // 
            // checkBoxBlink
            // 
            checkBoxBlink.AutoSize = true;
            checkBoxBlink.Location = new System.Drawing.Point(370, 9);
            checkBoxBlink.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            checkBoxBlink.Name = "checkBoxBlink";
            checkBoxBlink.Size = new System.Drawing.Size(117, 19);
            checkBoxBlink.TabIndex = 3;
            checkBoxBlink.Text = "Continuous blink";
            checkBoxBlink.UseVisualStyleBackColor = true;
            // 
            // infoText
            // 
            infoText.Location = new System.Drawing.Point(12, 10);
            infoText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            infoText.Name = "infoText";
            infoText.Size = new System.Drawing.Size(340, 46);
            infoText.TabIndex = 2;
            infoText.Text = "label1";
            // 
            // buttonAccept
            // 
            buttonAccept.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            buttonAccept.Location = new System.Drawing.Point(692, 8);
            buttonAccept.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            buttonAccept.Name = "buttonAccept";
            buttonAccept.Size = new System.Drawing.Size(114, 27);
            buttonAccept.TabIndex = 1;
            buttonAccept.Text = "Accept Baseline";
            buttonAccept.UseVisualStyleBackColor = true;
            buttonAccept.Click += buttonAccept_Click;
            // 
            // buttonFail
            // 
            buttonFail.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            buttonFail.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            buttonFail.Location = new System.Drawing.Point(813, 8);
            buttonFail.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            buttonFail.Name = "buttonFail";
            buttonFail.Size = new System.Drawing.Size(88, 27);
            buttonFail.TabIndex = 0;
            buttonFail.Text = "Fail";
            buttonFail.UseVisualStyleBackColor = true;
            buttonFail.Click += buttonFail_Click;
            // 
            // timer
            // 
            timer.Enabled = true;
            timer.Interval = 350;
            timer.Tick += timer_Tick;
            // 
            // bitmapViewer
            // 
            bitmapViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            bitmapViewer.Location = new System.Drawing.Point(0, 66);
            bitmapViewer.Margin = new System.Windows.Forms.Padding(56, 25, 56, 25);
            bitmapViewer.Name = "bitmapViewer";
            bitmapViewer.Size = new System.Drawing.Size(915, 816);
            bitmapViewer.TabIndex = 0;
            bitmapViewer.MouseDown += bitmapViewer_MouseDown;
            // 
            // BitmapCompareDialog2
            // 
            AcceptButton = buttonFail;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = buttonFail;
            ClientSize = new System.Drawing.Size(915, 882);
            Controls.Add(bitmapViewer);
            Controls.Add(panel1);
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "BitmapCompareDialog2";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Bitmaps do not match";
            WindowState = System.Windows.Forms.FormWindowState.Maximized;
            FormClosed += BitmapCompareDialog2_FormClosed;
            Shown += BitmapCompareDialog2_Shown;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);

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
    }
}