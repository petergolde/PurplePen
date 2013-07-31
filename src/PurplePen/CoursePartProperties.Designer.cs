namespace PurplePen
{
    partial class CoursePartProperties
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CoursePartProperties));
            this.checkBoxDisplayFinish = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
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
            // checkBoxDisplayFinish
            // 
            resources.ApplyResources(this.checkBoxDisplayFinish, "checkBoxDisplayFinish");
            this.checkBoxDisplayFinish.Name = "checkBoxDisplayFinish";
            this.checkBoxDisplayFinish.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // CoursePartProperties
            // 
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.label1);
            this.Controls.Add(this.checkBoxDisplayFinish);
            this.HelpTopic = "CoursePartProperties.htm";
            this.Name = "CoursePartProperties";
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.Controls.SetChildIndex(this.checkBoxDisplayFinish, 0);
            this.Controls.SetChildIndex(this.label1, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBoxDisplayFinish;
        private System.Windows.Forms.Label label1;
    }
}
