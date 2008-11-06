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
using System.IO;

namespace PurplePen
{
    public partial class NewEventWizard: Form
    {
        // The pages of the wizard.
        IWizardPage[] pages;
        NewEventTitle eventTitlePage;
        NewEventMapFile mapFilePage;
        NewEventBitmapScale bitmapScalePage;
        NewEventPrintScale printScalePage;
        NewEventDirectory directoryPage;
        NewEventNumbering numberingPage;
        NewEventFinal finalPage;

        // Which page is active?
        int activePage = -1;

        internal MapType mapType;
        internal float mapScale;
        internal float defaultPrintScale;
        internal string mapFileName;

        Controller.CreateEventInfo createEventInfo;

        // Get the create event info used to create the event.
        internal Controller.CreateEventInfo CreateEventInfo
        {
            get
            {
                return createEventInfo;
            }
        }
       
        public NewEventWizard()
        {
            InitializeComponent();
            InitializePages();
        }

        // Initialize all the pages of the wizard.
        public void InitializePages()
        {
            eventTitlePage = new NewEventTitle();
            mapFilePage = new NewEventMapFile();
            bitmapScalePage = new NewEventBitmapScale();
            printScalePage = new NewEventPrintScale();
            directoryPage = new NewEventDirectory();
            numberingPage = new NewEventNumbering();
            finalPage = new NewEventFinal();

            pages = new IWizardPage[] { eventTitlePage, mapFilePage, bitmapScalePage, printScalePage, directoryPage, numberingPage, finalPage};
        }

        private void NewEventWizard_Load(object sender, EventArgs e)
        {
            Application.Idle += Application_Idle;

            ActivatePage(0);
        }

        private void NewEventWizard_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Idle -= Application_Idle;
        }


        private void NewEventWizard_Shown(object sender, EventArgs e)
        {
            ((Control) pages[activePage]).Focus();
        }

        // Activate the page with the given index.
        void ActivatePage(int newPage)
        {
            if (newPage == activePage)
                return;

            if (activePage >= 0) {
                // Remove the existing page from the wizard.
                Controls.Remove((Control) pages[activePage]);
            }

            // Add the new page to the wizard.
            Control newWizardPage = (Control) (pages[newPage]);
            newWizardPage.Visible = false;
            Controls.Add(newWizardPage);
            newWizardPage.BringToFront();
            newWizardPage.Dock = DockStyle.Fill;
            newWizardPage.Visible = true;
            newWizardPage.Focus();
            activePage = newPage;

            IWizardPage newWizardPageIface = (IWizardPage) newWizardPage;
            this.titleLabel.Text = newWizardPageIface.Title;
        }

        // The "Next" button (or "Finish" button) is clicked to move to the next item.
        private void nextButton_Click(object sender, EventArgs e)
        {
            if (activePage + 1 >= pages.Length) {
                // at the end.
                string errorMessageText;
                SetCreateInfo();

                // See if we are likely to be able to create the event file.
                bool success = TryCreateEvent(out errorMessageText);
                if (success) {
                    // Yes. Calling code actually does the creation.
                    DialogResult = DialogResult.OK;
                }
                else {
                    // No.
                    finalPage.errorMessage.Text = errorMessageText;
                    finalPage.errorDisplayPanel.Visible = true;
                }
            }
            else {
                int nextPage = activePage + 1;

                // Skip the bitmap scale page if the map type is not bitmap.
                if (pages[nextPage] == bitmapScalePage && mapType != MapType.Bitmap)
                    nextPage += 1;

                ActivatePage(nextPage);
                if (activePage == pages.Length - 1) {
                    finalPage.eventFileName.Text = GetEventFullPath();
                    finalPage.errorDisplayPanel.Visible = false;
                }
            }
        }

        // The 'Back" button was clicked.
        private void backButton_Click(object sender, EventArgs e)
        {
            if (activePage > 0) {
                int nextPage = activePage - 1;

                // Skip the bitmap scale page if the map type is not bitmap.
                if (pages[nextPage] == bitmapScalePage && mapType != MapType.Bitmap)
                    nextPage -= 1;

                ActivatePage(nextPage);
            }
        }

        // Idle time. Update the buttons.
        private void Application_Idle(object sender, EventArgs e)
        {
            try {
                backButton.Enabled = (activePage > 0);
                nextButton.Enabled = pages[activePage].CanProceed;

                if (activePage == pages.Length - 1) {
                    nextButton.Text = MiscText.FinishButtonText;
                }
                else {
                    nextButton.Text = MiscText.NextButtonText;
                }
            }
            catch (Exception excep) {
                // Unlike other Winforms events, the Application_Idle event does not give the cool dialog when an exception happens (which allows
                // the user to recover. [Bug 1688896]
                Application.OnThreadException(excep);
            }
        }

        // Get the full path of the event name, given the various pages.
        string GetEventFullPath()
        {
            return Path.Combine(directoryPage.GetEventDirectory(mapFilePage.mapFileNameTextBox.Text), eventTitlePage.GetEventFileName());
        }

        // Set the creation info so we are ready to create.
        private void SetCreateInfo()
        {
            createEventInfo.title = eventTitlePage.titleText.Text;
            createEventInfo.eventFileName = GetEventFullPath();
            createEventInfo.mapType = mapType;
            createEventInfo.mapFileName = mapFileName;
            createEventInfo.scale = mapScale;
            createEventInfo.allControlsPrintScale = defaultPrintScale;
            createEventInfo.dpi = bitmapScalePage.dpi;
            createEventInfo.firstCode = (int) numberingPage.startingCodeNumericUpDown.Value;
            createEventInfo.disallowInvertibleCodes = numberingPage.disallowInvertibleCheckBox.Checked;
            createEventInfo.descriptionLangId = "en";  // UNDONE: how to set the initial description language?
        }

        // See if it is OK to try creating the event information. Check that no existing file is there, and also try to 
        // create an empty file. This covers almost all the error scenarios. Other errors will be reported, but the wizard will
        // be already gone.
        private bool TryCreateEvent(out string errorMessageText)
        {
            // If the directory doesn't exist, try to create it.
            string directory = Path.GetDirectoryName(createEventInfo.eventFileName);
            if (!Directory.Exists(directory)) {
                try {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception e) {
                    errorMessageText = string.Format(MiscText.CannotCreateDirectory, directory) + "\r\n" + e.Message;
                    return false;
                }
            }

            // If the file already exists, we don't allow overwriting. Tell user to create a different name.
            if (File.Exists(createEventInfo.eventFileName)) {
                errorMessageText = string.Format(MiscText.FileAlreadyExists, Path.GetFileName(createEventInfo.eventFileName));
                return false;
            }

            // Create a one byte file to see if we can.
            byte[] bytes = new byte[1] {0};
            try {
                File.WriteAllBytes(createEventInfo.eventFileName, bytes);
            }
            catch (Exception e) {
                errorMessageText = string.Format(MiscText.CannotCreateFile, Path.GetFileName(createEventInfo.eventFileName)) + "\r\n" + e.Message;
                return false;
            }

            // Everything looks OK.
            errorMessageText = "";
            return true;
        }

        // This interface is implemented by each page.
        public interface IWizardPage
        {
            bool CanProceed {get; }
            string Title { get;}
        }
    }
}