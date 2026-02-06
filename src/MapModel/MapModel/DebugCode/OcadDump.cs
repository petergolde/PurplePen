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


namespace PurplePen.MapModel.DebugCode
{
    public class OcadDump
    {
        FastBinaryReader reader;
        TextWriter writer;
        int version;    // version of the OCAD file
        int level = 0;  // 0 = exclude things that prevent good diffs., 1 = normal, 5 = include file positions/sizes
        string testDirectory;         // test directory -- to prevent absolute paths from causing problems.

        enum IconFormat { Ocad6, Ocad8Compressed, Ocad9 };

        public OcadDump()
        {
        }

        public OcadDump(int level)
        {
            this.level = level;
        }

        // Set the test directory to prevent absolute path probelsm.
        public OcadDump(string testDirectory)
        {
            this.testDirectory = testDirectory;
        }

        // Replace instances of the test directory with a place holder.
        string RemoveTestDirectory(string s)
        {
            if (testDirectory == null)
                return s;
            else 
                return s.Replace(testDirectory, "[[TESTDIR]]");
        }

        void DumpFileHeader(OcadFileHeader fh)
        {
            writer.WriteLine("OCAD File Header");
            writer.WriteLine("  OCADMark=0x{0:X}  SectionMark=0x{1:X}", fh.OCADMark, fh.SectionMark);
            writer.WriteLine("  Version={0}", fh.Version);
            if (level >= 1)
                writer.WriteLine("   SubVersion={0}", fh.Subversion);
            if (level >= 5) {
                writer.WriteLine("  FirstsymBlk=0x{0:X} FirstIdxBlk=0x{1:X}", fh.FirstSymBlk, fh.FirstIdxBlk);
                writer.WriteLine("  SetupPos=0x{0:X} SetupSize={1}", fh.SetupPos, fh.SetupSize);
                writer.WriteLine("  InfoPos=0x{0:X} InfoSize={1}", fh.InfoPos, fh.InfoSize);
                writer.WriteLine("  FirstStIndexBlk=0x{0:X}", fh.FirstStIndexBlk);
            }
            writer.WriteLine("  Reserved2=0x{0:X} Reserved3=0x{1:X} Reserved4=0x{2:X}", fh.Reserved2, fh.Reserved3, fh.Reserved4);

            writer.WriteLine();
        }

        void DumpInfo(OcadFileInfo fi)
        {
            writer.WriteLine("Ocad File Info:");
            writer.WriteLine("  {0}", fi.Info);
            writer.WriteLine();
        }

        void DumpSymbolHeader(OcadSymbolHeader sh)
        {
            writer.WriteLine("Symbol header: ");
            writer.WriteLine("  nColors={0}, nColorSep={1}", sh.nColors, sh.nColorSep);
            writer.WriteLine("  CyanFreq={0}; CyanAng={1}", sh.CyanFreq, sh.CyanAng);
            writer.WriteLine("  MagentaFreq={0}; MagentaAng={1}", sh.MagentaFreq, sh.MagentaAng);
            writer.WriteLine("  YellowFreq={0}; YellowAng={1}", sh.YellowFreq, sh.YellowAng);
            writer.WriteLine("  BlackFreq={0}; BlackAng={1}", sh.BlackFreq, sh.BlackAng);
            writer.WriteLine("  Reserved1=0x{0:X} Reserved2=0x{1:X}", sh.Res1, sh.Res2);
            writer.WriteLine();

            for (int i = 0; i < sh.nColors; ++i) {
                DumpOcadColorInfo(sh.aColorInfo[i]);
            }
            writer.WriteLine();

            for (int i = 0; i < sh.nColorSep; ++i) {
                DumpOcadColorSep(sh.aColorSep[i]);
            }
            writer.WriteLine();
        }

        void DumpOcadColorInfo(OcadColorInfo ci)
        {
            writer.Write("Color #{0}: ", ci.ColorNum);
            writer.Write("  name:  {0}", ci.ColorName);
            writer.Write("  overprint: {0}", ci.Overprint);
            writer.WriteLine("  value: {0}", ci.Color);
            if (level >= 1) {
                for (int i = 0; i < ci.SepPercentage.Length; ++i) {
                    if (ci.SepPercentage[i] != 255)
                        writer.WriteLine("    Color sep {0} - value {1}", i, ci.SepPercentage[i]);
                }
            }
        }

        void DumpOcadColorSep(OcadColorSep cs)
        {
            writer.Write("Color separation name: {0}", cs.SepName);
            writer.Write("  Color={0}", cs.Color);
            writer.WriteLine("  RasterFreq={0}, RasterAngle={1}", cs.RasterFreq, cs.RasterAngle);
        }

        void DumpBitmap(byte[] array, int start, int nBytes)
        {
            for (int i = start; i < start + nBytes; ++i) {
                int by = array[i];
                for (int j = 0; j < 8; ++j) {
                    int bit = (by & 1);
                    by >>= 1;
                    writer.Write("{0}", bit);
                }
            }
            writer.WriteLine();
        }

        static string FormatBinary(byte b)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder(); 

            for (int i = 0; i < 8; ++i) {
                if ((b & 1) == 0)
                    builder.Append('0');
                else
                    builder.Append('1');
                b = (byte)(b >> 1);
            }
            return builder.ToString();
        }

        void DumpIcon(byte[] array, IconFormat iconFormat)
        {
            if (iconFormat == IconFormat.Ocad8Compressed) {
                writer.WriteLine("Compressed: ");

                for (int i = 0; i < array.Length; ++i) {
                    writer.Write("{0,2:X} ", array[i]);
                    if (i % 16 == 15)
                        writer.WriteLine();
                }

                writer.WriteLine();

                byte[] comp = new byte[array.Length - 16];
                Array.Copy(array, 16, comp, 0, comp.Length);
                byte[] uncompressed = new byte[22 * 22];
                LZWCompression lzw = new LZWCompression();
                lzw.Expand(comp, uncompressed);

                writer.WriteLine("Uncompressed:");
                for (int i = 0; i < uncompressed.Length; ++i) {
                    writer.Write("{0,2:X} ", uncompressed[i]);
                    if (i % 22 == 21)
                        writer.WriteLine();
                }

                writer.WriteLine();
            }
            else if (iconFormat == IconFormat.Ocad6) {
                int i = 0;
                for (int row = 0; row < 22; ++row) {
                    writer.Write("    ");
                    for (int col = 0; col < 22; col += 2) {
                        writer.Write("{0:X} {1:X} ", ((array[i] & 0xF0) >> 4), (array[i] & 0xF));
                        ++i;
                    }
                    i += 1;
                    writer.WriteLine();
                }
            }
            else if (iconFormat == IconFormat.Ocad9) {
                int i = 0;
                for (int row = 0; row < 22; ++row) {
                    writer.Write("    ");
                    for (int col = 0; col < 22; ++col) {
                        writer.Write("{0,2:X} ", array[i]);
                        ++i;
                    }
                    writer.WriteLine();
                }
            }
            else {
                throw new ArgumentException("iconFormat");
            }
        }

        void DumpSetup(OcadSetup setup)
        {
            writer.WriteLine("Setup structure:");
            writer.WriteLine("  Offset={0}, rGridDist={1}", setup.Offset, setup.rGridDist);
            writer.WriteLine("  WorkMode={0}, LineMode={1}, EditMode={2}", setup.WorkMode, setup.LineMode, setup.EditMode);
            writer.WriteLine("  ActSym={0}, MapScale={1}", setup.ActSym, setup.MapScale);
            writer.WriteLine("  RealWorldOfsX={0}, RealWorldOfsY={1}, RealWorldAngle={2}, RealWorldGrid={3}", setup.RealWorldOfsX, setup.RealWorldOfsY, setup.RealWorldAngle, setup.RealWorldGrid);
            writer.WriteLine("  GpsAngle={0:F4}, nGpsAdjust={1}", setup.GpsAngle, setup.nGpsAdjust);
            for (int i = 0; i < setup.nGpsAdjust; ++i)
                DumpGpsAdjust(setup.aGpsAdjust[i]);
            writer.WriteLine("  DraftScaleX={0}, DraftScaleY={1}", setup.DraftScaleX, setup.DraftScaleY);
            writer.WriteLine("  TempOffset={0}, TemplateFileName={1}", setup.TempOffset, RemoveTestDirectory(setup.TemplateFileName));
            writer.WriteLine("  TemplateEnabled={0}, TempResol={1}, rTempAng={2}", setup.TemplateEnabled, setup.TempResol, setup.rTempAng);
            writer.WriteLine("  Res1={0}, Res2={1}", setup.Res1, setup.Res2);
            writer.WriteLine("  PrLowerLeft={0}, PrUpperRight={1}", setup.PrLowerLeft, setup.PrUpperRight);
            writer.WriteLine("  PrGrid={0}, PrGridColor={1}, PrOverlapX={2}, PrOverlapY={3}", setup.PrGrid, setup.PrGridColor, setup.PrOverlapX, setup.PrOverlapY);
            writer.WriteLine("  PrintScale={0}, PrIntensity={1}, prLineWidth={2}, PrReserved={3}", setup.PrintScale, setup.PrIntensity, setup.PrLineWidth, setup.PrReserved);
            writer.WriteLine("  PrStdFonts={0}, PrReserved2={1}, PrReserved3={2}", setup.PrStdFonts, setup.PrReserved2, setup.PrReserved3);
            writer.WriteLine("  PartialLowerLeft={0}, PartialLowerRight={1}", setup.PartialLowerLeft, setup.PartialUpperRight);
            writer.WriteLine("  Zoom={0}, nZoomHist={1}", setup.Zoom, setup.nZoomHist);
            for (int i = 0; i < setup.nZoomHist; ++i)
                DumpZoomRec(setup.ZoomHist[i]);
            writer.WriteLine("  RealWorldCoord={0}", setup.RealWorldCoord);
            if (level >= 1)
                writer.WriteLine("  FileName={0}", setup.FileName);
            writer.WriteLine("  HatchAreas={0}, DimTemp={1}, HideTemp={2}, TempMode={3}, TempColor={4}", setup.HatchAreas, setup.DimTemp, setup.HideTemp, setup.TempMode, setup.TempColor);
            writer.WriteLine();
        }

        void DumpGpsAdjust(OcadGpsAdjust gps)
        {
            writer.WriteLine("    Name={0}, lpMap={1}", gps.Name, gps.lpMap);
            writer.WriteLine("    Lat={0}, Long={1}", gps.Lat, gps.Long);
        }

        void DumpZoomRec(OcadZoomRec zoom)
        {
            writer.WriteLine("    Zoom={0}, Offset={1}", zoom.Zoom, zoom.Offset);
        }

        void DumpParamStringIndex(OcadStIndex index)
        {
            writer.WriteLine("String of type {0}", index.StType);
            if (level >= 5)
                writer.WriteLine("  Pos={0}, Len={1}", index.Pos, index.Len);
            writer.WriteLine("  ObjIndex={0}", index.ObjIndex);

            OcadParamString param = new OcadParamString();
            param.Read(reader, index);
            writer.WriteLine("  firstField={0}", RemoveTestDirectory(param.firstField));
            for (int i = 0; i < param.codes.Length; ++i) {
                writer.WriteLine("  code={0}, value={1}", param.codes[i], param.values[i]);
            }
            writer.WriteLine();
        }

        void DumpOcadSymbol(OcadSymbol sym)
        {
            if (version <= 8)
                writer.WriteLine("Symbol definition for '{0:F1}'", sym.Sym / 10.0);
            else
                writer.WriteLine("Symbol definition for '{0}.{1}'", sym.Sym / 1000, sym.Sym % 1000);

            writer.WriteLine(" {0}': ", sym.Description);
            writer.WriteLine("  Size={0}, Otp={1}, SymTp={2}, Flags={3}", sym.Size, sym.Otp, sym.SymTp, sym.Flags);
            if (level >= 1)
                writer.WriteLine("  Extent={0}", sym.Extent);  // extents don't round trip exactly.
            writer.Write("  ");
            if (level >= 1)
                writer.Write("Selected={0}, ", sym.Selected);
            writer.Write("  Status={0}, Tool={1}", sym.Status, sym.Tool);

            if (version <= 8)
                writer.WriteLine(", FrWidth={0}", sym.FrWidth);
            else
                writer.WriteLine();

            if (version >= 9) 
                writer.WriteLine("  CsMode={0}, CsObjType={1}, CsCdFlags={2}", sym.CsMode, sym.CsObjType, sym.CsCdFlags);

            if (level >= 5)
                writer.WriteLine("  FilePos={0}", sym.FilePos);

            if (version <= 8) {
                writer.Write("  Colorset=");
                DumpBitmap(sym.ColorSet, 0, 32);
            }
            else {
                writer.Write("  ColorsUsed=");
                for (int i = 0; i < sym.nColors; ++i)
                    writer.Write("{0} ", sym.ColorsUsed[i]);
                writer.WriteLine();
            }

            writer.WriteLine("  Icon=");
            if (version >= 9)
                DumpIcon(sym.IconBits, IconFormat.Ocad9);
            else if ((sym.Flags & 2) == 2)
                DumpIcon(sym.IconBits, IconFormat.Ocad8Compressed);
            else
                DumpIcon(sym.IconBits, IconFormat.Ocad6);

            if (sym is OcadPointSymbol) DumpOcadPointSymbol(sym as OcadPointSymbol);
            if (sym is OcadLineSymbol) DumpOcadLineSymbol(sym as OcadLineSymbol);
            if (sym is OcadLineTextSymbol) DumpOcadLineTextSymbol(sym as OcadLineTextSymbol);
            if (sym is OcadAreaSymbol) DumpOcadAreaSymbol(sym as OcadAreaSymbol);
            if (sym is OcadTextSymbol) DumpOcadTextSymbol(sym as OcadTextSymbol);
            if (sym is OcadRectSymbol) DumpOcadRectSymbol(sym as OcadRectSymbol);
        }

        void DumpOcadPointSymbol(OcadPointSymbol sym)
        {
            writer.WriteLine("  DataSize={0}", sym.DataSize);
            writer.WriteLine("  Reserved={0}", sym.Reserved);
            foreach (OcadSymbolElt elt in sym.symbolElts)
                DumpOcadSymbolElt(elt);

            writer.WriteLine();
        }

        void DumpOcadLineSymbol(OcadLineSymbol sym)
        {
            writer.WriteLine("  LineColor={0}, LineWidth={1}, LineEnds={2}", sym.LineColor, sym.LineWidth, sym.LineEnds);
            writer.WriteLine("  DistFromStart={0}, DistToEnd={1}, MainLength={2}, EndLength={3}", sym.DistFromStart, sym.DistToEnd, sym.MainLength, sym.EndLength);
            writer.WriteLine("  MainGap={0}, SecGap={1}, EndGap={2}", sym.MainGap, sym.SecGap, sym.EndGap);
            writer.WriteLine("  MinSym={0}, nPrimSym={1}, PrimSymDist={2}", sym.MinSym, sym.nPrimSym, sym.PrimSymDist);
            writer.WriteLine("  DblMode={0}, DblFlags={1}", sym.DblMode, sym.DblFlags);
            writer.WriteLine("  DblFillColor={0}, DblLeftColor={1}, DblRightColor={2}", sym.DblFillColor, sym.DblLeftColor, sym.DblRightColor);
            writer.WriteLine("  DblWidth={0}, DblLeftWidth={1}, DblRightWidth={2}", sym.DblWidth, sym.DblLeftWidth, sym.DblRightWidth);
            writer.WriteLine("  DblLength={0}, DblGap={1}", sym.DblLength, sym.DblGap);
            writer.WriteLine("  DecMode={0}, DecLast={1}", sym.DecMode, sym.DecLast);
            writer.Write("  FrWidth = {0}", sym.FrWidth);
            if (sym.FrWidth > 0 || level >= 1)
                writer.WriteLine(", FrColor={0}, FrStyle={1}", sym.FrColor, sym.FrStyle);
            else
                writer.WriteLine(); // if width is zero, nothing is drawn, so other fields are irrelevant.
            writer.WriteLine("  DblRes=[{0},{1},{2}]", sym.DblRes[0], sym.DblRes[1], sym.DblRes[2]);
            writer.WriteLine("  DecRes={0}", sym.DecRes);

            if (sym.PrimDElts.Length > 0) {
                writer.WriteLine("  PrimDElts:");
                foreach (OcadSymbolElt elt in sym.PrimDElts)
                    DumpOcadSymbolElt(elt);
            }
            if (sym.SecDElts.Length > 0) {
                writer.WriteLine("  SecDElts:");
                foreach (OcadSymbolElt elt in sym.SecDElts)
                    DumpOcadSymbolElt(elt);
            }
            if (sym.CornerDElts.Length > 0) {
                writer.WriteLine("  CornerDElts:");
                foreach (OcadSymbolElt elt in sym.CornerDElts)
                    DumpOcadSymbolElt(elt);
            }
            if (sym.StartDElts.Length > 0) {
                writer.WriteLine("  StartDElts:");
                foreach (OcadSymbolElt elt in sym.StartDElts)
                    DumpOcadSymbolElt(elt);
            }
            if (sym.EndDElts.Length > 0) {
                writer.WriteLine("  EndDElts:");
                foreach (OcadSymbolElt elt in sym.EndDElts)
                    DumpOcadSymbolElt(elt);
            }

            writer.WriteLine("UseSymbolFlags:{0}  Reserved:{1}", sym.UseSymbolFlags, sym.Reserved);
            writer.WriteLine();
        }

        void DumpOcadLineTextSymbol(OcadLineTextSymbol sym)
        {
            writer.WriteLine("  FontName={0}, FontColor={1}, FontSize={2}", sym.FontName, sym.FontColor, sym.FontSize);
            writer.WriteLine("  Weight={0}, Italic={1}, CharSpace={2}, WordSpace={3}", sym.Weight, sym.Italic, sym.CharSpace, sym.WordSpace);
            writer.WriteLine("  FrMode={0}, FrName={1}, FrColor={2}, FrSize={3}", sym.FrMode, sym.FrName, sym.FrColor, sym.FrSize);
            writer.WriteLine("  FrWeight={0}, FrItalic={1}, FrOfX={2}, FrOfY={3}", sym.FrWeight, sym.FrItalic, sym.FrOfX, sym.FrOfY);

            writer.WriteLine();
        }

        void DumpOcadAreaSymbol(OcadAreaSymbol sym)
        {
            writer.WriteLine("  Flags={0:X}, FillOn={1}, FillColor={2}", sym.Flags, sym.FillOn, sym.FillColor);
            writer.Write("  HatchMode={0}", sym.HatchMode);
            if (level >= 1 || sym.HatchMode > 0)
                writer.WriteLine(", HatchColor={0}, HatchLineWidth={1}, HatchDist={2}", sym.HatchColor, sym.HatchLineWidth, sym.HatchDist);
            else
                writer.WriteLine();
            writer.WriteLine("  HatchAngle1={0}, HatchAngle2={1}, HatchRes={2}", sym.HatchAngle1, sym.HatchAngle2, sym.HatchRes);
            writer.WriteLine("  StructMode={0}, StructWidth={1}, StructHeight={2}, StructAngle={3}", sym.StructMode, sym.StructWidth, sym.StructHeight, sym.StructAngle);
            if (sym.DataSize > 0) {
                writer.WriteLine("  StructElts:");
                foreach (OcadSymbolElt elt in sym.StructElts)
                    DumpOcadSymbolElt(elt);
            }

            writer.WriteLine();
        }

        void DumpOcadRectSymbol(OcadRectSymbol sym)
        {
            writer.WriteLine("  LineColor={0}, LineWidth={1}, Radius={2}", sym.LineColor, sym.LineWidth, sym.Radius);
            writer.WriteLine("  GridFlags={0:X}, CellWidth={1}, CellHeight={2}", sym.GridFlags, sym.CellWidth, sym.CellHeight);
            writer.WriteLine("  ResGridLineColor={0}, ResGridLineWidth={1}", sym.ResGridLineColor, sym.ResGridLineWidth);
            writer.WriteLine("  UnnumCells={0}, GridRes2={1}", sym.UnnumCells, sym.GridRes2);
            writer.WriteLine("  UnnumText={0}", sym.UnnumText);
            writer.WriteLine("  ResFontName={0}", sym.ResFontName);
            writer.WriteLine("  ResFontColor={0}, ResFontSize={1}, ResWeight={2}", sym.ResFontColor, sym.FontSize, sym.ResWeight);
            writer.WriteLine("  ResItalic={0}, ResOfsx={1}, ResOfsY={2}", sym.ResItalic, sym.ResOfsX, sym.ResOfsY);
            writer.WriteLine();
        }

        void DumpOcadTextSymbol(OcadTextSymbol sym)
        {
            writer.WriteLine("  FontName={0}, FontColor={1}, FontSize={2}", sym.FontName, sym.FontColor, sym.FontSize);
            writer.WriteLine("  Weight={0}, Italic={1}, CharSpace={2}, WordSpace={3}", sym.Weight, sym.Italic, sym.CharSpace, sym.WordSpace);
            writer.WriteLine("  Alignment={0}, LineSpace={1}, ParaSpace={2}", sym.Alignment, sym.LineSpace, sym.ParaSpace);
            writer.WriteLine("  IndentFirst={0}, IndentOther={1}", sym.IndentFirst, sym.IndentOther);
            writer.Write("  nTabs={0}: ", sym.nTabs);
            for (int i = 0; i < sym.nTabs; ++i)
                writer.Write("{0} ", sym.Tabs[i]);
            writer.WriteLine();
            writer.WriteLine("  LBOn={0}, LBColor={1}, LBWidth={2}, LBDist={3}", sym.LBOn, sym.LBColor, sym.LBWidth, sym.LBDist);
            writer.WriteLine("  FrMode={0}, FrName={1}, FrColor={2}, FrSize={3}", sym.FrMode, sym.FrName, sym.FrColor, sym.FrSize);
            writer.WriteLine("  FrWeight={0}, FrItalic={1}, FrOfX={2}, FrOfY={3}", sym.FrWeight, sym.FrItalic, sym.FrOfX, sym.FrOfY);

            writer.WriteLine();
        }

        void DumpOcadSymbolElt(OcadSymbolElt symelt)
        {
            writer.Write("  SymbolElement of type {0} -- ", symelt.stType);
            if (symelt.stType == 1) writer.WriteLine("line");
            else if (symelt.stType == 2) writer.WriteLine("area");
            else if (symelt.stType == 3) writer.WriteLine("circle");
            else if (symelt.stType == 4) writer.WriteLine("dot");
            else writer.WriteLine("UNKNOWN");

            writer.Write("    ");
            if (level >= 1)
                writer.WriteLine("stFlags={0}, stColor={1}, stLineWidth={2}, stDiameter={3}", symelt.stFlags, symelt.stColor, symelt.stLineWidth, symelt.stDiameter);
            else {
                // Only write the values that are significant.
                if (symelt.stType == 1)
                    writer.Write("stFlags={0}  ", symelt.stFlags);
                writer.Write("stColor={0}  ", symelt.stColor);
                if (symelt.stType == 1 || symelt.stType == 3)
                    writer.Write("stLineWidth={0}  ", symelt.stLineWidth);
                if (symelt.stType == 3 || symelt.stType == 4)
                    writer.Write("stDiameter={0}", symelt.stDiameter);
                writer.WriteLine();
            }

            DumpCoords(symelt.stCoords);
        }

        void DumpCoords(OcadCoord[] coords)
        {
            int i = 0, curLen;
            while (i < coords.Length) {
                writer.Write("    ");
                curLen = 4;
                while (i < coords.Length && curLen < 60) {
                    string s = string.Format("{0} ", coords[i]);
                    curLen += s.Length;
                    writer.Write(s);
                    ++i;
                }
                writer.WriteLine();
            }
        }

        void DumpOcadIndex(OcadIndex index)
        {
            writer.WriteLine("Index entry Sym={0:F1}", index.Sym / 10.0);

            if (version >= 9) {
                writer.WriteLine("  ObjType={0}  Rex={1}  Status={2}", index.ObjType, index.Rex, index.Status);
                writer.WriteLine("  ViewType={0}  Color={1}  ImpLayer={2}", index.ViewType, index.Color, index.ImpLayer);
            }

            if (level >= 1)
                writer.WriteLine("  Bounds={0}-{1}", index.LowerLeft, index.UpperRight);
            if (level >= 5)
                writer.WriteLine("  Pos={0}, Len={1}", index.Pos, index.Len);
        }

        void DumpOcadObject(OcadObject obj)
        {
            writer.WriteLine("Object Sym={0} Otp={1}", obj.Sym / 10.0, obj.Otp);
            writer.WriteLine("  nItem={0}, nText={1}", obj.nItem, obj.nText);
            writer.WriteLine("  Ang={0}", obj.Ang);
            writer.WriteLine("  Col={0} (0x{0:X})", obj.Col);
            writer.WriteLine("  LineWidth={0}  DiamFlags={1}", obj.LineWidth, obj.DiamFlags);

            if (obj.nText > 0)
                writer.WriteLine("  Text={0}", obj.text);
            if (obj.nObjectString > 0)
                writer.WriteLine("  Object String={0} (type={1})", obj.objectString, obj.ObjectStringType);
            if (obj.nDatabaseString > 0)
                writer.WriteLine("  Database string={0}", obj.databaseString);
            DumpCoords(obj.coords);
            writer.WriteLine();
        }

        OcadFileHeader ReadFileHeader()
        {
            OcadFileHeader fh = new OcadFileHeader();
            fh.Read(reader);

            return fh;
        }

        OcadSymbolHeader ReadSymbolHeader()
        {
            OcadSymbolHeader sh = new OcadSymbolHeader();
            sh.Read(reader);
            return sh;
        }

        OcadSetup ReadSetup(int filePos, int size)
        {
            OcadSetup setup = new OcadSetup();
            setup.Read(reader, filePos, size);
            return setup;
        }

        OcadSymbolBlocks ReadSymbolBlocks(int firstBlock)
        {
            OcadSymbolBlocks sb = new OcadSymbolBlocks();
            sb.Read(reader, firstBlock);
            return sb;
        }

        OcadSymbol[] ReadSymbols(OcadSymbolBlocks b)
        {
            List<OcadSymbol> list = new List<OcadSymbol>();

            for (int i = 0; i < b.filepositions.Length; ++i) {
                if (b.filepositions[i] != 0) {
                    OcadSymbol sym;
                    reader.Seek(b.filepositions[i], SeekOrigin.Begin);
                    sym = OcadSymbol.Read(reader, version);
                    list.Add(sym);
                }
            }

            return list.ToArray();
        }

        OcadIndexBlocks ReadIndexBlocks(int firstBlock)
        {
            OcadIndexBlocks ib = new OcadIndexBlocks();
            ib.Read(reader, firstBlock, version);
            return ib;
        }

        OcadObject[] ReadObjects(OcadIndexBlocks b)
        {
            List<OcadObject> list = new List<OcadObject>();
            for (int i = 0; i < b.indexes.Length; ++i) {
                if (b.indexes[i].Sym != 0) {
                    OcadObject obj = new OcadObject();
                    reader.Seek(b.indexes[i].Pos, SeekOrigin.Begin);
                    obj.Read(reader, version);
                    list.Add(obj);
                }
            }

            return list.ToArray();
        }

        OcadStIndexBlocks ReadStringIndexBlocks(int firstBlock)
        {
            OcadStIndexBlocks stblocks = new OcadStIndexBlocks();
            stblocks.Read(reader, firstBlock);
            return stblocks;
        }

        void DumpOcadFile()
        {
            OcadFileHeader header = ReadFileHeader();
            version = header.Version;

            OcadSymbolHeader symheader = null;
            if (version <= 8)
                symheader = ReadSymbolHeader();

            OcadSetup setup = null;
            if (version <= 8)
                setup = ReadSetup(header.SetupPos, header.SetupSize);

            OcadFileInfo info = null;
            if (version <= 8) {
                info = new OcadFileInfo();
                info.Read(reader, header.InfoPos, header.InfoSize);
            }

            OcadSymbolBlocks symblocks = ReadSymbolBlocks(header.FirstSymBlk);
            OcadSymbol[] symbols = ReadSymbols(symblocks);
            OcadIndexBlocks indexblocks = ReadIndexBlocks(header.FirstIdxBlk);
            OcadStIndexBlocks stindexblocks = null;
            if (header.FirstStIndexBlk != 0)
                stindexblocks = ReadStringIndexBlocks(header.FirstStIndexBlk);

            // CONSIDER: sort symbols and objects.
            DumpFileHeader(header);
            if (symheader != null)
                DumpSymbolHeader(symheader);
            if (setup != null)
                DumpSetup(setup);
            if (info != null)
                DumpInfo(info);

            for (int i = 0; i < symbols.Length; ++i)
                DumpOcadSymbol(symbols[i]);

            for (int i = 0; i < indexblocks.indexes.Length; ++i) {
                if (indexblocks.indexes[i].Sym != 0) {
                    OcadObject obj = new OcadObject();
                    DumpOcadIndex(indexblocks.indexes[i]);
                    reader.Seek(indexblocks.indexes[i].Pos, SeekOrigin.Begin);
                    obj.Read(reader, version);
                    DumpOcadObject(obj);
                }
            }

            if (stindexblocks != null) {
                for (int i = 0; i < stindexblocks.indexes.Length; ++i) {
                    if (stindexblocks.indexes[i].StType != 0) {
                        DumpParamStringIndex(stindexblocks.indexes[i]);
                    }
                }
            }
        }

        void DumpProjection()
        {
            OcadFileHeader header = ReadFileHeader();
            version = header.Version;

            OcadSymbolHeader symheader = null;
            if (version <= 8)
                symheader = ReadSymbolHeader();

            OcadSetup setup = null;
            if (version <= 8)
                setup = ReadSetup(header.SetupPos, header.SetupSize);

            OcadFileInfo info = null;
            if (version <= 8) {
                info = new OcadFileInfo();
                info.Read(reader, header.InfoPos, header.InfoSize);
            }

            OcadStIndexBlocks stindexblocks = null;
            if (header.FirstStIndexBlk != 0)
                stindexblocks = ReadStringIndexBlocks(header.FirstStIndexBlk);

            if (stindexblocks != null) {
                for (int i = 0; i < stindexblocks.indexes.Length; ++i) {
                    if (stindexblocks.indexes[i].StType == (int) OcadStringParam.ScalePar) {
                        OcadParamString param = new OcadParamString();
                        param.Read(reader, stindexblocks.indexes[i]);
                        for (int j = 0; j < param.codes.Length; ++j) {
                            if (param.codes[j] == 'i')
                                writer.WriteLine("Projection grid/zone ocad id = {0}", param.values[j]);
                        }

                    }
                }
            }
        }

        public void DumpFile(string ocadFileName, TextWriter writer)
        {
            this.writer = writer;
            reader = new FastBinaryReader(ocadFileName, Util.GetDefaultOcadEncoding());
            DumpOcadFile();

            this.writer = null;
        }

        public void DumpProjection(string ocadFileName, TextWriter writer)
        {
            this.writer = writer;
            reader = new FastBinaryReader(ocadFileName, Util.GetDefaultOcadEncoding());
            DumpProjection();
            this.writer = null;
        }
    }
}
