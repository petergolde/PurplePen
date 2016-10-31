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
            Assert.AreEqual(Path.GetFileName(pathname), @"gettestfile.txt");
            Assert.IsTrue(File.Exists(pathname));
            Assert.IsFalse(pathname.Contains(".."));

            pathname = TestUtil.GetTestFile(@"testutil\notexist.txt");
            Assert.AreEqual(Path.GetFileName(pathname), @"notexist.txt");
            Assert.IsFalse(File.Exists(pathname));
            Assert.IsFalse(pathname.Contains(".."));

        }

        [TestMethod]
        public void CompareBitmaps()
        {
            Bitmap bm1 = (Bitmap) Image.FromFile(TestUtil.GetTestFile(@"testutil\compare1.png"));
            Bitmap bm2 = (Bitmap) Image.FromFile(TestUtil.GetTestFile(@"testutil\compare2.gif"));
            Bitmap bm3 = (Bitmap) Image.FromFile(TestUtil.GetTestFile(@"testutil\compare3.gif"));

            Assert.IsNull(TestUtil.CompareBitmaps(bm1, bm2, Color.LightPink, Color.Transparent, 0));
            Bitmap diff = TestUtil.CompareBitmaps(bm1, bm3, Color.FromArgb(255, 225, 235), Color.Transparent, 0);
            Assert.IsNotNull(diff);

            Assert.IsNull(TestUtil.CompareBitmaps(diff, (Bitmap) Image.FromFile(TestUtil.GetTestFile(@"testutil\compare_difference.png")), Color.LightPink, Color.Transparent, 0));
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
            bool correct = TestUtil.CheckBaseline(bm, @"testutil\missing", 0);
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
            correct = TestUtil.CheckBaseline(bm2, @"testutil\missing", 0);
            Assert.IsTrue(correct);
            Assert.IsFalse(File.Exists(TestUtil.GetTestFile(@"testutil\missing_new.png")));
            Assert.IsFalse(File.Exists(TestUtil.GetTestFile(@"testutil\missing_diff.png")));

            Bitmap bm3 = CreateTestBitmap(true);
            correct = TestUtil.CheckBaseline(bm3, @"testutil\missing", 0);
            Assert.IsFalse(correct);
            Assert.IsTrue(File.Exists(TestUtil.GetTestFile(@"testutil\missing_new.png")));
            Assert.IsTrue(File.Exists(TestUtil.GetTestFile(@"testutil\missing_diff.png")));

            // The "new bitmap" should be correct.
            Assert.IsTrue(TestUtil.CompareBitmaps((Bitmap) Image.FromFile(TestUtil.GetTestFile(@"testutil\missing_new.png")), CreateTestBitmap(true), Color.LightPink, Color.Transparent, 0) == null);
        }
	

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
