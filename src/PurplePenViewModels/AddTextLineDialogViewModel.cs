// AddTextLineDialogViewModel.cs
//
// ViewModel for the "Add Text Line" dialog. Lets the user enter a line of text
// to place on a separate line of the control descriptions, and choose where it
// goes (above/below the item) and whether it applies to this course only or to
// all courses containing the item.
//
// There is no underlying settings class; the caller sets ObjectName,
// EnableThisCourse, TextLine and TextLineKind before showing, and reads TextLine
// and TextLineKind back after OK. No localized strings live here — the combo box
// captions (some of which embed the object name) are formatted in the View.
//
// Migrated from WinForms PurplePen/AddTextLine.cs.

using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the "Add Text Line" dialog. The caller sets
    /// <see cref="ObjectName"/> (the name of the control/leg the text attaches to)
    /// and <see cref="EnableThisCourse"/> before showing, seeds the initial value
    /// via <see cref="TextLine"/> and <see cref="TextLineKind"/>, then reads those
    /// two properties back after the dialog closes with OK.
    /// </summary>
    public partial class AddTextLineDialogViewModel : ViewModelBase
    {
        // === Inputs set by the caller ===

        /// <summary>
        /// The name of the object (control code, "start", etc.) the text line
        /// attaches to. The View inserts this into the localized combo box
        /// captions via Binding StringFormat, keeping the format strings in the
        /// View.
        /// </summary>
        [ObservableProperty]
        private string objectName = "";

        /// <summary>
        /// Whether the "this course only" option is available. When false the
        /// courses combo box is forced to "all courses" and disabled.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCoursesEnabled))]
        private bool enableThisCourse = true;

        // === UI state — bound to the dialog controls ===

        /// <summary>
        /// The text entered by the user, with real line breaks. Stored on a
        /// separate line per line break; converted to/from the "|"-separated
        /// storage form by <see cref="TextLine"/>.
        /// </summary>
        [ObservableProperty]
        private string text = "";

        /// <summary>
        /// Selected position: 0 = above the item, 1 = below the item.
        /// </summary>
        [ObservableProperty]
        private int positionIndex;

        /// <summary>
        /// Selected scope: 0 = this course only, 1 = all courses with the item.
        /// </summary>
        [ObservableProperty]
        private int coursesIndex;

        // === Computed UI state ===

        /// <summary>Whether the courses combo box is enabled.</summary>
        public bool IsCoursesEnabled => EnableThisCourse;

        // === Result accessors ===

        /// <summary>
        /// The text line in storage form: line breaks are stored as "|", and an
        /// empty entry maps to null. Round-trips the multiline <see cref="Text"/>.
        /// </summary>
        public string? TextLine
        {
            get {
                if (string.IsNullOrEmpty(Text))
                    return null;
                else
                    return Text.Replace("\r\n", "|").Replace("\n", "|");
            }
            set {
                if (value == null)
                    Text = "";
                else
                    Text = value.Replace("|", "\n");
            }
        }

        /// <summary>
        /// The kind of text line, derived from the position and courses
        /// selections. Setting it selects the matching combo box entries.
        /// </summary>
        public DescriptionLine.TextLineKind TextLineKind
        {
            get {
                if (PositionIndex == 0)
                    return (CoursesIndex == 0) ? DescriptionLine.TextLineKind.BeforeCourseControl : DescriptionLine.TextLineKind.BeforeControl;
                else
                    return (CoursesIndex == 0) ? DescriptionLine.TextLineKind.AfterCourseControl : DescriptionLine.TextLineKind.AfterControl;
            }
            set {
                if (value == DescriptionLine.TextLineKind.BeforeCourseControl || value == DescriptionLine.TextLineKind.BeforeControl)
                    PositionIndex = 0;
                else
                    PositionIndex = 1;

                if (value == DescriptionLine.TextLineKind.BeforeCourseControl || value == DescriptionLine.TextLineKind.AfterCourseControl)
                    CoursesIndex = 0;
                else
                    CoursesIndex = 1;
            }
        }
    }
}
