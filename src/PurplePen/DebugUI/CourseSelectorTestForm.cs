using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen.DebugUI
{
    partial class CourseSelectorTestForm: DpiFixedForm
    {
        public CourseSelectorTestForm(EventDB eventDB)
        {
            InitializeComponent();
            this.courseSelector1.EventDB = eventDB;
            this.courseSelector1.ShowAllControls = true;
            this.courseSelector1.SelectedCourseDesignators = new CourseDesignator[] { CourseDesignator.AllControls, new CourseDesignator(new Id<Course>(2), 1), new CourseDesignator(new Id<Course>(3)), new CourseDesignator(new Id<Course>(6)) };
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Id<Course>[] checkedCourses = courseSelector1.SelectedCourses;

            string output = "";
            foreach (Id<Course> courseId in checkedCourses) 
                output += courseId.ToString() + "\r\n";

            outputTextBox.Text = output;
        }
    }
}