using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class NewEventPrintScale: UserControl, NewEventWizard.IWizardPage
    {
        NewEventWizard containingWizard;

        public NewEventPrintScale()
        {
            InitializeComponent();
        }

        public bool CanProceed
        {
            get
            {
                float printScale = 0;
                bool result = float.TryParse(comboBoxPrintScale.Text, out printScale);
                if (result && printScale > 0) {
                    containingWizard.defaultPrintScale = printScale;
                    return true;
                }
                else
                    return false;
            }
        }

        public string Title
        {
            get { return labelTitle.Text; }
        }

        private void NewEventPrintScale_Load(object sender, EventArgs e)
        {
            containingWizard = (NewEventWizard) Parent;
        }

        private void NewEventPrintScale_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible) {
                if (containingWizard.defaultPrintScale == 0)
                    containingWizard.defaultPrintScale = containingWizard.mapScale;

                // map scale label
                labelMapScale.Text = Convert.ToString(containingWizard.mapScale);

                // print scale combo box
                comboBoxPrintScale.Items.Clear();
                foreach (float f in Util.PrintScaleList(containingWizard.mapScale))
                    comboBoxPrintScale.Items.Add(f);
                comboBoxPrintScale.Text = Convert.ToString(containingWizard.defaultPrintScale);
            }
        }
    }
}
