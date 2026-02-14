using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Windows.Forms;

namespace PurplePen
{
    partial class CreateGpx : PurplePen.OkCancelDialog
    {
        GpxCreationSettings settings;
        internal Controller controller;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public GpxCreationSettings CreationSettings
        {
            get {
                UpdateSettings();
                return settings; 
            }
            set
            {
                settings = value;
                UpdateDialog();
            }
        }

        // CONSDER: shouldn't take an eventDB. Should instead take a pair of CourseViewData/name or some such.
        public CreateGpx(EventDB eventDB)
        {
            InitializeComponent();
            courseSelector.EventDB = eventDB;
        }

        // Update the dialog with information from the settings.
        void UpdateDialog()
        {
            if (settings.CourseIds == null) {
                List<Id<Course>> courseList = courseSelector.SelectedCourses.ToList();
                courseList.Add(Id<Course>.None);
                courseSelector.SelectedCourses = courseList.ToArray();
            }
            else {
                courseSelector.SelectedCourses = settings.CourseIds;
            }
            if (settings.AllCourses)
                courseSelector.AllCoursesSelected = true;

            namePrefixTextBox.Text = settings.CodePrefix;
        }

        // Update the settings with information from the dialog.
        void UpdateSettings()
        {
            // Courses.
            settings.CourseIds = courseSelector.SelectedCourses;
            settings.AllCourses = courseSelector.AllCoursesSelected;
            settings.CodePrefix = namePrefixTextBox.Text;
        }


    }
}
