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
    partial class CourseLoad: Form
    {
        public Controller.CourseLoadInfo[] courseLoads;

        public CourseLoad()
        {
            InitializeComponent();
        }

        public void SetCourseLoads(Controller.CourseLoadInfo[] loads)
        {
            courseLoads = loads;

            for (int i = 0; i < loads.Length; ++i) {
                string loadString;
                if (loads[i].load < 0)
                    loadString = "";
                else
                    loadString = loads[i].load.ToString();

                grid.Rows.Add(loads[i].courseName, loadString);
            }
        }

        public Controller.CourseLoadInfo[] GetCourseLoads()
        {
            for (int i = 0; i < courseLoads.Length; ++i) {
                string loadString = (string) grid[1, i].Value;
                LoadFromString(loadString, out courseLoads[i].load);
            }

            return courseLoads;
        }

        public bool LoadFromString(string loadString, out int load)
        {
            if (loadString == null)
                loadString = "";

            string s = loadString.Trim();
            if (s == "") {
                load = -1;
                return true;
            }
            else {
                return int.TryParse(s, out load);
            }
        }

        // Show an error message.
        void ErrorMessage(string message)
        {
            ((MainFrame) Owner).ErrorMessage(message);
        }

        // When entering a load, validate that it is an integer 0-999999, or blank.
        private void grid_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex == 1) {
                string loadString = e.FormattedValue.ToString();
                int load;

                if (! LoadFromString(loadString, out load)) {
                    ErrorMessage(MiscText.BadLoad);
                    e.Cancel = true;
                }
            }
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void CourseLoad_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            Util.ShowHelpTopic(this, "CourseCompetitorLoad.htm");
            e.Cancel = true;
        }
    }
}