using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestWpfMap
{
    using System.Diagnostics;
    using System.Drawing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PurplePen.Graphics2D;
    using PurplePen.MapModel;

    [TestClass]
    public class BasicTests
    {
        [TestMethod]
        public void WpfFontMetrics()
        {
            GDIPlus_TextMetrics gdiMetrics = new GDIPlus_TextMetrics();
            WPF_TextMetrics wpfMetrics = new WPF_TextMetrics();

            ITextFaceMetrics gdiFontMetrics = gdiMetrics.GetTextFaceMetrics("Times New Roman", 24, TextEffects.None);
            ITextFaceMetrics wpfFontMetrics = wpfMetrics.GetTextFaceMetrics("Times New Roman", 24, TextEffects.None);

            Assert.AreEqual(gdiFontMetrics.Ascent, wpfFontMetrics.Ascent);
            Assert.AreEqual(gdiFontMetrics.Descent, wpfFontMetrics.Descent);
            Assert.AreEqual(gdiFontMetrics.CapHeight, wpfFontMetrics.CapHeight, 0.4F);
            Assert.AreEqual(gdiFontMetrics.EmHeight, wpfFontMetrics.EmHeight);
            Assert.AreEqual(gdiFontMetrics.SpaceWidth, wpfFontMetrics.SpaceWidth);
            Assert.AreEqual(gdiFontMetrics.RecommendedLineSpacing, wpfFontMetrics.RecommendedLineSpacing);
            Assert.AreEqual(gdiFontMetrics.GetTextWidth("BananaPhone is great"), wpfFontMetrics.GetTextWidth("BananaPhone is great"), 0.02F);

            SizeF gdiSize = gdiFontMetrics.GetTextSize("BananaPhone is great");
            SizeF skiaSize = wpfFontMetrics.GetTextSize("BananaPhone is great");
            Assert.AreEqual(gdiSize.Width, skiaSize.Width, 0.5F);
            Assert.AreEqual(gdiSize.Height, skiaSize.Height, 1.1F);

            ITextFaceMetrics tnrMetrics = wpfMetrics.GetTextFaceMetrics("Trebuchet MS", 50, TextEffects.None);
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
