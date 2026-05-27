// ViewAdditionalCoursesDialog.axaml.cs
//
// Code-behind for the View Additional Courses dialog. On open, initializes the
// CourseSelector control from the ViewModel's properties (the filter must be set
// before EventDB, since assigning EventDB populates the tree). On OK, writes the
// selected courses back to the ViewModel before closing.
//
// Migrated from WinForms PurplePen/ViewAdditionalCourses.cs.

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for choosing additional courses to display alongside the current
    /// course. The caller must set DataContext to a
    /// ViewAdditionalCoursesDialogViewModel before showing.
    /// </summary>
    public partial class ViewAdditionalCoursesDialog : Window
    {
        public ViewAdditionalCoursesDialog()
        {
            InitializeComponent();
            Opened += OnOpened;
        }

        /// <summary>
        /// Initializes the CourseSelector from the ViewModel when the dialog
        /// opens. This must happen in Opened (not the constructor) because the
        /// DataContext is set by the caller after construction. The filter is
        /// assigned before EventDB so it is applied while the tree is built.
        /// </summary>
        private void OnOpened(object? sender, System.EventArgs e)
        {
            ViewAdditionalCoursesDialogViewModel? vm = DataContext as ViewAdditionalCoursesDialogViewModel;
            if (vm == null)
                return;

            courseSelector.Filter = vm.CourseFilter;
            courseSelector.EventDB = vm.EventDB;
            courseSelector.SelectedCourses = (vm.DisplayedCourses ?? new List<Id<Course>>()).ToArray();
        }

        /// <summary>
        /// Writes the selected courses back to the ViewModel and closes with OK.
        /// </summary>
        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            ViewAdditionalCoursesDialogViewModel? vm = DataContext as ViewAdditionalCoursesDialogViewModel;
            if (vm != null) {
                vm.DisplayedCourses = courseSelector.SelectedCourses.ToList();
            }

            Close(true);
        }

        /// <summary>
        /// Cancels and closes the dialog.
        /// </summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
