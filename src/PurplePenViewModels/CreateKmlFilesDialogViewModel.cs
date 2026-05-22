// CreateKmlFilesDialogViewModel.cs
//
// ViewModel for the Create KML Files dialog. Follows the settings-class
// ViewModel pattern: each field of ExportKmlSettings is exposed as an
// individual observable property, and the Settings property is computed —
// its getter assembles a fresh ExportKmlSettings, and its setter
// decomposes one into the individual properties.

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Create KML Files dialog.
    /// Usage: the caller sets <see cref="EventDB"/>, then assigns
    /// <see cref="Settings"/> to seed the dialog. After the dialog
    /// returns true, read <see cref="Settings"/> to get the user's choices.
    /// </summary>
    public partial class CreateKmlFilesDialogViewModel : ViewModelBase
    {
        // ===== Inputs (set by caller before showing the dialog) =====

        /// <summary>The event database used to populate the course list.</summary>
        [ObservableProperty]
        private EventDB? eventDB;

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

        /// <summary>The selected file-creation mode (single file vs. one per course).</summary>
        [ObservableProperty]
        private ExportKmlSettings.KmlFileCreation fileCreation = ExportKmlSettings.KmlFileCreation.FilePerCourse;

        /// <summary>File name prefix (bound to the filename prefix textbox).</summary>
        [ObservableProperty]
        private string filePrefix = "";

        /// <summary>
        /// Output folder (bound to the "Other folder" textbox; only meaningful
        /// when <see cref="UseOtherDirectory"/> is true).
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

        // ===== Computed properties =====

        /// <summary>True when the "Other folder" textbox + button should be visible.</summary>
        public bool IsOtherDirectoryVisible => UseOtherDirectory;

        // ===== Settings: assembles / decomposes an ExportKmlSettings =====

        /// <summary>
        /// Bridge between the dialog's individual ViewModel properties and
        /// the <see cref="ExportKmlSettings"/> type the Controller expects.
        /// Getter assembles a fresh settings object; setter decomposes one
        /// into the individual ViewModel properties.
        /// </summary>
        public ExportKmlSettings Settings
        {
            get
            {
                Id<Course>[] courseIds = SelectedCourseDesignators
                    .Select(d => d.CourseId).ToArray();
                bool allCourses = EventDB != null
                                  && courseIds.Count(c => c != Id<Course>.None) == EventDB.AllCourseIds.Count;

                return new ExportKmlSettings {
                    CourseIds = courseIds,
                    AllCourses = allCourses,
                    FileCreation = FileCreation,
                    mapDirectory = UseMapDirectory,
                    fileDirectory = UseFileDirectory,
                    outputDirectory = OutputDirectory,
                    filePrefix = FilePrefix,
                    VariationChoicesPerCourse = VariationChoicesPerCourse,
                };
            }
            set
            {
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

                FileCreation = value.FileCreation;

                OutputDirectory = value.outputDirectory ?? "";
                FilePrefix = value.filePrefix ?? "";

                UseMapDirectory = value.mapDirectory;
                UseFileDirectory = value.fileDirectory;
                UseOtherDirectory = !value.mapDirectory && !value.fileDirectory;
            }
        }
    }
}
