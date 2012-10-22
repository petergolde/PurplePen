namespace PurplePen
{
    partial class CoursePartBanner
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CoursePartBanner));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.coursePartLabel = new System.Windows.Forms.Label();
            this.partComboBox = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.coursePartLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.partComboBox, 1, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // coursePartLabel
            // 
            resources.ApplyResources(this.coursePartLabel, "coursePartLabel");
            this.coursePartLabel.Name = "coursePartLabel";
            // 
            // partComboBox
            // 
            this.partComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.partComboBox.FormattingEnabled = true;
            resources.ApplyResources(this.partComboBox, "partComboBox");
            this.partComboBox.Name = "partComboBox";
            this.partComboBox.SelectedIndexChanged += new System.EventHandler(this.partComboBox_SelectedIndexChanged);
            // 
            // CoursePartBanner
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.SystemColors.ControlLight;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "CoursePartBanner";
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.CoursePartBanner_Paint);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label coursePartLabel;
        private System.Windows.Forms.ComboBox partComboBox;

    }
}
