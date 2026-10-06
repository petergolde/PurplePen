namespace PurplePen
{
    partial class AddTextLine
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddTextLine));
            this.textLineLabel = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.textBoxText = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.coursesLabel = new System.Windows.Forms.Label();
            this.positionLabel = new System.Windows.Forms.Label();
            this.comboBoxPosition = new System.Windows.Forms.ComboBox();
            this.comboBoxCourses = new System.Windows.Forms.ComboBox();
            this.tableLayoutPanel1.SuspendLayout();
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
            // textLineLabel
            // 
            resources.ApplyResources(this.textLineLabel, "textLineLabel");
            this.textLineLabel.Name = "textLineLabel";
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.textLineLabel, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.textBoxText, 0, 1);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // textBoxText
            // 
            this.textBoxText.AcceptsReturn = true;
            resources.ApplyResources(this.textBoxText, "textBoxText");
            this.textBoxText.Name = "textBoxText";
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.coursesLabel, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.positionLabel, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.comboBoxPosition, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.comboBoxCourses, 1, 1);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // coursesLabel
            // 
            resources.ApplyResources(this.coursesLabel, "coursesLabel");
            this.coursesLabel.Name = "coursesLabel";
            // 
            // positionLabel
            // 
            resources.ApplyResources(this.positionLabel, "positionLabel");
            this.positionLabel.Name = "positionLabel";
            // 
            // comboBoxPosition
            // 
            resources.ApplyResources(this.comboBoxPosition, "comboBoxPosition");
            this.comboBoxPosition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPosition.FormattingEnabled = true;
            this.comboBoxPosition.Items.AddRange(new object[] {
            resources.GetString("comboBoxPosition.Items"),
            resources.GetString("comboBoxPosition.Items1")});
            this.comboBoxPosition.Name = "comboBoxPosition";
            // 
            // comboBoxCourses
            // 
            resources.ApplyResources(this.comboBoxCourses, "comboBoxCourses");
            this.comboBoxCourses.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxCourses.FormattingEnabled = true;
            this.comboBoxCourses.Items.AddRange(new object[] {
            resources.GetString("comboBoxCourses.Items"),
            resources.GetString("comboBoxCourses.Items1")});
            this.comboBoxCourses.Name = "comboBoxCourses";
            // 
            // AddTextLine
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.tableLayoutPanel2);
            this.Controls.Add(this.tableLayoutPanel1);
            this.HelpTopic = "ItemAddTextLine.htm";
            this.Name = "AddTextLine";
            this.Controls.SetChildIndex(this.tableLayoutPanel1, 0);
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.Controls.SetChildIndex(this.tableLayoutPanel2, 0);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label textLineLabel;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.TextBox textBoxText;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Label coursesLabel;
        private System.Windows.Forms.Label positionLabel;
        private System.Windows.Forms.ComboBox comboBoxPosition;
        private System.Windows.Forms.ComboBox comboBoxCourses;
    }
}