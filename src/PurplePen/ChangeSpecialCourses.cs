using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    partial class ChangeSpecialCourses: OkCancelDialog
    {
        public ChangeSpecialCourses()
        {
            InitializeComponent();
        }

        // Get/set the event database.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public EventDB EventDB
        {
            get { return courseSelector.EventDB; }
            set { 
                courseSelector.EventDB = value; 
            }
        }

        // Get or set the courses checked in the dialog.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public CourseDesignator[] DisplayedCourses
        {
            get
            {
                return courseSelector.SelectedCourseDesignators;
            }
            set
            {
                courseSelector.SelectedCourseDesignators = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowAllControls
        {
            get { return courseSelector.ShowAllControls; }
            set { courseSelector.ShowAllControls = value; }
        }

    }
}