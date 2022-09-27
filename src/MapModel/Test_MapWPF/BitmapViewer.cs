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
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace TestingUtils
{
    public partial class BitmapViewer: UserControl
    {
        private Bitmap bitmap;

        private PointF centerPoint = new PointF(0, 0);			// center point in world coordinates.
        private float zoom = 1.0F;							    // zoom, 1.0 == one pixel to one pixel
        private Matrix xformWorldToPixel;						// transformation world->pixel coord
        private Matrix xformPixelToWorld;						// transformation pixel->world coord
        private RectangleF viewport;							// visible area in world coordinates

        // Mouse location (in world coords)
        PointF mouseLocation;
        bool mouseInView;  // if false, mouseLocation is invalid.

        // Dragging state
        bool dragScrollingInProgress = false;					// Are we dragging the map around?
        MouseButtons endDragScrollButton;                       // Which mouse button ends drag scrolling.
        Point lastDragScrollPoint;								// last point we dragged to

        public delegate void PointerEventHandler(object sender, bool inViewport, PointF location);
        public event EventHandler OnViewportChange;
        public event PointerEventHandler OnPointerMove;

        public BitmapViewer()
        {
            InitializeComponent();
        }

        public Bitmap Bitmap
        {
            get { return bitmap; }
            set { bitmap = value; ViewportChanged(); }
        }

        #region Transformation between world/view coordinates
        // Calculate the world->client matrix and viewport that corresponds to the area we are viewing,
        // based on the size of the control and the centerPoint and zoom.  The returned viewport has
        // Top and Bottom reversed, to correspond to the convention that Top <= Bottom.
        void CalculateWorldTransform()
        {
            // Get size, midpoint of the casvas in pixel coords.
            Size sizeInPixels = canvas.ClientSize;
            PointF midpoint = new PointF(sizeInPixels.Width / 2.0F, sizeInPixels.Height / 2.0F);

            // Calculate the world->canvas transform.
            Matrix matrix = new Matrix();
            float scaleFactor = zoom;
            matrix.Translate(midpoint.X, midpoint.Y);
            matrix.Scale(scaleFactor, scaleFactor);  
            matrix.Translate(-centerPoint.X, -centerPoint.Y);
            xformWorldToPixel = matrix.Clone();

            // Invert it to get the window->world transform.
            matrix.Invert();
            xformPixelToWorld = matrix;

            // Calculate the viewport in world coordinates.
            PointF[] pts = {
											new PointF(0.0F, 0.0F),
											new PointF((float) sizeInPixels.Width, (float) sizeInPixels.Height) };
            xformPixelToWorld.TransformPoints(pts);
            viewport = new RectangleF(pts[0].X, pts[0].Y, pts[1].X - pts[0].X, pts[1].Y - pts[0].Y);
        }

        public Matrix GetWorldToPixelXform()
        {
            return xformWorldToPixel;
        }

        // Transform one point from world to pixel coordinates. 
        public PointF WorldToPixel(PointF ptWorld)
        {
            PointF[] pts = { ptWorld };
            xformWorldToPixel.TransformPoints(pts);
            return pts[0];
        }

        // Transform one point from pixel to world coordinates. 
        public PointF PixelToWorld(PointF ptPixel)
        {
            PointF[] pts = new PointF[] { ptPixel };
            xformPixelToWorld.TransformPoints(pts);
            return pts[0];
        }

        // Transform distance from world to pixel coordinates. 
        public float WorldToPixelDistance(float distWorld)
        {
            PointF[] pts = new PointF[] { new PointF(distWorld, 0) };
            xformWorldToPixel.TransformVectors(pts);
            return pts[0].X;
        }

        // Transform distance from pixe to world coordinates. 
        public float PixelToWorldDistance(float distPixel)
        {
            PointF[] pts = new PointF[] { new PointF(distPixel, 0) };
            xformPixelToWorld.TransformVectors(pts);
            return pts[0].X;
        }

        // Zoom, keeping the given point at the same location in pixel coordinates.
        public void ZoomAroundPoint(PointF zoomPtWorld, float newZoom)
        {
            PointF zoomPtPixel = WorldToPixel(zoomPtWorld);
            ZoomFactor = newZoom;
            PointF zoomPtWorldNew = PixelToWorld(zoomPtPixel);
            PointF centerPtWorld = CenterPoint;
            centerPtWorld.X += zoomPtWorld.X - zoomPtWorldNew.X;
            centerPtWorld.Y += zoomPtWorld.Y - zoomPtWorldNew.Y;
            CenterPoint = centerPtWorld;
        }

        #endregion

        #region Property accessors
        public float ZoomFactor
        {
            get { return zoom; }
            set
            {
                // clamp zoom to a reasonable value.
                if (value < 0.03F)
                    value = 0.03F;
                if (value > 100.0F)
                    value = 100.0F;

                if (zoom != value) {
                    zoom = value;
                    ViewportChanged();
                }
            }
        }

        public PointF CenterPoint
        {
            get { return centerPoint; }
            set
            {
                if (centerPoint != value) {
                    centerPoint = value;
                    ViewportChanged();
                }
            }
        }

        public PointF PointerLocation
        {
            get
            {
                return mouseLocation;
            }
        }

        public bool PointerInView
        {
            get { return mouseInView; }
        }

        public RectangleF Viewport
        {
            get
            {
                return viewport;
            }

            set
            {
                float oldZoom = zoom;
                PointF oldCenterPoint = centerPoint;
                double newHorizZoom = viewport.Width / value.Width * zoom;
                double newVertZoom = viewport.Height / value.Height * zoom;

                zoom = (float) Math.Min(newHorizZoom, newVertZoom);
                centerPoint = new PointF((value.Left + value.Right) / 2.0F, (value.Top + value.Bottom) / 2.0F);
                if (zoom != oldZoom || centerPoint != oldCenterPoint)
                    ViewportChanged();
            }
        }

        #endregion Property Accessors

        #region Change handling
        // Indicates that the viewport was changed, and recalculates
        // everything based on ClientSize, Zoom, and CenterPoint.
        void ViewportChanged()
        {
            CalculateWorldTransform();
            canvas.Invalidate();

            UpdateScrollBars();

            if (OnViewportChange != null)
                OnViewportChange(this, EventArgs.Empty);
        }

        void UpdateScrollBars()
        {
            if (bitmap != null) {
                hScrollBar.Minimum = 0;
                hScrollBar.Maximum = bitmap.Width + (int) Math.Round(viewport.Width);
                Debug.Assert(hScrollBar.Maximum >= 0);
                int centerX = (int) Math.Round(CenterPoint.X);
                if (centerX < 0)
                    centerX = 0;
                if (centerX > bitmap.Width)
                    centerX = bitmap.Width;
                hScrollBar.Value = centerX;
                hScrollBar.LargeChange = Math.Min(hScrollBar.Maximum, (int) Math.Round(viewport.Width));
                hScrollBar.SmallChange = hScrollBar.LargeChange / 10;

                vScrollBar.Minimum = 0;
                vScrollBar.Maximum = bitmap.Height + (int) Math.Round(viewport.Height);
                Debug.Assert(vScrollBar.Maximum >= 0);
                int centerY = (int) Math.Round(CenterPoint.Y);
                if (centerY < 0)
                    centerY = 0;
                if (centerY > bitmap.Height)
                    centerY = bitmap.Height;
                vScrollBar.Value = centerY;
                vScrollBar.LargeChange = Math.Min(vScrollBar.Maximum, (int) Math.Round(viewport.Height));
                vScrollBar.SmallChange = vScrollBar.LargeChange / 10;
            }
        }

        #endregion Change handling

        #region Drag scrolling (middle button drag, or as request is response to a mouse down event.)

        // Scroll the given rectangle into view. Doesn't change the zoom factor.
        public void ScrollRectangleIntoView(RectangleF rect)
        {
            float dx = 0, dy = 0;
            int dxPixels, dyPixels;

            if (rect.Left < viewport.Left && rect.Right <= viewport.Right)
                dx = Math.Min(viewport.Left - rect.Left, viewport.Right - rect.Right);
            else if (rect.Right > viewport.Right && rect.Left >= viewport.Left)
                dx = -Math.Min(rect.Right - viewport.Right, rect.Left - viewport.Left);

            if (rect.Top < viewport.Top && rect.Bottom <= viewport.Bottom)
                dy = -Math.Min(viewport.Top - rect.Top, viewport.Bottom - rect.Bottom);
            else if (rect.Bottom > viewport.Bottom && rect.Top >= viewport.Top)
                dy = Math.Min(rect.Bottom - viewport.Bottom, rect.Top - viewport.Top);

            if (dx == 0 && dy == 0)
                return;         // Nothing to do.

            // Scroll a whole number of pixels.
            dxPixels = (int) Math.Ceiling(WorldToPixelDistance(dx));
            dyPixels = (int) Math.Ceiling(WorldToPixelDistance(dy));

            ScrollView(dxPixels, dyPixels);
        }


        [DllImport("user32.dll")]
        private static extern bool ScrollWindow(IntPtr hwnd, int dx, int dy, IntPtr lpRect, IntPtr lpClipRect);

        // Scroll the view by a certain number of PIXELs.
        public void ScrollView(int dxPixels, int dyPixels)
        {
            PointF centerPixels = WorldToPixel(centerPoint);
            centerPixels.X -= dxPixels;
            centerPixels.Y -= dyPixels;
            PointF newCenter = PixelToWorld(centerPixels);

            bool success;
            success = ScrollWindow(canvas.Handle, dxPixels, dyPixels, IntPtr.Zero, IntPtr.Zero);

            if (success) {
                // Clear the "uncovered area" (looks better).
                Rectangle rect = canvas.ClientRectangle;
                rect.Offset(dxPixels, dyPixels);
                using (Graphics g = canvas.CreateGraphics()) {
                    g.ExcludeClip(rect);
                    g.Clear(SystemColors.AppWorkspace);
                }

                centerPoint = newCenter;
                CalculateWorldTransform();

                UpdateScrollBars();

                if (OnViewportChange != null)
                    OnViewportChange(this, EventArgs.Empty);
            }
            else {
                Debug.Fail("Why did the Win32 API call fail?");
                CenterPoint = newCenter; // Use another way to scroll if the API doesn't work.
            }
        }

        public void BeginCanvasDragging(Point pt, MouseButtons endingButton)
        {
            dragScrollingInProgress = true;
            endDragScrollButton = endingButton;
            lastDragScrollPoint = pt;
            canvas.Cursor = Cursors.SizeAll;
        }

        void EndCanvasDragging(Point pt)
        {
            dragScrollingInProgress = false;
            canvas.Cursor = Cursors.Default;
        }

        void CanvasDrag(Point pt)
        {
            Debug.Assert(dragScrollingInProgress);
            ScrollView(pt.X - lastDragScrollPoint.X, pt.Y - lastDragScrollPoint.Y);

            lastDragScrollPoint = pt;
        }
        #endregion Drag scrolling 

        #region Event handlers

        void PointerMoved(bool mouseInViewport, int xViewport, int yViewport)
        {
            this.mouseInView = mouseInViewport;

            if (mouseInView) 
                mouseLocation = PixelToWorld(new PointF(xViewport, yViewport));
            else
                mouseLocation = new PointF();

            if (OnPointerMove != null)
                OnPointerMove(this, mouseInView, mouseLocation);
        }

        private void canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                BeginCanvasDragging(new Point(e.X, e.Y), e.Button);
            else
                OnMouseDown(e);
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragScrollingInProgress)
                CanvasDrag(new Point(e.X, e.Y));
            else
                PointerMoved(true, e.X, e.Y);
        }

        private void canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == endDragScrollButton) {
                EndCanvasDragging(new Point(e.X, e.Y));
            }

        }

        private void canvas_MouseLeave(object sender, EventArgs e)
        {
            PointerMoved(false, 0, 0);
        }

        private void canvas_MouseCaptureChanged(object sender, EventArgs e)
        {
            if (dragScrollingInProgress)
                EndCanvasDragging(lastDragScrollPoint);
        }

        private void canvas_MouseEnter(object sender, EventArgs e)
        {
            canvas.Focus();
        }

        private void canvas_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            const float zoomChange = 1.12F;
            int wheelDelta = e.Delta;
            float newZoom = ZoomFactor;

            // Determine the point to zoom around.
            Rectangle rect = canvas.ClientRectangle;
            PointF zoomPtPixel, zoomPtWorld;

            if (rect.Contains(e.X, e.Y))
                zoomPtPixel = new PointF(e.X, e.Y);
            else
                return;
            zoomPtWorld = PixelToWorld(zoomPtPixel);

            // Determine the new zoom factor.
            if (wheelDelta > 0) {
                while (wheelDelta >= 40) {
                    newZoom *= zoomChange;
                    wheelDelta -= 40;
                }
            }
            else {
                while (wheelDelta <= -40) {
                    newZoom /= zoomChange;
                    wheelDelta += 40;
                }
            }

            ZoomAroundPoint(zoomPtWorld, newZoom);
        }

        private void canvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (zoom > 1.0)
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
            else
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.MultiplyTransform(GetWorldToPixelXform());
            if (bitmap != null)
                g.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        }

        private void canvas_Resize(object sender, EventArgs e)
        {
            ViewportChanged();
        }

        #endregion EventHandlers

        private void hScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            PointF centerPt = CenterPoint;
            centerPt.X = e.NewValue;
            CenterPoint = centerPt;
        }

        private void vScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            PointF centerPt = CenterPoint;
            centerPt.Y = e.NewValue;
            CenterPoint = centerPt;
        }


    }
}

