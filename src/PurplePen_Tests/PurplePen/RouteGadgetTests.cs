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
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using TestingUtils;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PurplePen.Tests
{
    [TestClass]
    public class RouteGadgetTests : TestFixtureBase
    {
        // This is just to compare file sizes/quality of GIF vs. JPG.
        // [TestMethod]
        public void CompareFiles()
        {
            MapDisplay mapDisplay = new MapDisplay();
            string filenameJpeg = TestUtil.GetTestFile(@"routegadget\exporttest.jpg");
            string filenameGif = TestUtil.GetTestFile(@"routegadget\exporttest.gif");

            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"routegadget\salmon_la_sac-20090625.ocd"));
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 0.3F;

            ExportBitmap exporter = new ExportBitmap(mapDisplay.Clone());
            exporter.CreateBitmap(filenameJpeg, RectangleF.FromLTRB(-90F, 10F, 60F, 105F), ImageFormat.Jpeg, 200F, null);
            exporter.CreateBitmap(filenameGif, RectangleF.FromLTRB(-90F, 10F, 60F, 105F), ImageFormat.Gif, 200F, null);
        }



        [TestMethod]
        public void TestExportJpeg()
        {
            MapDisplay mapDisplay = new MapDisplay();
            string filename = TestUtil.GetTestFile(@"routegadget\exportjpeg1.jpg");
            string filenameBaseline = TestUtil.GetTestFile(@"routegadget\exportjpeg1_baseline.png");

            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"routegadget\SampleEvent.ocd"));
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 0.3F;

            ExportBitmap exporter = new ExportBitmap(mapDisplay.Clone());
            exporter.CreateBitmap(filename, RectangleF.FromLTRB(0F, 70F, 300F, 400F), ImageFormat.Jpeg, 200F, null);

            Assert.IsFalse(mapDisplay.AntiAlias);
            Assert.AreEqual(0.3F, mapDisplay.MapIntensity);

            Bitmap bmLoaded = (Bitmap) Image.FromFile(filename);
            Assert.AreEqual(200F, bmLoaded.VerticalResolution);
            Assert.AreEqual(200F, bmLoaded.HorizontalResolution);

            TestUtil.CompareBitmapBaseline(bmLoaded, filenameBaseline);
            bmLoaded.Dispose();
            File.Delete(filename);
        }

        [TestMethod]
        public void TestExportJpegAutoDpi1()
        {
            MapDisplay mapDisplay = new MapDisplay();
            string filename = TestUtil.GetTestFile(@"routegadget\exportjpeg2.jpg");
            string filenameBaseline = TestUtil.GetTestFile(@"routegadget\exportjpeg2_baseline.png");

            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"routegadget\SampleEvent.ocd"));
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 0.3F;

            ExportBitmap exporter = new ExportBitmap(mapDisplay.Clone());
            exporter.CreateBitmapAutoDpi(filename, RectangleF.FromLTRB(0F, 70F, 300F, 200F), ImageFormat.Jpeg, 1700, 100, 200);

            Assert.IsFalse(mapDisplay.AntiAlias);
            Assert.AreEqual(0.3F, mapDisplay.MapIntensity);

            Bitmap bmLoaded = (Bitmap)Image.FromFile(filename);
            Assert.AreEqual(140F, bmLoaded.VerticalResolution);
            Assert.AreEqual(140F, bmLoaded.HorizontalResolution);

            TestUtil.CompareBitmapBaseline(bmLoaded, filenameBaseline);
            bmLoaded.Dispose();
            File.Delete(filename);
        }

        [TestMethod]
        public void TestExportJpegAutoDpi2()
        {
            MapDisplay mapDisplay = new MapDisplay();
            string filename = TestUtil.GetTestFile(@"routegadget\exportjpeg3.jpg");
            string filenameBaseline = TestUtil.GetTestFile(@"routegadget\exportjpeg3_baseline.png");

            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"routegadget\SampleEvent.ocd"));
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 0.3F;

            ExportBitmap exporter = new ExportBitmap(mapDisplay.Clone());
            exporter.CreateBitmapAutoDpi(filename, RectangleF.FromLTRB(0F, 70F, 100F, 100F), ImageFormat.Jpeg, 1700, 100, 200);

            Assert.IsFalse(mapDisplay.AntiAlias);
            Assert.AreEqual(0.3F, mapDisplay.MapIntensity);

            Bitmap bmLoaded = (Bitmap)Image.FromFile(filename);
            Assert.AreEqual(200F, bmLoaded.VerticalResolution);
            Assert.AreEqual(200F, bmLoaded.HorizontalResolution);

            TestUtil.CompareBitmapBaseline(bmLoaded, filenameBaseline);
            bmLoaded.Dispose();
            File.Delete(filename);
        }


        [TestMethod]
        public void TestExportJpegAutoDpi3()
        {
            MapDisplay mapDisplay = new MapDisplay();
            string filename = TestUtil.GetTestFile(@"routegadget\exportjpeg4.jpg");
            string filenameBaseline = TestUtil.GetTestFile(@"routegadget\exportjpeg4_baseline.png");

            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"routegadget\SampleEvent.ocd"));
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 0.3F;

            ExportBitmap exporter = new ExportBitmap(mapDisplay.Clone());
            exporter.CreateBitmapAutoDpi(filename, RectangleF.FromLTRB(0F, 70F, 600F, 600F), ImageFormat.Jpeg, 1700, 100, 200);

            Assert.IsFalse(mapDisplay.AntiAlias);
            Assert.AreEqual(0.3F, mapDisplay.MapIntensity);

            Bitmap bmLoaded = (Bitmap)Image.FromFile(filename);
            Assert.AreEqual(100F, bmLoaded.VerticalResolution);
            Assert.AreEqual(100F, bmLoaded.HorizontalResolution);

            TestUtil.CompareBitmapBaseline(bmLoaded, filenameBaseline);
            bmLoaded.Dispose();
            File.Delete(filename);
        }


        [TestMethod]
        public void TestExportGif()
        {
            MapDisplay mapDisplay = new MapDisplay();
            string filename = TestUtil.GetTestFile(@"routegadget\exportgif1.gif");
            string filenameBaseline = TestUtil.GetTestFile(@"routegadget\exportgif1_baseline.png");

            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"routegadget\SampleEvent.ocd"));
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 0.3F;

            ExportBitmap exporter = new ExportBitmap(mapDisplay.Clone());
            exporter.CreateBitmap(filename, RectangleF.FromLTRB(0F, 70F, 300F, 400F), ImageFormat.Gif, 200F, null);

            Assert.IsFalse(mapDisplay.AntiAlias);
            Assert.AreEqual(0.3F, mapDisplay.MapIntensity);

            Bitmap bmLoaded = (Bitmap) Image.FromFile(filename);

            TestUtil.CompareBitmapBaseline(bmLoaded, filenameBaseline);
            bmLoaded.Dispose();
            File.Delete(filename);
        }

        [TestMethod]
        public void TestExportWorldFile()
        {
            MapDisplay mapDisplay = new MapDisplay();
            string filename = TestUtil.GetTestFile(@"routegadget\exportgif2.gif");
            string worldfile = TestUtil.GetTestFile(@"routegadget\exportgif2.gfw");
            string filenameBaseline = TestUtil.GetTestFile(@"routegadget\exportgif2_baseline.png");
            string worldfileBaseline = TestUtil.GetTestFile(@"routegadget\exportgif2.gfw.expected");

            mapDisplay.SetMapFile(MapType.OCAD, TestUtil.GetTestFile(@"routegadget\GRC-Jan2017.ocd"));
            mapDisplay.AntiAlias = false;
            mapDisplay.MapIntensity = 0.3F;

            ExportBitmap exporter = new ExportBitmap(mapDisplay.Clone());
            exporter.CreateBitmap(filename, RectangleF.FromLTRB(-43.78F, 201.04F, 168.46F, 418.32F), ImageFormat.Gif, 200F, mapDisplay.CoordinateMapper);

            Assert.IsFalse(mapDisplay.AntiAlias);
            Assert.AreEqual(0.3F, mapDisplay.MapIntensity);

            Bitmap bmLoaded = (Bitmap)Image.FromFile(filename);

            TestUtil.CompareBitmapBaseline(bmLoaded, filenameBaseline);
            bmLoaded.Dispose();

            TestUtil.CompareTextFileBaseline(worldfile, worldfileBaseline);

            File.Delete(filename);
            File.Delete(worldfile);
        }

        [TestMethod]
        public void ExportRouteGadget()
        {
            string ppenFileName = TestUtil.GetTestFile(@"routegadget\Sample Event.ppen");
            string xmlFileName = TestUtil.GetTestFile(@"routegadget\Sample Event.xml");
            string gifFileName = TestUtil.GetTestFile(@"routegadget\Sample Event.gif");

            TestUI ui = TestUI.Create();
            Controller controller = ui.controller;

            bool success = controller.LoadInitialFile(ppenFileName, true);
            Assert.IsTrue(success);

            controller.MapDisplay.SetCourse(controller.GetCourseLayout());

            success = controller.ExportRouteGadget(xmlFileName, gifFileName);
            Assert.IsTrue(success);

            Dictionary<string, string> exceptions = ExportXmlVersion2.TestFileExceptionMap();

            TestUtil.CompareBitmapBaseline((Bitmap) Image.FromFile(gifFileName), TestUtil.GetTestFile(@"routegadget\Sample Event GIF.baseline.png"));
            TestUtil.CompareTextFileBaseline(xmlFileName, TestUtil.GetTestFile(@"routegadget\Sample Event XML.baseline.xml"), exceptions);

            File.Delete(xmlFileName);
            File.Delete(gifFileName);
        }

    }

}

#endif
