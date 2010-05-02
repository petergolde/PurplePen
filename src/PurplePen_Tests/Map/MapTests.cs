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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.MapModel.Tests
{
    [TestClass]
    public class MapTests
    {
        // Check that we can read the template info from the give file name and that it matches what is expected.
        private void CheckReadTemplateInfo(string filename, TemplateInfo expected)
        {
            // Create and open the map file.
            Map map = new Map(new GDIPlus_TextMetrics());
            InputOutput.ReadFile(filename, map);

            TemplateInfo actual;
            using (map.Read())
                actual = map.Template;

            Assert.IsTrue(string.Compare(expected.absoluteFileName, actual.absoluteFileName, StringComparison.InvariantCultureIgnoreCase) == 0, "file names don't match");
            Assert.AreEqual(expected.angle, actual.angle);
            Assert.AreEqual(expected.centerPoint, actual.centerPoint);
            Assert.AreEqual(expected.dpi, actual.dpi);
            Assert.AreEqual(expected.visible, actual.visible);
        }


        [TestMethod]
        // Make sure that we can read the template information from a file.
        public void ReadTemplateInfo()
        {
            CheckReadTemplateInfo(TestUtil.GetTestFile(@"io\template6.ocd"), new TemplateInfo(@"C:\Documents and Settings\Peter\My Documents\CourseScribe\TestFiles\io\lp3.bmp", new PointF(-22.91F, -0.38F), 300F, (float)(-0.103743531740861 * 180.0 / Math.PI), true));
        }

        [TestMethod]
        // Make sure that we can read the template information from a file.
        public void ReadTemplateInfo9()
        {
            CheckReadTemplateInfo(TestUtil.GetTestFile(@"io\template9.ocd"), new TemplateInfo(TestUtil.GetTestFile(@"io\lp3.bmp"), new PointF(18.53543F, -6.157969F), 90.1276932F, 0.7513647F, true));
        }

        [TestMethod]
        // Make sure we can read template info with accented chararacters in it.
        public void ReadAccentedTemplateInfo()
        {
            CheckReadTemplateInfo(TestUtil.GetTestFile(@"io\accenttemplate9.ocd"), new TemplateInfo(TestUtil.GetTestFile(@"io\rähräh.jpg"), new PointF(0, 0), 300, 0, true));
        }

        void DumpMapFile(string mapFileName, string outputDump)
        {
            using (TextWriter writer = new StreamWriter(outputDump, false, System.Text.Encoding.UTF8)) {
                OcadDump dump = new OcadDump(TestUtil.GetTestFileDirectory());
                dump.DumpFile(mapFileName, writer);
            }
        }

        void DumpMap(Map map, int version, string mapFileName, string outputDump)
        {
            InputOutput.WriteFile(mapFileName, map, version);

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


        [TestMethod]
        // Make sure that we can write the template information to a file.
        public void WriteTemplateInfo()
        {
            Map map = new Map(new GDIPlus_TextMetrics());
            using (map.Write()) {
                map.MapScale = 15000;
                map.Template = new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\k.ocd"), new PointF(4.0F, 5.0F), 330F, 7.4F, true);
            }

            CheckDump(map, 7, TestUtil.GetTestFile(@"io\outputtemplate6_dump.txt"));
        }

        [TestMethod]
        // Make sure that we can write the template information to a file.
        public void WriteTemplateInfo8()
        {
            Map map = new Map(new GDIPlus_TextMetrics());
            using (map.Write()) {
                map.MapScale = 15000;
                map.Template = new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\k.ocd"), new PointF(4.0F, 5.0F), 330F, 7.4F, true);
            }

            CheckDump(map, 8, TestUtil.GetTestFile(@"io\outputtemplate8_dump.txt"));
        }

        [TestMethod]
        // Make sure that we can write the template information to a file.
        public void WriteTemplateInfo9()
        {
            Map map = new Map(new GDIPlus_TextMetrics());
            using (map.Write()) {
                map.MapScale = 15000;
                map.Template = new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\k.ocd"), new PointF(4.0F, 5.0F), 330F, 7.4F, true);
            }

            CheckDump(map, 9, TestUtil.GetTestFile(@"io\outputtemplate9_dump.txt"));
        }

        [TestMethod]
        // Make sure we can round trip the template information through a file.
        public void RoundTripTemplate()
        {
            TemplateInfo template = new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\k.ocd"), new PointF(4.0F, 5.0F), 330F, 7.4F, true);
            string filename = TestUtil.GetTestFile(@"io\outputtemplate_temp.ocd");

            Map map = new Map(new GDIPlus_TextMetrics());
            using (map.Write())
                map.Template = template;

            InputOutput.WriteFile(filename, map, 7);

            CheckReadTemplateInfo(filename, template);
        }

        [TestMethod]
        // Make sure we can round trip the template information through a file.
        public void RoundTripTemplate8()
        {
            TemplateInfo template = new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\k.ocd"), new PointF(4.0F, 5.0F), 330F, 7.4F, true);
            string filename = TestUtil.GetTestFile(@"io\outputtemplate_temp.ocd");

            Map map = new Map(new GDIPlus_TextMetrics());
            using (map.Write())
                map.Template = template;

            InputOutput.WriteFile(filename, map, 8);

            CheckReadTemplateInfo(filename, template);
        }

        [TestMethod]
        // Make sure we can round trip the template information through a file.
        public void RoundTripTemplate9()
        {
            TemplateInfo template = new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\k.ocd"), new PointF(4.0F, 5.0F), 330F, 7.4F, true);
            string filename = TestUtil.GetTestFile(@"io\outputtemplate_temp.ocd");

            Map map = new Map(new GDIPlus_TextMetrics());
            using (map.Write())
                map.Template = template;

            InputOutput.WriteFile(filename, map, 9);

            CheckReadTemplateInfo(filename, template);
        }

        [TestMethod]
        public void Bounds()
        {
            Map map = new Map(new GDIPlus_TextMetrics());
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
                black = map.AddColor("black", 1, 0, 0, 0, 1.0F);
                lineSymDef = new LineSymDef("line", 100000, black, 0.5F, LineStyle.Rounded);
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
        }
	

        void TestPrintArea(string filename, RectangleF expectedPrintArea)
        {
            Map map = new Map(new GDIPlus_TextMetrics());
            InputOutput.ReadFile(filename, map);

            RectangleF printArea;

            using (map.Read()) {
                printArea = map.PrintArea;
            }

            Assert.AreEqual(expectedPrintArea, printArea);
        }

        [TestMethod]
        public void ReadPrintArea6()
        {
            TestPrintArea(TestUtil.GetTestFile(@"io\printarea6.ocd"), RectangleF.FromLTRB(-30.5F, 12F, 111F, 98.6F));
        }

        [TestMethod]
        public void ReadPrintArea9()
        {
            TestPrintArea(TestUtil.GetTestFile(@"io\printarea9.ocd"), RectangleF.FromLTRB(-30.5F, 12F, 111F, 98.6F));
        }

        void TestScales(string filename, float expectedMapScale, float expectedPrintScale)
        {
            Map map = new Map(new GDIPlus_TextMetrics());
            InputOutput.ReadFile(filename, map);

            float mapScale, printScale;

            using (map.Read()) {
                mapScale = map.MapScale;
                printScale = map.PrintScale;
            }

            Assert.AreEqual(expectedMapScale, mapScale);
            Assert.AreEqual(expectedPrintScale, printScale);
        }

        [TestMethod]
        public void ReadScales6()
        {
            TestScales(TestUtil.GetTestFile(@"io\scales6.ocd"), 7000, 5500);
        }

        [TestMethod]
        public void ReadScales9()
        {
            TestScales(TestUtil.GetTestFile(@"io\scales9.ocd"), 7200, 2500);
        }

        void TestWriteScales(int version, string expectedDumpFileName)
        {
            Map map = new Map(new GDIPlus_TextMetrics());

            using (map.Write()) {
                map.MapScale = 7200;
                map.PrintScale = 2700;
            }

            CheckDump(map, version, expectedDumpFileName);
        }

        [TestMethod]
        public void WriteScales6()
        {
            TestWriteScales(6, TestUtil.GetTestFile(@"io\writescales6_dump.txt"));
        }

        [TestMethod]
        public void WriteScales9()
        {
            TestWriteScales(9, TestUtil.GetTestFile(@"io\writescales9_dump.txt"));
        }

        void TestWritePrintArea(int version, string expectedDumpFileName)
        {
            Map map = new Map(new GDIPlus_TextMetrics());

            using (map.Write()) {
                map.MapScale = 15000;
                map.PrintScale = 10000;
                map.PrintArea = RectangleF.FromLTRB(-30.5F, 12F, 111F, 98.6F);
            }

            CheckDump(map, version, expectedDumpFileName);
        }

        [TestMethod]
        public void WritePrintArea6()
        {
            TestWritePrintArea(6, TestUtil.GetTestFile(@"io\writeprintarea6_dump.txt"));
        }

        [TestMethod]
        public void WritePrintArea9()
        {
            TestWritePrintArea(9, TestUtil.GetTestFile(@"io\writeprintarea9_dump.txt"));
        }

        [TestMethod]
        public void GetFreeOcadId()
        {
            Map map = new Map(new GDIPlus_TextMetrics());
            InputOutput.ReadFile(TestUtil.GetTestFile(@"io\isompoints.ocd"), map);

            List<SymColor> colors;

            using (map.Read())
                colors = new List<SymColor>(map.AllColors);

            int ocadId;
            using (map.Read())
                ocadId = map.GetFreeSymdefOcadId(310);
            Assert.AreEqual(310000, ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymdefOcadId(310);
            Assert.AreEqual(310002, ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymdefOcadId(310);
            Assert.AreEqual(310003, ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymdefOcadId(310);
            Assert.AreEqual(310004, ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymdefOcadId(310);
            Assert.AreEqual(310005, ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymdefOcadId(310);
            Assert.AreEqual(310006, ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymdefOcadId(310);
            Assert.AreEqual(310007, ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymdefOcadId(310);
            Assert.AreEqual(310008, ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymdefOcadId(310);
            Assert.AreEqual(310009, ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymdefOcadId(310);
            Assert.AreEqual(311000, ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));

            using (map.Read())
                ocadId = map.GetFreeSymdefOcadId(310);
            Assert.AreEqual(311001, ocadId);

            using (map.Write())
                map.AddSymdef(new AreaSymDef("foo", ocadId, colors[0], null));
        }
    }
}

#endif //TEST
