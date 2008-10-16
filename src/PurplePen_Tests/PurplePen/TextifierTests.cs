/* Copyright (c) 2006-2007, Peter Golde
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
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestingUtils;

namespace PurplePen.Tests
{

    [TestClass]
    public class TextifierTests: TestFixtureBase
    {
        UndoMgr undomgr;
        EventDB eventDB;
        SymbolDB symbolDB;

        public void Setup()
        {
            undomgr = new UndoMgr(5);
            eventDB = new EventDB(undomgr);
            symbolDB = new SymbolDB(Util.GetFileInAppDirectory("symbols.xml"));
            eventDB.Load(TestUtil.GetTestFile("textifier\\sampleevent1.coursescribe"));
            eventDB.Validate();
        }

        [TestMethod]
        public void Capitalize()
        {
            Setup();
            Assert.AreEqual(null, Textifier.CapitalizeFirstLetter(null));
            Assert.AreEqual("", Textifier.CapitalizeFirstLetter(""));
            Assert.AreEqual("A", Textifier.CapitalizeFirstLetter("a"));
            Assert.AreEqual("Z", Textifier.CapitalizeFirstLetter("Z"));
            Assert.AreEqual("Étude", Textifier.CapitalizeFirstLetter("étude"));
            Assert.AreEqual("Hi there", Textifier.CapitalizeFirstLetter("hi there"));
        }

        [TestMethod]
        public void Normal()
        {
            Setup();
            Textifier textifier = new Textifier(eventDB, symbolDB, "en");

            Assert.AreEqual("Upper boulder, 2m high", textifier.CreateTextForControl(ControlId(2), ""));

            Assert.AreEqual("Overgrown pit, 3m deep", textifier.CreateTextForControl(ControlId(16), ""));

            Assert.AreEqual("S side of pit", textifier.CreateTextForControl(ControlId(17), ""));

            Assert.AreEqual("Reentrant, 4m deep", textifier.CreateTextForControl(ControlId(18), ""));

            Assert.AreEqual("Upper boulder, 2m high", textifier.CreateTextForControl(ControlId(2), ""));

            Assert.AreEqual("E edge of SE overgrown pond", textifier.CreateTextForControl(ControlId(28), ""));

            Assert.AreEqual("Stony ground, 6m by 7m (manned)", textifier.CreateTextForControl(ControlId(20), ""));
        }

        [TestMethod]
        public void MissingMainSymbol()
        {
            Setup();
            Textifier textifier = new Textifier(eventDB, symbolDB, "en");

            Assert.AreEqual("S ", textifier.CreateTextForControl(ControlId(19), ""));

            Assert.AreEqual("", textifier.CreateTextForControl(ControlId(21), ""));
        }

        [TestMethod]
        public void Between()
        {
            Setup();
            Textifier textifier = new Textifier(eventDB, symbolDB, "en");

            Assert.AreEqual("Between building and statue", textifier.CreateTextForControl(ControlId(8), ""));

            Assert.AreEqual("Between marshes, 4m by 4m and 5m by 6m", textifier.CreateTextForControl(ControlId(9), ""));

            Assert.AreEqual("Between path crossings", textifier.CreateTextForControl(ControlId(11), ""));

            Assert.AreEqual("Between boulders, 0.5m to 2.5m high", textifier.CreateTextForControl(ControlId(12), ""));
        }

        [TestMethod]
        public void JunctionCrossings()
        {
            Setup();
            Textifier textifier = new Textifier(eventDB, symbolDB, "en");

            Assert.AreEqual("N side of N power line and path crossing (radio)", textifier.CreateTextForControl(ControlId(5), ""));

            Assert.AreEqual("Small gully junction", textifier.CreateTextForControl(ControlId(7), ""));

            Assert.AreEqual("N side of road and power line crossing", textifier.CreateTextForControl(ControlId(10), ""));

            Assert.AreEqual("N path junction", textifier.CreateTextForControl(ControlId(13), ""));
        }

        [TestMethod]
        public void SimpleCombo()
        {
            Setup();
            Textifier textifier = new Textifier(eventDB, symbolDB, "en");

            Assert.AreEqual("Knoll and boulder", textifier.CreateTextForControl(ControlId(14), ""));
        }

        [TestMethod]
        public void Custom()
        {
            Setup();
            Textifier textifier = new Textifier(eventDB, symbolDB, "en");

            Assert.AreEqual("very marshy spot", textifier.CreateTextForControl(ControlId(4), "50 m"));
        }

        [TestMethod]
        public void CustomSymbols()
        {
            Setup();
            Textifier textifier = new Textifier(eventDB, symbolDB, "en");

            Assert.AreEqual("Wet man-made object", textifier.CreateTextForControl(ControlId(25), ""));
            Assert.AreEqual("Slide (medical)", textifier.CreateTextForControl(ControlId(26), ""));
            Assert.AreEqual("S side of light pole", textifier.CreateTextForControl(ControlId(27), ""));
            Assert.AreEqual("Between slides", textifier.CreateTextForControl(ControlId(29), ""));
            Assert.AreEqual("Knoll and boulder", textifier.CreateTextForControl(ControlId(14), ""));
        }

        [TestMethod]
        public void CustomSymbolsOtherLanguage()
        {
            Setup();
            Textifier textifier = new Textifier(eventDB, symbolDB, "bg");

            Assert.AreEqual("Marshy special item", textifier.CreateTextForControl(ControlId(25), ""));
            Assert.AreEqual("Smurf (first aid)", textifier.CreateTextForControl(ControlId(26), ""));
            Assert.AreEqual("S side of bigrash", textifier.CreateTextForControl(ControlId(27), ""));
            Assert.AreEqual("Between smurfella", textifier.CreateTextForControl(ControlId(29), ""));
            Assert.AreEqual("Knoll and boulder", textifier.CreateTextForControl(ControlId(14), ""));
        }

        [TestMethod]
        public void Start()
        {
            Setup();
            Textifier textifier = new Textifier(eventDB, symbolDB, "en");

            Assert.AreEqual("Start: open bare rock", textifier.CreateTextForControl(ControlId(1), ""));
        }

        [TestMethod]
        public void Directives()
        {
            Setup();
            Textifier textifier = new Textifier(eventDB, symbolDB, "en");

            Assert.AreEqual("Mandatory crossing point", textifier.CreateTextForControl(ControlId(3), ""));

            Assert.AreEqual("Navigate 160 m to finish funnel", textifier.CreateTextForControl(ControlId(6), "160 m"));

            Assert.AreEqual("Mandatory crossing point", textifier.CreateTextForControl(ControlId(15), "160 m"));
            Assert.AreEqual("Navigate to finish funnel", textifier.CreateTextForControl(ControlId(6), ""));
            Assert.AreEqual("Mandatory crossing point", textifier.CreateTextForControl(ControlId(15), ""));
        }

        [TestMethod]
        public void ShowText()
        {
            Setup();
            Textifier textifier = new Textifier(eventDB, symbolDB, "en");

            foreach (Id<ControlPoint> controlId in eventDB.AllControlPointIds) {
                Console.WriteLine("{0}: '{1}'", controlId, textifier.CreateTextForControl(controlId, "160 m"));
            }
        }

        [TestMethod]
        public void CreateTextForDirective()
        {
            Setup();
            Textifier textifier = new Textifier(eventDB, symbolDB, "en");

            Assert.AreEqual("Follow tapes 230 m between controls", textifier.CreateTextForDirective("13.2", "230 m"));
        }

        [TestMethod]
        public void GetTextFromSize()
        {
            Setup();
            Textifier textifier = new Textifier(eventDB, symbolDB, "en");


            Assert.AreEqual("5m high", textifier.GetTextFromSize("5", true, false, "", false, ""));
            Assert.AreEqual("5m deep", textifier.GetTextFromSize("5m", true, true, "", true, ""));
            Assert.AreEqual("5m deep", textifier.GetTextFromSize("5 m", true, true, "", true, ""));
            Assert.AreEqual("5m high", textifier.GetTextFromSize("5.0", true, false, "", false, ""));
            Assert.AreEqual("5m high", textifier.GetTextFromSize("5.0m ", true, false, "", false, ""));
            Assert.AreEqual("0.6m high", textifier.GetTextFromSize(".6 ", true, false, "", false, ""));
            Assert.AreEqual("4m by 3m", textifier.GetTextFromSize("4.0x3.0", true, false, "", false, ""));
            Assert.AreEqual("4m by 3m", textifier.GetTextFromSize("4x3", true, true, "", true, ""));
            Assert.AreEqual("0.5m to 3m high", textifier.GetTextFromSize("0.5/3", true, false, "", false, ""));
            Assert.AreEqual("0.5m to 3m deep", textifier.GetTextFromSize("0.5m/3m", true, true, "", true, ""));
            Assert.AreEqual("0.5m deep and 3m high", textifier.GetTextFromSize("0.5|3", true, true, "", false, ""));
            Assert.AreEqual("0.5m deep and 3.2m deep", textifier.GetTextFromSize("0,5|3.2", true, true, "", true, ""));
            Assert.AreEqual("2m by 3m and 5m by 7m", textifier.GetTextFromSize("2x3|5x7", true, false, "", false, ""));
        }

        [TestMethod]
        public void GetTextFromSizeDecimalStyles()
        {
            Setup();
            Textifier textifier = new Textifier(eventDB, symbolDB, "en");

            Assert.AreEqual("5m high", textifier.GetTextFromSize("5.0", true, false, "", false, ""));
            Assert.AreEqual("5m deep", textifier.GetTextFromSize("5,0", true, true, "", true, ""));
            Assert.AreEqual("5.2m deep", textifier.GetTextFromSize("5.2", true, true, "", true, ""));
            Assert.AreEqual("5.2m high", textifier.GetTextFromSize("5,2", true, false, "", false, ""));

            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");

            Assert.AreEqual("5m deep", textifier.GetTextFromSize("5.0", true, true, "", true, ""));
            Assert.AreEqual("5m high", textifier.GetTextFromSize("5,0", true, false, "", false, ""));
            Assert.AreEqual("5,2m high", textifier.GetTextFromSize("5.2", true, false, "", false, ""));
            Assert.AreEqual("5,2m deep", textifier.GetTextFromSize("5,2", true, true, "", true, ""));
        }

        // Sets up with special symbols.xml that has fake german text in it, to test gender/plural stuff.
        public void SetupWithGerman()
        {
            undomgr = new UndoMgr(5);
            eventDB = new EventDB(undomgr);
            symbolDB = new SymbolDB(TestUtil.GetTestFile("textifier\\de_symbols.xml"));
            eventDB.Load(TestUtil.GetTestFile("textifier\\sampleevent2.coursescribe"));
            eventDB.Validate();
        }

        // Get a simple german symbol.
        [TestMethod]
        public void SimpleGerman()
        {
            SetupWithGerman();
            Textifier textifier = new Textifier(eventDB, symbolDB, "de");

            Assert.AreEqual("De-marsh", textifier.CreateTextForControl(ControlId(4), ""));
        }

        [TestMethod]
        public void GermanGenderAgreement()
        {
            SetupWithGerman();
            Textifier textifier = new Textifier(eventDB, symbolDB, "de");

            Assert.AreEqual("De-overgrown-masc de-marsh", textifier.CreateTextForControl(ControlId(28), ""));
            Assert.AreEqual("De-overgrown-fem de-reentrant, 4m de-deep-fem", textifier.CreateTextForControl(ControlId(12), ""));
            Assert.AreEqual("De-overgrown-neut de-pit, 4m de-deep-neut", textifier.CreateTextForControl(ControlId(29), ""));
            Assert.AreEqual("De-between de-pit and de-reentrant, 6m de-deep-neut and 3m de-deep-fem", textifier.CreateTextForControl(ControlId(7), ""));

            Assert.AreEqual("De-N-masc side of de-upper-masc de-marsh", textifier.CreateTextForControl(ControlId(8), ""));
            Assert.AreEqual("De-N-fem side of de-upper-fem de-reentrant", textifier.CreateTextForControl(ControlId(11), ""));
            Assert.AreEqual("De-N-neut side of de-upper-neut de-pit", textifier.CreateTextForControl(ControlId(10), ""));
        }

        [TestMethod]
        public void ComplexGenderAgreement()
        {
            SetupWithGerman();
            Textifier textifier = new Textifier(eventDB, symbolDB, "de");

            Assert.AreEqual("De-N-fem side of de-junction of de-overgrown-plural-neut de-roads", textifier.CreateTextForControl(ControlId(18), ""));
        }

        [TestMethod] public void GermanPluralAgreement()
        {
            SetupWithGerman();
            Textifier textifier = new Textifier(eventDB, symbolDB, "de");

            Assert.AreEqual("De-between de-overgrown-plural-neut de-pits", textifier.CreateTextForControl(ControlId(9), ""));
            Assert.AreEqual("De-between de-overgrown-plural-masc de-marshes", textifier.CreateTextForControl(ControlId(13), ""));
            Assert.AreEqual("De-between de-overgrown-plural-fem de-reentrants", textifier.CreateTextForControl(ControlId(14), ""));
        }

    }
}

#endif //TEST
