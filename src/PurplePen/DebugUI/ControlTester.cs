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

namespace PurplePen.DebugUI
{
    partial class ControlTester : Form
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
            foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB)) {
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

        private void descriptionControl1_Change(DescriptionControl sender, DescriptionControl.ChangeKind kind, int line, int box, object newValue)
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
