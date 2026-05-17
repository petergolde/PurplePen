// CourseSelector.axaml.cs
//
// Avalonia port of the WinForms CourseSelector custom control. Displays a tree
// of courses with checkboxes, plus All/None selection buttons. Courses with
// variations also get a per-row "Choose Variations" button (only when
// ShowVariationChooser is true). Used by dialogs that need the user to pick
// which courses to print, export, or otherwise process.
//
// The "config" properties (EventDB, ShowAllControls, ShowCourseParts,
// ShowVariationChooser, Filter) are Avalonia StyledProperties so consumers
// can bind them in XAML. The selection / variation state remains as plain
// CLR properties for callers to read/write from code-behind on dialog
// open / OK — making them two-way bindable would require fighting the
// "default everything checked" behavior in LoadList and adding feedback-loop
// guards for each node's IsChecked change, which isn't worth the complexity
// when a couple of code-behind lines do the same job.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Custom control showing a checkbox tree of courses from an EventDB.
    /// Supports optional "All controls" node, course part sub-nodes, course
    /// filtering, and variation selection.
    /// </summary>
    public partial class CourseSelector : UserControl
    {
        // Tracks whether LoadList has populated Nodes for the current EventDB.
        // Reset when EventDB changes.
        private bool loaded;

        // Per-course variation choices selected by the user. Populated from
        // <see cref="VariationChoicesPerCourse"/> setter and updated by the
        // per-row Choose Variations button (once SelectVariations is ported).
        private Dictionary<Id<Course>, VariationChoices> variationChoicesPerCourse =
            new Dictionary<Id<Course>, VariationChoices>();

        /// <summary>Backing collection for the tree view items.</summary>
        public ObservableCollection<CourseSelectorNode> Nodes { get; } =
            new ObservableCollection<CourseSelectorNode>();

        // ===== Bindable (StyledProperty) configuration =====

        /// <summary>Avalonia property backing <see cref="EventDB"/>.</summary>
        public static readonly StyledProperty<EventDB?> EventDBProperty =
            AvaloniaProperty.Register<CourseSelector, EventDB?>(nameof(EventDB));

        /// <summary>The event database to read courses from. Bindable.</summary>
        public EventDB? EventDB
        {
            get => GetValue(EventDBProperty);
            set => SetValue(EventDBProperty, value);
        }

        /// <summary>Avalonia property backing <see cref="ShowAllControls"/>.</summary>
        public static readonly StyledProperty<bool> ShowAllControlsProperty =
            AvaloniaProperty.Register<CourseSelector, bool>(nameof(ShowAllControls));

        /// <summary>Whether to show the "All controls" node at the top of the tree.</summary>
        public bool ShowAllControls
        {
            get => GetValue(ShowAllControlsProperty);
            set => SetValue(ShowAllControlsProperty, value);
        }

        /// <summary>Avalonia property backing <see cref="ShowCourseParts"/>.</summary>
        public static readonly StyledProperty<bool> ShowCoursePartsProperty =
            AvaloniaProperty.Register<CourseSelector, bool>(nameof(ShowCourseParts));

        /// <summary>
        /// Whether to show individual course parts as child nodes when a
        /// course has multiple parts.
        /// </summary>
        public bool ShowCourseParts
        {
            get => GetValue(ShowCoursePartsProperty);
            set => SetValue(ShowCoursePartsProperty, value);
        }

        /// <summary>Avalonia property backing <see cref="ShowVariationChooser"/>.</summary>
        public static readonly StyledProperty<bool> ShowVariationChooserProperty =
            AvaloniaProperty.Register<CourseSelector, bool>(nameof(ShowVariationChooser));

        /// <summary>
        /// When true, courses with variations get a per-row "Choose Variations"
        /// button next to their checkbox.
        /// </summary>
        public bool ShowVariationChooser
        {
            get => GetValue(ShowVariationChooserProperty);
            set => SetValue(ShowVariationChooserProperty, value);
        }

        /// <summary>Avalonia property backing <see cref="Filter"/>.</summary>
        public static readonly StyledProperty<Func<CourseDesignator, bool>?> FilterProperty =
            AvaloniaProperty.Register<CourseSelector, Func<CourseDesignator, bool>?>(nameof(Filter));

        /// <summary>
        /// Optional filter predicate. When set, only courses/parts for which
        /// the filter returns true are shown.
        /// </summary>
        public Func<CourseDesignator, bool>? Filter
        {
            get => GetValue(FilterProperty);
            set => SetValue(FilterProperty, value);
        }

        public CourseSelector()
        {
            InitializeComponent();
            courseTreeView.ItemsSource = Nodes;
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            // When EventDB changes, throw away the existing tree and rebuild.
            // The other config properties only take effect on first load, so
            // they don't need to trigger a rebuild here.
            if (change.Property == EventDBProperty) {
                Nodes.Clear();
                loaded = false;
                LoadList();
            }
        }

        // ===== Non-bindable (CLR) selection / variation state =====

        /// <summary>
        /// Get or set the selected courses. Only use when ShowCourseParts is false.
        /// </summary>
        public Id<Course>[] SelectedCourses
        {
            get
            {
                CourseDesignator[] array = SelectedCourseDesignators;
                Id<Course>[] result = new Id<Course>[array.Length];
                for (int i = 0; i < array.Length; ++i)
                    result[i] = array[i].CourseId;
                return result;
            }
            set
            {
                Debug.Assert(!ShowCourseParts);

                CourseDesignator[] array = new CourseDesignator[value.Length];
                for (int i = 0; i < array.Length; ++i)
                    array[i] = new CourseDesignator(value[i]);
                SelectedCourseDesignators = array;
            }
        }

        /// <summary>
        /// Get or set whether all courses (other than All Controls) are selected.
        /// Only use when ShowCourseParts is false.
        /// </summary>
        public bool AllCoursesSelected
        {
            get
            {
                Debug.Assert(!ShowCourseParts);
                List<Id<Course>> selectedCourseIds = SelectedCourses.ToList();
                selectedCourseIds.Remove(Id<Course>.None);
                return EventDB != null && selectedCourseIds.Count == EventDB.AllCourseIds.Count;
            }
            set
            {
                Debug.Assert(!ShowCourseParts);

                List<Id<Course>> selectedCourseIds = SelectedCourses.ToList();
                bool allControls = selectedCourseIds.Contains(Id<Course>.None);
                selectedCourseIds.Clear();
                if (allControls)
                    selectedCourseIds.Add(Id<Course>.None);

                if (value && EventDB != null) {
                    selectedCourseIds.AddRange(EventDB.AllCourseIds);
                }

                SelectedCourses = selectedCourseIds.ToArray();
            }
        }

        /// <summary>
        /// Get or set the selected course designators. This is the primary way
        /// to read/write the selection, supporting both courses and course parts.
        /// </summary>
        public CourseDesignator[] SelectedCourseDesignators
        {
            get
            {
                LoadList();

                List<CourseDesignator> list = new List<CourseDesignator>();

                foreach (CourseSelectorNode node in Nodes) {
                    if (node.IsChecked) {
                        list.Add(node.Tag);
                    }
                    else {
                        foreach (CourseSelectorNode childNode in node.Children) {
                            if (childNode.IsChecked) {
                                list.Add(childNode.Tag);
                            }
                        }
                    }
                }

                return list.ToArray();
            }
            set
            {
                LoadList();

                // Uncheck all nodes first.
                foreach (CourseSelectorNode node in Nodes) {
                    node.IsChecked = false;
                }

                // Check nodes matching the provided designators.
                // Do children before parents so parent state is set correctly.
                foreach (CourseSelectorNode node in Nodes) {
                    if (Array.IndexOf(value, node.Tag) >= 0) {
                        node.IsChecked = true;
                    }
                    else {
                        foreach (CourseSelectorNode childNode in node.Children) {
                            if (Array.IndexOf(value, childNode.Tag) >= 0)
                                childNode.IsChecked = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get or set the variation choices per course. The getter returns a
        /// complete dictionary for all courses, using AllVariations as the
        /// default for courses without an explicit choice.
        /// </summary>
        public Dictionary<Id<Course>, VariationChoices> VariationChoicesPerCourse
        {
            get
            {
                Dictionary<Id<Course>, VariationChoices> result = new Dictionary<Id<Course>, VariationChoices>();
                if (EventDB != null) {
                    foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(EventDB, true)) {
                        if (variationChoicesPerCourse.TryGetValue(courseId, out VariationChoices? variationChoices)) {
                            result[courseId] = variationChoices;
                        }
                        else {
                            result[courseId] = new VariationChoices() { Kind = VariationChoices.VariationChoicesKind.AllVariations };
                        }
                    }
                }
                return result;
            }
            set
            {
                variationChoicesPerCourse.Clear();
                foreach (KeyValuePair<Id<Course>, VariationChoices> pair in value) {
                    variationChoicesPerCourse.Add(pair.Key, pair.Value);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            LoadList();
        }

        /// <summary>
        /// Populates the tree from the EventDB. Called once on first load or
        /// when SelectedCourseDesignators is accessed before the control is loaded.
        /// Re-runs if EventDB changes (see <see cref="OnPropertyChanged"/>).
        /// </summary>
        private void LoadList()
        {
            EventDB? eventDB = EventDB;
            if (eventDB == null || loaded)
                return;

            if (ShowAllControls) {
                Nodes.Add(new CourseSelectorNode(MiscText.AllControls, CourseDesignator.AllControls) {
                    IsChecked = true
                });
            }

            foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB, true)) {
                List<CourseSelectorNode>? parts = null;

                // If the course has parts, create child nodes for each part.
                int numberParts = QueryEvent.CountCourseParts(eventDB, new CourseDesignator(courseId), true);
                if (ShowCourseParts && numberParts > 1) {
                    parts = new List<CourseSelectorNode>();
                    for (int part = 0; part < numberParts; ++part) {
                        CourseDesignator partDesignator = new CourseDesignator(courseId, part);
                        if (CheckFilter(partDesignator)) {
                            parts.Add(new CourseSelectorNode(
                                string.Format(MiscText.PartN, part + 1),
                                partDesignator) {
                                IsChecked = true
                            });
                        }
                    }
                }

                // Add the course node.
                CourseDesignator designator = new CourseDesignator(courseId);
                if (CheckFilter(designator)) {
                    CourseSelectorNode node = new CourseSelectorNode(
                        eventDB.GetCourse(courseId).name,
                        designator) {
                        IsChecked = true,
                        // Show a per-row "Choose Variations" button only when the
                        // consumer asked for it AND this specific course has variations.
                        ShowVariationsButton = ShowVariationChooser
                                               && QueryEvent.HasVariations(eventDB, courseId)
                    };

                    if (parts != null) {
                        foreach (CourseSelectorNode partNode in parts) {
                            partNode.Parent = node;
                            node.Children.Add(partNode);
                        }
                    }

                    Nodes.Add(node);
                }
            }

            loaded = true;
        }

        /// <summary>Returns true if the designator passes the filter (or no filter is set).</summary>
        private bool CheckFilter(CourseDesignator designator)
        {
            Func<CourseDesignator, bool>? filter = Filter;
            if (filter == null)
                return true;
            return filter(designator);
        }

        /// <summary>Checks all top-level nodes.</summary>
        private void SelectAll_Click(object? sender, RoutedEventArgs e)
        {
            foreach (CourseSelectorNode node in Nodes)
                node.IsChecked = true;
        }

        /// <summary>Unchecks all top-level nodes.</summary>
        private void SelectNone_Click(object? sender, RoutedEventArgs e)
        {
            foreach (CourseSelectorNode node in Nodes)
                node.IsChecked = false;
        }

        /// <summary>
        /// Per-row "Choose Variations" button click handler. The button's
        /// DataContext is the <see cref="CourseSelectorNode"/> for that row,
        /// which tells us which course to show the variations dialog for.
        /// </summary>
        private void ChooseVariationsButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not CourseSelectorNode node)
                return;

            CourseDesignator courseDesignator = node.Tag;
            if (!courseDesignator.IsNotAllControls || EventDB == null)
                return;

#if PORTING
            // TODO: Port SelectVariations dialog to Avalonia and show it here.
            // Original code:
            //   SelectVariations variationsDialog = new SelectVariations(EventDB, courseDesignator.CourseId);
            //   if (variationChoicesPerCourse.TryGetValue(courseDesignator.CourseId, out VariationChoices variationChoices))
            //       variationsDialog.VariationChoices = variationChoices;
            //   if (variationsDialog.ShowDialog(this) == DialogResult.OK)
            //       variationChoicesPerCourse[courseDesignator.CourseId] = variationsDialog.VariationChoices;
#endif
        }
    }
}
