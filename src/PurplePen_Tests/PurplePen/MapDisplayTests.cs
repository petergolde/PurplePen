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
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using TestingUtils;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PurplePen.Tests
{
    [TestClass]
    public class MapDisplayTests: TestFixtureBase
    {
        Matrix transform;
        Bitmap bitmap;

        void SetupBitmap()
        {
            bitmap = new Bitmap(400, 400);
            transform = new Matrix();
            transform.Translate(0, bitmap.Height);
            transform.Scale(8F, -8F);
            transform.Translate(-50F, -170F);
        }

        void DrawToBitmap(MapDisplay mapdisplay, RectangleF clip)
        {
            Matrix inverse = transform.Clone();
            inverse.Invert();
            using (Region clipRegion = new Region(clip)) {
                clipRegion.Transform(inverse);
                mapdisplay.Draw(bitmap, transform, clipRegion);
            }
        }

        [TestMethod]
        public void OcadBoundsScale()
        {
            MapDisplay mapDisplay = new MapDisplay() ;

            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.ocd"));
            Assert.AreEqual(15000, mapDisplay.MapScale);
            TestUtil.AssertEqualRect(RectangleF.FromLTRB(-0.22F, 0.01F, 199.85F, 267.8F), mapDisplay.MapBounds, 0.01F, "map bounds");
        }

        [TestMethod]
        public void BitmapBounds()
        {
            MapDisplay mapDisplay = new MapDisplay();

            mapDisplay.SetMapFile(MapType.Bitmap, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.jpg"));
            mapDisplay.Dpi = 300;
            TestUtil.AssertEqualRect(RectangleF.FromLTRB(0F, 0F, 201.08F, 269.16F), mapDisplay.MapBounds, 0.01F, "map bounds");
        }

        [TestMethod]
        public void BasicOcadMap()
        {
            SetupBitmap();
            MapDisplay mapDisplay = new MapDisplay();

            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.ocd"));
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 1.0F;

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);
            DrawToBitmap(mapDisplay, drawRect);

            TestUtil.CompareBitmapBaseline(bitmap, TestUtil.GetTestFile(@"mapdisplay\BasicOcadMap.png"));
        }

        [TestMethod]
        public void BasicBitmapMap()
        {
            SetupBitmap();
            MapDisplay mapDisplay = new MapDisplay();

            mapDisplay.SetMapFile(MapType.Bitmap, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.jpg"));
            mapDisplay.Dpi = 300;
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 1.0F;

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);
            DrawToBitmap(mapDisplay, drawRect);

            TestUtil.CompareBitmapBaseline(bitmap, TestUtil.GetTestFile(@"mapdisplay\BasicBitmapMap.png"));
        }

        [TestMethod]
        public void AntialiasOcadMap()
        {
            SetupBitmap();
            MapDisplay mapDisplay = new MapDisplay();


            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.ocd"));
            mapDisplay.AntiAlias = true;
            mapDisplay.MapIntensity = 1.0F;

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);
            DrawToBitmap(mapDisplay, drawRect);

            TestUtil.CompareBitmapBaseline(bitmap, TestUtil.GetTestFile(@"mapdisplay\AntialiasOcadMap.png"));
        }

        [TestMethod]
        public void AntialiasBitmapMap()
        {
            SetupBitmap();
            MapDisplay mapDisplay = new MapDisplay();


            mapDisplay.SetMapFile(MapType.Bitmap, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.jpg"));
            mapDisplay.Dpi = 300;
            mapDisplay.AntiAlias = true;
            mapDisplay.MapIntensity = 1.0F;

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);
            DrawToBitmap(mapDisplay, drawRect);

            TestUtil.CompareBitmapBaseline(bitmap, TestUtil.GetTestFile(@"mapdisplay\AntialiasBitmapMap.png"));
        }

        [TestMethod]
        public void IntensityOcadMap()
        {
            SetupBitmap();
            MapDisplay mapDisplay = new MapDisplay();


            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.ocd"));
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 0.3F;

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);
            DrawToBitmap(mapDisplay, drawRect);

            TestUtil.CompareBitmapBaseline(bitmap, TestUtil.GetTestFile(@"mapdisplay\IntensityOcadMap.png"));
        }

        [TestMethod]
        public void IntensityBitmapMap()
        {
            SetupBitmap();
            MapDisplay mapDisplay = new MapDisplay();


            mapDisplay.SetMapFile(MapType.Bitmap, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.jpg"));
            mapDisplay.Dpi = 300;
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 0.3F;

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);
            DrawToBitmap(mapDisplay, drawRect);

            TestUtil.CompareBitmapBaseline(bitmap, TestUtil.GetTestFile(@"mapdisplay\IntensityBitmapMap.png"));
        }

        [TestMethod]
        public void CloneOcad()
        {
            MapDisplay mapDisplay = new MapDisplay();

            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.ocd"));
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 0.3F;

            MapDisplay cloned = mapDisplay.Clone();

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);

            SetupBitmap();
            cloned.AntiAlias = true;
            cloned.MapIntensity = 1.0F;
            DrawToBitmap(cloned, drawRect);

            TestUtil.CompareBitmapBaseline(bitmap, TestUtil.GetTestFile(@"mapdisplay\ClonedOcadMap.png"));

            SetupBitmap();
            DrawToBitmap(mapDisplay, drawRect);

            TestUtil.CompareBitmapBaseline(bitmap, TestUtil.GetTestFile(@"mapdisplay\NonclonedOcadMap.png"));
        }

        [TestMethod]
        public void CloneBitmap()
        {
            MapDisplay mapDisplay = new MapDisplay();

            mapDisplay.SetMapFile(MapType.Bitmap, TestUtil.GetTestFile(@"mapdisplay\SampleEvent.jpg"));
            mapDisplay.Dpi = 300;
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 0.3F;

            MapDisplay cloned = mapDisplay.Clone();

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);

            SetupBitmap();
            cloned.AntiAlias = true;
            cloned.MapIntensity = 1.0F;
            DrawToBitmap(cloned, drawRect);

            TestUtil.CompareBitmapBaseline(bitmap, TestUtil.GetTestFile(@"mapdisplay\ClonedBitmapMap.png"));

            SetupBitmap();
            DrawToBitmap(mapDisplay, drawRect);

            TestUtil.CompareBitmapBaseline(bitmap, TestUtil.GetTestFile(@"mapdisplay\NonclonedBitmapMap.png"));
        }

        [TestMethod]
        public void OverprintOcadMap()
        {
            SetupBitmap();
            MapDisplay mapDisplay = new MapDisplay();


            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"mapdisplay\overprint.ocd"));
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 1F;
            mapDisplay.OcadOverprintEffect = true;

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);
            DrawToBitmap(mapDisplay, drawRect);

            TestUtil.CompareBitmapBaseline(bitmap, TestUtil.GetTestFile(@"mapdisplay\OverprintOcadMap.png"));
        }


        [TestMethod]
        public void NoOverprintOcadMap()
        {
            SetupBitmap();
            MapDisplay mapDisplay = new MapDisplay();


            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"mapdisplay\overprint.ocd"));
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 1F;
            mapDisplay.OcadOverprintEffect = false;

            RectangleF drawRect = RectangleF.FromLTRB(50F, 170F, 100F, 220F);
            DrawToBitmap(mapDisplay, drawRect);

            TestUtil.CompareBitmapBaseline(bitmap, TestUtil.GetTestFile(@"mapdisplay\NoOverprintOcadMap.png"));
        }


    }
}

#endif //TEST
