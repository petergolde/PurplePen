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
using System.Runtime.InteropServices;

using PurplePen.MapModel;
using PurplePen.Graphics2D;
using System.IO;
using System.Globalization;

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
        }

        // Create a bitmap file of the mapDisplay supplied at construction.
        // If mapperForWorldFile is not null and real world coords are defined, also create a world file.
        public void CreateBitmap(string fileName, RectangleF rect, ImageFormat imageFormat, float dpi, CoordinateMapper mapperForWorldFile)
        {
            float bitmapWidth, bitmapHeight; // size of the bitmap in pixels.
            int pixelWidth, pixelHeight; // bitmapWidth/Height, rounded up to integer.

            bitmapWidth = (rect.Width / 25.4F) * dpi;
            bitmapHeight = (rect.Height / 25.4F) * dpi;
            pixelWidth = (int)Math.Ceiling(bitmapWidth);
            pixelHeight = (int)Math.Ceiling(bitmapHeight);

            Bitmap bitmap = new Bitmap(pixelWidth, pixelHeight, GDIPlus_GraphicsTarget.NonAlphaPixelFormat);
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

            if (mapperForWorldFile != null && mapperForWorldFile.HasRealWorldCoords) {
                string extension = Path.GetExtension(fileName);
                string worldFileName = Path.ChangeExtension(fileName, WorldFileExtension(extension));
                CreateWorldFile(worldFileName, rect, bitmapWidth, bitmapHeight, mapperForWorldFile);
            }
        }

        public void CreateBitmapAutoDpi(string fileName, RectangleF rect, ImageFormat imageFormat, int maxPixelWidth, float minDpi, float maxDpi, CoordinateMapper mapperForWorldFile = null)
        {
            float dpi = maxPixelWidth * 25.4F / Math.Max(rect.Width, rect.Height);

            if (dpi > maxDpi)
                dpi = maxDpi;
            else if (dpi < minDpi)
                dpi = minDpi;
            else {
                dpi = (float)Math.Round(dpi / 10F) * 10F;
            }

            CreateBitmap(fileName, rect, imageFormat, dpi, mapperForWorldFile);
        }

        // Get the file extension for a world file.
        private string WorldFileExtension(string extension)
        {
            if (extension.Length == 4) {
                return "." + extension[1].ToString() + extension[3].ToString() + "w";
            }
            else {
                return extension + "w";
            }
        }

        // Create a world file using the given coordinate mapper.
        // See https://en.wikipedia.org/wiki/World_file
        private void CreateWorldFile(string worldFileName, RectangleF rect, float bitmapWidth, float bitmapHeight, CoordinateMapper mapperForWorldFile)
        {
            double a, b, c, d, e, f;
            Matrix transform = Geometry.CreateInvertedRectangleTransform(new RectangleF(0, 0, bitmapWidth, bitmapHeight), rect);
            PointF[] transformedPoints = Geometry.TransformPoints(new PointF[] { new PointF(0, 0), new PointF(1, 0), new PointF(0, 1) }, transform);
            double[] realX = new double[transformedPoints.Length];
            double[] realY = new double[transformedPoints.Length];
            for (int i = 0; i < transformedPoints.Length; ++i) {
                mapperForWorldFile.GetRealWorld(transformedPoints[i], out realX[i], out realY[i]);
            }

            c = realX[0];
            f = realY[0];
            a = realX[1] - c;
            d = realY[1] - f;
            b = realX[2] - c;
            e = realY[2] - f;

            using (TextWriter writer = new StreamWriter(worldFileName)) {
                writer.WriteLine(a.ToString("F10", CultureInfo.InvariantCulture));
                writer.WriteLine(d.ToString("F10", CultureInfo.InvariantCulture));
                writer.WriteLine(b.ToString("F10", CultureInfo.InvariantCulture));
                writer.WriteLine(e.ToString("F10", CultureInfo.InvariantCulture));
                writer.WriteLine(c.ToString("F5", CultureInfo.InvariantCulture));
                writer.WriteLine(f.ToString("F5", CultureInfo.InvariantCulture));
            }
        }

    }


}
