using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class ViewAdditionalCourses : OkCancelDialog
    {
        public ViewAdditionalCourses(string courseName, Id<Course> currentCourse)
        {
            InitializeComponent();
            courseSelector.Filter = (designator => designator.CourseId != currentCourse);
            labelInstructions.Text = string.Format(labelInstructions.Text, courseName);
        }

        // Get/set the event database.
        public EventDB EventDB {
            get { return courseSelector.EventDB; }
            set {
                courseSelector.EventDB = value;
            }
        }

        // Get or set the courses checked in the dialog.
        public List<Id<Course>> DisplayedCourses {
            get {
                return (from cs in courseSelector.SelectedCourseDesignators select cs.CourseId).ToList();
            }
            set {
                if (value == null)
                    courseSelector.SelectedCourseDesignators = new CourseDesignator[0];
                else
                    courseSelector.SelectedCourseDesignators = (from id in value select new CourseDesignator(id)).ToArray();
            }
        }
    }
}
