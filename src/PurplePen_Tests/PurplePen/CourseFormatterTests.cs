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
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.Tests
{
    using System.Linq;
    using PurplePen.MapModel;

    [TestClass]
    public class CourseFormatterTests: TestFixtureBase
    {
        void CheckCourse(string testfileName, Id<Course> courseId, CourseLayer layer, string expected)
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            CourseView courseView;
            CourseLayout course;

            eventDB.Load(TestUtil.GetTestFile(testfileName));
            eventDB.Validate();

            // Create the course
            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(courseId));
            course = new CourseLayout();
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, defaultCourseAppearance, course, layer);

            // Dump it to a string.
            StringWriter writer = new StringWriter();
            course.Dump(writer);

            // Check that the string is correct.
            string actual = writer.ToString();
            if (expected != actual) {
                for (int i = 0; i < Math.Min(expected.Length, actual.Length); ++i)
                    if (actual[i] != expected[i]) {
                        Console.WriteLine("Difference at -->{0}", actual.Substring(i, 30));
                        break;
                    }
            }

            Assert.AreEqual(expected, writer.ToString());
        }

        [TestMethod]
        public void DefaultNumberLocation()
        {
            // Check that the default number location absent any constraints is in the right place.
            CheckCourse("courseformat\\marymoor1.coursescribe", CourseId(7), 0, @"
FirstAid:       special:1  scale:1  location:(14.5,31.2)
OOB:            special:4  scale:1  path:N(3,7)--N(11,2)--N(0,-7)--N(-12,-3)--N(3,7)
BasicText:      special:7  scale:1  text:SingleControl  top-left:(45,40)
                font-name:Times New Roman  font-style:Bold, Italic  font-height:4.32542  rect:(45,40)-(70,34)
Description:    layer:1  special:8  scale:1  rect:{X=-50,Y=34.5,Width=40.5,Height=15.5}
Control:        control:36  course-control:701  scale:1  location:(128.4,6.1)  gaps:
ControlNumber:  control:36  course-control:701  scale:1  text:36  top-left:(132.64,13.45)
                font-name:Arial  font-style:Regular  font-height:5.57
");
        }

        [TestMethod]
        public void StartAngle()
        {
            // Check that the default number location absent any constraints is in the right place.
            CheckCourse("courseformat\\marymoor1.coursescribe", CourseId(8), CourseLayer.AllControls, @"
FirstAid:       layer:12  special:1  scale:1  location:(14.5,31.2)
OOB:            layer:12  special:4  scale:1  path:N(3,7)--N(11,2)--N(0,-7)--N(-12,-3)--N(3,7)
BasicText:      layer:12  special:7  scale:1  text:StartAngle  top-left:(45,40)
                font-name:Times New Roman  font-style:Bold, Italic  font-height:5.417989  rect:(45,40)-(70,34)
Description:    layer:1  special:8  scale:1  rect:{X=-50,Y=29.5,Width=40.5,Height=20.5}
Start:          layer:12  control:1  course-control:801  scale:1  location:(56.8,-8.7)  orientation:82.66
Leg:            layer:12  control:1  course-control:801  scale:1  course-control2:802  path:N(52.79,-8.18)--N(23.1,-4.36)
Control:        layer:12  control:58  course-control:802  scale:1  location:(20.3,-4)  gaps:
ControlNumber:  layer:12  control:58  course-control:802  scale:1  text:1  top-left:(12.65,-4.41)
                font-name:Arial  font-style:Regular  font-height:5.57
");
        }

        [TestMethod]
        public void RegularCourse()
        {
            CheckCourse("courseformat\\marymoor1.coursescribe", CourseId(3), CourseLayer.MainCourse, @"
FirstAid:       special:1  scale:1  location:(14.5,31.2)
Crossing:       special:2  scale:1  location:(-4.2,21.7)  orientation:45
Boundary:       special:3  scale:1  path:N(11,2)--N(0,-7)--N(-12,-3)
OOB:            special:4  scale:1  path:N(3,7)--N(11,2)--N(0,-7)--N(-12,-3)--N(3,7)
BasicText:      special:7  scale:1  text:Course 3  top-left:(45,40)
                font-name:Times New Roman  font-style:Bold, Italic  font-height:5.417989  rect:(45,40)-(70,34)
Description:    layer:1  special:8  scale:1  rect:{X=-50,Y=-35.5,Width=40.5,Height=85.5}
Start:          control:1  course-control:301  scale:1  location:(56.8,-8.7)  orientation:162.99
Leg:            control:1  course-control:301  scale:1  course-control2:302  path:N(55.62,-12.56)--N(52.03,-24.3)
Control:        control:59  course-control:302  scale:1  location:(51.2,-27)  gaps:
Leg:            control:59  course-control:302  scale:1  course-control2:303  path:N(48.83,-25.46)--N(28.17,-12.04)
Control:        control:53  course-control:303  scale:1  location:(25.8,-10.5)  gaps:
Leg:            control:53  course-control:303  scale:1  course-control2:304  path:N(26.17,-7.7)--N(27.63,3.4)
Control:        control:41  course-control:304  scale:1  location:(28,6.2)  gaps:
Leg:            control:41  course-control:304  scale:1  course-control2:305  path:N(25.2,6.6)--N(2.1,9.9)
Control:        control:72  course-control:305  scale:1  location:(-0.7,10.3)  gaps:
Leg:            control:72  course-control:305  scale:1  course-control2:306  path:N(-0.27,13.09)--N(2.08,28.53)
Control:        control:47  course-control:306  scale:1  location:(2.51,31.32)  gaps:
ControlNumber:  control:47  course-control:306  scale:1  text:5  top-left:(5.96,27.43)
                font-name:Arial  font-style:Regular  font-height:5.57
Leg:            control:47  course-control:306  scale:1  course-control2:307  path:N(5.16,30.34)--N(37.45,18.38)
Control:        control:71  course-control:307  scale:1  location:(40.1,17.4)  gaps:
Leg:            control:71  course-control:307  scale:1  course-control2:308  path:N(42.92,17.55)--N(71.88,19.05)
Control:        control:77  course-control:308  scale:1  location:(74.7,19.2)  gaps:
Leg:            control:77  course-control:308  scale:1  course-control2:309  path:N(77.26,20.39)--N(91.34,26.91)
Control:        control:78  course-control:309  scale:1  location:(93.9,28.1)  gaps:
Leg:            control:78  course-control:309  scale:1  course-control2:310  path:N(91.53,29.63)--N(86.37,32.97)
Control:        control:43  course-control:310  scale:1  location:(84,34.5)  gaps:
Leg:            control:43  course-control:310  scale:1  course-control2:311  path:N(81.6,33)--N(74.4,28.5)
Control:        control:75  course-control:311  scale:1  location:(72,27)  gaps:
Leg:            control:75  course-control:311  scale:1  course-control2:312  path:N(69.19,26.76)--N(58.71,25.84)
Control:        control:45  course-control:312  scale:1  location:(55.9,25.6)  gaps:
Leg:            control:45  course-control:312  scale:1  course-control2:313  path:N(54.21,23.33)--N(43.59,9.07)  gaps: (s:4.99,l:3.5)
Control:        control:80  course-control:313  scale:1  location:(41.9,6.8)  gaps:
Leg:            control:80  course-control:313  scale:1  course-control2:314  path:N(44.46,5.61)--N(47.74,4.09)
Control:        control:38  course-control:314  scale:1  location:(50.3,2.9)  gaps:
Leg:            control:38  course-control:314  scale:1  course-control2:315  path:N(51.58,0.38)--N(51.69,0.16)
Finish:         control:2  course-control:315  scale:1  location:(53.2,-2.8)  gaps:
ControlNumber:  control:59  course-control:302  scale:1  text:1  top-left:(51.93,-30.59)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:53  course-control:303  scale:1  text:2  top-left:(18.15,-10.91)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:41  course-control:304  scale:1  text:3  top-left:(28.26,16.06)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:72  course-control:305  scale:1  text:4  top-left:(3.2,18.19)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:71  course-control:307  scale:1  text:6  top-left:(40.36,27.26)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:77  course-control:308  scale:1  text:7  top-left:(75.43,15.61)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:78  course-control:309  scale:1  text:8  top-left:(98.59,28.14)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:43  course-control:310  scale:1  text:9  top-left:(82.89,44.37)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:75  course-control:311  scale:1  text:10  top-left:(68.01,36.87)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:45  course-control:312  scale:1  text:11  top-left:(45.23,32.45)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:80  course-control:313  scale:1  text:12  top-left:(35.47,3.16)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:38  course-control:314  scale:1  text:13  top-left:(52.11,12.41)
                font-name:Arial  font-style:Regular  font-height:5.57
");
        }

        [TestMethod]
        public void ControlGaps()
        {
            CheckCourse("courseformat\\marymoor2.coursescribe", CourseId(1), CourseLayer.MainCourse, @"
FirstAid:       special:1  scale:1  location:(14.5,31.2)
Crossing:       special:2  scale:1  location:(-4.2,21.7)  orientation:45
OOB:            special:4  scale:1  path:N(3,7)--N(11,2)--N(0,-7)--N(-12,-3)--N(3,7)
BasicText:      special:7  scale:1  text:Course 1  top-left:(45,40)
                font-name:Times New Roman  font-style:Bold, Italic  font-height:5.417989  rect:(45,40)-(70,34)
Description:    layer:1  special:8  scale:1  rect:{X=-50,Y=-20.5,Width=65.5,Height=70.5}
Start:          control:1  course-control:101  scale:1  location:(56.8,-8.7)  orientation:186.71
Control:        control:73  course-control:102  scale:1  location:(57.4,-13.8)  gaps:48.7056961:144.71402
Leg:            control:73  course-control:102  scale:1  course-control2:103  path:N(54.83,-14.97)--N(40.17,-21.63)
Control:        control:44  course-control:103  scale:1  location:(37.6,-22.8)  gaps:
Leg:            control:44  course-control:103  scale:1  course-control2:104  path:N(34.8,-22.44)--N(24.8,-21.16)
Control:        control:49  course-control:104  scale:1  location:(22,-20.8)  gaps:157.5:191.25
Leg:            control:49  course-control:104  scale:1  course-control2:105  path:N(22.98,-18.15)--N(24.82,-13.15)
Control:        control:53  course-control:105  scale:1  location:(25.8,-10.5)  gaps:
Leg:            control:53  course-control:105  scale:1  course-control2:106  path:N(23.98,-8.34)--N(22.12,-6.16)
Control:        control:58  course-control:106  scale:1  location:(20.3,-4)  gaps:
Leg:            control:58  course-control:106  scale:1  course-control2:107  path:N(17.49,-4.32)--N(8.41,-5.38)
Control:        control:76  course-control:107  scale:1  location:(5.6,-5.7)  gaps:
Leg:            control:76  course-control:107  scale:1  course-control2:108  path:N(7.49,-3.6)--N(19.41,9.7)
Control:        control:70  course-control:108  scale:1  location:(21.3,11.8)  gaps:
Leg:            control:70  course-control:108  scale:1  course-control2:109  path:N(24.01,12.61)--N(37.39,16.59)
Control:        control:71  course-control:109  scale:1  location:(40.1,17.4)  gaps:
Leg:            control:71  course-control:109  scale:1  course-control2:110  path:N(42.5,15.91)--N(48.8,11.99)
Control:        control:55  course-control:110  scale:1  location:(51.2,10.5)  gaps:
Leg:            control:55  course-control:110  scale:1  course-control2:111  path:N(50.87,7.69)--N(50.63,5.71)
Control:        control:38  course-control:111  scale:1  location:(50.3,2.9)  gaps:
Leg:            control:38  course-control:111  scale:1  course-control2:112  path:N(51.58,0.38)--N(51.69,0.16)
Finish:         control:2  course-control:112  scale:1  location:(53.2,-2.8)  gaps:
ControlNumber:  control:73  course-control:102  scale:1  text:1  top-left:(59.48,-16.97)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:44  course-control:103  scale:1  text:2  top-left:(36.94,-26.44)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:49  course-control:104  scale:1  text:3  top-left:(18.64,-24.44)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:53  course-control:105  scale:1  text:4  top-left:(30.62,-5.23)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:58  course-control:106  scale:1  text:5  top-left:(24.2,3.89)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:76  course-control:107  scale:1  text:6  top-left:(10.15,0.94)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:70  course-control:108  scale:1  text:7  top-left:(20.19,21.67)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:71  course-control:109  scale:1  text:8  top-left:(40.36,27.26)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:55  course-control:110  scale:1  text:9  top-left:(55.75,17.14)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:38  course-control:111  scale:1  text:10  top-left:(39.28,6.53)
                font-name:Arial  font-style:Regular  font-height:5.57
");


            CheckCourse("courseformat\\marymoor2.coursescribe", CourseId(11), CourseLayer.MainCourse, @"
FirstAid:       special:1  scale:0.5  location:(14.5,31.2)
OOB:            special:4  scale:0.5  path:N(3,7)--N(11,2)--N(0,-7)--N(-12,-3)--N(3,7)
BasicText:      special:7  scale:1  text:5K  top-left:(45,40)
                font-name:Times New Roman  font-style:Bold, Italic  font-height:5.417989  rect:(45,40)-(70,34)
Description:    layer:1  special:8  scale:1  rect:{X=-50,Y=9.5,Width=40.5,Height=40.5}
Start:          control:1  course-control:916  scale:0.5  location:(56.8,-8.7)  orientation:126.29
Leg:            control:1  course-control:916  scale:0.5  course-control2:920  path:N(55.17,-9.9)--N(38.74,-21.96)
Control:        control:44  course-control:920  scale:0.5  location:(37.6,-22.8)  gaps:
Leg:            control:44  course-control:920  scale:0.5  course-control2:921  path:N(36.2,-22.62)--N(23.4,-20.98)
Control:        control:49  course-control:921  scale:0.5  location:(22,-20.8)  gaps:11.25:45
Leg:            control:49  course-control:921  scale:0.5  course-control2:918  path:N(22.31,-19.42)--N(27.69,4.82)
Control:        control:41  course-control:918  scale:0.5  location:(28,6.2)  gaps:
Leg:            control:41  course-control:918  scale:0.5  course-control2:919  path:N(29.04,7.16)--N(39.06,16.44)
Control:        control:71  course-control:919  scale:0.5  location:(40.1,17.4)  gaps:
Leg:            control:71  course-control:919  scale:0.5  course-control2:917  path:N(40.87,16.21)--N(52.3,-1.41)
Finish:         control:2  course-control:917  scale:0.5  location:(53.2,-2.8)  gaps:
ControlNumber:  control:44  course-control:920  scale:1  text:1  top-left:(37.96,-24.59)
                font-name:Arial  font-style:Regular  font-height:2.785
ControlNumber:  control:49  course-control:921  scale:1  text:2  top-left:(18.5,-21.63)
                font-name:Arial  font-style:Regular  font-height:2.785
ControlNumber:  control:41  course-control:918  scale:1  text:3  top-left:(24.37,9.95)
                font-name:Arial  font-style:Regular  font-height:2.785
ControlNumber:  control:71  course-control:919  scale:1  text:4  top-left:(40.23,22.33)
                font-name:Arial  font-style:Regular  font-height:2.785
");
        }

        [TestMethod]
        public void ScoreCourse()
        {
            CheckCourse("courseformat\\marymoor1.coursescribe", CourseId(9), 0, @"
FirstAid:       special:1  scale:1  location:(14.5,31.2)
Crossing:       special:2  scale:1  location:(-4.2,21.7)  orientation:45
OOB:            special:4  scale:1  path:N(3,7)--N(11,2)--N(0,-7)--N(-12,-3)--N(3,7)
BasicText:      special:7  scale:1  text:Score  top-left:(45,40)
                font-name:Times New Roman  font-style:Bold, Italic  font-height:5.417989  rect:(45,40)-(70,34)
Description:    layer:1  special:8  scale:1  rect:{X=-50,Y=-30.5,Width=40.5,Height=80.5}
Start:          control:1  course-control:901  scale:1  location:(56.8,-8.7)  orientation:0
Control:        control:38  course-control:908  scale:1  location:(50.3,2.9)  gaps:
Control:        control:41  course-control:914  scale:1  location:(28,6.2)  gaps:
Control:        control:44  course-control:912  scale:1  location:(37.6,-22.8)  gaps:
ControlNumber:  control:44  course-control:912  scale:1  text:44  top-left:(40.5,-27.69)
                font-name:Arial  font-style:Regular  font-height:5.57
Control:        control:52  course-control:910  scale:1  location:(43,-11.1)  gaps:
Control:        control:54  course-control:907  scale:1  location:(53.7,8.2)  gaps:84.34535:190.426529
Control:        control:55  course-control:906  scale:1  location:(51.2,10.5)  gaps:-95.65466:10.4265289
Control:        control:56  course-control:903  scale:1  location:(81.3,24)  gaps:
Control:        control:70  course-control:913  scale:1  location:(21.3,11.8)  gaps:
Control:        control:71  course-control:909  scale:1  location:(40.1,17.4)  gaps:
Control:        control:73  course-control:911  scale:1  location:(57.4,-13.8)  gaps:48.7056961:144.71402
Control:        control:75  course-control:902  scale:1  location:(72,27)  gaps:
Control:        control:77  course-control:905  scale:1  location:(74.7,19.2)  gaps:
Control:        control:78  course-control:904  scale:1  location:(93.9,28.1)  gaps:
Finish:         control:2  course-control:915  scale:1  location:(53.2,-2.8)  gaps:
ControlNumber:  control:38  course-control:908  scale:1  text:38  top-left:(39.28,6.53)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:41  course-control:914  scale:1  text:41  top-left:(24.46,2.56)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:52  course-control:910  scale:1  text:52  top-left:(31.98,-7.47)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:54  course-control:907  scale:1  text:54  top-left:(58.52,9.19)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:55  course-control:906  scale:1  text:55  top-left:(47.21,20.37)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:56  course-control:903  scale:1  text:56  top-left:(83.61,20.94)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:70  course-control:913  scale:1  text:70  top-left:(10.63,18.65)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:71  course-control:909  scale:1  text:71  top-left:(30.3,25.65)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:73  course-control:911  scale:1  text:73  top-left:(59.71,-16.86)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:75  course-control:902  scale:1  text:75  top-left:(66.61,36.87)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:77  course-control:905  scale:1  text:77  top-left:(71.16,15.56)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:78  course-control:904  scale:1  text:78  top-left:(97.11,36.74)
                font-name:Arial  font-style:Regular  font-height:5.57
");
        }

        [TestMethod]
        public void ScoreCourseSequence() {
            CheckCourse("courseformat\\marymoor4.coursescribe", CourseId(9), 0, @"
FirstAid:       special:1  scale:1  location:(14.5,31.2)
Crossing:       special:2  scale:1  location:(-4.2,21.7)  orientation:45
OOB:            special:4  scale:1  path:N(3,7)--N(11,2)--N(0,-7)--N(-12,-3)--N(3,7)
BasicText:      special:7  scale:1  text:Score  top-left:(45,40)
                font-name:Times New Roman  font-style:Bold, Italic  font-height:5.417989  rect:(45,40)-(70,34)
Description:    layer:1  special:8  scale:1  rect:{X=-50,Y=-30.5,Width=40.5,Height=80.5}
Start:          control:1  course-control:901  scale:1  location:(56.8,-8.7)  orientation:0
Control:        control:38  course-control:908  scale:1  location:(50.3,2.9)  gaps:
Control:        control:41  course-control:914  scale:1  location:(28,6.2)  gaps:
Control:        control:44  course-control:912  scale:1  location:(37.6,-22.8)  gaps:
ControlNumber:  control:44  course-control:912  scale:1  text:3  top-left:(42.05,-27.69)
                font-name:Arial  font-style:Regular  font-height:5.57
Control:        control:52  course-control:910  scale:1  location:(43,-11.1)  gaps:
Control:        control:54  course-control:907  scale:1  location:(53.7,8.2)  gaps:84.34535:190.426529
Control:        control:55  course-control:906  scale:1  location:(51.2,10.5)  gaps:-95.65466:10.4265289
Control:        control:56  course-control:903  scale:1  location:(81.3,24)  gaps:
Control:        control:70  course-control:913  scale:1  location:(21.3,11.8)  gaps:
Control:        control:71  course-control:909  scale:1  location:(40.1,17.4)  gaps:
Control:        control:73  course-control:911  scale:1  location:(57.4,-13.8)  gaps:48.7056961:144.71402
Control:        control:75  course-control:902  scale:1  location:(72,27)  gaps:
Control:        control:77  course-control:905  scale:1  location:(74.7,19.2)  gaps:
Control:        control:78  course-control:904  scale:1  location:(93.9,28.1)  gaps:
Finish:         control:2  course-control:915  scale:1  location:(53.2,-2.8)  gaps:
ControlNumber:  control:38  course-control:908  scale:1  text:1  top-left:(42.38,6.43)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:41  course-control:914  scale:1  text:2  top-left:(28.73,2.61)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:52  course-control:910  scale:1  text:4  top-left:(35.08,-7.57)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:54  course-control:907  scale:1  text:5  top-left:(58.39,8.24)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:55  course-control:906  scale:1  text:6  top-left:(48.76,20.37)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:56  course-control:903  scale:1  text:7  top-left:(84.56,21.63)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:70  course-control:913  scale:1  text:8  top-left:(14.04,19.29)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:71  course-control:909  scale:1  text:9  top-left:(33.74,26)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:73  course-control:911  scale:1  text:10  top-left:(59.71,-16.86)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:75  course-control:902  scale:1  text:11  top-left:(66.61,36.87)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:77  course-control:905  scale:1  text:12  top-left:(69.79,15.56)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:78  course-control:904  scale:1  text:13  top-left:(97.11,36.74)
                font-name:Arial  font-style:Regular  font-height:5.57
");
        }

        [TestMethod]
        public void ScoreCourseSequenceAndCode() {
            CheckCourse("courseformat\\marymoor5.coursescribe", CourseId(9), 0, @"
FirstAid:       special:1  scale:1  location:(14.5,31.2)
Crossing:       special:2  scale:1  location:(-4.2,21.7)  orientation:45
OOB:            special:4  scale:1  path:N(3,7)--N(11,2)--N(0,-7)--N(-12,-3)--N(3,7)
BasicText:      special:7  scale:1  text:Score  top-left:(45,40)
                font-name:Times New Roman  font-style:Bold, Italic  font-height:5.417989  rect:(45,40)-(70,34)
Description:    layer:1  special:8  scale:1  rect:{X=-50,Y=-30.5,Width=40.5,Height=80.5}
Start:          control:1  course-control:901  scale:1  location:(56.8,-8.7)  orientation:0
Control:        control:38  course-control:908  scale:1  location:(50.3,2.9)  gaps:
Control:        control:41  course-control:914  scale:1  location:(28,6.2)  gaps:
Control:        control:44  course-control:912  scale:1  location:(37.6,-22.8)  gaps:
ControlNumber:  control:44  course-control:912  scale:1  text:3-44  top-left:(38.03,-27.69)
                font-name:Arial  font-style:Regular  font-height:5.57
Control:        control:52  course-control:910  scale:1  location:(43,-11.1)  gaps:
Control:        control:54  course-control:907  scale:1  location:(53.7,8.2)  gaps:84.34535:190.426529
Control:        control:55  course-control:906  scale:1  location:(51.2,10.5)  gaps:-95.65466:10.4265289
Control:        control:56  course-control:903  scale:1  location:(81.3,24)  gaps:
Control:        control:70  course-control:913  scale:1  location:(21.3,11.8)  gaps:
Control:        control:71  course-control:909  scale:1  location:(40.1,17.4)  gaps:
Control:        control:73  course-control:911  scale:1  location:(57.4,-13.8)  gaps:48.7056961:144.71402
Control:        control:75  course-control:902  scale:1  location:(72,27)  gaps:
Control:        control:77  course-control:905  scale:1  location:(74.7,19.2)  gaps:
Control:        control:78  course-control:904  scale:1  location:(93.9,28.1)  gaps:
Finish:         control:2  course-control:915  scale:1  location:(53.2,-2.8)  gaps:
ControlNumber:  control:38  course-control:908  scale:1  text:1-38  top-left:(34.33,6.69)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:41  course-control:914  scale:1  text:2-41  top-left:(19.09,2.56)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:52  course-control:910  scale:1  text:4-52  top-left:(27.03,-9.36)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:54  course-control:907  scale:1  text:5-54  top-left:(58.53,10.63)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:55  course-control:906  scale:1  text:6-55  top-left:(44.74,20.37)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:56  course-control:903  scale:1  text:7-56  top-left:(81.64,20.37)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:70  course-control:913  scale:1  text:8-70  top-left:(6.2,19.61)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:71  course-control:909  scale:1  text:9-71  top-left:(26.55,26.63)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:73  course-control:911  scale:1  text:10-73  top-left:(60.91,-15.93)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:75  course-control:902  scale:1  text:11-75  top-left:(62.58,36.87)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:77  course-control:905  scale:1  text:12-77  top-left:(55.79,25.47)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:78  course-control:904  scale:1  text:13-78  top-left:(96.8,37)
                font-name:Arial  font-style:Regular  font-height:5.57
");
        }

        [TestMethod]
        public void ScoreCourseSequenceAndScore()
        {
            CheckCourse("courseformat\\marymoor8.coursescribe", CourseId(9), 0, @"
FirstAid:       special:1  scale:1  location:(14.5,31.2)
Crossing:       special:2  scale:1  location:(-4.2,21.7)  orientation:45
OOB:            special:4  scale:1  path:N(3,7)--N(11,2)--N(0,-7)--N(-12,-3)--N(3,7)
BasicText:      special:7  scale:1  text:Score  top-left:(45,40)
                font-name:Times New Roman  font-style:Bold, Italic  font-height:5.417989  rect:(45,40)-(70,34)
Description:    layer:1  special:8  scale:1  rect:{X=-50,Y=-30.5,Width=40.5,Height=80.5}
Start:          control:1  course-control:901  scale:1  location:(56.8,-8.7)  orientation:0
Control:        control:41  course-control:914  scale:1  location:(28,6.2)  gaps:
Control:        control:44  course-control:912  scale:1  location:(37.6,-22.8)  gaps:
ControlNumber:  control:44  course-control:912  scale:1  text:2  top-left:(42.05,-27.69)
                font-name:Arial  font-style:Regular  font-height:5.57
Control:        control:38  course-control:908  scale:1  location:(50.3,2.9)  gaps:
Control:        control:78  course-control:904  scale:1  location:(93.9,28.1)  gaps:
Control:        control:75  course-control:902  scale:1  location:(72,27)  gaps:
Control:        control:77  course-control:905  scale:1  location:(74.7,19.2)  gaps:
Control:        control:73  course-control:911  scale:1  location:(57.4,-13.8)  gaps:48.7056961:144.71402
Control:        control:70  course-control:913  scale:1  location:(21.3,11.8)  gaps:
Control:        control:71  course-control:909  scale:1  location:(40.1,17.4)  gaps:
Control:        control:54  course-control:907  scale:1  location:(53.7,8.2)  gaps:84.34535:190.426529
Control:        control:55  course-control:906  scale:1  location:(51.2,10.5)  gaps:-95.65466:10.4265289
Control:        control:56  course-control:903  scale:1  location:(81.3,24)  gaps:
Control:        control:52  course-control:910  scale:1  location:(43,-11.1)  gaps:
Finish:         control:2  course-control:915  scale:1  location:(53.2,-2.8)  gaps:
ControlNumber:  control:41  course-control:914  scale:1  text:1  top-left:(31.26,3.83)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:38  course-control:908  scale:1  text:3(5)  top-left:(36.29,10.48)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:78  course-control:904  scale:1  text:4(5)  top-left:(98.54,34.47)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:75  course-control:902  scale:1  text:5(10)  top-left:(63.21,36.87)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:77  course-control:905  scale:1  text:6(10)  top-left:(60.63,15.67)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:73  course-control:911  scale:1  text:7(15)  top-left:(61.1,-15.72)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:70  course-control:913  scale:1  text:8(20)  top-left:(4.6,19.94)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:71  course-control:909  scale:1  text:9(20)  top-left:(27.67,27.27)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:54  course-control:907  scale:1  text:10(25)  top-left:(56.9,5.77)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:55  course-control:906  scale:1  text:11(25)  top-left:(42.26,20.37)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:56  course-control:903  scale:1  text:12(25)  top-left:(79.17,20.36)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:52  course-control:910  scale:1  text:13(30)  top-left:(22.07,-9.68)
                font-name:Arial  font-style:Regular  font-height:5.57
");
        }


        [TestMethod]
        public void ScoreCourseCodeAndScore()
        {
            CheckCourse("courseformat\\marymoor7.coursescribe", CourseId(9), 0, @"
FirstAid:       special:1  scale:1  location:(14.5,31.2)
Crossing:       special:2  scale:1  location:(-4.2,21.7)  orientation:45
OOB:            special:4  scale:1  path:N(3,7)--N(11,2)--N(0,-7)--N(-12,-3)--N(3,7)
BasicText:      special:7  scale:1  text:Score  top-left:(45,40)
                font-name:Times New Roman  font-style:Bold, Italic  font-height:5.417989  rect:(45,40)-(70,34)
Description:    layer:1  special:8  scale:1  rect:{X=-50,Y=-30.5,Width=40.5,Height=80.5}
Start:          control:1  course-control:901  scale:1  location:(56.8,-8.7)  orientation:0
Control:        control:41  course-control:914  scale:1  location:(28,6.2)  gaps:
Control:        control:44  course-control:912  scale:1  location:(37.6,-22.8)  gaps:
ControlNumber:  control:44  course-control:912  scale:1  text:44  top-left:(40.5,-27.69)
                font-name:Arial  font-style:Regular  font-height:5.57
Control:        control:38  course-control:908  scale:1  location:(50.3,2.9)  gaps:
Control:        control:78  course-control:904  scale:1  location:(93.9,28.1)  gaps:
Control:        control:75  course-control:902  scale:1  location:(72,27)  gaps:
Control:        control:77  course-control:905  scale:1  location:(74.7,19.2)  gaps:
Control:        control:73  course-control:911  scale:1  location:(57.4,-13.8)  gaps:48.7056961:144.71402
Control:        control:70  course-control:913  scale:1  location:(21.3,11.8)  gaps:
Control:        control:71  course-control:909  scale:1  location:(40.1,17.4)  gaps:
Control:        control:54  course-control:907  scale:1  location:(53.7,8.2)  gaps:84.34535:190.426529
Control:        control:55  course-control:906  scale:1  location:(51.2,10.5)  gaps:-95.65466:10.4265289
Control:        control:56  course-control:903  scale:1  location:(81.3,24)  gaps:
Control:        control:52  course-control:910  scale:1  location:(43,-11.1)  gaps:
Finish:         control:2  course-control:915  scale:1  location:(53.2,-2.8)  gaps:
ControlNumber:  control:41  course-control:914  scale:1  text:41  top-left:(31.6,4.17)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:38  course-control:908  scale:1  text:38(5)  top-left:(55.13,5.27)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:78  course-control:904  scale:1  text:78(5)  top-left:(98.38,34.94)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:75  course-control:902  scale:1  text:75(10)  top-left:(61.66,36.87)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:77  course-control:905  scale:1  text:77(10)  top-left:(53.77,20.62)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:73  course-control:911  scale:1  text:73(15)  top-left:(60.6,-16.23)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:70  course-control:913  scale:1  text:70(20)  top-left:(2,20.46)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:71  course-control:909  scale:1  text:71(20)  top-left:(26.12,27.27)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:54  course-control:907  scale:1  text:54(25)  top-left:(32.77,12.16)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:55  course-control:906  scale:1  text:55(25)  top-left:(40.86,20.37)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:56  course-control:903  scale:1  text:56(25)  top-left:(79.17,20.36)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:52  course-control:910  scale:1  text:52(30)  top-left:(22.07,-9.68)
                font-name:Arial  font-style:Regular  font-height:5.57
");
        }



        [TestMethod]
        public void AllControls()
        {
            CheckCourse("courseformat\\marymoor1.coursescribe", CourseId(0), CourseLayer.AllControls, @"
FirstAid:       layer:12  special:1  scale:1  location:(14.5,31.2)
OOB:            layer:12  special:4  scale:1  path:N(3,7)--N(11,2)--N(0,-7)--N(-12,-3)--N(3,7)
BasicText:      layer:12  special:7  scale:1  text:All controls  top-left:(45,40)
                font-name:Times New Roman  font-style:Bold, Italic  font-height:5.201138  rect:(45,40)-(70,34)
Description:    layer:1  special:8  scale:1  rect:{X=-50,Y=-150.5,Width=40.5,Height=200.5}
Start:          layer:12  control:1  scale:1  location:(56.8,-8.7)  orientation:0
Control:        layer:12  control:35  scale:1  location:(115.6,-11.8)  gaps:
Control:        layer:12  control:36  scale:1  location:(128.4,6.1)  gaps:
Control:        layer:12  control:37  scale:1  location:(121.8,-23)  gaps:
Control:        layer:12  control:38  scale:1  location:(50.3,2.9)  gaps:
Control:        layer:12  control:39  scale:1  location:(129.2,37.7)  gaps:
Control:        layer:12  control:40  scale:1  location:(124.2,29.5)  gaps:
Control:        layer:12  control:41  scale:1  location:(28,6.2)  gaps:
Control:        layer:12  control:42  scale:1  location:(38.4,-14)  gaps:
Control:        layer:12  control:43  scale:1  location:(84,34.5)  gaps:
Control:        layer:12  control:44  scale:1  location:(37.6,-22.8)  gaps:
Control:        layer:12  control:45  scale:1  location:(55.9,25.6)  gaps:
Control:        layer:12  control:46  scale:1  location:(-9.2,8.3)  gaps:
Control:        layer:12  control:47  scale:1  location:(2.51,31.32)  gaps:
Control:        layer:12  control:48  scale:1  location:(21.5,40.2)  gaps:
Control:        layer:12  control:49  scale:1  location:(22,-20.8)  gaps:
Control:        layer:12  control:50  scale:1  location:(45.2,22.8)  gaps:
Control:        layer:12  control:51  scale:1  location:(16.4,-11.9)  gaps:
Control:        layer:12  control:52  scale:1  location:(43,-11.1)  gaps:
Control:        layer:12  control:53  scale:1  location:(25.8,-10.5)  gaps:
Control:        layer:12  control:54  scale:1  location:(53.7,8.2)  gaps:
Control:        layer:12  control:55  scale:1  location:(51.2,10.5)  gaps:
Control:        layer:12  control:56  scale:1  location:(81.3,24)  gaps:
Control:        layer:12  control:57  scale:1  location:(90,19.3)  gaps:
Control:        layer:12  control:58  scale:1  location:(20.3,-4)  gaps:
Control:        layer:12  control:59  scale:1  location:(51.2,-27)  gaps:
Control:        layer:12  control:70  scale:1  location:(21.3,11.8)  gaps:
Control:        layer:12  control:71  scale:1  location:(40.1,17.4)  gaps:
Control:        layer:12  control:72  scale:1  location:(-0.7,10.3)  gaps:
Control:        layer:12  control:73  scale:1  location:(57.4,-13.8)  gaps:
Control:        layer:12  control:74  scale:1  location:(61.2,-18.8)  gaps:
Control:        layer:12  control:75  scale:1  location:(72,27)  gaps:
Control:        layer:12  control:76  scale:1  location:(5.6,-5.7)  gaps:
Control:        layer:12  control:77  scale:1  location:(74.7,19.2)  gaps:
Control:        layer:12  control:78  scale:1  location:(93.9,28.1)  gaps:
Control:        layer:12  control:79  scale:1  location:(119.8,8.6)  gaps:
Control:        layer:12  control:80  scale:1  location:(41.9,6.8)  gaps:
Finish:         layer:12  control:2  scale:1  location:(53.2,-2.8)  gaps:
Code:           layer:12  control:35  scale:1  text:35  top-left:(109.94,-5.13)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:36  scale:1  text:36  top-left:(131.57,5.98)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:37  scale:1  text:37  top-left:(123.64,-24.89)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:38  scale:1  text:38  top-left:(43.45,2.43)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:39  scale:1  text:39  top-left:(130.72,44.56)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:40  scale:1  text:40  top-left:(118.87,27.42)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:41  scale:1  text:41  top-left:(28.82,3.86)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:42  scale:1  text:42  top-left:(31.26,-12.3)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:43  scale:1  text:43  top-left:(81.46,41.72)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:44  scale:1  text:44  top-left:(35.38,-25.25)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:45  scale:1  text:45  top-left:(57.74,23.71)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:46  scale:1  text:46  top-left:(-8.74,15.49)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:47  scale:1  text:47  top-left:(0.29,28.87)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:48  scale:1  text:48  top-left:(23.92,46.38)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:49  scale:1  text:49  top-left:(20.73,-23.25)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:50  scale:1  text:50  top-left:(40.56,29.92)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:51  scale:1  text:51  top-left:(10.17,-13.3)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:52  scale:1  text:52  top-left:(45.66,-12.22)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:53  scale:1  text:53  top-left:(28.22,-4.32)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:54  scale:1  text:54  top-left:(56.87,8.08)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:55  scale:1  text:55  top-left:(47.65,17.72)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:56  scale:1  text:56  top-left:(84.34,29.24)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:57  scale:1  text:57  top-left:(89.73,16.85)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:58  scale:1  text:58  top-left:(16.75,3.22)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:59  scale:1  text:59  top-left:(49.93,-29.45)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:70  scale:1  text:70  top-left:(14.83,17.69)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:71  scale:1  text:71  top-left:(32.96,19.1)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:72  scale:1  text:72  top-left:(0.82,17.16)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:73  scale:1  text:73  top-left:(52.07,-15.88)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:74  scale:1  text:74  top-left:(63.04,-20.69)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:75  scale:1  text:75  top-left:(64.86,29.73)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:76  scale:1  text:76  top-left:(3.38,-8.15)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:77  scale:1  text:77  top-left:(70.43,16.79)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:78  scale:1  text:78  top-left:(97.21,32.26)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:79  scale:1  text:79  top-left:(112.81,13.49)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:12  control:80  scale:1  text:80  top-left:(34.76,10.59)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
");
        }
	
        [TestMethod]
        public void Text()
        {
            CheckCourse("courseformat\\marymoor1.coursescribe", CourseId(10), 0, @"
FirstAid:       special:1  scale:1  location:(14.5,31.2)
OOB:            special:4  scale:1  path:N(3,7)--N(11,2)--N(0,-7)--N(-12,-3)--N(3,7)
BasicText:      special:5  scale:1  text:Banana Apple  top-left:(13,17)
                font-name:Times New Roman  font-style:Bold  font-height:9.530931  rect:(13,17)-(71,1)
BasicText:      special:6  scale:1  text:Frank Zappa  top-left:(13,-14)
                font-name:Arial  font-style:Bold  font-height:2.685315  rect:(13,-14)-(71,-17)
BasicText:      special:7  scale:1  text:Xavier  top-left:(45,40)
                font-name:Times New Roman  font-style:Bold, Italic  font-height:5.417989  rect:(45,40)-(70,34)
Description:    layer:1  special:8  scale:1  rect:{X=-50,Y=39.5,Width=40.5,Height=10.5}
");
        }

        [TestMethod]
        public void SpecialLegs()
        {
            CheckCourse("courseformat\\speciallegs.coursescribe", CourseId(1), 0, @"
Start:          control:1  course-control:1  scale:1  location:(74.1,14)  orientation:135
Leg:            control:1  course-control:1  scale:1  course-control2:2  path:N(71.24,11.14)--N(39.7,-20.4)
Control:        control:2  course-control:2  scale:1  location:(37.7,-22.4)  gaps:
FlaggedLeg:     control:2  course-control:2  scale:1  course-control2:3  path:N(35.72,-20.39)--N(10.18,5.49)
Control:        control:3  course-control:3  scale:1  location:(8.2,7.5)  gaps:
FlaggedLeg:     control:3  course-control:3  scale:1  course-control2:4  path:N(9.02,10.2)--N(12,20)
Leg:            control:3  course-control:3  scale:1  course-control2:4  path:N(12,20)--N(23.6,38.51)
Control:        control:4  course-control:4  scale:1  location:(25.1,40.9)  gaps:
Leg:            control:4  course-control:4  scale:1  course-control2:5  path:N(27.92,40.93)--N(35,41)--N(50,30)--N(65.67,45.23)
Control:        control:5  course-control:5  scale:1  location:(67.7,47.2)  gaps:
Leg:            control:5  course-control:5  scale:1  course-control2:6  path:N(69.65,45.15)--N(93.71,19.91)
Finish:         control:6  course-control:6  scale:1  location:(96,17.5)  gaps:
ControlNumber:  control:2  course-control:2  scale:1  text:1  top-left:(35.71,-26.04)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:3  course-control:3  scale:1  text:2  top-left:(0.28,8.45)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:4  course-control:4  scale:1  text:3  top-left:(19.92,50.3)
                font-name:Arial  font-style:Regular  font-height:5.57
ControlNumber:  control:5  course-control:5  scale:1  text:4  top-left:(66.59,57.07)
                font-name:Arial  font-style:Regular  font-height:5.57
");
        }

        [TestMethod]
        public void DisplayAllCourses()
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            CourseView courseView;
            CourseLayout course;

            eventDB.Load(TestUtil.GetTestFile("courseformat\\marymoor1.coursescribe"));
            eventDB.Validate();

            foreach (Id<Course> courseId in QueryEvent.SortedCourseIds(eventDB)) {
                courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(courseId));
                course = new CourseLayout();
                CourseFormatter.FormatCourseToLayout(symbolDB, courseView, defaultCourseAppearance, course, 0);
                course.Dump(Console.Out);
                Console.WriteLine();
            }

            courseView = CourseView.CreateViewingCourseView(eventDB, CourseDesignator.AllControls);
            course = new CourseLayout();
            CourseFormatter.FormatCourseToLayout(symbolDB, courseView, defaultCourseAppearance, course, 0);
            course.Dump(Console.Out);
        }

        [TestMethod]
        public void GetTextSize()
        {
            FontDesc myFont = new FontDesc("Arial", false, false, 5);
            SizeF size = CourseFormatter.GetTextSize("1234", myFont, 1);
            Assert.AreEqual(11.12, size.Width, 0.01); 
            Assert.AreEqual(3.46, size.Height, 0.01);

            Bitmap bm = new Bitmap(250, 250);
            using (Graphics g = Graphics.FromImage(bm))
            using (Font font = myFont.GetFont()) {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.TranslateTransform(bm.Width / 2, bm.Height / 2);
                g.ScaleTransform((float) (bm.Width / 20.0), (float) (bm.Height / 20.0));

                StringFormat format = new StringFormat(StringFormat.GenericTypographic);
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.FormatFlags |= StringFormatFlags.NoClip;

                RectangleF rect = new RectangleF(new PointF(-6, -6), size);
                g.Clear(Color.White);
                g.DrawString("1234", font, Brushes.Black, rect, format);
                g.DrawRectangle(new Pen(Color.Red, 0.05F), rect.Left, rect.Top, rect.Width, rect.Height);
            }

            TestUtil.CheckBitmapsBase(bm, "courseformat\\textsize");
        }

        [TestMethod]
        public void GetTextSize2()
        {
            FontDesc myFont = new FontDesc("Times New Roman", true, true, 5);
            SizeF size = CourseFormatter.GetTextSize("1234", myFont, 1.3F);
            Assert.AreEqual(13, size.Width, 0.01);
            Assert.AreEqual(4.38, size.Height, 0.01);

            Bitmap bm = new Bitmap(250, 250);
            using (Graphics g = Graphics.FromImage(bm))
            using (Font font = myFont.GetScaledFont(1.3F)) {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.TranslateTransform(bm.Width / 2, bm.Height / 2);
                g.ScaleTransform((float) (bm.Width / 20.0), (float) (bm.Height / 20.0));

                StringFormat format = new StringFormat(StringFormat.GenericTypographic);
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.FormatFlags |= StringFormatFlags.NoClip;

                RectangleF rect = new RectangleF(new PointF(-6, -6), size);
                g.Clear(Color.White);
                g.DrawString("1234", font, Brushes.Black, rect, format);
                g.DrawRectangle(new Pen(Color.Red, 0.05F), rect.Left, rect.Top, rect.Width, rect.Height);
            }

            TestUtil.CheckBitmapsBase(bm, "courseformat\\textsize2");
        }

        // Check a rectangle with its center at a particular angle.
        void TestRectangleCenter(double angle, PointF expectedResult)
        {
            PointF circleCenter = new PointF(-0.5F, 1F);
            float circleRadius = 2.5F;
            SizeF rectSize = new SizeF(6, 2.4F);
            PointF rectCenter;

            rectCenter = CourseFormatter.GetRectangleCenter(circleCenter, circleRadius, angle, rectSize);

            Assert.AreEqual(expectedResult.X, rectCenter.X, 0.001);
            Assert.AreEqual(expectedResult.Y, rectCenter.Y, 0.001);

            double angleTo = Math.Atan2(rectCenter.Y - circleCenter.Y, rectCenter.X - circleCenter.X);
            Assert.AreEqual(Math.IEEERemainder(angle, Math.PI * 2), angleTo, 0.01);

            // If this seems to be failing, the following code draws the circle and rectangle so you can see whats going on.
            //
            //Bitmap bm = new Bitmap(250, 250);
            //using (Graphics g = Graphics.FromImage(bm)) {
            //    g.TranslateTransform(bm.Width / 2, bm.Height / 2);
            //    g.ScaleTransform((float) (bm.Width / 15.0), - (float) (bm.Height / 15.0));
            //
            //    g.Clear(Color.White);
            //    g.DrawEllipse(new Pen(Color.Blue, 0.05F), circleCenter.X - circleRadius, circleCenter.Y - circleRadius, circleRadius * 2, circleRadius * 2);
            //    g.DrawRectangle(new Pen(Color.Red, 0.05F), rectCenter.X - rectSize.Width / 2, rectCenter.Y - rectSize.Height / 2, rectSize.Width, rectSize.Height);
            //}
            //
            //TestUtil.CheckBaseline(bm, "courseformat\\rectCenter_" + string.Format("{0:0.00}", angle));
        }

        [TestMethod]
        public void GetRectangleCenter()
        {
            TestRectangleCenter(0, new PointF(5F, 1F));
            TestRectangleCenter(Math.PI / 2, new PointF(-0.5F, 4.7F));
            TestRectangleCenter(Math.PI, new PointF(-6F, 1F));
            TestRectangleCenter(- Math.PI / 2, new PointF(-0.5F, -2.7F));
            TestRectangleCenter(0.1, new PointF(5F, 1.5518F));
            TestRectangleCenter(-0.1, new PointF(5F, 0.4482F));
            TestRectangleCenter(1, new PointF(1.8757F, 4.7F));
            TestRectangleCenter(0.77, new PointF(3.2097F, 4.5972F));
            TestRectangleCenter(0.6, new PointF(4.0827F, 4.1352F));
            TestRectangleCenter(0.45, new PointF(4.6444F, 3.4851F));
            TestRectangleCenter(0.2, new PointF(5F, 2.1149F));
            TestRectangleCenter(-1, new PointF(1.8757F, -2.7F));
            TestRectangleCenter(-0.77, new PointF(3.2097F, -2.5972F));
            TestRectangleCenter(-0.6, new PointF(4.0827F, -2.1352F));
            TestRectangleCenter(-0.45, new PointF(4.6444F, -1.4851F));
            TestRectangleCenter(-0.2, new PointF(5F, -0.1149F));
            TestRectangleCenter(1.5, new PointF(-0.2376F, 4.7F));
            TestRectangleCenter(1.6, new PointF(-0.6081F, 4.7F));
            TestRectangleCenter(-1.5, new PointF(-0.2376F, -2.7F));
            TestRectangleCenter(-1.6, new PointF(-0.6081F, -2.7F));
            TestRectangleCenter(2, new PointF(-2.1933F, 4.7F));
            TestRectangleCenter(2.4, new PointF(-4.369F, 4.5441F));
            TestRectangleCenter(-2.5, new PointF(-4.889F, -2.2787F));
            TestRectangleCenter(-1.8, new PointF(-1.3632F, -2.7F));
            TestRectangleCenter(3.05, new PointF(-6F, 1.5052F));
            TestRectangleCenter(3.2, new PointF(-6F, 0.6784F));
            TestRectangleCenter(-2.95, new PointF(-6F, -0.0668F));
        }

        [TestMethod]
        public void ExpandText()
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            CourseView courseView;

            eventDB.Load(TestUtil.GetTestFile("courseformat\\marymoor6.coursescribe"));
            eventDB.Validate();

            // Use course 1
            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(1)));

            string result;

            result = CourseFormatter.ExpandText(eventDB, courseView, "Simple text");
            Assert.AreEqual("Simple text", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$10 bill < $20 bill");
            Assert.AreEqual("$10 bill < $20 bill", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "");
            Assert.AreEqual("", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(CourseName)");
            Assert.AreEqual("Course 1", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(CoursePart)");
            Assert.AreEqual("", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(EventTitle): $(CourseName)");
            Assert.AreEqual("Marymoor WIOL 2 The remake: Course 1", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "Course: $(CourseName) Length: $(CourseLength) km Scale: $(PrintScale)");
            Assert.AreEqual("Course: Course 1 Length: 1.5 km Scale: 1:7,500", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "Course: $(CourseName) Climb: $(CourseClimb) m");
            Assert.AreEqual("Course: Course 1 Climb: 20 m", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(CourseName) / $(ClassList)");
            Assert.AreEqual("Course 1 / This is cool very cool", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(Variation)");
            Assert.AreEqual("", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(RelayTeam)");
            Assert.AreEqual("--", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(RelayLeg)");
            Assert.AreEqual("-", result);

            // All Controls
            courseView = CourseView.CreateViewingCourseView(eventDB, CourseDesignator.AllControls);

            result = CourseFormatter.ExpandText(eventDB, courseView, "Course: $(CourseName) Length: $(CourseLength) km Scale: $(PrintScale)");
            Assert.AreEqual("Course: All controls Length: 0.0 km Scale: 1:10,000", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(CourseName) / $(ClassList)");
            Assert.AreEqual("All controls / ", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(CoursePart)");
            Assert.AreEqual("", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(Variation)");
            Assert.AreEqual("", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(RelayTeam)");
            Assert.AreEqual("--", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(RelayLeg)");
            Assert.AreEqual("-", result);
        }

        [TestMethod]
        public void ExpandTextRelay()
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            CourseView courseView;
            string result;

            eventDB.Load(TestUtil.GetTestFile("queryevent\\variations.ppen"));
            eventDB.Validate();

            // Use course 1
            List<VariationInfo> variations = QueryEvent.GetAllVariations(eventDB, CourseId(1)).ToList();

            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(1), variations[0]));
            result = CourseFormatter.ExpandText(eventDB, courseView, "$(CourseName)");
            Assert.AreEqual("Course 1", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(Variation)");
            Assert.AreEqual("ACDEFH", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(RelayTeam)");
            Assert.AreEqual("--", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(RelayLeg)");
            Assert.AreEqual("-", result);

            CourseDesignator designator = QueryEvent.EnumerateCourseDesignators(eventDB, 
                new[] { CourseId(1) },
                new Dictionary<Id<Course>, VariationChoices>() { { CourseId(1), new VariationChoices() { Kind = VariationChoices.VariationChoicesKind.ChosenTeams, FirstTeam = 4, LastTeam = 6 } } }, 
                true).Skip(1).First();
            courseView = CourseView.CreateViewingCourseView(eventDB, designator);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(CourseName)");
            Assert.AreEqual("Course 1", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(Variation)");
            Assert.AreEqual("ADEFCH", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(RelayTeam)");
            Assert.AreEqual("4", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(RelayLeg)");
            Assert.AreEqual("2", result);

        }


        [TestMethod]
        public void ExpandTextMapExchange()
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            CourseView courseView;

            // Map Exchange
            eventDB.Load(TestUtil.GetTestFile("courseformat\\mapexchange1.ppen"));
            eventDB.Validate();

            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(6)));

            string result;

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(CourseName)");
            Assert.AreEqual("Course 5", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(CoursePart)");
            Assert.AreEqual("", result);

            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(6), 0));
            result = CourseFormatter.ExpandText(eventDB, courseView, "$(CourseName)");
            Assert.AreEqual("Course 5", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(CoursePart)");
            Assert.AreEqual("1", result);

            courseView = CourseView.CreateViewingCourseView(eventDB, new CourseDesignator(CourseId(6), 3));
            result = CourseFormatter.ExpandText(eventDB, courseView, "$(CourseName)");
            Assert.AreEqual("Course 5", result);

            result = CourseFormatter.ExpandText(eventDB, courseView, "$(CoursePart)");
            Assert.AreEqual("4", result);
        }

    }
}

#endif //TEST
