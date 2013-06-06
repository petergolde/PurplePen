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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    // Check for new updates, download and run the installer.
    static class Updater
    {
        // Locations of key files on the server.
        // Each file has two lines -- version number available, and file name in the same directory.
        private const string downloadLocation = "http://purple-pen.org/downloads/";
        private const string latestVersionName = "latest_version.txt";
        private const string latestPreleaseName = "latest_prerelease_version.txt";

        // Directly inside temp directory to store downloaded versions.
        private const string directoryName = "PurplePen";

        private static bool updateCheckStarted;
        private static BackgroundWorker versionCheckWorker;

        private class CheckResults
        {
            public string CurrentVersion;
            public string CurrentFileName;
            public string PrereleaseVersion;
            public string PrereleaseFileName;
        }

        // Owner window for dialogs and messages we put up.
        public static IWin32Window OwnerWindow { get; set; }

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

        private static void AskToDownload(string versionNumber, string fileName)
        {
            // Ask to see if user wants to update.
            string message = string.Format(MiscText.NewerVersionAvailable, Util.PrettyVersionString(versionNumber), Util.PrettyVersionString(VersionNumber.Current));
            DialogResult answer = MessageBox.Show(OwnerWindow, message, MiscText.AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
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
            var result = downloadProgressDialog.ShowDialog(OwnerWindow);

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
                        catch (Exception) 
                        { }
                    }
                }
            }
            catch (IOException) { }
        }

        static void versionCheckWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // The version check has completed. The result is either null, or the version string of the new version.
            if (!e.Cancelled && e.Error == null && e.Result != null) {
                CheckResults results = (CheckResults)e.Result;
                if (results.CurrentVersion != null && Util.CompareVersionStrings(VersionNumber.Current, results.CurrentVersion) < 0) {
                    AskToDownload(results.CurrentVersion, results.CurrentFileName);
                }
                if (results.PrereleaseVersion != null && Util.CompareVersionStrings(VersionNumber.Current, results.PrereleaseVersion) < 0 && Util.SameExceptRevision(VersionNumber.Current, results.PrereleaseVersion)) {
                    AskToDownload(results.PrereleaseVersion, results.PrereleaseFileName);
                }
                
            }

            versionCheckWorker.Dispose();
            versionCheckWorker = null;
        }

        static void versionCheckWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            DeletePreviouslyDownloadedFiles();

            // We need to check to see if a new version is available. We do this in the background.
            // If a new version is available, the version number is returned as the result of the background
            // processing. If no new version is available, null is returned.
            WebClient client = new WebClient();
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
            try {
                latestPrerelease = client.DownloadString(downloadLocation + latestPreleaseName);
            }
            catch (WebException) {
                latestPrerelease = null;
            }

            if (latestVersion != null) {
                // Get first line and second line.
                string[] lines = latestVersion.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                string version = lines.Length > 0 ? lines[0] : null;
                string filename = lines.Length > 1 ? lines[1] : null;

                if (!string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(filename)) {
                    results.CurrentVersion = version;
                    results.CurrentFileName = filename;
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

            e.Result = results;
        }
    }
}
