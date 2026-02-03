using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Map_Skia.Tests
{
    using System.Diagnostics;
    using System.Drawing;
    using PurplePen.Graphics2D;
    using PurplePen.MapModel;

    [TestFixture]
	public class BasicTests
	{
		[Test]
		public void SkiaFontMetrics()
		{
			GDIPlus_TextMetrics gdiMetrics = new GDIPlus_TextMetrics();
			Skia_TextMetrics skiaMetrics = new Skia_TextMetrics();

            ITextFaceMetrics gdiFontMetrics = gdiMetrics.GetTextFaceMetrics("Times New Roman", 24, TextEffects.None);
            ITextFaceMetrics skiaFontMetrics = skiaMetrics.GetTextFaceMetrics("Times New Roman", 24, TextEffects.None);

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

            ITextFaceMetrics tnrMetrics = skiaMetrics.GetTextFaceMetrics("Trebuchet MS", 50, TextEffects.None);
            Assert.AreEqual(50.0F, tnrMetrics.EmHeight, 0.1F);
            Assert.AreEqual(46.95F, tnrMetrics.Ascent, 0.1F);
            Assert.AreEqual(11.11F, tnrMetrics.Descent, 0.1F);
            Assert.AreEqual(36.25F, tnrMetrics.CapHeight, 0.5F);
            Assert.AreEqual(15.06F, tnrMetrics.SpaceWidth, 0.1F);
            Assert.AreEqual(58.05, tnrMetrics.RecommendedLineSpacing, 0.1F);
            Assert.AreEqual(305.93F, tnrMetrics.GetTextWidth("Hello, world  "), 0.1F);
            Assert.AreEqual(58.06F, tnrMetrics.GetTextSize("Hello, world").Height, 0.1F);

        }
    }
}
