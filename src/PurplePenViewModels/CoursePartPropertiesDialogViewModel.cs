// CoursePartPropertiesDialogViewModel.cs
//
// ViewModel for the "Course Part Properties" dialog, which edits the
// PartOptions of a single part of a multi-part course. The only editable
// setting today is whether the finish circle is displayed on this part.
//
// PartOptions lives in PurplePenCore (namespace PurplePen), so this VM wraps
// it with a computed property in the same spirit as the settings-class
// ViewModel pattern: the getter assembles a fresh PartOptions and the setter
// decomposes an incoming one. The caller seeds PartOptions from
// Controller.ActivePartOptions and reads it back to pass to
// Controller.ChangeActivePartOptions.
//
// Migrated from WinForms PurplePen/CoursePartProperties.cs.

using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Course Part Properties dialog. The caller sets
    /// <see cref="PartOptions"/> (the current options for the active part) and
    /// <see cref="ShowFinishCircleEnabled"/> before showing, then reads
    /// <see cref="PartOptions"/> back after the dialog closes with OK.
    /// </summary>
    public partial class CoursePartPropertiesDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// Whether the finish circle should be shown on this part of the
        /// course. Bound two-way to the "Display finish circle" check box.
        /// </summary>
        [ObservableProperty]
        private bool showFinish;

        /// <summary>
        /// Whether the "Display finish circle" check box is enabled. This is
        /// false for the last part of a course (the finish is always shown on
        /// the last part, so the option can't be changed there).
        /// </summary>
        [ObservableProperty]
        private bool showFinishCircleEnabled = true;

        /// <summary>
        /// The part options edited by the dialog. The getter builds a fresh
        /// PartOptions from the current UI state; the setter seeds the UI state
        /// from an existing PartOptions. This keeps a single source of truth
        /// for each field and mirrors the caller contract of the WinForms
        /// dialog (set before showing, read back after OK).
        /// </summary>
        public PartOptions PartOptions
        {
            get => new PartOptions() { ShowFinish = ShowFinish };
            set => ShowFinish = value.ShowFinish;
        }
    }
}
