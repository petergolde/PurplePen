/* Copyright (c) 2006-2007, Peter Golde
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
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class PunchPatternDialog: Form
    {
        Dictionary<string, PunchPattern> patternDictionary;
        PunchcardFormat punchcardFormat;
        string currentCode;

        public PunchPatternDialog()
        {
            InitializeComponent();

            codeList.DrawMode = DrawMode.OwnerDrawFixed;
            codeList.ItemHeight = codeList.Font.Height + 2;
        }

        // Get or set a dictionary containing all the punch patterns.
        public Dictionary<string, PunchPattern> AllPunchPatterns
        {
            get
            {
                if (currentCode != null)
                    patternDictionary[currentCode] = GetPunchPattern();
                return patternDictionary;
            }
            set
            {
                this.patternDictionary = value;

                FillListBox();
            }
        }

        // Get or set the punch card format
        public PunchcardFormat PunchcardFormat
        {
            get
            {
                return punchcardFormat;
            }
            set
            {
                punchcardFormat = (PunchcardFormat) value.Clone();
            }
        }

        // Fill the list box from the dictionary
        void FillListBox()
        {
            List<string> codes = new List<string>(patternDictionary.Keys);
            codes.Sort(Util.CompareCodes);

            codeList.Items.Clear();
            codeList.Items.AddRange(codes.ToArray());
            if (codeList.Items.Count > 0)
                codeList.SelectedIndex = 0;
        }

        // Place a punch pattern in the dot grid.
        void SetPunchPattern(PunchPattern punch)
        {
            if (punch == null) {
                dotGrid.DotsAcross = PunchcardAppearance.gridSize;
                dotGrid.DotsDown = PunchcardAppearance.gridSize;
                dotGrid.Clear();
            }
            else {
                dotGrid.DotsAcross = punch.size;
                dotGrid.DotsDown = punch.size;
                dotGrid.SetAllDots(punch.dots);
            }
        }

        // Read the current punch pattern out of the dotGrid
        PunchPattern GetPunchPattern()
        {
            PunchPattern punch = new PunchPattern();
            punch.size = dotGrid.DotsAcross;
            punch.dots = dotGrid.GetAllDots();
            if (punch.IsEmpty)
                return null;
            else
                return punch;
        }

        private void codeList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (currentCode != null)
                patternDictionary[currentCode] = GetPunchPattern();

            currentCode = (string) codeList.SelectedItem;
            SetPunchPattern(patternDictionary[currentCode]);
        }

        // Custom drawing, so the items with no punch pattern defined 
        // are drawn in red.
        private void codeList_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            // Get the string to draw, and the color to draw in.
            string s = (string) codeList.Items[e.Index];
            PunchPattern currentPunch;
            if (e.Index == codeList.SelectedIndex)
                currentPunch = GetPunchPattern();
            else
                currentPunch = patternDictionary[s];
            bool drawRed = (currentPunch == null);

            Brush textBrush;
            if ((e.State & DrawItemState.Selected) != 0)
                textBrush = SystemBrushes.HighlightText;
            else
                textBrush = drawRed ? Brushes.Red : SystemBrushes.WindowText;

            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            e.Graphics.DrawString(s, e.Font, textBrush, e.Bounds, StringFormat.GenericDefault);

            e.DrawFocusRectangle();
        }

        private void formatButton_Click(object sender, EventArgs e)
        {
            // Init dialog.
            PunchcardLayoutDialog dialog = new PunchcardLayoutDialog();
            dialog.PunchcardFormat = punchcardFormat;

            // show.
            DialogResult result = dialog.ShowDialog();

            // Get result if OK pressed.
            if (result == DialogResult.OK) {
                punchcardFormat = dialog.PunchcardFormat;
            }

            dialog.Dispose();
        }

        private void PunchPatternDialog_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            Util.ShowHelpTopic(this, "ControlsPunchPatterns.htm");
            e.Cancel = true;
        }

    }
}
