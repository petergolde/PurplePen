// MoveAllControlsDialogViewModel.cs
//
// ViewModel for the Move All Controls dialog. The user picks one of four ways
// to move every control on the map: move only, move and scale, move and rotate,
// or move, scale, and rotate. The caller reads the chosen Action back after the
// dialog returns true (Controller starts the interactive move from that value,
// so there is no underlying settings object).
//
// The four choices are bound to mutually-exclusive radio buttons in the View
// (a single GroupName keeps them exclusive); the computed Action reports which
// one is selected.

using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Move All Controls dialog. Defaults to "move only";
    /// read <see cref="Action"/> back after the dialog returns true.
    /// </summary>
    public partial class MoveAllControlsDialogViewModel : ViewModelBase
    {
        /// <summary>Move controls without scaling or rotating (the default).</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Action))]
        private bool isMove = true;

        /// <summary>Move and scale controls.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Action))]
        private bool isMoveScale;

        /// <summary>Move and rotate controls.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Action))]
        private bool isMoveRotate;

        /// <summary>Move, scale, and rotate controls.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Action))]
        private bool isMoveRotateScale;

        /// <summary>
        /// The move action selected by the user, derived from the radio buttons.
        /// </summary>
        public MoveAllControlsAction Action
        {
            get {
                if (IsMoveScale)
                    return MoveAllControlsAction.MoveScale;
                if (IsMoveRotate)
                    return MoveAllControlsAction.MoveRotate;
                if (IsMoveRotateScale)
                    return MoveAllControlsAction.MoveRotateScale;
                return MoveAllControlsAction.Move;
            }
        }
    }
}
