using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class NewEventPaperSize : UserControl, NewEventWizard.IWizardPage
    {
        NewEventWizard containingWizard;

        public NewEventPaperSize()
        {
            InitializeComponent();
        }

        public bool CanProceed
        {
            get
            {
                return true;
            }
        }

        public string Title
        {
            get
            {
                return labelTitle.Text;
            }
        }

        void NewEventPaperSize_Load(object sender , EventArgs e)
        {
            containingWizard = (NewEventWizard)Parent;

            RectangleF printArea = containingWizard.mapBounds;
            float printScaleRatio = containingWizard.DefaultPrintScale / containingWizard.MapScale;
            MapType mapType = containingWizard.MapType;

            int pageWidth, pageHeight, pageMargin;
            bool landscape;

            if (!printArea.IsEmpty && (mapType == MapType.PDF || mapType == MapType.Bitmap)) {
                // Bitmaps and PDFs, we have an exact size to use, and the margin is typically included in the image,
                // so we set our margin to zero and make the exact page size.
                MapUtil.GetExactPageSize(printArea, printScaleRatio, out pageWidth, out pageHeight, out landscape);
                pageMargin = 0;
            }
            else {
                // Set the default page setting from the map size and print scale.
                MapUtil.GetDefaultPageSize(printArea, printScaleRatio, out pageWidth, out pageHeight, out pageMargin, out landscape);
            }

            paperSizeControl.PaperSize = new System.Drawing.Printing.PaperSize("", pageWidth, pageHeight);
            paperSizeControl.MarginSize = pageMargin;
            paperSizeControl.Landscape = landscape;
        }
    }
}
