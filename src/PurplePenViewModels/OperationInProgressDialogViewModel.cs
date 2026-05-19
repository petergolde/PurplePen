// OperationInProgressDialogViewModel.cs
//
// ViewModel for the "Operation In Progress" dialog. Used by long-running
// commands (PDF/OCAD creation, etc.) to keep the user informed while work
// runs in the background. The caller shows this dialog via
// IDialogService.ShowOwnedDialog(..., disableOwner: true), updates
// InformationLabel and ProgressAmount as work progresses, and dismisses the
// dialog via the returned ICloseableDialog when done. If the user clicks
// Cancel, the dialog closes with false — the caller can observe that via
// the Result Task and abort the operation.

using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the "Operation In Progress" dialog. Shows a description
    /// of the current operation and a progress bar that's either determinate
    /// (0..1) or marquee-style indeterminate.
    /// </summary>
    public partial class OperationInProgressDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Status text shown above the progress bar. The caller updates this
        /// as the operation moves through its phases.
        /// </summary>
        [ObservableProperty]
        private string informationLabel = "";

        /// <summary>
        /// Progress through the operation in the range [0, 1], or <c>null</c>
        /// to indicate an operation of indeterminate length (the progress bar
        /// runs as a continuous marquee animation).
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProgressValue))]
        [NotifyPropertyChangedFor(nameof(IsIndeterminate))]
        private double? progressAmount;

        /// <summary>
        /// The progress value the bound ProgressBar uses (clamped to [0, 1]).
        /// Falls back to 0 when <see cref="ProgressAmount"/> is null — the bar
        /// is in indeterminate mode in that case, so the value is ignored.
        /// </summary>
        public double ProgressValue
        {
            get {
                double v = ProgressAmount ?? 0.0;
                if (v < 0) v = 0;
                if (v > 1) v = 1;
                return v;
            }
        }

        /// <summary>
        /// True when <see cref="ProgressAmount"/> is null — bound to the
        /// ProgressBar's IsIndeterminate so a null amount becomes a marquee
        /// animation.
        /// </summary>
        public bool IsIndeterminate => ProgressAmount == null;
    }
}
