using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    partial class AutoNumbering
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

        private FlowLayoutPanel flowLayoutPanel1;
        private Label startingCodeLabel;
        private NumericUpDown startingCodeNumericUpDown;
        private CheckBox disallowInvertibleCheckBox;
        private GroupBox existingControlsGroupBox;
        private RadioButton renumberExistingRadioButton;
        private RadioButton newControlsOnlyRadioButton;
        private Label automaticNumberingLabel;

    }
}