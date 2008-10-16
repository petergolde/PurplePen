using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class NonPrintableObjects: Form
    {
        string mapName;
        string[] badObjectList;

        public NonPrintableObjects()
        {
            InitializeComponent();
        }

        public string MapName
        {
            get { return mapName; }
            set
            {
                mapName = value;
                labelWarning.Text = string.Format(labelWarning.Text, mapName);
            }
        }

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

        private void NonPrintableObjects_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            Util.ShowHelpTopic(this, "NonPrintableObjectsDialog.htm");
            e.Cancel = true;
        }

        private void okButton_Click(object sender, EventArgs e)
        {

        }

    }
}