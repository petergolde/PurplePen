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

        /// <summary>
        /// The window that currently acts as the application's main window, used
        /// as the root owner for modal dialogs. Resolved dynamically from the
        /// desktop lifetime (rather than captured once) because the main window
        /// changes when the welcome screen hands off to the real main window.
        /// </summary>
        private static Window RootOwner
        {
            get {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                        && desktop.MainWindow != null) {
                    return desktop.MainWindow;
                }
                throw new InvalidOperationException("DialogService requires a desktop main window to own dialogs.");
            }
        }

        /// <summary>
        /// Shows a modal dialog for the given ViewModel. The owner is disabled
        /// until the dialog closes. Returns true if the dialog was accepted
        /// (the View called <c>Close(true)</c>), false otherwise.
        /// </summary>
        public async Task<bool> ShowDialogAsync<TViewModel>(TViewModel viewModel) where TViewModel : class
        {
            // Special case: FileOpenSingleViewModel uses the platform file-open
            // picker rather than a custom View.
            if (viewModel is FileOpenSingleViewModel fileOpenVm) {
                return await ShowFileOpenSingleAsync(fileOpenVm);
            }

            // Same pattern for file-save: use the platform save picker.
            if (viewModel is FileSaveViewModel fileSaveVm) {
                return await ShowFileSaveAsync(fileSaveVm);
            }

            // ShowDialog<bool?> picks up whatever the View passes to
            // Window.Close(object dialogResult). ShowOwnedDialog can't deliver
            // that bool (its Task is non-generic) so we use ShowDialog directly
            // here, sharing only the View resolution.
            Window dialog = CreateDialogWindow(viewModel);
            bool? result = await dialog.ShowDialog<bool?>(GetActiveOwner());
            return result == true;
        }

        /// <summary>
        /// Shows an owned dialog for the given ViewModel and returns
        /// immediately with an <see cref="INonModalDialog{TViewModel}"/> handle
        /// the caller can use to dismiss the dialog and observe when it closes.
        /// See <see cref="IDialogService.ShowOwnedDialog{TViewModel}"/> for the
        /// full contract.
        /// </summary>
        public INonModalDialog<TViewModel> ShowOwnedDialog<TViewModel>(
            TViewModel viewModel, bool disableOwner) where TViewModel : class
        {
            Window dialog = CreateDialogWindow(viewModel);
            Window owner = GetActiveOwner();

            Task task;
            if (disableOwner) {
                // Classic modal: Avalonia's ShowDialog disables the owner and
                // returns a Task that completes when the dialog closes.
                // Task<bool?> derives from Task so we can hand it back directly.
                task = dialog.ShowDialog<bool?>(owner);
            }
            else {
                // Owned but non-modal: Show(owner) doesn't return a Task at all,
                // so we wire one up ourselves via the Closed event. Subscribe
                // before Show so we never miss a same-tick close.
                TaskCompletionSource tcs = new TaskCompletionSource();
                void OnClosed(object? s, EventArgs e)
                {
                    dialog.Closed -= OnClosed;
                    tcs.TrySetResult();
                }
                dialog.Closed += OnClosed;
                dialog.Show(owner);
                task = tcs.Task;
            }

            return new DialogHandle<TViewModel>(dialog, viewModel, task);
        }

        /// <summary>
        /// Resolves the View type for the given ViewModel via the same naming
        /// convention as <see cref="ViewLocator"/>, instantiates it as a
        /// <see cref="Window"/>, and sets the DataContext.
        /// </summary>
        private static Window CreateDialogWindow<TViewModel>(TViewModel viewModel) where TViewModel : class
        {
            string viewModelName = typeof(TViewModel).FullName!;
            string viewName = viewModelName
                .Replace("PurplePen.ViewModels", "AvPurplePen.Views", StringComparison.Ordinal)
                .Replace("ViewModel", "", StringComparison.Ordinal);

            Type? viewType = ViewAssembly.GetType(viewName);
            if (viewType == null) {
                throw new InvalidOperationException(
                    $"Could not find View type '{viewName}' for ViewModel '{viewModelName}'.");
            }

            if (Activator.CreateInstance(viewType) is not Window dialog) {
                throw new InvalidOperationException(
                    $"View type '{viewName}' is not a Window.");
            }

            dialog.DataContext = viewModel;
            return dialog;
        }

        /// <summary>
        /// Single <see cref="INonModalDialog{TViewModel}"/> implementation that
        /// works for both modal and non-modal dialogs — the difference between
        /// the two is encapsulated in how the <see cref="ClosedTask"/> was
        /// built by <see cref="ShowOwnedDialog{TViewModel}"/>, not here.
        /// </summary>
        private sealed class DialogHandle<TViewModel> : INonModalDialog<TViewModel>
            where TViewModel : class
        {
            private readonly Window window;
            public TViewModel ViewModel { get; }
            public Task ClosedTask { get; }

            // Set to true inside Close(). Stays false when the dialog closes
            // for any other reason (Cancel button, title-bar X), so the
            // caller can distinguish those cases after ClosedTask completes.
            public bool ClosedProgrammatically { get; private set; }

            public DialogHandle(Window window, TViewModel viewModel, Task closedTask)
            {
                this.window = window;
                ViewModel = viewModel;
                ClosedTask = closedTask;
            }

            public void Close()
            {
                // Window.Close() on an already-closed window is harmless, but
                // skipping the call keeps ClosedProgrammatically from getting
                // flipped on after a user-initiated close — which would lie to
                // the caller.
                if (!ClosedTask.IsCompleted) {
                    ClosedProgrammatically = true;
                    window.Close();
                }
            }
        }

        /// <summary>
        /// Finds the topmost currently-open dialog so a newly-shown dialog is
        /// parented to it (and is therefore modal relative to it), instead of
        /// always being parented to the main window. Walks the chain of owned
        /// windows starting from <see cref="RootOwner"/>.
        /// </summary>
        private static Window GetActiveOwner()
        {
            // RootOwner guarantees the desktop lifetime exists, so this cast holds.
            IClassicDesktopStyleApplicationLifetime desktop = (IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;

            Window current = RootOwner;
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
            IStorageProvider storage = RootOwner.StorageProvider;

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
        /// Shows the platform file-save dialog. Translates
        /// <see cref="FileSaveViewModel"/> options into Avalonia's
        /// <see cref="FilePickerSaveOptions"/>, then uses
        /// <see cref="IStorageProvider.SaveFilePickerWithResultAsync"/> so we
        /// can report back which filter the user committed under (the
        /// regular <c>SaveFilePickerAsync</c> doesn't expose that).
        /// </summary>
        private async Task<bool> ShowFileSaveAsync(FileSaveViewModel viewModel)
        {
            IStorageProvider storage = RootOwner.StorageProvider;

            // Accept either ".ppen" or "ppen" from the caller — Avalonia
            // expects the bare extension without a leading dot.
            string defaultExtension = viewModel.DefaultExtension ?? "";
            if (defaultExtension.StartsWith('.'))
                defaultExtension = defaultExtension.Substring(1);

            List<FilePickerFileType> fileTypes = ParseFileFilters(viewModel.FileFilters);

            FilePickerSaveOptions options = new FilePickerSaveOptions {
                Title = viewModel.Title,
                FileTypeChoices = fileTypes,
                DefaultExtension = defaultExtension,
                ShowOverwritePrompt = viewModel.ShowOverwritePrompt,
                SuggestedFileName = viewModel.SuggestedFileName,
            };

            // Note: Avalonia's FilePickerSaveOptions doesn't have an
            // "initially active filter" property — the platform picks the
            // initial filter based on DefaultExtension. We respect the
            // user's chosen filter on the way out and write the new index
            // back to FileFilterIndex below.

            if (viewModel.InitialDirectory != null) {
                options.SuggestedStartLocation = await storage.TryGetFolderFromPathAsync(viewModel.InitialDirectory);
            }

            SaveFilePickerResult result = await storage.SaveFilePickerWithResultAsync(options);

            if (result.File == null) {
                viewModel.SelectedFile = null;
                return false;
            }

            viewModel.SelectedFile = result.File.Path.LocalPath;

            // Write back which filter the user committed under. Match by
            // reference — the same FilePickerFileType instances we passed in
            // are what come back. Fall back to leaving FileFilterIndex
            // unchanged if Avalonia returned a different instance.
            if (result.SelectedFileType != null) {
                int chosenIndex = fileTypes.IndexOf(result.SelectedFileType);
                if (chosenIndex >= 0)
                    viewModel.FileFilterIndex = chosenIndex + 1;
            }

            return true;
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
