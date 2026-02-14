using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class NonPrintableObjects: BaseDialog
    {
        string mapName;
        string[] badObjectList;

        public NonPrintableObjects(bool showCancelAndContinue)
        {
            InitializeComponent();
            if (!showCancelAndContinue) {
                cancelButton.Visible = false;
                okButton.Location = cancelButton.Location;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string MapName
        {
            get { return mapName; }
            set
            {
                mapName = value;
                labelWarning.Text = string.Format(labelWarning.Text, mapName);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string[] BadObjectList
        {
            get { return badObjectList; }
            set
            {
                badObjectList = value;

                listBoxBadObjects.Items.Clear();
                if (badObjectList != null) {
                    foreach (string s in badObjectList)
                        listBoxBadObjects.Items.Add(s);
                }
            }
        }

    }
}