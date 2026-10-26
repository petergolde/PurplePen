// PdfConverterService.cs
//
// Implementation of IPdfConverter for the WinForms application, using the PdfConverter
// library (PDFium). Nothing here refers to PDFium types outside of Convert, so neither
// PdfConverter.dll nor the native pdfium library is loaded until the first PDF map is converted.

using PurplePen.PdfConverter;

namespace PurplePen
{
    // Converts PDF files into PNG files by calling the PdfConverter library.
    class PdfConverterService : IPdfConverter
    {
        // Convert a PDF file into a PNG file at the given resolution.
        //   dpi: resolution of the resulting bitmap, in dots per inch.
        //   sourcePdfFileName: the PDF file to convert.
        //   destinationPngFileName: the PNG file to create; "%d" converts every page.
        public void Convert(float dpi, string sourcePdfFileName, string destinationPngFileName)
        {
            PdfConversion.Convert(dpi, sourcePdfFileName, destinationPngFileName);
        }
    }
}
