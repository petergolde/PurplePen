// MapIssueChoiceDialogViewModel.cs
//
// ViewModel for the "Select Map Issue Point" dialog. Shown when the user adds a
// map issue point, asking where the map is given to competitors: at the
// beginning of the marked route, partway along the marked route, or at the
// start triangle.
//
// Like MoveControlChoiceDialogViewModel there is no underlying settings class:
// the caller simply shows the dialog and reads MapIssueKind afterwards. The
// dialog carries no localized strings — all explanation and button captions
// live in the View (UIText.resx).

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the "Select Map Issue Point" dialog. The dialog has no
    /// inputs; after it closes the caller reads <see cref="MapIssueKind"/> to
    /// learn which choice the user made (left at <see cref="MapIssueKind.None"/>
    /// if the dialog was cancelled).
    /// </summary>
    public partial class MapIssueChoiceDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// The kind of map issue point the user chose. Set by the View before
        /// closing; the caller reads it after the dialog closes. Remains
        /// <see cref="MapIssueKind.None"/> if the dialog is cancelled.
        /// </summary>
        public MapIssueKind MapIssueKind { get; set; } = MapIssueKind.None;
    }
}
