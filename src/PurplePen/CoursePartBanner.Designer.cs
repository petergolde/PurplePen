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
            this.variationsLabel = new System.Windows.Forms.Label();
            this.partComboBox = new System.Windows.Forms.ComboBox();
            this.buttonProperties = new System.Windows.Forms.Button();
            this.variationsComboBox = new System.Windows.Forms.ComboBox();
            this.coursePartLabel = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.variationsComboBox, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.variationsLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.partComboBox, 3, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonProperties, 4, 0);
            this.tableLayoutPanel1.Controls.Add(this.coursePartLabel, 2, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // variationsLabel
            // 
            resources.ApplyResources(this.variationsLabel, "variationsLabel");
            this.variationsLabel.Name = "variationsLabel";
            // 
            // partComboBox
            // 
            this.partComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.partComboBox.FormattingEnabled = true;
            resources.ApplyResources(this.partComboBox, "partComboBox");
            this.partComboBox.Name = "partComboBox";
            this.partComboBox.SelectedIndexChanged += new System.EventHandler(this.partComboBox_SelectedIndexChanged);
            // 
            // buttonProperties
            // 
            resources.ApplyResources(this.buttonProperties, "buttonProperties");
            this.buttonProperties.Name = "buttonProperties";
            this.buttonProperties.UseVisualStyleBackColor = true;
            this.buttonProperties.Click += new System.EventHandler(this.buttonProperties_Click);
            // 
            // variationsComboBox
            // 
            this.variationsComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.variationsComboBox.FormattingEnabled = true;
            resources.ApplyResources(this.variationsComboBox, "variationsComboBox");
            this.variationsComboBox.Name = "variationsComboBox";
            this.variationsComboBox.SelectedIndexChanged += new System.EventHandler(this.variationsComboBox_SelectedIndexChanged);
            // 
            // coursePartLabel
            // 
            resources.ApplyResources(this.coursePartLabel, "coursePartLabel");
            this.coursePartLabel.Name = "coursePartLabel";
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
        private System.Windows.Forms.Label variationsLabel;
        private System.Windows.Forms.ComboBox partComboBox;
        private System.Windows.Forms.Button buttonProperties;
        private System.Windows.Forms.ComboBox variationsComboBox;
        private System.Windows.Forms.Label coursePartLabel;
    }
}
