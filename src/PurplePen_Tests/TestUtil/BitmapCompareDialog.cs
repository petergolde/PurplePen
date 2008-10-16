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
                bmDiff1 = TestUtil.CompareBitmaps(bmBaseline, NewBitmap, Color.LightPink, Color.Transparent);
                bmDiff2 = TestUtil.CompareBitmaps(bmBaseline, NewBitmap, Color.DarkBlue, Color.Transparent);
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
