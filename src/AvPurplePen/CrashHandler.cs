// CrashHandler.cs
//
// Catches unhandled exceptions from everywhere an Avalonia application can catch them,
// shows the crash dialog, sends an error report, and restarts with the user's unsaved
// work when they ask for it.
//
// The overriding rule in this file is that nothing in here may throw. It runs when the
// process is already in an unknown state, and an exception escaping the crash handler
// means the user loses their work with no explanation at all. Every entry point is
// therefore wrapped in a try/catch, and every helper degrades to a safe default rather
// than propagating a failure.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using AvPurplePen.ViewModels;
using AvPurplePen.Views;
using Microsoft.Extensions.DependencyInjection;
using PurplePen;
using PurplePen.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AvPurplePen
{
    /// <summary>
    /// Installs and implements the application's unhandled-exception handling.
    /// </summary>
    internal static class CrashHandler
    {
        /// <summary>
        /// The most crash dialogs to show in one run of the application. Past this, further
        /// crashes are reported silently. A user who has already seen three crash dialogs has
        /// made their point; continuing to interrupt them is just noise.
        /// </summary>
        private const int maxDialogsPerSession = 10;

        /// <summary>
        /// How long the same failure is suppressed after the user has dismissed a dialog for it.
        ///
        /// This exists to collapse a burst: a fault in a render or idle handler throws on every
        /// frame, and without this the user would face an endless series of dialogs. The window
        /// is deliberately short, because the other case matters too -- a user who hits a bug,
        /// continues working, and then hits the same bug again by repeating what they did should
        /// get the dialog again. Silently swallowing that is worse than showing it twice: the
        /// application would simply stop responding to that action with no explanation.
        ///
        /// Measured from when the dialog was dismissed, not when it was shown, so that sitting
        /// on the dialog for a while does not let a storm resume the moment it is closed.
        /// </summary>
        private static readonly TimeSpan repeatSuppressionWindow = TimeSpan.FromSeconds(10);

        /// <summary>
        /// How long to wait for a report to go out when there is no UI to show progress in
        /// (the fatal-startup and no-UI-thread paths).
        /// </summary>
        private static readonly TimeSpan silentReportTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// How long a terminating background-thread crash waits for the UI thread to show the
        /// dialog. Generous, because the user may be reading it, but bounded, so that a wedged
        /// or already-dead UI thread cannot leave the process hanging forever.
        /// </summary>
        private static readonly TimeSpan uiThreadHandoffTimeout = TimeSpan.FromMinutes(5);

        // Guards all the mutable state below. Held only briefly, never across showing the
        // dialog, so a nested crash can still take it and be recorded as suppressed.
        private static readonly object gate = new object();

        // True while the crash dialog is on screen. Further exceptions in that window are
        // swallowed rather than stacking up more dialogs.
        private static bool dialogActive;

        // Latched if the crash handler itself fails. Once set, we stop trying: whatever is
        // broken is broken badly enough that another attempt would just recurse.
        private static bool crashHandlerFailed;

        // How many crash dialogs have been shown so far this run. Note that this limits
        // dialogs only; the number of reports actually sent is capped separately, inside
        // StatisticsReporter.ReportExceptionAsync, which every send path goes through.
        private static int dialogsShownThisSession;

        // When a dialog for a given failure signature was last dismissed, so that a burst of
        // the same fault produces one dialog instead of an endless storm of them. See
        // repeatSuppressionWindow.
        private static readonly Dictionary<string, DateTime> signatureLastDismissed =
            new Dictionary<string, DateTime>(StringComparer.Ordinal);

        /// <summary>
        /// Installs the process-wide exception handlers. Called from Program.Main before any
        /// other initialization, so that a failure during startup is still caught.
        ///
        /// The Avalonia dispatcher does not exist yet at this point, so the dispatcher-level
        /// handlers are installed separately by <see cref="InstallDispatcherHandlers"/>.
        /// </summary>
        public static void InstallProcessHandlers()
        {
            if (SkipInstallation)
                return;

            try {
                AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
                TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            }
            catch (Exception) {
                // If we can't even attach handlers there is nothing else to be done; the
                // application runs with its previous (nonexistent) crash behavior.
            }
        }

        /// <summary>
        /// Installs the Avalonia dispatcher's exception handlers. Called from App.Initialize,
        /// which is the earliest point at which the dispatcher exists.
        ///
        /// This is the important one: essentially everything the user does arrives through the
        /// dispatcher, so this is the handler that catches ordinary bugs, and the only one
        /// from which the application can actually keep running afterwards.
        /// </summary>
        public static void InstallDispatcherHandlers()
        {
            if (SkipInstallation)
                return;

            try {
                Dispatcher.UIThread.UnhandledExceptionFilter += OnDispatcherUnhandledExceptionFilter;
                Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
            }
            catch (Exception) {
                // As above: fall back to the platform's default behavior.
            }
        }

        /// <summary>
        /// Environment variable that forces the crash handling on even under a debugger, so
        /// that the crash dialog and the recovery code can themselves be debugged. Any
        /// non-empty value enables it.
        /// </summary>
        private const string debugExceptionHandlingVariable = "PPEN_DEBUGEXCEPTIONHANDLING";

        /// <summary>
        /// Whether to skip the crash handling entirely and let exceptions propagate normally.
        ///
        /// Under a debugger, catching exceptions here is actively unhelpful: it hides the very
        /// failures the developer is trying to catch, and the debugger's own first-chance
        /// exception handling is a far better tool than this dialog. Setting
        /// PPEN_DEBUGEXCEPTIONHANDLING overrides that, for when the crash handling itself is
        /// what is being debugged.
        ///
        /// Program.Main also consults this, so that its outermost try/catch does not swallow
        /// startup exceptions out from under the debugger.
        /// </summary>
        public static bool SkipInstallation
        {
            get {
                if (!Debugger.IsAttached)
                    return false;

                return string.IsNullOrEmpty(Environment.GetEnvironmentVariable(debugExceptionHandlingVariable));
            }
        }

        /// <summary>
        /// Handles an exception that escaped everything else -- either it happened before
        /// Avalonia was running, or it escaped the dispatcher loop entirely. The application
        /// is over either way; the caller terminates the process afterwards.
        /// </summary>
        /// <param name="exception">The exception that was caught.</param>
        public static void HandleFatalStartupException(Exception exception)
        {
            try {
                if (exception == null)
                    return;

                // If Avalonia got far enough to have a usable window, we can still ask the user
                // what happened before going down.
                if (Dispatcher.UIThread.CheckAccess() && TryGetOwnerWindow() != null) {
                    HandleCrash(exception, canContinue: false);
                    return;
                }

                // Otherwise there is no way to show anything. Send what we can and give up.
                ReportSilently(exception);
            }
            catch (Exception) {
                // Nothing left to try.
            }
        }

        #region Handlers

        /// <summary>
        /// Raised by the dispatcher before the stack unwinds, to ask whether the exception
        /// should be caught at all.
        ///
        /// Forcing RequestCatch to true guarantees that our UnhandledException handler below
        /// actually runs, and that nothing else in the process can quietly opt out of it.
        /// </summary>
        private static void OnDispatcherUnhandledExceptionFilter(
            object? sender, DispatcherUnhandledExceptionFilterEventArgs e)
        {
            e.RequestCatch = true;
        }

        /// <summary>
        /// Handles an exception that escaped an operation running on the Avalonia dispatcher.
        /// This is the recoverable case: marking it handled lets the message loop carry on.
        /// </summary>
        private static void OnDispatcherUnhandledException(
            object? sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Always mark the exception as handled. Leaving it unhandled would let it escape
            // the dispatcher loop and terminate the process, which would make the "Continue
            // Working" button impossible. Whether the application really should keep going is
            // the user's decision, made in the dialog.
            e.Handled = true;

            HandleCrash(e.Exception, canContinue: true);
        }

        /// <summary>
        /// Handles an exception that reached the AppDomain -- typically thrown on a background
        /// thread that has no other handler. The runtime terminates the process after this
        /// returns regardless of what we do, so the application cannot continue.
        /// </summary>
        private static void OnAppDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            try {
                // ExceptionObject is typed as object because non-CLS-compliant languages can
                // throw things that aren't Exceptions. That is vanishingly rare, but it must
                // not make the crash handler itself fail.
                Exception exception = e.ExceptionObject as Exception
                                      ?? new Exception("Non-exception object thrown: " + e.ExceptionObject);

                if (Dispatcher.UIThread.CheckAccess()) {
                    // The exception escaped on the UI thread itself; show the dialog inline.
                    HandleCrash(exception, canContinue: false);
                }
                else {
                    HandleCrashFromBackgroundThread(exception);
                }
            }
            catch (Exception) {
                // Fall through to the exit below.
            }

            // The process is terminating either way; make it explicit and immediate rather
            // than letting the runtime's default crash dialog appear on top of ours.
            Environment.Exit(1);
        }

        /// <summary>
        /// Handles an unobserved exception from a faulted Task that was garbage collected
        /// without anyone looking at its result.
        ///
        /// Deliberately no dialog: these surface at an arbitrary later time, during a garbage
        /// collection, with no relationship to what the user is doing right now. A dialog
        /// would be confusing and the user could not usefully describe what caused it. The
        /// process is unaffected, so the exception is simply observed and reported quietly.
        /// </summary>
        private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try {
                e.SetObserved();

                if (e.Exception != null)
                    _ = Task.Run(() => ReportInBackground(e.Exception, userDescription: ""));
            }
            catch (Exception) {
                // Never let crash reporting destabilize the finalizer thread.
            }
        }

        /// <summary>
        /// Marshals a terminating background-thread crash onto the UI thread so the dialog can
        /// be shown there, waiting (with a bound) for the user to respond.
        /// </summary>
        /// <param name="exception">The exception that was caught.</param>
        private static void HandleCrashFromBackgroundThread(Exception exception)
        {
            try {
                Task uiTask = Dispatcher.UIThread.InvokeAsync(
                    () => HandleCrash(exception, canContinue: false),
                    DispatcherPriority.Send).GetTask();

                // Blocking here is the point: the runtime terminates the process as soon as this
                // handler returns, so we must not return until the user has answered the dialog.
                // The timeout is what makes it safe against a wedged UI thread.
#pragma warning disable VSTHRD002    // Synchronously waiting on tasks or awaiters.
                if (!uiTask.Wait(uiThreadHandoffTimeout)) {
#pragma warning restore VSTHRD002
                    // The UI thread never got to us -- it may be wedged, or already gone. Send
                    // the report from here instead so the crash isn't lost entirely.
                    ReportSilently(exception);
                }
            }
            catch (Exception) {
                ReportSilently(exception);
            }
        }

        #endregion

        #region The main crash flow

        /// <summary>
        /// The single funnel that every handler routes through. Must be called on the UI
        /// thread. Takes a recovery snapshot, shows the dialog, and acts on the user's choice.
        /// </summary>
        /// <param name="exception">The exception that was caught.</param>
        /// <param name="canContinue">
        /// Whether the application is able to keep running. When false, the dialog hides the
        /// "Continue Working" button, because the process is going to terminate regardless.
        /// </param>
        private static void HandleCrash(Exception exception, bool canContinue)
        {
            if (crashHandlerFailed || exception == null)
                return;

            // Decide whether this crash gets a dialog. All the shared state is inspected and
            // updated under the lock, but neither the dialog nor the report is handled inside
            // it. Set when the crash is worth reporting but not worth interrupting the user over.
            bool reportWithoutDialog = false;
            string signature = BuildSignature(exception);

            lock (gate) {
                if (dialogActive) {
                    // A crash while the crash dialog is up: swallow it. Showing a second dialog
                    // on top of the first would be unusable, and this exception is usually a
                    // downstream consequence of the first one anyway.
                    return;
                }

                if (signatureLastDismissed.TryGetValue(signature, out DateTime lastDismissed) &&
                    DateTime.UtcNow - lastDismissed < repeatSuppressionWindow) {
                    // The same failure has just been dealt with. This is a burst of one repeating
                    // fault rather than a new problem, so swallow it. Hitting the same bug again
                    // later does get a dialog -- see repeatSuppressionWindow.
                    return;
                }

                if (dialogsShownThisSession >= maxDialogsPerSession) {
                    // Enough. Report it, but stop interrupting the user.
                    reportWithoutDialog = true;
                }
                else {
                    dialogActive = true;
                    ++dialogsShownThisSession;
                }
            }

            if (reportWithoutDialog) {
                // Past the session limit: report it, but stop interrupting. Still record the
                // time, so the suppression window applies to these too.
                lock (gate) {
                    signatureLastDismissed[signature] = DateTime.UtcNow;
                }

                ReportInBackground(exception, userDescription: "");
                return;
            }

            RecoverySnapshot? snapshot = null;
            try {
                Controller? controller = TryGetLiveController();

                // Undo the half-executed command, if any, before doing anything else. This
                // matters for both outcomes: the snapshot below gets a consistent event
                // database, and a user who chooses to continue working carries on from the
                // last consistent state rather than from the middle of an aborted edit.
                RecoveryManager.RollBackIncompleteCommand(controller);

                // Take the snapshot BEFORE showing the dialog. The process is in an unknown
                // state and may die while the dialog is up, and the user's unsaved work is the
                // one thing here that cannot be regenerated. If they choose to keep working,
                // the snapshot is discarded below.
                snapshot = RecoveryManager.SaveSnapshot(controller);

                CrashDialogViewModel viewModel =
                    new CrashDialogViewModel(exception, canContinue, snapshot != null);

                CrashDialogResult result = ShowCrashDialogBlocking(viewModel);

                if (result == CrashDialogResult.Restart) {
                    // The dialog has already waited for the error report to be sent, so it is
                    // safe to terminate now.
                    RecoveryManager.RestartApplication(controller?.FileName, snapshot);

                    // Environment.Exit rather than desktop.Shutdown(): Shutdown runs the normal
                    // window-closing path, and MainWindow intercepts that to ask "save your
                    // changes?" -- exactly the wrong question after a crash, when the recovery
                    // snapshot has already been written and a replacement instance is starting.
                    // The process may also be corrupted enough that further managed teardown is
                    // unwise.
                    Environment.Exit(0);
                }

                // Not restarting, so the snapshot has no one to restore it.
                RecoveryManager.DiscardSnapshot(snapshot);
                snapshot = null;

                if (result == CrashDialogResult.Continue) {
                    // Send in the background: the user asked to get back to work, not to wait.
                    ReportInBackground(exception, viewModel.UserDescription);
                }
            }
            catch (Exception) {
                // The crash handler itself failed. Latch that so we don't try again and
                // recurse, and drop the snapshot cleanup on the floor -- stale snapshots are
                // purged at startup anyway.
                crashHandlerFailed = true;
            }
            finally {
                lock (gate) {
                    dialogActive = false;

                    // Start the suppression window now, from the dismissal rather than from when
                    // the dialog opened, so that a user who left the dialog up for a while does
                    // not have a repeating fault resume the instant they close it.
                    signatureLastDismissed[signature] = DateTime.UtcNow;
                }
            }
        }

        /// <summary>
        /// Shows the crash dialog and blocks the calling (UI) thread until the user dismisses
        /// it, without needing an async context.
        ///
        /// Window.ShowDialog returns a Task, but Dispatcher.UnhandledException is a synchronous
        /// void event with nothing to await on. Instead we push a nested dispatcher frame:
        /// PushFrame keeps pumping the platform message queue -- so the dialog is interactive
        /// and other windows repaint -- but does not return to the caller until the frame is
        /// stopped from the window's Closed event. The effect is exactly WinForms' modal
        /// ShowDialog inside Application.ThreadException: the failing operation stays suspended
        /// until the user has decided what to do.
        ///
        /// DialogService is deliberately not used. It throws when there is no main window --
        /// precisely the case this has to survive -- and it resolves views by reflection
        /// through the DI container, which may itself be what failed.
        /// </summary>
        /// <param name="viewModel">The ViewModel describing the crash.</param>
        /// <returns>What the user chose to do.</returns>
        private static CrashDialogResult ShowCrashDialogBlocking(CrashDialogViewModel viewModel)
        {
            CrashDialog dialog = new CrashDialog { DataContext = viewModel };

            // Stop the nested frame when the dialog closes. Closed is used rather than the Task
            // returned by ShowDialog because it fires for both the owned and the ownerless case
            // below, and cannot be missed by a dialog that closes immediately.
            DispatcherFrame frame = new DispatcherFrame();
            dialog.Closed += (s, e) => frame.Continue = false;

            Window? owner = TryGetOwnerWindow();
            if (owner != null) {
                try {
                    // Not awaited on purpose: the nested frame below is what waits. ShowDialog
                    // also disables the owner, which is what makes this modal.
#pragma warning disable VSTHRD110    // Observe the result of async calls.
                    dialog.ShowDialog(owner);
#pragma warning restore VSTHRD110
                }
                catch (Exception) {
                    // The owner may be in a state where it cannot parent a dialog -- it might be
                    // closing, or its platform handle may already be gone. Fall back to showing
                    // the crash dialog without an owner.
                    owner = null;
                }
            }

            if (owner == null) {
                // No usable owner: the crash happened before any window existed, or the main
                // window is already gone. Show the dialog as a top-level window. It isn't modal
                // in the window-manager sense, but the nested frame still keeps the failing
                // operation suspended, which is what actually matters here.
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.ShowInTaskbar = true;
                dialog.Show();
                dialog.Activate();
            }

            // Blocks here, pumping messages, until the Closed handler above runs.
            Dispatcher.UIThread.PushFrame(frame);

            return viewModel.Result;
        }

        #endregion

        #region Reporting

        /// <summary>
        /// Sends an error report on a background thread without waiting for it. Used on the
        /// "Continue Working" path and for crashes that get no dialog.
        /// </summary>
        /// <param name="exception">The exception to report.</param>
        /// <param name="userDescription">What the user typed, if anything.</param>
        private static void ReportInBackground(Exception exception, string userDescription)
        {
            try {
                StatisticsReporter reporter = Services.ServiceProvider.GetRequiredService<StatisticsReporter>();
                _ = reporter.ReportExceptionAsync(exception, userDescription);
            }
            catch (Exception) {
                // The DI container may be the thing that failed. A lost report is acceptable.
            }
        }

        /// <summary>
        /// Sends an error report and waits, briefly, for it to complete. Used where there is no
        /// UI to show progress in and the process is about to terminate, so the request has to
        /// finish before we exit.
        /// </summary>
        /// <param name="exception">The exception to report.</param>
        private static void ReportSilently(Exception exception)
        {
            try {
                StatisticsReporter reporter = Services.ServiceProvider.GetRequiredService<StatisticsReporter>();

                using CancellationTokenSource cancellation = new CancellationTokenSource(silentReportTimeout);

                // Blocking on purpose: the caller terminates the process immediately after this
                // returns, so the request has to complete first. Bounded by the timeout, and
                // there is no UI thread involved on any path that reaches here, so the usual
                // synchronization-context deadlock the analyzer warns about cannot occur.
#pragma warning disable VSTHRD002    // Synchronously waiting on tasks or awaiters.
                reporter.ReportExceptionAsync(exception, "", cancellation.Token)
                        .Wait(silentReportTimeout);
#pragma warning restore VSTHRD002
            }
            catch (Exception) {
                // Nothing more to try; the process is going down.
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Builds a signature identifying "the same failure", used to avoid reporting and
        /// re-prompting for a fault that repeats every frame.
        ///
        /// Only the exception type and the first few stack frames are used. Line numbers and
        /// argument values are excluded so that the same bug hit with different data still
        /// counts as the same failure.
        /// </summary>
        /// <param name="exception">The exception to build a signature for.</param>
        /// <returns>A signature string; never null.</returns>
        private static string BuildSignature(Exception exception)
        {
            try {
                StringBuilder signature = new StringBuilder();
                signature.Append(exception.GetType().FullName);

                StackTrace stackTrace = new StackTrace(exception, false);
                int frameCount = Math.Min(3, stackTrace.FrameCount);
                for (int i = 0; i < frameCount; ++i) {
                    System.Reflection.MethodBase? method = stackTrace.GetFrame(i)?.GetMethod();
                    if (method != null) {
                        signature.Append('|');
                        signature.Append(method.DeclaringType?.FullName);
                        signature.Append('.');
                        signature.Append(method.Name);
                    }
                }

                return signature.ToString();
            }
            catch (Exception) {
                // If the signature can't be computed, fall back to something unique so this
                // crash is never mistaken for a repeat of an earlier one and silently dropped.
                return Guid.NewGuid().ToString();
            }
        }

        /// <summary>
        /// Finds a window that can own the crash dialog, or null if there is none.
        ///
        /// Deliberately does not use DialogService.GetActiveOwner, which throws when there is no
        /// main window -- precisely the situation this has to survive.
        /// </summary>
        /// <returns>The topmost visible window, or null if no window is available.</returns>
        private static Window? TryGetOwnerWindow()
        {
            try {
                if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                    return null;

                // Start from the designated main window if it is actually on screen.
                Window? current = (desktop.MainWindow != null && desktop.MainWindow.IsVisible)
                                  ? desktop.MainWindow
                                  : null;

                if (current == null) {
                    foreach (Window window in desktop.Windows) {
                        if (window.IsVisible) {
                            current = window;
                            break;
                        }
                    }
                }

                // Walk down the chain of owned windows, so the crash dialog appears above any
                // dialog that was already open when the exception occurred.
                while (current != null) {
                    Window? owned = null;
                    foreach (Window window in desktop.Windows) {
                        if (window != current && window.IsVisible && ReferenceEquals(window.Owner, current)) {
                            owned = window;
                            break;
                        }
                    }

                    if (owned == null)
                        break;

                    current = owned;
                }

                return current;
            }
            catch (Exception) {
                // Fall back to an ownerless dialog rather than failing.
                return null;
            }
        }

        /// <summary>
        /// Finds the Controller for the currently open event, or null if none is open (the
        /// welcome screen is showing, or the crash happened during startup).
        /// </summary>
        /// <returns>The live controller, or null.</returns>
        private static Controller? TryGetLiveController()
        {
            try {
                if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                    return null;

                if (desktop.MainWindow?.DataContext is MainWindowViewModel mainViewModel &&
                    mainViewModel.Controller != null) {
                    return mainViewModel.Controller;
                }

                // The designated main window may still be the welcome screen while a real main
                // window is being brought up, so check the other windows too.
                foreach (Window window in desktop.Windows) {
                    if (window.DataContext is MainWindowViewModel viewModel && viewModel.Controller != null)
                        return viewModel.Controller;
                }

                return null;
            }
            catch (Exception) {
                // Never let controller discovery break the crash dialog. Without a controller
                // the user simply isn't offered recovery.
                return null;
            }
        }

        #endregion
    }
}
