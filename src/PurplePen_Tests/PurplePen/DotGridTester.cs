using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen.Tests
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