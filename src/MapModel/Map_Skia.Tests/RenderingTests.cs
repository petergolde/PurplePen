using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using TestingUtils;

namespace Map_Skia.Tests
{
    [TestFixture]
    public class RenderingTests
    {
        private const int MAX_PIXEL_DIFF = 25;

        void CheckTest(string filename, bool testLightenedColor, bool roundtripToOcad, int minOcadVersion, int maxOcadVersion)
        {
            string fullname = TestUtil.GetTestFile("skia_render\\" + filename);
            bool ok = RenderingUtil.VerifyTestFile(fullname, true, false, testLightenedColor, roundtripToOcad, false, minOcadVersion, maxOcadVersion, MAX_PIXEL_DIFF);
            Assert.IsTrue(ok, string.Format("Rendering test {0} did not compare correctly.", filename), ok);
        }

        void CheckTestAntiAlias(string filename, bool testLightenedColor, bool roundtripToOcad, int minOcadVersion, int maxOcadVersion)
        {
            string fullname = TestUtil.GetTestFile("skia_render\\" + filename);
            bool ok = RenderingUtil.VerifyTestFile(fullname, true, false, testLightenedColor, roundtripToOcad, true, minOcadVersion, maxOcadVersion, MAX_PIXEL_DIFF);
            Assert.IsTrue(ok, string.Format("Rendering test {0} did not compare correctly.", filename), ok);
        }

        void CheckTestNoPatternBitmaps(string filename, bool testLightenedColor, bool roundtripToOcad, int minOcadVersion, int maxOcadVersion)
        {
            string fullname = TestUtil.GetTestFile("skia_render\\" + filename);
            bool ok = RenderingUtil.VerifyTestFile(fullname, false, false, testLightenedColor, roundtripToOcad, false, minOcadVersion, maxOcadVersion, MAX_PIXEL_DIFF);
            Assert.IsTrue(ok, string.Format("Rendering test {0} did not compare correctly.", filename), ok);
        }

#if false

        [Test]
        public void TestWest()
        {
            CheckTest("teanwest.txt", false, true, 9, 12);
        }

        [Test]
        public void TestWest11()
        {
            CheckTest("teanwest11.txt", false, true, 11, 12);
        }
#endif
        [Test]
        public void LineSymbols()
        {
            CheckTest("isomlines.txt", true, true, 6, 12);
            CheckTest("isomlines9.txt", true, true, 6, 12);
        }


        [Test]
        public void Fences()
        {
            CheckTest("fences.txt", false, true, 6, 12);
            CheckTest("fences9.txt", false, true, 6, 12);
        }

        [Test]
        public void ShortFences()
        {
            CheckTest("shortfences.txt", false, false, 9, 9);
        }

        [Test]
        public void FramingLines()
        {
            CheckTest("framingline-test.txt", false, true, 6, 12);
            CheckTest("framingline-test9.txt", false, true, 6, 12);
        }

        [Test]
        public void DashLines()
        {
            CheckTest("dashline.txt", false, true, 6, 12);
            CheckTest("dashline9.txt", false, true, 6, 12);
        }


        [Test]
        public void PointSymbols()
        {
            CheckTest("isompoints.txt", true, true, 6, 12);
            CheckTest("isompoints_9.txt", false, true, 6, 12);
        }

        [Test]
        public void AreaSymbols()
        {
            CheckTest("isomarea.txt", true, true, 6, 12);
            CheckTest("isomarea9.txt", true, true, 6, 12);
        }

        [Test]
        public void AreaHoles()
        {
            CheckTest("holes.txt", false, true, 6, 12);
            CheckTest("holes9.txt", false, true, 6, 12);
        }

        [Test]
        public void CutCircles()
        {
            CheckTest("cutcircles.txt", false, true, 6, 12);
            // CheckTest("cutcircles9.txt", false, false, 6);    OCAD 9 has some strange problems with cut circles...
        }


        [Test]
        public void HiddenSymbols()
        {
            CheckTest("hiddensymbols.txt", false, true, 6, 12);
            CheckTest("hiddensymbols9.txt", false, true, 6, 12);
        }


        [Test]
        public void RotatedAreas()
        {
            CheckTest("rotarea-test.txt", false, true, 6, 12);
            CheckTest("rotarea-test9.txt", false, true, 6, 12);
        }

        [Test]
        public void BorderedAreas()
        {
            CheckTest("borderedarea9.txt", false, true, 9, 12);
        }


        [Test]
        public void TextSymbols()
        {
            CheckTestAntiAlias("simpletext.txt", true, true, 6, 12);
            CheckTestAntiAlias("simpletext9.txt", true, true, 6, 12);
        }

        [Test]
        public void PunchBox()
        {
            //CheckTest("punchbox.txt", false, true, 6, 12);
            CheckTestAntiAlias("punchbox9.txt", false, true, 6, 12);
            CheckTestAntiAlias("punchbox11.txt", false, false, 10, 12);
        }

#if false

        [Test]
        public void LakeSammMap()
        {
            CheckTest("lksamm1.txt", false, true, 6, 12);
            // CheckTest("lksamm2.txt", true, true, 6);   // this one has very slight rendering differences each time. odd...
            CheckTest("lksamm3.txt", false, true, 6, 12);
            CheckTest("lksamm4.txt", false, true, 6, 12);
        }

        [Test]
        public void LakeSammMap9()
        {
            CheckTest("lksamm9_1.txt", false, false, 6, 12);
            CheckTest("lksamm9_1.txt", false, false, 6, 12);
            // CheckTest("lksamm9_2.txt", true, false, 6);  // this one has very slight rendering differences each time. odd...
            CheckTest("lksamm9_3.txt", false, false, 6, 12);
            CheckTest("lksamm9_4.txt", false, false, 6, 12);
        }

        [Test]
        public void LakeSammMap11()
        {
            CheckTest("lksamm11_1.txt", false, false, 6, 12);
            CheckTest("lksamm11_1.txt", false, false, 6, 12);
            // CheckTest("lksamm11_2.txt", true, false, 6);  // this one has very slight rendering differences each time. odd...
            CheckTest("lksamm11_3.txt", false, false, 6, 12);
            CheckTest("lksamm11_4.txt", false, false, 6, 12);
        }

        [Test]
        public void LakeSammMap12()
        {
            CheckTest("lksamm12_1.txt", false, true, 6, 12);
            CheckTest("lksamm12_1.txt", false, true, 6, 12);
            // CheckTest("lksamm11_2.txt", true, false, 6);  // this one has very slight rendering differences each time. odd...
            CheckTest("lksamm12_3.txt", false, true, 6, 12);
            CheckTest("lksamm12_4.txt", false, true, 6, 12);
        }

#endif

        [Test]
        public void DeletedItems()
        {
            CheckTest("deleteditems.txt", false, true, 6, 12);
        }

        [Test]
        public void CornersAndEnds()
        {
            CheckTest("corner_ends.txt", false, true, 6, 12);
            CheckTest("corner_ends9.txt", false, true, 6, 12);
        }

        [Test]
        public void GlyphHoles()
        {
            CheckTest("glyphholes.txt", false, true, 6, 12);
            CheckTest("glyphholes9.txt", false, true, 6, 12);
        }

        [Test]
        public void ZeroGlyph()
        {
            CheckTest("zeroglyph9.txt", false, true, 6, 12);
            CheckTest("zeroglyph6.txt", false, true, 6, 12);
        }

        [Test]
        public void OffsetAreaPattern()
        {
            CheckTest("offsetpattern.txt", false, true, 6, 12);
        }

        [Test]
        public void OffsetAreaPatternNoBitmap()
        {
            CheckTestNoPatternBitmaps("offsetpattern_nopatbm.txt", false, true, 6, 12);
        }

        [Test]
        public void OffsetAreaPatternRotated()
        {
            CheckTest("offsetpatternrot.txt", false, true, 6, 12);
        }

        [Test]
        public void OffsetAreaPatternRotated2()
        {
            CheckTest("offsetpatternrot2.txt", false, true, 6, 12);
        }

        [Test]
        public void OffsetAreaPatternRotatedNoBitmap()
        {
            CheckTestNoPatternBitmaps("offsetpatternrot_nopatbm.txt", false, true, 6, 12);
        }

        [Test]
        public void OffsetAreaPatternRotated2NoBitmap()
        {
            CheckTestNoPatternBitmaps("offsetpatternrot2_nopatbm.txt", false, true, 6, 12);
        }

        [Test]
        public void AreaSymbolsBug()
        {
            CheckTest("isomareabug.txt", false, false, 9, 9);
        }

        [Test]
        public void AreaSymbolsBugNoBitmap()
        {
            CheckTestNoPatternBitmaps("isomareabug_nopatbm.txt", false, false, 9, 9);
        }

        [Test]
        public void AreaSymbolsNoBitmap()
        {
            CheckTestNoPatternBitmaps("isomarea_nopatbm.txt", true, true, 6, 9);
        }

        [Test]
        public void ArialNarrow()
        {
            CheckTestAntiAlias("arialnarrow.txt", false, false, 9, 9);
        }

        [Test]
        public void Cambria()
        {
            CheckTest("Cambria.txt", false, false, 6, 11);
        }

        [Test]
        public void CambriaBold()
        {
            CheckTest("Cambriabold.txt", false, false, 6, 11);
        }



        [Test]
        public void ParaSpacing()
        {
            CheckTestAntiAlias("paraspacing.txt", false, true, 6, 12);
            CheckTestAntiAlias("paraspacing9.txt", false, true, 6, 12);
        }
        
        [Test]
        public void ParaIdent()
        {
            CheckTestAntiAlias("paraindent9.txt", false, true, 6, 12);
            CheckTestAntiAlias("paraindent6.txt", false, true, 6, 12);
        }

        [Test]
        public void NarrowWrap()
        {
            CheckTestAntiAlias("textnarrowwrap.txt", false, true, 6, 12);
        }

        [Test]
        public void CharSpace()
        {
            CheckTestAntiAlias("charspace.txt", false, true, 6, 12);
        }

        [Test]
        public void WordSpace()
        {
            CheckTestAntiAlias("wordspace.txt", false, true, 6, 12);
        }

        [Test]
        public void ComboSpace()
        {
            CheckTestAntiAlias("combospace.txt", false, true, 6, 12);
        }

        [Test]
        public void TopAlignText()
        {
            CheckTestAntiAlias("topaligntext10.txt", false, true, 10, 12);
        }

        [Test]
        public void MidAlignText()
        {
            CheckTestAntiAlias("midaligntext10.txt", false, true, 10, 12);
        }

        [Test]
        public void CenterPointText()
        {
            CheckTestAntiAlias("textpoint10.txt", false, true, 10, 12);
        }

        [Test]
        public void Justify()
        {
            CheckTestAntiAlias("justify.txt", false, true, 6, 12);
        }

        [Test]
        public void TabbedText()
        {
            CheckTestAntiAlias("tabbedtext.txt", false, true, 6, 12);
        }

        [Test]
        public void Newlines()
        {
            CheckTestAntiAlias("newlines.txt", false, true, 6, 12);
        }

        [Test]
        public void UnderlineText()
        {
            CheckTestAntiAlias("underlinetext.txt", false, true, 6, 12);
        }

        [Test]
        public void LineText1()
        {
            CheckTestAntiAlias("linetext_6.txt", false, true, 6, 12);
            CheckTestAntiAlias("linetext_9.txt", false, true, 6, 12);
        }

        [Test]
        public void LineText2()
        {
            CheckTestAntiAlias("linetext2_6.txt", false, true, 6, 12);
            CheckTestAntiAlias("linetext2_9.txt", false, true, 6, 12);
        }

        [Test]
        public void LineTextSpacing()
        {
            CheckTestAntiAlias("linetextspacing.txt", false, true, 6, 12);
        }

        [Test]
        public void AllLineText()
        {
            CheckTestAntiAlias("alllinetext.txt", false, true, 6, 12);
        }

        [Test]
        public void LineTextTop()
        {
            CheckTestAntiAlias("linetext_top.txt", false, true, 10, 12);
        }

        [Test]
        public void LineTextMid()
        {
            CheckTestAntiAlias("linetext_mid.txt", false, true, 10, 12);
        }

        [Test]
        public void LineTextMid2()
        {
            CheckTestAntiAlias("linetext2_mid.txt", false, true, 10, 12);
        }

        [Test]
        public void FramingText1()
        {
            CheckTestAntiAlias("frametext1.txt", false, true, 7, 12);
        }

        [Test]
        public void FramingText2()
        {
            CheckTestAntiAlias("frametext2.txt", false, true, 9, 12);
        }

        [Test]
        public void FramingText3()
        {
            CheckTestAntiAlias("frametext3.txt", false, true, 9, 12);
            CheckTestAntiAlias("frametext3.txt", false, true, 6, 7);
        }

        [Test]
        public void Framing_Ocad6()
        {
            // Not supported in OCAD 8!!! (OCAD 8 didn't have font framing or offset framing
            CheckTestAntiAlias("framing_ocad6.txt", false, true, 6, 7);
            CheckTestAntiAlias("framing_ocad6.txt", false, true, 9, 12);
        }

        [Test]
        public void Framing_Ocad7()
        {
            // Not supported in OCAD 6 or 8!!! (OCAD 8 didn't have font framing or offset framing, OCAD 6 didn't have line framing).
            CheckTestAntiAlias("framing_ocad7.txt", false, true, 7, 7);
            CheckTestAntiAlias("framing_ocad7.txt", false, true, 9, 12);
        }

        [Test]
        public void Framing_Ocad8()
        {
            CheckTestAntiAlias("framing_ocad8.txt", false, true, 7, 12);
        }

        [Test]
        public void DoubleLines()
        {
            CheckTest("doublelines9.txt", false, true, 6, 12);
            CheckTest("doublelines6.txt", false, true, 6, 12);
        }

        [Test]
        public void CutDoubleSides()
        {
            CheckTest("cutdoublesides9.txt", false, true, 6, 12);
            CheckTest("cutdoublesides6.txt", false, true, 6, 12);
        }

        [Test]
        public void CutAreaBorder()
        {
            CheckTest("cutareaborder9.txt", false, true, 9, 12);
        }

        [Test]
        public void EuclideanDashLengths11()
        {
            CheckTest("euclid_dash11.txt", false, true, 11, 12);
        }

        [Test]
        public void BizzarroDashLengths11()
        {
            CheckTest("bizzarro_dash11.txt", false, true, 11, 12);
        }

        [Test]
        public void LineGaps10()
        {
            CheckTest("linegaps10.txt", false, true, 10, 12);
        }

        [Test]
        public void DashLengths()
        {
            CheckTest("dashlengths9.txt", false, true, 6, 12);
            CheckTest("dashlengths6.txt", false, true, 6, 12);
        }

        [Test]
        public void AngleDashes()
        {
            CheckTestAntiAlias("angledashes9.txt", false, true, 6, 12);
        }

        [Test]
        public void DoubleDashLengths()
        {
            CheckTest("dbldashlengths9.txt", false, true, 7, 12);  // OCAD 6 doesn't support dash points.
            CheckTest("dbldashlengths7.txt", false, true, 7, 12);  // OCAD 6 doesn't support dash points.
        }

        [Test]
        public void SecGapOnly()
        {
            CheckTest("secgaponly9.txt", false, true, 6, 12);
            CheckTest("secgaponly6.txt", false, true, 6, 12);
        }

        [Test]
        public void PointyEnds()
        {
            CheckTest("pointyends9.txt", false, true, 6, 12);
            CheckTest("pointyends6.txt", false, true, 6, 12);
        }

        [Test]
        public void BadPointyEnds()
        {
            CheckTest("badpointyends.txt", false, true, 6, 12);
        }

        [Test]
        public void Glyphs()
        {
            CheckTest("glyphs9.txt", false, true, 6, 12);
        }

        [Test]
        public void MainGlyphs()
        {
            CheckTest("mainglyph9.txt", false, true, 7, 12);
            CheckTest("mainglyph7.txt", false, true, 7, 12);
        }

        [Test]
        public void Max2Glyphs()
        {
            CheckTestAntiAlias("max2glyph9.txt", false, true, 7, 12);
            CheckTestAntiAlias("max2glyph7.txt", false, true, 7, 12);
        }

        [Test]
        public void MultiMainGlyphs()
        {
            CheckTest("multimainglyph9.txt", false, true, 7, 12);
            CheckTest("multimainglyph7.txt", false, true, 7, 12);
        }

        [Test]
        public void SecondaryGlyphs()
        {
            CheckTestAntiAlias("secglyph9.txt", false, true, 7, 12);
            CheckTestAntiAlias("secglyph7.txt", false, true, 7, 12);
        }

        [Test]
        public void StartEndGlyph()
        {
            CheckTest("startendglyph9.txt", false, true, 7, 12);
            CheckTest("startendglyph7.txt", false, true, 7, 12);
        }

        [Test]
        public void CornerGlyphs()
        {
            CheckTest("cornerglyphs9.txt", false, true, 6, 12);
            CheckTest("cornerglyphs6.txt", false, true, 6, 12);
        }

        [Test]
        public void DecreaseSymbols()
        {
            CheckTestAntiAlias("decreasesymbols.txt", false, true, 6, 12);
            CheckTestAntiAlias("decreasesymbols6.txt", false, true, 6, 12);
        }

        [Test]
        public void GraphicsObjects()
        {
            CheckTest("graphicobjects9.txt", true, true, 9, 12);
        }

        [Test]
        public void ImageObjects()
        {
            CheckTest("aiimport.txt", true, true, 9, 12);
        }

        [Test]
        public void Clouds()
        {
            CheckTestAntiAlias("Clouds.txt", false, true, 7, 12);
        }

        [Test]
        public void Clouds11()
        {
            CheckTestAntiAlias("Clouds11.txt", false, false, 11, 12);
        }

#if false
        [Test]
        public void LordHill()
        {
            CheckTest("LordHill.txt", false, false, 6, 12);
        }

        [Test]
        public void LordHill11()
        {
            CheckTest("LordHill11.txt", false, false, 11, 12);
        }
#endif

        [Test]
        public void MissingColor()
        {
            CheckTest("missingcolor.txt", false, false, 6, 12);
        }

        [Test]
        public void Decrease()
        {
            CheckTest("decrease.txt", false, false, 6, 12);
        }

        [Test]
        public void OddFenceEnds()
        {
            CheckTest("oddfenceends.txt", false, false, 6, 12);
        }

#if false
        [Test]
        public void Marymoor()
        {
            CheckTest("marymoor.txt", false, true, 7, 12);
        }

        [Test]
        public void Marymoor11()
        {
            CheckTest("marymoor11.txt", false, false, 11, 12);
        }
#endif
        [Test]
        public void LayoutObjects()
        {
            CheckTest("layout_objects11.txt", true, true, 11, 12);
        }

        [Test]
        public void LayoutObjects12()
        {
            CheckTest("layout_objects12.txt", true, false, 11, 12);
        }

        [Test]
        public void LayoutBitmapObjects()
        {
            CheckTest("layoutbitmap11.txt", true, true, 11, 12);
        }

        [Test]
        public void TemplateRendering()
        {
            CheckTest("template.txt", true, false, 9, 12);
        }

        [Test]
        public void TemplateRendering2()
        {
            CheckTest("template2.txt", true, false, 9, 12);
        }

        [Test]
        public void RecursiveTemplate()
        {
            CheckTest("recursivetempl.txt", false, false, 9, 12);
        }

        [Test]
        public void HiddenTemplateRendering()
        {
            CheckTest("templatehide.txt", true, false, 9, 12);
        }

        [Test]
        public void TemplateFraction1()
        {
            CheckTest("template_fraction1.txt", false, true, 9, 12);
        }

        [Test]
        public void TemplateFraction2()
        {
            CheckTest("template_fraction2.txt", false, true, 9, 12);
        }

        [Test]
        public void TestLogo()
        {
            CheckTest("testlogo.txt", false, false, 8, 8);
        }

        [Test]
        public void Ocad11Align()
        {
            CheckTestAntiAlias("ocad11templatealign.txt", false, false, 11, 12);
        }

        [Test]
        public void PunchBoxBug()
        {
            CheckTest("punchboxbug.txt", false, true, 9, 12);
        }

        [Test]
        public void SmallRectangleSymbols()
        {
            CheckTest("rectanglesymbols.txt", false, true, 6, 12);
        }

#if false
        [Test]
        public void PenistoneHill()
        {
            CheckTest("penistonehill.txt", false, false, 7, 7);
        }

        [Test]
        public void FtCasey()
        {
            CheckTest("FtCasey.txt", false, false, 7, 7);
        }

        [Test]
        public void NorthArrowEnds()
        {
            CheckTest("MagNorthArrowBug.txt", false, false, 11, 12);
        }

        [Test]
        public void DifferentNewlineTypes()
        {
            CheckTest("differentnewlinetypes.txt", false, false, 7, 7);
        }
#endif

/*
[Test]
public void Overprinting()
{
    CheckTestOverprinting("ocad11overprinting.txt", true, true, 6, 12);
}
*/

#if false
        [Test]
        public void KernTextOutline()
        {
            CheckTest("kern_text_outline.txt", false, true, 7, 12);
        }
#endif
    }

}
