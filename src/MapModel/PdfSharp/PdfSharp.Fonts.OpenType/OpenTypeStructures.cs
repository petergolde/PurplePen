#region PDFsharp - A .NET library for processing PDF
//
// Authors:
//   Stefan Lange (mailto:Stefan.Lange@pdfsharp.com)
//
// Copyright (c) 2005-2009 empira Software GmbH, Cologne (Germany)
//
// http://www.pdfsharp.com
// http://sourceforge.net/projects/pdfsharp
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion

#define VERBOSE_

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using PdfSharp.Drawing;
using PdfSharp.Internal;

using Fixed = System.Int32;
using FWord = System.Int16;
using UFWord = System.UInt16;

namespace PdfSharp.Fonts.OpenType
{
  internal enum PlatformId
  {
    Apple, Mac, Iso, Win
  }

  /// <summary>
  /// Only Symbol and Unicode is used by PDFsharp.
  /// </summary>
  internal enum WinEncodingId
  {
    Symbol, Unicode
  }

  /// <summary>
  /// CMap format 4: Segment mapping to delta values.
  /// The Windows standard format.
  /// </summary>
  internal class CMap4 : OpenTypeFontTable
  {
    public WinEncodingId encodingId; // Windows encoding ID.
    public ushort format; // Format number is set to 4.
    public ushort length; // This is the length in bytes of the subtable. 
    public ushort language; // This field must be set to zero for all cmap subtables whose platform IDs are other than Macintosh (platform ID 1). 
    public ushort segCountX2; // 2 x segCount.
    public ushort searchRange; // 2 x (2**floor(log2(segCount)))
    public ushort entrySelector; // log2(searchRange/2)
    public ushort rangeShift;
    public ushort[] endCount; // [segCount] / End characterCode for each segment, last=0xFFFF.
    public ushort[] startCount; // [segCount] / Start character code for each segment.
    public short[] idDelta; // [segCount] / Delta for all character codes in segment.
    public ushort[] idRangeOffs; // [segCount] / Offsets into glyphIdArray or 0
    public int glyphCount; // = (length - (16 + 4 * 2 * segCount)) / 2;
    public ushort[] glyphIdArray;     // Glyph index array (arbitrary length)

    public CMap4(FontData fontData, WinEncodingId encodingId)
      : base(fontData, "----")
    {
      this.encodingId = encodingId;
      Read();
    }

    internal void Read()
    {
      try
      {
        // m_EncodingID = encID;
        this.format = this.fontData.ReadUShort();
        Debug.Assert(this.format == 4, "Only format 4 expected.");
        this.length = this.fontData.ReadUShort();
        this.language = this.fontData.ReadUShort();  // Always null in Windows
        this.segCountX2 = this.fontData.ReadUShort();
        this.searchRange = this.fontData.ReadUShort();
        this.entrySelector = this.fontData.ReadUShort();
        this.rangeShift = this.fontData.ReadUShort();

        int segCount = this.segCountX2 / 2;
        this.glyphCount = (this.length - (16 + 8 * segCount)) / 2;

        //ASSERT_CONDITION(0 <= m_NumGlyphIds && m_NumGlyphIds < m_Length, "Invalid Index");

        this.endCount = new ushort[segCount];
        this.startCount = new ushort[segCount];
        this.idDelta = new short[segCount];
        this.idRangeOffs = new ushort[segCount];

        this.glyphIdArray = new ushort[this.glyphCount];

        for (int idx = 0; idx < segCount; idx++)
          this.endCount[idx] = this.fontData.ReadUShort();

        //ASSERT_CONDITION(m_EndCount[segs - 1] == 0xFFFF, "Out of Index");

        // Read reserved pad.
        this.fontData.ReadUShort();

        for (int idx = 0; idx < segCount; idx++)
          this.startCount[idx] = this.fontData.ReadUShort();

        for (int idx = 0; idx < segCount; idx++)
          this.idDelta[idx] = this.fontData.ReadShort();

        for (int idx = 0; idx < segCount; idx++)
          this.idRangeOffs[idx] = this.fontData.ReadUShort();

        for (int idx = 0; idx < this.glyphCount; idx++)
          this.glyphIdArray[idx] = this.fontData.ReadUShort();
      }
      catch (Exception ex)
      {
        throw new PdfSharpException(PSSR.ErrorReadingFontData, ex);
      }
    }
  }

  /// <summary>
  /// This table defines the mapping of character codes to the glyph index values used in the font.
  /// It may contain more than one subtable, in order to support more than one character encoding scheme.
  /// </summary>
  internal class CMapTable : OpenTypeFontTable
  {
    public const string Tag = TableTagNames.CMap;

    public ushort version;
    public ushort numTables;

    /// <summary>
    /// Is true for symbol font encoding.
    /// </summary>
    public bool symbol;

    public CMap4 cmap4;

    /// <summary>
    /// Initializes a new instance of the <see cref="CMapTable"/> class.
    /// </summary>
    public CMapTable(FontData fontData)
      : base(fontData, Tag)
    {
      Read();
    }

    internal void Read()
    {
      try
      {
        int tableOffset = this.fontData.Position;

        this.version = this.fontData.ReadUShort();
        this.numTables = this.fontData.ReadUShort();

        bool success = false;
        for (int idx = 0; idx < this.numTables; idx++)
        {
          PlatformId platformId = (PlatformId)this.fontData.ReadUShort();
          WinEncodingId encodingId = (WinEncodingId)this.fontData.ReadUShort();
          int offset = this.fontData.ReadLong();

          int currentPosition = this.fontData.Position;

          // Just read Windows stuff
          if (platformId == PlatformId.Win && (encodingId == WinEncodingId.Symbol || encodingId == WinEncodingId.Unicode))
          {
            this.symbol = encodingId == WinEncodingId.Symbol;

            this.fontData.Position = tableOffset + offset;
            this.cmap4 = new CMap4(this.fontData, encodingId);
            this.fontData.Position = currentPosition;
            // We have found what we are looking for, so break.
            success = true;
            break;
          }
        }
        if (!success)
          throw new InvalidOperationException("Font has no usable platform or encoding ID. It cannot be used with PDFsharp.");
      }
      catch (Exception ex)
      {
        throw new PdfSharpException(PSSR.ErrorReadingFontData, ex);
      }
    }
  }

  /// <summary>
  /// This table gives global information about the font. The bounding box values should be computed using 
  /// only glyphs that have contours. Glyphs with no contours should be ignored for the purposes of these calculations.
  /// </summary>
  internal class FontHeaderTable : OpenTypeFontTable
  {
    public const string Tag = TableTagNames.Head;

    public Fixed version; // 0x00010000 for version 1.0.
    public Fixed fontRevision;
    public uint checkSumAdjustment;
    public uint magicNumber; // Set to 0x5F0F3CF5
    public ushort flags;
    public ushort unitsPerEm; // Valid range is from 16 to 16384. This value should be a power of 2 for fonts that have TrueType outlines.
    public long created;
    public long modified;
    public short xMin, yMin; // For all glyph bounding boxes.
    public short xMax, yMax; // For all glyph bounding boxes.
    public ushort macStyle;
    public ushort lowestRecPPEM;
    public short fontDirectionHint;
    public short indexToLocFormat; // 0 for short offsets, 1 for long
    public short glyphDataFormat; // 0 for current format

    public FontHeaderTable(FontData fontData)
      : base(fontData, Tag)
    {
      Read();
    }

    public void Read()
    {
      try
      {
        this.version = this.fontData.ReadFixed();
        this.fontRevision = this.fontData.ReadFixed();
        this.checkSumAdjustment = this.fontData.ReadULong();
        this.magicNumber = this.fontData.ReadULong();
        this.flags = this.fontData.ReadUShort();
        this.unitsPerEm = this.fontData.ReadUShort();
        this.created = this.fontData.ReadLongDate();
        this.modified = this.fontData.ReadLongDate();
        this.xMin = this.fontData.ReadShort();
        this.yMin = this.fontData.ReadShort();
        this.xMax = this.fontData.ReadShort();
        this.yMax = this.fontData.ReadShort();
        this.macStyle = this.fontData.ReadUShort();
        this.lowestRecPPEM = this.fontData.ReadUShort();
        this.fontDirectionHint = this.fontData.ReadShort();
        this.indexToLocFormat = this.fontData.ReadShort();
        this.glyphDataFormat = this.fontData.ReadShort();
      }
      catch (Exception ex)
      {
        throw new PdfSharpException(PSSR.ErrorReadingFontData, ex);
      }
    }
  }

  /// <summary>
  /// This table contains information for horizontal layout. The values in the minRightSidebearing, 
  /// minLeftSideBearing and xMaxExtent should be computed using only glyphs that have contours.
  /// Glyphs with no contours should be ignored for the purposes of these calculations.
  /// All reserved areas must be set to 0. 
  /// </summary>
  internal class HorizontalHeaderTable : OpenTypeFontTable
  {
    public const string Tag = TableTagNames.HHea;

    public Fixed version; // 0x00010000 for version 1.0.
    public FWord ascender; // Typographic ascent. (Distance from baseline of highest ascender) 
    public FWord descender; // Typographic descent. (Distance from baseline of lowest descender) 
    public FWord lineGap; // Typographic line gap. Negative LineGap values are treated as zero in Windows 3.1, System 6, and System 7.
    public UFWord advanceWidthMax;
    public FWord minLeftSideBearing;
    public FWord minRightSideBearing;
    public FWord xMaxExtent;
    public short caretSlopeRise;
    public short caretSlopeRun;
    public short reserved1;
    public short reserved2;
    public short reserved3;
    public short reserved4;
    public short reserved5;
    public short metricDataFormat;
    public ushort numberOfHMetrics;

    public HorizontalHeaderTable(FontData fontData)
      : base(fontData, Tag)
    {
      Read();
    }

    public void Read()
    {
      try
      {
        this.version = this.fontData.ReadFixed();
        this.ascender = this.fontData.ReadFWord();
        this.descender = this.fontData.ReadFWord();
        this.lineGap = this.fontData.ReadFWord();
        this.advanceWidthMax = this.fontData.ReadUFWord();
        this.minLeftSideBearing = this.fontData.ReadFWord();
        this.minRightSideBearing = this.fontData.ReadFWord();
        this.xMaxExtent = this.fontData.ReadFWord();
        this.caretSlopeRise = this.fontData.ReadShort();
        this.caretSlopeRun = this.fontData.ReadShort();
        this.reserved1 = this.fontData.ReadShort();
        this.reserved2 = this.fontData.ReadShort();
        this.reserved3 = this.fontData.ReadShort();
        this.reserved4 = this.fontData.ReadShort();
        this.reserved5 = this.fontData.ReadShort();
        this.metricDataFormat = this.fontData.ReadShort();
        this.numberOfHMetrics = this.fontData.ReadUShort();
      }
      catch (Exception ex)
      {
        throw new PdfSharpException(PSSR.ErrorReadingFontData, ex);
      }
    }
  }

  internal class HorizontalMetrics : OpenTypeFontTable
  {
    public const string Tag = "----";

    public ushort advanceWidth;
    public short lsb;

    public HorizontalMetrics(FontData fontData)
      : base(fontData, Tag)
    {
      Read();
    }

    public void Read()
    {
      try
      {
        this.advanceWidth = this.fontData.ReadUFWord();
        this.lsb = this.fontData.ReadFWord();
      }
      catch (Exception ex)
      {
        throw new PdfSharpException(PSSR.ErrorReadingFontData, ex);
      }
    }
  }

  /// <summary>
  /// The type longHorMetric is defined as an array where each element has two parts:
  /// the advance width, which is of type USHORT, and the left side bearing, which is of type SHORT.
  /// These fields are in font design units.
  /// </summary>
  internal class HorizontalMetricsTable : OpenTypeFontTable
  {
    public const string Tag = TableTagNames.HMtx;

    public HorizontalMetrics[] metrics;
    public FWord[] leftSideBearing;

    public HorizontalMetricsTable(FontData fontData)
      : base(fontData, Tag)
    {
      Read();
    }

    public void Read()
    {
      try
      {
        HorizontalHeaderTable hhea = this.fontData.hhea;
        MaximumProfileTable maxp = this.fontData.maxp;
        if (hhea != null && maxp != null)
        {
          int numMetrics = hhea.numberOfHMetrics; //->NumberOfHMetrics();
          int numLsbs = maxp.numGlyphs - numMetrics;

          Debug.Assert(numMetrics != 0);
          Debug.Assert(numLsbs >= 0);

          this.metrics = new HorizontalMetrics[numMetrics];
          for (int idx = 0; idx < numMetrics; idx++)
            this.metrics[idx] = new HorizontalMetrics(this.fontData);

          if (numLsbs > 0)
          {
            this.leftSideBearing = new FWord[numLsbs];
            for (int idx = 0; idx < numLsbs; idx++)
              this.leftSideBearing[idx] = this.fontData.ReadFWord();
          }
        }
      }
      catch (Exception ex)
      {
        throw new PdfSharpException(PSSR.ErrorReadingFontData, ex);
      }
    }
  }

  // UNDONE
  internal class VerticalHeaderTable : OpenTypeFontTable
  {
    public const string Tag = TableTagNames.VHea;

    // code comes from HorizontalHeaderTable
    public Fixed version; // 0x00010000 for version 1.0.
    public FWord ascender; // Typographic ascent. (Distance from baseline of highest ascender) 
    public FWord descender; // Typographic descent. (Distance from baseline of lowest descender) 
    public FWord lineGap; // Typographic line gap. Negative LineGap values are treated as zero in Windows 3.1, System 6, and System 7.
    public UFWord advanceWidthMax;
    public FWord minLeftSideBearing;
    public FWord minRightSideBearing;
    public FWord xMaxExtent;
    public short caretSlopeRise;
    public short caretSlopeRun;
    public short reserved1;
    public short reserved2;
    public short reserved3;
    public short reserved4;
    public short reserved5;
    public short metricDataFormat;
    public ushort numberOfHMetrics;

    public VerticalHeaderTable(FontData fontData)
      : base(fontData, Tag)
    {
      Read();
    }

    public void Read()
    {
      try
      {
        this.version = this.fontData.ReadFixed();
        this.ascender = this.fontData.ReadFWord();
        this.descender = this.fontData.ReadFWord();
        this.lineGap = this.fontData.ReadFWord();
        this.advanceWidthMax = this.fontData.ReadUFWord();
        this.minLeftSideBearing = this.fontData.ReadFWord();
        this.minRightSideBearing = this.fontData.ReadFWord();
        this.xMaxExtent = this.fontData.ReadFWord();
        this.caretSlopeRise = this.fontData.ReadShort();
        this.caretSlopeRun = this.fontData.ReadShort();
        this.reserved1 = this.fontData.ReadShort();
        this.reserved2 = this.fontData.ReadShort();
        this.reserved3 = this.fontData.ReadShort();
        this.reserved4 = this.fontData.ReadShort();
        this.reserved5 = this.fontData.ReadShort();
        this.metricDataFormat = this.fontData.ReadShort();
        this.numberOfHMetrics = this.fontData.ReadUShort();
      }
      catch (Exception ex)
      {
        throw new PdfSharpException(PSSR.ErrorReadingFontData, ex);
      }
    }
  }

  internal class VerticalMetrics : OpenTypeFontTable
  {
    public const string Tag = "----";

    // code comes from HorizontalMetrics
    public ushort advanceWidth;
    public short lsb;

    public VerticalMetrics(FontData fontData)
      : base(fontData, Tag)
    {
      Read();
    }

    public void Read()
    {
      try
      {
        this.advanceWidth = this.fontData.ReadUFWord();
        this.lsb = this.fontData.ReadFWord();
      }
      catch (Exception ex)
      {
        throw new PdfSharpException(PSSR.ErrorReadingFontData, ex);
      }
    }
  }

  /// <summary>
  /// The vertical metrics table allows you to specify the vertical spacing for each glyph in a
  /// vertical font. This table consists of either one or two arrays that contain metric
  /// information (the advance heights and top sidebearings) for the vertical layout of each
  /// of the glyphs in the font.
  /// </summary>
  internal class VerticalMetricsTable : OpenTypeFontTable
  {
    // UNDONE
    public const string Tag = TableTagNames.VMtx;

    // code comes from HorizontalMetricsTable
    public HorizontalMetrics[] metrics;
    public FWord[] leftSideBearing;

    public VerticalMetricsTable(FontData fontData)
      : base(fontData, Tag)
    {
      Read();
      throw new NotImplementedException("VerticalMetricsTable");
    }

    public void Read()
    {
      try
      {
        HorizontalHeaderTable hhea = this.fontData.hhea;
        MaximumProfileTable maxp = this.fontData.maxp;
        if (hhea != null && maxp != null)
        {
          int numMetrics = hhea.numberOfHMetrics; //->NumberOfHMetrics();
          int numLsbs = maxp.numGlyphs - numMetrics;

          Debug.Assert(numMetrics != 0);
          Debug.Assert(numLsbs >= 0);

          this.metrics = new HorizontalMetrics[numMetrics];
          for (int idx = 0; idx < numMetrics; idx++)
            this.metrics[idx] = new HorizontalMetrics(this.fontData);

          if (numLsbs > 0)
          {
            this.leftSideBearing = new FWord[numLsbs];
            for (int idx = 0; idx < numLsbs; idx++)
              this.leftSideBearing[idx] = this.fontData.ReadFWord();
          }
        }
      }
      catch (Exception ex)
      {
        throw new PdfSharpException(PSSR.ErrorReadingFontData, ex);
      }
    }
  }

  /// <summary>
  /// This table establishes the memory requirements for this font.
  /// Fonts with CFF data must use Version 0.5 of this table, specifying only the numGlyphs field.
  /// Fonts with TrueType outlines must use Version 1.0 of this table, where all data is required.
  /// Both formats of OpenType require a 'maxp' table because a number of applications call the 
  /// Windows GetFontData() API on the 'maxp' table to determine the number of glyphs in the font.
  /// </summary>
  internal class MaximumProfileTable : OpenTypeFontTable
  {
    public const string Tag = TableTagNames.MaxP;

    public Fixed version;
    public ushort numGlyphs;
    public ushort maxPoints;
    public ushort maxContours;
    public ushort maxCompositePoints;
    public ushort maxCompositeContours;
    public ushort maxZones;
    public ushort maxTwilightPoints;
    public ushort maxStorage;
    public ushort maxFunctionDefs;
    public ushort maxInstructionDefs;
    public ushort maxStackElements;
    public ushort maxSizeOfInstructions;
    public ushort maxComponentElements;
    public ushort maxComponentDepth;

    public MaximumProfileTable(FontData fontData)
      : base(fontData, Tag)
    {
      Read();
    }

    public void Read()
    {
      try
      {
        this.version = this.fontData.ReadFixed();
        this.numGlyphs = this.fontData.ReadUShort();
        this.maxPoints = this.fontData.ReadUShort();
        this.maxContours = this.fontData.ReadUShort();
        this.maxCompositePoints = this.fontData.ReadUShort();
        this.maxCompositeContours = this.fontData.ReadUShort();
        this.maxZones = this.fontData.ReadUShort();
        this.maxTwilightPoints = this.fontData.ReadUShort();
        this.maxStorage = this.fontData.ReadUShort();
        this.maxFunctionDefs = this.fontData.ReadUShort();
        this.maxInstructionDefs = this.fontData.ReadUShort();
        this.maxStackElements = this.fontData.ReadUShort();
        this.maxSizeOfInstructions = this.fontData.ReadUShort();
        this.maxComponentElements = this.fontData.ReadUShort();
        this.maxComponentDepth = this.fontData.ReadUShort();
      }
      catch (Exception ex)
      {
        throw new PdfSharpException(PSSR.ErrorReadingFontData, ex);
      }
    }
  }

  /// <summary>
  /// The naming table allows multilingual strings to be associated with the OpenTypeTM font file.
  /// These strings can represent copyright notices, font names, family names, style names, and so on.
  /// To keep this table short, the font manufacturer may wish to make a limited set of entries in some
  /// small set of languages; later, the font can be "localized" and the strings translated or added.
  /// Other parts of the OpenType font file that require these strings can then refer to them simply by
  /// their index number. Clients that need a particular string can look it up by its platform ID, character
  /// encoding ID, language ID and name ID. Note that some platforms may require single byte character
  /// strings, while others may require double byte strings. 
  ///
  /// For historical reasons, some applications which install fonts perform version control using Macintosh
  /// platform (platform ID 1) strings from the 'name' table. Because of this, we strongly recommend that
  /// the 'name' table of all fonts include Macintosh platform strings and that the syntax of the version
  /// number (name id 5) follows the guidelines given in this document.
  /// </summary>
  internal class NameTable : OpenTypeFontTable
  {
    public const string Tag = TableTagNames.Name;

    public string Name = String.Empty;
    public string Style = String.Empty;
    public string PostscriptName = String.Empty;

    public ushort format;
    public ushort count;
    public ushort stringOffset;

    byte[] bytes;

    public NameTable(FontData fontData)
      : base(fontData, Tag)
    {
      Read();
    }

    public void Read()
    {
      try
      {
#if DEBUG
        this.fontData.Position = DirectoryEntry.Offset;
#endif
        this.bytes = new byte[DirectoryEntry.PaddedLength];
        Buffer.BlockCopy(this.fontData.Data, DirectoryEntry.Offset, bytes, 0, DirectoryEntry.Length);

        this.format = this.fontData.ReadUShort();
        this.count = this.fontData.ReadUShort();
        this.stringOffset = this.fontData.ReadUShort();

        for (int idx = 0; idx < this.count; idx++)
        {
          NameRecord nrec = ReadNameRecord();
          byte[] value = new byte[nrec.length];
          Buffer.BlockCopy(this.fontData.Data, DirectoryEntry.Offset + this.stringOffset + nrec.offset, value, 0, nrec.length);

          //Debug.WriteLine(nrec.platformID.ToString());

          // Read font name and style
          if (nrec.platformID == 0 || nrec.platformID == 3)
          {
            if (nrec.nameID == 1 && nrec.languageID == 0x0409)
            {
              if (String.IsNullOrEmpty(Name))
                Name = Encoding.BigEndianUnicode.GetString(value, 0, value.Length);
            }
            if (nrec.nameID == 2 && nrec.languageID == 0x0409)
            {
              if (String.IsNullOrEmpty(Style))
                Style = Encoding.BigEndianUnicode.GetString(value, 0, value.Length);
            }
            if (nrec.nameID == 6 && nrec.languageID == 0x409) {
              if (String.IsNullOrEmpty(PostscriptName))
                PostscriptName = Encoding.BigEndianUnicode.GetString(value, 0, value.Length);
            }
          }
          //string s1 = Encoding.Default.GetString(name);
          //string s2 = Encoding.BigEndianUnicode.GetString(name);
          //Debug.WriteLine(s1);
          //Debug.WriteLine(s2);
        }
        Debug.Assert(!String.IsNullOrEmpty(Name));
      }
      catch (Exception ex)
      {
        throw new PdfSharpException(PSSR.ErrorReadingFontData, ex);
      }
    }

    NameRecord ReadNameRecord()
    {
      NameRecord nrec = new NameRecord();
      nrec.platformID = this.fontData.ReadUShort();
      nrec.encodingID = this.fontData.ReadUShort();
      nrec.languageID = this.fontData.ReadUShort();
      nrec.nameID = this.fontData.ReadUShort();
      nrec.length = this.fontData.ReadUShort();
      nrec.offset = this.fontData.ReadUShort();
      return nrec;
    }

    class NameRecord
    {
      public ushort platformID;
      public ushort encodingID;
      public ushort languageID;
      public ushort nameID;
      public ushort length;
      public ushort offset;
    }
  }

  /// <summary>
  /// The OS/2 table consists of a set of metrics that are required in OpenType fonts. 
  /// </summary>
  internal class OS2Table : OpenTypeFontTable
  {
    public const string Tag = TableTagNames.OS2;

    [Flags]
    public enum FontSelectionFlags : ushort
    {
      Italic = 1 << 0,
      Bold = 1 << 5,
      Regular = 1 << 6,
    }

    public ushort version;
    public short xAvgCharWidth;
    public ushort usWeightClass;
    public ushort usWidthClass;
    public ushort fsType;
    public short ySubscriptXSize;
    public short ySubscriptYSize;
    public short ySubscriptXOffset;
    public short ySubscriptYOffset;
    public short ySuperscriptXSize;
    public short ySuperscriptYSize;
    public short ySuperscriptXOffset;
    public short ySuperscriptYOffset;
    public short yStrikeoutSize;
    public short yStrikeoutPosition;
    public short sFamilyClass;
    public byte[] panose; // = new byte[10];
    public uint ulUnicodeRange1; // Bits 0-31
    public uint ulUnicodeRange2; // Bits 32-63
    public uint ulUnicodeRange3; // Bits 64-95
    public uint ulUnicodeRange4; // Bits 96-127
    public string achVendID; // = "";
    public ushort fsSelection;
    public ushort usFirstCharIndex;
    public ushort usLastCharIndex;
    public short sTypoAscender;
    public short sTypoDescender;
    public short sTypoLineGap;
    public ushort usWinAscent;
    public ushort usWinDescent;
    // version >= 1
    public uint ulCodePageRange1; // Bits 0-31
    public uint ulCodePageRange2; // Bits 32-63
    // version >= 2
    public short sxHeight;
    public short sCapHeight;
    public ushort usDefaultChar;
    public ushort usBreakChar;
    public ushort usMaxContext;

    public OS2Table(FontData fontData)
      : base(fontData, Tag)
    {
      Read();
    }

    public void Read()
    {
      try
      {
        this.version = this.fontData.ReadUShort();
        this.xAvgCharWidth = this.fontData.ReadShort();
        this.usWeightClass = this.fontData.ReadUShort();
        this.usWidthClass = this.fontData.ReadUShort();
        this.fsType = this.fontData.ReadUShort();
        this.ySubscriptXSize = this.fontData.ReadShort();
        this.ySubscriptYSize = this.fontData.ReadShort();
        this.ySubscriptXOffset = this.fontData.ReadShort();
        this.ySubscriptYOffset = this.fontData.ReadShort();
        this.ySuperscriptXSize = this.fontData.ReadShort();
        this.ySuperscriptYSize = this.fontData.ReadShort();
        this.ySuperscriptXOffset = this.fontData.ReadShort();
        this.ySuperscriptYOffset = this.fontData.ReadShort();
        this.yStrikeoutSize = this.fontData.ReadShort();
        this.yStrikeoutPosition = this.fontData.ReadShort();
        this.sFamilyClass = this.fontData.ReadShort();
        this.panose = this.fontData.ReadBytes(10);
        this.ulUnicodeRange1 = this.fontData.ReadULong();
        this.ulUnicodeRange2 = this.fontData.ReadULong();
        this.ulUnicodeRange3 = this.fontData.ReadULong();
        this.ulUnicodeRange4 = this.fontData.ReadULong();
        this.achVendID = this.fontData.ReadString(4);
        this.fsSelection = this.fontData.ReadUShort();
        this.usFirstCharIndex = this.fontData.ReadUShort();
        this.usLastCharIndex = this.fontData.ReadUShort();
        this.sTypoAscender = this.fontData.ReadShort();
        this.sTypoDescender = this.fontData.ReadShort();
        this.sTypoLineGap = this.fontData.ReadShort();
        this.usWinAscent = this.fontData.ReadUShort();
        this.usWinDescent = this.fontData.ReadUShort();

        if (this.version >= 1)
        {
          this.ulCodePageRange1 = this.fontData.ReadULong();
          this.ulCodePageRange2 = this.fontData.ReadULong();

          if (this.version >= 2)
          {
            this.sxHeight = this.fontData.ReadShort();
            this.sCapHeight = this.fontData.ReadShort();
            this.usDefaultChar = this.fontData.ReadUShort();
            this.usBreakChar = this.fontData.ReadUShort();
            this.usMaxContext = this.fontData.ReadUShort();
          }
        }
      }
      catch (Exception ex)
      {
        throw new PdfSharpException(PSSR.ErrorReadingFontData, ex);
      }
    }
  }

  /// <summary>
  /// This table contains additional information needed to use TrueType or OpenTypeTM fonts
  /// on PostScript printers. 
  /// </summary>
  internal class PostScriptTable : OpenTypeFontTable
  {
    public const string Tag = TableTagNames.Post;

    public Fixed formatType;
    public float italicAngle;
    public FWord underlinePosition;
    public FWord underlineThickness;
    public ulong isFixedPitch;
    public ulong minMemType42;
    public ulong maxMemType42;
    public ulong minMemType1;
    public ulong maxMemType1;

    public PostScriptTable(FontData fontData)
      : base(fontData, Tag)
    {
      Read();
    }

    public void Read()
    {
      try
      {
        this.formatType = this.fontData.ReadFixed();
        this.italicAngle = this.fontData.ReadFixed() / 65536f;
        this.underlinePosition = this.fontData.ReadFWord();
        this.underlineThickness = this.fontData.ReadFWord();
        this.isFixedPitch = this.fontData.ReadULong();
        this.minMemType42 = this.fontData.ReadULong();
        this.maxMemType42 = this.fontData.ReadULong();
        this.minMemType1 = this.fontData.ReadULong();
        this.maxMemType1 = this.fontData.ReadULong();
      }
      catch (Exception ex)
      {
        throw new PdfSharpException(PSSR.ErrorReadingFontData, ex);
      }
    }
  }

  /// <summary>
  /// This table contains a list of values that can be referenced by instructions.
  /// They can be used, among other things, to control characteristics for different glyphs.
  /// The length of the table must be an integral number of FWORD units. 
  /// </summary>
  internal class ControlValueTable : OpenTypeFontTable
  {
    public const string Tag = TableTagNames.Cvt;

    FWord[] array; // List of n values referenceable by instructions. n is the number of FWORD items that fit in the size of the table.

    public ControlValueTable(FontData fontData)
      : base(fontData, Tag)
    {
      DirectoryEntry.Tag = TableTagNames.Cvt;
      DirectoryEntry = fontData.tableDictionary[TableTagNames.Cvt];
      Read();
    }

    public void Read()
    {
      try
      {
        int length = DirectoryEntry.Length / 2;
        this.array = new FWord[length];
        for (int idx = 0; idx < length; idx++)
          this.array[idx] = this.fontData.ReadFWord();
      }
      catch (Exception ex)
      {
        throw new PdfSharpException(PSSR.ErrorReadingFontData, ex);
      }
    }
  }

  /// <summary>
  /// This table is similar to the CVT Program, except that it is only run once, when the font is first used.
  /// It is used only for FDEFs and IDEFs. Thus the CVT Program need not contain function definitions.
  /// However, the CVT Program may redefine existing FDEFs or IDEFs. 
  /// </summary>
  internal class FontProgram : OpenTypeFontTable
  {
    public const string Tag = TableTagNames.Fpgm;

    byte[] bytes; // Instructions. n is the number of BYTE items that fit in the size of the table.

    public FontProgram(FontData fontData)
      : base(fontData, Tag)
    {
      DirectoryEntry.Tag = TableTagNames.Fpgm;
      DirectoryEntry = fontData.tableDictionary[TableTagNames.Fpgm];
      Read();
    }

    public void Read()
    {
      try
      {
        int length = DirectoryEntry.Length;
        this.bytes = new byte[length];
        for (int idx = 0; idx < length; idx++)
          this.bytes[idx] = this.fontData.ReadByte();
      }
      catch (Exception ex)
      {
        throw new PdfSharpException(PSSR.ErrorReadingFontData, ex);
      }
    }
  }

  /// <summary>
  /// The Control Value Program consists of a set of TrueType instructions that will be executed whenever the font or 
  /// point size or transformation matrix change and before each glyph is interpreted. Any instruction is legal in the
  /// CVT Program but since no glyph is associated with it, instructions intended to move points within a particular
  /// glyph outline cannot be used in the CVT Program. The name 'prep' is anachronistic. 
  /// </summary>
  internal class ControlValueProgram : OpenTypeFontTable
  {
    public const string Tag = TableTagNames.Prep;

    byte[] bytes; // Set of instructions executed whenever point size or font or transformation change. n is the number of BYTE items that fit in the size of the table.

    public ControlValueProgram(FontData fontData)
      : base(fontData, Tag)
    {
      DirectoryEntry.Tag = TableTagNames.Prep;
      DirectoryEntry = fontData.tableDictionary[TableTagNames.Prep];
      Read();
    }

    public void Read()
    {
      try
      {
        int length = DirectoryEntry.Length;
        this.bytes = new byte[length];
        for (int idx = 0; idx < length; idx++)
          this.bytes[idx] = this.fontData.ReadByte();
      }
      catch (Exception ex)
      {
        throw new PdfSharpException(PSSR.ErrorReadingFontData, ex);
      }
    }
  }

  /// <summary>
  /// This table contains information that describes the glyphs in the font in the TrueType outline format.
  /// Information regarding the rasterizer (scaler) refers to the TrueType rasterizer. 
  /// </summary>
  internal class GlyphSubstitutionTable : OpenTypeFontTable
  {
    public const string Tag = TableTagNames.GSUB;

    public GlyphSubstitutionTable(FontData fontData)
      : base(fontData, Tag)
    {
      DirectoryEntry.Tag = TableTagNames.GSUB;
      DirectoryEntry = fontData.tableDictionary[TableTagNames.GSUB];
      Read();
    }

    public void Read()
    {
      try
      {
      }
      catch (Exception ex)
      {
        throw new PdfSharpException(PSSR.ErrorReadingFontData, ex);
      }
    }
  }
}
