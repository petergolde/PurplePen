namespace PurplePen
{
    partial class NewEventPrintScale
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewEventPrintScale));
            this.newEventPrintScaleLabel = new System.Windows.Forms.Label();
            this.mapScaleLabel = new System.Windows.Forms.Label();
            this.defaultPrintScaleLabel = new System.Windows.Forms.Label();
            this.changeLaterLabel = new System.Windows.Forms.Label();
            this.oneToPrefixLabel1 = new System.Windows.Forms.Label();
            this.oneToPrefixLabel2 = new System.Windows.Forms.Label();
            this.labelMapScale = new System.Windows.Forms.Label();
            this.comboBoxPrintScale = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // newEventPrintScaleLabel
            // 
            resources.ApplyResources(this.newEventPrintScaleLabel, "newEventPrintScaleLabel");
            this.newEventPrintScaleLabel.Name = "newEventPrintScaleLabel";
            // 
            // mapScaleLabel
            // 
            resources.ApplyResources(this.mapScaleLabel, "mapScaleLabel");
            this.mapScaleLabel.Name = "mapScaleLabel";
            // 
            // defaultPrintScaleLabel
            // 
            resources.ApplyResources(this.defaultPrintScaleLabel, "defaultPrintScaleLabel");
            this.defaultPrintScaleLabel.Name = "defaultPrintScaleLabel";
            // 
            // changeLaterLabel
            // 
            resources.ApplyResources(this.changeLaterLabel, "changeLaterLabel");
            this.changeLaterLabel.Name = "changeLaterLabel";
            // 
            // oneToPrefixLabel1
            // 
            resources.ApplyResources(this.oneToPrefixLabel1, "oneToPrefixLabel1");
            this.oneToPrefixLabel1.Name = "oneToPrefixLabel1";
            // 
            // oneToPrefixLabel2
            // 
            resources.ApplyResources(this.oneToPrefixLabel2, "oneToPrefixLabel2");
            this.oneToPrefixLabel2.Name = "oneToPrefixLabel2";
            // 
            // labelMapScale
            // 
            resources.ApplyResources(this.labelMapScale, "labelMapScale");
            this.labelMapScale.Name = "labelMapScale";
            // 
            // comboBoxPrintScale
            // 
            this.comboBoxPrintScale.FormattingEnabled = true;
            resources.ApplyResources(this.comboBoxPrintScale, "comboBoxPrintScale");
            this.comboBoxPrintScale.Name = "comboBoxPrintScale";
            // 
            // NewEventPrintScale
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.comboBoxPrintScale);
            this.Controls.Add(this.labelMapScale);
            this.Controls.Add(this.oneToPrefixLabel2);
            this.Controls.Add(this.oneToPrefixLabel1);
            this.Controls.Add(this.changeLaterLabel);
            this.Controls.Add(this.defaultPrintScaleLabel);
            this.Controls.Add(this.mapScaleLabel);
            this.Controls.Add(this.newEventPrintScaleLabel);
            this.Name = "NewEventPrintScale";
            this.Load += new System.EventHandler(this.NewEventPrintScale_Load);
            this.VisibleChanged += new System.EventHandler(this.NewEventPrintScale_VisibleChanged);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label newEventPrintScaleLabel;
        private System.Windows.Forms.Label mapScaleLabel;
        private System.Windows.Forms.Label defaultPrintScaleLabel;
        private System.Windows.Forms.Label changeLaterLabel;
        private System.Windows.Forms.Label oneToPrefixLabel1;
        private System.Windows.Forms.Label oneToPrefixLabel2;
        private System.Windows.Forms.Label labelMapScale;
        private System.Windows.Forms.ComboBox comboBoxPrintScale;
    }
}
