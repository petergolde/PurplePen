/* Copyright (c) 2006-2008, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    partial class CourseSelector: UserControl
    {
        private bool showAllControls;
        private EventDB eventDB;

        private bool loaded = false;

        public CourseSelector()
        {
            InitializeComponent();
        }

        public bool ShowAllControls
        {
            get
            {
                return showAllControls;
            }
            set
            {
                showAllControls = value;
            }
        }

        internal EventDB EventDB
        {
            get
            {
                return eventDB;
            }
            set
            {
                eventDB = value;
            }
        }

        // Get or set the selected courses.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public Id<Course>[] SelectedCourses
        {
            get
            {
                LoadList();
                List<Id<Course>> list = new List<Id<Course>>();
                for (int i = 0; i < courseListBox.Items.Count; ++i) {
                    if (courseListBox.GetItemChecked(i)) {
                        list.Add(((CourseItem) courseListBox.Items[i]).courseId);
                    }
                }

                return list.ToArray();
            }

            set
            {
                LoadList();
                for (int i = 0; i < courseListBox.Items.Count; ++i) {
                    Id<Course> courseId = ((CourseItem) courseListBox.Items[i]).courseId;
                    courseListBox.SetItemChecked(i, Array.IndexOf(value, courseId) >= 0);
                }
            }
        }

            private void selectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < courseListBox.Items.Count; ++i) {
                courseListBox.SetItemChecked(i, true);
            }
        }

        private void selectNone_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < courseListBox.Items.Count; ++i) {
                courseListBox.SetItemChecked(i, false);
            }
        }

        private void CourseSelector_Load(object sender, EventArgs e)
        {
            LoadList();
        }

        private void LoadList()
        {
            if (eventDB != null && !loaded) {
                if (showAllControls) {
                    courseListBox.Items.Add(new CourseItem(Id<Course>.None, MiscText.AllControls));
                }

                List<CourseItem> list = new List<CourseItem>();
                foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB)) {
                    list.Add(new CourseItem(courseId, eventDB.GetCourse(courseId).name));
                }

                foreach (CourseItem item in list) {
                    int index = courseListBox.Items.Add(item);
                    courseListBox.SetItemChecked(index, true);
                }
                loaded = true;
            }
        }

        private class CourseItem
        {
            public Id<Course> courseId;
            public string name;

            public CourseItem(Id<Course> courseId, string name)
            {
                this.courseId = courseId;
                this.name = name;
            }

            public override string ToString()
            {
                return name;
            }
        }
    }
}
