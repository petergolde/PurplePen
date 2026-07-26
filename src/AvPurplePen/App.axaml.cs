using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Threading;
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
            //    new ResourceInclude(new Uri("avares://AvPurplePen/")) { Source = new Uri("/Themes/PurplePenScheme.axaml", UriKind.Relative) };

            //RequestedThemeVariant = PurplePenTheme;

            RequestedThemeVariant = ThemeVariant.Light;

            // The Avalonia dispatcher exists by the time Initialize runs, so this is the
            // earliest point at which the UI-thread exception handlers can be attached.
            CrashHandler.InstallDispatcherHandlers();
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

            base.OnFrameworkInitializationCompleted();

            ApplicationIdleService.Initialize();
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
                catch (Exception) {
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
    }
}