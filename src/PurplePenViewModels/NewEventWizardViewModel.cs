// NewEventWizardViewModel.cs
//
// Single ViewModel for the entire "New Event" wizard. The wizard walks the
// user through several pages (title, map file, optional bitmap/PDF scale,
// print scale, paper size, output directory, IOF standards, control
// numbering, and a final review) and assembles a Controller.CreateEventInfo
// to create the event.
//
// Because the wizard pages are heavily coupled (the print-scale default comes
// from the map scale, the paper-size default comes from the map bounds + print
// scale + map type, the final page shows the assembled path, etc.), all shared
// state lives on this one ViewModel rather than being split across nine
// per-page ViewModels. Each page View is a separate UserControl that binds to
// this same ViewModel as its DataContext and is shown/hidden by the
// Is<Page>Page bool properties below.
//
// Migrated from WinForms PurplePen/NewEventWizard.cs (+ the NewEvent* page
// UserControls).

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// The pages of the New Event wizard, in display order.
    /// </summary>
    public enum WizardPage
    {
        Title,
        MapFile,
        BitmapScale,    // shown only for Bitmap / PDF maps
        PrintScale,
        PaperSize,
        Directory,
        Standards,
        Numbering,
        Final
    }

    /// <summary>
    /// ViewModel for the New Event wizard. The caller constructs it, shows the
    /// dialog via <c>DialogService.ShowDialogAsync</c>, and on a true result
    /// reads <see cref="CreateEventInfo"/> to create the event.
    /// </summary>
    public partial class NewEventWizardViewModel : ViewModelBase
    {
        // Allowable ranges for the scale and DPI fields (mirrors EventFileDialogViewModel).
        private const float MinScale = 100;
        private const float MaxScale = 1000000;
        private const float MinDpi = 10;
        private const float MaxDpi = 10000;

        // The pages in display order. Navigation walks this array, skipping the
        // BitmapScale page for non-bitmap/PDF maps.
        private static readonly WizardPage[] PageOrder = {
            WizardPage.Title, WizardPage.MapFile, WizardPage.BitmapScale, WizardPage.PrintScale,
            WizardPage.PaperSize, WizardPage.Directory, WizardPage.Standards, WizardPage.Numbering,
            WizardPage.Final
        };

        // Validation results from the chosen map file that aren't directly bound
        // to the UI but are needed to assemble the final CreateEventInfo.
        private Size bitmapSize;
        private RectangleF mapBounds;
        private int? lowerPurpleMapLayer;

        // Set once the paper-size page has computed its defaults, so re-entering
        // the page doesn't clobber the user's manual changes.
        private bool paperSizeInitialized;

        private Controller.CreateEventInfo createEventInfo;

        /// <summary>
        /// Raised when the wizard should close. The argument is the dialog
        /// result: true when the event was successfully prepared (Finish), false
        /// on Cancel. The View's code-behind subscribes and calls Close(result).
        /// </summary>
        public event Action<bool>? RequestClose;

        public NewEventWizardViewModel()
        {
            // Seed the IOF standard radios from the last-used settings (defaulting
            // to the newest standards if settings aren't initialized, e.g. in tests).
            string mapStandard = UserSettings.Current?.NewEventMapStandard ?? "2017";
            string descriptionStandard = UserSettings.Current?.NewEventDescriptionStandard ?? "2018";

            isMap2017 = (mapStandard == "2017");
            isMapSpr2019 = (mapStandard == "Spr2019");
            isMap2000 = !isMap2017 && !isMapSpr2019;

            isDescriptions2018 = (descriptionStandard == "2018");
            isDescriptions2004 = !isDescriptions2018;
        }

        // ======================================================================
        // Navigation state
        // ======================================================================

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsTitlePage))]
        [NotifyPropertyChangedFor(nameof(IsMapFilePage))]
        [NotifyPropertyChangedFor(nameof(IsBitmapScalePage))]
        [NotifyPropertyChangedFor(nameof(IsPrintScalePage))]
        [NotifyPropertyChangedFor(nameof(IsPaperSizePage))]
        [NotifyPropertyChangedFor(nameof(IsDirectoryPage))]
        [NotifyPropertyChangedFor(nameof(IsStandardsPage))]
        [NotifyPropertyChangedFor(nameof(IsNumberingPage))]
        [NotifyPropertyChangedFor(nameof(IsFinalPage))]
        [NotifyPropertyChangedFor(nameof(IsLastPage))]
        [NotifyPropertyChangedFor(nameof(CanGoBack))]
        [NotifyPropertyChangedFor(nameof(CanProceed))]
        private WizardPage currentPage = WizardPage.Title;

        public bool IsTitlePage => CurrentPage == WizardPage.Title;
        public bool IsMapFilePage => CurrentPage == WizardPage.MapFile;
        public bool IsBitmapScalePage => CurrentPage == WizardPage.BitmapScale;
        public bool IsPrintScalePage => CurrentPage == WizardPage.PrintScale;
        public bool IsPaperSizePage => CurrentPage == WizardPage.PaperSize;
        public bool IsDirectoryPage => CurrentPage == WizardPage.Directory;
        public bool IsStandardsPage => CurrentPage == WizardPage.Standards;
        public bool IsNumberingPage => CurrentPage == WizardPage.Numbering;
        public bool IsFinalPage => CurrentPage == WizardPage.Final;

        /// <summary>True on the last page (Final), where Next becomes Finish.</summary>
        public bool IsLastPage => CurrentPage == WizardPage.Final;

        /// <summary>True when there is a previous page to go back to.</summary>
        public bool CanGoBack => CurrentPage != PageOrder[0];

        /// <summary>True when the map is a Bitmap or PDF (so the scale page applies).</summary>
        private bool IsBitmapOrPdf => MapType == MapType.Bitmap || MapType == MapType.PDF;

        // ======================================================================
        // Page 1: Title
        // ======================================================================

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanProceed))]
        private string titleText = "";

        /// <summary>The .ppen file name derived from the event title.</summary>
        private string EventFileName => Util.FilterInvalidPathChars(TitleText) + ".ppen";

        // ======================================================================
        // Page 2: Map file
        // ======================================================================

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasMapFile))]
        [NotifyPropertyChangedFor(nameof(HasMapError))]
        [NotifyPropertyChangedFor(nameof(ShowMapInfo))]
        [NotifyPropertyChangedFor(nameof(CanProceed))]
        private string mapFileName = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowDpiRow))]
        [NotifyPropertyChangedFor(nameof(CanProceed))]
        private MapType mapType;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasMapError))]
        [NotifyPropertyChangedFor(nameof(ShowMapInfo))]
        [NotifyPropertyChangedFor(nameof(CanProceed))]
        private string mapErrorMessage = "";

        /// <summary>True once a map file path has been chosen.</summary>
        public bool HasMapFile => !string.IsNullOrEmpty(MapFileName);

        /// <summary>True when the chosen map file failed validation.</summary>
        public bool HasMapError => HasMapFile && !string.IsNullOrEmpty(MapErrorMessage);

        /// <summary>True when the chosen map file validated successfully (show the info panel).</summary>
        public bool ShowMapInfo => HasMapFile && !HasMapError;

        // ======================================================================
        // Page 3: Bitmap / PDF scale
        // ======================================================================

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MapScaleDisplay))]
        [NotifyPropertyChangedFor(nameof(CanProceed))]
        private string scaleText = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanProceed))]
        private string dpiText = "";

        /// <summary>True when the DPI row applies (Bitmap maps only; PDFs have no DPI).</summary>
        public bool ShowDpiRow => MapType == MapType.Bitmap;

        /// <summary>The parsed map scale, or 0 if not a valid number.</summary>
        public float MapScale => float.TryParse(ScaleText, out float s) ? s : 0;

        /// <summary>The parsed DPI, or 0 if not a valid number.</summary>
        public float Dpi => float.TryParse(DpiText, out float d) ? d : 0;

        // ======================================================================
        // Page 4: Print scale
        // ======================================================================

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanProceed))]
        private string printScaleText = "";

        [ObservableProperty]
        private float[] printScales = Array.Empty<float>();

        /// <summary>The map scale shown as text on the print-scale page.</summary>
        public string MapScaleDisplay => MapScale.ToString();

        /// <summary>The parsed default print scale, or 0 if not a valid number.</summary>
        public float DefaultPrintScale => float.TryParse(PrintScaleText, out float s) ? s : 0;

        // ======================================================================
        // Page 5: Paper size (bound to the PaperSizeControl, all in hundredths of an inch)
        // ======================================================================

        [ObservableProperty]
        private int paperWidth;

        [ObservableProperty]
        private int paperHeight;

        [ObservableProperty]
        private int paperMargin;

        [ObservableProperty]
        private bool paperLandscape;

        // ======================================================================
        // Page 6: Output directory
        // ======================================================================

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanProceed))]
        private bool useMapDirectory = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(UseMapDirectory))]
        [NotifyPropertyChangedFor(nameof(ShowDirectoryName))]
        [NotifyPropertyChangedFor(nameof(CanProceed))]
        private bool useOtherFolder;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowDirectoryName))]
        [NotifyPropertyChangedFor(nameof(CanProceed))]
        private string directoryName = "";

        /// <summary>True when the chosen "other folder" path should be shown.</summary>
        public bool ShowDirectoryName => UseOtherFolder && DirectoryName.Length > 0;

        // ======================================================================
        // Page 7: IOF standards
        // ======================================================================

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanProceed))]
        private bool isMap2000;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanProceed))]
        private bool isMap2017;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanProceed))]
        private bool isMapSpr2019;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanProceed))]
        private bool isDescriptions2004;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanProceed))]
        private bool isDescriptions2018;

        // ======================================================================
        // Page 8: Numbering
        // ======================================================================

        [ObservableProperty]
        private int firstCode = 31;

        [ObservableProperty]
        private bool disallowInvertibleCodes;

        // ======================================================================
        // Page 9: Final review
        // ======================================================================

        [ObservableProperty]
        private string finalEventPath = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanProceed))]
        private bool hasFinalError;

        [ObservableProperty]
        private string finalErrorMessage = "";

        // ======================================================================
        // Per-page validation
        // ======================================================================

        /// <summary>
        /// Whether the Next/Finish button is enabled for the current page. This
        /// reproduces each WinForms page's CanProceed property.
        /// </summary>
        public bool CanProceed
        {
            get {
                switch (CurrentPage) {
                    case WizardPage.Title:
                        return TitleText.Length > 0;
                    case WizardPage.MapFile:
                        return HasMapFile && !HasMapError;
                    case WizardPage.BitmapScale:
                        return (MapType == MapType.PDF || IsDpiValid(DpiText)) && IsScaleValid(ScaleText);
                    case WizardPage.PrintScale:
                        return DefaultPrintScale > 0;
                    case WizardPage.PaperSize:
                        return true;
                    case WizardPage.Directory:
                        return !UseOtherFolder || DirectoryName.Length > 0;
                    case WizardPage.Standards:
                        return (IsDescriptions2004 || IsDescriptions2018) &&
                               (IsMap2000 || IsMap2017 || IsMapSpr2019);
                    case WizardPage.Numbering:
                        return true;
                    case WizardPage.Final:
                        return !HasFinalError;
                    default:
                        return false;
                }
            }
        }

        private static bool IsScaleValid(string text)
        {
            return float.TryParse(text, out float scale) && scale >= MinScale && scale <= MaxScale;
        }

        private static bool IsDpiValid(string text)
        {
            return float.TryParse(text, out float dpi) && dpi >= MinDpi && dpi <= MaxDpi;
        }

        // ======================================================================
        // Navigation commands
        // ======================================================================

        /// <summary>
        /// Advances to the next page, or, on the final page, attempts to finish
        /// the wizard (assemble the create info and verify the file can be
        /// written). On success, raises <see cref="RequestClose"/> with true.
        /// </summary>
        [RelayCommand]
        private void Next()
        {
            int index = Array.IndexOf(PageOrder, CurrentPage);

            if (index >= PageOrder.Length - 1) {
                Finish();
                return;
            }

            int nextIndex = index + 1;
            // Skip the bitmap-scale page unless the map is a bitmap or PDF.
            if (PageOrder[nextIndex] == WizardPage.BitmapScale && !IsBitmapOrPdf)
                nextIndex += 1;

            GoToPage(PageOrder[nextIndex]);
        }

        /// <summary>Returns to the previous page (skipping the bitmap-scale page as needed).</summary>
        [RelayCommand]
        private void Back()
        {
            int index = Array.IndexOf(PageOrder, CurrentPage);
            if (index <= 0)
                return;

            int prevIndex = index - 1;
            if (PageOrder[prevIndex] == WizardPage.BitmapScale && !IsBitmapOrPdf)
                prevIndex -= 1;

            GoToPage(PageOrder[prevIndex]);
        }

        /// <summary>Cancels the wizard.</summary>
        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }

        /// <summary>
        /// Switches to the given page and runs that page's entry logic (computing
        /// defaults derived from earlier pages).
        /// </summary>
        private void GoToPage(WizardPage page)
        {
            CurrentPage = page;
            OnEnterPage(page);
        }

        /// <summary>
        /// Page-entry setup: populates values that depend on data collected on
        /// earlier pages. Mirrors the WinForms pages' Load / VisibleChanged
        /// handlers.
        /// </summary>
        private void OnEnterPage(WizardPage page)
        {
            switch (page) {
                case WizardPage.PrintScale:
                    if (DefaultPrintScale == 0 && MapScale > 0)
                        PrintScaleText = MapScale.ToString();
                    PrintScales = MapUtil.PrintScaleList(MapScale);
                    break;

                case WizardPage.PaperSize:
                    InitializePaperSize();
                    break;

                case WizardPage.Final:
                    FinalEventPath = GetEventFullPath();
                    HasFinalError = false;
                    FinalErrorMessage = "";
                    break;
            }
        }

        /// <summary>
        /// Computes the default page size and margin from the map bounds, print
        /// scale, and map type. Bitmaps and PDFs use the exact image size with no
        /// margin; other maps use a default page size. Runs only once so manual
        /// edits survive navigating back and forth.
        /// </summary>
        private void InitializePaperSize()
        {
            if (paperSizeInitialized)
                return;
            paperSizeInitialized = true;

            float printScaleRatio = MapScale > 0 ? DefaultPrintScale / MapScale : 1;
            int pageWidth, pageHeight, pageMargin;
            bool landscape;

            if (!mapBounds.IsEmpty && (MapType == MapType.PDF || MapType == MapType.Bitmap)) {
                MapUtil.GetExactPageSize(mapBounds, printScaleRatio, out pageWidth, out pageHeight, out landscape);
                pageMargin = 0;
            }
            else {
                CoreMapUtil.GetDefaultPageSize(mapBounds, printScaleRatio, out pageWidth, out pageHeight, out pageMargin, out landscape);
            }

            PaperWidth = pageWidth;
            PaperHeight = pageHeight;
            PaperMargin = pageMargin;
            PaperLandscape = landscape;
        }

        // ======================================================================
        // Map file selection
        // ======================================================================

        /// <summary>
        /// Opens the file picker and validates the chosen map file, updating
        /// MapType, scale/DPI, bounds, and the error/info display.
        /// </summary>
        [RelayCommand]
        private async Task ChooseMapFile()
        {
            FileOpenSingleViewModel fileVm = new FileOpenSingleViewModel {
                FileFilters = MiscText.ChangeMapFile_FileFilter,
            };

            bool ok = await Services.DialogService.ShowDialogAsync(fileVm);
            if (ok && fileVm.SelectedFile != null) {
                MapFileName = fileVm.SelectedFile;
                ValidateMapFile();
            }
        }

        /// <summary>
        /// Validates <see cref="MapFileName"/> via CoreMapUtil and updates all
        /// derived state. For bitmap/PDF maps the scale defaults to 15000 (the
        /// user adjusts it on the bitmap-scale page).
        /// </summary>
        private void ValidateMapFile()
        {
            if (string.IsNullOrEmpty(MapFileName)) {
                MapType = MapType.None;
                MapErrorMessage = "";
                return;
            }

            bool ok = CoreMapUtil.ValidateMapFile(MapFileName, out float scale, out float dpi,
                out Size detectedBitmapSize, out RectangleF detectedBounds, out MapType detectedType,
                out int? detectedLowerPurple, out string errorMessage);

            if (ok) {
                MapType = detectedType;
                MapErrorMessage = "";
                bitmapSize = detectedBitmapSize;
                mapBounds = detectedBounds;
                lowerPurpleMapLayer = detectedLowerPurple;
                paperSizeInitialized = false;  // recompute paper size for the new map

                if (detectedType == MapType.OCAD) {
                    ScaleText = scale.ToString();
                }
                else if (detectedType == MapType.Bitmap) {
                    DpiText = dpi.ToString();
                    ScaleText = "15000";
                }
                else if (detectedType == MapType.PDF) {
                    ScaleText = "15000";
                }
            }
            else {
                MapType = MapType.None;
                MapErrorMessage = errorMessage;
                mapBounds = RectangleF.Empty;
                lowerPurpleMapLayer = null;
            }
        }

        // ======================================================================
        // Finish
        // ======================================================================

        /// <summary>The output directory, based on the directory-page choice.</summary>
        private string GetEventDirectory()
        {
            return UseMapDirectory ? (Path.GetDirectoryName(MapFileName) ?? "") : DirectoryName;
        }

        /// <summary>The full path of the .ppen file to create.</summary>
        private string GetEventFullPath()
        {
            return Path.Combine(GetEventDirectory(), EventFileName);
        }

        /// <summary>
        /// Assembles the <see cref="CreateEventInfo"/> and verifies the event
        /// file can be created. On success, raises <see cref="RequestClose"/>
        /// with true; on failure, shows the error on the final page.
        /// </summary>
        private void Finish()
        {
            SetCreateInfo();

            if (TryCreateEvent(out string errorMessageText)) {
                RequestClose?.Invoke(true);
            }
            else {
                FinalErrorMessage = errorMessageText;
                HasFinalError = true;
            }
        }

        /// <summary>Assembles the create-event info from all the page values.</summary>
        private void SetCreateInfo()
        {
            createEventInfo.title = TitleText;
            createEventInfo.eventFileName = GetEventFullPath();
            createEventInfo.mapType = MapType;
            createEventInfo.mapFileName = MapFileName;
            createEventInfo.scale = MapScale;
            createEventInfo.allControlsPrintScale = DefaultPrintScale;
            createEventInfo.dpi = Dpi;
            createEventInfo.firstCode = FirstCode;
            createEventInfo.disallowInvertibleCodes = DisallowInvertibleCodes;
            createEventInfo.descriptionLangId = null;  // use default description language.

            if (IsMap2017)
                createEventInfo.mapStandard = "2017";
            else if (IsMapSpr2019)
                createEventInfo.mapStandard = "Spr2019";
            else
                createEventInfo.mapStandard = "2000";

            createEventInfo.descriptionStandard = IsDescriptions2018 ? "2018" : "2004";

            // Default blending for the purple color.
            if (createEventInfo.mapType == MapType.OCAD && lowerPurpleMapLayer != null) {
                createEventInfo.blend = PurpleColorBlend.UpperLowerPurple;
                createEventInfo.lowerPurpleLayer = lowerPurpleMapLayer;
            }
            else {
                createEventInfo.blend = PurpleColorBlend.Blend;
            }

            PrintArea printArea = new PrintArea {
                autoPrintArea = true,
                restrictToPageSize = true,
                pageWidth = PaperWidth,
                pageHeight = PaperHeight,
                pageMargins = PaperMargin,
                pageLandscape = PaperLandscape
            };
            createEventInfo.printArea = printArea;
        }

        /// <summary>
        /// Verifies the event file can be created: creates the directory if
        /// needed, refuses to overwrite an existing file, and test-writes a byte.
        /// Returns false with an error message if any step fails.
        /// </summary>
        private bool TryCreateEvent(out string errorMessageText)
        {
            string directory = Path.GetDirectoryName(createEventInfo.eventFileName) ?? "";
            if (!Directory.Exists(directory)) {
                try {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception e) {
                    errorMessageText = string.Format(MiscText.CannotCreateDirectory, directory) + "\r\n" + e.Message;
                    return false;
                }
            }

            if (File.Exists(createEventInfo.eventFileName)) {
                errorMessageText = string.Format(MiscText.FileAlreadyExists, Path.GetFileName(createEventInfo.eventFileName));
                return false;
            }

            byte[] bytes = { 0 };
            try {
                File.WriteAllBytes(createEventInfo.eventFileName, bytes);
            }
            catch (Exception e) {
                errorMessageText = string.Format(MiscText.CannotCreateFile, Path.GetFileName(createEventInfo.eventFileName)) + "\r\n" + e.Message;
                return false;
            }

            errorMessageText = "";
            return true;
        }

        /// <summary>The assembled create-event info, valid after a true dialog result.</summary>
        public Controller.CreateEventInfo CreateEventInfo => createEventInfo;
    }
}
