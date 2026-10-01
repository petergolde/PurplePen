using PurplePen.MapModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class InitialScreen : BaseDialog
    {
        public InitialScreen()
        {
            InitializeComponent();

            // Only enable the sample event if it exists.
            openSampleRadioButton.Enabled = File.Exists(SampleEventFileName());

            // Only enable last event if it exists.
            if (File.Exists(UserSettings.Current.LastLoadedFile)) {
                openLastRadioButton.Text = string.Format(MiscText.OpenLastEvent, Path.GetFileNameWithoutExtension(UserSettings.Current.LastLoadedFile));
            }
            else {
                openLastRadioButton.Enabled = false;
                openLastRadioButton.Checked = false;
                openExistingRadioButton.Checked = true;
            }
        }

        // Create new event was selected.
        public async Task CreateNewEvent()
        {
            NewEventWizard wizard = new NewEventWizard();
            DialogResult result = wizard.ShowDialog(this);

            if (result == DialogResult.Cancel) {
                // User cancelled 
                // Go back and show the initial screen again.
                Show();
                Activate();
                return;
            }
            else {
                // Start the UI
                MainFrame mainFrame = new MainFrame();
                Controller controller = new Controller(mainFrame);

                // Create the new event.
                if (await controller.InitialNewEvent(wizard.CreateEventInfo)) {
                    // success

                    // show the main frame with the new event.
                    mainFrame.Show();
                    mainFrame.Activate();

                    // The initial screen is over and out.
                    Dispose();
                }
                else {
                    // Failure: Go back and show the initial screen again.
                    mainFrame.Dispose();
                    Show();
                    Activate();
                }
            }
        }

        // Open existing event was selected.
        public async Task OpenExistingEvent()
        {
            MainFrame mainFrame = new MainFrame();
            Controller controller = new Controller(mainFrame);

            string fileName = mainFrame.GetOpenFileName();
            if (fileName == null || !await controller.LoadInitialFile(fileName, true)) {
                // User cancelled or the file didn't load. 
                // Go back and show the initial screen again.
                mainFrame.Dispose();
                Activate();
                return;
            }

            // Start the UI
            mainFrame.Show();
            mainFrame.Activate();

            Dispose();      // The initial screen is over and out.
        }

        // Open existing event was selected.
        public async Task OpenLastViewedEvent()
        {
            MainFrame mainFrame = new MainFrame();
            Controller controller = new Controller(mainFrame);

            if (!await controller.LoadInitialFile(UserSettings.Current.LastLoadedFile, true)) {
                // User cancelled or the file didn't load. 
                // Go back and show the initial screen again.
                mainFrame.Dispose();
                Activate();
                return;
            }

            // Start the UI
            mainFrame.Show();
            mainFrame.Activate();

            Dispose();      // The initial screen is over and out.
        }

        // Get the file name of the sample event.
        string SampleEventFileName()
        {
            return Util.GetFileInAppDirectory(@"Samples\Sample Event.ppen");
        }

        // Open sample event was selected
        public async Task OpenSampleEvent()
        {
            MainFrame mainFrame = new MainFrame();
            Controller controller = new Controller(mainFrame);

            if (!await controller.LoadInitialFile(SampleEventFileName(), false)) {        // Don't set sample event as the last loaded file.
                // File didn't load. 
                // Go back and show the initial screen again.
                mainFrame.Dispose();
                Activate();
                return;
            }

            // Set the description language to the UI language.
            string langId = Util.CurrentLangName();
            if (controller.HasDescriptionLanguage(langId)) {
                controller.SetDescriptionLanguage(langId);
                controller.MarkClean();
            }

            // Start the UI
            mainFrame.Show();
            mainFrame.Activate();

            Dispose();      // The initial screen is over and out.
        }

        private async void okButton_Click(object sender, EventArgs e)
        {
            if (openExistingRadioButton.Checked) {
                await OpenExistingEvent();
            }
            else if (openLastRadioButton.Checked) {
                await OpenLastViewedEvent();
            }
            else if (createNewRadioButton.Checked) {
                await CreateNewEvent();
            }
            else if (openSampleRadioButton.Checked) {
                await OpenSampleEvent();
            }
            else
                Debug.Fail("how can this happen?");
        }

        private void quitButton_Click(object sender, EventArgs e)
        {
            Quit();
        }

        private void donationLink_Click(object sender, EventArgs e)
        {
            WindowsUtil.GoToWebPage("http://purple-pen.org/donate.htm");
        }

        private void donationLink_Click(object sender, LinkLabelLinkClickedEventArgs e)
        {
            donationLink_Click(sender, EventArgs.Empty);
        }



        private void Quit() {
            Close();
            Dispose();
            Application.ExitThread();
        }

        private void InitialScreen_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Shut down if the form is closed for some reason we don't understand.
            Application.Exit();
        }

        private void backgroundPanel_Paint(object sender, PaintEventArgs e)
        {
            LogoDrawing.DrawPurplePenLogo(new GDIPlus_GraphicsTarget(e.Graphics), backgroundPanel.ClientRectangle);
        }

        private void InitialScreen_Shown(object sender, EventArgs e)
        {
            // Begin check for new version in the background.
            Updater.CheckForUpdates();
        }

    }
}