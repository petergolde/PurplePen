// SelectLocationsForMoveDialogViewModel.cs
//
// ViewModel for the Select Locations For Move dialog. This is the second,
// interactive phase of the "Move All Controls" command: after the user picks
// how to move (move / scale / rotate) in MoveAllControlsDialog, this dialog
// guides them through clicking control points and new locations directly on
// the map. The dialog stays open (non-modal, owner stays interactive) while
// the user works, and drives the map's command modes through the Controller.
//
// The staging logic lives here. Each DialogStage corresponds to one Controller
// command mode (select a control, drag it to a new location, confirm). The
// Controller invokes the ControlSelected / LocationSelected callbacks as the
// user clicks; advancing the Stage re-enters the appropriate command mode.
//
// All localized strings live in the View (UIText.resx); this ViewModel exposes
// only booleans (for arrow visibility and active-step highlighting) and the
// preformatted numeric offset/scale/rotation strings.
//
// Migrated from WinForms PurplePen/SelectLocationsForMove.cs.

using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel driving the interactive control/location selection for the
    /// Move All Controls command. The caller sets <see cref="Controller"/> and
    /// <see cref="Action"/>, shows the dialog as an owned (non-modal) window,
    /// then calls <see cref="Start"/> once it is visible. When the dialog
    /// closes, the View calls <see cref="Confirm"/> (OK) or <see cref="HandleClosing"/>
    /// (Cancel / window close) so the Controller can finish or undo the move.
    /// </summary>
    public partial class SelectLocationsForMoveDialogViewModel : ViewModelBase
    {
        /// <summary>The steps the user walks through while picking locations.</summary>
        public enum DialogStage
        {
            Begin, SelectFirstControl, MoveFirstControl, SelectSecondControl, MoveSecondControl, Confirm
        }

        private Controller? controller;
        private MoveAllControlsAction action = MoveAllControlsAction.Move;
        private Id<ControlPoint> firstControlPoint, secondControlPoint;
        private Id<Special> firstSpecial, secondSpecial;
        private readonly PointF[] locations = new PointF[4];
        private bool mapUpdated = false;
        private bool finished = false;

        /// <summary>
        /// The current step. Changing it re-enters the matching Controller
        /// command mode and refreshes the step highlighting / offset display.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSelectFirstControlStage))]
        [NotifyPropertyChangedFor(nameof(IsMoveFirstControlStage))]
        [NotifyPropertyChangedFor(nameof(IsSelectSecondControlStage))]
        [NotifyPropertyChangedFor(nameof(IsMoveSecondControlStage))]
        [NotifyPropertyChangedFor(nameof(IsConfirmStage))]
        [NotifyPropertyChangedFor(nameof(ShowOffsets))]
        [NotifyPropertyChangedFor(nameof(ShowScale))]
        [NotifyPropertyChangedFor(nameof(ShowRotation))]
        [NotifyPropertyChangedFor(nameof(ShowConfirmButton))]
        [NotifyPropertyChangedFor(nameof(ShowBackButton))]
        [NotifyPropertyChangedFor(nameof(ConfirmIsDefault))]
        [NotifyPropertyChangedFor(nameof(BackIsDefault))]
        [NotifyCanExecuteChangedFor(nameof(BackCommand))]
        private DialogStage stage = DialogStage.Begin;

        /// <summary>The Controller that owns the active event; set by the caller before showing.</summary>
        public Controller? Controller
        {
            get => controller;
            set => controller = value;
        }

        /// <summary>
        /// The kind of move chosen in the preceding MoveAllControls dialog.
        /// Determines whether the second control / scale / rotation steps appear.
        /// </summary>
        public MoveAllControlsAction Action
        {
            get => action;
            set {
                action = value;
                OnPropertyChanged(nameof(ShowSecondControlRows));
                OnPropertyChanged(nameof(ShowScale));
                OnPropertyChanged(nameof(ShowRotation));
            }
        }

        // === Step highlighting / arrow visibility (active step) ===

        /// <summary>True while the user should click the first control to move.</summary>
        public bool IsSelectFirstControlStage => Stage == DialogStage.SelectFirstControl;

        /// <summary>True while the user should pick the new location of the first control.</summary>
        public bool IsMoveFirstControlStage => Stage == DialogStage.MoveFirstControl;

        /// <summary>True while the user should click the second control to move.</summary>
        public bool IsSelectSecondControlStage => Stage == DialogStage.SelectSecondControl;

        /// <summary>True while the user should pick the new location of the second control.</summary>
        public bool IsMoveSecondControlStage => Stage == DialogStage.MoveSecondControl;

        /// <summary>True while the user should confirm the computed move.</summary>
        public bool IsConfirmStage => Stage == DialogStage.Confirm;

        // === Conditional visibility ===

        /// <summary>The second-control steps only apply to scale/rotate moves.</summary>
        public bool ShowSecondControlRows => action != MoveAllControlsAction.Move;

        /// <summary>X/Y offset values are known once the first control has been moved.</summary>
        public bool ShowOffsets => Stage >= DialogStage.MoveFirstControl;

        /// <summary>Scale is meaningful once the second control is moved, for scaling actions.</summary>
        public bool ShowScale => Stage >= DialogStage.MoveSecondControl &&
            (action == MoveAllControlsAction.MoveScale || action == MoveAllControlsAction.MoveRotateScale);

        /// <summary>Rotation is meaningful once the second control is moved, for rotating actions.</summary>
        public bool ShowRotation => Stage >= DialogStage.MoveSecondControl &&
            (action == MoveAllControlsAction.MoveRotate || action == MoveAllControlsAction.MoveRotateScale);

        // === Buttons ===

        /// <summary>The Confirm button only appears on the final step.</summary>
        public bool ShowConfirmButton => Stage == DialogStage.Confirm;

        /// <summary>The Back button appears once the user has started moving controls.</summary>
        public bool ShowBackButton => Stage >= DialogStage.MoveFirstControl;

        /// <summary>Enter accepts Confirm on the final step.</summary>
        public bool ConfirmIsDefault => IsConfirmStage;

        /// <summary>Enter steps Back on the intermediate move steps (where Confirm is not shown).</summary>
        public bool BackIsDefault => ShowBackButton && !IsConfirmStage;

        // === Computed move values (preformatted for display) ===

        [ObservableProperty]
        private string xOffsetText = "";

        [ObservableProperty]
        private string yOffsetText = "";

        [ObservableProperty]
        private string scaleText = "";

        [ObservableProperty]
        private string rotationText = "";

        /// <summary>
        /// Enters the first step. Call once after the dialog is shown (the
        /// Controller's BeginMoveAllControls must already have run).
        /// </summary>
        public void Start()
        {
            Stage = DialogStage.SelectFirstControl;
        }

        /// <summary>
        /// Reacts to a Stage change: refresh the offset display, then re-enter
        /// the Controller command mode for the new step.
        /// </summary>
        partial void OnStageChanged(DialogStage value)
        {
            UpdateMoveValues();
            EnterNewStage();
        }

        /// <summary>
        /// Updates the formatted offset/scale/rotation strings from the points
        /// gathered so far.
        /// </summary>
        private void UpdateMoveValues()
        {
            if (Stage >= DialogStage.MoveSecondControl) {
                MoveAllComputations compute = new MoveAllComputations(action, locations);
                XOffsetText = compute.XOffset.ToString("###0.##");
                YOffsetText = compute.YOffset.ToString("###0.##");
                ScaleText = compute.Scale.ToString("##0.####");
                RotationText = compute.Rotation.ToString("##0.##");
            }
            else if (Stage >= DialogStage.MoveFirstControl) {
                MoveAllComputations compute = new MoveAllComputations(MoveAllControlsAction.Move, locations);
                XOffsetText = compute.XOffset.ToString("###0.##");
                YOffsetText = compute.YOffset.ToString("###0.##");
            }
        }

        /// <summary>
        /// Updates the live preview on the map and sets up the Controller command
        /// mode appropriate to the current step.
        /// </summary>
        private void EnterNewStage()
        {
            if (controller == null)
                return;

            switch (Stage) {
                case DialogStage.SelectFirstControl:
                    controller.MoveAllControlsUpdateMovement(MoveAllControlsAction.None, locations, mapUpdated);
                    mapUpdated = true;
                    controller.MoveAllControlSelectControl(null, ControlSelected);
                    break;
                case DialogStage.MoveFirstControl:
                    controller.MoveAllControlsUpdateMovement(MoveAllControlsAction.None, locations, mapUpdated);
                    mapUpdated = true;
                    controller.MoveAllControlsSelectNewLocation(firstControlPoint, firstSpecial, new PointF(), new PointF(), MoveAllControlsAction.Move, LocationSelected);
                    break;
                case DialogStage.SelectSecondControl:
                    controller.MoveAllControlsUpdateMovement(MoveAllControlsAction.Move, locations, mapUpdated);
                    mapUpdated = true;
                    controller.MoveAllControlSelectControl(locations[1], ControlSelected);
                    break;
                case DialogStage.MoveSecondControl:
                    controller.MoveAllControlsUpdateMovement(MoveAllControlsAction.Move, locations, mapUpdated);
                    mapUpdated = true;
                    controller.MoveAllControlsSelectNewLocation(secondControlPoint, secondSpecial, locations[2], locations[1], action, LocationSelected);
                    break;
                case DialogStage.Confirm:
                    controller.MoveAllControlsUpdateMovement(action, locations, mapUpdated);
                    mapUpdated = true;
                    controller.MoveAllControlsWaitingForConfirmation();
                    break;
            }
        }

        /// <summary>
        /// Controller callback: the user clicked a control or registration mark.
        /// </summary>
        private void ControlSelected(Id<ControlPoint> controlId, Id<Special> specialId, PointF location)
        {
            if (Stage == DialogStage.SelectFirstControl) {
                locations[0] = location;
                firstControlPoint = controlId;
                firstSpecial = specialId;
                Stage = DialogStage.MoveFirstControl;
            }
            else {
                locations[2] = location;
                secondControlPoint = controlId;
                secondSpecial = specialId;
                Stage = DialogStage.MoveSecondControl;
            }
        }

        /// <summary>
        /// Controller callback: the user dragged a control to a new location.
        /// While dragging <paramref name="finalLocation"/> is false; it becomes
        /// true on release, which advances to the next step.
        /// </summary>
        private void LocationSelected(PointF location, bool finalLocation)
        {
            if (Stage == DialogStage.MoveFirstControl) {
                locations[1] = location;

                if (finalLocation) {
                    if (action == MoveAllControlsAction.Move) {
                        Stage = DialogStage.Confirm;
                    }
                    else {
                        Stage = DialogStage.SelectSecondControl;
                    }
                }
            }
            else {
                locations[3] = location;

                if (finalLocation) {
                    Stage = DialogStage.Confirm;
                }
            }

            UpdateMoveValues();
        }

        /// <summary>
        /// Confirms the move (keeps the changes already applied to the map).
        /// Called by the View's Confirm button before it closes the dialog.
        /// </summary>
        public void Confirm()
        {
            if (finished)
                return;
            finished = true;
            controller?.FinishMoveAllControls(false);
        }

        /// <summary>
        /// Steps back to the previous selection. No-op past the first step.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGoBack))]
        private void Back()
        {
            if (Stage > DialogStage.SelectFirstControl) {
                if (action == MoveAllControlsAction.Move && Stage == DialogStage.Confirm) {
                    Stage = DialogStage.MoveFirstControl;
                }
                else {
                    Stage = (DialogStage)(Stage - 1);
                }
            }
        }

        private bool CanGoBack() => Stage > DialogStage.SelectFirstControl;

        /// <summary>
        /// Finishes the command when the dialog is closing without confirmation
        /// (Cancel button or the window's close box). Undoes the live preview if
        /// the map was changed. Safe to call after <see cref="Confirm"/>, which
        /// has already finished the command.
        /// </summary>
        public void HandleClosing()
        {
            if (finished)
                return;
            finished = true;
            controller?.FinishMoveAllControls(mapUpdated);
        }
    }
}
