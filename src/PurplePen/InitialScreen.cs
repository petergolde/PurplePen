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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace PurplePen
{
    public partial class InitialScreen : Form
    {
        public InitialScreen()
        {
            InitializeComponent();

            // Only enable the sample event if it exists.
            openSampleRadioButton.Enabled = File.Exists(SampleEventFileName());
        }

        // Create new event was selected.
        public void CreateNewEvent()
        {
            Hide();

            NewEventWizard wizard = new NewEventWizard();
            DialogResult result = wizard.ShowDialog();

            if (result == DialogResult.Cancel) {
                // User cancelled 
                // Go back and show the initial screen again.
                Show();
                return;
            }
            else {
                // Start the UI
                MainFrame mainFrame = new MainFrame();
                Controller controller = new Controller(mainFrame);

                // Create the new event.
                if (controller.InitialNewEvent(wizard.CreateEventInfo)) {
                    // success

                    // show the main frame with the new event.
                    mainFrame.Show();
                    mainFrame.Activate();

                    // The initial screen is over and out.
                    Close();
                    Dispose();
                }
                else {
                    // Failure: Go back and show the initial screen again.
                    mainFrame.Dispose();
                    Show();
                }
            }
        }

        // Open existing event was selected.
        public void OpenExistingEvent()
        {
            Hide();

            MainFrame mainFrame = new MainFrame();
            Controller controller = new Controller(mainFrame);

            string fileName = mainFrame.GetOpenFileName();
            if (fileName == null || ! controller.LoadInitialFile(fileName)) {
                // User cancelled or the file didn't load. 
                // Go back and show the initial screen again.
                mainFrame.Dispose();
                Show();
                return;
            }

            // Start the UI
            mainFrame.Show();
            mainFrame.Activate();

            Close();
            Dispose();      // The initial screen is over and out.
        }

        // Get the file name of the sample event.
        string SampleEventFileName()
        {
            return Util.GetFileInAppDirectory(@"Samples\Sample Event.ppen");
        }

        // Open sample event was selected.
        public void OpenSampleEvent()
        {
            Hide();

            MainFrame mainFrame = new MainFrame();
            Controller controller = new Controller(mainFrame);

            if (!controller.LoadInitialFile(SampleEventFileName())) {
                // File didn't load. 
                // Go back and show the initial screen again.
                mainFrame.Dispose();
                Show();
                return;
            }

            // Start the UI
            mainFrame.Show();
            mainFrame.Activate();

            Close();
            Dispose();      // The initial screen is over and out.
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (openExistingRadioButton.Checked) {
                OpenExistingEvent();
            }
            else if (createNewRadioButton.Checked) {
                CreateNewEvent();
            }
            else if (openSampleRadioButton.Checked) {
                OpenSampleEvent();
            }
            else
                Debug.Fail("how can this happen?");
        }

        private void quitButton_Click(object sender, EventArgs e)
        {
            Close();
            Dispose();
            Application.ExitThread();
        }

        private void InitialScreen_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Shut down if the form is closed for some reason we don't understand.
            if (e.CloseReason != CloseReason.UserClosing)
                Application.Exit();
        }

        private void backgroundPanel_Paint(object sender, PaintEventArgs e)
        {
            GraphicsHelper.DrawPurplePenLogo(e.Graphics, backgroundPanel);
        }
    }
}