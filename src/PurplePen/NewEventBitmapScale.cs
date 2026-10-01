using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace PurplePen
{
    public partial class NewEventBitmapScale: UserControl, NewEventWizard.IWizardPage
    {
        NewEventWizard containingWizard;
        public float dpi;
        public MapType mapType;

        public NewEventBitmapScale()
        {
            InitializeComponent();
        }

        public bool CanProceed
        {
            get {
                float mapScale = 0;
                bool result = (mapType == MapType.PDF || float.TryParse(dpiTextBox.Text, out dpi)) && 
                              float.TryParse(scaleTextBox.Text, out mapScale);
                if (result)
                    containingWizard.MapScale = mapScale;
                return result;
            }
        }

        public string Title
        {
            get { return labelTitle.Text; }
        }

        private void NewEventBitmapScale_Load(object sender, EventArgs e)
        {
            containingWizard = (NewEventWizard) Parent;
            mapType = containingWizard.MapType;

            if (mapType == MapType.Bitmap) {
                Bitmap bitmap = (Bitmap)Image.FromFile(containingWizard.MapFileName);

                // GIF format doesn't have built-in resolution, so don't default it.
                if (bitmap.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Gif))
                    dpiTextBox.Text = "";
                else
                    dpiTextBox.Text = bitmap.HorizontalResolution.ToString();

                pdfScaleLabel.Visible = false;
                bitmapScaleLabel.Visible = dpiTextBox.Visible = resolutionLabel.Visible = dpiLabel.Visible = true;
                bitmap.Dispose();
            }
            else {
                pdfScaleLabel.Visible = true;
                bitmapScaleLabel.Visible = dpiTextBox.Visible = resolutionLabel.Visible = dpiLabel.Visible = false;
            }

            scaleTextBox.Text = "15000";
        }
    }
}
