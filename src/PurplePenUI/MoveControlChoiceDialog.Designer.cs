namespace PurplePen
{
    partial class MoveControlChoiceDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MoveControlChoiceDialog));
            this.moveButton = new PurplePen.TitleDetailButton();
            this.labelExplanation = new System.Windows.Forms.Label();
            this.duplicateButton = new PurplePen.TitleDetailButton();
            this.cancelButton = new PurplePen.TitleDetailButton();
            this.labelOtherCourses = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // moveButton
            // 
            this.moveButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.moveButton.DetailText = "This course and the above listed courses will all change.";
            this.moveButton.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.moveButton.Icon = ((System.Drawing.Image)(resources.GetObject("moveButton.Icon")));
            this.moveButton.Location = new System.Drawing.Point(12, 122);
            this.moveButton.Name = "moveButton";
            this.moveButton.Size = new System.Drawing.Size(395, 90);
            this.moveButton.TabIndex = 0;
            this.moveButton.TitleText = "Move Control In All Courses";
            this.moveButton.Click += new System.EventHandler(this.moveButton_Click);
            // 
            // labelExplanation
            // 
            this.labelExplanation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelExplanation.Location = new System.Drawing.Point(14, 13);
            this.labelExplanation.Name = "labelExplanation";
            this.labelExplanation.Size = new System.Drawing.Size(393, 51);
            this.labelExplanation.TabIndex = 1;
            this.labelExplanation.Text = "Control \"{0}\" is present in the following other courses. If you move it, those co" +
    "urses will be changed also.\r\n";
            // 
            // duplicateButton
            // 
            this.duplicateButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.duplicateButton.DetailText = "Other courses will not change, and control \"{0}\" will be replaced with a new cont" +
    "rol at the new location in this course.";
            this.duplicateButton.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.duplicateButton.Icon = ((System.Drawing.Image)(resources.GetObject("duplicateButton.Icon")));
            this.duplicateButton.Location = new System.Drawing.Point(12, 218);
            this.duplicateButton.Name = "duplicateButton";
            this.duplicateButton.Size = new System.Drawing.Size(395, 90);
            this.duplicateButton.TabIndex = 2;
            this.duplicateButton.TitleText = "Create New Control In This Course";
            this.duplicateButton.Click += new System.EventHandler(this.duplicateButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DetailText = "Do not move this control.";
            this.cancelButton.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cancelButton.Icon = ((System.Drawing.Image)(resources.GetObject("cancelButton.Icon")));
            this.cancelButton.Location = new System.Drawing.Point(12, 314);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(395, 90);
            this.cancelButton.TabIndex = 3;
            this.cancelButton.TitleText = "Do Nothing";
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // labelOtherCourses
            // 
            this.labelOtherCourses.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelOtherCourses.Location = new System.Drawing.Point(30, 64);
            this.labelOtherCourses.Name = "labelOtherCourses";
            this.labelOtherCourses.Size = new System.Drawing.Size(377, 55);
            this.labelOtherCourses.TabIndex = 4;
            // 
            // MoveControlChoiceDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.ClientSize = new System.Drawing.Size(419, 409);
            this.Controls.Add(this.labelOtherCourses);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.duplicateButton);
            this.Controls.Add(this.labelExplanation);
            this.Controls.Add(this.moveButton);
            this.HelpTopic = "MovingSharedControl.htm";
            this.KeyPreview = true;
            this.Name = "MoveControlChoiceDialog";
            this.Text = "Moving Shared Control";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MoveControlChoiceDialog_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion

        private TitleDetailButton moveButton;
        private System.Windows.Forms.Label labelExplanation;
        private TitleDetailButton duplicateButton;
        private TitleDetailButton cancelButton;
        private System.Windows.Forms.Label labelOtherCourses;
    }
}
