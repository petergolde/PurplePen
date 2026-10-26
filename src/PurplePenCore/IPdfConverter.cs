// IPdfConverter.cs
//
// Abstraction for rasterizing a PDF file into a PNG bitmap.
// Lives in PurplePenCore so it can be accessed via Services.PdfConverter.
// The implementation lives in the application (AvPurplePen), which references the
// PdfConverter library, so that PurplePenCore does not depend on PDFium.

namespace PurplePen
{
    /// <summary>
    /// Service for converting a PDF file into a PNG file.
    /// </summary>
    public interface IPdfConverter
    {
        /// <summary>
        /// Converts a PDF file into a PNG file at the given resolution, recording the
        /// resolution in the PNG. Throws an exception describing the problem if the conversion fails.
        /// May be called on any thread.
        /// </summary>
        /// <param name="dpi">Resolution of the resulting bitmap, in dots per inch.</param>
        /// <param name="sourcePdfFileName">The PDF file to convert.</param>
        /// <param name="destinationPngFileName">The PNG file to create. If it contains "%d", every page
        /// is converted, with "%d" replaced by the 1-based page number; otherwise only the first page is converted.</param>
        void Convert(float dpi, string sourcePdfFileName, string destinationPngFileName);
    }
}
