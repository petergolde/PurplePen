using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class MoveAllControls : OkCancelDialog
    {
        public MoveAllControlsAction Action { 
            get {
                if (radioButtonMove.Checked)
                    return MoveAllControlsAction.Move;
                else if (radioButtonMoveAndScale.Checked)
                    return MoveAllControlsAction.MoveScale;
                else if (radioButtonMoveAndRotate.Checked)
                    return MoveAllControlsAction.MoveRotate;
                else if (radioButtonMoveRotateScale.Checked)
                    return MoveAllControlsAction.MoveRotateScale;

                throw new InvalidOperationException("No valid action is selected in MoveAllControls.");
            }
        }

        public MoveAllControls()
        {
            InitializeComponent();
        }
    }

    public enum MoveAllControlsAction
    {
        None, Move, MoveScale, MoveRotate, MoveRotateScale
    }
}
