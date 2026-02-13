using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;

namespace PurplePen
{
    partial class SelectLocationsForMove : BaseDialog
    {
        DialogStage stage;
        MoveAllControlsAction action;
        Controller controller;
        Id<ControlPoint> firstControlPoint, secondControlPoint;
        Id<Special> firstSpecial, secondSpecial;
        PointF[] locations = new PointF[4];
        bool mapUpdated = false;

        Color activeColor = Color.Red;
        Color inactiveColor = Color.Gray;

        public SelectLocationsForMove(Controller controller, MoveAllControlsAction action)
        {
            InitializeComponent();
            this.controller = controller;
            this.action = action;
            this.stage = DialogStage.Begin;
        }

        
        public DialogStage Stage
        {
            get { return stage; }
            set {
                if (stage != value) {
                    stage = value;
                    UpdateControls();
                    EnterNewStage();
                }
            }
        }

        public MoveAllControlsAction Action
        {
            get { return action; }
            set {
                action = value;
                UpdateControls();
            }
        }

        void UpdateControls()
        {
            labelArrowFirstControl.ForeColor = labelFirstControl.ForeColor = (stage == DialogStage.SelectFirstControl) ? activeColor : inactiveColor;
            labelArrowFirstNewLocation.ForeColor = labelFirstNewLocation.ForeColor = (stage == DialogStage.MoveFirstControl) ? activeColor : inactiveColor;
            labelArrowSecondControl.ForeColor = labelSecondControl.ForeColor = (stage == DialogStage.SelectSecondControl) ? activeColor : inactiveColor;
            labelArrowSecondNewLocation.ForeColor = labelSecondNewLocation.ForeColor = (stage == DialogStage.MoveSecondControl) ? activeColor : inactiveColor;
            labelArrowConfirmLocations.ForeColor = labelConfirmLocations.ForeColor = (stage == DialogStage.Confirm) ? activeColor : inactiveColor;

            labelArrowSecondControl.Visible = labelSecondControl.Visible = labelArrowSecondNewLocation.Visible = labelSecondNewLocation.Visible = (action != MoveAllControlsAction.Move);

            labelArrowFirstControl.Visible = (stage == DialogStage.SelectFirstControl);
            labelArrowFirstNewLocation.Visible = (stage == DialogStage.MoveFirstControl);
            labelArrowSecondControl.Visible = (stage == DialogStage.SelectSecondControl);
            labelArrowSecondNewLocation.Visible = (stage == DialogStage.MoveSecondControl);
            labelArrowConfirmLocations.Visible = (stage == DialogStage.Confirm);

            labelXOffset.Visible = labelDisplayXOffset.Visible = (stage >= DialogStage.MoveFirstControl);
            labelYOffset.Visible = labelDisplayYOffset.Visible = (stage >= DialogStage.MoveFirstControl);
            labelScale.Visible = labelDisplayScale.Visible = (stage >= DialogStage.MoveSecondControl && (action == MoveAllControlsAction.MoveScale || action == MoveAllControlsAction.MoveRotateScale));
            labelRotation.Visible = labelDisplayRotation.Visible = (stage >= DialogStage.MoveSecondControl && (action == MoveAllControlsAction.MoveRotate || action == MoveAllControlsAction.MoveRotateScale));

            if (stage == DialogStage.Confirm) {
                buttonConfirm.Visible = buttonBack.Visible = true;
                this.AcceptButton = buttonConfirm;
                this.ActiveControl = buttonConfirm;
            }
            else if (stage >= DialogStage.MoveFirstControl) {
                buttonConfirm.Visible = false;
                buttonBack.Visible = true;
                this.AcceptButton = buttonBack;
                this.ActiveControl = buttonBack;
            }
            else {
                buttonConfirm.Visible = buttonBack.Visible = false;
                this.AcceptButton = null;
                this.ActiveControl = null;
            }

            UpdateMoveValues();
        }

        private void UpdateMoveValues()
        {
            if (stage >= DialogStage.MoveSecondControl) {
                MoveAllComputations compute = new MoveAllComputations(action, locations);
                labelDisplayXOffset.Text = compute.XOffset.ToString("###0.##");
                labelDisplayYOffset.Text = compute.YOffset.ToString("###0.##");
                labelDisplayScale.Text = compute.Scale.ToString("##0.####");
                labelDisplayRotation.Text = compute.Rotation.ToString("##0.##");
            }
            else if (stage >= DialogStage.MoveFirstControl) {
                MoveAllComputations compute = new MoveAllComputations(MoveAllControlsAction.Move, locations);
                labelDisplayXOffset.Text = compute.XOffset.ToString("###0.##");
                labelDisplayYOffset.Text = compute.YOffset.ToString("###0.##");
            }
        }

        void EnterNewStage()
        {
            switch (stage) {
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

        void ControlSelected(Id<ControlPoint> controlId, Id<Special> specialId, PointF location)
        {
            if (stage == DialogStage.SelectFirstControl) {
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

        void LocationSelected(PointF location, bool finalLocation)
        {
            if (stage == DialogStage.MoveFirstControl) {
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

        private void buttonConfirm_Click(object sender, EventArgs e)
        {
            controller.FinishMoveAllControls(false);
            Hide();
            Dispose();
        }

        private void buttonBack_Click(object sender, EventArgs e)
        {
            if (stage > DialogStage.SelectFirstControl) {
                Stage = (DialogStage) (stage - 1);
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            controller.FinishMoveAllControls(mapUpdated);
            Hide();
            Dispose();
        }

        private void SelectLocationsForMove_FormClosing(object sender, FormClosingEventArgs e)
        {
            buttonCancel_Click(sender, e);
        }

        public enum DialogStage
        {
            Begin, SelectFirstControl, MoveFirstControl, SelectSecondControl, MoveSecondControl, Confirm
        }

        private void SelectLocationsForMove_Load(object sender, EventArgs e)
        {
            UpdateControls();

            Stage = DialogStage.SelectFirstControl;
        }
    }
}
