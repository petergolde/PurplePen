using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class AddForkDialog: OkCancelDialog
    {
        public AddForkDialog()
        {
            InitializeComponent();

            comboBoxNumberBranches.SelectedItem = "2";
        }

        public bool Loop
        {
            get
            {
                return radioButtonLoop.Checked;
            }

            set
            {
                radioButtonLoop.Checked = value;
                radioButtonFork.Checked = !value;
            }
        }

        public int NumberOfBranches
        {
            get
            {
                return int.Parse(comboBoxNumberBranches.SelectedItem.ToString());
            }

            set
            {
                comboBoxNumberBranches.SelectedItem = value.ToString();
            }
        }

        private void UpdateDialog()
        {
            if (Loop) {
                labelNumberLoops.Visible = true;
                labelNumberBranches.Visible = false;
                labelSummary.Text = string.Format(MiscText.LoopSummary, NumberOfBranches + 1, Factorial(NumberOfBranches));
            }
            else {
                labelNumberLoops.Visible = false;
                labelNumberBranches.Visible = true;
                labelSummary.Text = string.Format(MiscText.ForkSummary, string.Join(", ", from x in PossibleRelayParticipants(NumberOfBranches) select x.ToString()));
            }
        }

        private long Factorial(int n)
        {
            long result = 1;
            for (int i = 2; i <= n; ++i) {
                result *= i;
            }
            return result;
        }

        private List<int> PossibleRelayParticipants(int numForks)
        {
            List<int> result = new List<int>();

            for (int i = 2; i <= 20; ++i) {
                if (i % numForks == 0)
                    result.Add(i);
            }

            return result;
        }

        private void radioButtonFork_CheckedChanged(object sender, EventArgs e)
        {
            UpdateDialog();
        }

        private void comboBoxNumberBranches_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDialog();
        }

    }
}
