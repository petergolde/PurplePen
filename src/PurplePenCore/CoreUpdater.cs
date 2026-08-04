using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PurplePen
{
    // Class to do the core operations and finding and downloading updates. The platform specific
    // AvPurplePen does the actual running of the downloaded update, and all UI around that.
    //
    // The update manifest is a JSON document listing every update that is available, for every
    // platform and channel. It looks like this:
    //
    //     {
    //       "updates": [
    //         {
    //           "title": "Purple Pen 1.1 beta 1",
    //           "version": "1.1.0.0",
    //           "platform": "win-x64",
    //           "channel": "beta",
    //           "url": "https://example.com/update2.exe",
    //           "sha256": "3d2f...(64 lowercase hex digits)...9a1c",
    //           "message": "Beta release with new features."
    //         }
    //       ]
    //     }
    //
    // "url" and "sha256" are optional and go together: an entry with neither is a message-only
    // update, which tells the user how to update but has nothing to download (a Linux package
    // manager, for instance). Members that this code doesn't know about are ignored, so the
    // manifest can gain fields later without breaking older clients. An entry that is malformed --
    // an unparseable version, a missing required member, or a "url" with no "sha256" -- is
    // silently skipped, so one bad entry can't hide every other update.
    public class CoreUpdater
    {
        // Downloaded update files older than this many days are deleted from the download directory
        // whenever an update check runs. Public so that tests can construct file times relative to it.
        public const int DownloadRetentionDays = 10;

        // Name used for the downloaded file when one can't be worked out from the update's URL.
        private const string fallbackDownloadFileName = "purplepen-update";

        // Extension given to a download that is still in progress or not yet verified. The file is
        // renamed to its real name only once its hash has been checked, so a truncated or corrupt
        // download can never be mistaken for a usable installer.
        private const string partialDownloadExtension = ".partial";

        private readonly IFileDownloader fileDownloader;
        private readonly string downloadDirectory;

        // How the manifest is deserialized. Property names are matched case-insensitively so that
        // the lower-case names in the JSON ("updates", "sha256") bind to the Pascal-cased properties
        // of the DTO classes below.
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        // Create an updater.
        //   fileDownloader: used for every network access this class makes, so that it can be tested
        //     without a network and so the platform layer controls how downloading actually happens.
        //   downloadDirectory: directory that downloaded update files are written to. It doesn't
        //     have to exist yet; it is created when something is first downloaded into it.
        public CoreUpdater(IFileDownloader fileDownloader, string downloadDirectory)
        {
            this.fileDownloader = fileDownloader ?? throw new ArgumentNullException(nameof(fileDownloader));
            this.downloadDirectory = downloadDirectory ?? throw new ArgumentNullException(nameof(downloadDirectory));
        }

        // In the background, downloads the update manifest from the given URL, and checks it to find the latest update > currentVersion for the given platforms and channels.
        // Returns a CoreUpdateStatus object with the result. If multiple match, the best match: first get largest version; if multiple have the same version, get the one
        // with channel that is first in the channels list. If there is still a tie, return the first in the manifest.
        //
        // This also deletes any old downloaded update files in the downloadDirectory that are older than DownloadRetentionDays days.
        //
        //   manifestUrl: URL of the JSON manifest to download.
        //   currentVersion: the version running now, e.g. "1.0.0.0". Only updates strictly newer than this are considered.
        //   platform: the platform to find updates for, e.g. "win-x64". Matched case-insensitively.
        //   channels: channels the user is willing to take updates from, most preferred first, e.g. { "beta", "main" }. Matched case-insensitively.
        //   cancellationToken: cancels the manifest download.
        //
        // Never throws for a problem out in the world -- no network, a 404, a manifest that won't
        // parse -- those all come back as a CoreUpdateStatus with CheckSucceeded false and an
        // English diagnostic in ErrorMessage. Throws OperationCanceledException if cancelled, and
        // ArgumentException/ArgumentNullException for bad arguments, which are caller bugs.
        //
        // Safe to call directly from the UI thread: the work is all done on a background thread, so
        // the caller does not need to wrap it in a Task.Run of their own.
        public async Task<CoreUpdateStatus> CheckForUpdates(string manifestUrl, string currentVersion, string platform, string[] channels, CancellationToken cancellationToken)
        {
            if (manifestUrl == null)
                throw new ArgumentNullException(nameof(manifestUrl));
            if (string.IsNullOrEmpty(platform))
                throw new ArgumentException("Platform must be given.", nameof(platform));
            if (channels == null)
                throw new ArgumentNullException(nameof(channels));
            if (channels.Length == 0)
                throw new ArgumentException("At least one channel must be given.", nameof(channels));

            Version currentParsedVersion;
            if (!Version.TryParse(currentVersion, out currentParsedVersion))
                throw new ArgumentException("Current version is not a valid version number: " + currentVersion, nameof(currentVersion));

            // Everything else happens on a background thread, so that none of it lands on the
            // caller's thread. Awaiting the download doesn't block, but the housekeeping below does
            // synchronous file system work, and the parsing and selection that follow the download
            // would otherwise resume on the calling thread. Doing the lot on the pool is both
            // simpler to reason about than picking out the parts that block, and harder to get
            // wrong later. Nothing is tied up during the download itself: awaiting inside Task.Run
            // releases the pool thread just as it would anywhere else.
            return await Task.Run(async () => {
                // Housekeeping first, so it happens even when the check itself fails. It is
                // best-effort and never affects the result of the check.
                DeleteOldDownloads();

                try {
                    UpdateManifest manifest;

                    // The manifest is small, so it is downloaded into memory rather than to a file.
                    using (MemoryStream manifestStream = new MemoryStream()) {
                        await fileDownloader.DownloadFile(manifestUrl, manifestStream, null, cancellationToken);

                        manifestStream.Position = 0;
                        manifest = await JsonSerializer.DeserializeAsync<UpdateManifest>(manifestStream, jsonOptions, cancellationToken);
                    }

                    // A document that parses but has no "updates" member isn't a manifest at all;
                    // treat it as a failed check rather than as "no update available", so that
                    // pointing the program at the wrong URL doesn't look like being up to date. An
                    // "updates" member that is present but empty is a perfectly good manifest with
                    // nothing in it.
                    if (manifest == null || manifest.Updates == null)
                        throw new FormatException("Update manifest has no \"updates\" section.");

                    UpdateManifestEntry best = SelectBestUpdate(manifest.Updates, currentParsedVersion, platform, channels);

                    return new CoreUpdateStatus {
                        CheckSucceeded = true,
                        AvailableUpdate = (best == null) ? null : MakeAvailableUpdate(best)
                    };
                }
                catch (OperationCanceledException) {
                    // Cancellation isn't a failure of the check, it is the caller changing their
                    // mind, so let it propagate in the normal .NET way rather than reporting it as
                    // an error.
                    throw;
                }
                catch (Exception ex) {
                    return new CoreUpdateStatus {
                        CheckSucceeded = false,
                        AvailableUpdate = null,
                        ErrorMessage = ex.Message
                    };
                }
            });
        }

        // Download an update returned from CheckForUpdates. The update is downloaded to the downloadDirectory given in the constructor.
        // Progress is reported as a value from 0.0 to 1.0, or null if not known.
        //
        //   update: the update to download, as returned in CoreUpdateStatus.AvailableUpdate.
        //   progress: receives download progress; may be null if the caller doesn't want progress.
        //   cancellationToken: cancels the download.
        //
        // The file is downloaded under a temporary name and its SHA256 checked against the manifest
        // before it is given its final name, so the returned path always refers to a complete,
        // verified file. If anything goes wrong -- network failure, a hash that doesn't match, no
        // room on disk -- no partial file is left behind and DownloadSucceeded is false. Throws
        // OperationCanceledException if cancelled, and InvalidOperationException if asked to
        // download an update that has no downloadable file.
        //
        // Safe to call directly from the UI thread: the work is all done on a background thread, so
        // the caller does not need to wrap it in a Task.Run of their own, and a progress dialog
        // driven from the progress argument keeps repainting while the file downloads and is hashed.
        public async Task<DownloadedUpdate> DownloadUpdate(AvailableUpdate update, IProgress<double?> progress, CancellationToken cancellationToken)
        {
            if (update == null)
                throw new ArgumentNullException(nameof(update));
            if (!update.HasDownloadableFile)
                throw new InvalidOperationException("This update has no downloadable file.");

            // Everything else happens on a background thread, as in CheckForUpdates. It matters more
            // here: creating the directory, probing for an unused file name, the writes into the file
            // (the stream isn't opened for asynchronous I/O, so WriteAsync on it completes
            // synchronously), and above all hashing the finished file, which for a large installer is
            // a noticeable amount of disk reading and is exactly what would freeze a progress dialog.
            // Reporting progress from a background thread is safe: Progress<T> marshals its callbacks
            // back to the thread that created it.
            return await Task.Run(async () => {
                string temporaryPath = null;

                try {
                    Directory.CreateDirectory(downloadDirectory);

                    string finalPath = FindUnusedFileName(Path.Combine(downloadDirectory, GetFileNameFromUrl(update.Url)));
                    temporaryPath = finalPath + partialDownloadExtension;

                    using (FileStream destinationStream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
                        await fileDownloader.DownloadFile(update.Url, destinationStream, progress, cancellationToken);
                    }

                    string actualHash = ComputeSha256(temporaryPath);
                    if (!string.Equals(actualHash, update.Sha256, StringComparison.OrdinalIgnoreCase)) {
                        DeleteFileIgnoringErrors(temporaryPath);
                        return new DownloadedUpdate {
                            DownloadSucceeded = false,
                            ErrorMessage = string.Format("Downloaded file failed hash verification: expected SHA256 {0}, got {1}.", update.Sha256, actualHash)
                        };
                    }

                    // Only now, with the contents known to be right, does the file get its real name.
                    File.Move(temporaryPath, finalPath);

                    return new DownloadedUpdate {
                        DownloadSucceeded = true,
                        Path = finalPath
                    };
                }
                catch (OperationCanceledException) {
                    DeleteFileIgnoringErrors(temporaryPath);
                    throw;
                }
                catch (Exception ex) {
                    DeleteFileIgnoringErrors(temporaryPath);
                    return new DownloadedUpdate {
                        DownloadSucceeded = false,
                        ErrorMessage = ex.Message
                    };
                }
            });
        }

        // Pick the best update from the manifest, or null if none of them apply. An entry applies
        // only if it is well formed, is for this platform, is in one of the channels the caller
        // asked for, and is strictly newer than the version running now. Of those that apply, the
        // best is the one with the largest version; ties are broken by preferring the channel that
        // comes first in channels, and any remaining tie by manifest order.
        //   entries: the entries from the manifest, in the order they appear in it.
        //   currentVersion: the version running now.
        //   platform: the platform to find updates for.
        //   channels: acceptable channels, most preferred first.
        private static UpdateManifestEntry SelectBestUpdate(List<UpdateManifestEntry> entries, Version currentVersion, string platform, string[] channels)
        {
            UpdateManifestEntry best = null;
            Version bestVersion = null;
            int bestChannelIndex = 0;

            for (int manifestIndex = 0; manifestIndex < entries.Count; ++manifestIndex) {
                UpdateManifestEntry entry = entries[manifestIndex];

                if (entry == null || !IsWellFormed(entry))
                    continue;

                if (!string.Equals(entry.Platform, platform, StringComparison.OrdinalIgnoreCase))
                    continue;

                int channelIndex = IndexOfChannel(channels, entry.Channel);
                if (channelIndex < 0)
                    continue;

                Version entryVersion;
                if (!Version.TryParse(entry.Version, out entryVersion))
                    continue;

                if (entryVersion <= currentVersion)
                    continue;

                // Because entries are visited in manifest order, and this comparison is strict, an
                // entry only displaces the incumbent when it is genuinely better -- which leaves
                // the earliest entry in the manifest winning any tie that gets this far.
                bool better;
                if (best == null)
                    better = true;
                else if (entryVersion != bestVersion)
                    better = (entryVersion > bestVersion);
                else
                    better = (channelIndex < bestChannelIndex);

                if (better) {
                    best = entry;
                    bestVersion = entryVersion;
                    bestChannelIndex = channelIndex;
                }
            }

            return best;
        }

        // Returns true if a manifest entry has everything it needs to be usable. Entries that fail
        // this are skipped rather than reported, since a manifest is published by us and a bad
        // entry in it is our bug, not something the user can do anything about.
        //   entry: the entry to check.
        private static bool IsWellFormed(UpdateManifestEntry entry)
        {
            if (string.IsNullOrEmpty(entry.Version) || string.IsNullOrEmpty(entry.Platform) || string.IsNullOrEmpty(entry.Channel))
                return false;

            // A download with no hash to check it against would have to be either trusted blindly or
            // thrown away after downloading, so treat it as a broken entry instead.
            if (!string.IsNullOrEmpty(entry.Url) && string.IsNullOrEmpty(entry.Sha256))
                return false;

            return true;
        }

        // Returns the position of channel in channels, or -1 if it isn't there. The position matters
        // as well as the membership, because it is how ties between equal versions are broken.
        //   channels: acceptable channels, most preferred first.
        //   channel: the channel of a manifest entry.
        private static int IndexOfChannel(string[] channels, string channel)
        {
            for (int i = 0; i < channels.Length; ++i) {
                if (string.Equals(channels[i], channel, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        // Convert a manifest entry into the AvailableUpdate handed back to the caller. The URL and
        // hash stay internal: they are needed by DownloadUpdate, but are of no interest to the UI.
        //   entry: a well-formed manifest entry, as chosen by SelectBestUpdate.
        private static AvailableUpdate MakeAvailableUpdate(UpdateManifestEntry entry)
        {
            return new AvailableUpdate {
                Title = entry.Title,
                Version = entry.Version,
                Platform = entry.Platform,
                Channel = entry.Channel,
                ReleaseMessage = entry.Message,
                HasDownloadableFile = !string.IsNullOrEmpty(entry.Url),
                Url = entry.Url,
                Sha256 = entry.Sha256
            };
        }

        // Delete update files in the download directory that were downloaded a long time ago, so
        // that installers the user never ran don't accumulate forever. Best effort: a file that
        // can't be deleted (it may be running, or the directory may not be ours to write to) is
        // simply left alone, and no failure here is ever reported to the caller.
        private void DeleteOldDownloads()
        {
            try {
                if (!Directory.Exists(downloadDirectory))
                    return;

                DateTime deleteBefore = DateTime.UtcNow - TimeSpan.FromDays(DownloadRetentionDays);

                foreach (string file in Directory.GetFiles(downloadDirectory)) {
                    try {
                        if (File.GetLastWriteTimeUtc(file) < deleteBefore)
                            File.Delete(file);
                    }
                    catch (Exception) {
                        // Leave this one and carry on with the rest.
                    }
                }
            }
            catch (Exception) {
                // Nothing here is important enough to fail an update check over.
            }
        }

        // Work out what to call the downloaded file, from the last segment of the update's URL --
        // so "https://example.com/downloads/PurplePen_1.1.exe" downloads as "PurplePen_1.1.exe".
        // Falls back to a fixed name if the URL has nothing usable in it, and strips any characters
        // that aren't legal in a file name, since the URL comes from off the machine.
        //   url: the URL the update will be downloaded from.
        private static string GetFileNameFromUrl(string url)
        {
            string candidate = url;

            // Drop any query string or fragment; they are not part of the file name.
            int cut = candidate.IndexOfAny(new char[] { '?', '#' });
            if (cut >= 0)
                candidate = candidate.Substring(0, cut);

            int lastSlash = candidate.LastIndexOfAny(new char[] { '/', '\\' });
            if (lastSlash >= 0)
                candidate = candidate.Substring(lastSlash + 1);

            try {
                candidate = Uri.UnescapeDataString(candidate);
            }
            catch (Exception) {
                // A badly escaped URL just keeps its raw text.
            }

            StringBuilder cleaned = new StringBuilder();
            foreach (char c in candidate) {
                if (Array.IndexOf(Path.GetInvalidFileNameChars(), c) < 0)
                    cleaned.Append(c);
            }

            string fileName = cleaned.ToString().Trim();
            if (fileName.Length == 0 || fileName == "." || fileName == "..")
                fileName = fallbackDownloadFileName;

            return fileName;
        }

        // Find a file name that isn't in use, by appending "(1)", "(2)", and so on before the
        // extension. Also avoids a name whose ".partial" counterpart exists, so two downloads
        // running at once can't collide on their temporary files.
        //   path: the preferred full path.
        private static string FindUnusedFileName(string path)
        {
            if (!File.Exists(path) && !File.Exists(path + partialDownloadExtension))
                return path;

            string directory = Path.GetDirectoryName(path);
            string withoutExtension = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);

            for (int i = 1; ; ++i) {
                string candidate = Path.Combine(directory, withoutExtension + "(" + i.ToString() + ")" + extension);
                if (!File.Exists(candidate) && !File.Exists(candidate + partialDownloadExtension))
                    return candidate;
            }
        }

        // Returns the SHA256 of a file as a lowercase hex string, the same form the manifest uses.
        //   path: full path of the file to hash.
        private static string ComputeSha256(string path)
        {
            byte[] hash;

            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                hash = SHA256.HashData(stream);
            }

            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        // Delete a file, ignoring any problem doing so, and doing nothing if path is null. Used on
        // the failure paths of DownloadUpdate, where the reason for the failure is what should be
        // reported, not whatever goes wrong while cleaning up after it.
        //   path: full path of the file to delete; may be null.
        private static void DeleteFileIgnoringErrors(string path)
        {
            if (path == null)
                return;

            try {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception) {
                // Nothing useful to do about it.
            }
        }

        // IMPORTANT: the manifest format is defined in two places. These two classes are the reading
        // side; the writing side is ManifestFile.cs in the UpdateManifest tool (src\Tools\
        // UpdateManifest), which has its own copy of them. The copy exists because that tool is
        // standalone -- it isn't in PPen.slnx and doesn't reference PurplePenCore.
        //
        // Adding a field to the manifest therefore means three changes, not one:
        //   1. Add the property here, and use it wherever the entry is turned into an
        //      AvailableUpdate (see MakeAvailableUpdate) or checked (see IsWellFormed).
        //   2. Add the matching property to UpdateManifestEntry in the tool's ManifestFile.cs.
        //   3. Add a command-line option to the tool so the field can actually be set.
        // Skipping (2) and (3) leaves a field nothing can write; skipping (1) leaves one Purple Pen
        // silently ignores.

        // The manifest document, as deserialized from JSON. Unknown members in the JSON are ignored.
        private class UpdateManifest
        {
            public List<UpdateManifestEntry> Updates { get; set; }
        }

        // One update in the manifest, as deserialized from JSON. Everything is a string, and every
        // member can be missing, because the file is written by hand and validated in code rather
        // than by the deserializer.
        private class UpdateManifestEntry
        {
            public string Title { get; set; }
            public string Version { get; set; }
            public string Platform { get; set; }
            public string Channel { get; set; }
            public string Url { get; set; }
            public string Sha256 { get; set; }
            public string Message { get; set; }
        }
    }

    // Interface to encapsulate the downloading of files, so that we can test the CoreUpdater without actually downloading files,
    // and customize the downloading. May throw exceptions if the download fails, or if the cancellationToken is cancelled.
    public interface IFileDownloader
    {
        // Download the contents of url and write them to destinationStream.
        //   url: what to download.
        //   destinationStream: where to write it. The implementation writes from the current position and does not close the stream.
        //   progress: receives values from 0.0 to 1.0 as the download proceeds, or null values if the
        //     total size isn't known in advance. May itself be null, meaning the caller doesn't want progress.
        //   cancellationToken: cancels the download, by throwing OperationCanceledException.
        Task DownloadFile(string url, Stream destinationStream, IProgress<double?> progress, CancellationToken cancellationToken);
    }

    // Represents the result of checking for updates. This is returned from CheckForUpdates.
    public class CoreUpdateStatus
    {
        public bool CheckSucceeded; // True if the check for updates succeeded, even if no update is available.
        public AvailableUpdate AvailableUpdate; // Null if no update is available.
        public string ErrorMessage; // If CheckSucceeded is false, this contains the error message. In English, for logging; the UI supplies its own wording.
    }

    // Represents an update that is available for download. This is returned from CheckForUpdates.
    public class AvailableUpdate
    {
        public string Title;
        public string Version;
        public string Platform;
        public string Channel;
        public string ReleaseMessage;
        public bool HasDownloadableFile;  // Some updates only have a release message, and no downloadable file.

        // Private state for the URL, hash, etc of the update. Internal rather than private so that
        // the tests can check them; nothing outside this assembly needs them, because the only
        // thing to do with an AvailableUpdate is hand it back to DownloadUpdate.
        internal string Url;      // Where to download the update from. Null if HasDownloadableFile is false.
        internal string Sha256;   // Expected SHA256 of the download, as lowercase hex. Null if HasDownloadableFile is false.
    }

    // Represents the result of downloading an update. This is returned from DownloadUpdate.
    public class DownloadedUpdate
    {
        public bool DownloadSucceeded; // True if the download and verification succeeded.
        public string Path;  // Path to the downloaded file.
        public string ErrorMessage; // If DownloadSucceeded is false, this contains the error message. In English, for logging; the UI supplies its own wording.
    }

}
