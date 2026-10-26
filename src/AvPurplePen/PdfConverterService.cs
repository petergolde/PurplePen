// PdfConverterService.cs
//
// Implementation of IPdfConverter for Avalonia, using the PdfConverter library (PDFium).
// Nothing here refers to PDFium types outside of Convert, so neither PdfConverter.dll nor
// the native pdfium library is loaded until the first PDF map is converted.

using PurplePen;
using PurplePen.PdfConverter;

namespace AvPurplePen
{
    /// <summary>
    /// Converts PDF files into PNG files by calling the PdfConverter library.
    /// </summary>
    public class PdfConverterService : IPdfConverter
    {
        /// <summary>
        /// Converts a PDF file into a PNG file at the given resolution.
        /// </summary>
        /// <param name="dpi">Resolution of the resulting bitmap, in dots per inch.</param>
        /// <param name="sourcePdfFileName">The PDF file to convert.</param>
        /// <param name="destinationPngFileName">The PNG file to create; "%d" converts every page.</param>
        public void Convert(float dpi, string sourcePdfFileName, string destinationPngFileName)
        {
            PdfConversion.Convert(dpi, sourcePdfFileName, destinationPngFileName);
        }
    }
}
