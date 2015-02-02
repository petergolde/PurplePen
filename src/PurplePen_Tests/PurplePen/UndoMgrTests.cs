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
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace PurplePen.Tests
{
    using PurplePen.MapModel;

    [TestClass]
    public class UndoMgrTests
    {
        class SimpleAction : UndoableAction
        {
            string name;
            TextWriter writer;

            public SimpleAction(TextWriter writer, string name)
            {
                this.writer = writer;
                this.name = name;
                writer.WriteLine("Executing action '{0}'", name);
            }

            public override void Undo()
            {
                writer.WriteLine("Undoing   action '{0}'", name);
            }

            public override void Redo()
            {
                writer.WriteLine("Redoing   action '{0}'", name);
            }
        }

        class NonpersistentAction : UndoableAction
        {
            string name;
            TextWriter writer;

            public NonpersistentAction(TextWriter writer, string name)
            {
                this.writer = writer;
                this.name = name;
                writer.WriteLine("Executing non-persistent action '{0}'", name);
            }

            public override bool Nonpersistent
            {
                get { return true; }
            }

            public override void Undo()
            {
                writer.WriteLine("Undoing   non-persistent action '{0}'", name);
            }

            public override void Redo()
            {
                writer.WriteLine("Redoing   non-persisten action '{0}'", name);
            }
        }


        [TestMethod]
        public void InitialState()
        {
            // Check the initial state of the undo mgr.
            UndoMgr undomgr = new UndoMgr(5);

            Assert.IsFalse(undomgr.IsDirty);
            Assert.IsFalse(undomgr.CanUndo);
            Assert.IsFalse(undomgr.CanRedo);
            Assert.IsFalse(undomgr.CommandInProgress);
        }

        [TestMethod]
        public void Clear()
        {
            // Check clearing the undo mgr.
            UndoMgr undomgr = new UndoMgr(5);
            StringWriter writer = new StringWriter();

            undomgr.BeginCommand(123, "command1");
            undomgr.RecordAction(new SimpleAction(writer, "action1"));
            undomgr.RecordAction(new SimpleAction(writer, "action2"));
            undomgr.EndCommand(123);

            undomgr.BeginCommand(124, "command2");
            undomgr.RecordAction(new SimpleAction(writer, "action3"));
            undomgr.EndCommand(124);

            undomgr.Undo();
            undomgr.Clear();

            Assert.IsFalse(undomgr.IsDirty);
            Assert.IsFalse(undomgr.CanUndo);
            Assert.IsFalse(undomgr.CanRedo);
            Assert.IsFalse(undomgr.CommandInProgress);
        }

        [TestMethod]
        public void Undo1()
        {
            // Test basic undoing of persistant commands.

            UndoMgr undomgr = new UndoMgr(5);
            StringWriter writer = new StringWriter();

            undomgr.BeginCommand(23, "My first command");
            undomgr.RecordAction(new SimpleAction(writer, "action1"));
            undomgr.EndCommand(23);

            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.AreEqual("My first command", undomgr.UndoName);
            Assert.IsFalse(undomgr.CanRedo);
            Assert.IsFalse(undomgr.CommandInProgress);

            writer.WriteLine("Undo");
            undomgr.Undo();

            Assert.IsFalse(undomgr.IsDirty);
            Assert.IsFalse(undomgr.CanUndo);
            Assert.IsTrue(undomgr.CanRedo);
            Assert.AreEqual("My first command", undomgr.RedoName);
            Assert.IsFalse(undomgr.CommandInProgress);

            undomgr.BeginCommand(27, "My second command");
            undomgr.RecordAction(new SimpleAction(writer, "action2"));
            undomgr.RecordAction(new SimpleAction(writer, "action3"));
            undomgr.EndCommand(27);

            undomgr.BeginCommand(28, "My third command");
            undomgr.RecordAction(new SimpleAction(writer, "action4"));
            undomgr.RecordAction(new SimpleAction(writer, "action5"));
            undomgr.RecordAction(new SimpleAction(writer, "action6"));
            undomgr.EndCommand(28);

            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.AreEqual("My third command", undomgr.UndoName);
            Assert.IsFalse(undomgr.CanRedo);
            Assert.IsFalse(undomgr.CommandInProgress);

            writer.WriteLine("Undo");
            undomgr.Undo();

            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.AreEqual("My second command", undomgr.UndoName);
            Assert.IsTrue(undomgr.CanRedo);
            Assert.AreEqual("My third command", undomgr.RedoName);
            Assert.IsFalse(undomgr.CommandInProgress);

            writer.WriteLine("Undo");
            undomgr.Undo();

            Assert.IsFalse(undomgr.IsDirty);
            Assert.IsFalse(undomgr.CanUndo);
            Assert.IsTrue(undomgr.CanRedo);
            Assert.AreEqual("My second command", undomgr.RedoName);
            Assert.IsFalse(undomgr.CommandInProgress);

            Assert.AreEqual(
@"Executing action 'action1'
Undo
Undoing   action 'action1'
Executing action 'action2'
Executing action 'action3'
Executing action 'action4'
Executing action 'action5'
Executing action 'action6'
Undo
Undoing   action 'action6'
Undoing   action 'action5'
Undoing   action 'action4'
Undo
Undoing   action 'action3'
Undoing   action 'action2'
",
            writer.ToString());
        }

        [TestMethod]
        public void Redo1()
        {
            // Test basic redoing of persistant commands.

            UndoMgr undomgr = new UndoMgr(5);
            StringWriter writer = new StringWriter();

            undomgr.BeginCommand(23, "My first command");
            undomgr.RecordAction(new SimpleAction(writer, "action1"));
            undomgr.EndCommand(23);

            undomgr.BeginCommand(27, "My second command");
            undomgr.RecordAction(new SimpleAction(writer, "action2"));
            undomgr.RecordAction(new SimpleAction(writer, "action3"));
            undomgr.EndCommand(27);

            undomgr.BeginCommand(28, "My third command");
            undomgr.RecordAction(new SimpleAction(writer, "action4"));
            undomgr.RecordAction(new SimpleAction(writer, "action5"));
            undomgr.RecordAction(new SimpleAction(writer, "action6"));
            undomgr.EndCommand(28);

            writer.WriteLine("Undo");
            undomgr.Undo();
            writer.WriteLine("Undo");
            undomgr.Undo();

            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.AreEqual("My first command", undomgr.UndoName);
            Assert.IsTrue(undomgr.CanRedo);
            Assert.AreEqual("My second command", undomgr.RedoName);
            Assert.IsFalse(undomgr.CommandInProgress);

            writer.WriteLine("Redo");
            undomgr.Redo();

            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.AreEqual("My second command", undomgr.UndoName);
            Assert.IsTrue(undomgr.CanRedo);
            Assert.AreEqual("My third command", undomgr.RedoName);
            Assert.IsFalse(undomgr.CommandInProgress);

            writer.WriteLine("Redo");
            undomgr.Redo();

            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.AreEqual("My third command", undomgr.UndoName);
            Assert.IsFalse(undomgr.CanRedo);
            Assert.IsFalse(undomgr.CommandInProgress);

            writer.WriteLine("Undo");
            undomgr.Undo();

            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.AreEqual("My second command", undomgr.UndoName);
            Assert.IsTrue(undomgr.CanRedo);
            Assert.AreEqual("My third command", undomgr.RedoName);
            Assert.IsFalse(undomgr.CommandInProgress);

            undomgr.BeginCommand(28, "My fourth command");
            undomgr.RecordAction(new SimpleAction(writer, "action7"));
            undomgr.EndCommand(28);

            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.AreEqual("My fourth command", undomgr.UndoName);
            Assert.IsFalse(undomgr.CanRedo);
            Assert.IsFalse(undomgr.CommandInProgress);

            writer.WriteLine("Undo");
            undomgr.Undo();

            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.AreEqual("My second command", undomgr.UndoName);
            Assert.IsTrue(undomgr.CanRedo);
            Assert.AreEqual("My fourth command", undomgr.RedoName);
            Assert.IsFalse(undomgr.CommandInProgress);

            writer.WriteLine("Redo");
            undomgr.Redo();

            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.AreEqual("My fourth command", undomgr.UndoName);
            Assert.IsFalse(undomgr.CanRedo);
            Assert.IsFalse(undomgr.CommandInProgress);

            Assert.AreEqual(
@"Executing action 'action1'
Executing action 'action2'
Executing action 'action3'
Executing action 'action4'
Executing action 'action5'
Executing action 'action6'
Undo
Undoing   action 'action6'
Undoing   action 'action5'
Undoing   action 'action4'
Undo
Undoing   action 'action3'
Undoing   action 'action2'
Redo
Redoing   action 'action2'
Redoing   action 'action3'
Redo
Redoing   action 'action4'
Redoing   action 'action5'
Redoing   action 'action6'
Undo
Undoing   action 'action6'
Undoing   action 'action5'
Undoing   action 'action4'
Executing action 'action7'
Undo
Undoing   action 'action7'
Redo
Redoing   action 'action7'
",
            writer.ToString());
        }

        [TestMethod]
        public void Rollback1()
        {
            // Test basic rollback of persistant commands.

            UndoMgr undomgr = new UndoMgr(5);
            StringWriter writer = new StringWriter();

            undomgr.BeginCommand(23, "My first command");
            undomgr.RecordAction(new SimpleAction(writer, "action1"));
            Assert.IsFalse(undomgr.CanUndo);
            Assert.IsFalse(undomgr.CanRedo);
            Assert.IsTrue(undomgr.CommandInProgress);
            Assert.AreEqual("My first command", undomgr.CommandInProgressName);

            writer.WriteLine("Rollback");
            undomgr.Rollback();

            Assert.IsFalse(undomgr.IsDirty);
            Assert.IsFalse(undomgr.CanUndo);
            Assert.IsFalse(undomgr.CanRedo);
            Assert.IsFalse(undomgr.CommandInProgress);

            undomgr.BeginCommand(27, "My second command");
            undomgr.RecordAction(new SimpleAction(writer, "action2"));
            undomgr.RecordAction(new SimpleAction(writer, "action3"));
            undomgr.EndCommand(27);

            undomgr.BeginCommand(28, "My third command");
            undomgr.RecordAction(new SimpleAction(writer, "action4"));
            undomgr.RecordAction(new SimpleAction(writer, "action5"));
            undomgr.RecordAction(new SimpleAction(writer, "action6"));
            undomgr.EndCommand(28);

            writer.WriteLine("Undo");
            undomgr.Undo();

            undomgr.BeginCommand(29, "My fourth command");
            undomgr.RecordAction(new SimpleAction(writer, "action7"));
            undomgr.RecordAction(new SimpleAction(writer, "action8"));

            writer.WriteLine("Rollback");
            undomgr.Rollback();

            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.AreEqual("My second command", undomgr.UndoName);
            Assert.IsTrue(undomgr.CanRedo);
            Assert.AreEqual("My third command", undomgr.RedoName);
            Assert.IsFalse(undomgr.CommandInProgress);

            writer.WriteLine("Undo");
            undomgr.Undo();

            Assert.IsFalse(undomgr.IsDirty);
            Assert.IsFalse(undomgr.CanUndo);
            Assert.IsTrue(undomgr.CanRedo);
            Assert.AreEqual("My second command", undomgr.RedoName);
            Assert.IsFalse(undomgr.CommandInProgress);

            writer.WriteLine("Redo");
            undomgr.Redo();
            writer.WriteLine("Redo");
            undomgr.Redo();

            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.AreEqual("My third command", undomgr.UndoName);
            Assert.IsFalse(undomgr.CanRedo);
            Assert.IsFalse(undomgr.CommandInProgress);

            Assert.AreEqual(
@"Executing action 'action1'
Rollback
Undoing   action 'action1'
Executing action 'action2'
Executing action 'action3'
Executing action 'action4'
Executing action 'action5'
Executing action 'action6'
Undo
Undoing   action 'action6'
Undoing   action 'action5'
Undoing   action 'action4'
Executing action 'action7'
Executing action 'action8'
Rollback
Undoing   action 'action8'
Undoing   action 'action7'
Undo
Undoing   action 'action3'
Undoing   action 'action2'
Redo
Redoing   action 'action2'
Redoing   action 'action3'
Redo
Redoing   action 'action4'
Redoing   action 'action5'
Redoing   action 'action6'
",
            writer.ToString());
        }

        [TestMethod]
        public void Nonpersistent1()
        {
            // Initially recording a non-persistent action or command
            // should preserve initial state.
            UndoMgr undomgr = new UndoMgr(5);
            StringWriter writer = new StringWriter();

            undomgr.RecordAction(new NonpersistentAction(writer, "foo"));
            Assert.IsFalse(undomgr.IsDirty);
            Assert.IsFalse(undomgr.CanUndo);
            Assert.IsFalse(undomgr.CanRedo);
            Assert.IsFalse(undomgr.CommandInProgress);

            undomgr.BeginCommand(17, "My command");
            undomgr.RecordAction(new NonpersistentAction(writer, "foo"));
            undomgr.RecordAction(new NonpersistentAction(writer, "bar"));
            Assert.IsFalse(undomgr.IsDirty);
            Assert.IsFalse(undomgr.CanUndo);
            Assert.IsFalse(undomgr.CanRedo);
            Assert.IsTrue(undomgr.CommandInProgress);
            undomgr.EndCommand(17);

            Assert.IsFalse(undomgr.IsDirty);
            Assert.IsFalse(undomgr.CanUndo);
            Assert.IsFalse(undomgr.CanRedo);
            Assert.IsFalse(undomgr.CommandInProgress);

            Assert.AreEqual(
@"Executing non-persistent action 'foo'
Executing non-persistent action 'foo'
Executing non-persistent action 'bar'
",
            writer.ToString());
        }

        [TestMethod]
        public void MarkClean()
        {
            // Test that marking clean works.
            UndoMgr undomgr = new UndoMgr(5);
            StringWriter writer = new StringWriter();

            Assert.IsFalse(undomgr.IsDirty);
            Assert.IsFalse(undomgr.CanUndo);
            Assert.IsFalse(undomgr.CanRedo);
            Assert.IsFalse(undomgr.CommandInProgress);

            undomgr.BeginCommand(99, "command #1");
            undomgr.RecordAction(new SimpleAction(writer, "action1"));
            undomgr.RecordAction(new SimpleAction(writer, "action2"));
            undomgr.EndCommand(99);

            undomgr.BeginCommand(98, "command #2");
            undomgr.RecordAction(new SimpleAction(writer, "action3"));
            undomgr.RecordAction(new SimpleAction(writer, "action4"));
            undomgr.EndCommand(98);

            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.IsFalse(undomgr.CanRedo);

            undomgr.MarkClean();

            Assert.IsFalse(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.IsFalse(undomgr.CanRedo);

            undomgr.BeginCommand(97, "command #3");
            undomgr.RecordAction(new SimpleAction(writer, "action5"));
            undomgr.RecordAction(new SimpleAction(writer, "action6"));
            undomgr.EndCommand(97);

            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.IsFalse(undomgr.CanRedo);

            undomgr.Undo();

            Assert.IsFalse(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.IsTrue(undomgr.CanRedo);

            undomgr.Undo();
            Assert.IsTrue(undomgr.IsDirty);
            undomgr.Undo();
            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsFalse(undomgr.CanUndo);

            undomgr.Redo();
            Assert.IsTrue(undomgr.IsDirty);
            undomgr.Redo();
            Assert.IsFalse(undomgr.IsDirty);

            undomgr.Undo();
            Assert.IsTrue(undomgr.IsDirty);

            undomgr.MarkClean();
            Assert.IsFalse(undomgr.IsDirty);

            undomgr.Redo();
            Assert.IsTrue(undomgr.IsDirty);

            undomgr.Redo();
            Assert.IsTrue(undomgr.IsDirty);
        }

        [TestMethod]
        public void EmptyCommand()
        {
            // Check clearing the undo mgr.
            UndoMgr undomgr = new UndoMgr(5);
            StringWriter writer = new StringWriter();

            undomgr.BeginCommand(123, "command1");
            undomgr.EndCommand(123);

            Assert.IsFalse(undomgr.IsDirty);
            Assert.IsFalse(undomgr.CanUndo);
            Assert.IsFalse(undomgr.CanRedo);
            Assert.IsFalse(undomgr.CommandInProgress);

            undomgr.BeginCommand(124, "command2");
            undomgr.RecordAction(new SimpleAction(writer, "action3"));
            undomgr.RecordAction(new SimpleAction(writer, "action4"));
            undomgr.EndCommand(124);

            undomgr.BeginCommand(125, "command3");
            undomgr.RecordAction(new SimpleAction(writer, "action5"));
            undomgr.RecordAction(new SimpleAction(writer, "action6"));
            undomgr.EndCommand(125);

            undomgr.Undo();

            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.AreEqual("command2", undomgr.UndoName);
            Assert.IsTrue(undomgr.CanRedo);
            Assert.AreEqual("command3", undomgr.RedoName);
            Assert.IsFalse(undomgr.CommandInProgress);

            undomgr.BeginCommand(126, "command4");
            undomgr.EndCommand(126);

            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.AreEqual("command2", undomgr.UndoName);
            Assert.IsTrue(undomgr.CanRedo);
            Assert.AreEqual("command3", undomgr.RedoName);
            Assert.IsFalse(undomgr.CommandInProgress);
        }

        [TestMethod]
        public void FullQueue()
        {
            UndoMgr undomgr = new UndoMgr(3);
            StringWriter writer = new StringWriter();

            undomgr.BeginCommand(124, "command1");
            undomgr.RecordAction(new SimpleAction(writer, "action1"));
            undomgr.RecordAction(new SimpleAction(writer, "action2"));
            undomgr.EndCommand(124);

            undomgr.BeginCommand(125, "command2");
            undomgr.RecordAction(new SimpleAction(writer, "action3"));
            undomgr.RecordAction(new SimpleAction(writer, "action4"));
            undomgr.EndCommand(125);

            undomgr.BeginCommand(126, "command3");
            undomgr.RecordAction(new SimpleAction(writer, "action5"));
            undomgr.RecordAction(new SimpleAction(writer, "action6"));
            undomgr.EndCommand(126);

            undomgr.BeginCommand(127, "command4");
            undomgr.RecordAction(new SimpleAction(writer, "action7"));
            undomgr.RecordAction(new SimpleAction(writer, "action8"));
            undomgr.EndCommand(127);

            writer.WriteLine("Undo");
            undomgr.Undo();

            writer.WriteLine("Undo");
            undomgr.Undo();

            writer.WriteLine("Undo");
            undomgr.Undo();

            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsFalse(undomgr.CanUndo);
            Assert.IsTrue(undomgr.CanRedo);
            Assert.AreEqual("command2", undomgr.RedoName);

            writer.WriteLine("Redo");
            undomgr.Redo();

            writer.WriteLine("Redo");
            undomgr.Redo();

            writer.WriteLine("Redo");
            undomgr.Redo();

            Assert.AreEqual("command4", undomgr.UndoName);
            Assert.IsTrue(undomgr.IsDirty);
            Assert.IsTrue(undomgr.CanUndo);
            Assert.IsFalse(undomgr.CanRedo);

            Assert.AreEqual(
@"Executing action 'action1'
Executing action 'action2'
Executing action 'action3'
Executing action 'action4'
Executing action 'action5'
Executing action 'action6'
Executing action 'action7'
Executing action 'action8'
Undo
Undoing   action 'action8'
Undoing   action 'action7'
Undo
Undoing   action 'action6'
Undoing   action 'action5'
Undo
Undoing   action 'action4'
Undoing   action 'action3'
Redo
Redoing   action 'action3'
Redoing   action 'action4'
Redo
Redoing   action 'action5'
Redoing   action 'action6'
Redo
Redoing   action 'action7'
Redoing   action 'action8'
",
            writer.ToString());
        }

        [TestMethod]
        public void Nonpersistent2()
        {
            // Initially recording a non-persistent action or command
            // should preserve initial state.
            UndoMgr undomgr = new UndoMgr(5);
            StringWriter writer = new StringWriter();

            undomgr.BeginCommand(17, "Command1");
            undomgr.RecordAction(new NonpersistentAction(writer, "action1"));
            undomgr.RecordAction(new SimpleAction(writer, "action2"));
            undomgr.RecordAction(new NonpersistentAction(writer, "action3"));
            undomgr.EndCommand(17);
            undomgr.RecordAction(new NonpersistentAction(writer, "action4"));
            undomgr.RecordAction(new NonpersistentAction(writer, "action5"));

            writer.WriteLine("Undo");
            undomgr.Undo();

            writer.WriteLine("Redo");
            undomgr.Redo();

            writer.WriteLine("Undo");
            undomgr.Undo();

            writer.WriteLine("Redo");
            undomgr.Redo();

            undomgr.RecordAction(new NonpersistentAction(writer, "action6"));

            undomgr.BeginCommand(18, "Command2");
            undomgr.RecordAction(new NonpersistentAction(writer, "action7"));
            undomgr.RecordAction(new SimpleAction(writer, "action8"));
            undomgr.RecordAction(new NonpersistentAction(writer, "action9"));
            undomgr.EndCommand(18);

            writer.WriteLine("Undo");
            undomgr.Undo();

            writer.WriteLine("Redo");
            undomgr.Redo();


            Assert.AreEqual(
@"Executing non-persistent action 'action1'
Executing action 'action2'
Executing non-persistent action 'action3'
Executing non-persistent action 'action4'
Executing non-persistent action 'action5'
Undo
Undoing   non-persistent action 'action5'
Undoing   non-persistent action 'action4'
Undoing   non-persistent action 'action3'
Undoing   action 'action2'
Undoing   non-persistent action 'action1'
Redo
Redoing   non-persisten action 'action1'
Redoing   action 'action2'
Redoing   non-persisten action 'action3'
Redoing   non-persisten action 'action4'
Redoing   non-persisten action 'action5'
Undo
Undoing   non-persistent action 'action5'
Undoing   non-persistent action 'action4'
Undoing   non-persistent action 'action3'
Undoing   action 'action2'
Undoing   non-persistent action 'action1'
Redo
Redoing   non-persisten action 'action1'
Redoing   action 'action2'
Redoing   non-persisten action 'action3'
Redoing   non-persisten action 'action4'
Redoing   non-persisten action 'action5'
Executing non-persistent action 'action6'
Executing non-persistent action 'action7'
Executing action 'action8'
Executing non-persistent action 'action9'
Undo
Undoing   non-persistent action 'action9'
Undoing   action 'action8'
Undoing   non-persistent action 'action7'
Redo
Redoing   non-persisten action 'action7'
Redoing   action 'action8'
Redoing   non-persisten action 'action9'
",
            writer.ToString());
        }
    }
}

#endif //TEST
