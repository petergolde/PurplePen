using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class AddTextLine: OkCancelDialog
    {
        public AddTextLine()
        {
            InitializeComponent();
        }

        public AddTextLine(string objectName, bool enableThisCourse)
            : this()
        {
            for (int i = 0; i < comboBoxPosition.Items.Count; ++i)
                comboBoxPosition.Items[i] = string.Format((string) comboBoxPosition.Items[i], objectName);
            for (int i = 0; i < comboBoxCourses.Items.Count; ++i)
                comboBoxCourses.Items[i] = string.Format((string) comboBoxCourses.Items[i], objectName);

            if (!enableThisCourse) {
                comboBoxCourses.SelectedIndex = 1;
                comboBoxCourses.Enabled = false;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string TextLine
        {
            get {
                if (textBoxText.Text == "")
                    return null;
                else
                    return textBoxText.Text.Replace("\r\n", "|"); 
            }
            set
            {
                if (value == null)
                    textBoxText.Text = "";
                else
                    textBoxText.Text = value.Replace("|", "\r\n");
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DescriptionLine.TextLineKind TextLineKind
        {
            get
            {
                if (comboBoxPosition.SelectedIndex == 0)
                    return (comboBoxCourses.SelectedIndex == 0) ? DescriptionLine.TextLineKind.BeforeCourseControl : DescriptionLine.TextLineKind.BeforeControl;
                else
                    return (comboBoxCourses.SelectedIndex == 0) ? DescriptionLine.TextLineKind.AfterCourseControl : DescriptionLine.TextLineKind.AfterControl;
            }

            set
            {
                if (value == DescriptionLine.TextLineKind.BeforeCourseControl || value == DescriptionLine.TextLineKind.BeforeControl)
                    comboBoxPosition.SelectedIndex = 0;
                else
                    comboBoxPosition.SelectedIndex = 1;

                if (value == DescriptionLine.TextLineKind.BeforeCourseControl || value == DescriptionLine.TextLineKind.AfterCourseControl)
                    comboBoxCourses.SelectedIndex = 0;
                else
                    comboBoxCourses.SelectedIndex = 1;
            }
        }
    }
}
