using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class MapIssueChoiceDialog : PurplePen.BaseDialog
    {
        public MapIssueChoiceDialog()
        {
            InitializeComponent();
        }

        private MapIssueKind mapIssueKind;

        public MapIssueKind MapIssueKind {
            get { return mapIssueKind; }
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            mapIssueKind = MapIssueKind.Beginning;
            DialogResult = DialogResult.OK;
        }

        private void middleButton_Click(object sender, EventArgs e)
        {
            mapIssueKind = MapIssueKind.Middle;
            DialogResult = DialogResult.OK;
        }

        private void startTriangleButton_Click(object sender, EventArgs e)
        {
            mapIssueKind = MapIssueKind.End;
            DialogResult = DialogResult.OK;
        }

        private void MoveControlChoiceDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) {
                Control activeControl = this.ActiveControl;
                if (activeControl == startButton)
                    startButton_Click(this, EventArgs.Empty);
                else if (activeControl == middleButton)
                    middleButton_Click(this, EventArgs.Empty);
                else if (activeControl == startTriangleButton)
                    startTriangleButton_Click(this, EventArgs.Empty);
            }
            else if (e.KeyCode == Keys.Escape) {
                DialogResult = DialogResult.Cancel;
            }
        }
    }
}
