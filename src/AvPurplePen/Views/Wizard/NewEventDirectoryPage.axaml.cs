// NewEventDirectoryPage.axaml.cs
// Wizard page 6: where to save the event file. The "Choose Folder..." button
// uses the platform folder picker (which needs the TopLevel's StorageProvider),
// so it lives in code-behind rather than the ViewModel.
// Migrated from WinForms PurplePen/NewEventDirectory.cs.

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PurplePen.ViewModels;

namespace AvPurplePen.Views.Wizard
{
    /// <summary>Wizard page for choosing the output directory. Binds to NewEventWizardViewModel.</summary>
    public partial class NewEventDirectoryPage : UserControl
    {
        public NewEventDirectoryPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Opens the platform folder picker and writes the chosen folder back
        /// into the ViewModel's DirectoryName.
        /// </summary>
        private async void ChooseFolderButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not NewEventWizardViewModel vm)
                return;

            TopLevel? topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
                return;

            IStorageProvider storage = topLevel.StorageProvider;

            FolderPickerOpenOptions options = new FolderPickerOpenOptions {
                Title = UIText.NewEventDirectory_folderBrowserDialog_Description,
                AllowMultiple = false,
            };

            if (!string.IsNullOrEmpty(vm.DirectoryName)) {
                options.SuggestedStartLocation = await storage.TryGetFolderFromPathAsync(vm.DirectoryName);
            }

            IReadOnlyList<IStorageFolder> folders = await storage.OpenFolderPickerAsync(options);
            if (folders.Count > 0) {
                vm.DirectoryName = folders[0].Path.LocalPath;
            }
        }
    }
}
