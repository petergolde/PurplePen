using System;
using System.Collections.Generic;
using System.Diagnostics;

using SysDraw = System.Drawing;
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;

using PurplePen.MapModel;
using PurplePen.Graphics2D;

using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Fonts;

namespace PurplePen.MapModel
{
    // These should only be created by PdfWriter.
    internal class PdfDocumentWriter: IPdfDocumentWriter
    {
        private string fileName;
        private PdfDocument document;

        static PdfDocumentWriter()
        {
            // Set our font resolver so we can use our fonts in PDF output. This is tightly
            // coupled with PdfGraphicsTarget because it relies on the same font resolver to get the font data for measuring text.
           GlobalFontSettings.FontResolver = new PdfFontResolver();
        }

        // Create a PdfDocumentWriter with the given title.
        public PdfDocumentWriter(string fileName, string title, bool cmykMode)
        {
            this.fileName = fileName;
            document = new PdfDocument();
            document.Info.Title = title;
            document.Options.NoCompression = false;
            document.Options.CompressContentStreams = true;
            document.Options.ColorMode = cmykMode ? PdfColorMode.Cmyk : PdfColorMode.Rgb;
        }

        // Get a page.
        public IGraphicsTarget BeginPage(SizeF sizeInInches)
        {
            // Create an empty page
            PdfPage page = document.AddPage();

            // Set the sizes
            var pageSize = new PdfRectangle(new XRect(0, 0, sizeInInches.Width * 72.0F, sizeInInches.Height * 72.0F));
            page.MediaBox = page.ArtBox = page.BleedBox = page.TrimBox = page.CropBox = pageSize;

            // Get an XGraphics object for drawing
            XGraphics gfx = XGraphics.FromPdfPage(page);

            // Get a graphics target
            IGraphicsTarget target = new Pdf_GraphicsTarget(gfx, document.Options.ColorMode == PdfColorMode.Cmyk);

            // Change units to hundreths of inch from points.
            Matrix matrix = new Matrix();
            matrix.Scale(72F / 100F, 72F / 100F);
            target.PushTransform(matrix);

            return target;
        }

        // Test whether a page is readable. Throws an exception if not.
        public static void TestReadingPage(string pathName, int pageNumber)
        {
            using (PdfImporter importer = new PdfImporter(pathName)) {
                PdfPage page = importer.GetPage(pageNumber);
                XForm xform = importer.GetXForm(pageNumber);
                xform.Dispose();
            }
        }

        // Get a page that is a copy of a PDF page.
        public IGraphicsTarget BeginCopiedPage(string importedPdfPath, int pageNumber)
        {
            using (PdfImporter pdfImporter = new PdfImporter(importedPdfPath)) {
                PdfPage pageToCopy = pdfImporter.GetPage(pageNumber);

                // Create an copy of an existing page
                PdfPage page = document.AddPage(pageToCopy);

                // Get an XGraphics object for drawing
                XGraphics gfx = XGraphics.FromPdfPage(page);

                // Get a graphics target
                IGraphicsTarget target = new Pdf_GraphicsTarget(gfx, document.Options.ColorMode == PdfColorMode.Cmyk);

                PointF cropBoxOriginInPoints = CropboxOriginInPoints(pageToCopy);

                // Change units to hundreths of inch from points.
                Matrix matrix = new Matrix();

                matrix.Translate(cropBoxOriginInPoints.X, cropBoxOriginInPoints.Y);
                matrix.Scale(72F / 100F, 72F / 100F);
                target.PushTransform(matrix);

                //pageToCopy.Close();

                return target;
            }
        }

        // Get a page that is a copy of a PDF page.
        // sizeInInches is the size of the new page, in inches.
        // partialSourcePageInInches is the rectangle on the source page to copy, in inches. This maps to destinationCropInInches.
        // destinationCropInInches in the rectangle on the destination page to draw into, in inches. If you want the whole page, use new RectangleF(0,0,sizeInInches.Width,sizeInInches.Height)
        public IGraphicsTarget BeginCopiedPartialPage(string importedPdfPath, int pageNumber, SizeF sizeInInches, RectangleF partialSourcePageInInches, RectangleF destinationCropInInches)
        {
            using (PdfImporter pdfImporter = new PdfImporter(importedPdfPath))
            using (XForm xformToCopy = pdfImporter.GetXForm(pageNumber)) {
                PdfPage pageToCopy = pdfImporter.GetPage(pageNumber);

                PointF cropBoxOriginInPoints = CropboxOriginInPoints(pageToCopy);

                IGraphicsTarget target = BeginPage(sizeInInches);

                // Initial target is entire page. Push a clip to restrict to destinationCropInInches.
                RectangleF destRect = new RectangleF(destinationCropInInches.Left * 100, destinationCropInInches.Top * 100, destinationCropInInches.Width * 100, destinationCropInInches.Height * 100);
                target.PushClip(destRect);

                // Create transform that maps the source page to the destination crop rect. Destination is in hundreths of inches so must match that.
                RectangleF srcRect = new RectangleF(partialSourcePageInInches.Left * 100, partialSourcePageInInches.Top * 100, partialSourcePageInInches.Width * 100, partialSourcePageInInches.Height * 100);
                srcRect.Offset(cropBoxOriginInPoints.X / 72 * 100, cropBoxOriginInPoints.Y / 72 * 100);
                Matrix transform = Geometry.CreateRectangleTransform(srcRect, destRect);

                target.PushTransform(transform);
                XGraphics xGraphics = ((Pdf_GraphicsTarget)target).XGraphics;
                xGraphics.DrawImage(xformToCopy, new XRect(0, 0, xformToCopy.PointWidth / 72F * 100F, xformToCopy.PointHeight / 72F * 100F));
                target.PopTransform();
                target.PopClip();

                return target;
            }
        }

        private PointF CropboxOriginInPoints(PdfPage pageToCopy)
        {
            PdfRectangle cropRect = pageToCopy.CropBox;
            if (!cropRect.IsZero) {
                double translateX = cropRect.Location.X;
                double translateY = pageToCopy.Height.Point - (cropRect.Location.Y + cropRect.Size.Height);
                return new PointF((float)translateX, (float)translateY);
            }
            else {
                return new PointF();
            }
        }

        public void EndPage(IGraphicsTarget target)
        {
            target.Dispose();
        }

        // Save the PDF 
        public void Save()
        {
            document.Save(fileName);
        }
    }

    // Class that can create PdfDocumentWriter instances.
    public class PdfWriter: IPdfWriter
    {
        public bool CanReadPdfPage(string pdfImport, int pageImport)
        {
            try {
                PdfDocumentWriter.TestReadingPage(pdfImport, pageImport);
            }
            catch (Exception) {
                return false;
            }

            return true;
        }

        public IPdfDocumentWriter CreateDocument(string fileName, string title, bool cmykMode)
        {
            return new PdfDocumentWriter(fileName, title, cmykMode);
        }
    }
}
