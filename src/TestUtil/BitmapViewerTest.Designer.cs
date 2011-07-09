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

namespace TestingUtils
{
    partial class BitmapViewerTest
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BitmapViewerTest));
            this.bitmapViewer1 = new TestingUtils.BitmapViewer();
            this.SuspendLayout();
            // 
            // bitmapViewer1
            // 
            this.bitmapViewer1.Bitmap = null;
            this.bitmapViewer1.CenterPoint = ((System.Drawing.PointF) (resources.GetObject("bitmapViewer1.CenterPoint")));
            this.bitmapViewer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bitmapViewer1.Location = new System.Drawing.Point(0, 0);
            this.bitmapViewer1.Margin = new System.Windows.Forms.Padding(1200, 258, 1200, 258);
            this.bitmapViewer1.Name = "bitmapViewer1";
            this.bitmapViewer1.Size = new System.Drawing.Size(462, 380);
            this.bitmapViewer1.TabIndex = 0;
            this.bitmapViewer1.Viewport = ((System.Drawing.RectangleF) (resources.GetObject("bitmapViewer1.Viewport")));
            this.bitmapViewer1.ZoomFactor = 1F;
            // 
            // BitmapViewerTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(462, 380);
            this.Controls.Add(this.bitmapViewer1);
            this.Margin = new System.Windows.Forms.Padding(60, 28, 60, 28);
            this.Name = "BitmapViewerTest";
            this.Text = "BitmapViewerTest";
            this.ResumeLayout(false);

        }

        #endregion

        private TestingUtils.BitmapViewer bitmapViewer1;
    }
}
