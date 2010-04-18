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
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
#if WPF
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using SizeF = System.Drawing.SizeF;
using Matrix = System.Drawing.Drawing2D.Matrix;
#endif
#if WPF
using System.Windows.Media;
#else
using System.Drawing;
#endif

// Several fields in the below structures are not used.
#pragma warning disable 649

namespace PurplePen.MapModel
{
	class OcadConstants {
		// Convert an OCAD 4-bit bitmap icon to a Bitmap
		public static readonly Color[] ocadColorMap4Bit = {
			Color.FromArgb(255, 0, 0, 0),
			Color.FromArgb(255, 128, 0, 0),
			Color.FromArgb(255, 0, 128, 0),
			Color.FromArgb(255, 128, 128, 0),
			Color.FromArgb(255, 0, 0, 128),
			Color.FromArgb(255, 128, 0, 128),
			Color.FromArgb(255, 0, 128, 128),
			Color.FromArgb(255, 192, 192, 192),
			Color.FromArgb(255, 128, 128, 128),
			Color.FromArgb(255, 255, 0, 0),
			Color.FromArgb(255, 0, 255, 0),
			Color.FromArgb(255, 255, 255, 0),
			Color.FromArgb(255, 0, 0, 255),
			Color.FromArgb(255, 255, 0, 255),
			Color.FromArgb(255, 0, 255, 255),
			Color.FromArgb(0, 255, 255, 255)    // transparent white
		};

		// Convert an OCAD 8-bit bitmap icon to a Bitmap
		public static readonly Color[] ocadColorMap8Bit;

		static OcadConstants() {
			// Initialize ocadColorMap8Bit to a base-5 half-tone color cube.
			ocadColorMap8Bit = new Color[125];

			for (int i = 0; i < 125; ++i) {
				int r = (i / 25) * 64;
				int g = ((i / 5) % 5) * 64;
				int b = (i % 5) * 64;
				if (r > 255) r = 255;
				if (g > 255) g = 255;
				if (b > 255) b = 255;
				ocadColorMap8Bit[i] = Color.FromArgb(255, (byte)r, (byte)g, (byte)b);
			}

			ocadColorMap8Bit[124] = Color.FromArgb(0, 255, 255, 255); // transparent white.
		}
	}

    enum OcadStringParam
    {
        CsObject = 1,
        Course = 2,
        Class = 3,
        DataSDet = 4,
        DbObject = 5,
        OimFile = 6,
        PrevObj = 7,
        Template = 8,
        Color = 9,
        SpotColor = 10,
        FileInfo = 11,
        Zoom = 12,
        ImpLayer = 13,
        OimFind = 14,
        SymTree = 15,
        CountListParam,

        FirstSingleParam = 1024,
        DisplayPar = 1024,
        OimPar = 1025,
        PrintPar = 1026,
        CdPrintPar = 1027,
        TemplatePar = 1028,
        EpsPar = 1029,
        ViewPar = 1030,
        CoursePar = 1031,
        TiffPar = 1032,
        TilesPar = 1033,
        DbPar = 1034,
        ExportPar = 1035,
        CourseSelPar = 1036,
        ExpCsTextPar = 1037,
        ExpCsStatPar = 1038,
        ScalePar = 1039,
        DbCreateObjPar = 1040,
        LastSingleParam,
        CountSingleParam = LastSingleParam - FirstSingleParam
    }


	class OcadFileHeader {
		public short OCADMark;
		public short SectionMark;
		public short Version;
		public short Subversion;
		public int FirstSymBlk, FirstIdxBlk;
		public int SetupPos, SetupSize;
		public int InfoPos, InfoSize;
		public int FirstStIndexBlk;
		public int Reserved2, Reserved3, Reserved4;

		public void Read(BinaryReader reader) {
			OCADMark = reader.ReadInt16();
			SectionMark = reader.ReadInt16();
			Version = reader.ReadInt16();
			Subversion = reader.ReadInt16();
			FirstSymBlk = reader.ReadInt32(); 
			FirstIdxBlk = reader.ReadInt32(); 
			SetupPos = reader.ReadInt32(); 
			SetupSize = reader.ReadInt32(); 
			InfoPos = reader.ReadInt32(); 
			InfoSize = reader.ReadInt32(); 
			FirstStIndexBlk = reader.ReadInt32(); 
			Reserved2 = reader.ReadInt32(); 
			Reserved3 = reader.ReadInt32(); 
			Reserved4 = reader.ReadInt32(); 
		}

		public void Write(BinaryWriter writer) {
			writer.Write(OCADMark);
			writer.Write(SectionMark);
			writer.Write(Version);
			writer.Write(Subversion);
			writer.Write(FirstSymBlk);
			writer.Write(FirstIdxBlk);
			writer.Write(SetupPos);
			writer.Write(SetupSize);
			writer.Write(InfoPos);
			writer.Write(InfoSize);
			writer.Write(FirstStIndexBlk);
			writer.Write(Reserved2);
			writer.Write(Reserved3);
			writer.Write(Reserved4);
		}
	}

	class OcadFileInfo {
		public string Info;

		public void Read(BinaryReader reader, long InfoPos, int InfoSize) {
			Info = null;
			if (InfoSize != 0) {
				reader.BaseStream.Seek(InfoPos, SeekOrigin.Begin);
				char[] ch = reader.ReadChars(InfoSize);
				int end;
				for (end = 0; end < InfoSize; ++end)
					if (ch[end] == 0)
						break;

				if (end > 0) {
					Info = new string(ch, 0, end);
				}
			}
		}
	}

	class OcadSetup {
		public OcadCoord Offset;
		public double rGridDist;
		public short WorkMode;
		public short LineMode;
		public short EditMode;
		public short ActSym;
		public double MapScale;
		public double RealWorldOfsX;
		public double RealWorldOfsY;
		public double RealWorldAngle;
		public double RealWorldGrid;
		public double GpsAngle;
		public OcadGpsAdjust[] aGpsAdjust = new OcadGpsAdjust[12];
		public int nGpsAdjust;
		public double DraftScaleX;
		public double DraftScaleY;
		public OcadCoord TempOffset;
		public string TemplateFileName;
		public ushort TemplateEnabled;
		public short TempResol;
		public double rTempAng;
		public OcadCoord Res1;
		public double Res2;
		public OcadCoord PrLowerLeft;
		public OcadCoord PrUpperRight;
		public ushort PrGrid;
		public short PrGridColor;
		public short PrOverlapX;
		public short PrOverlapY;
		public double PrintScale;
		public short PrIntensity;
		public short PrLineWidth;
		public ushort PrReserved;
		public ushort PrStdFonts;
		public ushort PrReserved2;
		public ushort PrReserved3;
		public OcadCoord PartialLowerLeft;
		public OcadCoord PartialUpperRight;
		public double Zoom;
		public OcadZoomRec[] ZoomHist = new OcadZoomRec[9];
		public int nZoomHist;
		public ushort RealWorldCoord;
		public string FileName;
		public ushort HatchAreas;
		public ushort DimTemp;
		public ushort HideTemp;
		public short TempMode;
		public short TempColor;

		public OcadSetup Clone() {
			return (OcadSetup) this.MemberwiseClone();
		}

		private bool DoneReading(BinaryReader reader, long filePos, long fileLen) {
			if (reader.BaseStream.Position - filePos >= fileLen)
				return true;
			else
				return false;
		}

		public void Read(BinaryReader reader, long filePos, long fileLen) {
			reader.BaseStream.Seek(filePos, SeekOrigin.Begin);

			Offset.Read(reader);									if (DoneReading(reader, filePos, fileLen)) return;
			rGridDist = reader.ReadDouble();						if (DoneReading(reader, filePos, fileLen)) return;
			WorkMode = reader.ReadInt16();							if (DoneReading(reader, filePos, fileLen)) return;
			LineMode = reader.ReadInt16();							if (DoneReading(reader, filePos, fileLen)) return;
			EditMode = reader.ReadInt16();							if (DoneReading(reader, filePos, fileLen)) return;
			ActSym = reader.ReadInt16();							if (DoneReading(reader, filePos, fileLen)) return;
			MapScale = reader.ReadDouble();							if (DoneReading(reader, filePos, fileLen)) return;
			RealWorldOfsX = reader.ReadDouble();					if (DoneReading(reader, filePos, fileLen)) return;
			RealWorldOfsY = reader.ReadDouble();					if (DoneReading(reader, filePos, fileLen)) return;
			RealWorldAngle = reader.ReadDouble();					if (DoneReading(reader, filePos, fileLen)) return;
			RealWorldGrid = reader.ReadDouble();					if (DoneReading(reader, filePos, fileLen)) return;
			GpsAngle = reader.ReadDouble();							if (DoneReading(reader, filePos, fileLen)) return;
			for (int i = 0; i < aGpsAdjust.Length; ++i) 
				aGpsAdjust[i].Read(reader);
			nGpsAdjust = reader.ReadInt32();						if (DoneReading(reader, filePos, fileLen)) return;
			DraftScaleX = reader.ReadDouble();						if (DoneReading(reader, filePos, fileLen)) return;
			DraftScaleY = reader.ReadDouble();						if (DoneReading(reader, filePos, fileLen)) return;
			TempOffset.Read(reader);								if (DoneReading(reader, filePos, fileLen)) return;
			TemplateFileName = Util.ReadDelphiString(reader, 255);	if (DoneReading(reader, filePos, fileLen)) return;
			TemplateEnabled = reader.ReadUInt16();					if (DoneReading(reader, filePos, fileLen)) return;
			TempResol = reader.ReadInt16();							if (DoneReading(reader, filePos, fileLen)) return;
			rTempAng = reader.ReadDouble();							if (DoneReading(reader, filePos, fileLen)) return;
			Res1.Read(reader);										if (DoneReading(reader, filePos, fileLen)) return;
			Res2 = reader.ReadDouble();								if (DoneReading(reader, filePos, fileLen)) return;
			PrLowerLeft.Read(reader);								if (DoneReading(reader, filePos, fileLen)) return;
			PrUpperRight.Read(reader);								if (DoneReading(reader, filePos, fileLen)) return;
			PrGrid = reader.ReadUInt16();							if (DoneReading(reader, filePos, fileLen)) return;
			PrGridColor = reader.ReadInt16();						if (DoneReading(reader, filePos, fileLen)) return;
			PrOverlapX = reader.ReadInt16();						if (DoneReading(reader, filePos, fileLen)) return;
			PrOverlapY = reader.ReadInt16();						if (DoneReading(reader, filePos, fileLen)) return;
			PrintScale = reader.ReadDouble();						if (DoneReading(reader, filePos, fileLen)) return;
			PrIntensity = reader.ReadInt16();						if (DoneReading(reader, filePos, fileLen)) return;
			PrLineWidth = reader.ReadInt16();						if (DoneReading(reader, filePos, fileLen)) return;
			PrReserved = reader.ReadUInt16();						if (DoneReading(reader, filePos, fileLen)) return;
			PrStdFonts = reader.ReadUInt16();						if (DoneReading(reader, filePos, fileLen)) return;
			PrReserved2 = reader.ReadUInt16();						if (DoneReading(reader, filePos, fileLen)) return;
			PrReserved3 = reader.ReadUInt16();						if (DoneReading(reader, filePos, fileLen)) return;
			PartialLowerLeft.Read(reader);							if (DoneReading(reader, filePos, fileLen)) return;
			PartialUpperRight.Read(reader);							if (DoneReading(reader, filePos, fileLen)) return;
			Zoom = reader.ReadDouble();								if (DoneReading(reader, filePos, fileLen)) return;
			for (int i = 0; i < ZoomHist.Length; ++i)
				ZoomHist[i].Read(reader);
			nZoomHist = reader.ReadInt32();							if (DoneReading(reader, filePos, fileLen)) return;
			RealWorldCoord =  reader.ReadUInt16();					if (DoneReading(reader, filePos, fileLen)) return;
			FileName = Util.ReadDelphiString(reader, 255);			if (DoneReading(reader, filePos, fileLen)) return;
			HatchAreas = reader.ReadUInt16();						if (DoneReading(reader, filePos, fileLen)) return;
			DimTemp = reader.ReadUInt16();							if (DoneReading(reader, filePos, fileLen)) return;
			HideTemp = reader.ReadUInt16();							if (DoneReading(reader, filePos, fileLen)) return;
			TempMode = reader.ReadInt16();							if (DoneReading(reader, filePos, fileLen)) return;
			TempColor = reader.ReadInt16();							if (DoneReading(reader, filePos, fileLen)) return;
		}

		public void Write(BinaryWriter writer, int version) {
			int i;
			OcadGpsAdjust emptyGpsAdjust = new OcadGpsAdjust();
			OcadZoomRec emptyZoomRec = new OcadZoomRec();

			Offset.Write(writer);									
			writer.Write(rGridDist);						
			writer.Write(WorkMode);							
			writer.Write(LineMode);							
			writer.Write(EditMode);							
			writer.Write(ActSym);							
			writer.Write(MapScale);							
			writer.Write(RealWorldOfsX);					
			writer.Write(RealWorldOfsY);					
			writer.Write(RealWorldAngle);					
			writer.Write(RealWorldGrid);					
			writer.Write(GpsAngle);							
			for (i = 0; i < nGpsAdjust; ++i) 
				aGpsAdjust[i].Write(writer);
			for (; i < 12; ++i)
				emptyGpsAdjust.Write(writer);
			writer.Write(nGpsAdjust);						
			writer.Write(DraftScaleX);						
			writer.Write(DraftScaleY);						
			TempOffset.Write(writer);								
			Util.WriteDelphiString(writer, TemplateFileName, 255);	
			writer.Write(TemplateEnabled);					
			writer.Write(TempResol);							
			writer.Write(rTempAng);							
			Res1.Write(writer);										
			writer.Write(Res2);								
			PrLowerLeft.Write(writer);								
			PrUpperRight.Write(writer);								
			writer.Write(PrGrid);							
			writer.Write(PrGridColor);						
			writer.Write(PrOverlapX);						
			writer.Write(PrOverlapY);						
			writer.Write(PrintScale);						
			writer.Write(PrIntensity);						
			writer.Write(PrLineWidth);						
			writer.Write(PrReserved);						
			writer.Write(PrStdFonts);						
			writer.Write(PrReserved2);						
			writer.Write(PrReserved3);						
			PartialLowerLeft.Write(writer);							
			PartialUpperRight.Write(writer);							
			writer.Write(Zoom);								
			for (i = 0; i < nZoomHist; ++i)
				ZoomHist[i].Write(writer);
			for (; i < 9; ++i)
				emptyZoomRec.Write(writer);							
			writer.Write(nZoomHist);

			if (version >= 7) {
				writer.Write(RealWorldCoord);					
				Util.WriteDelphiString(writer, FileName, 255);			
				writer.Write(HatchAreas);						
				writer.Write(DimTemp);							
				writer.Write(HideTemp);							
				writer.Write(TempMode);							
				writer.Write(TempColor);							
			}
		}
	}

	struct OcadGpsAdjust {
		public OcadCoord lpMap;
		public double Lat, Long;
		public string Name;

		public void Read(BinaryReader reader) {
			lpMap.Read(reader);
			Lat = reader.ReadDouble();
			Long = reader.ReadDouble();
			Name = Util.ReadDelphiString(reader, 15);
		}

		public void Write(BinaryWriter writer) {
			lpMap.Write(writer);
			writer.Write(Lat);
			writer.Write(Long);
			Util.WriteDelphiString(writer, Name, 15);
		}
	}

	struct OcadZoomRec {
		public double Zoom;
		public OcadCoord Offset;

		public void Read(BinaryReader reader) {
			Zoom = reader.ReadDouble();
			Offset.Read(reader);
		}

		public void Write(BinaryWriter writer) {
			writer.Write(Zoom);
			Offset.Write(writer);
		}
	}

	class OcadSymbolHeader {
		public short nColors, nColorSep;
		public short CyanFreq, CyanAng;
		public short MagentaFreq, MagentaAng;
		public short YellowFreq, YellowAng;
		public short BlackFreq, BlackAng;
		public short Res1, Res2;
		public OcadColorInfo[] aColorInfo = new OcadColorInfo[256];
		public OcadColorSep[] aColorSep = new OcadColorSep[32];

		public OcadSymbolHeader Clone() {
			return (OcadSymbolHeader) this.MemberwiseClone();
		}

		public void Read(BinaryReader reader) {
			nColors = reader.ReadInt16();
			nColorSep = reader.ReadInt16();
			CyanFreq = reader.ReadInt16();
			CyanAng = reader.ReadInt16();
			MagentaFreq = reader.ReadInt16();
			MagentaAng = reader.ReadInt16();
			YellowFreq = reader.ReadInt16();
			YellowAng = reader.ReadInt16();
			BlackFreq = reader.ReadInt16();
			BlackAng = reader.ReadInt16();
			Res1 = reader.ReadInt16();
			Res2 = reader.ReadInt16();

			for (int i = 0; i < aColorInfo.Length; ++i) {
				aColorInfo[i] = new OcadColorInfo();
				aColorInfo[i].Read(reader);
			}

			for (int i = 0; i < aColorSep.Length; ++i) {
				aColorSep[i] = new OcadColorSep();
				aColorSep[i].Read(reader);
			}
		}

		public void Write(BinaryWriter writer) {
			writer.Write(nColors);
			writer.Write(nColorSep);
			writer.Write(CyanFreq);
			writer.Write(CyanAng);
			writer.Write(MagentaFreq);
			writer.Write(MagentaAng);
			writer.Write(YellowFreq);
			writer.Write(YellowAng);
			writer.Write(BlackFreq);
			writer.Write(BlackAng);
			writer.Write(Res1);
			writer.Write(Res2);

			Debug.Assert(aColorInfo.Length == 256 && aColorSep.Length == 32);

			foreach (OcadColorInfo colorInfo in aColorInfo) 
				colorInfo.Write(writer);

			foreach (OcadColorSep colorSep in aColorSep)
				colorSep.Write(writer);
		}
	}

	class OcadColorInfo {
		public short ColorNum;
		public short Reserved;
		public OcadCmyk Color;
		public string ColorName;
		public byte[] SepPercentage = new byte[32];

		public void Read(BinaryReader reader) {
			ColorNum = reader.ReadInt16();
			Reserved = reader.ReadInt16();
			Color = new OcadCmyk(); Color.Read(reader);
			ColorName = Util.ReadDelphiString(reader, 31);
			SepPercentage = Util.ReadByteArray(reader, 32);
		}

		public void Write(BinaryWriter writer) {
			writer.Write(ColorNum);
			writer.Write(Reserved);
			Color.Write(writer);
			Util.WriteDelphiString(writer, ColorName, 31);
			Debug.Assert(SepPercentage.Length == 32);
			writer.Write(SepPercentage);
		}
	}

	struct OcadCmyk {
		public byte cyan, magenta, yellow, black;

		public void Read(BinaryReader reader) {
			cyan = reader.ReadByte();
			magenta = reader.ReadByte();
			yellow = reader.ReadByte();
			black = reader.ReadByte();
		}

		public void Write(BinaryWriter writer) {
			writer.Write(cyan);
			writer.Write(magenta);
			writer.Write(yellow);
			writer.Write(black);
		}

		public override string ToString() {
			return string.Format(" Cy={0} Mg={1} Yl={2} Bk={3}", cyan, magenta, yellow, black);
		}
	}

	class OcadColorSep {
		public string SepName;
		public OcadCmyk Color;
		public short RasterFreq, RasterAngle;

		public void Read(BinaryReader reader) {
			SepName = Util.ReadDelphiString(reader, 15);
			Color = new OcadCmyk(); Color.Read(reader);
			RasterFreq = reader.ReadInt16();
			RasterAngle = reader.ReadInt16();
		}

		public void Write(BinaryWriter writer) {
			Util.WriteDelphiString(writer, SepName, 15);
			Color.Write(writer);
			writer.Write(RasterFreq);
			writer.Write(RasterAngle);
		}
	}
		
	class OcadSymbolBlocks {
		public int[] filepositions;

		public void Read(BinaryReader reader, int firstBlock) {
			List<int> posList = new List<int>();
			int nextBlock = firstBlock;

			while (nextBlock != 0) {
				reader.BaseStream.Seek(nextBlock, SeekOrigin.Begin);
				nextBlock = reader.ReadInt32();
				for (int i = 0; i < 256; ++i) {
					int pos = reader.ReadInt32();
					if (pos != 0)
						posList.Add(pos);
				}
			}

			filepositions = posList.ToArray();
		}
	}

	class OcadSymbolBlock {
		public int NextBlock;
		public int[] FilePos;

		public OcadSymbolBlock() {
			FilePos = new int[256];
		}

		public void Write(BinaryWriter writer) {
			writer.Write(NextBlock);
			Debug.Assert(FilePos.Length == 256);
			foreach (int pos in FilePos)
				writer.Write(pos);
		}
	}

	abstract class OcadSymbol {
		public int Size;
		public int Sym;                 // meaning different in version <= 8 and version >=9
		public short Otp;
		public byte SymTp;
		public byte Flags;
		public int Extent;
		public bool Selected;
		public byte Status;
		public short Tool;
		public short FrWidth;
        public byte CsMode;
        public byte CsObjType;
        public byte CsCdFlags;
		public int FilePos;
        public short Group;
		public byte[] ColorSet = new byte[256/8];      // used in version <= 8
        public short nColors;                                     // used in version >= 9
        public short[] ColorsUsed;                            // used in version >= 9
		public string Description;
		public byte[] IconBits;

		static public OcadSymbol Read(BinaryReader reader, int version) {

			int Size;
            int Sym = 0;
            short Otp;
            byte SymTp = 0;
            if (version <= 8) {
                Size = reader.ReadInt16();
                Sym = reader.ReadInt16();
                Otp = reader.ReadInt16();
                SymTp = reader.ReadByte();
            }
            else {
                Size = reader.ReadInt32();
                Sym = reader.ReadInt32();
                Otp = reader.ReadByte();
            }

			byte Flags = reader.ReadByte();
			OcadSymbol sym;

            if (Otp == 1) {
                sym = new OcadPointSymbol();
            }
            else if (Otp == 2 && SymTp == 0)
                sym = new OcadLineSymbol();
            else if (Otp == 2 && SymTp == 1)
                sym = new OcadLineTextSymbol();
            else if (Otp == 3)
                sym = new OcadAreaSymbol();
            else if (Otp == 4)
                sym = new OcadTextSymbol();
            else if (Otp == 5 || Otp == 7)
                sym = new OcadRectSymbol();
            else if (Otp == 6)
                sym = new OcadLineTextSymbol();
            else {
                Debug.Assert(false);
                return null;
            }

			sym.Size = Size;
			sym.Sym = Sym;
			sym.Otp = Otp;
			sym.SymTp = SymTp;
			sym.Flags = Flags;
            if (version <= 8)
			    sym.Extent = reader.ReadInt16();
			sym.Selected = (reader.ReadByte() != 0) ? true : false;
			sym.Status = reader.ReadByte();

            if (version <= 8) {
                sym.Tool = reader.ReadInt16();
                sym.FrWidth = reader.ReadInt16();
            }
            else {
                sym.Tool = reader.ReadByte();
                sym.CsMode = reader.ReadByte();
                sym.CsObjType = reader.ReadByte();
                sym.CsCdFlags = reader.ReadByte();
                sym.Extent = reader.ReadInt32();
            }

			sym.FilePos = reader.ReadInt32();

            if (version <= 8) {
                sym.ColorSet = Util.ReadByteArray(reader, 32);
            }
            else {
                sym.Group = reader.ReadInt16();
                sym.nColors = reader.ReadInt16();
                sym.ColorsUsed = new short[sym.nColors];
                for (int i = 0; i < 14; ++i) {
                    short colorId = reader.ReadInt16();
                    if (i < sym.nColors)
                        sym.ColorsUsed[i] = colorId;
                }
            }

			sym.Description = Util.ReadDelphiString(reader, 31);
			sym.IconBits = Util.ReadByteArray(reader, (version <= 8) ? 264 : 484);

			sym.ReadExtra(reader, version);

			return sym;
		}

		protected abstract void ReadExtra(BinaryReader reader, int version);

		public virtual void Write(BinaryWriter writer, int version) {
			FilePos = (int) writer.Seek(0, SeekOrigin.Current);

            if (version <= 8) {
                writer.Write((short) Size);
                writer.Write((short) Sym);
                writer.Write(Otp);
                writer.Write(SymTp);
                writer.Write(Flags);
                writer.Write((short) Extent);
            }
            else {
                writer.Write(Size);
                writer.Write(Sym);
                writer.Write((byte) Otp);
                writer.Write(Flags);
            }


			writer.Write(Selected ? (byte) 1: (byte)0);
			writer.Write(Status);

            if (version <= 8) {
                writer.Write(Tool);
                writer.Write(FrWidth);
            }
            else {
                writer.Write((byte) Tool);
                writer.Write(CsMode);
                writer.Write(CsObjType);
                writer.Write(CsCdFlags);
                writer.Write(Extent);
            }

			writer.Write(FilePos);

            if (version <= 8)
                writer.Write(ColorSet);
            else {
                writer.Write(Group);
                writer.Write(nColors);
                for (int i = 0; i < 14; ++i) {
                    if (i < nColors)
                        writer.Write(ColorsUsed[i]);
                    else
                        writer.Write((short) 0);
                }
            }

			Util.WriteDelphiString(writer, Description, 31);
			writer.Write(IconBits);
		}

		// Update the Size field at the beginning of the structure.
		protected void UpdateSize(BinaryWriter writer) {
			int current = (int) writer.Seek(0, SeekOrigin.Current);
			writer.Seek(FilePos, SeekOrigin.Begin);
			writer.Write((ushort) (current - FilePos));
			writer.Seek(current, SeekOrigin.Begin);
		}
	}

	class OcadPointSymbol: OcadSymbol {
		public short DataSize;
		public short Reserved;
		public OcadSymbolElt[] symbolElts;

		protected override void ReadExtra(BinaryReader reader, int version) {
			DataSize = reader.ReadInt16();
			Reserved = reader.ReadInt16();

			symbolElts = OcadSymbolElt.ReadElements(reader, DataSize);
		}

		public override void Write(BinaryWriter writer, int version) {
			base.Write(writer, version);

			writer.Write(DataSize);
			writer.Write(Reserved);
			OcadSymbolElt.WriteElements(writer, symbolElts);

			UpdateSize(writer);
		}
	}

	class OcadLineSymbol: OcadSymbol {
		public short LineColor;
		public short LineWidth;
		public ushort LineEnds;
		public short DistFromStart;
		public short DistToEnd;
		public short MainLength;
		public short EndLength;
		public short MainGap;
		public short SecGap;
		public short EndGap;
		public short MinSym;
		public short nPrimSym;
		public short PrimSymDist;
		public ushort DblMode;
		public ushort DblFlags;
		public short DblFillColor;
		public short DblLeftColor;
		public short DblRightColor;
		public short DblWidth;
		public short DblLeftWidth;
		public short DblRightWidth;
		public short DblLength;
		public short DblGap;
		public short[] DblRes = new short[3];
		public ushort DecMode;
		public short DecLast;
		public short DecRes;
		public short FrColor;
		public new short FrWidth;
		public short FrStyle;
		public short PrimDSize;
		public short SecDSize;
		public short CornerDSize;
		public short StartDSize;
		public short EndDSize;
		public short Reserved;
		public OcadSymbolElt[] PrimDElts;
		public OcadSymbolElt[] SecDElts;
		public OcadSymbolElt[] CornerDElts;
		public OcadSymbolElt[] StartDElts;
		public OcadSymbolElt[] EndDElts;

		protected override void ReadExtra(BinaryReader reader, int version) {
			LineColor = reader.ReadInt16();
			LineWidth = reader.ReadInt16();
			LineEnds = reader.ReadUInt16();
			DistFromStart = reader.ReadInt16();
			DistToEnd = reader.ReadInt16();
			MainLength = reader.ReadInt16();
			EndLength = reader.ReadInt16();
			MainGap = reader.ReadInt16();
			SecGap = reader.ReadInt16();
			EndGap = reader.ReadInt16();
			MinSym = reader.ReadInt16();
			nPrimSym = reader.ReadInt16();
			PrimSymDist = reader.ReadInt16();
			DblMode = reader.ReadUInt16();
			DblFlags = reader.ReadUInt16();
			DblFillColor = reader.ReadInt16();
			DblLeftColor = reader.ReadInt16();
			DblRightColor = reader.ReadInt16();
			DblWidth = reader.ReadInt16();
			DblLeftWidth = reader.ReadInt16();
			DblRightWidth = reader.ReadInt16();
			DblLength = reader.ReadInt16();
			DblGap = reader.ReadInt16();
			for (int i = 0; i < 3; ++i) { DblRes[i] = reader.ReadInt16(); }
			DecMode = reader.ReadUInt16();
			DecLast = reader.ReadInt16();
			DecRes = reader.ReadInt16();
			FrColor = reader.ReadInt16();
			FrWidth = reader.ReadInt16();
			FrStyle = reader.ReadInt16();
			PrimDSize = reader.ReadInt16();
			SecDSize = reader.ReadInt16();
			CornerDSize = reader.ReadInt16();
			StartDSize = reader.ReadInt16();
			EndDSize = reader.ReadInt16();
			Reserved = reader.ReadInt16();

			PrimDElts = OcadSymbolElt.ReadElements(reader, PrimDSize);
			SecDElts = OcadSymbolElt.ReadElements(reader, SecDSize);
			CornerDElts = OcadSymbolElt.ReadElements(reader, CornerDSize);
			StartDElts = OcadSymbolElt.ReadElements(reader, StartDSize);
			EndDElts = OcadSymbolElt.ReadElements(reader, EndDSize);
		}

		public override void Write(BinaryWriter writer, int version) {
			base.Write (writer, version);

			writer.Write(LineColor);
			writer.Write(LineWidth);
			writer.Write(LineEnds);
			writer.Write(DistFromStart);
			writer.Write(DistToEnd);
			writer.Write(MainLength);
			writer.Write(EndLength);
			writer.Write(MainGap);
			writer.Write(SecGap);
			writer.Write(EndGap);
			writer.Write(MinSym);
			writer.Write(nPrimSym);
			writer.Write(PrimSymDist);
			writer.Write(DblMode);
			writer.Write(DblFlags);
			writer.Write(DblFillColor);
			writer.Write(DblLeftColor);
			writer.Write(DblRightColor);
			writer.Write(DblWidth);
			writer.Write(DblLeftWidth);
			writer.Write(DblRightWidth);
			writer.Write(DblLength);
			writer.Write(DblGap);
			foreach (short s in DblRes)
				writer.Write(s);
			writer.Write(DecMode);
			writer.Write(DecLast);
			writer.Write(DecRes);
			writer.Write(FrColor);
			writer.Write(FrWidth);
			writer.Write(FrStyle);
			writer.Write(PrimDSize);
			writer.Write(SecDSize);
			writer.Write(CornerDSize);
			writer.Write(StartDSize);
			writer.Write(EndDSize);
			writer.Write(Reserved);
			OcadSymbolElt.WriteElements(writer, PrimDElts);
			OcadSymbolElt.WriteElements(writer, SecDElts);
			OcadSymbolElt.WriteElements(writer, CornerDElts);
			OcadSymbolElt.WriteElements(writer, StartDElts);
			OcadSymbolElt.WriteElements(writer, EndDElts);

			UpdateSize(writer);
		}

	}

	class OcadLineTextSymbol: OcadSymbol {
		public string FontName;
		public short FontColor;
		public short FontSize;
		public short Weight = 400;
		public bool Italic;
		public byte CharSet;
		public short CharSpace;
		public short WordSpace;
		public short Alignment;
		public byte FrMode;
        public byte FrFlags;
        public string FrName;   // NOTE: Ocad 8 converts framing font to framed by line, always using 1/10 of the char height.
		public short FrColor;
		public short FrSize;
		public short FrWeight = 400;
		public bool FrItalic;
		public short FrOfX;
		public short FrOfY;

		protected override void ReadExtra(BinaryReader reader, int version) {
			FontName = Util.ReadDelphiString(reader, 31);
			FontColor = reader.ReadInt16();
			FontSize = reader.ReadInt16();
			Weight = reader.ReadInt16();
			Italic = reader.ReadByte() != 0 ? true : false;
			CharSet = reader.ReadByte();
			CharSpace = reader.ReadInt16();
			WordSpace = reader.ReadInt16();
			Alignment = reader.ReadInt16();
            FrMode = reader.ReadByte();
            FrFlags = reader.ReadByte();
            FrName = Util.ReadDelphiString(reader, 31);
			FrColor = reader.ReadInt16();
			FrSize = reader.ReadInt16();
			FrWeight = reader.ReadInt16();
			FrItalic = reader.ReadUInt16() != 0 ? true : false;
			FrOfX = reader.ReadInt16();
			FrOfY = reader.ReadInt16();
		}

		public override void Write(BinaryWriter writer, int version) {
			base.Write (writer, version);

			Util.WriteDelphiString(writer, FontName, 31);
			writer.Write(FontColor);
			writer.Write(FontSize);
			writer.Write(Weight);
			writer.Write(Italic ? (byte) 1 : (byte) 0);
			writer.Write(CharSet);
			writer.Write(CharSpace);
			writer.Write(WordSpace);
			writer.Write(Alignment);
			writer.Write(FrMode);
			Util.WriteDelphiString(writer, FrName, 31);
			writer.Write(FrColor);
			writer.Write(FrSize);
			writer.Write(FrWeight);
			writer.Write(FrItalic ? (ushort) 1 : (ushort) 0);
			writer.Write(FrOfX);
			writer.Write(FrOfY);

			UpdateSize(writer);
		}
	}

	class OcadAreaSymbol: OcadSymbol {
        public int BorderSym;
        public ushort AreaFlags;
		public bool FillOn;
        public bool BorderOn;
		public short FillColor;
		public short HatchMode;
		public short HatchColor;
		public short HatchLineWidth;
		public short HatchDist;
		public short HatchAngle1;
		public short HatchAngle2;
		public short HatchRes;
		public short StructMode;
		public short StructWidth;
		public short StructHeight;
		public short StructAngle;
		public short StructRes;
		public short DataSize;
		public OcadSymbolElt[] StructElts;

		protected override void ReadExtra(BinaryReader reader, int version) {
            if (version <= 8) {
                AreaFlags = reader.ReadUInt16();
                FillOn = reader.ReadUInt16() != 0 ? true : false;
            }
            else {
                BorderSym = reader.ReadInt32();
            }
			FillColor = reader.ReadInt16();
			HatchMode = reader.ReadInt16();
			HatchColor = reader.ReadInt16();
			HatchLineWidth = reader.ReadInt16();
			HatchDist = reader.ReadInt16();
			HatchAngle1 = reader.ReadInt16();
			HatchAngle2 = reader.ReadInt16();

            if (version <= 8) {
                HatchRes = reader.ReadInt16();
            }
            else {
                FillOn = reader.ReadByte() != 0 ? true : false;
                BorderOn = reader.ReadByte() != 0 ? true : false;
            }

			StructMode = reader.ReadInt16();
			StructWidth = reader.ReadInt16();
			StructHeight = reader.ReadInt16();
			StructAngle = reader.ReadInt16();
			StructRes = reader.ReadInt16();
			DataSize = reader.ReadInt16();

			StructElts = OcadSymbolElt.ReadElements(reader, DataSize);
		}

		public override void Write(BinaryWriter writer, int version) {
			base.Write (writer, version);

            if (version <= 8) {
                writer.Write(AreaFlags);
                writer.Write(FillOn ? (short) 1 : (short) 0);
            }
            else {
                writer.Write(BorderSym);
            }

			writer.Write(FillColor);
			writer.Write(HatchMode);
			writer.Write(HatchColor);
			writer.Write(HatchLineWidth);
			writer.Write(HatchDist);
			writer.Write(HatchAngle1);
			writer.Write(HatchAngle2);

            if (version <= 8) {
                writer.Write(HatchRes);
            }
            else {
                writer.Write(FillOn ? (byte) 1 : (byte) 0);
                writer.Write(BorderOn ? (byte) 1 : (byte) 0);
            }

			writer.Write(StructMode);
			writer.Write(StructWidth);
			writer.Write(StructHeight);
			writer.Write(StructAngle);
			writer.Write(StructRes);
			writer.Write(DataSize);
			OcadSymbolElt.WriteElements(writer, StructElts);

			UpdateSize(writer);
		}

	}

	class OcadTextSymbol: OcadSymbol {
		public string FontName;
		public short FontColor;
		public short FontSize;
		public short Weight = 400;
		public bool Italic;
		public byte CharSet;
		public short CharSpace;
		public short WordSpace;
		public short Alignment;
		public short LineSpace;
		public short ParaSpace;
		public short IndentFirst;
		public short IndentOther;
		public short nTabs;
		public int[] Tabs = new int[32];
		public bool LBOn;
		public short LBColor;
		public short LBWidth;
		public short LBDist;
		public short Res4;
		public byte FrMode;
        public byte FrFlags;
		public string FrName;   // NOTE: Ocad 8 converts framing font to framed by line, always using 1/10 of the char height.
        public short FrLeft, FrBottom, FrRight, FrTop;
		public short FrColor;
		public short FrSize;
		public short FrWeight = 400;
		public bool FrItalic;
		public short FrOfX;
		public short FrOfY;

		protected override void ReadExtra(BinaryReader reader, int version) {
			FontName = Util.ReadDelphiString(reader, 31);
			FontColor = reader.ReadInt16();
			FontSize = reader.ReadInt16();
			Weight = reader.ReadInt16();
			Italic = reader.ReadByte() != 0 ? true : false;
			CharSet = reader.ReadByte();
			CharSpace = reader.ReadInt16();
			WordSpace = reader.ReadInt16();
			Alignment = reader.ReadInt16();
			LineSpace = reader.ReadInt16();
			ParaSpace = reader.ReadInt16();
			IndentFirst = reader.ReadInt16();
			IndentOther = reader.ReadInt16();
			nTabs = reader.ReadInt16();
			for (int i = 0; i < Tabs.Length; ++i)
				Tabs[i] = reader.ReadInt32();
			LBOn = reader.ReadUInt16() != 0 ? true : false;
			LBColor = reader.ReadInt16();
			LBWidth = reader.ReadInt16();
			LBDist = reader.ReadInt16();
			Res4 = reader.ReadInt16();
            if (version <= 8) {
                FrMode = reader.ReadByte();
                FrFlags = reader.ReadByte();
                FrName = Util.ReadDelphiString(reader, 31);
                FrColor = reader.ReadInt16();
                FrSize = reader.ReadInt16();
                FrWeight = reader.ReadInt16();
                FrItalic = reader.ReadUInt16() != 0 ? true : false;
                FrOfX = reader.ReadInt16();
                FrOfY = reader.ReadInt16();
            }
            else {
                FrMode = reader.ReadByte();
                FrFlags = reader.ReadByte();
                Util.ReadDelphiString(reader, 23);
                FrLeft = reader.ReadInt16();
                FrBottom = reader.ReadInt16();
                FrRight = reader.ReadInt16();
                FrTop = reader.ReadInt16();
                FrColor = reader.ReadInt16();
                FrSize = reader.ReadInt16();
                FrWeight = reader.ReadInt16();
                FrItalic = reader.ReadUInt16() != 0 ? true : false;
                FrOfX = reader.ReadInt16();
                FrOfY = reader.ReadInt16();
            }
		}

		public override void Write(BinaryWriter writer, int version) {
			base.Write (writer, version);

			Util.WriteDelphiString(writer, FontName, 31);
			writer.Write(FontColor);
			writer.Write(FontSize);
			writer.Write(Weight);
			writer.Write(Italic ? (byte) 1 : (byte) 0);
			writer.Write(CharSet);
			writer.Write(CharSpace);
			writer.Write(WordSpace);
			writer.Write(Alignment);
			writer.Write(LineSpace);
			writer.Write(ParaSpace);
			writer.Write(IndentFirst);
			writer.Write(IndentOther);
			writer.Write(nTabs);
			foreach (int i in Tabs)
				writer.Write(i);
			writer.Write(LBOn ? (ushort) 1 : (ushort) 0);
			writer.Write(LBColor);
			writer.Write(LBWidth);
			writer.Write(LBDist);
			writer.Write(Res4);
			writer.Write(FrMode);
            writer.Write(FrFlags);
            if (version >= 9) {
                Util.WriteDelphiString(writer, FrName, 23);
                writer.Write(FrLeft);
                writer.Write(FrBottom);
                writer.Write(FrRight);
                writer.Write(FrTop);
            }
            else {
                Util.WriteDelphiString(writer, FrName, 31);
            }
			writer.Write(FrColor);
			writer.Write(FrSize);
			writer.Write(FrWeight);
			writer.Write(FrItalic ? (ushort) 1 : (ushort) 0);
			writer.Write(FrOfX);
			writer.Write(FrOfY);

			UpdateSize(writer);
		}
	}

	class OcadRectSymbol: OcadSymbol {
		public short LineColor;
		public short LineWidth;
		public short Radius;
		public ushort GridFlags;
		public short CellWidth;
		public short CellHeight;
		public short ResGridLineColor;
		public short ResGridLineWidth;
		public short UnnumCells;
		public string UnnumText;
		public short GridRes2;
		public string ResFontName;
		public short ResFontColor;
		public short ResFontSize;
		public short ResWeight;
		public bool ResItalic;
		public short ResOfsX;
		public short ResOfsY;

		protected override void ReadExtra(BinaryReader reader, int version) {
			LineColor = reader.ReadInt16();
			LineWidth = reader.ReadInt16();
			Radius = reader.ReadInt16();
			GridFlags = reader.ReadUInt16();
			CellWidth = reader.ReadInt16();
			CellHeight= reader.ReadInt16();
			ResGridLineColor = reader.ReadInt16();
			ResGridLineWidth = reader.ReadInt16();
			UnnumCells = reader.ReadInt16();
			UnnumText = Util.ReadDelphiString(reader, 3);
			GridRes2 = reader.ReadInt16();
			ResFontName = Util.ReadDelphiString(reader, 31);
			ResFontColor = reader.ReadInt16();
			ResFontSize = reader.ReadInt16();
			ResWeight = reader.ReadInt16();
			ResItalic = reader.ReadUInt16() != 0 ? true : false;
			ResOfsX = reader.ReadInt16();
			ResOfsY = reader.ReadInt16();
		}
	}

	class OcadSymbolElt {
		public short stType;
		public ushort stFlags;
		public short stColor;
		public short stLineWidth;
		public short stDiameter;
		public short stnPoly;
		public short stRes1;
		public short stRes2;
		public OcadCoord[] stCoords;

		public int Read(BinaryReader reader) {
			stType = reader.ReadInt16();
			stFlags = reader.ReadUInt16();
			stColor = reader.ReadInt16();
			stLineWidth = reader.ReadInt16();
			stDiameter = reader.ReadInt16();
			stnPoly = reader.ReadInt16();
			stRes1 = reader.ReadInt16();
			stRes2 = reader.ReadInt16();
			stCoords = OcadCoord.ReadCoords(reader, stnPoly);

			return 2 + stnPoly;
		}

		static public OcadSymbolElt[] ReadElements(BinaryReader reader, int DataSize) {
            List<OcadSymbolElt> list = new List<OcadSymbolElt>();

			while (DataSize > 0) {
				OcadSymbolElt elt = new OcadSymbolElt();
				DataSize -= elt.Read(reader);
				list.Add(elt);
			}
			Debug.Assert(DataSize == 0);

			return list.ToArray();
		}

		public void Write(BinaryWriter writer) {
			writer.Write(stType);
			writer.Write(stFlags);
			writer.Write(stColor);
			writer.Write(stLineWidth);
			writer.Write(stDiameter);
			writer.Write(stnPoly);
			writer.Write(stRes1);
			writer.Write(stRes2);
			OcadCoord.WriteCoords(writer, stCoords);
		}

		public static void WriteElements(BinaryWriter writer, OcadSymbolElt[] elts) {
			if (elts != null) {
				foreach (OcadSymbolElt elt in elts)
					elt.Write(writer);
			}
		}
	}

	struct OcadCoord {
		public int x, y;

        public OcadCoord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

		public override string ToString() {
			double xCoord = (x >> 8) / 100.0;
			double yCoord = (y >> 8) / 100.0;
			int xFlags = (x & 0xFF);
			int yFlags = (y & 0xFF);

			string flags = "";
			if ((xFlags & 1) != 0)  flags += ", Curve1";
			if ((xFlags & 2) != 0)  flags += ", Curve2";
			if ((xFlags & 4) != 0)  flags += ", NoLeft";
			if ((yFlags & 1) != 0)  flags += ", Corner";
			if ((yFlags & 2) != 0)  flags += ", Hole";
			if ((yFlags & 4) != 0)  flags += ", NoRight";
			if ((yFlags & 8) != 0)  flags += ", DashPt";

			return string.Format("({0:F2},{1:F2}{2})", xCoord, yCoord, flags);
		}

		public void Read(BinaryReader reader) {
			x = reader.ReadInt32();
			y = reader.ReadInt32();
		}

		public static OcadCoord[] ReadCoords(BinaryReader reader, int nCoord) {
			OcadCoord[] coords = new OcadCoord[nCoord];
			for (int i = 0; i < nCoord; ++i) {
				coords[i].Read(reader);
			}
			return coords;
		}

		public void Write(BinaryWriter writer) {
			writer.Write(x);
			writer.Write(y);
		}

		public static void WriteCoords(BinaryWriter writer, OcadCoord[] coords) {
			foreach (OcadCoord coord in coords)
				coord.Write(writer);
		}
	}

	struct OcadIndex {
		public OcadCoord LowerLeft, UpperRight;
		public int Pos;
		public int Len;  // Note: the meaning of this changes in OCAD 8
		public int Sym;
        public byte ObjType;
        public byte Rex;
        public byte Status;
        public byte ViewType;
        public short Color;
        public short ImpLayer;

		public void Read(BinaryReader reader, int version) {
			LowerLeft = new OcadCoord();
			LowerLeft.Read(reader);
			UpperRight = new OcadCoord();
			UpperRight.Read(reader);
            if (version <= 8) {
                Pos = reader.ReadInt32();
                Len = reader.ReadInt16();
                Sym = reader.ReadInt16();
            }
            else {
                Pos = reader.ReadInt32();
                Len = reader.ReadInt32();
                Sym = reader.ReadInt32();
                ObjType = reader.ReadByte();
                Rex = reader.ReadByte();
                Status = reader.ReadByte();
                ViewType = reader.ReadByte();
                Color = reader.ReadInt16();
                reader.ReadInt16();
                ImpLayer = reader.ReadInt16();
                reader.ReadInt16();
            }
		}

		public void Write(BinaryWriter writer, int version) {
			LowerLeft.Write(writer);
			UpperRight.Write(writer);

            if (version <= 8) {
                writer.Write(Pos);
                writer.Write((short) Len);
                writer.Write((short) Sym);
            }
            else {
                writer.Write(Pos);
                writer.Write(Len);
                writer.Write(Sym);
                writer.Write(ObjType);
                writer.Write(Rex);
                writer.Write(Status);
                writer.Write(ViewType);
                writer.Write(Color);
                writer.Write((short) 0);
                writer.Write(ImpLayer);
                writer.Write((short) 0);
            }
		}
	}

	class OcadIndexBlock {
		public int NextBlock;
		public OcadIndex[] IndexArr;

		public OcadIndexBlock() {
			IndexArr = new OcadIndex[256];
		}

		public void Write(BinaryWriter writer, int version) {
			writer.Write(NextBlock);
			Debug.Assert(IndexArr.Length == 256);
			foreach (OcadIndex pos in IndexArr)
				pos.Write(writer, version);
		}
	}

	class OcadIndexBlocks {
		public OcadIndex[] indexes;

		public void Read(BinaryReader reader, int firstBlock, int version) {
            List<OcadIndex> indexList = new List<OcadIndex>();
			int nextBlock = firstBlock;

			while (nextBlock != 0) {
				reader.BaseStream.Seek(nextBlock, SeekOrigin.Begin);
				nextBlock = reader.ReadInt32();
				for (int i = 0; i < 256; ++i) {
					OcadIndex index = new OcadIndex();
					index.Read(reader, version);
					if (index.Sym != 0)
						indexList.Add(index);
				}
			}

			indexes = indexList.ToArray();
		}
	}

	class OcadObject {
		public int Sym;
		public byte Otp;
		public byte Unicode;
		public int nItem;
		public short nText;
		public short Ang;
        public uint Col;
        public short LineWidth;
        public short DiamFlags;
		public OcadCoord[] coords;
		public string text;

		public void Read(BinaryReader reader, int version) {
            if (version <= 8) {
                Sym = reader.ReadInt16();
                Otp = reader.ReadByte();
                Unicode = reader.ReadByte();
                nItem = reader.ReadInt16();
                nText = reader.ReadInt16();
                Ang = reader.ReadInt16();
                reader.ReadInt16();
                reader.ReadInt32();
                Util.ReadDelphiString(reader, 15);
                coords = OcadCoord.ReadCoords(reader, nItem);
            }
            else {
                Unicode = 1;
                Sym = reader.ReadInt32();
                Otp = reader.ReadByte();
                reader.ReadByte();  // Res0
                Ang = reader.ReadInt16();
                nItem = reader.ReadInt32();
                nText = reader.ReadInt16();
                reader.ReadInt16();
                Col = reader.ReadUInt32();
                LineWidth = reader.ReadInt16();
                DiamFlags = reader.ReadInt16();
                reader.ReadInt64();
                reader.ReadInt64();
                coords = OcadCoord.ReadCoords(reader, nItem);
            }

			if (nText > 0) {
				char[] chars;
				
				if (Unicode != 0) {
					chars = new char[nText * 4];
					for (int i = 0; i < nText * 4; ++i)
						chars[i] = (char) reader.ReadUInt16();
				}
				else {
					chars = reader.ReadChars(nText * 8);
				}

				int len;
				for (len = 0; len < chars.Length; ++len) {
					if (chars[len] == 0)
						break;
				}
				text = new string(chars, 0, len);
			}
			else
				text = null;
		}

		public void Write(BinaryWriter writer, int version) {
            if (version <= 8) {
                writer.Write((short) Sym);
                writer.Write(Otp);
                writer.Write(Unicode);
                writer.Write((short) nItem);
                writer.Write(nText);
                writer.Write(Ang);
                writer.Write((short) 0);
                writer.Write((int) ((version >= 8) ? 0 : 1));
                Util.WriteDelphiString(writer, "", 15);
            }
            else {
                writer.Write(Sym);
                writer.Write((byte)Otp);
                writer.Write((byte)0);
                writer.Write(Ang);
                writer.Write((int) nItem);
                writer.Write(nText);
                writer.Write((short) 0);
                writer.Write(Col);
                writer.Write(LineWidth);
                writer.Write(DiamFlags);
                writer.Write((long) 0);
                writer.Write((long) 0);
            }

            OcadCoord.WriteCoords(writer, coords);

			if (nText > 0) {
                if (version > 8 || Unicode != 0) {
                    char[] chars = new char[nText * 4];
                    for (int i = 0; i < text.Length; ++i)
                        writer.Write((ushort) text[i]);
                    for (int i = text.Length; i < nText * 4; ++i)
                        writer.Write((ushort) 0);
                }
                else {
                    char[] chars = new char[nText * 8];
                    if (text != null)
                        text.CopyTo(0, chars, 0, text.Length);
                    writer.Write(chars);
                }
			}
		}
	}

	struct OcadStIndex {
		public int Pos;
		public int Len;
		public int StType;
		public int ObjIndex;

		public void Read(BinaryReader reader) {
			Pos = reader.ReadInt32();
			Len = reader.ReadInt32();
			StType = reader.ReadInt32();
			ObjIndex = reader.ReadInt32();
		}

		public void Write(BinaryWriter writer) {
			writer.Write(Pos);
			writer.Write(Len);
			writer.Write(StType);
			writer.Write(ObjIndex);
		}
	}

	class OcadStIndexBlock {
		public int NextBlock;
		public OcadStIndex[] Table;

		public OcadStIndexBlock() {
			Table = new OcadStIndex[256];
		}

        public void Write(BinaryWriter writer)
        {
            writer.Write(NextBlock);
            Debug.Assert(Table.Length == 256);
            foreach (OcadStIndex stIndex in Table)
                stIndex.Write(writer);
        }
	}

	class OcadStIndexBlocks {
		public OcadStIndex[] indexes;

		public void Read(BinaryReader reader, int firstBlock) {
            List<OcadStIndex> indexList = new List<OcadStIndex>();
			int nextBlock = firstBlock;

			while (nextBlock != 0) {
				reader.BaseStream.Seek(nextBlock, SeekOrigin.Begin);
				nextBlock = reader.ReadInt32();
				for (int i = 0; i < 256; ++i) {
					OcadStIndex stindex = new OcadStIndex();
					stindex.Read(reader);
					if (stindex.StType != 0)
						indexList.Add(stindex);
				}
			}

			indexes = indexList.ToArray();
		}
	}

	class OcadParamString {
		public int StType;
		public int ObjIndex;
		public string firstField;
		public char[] codes;
		public string[] values;

        // Calculate character count to store, not counting final NUL.
        public int CharacterCount()
        {
            int charCount = 0;

            if (firstField != null)
                charCount += firstField.Length;

            for (int i = 0; i < codes.Length; ++i) {
                charCount += 2;  // tab and code;
                if (values[i] != null)
                    charCount += values[i].Length;
            }

            return charCount;
        }

		public void Read(BinaryReader reader, OcadStIndex index) {
			StType = index.StType;
			ObjIndex = index.ObjIndex;
			reader.BaseStream.Seek(index.Pos, SeekOrigin.Begin);
			char[] chars = reader.ReadChars(index.Len);

			// Count the number of values (number of tabs).
			int nValues = 0;
			int i;
			for (i = 0; i < chars.Length && chars[i] != 0; ++i) {
				if (chars[i] == '\t')
					++nValues;
			}

            codes = new char[nValues];
			values = new string[nValues];

			for (i = 0; i < chars.Length && chars[i] != 0 && chars[i] != '\t'; ++i)
				;
			firstField = new string(chars, 0, i);

			++i;
			int iValue = 0;
			while (i < chars.Length && chars[i] != 0) {
				int start = i;

				while (i < chars.Length && chars[i] != 0 && chars[i] != '\t')
					++i;

				codes[iValue] = chars[start];
				if (i > start)
					values[iValue] = new string(chars, start + 1, i - start - 1);

                ++iValue;

                if (i >= chars.Length || chars[i] == 0)
                    break;

				++i;
			}

            Debug.Assert(iValue == nValues);
		}

        public void Write(BinaryWriter writer, int totalChars)
        {
            char[] chars = new char[totalChars];
            int i = 0;

            if (firstField != null) {
                firstField.CopyTo(0, chars, i, firstField.Length);
                i += firstField.Length;
            }

            for (int codeIndex = 0; codeIndex < codes.Length; ++codeIndex) {
                chars[i++] = '\t';
                chars[i++] = codes[codeIndex];
                values[codeIndex].CopyTo(0, chars, i, values[codeIndex].Length);
                i += values[codeIndex].Length;
            }

            writer.Write(chars);
        }
	}
}
