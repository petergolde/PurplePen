// CrashDialog.axaml.cs
//
// Code-behind for the crash dialog. Most of the dialog is data-bound; the code here
// handles the three things that can't be: recording which button was pressed, opening the
// nested error-details dialog, and waiting for the error report to be sent before letting
// the Restart path close the window.
//
// The dialog is shown directly by CrashHandler rather than through DialogService, and the
// result is read from the ViewModel's Result property rather than from the window's dialog
// result -- CrashHandler may have to show this window without an owner (if the crash
// happened before any window existed), and an ownerless window has no dialog result.

using Avalonia.Controls;
using Avalonia.Interactivity;
using AvPurplePen.ViewModels;
using System;

namespace AvPurplePen.Views
{
    /// <summary>
    /// The dialog shown when an unhandled exception occurs. The caller must set DataContext
    /// to a <see cref="CrashDialogViewModel"/> before showing.
    /// </summary>
    public partial class CrashDialog : Window
    {
        /// <summary>
        /// How long to wait for the error report to be sent before restarting anyway. The
        /// shared HttpClient already retries with backoff, which can run considerably longer
        /// than a user should be made to wait for their application to come back.
        /// </summary>
        private static readonly TimeSpan sendReportTimeout = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Initializes the dialog and its components.
        /// </summary>
        public CrashDialog()
        {
            InitializeComponent();

            // Focus the description box, because typing what happened is the one thing only
            // the user can do. Done from Opened rather than the constructor, which runs before
            // the controls can take focus.
            Opened += (s, e) => descriptionTextBox.Focus();

            Closing += CrashDialog_Closing;
        }

        /// <summary>
        /// Prevents the window from being closed while an error report is in flight, which
        /// would abandon the HTTP request the Restart path is waiting on. The Restart handler
        /// closes the window itself once the send completes.
        /// </summary>
        private void CrashDialog_Closing(object? sender, WindowClosingEventArgs e)
        {
            if (DataContext is CrashDialogViewModel { IsSending: true })
                e.Cancel = true;
        }

        /// <summary>
        /// Continues without restarting. Sends an error report only if the checkbox is
        /// checked; the report itself is sent in the background by the crash handler so the
        /// user isn't kept waiting to get back to work.
        /// </summary>
        private void ContinueButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is CrashDialogViewModel vm) {
                vm.Result = vm.SendErrorReport ? CrashDialogResult.Continue
                                               : CrashDialogResult.ContinueWithoutSending;
            }

            Close();
        }

        /// <summary>
        /// Restarts Purple Pen. If an error report is being sent, waits for it to finish
        /// first: the crash handler terminates this process as soon as the dialog closes, so
        /// closing early would kill the request in flight.
        /// </summary>
        private async void RestartButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not CrashDialogViewModel vm) {
                Close();
                return;
            }

            vm.Result = CrashDialogResult.Restart;

            if (vm.SendErrorReport) {
                // Setting IsSending shows the progress bar and disables both buttons. It also
                // makes CrashDialog_Closing block the window's close button for the duration,
                // so the user cannot dismiss the dialog out from under the request.
                await vm.SendReportAsync(sendReportTimeout);
            }

            Close();
        }

        /// <summary>
        /// Shows the raw exception text in a nested modal dialog. This works while the crash
        /// handler is blocked on its nested dispatcher frame, because that frame keeps
        /// pumping the message queue.
        /// </summary>
        private async void ViewDetailsButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not CrashDialogViewModel vm)
                return;

            try {
                ErrorDetailsDialog details = new ErrorDetailsDialog {
                    DataContext = new ErrorDetailsDialogViewModel { ExceptionText = vm.ExceptionText }
                };

                await details.ShowDialog(this);
            }
            catch (Exception) {
                // The application is already in an unknown state. Failing to show the details
                // is not worth throwing a second exception out of the crash dialog over -- the
                // user can still describe the problem and send the report.
            }
        }
    }
}
