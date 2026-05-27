// ViewAdditionalCoursesDialogViewModel.cs
//
// ViewModel for the View Additional Courses dialog. Lets the user pick which
// other courses to display alongside the current course. Holds the EventDB,
// the current course name (shown in the instruction label), the current course
// id (excluded from the selectable list via CourseFilter), and the selected
// courses. The View passes these through to its embedded CourseSelector control
// and reads the result back on OK.
//
// Migrated from WinForms PurplePen/ViewAdditionalCourses.cs.

using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the View Additional Courses dialog.
    /// The caller sets <see cref="EventDB"/>, <see cref="CourseName"/>,
    /// <see cref="CurrentCourse"/>, and <see cref="DisplayedCourses"/> before
    /// showing the dialog. After the dialog closes with OK,
    /// <see cref="DisplayedCourses"/> contains the user's selection.
    /// </summary>
    public partial class ViewAdditionalCoursesDialogViewModel : ViewModelBase
    {
        /// <summary>The event database used to populate the course list.</summary>
        [ObservableProperty]
        private EventDB? eventDB;

        /// <summary>
        /// Name of the current course, shown in the instruction label.
        /// </summary>
        [ObservableProperty]
        private string courseName = "";

        /// <summary>
        /// The current course. It is excluded from the selectable list because
        /// it is always shown; see <see cref="CourseFilter"/>.
        /// </summary>
        public Id<Course> CurrentCourse { get; set; }

        /// <summary>
        /// The additional courses selected for display (set before showing;
        /// read after OK). May be null when no extra courses are selected.
        /// </summary>
        [ObservableProperty]
        private List<Id<Course>>? displayedCourses;

        /// <summary>
        /// Filter predicate for the CourseSelector that hides the current course
        /// (the one already being viewed) from the list of choices.
        /// </summary>
        public Func<CourseDesignator, bool> CourseFilter =>
            designator => designator.CourseId != CurrentCourse;
    }
}
