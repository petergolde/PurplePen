// PaperSizeControl.axaml.cs
//
// Avalonia port of the WinForms PaperSizeControl. Lets the user pick a paper
// size (from a list of standard sizes or "User defined"), a margin, and the
// orientation. Width / height / margin are exposed in hundredths of an inch
// through the PaperWidth / PaperHeight / PaperMargin styled properties (so they
// round-trip the same integer units the rest of PurplePen uses); the up/down
// fields display them in mm or inches depending on the current culture.
//
// The four styled properties are the single source of truth. UI events push
// into them (PushToProperties) and external changes pull back out
// (UpdateUIFromProperties); a single `suppress` flag breaks the feedback loop.
//
// Migrated from WinForms PurplePen/PaperSizeControl.cs.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using PurplePen;

namespace AvPurplePen.Views
{
    /// <summary>
    /// Reusable control for choosing paper size, margin, and orientation. All
    /// size values are in hundredths of an inch.
    /// </summary>
    public partial class PaperSizeControl : UserControl
    {
        // Index of the "User defined" entry in the combo (after the standard sizes).
        private static readonly int UserDefinedIndex = PrintingStandards.StandardPaperSizes.Length;

        // Breaks the UI <-> property feedback loop: true while we are pushing a
        // change in one direction so the reverse handler ignores it.
        private bool suppress;

        // ===== Styled properties (hundredths of an inch) =====

        /// <summary>Avalonia property backing <see cref="PaperWidth"/> (hundredths of an inch).</summary>
        public static readonly StyledProperty<int> PaperWidthProperty =
            AvaloniaProperty.Register<PaperSizeControl, int>(nameof(PaperWidth), defaultBindingMode: BindingMode.TwoWay);

        /// <summary>The paper width, in hundredths of an inch.</summary>
        public int PaperWidth
        {
            get => GetValue(PaperWidthProperty);
            set => SetValue(PaperWidthProperty, value);
        }

        /// <summary>Avalonia property backing <see cref="PaperHeight"/> (hundredths of an inch).</summary>
        public static readonly StyledProperty<int> PaperHeightProperty =
            AvaloniaProperty.Register<PaperSizeControl, int>(nameof(PaperHeight), defaultBindingMode: BindingMode.TwoWay);

        /// <summary>The paper height, in hundredths of an inch.</summary>
        public int PaperHeight
        {
            get => GetValue(PaperHeightProperty);
            set => SetValue(PaperHeightProperty, value);
        }

        /// <summary>Avalonia property backing <see cref="PaperMargin"/> (hundredths of an inch).</summary>
        public static readonly StyledProperty<int> PaperMarginProperty =
            AvaloniaProperty.Register<PaperSizeControl, int>(nameof(PaperMargin), defaultBindingMode: BindingMode.TwoWay);

        /// <summary>The margin (all sides), in hundredths of an inch.</summary>
        public int PaperMargin
        {
            get => GetValue(PaperMarginProperty);
            set => SetValue(PaperMarginProperty, value);
        }

        /// <summary>Avalonia property backing <see cref="Landscape"/>.</summary>
        public static readonly StyledProperty<bool> LandscapeProperty =
            AvaloniaProperty.Register<PaperSizeControl, bool>(nameof(Landscape), defaultBindingMode: BindingMode.TwoWay);

        /// <summary>Whether the page is in landscape orientation.</summary>
        public bool Landscape
        {
            get => GetValue(LandscapeProperty);
            set => SetValue(LandscapeProperty, value);
        }

        /// <summary>Avalonia property backing <see cref="SeparateMargins"/>.</summary>
        public static readonly StyledProperty<bool> SeparateMarginsProperty =
            AvaloniaProperty.Register<PaperSizeControl, bool>(nameof(SeparateMargins), defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Whether the four sides have separate, individually-editable margins
        /// (shown in a "Margins" group box) instead of a single margin applied to
        /// all sides. When true, the per-side margin properties
        /// (<see cref="PaperMarginTop"/>, etc.) are used; when false, the single
        /// <see cref="PaperMargin"/> is used.
        /// </summary>
        public bool SeparateMargins
        {
            get => GetValue(SeparateMarginsProperty);
            set => SetValue(SeparateMarginsProperty, value);
        }

        /// <summary>Avalonia property backing <see cref="PaperMarginTop"/> (hundredths of an inch).</summary>
        public static readonly StyledProperty<int> PaperMarginTopProperty =
            AvaloniaProperty.Register<PaperSizeControl, int>(nameof(PaperMarginTop), defaultBindingMode: BindingMode.TwoWay);

        /// <summary>The top margin, in hundredths of an inch. Used when <see cref="SeparateMargins"/> is true.</summary>
        public int PaperMarginTop
        {
            get => GetValue(PaperMarginTopProperty);
            set => SetValue(PaperMarginTopProperty, value);
        }

        /// <summary>Avalonia property backing <see cref="PaperMarginBottom"/> (hundredths of an inch).</summary>
        public static readonly StyledProperty<int> PaperMarginBottomProperty =
            AvaloniaProperty.Register<PaperSizeControl, int>(nameof(PaperMarginBottom), defaultBindingMode: BindingMode.TwoWay);

        /// <summary>The bottom margin, in hundredths of an inch. Used when <see cref="SeparateMargins"/> is true.</summary>
        public int PaperMarginBottom
        {
            get => GetValue(PaperMarginBottomProperty);
            set => SetValue(PaperMarginBottomProperty, value);
        }

        /// <summary>Avalonia property backing <see cref="PaperMarginLeft"/> (hundredths of an inch).</summary>
        public static readonly StyledProperty<int> PaperMarginLeftProperty =
            AvaloniaProperty.Register<PaperSizeControl, int>(nameof(PaperMarginLeft), defaultBindingMode: BindingMode.TwoWay);

        /// <summary>The left margin, in hundredths of an inch. Used when <see cref="SeparateMargins"/> is true.</summary>
        public int PaperMarginLeft
        {
            get => GetValue(PaperMarginLeftProperty);
            set => SetValue(PaperMarginLeftProperty, value);
        }

        /// <summary>Avalonia property backing <see cref="PaperMarginRight"/> (hundredths of an inch).</summary>
        public static readonly StyledProperty<int> PaperMarginRightProperty =
            AvaloniaProperty.Register<PaperSizeControl, int>(nameof(PaperMarginRight), defaultBindingMode: BindingMode.TwoWay);

        /// <summary>The right margin, in hundredths of an inch. Used when <see cref="SeparateMargins"/> is true.</summary>
        public int PaperMarginRight
        {
            get => GetValue(PaperMarginRightProperty);
            set => SetValue(PaperMarginRightProperty, value);
        }

        public PaperSizeControl()
        {
            InitializeComponent();
            InitUnits();
            InitPaperSizes();
            UpdateMarginVisibility();
            UpdateUIFromProperties();
        }

        /// <summary>
        /// Configures the up/down fields' units, decimal places, increment, and
        /// maximum based on whether the current culture is metric.
        /// </summary>
        private void InitUnits()
        {
            string units;
            int decimalPlaces;
            decimal increment;
            decimal maximum;

            if (Util.IsCurrentCultureMetric()) {
                units = "mm";
                decimalPlaces = 1;
                increment = 1.0M;
                maximum = 5000;
            }
            else {
                units = "in";
                decimalPlaces = 2;
                increment = 0.05M;
                maximum = 100;
            }

            string format = "0." + new string('0', decimalPlaces);
            foreach (NumericUpDown upDown in new[] { upDownWidth, upDownHeight, upDownMargin,
                                                     upDownMarginTop, upDownMarginBottom,
                                                     upDownMarginLeft, upDownMarginRight }) {
                upDown.FormatString = format;
                upDown.Increment = increment;
                upDown.Maximum = maximum;
                upDown.Minimum = 0;
            }

            labelUnitsWidth.Text = labelUnitsHeight.Text = labelUnitsMargin.Text = units;
            labelUnitsMarginTop.Text = labelUnitsMarginBottom.Text =
                labelUnitsMarginLeft.Text = labelUnitsMarginRight.Text = units;
        }

        /// <summary>Populates the paper-size combo with the standard sizes plus "User defined".</summary>
        private void InitPaperSizes()
        {
            System.Collections.Generic.List<string> items = new System.Collections.Generic.List<string>();
            foreach (PrintingPaperSize paperSize in PrintingStandards.StandardPaperSizes) {
                items.Add(Util.GetPaperSizeText(paperSize));
            }
            items.Add(MiscText.UserDefined);
            comboBoxPaperSize.ItemsSource = items;
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == PaperWidthProperty || change.Property == PaperHeightProperty ||
                change.Property == PaperMarginProperty || change.Property == LandscapeProperty ||
                change.Property == PaperMarginTopProperty || change.Property == PaperMarginBottomProperty ||
                change.Property == PaperMarginLeftProperty || change.Property == PaperMarginRightProperty) {
                UpdateUIFromProperties();
            }
            else if (change.Property == SeparateMarginsProperty) {
                UpdateMarginVisibility();
            }
        }

        /// <summary>
        /// Shows either the single-margin row or the separate-margins group box
        /// according to <see cref="SeparateMargins"/>.
        /// </summary>
        private void UpdateMarginVisibility()
        {
            bool separate = SeparateMargins;
            labelMargin.IsVisible = upDownMargin.IsVisible = labelUnitsMargin.IsVisible = !separate;
            groupSeparateMargins.IsVisible = separate;
        }

        /// <summary>
        /// Reflects the current property values into the UI: selects the matching
        /// standard size (or "User defined"), fills the up/down fields, and sets
        /// the orientation radios.
        /// </summary>
        private void UpdateUIFromProperties()
        {
            if (suppress)
                return;
            suppress = true;
            try {
                // Find a standard size matching the current width/height.
                int standardIndex = -1;
                for (int i = 0; i < PrintingStandards.StandardPaperSizes.Length; ++i) {
                    PrintingPaperSize ps = PrintingStandards.StandardPaperSizes[i];
                    if ((int)Math.Round(ps.SizeInHundreths.Width) == PaperWidth &&
                        (int)Math.Round(ps.SizeInHundreths.Height) == PaperHeight) {
                        standardIndex = i;
                        break;
                    }
                }

                if (standardIndex >= 0) {
                    comboBoxPaperSize.SelectedIndex = standardIndex;
                    upDownWidth.IsEnabled = upDownHeight.IsEnabled = false;
                }
                else {
                    comboBoxPaperSize.SelectedIndex = UserDefinedIndex;
                    upDownWidth.IsEnabled = upDownHeight.IsEnabled = true;
                }

                upDownWidth.Value = Util.GetDistanceValue(PaperWidth);
                upDownHeight.Value = Util.GetDistanceValue(PaperHeight);
                upDownMargin.Value = Util.GetDistanceValue(PaperMargin);
                upDownMarginTop.Value = Util.GetDistanceValue(PaperMarginTop);
                upDownMarginBottom.Value = Util.GetDistanceValue(PaperMarginBottom);
                upDownMarginLeft.Value = Util.GetDistanceValue(PaperMarginLeft);
                upDownMarginRight.Value = Util.GetDistanceValue(PaperMarginRight);

                radioPortrait.IsChecked = !Landscape;
                radioLandscape.IsChecked = Landscape;
            }
            finally {
                suppress = false;
            }
        }

        /// <summary>Reads the current UI state and writes it into the styled properties.</summary>
        private void PushToProperties()
        {
            if (suppress)
                return;
            suppress = true;
            try {
                PaperWidth = Util.GetDistanceFromValue(upDownWidth.Value ?? 0);
                PaperHeight = Util.GetDistanceFromValue(upDownHeight.Value ?? 0);
                PaperMargin = Util.GetDistanceFromValue(upDownMargin.Value ?? 0);
                PaperMarginTop = Util.GetDistanceFromValue(upDownMarginTop.Value ?? 0);
                PaperMarginBottom = Util.GetDistanceFromValue(upDownMarginBottom.Value ?? 0);
                PaperMarginLeft = Util.GetDistanceFromValue(upDownMarginLeft.Value ?? 0);
                PaperMarginRight = Util.GetDistanceFromValue(upDownMarginRight.Value ?? 0);
                Landscape = radioLandscape.IsChecked == true;
            }
            finally {
                suppress = false;
            }
        }

        /// <summary>
        /// Combo selection changed: for a standard size, fill and disable the
        /// width/height fields; for "User defined", enable them. Then publish.
        /// </summary>
        private void ComboBoxPaperSize_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (suppress)
                return;

            int index = comboBoxPaperSize.SelectedIndex;
            if (index >= 0 && index < PrintingStandards.StandardPaperSizes.Length) {
                PrintingPaperSize ps = PrintingStandards.StandardPaperSizes[index];
                suppress = true;
                try {
                    upDownWidth.Value = Util.GetDistanceValue((int)Math.Round(ps.SizeInHundreths.Width));
                    upDownHeight.Value = Util.GetDistanceValue((int)Math.Round(ps.SizeInHundreths.Height));
                    upDownWidth.IsEnabled = upDownHeight.IsEnabled = false;
                }
                finally {
                    suppress = false;
                }
            }
            else {
                upDownWidth.IsEnabled = upDownHeight.IsEnabled = true;
            }

            PushToProperties();
        }

        /// <summary>Up/down value changed: publish the new size/margin.</summary>
        private void UpDown_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            PushToProperties();
        }

        /// <summary>Orientation radio changed: publish the new orientation.</summary>
        private void Orientation_Changed(object? sender, RoutedEventArgs e)
        {
            PushToProperties();
        }
    }
}
