using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestingUtils
{
    public class BitmapTestUtil
    {
        // Compare two bitmaps. If they are different, return a difference bitmap, else return NULL.
        // The difference bitmap has "colorSame" background, and the bits from the second bitmap where differences are.
        // Used as helper from CompareBitmaps if a difference is detected.
        public static Bitmap DifferenceBitmaps(Bitmap bm1, Bitmap bm2, Color colorSame, Color colorDifferent, int maxPixelDifference = 0)
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
            double maxColorDiff = 0.0;
            for (int x = 0; x < width; ++x)
                for (int y = 0; y < height; ++y) {
                    Color color1 = bm1.GetPixel(x, y);
                    Color color2 = bm2.GetPixel(x, y);

                    if (color1 != color2 && (maxPixelDifference == 0 || !TestUtil.SimilarColors(color1, color2, maxPixelDifference))) {
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

                    if (color1 != color2) {
                        maxColorDiff = Math.Max(maxColorDiff, TestUtil.ColorDifference(color1, color2));
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
        public static Bitmap CompareBitmaps(Bitmap bm1, Bitmap bm2, Color colorSame, Color colorDifferent, int maxPixelDifference)
        {

            if (bm1.Width != bm2.Width || bm1.Height != bm2.Height)
                return (Bitmap)bm2.Clone();
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
                return DifferenceBitmaps(bm1, bm2, colorSame, colorDifferent, maxPixelDifference);
        }

        // Check a bitmap against a baseline file (which could not exist). If the baseline doesn't exist or doesn't compare the
        // same, launch a interactive dialog which displays the baselines.
        // (Unlike AssertBaseline/CheckBaseline, the file name is a the full file name, including extension).
        // bmNew is always disposed.
        public static void CompareBitmapBaseline(string bitmapNew, string baselineFile, int maxPixelDifference = 0)
        {
            Assert.IsTrue(File.Exists(bitmapNew));

            using (Bitmap bmNew = (Bitmap)Image.FromFile(bitmapNew)) {
                CompareBitmapBaseline(bmNew, baselineFile, maxPixelDifference);
            }
        }

        public static void CompareBitmapBaseline(Bitmap bmNew, string baselineFile, int maxPixelDifference = 0)
        {
            // Is the file different than the baseline?
            bool different = false, fail = false;

            baselineFile = TestUtil.GetSpecificFileName(baselineFile);

            Bitmap difference = null;

            if (!File.Exists(baselineFile))
                different = true;
            else {
                using (Bitmap bmBaseline = (Bitmap)Image.FromFile(baselineFile)) {
                    difference = CompareBitmaps(bmNew, bmBaseline, Color.LightPink, Color.Transparent, maxPixelDifference);
                    different = (difference != null);
                }
            }

            // Show the dialog.
            if (different) {
                if (TestUtil.SilentRun) {
                    // Nobody is watching, so leave the evidence on disk instead:
                    // what was rendered, and where it differs from the baseline.
                    SaveComparison(bmNew, difference, baselineFile);
                    fail = true;
                }
                else {
                    string tempNewFile = Path.Combine(Path.GetDirectoryName(baselineFile), Path.GetFileNameWithoutExtension(baselineFile) + "_tempnew.png");
                    bmNew.Save(tempNewFile, ImageFormat.Png);

                    BitmapCompareDialog2 dialog = new BitmapCompareDialog2();
                    dialog.MaxPixelDifference = maxPixelDifference;
                    dialog.BaselineFilename = baselineFile;
                    dialog.NewFilename = tempNewFile;
                    if (dialog.ShowDialog() == DialogResult.Cancel)
                        fail = true;        // Should fail the test.

                    File.Delete(tempNewFile);
                }
            }
            else
                bmNew.Dispose();

            if (difference != null)
                difference.Dispose();

            // Clean up and fail the test if desired.
            if (fail)
                Assert.Fail(string.Format("Bitmap compare against baseline file '{0}' failed", Path.GetFileName(baselineFile)));
        }

        // Writes what a failed comparison produced next to the baseline, so that an
        // unattended run leaves something to look at.
        //
        // Parameters:
        //   bmNew - the bitmap that was rendered.
        //   difference - the differences against the baseline, or null when the
        //     baseline file was missing altogether.
        //   baselineFile - full path of the baseline the comparison used.
        private static void SaveComparison(Bitmap bmNew, Bitmap difference, string baselineFile)
        {
            string directory = Path.GetDirectoryName(baselineFile);
            string stem = Path.GetFileNameWithoutExtension(baselineFile);

            try {
                bmNew.Save(Path.Combine(directory, stem + "_actual.png"), ImageFormat.Png);
                if (difference != null)
                    difference.Save(Path.Combine(directory, stem + "_diff.png"), ImageFormat.Png);
            }
            catch (IOException) {
                // Evidence is a convenience; failing to write it must not replace
                // the comparison's own result with an unrelated error.
            }
            catch (UnauthorizedAccessException) {
            }
        }

        // Same, but uses a "base file name".
        public static void CheckBitmapsBase(Bitmap bmNew, string baselineFileBaseName, int maxPixelDifference = 0)
        {
            CompareBitmapBaseline(bmNew, TestUtil.GetTestFile(baselineFileBaseName + "_baseline.png"), maxPixelDifference);
        }


    }
}
