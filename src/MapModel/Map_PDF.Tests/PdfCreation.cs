using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;

using PurplePen.MapModel;
using PurplePen.Graphics2D;
using TestingUtils;
using System.IO;
using PdfSharp.Pdf;
using PdfSharp.Drawing;

namespace Map_PDF.Tests
{
    static class PdfCreation
    {
        public static void CreatePdfAndPng(string pdfFileName, string pngFileName, int pixelWidth, int pixelHeight, bool useCmyk, Action<IGraphicsTarget> draw)
        {
            File.Delete(pdfFileName);
            File.Delete(pngFileName);

            // Create PDF at 100 pixel per inch.
            PdfWriter pdfWriter = new PdfWriter(Path.GetFileNameWithoutExtension(pdfFileName), useCmyk);
            IGraphicsTarget graphicsTarget = pdfWriter.BeginPage(new SizeF(pixelWidth / 100F, pixelHeight / 100F));
            draw(graphicsTarget);
            pdfWriter.EndPage(graphicsTarget);
            pdfWriter.Save(pdfFileName);

            // Start PDF viewer
            //Process.Start(pdfFileName);

            // Copy to PNG
            ConvertPdfToPng(pdfFileName, pngFileName);

        }

        public static void CreatePdfAndPngUsingCopiedPage(string pdfFileName, string pngFileName, string pdfImport, int pageImport, Action<IGraphicsTarget> draw)
        {
            File.Delete(pdfFileName);
            File.Delete(pngFileName);

            // Get imported page.
            PdfImporter importer = new PdfImporter(pdfImport);

            // Create PDF at 100 pixel per inch.
            PdfWriter pdfWriter = new PdfWriter(Path.GetFileNameWithoutExtension(pdfFileName), false);
            IGraphicsTarget graphicsTarget = pdfWriter.BeginCopiedPage(importer, pageImport);
            draw(graphicsTarget);
            pdfWriter.EndPage(graphicsTarget);
            pdfWriter.Save(pdfFileName);
            importer.Dispose();

            // Start PDF viewer
            //Process.Start(pdfFileName);

            // Copy to PNG
            ConvertPdfToPng(pdfFileName, pngFileName);

        }

        public static void CreatePdfAndPngUsingCopiedPartialPage(string pdfFileName, string pngFileName, string pdfImport, int pageImport, 
                                                                 SizeF sizeInInches, RectangleF partialPageInInches, RectangleF destRectangleInInches, Action<IGraphicsTarget> draw)
        {
            File.Delete(pdfFileName);
            File.Delete(pngFileName);

            // Get imported page.
            PdfImporter importer = new PdfImporter(pdfImport);

            // Create PDF at 100 pixel per inch.
            PdfWriter pdfWriter = new PdfWriter(Path.GetFileNameWithoutExtension(pdfFileName), false);
            IGraphicsTarget graphicsTarget = pdfWriter.BeginCopiedPartialPage(importer, pageImport, sizeInInches, partialPageInInches, destRectangleInInches);
            draw(graphicsTarget);
            pdfWriter.EndPage(graphicsTarget);
            pdfWriter.Save(pdfFileName);
            importer.Dispose();

            // Start PDF viewer
            //Process.Start(pdfFileName);

            // Copy to PNG
            ConvertPdfToPng(pdfFileName, pngFileName);

        }

        public static void ConvertPdfToPng(string pdfFileName, string pngFileName)
        {
            string arguments = String.Format(
                "-dSTRICT -dSAFER -dBATCH -dNOPAUSE -r100  -dTextAlphaBits=4 -dGraphicsAlphaBits=4 -sDEVICE=png16m -sOutputFile=\"{1}\" \"{0}\"",
                pdfFileName, pngFileName);

            ProcessStartInfo startInfo = new ProcessStartInfo(TestUtil.GetToolFullPath("gswin32c.exe"), arguments);
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Process process = Process.Start(startInfo);
            process.WaitForExit();
        }
    }
}
