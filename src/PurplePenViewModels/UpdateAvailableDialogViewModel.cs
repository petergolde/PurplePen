// UpdateAvailableDialogViewModel.cs
//
// ViewModel for the "an update is available" dialog. The dialog has two lives: first it presents
// the update and asks whether to install it, and then -- if the user says yes -- it turns into a
// progress dialog for the download, rather than being replaced by a second window.
//
// It carries no localized text. The caller formats the prompt and assigns it to Message, exactly as
// MessageBoxDialogViewModel is used, because the strings live in the View layer's UIText.resx.
//
// The download itself is not driven from here: it needs an IFileDownloader, a place to put the file
// and, afterwards, a way to launch an installer and exit -- all platform-specific. The Download
// command therefore only raises DownloadRequested, and AvPurplePen's UpdateManager does the work
// and drives IsDownloading and ProgressAmount from outside.

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the update-available dialog: the prompt and release notes, the two buttons,
    /// and the download progress bar that replaces them once a download starts.
    /// </summary>
    public partial class UpdateAvailableDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// The prompt, already formatted and localized by the caller. Names the available version
        /// and the one currently running, and (when there is something to download) asks whether to
        /// install it.
        /// </summary>
        [ObservableProperty]
        private string message = "";

        /// <summary>
        /// The release notes from the manifest, shown below the prompt. For an update with nothing
        /// to download this is where the user is told how to update instead — the sample manifest's
        /// "Install by doing apt-get update purple-pen.", for instance.
        /// </summary>
        [ObservableProperty]
        private string releaseMessage = "";

        /// <summary>
        /// True when the update has a file that can be downloaded and installed. When false the
        /// dialog is informational: the release message is all there is, and the only button is a
        /// close button.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsMessageOnly))]
        [NotifyPropertyChangedFor(nameof(IsAskingToDownload))]
        [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
        private bool hasDownloadableFile;

        /// <summary>
        /// The inverse of <see cref="HasDownloadableFile"/>, so the View can bind a control's
        /// visibility to it directly.
        /// </summary>
        public bool IsMessageOnly => !HasDownloadableFile;

        /// <summary>
        /// True while the dialog is asking whether to install an update that can be downloaded —
        /// that is, before any download has started. The "Remind Me Later" and "Download and
        /// Install" buttons are shown exactly when this is true. It exists so the View can bind a
        /// single property rather than combining two with a MultiBinding.
        /// </summary>
        public bool IsAskingToDownload => HasDownloadableFile && !IsDownloading;

        /// <summary>
        /// True once the download has started. The View swaps the "Remind Me Later" and
        /// "Download and Install" buttons for a progress bar and a Cancel button when this is set.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAskingToDownload))]
        [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
        private bool isDownloading;

        /// <summary>
        /// Status text shown beside the progress bar while downloading.
        /// </summary>
        [ObservableProperty]
        private string statusText = "";

        /// <summary>
        /// Progress through the download in the range [0, 1], or <c>null</c> when the server didn't
        /// say how big the file is (the bar then runs as a marquee). Follows the same pattern as
        /// <see cref="OperationInProgressDialogViewModel"/>.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProgressValue))]
        [NotifyPropertyChangedFor(nameof(IsIndeterminate))]
        private double? progressAmount;

        /// <summary>
        /// The progress value the bound ProgressBar uses, clamped to [0, 1]. Falls back to 0 when
        /// <see cref="ProgressAmount"/> is null — the bar is indeterminate then, so the value is
        /// ignored.
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
        /// True when <see cref="ProgressAmount"/> is null, which puts the progress bar into its
        /// marquee animation.
        /// </summary>
        public bool IsIndeterminate => ProgressAmount == null;

        /// <summary>
        /// Raised when the user asks for the update to be downloaded and installed. Handled by the
        /// platform layer, which owns the downloading and installing.
        /// </summary>
        public event Action? DownloadRequested;

        /// <summary>
        /// The "Download and Install" button. Disabled once a download is under way, so a second
        /// click can't start another one.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDownload))]
        private void Download()
        {
            DownloadRequested?.Invoke();
        }

        /// <summary>
        /// Whether the download command can run: only when there is a file to fetch and no download
        /// has been started yet.
        /// </summary>
        /// <returns>True if a download can be started now.</returns>
        private bool CanDownload()
        {
            return HasDownloadableFile && !IsDownloading;
        }
    }
}
