using NUnit.Framework;
using Map_SkiaStd;
using SkiaSharp;

namespace Map_Skia.Tests
{
    // Checks that the platform finds a font for text the requested family cannot draw.
    //
    // The rendering test that covers this draws two dozen scripts and compares the
    // result against a stored image, which makes it a picture of one machine's
    // installed fonts: a machine with another build of an emoji font, or another
    // rasteriser, cannot reproduce it however well the fallback works. What the
    // fallback promises is narrower and does hold anywhere -- that a codepoint the
    // chosen family has no glyph for is drawn by some font that does, rather than as
    // .notdef -- so that is what these ask.
    [TestFixture]
    public class FontFallbackTests
    {
        // A representative codepoint from each script the fallback has to reach, with
        // the name it is known by in the failure message.
        private static readonly object[] Scripts = {
            new object[] { 0x4E2D, "Han" },
            new object[] { 0x3042, "Hiragana" },
            new object[] { 0xD55C, "Hangul" },
            new object[] { 0x314E, "Hangul jamo" },
            new object[] { 0x0915, "Devanagari" },
            new object[] { 0x0995, "Bengali" },
            new object[] { 0x0E01, "Thai" },
            new object[] { 0x1780, "Khmer" },
            new object[] { 0x1000, "Myanmar" },
            new object[] { 0x0B95, "Tamil" },
            new object[] { 0x10D0, "Georgian" },
            new object[] { 0x0531, "Armenian" },
            new object[] { 0x1230, "Ethiopic" },
            new object[] { 0x05D0, "Hebrew" },
            new object[] { 0x0627, "Arabic" },
            new object[] { 0x2190, "Arrows" },
            new object[] { 0x2211, "Mathematical operators" },
            new object[] { 0x2654, "Chess" },
            new object[] { 0x2800, "Braille" },
            new object[] { 0x16A0, "Runic" },
        };

        // Arial has no glyph for any of these, so each one has to be found elsewhere.
        //
        // Parameters:
        //   codepoint - the character to draw.
        //   script - what to call it if the assertion fails.
        [TestCaseSource(nameof(Scripts))]
        public void FallbackFindsAFontFor(int codepoint, string script)
        {
            ShapedTypeface resolved = FontFallbackResolver.Resolve(
                codepoint, "Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

            Assert.That(resolved, Is.Not.Null, $"nothing was found for {script} (U+{codepoint:X4})");

            ushort glyph = resolved.Typeface.GetGlyph(codepoint);
            Assert.That(glyph, Is.Not.Zero,
                $"{resolved.Typeface.FamilyName} was chosen for {script} (U+{codepoint:X4}) but has no glyph for it");
        }

        // The family that is asked for is a hint, not a constraint: a codepoint it can
        // draw itself must not send the search elsewhere.
        [Test]
        public void FallbackLeavesACoveredCodepointAlone()
        {
            ShapedTypeface resolved = FontFallbackResolver.Resolve(
                'A', "Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved.Typeface.GetGlyph('A'), Is.Not.Zero);
        }

        // A codepoint no font has is reported as nothing found, rather than as a font
        // that would draw .notdef.
        [Test]
        public void FallbackReportsWhatItCannotFind()
        {
            // A plane 15 private use character, which no installed font claims.
            ShapedTypeface resolved = FontFallbackResolver.Resolve(
                0xFFFFD, "Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);

            if (resolved != null)
                Assert.That(resolved.Typeface.GetGlyph(0xFFFFD), Is.Not.Zero,
                    $"{resolved.Typeface.FamilyName} was chosen for U+FFFFD but has no glyph for it");
        }
    }
}
