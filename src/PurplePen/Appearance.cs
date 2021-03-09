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
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PurplePen
{
    using PurplePen.Graphics2D;
    using PurplePen.MapModel;
    using System.IO;

    // Describes a particular font, without instanting a Font object.
    public struct FontDesc
    {
        public string Name;              // family name of the font
        public bool Bold, Italic;         // font style
        public float EmHeight;          // Em height of the font

        public FontDesc(string name, bool bold, bool italic, float emHeight)
            : this(name, bold, italic, emHeight, emHeight)
        {
        }

        // If the requested font is Roboto Condensed, and it isn't installed, then Arial Narrow is tried,
        // if that isn't installed then the font is changed to Arial
        // and the height is changed to "emHeightNoArialNarrow".
        // If the requested font is Roboto, and it isn't installed, then use Arial.
        public FontDesc(string name, bool bold, bool italic, float emHeight, float emHeightNoArialNarrow)
        {
            this.Name = name;
            this.Bold = bold;
            this.Italic = italic;
            this.EmHeight = emHeight;

            if (name == "Roboto Condensed" && !RobotoCondensedInstalled) {
                if (ArialNarrowInstalled) {
                    this.Name = "Arial Narrow";
                }
                else {
                    this.Name = "Arial";
                    this.EmHeight = emHeightNoArialNarrow;
                }
            }
            else if (name == "Roboto" && !RobotoInstalled) {
                this.Name = "Arial";
            }
        }

        public FontStyle Style {
            get {
                return Util.GetFontStyle(Bold, Italic);
            }
        }

        public TextEffects TextEffects {
            get {
                TextEffects effects = TextEffects.None;
                if (Bold)
                    effects |= TextEffects.Bold;
                if (Italic)
                    effects |= TextEffects.Italic;
                return effects;
            }
        }

        public Font GetFont()
        {
            return GdiplusFontLoader.CreateFont(Name, EmHeight, Style);
        }

        public Font GetScaledFont(float scaleRatio)
        {
            return GdiplusFontLoader.CreateFont(Name, EmHeight * scaleRatio, Style);
        }

        private static bool checkedArialNarrow = false;
        private static bool installedArialNarrow;
        private static bool checkedRoboto = false;
        private static bool installedRoboto;
        private static bool checkedRobotoCondensed = false;
        private static bool installedRobotoCondensed;

        private static bool IsInstalled(string fontName)
        {
            return GdiplusFontLoader.FontFamilyIsInstalled(fontName);
        }


        // Is the font "Arial Narrow" installed?
        public static bool ArialNarrowInstalled {
            get {
                if (!checkedArialNarrow) {
                    installedArialNarrow = IsInstalled("Arial Narrow");
                    checkedArialNarrow = true;
                }

                return installedArialNarrow;
            }

            // This is for debugging purposes, so we can set this on/off in tests.
            set {
                checkedArialNarrow = true;
                installedArialNarrow = value;
            }
        }

        // Is the font "Roboto Condensed" installed?
        public static bool RobotoCondensedInstalled {
            get {
                if (!checkedRobotoCondensed) {
                    installedRobotoCondensed = IsInstalled("Roboto Condensed");
                    checkedRobotoCondensed = true;
                }

                return installedRobotoCondensed;
            }

            // This is for debugging purposes, so we can set this on/off in tests.
            set {
                checkedRobotoCondensed = true;
                installedRobotoCondensed = value;
            }
        }

        // Is the font "Roboto" installed?
        public static bool RobotoInstalled {
            get {
                if (!checkedRoboto) {
                    installedRoboto = IsInstalled("Roboto");
                    checkedRoboto = true;
                }

                return installedRoboto;
            }

            // This is for debugging purposes, so we can set this on/off in tests.
            set {
                checkedRoboto = true;
                installedRoboto = value;
            }
        }

        // Initialize fonts to use fonts installed in the fonts subdirectory for the Roboto
        // fonts. Done on startup.
        public static void InitializeFonts()
        {
            Uri uri = new Uri(typeof(FontDesc).Assembly.CodeBase);
            string executablePath = Path.GetDirectoryName(uri.LocalPath);
            string fontPath = Path.Combine(executablePath, "fonts");

#if true
            GdiplusFontLoader.AddFontFile("Roboto", FontStyle.Regular, Path.Combine(fontPath, "Roboto-Regular.ttf"));
            GdiplusFontLoader.AddFontFile("Roboto", FontStyle.Bold, Path.Combine(fontPath, "Roboto-Bold.ttf"));
            GdiplusFontLoader.AddFontFile("Roboto", FontStyle.Italic, Path.Combine(fontPath, "Roboto-Italic.ttf"));
            GdiplusFontLoader.AddFontFile("Roboto", FontStyle.Bold | FontStyle.Italic, Path.Combine(fontPath, "Roboto-BoldItalic.ttf"));
            GdiplusFontLoader.AddFontFile("Roboto Condensed", FontStyle.Regular, Path.Combine(fontPath, "RobotoCondensed-Regular.ttf"));
            GdiplusFontLoader.AddFontFile("Roboto Condensed", FontStyle.Bold, Path.Combine(fontPath, "RobotoCondensed-Bold.ttf"));
            GdiplusFontLoader.AddFontFile("Roboto Condensed", FontStyle.Italic, Path.Combine(fontPath, "RobotoCondensed-Italic.ttf"));
            GdiplusFontLoader.AddFontFile("Roboto Condensed", FontStyle.Bold | FontStyle.Italic, Path.Combine(fontPath, "RobotoCondensed-BoldItalic.ttf"));
            // GdiplusFontLoader.AddFontFile(Path.Combine(fontPath, "Roboto-Black.ttf"));
            // GdiplusFontLoader.AddFontFile(Path.Combine(fontPath, "Roboto-BlackItalic.ttf"));
            // GdiplusFontLoader.AddFontFile(Path.Combine(fontPath, "Roboto-Medium.ttf"));
            // GdiplusFontLoader.AddFontFile(Path.Combine(fontPath, "Roboto-MediumItalic.ttf"));
            // GdiplusFontLoader.AddFontFile(Path.Combine(fontPath, "Roboto-Thin.ttf"));
            // GdiplusFontLoader.AddFontFile(Path.Combine(fontPath, "Roboto-ThinItalic.ttf"));
            // GdiplusFontLoader.AddFontFile(Path.Combine(fontPath, "Roboto-Light.ttf"));
            // GdiplusFontLoader.AddFontFile(Path.Combine(fontPath, "Roboto-LightItalic.ttf"));
            // GdiplusFontLoader.AddFontFile(Path.Combine(fontPath, "RobotoCondensed-Light.ttf"));
            // GdiplusFontLoader.AddFontFile(Path.Combine(fontPath, "RobotoCondensed-LightItalic.ttf"));
#endif
        }
    }

    // Class with constants that describe how a description should appear.
    static class DescriptionAppearance
    {
        // Holds the dimensions of various objects in the desciptions. All units are relative to the box size, 
        // where the box size is 100 units on a side.

        // Thickness of lines in the description.
        public const float thickDescriptionLine = 5.0F;
        public const float thinDescriptionLine = 2.5F;

        // Font to use for the title line(s).
        public static readonly FontDesc titleFont = new FontDesc("Roboto Condensed", true, false, 63F, 56F);

        // Font to use for the control number (column A)  
        public static readonly FontDesc columnAFont = new FontDesc("Roboto", true, false, 63F);

        // Font to use for the code (column B)    
        public static readonly FontDesc columnBFont = new FontDesc("Roboto Condensed", false, false, 63F, 56F);

        // Font to use for the dimensions (column F)
        public static readonly FontDesc columnFFont = new FontDesc("Roboto", false, false, 50F);

        // Font to use for two dimensions (column F)
        public static readonly FontDesc columnFSmallFont = new FontDesc("Roboto Condensed", false, false, 45F, 42F);

        // Font to use in directive lines
        public static readonly FontDesc directiveFont = new FontDesc("Roboto Condensed", true, false, 63F, 56F);

        // Font to use for the text version of the description
        public static readonly FontDesc textFont = new FontDesc("Roboto Condensed", false, false, 43F, 40F);

        // Font to use for the text in the custom symbol key
        public static readonly FontDesc keyFont = new FontDesc("Roboto", false, false, 52F, 52F);

        // Font to use for other text lines.
        public static readonly FontDesc textLineFont = new FontDesc("Roboto Condensed", true, false, 56F, 50F);

    }

    // Describes the normal default course appearance. Most items can be customized via a CourseAppearance object,
    // so use this directly with care.
    static class NormalCourseAppearance
    {
        // The dimensions of objects in the course display. The units are in map units (which is always mm).
        // Other coordinates are directly in CourseObj class which has the details of what various symbols look like.

        // Thickness of most lines in the map (circles, legs, start triangle, etc.)
        public const float lineThickness = 0.35F;

        // Distances that the leg line should go to.
        public const float startRadius2000 = 4.04F;              // Also Spr2019
        public const float startRadius2017 = 3.46F;
        public const float controlOutsideDiameter2000 = 6.0F;    // Also Spr2019
        public const float controlOutsideDiameter2017 = 5.35F;
        public const float finishOutsideDiameter2000 = 7.0F;     // Also Spr2019
        public const float finishOutsideDiameter2017 = 6.35F;
        public const float finishInsideDiameter2000 = 5.0F;      // Also Spr2019
        public const float finishInsideDiameter2017 = 4.35F;

        public const float centerDotDiameter = 0.0F;
        public const float crossingRadius = 2.5F;

        // Map issue point dimensions
        public const float mapIssueLength = 2.5F, mapIssueWidth = 0.6F;  // Map Issue point size.

        // Font to use for the control number in regular courses.
        // An Em Height of 5.57 yields the IOF specified 4mm for the height of a digit.
        public const float nominalControlNumberHeight = 4.0F;          // nominal height from top to bottom of a digit
        public const float controlNumberHeightFactor = 5.57F / 4.0F;  // scale factor from control number height to font EM size
        public static readonly FontDesc controlNumberFont = new FontDesc("Roboto", false, false, controlNumberHeightFactor * nominalControlNumberHeight);
        public static readonly FontDesc controlNumberFontBold = new FontDesc("Roboto", true, false, controlNumberHeightFactor * nominalControlNumberHeight);

        public const float controlNumberCircleDistance = 1.825F;   // default distance of control number from edge of control circle

        // Font to use for the code number in score courses and all controls.
        // Em height of 4.18 yields actual digit height of 3mm.
        public static readonly FontDesc controlCodeFont = new FontDesc("Roboto Condensed", true, false, 4.18F, 4.00F);

        // Font to use for the variation code number in topology view.
        // Em height of 4.18 yields actual digit height of 3mm.
        public static readonly FontDesc variationCodeFont = new FontDesc("Roboto Condensed", false, false, 4.18F, 4.00F);

        public const float codeCircleDistance = 0.325F;   // default distance of code from edge of control circle

        public const double defaultControlNumberAngle = Math.PI / 6;  // default angle of the control number absent constraints, in radian.

        // The color to use in the map display for the control descriptions (black).
        public const string blackColorName = "Black";
        public const short blackColorOcadId = 0;
        public const float blackColorC = 0F;
        public const float blackColorM = 0F;
        public const float blackColorY = 0F;
        public const float blackColorK = 1F;

        // The color to use in the map display for the course (purple).
        // Taken from ISOM 2017 Appendix 1 – CMYK Printing and Colour Definitions
        public const string courseColorName = "Purple";
        public const short courseOcadId = 11;
        public const float courseColorC = 0.35F;
        public const float courseColorM = 0.85F;
        public const float courseColorY = 0F;
        public const float courseColorK = 0F;

        // The color to use in the map display for all other controls (low intensity purple).
        public const string allControlsColorName = "Light Purple";
        public const short allControlsOcadId = 34;
        public const float allControlsColorC = 0.0F;
        public const float allControlsColorM = 0.5F;
        public const float allControlsColorY = 0F;
        public const float allControlsColorK = 0.0F;

        public const string extraCourseColorName = "Extra Course {0}";
        public const short extraCourseOcadId = 37;
        public static readonly float[] extraCourseC = { 0.00F, 0.00F, 0.75F, 1.00F, 0.00F, 0.45F, 0.00F, 0.80F, 0.15F, 0.55F };
        public static readonly float[] extraCourseM = { 0.70F, 0.25F, 0.55F, 0.00F, 0.00F, 0.00F, 0.65F, 0.25F, 0.80F, 0.75F };
        public static readonly float[] extraCourseY = { 1.00F, 1.00F, 0.00F, 0.50F, 1.00F, 0.40F, 0.60F, 0.00F, 0.45F, 0.15F };
        public static readonly float[] extraCourseK = { 0.00F, 0.35F, 0.00F, 0.00F, 0.50F, 0.40F, 0.25F, 0.25F, 0.15F, 0.00F };

        // The color used to for the selected item in the map display.
        public static readonly Color highlightColor = Color.FromArgb(255, 70, 0);

        // Brush to use to highlight areas.
        public static readonly Brush areaHighlight = new HatchBrush(HatchStyle.Percent25, NormalCourseAppearance.highlightColor, Color.Transparent);

        // The font used for text specials.
        public static string fontNameTextSpecial = "Roboto";
        public static FontStyle fontStyleTextSpecial = FontStyle.Bold;
        public static SpecialColor fontColorTextSpecial = SpecialColor.Purple;
        public static float emHeightDefaultTextSpecial = 6F;    // default size when click instead of drag.

        // Default options for line specials.
        public static SpecialColor lineSpecialColor = SpecialColor.Purple;
        public static LineKind lineSpecialKind = LineKind.Single;
        public const float lineSpecialWidth = 0.35F;
        public const float lineSpecialGapSize = 0.50F;
        public const float lineSpecialDashSize = 2.0F;
    }

    // Class with constants that describe how a punch card should appear.
    static class PunchcardAppearance
    {
        // Holds the dimensions of various objects in the desciptions. All units are relative to the box size, 
        // where the box size is 100 units on a side.

        public const int gridSize = 7;              // number of dots per side in the grid.

        // Thickness of lines in the punch card.
        public const float thickLine = 5.0F;
        public const float thinLine = 2.5F;

        // Radius of dots in the punch card.
        public const float dotRadius = 6F;

        // Font to use for the title line.
        public static readonly FontDesc titleFont = new FontDesc("Roboto", true, false, 65F);

        // Font to use for the control number (upper-left corner)
        public static readonly FontDesc controlNumberFont = new FontDesc("Roboto", true, false, 17F);

        // Font to use for the code (upper right corner)    
        public static readonly FontDesc codeFont = new FontDesc("Roboto Condensed", false, false, 15F, 13F);

        // Font to use for the score (top middle)    
        public static readonly FontDesc scoreFont = new FontDesc("Roboto", false, true, 17F);

        // Default format
        public const int defaultBoxesAcross = 8;
        public const int defaultBoxesDown = 3;
        public const bool defaultLeftToRight = true;
        public const bool defaultTopToBottom = false;
    }
}
