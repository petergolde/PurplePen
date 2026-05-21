// DotGrid.axaml.cs
//
// Avalonia port of the WinForms DotGrid custom control. Displays a grid of
// squares with optional filled circles (dots). Clicking a square toggles its
// dot on/off. The single bindable property is Dots (bool[,]), whose dimensions
// determine the grid size.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Grid control that displays togglable filled circles in a grid of squares.
    /// Grid dimensions are derived from the Dots array.
    /// </summary>
    public partial class DotGrid : UserControl
    {
        // Diameter of dot as fraction of the grid cell size.
        private const double DotDiameterFraction = 0.7;

        private static readonly IBrush GridLineBrush = new SolidColorBrush(Colors.LightGray);
        private static readonly IPen GridLinePen = new Pen(GridLineBrush, 1);
        private static readonly IBrush DotBrush = Brushes.Black;

        // Internal dot state, indexed [row, col].
        private bool[,] dots = new bool[1, 1];

        public static readonly StyledProperty<bool[,]?> DotsProperty =
            AvaloniaProperty.Register<DotGrid, bool[,]?>(nameof(Dots));

        /// <summary>
        /// Gets or sets all dot values as a bool[row, col] array.
        /// The array dimensions determine the grid size.
        /// </summary>
        public bool[,]? Dots
        {
            get => GetValue(DotsProperty);
            set => SetValue(DotsProperty, value);
        }

        /// <summary>Number of grid columns (from the internal dots array).</summary>
        private int DotsAcross => dots.GetLength(1);

        /// <summary>Number of grid rows (from the internal dots array).</summary>
        private int DotsDown => dots.GetLength(0);

        public DotGrid()
        {
            InitializeComponent();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == DotsProperty)
            {
                ApplyDotsFromProperty();
                InvalidateVisual();
            }
        }

        /// <summary>Copy an incoming Dots property value into the internal array.</summary>
        private void ApplyDotsFromProperty()
        {
            bool[,]? incoming = Dots;
            if (incoming == null)
            {
                dots = new bool[1, 1];
                return;
            }

            dots = (bool[,])incoming.Clone();
        }

        /// <summary>Push the internal dot state out to the Dots property.</summary>
        private void SyncDotsProperty()
        {
            SetCurrentValue(DotsProperty, (bool[,])dots.Clone());
        }

        /// <summary>Calculate the pixel size of each grid cell.</summary>
        private double GetCellSize()
        {
            double availableWidth = Bounds.Width;
            double availableHeight = Bounds.Height;
            int across = DotsAcross;
            int down = DotsDown;

            if (across <= 0 || down <= 0)
                return 0;

            double sizeByWidth = availableWidth / across;
            double sizeByHeight = availableHeight / down;

            return Math.Max(3, Math.Min(sizeByWidth, sizeByHeight));
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            int across = DotsAcross;
            int down = DotsDown;
            double cellSize = GetCellSize();

            if (cellSize <= 0)
                return;

            double gridWidth = across * cellSize;
            double gridHeight = down * cellSize;

            // Draw white background for the grid area.
            context.DrawRectangle(Brushes.White, null, new Rect(0, 0, gridWidth, gridHeight));

            // Draw grid lines.
            for (int row = 0; row <= down; row++)
            {
                double y = row * cellSize;
                context.DrawLine(GridLinePen, new Point(0, y), new Point(gridWidth, y));
            }

            for (int col = 0; col <= across; col++)
            {
                double x = col * cellSize;
                context.DrawLine(GridLinePen, new Point(x, 0), new Point(x, gridHeight));
            }

            // Draw dots.
            double inset = cellSize * (1 - DotDiameterFraction) / 2.0;
            double dotDiameter = cellSize * DotDiameterFraction;
            double radius = dotDiameter / 2.0;

            for (int row = 0; row < down; row++)
            {
                for (int col = 0; col < across; col++)
                {
                    if (dots[row, col])
                    {
                        double cx = col * cellSize + inset + radius;
                        double cy = row * cellSize + inset + radius;
                        context.DrawEllipse(DotBrush, null, new Point(cx, cy), radius, radius);
                    }
                }
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            PointerPointProperties props = e.GetCurrentPoint(this).Properties;
            if (!props.IsLeftButtonPressed)
                return;

            double cellSize = GetCellSize();
            if (cellSize <= 0)
                return;

            Point pos = e.GetPosition(this);
            int col = (int)(pos.X / cellSize);
            int row = (int)(pos.Y / cellSize);

            if (row >= 0 && row < DotsDown && col >= 0 && col < DotsAcross)
            {
                dots[row, col] = !dots[row, col];
                SyncDotsProperty();
                InvalidateVisual();
            }
        }
    }
}
