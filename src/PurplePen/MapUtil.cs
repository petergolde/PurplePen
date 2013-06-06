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

namespace PurplePen
{
    static class MapUtil
    {
        public static ITextMetrics TextMetricsProvider = new GDIPlus_TextMetrics();

        // Validate the map file to make sure it is readable. If OK, return true and set the scale.
        // If not OK, return false and set the error message.
        public static bool ValidateMapFile(string mapFileName, out float scale, out float dpi, out MapType mapType, out string errorMessageText)
        {
            scale = 0; dpi = 0;
            mapType = MapType.None;

            Map map = new Map(TextMetricsProvider, new GDIPlus_FileLoader(Path.GetDirectoryName(mapFileName)));

            try {
                InputOutput.ReadFile(mapFileName, map);
            }
            catch (Exception e) {
                // Didn't load as an OCAD file. If it has a non-OCD extension, try loading as an image.
                if (string.Compare(Path.GetExtension(mapFileName), ".ocd", true) != 0) {
                    try {
                        Bitmap bitmap = (Bitmap) Image.FromFile(mapFileName);
                        dpi = bitmap.HorizontalResolution;
                        bitmap.Dispose();
                        mapType = MapType.Bitmap;
                        errorMessageText = "";
                        return true;
                    }
                    catch {
                        // Wasn't an bitmap file either.
                        errorMessageText = string.Format(MiscText.CannotReadImageFile, mapFileName);
                        return false;
                    }
                }

                errorMessageText = string.Format(MiscText.CannotReadMap, e.Message);
                return false;
            }

            using (map.Read())
                scale = map.MapScale;

            errorMessageText = "";
            mapType = MapType.OCAD;
            return true;
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
            return (h >= 0.70 && h <= 0.95 && v >= 0.20);
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

        // Get the purple color to use for display, taking into account the user preferences in courseAppearance, the map loaded into the mapDisplay, 
        // and the default purple if none of those provide a color. MapDisplay and courseAppearance can be null, in which case they won't be used.
        public static void GetPurpleColor(MapDisplay mapDisplay, CourseAppearance courseAppearance, out short ocadId, out float cyan, out float magenta, out float yellow, out float black)
        {
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
