/* Copyright (c) 2006-2008, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using PurplePen.MapModel;
using PurplePen.Graphics2D;
using ColorConverter = PurplePen.Graphics2D.ColorConverter;
using System.Drawing.Printing;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;

namespace PurplePen
{
    static class MapUtil
    {
        public static ITextMetrics TextMetricsProvider = new GDIPlus_TextMetrics();

        public static PaperSize[] StandardPaperSizes = {
            new PaperSize("A2", 1654, 2339),
            new PaperSize("A3", 1169, 1654),
            new PaperSize("A4", 827, 1169),
            new PaperSize("A5", 583, 827),
            new PaperSize("A6", 413, 583),
            new PaperSize("Letter", 850, 1100),
            new PaperSize("Legal", 850, 1400),
            new PaperSize("Tabloid", 1100, 1700)
        };

        public const int FirstEnglishPaperSizeIndex = 5;
        public const int DefaultEnglighPaperSizeIndex = 5;
        public const int DefaultMetricPaperSizeindex = 2;

        public const int DefaultEnglishMargin = 25;  // 1/4 of a inch.
        public const int DefaultMetricMargin = 28; // 7mm



        // Validate the map file to make sure it is readable. If OK, return true and set the scale.
        // If not OK, return false and set the error message. 
        public static bool ValidateMapFile(string mapFileName, out float scale, out float dpi, out Size bitmapSize, out RectangleF mapBounds, out MapType mapType, out string errorMessageText)
        {
            scale = 0; dpi = 0;
            mapType = MapType.None;
            bitmapSize = new Size();
            string fileExtension = Path.GetExtension(mapFileName);

            if (string.Compare(fileExtension, ".pdf", StringComparison.InvariantCultureIgnoreCase) == 0) {
                if (ValidatePdf(mapFileName, out dpi, out bitmapSize, out errorMessageText) != null) {
                    mapType = MapType.PDF;
                    mapBounds = new RectangleF(0, 0, (float)bitmapSize.Width / dpi * 25.4F, (float) bitmapSize.Height / dpi * 25.4F);
                    return true;
                }
                else {
                    mapBounds = new RectangleF();
                    return false;
                }
            }

            Map map = new Map(TextMetricsProvider, new GDIPlus_FileLoader(Path.GetDirectoryName(mapFileName)));

            try {
                InputOutput.ReadFile(mapFileName, map);
            }
            catch (Exception e) {
                // Didn't load as an OCAD file. If it has a non-OCD/OpenMapper extension, try loading as an image.
                if ((string.Compare(fileExtension, ".ocd", StringComparison.InvariantCultureIgnoreCase) != 0) && 
                    (string.Compare(fileExtension, ".omap", StringComparison.InvariantCultureIgnoreCase) != 0) &&
                    (string.Compare(fileExtension, ".xmap", StringComparison.InvariantCultureIgnoreCase) != 0)) 
                {
                    try {
                        Bitmap bitmap = (Bitmap) Image.FromFile(mapFileName);
                        bitmapSize = bitmap.Size;
                        dpi = bitmap.HorizontalResolution;
                        bitmap.Dispose();
                        mapType = MapType.Bitmap;
                        mapBounds = new RectangleF(0, 0, (float)bitmapSize.Width / dpi * 25.4F, (float)bitmapSize.Height / dpi * 25.4F);
                        errorMessageText = "";
                        return true;
                    }
                    catch {
                        // Wasn't an bitmap file either.
                        errorMessageText = string.Format(MiscText.CannotReadImageFile, mapFileName);
                        mapBounds = new RectangleF();
                        return false;
                    }
                }

                if (string.Compare(fileExtension, ".ocd", StringComparison.InvariantCultureIgnoreCase) == 0) {
                    errorMessageText = string.Format(MiscText.CannotReadMap, e.Message);
                }
                else {
                    errorMessageText = string.Format(MiscText.CannotReadMapOOM, e.Message);
                }

                mapBounds = new RectangleF();
                return false;
            }

            using (map.Read())
            {
                scale = map.MapScale;
                mapBounds = map.Bounds;
            }

            errorMessageText = "";
            mapType = MapType.OCAD;
            return true;
        }

        public static PdfMapFile ValidatePdf(string pdfFileName, out float dpi, out Size bitmapSize, out string errorMessageText)
        {
            IPdfLoadingStatus loadingStatus = new PdfLoadingUI();  // UNDONE: Should this be passed in instead?

            PdfMapFile mapFile = new PdfMapFile(pdfFileName);

            bool ok = true;
            PdfMapFile.ConversionStatus status = mapFile.BeginConversion();
            if (status == PdfMapFile.ConversionStatus.Working) {
                // Put up a modal dialog until loading is complete.
                mapFile.ConversionCompleted += delegate { 
                    loadingStatus.LoadingComplete(mapFile.Status == PdfMapFile.ConversionStatus.Success, mapFile.ConversionOutput);
                };
                if (status == PdfMapFile.ConversionStatus.Working) {
                    ok = loadingStatus.ShowLoadingStatus(pdfFileName);
                }
            }

            status = mapFile.Status;
            if (!ok || status == PdfMapFile.ConversionStatus.Failure) {
                errorMessageText = MiscText.PdfConversionFailed;
                if (!string.IsNullOrWhiteSpace(mapFile.ConversionOutput))
                    errorMessageText += ": " + mapFile.ConversionOutput;
                dpi = 0;
                bitmapSize = default(Size);
                return null;
            }

            // Make sure resulting image file can be read.
            try {
                Bitmap bitmap = (Bitmap)Image.FromFile(mapFile.PngFileName);
                dpi = bitmap.HorizontalResolution;
                bitmapSize = bitmap.Size;
                bitmap.Dispose();
                errorMessageText = "";
                return mapFile;
            }
            catch {
                // Couldn't read the resulting PNG
                errorMessageText = string.Format(MiscText.PdfResultNotReadable, mapFile.PngFileName);
                dpi = 0;
                bitmapSize = default(Size);
                return null;
            }
        }

        public static ToolboxIcon CreateToolboxIcon(Bitmap bm) {
            ToolboxIcon icon = new ToolboxIcon();

            for (int x = 0; x < ToolboxIcon.WIDTH; ++x) {
                for (int y = 0; y < ToolboxIcon.HEIGHT; ++y) {
                    icon.SetPixel(x, y, bm.GetPixel(x, y));
                }
            }

            return icon;
        }

        // Given a print area rectangle, find the best default page size that encloses it, using either the default
        // metric or english paper sizes. If the rectangle is empty, return default page.
        public static void GetDefaultPageSize(RectangleF printAreaRectangle, float printScaleRatio, out int pageWidth, out int pageHeight, out int pageMargin, out bool landscape)
        {
            bool metric = RegionInfo.CurrentRegion.IsMetric;

            if (printAreaRectangle.IsEmpty) {
                PaperSize paperSize = StandardPaperSizes[metric ? DefaultMetricPaperSizeindex : DefaultEnglighPaperSizeIndex];
                pageWidth = paperSize.Width;
                pageHeight = paperSize.Height;
                pageMargin = 0;
                landscape = false;
            }
            else {
                landscape = printAreaRectangle.Width > printAreaRectangle.Height;
                // Get needed page width and height in 1/100 of inch.
                float printAreaWidth = (landscape ? printAreaRectangle.Height : printAreaRectangle.Width) / printScaleRatio * 100 / 25.4F;
                float printAreaHeight = (landscape ? printAreaRectangle.Width : printAreaRectangle.Height) / printScaleRatio * 100 / 25.4F;

                int firstIndex = metric ? 0 : FirstEnglishPaperSizeIndex;
                int endIndex = metric ? FirstEnglishPaperSizeIndex : StandardPaperSizes.Length;
                int bestIndex = -1;

                // Scan through all paper indexes to find the smallest paper that fits the area.
                for (int i = firstIndex; i < endIndex; ++i) {
                    if (StandardPaperSizes[i].Width > printAreaWidth && StandardPaperSizes[i].Height > printAreaHeight &&
                        (bestIndex == -1 || StandardPaperSizes[i].Width < StandardPaperSizes[bestIndex].Width))
                        bestIndex = i;
                }
                if (bestIndex < 0)
                    bestIndex = metric ? DefaultMetricPaperSizeindex : DefaultEnglighPaperSizeIndex;

                pageWidth = StandardPaperSizes[bestIndex].Width;
                pageHeight = StandardPaperSizes[bestIndex].Height;

                // Use the default margin if it can fit, otherwise 0 margin.
                int defaultMargin = metric ? DefaultMetricMargin : DefaultEnglishMargin;
                if (pageWidth - printAreaWidth > defaultMargin * 2 &&
                    pageHeight - printAreaHeight > defaultMargin * 2) {
                    pageMargin = defaultMargin;
                }
                else {
                    pageMargin = 0;
                }
            }
        }

        // Give a map file name, get the default print area.
        public static PrintArea GetDefaultPrintArea(string mapFileName, float printScaleRatio)
        {
            float scale, dpi;
            Size bitmapSize;
            RectangleF mapBounds;
            MapType mapType;
            string errorMessageText;

            // If this failes, mapBounds will be empty rectangle, which is what we want to pass to GetDefaultPageSize;
            ValidateMapFile(mapFileName, out scale, out dpi, out bitmapSize, out mapBounds, out mapType, out errorMessageText);

            PrintArea printArea = new PrintArea();
            printArea.autoPrintArea = true;
            printArea.restrictToPageSize = true;
            GetDefaultPageSize(mapBounds, printScaleRatio, out printArea.pageWidth, out printArea.pageHeight, out printArea.pageMargins, out printArea.pageLandscape);
            return printArea;

        }
    }

    interface IPdfLoadingStatus
    {
        bool ShowLoadingStatus(string fileName);
        void LoadingComplete(bool success, string errorMessage);
    }

    static class FindPurple
    {
        // All the names called purple in different languages.
        private static string[] purpleNames = 
            { "Purple" };

        // Determine if a color is actually some shade of purple.
        public static bool IsPurple(float cyan, float magenta, float yellow, float black)
        {
            float h, s, v;
            ColorConverter.CmykToHsv(cyan, magenta, yellow, black, out h, out s, out v);
            return (h >= 0.70 && h <= 0.95 && s >= 0.5 && v >= 0.50);
        }

        // Determine if a color is actually close to 100% green.
        public static bool IsSolidGreen(float cyan, float magenta, float yellow, float black)
        {
            float h, s, v;
            ColorConverter.CmykToHsv(cyan, magenta, yellow, black, out h, out s, out v);
            return (h >= 0.20 && h <= 0.45 && s >= 0.7 && v >= 0.50);
        }


        // Determine if a color is black. Not all 4 colors, but only black.
        public static bool IsBlack(float cyan, float magenta, float yellow, float black)
        {
            return (black > 0.95f && cyan < 0.05F && yellow < 0.05F && cyan < 0.05F);
        }

        // Search all colors for one that is closest to pure magenta.
        public static SymColor FindClosedToMagenta(List<SymColor> colors)
        {
            float c, m, y, k;
            double distance, minDistance = 1000;
            SymColor bestColor = null;

            foreach (SymColor color in colors) {
                color.GetCMYK(out c, out m, out y, out k);
                if (IsPurple(c, m, y, k)) {
                    distance = c * c + (m - 1) * (m - 1) + (y * y) + (k * k);
                    if (distance < minDistance) {
                        minDistance = distance;
                        bestColor = color;
                    }
                }
            }

            return bestColor;
        }

        // Search all the colors for a color called "Purple".
        public static bool FindPurpleColor(List<SymColor> colors, out short ocadId, out float cyan, out float magenta, out float yellow, out float black)
        {
            float c, m, y, k;

            // Search all colors for one names "Purple" (in any language).
            foreach (SymColor color in colors) {
                if (Array.IndexOf(purpleNames, color.Name) >= 0) {
                    color.GetCMYK(out c, out m, out y, out k);
                    if (IsPurple(c, m, y, k)) {
                        ocadId = color.OcadId;
                        cyan = c; magenta = m; yellow = y; black = k;
                        return true;
                    }
                }
            }

            // Search all colors for one that is closest to pure magenta.
            SymColor bestColor = FindClosedToMagenta(colors);

            if (bestColor != null) {
                bestColor.GetCMYK(out c, out m, out y, out k);
                ocadId = bestColor.OcadId;
                cyan = c; magenta = m; yellow = y; black = k;
                return true;
            }

            // Did not find purple. 
            ocadId = -1;
            cyan = 0; magenta = 0; yellow = 0; black = 0;
            return false;
        }

        // Return the ocadId of the best lower purple color. We choose
        // the next purple color below the top-most solid green color and black color. If there isn't one,
        // we return the color below the top-most solid green/black.
        public static int FindLowerPurple(List<SymColor> colors)
        {
            int greenIndex = -1;
            int blackIndex = -1;

            // First, find the top-most solid green.
            for (int i = colors.Count - 1; i >= 0; --i) {
                float c, m, y, k;
                colors[i].GetCMYK(out c, out m, out y, out k);
                if (IsSolidGreen(c, m, y, k)) {
                    greenIndex = i;
                    break;
                }
            }

            // First, find the top-most black.
            for (int i = colors.Count - 1; i >= 0; --i) {
                float c, m, y, k;
                colors[i].GetCMYK(out c, out m, out y, out k);
                if (IsBlack(c, m, y, k)) {
                    blackIndex = i;
                    break;
                }
            }

            int bottomOfBlackAndGreen;
            if (blackIndex >= 0 && greenIndex >= 0) 
                bottomOfBlackAndGreen = Math.Min(blackIndex, greenIndex);
            else if (blackIndex >= 0)
                bottomOfBlackAndGreen = blackIndex;
            else if (greenIndex >= 0)
                bottomOfBlackAndGreen = greenIndex;
            else
                bottomOfBlackAndGreen = -1;

            // Find the next purple color below the solid green and black.
            if (bottomOfBlackAndGreen > 0) {
                for (int i = bottomOfBlackAndGreen - 1; i >= 0; --i) {
                    float c, m, y, k;
                    colors[i].GetCMYK(out c, out m, out y, out k);
                    if (IsPurple(c, m, y, k))
                        return colors[i].OcadId;
                }

                // If there is no purple below the black, return the color 
                // just below the black.
                return colors[bottomOfBlackAndGreen - 1].OcadId;
            }
            else {
                // If there is no black (or black is the bottom color), return the
                // top.
                return colors[colors.Count - 1].OcadId;
            }
        }

        // Get the purple color to use for display, taking into account the user preferences in courseAppearance, the map loaded into the mapDisplay, 
        // and the default purple if none of those provide a color. MapDisplay and courseAppearance can be null, in which case they won't be used.
        public static void GetPurpleColor(MapDisplay mapDisplay, CourseAppearance courseAppearance, out short ocadId, out float cyan, out float magenta, out float yellow, out float black, out bool overprint)
        {
            overprint = (courseAppearance == null) ? true : (courseAppearance.purpleColorBlend == PurpleColorBlend.Blend);

            if (courseAppearance != null && !courseAppearance.useDefaultPurple) {
                // Use the purple from the course display.
                cyan = courseAppearance.purpleC;
                magenta = courseAppearance.purpleM;
                yellow = courseAppearance.purpleY;
                black = courseAppearance.purpleK;
                ocadId = NormalCourseAppearance.courseOcadId;
                return;
            }
            else if (mapDisplay != null && FindPurpleColor(mapDisplay.GetMapColors(), out ocadId, out cyan, out magenta, out yellow, out black)) {
                // FindPurpleColor found a purple to use.
                return;
            }
            else {
                // Use the program default.
                ocadId = NormalCourseAppearance.courseOcadId;
                cyan = NormalCourseAppearance.courseColorC;
                magenta = NormalCourseAppearance.courseColorM;
                yellow = NormalCourseAppearance.courseColorY;
                black = NormalCourseAppearance.courseColorK;
                return;
            }
        }
    }
}
