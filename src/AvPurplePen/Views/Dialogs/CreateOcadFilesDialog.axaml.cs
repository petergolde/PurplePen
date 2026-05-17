// CreateOcadFilesDialog.axaml.cs
//
// Code-behind for the Create OCAD Files dialog. Most of the dialog state is
// data-bound directly to the ViewModel — see CreateOcadFilesDialogViewModel
// for the property layout. The only code here is for:
//   1. Selection / variation state on the CourseSelector, which isn't
//      bindable. Handled on Opened (pull from VM) and OK (push back to VM).
//   2. The "Select folder..." button, which needs the parent window's
//      StorageProvider to show the platform folder picker.
//
// Migrated from WinForms PurplePen/CreateOcadFiles.cs.

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for creating OCAD / OpenOrienteeringMapper files. The caller
    /// must set DataContext to a <see cref="CreateOcadFilesDialogViewModel"/>
    /// (with EventDB, RestrictToFormat, DialogTitle, and Settings populated)
    /// before showing.
    /// </summary>
    public partial class CreateOcadFilesDialog : Window
    {
        public CreateOcadFilesDialog()
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
            if (DataContext is not CreateOcadFilesDialogViewModel vm)
                return;

            courseSelector.SelectedCourseDesignators = vm.SelectedCourseDesignators;
            courseSelector.VariationChoicesPerCourse = vm.VariationChoicesPerCourse;
        }

        /// <summary>
        /// Opens the platform folder picker and writes the selected folder
        /// back into the ViewModel's OutputDirectory.
        /// </summary>
        private async void SelectOtherDirectoryButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not CreateOcadFilesDialogViewModel vm)
                return;

            IStorageProvider storage = StorageProvider;

            FolderPickerOpenOptions options = new FolderPickerOpenOptions {
                Title = UIText.CreateOcadFiles_folderBrowserDialog_Description,
                AllowMultiple = false,
            };

            if (!string.IsNullOrEmpty(vm.OutputDirectory)) {
                options.SuggestedStartLocation = await storage.TryGetFolderFromPathAsync(vm.OutputDirectory);
            }

            IReadOnlyList<IStorageFolder> folders = await storage.OpenFolderPickerAsync(options);
            if (folders.Count > 0) {
                vm.OutputDirectory = folders[0].Path.LocalPath;
            }
        }

        /// <summary>
        /// Pulls the CourseSelector's selection state back into the ViewModel
        /// (this is the only part not handled by data binding) and closes with OK.
        /// </summary>
        private void CreateButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is CreateOcadFilesDialogViewModel vm) {
                vm.SelectedCourseDesignators = courseSelector.SelectedCourseDesignators;
                vm.VariationChoicesPerCourse = courseSelector.VariationChoicesPerCourse;
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
