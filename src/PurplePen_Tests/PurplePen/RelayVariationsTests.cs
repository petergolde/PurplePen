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

        void DumpAssignment(RelayVariations relayAssignment, string fileName)
        {
            using (TextWriter writer = new StreamWriter(fileName)) {
                for (int team = 1; team <= relayAssignment.NumberOfTeams; ++team) {
                    writer.Write("Team {0,3}: \t", team);
                    for (int leg = 1; leg <= relayAssignment.NumberOfLegs; ++leg) {
                        if (leg != 0)
                            writer.Write("\t");
                        writer.Write("{0}", relayAssignment.GetVariation(team, leg).VariationCodeString);
                    }
                    writer.WriteLine();
                }
            }
        }

        void ValidateRelayVariationsTest(RelayVariations relayAsignment, string baselineName)
        {
            string baselineFileName = TestUtil.GetTestFile(baselineName + ".txt");
            string tempFileName = TestUtil.GetTestFile(baselineName + "_temp.txt");
            DumpAssignment(relayAsignment, tempFileName);
            TestUtil.CompareTextFileBaseline(tempFileName, baselineFileName);
            File.Delete(tempFileName);
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

        [TestMethod]
        public void GenerateAssignment1()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            var teamAssignment = new RelayVariations(eventDB, CourseId(3), 64, 6);
            ValidateRelayVariationsTest(teamAssignment, "relay\\twowayfork");
        }
    }
}
#endif
