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
    using System.Drawing.Drawing2D;
    using System.Linq;
    using PurplePen.Graphics2D;

    public class OcadExport {
        Map map;
        int version;
        BinaryWriter writer;
        string filename;
        bool useOcadSaved;
        Dictionary<SymDef, short> mapSymdefToSingleColor = new Dictionary<SymDef, short>();  // for OCAD 9, maps symdefs to a single color they use, if any. (-1 if not).
        List<LayoutObject> layoutObjects = new List<LayoutObject>();        // List of all layout objects in the map, for use in creating the parameter strings.
        List<LayoutObjectFont> layoutFonts = new List<LayoutObjectFont>();        // List of all layout fonts in the map.
        Dictionary<SymDef, int> mapSymdefToId = new Dictionary<SymDef, int>(); // map symdefs to the ids they use (call OcadIdOfSymdef instead of using directly)

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
            return (color == null) ? (short)0 : color.OcadId;
        }

        public OcadExport() {
        }

        // Write the given map to an OCAD file.
        //   version = 6,7,8 -- the OCAD version
        //   usedSavedOcadInfo utilizes symbol information that was saved away if this file was
        //     loaded from OCAD, for better round tripping. Setup structure information and 
        //     symbol header information is always used.
        public void WriteMap(Map map, string filename, int version, bool useSavedOcadInfo) {
            using (Stream stream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite)) {
                WriteMap(map, stream, filename, version, useSavedOcadInfo);
            }
        }

        // Write the given map to an OCAD file.
        //   version = 6,7,8 -- the OCAD version
        //   usedSavedOcadInfo utilizes symbol information that was saved away if this file was
        //     loaded from OCAD, for better round tripping. Setup structure information and 
        //     symbol header information is always used.
        public void WriteMap(Map map, Stream stream, string filename, int version, bool useSavedOcadInfo) {
            using (map.Read()) {
                this.map = map;
                this.version = version;
                this.filename = filename;
                this.useOcadSaved = useSavedOcadInfo;

                if (version < 6 || (version > 12 && version < 2018) || version > 2018)
                    throw new ArgumentException("Bad version number", nameof(version));

                OcadFileHeader fileHeader = new OcadFileHeader();

                writer = new BinaryWriter(stream, Encoding.Default);

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
                if (version == 11)
                    fileHeader.Subversion = 770;
                else if (version == 12)
                    fileHeader.Subversion = 256;
                else
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

                // Write the file information.
                if (version <= 8 && !string.IsNullOrEmpty(map.FileInformation)) {
                    fileHeader.InfoPos = WriteFileInformation(out fileHeader.InfoSize);
                }

                // rewrite the file header
                writer.Seek(0, SeekOrigin.Begin);
                fileHeader.Write(writer);
                
                // We do NOT close the binary writer here, but just flush it. This is 
                // to leave the underlying stream open.
                writer.Flush();
                writer = null;
            }
        }

        int WriteFileInformation(out int size)
        {
            int pos = (int)writer.Seek(0, SeekOrigin.Current);
            writer.Write(map.FileInformation.ToCharArray());
            size = (int)writer.Seek(0, SeekOrigin.Current) - pos;
            return pos;
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

            // Set real-world coord info.
            RealWorldCoords realWorldCoords = map.RealWorldCoords;
            setup.RealWorldOfsX = realWorldCoords.RealWorldOffsetX;
            setup.RealWorldOfsY = realWorldCoords.RealWorldOffsetY;
            setup.RealWorldAngle = realWorldCoords.RealWorldAngle;
            setup.RealWorldGrid = realWorldCoords.RealWorldGridDistance;
            setup.rGridDist = realWorldCoords.PaperGridDistance;
            setup.RealWorldCoord = (ushort)(realWorldCoords.RealWorldOn ? 1 : 0);

            // Set template for if version is <= 7. Parameter strings used for version 8 and above.
            // OCAD 7 supports only one template.
            IList<TemplateInfo> templates = map.Templates;
            if (templates.Count > 0 && version <= 7) {
                TemplateInfo template = templates[0];
                bool isOcadFile = false;

                try {
                    using (Stream stm = new FileStream(template.absoluteFileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        isOcadFile = InputOutput.IsOcadFile(stm);
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
                setup.HideTemp = (ushort) ((template.visible && !map.HideTemplates) ? 0 : 1);
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
                colorInfo.Overprint = color.OverPrint ? (short)-1 : (short)0;

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
                if ((symdef as TextSymDef).SymbolKind == TextSymDef.PreferredSymbolKind.LineText)
                    WriteLineTextSymDef(symdef as TextSymDef);
                else
                    WriteTextSymDef(symdef as TextSymDef);
            }
            else if (symdef is RectangleSymDef) {
                WriteRectangleSymDef(symdef as RectangleSymDef);
            }
            else {
                Debug.Fail("Unknownsymdef kind");
            }
        }

        int OcadIdFromSymdef(SymDef symdef)
        {
            int ocadId;
            if (mapSymdefToId.TryGetValue(symdef, out ocadId)) {
                return ocadId;
            }
            else {
                int? possibleOcadId;
                possibleOcadId = ParseOcadIdFromString(symdef.SymbolId);
                if (!possibleOcadId.HasValue) {
                    possibleOcadId = FindUnusedOcadId();
                }
                mapSymdefToId[symdef] = possibleOcadId.Value;
                return possibleOcadId.Value;
            }
        }

        // Convert symbol id into OCAD id for the given version is possible, else return null.
        int? ParseOcadIdFromString(string symbolId)
        {
            int intPart, fracPart;
            string[] parts = symbolId.Split('.');

            if (parts.Length == 1) {
                // Allow negative for layout layer, etc.
                if (int.TryParse(parts[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out intPart)) {
                    if (intPart < 0)
                        return intPart;
                    else if (version >= 9)
                        return intPart * 1000;
                    else if (intPart <= 999)
                        return intPart * 10;
                    else
                        return null;
                }
                else {
                    return null;
                }
            }
            else if (parts.Length == 2) {
                if (int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out intPart) &&
                    int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out fracPart)) 
                {
                    if (version >= 9)
                        return intPart * 1000 + fracPart;
                    else if (intPart <= 999)
                        return intPart * 10 + fracPart;
                    else
                        return null;
                }
                else {
                    return null;
                }
            }
            else {
                return null;
            }
        }

        int FindUnusedOcadId()
        {
            // Get an OCAD ID that isn't used, starting at 920.
            int integerPart = 920, fracPart = 0;
            int ocadId;
            string idString;

            for (;;) {
                idString = string.Format("{0}.{1}", integerPart, fracPart);
                if (version >= 9)
                    ocadId = integerPart * 1000 + fracPart;
                else
                    ocadId = integerPart * 10 + fracPart;

                if (map.SymdefFromSymbolId(idString) == null && !mapSymdefToId.ContainsValue(ocadId))
                    return ocadId;

                ++fracPart;
                if (fracPart >= 10) {
                    integerPart += 1;
                    fracPart = 0;
                }
            }

        }

        // Fill in common parts of the OcadSymbol class: except Size, Otp, SymTp, Extent
        void FillInCommonSymdef(OcadSymbol symbol, SymDef symdef) {
            symbol.Sym = OcadIdFromSymdef(symdef);
            symbol.Selected = false;
            symbol.Status = 0;
            symbol.FilePos = 0;

            List<short> colorsUsed = new List<short>(2);

            foreach (SymColor symcolor in map.AllColors) {
                if (symdef.HasColor(symcolor)) {
                    int colorid = NumberOfColor(symcolor);
                    if (version <= 8 && colorid >= 0 && colorid <= 255)
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

        short OcadLineStyle(LineJoin lineJoin, LineCap lineCap)
        {
            if (lineCap == LineCap.Round) {
                return 1;
            }
            else {
                if (lineJoin == LineJoin.Bevel || lineJoin == LineJoin.Round) {
                    return 0;
                }
                else {
                    return 4;
                }
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
                            case TextSymDefVertAlignment.MidpointAllLines: return 4;
                            case TextSymDefVertAlignment.Baseline: return 0;
                            case TextSymDefVertAlignment.Bottom: return 0;
                            default: return 0;
                        }

                    case TextSymDefHorizAlignment.Right:
                        switch (vertAlign) {
                            case TextSymDefVertAlignment.TopAscent: return 10;
                            case TextSymDefVertAlignment.Midpoint: return 6;
                            case TextSymDefVertAlignment.MidpointAllLines: return 6;
                            case TextSymDefVertAlignment.Baseline: return 2;
                            case TextSymDefVertAlignment.Bottom: return 2;
                            default: return 2;
                        }
                    case TextSymDefHorizAlignment.Center:
                        switch (vertAlign) {
                            case TextSymDefVertAlignment.TopAscent: return 9;
                            case TextSymDefVertAlignment.Midpoint: return 5;
                            case TextSymDefVertAlignment.MidpointAllLines: return 5;
                            case TextSymDefVertAlignment.Baseline: return 1;
                            case TextSymDefVertAlignment.Bottom: return 1;
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
            symbol.LineEnds = (ushort) OcadLineStyle(symdef.MainLineJoin, symdef.MainLineCap);
            
            LineSymDef.GlyphInfo[] glyphs = symdef.Glyphs;
            int startGlyphIndex = -1, endGlyphIndex = -1, cornerGlyphIndex = -1;
            int dashCenteredGlyphIndex = -1, gapCenteredGlyphIndex = -1, spacedGlyphIndex = -1, spacedOffsetGlyphIndex = -1;
            int primGlyphIndex = -1, secondaryGlyphIndex = -1;

            if (glyphs != null) {
                for (int i = 0; i < glyphs.Length; ++i) {
                    // UNDONE: give warning if more than one glyph of a certain type
                    switch (glyphs[i].location) {
                        case LineSymDef.GlyphLocation.Corners:
                        case LineSymDef.GlyphLocation.CornersIgnoreEnds:
                            cornerGlyphIndex = i; break;
                        case LineSymDef.GlyphLocation.DashCenters:
                            dashCenteredGlyphIndex = i; break;
                        case LineSymDef.GlyphLocation.End:
                            endGlyphIndex = i; break;
                        case LineSymDef.GlyphLocation.GapCenters:
                            gapCenteredGlyphIndex = i; break;
                        case LineSymDef.GlyphLocation.Spaced:
                            spacedGlyphIndex = i; break;
                        case LineSymDef.GlyphLocation.SpacedDecrease:
                            spacedGlyphIndex = i; break;
                        case LineSymDef.GlyphLocation.SpacedOffset:
                            spacedOffsetGlyphIndex = i; break;
                        case LineSymDef.GlyphLocation.Start:
                            startGlyphIndex = i; break;
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
                symbol.FrStyle =  OcadLineStyle(symdef.SecondLineJoin, symdef.SecondLineCap);
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

            byte ocad11SymbolFlags = 1;
            if (spacedOffsetGlyphIndex >= 0) { // UNDONE: if both secondaryGlyphIndex and spacedOffset, we can only represent one in OCAD.
                symbol.SecDElts = SymbolEltsFromGlyph(glyphs[spacedOffsetGlyphIndex].glyph, out symbol.SecDSize);
                ocad11SymbolFlags |= OcadLineSymbol.SymbolFlagSec;
            }
            if (secondaryGlyphIndex >= 0) {
                symbol.SecDElts = SymbolEltsFromGlyph(glyphs[secondaryGlyphIndex].glyph, out symbol.SecDSize);
                ocad11SymbolFlags |= OcadLineSymbol.SymbolFlagSec;
            }
            if (startGlyphIndex >= 0) {
                symbol.StartDElts = SymbolEltsFromGlyph(glyphs[startGlyphIndex].glyph, out symbol.StartDSize);
                ocad11SymbolFlags |= OcadLineSymbol.SymbolFlagStart;
            }
            if (endGlyphIndex >= 0) {
                symbol.EndDElts = SymbolEltsFromGlyph(glyphs[endGlyphIndex].glyph, out symbol.EndDSize);
                ocad11SymbolFlags |= OcadLineSymbol.SymbolFlagEnd;
            }
            if (cornerGlyphIndex >= 0) {
                symbol.CornerDElts = SymbolEltsFromGlyph(glyphs[cornerGlyphIndex].glyph, out symbol.CornerDSize);
                ocad11SymbolFlags |= OcadLineSymbol.SymbolFlagCorner;
            }
            if (version >= 11)
                symbol.UseSymbolFlags = ocad11SymbolFlags;

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
                    symbol.BorderSym = OcadIdFromSymdef(symdef.BorderSymdef);
                }
            }

            if (symdef.HasHatching) {
                // Only use the first two hatching, and the second only if it matches the first in all but angle.
                AreaSymDef.HatchInfo[] hatchings = symdef.GetHatchings().ToArray();
                symbol.HatchMode = 1;
                symbol.HatchColor = NumberOfColor(hatchings[0].hatchColor);
                symbol.HatchLineWidth = (short)ToOcadDimensions(hatchings[0].hatchWidth);
                if (version >= 9)
                    symbol.HatchDist = (short)ToOcadDimensions(hatchings[0].hatchSpacing);
                else
                    symbol.HatchDist = (short)ToOcadDimensions(hatchings[0].hatchSpacing - hatchings[0].hatchWidth);
                symbol.HatchAngle1 = AngleToOcad(hatchings[0].hatchAngle);

                if (hatchings.Length > 1 && hatchings[1].hatchColor == hatchings[0].hatchColor && hatchings[1].hatchWidth == hatchings[0].hatchWidth &&
                    hatchings[1].hatchSpacing == hatchings[0].hatchSpacing) {
                    // 2nd hatching that matches first in all but angle
                    symbol.HatchMode = 2;
                    symbol.HatchAngle2 = AngleToOcad(hatchings[1].hatchAngle);
                }
            }
            else {
                symbol.HatchMode = 0;
            }

            if (symdef.HasPattern) {
                // Only use the first pattern.
                AreaSymDef.PatternInfo patternInfo = symdef.GetPatterns().First();
                symbol.StructMode = (byte)(patternInfo.offsetRows ? 2 : 1);
                symbol.StructWidth = (short)ToOcadDimensions(patternInfo.patternWidth);
                symbol.StructHeight = (short)ToOcadDimensions(patternInfo.patternHeight);
                symbol.StructAngle = AngleToOcad(patternInfo.patternAngle);
                symbol.StructElts = SymbolEltsFromGlyph(patternInfo.patternGlyph, out symbol.DataSize);
                if (version >= 12) {
                    symbol.StructDraw = (byte)patternInfo.patternFillMode;
                    if (patternInfo.irregular) {
                        symbol.StructIrregularVarX = (byte)Math.Round(100.0 * patternInfo.irregularVarX);
                        symbol.StructIrregularVarY = (byte)Math.Round(100.0 * patternInfo.irregularVarY);
                        symbol.StructIrregularMinDist = (short)ToOcadDimensions(patternInfo.irregularMinDist);
                    }
                    else {
                        symbol.StructIrregularVarX = symbol.StructIrregularVarY = 0;
                        symbol.StructIrregularMinDist = 0;
                    }
                }
                else {
                    symbol.StructDraw = 0;
                }
            }
            else {
                symbol.StructMode = 0;
            }

            symbol.Write(writer, version);
        }

        void WriteRectangleSymDef(RectangleSymDef symdef)
        {
            OcadRectSymbol symbol = new OcadRectSymbol();
            FillInCommonSymdef(symbol, symdef);

            symbol.Otp = (short) ((version >= 9) ? 7 : 5);
            symbol.SymTp = 0;
            symbol.Extent = (short)ToOcadDimensions(symdef.LineThickness / 2);
            symbol.LineColor = symdef.LineColor != null ? (short)NumberOfColor(symdef.LineColor) : (short)0;
            symbol.LineWidth = (short)ToOcadDimensions(symdef.LineThickness);
            symbol.Radius = (short)ToOcadDimensions(symdef.CornerRadius);
            symbol.GridFlags = symdef.GridFlags;
            symbol.CellWidth = (short)ToOcadDimensions(symdef.CellWidth);
            symbol.CellHeight = (short)ToOcadDimensions(symdef.CellHeight);
            symbol.UnnumCells = (short) symdef.UnnumberedCells;
            symbol.UnnumText = symdef.UnnnumberedText;
            if (version >= 10)
                symbol.FontSize = (short)Math.Round(Geometry.PointsFromMm(symdef.TextSize) * 10);

            symbol.Write(writer, version);
        }


        void WriteTextSymDef(TextSymDef symdef) {
            OcadTextSymbol symbol = new OcadTextSymbol();
            FillInCommonSymdef(symbol, symdef);

            symbol.Otp = 4;
            symbol.SymTp = 1;

            if (symdef.FontColor != null)
                symbol.FontColor = NumberOfColor(symdef.FontColor);
            symbol.Italic = ((symdef.TextEffects & TextEffects.Italic) != 0);
            if (((symdef.TextEffects & TextEffects.Bold) != 0))
                symbol.Weight = 700;
            else
                symbol.Weight = 400;
            symbol.Alignment = OcadTextAlignment(symdef.FontAlignment, symdef.VertAlignment);
            symbol.FontSize = (short) Math.Round(Geometry.PointsFromMm(symdef.FontEmHeight) * 10);
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
                    symbol.PointSym = OcadIdFromSymdef(symdef.CenterPointSymdef);
                }
            }

            symbol.Write(writer, version);
        }

        void WriteLineTextSymDef(TextSymDef symdef)
        {
            OcadLineTextSymbol symbol = new OcadLineTextSymbol();
            FillInCommonSymdef(symbol, symdef);

            if (version < 9) {
                symbol.Otp = 2;
                symbol.SymTp = 1;
            }
            else {
                symbol.Otp = 6;
                symbol.SymTp = 0;
            }

            if (symdef.FontColor != null)
                symbol.FontColor = NumberOfColor(symdef.FontColor);
            symbol.Italic = ((symdef.TextEffects & TextEffects.Italic) != 0);
            if ((symdef.TextEffects & TextEffects.Bold) != 0)
                symbol.Weight = 700;
            else
                symbol.Weight = 400;
            symbol.Alignment = OcadTextAlignment(symdef.FontAlignment, symdef.VertAlignment);
            symbol.FontSize = (short)Math.Round(Geometry.PointsFromMm(symdef.FontEmHeight) * 10);
            symbol.FontName = symdef.FontName;
            symbol.CharSpace = (short)Math.Round(symdef.CharSpacing * 100F);
            symbol.WordSpace = (short)Math.Round(symdef.WordSpacing * 100F);

            short tempLeft, tempTop, tempRight, tempBottom;
            WriteFraming(symdef, out symbol.FrMode, out symbol.FrFlags, out symbol.FrName, out symbol.FrColor, out symbol.FrWidth,
                      out symbol.FrSize, out symbol.FrWeight, out symbol.FrItalic, out symbol.FrOfX, out symbol.FrOfY, out tempLeft, out tempTop, out tempRight, out tempBottom);

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
                    ocadFrItalic = ((symdef.TextEffects & TextEffects.Italic) != 0);
                    ocadFrWeight = (short) (((symdef.TextEffects & TextEffects.Bold) != 0) ? 700 : 400);
                    ocadFrSize = (short) Math.Round(Geometry.PointsFromMm(symdef.FontEmHeight) * 10);
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

        internal int NearestOcadColorSlow(Color col, bool use8bit) {
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

        internal int NearestOcadColor(Color col, bool use8bit) {
            if (use8bit) {
                int rQuintile = (col.R + 31) / 64;
                int gQuintile = (col.G + 31) / 64;
                int bQuintile = (col.B + 31) / 64;
                int index = (rQuintile * 25) + (gQuintile * 5) + bQuintile;
                return index;
            }
            else {
                Color[] colorMap = OcadConstants.ocadColorMap4Bit;

                // Search Ocad Color Map for closest color
                int bestIndex = 0;
                int bestDist = int.MaxValue;
                for (int index = 0; index < colorMap.Length; ++index) {
                    int dist = Util.ColorDistance(col, colorMap[index]);
                    if (dist < bestDist) {
                        bestIndex = index; bestDist = dist;
                    }
                }

                return bestIndex;
            }
        }


        // Convert an ToolboxIcon into an OCAD icon bits.
        byte[] ToolboxIconToOcadIcon(ToolboxIcon toolboxIcon, int version) {
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
                elt.stFlags = (ushort) OcadLineStyle(part.lineJoin, part.lineCap);
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
            int index, count;
            ICollection<Symbol> symbols = map.AllSymbols;

            index = 0; count = 0; 
            foreach (Symbol sym in symbols) {
                if (version < 9 && (sym is GraphicsAreaSymbol || sym is GraphicsLineSymbol || sym is ImageAreaSymbol || sym is ImageLineSymbol))
                    continue;     // image and graphics objects not supported in OCAD 6-8.
                if (sym is ImageBitmapSymbol) {
                    // image bitmap symbols don't have indexes
                    layoutObjects.Add(new LayoutObject(0, sym));  // zero means no index
                    continue;     
                }

                OcadIndex ocadIndex = new OcadIndex();

                ocadIndex.Pos = (int) writer.Seek(0, SeekOrigin.Current);
                RectangleF bounds = sym.BoundingBox;
                ocadIndex.LowerLeft = OcadCoordFromPoint(new PointF(bounds.Left, bounds.Top));
                ocadIndex.UpperRight = OcadCoordFromPoint(new PointF(bounds.Right, bounds.Bottom));
                ocadIndex.Sym = OcadIdFromSymdef(sym.Definition);

                OcadObject ocadObj;
                int layoutFontIndex;
                int nItems = WriteSymbol(sym, out ocadObj, out layoutFontIndex);

                // Make sure the LowerLeft/UpperRight encompasses all coordinates, otherwise OCAD
                // thinks the object is corrupt.
                for (int i = 0; i < ocadObj.coords.Length; ++i) {
                    int x = ocadObj.coords[i].x, y = ocadObj.coords[i].y;
                    if (ocadIndex.LowerLeft.x > x) { ocadIndex.LowerLeft.x = x; }
                    if (ocadIndex.UpperRight.x < x) { ocadIndex.UpperRight.x = x; }
                    if (ocadIndex.LowerLeft.y > y) { ocadIndex.LowerLeft.y = y; }
                    if (ocadIndex.UpperRight.y < y) { ocadIndex.UpperRight.y = y; }
                }

                if (version >= 9) {
                    ocadIndex.ObjType = ocadObj.Otp;
                    ocadIndex.Status = 1;
                    ocadIndex.ViewType = 0;
                    if (ocadObj.Sym == -2)
                        ocadIndex.Color = (short) ocadObj.Col;      // graphics object.
                    else if (ocadObj.Sym == -3 || ocadObj.Sym == -4)
                        ocadIndex.Color = -1;      // image or layout object.
                    else 
                        ocadIndex.Color = mapSymdefToSingleColor[sym.Definition];
                    ocadIndex.ImpLayer = 0;
                    if (version == 11)
                        ocadIndex.LayoutFont = (byte)layoutFontIndex;
                }

                if (version >= 11) {
                    // Remember any layout objects for later.
                    ImageSymDef imageDef = sym.Definition as ImageSymDef;
                    if (imageDef != null && imageDef.Layer == SymLayer.Layout) {
                        layoutObjects.Add(new LayoutObject(count + 1, sym));  // object indexes start at 1.
                    }
                }

                // UNDONE: throw exception if length of object is too long.
                if (version == 8)
                    ocadIndex.Len = (short) nItems;
                else if (version >= 12)
                    ocadIndex.Len = (short) (56 + 8 * nItems);
                else if (version >= 9)
                    ocadIndex.Len = (short) (40 + 8 * nItems);
                else
                    ocadIndex.Len = (short) (32 + 8 * nItems);

                currBlock.IndexArr[index] = ocadIndex;

                ++index; ++count;
                if (index == 256) {
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

            int currentPos = (int)writer.Seek(0, SeekOrigin.Current);
            if (offsetPrevBlock == 0)
                firstIndexBlock = currentPos;
            else {
                writer.Seek(offsetPrevBlock, SeekOrigin.Begin);
                writer.Write(currentPos);
                writer.Seek(currentPos, SeekOrigin.Begin);
            }
            currBlock.Write(writer, version);

            return firstIndexBlock;
        }

        OcadCoord[] GetAnyTextObjectCoords(PointF location, SizeF size, float angle, float width, float topAdjust, float leftAdjust,
                                           TextSymDefHorizAlignment horizAlignment, TextSymDefVertAlignment vertAlignment,
                                           float fontEmHeight, float fontWHeight, float fontAscent, float fontDescent)
        {
            PointF[] points;

            if (width > 0) {
                // Formatted text
                points = new PointF[4];
                float height = size.Height + fontEmHeight - fontAscent;

                location.Y -= (float)(topAdjust * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.X -= (float)(topAdjust * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.Y -= (float)(leftAdjust * Math.Sin((angle) / 360.0 * 2 * Math.PI));
                location.X -= (float)(leftAdjust * Math.Cos((angle) / 360.0 * 2 * Math.PI));

                points[3] = location;
                location.Y -= (float)(width * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.X += (float)(width * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                points[2] = location;
                location.Y -= (float)(height * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.X -= (float)(height * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                points[1] = location;
                location.Y += (float)(width * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.X -= (float)(width * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                points[0] = location;
            }
            else {
                // Unformatted text
                points = new PointF[5];

                location.Y -= (float)(topAdjust * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.X -= (float)(topAdjust * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                points[0] = location;

                if (version >= 10) {
                    if (vertAlignment == TextSymDefVertAlignment.TopAscent) {
                        location.Y -= (float)(fontWHeight * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                        location.X -= (float)(fontWHeight * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                    }
                    else if (vertAlignment == TextSymDefVertAlignment.Midpoint) {
                        location.Y -= (float)((fontWHeight / 2) * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                        location.X -= (float)((fontWHeight / 2) * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                    }
                }

                location.Y -= (float)(fontDescent * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.X -= (float)(fontDescent * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                if (horizAlignment == TextSymDefHorizAlignment.Right) {
                    location.Y += (float)(size.Width * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                    location.X -= (float)(size.Width * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                }
                else if (horizAlignment == TextSymDefHorizAlignment.Center) {
                    location.Y += (float)((size.Width / 2) * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                    location.X -= (float)((size.Width / 2) * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                }
                points[1] = location;
                location.Y -= (float)(size.Width * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.X += (float)(size.Width * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                points[2] = location;
                location.Y += (float)((fontDescent + fontEmHeight) * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.X += (float)((fontDescent + fontEmHeight) * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                points[3] = location;
                location.Y += (float)(size.Width * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
                location.X -= (float)(size.Width * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
                points[4] = location;
            }

            OcadCoord[] coords = new OcadCoord[points.Length];
            for (int i = 0; i < coords.Length; ++i)
                coords[i] = OcadCoordFromPoint(points[i]);

            return coords;
        }

        OcadCoord[] GetTextObjectCoords(TextSymbol sym) {
            TextSymDef symdef = ((TextSymDef) sym.Definition);
            float topAdjust = symdef.GetOcadTopAdjustment(sym.Width > 0, version);
            float leftAdjust = (sym.Width) > 0 ? symdef.GetOcadFormattedHorizAdjustment(sym.Width) : 0;

            return GetAnyTextObjectCoords(sym.Location, sym.TextSize, sym.Rotation, sym.Width, topAdjust, leftAdjust,
                   symdef.FontAlignment, symdef.VertAlignment, 
                   symdef.FontEmHeight, symdef.WHeight, symdef.FontAscent, symdef.FontDescent);
        }

        OcadCoord[] GetTextObjectCoords(ImageTextSymbol sym)
        {
            using (ITextFaceMetrics textFaceMetrics = map.TextMetricsProvider.GetTextFaceMetrics(sym.FontName, sym.FontSize, TextEffects.None)) {

                // OCAD top align uses the W height, while we use the Font ascent. Adjust for the small difference.
                float topAdjust = ((ImageSymDef)sym.Definition).GetOcadTopAdjustment(sym.FontName, sym.FontSize, sym.Width > 0);

                return GetAnyTextObjectCoords(sym.Location, sym.TextSize, sym.Rotation, sym.Width, topAdjust, 0, 
                TextSymDefHorizAlignment.Left, TextSymDefVertAlignment.Baseline, 
                textFaceMetrics.EmHeight, textFaceMetrics.CapHeight, textFaceMetrics.Ascent, textFaceMetrics.Descent);
            }
        }

        // From an CmykColor (not a SymColor), get a compressed
        // 32-bit CMYK value.
        // These are used in the Col fields of objects for image objects.
        uint CompressedCMYKFromColor(CmykColor color)
        {
            // Get CMYK as floats.
            float c, m, y, k;
            c = color.Cyan;
            m = color.Magenta;
            y = color.Yellow;
            k = color.Black;

            // Convert to ints in 0..200 range.
            uint cyan = (uint) Math.Round(c * 255F);
            uint magenta = (uint) Math.Round(m * 255F);
            uint yellow = (uint) Math.Round(y * 255F);
            uint black = (uint) Math.Round(k * 255F);

            // Pack into 32 bits.
            return cyan + (magenta << 8) + (yellow << 16) + (black << 24);
        }

        private static string ExportTextSymbolLines(string[] lines)
        {
            // OCAD has a weird rule where it ignores a single blank line at the 
            // beginning of text. In order to reverse this in case where there are
            // multiple blank lines, we have to add a blank line if there are existing
            // blank lines at the beginning. Otherwise we lose a blank line every time
            // that we round trip.
            string text = string.Join("\r\n", lines);

            if (lines.Length > 0 && lines[0].Length == 0) {
                text = "\r\n" + text;
            }

            return text;
        }



        // Write a symbol, return nItem + nText.
        int WriteSymbol(Symbol sym, out OcadObject obj, out int layoutFontIndex) {
            obj = new OcadObject();
            obj.Sym = OcadIdFromSymdef(sym.Definition);
            layoutFontIndex = 0;

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
            else if (sym is AreaSymbol) {
                AreaSymbol asym = sym as AreaSymbol;

                obj.Otp = 3;
                obj.coords = CoordsFromSymPathWithHoles(asym.Path);
                obj.Ang = AngleToOcad(asym.Angle);

                // UNDONE: if this symbol has a border and version <=8, need to create additional objects for the border.
            }
            else if (sym is TextSymbol) {
                TextSymbol tsym = sym as TextSymbol;

                obj.Otp = (byte)(tsym.Width > 0 ? 5 : 4);
                obj.text = ExportTextSymbolLines(tsym.Text);
                obj.Ang = AngleToOcad(tsym.Rotation);
                obj.coords = GetTextObjectCoords(tsym);
            }
            else if (sym is ImageTextSymbol) {
                ImageTextSymbol itsym = sym as ImageTextSymbol;

                Debug.Assert(((ImageSymDef)itsym.Definition).Layer == SymLayer.Image || ((ImageSymDef)itsym.Definition).Layer == SymLayer.Layout);
                Debug.Assert(obj.Sym == (((ImageSymDef)itsym.Definition).Layer == SymLayer.Image ? -3 : -4));

                obj.Otp = (byte)(itsym.Width > 0 ? 5 : 4);
                obj.text = ExportTextSymbolLines(itsym.Text);
                obj.Ang = AngleToOcad(itsym.Rotation);
                obj.coords = GetTextObjectCoords(itsym);
                obj.Col = CompressedCMYKFromColor(itsym.TextColor);
                if (version == 11)
                    layoutFontIndex = GetLayoutFontIndex(itsym.FontName, itsym.FontSize);
                else if (version >= 12) {
                    OcadParamString paramString = new OcadParamString();
                    paramString.StType = (int)OcadStringParam.LayoutFontAttributes;  // Not actually used!
                    paramString.firstField = itsym.FontName;
                    paramString.codes = new char[5];
                    paramString.values = new string[5];
                    paramString.codes[0] = 'o';
                    paramString.values[0] = "100";
                    paramString.codes[1] = 's';
                    paramString.values[1] = (itsym.FontSize / 25.4F * 72F).ToString(CultureInfo.InvariantCulture);
                    paramString.codes[2] = 'b';
                    paramString.values[2] = "0"; // UNDONE: bold.
                    paramString.codes[3] = 'i';
                    paramString.values[3] = "0"; // UNDONE: italic.
                    paramString.codes[4] = 'a';
                    paramString.values[4] = "0"; // UNDONE: alignment.
                    obj.objectString = paramString.AsString();
                    obj.ObjectStringType = 3;
                }
            }
            else if (sym is LineTextSymbol) {
                LineTextSymbol ltsym = sym as LineTextSymbol;

                obj.Otp = (byte)((version <= 8) ? 2 : 6);
                obj.text = ltsym.Text;
                obj.Ang = 0;
                obj.coords = CoordsFromSymPath(ltsym.Path);
            }
            else if (sym is GraphicsLineSymbol) {
                GraphicsLineSymbol glsym = sym as GraphicsLineSymbol;

                obj.Sym = -2;
                obj.Otp = 2;
                obj.coords = CoordsFromSymPath(glsym.Path);
                obj.Col = (uint) NumberOfColor(glsym.LineColor);
                obj.LineWidth = (short) ToOcadDimensions(glsym.Thickness);
                obj.DiamFlags = OcadLineStyle(glsym.LineJoin, glsym.LineCap);
            }
            else if (sym is ImageLineSymbol) {
                ImageLineSymbol ilsym = sym as ImageLineSymbol;

                Debug.Assert(((ImageSymDef)ilsym.Definition).Layer == SymLayer.Image || ((ImageSymDef)ilsym.Definition).Layer == SymLayer.Layout);
                Debug.Assert(obj.Sym == (((ImageSymDef)ilsym.Definition).Layer == SymLayer.Image ? -3 : -4));

                obj.Otp = 2;
                obj.coords = CoordsFromSymPath(ilsym.Path);
                obj.Col = CompressedCMYKFromColor(ilsym.LineColor);
                obj.LineWidth = (short) ToOcadDimensions(ilsym.Thickness);
                obj.DiamFlags = OcadLineStyle(ilsym.LineJoin, ilsym.LineCap);
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

                Debug.Assert(((ImageSymDef)iasym.Definition).Layer == SymLayer.Image || ((ImageSymDef)iasym.Definition).Layer == SymLayer.Layout);
                Debug.Assert(obj.Sym == (((ImageSymDef)iasym.Definition).Layer == SymLayer.Image ? -3 : -4));

                obj.Otp = 3;
                obj.coords = CoordsFromSymPathWithHoles(iasym.Path);
                obj.Col = CompressedCMYKFromColor(iasym.FillColor);
            }
            else if (sym is RectangleSymbol) {
                RectangleSymbol rectsym = sym as RectangleSymbol;

                obj.Otp = (byte)((version >= 9) ? 7 : 5);
                obj.Ang = AngleToOcad(rectsym.Rotation);
                obj.coords = CoordsFromSymPath(rectsym.Path);
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
            if (obj.objectString != null) {
                obj.nObjectString = (short)((obj.objectString.Length / 4) + 1);
            }
            if (obj.databaseString != null) {
                obj.nDatabaseString = (short)((obj.databaseString.Length / 4) + 1);
            }

            obj.Write(writer, version);
            return obj.nItem + obj.nText + obj.nObjectString + obj.nDatabaseString;
        }

        // Create the parameter strings for the templates.
        List<OcadParamString> CreateTemplateStringParameters(IList<TemplateInfo> templates)
        {
            List<OcadParamString> paramStrings = new List<OcadParamString>(templates.Count);

            foreach (TemplateInfo template in templates) {
                float mmPerPixel;
                if (template.dpi > 0)
                    mmPerPixel = 25.4F / template.dpi;
                else
                    mmPerPixel = 0;

                bool outputShear = template.shearAngle != template.angle;
                
                OcadParamString paramString = new OcadParamString();
                paramString.StType = (int)OcadStringParam.Template;
                paramString.ObjIndex = 0;
                paramString.codes = new char[outputShear ? 8 : 7];
                paramString.values = new string[outputShear ? 8 : 7];
                paramString.firstField = template.absoluteFileName;
                paramString.codes[0] = 'r';
                paramString.values[0] = template.visible ? "1" : "0";  // visible
                paramString.codes[1] = 's';
                paramString.values[1] = template.visible ? "1" : "0";  //visible
                float centerX = template.centerPoint.X;
                float centerY = template.centerPoint.Y;
                if (version == 8) {
                    // OCAD 8 stores centerX and centerY at 100X.
                    centerX = centerX * 100;
                    centerY = centerY * 100;
                }
                paramString.codes[2] = 'x';
                paramString.values[2] = centerX.ToString(CultureInfo.InvariantCulture);   // offset X
                paramString.codes[3] = 'y';
                paramString.values[3] = centerY.ToString(CultureInfo.InvariantCulture);  // offset Y
                paramString.codes[4] = 'a';
                paramString.values[4] = template.angle.ToString(CultureInfo.InvariantCulture);  // angle
                float pixelX = mmPerPixel * template.scaleX;
                float pixelY = mmPerPixel * template.scaleY;
                if (version == 8) {
                    // OCAD 8 stores pixelX and pixelY at 100X.
                    pixelX = pixelX * 100;
                    pixelY = pixelY * 100;
                }
                paramString.codes[5] = 'u';
                paramString.values[5] = pixelX.ToString(CultureInfo.InvariantCulture); // dpi x
                paramString.codes[6] = 'v';
                paramString.values[6] = pixelY.ToString(CultureInfo.InvariantCulture); // dpi y
                if (outputShear) {
                    paramString.codes[7] = 'b';
                    paramString.values[7] = template.shearAngle.ToString(CultureInfo.InvariantCulture);  // angle
                }
                paramStrings.Add(paramString);
            }

            return paramStrings;
        }

        // Create parameter strings for the GPS reference points.
        List<OcadParamString> CreateGpsReferenceInfoStringParameters(GpsReferenceInfo gpsReferenceInfo)
        {
            List<OcadParamString> paramStrings = new List<OcadParamString>(gpsReferenceInfo.ReferencePoints.Count + 1);

            OcadParamString paramString = new OcadParamString();
            paramString.StType = (int)OcadStringParam.GpsAdjustPar;
            paramString.ObjIndex = 0;
            paramString.codes = new char[3];
            paramString.values = new string[3];
            paramString.codes[0] = 'm';
            paramString.values[0] = gpsReferenceInfo.GpsReferenceOn ? "1" : "0";  // on
            paramString.codes[1] = 'n';
            paramString.values[2] = gpsReferenceInfo.ReferencePoints.Count.ToString(CultureInfo.InvariantCulture);  // number of reference points
            paramString.codes[2] = 'a';
            paramString.values[2] = gpsReferenceInfo.Angle.ToString(CultureInfo.InvariantCulture);  // angle
            paramStrings.Add(paramString);

            foreach (GpsReferencePoint gpsReferencePoint in gpsReferenceInfo.ReferencePoints) {
                paramString = new OcadParamString();
                paramString.StType = (int)OcadStringParam.GpsAdjustPoints;
                paramString.ObjIndex = 0;
                paramString.firstField = gpsReferencePoint.Name;
                paramString.codes = new char[5];
                paramString.values = new string[5];
                paramString.codes[0] = 'x';
                paramString.values[0] = gpsReferencePoint.MapCoord.X.ToString(CultureInfo.InvariantCulture);
                paramString.codes[1] = 'y';
                paramString.values[1] = gpsReferencePoint.MapCoord.Y.ToString(CultureInfo.InvariantCulture);
                paramString.codes[2] = 'h';
                paramString.values[2] = gpsReferencePoint.Longitude.ToString(CultureInfo.InvariantCulture);
                paramString.codes[3] = 'v';
                paramString.values[3] = gpsReferencePoint.Latitude.ToString(CultureInfo.InvariantCulture);
                paramString.codes[4] = 'c';
                paramString.values[4] = gpsReferencePoint.Active ? "1" : "0";
                paramStrings.Add(paramString);
            }

            return paramStrings;
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
                paramString.values[5] = color.OverPrint ? "1" : "0";

                paramStrings.Add(paramString);
            }

            return paramStrings;
        }

        List<OcadParamString> CreateFileInformationStringParameters()
        {
            List<OcadParamString> paramStrings = new List<OcadParamString>();
            string fileInfo = map.FileInformation;
            if (fileInfo != null && fileInfo.EndsWith("\r\n", StringComparison.Ordinal))
                fileInfo = fileInfo.Substring(0, fileInfo.Length - 2);

            if (!string.IsNullOrEmpty(fileInfo)) {
                string[] lines = fileInfo.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                foreach (string line in lines) {
                    OcadParamString paramString = new OcadParamString();
                    paramString.StType = (int)OcadStringParam.FileInfo;
                    paramString.firstField = line;
                    paramString.codes = new char[0];
                    paramString.values = new string[0];
                    paramStrings.Add(paramString);
                }
            }

            return paramStrings;
        }

        // Create the parameter string for the scale.
        OcadParamString CreateScaleParameter()
        {
            RealWorldCoords realWorldCoords = map.RealWorldCoords;

            int numParams = version >= 10 ? 9 : 7;
            if (realWorldCoords.RealWorldGridAndZone != 0)
                ++numParams;

            OcadParamString paramString = new OcadParamString();
            paramString.StType = (int) OcadStringParam.ScalePar;
            paramString.ObjIndex = 0;
            paramString.codes = new char[numParams];
            paramString.values = new string[numParams];
            paramString.codes[0] = 'm';
            paramString.values[0] = map.MapScale.ToString(CultureInfo.InvariantCulture);
            paramString.codes[1] = 'g';
            paramString.values[1] = realWorldCoords.PaperGridDistance.ToString(CultureInfo.InvariantCulture);   // grid scale
            paramString.codes[2] = 'r';
            paramString.values[2] = realWorldCoords.RealWorldOn ? "1" : "0";
            paramString.codes[3] = 'x';
            paramString.values[3] = realWorldCoords.RealWorldOffsetX.ToString(CultureInfo.InvariantCulture);   // real world X
            paramString.codes[4] = 'y';
            paramString.values[4] = realWorldCoords.RealWorldOffsetY.ToString(CultureInfo.InvariantCulture);   // real world Y
            paramString.codes[5] = 'a';
            paramString.values[5] = realWorldCoords.RealWorldAngle.ToString(CultureInfo.InvariantCulture);   // real world angle
            paramString.codes[6] = 'd';
            paramString.values[6] = realWorldCoords.RealWorldGridDistance.ToString(CultureInfo.InvariantCulture);   // real world grid
            if (version >= 10) {
                paramString.codes[7] = 'b';
                paramString.values[7] = realWorldCoords.RealWorldLocalOffsetX.ToString(CultureInfo.InvariantCulture);   // real world angle
                paramString.codes[8] = 'c';
                paramString.values[8] = realWorldCoords.RealWorldLocalOffsetY.ToString(CultureInfo.InvariantCulture);   // real world grid
            }
            if (realWorldCoords.RealWorldGridAndZone != 0) {
                paramString.codes[numParams - 1] = 'i';
                paramString.values[numParams - 1] = realWorldCoords.RealWorldGridAndZone.ToString(CultureInfo.InvariantCulture);
            }

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

        // Create the parameter string for the print parameters.
        OcadParamString CreateViewParameters()
        {
            int numCodes = 1;
            if (version >= 11)
                numCodes += 1;

            OcadParamString paramString = new OcadParamString();
            paramString.StType = (int)OcadStringParam.ViewPar;
            paramString.ObjIndex = 0;
            paramString.codes = new char[numCodes];
            paramString.values = new string[numCodes];
            paramString.codes[0] = 'd';
            paramString.values[0] = map.HideTemplates ? "1" : "0";
            if (version >= 11) {
                paramString.codes[1] = 'p';
                paramString.values[1] = map.UseEuclideanMetric ? "0" : "1";
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
                paramStrings.Add(CreateViewParameters());
                paramStrings.AddRange(CreateFileInformationStringParameters());
            }

            if (version >= 11) {
                // Add more string parameters.
                paramStrings.AddRange(CreateLayoutObjectParameters());
            }

            IList<TemplateInfo> templates = map.Templates;
            if (templates.Count > 0)
                paramStrings.AddRange(CreateTemplateStringParameters(templates));

            GpsReferenceInfo gpsReferenceInfo = map.GpsReferenceInfo;
            if (gpsReferenceInfo.ReferencePoints.Count > 0)
                paramStrings.AddRange(CreateGpsReferenceInfoStringParameters(gpsReferenceInfo));

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

        struct LayoutObject
        {
            public readonly int ObjectIndex;
            public readonly Symbol Symbol;
            public LayoutObject(int objectIndex, Symbol symbol)
            {
                this.ObjectIndex = objectIndex;
                this.Symbol = symbol;
            }
        }

        struct LayoutObjectFont: IEquatable<LayoutObjectFont>
        {
            public readonly string FontName;
            public readonly float FontSize;

            public LayoutObjectFont(string fontName, float fontSize)
            {
                this.FontName = fontName;
                this.FontSize = fontSize;
            }

            public bool Equals(LayoutObjectFont other)
            {
                return other.FontName == FontName && other.FontSize == FontSize;
            }

            public override int GetHashCode()
            {
                return FontName.GetHashCode() ^ FontSize.GetHashCode();
            }
        }

        private int GetLayoutFontIndex(string fontName, float fontSize)
        {
            LayoutObjectFont font = new LayoutObjectFont(fontName, fontSize);
            int index = layoutFonts.IndexOf(font);
            if (index >= 0) {
                return index;
            }
            else {
                layoutFonts.Add(font);
                return layoutFonts.Count - 1;
            }
        }

        List<OcadParamString> CreateLayoutObjectParameters()
        {
            // Sort layout objects by inverse sort order (top first)
            layoutObjects.Sort((LayoutObject lo1, LayoutObject lo2) => lo2.Symbol.SortOrder.CompareTo(lo1.Symbol.SortOrder));

            List<OcadParamString> paramStrings = new List<OcadParamString>();

            foreach (LayoutObject lo in layoutObjects) {
                Symbol sym = lo.Symbol;
                OcadParamString paramString = new OcadParamString();

                paramString.StType = (int)OcadStringParam.LayoutObjects;
                paramString.ObjIndex = lo.ObjectIndex;
                int numCodes = 3;

                if (sym is ImageLineSymbol)
                    paramString.firstField = "Line object";
                else if (sym is ImageAreaSymbol)
                    paramString.firstField = "Area object";
                else if (sym is ImageTextSymbol)
                    paramString.firstField = string.Format("Text object ({0})", string.Join("", ((ImageTextSymbol)sym).Text));
                else if (sym is ImageBitmapSymbol) {
                    paramString.firstField = ((ImageBitmapSymbol)sym).FileName;
                    numCodes = 6;
                    if (((ImageBitmapSymbol)sym).EmbeddedData != null) {
                        numCodes += 1;
                    }
                }

                paramString.codes = new char[numCodes];
                paramString.values = new string[numCodes];

                if (sym is ImageBitmapSymbol) {
                    ImageBitmapSymbol bmSym = (ImageBitmapSymbol)sym;
                    paramString.codes[0] = 'r';
                    paramString.values[0] = "1";
                    paramString.codes[1] = 's';
                    paramString.values[1] = bmSym.IsVisible ? "1" : "0";
                    paramString.codes[2] = 'x';
                    paramString.values[2] = bmSym.Location.X.ToString(CultureInfo.InvariantCulture);
                    paramString.codes[3] = 'y';
                    paramString.values[3] = bmSym.Location.Y.ToString(CultureInfo.InvariantCulture);
                    paramString.codes[4] = 'u';
                    paramString.values[4] = bmSym.MmPerPixX.ToString(CultureInfo.InvariantCulture);
                    paramString.codes[5] = 'v';
                    paramString.values[5] = bmSym.MmPerPixY.ToString(CultureInfo.InvariantCulture);
                    if (bmSym.EmbeddedData != null) {
                        paramString.codes[6] = 'F';
                        paramString.values[6] = Convert.ToBase64String(bmSym.EmbeddedData);
                    }
                }
                else {
                    paramString.codes[0] = 'r';
                    paramString.values[0] = "0";
                    paramString.codes[1] = 's';
                    paramString.values[1] = sym.IsVisible ? "1" : "0";
                    paramString.codes[2] = 'n';
                    paramString.values[2] = lo.ObjectIndex.ToString(CultureInfo.InvariantCulture);
                }

                paramStrings.Add(paramString);
            }

            // Only version 11 uses the layout font attributes.
            if (version == 11) {
                foreach (LayoutObjectFont font in layoutFonts) {
                    OcadParamString paramString = new OcadParamString();
                    paramString.StType = (int)OcadStringParam.LayoutFontAttributes;
                    paramString.firstField = font.FontName;
                    paramString.codes = new char[1];
                    paramString.values = new string[1];
                    paramString.codes[0] = 's';
                    paramString.values[0] = (font.FontSize / 25.4F * 72F).ToString(CultureInfo.InvariantCulture);

                    paramStrings.Add(paramString);
                }
            }
            return paramStrings;
        }
    }
}
