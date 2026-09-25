using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Core;
using Avalonia.Interactivity;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvPurplePen.Views;
using Semi.Avalonia;
using System;
using System.Linq;
using PurplePen;
using PurplePen.ViewModels;

namespace AvPurplePen
{
    public partial class App : Application
    {
        // True while a file the operating system asked us to open is loading, so that a second
        // request arriving meanwhile is ignored rather than started on top of it.
        private bool openingActivatedFile;

        /// <summary>
        /// Custom theme variant for PurplePen, based on Semi.Avalonia's Desert (Light) scheme.
        /// Colors are defined in Themes/PurplePenColors.axaml.
        /// </summary>
        /// 
        //public static readonly ThemeVariant PurplePenTheme = new("PurplePen", ThemeVariant.Light);

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

#if DEBUG
            this.AttachDeveloperTools();
#endif
            // Register our custom color scheme with the SemiTheme so its
            // ThemeDictionaries resolve our variant.
            //SemiTheme semiTheme = (SemiTheme)Styles[0];
            //semiTheme.Resources!.ThemeDictionaries[PurplePenTheme] =
            //    new ResourceInclude(new Uri("avares://PurplePen/")) { Source = new Uri("/Themes/PurplePenScheme.axaml", UriKind.Relative) };

            //RequestedThemeVariant = PurplePenTheme;

            RequestedThemeVariant = ThemeVariant.Light;

            // The Avalonia dispatcher exists by the time Initialize runs, so this is the
            // earliest point at which the UI-thread exception handlers can be attached.
            CrashHandler.InstallDispatcherHandlers();

            // The name macOS shows for the application menu, at the left end of the menu bar.
            // Without it the menu is named after the process, which is the assembly name.
            // Read once here: Avalonia passes it to the platform during startup, so a later
            // language change does not reach it. Every translation of MainFrame_Text is the
            // untranslated product name, so there is nothing to re-read.
            Name = UIText.MainFrame_Text;

            // Keeps the macOS menu bar from emptying out whenever a dialog is showing.
            MacMenuUtilities.InstallDialogMenus();

            // Clicking a NumericUpDown's up/down arrow puts focus back in its text box, text selected.
            Button.ClickEvent.AddClassHandler<NumericUpDown>(NumericUpDownArrow_Click, handledEventsToo: true);
        }

        /// <summary>
        /// Moves focus to a NumericUpDown's text box, with all its text selected, when one of its
        /// up/down arrows is clicked. The click reaches here after the spinner has already
        /// changed the value, so the selection covers the new text.
        /// The arrows are made non-focusable in App.axaml so that Tab skips them, but that alone
        /// leaves focus somewhere other than the text box after a click, and the Up/Down arrow
        /// keys then stop changing the value.
        /// </summary>
        /// <param name="numericUpDown">The NumericUpDown the click bubbled up to.</param>
        /// <param name="e">Event arguments; the source is the button that was clicked.</param>
        private static void NumericUpDownArrow_Click(NumericUpDown numericUpDown, RoutedEventArgs e)
        {
            if (e.Source is RepeatButton) {
                TextBox? textBox = numericUpDown.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
                if (textBox != null) {
                    textBox.Focus();
                    textBox.SelectAll();
                }
            }
        }

        /// <summary>
        /// Shows the About dialog, for the macOS application menu declared in App.axaml.
        /// Mirrors MainWindowViewModel's ShowAboutDialog command, which still serves the Help menu
        /// on Windows and Linux. It is repeated rather than delegated to because the application
        /// menu is also on screen while the welcome screen is showing, and there is no
        /// MainWindowViewModel then.
        /// </summary>
        /// <param name="sender">The menu item that was picked.</param>
        /// <param name="e">Event arguments (unused).</param>
        private async void AboutMenuItem_Click(object? sender, EventArgs e)
        {
            await Services.DialogService.ShowDialogAsync(new AboutDialogViewModel());
        }

        /// <summary>
        /// Shows the Switch Language dialog and applies the choice, for the macOS application menu
        /// declared in App.axaml. When a main window is up, this defers to its ViewModel's
        /// ShowSwitchLanguageDialog command, which also offers to switch the event's description
        /// language. Otherwise (the welcome screen is showing, and there is no event) it shows the
        /// dialog itself, for the same reason as <see cref="AboutMenuItem_Click"/>.
        /// </summary>
        /// <param name="sender">The menu item that was picked.</param>
        /// <param name="e">Event arguments (unused).</param>
        private async void ProgramLanguageMenuItem_Click(object? sender, EventArgs e)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow is MainWindow { DataContext: MainWindowViewModel mainViewModel }) {
                await mainViewModel.ShowSwitchLanguageDialogCommand.ExecuteAsync(null);
                return;
            }

            string currentCode = Services.UILanguage.LanguageCode;
            SwitchLanguageDialogViewModel viewModel = new SwitchLanguageDialogViewModel(
                currentCode, SwitchLanguageDialogViewModel.CreateDefaultLanguages());

            if (await Services.DialogService.ShowDialogAsync(viewModel) && viewModel.SelectedLanguage != null) {
                Services.UILanguage.LanguageCode = viewModel.SelectedLanguage.Code;
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                // Clean up crash-recovery snapshots left behind by earlier sessions that were
                // never recovered -- a restart that failed to launch, or a second crash during
                // recovery. Purely housekeeping; it never throws.
                RecoveryManager.PurgeStaleSnapshots(TimeSpan.FromDays(30));

                CommandLineOptions options = CommandLineOptions.Parse(desktop.Args);

                if (options.FileName != null) {
                    // A file was named on the command line -- either because the user opened a
                    // .ppen file directly, or because the crash handler restarted us. Open it
                    // straight away rather than making the user pick it out of the welcome screen.
                    StartWithCommandLineFile(desktop, options);
                }
                else {
                    // Show the welcome screen first. It creates and shows the real
                    // main window itself (possibly after the New Event wizard) once
                    // an event has been created or loaded.
                    InitialScreenWindow initialScreen = new InitialScreenWindow {
                        DataContext = new InitialScreenViewModel(),
                    };
                    desktop.MainWindow = initialScreen;
                }
            }

            // On macOS, a .ppen file opened from the Finder arrives as an "open documents" event
            // rather than on the command line -- both when it launches Purple Pen and when Purple
            // Pen is already running. On a launch the event is delivered once the native run loop
            // starts, which is after this method returns, so subscribing here doesn't miss it.
            // The feature is absent (or never raises File activations) on the other platforms.
            if (TryGetFeature(typeof(IActivatableLifetime)) is IActivatableLifetime activatableLifetime) {
                activatableLifetime.Activated += ActivatableLifetime_Activated;
            }

            base.OnFrameworkInitializationCompleted();

            ApplicationIdleService.Initialize();

            // Check for a newer version of Purple Pen, once per run. Posted at Background priority
            // for the same reason StartWithCommandLineFile is: the desktop lifetime shows the main
            // window before background-priority work runs, so by the time this executes there is a
            // window on screen for the update dialog to be owned by. (UpdateManager checks that
            // anyway -- Avalonia refuses to show a dialog owned by an unshown window.)
            Dispatcher.UIThread.Post(UpdateManager.CheckForUpdatesAtStartup, DispatcherPriority.Background);
        }

        /// <summary>
        /// Handles the operating system asking Purple Pen to open files -- on macOS, a .ppen file
        /// double-clicked in the Finder or dropped on the Dock icon. Purple Pen has one event open
        /// at a time, so only the first file is opened. It replaces the welcome screen if that is
        /// showing, or the current event (after the usual save prompt) if the main window is.
        ///
        /// The request is ignored while a dialog is up, or while an earlier request is still
        /// loading: swapping the event out from under a dialog that is working on it is not safe.
        /// </summary>
        /// <param name="sender">The activatable lifetime (unused).</param>
        /// <param name="e">Event arguments; only File activations are acted upon.</param>
        private async void ActivatableLifetime_Activated(object? sender, ActivatedEventArgs e)
        {
            if (e is not FileActivatedEventArgs fileArgs || openingActivatedFile)
                return;
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            string? fileName = fileArgs.Files.Select(item => item.TryGetLocalPath()).FirstOrDefault(path => path != null);
            if (fileName == null)
                return;

            // Any visible window other than the main window is a dialog.
            Window? mainWindow = desktop.MainWindow;
            if (mainWindow == null || desktop.Windows.Any(w => w != mainWindow && w.IsVisible))
                return;

            openingActivatedFile = true;
            try {
                mainWindow.Activate();

                if (mainWindow.DataContext is InitialScreenViewModel initialScreenViewModel) {
                    await initialScreenViewModel.OpenEventFile(fileName);
                }
                else if (mainWindow.DataContext is MainWindowViewModel mainWindowViewModel) {
                    await mainWindowViewModel.OpenPurplePenFile(fileName);
                }
            }
            catch (Exception exception) {
                // Ordinary load failures are reported to the user by the controller and never
                // reach here; see StartWithCommandLineFile for why anything else goes to stderr.
                ReportFailedInitialLoad(fileName, exception);
            }
            finally {
                openingActivatedFile = false;
            }
        }

        /// <summary>
        /// Opens the event named on the command line directly in the main window, bypassing the
        /// welcome screen.
        ///
        /// The load has to be deferred rather than awaited here: this method runs before the
        /// dispatcher's main loop starts, and Controller.LoadInitialFile is asynchronous and may
        /// itself put up dialogs (a missing map file, missing fonts) that need a shown window to
        /// own them. The desktop lifetime shows MainWindow itself once this method returns,
        /// which is why the welcome-screen path above never calls Show() either.
        /// </summary>
        /// <param name="desktop">The application lifetime, whose MainWindow is set here.</param>
        /// <param name="options">The parsed command line, whose FileName is non-null.</param>
        private void StartWithCommandLineFile(IClassicDesktopStyleApplicationLifetime desktop,
                                              CommandLineOptions options)
        {
            MainWindowViewModel viewModel = new MainWindowViewModel();
            Controller controller = new Controller(viewModel);
            MainWindow mainWindow = new MainWindow {
                DataContext = viewModel,
            };

            desktop.MainWindow = mainWindow;

            // Background priority, so the window is up and painted before the load starts.
            // InvokeAsync (rather than Post) so the async lambda is a Task rather than an
            // async void; the inner try/catch means it can never fault in any case.
            _ = Dispatcher.UIThread.InvokeAsync(async () => {
                bool loaded;
                try {
                    if (options.RecoveryFileName != null) {
                        // Restarted after a crash: the data comes from the recovery snapshot, but
                        // the document is presented as the original file and starts out dirty, so
                        // saving writes the recovered work back to where it belongs.
                        loaded = await controller.LoadRecoveryFile(options.RecoveryFileName, options.FileName!);
                    }
                    else {
                        loaded = await controller.LoadInitialFile(options.FileName!, true);
                    }
                }
                catch (Exception e) {
                    // Something unanticipated. The ordinary load failures -- a missing, corrupt or
                    // unreadable file -- report themselves through Controller.HandleExceptions and
                    // never reach here, so anything that does is a fault worth knowing about, and
                    // discarding it silently produces the least diagnosable symptom there is:
                    // Purple Pen starts, the file the user asked for does not open, and nothing
                    // says why.
                    //
                    // Written to standard error rather than shown in a dialog, because the dialog
                    // machinery is one of the things that can fail here, and a second failure while
                    // reporting the first would leave the user with even less. Someone running from
                    // a terminal sees it; the fallback below still keeps the application usable.
                    ReportFailedInitialLoad(options.FileName!, e);
                    loaded = false;
                }

                if (loaded && options.RecoveryFileName != null) {
                    // The recovered data is in memory now, so the snapshot has done its job.
                    RecoveryManager.CleanUpAfterSuccessfulLoad(options.RecoveryFileName);
                }

                if (!loaded) {
                    // The file couldn't be opened -- deleted, corrupt, or not readable. The
                    // controller has already told the user why; fall back to the welcome screen
                    // rather than leaving an empty main window on screen.
                    mainWindow.ShowInitialScreenInstead();
                }
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// Reports an exception that stopped the file named on the command line from opening.
        ///
        /// Standard error is the destination on purpose: it is the one channel that cannot itself
        /// be the thing that is broken, it costs nothing when nobody is watching, and it is where
        /// someone told to "run it from a terminal" will look.
        /// </summary>
        /// <param name="fileName">The file that was being opened.</param>
        /// <param name="exception">The exception that stopped it.</param>
        private static void ReportFailedInitialLoad(string fileName, Exception exception)
        {
            try {
                Console.Error.WriteLine("Purple Pen could not open \"{0}\":", fileName);
                Console.Error.WriteLine(exception.ToString());
            }
            catch (Exception) {
                // Reporting a failure must never become a failure of its own.
            }
        }
    }
}