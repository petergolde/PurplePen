// EventFileDialogViewModel.cs
//
// ViewModel for the Event File (Change Map File) dialog. Lets the user
// choose a map file and, for bitmap or PDF maps, enter scale and DPI
// values. Validates the chosen file via CoreMapUtil.ValidateMapFile and
// exposes the detected MapType plus any error message.

using System.Drawing;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Change Map File dialog. The caller calls
    /// <see cref="SetInitialMapFile"/> (and optionally sets
    /// <see cref="MapScale"/> and <see cref="Dpi"/>) before showing the
    /// dialog, then reads them back after the dialog returns true.
    /// </summary>
    public partial class EventFileDialogViewModel : ViewModelBase
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        [NotifyPropertyChangedFor(nameof(HasMapFile))]
        [NotifyPropertyChangedFor(nameof(ShowScaleDpiPanel))]
        [NotifyPropertyChangedFor(nameof(ShowDpiRow))]
        [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
        private string mapFile = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowScaleDpiPanel))]
        [NotifyPropertyChangedFor(nameof(ShowDpiRow))]
        [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
        private MapType mapType;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasError))]
        [NotifyPropertyChangedFor(nameof(ShowScaleDpiPanel))]
        private string errorMessageText = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
        private string scaleText = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
        private string dpiText = "";

        /// <summary>
        /// True when a map file has been selected (even if it failed validation).
        /// </summary>
        public bool HasMapFile => !string.IsNullOrEmpty(MapFile);

        /// <summary>
        /// True when the selected file failed validation.
        /// </summary>
        public bool HasError => HasMapFile && !string.IsNullOrEmpty(ErrorMessageText);

        /// <summary>
        /// True when the scale/DPI panel should be visible (Bitmap or PDF map).
        /// </summary>
        public bool ShowScaleDpiPanel => HasMapFile && !HasError && (MapType == MapType.Bitmap || MapType == MapType.PDF);

        /// <summary>
        /// True when the DPI row should be visible (Bitmap map only).
        /// </summary>
        public bool ShowDpiRow => MapType == MapType.Bitmap;

        /// <summary>
        /// Determines whether the OK button should be enabled.
        /// </summary>
        public bool IsOkEnabled
        {
            get {
                if (MapType == MapType.OCAD)
                    return true;
                else if (MapType == MapType.Bitmap)
                    return float.TryParse(ScaleText, out _) && float.TryParse(DpiText, out _);
                else if (MapType == MapType.PDF)
                    return float.TryParse(ScaleText, out _);
                else
                    return false;
            }
        }

        /// <summary>
        /// Gets the parsed map scale value, or 0 if the text is not a valid number.
        /// </summary>
        public float MapScale
        {
            get => float.TryParse(ScaleText, out float scale) ? scale : 0;
            set => ScaleText = value.ToString();
        }

        /// <summary>
        /// Gets the parsed DPI value, or 0 if the text is not a valid number.
        /// </summary>
        public float Dpi
        {
            get => float.TryParse(DpiText, out float dpi) ? dpi : 0;
            set => DpiText = value.ToString();
        }

        /// <summary>
        /// Sets the initial map file path and validates it. Call this before
        /// showing the dialog to seed the current file. Set <see cref="MapScale"/>
        /// and <see cref="Dpi"/> after this call (validation overwrites them).
        /// </summary>
        public void SetInitialMapFile(string path)
        {
            MapFile = path;
            ValidateMapFile();
        }

        /// <summary>
        /// Opens a file picker and, if a file is chosen, sets <see cref="MapFile"/>
        /// and validates it.
        /// </summary>
        [RelayCommand]
        private async Task ChooseMapFile()
        {
            FileOpenSingleViewModel fileVm = new FileOpenSingleViewModel {
                FileFilters = MiscText.ChangeMapFile_FileFilter,
            };

            bool ok = await Services.DialogService.ShowDialogAsync(fileVm);
            if (ok && fileVm.SelectedFile != null) {
                MapFile = fileVm.SelectedFile;
                ValidateMapFile();
            }
        }

        /// <summary>
        /// Validates the current map file path and updates MapType, ScaleText,
        /// DpiText, and ErrorMessageText accordingly.
        /// </summary>
        private void ValidateMapFile()
        {
            if (string.IsNullOrEmpty(MapFile)) {
                MapType = MapType.None;
                ErrorMessageText = "";
                return;
            }

            bool ok = CoreMapUtil.ValidateMapFile(MapFile, out float scale, out float dpi, out Size bitmapSize, out RectangleF mapBounds, out MapType detectedType, out int? _, out string errorMessage);

            if (ok) {
                MapType = detectedType;
                ErrorMessageText = "";

                if (detectedType == MapType.OCAD) {
                    ScaleText = scale.ToString();
                }
                else if (detectedType == MapType.Bitmap) {
                    DpiText = dpi.ToString();
                }
                else if (detectedType == MapType.PDF) {
                    ScaleText = scale.ToString();
                }
            }
            else {
                MapType = MapType.None;
                ErrorMessageText = errorMessage;
            }
        }
    }
}
