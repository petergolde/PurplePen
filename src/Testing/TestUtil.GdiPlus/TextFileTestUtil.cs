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
        public static void CompareTextFileBaseline(string newFile, string baseline)
        {
            CompareTextFileBaseline(newFile, baseline, new Dictionary<string, string>());
        }

        // Compare text file against a baseline, showing a dialog if they don't compare.
        public static void CompareTextFileBaseline(string newFile, string baseline, Dictionary<string, string> exceptionMap)
        {
            baseline = TestUtil.GetSpecificFileName(baseline);

            if (!File.Exists(baseline) || !TestUtil.CompareTextFiles(newFile, baseline, exceptionMap)) {
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
