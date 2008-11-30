/* Copyright (c) 2006-2008, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

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
        void ErrorMessage(string message)
        {
            ((MainFrame) Owner).ErrorMessage(message);
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
                ((MainFrame) Owner).ErrorMessage(reason);
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
                    ((MainFrame) Owner).WarningMessage(reason);
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