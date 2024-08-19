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
using System.Drawing;
using TestingUtils;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PurplePen.Tests
{
    using PurplePen.MapModel;

    [TestClass]
    public class CourseLayoutTests: TestFixtureBase
    {
        void CheckHitTest(CourseLayout course, PointF point, CourseLayer layerFilter, Predicate<CourseObj> filter, string expectedObject)
        {
            CourseObj courseobj = course.HitTest(point, 0.1F, layerFilter, filter);
            if (courseobj == null) {
                Assert.IsNull(expectedObject);
            }
            else {
                Console.WriteLine(courseobj);
                Assert.AreEqual(expectedObject, courseobj.ToString());
            }
        }

        [TestMethod]
        public void HitTest()
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            CourseView courseView;
            CourseLayout course;

            eventDB.Load(TestUtil.GetTestFile("courselayout\\marymoor1.coursescribe"));
            eventDB.Validate();

            // Create the all controls course
            courseView = CourseView.CreateViewingCourseView(eventDB, CourseDesignator.AllControls);
            course = new CourseLayout();
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, defaultCourseAppearance, course, 0);

            CheckHitTest(course, new PointF(9.0F, 12.4F), 0, null, "Water:          special:1  scale:1  location:(7.996275,12.34392)");
            CheckHitTest(course, new PointF(54.7F, 12.2F), 0, null, null);
            CheckHitTest(course, new PointF(0.5F, 9.0F), 0, null, "Control:        control:72  scale:1  location:(-0.7,10.3)  gaps:");
            CheckHitTest(course, new PointF(58.5F, -9.2F), 0, null, "Start:          control:1  scale:1  location:(56.8,-8.7)  orientation:0");
            CheckHitTest(course, new PointF(46.6F, -15.9F), 0, null, @"Code:           control:52  scale:1  text:52  top-left:(45.56,-12.18)
                font-name:Roboto Condensed  font-style:Bold  font-height:4.18");

            // Create course 3
            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(3)));
            course = new CourseLayout();
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, defaultCourseAppearance, course, 0);

            CheckHitTest(course, new PointF(-3.5F, 10.3F), 0, null, "Control:        control:72  course-control:305  scale:1  location:(-0.7,10.3)  gaps:");
            CheckHitTest(course, new PointF(35.6F, 17.7F), 0, null, null);
            CheckHitTest(course, new PointF(59.2F, 18.5F), 0, null, "Leg:            control:71  course-control:307  scale:1  course-control2:308  path:N(42.92,17.55)--N(71.88,19.05)");
            CheckHitTest(course, new PointF(72.1F, 33.5F), 0, null, @"ControlNumber:  control:75  course-control:311  scale:1  text:10  top-left:(66.57,37.12)
                font-name:Roboto  font-style:Regular  font-height:5.57");
            CheckHitTest(course, new PointF(50.2F, -2.9F), 0, null, @"Finish:         control:2  course-control:315  scale:1  location:(53.2,-2.8)  gaps:");

            // Add in all controls.  Test with true and false for all Layers.
            courseView = CourseView.CreateFilteredAllControlsView(eventDB, new CourseDesignator[] { Designator(3) }, ControlPointKind.Normal, 
                                                                  new CourseViewOptions() { showNonDescriptionSpecials = false, showDescriptionSpecials = true });
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, defaultCourseAppearance, course, CourseLayer.AllControls);

            CheckHitTest(course, new PointF(5.1F, -5.1F), CourseLayer.All, null, @"Control:        layer:12  control:76  scale:1  location:(5.6,-5.7)  gaps:");
            CheckHitTest(course, new PointF(5.1F, -5.1F), CourseLayer.MainCourse, null, null);

            // Test the type filter
            CheckHitTest(course, new PointF(59.2F, 18.5F), CourseLayer.MainCourse, (co => co is LegCourseObj), "Leg:            control:71  course-control:307  scale:1  course-control2:308  path:N(42.92,17.55)--N(71.88,19.05)");
            CheckHitTest(course, new PointF(59.2F, 18.5F), CourseLayer.MainCourse, (co => co is PointCourseObj), null);
            CheckHitTest(course, new PointF(-3.5F, 10.3F), CourseLayer.MainCourse, (co => co is PointCourseObj), "Control:        control:72  course-control:305  scale:1  location:(-0.7,10.3)  gaps:");
            CheckHitTest(course, new PointF(-3.5F, 10.3F), CourseLayer.MainCourse, (co => co is LineCourseObj), null);

        }

        [TestMethod]
        public void BoundingRect()
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            CourseView courseView;
            CourseLayout course;

            eventDB.Load(TestUtil.GetTestFile("courselayout\\marymoor1.coursescribe"));
            eventDB.Validate();

            // Create the all controls course
            courseView = CourseView.CreateViewingCourseView(eventDB, CourseDesignator.AllControls);
            course = new CourseLayout();
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, defaultCourseAppearance, course, 0);
            RectangleF bounding = course.BoundingRect();
            RectangleF expected = new RectangleF(-16.41F, -34.3F, 152.17F, 79.95F);
            TestUtil.AssertEqualRect(expected, bounding, 0.01F, "Bounding rect all controls");

            // Try course 1
            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(1)));
            course = new CourseLayout();
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, defaultCourseAppearance, course, 0);
            bounding = course.BoundingRect();
            expected = new RectangleF(-2.11F, -32.9F, 64.73F, 60.43F);
            TestUtil.AssertEqualRect(expected, bounding, 0.1F, "Bounding rect course 1");

            // Try course with control descriptions on it.
            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(10)));
            course = new CourseLayout();
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, defaultCourseAppearance, course, 0);
            bounding = course.BoundingRect();
            expected = new RectangleF(6.0F, -40.1F, 121.94F, 77.22F);
            TestUtil.AssertEqualRect(expected, bounding, 0.1F, "Bounding rect course 10");

            // Do an empty course
            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(11)));
            course = new CourseLayout();
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, defaultCourseAppearance, course, 0);
            bounding = course.BoundingRect();
            expected = RectangleF.FromLTRB(0,0,0,0);
            TestUtil.AssertEqualRect(expected, bounding, 0.001F, "Bounding rect blank course");

        }

        [TestMethod]
        public void AllControlsEquals()
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            CourseView courseView, courseView2;
            CourseLayout course, course2;

            eventDB.Load(TestUtil.GetTestFile("courselayout\\marymoor1.coursescribe"));
            eventDB.Validate();

            // Create the all controls course
            courseView = CourseView.CreateViewingCourseView(eventDB, CourseDesignator.AllControls);
            course = new CourseLayout();
            course.SetLayerColor(CourseLayer.Descriptions, 0, "Black", 0, 0, 0, 1F, false);
            course.SetLayerColor(CourseLayer.MainCourse, 12, "Purple", 0.2F, 1, 0, 0.1F, false);
            course.SetLowerLayerColor(CourseLayer.MainCourse, 13, "Lower purple", 0.2F, 1, 0, 0.1F, false);
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, defaultCourseAppearance, course, CourseLayer.MainCourse);

            // Create the all controls course again
            courseView2 = CourseView.CreateViewingCourseView(eventDB, CourseDesignator.AllControls);
            course2 = new CourseLayout();
            course2.SetLayerColor(CourseLayer.Descriptions, 0, "Black", 0, 0, 0, 1F, false);
            course2.SetLayerColor(CourseLayer.MainCourse, 12, "Purple", 0.2F, 1, 0, 0.1F, false);
            course2.SetLowerLayerColor(CourseLayer.MainCourse, 13, "Lower purple", 0.2F, 1, 0, 0.1F, false);
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView2, defaultCourseAppearance, course2, CourseLayer.MainCourse);

            // Make sure that they are equal.
            Assert.AreEqual(course, course, "CourseLayouts that are equivalent should compare equal.");
            Assert.AreEqual(course, course2, "CourseLayouts that are equivalent should compare equal.");
        }

        [TestMethod]
        public void CourseEquals()
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            CourseView courseView, courseView2;
            CourseLayout course, course2;

            eventDB.Load(TestUtil.GetTestFile("courselayout\\marymoor1.coursescribe"));
            eventDB.Validate();

            // Create the a course view and layout
            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(3)));
            course = new CourseLayout();
            course.SetLayerColor(CourseLayer.Descriptions, 0, "Black", 0, 0, 0, 1F, false);
            course.SetLayerColor(CourseLayer.MainCourse, 12, "Purple", 0.2F, 1, 0, 0.1F, false);
            course.SetLowerLayerColor(CourseLayer.MainCourse, 13, "Lower purple", 0.2F, 1, 0, 0.1F, false);
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, defaultCourseAppearance, course, CourseLayer.MainCourse);

            // Create it again
            courseView2 = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(3)));
            course2 = new CourseLayout();
            course2.SetLayerColor(CourseLayer.Descriptions, 0, "Black", 0, 0, 0, 1F, false);
            course2.SetLayerColor(CourseLayer.MainCourse, 12, "Purple", 0.2F, 1, 0, 0.1F, false);
            course2.SetLowerLayerColor(CourseLayer.MainCourse, 13, "Lower purple", 0.2F, 1, 0, 0.1F, false);
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView2, defaultCourseAppearance, course2, CourseLayer.MainCourse);

            // Make sure that they are equal.
            Assert.AreEqual(course, course, "CourseLayouts that are equivalent should compare equal.");
            Assert.AreEqual(course, course2, "CourseLayouts that are equivalent should compare equal.");
        }
	
	
    }
}

#endif //TEST
