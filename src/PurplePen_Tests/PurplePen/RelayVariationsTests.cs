using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

using PurplePen.MapModel;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

#if TEST

namespace PurplePen.Tests
{
    [TestClass]

    public class RelayVariationsTests: TestFixtureBase
    {
        TestUI ui;
        Controller controller;
        EventDB eventDB;

        public void Setup(string filename)
        {
            ui = TestUI.Create();
            controller = ui.controller;
            eventDB = controller.GetEventDB();
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile(filename), true);
            Assert.IsTrue(success);
        }

        [TestMethod]
        public void PossiblePaths()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            var relays = new RelayVariations(eventDB, CourseId(3), 1, 6);
            Assert.AreEqual(2, relays.GetTotalPossiblePaths());

            relays = new RelayVariations(eventDB, CourseId(4), 1, 6);
            Assert.AreEqual(6, relays.GetTotalPossiblePaths());

            relays = new RelayVariations(eventDB, CourseId(5), 1, 6);
            Assert.AreEqual(6, relays.GetTotalPossiblePaths());

            relays = new RelayVariations(eventDB, CourseId(1), 1, 6);
            Assert.AreEqual(37, relays.GetTotalPossiblePaths());
        }
    }
}
#endif
