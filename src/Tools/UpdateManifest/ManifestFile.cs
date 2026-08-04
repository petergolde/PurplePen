// ManifestFile.cs
//
// The update manifest: the classes it deserializes into, and reading, writing and editing it.
//
// IMPORTANT: the manifest format is defined in two places. These classes are a copy of the private
// UpdateManifest and UpdateManifestEntry classes at the bottom of PurplePenCore/CoreUpdater.cs,
// which is what Purple Pen itself reads the manifest with. They are copied rather than shared
// because this tool is standalone -- it is not part of PPen.slnx and doesn't reference PurplePenCore.
// Adding a field to the manifest means changing both, plus adding a command-line option here to set
// it. There is a comment in CoreUpdater.cs saying the same thing from the other side.

using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UpdateManifestTool
{
    /// <summary>
    /// The manifest document: every update that is available, for every platform and channel.
    /// </summary>
    public class UpdateManifest
    {
        public List<UpdateManifestEntry> Updates { get; set; } = new List<UpdateManifestEntry>();

        /// <summary>
        /// Anything in the JSON that isn't one of the members above. Held so that it survives being
        /// read and written back. See the note on <see cref="UpdateManifestEntry.ExtraMembers"/>.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtraMembers { get; set; }
    }

    /// <summary>
    /// One update in the manifest. Everything is a string, and every member can be missing, because
    /// the format is validated in code rather than by the deserializer.
    ///
    /// The declaration order below is the order the members are written out in, so it deliberately
    /// matches the order used in the manifests that already exist.
    /// </summary>
    public class UpdateManifestEntry
    {
        public string? Title { get; set; }
        public string? Version { get; set; }
        public string? Platform { get; set; }
        public string? Channel { get; set; }
        public string? Url { get; set; }
        public string? Sha256 { get; set; }
        public string? Message { get; set; }

        /// <summary>
        /// Anything in the JSON that isn't one of the members above.
        ///
        /// This has no counterpart in CoreUpdater.cs, which simply ignores members it doesn't know
        /// about — it only ever reads. This tool reads the manifest and writes it back, so without
        /// somewhere to keep them, running it against a hand-edited manifest would silently delete
        /// anything it didn't recognize. That is a bad property for the one tool whose whole job is
        /// editing that file. Extension-data members are written out under their original names, so
        /// the camel-case naming policy doesn't disturb them.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtraMembers { get; set; }
    }

    /// <summary>
    /// Reading, writing and editing the manifest file.
    /// </summary>
    public static class ManifestFile
    {
        /// <summary>
        /// How the manifest is read and written. The camel-case policy is what turns
        /// <c>Sha256</c> into <c>sha256</c> and <c>Updates</c> into <c>updates</c>; the null
        /// handling is what keeps a message-only entry from being written out with
        /// <c>"url": null</c> members that Purple Pen would then have to ignore.
        /// </summary>
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true,
        };

        /// <summary>
        /// Loads a manifest, or returns a new empty one if the file doesn't exist yet.
        /// </summary>
        /// <param name="path">Path of the manifest file.</param>
        /// <returns>The manifest, never null.</returns>
        /// <exception cref="JsonException">The file exists but isn't valid JSON.</exception>
        public static UpdateManifest Load(string path)
        {
            if (!File.Exists(path))
                return new UpdateManifest();

            string json = File.ReadAllText(path);

            // An empty file is treated as a new manifest rather than an error: it is what an
            // interrupted earlier run, or "touch manifest.json", leaves behind.
            if (string.IsNullOrWhiteSpace(json))
                return new UpdateManifest();

            UpdateManifest? manifest = JsonSerializer.Deserialize<UpdateManifest>(json, jsonOptions);
            if (manifest == null)
                throw new JsonException("The manifest file does not contain a JSON object.");

            // A manifest with no "updates" member parses fine but leaves the list null; the rest of
            // the tool is entitled to assume it can add to it.
            manifest.Updates ??= new List<UpdateManifestEntry>();

            return manifest;
        }

        /// <summary>
        /// Writes a manifest out, replacing whatever was there before.
        ///
        /// Written to a temporary file and then moved into place, so that running out of disk space
        /// or hitting an error partway through serializing can't leave a truncated manifest where a
        /// working one used to be. Publishing a half-written manifest would break update checking
        /// for everyone.
        /// </summary>
        /// <param name="path">Path of the manifest file.</param>
        /// <param name="manifest">The manifest to write.</param>
        public static void Save(string path, UpdateManifest manifest)
        {
            string json = JsonSerializer.Serialize(manifest, jsonOptions);

            string directory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".";
            Directory.CreateDirectory(directory);

            string temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, json);

            // Move over the top of the original. File.Move with overwrite is atomic enough for the
            // purpose: at no point is there a partly-written file at the manifest's own path.
            File.Move(temporaryPath, path, overwrite: true);
        }

        /// <summary>
        /// Finds the entry describing the same release as the one being added — same version, same
        /// platform, same channel — which is the entry a new one replaces.
        ///
        /// The version is compared as a parsed version number rather than as text, so that "1.2" and
        /// "1.2.0.0" are recognized as the same release instead of accumulating as two entries that
        /// Purple Pen would then have to break a tie between. Platform and channel are compared
        /// case-insensitively, matching how CoreUpdater matches them when it reads the manifest.
        /// </summary>
        /// <param name="manifest">The manifest to search.</param>
        /// <param name="version">Version of the update being added.</param>
        /// <param name="platform">Platform of the update being added.</param>
        /// <param name="channel">Channel of the update being added.</param>
        /// <returns>The index of the matching entry, or -1 if there isn't one.</returns>
        public static int FindMatchingEntry(UpdateManifest manifest, Version version, string platform, string channel)
        {
            for (int i = 0; i < manifest.Updates.Count; ++i) {
                UpdateManifestEntry entry = manifest.Updates[i];

                if (!Version.TryParse(entry.Version, out Version? entryVersion) || entryVersion != version)
                    continue;
                if (!string.Equals(entry.Platform, platform, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.Equals(entry.Channel, channel, StringComparison.OrdinalIgnoreCase))
                    continue;

                return i;
            }

            return -1;
        }

        /// <summary>
        /// Computes the SHA-256 of a file, as the lowercase hex string the manifest records.
        ///
        /// This has to agree exactly with CoreUpdater.ComputeSha256, which is what checks the
        /// downloaded file against the value written here; a difference in encoding would make every
        /// download fail verification.
        /// </summary>
        /// <param name="path">Full path of the file to hash.</param>
        /// <returns>The hash as 64 lowercase hex digits.</returns>
        public static string ComputeSha256(string path)
        {
            byte[] hash;

            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                hash = SHA256.HashData(stream);
            }

            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
