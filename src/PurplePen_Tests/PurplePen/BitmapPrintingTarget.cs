using PdfSharp;
using PurplePen;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using PurplePen.Tests;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurplePen_Tests.PurplePen
{
    // A printing target that prints to a list of bitmaps, one per page, for testing purposes.
    internal class BitmapPrintingTarget : IPrintingTarget
    {
        const float Dpi = 200F;   // using 200 dpi.

        int currentPage;  // pages start at 1.
        List<Bitmap> bitmaps = new List<Bitmap>();
        string documentTitle;

        public Bitmap[] Bitmaps => bitmaps.ToArray();

        public string DocumentTitle => documentTitle;

        public void StartPrinting(string documentTitle, int pageCount)
        {
            currentPage = 1;
            this.documentTitle = documentTitle;
        }

        public float GetPrinterDpi()
        {
            return Dpi;
        }

        public void PrintPage(int pageNumber, PrintingPaperSize paperSize, Action<IGraphicsTarget> drawPage)
        {
            Debug.Assert(pageNumber == currentPage, "Page numbers must start at 1 and be printed in order.");

            int width = (int) Math.Round(paperSize.SizeInHundreths.Width * 2);
            int height = (int) Math.Round(paperSize.SizeInHundreths.Height * 2);
            RectangleF drawingRectangle = new RectangleF(0, 0, width / 2F, height / 2F);  // Drawing coordinates are 1/100 of an inch.
            Bitmap bm = TestRenderingUtils.RenderToBitmap(width, height, drawingRectangle, false, graphicsTarget => {
                graphicsTarget.PushAntiAliasing(true);
                drawPage(graphicsTarget);
            });
            bm.SetResolution(Dpi, Dpi);

            bitmaps.Add(bm);

            ++currentPage;
        }

        public void EndPrinting()
        {
        }
    }
}
