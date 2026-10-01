using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class PunchcardLayoutDialog: OkCancelDialog
    {
        public PunchcardLayoutDialog()
        {
            InitializeComponent();
        }

        // Get or set the punch card format.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PunchcardFormat PunchcardFormat
        {
            get
            {
                PunchcardFormat format = new PunchcardFormat();

                format.boxesDown = (int) rowsUpDown.Value;
                format.boxesAcross = (int) colsUpDown.Value;

                if (orderLRTB.Checked)
                    { format.leftToRight = true; format.topToBottom = true;}
                else if (orderLRBT.Checked)
                    { format.leftToRight = true; format.topToBottom = false;}
                else if (orderRLTB.Checked)
                    { format.leftToRight = false; format.topToBottom = true;}
                else if (orderRLBT.Checked) 
                    { format.leftToRight = false; format.topToBottom = false; }

                return format;
            }

            set
            {
                rowsUpDown.Value = value.boxesDown;
                colsUpDown.Value = value.boxesAcross;

                if (value.leftToRight && value.topToBottom)
                    orderLRTB.Checked = true;
                else if (value.leftToRight && !value.topToBottom)
                    orderLRBT.Checked = true;
                else if (!value.leftToRight && value.topToBottom)
                    orderRLTB.Checked = true;
                else if (!value.leftToRight && !value.topToBottom)
                    orderRLBT.Checked = true;
            }
        }
    }
}