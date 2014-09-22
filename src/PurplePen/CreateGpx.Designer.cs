namespace PurplePen
{
    partial class CreateGpx
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateGpx));
            this.coursesGroupBox = new System.Windows.Forms.GroupBox();
            this.courseSelector = new PurplePen.CourseSelector();
            this.waypointGroupBox = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.namePrefixLabel = new System.Windows.Forms.Label();
            this.namePrefixTextBox = new System.Windows.Forms.TextBox();
            this.headerLabel = new System.Windows.Forms.Label();
            this.coursesGroupBox.SuspendLayout();
            this.waypointGroupBox.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
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
            // coursesGroupBox
            // 
            this.coursesGroupBox.Controls.Add(this.courseSelector);
            resources.ApplyResources(this.coursesGroupBox, "coursesGroupBox");
            this.coursesGroupBox.Name = "coursesGroupBox";
            this.coursesGroupBox.TabStop = false;
            // 
            // courseSelector
            // 
            resources.ApplyResources(this.courseSelector, "courseSelector");
            this.courseSelector.Name = "courseSelector";
            this.courseSelector.ShowAllControls = true;
            this.courseSelector.ShowCourseParts = false;
            // 
            // waypointGroupBox
            // 
            this.waypointGroupBox.Controls.Add(this.tableLayoutPanel2);
            resources.ApplyResources(this.waypointGroupBox, "waypointGroupBox");
            this.waypointGroupBox.Name = "waypointGroupBox";
            this.waypointGroupBox.TabStop = false;
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.namePrefixLabel, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.namePrefixTextBox, 1, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // namePrefixLabel
            // 
            resources.ApplyResources(this.namePrefixLabel, "namePrefixLabel");
            this.namePrefixLabel.Name = "namePrefixLabel";
            // 
            // namePrefixTextBox
            // 
            resources.ApplyResources(this.namePrefixTextBox, "namePrefixTextBox");
            this.namePrefixTextBox.Name = "namePrefixTextBox";
            // 
            // headerLabel
            // 
            resources.ApplyResources(this.headerLabel, "headerLabel");
            this.headerLabel.Name = "headerLabel";
            // 
            // CreateGpx
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.headerLabel);
            this.Controls.Add(this.waypointGroupBox);
            this.Controls.Add(this.coursesGroupBox);
            this.Name = "CreateGpx";
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.Controls.SetChildIndex(this.coursesGroupBox, 0);
            this.Controls.SetChildIndex(this.waypointGroupBox, 0);
            this.Controls.SetChildIndex(this.headerLabel, 0);
            this.coursesGroupBox.ResumeLayout(false);
            this.waypointGroupBox.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox coursesGroupBox;
        private CourseSelector courseSelector;
        private System.Windows.Forms.GroupBox waypointGroupBox;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label namePrefixLabel;
        private System.Windows.Forms.TextBox namePrefixTextBox;
        private System.Windows.Forms.Label headerLabel;
    }
}
