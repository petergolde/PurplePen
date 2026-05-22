// CreateKmlFilesDialog.axaml.cs
//
// Code-behind for the Create KML Files dialog. Most of the dialog state is
// data-bound directly to the ViewModel — see CreateKmlFilesDialogViewModel
// for the property layout. The code here handles:
//   1. Selection / variation state on the CourseSelector, which isn't
//      bindable. Handled on Opened (pull from VM) and OK (push back to VM).
//   2. The "Select folder..." button, which needs the parent window's
//      StorageProvider to show the platform folder picker.
//   3. The Files combo box, whose items are localized display strings
//      that map to KmlFileCreation enum values on the ViewModel.
//
// Migrated from WinForms PurplePen/CreateKmlFiles.cs.

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PurplePen;
using PurplePen.ViewModels;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog for creating KML files. The caller must set DataContext to a
    /// <see cref="CreateKmlFilesDialogViewModel"/> (with EventDB and Settings
    /// populated) before showing.
    /// </summary>
    public partial class CreateKmlFilesDialog : Window
    {
        /// <summary>
        /// One entry in the file-creation combo: a localized display string
        /// paired with the underlying <see cref="ExportKmlSettings.KmlFileCreation"/>
        /// value.
        /// </summary>
        private class FileCreationItem
        {
            public string DisplayName { get; }
            public ExportKmlSettings.KmlFileCreation Value { get; }

            public FileCreationItem(string displayName, ExportKmlSettings.KmlFileCreation value)
            {
                DisplayName = displayName;
                Value = value;
            }

            public override string ToString() => DisplayName;
        }

        private readonly FileCreationItem[] fileCreationItems;

        public CreateKmlFilesDialog()
        {
            InitializeComponent();

            fileCreationItems = new FileCreationItem[] {
                new FileCreationItem(UIText.CreateKmlFiles_filesCombo_Items, ExportKmlSettings.KmlFileCreation.SingleFile),
                new FileCreationItem(UIText.CreateKmlFiles_filesCombo_Items1, ExportKmlSettings.KmlFileCreation.FilePerCourse),
            };
            filesCombo.ItemsSource = fileCreationItems;

            Opened += OnOpened;
        }

        /// <summary>
        /// Push the ViewModel's initial selection state into the CourseSelector
        /// and the files combo. Done in Opened (not the constructor) because
        /// DataContext is set by the caller after construction.
        /// </summary>
        private void OnOpened(object? sender, EventArgs e)
        {
            if (DataContext is not CreateKmlFilesDialogViewModel vm)
                return;

            courseSelector.SelectedCourseDesignators = vm.SelectedCourseDesignators;
            courseSelector.VariationChoicesPerCourse = vm.VariationChoicesPerCourse;

            foreach (FileCreationItem item in fileCreationItems) {
                if (item.Value == vm.FileCreation) {
                    filesCombo.SelectedItem = item;
                    break;
                }
            }
        }

        /// <summary>
        /// Opens the platform folder picker and writes the selected folder
        /// back into the ViewModel's OutputDirectory.
        /// </summary>
        private async void SelectOtherDirectoryButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not CreateKmlFilesDialogViewModel vm)
                return;

            IStorageProvider storage = StorageProvider;

            FolderPickerOpenOptions options = new FolderPickerOpenOptions {
                Title = UIText.CreateKmlFiles_folderBrowserDialog_Description,
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
        /// Pulls the CourseSelector's selection state and the files combo
        /// selection back into the ViewModel, then closes with OK.
        /// </summary>
        private void CreateButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is CreateKmlFilesDialogViewModel vm) {
                vm.SelectedCourseDesignators = courseSelector.SelectedCourseDesignators;
                vm.VariationChoicesPerCourse = courseSelector.VariationChoicesPerCourse;

                if (filesCombo.SelectedItem is FileCreationItem selected) {
                    vm.FileCreation = selected.Value;
                }
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
