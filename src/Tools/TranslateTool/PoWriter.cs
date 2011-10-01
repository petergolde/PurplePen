using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace TranslateTool
{
    class PoWriterAttributes
    {
        public string Name;
        public string Version;
        public string Email;
        public bool writePOT;  // controls writing PO vs. writing POT.
    }

    class PoWriter
    {
        PoWriterAttributes attributes;
        StreamWriter writer;
        Dictionary<string, List<LocString>> stringDict = new Dictionary<string, List<LocString>>();

        public PoWriter(string outputFilename, PoWriterAttributes attributes)
        {
            this.attributes = attributes;
            writer = new StreamWriter(outputFilename, false, new UTF8Encoding(false));
            writer.NewLine = "\n";         // use UNIX style newlines.
        }

        public void WritePot(ResourceDirectory directory)
        {
            WriteHeader();

            foreach (ResXFile resXFile in directory.AllFiles) {
                foreach (LocString str in resXFile.AllStrings) {
                    ProcessString(str);
                }
            }

            WriteStrings();

            writer.Close();
            writer = null;
        }

        private void WriteStrings()
        {
            foreach (List<LocString> list in stringDict.Values) {
                WriteString(list);
            }
        }

        private void WriteString(List<LocString> list)
        {
            foreach (LocString str in list)
                writer.WriteLine("#: {0}, {1}", EncodeString(Path.GetFileName(str.File.NonLocalizedFileName)), str.Name);

            writer.Write("msgid ");
            WriteSplitEncodedText(list[0].NonLocalized);

            writer.Write("msgstr ");
            if (attributes.writePOT)
                writer.WriteLine("\"\"");
            else {
                string firstNonNull = null;
                foreach (LocString str in list) {
                    if (firstNonNull == null)
                        firstNonNull = list[0].Localized;
                }
                if (firstNonNull == null || firstNonNull == "")
                    writer.WriteLine("\"\"");
                else
                    WriteSplitEncodedText(firstNonNull);
            }

            writer.WriteLine();
        }

        private void WriteSplitEncodedText(string text)
        {
            List<string> lines = SplitLines(text);
            foreach (string line in lines)
                writer.WriteLine("\"{0}\"", EncodeString(line));
        }

        private List<string> SplitLines(string s)
        {
            List<string> list = new List<string>(s.Split(new string[] {"\r\n"}, StringSplitOptions.None));
            for (int i = 0; i < list.Count - 1; ++i)
                list[i] = list[i] + "\n";

            for (int i = 0; i < list.Count; ++i) {
                if (list[i].Length > 60) {
                    int splitAt = 60;
                    for (int index = splitAt - 2; index > 0; --index) {
                        if (list[i][index] == ' ') {
                            splitAt = index + 1;
                            break;
                        }
                    }

                    list.Insert(i + 1, list[i].Substring(splitAt));
                    list[i] = list[i].Substring(0, splitAt);
                }
            }

            list.RemoveAll(x => (x.Length == 0));      // remove empty strings

            return list;
        }

        private string EncodeString(string s)
        {
            StringBuilder builder = new StringBuilder(s.Length);

            foreach (char c in s) {
                if (c == '\0')
                    builder.Append("\\0");
                else if (c == '\r')
                    builder.Append("\\r");
                else if (c == '\n')
                    builder.Append("\\n");
                else if (c == '"')
                    builder.Append("\\\"");
                else if (c == '\\')
                    builder.Append("\\\\");
                else if (c < ' ')
                    builder.AppendFormat("\\{0}", Convert.ToString((int) c, 8));        // octal format
                else
                    builder.Append(c);
            }

            return builder.ToString();
        }

        private void ProcessString(LocString str)
        {
            if (str.NonLocalized != "") {
                if (!stringDict.ContainsKey(str.NonLocalized))
                    stringDict[str.NonLocalized] = new List<LocString>();

                stringDict[str.NonLocalized].Add(str);
            }
        }

        private void WriteHeader()
        {
            writer.WriteLine("# {0} localization file", attributes.Name);
            writer.WriteLine("# Copyright (C) 2008");
            writer.WriteLine("# This file is distributed under the same license as the {0} package.", attributes.Name);
            writer.WriteLine("# ");
            writer.WriteLine("#, fuzzy");
            writer.WriteLine("msgid \"\"");
            writer.WriteLine("msgstr \"\"");
            writer.WriteLine("\"Project-Id-Version: {0} {1}\\n\"", attributes.Name, attributes.Version);
            writer.WriteLine("\"Report-Msgid-Bugs-To: {0}\\n\"", attributes.Email);
            writer.WriteLine("\"POT-Creation-Date: {0}\\n\"", DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mmzzzz"));
            writer.WriteLine("\"PO-Revision-Date: \\n\"");
            writer.WriteLine("\"Last-Translator:  FULL NAME <EMAIL@ADDRESS>\\n\"");
            writer.WriteLine("\"Language-Team: \\n\"");
            writer.WriteLine("\"MIME-Version: 1.0\\n\"");
            writer.WriteLine("\"Content-Type: text/plain; charset=UTF-8\\n\"");
            writer.WriteLine("\"Content-Transfer-Encoding: 8bit\\n\"");
            writer.WriteLine();
        }
            

    }
}
