using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    partial class CourseLoad: OkCancelDialog
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
        async void ErrorMessage(string message)
        {
            await ((MainFrame) Owner).ErrorMessage(message);
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

    }
}