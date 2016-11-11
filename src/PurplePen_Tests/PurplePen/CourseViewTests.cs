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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.Tests
{
    using System.Linq;
    using PurplePen.MapModel;

    [TestClass]
    public class CourseViewTests: TestFixtureBase
    {
        internal void DumpCourseView(CourseView courseView, TextWriter writer)
        {
            writer.Write("Name='{0}', Kind='{1}', CourseId={2}", courseView.CourseName, courseView.Kind, courseView.BaseCourseId);
            if (! courseView.CourseDesignator.AllParts)
                writer.Write(", Part={0}", courseView.CourseDesignator.Part);
            writer.WriteLine();

            writer.WriteLine("Total Length={0}  Part Length={1}  Total Climb={2}  ScoreColumn={3}  Total Score={4}  Total Controls={5}", 
                             courseView.MaxTotalLength, courseView.PartLength, courseView.TotalClimb, courseView.ScoreColumn, courseView.TotalScore, courseView.TotalNormalControls);

            for (int i = 0; i < courseView.ControlViews.Count; ++i) {
                CourseView.ControlView controlView = courseView.ControlViews[i];

                writer.Write("{0,2}: [{1,2}] Ids:{2,3}", i, controlView.ordinal, controlView.controlId, controlView.courseControlIds[0]);
                for (int j = 0; j < controlView.courseControlIds.Length; ++j)
                    writer.Write(",{0,3}", controlView.courseControlIds[j]);
                if (controlView.hiddenControl)
                    writer.Write(" hidden");
                writer.WriteLine();

                if (controlView.legTo != null) {
                    writer.Write("    Legs: ");
                    for (int j = 0; j < controlView.legTo.Length; ++j)
                        writer.Write("(Next:{0},Id:{1},length:{2})  ", controlView.legTo[j], controlView.legId[j], controlView.legLength[j]);
                    writer.WriteLine();
                }
            }

            for (int i = 0; i < courseView.SpecialIds.Count; ++i) {
                writer.WriteLine("Special {0} ({1})", courseView.SpecialIds[i], courseView.EventDB.GetSpecial(courseView.SpecialIds[i]).kind);
            }

            for (int i = 0; i < courseView.DescriptionViews.Count; ++i) {
                writer.WriteLine("Description {0} ({1})", courseView.DescriptionViews[i].SpecialId, courseView.DescriptionViews[i].CourseDesignator);
            }

            for (int i = 0; i < courseView.ExtraCourseControls.Count; ++i) {
                writer.WriteLine("Extra course control {0}", courseView.ExtraCourseControls[i].id);
            }
        }

        [TestMethod]
        public void DisplayAllCourseViews()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));

            eventDB.Load(TestUtil.GetTestFile("courseview\\sampleevent1.coursescribe"));
            eventDB.Validate();

            foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB)) {
                CourseDesignator designator;
                if (QueryEvent.HasVariations(eventDB, courseId)) {
                    var variationPath = QueryEvent.GetAllVariations(eventDB, courseId).First().Path;
                    designator = new CourseDesignator(courseId, variationPath);
                }
                else {
                    designator = new CourseDesignator(courseId);
                }

                CourseView courseView = CourseView.CreateViewingCourseView(eventDB, designator);
                DumpCourseView(courseView, Console.Out);
                Console.WriteLine();
            }
        }

        [TestMethod]
        public void StandardCourseView()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            StringWriter writer = new StringWriter();
            string actual, expected;

            eventDB.Load(TestUtil.GetTestFile("courseview\\sampleevent1.coursescribe"));
            eventDB.Validate();

            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(4)));
            DumpCourseView(courseView, writer);
            actual = writer.ToString();
            expected =
@"Name='SampleCourse4', Kind='Normal', CourseId=4
Total Length=4667.309  Part Length=4667.309  Total Climb=173  ScoreColumn=-1  Total Score=0  Total Controls=4
 0: [ 0] Ids:  1, 11
    Legs: (Next:1,Id:0,length:340.1033)  
 1: [ 1] Ids: 11, 12
    Legs: (Next:2,Id:0,length:537.7557)  
 2: [-1] Ids: 22, 13
    Legs: (Next:3,Id:0,length:245.7254)  
 3: [-1] Ids:  3, 14
    Legs: (Next:4,Id:0,length:112.54)  
 4: [ 2] Ids:  4, 15
    Legs: (Next:5,Id:0,length:280.2271)  
 5: [-1] Ids: 15, 16
    Legs: (Next:6,Id:0,length:287.0649)  
 6: [ 3] Ids:  5, 17
    Legs: (Next:7,Id:0,length:1440)  
 7: [ 4] Ids: 18, 18
    Legs: (Next:8,Id:0,length:1423.892)  
 8: [-1] Ids:  6, 19
Special 1 (FirstAid)
Special 4 (OOB)
Description 6 (Course 4)
";
            Assert.AreEqual(expected, actual);
        }


        [TestMethod]
        public void StandardCourseViewWithCustomFirstNumber()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            StringWriter writer = new StringWriter();
            string actual, expected;

            eventDB.Load(TestUtil.GetTestFile("courseview\\sampleevent2.coursescribe"));
            eventDB.Validate();

            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, Designator(4));
            DumpCourseView(courseView, writer);
            actual = writer.ToString();
            expected =
@"Name='SampleCourse4', Kind='Normal', CourseId=4
Total Length=4667.309  Part Length=4667.309  Total Climb=173  ScoreColumn=-1  Total Score=0  Total Controls=4
 0: [ 0] Ids:  1, 11
    Legs: (Next:1,Id:0,length:340.1033)  
 1: [ 3] Ids: 11, 12
    Legs: (Next:2,Id:0,length:537.7557)  
 2: [-1] Ids: 22, 13
    Legs: (Next:3,Id:0,length:245.7254)  
 3: [-1] Ids:  3, 14
    Legs: (Next:4,Id:0,length:112.54)  
 4: [ 4] Ids:  4, 15
    Legs: (Next:5,Id:0,length:280.2271)  
 5: [-1] Ids: 15, 16
    Legs: (Next:6,Id:0,length:287.0649)  
 6: [ 5] Ids:  5, 17
    Legs: (Next:7,Id:0,length:1440)  
 7: [ 6] Ids: 18, 18
    Legs: (Next:8,Id:0,length:1423.892)  
 8: [-1] Ids:  6, 19
Special 1 (FirstAid)
Special 4 (OOB)
Description 6 (Course 4)
";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DescriptionSpecialsOnly()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            StringWriter writer = new StringWriter();
            string actual, expected;

            eventDB.Load(TestUtil.GetTestFile("courseview\\sampleevent1.coursescribe"));
            eventDB.Validate();

            CourseView courseView = CourseView.CreatePositioningCourseView(eventDB, new CourseDesignator(CourseId(4)));
            DumpCourseView(courseView, writer);
            actual = writer.ToString();
            expected =
@"Name='SampleCourse4', Kind='Normal', CourseId=4
Total Length=4667.309  Part Length=4667.309  Total Climb=173  ScoreColumn=-1  Total Score=0  Total Controls=4
 0: [ 0] Ids:  1, 11
    Legs: (Next:1,Id:0,length:340.1033)  
 1: [ 1] Ids: 11, 12
    Legs: (Next:2,Id:0,length:537.7557)  
 2: [-1] Ids: 22, 13
    Legs: (Next:3,Id:0,length:245.7254)  
 3: [-1] Ids:  3, 14
    Legs: (Next:4,Id:0,length:112.54)  
 4: [ 2] Ids:  4, 15
    Legs: (Next:5,Id:0,length:280.2271)  
 5: [-1] Ids: 15, 16
    Legs: (Next:6,Id:0,length:287.0649)  
 6: [ 3] Ids:  5, 17
    Legs: (Next:7,Id:0,length:1440)  
 7: [ 4] Ids: 18, 18
    Legs: (Next:8,Id:0,length:1423.892)  
 8: [-1] Ids:  6, 19
Description 6 (Course 4)
";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ScoreCourseView()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            StringWriter writer = new StringWriter();
            string actual, expected;

            eventDB.Load(TestUtil.GetTestFile("courseview\\sampleevent1.coursescribe"));
            eventDB.Validate();

            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(5)));
            DumpCourseView(courseView, writer);
            actual = writer.ToString();
            expected =
@"Name='Score 4', Kind='Score', CourseId=5
Total Length=0  Part Length=0  Total Climb=-1  ScoreColumn=0  Total Score=155  Total Controls=11
 0: [-1] Ids:  1,101
 1: [-1] Ids: 17,109
 2: [-1] Ids:  2,113
 3: [-1] Ids:  7,114
 4: [-1] Ids: 11,102
 5: [-1] Ids:  8,115
 6: [-1] Ids: 20,112
 7: [-1] Ids:  5,107
 8: [-1] Ids:  4,105
 9: [-1] Ids: 16,108
10: [-1] Ids: 18,110
11: [-1] Ids: 19,111
12: [-1] Ids:  6,116
13: [-1] Ids:  3,104
14: [-1] Ids: 15,106
15: [-1] Ids: 22,103
Special 1 (FirstAid)
Special 3 (Boundary)
Special 4 (OOB)
";

            
            Assert.AreEqual(expected, actual);


        }

        [TestMethod]
        public void ScoreCourseViewWithOrdinals() {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            StringWriter writer = new StringWriter();
            string actual, expected;

            eventDB.Load(TestUtil.GetTestFile("courseview\\sampleevent3.coursescribe"));
            eventDB.Validate();

            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, Designator(5));
            DumpCourseView(courseView, writer);
            actual = writer.ToString();
            expected =
@"Name='Score 4', Kind='Score', CourseId=5
Total Length=0  Part Length=0  Total Climb=-1  ScoreColumn=1  Total Score=155  Total Controls=11
 0: [-1] Ids:  1,101
 1: [ 1] Ids: 17,109
 2: [ 2] Ids:  2,113
 3: [ 3] Ids:  7,114
 4: [ 4] Ids: 11,102
 5: [ 5] Ids:  8,115
 6: [ 6] Ids: 20,112
 7: [ 7] Ids:  5,107
 8: [ 8] Ids:  4,105
 9: [ 9] Ids: 16,108
10: [10] Ids: 18,110
11: [11] Ids: 19,111
12: [-1] Ids:  6,116
13: [-1] Ids:  3,104
14: [-1] Ids: 15,106
15: [-1] Ids: 22,103
Special 1 (FirstAid)
Special 3 (Boundary)
Special 4 (OOB)
";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ScoreCourseViewWithCustomOrdinals()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            StringWriter writer = new StringWriter();
            string actual, expected;

            eventDB.Load(TestUtil.GetTestFile("courseview\\sampleevent4.coursescribe"));
            eventDB.Validate();

            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, Designator(5));
            DumpCourseView(courseView, writer);
            actual = writer.ToString();
            expected =
@"Name='Score 4', Kind='Score', CourseId=5
Total Length=0  Part Length=0  Total Climb=-1  ScoreColumn=1  Total Score=155  Total Controls=11
 0: [-1] Ids:  1,101
 1: [ 7] Ids: 17,109
 2: [ 8] Ids:  2,113
 3: [ 9] Ids:  7,114
 4: [10] Ids: 11,102
 5: [11] Ids:  8,115
 6: [12] Ids: 20,112
 7: [13] Ids:  5,107
 8: [14] Ids:  4,105
 9: [15] Ids: 16,108
10: [16] Ids: 18,110
11: [17] Ids: 19,111
12: [-1] Ids:  6,116
13: [-1] Ids:  3,104
14: [-1] Ids: 15,106
15: [-1] Ids: 22,103
Special 1 (FirstAid)
Special 3 (Boundary)
Special 4 (OOB)
";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ScoreDescriptionSpecialsOnly()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            StringWriter writer = new StringWriter();
            string actual, expected;

            eventDB.Load(TestUtil.GetTestFile("courseview\\sampleevent1.coursescribe"));
            eventDB.Validate();

            CourseView courseView = CourseView.CreatePositioningCourseView(eventDB, new CourseDesignator(CourseId(5)));
            DumpCourseView(courseView, writer);
            actual = writer.ToString();
            expected =
@"Name='Score 4', Kind='Score', CourseId=5
Total Length=0  Part Length=0  Total Climb=-1  ScoreColumn=0  Total Score=155  Total Controls=11
 0: [-1] Ids:  1,101
 1: [-1] Ids: 17,109
 2: [-1] Ids:  2,113
 3: [-1] Ids:  7,114
 4: [-1] Ids: 11,102
 5: [-1] Ids:  8,115
 6: [-1] Ids: 20,112
 7: [-1] Ids:  5,107
 8: [-1] Ids:  4,105
 9: [-1] Ids: 16,108
10: [-1] Ids: 18,110
11: [-1] Ids: 19,111
12: [-1] Ids:  6,116
13: [-1] Ids:  3,104
14: [-1] Ids: 15,106
15: [-1] Ids: 22,103
";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void AllControlsCourseView()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            StringWriter writer = new StringWriter();
            string actual, expected;

            eventDB.Load(TestUtil.GetTestFile("courseview\\sampleevent1.coursescribe"));
            eventDB.Validate();

            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, CourseDesignator.AllControls);
            DumpCourseView(courseView, writer);
            actual = writer.ToString();
            expected =
@"Name='All controls', Kind='AllControls', CourseId=0
Total Length=0  Part Length=0  Total Climb=-1  ScoreColumn=-1  Total Score=0  Total Controls=17
 0: [-1] Ids:  1,  0
 1: [-1] Ids: 23,  0
 2: [-1] Ids:  2,  0
 3: [-1] Ids:  4,  0
 4: [-1] Ids: 12,  0
 5: [-1] Ids:  7,  0
 6: [-1] Ids: 10,  0
 7: [-1] Ids: 11,  0
 8: [-1] Ids:  8,  0
 9: [-1] Ids:  9,  0
10: [-1] Ids: 13,  0
11: [-1] Ids: 14,  0
12: [-1] Ids: 16,  0
13: [-1] Ids: 17,  0
14: [-1] Ids: 18,  0
15: [-1] Ids: 19,  0
16: [-1] Ids: 20,  0
17: [-1] Ids: 21,  0
18: [-1] Ids:  5,  0
19: [-1] Ids:  6,  0
20: [-1] Ids: 24,  0
21: [-1] Ids:  3,  0
22: [-1] Ids: 15,  0
23: [-1] Ids: 22,  0
Special 1 (FirstAid)
Special 2 (OptCrossing)
Special 3 (Boundary)
Special 4 (OOB)
Special 5 (Text)
";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void FilteredAllControlsCourseView()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            StringWriter writer = new StringWriter();
            string actual, expected;

            eventDB.Load(TestUtil.GetTestFile("courseview\\sampleevent1.coursescribe"));
            eventDB.Validate();

            CourseView courseView = CourseView.CreateFilteredAllControlsView(eventDB, new CourseDesignator[] { Designator(3), Designator(4) }, ControlPointKind.Normal, false, true);
            DumpCourseView(courseView, writer);
            actual = writer.ToString();
            expected =
@"Name='All controls', Kind='AllControls', CourseId=0
Total Length=0  Part Length=0  Total Climb=-1  ScoreColumn=-1  Total Score=0  Total Controls=12
 0: [-1] Ids:  2,  0
 1: [-1] Ids: 12,  0
 2: [-1] Ids:  7,  0
 3: [-1] Ids: 10,  0
 4: [-1] Ids:  8,  0
 5: [-1] Ids:  9,  0
 6: [-1] Ids: 13,  0
 7: [-1] Ids: 14,  0
 8: [-1] Ids: 17,  0
 9: [-1] Ids: 19,  0
10: [-1] Ids: 20,  0
11: [-1] Ids: 21,  0
";
            Assert.AreEqual(expected, actual);
        }


        [TestMethod]
        public void SpecialLegs()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            StringWriter writer = new StringWriter();
            string actual, expected;

            eventDB.Load(TestUtil.GetTestFile("courseview\\speciallegs.coursescribe"));
            eventDB.Validate();

            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(1)));
            DumpCourseView(courseView, writer);
            actual = writer.ToString();
            expected =
@"Name='Leggy', Kind='Normal', CourseId=1
Total Length=2242.754  Part Length=2242.754  Total Climb=-1  ScoreColumn=-1  Total Score=0  Total Controls=4
 0: [ 0] Ids:  1,  1
    Legs: (Next:1,Id:0,length:515.8431)  
 1: [ 1] Ids:  2,  2
    Legs: (Next:2,Id:2,length:420.1177)  
 2: [ 2] Ids:  3,  3
    Legs: (Next:3,Id:3,length:377.5438)  
 3: [ 3] Ids:  4,  4
    Legs: (Next:4,Id:4,length:518.8033)  
 4: [ 4] Ids:  5,  5
    Legs: (Next:5,Id:0,length:410.4461)  
 5: [-1] Ids:  6,  6
";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MapExchangeParts()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            StringWriter writer = new StringWriter();
            string actual, expected;
            CourseView courseView;

            eventDB.Load(TestUtil.GetTestFile("courseview\\mapexchange2.ppen"));
            eventDB.Validate();

            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(6), 0));
            DumpCourseView(courseView, writer);
            actual = writer.ToString();
            expected =
@"Name='Course 5', Kind='Normal', CourseId=6, Part=0
Total Length=5002.36  Part Length=2643.736  Total Climb=-1  ScoreColumn=-1  Total Score=0  Total Controls=9
 0: [ 0] Ids:  1,601
    Legs: (Next:1,Id:5,length:191.3766)  
 1: [ 1] Ids: 59,602
    Legs: (Next:2,Id:0,length:379.3481)  
 2: [ 2] Ids: 51,603
    Legs: (Next:3,Id:6,length:326.0981)  
 3: [ 3] Ids: 46,604
    Legs: (Next:4,Id:0,length:258.272)  
 4: [ 4] Ids: 47,605
    Legs: (Next:5,Id:0,length:209.6365)  
 5: [ 5] Ids: 48,606
    Legs: (Next:6,Id:0,length:294.0153)  
 6: [ 6] Ids: 50,607
    Legs: (Next:7,Id:0,length:361.1994)  
 7: [ 7] Ids: 56,608
    Legs: (Next:8,Id:0,length:98.88374)  
 8: [ 8] Ids: 57,609
    Legs: (Next:9,Id:0,length:316.6276)  
 9: [ 9] Ids: 79,610
    Legs: (Next:10,Id:1,length:208.2787)  
10: [ 0] Ids: 35,611 hidden
";
            Assert.AreEqual(expected, actual);

            writer = new StringWriter();
            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(6), 1));
            DumpCourseView(courseView, writer);
            actual = writer.ToString();
            expected =
@"Name='Course 5', Kind='Normal', CourseId=6, Part=1
Total Length=5002.36  Part Length=1195.639  Total Climb=-1  ScoreColumn=-1  Total Score=0  Total Controls=4
 0: [ 0] Ids: 35,611
    Legs: (Next:1,Id:4,length:128.0156)  
 1: [10] Ids: 37,612
    Legs: (Next:2,Id:0,length:298.3907)  
 2: [11] Ids: 36,613
    Legs: (Next:3,Id:0,length:316.1013)  
 3: [12] Ids: 39,614
    Legs: (Next:4,Id:0,length:453.1313)  
 4: [13] Ids: 43,615
";
            Assert.AreEqual(expected, actual);

            writer = new StringWriter();
            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(6), 2));
            DumpCourseView(courseView, writer);
            actual = writer.ToString();
            expected =
@"Name='Course 5', Kind='Normal', CourseId=6, Part=2
Total Length=5002.36  Part Length=401.2206  Total Climb=-1  ScoreColumn=-1  Total Score=0  Total Controls=2
 0: [13] Ids: 43,615
    Legs: (Next:1,Id:2,length:401.2206)  
 1: [14] Ids: 54,616
Extra course control 620
";
            Assert.AreEqual(expected, actual);

            writer = new StringWriter();
            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(6), 3));
            DumpCourseView(courseView, writer);
            actual = writer.ToString();
            expected =
@"Name='Course 5', Kind='Normal', CourseId=6, Part=3
Total Length=5002.36  Part Length=761.7648  Total Climb=-1  ScoreColumn=-1  Total Score=0  Total Controls=4
 0: [14] Ids: 54,616
    Legs: (Next:1,Id:3,length:257.777)  
 1: [15] Ids: 41,617
    Legs: (Next:2,Id:0,length:227.2003)  
 2: [16] Ids: 42,618
    Legs: (Next:3,Id:0,length:200.6907)  
 3: [17] Ids: 38,619
    Legs: (Next:4,Id:0,length:76.09671)  
 4: [-1] Ids:  2,620
";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetViewBounds()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));

            eventDB.Load(TestUtil.GetTestFile("courseview\\sampleevent1.coursescribe"));
            eventDB.Validate();

            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, CourseDesignator.AllControls);
            RectangleF bounds = courseView.GetViewBounds();
            Assert.AreEqual(-51.4F, bounds.Left);
            Assert.AreEqual(-47.8F, bounds.Top);
            Assert.AreEqual(106.9F, bounds.Width);
            Assert.AreEqual(92.9F, bounds.Height, 0.0001);
            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(4)));
            Assert.AreEqual(new RectangleF(-51.4F, -39F, 96.8F, 84.1F), courseView.GetViewBounds());
            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(5)));
            Assert.AreEqual(new RectangleF(-51.4F, -39F, 106.9F, 84.1F), courseView.GetViewBounds());
        }

        [TestMethod]
        public void AllControlsCourseDesignator()
        {
            CourseDesignator d1, d2;

            d1 = CourseDesignator.AllControls;
            d2 = new CourseDesignator(Id<Course>.None);

            Assert.IsTrue(d1.Equals(d2));
            Assert.IsFalse(d1.Equals(new CourseDesignator(CourseId(1))));

            Assert.IsTrue(d1.GetHashCode() == d2.GetHashCode());
            Assert.IsFalse(d1.GetHashCode() == new CourseDesignator(CourseId(1)).GetHashCode());

            Assert.IsTrue(d1.IsAllControls);
            Assert.IsTrue(d1.CourseId.IsNone);
            Assert.IsTrue(d1.AllParts);
        }

        [TestMethod]
        public void CourseDesignatorRegular()
        {
            CourseDesignator d1, d2, d3;

            d1 = new CourseDesignator(CourseId(2));
            d2 = new CourseDesignator(CourseId(2));
            d3 = new CourseDesignator(CourseId(3));

            Assert.IsTrue(d1.Equals(d2));
            Assert.IsFalse(d1.Equals(d3));

            Assert.IsTrue(d1.GetHashCode() == d2.GetHashCode());
            Assert.IsFalse(d1.GetHashCode() == d3.GetHashCode());

            Assert.IsFalse(d1.IsAllControls);
            Assert.IsTrue(d1.CourseId.id == 2);
            Assert.IsTrue(d1.AllParts);
        }

        [TestMethod]
        public void CourseDesignatorPart()
        {
            CourseDesignator d1, d2, d3;

            d1 = new CourseDesignator(CourseId(2), 1);
            d2 = new CourseDesignator(CourseId(2), 1);
            d3 = new CourseDesignator(CourseId(2));

            Assert.IsTrue(d1.Equals(d2));
            Assert.IsFalse(d1.Equals(d3));

            Assert.IsTrue(d1.GetHashCode() == d2.GetHashCode());
            Assert.IsFalse(d1.GetHashCode() == d3.GetHashCode());

            Assert.IsFalse(d1.IsAllControls);
            Assert.IsTrue(d1.CourseId.id == 2);
            Assert.IsFalse(d1.AllParts);
            Assert.IsTrue(d1.Part == 1);
        }

        [TestMethod]
        public void AllVariations()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            StringWriter writer = new StringWriter();
            string actual, expected;

            eventDB.Load(TestUtil.GetTestFile("queryevent\\variations.ppen"));
            eventDB.Validate();

            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(1)));
            DumpCourseView(courseView, writer);
            actual = writer.ToString();
            expected =
@"Name='Course 1', Kind='AllVariations', CourseId=1
Total Length=4732.167  Part Length=4732.167  Total Climb=-1  ScoreColumn=-1  Total Score=0  Total Controls=21
 0: [-1] Ids:  1,  1
    Legs: (Next:1,Id:0,length:158.8382)  
 1: [-1] Ids:  2,  2, 24
    Legs: (Next:3,Id:0,length:209.0375)  (Next:2,Id:0,length:826.1527)  
 2: [-1] Ids: 24, 12
    Legs: (Next:21,Id:0,length:1333.715)  
 3: [-1] Ids:  3,  3
    Legs: (Next:4,Id:0,length:209.0376)  
 4: [-1] Ids:  4,  4, 25, 26, 27
    Legs: (Next:14,Id:0,length:236.2064)  (Next:5,Id:0,length:292.0621)  (Next:7,Id:0,length:266.2318)  (Next:9,Id:0,length:301.3232)  
 5: [-1] Ids: 12, 15
    Legs: (Next:6,Id:0,length:146.6947)  
 6: [-1] Ids: 13, 16
    Legs: (Next:4,Id:0,length:262.4181)  
 7: [-1] Ids: 14, 17
    Legs: (Next:8,Id:0,length:152.2906)  
 8: [-1] Ids: 15, 18
    Legs: (Next:4,Id:0,length:374.2521)  
 9: [-1] Ids: 16, 19
    Legs: (Next:10,Id:0,length:200.8046)  
10: [-1] Ids: 18, 30, 21
    Legs: (Next:11,Id:0,length:158.772)  (Next:12,Id:0,length:155.0242)  
11: [-1] Ids: 17, 20
    Legs: (Next:13,Id:0,length:142.4825)  
12: [-1] Ids: 19, 22
    Legs: (Next:13,Id:0,length:140.2542)  
13: [-1] Ids: 20, 23
    Legs: (Next:4,Id:0,length:260.0061)  
14: [-1] Ids:  5,  5
    Legs: (Next:15,Id:0,length:187.8728)  
15: [-1] Ids:  6,  6
    Legs: (Next:16,Id:0,length:248.7224)  
16: [-1] Ids:  7,  7
    Legs: (Next:17,Id:0,length:206.6461)  
17: [-1] Ids:  8,  8
    Legs: (Next:18,Id:0,length:212.0797)  
18: [-1] Ids:  9,  9, 28, 29
    Legs: (Next:21,Id:0,length:159.1022)  (Next:19,Id:0,length:153.8228)  (Next:20,Id:0,length:121.1895)  
19: [-1] Ids: 21, 13
    Legs: (Next:21,Id:0,length:155.6325)  
20: [-1] Ids: 22, 14
    Legs: (Next:21,Id:0,length:139.2523)  
21: [-1] Ids: 10, 10
    Legs: (Next:22,Id:0,length:196.9326)  
22: [-1] Ids: 11, 11
";
            Assert.AreEqual(expected, actual);
        }


    }
}

#endif //TEST
