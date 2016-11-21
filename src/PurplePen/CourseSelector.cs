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
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

namespace PurplePen
{
    partial class CourseSelector: UserControl
    {
        private bool showAllControls;
        private bool showCourseParts;
        private EventDB eventDB;
        private bool eventHasVariations = false;

        private bool loaded = false;

        private Dictionary<Id<Course>, VariationChoices> variationChoicesPerCourse = new Dictionary<Id<Course>, VariationChoices>();

        public CourseSelector()
        {
            InitializeComponent();

            buttonChooseVariations.Enabled = false;
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

        public bool ShowCourseParts
        {
            get
            {
                return showCourseParts;
            }
            set
            {
                showCourseParts = value;
            }
        }

        // Show the button to choose variations if any courses have variations.
        public bool ShowVariationChooser { get; set; }

        internal EventDB EventDB
        {
            get
            {
                return eventDB;
            }
            set
            {
                eventDB = value;

                // Check to see if any course has variations.
                eventHasVariations = false;
                foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB)) {
                    if (QueryEvent.HasVariations(eventDB, courseId))
                        eventHasVariations = true;
                }
            }
        }

        // Get or set the selected courses.
        // Only use if ShowCourseParts is false.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public Id<Course>[] SelectedCourses
        {
            get {
                CourseDesignator[] array = SelectedCourseDesignators;
                Id<Course>[] result = new Id<Course>[array.Length];
                for (int i = 0; i < array.Length; ++i)
                    result[i] = array[i].CourseId;
                return result;
            }

            set
            {
                Debug.Assert(!this.ShowCourseParts);

                CourseDesignator[] array = new CourseDesignator[value.Length];
                for (int i = 0; i < array.Length; ++i)
                    array[i] = new CourseDesignator(value[i]);
                SelectedCourseDesignators = array;
            }
        }

        // Get or set if all courses (other than all controls) is set.
        // Only use if ShowCourseParts is false.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public bool AllCoursesSelected
        {
            get
            {
                Debug.Assert(!this.ShowCourseParts);
                List<Id<Course>> selectedCourseIds = SelectedCourses.ToList();
                selectedCourseIds.Remove(Id<Course>.None);
                return selectedCourseIds.Count == eventDB.AllCourseIds.Count;
            }
            set
            {
                Debug.Assert(!this.ShowCourseParts);

                List<Id<Course>> selectedCourseIds = SelectedCourses.ToList();
                bool allControls = selectedCourseIds.Contains(Id<Course>.None);
                selectedCourseIds.Clear();
                if (allControls)
                    selectedCourseIds.Add(Id<Course>.None);

                if (value) {
                    selectedCourseIds.AddRange(eventDB.AllCourseIds);
                }

                SelectedCourses = selectedCourseIds.ToArray();
            }
        }

        // Get or set the selected courses designator
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), Browsable(false)]
        public CourseDesignator[] SelectedCourseDesignators
        {
            get
            {
                LoadList();

                List<CourseDesignator> list = new List<CourseDesignator>();

                foreach (TreeNode node in courseTreeView.Nodes) {
                    if (node.Checked) {
                        list.Add((CourseDesignator)(node.Tag));
                    }
                    else {
                        foreach (TreeNode childNode in node.Nodes) {
                            if (childNode.Checked) {
                                list.Add((CourseDesignator)(childNode.Tag));
                            }
                        }
                    }
                }

                return list.ToArray();
            }

            set
            {
                LoadList();

                foreach (TreeNode node in courseTreeView.Nodes) {
                    node.Checked = false;
                }

                // Do children before parents.
                foreach (TreeNode node in courseTreeView.Nodes) {
                    if (Array.IndexOf(value, ((CourseDesignator)(node.Tag))) >= 0)
                        node.Checked = true;
                    else {
                        foreach (TreeNode childNode in node.Nodes) {
                            if (Array.IndexOf(value, ((CourseDesignator)(childNode.Tag))) >= 0)
                                childNode.Checked = true;
                        }
                    }
                }
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Dictionary<Id<Course>, VariationChoices> VariationChoicesPerCourse
        {
            get {
                Dictionary<Id<Course>, VariationChoices> result = new Dictionary<Id<Course>, VariationChoices>();
                foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB)) {
                    VariationChoices variationChoices;
                    // Create dictionary with all courses, getting default for ones that were not changed.
                    if (variationChoicesPerCourse.TryGetValue(courseId, out variationChoices)) {
                        result[courseId] = variationChoices;
                    }
                    else {
                        result[courseId] = new VariationChoices() { Kind = VariationChoices.VariationChoicesKind.AllVariations };
                    }
                }

                return result;
            }
            set {
                variationChoicesPerCourse.Clear();
                foreach (var pair in value) {
                    variationChoicesPerCourse.Add(pair.Key, pair.Value);
                }
            }
        }

        private void selectAll_Click(object sender, EventArgs e)
        {
            foreach (TreeNode node in courseTreeView.Nodes)
                node.Checked = true;
        }

        private void selectNone_Click(object sender, EventArgs e)
        {
            foreach (TreeNode node in courseTreeView.Nodes)
                node.Checked = false;
        }

        private void CourseSelector_Load(object sender, EventArgs e)
        {
            LoadList();
        }

        private void LoadList()
        {
            if (eventDB != null && !loaded) {
                if (showAllControls) {
                    courseTreeView.Nodes.Add(new TreeNode(MiscText.AllControls) {Tag = CourseDesignator.AllControls});
                }

                foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB)) {
                    TreeNode[] parts = null;

                    // If the course has parts, get all the parts.
                    int numberParts = QueryEvent.CountCourseParts(eventDB, courseId);
                    if (showCourseParts && numberParts > 1) {
                        parts = new TreeNode[numberParts];
                        for (int part = 0; part < numberParts; ++part) {
                            parts[part] = new TreeNode(string.Format(MiscText.PartN, part + 1))
                            {
                                Tag = new CourseDesignator(courseId, part),
                                Checked = true
                            };
                        }
                    }

                    // Add node for the course to the tree.
                    TreeNode node;
                    if (parts != null)
                        node = new TreeNode(eventDB.GetCourse(courseId).name, parts);
                    else
                        node = new TreeNode(eventDB.GetCourse(courseId).name);

                    node.Tag = new CourseDesignator(courseId);
                    node.Checked = true;
                    courseTreeView.Nodes.Add(node);
                }

                courseTreeView.ExpandAll();

                buttonChooseVariations.Visible = (ShowVariationChooser && eventHasVariations);

                loaded = true;
            }
        }

        // Prevent tree nodes from being collapsed.
        private void courseTreeView_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            e.Cancel = true;
        }

        private int inCheckUpdating = 0;

        private void courseTreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (inCheckUpdating == 0) {
                inCheckUpdating++;

                TreeNode node = e.Node;
                if (node.Level == 0)
                    UpdateChildNodes(node);
                else
                    UpdateNodeBasedOnChildren(node.Parent);

                inCheckUpdating--;
            }
        }

        // Set all children to checked/unchecked based on the current node state.
        void UpdateChildNodes(TreeNode node)
        {
            bool isChecked = node.Checked;
            foreach (TreeNode childNode in node.Nodes)
                childNode.Checked = isChecked;
        }

        // Update parent node to checked iff all children are checked.
        void UpdateNodeBasedOnChildren(TreeNode parent)
        {
            bool anyUnchecked = false;

            foreach (TreeNode childNode in parent.Nodes)
                if (!childNode.Checked)
                    anyUnchecked = true;

            parent.Checked = !anyUnchecked;
        }

        private void buttonChooseVariations_Click(object sender, EventArgs e)
        {
            CourseDesignator courseDesignator = courseTreeView.SelectedNode.Tag as CourseDesignator;
            if (courseDesignator != null) {
                SelectVariations variationsDialog = new SelectVariations(eventDB, courseDesignator.CourseId);

                VariationChoices variationChoices;
                if (variationChoicesPerCourse.TryGetValue(courseDesignator.CourseId, out variationChoices)) {
                    variationsDialog.VariationChoices = variationChoices;
                }

                if (variationsDialog.ShowDialog(this) == DialogResult.OK) {
                    variationChoicesPerCourse[courseDesignator.CourseId] = variationsDialog.VariationChoices;
                }
            }
        }

        private void courseTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            bool courseHasVariations = false;
            CourseDesignator courseDesignator = courseTreeView.SelectedNode.Tag as CourseDesignator;
            if (courseDesignator != null && courseDesignator.IsNotAllControls) {
                courseHasVariations = QueryEvent.HasVariations(eventDB, courseDesignator.CourseId);
            }

            buttonChooseVariations.Enabled = courseHasVariations;
        }
    }
}
