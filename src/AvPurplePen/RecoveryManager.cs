// RecoveryManager.cs
//
// Crash recovery: saving a snapshot of the user's unsaved work when an unhandled
// exception occurs, restarting the application so that it can pick that snapshot up,
// and cleaning up snapshots once they are no longer needed.
//
// The snapshot is deliberately written through EventDB directly rather than through
// Controller.Save/SaveAs, because those have side effects (overwriting the user's real
// file, rewriting UserSettings.LastLoadedFile, and marking the document clean) that are
// all wrong on a crash path.

using PurplePen;
using PurplePen.MapModel;
using System;
using System.Diagnostics;
using System.IO;

namespace AvPurplePen
{
    /// <summary>
    /// Describes a crash-recovery snapshot that has been written to disk.
    /// </summary>
    /// <param name="FilePath">Full path of the .ppen snapshot file.</param>
    /// <param name="OriginalFileName">Full path of the file the user was actually editing.</param>
    /// <param name="DirectoryPath">The per-crash directory holding the snapshot; deleted as a unit.</param>
    internal sealed record RecoverySnapshot(string FilePath, string OriginalFileName, string DirectoryPath);

    /// <summary>
    /// Saves and restores crash-recovery snapshots, and restarts the application after a
    /// crash. All methods are defensive: a failure anywhere in here must never prevent the
    /// crash dialog from appearing, so everything is wrapped in a try/catch and degrades to
    /// "no recovery available" rather than throwing.
    /// </summary>
    internal static class RecoveryManager
    {
        /// <summary>
        /// The name of the command-line switch that tells a newly started instance to load a
        /// recovery snapshot. Written here, parsed in <see cref="CommandLineOptions"/>.
        /// </summary>
        public const string RecoverySwitch = "-recovery";

        /// <summary>
        /// The directory holding all crash-recovery snapshots, one subdirectory per crash.
        ///
        /// LocalApplicationData is used rather than the temporary directory because Windows
        /// Storage Sense and Disk Cleanup delete the contents of %TEMP% without warning, and
        /// this data is the user's unsaved work. It is also not written next to the original
        /// file, because that directory may be read-only, on a network share, or inside a
        /// cloud-sync folder that would immediately upload a junk file.
        /// </summary>
        public static string RecoveryRootDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "PurplePen", "Recovery");

        /// <summary>
        /// Rolls back an undo command that was left half-executed by the exception.
        ///
        /// If the exception was thrown between UndoMgr.BeginCommand and EndCommand, the undo
        /// manager is still inside that command. In that state MarkClean and MarkDirty both
        /// throw, and the next BeginCommand would operate on a stack that was never closed.
        /// Rolling back returns the event database to the last consistent state.
        ///
        /// This is done up front, before the crash dialog appears, so it benefits the
        /// "Continue Working" path just as much as it benefits the snapshot: the user carries
        /// on from a consistent state rather than from the middle of an aborted edit.
        /// </summary>
        /// <param name="controller">The live controller, or null if no event is open.</param>
        public static void RollBackIncompleteCommand(Controller? controller)
        {
            if (controller == null)
                return;

            try {
                UndoMgr? undoMgr = controller.GetUndoMgr();
                if (undoMgr != null && undoMgr.CommandInProgress)
                    undoMgr.Rollback();
            }
            catch (Exception) {
                // A failed rollback must not prevent the crash dialog from appearing. The
                // snapshot below may then be inconsistent, but an inconsistent snapshot is
                // still better than losing the work entirely.
            }
        }

        /// <summary>
        /// Writes a snapshot of the currently loaded event, if it has unsaved changes.
        ///
        /// Called before the crash dialog is shown, not after: the process is in an unknown
        /// state and may die while the dialog is up, and the user's unsaved work is the one
        /// thing that cannot be regenerated. If the user then chooses to continue working,
        /// the snapshot is discarded by <see cref="DiscardSnapshot"/>.
        /// </summary>
        /// <param name="controller">The live controller, or null if no event is open.</param>
        /// <returns>
        /// The snapshot that was written, or null if there was nothing to save (no event
        /// open, no unsaved changes, or the snapshot could not be written).
        /// </returns>
        public static RecoverySnapshot? SaveSnapshot(Controller? controller)
        {
            if (controller == null)
                return null;

            try {
                // Nothing to recover if the file on disk already matches what is in memory.
                // RollBackIncompleteCommand has already run, so IsDirty is safe to read.
                if (!controller.IsDirty)
                    return null;

                string originalFileName = controller.FileName;
                if (string.IsNullOrEmpty(originalFileName))
                    return null;

                // One directory per crash, so that concurrent instances and repeated crashes
                // cannot overwrite each other's snapshots.
                string directory = Path.Combine(
                    RecoveryRootDirectory,
                    string.Format("{0:yyyyMMdd-HHmmss}-{1}", DateTime.Now, Environment.ProcessId));
                Directory.CreateDirectory(directory);

                // Inside that directory the snapshot keeps the ORIGINAL file's name. EventDB.Save
                // records the path it was saved to, and Path.GetFileName of that path feeds the
                // $(FileName) course text macro (see CourseFormatter). Giving the snapshot a
                // different name would silently corrupt that macro if the user then chose to
                // continue working. It also makes an orphaned snapshot self-describing to anyone
                // browsing the recovery folder.
                string snapshotPath = Path.Combine(directory, Path.GetFileName(originalFileName));

                // Save via the EventDB rather than Controller.Save/SaveAs. SaveAs would write to
                // the user's real file, repoint the controller at the snapshot, rewrite
                // UserSettings.LastLoadedFile, and mark the document clean -- all wrong here.
                //
                // The map file reference survives being written to a different directory: EventDB
                // writes both a path relative to the .ppen file and an absolute-path attribute,
                // and falls back to the absolute one when the relative one does not resolve.
                controller.GetEventDB().Save(snapshotPath);

                return new RecoverySnapshot(snapshotPath, Path.GetFullPath(originalFileName), directory);
            }
            catch (Exception) {
                // Never let a failed snapshot prevent the crash dialog from appearing. The user
                // simply won't be offered recovery.
                return null;
            }
        }

        /// <summary>
        /// Deletes a snapshot that is no longer needed, because the user chose to continue
        /// working rather than restart.
        /// </summary>
        /// <param name="snapshot">The snapshot to delete; may be null, in which case nothing happens.</param>
        public static void DiscardSnapshot(RecoverySnapshot? snapshot)
        {
            if (snapshot == null)
                return;

            DeleteDirectoryQuietly(snapshot.DirectoryPath);
        }

        /// <summary>
        /// Deletes the recovery directory containing a snapshot that has just been loaded
        /// successfully by a restarted instance. The data is now in memory (and marked dirty),
        /// so the file on disk has done its job.
        /// </summary>
        /// <param name="recoveryFilePath">The snapshot path that was passed on the command line.</param>
        public static void CleanUpAfterSuccessfulLoad(string? recoveryFilePath)
        {
            if (string.IsNullOrEmpty(recoveryFilePath))
                return;

            try {
                // Only delete the containing directory if it really is one of ours. A malformed
                // or hand-edited command line must not be able to make us delete an arbitrary
                // directory full of the user's files.
                string? directory = Path.GetDirectoryName(Path.GetFullPath(recoveryFilePath));
                if (directory == null)
                    return;

                string? parent = Path.GetDirectoryName(directory);
                if (parent != null &&
                    string.Equals(Path.GetFullPath(parent), Path.GetFullPath(RecoveryRootDirectory),
                                  StringComparison.OrdinalIgnoreCase)) {
                    DeleteDirectoryQuietly(directory);
                }
            }
            catch (Exception) {
                // A leftover snapshot is harmless; PurgeStaleSnapshots will get it eventually.
            }
        }

        /// <summary>
        /// Deletes recovery snapshots left behind by earlier sessions -- from a restart that
        /// never happened, or a second crash during recovery. Called once at startup.
        /// </summary>
        /// <param name="maximumAge">Snapshots older than this are deleted.</param>
        public static void PurgeStaleSnapshots(TimeSpan maximumAge)
        {
            try {
                if (!Directory.Exists(RecoveryRootDirectory))
                    return;

                DateTime cutoff = DateTime.Now - maximumAge;
                foreach (string directory in Directory.GetDirectories(RecoveryRootDirectory)) {
                    try {
                        if (Directory.GetLastWriteTime(directory) < cutoff)
                            DeleteDirectoryQuietly(directory);
                    }
                    catch (Exception) {
                        // Skip anything we can't examine or delete, and carry on with the rest.
                    }
                }
            }
            catch (Exception) {
                // Purging is pure housekeeping; a failure here must not affect startup.
            }
        }

        /// <summary>
        /// Starts a fresh instance of Purple Pen, asking it to reopen the file the user was
        /// working on and, if there is one, to restore the recovery snapshot.
        ///
        /// The caller is responsible for terminating this instance afterwards.
        /// </summary>
        /// <param name="originalFileName">
        /// The file the user was editing, or null/empty if no event was open (in which case the
        /// new instance just shows the welcome screen).
        /// </param>
        /// <param name="snapshot">The snapshot to restore, or null if there were no unsaved changes.</param>
        public static void RestartApplication(string? originalFileName, RecoverySnapshot? snapshot)
        {
            try {
                ProcessStartInfo startInfo = new ProcessStartInfo {
                    FileName = GetExecutablePath(),
                    UseShellExecute = false,

                    // Do not inherit the current working directory blindly; use the install
                    // directory so a deleted or unavailable working directory can't fail the launch.
                    WorkingDirectory = AppContext.BaseDirectory
                };

                // Use ArgumentList rather than building a command line by concatenation:
                // it quotes each argument correctly, and event file names routinely contain spaces.
                if (!string.IsNullOrEmpty(originalFileName))
                    startInfo.ArgumentList.Add(originalFileName);

                if (snapshot != null) {
                    startInfo.ArgumentList.Add(RecoverySwitch);
                    startInfo.ArgumentList.Add(snapshot.FilePath);
                }

                Process.Start(startInfo);
            }
            catch (Exception) {
                // If the restart fails, the snapshot is still on disk. It will be purged after
                // the retention period, and until then a user (or support) can open it by hand.
            }
        }

        /// <summary>
        /// Gets the path of the executable to relaunch.
        /// </summary>
        /// <returns>The full path of the current process's executable.</returns>
        private static string GetExecutablePath()
        {
            string? path = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(path))
                return path;

            // Environment.ProcessPath is only null in unusual hosting scenarios; fall back to
            // the process's main module.
            return Process.GetCurrentProcess().MainModule?.FileName ?? "";
        }

        /// <summary>
        /// Deletes a directory and everything in it, ignoring any failure.
        /// </summary>
        /// <param name="directory">The directory to delete.</param>
        private static void DeleteDirectoryQuietly(string directory)
        {
            try {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            catch (Exception) {
                // The file may be locked, or on a volume we can no longer write to. Leaving it
                // behind is harmless -- PurgeStaleSnapshots will retry on a later run.
            }
        }
    }
}
