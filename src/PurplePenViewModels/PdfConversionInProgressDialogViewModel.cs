// PdfConversionInProgressDialogViewModel.cs
//
// ViewModel for the "Reading PDF" dialog shown while the PDFium converter
// reads a PDF map file in the background. While the conversion runs the dialog
// shows a marquee progress bar; the user can Cancel. If the conversion fails,
// the caller calls ShowErrorMessage to switch the dialog into its error state:
// a red failure label and a read-only text box with the converter's output are
// revealed and the progress bar fills to completion. The user then dismisses
// the dialog with Cancel.
//
// Migrated from WinForms PurplePen/PdfConversionInProgress.cs.

using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the "Reading PDF" dialog. Tracks whether the conversion
    /// is still in progress (marquee progress bar) or has failed (failure
    /// message and error text revealed, progress bar full).
    /// </summary>
    public partial class PdfConversionInProgressDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// True once the conversion has failed. Drives the visibility of the
        /// failure label and error text box and switches the progress bar from
        /// its indeterminate (marquee) state to a full, determinate bar.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsIndeterminate))]
        [NotifyPropertyChangedFor(nameof(ProgressValue))]
        private bool hasError;

        /// <summary>
        /// The converter's error output, shown in the read-only error text box
        /// once <see cref="HasError"/> is true.
        /// </summary>
        [ObservableProperty]
        private string errorMessage = "";

        /// <summary>
        /// True while the conversion is in progress — bound to the progress
        /// bar's IsIndeterminate so it runs as a marquee animation. Becomes
        /// false on failure so the bar shows as full (see <see cref="ProgressValue"/>).
        /// </summary>
        public bool IsIndeterminate => !HasError;

        /// <summary>
        /// The progress bar value (range [0, 1]). Ignored while indeterminate;
        /// on failure the bar is switched to determinate and filled completely.
        /// </summary>
        public double ProgressValue => HasError ? 1.0 : 0.0;

        /// <summary>
        /// Switches the dialog into its error state: reveals the failure label
        /// and the error text box (populated with <paramref name="message"/>)
        /// and fills the progress bar.
        /// </summary>
        /// <param name="message">The converter's error output to display.</param>
        public void ShowErrorMessage(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }
    }
}
