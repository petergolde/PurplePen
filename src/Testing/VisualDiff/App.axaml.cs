using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using VisualDiff.Models;
using VisualDiff.Views;

namespace VisualDiff
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                // The comparison window decides the process result, so do not let Avalonia shut down
                // implicitly before its Closed handler can supply that result.
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                string? argumentError = ValidateArguments(desktop.Args, out string? newFilename, out string? baselineFilename);
                if (argumentError != null) {
                    ShowStartupError(desktop, argumentError);
                }
                else {
                    BitmapComparison? comparison = null;
                    try {
                        comparison = new BitmapComparison(newFilename!, baselineFilename!);
                        MainWindow window = new MainWindow(comparison);
                        comparison = null; // MainWindow now owns and disposes the comparison.
                        desktop.MainWindow = window;
                        window.Closed += delegate { desktop.Shutdown(window.ExitCode); };
                    }
                    catch (Exception ex) {
                        comparison?.Dispose();
                        ShowStartupError(desktop, ex.Message);
                    }
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        // Validate the command-line shape and normalize both pathnames before any bitmap is loaded.
        private static string? ValidateArguments(string[]? args, out string? newFilename, out string? baselineFilename)
        {
            newFilename = null;
            baselineFilename = null;

            if (args == null || args.Length != 3)
                return "VisualDiff requires exactly three arguments:\n\nbitmap <new bitmap pathname> <baseline pathname>";

            if (args[0] != "bitmap")
                return "The first argument must be \"bitmap\" exactly.";

            if (string.IsNullOrWhiteSpace(args[1]))
                return "The new bitmap pathname cannot be empty.";

            if (string.IsNullOrWhiteSpace(args[2]))
                return "The baseline pathname cannot be empty.";

            try {
                newFilename = Path.GetFullPath(args[1]);
                baselineFilename = Path.GetFullPath(args[2]);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) {
                return "One of the bitmap pathnames is invalid:\n\n" + ex.Message;
            }

            if (!File.Exists(newFilename))
                return $"New bitmap file '{newFilename}' does not exist.";

            return null;
        }

        // Display a small Avalonia message box as the only window, then terminate with failure.
        private static void ShowStartupError(IClassicDesktopStyleApplicationLifetime desktop, string message)
        {
            MessageBoxWindow errorWindow = new MessageBoxWindow(message, centerOnScreen: true);
            desktop.MainWindow = errorWindow;
            errorWindow.Closed += delegate { desktop.Shutdown(1); };
        }
    }
}
