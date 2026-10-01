using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class ChangeAllCodes: OkCancelDialog
    {
        private object[] codeKeys;
        private EventDB eventDB;
        private bool ignoreCellChanges;

        public ChangeAllCodes()
        {
            ignoreCellChanges = true;
            InitializeComponent();
            ignoreCellChanges = false;
        }

        internal void SetEventDB(EventDB eventDB)
        {
            this.eventDB = eventDB;
        }

        // Get or set the codes.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public KeyValuePair<object, string>[] Codes
        {
            get
            {
                KeyValuePair<object, string>[] codes = new KeyValuePair<object, string>[codeKeys.Length];

                for (int i = 0; i < codeKeys.Length; ++i) {
                    codes[i] = new KeyValuePair<object, string>(codeKeys[i], grid[1, i].Value.ToString());
                }

                return codes;
            }
            set
            {
                codeKeys = new object[value.Length];

                ignoreCellChanges = true;
                for (int i = 0; i < value.Length; ++i) {
                    codeKeys[i] = value[i].Key;
                    grid.Rows.Add(value[i].Value, value[i].Value);
                }
                ignoreCellChanges = false;
            }
        }

        // Show an error message.
        async void ErrorMessage(string message)
        {
            await ((MainFrame) Owner).ErrorMessage(message);
        }

        // Show an warning message.
        async void WarningMessage(string message)
        {
            await ((MainFrame)Owner).WarningMessage(message);
        }

        // Update the formatting so that changed codes are displayed in red.
        private void grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == 1) {
                // formatting the new code.
                string newCode = (string) (e.Value);
                string oldCode = (string) (grid[0, e.RowIndex].Value);
                if (newCode != oldCode)
                    e.CellStyle.ForeColor = Color.Red;
            }
        }

        // When entering a code, make sure that it is valid.
        private void grid_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            string newCode = e.FormattedValue.ToString();
            string reason;

            if (!QueryEvent.IsLegalControlCode(newCode, out reason)) {
                // The code isn't valid. Disallow.
                ErrorMessage(reason);
                e.Cancel = true;
            }
        }

        // Change for non-preferred codes.
        private void grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (!ignoreCellChanges) {
                string newValue = grid[e.ColumnIndex, e.RowIndex].FormattedValue.ToString();
                string reason;

                QueryEvent.IsPreferredControlCode(eventDB, newValue, out reason);
                if (reason != null)
                    WarningMessage(reason);
            }
        }

        // Check for duplicate codes. Return the row number of a duplicate code if found, else -1.
        int FindDuplicateCodes()
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();        // dictionary to map code string to row number.

            for (int row = 0; row < grid.Rows.Count; ++row) {
                string code = (string) (grid[1, row].Value);
                if (dict.ContainsKey(code))
                    return dict[code];         // already present, return the row number.
                else
                    dict[code] = row;
            }

            return -1;     // no problem.
        }

        protected override bool OkButtonClicked()
        {
            // Check for duplicate codes.
            int duplicateRow = FindDuplicateCodes();

            if (duplicateRow >= 0) {
                // A duplicate was found.
                ErrorMessage(string.Format(MiscText.DuplicateCode, grid[1, duplicateRow].Value));
                grid.CurrentCell = grid[1, duplicateRow];
                return false;
            }
            else {
                return true;
            }
        }
    }
}