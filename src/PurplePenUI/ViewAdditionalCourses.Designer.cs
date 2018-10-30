namespace PurplePen
{
    partial class ViewAdditionalCourses
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ViewAdditionalCourses));
            this.courseSelector = new PurplePen.CourseSelector();
            this.labelInstructions = new System.Windows.Forms.Label();
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
            this.courseSelector.Filter = null;
            this.courseSelector.Name = "courseSelector";
            this.courseSelector.ShowAllControls = false;
            this.courseSelector.ShowCourseParts = false;
            this.courseSelector.ShowVariationChooser = false;
            // 
            // labelInstructions
            // 
            resources.ApplyResources(this.labelInstructions, "labelInstructions");
            this.labelInstructions.Name = "labelInstructions";
            // 
            // ViewAdditionalCourses
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.labelInstructions);
            this.Controls.Add(this.courseSelector);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.HelpTopic = "ViewAdditionalCourses.htm";
            this.Name = "ViewAdditionalCourses";
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.Controls.SetChildIndex(this.courseSelector, 0);
            this.Controls.SetChildIndex(this.labelInstructions, 0);
            this.ResumeLayout(false);

        }

        #endregion

        private CourseSelector courseSelector;
        private System.Windows.Forms.Label labelInstructions;
    }
}