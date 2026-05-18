// CreatePdfCoursesDialogViewModel.cs
//
// ViewModel for the Create PDF Files dialog. Follows the same Settings-class
// ViewModel pattern as CreateOcadFilesDialogViewModel and
// CreateImageFilesDialogViewModel (see AGENTS.md): each dialog field is an
// individual ObservableProperty, and CoursePdfSettings is a computed property
// whose getter assembles a fresh settings object and whose setter decomposes
// an incoming one.

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Create PDF Files dialog.
    /// Usage: the caller sets <see cref="EventDB"/>, <see cref="ShowMergeParts"/>,
    /// and <see cref="EnableChangeCropping"/>, then assigns <see cref="Settings"/>
    /// to seed the dialog. After OK, read <see cref="Settings"/> for the user's
    /// choices. The window title is fixed ("Create PDF Files") and set directly
    /// in the dialog AXAML.
    /// </summary>
    public partial class CreatePdfCoursesDialogViewModel : ViewModelBase
    {
        // ===== Inputs (set by caller before showing) =====

        /// <summary>The event database used to populate the course list.</summary>
        [ObservableProperty]
        private EventDB? eventDB;

        /// <summary>
        /// Whether the "Print Map Exchanges on Same Map" checkbox is visible.
        /// Only meaningful when at least one course has multiple parts.
        /// </summary>
        [ObservableProperty]
        private bool showMergeParts;

        /// <summary>
        /// Whether the user can change the "Crop / Print on multiple pages" combo.
        /// Disabled when the underlying map is a PDF (forced to crop).
        /// </summary>
        [ObservableProperty]
        private bool enableChangeCropping = true;

        // ===== UI state — bound directly to dialog controls =====

        /// <summary>Course designators selected by the user (set by code-behind on Open/OK).</summary>
        [ObservableProperty]
        private CourseDesignator[] selectedCourseDesignators = Array.Empty<CourseDesignator>();

        /// <summary>Per-course variation choices (set by code-behind on Open/OK).</summary>
        [ObservableProperty]
        private Dictionary<Id<Course>, VariationChoices> variationChoicesPerCourse =
            new Dictionary<Id<Course>, VariationChoices>();

        /// <summary>
        /// 0 = Crop to a single page, 1 = Print on multiple pages.
        /// Bound to the multi-page combo's SelectedIndex.
        /// </summary>
        [ObservableProperty]
        private int multiPageIndex; // 0 = crop, 1 = multi-page

        /// <summary>
        /// 0 = Course and map, 1 = Course only.
        /// Bound to the print-base-map combo's SelectedIndex.
        /// </summary>
        [ObservableProperty]
        private int printBaseMapIndex;

        /// <summary>
        /// 0 = RGB, 1 = CMYK. Bound to the color-model combo's SelectedIndex.
        /// Maps to <see cref="PurplePen.ColorModel"/> via +1 (the underlying
        /// enum starts at OCADCompatible=0, RGB=1, CMYK=2).
        /// </summary>
        [ObservableProperty]
        private int colorModelIndex;

        /// <summary>"Print Map Exchanges on Same Map" checkbox.</summary>
        [ObservableProperty]
        private bool mergeParts;

        /// <summary>
        /// 0 = One for all courses, 1 = One per course, 2 = One per course part/variation.
        /// Bound to the file-format combo's SelectedIndex.
        /// </summary>
        [ObservableProperty]
        private int fileFormatIndex; // matches CoursePdfSettings.PdfFileCreation

        /// <summary>File name prefix.</summary>
        [ObservableProperty]
        private string filePrefix = "";

        /// <summary>Output folder (only meaningful when <see cref="UseOtherDirectory"/> is true).</summary>
        [ObservableProperty]
        private string outputDirectory = "";

        /// <summary>True when the "Same folder as Purple Pen file" radio is selected.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOtherDirectoryVisible))]
        private bool useFileDirectory;

        /// <summary>True when the "Same folder as map file" radio is selected.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOtherDirectoryVisible))]
        private bool useMapDirectory;

        /// <summary>True when the "Other folder" radio is selected.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOtherDirectoryVisible))]
        private bool useOtherDirectory;

        // ===== Pass-through (set by caller, not UI-bound, preserved through dialog) =====

        /// <summary>Whether to render control descriptions on the page (not exposed in the UI).</summary>
        public bool RenderControlDescriptions { get; set; } = true;

        /// <summary>Whether to show a progress dialog while creating the PDFs (not exposed in the UI).</summary>
        public bool ShowProgressDialog { get; set; } = true;

        // ===== Computed properties =====

        /// <summary>True when the "Other folder" textbox + button should be visible.</summary>
        public bool IsOtherDirectoryVisible => UseOtherDirectory;

        // ===== Settings: assembles / decomposes a CoursePdfSettings =====

        /// <summary>
        /// Bridge between the dialog's individual ViewModel properties and the
        /// <see cref="CoursePdfSettings"/> type the Controller expects. Getter
        /// assembles a fresh settings object; setter decomposes one into the
        /// individual ViewModel properties.
        /// </summary>
        public CoursePdfSettings Settings
        {
            get
            {
                Id<Course>[] courseIds = SelectedCourseDesignators
                    .Select(d => d.CourseId).ToArray();
                bool allCourses = EventDB != null
                                  && courseIds.Count(c => c != Id<Course>.None) == EventDB.AllCourseIds.Count;

                return new CoursePdfSettings {
                    CourseIds = courseIds,
                    AllCourses = allCourses,
                    DontPrintBaseMap = PrintBaseMapIndex == 1,
                    // WinForms uses index 0 = "Crop to a single page" -> CropLargePrintArea = true.
                    CropLargePrintArea = MultiPageIndex == 0,
                    PrintMapExchangesOnOneMap = MergeParts,
                    // The combo lists RGB then CMYK; the underlying enum is
                    // OCADCompatible=0, RGB=1, CMYK=2. So index + 1.
                    ColorModel = (ColorModel)(ColorModelIndex + 1),
                    FileCreation = (CoursePdfSettings.PdfFileCreation)FileFormatIndex,
                    RenderControlDescriptions = RenderControlDescriptions,
                    ShowProgressDialog = ShowProgressDialog,
                    mapDirectory = UseMapDirectory,
                    fileDirectory = UseFileDirectory,
                    outputDirectory = OutputDirectory,
                    filePrefix = FilePrefix,
                    VariationChoicesPerCourse = VariationChoicesPerCourse,
                };
            }
            set
            {
                // Build the initial CourseDesignators selection: if AllCourses
                // is true, populate from EventDB.AllCourseIds (plus AllControls
                // if CourseIds included it); otherwise use CourseIds directly.
                List<CourseDesignator> designators = new List<CourseDesignator>();
                if (value.AllCourses && EventDB != null) {
                    designators.AddRange(EventDB.AllCourseIds.Select(id => new CourseDesignator(id)));
                    if (value.CourseIds != null && Array.IndexOf(value.CourseIds, Id<Course>.None) >= 0)
                        designators.Add(new CourseDesignator(Id<Course>.None));
                }
                else if (value.CourseIds != null) {
                    designators.AddRange(value.CourseIds.Select(id => new CourseDesignator(id)));
                }
                SelectedCourseDesignators = designators.ToArray();

                VariationChoicesPerCourse = value.VariationChoicesPerCourse
                                            ?? new Dictionary<Id<Course>, VariationChoices>();

                PrintBaseMapIndex = value.DontPrintBaseMap ? 1 : 0;
                MultiPageIndex = value.CropLargePrintArea ? 0 : 1;
                MergeParts = value.PrintMapExchangesOnOneMap;
                // Reverse of the +1 in the getter; clamp to [0, 1] for safety
                // since OCADCompatible (enum value 0) isn't selectable in the combo.
                int colorIndex = (int)value.ColorModel - 1;
                if (colorIndex < 0) colorIndex = 0;
                if (colorIndex > 1) colorIndex = 1;
                ColorModelIndex = colorIndex;

                FileFormatIndex = (int)value.FileCreation;

                RenderControlDescriptions = value.RenderControlDescriptions;
                ShowProgressDialog = value.ShowProgressDialog;

                OutputDirectory = value.outputDirectory ?? "";
                FilePrefix = value.filePrefix ?? "";

                UseMapDirectory = value.mapDirectory;
                UseFileDirectory = value.fileDirectory;
                UseOtherDirectory = !value.mapDirectory && !value.fileDirectory;
            }
        }
    }
}
