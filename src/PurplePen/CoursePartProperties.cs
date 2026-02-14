using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class CoursePartProperties : PurplePen.OkCancelDialog
    {
        public CoursePartProperties()
        {
            InitializeComponent();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PartOptions PartOptions
        {
            get { return new PartOptions() { ShowFinish = checkBoxDisplayFinish.Checked }; }
            set { checkBoxDisplayFinish.Checked = value.ShowFinish; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowFinishCircleEnabled
        {
            get { return checkBoxDisplayFinish.Enabled; }
            set { checkBoxDisplayFinish.Enabled = value; }
        }
    }
}
