using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace TranslateTool
{
    // Represents a locations.
    class PoLocation
    {
        public readonly string FileName;
        public readonly string Name;
        public PoLocation(string filename, string name)
        {
            this.FileName = filename;
            this.Name = name;
        }
    }

    // Represents a entry in the PO File
    class PoEntry
    {
        public readonly List<PoLocation> Locations;
        public readonly string NonLocalized;
        public readonly string Localized;

        public PoEntry(List<PoLocation> locations, string nonLocalized, string localized)
        {
            this.Locations = locations;
            this.NonLocalized = nonLocalized;
            this.Localized = localized;
        }
    }


    class PoReader
    {
#if TEST
        internal
#endif
 enum PoLineKind { Blank, Comment, LocationComment, KeywordString, String };

#if TEST
        internal
#endif
       class PoLine
        {
            public PoLineKind kind;
            public string str1, str2;
        }

        StreamReader reader;
        List<PoEntry> entries = new List<PoEntry>();
#if TEST
        internal
#endif
        int lineNumber;

        public PoReader(string inputFilename)
        {
            reader = new StreamReader(inputFilename, new UTF8Encoding(false));
            lineNumber = 0;
        }

        public List<PoEntry> ReadPo()
        {
            PoEntry entry;
            while ((entry = ReadPoEntry()) != null) {
                if (entry.NonLocalized != "")
                    entries.Add(entry);          // Don't add header entry
            }

            reader.Close();

            return entries;
        }

        void PoSyntaxError()
        {
            throw new ApplicationException(string.Format("Bad PO line at line {0}", lineNumber));
        }

        // Read one line from the PO File, parse it, and return it.
#if TEST
        internal
#endif
        PoLine ReadLine()
        {
            string s = reader.ReadLine();
            if (s == null)
                return null;

            lineNumber += 1;

            s = s.Trim();
            if (s == "") {
                return new PoLine() {kind = PoLineKind.Blank};
            }
            else if (s.StartsWith("#:")) {
                s = s.Substring(2).Trim();           // Remove command part.
                string[] a = s.Split(new char[] { ' ', '\t', ',' }, 2, StringSplitOptions.RemoveEmptyEntries);
                return new PoLine() { kind = PoLineKind.LocationComment, str1 = a[0], str2 = a[1] };
            }
            else if (s.StartsWith("#")) {
                return new PoLine() { kind = PoLineKind.Comment };
            }
            else if (s.StartsWith("\"")) {
                if (!s.EndsWith("\"") || s.Length < 2)
                    PoSyntaxError();
                s = s.Substring(1, s.Length - 2);
                return new PoLine() { kind = PoLineKind.String, str1 = ConvertQuotedString(s) };
            }
            else {
                int i = 0;
                while (i < s.Length && s[i] != ' ' && s[i] != '\t')
                    ++i;
                if (i >= s.Length || i == 0)
                    PoSyntaxError();

                string keyword = s.Substring(0, i);
                s = s.Substring(i).Trim();
                if (!(s.StartsWith("\"") && s.EndsWith("\"") && s.Length > 1))
                    PoSyntaxError();
                s = s.Substring(1, s.Length - 2);
                return new PoLine() { kind = PoLineKind.KeywordString, str1 = keyword, str2 = ConvertQuotedString(s) };
            }
        }

        PoEntry ReadPoEntry()
        {
            PoLine line;

            // skip whitespace.
            do {
                line = ReadLine();
            } while (line != null && line.kind == PoLineKind.Blank);

            if (line == null)
                return null;           // at end of file.

            List<PoLocation> locations = new List<PoLocation>();
            string localized = "", nonlocalized = "";
            string currentKeyword = null;

            do {
                if (line.kind == PoLineKind.LocationComment)
                    locations.Add(new PoLocation(line.str1, line.str2));
                else if (line.kind == PoLineKind.KeywordString) {
                    currentKeyword = line.str1;
                    if (currentKeyword == "msgid")
                        nonlocalized = line.str2;
                    else if (currentKeyword == "msgstr")
                        localized = line.str2;
                }
                else if (line.kind == PoLineKind.String) {
                    if (currentKeyword == "msgid")
                        nonlocalized += line.str1;
                    else if (currentKeyword == "msgstr")
                        localized += line.str1;
                }

                line = ReadLine();
            }
            while (line != null && line.kind != PoLineKind.Blank);

            return new PoEntry(locations, nonlocalized, localized);
        }

        // Convert quoted characters in a string
        public static string ConvertQuotedString(string s)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < s.Length; ++i) {
                char c = s[i];
                if (c != '\\')
                    builder.Append(c);
                else {
                    if (i == s.Length - 1)
                        break;
                    ++i;
                    c = s[i];
                    if (c >= '0' && c <= '7') {
                        // Octal format.
                        int j;
                        for (j = i; j < s.Length; ++j) {
                            if (s[j] < '0' || s[j] > '7')
                                break;
                        }
                        string octal = s.Substring(i, j - i);
                        i = j - 1;
                        c = (char) Convert.ToInt16(octal, 8);
                        builder.Append(c);
                    }
                    else if (c == 'r') {
                        builder.Append('\r');
                    }
                    else if (c == 'n') {
                        builder.Append("\r\n");
                    }
                    else if (c == '"') {
                        builder.Append('"');
                    }
                    else if (c == '\\') {
                        builder.Append('\\');
                    }
                    else
                        builder.Append(c);
                }
            }

            return builder.ToString();
        }
    }
}
