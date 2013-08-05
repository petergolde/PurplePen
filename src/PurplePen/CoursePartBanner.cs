using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

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
        public event EventHandler PropertiesClicked;

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

        public bool EnableProperties
        {
            get {
                return buttonProperties.Visible;
            }

            set {
                buttonProperties.Visible = value;
            }
        }

        public void UpdateDropdown()
        {
            UpdateNumberOfParts();
        }

        // Return selected part, or 0 for all parts.
        public int SelectedPart
        {
            get { return partComboBox.SelectedIndex - 1; }
            set {
                Debug.Assert(value >= -1 && value < numberOfParts);
                partComboBox.SelectedIndex = value + 1; 
            }
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

            if (numberOfParts > 1 && currentPart < numberOfParts)
                partComboBox.SelectedIndex = currentPart + 1;
            else
                partComboBox.SelectedIndex = 0;
            partComboBox.Enabled = (numberOfParts > 1);

            partComboBox.EndUpdate();
        }

        private void partComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (SelectedPartChanged != null)
                SelectedPartChanged(this, EventArgs.Empty);
        }

        private void CoursePartBanner_Paint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.FromKnownColor(KnownColor.ControlDark), 1)) {
                e.Graphics.DrawLine(pen, new Point(0, this.Height - 1), new Point(this.Width - 1, this.Height - 1));
            }
        }

        private void buttonProperties_Click(object sender, EventArgs e)
        {
            if (PropertiesClicked != null)
                PropertiesClicked(this, EventArgs.Empty);
        }
    }
}
