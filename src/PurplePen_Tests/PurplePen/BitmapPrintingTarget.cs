using PdfSharp;
using PurplePen;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

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
            Bitmap bm = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            bm.SetResolution(Dpi, Dpi);

            BitmapData bitmapData = bm.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bm.PixelFormat);
            try {
                SKImageInfo imageInfo = new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
                using (SKSurface surface = SKSurface.Create(imageInfo, bitmapData.Scan0, bitmapData.Stride)) {
                    SKCanvas canvas = surface.Canvas;
                    canvas.Clear(SKColors.White);
                    canvas.Scale(2, 2);  // Scaling must be set in 1/100 of an inch.

                    using (IGraphicsTarget grTarget = new Skia_GraphicsTarget(canvas)) {
                        grTarget.PushAntiAliasing(true);
                        drawPage(grTarget);
                    }
                }
            }
            finally {
                bm.UnlockBits(bitmapData);
            }

            bitmaps.Add(bm);

            ++currentPage;
        }

        public void EndPrinting()
        {
        }
    }
}
