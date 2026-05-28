// AddCourseDialogViewModel.cs
//
// ViewModel for the Add/Edit Course dialog. Exposes all course properties
// as bindable properties for the AXAML view. Course kind changes affect
// which controls are visible (score column vs. climb/length) and which
// label kind options are available.
//
// Validation uses INotifyDataErrorInfo via ObservableValidator (inherited
// from ViewModelBase). Scale, climb, and length fields are validated live
// as the user types, with errors displayed inline by Avalonia's binding system.
//
// Migrated from WinForms PurplePen/AddCourse.cs.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Add Course / Edit Course dialog.
    /// Provides bindable properties for course name, type, scale,
    /// climb, length, description kind, label kind, and score options.
    /// </summary>
    public partial class AddCourseDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Title text for the dialog window. Set by the caller (e.g. "Add Course",
        /// "Course Properties", "Duplicate Course"). When empty, the View falls
        /// back to its default localized title.
        /// </summary>
        [ObservableProperty]
        private string dialogTitle = "";

        /// <summary>
        /// The course name entered by the user.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
        [NotifyDataErrorInfo]
        [Required(ErrorMessageResourceName = "CourseNameRequired", ErrorMessageResourceType = typeof(MiscText))]
        private string courseName = "";

        /// <summary>
        /// Secondary title text (shown on second line of control description).
        /// In the UI this is multi-line; the caller converts newlines to "|".
        /// </summary>
        [ObservableProperty]
        private string secondaryTitle = "";

        /// <summary>
        /// Selected course kind (Normal or Score).
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsScoreCourse))]
        [NotifyPropertyChangedFor(nameof(IsNormalCourse))]
        [NotifyPropertyChangedFor(nameof(NormalCourseOpacity))]
        [NotifyPropertyChangedFor(nameof(ScoreCourseOpacity))]
        [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
        private CourseKind courseKind = CourseKind.Normal;

        /// <summary>
        /// Whether the user is allowed to change the course kind.
        /// Set to false when editing an existing course.
        /// </summary>
        [ObservableProperty]
        private bool canChangeCourseKind = true;

        /// <summary>
        /// The print scale text entered or selected by the user (e.g. "10000").
        /// This is a string because the scale combo is editable.
        /// Validation is done in the code-behind OkButton_Click because
        /// editable ComboBox doesn't support inline validation display.
        /// </summary>
        [ObservableProperty]
        private string printScaleText = "";

        /// <summary>
        /// Available print scales for the combo box dropdown, computed
        /// from the map scale by the caller via <see cref="InitializePrintScales"/>.
        /// </summary>
        public ObservableCollection<string> AvailablePrintScales { get; } = new ObservableCollection<string>();

        /// <summary>
        /// The climb text entered by the user (in meters). Empty means no climb.
        /// Validated to be empty or a number between 0 and 9999.
        /// </summary>
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [CustomValidation(typeof(AddCourseDialogViewModel), nameof(ValidateClimb))]
        private string climbText = "";

        /// <summary>
        /// The length text entered by the user (in km). Empty or "Automatic" means auto-length.
        /// Validated to be empty, "Automatic", or a number between 0 and 100.
        /// </summary>
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [CustomValidation(typeof(AddCourseDialogViewModel), nameof(ValidateLength))]
        private string lengthText = "";

        /// <summary>
        /// Selected description appearance (Symbols, Text, or SymbolsAndText).
        /// </summary>
        [ObservableProperty]
        private DescriptionKind descKind = DescriptionKind.Symbols;

        /// <summary>
        /// Selected control circle label style.
        /// </summary>
        [ObservableProperty]
        private ControlLabelKind controlLabelKind = ControlLabelKind.Sequence;

        /// <summary>
        /// The first control ordinal number (1–99).
        /// </summary>
        [ObservableProperty]
        private int firstControlOrdinal = 1;

        /// <summary>
        /// Score column index for score courses:
        /// 0 = Column A, 1 = Column B, 2 = Column H, 3 = (do not display).
        /// </summary>
        [ObservableProperty]
        private int scoreColumnIndex = 2;

        /// <summary>
        /// Whether the course is hidden from reports.
        /// </summary>
        [ObservableProperty]
        private bool hideFromReports;

        /// <summary>
        /// Computed: true when this is a score course.
        /// Controls visibility of score-specific fields.
        /// </summary>
        public bool IsScoreCourse => CourseKind == CourseKind.Score;

        /// <summary>
        /// Computed: true when this is a normal course.
        /// Controls visibility of climb/length fields.
        /// </summary>
        public bool IsNormalCourse => CourseKind != CourseKind.Score;

        /// <summary>
        /// Computed: opacity for normal-course-only fields.
        /// Returns 1.0 for normal courses, 0.0 for score courses.
        /// Used instead of IsVisible to keep grid rows at stable height.
        /// </summary>
        public double NormalCourseOpacity => IsNormalCourse ? 1.0 : 0.0;

        /// <summary>
        /// Computed: opacity for score-course-only fields.
        /// Returns 1.0 for score courses, 0.0 for normal courses.
        /// Used instead of IsVisible to keep grid rows at stable height.
        /// </summary>
        public double ScoreCourseOpacity => IsScoreCourse ? 1.0 : 0.0;

        /// <summary>
        /// Computed: OK button is enabled only when a course name has been entered
        /// and there are no validation errors.
        /// </summary>
        public bool IsOkEnabled => !string.IsNullOrEmpty(CourseName) && !HasErrors;

        /// <summary>
        /// Parameterless constructor for the Avalonia designer.
        /// Populates fields with sample data.
        /// </summary>
        public AddCourseDialogViewModel()
        {
            AvailablePrintScales.Add("4000");
            AvailablePrintScales.Add("5000");
            AvailablePrintScales.Add("7500");
            AvailablePrintScales.Add("10000");
            AvailablePrintScales.Add("15000");
            PrintScaleText = "10000";

            // When validation errors change, update IsOkEnabled so the
            // OK button enables/disables based on validation state.
            ErrorsChanged += (s, e) => OnPropertyChanged(nameof(IsOkEnabled));
        }

        /// <summary>
        /// Populates the available print scales based on the map scale.
        /// Called by the dialog opener after creating the ViewModel.
        /// </summary>
        /// <param name="mapScale">The map's native scale (e.g. 15000).</param>
        public void InitializePrintScales(float mapScale)
        {
            AvailablePrintScales.Clear();
            foreach (float scale in MapUtil.PrintScaleList(mapScale)) {
                AvailablePrintScales.Add(scale.ToString());
            }
        }

        /// <summary>
        /// Validates that climb is empty or a number between 0 and 9999.
        /// </summary>
        public static ValidationResult? ValidateClimb(string value, ValidationContext context)
        {
            if (string.IsNullOrEmpty(value))
                return ValidationResult.Success;
            if (!float.TryParse(value, out float climb) || climb < 0 || climb > 9999)
                return new ValidationResult(MiscText.BadClimb);
            return ValidationResult.Success;
        }

        /// <summary>
        /// Validates that length is empty, "Automatic", or a number between 0 and 100 km.
        /// </summary>
        public static ValidationResult? ValidateLength(string value, ValidationContext context)
        {
            if (string.IsNullOrEmpty(value))
                return ValidationResult.Success;
            if (string.Compare(value, MiscText.AutomaticLength, StringComparison.CurrentCultureIgnoreCase) == 0)
                return ValidationResult.Success;
            if (!float.TryParse(value, out float length) || length <= 0 || length >= 100)
                return new ValidationResult(MiscText.BadLength);
            return ValidationResult.Success;
        }

        /// <summary>
        /// Called when CourseKind changes. Updates computed visibility properties,
        /// clamps ControlLabelKind to valid range, and re-validates climb/length
        /// since they are only relevant for normal courses.
        /// </summary>
        partial void OnCourseKindChanged(CourseKind value)
        {
            // Normal courses only support the first 3 label kinds.
            if (value == CourseKind.Normal &&
                (ControlLabelKind == ControlLabelKind.SequenceAndScore ||
                 ControlLabelKind == ControlLabelKind.CodeAndScore ||
                 ControlLabelKind == ControlLabelKind.Score)) {
                ControlLabelKind = ControlLabelKind.Sequence;
            }
        }




        /// <summary>
        /// Gets the ScoreColumn value as used by the data model.
        /// 0 = Column A, 1 = Column B, 7 = Column H, -1 = None.
        /// </summary>
        public int ScoreColumn
        {
            get {
                if (CourseKind != CourseKind.Score)
                    return -1;
                switch (ScoreColumnIndex) {
                    case 0: return 0;  // Column A
                    case 1: return 1;  // Column B
                    case 2: return 7;  // Column H
                    default: return -1; // None
                }
            }
            set {
                switch (value) {
                    case 0: ScoreColumnIndex = 0; break;
                    case 1: ScoreColumnIndex = 1; break;
                    case 7: ScoreColumnIndex = 2; break;
                    default: ScoreColumnIndex = 3; break;
                }
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

        /// <summary>
        /// Gets or sets the climb in meters. Negative means no climb specified.
        /// </summary>
        public float Climb
        {
            get {
                if (string.IsNullOrEmpty(ClimbText))
                    return -1;
                if (float.TryParse(ClimbText, out float climb))
                    return climb;
                return -1;
            }
            set {
                ClimbText = value < 0 ? "" : value.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the course length in meters. Null means automatic.
        /// </summary>
        public float? Length
        {
            get {
                if (string.IsNullOrEmpty(LengthText) ||
                    string.Compare(LengthText, MiscText.AutomaticLength, StringComparison.CurrentCultureIgnoreCase) == 0)
                    return null;
                if (float.TryParse(LengthText, out float km))
                    return km * 1000;  // Convert km to meters
                return null;
            }
            set {
                if (value.HasValue)
                    LengthText = string.Format("{0:0.0##}", value.Value / 1000F);
                else
                    LengthText = MiscText.AutomaticLength;
            }
        }

        /// <summary>
        /// Gets or sets the secondary title in pipe-delimited format (for the data model).
        /// Converts between "|" (model) and newlines (UI).
        /// </summary>
        public string? SecondaryTitlePipeDelimited
        {
            get {
                if (string.IsNullOrEmpty(SecondaryTitle))
                    return null;
                return SecondaryTitle.Replace("\r\n", "|").Replace("\n", "|");
            }
            set {
                SecondaryTitle = value == null ? "" : value.Replace("|", "\r\n");
            }
        }
    }
}
