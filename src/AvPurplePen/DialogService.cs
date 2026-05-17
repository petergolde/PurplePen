// DialogService.cs
//
// Implementation of IDialogService for Avalonia.
// Uses the ViewLocator convention to resolve ViewModel types to View types,
// then shows the View as a modal dialog.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using PurplePen;
using PurplePen.ViewModels;

namespace AvPurplePen
{
    /// <summary>
    /// Shows modal dialogs by resolving Views from ViewModels using the
    /// same naming convention as <see cref="ViewLocator"/>:
    /// PurplePen.ViewModels.FooViewModel → AvPurplePen.Views.FooView.
    /// </summary>
    [RequiresUnreferencedCode("DialogService uses reflection to resolve View types from ViewModel types.")]
    public class DialogService : IDialogService
    {
        private static readonly Assembly ViewAssembly = typeof(DialogService).Assembly;

        private readonly Window ownerWindow;

        /// <summary>
        /// Creates a new DialogService that shows dialogs owned by the given window.
        /// </summary>
        /// <param name="ownerWindow">The parent window for modal dialogs.</param>
        public DialogService(Window ownerWindow)
        {
            this.ownerWindow = ownerWindow;
        }

        /// <summary>
        /// Shows a modal dialog for the given ViewModel.
        /// The View is resolved via the naming convention FooViewModel → FooView.
        /// The ViewModel is set as the View's DataContext before showing.
        /// </summary>
        public async Task<bool> ShowDialogAsync<TViewModel>(TViewModel viewModel) where TViewModel : class
        {
            // Special case: FileOpenSingleViewModel uses the platform file-open dialog
            // rather than a custom View.
            if (viewModel is FileOpenSingleViewModel fileOpenVm) {
                return await ShowFileOpenSingleAsync(fileOpenVm);
            }

            // Resolve the View type from the ViewModel type using the same convention as ViewLocator.
            string viewModelName = typeof(TViewModel).FullName!;
            string viewName = viewModelName
                .Replace("PurplePen.ViewModels", "AvPurplePen.Views", StringComparison.Ordinal)
                .Replace("ViewModel", "", StringComparison.Ordinal);

            Type? viewType = ViewAssembly.GetType(viewName);
            if (viewType == null) {
                throw new InvalidOperationException($"Could not find View type '{viewName}' for ViewModel '{viewModelName}'.");
            }

            if (Activator.CreateInstance(viewType) is not Window dialog) {
                throw new InvalidOperationException($"View type '{viewName}' is not a Window.");
            }

            dialog.DataContext = viewModel;
            bool? result = await dialog.ShowDialog<bool?>(GetActiveOwner());
            return result == true;
        }

        /// <summary>
        /// Finds the topmost currently-open dialog so a newly-shown dialog is
        /// parented to it (and is therefore modal relative to it), instead of
        /// always being parented to MainWindow. Walks the chain of owned
        /// windows starting from <see cref="ownerWindow"/>.
        /// </summary>
        private Window GetActiveOwner()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) {
                return ownerWindow;
            }

            Window current = ownerWindow;
            while (true) {
                // Find a visible window whose Owner is `current`. Each Avalonia
                // dialog opened with ShowDialog(owner) records that owner on
                // the new window, so this chain walks down to whichever dialog
                // is on top right now.
                Window? owned = null;
                foreach (Window w in desktop.Windows) {
                    if (w != current && w.IsVisible && ReferenceEquals(w.Owner, current)) {
                        owned = w;
                        break;
                    }
                }
                if (owned == null)
                    return current;
                current = owned;
            }
        }

        /// <summary>
        /// Shows the platform file-open dialog for selecting a single file.
        /// Translates <see cref="FileOpenSingleViewModel"/> options into Avalonia's
        /// <see cref="FilePickerOpenOptions"/> and writes the result back to the ViewModel.
        /// </summary>
        private async Task<bool> ShowFileOpenSingleAsync(FileOpenSingleViewModel viewModel)
        {
            IStorageProvider storage = ownerWindow.StorageProvider;

            FilePickerOpenOptions options = new FilePickerOpenOptions {
                AllowMultiple = false,
                Title = viewModel.Title,
                FileTypeFilter = ParseFileFilters(viewModel.FileFilters)
            };

            // Set the initially selected filter (1-based index).
            int zeroBasedIndex = viewModel.InitialFileFilterIndex - 1;
            if (zeroBasedIndex >= 0 && zeroBasedIndex < options.FileTypeFilter.Count) {
                options.SuggestedFileType = options.FileTypeFilter[zeroBasedIndex];
            }

            if (viewModel.InitialDirectory != null) {
                options.SuggestedStartLocation = await storage.TryGetFolderFromPathAsync(viewModel.InitialDirectory);
            }

            IReadOnlyList<IStorageFile> files = await storage.OpenFilePickerAsync(options);

            if (files.Count > 0) {
                viewModel.SelectedFile = files[0].Path.LocalPath;
                return true;
            }

            viewModel.SelectedFile = null;
            return false;
        }

        /// <summary>
        /// Parses a Windows-style file filter string (e.g. "Purple Pen files|*.ppen|All files|*.*")
        /// into a list of <see cref="FilePickerFileType"/> for Avalonia's file picker.
        /// The filters are returned in the same order as specified in the string.
        /// </summary>
        /// <param name="filterString">
        /// Pipe-delimited pairs of display name and pattern, e.g. "Name1|*.ext1|Name2|*.ext2".
        /// </param>
        /// <returns>A list of file type filters for the Avalonia file picker.</returns>
        internal static List<FilePickerFileType> ParseFileFilters(string filterString)
        {
            List<FilePickerFileType> filters = new List<FilePickerFileType>();

            if (string.IsNullOrEmpty(filterString)) {
                return filters;
            }

            string[] parts = filterString.Split('|');

            // Each filter is a pair: display name, then pattern(s).
            for (int i = 0; i + 1 < parts.Length; i += 2) {
                string name = parts[i];
                // Patterns can be semicolon-separated (e.g. "*.jpg;*.png").
                string[] patterns = parts[i + 1].Split(';');
                filters.Add(new FilePickerFileType(name) { Patterns = patterns });
            }

            return filters;
        }
    }
}
