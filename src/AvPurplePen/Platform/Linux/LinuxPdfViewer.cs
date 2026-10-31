// LinuxPdfViewer.cs
//
// Opens PDF files in the desktop's default PDF viewer on Linux, using xdg-utils.
// Based on the PdfTest sample's PdfViewer.
//
// xdg-mime is used first to check that a default viewer exists, because xdg-open's own failure
// for that case is just a generic exit code. xdg-open then launches the viewer: on GNOME and KDE
// it hands off to gio/kde-open and exits right away, but in its generic mode it runs the viewer
// in the foreground and doesn't exit until the viewer closes. So if xdg-open is still running
// after LaunchTimeout, the viewer is assumed to have started and is left running.
//
// Only compiled for the plain net10.0 target framework (see AvPurplePen.csproj).

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvPurplePen.Platform.Linux
{
    /// <summary>
    /// Why a PDF couldn't be opened in the default viewer.
    /// </summary>
    internal enum PdfViewerError
    {
        CommandNotFound,    // xdg-mime or xdg-open can't be run (xdg-utils isn't installed).
        NoDefaultViewer,    // No default application is configured for PDF files.
        LaunchFailed        // xdg-open failed.
    }

    /// <summary>
    /// Thrown when a PDF couldn't be opened in the default viewer.
    /// </summary>
    internal class PdfViewerException : Exception
    {
        /// <summary>Why the PDF couldn't be opened.</summary>
        public PdfViewerError Error { get; }

        /// <summary>
        /// Create the exception.
        /// </summary>
        /// <param name="error">Why the PDF couldn't be opened.</param>
        /// <param name="message">A description of the failure (not localized).</param>
        /// <param name="innerException">The underlying exception, if any.</param>
        public PdfViewerException(PdfViewerError error, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            Error = error;
        }
    }

    /// <summary>
    /// Opens PDF files in the desktop's default PDF viewer, using xdg-utils.
    /// </summary>
    internal static class LinuxPdfViewer
    {
        // How long to wait for xdg-open to exit before assuming it is hosting the viewer in the foreground.
        private static readonly TimeSpan LaunchTimeout = TimeSpan.FromSeconds(5);

        // Environment variables holding search paths that the AppImage's AppRun script prepends its own
        // directories to. Those entries are removed for the processes started here (see StartProcess).
        private static readonly string[] AppImagePathVariables = { "LD_LIBRARY_PATH", "XDG_DATA_DIRS", "PATH" };

        /// <summary>
        /// Opens the PDF in the default viewer, and completes once the viewer has been launched.
        /// </summary>
        /// <param name="pdfPath">The PDF file to open.</param>
        /// <exception cref="FileNotFoundException">The file doesn't exist.</exception>
        /// <exception cref="PdfViewerException">xdg-utils isn't installed, no default PDF viewer is configured, or the launch failed.</exception>
        public static async Task OpenAsync(string pdfPath)
        {
            string fullPath = Path.GetFullPath(pdfPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("PDF file not found.", fullPath);

            string defaultViewer = await GetDefaultViewerAsync();
            if (defaultViewer.Length == 0)
                throw new PdfViewerException(PdfViewerError.NoDefaultViewer, "No default PDF viewer is configured.");

            await LaunchAsync(fullPath);
        }

        /// <summary>
        /// Gets the desktop file ID of the default PDF viewer (e.g. "org.gnome.Papers.desktop").
        /// </summary>
        /// <returns>The desktop file ID, or an empty string if there is none.</returns>
        /// <exception cref="PdfViewerException">xdg-mime can't be run.</exception>
        public static async Task<string> GetDefaultViewerAsync()
        {
            using (Process process = StartProcess("xdg-mime", new[] { "query", "default", "application/pdf" }, redirectOutput: true)) {
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                return output.Trim();
            }
        }

        // Run xdg-open on the file. Completes when xdg-open exits successfully, or is still running after
        // LaunchTimeout. Throws PdfViewerException if it fails.
        private static async Task LaunchAsync(string fullPath)
        {
            // Wait on the Exited event rather than WaitForExitAsync: the viewer may inherit xdg-open's
            // stderr pipe, and WaitForExitAsync waits for that pipe to close, i.e. for the viewer to exit.
            TaskCompletionSource exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            StringBuilder errorOutput = new StringBuilder();

            using (Process process = StartProcess("xdg-open", new[] { fullPath }, redirectOutput: false, configure: p => {
                p.StartInfo.RedirectStandardError = true;
                p.EnableRaisingEvents = true;
                p.Exited += (sender, e) => exited.TrySetResult();
                p.ErrorDataReceived += (sender, e) => {
                    if (e.Data != null) {
                        lock (errorOutput)
                            errorOutput.AppendLine(e.Data);
                    }
                };
            })) {
                process.BeginErrorReadLine();

                if (await Task.WhenAny(exited.Task, Task.Delay(LaunchTimeout)) != exited.Task)
                    return; // Still running: xdg-open is hosting the viewer in the foreground.

                if (process.ExitCode != 0) {
                    // Give the last stderr lines a moment to arrive.
                    await Task.Delay(100);
                    string details;
                    lock (errorOutput)
                        details = errorOutput.ToString().Trim();
                    throw new PdfViewerException(PdfViewerError.LaunchFailed,
                        $"xdg-open failed: {DescribeExitCode(process.ExitCode)}" + (details.Length > 0 ? $" ({details})" : ""));
                }
            }
        }

        // Start a command, without a shell. configure can set further options before it starts.
        // Throws PdfViewerException if the command can't be run.
        private static Process StartProcess(string command, string[] arguments, bool redirectOutput, Action<Process>? configure = null)
        {
            Process process = new Process();
            process.StartInfo.FileName = command;
            foreach (string argument in arguments)
                process.StartInfo.ArgumentList.Add(argument);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = redirectOutput;
            RemoveAppImagePaths(process.StartInfo);
            configure?.Invoke(process);

            try {
                process.Start();
            }
            catch (Win32Exception ex) {
                process.Dispose();
                throw new PdfViewerException(PdfViewerError.CommandNotFound,
                    $"Could not run '{command}'. Is xdg-utils installed?", ex);
            }
            return process;
        }

        // When running from an AppImage, remove the AppImage's own directories from the search paths that its
        // AppRun script changed (e.g. the bundled ICU in LD_LIBRARY_PATH), so that xdg-utils and the viewer it
        // launches see the host's environment and don't load the AppImage's bundled libraries.
        private static void RemoveAppImagePaths(ProcessStartInfo startInfo)
        {
            string? appDir = Environment.GetEnvironmentVariable("APPDIR");
            if (string.IsNullOrEmpty(appDir))
                return;

            string appDirPrefix = appDir.TrimEnd('/') + "/";
            foreach (string variable in AppImagePathVariables) {
                if (!startInfo.Environment.TryGetValue(variable, out string? value) || value == null)
                    continue;

                List<string> entries = value.Split(':')
                    .Where(entry => entry != appDir && !entry.StartsWith(appDirPrefix, StringComparison.Ordinal))
                    .ToList();
                if (entries.Count == 0)
                    startInfo.Environment.Remove(variable);
                else
                    startInfo.Environment[variable] = string.Join(":", entries);
            }
        }

        // Describe an xdg-open exit code, as documented in xdg-open(1).
        private static string DescribeExitCode(int exitCode)
        {
            switch (exitCode) {
                case 1: return "error in command line syntax";
                case 2: return "file does not exist";
                case 3: return "a required tool could not be found";
                case 4: return "the action failed";
                case 5: return "no permission to read the file";
                default: return $"exit code {exitCode}";
            }
        }
    }
}
