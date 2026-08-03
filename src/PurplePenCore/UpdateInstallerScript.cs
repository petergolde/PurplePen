using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace PurplePen
{
    // The platform an update is being installed on. Passed in rather than detected, both because the
    // manifest already describes updates per platform and so that every platform's script can be
    // generated -- and tested -- from any one of them.
    public enum UpdatePlatform
    {
        Windows,
        MacOS,
        Linux
    }

    // Everything the platform layer needs in order to install a downloaded update: the text of a
    // script to write out, and the command line that runs it. Nothing here touches the file system
    // or starts a process; the caller writes ScriptText to a file named ScriptFileName and then
    // starts LauncherFileName with LauncherArguments, having substituted the script's full path for
    // the ScriptPathPlaceholder entry in the argument list.
    public class InstallScript
    {
        public string ScriptFileName;         // File name (not path) the script should be written under.
        public string ScriptText;             // Contents of the script.
        public string LauncherFileName;       // Program that runs the script, e.g. "cmd.exe" or "/bin/sh".
        public string[] LauncherArguments;    // Arguments to it; one entry is ScriptPathPlaceholder.

        // The entry in LauncherArguments that the caller replaces with the full path of the script
        // file it wrote. A placeholder is used rather than having the caller pass the path in
        // beforehand, because the caller doesn't know the file name until this object is built.
        public const string ScriptPathPlaceholder = "<script>";
    }

    // Builds the script that installs a downloaded update. The script's first job is to wait for
    // Purple Pen to exit -- an installer can't replace files that are still in use -- and its second
    // is to run whatever counts as "installing" for the platform and file type at hand.
    //
    // The general shape follows NetSparkle's SparkleUpdater.RunDownloadedInstaller, but with two
    // deliberate differences. Nothing here ever runs "sudo": a script started from a GUI has no
    // terminal to type a password into, so an elevation prompt would simply fail with no explanation.
    // Linux packages are handed to the desktop's own installer via xdg-open instead, which prompts
    // for authentication properly. And the macOS in-place replacement moves the old application
    // aside rather than deleting it, so that a failure part-way through leaves a working copy behind.
    public static class UpdateInstallerScript
    {
        // How many seconds the script waits for Purple Pen to exit before giving up and installing
        // anyway. A bound is needed so that a wedged process can't leave the script spinning forever;
        // 90 seconds is far longer than a normal shutdown and matches what NetSparkle uses.
        private const int maxWaitSeconds = 90;

        // Recognized file extensions, longest first so that ".tar.gz" is matched before ".gz" would
        // be. Path.GetExtension is no use for these: for "purplepen.tar.gz" it returns ".gz".
        private static readonly string[] knownExtensions = {
            ".tar.gz", ".exe", ".msi", ".dmg", ".zip", ".deb", ".rpm"
        };

        // Which of the recognized extensions each platform can install.
        private static readonly Dictionary<UpdatePlatform, string[]> supportedExtensions =
            new Dictionary<UpdatePlatform, string[]> {
                { UpdatePlatform.Windows, new string[] { ".exe", ".msi" } },
                { UpdatePlatform.MacOS, new string[] { ".dmg", ".zip" } },
                { UpdatePlatform.Linux, new string[] { ".deb", ".rpm", ".tar.gz" } },
            };

        // Returns true if this platform knows how to install a file with this name's extension, so
        // that a caller can offer to install rather than just reporting where the download landed.
        // Callers should use this before Build, which throws for anything it can't handle.
        //   platform: the platform the update will be installed on.
        //   downloadedFilePath: path or name of the downloaded file; only its extension matters.
        public static bool IsSupported(UpdatePlatform platform, string downloadedFilePath)
        {
            if (downloadedFilePath == null)
                return false;

            string extension = GetKnownExtension(downloadedFilePath);
            if (extension == null)
                return false;

            return Array.IndexOf(supportedExtensions[platform], extension) >= 0;
        }

        // Build the script that waits for Purple Pen to exit and then installs the update.
        //   platform: the platform the update will be installed on.
        //   downloadedFilePath: full path of the downloaded, hash-verified update file.
        //   processIdToWaitFor: process id of the running Purple Pen, which the script waits to exit.
        //   applicationPath: what is being replaced -- on macOS the ".app" bundle of the running
        //     application, on Linux the directory the application is installed in. Not used on
        //     Windows, where the installer knows where to put things, and may be null there.
        //   executableName: file name (not path) of the Purple Pen executable within applicationPath,
        //     used to start the new version once it is installed. Only meaningful for a Linux
        //     tarball, which is a plain drop-in replacement; every other kind of update either
        //     relaunches itself or leaves an installer in charge. Null when the caller could not
        //     determine the name, in which case the script installs the update but does not restart
        //     Purple Pen -- better than guessing a name and running the wrong program.
        // Throws NotSupportedException if this platform can't install this kind of file; use
        // IsSupported to check first.
        public static InstallScript Build(UpdatePlatform platform, string downloadedFilePath, int processIdToWaitFor, string applicationPath, string executableName)
        {
            if (downloadedFilePath == null)
                throw new ArgumentNullException(nameof(downloadedFilePath));

            if (!IsSupported(platform, downloadedFilePath)) {
                throw new NotSupportedException(
                    string.Format("Cannot install \"{0}\" on {1}.", downloadedFilePath, platform));
            }

            string extension = GetKnownExtension(downloadedFilePath);

            if (platform == UpdatePlatform.Windows)
                return BuildWindowsScript(downloadedFilePath, extension, processIdToWaitFor);
            else
                return BuildUnixScript(platform, downloadedFilePath, extension, processIdToWaitFor, applicationPath, executableName);
        }

        // Build the Windows batch file: wait for the process to go away, then run the installer.
        //   downloadedFilePath: full path of the downloaded update file.
        //   extension: its extension, already known to be ".exe" or ".msi".
        //   processIdToWaitFor: process id of the running Purple Pen.
        private static InstallScript BuildWindowsScript(string downloadedFilePath, string extension, int processIdToWaitFor)
        {
            string processId = processIdToWaitFor.ToString(CultureInfo.InvariantCulture);
            StringBuilder script = new StringBuilder();

            script.AppendLine("@echo off");
            script.AppendLine("rem Installs a Purple Pen update. Written by Purple Pen itself; safe to delete.");
            script.AppendLine();

            // Wait for Purple Pen to exit. tasklist filtered by process id prints a header and no
            // rows once the process is gone, so piping through "find" on the id and testing errorlevel
            // detects that reliably. The counter bounds the wait.
            script.AppendLine("setlocal enabledelayedexpansion");
            script.AppendLine("set /a waited=0");
            script.AppendLine(":waitloop");
            script.AppendFormat("tasklist /FI \"PID eq {0}\" /NH 2>nul | find \"{0}\" >nul", processId).AppendLine();
            script.AppendLine("if errorlevel 1 goto installnow");
            script.AppendLine("set /a waited+=1");
            script.AppendFormat("if !waited! GEQ {0} goto installnow", maxWaitSeconds).AppendLine();

            // "timeout" needs /nobreak so a stray keypress can't cut the wait short, and its output
            // is discarded because the console window is hidden anyway.
            script.AppendLine("timeout /t 1 /nobreak >nul");
            script.AppendLine("goto waitloop");
            script.AppendLine();
            script.AppendLine(":installnow");

            if (extension == ".msi") {
                // An .msi isn't executable in its own right; msiexec installs it.
                script.AppendFormat("msiexec /i {0}", Quote(downloadedFilePath)).AppendLine();
            }
            else {
                // The empty "" is the window title argument to start, which is required whenever the
                // path that follows is quoted -- without it, start treats the quoted path as the title
                // and does nothing. /wait keeps the script alive until the installer finishes.
                script.AppendFormat("start \"\" /wait {0}", Quote(downloadedFilePath)).AppendLine();
            }

            return new InstallScript {
                ScriptFileName = "PurplePenInstallUpdate.cmd",
                ScriptText = script.ToString(),
                LauncherFileName = "cmd.exe",

                // /c runs the script and exits. The path is quoted by ProcessStartInfo.ArgumentList,
                // so no quoting is done here.
                LauncherArguments = new string[] { "/c", InstallScript.ScriptPathPlaceholder }
            };
        }

        // Build the macOS or Linux shell script: wait for the process to go away, then install.
        //   platform: MacOS or Linux.
        //   downloadedFilePath: full path of the downloaded update file.
        //   extension: its extension, already known to be supported on this platform.
        //   processIdToWaitFor: process id of the running Purple Pen.
        //   applicationPath: the ".app" bundle (macOS) or install directory (Linux) being replaced.
        //   executableName: name of the executable to restart afterwards, or null not to restart.
        private static InstallScript BuildUnixScript(UpdatePlatform platform, string downloadedFilePath, string extension, int processIdToWaitFor, string applicationPath, string executableName)
        {
            string processId = processIdToWaitFor.ToString(CultureInfo.InvariantCulture);
            StringBuilder script = new StringBuilder();

            script.AppendLine("#!/bin/sh");
            script.AppendLine("# Installs a Purple Pen update. Written by Purple Pen itself; safe to delete.");
            script.AppendLine();

            // Wait for Purple Pen to exit. "ps -p" succeeds while the process exists; the counter
            // bounds the wait the same way the Windows script does.
            script.AppendLine("waited=0");
            script.AppendFormat("while ps -p {0} > /dev/null 2>&1; do", processId).AppendLine();
            script.AppendFormat("    if [ $waited -ge {0} ]; then break; fi", maxWaitSeconds).AppendLine();
            script.AppendLine("    waited=$((waited+1))");
            script.AppendLine("    sleep 1");
            script.AppendLine("done");
            script.AppendLine();

            if (platform == UpdatePlatform.MacOS) {
                if (extension == ".dmg") {
                    // "open" mounts the disk image and shows it in Finder. The user drags Purple Pen
                    // to Applications from there; there is no reliable unattended way to do it for
                    // them, because the image's layout is up to whoever built it.
                    script.AppendFormat("open {0}", Quote(downloadedFilePath)).AppendLine();
                }
                else {
                    AppendMacZipReplacement(script, downloadedFilePath, applicationPath);
                }
            }
            else {
                if (extension == ".tar.gz") {
                    AppendLinuxArchiveExtraction(script, downloadedFilePath, applicationPath, executableName);
                }
                else {
                    // .deb and .rpm both need root. Rather than running the package tools directly --
                    // which would need a password we have no way to ask for -- hand the file to the
                    // desktop environment, whose graphical installer (GNOME Software, Discover,
                    // GDebi, ...) prompts for authentication properly.
                    script.AppendFormat("xdg-open {0}", Quote(downloadedFilePath)).AppendLine();
                }
            }

            return new InstallScript {
                ScriptFileName = "PurplePenInstallUpdate.sh",
                ScriptText = script.ToString(),

                // Run the script by handing it to the shell rather than executing it directly, so
                // there is no need to make the file executable first.
                LauncherFileName = "/bin/sh",
                LauncherArguments = new string[] { InstallScript.ScriptPathPlaceholder }
            };
        }

        // Append the macOS in-place replacement of the application bundle: expand the new one into a
        // staging directory, swap it for the old one, and relaunch.
        //
        // The swap moves the old bundle aside instead of deleting it, and puts it back if the move of
        // the new one fails. Deleting first -- which is what NetSparkle does -- means that a failure
        // between the delete and the move leaves the user with no application at all and no way to
        // get one except to download it again by hand.
        //   script: the script being built.
        //   downloadedFilePath: full path of the downloaded .zip.
        //   applicationPath: full path of the ".app" bundle to replace.
        private static void AppendMacZipReplacement(StringBuilder script, string downloadedFilePath, string applicationPath)
        {
            // Paths go into shell variables once, at the top, rather than being repeated inline. It
            // keeps the rest of the script readable and means each path is quoted in exactly one place.
            script.AppendFormat("ZIPFILE={0}", Quote(downloadedFilePath)).AppendLine();
            script.AppendFormat("APP={0}", Quote(applicationPath)).AppendLine();
            script.AppendLine("OLDAPP=\"$APP.old\"");
            script.AppendLine("STAGING=\"$(mktemp -d)\"");
            script.AppendLine();

            // ditto, rather than unzip, because it preserves resource forks, symlinks and extended
            // attributes -- all of which an .app bundle relies on, and unzip discards.
            script.AppendLine("ditto -x -k \"$ZIPFILE\" \"$STAGING\" || exit 1");
            script.AppendLine();

            // The bundle may be at the root of the archive or one level down, depending on how the
            // zip was made, so search rather than assuming.
            script.AppendLine("NEWAPP=\"$(find \"$STAGING\" -maxdepth 2 -name '*.app' -print 2>/dev/null | head -n 1)\"");
            script.AppendLine("if [ -z \"$NEWAPP\" ]; then rm -rf \"$STAGING\"; exit 1; fi");
            script.AppendLine();

            // Swap, keeping the old bundle until the new one is safely in place, and putting it back
            // if the move fails.
            script.AppendLine("rm -rf \"$OLDAPP\"");
            script.AppendLine("mv \"$APP\" \"$OLDAPP\" || { rm -rf \"$STAGING\"; exit 1; }");
            script.AppendLine("if mv \"$NEWAPP\" \"$APP\"; then");
            script.AppendLine("    rm -rf \"$OLDAPP\"");
            script.AppendLine("else");
            script.AppendLine("    mv \"$OLDAPP\" \"$APP\"");
            script.AppendLine("    rm -rf \"$STAGING\"");
            script.AppendLine("    exit 1");
            script.AppendLine("fi");
            script.AppendLine("rm -rf \"$STAGING\"");
            script.AppendLine();

            // Downloaded files carry a quarantine attribute that makes macOS refuse to launch them
            // without a warning; clearing it on the bundle we just installed avoids that. It is
            // allowed to fail (an older macOS may not have xattr), hence the "|| true".
            script.AppendLine("xattr -dr com.apple.quarantine \"$APP\" 2>/dev/null || true");
            script.AppendLine("open \"$APP\"");
        }

        // Append the Linux extraction of a .tar.gz over the installation directory, and the relaunch.
        //   script: the script being built.
        //   downloadedFilePath: full path of the downloaded .tar.gz.
        //   applicationPath: directory the application is installed in.
        //   executableName: name of the executable to restart afterwards, or null not to restart.
        private static void AppendLinuxArchiveExtraction(StringBuilder script, string downloadedFilePath, string applicationPath, string executableName)
        {
            script.AppendFormat("ARCHIVE={0}", Quote(downloadedFilePath)).AppendLine();
            script.AppendFormat("INSTALLDIR={0}", Quote(applicationPath)).AppendLine();
            script.AppendLine();

            // --overwrite so files left over from the previous version are replaced rather than
            // skipped when their timestamps happen to be newer.
            script.AppendLine("tar -xzf \"$ARCHIVE\" -C \"$INSTALLDIR\" --overwrite || exit 1");
            script.AppendLine();

            // A tarball drop-in replacement is complete once it is extracted, so start the new
            // version straight away -- but only if the caller could tell us what it is called. The
            // -x test covers the case where the new version renamed or dropped the executable.
            if (!string.IsNullOrEmpty(executableName)) {
                script.AppendFormat("EXECUTABLE=\"$INSTALLDIR/{0}\"", executableName).AppendLine();
                script.AppendLine("if [ -x \"$EXECUTABLE\" ]; then");
                script.AppendLine("    \"$EXECUTABLE\" &");
                script.AppendLine("fi");
            }
        }

        // Returns the longest recognized extension that path ends with, or null if it ends with none
        // of them. Matching is case-insensitive, and works on multi-part extensions like ".tar.gz",
        // which Path.GetExtension reports as just ".gz".
        //   path: the file path or name to examine.
        private static string GetKnownExtension(string path)
        {
            foreach (string extension in knownExtensions) {
                if (path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    return extension;
            }

            return null;
        }

        // Wrap a path in double quotes so that spaces in it don't split it into several arguments.
        // Both cmd and sh treat double quotes this way. Paths here are file names we chose and
        // directories the application is installed in, so there is nothing to escape beyond this.
        //   path: the path to quote.
        private static string Quote(string path)
        {
            return "\"" + path + "\"";
        }
    }
}
