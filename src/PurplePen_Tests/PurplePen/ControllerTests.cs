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
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.Tests
{
    [TestClass]
    public class ControllerTests: TestFixtureBase
    {
        TestUI ui;
        Controller controller;

        void MakeDirty()
        {
            UndoMgr undoMgr = controller.GetUndoMgr();
            EventDB eventDB = controller.GetEventDB();

            undoMgr.BeginCommand(197, "Add Control Point");
            eventDB.AddControlPoint(new ControlPoint(ControlPointKind.Normal, "998", new PointF(12, 32)));
            undoMgr.EndCommand(197);
        }

        [TestInitialize]
        public void Setup()
        {
            ui = TestUI.Create();
            controller = ui.controller;
        }

        [TestMethod]
        public void LoadInitialFile()
        {
            string fileName = TestUtil.GetTestFile("controller\\sampleevent1.coursescribe");

            bool success = controller.LoadInitialFile(fileName, true);
            Assert.IsTrue(success);

            EventDB eventDB = controller.GetEventDB();

            Assert.AreEqual("White", eventDB.GetCourse(CourseId(1)).name);
            Assert.AreEqual("Yellow", eventDB.GetCourse(CourseId(2)).name);
            Assert.AreEqual("Rambo", eventDB.GetCourse(CourseId(3)).name);
        }

        [TestMethod]
        public void LoadMissingMapFile()
        {
            string fileName = TestUtil.GetTestFile("controller\\missingmap2.ppen");

            ui.expectedMissingMapFile = @"c:\users\Peter\Documents\Orienteering\Goofy.ocd";
            ui.newMapFile = TestUtil.GetTestFile("controller\\Lake Sammamish.ocd");
            ui.newMapScale = 15000;
            ui.newMapType = MapType.OCAD;
            ui.newMapDpi = 0;

            bool success = controller.LoadInitialFile(fileName, true);
            Assert.IsTrue(success);

            string expected = "ERROR: '" + String.Format(MiscText.MissingMapFile, Path.GetFileName(ui.expectedMissingMapFile)) + "'\r\n";
            Assert.AreEqual(expected, ui.output.ToString());

            // The new map file should be set.
            Assert.AreEqual(ui.newMapFile, controller.MapFileName);
            Assert.IsTrue(controller.GetUndoMgr().CanUndo);            // Change of map file is undoable.
            Assert.IsTrue(controller.GetUndoMgr().IsDirty);      // And the event is now dirty, since the map file changes.
        }

        // Load an event with map file not in given location, but in current directory.
        [TestMethod]
        public void LoadWrongDirectoryMap()
        {
            string fileName = TestUtil.GetTestFile("controller\\missingmap.ppen");

            bool success = controller.LoadInitialFile(fileName, true);
            Assert.IsTrue(success);

            EventDB eventDB = controller.GetEventDB();

            Assert.AreEqual(Path.GetDirectoryName(fileName), Path.GetDirectoryName(controller.MapFileName));
            Assert.IsTrue(controller.GetUndoMgr().CanUndo);            // Change of map file is undoable.
            Assert.IsTrue(controller.GetUndoMgr().IsDirty);      // And the event is now dirty, since the map file changes.
            Assert.AreEqual("", ui.output.ToString());        // No error messages.
        }


        [TestMethod]
        public void LoadBogusFile()
        {
            string fileName = TestUtil.GetTestFile("XBogus.coursescribe");

            bool success = controller.LoadInitialFile(fileName, true);
            Assert.IsFalse(success);
            Console.WriteLine(ui.output.ToString());

            string expected =
@"ERROR: 'Cannot load '" + fileName + @"' for the following reason:

File format error in file '" + fileName + @"'
at line 45, column 3:
Invalid control point kind 'norfmal''
";

            Assert.AreEqual(expected, ui.output.ToString());
        }

        [TestMethod]
        public void InitialNewEvent()
        {
            Controller.CreateEventInfo info;
            info.title = "My New Event";
            info.eventFileName = TestUtil.GetTestFile("initial\\newevent1.coursescribe");
            info.mapFileName = TestUtil.GetTestFile("initial\\marymoor.ocd");
            info.mapType = MapType.OCAD;
            info.dpi = 0;
            info.scale = 10000;
            info.allControlsPrintScale = 7500;
            info.firstCode = 100;
            info.disallowInvertibleCodes = true;
            info.descriptionLangId = "de";

            bool success = controller.InitialNewEvent(info);
            Assert.IsTrue(success);

            EventDB eventDB = controller.GetEventDB();
            eventDB.Validate();

            Event e = eventDB.GetEvent();
            Assert.AreEqual(info.mapFileName, e.mapFileName);
            Assert.AreEqual(info.mapFileName, controller.MapFileName);
            Assert.AreEqual(MapType.OCAD, e.mapType);
            Assert.AreEqual(10000, e.mapScale);
            Assert.AreEqual(7500, e.allControlsPrintScale);
            Assert.AreEqual(DescriptionKind.Symbols, e.allControlsDescKind);
            Assert.AreEqual(100, e.firstControlCode);
            Assert.AreEqual(true, e.disallowInvertibleCodes);
            Assert.AreEqual("My New Event", e.title);
            Assert.AreEqual(info.eventFileName, controller.FileName);
            Assert.AreEqual("de", e.descriptionLangId);
            Assert.IsFalse(controller.IsDirty);

            // Make sure we can add a new control and a new course.
            UndoMgr undomgr = controller.GetUndoMgr();
            undomgr.BeginCommand(771, "hi");
            ChangeEvent.AddControlPoint(eventDB, ControlPointKind.Start, null, new PointF(10, 10), 0);
            ChangeEvent.AddControlPoint(eventDB, ControlPointKind.Normal, "31", new PointF(20, 7), 0);
            ChangeEvent.CreateCourse(eventDB, CourseKind.Normal, "Course 1", ControlLabelKind.Sequence, -1, null, 10000, -1, DescriptionKind.Symbols, 1, true);
            undomgr.EndCommand(771);
            eventDB.Validate();
        }


        // Test closing the current file and creating a new event.
        [TestMethod]
        public void NewEvent()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);
            controller.SaveAs(TestUtil.GetTestFile("file_temp.coursescribe"));
            File.Delete(TestUtil.GetTestFile("file_temp.coursescribe"));
            controller.SelectTab(1);
            MakeDirty();

            ui.returnQuestion = DialogResult.No;
            Controller.CreateEventInfo info;
            info.title = "My New Event";
            info.eventFileName = TestUtil.GetTestFile("initial\\newevent1.coursescribe");
            info.mapFileName = TestUtil.GetTestFile("initial\\marymoor.ocd");
            info.mapType = MapType.OCAD;
            info.dpi = 0;
            info.scale = 10000;
            info.allControlsPrintScale = 7500;
            info.firstCode = 55;
            info.disallowInvertibleCodes = false;
            info.descriptionLangId = "en";

            success = controller.TryCloseFile();
            Assert.IsTrue(success);
            success = controller.NewEvent(info);
            Assert.IsTrue(success);
            Assert.IsFalse(File.Exists(TestUtil.GetTestFile("file_temp.coursescribe")));  // make sure it was NOT saved.
            Assert.AreEqual(
@"YES/NO/CANCEL QUESTION: 'Do you want to save the changes you made to 'file_temp.coursescribe'?' (default yes)
  (returned no)
",
            ui.output.ToString());

            EventDB eventDB = controller.GetEventDB();
            eventDB.Validate();

            Event e = eventDB.GetEvent();
            Assert.AreEqual(info.mapFileName, e.mapFileName);
            Assert.AreEqual(info.mapFileName, controller.MapFileName);
            Assert.AreEqual(MapType.OCAD, e.mapType);
            Assert.AreEqual(10000, e.mapScale);
            Assert.AreEqual(7500, e.allControlsPrintScale);
            Assert.AreEqual(DescriptionKind.Symbols, e.allControlsDescKind);
            Assert.AreEqual("My New Event", e.title);
            Assert.AreEqual(55, e.firstControlCode);
            Assert.AreEqual(false, e.disallowInvertibleCodes);
            Assert.AreEqual(info.eventFileName, controller.FileName);
            Assert.IsFalse(controller.IsDirty);

            // Make sure we can add a new control and a new course.
            UndoMgr undomgr = controller.GetUndoMgr();
            undomgr.BeginCommand(771, "hi");
            ChangeEvent.AddControlPoint(eventDB, ControlPointKind.Start, null, new PointF(10, 10), 0);
            ChangeEvent.AddControlPoint(eventDB, ControlPointKind.Normal, "31", new PointF(20, 7), 0);
            ChangeEvent.CreateCourse(eventDB, CourseKind.Normal, "Course 1", ControlLabelKind.Sequence, -1, null, 10000, -1, DescriptionKind.Symbols, 1, true);
            undomgr.EndCommand(771);
            eventDB.Validate();
        }


        [TestMethod]
        public void InitialNewEventError()
        {
            Controller.CreateEventInfo info;
            info.title = "My New Event";
            info.eventFileName = TestUtil.GetTestFile("begrlothit\\zappy\\foo.coursescribe");
            info.mapFileName = TestUtil.GetTestFile("initial\\marymoor.ocd");
            info.mapType = MapType.OCAD;
            info.dpi = 0;
            info.scale = 10000;
            info.allControlsPrintScale = 7500;
            info.firstCode = 55;
            info.disallowInvertibleCodes = false;
            info.descriptionLangId = "en";

            bool success = controller.InitialNewEvent(info);
            Assert.IsFalse(success);

            string expected =
@"ERROR: 'Cannot save '" + info.eventFileName + @"' for the following reason:

Could not find a part of the path '" + info.eventFileName + "'.'\r\n";
            Assert.AreEqual(expected, ui.output.ToString());
        }
	

        [TestMethod]
        public void IsDirty()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            Assert.IsFalse(controller.IsDirty);
            MakeDirty();
            Assert.IsTrue(controller.IsDirty);
            controller.SaveAs(TestUtil.GetTestFile("file_temp.coursescribe"));
            Assert.IsFalse(controller.IsDirty);
        }

        [TestMethod]
        public void SaveAs()
        {
            EventDB eventDB;

            // Delete the new files so we make sure they are being saved.
            string newFile1 = TestUtil.GetTestFile("file1_temp.coursescribe");
            string newFile2 = TestUtil.GetTestFile("file2_temp.coursescribe");
            File.Delete(newFile1);
            File.Delete(newFile2);

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SaveAs(newFile1);
            Assert.IsFalse(controller.IsDirty);

            // Load the file we saved, make sure it is ok.
            Setup();
            success = controller.LoadInitialFile(newFile1, true);
            Assert.IsTrue(success);

            eventDB = controller.GetEventDB();
            Assert.AreEqual("White", eventDB.GetCourse(CourseId(1)).name);
            Assert.AreEqual("Yellow", eventDB.GetCourse(CourseId(2)).name);
            Assert.AreEqual("Rambo", eventDB.GetCourse(CourseId(3)).name);

            // Add something, and save to yet another file.
            MakeDirty();

            controller.SaveAs(newFile2);
            Assert.IsFalse(controller.IsDirty);

            // Load the new files we saved, make sure it has the change.
            Setup();
            success = controller.LoadInitialFile(newFile2, true);
            Assert.IsTrue(success);

            eventDB = controller.GetEventDB();
            Assert.AreEqual("White", eventDB.GetCourse(CourseId(1)).name);
            Assert.AreEqual("Yellow", eventDB.GetCourse(CourseId(2)).name);
            Assert.AreEqual("Rambo", eventDB.GetCourse(CourseId(3)).name);
            Assert.AreEqual("998", eventDB.GetControl(ControlId(25)).code);
        }

        [TestMethod]
        public void TryCloseFileClean()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            success = controller.TryCloseFile();
            Assert.IsTrue(success);
            Assert.AreEqual("", ui.output.ToString());  // no messages to the user.
        }

        [TestMethod]
        public void TryCloseFileYes()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);
            controller.SaveAs(TestUtil.GetTestFile("file_temp.coursescribe"));
            File.Delete(TestUtil.GetTestFile("file_temp.coursescribe"));
            MakeDirty();

            ui.returnQuestion = DialogResult.Yes;
            success = controller.TryCloseFile();
            Assert.IsTrue(success);
            Assert.IsTrue(File.Exists(TestUtil.GetTestFile("file_temp.coursescribe")));  // make sure it was saved.
            Assert.AreEqual(
@"YES/NO/CANCEL QUESTION: 'Do you want to save the changes you made to 'file_temp.coursescribe'?' (default yes)
  (returned yes)
",
            ui.output.ToString());
        }

        [TestMethod]
        public void TryCloseFileNo()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);
            controller.SaveAs(TestUtil.GetTestFile("file_temp.coursescribe"));
            File.Delete(TestUtil.GetTestFile("file_temp.coursescribe"));
            MakeDirty();

            ui.returnQuestion = DialogResult.No;
            success = controller.TryCloseFile();
            Assert.IsTrue(success);
            Assert.IsFalse(File.Exists(TestUtil.GetTestFile("file_temp.coursescribe")));  // make sure it was NOT saved.
            Assert.AreEqual(
@"YES/NO/CANCEL QUESTION: 'Do you want to save the changes you made to 'file_temp.coursescribe'?' (default yes)
  (returned no)
",
            ui.output.ToString());
        }


        [TestMethod]
        public void TryCloseFileCancel()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);
            controller.SaveAs(TestUtil.GetTestFile("file_temp.coursescribe"));
            File.Delete(TestUtil.GetTestFile("file_temp.coursescribe"));
            MakeDirty();

            ui.returnQuestion = DialogResult.Cancel;
            success = controller.TryCloseFile();
            Assert.IsFalse(success);
            Assert.IsFalse(File.Exists(TestUtil.GetTestFile("file_temp.coursescribe")));  // make sure it was NOT saved.
            Assert.AreEqual(
@"YES/NO/CANCEL QUESTION: 'Do you want to save the changes you made to 'file_temp.coursescribe'?' (default yes)
  (returned cancel)
",
            ui.output.ToString());
        }

        // Test closing the current file and opening a new one.
        [TestMethod]
        public void LoadNewFile()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);
            controller.SaveAs(TestUtil.GetTestFile("controller\\file_temp.coursescribe"));
            File.Delete(TestUtil.GetTestFile("controller\\file_temp.coursescribe"));
            controller.SelectTab(1);
            MakeDirty();

            ui.returnQuestion = DialogResult.No;
            success = controller.TryCloseFile();
            controller.LoadNewFile(TestUtil.GetTestFile("marymoor.ppen"));
            Assert.IsTrue(success);
            Assert.IsFalse(File.Exists(TestUtil.GetTestFile("controller\\file_temp.coursescribe")));  // make sure it was NOT saved.
            Assert.AreEqual(
@"YES/NO/CANCEL QUESTION: 'Do you want to save the changes you made to 'file_temp.coursescribe'?' (default yes)
  (returned no)
",
            ui.output.ToString());

            Assert.AreEqual("marymoor.ppen", Path.GetFileName(controller.FileName));
            controller.GetEventDB().Validate();
            Assert.IsFalse(controller.GetUndoStatus().CanUndo);
            Assert.IsFalse(controller.GetUndoStatus().CanRedo);
            Assert.IsFalse(controller.IsDirty);
            Assert.AreEqual(0, controller.ActiveTab);
        }
	

        [TestMethod]
        public void UndoRedo()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);
            Assert.IsFalse(controller.IsDirty);

            UndoStatus status = controller.GetUndoStatus();
            Assert.IsFalse(status.CanUndo);
            Assert.IsFalse(status.CanRedo);

            MakeDirty();

            status = controller.GetUndoStatus();
            Assert.IsTrue(status.CanUndo);
            Assert.IsFalse(status.CanRedo);
            Assert.AreEqual("Add Control Point", status.UndoName);

            controller.Undo();
            Assert.IsFalse(controller.IsDirty);

            status = controller.GetUndoStatus();
            Assert.IsFalse(status.CanUndo);
            Assert.IsTrue(status.CanRedo);
            Assert.AreEqual("Add Control Point", status.RedoName);

            controller.Redo();
            Assert.IsTrue(controller.IsDirty);

            status = controller.GetUndoStatus();
            Assert.IsTrue(status.CanUndo);
            Assert.IsFalse(status.CanRedo);
            Assert.AreEqual("Add Control Point", status.UndoName);
        }


        [TestMethod]
        public void DescriptionChange()
        {
            EventDB eventDB = controller.GetEventDB();
            SymbolDB symbolDB = ui.symbolDB;
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(3);
            controller.DescriptionChange(DescriptionControl.ChangeKind.Directive, 4, 0, symbolDB["13.4"]);
            controller.SelectTab(5);
            controller.DescriptionChange(DescriptionControl.ChangeKind.DescriptionBox, 3, 2, symbolDB["0.2NE"]);
            controller.SelectTab(0);
            controller.DescriptionChange(DescriptionControl.ChangeKind.DescriptionBox, 4, 5, null);
            controller.SelectTab(1);
            controller.DescriptionChange(DescriptionControl.ChangeKind.DescriptionBox, 8, 7, symbolDB["12.2"]);
            controller.DescriptionChange(DescriptionControl.ChangeKind.DescriptionBox, 8, 6, null);
            controller.DescriptionChange(DescriptionControl.ChangeKind.DescriptionBox, 8, 5, "2/4");

            ControlPoint control;

            control = eventDB.GetControl(ControlId(22));
            Assert.AreEqual("13.4", control.symbolIds[0]);
            control = eventDB.GetControl(ControlId(1));
            Assert.AreEqual("0.2NE", control.symbolIds[0]);
            control = eventDB.GetControl(ControlId(2));
            Assert.IsNull(control.symbolIds[3]);
            Assert.IsNull(control.columnFText);
            control = eventDB.GetControl(ControlId(5));
            Assert.AreEqual("12.2", control.symbolIds[5]);
            Assert.AreEqual(null, control.symbolIds[4]);
            Assert.AreEqual(null, control.symbolIds[3]);
            Assert.AreEqual("2/4", control.columnFText);

            eventDB.Validate();
        }

        [TestMethod]
        public void CodeChange()
        {
            EventDB eventDB = controller.GetEventDB();
            SymbolDB symbolDB = ui.symbolDB;
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(1);
            controller.DescriptionChange(DescriptionControl.ChangeKind.Code, 8, 1, "997");
            controller.SelectTab(0);
            controller.DescriptionChange(DescriptionControl.ChangeKind.Code, 5, 1, "992");

            ControlPoint control;

            control = eventDB.GetControl(ControlId(5));
            Assert.AreEqual("997", control.code);
            control = eventDB.GetControl(ControlId(4));
            Assert.AreEqual("992", control.code);

            Assert.AreEqual("", ui.output.ToString());

            eventDB.Validate();
        }

        [TestMethod]
        public void ChangeControlInCourse()
        {
            // course control 15 -- was control 4 (code 32). change to control 17 (code 302)
            EventDB eventDB = controller.GetEventDB();
            SymbolDB symbolDB = ui.symbolDB;
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            CourseControl courseControl;
            courseControl = eventDB.GetCourseControl(CourseControlId(15));
            Assert.AreEqual("32", eventDB.GetControl(courseControl.control).code);

            controller.SelectTab(3);
            controller.DescriptionChange(DescriptionControl.ChangeKind.Code, 5, 1, "302");

            courseControl = eventDB.GetCourseControl(CourseControlId(15));
            Assert.AreEqual("302", eventDB.GetControl(courseControl.control).code);
            Assert.AreEqual(ControlId(17), courseControl.control);

            Assert.AreEqual("", ui.output.ToString());

            eventDB.Validate();
        }

        [TestMethod]
        public void DuplicateCodeAllControls()
        {
            EventDB eventDB = controller.GetEventDB();
            SymbolDB symbolDB = ui.symbolDB;
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(0);
            controller.DescriptionChange(DescriptionControl.ChangeKind.Code, 7, 1, "302");

            ControlPoint control;

            control = eventDB.GetControl(ControlId(7));
            Assert.AreEqual("189", control.code);

            Assert.AreEqual("ERROR: 'The control code '302' is already used by another control.'\r\n", ui.output.ToString());

            eventDB.Validate();
        }

        [TestMethod]
        public void InvertibleCode()
        {
            EventDB eventDB = controller.GetEventDB();
            SymbolDB symbolDB = ui.symbolDB;
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(0);
            controller.DescriptionChange(DescriptionControl.ChangeKind.Code, 7, 1, "666");

            ControlPoint control;

            control = eventDB.GetControl(ControlId(7));
            Assert.AreEqual("666", control.code);   // Change did commit.

            Assert.AreEqual("WARNING: 'A control code should not look like another number when upside-down.'\r\n", ui.output.ToString());

            eventDB.Validate();
        }

        [TestMethod]
        public void EventTitleChange()
        {
            EventDB eventDB = controller.GetEventDB();
            SymbolDB symbolDB = ui.symbolDB;
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(1);
            controller.DescriptionChange(DescriptionControl.ChangeKind.Title, 0, 0, "Nifty Event");

            Event e;

            e = eventDB.GetEvent();
            Assert.AreEqual("Nifty Event", e.title);

            eventDB.Validate();
        }

        [TestMethod]
        public void CourseClimbChange()
        {
            EventDB eventDB = controller.GetEventDB();
            SymbolDB symbolDB = ui.symbolDB;
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(1);
            controller.DescriptionChange(DescriptionControl.ChangeKind.Climb, 1, 2, "213.4");
            controller.SelectTab(3);
            controller.DescriptionChange(DescriptionControl.ChangeKind.Climb, 1, 2, "");

            Assert.AreEqual(213.4F, eventDB.GetCourse(CourseId(6)).climb);
            Assert.AreEqual(-1F, eventDB.GetCourse(CourseId(4)).climb);

            controller.SelectTab(3);
            controller.DescriptionChange(DescriptionControl.ChangeKind.Climb, 1, 2, "25m");

            Assert.AreEqual(25F, eventDB.GetCourse(CourseId(4)).climb);

            eventDB.Validate();
        }

        [TestMethod]
        public void CourseNameChange()
        {
            EventDB eventDB = controller.GetEventDB();
            SymbolDB symbolDB = ui.symbolDB;
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(1);
            controller.DescriptionChange(DescriptionControl.ChangeKind.CourseName, 1, 0, "Blue 1");

            Assert.AreEqual("Blue 1", eventDB.GetCourse(CourseId(6)).name);

            eventDB.Validate();
        }

        [TestMethod]
        public void SecondaryTitleChange()
        {
            EventDB eventDB = controller.GetEventDB();
            SymbolDB symbolDB = ui.symbolDB;
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(5);
            controller.DescriptionChange(DescriptionControl.ChangeKind.SecondaryTitle, 1, 0, null);
            controller.SelectTab(4);
            controller.DescriptionChange(DescriptionControl.ChangeKind.SecondaryTitle, 1, 0, "hello");

            Assert.IsNull(eventDB.GetCourse(CourseId(1)).secondaryTitle);
            Assert.AreEqual("hello", eventDB.GetCourse(CourseId(5)).secondaryTitle);

            eventDB.Validate();
        }

        [TestMethod]
        public void ScoreChange()
        {
            EventDB eventDB = controller.GetEventDB();
            SymbolDB symbolDB = ui.symbolDB;
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(4);
            controller.DescriptionChange(DescriptionControl.ChangeKind.Score, 7, 0, "35");
            controller.DescriptionChange(DescriptionControl.ChangeKind.Score, 8, 0, "");
            controller.SelectTab(2);
            controller.DescriptionChange(DescriptionControl.ChangeKind.Score, 4, 0, "100");

            CourseControl courseControl;

            courseControl = eventDB.GetCourseControl(CourseControlId(102));
            Assert.AreEqual(35, courseControl.points);
            courseControl = eventDB.GetCourseControl(CourseControlId(112));
            Assert.AreEqual(0, courseControl.points);
            courseControl = eventDB.GetCourseControl(CourseControlId(52));
            Assert.AreEqual(100, courseControl.points);

            Assert.AreEqual("", ui.output.ToString());

            eventDB.Validate();
        }

        [TestMethod]
        public void KeyValueChange()
        {
            Dictionary<string, List<SymbolText>> customSymbolText;
            Dictionary<string, bool> customSymbolKey;
            EventDB eventDB = controller.GetEventDB();
            SymbolDB symbolDB = ui.symbolDB;
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent5.ppen"), true);
            Assert.IsTrue(success);

            // Change to new text.
            controller.SelectTab(1);
            controller.SelectDescriptionLine(17);
            controller.DescriptionChange(DescriptionControl.ChangeKind.Key, 17, 0, "MASH");
            controller.GetCustomSymbolText(out customSymbolText, out customSymbolKey);
            Assert.AreEqual("MASH", customSymbolText["12.1"][0].Text);
            Assert.IsTrue(customSymbolKey["12.1"]);

            // Remove
            controller.SelectDescriptionLine(18);
            controller.DescriptionChange(DescriptionControl.ChangeKind.Key, 18, 0, null);
            controller.GetCustomSymbolText(out customSymbolText, out customSymbolKey);
            Assert.IsFalse(customSymbolText.ContainsKey("6.1"));
            Assert.IsFalse(customSymbolKey.ContainsKey("6.1"));
        }
	

        [TestMethod]
        public void BadScore()
        {
            EventDB eventDB = controller.GetEventDB();
            SymbolDB symbolDB = ui.symbolDB;
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(4);
            controller.DescriptionChange(DescriptionControl.ChangeKind.Score, 7, 0, "-1");

            CourseControl courseControl;

            courseControl = eventDB.GetCourseControl(CourseControlId(102));
            Assert.AreEqual(10, courseControl.points);

            Assert.AreEqual("ERROR: 'The points for a control must be an integer 1-999.'\r\n", ui.output.ToString());

            eventDB.Validate();
        }

        [TestMethod]
        public void BadClimb()
        {
            EventDB eventDB = controller.GetEventDB();
            SymbolDB symbolDB = ui.symbolDB;
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(5);
            controller.DescriptionChange(DescriptionControl.ChangeKind.Climb, 1, 2, "-1");

            Assert.AreEqual(66F, eventDB.GetCourse(CourseId(1)).climb);
            Assert.AreEqual("ERROR: 'The climb for a course must be a number 0-9999, or blank.'\r\n", ui.output.ToString());

            eventDB.Validate();
        }

        [TestMethod]
        public void MapNameAndType()
        {
            string fileName = TestUtil.GetTestFile("controller\\sampleevent1.coursescribe");

            bool success = controller.LoadInitialFile(fileName, true);
            Assert.IsTrue(success);

            Assert.AreEqual(MapType.OCAD, controller.MapType);
            Assert.AreEqual(TestUtil.GetTestFile("controller\\Lake Sammamish.ocd"), controller.MapFileName);
        }

        [TestMethod]
        public void ChangeMap()
        {
            UndoMgr undomgr = controller.GetUndoMgr();
            string fileName = TestUtil.GetTestFile("controller\\sampleevent1.coursescribe");

            bool success = controller.LoadInitialFile(fileName, true);
            Assert.IsTrue(success);

            Assert.AreEqual(MapType.OCAD, controller.MapType);
            Assert.AreEqual(TestUtil.GetTestFile("controller\\Lake Sammamish.ocd"), controller.MapFileName);
            Assert.AreEqual(15000, controller.MapScale);

            controller.ChangeMapFile(MapType.Bitmap, TestUtil.GetTestFile("controller\\SampleEvent.jpg"), 14000, 128);

            Assert.AreEqual(MapType.Bitmap, controller.MapType);
            Assert.AreEqual(TestUtil.GetTestFile("controller\\SampleEvent.jpg"), controller.MapFileName);
            Assert.AreEqual(14000, controller.MapScale);
            Assert.AreEqual(128, controller.MapDpi);

            MapDisplay mapDisplay = controller.MapDisplay;
            Assert.AreEqual(MapType.Bitmap, mapDisplay.MapType);
            Assert.AreEqual(TestUtil.GetTestFile("controller\\SampleEvent.jpg"), mapDisplay.FileName);
            Assert.AreEqual(128, mapDisplay.Dpi);

            undomgr.Undo();

            Assert.AreEqual(MapType.OCAD, controller.MapType);
            Assert.AreEqual(TestUtil.GetTestFile("controller\\Lake Sammamish.ocd"), controller.MapFileName);
            Assert.AreEqual(15000, controller.MapScale);

            mapDisplay = controller.MapDisplay;
            Assert.AreEqual(MapType.OCAD, mapDisplay.MapType);
            Assert.AreEqual(TestUtil.GetTestFile("controller\\Lake Sammamish.ocd"), mapDisplay.FileName);
            Assert.AreEqual(15000, mapDisplay.MapScale);

            undomgr.Redo();

            Assert.AreEqual(MapType.Bitmap, controller.MapType);
            Assert.AreEqual(TestUtil.GetTestFile("controller\\SampleEvent.jpg"), controller.MapFileName);
            Assert.AreEqual(14000, controller.MapScale);
            Assert.AreEqual(128, controller.MapDpi);

            mapDisplay = controller.MapDisplay;
            Assert.AreEqual(MapType.Bitmap, mapDisplay.MapType);
            Assert.AreEqual(TestUtil.GetTestFile("controller\\SampleEvent.jpg"), mapDisplay.FileName);
            Assert.AreEqual(128, mapDisplay.Dpi);
        }
	

        [TestMethod]
        public void TabList()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            string[] expected = { "All controls", "Green Y", "Rambo", "SampleCourse4", "Score 4", "White", "Yellow" };
            string[] actual = controller.GetTabNames();

            Assert.AreEqual(7, actual.Length);

            for (int i = 0; i < expected.Length; ++i) {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void ActiveTab()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            Assert.AreEqual(0, controller.ActiveTab);

            controller.SelectTab(5);
            Assert.AreEqual(5, controller.ActiveTab);

            UndoMgr undoMgr = controller.GetUndoMgr();
            EventDB eventDB = controller.GetEventDB();

            undoMgr.BeginCommand(197, "Add course");
            eventDB.AddCourse(new Course(CourseKind.Normal, "AAA", 15000, 1));
            undoMgr.EndCommand(197);

            Assert.AreEqual(6, controller.ActiveTab);

            undoMgr.BeginCommand(198, "Remove courses");
            eventDB.RemoveCourse(CourseId(1));
            undoMgr.EndCommand(198);

            Assert.AreEqual(0, controller.ActiveTab);
        }

        [TestMethod]
        public void CanDelete()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            Assert.IsFalse(controller.CanDeleteSelection());

            controller.SelectTab(1);
            Assert.IsFalse(controller.CanDeleteSelection());

            controller.SelectDescriptionLine(7);
            Assert.IsTrue(controller.CanDeleteSelection());

            controller.SelectTab(0);
            Assert.IsFalse(controller.CanDeleteSelection());
            controller.SelectDescriptionLine(2);
            Assert.IsTrue(controller.CanDeleteSelection());
        }

        [TestMethod]
        public void CanDeleteSpecial()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            Assert.IsFalse(controller.CanDeleteSelection());
            controller.GetSelectionMgr().SelectSpecial(SpecialId(1));
            Assert.IsTrue(controller.CanDeleteSelection());

            controller.SelectTab(3);

            Assert.IsFalse(controller.CanDeleteSelection());
            controller.GetSelectionMgr().SelectSpecial(SpecialId(3));
            Assert.IsTrue(controller.CanDeleteSelection());
        }

        [TestMethod]
        public void DeleteSpecial()
        {
            EventDB eventDB = controller.GetEventDB();
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            controller.GetSelectionMgr().SelectSpecial(SpecialId(1));
            success = controller.DeleteSelection();
            Assert.IsTrue(success);
            Assert.IsFalse(eventDB.IsSpecialPresent(SpecialId(1)));

            controller.SelectTab(3);
            controller.GetSelectionMgr().SelectSpecial(SpecialId(3));
            success = controller.DeleteSelection();
            Assert.IsTrue(success);
            Assert.IsFalse(eventDB.IsSpecialPresent(SpecialId(3)));

            controller.Undo();
            controller.Undo();

            Assert.IsTrue(eventDB.IsSpecialPresent(SpecialId(3)));
            Assert.IsTrue(eventDB.IsSpecialPresent(SpecialId(1)));
        }

        [TestMethod]
        public void DeleteCourseControl()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(1);
            controller.SelectDescriptionLine(7);

            success = controller.DeleteSelection();
            Assert.IsTrue(success);

            EventDB eventDB = controller.GetEventDB();
            Assert.IsFalse(eventDB.IsCourseControlPresent(CourseControlId(206)));
            Assert.AreEqual(207, eventDB.GetCourseControl(CourseControlId(205)).nextCourseControl.id);
            Assert.IsTrue(eventDB.IsControlPresent(ControlId(17)));
            Assert.AreEqual("", ui.output.ToString());  // no messages to the user.

            controller.Undo();

            Assert.IsTrue(eventDB.IsCourseControlPresent(CourseControlId(206)));
            Assert.AreEqual(206, eventDB.GetCourseControl(CourseControlId(205)).nextCourseControl.id);
        }

        [TestMethod]
        public void DeleteCourseControlAndControl()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(4);
            controller.SelectDescriptionLine(14);

            ui.returnQuestion = DialogResult.Yes;
            success = controller.DeleteSelection();
            Assert.IsTrue(success);

            EventDB eventDB = controller.GetEventDB();
            Assert.IsFalse(eventDB.IsCourseControlPresent(CourseControlId(111)));
            Assert.AreEqual(112, eventDB.GetCourseControl(CourseControlId(110)).nextCourseControl.id);
            Assert.IsFalse(eventDB.IsControlPresent(ControlId(19)));
            Assert.AreEqual(
@"YES/NO QUESTION: 'Control ""304"" is no longer used by any course. Do you want to delete this control from the control collection?' (default no)
  (returned yes)
",
            ui.output.ToString());

            controller.Undo();

            Assert.IsTrue(eventDB.IsCourseControlPresent(CourseControlId(206)));
            Assert.AreEqual(206, eventDB.GetCourseControl(CourseControlId(205)).nextCourseControl.id);
            Assert.IsTrue(eventDB.IsControlPresent(ControlId(19)));
        }

        [TestMethod]
        public void DeleteCourseControlButNotControl()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(4);
            controller.SelectDescriptionLine(14);

            ui.returnQuestion = DialogResult.No;
            success = controller.DeleteSelection();
            Assert.IsTrue(success);

            EventDB eventDB = controller.GetEventDB();
            Assert.IsFalse(eventDB.IsCourseControlPresent(CourseControlId(111)));
            Assert.AreEqual(112, eventDB.GetCourseControl(CourseControlId(110)).nextCourseControl.id);
            Assert.IsTrue(eventDB.IsControlPresent(ControlId(19)));
            Assert.AreEqual(
@"YES/NO QUESTION: 'Control ""304"" is no longer used by any course. Do you want to delete this control from the control collection?' (default no)
  (returned no)
",
            ui.output.ToString());

            controller.Undo();

            Assert.IsTrue(eventDB.IsCourseControlPresent(CourseControlId(206)));
            Assert.AreEqual(206, eventDB.GetCourseControl(CourseControlId(205)).nextCourseControl.id);
            Assert.IsTrue(eventDB.IsControlPresent(ControlId(19)));
        }

        [TestMethod]
        public void DeleteAllControlsUnusedControl()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(0);
            controller.SelectDescriptionLine(3);

            ui.returnQuestion = DialogResult.No;
            success = controller.DeleteSelection();
            Assert.IsTrue(success);

            EventDB eventDB = controller.GetEventDB();
            Assert.IsFalse(eventDB.IsControlPresent(ControlId(23)));
            Assert.AreEqual("", ui.output.ToString());

            controller.Undo();

            Assert.IsTrue(eventDB.IsControlPresent(ControlId(23)));
        }

        [TestMethod]
        public void DeleteAllControlsUsedControlYes()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(0);
            controller.SelectDescriptionLine(18);

            ui.returnQuestion = DialogResult.Yes;
            success = controller.DeleteSelection();
            Assert.IsTrue(success);

            EventDB eventDB = controller.GetEventDB();
            Assert.IsFalse(eventDB.IsControlPresent(ControlId(20)));
            Assert.IsFalse(eventDB.IsCourseControlPresent(CourseControlId(202)));
            Assert.AreEqual(203, eventDB.GetCourseControl(CourseControlId(201)).nextCourseControl.id);
            Assert.IsFalse(eventDB.IsCourseControlPresent(CourseControlId(112)));
            Assert.AreEqual(113, eventDB.GetCourseControl(CourseControlId(111)).nextCourseControl.id);
            Assert.AreEqual(
@"YES/NO QUESTION: 'Control ""305"" is currently used by the following courses: Green Y, Score 4. Do you want to delete this control anyway?' (default no)
  (returned yes)
",
            ui.output.ToString());

            controller.Undo();

            Assert.IsTrue(eventDB.IsControlPresent(ControlId(20)));
            Assert.IsTrue(eventDB.IsCourseControlPresent(CourseControlId(202)));
            Assert.AreEqual(202, eventDB.GetCourseControl(CourseControlId(201)).nextCourseControl.id);
            Assert.IsTrue(eventDB.IsCourseControlPresent(CourseControlId(112)));
            Assert.AreEqual(112, eventDB.GetCourseControl(CourseControlId(111)).nextCourseControl.id);
        }

        [TestMethod]
        public void DeleteAllControlsUsedControlNo()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(0);
            controller.SelectDescriptionLine(18);

            ui.returnQuestion = DialogResult.No;
            success = controller.DeleteSelection();
            Assert.IsFalse(success);

            EventDB eventDB = controller.GetEventDB();
            Assert.IsTrue(eventDB.IsControlPresent(ControlId(20)));
            Assert.IsTrue(eventDB.IsCourseControlPresent(CourseControlId(202)));
            Assert.AreEqual(202, eventDB.GetCourseControl(CourseControlId(201)).nextCourseControl.id);
            Assert.IsTrue(eventDB.IsCourseControlPresent(CourseControlId(112)));
            Assert.AreEqual(112, eventDB.GetCourseControl(CourseControlId(111)).nextCourseControl.id);
            Assert.AreEqual(
@"YES/NO QUESTION: 'Control ""305"" is currently used by the following courses: Green Y, Score 4. Do you want to delete this control anyway?' (default no)
  (returned no)
",
            ui.output.ToString());
        }

        [TestMethod]
        public void DeleteStartCourseControlAndControl()
        {
            EventDB eventDB = controller.GetEventDB();
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            // Change course 6 to use a new start control.
            UndoMgr undoMgr = controller.GetUndoMgr();
            undoMgr.BeginCommand(334, "Add start");
            Id<CourseControl> courseControlId = ChangeEvent.AddStartToCourse(eventDB, ControlId(23), CourseId(6), false);
            undoMgr.EndCommand(334);

            controller.SelectTab(1);
            controller.SelectDescriptionLine(2);

            ui.returnQuestion = DialogResult.Yes;
            success = controller.DeleteSelection();
            Assert.IsTrue(success);

            Assert.IsFalse(eventDB.IsCourseControlPresent(courseControlId));
            Assert.IsFalse(eventDB.IsControlPresent(ControlId(23)));
            Assert.AreEqual(
@"YES/NO QUESTION: 'Control ""Start"" is no longer used by any course. Do you want to delete this control from the control collection?' (default no)
  (returned yes)
",
            ui.output.ToString());

            controller.Undo();

            Assert.IsTrue(eventDB.IsCourseControlPresent(courseControlId));
            Assert.IsTrue(eventDB.IsControlPresent(ControlId(23)));
        }

        // Is there an all controls layer?
        internal static bool IsAllControlsLayer(CourseLayout courseLayout)
        {
            foreach (CourseObj obj in courseLayout) {
                if (obj.layer == CourseLayer.AllControls)
                    return true;
            }

            return false;
        }

        [TestMethod]
        public void ShowAllControls()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(1);

            CourseLayout course = controller.GetCourseLayout();
            Assert.IsFalse(IsAllControlsLayer(course));

            // Turn show all controls on. Should cause a changenum change as well as save the property.
            Assert.IsFalse(controller.ShowAllControls);
            long changeNum = controller.ChangeNum;

            controller.ShowAllControls = true;

            Assert.IsTrue(controller.ShowAllControls);
            Assert.AreNotEqual((decimal) changeNum, (decimal) controller.ChangeNum);
            changeNum = controller.ChangeNum;

            // Setting again should not cause a ChangeNum change.
            controller.ShowAllControls = true;
            Assert.IsTrue(controller.ShowAllControls);
            Assert.AreEqual((decimal) changeNum, (decimal) controller.ChangeNum);

            // We should now have a background layout with the all controls part in it.
            course = controller.GetCourseLayout();
            Assert.IsTrue(IsAllControlsLayer(course));

            controller.ShowAllControls = false;

            Assert.IsFalse(controller.ShowAllControls);
            Assert.AreNotEqual((decimal) changeNum, (decimal) controller.ChangeNum);

            course = controller.GetCourseLayout();
            Assert.IsFalse(IsAllControlsLayer(course));
        }

        [TestMethod]
        public void ShowAllControls2()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            // Show all controls is available for the all controls tab, but doesn't add another course layout.
            controller.SelectTab(0);

            CourseLayout course = controller.GetCourseLayout();
            Assert.IsFalse(IsAllControlsLayer(course));

            // Turn show all controls on. Should cause a changenum change as well as save the property.
            Assert.IsFalse(controller.ShowAllControls);
            long changeNum = controller.ChangeNum;

            controller.ShowAllControls = true;

            Assert.IsTrue(controller.ShowAllControls);
            Assert.AreNotEqual((decimal) changeNum, (decimal) controller.ChangeNum);
            changeNum = controller.ChangeNum;

            // Setting again should not cause a ChangeNum change.
            controller.ShowAllControls = true;
            Assert.IsTrue(controller.ShowAllControls);
            Assert.AreEqual((decimal) changeNum, (decimal) controller.ChangeNum);

            // We should now have a background layout with the all controls part in it.
            course = controller.GetCourseLayout();
            Assert.IsFalse(IsAllControlsLayer(course));

            controller.ShowAllControls = false;

            Assert.IsFalse(controller.ShowAllControls);
            Assert.AreNotEqual((decimal) changeNum, (decimal) controller.ChangeNum);
        }

        [TestMethod]
        public void SetTemporaryControlView()
        {
            StringWriter writer;
            string mainCourseText;
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent3.coursescribe"), true);
            Assert.IsTrue(success);

            controller.SelectTab(1);
            CourseLayout course = controller.GetCourseLayout();
            writer = new StringWriter();
            course.Dump(writer);
            mainCourseText = writer.ToString();

            controller.SetTemporaryControlView(true, ControlPointKind.Normal);
            course = controller.GetCourseLayout();
            writer = new StringWriter();
            course.Dump(writer);
            Assert.AreEqual(mainCourseText + 
@"Control:        layer:2  control:3  scale:1  location:(20,-10.5)  gaps:11111111111111111111111111011111
Control:        layer:2  control:4  scale:1  location:(35.4,-22.5)  gaps:11111111111111111111111111111111
Code:           layer:2  control:3  scale:1  text:32  top-left:(13.15,-10.97)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:2  control:4  scale:1  text:GO  top-left:(38.27,-16.92)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
", writer.ToString());

            controller.SetTemporaryControlView(true, ControlPointKind.Start);
            course = controller.GetCourseLayout();
            writer = new StringWriter();
            course.Dump(writer);
            Assert.AreEqual(mainCourseText + 
@"Start:          layer:2  control:7  scale:1  location:(0,5)  orientation:0
", writer.ToString());

            controller.ShowAllControls = true;
            course = controller.GetCourseLayout();
            writer = new StringWriter();
            course.Dump(writer);
            Assert.AreEqual(mainCourseText + 
@"Start:          layer:2  control:7  scale:1  location:(0,5)  orientation:0
", writer.ToString());

            controller.SetTemporaryControlView(false, ControlPointKind.None);
            course = controller.GetCourseLayout();
            writer = new StringWriter();
            course.Dump(writer);
            Assert.AreEqual(mainCourseText +
@"Start:          layer:2  control:7  scale:1  location:(0,5)  orientation:0
Control:        layer:2  control:3  scale:1  location:(20,-10.5)  gaps:11111111111111111111111111011111
Control:        layer:2  control:4  scale:1  location:(35.4,-22.5)  gaps:11111111111111111111111111111111
Code:           layer:2  control:3  scale:1  text:32  top-left:(13.15,-10.97)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
Code:           layer:2  control:4  scale:1  text:GO  top-left:(38.27,-16.92)
                font-name:Arial Narrow  font-style:Bold  font-height:4.18
", writer.ToString());

            controller.ShowAllControls = false;
            course = controller.GetCourseLayout();
            writer = new StringWriter();
            course.Dump(writer);
            Assert.AreEqual(mainCourseText, writer.ToString());

        }

        [TestMethod]
        public void ScrollHighlightIntoView()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            Assert.IsFalse(controller.ScrollHighlightIntoView);

            controller.SelectTab(1);
            controller.SelectDescriptionLine(4);

            // SelectionIntoView only returns true once.
            Assert.IsTrue(controller.ScrollHighlightIntoView);
            Assert.IsFalse(controller.ScrollHighlightIntoView);
        }

        [TestMethod]
        public void DeleteCourseNotControls()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\marymoor.coursescribe"), true);
            Assert.IsTrue(success);

            Assert.IsFalse(controller.CanDeleteCurrentCourse());

            controller.SelectTab(6);

            Assert.IsTrue(controller.CanDeleteCurrentCourse());

            ui.returnQuestion = DialogResult.No;
            success = controller.DeleteCurrentCourse();
            Assert.IsTrue(success);

            EventDB eventDB = controller.GetEventDB();
            Assert.IsFalse(eventDB.IsCoursePresent(CourseId(6)));
            Assert.IsFalse(eventDB.IsCourseControlPresent(CourseControlId(601)));
            Assert.IsFalse(eventDB.IsCourseControlPresent(CourseControlId(620)));
            Assert.IsFalse(eventDB.IsCourseControlPresent(CourseControlId(608)));
            Assert.IsTrue(eventDB.IsControlPresent(ControlId(35)));
            Assert.IsTrue(eventDB.IsControlPresent(ControlId(37)));
            Assert.AreEqual(
@"YES/NO QUESTION: 'Controls ""35"", ""37"" are no longer used by any course. Do you want to delete these controls from the control collection?' (default no)
  (returned no)
",
            ui.output.ToString());

            controller.Undo();

            Assert.IsTrue(eventDB.IsCoursePresent(CourseId(6)));
            Assert.IsTrue(eventDB.IsCourseControlPresent(CourseControlId(601)));
            Assert.IsTrue(eventDB.IsCourseControlPresent(CourseControlId(620)));
            Assert.IsTrue(eventDB.IsCourseControlPresent(CourseControlId(608)));
            Assert.IsTrue(eventDB.IsControlPresent(ControlId(35)));
            Assert.IsTrue(eventDB.IsControlPresent(ControlId(37)));
        }

        [TestMethod]
        public void DeleteCourseAndControls()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\marymoor.coursescribe"), true);
            Assert.IsTrue(success);

            Assert.IsFalse(controller.CanDeleteCurrentCourse());

            controller.SelectTab(6);

            Assert.IsTrue(controller.CanDeleteCurrentCourse());

            ui.returnQuestion = DialogResult.Yes;
            success = controller.DeleteCurrentCourse();
            Assert.IsTrue(success);

            EventDB eventDB = controller.GetEventDB();
            Assert.IsFalse(eventDB.IsCoursePresent(CourseId(6)));
            Assert.IsFalse(eventDB.IsCourseControlPresent(CourseControlId(601)));
            Assert.IsFalse(eventDB.IsCourseControlPresent(CourseControlId(620)));
            Assert.IsFalse(eventDB.IsCourseControlPresent(CourseControlId(608)));
            Assert.IsFalse(eventDB.IsControlPresent(ControlId(35)));
            Assert.IsFalse(eventDB.IsControlPresent(ControlId(37)));
            Assert.AreEqual(
@"YES/NO QUESTION: 'Controls ""35"", ""37"" are no longer used by any course. Do you want to delete these controls from the control collection?' (default no)
  (returned yes)
",
            ui.output.ToString());

            controller.Undo();

            Assert.IsTrue(eventDB.IsCoursePresent(CourseId(6)));
            Assert.IsTrue(eventDB.IsCourseControlPresent(CourseControlId(601)));
            Assert.IsTrue(eventDB.IsCourseControlPresent(CourseControlId(620)));
            Assert.IsTrue(eventDB.IsCourseControlPresent(CourseControlId(608)));
            Assert.IsTrue(eventDB.IsControlPresent(ControlId(35)));
            Assert.IsTrue(eventDB.IsControlPresent(ControlId(37)));
        }

        [TestMethod]
        public void AddNewCourse()
        {
            EventDB eventDB = controller.GetEventDB();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\marymoor.coursescribe"), true);
            Assert.IsTrue(success);

            controller.NewCourse(CourseKind.Normal, "My New Course", ControlLabelKind.SequenceAndCode, 1, "Secondary Title", 15000, 25, DescriptionKind.Symbols, 3);
            Assert.AreEqual("My New Course", controller.GetTabNames()[controller.ActiveTab]);
            Id<Course> newCourse = controller.GetSelectionMgr().Selection.ActiveCourseId;

            Course course = eventDB.GetCourse(newCourse);
            Assert.AreEqual(CourseKind.Normal, course.kind);
            Assert.AreEqual("My New Course", course.name);
            Assert.AreEqual("Secondary Title", course.secondaryTitle);
            Assert.AreEqual(DescriptionKind.Symbols, course.descKind);
            Assert.AreEqual(15000F, course.printScale);
            Assert.AreEqual(25F, course.climb);
            Assert.AreEqual(3, course.firstControlOrdinal);
            Assert.AreEqual(ControlLabelKind.SequenceAndCode, course.labelKind);
            Assert.AreEqual(1, course.scoreColumn);
            Assert.AreEqual(1, eventDB.GetCourseControl(course.firstCourseControl).control.id);
            Assert.AreEqual(2, eventDB.GetCourseControl(eventDB.GetCourseControl(course.firstCourseControl).nextCourseControl).control.id);
        }

        [TestMethod]
        public void ChangeCourseProperties()
        {
            EventDB eventDB = controller.GetEventDB();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\marymoor.coursescribe"), true);

            Assert.IsTrue(success);

            Assert.IsFalse(controller.CanChangeCourseProperties());

            controller.SelectTab(3);
            Assert.IsTrue(controller.CanChangeCourseProperties());

            CourseKind courseKind;
            string courseName, secondaryTitle;
            float printScale, climb;
            DescriptionKind descKind;
            int firstControlOrdinal;
            ControlLabelKind labelKind;
            int scoreColumn;

            controller.GetCurrentCourseProperties(out courseKind, out courseName, out labelKind, out scoreColumn, out secondaryTitle, out printScale, out climb, out descKind, out firstControlOrdinal);
            Assert.AreEqual(CourseKind.Normal, courseKind);
            Assert.AreEqual("Course 3", courseName);
            Assert.AreEqual(null, secondaryTitle);
            Assert.AreEqual(10000, printScale);
            Assert.IsTrue(climb < 0);
            Assert.AreEqual(DescriptionKind.Symbols, descKind);
            Assert.AreEqual(1, firstControlOrdinal);
            Assert.AreEqual(-1, scoreColumn);
            Assert.AreEqual(ControlLabelKind.Sequence, labelKind);

            controller.ChangeCurrentCourseProperties(CourseKind.Score, "Xavier", ControlLabelKind.Code, 1, "super hard", 5000, 55, DescriptionKind.SymbolsAndText, 12);

            Assert.AreEqual(3, controller.ActiveTab);   // changing name does not change the sort order.
            controller.GetCurrentCourseProperties(out courseKind, out courseName, out labelKind, out scoreColumn, out secondaryTitle, out printScale, out climb, out descKind, out firstControlOrdinal);
            Assert.AreEqual(CourseKind.Score, courseKind);
            Assert.AreEqual("Xavier", courseName);
            Assert.AreEqual(ControlLabelKind.Code, labelKind);
            Assert.AreEqual("super hard", secondaryTitle);
            Assert.AreEqual(5000, printScale);
            Assert.AreEqual(55, climb);
            Assert.AreEqual(DescriptionKind.SymbolsAndText, descKind);
            Assert.AreEqual(12, firstControlOrdinal);
            Assert.AreEqual(1, scoreColumn);
        }

        [TestMethod]
        public void GetAllControlsProperties()
        {
            EventDB eventDB = controller.GetEventDB();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent12.ppen"), true);

            Assert.IsTrue(success);

            float printScale;
            DescriptionKind descKind;

            controller.GetAllControlsProperties(out printScale, out descKind);
            Assert.AreEqual(9000, printScale);
            Assert.AreEqual(DescriptionKind.SymbolsAndText, descKind);
        }

        [TestMethod]
        public void MoveSpecial()
        {
            EventDB eventDB = controller.GetEventDB();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            controller.MoveSpecial(SpecialId(1), new PointF[1] { new PointF(12.1F, -38.1F) });
            Assert.AreEqual(new PointF(12.1F, -38.1F), eventDB.GetSpecial(SpecialId(1)).locations[0]);
        }

        [TestMethod]
        public void MoveSpecialDelta()
        {
            EventDB eventDB = controller.GetEventDB();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\marymoor2.coursescribe"), true);
            Assert.IsTrue(success);

            controller.MoveSpecialDelta(SpecialId(1), 6.5F, -1.1F);
            Assert.AreEqual(new PointF(14.5F + 6.5F, 31.2F - 1.1F), eventDB.GetSpecial(SpecialId(1)).locations[0]);

            controller.MoveSpecialDelta(SpecialId(3), 6.5F, -1.1F);
            Assert.AreEqual(new PointF(11F + 6.5F, 2F - 1.1F), eventDB.GetSpecial(SpecialId(3)).locations[0]);
            Assert.AreEqual(new PointF(0F + 6.5F, -7F - 1.1F), eventDB.GetSpecial(SpecialId(3)).locations[1]);
            Assert.AreEqual(new PointF(-12F + 6.5F, -3F - 1.1F), eventDB.GetSpecial(SpecialId(3)).locations[2]);
        }

        [TestMethod]
        public void CanSetLegFlagging()
        {
            EventDB eventDB = controller.GetEventDB();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\speciallegs.coursescribe"), true);
            Assert.IsTrue(success);

            FlaggingKind flagging;

            controller.SelectTab(1);
            controller.SelectDescriptionLine(1);
            Assert.AreEqual(CommandStatus.Disabled, controller.CanSetLegFlagging(out flagging));

            controller.GetSelectionMgr().SelectLeg(CourseControlId(2), CourseControlId(3));
            Assert.AreEqual(CommandStatus.Enabled, controller.CanSetLegFlagging(out flagging));
            Assert.AreEqual(FlaggingKind.All, flagging);
        }

        [TestMethod]
        public void SetLegFlagging()
        {
            EventDB eventDB = controller.GetEventDB();
            UndoMgr undoMgr = controller.GetUndoMgr();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\speciallegs.coursescribe"), true);
            Assert.IsTrue(success);

            FlaggingKind flagging;

            controller.SelectTab(1);
            controller.SelectDescriptionLine(1);
            Assert.AreEqual(CommandStatus.Disabled, controller.CanSetLegFlagging(out flagging));

            controller.GetSelectionMgr().SelectLeg(CourseControlId(2), CourseControlId(3));
            controller.SetLegFlagging(FlaggingKind.Begin);

            Assert.AreEqual(FlaggingKind.Begin, QueryEvent.GetLegFlagging(eventDB, ControlId(2), ControlId(3)));

            controller.Undo();

            Assert.AreEqual(FlaggingKind.All, QueryEvent.GetLegFlagging(eventDB, ControlId(2), ControlId(3)));
        }


        [TestMethod]
        public void GetAllCodes()
        {
            EventDB eventDB = controller.GetEventDB();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            string[] expected = { "31","32","74","189","190","191","210","211","290","291","301","302","303","304","305","306","GO"};
            KeyValuePair<object, string>[] codes = controller.GetAllControlCodes();

            Assert.AreEqual(expected.Length, codes.Length);
            for (int i = 0; i < expected.Length; ++i) {
                Assert.AreEqual(expected[i], codes[i].Value);
            }
        }

        [TestMethod]
        public void SetAllCodes()
        {
            EventDB eventDB = controller.GetEventDB();
            UndoMgr undoMgr = controller.GetUndoMgr();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            string[] expected = { "31", "32", "74", "189", "190", "191", "210", "211", "290", "291", "301", "302", "303", "304", "305", "306", "GO" };
            KeyValuePair<object, string>[] codes = controller.GetAllControlCodes();

            Assert.AreEqual(expected.Length, codes.Length);
            for (int i = 0; i < expected.Length; ++i) {
                Assert.AreEqual(expected[i], codes[i].Value);
            }

            codes[2] = new KeyValuePair<object,string>(codes[2].Key, "54");
            codes[4] = new KeyValuePair<object, string>(codes[4].Key, "XL");
            codes[16] = new KeyValuePair<object, string>(codes[16].Key, "ZZ");
            controller.SetAllControlCodes(codes);
            eventDB.Validate();

            Assert.AreEqual("54", eventDB.GetControl(ControlId(12)).code);
            Assert.AreEqual("XL", eventDB.GetControl(ControlId(10)).code);
            Assert.AreEqual("ZZ", eventDB.GetControl(ControlId(5)).code);

            undoMgr.Undo();

            Assert.AreEqual("74", eventDB.GetControl(ControlId(12)).code);
            Assert.AreEqual("190", eventDB.GetControl(ControlId(10)).code);
            Assert.AreEqual("GO", eventDB.GetControl(ControlId(5)).code);
        }

        [TestMethod]
        public void GetAllCourseLoads()
        {
            Controller.CourseLoadInfo[] loads;

            EventDB eventDB = controller.GetEventDB();
            UndoMgr undoMgr = controller.GetUndoMgr();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\marymoor3.coursescribe"), true);
            Assert.IsTrue(success);

            loads = controller.GetAllCourseLoads();

            Assert.AreEqual(10, loads.Length);

            Assert.AreEqual("Course 1", loads[0].courseName);
            Assert.AreEqual(1, loads[0].load);
            Assert.AreEqual("Course 2", loads[1].courseName);
            Assert.AreEqual(2, loads[1].load);
            Assert.AreEqual("Course 3", loads[2].courseName);
            Assert.AreEqual(3, loads[2].load);
            Assert.AreEqual("Course 4B", loads[3].courseName);
            Assert.AreEqual(4, loads[3].load);
            Assert.AreEqual("Course 4G", loads[4].courseName);
            Assert.AreEqual(100, loads[4].load);
            Assert.AreEqual("Course 5", loads[5].courseName);
            Assert.AreEqual(5, loads[5].load);
            Assert.AreEqual("Score", loads[6].courseName);
            Assert.AreEqual(-1, loads[6].load);
            Assert.AreEqual("SingleControl", loads[7].courseName);
            Assert.AreEqual(8, loads[7].load);
            Assert.AreEqual("StartAngle", loads[8].courseName);
            Assert.AreEqual(-1, loads[8].load);
            Assert.AreEqual("Xavier", loads[9].courseName);
            Assert.AreEqual(-1, loads[9].load);
        }

        [TestMethod]
        public void SetAllCourseLoads()
        {
            EventDB eventDB = controller.GetEventDB();
            UndoMgr undoMgr = controller.GetUndoMgr();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\marymoor3.coursescribe"), true);
            Assert.IsTrue(success);

            Controller.CourseLoadInfo[] loads = controller.GetAllCourseLoads();

            loads[3].load = -1;
            loads[4].load = 50;
            loads[8].load = 25;
            loads[0].load = 14;

            controller.SetAllCourseLoads(loads);
            eventDB.Validate();

            Assert.AreEqual(-1, eventDB.GetCourse(CourseId(4)).load);
            Assert.AreEqual(50, eventDB.GetCourse(CourseId(5)).load);
            Assert.AreEqual(25, eventDB.GetCourse(CourseId(8)).load);
            Assert.AreEqual(14, eventDB.GetCourse(CourseId(1)).load);
        }


        [TestMethod]
        public void GetAllCourseSortOrders()
        {
            Controller.CourseOrderInfo[] orders;

            EventDB eventDB = controller.GetEventDB();
            UndoMgr undoMgr = controller.GetUndoMgr();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\marymoor5.coursescribe"), true);
            Assert.IsTrue(success);

            orders = controller.GetAllCourseOrders();

            Assert.AreEqual(6, orders.Length);

            Assert.AreEqual("Course 1", orders[0].courseName);
            Assert.AreEqual(1, orders[0].sortOrder);
            Assert.AreEqual("Course 4G", orders[1].courseName);
            Assert.AreEqual(3, orders[1].sortOrder);
            Assert.AreEqual("Course 3", orders[2].courseName);
            Assert.AreEqual(4, orders[2].sortOrder);
            Assert.AreEqual("Course 2", orders[3].courseName);
            Assert.AreEqual(5, orders[3].sortOrder);
            Assert.AreEqual("Course 4B", orders[4].courseName);
            Assert.AreEqual(8, orders[4].sortOrder);
            Assert.AreEqual("Course 5", orders[5].courseName);
            Assert.AreEqual(11, orders[5].sortOrder);
        }

        [TestMethod]
        public void SetAllCourseSortOrders()
        {
            EventDB eventDB = controller.GetEventDB();
            UndoMgr undoMgr = controller.GetUndoMgr();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\marymoor5.coursescribe"), true);
            Assert.IsTrue(success);

            Controller.CourseOrderInfo[] orders = controller.GetAllCourseOrders();

            orders[0].sortOrder = 4;
            orders[2].sortOrder = 5;
            orders[3].sortOrder = 1;

            controller.SetAllCourseOrders(orders);
            eventDB.Validate();

            Assert.AreEqual(4, eventDB.GetCourse(CourseId(1)).sortOrder);
            Assert.AreEqual(5, eventDB.GetCourse(CourseId(3)).sortOrder);
            Assert.AreEqual(1, eventDB.GetCourse(CourseId(2)).sortOrder);
            Assert.AreEqual(3, eventDB.GetCourse(CourseId(5)).sortOrder);
        }

        [TestMethod]
        public void MoveControlNumber()
        {
            EventDB eventDB = controller.GetEventDB();
            UndoMgr undoMgr = controller.GetUndoMgr();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            Assert.IsFalse(eventDB.GetCourseControl(CourseControlId(206)).customNumberPlacement);

            controller.MoveControlNumber(ControlId(17), CourseControlId(206), new PointF(-31.4F, 27.1F));

            Assert.IsTrue(eventDB.GetCourseControl(CourseControlId(206)).customNumberPlacement);
            Assert.AreEqual(10F, eventDB.GetCourseControl(CourseControlId(206)).numberDeltaX, 0.001F);
            Assert.AreEqual(-8F, eventDB.GetCourseControl(CourseControlId(206)).numberDeltaY, 0.001F);

            controller.MoveControlNumber(ControlId(17), CourseControlId(206), new PointF(-40F, 34F));

            Assert.IsFalse(eventDB.GetCourseControl(CourseControlId(206)).customNumberPlacement);
        }

        [TestMethod]
        public void MoveAllControlsCode()
        {
            EventDB eventDB = controller.GetEventDB();
            UndoMgr undoMgr = controller.GetUndoMgr();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\sampleevent1.coursescribe"), true);
            Assert.IsTrue(success);

            Assert.IsFalse(eventDB.GetControl(ControlId(17)).customCodeLocation);

            controller.MoveControlNumber(ControlId(17), Id<CourseControl>.None, new PointF(-35F, 30F));

            Assert.IsTrue(eventDB.GetControl(ControlId(17)).customCodeLocation);
            Assert.AreEqual(-38.55F, eventDB.GetControl(ControlId(17)).codeLocationAngle, 0.01F);

            controller.MoveControlNumber(ControlId(17), Id<CourseControl>.None, new PointF(-40F, 34F));

            Assert.IsFalse(eventDB.GetControl(ControlId(17)).customCodeLocation);
        }

        [TestMethod]
        public void MissingFontWarning()
        {
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\missingfont.ppen"), true);
            Assert.IsTrue(success);

            // First time, should get the list of missing fonts.
            string[] result = controller.MissingFontList();
            TestUtil.TestEnumerableAnyOrder(result, new string[] { "Spyroclassic", "GeosansLight" });

            // Second time, should be null or empty.
            result = controller.MissingFontList();
            Assert.IsTrue(result == null || result.Length == 0);
        }
	

        void DumpMapFile(string mapFileName, string outputDump)
        {
            using (TextWriter writer = new StreamWriter(outputDump, false, System.Text.Encoding.UTF8)) {
                PurplePen.MapModel.DebugCode.OcadDump dump = new PurplePen.MapModel.DebugCode.OcadDump(TestUtil.GetTestFileDirectory());
                dump.DumpFile(mapFileName, writer);
            }
        }

        void CheckDump(string ocadFile, string expectedDumpFile)
        {
            string directory = Path.GetDirectoryName(ocadFile);
            string basename = Path.GetFileNameWithoutExtension(ocadFile);
            string dumpNewFileName = directory + @"\" + basename + @"_newdump.txt";

            DumpMapFile(ocadFile, dumpNewFileName);
            TestUtil.CompareTextFileBaseline(dumpNewFileName, expectedDumpFile);
            File.Delete(dumpNewFileName);
        }

        // Create some courses, write them, and check against a dump.
        void CreateOcadFiles(string file, OcadCreationSettings settings, CourseAppearance appearance, string[] expectedFiles, string[] expectedDumps)
        {
            EventDB eventDB = controller.GetEventDB();

            bool success = controller.LoadInitialFile(file, true);
            Assert.IsTrue(success);

            controller.SetCourseAppearance(appearance);

            for (int i = 0; i < expectedFiles.Length; ++i) {
                File.Delete(expectedFiles[i]);
            }

            success = controller.CreateOcadFiles(settings);
            Assert.IsTrue(success);

            for (int i = 0; i < expectedFiles.Length; ++i) {
                CheckDump(expectedFiles[i], expectedDumps[i]);
            }
        }

        [TestMethod]
        public void OcadCreation1()
        {
            OcadCreationSettings settings = new OcadCreationSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("controller\\ocad_create1");
            settings.CourseIds = new Id<Course>[1] { CourseId(2) };
            settings.version = 8;
            settings.cyan = 0.15F;
            settings.magenta = 0.9F;
            settings.yellow = 0;
            settings.black = 0.25F;

            Directory.CreateDirectory(settings.outputDirectory);

            CreateOcadFiles(TestUtil.GetTestFile("controller\\marymoor4.ppen"), settings, new CourseAppearance(),
                new string[1] { TestUtil.GetTestFile("controller\\ocad_create1\\Course 2.ocd") },
                new string[1] { TestUtil.GetTestFile("controller\\ocad_create1\\Course 2_expected.txt") });
        }


        [TestMethod]
        public void OcadCreation2()
        {
            OcadCreationSettings settings = new OcadCreationSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("controller\\ocad_create2");
            settings.CourseIds = new Id<Course>[3] { CourseId(3), CourseId(5), Id<Course>.None };
            settings.version = 9;
            settings.cyan = 0.15F;
            settings.magenta = 0.9F;
            settings.yellow = 0;
            settings.black = 0.25F;

            Directory.CreateDirectory(settings.outputDirectory);

            CreateOcadFiles(TestUtil.GetTestFile("controller\\marymoor4.ppen"), settings, new CourseAppearance(),
                                        new string[3] { TestUtil.GetTestFile("controller\\ocad_create2\\Course 3.ocd"),
                                                                 TestUtil.GetTestFile("controller\\ocad_create2\\Course 4G.ocd"),
                                                                 TestUtil.GetTestFile("controller\\ocad_create2\\All controls.ocd")},
                                        new string[3] { TestUtil.GetTestFile("controller\\ocad_create2\\Course 3_expected.txt"),
                                                                 TestUtil.GetTestFile("controller\\ocad_create2\\Course 4G_expected.txt"),
                                                                 TestUtil.GetTestFile("controller\\ocad_create2\\All controls_expected.txt")});
        }

        [TestMethod]
        public void OcadCreation3()
        {
            OcadCreationSettings settings = new OcadCreationSettings();
            settings.mapDirectory = false;
            settings.fileDirectory = true;
            settings.outputDirectory = TestUtil.GetTestFile("controller\\ocad_create1");  // intentionally wrong!
            settings.CourseIds = new Id<Course>[1] { CourseId(3) };
            settings.version = 6;
            settings.cyan = 0.15F;
            settings.magenta = 0.9F;
            settings.yellow = 0;
            settings.black = 0.25F;

            Directory.CreateDirectory(TestUtil.GetTestFile("controller\\ocad_create3"));

            string outputFile = TestUtil.GetTestFile("controller\\ocad_create3\\Course 3.ocd");
            File.Delete(outputFile);
            Assert.IsFalse(File.Exists(outputFile));
            CreateOcadFiles(TestUtil.GetTestFile("controller\\ocad_create3\\marymoor4.ppen"), settings, new CourseAppearance(),
                new string[1] { TestUtil.GetTestFile("controller\\ocad_create3\\Course 3.ocd")},
                new string[1] { TestUtil.GetTestFile("controller\\ocad_create3\\Course 3_expected.txt")});
            Assert.IsTrue(File.Exists(outputFile));
        }

        [TestMethod]
        public void OcadCreation4()
        {
            OcadCreationSettings settings = new OcadCreationSettings();
            settings.mapDirectory = true;
            settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("controller\\ocad_create1");  // intentionally wrong!
            settings.CourseIds = new Id<Course>[1] { CourseId(3) };
            settings.version = 7;
            settings.cyan = 0.15F;
            settings.magenta = 0.9F;
            settings.yellow = 0;
            settings.black = 0.25F;

            Directory.CreateDirectory(TestUtil.GetTestFile("controller\\ocad_create3"));
            Directory.CreateDirectory(TestUtil.GetTestFile("controller\\ocad_create4"));

            string outputFile = TestUtil.GetTestFile("controller\\Course 3.ocd");
            File.Delete(outputFile);
            Assert.IsFalse(File.Exists(outputFile));
            CreateOcadFiles(TestUtil.GetTestFile("controller\\ocad_create3\\marymoor4.ppen"), settings, new CourseAppearance(),
                new string[1] { TestUtil.GetTestFile("controller\\Course 3.ocd") },
                new string[1] { TestUtil.GetTestFile("controller\\ocad_create4\\Course 3_expected.txt") });
            Assert.IsTrue(File.Exists(outputFile));
            File.Delete(outputFile);
        }

        // Test invalid paths and prefix
        [TestMethod]
        public void OcadCreation5()
        {
            OcadCreationSettings settings = new OcadCreationSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("controller\\ocad_create5");
            settings.filePrefix = "MyEvent/Coolthing";
            settings.CourseIds = new Id<Course>[1] { CourseId(2) };
            settings.version = 8;
            settings.cyan = 0.15F;
            settings.magenta = 0.9F;
            settings.yellow = 0;
            settings.black = 0.25F;

            Directory.CreateDirectory(settings.outputDirectory);

            CreateOcadFiles(TestUtil.GetTestFile("controller\\create_ocad5.ppen"), settings, new CourseAppearance(),
                new string[1] { TestUtil.GetTestFile("controller\\ocad_create5\\MyEvent_Coolthing-A&B_C&D_E_F.ocd") },
                new string[1] { TestUtil.GetTestFile("controller\\ocad_create5\\MyEvent_Coolthing-A&B_C&D_E_F_expected.txt") });
        }

        [TestMethod]
        public void OcadCreation6()
        {
            OcadCreationSettings settings = new OcadCreationSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("controller\\ocad_create6");
            settings.CourseIds = new Id<Course>[1] { CourseId(2) };
            settings.version = 8;

            CourseAppearance appearance = new CourseAppearance();
            appearance.controlCircleSize = 0.75F;  //smaller circles
            appearance.lineWidth = 3F; // thin lines
            appearance.numberHeight = 0.5F; // small numbers.
            appearance.useDefaultPurple = false;

            settings.cyan = appearance.purpleC = 0.32F;
            settings.yellow = appearance.purpleY = 1.00F;
            settings.magenta = appearance.purpleM = 0;
            settings.black = appearance.purpleK = 0.30F;

            Directory.CreateDirectory(settings.outputDirectory);

            CreateOcadFiles(TestUtil.GetTestFile("controller\\marymoor4.ppen"), settings, appearance,
                new string[1] { TestUtil.GetTestFile("controller\\ocad_create6\\Course 2.ocd") },
                new string[1] { TestUtil.GetTestFile("controller\\ocad_create6\\Course 2_expected.txt") });
        }

        [TestMethod]
        public void OcadCreation7() {
            OcadCreationSettings settings = new OcadCreationSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("controller\\ocad_create7");
            settings.CourseIds = new Id<Course>[1] { CourseId(2) };
            settings.version = 8;

            CourseAppearance appearance = new CourseAppearance();
            appearance.controlCircleSize = 1.1F;  //smaller circles
            appearance.lineWidth = 1.1F;
            appearance.numberHeight = 1.2F; // slightly big numbers.
            appearance.numberBold = true;
            appearance.useDefaultPurple = false;

            settings.cyan = appearance.purpleC = 0.00F;
            settings.yellow = appearance.purpleY = 1.00F;
            settings.magenta = appearance.purpleM = 1.00F;
            settings.black = appearance.purpleK = 0.30F;

            Directory.CreateDirectory(settings.outputDirectory);

            CreateOcadFiles(TestUtil.GetTestFile("controller\\marymoor4.ppen"), settings, appearance,
                new string[1] { TestUtil.GetTestFile("controller\\ocad_create7\\Course 2.ocd") },
                new string[1] { TestUtil.GetTestFile("controller\\ocad_create7\\Course 2_expected.txt") });
        }

        // Test overwritting files
        [TestMethod]
        public void OverwritingOcadFiles()
        {
            OcadCreationSettings settings = new OcadCreationSettings();
            settings.mapDirectory = settings.fileDirectory = false;
            settings.outputDirectory = TestUtil.GetTestFile("controller\\ocad_create5");
            settings.filePrefix = "MyEvent/Coolthing";
            settings.CourseIds = new Id<Course>[] { CourseId(4), CourseId(2), CourseId(6) };
            settings.version = 8;
            settings.cyan = 0.15F;
            settings.magenta = 0.9F;
            settings.yellow = 0;
            settings.black = 0.25F;

            EventDB eventDB = controller.GetEventDB();

            // First, create ocad files.
            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\create_ocad5.ppen"), true);
            Assert.IsTrue(success);

            success = controller.CreateOcadFiles(settings);
            Assert.IsTrue(success);

            // Next, see if overwritting files are correct.
            settings.CourseIds = new Id<Course>[] { CourseId(1), CourseId(2), CourseId(3), CourseId(6) };

            List<string> result = controller.OverwritingOcadFiles(settings);
            CollectionAssert.AreEquivalent(new string[] {
                        TestUtil.GetTestFile("controller\\ocad_create5\\MyEvent_Coolthing-A&B_C&D_E_F.ocd"),
                        TestUtil.GetTestFile("controller\\ocad_create5\\MyEvent_Coolthing-Course 5.ocd")
                    }, result);
        }


        [TestMethod]
        public void CanAddTextLine()
        {
            string text, objectName;
            DescriptionLine.TextLineKind textLineKind;
            bool canAdd, enableThisCourse;

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\desctext.ppen"), true);
            Assert.IsTrue(success);

            controller.SelectTab(0);    // All controls.
            controller.SelectDescriptionLine(8);
            canAdd = controller.CanAddTextLine(out text, out textLineKind, out objectName, out enableThisCourse);
            Assert.IsTrue(canAdd);
            Assert.IsFalse(enableThisCourse);
            Assert.IsNull(text);
            Assert.AreEqual(DescriptionLine.TextLineKind.AfterControl, textLineKind);

            controller.SelectTab(0);    // All controls.
            controller.SelectDescriptionLine(0);  //title
            canAdd = controller.CanAddTextLine(out text, out textLineKind, out objectName, out enableThisCourse);
            Assert.IsFalse(canAdd);

            controller.SelectTab(0);    // All controls.
            controller.SelectDescriptionLine(5);  // already has a line
            canAdd = controller.CanAddTextLine(out text, out textLineKind, out objectName, out enableThisCourse);
            Assert.IsTrue(canAdd);
            Assert.IsFalse(enableThisCourse);
            Assert.AreEqual("Beware of frogs!", text);
            Assert.AreEqual(DescriptionLine.TextLineKind.BeforeControl, textLineKind);
            Assert.AreEqual("Control 32", objectName);

            controller.SelectTab(4);    
            controller.SelectDescriptionLine(21);
            canAdd = controller.CanAddTextLine(out text, out textLineKind, out objectName, out enableThisCourse);
            Assert.IsTrue(canAdd);
            Assert.IsTrue(enableThisCourse);
            Assert.AreEqual("All done!", text);
            Assert.AreEqual(DescriptionLine.TextLineKind.AfterControl, textLineKind);
            Assert.AreEqual("Finish", objectName);

            controller.SelectTab(4);
            controller.SelectDescriptionLine(12);
            canAdd = controller.CanAddTextLine(out text, out textLineKind, out objectName, out enableThisCourse);
            Assert.IsTrue(canAdd);
            Assert.IsTrue(enableThisCourse);
            Assert.AreEqual("Course Control 303 before", text);
            Assert.AreEqual(DescriptionLine.TextLineKind.BeforeCourseControl, textLineKind);
            Assert.AreEqual("Control 303", objectName);
        }

        [TestMethod]
        public void AddTextLine()
        {
            EventDB eventDB = controller.GetEventDB();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\desctext.ppen"), true);
            Assert.IsTrue(success);

            controller.SelectTab(0);    // All controls.
            controller.SelectDescriptionLine(8);
            controller.AddTextLine("hello sailor", DescriptionLine.TextLineKind.BeforeControl);
            Assert.AreEqual("hello sailor", eventDB.GetControl(ControlId(7)).descTextBefore);
            Assert.AreEqual(null, eventDB.GetControl(ControlId(7)).descTextAfter);
            CheckHighlightedLines(controller, 8, 8);

            controller.GetUndoMgr().Undo();
            Assert.AreEqual(null, eventDB.GetControl(ControlId(7)).descTextBefore);
            Assert.AreEqual(null, eventDB.GetControl(ControlId(7)).descTextAfter);
            CheckHighlightedLines(controller, -1, -1);

            controller.SelectTab(4);
            controller.SelectDescriptionLine(13);
            controller.AddTextLine("hello there", DescriptionLine.TextLineKind.AfterCourseControl);
            Assert.AreEqual("Control 303 before", eventDB.GetControl(ControlId(18)).descTextBefore);
            Assert.AreEqual("Control 303 after", eventDB.GetControl(ControlId(18)).descTextAfter);
            Assert.AreEqual("Course Control 303 before", eventDB.GetCourseControl(CourseControlId(208)).descTextBefore);
            Assert.AreEqual("hello there", eventDB.GetCourseControl(CourseControlId(208)).descTextAfter);
            CheckHighlightedLines(controller, 14, 14);

            controller.SelectTab(4);
            controller.SelectDescriptionLine(13);
            controller.AddTextLine("", DescriptionLine.TextLineKind.AfterControl);
            Assert.AreEqual("Control 303 before", eventDB.GetControl(ControlId(18)).descTextBefore);
            Assert.AreEqual(null, eventDB.GetControl(ControlId(18)).descTextAfter);
            Assert.AreEqual("Course Control 303 before", eventDB.GetCourseControl(CourseControlId(208)).descTextBefore);
            Assert.AreEqual("hello there", eventDB.GetCourseControl(CourseControlId(208)).descTextAfter);
            CheckHighlightedLines(controller, 13, 13);
        }

        [TestMethod]
        public void DeleteTextLine()
        {
            EventDB eventDB = controller.GetEventDB();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\desctext.ppen"), true);
            Assert.IsTrue(success);

            controller.SelectTab(4);    // All controls.
            controller.SelectDescriptionLine(12);
            Assert.IsTrue(controller.CanDeleteSelection());
            controller.DeleteSelection();
            Assert.AreEqual("Control 303 before", eventDB.GetControl(ControlId(18)).descTextBefore);
            Assert.AreEqual("Control 303 after", eventDB.GetControl(ControlId(18)).descTextAfter);
            Assert.AreEqual(null, eventDB.GetCourseControl(CourseControlId(208)).descTextBefore);
            Assert.AreEqual("Course Control 303 after", eventDB.GetCourseControl(CourseControlId(208)).descTextAfter);

            controller.SelectDescriptionLine(14);
            Assert.IsTrue(controller.CanDeleteSelection());
            controller.DeleteSelection();
            Assert.AreEqual("Control 303 before", eventDB.GetControl(ControlId(18)).descTextBefore);
            Assert.AreEqual(null, eventDB.GetControl(ControlId(18)).descTextAfter);
            Assert.AreEqual(null, eventDB.GetCourseControl(CourseControlId(208)).descTextBefore);
            Assert.AreEqual("Course Control 303 after", eventDB.GetCourseControl(CourseControlId(208)).descTextAfter);

            Assert.AreEqual("Beware of frogs!", eventDB.GetControl(ControlId(4)).descTextBefore);
            controller.SelectTab(0);    // All controls.
            controller.SelectDescriptionLine(5);
            Assert.IsTrue(controller.CanDeleteSelection());
            controller.DeleteSelection();
            Assert.AreEqual(null, eventDB.GetControl(ControlId(4)).descTextBefore);
        }


        [TestMethod]
        public void ChangeTextLine()
        {
            EventDB eventDB = controller.GetEventDB();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\desctext.ppen"), true);
            Assert.IsTrue(success);

            controller.SelectTab(4);    // All controls.
            controller.SelectDescriptionLine(12);
            controller.DescriptionChange(DescriptionControl.ChangeKind.TextLine, 12, 0, "new text");
            Assert.AreEqual("Control 303 before", eventDB.GetControl(ControlId(18)).descTextBefore);
            Assert.AreEqual("Control 303 after", eventDB.GetControl(ControlId(18)).descTextAfter);
            Assert.AreEqual("new text", eventDB.GetCourseControl(CourseControlId(208)).descTextBefore);
            Assert.AreEqual("Course Control 303 after", eventDB.GetCourseControl(CourseControlId(208)).descTextAfter);
            
            controller.SelectDescriptionLine(15);
            controller.DescriptionChange(DescriptionControl.ChangeKind.TextLine, 15, 0, "");
            Assert.AreEqual("Control 303 before", eventDB.GetControl(ControlId(18)).descTextBefore);
            Assert.AreEqual(null, eventDB.GetControl(ControlId(18)).descTextAfter);
            Assert.AreEqual("new text", eventDB.GetCourseControl(CourseControlId(208)).descTextBefore);
            Assert.AreEqual("Course Control 303 after", eventDB.GetCourseControl(CourseControlId(208)).descTextAfter);

            Assert.AreEqual("Beware of frogs!", eventDB.GetControl(ControlId(4)).descTextBefore);
            controller.SelectTab(0);    // All controls.
            controller.SelectDescriptionLine(5);
            controller.DescriptionChange(DescriptionControl.ChangeKind.TextLine, 5, 0, "smelly cat");
            Assert.AreEqual("smelly cat", eventDB.GetControl(ControlId(4)).descTextBefore); 
        }

        [TestMethod]
        public void GetUnusedControls()
        {
            EventDB eventDB = controller.GetEventDB();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\marymoor5.ppen"), true);
            Assert.IsTrue(success);

            List<KeyValuePair<Id<ControlPoint>, string>> result, expected;
            expected = new List<KeyValuePair<Id<ControlPoint>, string>>()
            {
                new KeyValuePair<Id<ControlPoint>, string> (ControlId(83), "Start"),
                new KeyValuePair<Id<ControlPoint>, string> (ControlId(84), "31"),
                new KeyValuePair<Id<ControlPoint>, string> (ControlId(42), "42"),
                new KeyValuePair<Id<ControlPoint>, string> (ControlId(55), "55"),
                new KeyValuePair<Id<ControlPoint>, string> (ControlId(87), "88"),
                new KeyValuePair<Id<ControlPoint>, string> (ControlId(82), "101"),
                new KeyValuePair<Id<ControlPoint>, string> (ControlId(81), "102"),
                new KeyValuePair<Id<ControlPoint>, string> (ControlId(85), "Finish"),
                new KeyValuePair<Id<ControlPoint>, string> (ControlId(86), "Crossing"),
            };

            result = controller.GetUnusedControls();

            CollectionAssert.AreEqual(expected, result);
        }

        [TestMethod]
        public void RemoveControls()
        {
            EventDB eventDB = controller.GetEventDB();

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile("controller\\marymoor5.ppen"), true);
            Assert.IsTrue(success);

            controller.RemoveControls(new List<Id<ControlPoint>> { ControlId(83), ControlId(86), ControlId(87), ControlId(42) });

            Assert.IsFalse(eventDB.IsControlPresent(ControlId(86)));
            Assert.IsFalse(eventDB.IsControlPresent(ControlId(83)));
            Assert.IsFalse(eventDB.IsControlPresent(ControlId(87)));
            Assert.IsFalse(eventDB.IsControlPresent(ControlId(42)));
            Assert.IsTrue(eventDB.IsControlPresent(ControlId(85)));
            Assert.IsTrue(eventDB.IsControlPresent(ControlId(82)));
            Assert.IsTrue(eventDB.IsControlPresent(ControlId(55)));

            List<KeyValuePair<Id<ControlPoint>, string>> result, expected;
            expected = new List<KeyValuePair<Id<ControlPoint>, string>>()
            {
                new KeyValuePair<Id<ControlPoint>, string> (ControlId(84), "31"),
                new KeyValuePair<Id<ControlPoint>, string> (ControlId(55), "55"),
                new KeyValuePair<Id<ControlPoint>, string> (ControlId(82), "101"),
                new KeyValuePair<Id<ControlPoint>, string> (ControlId(81), "102"),
                new KeyValuePair<Id<ControlPoint>, string> (ControlId(85), "Finish"),
            };

            result = controller.GetUnusedControls();

            CollectionAssert.AreEqual(expected, result);
        }

        [TestMethod]
        public void GetDescriptionLanguage()
        {
            EventDB eventDB = controller.GetEventDB();
            UndoMgr undoMgr = controller.GetUndoMgr();

            undoMgr.BeginCommand(444, "change language");
            Event ev = eventDB.GetEvent();
            ev = (Event) ev.Clone();
            ev.descriptionLangId = "de";
            eventDB.ChangeEvent(ev);
            undoMgr.EndCommand(444);

            Assert.AreEqual("de", controller.GetDescriptionLanguage());
        }
    }
}

#endif //TEST
