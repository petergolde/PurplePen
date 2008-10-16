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

#if TEST
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.MapModel.Tests
{
    [TestClass]
    public class BoundsTests
    {
        [TestInitialize]
        public void Init()
        {
        }

        static Bitmap RenderBitmap(Map map, Size bitmapSize, RectangleF mapArea)
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
            using (Graphics g = Graphics.FromImage(bitmapNew)) {
                RenderOptions renderOpts = new RenderOptions();
                renderOpts.forceBitmapGlyphs = false;
                renderOpts.minResolution = mapArea.Width / (float) bitmapSize.Width;
                renderOpts.showSymbolBounds = true;

                g.Clear(Color.White);
                g.Transform = matrix;

                using (map.Read())
                    map.Draw(g, mapArea, renderOpts);
            }

            return bitmapNew;
        }

        // Verifies a test file. Returns true on success, false on failure. In the failure case, 
        // a difference bitmap is written out.
        static bool VerifyTestFile(string filename)
        {

            string pngFileName;
            string mapFileName;
            string directoryName;
            RectangleF mapArea;
            Size size;

            // Read the test file, and get the other file names and the area.
            using (StreamReader reader = new StreamReader(filename)) {
                mapFileName = reader.ReadLine();
                pngFileName = reader.ReadLine();
                float left, right, top, bottom;
                string area = reader.ReadLine();
                string[] coords = area.Split(',');
                left = float.Parse(coords[0]); bottom = float.Parse(coords[1]); right = float.Parse(coords[2]); top = float.Parse(coords[3]);
                mapArea = new RectangleF(left, top, right - left, bottom - top);
                string sizeLine = reader.ReadLine();
                coords = sizeLine.Split(',');
                size = new Size(int.Parse(coords[0]), int.Parse(coords[1]));
            }

            // Convert to absolute paths.
            directoryName = Path.GetDirectoryName(filename);
            mapFileName = Path.Combine(directoryName, mapFileName);
            pngFileName = Path.Combine(directoryName, pngFileName);

            // Create and open the map file.
            Map map = new Map();
            InputOutput.ReadFile(mapFileName, map);

            // Draw into a new bitmap.
            Bitmap bitmapNew = RenderBitmap(map, size, mapArea);

            TestUtil.CompareBitmapBaseline(bitmapNew, pngFileName);

            return true;
        }


        void CheckTest(string filename)
        {
            string fullname = TestUtil.GetTestFile("bounds\\" + filename);
            bool ok = VerifyTestFile(fullname);
            Assert.IsTrue(ok, string.Format("Bounds test {0} did not compare correctly.", filename), ok);
        }

        [TestMethod]
        public void Lines()
        {
            CheckTest("lines.txt");
        }

        [TestMethod]
        public void Areas()
        {
            CheckTest("areas.txt");
        }

        [TestMethod]
        public void Points()
        {
            CheckTest("points.txt");
        }

        [TestMethod]
        public void LineText()
        {
            CheckTest("linetext.txt");
        }

        [TestMethod]
        public void Text()
        {
            CheckTest("text.txt");
        }

        [TestMethod]
        public void TextUnderline()
        {
            CheckTest("textunderline.txt");
        }

        [TestMethod]
        public void TextFraming()
        {
            CheckTest("textframing.txt");
        }

    }

}

#endif //TEST
