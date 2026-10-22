// PrintPreviewDialogViewModel.cs
//
// ViewModel for the Print Preview dialog. The dialog is generic: it shows the
// pages recorded in a RecordingPrintTarget, so it can preview anything that
// prints through an IPrintingTarget (descriptions, punch cards, courses, ...).
//
// Typical usage:
//
//     using (RecordingPrintTarget printTarget = new RecordingPrintTarget()) {
//         if (controller.PrintDescriptions(printTarget, settings, paperSizeWithMargins)) {
//             PrintPreviewDialogViewModel vm = new PrintPreviewDialogViewModel { PrintTarget = printTarget };
//             await Services.DialogService.ShowDialogAsync(vm);
//         }
//     }
//
// The caller owns the RecordingPrintTarget and disposes it after the dialog
// closes. (Any background rendering still in progress at that time finishes
// safely; see RecordingPrintTarget.)
//
// This ViewModel handles page navigation only. Zooming and panning are pure
// view behavior and live in the PrintPageViewer control.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PurplePen.Graphics2D;
using System;
using System.Collections.Generic;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Print Preview dialog. The caller sets
    /// <see cref="PrintTarget"/> (already printed to) and optionally
    /// <see cref="ColorConverter"/>, then shows the dialog.
    /// </summary>
    public partial class PrintPreviewDialogViewModel : ViewModelBase
    {
        // ===== Inputs (set by caller before showing) =====

        /// <summary>
        /// The print target holding the recorded pages to preview. Setting it
        /// resets the current page to the first page.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Pages))]
        [NotifyPropertyChangedFor(nameof(PageCount))]
        [NotifyPropertyChangedFor(nameof(HasPages))]
        [NotifyPropertyChangedFor(nameof(DocumentTitle))]
        private RecordingPrintTarget? printTarget;

        /// <summary>
        /// Converts CMYK colors to display colors when drawing the pages. Null
        /// uses the rendering backend's default (simple) conversion, which
        /// matches RGB PDF output.
        /// </summary>
        public IColorConverter? ColorConverter { get; set; }

        // ===== UI state =====

        /// <summary>
        /// Zero-based index of the page being shown, or -1 if there are no
        /// pages. Bound two-way to the page thumbnail list.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentPage))]
        [NotifyPropertyChangedFor(nameof(CurrentPageNumber))]
        [NotifyCanExecuteChangedFor(nameof(FirstPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(LastPageCommand))]
        private int currentPageIndex = -1;

        // ===== Computed properties =====

        /// <summary>The recorded pages, in order. Empty if there is no print target.</summary>
        public IReadOnlyList<RecordedPrintPage> Pages =>
            PrintTarget?.Pages ?? (IReadOnlyList<RecordedPrintPage>)Array.Empty<RecordedPrintPage>();

        /// <summary>The number of pages.</summary>
        public int PageCount => Pages.Count;

        /// <summary>True if there is at least one page to show.</summary>
        public bool HasPages => PageCount > 0;

        /// <summary>The title of the printed document.</summary>
        public string DocumentTitle => PrintTarget?.DocumentTitle ?? "";

        /// <summary>The page being shown, or null if there are no pages.</summary>
        public RecordedPrintPage? CurrentPage =>
            (CurrentPageIndex >= 0 && CurrentPageIndex < PageCount) ? Pages[CurrentPageIndex] : null;

        /// <summary>
        /// One-based number of the page being shown (0 if there are no pages).
        /// Settable so a page-number box can be bound to it two-way; out of
        /// range values are clamped.
        /// </summary>
        public int CurrentPageNumber
        {
            get => CurrentPageIndex + 1;
            set {
                if (HasPages)
                    CurrentPageIndex = Math.Clamp(value, 1, PageCount) - 1;
            }
        }

        // Reset to the first page whenever the print target changes.
        partial void OnPrintTargetChanged(RecordingPrintTarget? value)
        {
            CurrentPageIndex = HasPages ? 0 : -1;
        }

        // ===== Page navigation commands =====

        /// <summary>Go to the first page.</summary>
        [RelayCommand(CanExecute = nameof(CanGoBack))]
        private void FirstPage() => CurrentPageIndex = 0;

        /// <summary>Go to the previous page.</summary>
        [RelayCommand(CanExecute = nameof(CanGoBack))]
        private void PreviousPage() => CurrentPageIndex -= 1;

        /// <summary>Go to the next page.</summary>
        [RelayCommand(CanExecute = nameof(CanGoForward))]
        private void NextPage() => CurrentPageIndex += 1;

        /// <summary>Go to the last page.</summary>
        [RelayCommand(CanExecute = nameof(CanGoForward))]
        private void LastPage() => CurrentPageIndex = PageCount - 1;

        // True if there is a page before the current one.
        private bool CanGoBack() => HasPages && CurrentPageIndex > 0;

        // True if there is a page after the current one.
        private bool CanGoForward() => HasPages && CurrentPageIndex < PageCount - 1;
    }
}
