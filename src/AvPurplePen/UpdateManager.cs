// UpdateManager.cs
//
// Drives the whole update experience: check the manifest once at startup, tell the user if there is
// something newer, download it if they want it, prompt them to save any open work, write out an
// install script and exit so the script can replace us.
//
// The platform-independent parts are elsewhere. CoreUpdater fetches and parses the manifest, decides
// which update applies, downloads it and verifies its SHA256. UpdateInstallerScript works out what
// the install script has to say. This class supplies the things neither of them can: an HTTP
// downloader, the dialogs, the save prompt, and the file and process handling at the end.
//
// Replaces the WinForms PurplePen/Updater.cs, which is Windows-only, checks a pair of fixed text
// files rather than a manifest, and verifies nothing that it downloads.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using AvPurplePen.Views;
using Microsoft.Extensions.DependencyInjection;
using PurplePen;
using PurplePen.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AvPurplePen
{
    /// <summary>
    /// Checks for a newer version of Purple Pen at startup and, with the user's agreement,
    /// downloads and installs it.
    /// </summary>
    public static class UpdateManager
    {
        /// <summary>
        /// The manifest listing every available update, for every platform and channel.
        /// </summary>
        private const string manifestUrl = "https://downloads.purple-pen.org/manifest.json";

        /// <summary>
        /// Path, relative to the per-user local application data directory, that downloaded updates
        /// and install scripts go into. CoreUpdater sweeps files older than ten days out of it
        /// whenever it checks for updates.
        /// </summary>
        private static readonly string[] downloadDirectoryParts = { "PurplePen", "Updates" };

        /// <summary>
        /// Set once the check has been started. The user asked for one check per run: a second one
        /// could only produce a duplicate prompt, and the manifest doesn't change while we run.
        /// </summary>
        private static bool checkStarted;

        /// <summary>
        /// Starts the update check. Returns as soon as the check is under way; everything the user
        /// sees happens later, once the manifest has been fetched.
        ///
        /// Never throws and never reports a failure. A user with no network connection, or behind a
        /// proxy that blocks us, must not be greeted by an error box every time they start the
        /// program — from their point of view there simply is no update.
        /// </summary>
        public static void CheckForUpdatesAtStartup()
        {
            if (checkStarted)
                return;
            checkStarted = true;

            // Fire and forget: nothing waits for this, and CheckAndPrompt handles its own errors.
            _ = CheckAndPrompt();
        }

        /// <summary>
        /// Fetches the manifest, and shows the update dialog if there is something newer to offer.
        /// </summary>
        /// <returns>A task that completes once the user has finished with the dialog, or at once if
        /// there is no update to show.</returns>
        private static async Task CheckAndPrompt()
        {
            try {
                CoreUpdater updater = CreateUpdater();

                CoreUpdateStatus status = await updater.CheckForUpdates(
                    manifestUrl, VersionNumber.Current, GetPlatformName(), GetChannels(), CancellationToken.None);

                if (!status.CheckSucceeded) {
                    // No network, a 404, a manifest that won't parse. Nothing the user can act on,
                    // and it isn't worth interrupting their startup over.
                    Debug.WriteLine("Update check failed: " + status.ErrorMessage);
                    return;
                }

                if (status.AvailableUpdate == null)
                    return;     // Already up to date.

                await ShowUpdateDialog(updater, status.AvailableUpdate);
            }
            catch (Exception ex) {
                // Same reasoning as a failed check: an update is a convenience, and no failure to
                // offer one justifies an error box at startup.
                Debug.WriteLine("Update check threw: " + ex);
            }
        }

        /// <summary>
        /// Shows the update dialog and, if the user asks for it, downloads and installs the update.
        /// </summary>
        /// <param name="updater">The updater to download through.</param>
        /// <param name="update">The update to offer.</param>
        /// <returns>A task that completes when the dialog has closed. If the update is installed,
        /// the process exits before this ever completes.</returns>
        private static async Task ShowUpdateDialog(CoreUpdater updater, AvailableUpdate update)
        {
            if (!IsReadyToShowDialogs())
                return;

            string prettyNewVersion = Util.PrettyVersionString(update.Version);
            string prettyCurrentVersion = Util.PrettyVersionString(VersionNumber.Current);

            UpdateAvailableDialogViewModel viewModel = new UpdateAvailableDialogViewModel {
                // An update with a file to fetch gets the question; one that carries only release
                // notes -- telling the user to update through a package manager, say -- gets the
                // same announcement without it, because there is nothing here to say yes to.
                Message = update.HasDownloadableFile
                    ? string.Format(UIText.MiscText_NewerVersionAvailable, prettyNewVersion, prettyCurrentVersion)
                    : string.Format(UIText.UpdateAvailable_newVersionAvailable_Text, prettyNewVersion, prettyCurrentVersion),
                ReleaseMessage = update.ReleaseMessage ?? "",
                HasDownloadableFile = update.HasDownloadableFile,
            };

            // Cancelled if the user closes the dialog during the download. Disposed by hand at the
            // end rather than with "using": the download runs alongside this method, and disposing
            // the source while its token is still in use can fault the very operation we are trying
            // to cancel.
            CancellationTokenSource downloadCancellation = new CancellationTokenSource();

            // Shown as an owned dialog rather than with ShowDialogAsync, because the download has to
            // be able to dismiss the window itself once it finishes.
            INonModalDialog<UpdateAvailableDialogViewModel> dialog =
                Services.DialogService.ShowOwnedDialog(viewModel, disableOwner: true);

            // Set inside the download handler so that, after the dialog closes, we can tell a
            // completed download from a "Remind Me Later". Stays null if no download was started,
            // or if one was started and then cancelled.
            DownloadedUpdate? downloaded = null;

            // The running download, so we can wait for it to unwind before disposing the token
            // source above. Null until the user asks for the update.
            Task? downloadTask = null;

            viewModel.DownloadRequested += () => {
                // The event is synchronous and the download has to outlive it -- the dialog stays up
                // showing progress -- so the task is kept rather than awaited here.
                downloadTask = RunDownload();
            };

            async Task RunDownload()
            {
                viewModel.IsDownloading = true;
                viewModel.StatusText = UIText.DownloadProgressDialog_Text;

                // Progress<T> marshals its callbacks to the thread that created it -- the UI thread
                // -- so the ViewModel is only ever touched from there, even though CoreUpdater does
                // the downloading on a background thread.
                Progress<double?> progress = new Progress<double?>(fraction => viewModel.ProgressAmount = fraction);

                try {
                    downloaded = await updater.DownloadUpdate(update, progress, downloadCancellation.Token);
                }
                catch (OperationCanceledException) {
                    // The user closed the dialog while it was downloading; the partial file has
                    // already been cleaned up by CoreUpdater. Nothing more to do.
                    return;
                }
                catch (Exception ex) {
                    downloaded = new DownloadedUpdate { DownloadSucceeded = false, ErrorMessage = ex.Message };
                }

                dialog.Close();
            }

            await dialog.ClosedTask;

            // The user closed the dialog themselves: either "Remind Me Later" before the download
            // started, or Cancel during it. Cancel anything still running. (When the dialog was
            // closed by RunDownload instead, the download has already finished and this does
            // nothing.)
            bool userClosedDialog = !dialog.ClosedProgrammatically;
            if (userClosedDialog)
                await downloadCancellation.CancelAsync();

            // Let the download finish unwinding before the token source goes away. RunDownload
            // handles its own exceptions, so awaiting it here cannot throw; the catch is only
            // there so that a bug in it can't take out the update path entirely.
            if (downloadTask != null) {
                try {
                    await downloadTask;
                }
                catch (Exception ex) {
                    Debug.WriteLine("Update download faulted: " + ex);
                }
            }

            downloadCancellation.Dispose();

            if (!userClosedDialog && downloaded != null)
                await InstallDownloadedUpdate(downloaded);
        }

        /// <summary>
        /// Handles a finished download: reports a failure, or prompts to save open work, writes the
        /// install script, starts it and exits.
        /// </summary>
        /// <param name="downloaded">The result of the download.</param>
        /// <returns>A task that never completes when the install goes ahead, because the process
        /// exits.</returns>
        private static async Task InstallDownloadedUpdate(DownloadedUpdate downloaded)
        {
            if (!downloaded.DownloadSucceeded) {
                // Worth telling the user about, unlike a failed check: they asked for this and are
                // waiting on it. The message carries CoreUpdater's diagnostic, which distinguishes a
                // lost connection from a file that failed its hash check.
                await ShowMessage(string.Format(UIText.UpdateAvailable_downloadFailed_Text, downloaded.ErrorMessage ?? ""),
                                  MessageBoxIcon.Error);
                return;
            }

            UpdatePlatform platform = GetPlatform();
            string? applicationPath = GetApplicationPath(platform);

            // The manifest can name a file this platform has no way to install -- an archive format
            // we don't handle, or a macOS .zip when we can't find the application bundle to replace.
            // The download is still good, so point the user at it rather than discarding it.
            if (applicationPath == null || !UpdateInstallerScript.IsSupported(platform, downloaded.Path)) {
                await ShowMessage(string.Format(UIText.UpdateAvailable_downloadedManually_Text, downloaded.Path),
                                  MessageBoxIcon.Information);
                return;
            }

            // Give the user their chance to save. On the welcome screen there is no open event, so
            // there is nothing to ask about.
            MainWindowViewModel? mainWindowViewModel = GetMainWindowViewModel();
            if (mainWindowViewModel?.Controller != null) {
                if (!await mainWindowViewModel.Controller.TryCloseFile()) {
                    // The user backed out of the save prompt, which cancels the install too. The
                    // verified file stays in the download directory; if they update within ten days
                    // it will still be there, and otherwise CoreUpdater sweeps it away.
                    return;
                }
            }

            if (!StartInstaller(platform, downloaded.Path, applicationPath)) {
                await ShowMessage(string.Format(UIText.UpdateAvailable_downloadedManually_Text, downloaded.Path),
                                  MessageBoxIcon.Information);
                return;
            }

            // Environment.Exit rather than desktop.Shutdown: the file has already been closed by
            // TryCloseFile above, and Shutdown would re-enter MainWindow's close handler and prompt
            // to save a second time. Nothing is lost by exiting abruptly -- UserSettings is written
            // whenever it changes, not at shutdown. CrashHandler exits the same way, for the same
            // reason.
            Environment.Exit(0);
        }

        /// <summary>
        /// Writes the install script next to the downloaded file and starts it.
        /// </summary>
        /// <param name="platform">The platform being installed on.</param>
        /// <param name="downloadedFilePath">The downloaded, verified update file.</param>
        /// <param name="applicationPath">The bundle or directory being replaced.</param>
        /// <returns>True if the script was written and started; false if it could not be, in which
        /// case the caller should leave the application running.</returns>
        private static bool StartInstaller(UpdatePlatform platform, string downloadedFilePath, string applicationPath)
        {
            try {
                InstallScript installScript = UpdateInstallerScript.Build(
                    platform, downloadedFilePath, Environment.ProcessId, applicationPath, GetOwnExecutableName());

                // The script goes into the download directory rather than a temporary directory of
                // its own, so that CoreUpdater's ten-day sweep cleans it up along with the installer
                // it ran. The script can't delete itself: it is still executing when the install
                // finishes.
                string scriptPath = Path.Combine(GetDownloadDirectory(), installScript.ScriptFileName);
                File.WriteAllText(scriptPath, installScript.ScriptText);

                ProcessStartInfo startInfo = new ProcessStartInfo {
                    FileName = installScript.LauncherFileName,
                    UseShellExecute = false,
                    CreateNoWindow = true,

                    // The install directory rather than the current one, which may since have been
                    // deleted or become unavailable and would then fail the launch.
                    WorkingDirectory = AppContext.BaseDirectory,
                };

                // ArgumentList quotes each argument properly; the script's path runs through the
                // temporary directory, which routinely contains spaces.
                foreach (string argument in installScript.LauncherArguments) {
                    startInfo.ArgumentList.Add(
                        argument == InstallScript.ScriptPathPlaceholder ? scriptPath : argument);
                }

                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex) {
                // No room on disk, no permission to write the script, or no shell to run it. The
                // download is still fine, so the caller falls back to telling the user where it is.
                Debug.WriteLine("Could not start the update installer: " + ex);
                return false;
            }
        }

        /// <summary>
        /// Creates the updater, wired up to download over HTTP into the update download directory.
        /// </summary>
        /// <returns>A ready-to-use updater.</returns>
        private static CoreUpdater CreateUpdater()
        {
            IHttpClientFactory httpClientFactory = Services.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            return new CoreUpdater(new HttpFileDownloader(httpClientFactory), GetDownloadDirectory());
        }

        /// <summary>
        /// Gets the directory that downloaded updates and install scripts are written to. It does
        /// not need to exist: CoreUpdater creates it when it first downloads something.
        ///
        /// Local application data rather than the temporary directory, because on Linux the
        /// temporary directory is normally /tmp, which is shared by every user on the machine. A
        /// downloaded update sits there through the save prompt -- which can be open for minutes --
        /// before being run or extracted over the installation directory, and that is too long to
        /// leave something we are about to execute in a directory anyone can write to. Local
        /// application data is per-user on all three platforms. (Local, not roaming: on Windows
        /// roaming application data is copied to and from a server at logon and logoff on domains
        /// that enable it, which is no place for a hundred-megabyte installer.)
        /// </summary>
        /// <returns>The full path of the download directory.</returns>
        private static string GetDownloadDirectory()
        {
            string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // GetFolderPath returns an empty string on Unix when HOME isn't set, and combining onto
            // that would produce a relative path rooted at the current directory. Falling back to
            // the temporary directory keeps updating working in that (unusual) case.
            string root = string.IsNullOrEmpty(localApplicationData) ? Path.GetTempPath() : localApplicationData;

            return Path.Combine(root, Path.Combine(downloadDirectoryParts));
        }

        /// <summary>
        /// Gets the update channels to accept, most preferred first. A prerelease build is offered
        /// newer prereleases as well as stable versions; a stable build is only offered stable ones,
        /// so that installing a release never quietly moves someone onto the beta track. This
        /// matches what the WinForms updater does.
        /// </summary>
        /// <returns>The channel names to look for in the manifest.</returns>
        private static string[] GetChannels()
        {
            if (Util.IsPrerelease(VersionNumber.Current))
                return new string[] { "beta", "main" };
            else
                return new string[] { "main" };
        }

        /// <summary>
        /// Gets the platform this is running on, as the manifest names it: an operating system and
        /// an architecture, such as "win-x64", "osx-arm64" or "linux-x64".
        ///
        /// Built here rather than read from <see cref="RuntimeInformation.RuntimeIdentifier"/>,
        /// which is not guaranteed to be a portable identifier of this shape — a self-contained
        /// publish can report something more specific, which would then match nothing in the
        /// manifest and silently disable updating.
        /// </summary>
        /// <returns>The platform name to look up in the manifest.</returns>
        private static string GetPlatformName()
        {
            string operatingSystem;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                operatingSystem = "win";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                operatingSystem = "osx";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                operatingSystem = "linux";
            else
                operatingSystem = "unknown";

            string architecture;
            switch (RuntimeInformation.OSArchitecture) {
                case Architecture.X64: architecture = "x64"; break;
                case Architecture.X86: architecture = "x86"; break;
                case Architecture.Arm64: architecture = "arm64"; break;
                case Architecture.Arm: architecture = "arm"; break;
                default: architecture = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant(); break;
            }

            return operatingSystem + "-" + architecture;
        }

        /// <summary>
        /// Gets the platform as the install-script builder expects it.
        /// </summary>
        /// <returns>The platform being run on.</returns>
        private static UpdatePlatform GetPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return UpdatePlatform.MacOS;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return UpdatePlatform.Linux;
            else
                return UpdatePlatform.Windows;
        }

        /// <summary>
        /// Gets what the install script will be replacing: on macOS the application bundle, on Linux
        /// the installation directory. Windows installers work this out for themselves and get null.
        /// </summary>
        /// <param name="platform">The platform being installed on.</param>
        /// <returns>The path to replace, or null on macOS when the application bundle can't be
        /// found — which means the caller must not try to install in place.</returns>
        private static string? GetApplicationPath(UpdatePlatform platform)
        {
            switch (platform) {
                case UpdatePlatform.MacOS:
                    return GetMacApplicationBundlePath();

                case UpdatePlatform.Linux:
                    return AppContext.BaseDirectory;

                default:
                    // Windows: the installer knows where the program goes. Returning null here would
                    // be read as "can't install", so return something harmless instead.
                    return "";
            }
        }

        /// <summary>
        /// Gets the file name of the running Purple Pen executable, so that an update which is a
        /// plain drop-in replacement can restart it afterwards. Taken from the running process
        /// rather than hard-coded, so that renaming the executable can't quietly break the restart.
        /// </summary>
        /// <returns>
        /// The executable's file name, or null when it can't be determined. Null happens when
        /// Purple Pen was started as "dotnet AvPurplePen.dll": the running process is then the
        /// shared .NET host, which lives outside our installation directory and is emphatically not
        /// what should be restarted. Rather than guess, we say we don't know, and the install script
        /// leaves the user to start the new version themselves.
        /// </returns>
        private static string? GetOwnExecutableName()
        {
            string? processPath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(processPath))
                return null;

            string? processDirectory = Path.GetDirectoryName(processPath);
            if (processDirectory == null)
                return null;

            // A published Purple Pen runs from its own launcher inside the installation directory;
            // anything running from elsewhere is a host that happens to be executing us.
            if (!string.Equals(Path.TrimEndingDirectorySeparator(processDirectory),
                               Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory),
                               StringComparison.OrdinalIgnoreCase)) {
                return null;
            }

            return Path.GetFileName(processPath);
        }

        /// <summary>
        /// Finds the ".app" bundle containing the running executable, by walking up from it.
        /// </summary>
        /// <returns>The full path of the bundle, or null when there isn't one — which happens when
        /// running from a build directory rather than from an installed copy.</returns>
        private static string? GetMacApplicationBundlePath()
        {
            string? path = Environment.ProcessPath;
            if (string.IsNullOrEmpty(path))
                return null;

            string? executableDirectory = Path.GetDirectoryName(path);
            if (executableDirectory == null)
                return null;

            // A bundled application runs from Purple Pen.app/Contents/MacOS/, so the bundle is a few
            // levels up. Walking rather than counting levels, since the layout isn't ours to assume.
            DirectoryInfo? directory = new DirectoryInfo(executableDirectory);
            while (directory != null) {
                if (directory.Name.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                    return directory.FullName;
                directory = directory.Parent;
            }

            return null;
        }

        /// <summary>
        /// Gets the ViewModel of the main window, when the application is showing an open event.
        /// </summary>
        /// <returns>The main window's ViewModel, or null when the welcome screen is showing instead
        /// (in which case there is no open event and nothing to save).</returns>
        private static MainWindowViewModel? GetMainWindowViewModel()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    && desktop.MainWindow is MainWindow mainWindow) {
                return mainWindow.DataContext as MainWindowViewModel;
            }

            return null;
        }

        /// <summary>
        /// Checks that there is a window on screen for a dialog to be owned by. Avalonia refuses to
        /// show a dialog owned by a window that hasn't been shown yet, and the check runs early
        /// enough in startup that it is worth making sure.
        /// </summary>
        /// <returns>True if a dialog can be shown now.</returns>
        private static bool IsReadyToShowDialogs()
        {
            return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                   && desktop.MainWindow != null
                   && desktop.MainWindow.IsVisible;
        }

        /// <summary>
        /// Shows a simple message box with an OK button.
        /// </summary>
        /// <param name="message">The text to show.</param>
        /// <param name="icon">The icon to show beside it.</param>
        /// <returns>A task that completes when the user dismisses the message.</returns>
        private static async Task ShowMessage(string message, MessageBoxIcon icon)
        {
            MessageBoxDialogViewModel viewModel = new MessageBoxDialogViewModel {
                Message = message,
                Title = UIText.MiscText_AppTitle,
                Buttons = MessageBoxButtons.Ok,
                DefaultButton = MessageBoxButton.Ok,
                Icon = icon,
            };

            await Services.DialogService.ShowDialogAsync(viewModel);
        }
    }
}
