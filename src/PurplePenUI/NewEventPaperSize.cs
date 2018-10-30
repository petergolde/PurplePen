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

            // Set the default page setting from the map size and print scale.
            int pageWidth, pageHeight, pageMargin;
            bool landscape;
            MapUtil.GetDefaultPageSize(printArea, printScaleRatio, out pageWidth, out pageHeight, out pageMargin, out landscape);
            paperSizeControl.PaperSize = new System.Drawing.Printing.PaperSize("", pageWidth, pageHeight);
            paperSizeControl.MarginSize = pageMargin;
            paperSizeControl.Landscape = landscape;
        }
    }
}
