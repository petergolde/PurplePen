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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen.DebugUI
{
    partial class SymbolBrowser : Form
    {
        SymbolDB symbolDB;
        string idSelected;

        public SymbolBrowser()
        {
            InitializeComponent();

        }

        public void Initialize(SymbolDB symbolDB)
        {
            this.symbolDB = symbolDB;

            foreach (Symbol sym in symbolDB.AllSymbols) {
                listBoxSymbols.Items.Add(sym.Id + " - " + sym.GetName(Util.CurrentLangName()));
            }
       }

        private void listBoxSymbol_SelectedIndexChanged(object sender, EventArgs e)
        {
            string s= (string)listBoxSymbols.SelectedItem;
            if (s == null || s == "")
                idSelected = null;
            else {
                int i = s.IndexOf('-');
                idSelected = s.Substring(0, i).Trim();
            }

            labelType.Text = symbolDB[idSelected].Kind.ToString();
            labelName.Text = symbolDB[idSelected].GetName("en");
            labelText.Text = symbolDB[idSelected].GetText("en");
            Invalidate(true);
        }

        private void pictureSymbol_Paint(object sender, PaintEventArgs e)
        {
            RectangleF rect = (RectangleF)pictureSymbol.ClientRectangle;
            if (idSelected != null && idSelected != "")  {
                Symbol sym = symbolDB[idSelected];

                if (sym.Kind >= 'T') {
                    // instructional line
                    rect.Inflate(0, - rect.Height * 3.5F / 8.0F);
                    e.Graphics.DrawLine(Pens.Black, rect.Left, rect.Top, rect.Right, rect.Top);
                    e.Graphics.DrawLine(Pens.Black, rect.Left, rect.Bottom, rect.Right, rect.Bottom);
                    sym.Draw(e.Graphics, Color.Black, rect);
                }
                else {
                    sym.Draw(e.Graphics, Color.Black, rect);
                }
            }
        }

        internal static Bitmap RenderToBitmap(Symbol sym)
        {
            Bitmap bm = new Bitmap(256, 256);
            Graphics g = Graphics.FromImage(bm);
            g.Clear(Color.White);
            RectangleF rect = new RectangleF(0.0F, 0.0F, 256.0F, 256.0F);

            sym.Draw(g, Color.Black, rect);

            g.Dispose();

            return bm;
        }

        private void buttonCreateImage_Click(object sender, EventArgs e)
        {
            if (idSelected != null && idSelected != "") {
                Symbol sym = symbolDB[idSelected];

                using (Bitmap bm = RenderToBitmap(sym))
                    bm.Save(idSelected + ".png", System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private void buttonPrint_Click(object sender, EventArgs e)
        {
            printDocument.Print();
        }

        private void printDocument_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            float mm = 7;

            Graphics g = e.Graphics;

            Pen p = new Pen(Color.Black, 1F);

            float dx = mm * 100F / 25.4F;
            float dy = mm * 100F / 25.4F;

            float x, y;

            x = e.MarginBounds.Left;
            while (x < e.MarginBounds.Right) {
                g.DrawLine(Pens.Black, x, e.MarginBounds.Top, x, e.MarginBounds.Bottom);
                x += dx;
            }

            y = e.MarginBounds.Top;
            while (y < e.MarginBounds.Bottom) {
                g.DrawLine(Pens.Black, e.MarginBounds.Left, y, e.MarginBounds.Right, y);
                y += dy;
            }

            IEnumerator<Symbol> enumSymbol = symbolDB.AllSymbols.GetEnumerator();

            for (x = e.MarginBounds.Left; x < e.MarginBounds.Right; x += dx) {
                for (y = e.MarginBounds.Top; y < e.MarginBounds.Bottom; y += dy) {
                    RectangleF rect = new RectangleF(x, y, dx, dy);
                    if (enumSymbol.MoveNext() == false) {
                        enumSymbol = symbolDB.AllSymbols.GetEnumerator();
                        enumSymbol.MoveNext();
                    }

                    Symbol sym = enumSymbol.Current;

                    sym.Draw(g, Color.Black, rect);
                }
            }

        }

    }


}