using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Map_Skia.Tests
{
    using HarfBuzzSharp;
    using Map_SkiaStd;
    using PurplePen.Graphics2D;
    using PurplePen.MapModel;
    using SkiaSharp;
    using System.Diagnostics;
    using System.Drawing;

    [TestFixture]
	public class BasicTests
	{
		[Test]
		public void SkiaFontMetrics()
		{
            Skia_TextMetrics skiaMetrics = new Skia_TextMetrics();

            // This used to test that the GDIPlus and Skia font metrics were the same.
            // However, I don't really want to reference GDIPlus in this project, so I'm just going to test that the Skia font metrics are reasonable.
#if false
            GDIPlus_TextMetrics gdiMetrics = new GDIPlus_TextMetrics();

            ITextFaceMetrics gdiFontMetrics = gdiMetrics.GetTextFaceMetrics("Times New Roman", 24, TextEffects.Regular);
            ITextFaceMetrics skiaFontMetrics = skiaMetrics.GetTextFaceMetrics("Times New Roman", 24, TextEffects.Regular);

            Assert.AreEqual(gdiFontMetrics.Ascent, skiaFontMetrics.Ascent);
            Assert.AreEqual(gdiFontMetrics.Descent, skiaFontMetrics.Descent);
            Assert.AreEqual(gdiFontMetrics.CapHeight, skiaFontMetrics.CapHeight, 0.02F);
            Assert.AreEqual(gdiFontMetrics.EmHeight, skiaFontMetrics.EmHeight);
            Assert.AreEqual(gdiFontMetrics.SpaceWidth, skiaFontMetrics.SpaceWidth);
            Assert.AreEqual(gdiFontMetrics.RecommendedLineSpacing, skiaFontMetrics.RecommendedLineSpacing);
            Assert.AreEqual(gdiFontMetrics.GetTextWidth("BananaPhone is great"), skiaFontMetrics.GetTextWidth("BananaPhone is great"), 0.05);

            SizeF gdiSize = gdiFontMetrics.GetTextSize("BananaPhone is great");
            SizeF skiaSize = skiaFontMetrics.GetTextSize("BananaPhone is great");
            Assert.AreEqual(gdiSize.Width, skiaSize.Width, 0.5F);
            Assert.AreEqual(gdiSize.Height, skiaSize.Height, 0.05F);
#endif
            ITextFaceMetrics tnrMetrics = skiaMetrics.GetTextFaceMetrics("Trebuchet MS", 50, TextEffects.Regular);
            Assert.AreEqual(50.0F, tnrMetrics.EmHeight, 0.1F);
            Assert.AreEqual(46.95F, tnrMetrics.Ascent, 0.1F);
            Assert.AreEqual(11.11F, tnrMetrics.Descent, 0.1F);
            Assert.AreEqual(36.25F, tnrMetrics.CapHeight, 0.5F);
            Assert.AreEqual(15.06F, tnrMetrics.SpaceWidth, 0.1F);
            Assert.AreEqual(58.05, tnrMetrics.RecommendedLineSpacing, 0.1F);
            Assert.AreEqual(305.93F, tnrMetrics.GetTextWidth("Hello, world  "), 0.1F);
            Assert.AreEqual(58.06F, tnrMetrics.GetTextSize("Hello, world").Height, 0.1F);

        }

        [Test]
        public void IsTextFaceInstalled()
        {
            Skia_TextMetrics skiaMetrics = new Skia_TextMetrics();

            Assert.IsTrue(skiaMetrics.TextFaceIsInstalled("Arial"));
            Assert.IsTrue(skiaMetrics.TextFaceIsInstalled("Times New Roman"));
            Assert.IsTrue(skiaMetrics.TextFaceIsInstalled("Arial Narrow"));
            Assert.IsTrue(skiaMetrics.TextFaceIsInstalled("Leelawadee UI"));
            Assert.IsTrue(skiaMetrics.TextFaceIsInstalled("Leelawadee UI Semilight"));
            Assert.IsTrue(skiaMetrics.TextFaceIsInstalled("Bahnschrift"));
            Assert.IsTrue(skiaMetrics.TextFaceIsInstalled("Bahnschrift SemiBold"));
            Assert.IsFalse(skiaMetrics.TextFaceIsInstalled("Bahnschrift Bold Banana"));
            Assert.IsFalse(skiaMetrics.TextFaceIsInstalled("Big Chicken"));
            Assert.IsFalse(skiaMetrics.TextFaceIsInstalled("Tekton"));
        }

        // Verifies that two Get() calls with the same family/style return the same instance.
        [Test]
        public void ShapedTypeface_Get_ReturnsSameInstance()
        {
            ShapedTypeface.ClearCache();

            ShapedTypeface a = ShapedTypeface.Get("Arial", SkiaSharp.SKFontStyleWeight.Normal, SkiaSharp.SKFontStyleWidth.Normal, SkiaSharp.SKFontStyleSlant.Upright);
            ShapedTypeface b = ShapedTypeface.Get("Arial", SkiaSharp.SKFontStyleWeight.Normal, SkiaSharp.SKFontStyleWidth.Normal, SkiaSharp.SKFontStyleSlant.Upright);

            Assert.AreSame(a, b);

            b.Dispose();
            a.Dispose();
            ShapedTypeface.ClearCache();
        }

        // Verifies that the cache key comparison is case-insensitive on family name.
        [Test]
        public void ShapedTypeface_Get_CaseInsensitive()
        {
            ShapedTypeface.ClearCache();

            ShapedTypeface lower = ShapedTypeface.Get("arial", SkiaSharp.SKFontStyleWeight.Normal, SkiaSharp.SKFontStyleWidth.Normal, SkiaSharp.SKFontStyleSlant.Upright);
            ShapedTypeface upper = ShapedTypeface.Get("ARIAL", SkiaSharp.SKFontStyleWeight.Normal, SkiaSharp.SKFontStyleWidth.Normal, SkiaSharp.SKFontStyleSlant.Upright);

            Assert.AreSame(lower, upper);

            upper.Dispose();
            lower.Dispose();
            ShapedTypeface.ClearCache();
        }

        // Verifies that different styles produce different cached instances.
        [Test]
        public void ShapedTypeface_Get_DifferentStylesAreDifferentInstances()
        {
            ShapedTypeface.ClearCache();

            ShapedTypeface normal = ShapedTypeface.Get("Arial", SkiaSharp.SKFontStyleWeight.Normal, SkiaSharp.SKFontStyleWidth.Normal, SkiaSharp.SKFontStyleSlant.Upright);
            ShapedTypeface bold = ShapedTypeface.Get("Arial", SkiaSharp.SKFontStyleWeight.Bold, SkiaSharp.SKFontStyleWidth.Normal, SkiaSharp.SKFontStyleSlant.Upright);

            Assert.AreNotSame(normal, bold);

            bold.Dispose();
            normal.Dispose();
            ShapedTypeface.ClearCache();
        }

        // Verifies that a cached entry survives Dispose() and can be reused by a subsequent Get().
        [Test]
        public void ShapedTypeface_CachedEntry_SurvivesDispose()
        {
            ShapedTypeface.ClearCache();

            ShapedTypeface first = ShapedTypeface.Get("Arial", SkiaSharp.SKFontStyleWeight.Normal, SkiaSharp.SKFontStyleWidth.Normal, SkiaSharp.SKFontStyleSlant.Upright);
            first.Dispose();

            // Entry is still in cache with refCount 0; Get() should return the same instance.
            ShapedTypeface second = ShapedTypeface.Get("Arial", SkiaSharp.SKFontStyleWeight.Normal, SkiaSharp.SKFontStyleWidth.Normal, SkiaSharp.SKFontStyleSlant.Upright);
            Assert.AreSame(first, second);

            // The typeface should still be usable (resources not disposed).
            Assert.IsTrue(second.HasGlyph('A'));

            second.Dispose();
            ShapedTypeface.ClearCache();
        }

        // Verifies that ClearCache() removes entries with refCount 0 but not entries still in use.
        [Test]
        public void ShapedTypeface_ClearCache_RemovesUnusedOnly()
        {
            ShapedTypeface.ClearCache();

            ShapedTypeface held = ShapedTypeface.Get("Arial", SkiaSharp.SKFontStyleWeight.Normal, SkiaSharp.SKFontStyleWidth.Normal, SkiaSharp.SKFontStyleSlant.Upright);
            ShapedTypeface released = ShapedTypeface.Get("Times New Roman", SkiaSharp.SKFontStyleWeight.Normal, SkiaSharp.SKFontStyleWidth.Normal, SkiaSharp.SKFontStyleSlant.Upright);
            released.Dispose(); // refCount drops to 0

            ShapedTypeface.ClearCache();

            // "held" should still be the same cached instance.
            ShapedTypeface heldAgain = ShapedTypeface.Get("Arial", SkiaSharp.SKFontStyleWeight.Normal, SkiaSharp.SKFontStyleWidth.Normal, SkiaSharp.SKFontStyleSlant.Upright);
            Assert.AreSame(held, heldAgain);

            // "Times New Roman" was cleared; a new Get() should create a new instance.
            ShapedTypeface newTnr = ShapedTypeface.Get("Times New Roman", SkiaSharp.SKFontStyleWeight.Normal, SkiaSharp.SKFontStyleWidth.Normal, SkiaSharp.SKFontStyleSlant.Upright);
            Assert.AreNotSame(released, newTnr);

            heldAgain.Dispose();
            held.Dispose();
            newTnr.Dispose();
            ShapedTypeface.ClearCache();
        }

        // Verifies that FromTypeface() creates a non-cached instance that disposes normally.
        [Test]
        public void ShapedTypeface_FromTypeface_NotCached()
        {
            ShapedTypeface.ClearCache();

            SkiaSharp.SKTypeface skTypeface = SkiaSharp.SKTypeface.FromFamilyName("Arial");
            ShapedTypeface fromTf = ShapedTypeface.FromTypeface(skTypeface);

            // Should be usable.
            Assert.IsTrue(fromTf.HasGlyph('A'));

            // Should not be the same as a cached instance.
            ShapedTypeface cached = ShapedTypeface.Get("Arial", SkiaSharp.SKFontStyleWeight.Normal, SkiaSharp.SKFontStyleWidth.Normal, SkiaSharp.SKFontStyleSlant.Upright);
            Assert.AreNotSame(fromTf, cached);

            fromTf.Dispose();
            cached.Dispose();
            ShapedTypeface.ClearCache();
        }

        // Helper: returns the path to a font file in the test fonts directory.
        private static string FontPath(string fileName)
        {
            return System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(typeof(BasicTests).Assembly.Location),
                "fonts",
                fileName);
        }

        // Helper: clears private fonts and ShapedTypeface cache before each font matching test.
        private void ClearFontState()
        {
            SkiaFontManager.ClearPrivateFonts();
            ShapedTypeface.ClearCache();
        }

        // Verifies that an exact weight/slant match returns the correct font.
        [Test]
        public void FontMatch_ExactMatch()
        {
            ClearFontState();

            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, FontPath("Roboto-Regular.ttf"));
            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, FontPath("Roboto-Bold.ttf"));

            using (SKTypeface typeface = SkiaFontManager.CreateTypeface("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)) {
                Assert.AreEqual((int)SKFontStyleWeight.Normal, typeface.FontStyle.Weight);
            }

            using (SKTypeface typeface = SkiaFontManager.CreateTypeface("TestFont", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)) {
                Assert.AreEqual((int)SKFontStyleWeight.Bold, typeface.FontStyle.Weight);
            }

            ClearFontState();
        }

        // Verifies that requesting a non-registered family name falls through to system fonts
        // (returns a typeface but not from the private collection).
        [Test]
        public void FontMatch_FamilyNotFound()
        {
            ClearFontState();

            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, FontPath("Roboto-Regular.ttf"));

            // Request a family that was never registered. Should fall through to system fonts.
            using (SKTypeface typeface = SkiaFontManager.CreateTypeface("NonExistentFamily", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)) {
                // Should get Arial (the default fallback), not Roboto.
                Assert.AreNotEqual("Roboto", typeface.FamilyName);
            }

            ClearFontState();
        }

        // Verifies that family name matching is case-insensitive.
        [Test]
        public void FontMatch_CaseInsensitiveFamily()
        {
            ClearFontState();

            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, FontPath("Roboto-Regular.ttf"));

            // Request with different case — should still find the private font.
            using (SKTypeface typeface = SkiaFontManager.CreateTypeface("testfont", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)) {
                Assert.AreEqual("Roboto", typeface.FamilyName);
            }

            using (SKTypeface typeface = SkiaFontManager.CreateTypeface("TESTFONT", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)) {
                Assert.AreEqual("Roboto", typeface.FamilyName);
            }

            ClearFontState();
        }

        // CSS spec: when weight 400 is requested but unavailable, try 500 first.
        [Test]
        public void FontMatch_Weight400_Tries500First()
        {
            ClearFontState();

            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Light, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, FontPath("Roboto-Light.ttf"));
            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Medium, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, FontPath("Roboto-Medium.ttf"));

            using (SKTypeface typeface = SkiaFontManager.CreateTypeface("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)) {
                Assert.AreEqual((int)SKFontStyleWeight.Medium, typeface.FontStyle.Weight);
            }

            ClearFontState();
        }

        // CSS spec: when weight 500 is requested but unavailable, try 400 first.
        [Test]
        public void FontMatch_Weight500_Tries400First()
        {
            ClearFontState();

            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, FontPath("Roboto-Regular.ttf"));
            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, FontPath("Roboto-Bold.ttf"));

            using (SKTypeface typeface = SkiaFontManager.CreateTypeface("TestFont", SKFontStyleWeight.Medium, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)) {
                Assert.AreEqual((int)SKFontStyleWeight.Normal, typeface.FontStyle.Weight);
            }

            ClearFontState();
        }

        // CSS spec: for weight < 400, try descending below first.
        [Test]
        public void FontMatch_WeightBelow400_DescendingFirst()
        {
            ClearFontState();

            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Thin, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, FontPath("Roboto-Thin.ttf"));
            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Light, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, FontPath("Roboto-Light.ttf"));

            // Request ExtraLight (200), which is between Thin (100) and Light (300).
            // For < 400, descending below 200 is tried first → Thin (100).
            // Note: Roboto-Thin.ttf reports its internal weight as 250, not 100.
            using (SKTypeface typeface = SkiaFontManager.CreateTypeface("TestFont", SKFontStyleWeight.ExtraLight, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)) {
                Assert.AreEqual(250, typeface.FontStyle.Weight);
            }

            ClearFontState();
        }

        // CSS spec: for weight > 500, try ascending above first.
        [Test]
        public void FontMatch_WeightAbove500_AscendingFirst()
        {
            ClearFontState();

            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, FontPath("Roboto-Bold.ttf"));
            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Black, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, FontPath("Roboto-Black.ttf"));

            // Request ExtraBold (800), between Bold (700) and Black (900).
            // For > 500, ascending above 800 is tried first → Black (900).
            using (SKTypeface typeface = SkiaFontManager.CreateTypeface("TestFont", SKFontStyleWeight.ExtraBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)) {
                Assert.AreEqual((int)SKFontStyleWeight.Black, typeface.FontStyle.Weight);
            }

            ClearFontState();
        }

        // CSS spec: for weight > 500, if nothing above exists, fall back to descending below.
        [Test]
        public void FontMatch_WeightAbove500_FallsBackDescending()
        {
            ClearFontState();

            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, FontPath("Roboto-Regular.ttf"));

            // Request Bold (700), but only Regular (400) is available.
            // For > 500, ascending above finds nothing, then descending below → Regular (400).
            using (SKTypeface typeface = SkiaFontManager.CreateTypeface("TestFont", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)) {
                Assert.AreEqual((int)SKFontStyleWeight.Normal, typeface.FontStyle.Weight);
            }

            ClearFontState();
        }

        // Verifies that requesting Italic when only Upright exists falls back to Upright.
        [Test]
        public void FontMatch_SlantItalicFallsToUpright()
        {
            ClearFontState();

            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, FontPath("Roboto-Regular.ttf"));

            using (SKTypeface typeface = SkiaFontManager.CreateTypeface("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic)) {
                Assert.AreEqual(SKFontStyleSlant.Upright, typeface.FontStyle.Slant);
            }

            ClearFontState();
        }

        // Verifies that requesting Italic matches Italic when both Upright and Italic are available.
        [Test]
        public void FontMatch_SlantItalicExactMatch()
        {
            ClearFontState();

            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, FontPath("Roboto-Regular.ttf"));
            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic, FontPath("Roboto-Italic.ttf"));

            using (SKTypeface typeface = SkiaFontManager.CreateTypeface("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic)) {
                Assert.AreEqual(SKFontStyleSlant.Italic, typeface.FontStyle.Slant);
            }

            ClearFontState();
        }

        // CSS spec: for normal/condensed widths, narrower widths are preferred first, then wider.
        // We use font files with different weights (Regular=400 vs Bold=700) to distinguish which
        // file was selected, since the loaded typeface reports its own internal width metadata
        // regardless of the width we registered it with.
        [Test]
        public void FontMatch_WidthNormalPrefersNarrower()
        {
            ClearFontState();

            // Register Regular.ttf as Condensed(3), Bold.ttf as Expanded(7), both with same weight key.
            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.Condensed, SKFontStyleSlant.Upright, FontPath("Roboto-Regular.ttf"));
            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.Expanded, SKFontStyleSlant.Upright, FontPath("Roboto-Bold.ttf"));

            // Request Normal width (5). For width <= Normal, narrower first → Condensed (3).
            // The Condensed slot maps to Roboto-Regular.ttf, which reports weight 400.
            using (SKTypeface typeface = SkiaFontManager.CreateTypeface("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)) {
                Assert.AreEqual(400, typeface.FontStyle.Weight, "Should select the Condensed variant (Roboto-Regular.ttf)");
            }

            ClearFontState();
        }

        // CSS spec: for expanded widths, wider widths are preferred first, then narrower.
        [Test]
        public void FontMatch_WidthExpandedPrefersWider()
        {
            ClearFontState();

            // Register Regular.ttf as Condensed(3), Bold.ttf as UltraExpanded(9), both with same weight key.
            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.Condensed, SKFontStyleSlant.Upright, FontPath("Roboto-Regular.ttf"));
            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.UltraExpanded, SKFontStyleSlant.Upright, FontPath("Roboto-Bold.ttf"));

            // Request SemiExpanded (6). For width > Normal, wider first → UltraExpanded (9).
            // The UltraExpanded slot maps to Roboto-Bold.ttf, which reports weight 700.
            using (SKTypeface typeface = SkiaFontManager.CreateTypeface("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.SemiExpanded, SKFontStyleSlant.Upright)) {
                Assert.AreEqual(700, typeface.FontStyle.Weight, "Should select the UltraExpanded variant (Roboto-Bold.ttf)");
            }

            ClearFontState();
        }

        // Combined test: width, slant, and weight narrowing work together correctly.
        // Register several variants and verify three-stage narrowing picks the right one.
        [Test]
        public void FontMatch_CombinedMatching()
        {
            ClearFontState();

            // Register: Regular/Upright, Regular/Italic, Bold/Upright, Bold/Italic
            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, FontPath("Roboto-Regular.ttf"));
            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic, FontPath("Roboto-Italic.ttf"));
            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, FontPath("Roboto-Bold.ttf"));
            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic, FontPath("Roboto-BoldItalic.ttf"));

            // Request SemiBold (600) + Italic. Width matches all (Normal). Slant narrows to Italic variants.
            // Weight: 600 > 500, ascending above → Bold (700).
            using (SKTypeface typeface = SkiaFontManager.CreateTypeface("TestFont", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic)) {
                Assert.AreEqual((int)SKFontStyleWeight.Bold, typeface.FontStyle.Weight);
                Assert.AreEqual(SKFontStyleSlant.Italic, typeface.FontStyle.Slant);
            }

            // Request Medium (500) + Upright. Width matches all. Slant narrows to Upright.
            // Weight: 500 tries 400 → Normal (400).
            using (SKTypeface typeface = SkiaFontManager.CreateTypeface("TestFont", SKFontStyleWeight.Medium, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)) {
                Assert.AreEqual((int)SKFontStyleWeight.Normal, typeface.FontStyle.Weight);
                Assert.AreEqual(SKFontStyleSlant.Upright, typeface.FontStyle.Slant);
            }

            ClearFontState();
        }

        // Verifies that single-variant registration always returns that variant regardless of request.
        [Test]
        public void FontMatch_SingleVariant_AlwaysReturned()
        {
            ClearFontState();

            SkiaFontManager.AddFontFile("TestFont", SKFontStyleWeight.Light, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright, FontPath("Roboto-Light.ttf"));

            // Request something completely different — should still get the only registered variant.
            using (SKTypeface typeface = SkiaFontManager.CreateTypeface("TestFont", SKFontStyleWeight.Black, SKFontStyleWidth.Expanded, SKFontStyleSlant.Italic)) {
                Assert.AreEqual((int)SKFontStyleWeight.Light, typeface.FontStyle.Weight);
            }

            ClearFontState();
        }
    }
}
