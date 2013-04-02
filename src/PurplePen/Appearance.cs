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

        // If the requested font is Arial Narrow, and it isn't installed, then the font is changed to Arial
        // and the height is changed to "emHeightNoArialNarrow".
        public FontDesc(string name, bool bold, bool italic, float emHeight, float emHeightNoArialNarrow)
        {
            this.Name = name;
            this.Bold = bold;
            this.Italic = italic;
            this.EmHeight = emHeight;

            if (name == "Arial Narrow" && !ArialNarrowInstalled) {
                this.Name = "Arial";
                this.EmHeight = emHeightNoArialNarrow;
            }
        }

        public FontStyle Style
        {
            get
            {
                FontStyle fontStyle = FontStyle.Regular;
                if (Bold)
                    fontStyle |= FontStyle.Bold;
                if (Italic)
                    fontStyle |= FontStyle.Italic;
                return fontStyle;
            }
        }

        public Font GetFont()
        {
            return new Font(Name, EmHeight, Style, GraphicsUnit.World);
        }

        public Font GetScaledFont(float scaleRatio)
        {
            return new Font(Name, EmHeight * scaleRatio, Style, GraphicsUnit.World);
        }

        private static bool checkedArialNarrow = false;
        private static bool installedArialNarrow;

        // Is the font "Arial Narrow" installed?
        public static bool ArialNarrowInstalled
        {
            get
            {
                if (!checkedArialNarrow) {
                    string[] commandLineArgs = Environment.GetCommandLineArgs();
                    if (Array.IndexOf(commandLineArgs, "/noarialnarrow") >= 0) {
                        // Override -- use /noarialnarrow on the command line and arial narrow won't be used.
                        checkedArialNarrow = true;
                        installedArialNarrow = false;
                    }
                    else {
                        // Test if Arial Narrow is installed.
                        try {
                            new FontFamily("Arial Narrow");
                            installedArialNarrow = true;
                        }
                        catch (ArgumentException) {
                            installedArialNarrow = false;
                        }
                        checkedArialNarrow = true;
                    }
                }

                return installedArialNarrow;
            }

            // This is for debugging purposes, so we can set this on/off in tests.
            set
            {
                checkedArialNarrow = true;
                installedArialNarrow = value;
            }
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
        public static readonly FontDesc titleFont = new FontDesc("Arial Narrow", true, false, 63F, 56F);

        // Font to use for the control number (column A)  
        public static readonly FontDesc columnAFont = new FontDesc("Arial", true, false, 63F);

        // Font to use for the code (column B)    
        public static readonly FontDesc columnBFont = new FontDesc("Arial Narrow", true, false, 63F, 56F);

        // Font to use for the dimensions (column F)
        public static readonly FontDesc columnFFont = new FontDesc("Arial", false, false, 50F);

        // Font to use for two dimensions (column F)
        public static readonly FontDesc columnFSmallFont = new FontDesc("Arial Narrow", false, false, 45F, 42F);

        // Font to use in directive lines
        public static readonly FontDesc directiveFont = new FontDesc("Arial Narrow", true, false, 63F, 56F);

        // Font to use for the text version of the description
        public static readonly FontDesc textFont = new FontDesc("Arial Narrow", false, false, 43F, 40F);

        // Font to use for the text in the custom symbol key
        public static readonly FontDesc keyFont = new FontDesc("Arial", false, false, 52F, 52F);

        // Font to use for other text lines.
        public static readonly FontDesc textLineFont = new FontDesc("Arial Narrow", true, false, 56F, 50F);

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
        public const float startRadius = 4.04F;
        public const float controlOutsideDiameter = 6.0F;  
        public const float finishOutsideDiameter = 7.0F;
        public const float centerDotDiameter = 0.0F;
        public const float crossingRadius = 2.5F;

        // Font to use for the control number in regular courses.
        // An Em Height of 5.57 yields the IOF specified 4mm for the height of a digit.
        public const float nominalControlNumberHeight = 4.0F;          // nominal height from top to bottom of a digit
        public const float controlNumberHeightFactor = 5.57F / 4.0F;  // scale factor from control number height to font EM size
        public static readonly FontDesc controlNumberFont = new FontDesc("Arial", false, false, controlNumberHeightFactor * nominalControlNumberHeight);
        public static readonly FontDesc controlNumberFontBold = new FontDesc("Arial", true, false, controlNumberHeightFactor * nominalControlNumberHeight);

        public const float controlNumberCircleDistance = 1.825F;   // default distance of control number from edge of control circle

        // Font to use for the code number in score courses and all controls.
        // Em height of 4.18 yields actual digit height of 3mm.
        public static readonly FontDesc controlCodeFont = new FontDesc("Arial Narrow", true, false, 4.18F, 4.00F);

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
        public const string courseColorName = "Purple";
        public const short courseOcadId = 11;
        public const float courseColorC = 0F;
        public const float courseColorM = 1.0F;
        public const float courseColorY = 0F;
        public const float courseColorK = 0F;

        // The color to use in the map display for all other controls (low intensity purple).
        public const string allControlsColorName = "Light Purple";
        public const short allControlsOcadId = 34;
        public const float allControlsColorC = 0.0F;
        public const float allControlsColorM = 0.5F;
        public const float allControlsColorY = 0F;
        public const float allControlsColorK = 0.0F;

        // The color used to for the selected item in the map display.
        public static readonly Color highlightColor = Color.FromArgb(255, 70, 0);

        // Brush to use to highlight areas.
        public static readonly Brush areaHighlight = new HatchBrush(HatchStyle.Percent25, NormalCourseAppearance.highlightColor, Color.Transparent);

        // The font used for text specials.
        public static string fontNameTextSpecial = "Arial";
        public static FontStyle fontStyleTextSpecial = FontStyle.Bold;
        public static float emHeightDefaultTextSpecial = 6F;    // default size when click instead of drag.
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
        public static readonly FontDesc titleFont = new FontDesc("Arial", true, false, 65F);

        // Font to use for the control number (upper-left corner)
        public static readonly FontDesc controlNumberFont = new FontDesc("Arial", true, false, 17F);

        // Font to use for the code (upper right corner)    
        public static readonly FontDesc codeFont = new FontDesc("Arial Narrow", false, false, 15F, 13F);

        // Font to use for the score (top middle)    
        public static readonly FontDesc scoreFont = new FontDesc("Arial", false, true, 17F);

        // Default format
        public const int defaultBoxesAcross = 8;
        public const int defaultBoxesDown = 3;
        public const bool defaultLeftToRight = true;
        public const bool defaultTopToBottom = false;
    }
}
