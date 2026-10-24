using PurplePen.Graphics2D;
using PurplePen.MapModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PurplePen
{
    // A printing target that prints to a PDF file.
    public class PdfPrintTarget : IPrintingTarget
    {
        // The resolution reported for PDFs. PDFs are inherently high resolution, so a high number is used: 1200 seems good.
        public const float PdfDpi = 1200F;

        private readonly string pathName;
        private readonly bool cmykMode;

        private IPdfDocumentWriter pdfWriter;

        public PdfPrintTarget(string pathName, bool cmykMode)
        {
            this.pathName = pathName;
            this.cmykMode = cmykMode;
        }

        public void StartPrinting(string documentTitle, int pageCount)
        {
            pdfWriter = Services.PdfWriter.CreateDocument(pathName, documentTitle, cmykMode);
        }

        // PDFs are inherently high resolution, so return a high resolution number here.
        public float GetPrinterDpi()
        {
            return PdfDpi;
        }

        public void EndPrinting()
        {
            pdfWriter.Save();
        }

        public void PrintPage(int pageNumber, PrintingPaperSize paperSize, Action<IGraphicsTarget> drawPage)
        {
            using (IGraphicsTarget grTarget = pdfWriter.BeginPage(paperSize.SizeInInches)) {
                drawPage(grTarget);
            }
        }
    }
}
