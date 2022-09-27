/* Copyright (c) 2016, Peter Golde
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
using NUnit.Framework;
using System.Drawing;

using TestingUtils;
using System.IO;
using System.Linq;

namespace PurplePen.MapModel.Tests
{
    [TestFixture]
    public class OpenMapperImportTests
    {
        Map LoadMap(string baseName)
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("openmapper")));
            InputOutput.ReadFile(TestUtil.GetTestFile("openmapper\\" + baseName), map);
            return map;
        }

        Map RoundTripMap(Map map, string basename, int version = 6)
        {
            MapFileFormat fileFormat;
            string tempPath = GetTempPath(basename);

            if (Path.GetExtension(basename).Equals(".xmap", StringComparison.InvariantCultureIgnoreCase))
                fileFormat = new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.XMap, version);
            else
                fileFormat = new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.OMap, version);

            InputOutput.WriteFile(tempPath, map, fileFormat);

            Map newMap = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("openmapper")));
            InputOutput.ReadFile(tempPath, newMap);

            return newMap;
        }

        IEnumerable<Map> LoadMaps(params string[] baseNames)
        {
            foreach (String baseName in baseNames) {
                yield return LoadMap(baseName);
            }
        }

        string GetTempPath(string baseName)
        {
            string pathName = TestUtil.GetTestFile("openmapper\\" + baseName);
            string extension = Path.GetExtension(pathName);
            return Path.ChangeExtension(pathName, null) + "_temp" + extension;
        }

        void VerifyRendering(Map map, RectangleF area, Size size, string baseline, bool antiAlias)
        {
            Bitmap result;

            if (antiAlias)
                result = Rendering.RenderAntiAliasBitmap(map, size, area, true, false, 1.0F);
            else
                result = Rendering.RenderBitmap(map, size, area, true, false, 1.0F);

            TestUtil.CompareBitmapBaseline(result, TestUtil.GetTestFile("openmapper\\" + baseline));
        }

        void VerifyRendering(Map map, RectangleF area, string baseline)
        {
            Size size;
            float ratio;
            if (area.Width > area.Height) {
                ratio = area.Width / 1000;
            }
            else {
                ratio = area.Height / 1000;
            }
            size = new Size((int)Math.Round(area.Width / ratio), (int)Math.Round(area.Height / ratio));

            Bitmap result = Rendering.RenderBitmap(map, size, area, true, false, 1.0F);

            TestUtil.CompareBitmapBaseline(result, TestUtil.GetTestFile("openmapper\\" + baseline));
        }

        void VerifyRenderingOverprint(Map map, RectangleF area, string baseline)
        {
            Size size;
            float ratio;
            if (area.Width > area.Height) {
                ratio = area.Width / 1000;
            }
            else {
                ratio = area.Height / 1000;
            }
            size = new Size((int)Math.Round(area.Width / ratio), (int)Math.Round(area.Height / ratio));

            Bitmap result = Rendering.RenderBitmap(map, size, area, true, true, 1.0F);

            TestUtil.CompareBitmapBaseline(result, TestUtil.GetTestFile("openmapper\\" + baseline));
        }

        void VerifyRenderingAndRoundtrip(string basename, RectangleF area, string baseline, int version = 6)
        {
            Map map = LoadMap(basename);
            VerifyRendering(map, area, baseline);

            Map newMap = RoundTripMap(map, basename, version);

            // Can use a separate baseline for the round trip part.
            string baselineRT = TestUtil.AppendToPathName(baseline, "_rt");

            if (File.Exists(TestUtil.GetTestFile("openmapper\\" + baselineRT))) {
                VerifyRendering(newMap, area, baselineRT);
            }
            else {
                VerifyRendering(newMap, area, baseline);
            }

            //File.Delete(GetTempPath(basename));
        }

        void VerifyRenderingAndRoundtripOverprint(string basename, RectangleF area, string baseline, int version = 6)
        {
            Map map = LoadMap(basename);
            VerifyRenderingOverprint(map, area, baseline);

            Map newMap = RoundTripMap(map, basename, version);

            // Can use a separate baseline for the round trip part.
            string baselineRT = TestUtil.AppendToPathName(baseline, "_rt");

            if (File.Exists(TestUtil.GetTestFile("openmapper\\" + baselineRT))) {
                VerifyRenderingOverprint(newMap, area, baselineRT);
            }
            else {
                VerifyRenderingOverprint(newMap, area, baseline);
            }

            //File.Delete(GetTempPath(basename));
        }

        void VerifyRenderingNoBitmap(Map map, RectangleF area, string baseline)
        {
            Size size;
            float ratio;
            if (area.Width > area.Height) {
                ratio = area.Width / 1000;
            }
            else {
                ratio = area.Height / 1000;
            }
            size = new Size((int)Math.Round(area.Width / ratio), (int)Math.Round(area.Height / ratio));

            Bitmap result = Rendering.RenderBitmap(map, size, area, false, false, 1.0F);

            TestUtil.CompareBitmapBaseline(result, TestUtil.GetTestFile("openmapper\\" + baseline));
        }

        [Test]
        public void IsOpenMapperFile()
        {
            using (Stream stream = new FileStream(TestUtil.GetTestFile("openmapper\\teanaway.xmap"), FileMode.Open, FileAccess.Read)) {
                Assert.IsTrue(InputOutput.IsOpenMapperFile(stream));
            }
            using (Stream stream = new FileStream(TestUtil.GetTestFile("openmapper\\teanaway.omap"), FileMode.Open, FileAccess.Read)) {
                Assert.IsTrue(InputOutput.IsOpenMapperFile(stream));
            }
            using (Stream stream = new FileStream(TestUtil.GetTestFile("openmapper\\TeanawayValley.ocd"), FileMode.Open, FileAccess.Read)) {
                Assert.IsFalse(InputOutput.IsOpenMapperFile(stream));
            }
        }

        [Test]
        public void LoadColors()
        {
            foreach (Map map in LoadMaps("teanaway.xmap", "teanaway.omap")) {
                List<SymColor> colors;
                using (map.Read()) {
                    colors = map.AllColors.ToList();
                }

                Assert.AreEqual(35, colors.Count); // one extra for registration black.

                SymColor yellow65 = colors[4];
                Assert.AreEqual("Yellow 65%", yellow65.Name);
                Assert.AreEqual(0, yellow65.CmykColor.Cyan);
                Assert.AreEqual(0.21F, yellow65.CmykColor.Magenta);
                Assert.AreEqual(0.6F, yellow65.CmykColor.Yellow);
                Assert.AreEqual(0, yellow65.CmykColor.Black);
            }
        }

        [Test]
        public void Blank()
        {
            VerifyRenderingAndRoundtrip("blank.xmap", RectangleF.FromLTRB(-100, -100, 100, 100), "blank.png");
        }


        [Test]
        public void IsomPoints()
        {
            foreach (string baseName in new[] { "isompoints.xmap", "isompoints.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, RectangleF.FromLTRB(-109, 66, -94, 74), "isompoints_baseline.png");
            }
        }


        [Test]
        public void LargeIsomPoints()
        {
            foreach (string baseName in new[] { "largeisompoints.xmap", "largeisompoints.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, RectangleF.FromLTRB(-118, 62, -76, 87), "largeisompoints_baseline.png");
            }
        }

        [Test]
        public void IsomLines()
        {
            foreach (string baseName in new[] { "isomlines.xmap", "isomlines.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, RectangleF.FromLTRB(-145, 54, -68, 100), "isomlines_baseline.png");
            }
        }

        [Test]
        public void IsomArea()
        {
            foreach (string baseName in new[] { "isomarea.xmap", "isomarea.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, RectangleF.FromLTRB(-218, 103, -197.3F, 115), "isomarea_baseline.png");
            }
        }

        [Test]
        public void IsomAreaNoBitmap()
        {
            foreach (Map map in LoadMaps("isomarea.xmap", "isomarea.omap")) {
                VerifyRenderingNoBitmap(map, RectangleF.FromLTRB(-218, 103, -197.3F, 115), "isomarea_nobm_baseline.png");
            }
        }

        [Test]
        public void SpecialAreas()
        {
            RectangleF rect = RectangleF.FromLTRB(-87, -3, -56, 16);
            foreach (string baseName in new[] { "specialareas.xmap", "specialareas.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "specialareas_baseline.png");
            }
        }

        [Test]
        public void SpecialAreasNoBitmap()
        {
            RectangleF rect = RectangleF.FromLTRB(-87, -3, -56, 16);
            foreach (Map map in LoadMaps("specialareas.xmap", "specialareas.omap")) {
                VerifyRenderingNoBitmap(map, rect, "specialareas_nobm_baseline.png");
            }
        }

        [Test]
        public void RotatedAreas()
        {
            RectangleF rect = RectangleF.FromLTRB(-37, -13, -10, 2);
            foreach (string baseName in new[] { "rotarea.xmap", "rotarea.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "rotarea.png");
            }
        }

        [Test]
        public void RotatedAreasNoBitmap()
        {
            RectangleF rect = RectangleF.FromLTRB(-37, -13, -10, 2);
            foreach (Map map in LoadMaps("rotarea.xmap", "rotarea.omap")) {
                VerifyRenderingNoBitmap(map, rect, "rotarea_nobm_baseline.png");
            }
        }

        [Test]
        public void Text1()
        {
            RectangleF rect = RectangleF.FromLTRB(-160, 4, -80, 68);
            foreach (string baseName in new[] { "text1.xmap", "text1.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "text1_baseline.png");
            }
        }

        [Test]
        public void Text2()
        {
            RectangleF rect = RectangleF.FromLTRB(-210, 3, -38, 118);
            foreach (string baseName in new[] { "text2.xmap", "text2.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "text2_baseline.png");
            }
        }

        [Test]
        public void Text2_Version9()
        {
            RectangleF rect = RectangleF.FromLTRB(-210, 3, -38, 118);
            foreach (string baseName in new[] { "text2_v9.xmap", "text2_v9.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "text2_baseline.png", 9);
            }
        }

        [Test]
        public void Text3()
        {
            RectangleF rect = RectangleF.FromLTRB(-210, 3, -38, 121);
            foreach (string baseName in new[] { "text3.xmap", "text3.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "text3_baseline.png");
            }
        }

        [Test]
        public void Text4()
        {
            RectangleF rect = RectangleF.FromLTRB(-210, 3, -38, 121);
            foreach (string baseName in new[] { "text4.xmap", "text4.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "text4_baseline.png");
            }
        }

        [Test]
        public void Text5()
        {
            RectangleF rect = RectangleF.FromLTRB(-210, 3, -38, 121);
            foreach (string baseName in new[] { "text5.xmap", "text5.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "text5_baseline.png");
            }
        }

        [Test]
        public void Text6()
        {
            RectangleF rect = RectangleF.FromLTRB(-210, 3, -38, 121);
            foreach (string baseName in new[] { "text6.xmap", "text6.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "text6_baseline.png");
            }
        }


        [Test]
        public void Text7()
        {
            RectangleF rect = RectangleF.FromLTRB(-210, 3, -38, 121);
            foreach (string baseName in new[] { "text7.xmap", "text7.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "text7_baseline.png");
            }
        }

        [Test]
        public void Text8()
        {
            RectangleF rect = RectangleF.FromLTRB(-210, 3, -38, 121);
            foreach (string baseName in new[] { "text8.xmap", "text8.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "text8_baseline.png");
            }
        }

        [Test]
        public void Text9()
        {
            RectangleF rect = RectangleF.FromLTRB(-147, 75, -38, 111);
            foreach (string baseName in new[] { "text9.xmap", "text9.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "text9_baseline.png");
            }
        }

        [Test]
        public void DashLengths()
        {
            RectangleF rect = RectangleF.FromLTRB(-26, 44, -12, 52);
            foreach (string baseName in new[] { "dashlength.xmap", "dashlength.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "dashlength_baseline.png");
            }
        }

        [Test]
        public void DashPoints()
        {
            RectangleF rect = RectangleF.FromLTRB(-27, 40, -8, 54);
            foreach (string baseName in new[] { "dashpoint.xmap", "dashpoint.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "dashpoint_baseline.png");
            }
        }

        [Test]
        public void LineSymSpacing()
        {
            RectangleF rect = RectangleF.FromLTRB(-27, 39, -14, 55);
            foreach (string baseName in new[] { "linesymspacing.xmap", "linesymspacing.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "linesymspacing_baseline.png");
            }
        }


        [Test]
        public void LineSymDashPoints()
        {
            RectangleF rect = RectangleF.FromLTRB(-29, 40, -14, 53);
            foreach (string baseName in new[] { "symdashpoint.xmap", "symdashpoint.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "symdashpoint_baseline.png");
            }
        }

        [Test]
        public void ComboLines()
        {
            RectangleF rect = RectangleF.FromLTRB(-98, 17, -45, 37);
            foreach (string baseName in new[] { "comboline1.xmap", "comboline1.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "comboline1_baseline.png");
            }
        }

        [Test]
        public void ComboArea()
        {
            RectangleF rect = RectangleF.FromLTRB(-99, 33, -57, 58);
            foreach (string baseName in new[] { "comboarea.xmap", "comboarea.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "comboarea.png");
            }
        }

        [Test]
        public void RegMarks()
        {
            RectangleF rect = RectangleF.FromLTRB(-50, 20, -34, 33);
            foreach (string baseName in new[] { "regmarks.xmap", "regmarks.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "regmarks.png");
            }
        }

        
        [Test]
        public void SPUMap()
        {
            RectangleF rect = RectangleF.FromLTRB(-150, -225, -20, -20);
            foreach (string baseName in new[] { "SPU_PP.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "SPU.png");
            }
        }

        [Test]
        public void NSCMap()
        {
            RectangleF rect = RectangleF.FromLTRB(-90, -125, 115, 150);
            foreach (string baseName in new[] { "NSeattleCollege.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "NSeattleCollege.png");
            }
        }
    
        
        [Test]
        public void DotSpacing()
        {
            RectangleF rect = RectangleF.FromLTRB(80, -116, 86, -110);
            foreach (string baseName in new[] { "dotspacing.xmap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "dotspacing.png");
            }
        }

        [Test]
        public void DotSpacingDash()
        {
            RectangleF rect = RectangleF.FromLTRB(80, -116, 95, -110);
            foreach (string baseName in new[] { "dotspacing_dash.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "dotspacing_dash.png");
            }
        }

        [Test]
        public void FenceSpacing()
        {
            RectangleF rect = RectangleF.FromLTRB(16, -86, 42, -68);
            foreach (string baseName in new[] { "fencespacing.xmap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "fencespacing.png");
            }
        }

        [Test]
        public void FenceDashPoint()
        {
            RectangleF rect = RectangleF.FromLTRB(119, -75, 127.5F, -68);
            foreach (string baseName in new[] { "fencedashpoint.xmap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "fencedashpoint.png");
            }
        }


        [Test]
        public void Wholestructure1()
        {
            RectangleF rect = RectangleF.FromLTRB(-20, -4.8F, 4.35F, 10.25F);
            foreach (string baseName in new[] { "wholestructure.xmap", "wholestructure.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "wholestructure.png");
            }
        }

        [Test]
        public void Wholestructure2()
        {
            RectangleF rect = RectangleF.FromLTRB(-26.15F, -7.5F, 9.2F, 14.65F);
            foreach (string baseName in new[] { "wholestructure2.xmap", "wholestructure2.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "wholestructure2.png");
            }
        }

        [Test]
        public void Wholestructure3()
        {
            RectangleF rect = RectangleF.FromLTRB(-20, -4.8F, 4.35F, 10.25F);
            foreach (string baseName in new[] { "wholestructure3.xmap", "wholestructure3.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "wholestructure3.png");
            }
        }

        [Test]
        public void Wholestructure4()
        {
            RectangleF rect = RectangleF.FromLTRB(-26.15F, -7.5F, 9.2F, 14.65F);
            foreach (string baseName in new[] { "wholestructure4.xmap", "wholestructure4.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "wholestructure4.png");
            }
        }

        [Test]
        public void HelperSymbols()
        {
            RectangleF rect = RectangleF.FromLTRB(-48F, 26F, -34F, 39F);
            foreach (string baseName in new[] { "helpers.xmap", "helpers.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "helpers.png");
            }
        }

        [Test]
        public void Georeferencing1()
        {
            foreach (string basename in new[] { "georef1.xmap", "georef1.omap" }) {
                Map map = LoadMap(basename);
                using (map.Read()) {
                    RealWorldCoords realWorldCoords = map.RealWorldCoords;
                    Assert.AreEqual(15000, map.MapScale);
                    Assert.AreEqual(15000, map.PrintScale);

                    Assert.AreEqual(-20.37, realWorldCoords.RealWorldAngle, 0.00001);
                    Assert.AreEqual(732828, realWorldCoords.RealWorldOffsetX, 1);
                    Assert.AreEqual(-185, realWorldCoords.RealWorldOffsetY, 1);
                    Assert.AreEqual(2023, realWorldCoords.RealWorldGridAndZone);
                }

                // RoundTrip
                Map newMap = RoundTripMap(map, basename);
                using (newMap.Read()) {
                    RealWorldCoords realWorldCoords = newMap.RealWorldCoords;
                    Assert.AreEqual(15000, newMap.MapScale);
                    Assert.AreEqual(15000, newMap.PrintScale);

                    Assert.AreEqual(-20.37, realWorldCoords.RealWorldAngle, 0.00001);
                    Assert.AreEqual(732828, realWorldCoords.RealWorldOffsetX, 1);
                    Assert.AreEqual(-185, realWorldCoords.RealWorldOffsetY, 1);
                    Assert.AreEqual(2023, realWorldCoords.RealWorldGridAndZone);
                }
            }
        }

        [Test]
        public void Georeferencing2()
        {
            foreach (Map map in LoadMaps("georef_none.xmap")) {
                using (map.Read()) {
                    RealWorldCoords realWorldCoords = map.RealWorldCoords;
                    Assert.AreEqual(15000, map.MapScale);
                    Assert.AreEqual(15000, map.PrintScale);

                    Assert.AreEqual(0, realWorldCoords.RealWorldGridAndZone);
                }
            }
        }

        [Test]
        public void GeoreferencingEpsg()
        {
            foreach (string basename in new[] { "georef_epsg.xmap", "georef_epsg.omap" }) {
                Map map = LoadMap(basename);
                using (map.Read()) {
                    RealWorldCoords realWorldCoords = map.RealWorldCoords;
                    Assert.AreEqual(0, realWorldCoords.RealWorldGridAndZone);
                    Assert.AreEqual(2393, realWorldCoords.Epsg);
                    Assert.AreEqual("+proj=tmerc +lat_0=0 +lon_0=27 +k=1 +x_0=3500000 +y_0=0 +ellps=intl +units=m +no_defs ", realWorldCoords.Proj4String);
                    Assert.AreEqual(MapProjectionType.Known, realWorldCoords.ProjectionType);
                }

                // RoundTrip
                Map newMap = RoundTripMap(map, basename);
                using (newMap.Read()) {
                    RealWorldCoords realWorldCoords = newMap.RealWorldCoords;
                    Assert.AreEqual(0, realWorldCoords.RealWorldGridAndZone);
                    Assert.AreEqual(2393, realWorldCoords.Epsg);
                    Assert.AreEqual("+proj=tmerc +lat_0=0 +lon_0=27 +k=1 +x_0=3500000 +y_0=0 +ellps=intl +units=m +no_defs ", realWorldCoords.Proj4String);
                    Assert.AreEqual(MapProjectionType.Known, realWorldCoords.ProjectionType);
                }
            }
        }

        [Test]
        public void GeoreferencingGk()
        {
            foreach (string basename in new[] { "georef_gk.xmap", "georef_gk.omap" }) {
                Map map = LoadMap(basename);
                using (map.Read()) {
                    RealWorldCoords realWorldCoords = map.RealWorldCoords;
                    Assert.AreEqual(0, realWorldCoords.RealWorldGridAndZone);
                    Assert.AreEqual(0, realWorldCoords.Epsg);
                    Assert.AreEqual("+proj=tmerc +lat_0=0 +lon_0=12 +k=1.000000 +x_0=4500000 +y_0=0 +ellps=bessel +datum=potsdam +units=m +no_defs", realWorldCoords.Proj4String);
                    Assert.AreEqual(MapProjectionType.Known, realWorldCoords.ProjectionType);
               }

                // RoundTrip
                Map newMap = RoundTripMap(map, basename);
                using (newMap.Read()) {
                    RealWorldCoords realWorldCoords = newMap.RealWorldCoords;
                    Assert.AreEqual(0, realWorldCoords.RealWorldGridAndZone);
                    Assert.AreEqual(0, realWorldCoords.Epsg);
                    Assert.AreEqual("+proj=tmerc +lat_0=0 +lon_0=12 +k=1.000000 +x_0=4500000 +y_0=0 +ellps=bessel +datum=potsdam +units=m +no_defs", realWorldCoords.Proj4String);
                    Assert.AreEqual(MapProjectionType.Known, realWorldCoords.ProjectionType);
                }
            }
        }

        [Test]
        public void GeoreferencingProj4()
        {
            foreach (string basename in new[] { "georef_proj4.xmap", "georef_proj4.omap" }) {
                Map map = LoadMap(basename);
                using (map.Read()) {
                    RealWorldCoords realWorldCoords = map.RealWorldCoords;
                    Assert.AreEqual(0, realWorldCoords.RealWorldGridAndZone);
                    Assert.AreEqual(0, realWorldCoords.Epsg);
                    Assert.AreEqual("+proj=lcc +lat_1=48.33333333333334 +lat_0=48.33333333333334 +lon_0=-105.5 +k_0=1.00012 +x_0=199999.9999992 +y_0=99999.99999960001 +ellps=GRS80 +units=ft +no_defs", realWorldCoords.Proj4String);
                    Assert.AreEqual(MapProjectionType.Known, realWorldCoords.ProjectionType);
                }

                // RoundTrip
                Map newMap = RoundTripMap(map, basename);
                using (newMap.Read()) {
                    RealWorldCoords realWorldCoords = newMap.RealWorldCoords;
                    Assert.AreEqual(0, realWorldCoords.RealWorldGridAndZone);
                    Assert.AreEqual(0, realWorldCoords.Epsg);
                    Assert.AreEqual("+proj=lcc +lat_1=48.33333333333334 +lat_0=48.33333333333334 +lon_0=-105.5 +k_0=1.00012 +x_0=199999.9999992 +y_0=99999.99999960001 +ellps=GRS80 +units=ft +no_defs", realWorldCoords.Proj4String);
                    Assert.AreEqual(MapProjectionType.Known, realWorldCoords.ProjectionType);
                }
            }
        }

        [Test]
        public void MapNotes()
        {
            string expected = "These are my cool map <notes>.\r\n\r\nI have some interesting characters. âãäÔ¿ÇĆáǾǼʣΔΣ";

            foreach (string basename in new[] { "georef1.xmap", "georef1.omap" }) {
                Map map = LoadMap(basename);
                using (map.Read()) {
                    string mapNotes = map.FileInformation;

                    Assert.AreEqual(expected, mapNotes);
                }

                // RoundTrip
                Map newMap = RoundTripMap(map, basename);
                using (newMap.Read()) {
                    string mapNotes = newMap.FileInformation;

                    Assert.AreEqual(expected, mapNotes);
                }
            }
        }

        [Test]
        public void PrintSettings()
        {
            foreach (string basename in new[] { "printarea.xmap", "printarea.omap"}) {
                Map map = LoadMap(basename);
                using (map.Read()) {
                    float printScale = map.PrintScale;
                    Assert.AreEqual(19000, printScale);
                    RectangleF printArea = map.PrintArea;
                    Assert.AreEqual(new RectangleF(-50, 30, 15, 10), printArea);
                }
                
                // RoundTrip
                Map newMap = RoundTripMap(map, basename);
                using (newMap.Read()) {
                    float printScale = newMap.PrintScale;
                    Assert.AreEqual(19000, printScale);
                    RectangleF printArea = newMap.PrintArea;
                    Assert.AreEqual(new RectangleF(-50, 30, 15, 10), printArea);
                }

            }
        }

        [Test]
        public void Templates()
        {
            RectangleF rect = RectangleF.FromLTRB(-50, -50, 75, 35);
            foreach (string baseName in new[] { "template1.xmap", "template1.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "template1.png");
            }
        }

        [Test]
        public void MapTemplate()
        {
            RectangleF rect = RectangleF.FromLTRB(10, -25, 220, 265);
            foreach (string baseName in new[] { "massysprint22.03.17.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "massysprint.png");
            }
        }

        [Test]
        public void MissingFonts()
        {
            foreach (Map map in LoadMaps("missingfont.xmap", "missingfont.omap")) {
                string[] missingFonts = map.MissingFonts;
                Assert.AreEqual(1, missingFonts.Length);
                Assert.AreEqual("Ambrosia", missingFonts[0]);
            }
        }

        [Test]
        public void BadLineObjects()
        {
            RectangleF rect = RectangleF.FromLTRB(-65.5F, -58, 136, 56);
            foreach (string baseName in new[] { "HG_Mar28.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "HG_Mar28.png");
            }
        }

        [Test]
        public void CutCircles()
        {
            RectangleF rect = RectangleF.FromLTRB(-16, -20, 35, 27);
            foreach (string baseName in new[] { "cutcircles.ocd" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "cutcircles.png");
            }
        }

        [Test]
        public void TextBlankLines()
        {
            RectangleF rect = RectangleF.FromLTRB(-349, 5, -192, 51);
            foreach (string baseName in new[] { "text_blank_lines.omap", "text_blank_lines.xmap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "text_blank_lines.png");
            }
        }

        [Test]
        public void CornerScaling()
        {
            RectangleF rect = RectangleF.FromLTRB(-17, -17, 17, 13);
            foreach (string baseName in new[] { "cornerscaling.xmap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "cornerscaling.png");
            }
        }

        [Test]
        public void NewPointedEnds()
        {
            RectangleF rect = RectangleF.FromLTRB(-5.75F, 15, 1, 20.75F);
            foreach (string baseName in new[] { "newpointedends.xmap", "newpointedends.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "newpointedends.png", 9);
            }
        }

        [Test]
        public void MidSymbolsVersion9()
        {
            RectangleF rect = RectangleF.FromLTRB(-62, 38, -19, 78);
            foreach (string baseName in new[] { "midsymbols_v9.xmap", "midsymbols_v9.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "midsymbols_v9.png", 9);
            }
        }

        [Test]
        public void BadVersion()
        {
            string mapName = TestUtil.GetTestFile("openmapper\\" + "BCCH_rev_Oct25.omap");
            Assert.IsTrue(InputOutput.IsOpenMapperFile(mapName));

            try {
                Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("openmapper")));
                InputOutput.ReadFile(mapName, map);
                Assert.Fail("Should throw Exception");
            }
            catch (Exception e) {
                Assert.AreEqual("File format error in file '" + mapName + "':\r\nOpenOrienteering file format version 5 is not supported; only versions 6 through 9 are supported.", 
                    e.Message);
            }
        }

        [Test]
        public void MultiLineBelow()
        {
            RectangleF rect = RectangleF.FromLTRB(-16, -94, 94, 10);
            foreach (string baseName in new[] { "multilinebelow.xmap", "multilinebelow.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "multilinebelow_baseline.png");
            }
        }


        [Test]
        public void BadHatching()
        {
            RectangleF rect = RectangleF.FromLTRB(130, 73, 187, 120);
            foreach (string baseName in new[] { "badhatching.xmap", "badhatching.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "badhatching_baseline.png", 9);
            }
        }

        [Test]
        public void GeorefTemplate()
        {
            RectangleF rect = RectangleF.FromLTRB(-205, -85, 120, 200);
            foreach (string baseName in new[] { "GeorefTemplate.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "GeorefTemplate.png", 9);
            }
        }

        [Test]
        public void GeorefTemplate2()
        {
            RectangleF rect = RectangleF.FromLTRB(-130, -255, 130, 260);
            foreach (string baseName in new[] { "whitekirk-utm30.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "whitekirk-utm30.png", 9);
            }
        }

        [Test]
        public void GeorefTemplate3()
        {
            RectangleF rect = RectangleF.FromLTRB(-130, -255, 130, 260);
            foreach (string baseName in new[] { "whitekirk-3857.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "whitekirk-3857.png", 9);
            }
        }

        [Test]
        public void GeorefTemplate4()
        {
            RectangleF rect = RectangleF.FromLTRB(-130, -255, 130, 260);
            foreach (string baseName in new[] { "whitekirk-27700.omap" }) {
                VerifyRenderingAndRoundtrip(baseName, rect, "whitekirk-27700.png", 9);
            }
        }

        [Test]
        public void Overprinting()
        {
            RectangleF rect = RectangleF.FromLTRB(-19.37F, -12.27F, 18.86F, 9.87F);
            VerifyRenderingAndRoundtripOverprint("overprinting.omap", rect, "overprinting.png", 9);
        }

    }
}

#endif //TEST
