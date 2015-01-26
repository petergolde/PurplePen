using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class MoveControlChoiceDialog : PurplePen.BaseDialog
    {
        public MoveControlChoiceDialog(string code, string courseList)
        {
            InitializeComponent();

            labelExplanation.Text = string.Format(labelExplanation.Text, code);
            duplicateButton.DetailText = string.Format(duplicateButton.DetailText, code);
            labelOtherCourses.Text = courseList;
        }

        private void moveButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Yes;
        }

        private void duplicateButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void MoveControlChoiceDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) {
                Control activeControl = this.ActiveControl;
                if (activeControl == moveButton)
                    moveButton_Click(this, EventArgs.Empty);
                else if (activeControl == duplicateButton)
                    duplicateButton_Click(this, EventArgs.Empty);
                else if (activeControl == cancelButton)
                    cancelButton_Click(this, EventArgs.Empty);
            }
            else if (e.KeyCode == Keys.Escape) {
                cancelButton_Click(this, EventArgs.Empty);
            }
        }
    }
}
