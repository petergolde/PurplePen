/* Copyright 2006-2026 Peter Golde
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 * this list of conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 *
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names of its
 * contributors may be used to endorse or promote products derived from this
 * software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using PurplePen.MapModel;
using PurplePen.Graphics2D;
using ColorConverter = PurplePen.Graphics2D.ColorConverter;

namespace PurplePen
{
    public static class MapUtil
    {
        public static ITextMetrics TextMetricsProvider = Services.TextMetricsProvider;

        // Get a list of print scales from a map scale.
        // Current algorithm: use 4000, 5000, 7500, 10000, 15000, plus the map scale itself.
        public static float[] PrintScaleList(float mapScale)
        {
            List<float> result = new List<float>(new float[] { 4000, 5000, 7500, 10000, 15000 });
            if (!result.Contains(mapScale))
                result.Add(mapScale);
            result.Sort();
            return result.ToArray();
        }




        // Given a print area rectangle, get the exact size needed to print it at the given scale ratio. Because there tend to be rounding errors
        // around the range of about 1/100 of a inch due to quantization, we check standard paper sizes to see if one is very very close.
        public static void GetExactPageSize(RectangleF printAreaRectangle, float printScaleRatio, out int pageWidth, out int pageHeight, out bool landscape)
        {
            const float standardSizeTolerance = 1F; // tolerance for matching standard paper sizes, in hundredths of an inch.
            landscape = printAreaRectangle.Width > printAreaRectangle.Height;

            // Get needed page width and height in 1/100 of inch.
            float printAreaWidth = (landscape ? printAreaRectangle.Height : printAreaRectangle.Width) / printScaleRatio * 100 / 25.4F;
            float printAreaHeight = (landscape ? printAreaRectangle.Width : printAreaRectangle.Height) / printScaleRatio * 100 / 25.4F;

            // See if we are very close to a standard paper size.
            foreach (PrintingPaperSize paperSize in PrintingStandards.StandardPaperSizes) {
                if (Math.Abs(paperSize.SizeInHundreths.Width - printAreaWidth) < standardSizeTolerance && Math.Abs(paperSize.SizeInHundreths.Height - printAreaHeight) < standardSizeTolerance) {
                    pageWidth = (int) Math.Round(paperSize.SizeInHundreths.Width);
                    pageHeight = (int) Math.Round(paperSize.SizeInHundreths.Height);
                    return;
                }
            }

            // Not close to a standard size, use exact size.
            pageWidth = (int) Math.Round(printAreaWidth);
            pageHeight = (int) Math.Round(printAreaHeight);
        }

    }

    public static class FindPurple
    {
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

        // Search all colors for one that is closest to the IO purple color.
        public static SymColor FindClosestToIofPurple(List<SymColor> colors)
        {
            float c, m, y, k;
            float cIOF = NormalCourseAppearance.courseColorC, mIOF = NormalCourseAppearance.courseColorM, yIOF = NormalCourseAppearance.courseColorY, kIOF = NormalCourseAppearance.courseColorK;
            double distance, minDistance = 1000;
            SymColor bestColor = null;

            foreach (SymColor color in colors) {
                color.GetCMYK(out c, out m, out y, out k);
                if (IsPurple(c, m, y, k)) {
                    distance = ((c - cIOF) * (c - cIOF)) + ((m - mIOF) * (m - mIOF)) + ((y - yIOF) * (y - yIOF)) + ((k - kIOF) * (k - kIOF));
                    if (distance < minDistance) {
                        minDistance = distance;
                        bestColor = color;
                    }
                }
            }

            return bestColor;
        }

        // Find the best purple color.
        public static bool FindPurpleColor(List<SymColor> colors, out short ocadId, out float cyan, out float magenta, out float yellow, out float black)
        {
            float c, m, y, k;

            // Search all colors for one that is closest to IOF definition of purple.
            SymColor bestColor = FindClosestToIofPurple(colors);

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
        // the next purple color below the top-most black color. If there isn't one,
        // we return the color below the top-most solid green/black.
        //
        // Returns the best OCAD ID, and a bool indicating whether the color is a lower purple
        // we found as expected.
        public static (int, bool) FindLowerPurpleHelper(List<SymColor> colors)
        {
            bool foundPurple;
            float cPurple, mPurple, yPurple, kPurple;

            // Get the best purple in the colors.
            foundPurple = FindPurpleColor(colors, out short _, out cPurple, out mPurple, out yPurple, out kPurple);

            if (foundPurple) {
                // Start at half way up the color chart, and find the lowest color that exactly matches the given purple.
                for (int i = colors.Count / 2; i < colors.Count; ++i) {
                    float c, m, y, k;
                    colors[i].GetCMYK(out c, out m, out y, out k);
                    if (c == cPurple && m == mPurple && y == yPurple && k == kPurple) {
                        // There must be a black, then another purple above this.
                        for (int j = i + 1; j < colors.Count; ++j) {
                            float c2, m2, y2, k2;
                            colors[j].GetCMYK(out c2, out m2, out y2, out k2);
                            if (IsBlack(c2, m2, y2, k2)) {
                                for (int l = j + 1; l < colors.Count; ++l) {
                                    float c3, m3, y3, k3;
                                    colors[l].GetCMYK(out c3, out m3, out y3, out k3);
                                    if (c3 == cPurple && m3 == mPurple && y3 == yPurple && k3 == kPurple) {
                                        return (colors[i].OcadId, true); // Return first purple found.
                                    }
                                }
                            }
                        }

                        break;
                    }
                }
            }

            // If that didn't work, we start at the top and find the top-most black, then solid green.
            int topGreenIndex = -1;
            int topBlackIndex = -1;

            for (int i = colors.Count - 1; i >= 0; --i) {
                float c, m, y, k;
                colors[i].GetCMYK(out c, out m, out y, out k);
                if (IsBlack(c, m, y, k)) {
                    topBlackIndex = i;
                    break;
                }
            }

            if (topBlackIndex > 0) { 
                for (int i = topBlackIndex - 1; i >= 0; --i) {
                    float c, m, y, k;
                    colors[i].GetCMYK(out c, out m, out y, out k);
                    if (IsSolidGreen(c, m, y, k)) {
                        topGreenIndex = i;
                        break;
                    }
                }
            }

            // Return the color just below that green.
            if (topGreenIndex > 0)
                return (colors[topGreenIndex - 1].OcadId, false);

            // Otherwise, just return top color.
            return (colors[colors.Count - 1].OcadId, false);
        }

        // Retur the OCAD ID of the best place to put lower purple than can be found. She always
        // be comfirmed by the user, because in some cases it might not be great.
        public static int FindBestLowerPurpleLayer(List<SymColor> colors)
        {
            int ocadID;
            (ocadID, _) = FindLowerPurpleHelper(colors);
            return ocadID;
        }

        // Find the lower purple layer if it exists, otherwise returns NULL. Will always
        // be a purple color.
        public static int? FindLowerPurpleIfPresent(List<SymColor> colors)
        {
            int ocadID;
            bool goodPurple;
            (ocadID, goodPurple) = FindLowerPurpleHelper(colors);

            if (goodPurple) {
                return ocadID;
            }
            else {
                return null;
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
