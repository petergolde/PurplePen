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
using System.Drawing;
using System.IO;
using TestingUtils;

using PurplePen.MapView;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PurplePen.Tests
{
    // A "test" ui that can be used instead of the real user interface for running tests.
    // Everything goes through the output member, which is usuall a StringWriter but 
    // change be changed to Console.Out or a file. A few public data members can be used for setting
    // the results to queries.
    sealed class TestUI: IUserInterface, IDisposable
    {
        public Controller controller;
        public SymbolDB symbolDB;
        public TextWriter output = new StringWriter();   // where output goes.

        // These data members change return values.
        public string returnOpenFileName = TestUtil.GetTestFile("sampleevent1.coursescribe");
        public System.Windows.Forms.DialogResult returnQuestion = System.Windows.Forms.DialogResult.None;  // None means return default.

        // This tracks the mouse position and pixel size.
        PointF mouseLocation;
        float pixelSize = 0.1F;

        public static TestUI Create()
        {
            TestUI ui = new TestUI();
            Controller controller = new Controller(ui);
            return ui;
        }

        private void Dispose(bool disposing)
        {
            if (disposing) {
                output?.Dispose();
                output = null;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
        }



        public void Initialize(Controller controller, SymbolDB symbolDB)
        {
            this.controller = controller;
            this.symbolDB = symbolDB;
        }

        public void MouseMoved(float x, float y, float pixelSize)
        {
            this.mouseLocation = new PointF(x, y);
            this.pixelSize = pixelSize;
            controller.MouseMoved(Pane.Map, mouseLocation, pixelSize);
        }

        public MapViewer.DragAction LeftButtonDown(float x, float y, float pixelSize)
        {
            this.mouseLocation = new PointF(x, y);
            this.pixelSize = pixelSize;
            return controller.LeftButtonDown(Pane.Map, mouseLocation, pixelSize);
        }


        public bool GetCurrentLocation(out PointF location, out float pixelSize)
        {
            location = mouseLocation;
            pixelSize = this.pixelSize;
            return true;
        }

        public Size Size
        {
            get
            {
                return new Size(100, 100);
            }
        }

        public int LogicalToDeviceUnits(int value)
        {
            return value;
        }

        public string GetOpenFileName()
        {
            return returnOpenFileName;
        }

        public void ErrorMessage(string message)
        {
            output.WriteLine("ERROR: '{0}'", message);
        }

        public void WarningMessage(string message)
        {
            output.WriteLine("WARNING: '{0}'", message);
        }

        public void InfoMessage(string message)
        {
            output.WriteLine("INFO: '{0}'", message);
        }

        public bool YesNoQuestion(string message, bool yesDefault)
        {
            output.WriteLine("YES/NO QUESTION: '{0}' (default {1})", message, yesDefault ? "yes" : "no");
            bool retVal;

            if (returnQuestion == System.Windows.Forms.DialogResult.None)
                retVal = yesDefault;
            else if (returnQuestion == System.Windows.Forms.DialogResult.Yes)
                retVal = true;
            else
                retVal = false;
            output.WriteLine("  (returned {0})", retVal ? "yes" : "no");
            return retVal;
        }

        public bool OKCancelMessage(string message, bool okDefault)
        {
            output.WriteLine("OK/CANCEL MESSAGE: '{0}' (default {1})", message, okDefault ? "ok" : "cancel");
            bool retVal;

            if (returnQuestion == System.Windows.Forms.DialogResult.None)
                retVal = okDefault;
            else if (returnQuestion == System.Windows.Forms.DialogResult.OK)
                retVal = true;
            else
                retVal = false;
            output.WriteLine("  (returned {0})", retVal ? "ok" : "cancel");
            return retVal;
        }

        public System.Windows.Forms.DialogResult YesNoCancelQuestion(string message, bool yesDefault)
        {
            output.WriteLine("YES/NO/CANCEL QUESTION: '{0}' (default {1})", message, yesDefault ? "yes" : "no");
            System.Windows.Forms.DialogResult retVal;

            if (returnQuestion == System.Windows.Forms.DialogResult.None)
                retVal = yesDefault ? System.Windows.Forms.DialogResult.Yes : System.Windows.Forms.DialogResult.No;
            else 
                retVal = returnQuestion;

            output.WriteLine("  (returned {0})", retVal.ToString().ToLower());
            return retVal;
        }

        public System.Windows.Forms.DialogResult MovingSharedControl(string controlCode, string otherCourses)
        {
            output.WriteLine("MOVING SHARED CONTROL QUESTION: '{0}' in '{1}'", controlCode, otherCourses);
            return returnQuestion;
        }


        // Strings expected and returned from FindMissingMapFile.
        public string expectedMissingMapFile, newMapFile;
        public MapType newMapType;
        public float newMapDpi, newMapScale;

        public bool FindMissingMapFile(string missingMapFile)
        {
            Assert.AreEqual(expectedMissingMapFile, missingMapFile);
            controller.ChangeMapFile(newMapType, newMapFile, newMapScale, newMapDpi);
            return true;
        }

        public void InitiateMapDragging(PointF initialPos, System.Windows.Forms.MouseButtons buttonEnd)
        {
            throw new NotSupportedException();
        }


        public void ShowProgressDialog(bool knownDuration, Action onCancelPressed = null)
        {
        }

        public bool UpdateProgressDialog(string info, double fractionDone)
        {
            return false;
        }

        public void EndProgressDialog()
        {
        }

        public void ShowTopologyView()
        {
        }
    }
}

#endif //TEST
