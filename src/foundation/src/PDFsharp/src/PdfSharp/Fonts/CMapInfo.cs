// PDFsharp - A .NET library for processing PDF
// See the LICENSE file in the solution root for more information.

using Microsoft.Extensions.Logging;
using PdfSharp.Fonts.Internal;
using PdfSharp.Fonts.OpenType;
using PdfSharp.Logging;
using PdfSharp.Pdf.Internal;

namespace PdfSharp.Fonts
{
    /// <summary>
    /// Helper class that determines the characters used in a particular font.
    /// </summary>
    class CMapInfo
    {
        public CMapInfo(OpenTypeDescriptor descriptor)
        {
            Debug.Assert(descriptor != null);
            _descriptor = descriptor;
        }
        readonly OpenTypeDescriptor _descriptor;

        public void AddChars(CodePointGlyphIndexPair[] codePoints)
        {
            int length = codePoints.Length;

            for (int idx = 0; idx < length; idx++)
            {
                var item = codePoints[idx];
                if (item.GlyphIndex == 0)
                    continue;

                // Every glyph that is drawn must be in the subset, so record it before the
                // code point check below. One code point can legitimately map to more than one
                // glyph -- a contextual alternate, or a substitution supplied by a RenderTextEvent
                // handler -- and CodePointsToGlyphIndices can only remember the first of them.
                // Recording the glyph afterwards would leave the later ones out of the embedded
                // font while the content stream still refers to them.
                GlyphIndices[item.GlyphIndex] = default;

                if (CodePointsToGlyphIndices.ContainsKey(item.CodePoint))
                    continue;

                CodePointsToGlyphIndices.Add(item.CodePoint, item.GlyphIndex);
                MinCodePoint = Math.Min(MinCodePoint, item.CodePoint);
                MaxCodePoint = Math.Max(MaxCodePoint, item.CodePoint);
            }
        }

        internal bool Contains(char ch)
            => CodePointsToGlyphIndices.ContainsKey(ch);

        public int[] Chars
        {
            get
            {
                var chars = new int[CodePointsToGlyphIndices.Count];
                CodePointsToGlyphIndices.Keys.CopyTo(chars, 0);
                Array.Sort(chars);
                return chars;
            }
        }

        public ushort[] GetGlyphIndices()
        {
            var indices = new ushort[GlyphIndices.Count];
            GlyphIndices.Keys.CopyTo(indices, 0);
            Array.Sort(indices);
            return indices;
        }

        public int MinCodePoint = Int32.MaxValue;  
        public int MaxCodePoint = Int32.MinValue;

        /// <summary>
        /// Maps a Unicode code point to a glyph ID.
        /// </summary>
        public Dictionary<int, ushort> CodePointsToGlyphIndices = [];

        /// <summary>
        /// Collects all used glyph IDs. Value is not used.
        /// </summary>
        public Dictionary<ushort, object?> GlyphIndices = [];
    }
}
