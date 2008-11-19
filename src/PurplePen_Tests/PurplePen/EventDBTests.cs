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
using System.Xml;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;


namespace PurplePen.Tests
{
    [TestClass]
    public class EventDBTests: TestFixtureBase
    {
        [TestMethod]
        public void RoundTripControlPoints()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);

            ControlPoint ctl1, ctl2, ctl3, ctl4, ctl5, ctl6, ctl7;

            undomgr.BeginCommand(61, "Command1");

            ctl1 = new ControlPoint(ControlPointKind.Start, null, new PointF(5, 0));
            ctl1.symbolIds[1] = "2.8";
            ctl1.symbolIds[2] = "8.5";
            eventDB.AddControlPoint(ctl1);

            ctl2 = new ControlPoint(ControlPointKind.Normal, "31", new PointF(10, 10));
            ctl2.symbolIds[0] = "0.3";
            ctl2.symbolIds[1] = "2.4";
            ctl2.columnFText = "2m";
            ctl2.customCodeLocation = true;
            ctl2.codeLocationAngle = 97F;
            eventDB.AddControlPoint(ctl2);

            ctl3 = new ControlPoint(ControlPointKind.CrossingPoint, null, new PointF(13, -7.8F));
            ctl3.symbolIds[0] = "13.2";
            ctl3.orientation = 94.5F;
            eventDB.AddControlPoint(ctl3);

            ctl4 = new ControlPoint(ControlPointKind.Normal, "32", new PointF(20, -10.5F));
            ctl4.symbolIds[1] = "3.7";
            ctl4.symbolIds[5] = "12.1";
            ctl4.gaps = new Dictionary<int,uint>();
            ctl4.gaps.Add(15000, 0xFFFFFFDF);
            ctl4.gaps.Add(10000, 0xFF00FFFF);
            ctl4.descriptionText = "very marshy spot";
            ctl4.punches = new PunchPattern();
            ctl4.punches.size = 9;
            ctl4.punches.dots = new bool[ctl4.punches.size, ctl4.punches.size];
            ctl4.punches.dots[0, 0] = true;
            ctl4.punches.dots[4, 4] = true;
            ctl4.punches.dots[8, 8] = true;
            ctl4.punches.dots[8, 0] = true;
            eventDB.AddControlPoint(ctl4);

            ctl5 = new ControlPoint(ControlPointKind.Normal, "GO", new PointF(35.4F, -22.5F));
            ctl5.symbolIds[0] = "0.1N";
            ctl5.symbolIds[1] = "5.5";
            ctl5.symbolIds[2] = "5.2";
            ctl5.symbolIds[3] = "10.1";
            ctl5.symbolIds[4] = "11.1N";
            ctl5.symbolIds[5] = "12.3";
            eventDB.AddControlPoint(ctl5);

            ctl6 = new ControlPoint(ControlPointKind.Finish, null, new PointF(30.3F, -27.11F));
            ctl6.symbolIds[0] = "14.2";
            eventDB.AddControlPoint(ctl6);

            ctl7 = new ControlPoint(ControlPointKind.Normal, "QX", new PointF(43, 7.1F));
            ctl7.symbolIds[1] = "3.6";
            ctl7.descTextBefore = "hi there";
            ctl7.descTextAfter = "bye there";
            eventDB.AddControlPoint(ctl7);

            undomgr.EndCommand(61);

            eventDB.Save(TestUtil.GetTestFile("eventdb\\testoutput_temp.xml"));

            undomgr.Clear();
            eventDB = new EventDB(undomgr);

            eventDB.Load(TestUtil.GetTestFile("eventdb\\testoutput_temp.xml"));

            TestUtil.TestEnumerableAnyOrder(eventDB.AllControlPointPairs,
                new KeyValuePair<Id<ControlPoint>, ControlPoint>[] {
                    new KeyValuePair<Id<ControlPoint>,ControlPoint>(ControlId(1), ctl1),
                    new KeyValuePair<Id<ControlPoint>,ControlPoint>(ControlId(2), ctl2),
                    new KeyValuePair<Id<ControlPoint>,ControlPoint>(ControlId(3), ctl3),
                    new KeyValuePair<Id<ControlPoint>,ControlPoint>(ControlId(4), ctl4),
                    new KeyValuePair<Id<ControlPoint>,ControlPoint>(ControlId(5), ctl5),
                    new KeyValuePair<Id<ControlPoint>,ControlPoint>(ControlId(6), ctl6),
                    new KeyValuePair<Id<ControlPoint>,ControlPoint>(ControlId(7), ctl7)
                }
            );
        }

        [TestMethod]
        public void RoundTripCourses()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);

            Course course1, course2, course3;

            undomgr.BeginCommand(61, "Command1");

            course1 = new Course(CourseKind.Normal, "White", 15000, 1);
            course1.descKind = DescriptionKind.Symbols;
            course1.secondaryTitle = "White is right";
            course1.load = 0;
            course1.firstCourseControl = CourseControlId(1);
            eventDB.AddCourse(course1);

            course2 = new Course(CourseKind.Normal, "Yellow", 15000, 2);
            course2.descKind = DescriptionKind.SymbolsAndText;
            course2.climb = 95;
            course2.firstCourseControl = CourseControlId(0);
            course2.printArea = new RectangleF(50, 70, 200, 100);
            eventDB.AddCourse(course2);

            course3 = new Course(CourseKind.Score, "Rambo", 10000, 3);
            course3.secondaryTitle = "";
            course3.firstCourseControl = CourseControlId(2);
            course3.load = 125;
            course3.climb = 0;
            course3.descKind = DescriptionKind.Text;
            eventDB.AddCourse(course3);

            undomgr.EndCommand(61);

            eventDB.Save(TestUtil.GetTestFile("eventdb\\testoutput_temp.xml"));

            undomgr.Clear();
            eventDB = new EventDB(undomgr);

            eventDB.Load(TestUtil.GetTestFile("eventdb\\testoutput_temp.xml"));

            TestUtil.TestEnumerableAnyOrder(eventDB.AllCoursePairs,
                new KeyValuePair<Id<Course>, Course>[] {
                    new KeyValuePair<Id<Course>,Course>(CourseId(1), course1),
                    new KeyValuePair<Id<Course>,Course>(CourseId(2), course2),
                    new KeyValuePair<Id<Course>,Course>(CourseId(3), course3),
                }
            );
        }

        [TestMethod]
        public void RoundTripCourseControls()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);

            CourseControl ctl1, ctl2, ctl3, ctl4, ctl5, ctl6, ctl7;

            undomgr.BeginCommand(61, "Command1");

            ctl1 = new CourseControl(ControlId(1), CourseControlId(2));
            eventDB.AddCourseControl(ctl1);

            ctl2 = new CourseControl(ControlId(2), Id<CourseControl>.None);
            ctl2.split = true;
            ctl2.nextSplitCourseControls = new Id<CourseControl>[2] { CourseControlId(3), CourseControlId(4) };
            eventDB.AddCourseControl(ctl2);

            ctl3 = new CourseControl(ControlId(5), CourseControlId(5));
            ctl3.points = 10;
            eventDB.AddCourseControl(ctl3);

            ctl4 = new CourseControl(ControlId(6), CourseControlId(5));
            ctl4.points = 20;
            ctl4.customNumberPlacement = true;
            ctl4.numberDeltaX = -6.3F;
            ctl4.numberDeltaY = 7.41F;
            eventDB.AddCourseControl(ctl4);

            ctl5 = new CourseControl(ControlId(7), CourseControlId(6));
            ctl5.join = true;
            eventDB.AddCourseControl(ctl5);

            ctl6 = new CourseControl(ControlId(8), Id<CourseControl>.None);
            eventDB.AddCourseControl(ctl6);

            ctl7 = new CourseControl(ControlId(6), CourseControlId(5));
            ctl7.descTextBefore = "hello";
            ctl7.descTextAfter = "goodbye";
            eventDB.AddCourseControl(ctl7);

            undomgr.EndCommand(61);

            eventDB.Save(TestUtil.GetTestFile("eventdb\\testoutput_temp.xml"));

            undomgr.Clear();
            eventDB = new EventDB(undomgr);

            eventDB.Load(TestUtil.GetTestFile("eventdb\\testoutput_temp.xml"));

            CollectionAssert.AreEquivalent(new List<KeyValuePair<Id<CourseControl>, CourseControl>>(eventDB.AllCourseControlPairs),
                new KeyValuePair<Id<CourseControl>, CourseControl>[] {
                    new KeyValuePair<Id<CourseControl>,CourseControl>(CourseControlId(1), ctl1),
                    new KeyValuePair<Id<CourseControl>,CourseControl>(CourseControlId(2), ctl2),
                    new KeyValuePair<Id<CourseControl>,CourseControl>(CourseControlId(3), ctl3),
                    new KeyValuePair<Id<CourseControl>,CourseControl>(CourseControlId(4), ctl4),
                    new KeyValuePair<Id<CourseControl>,CourseControl>(CourseControlId(5), ctl5),
                    new KeyValuePair<Id<CourseControl>,CourseControl>(CourseControlId(6), ctl6),
                    new KeyValuePair<Id<CourseControl>,CourseControl>(CourseControlId(7), ctl7),
                }
            );
        }

        [TestMethod]
        public void RoundTripLeg()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);

            Leg leg1, leg2, leg3, leg4, leg5;
            ControlPoint ctl1, ctl2;
            Id<ControlPoint> ctlId1, ctlId2;

            undomgr.BeginCommand(81, "Command1");

            ctl1 = new ControlPoint(ControlPointKind.Normal, "31", new PointF(10, 10));
            ctl1.symbolIds[0] = "0.3";
            ctl1.symbolIds[1] = "2.4";
            ctl1.columnFText = "2m";
            ctlId1 = eventDB.AddControlPoint(ctl1);

            ctl2 = new ControlPoint(ControlPointKind.Normal, "32", new PointF(20, -10.5F));
            ctl2.symbolIds[1] = "3.7";
            ctl2.symbolIds[5] = "12.1";
            ctl2.gaps = new Dictionary<int, uint>();
            ctl2.gaps.Add(15000, 0xFFFFFFDF);
            ctl2.gaps.Add(10000, 0xFF00FFFF);
            ctl2.descriptionText = "very marshy spot";
            ctlId2 = eventDB.AddControlPoint(ctl2);

            leg1 = new Leg(ctlId1, ctlId2);
            eventDB.AddLeg(leg1);

            leg2 = new Leg(ctlId1, ctlId2);
            leg2.flagging = FlaggingKind.All;
            eventDB.AddLeg(leg2);

            leg3 = new Leg(ctlId1, ctlId2);
            leg3.flagging = FlaggingKind.Begin;
            leg3.flagStartStop = new PointF(5, -7.5F);
            leg3.bends = new PointF[2] { leg3.flagStartStop, new PointF(17, 6) };
            eventDB.AddLeg(leg3);

            leg4 = new Leg(ctlId1, ctlId2);
            leg4.flagging = FlaggingKind.End;
            leg4.flagStartStop = new PointF(5, -7.5F);
            leg4.bends = new PointF[1] { leg4.flagStartStop };
            leg4.gaps = new LegGap[2] { new LegGap(2, 0.3F), new LegGap(3.4F, 0.4F)};
            eventDB.AddLeg(leg4);

            leg5 = new Leg(ctlId1, ctlId2);
            leg5.gaps = new LegGap[3] { new LegGap(0.9F, 0.3F), new LegGap(3.4F, 0.4F), new LegGap(4.5F, 1.1F) };
            eventDB.AddLeg(leg5);

            undomgr.EndCommand(81);

            eventDB.Validate();

            eventDB.Save(TestUtil.GetTestFile("eventdb\\testoutput_temp.xml"));

            undomgr.Clear();
            eventDB = new EventDB(undomgr);

            eventDB.Load(TestUtil.GetTestFile("eventdb\\testoutput_temp.xml"));
            eventDB.Validate();

            TestUtil.TestEnumerableAnyOrder(eventDB.AllLegPairs,
                new KeyValuePair<Id<Leg>, Leg>[] {
                    new KeyValuePair<Id<Leg>,Leg>(LegId(1), leg1),
                    new KeyValuePair<Id<Leg>,Leg>(LegId(2), leg2),
                    new KeyValuePair<Id<Leg>,Leg>(LegId(3), leg3),
                    new KeyValuePair<Id<Leg>,Leg>(LegId(4), leg4),
                    new KeyValuePair<Id<Leg>,Leg>(LegId(5), leg5)
                }
            );
        }
	

        [TestMethod]
        public void RoundTripEvent()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);

            Event e = new Event();

            e.title = "Hello";
            e.notes = "These are my notes";
            e.mapType = MapType.OCAD;
            e.mapFileName = "C:\\hello.ocad";
            e.mapScale = 14000;
            e.allControlsPrintScale = 10000;
            e.allControlsDescKind = DescriptionKind.SymbolsAndText;
            e.printArea = new RectangleF(50, 70, 200, 300);
            e.punchcardFormat.boxesAcross = 7;
            e.punchcardFormat.boxesDown = 5;
            e.punchcardFormat.leftToRight = false;
            e.punchcardFormat.topToBottom = true;

            undomgr.BeginCommand(198, "change event");
            eventDB.ChangeEvent(e);
            undomgr.EndCommand(198);

            eventDB.Save(TestUtil.GetTestFile("eventdb\\testoutput1_temp.xml"));

            undomgr.Clear();
            eventDB = new EventDB(undomgr);

            eventDB.Load(TestUtil.GetTestFile("eventdb\\testoutput1_temp.xml"));

            Assert.AreEqual(e, eventDB.GetEvent());
            Assert.AreEqual(7, e.punchcardFormat.boxesAcross);

            /* --- */

            undomgr.Clear();
            eventDB = new EventDB(undomgr);

            e.title = "Hi";
            e.notes = null;
            e.mapType = MapType.None;
            e.mapFileName = null;

            undomgr.BeginCommand(198, "change event");
            eventDB.ChangeEvent(e);
            undomgr.EndCommand(198);

            eventDB.Save(TestUtil.GetTestFile("eventdb\\testoutput2_temp.xml"));

            undomgr.Clear();
            eventDB = new EventDB(undomgr);

            eventDB.Load(TestUtil.GetTestFile("eventdb\\testoutput2_temp.xml"));

            Assert.AreEqual(e, eventDB.GetEvent());

            /* --- */

            undomgr.Clear();
            eventDB = new EventDB(undomgr);

            e.title = "Hi";
            e.notes = null;
            e.mapType = MapType.Bitmap;
            e.mapFileName = TestUtil.GetTestFile("eventdb\\maps\\testoutput3.jpg");
            e.mapScale = 12000;
            e.mapDpi = 330;
            e.allControlsPrintScale = 7500;
            e.allControlsDescKind = DescriptionKind.Text;
            e.descriptionLangId = "bg";

            undomgr.BeginCommand(198, "change event");
            eventDB.ChangeEvent(e);
            undomgr.EndCommand(198);

            eventDB.Save(TestUtil.GetTestFile("eventdb\\testoutput3_temp.xml"));

            undomgr.Clear();
            eventDB = new EventDB(undomgr);

            eventDB.Load(TestUtil.GetTestFile("eventdb\\testoutput3_temp.xml"));

            Assert.AreEqual(e, eventDB.GetEvent());

            /* --- */

            undomgr.Clear();
            eventDB = new EventDB(undomgr);

            e.title = "Hi";
            e.notes = null;
            e.mapType = MapType.None;
            e.mapFileName = null;
            e.mapScale = 12000;
            e.mapDpi = 330;
            e.allControlsPrintScale = 7500;
            e.allControlsDescKind = DescriptionKind.Text;
            e.courseAppearance.lineWidth = 1.3F;
            e.courseAppearance.controlCircleSize = 0.9F;
            e.courseAppearance.numberHeight = 1.1F;
            e.courseAppearance.useDefaultPurple = false;
            e.courseAppearance.purpleC = 0.4F;
            e.courseAppearance.purpleM = 0.4F;
            e.courseAppearance.purpleY = 0.4F;
            e.courseAppearance.purpleK = 0.4F;

            undomgr.BeginCommand(198, "change event");
            eventDB.ChangeEvent(e);
            undomgr.EndCommand(198);

            eventDB.Save(TestUtil.GetTestFile("eventdb\\testoutput5_temp.xml"));

            undomgr.Clear();
            eventDB = new EventDB(undomgr);

            eventDB.Load(TestUtil.GetTestFile("eventdb\\testoutput5_temp.xml"));

            Assert.AreEqual(e, eventDB.GetEvent());

            /* --- */

            undomgr.Clear();
            eventDB = new EventDB(undomgr);

            e.title = "Mr. Event";
            e.notes = null;
            e.mapType = MapType.OCAD;
            e.mapFileName = TestUtil.GetTestFile("eventdb\\My Map-File.ocad");
            e.ignoreMissingFonts = true;

            List<SymbolText> texts = new List<SymbolText>();
            SymbolText text1 = new SymbolText(), text2 = new SymbolText(), text3 = new SymbolText();
            text1.Lang = "en"; text1.Gender = ""; text1.Plural = false; text1.Text = "man-made & cool object";
            text2.Lang = "en"; text2.Gender = ""; text2.Plural = true; text2.Text = "man-made & cool objects";
            text3.Lang = "bg"; text3.Gender = "masculine"; text3.Plural = false; text3.Text = "bulgarish mm";
            texts.Add(text1); texts.Add(text2); texts.Add(text3);
            e.customSymbolText["6.1"] = texts;

            texts = new List<SymbolText>();
            text1.Lang = "en"; text1.Gender = ""; text1.Plural = false; text1.Text = "boopsie";
            texts.Add(text1);
            e.customSymbolText["6.2"] = texts;
            e.customSymbolKey["6.2"] = true;
            e.customSymbolKey["6.1"] = false;

            undomgr.BeginCommand(198, "change event");
            eventDB.ChangeEvent(e);
            undomgr.EndCommand(198);

            eventDB.Save(TestUtil.GetTestFile("eventdb\\testoutput4_temp.xml"));

            undomgr.Clear();
            eventDB = new EventDB(undomgr);

            eventDB.Load(TestUtil.GetTestFile("eventdb\\testoutput4_temp.xml"));

            Assert.AreEqual(e, eventDB.GetEvent());
        }

        [TestMethod]
        public void RoundTripSpecials()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);

            Special sp1, sp2, sp3, sp4, sp5, sp6, sp7;

            undomgr.BeginCommand(88, "Command1");

            sp1 = new Special(SpecialKind.FirstAid, new PointF[1] { new PointF(4.5F, 1.2F) });
            sp2 = new Special(SpecialKind.OptCrossing, new PointF[1] { new PointF(-4.2F, 1.7F) });
            sp2.allCourses = false;
            sp2.courses = new Id<Course>[3] { CourseId(1), CourseId(2), CourseId(3) };
            sp2.orientation = 45F;
            sp3 = new Special(SpecialKind.Boundary, new PointF[2] { new PointF(8, 7), new PointF(1, 2) });
            sp4 = new Special(SpecialKind.OOB, new PointF[4] { new PointF(3, 7), new PointF(11, 2), new PointF(0, -1), new PointF(-12, -3) });
            sp5 = new Special(SpecialKind.Text, new PointF[2] { new PointF(3, 7), new PointF(11, 4) });
            sp5.text = "Hello";
            sp5.fontName = "Tahoma";
            sp5.fontBold = true;
            sp5.fontItalic = false;
            sp5.allCourses = false;
            sp5.courses = new Id<Course>[1] { CourseId(2) };
            sp6 = new Special(SpecialKind.Descriptions, new PointF[2] { new PointF(5, 6), new PointF(11, 6) });
            sp7 = new Special(SpecialKind.Text, new PointF[2] { new PointF(8, 7), new PointF(18, 5) });
            sp7.fontName = "Courier New";
            sp7.fontBold = false;
            sp7.fontItalic = true;
            sp7.text = "$(CourseName)";

            eventDB.AddSpecial(sp1);
            eventDB.AddSpecial(sp2);
            eventDB.AddSpecial(sp3);
            eventDB.AddSpecial(sp4);
            eventDB.AddSpecial(sp5);
            eventDB.AddSpecial(sp6);
            eventDB.AddSpecial(sp7);

            undomgr.EndCommand(88);

            eventDB.Save(TestUtil.GetTestFile("eventDB\\testoutput_temp.xml"));

            undomgr.Clear();
            eventDB = new EventDB(undomgr);

            eventDB.Load(TestUtil.GetTestFile("eventdb\\testoutput_temp.xml"));

            TestUtil.TestEnumerableAnyOrder(eventDB.AllSpecialPairs,
                new KeyValuePair<Id<Special>, Special>[] {
                    new KeyValuePair<Id<Special>,Special>(SpecialId(1), sp1),
                    new KeyValuePair<Id<Special>,Special>(SpecialId(2), sp2),
                    new KeyValuePair<Id<Special>,Special>(SpecialId(3), sp3),
                    new KeyValuePair<Id<Special>,Special>(SpecialId(4), sp4),
                    new KeyValuePair<Id<Special>,Special>(SpecialId(5), sp5),
                    new KeyValuePair<Id<Special>,Special>(SpecialId(6), sp6),
                    new KeyValuePair<Id<Special>,Special>(SpecialId(7), sp7),
                }
            );
        }
	

        [TestMethod]
        public void ValidateSampleEvent1()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);

            eventDB.Load(TestUtil.GetTestFile("eventdb\\sampleevent1.coursescribe"));
            eventDB.Validate();
        }

        [TestMethod]
        public void Event()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);

            Event e = eventDB.GetEvent();
            Assert.AreEqual(e.title, "");
            Assert.AreEqual(e.notes, null);
            Assert.AreEqual(e.mapType, MapType.None);

            Event e2 = new Event();
            e2.title = "Hello";
            e2.notes = "These are my notes";
            e2.mapType = MapType.OCAD;
            e2.mapFileName = "C:\\hello.ocad";

            undomgr.BeginCommand(198, "change event");
            eventDB.ChangeEvent(e2);
            undomgr.EndCommand(198);

            e = eventDB.GetEvent();
            Assert.AreEqual(e2, e);
        }

        [TestMethod]
        public void HasChanged()
        {
            UndoMgr undomgr = new UndoMgr(5);
            EventDB eventDB = new EventDB(undomgr);
            long changeNum = 0;

            Assert.IsTrue(changeNum < eventDB.ChangeNum);
            changeNum = eventDB.ChangeNum;

            undomgr.BeginCommand(61, "Command1");

            ControlPoint ctl1 = new ControlPoint(ControlPointKind.Start, null, new PointF(5, 0));
            ctl1.symbolIds[1] = "2.8";
            ctl1.symbolIds[2] = "8.5";
            eventDB.AddControlPoint(ctl1);

            undomgr.EndCommand(61);

            Assert.IsTrue(changeNum < eventDB.ChangeNum);
            changeNum = eventDB.ChangeNum;

            eventDB = new EventDB(undomgr);

            Assert.IsTrue(changeNum != eventDB.ChangeNum);
            changeNum = eventDB.ChangeNum;

            eventDB = new EventDB(undomgr);

            Assert.IsTrue(changeNum != eventDB.ChangeNum);
            changeNum = eventDB.ChangeNum;


        }
    }
}

#endif //TEST
