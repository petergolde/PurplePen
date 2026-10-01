using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

using PurplePen.MapModel;

namespace PurplePen
{
    public partial class NewEventMapFile: UserControl, NewEventWizard.IWizardPage
    {
        NewEventWizard containingWizard;

        public NewEventMapFile()
        {
            InitializeComponent();
        }

        public bool CanProceed
        {
            get { return (mapFileNameTextBox.Text.Length > 0) && !errorDisplayPanel.Visible; }
        }

        public string Title
        {
            get { return labelTitle.Text; }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK) {
                containingWizard.MapFileName = mapFileNameTextBox.Text = openFileDialog.FileName;
                mapFileDisplay.Visible = true;

                string errorMessageText;
                float dpi;  // not used here.
                float mapScale;
                MapType mapType;
                int? lowerPurpleMapLayer;
                Size bitmapSize;
                RectangleF mapBounds;
                if (CoreMapUtil.ValidateMapFile(containingWizard.MapFileName, out mapScale, out dpi, out bitmapSize, out mapBounds, out mapType, out lowerPurpleMapLayer, out errorMessageText)) 
                {
                    // map file is OK.
                    containingWizard.MapScale = mapScale;
                    containingWizard.MapType = mapType;
                    containingWizard.BitmapSize = bitmapSize;
                    containingWizard.mapBounds = mapBounds;
                    containingWizard.LowerPurpleMapLayer = lowerPurpleMapLayer;
                    errorDisplayPanel.Visible = false;
                    infoDisplayPanel.Visible = true;
                    ((Control)ParentForm.AcceptButton).Focus();
                }
                else {
                    // map file is not OK. Show message.
                    errorMessage.Text = errorMessageText;
                    infoDisplayPanel.Visible = false;
                    errorDisplayPanel.Visible = true;
                }
            }
        }


        private void NewEventMapFile_Load(object sender, EventArgs e)
        {
            containingWizard = (NewEventWizard) Parent;
        }
    }
}
