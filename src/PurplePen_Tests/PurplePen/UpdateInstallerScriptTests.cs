/* Copyright (c) Peter Golde
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

#if TEST
using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PurplePen.Tests
{
    // Tests for UpdateInstallerScript, which builds the script that waits for Purple Pen to exit and
    // then installs a downloaded update.
    //
    // Because Build takes the target platform as a parameter rather than detecting it, all three
    // platforms are covered from a single (Windows) test run. Nothing here writes a file or starts a
    // process -- the class produces text, and these tests read that text.
    [TestClass]
    public class UpdateInstallerScriptTests
    {
        // Process id used throughout, chosen to be distinctive enough that finding it in the script
        // text can't be a coincidence.
        private const int testProcessId = 47110;

        // Application paths passed to Build. Only used on macOS and Linux.
        private const string macAppPath = "/Applications/Purple Pen.app";
        private const string linuxInstallDirectory = "/opt/purplepen";

        // Name of the executable to restart after a Linux tarball update. In the application this
        // comes from the running process rather than being a constant.
        private const string executableName = "AvPurplePen";

        // Build a script and return its text, for the common case where only the text matters.
        //   platform: platform to build for.
        //   downloadedFilePath: the downloaded update file.
        //   applicationPath: the bundle or install directory, where the platform needs one.
        private static string BuildScriptText(UpdatePlatform platform, string downloadedFilePath, string applicationPath)
        {
            return UpdateInstallerScript.Build(platform, downloadedFilePath, testProcessId, applicationPath, executableName).ScriptText;
        }

        // ---------- Which files each platform can install ----------

        [TestMethod]
        public void SupportedExtensionsPerPlatform()
        {
            // Windows installers.
            Assert.IsTrue(UpdateInstallerScript.IsSupported(UpdatePlatform.Windows, @"C:\temp\PurplePen.exe"));
            Assert.IsTrue(UpdateInstallerScript.IsSupported(UpdatePlatform.Windows, @"C:\temp\PurplePen.msi"));

            // macOS.
            Assert.IsTrue(UpdateInstallerScript.IsSupported(UpdatePlatform.MacOS, "/tmp/PurplePen.dmg"));
            Assert.IsTrue(UpdateInstallerScript.IsSupported(UpdatePlatform.MacOS, "/tmp/PurplePen.zip"));

            // Linux.
            Assert.IsTrue(UpdateInstallerScript.IsSupported(UpdatePlatform.Linux, "/tmp/purplepen.deb"));
            Assert.IsTrue(UpdateInstallerScript.IsSupported(UpdatePlatform.Linux, "/tmp/purplepen.rpm"));
            Assert.IsTrue(UpdateInstallerScript.IsSupported(UpdatePlatform.Linux, "/tmp/purplepen.tar.gz"));
        }

        [TestMethod]
        public void ExtensionsFromOtherPlatformsAreNotSupported()
        {
            // Each of these is a perfectly good installer -- just not on this platform.
            Assert.IsFalse(UpdateInstallerScript.IsSupported(UpdatePlatform.Windows, "/tmp/PurplePen.dmg"));
            Assert.IsFalse(UpdateInstallerScript.IsSupported(UpdatePlatform.Windows, "/tmp/purplepen.deb"));
            Assert.IsFalse(UpdateInstallerScript.IsSupported(UpdatePlatform.Windows, "/tmp/purplepen.tar.gz"));

            Assert.IsFalse(UpdateInstallerScript.IsSupported(UpdatePlatform.MacOS, @"C:\temp\PurplePen.exe"));
            Assert.IsFalse(UpdateInstallerScript.IsSupported(UpdatePlatform.MacOS, @"C:\temp\PurplePen.msi"));
            Assert.IsFalse(UpdateInstallerScript.IsSupported(UpdatePlatform.MacOS, "/tmp/purplepen.rpm"));

            Assert.IsFalse(UpdateInstallerScript.IsSupported(UpdatePlatform.Linux, @"C:\temp\PurplePen.msi"));
            Assert.IsFalse(UpdateInstallerScript.IsSupported(UpdatePlatform.Linux, "/tmp/PurplePen.dmg"));

            // A .zip is a macOS update, not a Linux one.
            Assert.IsFalse(UpdateInstallerScript.IsSupported(UpdatePlatform.Linux, "/tmp/purplepen.zip"));
        }

        [TestMethod]
        public void UnknownExtensionsAreNotSupported()
        {
            Assert.IsFalse(UpdateInstallerScript.IsSupported(UpdatePlatform.Windows, @"C:\temp\readme.txt"));
            Assert.IsFalse(UpdateInstallerScript.IsSupported(UpdatePlatform.MacOS, "/tmp/PurplePen"));
            Assert.IsFalse(UpdateInstallerScript.IsSupported(UpdatePlatform.Linux, "/tmp/purplepen.tar"));
            Assert.IsFalse(UpdateInstallerScript.IsSupported(UpdatePlatform.Windows, null));
        }

        [TestMethod]
        public void ExtensionsMatchIgnoringCase()
        {
            // Servers and build scripts are inconsistent about case; the extension of a URL's last
            // segment shouldn't decide whether an update can be installed.
            Assert.IsTrue(UpdateInstallerScript.IsSupported(UpdatePlatform.Windows, @"C:\temp\PurplePen.EXE"));
            Assert.IsTrue(UpdateInstallerScript.IsSupported(UpdatePlatform.MacOS, "/tmp/PurplePen.DMG"));
            Assert.IsTrue(UpdateInstallerScript.IsSupported(UpdatePlatform.Linux, "/tmp/purplepen.TAR.GZ"));
        }

        [TestMethod]
        public void TarGzIsTreatedAsOneExtension()
        {
            // Path.GetExtension would report ".gz" here. If the code used it, a .tar.gz would look
            // like an unknown ".gz" file and Linux updates would never install.
            Assert.IsTrue(UpdateInstallerScript.IsSupported(UpdatePlatform.Linux, "/tmp/purplepen-1.2.tar.gz"));

            // A bare .gz is not a supported update, and must not be mistaken for a .tar.gz.
            Assert.IsFalse(UpdateInstallerScript.IsSupported(UpdatePlatform.Linux, "/tmp/purplepen.gz"));

            string script = BuildScriptText(UpdatePlatform.Linux, "/tmp/purplepen-1.2.tar.gz", linuxInstallDirectory);
            StringAssert.Contains(script, "tar -xzf");
        }

        [TestMethod]
        public void BuildThrowsForUnsupportedCombinations()
        {
            // Build is documented to throw rather than silently produce a script that does nothing;
            // callers are expected to have asked IsSupported first.
            Assert.ThrowsExactly<NotSupportedException>(() =>
                UpdateInstallerScript.Build(UpdatePlatform.Linux, "/tmp/PurplePen.msi", testProcessId, linuxInstallDirectory, executableName));

            Assert.ThrowsExactly<NotSupportedException>(() =>
                UpdateInstallerScript.Build(UpdatePlatform.Windows, "/tmp/purplepen.deb", testProcessId, null, null));

            Assert.ThrowsExactly<NotSupportedException>(() =>
                UpdateInstallerScript.Build(UpdatePlatform.MacOS, "/tmp/readme.txt", testProcessId, macAppPath, null));

            Assert.ThrowsExactly<ArgumentNullException>(() =>
                UpdateInstallerScript.Build(UpdatePlatform.Windows, null, testProcessId, null, null));
        }

        // ---------- Waiting for Purple Pen to exit ----------

        [TestMethod]
        public void WindowsScriptWaitsForTheProcessToExit()
        {
            string script = BuildScriptText(UpdatePlatform.Windows, @"C:\temp\PurplePen.exe", null);

            // The process id has to appear in the tasklist filter, or the script would either install
            // immediately (over a running program) or wait for the wrong process.
            StringAssert.Contains(script, "tasklist");
            StringAssert.Contains(script, "PID eq " + testProcessId.ToString());

            // A bounded wait, so a wedged Purple Pen can't leave the script running forever.
            StringAssert.Contains(script, "GEQ 90");
            StringAssert.Contains(script, "timeout /t 1");
        }

        [TestMethod]
        public void UnixScriptWaitsForTheProcessToExit()
        {
            string macScript = BuildScriptText(UpdatePlatform.MacOS, "/tmp/PurplePen.dmg", macAppPath);
            string linuxScript = BuildScriptText(UpdatePlatform.Linux, "/tmp/purplepen.deb", linuxInstallDirectory);

            foreach (string script in new string[] { macScript, linuxScript }) {
                StringAssert.Contains(script, "ps -p " + testProcessId.ToString());
                StringAssert.Contains(script, "sleep 1");
                StringAssert.Contains(script, "-ge 90");
            }
        }

        // ---------- Windows ----------

        [TestMethod]
        public void WindowsExeIsRunDirectly()
        {
            InstallScript script = UpdateInstallerScript.Build(
                UpdatePlatform.Windows, @"C:\Users\me\AppData\Local\Temp\PurplePen\PurplePen_4.0.exe", testProcessId, null, null);

            // The empty "" is start's window-title argument. Without it, start treats the quoted path
            // that follows as the title and never launches anything.
            StringAssert.Contains(script.ScriptText, "start \"\" /wait \"C:\\Users\\me\\AppData\\Local\\Temp\\PurplePen\\PurplePen_4.0.exe\"");

            Assert.AreEqual("PurplePenInstallUpdate.cmd", script.ScriptFileName);
            Assert.AreEqual("cmd.exe", script.LauncherFileName);
            CollectionAssert.AreEqual(new string[] { "/c", InstallScript.ScriptPathPlaceholder }, script.LauncherArguments);
        }

        [TestMethod]
        public void WindowsMsiIsRunThroughMsiexec()
        {
            // An .msi is data, not an executable, so running it directly would do nothing.
            string script = BuildScriptText(UpdatePlatform.Windows, @"C:\temp\PurplePen_4.0.msi", null);

            StringAssert.Contains(script, "msiexec /i \"C:\\temp\\PurplePen_4.0.msi\"");
            Assert.IsFalse(script.Contains("start \"\" /wait"), "an .msi should not be launched directly");
        }

        [TestMethod]
        public void WindowsScriptIsABatchFile()
        {
            string script = BuildScriptText(UpdatePlatform.Windows, @"C:\temp\PurplePen.exe", null);

            // "@echo off" keeps the console window quiet; the delayed expansion is what makes the
            // !waited! counter in the wait loop work at all.
            StringAssert.StartsWith(script, "@echo off");
            StringAssert.Contains(script, "setlocal enabledelayedexpansion");
        }

        // ---------- macOS ----------

        [TestMethod]
        public void MacDmgIsOpenedInFinder()
        {
            InstallScript script = UpdateInstallerScript.Build(
                UpdatePlatform.MacOS, "/tmp/PurplePen 4.0.dmg", testProcessId, macAppPath, executableName);

            // A disk image's layout is up to whoever built it, so the user does the drag; all the
            // script can usefully do is mount it and show it.
            StringAssert.Contains(script.ScriptText, "open \"/tmp/PurplePen 4.0.dmg\"");

            Assert.AreEqual("PurplePenInstallUpdate.sh", script.ScriptFileName);
            Assert.AreEqual("/bin/sh", script.LauncherFileName);
            CollectionAssert.AreEqual(new string[] { InstallScript.ScriptPathPlaceholder }, script.LauncherArguments);
        }

        [TestMethod]
        public void MacZipReplacesTheApplicationBundle()
        {
            string script = BuildScriptText(UpdatePlatform.MacOS, "/tmp/PurplePen_4.0.zip", macAppPath);

            // Both paths reach the script.
            StringAssert.Contains(script, "ZIPFILE=\"/tmp/PurplePen_4.0.zip\"");
            StringAssert.Contains(script, "APP=\"/Applications/Purple Pen.app\"");

            // ditto rather than unzip: an .app bundle depends on symlinks and extended attributes
            // that unzip discards.
            StringAssert.Contains(script, "ditto -x -k \"$ZIPFILE\"");

            // The bundle is found rather than assumed, since it may sit at the archive root or one
            // level down.
            StringAssert.Contains(script, "-name '*.app'");

            // Quarantine is cleared, or macOS warns about the app we just installed, and the new
            // version is started.
            StringAssert.Contains(script, "xattr -dr com.apple.quarantine \"$APP\"");
            StringAssert.Contains(script, "open \"$APP\"");
        }

        [TestMethod]
        public void MacZipReplacementIsRecoverable()
        {
            string script = BuildScriptText(UpdatePlatform.MacOS, "/tmp/PurplePen_4.0.zip", macAppPath);

            // The old bundle is moved aside, not deleted, before the new one goes in...
            StringAssert.Contains(script, "mv \"$APP\" \"$OLDAPP\"");

            // ...and is put back if the new one can't be moved into place. Deleting first would mean
            // a failure at the wrong moment leaves the user with no application at all.
            StringAssert.Contains(script, "mv \"$OLDAPP\" \"$APP\"");

            // The old copy is only discarded on the success branch.
            int successfulMove = script.IndexOf("if mv \"$NEWAPP\" \"$APP\"; then", StringComparison.Ordinal);
            int discardOldCopy = script.IndexOf("    rm -rf \"$OLDAPP\"", StringComparison.Ordinal);
            Assert.IsTrue(successfulMove >= 0, "expected a guarded move of the new bundle");
            Assert.IsTrue(discardOldCopy > successfulMove, "the old bundle should only be deleted after the new one is in place");
        }

        // ---------- Linux ----------

        [TestMethod]
        public void LinuxPackagesAreHandedToTheDesktop()
        {
            string debScript = BuildScriptText(UpdatePlatform.Linux, "/tmp/purplepen_4.0.deb", linuxInstallDirectory);
            string rpmScript = BuildScriptText(UpdatePlatform.Linux, "/tmp/purplepen-4.0.rpm", linuxInstallDirectory);

            StringAssert.Contains(debScript, "xdg-open \"/tmp/purplepen_4.0.deb\"");
            StringAssert.Contains(rpmScript, "xdg-open \"/tmp/purplepen-4.0.rpm\"");
        }

        [TestMethod]
        public void LinuxScriptNeverUsesSudo()
        {
            // A script started from a GUI has no terminal, so a password prompt has nowhere to appear
            // and the install would fail silently. Installing packages is left to the desktop's own
            // installer, which can ask for authentication properly.
            foreach (string file in new string[] { "/tmp/purplepen.deb", "/tmp/purplepen.rpm", "/tmp/purplepen.tar.gz" }) {
                string script = BuildScriptText(UpdatePlatform.Linux, file, linuxInstallDirectory);
                Assert.IsFalse(script.Contains("sudo"), "the Linux script must not use sudo, but does for " + file);
                Assert.IsFalse(script.Contains("pkexec"), "the Linux script must not use pkexec, but does for " + file);
            }
        }

        [TestMethod]
        public void LinuxArchiveIsExtractedOverTheInstallDirectory()
        {
            string script = BuildScriptText(UpdatePlatform.Linux, "/tmp/purplepen-4.0.tar.gz", linuxInstallDirectory);

            StringAssert.Contains(script, "ARCHIVE=\"/tmp/purplepen-4.0.tar.gz\"");
            StringAssert.Contains(script, "INSTALLDIR=\"/opt/purplepen\"");

            // --overwrite, or files from the previous version survive when their timestamps happen to
            // look newer than the ones being extracted.
            StringAssert.Contains(script, "tar -xzf \"$ARCHIVE\" -C \"$INSTALLDIR\" --overwrite");

            // A tarball replacement is complete once extracted, so the new version is relaunched --
            // under whatever name the caller reported, not a name assumed here.
            StringAssert.Contains(script, "EXECUTABLE=\"$INSTALLDIR/AvPurplePen\"");
            StringAssert.Contains(script, "\"$EXECUTABLE\" &");
        }

        [TestMethod]
        public void LinuxArchiveUsesTheExecutableNameItIsGiven()
        {
            // The name comes from the running process, so a renamed executable has to follow through
            // into the script rather than a hard-coded name being used.
            string script = UpdateInstallerScript.Build(
                UpdatePlatform.Linux, "/tmp/purplepen-4.0.tar.gz", testProcessId, linuxInstallDirectory, "purple-pen").ScriptText;

            StringAssert.Contains(script, "EXECUTABLE=\"$INSTALLDIR/purple-pen\"");
            Assert.IsFalse(script.Contains("AvPurplePen"), "the script should not contain any assumed executable name");
        }

        [TestMethod]
        public void LinuxArchiveSkipsTheRestartWhenTheExecutableNameIsUnknown()
        {
            // Started as "dotnet AvPurplePen.dll", the running process is the shared .NET host, so
            // the caller can't say what to restart and passes null. The update must still install;
            // only the restart is given up on, because relaunching the wrong program would be worse
            // than leaving the user to start Purple Pen themselves.
            string script = UpdateInstallerScript.Build(
                UpdatePlatform.Linux, "/tmp/purplepen-4.0.tar.gz", testProcessId, linuxInstallDirectory, null).ScriptText;

            StringAssert.Contains(script, "tar -xzf \"$ARCHIVE\" -C \"$INSTALLDIR\" --overwrite");
            Assert.IsFalse(script.Contains("EXECUTABLE"), "no restart should be attempted without a name to restart");
        }

        // ---------- Paths ----------

        [TestMethod]
        public void PathsWithSpacesAreQuoted()
        {
            // Purple Pen's own download directory sits under the user's profile, which routinely has
            // a space in it. An unquoted path there would split into two arguments.
            string windowsScript = BuildScriptText(UpdatePlatform.Windows, @"C:\Documents and Settings\me\Purple Pen 4.0.exe", null);
            StringAssert.Contains(windowsScript, "\"C:\\Documents and Settings\\me\\Purple Pen 4.0.exe\"");

            string macScript = BuildScriptText(UpdatePlatform.MacOS, "/tmp/my downloads/Purple Pen.dmg", macAppPath);
            StringAssert.Contains(macScript, "open \"/tmp/my downloads/Purple Pen.dmg\"");

            string linuxScript = BuildScriptText(UpdatePlatform.Linux, "/tmp/my downloads/purple pen.deb", linuxInstallDirectory);
            StringAssert.Contains(linuxScript, "xdg-open \"/tmp/my downloads/purple pen.deb\"");
        }
    }
}
#endif
