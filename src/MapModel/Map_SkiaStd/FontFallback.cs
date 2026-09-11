using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

namespace Map_SkiaStd
{
    // Knowledge about scripts that OpenType cannot render from a font's character map alone.
    //
    // Most scripts render correctly as soon as the font has a glyph for each codepoint. A
    // handful do not: cursive joining (Arabic, Syriac), glyph reordering and conjunct
    // formation (the Indic scripts), and dependent-vowel positioning (Khmer, Myanmar) are all
    // implemented by rules that live in the font's GSUB and GPOS layout tables. A font that
    // maps those codepoints through cmap but declares none of the required script tags will
    // shape the text into visibly wrong output, so it must not be preferred during font
    // fallback over a font that is properly equipped.
    //
    // The table of required tags mirrors the one Avalonia uses (FontFallbackScriptHints), which
    // in turn follows the OpenType script tag registry. Indic and Myanmar scripts have both a
    // modern ("dev2") and a legacy ("deva") tag; declaring either one is enough.
    public static class ComplexScripts
    {
        private const uint TagGSUB = 0x47535542;    // 'GSUB'
        private const uint TagGPOS = 0x47504F53;    // 'GPOS'

        // No script that needs OpenType layout tables has codepoints below U+0600 (Arabic is
        // the lowest). Latin, Greek, Cyrillic, Armenian, Hebrew and all common punctuation are
        // therefore below this threshold and can skip the check entirely, which keeps the cost
        // off the path that nearly all Purple Pen text takes.
        private const int LowestComplexCodepoint = 0x0600;

        // Builds a 4-byte OpenType tag from its characters, in the big-endian order used by
        // both OpenType and HarfBuzz.
        private static uint MakeTag(char a, char b, char c, char d)
        {
            return ((uint)a << 24) | ((uint)b << 16) | ((uint)c << 8) | (uint)d;
        }

        private static readonly uint TagArab = MakeTag('a', 'r', 'a', 'b');
        private static readonly uint TagSyrc = MakeTag('s', 'y', 'r', 'c');
        private static readonly uint TagMong = MakeTag('m', 'o', 'n', 'g');
        private static readonly uint TagThaa = MakeTag('t', 'h', 'a', 'a');
        private static readonly uint TagKhmr = MakeTag('k', 'h', 'm', 'r');
        private static readonly uint TagTibt = MakeTag('t', 'i', 'b', 't');
        private static readonly uint TagSinh = MakeTag('s', 'i', 'n', 'h');
        private static readonly uint TagDev2 = MakeTag('d', 'e', 'v', '2');
        private static readonly uint TagDeva = MakeTag('d', 'e', 'v', 'a');
        private static readonly uint TagBng2 = MakeTag('b', 'n', 'g', '2');
        private static readonly uint TagBeng = MakeTag('b', 'e', 'n', 'g');
        private static readonly uint TagGur2 = MakeTag('g', 'u', 'r', '2');
        private static readonly uint TagGuru = MakeTag('g', 'u', 'r', 'u');
        private static readonly uint TagGjr2 = MakeTag('g', 'j', 'r', '2');
        private static readonly uint TagGujr = MakeTag('g', 'u', 'j', 'r');
        private static readonly uint TagOry2 = MakeTag('o', 'r', 'y', '2');
        private static readonly uint TagOrya = MakeTag('o', 'r', 'y', 'a');
        private static readonly uint TagTml2 = MakeTag('t', 'm', 'l', '2');
        private static readonly uint TagTaml = MakeTag('t', 'a', 'm', 'l');
        private static readonly uint TagTel2 = MakeTag('t', 'e', 'l', '2');
        private static readonly uint TagTelu = MakeTag('t', 'e', 'l', 'u');
        private static readonly uint TagKnd2 = MakeTag('k', 'n', 'd', '2');
        private static readonly uint TagKnda = MakeTag('k', 'n', 'd', 'a');
        private static readonly uint TagMlm2 = MakeTag('m', 'l', 'm', '2');
        private static readonly uint TagMlym = MakeTag('m', 'l', 'y', 'm');
        private static readonly uint TagMym2 = MakeTag('m', 'y', 'm', '2');
        private static readonly uint TagMymr = MakeTag('m', 'y', 'm', 'r');

        // Returns true if the given codepoint could possibly belong to a script that needs
        // OpenType layout tables. A false result is definitive and needs no script lookup;
        // a true result means TryGetRequiredTags should be consulted.
        //
        // Parameters:
        //   codepoint - the Unicode codepoint to test.
        public static bool MayRequireLayoutTables(int codepoint)
        {
            return codepoint >= LowestComplexCodepoint;
        }

        // Returns the GSUB/GPOS script tag(s) that a font must declare in order to shape the
        // given script. Returns false for scripts that render acceptably from the character
        // map alone (Latin, CJK, Hangul, Hebrew, Thai, ...), for which no layout-table check
        // should be applied.
        //
        // Parameters:
        //   script - the script of the text, as reported by HarfBuzz.
        //   primary - on success, the preferred script tag.
        //   secondary - on success, the alternative script tag. Equal to primary for scripts
        //               that have only one.
        public static bool TryGetRequiredTags(HarfBuzzSharp.Script script, out uint primary, out uint secondary)
        {
            if (script.Equals(HarfBuzzSharp.Script.Arabic)) { primary = secondary = TagArab; return true; }
            if (script.Equals(HarfBuzzSharp.Script.Syriac)) { primary = secondary = TagSyrc; return true; }
            if (script.Equals(HarfBuzzSharp.Script.Mongolian)) { primary = secondary = TagMong; return true; }
            if (script.Equals(HarfBuzzSharp.Script.Thaana)) { primary = secondary = TagThaa; return true; }
            if (script.Equals(HarfBuzzSharp.Script.Khmer)) { primary = secondary = TagKhmr; return true; }
            if (script.Equals(HarfBuzzSharp.Script.Tibetan)) { primary = secondary = TagTibt; return true; }
            if (script.Equals(HarfBuzzSharp.Script.Sinhala)) { primary = secondary = TagSinh; return true; }
            if (script.Equals(HarfBuzzSharp.Script.Devanagari)) { primary = TagDev2; secondary = TagDeva; return true; }
            if (script.Equals(HarfBuzzSharp.Script.Bengali)) { primary = TagBng2; secondary = TagBeng; return true; }
            if (script.Equals(HarfBuzzSharp.Script.Gurmukhi)) { primary = TagGur2; secondary = TagGuru; return true; }
            if (script.Equals(HarfBuzzSharp.Script.Gujarati)) { primary = TagGjr2; secondary = TagGujr; return true; }
            if (script.Equals(HarfBuzzSharp.Script.Oriya)) { primary = TagOry2; secondary = TagOrya; return true; }
            if (script.Equals(HarfBuzzSharp.Script.Tamil)) { primary = TagTml2; secondary = TagTaml; return true; }
            if (script.Equals(HarfBuzzSharp.Script.Telugu)) { primary = TagTel2; secondary = TagTelu; return true; }
            if (script.Equals(HarfBuzzSharp.Script.Kannada)) { primary = TagKnd2; secondary = TagKnda; return true; }
            if (script.Equals(HarfBuzzSharp.Script.Malayalam)) { primary = TagMlm2; secondary = TagMlym; return true; }
            if (script.Equals(HarfBuzzSharp.Script.Myanmar)) { primary = TagMym2; secondary = TagMymr; return true; }

            primary = 0;
            secondary = 0;
            return false;
        }

        // Reads the set of OpenType script tags that the given typeface declares in its GSUB
        // and GPOS layout tables. A font with neither table returns an empty set.
        //
        // Parameters:
        //   typeface - the typeface to inspect. Must not be null.
        public static HashSet<uint> ReadDeclaredScriptTags(SKTypeface typeface)
        {
            if (typeface == null)
                throw new ArgumentNullException(nameof(typeface));

            HashSet<uint> tags = new HashSet<uint>();

            // GetTableData returns null for a table the font doesn't have.
            AddScriptTagsFromTable(typeface.GetTableData(TagGSUB), tags);
            AddScriptTagsFromTable(typeface.GetTableData(TagGPOS), tags);

            return tags;
        }

        // Adds the script tags declared by one GSUB or GPOS table to the given set.
        //
        // Both tables start with the same header: a 32-bit version followed by 16-bit offsets
        // to the ScriptList, FeatureList and LookupList. The ScriptList is a 16-bit record
        // count followed by that many 6-byte records, each a 4-byte script tag and a 2-byte
        // offset. Only the tags are needed here, so the records themselves are not followed.
        //
        // Parameters:
        //   table - the raw contents of the table, or null if the font doesn't have it.
        //   tags - the set to add the declared tags to.
        private static void AddScriptTagsFromTable(byte[] table, HashSet<uint> tags)
        {
            // 10 bytes is the shortest possible header (version plus three offsets).
            if (table == null || table.Length < 10)
                return;

            int scriptListOffset = ReadUInt16(table, 4);
            if (scriptListOffset == 0 || scriptListOffset + 2 > table.Length)
                return;

            int scriptCount = ReadUInt16(table, scriptListOffset);
            int recordsStart = scriptListOffset + 2;

            // A malformed or truncated table is ignored rather than read past the end.
            if (recordsStart + scriptCount * 6 > table.Length)
                return;

            for (int i = 0; i < scriptCount; i++) {
                tags.Add(ReadUInt32(table, recordsStart + i * 6));
            }
        }

        // Reads a big-endian unsigned 16-bit value at the given offset.
        private static ushort ReadUInt16(byte[] table, int offset)
        {
            return (ushort)((table[offset] << 8) | table[offset + 1]);
        }

        // Reads a big-endian unsigned 32-bit value at the given offset.
        private static uint ReadUInt32(byte[] table, int offset)
        {
            return ((uint)table[offset] << 24) | ((uint)table[offset + 1] << 16) |
                   ((uint)table[offset + 2] << 8) | table[offset + 3];
        }
    }

    // Finds a font for a codepoint that the requested font cannot render, by asking the
    // platform's own font fallback engine through SKFontManager.MatchCharacter:
    //
    //     Windows  IDWriteFontFallback::MapCharacters  (the DirectWrite system fallback)
    //     macOS    CTFontCreateForString
    //     Linux    fontconfig, matching on FC_CHARSET and FC_LANG
    //
    // This is exactly what Avalonia does for its own text rendering -- it ships no curated
    // list of fallback families and delegates the whole question to the platform. Doing the
    // same here replaces a hand-written, Windows-only list of families with the fallback
    // chain the operating system itself uses, on every platform.
    //
    // Private fonts registered through SkiaFontManager.AddFontFile are deliberately not
    // considered: the platform cannot see them, and fallback is about covering codepoints the
    // chosen font lacks, not about substituting for the chosen font.
    public static class FontFallbackResolver
    {
        // Cache of resolved fallbacks, keyed by everything that can change the answer. A null
        // value is a negative entry recording that the platform has no font for the codepoint,
        // so the miss is not retried for every character of every string that contains it.
        private static readonly ConcurrentDictionary<(int, string, SKFontStyleWeight, SKFontStyleWidth, SKFontStyleSlant, string), ShapedTypeface> cache
            = new ConcurrentDictionary<(int, string, SKFontStyleWeight, SKFontStyleWidth, SKFontStyleSlant, string), ShapedTypeface>();

        private static string language = null;

        // The BCP-47 language tag handed to the platform when resolving a fallback. It decides
        // Han unification: U+4E2D should resolve to a different font for zh-CN, ja-JP and
        // ko-KR, and the tag is the only way to say which is wanted. Null (the default) means
        // the current UI culture, which is what Avalonia uses. Set it to the empty string to
        // give the platform no hint at all, which makes fallback independent of the user's
        // language settings -- useful when deterministic output is wanted.
        public static string Language
        {
            get { return language ?? CultureInfo.CurrentUICulture.Name; }
            set { language = value; }
        }

        // Finds a font that can render the given codepoint. Returns null if the platform has
        // no font for it, in which case the caller should render .notdef (tofu).
        //
        // The returned instance is owned by the ShapedTypeface cache; the caller must not
        // dispose it.
        //
        // Parameters:
        //   codepoint - the Unicode codepoint that needs a font.
        //   familyName - the family the text asked for. Passed to the platform as a hint so
        //                that the fallback is stylistically close to it. May be null.
        //   weight, width, slant - the style that was requested for the text.
        public static ShapedTypeface Resolve(int codepoint, string familyName,
                                             SKFontStyleWeight weight, SKFontStyleWidth width, SKFontStyleSlant slant)
        {
            (int, string, SKFontStyleWeight, SKFontStyleWidth, SKFontStyleSlant, string) key =
                (codepoint, familyName ?? "", weight, width, slant, Language);

            ShapedTypeface entry;
            if (cache.TryGetValue(key, out entry))
                return entry;

            ShapedTypeface resolved = Match(codepoint, familyName, weight, width, slant, key.Item6);

            if (cache.TryAdd(key, resolved))
                return resolved;

            // Another thread resolved the same key first. Both calls found the same face --
            // ShapedTypeface.GetOrAdd collapses them -- so release the extra reference this
            // one took and use the cached entry, keeping one reference per cache entry.
            if (resolved != null)
                resolved.Dispose();

            return cache.TryGetValue(key, out entry) ? entry : resolved;
        }

        // Asks the platform for a font covering the codepoint and wraps the result in a
        // cached ShapedTypeface. Returns null if the platform has nothing.
        private static ShapedTypeface Match(int codepoint, string familyName,
                                            SKFontStyleWeight weight, SKFontStyleWidth width, SKFontStyleSlant slant,
                                            string languageTag)
        {
            string[] bcp47 = string.IsNullOrEmpty(languageTag) ? null : new string[] { languageTag };

            // An empty family name must be passed as null, or the platform treats it as a
            // request for a family literally called "".
            SKTypeface matched = SKFontManager.Default.MatchCharacter(
                string.IsNullOrEmpty(familyName) ? null : familyName, weight, width, slant, bcp47, codepoint);

            if (matched == null)
                return null;

            // Hand ownership of the typeface to the shared cache, which also makes the face
            // reachable by family name -- PDF export relies on that to embed it later.
            return ShapedTypeface.GetOrAdd(matched);
        }

        // Drops every cached fallback and releases the reference each one holds on its
        // ShapedTypeface, so that those entries become collectable. Called by
        // ShapedTypeface.ClearCache(); there is no reason to call it directly.
        internal static void ClearCache()
        {
            foreach ((int, string, SKFontStyleWeight, SKFontStyleWidth, SKFontStyleSlant, string) key in cache.Keys) {
                ShapedTypeface entry;
                if (cache.TryRemove(key, out entry) && entry != null)
                    entry.Dispose();
            }
        }
    }
}
