/* Copyright (c) 2013, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;


namespace PurplePen
{
#if MSSTORE
    // It seems like I should be able to reference something to get this, but I can't figure it out and this seems to work.
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
    internal interface IInitializeWithWindow
    {
        void Initialize(IntPtr hwnd);
    }
#endif

    // Check for new updates, download and run the installer.
    static class Updater
    {
        // Locations of key files on the server.
        // Each file has two lines -- version number available, and file name in the same directory.
        private const string downloadLocation = "http://purple-pen.org/downloads/";
        private const string latestVersionName = "latest_version.txt";
        private const string latestPreleaseName = "latest_prerelease_version.txt";

        // Key for our uninstaller in the registry. Used to see if we should uninstall the standalone version.
        private const string uninstallKey = "{347D1E62-7134-4827-9679-4952BEC91C95}_is1";

        // Directly inside temp directory to store downloaded versions.
        private const string directoryName = "PurplePen";

        private static bool updateCheckStarted;
        private static BackgroundWorker versionCheckWorker;

        private class CheckResults
        {
#if MSSTORE
            // These are for the store version.
            public string UninstallProgramName;       // If non-null, name of program to uninstall.
            public string UninstallProgramArguments;  // If non-null, arguments to uninstall program.
            public bool NewStoreVersionAvailable;     // True if a new store version is available.
#else
            // These are for the non-store version.
            public string CurrentVersion;
            public string CurrentFileName;
            public string CurrentStoreDownloadUrl;
            public string PrereleaseVersion;
            public string PrereleaseFileName;
#endif
        }

        // If non-null, controller to check to see if we can save.
        public static Controller Controller { get; set; }

        public static void CheckForUpdates()
        {
            if (!updateCheckStarted) {
                updateCheckStarted = true;
                versionCheckWorker = new BackgroundWorker();
                versionCheckWorker.DoWork += versionCheckWorker_DoWork;
                versionCheckWorker.RunWorkerCompleted += versionCheckWorker_RunWorkerCompleted;
                versionCheckWorker.RunWorkerAsync();
            }
        }

#if MSSTORE
        // Set the window on the store context, so that the store can show UI.
        private static void SetWindowOnStoreContext(StoreContext storeContext)
        {
            Form mainForm = Application.OpenForms.Cast<Form>().First(x => x.Visible && x.Enabled);
            ((IInitializeWithWindow)(object)storeContext).Initialize(mainForm.Handle);
        }

        // Ask user whether to uninstall the standalone version, return true if user wants to.
        private static bool AskToUninstallOldVersion()
        {
            // Ask to see if user wants to update.
            string message = string.Format(MiscText.UninstallNonStoreVersion);
            DialogResult answer = MessageBox.Show(message, MiscText.AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (answer != DialogResult.Yes) {
                return false;
            }
            else {
                return true;
            }
        }

        private static string TrimMatchingQuotes(string input)
        {
            if ((input.Length >= 2) &&
                (input[0] == '"') && (input[input.Length - 1] == '"'))
                return input.Substring(1, input.Length - 2);

            return input;
        }

        private static string[] SplitCommandLine(string s)
        {
            bool inQuotes = false;

            for (int i = 0; i < s.Length; i++) {
                if (s[i] == '"') {
                    inQuotes = !inQuotes;
                }

                if (!inQuotes && s[i] == ' ') {
                    return new string[] { TrimMatchingQuotes(s.Substring(0, i)), s.Substring(i + 1) };
                }
            }

            return new string[] { TrimMatchingQuotes(s), "" };
        }
#endif

#if !MSSTORE
        private static void AskToDownload(string versionNumber, string fileName)
        {
            // Ask to see if user wants to update.
            string message = string.Format(MiscText.NewerVersionAvailable, Util.PrettyVersionString(versionNumber), Util.PrettyVersionString(VersionNumber.Current));
            DialogResult answer = MessageBox.Show(message, MiscText.AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
            if (answer != DialogResult.Yes) {
                return;
            }

            // If we have a controller, make sure we can exit.
            if (Controller != null) {
                if (!Controller.TryCloseFile())
                    return;
            }

            DownloadAndInstall(new Uri(downloadLocation + fileName), fileName, true);
        }

        public static bool DownloadAndInstall(Uri downloadFrom, string fileName, bool exitToInstall)
        {
            WebClient client = new WebClient();
            string downloadedFile = Path.Combine(GetDownloadDirectory(), fileName);
            bool completed = false;
            bool success = false;

            downloadedFile = FindNonexistantFile(downloadedFile);

            DownloadProgressDialog downloadProgressDialog = new DownloadProgressDialog();
            client.DownloadProgressChanged += (sender, e) => { downloadProgressDialog.SetProgress(e.ProgressPercentage); };
            client.DownloadFileCompleted += (sender, e) => {
                completed = true;
                downloadProgressDialog.DialogResult = DialogResult.OK;
            };

            client.DownloadFileAsync(downloadFrom, downloadedFile);
            var result = downloadProgressDialog.ShowDialog();

            if (result == DialogResult.OK && completed) {
                success = Install(downloadedFile, exitToInstall);
            }

            client.Dispose();
            downloadProgressDialog.Dispose();
            return success;
        }

        // Find a file name that doesn't exists by appending "(1)", "(2)", etc.
        private static string FindNonexistantFile(string downloadedFile)
        {
            if (!File.Exists(downloadedFile))
                return downloadedFile;
            else {
                string newFile;
                int i = 1;
                do {
                    newFile = Path.Combine(Path.GetDirectoryName(downloadedFile), Path.GetFileNameWithoutExtension(downloadedFile) + "(" + i.ToString() + ")" + Path.GetExtension(downloadedFile));
                    i += 1;
                } while (File.Exists(newFile));
                return newFile;
            }
        }

        // Returns on failure. On success, starts installer and exits.
        private static bool Install(string downloadedInstallerFile, bool exitToInstall)
        {
            try {
                var process = Process.Start(downloadedInstallerFile);
                if (process != null) {
                    if (exitToInstall) {
                        process.WaitForInputIdle();
                        process.Dispose();
                        Environment.Exit(0);
                        return true;
                    }
                    else {
                        process.WaitForExit();
                        bool success = (process.ExitCode == 0);
                        process.Dispose();
                        return success;
                    }
                }
                else {
                    return false;
                }
            }
            catch (Exception) {
                return false;
            }
        }

        private static string GetDownloadDirectory()
        {
            string tempDir = Path.GetTempPath();
            string downloadDir = Path.Combine(tempDir, directoryName);
            if (!Directory.Exists(downloadDir))
                Directory.CreateDirectory(downloadDir);
            if (Directory.Exists(downloadDir))
                return downloadDir;
            else
                return null;
        }

        private static void DeletePreviouslyDownloadedFiles()
        {
            try {
                string downloadDir = GetDownloadDirectory();
                if (downloadDir != null) {
                    foreach (string file in Directory.GetFiles(downloadDir, "*.exe")) {
                        try {
                            File.Delete(file);
                        }
                        catch (Exception) { }
                    }
                }
            }
            catch (IOException) { }
        }
#endif

        static void versionCheckWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // The version check has completed. The result is either null, or the version string of the new version.
            if (!e.Cancelled && e.Error == null && e.Result != null) {
                CheckResults results = (CheckResults)e.Result;

#if MSSTORE
                if (results.NewStoreVersionAvailable) {
                    StoreContext storeContext = StoreContext.GetDefault();

                    /* Can't test this until we have a real version in the store. */
#if false
                    // Should we do a message box first to ask user if they want to update? Not sure what this message looks like.
                    IReadOnlyList<StorePackageUpdate> updates = storeContext.GetAppAndOptionalStorePackageUpdatesAsync().AsTask().Result;
                    SetWindowOnStoreContext(storeContext);
                    IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> downloadOperation =
                            storeContext.RequestDownloadAndInstallStorePackageUpdatesAsync(updates);
                    StorePackageUpdateResult result = downloadOperation.AsTask().Result;
#endif
                }

                if (results.UninstallProgramName != null) {
                    bool uninstall = AskToUninstallOldVersion();

                    if (uninstall) {
                        // Uninstall the standalone version.
                        Process process = new Process();
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        process.StartInfo.FileName = results.UninstallProgramName;
                        process.StartInfo.Arguments = results.UninstallProgramArguments;
                        try {
                            process.Start();
                            process.WaitForExit();
                        }
                        catch (Exception) {
                            // Ignore errors.
                        }
                    }
                }
#else
                if (results.CurrentVersion != null && Util.CompareVersionStrings(VersionNumber.Current, results.CurrentVersion) < 0) {
                    AskToDownload(results.CurrentVersion, results.CurrentFileName);
                }
                else if (results.PrereleaseVersion != null && Util.CompareVersionStrings(VersionNumber.Current, results.PrereleaseVersion) < 0 && Util.SameExceptRevision(VersionNumber.Current, results.PrereleaseVersion)) {
                    AskToDownload(results.PrereleaseVersion, results.PrereleaseFileName);
                }
#endif
            }

            versionCheckWorker.Dispose();
            versionCheckWorker = null;
        }

        static void versionCheckWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            WebClient client = new WebClient();

#if MSSTORE
            // For the store version, we don't check a file for updates. 
            // We do two other things:
            //   1. Check if a new store version is available
            //   2. Check if the standalone version is installed, so we could uninstall it.
            CheckResults results = new CheckResults();

            results.NewStoreVersionAvailable = false;
            try {
#if false
                // Can't test this until we have a real version in the store.
                StoreContext storeContext = StoreContext.GetDefault();
                IReadOnlyList<StorePackageUpdate> updates = storeContext.GetAppAndOptionalStorePackageUpdatesAsync().GetResults();

                results.NewStoreVersionAvailable = (updates.Count > 0); // UNDONE: Check if a new store version is available.
#endif
            }
            catch {
                results.NewStoreVersionAvailable = false;
            }

            //Tou can find your uninstall string at one of these two locations.
            String uninstallString = (String)Microsoft.Win32.Registry.GetValue
                (@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" + uninstallKey, "UninstallString", null);
            if (uninstallString == null) {
                uninstallString = (String)Microsoft.Win32.Registry.GetValue
                (@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + uninstallKey, "UninstallString", null);
            }

            //Detect if the previous version of the Desktop application is installed.
            if (uninstallString != null) {
                string[] uninstallArgs = SplitCommandLine(uninstallString);
                results.UninstallProgramName = uninstallArgs[0];
                if (uninstallArgs.Length > 1) {
                    results.UninstallProgramArguments = uninstallArgs[1];
                }
            }
#else
            // FOr the non-store version, we check for updates by downloading a file from the server.

            DeletePreviouslyDownloadedFiles();

            // We need to check to see if a new version is available. We do this in the background.
            // If a new version is available, the version number is returned as the result of the background
            // processing. If no new version is available, null is returned.
            CheckResults results = new CheckResults();

            // Download latest version.
            string latestVersion = null;
            string latestPrerelease = null;
            try {
                latestVersion = client.DownloadString(downloadLocation + latestVersionName);
            }
            catch (WebException) {
                latestVersion = null;
            }

            // Only check latest prerelease if this is a pre-release.
            if (Util.IsPrerelease(VersionNumber.Current)) {
                try {
                    latestPrerelease = client.DownloadString(downloadLocation + latestPreleaseName);
                }
                catch (WebException) {
                    latestPrerelease = null;
                }
            }

            if (latestVersion != null) {
                // Get first line and second line.
                string[] lines = latestVersion.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                string version = lines.Length > 0 ? lines[0] : null;
                string filename = lines.Length > 1 ? lines[1] : null;
                string storeDownloadUrl = lines.Length > 2 ? lines[2] : null;

                if (!string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(filename)) {
                    results.CurrentVersion = version;
                    results.CurrentFileName = filename;
                    results.CurrentStoreDownloadUrl = storeDownloadUrl;
                }
            }

            if (latestPrerelease != null) {
                // Get first line and second line.
                string[] lines = latestPrerelease.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                string version = lines.Length > 0 ? lines[0] : null;
                string filename = lines.Length > 1 ? lines[1] : null;

                if (!string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(filename)) {
                    results.PrereleaseVersion = version;
                    results.PrereleaseFileName = filename;
                }
            }
#endif

            // Collect anonymous statistics, so we can know number of time the program is invoked, and from where, which version and language people are using.
            string uiLanguage = Settings.Default.UILanguage;
            if (string.IsNullOrEmpty(uiLanguage))
                uiLanguage = CultureInfo.CurrentUICulture.Name;

            string versionString = VersionNumber.Current;
#if MSSTORE
            versionString += "S";   // Add S to indicate store version.
#endif

            string status = string.Format("{{\"Version\":\"{0}\", \"Locale\":\"{1}\", \"TimeZone\":\"{2}\", \"UILang\":\"{3}\", \"ClientId\":\"{4}\", \"OSVersion\":\"{5}\"}}",
                JsonEncode(versionString),
                JsonEncode(CultureInfo.CurrentCulture.Name),
                JsonEncode(TimeZoneInfo.Local.StandardName),
                JsonEncode(uiLanguage),
                JsonEncode(Settings.Default.ClientId.ToString()),
                JsonEncode(CrashReporterDotNET.HelperMethods.GetWindowsVersion()));
            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            client.Encoding = Encoding.UTF8;

            try {
                client.UploadStringAsync(new Uri("http://monitor.purple-pen.org/api/Invocation"), status);
            }
            catch (WebException ex) {
                // Ignore problems.
                Debug.WriteLine(ex.ToString());
            }

            e.Result = results;
        }

        // Encode a string for JSON
        static string JsonEncode(string s)
        {
            if (s == null)
                s = "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
