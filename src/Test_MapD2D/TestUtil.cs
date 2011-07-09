/* Copyright (c) 2006-2007, Peter Golde
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

using System;
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
        public static Bitmap DifferenceBitmaps(Bitmap bm1, Bitmap bm2, Color colorSame, Color colorDifferent)
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

        // Compare two bitmaps and return if they are different.
        public static bool AreBitmapsDifferent(Bitmap bm1, Bitmap bm2)
        {

            if (bm1.Width != bm2.Width || bm1.Height != bm2.Height)
                return true;
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

            return different;
        }

        // Check a new bitmap against a baseline file (which could not exist). If the baseline doesn't exist or doesn't compare the
        // same, launch a interactive dialog which displays the baselines.
        public static void CompareBitmapBaseline(string newFile, string baselineFile)
        {
            // Is the file different than the baseline?
            bool different = false, fail = false;

            if (! File.Exists(baselineFile))
                different = true;
            else {
                using (Bitmap bmNew = (Bitmap) Image.FromFile(newFile))
                using (Bitmap bmBaseline = (Bitmap) Image.FromFile(baselineFile)) 
                {
                    different = AreBitmapsDifferent(bmNew, bmBaseline);
                }
            }

            // Show the dialog.
            if (different) {
                BitmapCompareDialog2 dialog = new BitmapCompareDialog2();
                dialog.BaselineFilename = baselineFile;
                dialog.NewFilename = newFile;
                if (dialog.ShowDialog() == DialogResult.Cancel)
                    fail = true;        // Should fail the test.
            }

            // Clean up and fail the test if desired.
            if (fail) 
                Assert.Fail(string.Format("Bitmap compare against baseline file '{0}' failed", Path.GetFileName(baselineFile)));
        }

#if false
        // Compare two text files line by line. Return true if the same, false if different.
        public static bool CompareTextFiles(string filename1, string filename2)
        {
            bool equal = true;
            string line1, line2;

            using (TextReader reader1 = new StreamReader(filename1))
            using (TextReader reader2 = new StreamReader(filename2)) {
                do {
                    line1 = reader1.ReadLine();
                    line2 = reader2.ReadLine();
                    if (line1 != line2)
                        equal = false;
                } while (line1 != null && line2 != null);
            }

            return equal;
        }

        // Compare text file against a baseline, showing a dialog if they don't compare.
        public static void CompareTextFileBaseline(string newFile, string baseline)
        {
            if (!File.Exists(baseline) || !CompareTextFiles(newFile, baseline)) 
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
#endif

    }
}
