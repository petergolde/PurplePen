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
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

using PurplePen.MapModel;

namespace PurplePen.Tests
{
    [TestClass]
    public class QueryEventTests: TestFixtureBase
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
        public void FindCode()
        {
            Setup("queryevent\\sampleevent1.coursescribe");
            Assert.AreEqual(14, QueryEvent.FindCode(eventDB, "291").id);
            Assert.AreEqual(0, QueryEvent.FindCode(eventDB, "MN").id);
        }

        [TestMethod]
        public void IsCodeIsUse()
        {
            Setup("queryevent\\sampleevent1.coursescribe");
            Assert.IsTrue(QueryEvent.IsCodeInUse(eventDB, "291"));
            Assert.IsFalse(QueryEvent.IsCodeInUse(eventDB, "MN"));
        }

        [TestMethod]
        public void CoursesUsingControl()
        {
            Setup("queryevent\\sampleevent1.coursescribe");

            Id<Course>[] result;

            result = QueryEvent.CoursesUsingControl(eventDB, ControlId(23));
            Assert.AreEqual(0, result.Length);

            result = QueryEvent.CoursesUsingControl(eventDB, ControlId(6));
            TestUtil.TestEnumerableAnyOrder(result, new Id<Course>[] { CourseId(1), CourseId(4), CourseId(3), CourseId(5), CourseId(6) });
        }

        [TestMethod]
        public void ShouldWarnAboutMovingCourse()
        {
            Setup("queryevent\\sampleevent1.coursescribe");

            Id<Course>[] result;

            result = QueryEvent.ShouldWarnAboutMovingControl(eventDB, CourseId(6), CourseControlId(205), new PointF(25, 30));
            TestUtil.TestEnumerableAnyOrder(result, new Id<Course>[] { CourseId(3), CourseId(4), CourseId(5) });

            result = QueryEvent.ShouldWarnAboutMovingControl(eventDB, CourseId(6), CourseControlId(205), new PointF(20, -10));
            Assert.IsNull(result);

            result = QueryEvent.ShouldWarnAboutMovingControl(eventDB, CourseId(4), CourseControlId(19), new PointF(30, -25));
            Assert.IsNull(result);

            Setup("queryevent\\marymoor5.ppen");

            result = QueryEvent.ShouldWarnAboutMovingControl(eventDB, CourseId(6), CourseControlId(612), new PointF(30, -25));
            Assert.IsNull(result);


        }

        [TestMethod]
        public void ControlsUnusedInCourses()
        {
            Setup("queryevent\\marymoor5.ppen");
            Id<ControlPoint>[] expected = { ControlId(83), ControlId(88), ControlId(87), ControlId(82), ControlId(81), ControlId(84) };

            List<Id<ControlPoint>> result = QueryEvent.ControlsUnusedInCourses(eventDB);

            CollectionAssert.AreEquivalent(expected, result);
        }

        [TestMethod]
        public void CoursesUsingLeg()
        {
            Setup("queryevent\\marymoor.coursescribe");

            Id<Course>[] result;

            result = QueryEvent.CoursesUsingLeg(eventDB, ControlId(44), ControlId(70));
            TestUtil.TestEnumerableAnyOrder(result, new Id<Course>[] { });

            result = QueryEvent.CoursesUsingLeg(eventDB, ControlId(72), ControlId(70));
            TestUtil.TestEnumerableAnyOrder(result, new Id<Course>[] { CourseId(2) });

            result = QueryEvent.CoursesUsingLeg(eventDB, ControlId(48), ControlId(50));
            TestUtil.TestEnumerableAnyOrder(result, new Id<Course>[] { CourseId(4), CourseId(6) });
        }

        [TestMethod]
        public void CoursesUsingLeg2()
        {
            Setup("queryevent\\sampleevent1.coursescribe");

            Id<Course>[] result;

            result = QueryEvent.CoursesUsingLeg(eventDB, ControlId(1), ControlId(11));
            TestUtil.TestEnumerableAnyOrder(result, new Id<Course>[] { CourseId(4) });  // should not include score courses!
        }

        [TestMethod]
        public void EnumCourseControlIds()
        {
            Setup("queryevent\\sampleevent1.coursescribe");

            List<Id<CourseControl>> result = new List<Id<CourseControl>>();

            foreach (Id<CourseControl> id in QueryEvent.EnumCourseControlIds(eventDB, Designator(1)))
                result.Add(id);

            TestUtil.TestEnumerableAnyOrder(result, new Id<CourseControl>[] { CourseControlId(1), CourseControlId(2), CourseControlId(3), CourseControlId(4), CourseControlId(5), CourseControlId(6), CourseControlId(7) });
        }

        [TestMethod]
        public void EnumCourseControlIdsVariation()
        {
            Setup("queryevent\\variations.ppen");
            List<Id<CourseControl>> result;

            result = QueryEvent.EnumCourseControlIds(eventDB, Designator(1)).ToList();
            CollectionAssert.AreEquivalent(result, new[] {
                CourseControlId(1), CourseControlId(2), CourseControlId(3), CourseControlId(4), CourseControlId(5),
                CourseControlId(6), CourseControlId(7), CourseControlId(8), CourseControlId(9), CourseControlId(10),
                CourseControlId(11), CourseControlId(12), CourseControlId(13), CourseControlId(14), CourseControlId(15),
                CourseControlId(16), CourseControlId(17), CourseControlId(18), CourseControlId(19), CourseControlId(20),
                CourseControlId(21), CourseControlId(22), CourseControlId(23), CourseControlId(24), CourseControlId(25),
                CourseControlId(26), CourseControlId(27), CourseControlId(28), CourseControlId(29), CourseControlId(30),
            });

            /////////////////////////////////////////////////////////////////

            VariationInfo.VariationPath variationPath = new VariationInfo.VariationPath(new[] {
                CourseControlId(24)
            });
            VariationInfo variationInfo = new VariationInfo("B", variationPath);

            result = QueryEvent.EnumCourseControlIds(eventDB, new CourseDesignator(CourseId(1), variationInfo)).ToList();
            CollectionAssert.AreEqual(result, new[] {
                CourseControlId(1), CourseControlId(24), CourseControlId(12), CourseControlId(10), CourseControlId(11)
            });

            /////////////////////////////////////////////////////////////////

            variationPath = new VariationInfo.VariationPath(new[] {
                CourseControlId(2),
                CourseControlId(27),
                CourseControlId(30),
                CourseControlId(26),
                CourseControlId(25),
                CourseControlId(4),
                CourseControlId(28),
            });
            variationInfo = new VariationInfo("AEFDCI", variationPath);

            // TODO VARIATIONS
            result = QueryEvent.EnumCourseControlIds(eventDB, new CourseDesignator(CourseId(1), variationInfo)).ToList();
            CollectionAssert.AreEqual(result, new[] {
                CourseControlId(1), CourseControlId(2), CourseControlId(3), CourseControlId(27), CourseControlId(19),
                CourseControlId(30), CourseControlId(20), CourseControlId(23), CourseControlId(26), CourseControlId(17),
                CourseControlId(18), CourseControlId(25), CourseControlId(15), CourseControlId(16), CourseControlId(4),
                CourseControlId(5), CourseControlId(6), CourseControlId(7), CourseControlId(8), CourseControlId(28),
                CourseControlId(13), CourseControlId(10), CourseControlId(11)
            });


        }

        // Test EnumCourseControlIds for map exchanges.
        [TestMethod]
        public void EnumCourseControlIdsMapExchange()
        {
            Setup("queryevent\\mapexchange1.ppen");

            List<Id<CourseControl>> result = new List<Id<CourseControl>>();

            foreach (Id<CourseControl> id in QueryEvent.EnumCourseControlIds(eventDB, Designator(6)))
                result.Add(id);

            CollectionAssert.AreEqual(result, new Id<CourseControl>[] { CourseControlId(601), CourseControlId(602), CourseControlId(603), CourseControlId(604), CourseControlId(605),
                                                                              CourseControlId(606), CourseControlId(607), CourseControlId(608), CourseControlId(609), CourseControlId(610),
                                                                              CourseControlId(611), CourseControlId(612), CourseControlId(613), CourseControlId(614), CourseControlId(615),
                                                                              CourseControlId(616), CourseControlId(617), CourseControlId(618), CourseControlId(619), CourseControlId(620)
                });

            result.Clear();
            foreach (Id<CourseControl> id in QueryEvent.EnumCourseControlIds(eventDB, Designator(6, 0)))
                result.Add(id);

            CollectionAssert.AreEqual(result, new Id<CourseControl>[] { CourseControlId(601), CourseControlId(602), CourseControlId(603), CourseControlId(604), CourseControlId(605),
                                                                              CourseControlId(606), CourseControlId(607), CourseControlId(608), CourseControlId(609), CourseControlId(610),
                                                                              CourseControlId(611)
                });

            result.Clear();

            foreach (Id<CourseControl> id in QueryEvent.EnumCourseControlIds(eventDB, Designator(6, 1)))
                result.Add(id);

            CollectionAssert.AreEqual(result, new Id<CourseControl>[] { CourseControlId(611), CourseControlId(612), CourseControlId(613), CourseControlId(614), CourseControlId(615)
                });

            result.Clear();

            foreach (Id<CourseControl> id in QueryEvent.EnumCourseControlIds(eventDB, Designator(6, 2)))
                result.Add(id);

            CollectionAssert.AreEqual(result, new Id<CourseControl>[] { CourseControlId(615), CourseControlId(616)
                });

            result.Clear();

            foreach (Id<CourseControl> id in QueryEvent.EnumCourseControlIds(eventDB, Designator(6, 3)))
                result.Add(id);

            CollectionAssert.AreEqual(result, new Id<CourseControl>[] {  CourseControlId(616), CourseControlId(617), CourseControlId(618), CourseControlId(619), CourseControlId(620)
                });

            result.Clear();
        }


        [TestMethod]
        public void EnumLegs()
        {
            Setup("queryevent\\sampleevent1.coursescribe");

            List<QueryEvent.LegInfo> result = new List<QueryEvent.LegInfo>();

            foreach (QueryEvent.LegInfo id in QueryEvent.EnumLegs(eventDB, Designator(1)))
                result.Add(id);

            CollectionAssert.AreEquivalent(result, new QueryEvent.LegInfo[]
            {
                new QueryEvent.LegInfo(CourseControlId(1), CourseControlId(2), 1),
                new QueryEvent.LegInfo(CourseControlId(7), CourseControlId(4), 0.5),
                new QueryEvent.LegInfo(CourseControlId(4), CourseControlId(5), 0.5),
                new QueryEvent.LegInfo(CourseControlId(2), CourseControlId(3), 0.5),
                new QueryEvent.LegInfo(CourseControlId(3), CourseControlId(5), 0.5),
                new QueryEvent.LegInfo(CourseControlId(5), CourseControlId(6), 1),
            });

            // Score course has no legs in it.
            foreach (QueryEvent.LegInfo id in QueryEvent.EnumLegs(eventDB, Designator(5)))
                Assert.Fail("Score course should have no legs in it");

            // Empty course has no legs in it.
            foreach (QueryEvent.LegInfo id in QueryEvent.EnumLegs(eventDB, Designator(2)))
                Assert.Fail("Empty course should have no legs in it");
        }

        [TestMethod]
        public void EnumLegsVariations()
        {
            Setup("queryevent\\variations.ppen");

            List<QueryEvent.LegInfo> result;

            VariationInfo.VariationPath variationPath = new VariationInfo.VariationPath(new[] {
                CourseControlId(2),
                CourseControlId(27),
                CourseControlId(30),
                CourseControlId(26),
                CourseControlId(25),
                CourseControlId(4),
                CourseControlId(28),
            });
            VariationInfo variationInfo = new VariationInfo("AEFDCI", variationPath);

            result = QueryEvent.EnumLegs(eventDB, new CourseDesignator(CourseId(1), variationInfo)).ToList();

            CollectionAssert.AreEqual(result, new[] {
                new QueryEvent.LegInfo(CourseControlId(1), CourseControlId(2)),
                new QueryEvent.LegInfo(CourseControlId(2), CourseControlId(3)),
                new QueryEvent.LegInfo(CourseControlId(3), CourseControlId(4)),
                new QueryEvent.LegInfo(CourseControlId(27), CourseControlId(19)),
                new QueryEvent.LegInfo(CourseControlId(19), CourseControlId(21)),
                new QueryEvent.LegInfo(CourseControlId(30), CourseControlId(20)),
                new QueryEvent.LegInfo(CourseControlId(20), CourseControlId(23)),
                new QueryEvent.LegInfo(CourseControlId(23), CourseControlId(4)),
                new QueryEvent.LegInfo(CourseControlId(26), CourseControlId(17)),
                new QueryEvent.LegInfo(CourseControlId(17), CourseControlId(18)),
                new QueryEvent.LegInfo(CourseControlId(18), CourseControlId(4)),
                new QueryEvent.LegInfo(CourseControlId(25), CourseControlId(15)),
                new QueryEvent.LegInfo(CourseControlId(15), CourseControlId(16)),
                new QueryEvent.LegInfo(CourseControlId(16), CourseControlId(4)),
                new QueryEvent.LegInfo(CourseControlId(4), CourseControlId(5)),
                new QueryEvent.LegInfo(CourseControlId(5), CourseControlId(6)),
                new QueryEvent.LegInfo(CourseControlId(6), CourseControlId(7)),
                new QueryEvent.LegInfo(CourseControlId(7), CourseControlId(8)),
                new QueryEvent.LegInfo(CourseControlId(8), CourseControlId(9)),
                new QueryEvent.LegInfo(CourseControlId(28), CourseControlId(13)),
                new QueryEvent.LegInfo(CourseControlId(13), CourseControlId(10)),
                new QueryEvent.LegInfo(CourseControlId(10), CourseControlId(11))
            });


        }

        [TestMethod]
        public void EnumLegsMapExchange()
        {
            Setup("queryevent\\mapexchange1.ppen");

            List<QueryEvent.LegInfo> result = new List<QueryEvent.LegInfo>();

            foreach (QueryEvent.LegInfo id in QueryEvent.EnumLegs(eventDB, Designator(6)))
                result.Add(id);

            CollectionAssert.AreEqual(result, new QueryEvent.LegInfo[]
            {
                new QueryEvent.LegInfo(CourseControlId(601), CourseControlId(602)),
                new QueryEvent.LegInfo(CourseControlId(602), CourseControlId(603)),
                new QueryEvent.LegInfo(CourseControlId(603), CourseControlId(604)),
                new QueryEvent.LegInfo(CourseControlId(604), CourseControlId(605)),
                new QueryEvent.LegInfo(CourseControlId(605), CourseControlId(606)),
                new QueryEvent.LegInfo(CourseControlId(606), CourseControlId(607)),
                new QueryEvent.LegInfo(CourseControlId(607), CourseControlId(608)),
                new QueryEvent.LegInfo(CourseControlId(608), CourseControlId(609)),
                new QueryEvent.LegInfo(CourseControlId(609), CourseControlId(610)),
                new QueryEvent.LegInfo(CourseControlId(610), CourseControlId(611)),
                new QueryEvent.LegInfo(CourseControlId(611), CourseControlId(612)),
                new QueryEvent.LegInfo(CourseControlId(612), CourseControlId(613)),
                new QueryEvent.LegInfo(CourseControlId(613), CourseControlId(614)),
                new QueryEvent.LegInfo(CourseControlId(614), CourseControlId(615)),
                new QueryEvent.LegInfo(CourseControlId(615), CourseControlId(616)),
                new QueryEvent.LegInfo(CourseControlId(616), CourseControlId(617)),
                new QueryEvent.LegInfo(CourseControlId(617), CourseControlId(618)),
                new QueryEvent.LegInfo(CourseControlId(618), CourseControlId(619)),
                new QueryEvent.LegInfo(CourseControlId(619), CourseControlId(620)),
            });

            result.Clear();

            foreach (QueryEvent.LegInfo id in QueryEvent.EnumLegs(eventDB, Designator(6, 0)))
                result.Add(id);

            CollectionAssert.AreEqual(result, new QueryEvent.LegInfo[]
            {
                new QueryEvent.LegInfo(CourseControlId(601), CourseControlId(602)),
                new QueryEvent.LegInfo(CourseControlId(602), CourseControlId(603)),
                new QueryEvent.LegInfo(CourseControlId(603), CourseControlId(604)),
                new QueryEvent.LegInfo(CourseControlId(604), CourseControlId(605)),
                new QueryEvent.LegInfo(CourseControlId(605), CourseControlId(606)),
                new QueryEvent.LegInfo(CourseControlId(606), CourseControlId(607)),
                new QueryEvent.LegInfo(CourseControlId(607), CourseControlId(608)),
                new QueryEvent.LegInfo(CourseControlId(608), CourseControlId(609)),
                new QueryEvent.LegInfo(CourseControlId(609), CourseControlId(610)),
                new QueryEvent.LegInfo(CourseControlId(610), CourseControlId(611)),
            });

            result.Clear();
            foreach (QueryEvent.LegInfo id in QueryEvent.EnumLegs(eventDB, Designator(6, 1)))
                result.Add(id);

            CollectionAssert.AreEqual(result, new QueryEvent.LegInfo[]
            {
                new QueryEvent.LegInfo(CourseControlId(611), CourseControlId(612)),
                new QueryEvent.LegInfo(CourseControlId(612), CourseControlId(613)),
                new QueryEvent.LegInfo(CourseControlId(613), CourseControlId(614)),
                new QueryEvent.LegInfo(CourseControlId(614), CourseControlId(615)),
            });

            result.Clear();
            foreach (QueryEvent.LegInfo id in QueryEvent.EnumLegs(eventDB, Designator(6, 2)))
                result.Add(id);

            CollectionAssert.AreEqual(result, new QueryEvent.LegInfo[]
            {
                new QueryEvent.LegInfo(CourseControlId(615), CourseControlId(616)),
            });

            result.Clear();
            foreach (QueryEvent.LegInfo id in QueryEvent.EnumLegs(eventDB, Designator(6, 3)))
                result.Add(id);

            CollectionAssert.AreEqual(result, new QueryEvent.LegInfo[]
            {
                new QueryEvent.LegInfo(CourseControlId(616), CourseControlId(617)),
                new QueryEvent.LegInfo(CourseControlId(617), CourseControlId(618)),
                new QueryEvent.LegInfo(CourseControlId(618), CourseControlId(619)),
                new QueryEvent.LegInfo(CourseControlId(619), CourseControlId(620)),
            });

            result.Clear();
        }

        [TestMethod]
        public void EnumLegsRelay()
        {
            Setup("queryevent\\relay.ppen");

            List<QueryEvent.LegInfo> result = new List<QueryEvent.LegInfo>();

            foreach (QueryEvent.LegInfo li in QueryEvent.EnumLegs(eventDB, Designator(4)))
                result.Add(li);

            CollectionAssert.AreEqual(result, new QueryEvent.LegInfo[]
            {
                new QueryEvent.LegInfo(CourseControlId(48), CourseControlId(52)),
                new QueryEvent.LegInfo(CourseControlId(49), CourseControlId(50), 0.5),
                new QueryEvent.LegInfo(CourseControlId(50), CourseControlId(51), 0.5),
                new QueryEvent.LegInfo(CourseControlId(51), CourseControlId(55), 0.5),
                new QueryEvent.LegInfo(CourseControlId(52), CourseControlId(53), 0.5),
                new QueryEvent.LegInfo(CourseControlId(53), CourseControlId(54), 0.5),
                new QueryEvent.LegInfo(CourseControlId(54), CourseControlId(55), 0.5),
                new QueryEvent.LegInfo(CourseControlId(58), CourseControlId(57), 0.33333333333333331),
                new QueryEvent.LegInfo(CourseControlId(59), CourseControlId(62), 0.33333333333333331),
                new QueryEvent.LegInfo(CourseControlId(62), CourseControlId(57), 0.33333333333333331),
                new QueryEvent.LegInfo(CourseControlId(55), CourseControlId(60), 0.33333333333333331),
                new QueryEvent.LegInfo(CourseControlId(60), CourseControlId(57), 0.33333333333333331),
                new QueryEvent.LegInfo(CourseControlId(57), CourseControlId(56)),
            });
        }

        [TestMethod]
        public void FindClosestLeg()
        {
            Setup("queryevent\\sampleevent1.coursescribe");

            QueryEvent.LegInfo result = QueryEvent.FindClosestLeg(eventDB, Designator(4), new PointF(-8.6F, 0.5F));
            Assert.AreEqual(CourseControlId(11), result.courseControlId1);
            Assert.AreEqual(CourseControlId(12), result.courseControlId2);

            result = QueryEvent.FindClosestLeg(eventDB, Designator(4), new PointF(-57F, 37F));
            Assert.AreEqual(CourseControlId(18), result.courseControlId1);
            Assert.AreEqual(CourseControlId(19), result.courseControlId2);

            result = QueryEvent.FindClosestLeg(eventDB, Designator(4), new PointF(-37F, 60F));
            Assert.AreEqual(CourseControlId(17), result.courseControlId1);
            Assert.AreEqual(CourseControlId(18), result.courseControlId2);

            result = QueryEvent.FindClosestLeg(eventDB, Designator(4), new PointF(9.8F, 4.5F));
            Assert.AreEqual(CourseControlId(12), result.courseControlId1);
            Assert.AreEqual(CourseControlId(13), result.courseControlId2);



            // Score course should have no legs.
            result = QueryEvent.FindClosestLeg(eventDB, Designator(5), new PointF(-8.6F, 0.5F));
            Assert.IsTrue(result.courseControlId1.IsNone);
            Assert.IsTrue(result.courseControlId2.IsNone);

            // Empty course should have no legs.
            result = QueryEvent.FindClosestLeg(eventDB, Designator(2), new PointF(-8.6F, 0.5F));
            Assert.IsTrue(result.courseControlId1.IsNone);
            Assert.IsTrue(result.courseControlId2.IsNone);
        }

        [TestMethod]
        public void CourseIsForked()
        {
            Setup("queryevent\\variations.ppen");
            Assert.IsTrue(QueryEvent.CourseIsForked(eventDB, Designator(1)));

            Setup("queryevent\\mapexchange1.ppen");
            Assert.IsFalse(QueryEvent.CourseIsForked(eventDB, Designator(1)));

        }

        // Test FindClosesLeg for map exchanges.
        [TestMethod]
        public void FindClosestLegMapExchange()
        {
            Setup("queryevent\\mapexchange1.ppen");

            QueryEvent.LegInfo result = QueryEvent.FindClosestLeg(eventDB, Designator(6), new PointF(4.4F, 17.6F));
            Assert.AreEqual(CourseControlId(604), result.courseControlId1);
            Assert.AreEqual(CourseControlId(605), result.courseControlId2);

            result = QueryEvent.FindClosestLeg(eventDB, Designator(6), new PointF(63.6F, 14.2F));
            Assert.AreEqual(CourseControlId(615), result.courseControlId1);
            Assert.AreEqual(CourseControlId(616), result.courseControlId2);

            result = QueryEvent.FindClosestLeg(eventDB, Designator(6), new PointF(42.5F, -5.4F));
            Assert.AreEqual(CourseControlId(618), result.courseControlId1);
            Assert.AreEqual(CourseControlId(619), result.courseControlId2);

            result = QueryEvent.FindClosestLeg(eventDB, Designator(6, 0), new PointF(68.7F, 19.7F));
            Assert.AreEqual(CourseControlId(607), result.courseControlId1);
            Assert.AreEqual(CourseControlId(608), result.courseControlId2);

            result = QueryEvent.FindClosestLeg(eventDB, Designator(6, 1), new PointF(68.7F, 19.7F));
            Assert.AreEqual(CourseControlId(614), result.courseControlId1);
            Assert.AreEqual(CourseControlId(615), result.courseControlId2);

            result = QueryEvent.FindClosestLeg(eventDB, Designator(6, 2), new PointF(68.7F, 19.7F));
            Assert.AreEqual(CourseControlId(615), result.courseControlId1);
            Assert.AreEqual(CourseControlId(616), result.courseControlId2);

            result = QueryEvent.FindClosestLeg(eventDB, Designator(6, 3), new PointF(68.7F, 19.7F));
            Assert.AreEqual(CourseControlId(616), result.courseControlId1);
            Assert.AreEqual(CourseControlId(617), result.courseControlId2);
        }

        [TestMethod]
        public void DoesCourseControlPrecede()
        {
            bool result;

            Setup("queryevent\\mapexchange1.ppen");

            result = QueryEvent.DoesCourseControlPrecede(eventDB, Designator(6), CourseControlId(601), CourseControlId(613));
            Assert.IsTrue(result);

            result = QueryEvent.DoesCourseControlPrecede(eventDB, Designator(6), CourseControlId(613), CourseControlId(601));
            Assert.IsFalse(result);

            result = QueryEvent.DoesCourseControlPrecede(eventDB, Designator(6), CourseControlId(603), CourseControlId(604));
            Assert.IsTrue(result);

            result = QueryEvent.DoesCourseControlPrecede(eventDB, Designator(6), CourseControlId(604), CourseControlId(603));
            Assert.IsFalse(result);

            result = QueryEvent.DoesCourseControlPrecede(eventDB, Designator(6), CourseControlId(610), CourseControlId(620));
            Assert.IsTrue(result);

            result = QueryEvent.DoesCourseControlPrecede(eventDB, Designator(6), CourseControlId(620), CourseControlId(610));
            Assert.IsFalse(result);

            result = QueryEvent.DoesCourseControlPrecede(eventDB, Designator(6), CourseControlId(603), CourseControlId(603));
            Assert.IsFalse(result);

            result = QueryEvent.DoesCourseControlPrecede(eventDB, Designator(6), CourseControlId(204), CourseControlId(603));
            Assert.IsFalse(result);

            result = QueryEvent.DoesCourseControlPrecede(eventDB, Designator(6), CourseControlId(603), CourseControlId(204));
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void GetCourseControlsInCourse()
        {
            Setup("queryevent\\sampleevent1.coursescribe");

            Id<CourseControl>[] result;

            result = QueryEvent.GetCourseControlsInCourse(eventDB, Designator(1), ControlId(8));
            TestUtil.TestEnumerableAnyOrder(result, new Id<CourseControl>[] { CourseControlId(4) });
            result = QueryEvent.GetCourseControlsInCourse(eventDB, Designator(1), ControlId(19));
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void GetCourseControlsInCourseMapExchange()
        {
            Setup("queryevent\\mapexchange2.ppen");

            Id<CourseControl>[] result;

            result = QueryEvent.GetCourseControlsInCourse(eventDB, Designator(6), ControlId(48));
            CollectionAssert.AreEqual(result, new Id<CourseControl>[] { CourseControlId(606), CourseControlId(623), CourseControlId(626), CourseControlId(628), CourseControlId(629) });
            result = QueryEvent.GetCourseControlsInCourse(eventDB, Designator(6, 0), ControlId(48));
            CollectionAssert.AreEqual(result, new Id<CourseControl>[] { CourseControlId(606), CourseControlId(623), CourseControlId(626) });
            result = QueryEvent.GetCourseControlsInCourse(eventDB, Designator(6, 1), ControlId(48));
            CollectionAssert.AreEqual(result, new Id<CourseControl>[] { CourseControlId(628) });
            result = QueryEvent.GetCourseControlsInCourse(eventDB, Designator(6, 2), ControlId(48));
            CollectionAssert.AreEqual(result, new Id<CourseControl>[] { });
            result = QueryEvent.GetCourseControlsInCourse(eventDB, Designator(6, 3), ControlId(48));
            CollectionAssert.AreEqual(result, new Id<CourseControl>[] { CourseControlId(629) });
        }

        [TestMethod]
        public void CourseUsesControl()
        {
            Setup("queryevent\\sampleevent1.coursescribe");

            Assert.IsTrue(QueryEvent.CourseUsesControl(eventDB, Designator(1), ControlId(8)));
            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(2), ControlId(17)));
            Assert.IsTrue(QueryEvent.CourseUsesControl(eventDB, Designator(4), ControlId(1)));
            Assert.IsTrue(QueryEvent.CourseUsesControl(eventDB, Designator(4), ControlId(6)));
            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(4), ControlId(8)));
        }

        [TestMethod]
        public void CourseUsesControlMapExchange()
        {
            Setup("queryevent\\mapexchange1.ppen");

            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(6), ControlId(88)));
            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(6, 0), ControlId(88)));
            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(6, 1), ControlId(88)));
            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(6, 2), ControlId(88)));
            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(6, 3), ControlId(88)));

            Assert.IsTrue(QueryEvent.CourseUsesControl(eventDB, Designator(6), ControlId(35)));
            Assert.IsTrue(QueryEvent.CourseUsesControl(eventDB, Designator(6, 0), ControlId(35)));
            Assert.IsTrue(QueryEvent.CourseUsesControl(eventDB, Designator(6, 1), ControlId(35)));
            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(6, 2), ControlId(35)));
            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(6, 3), ControlId(35)));

            Assert.IsTrue(QueryEvent.CourseUsesControl(eventDB, Designator(6), ControlId(43)));
            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(6, 0), ControlId(43)));
            Assert.IsTrue(QueryEvent.CourseUsesControl(eventDB, Designator(6, 1), ControlId(43)));
            Assert.IsTrue(QueryEvent.CourseUsesControl(eventDB, Designator(6, 2), ControlId(43)));
            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(6, 3), ControlId(43)));

            Assert.IsTrue(QueryEvent.CourseUsesControl(eventDB, Designator(6), ControlId(54)));
            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(6, 0), ControlId(54)));
            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(6, 1), ControlId(54)));
            Assert.IsTrue(QueryEvent.CourseUsesControl(eventDB, Designator(6, 2), ControlId(54)));
            Assert.IsTrue(QueryEvent.CourseUsesControl(eventDB, Designator(6, 3), ControlId(54)));

            Assert.IsTrue(QueryEvent.CourseUsesControl(eventDB, Designator(6), ControlId(1)));
            Assert.IsTrue(QueryEvent.CourseUsesControl(eventDB, Designator(6, 0), ControlId(1)));
            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(6, 1), ControlId(1)));
            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(6, 2), ControlId(1)));
            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(6, 3), ControlId(1)));

            Assert.IsTrue(QueryEvent.CourseUsesControl(eventDB, Designator(6), ControlId(2)));
            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(6, 0), ControlId(2)));
            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(6, 1), ControlId(2)));
            Assert.IsFalse(QueryEvent.CourseUsesControl(eventDB, Designator(6, 2), ControlId(2)));
            Assert.IsTrue(QueryEvent.CourseUsesControl(eventDB, Designator(6, 3), ControlId(2)));
        }

        [TestMethod]
        public void IsPreferredCode()
        {
            bool legal;
            string reason;

            Setup("queryevent\\sampleevent1.coursescribe");

            legal = QueryEvent.IsPreferredControlCode(eventDB, "31", out reason);
            Assert.IsTrue(legal);
            Assert.IsNull(reason);

            legal = QueryEvent.IsPreferredControlCode(eventDB, "101", out reason);
            Assert.IsTrue(legal);
            Assert.IsNull(reason);

            legal = QueryEvent.IsPreferredControlCode(eventDB, "XX", out reason);
            Assert.IsTrue(legal);
            Assert.IsNull(reason);

            legal = QueryEvent.IsPreferredControlCode(eventDB, "R12", out reason);
            Assert.IsTrue(legal);
            Assert.IsNull(reason);

            // Under 31 is no longer flagged as non-preferred.
            legal = QueryEvent.IsPreferredControlCode(eventDB, "30", out reason);
            Assert.IsTrue(legal);
            Assert.IsNull(reason);

            legal = QueryEvent.IsPreferredControlCode(eventDB, "051", out reason);
            Assert.IsTrue(legal);
            Assert.AreEqual(reason, MiscText.CodeBeginsWithZero);

            legal = QueryEvent.IsPreferredControlCode(eventDB, "66", out reason);
            Assert.IsTrue(legal);
            Assert.AreEqual(reason, MiscText.CodeCouldBeUpsideDown);

            legal = QueryEvent.IsPreferredControlCode(eventDB, "161", out reason);
            Assert.IsTrue(legal);
            Assert.AreEqual(reason, MiscText.CodeCouldBeUpsideDown);

            legal = QueryEvent.IsPreferredControlCode(eventDB, "Z", out reason);
            Assert.IsTrue(legal);
            Assert.IsNull(reason);

            // Under 31 is no longer flagged as non-preferred.
            legal = QueryEvent.IsPreferredControlCode(eventDB, "1", out reason);
            Assert.IsTrue(legal);
            Assert.IsNull(reason);

            legal = QueryEvent.IsPreferredControlCode(eventDB, "1234", out reason);
            Assert.IsFalse(legal);
            Assert.IsFalse(legal, MiscText.CodeBadLength);

            legal = QueryEvent.IsPreferredControlCode(eventDB, " X", out reason);
            Assert.IsFalse(legal);
            Assert.AreEqual(reason, MiscText.CodeContainsSpace);

            undomgr.BeginCommand(555, "Change auto numbering");
            ChangeEvent.ChangeAutoNumbering(eventDB, 31, false);  // allow invertible codes.
            undomgr.EndCommand(555);
            legal = QueryEvent.IsPreferredControlCode(eventDB, "66", out reason);
            Assert.IsTrue(legal);
            Assert.IsNull(reason);
        }

        [TestMethod]
        public void IsLegalCode()
        {
            bool legal;
            string reason;

            legal = QueryEvent.IsLegalControlCode("31", out reason);
            Assert.IsTrue(legal);
            Assert.IsNull(reason);

            legal = QueryEvent.IsLegalControlCode("101", out reason);
            Assert.IsTrue(legal);
            Assert.IsNull(reason);

            legal = QueryEvent.IsLegalControlCode("XX", out reason);
            Assert.IsTrue(legal);
            Assert.IsNull(reason);

            legal = QueryEvent.IsLegalControlCode("R12", out reason);
            Assert.IsTrue(legal);
            Assert.IsNull(reason);

            legal = QueryEvent.IsLegalControlCode("30", out reason);
            Assert.IsTrue(legal);
            Assert.IsNull(reason);

            legal = QueryEvent.IsLegalControlCode("051", out reason);
            Assert.IsTrue(legal);
            Assert.IsNull(reason);

            legal = QueryEvent.IsLegalControlCode("66", out reason);
            Assert.IsTrue(legal);
            Assert.IsNull(reason);

            legal = QueryEvent.IsLegalControlCode("161", out reason);
            Assert.IsTrue(legal);
            Assert.IsNull(reason);

            legal = QueryEvent.IsLegalControlCode("Z", out reason);
            Assert.IsTrue(legal);
            Assert.IsNull(reason);

            legal = QueryEvent.IsLegalControlCode("1234", out reason);
            Assert.IsFalse(legal);
            Assert.AreEqual(reason, MiscText.CodeBadLength);

            legal = QueryEvent.IsLegalControlCode(" X", out reason);
            Assert.IsFalse(legal);
            Assert.AreEqual(reason, MiscText.CodeContainsSpace);
        }



        [TestMethod]
        public void GetNextControlCode1()
        {
            Setup("queryevent\\sampleevent1.coursescribe");

            Assert.AreEqual("33", QueryEvent.NextUnusedControlCode(eventDB));
        }

        [TestMethod]
        public void GetNextControlCode2()
        {
            Setup("queryevent\\nocontrols.coursescribe");

            Assert.AreEqual("31", QueryEvent.NextUnusedControlCode(eventDB));
        }

        [TestMethod]
        public void GetNextControlCode3()
        {
            Setup("queryevent\\sampleevent4.coursescribe");

            Assert.AreEqual("67", QueryEvent.NextUnusedControlCode(eventDB));
        }

        [TestMethod]
        public void GetNextControlCode4()
        {
            Setup("queryevent\\sampleevent4.coursescribe");

            undomgr.BeginCommand(555, "change numbering");
            ChangeEvent.ChangeAutoNumbering(eventDB, 64, false);
            undomgr.EndCommand(555);

            Assert.AreEqual("66", QueryEvent.NextUnusedControlCode(eventDB));
        }

        [TestMethod]
        public void LastNonFinishCourseControl()
        {
            Id<CourseControl> courseControlId;
            Setup("queryevent\\sampleevent1.coursescribe");

            courseControlId = QueryEvent.LastCourseControl(eventDB, CourseId(1), true);
            Assert.AreEqual(5, courseControlId.id);

            courseControlId = QueryEvent.LastCourseControl(eventDB, CourseId(1), false);
            Assert.AreEqual(6, courseControlId.id);

            courseControlId = QueryEvent.LastCourseControl(eventDB, CourseId(3), true);
            Assert.AreEqual(58, courseControlId.id);

            courseControlId = QueryEvent.LastCourseControl(eventDB, CourseId(3), false);
            Assert.AreEqual(59, courseControlId.id);

            courseControlId = QueryEvent.LastCourseControl(eventDB, CourseId(2), true);
            Assert.IsTrue(courseControlId.IsNone);

            courseControlId = QueryEvent.LastCourseControl(eventDB, CourseId(2), false);
            Assert.IsTrue(courseControlId.IsNone);
        }

        [TestMethod]
        public void LastCourseControl2()
        {
            Id<CourseControl> courseControlId;
            Setup("queryevent\\sampleevent5.coursescribe");

            courseControlId = QueryEvent.LastCourseControl(eventDB, CourseId(1), true);
            Assert.AreEqual(102, courseControlId.id);

            courseControlId = QueryEvent.LastCourseControl(eventDB, CourseId(1), false);
            Assert.AreEqual(102, courseControlId.id);
        }

        [TestMethod]
        public void HasFinishControl()
        {
            Setup("queryevent\\sampleevent10.ppen");

            Assert.IsFalse(QueryEvent.HasFinishControl(eventDB, CourseId(1)));  // NoStartOrFinish
            Assert.IsFalse(QueryEvent.HasFinishControl(eventDB, CourseId(2)));  // Empty
            Assert.IsTrue(QueryEvent.HasFinishControl(eventDB, CourseId(3)));  // Score
            Assert.IsFalse(QueryEvent.HasFinishControl(eventDB, CourseId(4)));  // StartOnly
            Assert.IsTrue(QueryEvent.HasFinishControl(eventDB, CourseId(5)));  // FinishOnly
            Assert.IsFalse(QueryEvent.HasFinishControl(eventDB, CourseId(6)));  // NoFinish
            Assert.IsTrue(QueryEvent.HasFinishControl(eventDB, CourseId(7)));  // NoStart
            Assert.IsTrue(QueryEvent.HasFinishControl(eventDB, CourseId(8)));  // Regular
            Assert.IsTrue(QueryEvent.HasFinishControl(eventDB, CourseId(9)));  // StartFinishOnly
        }

        [TestMethod]
        public void HasStartControl()
        {
            Setup("queryevent\\sampleevent10.ppen");

            Assert.IsFalse(QueryEvent.HasStartControl(eventDB, CourseId(1)));  // NoStartOrFinish
            Assert.IsFalse(QueryEvent.HasStartControl(eventDB, CourseId(2)));  // Empty
            Assert.IsTrue(QueryEvent.HasStartControl(eventDB, CourseId(3)));  // Score
            Assert.IsTrue(QueryEvent.HasStartControl(eventDB, CourseId(4)));  // StartOnly
            Assert.IsFalse(QueryEvent.HasStartControl(eventDB, CourseId(5)));  // FinishOnly
            Assert.IsTrue(QueryEvent.HasStartControl(eventDB, CourseId(6)));  // NoFinish
            Assert.IsFalse(QueryEvent.HasStartControl(eventDB, CourseId(7)));  // NoStart
            Assert.IsTrue(QueryEvent.HasStartControl(eventDB, CourseId(8)));  // Regular
            Assert.IsTrue(QueryEvent.HasStartControl(eventDB, CourseId(9)));  // StartFinishOnly
        }

        [TestMethod]
        public void FindControlInsertionPoint1()
        {
            // Using GreenY -- a regular course with start and finish.
            Setup("queryevent\\sampleevent11.ppen");

            Id<CourseControl> courseControlId1, courseControlId2;
            CourseDesignator courseDesignator = Designator(6);
            LegInsertionLoc legInsertionLoc;

            // Case 1: no course control or leg specified. Should go right before finish.
            courseControlId1 = Id<CourseControl>.None;
            courseControlId2 = Id<CourseControl>.None;
            legInsertionLoc = LegInsertionLoc.Normal;
            QueryEvent.FindControlInsertionPoint(eventDB, courseDesignator, ref courseControlId1, ref courseControlId2, ref legInsertionLoc);
            Assert.AreEqual(212, courseControlId1.id);
            Assert.AreEqual(213, courseControlId2.id);
            Assert.AreEqual(LegInsertionLoc.Normal, legInsertionLoc);

            // Case 2: selected control is start.
            courseControlId1 = CourseControlId(201);
            courseControlId2 = Id<CourseControl>.None;
            legInsertionLoc = LegInsertionLoc.Normal;
            QueryEvent.FindControlInsertionPoint(eventDB, courseDesignator, ref courseControlId1, ref courseControlId2, ref legInsertionLoc);
            Assert.AreEqual(201, courseControlId1.id);
            Assert.AreEqual(202, courseControlId2.id);
            Assert.AreEqual(LegInsertionLoc.Normal, legInsertionLoc);

            // Case 3: selected control is finish.
            courseControlId1 = CourseControlId(213);
            courseControlId2 = Id<CourseControl>.None;
            legInsertionLoc = LegInsertionLoc.Normal;
            QueryEvent.FindControlInsertionPoint(eventDB, courseDesignator, ref courseControlId1, ref courseControlId2, ref legInsertionLoc);
            Assert.AreEqual(212, courseControlId1.id);
            Assert.AreEqual(213, courseControlId2.id);
            Assert.AreEqual(LegInsertionLoc.Normal, legInsertionLoc);

            // Case 4: selected control is regular control
            courseControlId1 = CourseControlId(206);
            courseControlId2 = Id<CourseControl>.None;
            legInsertionLoc = LegInsertionLoc.Normal;
            QueryEvent.FindControlInsertionPoint(eventDB, courseDesignator, ref courseControlId1, ref courseControlId2, ref legInsertionLoc);
            Assert.AreEqual(206, courseControlId1.id);
            Assert.AreEqual(207, courseControlId2.id);
            Assert.AreEqual(LegInsertionLoc.Normal, legInsertionLoc);

            // Case 5: selected leg
            courseControlId1 = CourseControlId(204);
            courseControlId2 = CourseControlId(205);
            legInsertionLoc = LegInsertionLoc.Normal;
            QueryEvent.FindControlInsertionPoint(eventDB, courseDesignator, ref courseControlId1, ref courseControlId2, ref legInsertionLoc);
            Assert.AreEqual(204, courseControlId1.id);
            Assert.AreEqual(205, courseControlId2.id);
            Assert.AreEqual(LegInsertionLoc.Normal, legInsertionLoc);
        }

        [TestMethod]
        public void FindControlInsertionPoint2()
        {
            // Using White -- a course with no finish.
            Setup("queryevent\\sampleevent11.ppen");

            Id<CourseControl> courseControlId1, courseControlId2;
            CourseDesignator courseDesignator = Designator(7);
            LegInsertionLoc legInsertionLoc;

            // Case 1: no course control or leg specified. Should go at end.
            courseControlId1 = Id<CourseControl>.None;
            courseControlId2 = Id<CourseControl>.None;
            legInsertionLoc = LegInsertionLoc.Normal;
            QueryEvent.FindControlInsertionPoint(eventDB, courseDesignator, ref courseControlId1, ref courseControlId2, ref legInsertionLoc);
            Assert.AreEqual(216, courseControlId1.id);
            Assert.IsTrue(courseControlId2.IsNone);
            Assert.AreEqual(LegInsertionLoc.Normal, legInsertionLoc);

            // Case 2: selected control is last.
            courseControlId1 = CourseControlId(216);
            courseControlId2 = Id<CourseControl>.None;
            legInsertionLoc = LegInsertionLoc.Normal;
            QueryEvent.FindControlInsertionPoint(eventDB, courseDesignator, ref courseControlId1, ref courseControlId2, ref legInsertionLoc);
            Assert.AreEqual(216, courseControlId1.id);
            Assert.IsTrue(courseControlId2.IsNone);
            Assert.AreEqual(LegInsertionLoc.Normal, legInsertionLoc);

            // Case 3: selected control is start.
            courseControlId1 = CourseControlId(214);
            courseControlId2 = Id<CourseControl>.None;
            legInsertionLoc = LegInsertionLoc.Normal;
            QueryEvent.FindControlInsertionPoint(eventDB, courseDesignator, ref courseControlId1, ref courseControlId2, ref legInsertionLoc);
            Assert.AreEqual(214, courseControlId1.id);
            Assert.AreEqual(216, courseControlId2.id);
            Assert.AreEqual(LegInsertionLoc.Normal, legInsertionLoc);

            // Case 5: selected leg
            courseControlId1 = CourseControlId(214);
            courseControlId2 = CourseControlId(216);
            legInsertionLoc = LegInsertionLoc.Normal;
            QueryEvent.FindControlInsertionPoint(eventDB, courseDesignator, ref courseControlId1, ref courseControlId2, ref legInsertionLoc);
            Assert.AreEqual(214, courseControlId1.id);
            Assert.AreEqual(216, courseControlId2.id);
            Assert.AreEqual(LegInsertionLoc.Normal, legInsertionLoc);
        }

        [TestMethod]
        public void FindControlInsertionPoint3()
        {
            // Usi2ng Yellow -- an empty course
            Setup("queryevent\\sampleevent11.ppen");

            Id<CourseControl> courseControlId1, courseControlId2;
            CourseDesignator courseDesignator = Designator(2);
            LegInsertionLoc legInsertionLoc;

            // Case 1: no course control or leg specified. Should go as only control.
            courseControlId1 = Id<CourseControl>.None;
            courseControlId2 = Id<CourseControl>.None;
            legInsertionLoc = LegInsertionLoc.Normal;
            QueryEvent.FindControlInsertionPoint(eventDB, courseDesignator, ref courseControlId1, ref courseControlId2, ref legInsertionLoc);
            Assert.IsTrue(courseControlId1.IsNone);
            Assert.IsTrue(courseControlId2.IsNone);
        }

        // UNDONE MAPEXCHANGE: Test FindControlInsertionPoint for map exchanges.


        [TestMethod]
        public void CourseContainsSpecial()
        {
            Setup("queryevent\\sampleevent6.coursescribe");

            Assert.IsTrue(QueryEvent.CourseContainsSpecial(eventDB, CourseDesignator.AllControls, SpecialId(1)));
            Assert.IsFalse(QueryEvent.CourseContainsSpecial(eventDB, CourseDesignator.AllControls, SpecialId(3)));
            Assert.IsFalse(QueryEvent.CourseContainsSpecial(eventDB, CourseDesignator.AllControls, SpecialId(4)));
            Assert.IsTrue(QueryEvent.CourseContainsSpecial(eventDB, Designator(3), SpecialId(4)));
            Assert.IsFalse(QueryEvent.CourseContainsSpecial(eventDB, Designator(3), SpecialId(5)));
            Assert.IsTrue(QueryEvent.CourseContainsSpecial(eventDB, Designator(6), SpecialId(4)));
            Assert.IsTrue(QueryEvent.CourseContainsSpecial(eventDB, Designator(6), SpecialId(5)));
            Assert.IsTrue(QueryEvent.CourseContainsSpecial(eventDB, Designator(6), SpecialId(1)));
        }

        [TestMethod]
        public void CourseContainsSpecialMapExchange()
        {
            Setup("queryevent\\mapexchange1.ppen");

            Assert.IsTrue(QueryEvent.CourseContainsSpecial(eventDB, CourseDesignator.AllControls, SpecialId(1)));
            Assert.IsFalse(QueryEvent.CourseContainsSpecial(eventDB, CourseDesignator.AllControls, SpecialId(3)));
            Assert.IsTrue(QueryEvent.CourseContainsSpecial(eventDB, CourseDesignator.AllControls, SpecialId(4)));

            Assert.IsTrue(QueryEvent.CourseContainsSpecial(eventDB, Designator(2), SpecialId(3)));
            Assert.IsTrue(QueryEvent.CourseContainsSpecial(eventDB, Designator(3), SpecialId(3)));
            Assert.IsFalse(QueryEvent.CourseContainsSpecial(eventDB, Designator(6), SpecialId(3)));
            Assert.IsFalse(QueryEvent.CourseContainsSpecial(eventDB, new CourseDesignator(CourseId(6), 2), SpecialId(3)));
            Assert.IsTrue(QueryEvent.CourseContainsSpecial(eventDB, new CourseDesignator(CourseId(2), 1), SpecialId(3)));
            Assert.IsFalse(QueryEvent.CourseContainsSpecial(eventDB, new CourseDesignator(CourseId(2), 0), SpecialId(3)));

            Assert.IsFalse(QueryEvent.CourseContainsSpecial(eventDB, Designator(2), SpecialId(2)));
            Assert.IsFalse(QueryEvent.CourseContainsSpecial(eventDB, Designator(3), SpecialId(2)));
            Assert.IsTrue(QueryEvent.CourseContainsSpecial(eventDB, Designator(6), SpecialId(2)));
            Assert.IsTrue(QueryEvent.CourseContainsSpecial(eventDB, new CourseDesignator(CourseId(6), 2), SpecialId(2)));
            Assert.IsFalse(QueryEvent.CourseContainsSpecial(eventDB, new CourseDesignator(CourseId(2), 1), SpecialId(2)));
            Assert.IsFalse(QueryEvent.CourseContainsSpecial(eventDB, new CourseDesignator(CourseId(2), 0), SpecialId(2)));
        }

        [TestMethod]
        public void ComputeLegLength()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);

            ControlPoint control1, control2;
            Event ev = new Event();

            control1 = new ControlPoint(ControlPointKind.Normal, "31", new PointF(4.5F, -10.1F));
            control2 = new ControlPoint(ControlPointKind.Normal, "32", new PointF(-6.8F, 14.1F));
            undomgr.BeginCommand(123, "Add controls");
            Id<ControlPoint> id1 = eventDB.AddControlPoint(control1);
            Id<ControlPoint> id2 = eventDB.AddControlPoint(control2);
            ev.mapScale = 15000;
            eventDB.ChangeEvent(ev);
            undomgr.EndCommand(123);

            float length = QueryEvent.ComputeLegLength(eventDB, id1, id2, Id<Leg>.None);

            Assert.AreEqual(400.623F, length, 0.01);

            SymPath path = QueryEvent.GetLegPath(eventDB, id1, id2);
            Assert.AreEqual(new SymPath(new PointF[] { new PointF(4.5F, -10.1F), new PointF(-6.8F, 14.1F) }), path);
        }


        [TestMethod]
        public void ComputeLegLengthBends()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);

            ControlPoint control1, control2;
            Leg leg1;
            Event ev = new Event();

            control1 = new ControlPoint(ControlPointKind.Normal, "31", new PointF(4.5F, -10.1F));
            control2 = new ControlPoint(ControlPointKind.Normal, "32", new PointF(-6.8F, 14.1F));
            undomgr.BeginCommand(123, "Add controls");
            Id<ControlPoint> id1 = eventDB.AddControlPoint(control1);
            Id<ControlPoint> id2 = eventDB.AddControlPoint(control2);

            leg1 = new Leg(id1, id2);
            leg1.bends = new PointF[] { new PointF(7, 8), new PointF(10, 14) };
            Id<Leg> legId = eventDB.AddLeg(leg1);

            ev.mapScale = 15000;
            eventDB.ChangeEvent(ev);
            undomgr.EndCommand(123);

            float length = QueryEvent.ComputeLegLength(eventDB, id1, id2, legId);

            Assert.AreEqual(626.7, length, 0.01);

            SymPath path = QueryEvent.GetLegPath(eventDB, id1, id2);
            Assert.AreEqual(new SymPath(new PointF[] { new PointF(4.5F, -10.1F), new PointF(7, 8), new PointF(10, 14), new PointF(-6.8F, 14.1F) }), path);
        }

        [TestMethod]
        public void ComputeStraightLineControlDistance()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);

            ControlPoint control1, control2, control3;
            Leg leg1;
            Event ev = new Event();

            control1 = new ControlPoint(ControlPointKind.Normal, "31", new PointF(4.5F, -10.1F));
            control2 = new ControlPoint(ControlPointKind.Normal, "32", new PointF(-6.8F, 14.1F));
            control3 = new ControlPoint(ControlPointKind.Normal, "33", new PointF(-12.8F, 32.9F));
            undomgr.BeginCommand(123, "Add controls");
            Id<ControlPoint> id1 = eventDB.AddControlPoint(control1);
            Id<ControlPoint> id2 = eventDB.AddControlPoint(control2);
            Id<ControlPoint> id3 = eventDB.AddControlPoint(control3);

            leg1 = new Leg(id1, id2);
            leg1.bends = new PointF[] { new PointF(7, 8), new PointF(10, 14) };
            Id<Leg> legId = eventDB.AddLeg(leg1);

            ev.mapScale = 15000;
            eventDB.ChangeEvent(ev);
            undomgr.EndCommand(123);

            float length = QueryEvent.ComputeStraightLineControlDistance(eventDB, id1, id2);
            Assert.AreEqual(400.62F, length, 0.01F);  // Doesn't take bend in leg into account!

            length = QueryEvent.ComputeStraightLineControlDistance(eventDB, id3, id2);
            Assert.AreEqual(296.01F, length, 0.01F);
        }

        [TestMethod]
        public void ComputeFlaggedLegLength()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);

            ControlPoint control1, control2;
            Leg leg1;
            Event ev = new Event();

            control1 = new ControlPoint(ControlPointKind.Normal, "31", new PointF(4F, -10F));
            control2 = new ControlPoint(ControlPointKind.Normal, "32", new PointF(-3F, 14F));
            undomgr.BeginCommand(123, "Add controls");
            Id<ControlPoint> id1 = eventDB.AddControlPoint(control1);
            Id<ControlPoint> id2 = eventDB.AddControlPoint(control2);

            leg1 = new Leg(id1, id2);
            leg1.bends = new PointF[] { new PointF(6, 3), new PointF(7, 8), new PointF(10, 14) };
            Id<Leg> legId = eventDB.AddLeg(leg1);

            // distances. 13.153, 5.099, 6.708, 13.00   * 15000

            ev.mapScale = 15000;
            eventDB.ChangeEvent(ev);
            undomgr.EndCommand(123);

            float length = QueryEvent.ComputeFlaggedLegLength(eventDB, id1, id2, legId);
            Assert.AreEqual(569.4, length, 0.01);

            undomgr.BeginCommand(123, "Change leg");
            leg1.flagging = FlaggingKind.All;
            eventDB.ReplaceLeg(legId, leg1);
            undomgr.EndCommand(123);
            length = QueryEvent.ComputeFlaggedLegLength(eventDB, id1, id2, legId);
            Assert.AreEqual(569.4, length, 0.01);

            undomgr.BeginCommand(123, "Change leg");
            leg1.flagging = FlaggingKind.Begin;
            leg1.flagStartStop = new PointF(7, 8);
            eventDB.ReplaceLeg(legId, leg1);
            undomgr.EndCommand(123);
            length = QueryEvent.ComputeFlaggedLegLength(eventDB, id1, id2, legId);
            Assert.AreEqual(273.78, length, 0.01);

            undomgr.BeginCommand(123, "Change leg");
            leg1.flagging = FlaggingKind.End;
            eventDB.ReplaceLeg(legId, leg1);
            undomgr.EndCommand(123);
            length = QueryEvent.ComputeFlaggedLegLength(eventDB, id1, id2, legId);
            Assert.AreEqual(295.62, length, 0.01);
        }

        [TestMethod]
        public void FindLeg()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);

            ControlPoint control1, control2, control3, control4;
            Leg leg1, leg2;
            Id<Leg> legId1, legId2;
            Event ev = new Event();

            control1 = new ControlPoint(ControlPointKind.Normal, "31", new PointF(4.5F, -10.1F));
            control2 = new ControlPoint(ControlPointKind.Normal, "32", new PointF(-6.8F, 14.1F));
            control3 = new ControlPoint(ControlPointKind.Normal, "33", new PointF(5F, 19F));
            control4 = new ControlPoint(ControlPointKind.Normal, "34", new PointF(18F, 1F));
            undomgr.BeginCommand(124, "Add controls");
            Id<ControlPoint> id1 = eventDB.AddControlPoint(control1);
            Id<ControlPoint> id2 = eventDB.AddControlPoint(control2);
            Id<ControlPoint> id3 = eventDB.AddControlPoint(control3);
            Id<ControlPoint> id4 = eventDB.AddControlPoint(control4);

            leg1 = new Leg(id1, id2);
            legId1 = eventDB.AddLeg(leg1);
            leg2 = new Leg(id4, id2);
            legId2 = eventDB.AddLeg(leg2);

            undomgr.EndCommand(124);

            Id<Leg> result = QueryEvent.FindLeg(eventDB, id1, id3);
            Assert.IsTrue(result.IsNone);
            result = QueryEvent.FindLeg(eventDB, id2, id1);
            Assert.IsTrue(result.IsNone);
            result = QueryEvent.FindLeg(eventDB, id3, id2);
            Assert.IsTrue(result.IsNone);
            result = QueryEvent.FindLeg(eventDB, id1, id2);
            Assert.IsTrue(result == legId1);
            result = QueryEvent.FindLeg(eventDB, id4, id2);
            Assert.IsTrue(result == legId2);
        }

        [TestMethod]
        public void GetLegFlagging()
        {
            Setup("queryevent\\speciallegs.coursescribe");

            FlaggingKind flagging;

            flagging = QueryEvent.GetLegFlagging(eventDB, ControlId(1), ControlId(2));
            Assert.AreEqual(flagging, FlaggingKind.None);
            flagging = QueryEvent.GetLegFlagging(eventDB, ControlId(2), ControlId(3));
            Assert.AreEqual(flagging, FlaggingKind.All);
            flagging = QueryEvent.GetLegFlagging(eventDB, ControlId(3), ControlId(4));
            Assert.AreEqual(flagging, FlaggingKind.Begin);
            flagging = QueryEvent.GetLegFlagging(eventDB, ControlId(4), ControlId(5));
            Assert.AreEqual(flagging, FlaggingKind.None);
            flagging = QueryEvent.GetLegFlagging(eventDB, ControlId(5), ControlId(6));
            Assert.AreEqual(flagging, FlaggingKind.All);
        }

        [TestMethod]
        public void GetLegGaps()
        {
            Setup("queryevent\\gappedlegs.coursescribe");

            LegGap[] gaps;

            gaps = QueryEvent.GetLegGaps(eventDB, ControlId(1), ControlId(2));
            TestUtil.TestEnumerableAnyOrder(gaps, new LegGap[] { new LegGap(7, 3.5F), new LegGap(25, 9) });
            gaps = QueryEvent.GetLegGaps(eventDB, ControlId(5), ControlId(6));
            Assert.AreEqual(gaps, null);
        }

        [TestMethod]
        public void GetSpecialDisplayedCourses()
        {
            CourseDesignator[] result;

            Setup("queryevent\\specials.ppen");

            result = QueryEvent.GetSpecialDisplayedCourses(eventDB, SpecialId(7));
            TestUtil.TestEnumerableAnyOrder(result, new CourseDesignator[] { Designator(0), Designator(1), Designator(2), Designator(3), Designator(4), Designator(5), Designator(6), Designator(7), Designator(8), Designator(9), Designator(10) });

            result = QueryEvent.GetSpecialDisplayedCourses(eventDB, SpecialId(2));
            TestUtil.TestEnumerableAnyOrder(result, new CourseDesignator[] { Designator(1), Designator(2), Designator(3), Designator(9) });
        }

        [TestMethod]
        public void GetSpecialLength()
        {
            Setup("queryevent\\specials.ppen");

            float result = QueryEvent.ComputeSpecialLength(eventDB, SpecialId(10));
            Assert.AreEqual(304.2248F, result);
            result = QueryEvent.ComputeSpecialLength(eventDB, SpecialId(9));
            Assert.AreEqual(905.0821F, result);
        }

        [TestMethod]
        public void CountCourses()
        {
            Setup("queryEvent\\specials.ppen");
            Assert.AreEqual(10, QueryEvent.CountCourses(eventDB));
        }

        [TestMethod]
        public void GetAllPunchPatterns()
        {
            Setup("queryEvent\\sampleevent4.coursescribe");
            Dictionary<string, PunchPattern> allPatterns = QueryEvent.GetAllPunchPatterns(eventDB);

            Assert.AreEqual(5, allPatterns.Count);
            Assert.IsTrue(allPatterns.ContainsKey("60"));
            Assert.IsTrue(allPatterns.ContainsKey("62"));
            Assert.IsTrue(allPatterns.ContainsKey("63"));
            Assert.IsTrue(allPatterns.ContainsKey("64"));
            Assert.IsTrue(allPatterns.ContainsKey("65"));
            Assert.IsNull(allPatterns["62"]);
            Assert.IsNull(allPatterns["65"]);
            Assert.IsNotNull(allPatterns["60"]);
            Assert.IsNotNull(allPatterns["64"]);
            Assert.IsNotNull(allPatterns["63"]);
            Assert.AreEqual(9, allPatterns["60"].size);
            Assert.AreEqual(9, allPatterns["64"].size);
            Assert.AreEqual(9, allPatterns["63"].size);
            Assert.IsTrue(allPatterns["60"].dots[0, 0]);
            Assert.IsTrue(allPatterns["60"].dots[4, 4]);
            Assert.IsTrue(allPatterns["60"].dots[8, 0]);
            Assert.IsFalse(allPatterns["60"].dots[0, 8]);
            Assert.IsTrue(allPatterns["60"].dots[8, 0]);
            Assert.IsTrue(allPatterns["64"].dots[8, 2]);
            Assert.IsFalse(allPatterns["64"].dots[0, 0]);
        }

        [TestMethod]
        public void GetPrintArea()
        {
            Setup("queryevent\\sampleevent7.coursescribe");

            RectangleF defaultArea = new RectangleF(40, 70, 100.5F, 200.5F);
            PrintArea result;

            result = QueryEvent.GetPrintArea(eventDB, Designator(4));
            Assert.IsFalse(result.autoPrintArea);
            Assert.AreEqual(10.3F, result.printAreaRectangle.Left, 0.001F);
            Assert.AreEqual(23.1F, result.printAreaRectangle.Bottom, 0.001F);
            Assert.AreEqual(31.1F, result.printAreaRectangle.Right, 0.001F);
            Assert.AreEqual(-123.5F, result.printAreaRectangle.Top, 0.001F);

            result = QueryEvent.GetPrintArea(eventDB, CourseDesignator.AllControls);
            Assert.IsFalse(result.autoPrintArea);
            Assert.AreEqual(100.3F, result.printAreaRectangle.Left, 0.001F);
            Assert.AreEqual(234.1F, result.printAreaRectangle.Bottom, 0.001F);
            Assert.AreEqual(312.1F, result.printAreaRectangle.Right, 0.001F);
            Assert.AreEqual(-23.5F, result.printAreaRectangle.Top, 0.001F);

            result = QueryEvent.GetPrintArea(eventDB, Designator(6));
            Assert.IsTrue(result.autoPrintArea);
        }


        [TestMethod]
        public void GetPrintArea2()
        {
            Setup("queryevent\\mapexchange1.ppen");

            RectangleF defaultArea = new RectangleF(40, 70, 100.5F, 200.5F);
            PrintArea result;

            result = QueryEvent.GetPrintArea(eventDB, CourseDesignator.AllControls);
            Assert.IsTrue(result.autoPrintArea);

            result = QueryEvent.GetPrintArea(eventDB, Designator(2));
            Assert.IsFalse(result.autoPrintArea);
            Assert.AreEqual(10F, result.printAreaRectangle.Left, 0.001F);
            Assert.AreEqual(50F, result.printAreaRectangle.Bottom, 0.001F);
            Assert.AreEqual(55F, result.printAreaRectangle.Right, 0.001F);
            Assert.AreEqual(-10F, result.printAreaRectangle.Top, 0.001F);

            result = QueryEvent.GetPrintArea(eventDB, new CourseDesignator(CourseId(2), 0));
            Assert.IsFalse(result.autoPrintArea);
            Assert.AreEqual(10F, result.printAreaRectangle.Left, 0.001F);
            Assert.AreEqual(50F, result.printAreaRectangle.Bottom, 0.001F);
            Assert.AreEqual(55F, result.printAreaRectangle.Right, 0.001F);
            Assert.AreEqual(-10F, result.printAreaRectangle.Top, 0.001F);

            result = QueryEvent.GetPrintArea(eventDB, new CourseDesignator(CourseId(2), 1));
            Assert.IsFalse(result.autoPrintArea);
            Assert.AreEqual(0F, result.printAreaRectangle.Left, 0.001F);
            Assert.AreEqual(80F, result.printAreaRectangle.Bottom, 0.001F);
            Assert.AreEqual(90F, result.printAreaRectangle.Right, 0.001F);
            Assert.AreEqual(30F, result.printAreaRectangle.Top, 0.001F);
        }

        [TestMethod]
        public void GetPartOptions()
        {
            Setup("queryEvent\\mapexchange1.ppen");

            PartOptions result;

            result = QueryEvent.GetPartOptions(eventDB, Designator(2));
            Assert.AreEqual(false, result.ShowFinish);

            result = QueryEvent.GetPartOptions(eventDB, new CourseDesignator(CourseId(6), 0));
            Assert.AreEqual(false, result.ShowFinish);

            result = QueryEvent.GetPartOptions(eventDB, new CourseDesignator(CourseId(6), 1));
            Assert.AreEqual(false, result.ShowFinish);

            result = QueryEvent.GetPartOptions(eventDB, new CourseDesignator(CourseId(6), 2));
            Assert.AreEqual(true, result.ShowFinish);

            result = QueryEvent.GetPartOptions(eventDB, new CourseDesignator(CourseId(6), 3));
            Assert.AreEqual(true, result.ShowFinish);
        }

        [TestMethod]
        public void GetControlLoad()
        {
            int load;

            Setup("queryevent\\sampleevent8.coursescribe");

            load = QueryEvent.GetControlLoad(eventDB, ControlId(13));    // control "290"; unused by any course
            Assert.AreEqual(-1, load);

            load = QueryEvent.GetControlLoad(eventDB, ControlId(16));    // control "301"; used by "rambo", "Score 4"
            Assert.AreEqual(-1, load);

            load = QueryEvent.GetControlLoad(eventDB, ControlId(8));    // control "8"; used by "Green Y", "Score 4", "White" (0.5 load)
            Assert.AreEqual(32, load);

            load = QueryEvent.GetControlLoad(eventDB, ControlId(14));    // control "291"; used by "Sample Course 4"
            Assert.AreEqual(0, load);

            load = QueryEvent.GetControlLoad(eventDB, ControlId(2));    // control "31"; used by  "Score 4", "White"
            Assert.AreEqual(25, load);
        }

        [TestMethod]
        public void GetControlVisitLoad()
        {
            int load;

            Setup("queryevent\\visitload.ppen");

            load = QueryEvent.GetControlVisitLoad(eventDB, ControlId(21));    // control "49"
            Assert.AreEqual(100, load);

            load = QueryEvent.GetControlVisitLoad(eventDB, ControlId(14));    // control "14"
            Assert.AreEqual(100, load);

            load = QueryEvent.GetControlVisitLoad(eventDB, ControlId(4));    // control "33"
            Assert.AreEqual(300, load);

            load = QueryEvent.GetControlVisitLoad(eventDB, ControlId(20));    // control "48"
            Assert.AreEqual(100, load);

            load = QueryEvent.GetControlVisitLoad(eventDB, ControlId(13));    // control "41"
            Assert.AreEqual(-1, load);

            load = QueryEvent.GetControlVisitLoad(eventDB, ControlId(7));    // control "36"
            Assert.AreEqual(150, load);

            load = QueryEvent.GetControlVisitLoad(eventDB, ControlId(23));    // control "51"
            Assert.AreEqual(50, load);
        }


        [TestMethod]
        public void GetControlLoadRelay()
        {
            int load;

            Setup("reports\\marymoor7.ppen");

            load = QueryEvent.GetControlLoad(eventDB, ControlId(82));    // start; used by relay
            Assert.AreEqual(100, load);

            load = QueryEvent.GetControlLoad(eventDB, ControlId(41));    // "41"; used by all relay
            Assert.AreEqual(100, load);

            load = QueryEvent.GetControlLoad(eventDB, ControlId(91));    // "44"; used by all relay
            Assert.AreEqual(100, load);

            load = QueryEvent.GetControlLoad(eventDB, ControlId(83));    // "31"; used by all relay
            Assert.AreEqual(100, load);

            load = QueryEvent.GetControlLoad(eventDB, ControlId(81));    // "60"; not used relay
            Assert.AreEqual(-1, load);

            load = QueryEvent.GetControlLoad(eventDB, ControlId(46));    // "46"; used by 1/2 in relay
            Assert.AreEqual(50, load);

            load = QueryEvent.GetControlLoad(eventDB, ControlId(42));    // "42"; used by 1/2 in relay
            Assert.AreEqual(50, load);

            load = QueryEvent.GetControlLoad(eventDB, ControlId(49));    // "49"; used by 1/4 in relay
            Assert.AreEqual(25, load);

            load = QueryEvent.GetControlLoad(eventDB, ControlId(58));    // "58"; used by 1/4 in relay
            Assert.AreEqual(25, load);
        }

        [TestMethod]
        public void GetControlLoadRelay2()
        {
            int load;

            Setup("reports\\marymoor8.ppen");

            load = QueryEvent.GetControlLoad(eventDB, ControlId(82));    // start; used by relay
            Assert.AreEqual(100, load);

            load = QueryEvent.GetControlLoad(eventDB, ControlId(43));    // "43"; relay loop 1
            Assert.AreEqual(100, load);

            load = QueryEvent.GetControlLoad(eventDB, ControlId(70));    // "70"; relay loop 1
            Assert.AreEqual(100, load);

            
            load = QueryEvent.GetControlLoad(eventDB, ControlId(91));    // "44"
            Assert.AreEqual(100, load);

            load = QueryEvent.GetControlLoad(eventDB, ControlId(42));    // "42";
            Assert.AreEqual(50, load);

            load = QueryEvent.GetControlLoad(eventDB, ControlId(42));    // "32"
            Assert.AreEqual(50, load);

            load = QueryEvent.GetControlLoad(eventDB, ControlId(91));    // "44"
            Assert.AreEqual(100, load);


        }

        [TestMethod]
        public void GetLegLoad()
        {
            int load;

            Setup("queryevent\\marymoor2.coursescribe");

            load = QueryEvent.GetLegLoad(eventDB, ControlId(57), ControlId(79));   // 57 - 79: 4B and 5
            Assert.AreEqual(106, load);

            load = QueryEvent.GetLegLoad(eventDB, ControlId(70), ControlId(50));   // 70 - 50: 2
            Assert.AreEqual(-1, load);

            load = QueryEvent.GetLegLoad(eventDB, ControlId(79), ControlId(35));   // 79 - 35: 5
            Assert.AreEqual(71, load);

            load = QueryEvent.GetLegLoad(eventDB, ControlId(38), ControlId(2));   // 38 - finish: all
            Assert.AreEqual(126, load);

            load = QueryEvent.GetLegLoad(eventDB, ControlId(72), ControlId(36)); // 72-36: doesn't exist.
            Assert.AreEqual(-1, load);
        }

        [TestMethod]
        public void GetLegLoadRelay()
        {
            int load;

            Setup("reports\\marymoor7.ppen");

            load = QueryEvent.GetLegLoad(eventDB, ControlId(82), ControlId(50));   // start to 50
            Assert.AreEqual(100, load);
            
            load = QueryEvent.GetLegLoad(eventDB, ControlId(41), ControlId(46));   // 41 - 46:
            Assert.AreEqual(50, load);

            load = QueryEvent.GetLegLoad(eventDB, ControlId(46), ControlId(91));   // 46 - 44
            Assert.AreEqual(50, load);

            load = QueryEvent.GetLegLoad(eventDB, ControlId(91), ControlId(83));   // 44 - 31
            Assert.AreEqual(100, load);

            load = QueryEvent.GetLegLoad(eventDB, ControlId(41), ControlId(42));   // 41 - 42
            Assert.AreEqual(50, load);

            load = QueryEvent.GetLegLoad(eventDB, ControlId(42), ControlId(58));   // 42 - 58
            Assert.AreEqual(25, load);

            load = QueryEvent.GetLegLoad(eventDB, ControlId(42), ControlId(49));   // 42 - 49
            Assert.AreEqual(25, load);

            load = QueryEvent.GetLegLoad(eventDB, ControlId(49), ControlId(91));   // 49 - 44
            Assert.AreEqual(25, load);

        }

        [TestMethod]
        public void GetLegLoadRelay2()
        {
            int load;

            Setup("reports\\marymoor8.ppen");

            load = QueryEvent.GetLegLoad(eventDB, ControlId(82), ControlId(50));   // start to 50
            Assert.AreEqual(100, load);

            load = QueryEvent.GetLegLoad(eventDB, ControlId(50), ControlId(70));   // 50 - 70
            Assert.AreEqual(100, load);

            load = QueryEvent.GetLegLoad(eventDB, ControlId(50), ControlId(41));   // 50 - 41
            Assert.AreEqual(100, load);

            load = QueryEvent.GetLegLoad(eventDB, ControlId(51), ControlId(42));   // 51 - 42
            Assert.AreEqual(50, load);

            load = QueryEvent.GetLegLoad(eventDB, ControlId(42), ControlId(80));   // 42 - 80
            Assert.AreEqual(50, load);

            load = QueryEvent.GetLegLoad(eventDB, ControlId(88), ControlId(42));   // 32 - 42
            Assert.AreEqual(50, load);
        }

        [TestMethod]
        public void GetCourseLoad()
        {
            int load;

            Setup("queryevent\\marymoor2.coursescribe");

            load = QueryEvent.GetCourseLoad(eventDB, CourseId(2));   // Course 2
            Assert.AreEqual(-1, load);

            load = QueryEvent.GetCourseLoad(eventDB, CourseId(6));   // Course 5
            Assert.AreEqual(71, load);
        }

        [TestMethod]
        public void GetCourseSortOrder()
        {
            int sortOrder;

            Setup("queryevent\\marymoor4.coursescribe");

            sortOrder = QueryEvent.GetCourseSortOrder(eventDB, CourseId(2));   // Course 2
            Assert.AreEqual(5, sortOrder);

            sortOrder = QueryEvent.GetCourseSortOrder(eventDB, CourseId(6));   // Course 5
            Assert.AreEqual(11, sortOrder);
        }

        [TestMethod]
        public void AllCoursesHaveLoads()
        {
            Setup("queryevent\\marymoor2.coursescribe");
            Assert.IsFalse(QueryEvent.AllCoursesHaveLoads(eventDB));

            Setup("queryevent\\sampleevent4.coursescribe");
            Assert.IsFalse(QueryEvent.AllCoursesHaveLoads(eventDB));

            Setup("queryevent\\marymoor3.coursescribe");
            Assert.IsTrue(QueryEvent.AllCoursesHaveLoads(eventDB));
        }


        [TestMethod]
        public void SortedCourseIds()
        {
            Setup("queryevent\\coursenames.ppen");

            Id<Course>[] sortedCourseIds = QueryEvent.SortedCourseIds(eventDB);

            Assert.AreEqual(4, sortedCourseIds.Length);
            Assert.AreEqual(CourseId(2), sortedCourseIds[0]);
            Assert.AreEqual("alpha dude", eventDB.GetCourse(sortedCourseIds[0]).name);
            Assert.AreEqual(CourseId(1), sortedCourseIds[1]);
            Assert.AreEqual("Banana", eventDB.GetCourse(sortedCourseIds[1]).name);
            Assert.AreEqual(CourseId(4), sortedCourseIds[2]);
            Assert.AreEqual("Mr. B", eventDB.GetCourse(sortedCourseIds[2]).name);
            Assert.AreEqual(CourseId(3), sortedCourseIds[3]);
            Assert.AreEqual("smedly", eventDB.GetCourse(sortedCourseIds[3]).name);
        }

        [TestMethod]
        public void GetCustomSymbolText()
        {
            Setup("queryevent\\sampleevent3.ppen");

            Dictionary<string, List<SymbolText>> customSymbolText;
            Dictionary<string, bool> customSymbolKey;

            QueryEvent.GetCustomSymbolText(eventDB, out customSymbolText, out customSymbolKey);

            Assert.AreEqual("man-made object", customSymbolText["6.1"][0].Text);
            Assert.AreEqual(true, customSymbolKey["6.1"]);
            Assert.AreEqual("playground equipment", customSymbolText["6.2"][0].Text);
            Assert.AreEqual(false, customSymbolKey["6.2"]);
            Assert.AreEqual("light pole", customSymbolText["5.6"][0].Text);
            Assert.AreEqual(true, customSymbolKey["5.6"]);
            Assert.AreEqual("medical", customSymbolText["12.1"][0].Text);
            Assert.AreEqual(true, customSymbolKey["12.1"]);
            Assert.AreEqual("wet {0}", customSymbolText["8.7"][0].Text);
            Assert.AreEqual(false, customSymbolKey["8.7"]);
            Assert.IsFalse(customSymbolText.ContainsKey("5.23"));

            // Make sure that changes to the retrieved dictionaries don't affect the event.
            SymbolText text = new SymbolText(), text2 = new SymbolText();
            text.Lang = "xx"; text.Text = "hydrant";
            customSymbolText["6.2"] = new List<SymbolText>() { text };
            customSymbolKey["12.1"] = false;
            customSymbolText.Remove("8.7");
            text.Lang = "en"; text.Text = "overhang";
            customSymbolText.Add("5.23", new List<SymbolText>() { text });

            QueryEvent.GetCustomSymbolText(eventDB, out customSymbolText, out customSymbolKey);

            Assert.AreEqual("man-made object", customSymbolText["6.1"][0].Text);
            Assert.AreEqual(true, customSymbolKey["6.1"]);
            Assert.AreEqual("playground equipment", customSymbolText["6.2"][0].Text);
            Assert.AreEqual(false, customSymbolKey["6.2"]);
            Assert.AreEqual("light pole", customSymbolText["5.6"][0].Text);
            Assert.AreEqual(true, customSymbolKey["5.6"]);
            Assert.AreEqual("medical", customSymbolText["12.1"][0].Text);
            Assert.AreEqual(true, customSymbolKey["12.1"]);
            Assert.AreEqual("wet {0}", customSymbolText["8.7"][0].Text);
            Assert.AreEqual(false, customSymbolKey["8.7"]);
            Assert.IsFalse(customSymbolText.ContainsKey("5.23"));
        }

        [TestMethod]
        public void GetControlGaps()
        {
            Setup("queryevent\\sampleevent1.coursescribe");

            CircleGap[] result;

            result = QueryEvent.GetControlGaps(eventDB, ControlId(4), 15000F);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(new CircleGap(56.25F, 67.5F), result[0]);

            result = QueryEvent.GetControlGaps(eventDB, ControlId(4), 12000F);
            Assert.IsNull(result);

            result = QueryEvent.GetControlGaps(eventDB, ControlId(3), 15000F);
            Assert.IsNull(result);

            undomgr.BeginCommand(819, "add gap");

            ChangeEvent.ChangeControlGaps(eventDB, ControlId(2), 12000F, null);
            ChangeEvent.ChangeControlGaps(eventDB, ControlId(2), 10000F, new CircleGap[] { new CircleGap(20, 40), new CircleGap(180, 270) });

            undomgr.EndCommand(819);
            eventDB.Validate();

            result = QueryEvent.GetControlGaps(eventDB, ControlId(2), 12000F);
            Assert.IsNull(result);

            result = QueryEvent.GetControlGaps(eventDB, ControlId(2), 10000F);
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(20, result[0].startAngle);
            Assert.AreEqual(270, result[1].stopAngle);
        }

        [TestMethod]
        public void GetPrintScale()
        {
            Setup("queryevent\\sampleevent12.ppen");

            float result;

            result = QueryEvent.GetPrintScale(eventDB, CourseId(2));
            Assert.AreEqual(15000, result);

            result = QueryEvent.GetPrintScale(eventDB, CourseId(3));
            Assert.AreEqual(10000, result);

            result = QueryEvent.GetPrintScale(eventDB, Id<Course>.None);
            Assert.AreEqual(9000, result);

            Setup("queryevent\\sampleevent1.coursescribe");

            result = QueryEvent.GetPrintScale(eventDB, Id<Course>.None);
            Assert.AreEqual(15000, result);
        }

        [TestMethod]
        public void GetDefaultDescKind()
        {
            Setup("queryevent\\sampleevent12.ppen");

            DescriptionKind result;

            result = QueryEvent.GetDefaultDescKind(eventDB, Id<Course>.None);
            Assert.AreEqual(DescriptionKind.SymbolsAndText, result);

            result = QueryEvent.GetDefaultDescKind(eventDB, CourseId(2));
            Assert.AreEqual(DescriptionKind.Symbols, result);

            result = QueryEvent.GetDefaultDescKind(eventDB, CourseId(3));
            Assert.AreEqual(DescriptionKind.Text, result);
        }

        [TestMethod]
        public void GetEventTitle1()
        {
            Setup("queryevent\\sampleevent12.ppen");
            string result = QueryEvent.GetEventTitle(eventDB, " ");
            Assert.AreEqual("Sample Event 1", result);
        }

        [TestMethod]
        public void GetEventTitle2()
        {
            Setup("queryevent\\sampleevent13.ppen");

            string result = QueryEvent.GetEventTitle(eventDB, "+++");
            Assert.AreEqual("Sample Event 1+++Second line+++Third line", result);

            result = QueryEvent.GetEventTitle(eventDB, " ");
            Assert.AreEqual("Sample Event 1 Second line Third line", result);

            result = QueryEvent.GetEventTitle(eventDB, "\r\n");
            Assert.AreEqual("Sample Event 1\r\nSecond line\r\nThird line", result);
        }

        [TestMethod]
        public void CountCourseParts()
        {
            Setup("queryevent\\mapexchange1.ppen");

            Assert.AreEqual(4, QueryEvent.CountCourseParts(eventDB, Designator(6)));
            Assert.AreEqual(1, QueryEvent.CountCourseParts(eventDB, Designator(1)));
            Assert.AreEqual(2, QueryEvent.CountCourseParts(eventDB, Designator(2)));
        }

        [TestMethod]
        public void GetCoursePartBounds()
        {
            Setup("queryevent\\mapexchange1.ppen");

            Id<CourseControl> startCourseControlId, endCourseControlId;
            bool result;

            result = QueryEvent.GetCoursePartBounds(eventDB, Designator(6, 0), out startCourseControlId, out endCourseControlId);
            Assert.IsTrue(result);
            Assert.AreEqual(CourseControlId(601), startCourseControlId);
            Assert.AreEqual(CourseControlId(611), endCourseControlId);

            result = QueryEvent.GetCoursePartBounds(eventDB, Designator(6, 1), out startCourseControlId, out endCourseControlId);
            Assert.IsTrue(result);
            Assert.AreEqual(CourseControlId(611), startCourseControlId);
            Assert.AreEqual(CourseControlId(615), endCourseControlId);

            result = QueryEvent.GetCoursePartBounds(eventDB, Designator(6, 2), out startCourseControlId, out endCourseControlId);
            Assert.IsTrue(result);
            Assert.AreEqual(CourseControlId(615), startCourseControlId);
            Assert.AreEqual(CourseControlId(616), endCourseControlId);

            result = QueryEvent.GetCoursePartBounds(eventDB, Designator(6, 3), out startCourseControlId, out endCourseControlId);
            Assert.IsTrue(result);
            Assert.AreEqual(CourseControlId(616), startCourseControlId);
            Assert.AreEqual(CourseControlId(620), endCourseControlId);

            result = QueryEvent.GetCoursePartBounds(eventDB, Designator(6, 4), out startCourseControlId, out endCourseControlId);
            Assert.IsFalse(result);

            result = QueryEvent.GetCoursePartBounds(eventDB, Designator(1, 0), out startCourseControlId, out endCourseControlId);
            Assert.IsTrue(result);
            Assert.AreEqual(CourseControlId(101), startCourseControlId);
            Assert.AreEqual(CourseControlId(112), endCourseControlId);

            result = QueryEvent.GetCoursePartBounds(eventDB, Designator(1, 1), out startCourseControlId, out endCourseControlId);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AnyMultipart1()
        {
            Setup("queryevent\\mapexchange1.ppen");

            bool result = QueryEvent.AnyMultipartCourses(eventDB);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void AnyMultipart2()
        {
            Setup("queryevent\\marymoor2.coursescribe");

            bool result = QueryEvent.AnyMultipartCourses(eventDB);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsCourseControlInPart()
        {
            Setup("queryevent\\mapexchange1.ppen");

            Assert.IsTrue(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 0), CourseControlId(601)));
            Assert.IsTrue(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 0), CourseControlId(611)));
            Assert.IsTrue(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 0), CourseControlId(602)));
            Assert.IsTrue(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 0), CourseControlId(610)));
            Assert.IsFalse(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 0), CourseControlId(612)));
            Assert.IsFalse(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 0), CourseControlId(620)));

            Assert.IsTrue(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 1), CourseControlId(611)));
            Assert.IsTrue(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 1), CourseControlId(615)));
            Assert.IsTrue(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 1), CourseControlId(612)));
            Assert.IsTrue(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 1), CourseControlId(614)));
            Assert.IsFalse(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 1), CourseControlId(601)));
            Assert.IsFalse(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 1), CourseControlId(610)));

            Assert.IsTrue(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 2), CourseControlId(616)));
            Assert.IsTrue(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 2), CourseControlId(615)));
            Assert.IsFalse(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 2), CourseControlId(601)));
            Assert.IsFalse(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 2), CourseControlId(614)));
            Assert.IsFalse(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 2), CourseControlId(617)));
            Assert.IsFalse(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 2), CourseControlId(620)));

            Assert.IsTrue(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 3), CourseControlId(616)));
            Assert.IsTrue(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 3), CourseControlId(620)));
            Assert.IsTrue(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 3), CourseControlId(617)));
            Assert.IsTrue(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 3), CourseControlId(619)));
            Assert.IsFalse(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 3), CourseControlId(615)));
            Assert.IsFalse(QueryEvent.IsCourseControlInPart(eventDB, Designator(6, 3), CourseControlId(614)));

            Assert.IsTrue(QueryEvent.IsCourseControlInPart(eventDB, Designator(1, 0), CourseControlId(101)));
            Assert.IsTrue(QueryEvent.IsCourseControlInPart(eventDB, Designator(1, 0), CourseControlId(112)));
            Assert.IsTrue(QueryEvent.IsCourseControlInPart(eventDB, Designator(1, 0), CourseControlId(102)));
            Assert.IsTrue(QueryEvent.IsCourseControlInPart(eventDB, Designator(1, 0), CourseControlId(111)));
        }

        [TestMethod]
        public void EnumerateCourseDesignators()
        {
            IEnumerable<CourseDesignator> result;

            Setup("queryevent\\mapexchange1.ppen");

            result = QueryEvent.EnumerateCourseDesignators(eventDB, new Id<Course>[] { CourseId(2), CourseId(3), Id<Course>.None }, false);
            CollectionAssert.AreEqual(new CourseDesignator[] { Designator(2), Designator(3), CourseDesignator.AllControls }, result.ToList());

            result = QueryEvent.EnumerateCourseDesignators(eventDB, new Id<Course>[] { CourseId(2), CourseId(3), Id<Course>.None }, true);
            CollectionAssert.AreEqual(new CourseDesignator[] { new CourseDesignator(CourseId(2), 0), new CourseDesignator(CourseId(2), 1), Designator(3), CourseDesignator.AllControls }, result.ToList());

            result = QueryEvent.EnumerateCourseDesignators(eventDB, new Id<Course>[] { CourseId(6), CourseId(3) }, false);
            CollectionAssert.AreEqual(new CourseDesignator[] { Designator(6), Designator(3) }, result.ToList());

            result = QueryEvent.EnumerateCourseDesignators(eventDB, new Id<Course>[] { CourseId(6), CourseId(3) }, true);
            CollectionAssert.AreEqual(new CourseDesignator[] { new CourseDesignator(CourseId(6), 0), new CourseDesignator(CourseId(6), 1), new CourseDesignator(CourseId(6), 2), new CourseDesignator(CourseId(6), 3), Designator(3) }, result.ToList());
        }

        [TestMethod]
        public void CreateOutputFileName()
        {
            string result;

            Setup("queryevent\\mapexchange1.ppen");

            result = QueryEvent.CreateOutputFileName(eventDB, null, "", "", ".xyz");
            Assert.AreEqual("Marymoor WIOL 2.xyz", result);

            result = QueryEvent.CreateOutputFileName(eventDB, null, "PRE/TEST", "", ".a");
            Assert.AreEqual("PRE_TEST-Marymoor WIOL 2.a", result);

            result = QueryEvent.CreateOutputFileName(eventDB, Designator(2), "", "-ext", ".xyz");
            Assert.AreEqual("Course 2-ext.xyz", result);

            result = QueryEvent.CreateOutputFileName(eventDB, Designator(2), "PRETEST", "", ".a");
            Assert.AreEqual("PRETEST-Course 2.a", result);

            result = QueryEvent.CreateOutputFileName(eventDB, new CourseDesignator(CourseId(2), 0), "", "", ".xyz");
            Assert.AreEqual("Course 2-1.xyz", result);

            result = QueryEvent.CreateOutputFileName(eventDB, new CourseDesignator(CourseId(6), 2), "PRETEST", "", ".a");
            Assert.AreEqual("PRETEST-Course 5-3.a", result);

            result = QueryEvent.CreateOutputFileName(eventDB, CourseDesignator.AllControls, null, "", ".xyz");
            Assert.AreEqual("All controls.xyz", result);

            result = QueryEvent.CreateOutputFileName(eventDB, CourseDesignator.AllControls, "A", "_Done", ".a");
            Assert.AreEqual("A-All controls_Done.a", result);

        }

        [TestMethod]
        public void IsImageNameInUse()
        {
            bool result;
            Setup("queryevent\\sampleevent14.coursescribe");

            result = QueryEvent.IsImageNameUsed(eventDB, "mrsneeze.jpg");
            Assert.IsTrue(result);
            result = QueryEvent.IsImageNameUsed(eventDB, "test.jpg");
            Assert.IsFalse(result);

            undomgr.BeginCommand(1038, "add image");
            ChangeEvent.AddImageSpecial(eventDB, new RectangleF(0, 0, 1, 1), (Bitmap)Image.FromFile(TestUtil.GetTestFile("coursesymbols\\mrsneeze.jpg")), "test.jpg");
            undomgr.EndCommand(1038);

            result = QueryEvent.IsImageNameUsed(eventDB, "test.jpg");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void UniqueImageName()
        {
            string result;
            Setup("queryevent\\sampleevent14.coursescribe");

            result = QueryEvent.UniqueImageName(eventDB, "test.jpg");
            Assert.AreEqual("test.jpg", result);
            result = QueryEvent.UniqueImageName(eventDB, "mrsneeze.jpg");
            Assert.AreEqual("mrsneeze(1).jpg", result);

            undomgr.BeginCommand(1038, "add image");
            ChangeEvent.AddImageSpecial(eventDB, new RectangleF(0, 0, 1, 1), (Bitmap)Image.FromFile(TestUtil.GetTestFile("coursesymbols\\mrsneeze.jpg")), result);
            undomgr.EndCommand(1038);

            result = QueryEvent.UniqueImageName(eventDB, "mrsneeze.jpg");
            Assert.AreEqual("mrsneeze(2).jpg", result);

            undomgr.BeginCommand(1038, "add image");
            ChangeEvent.AddImageSpecial(eventDB, new RectangleF(0, 0, 1, 1), (Bitmap)Image.FromFile(TestUtil.GetTestFile("coursesymbols\\mrsneeze.jpg")), "foo");
            undomgr.EndCommand(1038);

            result = QueryEvent.UniqueImageName(eventDB, "foo");
            Assert.AreEqual("foo(1)", result);
        }

        [TestMethod]
        public void GetVariationString()
        {
            Setup("queryevent\\variations.ppen");

            string result;

            VariationInfo.VariationPath variationPath = new VariationInfo.VariationPath(new[] {
                CourseControlId(2),
                CourseControlId(27),
                CourseControlId(30),
                CourseControlId(26),
                CourseControlId(25),
                CourseControlId(4),
                CourseControlId(28),
            });

            result = QueryEvent.GetVariationString(eventDB, new Id<Course>(1), variationPath);
            Assert.AreEqual("AEFDCI", result);
        }

        [TestMethod]
        public void GetAllVariations()
        {
            Setup("queryevent\\variations.ppen");

            IEnumerable<VariationInfo> result = QueryEvent.GetAllVariations(eventDB, new Id<Course>(1));
            List<string> pathStrings = (from v in result orderby v.CodeString select v.CodeString).ToList();

            CollectionAssert.AreEquivalent(pathStrings, new[] {
                "ACDEFH",
                "ACDEFI",
                "ACDEFJ",
                "ACDEGH",
                "ACDEGI",
                "ACDEGJ",
                "ACEFDH",
                "ACEFDI",
                "ACEFDJ",
                "ACEGDH",
                "ACEGDI",
                "ACEGDJ",
                "ADCEFH",
                "ADCEFI",
                "ADCEFJ",
                "ADCEGH",
                "ADCEGI",
                "ADCEGJ",
                "ADEFCH",
                "ADEFCI",
                "ADEFCJ",
                "ADEGCH",
                "ADEGCI",
                "ADEGCJ",
                "AEFCDH",
                "AEFCDI",
                "AEFCDJ",
                "AEFDCH",
                "AEFDCI",
                "AEFDCJ",
                "AEGCDH",
                "AEGCDI",
                "AEGCDJ",
                "AEGDCH",
                "AEGDCI",
                "AEGDCJ",
                "B"
            });
        }

        [TestMethod]
        public void CanAddVariation()
        {
            Setup("queryevent\\variations.ppen");

            QueryEvent.AddVariationResult result = QueryEvent.CanAddVariation(eventDB, CourseDesignator.AllControls, CourseControlId(2));
            Assert.AreEqual(QueryEvent.AddVariationResult.CantAddInAllControls, result);

            result = QueryEvent.CanAddVariation(eventDB, Designator(1), CourseControlId(3));
            Assert.AreEqual(QueryEvent.AddVariationResult.OK, result);

            result = QueryEvent.CanAddVariation(eventDB, Designator(1), CourseControlId(2));
            Assert.AreEqual(QueryEvent.AddVariationResult.VariationAlreadyExists, result);

            result = QueryEvent.CanAddVariation(eventDB, Designator(1), CourseControlId(11));
            Assert.AreEqual(QueryEvent.AddVariationResult.CantAddToFinishControl, result);

            result = QueryEvent.CanAddVariation(eventDB, Designator(1), CourseControlId(4));
            Assert.AreEqual(QueryEvent.AddVariationResult.VariationAlreadyExists, result);

            result = QueryEvent.CanAddVariation(eventDB, Designator(1), CourseControlId(25));
            Assert.AreEqual(QueryEvent.AddVariationResult.VariationAlreadyExists, result);

            result = QueryEvent.CanAddVariation(eventDB, Designator(1), CourseControlId(19));
            Assert.AreEqual(QueryEvent.AddVariationResult.OK, result);

            result = QueryEvent.CanAddVariation(eventDB, Designator(1), CourseControlId(1));
            Assert.AreEqual(QueryEvent.AddVariationResult.OK, result);

            VariationInfo.VariationPath variationPath = new VariationInfo.VariationPath(new[] {
                CourseControlId(2),
                CourseControlId(27),
                CourseControlId(30),
                CourseControlId(26),
                CourseControlId(25),
                CourseControlId(4),
                CourseControlId(28),
            });
            VariationInfo variationInfo = new VariationInfo("AEFDCI", variationPath);

            CourseDesignator designator = new CourseDesignator(CourseId(1), variationInfo);

            result = QueryEvent.CanAddVariation(eventDB, designator, CourseControlId(3));
            Assert.AreEqual(QueryEvent.AddVariationResult.OK, result);

            result = QueryEvent.CanAddVariation(eventDB, designator, CourseControlId(2));
            Assert.AreEqual(QueryEvent.AddVariationResult.VariationAlreadyExists, result);

            result = QueryEvent.CanAddVariation(eventDB, designator, CourseControlId(11));
            Assert.AreEqual(QueryEvent.AddVariationResult.CantAddToFinishControl, result);

            result = QueryEvent.CanAddVariation(eventDB, designator, CourseControlId(4));
            Assert.AreEqual(QueryEvent.AddVariationResult.VariationAlreadyExists, result);

            result = QueryEvent.CanAddVariation(eventDB, designator, CourseControlId(25));
            Assert.AreEqual(QueryEvent.AddVariationResult.VariationAlreadyExists, result);

            result = QueryEvent.CanAddVariation(eventDB, designator, CourseControlId(19));
            Assert.AreEqual(QueryEvent.AddVariationResult.OK, result);

            result = QueryEvent.CanAddVariation(eventDB, designator, CourseControlId(1));
            Assert.AreEqual(QueryEvent.AddVariationResult.OK, result);


        }

        [TestMethod]
        public void GetDesignatorsFromVariationChoices()
        {
            Setup("queryevent\\variations.ppen");

            List<CourseDesignator> result;
            CourseDesignator[] expected;

            result = QueryEvent.GetDesignatorsFromVariationChoices(eventDB, CourseId(2), new VariationChoices() { Kind = VariationChoices.VariationChoicesKind.Combined }).ToList();
            expected = new[] { new CourseDesignator(CourseId(2)) };
            CollectionAssert.AreEqual(expected, result);

            result = QueryEvent.GetDesignatorsFromVariationChoices(eventDB, CourseId(2), new VariationChoices() { Kind = VariationChoices.VariationChoicesKind.AllVariations }).ToList();
            expected = (from vi in QueryEvent.GetAllVariations(eventDB, CourseId(2)) select new CourseDesignator(CourseId(2), vi)).ToArray();
            CollectionAssert.AreEqual(expected, result);

            result = QueryEvent.GetDesignatorsFromVariationChoices(eventDB, CourseId(2), new VariationChoices() {
                Kind = VariationChoices.VariationChoicesKind.ChosenVariations,
                ChosenVariations = new List<string> { "AC", "BD"} 
                }).ToList();
            expected = (from vi in QueryEvent.GetAllVariations(eventDB, CourseId(2))
                        where vi.CodeString=="AC" || vi.CodeString == "BD"
                        select new CourseDesignator(CourseId(2), vi)).ToArray();
            CollectionAssert.AreEqual(expected, result);

            VariationInfo ac, ad, bc, bd;
            ac = (from vi in QueryEvent.GetAllVariations(eventDB, CourseId(2))
                 where vi.CodeString == "AC" 
                 select vi).First();
            ad = (from vi in QueryEvent.GetAllVariations(eventDB, CourseId(2))
                  where vi.CodeString == "AD"
                  select vi).First();
            bc = (from vi in QueryEvent.GetAllVariations(eventDB, CourseId(2))
                  where vi.CodeString == "BC"
                  select vi).First();
            bd = (from vi in QueryEvent.GetAllVariations(eventDB, CourseId(2))
                  where vi.CodeString == "BD"
                  select vi).First();
            expected = new CourseDesignator[] {
                new CourseDesignator(CourseId(2), new VariationInfo(bd.CodeString, bd.Path, 6, 1)),
                new CourseDesignator(CourseId(2), new VariationInfo(bc.CodeString, bc.Path, 6, 2)),
                new CourseDesignator(CourseId(2), new VariationInfo(ac.CodeString, ac.Path, 6, 3)),
                new CourseDesignator(CourseId(2), new VariationInfo(ad.CodeString, ad.Path, 6, 4)),
                new CourseDesignator(CourseId(2), new VariationInfo(ad.CodeString, ad.Path, 7, 1)),
                new CourseDesignator(CourseId(2), new VariationInfo(ac.CodeString, ac.Path, 7, 2)),
                new CourseDesignator(CourseId(2), new VariationInfo(bc.CodeString, bc.Path, 7, 3)),
                new CourseDesignator(CourseId(2), new VariationInfo(bd.CodeString, bd.Path, 7, 4))
            };
            result = QueryEvent.GetDesignatorsFromVariationChoices(eventDB, CourseId(2), new VariationChoices() {
                Kind = VariationChoices.VariationChoicesKind.ChosenTeams,
                FirstTeam = 6,
                LastTeam = 7
            }).ToList();

            CollectionAssert.AreEqual(expected, result);

        }

        [TestMethod]
        public void GetForkStart()
        {
            Setup("queryevent\\variations.ppen");

            Id<CourseControl> result;

            result = QueryEvent.GetForkStart(eventDB, CourseId(1), CourseControlId(1));
            Assert.IsTrue(result.IsNone);

            result = QueryEvent.GetForkStart(eventDB, CourseId(1), CourseControlId(2));
            Assert.AreEqual(2, result.id);

            result = QueryEvent.GetForkStart(eventDB, CourseId(1), CourseControlId(3));
            Assert.AreEqual(2, result.id);

            result = QueryEvent.GetForkStart(eventDB, CourseId(1), CourseControlId(10));
            Assert.IsTrue(result.IsNone);

            result = QueryEvent.GetForkStart(eventDB, CourseId(1), CourseControlId(17));
            Assert.AreEqual(26, result.id);

            result = QueryEvent.GetForkStart(eventDB, CourseId(1), CourseControlId(5));
            Assert.AreEqual(2, result.id);

            result = QueryEvent.GetForkStart(eventDB, CourseId(1), CourseControlId(6));
            Assert.AreEqual(2, result.id);

            result = QueryEvent.GetForkStart(eventDB, CourseId(1), CourseControlId(20));
            Assert.AreEqual(30, result.id);

            result = QueryEvent.GetForkStart(eventDB, CourseId(1), CourseControlId(23));
            Assert.AreEqual(27, result.id);

        }
    }
}
#endif
