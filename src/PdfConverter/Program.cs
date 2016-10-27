using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PdfiumViewer;

namespace PurplePen.PdfConverter
{
    class Program
    {
        static float dpi;
        static string sourcePdfFileName;
        static string destinationPngFileName;

        static int Main(string[] args)
        {
            try {
                if (!ParseArguments(args))
                    return 2;

                DoConversion();
                return 0;
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                return 1;
            }
        }

        static bool ParseArguments(string[] args)
        {
            if (args.Length != 3) {
                Console.WriteLine("usage: PdfConverter <dpi> <source_pdf> <destination_png>");
                return false;
            }

            if (!float.TryParse(args[0], out dpi) || dpi< 1 || dpi> 100000) {
                Console.WriteLine("Invalid value for dpi: {0}", args[0]);
                return false;
            }

            sourcePdfFileName = args[1];
            destinationPngFileName = args[2];

            if (!File.Exists(sourcePdfFileName)) {
                Console.WriteLine("Source PDF '{0}' does not exists.", sourcePdfFileName);
                return false;
            }

            return true;
        }

        static void DoConversion()
        {
            using (PdfDocument document = PdfDocument.Load(sourcePdfFileName)) {
                if (destinationPngFileName.Contains("%d")) {
                    // Multi-page convert.
                    int numPages = document.PageCount;
                    for (int pageNumber = 0; pageNumber < numPages; ++pageNumber) {
                        string destFileName = destinationPngFileName.Replace("%d", (pageNumber+1).ToString());
                        SavePng(document, pageNumber, destFileName);
                    }
                }
                else {
                    SavePng(document, 0, destinationPngFileName);
                }
            }
        }

        private static void SavePng(PdfDocument document, int pageNumber, string destFileName)
        {
            SizeF sizeInPoints = document.PageSizes[pageNumber];
            int widthInPixels = (int)Math.Round(sizeInPoints.Width * (float)dpi / 72F);
            int heightInPixels = (int)Math.Round(sizeInPoints.Height * (float)dpi / 72F);
            using (Image image = document.Render(pageNumber, widthInPixels, heightInPixels, dpi, dpi, true)) {
                image.Save(destFileName, ImageFormat.Png);
            }
        }
    }
}
