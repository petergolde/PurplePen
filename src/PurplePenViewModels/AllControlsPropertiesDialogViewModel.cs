// AllControlsPropertiesDialogViewModel.cs
//
// ViewModel for the All Controls Properties dialog. Exposes the printing
// scale and description appearance for the "All Controls" view as bindable
// properties for the AXAML view.
//
// The print scale combo is editable, so PrintScaleText is a string and its
// validation is done in the code-behind OkButton_Click (editable ComboBox
// does not support inline validation display).
//
// Migrated from WinForms PurplePen/AllControlsProperties.cs.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the All Controls Properties dialog.
    /// Provides bindable properties for the map printing scale and the
    /// description appearance used when printing the All Controls view.
    /// </summary>
    public partial class AllControlsPropertiesDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// The print scale text entered or selected by the user (e.g. "10000").
        /// This is a string because the scale combo is editable. Validation is
        /// done in the code-behind OkButton_Click because an editable ComboBox
        /// does not support inline validation display.
        /// </summary>
        [ObservableProperty]
        private string printScaleText = "";

        /// <summary>
        /// Available print scales for the combo box dropdown, computed from the
        /// map scale by the caller via <see cref="InitializePrintScales"/>.
        /// </summary>
        public ObservableCollection<string> AvailablePrintScales { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Selected description appearance (Symbols, Text, or SymbolsAndText).
        /// </summary>
        [ObservableProperty]
        private DescriptionKind descKind = DescriptionKind.Symbols;

        /// <summary>
        /// Parameterless constructor for the Avalonia designer.
        /// Populates fields with sample data.
        /// </summary>
        public AllControlsPropertiesDialogViewModel()
        {
            AvailablePrintScales.Add("5000");
            AvailablePrintScales.Add("7500");
            AvailablePrintScales.Add("10000");
            AvailablePrintScales.Add("15000");
            PrintScaleText = "10000";
        }

        /// <summary>
        /// Populates the available print scales based on the map scale.
        /// Called by the dialog opener after creating the ViewModel.
        /// </summary>
        /// <param name="mapScale">The map's native scale (e.g. 15000).</param>
        public void InitializePrintScales(float mapScale)
        {
            AvailablePrintScales.Clear();
            foreach (int scale in MapUtil.PrintScaleList(mapScale)) {
                AvailablePrintScales.Add(scale.ToString());
            }
        }

        /// <summary>
        /// Gets or sets the print scale as a float.
        /// Returns 0 if the text cannot be parsed.
        /// </summary>
        public float PrintScale
        {
            get {
                if (float.TryParse(PrintScaleText, out float scale))
                    return scale;
                return 0;
            }
            set {
                PrintScaleText = value.ToString();
            }
        }
    }
}
