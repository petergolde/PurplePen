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
using System.IO;
using NUnit.Framework;
using TestingUtils;

namespace PurplePen.MapModel.Tests
{
    [TestFixture]
    public class MapTests
    {
        // Check that we can read the template info from the give file name and that it matches what is expected.
        private void CheckReadTemplateInfo(string filename, TemplateInfo expected)
        {
            // Create and open the map file.
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(Path.GetDirectoryName(filename)));
            InputOutput.ReadFile(filename, map);

            TemplateInfo actual;
            using (map.Read())
                actual = map.Templates[0];

            Assert.IsTrue(string.Compare(expected.absoluteFileName, actual.absoluteFileName, StringComparison.InvariantCultureIgnoreCase) == 0, "file names don't match");
            Assert.AreEqual(expected.angle, actual.angle, 0.0001F);
            Assert.AreEqual(expected.centerPoint, actual.centerPoint);
            Assert.AreEqual(expected.dpi, actual.dpi, 0.0001F);
            Assert.AreEqual(expected.scaleX, actual.scaleX, 0.0001F);
            Assert.AreEqual(expected.scaleY, actual.scaleY, 0.0001F);
            Assert.AreEqual(expected.shearAngle, actual.shearAngle, 0.0001F);
            Assert.AreEqual(expected.visible, actual.visible);
        }

        // Check that we can read the template info from the give file name and that it matches what is expected.
        private void CheckReadTemplateInfos(string filename, TemplateInfo[] expected)
        {
            // Create and open the map file.
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(Path.GetDirectoryName(filename)));
            InputOutput.ReadFile(filename, map);

            IList<TemplateInfo> actual;
            using (map.Read())
                actual = map.Templates;

            for (int i = 0; i < actual.Count; ++i) {
                Assert.IsTrue(string.Compare(expected[i].absoluteFileName, actual[i].absoluteFileName, StringComparison.InvariantCultureIgnoreCase) == 0, "file names don't match");
                Assert.AreEqual(expected[i].angle, actual[i].angle, 0.0001F);
                Assert.AreEqual(expected[i].centerPoint, actual[i].centerPoint);
                Assert.AreEqual(expected[i].dpi, actual[i].dpi, 0.0001F);
                Assert.AreEqual(expected[i].scaleX, actual[i].scaleX, 0.0001F);
                Assert.AreEqual(expected[i].scaleY, actual[i].scaleY, 0.0001F);
                Assert.AreEqual(expected[i].shearAngle, actual[i].shearAngle, 0.0001F);
                Assert.AreEqual(expected[i].visible, actual[i].visible);
            }
        }


        [Test]
        // Make sure that we can read the template information from a file.
        public void ReadTemplateInfo()
        {
            CheckReadTemplateInfo(TestUtil.GetTestFile(@"io\template6.ocd"), new TemplateInfo(@"C:\Documents and Settings\Peter\My Documents\CourseScribe\TestFiles\io\lp3.bmp", new PointF(-22.91F, -0.38F), 300F, (float)(-0.103743531740861 * 180.0 / Math.PI), true));
        }

        [Test]
        // Make sure that we can read the template information from a file.
        public void ReadTemplateInfo9()
        {
            CheckReadTemplateInfo(TestUtil.GetTestFile(@"io\template9.ocd"), new TemplateInfo(TestUtil.GetTestFile(@"io\lp3.bmp"), new PointF(18.53543F, -6.157969F), 90.1276932F, 0.7513647F, 1.0F, 0.841129482F, -7.80118727F, true));
        }

        [Test]
        // Make sure that we can read the template information from a file.
        public void ReadMultiTemplateInfo9()
        {
            CheckReadTemplateInfos(TestUtil.GetTestFile(@"io\multitemplate9.ocd"), 
                new TemplateInfo[] {
                    new TemplateInfo(TestUtil.GetTestFile(@"io\Winter.jpg"), new PointF(-44.7609367F, 16.1286335F), 997.831848F, -40.1391335F, 1.0F, 1.88852251F, -30.0885315F, true),
                    new TemplateInfo(TestUtil.GetTestFile(@"io\Water lilies.jpg"), new PointF(-7.543477F, 7.235508F), 348.324F, -40.81563F, 1.0F, 1.04275668F, -26.53024F, true),
                    new TemplateInfo(TestUtil.GetTestFile(@"C:\Users\Peter\Documents\temp\Lincoln-CMYK.bmp"), new PointF(-27.7163277F, 5.712031F), 401.0379F, 13.3253765F, 1.0F, 1.19724464F, 3.447377F, true)
                });
        }

        [Test]
        // Make sure we can read template info with accented chararacters in it.
        public void ReadAccentedTemplateInfo()
        {
            CheckReadTemplateInfo(TestUtil.GetTestFile(@"io\accenttemplate9.ocd"), new TemplateInfo(TestUtil.GetTestFile(@"io\rähräh.jpg"), new PointF(0, 0), 300, 0, true));
        }

        void DumpMapFile(string mapFileName, string outputDump)
        {
            using (TextWriter writer = new StreamWriter(outputDump, false, System.Text.Encoding.UTF8)) {
                DebugCode.OcadDump dump = new DebugCode.OcadDump(TestUtil.GetTestFileDirectory());
                dump.DumpFile(mapFileName, writer);
            }
        }

        void DumpMap(Map map, int version, string mapFileName, string outputDump)
        {
            InputOutput.WriteFile(mapFileName, map, new MapFileFormat(MapFileFormatKind.OCAD, version));

            DumpMapFile(mapFileName, outputDump);
        }

        void CheckDump(Map map, int version, string expectedDumpFile)
        {
            string directory = Path.GetDirectoryName(expectedDumpFile);
            string basename = Path.GetFileNameWithoutExtension(expectedDumpFile);
            string mapNewFileName = directory + @"\" + basename + @"_new_temp.ocd";
            string dumpNewFileName = directory + @"\" + basename + @"_new_dump_temp.txt";

            DumpMap(map, version, mapNewFileName, dumpNewFileName);

            TestUtil.CompareTextFileBaseline(dumpNewFileName, expectedDumpFile);
            File.Delete(dumpNewFileName);
        }


        [Test]
        // Make sure that we can write the template information to a file.
        public void WriteTemplateInfo()
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("io")));
            using (map.Write()) {
                map.MapScale = 15000;
                map.Templates = new TemplateInfo[] { new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\k.ocd"), new PointF(4.0F, 5.0F), 330F, 7.4F, true) };
            }

            CheckDump(map, 7, TestUtil.GetTestFile(@"io\outputtemplate6_dump.txt"));
        }

        [Test]
        // Make sure that we can write the template information to a file.
        public void WriteTemplateInfo8()
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("io")));
            using (map.Write()) {
                map.MapScale = 15000;
                map.Templates = new TemplateInfo[] { new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\k.ocd"), new PointF(4.0F, 5.0F), 330F, 7.4F, true) };
            }

            CheckDump(map, 8, TestUtil.GetTestFile(@"io\outputtemplate8_dump.txt"));
        }

        [Test]
        // Make sure that we can write the template information to a file.
        public void WriteTemplateInfo9()
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("io")));
            using (map.Write()) {
                map.MapScale = 15000;
                map.Templates = new TemplateInfo[] { new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\k.ocd"), new PointF(4.0F, 5.0F), 330F, 7.4F, true) };
            }

            CheckDump(map, 9, TestUtil.GetTestFile(@"io\outputtemplate9_dump.txt"));
        }

        [Test]
        // Make sure that we can write the template information to a file.
        public void WriteMultiTemplateInfo9()
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("io")));
            using (map.Write()) {
                map.MapScale = 15000;
                map.Templates = new TemplateInfo[] { 
                    new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\k.ocd"), new PointF(4.0F, 5.0F), 330F, 7.4F, 1.0F, 0.34F, -18.4F, true),
                    new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\l.jpg"), new PointF(7F, -5F), 1000F, -12.5F, 1.0F, 1.99F, 1.4F, false),
                    new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\m.bmp"), new PointF(12F, -5.673F), 15F, 7.4F, 1.0F, 1.0F, 45.4F, true),
                
                };
            }

            CheckDump(map, 9, TestUtil.GetTestFile(@"io\outputmultitemplate9_dump.txt"));
        }

        [Test]
        // Make sure we can round trip the template information through a file.
        public void RoundTripTemplate()
        {
            TemplateInfo template = new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\k.ocd"), new PointF(4.0F, 5.0F), 330F, 7.4F, true);
            string filename = TestUtil.GetTestFile(@"io\outputtemplate_temp.ocd");

            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("io")));
            using (map.Write())
                map.Templates = new TemplateInfo[] { template };

            InputOutput.WriteFile(filename, map, new MapFileFormat(MapFileFormatKind.OCAD, 7));

            CheckReadTemplateInfo(filename, template);
        }

        [Test]
        // Make sure we can round trip the template information through a file.
        public void RoundTripTemplate8()
        {
            TemplateInfo template = new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\k.ocd"), new PointF(4.0F, 5.0F), 330F, 7.4F, true);
            string filename = TestUtil.GetTestFile(@"io\outputtemplate_temp.ocd");

            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("io")));
            using (map.Write())
                map.Templates = new TemplateInfo[] { template };

            InputOutput.WriteFile(filename, map, new MapFileFormat(MapFileFormatKind.OCAD, 8));

            CheckReadTemplateInfo(filename, template);
        }

        [Test]
        // Make sure we can round trip the template information through a file.
        public void RoundTripTemplate9()
        {
            TemplateInfo template = new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\k.ocd"), new PointF(4.0F, 5.0F), 330F, 7.4F, true);
            string filename = TestUtil.GetTestFile(@"io\outputtemplate_temp.ocd");

            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("io")));
            using (map.Write())
                map.Templates = new TemplateInfo[] { template };

            InputOutput.WriteFile(filename, map, new MapFileFormat(MapFileFormatKind.OCAD, 9));

            CheckReadTemplateInfo(filename, template);
        }

        [Test]
        // Make sure we can round trip the template information through a file.
        public void RoundTripMultiTemplate9()
        {
            TemplateInfo[] templates = 
                {
                    new TemplateInfo(TestUtil.GetTestFile(@"io\Winter.jpg"), new PointF(-44.76F, 16.1F), 997F, -40.1395F, 1.0F, 1.88F, -30.088F, true),
                    new TemplateInfo(TestUtil.GetTestFile(@"io\Water lilies.jpg"), new PointF(-7.54F, 7.28F), 348.3F, -40.81F, 1.0F, 1.04F, -26.530F, true),
                    new TemplateInfo(TestUtil.GetTestFile(@"C:\Users\Peter\Documents\temp\Lincoln-CMYK.bmp"), new PointF(-27.2F, 5.712F), 401.03F, 13.325F, 1.0F, 1.1972F, 3.447F, true)
                };

            string filename = TestUtil.GetTestFile(@"io\outputmultitemplate_temp.ocd");

            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("io")));
            using (map.Write())
                map.Templates = templates;
            
            InputOutput.WriteFile(filename, map, new MapFileFormat(MapFileFormatKind.OCAD, 9));

            CheckReadTemplateInfos(filename, templates);
        }

        // Check that we can read the real world coords from the give file name and that it matches what is expected.
        private void CheckReadRealWorldCoords(string filename, RealWorldCoords expected, MapProjectionType projectionTypeExpected, string proj4StringExpected)
        {
            // Create and open the map file.
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(Path.GetDirectoryName(filename)));
            InputOutput.ReadFile(filename, map);

            RealWorldCoords actual;
            using (map.Read())
                actual = map.RealWorldCoords;

            Assert.AreEqual(expected.PaperGridDistance, actual.PaperGridDistance, 0.0001);
            Assert.AreEqual(expected.RealWorldOn, actual.RealWorldOn);
            Assert.AreEqual(expected.RealWorldAngle, actual.RealWorldAngle, 0.0001);
            Assert.AreEqual(expected.RealWorldGridDistance, actual.RealWorldGridDistance, 1);
            Assert.AreEqual(expected.RealWorldOffsetX, actual.RealWorldOffsetX, 0.0001);
            Assert.AreEqual(expected.RealWorldOffsetY, actual.RealWorldOffsetY, 0.0001);
            Assert.AreEqual(expected.RealWorldLocalOffsetX, actual.RealWorldLocalOffsetX, 0.0001);
            Assert.AreEqual(expected.RealWorldLocalOffsetY, actual.RealWorldLocalOffsetY, 0.0001);
            Assert.AreEqual(expected.RealWorldGridAndZone, actual.RealWorldGridAndZone);

            Assert.AreEqual(projectionTypeExpected, actual.ProjectionType);
            Assert.AreEqual(proj4StringExpected, actual.Proj4String);
        }

        [Test] 
        public void ReadRealWorldCoords11()
        {
            var expected = new RealWorldCoords() {
                RealWorldOn = true,
                RealWorldGridAndZone = -2003,
                RealWorldOffsetX = 431983,
                RealWorldOffsetY = -734682,
                RealWorldAngle = 34.1998,
                PaperGridDistance = 50,
                RealWorldGridDistance = 72,
                RealWorldLocalOffsetX = 5.71,
                RealWorldLocalOffsetY = -7.22,
            };

            CheckReadRealWorldCoords(TestUtil.GetTestFile(@"io\realworldcoord11.ocd"), expected, MapProjectionType.Known, @"+proj=utm +zone=3 +south +datum=WGS84 +units=m +no_defs");
        }

        [Test]
        public void ReadRealWorldCoords7()
        {
            var expected = new RealWorldCoords() {
                RealWorldOn = true,
                RealWorldGridAndZone = 0,
                RealWorldOffsetX = -432153,
                RealWorldOffsetY = 244332,
                RealWorldAngle = 289.34,
                PaperGridDistance = 500,
                RealWorldGridDistance = 173,
                RealWorldLocalOffsetX = 0,
                RealWorldLocalOffsetY = 0,
            };

            CheckReadRealWorldCoords(TestUtil.GetTestFile(@"io\realworldcoord7.ocd"), expected, MapProjectionType.None, null);
        }

        [Test]
        public void RoundTripRealWorldCoords()
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFileDirectory()));

            var expected = new RealWorldCoords() {
                RealWorldOn = true,
                RealWorldGridAndZone = -2003,
                RealWorldOffsetX = 431983,
                RealWorldOffsetY = -734682,
                RealWorldAngle = 34.1998,
                PaperGridDistance = 50,
                RealWorldGridDistance = 72,
                RealWorldLocalOffsetX = 5.71,
                RealWorldLocalOffsetY = -7.22,
            };

            using (map.Write()) {
                map.MapScale = 2400;
                map.RealWorldCoords = expected;
            }

            CheckDump(map, 11, TestUtil.GetTestFile(@"io\roundtriprealworldcoords11.txt"));
            CheckDump(map, 8, TestUtil.GetTestFile(@"io\roundtriprealworldcoords8.txt"));
        }


        [Test]
        public void Bounds()
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("io")));
            SymColor black;
            RectangleF result;
            LineSymDef lineSymDef;
            LineSymbol sym1, sym2;

            // No symbols -- empty bounds.
            using (map.Read())
                result = map.Bounds;
            Assert.AreEqual(new RectangleF(), result);

            // Add a symdef, --should still be empty
            using (map.Write()) {
                black = map.AddColor("black", 1, 0, 0, 0, 1.0F, false);
                lineSymDef = new LineSymDef("line", "100.0", black, 0.5F, LineJoin.Round, LineCap.Round);
                map.AddSymdef(lineSymDef);
            }

            using (map.Read())
                result = map.Bounds;
            Assert.AreEqual(new RectangleF(), result);

            // Add one symbol
            using (map.Write()) {
                sym1 = new LineSymbol(lineSymDef, new SymPath(new PointF[] { new PointF(5,5), new PointF(10,20), new PointF(20, 3)}));
                map.AddSymbol(sym1);
            }

            using (map.Read())
                result = map.Bounds;
            Assert.AreEqual(RectangleF.FromLTRB(4.75F, 2.75F, 20.25F, 20.25F), result);

            // Add another symbol
            using (map.Write()) {
                sym2 = new LineSymbol(lineSymDef, new SymPath(new PointF[] { new PointF(-7, 14), new PointF(-9, 33) }));
                map.AddSymbol(sym2);
            }

            using (map.Read())
                result = map.Bounds;
            Assert.AreEqual(RectangleF.FromLTRB(-9.25F, 2.75F, 20.25F, 33.25F), result);

            // Remove 1st symbol.
            using (map.Write()) {
                map.RemoveSymbol(sym1);
            }

            using (map.Read())
                result = map.Bounds;
            Assert.AreEqual(RectangleF.FromLTRB(-9.25F, 13.75F, -6.75F, 33.25F), result);

            // Make sure 2nd time works the same.
            using (map.Read())  
                result = map.Bounds;
            Assert.AreEqual(RectangleF.FromLTRB(-9.25F, 13.75F, -6.75F, 33.25F), result);

            // Add hidden symdef.
            LineSymDef hiddenSymDef;
            using (map.Write()) {
                hiddenSymDef = new LineSymDef("line", "100.0", black, 1.0F, LineJoin.Round, LineCap.Round);
                map.AddSymdef(hiddenSymDef);
                map.SetSymdefVisible(hiddenSymDef, false);
            }

            Symbol sym3;
            // Add hidden symbol
            using (map.Write()) {
                sym3 = new LineSymbol(hiddenSymDef, new SymPath(new PointF[] { new PointF(-14, 20), new PointF(-17, 44) }));
                map.AddSymbol(sym3);
            }

            // Hidden symbol shouldn't change bounds.
            using (map.Read())
                result = map.Bounds;
            Assert.AreEqual(RectangleF.FromLTRB(-9.25F, 13.75F, -6.75F, 33.25F), result);

            // Make symdef unhidden.
            using (map.Write()) {
                map.SetSymdefVisible(hiddenSymDef, true);
            }

            // Bounds should update.
            using (map.Read())
                result = map.Bounds;
            Assert.AreEqual(RectangleF.FromLTRB(-17.5F, 13.75F, -6.75F, 44.5F), result);

            // Make symdef hidden.
            using (map.Write()) {
                map.SetSymdefVisible(hiddenSymDef, false);
            }

            // Bounds go back..
            using (map.Read())
                result = map.Bounds;
            Assert.AreEqual(RectangleF.FromLTRB(-9.25F, 13.75F, -6.75F, 33.25F), result);


        }
	

        void TestPrintArea(string filename, RectangleF expectedPrintArea)
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("io")));
            InputOutput.ReadFile(filename, map);

            RectangleF printArea;

            using (map.Read()) {
                printArea = map.PrintArea;
            }

            Assert.AreEqual(expectedPrintArea, printArea);
        }

        [Test]
        public void ReadPrintArea6()
        {
            TestPrintArea(TestUtil.GetTestFile(@"io\printarea6.ocd"), RectangleF.FromLTRB(-30.5F, 12F, 111F, 98.6F));
        }

        [Test]
        public void ReadPrintArea9()
        {
            TestPrintArea(TestUtil.GetTestFile(@"io\printarea9.ocd"), RectangleF.FromLTRB(-30.5F, 12F, 111F, 98.6F));
        }

        void TestScales(string filename, float expectedMapScale, float expectedPrintScale)
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("io")));
            InputOutput.ReadFile(filename, map);

            float mapScale, printScale;

            using (map.Read()) {
                mapScale = map.MapScale;
                printScale = map.PrintScale;
            }

            Assert.AreEqual(expectedMapScale, mapScale);
            Assert.AreEqual(expectedPrintScale, printScale);
        }

        [Test]
        public void ReadScales6()
        {
            TestScales(TestUtil.GetTestFile(@"io\scales6.ocd"), 7000, 5500);
        }

        [Test]
        public void ReadScales9()
        {
            TestScales(TestUtil.GetTestFile(@"io\scales9.ocd"), 7200, 2500);
        }

        void TestWriteScales(int version, string expectedDumpFileName)
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("io")));

            using (map.Write()) {
                map.MapScale = 7200;
                map.PrintScale = 2700;
            }

            CheckDump(map, version, expectedDumpFileName);
        }

        [Test]
        public void WriteScales6()
        {
            TestWriteScales(6, TestUtil.GetTestFile(@"io\writescales6_dump.txt"));
        }

        [Test]
        public void WriteScales9()
        {
            TestWriteScales(9, TestUtil.GetTestFile(@"io\writescales9_dump.txt"));
        }

        void TestWritePrintArea(int version, string expectedDumpFileName)
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("io")));

            using (map.Write()) {
                map.MapScale = 15000;
                map.PrintScale = 10000;
                map.PrintArea = RectangleF.FromLTRB(-30.5F, 12F, 111F, 98.6F);
            }

            CheckDump(map, version, expectedDumpFileName);
        }

        [Test]
        public void WritePrintArea6()
        {
            TestWritePrintArea(6, TestUtil.GetTestFile(@"io\writeprintarea6_dump.txt"));
        }

        [Test]
        public void WritePrintArea9()
        {
            TestWritePrintArea(9, TestUtil.GetTestFile(@"io\writeprintarea9_dump.txt"));
        }

        [Test]
        public void GetFreeOcadId()
        {
            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("io")));
            InputOutput.ReadFile(TestUtil.GetTestFile(@"io\isompoints.ocd"), map);

            List<SymColor> colors;

            using (map.Read())
                colors = new List<SymColor>(map.AllColors);

            string ocadId;
            using (map.Read())
                ocadId = map.GetFreeSymbolId(310);
            Assert.AreEqual("310.0", ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymbolId(310);
            Assert.AreEqual("310.2", ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymbolId(310);
            Assert.AreEqual("310.3", ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymbolId(310);
            Assert.AreEqual("310.4", ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymbolId(310);
            Assert.AreEqual("310.5", ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymbolId(310);
            Assert.AreEqual("310.6", ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymbolId(310);
            Assert.AreEqual("310.7", ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymbolId(310);
            Assert.AreEqual("310.8", ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymbolId(310);
            Assert.AreEqual("310.9", ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymbolId(310);
            Assert.AreEqual("311.0", ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymbolId(310);
            Assert.AreEqual("311.1", ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));
        }

        [Test]
        // Make sure we can round trip the Euclidean metric information through a file.
        public void RoundTripMetric()
        {
            string filename = TestUtil.GetTestFile(@"io\metric_temp.ocd");

            Map map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("io")));
            using (map.Write())
                map.UseEuclideanMetric = true;

            InputOutput.WriteFile(filename, map, new MapFileFormat(MapFileFormatKind.OCAD, 11));

            map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("io")));
            InputOutput.ReadFile(filename, map);
            using (map.Read()) {
                Assert.IsTrue(map.UseEuclideanMetric);
            }

            map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("io")));
            using (map.Write())
                map.UseEuclideanMetric = false;

            InputOutput.WriteFile(filename, map, new MapFileFormat(MapFileFormatKind.OCAD, 11));

            map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(TestUtil.GetTestFile("io")));
            InputOutput.ReadFile(filename, map);
            using (map.Read()) {
                Assert.IsFalse(map.UseEuclideanMetric);
            }

        }

    }


}

#endif //TEST
