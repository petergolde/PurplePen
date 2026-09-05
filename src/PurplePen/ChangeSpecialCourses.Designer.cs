namespace PurplePen
{
    partial class ChangeSpecialCourses
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChangeSpecialCourses));
            this.courseSelector = new PurplePen.CourseSelector();
            this.changeDisplayedCoursesLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            // 
            // cancelButton
            // 
            resources.ApplyResources(this.cancelButton, "cancelButton");
            // 
            // courseSelector
            // 
            resources.ApplyResources(this.courseSelector, "courseSelector");
            this.courseSelector.Name = "courseSelector";
            this.courseSelector.ShowAllControls = false;
            this.courseSelector.ShowCourseParts = true;
            // 
            // changeDisplayedCoursesLabel
            // 
            resources.ApplyResources(this.changeDisplayedCoursesLabel, "changeDisplayedCoursesLabel");
            this.changeDisplayedCoursesLabel.Name = "changeDisplayedCoursesLabel";
            // 
            // ChangeSpecialCourses
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.changeDisplayedCoursesLabel);
            this.Controls.Add(this.courseSelector);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.HelpTopic = "EditChangeDisplayedCourses.htm";
            this.Name = "ChangeSpecialCourses";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Controls.SetChildIndex(this.courseSelector, 0);
            this.Controls.SetChildIndex(this.changeDisplayedCoursesLabel, 0);
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.ResumeLayout(false);

        }

        #endregion

        private CourseSelector courseSelector;
        private System.Windows.Forms.Label changeDisplayedCoursesLabel;
    }
}
