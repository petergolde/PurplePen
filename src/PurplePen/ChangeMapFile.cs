using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace PurplePen
{
    public partial class ChangeMapFile: OkCancelDialog
    {
        private string mapFile;
        private MapType mapType;

        public ChangeMapFile()
        {
            InitializeComponent();
            errorMessage.Font = new Font(Font, FontStyle.Bold);

            UpdateMapFile();
        }

        public string MapFile
        {
            get
            {
                return mapFile;
            }
            set
            {
                mapFile = value;

                UpdateMapFile();
            }
        }

        public MapType MapType
        {
            get
            {
                return mapType;
            }
        }

        public float MapScale
        {
            get
            {
                float scale;
                if (float.TryParse(textBoxScale.Text, out scale))
                    return scale;
                else
                    return 0;
            }
            set
            {
                textBoxScale.Text = value.ToString();
            }
        }

        public float Dpi
        {
            get
            {
                float dpi;
                if (float.TryParse(textBoxDpi.Text, out dpi))
                    return dpi;
                else
                    return 0;
            }
            set
            {
                textBoxDpi.Text = value.ToString();
            }
        }

        // Shows only the open file dialog.
        public DialogResult ShowOpenFileDialogOnly(IWin32Window owner)
        {
            // Choose a new map file.
            openFileDialog.FileName = mapFile;

            DialogResult result = openFileDialog.ShowDialog(owner);
            if (result == DialogResult.OK) {
                MapFile = openFileDialog.FileName;

                if (IsOK() && MapType == MapType.OCAD)
                    return DialogResult.OK;       // We chose a valid file, and don't need the bitmap resolution information.
            }

            // We need to show the dialog after all.
            return ShowDialog(owner);
        }

        // The map file name has been updated.
        void UpdateMapFile()
        {
            if (mapFile == null || mapFile == "") {
                // No map file.
                errorDisplayPanel.Visible = false;
                panelScaleDpi.Visible = false;
            }
            else {
                // Validate the map file.
                string errorMessageText;
                float dpi, mapScale;
                bool ok = MapUtil.ValidateMapFile(mapFile, out mapScale, out dpi, out mapType, out errorMessageText);
                if (ok) {
                    if (mapType == MapType.OCAD) {
                        panelScaleDpi.Visible = false;
                        errorDisplayPanel.Visible = false;
                        textBoxScale.Text = mapScale.ToString();
                    }
                    else if (mapType == MapType.Bitmap) {
                        panelScaleDpi.Visible = true;
                        errorDisplayPanel.Visible = false;
                        textBoxDpi.Text = dpi.ToString();
                    }
                    else {
                        Debug.Fail("unexpected map type.");
                    }
                }
                else {
                    errorMessage.Text = errorMessageText;
                    panelScaleDpi.Visible = false;
                    errorDisplayPanel.Visible = true;
                }
            }

            mapFileNameTextBox.Text = mapFile;
            UpdateOKButton();
        }

        private void buttonChooseFile_Click(object sender, EventArgs e)
        {
            // Choose a new map file.
            openFileDialog.FileName = mapFile;

            DialogResult result = openFileDialog.ShowDialog();
            if (result == DialogResult.OK) 
                MapFile = openFileDialog.FileName;
        }

        // Should the OK button be enabled?
        private bool IsOK()
        {
            if (mapType == MapType.OCAD)
                return true;
            else if (mapType == MapType.Bitmap) {
                float dummy1, dummy2;
                return (float.TryParse(textBoxDpi.Text, out dummy1) && float.TryParse(textBoxScale.Text, out dummy2));
            }
            else
                return false;
        }

        private void UpdateOKButton()
        {
            okButton.Enabled = IsOK();
        }

        private void textBoxScale_TextChanged(object sender, EventArgs e)
        {
            UpdateOKButton();
        }

        private void textBoxDpi_TextChanged(object sender, EventArgs e)
        {
            UpdateOKButton();
        }


    }
}