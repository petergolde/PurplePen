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
    [TestClass]
    public class CourseViewTests: TestFixtureBase
    {
        internal void DumpCourseView(CourseView courseView, TextWriter writer)
        {
            writer.WriteLine("Name='{0}', Kind='{1}', CourseId={2}", courseView.CourseName, courseView.Kind, courseView.BaseCourseId);
            writer.WriteLine("Total Length={0}  Total Climb={1}  Total Score={2}  Total Controls={3}", courseView.TotalLength, courseView.TotalClimb, courseView.TotalScore, courseView.TotalNormalControls);

            for (int i = 0; i < courseView.ControlViews.Count; ++i) {
                CourseView.ControlView controlView = courseView.ControlViews[i];

                writer.WriteLine("{0,2}: Ids:{1,3},{2,3}", i, controlView.controlId, controlView.courseControlId);
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
                CourseView courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(courseId));
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
Total Length=4667.309  Total Climb=173  Total Score=0  Total Controls=4
 0: Ids:  1, 11
    Legs: (Next:1,Id:0,length:340.1033)  
 1: Ids: 11, 12
    Legs: (Next:2,Id:0,length:537.7557)  
 2: Ids: 22, 13
    Legs: (Next:3,Id:0,length:245.7254)  
 3: Ids:  3, 14
    Legs: (Next:4,Id:0,length:112.54)  
 4: Ids:  4, 15
    Legs: (Next:5,Id:0,length:280.2271)  
 5: Ids: 15, 16
    Legs: (Next:6,Id:0,length:287.0649)  
 6: Ids:  5, 17
    Legs: (Next:7,Id:0,length:1440)  
 7: Ids: 18, 18
    Legs: (Next:8,Id:0,length:1423.892)  
 8: Ids:  6, 19
Special 1 (FirstAid)
Special 4 (OOB)
Special 6 (Descriptions)
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
Total Length=4667.309  Total Climb=173  Total Score=0  Total Controls=4
 0: Ids:  1, 11
    Legs: (Next:1,Id:0,length:340.1033)  
 1: Ids: 11, 12
    Legs: (Next:2,Id:0,length:537.7557)  
 2: Ids: 22, 13
    Legs: (Next:3,Id:0,length:245.7254)  
 3: Ids:  3, 14
    Legs: (Next:4,Id:0,length:112.54)  
 4: Ids:  4, 15
    Legs: (Next:5,Id:0,length:280.2271)  
 5: Ids: 15, 16
    Legs: (Next:6,Id:0,length:287.0649)  
 6: Ids:  5, 17
    Legs: (Next:7,Id:0,length:1440)  
 7: Ids: 18, 18
    Legs: (Next:8,Id:0,length:1423.892)  
 8: Ids:  6, 19
Special 6 (Descriptions)
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
Total Length=0  Total Climb=-1  Total Score=155  Total Controls=11
 0: Ids:  1,101
 1: Ids: 17,109
 2: Ids:  2,113
 3: Ids:  7,114
 4: Ids: 11,102
 5: Ids:  8,115
 6: Ids: 20,112
 7: Ids:  5,107
 8: Ids:  4,105
 9: Ids: 16,108
10: Ids: 18,110
11: Ids: 19,111
12: Ids:  6,116
13: Ids: 15,106
14: Ids:  3,104
15: Ids: 22,103
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
Total Length=0  Total Climb=-1  Total Score=155  Total Controls=11
 0: Ids:  1,101
 1: Ids: 17,109
 2: Ids:  2,113
 3: Ids:  7,114
 4: Ids: 11,102
 5: Ids:  8,115
 6: Ids: 20,112
 7: Ids:  5,107
 8: Ids:  4,105
 9: Ids: 16,108
10: Ids: 18,110
11: Ids: 19,111
12: Ids:  6,116
13: Ids: 15,106
14: Ids:  3,104
15: Ids: 22,103
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
Total Length=0  Total Climb=-1  Total Score=0  Total Controls=17
 0: Ids: 23,  0
 1: Ids:  1,  0
 2: Ids:  2,  0
 3: Ids:  4,  0
 4: Ids: 12,  0
 5: Ids:  7,  0
 6: Ids: 10,  0
 7: Ids: 11,  0
 8: Ids:  8,  0
 9: Ids:  9,  0
10: Ids: 13,  0
11: Ids: 14,  0
12: Ids: 16,  0
13: Ids: 17,  0
14: Ids: 18,  0
15: Ids: 19,  0
16: Ids: 20,  0
17: Ids: 21,  0
18: Ids:  5,  0
19: Ids:  6,  0
20: Ids: 24,  0
21: Ids: 15,  0
22: Ids:  3,  0
23: Ids: 22,  0
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

            CourseView courseView = CourseView.CreateFilteredAllControlsView(eventDB, new Id<Course>[] {CourseId(3), CourseId(4)}, ControlPointKind.Normal, false, true);
            DumpCourseView(courseView, writer);
            actual = writer.ToString();
            expected =
@"Name='All controls', Kind='AllControls', CourseId=0
Total Length=0  Total Climb=-1  Total Score=0  Total Controls=12
 0: Ids:  2,  0
 1: Ids: 12,  0
 2: Ids:  7,  0
 3: Ids: 10,  0
 4: Ids:  8,  0
 5: Ids:  9,  0
 6: Ids: 13,  0
 7: Ids: 14,  0
 8: Ids: 17,  0
 9: Ids: 19,  0
10: Ids: 20,  0
11: Ids: 21,  0
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
Total Length=2242.754  Total Climb=-1  Total Score=0  Total Controls=4
 0: Ids:  1,  1
    Legs: (Next:1,Id:0,length:515.8431)  
 1: Ids:  2,  2
    Legs: (Next:2,Id:2,length:420.1177)  
 2: Ids:  3,  3
    Legs: (Next:3,Id:3,length:377.5438)  
 3: Ids:  4,  4
    Legs: (Next:4,Id:4,length:518.8033)  
 4: Ids:  5,  5
    Legs: (Next:5,Id:0,length:410.4461)  
 5: Ids:  6,  6
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
    }
}

#endif //TEST
