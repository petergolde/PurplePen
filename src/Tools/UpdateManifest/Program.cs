// Program.cs
//
// UpdateManifest: a command-line tool for creating and updating the Purple Pen update manifest.
//
// Purple Pen checks a JSON manifest at startup to find out whether a newer version is available
// (see PurplePenCore/CoreUpdater.cs). Each entry names a version, the platform and channel it is
// for, where to download it, and the SHA-256 of the download. Writing that by hand means pasting in
// a 64-character hash correctly every time; get one digit wrong and every user on that platform gets
// a download that fails verification, with nothing to say whether it was a typo or a tampered-with
// file. This tool hashes the installer itself.
//
// Run it once per update: it adds an entry, or replaces the existing entry for the same version,
// platform and channel.

using System.Text.Json;

namespace UpdateManifestTool
{
    /// <summary>
    /// Entry point and command-line handling.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Runs the tool.
        /// </summary>
        /// <param name="args">The command line; see <see cref="PrintUsage"/>.</param>
        /// <returns>0 on success, 1 on any error.</returns>
        public static int Main(string[] args)
        {
            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h") || args.Contains("-?")) {
                PrintUsage();
                return args.Length == 0 ? 1 : 0;
            }

            try {
                Options options = Options.Parse(args);
                Run(options);
                return 0;
            }
            catch (OptionException ex) {
                // A problem with what the user typed: say what is wrong and remind them of the
                // usage, since they are most likely at a prompt getting the options straight.
                Console.Error.WriteLine("error: " + ex.Message);
                Console.Error.WriteLine();
                PrintUsage(Console.Error);
                return 1;
            }
            catch (JsonException ex) {
                Console.Error.WriteLine("error: the manifest file could not be parsed: " + ex.Message);
                return 1;
            }
            catch (Exception ex) {
                // A missing file, no permission to write, a full disk.
                Console.Error.WriteLine("error: " + ex.Message);
                return 1;
            }
        }

        /// <summary>
        /// Adds or replaces the entry described by the options, and writes the manifest back out.
        /// </summary>
        /// <param name="options">The validated command line.</param>
        private static void Run(Options options)
        {
            UpdateManifest manifest = ManifestFile.Load(options.ManifestPath);

            UpdateManifestEntry entry = new UpdateManifestEntry {
                Title = options.Title,
                Version = options.Version,
                Platform = options.Platform,
                Channel = options.Channel,
                Message = options.Message,
            };

            // No file means a message-only entry: the release notes tell the user how to update --
            // through a package manager, say -- and there is nothing for Purple Pen to download.
            if (options.FilePath != null) {
                entry.Url = options.ResolveUrl();
                entry.Sha256 = ManifestFile.ComputeSha256(options.FilePath);
            }

            int existingIndex = ManifestFile.FindMatchingEntry(
                manifest, options.ParsedVersion, options.Platform, options.Channel);

            string action;
            if (existingIndex >= 0) {
                // Replaced in place rather than removed and appended, so that re-publishing a
                // release doesn't shuffle the manifest's order about.
                //
                // Anything in the old entry that this tool doesn't know about is carried across, for
                // the same reason the extension data exists at all: replacing an entry should not
                // quietly discard a member somebody added by hand.
                entry.ExtraMembers = manifest.Updates[existingIndex].ExtraMembers;
                manifest.Updates[existingIndex] = entry;
                action = "Replaced";
            }
            else {
                manifest.Updates.Add(entry);
                action = "Added";
            }

            ManifestFile.Save(options.ManifestPath, manifest);

            // Report what happened, so that a release script's log shows which entry was touched and
            // what hash went in -- the two things worth checking after the fact.
            Console.WriteLine("{0} {1} {2} ({3}) in {4}",
                              action, options.Platform, options.Version, options.Channel, options.ManifestPath);
            if (entry.Url != null)
                Console.WriteLine("  url:    {0}", entry.Url);
            if (entry.Sha256 != null)
                Console.WriteLine("  sha256: {0}", entry.Sha256);
            Console.WriteLine("  {0} update(s) in the manifest.", manifest.Updates.Count);
        }

        /// <summary>
        /// Prints how to use the tool.
        /// </summary>
        /// <param name="writer">Where to print it; defaults to standard output.</param>
        private static void PrintUsage(TextWriter? writer = null)
        {
            writer ??= Console.Out;

            writer.WriteLine("UpdateManifest -- creates or updates a Purple Pen update manifest.");
            writer.WriteLine();
            writer.WriteLine("usage: UpdateManifest --manifest <path> --title <text> --version <a.b.c.d>");
            writer.WriteLine("                      --platform <id> --channel <name>");
            writer.WriteLine("                      [--message <text> | --message-file <path>]");
            writer.WriteLine("                      [--file <path>] [--url <url>] [--url-base <base-url>]");
            writer.WriteLine();
            writer.WriteLine("  --manifest <path>      Manifest to update. Created if it does not exist.");
            writer.WriteLine("  --title <text>         Display name, e.g. \"Purple Pen 4.0 beta 3\".");
            writer.WriteLine("  --version <a.b.c.d>    Version number of the update.");
            writer.WriteLine("  --platform <id>        Platform the update is for, e.g. win-x64, osx-arm64,");
            writer.WriteLine("                         linux-x64.");
            writer.WriteLine("  --channel <name>       Channel the update is on, e.g. main, beta.");
            writer.WriteLine("  --message <text>       Release notes shown to the user.");
            writer.WriteLine("  --message-file <path>  Release notes read from a file, for notes that run to");
            writer.WriteLine("                         more than one line.");
            writer.WriteLine("  --file <path>          The installer. Its SHA-256 is computed and recorded.");
            writer.WriteLine("                         Leave it out for an entry that only carries release");
            writer.WriteLine("                         notes and has nothing to download.");
            writer.WriteLine("  --url <url>            Where the installer will be downloaded from.");
            writer.WriteLine("  --url-base <base-url>  Alternative to --url: combined with the name of the");
            writer.WriteLine("                         file given by --file. --url wins if both are given.");
            writer.WriteLine();
            writer.WriteLine("An entry with the same version, platform and channel as an existing one replaces");
            writer.WriteLine("it, keeping its place in the file. Anything else is added at the end.");
            writer.WriteLine();
            writer.WriteLine("example:");
            writer.WriteLine("  UpdateManifest --manifest manifest.json --title \"Purple Pen 4.0\" \\");
            writer.WriteLine("      --version 4.0.0.500 --platform win-x64 --channel main \\");
            writer.WriteLine("      --file out\\PurplePen_4.0.exe \\");
            writer.WriteLine("      --url-base https://purple-pen.org/downloads/ \\");
            writer.WriteLine("      --message \"Fixes printing of relay courses.\"");
        }
    }

    /// <summary>
    /// Something wrong with the command line, as opposed to something wrong out in the world.
    /// Reported with the usage message, because the user is most likely still getting the options
    /// right.
    /// </summary>
    public class OptionException : Exception
    {
        public OptionException(string message) : base(message) { }
    }

    /// <summary>
    /// The parsed and validated command line.
    /// </summary>
    public class Options
    {
        public string ManifestPath { get; private set; } = "";
        public string Title { get; private set; } = "";
        public string Version { get; private set; } = "";
        public string Platform { get; private set; } = "";
        public string Channel { get; private set; } = "";
        public string? Message { get; private set; }
        public string? FilePath { get; private set; }
        public string? Url { get; private set; }
        public string? UrlBase { get; private set; }

        /// <summary>
        /// <see cref="Version"/> parsed, for comparing against the versions already in the manifest.
        /// </summary>
        public Version ParsedVersion { get; private set; } = new Version();

        /// <summary>
        /// Works out the URL to record for the download: the one given outright if there is one,
        /// otherwise the base URL with the file's name on the end.
        /// </summary>
        /// <returns>The URL to put in the manifest.</returns>
        public string ResolveUrl()
        {
            if (Url != null)
                return Url;

            // Exactly one separator between the two halves, whether or not the base ends with one.
            string separator = UrlBase!.EndsWith('/') ? "" : "/";
            return UrlBase + separator + Uri.EscapeDataString(Path.GetFileName(FilePath!));
        }

        /// <summary>
        /// Parses and validates a command line.
        /// </summary>
        /// <param name="args">The arguments, as given to Main.</param>
        /// <returns>The options.</returns>
        /// <exception cref="OptionException">The command line is malformed, incomplete, or asks for
        /// something that would produce an entry Purple Pen could not use.</exception>
        public static Options Parse(string[] args)
        {
            Options options = new Options();
            string? messageFile = null;

            for (int i = 0; i < args.Length; ++i) {
                string name = args[i];

                if (!name.StartsWith("--", StringComparison.Ordinal))
                    throw new OptionException($"unexpected argument \"{name}\"; options start with \"--\".");

                // Every option this tool has takes a value, so the check can be made once here.
                if (i + 1 >= args.Length)
                    throw new OptionException($"{name} needs a value.");

                string value = args[++i];

                switch (name) {
                    case "--manifest": options.ManifestPath = value; break;
                    case "--title": options.Title = value; break;
                    case "--version": options.Version = value; break;
                    case "--platform": options.Platform = value; break;
                    case "--channel": options.Channel = value; break;
                    case "--message": options.Message = value; break;
                    case "--message-file": messageFile = value; break;
                    case "--file": options.FilePath = value; break;
                    case "--url": options.Url = value; break;
                    case "--url-base": options.UrlBase = value; break;
                    default:
                        throw new OptionException($"unknown option \"{name}\".");
                }
            }

            options.Validate(messageFile);
            return options;
        }

        /// <summary>
        /// Checks that the options are complete and consistent, and reads the message file if one
        /// was given.
        /// </summary>
        /// <param name="messageFile">The --message-file value, or null.</param>
        /// <exception cref="OptionException">Something is missing or contradictory.</exception>
        private void Validate(string? messageFile)
        {
            RequireOption(ManifestPath, "--manifest");
            RequireOption(Title, "--title");
            RequireOption(Version, "--version");
            RequireOption(Platform, "--platform");
            RequireOption(Channel, "--channel");

            if (!System.Version.TryParse(Version, out Version? parsedVersion))
                throw new OptionException($"--version \"{Version}\" is not a valid version number; expected something like 4.0.0.500.");
            ParsedVersion = parsedVersion;

            if (Message != null && messageFile != null)
                throw new OptionException("--message and --message-file cannot both be given.");

            if (messageFile != null) {
                if (!File.Exists(messageFile))
                    throw new OptionException($"--message-file \"{messageFile}\" does not exist.");
                Message = File.ReadAllText(messageFile);
            }

            if (FilePath != null) {
                if (!File.Exists(FilePath))
                    throw new OptionException($"--file \"{FilePath}\" does not exist.");

                // Purple Pen won't download something it can't check, and it can't check something
                // it can't find, so an entry needs both or neither.
                if (Url == null && UrlBase == null)
                    throw new OptionException("--file needs either --url or --url-base, to say where the file will be downloaded from.");
            }
            else {
                // A url with no sha256 is exactly what CoreUpdater treats as a malformed entry and
                // skips, which would make the update invisible rather than obviously broken. Refuse
                // to write one.
                if (Url != null || UrlBase != null)
                    throw new OptionException("--url and --url-base need --file, which is what the recorded hash is computed from.");
            }
        }

        /// <summary>
        /// Throws if a required option was not given.
        /// </summary>
        /// <param name="value">The value it was given.</param>
        /// <param name="name">The option's name, for the error message.</param>
        /// <exception cref="OptionException">The value is missing or empty.</exception>
        private static void RequireOption(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
                throw new OptionException($"{name} is required.");
        }
    }
}
