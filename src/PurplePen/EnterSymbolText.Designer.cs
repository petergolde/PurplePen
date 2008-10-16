namespace PurplePen
{
    partial class EnterSymbolText
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EnterSymbolText));
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOk = new System.Windows.Forms.Button();
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.numberColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.genderColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.textColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.checkBoxPlural = new System.Windows.Forms.CheckBox();
            this.checkBoxGender = new System.Windows.Forms.CheckBox();
            this.labelDescription = new System.Windows.Forms.Label();
            this.labelGenderChooser = new System.Windows.Forms.Label();
            this.comboBoxGenderChooser = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize) (this.dataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonCancel
            // 
            resources.ApplyResources(this.buttonCancel, "buttonCancel");
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // buttonOk
            // 
            resources.ApplyResources(this.buttonOk, "buttonOk");
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
            // 
            // dataGridView
            // 
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.AllowUserToDeleteRows = false;
            this.dataGridView.AllowUserToResizeColumns = false;
            this.dataGridView.AllowUserToResizeRows = false;
            resources.ApplyResources(this.dataGridView, "dataGridView");
            this.dataGridView.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.numberColumn,
            this.genderColumn,
            this.textColumn});
            this.dataGridView.MultiSelect = false;
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.RowHeadersVisible = false;
            this.dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dataGridView.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.dataGridView_CellValidating);
            // 
            // numberColumn
            // 
            this.numberColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            resources.ApplyResources(this.numberColumn, "numberColumn");
            this.numberColumn.Name = "numberColumn";
            this.numberColumn.ReadOnly = true;
            this.numberColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // genderColumn
            // 
            this.genderColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            resources.ApplyResources(this.genderColumn, "genderColumn");
            this.genderColumn.Name = "genderColumn";
            this.genderColumn.ReadOnly = true;
            this.genderColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // textColumn
            // 
            this.textColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            resources.ApplyResources(this.textColumn, "textColumn");
            this.textColumn.Name = "textColumn";
            this.textColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // checkBoxPlural
            // 
            resources.ApplyResources(this.checkBoxPlural, "checkBoxPlural");
            this.checkBoxPlural.Name = "checkBoxPlural";
            this.checkBoxPlural.UseVisualStyleBackColor = true;
            this.checkBoxPlural.CheckedChanged += new System.EventHandler(this.checkBoxPluralOrGender_CheckedChanged);
            // 
            // checkBoxGender
            // 
            resources.ApplyResources(this.checkBoxGender, "checkBoxGender");
            this.checkBoxGender.Name = "checkBoxGender";
            this.checkBoxGender.UseVisualStyleBackColor = true;
            this.checkBoxGender.CheckedChanged += new System.EventHandler(this.checkBoxPluralOrGender_CheckedChanged);
            // 
            // labelDescription
            // 
            resources.ApplyResources(this.labelDescription, "labelDescription");
            this.labelDescription.Name = "labelDescription";
            // 
            // labelGenderChooser
            // 
            resources.ApplyResources(this.labelGenderChooser, "labelGenderChooser");
            this.labelGenderChooser.Name = "labelGenderChooser";
            // 
            // comboBoxGenderChooser
            // 
            this.comboBoxGenderChooser.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxGenderChooser.FormattingEnabled = true;
            resources.ApplyResources(this.comboBoxGenderChooser, "comboBoxGenderChooser");
            this.comboBoxGenderChooser.Name = "comboBoxGenderChooser";
            // 
            // EnterSymbolText
            // 
            this.AcceptButton = this.buttonOk;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.buttonCancel;
            this.Controls.Add(this.comboBoxGenderChooser);
            this.Controls.Add(this.labelGenderChooser);
            this.Controls.Add(this.labelDescription);
            this.Controls.Add(this.checkBoxGender);
            this.Controls.Add(this.checkBoxPlural);
            this.Controls.Add(this.dataGridView);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOk);
            this.HelpButton = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EnterSymbolText";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Shown += new System.EventHandler(this.EnterSymbolText_Shown);
            ((System.ComponentModel.ISupportInitialize) (this.dataGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.DataGridView dataGridView;
        private System.Windows.Forms.CheckBox checkBoxPlural;
        private System.Windows.Forms.CheckBox checkBoxGender;
        private System.Windows.Forms.Label labelDescription;
        private System.Windows.Forms.Label labelGenderChooser;
        private System.Windows.Forms.ComboBox comboBoxGenderChooser;
        private System.Windows.Forms.DataGridViewTextBoxColumn numberColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn genderColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn textColumn;
    }
}