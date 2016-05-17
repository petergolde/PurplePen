/* Copyright (c) 2006-2008, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

#if TEST

using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.Tests
{
    [TestClass]
    public class DescriptionLocalizeTests
    {
        [TestMethod]
        public void AddLanguage()
        {
            File.Delete(TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));
            File.Copy(TestUtil.GetTestFile("desclocalize\\symbols.xml"), TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));

            SymbolDB symbolDB = new SymbolDB(TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));
            DescriptionLocalize localizer = new DescriptionLocalize(symbolDB);

            localizer.AddLanguage(new SymbolLanguage("Dutch", "nl", false, false, false, null, false, null), "en");
            localizer.AddLanguage(new SymbolLanguage("Korean", "ko", true, false, true, new string[] { "masculine", "feminine" }, false, null), "en");

            SymbolLanguage[] expected = {
                                            new SymbolLanguage("English", "en", true, false, false, null, false, null),
                                            new SymbolLanguage("Francais", "fr", true, true, true, new string[] { "masculine", "feminine" }, false, null),
                                            new SymbolLanguage("Deutsch", "de", true, true, true, new string[] {"masculine", "feminine", "neuter"}, false, null),
                                            new SymbolLanguage("Dutch", "nl", false, false, false, null, false, null),
                                            new SymbolLanguage("Korean", "ko", true, false, true, new string[] { "masculine", "feminine" }, false, null)};

            List<SymbolLanguage> languages = new List<SymbolLanguage>(symbolDB.AllLanguages);

            CollectionAssert.AreEquivalent(expected, languages);

            SymbolText[] expectedTerraces = {
                new SymbolText() {Lang = "en", Plural = false, Gender = "", Text = "terrace"},
                new SymbolText() {Lang = "en", Plural = true, Gender = "", Text = "terraces"},
                new SymbolText() {Lang = "nl", Plural = false, Gender = "", Text = "terrace"},
                new SymbolText() {Lang = "nl", Plural = true, Gender = "", Text = "terraces"},
                new SymbolText() {Lang = "ko", Plural = false, Gender = "", Text = "terrace"},
                new SymbolText() {Lang = "ko", Plural = true, Gender = "", Text = "terraces"},
            };
            CollectionAssert.AreEquivalent(expectedTerraces, symbolDB["1.1"].SymbolTexts);

            Assert.AreEqual("Terrace", symbolDB["1.1"].GetName("nl"));
            Assert.AreEqual("Terrace", symbolDB["1.1"].GetName("ko"));
        }

        [TestMethod]
        public void ReplaceLanguage()
        {
            File.Delete(TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));
            File.Copy(TestUtil.GetTestFile("desclocalize\\symbols.xml"), TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));

            SymbolDB symbolDB = new SymbolDB(TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));
            DescriptionLocalize localizer = new DescriptionLocalize(symbolDB);

            localizer.AddLanguage(new SymbolLanguage("Anglais", "en", true, false, true, new string[] { "male", "female" }, false, null), "en");
            localizer.AddLanguage(new SymbolLanguage("Korean", "ko", true, false, true, new string[] { "masculine", "feminine" }, false, null), "en");
            localizer.AddLanguage(new SymbolLanguage("Korean", "ko", false, false, false, null, false, null), "en");

            SymbolLanguage[] expected = {
                                            new SymbolLanguage("Anglais", "en", true, false, true, new string[] { "male", "female" }, false, null),
                                            new SymbolLanguage("Francais", "fr", true, true, true, new string[] { "masculine", "feminine" }, false, null),
                                            new SymbolLanguage("Deutsch", "de", true, true, true, new string[] {"masculine", "feminine", "neuter"}, false, null),
                                            new SymbolLanguage("Korean", "ko", false, false, false, null, false, null) };

            List<SymbolLanguage> languages = new List<SymbolLanguage>(symbolDB.AllLanguages);

            CollectionAssert.AreEquivalent(expected, languages);
        }

        [TestMethod]
        public void AddSymbolText()
        {
            File.Delete(TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));
            File.Copy(TestUtil.GetTestFile("desclocalize\\symbols.xml"), TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));

            SymbolDB symbolDB = new SymbolDB(TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));
            DescriptionLocalize localizer = new DescriptionLocalize(symbolDB);

            Dictionary<string, List<SymbolText>> dictionary = new Dictionary<string, List<SymbolText>>();

            dictionary["1.1"] = new List<SymbolText>()
            {
                new SymbolText() {Lang = "de", Plural = false, Gender = "masculine", Text = "german-terrace"},
                new SymbolText() {Lang = "de", Plural = true, Gender = "masculine", Text = "german-terraces"},
                new SymbolText() {Lang = "fr", Plural = false, Gender = "feminine", Text = "french-terrace"},
                new SymbolText() {Lang = "fr", Plural = true, Gender = "feminine", Text = "french-terraces"},
            };
            dictionary["5.20"] = new List<SymbolText>()
            {
                new SymbolText() {Lang = "de", Plural = false, Gender = "neuter", Text = "german-statue"},
                new SymbolText() {Lang = "de", Plural = true, Gender = "neuter", Text = "german-statues"},
            };

            localizer.CustomizeDescriptionTexts(dictionary);

            SymbolText[] expectedTerraces = {
                new SymbolText() {Lang = "en", Plural = false, Gender = "", Text = "terrace"},
                new SymbolText() {Lang = "en", Plural = true, Gender = "", Text = "terraces"},
                new SymbolText() {Lang = "de", Plural = false, Gender = "masculine", Text = "german-terrace"},
                new SymbolText() {Lang = "de", Plural = true, Gender = "masculine", Text = "german-terraces"},
                new SymbolText() {Lang = "fr", Plural = false, Gender = "feminine", Text = "french-terrace"},
                new SymbolText() {Lang = "fr", Plural = true, Gender = "feminine", Text = "french-terraces"}
            };
            SymbolText[] expectedStatues = {
                new SymbolText() {Lang = "en", Plural = false, Gender = "", Text = "statue"},
                new SymbolText() {Lang = "en", Plural = true, Gender = "", Text = "statues"},
                new SymbolText() {Lang = "de", Plural = false, Gender = "neuter", Text = "german-statue"},
                new SymbolText() {Lang = "de", Plural = true, Gender = "neuter", Text = "german-statues"},
            };

            CollectionAssert.AreEquivalent(expectedTerraces, symbolDB["1.1"].SymbolTexts);
            CollectionAssert.AreEquivalent(expectedStatues, symbolDB["5.20"].SymbolTexts);
        }

        [TestMethod]
        public void ChangeSymbolText()
        {
            File.Delete(TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));
            File.Copy(TestUtil.GetTestFile("desclocalize\\symbols.xml"), TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));

            SymbolDB symbolDB = new SymbolDB(TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));
            DescriptionLocalize localizer = new DescriptionLocalize(symbolDB);

            Dictionary<string, List<SymbolText>> dictionary = new Dictionary<string, List<SymbolText>>();

            dictionary["1.3"] = new List<SymbolText>()
            {
                new SymbolText() {Lang = "en", Plural = false, Gender = "", Text = "sloping valley thing"},
            };

            localizer.CustomizeDescriptionTexts(dictionary);

            SymbolText[] expectedReentrants = {
                new SymbolText() {Lang = "en", Plural = false, Gender = "", Text = "sloping valley thing"}
            };

            CollectionAssert.AreEquivalent(expectedReentrants, symbolDB["1.3"].SymbolTexts);
        }

        [TestMethod]
        public void AddName()
        {
            File.Delete(TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));
            File.Copy(TestUtil.GetTestFile("desclocalize\\symbols.xml"), TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));

            SymbolDB symbolDB = new SymbolDB(TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));
            DescriptionLocalize localizer = new DescriptionLocalize(symbolDB);

            Dictionary<string, List<SymbolText>> dictionary = new Dictionary<string, List<SymbolText>>();

            dictionary["1.1"] = new List<SymbolText>()
            {
                new SymbolText() {Lang = "de", Text = "GermanTerrace"},
                new SymbolText() {Lang = "fr", Text = "FrenchTerrace"},
            };

            localizer.CustomizeDescriptionNames(dictionary);

            Assert.AreEqual("Terrace", symbolDB["1.1"].GetName("en"));
            Assert.AreEqual("GermanTerrace", symbolDB["1.1"].GetName("de"));
            Assert.AreEqual("FrenchTerrace", symbolDB["1.1"].GetName("fr"));
        }



        [TestMethod]
        public void ReplaceName()
        {
            File.Delete(TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));
            File.Copy(TestUtil.GetTestFile("desclocalize\\symbols.xml"), TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));

            SymbolDB symbolDB = new SymbolDB(TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));
            DescriptionLocalize localizer = new DescriptionLocalize(symbolDB);

            Dictionary<string, List<SymbolText>> dictionary = new Dictionary<string, List<SymbolText>>();

            dictionary["5.20"] = new List<SymbolText>()
            {
                new SymbolText() {Lang = "de", Text = "New German Statue"},
                new SymbolText() {Lang = "en", Text = "New English Statue"},
            };

            localizer.CustomizeDescriptionNames(dictionary);

            Assert.AreEqual("New English Statue", symbolDB["5.20"].GetName("en"));
            Assert.AreEqual("New German Statue", symbolDB["5.20"].GetName("de"));
        }

        [TestMethod]
        public void Merge()
        {
            File.Delete(TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));
            File.Copy(TestUtil.GetTestFile("desclocalize\\symbols.xml"), TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));

            SymbolDB symbolDB = new SymbolDB(TestUtil.GetTestFile("desclocalize\\symbols_working.xml"));
            DescriptionLocalize localizer = new DescriptionLocalize(symbolDB);

            localizer.MergeSymbolsFile(TestUtil.GetTestFile("desclocalize\\symbols-to-merge.xml"), "fr");

            SymbolLanguage[] expected = {
                                            new SymbolLanguage("English", "en", true, false, false, null, false, null),
                                            new SymbolLanguage("French", "fr", true, true, true, new string[] { "masculine", "feminine" }, false, null),
                                            new SymbolLanguage("Deutsch", "de", true, true, true, new string[] {"masculine", "feminine", "neuter"}, false, null)};

            List<SymbolLanguage> languages = new List<SymbolLanguage>(symbolDB.AllLanguages);

            CollectionAssert.AreEquivalent(expected, languages);

            SymbolText[] expectedTexts = {
                new SymbolText() {Lang = "en", Plural = false, Gender = "", Text = "Length {0}, climb {1}"},
                new SymbolText() {Lang = "fr", Plural = false, Gender = "feminine", Text = "Frenchy Length {0}, climb {1}"},
            };
            List<SymbolText> actualTexts = symbolDB["course_length_climb"].SymbolTexts;

            CollectionAssert.AreEquivalent(expectedTexts, actualTexts);

            expectedTexts = new SymbolText[] {
                new SymbolText() {Lang = "en", Plural = false, Gender = "", Text = "overgrown {0}"},
                new SymbolText() {Lang = "fr", Plural = false, Gender = "feminine", Text = "fr sing fem overgrown {0}"},
                new SymbolText() {Lang = "fr", Plural = true, Gender = "feminine", Text = "fr plur fem overgrown {0}"},
                new SymbolText() {Lang = "fr", Plural = false, Gender = "masculine", Text = "fr sing masc overgrown {0}"},
                new SymbolText() {Lang = "fr", Plural = true, Gender = "masculine", Text = "fr plur masc overgrown {0}"},
            };
            actualTexts = symbolDB["8.4"].SymbolTexts;

            CollectionAssert.AreEquivalent(expectedTexts, actualTexts);

            Assert.AreEqual("Fr Overgrown", symbolDB["8.4"].GetName("fr"));

        }

    }
}
#endif
