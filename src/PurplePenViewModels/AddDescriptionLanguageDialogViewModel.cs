// AddDescriptionLanguageDialogViewModel.cs
//
// ViewModel for the Add Description Language dialog (a translation tool). The
// user picks a culture for the new language, its native name, the grammatical
// features the description text generator needs (plural nouns/modifiers,
// gender modifiers, case modifiers), and an existing language whose texts are
// copied as the starting point.
//
// Migrated from WinForms PurplePen/DebugUI/NewLanguage.cs.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the Add Description Language dialog. Set <see cref="SymbolDB"/>
    /// before showing; after the dialog returns true, read <see cref="SymbolLanguage"/>
    /// (the new language) and <see cref="CopyFromLangId"/> (the language to copy texts from).
    /// </summary>
    public partial class AddDescriptionLanguageDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// The symbol database. Setting it fills <see cref="CopyFromLanguages"/> and
        /// selects English as the language to copy from.
        /// </summary>
        [ObservableProperty]
        private SymbolDB? symbolDB;

        /// <summary>All cultures known to .NET, sorted by culture name, for the new-language combo box.</summary>
        public IReadOnlyList<CultureInfo> AllCultures { get; }

        /// <summary>Language ids of the languages already in the symbol DB, sorted, for the copy-from combo box.</summary>
        [ObservableProperty]
        private IReadOnlyList<string> copyFromLanguages = Array.Empty<string>();

        /// <summary>The culture chosen for the new language. Choosing one fills in <see cref="LanguageName"/>.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
        private CultureInfo? selectedCulture;

        /// <summary>The name of the new language, in that language.</summary>
        [ObservableProperty]
        private string languageName = "";

        /// <summary>Whether plural nouns are used for between/junction/crossing.</summary>
        [ObservableProperty]
        private bool pluralNouns;

        /// <summary>Whether plural adjectives are used for plural nouns. Only meaningful if <see cref="PluralNouns"/>.</summary>
        [ObservableProperty]
        private bool pluralModifiers;

        /// <summary>Whether adjectives change based on the gender of the noun they modify.</summary>
        [ObservableProperty]
        private bool genderModifiers;

        /// <summary>The genders, separated by commas or spaces. Only used if <see cref="GenderModifiers"/>.</summary>
        [ObservableProperty]
        private string genders = "";

        /// <summary>Whether nouns change case based on their modifiers.</summary>
        [ObservableProperty]
        private bool caseModifiers;

        /// <summary>The cases, separated by commas or spaces. Only used if <see cref="CaseModifiers"/>.</summary>
        [ObservableProperty]
        private string cases = "";

        /// <summary>The language id of the existing language whose texts are copied into the new language.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOkEnabled))]
        private string? copyFromLangId;

        /// <summary>OK is enabled once both a new language and a language to copy from are chosen.</summary>
        public bool IsOkEnabled => SelectedCulture != null && CopyFromLangId != null;

        /// <summary>
        /// Creates the ViewModel and fills in the list of all cultures. The invariant
        /// culture (which has an empty name) is left out, since it isn't a real language.
        /// </summary>
        public AddDescriptionLanguageDialogViewModel()
        {
            AllCultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                                     .Where(c => !string.IsNullOrEmpty(c.Name))
                                     .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                                     .ToArray();
        }

        /// <summary>
        /// The new language described by the dialog's fields. Genders and cases are
        /// split on commas and spaces, and are empty when their checkbox is off.
        /// Plural modifiers are only used together with plural nouns.
        /// </summary>
        public SymbolLanguage SymbolLanguage
        {
            get
            {
                string[] separators = { ",", " " };
                return new SymbolLanguage(
                    LanguageName,
                    SelectedCulture?.Name ?? "",
                    PluralNouns,
                    PluralNouns && PluralModifiers,
                    GenderModifiers,
                    GenderModifiers ? Genders.Split(separators, StringSplitOptions.RemoveEmptyEntries) : new string[0],
                    CaseModifiers,
                    CaseModifiers ? Cases.Split(separators, StringSplitOptions.RemoveEmptyEntries) : new string[0]);
            }
        }

        /// <summary>Fills the copy-from list from the new symbol DB and selects English.</summary>
        /// <param name="value">The new symbol DB.</param>
        partial void OnSymbolDBChanged(SymbolDB? value)
        {
            if (value == null) {
                CopyFromLanguages = Array.Empty<string>();
                CopyFromLangId = null;
                return;
            }

            CopyFromLanguages = value.AllLanguages.Select(lang => lang.LangId)
                                                  .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                                                  .ToArray();
            CopyFromLangId = CopyFromLanguages.Contains("en") ? "en" : CopyFromLanguages.FirstOrDefault();
        }

        /// <summary>Fills in the native name of the newly chosen culture.</summary>
        /// <param name="value">The newly chosen culture.</param>
        partial void OnSelectedCultureChanged(CultureInfo? value)
        {
            if (value != null) {
                LanguageName = value.NativeName;
            }
        }

        /// <summary>Plural adjectives only apply with plural nouns, so clear that choice whenever plural nouns changes.</summary>
        /// <param name="value">The new value of PluralNouns.</param>
        partial void OnPluralNounsChanged(bool value)
        {
            PluralModifiers = false;
        }
    }
}
