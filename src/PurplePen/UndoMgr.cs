/* Copyright (c) 2006-2007, Peter Golde
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

using System;
using System.Collections.Generic;
using System.IO;

namespace PurplePen
{
    /// <summary>
    /// The UndoMgr handle Undo and Redo support, support for
    /// undoing the current command (rollback) in case of an exception,
    /// and the dirty bit.
    /// </summary>
    public class UndoMgr
    {
        int maxCommands;        // maximum number of command in the undoable list.

        // In the list of commands, index lastExecuted is the most
        // recent command. If no undo has recently be done, this will be zero.
        List<Command> commands = new List<Command>();
        int lastExecuted;

        Command currentCommand;  // The current command being executed, if any (else null).
        int currentCookie;       // Cookie for the current command.

        public UndoMgr(int maxCommands)
        {
            this.maxCommands = maxCommands;
            Clear();
        }

        /// <summary>
        /// Is the state of the document clean or dirty? The document is 
        /// clean immediately after the UndoMgr is created, cleared, or after
        /// a MarkClean method is called, or when we undo/redo to such a point.
        /// </summary>
        public bool IsDirty
        {
            get
            {
                if (commands.Count > 0 && lastExecuted < commands.Count)
                    return commands[lastExecuted].isDirty;
                else
                    return true;
            }
        }

        /// <summary>
        /// Marks the state of the document as clean. IsDirty will return
        /// false. If we undo back to this state, the document is clean again.
        /// Typically used after saving the document.
        /// </summary>
        public void MarkClean()
        {
            if (CommandInProgress)
                throw new ApplicationException("Cannot mark as clean when inside a command.");

            // All other command must make the document dirty.
            foreach (Command c in commands)
                c.isDirty = true;

            // Last command made the document clean.
            if (commands.Count > 0)
                commands[lastExecuted].isDirty = false;
        }


        /// <summary>
        /// Clear the state of the undo manager. Typically used when a new document
        /// is loaded.
        /// </summary>
        public void Clear()
        {
            if (CommandInProgress)
                throw new ApplicationException(string.Format("Command with cookie {0} is already in progress.", currentCookie));
            commands.Clear();

            // We place a special "start command" that can't be undone in, to hold the dirty
            // bit.
            Command startCommand = new Command(null);
            startCommand.isDirty = false;
            commands.Add(startCommand);
            lastExecuted = 0;
        }

        /// <summary>
        /// Begin a command with the given user-visible name.
        /// The cookie is an internal identifier just to allow
        /// checking that each EndCommand is paired correctly.
        /// </summary>
        public void BeginCommand(int cookie, string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            if (CommandInProgress)
                throw new ApplicationException(string.Format("Command with cookie {0} is already in progress.", currentCookie));

            currentCookie = cookie;
            currentCommand = new Command(name);
        }

        /// <summary>
        /// End a command. The cookie must be the same as passed to BeginCommand. Commands
        /// cannot be nested.
        /// </summary>
        public void EndCommand(int cookie)
        {
            if (!CommandInProgress)
                throw new ApplicationException("EndCommand called while no command is in progress.");
            if (currentCookie != cookie)
                throw new ApplicationException(string.Format("Wrong cookie -- Command with cookie {0} is in progress.", currentCookie));

            // Get back to no command in progress state.
            Command recorded = currentCommand;
            currentCommand = null;
            currentCookie = -1;

            // If the command is empty, then discard it.
            if (recorded.Empty)
                return;

            // If the command has just non-persistent action, merge it. If no commands
            // exist to merge it with, discard it.
            if (recorded.Nonpersistent) {
                if (commands.Count > 0 && lastExecuted < commands.Count)
                    commands[lastExecuted].MergeCommand(recorded);
                return;
            }

            // We have a command that modifies persistant state.
            // First, discard all commands previous undone, so we can't redo them.
            if (commands.Count > 0 && lastExecuted > 0) {
                commands.RemoveRange(0, lastExecuted);
                lastExecuted = 0;
            }

            // Add the command to the front.
            commands.Insert(0, recorded);
            lastExecuted = 0;

            // Limit the length of the undo stack.
            if (commands.Count > maxCommands)
                commands.RemoveRange(maxCommands, commands.Count - maxCommands);
        }

        /// <summary>
        /// Record an action. Persistant action must be recorded inside a command; non-persistant
        /// action can be recorded either inside or outside a command.
        /// </summary>
        public void RecordAction(UndoableAction action)
        {
            if (!action.Nonpersistent && !CommandInProgress)
                throw new ApplicationException("Cannot record a persistent action outside of a command.");

            if (CommandInProgress) {
                // Add the action to the current command.
                currentCommand.AddAction(action);
            }
            else if (commands.Count > 0 && lastExecuted < commands.Count) {
                // Adding a non-persistant action to the last thing done.
                commands[lastExecuted].AddAction(action);
            }
        }

        /// <summary>
        /// Is a command in progress?
        /// </summary>
        public bool CommandInProgress
        {
            get { return currentCommand != null;  }
        }

        /// <summary>
        /// Name of the command in progress.
        /// </summary>
        public string CommandInProgressName
        {
            get {
                if (!CommandInProgress)
                    throw new ApplicationException("No command in progress.");

                return (currentCommand.name);
            }
        }

        /// <summary>
        /// Rollback the current command that is in progress, and get back to 
        /// a state where no command is in progress.
        /// </summary>
        public void Rollback()
        {
            if (! CommandInProgress)
                throw new ApplicationException("Rollback called while no command is in progress.");

            currentCommand.Undo();
            currentCommand = null;
            currentCookie = -1;
        }

        /// <summary>
        /// Can an Undo be done?
        /// </summary>
        public bool CanUndo
        {
            get {
                if (CommandInProgress)
                    return false;

                // We can't undo the start command, which is indicated by a null name.
                if (commands.Count > 0 && lastExecuted < commands.Count)
                    return commands[lastExecuted].name != null;
                else
                    return false;
            }
        }

        /// <summary>
        /// Name of the next command to undo.
        /// </summary>
        public string UndoName
        {
            get {
                if (!CanUndo)
                    throw new ApplicationException(string.Format("Can't undo in this state.", currentCookie));

                return commands[lastExecuted].name;
            }
        }

        /// <summary>
        /// Undo the last command.
        /// </summary>
        public void Undo()
        {
            if (! CanUndo)
                throw new ApplicationException(string.Format("Can't undo in this state.", currentCookie));

            commands[lastExecuted].Undo();
            ++lastExecuted;
        }

        /// <summary>
        /// Can an Redo be done?
        /// </summary>
        public bool CanRedo
        {
            get {
                if (CommandInProgress)
                    return false;

                return (commands.Count > 0 && lastExecuted > 0);
            }
        }

        /// <summary>
        /// Name of the next command to redo.
        /// </summary>
        public string RedoName
        {
            get {
                if (!CanRedo)
                    throw new ApplicationException(string.Format("Can't redo in this state.", currentCookie));

                return commands[lastExecuted - 1].name;
            }
        }

        public void Redo()
        {
            if (! CanRedo)
                throw new ApplicationException(string.Format("Cannot redo in the current state.", currentCookie));

            commands[lastExecuted - 1].Redo();
            --lastExecuted;
        }

        /// <summary>
        /// A command is the user-visible manifestation of something that is 
        /// undoable. Is it made up of one or more actions and has a user-visible
        /// name (e.g., "Remove Control").
        /// </summary>
        class Command
        {
            List<UndoableAction> actions; // Actions in the command from first to last as
                                          // originally done.
            public string name;        // Name of the command.
            public bool isDirty;       // Is the state dirty after this command?

            // Begin a new command.
            public Command(string name)
            {
                this.name = name;
                actions = new List<UndoableAction>();
                isDirty = true;
            }

            // Is the command empty -- no actions?
            public bool Empty {
                get { return actions.Count == 0; }
            }

            // A command is non-persistent if it is empty, or if 
            // all actions in it are non-persistent.
            public bool Nonpersistent
            {
                get
                {
                    bool nonPersistent = true;
                    foreach (UndoableAction action in actions) {
                        if (!action.Nonpersistent)
                            nonPersistent = false;
                    }

                    return nonPersistent;
                }
            }


            // Add a new action to this command.
            public void AddAction(UndoableAction action)
            {
                actions.Add(action);
            }

            // Add all of the actions from another command to the end of this one.
            public void MergeCommand(Command other)
            {
                actions.AddRange(other.actions);
            }

            // Undo all of the actions in this command.
            public void Undo()
            {
                for (int i = actions.Count - 1; i >= 0; --i) {
                    actions[i].Undo();
                }
            }

            // Redo all of the actions in this command.
            public void Redo()
            {
                foreach (UndoableAction action in actions)
                    action.Redo();
            }
        }
    }

    /// <summary>
    /// A base class for undoable actions. One or more persistant undoable
    /// actions make up an undoable command.
    /// </summary>
    public abstract class UndoableAction {
        /// <summary>
        /// A non-persistant is an action that is like changing the selection or
        /// view, that doesn't affect the persistent state. Non-persistant actions don't
        /// change the dirty state. Non-persistant actions can be recorded outside a command
        /// and are never the sole focus of a command itself. If a command consists
        /// entirely of non-persistant actions, it is folded into the previous command.
        /// </summary>
        public virtual bool Nonpersistent {
            get { return false; }
        }
        
        public abstract void Undo();
        public abstract void Redo();
    }

}
