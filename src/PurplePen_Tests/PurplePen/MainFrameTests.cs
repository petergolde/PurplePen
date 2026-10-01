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
using System.Threading.Tasks;

namespace PurplePen.Tests
{
    [TestClass]
    public sealed class MainFrameTests: IDisposable
    {
        MainFrame mainFrame;
        Controller controller;

        async Task LoadInitialFile(string filename)
        {
            mainFrame = new MainFrame();
            controller = new Controller(mainFrame);

            bool success = await controller.LoadInitialFile(TestUtil.GetTestFile(filename), true);
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
        public async Task WrongScale()
        {
            // Map sure the map scale is correct upon load of the map. The recorded map scale is wrong.
            await LoadInitialFile("mainframe\\wrongscale.coursescribe");
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
