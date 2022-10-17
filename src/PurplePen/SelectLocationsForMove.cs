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

            labelXOffset.Visible = labelDisplayXOffset.Visible = (stage == DialogStage.Confirm);
            labelYOffset.Visible = labelDisplayYOffset.Visible = (stage == DialogStage.Confirm);
            labelScale.Visible = labelDisplayScale.Visible = (stage == DialogStage.Confirm && (action == MoveAllControlsAction.MoveScale || action == MoveAllControlsAction.MoveRotateScale));
            labelRotation.Visible = labelDisplayRotation.Visible = (stage == DialogStage.Confirm && (action == MoveAllControlsAction.MoveRotate || action == MoveAllControlsAction.MoveRotateScale));

            buttonConfirm.Visible = buttonRestart.Visible = (stage == DialogStage.Confirm);
        }

        void EnterNewStage()
        {
            switch (stage) {
                case DialogStage.SelectFirstControl:
                    controller.MoveAllControlSelectControl(ControlSelected);
                    break;
                case DialogStage.MoveFirstControl:
                    controller.MoveAllControlsSelectNewLocation(firstControlPoint, firstSpecial, LocationSelected);
                    break;
                case DialogStage.SelectSecondControl:
                    controller.MoveAllControlSelectControl(ControlSelected);
                    break;
                case DialogStage.MoveSecondControl:
                    controller.MoveAllControlsSelectNewLocation(secondControlPoint, secondSpecial, LocationSelected);
                    break;
                case DialogStage.Confirm:
                    controller.MoveAllControlsWaitingForConfirmation();
                    break;
            }
        }

        void ControlSelected(Id<ControlPoint> controlId, Id<Special> specialId, PointF location)
        {
            if (stage == DialogStage.SelectFirstControl) {
                firstControlPoint = controlId;
                firstSpecial = specialId;
                Stage = DialogStage.MoveFirstControl;
            }
            else {
                secondControlPoint = controlId;
                secondSpecial = specialId;
                Stage = DialogStage.MoveSecondControl;
            }
        }

        void LocationSelected(PointF location)
        {
            if (stage == DialogStage.MoveFirstControl) {
                if (action == MoveAllControlsAction.Move) {
                    Stage = DialogStage.Confirm;
                }
                else {
                    Stage = DialogStage.SelectSecondControl;
                }
            }
            else {
                Stage = DialogStage.Confirm;
            }
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
