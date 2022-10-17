namespace PurplePen
{
    partial class MoveAllControls
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MoveAllControls));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.labelDescription = new System.Windows.Forms.Label();
            this.radioButtonMove = new System.Windows.Forms.RadioButton();
            this.radioButtonMoveAndScale = new System.Windows.Forms.RadioButton();
            this.radioButtonMoveAndRotate = new System.Windows.Forms.RadioButton();
            this.radioButtonMoveRotateScale = new System.Windows.Forms.RadioButton();
            this.tableLayoutPanel1.SuspendLayout();
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
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.labelDescription, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.radioButtonMove, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.radioButtonMoveAndScale, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.radioButtonMoveAndRotate, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.radioButtonMoveRotateScale, 0, 4);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // labelDescription
            // 
            resources.ApplyResources(this.labelDescription, "labelDescription");
            this.labelDescription.Name = "labelDescription";
            // 
            // radioButtonMove
            // 
            resources.ApplyResources(this.radioButtonMove, "radioButtonMove");
            this.radioButtonMove.Checked = true;
            this.radioButtonMove.Name = "radioButtonMove";
            this.radioButtonMove.TabStop = true;
            this.radioButtonMove.UseVisualStyleBackColor = true;
            // 
            // radioButtonMoveAndScale
            // 
            resources.ApplyResources(this.radioButtonMoveAndScale, "radioButtonMoveAndScale");
            this.radioButtonMoveAndScale.Name = "radioButtonMoveAndScale";
            this.radioButtonMoveAndScale.UseVisualStyleBackColor = true;
            // 
            // radioButtonMoveAndRotate
            // 
            resources.ApplyResources(this.radioButtonMoveAndRotate, "radioButtonMoveAndRotate");
            this.radioButtonMoveAndRotate.Name = "radioButtonMoveAndRotate";
            this.radioButtonMoveAndRotate.UseVisualStyleBackColor = true;
            // 
            // radioButtonMoveRotateScale
            // 
            resources.ApplyResources(this.radioButtonMoveRotateScale, "radioButtonMoveRotateScale");
            this.radioButtonMoveRotateScale.Name = "radioButtonMoveRotateScale";
            this.radioButtonMoveRotateScale.UseVisualStyleBackColor = true;
            // 
            // MoveAllControls
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "MoveAllControls";
            this.Controls.SetChildIndex(this.okButton, 0);
            this.Controls.SetChildIndex(this.cancelButton, 0);
            this.Controls.SetChildIndex(this.tableLayoutPanel1, 0);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label labelDescription;
        private System.Windows.Forms.RadioButton radioButtonMove;
        private System.Windows.Forms.RadioButton radioButtonMoveAndScale;
        private System.Windows.Forms.RadioButton radioButtonMoveAndRotate;
        private System.Windows.Forms.RadioButton radioButtonMoveRotateScale;
    }
}