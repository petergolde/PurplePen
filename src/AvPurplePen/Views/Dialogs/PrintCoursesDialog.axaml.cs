// PrintCoursesDialog.axaml.cs
//
// Code-behind for the Print Courses dialog. Most state is data-bound to
// PrintCoursesDialogViewModel — see the ViewModel for the property layout.
// This file handles:
//   1. CourseSelector selection (not bindable). Pulled from VM on Opened,
//      pushed back on Print and Preview.
//   2. Checking that at least one course is selected before printing or
//      previewing, with an error message if not.
//   3. The Preview… button, which pushes the CourseSelector selection into
//      the ViewModel and then runs its PreviewCommand.
//
// Migrated from WinForms PurplePen/PrintCourses.cs.

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for printing courses. The caller must set DataContext to a
    /// <see cref="PrintCoursesDialogViewModel"/> (with EventDB, DocumentPrinting,
    /// ShowMergeParts, and Settings populated) before showing.
    /// </summary>
    public partial class PrintCoursesDialog : Window
    {
        public PrintCoursesDialog()
        {
            InitializeComponent();
            Opened += OnOpened;
        }

        /// <summary>
        /// Push the ViewModel's initial selection state into the CourseSelector.
        /// Done in Opened (not the constructor) because DataContext is set by
        /// the caller after construction.
        /// </summary>
        private void OnOpened(object? sender, EventArgs e)
        {
            if (DataContext is not PrintCoursesDialogViewModel vm)
                return;

            courseSelector.SelectedCourseDesignators = vm.SelectedCourseDesignators;
            courseSelector.VariationChoicesPerCourse = vm.VariationChoicesPerCourse;
        }

        /// <summary>
        /// Print button. Pulls the CourseSelector's selection state back into the
        /// ViewModel and closes with OK, unless no courses are selected.
        /// </summary>
        private async void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not PrintCoursesDialogViewModel vm) {
                Close(false);
                return;
            }

            PushSelectionToViewModel(vm);
            if (await CheckSomeCoursesSelected(vm))
                Close(true);
        }

        /// <summary>
        /// Preview… — pushes the current course selection into the ViewModel
        /// (the CourseSelector isn't bindable), then shows the Print Preview
        /// dialog via the ViewModel's PreviewCommand.
        /// </summary>
        private async void PreviewButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not PrintCoursesDialogViewModel vm)
                return;

            PushSelectionToViewModel(vm);
            if (await CheckSomeCoursesSelected(vm) && vm.PreviewCommand.CanExecute(null))
                vm.PreviewCommand.Execute(null);
        }

        /// <summary>Cancels and closes the dialog.</summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        /// <summary>
        /// Copies the CourseSelector's selection state (which isn't bindable)
        /// into the ViewModel.
        /// </summary>
        private void PushSelectionToViewModel(PrintCoursesDialogViewModel vm)
        {
            vm.SelectedCourseDesignators = courseSelector.SelectedCourseDesignators;
            vm.VariationChoicesPerCourse = courseSelector.VariationChoicesPerCourse;
        }

        /// <summary>
        /// Returns true if at least one course is selected. Otherwise shows an
        /// error message (modal to this dialog) and returns false.
        /// </summary>
        private static async Task<bool> CheckSomeCoursesSelected(PrintCoursesDialogViewModel vm)
        {
            if (vm.SelectedCourseDesignators.Length > 0)
                return true;

            MessageBoxDialogViewModel messageVm = new MessageBoxDialogViewModel {
                Message = MiscText.NoCoursesSelected,
                Buttons = MessageBoxButtons.Ok,
                DefaultButton = MessageBoxButton.Ok,
                Icon = MessageBoxIcon.Error
            };
            await Services.DialogService.ShowDialogAsync(messageVm);
            return false;
        }
    }
}
