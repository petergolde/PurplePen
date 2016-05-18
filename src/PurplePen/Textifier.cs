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

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Globalization;

namespace PurplePen
{
    class Textifier
    {
        EventDB eventDB;
        SymbolDB symbolDB;
        string language;

        // Initialize the textifier.
        public Textifier(EventDB eventDB, SymbolDB symbolDB, string language)
        {
            this.eventDB = eventDB;
            this.symbolDB = symbolDB;
            this.language = language;
        }

        /// <summary>
        /// Create the text version of a particular control point.
        /// The distanceText parameter is used for finish, marked route end,
        /// and other similar features, and be the distance, already rounded with "m" suffix. Use "" if none.
        /// Custom text for the control or for symbols are taken into account.
        /// </summary>
        public string CreateTextForControl(Id<ControlPoint> controlId, string distanceText)
        {
            ControlPoint controlPoint = eventDB.GetControl(controlId);

            // If there is custom text, just return it.
            if (!string.IsNullOrEmpty(controlPoint.descriptionText))
                return controlPoint.descriptionText;

            string text;

            switch (controlPoint.kind) {
                case ControlPointKind.Normal:
                    text = CreateTextForNormalControl(controlPoint);
                    break;

                case ControlPointKind.Start:
                case ControlPointKind.MapExchange:
                    text = CreateTextForStartControl(controlPoint);
                    break;

                case ControlPointKind.Finish:
                case ControlPointKind.CrossingPoint:
                    text = CreateTextForDirective(controlPoint.symbolIds[0], distanceText);
                    break;

                default:
                    Debug.Fail("bad control point kind"); text = ""; break;
            }

            return CapitalizeFirstLetter(text);
        }

        string CreateTextForNormalControl(ControlPoint controlPoint)
        {
            Symbol[] symbols = GetSymbols(controlPoint);

            // Get the main feature, including modifiers and between/crossing/junction modifiers.
            string fullTextGender;
            string fullText = GetMainFeatureText(controlPoint, symbols, out fullTextGender);

            // Add size from coumn F.
            if (!string.IsNullOrEmpty(controlPoint.columnFText)) {
                Symbol symbolControllingNounCase; // not used.
                fullText = fullText + ", " + GetTextFromColumnF(controlPoint.columnFText, symbols, out symbolControllingNounCase);
            }

            // Which of many, column C
            fullText = AddSymbolToCurrent(fullText, fullTextGender, symbols[0]);

            // Flag location, column G
            if (symbols[4] != null && symbols[4].Id != "11.15")        // "between" is handled elsewhere.
                fullText = AddSymbolToCurrent(fullText, fullTextGender, symbols[4]);

            // Extra info (column H)
            fullText = AddColumnHString(fullText, symbols[5]);

            // We're done.
            return fullText;
        }


        // Add a symbol, if non-null, to the current text. The symbol text is assumed to be a formatting string where {0} has current text
        string AddSymbolToCurrent(string current, string gender, Symbol symbol)
        {
            if (symbol == null)
                return current;
            else
                return string.Format(GetSymbolText(symbol, gender), current);
        }

        // Add columnH info
        string AddColumnHString(string current, Symbol symbol)
        {
            if (symbol == null)
                return current;
            else
                return string.Format("{0} ({1})", current, GetSymbolText(symbol, ""));
        }


        public string CreateTextForDirective(string symbolId, string distanceText)
        {
            if (symbolId == null)
                return null;        // no text available.

            Symbol symbol = symbolDB[symbolId];

            // Always need to add a space suffix unless it is empty..
            if (distanceText != "")
                distanceText = distanceText + " ";
            return CapitalizeFirstLetter(string.Format(symbol.GetText(language), distanceText));  // Note: directive texts cant be customized, so we don't use GetSymbolText here.
        }

        string CreateTextForStartControl(ControlPoint controlPoint)
        {
            // Add "Start: " to the description for a regular control.
            return GetSymbolText(symbolDB["start"], "") + ": " + CreateTextForNormalControl(controlPoint);
        }

        // Get the text for a symbol. Checks the eventDB for overrides to the symbol text; otherwise uses the default for the symbol.
        string GetSymbolText(Symbol symbol, string gender, string nounCase = "")
        {
            Event ev = eventDB.GetEvent();
            string id = symbol.Id;

            if (ev.customSymbolText.ContainsKey(id) && Symbol.ContainsLanguage(ev.customSymbolText[id], language))
                return Symbol.GetBestSymbolText(symbolDB, ev.customSymbolText[id], language, false, gender, nounCase);
            else
                return symbol.GetText(language, gender, nounCase);
        }

        // Get the plural text for a symbol. Checks the eventDB for overrides to the symbol text; otherwise uses the default for the symbol.
        string GetSymbolPluralText(Symbol symbol, string gender, string nounCase = "")
        {
            Event ev = eventDB.GetEvent();
            string id = symbol.Id;

            if (ev.customSymbolText.ContainsKey(id) && Symbol.ContainsLanguage(ev.customSymbolText[id], language))
                return Symbol.GetBestSymbolText(symbolDB, ev.customSymbolText[id], language, true, gender, nounCase);
            else
                return symbol.GetPluralText(language, gender, nounCase);
        }

        // Get the gender for a symbol. Checks the eventDB for overrides to the symbol text; otherwise uses the default for the symbol.
        string GetSymbolGender(Symbol symbol)
        {
            Event ev = eventDB.GetEvent();
            string id = symbol.Id;

            if (ev.customSymbolText.ContainsKey(id) && Symbol.ContainsLanguage(ev.customSymbolText[id], language))
                return Symbol.GetSymbolGender(symbolDB, ev.customSymbolText[id], language);
            else
                return symbol.GetGender(language);
        }

        // Get the modified case for a symbol. Checks the eventDB for overrides to the symbol text; otherwise uses the default for the symbol.
        string GetSymbolModifiedCase(Symbol symbol)
        {
            Event ev = eventDB.GetEvent();
            string id = symbol.Id;

            if (ev.customSymbolText.ContainsKey(id) && Symbol.ContainsLanguage(ev.customSymbolText[id], language))
                return Symbol.GetModifiedCase(symbolDB, ev.customSymbolText[id], language);
            else
                return symbol.GetModifiedCase(language);
        }

        // Create a combination string for crossing/junction/between.
        string CombineSymbols(Symbol comboSymbol, string mainFeature, string mainFeaturePlural, string secondaryFeature, out string pluralCombo, out string gender)
        {
            pluralCombo = null;
            if (secondaryFeature == null && mainFeaturePlural != null) {
                Symbol singleComboSymbol = GetSingleVersionOfComboSymbol(comboSymbol);
                pluralCombo = string.Format(singleComboSymbol.GetPluralText(language), mainFeature, mainFeaturePlural);
                gender = singleComboSymbol.GetGender(language);
                return string.Format(singleComboSymbol.GetText(language), mainFeature, mainFeaturePlural);
            }
            else {
                pluralCombo = string.Format(comboSymbol.GetPluralText(language), mainFeature, secondaryFeature == null ? mainFeature : secondaryFeature);
                gender = comboSymbol.GetGender(language);
                return string.Format(comboSymbol.GetText(language), mainFeature, secondaryFeature == null ? mainFeature : secondaryFeature);
            }
        }

        // Is this a dual-main symbol (two different main symbols in D and E)
        bool IsDualMainSymbol(Symbol[] symbols)
        {
            return (symbols[1] != null && symbols[2] != null && symbols[2].Kind == 'D' && symbols[2].Id != symbols[1].Id);
        }

        Symbol GetSingleVersionOfComboSymbol(Symbol comboSymbol)
        {
            return symbolDB[comboSymbol.Id + "single"];
        }

        // Get the text associated with the main feature. Normally this is just the symbol in column D. But, 
        // there could be a second main feature in column E. Also handles crossing/junction in column F and between in column G.
        string GetMainFeatureText(ControlPoint controlPoint, Symbol[] symbols, out string mainFeatureGender)
        {
            string mainFeature, mainFeaturePlural, mainFeatureCase, secondaryFeature = null;
            bool comboUsed = false;
            
            mainFeatureGender = "";

            if (symbols[1] == null)
                return "";                // no main symbol.

            // Get the main feature (column D) and secondary feature (column E, if a column D symbol is there)

            mainFeatureCase = GetNounCase(controlPoint, false, false, false);
            mainFeature = GetSymbolText(symbols[1], "", mainFeatureCase);
            mainFeatureGender = GetSymbolGender(symbols[1]);
            mainFeaturePlural = GetSymbolPluralText(symbols[1], "", mainFeatureCase);

            if (symbols[2] != null) {
                if (symbols[2].Kind == 'D') {
                    // Additional feature used for combination.
                    secondaryFeature = GetSymbolText(symbols[2], "", mainFeatureCase);
                    if (secondaryFeature == mainFeature)
                        secondaryFeature = null;        // we treate "road/road/crossing" the same as "road/ /crossing" ==> "road crossing".
                }
                else if (symbols[2].Kind == 'E') {
                    // Modifier to the main feature.
                    string modifier = GetSymbolText(symbols[2], mainFeatureGender);
                    mainFeature = string.Format(modifier, mainFeature);

                    if (mainFeaturePlural != null) {
                        string pluralModifier;
                        pluralModifier = GetSymbolPluralText(symbols[2], mainFeatureGender);
                        mainFeaturePlural = string.Format(pluralModifier, mainFeaturePlural);
                    }
                }
            }

            // Do we have crossing/junction combo?
            if (symbols[3] != null) {
                mainFeature = CombineSymbols(symbols[3], mainFeature, mainFeaturePlural, secondaryFeature, out mainFeaturePlural, out mainFeatureGender);
                secondaryFeature = null;
                comboUsed = true;
            }

            // Do have have a between combo?  (Note that we can have BOTH a between and a crossing combo.)
            if (symbols[4] != null && symbols[4].Id == "11.15") {
                mainFeature = CombineSymbols(symbols[4], mainFeature, mainFeaturePlural, secondaryFeature, out mainFeaturePlural, out mainFeatureGender);
                comboUsed = true;
            }

            if (!comboUsed && secondaryFeature != null) {
                // No combo symbol present, but a secondary regular symbol present in column E. This is non-standard.
                mainFeature = CombineSymbols(symbolDB["basic_combo"], mainFeature, mainFeaturePlural, secondaryFeature, out mainFeaturePlural, out mainFeatureGender);
            }

            return mainFeature;
        }


        // Get the case of a noun by looking at modifiers. Options allow ignoring the between or cross/junction modifiers
        string GetNounCase(ControlPoint controlPoint, bool ignoreBetween, bool ignoreCrossJunction, bool ignoreColumnE)
        {
            // We check modifiers in the following order, "out to in", so that inner-most modifiers override outermost.
            // Column H, column G (except "between"), column C, column F (except "cross/junction"), between, cross or junction, column E
            string nounCase = "";
            Symbol[] symbols = GetSymbols(controlPoint);

            // Column H
            if (symbols[5] != null)
                nounCase = ApplyNounCase(symbols[5], nounCase);

            // Column G except "between"
            if (symbols[4] != null && symbols[4].Id != "11.15")
                nounCase = ApplyNounCase(symbols[4], nounCase);

            // Column C
            if (symbols[0] != null)
                nounCase = ApplyNounCase(symbols[0], nounCase);

            // Column F
            if (!string.IsNullOrEmpty(controlPoint.columnFText)) {
                Symbol symbolControllingNounCase;
                GetTextFromColumnF(controlPoint.columnFText, symbols, out symbolControllingNounCase);
                if (symbolControllingNounCase != null)
                    nounCase = ApplyNounCase(symbolControllingNounCase, nounCase);
            }

            // Between
            if (!ignoreBetween && symbols[4] != null && symbols[4].Id == "11.15") {
                Symbol s = symbols[4];
                if (!IsDualMainSymbol(symbols)) {
                    s = GetSingleVersionOfComboSymbol(s);
                }
                nounCase = ApplyNounCase(s, nounCase);
            }

            // Cross or junction
            if (!ignoreCrossJunction && symbols[3] != null) {
                Symbol s = symbols[3];
                if (!IsDualMainSymbol(symbols)) {
                    s = GetSingleVersionOfComboSymbol(s);
                }
                nounCase = ApplyNounCase(s, nounCase);
            }

            // Column E
            if (!ignoreColumnE && symbols[2] != null && symbols[2].Kind == 'E') {
                nounCase = ApplyNounCase(symbols[2], nounCase);
            }

            return nounCase;
        }

        string ApplyNounCase(Symbol symbol, string currentNounCase)
        {
            // Get the case to apply to the modified symbol.
            string modifiedNounCase = GetSymbolModifiedCase(symbol);

            // If it wasn't empty, use, otherwise use the current one.
            if (string.IsNullOrEmpty(modifiedNounCase))
                return currentNounCase;
            else
                return modifiedNounCase;
        }

        string GetTextFromColumnF(string columnFText, Symbol[] symbols, out Symbol symbolControllingNounCase)
        {
            bool firstIsDeep, secondIsDeep;
            string genderFirst = "", genderSecond = "";
            firstIsDeep = secondIsDeep = (symbols[1] != null && symbols[1].SizeIsDepth);
            if (symbols[1] != null)
                genderFirst = GetSymbolGender(symbols[1]);

            if (symbols[2] != null && symbols[2].Kind == 'D') {
                secondIsDeep = symbols[2].SizeIsDepth;
                genderSecond = GetSymbolGender(symbols[2]);
            }

            return GetTextFromSize(columnFText, true, firstIsDeep, genderFirst, secondIsDeep, genderSecond, out symbolControllingNounCase);
        }

        // Convert a size text to a textual equivalent. E.g. "2x4" -> "2m by 4m", "5.0m" -> "5m high", etc. 
        // Return the input size text if the number is in an unrecognized format.
        //
        // If the size text isn't in one of the normal forms, it is just returned. The normal forms are:
        //    "3.5" "3.5m" "3.5 m"
        //    "3.5/2.0" "3.5m/2.0m"
        //    "3.5|2.0" "3.5m|2.0m"
        //    "4x8" "4mx8m"

#if TEST
        internal
#endif //TEST
        string GetTextFromSize(string size, bool useDeepOrHigh, bool firstIsDeep, string genderFirst, bool secondIsDeep, string genderSecond)
        {
            Symbol symbolControllingNounCase;
            return GetTextFromSize(size, useDeepOrHigh, firstIsDeep, genderFirst, secondIsDeep, genderSecond, out symbolControllingNounCase);
        }

        string GetTextFromSize(string size, bool useDeepOrHigh, bool firstIsDeep, string genderFirst, bool secondIsDeep, string genderSecond, out Symbol symbolControllingNounCase)
        {
            symbolControllingNounCase = null;

            // If it's a combo, figure out which kind and the correct combining word.
            Symbol combiner = null;
            int index = size.IndexOf('|');
            if (index >= 0) {
                combiner = symbolDB["9.4"];
            }
            else {
                index = size.IndexOf('/');
                if (index >= 0) {
                    combiner = firstIsDeep ? symbolDB["9.3deep"] : symbolDB["9.3high"];
                    symbolControllingNounCase = combiner;
                    useDeepOrHigh = false;
                }
                else {
                    index = size.IndexOf('x');
                    if (index >= 0) {
                        combiner = symbolDB["9.2"];
                        symbolControllingNounCase = combiner;
                        useDeepOrHigh = false;
                    }
                }
            }

            if (combiner != null) {
                // Combo string
                Symbol firstSymbolControllingNounCase;
                string firstText = GetTextFromSize(size.Substring(0, index), useDeepOrHigh, firstIsDeep, genderFirst, firstIsDeep, genderFirst, out firstSymbolControllingNounCase);
                string secondText = GetTextFromSize(size.Substring(index + 1), useDeepOrHigh, secondIsDeep, genderSecond, secondIsDeep, genderSecond);
                if (string.IsNullOrEmpty(firstText) || string.IsNullOrEmpty(secondText)) {
                    return size;
                }
                else {
                    if (symbolControllingNounCase == null)
                        symbolControllingNounCase = firstSymbolControllingNounCase;

                    return string.Format(combiner.GetText(language, genderFirst), firstText, secondText);
                }
            }
            else {
                // Simple string.
                // Trim spaces and any "m" suffix.
                size = size.Trim();
                if (size.EndsWith("m", StringComparison.InvariantCulture)) {
                    size = size.Substring(0, size.Length - 1);
                }

                // Convert to a double. Allow the current culture, US and french cultures (to allow either "." or "," as the decimal point.)
                double value;
                if (double.TryParse(size, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, null, out value) ||
                    double.TryParse(size, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.GetCultureInfo("en-US"), out value) ||
                    double.TryParse(size, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.GetCultureInfo("fr-FR"), out value)) {
                    string text = Convert.ToString(value) + "m";
                    if (useDeepOrHigh) {
                        Symbol symbol = firstIsDeep ? symbolDB["9.1deep"] : symbolDB["9.1high"];
                        symbolControllingNounCase = symbol;
                        return string.Format(symbol.GetText(language, genderFirst), text);
                    }
                    else {
                        return text;
                    }
                }
                else
                    return size;
            }
        }

        Symbol[] GetSymbols(ControlPoint controlPoint)
        {
            string[] ids = controlPoint.symbolIds;

            Symbol[] symbols = new Symbol[ids.Length];

            for (int i = 0; i < ids.Length; ++i) {
                if (ids[i] != null)
                    symbols[i] = symbolDB[ids[i]];
            }

            return symbols;
        }

        /// <summary>
        /// Capitalize the first letter of a string.
        /// </summary>
#if TEST
        internal
#endif //TEST
        static string CapitalizeFirstLetter(string s)
        {
            if (s != null && s.Length >= 1)
                return char.ToUpperInvariant(s[0]) + s.Substring(1);
            else
                return s;
        }
    }
}
