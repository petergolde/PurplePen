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
using System.Threading.Tasks;

namespace PurplePen.Tests
{
    [TestClass]
    public sealed class CompatibilityTests: TestFixtureBase, IDisposable
    {
        MainFrame mainFrame;
        Controller controller;

        async Task LoadInitialFile(string filename)
        {
            mainFrame = new MainFrame();
            controller = new Controller(mainFrame);

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile(filename), true);
            Assert.IsTrue(success);

            controller.GetEventDB().Validate();

            // Start the UI
            mainFrame.Show();
        }

        private void Dispose(bool disposing)
        {
                if (disposing) {
                    mainFrame?.Dispose();
                    mainFrame = null;
                    controller?.Dispose();
                    controller = null;
                }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
        }

        void CloseMainFrame()
        {
            mainFrame.Dispose();
            mainFrame = null;
        }

        // Test loading a file.
        async Task TestLoadFile(string filename)
        {
            await LoadInitialFile(filename);
            Application.DoEvents();
            Application.RaiseIdle(EventArgs.Empty);
            controller.GetEventDB().Validate();

            CloseMainFrame();
        }

        [TestMethod]
        public async Task Version100beta1()
        {
            await TestLoadFile("compatibility\\Sample Event_100b1.ppen");
        }


        [TestMethod]
        public async Task Version100beta2()
        {
            await TestLoadFile("compatibility\\Sample Event_100b2.ppen");

            // Make sure gaps converted correctly.
            EventDB eventDB = controller.GetEventDB();
            ControlPoint control = eventDB.GetControl(ControlId(3));
            Assert.AreEqual(2, control.gaps.Count);
            CollectionAssert.AreEqual(control.gaps[10000], CircleGap.ComputeCircleGaps(0x1FFFFF80));
            CollectionAssert.AreEqual(control.gaps[15000], CircleGap.ComputeCircleGaps(0x1FFFFF80));

            // Make sure all controls scale, description kind is correct by default.
            Assert.AreEqual(15000, eventDB.GetEvent().allControlsPrintScale);
            Assert.AreEqual(DescriptionKind.Symbols, eventDB.GetEvent().allControlsDescKind);
        }

        [TestMethod]
        public async Task Version101()
        {
            await TestLoadFile("compatibility\\Sample Event_101.ppen");
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
            Assert.IsFalse(course.hideFromReports);
        }

        [TestMethod]
        public async Task OldStyleCustomText()
        {
            await TestLoadFile("compatibility\\customtext.ppen");

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
