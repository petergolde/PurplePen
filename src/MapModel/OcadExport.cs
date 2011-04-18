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
using System.Text;
using System.Diagnostics;
using System.Globalization;
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using Matrix = System.Drawing.Drawing2D.Matrix;
using Color = System.Drawing.Color;

namespace PurplePen.MapModel {
    public class OcadExport {
        Map map;
        int version;
        BinaryWriter writer;
        string filename;
        bool useOcadSaved;
        Dictionary<SymDef, short> mapSymdefToSingleColor = new Dictionary<SymDef, short>();  // for OCAD 9, maps symdefs to a single color they use, if any. (-1 if not).

        // Convert from world coord (float in mm) to OCAD coords (int in units of .01 mm)
        static int ToOcadDimensions(float worldDimen) {
            return (int) Math.Round(((double)worldDimen) * 100.0);
        }

        // Convert from degrees to OCAD ang (0.1 degrees)
        short AngleToOcad(float degrees) {
            short angle = (short) Math.Round(degrees * 10.0F);
            return angle;
        }

        static OcadCoord OcadCoordFromPoint(PointF pt) {
            OcadCoord coord;
            coord.x = ToOcadDimensions(pt.X) << 8;
            coord.y = ToOcadDimensions(pt.Y) << 8;
            return coord;
        }

        // Get the color number of a particular color.
        short NumberOfColor(SymColor color) {
            return color.OcadId;
        }

        public OcadExport() {
        }

        // Write the given map to an OCAD file.
        //   version = 6,7,8 -- the OCAD version
        //   usedSavedOcadInfo utilizes symbol information that was saved away if this file was
        //     loaded from OCAD, for better round tripping. Setup structure information and 
        //     symbol header information is always used.
        public void WriteMap(Map map, string filename, int version, bool useSavedOcadInfo) {
            using (map.Read()) {
                this.map = map;
                this.version = version;
                this.filename = filename;
                this.useOcadSaved = useSavedOcadInfo;

                if (version < 6 || version > 10)
                    throw new ArgumentException("Bad version number", "version");

                OcadFileHeader fileHeader = new OcadFileHeader();

                using (writer = new BinaryWriter(new FileStream(filename, FileMode.Create, FileAccess.ReadWrite), Encoding.GetEncoding(1252))) {
                    fileHeader.OCADMark = 3245;

                    if (version == 6)
                        fileHeader.SectionMark = 0;
                    else if (version == 7)
                        fileHeader.SectionMark = 7;
                    else if (version == 8)
                        fileHeader.SectionMark = 2;
                    else
                        fileHeader.SectionMark = 0;

                    fileHeader.Version = (short) version;
                    fileHeader.Subversion = 0;
                    // other fields will be filled in later and re-written at the end
                
                    fileHeader.Write(writer);


                    if (version <= 8) {
                        WriteSymbolHeader();
                        fileHeader.SetupPos = WriteSetup(out fileHeader.SetupSize);
                    }

                    fileHeader.FirstSymBlk = WriteSymbols();
                    fileHeader.FirstIdxBlk = WriteObjects();

                    if (version >= 8)
                        fileHeader.FirstStIndexBlk = WriteStringParameters();

                    // rewrite the file header
                    writer.Seek(0, SeekOrigin.Begin);
                    fileHeader.Write(writer);
                }	
            }
        }

        int WriteSetup(out int size) {
            int pos = (int) writer.Seek(0, SeekOrigin.Current);

            OcadSetup setup = map.OcadSetupStructure;

            if (setup == null) {
                // Default value if no existing setup structure.
                setup = new OcadSetup();
                setup.rGridDist = 10;
                setup.MapScale = 15000;
                if (version < 8)
                    setup.DraftScaleX = setup.DraftScaleY = setup.MapScale;
                setup.PrintScale = setup.MapScale;
                setup.Zoom = 4;
            }

            // Value to always set in the setup structure.
            setup.FileName = filename;
            setup.MapScale = map.MapScale;
            if (map.PrintScale != 0)
                setup.PrintScale = map.PrintScale;
            else
                setup.PrintScale = map.MapScale;

            if (map.PrintArea != RectangleF.Empty) {
                // note: bottom and top switched due to Y-positive coord system.
                setup.PrLowerLeft = OcadCoordFromPoint(new PointF(map.PrintArea.Left, map.PrintArea.Top));
                setup.PrUpperRight = OcadCoordFromPoint(new PointF(map.PrintArea.Right, map.PrintArea.Bottom));
            }

            // Set template for if version is <= 7. Parameter strings used for version 8 and above.
            TemplateInfo template = map.Template;
            if (template != null && version <= 7) {
                bool isOcadFile = false;
                try {
                    using (Stream stm = new FileStream(template.absoluteFileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        isOcadFile = OcadImport.IsOcadFile(stm);
                    }
                }
                catch (IOException) {
                    // Couldn't read file, so use the extension to decide.
                    isOcadFile = Path.GetExtension(template.absoluteFileName) == ".ocd";
                }

                if (version == 7 && isOcadFile)
                    setup.TemplateEnabled = 2;
                else
                    setup.TemplateEnabled = 1;

                setup.DraftScaleX = setup.DraftScaleY = map.MapScale;
                setup.TemplateFileName = template.absoluteFileName;
                setup.TempOffset = OcadCoordFromPoint(template.centerPoint);
                setup.rTempAng = template.angle / 180.0 * Math.PI;
                setup.TempResol = (short) Math.Round(template.dpi);
                setup.HideTemp = (ushort) (template.visible ? 0 : 1);
            }

            setup.Write(writer, version);

            size = (int) writer.Seek(0, SeekOrigin.Current) - pos;
            return pos;
        }
 
        void WriteSymbolHeader() {
            OcadSymbolHeader symheader = map.OcadSymbolHeaderStructure;
            int i;
            
            if (symheader == null) {
                symheader = new OcadSymbolHeader();
                symheader.BlackAng = 450;
                symheader.BlackFreq = 1500;
                symheader.CyanAng = 150;
                symheader.CyanFreq = 1500;
                symheader.MagentaAng = 750;
                symheader.MagentaFreq = 1500;
                symheader.YellowAng = 0;
                symheader.YellowFreq = 1500;

                OcadColorSep emptyColorSep = new OcadColorSep();
                for (i = 0; i < symheader.aColorSep.Length; ++i)
                    symheader.aColorSep[i] = emptyColorSep;
            }

            ICollection<SymColor> colors = map.AllColors;

            symheader.nColors = (short) colors.Count;
            i = colors.Count - 1;
            foreach (SymColor color in colors) {
                OcadColorInfo colorInfo = new OcadColorInfo();
                colorInfo.ColorNum = color.OcadId;
                colorInfo.ColorName = color.Name;
                float cyan, magenta, yellow, black;
                color.GetCMYK(out cyan, out magenta, out yellow, out black);
                colorInfo.Color.cyan = (byte) Math.Round(200.0F * cyan);
                colorInfo.Color.magenta = (byte) Math.Round(200.0F * magenta);
                colorInfo.Color.yellow = (byte) Math.Round(200.0F * yellow);
                colorInfo.Color.black = (byte) Math.Round(200.0F * black);
                for (int j = 0; j < colorInfo.SepPercentage.Length; ++j)
                    colorInfo.SepPercentage[j] = 255;
                symheader.aColorInfo[i--] = colorInfo;
            }

            // Fill in the rest of the array with empty colors.
            OcadColorInfo emptyColor = new OcadColorInfo();
            i = colors.Count;
            while (i < symheader.aColorInfo.Length) {
                symheader.aColorInfo[i++] = emptyColor;
            }

            symheader.Write(writer);
        }

        // Write all symdefs, return the file pos of first symbol block.
        int WriteSymbols() {
            int firstSymbolBlock = 0;
            int offsetPrevBlock = 0;
            OcadSymbolBlock currBlock = new OcadSymbolBlock();
            int index, count, total;
            ICollection<SymDef> symdefs = map.AllSymdefs;

            total = symdefs.Count;
            index = 0; count = 0;
            foreach (SymDef symdef in symdefs) {
                ++count;

                if (!(symdef is GraphicsSymDef) && !(symdef is ImageSymDef)) {   // graphics symdefs and image symdefs are not written to the OCAD file.
                    currBlock.FilePos[index] = (int) writer.Seek(0, SeekOrigin.Current);
                    WriteSymbolDef(symdef);

                    ++index; 
                }

                if (index == 256 || count == total) {
                    index = 0;

                    int current = (int) writer.Seek(0, SeekOrigin.Current);
                    if (offsetPrevBlock == 0)
                        firstSymbolBlock = current;
                    else {
                        writer.Seek(offsetPrevBlock, SeekOrigin.Begin);
                        writer.Write(current);
                        writer.Seek(current, SeekOrigin.Begin);
                    }
                    currBlock.Write(writer);
                    offsetPrevBlock = current;
                    currBlock = new OcadSymbolBlock();
                }
            }
            Debug.Assert(count == total);

            return firstSymbolBlock;
        }

        void WriteSymbolDef(SymDef symdef) {
            if (symdef is LineSymDef) {
                WriteLineSymDef(symdef as LineSymDef);
            }
            else if (symdef is PointSymDef) {
                WritePointSymDef(symdef as PointSymDef);
            }
            else if (symdef is AreaSymDef) {
                WriteAreaSymDef(symdef as AreaSymDef);
            }
            else if (symdef is TextSymDef) {
                WriteTextSymDef(symdef as TextSymDef);
            }
            else {
                Debug.Fail("Unknownsymdef kind");
            }
        }

        // Convert a symdef ID into the file storage format. This is trival for the OCAD 9 format, but
        // tricky for OCAD 8 and below.
        int ConvertSymdefId(int ocadID)
        {
            if (version >= 9)
                return ocadID;
            else {
                int intPart = (ocadID / 1000);
                int fracPart = (ocadID % 1000);

                if (intPart >= 1000) {
                    // UNDONE: pick unused ID instead, and record it somehow. This is totally BOGUS!
                    intPart = 999;
                }
                if (fracPart >= 10) {
                    // UNDONE: pick unused ID instead, and record it somehow. This is totally BOGUS!
                    fracPart = 9;
                }
                return (intPart * 10) + fracPart;
            }
        }

        // Fill in common parts of the OcadSymbol class: except Size, Otp, SymTp, Extent
        void FillInCommonSymdef(OcadSymbol symbol, SymDef symdef) {
            symbol.Sym = ConvertSymdefId(symdef.OcadID);
            symbol.Selected = false;
            symbol.Status = 0;
            symbol.FilePos = 0;

            List<short> colorsUsed = new List<short>(2);

            foreach (SymColor symcolor in map.AllColors) {
                if (symdef.HasColor(symcolor)) {
                    int colorid = NumberOfColor(symcolor);
                    symbol.ColorSet[colorid / 8] |= (byte) (1 << (colorid % 8));

                    if (version >= 9) 
                        colorsUsed.Add((short) colorid);
                }
            }

            if (version >= 9) {
                symbol.nColors = (short) colorsUsed.Count;
                symbol.ColorsUsed = colorsUsed.ToArray();
                mapSymdefToSingleColor[symdef] = (colorsUsed.Count) == 1 ? colorsUsed[0] : (short) -1;     // if this symdef is single-color, save that away for use in writing the objects.
            }

            
            symbol.Description = symdef.Name;
            symbol.IconBits = ToolboxIconToOcadIcon(symdef.ToolboxImage, version);

            if (version == 8) 
                symbol.Flags |= 2;

            if (!map.IsSymdefVisible(symdef)) {
                // Symbol is hidden.
                symbol.Status = 2;
            }
        }

        short OcadLineStyle(LineStyle lineStyle)
        {
            switch (lineStyle) {
            case LineStyle.Rounded:
                return 1;
            case LineStyle.Mitered:
                return 4;
            case LineStyle.Beveled:
            case LineStyle.FlatRounded:           // shouldn't happen.
                return 0;
            default:
                return 1;
            }
        }

        short OcadTextAlignment(TextSymDefHorizAlignment horizAlign, TextSymDefVertAlignment vertAlign) {
            if (version <= 9) {
                // TODO: If we are saving a non-defalt vertical alginemtn in OCAD 9 or before, what is correct behavior?
                // TODO: probably to adjust the coordinates on save. Also should have a way that the user knows what is happening.
                switch (horizAlign) {
                    case TextSymDefHorizAlignment.Left: return 0;
                    case TextSymDefHorizAlignment.Center: return 1;
                    case TextSymDefHorizAlignment.Right: return 2;
                    case TextSymDefHorizAlignment.Justified: return 3;
                    default: return 0;
                }
            }
            else {
                switch (horizAlign) {
                    case TextSymDefHorizAlignment.Left:
                        switch (vertAlign) {
                            case TextSymDefVertAlignment.TopAscent: return 8;
                            case TextSymDefVertAlignment.Midpoint: return 4;
                            case TextSymDefVertAlignment.Baseline: return 0;
                            default: return 0;
                        }

                    case TextSymDefHorizAlignment.Right:
                        switch (vertAlign) {
                            case TextSymDefVertAlignment.TopAscent: return 10;
                            case TextSymDefVertAlignment.Midpoint: return 6;
                            case TextSymDefVertAlignment.Baseline: return 2;
                            default: return 2;
                        }
                    case TextSymDefHorizAlignment.Center:
                        switch (vertAlign) {
                            case TextSymDefVertAlignment.TopAscent: return 9;
                            case TextSymDefVertAlignment.Midpoint: return 5;
                            case TextSymDefVertAlignment.Baseline: return 1;
                            default: return 0;
                        }
                    case TextSymDefHorizAlignment.Justified:
                        return 3;
                    default:
                        return 0;
                }
            }
        }


        void WriteLineSymDef(LineSymDef symdef) {
            OcadLineSymbol symbol = new OcadLineSymbol();
            FillInCommonSymdef(symbol, symdef);

            symbol.Otp = 2;
            symbol.SymTp = 0;
            symbol.Extent = (short) ToOcadDimensions(symdef.MaxThickness / 2);
            symbol.LineColor = symdef.LineColor != null ? (short) NumberOfColor(symdef.LineColor) : (short)0;
            symbol.LineWidth = (short) ToOcadDimensions(symdef.LineThickness);
            symbol.DistFromStart = 0;
            symbol.DistToEnd = 0;
            symbol.LineEnds = (ushort) OcadLineStyle(symdef.MainLineStyle);
            
            LineSymDef.GlyphInfo[] glyphs = symdef.Glyphs;
            int startGlyphIndex = -1, endGlyphIndex = -1, cornerGlyphIndex = -1;
            int dashCenteredGlyphIndex = -1, gapCenteredGlyphIndex = -1, spacedGlyphIndex = -1, spacedOffsetGlyphIndex = -1;
            int primGlyphIndex = -1, secondaryGlyphIndex = -1;
            
            if (glyphs != null) {
                for (int i = 0; i < glyphs.Length; ++i) {
                    // UNDONE: give warning if more than one glyph of a certain type
                    switch (glyphs[i].location) {
                    case LineSymDef.GlyphLocation.Corners:			cornerGlyphIndex = i; break;
                    case LineSymDef.GlyphLocation.DashCenters:		dashCenteredGlyphIndex = i; break;
                    case LineSymDef.GlyphLocation.MiddleDashCenters:		secondaryGlyphIndex = i; break;
                    case LineSymDef.GlyphLocation.End:				endGlyphIndex = i; break;
                    case LineSymDef.GlyphLocation.GapCenters:		gapCenteredGlyphIndex = i; break;
                    case LineSymDef.GlyphLocation.Spaced: spacedGlyphIndex = i; break;
                    case LineSymDef.GlyphLocation.SpacedDecrease: spacedGlyphIndex = i; break;
                    case LineSymDef.GlyphLocation.SpacedOffset: spacedOffsetGlyphIndex = i; break;
                    case LineSymDef.GlyphLocation.Start: startGlyphIndex = i; break;
                    }
                }
            }
                                    
            LineSymDef.DashInfo dashInfo = new LineSymDef.DashInfo();
            if (symdef.IsDashed) {
                dashInfo = symdef.Dashes;
                symbol.MainLength = (short) ToOcadDimensions(dashInfo.dashLength);
                symbol.EndLength = (short) ToOcadDimensions(dashInfo.firstDashLength);
                symbol.MainGap = (short) ToOcadDimensions(dashInfo.gapLength);
                symbol.MinSym = (short)(dashInfo.minGaps - 1);
                symbol.SecGap = (short) (dashInfo.secondaryMiddleGaps > 0 ? ToOcadDimensions(dashInfo.secondaryMiddleLength) : 0);
                symbol.EndGap = (short) (dashInfo.secondaryEndGaps > 0 ? ToOcadDimensions(dashInfo.secondaryEndLength) : 0);
            }
            else {
                symbol.MainLength = symbol.EndLength = 0;
                symbol.MainGap = symbol.SecGap = 0;
                symbol.MinSym = 0;
            }

            // The primary glyph is the one centered on gaps.
            primGlyphIndex = gapCenteredGlyphIndex;

            if (spacedGlyphIndex >= 0 && glyphs[spacedGlyphIndex].location == LineSymDef.GlyphLocation.SpacedDecrease) {
                // decreasing symbol.
                symbol.MainLength = (short) ToOcadDimensions(glyphs[spacedGlyphIndex].distance);
                symbol.EndLength = (short) ToOcadDimensions(glyphs[spacedGlyphIndex].firstDistance);
                symbol.MinSym = (short) (glyphs[spacedGlyphIndex].minimum - 1);
                symbol.DecMode = glyphs[spacedGlyphIndex].decreaseBothEnds ? (ushort)2 : (ushort)1;
                symbol.DecLast = (short) Math.Round(glyphs[spacedGlyphIndex].decreaseLimit * 100);
                primGlyphIndex = spacedGlyphIndex;
            }
            else if (spacedGlyphIndex >= 0 && (!symdef.IsDashed || symdef.LineThickness == 0)) {
                // the spaced glyph is actually the primary one, since there is no dashes.
                symbol.MainLength = (short) ToOcadDimensions(glyphs[spacedGlyphIndex].distance);
                symbol.EndLength = (short) ToOcadDimensions(glyphs[spacedGlyphIndex].firstDistance);
                symbol.MainGap = 0;
                symbol.SecGap = 0;
                symbol.MinSym = (short) (glyphs[spacedGlyphIndex].minimum - 1);
                primGlyphIndex = spacedGlyphIndex;
            }
            else if (spacedOffsetGlyphIndex >= 0 && (!symdef.IsDashed || symdef.LineThickness == 0)) {
                // the spaced offset glyph is actually the primary one, since there is no dashes.
                symbol.MainLength = (short) ToOcadDimensions(glyphs[spacedOffsetGlyphIndex].distance);
                symbol.EndLength = (short) ToOcadDimensions(glyphs[spacedOffsetGlyphIndex].firstDistance);
                symbol.MainGap = 0;
                symbol.SecGap = 0;
                symbol.MinSym = (short) (glyphs[spacedOffsetGlyphIndex].minimum - 1);
            }
            else if (dashCenteredGlyphIndex >= 0 && gapCenteredGlyphIndex == -1 && symdef.IsDashed && dashInfo.secondaryMiddleGaps == 0 && dashInfo.secondaryEndGaps == 0) {
                // Use the secondary gaps to represent the primary gaps.
                symbol.MinSym += 1;
                symbol.MainLength += symbol.MainGap;
                symbol.SecGap = symbol.MainGap;
                symbol.MainGap = 0;
                symbol.EndLength = (short) ((symbol.MainLength + 1) / 2);
                primGlyphIndex = dashCenteredGlyphIndex;
            }				

            if (symdef.HasSecondLine) {
                symbol.FrColor = (short) NumberOfColor(symdef.SecondLineColor);
                symbol.FrWidth = (short) ToOcadDimensions(symdef.SecondThickness);
                symbol.FrStyle =  OcadLineStyle(symdef.SecondLineStyle);
            }
            
            if (symdef.IsDoubleLine) {
                LineSymDef.DoubleLineInfo doubleInfo = symdef.DoubleLines;

                symbol.DblMode = 1;  // may be changed below.
                symbol.DblWidth = (short) ToOcadDimensions(doubleInfo.doubleThick);

                if (doubleInfo.doubleFillColor != null) {
                    symbol.DblFlags |= 1;
                    symbol.DblFillColor = NumberOfColor(doubleInfo.doubleFillColor);
                }

                if (doubleInfo.doubleLeftColor != null) {
                    symbol.DblLeftColor = NumberOfColor(doubleInfo.doubleLeftColor);
                    symbol.DblLeftWidth = (short) ToOcadDimensions(doubleInfo.doubleLeftWidth);
                }
                if (doubleInfo.doubleRightColor != null) {
                    symbol.DblRightColor = NumberOfColor(doubleInfo.doubleRightColor);
                    symbol.DblRightWidth = (short) ToOcadDimensions(doubleInfo.doubleRightWidth);
                }

                if (doubleInfo.doubleLeftDashed || doubleInfo.doubleRightDashed || doubleInfo.doubleFillDashed) {
                    symbol.DblLength = (short) ToOcadDimensions(doubleInfo.doubleDashes.dashLength);
                    symbol.DblGap = (short) ToOcadDimensions(doubleInfo.doubleDashes.gapLength);

                    if (doubleInfo.doubleFillDashed)
                        symbol.DblMode = 4;
                    else if (doubleInfo.doubleRightDashed)
                        symbol.DblMode = 3;
                    else
                        symbol.DblMode = 2;
                }
            }
            
            if (symdef.IsShortened) {
                LineSymDef.ShortenInfo shortening = symdef.Shortening;

                symbol.DistFromStart = (short) ToOcadDimensions(shortening.shortenBeginning);
                symbol.DistToEnd = (short) ToOcadDimensions(shortening.shortenEnd);
                if (shortening.pointyEnds)
                    symbol.LineEnds |= 2;
            }
            
            if (primGlyphIndex >= 0) {
                LineSymDef.GlyphInfo glyph = glyphs[primGlyphIndex];
                symbol.nPrimSym = (short) glyph.number;
                symbol.PrimSymDist = (short) ToOcadDimensions(glyph.spacing);
                symbol.PrimDElts = SymbolEltsFromGlyph(glyph.glyph, out symbol.PrimDSize);
            }
            if (spacedOffsetGlyphIndex >= 0)  // UNDONE: if both secondaryGlyphIndex and spacedOffset, we can only represent one in OCAD.
                symbol.SecDElts = SymbolEltsFromGlyph(glyphs[spacedOffsetGlyphIndex].glyph, out symbol.SecDSize);
            if (secondaryGlyphIndex >= 0) 
                symbol.SecDElts = SymbolEltsFromGlyph(glyphs[secondaryGlyphIndex].glyph, out symbol.SecDSize);
            if (startGlyphIndex >= 0) 
                symbol.StartDElts = SymbolEltsFromGlyph(glyphs[startGlyphIndex].glyph, out symbol.StartDSize);
            if (endGlyphIndex >= 0) 
                symbol.EndDElts = SymbolEltsFromGlyph(glyphs[endGlyphIndex].glyph, out symbol.EndDSize);
            if (cornerGlyphIndex >= 0) 
                symbol.CornerDElts = SymbolEltsFromGlyph(glyphs[cornerGlyphIndex].glyph, out symbol.CornerDSize);

            symbol.Write(writer, version);
        }

        void WritePointSymDef(PointSymDef symdef) {
            OcadPointSymbol symbol = new OcadPointSymbol();
            FillInCommonSymdef(symbol, symdef);

            symbol.Otp = 1;
            symbol.SymTp = 0;
            symbol.Extent = (short) ToOcadDimensions(symdef.Radius);
            symbol.symbolElts = SymbolEltsFromGlyph(symdef.Glyph, out symbol.DataSize);

            if (symdef.AllowRotation)
                symbol.Flags |= 1;

            symbol.Write(writer, version);
        }

        void WriteAreaSymDef(AreaSymDef symdef) {
            OcadAreaSymbol symbol = new OcadAreaSymbol();
            FillInCommonSymdef(symbol, symdef);

            symbol.Otp = 3;
            symbol.SymTp = 0;
            symbol.Extent = 0;
            symbol.FillOn = symdef.FillColor != null;
            if (symdef.FillColor != null)
                symbol.FillColor = NumberOfColor(symdef.FillColor);

            if (symdef.BorderSymdef != null) {
                // A border symbol. Supported directly in OCAD 9. Must be emulated by writing additional objects in 6-8,
                // which is handled elsewhere.
                if (version >= 9) {
                    symbol.BorderOn = true;
                    symbol.BorderSym = ConvertSymdefId(symdef.BorderSymdef.OcadID);
                }
            }

            int hatchMode;
            SymColor hatchColor;
            float hatchWidth, hatchSpacing, angle1, angle2;

            symdef.GetHatching(out hatchMode, out hatchColor, out hatchWidth, out hatchSpacing, out angle1, out angle2);
            symbol.HatchMode = (short) hatchMode;
            if (hatchMode > 0) {
                symbol.HatchColor = NumberOfColor(hatchColor);
                symbol.HatchLineWidth = (short) ToOcadDimensions(hatchWidth);
                if (version >= 9)
                    symbol.HatchDist = (short) ToOcadDimensions(hatchSpacing);
                else
                    symbol.HatchDist = (short) ToOcadDimensions(hatchSpacing - hatchWidth);
                symbol.HatchAngle1 = AngleToOcad(angle1);
                symbol.HatchAngle2 = AngleToOcad(angle2);
            }

            bool drawPattern, offsetRows;
            float width, height, angle;
            Glyph glyph;
            symdef.GetPattern(out drawPattern, out offsetRows, out width, out height, out angle, out glyph);

            if (drawPattern) {
                symbol.StructMode = (short) (offsetRows ? 2 : 1);
                symbol.StructWidth = (short) ToOcadDimensions(width);
                symbol.StructHeight = (short) ToOcadDimensions(height);
                symbol.StructAngle = AngleToOcad(angle);
                symbol.StructElts = SymbolEltsFromGlyph(glyph, out symbol.DataSize);
            }

            symbol.Write(writer, version);
        }

        void WriteTextSymDef(TextSymDef symdef) {
            OcadTextSymbol symbol = new OcadTextSymbol();
            FillInCommonSymdef(symbol, symdef);

            symbol.Otp = 4;
            symbol.SymTp = 1;

            if (symdef.FontColor != null)
                symbol.FontColor = NumberOfColor(symdef.FontColor);
            symbol.Italic = symdef.Italic;
            if (symdef.Bold)
                symbol.Weight = 700;
            else
                symbol.Weight = 400;
            symbol.Alignment = OcadTextAlignment(symdef.FontAlignment, symdef.VertAlignment);
            symbol.FontSize = (short) Math.Round(symdef.FontEmHeight / 25.4F * 720F);
            symbol.FontName = symdef.FontName;
            symbol.LineSpace = (short) Math.Round(symdef.LineSpacing * 100F / symdef.FontEmHeight);
            if (symdef.Underline.underlineOn)
                symbol.ParaSpace = (short) ToOcadDimensions(symdef.ParaSpacing - symdef.Underline.underlineDistance - symdef.Underline.underlineWidth);
            else
                symbol.ParaSpace = (short) ToOcadDimensions(symdef.ParaSpacing);
            symbol.IndentFirst = (short) ToOcadDimensions(symdef.FirstIndent);
            symbol.IndentOther = (short) ToOcadDimensions(symdef.RestIndent);
            symbol.CharSpace = (short) Math.Round(symdef.CharSpacing * 100F);
            symbol.WordSpace = (short) Math.Round(symdef.WordSpacing * 100F);

            float[] tabs = symdef.Tabs;
            if (tabs != null && tabs.Length > 0) {
                symbol.nTabs = (short) Math.Min(32, tabs.Length);
                for (int i = 0; i < symbol.nTabs; ++i)
                    symbol.Tabs[i] = (short) ToOcadDimensions(tabs[i]);
            }

            WriteFraming(symdef, out symbol.FrMode, out symbol.FrFlags, out symbol.FrName, out symbol.FrColor, out symbol.FrWidth, 
                                  out symbol.FrSize, out symbol.FrWeight, out symbol.FrItalic, out symbol.FrOfX, out symbol.FrOfY, out symbol.FrLeft, out symbol.FrTop, out symbol.FrRight, out symbol.FrBottom);

            TextSymDef.Underlining underline = symdef.Underline;
            if (underline.underlineOn) {
                symbol.LBOn = true;
                symbol.LBColor = NumberOfColor(underline.underlineColor);
                symbol.LBDist = (short) ToOcadDimensions(underline.underlineDistance);
                symbol.LBWidth = (short) ToOcadDimensions(underline.underlineWidth);
            }

            if (symdef.CenterPointSymdef != null) {
                // A center point symbol. Supported directly in OCAD 10. 
                // UNDONE: must be emulated by writing additional objects in 6-9, which should be handled elsewhere.
                if (version >= 10) {
                    symbol.PointSymOn = true;
                    symbol.PointSym = ConvertSymdefId(symdef.CenterPointSymdef.OcadID);
                }
            }

            symbol.Write(writer, version);
        }

        // Set the framing parts.
        private void WriteFraming(TextSymDef symdef, out byte ocadFrMode, out byte ocadFrFlags, out string ocadFrName, out short ocadFrColor, out short ocadFrWidth, out short ocadFrSize, 
                                                  out short ocadFrWeight, out bool ocadFrItalic, out short ocadFrOfX, out short ocadFrOfY, out short ocadFrLeft, out short ocadFrTop, out short ocadFrRight, out short ocadFrBottom)
        {
            TextSymDef.Framing framing = symdef.FramingInfo;
            ocadFrMode = ocadFrFlags = 0;
            ocadFrColor = ocadFrWidth = ocadFrSize = ocadFrOfX = ocadFrOfY =
                                     ocadFrLeft = ocadFrTop = ocadFrRight = ocadFrBottom = 0;
            ocadFrWeight = 400;
            ocadFrItalic = false;
            ocadFrName = "";

            switch (framing.framingStyle) {
            case TextSymDef.FramingStyle.None:
                break;

            case TextSymDef.FramingStyle.Line:
                if (version >= 7) {
                    ocadFrMode = 2;
                    if (version == 7)
                        ocadFrWidth = (short) ToOcadDimensions(framing.lineWidth);
                    else
                        ocadFrSize = (short) ToOcadDimensions(framing.lineWidth);
                    ocadFrColor = NumberOfColor(framing.framingColor);
                    if (framing.lineStyle == LineStyle.Rounded)
                        ocadFrFlags = 1;
                    else if (framing.lineStyle == LineStyle.Beveled)
                        ocadFrFlags = 2;
                    else
                        ocadFrFlags = 3;
                }
                break;

            case TextSymDef.FramingStyle.Shadow:
                if (version >= 9) {
                    ocadFrMode = 1;
                    ocadFrColor = NumberOfColor(framing.framingColor);
                    ocadFrOfX = (short) ToOcadDimensions(framing.shadowX);
                    ocadFrOfY = (short) ToOcadDimensions(framing.shadowY);
                }
                else if (version <= 7) {
                    ocadFrMode = 1;
                    ocadFrColor = NumberOfColor(framing.framingColor);
                    ocadFrOfX = (short) ToOcadDimensions(framing.shadowX);
                    ocadFrOfY = (short) ToOcadDimensions(framing.shadowY);
                    ocadFrName = symdef.FontName;
                    ocadFrItalic = symdef.Italic;
                    ocadFrWeight = (short) (symdef.Bold ? 700 : 400);
                    ocadFrSize = (short) Math.Round(symdef.FontEmHeight / 25.4F * 720F);
                }
                break;

            case TextSymDef.FramingStyle.Rectangle:
                if (version >= 9) {
                    ocadFrMode = 3;
                    ocadFrColor = NumberOfColor(framing.framingColor);
                    ocadFrLeft = (short) ToOcadDimensions(framing.rectBorderLeft);
                    ocadFrTop = (short) ToOcadDimensions(framing.rectBorderTop);
                    ocadFrRight = (short) ToOcadDimensions(framing.rectBorderRight);
                    ocadFrBottom = (short) ToOcadDimensions(framing.rectBorderBottom);
                }
                break;
            }
        }


        int NearestOcadColor(Color col, bool use8bit) {
            Color[] colorMap = use8bit ? OcadConstants.ocadColorMap8Bit : OcadConstants.ocadColorMap4Bit;

            // Search Ocad Color Map for closest color
            int bestIndex = 0;
            double bestDist = double.PositiveInfinity;
            for (int index = 0; index < colorMap.Length; ++index) {
                double dist = Util.ColorDistance(col, colorMap[index]);
                if (dist < bestDist) {
                    bestIndex = index; bestDist = dist;
                }
            }

            return bestIndex;
        }


        // Convert an ToolboxIcon into an OCAD icon bits.
        unsafe byte[] ToolboxIconToOcadIcon(ToolboxIcon toolboxIcon, int version) {
            if (version <= 7) {
                // ocad 6, 7 -- 4 bit color
                byte[] array = new byte[264];

                for (int row = 1; row <= 22; ++row)
                    for (int col = 1; col <= 22; col += 2) {
                        int colorIndex1 = NearestOcadColor(toolboxIcon.GetPixel(col, row), false);
                        int colorIndex2 = NearestOcadColor(toolboxIcon.GetPixel(col + 1, row), false);
                        array[(22 - row) * 12 + (col - 1) / 2] = (byte) ((colorIndex1 << 4) + colorIndex2);
                    }

                return array;
            }
            else {
                // ocad 8/9  -- 8 bit color
                byte[] uncompressed = new byte[22 * 22];
                for (int row = 1; row <= 22; ++row)
                    for (int col = 1; col <= 22; ++col) {
                        int colorIndex = NearestOcadColor(toolboxIcon.GetPixel(col, row), true);
                        uncompressed[(22-row) * 22 + (col - 1)] = colorIndex > 128 ? (byte)0 : (byte) colorIndex;
                    }

                if (version == 8) {
                    // OCAD 8 -- compressed.

                    byte[] array = new byte[264];
                    byte[] compressed = new byte[264 - 16];
                    for (int i = 0; i < compressed.Length; ++i)
                        compressed[i] = 0xFF;

                    LZWCompression compressor = new LZWCompression();
                    compressor.Compress(uncompressed, compressed);

                    Array.Copy(compressed, 0, array, 16, compressed.Length);
                    for (int i = 0; i < 16; ++i)
                        array[i] = 0xFF;

                    return array;
                }
                else {
                    // OCAD 9 -- uncompressed
                    return uncompressed;
                }
            }
        }

        OcadSymbolElt[] SymbolEltsFromGlyph(Glyph glyph, out short datasize) {
            Glyph.GlyphPart[] parts = glyph.GetParts();
            OcadSymbolElt[] elts = new OcadSymbolElt[parts.Length];

            datasize = 0;

            for (int i = 0; i < parts.Length; ++i) {
                elts[i] = SymbolEltFromGlyphPart(parts[i]);
                datasize += (short) (2 + elts[i].stnPoly);
            }

            return elts;
        }

        OcadSymbolElt SymbolEltFromGlyphPart(Glyph.GlyphPart part) {
            OcadSymbolElt elt = new OcadSymbolElt();

            elt.stColor = NumberOfColor(part.color);

            switch (part.kind) {
            case GlyphPartKind.Area:
                elt.stType = 2;
                elt.stCoords = CoordsFromSymPathWithHoles(part.areaPath);
                break;

            case GlyphPartKind.Line:
                elt.stType = 1;
                elt.stFlags = (ushort) OcadLineStyle(part.lineStyle);
                elt.stLineWidth = (short) ToOcadDimensions(part.lineWidth);
                elt.stCoords = CoordsFromSymPath(part.path);
                break;

            case GlyphPartKind.Circle:
                elt.stType = 3;
                elt.stLineWidth = (short) ToOcadDimensions(part.lineWidth);
                if (version > 8)
                    elt.stDiameter = (short) ToOcadDimensions(part.circleDiam - part.lineWidth);
                else
                    elt.stDiameter = (short) ToOcadDimensions(part.circleDiam);
                elt.stCoords = new OcadCoord[1] { OcadCoordFromPoint(part.point) };
                break;

            case GlyphPartKind.FilledCircle:
                elt.stType = 4;
                elt.stDiameter = (short) ToOcadDimensions(part.circleDiam);
                elt.stCoords = new OcadCoord[1] { OcadCoordFromPoint(part.point) };
                break;

            default:
                Debug.Fail("bad GlyphPartKind");
                break;
            }

            elt.stnPoly = (short) elt.stCoords.Length;
            return elt;
        }

        OcadCoord[] CoordsFromSymPath(SymPath path) {
            PointF[] points = path.Points;
            PointKind[] kinds = path.PointKinds;
            byte[] startStopFlags = path.StartStopFlags;
            int length = points.Length;
            if (path.LastPointSynthesized)
                length -= 1;

            OcadCoord[] coords = new OcadCoord[length];

            for (int i = 0; i < length; ++i) {
                coords[i] = OcadCoordFromPoint(points[i]);

                switch (kinds[i]) {
                case PointKind.Normal:			
                    break;
                case PointKind.Dash:			
                    if (version >= 7) coords[i].y |= 8; 
                    break;
                case PointKind.Corner:			
                    coords[i].y |= 1; break;
                case PointKind.BezierControl:	
                    if (i > 0 && kinds[i-1] == PointKind.BezierControl)
                        coords[i].x |= 2;
                    else
                        coords[i].x |= 1;
                    break;
                }

                if (startStopFlags != null && i < startStopFlags.Length) {
                    if ((startStopFlags[i] & SymPath.DOUBLE_LEFT_STARTSTOPFLAG) != 0)
                        coords[i].x |= 4;
                    if ((startStopFlags[i] & SymPath.DOUBLE_RIGHT_STARTSTOPFLAG) != 0)
                        coords[i].y |= 4;
                    if ((startStopFlags[i] & SymPath.MAIN_STARTSTOPFLAG) != 0)
                        coords[i].x |= 8;
                }
            }

            return coords;
        }

        OcadCoord[] CoordsFromSymPathWithHoles(SymPathWithHoles path) {
            OcadCoord[] firstCoords = CoordsFromSymPath(path.MainPath);

            SymPath[] holes = path.Holes;

            if (holes == null)
                return firstCoords;

            int totalLength = firstCoords.Length;

            OcadCoord[][] holeCoords = new OcadCoord[holes.Length][];
            for (int i = 0; i < holes.Length; ++i) {
                holeCoords[i] = CoordsFromSymPath(holes[i]);
                totalLength += holeCoords[i].Length;
            }

            OcadCoord[] fullCoords = new OcadCoord[totalLength];
            Array.Copy(firstCoords, 0, fullCoords, 0, firstCoords.Length);
            totalLength = firstCoords.Length;
            for (int i = 0; i < holes.Length; ++i) {
                Array.Copy(holeCoords[i], 0, fullCoords, totalLength, holeCoords[i].Length);
                fullCoords[totalLength].y |= 2; // mark beginning of hole.
                totalLength += holeCoords[i].Length;
            }

            return fullCoords;
        }

        // Write all object, return the file pos of first index block.
        int WriteObjects() {
            int firstIndexBlock = 0;
            int offsetPrevBlock = 0;
            OcadIndexBlock currBlock = new OcadIndexBlock();
            int index, count, total;
            ICollection<Symbol> symbols = map.AllSymbols;

            total = symbols.Count;
            index = 0; count = 0;
            foreach (Symbol sym in symbols) {
                if (version < 9 && (sym is GraphicsAreaSymbol || sym is GraphicsLineSymbol))
                    continue;     // graphics objects not supported in OCAD 6-8.

                OcadIndex ocadIndex = new OcadIndex();

                ocadIndex.Pos = (int) writer.Seek(0, SeekOrigin.Current);
                RectangleF bounds = sym.BoundingBox;
                ocadIndex.LowerLeft = OcadCoordFromPoint(new PointF(bounds.Left, bounds.Top));
                ocadIndex.UpperRight = OcadCoordFromPoint(new PointF(bounds.Right, bounds.Bottom));
                ocadIndex.Sym = ConvertSymdefId(sym.Definition.OcadID);

                OcadObject ocadObj;
                int nItems = WriteSymbol(sym, out ocadObj);

                if (version >= 9) {
                    ocadIndex.ObjType = ocadObj.Otp;
                    ocadIndex.Status = 1;
                    ocadIndex.ViewType = 0;
                    if (version >= 9 && ocadObj.Sym == -2)
                        ocadIndex.Color = (short) ocadObj.Col;      // graphics object.
                    else if (version >= 9 && ocadObj.Sym == -3)
                        ocadIndex.Color = 0;      // image object.
                    else 
                        ocadIndex.Color = mapSymdefToSingleColor[sym.Definition];
                    ocadIndex.ImpLayer = 0;
                }

                // UNDONE: throw exception if length of object is too long.
                if (version == 8)
                    ocadIndex.Len = (short) nItems;
                else if (version >= 9)
                    ocadIndex.Len = (short) (40 + 8 * nItems);
                else
                    ocadIndex.Len = (short) (32 + 8 * nItems);

                currBlock.IndexArr[index] = ocadIndex;

                ++index; ++count;
                if (index == 256 || count == total) {
                    index = 0;
                    int current = (int) writer.Seek(0, SeekOrigin.Current);
                    if (offsetPrevBlock == 0) 
                        firstIndexBlock = current;
                    else {
                        writer.Seek(offsetPrevBlock, SeekOrigin.Begin);
                        writer.Write(current);
                        writer.Seek(current, SeekOrigin.Begin);
                    }
                    currBlock.Write(writer, version);
                    offsetPrevBlock = current;

                    // reset for next block.
                    currBlock.NextBlock = 0;
                    for (int i = 0; i < currBlock.IndexArr.Length; ++i)
                        currBlock.IndexArr[i] = new OcadIndex();
                }
            }

            return firstIndexBlock;
        }

        OcadCoord[] GetTextObjectCoords(TextSymbol sym) {
            TextSymDef symdef = ((TextSymDef) sym.Definition);
            PointF location = sym.Location;
            SizeF size = sym.TextSize;
            float angle = sym.Rotation;
            float width = sym.Width;

            PointF[] points;

            if (width > 0) {
                // Formatted text
                points = new PointF[4];
                float topAdjust = symdef.FontEmHeight - (symdef.FontAscent + symdef.FontDescent);
                float height = size.Height + symdef.FontEmHeight - symdef.FontAscent;

                // OCAD adds an extra internal leading (incorrectly).
                topAdjust = symdef.FontEmHeight - (symdef.FontAscent + symdef.FontDescent);

                // OCAD always aligns formatted text by the top.
                // TODO: Should we do this different for OCAD 9 and before if VertAlignment is not BaseLine?
                topAdjust = symdef.GetOcadTopAdjustment(true);

                location.Y -= (float)(topAdjust * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.X -= (float) (topAdjust * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                points[3] = location;
                location.Y -= (float) (width * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.X += (float) (width * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                points[2] = location;
                location.Y -= (float) (height * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.X -= (float) (height * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                points[1] = location;
                location.Y += (float) (width * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.X -= (float) (width * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                points[0] = location;
            }
            else {
                // Unformatted text
                float topAdjust = 0;
                float height = symdef.FontEmHeight;
                float descent = symdef.FontDescent;
                points = new PointF[5];

                // OCAD top align uses the W height, while we use the Font ascent. Adjust for the small difference.
                // TODO: Should we do this different for OCAD 9 and before if VertAlignment is not BaseLine?
                topAdjust = symdef.GetOcadTopAdjustment(false);

                location.Y -= (float) (topAdjust * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.X -= (float) (topAdjust * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                points[0] = location;
                location.Y -= (float) (descent * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.X -= (float) (descent * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                if (symdef.FontAlignment == TextSymDefHorizAlignment.Right) {
                    location.Y += (float) (size.Width * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                    location.X -= (float) (size.Width * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                }
                else if (symdef.FontAlignment == TextSymDefHorizAlignment.Center) {
                    location.Y += (float) ((size.Width/2) * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                    location.X -= (float) ((size.Width/2) * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                }
                points[1] = location;
                location.Y -= (float) (size.Width * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.X += (float) (size.Width * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                points[2] = location;
                location.Y += (float) ((descent+height) * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.X += (float) ((descent+height) * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                points[3] = location;
                location.Y += (float) (size.Width * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.X -= (float) (size.Width * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                points[4] = location;
            }

            OcadCoord[] coords = new OcadCoord[points.Length];
            for (int i = 0; i < coords.Length; ++i)
                coords[i] = OcadCoordFromPoint(points[i]);
    
            return coords;
        }

        // From an cColor (not a SymColor), get a compressed
        // 32-bit CMYK value.
        // These are used in the Col fields of objects for image objects.
        uint CompressedCMYKFromColor(Color color)
        {
            // Get red/green/blue parts
            int red = color.R;
            int green = color.G;
            int blue = color.B;

            float r = (float) red / 255F;
            float g = (float) green / 255F;
            float b = (float) blue / 255F;

            // Convert to CMYK as floats.
            float c, m, y, k;
            SymColor.RGBtoCMYK(r, g, b, out c, out m, out y, out k);

            // Convert to ints in 0..200 range.
            uint cyan = (uint) Math.Round(c * 200F);
            uint magenta = (uint) Math.Round(m * 200F);
            uint yellow = (uint) Math.Round(y * 200F);
            uint black = (uint) Math.Round(k * 200F);

            // Pack into 32 bits.
            return cyan + (magenta << 8) + (yellow << 16) + (black << 24);
        }


        // Write a symbol, return nItem + nText.
        int WriteSymbol(Symbol sym, out OcadObject obj) {
            obj = new OcadObject();
            obj.Sym = ConvertSymdefId(sym.Definition.OcadID);

            if (sym is PointSymbol) {
                PointSymbol psym = sym as PointSymbol;

                obj.Otp = 1;
                obj.Ang = AngleToOcad(psym.Rotation);
                OcadCoord pointCoord = OcadCoordFromPoint(psym.Location);
                if (psym.Gaps == null) {
                    obj.coords = new OcadCoord[1] { pointCoord };
                }
                else {
                    // object has circle gaps in it.
                    obj.coords = new OcadCoord[1 + psym.Gaps.Length / 2];
                    obj.coords[0] = pointCoord;
                    for (int i = 0; i < psym.Gaps.Length; i += 2) {
                        obj.coords[i / 2 + 1].x = AngleToOcad(psym.Gaps[i]);
                        obj.coords[i / 2 + 1].y = AngleToOcad(psym.Gaps[i + 1]);
                    }
                }
            }
            else if (sym is LineSymbol) {
                LineSymbol lsym = sym as LineSymbol;

                obj.Otp = 2;
                obj.coords = CoordsFromSymPath(lsym.Path);
            }
            else if (sym is GraphicsLineSymbol) {
                GraphicsLineSymbol glsym = sym as GraphicsLineSymbol;

                obj.Sym = -2;
                obj.Otp = 2;
                obj.coords = CoordsFromSymPath(glsym.Path);
                obj.Col = (uint) NumberOfColor(glsym.LineColor);
                obj.LineWidth = (short) ToOcadDimensions(glsym.Thickness);
                obj.DiamFlags = OcadLineStyle(glsym.LineStyle);
            }
            else if (sym is ImageLineSymbol) {
                ImageLineSymbol ilsym = sym as ImageLineSymbol;

                obj.Sym = -3;
                obj.Otp = 2;
                obj.coords = CoordsFromSymPath(ilsym.Path);
                obj.Col = CompressedCMYKFromColor(ilsym.LineColor);
                obj.LineWidth = (short) ToOcadDimensions(ilsym.Thickness);
                obj.DiamFlags = OcadLineStyle(ilsym.LineStyle);
            }
            else if (sym is AreaSymbol) {
                AreaSymbol asym = sym as AreaSymbol;

                obj.Otp = 3;
                obj.coords = CoordsFromSymPathWithHoles(asym.Path);
                obj.Ang = AngleToOcad(asym.Angle);

                // UNDONE: if this symbol has a border and version <=8, need to create additional objects for the border.
            }
            else if (sym is GraphicsAreaSymbol) {
                GraphicsAreaSymbol gasym = sym as GraphicsAreaSymbol;

                obj.Sym = -2;
                obj.Otp = 3;
                obj.coords = CoordsFromSymPathWithHoles(gasym.Path);
                obj.Col = (uint) NumberOfColor(gasym.FillColor);
            }
            else if (sym is ImageAreaSymbol) {
                ImageAreaSymbol iasym = sym as ImageAreaSymbol;

                obj.Sym = -3;
                obj.Otp = 3;
                obj.coords = CoordsFromSymPathWithHoles(iasym.Path);
                obj.Col = CompressedCMYKFromColor(iasym.FillColor);
            }
            else if (sym is TextSymbol) {
                TextSymbol tsym = sym as TextSymbol;

                obj.Otp = (byte) (tsym.Width > 0 ? 5 : 4);           
                obj.text = string.Join("\r\n", tsym.Text);
                obj.Ang = AngleToOcad(tsym.Rotation);
                obj.coords = GetTextObjectCoords(tsym);
            }
            else if (sym is LineTextSymbol) {
                LineTextSymbol ltsym = sym as LineTextSymbol;

                obj.Otp = (byte) ((version <= 8) ? 2 : 6);
                obj.text = ltsym.Text;
                obj.Ang = 0;
                obj.coords = CoordsFromSymPath(ltsym.Path);
            }
            else {
                Debug.Fail("Unexpected symbol type");
            }

            // UNDONE: deal with overflow of a short below.
            if (obj.coords != null)
                obj.nItem = (short) (obj.coords.Length);
            if (obj.text != null) {
                if (version >= 9 || obj.Unicode != 0)
                    obj.nText = (short) ((obj.text.Length / 4) + 1);
                else
                    obj.nText = (short) ((obj.text.Length / 8) + 1);
            }

            obj.Write(writer, version);
            return obj.nItem + obj.nText;
        }

        // Create the parameter string for the template.
        OcadParamString CreateTemplateStringParameter(TemplateInfo template)
        {
            float mmPerPixel;
            if (template.dpi > 0)
                mmPerPixel = 25.4F / template.dpi;
            else
                mmPerPixel = 0;

            OcadParamString paramString = new OcadParamString();
            paramString.StType = (int) OcadStringParam.Template ;
            paramString.ObjIndex = 0;
            paramString.codes = new char[7];
            paramString.values = new string[7];
            paramString.firstField = template.absoluteFileName; 
            paramString.codes[0] = 'r';
            paramString.values[0] = template.visible ? "1" : "0";  // visible
            paramString.codes[1] = 's';
            paramString.values[1] = template.visible ? "1" : "0";  //visible
            paramString.codes[2] = 'x';
            paramString.values[2] = template.centerPoint.X.ToString(CultureInfo.InvariantCulture);   // offset X
            paramString.codes[3] = 'y';
            paramString.values[3] = template.centerPoint.Y.ToString(CultureInfo.InvariantCulture); ;  // offset Y
            paramString.codes[4] = 'a'; 
            paramString.values[4] = template.angle.ToString(CultureInfo.InvariantCulture);  // angle
            paramString.codes[5] = 'u';
            paramString.values[5] = mmPerPixel.ToString(CultureInfo.InvariantCulture); // dpi x
            paramString.codes[6] = 'v';
            paramString.values[6] = mmPerPixel.ToString(CultureInfo.InvariantCulture); // dpi y

            return paramString;
        }

        // Create the parameter strings for all the colors.
        List<OcadParamString> CreateColorStringParameters()
        {
            List<SymColor> colors = new List<SymColor>(map.AllColors);
            List<OcadParamString> paramStrings = new List<OcadParamString>(colors.Count);

            int colorCount = colors.Count;
            for (int i = colorCount - 1; i >= 0; --i) {
                // Process colors in reverse order.
                SymColor color = colors[i];

                OcadParamString paramString = new OcadParamString();
                float cyan, magenta, yellow, black;

                color.GetCMYK(out cyan, out magenta, out yellow, out black);

                paramString.StType = (int) OcadStringParam.Color;
                paramString.ObjIndex = 0;
                paramString.firstField = color.Name;
                paramString.codes = new char[6];
                paramString.values = new string[6];
                paramString.codes[0] = 'n';
                paramString.values[0] = color.OcadId.ToString();
                paramString.codes[1] = 'c';
                paramString.values[1] = (cyan * 100F) .ToString(CultureInfo.InvariantCulture);
                paramString.codes[2] = 'm';
                paramString.values[2] = (magenta * 100F).ToString(CultureInfo.InvariantCulture);
                paramString.codes[3] = 'y';
                paramString.values[3] = (yellow * 100F).ToString(CultureInfo.InvariantCulture);
                paramString.codes[4] = 'k';
                paramString.values[4] = (black * 100F).ToString(CultureInfo.InvariantCulture);
                paramString.codes[5] = 'o';
                paramString.values[5] = "0";

                paramStrings.Add(paramString);
            }

            return paramStrings;
        }

        // Create the parameter string for the scale.
        OcadParamString CreateScaleParameter()
        {
            OcadParamString paramString = new OcadParamString();
            paramString.StType = (int) OcadStringParam.ScalePar;
            paramString.ObjIndex = 0;
            paramString.codes = new char[7];
            paramString.values = new string[7];
            paramString.codes[0] = 'm';
            paramString.values[0] = map.MapScale.ToString(CultureInfo.InvariantCulture);
            paramString.codes[1] = 'g';
            paramString.values[1] = "10.00";   // grid scale
            paramString.codes[2] = 'r';
            paramString.values[2] = "0";   // real world coords off 
            paramString.codes[3] = 'x';
            paramString.values[3] = "0.0";  // real world X
            paramString.codes[4] = 'y';
            paramString.values[4] = "0.0";  // real world Y
            paramString.codes[5] = 'a';
            paramString.values[5] = "0.0";  // real world angle
            paramString.codes[6] = 'd';
            paramString.values[6] = "500.0";  // real world grid

            return paramString;
        }

        // Create the parameter string for the print parameters.
        OcadParamString CreatePrintParameter()
        {
            RectangleF printArea = map.PrintArea;
            bool entireMap = false;
            if (printArea.Left == 0 && printArea.Right == 0 && printArea.Top == 0 && printArea.Bottom == 0)
                entireMap = true;

            int numCodes = entireMap ? 2 : 6;

            OcadParamString paramString = new OcadParamString();
            paramString.StType = (int) OcadStringParam.PrintPar;
            paramString.ObjIndex = 0;
            paramString.codes = new char[numCodes];
            paramString.values = new string[numCodes];
            paramString.codes[0] = 'a';
            paramString.values[0] = map.PrintScale.ToString(CultureInfo.InvariantCulture);
            paramString.codes[1] = 'r';
            paramString.values[1] = entireMap ? "0" : "1";
            if (!entireMap) {
                paramString.codes[2] = 'L';
                paramString.values[2] = printArea.Left.ToString(CultureInfo.InvariantCulture);
                paramString.codes[3] = 'B';
                paramString.values[3] = printArea.Top.ToString(CultureInfo.InvariantCulture);   // note: bottom and top switched due to Y-positive coord system.
                paramString.codes[4] = 'R';
                paramString.values[4] = printArea.Right.ToString(CultureInfo.InvariantCulture);
                paramString.codes[5] = 'T';
                paramString.values[5] = printArea.Bottom.ToString(CultureInfo.InvariantCulture);   // note: bottom and top switched due to Y-positive coord system.
            }

            return paramString;
        }

        // Write out all string parameters, return position of first block.
        int WriteStringParameters()
        {
            List<OcadParamString> paramStrings = new List<OcadParamString>();

            if (version >= 9) {
                // Add all the string parameters needed to the list of string parameters.
                paramStrings.AddRange(CreateColorStringParameters());
                paramStrings.Add(CreateScaleParameter());
                paramStrings.Add(CreatePrintParameter());
            }

            TemplateInfo template = map.Template;
            if (template != null)
                paramStrings.Add(CreateTemplateStringParameter(template));

            // Write out all the parameters strings.
            int firstStIndexBlock = 0;
            int offsetPrevBlock = 0;
            OcadStIndexBlock currBlock = new OcadStIndexBlock();
            int index, count, total;

            total = paramStrings.Count;
            index = 0; count = 0;
            foreach (OcadParamString paramString in paramStrings) {
                currBlock.Table[index].Pos = (int) writer.Seek(0, SeekOrigin.Current);
                currBlock.Table[index].Len = (paramString.CharacterCount() + 2) & ~1;  // add 1 for terminating nul, pad to even boundary.
                currBlock.Table[index].ObjIndex = paramString.ObjIndex;
                currBlock.Table[index].StType = paramString.StType;
                paramString.Write(writer, currBlock.Table[index].Len);

                ++index; ++count;
                if (index == 256 || count == total) {
                    index = 0;

                    int current = (int) writer.Seek(0, SeekOrigin.Current);
                    if (offsetPrevBlock == 0)
                        firstStIndexBlock = current;
                    else {
                        writer.Seek(offsetPrevBlock, SeekOrigin.Begin);
                        writer.Write(current);
                        writer.Seek(current, SeekOrigin.Begin);
                    }
                    currBlock.Write(writer);
                    offsetPrevBlock = current;
                    currBlock = new OcadStIndexBlock();
                }
            }
            Debug.Assert(count == total);

            return firstStIndexBlock;
        }
    }
}
