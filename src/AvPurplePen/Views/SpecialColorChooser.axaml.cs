// SpecialColorChooser.axaml.cs
//
// Avalonia custom control for choosing a SpecialColor. Provides a drop-down
// list of preset colors (with color swatches) and a button to open the
// ColorChooserDialog for custom colors.
//
// Bindable properties:
//   Color       (SpecialColor) — the currently selected color.
//   PurpleColor (CmykColor)    — the CMYK color used for the Purple and
//                                 Lower Purple swatches.
//
// Ported from the WinForms SpecialColorChooser helper class in
// PurplePen/ColorChooserDialog.cs.

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using PurplePen;
using PurplePen.Graphics2D;
using PurplePen.ViewModels;

namespace AvPurplePen
{
    /// <summary>
    /// Data item for a single entry in the SpecialColorChooser drop-down.
    /// Public and non-nested so it can be referenced via x:DataType in AXAML.
    /// </summary>
    public class SpecialColorItem
    {
        /// <summary>Localized display name (e.g. "Black", "Purple", "Custom Color...").</summary>
        public string Name { get; set; } = "";

        /// <summary>Brush for the color swatch rectangle, or null if no swatch.</summary>
        public IBrush? SwatchBrush { get; set; }

        /// <summary>Whether to show the swatch rectangle.</summary>
        public bool HasSwatch => SwatchBrush != null;

        /// <summary>
        /// The SpecialColor this item represents. Null for the uninitialized
        /// "Custom Color" entry (before the user has picked a custom color).
        /// </summary>
        public SpecialColor? SpecialColor { get; set; }
    }
}

namespace AvPurplePen.Views
{
    /// <summary>
    /// Custom control for choosing a SpecialColor. Displays a ComboBox with
    /// color swatches on the left and a "Change Color..." button on the right.
    /// </summary>
    public partial class SpecialColorChooser : UserControl
    {
        private bool suppressSync;
        private readonly ObservableCollection<SpecialColorItem> colorItems = new();

        /// <summary>Styled property for the currently selected SpecialColor.</summary>
        public static readonly StyledProperty<SpecialColor> ColorProperty =
            AvaloniaProperty.Register<SpecialColorChooser, SpecialColor>(
                nameof(Color), defaultValue: SpecialColor.Black);

        /// <summary>Gets or sets the currently selected SpecialColor.</summary>
        public SpecialColor Color
        {
            get => GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        /// <summary>Styled property for the purple color used in swatches.</summary>
        public static readonly StyledProperty<CmykColor?> PurpleColorProperty =
            AvaloniaProperty.Register<SpecialColorChooser, CmykColor?>(nameof(PurpleColor));

        /// <summary>
        /// Gets or sets the CMYK color used for the Upper Purple and Lower
        /// Purple swatches.
        /// </summary>
        public CmykColor? PurpleColor
        {
            get => GetValue(PurpleColorProperty);
            set => SetValue(PurpleColorProperty, value);
        }

        public SpecialColorChooser()
        {
            InitializeComponent();
            BuildColorItems();
            comboBoxColor.ItemsSource = colorItems;
            suppressSync = true;
            comboBoxColor.SelectedIndex = 0;
            suppressSync = false;
        }

        /// <summary>Responds to changes in Color or PurpleColor properties.</summary>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == PurpleColorProperty) {
                UpdatePurpleSwatches();
            }
            else if (change.Property == ColorProperty) {
                SyncSelectionFromColor();
            }
        }

        /// <summary>Populates the drop-down with preset colors and a Custom entry.</summary>
        private void BuildColorItems()
        {
            CmykColor purple = PurpleColor ?? CmykColor.FromCmyk(0.55F, 0.85F, 0, 0.07F);

            colorItems.Clear();
            colorItems.Add(MakeItem(UIText.MiscText_Black, SpecialColor.Black, CmykColor.FromCmyk(0, 0, 0, 1)));
            colorItems.Add(MakeItem(UIText.MiscText_Purple, SpecialColor.UpperPurple, purple));
            colorItems.Add(MakeItem(UIText.MiscText_LowerPurple, SpecialColor.LowerPurple, purple));
            colorItems.Add(MakeItem(UIText.MiscText_Red, new SpecialColor(CmykColor.FromCmyk(0, 1, 1, 0)), CmykColor.FromCmyk(0, 1, 1, 0)));
            colorItems.Add(MakeItem(UIText.MiscText_Yellow, new SpecialColor(CmykColor.FromCmyk(0, 0, 1, 0)), CmykColor.FromCmyk(0, 0, 1, 0)));
            colorItems.Add(MakeItem(UIText.MiscText_Green, new SpecialColor(CmykColor.FromCmyk(1, 0, 1, 0)), CmykColor.FromCmyk(1, 0, 1, 0)));
            colorItems.Add(MakeItem(UIText.MiscText_LightBlue, new SpecialColor(CmykColor.FromCmyk(1, 0, 0, 0)), CmykColor.FromCmyk(1, 0, 0, 0)));
            colorItems.Add(MakeItem(UIText.MiscText_DarkBlue, new SpecialColor(CmykColor.FromCmyk(1, 1, 0, 0)), CmykColor.FromCmyk(1, 1, 0, 0)));
            colorItems.Add(new SpecialColorItem { Name = UIText.MiscText_CustomColor });
        }

        /// <summary>Creates a preset color item with a swatch brush.</summary>
        private static SpecialColorItem MakeItem(string name, SpecialColor specialColor, CmykColor cmykColor)
        {
            return new SpecialColorItem {
                Name = name,
                SpecialColor = specialColor,
                SwatchBrush = CmykToBrush(cmykColor)
            };
        }

        /// <summary>Converts a CmykColor to an Avalonia SolidColorBrush via SwopColorConverter.</summary>
        private static SolidColorBrush CmykToBrush(CmykColor cmyk)
        {
            System.Drawing.Color sdColor = SwopColorConverter.Instance.ToColor(cmyk);
            return new SolidColorBrush(Avalonia.Media.Color.FromRgb(sdColor.R, sdColor.G, sdColor.B));
        }

        /// <summary>Updates the Purple and Lower Purple swatch brushes when PurpleColor changes.</summary>
        private void UpdatePurpleSwatches()
        {
            CmykColor purple = PurpleColor ?? CmykColor.FromCmyk(0.55F, 0.85F, 0, 0.07F);

            if (colorItems.Count >= 3) {
                colorItems[1] = MakeItem(UIText.MiscText_Purple, SpecialColor.UpperPurple, purple);
                colorItems[2] = MakeItem(UIText.MiscText_LowerPurple, SpecialColor.LowerPurple, purple);

                // Re-select if the current selection was one of the replaced items.
                if (comboBoxColor.SelectedIndex == 1 || comboBoxColor.SelectedIndex == 2) {
                    suppressSync = true;
                    int idx = comboBoxColor.SelectedIndex;
                    comboBoxColor.SelectedIndex = -1;
                    comboBoxColor.SelectedIndex = idx;
                    suppressSync = false;
                }
            }
        }

        /// <summary>
        /// Syncs the ComboBox selection to match the current Color property value.
        /// Called when Color is set externally.
        /// </summary>
        private void SyncSelectionFromColor()
        {
            if (suppressSync) return;

            SpecialColor color = Color;

            // Search presets for a match.
            for (int i = 0; i < colorItems.Count - 1; i++) {
                SpecialColor? itemColor = colorItems[i].SpecialColor;
                if (itemColor != null && itemColor.Equals(color)) {
                    suppressSync = true;
                    comboBoxColor.SelectedIndex = i;
                    suppressSync = false;
                    return;
                }
            }

            // No preset match — update the Custom entry with the custom color.
            int customIndex = colorItems.Count - 1;
            colorItems[customIndex] = new SpecialColorItem {
                Name = UIText.MiscText_CustomColor,
                SpecialColor = color,
                SwatchBrush = CmykToBrush(color.CustomColor)
            };
            suppressSync = true;
            comboBoxColor.SelectedIndex = customIndex;
            suppressSync = false;
        }

        /// <summary>Handles ComboBox selection changes from the user.</summary>
        private async void ComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (suppressSync) return;
            if (comboBoxColor.SelectedItem is not SpecialColorItem item) return;

            if (item.SpecialColor == null) {
                // Uninitialized "Custom Color" entry — open the color chooser.
                await ShowCustomColorDialog(CmykColor.FromCmyk(0, 0, 0, 0));
            }
            else {
                suppressSync = true;
                Color = item.SpecialColor;
                suppressSync = false;
            }
        }

        /// <summary>Opens the ColorChooserDialog seeded with the current custom color.</summary>
        private async void ChangeColor_Click(object? sender, RoutedEventArgs e)
        {
            CmykColor startColor = Color.Kind == SpecialColor.ColorKind.Custom
                ? Color.CustomColor
                : CmykColor.FromCmyk(0, 0, 0, 0);

            await ShowCustomColorDialog(startColor);
        }

        /// <summary>Shows the ColorChooserDialog and applies the result.</summary>
        private async Task ShowCustomColorDialog(CmykColor startColor)
        {
            ColorChooserDialogViewModel vm = new ColorChooserDialogViewModel();
            vm.Color = startColor;

            bool result = await Services.DialogService.ShowDialogAsync(vm);
            if (result) {
                Color = new SpecialColor(vm.Color);
            }
        }
    }
}
