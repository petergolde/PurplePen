using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class DotGrid: UserControl
    {
        public int dotsAcross = 1, dotsDown = 1;             // Size of the grid.
        public List<List<bool>> dots;                  // The dots.
        public int pixelsPerDot;                            // Size of the grid.
        public Size clientSize;                               // Client size of this control.

        private const float DOTWIDTH = 0.7F;     // diameter of dot as fraction of the grid size.

        public DotGrid()
        {
            InitializeComponent();
            ResizeTo(dotsAcross, dotsDown);
        }

        // Number of dots across.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int DotsAcross {
            get { return dotsAcross; }
            set {
                if (dotsAcross != value) {
                    ResizeTo(value, dotsDown);
                    UpdateGridSize();
                    Invalidate();
                }
            }
        }

        // Number of dots down.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int DotsDown {
            get { return dotsDown; }
            set {
                if (dotsDown != value) {
                    ResizeTo(dotsAcross, value);
                    UpdateGridSize();
                    Invalidate();
                }
            }
        }

        // Get all dot values
        public bool[,] GetAllDots()
        {
            bool[,] dotValues = new bool[dotsDown, dotsAcross];

            for (int row = 0; row < dotsDown; ++row)
                for (int col = 0; col < dotsAcross; ++col)
                    dotValues[row, col] = dots[row][col];

            return dotValues;
        }

        // Set all dot values.
        public void SetAllDots(bool [,] dotValues) {
            for (int row = 0; row < dotsDown; ++row)
                for (int col = 0; col < dotsAcross; ++col)
                    SetDot(row, col, dotValues[row, col]);
        }

        // Clear
        public void Clear()
        {
            for (int row = 0; row < dotsDown; ++row)
                for (int col = 0; col < dotsAcross; ++col)
                    SetDot(row, col, false);
        }


        // Set one dot.
        public void SetDot(int row, int col, bool dotValue)
        {
            if (GetDot(row, col) != dotValue) {
                if (dotValue)
                    DrawDot(row, col);
                else
                    EraseDot(row, col);
                dots[row][col] = dotValue;
            }
        }

        // Get one dot.
        public bool GetDot(int row, int col)
        {
            return dots[row][col];
        }

        // Draw a dot
        void DrawDot(int row, int col)
        {
            using (Graphics g = CreateGraphics()) {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                DrawDot(g, row, col);
            }
        }

        // Draw a dot
        void DrawDot(Graphics g, int row, int col)
        {
            RectangleF rectGridCell = new RectangleF(col * pixelsPerDot, row * pixelsPerDot, pixelsPerDot, pixelsPerDot);
            RectangleF rectCircle = rectGridCell;
            rectCircle.Inflate(- (pixelsPerDot * (1 - DOTWIDTH)) / 2F, - (pixelsPerDot * (1 - DOTWIDTH)) / 2F);

            g.FillEllipse(Brushes.Black, rectCircle);
        }

        // Erase a dot
        void EraseDot(int row, int col)
        {
            using (Graphics g = CreateGraphics()) {
                EraseDot(g, row, col);
            }
        }

        // Erase a dot
        void EraseDot(Graphics g, int row, int col)
        {
            Rectangle rectGridCell = new Rectangle(col * pixelsPerDot + 1, row * pixelsPerDot + 1, pixelsPerDot - 1, pixelsPerDot - 1);

            g.FillRectangle(Brushes.White, rectGridCell);
        }

        // Draw grid
        void DrawGrid(Graphics g)
        {
            Pen p = new Pen(Color.LightGray, 0);
            int height = dotsDown * pixelsPerDot;
            int width = dotsAcross * pixelsPerDot;
            
            // Draw the grid.
            for (int row = 0; row <= dotsDown; ++row) 
                g.DrawLine(p, 0, row * pixelsPerDot, width, row * pixelsPerDot);
            for (int col = 0; col <= dotsAcross; ++col)
                g.DrawLine(p, col * pixelsPerDot, 0, col * pixelsPerDot, height);

            // Fill area around the grid.
            g.FillRectangle(SystemBrushes.Control, width + 1, 0, clientSize.Width - width - 1, clientSize.Height);
            g.FillRectangle(SystemBrushes.Control, 0, height + 1, clientSize.Width, clientSize.Height - height - 1);
        }

        // Update grid size.
        void UpdateGridSize()
        {
            // Calculate that pixel size that makes the whole grid fit in the client size.
            int pixelSizeAcross = (clientSize.Width - 1) / dotsAcross;
            int pixelSizeDown = (clientSize.Height - 1) / dotsDown;

            // Must be at least 3 pixels per dot.
            pixelsPerDot = Math.Max(3, Math.Min(pixelSizeAcross, pixelSizeDown));
        }


        // Adjust the internal dot array to a new size.
        void ResizeTo(int newDotsAcross, int newDotsDown)
        {
            if (dots == null)
                dots = new List<List<bool>>();

            // adjust rows.
            if (dots.Count < newDotsDown) {
                for (int i = dots.Count; i < newDotsDown; ++i)
                    dots.Add(new List<bool>());
            }
            else if (dots.Count > newDotsDown)
                dots.RemoveRange(newDotsDown, dots.Count - newDotsDown);

            // adjust columns
            for (int row = 0; row < newDotsDown; ++row) {
                if (dots[row].Count < newDotsAcross) {
                    for (int i = dots[row].Count; i < newDotsAcross; ++i)
                        dots[row].Add(false);
                }
                else if (dots[row].Count > newDotsAcross)
                    dots[row].RemoveRange(newDotsAcross, dots[row].Count - newDotsAcross);
            }

            dotsAcross = newDotsAcross;
            dotsDown = newDotsDown;
        }

        private void DotGrid_Resize(object sender, EventArgs e)
        {
            clientSize = this.ClientSize;
            UpdateGridSize();
            Invalidate();
        }

        private void DotGrid_Paint(object sender, PaintEventArgs e)
        {
            // Draw the grid.
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
            DrawGrid(e.Graphics);

            // Draw the dots.
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            for (int row = 0; row < dotsDown; ++row)
                for (int col = 0; col < dotsAcross; ++col) {
                    if (GetDot(row, col))
                        DrawDot(e.Graphics, row, col);
                }
        }

        private void DotGrid_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) {
                int col = e.X / pixelsPerDot;
                int row = e.Y / pixelsPerDot;

                if (col >= 0 && col < dotsAcross && row >= 0 && row < dotsDown)
                    SetDot(row, col, !GetDot(row, col));
            }
        }
    }
}
