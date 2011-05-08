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

namespace PurplePen.MapModel
{
    class OcadFileFormatException: ApplicationException
    {
        public OcadFileFormatException(string message, params object[] arguments) :
            base(string.Format(message, arguments))
        {}
    }

    class OcadImport {
        Map map;
        BinaryReader reader;
        string filename;        // File name being read.
        int version;       // OCAD version being read
        long totalFileSize;        // Total length of file, for validating offsets.

        // Map the symbol colors to SymColor instances.
        Dictionary<int, SymColor> mapOcadIdToSymColor = new Dictionary<int, SymColor>();

        // Keys = symbol def numberic ids.
        // Values = symdef instances.
        Dictionary<int, SymDef> symdefids = new Dictionary<int, SymDef>();

        // The one and only GraphicsSymDef, if any. Created on demand if needed.
        GraphicsSymDef graphicsSymDef = null;

        // The one and only ImageSymDef, if any. Created on demand if needed.
        ImageSymDef imageSymDef = null;

        // Keys = symdef def number ids for a rectangle symbol
        // Values = RectangleInfo classes that describe the rectangle symbol.
        Dictionary<int, RectangleInfo> rectangleInfos = new Dictionary<int, RectangleInfo>();

        // Font names that aren't present.
        Dictionary<string, bool> missingFontNames = new Dictionary<string, bool>();

        // Symdefs that can't be rendered correctly --- symdef maps to a list of strings that describe the problem.
        Dictionary<SymDef, List<string>> nonRenderableSymdefs = new Dictionary<SymDef, List<string>>();

        // Symbols that can't be rendered correct -- match an error message (which includes a symdef name) to number of instances.
        Dictionary<string, int> nonRenderableObjects = new Dictionary<string, int>();

        int ocadIdNext = 999950;  // next OCAD id to assign a synthetic symbol.

        public OcadImport(Map map) {
            this.map = map;
        }

        // Convert from OCAD coords (int in units of .01 mm) to world coord (float in mm)
        static float ToWorldDimensions(int ocadDimen) {
            return (float) (((double)ocadDimen) / 100.0);
        }

        // Convert from OCAD ang (0.1 degrees) to degrees
        static float AngleToDegrees(int ocadAngle) {
            ocadAngle = ocadAngle % 3600;
            return (float) ocadAngle * 0.1F;
        }

        static ToolboxIcon ConvertOcadIcon(byte[] bytes) {
            ToolboxIcon icon = new ToolboxIcon();

            for (int i = 0; i < 24; ++i) {
                for (int j = 0; j < 24; ++j) {

                    // Ocad bitmap is 22x22 -- border is transparent
                    if (i == 0 || i == 23 || j == 0 || j == 23)
                        icon.SetPixel(i, j, Color.Transparent);
                    else {
                        int x = i-1, y = 22-j; // coords in ocad bitmap.
                        byte ocadPixel;
                        if ((x & 1) == 0) 
                            ocadPixel = (byte) (bytes[y * 12 + (x/2)] >> 4);
                        else
                            ocadPixel = (byte) (bytes[y * 12 + (x/2)] & 0xF);

                        icon.SetPixel(i, j, OcadConstants.ocadColorMap4Bit[ocadPixel]);
                    }
                }
            }

            return icon;
        }

        static ToolboxIcon ConvertOcad9Icon(byte[] bytes)
        {
            ToolboxIcon icon = new ToolboxIcon();

            for (int i = 0; i < 24; ++i) {
                for (int j = 0; j < 24; ++j) {

                    // Ocad bitmap is 22x22 -- border is transparent
                    if (i == 0 || i == 23 || j == 0 || j == 23)
                        icon.SetPixel(i, j, Color.Transparent);
                    else {
                        int x = i-1, y = 22-j; // coords in ocad bitmap.
                        byte ocadPixel = bytes[y * 22 + x];
                        if (ocadPixel >= OcadConstants.ocadColorMap8Bit.Length)
                            icon.SetPixel(i, j, Color.White);
                        else
                            icon.SetPixel(i, j, OcadConstants.ocadColorMap8Bit[ocadPixel]);
                    }
                }
            }

            return icon;
        }

        static ToolboxIcon ConvertCompressedOcadIcon(byte[] bytes)
        {
            byte[] compressed = new byte[248];
            byte[] uncompressed = new byte[22 * 22];

            Array.Copy(bytes, 16, compressed, 0, 248);
            LZWCompression compressor = new LZWCompression();
            compressor.Expand(compressed, uncompressed);

            return ConvertOcad9Icon(uncompressed);
        }

        // See if this looks like an ocad file.
        public static bool IsOcadFile(Stream stm) {
            int byte1 = stm.ReadByte();
            int byte2 = stm.ReadByte();
            stm.Seek(-2, SeekOrigin.Current);
            return (byte1 == 0xAD && byte2 == 0x0C);
        }

        // Read an OCAD file from a stream, and returns the version number (6,7,8).
        public int ReadOcadFile(Stream stm, string filename) {
            // Get the total size of the stream.
            totalFileSize = stm.Length;

            using (reader = new BinaryReader(stm, Encoding.GetEncoding(1252))) 
            using (map.Write()) {
                map.Clear();

                // Read the header and color information, and the setup block.
                this.filename = filename;
                OcadFileHeader header = ReadFileHeader();
                version = header.Version;

                if (version < 6 || version > 10) 
                    throw new OcadFileFormatException("File is in OCAD {0} format. Only OCAD formats 6, 7, 8, 9, and 10 are supported.", version);

                if (version <= 8) {
                    // Only version 8 and less have the symbol header and setup structures. OCAD 9
                    // puts this information into the string parameters.
                    OcadSymbolHeader symheader = ReadSymbolHeader();
                    OcadSetup setup = ReadSetup(header);

                    // Get the scale of the map.
                    map.MapScale = (float) setup.MapScale;
                    map.PrintScale = (float) setup.PrintScale;

                    // Get the print area of the map.
                    map.PrintArea = RectangleF.FromLTRB(
                        PointFromOcadCoord(setup.PrLowerLeft).X, PointFromOcadCoord(setup.PrLowerLeft).Y,
                        PointFromOcadCoord(setup.PrUpperRight).X, PointFromOcadCoord(setup.PrUpperRight).Y);

                    // Get the template infomation out of the setup (if OCAD 7 or less).
                    if (version <= 7) {
                        map.Template = ReadTemplateInfo(setup);
                    }

                    CreateColors(symheader);

                    // Save away the symbol header and setup structure for round-trip purposes.
                    for (int i = 0; i < symheader.aColorInfo.Length; ++i)
                        symheader.aColorInfo[i] = null;
                    symheader.nColors = 0;

                    map.OcadSymbolHeaderStructure = symheader;
                    map.OcadSetupStructure = setup;
                }

                if (version >= 8) {
                    // Read the string parameters into paramStrings and paramStringLists
                    OcadParamString[] simpleStringParameters;
                    List<OcadParamString>[] listStringParameters;
                    ReadStringParameters(header.FirstStIndexBlk, out simpleStringParameters, out listStringParameters);

                    if (version >= 9) {
                        // Get the map scale
                        if (simpleStringParameters[OcadStringParam.ScalePar - OcadStringParam.FirstSingleParam] != null) {
                            map.MapScale = GetParamFloat(simpleStringParameters[OcadStringParam.ScalePar - OcadStringParam.FirstSingleParam], 'm', 10000.0F);
                        }
                        
                        // Get the print scale and print area
                        if (simpleStringParameters[OcadStringParam.PrintPar - OcadStringParam.FirstSingleParam] != null) {
                            map.PrintScale = GetParamFloat(simpleStringParameters[OcadStringParam.PrintPar - OcadStringParam.FirstSingleParam], 'a', map.MapScale);

                            float l, b, t, r;
                            l = GetParamFloat(simpleStringParameters[OcadStringParam.PrintPar - OcadStringParam.FirstSingleParam], 'L', 0F);
                            t = GetParamFloat(simpleStringParameters[OcadStringParam.PrintPar - OcadStringParam.FirstSingleParam], 'B', 0F);  // note reversed Y
                            r = GetParamFloat(simpleStringParameters[OcadStringParam.PrintPar - OcadStringParam.FirstSingleParam], 'R', 0F);
                            b = GetParamFloat(simpleStringParameters[OcadStringParam.PrintPar - OcadStringParam.FirstSingleParam], 'T', 0F);  // note reversed Y
                            map.PrintArea = RectangleF.FromLTRB(l, t, r, b);
                        }

                        // Create the colors in the map, populating the "colors" array.
                        if (listStringParameters[(int) OcadStringParam.Color] != null)
                            CreateColors(listStringParameters[(int) OcadStringParam.Color]);
                    }

                    if (listStringParameters[(int) OcadStringParam.Template] != null) {
                        // Only import the first template right now.
                        map.Template = ReadTemplateInfo(listStringParameters[(int) OcadStringParam.Template][0]);
                    }
                }

                // Read the symbols (symdefs in our terminology)
                OcadSymbolBlocks symblocks = ReadSymbolBlocks(header.FirstSymBlk);
                OcadSymbol[] symbols = ReadSymbols(symblocks);

                // Create the symdefs in the map, populating the "symdefids" hashtable.
                CreateSymdefs(symbols);

                // Read and create the objects (symbols in our terminology)
                OcadIndexBlocks indexblocks = ReadIndexBlocks(header.FirstIdxBlk);
                ReadAndCreateObjects(indexblocks);

                // Set the missing fonts in the new map.
                map.missingFonts = new List<string>(this.missingFontNames.Keys);

                // Set the non-renderable objects.
                map.nonRenderableObjects = new List<string>();
                foreach (KeyValuePair<string, int> pair in this.nonRenderableObjects) 
                    map.nonRenderableObjects.Add(string.Format("{0}{1} object{2})", pair.Key, pair.Value, pair.Value >= 2 ? "s": ""));

                return version;
            }
        }

        OcadFileHeader ReadFileHeader() {
            OcadFileHeader fh = new OcadFileHeader();
            fh.Read(reader);

            return fh;
        }

        OcadSymbolHeader ReadSymbolHeader() {
            OcadSymbolHeader sh = new OcadSymbolHeader();
            sh.Read(reader);
            return sh;
        }

        OcadSetup ReadSetup(OcadFileHeader fh) {
            OcadSetup setup = new OcadSetup();
            setup.Read(reader, fh.SetupPos, fh.SetupSize);
            return setup;
        }

        // Get the template infomation about the setup struction into a TemplateInfo class.
        // Return null if no template is there.
        TemplateInfo ReadTemplateInfo(OcadSetup setup)
        {
            if (setup.TemplateEnabled != 0 && setup.TemplateFileName != null && setup.TemplateFileName.Length > 0) {
                // template name might be relative. Make it absolute.
                string absoluteFileName = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(this.filename), setup.TemplateFileName));
                return new TemplateInfo(absoluteFileName, PointFromOcadCoord(setup.TempOffset), (float) setup.TempResol,
                                                         (float) (setup.rTempAng * 180.0 / Math.PI), setup.HideTemp == 0);
            }
            else {
                return null;
            }
        }

        // Get the template information from a string parameter into a TemplateInfo class.
        TemplateInfo ReadTemplateInfo(OcadParamString paramString)
        {
            string relativeFileName = paramString.firstField;
            string absoluteFileName;

            try {
                absoluteFileName = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(this.filename), relativeFileName));
            }
            catch (Exception) {
                // An argument exception occurs in the template info has bad characters. Other weird exceptions are possible.
                // Just use the relative name in this case (kind of a punt, but hey...)
                absoluteFileName = relativeFileName;
            }

            float offsetX = GetParamFloat(paramString, 'x', 0F);
            float offsetY = GetParamFloat(paramString, 'y', 0F);
            float angle = GetParamFloat(paramString, 'a', 0F);
            float dpi = 25.4F / GetParamFloat(paramString, 'u', 0F);
            bool enabled = (GetParamInt(paramString, 's', 1) != 0);

            return new TemplateInfo(absoluteFileName, new PointF(offsetX, offsetY), dpi, angle, enabled);
        }

        // Read all the string parameters into arrays.
        void ReadStringParameters(int firstStIndexBlk, out OcadParamString[] simpleStringParameters, out List<OcadParamString>[] listStringParameters)
        {
            // Allocate the arrays of objects to create.
            simpleStringParameters = new OcadParamString[(int) OcadStringParam.CountSingleParam];
            listStringParameters = new List<OcadParamString>[(int) OcadStringParam.CountListParam];

            if (firstStIndexBlk == 0)
                return;

            OcadStIndexBlocks stindexblocks = new OcadStIndexBlocks();
            stindexblocks.Read(reader, firstStIndexBlk);

            if (stindexblocks == null) 
                return;

            for (int i = 0; i < stindexblocks.indexes.Length; ++i) {
                OcadParamString param = new OcadParamString();
                param.Read(reader, stindexblocks.indexes[i]);

                // Sometimes the "zoom" (12) parameter shows up as -12. Not sure why -- bug in OCAD?
                if (param.StType < 0)
                    param.StType = -param.StType;

                OcadStringParam stringType = (OcadStringParam) param.StType;

                if (stringType != 0) {
                    if (stringType < OcadStringParam.CountListParam) {
                        if (listStringParameters[(int) stringType] == null)
                            listStringParameters[(int) stringType] = new List<OcadParamString>();
                        listStringParameters[(int) stringType].Add(param);
                    }
                    else if (stringType < OcadStringParam.LastSingleParam && stringType >= OcadStringParam.FirstSingleParam) {
                        simpleStringParameters[stringType - OcadStringParam.FirstSingleParam] = param;
                    }
                }
            }
        }

        // Get a string parameter out of an OcadParamString. Use \0 to get the first field.
        // Returns null if passed null or if the parameter doesn't exist.
        string GetParamString(OcadParamString paramString, char code)
        {
            if (paramString == null)
                return null;

            if (code == '\0')
                return paramString.firstField;

            if (paramString.codes == null)
                return null;

            for (int i = 0; i < paramString.codes.Length; ++i) {
                if (paramString.codes[i] == code)
                    return paramString.values[i];
            }

            return null;
        }

        // Get a string parameter out of an OcadParamString. Use \0 to get the first field.
        // Returns defaultValue if passed null or if the parameter doesn't exist.
        int GetParamInt(OcadParamString paramString, char code, int defaultValue)
        {
            string s = GetParamString(paramString, code);
            if (s == null)
                return defaultValue;

            int i;
            if (int.TryParse(s, out i))
                return i;
            else
                return defaultValue;
        }

        // Get a float parameter out of an OcadParamString. Use \0 to get the first field.
        // Returns defaultValue if passed null or if the parameter doesn't exist.
        float GetParamFloat(OcadParamString paramString, char code, float defaultValue)
        {
            string s = GetParamString(paramString, code);
            if (s == null)
                return defaultValue;

            float f;
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
                return f;
            else
                return defaultValue;
        }

        // Create the SymColor objects in the map, and also cache them in the colors array.
        void CreateColors(OcadSymbolHeader symheader) {
            for (int i = symheader.nColors - 1; i >= 0 ; --i) {
                OcadColorInfo ocadColorInfo = symheader.aColorInfo[i];
                SymColor color;
                color = map.AddColor(ocadColorInfo.ColorName, ocadColorInfo.ColorNum,
                    (float)(ocadColorInfo.Color.cyan)/200.0F,
                    (float)(ocadColorInfo.Color.magenta)/200.0F,
                    (float)(ocadColorInfo.Color.yellow)/200.0F,
                    (float)(ocadColorInfo.Color.black)/200.0F);
                mapOcadIdToSymColor[ocadColorInfo.ColorNum] = color;
            }
        }

        // Create the SymColor objects in the map, and also cache them in the colors array.
        void CreateColors(List<OcadParamString> paramStrings)
        {
            if (paramStrings == null)
                return;

            for (int i = paramStrings.Count - 1; i >= 0; --i) {
                OcadParamString paramString = paramStrings[i];

                string colorName = GetParamString(paramString, '\0');
                int colorNum = GetParamInt(paramString, 'n', 0);
                float cyan = GetParamFloat(paramString, 'c', 0.0F) / 100.0F;
                float magenta = GetParamFloat(paramString, 'm', 0.0F) / 100.0F;
                float yellow = GetParamFloat(paramString, 'y', 0.0F) / 100.0F;
                float black = GetParamFloat(paramString, 'k', 0.0F) / 100.0F;

                SymColor color;
                color = map.AddColor(colorName, (short) colorNum, cyan, magenta, yellow, black);
                mapOcadIdToSymColor[colorNum] = color;
            }
        }

        // Get a color, use topmost color if not there. This mimics what OCAD does with a missing color index, which happens fairly often.
        SymColor GetColor(int ocadId)
        {
            if (!mapOcadIdToSymColor.ContainsKey(ocadId)) {
                List<SymColor> colors = colors = new List<SymColor>(map.AllColors);
                return colors[colors.Count - 1];
            }
            else
                return mapOcadIdToSymColor[ocadId];
        }

        // Get a color, return null if not there.
        SymColor GetOptionalColor(int ocadId)
        {
            if (!mapOcadIdToSymColor.ContainsKey(ocadId))
                return null;
            else
                return mapOcadIdToSymColor[ocadId];
        }

        void CreateSymdef(OcadSymbol ocadSym)
        {
            int symid;

            // Version 9 allows 3 decimal digits in OCAD symbols.
            if (version >= 9)
                symid = ocadSym.Sym;
            else
                symid = (ocadSym.Sym / 10) * 1000 + (ocadSym.Sym % 10);

            string name = ocadSym.Description;
            SymDef symdef = null;

            switch (ocadSym.Otp) {
            default:
#if DEBUG
                throw new OcadFileFormatException("Invalid Otp value {0} in symbol {1}", ocadSym.Otp, symid);
#else
                break;
#endif
            case 1:
                symdef = CreatePointSymdef(name, symid, ocadSym as OcadPointSymbol);
                break;
            case 2:
                // Line symbols.
                // FEATURE: Line Text symbols
                if (version >= 9 || ocadSym.SymTp == 0)
                    symdef = CreateLineSymdef(name, symid, ocadSym as OcadLineSymbol);
                else
                    symdef = CreateLineTextSymdef(name, symid, ocadSym as OcadLineTextSymbol);
                break;
            case 3:
                // Area symbols.
                symdef = CreateAreaSymdef(name, symid, ocadSym as OcadAreaSymbol);
                break;

            case 4:
                symdef = CreateTextSymdef(name, symid, ocadSym as OcadTextSymbol);
                break;
            case 5:
            case 7:
                symdef = CreateRectangleSymdef(name, symid, ocadSym as OcadRectSymbol);
                break;
            case 6:
                symdef = CreateLineTextSymdef(name, symid, ocadSym as OcadLineTextSymbol);
                break;
            }

            if (symdef != null) {
                if (version >= 9)
                    symdef.ToolboxImage = ConvertOcad9Icon(ocadSym.IconBits);
                else if ((ocadSym.Flags & 2) != 0)
                    symdef.ToolboxImage = ConvertCompressedOcadIcon(ocadSym.IconBits);
                else
                    symdef.ToolboxImage = ConvertOcadIcon(ocadSym.IconBits);

                map.AddSymdef(symdef);
                symdefids[symid] = symdef;

                if (ocadSym.Status == 2) {
                    // symbol is hidden.
                    map.SetSymdefVisible(symdef, false);
                }
            }
        }

        void CreateSymdefs(OcadSymbol[] symbols)
        {
            // We need to read area and text symdefs after all others, because area symdefs can depend on line symdefs and text symdefs can depend on point symdefs.
            foreach (OcadSymbol ocadSym in symbols) {
                if (ocadSym.Otp != 3 && ocadSym.Otp != 4)
                    CreateSymdef(ocadSym);
            }

            foreach (OcadSymbol ocadSym in symbols) {
                if (ocadSym.Otp == 3 || ocadSym.Otp == 4)
                    CreateSymdef(ocadSym);
            }
        }

        Glyph CreateGlyph(OcadSymbolElt[] elts) {
            if (elts == null)
                return null;

            Glyph glyph = new Glyph();

            foreach (OcadSymbolElt elt in elts) {
                SymColor color = GetColor(elt.stColor);

                switch (elt.stType) {
                case 1: // line
                    glyph.AddLine(color, CreateSymPath(elt.stCoords), ToWorldDimensions(elt.stLineWidth), ImportLineStyle((short) elt.stFlags));
                    break;
                case 2: // area
                    SymPathWithHoles areaPath = CreateAreaSymPath(elt.stCoords);
                    glyph.AddArea(color, areaPath);
                    break;
                case 3: // circle
                    float width = ToWorldDimensions(elt.stLineWidth);
                    float diameter = ToWorldDimensions(elt.stDiameter);
                    if (version >= 9)
                        diameter += width;              // diameter is from middle of line in OCAD 9+, from outer edges in OCAD 6-8.
                    glyph.AddCircle(color, PointFromOcadCoord(elt.stCoords[0]), width, diameter);
                    break;
                case 4:
                    glyph.AddFilledCircle(color, PointFromOcadCoord(elt.stCoords[0]), ToWorldDimensions(elt.stDiameter));
                    break;
                default:
#if DEBUG
                    throw new OcadFileFormatException("Invalid symbol element kind {0}", elt.stType);
#else
                    break;
#endif
                }
            }

            glyph.ConstructionComplete();

            return glyph;
        }

        SymDef CreatePointSymdef(string name, int ocadID, OcadPointSymbol ocadSym) {
            bool allowRotation = ((ocadSym.Flags & 1) != 0);
            Glyph glyph = CreateGlyph(ocadSym.symbolElts);
            return new PointSymDef(name, ocadID, glyph, allowRotation);
        }

        LineStyle ImportLineStyle(short lineEnds)
        {
            if (lineEnds == 1)
                return LineStyle.Rounded;
            else if (lineEnds == 4)
                return LineStyle.Mitered;
            else
                return LineStyle.Beveled;
        }

        SymDef CreateLineSymdef(string name, int ocadID, OcadLineSymbol ocadSym) {
            float width = ToWorldDimensions(ocadSym.LineWidth);

            SymColor color = null;
            if (width > 0)
                color = GetColor(ocadSym.LineColor);

            LineStyle lineStyle = ImportLineStyle((short) ocadSym.LineEnds);

            LineSymDef symdef = new LineSymDef(name, ocadID, color, width, lineStyle);

            if (ocadSym.DistFromStart != 0 || ocadSym.DistToEnd != 0) {
                LineSymDef.ShortenInfo shortenInfo;
                shortenInfo.shortenBeginning = ToWorldDimensions(ocadSym.DistFromStart);
                shortenInfo.shortenEnd = ToWorldDimensions(ocadSym.DistToEnd);
                shortenInfo.pointyEnds = ((ocadSym.LineEnds & 2) != 0);

                symdef.SetShortening(shortenInfo);
            }

            if (ocadSym.FrWidth > 0) {
                SymColor secondColor = GetColor(ocadSym.FrColor);
                float secondWidth = ToWorldDimensions(ocadSym.FrWidth);
                LineStyle secondLineStyle = ImportLineStyle(ocadSym.FrStyle);

                symdef.SetSecondLine(secondColor, secondWidth, secondLineStyle);
            }

            if (ocadSym.DblMode > 0) {
                LineSymDef.DoubleLineInfo doubleLines;
                if ((ocadSym.DblFlags & 1) != 0) {
                    doubleLines.doubleFillColor = GetColor(ocadSym.DblFillColor);
                }
                else {
                    doubleLines.doubleFillColor = null;
                }

                doubleLines.doubleLeftColor = GetColor(ocadSym.DblLeftColor);
                doubleLines.doubleRightColor = GetColor(ocadSym.DblRightColor);
                doubleLines.doubleThick = ToWorldDimensions(ocadSym.DblWidth);
                doubleLines.doubleLeftWidth = ToWorldDimensions(ocadSym.DblLeftWidth);
                doubleLines.doubleRightWidth = ToWorldDimensions(ocadSym.DblRightWidth);

                if (ocadSym.DblMode > 1) {
                    if (ocadSym.DblMode == 2) {
                        doubleLines.doubleLeftDashed = true;
                        doubleLines.doubleFillDashed = doubleLines.doubleRightDashed = false;
                    }
                    else if (ocadSym.DblMode == 3) {
                        doubleLines.doubleLeftDashed = doubleLines.doubleRightDashed = true;
                        doubleLines.doubleFillDashed = false;
                    }
                    else {
                        doubleLines.doubleFillDashed = doubleLines.doubleLeftDashed = doubleLines.doubleRightDashed = true;
                    }

                    doubleLines.doubleDashes.dashLength = doubleLines.doubleDashes.firstDashLength = doubleLines.doubleDashes.lastDashLength = ToWorldDimensions(ocadSym.DblLength);
                    doubleLines.doubleDashes.gapLength = ToWorldDimensions(ocadSym.DblGap);
                    doubleLines.doubleDashes.minGaps = 1;
                    doubleLines.doubleDashes.secondaryEndGaps = 0;
                    doubleLines.doubleDashes.secondaryEndLength = 0;
                    doubleLines.doubleDashes.secondaryMiddleGaps = 0;
                    doubleLines.doubleDashes.secondaryMiddleLength = 0;
                }
                else {
                    doubleLines.doubleFillDashed = doubleLines.doubleLeftDashed = doubleLines.doubleRightDashed = false;
                    doubleLines.doubleDashes = new LineSymDef.DashInfo();
                }
                symdef.SetDoubleLines(doubleLines);
            }

            if (ocadSym.MainGap > 0 || ocadSym.SecGap > 0) {
                LineSymDef.DashInfo dashInfo = new LineSymDef.DashInfo();

                dashInfo.dashLength = ToWorldDimensions(ocadSym.MainLength);
                dashInfo.firstDashLength = dashInfo.lastDashLength = ToWorldDimensions(ocadSym.EndLength);
                dashInfo.gapLength = ToWorldDimensions(ocadSym.MainGap);
                dashInfo.minGaps = ocadSym.MinSym + 1;

                if (ocadSym.SecGap > 0) {
                    dashInfo.secondaryMiddleGaps = 1;
                    dashInfo.secondaryMiddleLength = ToWorldDimensions(ocadSym.SecGap);
                }
                if (ocadSym.EndGap > 0) {
                    dashInfo.secondaryEndGaps = 1;
                    dashInfo.secondaryEndLength = ToWorldDimensions(ocadSym.EndGap);
                }

                symdef.SetDashInfo(dashInfo);
            }

            int numGlyphs = 0, iGlyph = 0;
            LineSymDef.GlyphInfo[] glyphs = null;

            if (ocadSym.PrimDElts != null && ocadSym.PrimDElts.Length > 0)			++numGlyphs;
            if (ocadSym.SecDElts != null && ocadSym.SecDElts.Length > 0)			++numGlyphs;
            if (ocadSym.CornerDElts != null && ocadSym.CornerDElts.Length > 0)		++numGlyphs;
            if (ocadSym.StartDElts != null && ocadSym.StartDElts.Length > 0)		++numGlyphs;
            if (ocadSym.EndDElts != null && ocadSym.EndDElts.Length > 0)			++numGlyphs;

            if (numGlyphs > 0) {
                glyphs = new LineSymDef.GlyphInfo[numGlyphs];

                if (ocadSym.PrimDElts != null && ocadSym.PrimDElts.Length > 0) {
                    LineSymDef.GlyphInfo glyphInfo = new LineSymDef.GlyphInfo();

                    glyphInfo.glyph = CreateGlyph(ocadSym.PrimDElts);
                    glyphInfo.number = ocadSym.nPrimSym;
                    glyphInfo.spacing = ToWorldDimensions(ocadSym.PrimSymDist);

                    if (ocadSym.DecMode > 0) {
                        glyphInfo.location = LineSymDef.GlyphLocation.SpacedDecrease;
                        glyphInfo.distance = ToWorldDimensions(ocadSym.MainLength + ocadSym.MainGap);
                        glyphInfo.firstDistance = glyphInfo.lastDistance = ToWorldDimensions(ocadSym.EndLength) + (ToWorldDimensions(ocadSym.MainGap) / 2);
                        glyphInfo.minimum = Math.Max(1, ocadSym.MinSym + 1);   // OCAD always does at least one symbol, even if you say zero!
                        glyphInfo.decreaseLimit = (float) ocadSym.DecLast / 100F;
                        glyphInfo.decreaseBothEnds = (ocadSym.DecMode == 2);
                    }
                    else if (width > 0 && ocadSym.MainGap > 0 || ocadSym.SecGap > 0) {
                        glyphInfo.location = LineSymDef.GlyphLocation.GapCenters;
                        glyphInfo.minimum = 1; // OCAD always does at least one symbol, even if you say zero!
                    }
                    else {
                        glyphInfo.location = LineSymDef.GlyphLocation.Spaced;
                        glyphInfo.distance = ToWorldDimensions(ocadSym.MainLength + ocadSym.MainGap);
                        glyphInfo.firstDistance = glyphInfo.lastDistance = ToWorldDimensions(ocadSym.EndLength) + (ToWorldDimensions(ocadSym.MainGap) / 2);
                        glyphInfo.minimum = Math.Max(1, ocadSym.MinSym + 1);   // OCAD always does at least one symbol, even if you say zero!
                    }

                    glyphs[iGlyph++] = glyphInfo;
                }

                if (ocadSym.SecDElts != null && ocadSym.SecDElts.Length > 0) {
                    LineSymDef.GlyphInfo glyphInfo = new LineSymDef.GlyphInfo();
                    glyphInfo.glyph = CreateGlyph(ocadSym.SecDElts);
                    glyphInfo.number = 1;
                    glyphInfo.location = LineSymDef.GlyphLocation.SpacedOffset;
                    glyphInfo.offset = ToWorldDimensions(ocadSym.MainLength) / 2;
                    glyphInfo.distance = ToWorldDimensions(ocadSym.MainLength + ocadSym.MainGap);
                    glyphInfo.firstDistance = glyphInfo.lastDistance = ToWorldDimensions(ocadSym.EndLength) + (ToWorldDimensions(ocadSym.MainGap) / 2);
                    glyphInfo.minimum = Math.Max(1, ocadSym.MinSym + 1);   // OCAD always does at least one symbol, even if you say zero!

                    glyphs[iGlyph++] = glyphInfo;
                }

                if (ocadSym.CornerDElts != null && ocadSym.CornerDElts.Length > 0) {
                    LineSymDef.GlyphInfo glyphInfo = new LineSymDef.GlyphInfo();
                    glyphInfo.glyph = CreateGlyph(ocadSym.CornerDElts);
                    glyphInfo.number = 1;
                    glyphInfo.location = LineSymDef.GlyphLocation.Corners;

                    glyphs[iGlyph++] = glyphInfo;
                }

                if (ocadSym.StartDElts != null && ocadSym.StartDElts.Length > 0) {
                    LineSymDef.GlyphInfo glyphInfo = new LineSymDef.GlyphInfo();
                    glyphInfo.glyph = CreateGlyph(ocadSym.StartDElts);
                    glyphInfo.number = 1;
                    glyphInfo.location = LineSymDef.GlyphLocation.Start;

                    glyphs[iGlyph++] = glyphInfo;
                }

                if (ocadSym.EndDElts != null && ocadSym.EndDElts.Length > 0) {
                    LineSymDef.GlyphInfo glyphInfo = new LineSymDef.GlyphInfo();
                    glyphInfo.glyph = CreateGlyph(ocadSym.EndDElts);
                    glyphInfo.number = 1;
                    glyphInfo.location = LineSymDef.GlyphLocation.End;

                    glyphs[iGlyph++] = glyphInfo;
                }

                symdef.SetGlyphs(glyphs);
            }

            return symdef;
        }

        // Rectangle symbols don't correspond to regular SymDefs. Instead, they correspond to a 
        // line symdef describing the line information, plus an RectangleInfo that holds the extra 
        // information used only during import to create the line symbols that make up a rectangle.
        class RectangleInfo {
            public float cornerRadius;	   // if >0, the radius of corners for a rounded rectangle.
            public bool grid;			   // is there a grid?

            // The following are only valud is grid is true
            public LineSymDef gridLines;  // symdef for the grid lines, only if grid is true
            public TextSymDef gridText;   // symdef for the grid text, only if grid is true
            public bool numberFromBottom; // how to order the numbers
            public float cellWidth, cellHeight; // size of cells in the grid.
            public int unnumberedCells;   // number of unnumbered cells
            public string unnumberedText;  // test of unnumbered calls
        }

        // If a rectangle symbol include a grid, then we create extra line and text symbols
        // to handle the grid and numbers in the grid.
        void CreateRectangleGridSymdefs(string name, SymColor color, out LineSymDef lineSymdef, out TextSymDef textSymdef) {
            // UNDONE: Really should check that a real 
            // UNDONE: symbol isn't using these ids, or that there aren't already synthetic symbols that match.
            lineSymdef = new LineSymDef(name + " grid lines", ocadIdNext++, color, 0.15F, LineStyle.Beveled);
            textSymdef = new TextSymDef(name + " grid text", ocadIdNext++, null);
            textSymdef.SetFont("Arial", 15F / 72F * 25.4F, true, false, color, 0, 0, 0, 0, null, 0, 1F, TextSymDefHorizAlignment.Left, TextSymDefVertAlignment.TopAscent);
            map.AddSymdef(lineSymdef);
            map.AddSymdef(textSymdef);
        }

        // A rectange symdef is just a line symdef, but additional information is cached away.
        SymDef CreateRectangleSymdef(string name, int ocadID, OcadRectSymbol ocadSym) {
            // A rectangle symdef is just a line symbol with an additional RectangleInfo.
            SymColor color = GetColor(ocadSym.LineColor);

            float width = ToWorldDimensions(ocadSym.LineWidth);

            LineSymDef symdef = new LineSymDef(name, ocadID, color, width, ocadSym.Radius > 0 ? LineStyle.Rounded : LineStyle.Mitered);

            RectangleInfo rectinfo = new RectangleInfo();
            rectinfo.cornerRadius = ToWorldDimensions(ocadSym.Radius);

            if ((ocadSym.GridFlags & 1) != 0) {
                // we have a grid.
                rectinfo.grid = true;
                CreateRectangleGridSymdefs(name, color, out rectinfo.gridLines, out rectinfo.gridText);
                rectinfo.numberFromBottom = ((ocadSym.GridFlags & 4) != 0) ? true: false;
                rectinfo.cellWidth = ToWorldDimensions(ocadSym.CellWidth);
                rectinfo.cellHeight = ToWorldDimensions(ocadSym.CellHeight);
                rectinfo.unnumberedCells = ocadSym.UnnumCells;
                rectinfo.unnumberedText = ocadSym.UnnumText;
            }

            // remember the rectangle info for later use in creating the symbol.
            rectangleInfos[ocadID] = rectinfo;

            return symdef;
        }

        SymDef CreateAreaSymdef(string name, int ocadID, OcadAreaSymbol ocadSym) {
            SymColor color;
            AreaSymDef symdef;
            LineSymDef borderSymdef = null;

            if (ocadSym.FillOn) {
                color = GetColor(ocadSym.FillColor);
            }
            else {
                color = null;
            }

            if (ocadSym.BorderOn) {
                if (!symdefids.ContainsKey(ocadSym.BorderSym)) {
#if DEBUG
                    throw new OcadFileFormatException("Invalid border sym {0} in symbol {1}", ocadSym.BorderSym, ocadSym.Sym);
#endif
                }
                else {
                    borderSymdef = symdefids[ocadSym.BorderSym] as LineSymDef; 
                }
            }

            symdef = new AreaSymDef(name, ocadID, color, borderSymdef);

            if (ocadSym.HatchMode > 0) {
                SymColor hatchColor = GetColor(ocadSym.HatchColor);
                symdef.SetHatching(ocadSym.HatchMode, hatchColor, 
                    ToWorldDimensions(ocadSym.HatchLineWidth),
                    ToWorldDimensions((version <= 8) ? (ocadSym.HatchLineWidth + ocadSym.HatchDist) : ocadSym.HatchDist),
                    AngleToDegrees(ocadSym.HatchAngle1),
                    AngleToDegrees(ocadSym.HatchAngle2));
            }

            if (ocadSym.StructMode > 0) {
                symdef.SetPattern(true, (ocadSym.StructMode == 2), 
                    ToWorldDimensions(ocadSym.StructWidth),
                    ToWorldDimensions(ocadSym.StructHeight),
                    AngleToDegrees(ocadSym.StructAngle),
                    CreateGlyph(ocadSym.StructElts));
            }

            return symdef;
        }

        SymDef CreateTextSymdef(string name, int ocadID, OcadTextSymbol ocadSym) {
            SymColor fontColor;
            bool bold, italic;
            TextSymDef symdef;
            float fontSize;
            float paraSpacing;
            float firstIndent, restIndent;
            float charSpacing, wordSpacing;
            float[] tabs;
            TextSymDefHorizAlignment fontAlign;
            TextSymDefVertAlignment vertAlign;
            PointSymDef centerPointSymdef = null;

            fontColor = GetColor(ocadSym.FontColor);

            italic = ocadSym.Italic;
            bold = (ocadSym.Weight >= 500);

            DecodeAlignment(ocadSym.Alignment, out fontAlign, out vertAlign);

            // ocadSym.FontSize is in 10ths of a point. Convert to mm.
            fontSize = ocadSym.FontSize / 720F * 25.4F;

            paraSpacing = ToWorldDimensions(ocadSym.ParaSpace);   // paragraph spacing in mm.
            firstIndent = ToWorldDimensions(ocadSym.IndentFirst);       // indent first line in mm.
            restIndent = ToWorldDimensions(ocadSym.IndentOther);    // indent rest lines in mm.
            charSpacing = ocadSym.CharSpace / 100F;
            wordSpacing = ocadSym.WordSpace / 100F;
            if (ocadSym.LBOn) {
                paraSpacing += ToWorldDimensions(ocadSym.LBDist + ocadSym.LBWidth);   // underlining counts in paragraph spacing.
            }

            tabs = null;
            if (ocadSym.nTabs > 0) {
                tabs = new float[ocadSym.nTabs];
                for (int i = 0; i < ocadSym.nTabs; ++i)
                    tabs[i] = ToWorldDimensions(ocadSym.Tabs[i]);
            }

            if (version >= 10 && ocadSym.PointSymOn) {
                if (!symdefids.ContainsKey(ocadSym.PointSym)) {
#if DEBUG
                    throw new OcadFileFormatException("Invalid center point sym {0} in symbol {1}", ocadSym.PointSym, ocadSym.Sym);
#endif
                }
                else {
                    centerPointSymdef = symdefids[ocadSym.PointSym] as PointSymDef;
                }
            }

            symdef = new TextSymDef(name, ocadID, centerPointSymdef);

            symdef.SetFont(ocadSym.FontName, fontSize, bold, italic, fontColor, fontSize * ocadSym.LineSpace / 100F, paraSpacing, firstIndent, restIndent, tabs, charSpacing, wordSpacing, fontAlign, vertAlign);

            // handle framing.
            ReadFraming(ocadSym.FrMode, ocadSym.FrFlags, ocadSym.FrColor, ocadSym.FrWidth, ocadSym.FrSize, ocadSym.FrOfX, ocadSym.FrOfY, ocadSym.FrLeft, ocadSym.FrTop, ocadSym.FrRight, ocadSym.FrBottom, symdef);

            if (ocadSym.LBOn) {
                TextSymDef.Underlining underline = new TextSymDef.Underlining();
                underline.underlineOn = true;
                underline.underlineColor = GetColor(ocadSym.LBColor);
                underline.underlineDistance = ToWorldDimensions(ocadSym.LBDist);
                underline.underlineWidth = ToWorldDimensions(ocadSym.LBWidth);
                symdef.SetUnderline(underline);
            }

            return symdef;
        }

        // Decode the OCAD alignment enumeration into horizontal and vertical alignment parts.
        void DecodeAlignment(short ocadAlignment, out TextSymDefHorizAlignment horizAlignment, out TextSymDefVertAlignment vertAlignment) {
            vertAlignment = TextSymDefVertAlignment.Baseline;
            horizAlignment = TextSymDefHorizAlignment.Left;
            switch (ocadAlignment) {
                case 0: horizAlignment = TextSymDefHorizAlignment.Left; break;
                case 1: horizAlignment = TextSymDefHorizAlignment.Center; break;
                case 2: horizAlignment = TextSymDefHorizAlignment.Right; break;
                case 3: horizAlignment = TextSymDefHorizAlignment.Justified; break;
                case 4: if (version > 9) { horizAlignment = TextSymDefHorizAlignment.Left; vertAlignment = TextSymDefVertAlignment.Midpoint; } break;
                case 5: if (version > 9) { horizAlignment = TextSymDefHorizAlignment.Center; vertAlignment = TextSymDefVertAlignment.Midpoint; } break;
                case 6: if (version > 9) { horizAlignment = TextSymDefHorizAlignment.Right; vertAlignment = TextSymDefVertAlignment.Midpoint; } break;
                case 8: if (version > 9) { horizAlignment = TextSymDefHorizAlignment.Left; vertAlignment = TextSymDefVertAlignment.TopAscent; } break;
                case 9: if (version > 9) { horizAlignment = TextSymDefHorizAlignment.Center; vertAlignment = TextSymDefVertAlignment.TopAscent; } break;
                case 10: if (version > 9) { horizAlignment = TextSymDefHorizAlignment.Right; vertAlignment = TextSymDefVertAlignment.TopAscent; } break;
            }
        }

        // Apply framing to a text sym.
        private void ReadFraming(byte ocadFrMode, byte ocadFrFlags, short ocadFrColor, short ocadFrWidth, short ocadFrSize, short ocadFrOfX, short ocadFrOfY, short ocadFrLeft, short ocadFrTop, short ocadFrRight, short ocadFrBottom, TextSymDef symdef)
        {
            if (ocadFrMode == 2 && version >= 7) {
                // line framing
                TextSymDef.Framing framing = new TextSymDef.Framing();
                framing.framingStyle = TextSymDef.FramingStyle.Line;
                if (version == 7)
                    framing.lineWidth = ToWorldDimensions(ocadFrWidth);
                else
                    framing.lineWidth = ToWorldDimensions(ocadFrSize);
                framing.framingColor = GetColor(ocadFrColor);
                if (ocadFrFlags == 1) {
                    framing.lineStyle = LineStyle.Rounded;
                }
                else if (ocadFrFlags == 2) {
                    framing.lineStyle = LineStyle.Beveled;
                }
                else if (ocadFrFlags == 3 ||ocadFrFlags == 0) {
                    framing.lineStyle = LineStyle.Mitered;
                }

                symdef.SetFraming(framing);
            }
            else if (ocadFrMode == 1 && version >= 9) {
                // Shadow framing
                TextSymDef.Framing framing = new TextSymDef.Framing();
                framing.framingStyle = TextSymDef.FramingStyle.Shadow;
                framing.framingColor = GetColor(ocadFrColor);
                framing.shadowX = ToWorldDimensions(ocadFrOfX);
                framing.shadowY = ToWorldDimensions(ocadFrOfY);

                symdef.SetFraming(framing);
            }
            else if (ocadFrMode == 3 && version >= 9) {
                // rectangle framing
                TextSymDef.Framing framing = new TextSymDef.Framing();
                framing.framingStyle = TextSymDef.FramingStyle.Rectangle;
                framing.framingColor = GetColor(ocadFrColor);
                framing.rectBorderLeft = ToWorldDimensions(ocadFrLeft);
                framing.rectBorderRight = ToWorldDimensions(ocadFrRight);
                framing.rectBorderTop = ToWorldDimensions(ocadFrTop);
                framing.rectBorderBottom = ToWorldDimensions(ocadFrBottom);

                symdef.SetFraming(framing);
            }
            else if (ocadFrMode == 1 && version <= 7) {
                // Framing font. Just use the offset and ignore the font.
                TextSymDef.Framing framing = new TextSymDef.Framing();
                framing.framingStyle = TextSymDef.FramingStyle.Shadow;
                framing.framingColor = GetColor(ocadFrColor);
                framing.shadowX = ToWorldDimensions(ocadFrOfX);
                framing.shadowY = ToWorldDimensions(ocadFrOfY);

                symdef.SetFraming(framing);
            }
        }

        SymDef CreateLineTextSymdef(string name, int ocadID, OcadLineTextSymbol ocadSym)
        {
            SymColor fontColor;
            bool bold, italic;
            TextSymDef symdef;
            float fontSize;
            float charSpacing;
            float wordSpacing;
            TextSymDefHorizAlignment fontAlign;
            TextSymDefVertAlignment vertAlign;

            fontColor = GetColor(ocadSym.FontColor);

            italic = ocadSym.Italic;
            bold = (ocadSym.Weight >= 500);

            DecodeAlignment(ocadSym.Alignment, out fontAlign, out vertAlign);

            // ocadSym.FontSize is in 10ths of a point. Convert to mm.
            fontSize = ocadSym.FontSize / 720F * 25.4F;

            charSpacing = ocadSym.CharSpace / 100F;
            wordSpacing = ocadSym.WordSpace / 100F;

            symdef = new TextSymDef(name, ocadID, null);

            symdef.SetFont(ocadSym.FontName, fontSize, bold, italic, fontColor, fontSize, 0F, 0F, 0F, null, charSpacing, wordSpacing, fontAlign, vertAlign);

            // handle framing.
            ReadFraming(ocadSym.FrMode, ocadSym.FrFlags, ocadSym.FrColor, ocadSym.FrWidth, ocadSym.FrSize, ocadSym.FrOfX, ocadSym.FrOfY, 0, 0, 0, 0, symdef);

            return symdef;
        }

        // Get the one and only graphics symdef for this map. Used for graphics objects -- created with OCAD To Graphics command.
        GraphicsSymDef GetGraphicsSymDef()
        {
            if (graphicsSymDef == null) {
                graphicsSymDef = new GraphicsSymDef();
                map.AddSymdef(graphicsSymDef);
            }

            return graphicsSymDef;
        }

        // Get the one and only image symdef for this map. Used for iamge objects -- created with OCAD image import command.
        ImageSymDef GetImageSymDef()
        {
            if (imageSymDef == null) {
                imageSymDef = new ImageSymDef();
                map.AddSymdef(imageSymDef);
            }

            return imageSymDef;
        }

        OcadSymbolBlocks ReadSymbolBlocks(int firstBlock)
        {
            OcadSymbolBlocks sb = new OcadSymbolBlocks();
            sb.Read(reader, firstBlock);
            return sb;
        }

        OcadSymbol[] ReadSymbols(OcadSymbolBlocks b) {
            List<OcadSymbol> list = new List<OcadSymbol>();
        
            for (int i = 0; i < b.filepositions.Length; ++i) {
                if (b.filepositions[i] > 0 && b.filepositions[i] <= totalFileSize) {
                    OcadSymbol sym;
                    reader.BaseStream.Seek(b.filepositions[i], SeekOrigin.Begin);
                    sym = OcadSymbol.Read(reader, version);
                    if (sym != null)
                        list.Add(sym);
                }
            }

            return list.ToArray();
        }

        OcadIndexBlocks ReadIndexBlocks(int firstBlock) {
            OcadIndexBlocks ib = new OcadIndexBlocks();
            ib.Read(reader, firstBlock, version);
            return ib;
        }

        void ReadAndCreateObjects(OcadIndexBlocks b) {
            for (int i = 0; i < b.indexes.Length; ++i) {
                if (b.indexes[i].Sym != 0 && (version <= 8 || b.indexes[i].Status == 1) && b.indexes[i].Pos > 0 && b.indexes[i].Pos <= totalFileSize) {
                    OcadObject obj = new OcadObject();
                    reader.BaseStream.Seek(b.indexes[i].Pos, SeekOrigin.Begin);
                    obj.Read(reader, version);
                    CreateObject(obj);
                }
            }
        }

        void CreateObject(OcadObject obj) {
            int symid;
            if (version >= 9) {
                if (obj.Sym == -1) {
                    return;             // imported object
                }
                else if (obj.Sym == -2) {
                    // Graphics object -- from broken apart symbol.
                    CreateGraphicsObject(obj);
                    return;
                }
                else if (obj.Sym == -3) {
                    // Image object -- from imported AI/WMF
                    CreateImageObject(obj);
                    return;
                }

                symid = obj.Sym;
            }
            else {
                symid = (obj.Sym / 10) * 1000 + (obj.Sym % 10);
            }

            if (! symdefids.ContainsKey(symid) || symdefids[symid] == null) {
                // Unknown symbol definition.
                // CONSIDER: produce warning that at unsymbols object is being ignored?
                return;
            }

            SymDef symdef = symdefids[symid];

            Symbol sym = null;

            // Note that we just ignore a symbol if the Otp value is inconsistent with the type of
            // symdef it is associated with. This can happen in OCAD files if a symbol type is deleted 
            // and replaced with a different kind.
            switch (obj.Otp) {
            default: 
#if DEBUG
                throw new OcadFileFormatException("Invalid Otp value {0} in object", obj.Otp);
#else
                break;
#endif
            case 1:
                if (symdef is PointSymDef)
                    sym = CreatePointSymbol(obj, symdef as PointSymDef);
                break; 
            case 2:
                // Line or line text symbols.
                if (symdef is LineSymDef)
                    sym = CreateLineSymbol(obj, symdef as LineSymDef);
                else if (symdef is TextSymDef)
                    sym = CreateLineTextSymbol(obj, symdef as TextSymDef);
                break;
            case 3:
                // Area symbols.
                if (symdef is AreaSymDef)
                    sym = CreateAreaSymbol(obj, symdef as AreaSymDef);
                break;

            case 4:
                // Text symbols
                if (symdef is TextSymDef)
                    sym = CreateTextSymbol(obj, symdef as TextSymDef, false);
                break;

            case 6:
                // Line text symbol.
                if (symdef is TextSymDef)
                    sym = CreateLineTextSymbol(obj, symdef as TextSymDef);
                break;

            case 5: case 7:
                // formatted text or rectangle symbol
                if (symdef is TextSymDef) 
                    sym = CreateTextSymbol(obj, symdef as TextSymDef, true);
                else if (symdef is LineSymDef) {
                    RectangleInfo rectinfo = (RectangleInfo) rectangleInfos[symid];
                    Symbol[] syms = CreateRectangleSymbol(obj, symdef as LineSymDef, rectinfo);
                    if (syms != null) {
                        foreach (Symbol s in syms) {
                            map.AddSymbol(s);
                        }
                    }
                }

                break;
            }

            if (sym != null) {
                map.AddSymbol(sym);
                CheckSymbolRenderable(sym);
            }
        }

        // Create a graphics object -- an object created from an object broken apart with the To Graphics command.
        void CreateGraphicsObject(OcadObject obj)
        {
            GraphicsSymDef def = GetGraphicsSymDef();    // Get the symdef.
            Symbol sym = null;

            if (obj.Otp == 3) {
                sym = CreateAreaGraphicsObject(obj, def);
            }
            else if (obj.Otp == 2) {
                sym = CreateLineGraphicsObject(obj, def);
            }

            if (sym != null) {
                map.AddSymbol(sym);
                CheckSymbolRenderable(sym);
            }
        }

        // Create a iamge object -- an object created  from an image import operations
        void CreateImageObject(OcadObject obj)
        {
            ImageSymDef def = GetImageSymDef();    // Get the symdef.
            Symbol sym = null;

            if (obj.Otp == 3) {
                sym = CreateAreaImageObject(obj, def);
            }
            else if (obj.Otp == 2) {
                sym = CreateLineImageObject(obj, def);
            }

            if (sym != null) {
                map.AddSymbol(sym);
                CheckSymbolRenderable(sym);
            }
        }

        PointSymbol CreatePointSymbol(OcadObject obj, PointSymDef symdef)
        {
            if (symdef == null) {
#if DEBUG
                throw new OcadFileFormatException("Object has unknown or inconsistent symbol type {0}", obj.Sym);
#else
                return null;
#endif
            }

            PointF location = PointFromOcadCoord(obj.coords[0]);

            // Determine if there are any circle gaps.
            float[] gaps = null;
            if (obj.coords.Length > 1) {
                // The additional coordinates give circle gaps. They are expressed in pairs of OCAD angles, where the X is the start and
                // the Y is the end. 
                gaps = new float[(obj.coords.Length - 1) * 2];
                for (int i = 0; i < obj.coords.Length - 1; ++i) {
                    OcadCoord ocadGap = obj.coords[i + 1];
                    gaps[i * 2] = AngleToDegrees(ocadGap.x);
                    gaps[i * 2 + 1] = AngleToDegrees(ocadGap.y);
                }
            }

            return new PointSymbol(symdef, location, AngleToDegrees(obj.Ang), gaps);
        }

        LineSymbol CreateLineSymbol(OcadObject obj, LineSymDef symdef) {
            if (symdef == null) {
#if DEBUG
                throw new OcadFileFormatException("Object has unknown or inconsistent symbol type {0}", obj.Sym);
#else
                return null;
#endif
            }

            if (obj.coords == null || obj.coords.Length < 2)
                return null;

            SymPath path = CreateSymPath(obj.coords);

            return new LineSymbol(symdef, path);
        }

        // Create an line graphics object ---- an line object created from an object broken apart with the To Graphics command.
        GraphicsLineSymbol CreateLineGraphicsObject(OcadObject obj, GraphicsSymDef symdef)
        {
            if (obj.coords == null || obj.coords.Length < 2)
                return null;

            SymPath path = CreateSymPath(obj.coords);

            LineStyle lineStyle = ImportLineStyle(obj.DiamFlags);

            return new GraphicsLineSymbol(symdef, path, GetColor((int) obj.Col), ToWorldDimensions(obj.LineWidth), lineStyle);
        }

        // From an compressed CMYK in a 32-bit uint, get a Color (not a SymColor)
        // These are used in the Col fields of objects for image objects.
        Color ColorFromCompressedCMYK(uint cmyk)
        {
            // Get CMYK as floats from 0 to 1
            float k = (float) ((cmyk & 0xFF000000) >> 24) / 200.0F;
            float y = (float) ((cmyk & 0x00FF0000) >> 16) / 200.0F;
            float m = (float) ((cmyk & 0x0000FF00) >> 8) / 200.0F;
            float c = (float) ((cmyk & 0x000000FF)) / 200.0F;

            // Convert to RGB in floats from 0,1
            float r, g, b;
            SymColor.CMYKtoRGB(c, m, y, k, out r, out g, out b);

            // Convert to ints in 0..255 ranges.
            int red = (int) Math.Round(r * 255);
            int green = (int) Math.Round(g * 255);
            int blue = (int) Math.Round(b * 255);

            return Color.FromArgb(255, (byte)red, (byte)green, (byte)blue);
        }


        // Create an image graphics object ---- an line object created from an image import
        ImageLineSymbol CreateLineImageObject(OcadObject obj, ImageSymDef symdef)
        {
            if (obj.coords == null || obj.coords.Length < 2)
                return null;

            SymPath path = CreateSymPath(obj.coords);

            LineStyle lineStyle = ImportLineStyle(obj.DiamFlags);

            return new ImageLineSymbol(symdef, path, ColorFromCompressedCMYK(obj.Col), ToWorldDimensions(obj.LineWidth), lineStyle);
        }

        LineTextSymbol CreateLineTextSymbol(OcadObject obj, TextSymDef symdef)
        {
            if (symdef == null) {
#if DEBUG
                throw new OcadFileFormatException("Object has unknown or inconsistent symbol type {0}", obj.Sym);
#else
                return null;
#endif
            }

            if (obj.coords == null || obj.coords.Length < 2)
                return null;

            SymPath path = CreateSymPath(obj.coords);
            string text = obj.text;

            CheckFont(symdef.FontName);

            return new LineTextSymbol(symdef, path, text);
        }

        // Rectangle symbols may be translated into multiple symbols (if there is a grid).
        Symbol[] CreateRectangleSymbol(OcadObject obj, LineSymDef symdef, RectangleInfo rectinfo) {
            List<Symbol> symlist = new List<Symbol>();  // list of symbols we're creating.

            if (symdef == null) {
#if DEBUG
                throw new OcadFileFormatException("Object has unknown or inconsistent symbol type {0}", obj.Sym);
#else
                return null;
#endif
            }

            if (obj.coords == null || obj.coords.Length < 2)
                return null;

            // Create the main rectangle symbol.
            // Determine size of the rectangle, and matrix needed to transform points to their correct location.
            PointF[] pts = {PointFromOcadCoord(obj.coords[0]), PointFromOcadCoord(obj.coords[1]), PointFromOcadCoord(obj.coords[2]), PointFromOcadCoord(obj.coords[3])};
            SizeF size = new SizeF(Util.DistanceF(pts[0], pts[1]), Util.DistanceF(pts[0], pts[3]));
            float angle = Util.Angle(pts[0], pts[1]);
            Matrix matrix = new Matrix();
            matrix.Translate(pts[0].X, pts[0].Y);
            matrix.Rotate(angle);

            SymPath path;
            PointKind[] kinds;
            PointF[] pathpts;
            if (rectinfo.cornerRadius == 0) {
                kinds = new PointKind[] {PointKind.Corner, PointKind.Corner, PointKind.Corner, PointKind.Corner, PointKind.Corner};
                pathpts = new PointF[] { new PointF(0,0), new PointF(0, size.Height), new PointF(size.Width, size.Height), 
                                           new PointF(size.Width, 0), new PointF(0,0)};
            }
            else {
                kinds = new PointKind[] {PointKind.Normal, PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
                                            PointKind.Normal, PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
                                            PointKind.Normal, PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
                                            PointKind.Normal, PointKind.Normal, PointKind.BezierControl, PointKind.BezierControl,
                                            PointKind.Normal};
                pathpts = new PointF[] { new PointF(rectinfo.cornerRadius, 0),
                                           new PointF(size.Width - rectinfo.cornerRadius, 0),
                                           new PointF(size.Width - (1-Util.kappa) * rectinfo.cornerRadius, 0),
                                           new PointF(size.Width, (1-Util.kappa) * rectinfo.cornerRadius),
                                           new PointF(size.Width, rectinfo.cornerRadius),
                                           new PointF(size.Width, size.Height - rectinfo.cornerRadius),
                                           new PointF(size.Width, size.Height - (1-Util.kappa) * rectinfo.cornerRadius),
                                           new PointF(size.Width - (1-Util.kappa) * rectinfo.cornerRadius, size.Height),
                                           new PointF(size.Width - rectinfo.cornerRadius, size.Height),
                                           new PointF(rectinfo.cornerRadius, size.Height),
                                           new PointF((1-Util.kappa) * rectinfo.cornerRadius, size.Height),
                                           new PointF(0, size.Height - (1-Util.kappa) * rectinfo.cornerRadius),
                                           new PointF(0, size.Height - rectinfo.cornerRadius),
                                           new PointF(0, rectinfo.cornerRadius),
                                           new PointF(0, (1-Util.kappa) * rectinfo.cornerRadius),
                                           new PointF((1-Util.kappa) * rectinfo.cornerRadius, 0),
                                           new PointF(rectinfo.cornerRadius, 0)};
            }

            pathpts = GraphicsUtil.TransformPoints(pathpts, matrix);
            for (int i = 0; i < pathpts.Length; ++i)
                pathpts[i] = new PointF((float) Math.Round(pathpts[i].X, 2), (float) Math.Round(pathpts[i].Y, 2));   // round to 2 decimals, so round trip to OCAD without change.
            path = new SymPath(pathpts, kinds);
            symlist.Add(new LineSymbol(symdef, path));

            if (rectinfo.grid) {
                if (size.Width > 0 && size.Height > 0) {
                    int cxCells = (int) Math.Round(size.Width / rectinfo.cellWidth);
                    if (cxCells < 1)
                        cxCells = 1;
                    int cyCells = (int) Math.Round(size.Height / rectinfo.cellHeight);
                    if (cyCells < 1)
                        cyCells = 1;

                    float width = size.Width / cxCells;
                    float height = size.Height / cyCells;

                    CreateGridLines(size, matrix, cxCells, cyCells, width, height, rectinfo.gridLines, symlist);
                    CreateGridText(size, matrix, angle, cxCells, cyCells, width, height, rectinfo, symlist);
                }
            }

            return symlist.ToArray();
        }

        // Create the grid lines for a rectangle of the given size with given cellwidth/height and using the
        // line symdef. Transform the points by the given matrix before creating.
        void CreateGridLines(SizeF size, Matrix matrix, int cxCells, int cyCells, float width, float height, LineSymDef symdef, List<Symbol> symlist) {
            PointKind[] kinds = { PointKind.Normal, PointKind.Normal };
            PointF[] pts = new PointF[2];

            for (int x = 1; x < cxCells; ++x) {
                pts[0] = new PointF(x * width, 0);
                pts[1] = new PointF(x * width, size.Height);
                pts = GraphicsUtil.TransformPoints(pts, matrix);
                SymPath path = new SymPath(pts, kinds);
                symlist.Add(new LineSymbol(symdef, path));
            }

            for (int y = 1; y < cyCells; ++y) {
                pts[0] = new PointF(0, y * height);
                pts[1] = new PointF(size.Width, y * height);
                pts = GraphicsUtil.TransformPoints(pts, matrix);
                SymPath path = new SymPath(pts, kinds);
                symlist.Add(new LineSymbol(symdef, path));
            }
        }

        // Create the grid text symbols,
        void CreateGridText(SizeF size, Matrix matrix, float angle, int cxCells, int cyCells, float width, float height, RectangleInfo rectinfo, List<Symbol> symlist) {
            PointF[] pts = new PointF[1];

            for (int y = 0; y < cyCells; ++y) 
                for (int x = 0; x < cxCells; ++x) {
                    int cellNum;
                    string cellText;

                    if (rectinfo.numberFromBottom)
                        cellNum = y * cxCells + x + 1;
                    else
                        cellNum = (cyCells - 1 - y) * cxCells + x + 1;

                    if (cellNum > cxCells * cyCells - rectinfo.unnumberedCells)
                        cellText = rectinfo.unnumberedText;
                    else
                        cellText = cellNum.ToString(); 

                    pts[0] = new PointF((x + 0.07F) * width, (y + 1 - 0.04F) * height);
                    pts[0].Y -= rectinfo.gridText.FontAscent - rectinfo.gridText.FontEmHeight;
                    pts = GraphicsUtil.TransformPoints(pts, matrix);

                    TextSymbol sym = new TextSymbol(rectinfo.gridText, new string[] {cellText}, pts[0], angle, 0);
                    symlist.Add(sym);
                }
        }

        AreaSymbol CreateAreaSymbol(OcadObject obj, AreaSymDef symdef) {
            if (symdef == null) {
#if DEBUG
                throw new OcadFileFormatException("Object has unknown or inconsistent symbol type {0}", obj.Sym);
#else
                return null;
#endif
            }

            SymPathWithHoles path = CreateAreaSymPath(obj.coords);

            return new AreaSymbol(symdef, path, AngleToDegrees(obj.Ang));
        }

        // Create an area graphics object ---- an area object created from an object broken apart with the To Graphics command.
        GraphicsAreaSymbol CreateAreaGraphicsObject(OcadObject obj, GraphicsSymDef symdef)
        {
            SymPathWithHoles path = CreateAreaSymPath(obj.coords);

            return new GraphicsAreaSymbol(symdef, path, GetColor((int) obj.Col));
        }

        // Create an area iamge object ---- an area object created from an image import
        ImageAreaSymbol CreateAreaImageObject(OcadObject obj, ImageSymDef symdef)
        {
            SymPathWithHoles path = CreateAreaSymPath(obj.coords);

            return new ImageAreaSymbol(symdef, path, ColorFromCompressedCMYK(obj.Col));
        }


        TextSymbol CreateTextSymbol(OcadObject obj, TextSymDef symdef, bool formatted) {
            if (symdef == null) {
#if DEBUG
                throw new OcadFileFormatException("Object has unknown or inconsistent symbol type {0}", obj.Sym);
#else
                return null;
#endif
            }

            string text = obj.text;

            PointF location;
            float width, topAdjust;
            float angle = AngleToDegrees(obj.Ang);

            if (formatted) {
                location = PointFromOcadCoord(obj.coords[3]);
                width = Util.DistanceF(location, PointFromOcadCoord(obj.coords[2]));

                // OCAD adds an extra internal leading (incorrectly).
                topAdjust = symdef.GetOcadTopAdjustment(true, version);
            }
            else {
                location = PointFromOcadCoord(obj.coords[0]);
                width = 0;
                topAdjust = 0;

                // OCAD top align uses the W height, while we use the Font ascent. Adjust for the small difference.
                topAdjust = symdef.GetOcadTopAdjustment(false, version);
            }

            location.Y += (float) (topAdjust * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
            location.X += (float) (topAdjust * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));

            string[] lines = Util.SplitLines(text);

            CheckFont(symdef.FontName);

            return new TextSymbol(symdef, lines, location, angle, width);
        }

        PointF[] lineBuilder = new PointF[1]; // used to build lines in CreateSymPath.

        PointF PointFromOcadCoord(OcadCoord coord) {
            return new PointF(ToWorldDimensions(coord.x >> 8), ToWorldDimensions(coord.y >> 8));
        }

        PointKind PointKindFromOcadCoord(OcadCoord coord) {
            if ((coord.x & 3) != 0)
                return PointKind.BezierControl;
            else if ((coord.y & 1) != 0)
                return PointKind.Corner;
            else if ((coord.y & 8) != 0)
                return PointKind.Dash;
            else
                return PointKind.Normal;
        }

        // Is this OCAD coordinate starting a hole?
        static bool IsOcadCoordHoleStart(OcadCoord coord) {
            if ((coord.y & 2) != 0)
                return true;
            else
                return false;
        }

        // Is this OCAD coordinate a bezier control point?
        static bool IsOcadCoordBezierControl(OcadCoord coord)
        {
            if ((coord.x & 3) != 0)
                return true;
            else
                return false;
        }

        // Is this OCAD coordinate a left-double-ine cutout?
        static bool IsOcadCoordLeftCutOut(OcadCoord coord)
        {
            if ((coord.x & 4) != 0)
                return true;
            else
                return false;
        }

        // Is this OCAD coordinate a right-double-ine cutout?
        static bool IsOcadCoordRightCutOut(OcadCoord coord)
        {
            if ((coord.y & 4) != 0)
                return true;
            else
                return false;
        }

        // Is this OCAD coordinate a main cutout?
        static bool IsOcadCoordMainCutOut(OcadCoord coord)
        {
            if ((coord.x & 8) != 0)
                return true;
            else
                return false;
        }

        // Is this COAD coord any cutout?
        static bool IsOcadCoordAnyCutOut(OcadCoord coord)
        {
            if (((coord.x & 0xC) != 0) || ((coord.y & 4) != 0))
                return true;
            else
                return false;
        }



        PointF[] PointsFromOcadCoord(OcadCoord[] coords)
        {
            PointF[] pts = new PointF[coords.Length];
            for (int i = 0; i < coords.Length; ++i) 
                pts[i] = PointFromOcadCoord(coords[i]);
            return pts;
        }

        // Scan the OCAD coordinates and fix the following problems:
        // 1. not exactly two bezier control points in a row
        // 2. start or end with bezier control points
        // 3. holes when none are allowed.
        // If holes are allowed, counts how many there are.
        // Also returns if there are any cutout flags.
        // CONDIER: we should somehow log these errors and show them to the user.
#if TEST
        internal
#endif 
        static OcadCoord[] FixOcadCoords(OcadCoord[] coords, bool allowHoles, out int numHoles, out bool anyCutouts)
        {
            bool foundProblem = false;
            int numBezierControls = 0;      // tracks the current number of bezier controls found.

            numHoles = 0;
            anyCutouts = false;

            if (coords.Length == 0)
                return coords;

            if (IsOcadCoordBezierControl(coords[0]))
                foundProblem = true;

            for (int i = 0; i < coords.Length; ++i) {
                OcadCoord coord = coords[i];

                if (IsOcadCoordAnyCutOut(coord))
                    anyCutouts = true;

                if (i >= 1 && IsOcadCoordHoleStart(coord)) {
                    if (IsOcadCoordBezierControl(coord))
                        foundProblem = true;
                    if (numBezierControls != 0)
                        foundProblem = true;
                    if (!allowHoles)
                        foundProblem = true;
                    else 
                        ++numHoles;
                }

                if (IsOcadCoordBezierControl(coord))
                    ++numBezierControls;
                else {
                    if (numBezierControls != 0 && numBezierControls != 2)
                        foundProblem = true;
                    numBezierControls = 0;
                }
            }

            if (numBezierControls != 0)
                foundProblem = true;       // should not end with bezier controls.

            // If there were no problems, just return the argument and done (fast, common case).
            if (!foundProblem)
                return coords;
            else
                return PatchUpOcadCoords(coords, allowHoles);
        }

        // Scan the OCAD coordinates and fix the following problems:
        // 1. not exactly two bezier control points in a row
        // 2. start or end with bezier control points
        // 3. holes when none are allowed.
        // If holes are allowed, counts how many there are.
        static OcadCoord[] PatchUpOcadCoords(OcadCoord[] coords, bool allowHoles)
        {
            int numBezierControls = 0;                                      // number of consecutive bezier control points found.
            bool atStart = true;                                                 // are we at the start or the start of a hole?
            List<OcadCoord> list = new List<OcadCoord>();    // the list of coordinates we are building up.

            for (int i = 0; i < coords.Length; ++i) {
                OcadCoord coord = coords[i];

                if (IsOcadCoordHoleStart(coord)) {
                    if (numBezierControls != 0) {
                        // remove trailing bezier control points.
                        list.RemoveRange(list.Count - numBezierControls, numBezierControls);
                        numBezierControls = 0;
                    }

                    atStart = true;

                    if (!allowHoles)
                        break;
                }

                if (IsOcadCoordBezierControl(coord)) {
                    // At the start ignore bezier control points.
                    if (!atStart) {
                        ++numBezierControls;
                        list.Add(coord);
                    }
                }
                else {
                    if (numBezierControls != 0 && numBezierControls != 2)
                        list.RemoveRange(list.Count - numBezierControls, numBezierControls);
                    numBezierControls = 0;
                    if (atStart && list.Count != 0)
                        coord.y |= 2;             // we need to start a hole here, since the bezier that started the hole might have been nuked.
                    list.Add(coord);
                    atStart = false;
                }
            }

            // Remove trailing bezier control points.
            if (numBezierControls != 0) 
                list.RemoveRange(list.Count - numBezierControls, numBezierControls);

            return list.ToArray();
        }

        // Convert flags in the OCAD coord to the sympath start/stop flags.
        byte StartStopFlagsFromCoord(OcadCoord coord)
        {
            byte b = 0;
            if (IsOcadCoordLeftCutOut(coord))
                b |= SymPath.DOUBLE_LEFT_STARTSTOPFLAG;
            if (IsOcadCoordRightCutOut(coord))
                b |= SymPath.DOUBLE_RIGHT_STARTSTOPFLAG;
            if (IsOcadCoordMainCutOut(coord))
                b |= SymPath.MAIN_STARTSTOPFLAG;

            return b;
        }
            
        SymPath CreateSymPath(OcadCoord[] coords) {
            int dummy;
            bool anyCutouts;
            coords = FixOcadCoords(coords, false, out dummy, out anyCutouts);

            PointF[] points = new PointF[coords.Length];
            PointKind[] kinds = new PointKind[coords.Length];
            byte[] startStopFlags = null;

            if (anyCutouts)
                startStopFlags = new byte[coords.Length - 1];

            for (int i = 0; i < coords.Length; ++i) {
                points[i] = PointFromOcadCoord(coords[i]);
                kinds[i] = PointKindFromOcadCoord(coords[i]);
                if (anyCutouts && i < coords.Length - 1) 
                    startStopFlags[i] = StartStopFlagsFromCoord(coords[i]);
            }

            return new SymPath(points, kinds, startStopFlags, false);
        }

        // Creates holes, and also closes the path (and holes)
        SymPathWithHoles CreateAreaSymPath(OcadCoord[] coords) {
            int numHoles;
            bool anyCutouts;

            coords = FixOcadCoords(coords, true, out numHoles, out anyCutouts);

            SymPath[] holes;
            if (numHoles > 0)
                holes = new SymPath[numHoles];
            else
                holes = null;

            SymPath path = null;

            // Pass 2: allocate the correct sizes of arrays and fill them in
            int startIndex = 0;
            int holeNumber = -1;
            for (int i = 1; i <= coords.Length; ++i) {
                if (i == coords.Length || IsOcadCoordHoleStart(coords[i])) {
                    // Found the end of the main path or hole. 

                    int size = i - startIndex;
                    int arraySize = size;

                    // Make a closed path by duplicating first coord?
                    bool closed = (size <= 1 || PointFromOcadCoord(coords[i-1]) != PointFromOcadCoord(coords[startIndex]));
                    if (closed) {
                        ++arraySize;
                    }

                    PointF[] points = new PointF[arraySize];
                    PointKind[] kinds = new PointKind[arraySize];
                    byte[] startStopFlags = null;
                    if (anyCutouts)
                        startStopFlags = new byte[arraySize - 1];

                    for (int pointIndex = 0; pointIndex < size; ++pointIndex) {
                        points[pointIndex] = PointFromOcadCoord(coords[pointIndex + startIndex]);
                        kinds[pointIndex] = PointKindFromOcadCoord(coords[pointIndex + startIndex]);
                        if (startStopFlags != null && pointIndex < startStopFlags.Length)
                            startStopFlags[pointIndex] = StartStopFlagsFromCoord(coords[pointIndex + startIndex]);
                    }
                    if (closed) {
                        points[arraySize - 1] = PointFromOcadCoord(coords[startIndex]);
                        kinds[arraySize - 1] = PointKindFromOcadCoord(coords[startIndex]);
                    }

                    SymPath p = new SymPath(points, kinds, startStopFlags, closed);
                    if (holeNumber == -1)
                        path = p;
                    else
                        holes[holeNumber] = p;

                    ++holeNumber;
                    startIndex = i;
                }
            }

            return new SymPathWithHoles(path, holes);
        }

        // Mark a symdef as non-renderable. This won't necessarily cause the user to be notified, only if the symbols that use
        // that symdef actually occur.
        void SymdefIsNotRenderable(SymDef symdef, string reason)
        {
            if (!nonRenderableSymdefs.ContainsKey(symdef))
                nonRenderableSymdefs[symdef] = new List<string>();

            nonRenderableSymdefs[symdef].Add(reason);
        }

        // Check if a symbol uses a visible, non-renderable symdef. If so, add to the list of non-renderable objects.
        void CheckSymbolRenderable(Symbol sym)
        {
            SymDef symdef = sym.Definition;

            // The symdef is indeed non-renderable. Create a message for each reason.
            if (symdef != null && nonRenderableSymdefs.ContainsKey(symdef)) {
                foreach (string s in nonRenderableSymdefs[symdef]) {
                    SymbolNotRenderable(s, symdef);
                }
            }
        }

        // An object of a specific symdef is not renderable for an object-specific reason. Use SymdefIsNotRenderable if all
        // objects of this symdef are not renderable. If no symdef, use null.
        private void SymbolNotRenderable(string reason, SymDef symdef)
        {
            string s;
            if (symdef == null) {
                s = reason + " (";
            }
            else if (map.IsSymdefVisible(symdef)) {
                s = String.Format("{0} ({1}.{2}:{3}, ", reason, symdef.OcadID / 1000, symdef.OcadID % 1000, symdef.Name);
            }
            else {
                return;     // invisible symbol, this is renderable.
            }


            if (nonRenderableObjects.ContainsKey(s))
                nonRenderableObjects[s] += 1;
            else
                nonRenderableObjects[s] = 1;

        }


        // Check a font to see if it exists or not, and if not, remember it. Only done when an symbol (not a symdef) that uses that font is read.
        void CheckFont(string fontName)
        {
            if (!missingFontNames.ContainsKey(fontName) && !map.TextMetricsProvider.TextFaceIsInstalled(fontName))
                missingFontNames.Add(fontName, true);
        }
    }
}
