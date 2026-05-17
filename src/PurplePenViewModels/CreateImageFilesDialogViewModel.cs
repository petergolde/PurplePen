// CreateImageFilesDialogViewModel.cs
//
// ViewModel for the Create Image Files dialog. Follows the same Settings-class
// ViewModel pattern as CreateOcadFilesDialogViewModel (see AGENTS.md): every
// dialog field is an individual ObservableProperty, and BitmapCreationSettings
// is a computed property whose getter assembles a fresh settings object and
// whose setter decomposes an incoming one.

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Create Image Files dialog.
    /// Usage: the caller sets <see cref="EventDB"/> and <see cref="WorldFileEnabled"/>,
    /// then assigns <see cref="Settings"/> to seed the dialog. After OK, read
    /// <see cref="Settings"/> for the user's choices. The window title is fixed
    /// ("Create Image Files") and set directly in the dialog AXAML.
    /// </summary>
    public partial class CreateImageFilesDialogViewModel : ViewModelBase
    {
        // ===== Inputs (set by caller before showing) =====

        /// <summary>The event database used to populate the course list.</summary>
        [ObservableProperty]
        private EventDB? eventDB;

        /// <summary>
        /// Whether the World File combo is enabled. Disabled when the current
        /// map has no real-world coordinates.
        /// </summary>
        [ObservableProperty]
        private bool worldFileEnabled = true;

        // ===== UI state — bound directly to dialog controls =====

        /// <summary>Course designators selected by the user (set by code-behind on Open/OK).</summary>
        [ObservableProperty]
        private CourseDesignator[] selectedCourseDesignators = Array.Empty<CourseDesignator>();

        /// <summary>Per-course variation choices (set by code-behind on Open/OK).</summary>
        [ObservableProperty]
        private Dictionary<Id<Course>, VariationChoices> variationChoicesPerCourse =
            new Dictionary<Id<Course>, VariationChoices>();

        /// <summary>The selected file-format combo item.</summary>
        [ObservableProperty]
        private BitmapKindOption? selectedFileFormat;

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

        /// <summary>DPI as a string (the WinForms combo was editable, so users could type custom values).</summary>
        [ObservableProperty]
        private string dpiText = "200";

        /// <summary>Selected color model — true for CMYK, false for RGB. Bound to the combo's SelectedIndex.</summary>
        [ObservableProperty]
        private int colorModelIndex; // 0 = RGB, 1 = CMYK

        /// <summary>Whether to write a world file alongside each image. Combo: 0 = No, 1 = Yes.</summary>
        [ObservableProperty]
        private int worldFileIndex;

        /// <summary>Whether to render the base map. Combo: 0 = Course and map, 1 = Course only.</summary>
        [ObservableProperty]
        private int printBaseMapIndex;

        // ===== Computed properties =====

        /// <summary>True when the "Other folder" textbox + button should be visible.</summary>
        public bool IsOtherDirectoryVisible => UseOtherDirectory;

        /// <summary>
        /// The bitmap file formats shown in the combo. Built per instance
        /// (not static) so the localized display strings reflect the current
        /// UI language.
        /// </summary>
        public IReadOnlyList<BitmapKindOption> AvailableFileFormats { get; }

        /// <summary>Standard DPI options shown in the combo dropdown.</summary>
        public IReadOnlyList<string> DpiOptions { get; } =
            new[] { "100", "150", "200", "300", "400", "500", "600" };

        public CreateImageFilesDialogViewModel()
        {
            AvailableFileFormats = new[] {
                new BitmapKindOption("PNG",  BitmapCreationSettings.BitmapKind.Png),
                new BitmapKindOption("JPEG", BitmapCreationSettings.BitmapKind.Jpeg),
                new BitmapKindOption("GIF",  BitmapCreationSettings.BitmapKind.Gif),
            };
            SelectedFileFormat = AvailableFileFormats[0];
        }

        // ===== Settings: assembles / decomposes a BitmapCreationSettings =====

        /// <summary>
        /// Bridge between the dialog's individual ViewModel properties and the
        /// <see cref="BitmapCreationSettings"/> type the Controller expects.
        /// Getter assembles a fresh settings object; setter decomposes one
        /// into the individual ViewModel properties.
        /// </summary>
        public BitmapCreationSettings Settings
        {
            get
            {
                // Parse the DPI string; fall back to 200 if it doesn't parse.
                if (!float.TryParse(DpiText, NumberStyles.Float, CultureInfo.CurrentCulture, out float dpi))
                    dpi = 200;

                Id<Course>[] courseIds = SelectedCourseDesignators
                    .Select(d => d.CourseId).ToArray();
                bool allCourses = EventDB != null
                                  && courseIds.Count(c => c != Id<Course>.None) == EventDB.AllCourseIds.Count;

                return new BitmapCreationSettings {
                    CourseIds = courseIds,
                    AllCourses = allCourses,
                    ExportedBitmapKind = SelectedFileFormat?.Kind ?? BitmapCreationSettings.BitmapKind.Png,
                    Dpi = dpi,
                    ColorModel = ColorModelIndex == 1 ? ColorModel.CMYK : ColorModel.RGB,
                    WorldFile = WorldFileIndex == 1,
                    DontPrintBaseMap = PrintBaseMapIndex == 1,
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

                SelectedFileFormat = AvailableFileFormats.FirstOrDefault(f => f.Kind == value.ExportedBitmapKind)
                                     ?? AvailableFileFormats[0];

                DpiText = value.Dpi.ToString(CultureInfo.CurrentCulture);
                ColorModelIndex = value.ColorModel == ColorModel.CMYK ? 1 : 0;
                WorldFileIndex = value.WorldFile ? 1 : 0;
                PrintBaseMapIndex = value.DontPrintBaseMap ? 1 : 0;

                OutputDirectory = value.outputDirectory ?? "";
                FilePrefix = value.filePrefix ?? "";

                UseMapDirectory = value.mapDirectory;
                UseFileDirectory = value.fileDirectory;
                UseOtherDirectory = !value.mapDirectory && !value.fileDirectory;
            }
        }
    }

    /// <summary>
    /// One entry in the file-format combo: display name + underlying BitmapKind.
    /// Equality is by Kind so ComboBox.SelectedItem round-trips correctly.
    /// </summary>
    public class BitmapKindOption
    {
        public string DisplayName { get; }
        public BitmapCreationSettings.BitmapKind Kind { get; }

        public BitmapKindOption(string displayName, BitmapCreationSettings.BitmapKind kind)
        {
            DisplayName = displayName;
            Kind = kind;
        }

        public override string ToString() => DisplayName;

        public override bool Equals(object? obj)
        {
            return (obj is BitmapKindOption other) && (Kind == other.Kind);
        }

        public override int GetHashCode() => (int)Kind;
    }
}
