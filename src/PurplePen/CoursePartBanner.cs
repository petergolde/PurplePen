using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    /// <summary>
    /// Provides the Banner at the top of the course for selecting among different parts of a course.
    /// </summary>
    public partial class CoursePartBanner : UserControl
    {
        private int numberOfParts;

        public CoursePartBanner()
        {
            InitializeComponent();

            numberOfParts = 1;
            UpdateNumberOfParts();
        }

        public event EventHandler SelectedPartChanged;

        public int NumberOfParts
        {
            get { return numberOfParts; }

            set
            {
                if (numberOfParts != value) {
                    numberOfParts = value;
                    UpdateNumberOfParts();
                }
            }
        }

        // Return selected part, or 0 for all parts.
        public int SelectedPart
        {
            get { return partComboBox.SelectedIndex; }
        }

        private void UpdateNumberOfParts()
        {
            int currentPart = SelectedPart;

            partComboBox.BeginUpdate();
            partComboBox.Items.Clear();
            partComboBox.Items.Add(MiscText.AllParts);
            if (numberOfParts > 1) {
                for (int i = 1; i <= numberOfParts; ++i)
                    partComboBox.Items.Add(string.Format(MiscText.PartXOfY, i, numberOfParts));
            }
            partComboBox.EndUpdate();

            partComboBox.SelectedIndex = currentPart;
        }

        private void partComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SelectedPartChanged != null)
                SelectedPartChanged(this, EventArgs.Empty);
        }
    }
}
