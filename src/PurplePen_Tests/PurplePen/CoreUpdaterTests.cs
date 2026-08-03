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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.Tests
{
    // Tests for CoreUpdater: choosing the right update out of a manifest, downloading and verifying
    // it, and cleaning up old downloads. Everything runs against DummyFileDownloader below, so no
    // test here touches the network. The manifests and payload files live in TestFiles\updater.
    [TestClass]
    public class CoreUpdaterTests
    {
        // URL the tests use for the main manifest. Nothing is ever fetched from it; it is just a key
        // into the dummy downloader's table of canned content.
        private const string manifestUrl = "https://example.com/updates/manifest.json";

        // Platforms and channels appearing in TestFiles\updater\manifest.json.
        private const string windowsPlatform = "win-x64";
        private const string macPlatform = "osx-arm64";
        private const string linuxPlatform = "linux-x64";
        private static readonly string[] mainOnly = { "main" };
        private static readonly string[] betaOnly = { "beta" };
        private static readonly string[] mainThenBeta = { "main", "beta" };
        private static readonly string[] betaThenMain = { "beta", "main" };

        // Set by MSTest; used to give each test its own download directory.
        public TestContext TestContext { get; set; }

        // Directory this test downloads into. Every test gets its own, named after the test, because
        // the test assembly runs test methods in parallel and they would otherwise be deleting each
        // other's files. Created before the test and removed afterwards, so nothing is left behind
        // in the source tree.
        private string downloadDirectory;

        // Contents of TestFiles\updater\update1.dat and update2.dat, whose SHA256 hashes are the
        // ones recorded in the test manifests.
        private byte[] payload1;
        private byte[] payload2;

        // Create an empty download directory and read the payload files.
        [TestInitialize]
        public void Initialize()
        {
            downloadDirectory = TestUtil.GetTestFile("updater\\temp_" + TestContext.TestName);
            DeleteDownloadDirectory();
            Directory.CreateDirectory(downloadDirectory);

            payload1 = File.ReadAllBytes(TestUtil.GetTestFile("updater\\update1.dat"));
            payload2 = File.ReadAllBytes(TestUtil.GetTestFile("updater\\update2.dat"));
        }

        // Remove the download directory, so no test files are left in the source tree.
        [TestCleanup]
        public void Cleanup()
        {
            DeleteDownloadDirectory();
        }

        // Delete the download directory and everything in it, ignoring the case where it isn't there.
        private void DeleteDownloadDirectory()
        {
            if (Directory.Exists(downloadDirectory))
                Directory.Delete(downloadDirectory, true);
        }

        // Create a downloader that serves the main test manifest, plus every payload URL that
        // appears in it. Tests that want something different add to or replace the entries.
        private DummyFileDownloader CreateDownloader()
        {
            DummyFileDownloader downloader = new DummyFileDownloader();

            downloader.AddFile(manifestUrl, TestUtil.GetTestFile("updater\\manifest.json"));

            // update1.dat is the content whose hash the 1.0/1.1/1.2 main-channel entries record;
            // update2.dat is the content the beta, macOS and dev entries record.
            downloader.SetFile("https://example.com/downloads/PurplePen_1.0.exe", payload1);
            downloader.SetFile("https://example.com/downloads/PurplePen_1.1.exe", payload1);
            downloader.SetFile("https://example.com/downloads/PurplePen_1.2.exe", payload1);
            downloader.SetFile("https://example.com/downloads/PurplePen_1.2_duplicate.exe", payload1);
            downloader.SetFile("https://example.com/downloads/PurplePen_1.2_beta1.exe", payload2);
            downloader.SetFile("https://example.com/downloads/PurplePen_1.3_beta1.dmg", payload2);
            downloader.SetFile("https://example.com/downloads/PurplePen_1.4_dev.exe?token=abc123", payload2);

            return downloader;
        }

        // Create an updater using the given downloader and the per-test download directory.
        private CoreUpdater CreateUpdater(DummyFileDownloader downloader)
        {
            return new CoreUpdater(downloader, downloadDirectory);
        }

        // Run a check against the main test manifest and assert that it succeeded, returning the
        // update that was found (which may be null if there wasn't one).
        //   downloader: the downloader to use.
        //   currentVersion: the version pretending to be running.
        //   platform: platform to check for.
        //   channels: acceptable channels, most preferred first.
        private async Task<AvailableUpdate> CheckSucceeding(DummyFileDownloader downloader, string currentVersion, string platform, string[] channels)
        {
            CoreUpdateStatus status = await CreateUpdater(downloader).CheckForUpdates(manifestUrl, currentVersion, platform, channels, CancellationToken.None);

            Assert.IsTrue(status.CheckSucceeded, "check should have succeeded, but failed with: " + status.ErrorMessage);
            Assert.IsNull(status.ErrorMessage);
            return status.AvailableUpdate;
        }

        // ---------- Choosing an update ----------

        [TestMethod]
        public async Task FindsNewestUpdateForPlatformAndChannel()
        {
            AvailableUpdate update = await CheckSucceeding(CreateDownloader(), "1.0.0.0", windowsPlatform, mainOnly);

            // 1.2.0.0 main, not the older 1.1.0.0, and not the 1.2.0.0 beta.
            Assert.IsNotNull(update);
            Assert.AreEqual("Purple Pen 1.2", update.Title);
            Assert.AreEqual("1.2.0.0", update.Version);
            Assert.AreEqual("Stable release with the 1.2 features.", update.ReleaseMessage);
            Assert.IsTrue(update.HasDownloadableFile);

            // Platform and channel come back exactly as spelled in the manifest, which for this
            // entry is upper case -- matching is case-insensitive, but nothing is rewritten.
            Assert.AreEqual("WIN-X64", update.Platform);
            Assert.AreEqual("MAIN", update.Channel);

            // The URL and hash are carried along for DownloadUpdate to use.
            Assert.AreEqual("https://example.com/downloads/PurplePen_1.2.exe", update.Url);
            Assert.AreEqual("EFC41EAB1F9F266B4C634E0665B089C8458FA88D067422149D6CA8EE9AF07D78", update.Sha256);
        }

        [TestMethod]
        public async Task FetchesTheManifestFromTheGivenUrl()
        {
            DummyFileDownloader downloader = CreateDownloader();
            await CheckSucceeding(downloader, "1.0.0.0", windowsPlatform, mainOnly);

            // Checking for updates downloads the manifest and nothing else.
            CollectionAssert.AreEqual(new string[] { manifestUrl }, downloader.RequestedUrls);
        }

        [TestMethod]
        public async Task NoUpdateWhenCurrentVersionIsTheNewest()
        {
            // Exactly the newest version available: an update must be strictly newer to count.
            AvailableUpdate update = await CheckSucceeding(CreateDownloader(), "1.2.0.0", windowsPlatform, mainOnly);

            Assert.IsNull(update);
        }

        [TestMethod]
        public async Task NoUpdateWhenCurrentVersionIsNewerThanTheManifest()
        {
            AvailableUpdate update = await CheckSucceeding(CreateDownloader(), "2.0.0.0", windowsPlatform, mainThenBeta);

            Assert.IsNull(update);
        }

        [TestMethod]
        public async Task ChannelOrderBreaksVersionTie()
        {
            // The manifest has 1.2.0.0 in both main and beta for win-x64. Whichever channel the
            // caller lists first wins, regardless of the order the two appear in the manifest
            // (beta comes first there).
            AvailableUpdate mainPreferred = await CheckSucceeding(CreateDownloader(), "1.0.0.0", windowsPlatform, mainThenBeta);
            Assert.AreEqual("Purple Pen 1.2", mainPreferred.Title);

            AvailableUpdate betaPreferred = await CheckSucceeding(CreateDownloader(), "1.0.0.0", windowsPlatform, betaThenMain);
            Assert.AreEqual("Purple Pen 1.2 beta 1", betaPreferred.Title);
        }

        [TestMethod]
        public async Task VersionBeatsChannelPreference()
        {
            // "dev" is listed first, but the dev entry is 1.4.0.0 and the main one is 1.2.0.0, so
            // the higher version wins -- channel order only breaks ties between equal versions.
            AvailableUpdate update = await CheckSucceeding(CreateDownloader(), "1.0.0.0", windowsPlatform, new string[] { "main", "dev" });

            Assert.AreEqual("Purple Pen 1.4 dev build", update.Title);
            Assert.AreEqual("1.4.0.0", update.Version);
        }

        [TestMethod]
        public async Task ManifestOrderBreaksRemainingTie()
        {
            // Two entries with the same version, platform and channel: the one earlier in the
            // manifest wins.
            AvailableUpdate update = await CheckSucceeding(CreateDownloader(), "1.0.0.0", windowsPlatform, mainOnly);

            Assert.AreEqual("Purple Pen 1.2", update.Title);
            Assert.AreNotEqual("Purple Pen 1.2 (duplicate entry)", update.Title);
        }

        [TestMethod]
        public async Task IgnoresOtherPlatforms()
        {
            // The macOS entry is version 1.3.0.0, higher than anything for Windows, but it must not
            // be offered to a Windows user.
            AvailableUpdate windows = await CheckSucceeding(CreateDownloader(), "1.0.0.0", windowsPlatform, betaThenMain);
            Assert.AreEqual("Purple Pen 1.2 beta 1", windows.Title);

            AvailableUpdate mac = await CheckSucceeding(CreateDownloader(), "1.0.0.0", macPlatform, betaThenMain);
            Assert.AreEqual("Purple Pen 1.3 beta 1", mac.Title);
        }

        [TestMethod]
        public async Task NoUpdateForUnknownPlatform()
        {
            AvailableUpdate update = await CheckSucceeding(CreateDownloader(), "1.0.0.0", "haiku-m68k", mainThenBeta);

            Assert.IsNull(update);
        }

        [TestMethod]
        public async Task IgnoresChannelsNotAskedFor()
        {
            // Asking only for main must not turn up the 1.2.0.0 beta or the 1.4.0.0 dev build.
            AvailableUpdate update = await CheckSucceeding(CreateDownloader(), "1.0.0.0", windowsPlatform, mainOnly);
            Assert.AreEqual("Purple Pen 1.2", update.Title);

            // Asking only for beta must not turn up the main-channel entries.
            AvailableUpdate beta = await CheckSucceeding(CreateDownloader(), "1.0.0.0", windowsPlatform, betaOnly);
            Assert.AreEqual("Purple Pen 1.2 beta 1", beta.Title);
        }

        [TestMethod]
        public async Task PlatformAndChannelMatchIgnoringCase()
        {
            AvailableUpdate update = await CheckSucceeding(CreateDownloader(), "1.0.0.0", "WiN-x64", new string[] { "MaIn" });

            Assert.AreEqual("Purple Pen 1.2", update.Title);
        }

        [TestMethod]
        public async Task MessageOnlyUpdateHasNoDownloadableFile()
        {
            // The Linux entry tells the user to use their package manager; there is nothing to fetch.
            AvailableUpdate update = await CheckSucceeding(CreateDownloader(), "1.0.0.0", linuxPlatform, mainOnly);

            Assert.IsNotNull(update);
            Assert.AreEqual("Purple Pen 1.2 for Linux", update.Title);
            Assert.AreEqual("Install by doing apt-get update purple-pen.", update.ReleaseMessage);
            Assert.IsFalse(update.HasDownloadableFile);
            Assert.IsNull(update.Url);
            Assert.IsNull(update.Sha256);
        }

        [TestMethod]
        public async Task SkipsMalformedEntries()
        {
            // The manifest holds three deliberately broken win-x64/main entries: an unparseable
            // version, a 1.9.0.0 entry with a url but no sha256, and a 1.7.0.0 entry with no
            // platform. All are higher than 1.2.0.0, so if any were used it would be chosen.
            AvailableUpdate update = await CheckSucceeding(CreateDownloader(), "1.0.0.0", windowsPlatform, mainOnly);

            Assert.AreEqual("Purple Pen 1.2", update.Title);
        }

        [TestMethod]
        public async Task IgnoresUnknownMembersInTheManifest()
        {
            // The dev entry carries a "buildNumber" and an "unknownMember" object that this version
            // of the code knows nothing about. It must still be usable.
            AvailableUpdate update = await CheckSucceeding(CreateDownloader(), "1.0.0.0", windowsPlatform, new string[] { "dev" });

            Assert.AreEqual("Purple Pen 1.4 dev build", update.Title);
        }

        // ---------- Failures while checking ----------

        [TestMethod]
        public async Task CheckFailsWhenTheManifestCannotBeDownloaded()
        {
            DummyFileDownloader downloader = CreateDownloader();
            downloader.ExceptionToThrow = new IOException("The network cable is unplugged.");

            CoreUpdateStatus status = await CreateUpdater(downloader).CheckForUpdates(manifestUrl, "1.0.0.0", windowsPlatform, mainOnly, CancellationToken.None);

            Assert.IsFalse(status.CheckSucceeded);
            Assert.IsNull(status.AvailableUpdate);
            Assert.AreEqual("The network cable is unplugged.", status.ErrorMessage);
        }

        [TestMethod]
        public async Task CheckFailsWhenTheManifestUrlIsNotFound()
        {
            // A URL the downloader has no content for stands in for a 404.
            DummyFileDownloader downloader = CreateDownloader();

            CoreUpdateStatus status = await CreateUpdater(downloader).CheckForUpdates("https://example.com/updates/nosuchfile.json", "1.0.0.0", windowsPlatform, mainOnly, CancellationToken.None);

            Assert.IsFalse(status.CheckSucceeded);
            Assert.IsNull(status.AvailableUpdate);
            Assert.IsFalse(string.IsNullOrEmpty(status.ErrorMessage));
        }

        [TestMethod]
        public async Task CheckFailsOnUnparseableJson()
        {
            DummyFileDownloader downloader = CreateDownloader();
            downloader.AddFile(manifestUrl, TestUtil.GetTestFile("updater\\badjson.json"));

            CoreUpdateStatus status = await CreateUpdater(downloader).CheckForUpdates(manifestUrl, "1.0.0.0", windowsPlatform, mainOnly, CancellationToken.None);

            Assert.IsFalse(status.CheckSucceeded);
            Assert.IsNull(status.AvailableUpdate);
            Assert.IsFalse(string.IsNullOrEmpty(status.ErrorMessage));
        }

        [TestMethod]
        public async Task CheckFailsWhenThereIsNoUpdatesSection()
        {
            // Valid JSON, but not a manifest -- most likely the wrong URL. Reporting this as
            // "no update available" would hide the mistake, so it is a failure instead.
            DummyFileDownloader downloader = CreateDownloader();
            downloader.AddFile(manifestUrl, TestUtil.GetTestFile("updater\\noupdates.json"));

            CoreUpdateStatus status = await CreateUpdater(downloader).CheckForUpdates(manifestUrl, "1.0.0.0", windowsPlatform, mainOnly, CancellationToken.None);

            Assert.IsFalse(status.CheckSucceeded);
            Assert.IsNull(status.AvailableUpdate);
            Assert.IsFalse(string.IsNullOrEmpty(status.ErrorMessage));
        }

        [TestMethod]
        public async Task EmptyUpdatesSectionSucceedsWithNoUpdate()
        {
            DummyFileDownloader downloader = CreateDownloader();
            downloader.AddFile(manifestUrl, TestUtil.GetTestFile("updater\\emptyupdates.json"));

            AvailableUpdate update = await CheckSucceeding(downloader, "1.0.0.0", windowsPlatform, mainOnly);

            Assert.IsNull(update);
        }

        [TestMethod]
        public async Task CheckThrowsWhenCancelled()
        {
            DummyFileDownloader downloader = CreateDownloader();
            CancellationTokenSource cancellation = new CancellationTokenSource();
            downloader.BeforeEachChunk = () => cancellation.Cancel();

            // Cancellation isn't an error, so it comes back the normal .NET way rather than as a
            // failed CoreUpdateStatus.
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await CreateUpdater(downloader).CheckForUpdates(manifestUrl, "1.0.0.0", windowsPlatform, mainOnly, cancellation.Token));
        }

        [TestMethod]
        public async Task CheckThrowsOnBadArguments()
        {
            CoreUpdater updater = CreateUpdater(CreateDownloader());

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await updater.CheckForUpdates(null, "1.0.0.0", windowsPlatform, mainOnly, CancellationToken.None));

            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await updater.CheckForUpdates(manifestUrl, "1.0.0.0", "", mainOnly, CancellationToken.None));

            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await updater.CheckForUpdates(manifestUrl, "1.0.0.0", windowsPlatform, new string[0], CancellationToken.None));

            // A current version that doesn't parse is a bug in the calling program, not something
            // the user or the server did, so it throws rather than reporting a failed check.
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await updater.CheckForUpdates(manifestUrl, "version one", windowsPlatform, mainOnly, CancellationToken.None));
        }

        // ---------- Cleaning up old downloads ----------

        [TestMethod]
        public async Task CheckDeletesOldDownloads()
        {
            string tooOld = CreateFileWithAge("PurplePen_0.8.exe", CoreUpdater.DownloadRetentionDays + 10);
            string justTooOld = CreateFileWithAge("PurplePen_0.9.exe", CoreUpdater.DownloadRetentionDays + 1);
            string recent = CreateFileWithAge("PurplePen_1.0.exe", 2);

            await CheckSucceeding(CreateDownloader(), "1.0.0.0", windowsPlatform, mainOnly);

            Assert.IsFalse(File.Exists(tooOld), "a download older than the retention period should be deleted");
            Assert.IsFalse(File.Exists(justTooOld), "a download older than the retention period should be deleted");
            Assert.IsTrue(File.Exists(recent), "a recent download should be kept");
        }

        [TestMethod]
        public async Task CheckSucceedsWhenTheDownloadDirectoryDoesNotExist()
        {
            // Nothing has been downloaded yet, so there is no directory to clean up. That must not
            // upset the check.
            DeleteDownloadDirectory();

            AvailableUpdate update = await CheckSucceeding(CreateDownloader(), "1.0.0.0", windowsPlatform, mainOnly);

            Assert.IsNotNull(update);
        }

        [TestMethod]
        public async Task CleanupHappensEvenWhenTheCheckFails()
        {
            string tooOld = CreateFileWithAge("PurplePen_0.8.exe", CoreUpdater.DownloadRetentionDays + 10);

            DummyFileDownloader downloader = CreateDownloader();
            downloader.ExceptionToThrow = new IOException("No network.");
            CoreUpdateStatus status = await CreateUpdater(downloader).CheckForUpdates(manifestUrl, "1.0.0.0", windowsPlatform, mainOnly, CancellationToken.None);

            Assert.IsFalse(status.CheckSucceeded);
            Assert.IsFalse(File.Exists(tooOld));
        }

        // Write a file into the download directory and backdate it, so that the retention rule can
        // be tested without waiting.
        //   name: file name within the download directory.
        //   ageInDays: how long ago the file should look as though it was written.
        private string CreateFileWithAge(string name, int ageInDays)
        {
            string path = Path.Combine(downloadDirectory, name);
            File.WriteAllText(path, "not a real installer");
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow - TimeSpan.FromDays(ageInDays));
            return path;
        }

        // ---------- Downloading ----------

        [TestMethod]
        public async Task DownloadsAndVerifiesUpdate()
        {
            DummyFileDownloader downloader = CreateDownloader();
            AvailableUpdate update = await CheckSucceeding(downloader, "1.0.0.0", windowsPlatform, mainOnly);

            DownloadedUpdate downloaded = await CreateUpdater(downloader).DownloadUpdate(update, null, CancellationToken.None);

            Assert.IsTrue(downloaded.DownloadSucceeded, downloaded.ErrorMessage);
            Assert.IsNull(downloaded.ErrorMessage);

            // The name comes from the last segment of the URL, and the file lands in the download
            // directory with exactly the bytes that were served.
            Assert.AreEqual(Path.Combine(downloadDirectory, "PurplePen_1.2.exe"), downloaded.Path);
            CollectionAssert.AreEqual(payload1, File.ReadAllBytes(downloaded.Path));

            // Nothing else, in particular no leftover ".partial" file.
            Assert.AreEqual(1, Directory.GetFiles(downloadDirectory).Length);
        }

        [TestMethod]
        public async Task DownloadStripsQueryStringFromFileName()
        {
            DummyFileDownloader downloader = CreateDownloader();
            AvailableUpdate update = await CheckSucceeding(downloader, "1.0.0.0", windowsPlatform, new string[] { "dev" });

            DownloadedUpdate downloaded = await CreateUpdater(downloader).DownloadUpdate(update, null, CancellationToken.None);

            Assert.IsTrue(downloaded.DownloadSucceeded, downloaded.ErrorMessage);
            Assert.AreEqual(Path.Combine(downloadDirectory, "PurplePen_1.4_dev.exe"), downloaded.Path);
            CollectionAssert.AreEqual(payload2, File.ReadAllBytes(downloaded.Path));
        }

        [TestMethod]
        public async Task DownloadReportsProgress()
        {
            DummyFileDownloader downloader = CreateDownloader();
            downloader.ChunkSize = 32;
            AvailableUpdate update = await CheckSucceeding(downloader, "1.0.0.0", windowsPlatform, mainOnly);

            List<double?> reported = new List<double?>();
            Progress progress = new Progress(reported);

            DownloadedUpdate downloaded = await CreateUpdater(downloader).DownloadUpdate(update, progress, CancellationToken.None);

            Assert.IsTrue(downloaded.DownloadSucceeded, downloaded.ErrorMessage);
            Assert.IsTrue(reported.Count > 1, "progress should be reported more than once for a multi-chunk download");

            double previous = 0.0;
            foreach (double? value in reported) {
                Assert.IsTrue(value.HasValue);
                Assert.IsTrue(value.Value >= 0.0 && value.Value <= 1.0, "progress should be between 0 and 1, was " + value.Value);
                Assert.IsTrue(value.Value >= previous, "progress should never go backwards");
                previous = value.Value;
            }

            Assert.AreEqual(1.0, reported[reported.Count - 1].Value, 0.0001, "progress should finish at 1.0");
        }

        [TestMethod]
        public async Task DownloadToleratesUnknownProgress()
        {
            // A server that doesn't send a content length means the fraction done can't be worked
            // out, and the downloader reports null. That must not stop the download working.
            DummyFileDownloader downloader = CreateDownloader();
            downloader.ChunkSize = 32;
            downloader.ReportUnknownProgress = true;
            AvailableUpdate update = await CheckSucceeding(downloader, "1.0.0.0", windowsPlatform, mainOnly);

            List<double?> reported = new List<double?>();
            DownloadedUpdate downloaded = await CreateUpdater(downloader).DownloadUpdate(update, new Progress(reported), CancellationToken.None);

            Assert.IsTrue(downloaded.DownloadSucceeded, downloaded.ErrorMessage);
            Assert.IsTrue(reported.Count > 0);
            foreach (double? value in reported)
                Assert.IsFalse(value.HasValue);
        }

        [TestMethod]
        public async Task DownloadFailsOnHashMismatch()
        {
            // Serve the wrong content for the 1.2 installer. This is what a corrupted download or a
            // tampered-with server would look like.
            DummyFileDownloader downloader = CreateDownloader();
            downloader.SetFile("https://example.com/downloads/PurplePen_1.2.exe", payload2);
            AvailableUpdate update = await CheckSucceeding(downloader, "1.0.0.0", windowsPlatform, mainOnly);

            DownloadedUpdate downloaded = await CreateUpdater(downloader).DownloadUpdate(update, null, CancellationToken.None);

            Assert.IsFalse(downloaded.DownloadSucceeded);
            Assert.IsNull(downloaded.Path);
            Assert.IsFalse(string.IsNullOrEmpty(downloaded.ErrorMessage));

            // A file that failed verification must not be left where it could be run.
            AssertDownloadDirectoryIsEmpty();
        }

        [TestMethod]
        public async Task DownloadFailsWhenTheDownloadThrows()
        {
            DummyFileDownloader downloader = CreateDownloader();
            AvailableUpdate update = await CheckSucceeding(downloader, "1.0.0.0", windowsPlatform, mainOnly);

            downloader.ExceptionToThrow = new IOException("Connection reset.");
            DownloadedUpdate downloaded = await CreateUpdater(downloader).DownloadUpdate(update, null, CancellationToken.None);

            Assert.IsFalse(downloaded.DownloadSucceeded);
            Assert.IsNull(downloaded.Path);
            Assert.AreEqual("Connection reset.", downloaded.ErrorMessage);
            AssertDownloadDirectoryIsEmpty();
        }

        [TestMethod]
        public async Task DownloadThrowsWhenCancelled()
        {
            DummyFileDownloader downloader = CreateDownloader();
            AvailableUpdate update = await CheckSucceeding(downloader, "1.0.0.0", windowsPlatform, mainOnly);

            CancellationTokenSource cancellation = new CancellationTokenSource();
            downloader.ChunkSize = 32;
            downloader.BeforeEachChunk = () => cancellation.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await CreateUpdater(downloader).DownloadUpdate(update, null, cancellation.Token));

            AssertDownloadDirectoryIsEmpty();
        }

        [TestMethod]
        public async Task DownloadUsesANewNameWhenTheFileAlreadyExists()
        {
            DummyFileDownloader downloader = CreateDownloader();
            AvailableUpdate update = await CheckSucceeding(downloader, "1.0.0.0", windowsPlatform, mainOnly);

            string existing = Path.Combine(downloadDirectory, "PurplePen_1.2.exe");
            File.WriteAllText(existing, "downloaded earlier");

            DownloadedUpdate downloaded = await CreateUpdater(downloader).DownloadUpdate(update, null, CancellationToken.None);

            Assert.IsTrue(downloaded.DownloadSucceeded, downloaded.ErrorMessage);
            Assert.AreEqual(Path.Combine(downloadDirectory, "PurplePen_1.2(1).exe"), downloaded.Path);
            CollectionAssert.AreEqual(payload1, File.ReadAllBytes(downloaded.Path));

            // The file that was already there is untouched.
            Assert.AreEqual("downloaded earlier", File.ReadAllText(existing));

            // And a third download gets yet another name.
            DownloadedUpdate again = await CreateUpdater(downloader).DownloadUpdate(update, null, CancellationToken.None);
            Assert.IsTrue(again.DownloadSucceeded, again.ErrorMessage);
            Assert.AreEqual(Path.Combine(downloadDirectory, "PurplePen_1.2(2).exe"), again.Path);
        }

        [TestMethod]
        public async Task DownloadCreatesTheDownloadDirectory()
        {
            DummyFileDownloader downloader = CreateDownloader();
            AvailableUpdate update = await CheckSucceeding(downloader, "1.0.0.0", windowsPlatform, mainOnly);

            DeleteDownloadDirectory();
            Assert.IsFalse(Directory.Exists(downloadDirectory));

            DownloadedUpdate downloaded = await CreateUpdater(downloader).DownloadUpdate(update, null, CancellationToken.None);

            Assert.IsTrue(downloaded.DownloadSucceeded, downloaded.ErrorMessage);
            Assert.IsTrue(File.Exists(downloaded.Path));
        }

        [TestMethod]
        public async Task DownloadThrowsForAMessageOnlyUpdate()
        {
            DummyFileDownloader downloader = CreateDownloader();
            AvailableUpdate update = await CheckSucceeding(downloader, "1.0.0.0", linuxPlatform, mainOnly);
            Assert.IsFalse(update.HasDownloadableFile);

            // Asking to download an update that has nothing to download is a bug in the caller.
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await CreateUpdater(downloader).DownloadUpdate(update, null, CancellationToken.None));
        }

        [TestMethod]
        public async Task DownloadThrowsForNullUpdate()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await CreateUpdater(CreateDownloader()).DownloadUpdate(null, null, CancellationToken.None));
        }

        // Assert that nothing at all was left in the download directory, which is how every failed
        // download should leave it.
        private void AssertDownloadDirectoryIsEmpty()
        {
            string[] files = Directory.GetFiles(downloadDirectory);
            Assert.AreEqual(0, files.Length, "no file should be left behind, but found: " + string.Join(", ", files));
        }

        // Collects the progress values reported during a download. Progress<T> is deliberately not
        // used: it posts to the synchronization context, so the reports would arrive after the test
        // had already looked at the list.
        private class Progress: IProgress<double?>
        {
            private readonly List<double?> reported;

            // Create a progress sink that appends every reported value to the given list.
            //   reported: list to append to.
            public Progress(List<double?> reported)
            {
                this.reported = reported;
            }

            // Record one reported value.
            //   value: fraction of the download that is complete, or null if not known.
            public void Report(double? value)
            {
                reported.Add(value);
            }
        }

        // An IFileDownloader that serves canned content out of memory instead of using the network,
        // so the updater's logic can be tested without a server and without any timing dependency.
        // Content is written in chunks with a progress report after each one, and cancellation is
        // checked between chunks, so partial downloads and cancellation can be exercised too.
        private class DummyFileDownloader: IFileDownloader
        {
            // What each URL serves. A URL that isn't here throws when downloaded, which is how a 404
            // is simulated.
            private readonly Dictionary<string, byte[]> files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

            // Every URL that has been asked for, in order.
            public readonly List<string> RequestedUrls = new List<string>();

            // If set, thrown instead of serving anything -- a network failure.
            public Exception ExceptionToThrow;

            // How many bytes are written between progress reports.
            public int ChunkSize = 1024;

            // If true, progress is reported as null (size not known) rather than as a fraction.
            public bool ReportUnknownProgress;

            // Called before each chunk is written. A test uses this to cancel partway through.
            public Action BeforeEachChunk;

            // Make url serve the given bytes, replacing anything already there.
            //   url: the URL to serve.
            //   content: what it should return.
            public void SetFile(string url, byte[] content)
            {
                files[url] = content;
            }

            // Make url serve the contents of a file on disk.
            //   url: the URL to serve.
            //   path: full path of the file whose contents it should return.
            public void AddFile(string url, string path)
            {
                files[url] = File.ReadAllBytes(path);
            }

            // Serve url into destinationStream, reporting progress and honouring cancellation.
            //   url: what to download.
            //   destinationStream: where to write it.
            //   progress: receives progress, or null if the caller doesn't want any.
            //   cancellationToken: checked before every chunk.
            public async Task DownloadFile(string url, Stream destinationStream, IProgress<double?> progress, CancellationToken cancellationToken)
            {
                RequestedUrls.Add(url);

                if (ExceptionToThrow != null)
                    throw ExceptionToThrow;

                byte[] content;
                if (!files.TryGetValue(url, out content))
                    throw new FileNotFoundException("The requested URL was not found on this server: " + url);

                int written = 0;
                while (written < content.Length) {
                    if (BeforeEachChunk != null)
                        BeforeEachChunk();

                    cancellationToken.ThrowIfCancellationRequested();

                    int count = Math.Min(ChunkSize, content.Length - written);
                    await destinationStream.WriteAsync(content, written, count, cancellationToken);
                    written += count;

                    if (progress != null)
                        progress.Report(ReportUnknownProgress ? (double?)null : (double)written / content.Length);
                }
            }
        }
    }
}
#endif
