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
using System.Linq;
using NUnit.Framework;


namespace PurplePen.MapModel.Tests
{
    using PurplePen.Graphics2D;
    using TestingUtils;

    [TestFixture]
    public class UndoTests
    {
        Map map;
        UndoMgr undoMgr;

        void LoadMap(string name)
        {
            string filename = TestUtil.GetTestFile(name);
            // Create and open the map file.
            map = new Map(new GDIPlus_TextMetrics(), new GDIPlus_FileLoader(Path.GetDirectoryName(filename)));
            InputOutput.ReadFile(filename, map);
            undoMgr = new UndoMgr(100);
            map.UndoMgr = undoMgr;
        }

        void Undo()
        {
            using (map.Write()) {
                undoMgr.Undo();
            }
        }

        void Redo()
        {
            using (map.Write()) {
                undoMgr.Redo();
            }
        }

        [Test]
        public void UndoMapScale()
        {
            LoadMap(@"undo\simple1.ocd");

            using (map.Read()) {
                Assert.AreEqual(15000, map.MapScale);
            }

            using (map.UndoableWrite("Map Scale")) {
                map.MapScale = 9000;
            }

            using (map.Read()) {
                Assert.AreEqual(9000, map.MapScale);
            }

            Undo();

            using (map.Read()) {
                Assert.AreEqual(15000, map.MapScale);
            }

            Redo();

            using (map.Read()) {
                Assert.AreEqual(9000, map.MapScale);
            }
        }

        [Test]
        public void UndoPrintScale()
        {
            LoadMap(@"undo\simple1.ocd");

            using (map.Read()) {
                Assert.AreEqual(8000, map.PrintScale);
            }

            using (map.UndoableWrite("Print Scale")) {
                map.PrintScale = 4000;
            }

            using (map.Read()) {
                Assert.AreEqual(4000, map.PrintScale);
            }

            Undo();

            using (map.Read()) {
                Assert.AreEqual(8000, map.PrintScale);
            }

            Redo();

            using (map.Read()) {
                Assert.AreEqual(4000, map.PrintScale);
            }
        }

        [Test]
        public void UndoPrintArea()
        {
            LoadMap(@"undo\simple1.ocd");

            using (map.Read()) {
                Assert.AreEqual(RectangleF.FromLTRB(-50, -80, 60, 75), map.PrintArea);
            }

            using (map.UndoableWrite("Print Area")) {
                map.PrintArea = RectangleF.FromLTRB(-20, -105, 78, 81);
            }

            using (map.Read()) {
                Assert.AreEqual(RectangleF.FromLTRB(-20, -105, 78, 81), map.PrintArea);
            }

            Undo();

            using (map.Read()) {
                Assert.AreEqual(RectangleF.FromLTRB(-50, -80, 60, 75), map.PrintArea);
            }

            Redo();

            using (map.Read()) {
                Assert.AreEqual(RectangleF.FromLTRB(-20, -105, 78, 81), map.PrintArea);
            }
        }


            [Test]
        public void UndoTemplates()
        {
            TemplateInfo[] newTemplates = { 
                new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\k.ocd"), new PointF(4.0F, 5.0F), 330F, 7.4F, 1.0F, 0.34F, -18.4F, true),
                new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\l.jpg"), new PointF(7F, -5F), 1000F, -12.5F, 1.0F, 1.99F, 1.4F, false),
                new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\m.bmp"), new PointF(12F, -5.673F), 15F, 7.4F, 1.0F, 1.0F, 45.4F, true),
                
            };

            LoadMap(@"undo\simple1.ocd");

            using (map.Read()) {
                Assert.AreEqual(0, map.Templates.Count);
            }

            using (map.UndoableWrite("Templates")) {
                map.Templates = newTemplates;
            }

            using (map.Read()) {
                CollectionAssert.AreEqual(newTemplates, map.Templates);
            }

            Undo();

            using (map.Read()) {
                Assert.AreEqual(0, map.Templates.Count);
            }

            Redo();

            using (map.Read()) {
                CollectionAssert.AreEqual(newTemplates, map.Templates);
            }
        }

        [Test]
        public void UndoHideTemplates()
        {
            TemplateInfo[] newTemplates = new TemplateInfo[] { 
                new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\k.ocd"), new PointF(4.0F, 5.0F), 330F, 7.4F, 1.0F, 0.34F, -18.4F, true),
                new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\l.jpg"), new PointF(7F, -5F), 1000F, -12.5F, 1.0F, 1.99F, 1.4F, false),
                new TemplateInfo(TestUtil.GetTestFile(@"io\subdir\m.bmp"), new PointF(12F, -5.673F), 15F, 7.4F, 1.0F, 1.0F, 45.4F, true),
                
            };

            LoadMap(@"undo\simple1.ocd");

            using (map.UndoableWrite("Templates")) {
                map.Templates = newTemplates;
            }

            using (map.Read()) {
                Assert.IsFalse(map.HideTemplates);
            }

            using (map.UndoableWrite("Hiden Templates")) {
                map.HideTemplates = true;
            }

            using (map.Read()) {
                Assert.IsTrue(map.HideTemplates);
            }

            Undo();

            using (map.Read()) {
                Assert.IsFalse(map.HideTemplates);
            }

            Redo();

            using (map.Read()) {
                Assert.IsTrue(map.HideTemplates);
            }
        }

        [Test]
        public void UndoMetric()
        {
            LoadMap(@"undo\simple1.ocd");

            using (map.Read()) {
                Assert.IsFalse(map.UseEuclideanMetric);
            }

            using (map.UndoableWrite("Metric")) {
                map.UseEuclideanMetric = true;
            }

            using (map.Read()) {
                Assert.IsTrue(map.UseEuclideanMetric);
            }

            Undo();

            using (map.Read()) {
                Assert.IsFalse(map.UseEuclideanMetric);
            }

            Redo();

            using (map.Read()) {
                Assert.IsTrue(map.UseEuclideanMetric);
            }
        }

        [Test]
        public void UndoRealWorldCoords()
        {
            var c1 = new RealWorldCoords() {
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

            var c2 = new RealWorldCoords() {
                RealWorldOn = true,
                RealWorldGridAndZone = -2403,
                RealWorldOffsetX = 431683,
                RealWorldOffsetY = -732682,
                RealWorldAngle = 34.1958,
                PaperGridDistance = 100,
                RealWorldGridDistance = 123,
                RealWorldLocalOffsetX = 0.71,
                RealWorldLocalOffsetY = -1.22,
            };

            LoadMap(@"undo\simple1.ocd");

            using (map.UndoableWrite("Set Real World Coords")) {
                map.RealWorldCoords = c1;
            }
            using (map.Read()) {
                Assert.AreEqual(c1, map.RealWorldCoords);
            }

            using (map.UndoableWrite("Set Real World Coords")) {
                map.RealWorldCoords = c2;
            }

            using (map.Read()) {
                Assert.AreEqual(c2, map.RealWorldCoords);
            }

            Undo();

            using (map.Read()) {
                Assert.AreEqual(c1, map.RealWorldCoords);
            }

            Redo();

            using (map.Read()) {
                Assert.AreEqual(c2, map.RealWorldCoords);
            }
        }

        [Test]
        public void UndoColorMatrix()
        {
            ColorMatrix cm = new ColorMatrix(new float[][] {
                           new float[] {0.4F,  0,  0,  0, 0},
                           new float[] {0,  0.4F,  0,  0, 0},
                           new float[] {0,  0,  0.4F,  0, 0},
                           new float[] {0,  0,  0,  1, 0},
                           new float[] {0.6F, 0.6F, 0.6F, 0, 1}
                    });

            LoadMap(@"undo\simple1.ocd");

            using (map.Read()) {
                Assert.IsNull(map.ColorMatrix);
            }

            using (map.UndoableWrite("Change Color Matrix")) {
                map.ColorMatrix = cm;
            }

            using (map.Read()) {
                Assert.IsTrue(ColorMatrix.Equal(cm, map.ColorMatrix));
            }

            Undo();

            using (map.Read()) {
                Assert.IsNull(map.ColorMatrix);
            }

            Redo();

            using (map.Read()) {
                Assert.IsTrue(ColorMatrix.Equal(cm, map.ColorMatrix));
            }
        }

        [Test]
        public void UndoColorTableChanges()
        {
            LoadMap(@"undo\simple1.ocd");

            using (map.Read()) {
                List<SymColor> colors = new List<SymColor>(map.AllColors);
                Assert.AreEqual(37, colors.Count);
                Assert.AreEqual("Yellow 50%/Green 20%", colors[0].Name);
                Assert.AreEqual("Layout color Brown", colors[36].Name);
            }
            
            using (map.UndoableWrite("Add Color")) {
                map.AddColor("Foo", 532, 0.3F, 0.5F, 0.1F, 0.64F, false);
                map.AddColorBottom("Bar", 556, 0.1F, 0.6F, 0.4F, 0.11F, true);
                map.AddColorAtIndex(22, "Baz", 443, 0.2F, 0.7F, 0.2F, 0.13F, false);
            }

            using (map.Read()) {
                List<SymColor> colors = new List<SymColor>(map.AllColors);
                Assert.AreEqual(40, colors.Count);
                Assert.AreEqual("Bar", colors[0].Name);
                Assert.AreEqual(0.1F, colors[0].CmykColor.Cyan);
                Assert.AreEqual("Yellow 50%/Green 20%", colors[1].Name);
                Assert.AreEqual("Baz", colors[22].Name);
                Assert.AreEqual(0.13F, colors[22].CmykColor.Black);
                Assert.AreEqual("Layout color Brown", colors[38].Name);
                Assert.AreEqual("Foo", colors[39].Name);
                Assert.AreEqual(0.5F, colors[39].CmykColor.Magenta);
            }
            
            Undo();

            using (map.Read()) {
                List<SymColor> colors = new List<SymColor>(map.AllColors);
                Assert.AreEqual(37, colors.Count);
                Assert.AreEqual("Yellow 50%/Green 20%", colors[0].Name);
                Assert.AreEqual("Layout color Brown", colors[36].Name);
            }

            Redo();

            using (map.Read()) {
                List<SymColor> colors = new List<SymColor>(map.AllColors);
                Assert.AreEqual(40, colors.Count);
                Assert.AreEqual("Bar", colors[0].Name);
                Assert.AreEqual(0.1F, colors[0].CmykColor.Cyan);
                Assert.AreEqual("Yellow 50%/Green 20%", colors[1].Name);
                Assert.AreEqual("Baz", colors[22].Name);
                Assert.AreEqual(0.13F, colors[22].CmykColor.Black);
                Assert.AreEqual("Layout color Brown", colors[38].Name);
                Assert.AreEqual("Foo", colors[39].Name);
                Assert.AreEqual(0.5F, colors[39].CmykColor.Magenta);
            }

            using (map.UndoableWrite("Remove Color")) {
                map.RemoveColorAtIndex(39);
                map.RemoveColorAtIndex(22);
            }

            using (map.Read()) {
                List<SymColor> colors = new List<SymColor>(map.AllColors);
                Assert.AreEqual(38, colors.Count);
                Assert.AreEqual("Bar", colors[0].Name);
                Assert.AreEqual("Blue 50%", colors[22].Name);
                Assert.AreEqual("Layout color Brown", colors[37].Name);
            }

            Undo();

            using (map.Read()) {
                List<SymColor> colors = new List<SymColor>(map.AllColors);
                Assert.AreEqual(40, colors.Count);
                Assert.AreEqual("Bar", colors[0].Name);
                Assert.AreEqual(0.1F, colors[0].CmykColor.Cyan);
                Assert.AreEqual("Yellow 50%/Green 20%", colors[1].Name);
                Assert.AreEqual("Baz", colors[22].Name);
                Assert.AreEqual(0.13F, colors[22].CmykColor.Black);
                Assert.AreEqual("Layout color Brown", colors[38].Name);
                Assert.AreEqual("Foo", colors[39].Name);
                Assert.AreEqual(0.5F, colors[39].CmykColor.Magenta);
            }


            Redo();

            using (map.Read()) {
                List<SymColor> colors = new List<SymColor>(map.AllColors);
                Assert.AreEqual(38, colors.Count);
                Assert.AreEqual("Bar", colors[0].Name);
                Assert.AreEqual("Blue 50%", colors[22].Name);
                Assert.AreEqual(0.48F, colors[22].CmykColor.Cyan);
                Assert.AreEqual("Layout color Brown", colors[37].Name);
            }

            using (map.UndoableWrite("Change Color")) {
                map.ChangeColorAtIndex(22, "Zip", 224, 0.6F, 0.23F, 0.4F, 0.3F, true);
            }

            using (map.Read()) {
                List<SymColor> colors = new List<SymColor>(map.AllColors);
                Assert.AreEqual(38, colors.Count);
                Assert.AreEqual("Bar", colors[0].Name);
                Assert.AreEqual("Zip", colors[22].Name);
                Assert.AreEqual(224, colors[22].OcadId);
                Assert.AreEqual(0.6F, colors[22].CmykColor.Cyan);
                Assert.AreEqual(0.23F, colors[22].CmykColor.Magenta);
                Assert.AreEqual(0.4F, colors[22].CmykColor.Yellow);
                Assert.AreEqual(0.3F, colors[22].CmykColor.Black);
                Assert.AreEqual(true, colors[22].OverPrint);
                Assert.AreEqual("Layout color Brown", colors[37].Name);
            }

            Undo();

            using (map.Read()) {
                List<SymColor> colors = new List<SymColor>(map.AllColors);
                Assert.AreEqual(38, colors.Count);
                Assert.AreEqual("Bar", colors[0].Name);
                Assert.AreEqual("Blue 50%", colors[22].Name);
                Assert.AreEqual(108, colors[22].OcadId);
                Assert.AreEqual(0.48F, colors[22].CmykColor.Cyan);
                Assert.AreEqual(0.2F, colors[22].CmykColor.Magenta);
                Assert.AreEqual(0F, colors[22].CmykColor.Yellow);
                Assert.AreEqual(0F, colors[22].CmykColor.Black);
                Assert.AreEqual(false, colors[22].OverPrint);
                Assert.AreEqual("Layout color Brown", colors[37].Name);
            }

            Redo();

            using (map.Read()) {
                List<SymColor> colors = new List<SymColor>(map.AllColors);
                Assert.AreEqual(38, colors.Count);
                Assert.AreEqual("Bar", colors[0].Name);
                Assert.AreEqual("Zip", colors[22].Name);
                Assert.AreEqual(224, colors[22].OcadId);
                Assert.AreEqual(0.6F, colors[22].CmykColor.Cyan);
                Assert.AreEqual(0.23F, colors[22].CmykColor.Magenta);
                Assert.AreEqual(0.4F, colors[22].CmykColor.Yellow);
                Assert.AreEqual(0.3F, colors[22].CmykColor.Black);
                Assert.AreEqual(true, colors[22].OverPrint);
                Assert.AreEqual("Layout color Brown", colors[37].Name);
            }

        }

        [Test]
        public void UndoSymbolChange()
        {
            LoadMap(@"undo\simple1.ocd");

            Symbol s;

            using (map.Read()) {
                s = new PointSymbol((PointSymDef) map.SymdefFromSymbolId("113.0"), new PointF(-6, -5), 30, null);
                Assert.False(map.AllSymbols.Contains(s));
                Assert.Null(s.ContainingMap);
            }

            using (map.UndoableWrite("Add Symbol")) {
                map.AddSymbol(s);
            }

            using (map.Read()) {
                Assert.True(map.AllSymbols.Contains(s));
                Assert.AreSame(map, s.ContainingMap);
            }

            Undo();

            using (map.Read()) {
                Assert.False(map.AllSymbols.Contains(s));
                Assert.Null(s.ContainingMap);
            }

            Redo();

            using (map.Read()) {
                Assert.True(map.AllSymbols.Contains(s));
                Assert.AreSame(map, s.ContainingMap);
            }

            using (map.UndoableWrite("Remove Symbol")) {
                map.RemoveSymbol(s);
            }

            using (map.Read()) {
                Assert.False(map.AllSymbols.Contains(s));
                Assert.Null(s.ContainingMap);
            }

            Undo();

            Symbol hitSym;

            using (map.Read()) {
                SymbolHit[] hits = map.HitTest(new PointF(-6, -5), 0.5F, new MapHitTestOptions());
                hitSym = hits[0].symbol;
                Assert.AreSame(s.Definition, hitSym.Definition);
                Assert.True(map.AllSymbols.Contains(hitSym));
                Assert.AreSame(map, hitSym.ContainingMap);
            }

            Redo();

            using (map.Read()) {
                Assert.False(map.AllSymbols.Contains(s));
                Assert.Null(s.ContainingMap);
                Assert.False(map.AllSymbols.Contains(hitSym));
                Assert.Null(hitSym.ContainingMap);
            }

        }

        [Test]
        public void UndoSymdefChange()
        {
            LoadMap(@"undo\simple1.ocd");

            SymDef s;

            using (map.Read()) {
                s = new LineSymDef("Fizzle", "415.223", map.AllColors.ElementAt(5), 0.4F, LineJoinMode.Bevel, LineCapMode.Flat);
                Assert.False(map.AllSymdefs.Contains(s));
                Assert.Null(s.ContainingMap);
            }

            using (map.UndoableWrite("Add SymDef")) {
                map.AddSymdef(s);
            }

            using (map.Read()) {
                Assert.True(map.AllSymdefs.Contains(s));
                Assert.AreSame(map, s.ContainingMap);
            }

            Undo();

            using (map.Read()) {
                Assert.False(map.AllSymdefs.Contains(s));
                Assert.Null(s.ContainingMap);
            }

            Redo();

            using (map.Read()) {
                Assert.True(map.AllSymdefs.Contains(s));
                Assert.AreSame(map, s.ContainingMap);
            }

            using (map.UndoableWrite("Remove SymDef")) {
                map.RemoveSymdef(s);
            }

            using (map.Read()) {
                Assert.False(map.AllSymdefs.Contains(s));
                Assert.Null(s.ContainingMap);
            }

            Undo();

            using (map.Read()) {
                Assert.True(map.AllSymdefs.Contains(s));
                Assert.AreSame(map, s.ContainingMap);
            }

            Redo();

            using (map.Read()) {
                Assert.False(map.AllSymdefs.Contains(s));
                Assert.Null(s.ContainingMap);
            }

        }

        [Test]
        public void UndoSymdefVisible()
        {
            LoadMap(@"undo\simple1.ocd");

            SymDef s;
            using (map.Read()) {
                s = map.SymdefFromSymbolId("113.0");
                Assert.IsTrue(map.IsSymdefVisible(s));
            }

            using (map.UndoableWrite("Symdef Visible")) {
                map.SetSymdefVisible(s, false);
            }

            using (map.Read()) {
                Assert.IsFalse(map.IsSymdefVisible(s));
            }

            Undo();

            using (map.Read()) {
                Assert.IsTrue(map.IsSymdefVisible(s));
            }

            Redo();

            using (map.Read()) {
                Assert.IsFalse(map.IsSymdefVisible(s));
            }
        }




    }
}

#endif //TEST
