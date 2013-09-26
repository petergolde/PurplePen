/* Copyright (c) 2006-2008, Peter Golde
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
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestingUtils
{
    // Utilities that are useful for test programs.
    public static class TestUtil
    {
        // Get the test file direction
        public static string GetTestFileDirectory()
        {
            Uri uri = new Uri(typeof(TestUtil).Assembly.CodeBase);
            string callingPath = Path.GetDirectoryName(uri.LocalPath);
            return Path.GetFullPath(Path.Combine(callingPath, @"..\..\..\TestFiles"));
        }

        // Get a file from the test file directory.
        public static string GetTestFile(string basename)
        {
            return Path.GetFullPath(Path.Combine(GetTestFileDirectory(), basename));
        }

        // Compare two bitmaps. If they are different, return a difference bitmap, else return NULL.
        // The difference bitmap has "colorSame" background, and the bits from the second bitmap where differences are.
        // Used as helper from CompareBitmaps if a difference is detected.
        private static Bitmap DifferenceBitmaps(Bitmap bm1, Bitmap bm2, Color colorSame, Color colorDifferent)
        {
            int width = bm1.Width, height = bm1.Height;

            Bitmap diff = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(diff)) {
                if (colorSame != Color.Transparent)
                    g.Clear(colorSame);
                else if (colorDifferent != Color.Transparent)
                    g.Clear(colorDifferent);
            }

            bool different = false;
            for (int x = 0; x < width; ++x)
                for (int y = 0; y < height; ++y) {
                    Color color1 = bm1.GetPixel(x, y);
                    Color color2 = bm2.GetPixel(x, y);
                    if (color1 != color2) {
                        if (colorDifferent == Color.Transparent)
                            diff.SetPixel(x, y, color2);
                        else if (colorSame != Color.Transparent)
                            diff.SetPixel(x, y, colorDifferent);
                        different = true;
                    }
                    else {
                        if (colorSame == Color.Transparent)
                            diff.SetPixel(x, y, color2);
                    }
                }

            if (different)
                return diff;
            else {
                diff.Dispose();
                return null;
            }
        }

        // Compare two bitmaps. If they are different, return a difference bitmap, else return NULL.
        // The difference bitmap has light gray background, and the bits from the second bitmap where differences are.
        // If the bitmaps are different sizes, bm2 is returned.
        public static Bitmap CompareBitmaps(Bitmap bm1, Bitmap bm2, Color colorSame, Color colorDifferent)
        {

            if (bm1.Width != bm2.Width || bm1.Height != bm2.Height)
                return (Bitmap) bm2.Clone();
            int width = bm1.Width, height = bm1.Height;
            Rectangle rect = new Rectangle(0, 0, width, height);
            bool different = false;

            // This is a lot faster that using a zillion GetPixel calls.
            unsafe {
                BitmapData bmdata1 = bm1.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                BitmapData bmdata2 = bm2.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                for (int scan = 0; scan < bmdata1.Height; ++scan) {
                    byte* pixel1 = ((byte*)bmdata1.Scan0) + scan * bmdata1.Stride;
                    byte* pixel2 = ((byte*)bmdata2.Scan0) + scan * bmdata2.Stride;
                    for (int i = 0; i < width * 3; ++i) {
                        if (*pixel1++ != *pixel2++)
                            different = true;
                    }
                }

                bm1.UnlockBits(bmdata1);
                bm2.UnlockBits(bmdata2);
            }

            if (!different)
                return null;
            else
                return DifferenceBitmaps(bm1, bm2, colorSame, colorDifferent);
        }

        // Check a bitmap against a baseline file, named with "_baseline.png" in the normal test files place.
        // If the compare succeeds, true is returned.
        // If the compare fails, false is returned and _new.png and _diff.png files are created.
        // If the baseline is missing, an _baseline_new is created.
        //
        // If the file "updatebaselines" is in that directory; automatically updates baselines (and fails).
        public static bool CheckBaseline(Bitmap bmNew, string basefilename)
        {
            string baselineFile = GetTestFile(basefilename + "_baseline.png");
            string baselineDir = Path.GetDirectoryName(baselineFile);
            bool updateBaselines = File.Exists(Path.Combine(baselineDir, "updatebaselines"));
            if (!File.Exists(baselineFile)) {
                // no baseline -- create a new baseline file.
                string newBaselineFile;
                if (updateBaselines) 
                    newBaselineFile = GetTestFile(basefilename + "_baseline.png");
                else
                    newBaselineFile = GetTestFile(basefilename + "_baseline_new.png");
                bmNew.Save(newBaselineFile, ImageFormat.Png);
                bmNew.Dispose();
                return false;
            }
            else {
                Bitmap bmBaseline = (Bitmap) Image.FromFile(baselineFile);
                Bitmap bmDiff = CompareBitmaps(bmBaseline, bmNew, Color.LightPink, Color.Transparent);
                bmBaseline.Dispose();

                if (bmDiff == null) {
                    // Bitmap compare correctly.
                    bmNew.Dispose();
                    if (updateBaselines)
                        return false;         // so that you don't forget to remove the updatebaselines file!
                    else
                        return true;
                }
                else {
                    // Bitmap didn't compare. Output the new and the diff.
                    string newFile;
                    if (updateBaselines)
                        newFile = GetTestFile(basefilename + "_baseline.png");
                    else
                        newFile = GetTestFile(basefilename + "_new.png");
                    string diffFile = GetTestFile(basefilename + "_diff.png");
                    bmNew.Save(newFile, ImageFormat.Png);
                    bmDiff.Save(diffFile, ImageFormat.Png);
                    bmNew.Dispose();
                    bmDiff.Dispose();
                    return false;
                }
            }                
        }

        // Same as CheckBaseline, but asserts on failure.
        public static void AssertBaseline(Bitmap bmNew, string basefilename)
        {
            Assert.IsTrue(CheckBaseline(bmNew, basefilename), string.Format("Bitmap '{0}' does not compare correctly against baseline", basefilename));
        }

        // Check a bitmap against a baseline file (which could not exist). If the baseline doesn't exist or doesn't compare the
        // same, launch a interactive dialog which displays the baselines.
        // (Unlike AssertBaseline/CheckBaseline, the file name is a the full file name, including extension).
        // bmNew is always disposed.
        public static void CompareBitmapBaseline(string bitmapNew, string baselineFile)
        {
            Assert.IsTrue(File.Exists(bitmapNew));

            using (Bitmap bmNew = (Bitmap) Image.FromFile(bitmapNew))
            {
                CompareBitmapBaseline(bmNew, baselineFile);
            }
        }

        public static void CompareBitmapBaseline(Bitmap bmNew, string baselineFile)
        {
            // Is the file different than the baseline?
            bool different = false, fail = false;

            if (! File.Exists(baselineFile))
                different = true;
            else {
                using (Bitmap bmBaseline = (Bitmap) Image.FromFile(baselineFile)) {
                    Bitmap diff = CompareBitmaps(bmNew, bmBaseline, Color.LightPink, Color.Transparent);
                    different = (diff != null);
                    if (diff != null)
                        diff.Dispose();
                }
            }

            // Show the dialog.
            if (different) {
/*                BitmapCompareDialog dialog = new BitmapCompareDialog();
                dialog.BaselineFilename = baselineFile;
                dialog.NewBitmap = bmNew;
 */
                string tempNewFile = Path.Combine(Path.GetDirectoryName(baselineFile), Path.GetFileNameWithoutExtension(baselineFile) + "_tempnew.png");
                bmNew.Save(tempNewFile, ImageFormat.Png);

                BitmapCompareDialog2 dialog = new BitmapCompareDialog2();
                dialog.BaselineFilename = baselineFile;
                dialog.NewFilename = tempNewFile;
                if (dialog.ShowDialog() == DialogResult.Cancel)
                    fail = true;        // Should fail the test.

                File.Delete(tempNewFile);
            }
            else
                bmNew.Dispose();

            // Clean up and fail the test if desired.
            if (fail) 
                Assert.Fail(string.Format("Bitmap compare against baseline file '{0}' failed", Path.GetFileName(baselineFile)));
        }

        // Same, but uses a "base file name".
        public static void CheckBitmapsBase(Bitmap bmNew, string baselineFileBaseName)
        {
            CompareBitmapBaseline(bmNew, GetTestFile(baselineFileBaseName + "_baseline.png"));
        }

        public static void TestEnumerableAnyOrder<T>(IEnumerable<T> e, T[] expected)
        {
            bool[] found = new bool[expected.Length];
            int i = 0;
            foreach (T item in e) {
                int index;
                for (index = 0; index < expected.Length; ++index) {
                    if (!found[index] && object.Equals(expected[index], item))
                        break;
                }
                Assert.IsTrue(index < expected.Length);
                Assert.IsTrue(object.Equals(expected[index], item));
                found[index] = true;
                ++i;
            }
            Assert.AreEqual(expected.Length, i);
        }

        public static void TestEnumerableAnyOrder<T>(System.Collections.IEnumerable e, T[] expected)
        {
            bool[] found = new bool[expected.Length];
            int i = 0;
            foreach (T item in e) {
                int index;
                for (index = 0; index < expected.Length; ++index) {
                    if (!found[index] && object.Equals(expected[index], item))
                        break;
                }
                Assert.IsTrue(index < expected.Length);
                Assert.IsTrue(object.Equals(expected[index], item));
                found[index] = true;
                ++i;
            }
            Assert.AreEqual(expected.Length, i);
        }

        // Compare two text files line by line. Return true if the same, false if different.
        public static bool CompareTextFiles(string filename1, string filename2) {
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

        public static void CompareTextFileBaseline(string newFile, string baseline) {
            CompareTextFileBaseline(newFile, baseline, new Dictionary<string, string>());
        }

        // Compare text file against a baseline, showing a dialog if they don't compare.
        public static void CompareTextFileBaseline(string newFile, string baseline, Dictionary<string, string> exceptionMap)
        {
            if (!File.Exists(baseline) || !CompareTextFiles(newFile, baseline, exceptionMap)) 
            {
                TextFileCompareDialog dialog = new TextFileCompareDialog();
                dialog.BaselineFilename = baseline;
                dialog.NewFilename = newFile;
                DialogResult result = dialog.ShowDialog();
                dialog.Dispose();

                if (result == DialogResult.Cancel)
                    Assert.Fail(string.Format("{0} and {1} do not compare", newFile, baseline));
            }
        }

        public static void AssertEqualRect(RectangleF expected, RectangleF actual, double delta, string s)
        {
            if (Math.Abs(expected.Left - actual.Left) > delta ||
                Math.Abs(expected.Right - actual.Right) > delta ||
                Math.Abs(expected.Top - actual.Top) > delta ||
                Math.Abs(expected.Bottom - actual.Bottom) > delta) 
            {
                Assert.AreEqual(expected, actual, s);
            }
        }


    }

}
#endif
