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
    public partial class LegAssignmentsDialog : OkCancelDialog
    {
        public event EventHandler<ValidationEventArgs> Validate;

        public LegAssignmentsDialog(List<char[]> codes)
        {
            InitializeComponent();

            bool oddGroup = false;
            foreach (char[] branchGroup in codes) {
                foreach (char c in branchGroup) {
                    DataGridViewCellStyle style = new DataGridViewCellStyle() {
                        BackColor = oddGroup ? Color.White : Color.FromArgb(0xFF, 0xd0, 0xff, 0xff)
                    };
                    int row = grid.Rows.Add(c.ToString(), "");
                    grid.Rows[row].DefaultCellStyle = style;
                }

                oddGroup = !oddGroup;
            }
        }

        protected override bool OkButtonClicked()
        {
            ValidationEventArgs eventArgs = new ValidationEventArgs(null);
            Validate?.Invoke(this, eventArgs);

            if (eventArgs.ErrorMessage != null) {
                MessageBox.Show(this, eventArgs.ErrorMessage);
                return false;
            }
            else {
                return true;
            }
        }

        public FixedBranchAssignments FixedBranchAssignments
        {
            get {
                FixedBranchAssignments fixedBranchAssignments = new FixedBranchAssignments();

                for (int row = 0; row < grid.Rows.Count; ++row) {
                    char code = ((string)(grid[0, row].Value))[0];
                    string legText = ((string)grid[1, row].Value);
                    List<int> legs = ParseLegText(legText);
                    foreach (int leg in legs) {
                        fixedBranchAssignments.AddBranchAssignment(code, leg);
                    }
                }

                return fixedBranchAssignments;
            }

            set {
                for (int row = 0; row < grid.Rows.Count; ++row) {
                    char code = ((string)(grid[0, row].Value))[0];
                    if (value.BranchIsFixed(code)) {
                        string legs = CreateLegText(value.FixedLegsForBranch(code));
                        grid[1, row].Value = legs;
                    }
                }
            }
        }

        private string CreateLegText(ICollection<int> legs)
        {
            StringBuilder builder = new StringBuilder();

            foreach (int leg in legs) {
                if (builder.Length != 0)
                    builder.Append(", ");
                builder.Append((leg + 1).ToString());
            }

            return builder.ToString();
        }

        private List<int> ParseLegText(string legText)
        {
            List<int> result = new List<int>();

            if (string.IsNullOrWhiteSpace(legText))
                return result;
            
            string[] fields = legText.Split(new[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in fields) {
                int leg;
                if (int.TryParse(s, out leg))
                    result.Add(leg - 1);
            }
            return result;
        }

        public class ValidationEventArgs: EventArgs
        {
            public String ErrorMessage;

            public ValidationEventArgs(string errorMessage)
            {
                ErrorMessage = errorMessage;
            }
        }
    }
}
