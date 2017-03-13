using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

using PurplePen.MapModel;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;
using System.Linq;

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
                        writer.Write("{0}", relayAssignment.GetVariation(team, leg).CodeString);
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

            var relays = new RelayVariations(eventDB, CourseId(3), new RelaySettings(1, 6));
            Assert.AreEqual(2, relays.GetTotalPossiblePaths());

            relays = new RelayVariations(eventDB, CourseId(4), new RelaySettings(1, 6));
            Assert.AreEqual(6, relays.GetTotalPossiblePaths());

            relays = new RelayVariations(eventDB, CourseId(5), new RelaySettings(1, 6));
            Assert.AreEqual(6, relays.GetTotalPossiblePaths());

            relays = new RelayVariations(eventDB, CourseId(1), new RelaySettings(1, 6));
            Assert.AreEqual(37, relays.GetTotalPossiblePaths());
        }

        [TestMethod]
        public void GenerateAssignment1()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            var teamAssignment = new RelayVariations(eventDB, CourseId(3), new RelaySettings(64, 6));
            ValidateRelayVariationsTest(teamAssignment, "relay\\twowayfork");
        }

        [TestMethod]
        public void GenerateAssignment2()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            var teamAssignment = new RelayVariations(eventDB, CourseId(5), new RelaySettings(64, 6));
            ValidateRelayVariationsTest(teamAssignment, "relay\\threeloop");
        }

        [TestMethod]
        public void GenerateAssignment3()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            var teamAssignment = new RelayVariations(eventDB, CourseId(4), new RelaySettings(64, 6));
            ValidateRelayVariationsTest(teamAssignment, "relay\\doublebranch");
        }

        [TestMethod]
        public void GenerateAssignment4()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            var teamAssignment = new RelayVariations(eventDB, CourseId(6), new RelaySettings(64, 6));
            ValidateRelayVariationsTest(teamAssignment, "relay\\loopwithbranches");
        }
        
        
        [TestMethod]
        public void GenerateAssignment5()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            var teamAssignment = new RelayVariations(eventDB, CourseId(7), new RelaySettings(64, 5));
            ValidateRelayVariationsTest(teamAssignment, "relay\\nestedbranches");
        }

        [TestMethod]
        public void GenerateAssignment6()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            var teamAssignment = new RelayVariations(eventDB, CourseId(1), new RelaySettings(64, 5));
            ValidateRelayVariationsTest(teamAssignment, "relay\\complexuneven");
        }

        [TestMethod]
        public void FixedBranches1()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            FixedBranchAssignments fixedBranchAssignments = new FixedBranchAssignments();
            fixedBranchAssignments.AddBranchAssignment('A', 3);
            fixedBranchAssignments.AddBranchAssignment('A', 1);
            var teamAssignment = new RelayVariations(eventDB, CourseId(8), new RelaySettings(30, 5, fixedBranchAssignments));
            ValidateRelayVariationsTest(teamAssignment, "relay\\twowayfixed");
        }

        [TestMethod]
        public void FixedBranches2()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            FixedBranchAssignments fixedBranchAssignments = new FixedBranchAssignments();
            fixedBranchAssignments.AddBranchAssignment('A', 3);
            fixedBranchAssignments.AddBranchAssignment('A', 1);
            fixedBranchAssignments.AddBranchAssignment('B', 2);
            fixedBranchAssignments.AddBranchAssignment('B', 0);
            fixedBranchAssignments.AddBranchAssignment('A', 4);
            var teamAssignment = new RelayVariations(eventDB, CourseId(7), new RelaySettings(30, 5, fixedBranchAssignments));
            ValidateRelayVariationsTest(teamAssignment, "relay\\nestedfixed");
        }

        [TestMethod]
        public void FixedBranches3()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            FixedBranchAssignments fixedBranchAssignments = new FixedBranchAssignments();
            fixedBranchAssignments.AddBranchAssignment('A', 3);
            fixedBranchAssignments.AddBranchAssignment('A', 1);
            fixedBranchAssignments.AddBranchAssignment('A', 4);
            var teamAssignment = new RelayVariations(eventDB, CourseId(7), new RelaySettings(30, 5, fixedBranchAssignments));
            ValidateRelayVariationsTest(teamAssignment, "relay\\nestedfixed2");
        }

        [TestMethod]
        public void FixedBranches4()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            FixedBranchAssignments fixedBranchAssignments = new FixedBranchAssignments();
            fixedBranchAssignments.AddBranchAssignment('A', 3);
            fixedBranchAssignments.AddBranchAssignment('A', 1);
            fixedBranchAssignments.AddBranchAssignment('A', 4);
            fixedBranchAssignments.AddBranchAssignment('A', 0);
            var teamAssignment = new RelayVariations(eventDB, CourseId(7), new RelaySettings(30, 5, fixedBranchAssignments));
            ValidateRelayVariationsTest(teamAssignment, "relay\\nestedfixed3");
        }

        [TestMethod]
        public void FixedBranches5()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            FixedBranchAssignments fixedBranchAssignments = new FixedBranchAssignments();
            fixedBranchAssignments.AddBranchAssignment('B', 0);
            var teamAssignment = new RelayVariations(eventDB, CourseId(7), new RelaySettings(30, 5, fixedBranchAssignments));
            ValidateRelayVariationsTest(teamAssignment, "relay\\nestedfixed4");
        }

        [TestMethod]
        public void FixedBranches6()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            FixedBranchAssignments fixedBranchAssignments = new FixedBranchAssignments();
            fixedBranchAssignments.AddBranchAssignment('B', 3);
            var teamAssignment = new RelayVariations(eventDB, CourseId(9), new RelaySettings(30, 5, fixedBranchAssignments));
            ValidateRelayVariationsTest(teamAssignment, "relay\\fivewayfixed");
        }

        [TestMethod]
        public void FixedBranches7()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            FixedBranchAssignments fixedBranchAssignments = new FixedBranchAssignments();
            fixedBranchAssignments.AddBranchAssignment('B', 6);
            var teamAssignment = new RelayVariations(eventDB, CourseId(1), new RelaySettings(30, 7, fixedBranchAssignments));
            ValidateRelayVariationsTest(teamAssignment, "relay\\complexfixed");
        }

        [TestMethod]
        public void FixedBranches8()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            FixedBranchAssignments fixedBranchAssignments = new FixedBranchAssignments();
            fixedBranchAssignments.AddBranchAssignment('B', 6);
            fixedBranchAssignments.AddBranchAssignment('B', 0);
            var teamAssignment = new RelayVariations(eventDB, CourseId(1), new RelaySettings(30, 7, fixedBranchAssignments));
            ValidateRelayVariationsTest(teamAssignment, "relay\\complexfixed2");
        }



        [TestMethod]
        public void BranchWarnings1()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            var teamAssignment = new RelayVariations(eventDB, CourseId(3), new RelaySettings(64, 5));
            var warnings = teamAssignment.GetBranchWarnings().ToArray();
            Assert.AreEqual(1, warnings.Length);
            Assert.AreEqual(3, warnings[0].numMore);
            CollectionAssert.AreEquivalent(new[] { 'A' }, warnings[0].codeMore);
            Assert.AreEqual(2, warnings[0].numLess);
            CollectionAssert.AreEquivalent(new[] { 'B' }, warnings[0].codeLess);
        }

        [TestMethod]
        public void BranchWarnings2()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            var teamAssignment = new RelayVariations(eventDB, CourseId(4), new RelaySettings(64, 5));
            var warnings = teamAssignment.GetBranchWarnings().ToArray();
            Assert.AreEqual(2, warnings.Length);
            Assert.AreEqual(3, warnings[0].numMore);
            CollectionAssert.AreEquivalent(new[] { 'A' }, warnings[0].codeMore);
            Assert.AreEqual(2, warnings[0].numLess);
            CollectionAssert.AreEquivalent(new[] { 'B' }, warnings[0].codeLess);
            Assert.AreEqual(2, warnings[1].numMore);
            CollectionAssert.AreEquivalent(new[] { 'C', 'D' }, warnings[1].codeMore);
            Assert.AreEqual(1, warnings[1].numLess);
            CollectionAssert.AreEquivalent(new[] { 'E' }, warnings[1].codeLess);
        }

        [TestMethod]
        public void BranchWarnings3()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            var teamAssignment = new RelayVariations(eventDB, CourseId(7), new RelaySettings(64, 4));
            var warnings = teamAssignment.GetBranchWarnings().ToArray();
            Assert.AreEqual(1, warnings.Length);
            Assert.AreEqual(1, warnings[0].numMore);
            CollectionAssert.AreEquivalent(new[] { 'E', 'F' }, warnings[0].codeMore);
            Assert.AreEqual(0, warnings[0].numLess);
            CollectionAssert.AreEquivalent(new[] { 'G' }, warnings[0].codeLess);
        }

        [TestMethod]
        public void BranchWarnings4()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));

            var teamAssignment = new RelayVariations(eventDB, CourseId(7), new RelaySettings(64, 5));
            var warnings = teamAssignment.GetBranchWarnings().ToArray();
            Assert.AreEqual(1, warnings.Length);
            Assert.AreEqual(3, warnings[0].numMore);
            CollectionAssert.AreEquivalent(new[] { 'A' }, warnings[0].codeMore);
            Assert.AreEqual(2, warnings[0].numLess);
            CollectionAssert.AreEquivalent(new[] { 'B' }, warnings[0].codeLess);
        }

        [TestMethod]
        public void ExportCsv()
        {
            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));
            string tempOutputFile = TestUtil.GetTestFile("relay\\doublebranch_temp.csv");
            string baselineFile = TestUtil.GetTestFile("relay\\doublebranch_baseline.csv");

            var teamAssignment = new RelayVariations(eventDB, CourseId(4), new RelaySettings(143, 6));
            var csvWriter = new CsvWriter();
            csvWriter.WriteCsv(tempOutputFile, teamAssignment);

            TestUtil.CompareTextFileBaseline(tempOutputFile, baselineFile);

            File.Delete(tempOutputFile);
        }

        [TestMethod]
        public void ExportXml()
        {
            Dictionary<string, string> exceptions = ExportXmlVersion3.TestFileExceptionMap();

            Setup(TestUtil.GetTestFile("relay\\relay.ppen"));
            string tempOutputFile = TestUtil.GetTestFile("relay\\doublebranch_temp.xml");
            string baselineFile = TestUtil.GetTestFile("relay\\doublebranch_baseline.xml");

            var teamAssignment = new RelayVariations(eventDB, CourseId(4), new RelaySettings(143, 6));
            var xmlExporter = new ExportRelayVariations3();
            xmlExporter.WriteFullXml(tempOutputFile, teamAssignment, eventDB, CourseId(4));

            TestUtil.CompareTextFileBaseline(tempOutputFile, baselineFile, exceptions);

            File.Delete(tempOutputFile);
        }





    }
}
#endif
