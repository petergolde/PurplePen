// CreateOcadFilesDialogViewModel.cs
//
// ViewModel for the Create OCAD Files dialog. Rather than holding an
// OcadCreationSettings instance and shuffling its fields back and forth with
// the View, every setting is exposed as an individual observable property
// that the dialog binds to directly. The Settings property is computed —
// its getter assembles a fresh OcadCreationSettings from the individual
// properties, and its setter decomposes an incoming OcadCreationSettings
// into them. This keeps the dialog code-behind to a minimum (just the
// folder picker and a tiny bit of selection plumbing the CourseSelector
// doesn't expose for binding).

using CommunityToolkit.Mvvm.ComponentModel;
using PurplePen.MapModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Create OCAD Files dialog.
    /// Usage: the caller sets <see cref="EventDB"/>, <see cref="RestrictToFormat"/>,
    /// and <see cref="DialogTitle"/>, then assigns <see cref="Settings"/> to seed
    /// the dialog. After the dialog returns true, read <see cref="Settings"/>
    /// to get the user's choices.
    /// </summary>
    public partial class CreateOcadFilesDialogViewModel : ViewModelBase
    {
        // Master list of file format options shown in the combo, in the same
        // order they appeared in the WinForms dialog. The visible subset is
        // filtered by RestrictToFormat (see AvailableFileFormats).
        //
        // Instance-level (not static) because the display strings come from
        // MiscText, which changes when the user switches UI language. A static
        // initializer would capture whatever language was current at first
        // reference and stay frozen. Building this per-VM-instance picks up
        // the current language each time the dialog is opened; the language
        // can't change while the dialog is open.
        private readonly FileFormatOption[] allFileFormats = {
            new FileFormatOption(MiscText.OCAD + " 6",  new MapFileFormat(MapFileFormatKind.OCAD, 6)),
            new FileFormatOption(MiscText.OCAD + " 7",  new MapFileFormat(MapFileFormatKind.OCAD, 7)),
            new FileFormatOption(MiscText.OCAD + " 8",  new MapFileFormat(MapFileFormatKind.OCAD, 8)),
            new FileFormatOption(MiscText.OCAD + " 9",  new MapFileFormat(MapFileFormatKind.OCAD, 9)),
            new FileFormatOption(MiscText.OCAD + " 10", new MapFileFormat(MapFileFormatKind.OCAD, 10)),
            new FileFormatOption(MiscText.OCAD + " 11", new MapFileFormat(MapFileFormatKind.OCAD, 11)),
            new FileFormatOption(MiscText.OCAD + " 12", new MapFileFormat(MapFileFormatKind.OCAD, 12)),
            new FileFormatOption(MiscText.OCAD + " 2018", new MapFileFormat(MapFileFormatKind.OCAD, 2018)),
            new FileFormatOption(MiscText.OpenOrienteeringMapper + " 0.7 (.omap)", new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.OMap, 6)),
            new FileFormatOption(MiscText.OpenOrienteeringMapper + " 0.7 (.xmap)", new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.XMap, 6)),
            new FileFormatOption(MiscText.OpenOrienteeringMapper + " 0.8 (.omap)", new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.OMap, 7)),
            new FileFormatOption(MiscText.OpenOrienteeringMapper + " 0.8 (.xmap)", new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.XMap, 7)),
            new FileFormatOption(MiscText.OpenOrienteeringMapper + " 0.9 (.omap)", new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.OMap, 9)),
            new FileFormatOption(MiscText.OpenOrienteeringMapper + " 0.9 (.xmap)", new MapFileFormat(MapFileFormatKind.OpenMapper, OpenMapperSubKind.XMap, 9)),
        };

        // ===== Inputs (set by caller before showing the dialog) =====

        /// <summary>The event database used to populate the course list.</summary>
        [ObservableProperty]
        private EventDB? eventDB;

        /// <summary>
        /// Restrict the file-format combo to one map kind (e.g. OCAD only,
        /// when the current map is OCAD). <see cref="MapFileFormatKind.None"/>
        /// allows all formats.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AvailableFileFormats))]
        private MapFileFormatKind restrictToFormat = MapFileFormatKind.None;

        /// <summary>The window title (computed by the caller, e.g. "Create OCAD Files").</summary>
        [ObservableProperty]
        private string dialogTitle = "";

        // ===== UI state — bound directly to dialog controls =====

        /// <summary>
        /// Course designators selected by the user. Read by the dialog
        /// code-behind from the CourseSelector on OK; not currently
        /// bound (the CourseSelector's selection state isn't bindable).
        /// </summary>
        [ObservableProperty]
        private CourseDesignator[] selectedCourseDesignators = Array.Empty<CourseDesignator>();

        /// <summary>Per-course variation choices. Read from the CourseSelector on OK.</summary>
        [ObservableProperty]
        private Dictionary<Id<Course>, VariationChoices> variationChoicesPerCourse =
            new Dictionary<Id<Course>, VariationChoices>();

        /// <summary>The selected file-format combo item.</summary>
        [ObservableProperty]
        private FileFormatOption? selectedFileFormat;

        /// <summary>File name prefix (bound to the filename prefix textbox).</summary>
        [ObservableProperty]
        private string filePrefix = "";

        /// <summary>
        /// Output folder (bound to the "Other folder" textbox; only meaningful
        /// when <see cref="UseOtherDirectory"/> is true, but the value is also
        /// what gets written to OcadCreationSettings.outputDirectory).
        /// </summary>
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

        // ===== Pass-through (set by the caller via FindPurple.GetPurpleColor,
        // preserved unchanged through the dialog, written back into Settings) =====

        public short ColorOcadId { get; set; }
        public float Cyan { get; set; }
        public float Magenta { get; set; }
        public float Yellow { get; set; }
        public float Black { get; set; }
        public bool PurpleOverprint { get; set; }

        // ===== Computed properties =====

        /// <summary>True when the "Other folder" textbox + button should be visible.</summary>
        public bool IsOtherDirectoryVisible => UseOtherDirectory;

        /// <summary>
        /// The file formats shown in the combo, filtered by
        /// <see cref="RestrictToFormat"/>.
        /// </summary>
        public IReadOnlyList<FileFormatOption> AvailableFileFormats =>
            RestrictToFormat == MapFileFormatKind.None
                ? allFileFormats
                : allFileFormats.Where(f => f.Format.kind == RestrictToFormat).ToArray();

        // ===== Settings: assembles / decomposes an OcadCreationSettings =====

        /// <summary>
        /// Bridge between the dialog's individual ViewModel properties and
        /// the <see cref="OcadCreationSettings"/> type the Controller expects.
        /// Getter assembles a fresh settings object; setter decomposes one
        /// into the individual ViewModel properties (expanding AllCourses
        /// to a list of CourseDesignators using <see cref="EventDB"/>).
        /// </summary>
        public OcadCreationSettings Settings
        {
            get
            {
                Id<Course>[] courseIds = SelectedCourseDesignators
                    .Select(d => d.CourseId).ToArray();
                bool allCourses = EventDB != null
                                  && courseIds.Count(c => c != Id<Course>.None) == EventDB.AllCourseIds.Count;

                return new OcadCreationSettings {
                    CourseIds = courseIds,
                    AllCourses = allCourses,
                    fileFormat = SelectedFileFormat?.Format ?? default,
                    mapDirectory = UseMapDirectory,
                    fileDirectory = UseFileDirectory,
                    outputDirectory = OutputDirectory,
                    filePrefix = FilePrefix,
                    colorOcadId = ColorOcadId,
                    cyan = Cyan,
                    magenta = Magenta,
                    yellow = Yellow,
                    black = Black,
                    purpleOverprint = PurpleOverprint,
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

                // Pick the matching FileFormatOption from the visible list so
                // the ComboBox can show it. Falls back to the first available
                // format when the incoming format isn't compatible with the
                // current RestrictToFormat.
                SelectedFileFormat = AvailableFileFormats.FirstOrDefault(f => f.Format.Equals(value.fileFormat))
                                     ?? AvailableFileFormats.FirstOrDefault();

                OutputDirectory = value.outputDirectory ?? "";
                FilePrefix = value.filePrefix ?? "";

                // Folder radios — exactly one of the three should be true.
                UseMapDirectory = value.mapDirectory;
                UseFileDirectory = value.fileDirectory;
                UseOtherDirectory = !value.mapDirectory && !value.fileDirectory;

                ColorOcadId = value.colorOcadId;
                Cyan = value.cyan;
                Magenta = value.magenta;
                Yellow = value.yellow;
                Black = value.black;
                PurpleOverprint = value.purpleOverprint;
            }
        }
    }

    /// <summary>
    /// One entry in the file-format combo: a localized display string paired
    /// with the underlying <see cref="MapFileFormat"/>. Equality is by Format
    /// so ComboBox.SelectedItem round-trips correctly.
    /// </summary>
    public class FileFormatOption
    {
        public string DisplayName { get; }
        public MapFileFormat Format { get; }

        public FileFormatOption(string displayName, MapFileFormat format)
        {
            DisplayName = displayName;
            Format = format;
        }

        // ToString is what ComboBox shows when no ItemTemplate is provided.
        public override string ToString() => DisplayName;

        public override bool Equals(object? obj) =>
            obj is FileFormatOption other && Format.Equals(other.Format);
        public override int GetHashCode() => Format.GetHashCode();
    }
}
