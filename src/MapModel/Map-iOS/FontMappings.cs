﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace PurplePen.MapModel
{
    // iOS is very slow doing lookups from font name to postscript font name. This class
    // has the mappings for all fonts included in iOS 7.
    public static class FontMappings
    {
        public static void AddFontMappings(ConcurrentDictionary<string, string> fontDictionary)
        {
            for (int i = 0; i < fontMappings.Length; i += 2) {
                fontDictionary.TryAdd(fontMappings[i], fontMappings[i + 1]);
            }
        }

        static string[] fontMappings = {
            "Academy Engraved LET", "AcademyEngravedLetPlain",
            "Academy Engraved LET Plain:1.0", "AcademyEngravedLetPlain",
            "Al Nile", "AlNile",
            "Al Nile Bold", "AlNile-Bold",
            "American Typewriter", "AmericanTypewriter",
            "American Typewriter Bold", "AmericanTypewriter-Bold",
            "American Typewriter Condensed", "AmericanTypewriter-Condensed",
            "American Typewriter Condensed Bold", "AmericanTypewriter-CondensedBold",
            "American Typewriter Condensed Light", "AmericanTypewriter-CondensedLight",
            "American Typewriter Light", "AmericanTypewriter-Light",
            "Apple Color Emoji", "AppleColorEmoji",
            "Apple SD Gothic Neo", "AppleSDGothicNeo-Regular",
            "Apple SD Gothic Neo Bold", "AppleSDGothicNeo-Bold",
            "Apple SD Gothic Neo Light", "AppleSDGothicNeo-Light",
            "Apple SD Gothic Neo Medium", "AppleSDGothicNeo-Medium",
            "Apple SD Gothic Neo Regular", "AppleSDGothicNeo-Regular",
            "Apple SD Gothic Neo SemiBold", "AppleSDGothicNeo-SemiBold",
            "Apple SD Gothic Neo Thin", "AppleSDGothicNeo-Thin",
            "AppleGothic", "AppleGothic",
            "AppleGothic Regular", "AppleGothic",
            "Arial", "ArialMT",
            "Arial Bold", "Arial-BoldMT",
            "Arial Bold Italic", "Arial-BoldItalicMT",
            "Arial Hebrew", "ArialHebrew",
            "Arial Hebrew Bold", "ArialHebrew-Bold",
            "Arial Hebrew Light", "ArialHebrew-Light",
            "Arial Italic", "Arial-ItalicMT",
            "Arial Rounded MT Bold", "ArialRoundedMTBold",
            "Avenir", "Avenir-Roman",
            "Avenir Black", "Avenir-Black",
            "Avenir Black Oblique", "Avenir-BlackOblique",
            "Avenir Book", "Avenir-Book",
            "Avenir Book Oblique", "Avenir-BookOblique",
            "Avenir Heavy", "Avenir-Heavy",
            "Avenir Heavy Oblique", "Avenir-HeavyOblique",
            "Avenir Light", "Avenir-Light",
            "Avenir Light Oblique", "Avenir-LightOblique",
            "Avenir Medium", "Avenir-Medium",
            "Avenir Medium Oblique", "Avenir-MediumOblique",
            "Avenir Next", "AvenirNext-Regular",
            "Avenir Next Bold", "AvenirNext-Bold",
            "Avenir Next Bold Italic", "AvenirNext-BoldItalic",
            "Avenir Next Condensed", "AvenirNextCondensed-Regular",
            "Avenir Next Condensed Bold", "AvenirNextCondensed-Bold",
            "Avenir Next Condensed Bold Italic", "AvenirNextCondensed-BoldItalic",
            "Avenir Next Condensed Demi Bold", "AvenirNextCondensed-DemiBold",
            "Avenir Next Condensed Demi Bold Italic", "AvenirNextCondensed-DemiBoldItalic",
            "Avenir Next Condensed Heavy", "AvenirNextCondensed-Heavy",
            "Avenir Next Condensed Heavy Italic", "AvenirNextCondensed-HeavyItalic",
            "Avenir Next Condensed Italic", "AvenirNextCondensed-Italic",
            "Avenir Next Condensed Medium", "AvenirNextCondensed-Medium",
            "Avenir Next Condensed Regular", "AvenirNextCondensed-Regular",
            "Avenir Next Condensed Ultra Light", "AvenirNextCondensed-UltraLight",
            "Avenir Next Condensed Ultra Light Italic", "AvenirNextCondensed-UltraLightItalic",
            "Avenir Next Demi Bold", "AvenirNext-DemiBold",
            "Avenir Next Demi Bold Italic", "AvenirNext-DemiBoldItalic",
            "Avenir Next Heavy", "AvenirNext-Heavy",
            "Avenir Next Heavy Italic", "AvenirNext-HeavyItalic",
            "Avenir Next Italic", "AvenirNext-Italic",
            "Avenir Next Medium", "AvenirNext-Medium",
            "Avenir Next Medium Condensed Italic", "AvenirNextCondensed-MediumItalic",
            "Avenir Next Medium Italic", "AvenirNext-MediumItalic",
            "Avenir Next Regular", "AvenirNext-Regular",
            "Avenir Next Ultra Light Italic", "AvenirNext-UltraLightItalic",
            "Avenir Oblique", "Avenir-Oblique",
            "Avenir Roman", "Avenir-Roman",
            "AvenirNext-UltraLight", "AvenirNext-UltraLight",
            "Bangla Sangam MN", "BanglaSangamMN",
            "Bangla Sangam MN Bold", "BanglaSangamMN-Bold",
            "Baskerville", "Baskerville",
            "Baskerville Bold", "Baskerville-Bold",
            "Baskerville Bold Italic", "Baskerville-BoldItalic",
            "Baskerville Italic", "Baskerville-Italic",
            "Baskerville SemiBold", "Baskerville-SemiBold",
            "Baskerville SemiBold Italic", "Baskerville-SemiBoldItalic",
            "Bodoni 72", "BodoniSvtyTwoITCTT-Book",
            "Bodoni 72 Bold", "BodoniSvtyTwoITCTT-Bold",
            "Bodoni 72 Book", "BodoniSvtyTwoITCTT-Book",
            "Bodoni 72 Book Italic", "BodoniSvtyTwoITCTT-BookIta",
            "Bodoni 72 Oldstyle", "BodoniSvtyTwoOSITCTT-Book",
            "Bodoni 72 Oldstyle Bold", "BodoniSvtyTwoOSITCTT-Bold",
            "Bodoni 72 Oldstyle Book", "BodoniSvtyTwoOSITCTT-Book",
            "Bodoni 72 Oldstyle Book Italic", "BodoniSvtyTwoOSITCTT-BookIt",
            "Bodoni 72 Smallcaps", "BodoniSvtyTwoSCITCTT-Book",
            "Bodoni 72 Smallcaps Book", "BodoniSvtyTwoSCITCTT-Book",
            "Bodoni Ornaments", "BodoniOrnamentsITCTT",
            "Bradley Hand", "BradleyHandITCTT-Bold",
            "Bradley Hand Bold", "BradleyHandITCTT-Bold",
            "Chalkboard SE", "ChalkboardSE-Regular",
            "Chalkboard SE Bold", "ChalkboardSE-Bold",
            "Chalkboard SE Light", "ChalkboardSE-Light",
            "Chalkboard SE Regular", "ChalkboardSE-Regular",
            "Chalkduster", "Chalkduster",
            "Cochin", "Cochin",
            "Cochin Bold", "Cochin-Bold",
            "Cochin Bold Italic", "Cochin-BoldItalic",
            "Cochin Italic", "Cochin-Italic",
            "Copperplate", "Copperplate",
            "Copperplate Bold", "Copperplate-Bold",
            "Copperplate Light", "Copperplate-Light",
            "Courier", "Courier",
            "Courier Bold", "Courier-Bold",
            "Courier Bold Oblique", "Courier-BoldOblique",
            "Courier New", "CourierNewPSMT",
            "Courier New Bold", "CourierNewPS-BoldMT",
            "Courier New Bold Italic", "CourierNewPS-BoldItalicMT",
            "Courier New Italic", "CourierNewPS-ItalicMT",
            "Courier Oblique", "Courier-Oblique",
            "Damascus", "Damascus",
            "Damascus Bold", "DamascusBold",
            "Damascus Medium", "DamascusMedium",
            "Damascus Semi Bold", "DamascusSemiBold",
            "DB LCD Temp", "DBLCDTempBlack",
            "DB LCD Temp Black", "DBLCDTempBlack",
            "Devanagari Sangam MN", "DevanagariSangamMN",
            "Devanagari Sangam MN Bold", "DevanagariSangamMN-Bold",
            "Didot", "Didot",
            "Didot Bold", "Didot-Bold",
            "Didot Italic", "Didot-Italic",
            "DIN Alternate", "DINAlternate-Bold",
            "DIN Alternate Bold", "DINAlternate-Bold",
            "DIN Condensed", "DINCondensed-Bold",
            "DIN Condensed Bold", "DINCondensed-Bold",
            "Diwan Mishafi", "DiwanMishafi",
            "Euphemia UCAS", "EuphemiaUCAS",
            "Euphemia UCAS Bold", "EuphemiaUCAS-Bold",
            "Euphemia UCAS Italic", "EuphemiaUCAS-Italic",
            "Farah", "Farah",
            "Futura", "Futura-Medium",
            "Futura Condensed ExtraBold", "Futura-CondensedExtraBold",
            "Futura Condensed Medium", "Futura-CondensedMedium",
            "Futura Medium", "Futura-Medium",
            "Futura Medium Italic", "Futura-MediumItalic",
            "Geeza Pro", "GeezaPro",
            "Geeza Pro Bold", "GeezaPro-Bold",
            "Geeza Pro Light", "GeezaPro-Light",
            "Georgia", "Georgia",
            "Georgia Bold", "Georgia-Bold",
            "Georgia Bold Italic", "Georgia-BoldItalic",
            "Georgia Italic", "Georgia-Italic",
            "Gill Sans", "GillSans",
            "Gill Sans Bold", "GillSans-Bold",
            "Gill Sans Bold Italic", "GillSans-BoldItalic",
            "Gill Sans Italic", "GillSans-Italic",
            "Gill Sans Light", "GillSans-Light",
            "Gill Sans Light Italic", "GillSans-LightItalic",
            "Gujarati Sangam MN", "GujaratiSangamMN",
            "Gujarati Sangam MN Bold", "GujaratiSangamMN-Bold",
            "Gurmukhi MN", "GurmukhiMN",
            "Gurmukhi MN Bold", "GurmukhiMN-Bold",
            "Heiti SC", "STHeitiSC-Light",
            "Heiti SC Light", "STHeitiSC-Light",
            "Heiti SC Medium", "STHeitiSC-Medium",
            "Heiti TC", "STHeitiTC-Light",
            "Heiti TC Light", "STHeitiTC-Light",
            "Heiti TC Medium", "STHeitiTC-Medium",
            "Helvetica", "Helvetica",
            "Helvetica Bold", "Helvetica-Bold",
            "Helvetica Bold Oblique", "Helvetica-BoldOblique",
            "Helvetica Light", "Helvetica-Light",
            "Helvetica Light Oblique", "Helvetica-LightOblique",
            "Helvetica Neue", "HelveticaNeue",
            "Helvetica Neue Bold", "HelveticaNeue-Bold",
            "Helvetica Neue Bold Italic", "HelveticaNeue-BoldItalic",
            "Helvetica Neue Condensed Black", "HelveticaNeue-CondensedBlack",
            "Helvetica Neue Condensed Bold", "HelveticaNeue-CondensedBold",
            "Helvetica Neue Italic", "HelveticaNeue-Italic",
            "Helvetica Neue Light", "HelveticaNeue-Light",
            "Helvetica Neue Light Italic", "HelveticaNeue-LightItalic",
            "Helvetica Neue Medium", "HelveticaNeue-Medium",
            "Helvetica Neue Medium Italic", "HelveticaNeue-MediumItalic",
            "Helvetica Neue Thin", "HelveticaNeue-Thin",
            "Helvetica Neue Thin Italic", "HelveticaNeue-ThinItalic",
            "Helvetica Neue UltraLight", "HelveticaNeue-UltraLight",
            "Helvetica Neue UltraLight Italic", "HelveticaNeue-UltraLightItalic",
            "Helvetica Oblique", "Helvetica-Oblique",
            "Hiragino Kaku Gothic ProN", "HiraKakuProN-W3",
            "Hiragino Kaku Gothic ProN W3", "HiraKakuProN-W3",
            "Hiragino Kaku Gothic ProN W6", "HiraKakuProN-W6",
            "Hiragino Mincho ProN", "HiraMinProN-W3",
            "Hiragino Mincho ProN W3", "HiraMinProN-W3",
            "Hiragino Mincho ProN W6", "HiraMinProN-W6",
            "Hoefler Text", "HoeflerText-Regular",
            "Hoefler Text Black", "HoeflerText-Black",
            "Hoefler Text Black Italic", "HoeflerText-BlackItalic",
            "Hoefler Text Italic", "HoeflerText-Italic",
            "Iowan Old Style", "IowanOldStyle-Roman",
            "Iowan Old Style Bold", "IowanOldStyle-Bold",
            "Iowan Old Style Bold Italic", "IowanOldStyle-BoldItalic",
            "Iowan Old Style Italic", "IowanOldStyle-Italic",
            "Iowan Old Style Roman", "IowanOldStyle-Roman",
            "Kailasa", "Kailasa",
            "Kailasa Bold", "Kailasa-Bold",
            "Kailasa Regular", "Kailasa",
            "Kannada Sangam MN", "KannadaSangamMN",
            "Kannada Sangam MN Bold", "KannadaSangamMN-Bold",
            "Malayalam Sangam MN", "MalayalamSangamMN",
            "Malayalam Sangam MN Bold", "MalayalamSangamMN-Bold",
            "Marion", "Marion-Regular",
            "Marion Bold", "Marion-Bold",
            "Marion Italic", "Marion-Italic",
            "Marion Regular", "Marion-Regular",
            "Marker Felt", "MarkerFelt-Thin",
            "Marker Felt Thin", "MarkerFelt-Thin",
            "Marker Felt Wide", "MarkerFelt-Wide",
            "Menlo", "Menlo-Regular",
            "Menlo Bold", "Menlo-Bold",
            "Menlo Bold Italic", "Menlo-BoldItalic",
            "Menlo Italic", "Menlo-Italic",
            "Menlo Regular", "Menlo-Regular",
            "Mishafi", "DiwanMishafi",
            "Noteworthy", "Noteworthy-Light",
            "Noteworthy Bold", "Noteworthy-Bold",
            "Noteworthy Light", "Noteworthy-Light",
            "Optima", "Optima-Regular",
            "Optima Bold", "Optima-Bold",
            "Optima Bold Italic", "Optima-BoldItalic",
            "Optima ExtraBlack", "Optima-ExtraBlack",
            "Optima Italic", "Optima-Italic",
            "Optima Regular", "Optima-Regular",
            "Oriya Sangam MN", "OriyaSangamMN",
            "Oriya Sangam MN Bold", "OriyaSangamMN-Bold",
            "Palatino", "Palatino-Roman",
            "Palatino Bold", "Palatino-Bold",
            "Palatino Bold Italic", "Palatino-BoldItalic",
            "Palatino Italic", "Palatino-Italic",
            "Papyrus", "Papyrus",
            "Papyrus Condensed", "Papyrus-Condensed",
            "Party LET", "PartyLetPlain",
            "Party LET Plain", "PartyLetPlain",
            "Savoye LET", "SavoyeLetPlain",
            "Savoye LET Plain:1.0", "SavoyeLetPlain",
            "Sinhala Sangam MN", "SinhalaSangamMN",
            "Sinhala Sangam MN Bold", "SinhalaSangamMN-Bold",
            "Snell Roundhand", "SnellRoundhand",
            "Snell Roundhand Black", "SnellRoundhand-Black",
            "Snell Roundhand Bold", "SnellRoundhand-Bold",
            "Superclarendon", "Superclarendon-Light",
            "Superclarendon Black", "Superclarendon-Black",
            "Superclarendon Black Italic", "Superclarendon-BlackItalic",
            "Superclarendon Bold", "Superclarendon-Bold",
            "Superclarendon Bold Italic", "Superclarendon-BoldItalic",
            "Superclarendon Italic", "Superclarendon-Italic",
            "Superclarendon Light", "Superclarendon-Light",
            "Superclarendon Light Italic", "Superclarendon-LightItalic",
            "Superclarendon Regular", "Superclarendon-Regular",
            "Symbol", "Symbol",
            "Tamil Sangam MN", "TamilSangamMN",
            "Tamil Sangam MN Bold", "TamilSangamMN-Bold",
            "Telugu Sangam MN", "TeluguSangamMN",
            "Telugu Sangam MN Bold", "TeluguSangamMN-Bold",
            "Thonburi", "Thonburi",
            "Thonburi Bold", "Thonburi-Bold",
            "Thonburi Light", "Thonburi-Light",
            "Times New Roman", "TimesNewRomanPSMT",
            "Times New Roman Bold", "TimesNewRomanPS-BoldMT",
            "Times New Roman Bold Italic", "TimesNewRomanPS-BoldItalicMT",
            "Times New Roman Italic", "TimesNewRomanPS-ItalicMT",
            "Trebuchet MS", "TrebuchetMS",
            "Trebuchet MS Bold", "TrebuchetMS-Bold",
            "Trebuchet MS Bold Italic", "Trebuchet-BoldItalic",
            "Trebuchet MS Italic", "TrebuchetMS-Italic",
            "Verdana", "Verdana",
            "Verdana Bold", "Verdana-Bold",
            "Verdana Bold Italic", "Verdana-BoldItalic",
            "Verdana Italic", "Verdana-Italic",
            "Zapf Dingbats", "ZapfDingbatsITC",
            "Zapfino", "Zapfino",

        };
    }
}

