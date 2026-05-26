using SkiaSharp;
using SkiaSharp.HarfBuzz;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Threading;

namespace Map_SkiaStd
{
    // Wraps an SKTypeface together with its associated HarfBuzz font objects,
    // providing glyph existence checks and text shaping capabilities.
    //
    // The HarfBuzz pipeline is: SKTypeface -> SKStreamAsset -> Blob -> Face -> Font.
    // These objects are created together and must be disposed together.
    //
    // Instances obtained via Get() are cached by font family/style and reference-counted.
    // Dispose() decrements the reference count but does not release resources; cached
    // entries remain available for reuse. Call ClearCache() to dispose entries whose
    // reference count has reached zero.
    //
    // Instances obtained via FromTypeface() are not cached and dispose normally when
    // their reference count reaches zero.
    public class ShapedTypeface : IDisposable
    {
        public readonly SKTypeface Typeface;
        public readonly SKFont CheckFont;           // Used only for glyph existence checks
        public readonly HarfBuzzSharp.Blob HBBlob;
        public readonly HarfBuzzSharp.Face HBFace;
        public readonly HarfBuzzSharp.Font HBFont;
        private readonly SKStreamAsset fontStream;  // Must stay alive; HBBlob references its memory

        // Cache key: family name (upper-cased for case-insensitive comparison), weight, width, slant.
        private static readonly ConcurrentDictionary<(string, SKFontStyleWeight, SKFontStyleWidth, SKFontStyleSlant), ShapedTypeface> cache
            = new ConcurrentDictionary<(string, SKFontStyleWeight, SKFontStyleWidth, SKFontStyleSlant), ShapedTypeface>();

        private int refCount;
        private readonly bool isCached;

        // Private constructor that builds the HarfBuzz pipeline from family name and style.
        //
        // Parameters:
        //   familyName - the font family name (e.g., "Segoe UI", "Arial")
        //   weight - font weight (e.g., SKFontStyleWeight.Normal, SKFontStyleWeight.Bold)
        //   width - font width/stretch (e.g., SKFontStyleWidth.Normal)
        //   slant - font slant (e.g., SKFontStyleSlant.Upright, SKFontStyleSlant.Italic)
        //   cached - true if this instance is managed by the static cache
        private ShapedTypeface(string familyName,
                              SKFontStyleWeight weight,
                              SKFontStyleWidth width,
                              SKFontStyleSlant slant,
                              bool cached)
        {
            Typeface = SkiaFontManager.CreateTypeface(familyName, weight, width, slant);

            // SKFont is used solely for checking glyph availability via GetGlyph().
            // The size doesn't matter for glyph existence checks.
            CheckFont = new SKFont(Typeface);

            // Build the HarfBuzz font from the typeface's raw font data.
            // OpenStream() gives us the raw TrueType/OpenType data as an SKStreamAsset.
            // ToHarfBuzzBlob() wraps the stream's memory (does not copy), so the stream
            // must remain alive for the lifetime of this instance.
            fontStream = Typeface.OpenStream();
            HBBlob = fontStream.ToHarfBuzzBlob();
            HBFace = new HarfBuzzSharp.Face(HBBlob, 0);
            HBFace.UnitsPerEm = Typeface.UnitsPerEm;
            HBFont = new HarfBuzzSharp.Font(HBFace);

            // Set the font scale to design units. HarfBuzz will return glyph positions
            // in these units; we scale to display coordinates later using
            // (fontSize / unitsPerEm).
            HBFont.SetScale(HBFace.UnitsPerEm, HBFace.UnitsPerEm);

            // Cached entries start at 0; each Get() call increments.
            // Non-cached entries are never created through this path.
            refCount = 0;
            isCached = cached;
        }

        // Private constructor that builds the HarfBuzz pipeline from an existing SKTypeface.
        // This constructor is used for the non-cached path. Takes ownership of the typeface.
        private ShapedTypeface(SKTypeface typeface)
        {
            Typeface = typeface;

            CheckFont = new SKFont(Typeface);

            fontStream = Typeface.OpenStream();
            HBBlob = fontStream.ToHarfBuzzBlob();
            HBFace = new HarfBuzzSharp.Face(HBBlob, 0);
            HBFace.UnitsPerEm = Typeface.UnitsPerEm;
            HBFont = new HarfBuzzSharp.Font(HBFace);

            HBFont.SetScale(HBFace.UnitsPerEm, HBFace.UnitsPerEm);

            refCount = 1;
            isCached = false;
        }

        // Returns a cached ShapedTypeface for the given family name and style. If one already
        // exists in the cache, its reference count is incremented and the same instance is
        // returned. Otherwise a new instance is created with reference count 1.
        //
        // Thread-safe: uses ConcurrentDictionary.GetOrAdd and Interlocked.Increment.
        public static ShapedTypeface Get(string familyName,
                                         SKFontStyleWeight weight,
                                         SKFontStyleWidth width,
                                         SKFontStyleSlant slant)
        {
            (string, SKFontStyleWeight, SKFontStyleWidth, SKFontStyleSlant) key =
                (familyName.ToUpperInvariant(), weight, width, slant);

            ShapedTypeface entry = cache.GetOrAdd(key,
                k => new ShapedTypeface(familyName, weight, width, slant, cached: true));

            // Cached entries are created with refCount=0. Each Get() caller increments,
            // so refCount always equals the number of active callers holding a reference.
            Interlocked.Increment(ref entry.refCount);
            return entry;
        }

        // Creates a non-cached ShapedTypeface from an existing SKTypeface. Takes ownership
        // of the typeface. The instance is not stored in the cache and disposes its resources
        // normally when the reference count reaches zero.
        public static ShapedTypeface FromTypeface(SKTypeface typeface)
        {
            return new ShapedTypeface(typeface);
        }

        // Get the data of the font.
        public byte[] GetFontData()
        {
            fontStream.Rewind();
            byte[] data = new byte[fontStream.Length];
            fontStream.Read(data, fontStream.Length);
            return data;
        }

        // Returns true if this typeface contains a glyph for the given Unicode codepoint.
        // A return value of false means the font would render the .notdef glyph (tofu).
        public bool HasGlyph(int codepoint)
        {
            return CheckFont.GetGlyph(codepoint) != 0;
        }

        // Decrements the reference count. For cached entries, resources are not released
        // (the entry stays in the cache for reuse). For non-cached entries, resources are
        // disposed when the count reaches zero.
        public void Dispose()
        {
            int newCount = Interlocked.Decrement(ref refCount);

            if (!isCached && newCount <= 0)
            {
                DisposeResources();
            }
        }

        // Releases all native resources held by this instance.
        private void DisposeResources()
        {
            CheckFont?.Dispose();
            HBFont?.Dispose();
            HBFace?.Dispose();
            HBBlob?.Dispose();
            fontStream?.Dispose();
            Typeface?.Dispose();
        }

        // Disposes and removes all cached entries whose reference count is zero or less
        // (no active callers). Entries still held by callers are left untouched.
        //
        // Thread-safe: iterates a snapshot of cache keys and uses TryRemove. A small race
        // window exists where an entry could be re-requested between the count check and
        // removal, but GetOrAdd handles this by creating a new entry if needed.
        public static void ClearCache()
        {
            foreach ((string, SKFontStyleWeight, SKFontStyleWidth, SKFontStyleSlant) key in cache.Keys)
            {
                if (cache.TryGetValue(key, out ShapedTypeface entry))
                {
                    if (entry.refCount <= 0)
                    {
                        if (cache.TryRemove(key, out ShapedTypeface removed))
                        {
                            removed.DisposeResources();
                        }
                    }
                }
            }
        }
    }

    // This class handles Skia rendering of text, using HarfBuzz for shaping,
    // and handling font fallbacks for missing glyphs.
    //
    // Text rendering with proper international support requires two key capabilities:
    //
    // 1. Text shaping (via HarfBuzz): Converts Unicode text into positioned glyphs,
    //    handling ligatures, kerning, and complex script rules (e.g., Arabic joining,
    //    Devanagari conjuncts). Without shaping, text in complex scripts renders
    //    incorrectly or illegibly.
    //
    // 2. Font fallback: When the primary font doesn't contain a glyph for a character
    //    (e.g., emoji, CJK characters), the text is split into runs and each run is
    //    rendered with the first available font that supports those characters.
    //
    // The overall pipeline is:
    //   Input text
    //     -> Segment by typeface coverage (which font has glyphs for which characters)
    //     -> Shape each segment independently with HarfBuzz
    //     -> Accumulate glyph positions across segments
    //     -> Draw all segments as a single SKTextBlob with multiple font runs
    //
    // This class does not own the ShapedTypeface instances passed to it; the caller
    // retains ownership and is responsible for disposing them.
    public class EnhancedTypeface
    {
        // A contiguous range of text that should be shaped with a specific typeface.
        // Used internally to represent the result of font fallback segmentation.
        private struct TextRun
        {
            public ShapedTypeface Entry;
            public int Start;   // Start index in the original text (UTF-16 char index)
            public int Length;  // Length in UTF-16 chars
        }

        // The result of shaping a single text run with HarfBuzz.
        private struct ShapedRunResult
        {
            public ShapedTypeface Entry;
            public ushort[] GlyphIds;    // Glyph indices within the typeface
            public SKPoint[] Positions;  // Glyph positions, scaled to fontSize, with X accumulated from prior runs
            public uint[] Clusters;      // Cluster values adjusted to indices in the full original text
            public float Width;          // Total advance width of this run
            public int TextStart;        // Start index of this run's text in the original string
            public int TextLength;       // Length of this run's text in UTF-16 chars
        }

        private ShapedTypeface mainEntry;
        private ShapedTypeface[] fallbackEntries;
        private HarfBuzzSharp.Feature[] features;

        // Create an EnhancedTypeface with the main ShapedTypeface, fallback ShapedTypefaces
        // for missing glyphs, and HarfBuzz properties for shaping.
        //
        // The harfBuzzProperties dictionary maps OpenType feature tags (4-character strings
        // like "kern", "liga", "calt") to integer values (typically 1 to enable, 0 to disable).
        // These are passed to HarfBuzz during shaping to control typographic features.
        //
        // This class does not take ownership of the ShapedTypeface instances; the caller
        // must keep them alive for the lifetime of this EnhancedTypeface and dispose them
        // separately.
        public EnhancedTypeface(ShapedTypeface mainTypeface,
                              ShapedTypeface[] fallbackTypefaces,
                              IDictionary<string, int> harfBuzzProperties)
        {
            mainEntry = mainTypeface;
            fallbackEntries = fallbackTypefaces ?? new ShapedTypeface[0];

            // Convert the properties dictionary to HarfBuzz Feature objects.
            List<HarfBuzzSharp.Feature> featureList = new List<HarfBuzzSharp.Feature>();
            if (harfBuzzProperties != null)
            {
                foreach (KeyValuePair<string, int> kvp in harfBuzzProperties)
                {
                    string tag = kvp.Key;
                    if (tag.Length >= 4)
                    {
                        featureList.Add(new HarfBuzzSharp.Feature(
                            new HarfBuzzSharp.Tag(tag[0], tag[1], tag[2], tag[3]),
                            (uint)kvp.Value));
                    }
                }
            }
            features = featureList.ToArray();
        }

        // Finds the first typeface (main or fallback) that contains a glyph for the
        // given Unicode codepoint. Returns the main entry if no typeface has the glyph,
        // which will result in a .notdef (tofu) glyph being rendered.
        private ShapedTypeface FindTypefaceForCodepoint(int codepoint)
        {
            if (mainEntry.HasGlyph(codepoint))
                return mainEntry;

            for (int i = 0; i < fallbackEntries.Length; i++)
            {
                if (fallbackEntries[i].HasGlyph(codepoint))
                    return fallbackEntries[i];
            }

            // No typeface has this glyph; fall back to main (will render .notdef).
            return mainEntry;
        }

        // Segments the input text into contiguous runs, where each run uses a single typeface.
        // Adjacent characters that map to the same typeface are merged into one run.
        //
        // Iterates by Unicode codepoint (not UTF-16 char) to correctly handle surrogate
        // pairs for emoji, supplementary CJK characters, etc.
        private List<TextRun> SegmentByTypeface(string text)
        {
            List<TextRun> runs = new List<TextRun>();

            if (string.IsNullOrEmpty(text))
                return runs;

            ShapedTypeface currentEntry = null;
            int runStart = 0;
            int i = 0;

            while (i < text.Length)
            {
                // Decode the Unicode codepoint at position i.
                // Surrogate pairs (used for codepoints above U+FFFF) occupy two UTF-16 chars.
                int codepoint;
                int charCount;
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    codepoint = char.ConvertToUtf32(text[i], text[i + 1]);
                    charCount = 2;
                }
                else
                {
                    codepoint = text[i];
                    charCount = 1;
                }

                ShapedTypeface entry = FindTypefaceForCodepoint(codepoint);

                if (entry != currentEntry && currentEntry != null)
                {
                    // The typeface changed; emit the accumulated run.
                    runs.Add(new TextRun
                    {
                        Entry = currentEntry,
                        Start = runStart,
                        Length = i - runStart
                    });
                    runStart = i;
                }

                currentEntry = entry;
                i += charCount;
            }

            // Emit the final run.
            if (currentEntry != null)
            {
                runs.Add(new TextRun
                {
                    Entry = currentEntry,
                    Start = runStart,
                    Length = text.Length - runStart
                });
            }

            return runs;
        }

        // Shapes a single text run using HarfBuzz and returns the positioned glyphs.
        //
        // HarfBuzz operates in font design units (typically 1000 or 2048 units per em).
        // The positions it returns must be scaled by (fontSize / unitsPerEm) to convert
        // to display coordinates.
        //
        // The xOffset parameter is the accumulated advance width from prior runs, so that
        // this run's glyphs are positioned after the preceding text.
        private ShapedRunResult ShapeRun(TextRun run, string fullText, float fontSize, float xOffset)
        {
            string runText = fullText.Substring(run.Start, run.Length);
            ShapedTypeface entry = run.Entry;

            // Scale factor: converts HarfBuzz design units to display coordinates.
            float scale = fontSize / entry.HBFace.UnitsPerEm;

            using (HarfBuzzSharp.Buffer buffer = new HarfBuzzSharp.Buffer())
            {
                // Add the run's text to the buffer as UTF-16.
                buffer.AddUtf16(runText);

                // Let HarfBuzz auto-detect text direction (LTR/RTL), script (Latin, Arabic, etc.),
                // and language from the text content.
                buffer.GuessSegmentProperties();

                // Perform shaping: applies the font's OpenType layout rules (GSUB/GPOS tables)
                // to convert Unicode codepoints into positioned glyphs. This handles ligatures,
                // kerning, mark positioning, and complex script reordering.
                entry.HBFont.Shape(buffer, features);

                // Extract the shaping results.
                HarfBuzzSharp.GlyphInfo[] infos = buffer.GlyphInfos;
                HarfBuzzSharp.GlyphPosition[] hbPositions = buffer.GlyphPositions;
                int glyphCount = infos.Length;

                ushort[] glyphIds = new ushort[glyphCount];
                SKPoint[] positions = new SKPoint[glyphCount];
                uint[] clusters = new uint[glyphCount];
                float cursorX = xOffset;

                for (int i = 0; i < glyphCount; i++)
                {
                    // After shaping, GlyphInfo.Codepoint contains the glyph ID (not the
                    // original Unicode codepoint). This is the index into the font's glyph table.
                    glyphIds[i] = (ushort)infos[i].Codepoint;

                    // Compute the display position for this glyph:
                    //   X = cursor position + per-glyph X offset (scaled from design units)
                    //   Y = per-glyph Y offset (scaled, negated because HarfBuzz uses Y-up
                    //       while Skia uses Y-down)
                    //
                    // These positions are relative to the baseline at Y=0; the caller adds
                    // the ascent offset when drawing.
                    positions[i] = new SKPoint(
                        cursorX + hbPositions[i].XOffset * scale,
                        -hbPositions[i].YOffset * scale
                    );

                    // Adjust cluster values to be indices into the full original text string,
                    // not just this run's substring. HarfBuzz cluster values are UTF-16 char
                    // indices into the text that was added to the buffer.
                    clusters[i] = (uint)(infos[i].Cluster + run.Start);

                    // Advance the cursor by this glyph's advance width.
                    cursorX += hbPositions[i].XAdvance * scale;
                }

                return new ShapedRunResult
                {
                    Entry = entry,
                    GlyphIds = glyphIds,
                    Positions = positions,
                    Clusters = clusters,
                    Width = cursorX - xOffset,
                    TextStart = run.Start,
                    TextLength = run.Length
                };
            }
        }

        // Shapes the complete text string, handling font fallback.
        // Returns a list of shaped runs (one per typeface segment) and the total advance width.
        private (List<ShapedRunResult> runs, float totalWidth) ShapeText(string text, float fontSize)
        {
            List<TextRun> textRuns = SegmentByTypeface(text);
            List<ShapedRunResult> shapedRuns = new List<ShapedRunResult>();
            float xOffset = 0;

            foreach (TextRun textRun in textRuns)
            {
                ShapedRunResult result = ShapeRun(textRun, text, fontSize, xOffset);
                shapedRuns.Add(result);
                xOffset += result.Width;
            }

            return (shapedRuns, xOffset);
        }

        // Builds an SKTextBlob from the shaped runs. Each run becomes a separate positioned
        // run in the blob, allowing different typefaces within a single blob.
        //
        // The yBaseline parameter is added to all glyph Y positions to shift from baseline-
        // relative coordinates to the desired drawing position.
        //
        // Returns null if there are no glyphs to draw.
        private SKTextBlob BuildTextBlob(List<ShapedRunResult> runs, float fontSize, float yBaseline, SKPaint paint)
        {
            using (SKTextBlobBuilder builder = new SKTextBlobBuilder())
            {
                bool hasGlyphs = false;

                foreach (ShapedRunResult run in runs)
                {
                    if (run.GlyphIds.Length == 0)
                        continue;

                    hasGlyphs = true;

                    // Apply the baseline offset to Y positions.
                    SKPoint[] adjustedPositions = new SKPoint[run.Positions.Length];
                    for (int i = 0; i < run.Positions.Length; i++)
                    {
                        adjustedPositions[i] = new SKPoint(
                            run.Positions[i].X,
                            run.Positions[i].Y + yBaseline
                        );
                    }

                    // Each run gets its own font in the text blob, enabling mixed-typeface rendering.
                    // All properties are explicitly set to avoid platform-dependent defaults
                    // that can cause non-deterministic glyph rasterization.
                    using (SKFont skFont = new SKFont(run.Entry.Typeface, fontSize))
                    {
                        skFont.Edging = paint.IsAntialias ? SKFontEdging.Antialias : SKFontEdging.Alias;
                        skFont.Hinting = SKFontHinting.None;
                        skFont.Subpixel = false;
                        skFont.EmbeddedBitmaps = false;
                        skFont.LinearMetrics = true;
                        skFont.BaselineSnap = false;
                        skFont.ForceAutoHinting = false;
                        SKPositionedRunBuffer runBuffer = builder.AllocatePositionedRun(skFont, run.GlyphIds.Length);
                        runBuffer.SetGlyphs(run.GlyphIds);
                        runBuffer.SetPositions(adjustedPositions);
                    }
                }

                return hasGlyphs ? builder.Build() : null;
            }
        }

        // Returns the ascent of the main typeface at the given font size.
        // The ascent is the distance from the top of the tallest glyph to the baseline,
        // returned as a positive value.
        private float GetMainAscent(float fontSize)
        {
            using (SKFont skFont = new SKFont(mainEntry.Typeface, fontSize))
            {
                skFont.GetFontMetrics(out SKFontMetrics metrics);
                return -metrics.Ascent; // Skia reports ascent as negative; we return positive.
            }
        }

        // Shape the text using HarfBuzz, and then draw it on the canvas
        // using SkiaSharp, using the main typeface and fallback typefaces as needed.
        //
        // The origin is the top-left corner of the text block. Internally, the text is
        // drawn at origin.Y + ascent because Skia's text drawing functions position text
        // at the baseline, not the top.
        //
        // When DRAW_TEXT_AS_PATHS is defined, text is rendered by converting glyph outlines
        // to paths and filling/stroking them. This bypasses the platform glyph rasterizer
        // (DirectWrite on Windows) entirely, producing deterministic pixel-identical output
        // across runs, at the cost of losing hinting optimizations.
        public void DrawText(SKCanvas canvas, string text, SKPoint origin, float fontSize, SKPaint paint)
        {
            if (string.IsNullOrEmpty(text))
                return;

#if DRAW_TEXT_AS_PATHS
            using (SKPath textPath = GetTextPath(text, origin, fontSize))
            {
                if (!textPath.IsEmpty)
                {
                    canvas.DrawPath(textPath, paint);
                }
            }
#else
            (List<ShapedRunResult> runs, float totalWidth) = ShapeText(text, fontSize);
            float ascent = GetMainAscent(fontSize);

            using (SKTextBlob blob = BuildTextBlob(runs, fontSize, ascent, paint))
            {
                if (blob != null)
                {
                    canvas.DrawText(blob, origin.X, origin.Y, paint);
                }
            }
#endif
        }

        // Shape the text using HarfBuzz and return an SKPath that outlines the text glyphs,
        // using the main typeface and fallback typefaces as needed.
        //
        // The origin is the top-left corner of the text block. Internally, the ascent is
        // added to the Y coordinate because glyph paths are relative to the baseline.
        //
        // Returns an empty path if the text is null or empty.
        public SKPath GetTextPath(string text, SKPoint origin, float fontSize)
        {
            SKPath resultPath = new SKPath();

            if (string.IsNullOrEmpty(text))
                return resultPath;

            (List<ShapedRunResult> runs, float totalWidth) = ShapeText(text, fontSize);
            float ascent = GetMainAscent(fontSize);

            foreach (ShapedRunResult run in runs)
            {
                if (run.GlyphIds.Length == 0)
                    continue;

                using (SKFont skFont = new SKFont(run.Entry.Typeface, fontSize))
                {
                    for (int i = 0; i < run.GlyphIds.Length; i++)
                    {
                        using (SKPath glyphPath = skFont.GetGlyphPath(run.GlyphIds[i]))
                        {
                            if (glyphPath != null && !glyphPath.IsEmpty)
                            {
                                // Translate glyph path to the correct position, including
                                // origin offset and baseline adjustment.
                                float x = origin.X + run.Positions[i].X;
                                float y = origin.Y + run.Positions[i].Y + ascent;
                                resultPath.AddPath(glyphPath, x, y);
                            }
                        }
                    }
                }
            }

            return resultPath;
        }

        // Returns the total advance width of the shaped text. The advance width is the
        // horizontal distance the cursor moves after drawing the full text string.
        public float MeasureTextAdvanceWidth(string text, float fontSize)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            (List<ShapedRunResult> runs, float totalWidth) = ShapeText(text, fontSize);
            return totalWidth;
        }

        // Returns the tight bounding box of the shaped text. The bounds are computed from
        // per-glyph bounding boxes (via SKFont.GetGlyphWidths) and are relative to the
        // baseline at Y=0 (negative Top means above baseline, positive Bottom means below).
        // This matches the coordinate convention used by SKPaint.MeasureText and
        // SKFont.MeasureText.
        public SKRect MeasureTextBounds(string text, float fontSize)
        {
            if (string.IsNullOrEmpty(text))
                return SKRect.Empty;

            (List<ShapedRunResult> runs, float totalWidth) = ShapeText(text, fontSize);

            // Compute tight bounds by unioning each glyph's bounding box offset by its position.
            SKRect totalBounds = SKRect.Empty;
            bool hasBounds = false;

            foreach (ShapedRunResult run in runs)
            {
                if (run.GlyphIds.Length == 0)
                    continue;

                using (SKFont skFont = new SKFont(run.Entry.Typeface, fontSize))
                {
                    // GetGlyphWidths returns per-glyph advance widths and tight bounding boxes.
                    // The bounds are relative to each glyph's origin (0, 0).
                    SKRect[] glyphBounds = new SKRect[run.GlyphIds.Length];
                    skFont.GetGlyphWidths(run.GlyphIds, null, glyphBounds);

                    for (int i = 0; i < run.GlyphIds.Length; i++)
                    {
                        // Offset the glyph's tight bounds by its shaped position.
                        SKRect gb = glyphBounds[i];
                        SKRect positioned = new SKRect(
                            run.Positions[i].X + gb.Left,
                            run.Positions[i].Y + gb.Top,
                            run.Positions[i].X + gb.Right,
                            run.Positions[i].Y + gb.Bottom
                        );

                        if (!hasBounds)
                        {
                            totalBounds = positioned;
                            hasBounds = true;
                        }
                        else
                        {
                            totalBounds = SKRect.Union(totalBounds, positioned);
                        }
                    }
                }
            }

            return totalBounds;
        }

        // Measure the size of the shaped text, and return the position of each glyph.
        //
        // Each GlyphPosition includes the glyph ID, the source text it represents
        // (derived from HarfBuzz cluster mapping), its absolute position on the canvas,
        // and the typeface that should be used to render it.
        public GlyphPosition[] GetGlyphPositions(string text, SKPoint origin, float fontSize)
        {
            if (string.IsNullOrEmpty(text))
                return new GlyphPosition[0];

            (List<ShapedRunResult> runs, float totalWidth) = ShapeText(text, fontSize);
            float ascent = GetMainAscent(fontSize);

            List<GlyphPosition> result = new List<GlyphPosition>();

            foreach (ShapedRunResult run in runs)
            {
                for (int i = 0; i < run.GlyphIds.Length; i++)
                {
                    // Determine the source text that this glyph represents using HarfBuzz
                    // cluster mapping. A "cluster" groups one or more glyphs that correspond
                    // to one or more source characters (e.g., a ligature glyph maps to multiple
                    // source characters, or a combining sequence produces one glyph from
                    // multiple codepoints).
                    //
                    // The cluster value is the UTF-16 char index in the original text where
                    // this glyph's source characters start. We find the end of the cluster
                    // by looking for the next different cluster value.
                    int clusterStart = (int)run.Clusters[i];
                    int clusterEnd = FindClusterEnd(run, i, text.Length);

                    string glyphText = (clusterStart < clusterEnd)
                        ? text.Substring(clusterStart, clusterEnd - clusterStart)
                        : "";

                    result.Add(new GlyphPosition
                    {
                        GlyphId = run.GlyphIds[i],
                        GlyphText = glyphText,
                        Position = new SKPoint(
                            origin.X + run.Positions[i].X,
                            origin.Y + ascent + run.Positions[i].Y
                        ),
                        Typeface = run.Entry.Typeface
                    });
                }
            }

            return result.ToArray();
        }

        // Finds the end of the cluster that glyph at index glyphIndex belongs to.
        // Scans forward through the cluster array to find the next different cluster value,
        // which marks the start of the next cluster (and thus the end of the current one).
        // If this is the last unique cluster in the run, the end is the run's text end.
        //
        // For LTR text, clusters increase monotonically. For RTL text, they decrease.
        // This method handles both cases by taking the absolute range.
        private int FindClusterEnd(ShapedRunResult run, int glyphIndex, int textLength)
        {
            uint currentCluster = run.Clusters[glyphIndex];

            // Look for the next glyph with a different cluster value.
            for (int j = glyphIndex + 1; j < run.Clusters.Length; j++)
            {
                if (run.Clusters[j] != currentCluster)
                {
                    int nextCluster = (int)run.Clusters[j];
                    // For LTR, nextCluster > currentCluster; for RTL, it may be less.
                    // Return whichever is further from clusterStart.
                    return Math.Max((int)currentCluster + 1, nextCluster);
                }
            }

            // This is the last unique cluster in the run; it extends to the end of
            // the run's portion of the original text.
            return Math.Min(run.TextStart + run.TextLength, textLength);
        }
    }

    // Indicates the position of a glyph, including its ID, the text it represents,
    // its position on the canvas, and the typeface used to render it.
    public class GlyphPosition
    {
        public int GlyphId;
        public string GlyphText;
        public SKPoint Position;
        public SKTypeface Typeface;
    }


    // Manages private SKTypeFace instances, or falls back to system-installed fonts if not found. 
    // Also handles parsing of font family name suffixes to match requested font styles to available system fonts,
    // and provides a default font family name to use when no match is found.
    public static class SkiaFontManager
    {
        private static object lockObj = new object();
        private static Dictionary<FontKey, string> privateTypeFace = new Dictionary<FontKey, string>();
        private static string defaultFontFamilyName = "Arial";
        private static readonly SuffixEntry[] allSuffixes = BuildSuffixTable();


        // The default typeface for our current platform. Skia uses this as a fallback when Skia's font matching
        // fails to find a reasonable match for the requested family name, so we check it to see if Skia
        // did a fallback. We DON'T use this as OUR default, that is set via SetDefaultFontFamilyName, and defaults to Arial if not set.
        private static SKTypeface skiaDefaultFont = SKTypeface.FromFamilyName(null);

        // Add a new font file path for a font. If this familyName/fontStyle is later requested,
        // use the given font path to load the font.
        public static void AddFontFile(string familyName, SKFontStyleWeight weight, SKFontStyleWidth width, SKFontStyleSlant slant, string fontFilePath)
        {
            lock (lockObj) {
                fontFilePath = Path.GetFullPath(fontFilePath);
                if (!File.Exists(fontFilePath)) {
                    Debug.Fail("Font path doesn't exist.");
                    return;
                }

                FontKey fontKey = new FontKey(familyName, weight, width, slant);
                if (!privateTypeFace.ContainsKey(fontKey)) {
                    privateTypeFace.Add(fontKey, fontFilePath);
                }
            }
        }

        // Set the default font family name to use when CreateTypeface is called with a family name that doesn't match any private or system font.
        // This defaults to "Arial" if not set. Be sure to set this to a font that is actually installed. It can be a font registered with
        // AddFontFile.
        public static void SetDefaultFontFamilyName(string familyName)
        {
            lock (lockObj) {
                defaultFontFamilyName = familyName;
            }
        }

        // Create a typeface, using either a system font or a private font file if one was registered with AddFontFile. No caching is 
        // done at this layer (even for private font files), because that is done at the ShapedTypeface layer instead. This function
        // always creates a new SKTypeface, and ownership and responsibility for Disposing it is passed to the caller.
        //
        // Never returns null; if nothing is found the font registered as the default font (usually "Arial") will be returned,
        // or if that isn't found either, then the platform default font will be returned.
        public static SKTypeface CreateTypeface(string familyName, SKFontStyleWeight weight, SKFontStyleWidth width, SKFontStyleSlant slant)
        {
            SKTypeface typeface;

            lock (lockObj) {
                // Try the private font collection first, using CSS Fonts Level 3 Section 5.2
                // matching rules to find the best available variant for the requested style.
                string privateFontPath = FindBestPrivateFontMatch(familyName, weight, width, slant);
                if (privateFontPath != null) {
                    typeface = SKTypeface.FromFile(privateFontPath);
                    Debug.Assert(typeface != null, "Failed to load typeface from private font file; check that you registered your font with a valid font file");
                    if (typeface != null) {
                        // If the family name wasn't what is in the font, re-record using information from the typeface. This allows us to re-get the font from the typeface; e.g., in
                        // PdfGraphicsTarget.XFontFromTypeface.
                        if (!string.Equals(typeface.FamilyName, familyName, StringComparison.OrdinalIgnoreCase)) {
                            FontKey perfectKey = new FontKey(typeface.FamilyName, (SKFontStyleWeight) typeface.FontWeight, (SKFontStyleWidth) typeface.FontWidth, typeface.FontSlant);
                            if (!privateTypeFace.ContainsKey(perfectKey)) {
                                privateTypeFace.Add(perfectKey, privateFontPath);
                            }
                        }
                        return typeface;
                    }
                }

                // No private font matched this family name. Try system fonts with suffix parsing.
                typeface = TryGetTypefaceFromNameAndStyle(familyName, weight, width, slant);

                if (typeface == null && familyName != defaultFontFamilyName) {
                    // Still no font found in either the private collection or system fonts.
                    // We default to the default font family name set (usually "Arial"), not to the platform default font. This is how OCAD works.
                    // So try again with the default family name.
                    typeface = CreateTypeface(defaultFontFamilyName, weight, width, slant);
                }

                if (typeface == null) {
                    Debug.Fail("Default font was not found. Please configure the font manager with a valid default font family name that is installed on the system, or register a private font file for the default font family name.");

                    // SKTypeface.FromFamilyName will never return null, it will just return the default font if the family name isn't found.
                    // So if we got here, it means even the default font family name isn't found, so we just create a typeface
                    // with the default family name and hope for the best. It will likely fall back to the platform default font,
                    // which may not be what we want but at least something will be rendered.
                    typeface = SKTypeface.FromFamilyName(defaultFontFamilyName, weight, width, slant);
                }

                return typeface;
            }
        }

        public static bool FontFamilyIsInstalled(string familyName)
        {
            lock (lockObj) {
                try {
                    if (privateTypeFace.Any(pair => string.Equals(pair.Key.familyName, familyName, StringComparison.OrdinalIgnoreCase))) {
                        return true;
                    }
                    else {
                        using (SKTypeface typeface = TryGetTypefaceFromNameAndStyle(familyName, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)) {
                            return typeface != null;
                        }
                    }
                }
                catch {
                    return false;
                }
            }
        }

        // These are common suffixes on font names. Skia doesn't seem to handle these font suffixes in its matching
        // algorith, so we do it instead in TryGetTypefaceFromNameAndStyle.
        private static SuffixEntry[] BuildSuffixTable()
        {
            var entries = new List<SuffixEntry>();

            // Weight suffixes
            entries.Add(new SuffixEntry("Extra Black", SuffixKind.Weight, 950));
            entries.Add(new SuffixEntry("ExtraBlack", SuffixKind.Weight, 950, true));
            entries.Add(new SuffixEntry("Ultra Black", SuffixKind.Weight, 950));
            entries.Add(new SuffixEntry("UltraBlack", SuffixKind.Weight, 950));
            entries.Add(new SuffixEntry("Black", SuffixKind.Weight, 900, true));
            entries.Add(new SuffixEntry("Heavy", SuffixKind.Weight, 900));
            entries.Add(new SuffixEntry("Extra Bold", SuffixKind.Weight, 800));
            entries.Add(new SuffixEntry("ExtraBold", SuffixKind.Weight, 800, true));
            entries.Add(new SuffixEntry("Ultra Bold", SuffixKind.Weight, 800));
            entries.Add(new SuffixEntry("UltraBold", SuffixKind.Weight, 800));
            entries.Add(new SuffixEntry("Bold", SuffixKind.Weight, 700, true));
            entries.Add(new SuffixEntry("Semi Bold", SuffixKind.Weight, 600));
            entries.Add(new SuffixEntry("SemiBold", SuffixKind.Weight, 600, true));
            entries.Add(new SuffixEntry("Semibold", SuffixKind.Weight, 600));
            entries.Add(new SuffixEntry("Demi Bold", SuffixKind.Weight, 600));
            entries.Add(new SuffixEntry("DemiBold", SuffixKind.Weight, 600));
            entries.Add(new SuffixEntry("Demibold", SuffixKind.Weight, 600));
            entries.Add(new SuffixEntry("Medium", SuffixKind.Weight, 500, true));
            entries.Add(new SuffixEntry("Light", SuffixKind.Weight, 300, true));
            entries.Add(new SuffixEntry("Semi Light", SuffixKind.Weight, 350));
            entries.Add(new SuffixEntry("SemiLight", SuffixKind.Weight, 350, true));
            entries.Add(new SuffixEntry("Semilight", SuffixKind.Weight, 350));
            entries.Add(new SuffixEntry("Extra Light", SuffixKind.Weight, 200));
            entries.Add(new SuffixEntry("ExtraLight", SuffixKind.Weight, 200, true));
            entries.Add(new SuffixEntry("Ultra Light", SuffixKind.Weight, 200));
            entries.Add(new SuffixEntry("UltraLight", SuffixKind.Weight, 200));
            entries.Add(new SuffixEntry("Thin", SuffixKind.Weight, 100, true));
            entries.Add(new SuffixEntry("Hairline", SuffixKind.Weight, 100));

            // Width suffixes (values match SKFontStyleWidth enum)
            entries.Add(new SuffixEntry("Ultra Condensed", SuffixKind.Width, 1));
            entries.Add(new SuffixEntry("UltraCondensed", SuffixKind.Width, 1, true));
            entries.Add(new SuffixEntry("Extra Condensed", SuffixKind.Width, 2));
            entries.Add(new SuffixEntry("ExtraCondensed", SuffixKind.Width, 2, true));
            entries.Add(new SuffixEntry("Condensed", SuffixKind.Width, 3, true));
            entries.Add(new SuffixEntry("Narrow", SuffixKind.Width, 3));
            entries.Add(new SuffixEntry("Compressed", SuffixKind.Width, 3));
            entries.Add(new SuffixEntry("Semi Condensed", SuffixKind.Width, 4));
            entries.Add(new SuffixEntry("SemiCondensed", SuffixKind.Width, 4, true));
            entries.Add(new SuffixEntry("Semi Expanded", SuffixKind.Width, 6));
            entries.Add(new SuffixEntry("SemiExpanded", SuffixKind.Width, 6, true));
            entries.Add(new SuffixEntry("Expanded", SuffixKind.Width, 7));
            entries.Add(new SuffixEntry("Wide", SuffixKind.Width, 7, true));
            entries.Add(new SuffixEntry("Extra Expanded", SuffixKind.Width, 8));
            entries.Add(new SuffixEntry("ExtraExpanded", SuffixKind.Width, 8, true));
            entries.Add(new SuffixEntry("Ultra Expanded", SuffixKind.Width, 9));
            entries.Add(new SuffixEntry("UltraExpanded", SuffixKind.Width, 9, true));

            // Slant suffixes (values match SKFontStyleSlant enum)
            entries.Add(new SuffixEntry("Italic", SuffixKind.Slant, (int)SKFontStyleSlant.Italic, true));
            entries.Add(new SuffixEntry("Oblique", SuffixKind.Slant, (int)SKFontStyleSlant.Oblique, true));

            // Sort longest first so "Extra Bold" is tried before "Bold"
            return entries.OrderByDescending(e => e.Suffix.Length).ToArray();
        }

        /// <summary>
        /// Resolves a font name and weight/width/slant to a TypeFace.
        /// This is similar to what SKTypeface.FromFamilyName does, with key differences:
        /// 1) Resolves suffixes like "Segoe UI Light" or "Arial Narrow Bold Italic"). to an
        /// Any suffixes parsed from the name override the passed in weight/width/slant.
        /// 2) Returns null if the font cannot be resolved, instead of a platform default
        /// like "Segoe UI".
        /// 3) Uses any typefaces registered with AddFontFile, which SKTypeface.FromFamilyName doesn't know about.
        /// </summary>
        public static SKTypeface TryGetTypefaceFromNameAndStyle(
            string familyName,
            SKFontStyleWeight weightModifier,
            SKFontStyleWidth widthModifier,
            SKFontStyleSlant slantModifier)
        {
            // Try the full name as-is first — maybe SkiaSharp or our registry handles it directly
            SKTypeface typeface = CreateTypefaceFromSystemOrPrivate(familyName, weightModifier, widthModifier, slantModifier);
            if (typeface != null)
                return typeface;

            // Parse suffixes from the name
            var baseName = familyName.Trim();
            int? parsedWeight = null;
            int? parsedWidth = null;
            int? parsedSlant = null;

            bool found;
            do {
                found = false;
                foreach (var entry in allSuffixes) {
                    if (!baseName.EndsWith(" " + entry.Suffix, StringComparison.OrdinalIgnoreCase))
                        continue;

                    switch (entry.Kind) {
                    case SuffixKind.Weight:
                        if (!parsedWeight.HasValue)
                            parsedWeight = entry.Value;
                        break;
                    case SuffixKind.Width:
                        if (!parsedWidth.HasValue)
                            parsedWidth = entry.Value;
                        break;
                    case SuffixKind.Slant:
                        if (!parsedSlant.HasValue)
                            parsedSlant = entry.Value;
                        break;
                    }

                    baseName = baseName.Substring(0, baseName.Length - entry.Suffix.Length - 1).TrimEnd();
                    found = true;
                    break; // restart the loop with the shortened name
                }
            } while (found && baseName.Length > 0);

            if (baseName.Length == 0) {
                // We stripped everything — the whole name was suffixes, which is wrong.
                return null;
            }

            // Build final style: parsed suffixes override the explicit parameters
            var finalStyle = new SKFontStyle(
                parsedWeight ?? (int)weightModifier,
                parsedWidth ?? (int)widthModifier,
                parsedSlant.HasValue ? (SKFontStyleSlant)parsedSlant.Value : slantModifier);

            typeface = CreateTypefaceFromSystemOrPrivate(baseName, 
                parsedWeight.HasValue ? (SKFontStyleWeight)parsedWeight.Value : weightModifier,
                parsedWidth.HasValue ? (SKFontStyleWidth)parsedWidth.Value : widthModifier,
                parsedSlant.HasValue ? (SKFontStyleSlant)parsedSlant.Value : slantModifier);

            return typeface;
        }

        // Create a typeface, using either a system font or a private font file if one was registered with AddFontFile. No caching is 
        // done at this layer (even for private font files), because that is done at the ShapedTypeface layer instead. This function
        // always creates a new SKTypeface, and ownership and responsibility for Disposing it is passed to the caller. Returns null
        // (not a default typeface) if no matching typeface in the system or private registry.
        private static SKTypeface CreateTypefaceFromSystemOrPrivate(string familyName, SKFontStyleWeight weight, SKFontStyleWidth width, SKFontStyleSlant slant)
        {
            lock (lockObj) {
                // Try the private font collection first, using CSS Fonts Level 3 Section 5.2
                // matching rules to find the best available variant for the requested style.
                string privateFontPath = FindBestPrivateFontMatch(familyName, weight, width, slant);
                if (privateFontPath != null) {
                    return SKTypeface.FromFile(privateFontPath);
                }

                // SKTypeface.FromFamilyName always returns something. We want to return null if it can't find a reasonable match,
                // so we have to apply some heuristics (encapsulated in IsGoodFamilyNameMatch) to the result to determine
                // if it's a good match or just a fallback.
                SKTypeface typeface = SKTypeface.FromFamilyName(familyName, weight, width, slant);
                if (!IsGoodFamilyNameMatch(familyName, typeface)) {
                    typeface.Dispose();
                    return null;
                }

                return typeface;
            }
        }

        // Is this a likely good match for the requested family name? Skia's font matching can be unpredictable,
        // especially when the requested family name is not installed. This method applies some heuristics
        // to determine if the returned typeface is a reasonable match or if it's likely a fallback that
        // doesn't correspond to the requested family at all.
        private static bool IsGoodFamilyNameMatch(string requestedFamily, SKTypeface result)
        {
            // Exact match (case-insensitive) — definitely good
            if (string.Equals(requestedFamily, result.FamilyName, StringComparison.OrdinalIgnoreCase))
                return true;

            // Check if the platform even recognizes this family name
            using (SKFontStyleSet styleSet = SKFontManager.Default.GetFontStyles(requestedFamily)) {
                if (styleSet.Count == 0)
                    return false;
            }

            // If its the default fallback family, it's not a good match.
            // This is a heuristic to detect when Skia fails to find any reasonable match and just returns the default font.
            return (result.FamilyName != skiaDefaultFont.FamilyName);
        }



        // Returns the default suffix string for a given SuffixKind and value,
        // or null if no default suffix exists.
        private static string GetDefaultSuffix(SuffixKind kind, int value)
        {
            foreach (SuffixEntry entry in allSuffixes) {
                if (entry.Kind == kind && entry.Value == value && entry.DefaultSuffix) {
                    return entry.Suffix;
                }
            }
            return null;
        }

        // Returns an array of all available font family names, combining both
        // private registered fonts and system fonts from SKFontManager.Default.
        // For system fonts, each font style in a family generates a suffixed name
        // based on its weight,\ and width (e.g., "Bahnschrift Light",
        // "Bahnschrift SemiBold Condensed").
        // Weight 400/700, width 5 are considered standard and produce no suffix.
        // Duplicates are removed using case-insensitive comparison, and the
        // result is sorted alphabetically (case-insensitive).
        public static string[] GetFontFamilies()
        {
            HashSet<string> familyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            lock (lockObj) {
                foreach (FontKey key in privateTypeFace.Keys) {
                    familyNames.Add(key.familyName);
                }
            }

            SKFontManager systemFontManager = SKFontManager.Default;
            foreach (string family in systemFontManager.FontFamilies) {
                using (SKFontStyleSet styleSet = systemFontManager.GetFontStyles(family)) {
                    // Check if all styles in this family share the same weight or width.
                    // If so, suppress that suffix even if the value isn't Normal/Bold.
                    bool allSameWeight = true;
                    bool allSameWidth = true;
                    if (styleSet.Count > 0) {
                        int firstWeight = styleSet[0].Weight;
                        int firstWidth = (int)styleSet[0].Width;
                        for (int i = 1; i < styleSet.Count; i++) {
                            if (styleSet[i].Weight != firstWeight)
                                allSameWeight = false;
                            if ((int)styleSet[i].Width != firstWidth)
                                allSameWidth = false;
                        }
                    }

                    for (int i = 0; i < styleSet.Count; i++) {
                        SKFontStyle style = styleSet[i];
                        string name = family;

                        int weight = style.Weight;
                        if (!allSameWeight && weight != (int)SKFontStyleWeight.Normal && weight != (int)SKFontStyleWeight.Bold) {
                            string weightSuffix = GetDefaultSuffix(SuffixKind.Weight, weight);
                            if (weightSuffix != null) {
                                name += " " + weightSuffix;
                            }
                        }

                        int width = (int)style.Width;
                        if (!allSameWidth && width != (int)SKFontStyleWidth.Normal) {
                            string widthSuffix = GetDefaultSuffix(SuffixKind.Width, width);
                            if (widthSuffix != null) {
                                name += " " + widthSuffix;
                            }
                        }

                        familyNames.Add(name);
                    }
                }
            }

            string[] result = familyNames.ToArray();
            Array.Sort(result, StringComparer.OrdinalIgnoreCase);
            return result;
        }

        // Clears all registered private font files. Primarily for test isolation.
        public static void ClearPrivateFonts()
        {
            lock (lockObj) {
                privateTypeFace.Clear();
            }
        }

        // Finds the best-matching font file path from the private font collection using
        // CSS Fonts Level 3 Section 5.2 font style matching rules.
        //
        // The algorithm narrows candidates in order: width (font-stretch), then slant
        // (font-style), then weight. At each stage, the candidate set is reduced to
        // only those entries that best match the requested property.
        //
        // Parameters:
        //   familyName - the requested font family name (matched case-insensitively)
        //   weight     - the requested font weight (e.g., Normal=400, Bold=700)
        //   width      - the requested font width/stretch (e.g., Normal=5, Condensed=3)
        //   slant      - the requested font slant (Upright, Italic, or Oblique)
        //
        // Returns:
        //   The font file path of the best-matching registered font, or null if no font
        //   with a matching family name is registered in the private font collection.
        //
        // IMPORTANT: Must be called while holding lockObj.
        private static string FindBestPrivateFontMatch(
            string familyName,
            SKFontStyleWeight weight,
            SKFontStyleWidth width,
            SKFontStyleSlant slant)
        {
            // Stage 1: Filter by family name (case-insensitive exact match).
            List<KeyValuePair<FontKey, string>> candidates = new List<KeyValuePair<FontKey, string>>();
            foreach (KeyValuePair<FontKey, string> pair in privateTypeFace) {
                if (string.Equals(pair.Key.familyName, familyName, StringComparison.OrdinalIgnoreCase)) {
                    candidates.Add(pair);
                }
            }

            if (candidates.Count == 0)
                return null;

            if (candidates.Count == 1)
                return candidates[0].Value;

            // Stage 2: Narrow by width (font-stretch).
            candidates = NarrowByWidth(candidates, (int)width);

            if (candidates.Count == 1)
                return candidates[0].Value;

            // Stage 3: Narrow by slant (font-style).
            candidates = NarrowBySlant(candidates, slant);

            if (candidates.Count == 1)
                return candidates[0].Value;

            // Stage 4: Select by weight.
            return SelectByWeight(candidates, (int)weight);
        }

        // Narrows the candidate list to those entries whose width best matches the
        // requested width, following CSS Fonts Level 3 Section 5.2 font-stretch rules.
        //
        // If the requested width is normal (5) or condensed (1-4), narrower widths are
        // preferred first, then wider. If expanded (6-9), wider widths are preferred
        // first, then narrower.
        private static List<KeyValuePair<FontKey, string>> NarrowByWidth(
            List<KeyValuePair<FontKey, string>> candidates,
            int requestedWidth)
        {
            // Collect distinct available widths.
            HashSet<int> availableWidths = new HashSet<int>();
            foreach (KeyValuePair<FontKey, string> pair in candidates) {
                availableWidths.Add((int)pair.Key.width);
            }

            // If exact match exists, use it.
            if (availableWidths.Contains(requestedWidth)) {
                return FilterByWidth(candidates, requestedWidth);
            }

            int bestWidth;

            if (requestedWidth <= (int)SKFontStyleWidth.Normal) {
                // Normal or condensed: try narrower first (descending), then wider (ascending).
                bestWidth = LargestBelow(availableWidths, requestedWidth);
                if (bestWidth == -1)
                    bestWidth = SmallestAbove(availableWidths, requestedWidth);
            }
            else {
                // Expanded: try wider first (ascending), then narrower (descending).
                bestWidth = SmallestAbove(availableWidths, requestedWidth);
                if (bestWidth == -1)
                    bestWidth = LargestBelow(availableWidths, requestedWidth);
            }

            if (bestWidth == -1)
                return candidates;

            return FilterByWidth(candidates, bestWidth);
        }

        // Filters the candidate list to only entries with the given width value.
        private static List<KeyValuePair<FontKey, string>> FilterByWidth(
            List<KeyValuePair<FontKey, string>> candidates,
            int width)
        {
            List<KeyValuePair<FontKey, string>> result = new List<KeyValuePair<FontKey, string>>();
            foreach (KeyValuePair<FontKey, string> pair in candidates) {
                if ((int)pair.Key.width == width)
                    result.Add(pair);
            }
            return result;
        }

        // Narrows the candidate list to those entries whose slant best matches the
        // requested slant, following CSS Fonts Level 3 Section 5.2 font-style rules.
        //
        // Italic requested:  prefer Italic -> Oblique -> Upright
        // Oblique requested: prefer Oblique -> Italic -> Upright
        // Upright requested: prefer Upright -> Oblique -> Italic
        private static List<KeyValuePair<FontKey, string>> NarrowBySlant(
            List<KeyValuePair<FontKey, string>> candidates,
            SKFontStyleSlant requestedSlant)
        {
            SKFontStyleSlant[] preferenceOrder;

            switch (requestedSlant) {
            case SKFontStyleSlant.Italic:
                preferenceOrder = new[] { SKFontStyleSlant.Italic, SKFontStyleSlant.Oblique, SKFontStyleSlant.Upright };
                break;
            case SKFontStyleSlant.Oblique:
                preferenceOrder = new[] { SKFontStyleSlant.Oblique, SKFontStyleSlant.Italic, SKFontStyleSlant.Upright };
                break;
            default:
                preferenceOrder = new[] { SKFontStyleSlant.Upright, SKFontStyleSlant.Oblique, SKFontStyleSlant.Italic };
                break;
            }

            foreach (SKFontStyleSlant slant in preferenceOrder) {
                List<KeyValuePair<FontKey, string>> filtered = new List<KeyValuePair<FontKey, string>>();
                foreach (KeyValuePair<FontKey, string> pair in candidates) {
                    if (pair.Key.slant == slant)
                        filtered.Add(pair);
                }
                if (filtered.Count > 0)
                    return filtered;
            }

            // Should not reach here since candidates is non-empty.
            return candidates;
        }

        // Selects the font file path from the candidates whose weight best matches
        // the requested weight, following CSS Fonts Level 3 Section 5.2 font-weight rules.
        //
        // For weights below 400: try descending below the requested weight, then ascending above.
        // For weight 400: try 500, then descending below 400, then ascending above 500.
        // For weight 500: try 400, then descending below 400, then ascending above 500.
        // For weights above 500: try ascending above the requested weight, then descending below.
        private static string SelectByWeight(
            List<KeyValuePair<FontKey, string>> candidates,
            int requestedWeight)
        {
            // Build a sorted set of available weights and a map from weight to file path.
            SortedSet<int> availableWeights = new SortedSet<int>();
            Dictionary<int, string> weightToPath = new Dictionary<int, string>();
            foreach (KeyValuePair<FontKey, string> pair in candidates) {
                int w = (int)pair.Key.weight;
                availableWeights.Add(w);
                if (!weightToPath.ContainsKey(w))
                    weightToPath[w] = pair.Value;
            }

            // Exact match.
            if (weightToPath.ContainsKey(requestedWeight))
                return weightToPath[requestedWeight];

            int bestWeight;

            if (requestedWeight == 400) {
                // Try 500, then descending below 400, then ascending above 500.
                if (weightToPath.ContainsKey(500))
                    return weightToPath[500];
                bestWeight = LargestBelow(availableWeights, 400);
                if (bestWeight == -1)
                    bestWeight = SmallestAbove(availableWeights, 500);
            }
            else if (requestedWeight == 500) {
                // Try 400, then descending below 400, then ascending above 500.
                if (weightToPath.ContainsKey(400))
                    return weightToPath[400];
                bestWeight = LargestBelow(availableWeights, 400);
                if (bestWeight == -1)
                    bestWeight = SmallestAbove(availableWeights, 500);
            }
            else if (requestedWeight < 400) {
                // Descending below requested, then ascending above.
                bestWeight = LargestBelow(availableWeights, requestedWeight);
                if (bestWeight == -1)
                    bestWeight = SmallestAbove(availableWeights, requestedWeight);
            }
            else {
                // Above 500: ascending above requested, then descending below.
                bestWeight = SmallestAbove(availableWeights, requestedWeight);
                if (bestWeight == -1)
                    bestWeight = LargestBelow(availableWeights, requestedWeight);
            }

            if (bestWeight != -1 && weightToPath.ContainsKey(bestWeight))
                return weightToPath[bestWeight];

            // Fallback: should not happen, but return first candidate.
            return candidates[0].Value;
        }

        // Returns the largest value in the collection that is strictly less than target,
        // or -1 if no such value exists.
        private static int LargestBelow(IEnumerable<int> values, int target)
        {
            int best = -1;
            foreach (int v in values) {
                if (v < target && v > best)
                    best = v;
            }
            return best;
        }

        // Returns the smallest value in the collection that is strictly greater than target,
        // or -1 if no such value exists.
        private static int SmallestAbove(IEnumerable<int> values, int target)
        {
            int best = -1;
            foreach (int v in values) {
                if (v > target && (best == -1 || v < best))
                    best = v;
            }
            return best;
        }

        // Class to hold a suffix entry for parsing font names. Each entry includes the suffix string,
        // the kind of suffix (weight/width/slant), and the value to apply if this suffix is present.

        private class SuffixEntry
        {
            public string Suffix { get; }
            public SuffixKind Kind { get; }
            public int Value { get; }
            public bool DefaultSuffix { get; }

            public SuffixEntry(string suffix, SuffixKind kind, int value, bool defaultSuffix = false)
            {
                Suffix = suffix;
                Kind = kind;
                Value = value;
                DefaultSuffix = defaultSuffix;
            }
        }

        private enum SuffixKind { Weight, Width, Slant }

        // Struct to hold a key for distinguishing fonts.
        private struct FontKey
        {
            public string familyName;
            public SKFontStyleWeight weight;
            public SKFontStyleWidth width;
            public SKFontStyleSlant slant;

            public FontKey(string familyName, SKFontStyleWeight weight, SKFontStyleWidth width, SKFontStyleSlant slant)
            {
                this.familyName = familyName;
                this.weight = weight;
                this.width = width;
                this.slant = slant;
            }
        }

    }
}
