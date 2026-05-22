// CreateGpxDialog.axaml.cs
//
// Code-behind for the Create GPX File dialog. Most state is data-bound to
// CreateGpxDialogViewModel. This file handles:
//   1. CourseSelector selection state (not bindable). Pulled from VM on
//      Opened, pushed back on OK.
//
// Migrated from WinForms PurplePen/CreateGpx.cs.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for creating a GPX file of course control coordinates.
    /// The caller must set DataContext to a
    /// <see cref="CreateGpxDialogViewModel"/> (with EventDB and Settings
    /// populated) before showing.
    /// </summary>
    public partial class CreateGpxDialog : Window
    {
        public CreateGpxDialog()
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
            if (DataContext is not CreateGpxDialogViewModel vm)
                return;

            courseSelector.SelectedCourses = vm.SelectedCourses;
            if (vm.AllCoursesSelected)
                courseSelector.AllCoursesSelected = true;
        }

        /// <summary>
        /// OK button. Pulls the CourseSelector's selection state back into the
        /// ViewModel and closes with OK.
        /// </summary>
        private void OkButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is CreateGpxDialogViewModel vm) {
                vm.SelectedCourses = courseSelector.SelectedCourses;
                vm.AllCoursesSelected = courseSelector.AllCoursesSelected;
            }
            Close(true);
        }

        /// <summary>Cancels and closes the dialog.</summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
