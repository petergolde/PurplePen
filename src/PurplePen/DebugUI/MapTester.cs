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
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

using PurplePen.Graphics2D;
using PurplePen.MapModel;
using PurplePen.MapView;

namespace PurplePen.DebugUI
{
    public partial class MapTester : DpiFixedForm
    {
        IMapViewerHighlight highlight;
        MapDisplay mapDisplay;

        public MapTester()
        {
            InitializeComponent();
            intensityCombo.SelectedIndex = 9;
        }

        // Update all the labels and scroll-bars in the main frame.
        void UpdateLabels()
        {
            UpdatePointerLabel(mapViewer.PointerInView, mapViewer.PointerLocation);

            zoomLabel.Text = string.Format("Zoom: {0}%", mapViewer.ZoomFactor * 100);
            PointF center = mapViewer.CenterPoint;

            // Also update the scroll bars.
            RectangleF fullSize = new RectangleF(-1000, -1000, 2000, 2000);  // TODO: this should be the full size of the map.
            RectangleF viewport = mapViewer.Viewport;

            horizScroll.Minimum = (int)Math.Round(fullSize.Left);
            horizScroll.Maximum = (int)Math.Round(fullSize.Right);
            vertScroll.Maximum = -(int)Math.Round(fullSize.Top);
            vertScroll.Minimum = -(int)Math.Round(fullSize.Bottom);
            horizScroll.Value = (int)Math.Round(Math.Max(Math.Min(center.X, fullSize.Right), fullSize.Left));
            vertScroll.Value = (int)Math.Round(-Math.Max(Math.Min(center.Y, fullSize.Bottom), fullSize.Top));
            horizScroll.LargeChange = (int)Math.Round(viewport.Width * 0.9);
            vertScroll.LargeChange = (int)Math.Round(viewport.Height * 0.9);
            horizScroll.SmallChange = (int)Math.Round(viewport.Width / 8);
            vertScroll.SmallChange = (int)Math.Round(viewport.Height / 8);
        }

        // Update the label that shows the current pointer location.
        void UpdatePointerLabel(bool inViewport, System.Drawing.PointF location)
        {
            Debug.Assert(inViewport == mapViewer.PointerInView);
            Debug.Assert(location == mapViewer.PointerLocation);

            if (inViewport) {
                locationLabel.Text = string.Format("({0:##0.00},{1:##0.00})", location.X, location.Y);
            }
            else {
                locationLabel.Text = "";
            }
        }

        Bitmap RenderBitmap(Size bitmapSize, RectangleF mapArea)
        {
            // Calculate the transform matrix.
            PointF midpoint = new PointF(bitmapSize.Width / 2.0F, bitmapSize.Height / 2.0F);
            float scaleFactor = (float) bitmapSize.Width / mapArea.Width;
            PointF centerPoint = new PointF((mapArea.Left + mapArea.Right) / 2, (mapArea.Top + mapArea.Bottom) / 2);
            Matrix matrix = new Matrix();
            matrix.Translate(midpoint.X, midpoint.Y);
            matrix.Scale(scaleFactor, -scaleFactor);  // y scale is negative to get to cartesian orientation.
            matrix.Translate(-centerPoint.X, -centerPoint.Y);

            // Draw into a new bitmap.
            Bitmap bitmapNew = new Bitmap(bitmapSize.Width, bitmapSize.Height, PixelFormat.Format24bppRgb);
            float minResolution = mapArea.Width / (float) bitmapSize.Width;

            if (mapDisplay != null) {
                mapDisplay.AntiAlias = false;
                mapDisplay.ShowSymbolBounds = showBoundsCheckBox.Checked;
                mapDisplay.Draw(bitmapNew, matrix);
            }

            return bitmapNew;
        }



        // Create a new test file from the given map, using the current view in the viewer, 
        // and write it to the give test file.
        public void CreateTestFile(string testFileName, string mapFileName)
        {
            string pngFileName = Path.ChangeExtension(testFileName, ".png");
            RectangleF mapArea = mapViewer.Viewport;
            Size size = mapViewer.Size;

            using (StreamWriter writer = new StreamWriter(testFileName, false, System.Text.Encoding.UTF8)) {
                writer.WriteLine(Path.GetFileName(mapFileName));
                writer.WriteLine(Path.GetFileName(pngFileName));
                writer.WriteLine("{0:R},{1:R},{2:R},{3:R}", mapArea.Left, mapArea.Bottom, mapArea.Right, mapArea.Top);
                writer.WriteLine("{0},{1}", size.Width, size.Height);
            }

            Bitmap bitmap = RenderBitmap(size, mapArea);
            bitmap.Save(pngFileName, System.Drawing.Imaging.ImageFormat.Png);
            bitmap.Dispose();
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog.ShowDialog();
            if (result == DialogResult.OK) {
                mapDisplay = new MapDisplay();
                mapDisplay.SetMapFile(Path.GetExtension(openFileDialog.FileName) == ".ocd" ? MapType.OCAD : MapType.Bitmap, openFileDialog.FileName);
                mapDisplay.AntiAlias = antialiasCheckBox.Checked;
                mapDisplay.ShowSymbolBounds = showBoundsCheckBox.Checked;
                mapDisplay.MapIntensity = float.Parse(intensityCombo.Text);
                mapViewer.SetMap(mapDisplay);
            }
        }

        private void mapViewer_MouseEnter(object sender, EventArgs e)
        {
            mapViewer.Focus();
        }

        private void showGrid_CheckedChanged(object sender, EventArgs e)
        {
            mapViewer.ShowGrid = showGrid.Checked;
        }

        private void mapViewer_OnPointerMove(object sender, bool inViewport, PointF location)
        {
            UpdatePointerLabel(inViewport, location);
            if (inViewport && ActiveForm == this)
                mapViewer.Focus();
        }

        private MapViewer.DragAction mapViewer_OnMouseEvent(object sender, MouseAction action, int buttonNumber, bool[] whichButtonsDown, PointF location, PointF locationStart)
        {
            if (action == MouseAction.Down && buttonNumber == MapViewer.RightMouseButton)
                return MapViewer.DragAction.DelayedDrag;

            if (action == MouseAction.Drag && buttonNumber == MapViewer.RightMouseButton) {
                RectangleF rect = new RectangleF(Math.Min(location.X, locationStart.X),
                                                 Math.Min(location.Y, locationStart.Y),
                                                 Math.Abs(locationStart.X - location.X),
                                                 Math.Abs(locationStart.Y - location.Y));
                highlight = new RectangleHighlight(rect);
                mapViewer.ChangeHighlight(new IMapViewerHighlight[] { highlight });
                return MapViewer.DragAction.DelayedDrag;
            }

            // Allow the left mouse button to drag.
            if (action == MouseAction.Down && buttonNumber == MapViewer.LeftMouseButton)
                return MapViewer.DragAction.MapDrag;
            else
                return MapViewer.DragAction.None;
        }

        private void zoomCombo_TextChanged(object sender, EventArgs e)
        {
            string text = zoomCombo.Text;

            text = text.Trim();
            if (text.EndsWith("%", StringComparison.InvariantCulture))
                text = text.Substring(0, text.Length - 1);

            float percent;
            if (float.TryParse(text, out percent)) {
                mapViewer.ZoomFactor = percent / 100F;
            }
        }

        private void vertScroll_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.Type == ScrollEventType.SmallIncrement) {
                mapViewer.ScrollView(0, -mapViewer.Height / 6);
            }
            else if (e.Type == ScrollEventType.SmallDecrement) {
                mapViewer.ScrollView(0, mapViewer.Height / 6);
            }
            else if (e.Type == ScrollEventType.LargeIncrement) {
                mapViewer.ScrollView(0, -mapViewer.Height * 5 / 6);
            }
            else if (e.Type == ScrollEventType.LargeDecrement) {
                mapViewer.ScrollView(0, mapViewer.Height * 5 / 6);
            }
            else if (e.Type == ScrollEventType.ThumbPosition) {
                PointF center = mapViewer.CenterPoint;
                center.Y = -e.NewValue;
                mapViewer.CenterPoint = center;
            }
        }

        private void horizScroll_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.Type == ScrollEventType.SmallIncrement) {
                mapViewer.ScrollView(-mapViewer.Width / 6, 0);
            }
            else if (e.Type == ScrollEventType.SmallDecrement) {
                mapViewer.ScrollView(mapViewer.Width / 6, 0);
            }
            else if (e.Type == ScrollEventType.LargeIncrement) {
                mapViewer.ScrollView(-mapViewer.Width * 5 / 6, 0);
            }
            else if (e.Type == ScrollEventType.LargeDecrement) {
                mapViewer.ScrollView(mapViewer.Width * 5 / 6, 0);
            }
            else if (e.Type == ScrollEventType.ThumbPosition) {
                PointF center = mapViewer.CenterPoint;
                center.X = e.NewValue;
                mapViewer.CenterPoint = center;
            }
        }

        private void mapViewer_OnViewportChange(object sender, EventArgs e)
        {
            UpdateLabels();
        }

        private void buttonScrollIntoView_Click(object sender, EventArgs e)
        {
            if (highlight != null)
                mapViewer.ScrollRectangleIntoView(highlight.GetHighlightBounds());
        }

        private void buttonCreateTest_Click(object sender, EventArgs e)
        {
            DialogResult result = saveFileDialog.ShowDialog(this);
            if (result == DialogResult.OK) {
                CreateTestFile(saveFileDialog.FileName, openFileDialog.FileName);
            }
        }

        private void antialiasCheckBox_Click(object sender, EventArgs e)
        {
            mapDisplay.AntiAlias = antialiasCheckBox.Checked;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mapDisplay != null)
                mapDisplay.MapIntensity = float.Parse(intensityCombo.Text);
        }

        private void showBoundsCheckBox_Click(object sender, EventArgs e)
        {
            mapDisplay.ShowSymbolBounds = showBoundsCheckBox.Checked;
        }

    }
}