// TopicConverter.cs
//
// Converts one HTML Help topic page into a web help page.
//
// The HTML Help topics were all made from the same Dreamweaver template (master.dwt), so they share
// one shape: an old-style <HEAD> with the template's comments, a <title>, a link to StyleSheet.css and
// occasionally a <style> block; then a <BODY> whose content sits between template comments. The
// converted page keeps the title, any <style> blocks and the body content, and gives it a modern head
// that loads the shared web help files (help.css, toc.js, help.js). The body content goes inside
// <main id="topic" data-pagefind-body>: help.js lays the page out around that element, and the
// data-pagefind-body attribute tells Pagefind to index only the topic text, not the sidebar.
//
// It also checks every link and image reference. The CHM viewer ignored case in file names, but a
// web server on Linux does not, so a link to "StyleSheet.css" would break when the file is really
// "Stylesheet.css". Links that differ from the real file name only in case are corrected; links to
// files that do not exist at all are reported.

using System.Text;
using System.Text.RegularExpressions;

namespace HelpConverter
{
    /// <summary>
    /// The problems found and fixed while converting, for the summary printed at the end.
    /// </summary>
    public class ConversionReport
    {
        /// <summary>Links whose case was corrected, as "page: old -> new".</summary>
        public List<string> FixedLinks { get; } = new List<string>();

        /// <summary>Links to files that do not exist, as "page: link".</summary>
        public List<string> BrokenLinks { get; } = new List<string>();
    }

    /// <summary>
    /// Converts HTML Help topic pages into web help pages.
    /// </summary>
    public class TopicConverter
    {
        // The <title> element. Group 1 is the title text.
        private static readonly Regex titleRegex = new Regex(@"<title>(.*?)</title>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        // A <style> block, kept as-is. A few topics define table styles this way.
        private static readonly Regex styleRegex = new Regex(@"<style\b[^>]*>.*?</style>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        // The content of <body>. Group 1 is everything between <body> and </body>.
        private static readonly Regex bodyRegex = new Regex(@"<body\b[^>]*>(.*)</body>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        // The Dreamweaver template markers, such as <!-- #BeginEditable "content" -->.
        private static readonly Regex templateCommentRegex = new Regex(@"<!--\s*#(Begin|End)(Editable|Template)\b.*?-->",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        // An href="..." or src="..." attribute. Group 1 is the attribute name, group 2 the value.
        private static readonly Regex linkRegex = new Regex(@"\b(href|src)\s*=\s*""([^""]*)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // The start of an absolute URL, such as "https:" or "mailto:".
        private static readonly Regex schemeRegex = new Regex(@"^[a-zA-Z][a-zA-Z0-9+.-]*:", RegexOptions.Compiled);

        // Every file in the source directory, keyed case-insensitively, giving its real name.
        private readonly Dictionary<string, string> filesByName;

        private readonly ConversionReport report;

        /// <summary>
        /// Creates a converter for the topics in one directory.
        /// </summary>
        /// <param name="sourceDirectory">The directory holding the HTML Help source.</param>
        /// <param name="report">Where to record fixed and broken links.</param>
        public TopicConverter(string sourceDirectory, ConversionReport report)
        {
            this.report = report;
            filesByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in Directory.GetFiles(sourceDirectory)) {
                string name = Path.GetFileName(path);
                filesByName[name] = name;
            }
        }

        /// <summary>
        /// Converts one topic page and writes the result.
        /// </summary>
        /// <param name="sourcePath">The HTML Help topic page to read.</param>
        /// <param name="outputPath">The web help page to write.</param>
        public void Convert(string sourcePath, string outputPath)
        {
            string pageName = Path.GetFileName(sourcePath);
            string html = ReadText(sourcePath);

            Match titleMatch = titleRegex.Match(html);
            string title = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : Path.GetFileNameWithoutExtension(sourcePath);

            // <style> blocks are only kept from the head; anything in the body stays where it is.
            Match bodyMatch = bodyRegex.Match(html);
            if (!bodyMatch.Success) {
                throw new InvalidDataException($"{pageName} has no <body>.");
            }
            string head = html.Substring(0, bodyMatch.Index);
            List<string> styles = styleRegex.Matches(head).Select(m => m.Value).ToList();

            string content = templateCommentRegex.Replace(bodyMatch.Groups[1].Value, "");
            content = FixLinks(content, pageName);
            content = NormalizeLineEndings(content.Trim());

            StringBuilder page = new StringBuilder();
            page.Append("<!DOCTYPE html>\r\n");
            page.Append("<html lang=\"en\">\r\n");
            page.Append("<head>\r\n");
            page.Append("<meta charset=\"utf-8\">\r\n");
            page.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\r\n");
            page.Append("<title>" + title + "</title>\r\n");
            page.Append("<link rel=\"stylesheet\" href=\"Stylesheet.css\">\r\n");
            page.Append("<link rel=\"stylesheet\" href=\"help.css\">\r\n");
            page.Append("<script src=\"toc.js\" defer></script>\r\n");
            page.Append("<script src=\"help.js\" defer></script>\r\n");
            foreach (string style in styles) {
                page.Append(NormalizeLineEndings(style) + "\r\n");
            }
            page.Append("</head>\r\n");
            page.Append("<body>\r\n");
            page.Append("<main id=\"topic\" data-pagefind-body>\r\n");
            page.Append(content + "\r\n");
            page.Append("</main>\r\n");
            page.Append("</body>\r\n");
            page.Append("</html>\r\n");

            File.WriteAllText(outputPath, page.ToString(), new UTF8Encoding(false));
        }

        /// <summary>
        /// Checks every link and image reference in some HTML, correcting ones whose file name is
        /// in the wrong case and reporting ones to files that do not exist.
        /// </summary>
        /// <param name="html">The HTML to check.</param>
        /// <param name="pageName">The page the HTML came from, for the report.</param>
        /// <returns>The HTML with corrected links.</returns>
        private string FixLinks(string html, string pageName)
        {
            return linkRegex.Replace(html, match => {
                string attribute = match.Groups[1].Value;
                string value = match.Groups[2].Value;
                string fixedValue = FixLink(value, pageName);
                return $"{attribute}=\"{fixedValue}\"";
            });
        }

        /// <summary>
        /// Checks one link or image reference.
        /// </summary>
        /// <param name="value">The href or src value.</param>
        /// <param name="pageName">The page it came from, for the report.</param>
        /// <returns>The value, with the file name's case corrected if needed.</returns>
        private string FixLink(string value, string pageName)
        {
            // Links to other sites, and links within the same page, are left alone.
            if (value.Length == 0 || value.StartsWith('#') || schemeRegex.IsMatch(value)) {
                return value;
            }

            // Split "Page.htm#section" into the file and the "#section" after it.
            int suffixStart = value.IndexOfAny(new char[] { '#', '?' });
            string path = (suffixStart < 0) ? value : value.Substring(0, suffixStart);
            string suffix = (suffixStart < 0) ? "" : value.Substring(suffixStart);
            string fileName = Uri.UnescapeDataString(path);

            if (!filesByName.TryGetValue(fileName, out string? realName)) {
                report.BrokenLinks.Add($"{pageName}: {value}");
                return value;
            }

            if (realName == fileName) {
                return value;
            }

            // Keep the link written the way it was: escaped ("Moving%20around.htm") or not.
            string newPath = (path != fileName) ? Uri.EscapeDataString(realName) : realName;
            string newValue = newPath + suffix;
            report.FixedLinks.Add($"{pageName}: {value} -> {newValue}");
            return newValue;
        }

        /// <summary>
        /// Reads a text file that is UTF-8 (with or without a byte order mark) or, failing that,
        /// Windows-1252, the other encoding HTML Help Workshop pages were commonly saved in.
        /// </summary>
        /// <param name="path">The file to read.</param>
        /// <returns>The file's text, without any byte order mark.</returns>
        public static string ReadText(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            string text;
            try {
                text = new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (DecoderFallbackException) {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                text = Encoding.GetEncoding(1252).GetString(bytes);
            }
            return text.TrimStart('﻿');
        }

        /// <summary>
        /// Makes every line end in CR LF, like the source files.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The text with consistent line endings.</returns>
        private static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
        }
    }
}
