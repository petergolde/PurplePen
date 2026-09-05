namespace PurplePen.Tests
{
    partial class CourseSelectorTestForm
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
            this.courseSelector1 = new PurplePen.CourseSelector();
            this.button1 = new System.Windows.Forms.Button();
            this.outputTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // courseSelector1
            // 
            this.courseSelector1.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.courseSelector1.Location = new System.Drawing.Point(12, 12);
            this.courseSelector1.Name = "courseSelector1";
            this.courseSelector1.SelectedCourses = new PurplePen.Id<PurplePen.Course>[0];
            this.courseSelector1.ShowAllControls = false;
            this.courseSelector1.Size = new System.Drawing.Size(180, 249);
            this.courseSelector1.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(233, 23);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(142, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "GetCheckedIds";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // outputTextBox
            // 
            this.outputTextBox.Location = new System.Drawing.Point(235, 67);
            this.outputTextBox.Multiline = true;
            this.outputTextBox.Name = "outputTextBox";
            this.outputTextBox.Size = new System.Drawing.Size(139, 164);
            this.outputTextBox.TabIndex = 2;
            // 
            // CourseSelectorTestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(416, 273);
            this.Controls.Add(this.outputTextBox);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.courseSelector1);
            this.Name = "CourseSelectorTestForm";
            this.Text = "CourseSelectorTestForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private PurplePen.CourseSelector courseSelector1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox outputTextBox;
    }
}
