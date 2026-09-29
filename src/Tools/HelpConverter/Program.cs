// Program.cs
//
// HelpConverter: a one-time conversion of the Purple Pen HTML Help (CHM) source into a static help
// website.
//
// The Windows version of Purple Pen shows its help in the Windows HTML Help viewer, compiled by HTML
// Help Workshop from doc\userdocs\Help. The cross-platform version shows help in a web browser
// instead, from a plain static website in doc\userdocs\WebHelp. This tool produces that website:
//
//   - Table of Contents.hhc becomes toc.js, the tree shown on the left of every page.
//   - Each topic .htm becomes a web page with the same file name, whose head loads the shared web
//     help files (help.css, toc.js, help.js). See TopicConverter for details.
//   - Images and the stylesheet are copied unchanged.
//
// help.js, help.css, index.html and build-search.cmd in the output directory are written by hand and
// are not touched by this tool. After converting, run build-search.cmd to build the search index.
//
// Once the conversion is done, the website is edited directly and this tool is not run again: that
// is why it refuses to overwrite an existing conversion unless given --force.

namespace HelpConverter
{
    /// <summary>
    /// Entry point and command-line handling.
    /// </summary>
    public static class Program
    {
        // Files in the HTML Help source that do not belong on the website: the Dreamweaver template,
        // the HTML Help Workshop project and contents files, a Paint.NET image, and Windows thumbnails.
        private static readonly HashSet<string> skippedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            ".dwt", ".hhp", ".hhc", ".pdn", ".db"
        };

        /// <summary>
        /// Runs the tool.
        /// </summary>
        /// <param name="args">The command line; see <see cref="PrintUsage"/>.</param>
        /// <returns>0 on success, 1 on any error.</returns>
        public static int Main(string[] args)
        {
            if (args.Contains("--help") || args.Contains("-h") || args.Contains("-?")) {
                PrintUsage();
                return 0;
            }

            try {
                string? sourceDirectory = null;
                string? outputDirectory = null;
                bool force = false;

                for (int i = 0; i < args.Length; ++i) {
                    if (args[i] == "--force") {
                        force = true;
                    }
                    else if (args[i] == "--source" && i + 1 < args.Length) {
                        sourceDirectory = args[++i];
                    }
                    else if (args[i] == "--output" && i + 1 < args.Length) {
                        outputDirectory = args[++i];
                    }
                    else {
                        Console.Error.WriteLine("error: unrecognized argument: " + args[i]);
                        Console.Error.WriteLine();
                        PrintUsage();
                        return 1;
                    }
                }

                // By default, find doc\userdocs in the repository this tool was built from.
                if (sourceDirectory == null || outputDirectory == null) {
                    string userDocs = FindUserDocsDirectory();
                    sourceDirectory ??= Path.Combine(userDocs, "Help");
                    outputDirectory ??= Path.Combine(userDocs, "WebHelp");
                }

                return Run(Path.GetFullPath(sourceDirectory), Path.GetFullPath(outputDirectory), force);
            }
            catch (Exception ex) {
                Console.Error.WriteLine("error: " + ex.Message);
                return 1;
            }
        }

        /// <summary>
        /// Converts the help.
        /// </summary>
        /// <param name="sourceDirectory">The HTML Help source directory.</param>
        /// <param name="outputDirectory">The web help directory to write.</param>
        /// <param name="force">Whether to overwrite an existing conversion.</param>
        /// <returns>0 on success, 1 if the conversion was refused.</returns>
        private static int Run(string sourceDirectory, string outputDirectory, bool force)
        {
            string[] hhcFiles = Directory.GetFiles(sourceDirectory, "*.hhc");
            if (hhcFiles.Length != 1) {
                throw new InvalidOperationException($"expected exactly one .hhc file in {sourceDirectory}, found {hhcFiles.Length}.");
            }

            // The converted pages get edited by hand afterwards; don't quietly throw that work away.
            if (!force && Directory.Exists(outputDirectory)
                    && (File.Exists(Path.Combine(outputDirectory, "toc.js")) || Directory.GetFiles(outputDirectory, "*.htm").Length > 0)) {
                Console.Error.WriteLine($"error: {outputDirectory} already holds converted help, which may have been edited since.");
                Console.Error.WriteLine("Use --force to overwrite it.");
                return 1;
            }

            Directory.CreateDirectory(outputDirectory);
            ConversionReport report = new ConversionReport();
            TopicConverter converter = new TopicConverter(sourceDirectory, report);

            // The Contents tree.
            List<TocNode> toc = TocParser.Parse(hhcFiles[0]);
            List<string> missingTocPages = FixTocPages(toc, sourceDirectory, report);
            TocParser.WriteTocJs(toc, Path.Combine(outputDirectory, "toc.js"));

            // The topics, images and stylesheet.
            int topicCount = 0, copiedCount = 0;
            foreach (string sourcePath in Directory.GetFiles(sourceDirectory).OrderBy(p => p, StringComparer.OrdinalIgnoreCase)) {
                string fileName = Path.GetFileName(sourcePath);
                string extension = Path.GetExtension(sourcePath);
                string outputPath = Path.Combine(outputDirectory, fileName);

                if (extension.Equals(".htm", StringComparison.OrdinalIgnoreCase) || extension.Equals(".html", StringComparison.OrdinalIgnoreCase)) {
                    converter.Convert(sourcePath, outputPath);
                    ++topicCount;
                }
                else if (!skippedExtensions.Contains(extension)) {
                    File.Copy(sourcePath, outputPath, true);
                    ++copiedCount;
                }
            }

            // Topics that can only be reached by a link from another topic, not from the tree.
            HashSet<string> tocPages = new HashSet<string>(
                TocParser.AllNodes(toc).Where(n => n.Page != null).Select(n => n.Page!), StringComparer.OrdinalIgnoreCase);
            List<string> topicsNotInToc = Directory.GetFiles(sourceDirectory, "*.htm")
                .Select(p => Path.GetFileName(p))
                .Where(name => !tocPages.Contains(name))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Console.WriteLine($"Converted {topicCount} topics and copied {copiedCount} other files to {outputDirectory}.");
            Console.WriteLine($"Wrote toc.js with {TocParser.AllNodes(toc).Count()} entries.");
            PrintList("Links whose file name case was corrected", report.FixedLinks);
            PrintList("BROKEN links (the file does not exist)", report.BrokenLinks);
            PrintList("Contents entries whose page does not exist", missingTocPages);
            PrintList("Topics not in the Contents tree (reachable only by links)", topicsNotInToc);
            Console.WriteLine();
            Console.WriteLine("Next: run build-search.cmd in the output directory to build the search index.");
            return 0;
        }

        /// <summary>
        /// Checks that every page in the Contents tree exists, correcting the case of page names
        /// that differ from the real file only in case.
        /// </summary>
        /// <param name="toc">The Contents tree.</param>
        /// <param name="sourceDirectory">The directory holding the pages.</param>
        /// <param name="report">Where to record corrected names.</param>
        /// <returns>The pages in the tree that do not exist.</returns>
        private static List<string> FixTocPages(List<TocNode> toc, string sourceDirectory, ConversionReport report)
        {
            Dictionary<string, string> filesByName = Directory.GetFiles(sourceDirectory)
                .Select(p => Path.GetFileName(p))
                .ToDictionary(name => name, name => name, StringComparer.OrdinalIgnoreCase);

            List<string> missing = new List<string>();
            foreach (TocNode node in TocParser.AllNodes(toc)) {
                if (node.Page == null) {
                    continue;
                }
                if (!filesByName.TryGetValue(node.Page, out string? realName)) {
                    missing.Add($"{node.Title}: {node.Page}");
                }
                else if (realName != node.Page) {
                    report.FixedLinks.Add($"Contents \"{node.Title}\": {node.Page} -> {realName}");
                    node.Page = realName;
                }
            }
            return missing;
        }

        /// <summary>
        /// Finds doc\userdocs by walking up from the directory this tool runs from.
        /// </summary>
        /// <returns>The full path of doc\userdocs.</returns>
        private static string FindUserDocsDirectory()
        {
            DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null) {
                string candidate = Path.Combine(directory.FullName, "doc", "userdocs");
                if (Directory.Exists(Path.Combine(candidate, "Help"))) {
                    return candidate;
                }
                directory = directory.Parent;
            }
            throw new InvalidOperationException("could not find doc\\userdocs\\Help above the tool's directory; use --source and --output.");
        }

        /// <summary>
        /// Prints a heading and a list of items, or nothing if the list is empty.
        /// </summary>
        /// <param name="heading">What the items are.</param>
        /// <param name="items">The items.</param>
        private static void PrintList(string heading, List<string> items)
        {
            if (items.Count == 0) {
                return;
            }
            Console.WriteLine();
            Console.WriteLine($"{heading} ({items.Count}):");
            foreach (string item in items) {
                Console.WriteLine("    " + item);
            }
        }

        /// <summary>
        /// Prints the command-line usage.
        /// </summary>
        private static void PrintUsage()
        {
            Console.WriteLine("usage: HelpConverter [--source <dir>] [--output <dir>] [--force]");
            Console.WriteLine();
            Console.WriteLine("Converts the HTML Help (CHM) source into a static help website.");
            Console.WriteLine();
            Console.WriteLine("  --source <dir>   The HTML Help source. Default: doc\\userdocs\\Help in this repository.");
            Console.WriteLine("  --output <dir>   Where to write the website. Default: doc\\userdocs\\WebHelp.");
            Console.WriteLine("  --force          Overwrite an existing conversion, losing any edits made to it.");
        }
    }
}
