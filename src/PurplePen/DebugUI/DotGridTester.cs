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
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen.DebugUI
{
    public partial class DotGridTester: Form
    {
        public DotGridTester()
        {
            InitializeComponent();
        }

        private void rowsControl_ValueChanged(object sender, EventArgs e)
        {
            this.dotGrid1.DotsDown = (int) rowsControl.Value;
        }

        private void colControl_ValueChanged(object sender, EventArgs e)
        {
            this.dotGrid1.DotsAcross = (int) colControl.Value;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool[,] grid = new bool[dotGrid1.DotsDown, dotGrid1.DotsAcross];

            for (int row = 0; row < dotGrid1.DotsDown; ++row)
                for (int col = 0; col < dotGrid1.DotsAcross; ++col)
                    grid[row, col] = (row + col) % 2 == 0;

            dotGrid1.SetAllDots(grid);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            dotGrid1.Clear();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            for (int row = 0; row < dotGrid1.DotsDown; ++row)
                for (int col = 0; col < dotGrid1.DotsAcross; ++col)
                    dotGrid1.SetDot(row, col, (row + col) % 2 == 0);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            bool[,] allDots = dotGrid1.GetAllDots();
            StringBuilder builder = new StringBuilder();

            for (int row = 0; row < dotGrid1.DotsDown; ++row) {
                for (int col = 0; col < dotGrid1.DotsAcross; ++col) {
                    builder.Append(allDots[row, col] ? '@' : '.');
                }
                builder.Append("\r\n");
            }

            MessageBox.Show(builder.ToString());
        }


    }
}