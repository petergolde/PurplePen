// ChangeSpecialCoursesDialog.axaml.cs
//
// Code-behind for the Change Displayed Courses dialog. On open, initializes
// the CourseSelector control from the ViewModel's properties. On OK, writes
// the selected courses back to the ViewModel before closing.
//
// Migrated from WinForms PurplePen/ChangeSpecialCourses.cs.

using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for changing which courses a special object is displayed on.
    /// The caller must set DataContext to a ChangeSpecialCoursesDialogViewModel
    /// before showing.
    /// </summary>
    public partial class ChangeSpecialCoursesDialog : Window
    {
        public ChangeSpecialCoursesDialog()
        {
            InitializeComponent();
            Opened += OnOpened;
        }

        /// <summary>
        /// Initializes the CourseSelector control from the ViewModel when the
        /// dialog opens. This must happen in Opened (not the constructor)
        /// because DataContext is set by the caller after construction.
        /// </summary>
        private void OnOpened(object? sender, System.EventArgs e)
        {
            ChangeSpecialCoursesDialogViewModel? vm = DataContext as ChangeSpecialCoursesDialogViewModel;
            if (vm == null)
                return;

            courseSelector.EventDB = vm.EventDB;
            courseSelector.ShowAllControls = vm.ShowAllControls;
            courseSelector.ShowCourseParts = true;
            courseSelector.SelectedCourseDesignators = vm.DisplayedCourses;
        }

        /// <summary>
        /// Writes the selected courses back to the ViewModel and closes with OK.
        /// </summary>
        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            ChangeSpecialCoursesDialogViewModel? vm = DataContext as ChangeSpecialCoursesDialogViewModel;
            if (vm != null) {
                vm.DisplayedCourses = courseSelector.SelectedCourseDesignators;
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
