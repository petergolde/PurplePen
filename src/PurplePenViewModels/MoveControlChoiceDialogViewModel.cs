// MoveControlChoiceDialogViewModel.cs
//
// ViewModel for the "Moving Shared Control" dialog. Shown when the user moves a
// control that is shared by several courses, asking whether to move the control
// in all courses, create a new control in just this course, or do nothing.
//
// Like MessageBoxDialogViewModel there is no underlying settings class; the
// caller sets Code and CourseList before showing, and reads Choice afterwards.
// The dialog contains no localized strings — the View formats the explanation
// and button captions, inserting Code where needed.

using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// The action the user chose in the "Moving Shared Control" dialog.
    /// </summary>
    public enum MoveControlChoice
    {
        None,       // Dialog dismissed without an explicit choice.
        Move,       // Move the control in this and all other courses.
        Duplicate,  // Create a new control in this course only.
        DoNothing   // Cancel — do not move the control.
    }

    /// <summary>
    /// ViewModel for the "Moving Shared Control" dialog. The caller sets
    /// <see cref="Code"/> (the control's code) and <see cref="CourseList"/>
    /// (the names of the other courses containing the control) before showing,
    /// then reads <see cref="Choice"/> after the dialog closes.
    /// </summary>
    public partial class MoveControlChoiceDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// The code of the control being moved. The View inserts this into the
        /// localized explanation text and the "create new control" detail text
        /// via Binding StringFormat, keeping the format strings in the View.
        /// </summary>
        [ObservableProperty]
        private string code = "";

        /// <summary>
        /// The list of other courses that share this control, formatted for
        /// display (one course per line). Shown verbatim, so it carries no
        /// localized text of its own.
        /// </summary>
        [ObservableProperty]
        private string courseList = "";

        /// <summary>
        /// The action the user chose. Set by the View before closing; the
        /// caller reads it after the dialog closes.
        /// </summary>
        public MoveControlChoice Choice { get; set; } = MoveControlChoice.None;
    }
}
