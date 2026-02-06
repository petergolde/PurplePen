using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace TestingUtils
{
    public partial class BitmapCompareDialog2: Form
    {
        public string BaselineFilename;
        public string NewFilename;

        Bitmap bmNew = null, bmBaseline = null, bmDiff = null, bmWhite = null;
        bool nowShowingNew = true;

        public int MaxPixelDifference = 0;


        public BitmapCompareDialog2()
        {
            InitializeComponent();
        }

        private void BitmapCompareDialog2_Shown(object sender, EventArgs e)
        {
            string text = "";

            if (!File.Exists(BaselineFilename))
                text = string.Format("Baseline file '{0}' does not exist", Path.GetFileName(BaselineFilename));
            else {
                bmBaseline = (Bitmap) Image.FromFile(BaselineFilename);
            }

            if (!File.Exists(NewFilename))
                text = string.Format("New file '{0}' does not exist", Path.GetFileName(NewFilename));
            else {
                bmNew = (Bitmap) Image.FromFile(NewFilename);
            }

            if (bmBaseline != null && bmNew != null) {
                if (bmBaseline.Size != bmNew.Size)
                    text = string.Format("Baseline file '{0}' of different size from new bitmap '{1}'", Path.GetFileName(BaselineFilename), Path.GetFileName(NewFilename));
                else
                    text = string.Format("Baseline file '{0}' is different from new bitmap '{1}'", Path.GetFileName(BaselineFilename), Path.GetFileName(NewFilename));
#if TEST
                bmDiff = TestUtil.CompareBitmaps(bmBaseline, bmNew, Color.White, Color.Red, MaxPixelDifference);
                bmWhite = new Bitmap(bmDiff.Width, bmDiff.Height);
                Graphics g = Graphics.FromImage(bmWhite);
                g.Clear(Color.White);
                g.Dispose();
#endif //TEST
            }

            if (bmNew != null) 
                bitmapViewer.Viewport = new RectangleF(0, 0, bmNew.Width, bmNew.Height);

            infoText.Text = text;
            UpdateViewer();
        }

        private void BitmapCompareDialog2_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (bmNew != null) {
                bmNew.Dispose();
                bmNew = null;
            }
            if (bmBaseline != null) {
                bmBaseline.Dispose();
                bmBaseline = null;
            }
            if (bmDiff != null) {
                bmDiff.Dispose();
                bmDiff = null;
            }
            if (bmWhite != null) {
                bmWhite.Dispose();
                bmWhite = null;
            }
        }

        private void UpdateViewer()
        {
            if (nowShowingNew) {
                if (checkBoxRed.Checked) {
                    bitmapViewer.Bitmap = bmWhite; labelNowShowing.Text = "all white";
                }
                else {
                    bitmapViewer.Bitmap = bmNew; labelNowShowing.Text = "new bitmap";
                }
            }
            else {
                if (checkBoxRed.Checked) {
                    bitmapViewer.Bitmap = bmDiff; labelNowShowing.Text = "red where differences are";
                }
                else {
                    bitmapViewer.Bitmap = bmBaseline; labelNowShowing.Text = "baseline bitmap";
                }
            }
        }

        private void SwitchBitmaps()
        {
            nowShowingNew = !nowShowingNew;
            UpdateViewer();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (checkBoxBlink.Checked)
                SwitchBitmaps();
        }

        private void bitmapViewer_MouseDown(object sender, MouseEventArgs e)
        {
            SwitchBitmaps();
        }

        private void buttonFixBitness_Click(object sender, EventArgs e)
        {
            if (TestUtil.HasBitnessSuffix(BaselineFilename)) {
                MessageBox.Show("Already bitness specific.");
                return;
            }

            if (bmNew != null) {
                if (bmBaseline != null) {
                    bmBaseline.Dispose();
                    bmBaseline = null;
                }

                (string filenameNewSave, string filenameBaselineSave) = TestUtil.AddBitnessSuffix(BaselineFilename);

                File.Move(BaselineFilename, filenameBaselineSave);
                bmNew.Save(filenameNewSave, ImageFormat.Png);

                DialogResult = DialogResult.OK;
            }
        }

        private void buttonFixFramework_Click(object sender, EventArgs e)
        {
            if (TestUtil.HasFrameworkSuffix(BaselineFilename)) {
                MessageBox.Show("Already framework specific.");
                return;
            }

            if (bmNew != null) {
                if (bmBaseline != null) {
                    bmBaseline.Dispose();
                    bmBaseline = null;
                }

                (string filenameNewSave, string filenameBaselineSave) = TestUtil.AddFrameworkSuffix(BaselineFilename);

                File.Move(BaselineFilename, filenameBaselineSave);
                bmNew.Save(filenameNewSave, ImageFormat.Png);

                DialogResult = DialogResult.OK;
            }
        }

        private void buttonAccept_Click(object sender, EventArgs e)
        {
            if (bmNew != null) {
                if (bmBaseline != null) {
                    bmBaseline.Dispose();
                    bmBaseline = null;
                }

                bmNew.Save(BaselineFilename, ImageFormat.Png);
                DialogResult = DialogResult.OK;
            }
        }

        private void buttonFail_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void checkBoxRed_CheckedChanged(object sender, EventArgs e)
        {
            UpdateViewer();
        }
    }
}
