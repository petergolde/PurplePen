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

#if TEST
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.Tests
{

    [TestClass]
    public class XmlInputTests
    {
        [TestMethod]
        public void CloseFile()
        {
            // Make sure that Dispose really closes the underlying file.
            File.Copy(TestUtil.GetTestFile("xmlinput.xml"), TestUtil.GetTestFile("temp.xml"));
            XmlInput xmlinput = new XmlInput(TestUtil.GetTestFile("temp.xml"));
            xmlinput.CheckElement("rootsym");
            xmlinput.Dispose();
            File.Delete(TestUtil.GetTestFile("temp.xml"));
        }

        [TestMethod]
        public void Read1()
        {
            XmlInput xmlinput = new XmlInput(TestUtil.GetTestFile("xmlinput.xml"));
            xmlinput.CheckElement("rootsym");

            try {
                xmlinput.CheckElement("fiddle");
                Assert.Fail("expect exception");
            }
            catch (Exception e) {
                Assert.IsTrue(e is XmlFileFormatException);
            }

            xmlinput.Read();
            xmlinput.CheckElement("fiddle");
            Assert.AreEqual(1, xmlinput.GetAttributeInt("y"));
            Assert.AreEqual(false, xmlinput.GetAttributeBool("z"));
            Assert.AreEqual("foo", xmlinput.GetAttributeString("x"));

            xmlinput.Skip();

            xmlinput.CheckElement("faddle");
            Assert.AreEqual(3.4F, xmlinput.GetAttributeFloat("x"));
            try {
                Assert.AreEqual("", xmlinput.GetAttributeString("q"));
                Assert.Fail("should throw");
            }
            catch (Exception e) {
                Assert.IsTrue(e is XmlFileFormatException);
            }

            int i = 0;
            bool first = true;
            while (xmlinput.FindSubElement(first, "hello", "zung", "zang", "zing")) {
                ++i;
                first = false;

                switch (xmlinput.Name) {
                    case "zing":
                        Assert.AreEqual(5, xmlinput.GetAttributeInt("why", 5));
                        Assert.AreEqual(true, xmlinput.GetAttributeBool("why", true));
                        Assert.AreEqual(5.7F, xmlinput.GetAttributeFloat("why", 5.7F));
                        Assert.AreEqual("Whazzzle", xmlinput.GetAttributeString("why", "Whazzzle"));
                        Assert.AreEqual("This is the zing", xmlinput.GetContentString());
                        break;

                    case "zang":
                        Assert.AreEqual(true, xmlinput.GetAttributeBool("r"));
                        try {
                            Assert.AreEqual(true, xmlinput.GetAttributeBool("z"));
                            Assert.Fail("should throw");
                        }
                        catch (Exception) {
                        }

                        xmlinput.Skip();
                        break;

                    case "zung":
                        xmlinput.Read();
                        xmlinput.CheckElement("diddle");
                        xmlinput.Skip();
                        xmlinput.Skip();
                        break;
                    default:
                        Assert.Fail("shouldn't get here");
                        break;
                }
            }

            Assert.AreEqual(4, i);
        }

        [TestMethod]
        public void TextReader()
        {
            string input =
    @"<?xml version=""1.0"" encoding=""utf-8""?>
<rootsym>
	<!-- This is a comment -->
	<fiddle x=""foo"" y=""1"" z=""false""></fiddle>
</rootsym>";

            XmlInput xmlinput = new XmlInput(new StringReader(input), "teststring");

            xmlinput.CheckElement("rootsym");
            xmlinput.Read();
            xmlinput.CheckElement("fiddle");
            Assert.AreEqual(1, xmlinput.GetAttributeInt("y"));
            Assert.AreEqual(false, xmlinput.GetAttributeBool("z"));
            Assert.AreEqual("foo", xmlinput.GetAttributeString("x"));
            xmlinput.Skip();
        }

#if false
        // for the Sandy symbol.
        [TestMethod]
        public void RandomPoints()
        {
            List<Point> pts = new List<Point>();
            Random rand = new Random();
            for (int i = 0; i < 50; ++i) {
            AGAIN:
                int x = rand.Next(-70, 70);
                int y = rand.Next(-70, 70);
                foreach (Point pt in pts) {
                    if ((x - pt.X)*(x-pt.X) +(y-pt.Y)*(y-pt.Y) < 18*18)
                        goto AGAIN;
                }
                Console.WriteLine(@"<filled-circle radius=""5"">");
                Console.WriteLine("\t<point x=\"{0}\" y=\"{1}\"/>", x, y);
                Console.WriteLine(@"</filled-circle>");
                pts.Add(new Point(x, y));
            }
        }
#endif

    }
}
#endif //TEST
