using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class NewEventStandards: UserControl, NewEventWizard.IWizardPage
    {
        public NewEventStandards()
        {
            InitializeComponent();

            if (Settings.Default.NewEventMapStandard == "2017")
                radioButtonMap2017.Checked = true;
            else
                radioButtonMap2000.Checked = true;

            if (Settings.Default.NewEventDescriptionStandard == "2018")
                radioButtonDescriptions2018.Checked = true;
            else
                radioButtonDescriptions2004.Checked = true;
        }

        public bool CanProceed
        {
            get {
                return (radioButtonDescriptions2004.Checked || radioButtonDescriptions2018.Checked) &&
                       (radioButtonMap2000.Checked || radioButtonMap2017.Checked);

            } 
        }

        public string Title
        {
            get { return labelTitle.Text; }
        }


    }
}
