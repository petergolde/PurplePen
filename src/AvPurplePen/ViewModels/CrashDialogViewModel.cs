// CrashDialogViewModel.cs
//
// ViewModel for the dialog shown when an unhandled exception occurs.
//
// NOTE ON PLACEMENT: ViewModels normally live in the PurplePenViewModels project
// (namespace PurplePen.ViewModels), because DialogService resolves a View from a
// ViewModel by string-replacing that namespace. This one deliberately lives in
// AvPurplePen instead, for two reasons:
//
//   1. The crash dialog is never shown through DialogService. DialogService throws when
//      there is no main window (exactly the case a crash handler has to survive), is
//      annotated [RequiresUnreferencedCode], and resolves Views by reflection through the
//      DI container -- which may itself be the thing that just failed. CrashHandler
//      therefore constructs the View directly.
//   2. Keeping the whole crash-handling feature in one project makes it self-contained.
//
// AvPurplePen already references CommunityToolkit.Mvvm, so the [ObservableProperty]
// source generators work here exactly as they do in PurplePenViewModels.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using PurplePen;
using PurplePen.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AvPurplePen.ViewModels
{
    /// <summary>
    /// What the user chose to do in the crash dialog.
    /// </summary>
    public enum CrashDialogResult
    {
        /// <summary>
        /// Keep running without sending a report. This is what closing the dialog with the
        /// window's close button does, even if the "send error report" box was checked.
        /// It is also the default, so any path that dismisses the dialog without an
        /// explicit choice errs on the side of not sending anything.
        /// </summary>
        ContinueWithoutSending,

        /// <summary>Keep running, and send an error report in the background.</summary>
        Continue,

        /// <summary>Restart Purple Pen, restoring any unsaved work.</summary>
        Restart,
    }

    /// <summary>
    /// ViewModel for the crash dialog. Holds the state the dialog displays and collects,
    /// and knows how to send the error report.
    /// </summary>
    public partial class CrashDialogViewModel : ViewModelBase
    {
        // The exception being reported. Not exposed as a bindable property: the dialog only
        // ever displays ExceptionText, and the reporting code needs the exception itself.
        private readonly Exception? exception;

        /// <summary>
        /// Whether to send an error report. Checked by default, since a report that nobody
        /// sends helps nobody. Clearing it also hides the description box, because there is
        /// nowhere for that text to go.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DescriptionOpacity))]
        private bool sendErrorReport = true;

        /// <summary>
        /// What the user typed about what they were doing when the error occurred. This is
        /// the only part of the report that isn't derivable from the exception itself.
        /// </summary>
        [ObservableProperty]
        private string userDescription = "";

        /// <summary>
        /// True while the error report is being sent, so the dialog can show progress and
        /// disable its buttons. Only used on the Restart path, which has to wait for the
        /// send to finish before the process can terminate.
        /// </summary>
        [ObservableProperty]
        private bool isSending;

        /// <summary>
        /// Whether the application can actually keep running. False for exceptions caught
        /// outside the Avalonia dispatcher (a background thread, or a failure during
        /// startup), where the process is going to terminate no matter what the user picks.
        /// The dialog hides the "Continue Working" button and shows different explanatory
        /// text when this is false, rather than offering a button that can't do what it says.
        /// </summary>
        [ObservableProperty]
        private bool canContinue = true;

        /// <summary>
        /// Whether a recovery snapshot of unsaved work was successfully written, so the
        /// dialog can tell the user their work will come back if they restart.
        /// </summary>
        [ObservableProperty]
        private bool hasRecoverableChanges;

        /// <summary>
        /// The full text of the exception, shown by the error details dialog.
        /// </summary>
        public string ExceptionText { get; } = "";

        /// <summary>
        /// Opacity of the description label and text box: they are faded out rather than
        /// hidden when the checkbox is cleared, so that the dialog's layout does not jump.
        /// A double (rather than the bool) because compiled bindings require the target's
        /// exact type.
        /// </summary>
        public double DescriptionOpacity => SendErrorReport ? 1.0 : 0.0;

        /// <summary>
        /// What the user chose. Read by the crash handler after the dialog closes. Defaults
        /// to ContinueWithoutSending so that dismissing the dialog any other way (the window
        /// close button, or the dialog being destroyed) neither sends a report nor exits.
        /// </summary>
        public CrashDialogResult Result { get; set; } = CrashDialogResult.ContinueWithoutSending;

        /// <summary>
        /// Parameterless constructor, required so the Avalonia designer can instantiate the
        /// ViewModel for the View's Design.DataContext.
        /// </summary>
        public CrashDialogViewModel()
        {
        }

        /// <summary>
        /// Creates the ViewModel for a real crash.
        /// </summary>
        /// <param name="exception">The unhandled exception that occurred.</param>
        /// <param name="canContinue">
        /// Whether the application can keep running; false for fatal crashes, which hides
        /// the "Continue Working" button.
        /// </param>
        /// <param name="hasRecoverableChanges">
        /// Whether a recovery snapshot of the user's unsaved work was written successfully.
        /// </param>
        public CrashDialogViewModel(Exception exception, bool canContinue, bool hasRecoverableChanges)
        {
            this.exception = exception;

            // ToString() includes the message, the stack trace, and the whole chain of inner
            // exceptions. Captured once here, because reading it later could itself throw if
            // a custom exception type has a misbehaving ToString.
            try {
                ExceptionText = exception?.ToString() ?? "";
            }
            catch (Exception e) {
                ExceptionText = "Exception.ToString() failed: " + e.Message;
            }

            CanContinue = canContinue;
            HasRecoverableChanges = hasRecoverableChanges;
        }

        /// <summary>
        /// Sends the error report and waits for the attempt to complete. Used by the Restart
        /// button, which must not terminate the process until the report has gone out.
        /// Never throws.
        /// </summary>
        /// <param name="timeout">
        /// Hard ceiling on how long to wait. The shared HttpClient already has the standard
        /// resilience handler (retries with backoff), which can keep trying for considerably
        /// longer than a user should be made to stare at a crash dialog waiting for their
        /// application to come back.
        /// </param>
        /// <returns>True if the report was sent successfully within the timeout.</returns>
        public async Task<bool> SendReportAsync(TimeSpan timeout)
        {
            if (exception == null)
                return false;

            IsSending = true;
            try {
                StatisticsReporter reporter = Services.ServiceProvider.GetRequiredService<StatisticsReporter>();

                using CancellationTokenSource cancellation = new CancellationTokenSource(timeout);
                return await reporter.ReportExceptionAsync(exception, UserDescription, cancellation.Token);
            }
            catch (Exception) {
                // Reporting must never itself throw on the crash path.
                return false;
            }
            finally {
                IsSending = false;
            }
        }
    }
}
