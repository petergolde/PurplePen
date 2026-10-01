namespace PurplePen
{
    partial class CourseLoad
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CourseLoad));
            this.grid = new System.Windows.Forms.DataGridView();
            this.CourseName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Load = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.courseLoadLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
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
            // grid
            // 
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AllowUserToResizeColumns = false;
            this.grid.AllowUserToResizeRows = false;
            resources.ApplyResources(this.grid, "grid");
            this.grid.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.CourseName,
            this.Load});
            this.grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnKeystroke;
            this.grid.MultiSelect = false;
            this.grid.Name = "grid";
            this.grid.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.grid.RowHeadersVisible = false;
            this.grid.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.grid.ShowEditingIcon = false;
            this.grid.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.grid_CellValidating);
            // 
            // CourseName
            // 
            this.CourseName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.CourseName.FillWeight = 63F;
            resources.ApplyResources(this.CourseName, "CourseName");
            this.CourseName.Name = "CourseName";
            this.CourseName.ReadOnly = true;
            this.CourseName.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.CourseName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // Load
            // 
            this.Load.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Load.FillWeight = 37F;
            resources.ApplyResources(this.Load, "Load");
            this.Load.Name = "Load";
            this.Load.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.Load.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // courseLoadLabel
            // 
            resources.ApplyResources(this.courseLoadLabel, "courseLoadLabel");
            this.courseLoadLabel.Name = "courseLoadLabel";
            // 
            // CourseLoad
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.courseLoadLabel);
            this.Controls.Add(this.grid);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.HelpTopic = "CourseCompetitorLoad.htm";
            this.Name = "CourseLoad";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Controls.SetChildIndex(this.grid, 0);
            this.Controls.SetChildIndex(this.courseLoadLabel, 0);
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.Label courseLoadLabel;
        private System.Windows.Forms.DataGridViewTextBoxColumn CourseName;
        private new System.Windows.Forms.DataGridViewTextBoxColumn Load;
    }
}