#if TEST

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace TestingUtils.Tests
{
    [TestClass]
    public class TestUtilTests
    {
        [TestMethod]
        public void GetTestFile()
        {
            string pathname = TestUtil.GetTestFile(@"testutil\gettestfile.txt");
            Assert.AreEqual(@"gettestfile.txt", Path.GetFileName(pathname));
            Assert.IsTrue(File.Exists(pathname));
            Assert.IsFalse(pathname.Contains(".."));

            pathname = TestUtil.GetTestFile(@"testutil\notexist.txt");
            Assert.AreEqual(@"notexist.txt", Path.GetFileName(pathname));
            Assert.IsFalse(File.Exists(pathname));
            Assert.IsFalse(pathname.Contains(".."));

        }

#if false
        [TestMethod]
        public void CompareBitmaps()
        {
            Bitmap bm1 = (Bitmap) Image.FromFile(TestUtil.GetTestFile(@"testutil\compare1.png"));
            Bitmap bm2 = (Bitmap) Image.FromFile(TestUtil.GetTestFile(@"testutil\compare2.gif"));
            Bitmap bm3 = (Bitmap) Image.FromFile(TestUtil.GetTestFile(@"testutil\compare3.gif"));

            Assert.IsNull(BitmapTestUtil.CompareBitmaps(bm1, bm2, Color.LightPink, Color.Transparent, 0));
            Bitmap diff = BitmapTestUtil.CompareBitmaps(bm1, bm3, Color.FromArgb(255, 225, 235), Color.Transparent, 0);
            Assert.IsNotNull(diff);

            Assert.IsNull(BitmapTestUtil.CompareBitmaps(diff, (Bitmap) Image.FromFile(TestUtil.GetTestFile(@"testutil\compare_difference.png")), Color.LightPink, Color.Transparent, 0));
        }

        private Bitmap CreateTestBitmap(bool addSlash)
        {
            Bitmap bm = new Bitmap(250, 250);

            using (Graphics g = Graphics.FromImage(bm)) {
                g.Clear(Color.Green);
                g.DrawEllipse(new Pen(Color.Violet, 10), RectangleF.FromLTRB(50, 50, 220, 160));
                if (addSlash)
                    g.DrawLine(new Pen(Color.Turquoise, 25), 30, 200, 190, 70);
            }

            return bm;
        }

        [TestMethod]
        public void CheckBaseline()
        {
            // Make sure no baseline exists.
            File.Delete(TestUtil.GetTestFile(@"testutil\missing_baseline.png"));
            Assert.IsFalse(File.Exists(TestUtil.GetTestFile(@"testutil\missing_baseline.png")));

            // Test against non-existant baseline -- should create a baseline_new.
            Bitmap bm = CreateTestBitmap(false);
            bool correct = BitmapTestUtil.CheckBaseline(bm, @"testutil\missing", 0);
            Assert.IsFalse(correct);
            Assert.IsTrue(File.Exists(TestUtil.GetTestFile(@"testutil\missing_baseline_new.png")));

            // Remove baseline, new, diff.
            File.Delete(TestUtil.GetTestFile(@"testutil\missing_baseline.png"));
            File.Delete(TestUtil.GetTestFile(@"testutil\missing_new.png"));
            File.Delete(TestUtil.GetTestFile(@"testutil\missing_diff.png"));
            Assert.IsFalse(File.Exists(TestUtil.GetTestFile(@"testutil\missing_baseline.png")));
            Assert.IsFalse(File.Exists(TestUtil.GetTestFile(@"testutil\missing_new.png")));
            Assert.IsFalse(File.Exists(TestUtil.GetTestFile(@"testutil\missing_diff.png")));

            // Create the new baseline.
            File.Move(TestUtil.GetTestFile(@"testutil\missing_baseline_new.png"), TestUtil.GetTestFile(@"testutil\missing_baseline.png"));

            // Check identical bitmap against the baseline.
            Bitmap bm2 = CreateTestBitmap(false);
            correct = BitmapTestUtil.CheckBaseline(bm2, @"testutil\missing", 0);
            Assert.IsTrue(correct);
            Assert.IsFalse(File.Exists(TestUtil.GetTestFile(@"testutil\missing_new.png")));
            Assert.IsFalse(File.Exists(TestUtil.GetTestFile(@"testutil\missing_diff.png")));

            Bitmap bm3 = CreateTestBitmap(true);
            correct = BitmapTestUtil.CheckBaseline(bm3, @"testutil\missing", 0);
            Assert.IsFalse(correct);
            Assert.IsTrue(File.Exists(TestUtil.GetTestFile(@"testutil\missing_new.png")));
            Assert.IsTrue(File.Exists(TestUtil.GetTestFile(@"testutil\missing_diff.png")));

            // The "new bitmap" should be correct.
            Assert.IsTrue(BitmapTestUtil.CompareBitmaps((Bitmap) Image.FromFile(TestUtil.GetTestFile(@"testutil\missing_new.png")), CreateTestBitmap(true), Color.LightPink, Color.Transparent, 0) == null);
        }
#endif

        [TestMethod]
        public void TestEnumerableAnyOrder()
        {
            List<string> list = new List<string>();
            list.Add("foobar");
            list.Add("bazbar".Substring(0, 3));
            list.Add("sniggles");
            list.Add("snoggles");

            TestUtil.TestEnumerableAnyOrder(list, new string[] { "snoggles", "baz", "foobar", "sniggles" });
            TestUtil.TestEnumerableAnyOrder((System.Collections.IEnumerable) list, new string[] { "snoggles", "baz", "foobar", "sniggles" });
        }
    }
}
#endif
