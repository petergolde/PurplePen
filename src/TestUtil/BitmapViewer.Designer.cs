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
    partial class BitmapViewer
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
            if (disposing && bitmap != null)
                bitmap.Dispose();

            if (disposing && (components != null)) {
                components.Dispose();
            }

            if (disposing && (xformPixelToWorld != null)) {
                xformPixelToWorld.Dispose();
                xformPixelToWorld = null;
            }

            if (disposing && (xformWorldToPixel != null)) {
                xformWorldToPixel.Dispose();
                xformWorldToPixel = null;
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            hScrollBar = new System.Windows.Forms.HScrollBar();
            vScrollBar = new System.Windows.Forms.VScrollBar();
            canvas = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)canvas).BeginInit();
            SuspendLayout();
            // 
            // hScrollBar
            // 
            hScrollBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            hScrollBar.Location = new System.Drawing.Point(0, 331);
            hScrollBar.Minimum = 20;
            hScrollBar.Name = "hScrollBar";
            hScrollBar.Size = new System.Drawing.Size(363, 17);
            hScrollBar.TabIndex = 0;
            hScrollBar.Value = 50;
            hScrollBar.Scroll += hScrollBar_Scroll;
            // 
            // vScrollBar
            // 
            vScrollBar.Dock = System.Windows.Forms.DockStyle.Right;
            vScrollBar.Location = new System.Drawing.Point(346, 0);
            vScrollBar.Name = "vScrollBar";
            vScrollBar.Size = new System.Drawing.Size(17, 331);
            vScrollBar.TabIndex = 1;
            vScrollBar.Scroll += vScrollBar_Scroll;
            // 
            // canvas
            // 
            canvas.BackColor = System.Drawing.SystemColors.AppWorkspace;
            canvas.Dock = System.Windows.Forms.DockStyle.Fill;
            canvas.Location = new System.Drawing.Point(0, 0);
            canvas.Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            canvas.Name = "canvas";
            canvas.Size = new System.Drawing.Size(346, 331);
            canvas.TabIndex = 2;
            canvas.TabStop = false;
            canvas.Paint += canvas_Paint;
            canvas.MouseCaptureChanged += canvas_MouseCaptureChanged;
            canvas.MouseDown += canvas_MouseDown;
            canvas.MouseEnter += canvas_MouseEnter;
            canvas.MouseLeave += canvas_MouseLeave;
            canvas.MouseMove += canvas_MouseMove;
            canvas.MouseUp += canvas_MouseUp;
            canvas.MouseWheel += canvas_MouseWheel;
            canvas.Resize += canvas_Resize;
            // 
            // BitmapViewer
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            Controls.Add(canvas);
            Controls.Add(vScrollBar);
            Controls.Add(hScrollBar);
            Margin = new System.Windows.Forms.Padding(48, 22, 48, 22);
            Name = "BitmapViewer";
            Size = new System.Drawing.Size(363, 348);
            ((System.ComponentModel.ISupportInitialize)canvas).EndInit();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.HScrollBar hScrollBar;
        private System.Windows.Forms.VScrollBar vScrollBar;
        private System.Windows.Forms.PictureBox canvas;
    }
}
