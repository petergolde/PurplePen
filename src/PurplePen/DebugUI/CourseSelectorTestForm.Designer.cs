namespace PurplePen.DebugUI
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
            this.button1 = new System.Windows.Forms.Button();
            this.outputTextBox = new System.Windows.Forms.TextBox();
            this.courseSelector1 = new PurplePen.CourseSelector();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(3728, 170);
            this.button1.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(2272, 170);
            this.button1.TabIndex = 1;
            this.button1.Text = "GetCheckedIds";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // outputTextBox
            // 
            this.outputTextBox.Location = new System.Drawing.Point(3760, 495);
            this.outputTextBox.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            this.outputTextBox.Multiline = true;
            this.outputTextBox.Name = "outputTextBox";
            this.outputTextBox.Size = new System.Drawing.Size(2164, 1186);
            this.outputTextBox.TabIndex = 2;
            // 
            // courseSelector1
            // 
            this.courseSelector1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.courseSelector1.Location = new System.Drawing.Point(0, 0);
            this.courseSelector1.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            this.courseSelector1.Name = "courseSelector1";
            this.courseSelector1.ShowAllControls = true;
            this.courseSelector1.ShowCourseParts = true;
            this.courseSelector1.Size = new System.Drawing.Size(384, 362);
            this.courseSelector1.TabIndex = 0;
            // 
            // CourseSelectorTestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(384, 362);
            this.Controls.Add(this.outputTextBox);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.courseSelector1);
            this.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
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
