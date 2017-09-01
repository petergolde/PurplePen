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
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Xml;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

using PurplePen.MapModel;

namespace PurplePen.Tests
{
    [TestClass]
    public class SymbolDBTests
    {
        // Draw a grid on the graphics
        void DrawGrid(Graphics g, RectangleF rect, float spacing)
        {
            Pen pen = new Pen(Color.FromArgb(100, Color.MidnightBlue), 0.0F);

            // Draw the grid.
            for (float x = (int) ((rect.Left) / spacing) * spacing; x <= rect.Right; x += spacing) {
                g.DrawLine(pen, x, rect.Top, x, rect.Bottom);
            }
            for (float y = (int) ((rect.Top) / spacing) * spacing; y <= rect.Bottom; y += spacing) {
                g.DrawLine(pen, rect.Left, y, rect.Right, y);
            }

            pen.Dispose();
        }

        static Bitmap RenderToBitmap(Symbol sym)
        {
            Bitmap bm = new Bitmap(256, 256);
            Graphics g = Graphics.FromImage(bm);
            g.Clear(Color.White);
            RectangleF rect = new RectangleF(0.0F, 0.0F, 256.0F, 256.0F);

            sym.Draw(g, Color.Black, rect);

            g.Dispose();

            return bm;
        }


        [TestMethod]
        public void SymbolRendering()
        {
            SymbolDB symbolDB = new SymbolDB(TestUtil.GetTestFile("symbols\\testread.xml"));

            foreach (Symbol symbol in symbolDB.AllSymbols) {
                Bitmap bmNew = RenderToBitmap(symbol);
                TestUtil.CheckBitmapsBase(bmNew, "symbols\\" + symbol.Id);
            }
        }

        [TestMethod]
        public void ReadSymbols()
        {
            SymbolDB symbolDB = new SymbolDB(TestUtil.GetTestFile("symbols\\testread.xml"));

            ICollection<Symbol> symbolCollection = symbolDB.AllSymbols;
            Assert.AreEqual(7, symbolCollection.Count);

            Symbol[] symbols = new Symbol[symbolCollection.Count];
            symbolCollection.CopyTo(symbols, 0);

            Assert.AreSame(symbols[0], symbolDB["1.10"]);
            Assert.AreEqual('D', symbols[0].Kind);
            Assert.AreEqual(symbols[0].Id, "1.10");
            Assert.AreEqual(symbols[0].GetName("en"), "Knoll");
            Assert.AreEqual(symbols[0].GetText("en"), "knoll");
            Assert.AreEqual(1, symbols[0].strokes.Length);
            Assert.AreEqual(Symbol.SymbolStrokes.Disc, symbols[0].strokes[0].kind);
            Assert.AreEqual(10F, symbols[0].strokes[0].radius);
            Assert.AreEqual(1, symbols[0].strokes[0].points.Length);
            Assert.AreEqual(0F, symbols[0].strokes[0].points[0].X);
            Assert.AreEqual(10F, symbols[0].strokes[0].points[0].Y);

            Assert.AreSame(symbols[1], symbolDB["1.14"]);
            Assert.AreEqual('D', symbols[1].Kind);
            Assert.AreEqual(symbols[1].Id, "1.14");
            Assert.AreEqual(symbols[1].GetName("en"), "Pit");
            Assert.AreEqual(symbols[1].GetText("en"), "pit");
            Assert.AreEqual(symbols[1].GetPluralText("en"), "pits");
            Assert.AreEqual(1, symbols[1].strokes.Length);
            Assert.AreEqual(Symbol.SymbolStrokes.Polyline, symbols[1].strokes[0].kind);
            Assert.AreEqual(5F, symbols[1].strokes[0].thickness);
            Assert.AreEqual(LineCap.Round, symbols[1].strokes[0].ends);
            Assert.AreEqual(LineJoin.Miter, symbols[1].strokes[0].corners);
            Assert.AreEqual(3, symbols[1].strokes[0].points.Length);
            Assert.AreEqual(-40F, symbols[1].strokes[0].points[0].X);
            Assert.AreEqual(50F, symbols[1].strokes[0].points[0].Y);
            Assert.AreEqual(0.0F, symbols[1].strokes[0].points[1].X);
            Assert.AreEqual(-20F, symbols[1].strokes[0].points[1].Y);
            Assert.AreEqual(40F, symbols[1].strokes[0].points[2].X);
            Assert.AreEqual(50F, symbols[1].strokes[0].points[2].Y);


            Assert.AreSame(symbols[2], symbolDB["5.17"]);
            Assert.AreEqual('D', symbols[2].Kind);
            Assert.AreEqual(symbols[2].Id, "5.17");
            Assert.AreEqual(symbols[2].GetName("en"), "Boundary stone, Cairn");
            Assert.AreEqual(symbols[2].GetText("en"), "cairn");
            Assert.AreEqual(symbols[2].GetPluralText("en"), "cairns");
            Assert.AreEqual(2, symbols[2].strokes.Length);

            Assert.AreEqual(Symbol.SymbolStrokes.Circle, symbols[2].strokes[0].kind);
            Assert.AreEqual(50F, symbols[2].strokes[0].radius);
            Assert.AreEqual(5F, symbols[2].strokes[0].thickness);
            Assert.AreEqual(1, symbols[2].strokes[0].points.Length);
            Assert.AreEqual(0.0F, symbols[2].strokes[0].points[0].X);
            Assert.AreEqual(10F, symbols[2].strokes[0].points[0].Y);

            Assert.AreEqual(Symbol.SymbolStrokes.Disc, symbols[2].strokes[1].kind);
            Assert.AreEqual(10F, symbols[2].strokes[1].radius);
            Assert.AreEqual(1, symbols[2].strokes[1].points.Length);
            Assert.AreEqual(0F, symbols[2].strokes[1].points[0].X);
            Assert.AreEqual(10F, symbols[2].strokes[1].points[0].Y);


            Assert.AreSame(symbols[3], symbolDB["4.1"]);
            Assert.AreEqual('D', symbols[3].Kind);
            Assert.AreEqual(symbols[3].Id, "4.1");
            Assert.AreEqual(symbols[3].GetName("en"), "Open land");
            Assert.AreEqual(symbols[3].GetText("en"), "open land");
            Assert.AreEqual(symbols[3].GetPluralText("en"), "open land");
            Assert.AreEqual(symbols[3].GetText("de"), "smelly");
            Assert.AreEqual(symbols[3].GetText("xx"), "gibberish");
            Assert.AreEqual(symbols[3].GetPluralText("xx"), "plural gibberish");
            Assert.AreEqual(1, symbols[3].strokes.Length);
            Assert.AreEqual(Symbol.SymbolStrokes.Polygon, symbols[3].strokes[0].kind);
            Assert.AreEqual(5F, symbols[3].strokes[0].thickness);
            Assert.AreEqual(LineJoin.Miter, symbols[3].strokes[0].corners);
            Assert.AreEqual(4, symbols[3].strokes[0].points.Length);
            Assert.AreEqual(0.0, symbols[3].strokes[0].points[0].X);
            Assert.AreEqual(50F, symbols[3].strokes[0].points[0].Y);
            Assert.AreEqual(50F, symbols[3].strokes[0].points[1].X);
            Assert.AreEqual(0.0F, symbols[3].strokes[0].points[1].Y);
            Assert.AreEqual(0.0F, symbols[3].strokes[0].points[2].X);
            Assert.AreEqual(-50F, symbols[3].strokes[0].points[2].Y);
            Assert.AreEqual(-50F, symbols[3].strokes[0].points[3].X);
            Assert.AreEqual(0.0F, symbols[3].strokes[0].points[3].Y);


            Assert.AreSame(symbols[4], symbolDB["2.2"]);
            Assert.AreEqual('D', symbols[4].Kind);
            Assert.AreEqual("2.2", symbols[4].Id);
            Assert.AreEqual(symbols[4].GetName("en"), "Rock pillar");
            Assert.AreEqual(symbols[4].GetText("en"), "rock pillar");
            Assert.AreEqual(1, symbols[4].strokes.Length);
            Assert.AreEqual(Symbol.SymbolStrokes.FilledPolygon, symbols[4].strokes[0].kind);
            Assert.AreEqual(3, symbols[4].strokes[0].points.Length);
            Assert.AreEqual(0.0, symbols[4].strokes[0].points[0].X);
            Assert.AreEqual(70F, symbols[4].strokes[0].points[0].Y);
            Assert.AreEqual(50F, symbols[4].strokes[0].points[1].X);
            Assert.AreEqual(-50F, symbols[4].strokes[0].points[1].Y);
            Assert.AreEqual(-50F, symbols[4].strokes[0].points[2].X);
            Assert.AreEqual(-50F, symbols[4].strokes[0].points[2].Y);


            Assert.AreSame(symbols[5], symbolDB["1.3"]);
            Assert.AreEqual('D', symbols[5].Kind);
            Assert.AreEqual("1.3", symbols[5].Id);
            Assert.AreEqual(symbols[5].GetName("en"), "Reentrant");
            Assert.AreEqual(symbols[5].GetText("en"), "reentrant");
            Assert.AreEqual(1, symbols[5].strokes.Length);
            Assert.AreEqual(Symbol.SymbolStrokes.PolyBezier, symbols[5].strokes[0].kind);
            Assert.AreEqual(12.5F, symbols[5].strokes[0].thickness);
            Assert.AreEqual(LineCap.Flat, symbols[5].strokes[0].ends);
            Assert.AreEqual(13, symbols[5].strokes[0].points.Length);
            Assert.AreEqual(-80F, symbols[5].strokes[0].points[0].X);
            Assert.AreEqual(-80F, symbols[5].strokes[0].points[0].Y);
            Assert.AreEqual(-50F, symbols[5].strokes[0].points[1].X);
            Assert.AreEqual(-80F, symbols[5].strokes[0].points[1].Y);
            Assert.AreEqual(-50F, symbols[5].strokes[0].points[2].X);
            Assert.AreEqual(-30F, symbols[5].strokes[0].points[2].Y);
            Assert.AreEqual(-45F, symbols[5].strokes[0].points[3].X);
            Assert.AreEqual(0.0F, symbols[5].strokes[0].points[3].Y);
            Assert.AreEqual(-40F, symbols[5].strokes[0].points[4].X);
            Assert.AreEqual(30F, symbols[5].strokes[0].points[4].Y);
            Assert.AreEqual(-35F, symbols[5].strokes[0].points[5].X);
            Assert.AreEqual(80F, symbols[5].strokes[0].points[5].Y);
            Assert.AreEqual(0F, symbols[5].strokes[0].points[6].X);
            Assert.AreEqual(80F, symbols[5].strokes[0].points[6].Y);
            Assert.AreEqual(35F, symbols[5].strokes[0].points[7].X);
            Assert.AreEqual(80F, symbols[5].strokes[0].points[7].Y);
            Assert.AreEqual(40F, symbols[5].strokes[0].points[8].X);
            Assert.AreEqual(30F, symbols[5].strokes[0].points[8].Y);
            Assert.AreEqual(45F, symbols[5].strokes[0].points[9].X);
            Assert.AreEqual(0.0F, symbols[5].strokes[0].points[9].Y);
            Assert.AreEqual(50, symbols[5].strokes[0].points[10].X);
            Assert.AreEqual(-30F, symbols[5].strokes[0].points[10].Y);
            Assert.AreEqual(50F, symbols[5].strokes[0].points[11].X);
            Assert.AreEqual(-80F, symbols[5].strokes[0].points[11].Y);
            Assert.AreEqual(80F, symbols[5].strokes[0].points[12].X);
            Assert.AreEqual(-80F, symbols[5].strokes[0].points[12].Y);


            Assert.AreSame(symbols[6], symbolDB["0.4"]);
            Assert.AreEqual('Q', symbols[6].Kind);
            Assert.AreEqual("0.4", symbols[6].Id);
            Assert.AreEqual(symbols[6].GetName("en"), "Filled ellipse");
            Assert.AreEqual(symbols[6].GetText("en"), "ellipse");
            Assert.AreEqual(1, symbols[6].strokes.Length);
            Assert.AreEqual(Symbol.SymbolStrokes.FilledPolyBezier, symbols[6].strokes[0].kind);
            Assert.AreEqual(13, symbols[6].strokes[0].points.Length);
            Assert.AreEqual(0.0F, symbols[6].strokes[0].points[0].X);
            Assert.AreEqual(50F, symbols[6].strokes[0].points[0].Y);
            Assert.AreEqual(38F, symbols[6].strokes[0].points[1].X);
            Assert.AreEqual(50F, symbols[6].strokes[0].points[1].Y);
            Assert.AreEqual(70F, symbols[6].strokes[0].points[2].X);
            Assert.AreEqual(28F, symbols[6].strokes[0].points[2].Y);
            Assert.AreEqual(70F, symbols[6].strokes[0].points[3].X);
            Assert.AreEqual(0.0F, symbols[6].strokes[0].points[3].Y);
            Assert.AreEqual(70F, symbols[6].strokes[0].points[4].X);
            Assert.AreEqual(-28F, symbols[6].strokes[0].points[4].Y);
            Assert.AreEqual(38F, symbols[6].strokes[0].points[5].X);
            Assert.AreEqual(-50F, symbols[6].strokes[0].points[5].Y);
            Assert.AreEqual(0.0F, symbols[6].strokes[0].points[6].X);
            Assert.AreEqual(-50F, symbols[6].strokes[0].points[6].Y);
            Assert.AreEqual(-38F, symbols[6].strokes[0].points[7].X);
            Assert.AreEqual(-50F, symbols[6].strokes[0].points[7].Y);
            Assert.AreEqual(-70F, symbols[6].strokes[0].points[8].X);
            Assert.AreEqual(-28F, symbols[6].strokes[0].points[8].Y);
            Assert.AreEqual(-70F, symbols[6].strokes[0].points[9].X);
            Assert.AreEqual(0.0F, symbols[6].strokes[0].points[9].Y);
            Assert.AreEqual(-70F, symbols[6].strokes[0].points[10].X);
            Assert.AreEqual(28F, symbols[6].strokes[0].points[10].Y);
            Assert.AreEqual(-38F, symbols[6].strokes[0].points[11].X);
            Assert.AreEqual(50F, symbols[6].strokes[0].points[11].Y);
            Assert.AreEqual(0.0F, symbols[6].strokes[0].points[12].X);
            Assert.AreEqual(50F, symbols[6].strokes[0].points[12].Y);
        }

        [TestMethod]
        public void LanguageList()
        {
            SymbolDB symbolDB = new SymbolDB(TestUtil.GetTestFile("symbols\\intl_symbols.xml"));

            List<SymbolLanguage> languages = new List<SymbolLanguage>(symbolDB.AllLanguages);

            Assert.AreEqual(2, languages.Count);

            Assert.AreEqual("en", languages[0].LangId);
            Assert.AreEqual("English", languages[0].Name);
            Assert.AreEqual(false, languages[0].PluralModifiers);
            Assert.AreEqual(true, languages[0].PluralNouns);
            Assert.AreEqual(false, languages[0].GenderModifiers);

            Assert.AreEqual("bg", languages[1].LangId);
            Assert.AreEqual("Bulgarish", languages[1].Name);
            Assert.AreEqual(true, languages[1].PluralModifiers);
            Assert.AreEqual(true, languages[1].PluralNouns);
            Assert.AreEqual(true, languages[1].GenderModifiers);
            CollectionAssert.AreEqual(new string[] { "masculine", "feminine" }, languages[1].Genders);
        }

        [TestMethod]
        public void HasLanguage()
        {
            SymbolDB symbolDB = new SymbolDB(TestUtil.GetTestFile("symbols\\intl_symbols.xml"));
            Assert.IsTrue(symbolDB.HasLanguage("en"));
            Assert.IsTrue(symbolDB.HasLanguage("bg"));
            Assert.IsFalse(symbolDB.HasLanguage("az"));
        }

        [TestMethod]
        public void LanguageTexts()
        {
            SymbolDB symbolDB = new SymbolDB(TestUtil.GetTestFile("symbols\\intl_symbols.xml"));
            Symbol pitSymbol = symbolDB["1.14"];
            Symbol deepSymbol = symbolDB["8.3"];

            Assert.AreEqual("pito", pitSymbol.GetText("bg"));
            Assert.AreEqual("masculine", pitSymbol.GetGender("bg"));

            Assert.AreEqual("deepo", deepSymbol.GetText("bg", "masculine"));
            Assert.AreEqual("deepa", deepSymbol.GetText("bg", "feminine"));
            Assert.AreEqual("deepos", deepSymbol.GetPluralText("bg", "masculine"));
            Assert.AreEqual("deepas", deepSymbol.GetPluralText("bg", "feminine"));

            Assert.AreEqual("pit", pitSymbol.GetText("xx"));
            Assert.AreEqual("pits", pitSymbol.GetPluralText("xx"));
        }


        // Test for rendering into a map.

        // Render one course object to a map.
        internal Map RenderSymbolToMap(Symbol sym, float boxSize)
        {
            Map map = new Map(new GDIPlus_TextMetrics(), null);

            using (map.Write()) {
                //Dictionary<object, SymDef> dict = new Dictionary<object, SymDef>();
                SymColor symColor = map.AddColor("Purple", 11, 0.045F, 0.59F, 0, 0.255F, false);
                PointSymDef symdef = sym.CreateSymdef(map, symColor, boxSize);
                PointSymbol symbol = new PointSymbol(symdef, new PointF(0,0), 0, null);
                map.AddSymbol(symbol);
            }
            return map;
        }

        // Get the transform matrix from world coords to bitmap coords.
        Matrix GetTransform(Size sizeBitmap)
        {
            Matrix m = new Matrix();

            m.Translate(sizeBitmap.Width / 2, sizeBitmap.Height / 2);
            m.Scale((float) (sizeBitmap.Width / 8.0), -(float) (sizeBitmap.Height / 8.0));
            return m;
        }


        // Render a course to a bitmap for testing purposes. 
        internal Bitmap RenderSymbolMapToBitmap(Symbol sym)
        {
            Map map = RenderSymbolToMap(sym, 8.0F);

            Bitmap bm = new Bitmap(250, 250);
            using (Graphics g = Graphics.FromImage(bm)) {
                RenderOptions options = new RenderOptions();

                options.usePatternBitmaps = true;
                options.minResolution = (float) (8.0 / bm.Width);
                options.renderTemplates = RenderTemplateOption.MapAndTemplates;

                Matrix saveTransform = g.Transform;

                g.MultiplyTransform(GetTransform(bm.Size));

                g.Clear(Color.White);

                DrawGrid(g, new RectangleF(-4.0F, -4.0F, 8.0F, 8.0F), 1.0F);

                using (map.Read())
                    map.Draw(new GDIPlus_GraphicsTarget(g), new RectangleF(-100F, -100F, 200F, 200F), options, null);

                // Now use normal drawing to super-impose.
                g.Transform = saveTransform;
                RectangleF rect = new RectangleF(0.0F, 0.0F, bm.Width, bm.Height);
                sym.Draw(g, Color.FromArgb(50, Color.Black), rect);
            }

            return bm;

        }

        [TestMethod]
        public void SymbolOCADRendering()
        {
            SymbolDB symbolDB = new SymbolDB(TestUtil.GetTestFile("symbols\\testread.xml"));

            foreach (Symbol symbol in symbolDB.AllSymbols) {
                Bitmap bmNew = RenderSymbolMapToBitmap(symbol);
                TestUtil.CheckBitmapsBase(bmNew, "symbols\\" + symbol.Id + "_ocad");
            }
        }

        [TestMethod]
        public void MultiStandard()
        {
            SymbolDB symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"), "2004");

            Debug.Assert(symbolDB.SymbolExistsInStandard("1.10", "2004"));
            Debug.Assert(symbolDB.SymbolExistsInStandard("1.10", "2018"));
            Symbol knoll = symbolDB["1.10"];
            Debug.Assert(knoll.InStandard("2004"));
            Debug.Assert(!knoll.InStandard("2018"));

            symbolDB.Standard = "2018";
            knoll = symbolDB["1.10"];
            Debug.Assert(!knoll.InStandard("2004"));
            Debug.Assert(knoll.InStandard("2018"));

            Debug.Assert(symbolDB.SymbolExistsInStandard("1.11", "2004"));
            Debug.Assert(symbolDB.SymbolExistsInStandard("1.11", "2018"));
            symbolDB.Standard = "2004";
            Symbol saddle = symbolDB["1.11"];
            Debug.Assert(saddle.InStandard("2004"));
            Debug.Assert(saddle.InStandard("2018"));

            symbolDB.Standard = "2018";
            saddle = symbolDB["1.11"];
            Debug.Assert(saddle.InStandard("2004"));
            Debug.Assert(saddle.InStandard("2018"));

            Debug.Assert(! symbolDB.SymbolExistsInStandard("2.10", "2004"));
            Debug.Assert(symbolDB.SymbolExistsInStandard("2.10", "2018"));
            symbolDB.Standard = "2018";
            Symbol trench = symbolDB["2.10"];
            Debug.Assert(!trench.InStandard("2004"));
            Debug.Assert(trench.InStandard("2018"));
            Debug.Assert(trench.ReplacementId == "1.7");

            symbolDB.Standard = "2004";
            try {
                trench = symbolDB["2.10"];
                Debug.Fail("should throw exception");
            }
            catch (Exception e) {
                Debug.Assert(e is InvalidOperationException);
            }

            symbolDB.Standard = "2004";
            Debug.Assert(symbolDB.SymbolExistsInStandard("11.7", "2004"));
            Debug.Assert(symbolDB.SymbolExistsInStandard("11.7", "2018"));
            Symbol bend = symbolDB["11.7"];
            Debug.Assert(bend.InStandard("2004"));
            Debug.Assert(!bend.InStandard("2018"));
            Debug.Assert(bend.Kind == 'G');

            symbolDB.Standard = "2018";
            bend = symbolDB["11.7"];
            Debug.Assert(!bend.InStandard("2004"));
            Debug.Assert(bend.InStandard("2018"));
            Debug.Assert(bend.Kind == 'F');
        }
    }


}
#endif
