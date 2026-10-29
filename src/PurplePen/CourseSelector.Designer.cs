namespace PurplePen
{
    partial class CourseSelector
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CourseSelector));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.selectAll = new System.Windows.Forms.Button();
            this.selectNone = new System.Windows.Forms.Button();
            this.buttonChooseVariations = new System.Windows.Forms.Button();
            this.courseTreeView = new System.Windows.Forms.TreeView();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.selectAll, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.selectNone, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonChooseVariations, 0, 1);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // selectAll
            // 
            resources.ApplyResources(this.selectAll, "selectAll");
            this.selectAll.Name = "selectAll";
            this.selectAll.UseVisualStyleBackColor = true;
            this.selectAll.Click += new System.EventHandler(this.selectAll_Click);
            // 
            // selectNone
            // 
            resources.ApplyResources(this.selectNone, "selectNone");
            this.selectNone.Name = "selectNone";
            this.selectNone.UseVisualStyleBackColor = true;
            this.selectNone.Click += new System.EventHandler(this.selectNone_Click);
            // 
            // buttonChooseVariations
            // 
            resources.ApplyResources(this.buttonChooseVariations, "buttonChooseVariations");
            this.tableLayoutPanel1.SetColumnSpan(this.buttonChooseVariations, 2);
            this.buttonChooseVariations.Name = "buttonChooseVariations";
            this.buttonChooseVariations.UseVisualStyleBackColor = true;
            this.buttonChooseVariations.Click += new System.EventHandler(this.buttonChooseVariations_Click);
            // 
            // courseTreeView
            // 
            this.courseTreeView.CheckBoxes = true;
            resources.ApplyResources(this.courseTreeView, "courseTreeView");
            this.courseTreeView.LineColor = System.Drawing.Color.White;
            this.courseTreeView.Name = "courseTreeView";
            this.courseTreeView.ShowPlusMinus = false;
            this.courseTreeView.ShowRootLines = false;
            this.courseTreeView.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.courseTreeView_AfterCheck);
            this.courseTreeView.BeforeCollapse += new System.Windows.Forms.TreeViewCancelEventHandler(this.courseTreeView_BeforeCollapse);
            this.courseTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.courseTreeView_AfterSelect);
            // 
            // CourseSelector
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.courseTreeView);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "CourseSelector";
            this.Load += new System.EventHandler(this.CourseSelector_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button selectAll;
        private System.Windows.Forms.Button selectNone;
        private System.Windows.Forms.TreeView courseTreeView;
        private System.Windows.Forms.Button buttonChooseVariations;
    }
}
