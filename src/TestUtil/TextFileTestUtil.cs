using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestingUtils
{
    public static class TextFileTestUtil
    {
        // Compare two text files line by line. Return true if the same, false if different.
        public static bool CompareTextFiles(string filename1, string filename2)
        {
            return CompareTextFiles(filename1, filename2, new Dictionary<string, string>());
        }

        // Compare two text files line by line. Return true if the same, false if different.
        // An exception map maps strings to regular expressions that can match.
        public static bool CompareTextFiles(string newFile, string baseline, Dictionary<string, string> exceptionMap)
        {
            bool equal = true;
            string line1, line2;

            using (TextReader reader1 = new StreamReader(baseline))
            using (TextReader reader2 = new StreamReader(newFile)) {
                do {
                    line1 = reader1.ReadLine();
                    line2 = reader2.ReadLine();
                    if (line1 != line2) {
                        bool matched = false;
                        foreach (KeyValuePair<string, string> pair in exceptionMap) {
                            if (line1 != null && Regex.Match(line1, pair.Key).Success) {
                                matched = true;
                                if (line2 == null || !Regex.Match(line2, pair.Value).Success)
                                    equal = false;
                                break;
                            }
                        }

                        if (!matched)
                            equal = false;
                    }
                } while (line1 != null && line2 != null);
            }

            return equal;
        }

        public static void CompareTextFileBaseline(string newFile, string baseline)
        {
            CompareTextFileBaseline(newFile, baseline, new Dictionary<string, string>());
        }

        // Compare text file against a baseline, showing a dialog if they don't compare.
        public static void CompareTextFileBaseline(string newFile, string baseline, Dictionary<string, string> exceptionMap)
        {
            baseline = TestUtil.GetSpecificFileName(baseline);

            if (!File.Exists(baseline) || !CompareTextFiles(newFile, baseline, exceptionMap)) {
                if (TestUtil.SilentRun) {
                    Assert.Fail(string.Format("{0} and {1} do not compare", newFile, baseline));
                }
                else {
                    TextFileCompareDialog dialog = new TextFileCompareDialog();
                    dialog.BaselineFilename = baseline;
                    dialog.NewFilename = newFile;
                    DialogResult result = dialog.ShowDialog();
                    dialog.Dispose();

                    if (result == DialogResult.Cancel)
                        Assert.Fail(string.Format("{0} and {1} do not compare", newFile, baseline));
                }
            }
        }


    }
}
