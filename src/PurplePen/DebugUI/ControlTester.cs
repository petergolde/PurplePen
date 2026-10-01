using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen.DebugUI
{
    partial class ControlTester : DpiFixedForm
    {
        SymbolDB symbolDB;
        EventDB eventDB;
        CourseView courseView;

        class CourseItem
        {
            private EventDB eventDB;

            public Id<Course> id;

            public CourseItem(EventDB eventDB, Id<Course> id)
            {
                this.eventDB = eventDB;
                this.id = id;
            }

            public override string ToString()
            {
                if (id.IsNone)
                    return "All Controls";
                else
                    return string.Format("{0} - {1}", id, eventDB.GetCourse(id).name);
            }
        }

        public ControlTester()
        {
            InitializeComponent();
        }

        public void Initialize(EventDB eventDB, SymbolDB symbolDB)
        {
            eventDB.Validate();

            this.eventDB = eventDB;
            this.symbolDB = symbolDB;
            descriptionControl1.SymbolDB = symbolDB;

            listBoxCourses.Items.Add(new CourseItem(eventDB, Id<Course>.None));
            foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB, true)) {
                listBoxCourses.Items.Add(new CourseItem(eventDB, courseId));
            }

            listBoxCourses.SelectedIndex = 0;
        }

        private DescriptionLine[] GetDescription()
        {
            CourseItem courseItem = (CourseItem)(listBoxCourses.SelectedItem);
            Id<Course> id;

            if (courseItem == null)
                id = Id<Course>.None;
            else
                id = courseItem.id;

            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(id));

            DescriptionFormatter descFormatter = new DescriptionFormatter(courseView, symbolDB, DescriptionFormatter.Purpose.ForUI);
            return descFormatter.CreateDescription(false);
        }

        private void listBoxCourses_SelectedIndexChanged(object sender, EventArgs e)
        {
            descriptionControl1.Description = GetDescription();
            descriptionControl1.CourseKind = courseView.Kind;
        }

        private void descriptionControl1_Change(DescriptionControl sender, DescriptionChangeKind kind, int line, int box, object newValue)
        {
            this.eventLabel.Text = string.Format("Change: {0}", kind);
            lineLabel.Text = string.Format("Line: {0}", line);
            boxLabel.Text = string.Format("Box: {0}", box);
            if (newValue == null)
                newValueLabel.Text = "New Value: no symbol";
            else if (newValue is Symbol)
                newValueLabel.Text = string.Format("New Value: Symbol {0}", ((Symbol)newValue).Id);
            else
                newValueLabel.Text = String.Format("New Value: '{0}", (string)newValue);
        }
    }
}
