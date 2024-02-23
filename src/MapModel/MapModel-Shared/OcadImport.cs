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
    using System.Drawing.Drawing2D;
    using PurplePen.Graphics2D;

    public class OcadFileFormatException : ApplicationException
    {
        public OcadFileFormatException(string message, params object[] arguments) :
            base(string.Format(message, arguments))
        {}
    }

    class OcadImport {
        Map map;
        FastBinaryReader reader;
        string filename;        // File name being read.
        int version;       // OCAD version being read
        long totalFileSize;        // Total length of file, for validating offsets.

        // Map the symbol colors to SymColor instances.
        Dictionary<int, SymColor> mapOcadIdToSymColor = new Dictionary<int, SymColor>();

        // Keys = symbol def ids.
        // Values = symdef instances.
        Dictionary<string, SymDef> symdefids = new Dictionary<string, SymDef>();

        // The one and only GraphicsSymDef, if any. Created on demand if needed.
        GraphicsSymDef graphicsSymDef = null;

        // Sort order for image objects (not layout objects, which have their own sort order defined in parameter strings).
        int imageObjectSortOrder = 0;  

        // Keys = object index
        // Values = LayoutObjectInfo that give more information about the layout object.
        Dictionary<int, LayoutObjectInfo> layoutObjectInfos = null;

        // layout bitmaps.
        LayoutBitmapInfo[] layoutBitmaps = null;

        // All the layout font attributes.
        LayoutFontInfo[] layoutFontAttributes = null;

        // Font names that aren't present.
        Dictionary<string, bool> missingFontNames = new Dictionary<string, bool>();

        // Symdefs that can't be rendered correctly --- symdef maps to a list of strings that describe the problem.
        Dictionary<SymDef, List<string>> nonRenderableSymdefs = new Dictionary<SymDef, List<string>>();

        // Symbols that can't be rendered correct -- match an error message (which includes a symdef name) to number of instances.
        Dictionary<string, int> nonRenderableObjects = new Dictionary<string, int>();

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

        public int ReadOcadFile(string filename)
        {
            return ReadOcadFile(File.ReadAllBytes(filename), filename);
        }

        // Read an OCAD file from a stream, and returns the version number (6,7,8).
        public int ReadOcadFile(byte[] bytes, string filename) {
            this.filename = filename;

            // Open the file and read the contents.
            reader = new FastBinaryReader(bytes, Encoding.Default);

            // Get the total size of the stream.
            totalFileSize = reader.Length;

            // Check that its an OCAD file.
            int byte1 = reader.ReadByte();
            int byte2 = reader.ReadByte();
            reader.Seek(-2, SeekOrigin.Current);
            if (!(byte1 == 0xAD && byte2 == 0x0C))
                throw new OcadFileFormatException("File is not an OCAD file");

            using (map.Write()) {
                map.Clear();

                // Read the header and color information, and the setup block.
                this.filename = filename;
                OcadFileHeader header = ReadFileHeader();
                version = header.Version;

                if (version < 6 || (version > 12 && version < 2018) || version > 2018) 
                    throw new OcadFileFormatException("File is in OCAD {0} format. Only OCAD formats 6, 7, 8, 9, 10, 11, 12, and 2018 are supported.", version);

                if (version <= 8) {
                    // Only version 8 and less have the symbol header and setup structures. OCAD 9
                    // puts this information into the string parameters.
                    OcadSymbolHeader symheader = ReadSymbolHeader();
                    OcadSetup setup = ReadSetup(header);

                    // Get the scale of the map.
                    map.MapScale = (float) setup.MapScale;
                    map.PrintScale = (float) setup.PrintScale;

                    // Get the real-world coord info.
                    RealWorldCoords realWorldCoords = new RealWorldCoords();
                    realWorldCoords.RealWorldOffsetX = setup.RealWorldOfsX;
                    realWorldCoords.RealWorldOffsetY = setup.RealWorldOfsY;
                    realWorldCoords.RealWorldAngle = setup.RealWorldAngle;
                    realWorldCoords.RealWorldGridDistance = setup.RealWorldGrid;
                    realWorldCoords.PaperGridDistance = setup.rGridDist;
                    realWorldCoords.RealWorldOn = (setup.RealWorldCoord != 0);
                    map.RealWorldCoords = realWorldCoords;

                    // Get the print area of the map.
                    map.PrintArea = RectangleF.FromLTRB(
                        PointFromOcadCoord(setup.PrLowerLeft).X, PointFromOcadCoord(setup.PrLowerLeft).Y,
                        PointFromOcadCoord(setup.PrUpperRight).X, PointFromOcadCoord(setup.PrUpperRight).Y);

                    OcadFileInfo fileInfo = new OcadFileInfo();
                    fileInfo.Read(reader, header.InfoPos, header.InfoSize);
                    map.FileInformation = fileInfo.Info;

                    // Get the template infomation out of the setup (if OCAD 7 or less).
                    if (version <= 7) {
                        TemplateInfo templateInfo = ReadTemplateInfo(setup);
                        if (templateInfo != null)
                            map.Templates = new TemplateInfo[] { templateInfo };
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
                            RealWorldCoords realWorldCoords = new RealWorldCoords();
                            map.MapScale = GetParamFloat(simpleStringParameters[OcadStringParam.ScalePar - OcadStringParam.FirstSingleParam], 'm', 10000.0F);
                            realWorldCoords.PaperGridDistance = GetParamDouble(simpleStringParameters[OcadStringParam.ScalePar - OcadStringParam.FirstSingleParam], 'g', realWorldCoords.PaperGridDistance);
                            realWorldCoords.RealWorldOffsetX = GetParamDouble(simpleStringParameters[OcadStringParam.ScalePar - OcadStringParam.FirstSingleParam], 'x', realWorldCoords.RealWorldOffsetX);
                            realWorldCoords.RealWorldOffsetY = GetParamDouble(simpleStringParameters[OcadStringParam.ScalePar - OcadStringParam.FirstSingleParam], 'y', realWorldCoords.RealWorldOffsetY);
                            realWorldCoords.RealWorldAngle = GetParamDouble(simpleStringParameters[OcadStringParam.ScalePar - OcadStringParam.FirstSingleParam], 'a', realWorldCoords.RealWorldAngle);
                            realWorldCoords.RealWorldGridDistance = GetParamDouble(simpleStringParameters[OcadStringParam.ScalePar - OcadStringParam.FirstSingleParam], 'd', realWorldCoords.RealWorldGridDistance);
                            realWorldCoords.RealWorldOn = GetParamInt(simpleStringParameters[OcadStringParam.ScalePar - OcadStringParam.FirstSingleParam], 'r', 0) != 0;
                            realWorldCoords.RealWorldLocalOffsetX = GetParamDouble(simpleStringParameters[OcadStringParam.ScalePar - OcadStringParam.FirstSingleParam], 'b', realWorldCoords.RealWorldLocalOffsetX);
                            realWorldCoords.RealWorldLocalOffsetY = GetParamDouble(simpleStringParameters[OcadStringParam.ScalePar - OcadStringParam.FirstSingleParam], 'c', realWorldCoords.RealWorldLocalOffsetY);
                            realWorldCoords.RealWorldGridAndZone = GetParamInt(simpleStringParameters[OcadStringParam.ScalePar - OcadStringParam.FirstSingleParam], 'i', 0);
                            if (realWorldCoords.RealWorldGridAndZone == 64000 || realWorldCoords.RealWorldGridAndZone == 65000) {
                                // Pseudo-mercator has a Grid Scale Factor other than 1. Figure it out.
                                realWorldCoords.GridScaleFactor = ProjectionUtil.CalcGridScaleFactor(realWorldCoords.Proj4String, realWorldCoords.RealWorldOffsetX, realWorldCoords.RealWorldOffsetY);
                            }
                            map.RealWorldCoords = realWorldCoords;
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

                        // Get the view parameters.
                        if (simpleStringParameters[OcadStringParam.ViewPar - OcadStringParam.FirstSingleParam] != null) {
                            map.HideTemplates = GetParamInt(simpleStringParameters[OcadStringParam.ViewPar - OcadStringParam.FirstSingleParam], 'd', 0) != 0;
                            map.HideLayout = GetParamInt(simpleStringParameters[OcadStringParam.ViewPar - OcadStringParam.FirstSingleParam], 'l', 0) != 0;
                            if (version >= 11)
                                map.UseEuclideanMetric = GetParamInt(simpleStringParameters[OcadStringParam.ViewPar - OcadStringParam.FirstSingleParam], 'p', 0) == 0;
                        }

                        // Get the file information.
                        if (listStringParameters[(int) OcadStringParam.FileInfo] != null) {
                            map.FileInformation = ReadFileInformation(listStringParameters[(int) OcadStringParam.FileInfo]);
                        }
                        else {
                            map.FileInformation = "";
                        }
                    }

                    if (listStringParameters[(int) OcadStringParam.Template] != null) {
                        // Only import the first template right now.
                        map.Templates = ReadTemplateInfos(listStringParameters[(int) OcadStringParam.Template]);
                    }

                    if (version >= 11 && listStringParameters[(int)OcadStringParam.LayoutObjects] != null) {
                        this.layoutObjectInfos = ReadLayoutObjectInfos(listStringParameters[(int)OcadStringParam.LayoutObjects], out this.layoutBitmaps);
                    }

                    // Only version 11 uses the layout font attributes. Version 12 uses the object string.
                    if (version == 11 && listStringParameters[(int)OcadStringParam.LayoutFontAttributes] != null) {
                        this.layoutFontAttributes = ReadLayoutFontAttributes(listStringParameters[(int)OcadStringParam.LayoutFontAttributes]);
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

                // Layout layer bitmaps are not in the index blocks.
                CreateLayoutBitmaps(map.GetLayoutSymDef());

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
            if (version == 8) {
                // OCAD 8 stores offsetX and offsetY scaled 100x.
                offsetX = offsetX / 100F;
                offsetY = offsetY / 100F;
            }
            float angle = GetParamFloat(paramString, 'a', 0F);
            float shearAngle = GetParamFloat(paramString, 'b', angle);  // default is same as angle.
            float dpi = 0;
            float scaleX = 1.0F, scaleY = 1.0F;
            float pixelX = GetParamFloat(paramString, 'u', 0F);
            float pixelY = GetParamFloat(paramString, 'v', 0F);
            if (version == 8) {
                // OCAD 8 stores pixelX and pixelY scaled 100x.
                pixelX = pixelX / 100F;
                pixelY = pixelY / 100F;
            }
            if (pixelX != 0 && pixelY != 0) {
                dpi = 25.4F / pixelX;
                scaleX = 1.0F;
                scaleY = pixelY / pixelX;
            }
            bool enabled = (GetParamInt(paramString, 's', 1) != 0);

            return new TemplateInfo(absoluteFileName, new PointF(offsetX, offsetY), dpi, angle, scaleX, scaleY, shearAngle, enabled);
        }

        List<TemplateInfo> ReadTemplateInfos(List<OcadParamString> paramStrings)
        {
            if (paramStrings.Count == 0)
                return null;

            List<TemplateInfo> templateInfos = new List<TemplateInfo>(paramStrings.Count);
            foreach (OcadParamString paramString in paramStrings) {
                templateInfos.Add(ReadTemplateInfo(paramString));
            }

            return templateInfos;
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

                // Non-=positive type means deleted (skip).
                if (param.StType <= 0)
                    continue;

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

        // Get a double parameter out of an OcadParamString. Use \0 to get the first field.
        // Returns defaultValue if passed null or if the parameter doesn't exist.
        double GetParamDouble(OcadParamString paramString, char code, double defaultValue)
        {
            string s = GetParamString(paramString, code);
            if (s == null)
                return defaultValue;

            double f;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
                return f;
            else
                return defaultValue;
        }

        string ReadFileInformation(List<OcadParamString> fileInfoParamStrings)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < fileInfoParamStrings.Count; ++i) {
                OcadParamString ps = fileInfoParamStrings[i];
                builder.Append(ps.firstField);
                builder.Append("\r\n");
            }

            return builder.ToString();
        }

        // Create the SymColor objects in the map, and also cache them in the colors array.
        // Note that we don't preserve the spot color information.
        void CreateColors(OcadSymbolHeader symheader) {
            for (int i = symheader.nColors - 1; i >= 0 ; --i) {
                OcadColorInfo ocadColorInfo = symheader.aColorInfo[i];
                SymColor color;
                color = map.AddColor(ocadColorInfo.ColorName, ocadColorInfo.ColorNum,
                    (float)(ocadColorInfo.Color.cyan)/200.0F,
                    (float)(ocadColorInfo.Color.magenta)/200.0F,
                    (float)(ocadColorInfo.Color.yellow)/200.0F,
                    (float)(ocadColorInfo.Color.black)/200.0F,
                    ocadColorInfo.Overprint != 0);
                mapOcadIdToSymColor[ocadColorInfo.ColorNum] = color;
            }
        }

        // Create the SymColor objects in the map, and also cache them in the colors array.
        // Note that we don't preserve the spot color information, or the transparency.
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
                bool overprint = GetParamInt(paramString, 'o', 0) != 0;

                SymColor color;
                color = map.AddColor(colorName, (short) colorNum, cyan, magenta, yellow, black, overprint);
                mapOcadIdToSymColor[colorNum] = color;
            }
        }

        // Get a color, return null if not there.
        SymColor GetColor(int ocadId)
        {
            if (!mapOcadIdToSymColor.ContainsKey(ocadId))
                return null;
            else
                return mapOcadIdToSymColor[ocadId];
        }

        string SymbolIdFromOcadSymbolNumber(int ocadSymbolNumber)
        {
            Debug.Assert(ocadSymbolNumber >= 0);

            if (version >= 9) {
                // OCAD 9 and above uses id of form xxxx.yyy, where integer is xxxxyyy.
                int integerPart = ocadSymbolNumber / 1000;
                int fracPart = ocadSymbolNumber - integerPart * 1000;
                return string.Format("{0}.{1}", integerPart, fracPart);
            }
            else {
                // OCAD 8 and below uses id of form xxx.y, where integer is xxxy.
                int integerPart = ocadSymbolNumber / 10;
                int fracPart = ocadSymbolNumber - integerPart * 10;
                return string.Format("{0}.{1}", integerPart, fracPart);
            }
        }

        void CreateSymdef(OcadSymbol ocadSym)
        {
            string symid = SymbolIdFromOcadSymbolNumber(ocadSym.Sym);

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

                if ((ocadSym.Status & 2) != 0) {
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
                if (color != null) {
                    switch (elt.stType) {
                        case 1: // line
                            SymPath path = CreateSymPath(elt.stCoords);
                            if (path != null)
                                glyph.AddLine(color, path, ToWorldDimensions(elt.stLineWidth), ImportLineJoin((short)elt.stFlags), ImportLineCap((short)elt.stFlags));
                            break;
                        case 2: // area
                            SymPathWithHoles areaPath = CreateAreaSymPath(elt.stCoords);
                            if (areaPath != null)
                                glyph.AddArea(color, areaPath);
                            break;
                        case 3: // circle
                            float width = ToWorldDimensions(elt.stLineWidth);
                            float diameter = ToWorldDimensions(elt.stDiameter);
                            if (version >= 9 && diameter > width)
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
            }

            glyph.ConstructionComplete();

            return glyph;
        }

        SymDef CreatePointSymdef(string name, string symbolId, OcadPointSymbol ocadSym) {
            bool allowRotation = ((ocadSym.Flags & 1) != 0);
            Glyph glyph = CreateGlyph(ocadSym.symbolElts);
            return new PointSymDef(name, symbolId, glyph, allowRotation);
        }

        LineStyle ImportLineStyle(short lineEnds)
        {
            if ((lineEnds & 1) == 1)
                return LineStyle.Rounded;
            else if ((lineEnds & 4) == 4)
                return LineStyle.Mitered;
            else
                return LineStyle.Beveled;
        }

        LineJoin ImportLineJoin(short lineEnds)
        {
            if ((lineEnds & 1) == 1)
                return LineJoin.Round;
            else if ((lineEnds & 4) == 4)
                return LineJoin.Miter;
            else
                return LineJoin.Bevel;
        }

        LineCap ImportLineCap(short lineEnds)
        {
            if ((lineEnds & 1) == 1)
                return LineCap.Round;
            else if ((lineEnds & 4) == 4)
                return LineCap.Flat;
            else
                return LineCap.Flat;
        }

        SymDef CreateLineSymdef(string name, string symbolId, OcadLineSymbol ocadSym) {
            float width = ToWorldDimensions(ocadSym.LineWidth);

            SymColor color = null;
            if (width > 0) {
                color = GetColor(ocadSym.LineColor);
            }

            LineJoin lineJoin = ImportLineJoin((short) ocadSym.LineEnds);
            LineCap lineCap = ImportLineCap((short)ocadSym.LineEnds);

            LineSymDef symdef = new LineSymDef(name, symbolId, color, width, lineJoin, lineCap);

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
                LineJoin secondLineJoin = ImportLineJoin(ocadSym.FrStyle);
                LineCap secondLineCap = ImportLineCap(ocadSym.FrStyle);

                symdef.SetSecondLine(secondColor, secondWidth, secondLineJoin, secondLineCap);
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
                    doubleLines.doubleDashes.halfEndDashLengthWhenClosed = false;
                    doubleLines.doubleDashes.spacingMethod = LineSymDef.SpacingMethod.OCAD;
                }
                else {
                    doubleLines.doubleFillDashed = doubleLines.doubleLeftDashed = doubleLines.doubleRightDashed = false;
                    doubleLines.doubleDashes = new LineSymDef.DashInfo();
                }

                symdef.SetDoubleLines(doubleLines);
            }

            if (ocadSym.MainGap > 0 || ocadSym.SecGap > 0) {
                LineSymDef.DashInfo dashInfo = new LineSymDef.DashInfo();

                dashInfo.spacingMethod = LineSymDef.SpacingMethod.OCAD;
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

            bool primGlyph, secGlyph, cornerGlyph, startGlyph, endGlyph;

            primGlyph = (ocadSym.PrimDElts != null && ocadSym.PrimDElts.Length > 0);
            secGlyph = (ocadSym.SecDElts != null && ocadSym.SecDElts.Length > 0);
            cornerGlyph = (ocadSym.CornerDElts != null && ocadSym.CornerDElts.Length > 0);
            startGlyph = (ocadSym.StartDElts != null && ocadSym.StartDElts.Length > 0);
            endGlyph = (ocadSym.EndDElts != null && ocadSym.EndDElts.Length > 0);

            if (version >= 11) {
                // OCAD 11 file format has bits to turn glyphs off also. We don't preserve glyphs that
                // are turned off (unlike OCAD itself).
                if ((ocadSym.UseSymbolFlags & OcadLineSymbol.SymbolFlagSec) == 0) secGlyph = false;
                if ((ocadSym.UseSymbolFlags & OcadLineSymbol.SymbolFlagCorner) == 0) cornerGlyph = false;
                if ((ocadSym.UseSymbolFlags & OcadLineSymbol.SymbolFlagStart) == 0) startGlyph = false;
                if ((ocadSym.UseSymbolFlags & OcadLineSymbol.SymbolFlagEnd) == 0) endGlyph = false;
            }

            if (primGlyph)			++numGlyphs;
            if (secGlyph)			++numGlyphs;
            if (cornerGlyph)		++numGlyphs;
            if (startGlyph)		    ++numGlyphs;
            if (endGlyph)			++numGlyphs;

            if (numGlyphs > 0) {
                glyphs = new LineSymDef.GlyphInfo[numGlyphs];

                if (primGlyph) {
                    LineSymDef.GlyphInfo glyphInfo = new LineSymDef.GlyphInfo();

                    glyphInfo.spacingMethod = LineSymDef.SpacingMethod.OCAD;
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

                        if (ocadSym.DecMode == 3)
                            SymdefIsNotRenderable(symdef, "OCAD 12 feature: line decrease toward beginning of line");
                        else if (!ocadSym.DecSymbolDistance)
                            SymdefIsNotRenderable(symdef, "OCAD 12 feature: line decrease where symbol distances do not decrease");
                        else if (ocadSym.DecSymbolWidth)
                            SymdefIsNotRenderable(symdef, "OCAD 12 feature: line decrease where symbol widths decrease");

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

                if (secGlyph) {
                    LineSymDef.GlyphInfo glyphInfo = new LineSymDef.GlyphInfo();
                    glyphInfo.spacingMethod = LineSymDef.SpacingMethod.OCAD;
                    glyphInfo.glyph = CreateGlyph(ocadSym.SecDElts);
                    glyphInfo.number = 1;
                    glyphInfo.location = LineSymDef.GlyphLocation.SpacedOffset;
                    glyphInfo.offset = ToWorldDimensions(ocadSym.MainLength) / 2;
                    glyphInfo.distance = ToWorldDimensions(ocadSym.MainLength + ocadSym.MainGap);
                    glyphInfo.firstDistance = glyphInfo.lastDistance = ToWorldDimensions(ocadSym.EndLength) + (ToWorldDimensions(ocadSym.MainGap) / 2);
                    glyphInfo.minimum = Math.Max(1, ocadSym.MinSym + 1);   // OCAD always does at least one symbol, even if you say zero!

                    glyphs[iGlyph++] = glyphInfo;
                }

                if (cornerGlyph) {
                    LineSymDef.GlyphInfo glyphInfo = new LineSymDef.GlyphInfo();
                    glyphInfo.glyph = CreateGlyph(ocadSym.CornerDElts);
                    glyphInfo.number = 1;
                    glyphInfo.location = LineSymDef.GlyphLocation.Corners;

                    glyphs[iGlyph++] = glyphInfo;
                }

                if (startGlyph) {
                    LineSymDef.GlyphInfo glyphInfo = new LineSymDef.GlyphInfo();
                    glyphInfo.spacingMethod = LineSymDef.SpacingMethod.OCAD;
                    glyphInfo.glyph = CreateGlyph(ocadSym.StartDElts);
                    glyphInfo.number = 1;
                    glyphInfo.location = LineSymDef.GlyphLocation.Start;

                    glyphs[iGlyph++] = glyphInfo;
                }

                if (endGlyph) {
                    LineSymDef.GlyphInfo glyphInfo = new LineSymDef.GlyphInfo();
                    glyphInfo.spacingMethod = LineSymDef.SpacingMethod.OCAD;
                    glyphInfo.spacingMethod = LineSymDef.SpacingMethod.OCAD;
                    glyphInfo.glyph = CreateGlyph(ocadSym.EndDElts);
                    glyphInfo.number = 1;
                    glyphInfo.location = LineSymDef.GlyphLocation.End;

                    glyphs[iGlyph++] = glyphInfo;
                }

                symdef.SetGlyphs(glyphs);
            }

            return symdef;
        }


        SymDef CreateRectangleSymdef(string name, string symbolId, OcadRectSymbol ocadSym) 
        {
            SymColor color = GetColor(ocadSym.LineColor);
            float width = ToWorldDimensions(ocadSym.LineWidth);
            float cornerRadius = ToWorldDimensions(ocadSym.Radius);
            ushort gridFlags = ocadSym.GridFlags;
            float cellWidth = ToWorldDimensions(ocadSym.CellWidth);
            float cellHeight = ToWorldDimensions(ocadSym.CellHeight);
            int unnumberedCells = ocadSym.UnnumCells;
            string unnumberedText = ocadSym.UnnumText;
            float textSize;
            if (version >= 10 && ocadSym.FontSize > 0)
                textSize = Geometry.MmFromPoints(ocadSym.FontSize / 10F);
            else
                textSize = 15F / 72F * 25.4F;  // default point size is 15.

            return new RectangleSymDef(name, symbolId, color, width, cornerRadius, gridFlags, cellWidth, cellHeight, textSize, unnumberedCells, unnumberedText);
        }

        SymDef CreateAreaSymdef(string name, string symbolId, OcadAreaSymbol ocadSym) {
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
                string borderSymId = SymbolIdFromOcadSymbolNumber(ocadSym.BorderSym);
                if (!symdefids.ContainsKey(borderSymId)) {
#if DEBUG
                    throw new OcadFileFormatException("Invalid border sym {0} in symbol {1}", ocadSym.BorderSym, ocadSym.Sym);
#endif
                }
                else {
                    borderSymdef = symdefids[borderSymId] as LineSymDef; 
                }
            }

            symdef = new AreaSymDef(name, symbolId, color, borderSymdef);

            if (ocadSym.HatchMode > 0) {
                SymColor hatchColor = GetColor(ocadSym.HatchColor);
                if (hatchColor != null) {
                    AreaSymDef.HatchInfo hatchInfo;
                    hatchInfo.hatchColor = hatchColor;
                    hatchInfo.hatchWidth = ToWorldDimensions(ocadSym.HatchLineWidth);
                    hatchInfo.hatchSpacing = ToWorldDimensions((version <= 8) ? (ocadSym.HatchLineWidth + ocadSym.HatchDist) : ocadSym.HatchDist);
                    hatchInfo.hatchAngle = AngleToDegrees(ocadSym.HatchAngle1);
                    hatchInfo.hatchOffset = 0;
                    hatchInfo.hatchRotateMode = ((ocadSym.Flags & 1) != 0) ? AreaSymDef.RotateMode.Always : AreaSymDef.RotateMode.ManualOnly;
                    symdef.AddHatching(hatchInfo);
                    if (ocadSym.HatchMode == 2) {
                        // Add second hatching.
                        hatchInfo.hatchAngle = AngleToDegrees(ocadSym.HatchAngle2);
                        symdef.AddHatching(hatchInfo);
                    }
                }
            }

            if (ocadSym.StructMode > 0 && ocadSym.StructWidth > 0 && ocadSym.StructHeight > 0) {
                AreaSymDef.PatternInfo patternInfo;
                patternInfo.offsetRows = (ocadSym.StructMode == 2);
                patternInfo.patternWidth = ToWorldDimensions(ocadSym.StructWidth);
                patternInfo.patternHeight = ToWorldDimensions(ocadSym.StructHeight);
                patternInfo.patternAngle = AngleToDegrees(ocadSym.StructAngle);
                patternInfo.patternGlyph = CreateGlyph(ocadSym.StructElts);
                patternInfo.patternOffsetX = patternInfo.patternOffsetY = 0;
                patternInfo.patternRotateMode =  ((ocadSym.Flags & 1) != 0) ? AreaSymDef.RotateMode.Always : AreaSymDef.RotateMode.ManualOnly;
                if (version >= 12) {
                    patternInfo.patternFillMode = (AreaSymDef.PatternFillMode)ocadSym.StructDraw;
                    patternInfo.irregular = ocadSym.StructIrregularVarX > 0 && ocadSym.StructIrregularVarY > 0;  // OCAD does AND, not OR. Seems like a bug but I will copy....
                    patternInfo.irregularVarX = (float)ocadSym.StructIrregularVarX / 100.0F;
                    patternInfo.irregularVarY = (float)ocadSym.StructIrregularVarY / 100.0F;
                    patternInfo.irregularMinDist = ToWorldDimensions(ocadSym.StructIrregularMinDist);
                }
                else {
                    patternInfo.patternFillMode = AreaSymDef.PatternFillMode.Clip;
                    patternInfo.irregular = false;
                    patternInfo.irregularVarX = patternInfo.irregularVarY = patternInfo.irregularMinDist = 0;
                }

                symdef.AddPattern(patternInfo);
            }

            return symdef;
        }

        SymDef CreateTextSymdef(string name, string symbolId, OcadTextSymbol ocadSym) {
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
            fontSize = Geometry.MmFromPoints(ocadSym.FontSize / 10F);

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
                string pointSymId = SymbolIdFromOcadSymbolNumber(ocadSym.PointSym);

                if (!symdefids.ContainsKey(pointSymId)) {
#if DEBUG
                    throw new OcadFileFormatException("Invalid center point sym {0} in symbol {1}", ocadSym.PointSym, ocadSym.Sym);
#endif
                }
                else {
                    centerPointSymdef = symdefids[pointSymId] as PointSymDef;
                }
            }

            symdef = new TextSymDef(name, symbolId, TextSymDef.PreferredSymbolKind.NormalText, centerPointSymdef);

            TextEffects effects = TextEffects.None;
            if (bold)
                effects |= TextEffects.Bold;
            if (italic)
                effects |= TextEffects.Italic;

            symdef.SetFont(ocadSym.FontName, fontSize, effects, fontColor, fontSize * ocadSym.LineSpace / 100F, paraSpacing, firstIndent, restIndent, tabs, charSpacing, wordSpacing, fontAlign, vertAlign);

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

                if (framing.framingColor != null)
                    symdef.SetFraming(framing);
            }
            else if (ocadFrMode == 1 && version >= 9) {
                // Shadow framing
                TextSymDef.Framing framing = new TextSymDef.Framing();
                framing.framingStyle = TextSymDef.FramingStyle.Shadow;
                framing.framingColor = GetColor(ocadFrColor);
                framing.shadowX = ToWorldDimensions(ocadFrOfX);
                framing.shadowY = ToWorldDimensions(ocadFrOfY);

                if (framing.framingColor != null)
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

                if (framing.framingColor != null)
                    symdef.SetFraming(framing);
            }
            else if (ocadFrMode == 1 && version <= 7) {
                // Framing font. Just use the offset and ignore the font.
                TextSymDef.Framing framing = new TextSymDef.Framing();
                framing.framingStyle = TextSymDef.FramingStyle.Shadow;
                framing.framingColor = GetColor(ocadFrColor);
                framing.shadowX = ToWorldDimensions(ocadFrOfX);
                framing.shadowY = ToWorldDimensions(ocadFrOfY);

                if (framing.framingColor != null)
                    symdef.SetFraming(framing);
            }
        }

        SymDef CreateLineTextSymdef(string name, string symbolId, OcadLineTextSymbol ocadSym)
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
            fontSize = Geometry.MmFromPoints(ocadSym.FontSize / 10F);

            charSpacing = ocadSym.CharSpace / 100F;
            wordSpacing = ocadSym.WordSpace / 100F;

            TextEffects effects = TextEffects.None;
            if (bold)
                effects |= TextEffects.Bold;
            if (italic)
                effects |= TextEffects.Italic;

            symdef = new TextSymDef(name, symbolId, TextSymDef.PreferredSymbolKind.LineText, null);

            symdef.SetFont(ocadSym.FontName, fontSize, effects, fontColor, fontSize, 0F, 0F, 0F, null, charSpacing, wordSpacing, fontAlign, vertAlign);

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
                    reader.Seek(b.filepositions[i], SeekOrigin.Begin);
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
                    reader.Seek(b.indexes[i].Pos, SeekOrigin.Begin);
                    obj.Read(reader, version);
                    CreateObject(b.indexes[i], obj);
                }
            }
        }

        void CreateObject(OcadIndex index, OcadObject obj) {
            string symid;
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
                    CreateImageObject(index, obj, SymLayer.Image);
                    return;
                }
                else if (obj.Sym == -4) {
                    // Layout object 
                    CreateImageObject(index, obj, SymLayer.Layout);
                    return;
                }
            }

            symid = SymbolIdFromOcadSymbolNumber(obj.Sym);


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
                else if (symdef is RectangleSymDef) 
                    sym = CreateRectangleSymbol(obj, symdef as RectangleSymDef);

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
        void CreateImageObject(OcadIndex index, OcadObject obj, SymLayer layer)
        {
            Debug.Assert(layer == SymLayer.Image || layer == SymLayer.Layout);

            ImageSymDef def = (layer == SymLayer.Image) ? map.GetImageSymDef() : map.GetLayoutSymDef();    // Get the symdef.
            Symbol sym = null;

            if (obj.Otp == 3) {
                sym = CreateAreaImageObject(index, obj, def, layer);
            }
            else if (obj.Otp == 2) {
                sym = CreateLineImageObject(index, obj, def, layer);
            }
            else if (obj.Otp == 4) {
                sym = CreateTextImageObject(index, obj, def, layer, false);
            }
            else if (obj.Otp == 5) {
                sym = CreateTextImageObject(index, obj, def, layer, true);
            }

            // UNDONE: handle opacity.
            if (version >= 12 && obj.nObjectString > 0) {
                OcadParamString paramString = new OcadParamString();
                paramString.CreateFromString(1, 0, obj.objectString);
                int opacity = GetParamInt(paramString, 'o', 100);

                if (opacity != 100)
                    SymbolNotRenderable("OCAD 12 feature: opacity < 100% for layout layer", null);
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
            if (path == null)
                return null;

            return new LineSymbol(symdef, path);
        }

        // Create an line graphics object ---- an line object created from an object broken apart with the To Graphics command.
        GraphicsLineSymbol CreateLineGraphicsObject(OcadObject obj, GraphicsSymDef symdef)
        {
            if (obj.coords == null || obj.coords.Length < 2)
                return null;

            SymPath path = CreateSymPath(obj.coords);
            if (path == null)
                return null;

            LineJoin lineJoin = ImportLineJoin(obj.DiamFlags);
            LineCap lineCap = ImportLineCap(obj.DiamFlags);

            return new GraphicsLineSymbol(symdef, path, GetColor((int) obj.Col), ToWorldDimensions(obj.LineWidth), lineJoin, lineCap);
        }

        // From an compressed CMYK in a 32-bit uint, get a CmykColor (not a SymColor)
        // These are used in the Col fields of objects for image objects.
        CmykColor ColorFromCompressedCMYK(uint cmyk)
        {
            // Get CMYK as floats from 0 to 1
            float k = (float) ((cmyk & 0xFF000000) >> 24) / 255.0F;
            float y = (float) ((cmyk & 0x00FF0000) >> 16) / 255.0F;
            float m = (float) ((cmyk & 0x0000FF00) >> 8) / 255.0F;
            float c = (float) ((cmyk & 0x000000FF)) / 255.0F;

            return CmykColor.FromCmyk(c, m, y, k);
        }


        // Create an image graphics object ---- an line object created from an image import
        ImageLineSymbol CreateLineImageObject(OcadIndex index, OcadObject obj, ImageSymDef symdef, SymLayer layer)
        {
            bool isVisible;
            int sortOrder;
            GetImageObjectInfo(index.ObjectIndex, layer, out isVisible, out sortOrder);

            if (obj.coords == null || obj.coords.Length < 2)
                return null;

            SymPath path = CreateSymPath(obj.coords);
            if (path == null)
                return null;

            LineJoin lineJoin = ImportLineJoin(obj.DiamFlags);
            LineCap lineCap = ImportLineCap(obj.DiamFlags);

            return new ImageLineSymbol(symdef, path, ColorFromCompressedCMYK(obj.Col), ToWorldDimensions(obj.LineWidth), lineJoin, lineCap, isVisible, sortOrder);
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
            if (path == null)
                return null;

            string text = obj.text ?? "";

            CheckFont(symdef.FontName);

            return new LineTextSymbol(symdef, path, text, TextSymDefHorizAlignment.Default, TextSymDefVertAlignment.Default);
        }


        RectangleSymbol CreateRectangleSymbol(OcadObject obj, RectangleSymDef symdef)
        {
            if (obj.coords == null || obj.coords.Length < 4)
                return null;

            // Create the main rectangle symbol.
            // Determine size of the rectangle, and matrix needed to transform points to their correct location.
            PointF[] pts = { PointFromOcadCoord(obj.coords[0]), PointFromOcadCoord(obj.coords[1]), PointFromOcadCoord(obj.coords[2]), PointFromOcadCoord(obj.coords[3]) };
            SizeF size = new SizeF(Geometry.DistanceF(pts[0], pts[1]), Geometry.DistanceF(pts[0], pts[3]));

            if (size.Width == 0 && size.Height == 0)
                return null;            // OCAD ignores zero-size rectangles entirely.

            float angle = Geometry.Angle(pts[0], pts[1]);

            return new RectangleSymbol(symdef, pts[0], size, angle);
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
            if (path == null)
                return null;

            return new AreaSymbol(symdef, path, AngleToDegrees(obj.Ang), new PointF());
        }

        // Create an area graphics object ---- an area object created from an object broken apart with the To Graphics command.
        GraphicsAreaSymbol CreateAreaGraphicsObject(OcadObject obj, GraphicsSymDef symdef)
        {
            SymPathWithHoles path = CreateAreaSymPath(obj.coords);
            if (path == null)
                return null;

            return new GraphicsAreaSymbol(symdef, path, GetColor((int) obj.Col));
        }

        // Create an area iamge object ---- an area object created from an image import
        ImageAreaSymbol CreateAreaImageObject(OcadIndex index, OcadObject obj, ImageSymDef symdef, SymLayer layer)
        {
            bool isVisible;
            int sortOrder;
            GetImageObjectInfo(index.ObjectIndex, layer, out isVisible, out sortOrder);

            SymPathWithHoles path = CreateAreaSymPath(obj.coords);
            if (path == null)
                return null;
            return new ImageAreaSymbol(symdef, path, ColorFromCompressedCMYK(obj.Col), isVisible, sortOrder);
        }

        private void GetImageObjectInfo(int objIndex, SymLayer layer, out bool isVisible, out int sortOrder)
        {
            isVisible = true;
            sortOrder = this.imageObjectSortOrder++;

            if (layer == SymLayer.Layout) {
                LayoutObjectInfo objectInfo;
                if (layoutObjectInfos != null && layoutObjectInfos.TryGetValue(objIndex, out objectInfo)) {
                    isVisible = objectInfo.IsVisible;
                    sortOrder = objectInfo.SortOrder;
                }
                else {
                    isVisible = false;
                    sortOrder = -1;
                }
            }
        }

        private static string[] ImportTextSymbolLines(string text)
        {
            // OCAD has a weird rule where it ignores a single blank line at the 
            // beginning of text.
            string[] lines = Util.SplitLines(text);

            if (lines.Length > 0 && lines[0].Length == 0) {
                lines = Util.ArraySlice(lines, 1, lines.Length - 1);
            }

            return lines;
        }

        TextSymbol CreateTextSymbol(OcadObject obj, TextSymDef symdef, bool formatted) {
            if (symdef == null) {
#if DEBUG
                throw new OcadFileFormatException("Object has unknown or inconsistent symbol type {0}", obj.Sym);
#else
                return null;
#endif
            }

            string text = obj.text ?? "";

            PointF location;
            float width, topAdjust, leftAdjust;
            float angle = AngleToDegrees(obj.Ang);

            if (formatted) {
                location = PointFromOcadCoord(obj.coords[3]);
                width = Geometry.DistanceF(location, PointFromOcadCoord(obj.coords[2]));

                // OCAD adds an extra internal leading (incorrectly).
                topAdjust = symdef.GetOcadTopAdjustment(true, version);
                leftAdjust = symdef.GetOcadFormattedHorizAdjustment(width);
            }
            else {
                location = PointFromOcadCoord(obj.coords[0]);
                width = 0;
                topAdjust = 0;

                // OCAD top align uses the W height, while we use the Font ascent. Adjust for the small difference.
                topAdjust = symdef.GetOcadTopAdjustment(false, version);
                leftAdjust = 0;
            }

            location.Y += (float) (topAdjust * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
            location.X += (float) (topAdjust * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));
            location.Y += (float) (leftAdjust * Math.Sin((angle) / 360.0 * 2 * Math.PI));
            location.X += (float) (leftAdjust * Math.Cos((angle) / 360.0 * 2 * Math.PI));

            string[] lines = ImportTextSymbolLines(text);

            CheckFont(symdef.FontName);

            return new TextSymbol(symdef, lines, location, angle, width, TextSymDefHorizAlignment.Default, TextSymDefVertAlignment.Default);
        }

        // Create an text image  object ---- an text object created on the layout later
        ImageTextSymbol CreateTextImageObject(OcadIndex index, OcadObject obj, ImageSymDef symdef, SymLayer layer, bool formatted)
        {
            bool isVisible;
            int sortOrder;
            GetImageObjectInfo(index.ObjectIndex, layer, out isVisible, out sortOrder);

            string fontName;
            float fontSize;
            if (version >= 12 && obj.objectString != null && obj.ObjectStringType == 3) {
                OcadParamString paramString = new OcadParamString();
                paramString.CreateFromString(1, 0, obj.objectString);
                fontName = paramString.firstField;
                fontSize = GetParamFloat(paramString, 's', 27.0F);
                int bold = GetParamInt(paramString, 'b', 0);
                int italic = GetParamInt(paramString, 'i', 0);
                int alignment = GetParamInt(paramString, 'a', 0);
                
                if (bold != 0)
                    SymbolNotRenderable("OCAD 12 feature: text in layout layer with bold font", null);
                if (italic != 0)
                    SymbolNotRenderable("OCAD 12 feature: text in layout layer with italic font", null);
                if (alignment != 0)
                    SymbolNotRenderable("OCAD 12 feature: text in layout layer with alignment other than left", null);

                // UNDONE: bold, italic, alignment.
            }
            else if (layoutFontAttributes != null && index.LayoutFont < layoutFontAttributes.Length) {
                fontName = layoutFontAttributes[index.LayoutFont].FontName;
                fontSize = layoutFontAttributes[index.LayoutFont].FontSize;
            }
            else {
                fontName = "Arial";
                fontSize = 27.0F;
            }
            // fontSize is in points. Convert to mm.
            fontSize = Geometry.MmFromPoints(fontSize);

            string text = obj.text ?? "";

            PointF location;
            float topAdjust;
            float angle = AngleToDegrees(obj.Ang);
            float width;

            // OCAD top align uses the W height, while we use the Font ascent. Adjust for the small difference.
            topAdjust = symdef.GetOcadTopAdjustment(fontName, fontSize, formatted);

            if (formatted) {
                location = PointFromOcadCoord(obj.coords[3]);
                width = Geometry.DistanceF(location, PointFromOcadCoord(obj.coords[2]));

                // OCAD adds an extra internal leading (incorrectly).
                topAdjust = symdef.GetOcadTopAdjustment(fontName, fontSize, true);
            }
            else {
                location = PointFromOcadCoord(obj.coords[0]);
                width = 0;
                topAdjust = 0;

                // OCAD top align uses the W height, while we use the Font ascent. Adjust for the small difference.
                topAdjust = symdef.GetOcadTopAdjustment(fontName, fontSize, false);
            }


            location.Y += (float)(topAdjust * Math.Sin((angle + 90.0) / 360.0 * 2 * Math.PI));
            location.X += (float)(topAdjust * Math.Cos((angle + 90.0) / 360.0 * 2 * Math.PI));

            string[] lines = ImportTextSymbolLines(text);

            CheckFont(fontName);

            return new ImageTextSymbol(symdef, lines, ColorFromCompressedCMYK(obj.Col), fontName, fontSize, location, angle, width, isVisible, sortOrder);
        }

        ImageBitmapSymbol CreateImageBitmapObject(ImageSymDef symdef, string path, PointF location, float mmPerPixX, float mmPerPixY, byte[] embeddedData, bool isVisible, int sortOrder )
        {
            return new ImageBitmapSymbol(symdef, path, location, mmPerPixX, mmPerPixY, embeddedData, isVisible, sortOrder, map.FileLoader);
        }

        void CreateLayoutBitmaps(ImageSymDef symdef)
        {
            if (this.layoutBitmaps != null) {
                foreach (LayoutBitmapInfo bitmapInfo in this.layoutBitmaps) {
                    var sym = CreateImageBitmapObject(symdef, bitmapInfo.Path, bitmapInfo.Position, bitmapInfo.MmPerPixX, bitmapInfo.MmPerPixY, bitmapInfo.embeddedData, bitmapInfo.IsVisible, bitmapInfo.SortOrder);
                    if (sym != null) {
                        map.AddSymbol(sym);
                    }
                }
            }
        }

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
        // CONSIDER: we should somehow log these errors and show them to the user.
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
            
        // Creates a path from the given coordinates. May return null if the coordinate
        // array, after fixing, is too short.
        SymPath CreateSymPath(OcadCoord[] coords) {
            int dummy;
            bool anyCutouts;
            coords = FixOcadCoords(coords, false, out dummy, out anyCutouts);

            if (coords.Length < 2)
                return null;

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

            if (coords.Length < 2)
                return null;

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

                    SymPath p;
                    
                    if (points.Length < 2)
                        p =  null;
                    else
                        p  = new SymPath(points, kinds, startStopFlags, closed);

                    if (holeNumber == -1)
                        path = p;
                    else
                        holes[holeNumber] = p;

                    if (! (holeNumber >= 0 && p == null))
                        ++holeNumber;
                    startIndex = i;
                }
            }

            if (path == null)
                return null;

            if (holes != null && holeNumber < holes.Length)
            {
                holes = Util.ChangeArrayLength(holes, holeNumber);
                if (holes.Length == 0)
                    holes = null;
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
                s = String.Format("{0} ({1}:{2}, ", reason, symdef.SymbolId, symdef.Name);
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

        // Information about a layout object.
        struct LayoutObjectInfo
        {
            public readonly bool IsVisible;
            public readonly int SortOrder;

            public LayoutObjectInfo(bool isVisible, int sortOrder)
            {
                this.IsVisible = isVisible;
                this.SortOrder = sortOrder;
            }
        }

        // Information about fonts for layout objects.
        struct LayoutFontInfo
        {
            public readonly string FontName;
            public readonly float FontSize;

            public LayoutFontInfo(string fontName, float fontSize) 
            {
                this.FontName = fontName;
                this.FontSize = fontSize;
            }
        }

        struct LayoutBitmapInfo
        {
            public readonly bool IsVisible;
            public readonly int SortOrder;
            public readonly string Path;
            public readonly PointF Position;
            public readonly float MmPerPixX;
            public readonly float MmPerPixY;
            public readonly byte[] embeddedData;
            
            public LayoutBitmapInfo(string path, PointF position, float mmPerPixX, float mmPerPixY, byte[] embeddedData, bool isVisible, int sortOrder)
            {
                this.Path = path;
                this.Position = position;
                this.MmPerPixX = mmPerPixX;
                this.MmPerPixY = mmPerPixY;
                this.embeddedData = embeddedData;
                this.IsVisible = isVisible;
                this.SortOrder = sortOrder;
            }
        }


        Dictionary<int, LayoutObjectInfo> ReadLayoutObjectInfos(List<OcadParamString> parameterStrings, out LayoutBitmapInfo[] bitmapInfos)
        {
            Dictionary<int, LayoutObjectInfo> dict = new Dictionary<int, LayoutObjectInfo>();
            List<LayoutBitmapInfo> bitmaps = new List<LayoutBitmapInfo>();
            int sortOrder = 0;

            foreach (OcadParamString paramStr in parameterStrings) {
                sortOrder -= 1;  // Since the ordering in the param strings is highest to lowest, the sort order must go down.

                bool vis = GetParamInt(paramStr, 's', 1) != 0;
                int objIndex = GetParamInt(paramStr, 'n', paramStr.ObjIndex);
                bool bitmap = GetParamInt(paramStr, 'r', 0) != 0;

                if (bitmap) {
                    float x = (float)Math.Round(GetParamFloat(paramStr, 'x', 0), 2);
                    float y = (float)Math.Round(GetParamFloat(paramStr, 'y', 0), 2);
                    float pixelSizeX = GetParamFloat(paramStr, 'u', 0.1F);
                    float pixelSizeY = GetParamFloat(paramStr, 'v', 0.1F);

                    // Embedded bitmaps are stored as base64 strings with code 'F'.
                    string embeddedDataBase64 = GetParamString(paramStr, 'F');
                    byte[] embeddedData = null;
                    if (embeddedDataBase64 != null) {
                        embeddedData = Convert.FromBase64String(embeddedDataBase64);
                    }

                    bitmaps.Add(new LayoutBitmapInfo(paramStr.firstField, new PointF(x, y), pixelSizeX, pixelSizeY, embeddedData, vis, sortOrder));
                }
                else { 
                    dict[objIndex] = new LayoutObjectInfo(vis, sortOrder);
                }
            }

            bitmapInfos = bitmaps.ToArray();
            return dict;
        }

        LayoutFontInfo[] ReadLayoutFontAttributes(List<OcadParamString> parametersStrings)
        {
            List<LayoutFontInfo> list = new List<LayoutFontInfo>();
            foreach (OcadParamString paramStr in parametersStrings) {
                string fontName = paramStr.firstField;
                float fontSize = GetParamFloat(paramStr, 's', 10.0F);
                list.Add(new LayoutFontInfo(fontName, fontSize));
            }

            return list.ToArray();
        }

    }

    // An alternative to BinaryReader that is much faster, and reads the entire file at once.
    class FastBinaryReader
    {
        byte[] bytes;
        Encoding encoding;
        int pos;

        public FastBinaryReader(string fileName, Encoding encoding)
            : this(File.ReadAllBytes(fileName), encoding)
        {
        }

        public FastBinaryReader(byte[] bytes, Encoding encoding)
        {
            this.encoding = encoding;
            this.bytes = bytes;
            pos = 0;
        }

        public int Length
        {
            get { return bytes.Length; }
        }

        public int Position
        {
            get { return pos; }
        }

        public int Remaining
        {
            get { return bytes.Length - pos; }
        }

        public int Seek(int delta, SeekOrigin origin)
        {
            int newPos = pos ;

            switch (origin) {
                case SeekOrigin.Begin:
                    newPos = delta;
                    break;

                case SeekOrigin.Current:
                    newPos = pos + delta;
                    break;

                case SeekOrigin.End:
                    newPos = bytes.Length + delta;
                    break;
            }

            if (newPos < 0 || newPos > bytes.Length)
                throw new ArgumentOutOfRangeException();

            pos = newPos;

            return pos;
        }

        public byte ReadByte()
        {
            byte ret = bytes[pos++];
            return ret;
        }

        public bool ReadBoolean()
        {
            bool ret = (bytes[pos++] != 0);
            return ret;
        }

        public short ReadInt16()
        {
            short ret = unchecked((short) (bytes[pos] + (bytes[pos + 1] << 8)));
            pos += 2;
            return ret;
        }

        public ushort ReadUInt16()
        {
            ushort ret = unchecked((ushort) (bytes[pos] + (bytes[pos + 1] << 8)));
            pos += 2;
            return ret;
        }

        public int ReadInt32()
        {
            int ret = unchecked((int) (bytes[pos] + (bytes[pos + 1] << 8) + (bytes[pos + 2] << 16) + (bytes[pos + 3] << 24)));
            pos += 4;

            return ret;
        }

        public uint ReadUInt32()
        {
            uint ret = unchecked((uint) (bytes[pos] + (bytes[pos + 1] << 8) + (bytes[pos + 2] << 16) + (bytes[pos + 3] << 24)));
            pos += 4;

            return ret;
        }

        public double ReadDouble()
        {
            double ret = BitConverter.ToDouble(bytes, pos);
            pos += 8;
            return ret;
        }

        public long ReadInt64()
        {
            long ret = BitConverter.ToInt64(bytes, pos);
            pos += 8;
            return ret;
        }

        // I think the BinaryReader method takes numberOfChars instead. Doesn't really 
        // matter for our purpose since we use a single-byte encoding.
        public char[] ReadChars(int numberOfBytes)
        {
            char[] result = encoding.GetChars(bytes, pos, numberOfBytes);
            pos += numberOfBytes;

            return result;
        }
    }
}
