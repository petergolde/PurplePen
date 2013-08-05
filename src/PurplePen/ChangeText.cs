using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class ChangeText: OkCancelDialog
    {
        public ChangeText()
        {
            InitializeComponent();
        }

        public ChangeText(string title, string explanation, bool allowSpecialTextInsert)
            : this()
        {
            this.Text = title;
            this.usageLabel.Text = explanation;
            if (!allowSpecialTextInsert)
                insertSpecialButton.Visible = false;

            textBoxMain_TextChanged(this, EventArgs.Empty);
        }

        public string UserText
        {
            set
            {
                textBoxMain.Text = value;
            }
            get
            {
                return textBoxMain.Text;
            }
        }

        void InsertSpecialText(string specialText)
        {
            textBoxMain.Paste(specialText);
            textBoxMain.Focus();
        }

        private void insertSpecialButton_Click(object sender, EventArgs e)
        {
            specialTextMenu.Show(insertSpecialButton, new Point(0, insertSpecialButton.Height), ToolStripDropDownDirection.BelowRight);
        }

        private void eventTitleMenuItem_Click(object sender, EventArgs e)
        {
            InsertSpecialText(TextMacros.EventTitle);
        }

        private void courseNameMenuItem_Click(object sender, EventArgs e)
        {
            InsertSpecialText(TextMacros.CourseName);
        }

        private void coursePartMenuItem_Click(object sender, EventArgs e)
        {
            InsertSpecialText(TextMacros.CoursePart);
        }

        private void courseLengthMenuItem_Click(object sender, EventArgs e)
        {
            InsertSpecialText(TextMacros.CourseLength);
        }

        private void courseClimbMenuItem_Click(object sender, EventArgs e)
        {
            InsertSpecialText(TextMacros.CourseClimb);
        }

        private void classListMenuItem_Click(object sender, EventArgs e)
        {
            InsertSpecialText(TextMacros.ClassList);
        }

        private void printScaleMenuItem_Click(object sender, EventArgs e)
        {
            InsertSpecialText(TextMacros.PrintScale);
        }

        private void textBoxMain_TextChanged(object sender, EventArgs e)
        {
            okButton.Enabled = textBoxMain.Text != "";
        }
    }
}
