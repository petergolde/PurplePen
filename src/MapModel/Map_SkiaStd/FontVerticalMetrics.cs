using SkiaSharp;
using System;

namespace Map_SkiaStd
{
    // Vertical font metrics (ascent, descent, leading) read directly from the font file's
    // OS/2 and hhea tables, expressed in em units.
    //
    // WHY THIS EXISTS: SKFont.Metrics is NOT platform-independent, even when the typeface is
    // loaded from the identical .ttf file via SKTypeface.FromFile. Skia delegates
    // generateFontMetrics() to the platform scaler context, and each platform reads a
    // different field of the font:
    //
    //     Windows (DirectWrite): OS/2 usWinAscent  (sTypoAscender if USE_TYPO_METRICS)
    //     Linux   (FreeType):    hhea.ascender     (sTypoAscender if USE_TYPO_METRICS)
    //     macOS   (CoreText):    hhea.ascender     (via CTFontGetAscent)
    //
    // A TrueType file carries three different ascent values and nothing enforces that they
    // agree. For Roboto (2048 units/em) they are 1536 (sTypo), 1900 (hhea) and 1946 (usWin),
    // so the same font renders text at a different vertical position on each platform. For
    // Calibri the Windows and Linux ascents differ by 27%.
    //
    // This class reproduces the Windows (DirectWrite / GDI+) result on every platform. That
    // is the value Purple Pen must preserve: the GDI+ backend uses FontFamily.GetCellAscent
    // (= usWinAscent), OCAD is a Windows/GDI program, and every existing .ppen and exported
    // .ocd was authored against that number.
    public struct FontVerticalMetrics
    {
        // Distance from the baseline to the top of the text, in em units (positive).
        public float Ascent;

        // Distance from the baseline to the bottom of the text, in em units (positive).
        public float Descent;

        // Extra space between lines beyond ascent + descent, in em units.
        public float Leading;

        private const uint TagOS2 = 0x4F532F32;     // 'OS/2'
        private const uint TagHhea = 0x68686561;    // 'hhea'

        // Minimum table lengths needed for the fields we read.
        private const int MinOS2Length = 78;
        private const int MinHheaLength = 10;

        // Reads a big-endian unsigned 16-bit value at the given offset.
        private static ushort ReadUInt16(byte[] table, int offset)
        {
            return (ushort)((table[offset] << 8) | table[offset + 1]);
        }

        // Reads a big-endian signed 16-bit value at the given offset.
        private static short ReadInt16(byte[] table, int offset)
        {
            return (short)((table[offset] << 8) | table[offset + 1]);
        }

        // Computes the vertical metrics for the given typeface by reading its OS/2 and hhea
        // tables. The returned values are in em units; multiply by the em height to get
        // metrics at a particular size.
        //
        // Parameters:
        //   typeface - the typeface to measure. Must not be null.
        public static FontVerticalMetrics FromTypeface(SKTypeface typeface)
        {
            if (typeface == null)
                throw new ArgumentNullException(nameof(typeface));

            // GetTableData returns null for a table the font doesn't have.
            return FromTables(typeface.GetTableData(TagOS2),
                              typeface.GetTableData(TagHhea),
                              typeface.UnitsPerEm);
        }

        // Computes the vertical metrics from the raw contents of the OS/2 and hhea tables.
        // Either table may be null or too short, in which case the remaining information is
        // used. The returned values are in em units.
        //
        // Parameters:
        //   os2Table - contents of the 'OS/2' table, or null if the font has none.
        //   hheaTable - contents of the 'hhea' table, or null if the font has none.
        //   unitsPerEm - the font's design units per em, from the 'head' table.
        public static FontVerticalMetrics FromTables(byte[] os2Table, byte[] hheaTable, int unitsPerEm)
        {
            if (unitsPerEm <= 0)
                unitsPerEm = 1000;

            short hheaAscender = 0, hheaDescender = 0, hheaLineGap = 0;
            if (hheaTable != null && hheaTable.Length >= MinHheaLength) {
                hheaAscender = ReadInt16(hheaTable, 4);
                hheaDescender = ReadInt16(hheaTable, 6);
                hheaLineGap = ReadInt16(hheaTable, 8);
            }

            bool os2Usable = (os2Table != null && os2Table.Length >= MinOS2Length &&
                              ReadUInt16(os2Table, 0) != 0xFFFF);

            int ascent, descent, leading;

            if (os2Usable && (ReadUInt16(os2Table, 62) & (1 << 7)) != 0) {
                // fsSelection bit 7 (USE_TYPO_METRICS) is set: the font asserts that its
                // sTypo* values are the correct ones to use for line layout. DirectWrite,
                // GDI+ and FreeType all honor this bit.
                ascent = ReadInt16(os2Table, 68);       // sTypoAscender
                descent = -ReadInt16(os2Table, 70);     // sTypoDescender (negative in the font)
                leading = ReadInt16(os2Table, 72);      // sTypoLineGap
            }
            else if (os2Usable) {
                // The normal case: use the Windows metrics, as DirectWrite and GDI+ do.
                ascent = ReadUInt16(os2Table, 74);      // usWinAscent
                descent = ReadUInt16(os2Table, 76);     // usWinDescent

                // The GDI "external leading" formula. usWinAscent/usWinDescent originated as
                // a glyph clipping box, so they are typically taller than the hhea metrics;
                // the line gap is reduced by however much taller they are, floored at zero.
                // This is what DirectWrite reports as DWRITE_FONT_METRICS.lineGap.
                int winCellHeight = ascent + descent;
                int hheaCellHeight = hheaAscender - hheaDescender;
                leading = Math.Max(0, hheaLineGap - (winCellHeight - hheaCellHeight));
            }
            else {
                // No usable OS/2 table (it is technically optional). Fall back to hhea.
                ascent = hheaAscender;
                descent = -hheaDescender;
                leading = hheaLineGap;
            }

            return new FontVerticalMetrics {
                Ascent = (float)ascent / unitsPerEm,
                Descent = (float)descent / unitsPerEm,
                Leading = (float)leading / unitsPerEm,
            };
        }
    }
}
