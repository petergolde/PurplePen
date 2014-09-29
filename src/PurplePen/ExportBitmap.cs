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
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

using PurplePen.MapModel;
using PurplePen.Graphics2D;

namespace PurplePen
{
    // Class for exporting the map to a bitmap file..
    class ExportBitmap
    {
        private MapDisplay mapDisplay;

        // Create an instance for displaying the given mapDisplay. This should be
        // a clone, since it will be changed.
        public ExportBitmap(MapDisplay mapDisplay)
        {
            this.mapDisplay = mapDisplay;
            this.mapDisplay.AntiAlias = true;
            this.mapDisplay.MapIntensity = 1.0F;
            this.mapDisplay.Printing = false;
            this.mapDisplay.ColorModel = ColorModel.CMYK;
        }

        // Create a bitmap file of the mapDisplay supplied at construction.
        public void CreateBitmap(string fileName, RectangleF rect, ImageFormat imageFormat, float dpi)
        {
            float bitmapWidth, bitmapHeight; // size of the bitmap in pixels.
            int pixelWidth, pixelHeight; // bitmapWidth/Height, rounded up to integer.

            bitmapWidth = (rect.Width / 25.4F) * dpi;
            bitmapHeight = (rect.Height / 25.4F) * dpi;
            pixelWidth = (int)Math.Ceiling(bitmapWidth);
            pixelHeight = (int) Math.Ceiling(bitmapHeight);

            Bitmap bitmap = new Bitmap(pixelWidth, pixelHeight, PixelFormat.Format24bppRgb);
            bitmap.SetResolution(dpi, dpi);

            // Set the transform
            Matrix transform = Geometry.CreateInvertedRectangleTransform(rect, new RectangleF(0, 0, bitmapWidth, bitmapHeight));

            // And draw.
            mapDisplay.Draw(bitmap, transform);

            // JPEG and GIF have special code paths because the default Save method isn't
            // really good enough.
            if (imageFormat == ImageFormat.Jpeg)
                BitmapUtil.SaveJpeg(bitmap, fileName, 80);
            else if (imageFormat == ImageFormat.Gif)
                BitmapUtil.SaveGif(bitmap, fileName);
            else
                bitmap.Save(fileName, imageFormat);

            bitmap.Dispose();
        }

        public void CreateBitmapAutoDpi(string fileName, RectangleF rect, ImageFormat imageFormat, int maxPixelWidth, float minDpi, float maxDpi)
        {
            float dpi = maxPixelWidth * 25.4F / Math.Max(rect.Width, rect.Height);

            if (dpi > maxDpi)
                dpi = maxDpi;
            else if (dpi < minDpi)
                dpi = minDpi;
            else {
                dpi = (float)Math.Round(dpi / 10F) * 10F;
            }

            CreateBitmap(fileName, rect, imageFormat, dpi);
        }
    }

}
