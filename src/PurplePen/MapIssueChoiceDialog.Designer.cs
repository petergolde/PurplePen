namespace PurplePen
{
    partial class MapIssueChoiceDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MapIssueChoiceDialog));
            this.startButton = new PurplePen.TitleDetailButton();
            this.labelExplanation = new System.Windows.Forms.Label();
            this.middleButton = new PurplePen.TitleDetailButton();
            this.startTriangleButton = new PurplePen.TitleDetailButton();
            this.cancelButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // startButton
            // 
            this.startButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.startButton.DetailText = "The map is given out at the beginning of the marked route to the start. Navigatio" +
    "n begins at the start triangle.";
            this.startButton.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.startButton.Icon = ((System.Drawing.Image)(resources.GetObject("startButton.Icon")));
            this.startButton.Location = new System.Drawing.Point(12, 86);
            this.startButton.Name = "startButton";
            this.startButton.Size = new System.Drawing.Size(395, 90);
            this.startButton.TabIndex = 1;
            this.startButton.TitleText = "Map Issue at Beginning";
            this.startButton.Click += new System.EventHandler(this.startButton_Click);
            // 
            // labelExplanation
            // 
            this.labelExplanation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelExplanation.Location = new System.Drawing.Point(14, 13);
            this.labelExplanation.Name = "labelExplanation";
            this.labelExplanation.Size = new System.Drawing.Size(393, 70);
            this.labelExplanation.TabIndex = 0;
            this.labelExplanation.Text = "The point at which is the map is issued should be marked on the map. Where is the" +
    " map given to competitors?";
            // 
            // middleButton
            // 
            this.middleButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.middleButton.DetailText = "The map is given out partially along the marked route to the start. Navigation be" +
    "gins at the start triangle.";
            this.middleButton.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.middleButton.Icon = ((System.Drawing.Image)(resources.GetObject("middleButton.Icon")));
            this.middleButton.Location = new System.Drawing.Point(12, 182);
            this.middleButton.Name = "middleButton";
            this.middleButton.Size = new System.Drawing.Size(395, 90);
            this.middleButton.TabIndex = 2;
            this.middleButton.TitleText = "Map Issue along Marked Route";
            this.middleButton.Click += new System.EventHandler(this.middleButton_Click);
            // 
            // startTriangleButton
            // 
            this.startTriangleButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.startTriangleButton.DetailText = "The map is issued at the start triangle (where navigation begins).";
            this.startTriangleButton.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.startTriangleButton.Icon = ((System.Drawing.Image)(resources.GetObject("startTriangleButton.Icon")));
            this.startTriangleButton.Location = new System.Drawing.Point(12, 278);
            this.startTriangleButton.Name = "startTriangleButton";
            this.startTriangleButton.Size = new System.Drawing.Size(395, 90);
            this.startTriangleButton.TabIndex = 3;
            this.startTriangleButton.TitleText = "Map Issue at the Start Triangle";
            this.startTriangleButton.Click += new System.EventHandler(this.startTriangleButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(332, 374);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 4;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // MapIssueChoiceDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(419, 409);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.startTriangleButton);
            this.Controls.Add(this.middleButton);
            this.Controls.Add(this.labelExplanation);
            this.Controls.Add(this.startButton);
            this.HelpTopic = "EditAddTimedStart.htm";
            this.KeyPreview = true;
            this.Name = "MapIssueChoiceDialog";
            this.Text = "Select Map Issue Point";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MoveControlChoiceDialog_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion

        private TitleDetailButton startButton;
        private System.Windows.Forms.Label labelExplanation;
        private TitleDetailButton middleButton;
        private TitleDetailButton startTriangleButton;
        private System.Windows.Forms.Button cancelButton;
    }
}
