// UpdateAvailableDialog.axaml.cs
//
// Code-behind for the update-available dialog. Almost everything is bound to
// UpdateAvailableDialogViewModel; this file only translates the buttons that close the window into
// a dialog result.
//
// "Download and Install" is deliberately not handled here: it is bound to the ViewModel's
// DownloadCommand, which raises DownloadRequested for UpdateManager to act on, and the window stays
// open so it can show the download's progress.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Dialog telling the user that a newer version of Purple Pen is available. The caller must set
    /// DataContext to a <see cref="PurplePen.ViewModels.UpdateAvailableDialogViewModel"/> before
    /// showing, and is expected to show it with
    /// <see cref="PurplePen.IDialogService.ShowOwnedDialog{TViewModel}"/> so that it can dismiss the
    /// window itself once the download has finished.
    /// </summary>
    public partial class UpdateAvailableDialog : Window
    {
        public UpdateAvailableDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Closes the dialog without installing. Serves both "Remind Me Later" and, for an update
        /// with nothing to download, the close button — in neither case is there anything to do
        /// beyond dismissing the window.
        /// </summary>
        private void RemindLaterButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        /// <summary>
        /// Cancels an in-progress download. Closing the window is all that is needed: UpdateManager
        /// watches for the dialog closing and cancels the download's CancellationToken, which makes
        /// CoreUpdater clean up the partly-downloaded file.
        /// </summary>
        private void CancelButton_Click(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
