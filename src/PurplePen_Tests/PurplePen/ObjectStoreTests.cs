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
using System.Xml;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.Tests
{
    [TestClass]
    public class ObjectStoreTests
    {
        class TestObject : StorableObject
        {
            public int x; 
            public string s;
            public float f;

            public TestObject()
            {
            }

            public TestObject(int x, string s, float f)
            {
                this.x = x;
                this.s = s;
                this.f = f;
            }

            public override bool Equals(object obj)
            {
                if (obj is TestObject) {
                    TestObject other = (TestObject)obj;
                    return (this.x == other.x && this.s == other.s && this.f == other.f);
                }
                else {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return x.GetHashCode() ^ s.GetHashCode() ^ f.GetHashCode();
            }

            public override string ElementName
            {
                get { return "testobject"; }
            }

            public override void ReadAttributesAndContent(XmlInput xmlinput)
            {
                x = xmlinput.GetAttributeInt("x");
                f = xmlinput.GetAttributeFloat("f");
                s = xmlinput.GetContentString();
            }

            public override void WriteAttributesAndContent(XmlTextWriter xmloutput)
            {
                xmloutput.WriteAttributeString("x", XmlConvert.ToString(x));
                xmloutput.WriteAttributeString("f", XmlConvert.ToString(f));
                xmloutput.WriteString(s);
            }

        }

        [TestMethod]
        public void AddRemove()
        {
            Id<TestObject> id;
            TestObject o, p;
            UndoMgr undomgr = new UndoMgr(5);
            ObjectStore<TestObject> objstore = new ObjectStore<TestObject>(undomgr);

            undomgr.BeginCommand(57, "Command1");

            o = new TestObject(5, "hello", 5.4F);
            id = objstore.Add(o);
            Assert.AreEqual(1, id.id);
            Assert.IsTrue(objstore.IsPresent(new Id<TestObject>(1)));
            p = objstore[new Id<TestObject>(1)];
            Assert.AreEqual(o, p);
            Assert.IsFalse(o == p);

            o = new TestObject(6, "hi", 5.4F);
            id = objstore.Add(o);
            Assert.AreEqual(2, id.id);
            Assert.IsTrue(objstore.IsPresent(new Id<TestObject>(2)));
            p = objstore[new Id<TestObject>(2)];
            Assert.AreEqual(o, p);
            Assert.IsFalse(o == p);

            objstore.Remove(new Id<TestObject>(1));
            Assert.IsFalse(objstore.IsPresent(new Id<TestObject>(1)));

            o = new TestObject(7, "xx", 1.1F);
            id = objstore.Add(o);
            Assert.AreEqual(3, id.id);
            Assert.IsTrue(objstore.IsPresent(new Id<TestObject>(3)));
            p = objstore[new Id<TestObject>(3)];
            Assert.AreEqual(o, p);
            Assert.IsFalse(o == p);

            objstore.Remove(new Id<TestObject>(3));

            int count = 0;
            foreach (TestObject x in objstore.All) {
                Assert.AreEqual(x, new TestObject(6, "hi", 5.4F));
                ++count;
            }
            Assert.AreEqual(1, count);

            undomgr.EndCommand(57);
        }

        [TestMethod]
        public void Replace()
        {
            Id<TestObject> id;
            TestObject o, p;
            UndoMgr undomgr = new UndoMgr(5);
            ObjectStore<TestObject> objstore = new ObjectStore<TestObject>(undomgr);

            undomgr.BeginCommand(57, "Command1");

            o = new TestObject(5, "hello", 5.4F);
            id = objstore.Add(o);
            Assert.AreEqual(1, id.id);
            Assert.IsTrue(objstore.IsPresent(new Id<TestObject>(1)));
            p = objstore[new Id<TestObject>(1)];
            Assert.AreEqual(o, p);
            Assert.IsFalse(o == p);

            o = new TestObject(6, "hi", 5.4F);
            id = objstore.Add(o);
            Assert.AreEqual(2, id.id);
            Assert.IsTrue(objstore.IsPresent(new Id<TestObject>(2)));
            p = objstore[new Id<TestObject>(2)];
            Assert.AreEqual(o, p);
            Assert.IsFalse(o == p);

            o = new TestObject(11, "mr ed", 9.7F);
            objstore.Replace(new Id<TestObject>(1), o);
            Assert.IsTrue(objstore.IsPresent(new Id<TestObject>(1)));
            p = objstore[new Id<TestObject>(1)];
            Assert.AreEqual(o, p);
            Assert.IsFalse(o == p);

            o = new TestObject(13, "baz", 2.4F);
            objstore.Replace(new Id<TestObject>(2), o);
            Assert.IsTrue(objstore.IsPresent(new Id<TestObject>(2)));
            p = objstore[new Id<TestObject>(2)];
            Assert.AreEqual(o, p);
            Assert.IsFalse(o == p);

            TestUtil.TestEnumerableAnyOrder(objstore.All, new TestObject[] { new TestObject(11, "mr ed", 9.7F), new TestObject(13, "baz", 2.4F) });
        }

        [TestMethod]
        public void Present()
        {
            Id<TestObject> id;
            TestObject o;
            UndoMgr undomgr = new UndoMgr(5);
            ObjectStore<TestObject> objstore = new ObjectStore<TestObject>(undomgr);

            undomgr.BeginCommand(57, "Command1");

            o = new TestObject(5, "hello", 5.4F);
            id = objstore.Add(o);
            Assert.AreEqual(1, id.id);

            Assert.IsTrue(objstore.IsPresent(new Id<TestObject>(1)));
            Assert.IsFalse(objstore.IsPresent(new Id<TestObject>(2)));

            objstore.CheckPresent(new Id<TestObject>(1));
            try {
                objstore.IsPresent(new Id<TestObject>(2));
                Assert.Fail("should throw");
            }
            catch { }
        }

        [TestMethod]
        public void UndoRedo()
        {
            UndoMgr undomgr = new UndoMgr(5);
            ObjectStore<TestObject> objstore = new ObjectStore<TestObject>(undomgr);

            undomgr.BeginCommand(57, "Command1");
            objstore.Add(new TestObject(5, "hello", 5.4F));
            objstore.Add(new TestObject(7, "hi", 3.4F));
            objstore.Add(new TestObject(9, "bat", 9.9F));
            undomgr.EndCommand(57);

            undomgr.BeginCommand(58, "Command2");
            objstore.Add(new TestObject(11, "foo", 1.4F));
            objstore.Replace(new Id<TestObject>(2), new TestObject(8, "goodbye", 9.4F));
            objstore.Remove(new Id<TestObject>(1));
            undomgr.EndCommand(58);

            TestUtil.TestEnumerableAnyOrder(objstore.All, new TestObject[] { new TestObject(9, "bat", 9.9F), new TestObject(11, "foo", 1.4F), new TestObject(8, "goodbye", 9.4F) });

            undomgr.Undo();

            TestUtil.TestEnumerableAnyOrder(objstore.All, new TestObject[] { new TestObject(5, "hello", 5.4F), new TestObject(7, "hi", 3.4F), new TestObject(9, "bat", 9.9F) });

            undomgr.Undo();

            TestUtil.TestEnumerableAnyOrder(objstore.All, new TestObject[] { });

            undomgr.Redo();

            TestUtil.TestEnumerableAnyOrder(objstore.All, new TestObject[] { new TestObject(5, "hello", 5.4F), new TestObject(7, "hi", 3.4F), new TestObject(9, "bat", 9.9F) });

            undomgr.Redo();

            TestUtil.TestEnumerableAnyOrder(objstore.All, new TestObject[] { new TestObject(9, "bat", 9.9F), new TestObject(11, "foo", 1.4F), new TestObject(8, "goodbye", 9.4F) });
        }

        [TestMethod]
        public void AllPairs()
        {
            UndoMgr undomgr = new UndoMgr(5);
            ObjectStore<TestObject> objstore = new ObjectStore<TestObject>(undomgr);

            undomgr.BeginCommand(57, "Command1");
            objstore.Add(new TestObject(5, "hello", 5.4F));
            objstore.Add(new TestObject(7, "hi", 3.4F));
            objstore.Add(new TestObject(9, "bat", 9.9F));
            undomgr.EndCommand(57);

            undomgr.BeginCommand(58, "Command2");
            objstore.Add(new TestObject(11, "foo", 1.4F));
            objstore.Replace(new Id<TestObject>(2), new TestObject(8, "goodbye", 9.4F));
            objstore.Remove(new Id<TestObject>(1));
            undomgr.EndCommand(58);

            TestUtil.TestEnumerableAnyOrder(objstore.AllPairs, new KeyValuePair<Id<TestObject>, TestObject>[] {
                                        new KeyValuePair<Id<TestObject>, TestObject>(new Id<TestObject>(3), new TestObject(9, "bat", 9.9F)), 
                                        new KeyValuePair<Id<TestObject>, TestObject>(new Id<TestObject>(4), new TestObject(11, "foo", 1.4F)), 
                                        new KeyValuePair<Id<TestObject>, TestObject>(new Id<TestObject>(2), new TestObject(8, "goodbye", 9.4F)) });
        }

        [TestMethod]
        public void Save()
        {
            UndoMgr undomgr = new UndoMgr(5);
            ObjectStore<TestObject> objstore = new ObjectStore<TestObject>(undomgr);

            undomgr.BeginCommand(57, "Command1");
            objstore.Add(new TestObject(5, "hello", 5.4F));
            objstore.Add(new TestObject(7, "hi", 3.4F));
            objstore.Add(new TestObject(9, "bat", 9.9F));
            undomgr.EndCommand(57);

            undomgr.BeginCommand(58, "Command2");
            objstore.Add(new TestObject(11, "foo", 1.4F));
            objstore.Replace(new Id<TestObject>(2), new TestObject(8, "goodbye", 9.4F));
            objstore.Remove(new Id<TestObject>(1));
            undomgr.EndCommand(58);

            TextWriter writer = new StringWriter();
            XmlTextWriter xmloutput = new XmlTextWriter(writer);
            xmloutput.Formatting = Formatting.Indented;
            xmloutput.Namespaces = false;

            xmloutput.WriteStartDocument();
            xmloutput.WriteStartElement("testobjects");

            objstore.Save(xmloutput);

            xmloutput.WriteEndElement();
            xmloutput.WriteEndDocument();
            xmloutput.Close();

            Assert.AreEqual(
@"<?xml version=""1.0"" encoding=""utf-16""?>
<testobjects>
  <testobject id=""2"" x=""8"" f=""9.4"">goodbye</testobject>
  <testobject id=""3"" x=""9"" f=""9.9"">bat</testobject>
  <testobject id=""4"" x=""11"" f=""1.4"">foo</testobject>
</testobjects>",
            writer.ToString());

        }

        [TestMethod]
        public void Load()
        {
            UndoMgr undomgr = new UndoMgr(5);
            ObjectStore<TestObject> objstore = new ObjectStore<TestObject>(undomgr);

            string xmlText =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<testobjects>
  <testobject id=""2"" x=""8"" f=""9.4"">goodbye</testobject>
  <testobject id=""3"" x=""9"" f=""9.9"">bat</testobject>
  <testobject id=""4"" x=""11"" f=""1.4"">foo</testobject>
</testobjects>";

            XmlInput xmlinput = new XmlInput(new StringReader(xmlText), "testfile");

            xmlinput.CheckElement("testobjects");
            xmlinput.Read();

            objstore.Load(xmlinput);
            xmlinput.Dispose();

            TestUtil.TestEnumerableAnyOrder(objstore.AllPairs, 
                                        new KeyValuePair<Id<TestObject>, TestObject>[] {
                                        new KeyValuePair<Id<TestObject>, TestObject>(new Id<TestObject>(3), new TestObject(9, "bat", 9.9F)), 
                                        new KeyValuePair<Id<TestObject>, TestObject>(new Id<TestObject>(4), new TestObject(11, "foo", 1.4F)), 
                                        new KeyValuePair<Id<TestObject>, TestObject>(new Id<TestObject>(2), new TestObject(8, "goodbye", 9.4F)) });
        }

        [TestMethod]
        public void LoadModifySave()
        {
            Id<TestObject> id;
            UndoMgr undomgr = new UndoMgr(5);
            ObjectStore<TestObject> objstore = new ObjectStore<TestObject>(undomgr);

            string xmlText =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<testobjects>
  <testobject id=""2"" x=""8"" f=""9.4"">goodbye</testobject>
  <testobject id=""4"" x=""11"" f=""1.4"">foo</testobject>
</testobjects>";

            XmlInput xmlinput = new XmlInput(new StringReader(xmlText), "testfile");

            xmlinput.CheckElement("testobjects");
            xmlinput.Read();

            objstore.Load(xmlinput);
            xmlinput.Dispose();

            undomgr.BeginCommand(57, "Command1");
            id = objstore.Add(new TestObject(5, "hello", 5.4F));
            Assert.AreEqual(5, id.id);
            id = objstore.Add(new TestObject(9, "bat", 9.9F));
            Assert.AreEqual(6, id.id);
            undomgr.EndCommand(57);

            undomgr.BeginCommand(58, "Command2");
            objstore.Replace(new Id<TestObject>(2), new TestObject(-9, "elvis", 9.1F));
            objstore.Remove(new Id<TestObject>(4));
            undomgr.EndCommand(58);

            TextWriter writer = new StringWriter();
            XmlTextWriter xmloutput = new XmlTextWriter(writer);
            xmloutput.Formatting = Formatting.Indented;
            xmloutput.Namespaces = false;

            xmloutput.WriteStartDocument();
            xmloutput.WriteStartElement("testobjects");

            objstore.Save(xmloutput);

            xmloutput.WriteEndElement();
            xmloutput.WriteEndDocument();
            xmloutput.Close();

            Assert.AreEqual(
@"<?xml version=""1.0"" encoding=""utf-16""?>
<testobjects>
  <testobject id=""2"" x=""-9"" f=""9.1"">elvis</testobject>
  <testobject id=""5"" x=""5"" f=""5.4"">hello</testobject>
  <testobject id=""6"" x=""9"" f=""9.9"">bat</testobject>
</testobjects>",
            writer.ToString());
        }

        [TestMethod]
        public void ChangeNum()
        {
            int changeNum;

            Id<TestObject> id;
            TestObject o;
            UndoMgr undomgr = new UndoMgr(5);
            ObjectStore<TestObject> objstore = new ObjectStore<TestObject>(undomgr);

            changeNum = objstore.ChangeNum;
            undomgr.BeginCommand(57, "Command1");

            o = new TestObject(5, "hello", 5.4F);
            id = objstore.Add(o);
            Assert.IsTrue(changeNum < objstore.ChangeNum);
            changeNum = objstore.ChangeNum;

            objstore.Remove(new Id<TestObject>(1));
            Assert.IsTrue(changeNum < objstore.ChangeNum);
            changeNum = objstore.ChangeNum;

            id = objstore.Add(o);
            Assert.IsTrue(changeNum < objstore.ChangeNum);
            changeNum = objstore.ChangeNum;

            objstore.Replace(new Id<TestObject>(2), o);
            Assert.IsTrue(changeNum < objstore.ChangeNum);
            changeNum = objstore.ChangeNum;

            undomgr.EndCommand(57);

            undomgr.Undo();
            Assert.IsTrue(changeNum < objstore.ChangeNum);
            changeNum = objstore.ChangeNum;

            undomgr.Redo();
            Assert.IsTrue(changeNum < objstore.ChangeNum);
            changeNum = objstore.ChangeNum;
        }
    }
}

#endif //TEST
