using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PurplePen.DebugUI
{
    internal class UntranslatedSymbolTexts
    {
        SymbolDB symbolDB;

        public string ReportOnUntranslatedSymbolTexts(SymbolDB symbolDB)
        {
            StringBuilder report = new StringBuilder();
            List<Symbol> allSymbols = symbolDB.AllSymbols.ToList();

            this.symbolDB = symbolDB;

            foreach (Symbol symbol in allSymbols) {
                report.Append(ReportUntranslatedLanguages(symbol));
            }

            return report.ToString();
        }

        private string ReportUntranslatedLanguages(Symbol symbol)
        {
            List<SymbolLanguage> allLanguages = symbolDB.AllLanguages.ToList();
            List<SymbolText> symbolTexts = symbol.SymbolTexts;
            List<SymbolLanguage> untranslatedLanguages = new List<SymbolLanguage>();

            foreach (SymbolLanguage language in allLanguages) {
                if (!Symbol.ContainsLanguage(symbolTexts, language.LangId)) {
                    untranslatedLanguages.Add(language);
                }
            }

            if (untranslatedLanguages.Count > 0) {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine($"Symbol '{symbol.GetText("en")}':");
                foreach (SymbolLanguage language in untranslatedLanguages) {
                    builder.AppendLine($"{language.Name} ({language.LangId})");
                }

                builder.AppendLine();

                return builder.ToString();
            }
            else {
                return "";
            }   
        }

    }
}
