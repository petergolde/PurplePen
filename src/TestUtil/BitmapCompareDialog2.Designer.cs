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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BitmapCompareDialog2));
            this.panel1 = new System.Windows.Forms.Panel();
            this.buttonFail = new System.Windows.Forms.Button();
            this.buttonAccept = new System.Windows.Forms.Button();
            this.infoText = new System.Windows.Forms.Label();
            this.checkBoxBlink = new System.Windows.Forms.CheckBox();
            this.checkBoxRed = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.labelNowShowing = new System.Windows.Forms.Label();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.bitmapViewer = new TestingUtils.BitmapViewer();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.labelNowShowing);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.checkBoxRed);
            this.panel1.Controls.Add(this.checkBoxBlink);
            this.panel1.Controls.Add(this.infoText);
            this.panel1.Controls.Add(this.buttonAccept);
            this.panel1.Controls.Add(this.buttonFail);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(784, 57);
            this.panel1.TabIndex = 1;
            // 
            // buttonFail
            // 
            this.buttonFail.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonFail.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonFail.Location = new System.Drawing.Point(697, 12);
            this.buttonFail.Name = "buttonFail";
            this.buttonFail.Size = new System.Drawing.Size(75, 23);
            this.buttonFail.TabIndex = 0;
            this.buttonFail.Text = "Fail";
            this.buttonFail.UseVisualStyleBackColor = true;
            this.buttonFail.Click += new System.EventHandler(this.buttonFail_Click);
            // 
            // buttonAccept
            // 
            this.buttonAccept.Anchor = ((System.Windows.Forms.AnchorStyles) ((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAccept.Location = new System.Drawing.Point(593, 12);
            this.buttonAccept.Name = "buttonAccept";
            this.buttonAccept.Size = new System.Drawing.Size(98, 23);
            this.buttonAccept.TabIndex = 1;
            this.buttonAccept.Text = "Accept Baseline";
            this.buttonAccept.UseVisualStyleBackColor = true;
            this.buttonAccept.Click += new System.EventHandler(this.buttonAccept_Click);
            // 
            // infoText
            // 
            this.infoText.Location = new System.Drawing.Point(12, 9);
            this.infoText.Name = "infoText";
            this.infoText.Size = new System.Drawing.Size(291, 40);
            this.infoText.TabIndex = 2;
            this.infoText.Text = "label1";
            // 
            // checkBoxBlink
            // 
            this.checkBoxBlink.AutoSize = true;
            this.checkBoxBlink.Location = new System.Drawing.Point(317, 8);
            this.checkBoxBlink.Name = "checkBoxBlink";
            this.checkBoxBlink.Size = new System.Drawing.Size(104, 17);
            this.checkBoxBlink.TabIndex = 3;
            this.checkBoxBlink.Text = "Continuous blink";
            this.checkBoxBlink.UseVisualStyleBackColor = true;
            // 
            // checkBoxRed
            // 
            this.checkBoxRed.AutoSize = true;
            this.checkBoxRed.Location = new System.Drawing.Point(432, 8);
            this.checkBoxRed.Name = "checkBoxRed";
            this.checkBoxRed.Size = new System.Drawing.Size(132, 17);
            this.checkBoxRed.TabIndex = 4;
            this.checkBoxRed.Text = "Show difference in red";
            this.checkBoxRed.UseVisualStyleBackColor = true;
            this.checkBoxRed.CheckedChanged += new System.EventHandler(this.checkBoxRed_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(314, 36);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(74, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Now showing:";
            // 
            // labelNowShowing
            // 
            this.labelNowShowing.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
            this.labelNowShowing.Location = new System.Drawing.Point(394, 36);
            this.labelNowShowing.Name = "labelNowShowing";
            this.labelNowShowing.Size = new System.Drawing.Size(196, 17);
            this.labelNowShowing.TabIndex = 6;
            this.labelNowShowing.Text = "now showing";
            // 
            // timer
            // 
            this.timer.Enabled = true;
            this.timer.Interval = 350;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // bitmapViewer
            // 
            this.bitmapViewer.Bitmap = null;
            this.bitmapViewer.CenterPoint = ((System.Drawing.PointF) (resources.GetObject("bitmapViewer.CenterPoint")));
            this.bitmapViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bitmapViewer.Location = new System.Drawing.Point(0, 57);
            this.bitmapViewer.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            this.bitmapViewer.Name = "bitmapViewer";
            this.bitmapViewer.Size = new System.Drawing.Size(784, 707);
            this.bitmapViewer.TabIndex = 0;
            this.bitmapViewer.Viewport = ((System.Drawing.RectangleF) (resources.GetObject("bitmapViewer.Viewport")));
            this.bitmapViewer.ZoomFactor = 1F;
            this.bitmapViewer.MouseDown += new System.Windows.Forms.MouseEventHandler(this.bitmapViewer_MouseDown);
            // 
            // BitmapCompareDialog2
            // 
            this.AcceptButton = this.buttonFail;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonFail;
            this.ClientSize = new System.Drawing.Size(784, 764);
            this.Controls.Add(this.bitmapViewer);
            this.Controls.Add(this.panel1);
            this.Name = "BitmapCompareDialog2";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Bitmaps do not match";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Shown += new System.EventHandler(this.BitmapCompareDialog2_Shown);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.BitmapCompareDialog2_FormClosed);
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
    }
}