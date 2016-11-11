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
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.Tests
{
    using System.Linq;
    using PurplePen.MapModel;


    [TestClass]
    public class DescriptionFormatterTests: TestFixtureBase
    {
        [TestMethod]
        public void DisplayAllCourses()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            CourseView courseView;
            DescriptionFormatter descFormatter;
            DescriptionLine[] description;

            eventDB.Load(TestUtil.GetTestFile("descformatter\\sampleevent1.coursescribe"));
            eventDB.Validate();

            foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB)) {
                CourseDesignator designator;
                if (QueryEvent.HasVariations(eventDB, courseId)) {
                    var variationInfo = QueryEvent.GetAllVariations(eventDB, courseId).First();
                    designator = new CourseDesignator(courseId, variationInfo);
                }
                else {
                    designator = new CourseDesignator(courseId);
                }
                courseView = CourseView.CreateViewingCourseView(eventDB, designator);
                descFormatter = new DescriptionFormatter(courseView, symbolDB);
                description = descFormatter.CreateDescription(false);
                DescriptionFormatter.DumpDescription(symbolDB, description, Console.Out);
                Console.WriteLine();
            }

            courseView = CourseView.CreateViewingCourseView(eventDB, CourseDesignator.AllControls);
            descFormatter = new DescriptionFormatter(courseView, symbolDB);
            description = descFormatter.CreateDescription(false);
            DescriptionFormatter.DumpDescription(symbolDB, description, Console.Out);

        }

        private void TestCourseFormatter(string testFileName, Id<Course> courseId, bool createKey, string expected)
        {
            TestCourseFormatter(testFileName, new CourseDesignator(courseId), createKey, expected);
        }

        // Test the course description formatter with a given file and course
        private void TestCourseFormatter(string testFileName, CourseDesignator courseDesignator, bool createKey, string expected)
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            StringWriter writer = new StringWriter();
            string actual;

            eventDB.Load(TestUtil.GetTestFile(testFileName));
            eventDB.Validate();

            CourseView courseView;
            courseView = CourseView.CreateViewingCourseView(eventDB, courseDesignator);
            DescriptionFormatter descFormatter = new DescriptionFormatter(courseView, symbolDB);
            DescriptionLine[] description = descFormatter.CreateDescription(createKey);

            DescriptionFormatter.DumpDescription(symbolDB, description, writer);
            actual = writer.ToString();

            Assert.AreEqual(expected, actual);
        }


        [TestMethod]
        public void NormalCourseFormatter()
        {
            TestCourseFormatter("descformatter\\sampleevent1.coursescribe", CourseId(4), false, 
@"      | Sample Event 1                                |   [Sample Event 1]
      |SampleCourse4    |4.7 km           |175 m      |   [Length 4.7 km, climb 175 m]
(  1) |start|     |     |  2.8|  8.5|     |     |     |   [Start: open bare rock]
( 11) |    1|  191|     |  5.2|  5.2| 10.1|11.15|     |   [Between path crossings]
( 22) |                 13.4:                         |   [Mandatory passage]
(  4) |    2|   32|     |  3.7|     |     |     | 12.1|   [very marshy spot]
( 15) |                 13.3:                         |   [Mandatory crossing point]
(  5) |    3|   GO| 0.1N|  5.5|  5.2| 10.1|11.1N| 12.3|   [N side of N power line and path crossing (radio)]
( 18) |    4|  303|     |  1.3|     |    4|     |     |   [Reentrant, 4m deep]
(  6) |                 14.2: 1420 m                  |   [Navigate 1420 m to finish funnel]
");
        }

        [TestMethod]
        public void ScoreCourseFormatter()
        {
            TestCourseFormatter("descformatter\\sampleevent1.coursescribe", CourseId(5), false, 
@"      | Sample Event 1                                |   [Sample Event 1]
      | Score more!                                   |   [Score more!]
      |Score 4          |155 points                   |   [155 points]
(  1) |start|     |     |  2.8|  8.5|     |     |     |   [Start: open bare rock]
( 17) |    5|  302|     | 1.14|     |     |11.1S|     |   [S side of pit]
(  2) |   10|   31|  0.3|  2.4|     |   2m|     |     |   [Upper boulder, 2m high]
(  7) |   10|  189|     |  1.8|  1.8| 10.2|     |     |   [Small gully junction]
( 11) |   10|  191|     |  5.2|  5.2| 10.1|11.15|     |   [Between path crossings]
(  8) |   10|  210|     | 5.11| 5.20|     |11.15|     |   [Between building and statue]
( 20) |   10|  305|     |  2.7|     |  6x7|     | 12.4|   [Stony ground, 6m by 7m (manned)]
(  5) |   10|   GO| 0.1N|  5.5|  5.2| 10.1|11.1N| 12.3|   [N side of N power line and path crossing (radio)]
(  4) |   20|   32|     |  3.7|     |     |     | 12.1|   [very marshy spot]
( 16) |   20|  301|     | 1.14|  8.4|  3.0|     |     |   [Overgrown pit, 3m deep]
( 18) |   20|  303|     |  1.3|     |    4|     |     |   [Reentrant, 4m deep]
( 19) |   30|  304| 0.1S|     |  8.5|     |     |     |   [S ]
");
        }

        [TestMethod]
        public void ScoreCourseFormatter2()
        {
            TestCourseFormatter("descformatter\\sampleevent1.coursescribe", CourseId(3), false, 
@"      | Sample Event 1                                |   [Sample Event 1]
      |Rambo            |4 controls                   |   [4 controls]
(  1) |start|     |     |  2.8|  8.5|     |     |     |   [Start: open bare rock]
(  4) |     |   32|     |  3.7|     |     |     | 12.1|   [very marshy spot]
( 11) |     |  191|     |  5.2|  5.2| 10.1|11.15|     |   [Between path crossings]
( 16) |     |  301|     | 1.14|  8.4|  3.0|     |     |   [Overgrown pit, 3m deep]
(  5) |     |   GO| 0.1N|  5.5|  5.2| 10.1|11.1N| 12.3|   [N side of N power line and path crossing (radio)]
");
        }

        [TestMethod]
        public void ScoreCourseFormatterColumnB() {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            StringWriter writer = new StringWriter();
            string actual, expected;

            eventDB.Load(TestUtil.GetTestFile("descformatter\\sampleevent3.coursescribe"));
            eventDB.Validate();

            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, Designator(5));
            DescriptionLine[] description = new DescriptionFormatter(courseView, symbolDB).CreateDescription(false);

            DescriptionFormatter.DumpDescription(symbolDB, description, writer);
            actual = writer.ToString();
            expected =
@"      | Sample Event 1                                |   [Sample Event 1]
      | Score more!                                   |   [Score more!]
      |Score 4          |155 points                   |   [155 points]
(  1) |start|     |     |  2.8|  8.5|     |     |     |   [Start: open bare rock]
( 17) |    1|    5|     | 1.14|     |     |11.1S|     |   [S side of pit]
(  2) |    2|   10|  0.3|  2.4|     |   2m|     |     |   [Upper boulder, 2m high]
(  7) |    3|   10|     |  1.8|  1.8| 10.2|     |     |   [Small gully junction]
( 11) |    4|   10|     |  5.2|  5.2| 10.1|11.15|     |   [Between path crossings]
(  8) |    5|   10|     | 5.11| 5.20|     |11.15|     |   [Between building and statue]
( 20) |    6|   10|     |  2.7|     |  6x7|     | 12.4|   [Stony ground, 6m by 7m (manned)]
(  5) |    7|   10| 0.1N|  5.5|  5.2| 10.1|11.1N| 12.3|   [N side of N power line and path crossing (radio)]
(  4) |    8|   20|     |  3.7|     |     |     | 12.1|   [very marshy spot]
( 16) |    9|   20|     | 1.14|  8.4|  3.0|     |     |   [Overgrown pit, 3m deep]
( 18) |   10|   20|     |  1.3|     |    4|     |     |   [Reentrant, 4m deep]
( 19) |   11|   30| 0.1S|     |  8.5|     |     |     |   [S ]
";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ScoreCourseFormatterColumnH() {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            StringWriter writer = new StringWriter();
            string actual, expected;

            eventDB.Load(TestUtil.GetTestFile("descformatter\\sampleevent4.coursescribe"));
            eventDB.Validate();

            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, Designator(5));
            DescriptionLine[] description = new DescriptionFormatter(courseView, symbolDB).CreateDescription(false);

            DescriptionFormatter.DumpDescription(symbolDB, description, writer);
            actual = writer.ToString();
            expected =
@"      | Sample Event 1                                |   [Sample Event 1]
      | Score more!                                   |   [Score more!]
      |Score 4          |155 points                   |   [155 points]
(  1) |start|     |     |  2.8|  8.5|     |     |     |   [Start: open bare rock]
( 17) |    1|  302|     | 1.14|     |     |11.1S|    5|   [S side of pit]
(  2) |    2|   31|  0.3|  2.4|     |   2m|     |   10|   [Upper boulder, 2m high]
(  7) |    3|  189|     |  1.8|  1.8| 10.2|     |   10|   [Small gully junction]
( 11) |    4|  191|     |  5.2|  5.2| 10.1|11.15|   10|   [Between path crossings]
(  8) |    5|  210|     | 5.11| 5.20|     |11.15|   10|   [Between building and statue]
( 20) |    6|  305|     |  2.7|     |  6x7|     |   10|   [Stony ground, 6m by 7m (manned)]
(  5) |    7|   GO| 0.1N|  5.5|  5.2| 10.1|11.1N|   10|   [N side of N power line and path crossing (radio)]
(  4) |    8|   32|     |  3.7|     |     |     |   20|   [very marshy spot]
( 16) |    9|  301|     | 1.14|  8.4|  3.0|     |   20|   [Overgrown pit, 3m deep]
( 18) |   10|  303|     |  1.3|     |    4|     |   20|   [Reentrant, 4m deep]
( 19) |   11|  304| 0.1S|     |  8.5|     |     |   30|   [S ]
";
            Assert.AreEqual(expected, actual);
        }


        [TestMethod]
        public void ScoreCourseFormatterNoScore() {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            StringWriter writer = new StringWriter();
            string actual, expected;

            eventDB.Load(TestUtil.GetTestFile("descformatter\\sampleevent5.coursescribe"));
            eventDB.Validate();

            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, Designator(5));
            DescriptionLine[] description = new DescriptionFormatter(courseView, symbolDB).CreateDescription(false);

            DescriptionFormatter.DumpDescription(symbolDB, description, writer);
            actual = writer.ToString();
            expected =
@"      | Sample Event 1                                |   [Sample Event 1]
      | Score more!                                   |   [Score more!]
      |Score 4          |155 points                   |   [155 points]
(  1) |start|     |     |  2.8|  8.5|     |     |     |   [Start: open bare rock]
( 17) |    1|  302|     | 1.14|     |     |11.1S|     |   [S side of pit]
(  2) |    2|   31|  0.3|  2.4|     |   2m|     |     |   [Upper boulder, 2m high]
(  7) |    3|  189|     |  1.8|  1.8| 10.2|     |     |   [Small gully junction]
( 11) |    4|  191|     |  5.2|  5.2| 10.1|11.15|     |   [Between path crossings]
(  8) |    5|  210|     | 5.11| 5.20|     |11.15|     |   [Between building and statue]
( 20) |    6|  305|     |  2.7|     |  6x7|     | 12.4|   [Stony ground, 6m by 7m (manned)]
(  5) |    7|   GO| 0.1N|  5.5|  5.2| 10.1|11.1N| 12.3|   [N side of N power line and path crossing (radio)]
(  4) |    8|   32|     |  3.7|     |     |     | 12.1|   [very marshy spot]
( 16) |    9|  301|     | 1.14|  8.4|  3.0|     |     |   [Overgrown pit, 3m deep]
( 18) |   10|  303|     |  1.3|     |    4|     |     |   [Reentrant, 4m deep]
( 19) |   11|  304| 0.1S|     |  8.5|     |     |     |   [S ]
";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EmptyCourseFormatter()
        {
            TestCourseFormatter("descformatter\\sampleevent1.coursescribe", CourseId(2), false, 
@"      | Sample Event 1                                |   [Sample Event 1]
      |Yellow           |0.0 km           |           |   [Length 0.0 km]
");
        }

        [TestMethod]
        public void AllControlsFormatter()
        {
            TestCourseFormatter("descformatter\\sampleevent1.coursescribe", Id<Course>.None, false,
@"      | Sample Event 1                                |   [Sample Event 1]
      |All controls     |17 controls                  |   [17 controls]
(  1) |start|     |     |  2.8|  8.5|     |     |     |   [Start: open bare rock]
( 23) |start|     |0.2NW|  1.7|     |  2.5|     |     |   [Start: NW gully, 2.5m deep]
(  2) |     |   31|  0.3|  2.4|     |   2m|     |     |   [Upper boulder, 2m high]
(  4) |     |   32|     |  3.7|     |     |     | 12.1|   [very marshy spot]
( 12) |     |   74|     |  2.4|     |0.5/2.5|11.15|     |   [Between boulders, 0.5m to 2.5m high]
(  7) |     |  189|     |  1.8|  1.8| 10.2|     |     |   [Small gully junction]
( 10) |     |  190|     |  5.1|  5.5| 10.1|11.1N|     |   [N side of road and power line crossing]
( 11) |     |  191|     |  5.2|  5.2| 10.1|11.15|     |   [Between path crossings]
(  8) |     |  210|     | 5.11| 5.20|     |11.15|     |   [Between building and statue]
(  9) |     |  211|     |  3.7|  3.7|4x4|5x6|11.15|     |   [Between marshes, 4m by 4m and 5m by 6m]
( 13) |     |  290| 0.1N|  5.2|     | 10.2|     |     |   [N path junction]
( 14) |     |  291|     | 1.10|  2.4|     |     |     |   [Knoll and boulder]
( 16) |     |  301|     | 1.14|  8.4|  3.0|     |     |   [Overgrown pit, 3m deep]
( 17) |     |  302|     | 1.14|     |     |11.1S|     |   [S side of pit]
( 18) |     |  303|     |  1.3|     |    4|     |     |   [Reentrant, 4m deep]
( 19) |     |  304| 0.1S|     |  8.5|     |     |     |   [S ]
( 20) |     |  305|     |  2.7|     |  6x7|     | 12.4|   [Stony ground, 6m by 7m (manned)]
( 21) |     |  306|     |     |     |     |     |     |   []
(  5) |     |   GO| 0.1N|  5.5|  5.2| 10.1|11.1N| 12.3|   [N side of N power line and path crossing (radio)]
(  6) |                 14.2:                         |   [Navigate to finish funnel]
( 24) |                 14.1:                         |   [Follow tapes to finish]
(  3) |                 13.3:                         |   [Mandatory crossing point]
( 15) |                 13.3:                         |   [Mandatory crossing point]
( 22) |                 13.4:                         |   [Mandatory passage]
");
        }

        [TestMethod]
        public void SpecialLegsFormatter()
        {
            TestCourseFormatter("descformatter\\speciallegs.coursescribe", CourseId(1), false, 
@"      | SpecialLegs                                   |   [SpecialLegs]
      |Leggy            |2.3 km           |           |   [Length 2.3 km]
(  1) |start|     |     |     |     |     |     |     |   [Start: ]
(  2) |    1|   31|     |     |     |     |     |     |   []
(  2) |                 13.2: 420 m                   |   [Follow tapes 420 m between controls]
(  3) |    2|   32|     |     |     |     |     |     |   []
(  3) |                 13.1: 130 m                   |   [Follow tapes 130 m away from control]
(  4) |    3|   33|     |     |     |     |     |     |   []
(  5) |    4|   34|     |     |     |     |     |     |   []
(  6) |                 14.3: 410 m                   |   [Navigate 410 m to finish]
");
        }

        [TestMethod]
        public void SpecialLegsCrossingFormatter()
        {
            TestCourseFormatter("descformatter\\speciallegs.coursescribe", CourseId(2), false,
@"      | SpecialLegs                                   |   [SpecialLegs]
      |Crossings        |2.5 km           |           |   [Length 2.5 km]
(  1) |start|     |     |     |     |     |     |     |   [Start: ]
(  5) |    1|   34|     |     |     |     |     |     |   []
(  7) |                 13.3:                         |   [Mandatory crossing point]
(  7) |                 13.2: 410 m                   |   [Follow tapes 410 m between controls]
(  3) |    2|   32|     |     |     |     |     |     |   []
(  2) |    3|   31|     |     |     |     |     |     |   []
(  8) |                 13.3:                         |   [Mandatory crossing point]
(  8) |                 13.1: 140 m                   |   [Follow tapes 140 m away from control]
(  9) |    4|   35|     |     |     |     |     |     |   []
(  6) |                 14.3: 250 m                   |   [Navigate 250 m to finish]
");
        }

        [TestMethod]
        public void ClearTextAndSymbols()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            StringWriter writer = new StringWriter();
            string actual, expected;

            eventDB.Load(TestUtil.GetTestFile("descformatter\\sampleevent1.coursescribe"));
            eventDB.Validate();

            CourseView courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(4)));
            DescriptionFormatter descFormatter = new DescriptionFormatter(courseView, symbolDB);
            DescriptionLine[] description = descFormatter.CreateDescription(false);
            DescriptionFormatter.ClearTextAndSymbols(description);

            DescriptionFormatter.DumpDescription(symbolDB, description, writer);
            actual = writer.ToString();
            expected =
@"      |                                               |
      |                 |                 |           |
(  1) |     |     |     |     |     |     |     |     |
( 11) |     |     |     |     |     |     |     |     |
( 22) |                     :                         |
(  4) |     |     |     |     |     |     |     |     |
( 15) |                     :                         |
(  5) |     |     |     |     |     |     |     |     |
( 18) |     |     |     |     |     |     |     |     |
(  6) |                     :                         |
";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void CustomSymbolKey()
        {
            TestCourseFormatter("descformatter\\sampleevent2.coursescribe", CourseId(6), true, 
@"      | Sample Event 1                                |   [Sample Event 1]
      |Green Y          |9.4 km           |           |   [Length 9.4 km]
(  1) |start|     |     |  2.8|  8.5|     |     |     |   [Start: open bare rock]
( 20) |    1|  305|     |  2.7|     |  6x7|     | 12.4|   [Stony ground, 6m by 7m (manned)]
(  9) |    2|  211|     |  3.7|  3.7|4x4|5x6|11.15|     |   [Between marshes, 4m by 4m and 5m by 6m]
( 26) |    3|  319|     |  6.2|     |     |     | 12.1|   [Playground equipment (medical)]
(  4) |    4|   32|     |  3.7|     |     |     | 12.1|   [very marshy spot]
( 25) |    5|  309|     |  6.1|  8.7|     |     |     |   [Wet man-made object]
(  5) |    6|   GO| 0.1N|  5.5|  5.2| 10.1|11.1N| 12.3|   [N side of N power line and path crossing (radio)]
( 18) |    7|  303|     |  1.3|     |    4|     |     |   [Reentrant, 4m deep]
( 12) |    8|   74|     |  2.4|     |0.5/2.5|11.15|     |   [Between boulders, 0.5m to 2.5m high]
( 11) |    9|  191|     |  5.2|  5.2| 10.1|11.15|     |   [Between path crossings]
(  7) |   10|  189|     |  1.8|  1.8| 10.2|     |     |   [Small gully junction]
( 10) |   11|  190|     |  5.1|  5.5| 10.1|11.1N|     |   [N side of road and power line crossing]
(  6) |                 14.2: 350 m                   |   [Navigate 350 m to finish funnel]
      |12.1             |medical                      |
      |6.1              |man-made object              |
");
        }

        [TestMethod]
        public void DescriptionText()
        {
            TestCourseFormatter("descformatter\\desctext.ppen", CourseId(6), true, 
@"      | Sample Event 1                                |   [Sample Event 1]
      |Green Y          |9.5 km           |           |   [Length 9.5 km]
(  1) | Get ready to go!                              |   [Get ready to go!]
(  1) |start|     |     |  2.8|  8.5|     |     |     |   [Start: open bare rock]
( 20) |    1|  305|     |  2.7|     |  6x7|     | 12.4|   [Stony ground, 6m by 7m (manned)]
(  9) |    2|  211|     |  3.7|  3.7|4x4|5x6|11.15|     |   [Between marshes, 4m by 4m and 5m by 6m]
(  8) |    3|  210|     | 5.11| 5.20|     |11.15|     |   [Between building and statue]
(  4) | Beware of frogs!                              |   [Beware of frogs!]
(  4) |    4|   32|     |  3.7|     |     |     | 12.1|   [very marshy spot]
( 17) |    5|  302|     | 1.14|     |     |11.1S|     |   [S side of pit]
(  5) |    6|   GO| 0.1N|  5.5|  5.2| 10.1|11.1N| 12.3|   [N side of N power line and path crossing (radio)]
( 18) | Control 303 before                            |   [Control 303 before]
( 18) | Course Control 303 before                     |   [Course Control 303 before]
( 18) |    7|  303|     |  1.3|     |    4|     |     |   [Reentrant, 4m deep]
( 18) | Course Control 303 after                      |   [Course Control 303 after]
( 18) | Control 303 after                             |   [Control 303 after]
( 12) |    8|   74|     |  2.4|     |0.5/2.5|11.15|     |   [Between boulders, 0.5m to 2.5m high]
( 11) |    9|  191|     |  5.2|  5.2| 10.1|11.15|     |   [Between path crossings]
( 11) | Path crossing was easy!                       |   [Path crossing was easy!]
(  7) |   10|  189|     |  1.8|  1.8| 10.2|     |     |   [Small gully junction]
( 10) |   11|  190|     |  5.1|  5.5| 10.1|11.1N|     |   [N side of road and power line crossing]
(  6) |                 14.2: 350 m                   |   [Navigate 350 m to finish funnel]
(  6) | All done!                                     |   [All done!]
");
        }

        [TestMethod]
        public void MapExchangeFullDescription()
        {
            TestCourseFormatter("descformatter\\mapexchange1.ppen", CourseId(6), true, 
@"      | Marymoor WIOL 2                               |   [Marymoor WIOL 2]
      |Course 5         |5.0 km           |           |   [Length 5.0 km]
(  1) |start|     |     |  4.4|     |     |     |     |   [Start: clearing]
( 59) |    1|   59|     |  4.5|     |     |11.1N|     |   [N side of thicket]
( 51) |    2|   51|     |  4.5|     |     |11.1NW|     |   [NW side of thicket]
( 46) |    3|   46|     | 1.12|     |     |11.1NW|     |   [NW side of depression]
( 47) |    4|   47|     |  4.5|     |     |11.1NW|     |   [NW side of thicket]
( 48) |    5|   48|     |  1.9|     |     |11.3NE|     |   [NE part of hill]
( 50) |    6|   50|     | 1.12|     |     |11.3NW|     |   [NW part of depression]
( 56) |    7|   56| 0.1W|  4.5|     |     |11.1E|     |   [E side of W thicket]
( 57) |    8|   57|     |  4.5|     |     |11.1NW|     |   [NW side of thicket]
( 79) |    9|   79|     |  4.8|     |     |11.2W|     |   [W edge of copse]
( 79) |                 13.5: 210 m                   |   [Follow tapes 210 m to map exchange]
( 35) |start|     | 0.1E|  4.5|     |     |11.2E|     |   [Start: E edge of E thicket]
( 37) |   10|   37|     |  4.5|     |     |11.6NW|     |   [NW tip of thicket]
( 36) |   11|   36|     |  4.9|     |     |     |     |   [Lone tree]
( 39) |   12|   39|     |  4.8|     |     |11.6S|     |   [S tip of copse]
( 43) |   13|   43| 0.1E|  3.5|  4.5| 10.1|     |     |   [E ditch and thicket crossing]
( 43) |          13.5control: 0 m                     |   [Map exchange at the control]
( 54) |   14|   54|     |  3.5|  4.5| 10.1|11.2NW|     |   [NW edge of ditch and thicket crossing]
( 54) |          13.5control: 0 m                     |   [Map exchange at the control]
( 41) |   15|   41|     |  4.5|     |     |11.2NW|     |   [NW edge of thicket]
( 42) |   16|   42|     |  4.7|  4.7| 10.2|     |     |   [Vegetation boundary junction]
( 38) |   17|   38|     |  4.3|     |     |11.5E|     |   [E outside corner of forest corner]
(  2) |                 14.3: 60 m                    |   [Navigate 60 m to finish]
");
        }

        [TestMethod]
        public void MapExchangePart1()
        {
            TestCourseFormatter("descformatter\\mapexchange1.ppen", new CourseDesignator(CourseId(6), 0), true,
@"      | Marymoor WIOL 2                               |   [Marymoor WIOL 2]
      |Course 5-1       |5.0 km           |           |   [Length 5.0 km]
(  1) |start|     |     |  4.4|     |     |     |     |   [Start: clearing]
( 59) |    1|   59|     |  4.5|     |     |11.1N|     |   [N side of thicket]
( 51) |    2|   51|     |  4.5|     |     |11.1NW|     |   [NW side of thicket]
( 46) |    3|   46|     | 1.12|     |     |11.1NW|     |   [NW side of depression]
( 47) |    4|   47|     |  4.5|     |     |11.1NW|     |   [NW side of thicket]
( 48) |    5|   48|     |  1.9|     |     |11.3NE|     |   [NE part of hill]
( 50) |    6|   50|     | 1.12|     |     |11.3NW|     |   [NW part of depression]
( 56) |    7|   56| 0.1W|  4.5|     |     |11.1E|     |   [E side of W thicket]
( 57) |    8|   57|     |  4.5|     |     |11.1NW|     |   [NW side of thicket]
( 79) |    9|   79|     |  4.8|     |     |11.2W|     |   [W edge of copse]
( 79) |                 13.5: 210 m                   |   [Follow tapes 210 m to map exchange]
");
        }

        [TestMethod]
        public void MapExchangePart2()
        {
            TestCourseFormatter("descformatter\\mapexchange1.ppen", new CourseDesignator(CourseId(6), 1), true,
@"      | Marymoor WIOL 2                               |   [Marymoor WIOL 2]
      |Course 5-2       |5.0 km           |           |   [Length 5.0 km]
( 35) |start|     | 0.1E|  4.5|     |     |11.2E|     |   [Start: E edge of E thicket]
( 37) |   10|   37|     |  4.5|     |     |11.6NW|     |   [NW tip of thicket]
( 36) |   11|   36|     |  4.9|     |     |     |     |   [Lone tree]
( 39) |   12|   39|     |  4.8|     |     |11.6S|     |   [S tip of copse]
( 43) |   13|   43| 0.1E|  3.5|  4.5| 10.1|     |     |   [E ditch and thicket crossing]
( 43) |          13.5control: 0 m                     |   [Map exchange at the control]
");
        }

        [TestMethod]
        public void MapExchangePart3()
        {
            TestCourseFormatter("descformatter\\mapexchange1.ppen", new CourseDesignator(CourseId(6), 2), true,
@"      | Marymoor WIOL 2                               |   [Marymoor WIOL 2]
      |Course 5-3       |5.0 km           |           |   [Length 5.0 km]
( 43) |   13|   43| 0.1E|  3.5|  4.5| 10.1|     |     |   [E ditch and thicket crossing]
( 54) |   14|   54|     |  3.5|  4.5| 10.1|11.2NW|     |   [NW edge of ditch and thicket crossing]
( 54) |          13.5control: 0 m                     |   [Map exchange at the control]
");
        }

        [TestMethod]
        public void MapExchangePart4()
        {
            TestCourseFormatter("descformatter\\mapexchange1.ppen", new CourseDesignator(CourseId(6), 3), true,
@"      | Marymoor WIOL 2                               |   [Marymoor WIOL 2]
      |Course 5-4       |5.0 km           |           |   [Length 5.0 km]
( 54) |   14|   54|     |  3.5|  4.5| 10.1|11.2NW|     |   [NW edge of ditch and thicket crossing]
( 41) |   15|   41|     |  4.5|     |     |11.2NW|     |   [NW edge of thicket]
( 42) |   16|   42|     |  4.7|  4.7| 10.2|     |     |   [Vegetation boundary junction]
( 38) |   17|   38|     |  4.3|     |     |11.5E|     |   [E outside corner of forest corner]
(  2) |                 14.3: 60 m                    |   [Navigate 60 m to finish]
");
        }

        [TestMethod]
        public void MapExchangeWithFlaggedLegsFullDescription()
        {
            TestCourseFormatter("descformatter\\mapexchange2.ppen", CourseId(6), true,
@"      | Marymoor WIOL 2                               |   [Marymoor WIOL 2]
      |Course 5         |5.0 km           |           |   [Length 5.0 km]
(  1) |start|     |     |  4.4|     |     |     |     |   [Start: clearing]
(  1) |                 13.2: 190 m                   |   [Follow tapes 190 m between controls]
( 59) |    1|   59|     |  4.5|     |     |11.1N|     |   [N side of thicket]
( 51) |    2|   51|     |  4.5|     |     |11.1NW|     |   [NW side of thicket]
( 51) |                 13.2: 330 m                   |   [Follow tapes 330 m between controls]
( 46) |    3|   46|     | 1.12|     |     |11.1NW|     |   [NW side of depression]
( 47) |    4|   47|     |  4.5|     |     |11.1NW|     |   [NW side of thicket]
( 48) |    5|   48|     |  1.9|     |     |11.3NE|     |   [NE part of hill]
( 50) |    6|   50|     | 1.12|     |     |11.3NW|     |   [NW part of depression]
( 56) |    7|   56| 0.1W|  4.5|     |     |11.1E|     |   [E side of W thicket]
( 57) |    8|   57|     |  4.5|     |     |11.1NW|     |   [NW side of thicket]
( 79) |    9|   79|     |  4.8|     |     |11.2W|     |   [W edge of copse]
( 79) |                 13.5: 210 m                   |   [Follow tapes 210 m to map exchange]
( 35) |start|     | 0.1E|  4.5|     |     |11.2E|     |   [Start: E edge of E thicket]
( 35) |                 13.2: 130 m                   |   [Follow tapes 130 m between controls]
( 37) |   10|   37|     |  4.5|     |     |11.6NW|     |   [NW tip of thicket]
( 36) |   11|   36|     |  4.9|     |     |     |     |   [Lone tree]
( 39) |   12|   39|     |  4.8|     |     |11.6S|     |   [S tip of copse]
( 43) |   13|   43| 0.1E|  3.5|  4.5| 10.1|     |     |   [E ditch and thicket crossing]
( 43) |          13.5control: 0 m                     |   [Map exchange at the control]
( 43) |                 13.2: 400 m                   |   [Follow tapes 400 m between controls]
( 54) |   14|   54|     |  3.5|  4.5| 10.1|11.2NW|     |   [NW edge of ditch and thicket crossing]
( 54) |          13.5control: 0 m                     |   [Map exchange at the control]
( 54) |                 13.2: 260 m                   |   [Follow tapes 260 m between controls]
( 41) |   15|   41|     |  4.5|     |     |11.2NW|     |   [NW edge of thicket]
( 42) |   16|   42|     |  4.7|  4.7| 10.2|     |     |   [Vegetation boundary junction]
( 38) |   17|   38|     |  4.3|     |     |11.5E|     |   [E outside corner of forest corner]
(  2) |                 14.1: 80 m                    |   [Follow tapes 80 m to finish]
");
        }
    }


}


#endif //TEST
