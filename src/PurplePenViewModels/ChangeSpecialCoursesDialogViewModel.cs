// ChangeSpecialCoursesDialogViewModel.cs
//
// ViewModel for the Change Displayed Courses dialog. Holds the EventDB
// reference, the ShowAllControls flag, and the currently selected
// CourseDesignator array. The View passes these through to its embedded
// CourseSelector control and reads the results back on OK.

using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Change Displayed Courses dialog.
    /// The caller sets <see cref="EventDB"/>, <see cref="ShowAllControls"/>,
    /// and <see cref="DisplayedCourses"/> before showing the dialog.
    /// After the dialog closes with OK, <see cref="DisplayedCourses"/>
    /// contains the user's selection.
    /// </summary>
    public partial class ChangeSpecialCoursesDialogViewModel : ViewModelBase
    {
        /// <summary>The event database used to populate the course list.</summary>
        [ObservableProperty]
        private EventDB? eventDB;

        /// <summary>Whether to show the "All controls" option in the course selector.</summary>
        [ObservableProperty]
        private bool showAllControls;

        /// <summary>
        /// The courses currently selected (set before showing; read after OK).
        /// </summary>
        [ObservableProperty]
        private CourseDesignator[] displayedCourses = [];
    }
}
