// TocParser.cs
//
// Reads the HTML Help Workshop table of contents (a .hhc "sitemap" file) into a tree of TocNode, and
// writes that tree out as toc.js, the Contents tree used by the web help.
//
// A .hhc file is HTML-ish rather than well-formed: nested <UL> lists whose <LI> items each hold an
// <OBJECT type="text/sitemap"> with <param name="Name"> and (optionally) <param name="Local">. A
// nested <UL> belongs to the <LI> just before it. <LI> and <param> are never closed. So instead of
// an HTML parser, this walks the few tags that matter in document order.

using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HelpConverter
{
    /// <summary>
    /// One entry in the Contents tree.
    /// </summary>
    public class TocNode
    {
        /// <summary>The text shown in the tree.</summary>
        public string Title { get; set; } = "";

        /// <summary>The topic file the entry opens, or null for a folder with no page of its own.</summary>
        public string? Page { get; set; }

        /// <summary>The entries nested under this one; empty for a plain topic.</summary>
        public List<TocNode> Children { get; } = new List<TocNode>();
    }

    /// <summary>
    /// Parses .hhc files and writes toc.js.
    /// </summary>
    public static class TocParser
    {
        // Matches the opening or closing tag of the only elements that matter: UL, LI, OBJECT, PARAM.
        // Group 1 is "/" for a closing tag, group 2 the tag name, group 3 the attributes.
        private static readonly Regex tagRegex = new Regex(@"<(/?)(ul|li|object|param)\b([^>]*)>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches one name="value" attribute. Group 1 is the name, group 2 the value.
        private static readonly Regex attributeRegex = new Regex(@"([\w-]+)\s*=\s*""([^""]*)""",
            RegexOptions.Compiled);

        /// <summary>
        /// Reads a .hhc file into a tree.
        /// </summary>
        /// <param name="hhcPath">The path of the .hhc file.</param>
        /// <returns>The top-level entries of the tree.</returns>
        public static List<TocNode> Parse(string hhcPath)
        {
            string text = File.ReadAllText(hhcPath);

            List<TocNode> roots = new List<TocNode>();

            // The list that entries are currently being added to, innermost last.
            Stack<List<TocNode>> openLists = new Stack<List<TocNode>>();

            // The entry whose <OBJECT> is being read, or null outside an <OBJECT type="text/sitemap">.
            TocNode? current = null;

            foreach (Match match in tagRegex.Matches(text)) {
                bool isClosing = match.Groups[1].Value == "/";
                string tag = match.Groups[2].Value.ToLowerInvariant();
                Dictionary<string, string> attributes = ParseAttributes(match.Groups[3].Value);

                if (tag == "ul" && !isClosing) {
                    // The outermost list holds the top-level entries; any other list holds the
                    // children of the last entry added to the enclosing list.
                    if (openLists.Count == 0) {
                        openLists.Push(roots);
                    }
                    else {
                        List<TocNode> enclosing = openLists.Peek();
                        if (enclosing.Count == 0) {
                            throw new InvalidDataException("A nested <UL> in the table of contents has no <LI> before it to belong to.");
                        }
                        openLists.Push(enclosing[enclosing.Count - 1].Children);
                    }
                }
                else if (tag == "ul" && isClosing) {
                    if (openLists.Count > 0) {
                        openLists.Pop();
                    }
                }
                else if (tag == "object" && !isClosing) {
                    // Only "text/sitemap" objects are entries; the "text/site properties" object at the
                    // top holds window settings.
                    if (attributes.TryGetValue("type", out string? type) && type.Equals("text/sitemap", StringComparison.OrdinalIgnoreCase)) {
                        current = new TocNode();
                    }
                }
                else if (tag == "param" && current != null) {
                    attributes.TryGetValue("name", out string? name);
                    attributes.TryGetValue("value", out string? value);
                    if (name != null && value != null) {
                        if (name.Equals("Name", StringComparison.OrdinalIgnoreCase)) {
                            current.Title = WebUtility.HtmlDecode(value);
                        }
                        else if (name.Equals("Local", StringComparison.OrdinalIgnoreCase)) {
                            current.Page = WebUtility.HtmlDecode(value);
                        }
                    }
                }
                else if (tag == "object" && isClosing && current != null) {
                    if (openLists.Count == 0) {
                        throw new InvalidDataException($"The table of contents entry \"{current.Title}\" is not inside a <UL>.");
                    }
                    openLists.Peek().Add(current);
                    current = null;
                }
            }

            return roots;
        }

        /// <summary>
        /// Visits every entry in a tree, parents before their children.
        /// </summary>
        /// <param name="nodes">The entries to visit.</param>
        /// <returns>All the entries, depth first.</returns>
        public static IEnumerable<TocNode> AllNodes(IEnumerable<TocNode> nodes)
        {
            foreach (TocNode node in nodes) {
                yield return node;
                foreach (TocNode child in AllNodes(node.Children)) {
                    yield return child;
                }
            }
        }

        /// <summary>
        /// Writes the tree as toc.js: a JavaScript file that sets window.helpToc, laid out one entry
        /// per line so that it is easy to edit by hand.
        /// </summary>
        /// <param name="nodes">The top-level entries.</param>
        /// <param name="outputPath">The path of the toc.js file to write.</param>
        public static void WriteTocJs(List<TocNode> nodes, string outputPath)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(
@"// toc.js
//
// The Contents tree shown on the left of every Purple Pen Help page. help.js reads this and draws
// the tree; edit this file to add, remove, rename or reorder topics.
//
// Each entry has:
//   title     The text shown in the tree.
//   page      The topic file it opens. Leave it out for a folder that has no page of its own.
//   children  The entries nested under it (a folder). Leave it out for a plain topic.
//
// Remember the comma between entries. A missing or extra comma stops the tree from appearing at
// all; the browser's developer console (F12) will show the line with the mistake.

window.helpToc = [
");
            WriteNodes(builder, nodes, 1);
            builder.Append("];\n");

            File.WriteAllText(outputPath, builder.ToString(), new UTF8Encoding(false));
        }

        /// <summary>
        /// Writes a list of entries, one per line, recursing into children.
        /// </summary>
        /// <param name="builder">Where to write.</param>
        /// <param name="nodes">The entries to write.</param>
        /// <param name="depth">How far to indent (in units of 4 spaces).</param>
        private static void WriteNodes(StringBuilder builder, List<TocNode> nodes, int depth)
        {
            string indent = new string(' ', depth * 4);

            for (int i = 0; i < nodes.Count; ++i) {
                TocNode node = nodes[i];
                string separator = (i == nodes.Count - 1) ? "" : ",";

                builder.Append(indent);
                builder.Append("{ title: ");
                builder.Append(JsString(node.Title));
                if (node.Page != null) {
                    builder.Append(", page: ");
                    builder.Append(JsString(node.Page));
                }

                if (node.Children.Count == 0) {
                    builder.Append(" }");
                    builder.Append(separator);
                    builder.Append('\n');
                }
                else {
                    builder.Append(", children: [\n");
                    WriteNodes(builder, node.Children, depth + 1);
                    builder.Append(indent);
                    builder.Append("] }");
                    builder.Append(separator);
                    builder.Append('\n');
                }
            }
        }

        /// <summary>
        /// Quotes a string as a JavaScript string literal.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <returns>The literal, including the double quotes.</returns>
        private static string JsString(string value)
        {
            // A JSON string is a valid JavaScript string. The relaxed encoder leaves accented letters
            // and the like readable instead of turning them into \u escapes.
            return JsonSerializer.Serialize(value, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        }

        /// <summary>
        /// Parses the attributes of a tag.
        /// </summary>
        /// <param name="text">The text of the tag between its name and the closing "&gt;".</param>
        /// <returns>The attributes, keyed case-insensitively by name.</returns>
        private static Dictionary<string, string> ParseAttributes(string text)
        {
            Dictionary<string, string> attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in attributeRegex.Matches(text)) {
                attributes[match.Groups[1].Value] = match.Groups[2].Value;
            }
            return attributes;
        }
    }
}
