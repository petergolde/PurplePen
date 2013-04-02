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
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Resources;
using System.Reflection;

using PurplePen.MapModel;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;
using PurplePen.Graphics2D;

namespace PurplePen.Tests
{

    [TestClass]
    public class UtilTests
    {
        [TestMethod]
        public void GetRelativeFileName()
        {
            Assert.AreEqual("foo.xml", Util.GetRelativeFileName(@"c:\hello there\hi\bar.xml", @"c:\hello there\hi\foo.xml"));
            Assert.AreEqual(@"map files\foo.xml", Util.GetRelativeFileName(@"c:\hello there\hi\bar.xml", @"c:\hello there\hi\map files\foo.xml"));
            Assert.AreEqual(@"..\..\hello there\hi\map files\foo.xml", Util.GetRelativeFileName(@"c:\glop\hi\bar.xml", @"c:\hello there\hi\map files\foo.xml"));
            Assert.AreEqual(@"d:\hello there\hi\map files\foo.xml", Util.GetRelativeFileName(@"c:\glop\hi\bar.xml", @"d:\hello there\hi\map files\foo.xml"));
            Assert.AreEqual(@"map files\foo.xml", Util.GetRelativeFileName(@"c:\hello there\hi\foo.xml", @"c:\hello there\hi\map files\foo.xml"));
        }

        [TestMethod]
        public void GetRelativeFileName2()
        {
            string xmlName = TestUtil.GetTestFile("output_test.xml");
            XmlTextWriter writer = new XmlTextWriter(xmlName, Encoding.UTF8);

            Assert.AreEqual("foo.ocad", Util.GetRelativeFileName(writer, TestUtil.GetTestFile("foo.ocad")));
            Assert.AreEqual(@"map files\foo.ocad", Util.GetRelativeFileName(writer, TestUtil.GetTestFile(@"map files\foo.ocad")));
            Assert.AreEqual(@"x:\hello there\hi\map files\foo.xml", Util.GetRelativeFileName(writer, @"x:\hello there\hi\map files\foo.xml"));

            writer.Close();

            writer = new XmlTextWriter(new StringWriter());

            Assert.AreEqual(TestUtil.GetTestFile("foo.ocad"), Util.GetRelativeFileName(writer, TestUtil.GetTestFile("foo.ocad")));
            Assert.AreEqual(TestUtil.GetTestFile(@"map files\foo.ocad"), Util.GetRelativeFileName(writer, TestUtil.GetTestFile(@"map files\foo.ocad")));
            Assert.AreEqual(@"x:\hello there\hi\map files\foo.xml", Util.GetRelativeFileName(writer, @"x:\hello there\hi\map files\foo.xml"));

            writer.Close();
        }

        [TestMethod]
        public void RemoveMeterSuffix()
        {
            Assert.AreEqual(null, Util.RemoveMeterSuffix(null));
            Assert.AreEqual("", Util.RemoveMeterSuffix(""));
            Assert.AreEqual("foo", Util.RemoveMeterSuffix("foo"));
            Assert.AreEqual("5", Util.RemoveMeterSuffix("5"));
            Assert.AreEqual("5", Util.RemoveMeterSuffix("5m"));
            Assert.AreEqual("5", Util.RemoveMeterSuffix("5 m"));
            Assert.AreEqual("5", Util.RemoveMeterSuffix("5m "));
            Assert.AreEqual("5", Util.RemoveMeterSuffix("5 m "));
        }

        [TestMethod]
        public void Round()
        {
            RectangleF r;
            Rectangle s, t;

            r = new RectangleF(1.3F, 1.0F, 5.3F, 5.6F);
            Console.WriteLine("Before: ({0},{1})-({2},{3}), wid={4}, height={5}", r.Left, r.Top, r.Right, r.Bottom, r.Width, r.Height);
            s = Rectangle.Round(r);
            Console.WriteLine("After: ({0},{1})-({2},{3}), wid={4}, height={5}", s.Left, s.Top, s.Right, s.Bottom, s.Width, s.Height);
            t = Util.Round(r);
            Console.WriteLine("After: ({0},{1})-({2},{3}), wid={4}, height={5}", t.Left, t.Top, t.Right, t.Bottom, t.Width, t.Height);
            Assert.AreEqual(1, t.Left);
            Assert.AreEqual(1, t.Top);
            Assert.AreEqual(7, t.Right);
            Assert.AreEqual(7, t.Bottom);
        }

        [TestMethod]
        public void IsInteger()
        {
            Assert.IsTrue(Util.IsInteger("35"));
            Assert.IsTrue(Util.IsInteger("100"));
            Assert.IsFalse(Util.IsInteger("-20"));
            Assert.IsFalse(Util.IsInteger("4.5"));
            Assert.IsFalse(Util.IsInteger("GO"));
            Assert.IsFalse(Util.IsInteger(""));
        }

        [TestMethod]
        public void CompareCodes()
        {
            Assert.AreEqual(0, Util.CompareCodes(null, null));
            Assert.AreEqual(-1, Util.CompareCodes(null, "5"));
            Assert.AreEqual(1, Util.CompareCodes("GO", null));
            Assert.AreEqual(-1, Util.CompareCodes("78", "135"));
            Assert.AreEqual(0, Util.CompareCodes("78", "78"));
            Assert.AreEqual(-1, Util.CompareCodes("135", "HI"));
            Assert.AreEqual(1, Util.CompareCodes("0V", "23"));
            Assert.AreEqual(-1, Util.CompareCodes("HI", "X"));
            Assert.AreEqual(0, Util.CompareCodes("HI", "HI"));
            Assert.AreEqual(1, Util.CompareCodes("HI", "ab"));
        }

        [TestMethod]
        public void AddPointToArray()
        {
            PointF[] array = { new PointF(3, 7), new PointF(11, 2), new PointF(0, -7), new PointF(-12, -3), new PointF(4, 6) };
            array = Util.AddPointToArray(array, new PointF(-5, 5));
            array = Util.AddPointToArray(array, new PointF(-4, -2));
            array = Util.AddPointToArray(array, new PointF(12, -1));
            Assert.AreEqual(8, array.Length);
            Assert.AreEqual(new PointF(3, 7), array[0]);
            Assert.AreEqual(new PointF(11, 2), array[1]);
            Assert.AreEqual(new PointF(12, -1), array[2]);
            Assert.AreEqual(new PointF(0, -7), array[3]);
            Assert.AreEqual(new PointF(-4, -2), array[4]);
            Assert.AreEqual(new PointF(-12, -3), array[5]);
            Assert.AreEqual(new PointF(-5, 5), array[6]);
            Assert.AreEqual(new PointF(4,6), array[7]);
        }

        [TestMethod]
        public void RemovePointFromArray()
        {
            PointF[] array = { new PointF(3, 7), new PointF(11, 2), new PointF(0, -7), new PointF(-12, -3), new PointF(4, 6) };
            array = Util.RemovePointFromArray(array, new PointF(0, -7));
            array = Util.RemovePointFromArray(array, new PointF(3, 7));
            array = Util.RemovePointFromArray(array, new PointF(4, 6));
            Assert.AreEqual(2, array.Length);
            Assert.AreEqual(new PointF(11, 2), array[0]);
            Assert.AreEqual(new PointF(-12, -3), array[1]);
        }

        [TestMethod]
        public void GetBit()
        {
            uint u = 0x7F451201;

            Assert.IsTrue(Util.GetBit(u, 0));
            Assert.IsTrue(Util.GetBit(u, 32));
            Assert.IsTrue(Util.GetBit(u, 64));

            Assert.IsFalse(Util.GetBit(u, 1));
            Assert.IsFalse(Util.GetBit(u, 33));
            Assert.IsFalse(Util.GetBit(u, -31));

            Assert.IsTrue(Util.GetBit(u, 18));
            Assert.IsTrue(Util.GetBit(u, 32 + 18));
            Assert.IsTrue(Util.GetBit(u, 18 - 32));

        }

        [TestMethod]
        public void SetBit()
        {
            uint u = 0x7F451201;

            Assert.AreEqual(0x7F451200U, Util.SetBit(u, 0, false));
            Assert.AreEqual(0x7F451201U, Util.SetBit(u, 0, true));
            Assert.AreEqual(0x7F451200U, Util.SetBit(u, 32, false));
            Assert.AreEqual(0x7F451201U, Util.SetBit(u, 32, true));

            Assert.AreEqual(0x7F451201U, Util.SetBit(u, 1, false));
            Assert.AreEqual(0x7F451203U, Util.SetBit(u, 1, true));
            Assert.AreEqual(0x7F451201U, Util.SetBit(u, 33, false));
            Assert.AreEqual(0x7F451203U, Util.SetBit(u, 33, true));
            Assert.AreEqual(0x7F451201U, Util.SetBit(u, -31, false));
            Assert.AreEqual(0x7F451203U, Util.SetBit(u, -31, true));

            Assert.AreEqual(0x77451201U, Util.SetBit(u, 27, false));
            Assert.AreEqual(0x7F451201U, Util.SetBit(u, 27, true));
            Assert.AreEqual(0x77451201U, Util.SetBit(u, 27+32, false));
            Assert.AreEqual(0x7F451201U, Util.SetBit(u, 27+32, true));
            Assert.AreEqual(0x77451201U, Util.SetBit(u, 27-32, false));
            Assert.AreEqual(0x7F451201U, Util.SetBit(u, 27-32, true));

        }

        [TestMethod]
        public void CompareVersionString()
        {
            Assert.AreEqual(1, Util.CompareVersionStrings("1.0.4.2", "1.0.3.4"));
            Assert.AreEqual(-1, Util.CompareVersionStrings("1.4.2", "1.4.2.1"));
            Assert.AreEqual(-1, Util.CompareVersionStrings("1.4.2", "2.1.2.1"));
            Assert.AreEqual(-1, Util.CompareVersionStrings("0.0.4.2", "2.0"));
            Assert.AreEqual(0, Util.CompareVersionStrings("0.0.4.2", "0.0.4.2"));
        }

        [TestMethod]
        public void PrettyVersionString()
        {
            Assert.AreEqual("1.0.4", Util.PrettyVersionString("1.0.4.500"));
            Assert.AreEqual("2.0.0", Util.PrettyVersionString("2.0.0.500"));
            Assert.AreEqual("2.1.1 Beta 2", Util.PrettyVersionString("2.1.1.220"));
            Assert.AreEqual("1.0.0 RC 3", Util.PrettyVersionString("1.0.0.330"));
            Assert.AreEqual("1.0.1 Alpha 1", Util.PrettyVersionString("1.0.1.110"));
        }

        [TestMethod]
        public void CopyDictionary()
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            dict.Add("hello", 12);
            dict.Add("goodbye", 2);
            dict.Add("elvis", 981);
            dict.Add("foo", 0);
            dict.Add("bar", -3);
            dict.Add("baz", 1299);

            Dictionary<string, int> dict2 = Util.CopyDictionary(dict);
            dict.Remove("goodbye");
            dict["elvis"] = 8991;
            dict.Add("bizarre", 99);
            dict.Add("bellevue", 101);

            Assert.AreEqual(12, dict2["hello"]);
            Assert.AreEqual(2, dict2["goodbye"]);
            Assert.AreEqual(981, dict2["elvis"]);
            Assert.AreEqual(0, dict2["foo"]);
            Assert.AreEqual(-3, dict2["bar"]);
            Assert.AreEqual(1299, dict2["baz"]);
            Assert.IsFalse(dict2.ContainsKey("bizarre"));
            Assert.IsFalse(dict2.ContainsKey("bellevue"));
        }

        [TestMethod]
        public void FilterInvalidPathChars()
        {
            string result;

            result = Util.FilterInvalidPathChars(@"baz.txt");
            Assert.AreEqual(@"baz.txt", result);

            result = Util.FilterInvalidPathChars(@"foo&bar");
            Assert.AreEqual(@"foo&bar", result);

            result = Util.FilterInvalidPathChars(@"foo/bar\baz");
            Assert.AreEqual(@"foo_bar_baz", result);

            result = Util.FilterInvalidPathChars(@"foo<bar|baz>");
            Assert.AreEqual(@"foo_bar_baz_", result);
        }

        [TestMethod]
        public void PrintScaleList()
        {
            float[] result;

            result = Util.PrintScaleList(7500);
            CollectionAssert.AreEqual(new float[] { 4000, 5000, 7500, 10000, 15000 }, result);

            result = Util.PrintScaleList(8000);
            CollectionAssert.AreEqual(new float[] { 4000, 5000, 7500, 8000, 10000, 15000 }, result);
        }

        [TestMethod]
        public void RemoveHotkeyPrefix()
        {
            Assert.AreEqual("My Report", Util.RemoveHotkeyPrefix("My &Report"));
            Assert.AreEqual("Hello", Util.RemoveHotkeyPrefix("Hello"));
        }

        /*
        void WriteResourceText(Type type, string filename)
        {
            ResXResourceWriter writer = new ResXResourceWriter(filename);

            foreach (FieldInfo fi in type.GetFields()) {
                string name = fi.Name;
                string value = (string) (fi.GetRawConstantValue());
                Console.WriteLine("Name={0}   Value={1}", name, value);
                writer.AddResource(name, value);
            }

            writer.Generate();
            writer.Close();
        }

        [TestMethod] public void WriteText()
        {
            WriteResourceText(typeof(MiscText), @"c:\users\peter\documents\ppen\src\purplepen\purplepen\MiscText.resx");
            WriteResourceText(typeof(CommandNameText), @"c:\users\peter\documents\ppen\src\purplepen\purplepen\CommandNameText.resx");
            WriteResourceText(typeof(StatusBarText), @"c:\users\peter\documents\ppen\src\purplepen\purplepen\StatusBarText.resx");
        }
         */

    }
}

#endif //TEST
