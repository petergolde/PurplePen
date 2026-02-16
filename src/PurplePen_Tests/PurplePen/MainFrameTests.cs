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
    public sealed class MainFrameTests: IDisposable
    {
        MainFrame mainFrame;
        Controller controller;

        void LoadInitialFile(string filename)
        {
            mainFrame = new MainFrame();
            controller = new Controller(mainFrame);

            bool success = controller.LoadInitialFile(TestUtil.GetTestFile(filename), true);
            Assert.IsTrue(success);

            // Start the UI
            mainFrame.Show();
        }

        private void Dispose(bool disposing)
        {
            if (disposing) {
                mainFrame?.Dispose();
                mainFrame = null;
                controller?.Dispose();
                controller = null;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
        }


        void CloseMainFrame()
        {
            mainFrame.Dispose();
            mainFrame = null;
        }

        [TestMethod]
        public void WrongScale()
        {
            // Map sure the map scale is correct upon load of the map. The recorded map scale is wrong.
            LoadInitialFile("mainframe\\wrongscale.coursescribe");
            Application.DoEvents();
            Application.RaiseIdle(EventArgs.Empty);

            Event ev = controller.GetEventDB().GetEvent();
            Assert.AreEqual(15000, ev.mapScale);
            Assert.IsTrue(controller.IsDirty);

            CloseMainFrame();
        }
    }
}

#endif //TEST
