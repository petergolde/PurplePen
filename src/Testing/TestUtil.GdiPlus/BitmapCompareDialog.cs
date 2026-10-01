using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace TestingUtils
{
    public partial class BitmapCompareDialog: Form
    {
        public Bitmap NewBitmap;
        public string BaselineFilename;

        public BitmapCompareDialog()
        {
            InitializeComponent();
        }

        public int MaxPixelDifference = 0;

        // View some bitmap files.
        public static void ViewFiles(params string[] filenames)
        {
            StringBuilder arguments = new StringBuilder();

            foreach (string s in filenames) {
                arguments.Append("\"");
                arguments.Append(s);
                arguments.Append("\" ");
            }

            System.Diagnostics.Process.Start(@"C:\Program Files\Jasc Software Inc\Paint Shop Pro 7\psp.exe", arguments.ToString());
        }

        private void BitmapCompareDialog_Shown(object sender, EventArgs e)
        {
            Bitmap bmBaseline = null, bmDiff1 = null, bmDiff2 = null;
            string text;

            if (!File.Exists(BaselineFilename))
                text = string.Format("Baseline file '{0}' does not exist", Path.GetFileName(BaselineFilename));
            else {
                bmBaseline = (Bitmap) Image.FromFile(BaselineFilename);
                if (bmBaseline.Size != NewBitmap.Size)
                    text = string.Format("Baseline file '{0}' of different size", Path.GetFileName(BaselineFilename));
                else
                    text = string.Format("Baseline file '{0}' is different", Path.GetFileName(BaselineFilename));
#if TEST
                bmDiff1 = BitmapTestUtil.CompareBitmaps(bmBaseline, NewBitmap, Color.LightPink, Color.Transparent, MaxPixelDifference);
                bmDiff2 = BitmapTestUtil.CompareBitmaps(bmBaseline, NewBitmap, Color.DarkBlue, Color.Transparent, MaxPixelDifference);
#endif //TEST
            }

            infoLabel.Text = text;

            // Initialize the viewers.
            bitmapViewerBaseline.Bitmap = bmBaseline;
            bitmapViewerNew.Bitmap = NewBitmap;
            bitmapViewerDiff1.Bitmap = bmDiff1;
            bitmapViewerDiff2.Bitmap = bmDiff2;
        }

        private void acceptBaselineButton_Click(object sender, EventArgs e)
        {
            if (bitmapViewerBaseline.Bitmap != null) {
                bitmapViewerBaseline.Bitmap.Dispose();
                bitmapViewerBaseline.Bitmap = null;
            }

            NewBitmap.Save(BaselineFilename, ImageFormat.Png);
            DialogResult = DialogResult.OK;
        }

        private void failButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private bool inViewportChange;

        private void bitmapViewer_OnViewportChange(object sender, EventArgs e)
        {
            if (!inViewportChange) {
                inViewportChange = true;

                RectangleF newViewport = ((BitmapViewer) sender).Viewport;

                if (sender != bitmapViewerBaseline)
                    bitmapViewerBaseline.Viewport = newViewport;
                if (sender != bitmapViewerNew)
                    bitmapViewerNew.Viewport = newViewport;
                if (sender != bitmapViewerDiff1)
                    bitmapViewerDiff1.Viewport = newViewport;
                if (sender != bitmapViewerDiff2)
                    bitmapViewerDiff2.Viewport = newViewport;

                inViewportChange = false;
            }
        }

        private void BitmapCompareDialog_Load(object sender, EventArgs e)
        {

        }
    }
}
