#if TEST
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.Tests
{
    using PurplePen.MapModel;

    [TestClass]
    public class ReportTests
    {
        UndoMgr undomgr;
        EventDB eventDB;

        public void Setup(string basename)
        {
            undomgr = new UndoMgr(10);
            eventDB = new EventDB(undomgr);

            eventDB.Load(TestUtil.GetTestFile(basename));
            eventDB.Validate();
        }

        [TestMethod]
        public void CourseLoad1()
        {
            Setup(@"reports\marymoor2.coursescribe");

            Reports reports = new Reports();
            string result = reports.CreateLoadReport(eventDB);

            string expectedFile = TestUtil.GetTestFile(@"reports\CourseLoad1_expected.html");
            string actualFile = TestUtil.GetTestFile(@"reports\CourseLoad1_result.html");

            File.WriteAllText(actualFile, result);
            try {
                TextFileTestUtil.CompareTextFileBaseline(actualFile, expectedFile);
            }
            finally {
                File.Delete(actualFile);
            }
        }

        [TestMethod]
        public void CourseLoad2()
        {
            Setup(@"reports\marymoor3.coursescribe");

            Reports reports = new Reports();
            string result = reports.CreateLoadReport(eventDB);

            string expectedFile = TestUtil.GetTestFile(@"reports\CourseLoad2_expected.html");
            string actualFile = TestUtil.GetTestFile(@"reports\CourseLoad2_result.html");

            File.WriteAllText(actualFile, result);
            try {
                TextFileTestUtil.CompareTextFileBaseline(actualFile, expectedFile);
            }
            finally {
                File.Delete(actualFile);
            }
        }


        [TestMethod]
        public void CourseLoad3()
        {
            Setup(@"reports\visitload.ppen");

            Reports reports = new Reports();
            string result = reports.CreateLoadReport(eventDB);

            string expectedFile = TestUtil.GetTestFile(@"reports\CourseLoad3_expected.html");
            string actualFile = TestUtil.GetTestFile(@"reports\CourseLoad3_result.html");

            File.WriteAllText(actualFile, result);
            try {
                TextFileTestUtil.CompareTextFileBaseline(actualFile, expectedFile);
            }
            finally {
                File.Delete(actualFile);
            }
        }


        [TestMethod]
        public void CrossRef()
        {
            Setup(@"reports\marymoor4.coursescribe");

            Reports reports = new Reports();
            string result = reports.CreateCrossReferenceReport(eventDB);

            string expectedFile = TestUtil.GetTestFile(@"reports\CrossRef_expected.html");
            string actualFile = TestUtil.GetTestFile(@"reports\CrossRef_result.html");

            File.WriteAllText(actualFile, result);
            try {
                TextFileTestUtil.CompareTextFileBaseline(actualFile, expectedFile);
            }
            finally {
                File.Delete(actualFile);
            }
    }

        [TestMethod]
        public void NearbyControls1()
        {
            Setup(@"reports\close1.ppen");

            Reports reports = new Reports();
            string result = reports.CreateEventAuditReport(eventDB);

            string expectedFile = TestUtil.GetTestFile(@"reports\NearbyControls1_expected.html");
            string actualFile = TestUtil.GetTestFile(@"reports\NearbyControls1_result.html");

            File.WriteAllText(actualFile, result);
            try {
                TextFileTestUtil.CompareTextFileBaseline(actualFile, expectedFile);
            }
            finally {
                File.Delete(actualFile);
            }
        }

        [TestMethod]
        public void NearbyControls2()
        {
            Setup(@"reports\close2.ppen");

            Reports reports = new Reports();
            string result = reports.CreateEventAuditReport(eventDB);

           string expectedFile = TestUtil.GetTestFile(@"reports\NearbyControls2_expected.html");
            string actualFile = TestUtil.GetTestFile(@"reports\NearbyControls2_result.html");

            File.WriteAllText(actualFile, result);
            try {
                TextFileTestUtil.CompareTextFileBaseline(actualFile, expectedFile);
            }
            finally {
                File.Delete(actualFile);
            }
        }

        [TestMethod]
        public void DuplicatedControls()
        {
            Setup(@"reports\dupcontrol.ppen");

            Reports reports = new Reports();
            string result = reports.CreateEventAuditReport(eventDB);

            string expectedFile = TestUtil.GetTestFile(@"reports\DuplicatedControls_expected.html");
            string actualFile = TestUtil.GetTestFile(@"reports\DuplicatedControls_result.html");

            File.WriteAllText(actualFile, result);
            try {
                TextFileTestUtil.CompareTextFileBaseline(actualFile, expectedFile);
            }
            finally {
                File.Delete(actualFile);
            }
        }

        [TestMethod]
        public void LegsBothDirections()
        {
            Setup(@"reports\reversal.ppen");

            Reports reports = new Reports();
            string result = reports.CreateEventAuditReport(eventDB);

            string expectedFile = TestUtil.GetTestFile(@"reports\LegsBothDirections_expected.html");
            string actualFile = TestUtil.GetTestFile(@"reports\LegsBothDirections_result.html");

            File.WriteAllText(actualFile, result);
            try {
                TextFileTestUtil.CompareTextFileBaseline(actualFile, expectedFile);
            }
            finally {
                File.Delete(actualFile);
            }
        }

        [TestMethod]
        public void EventAudit()
        {
            Setup(@"reports\marymoor6.ppen");

            Reports reports = new Reports();
            string result = reports.CreateEventAuditReport(eventDB);

            string expectedFile = TestUtil.GetTestFile(@"reports\EventAudit_expected.html");
            string actualFile = TestUtil.GetTestFile(@"reports\EventAudit_result.html");

            File.WriteAllText(actualFile, result);
            try {
                TextFileTestUtil.CompareTextFileBaseline(actualFile, expectedFile);
            }
            finally {
                File.Delete(actualFile);
            }
        }

        [TestMethod]
        public void CourseSummary()
        {
            Setup(@"reports\marymoor.coursescribe");

            Reports reports = new Reports();
            string result = reports.CreateCourseSummaryReport(eventDB);


            string expectedFile = TestUtil.GetTestFile(@"reports\CourseSummary_expected.html");
            string actualFile = TestUtil.GetTestFile(@"reports\CourseSummary_result.html");

            File.WriteAllText(actualFile, result);
            try {
                TextFileTestUtil.CompareTextFileBaseline(actualFile, expectedFile);
            }
            finally {
                File.Delete(actualFile);
            }

        }

        [TestMethod]
        public void CourseSummary2()
        {
            Setup(@"reports\relay1.ppen");

            Reports reports = new Reports();
            string result = reports.CreateCourseSummaryReport(eventDB);

            string expectedFile = TestUtil.GetTestFile(@"reports\CourseSummary2_expected.html");
            string actualFile = TestUtil.GetTestFile(@"reports\CourseSummary2_result.html");

            File.WriteAllText(actualFile, result);
            try {
                TextFileTestUtil.CompareTextFileBaseline(actualFile, expectedFile);
            }
            finally {
                File.Delete(actualFile);
            }

        }

        [TestMethod]
        public void LegLength()
        {
            Setup(@"reports\marymoor5.ppen");

            Reports reports = new Reports();
            string result = reports.CreateLegLengthReport(eventDB);

            string expectedFile = TestUtil.GetTestFile(@"reports\LegLength_expected.html");
            string actualFile = TestUtil.GetTestFile(@"reports\LegLength_result.html");

            File.WriteAllText(actualFile, result);
            try {
                TextFileTestUtil.CompareTextFileBaseline(actualFile, expectedFile);
            }
            finally {
                File.Delete(actualFile);
            }
        }

        [TestMethod]
        public void LegLength2()
        {
            Setup(@"reports\relay2.ppen");

            Reports reports = new Reports();
            string result = reports.CreateLegLengthReport(eventDB);

            string expectedFile = TestUtil.GetTestFile(@"reports\LegLength2_expected.html");
            string actualFile = TestUtil.GetTestFile(@"reports\LegLength2_result.html");

            File.WriteAllText(actualFile, result);
            try {
                TextFileTestUtil.CompareTextFileBaseline(actualFile, expectedFile);
            }
            finally {
                File.Delete(actualFile);
            }
        }

        [TestMethod]
        public void TestReport()
        {
            Setup(@"reports\marymoor.coursescribe");
            string expectedFile = TestUtil.GetTestFile(@"reports\TestReport_expected.html");
            string actualFile = TestUtil.GetTestFile(@"reports\TestReport_result.html");

            Reports reports = new Reports();
            string result = reports.CreateTestReport(eventDB);

            File.WriteAllText(actualFile, result);
            try {
                TextFileTestUtil.CompareTextFileBaseline(actualFile, expectedFile);
            }
            finally {
                File.Delete(actualFile);
            }
        }
    }
}

#endif //TEST
