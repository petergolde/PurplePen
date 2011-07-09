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
using System.Diagnostics;
using System.Globalization;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.Tests
{
    [TestClass]
    public class CompatibilityTests: TestFixtureBase
    {
        MainFrame mainFrame;
        Controller controller;

        void LoadInitialFile(string filename)
        {
            mainFrame = new MainFrame();
            controller = new Controller(mainFrame);

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile(filename), true);
            Assert.IsTrue(success);

            controller.GetEventDB().Validate();

            // Start the UI
            mainFrame.Show();
        }

        void CloseMainFrame()
        {
            mainFrame.Dispose();
            mainFrame = null;
        }

        // Test loading a file.
        void TestLoadFile(string filename)
        {
            LoadInitialFile(filename);
            Application.DoEvents();
            Application.RaiseIdle(EventArgs.Empty);
            controller.GetEventDB().Validate();

            CloseMainFrame();
        }

        [TestMethod]
        public void Version100beta1()
        {
            TestLoadFile("compatibility\\Sample Event_100b1.ppen");
        }


        [TestMethod]
        public void Version100beta2()
        {
            TestLoadFile("compatibility\\Sample Event_100b2.ppen");

            // Make sure gaps converted correctly.
            EventDB eventDB = controller.GetEventDB();
            ControlPoint control = eventDB.GetControl(ControlId(3));
            CollectionAssert.AreEquivalent(
                new KeyValuePair<int, uint>[] {
                    new KeyValuePair<int,uint>(10000, 0x1FFFFF80),
                    new KeyValuePair<int,uint>(15000, 0x1FFFFF80),
                },
                control.gaps);


            // Make sure all controls scale, description kind is correct by default.
            Assert.AreEqual(15000, eventDB.GetEvent().allControlsPrintScale);
            Assert.AreEqual(DescriptionKind.Symbols, eventDB.GetEvent().allControlsDescKind);
        }

        [TestMethod]
        public void Version101()
        {
            TestLoadFile("compatibility\\Sample Event_101.ppen");
            // Make sure first ordinal is correct.
            // Make sure label kind is correct.
            EventDB eventDB = controller.GetEventDB();
            Course course = eventDB.GetCourse(CourseId(3));
            Assert.AreEqual(1, course.firstControlOrdinal);
            Assert.AreEqual(ControlLabelKind.Sequence, course.labelKind);
            Assert.AreEqual(-1, course.scoreColumn);
            course = eventDB.GetCourse(CourseId(6));
            Assert.AreEqual(ControlLabelKind.Code, course.labelKind);
            Assert.AreEqual(0, course.scoreColumn);
        }

        [TestMethod]
        public void OldStyleCustomText()
        {
            TestLoadFile("compatibility\\customtext.ppen");

            // Make sure the custom text is right.
            EventDB eventDB = controller.GetEventDB();
            Event ev = eventDB.GetEvent();

            Assert.AreEqual(5, ev.customSymbolKey.Count);
            Assert.AreEqual(5, ev.customSymbolText.Count);

            Assert.AreEqual(true, ev.customSymbolKey["6.1"]);
            Assert.AreEqual(true, ev.customSymbolKey["5.6"]);
            Assert.AreEqual(false, ev.customSymbolKey["8.7"]);

            Assert.AreEqual(1, ev.customSymbolText["6.2"].Count);
            Assert.AreEqual("en", ev.customSymbolText["6.2"][0].Lang);
            Assert.AreEqual(false, ev.customSymbolText["6.2"][0].Plural);
            Assert.AreEqual("", ev.customSymbolText["6.2"][0].Gender);
            Assert.AreEqual("playground equipment", ev.customSymbolText["6.2"][0].Text);

            Assert.AreEqual(1, ev.customSymbolText["8.7"].Count);
            Assert.AreEqual("en", ev.customSymbolText["8.7"][0].Lang);
            Assert.AreEqual(false, ev.customSymbolText["8.7"][0].Plural);
            Assert.AreEqual("", ev.customSymbolText["8.7"][0].Gender);
            Assert.AreEqual("wet {0}", ev.customSymbolText["8.7"][0].Text);
        }

    }
}

#endif //TEST
