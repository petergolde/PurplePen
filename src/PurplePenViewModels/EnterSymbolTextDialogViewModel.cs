// EnterSymbolTextDialogViewModel.cs
//
// ViewModel for the "Customized Symbol Text" (EnterSymbolText) dialog. The
// dialog lets the user enter the customized description text for a single
// symbol, in a single language, across every grammatical form the language
// supports (singular/plural, gender, case). Each form combination is one row
// in a grid; the user edits the "Text" column. Three check boxes toggle which
// of the plural / gender / case dimensions are expanded into rows. Optionally a
// gender chooser and a "case of modified symbol" chooser are shown below the
// grid (for symbols that take a gender of their own, or modifiers that change
// the case of the noun they modify).
//
// Migrated from WinForms PurplePen/EnterSymbolText.cs. It is launched by the
// CustomSymbolText dialog (see CustomSymbolTextDialogViewModel.ChangeText).
//
// Caller configures the VM through an object initializer and reads the result
// back from SymbolTexts after OK. The properties must be assigned in this order
// (object initializers assign in written order):
//   1. SymbolDB            - the symbol database.
//   2. Language            - builds the gender / case chooser lists.
//   3. Allow*/Show* flags  - which form dimensions are available / expanded,
//      ShowGenderList / ShowCaseList, TranslateFillIn.
//   4. SymbolTexts         - seeds the grid and the choosers (must be last).
//
// All localized UI chrome (labels, the "plural"/"singular" words, the
// "(unchanged)" case option, the require-star error) lives in the View. The
// strings this VM exposes (the gender / case names from the language, the
// description texts) are data, not UI text.

using CommunityToolkit.Mvvm.ComponentModel;
using PurplePen.MapModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the EnterSymbolText dialog. Handles all of the dialog's
    /// logic: building the per-form grid rows, reading edits back out, and
    /// keeping the gender / case choosers in sync.
    /// </summary>
    public partial class EnterSymbolTextDialogViewModel : ViewModelBase
    {
        // === Inputs set by the caller ===

        // The symbol database, needed by Symbol.GetBestSymbolText.
        private SymbolDB? symbolDB;

        /// <summary>The symbol database.</summary>
        public SymbolDB? SymbolDB {
            get => symbolDB;
            set => symbolDB = value;
        }

        // The language whose texts are being edited. Setting it (re)builds the
        // gender and case chooser lists.
        private SymbolLanguage? language;

        /// <summary>
        /// The language being edited. Setting it fills the gender and case
        /// chooser lists, so it must be assigned before <see cref="SymbolTexts"/>.
        /// </summary>
        public SymbolLanguage? Language {
            get => language;
            set { language = value; BuildChoiceLists(); }
        }

        // Whether to translate the fill-in placeholder "{0}" to/from "*" for
        // display (used for modifier symbols, where "*" marks the modified noun).
        private bool translateFillIn;

        /// <summary>
        /// True for modifier symbols: the stored "{0}" placeholder is shown as
        /// "*" in the grid, and the user's text is required to contain a "*".
        /// </summary>
        public bool TranslateFillIn {
            get => translateFillIn;
            set => translateFillIn = value;
        }

        // === Check-box state (visibility + checked) ===

        /// <summary>Whether the "Show plural forms" check box is shown at all.</summary>
        [ObservableProperty]
        private bool allowPluralForms;

        /// <summary>Whether the "Show gender forms" check box is shown at all.</summary>
        [ObservableProperty]
        private bool allowGenderForms;

        /// <summary>Whether the "Show case forms" check box is shown at all.</summary>
        [ObservableProperty]
        private bool allowCaseForms;

        /// <summary>State of "Show plural forms" — expands the plural dimension.</summary>
        [ObservableProperty]
        private bool showPluralForms;

        /// <summary>State of "Show gender forms" — expands the gender dimension.</summary>
        [ObservableProperty]
        private bool showGenderForms;

        /// <summary>State of "Show case forms" — expands the case dimension.</summary>
        [ObservableProperty]
        private bool showCaseForms;

        // === Gender / case choosers (below the grid) ===

        /// <summary>Whether the gender chooser (label + combo) is shown.</summary>
        [ObservableProperty]
        private bool showGenderList;

        /// <summary>Whether the case chooser (label + combo) is shown.</summary>
        [ObservableProperty]
        private bool showCaseList;

        /// <summary>The gender names available for selection (language data).</summary>
        public ObservableCollection<string> GenderChoices { get; } = new ObservableCollection<string>();

        /// <summary>The currently selected gender (from the chooser).</summary>
        [ObservableProperty]
        private string? selectedGender;

        /// <summary>The case-of-modified-symbol options, including the leading "(unchanged)".</summary>
        public ObservableCollection<CaseChoiceItem> CaseChoices { get; } = new ObservableCollection<CaseChoiceItem>();

        /// <summary>The currently selected case-of-modified option.</summary>
        [ObservableProperty]
        private CaseChoiceItem? selectedCaseChoice;

        // === The grid ===

        /// <summary>The grid rows, one per grammatical form combination.</summary>
        public ObservableCollection<SymbolTextRow> Rows { get; } = new ObservableCollection<SymbolTextRow>();

        // Column visibility mirrors the WinForms DataGridView column.Visible
        // flags. They are updated only inside UpdateGrid (NOT when the check box
        // toggles), so that ReadGrid — which runs *before* UpdateGrid on a
        // toggle — reads the grid using its current (old) structure. The View
        // binds the DataGrid columns' IsVisible to these.
        private bool numberColumnVisible;
        private bool genderColumnVisible;
        private bool caseColumnVisible;

        /// <summary>Whether the "Number" (singular/plural) column is shown.</summary>
        public bool NumberColumnVisible => numberColumnVisible;

        /// <summary>Whether the "Gender" column is shown.</summary>
        public bool GenderColumnVisible => genderColumnVisible;

        /// <summary>Whether the "Case" column is shown.</summary>
        public bool CaseColumnVisible => caseColumnVisible;

        // The texts being edited. Null until SymbolTexts is first assigned;
        // the check-box change handlers use this as the "grid is live" guard
        // (mirrors the WinForms `if (symbolTexts != null)` check).
        private List<SymbolText>? symbolTexts;

        /// <summary>
        /// The symbol texts being edited. The setter seeds the grid and the
        /// choosers; the getter reads the current grid edits back out.
        /// </summary>
        public List<SymbolText> SymbolTexts {
            get {
                ReadGrid();
                return symbolTexts!;
            }
            set {
                symbolTexts = value;
                UpdateGrid();
                SeedChoosersFromTexts();
            }
        }

        // === Check-box change handlers ===

        // When a form check box toggles, save the current edits then rebuild the
        // grid for the new set of dimensions. Guarded so it does nothing during
        // the initial property assignment (before SymbolTexts is set).
        partial void OnShowPluralFormsChanged(bool value) => OnFormToggle();
        partial void OnShowGenderFormsChanged(bool value) => OnFormToggle();
        partial void OnShowCaseFormsChanged(bool value) => OnFormToggle();

        private void OnFormToggle()
        {
            if (symbolTexts == null)
                return;     // still being configured; grid not live yet.

            ReadGrid();
            UpdateGrid();
        }

        // === Choosers ===

        // Build the gender and case chooser lists from the language. Mirrors the
        // WinForms SetLanguage method.
        private void BuildChoiceLists()
        {
            GenderChoices.Clear();
            CaseChoices.Clear();

            if (language == null)
                return;

            if (language.Genders != null) {
                foreach (string gender in language.Genders)
                    GenderChoices.Add(gender);
            }

            if (language.CaseModifiers && language.Cases != null && language.Cases.Length > 0) {
                // The first item is the "(unchanged)" option (its display text
                // comes from the View); the rest are the language's cases.
                CaseChoices.Add(new CaseChoiceItem("", isUnchanged: true));
                foreach (string nounCase in language.Cases)
                    CaseChoices.Add(new CaseChoiceItem(nounCase, isUnchanged: false));
            }
        }

        // Pick the initial gender / case selections from the supplied texts.
        // Mirrors the gender / case selection in the WinForms SymbolTexts setter.
        private void SeedChoosersFromTexts()
        {
            if (symbolTexts == null)
                return;

            if (ShowGenderList) {
                foreach (SymbolText symtext in symbolTexts) {
                    if (!string.IsNullOrEmpty(symtext.Gender)) {
                        SelectedGender = symtext.Gender;
                        break;
                    }
                }
            }

            if (ShowCaseList) {
                SelectedCaseChoice = CaseChoices.FirstOrDefault(c => c.IsUnchanged);
                foreach (SymbolText symtext in symbolTexts) {
                    if (!string.IsNullOrEmpty(symtext.CaseOfModified)) {
                        CaseChoiceItem? match = CaseChoices.FirstOrDefault(c => !c.IsUnchanged && c.Value == symtext.CaseOfModified);
                        if (match != null)
                            SelectedCaseChoice = match;
                        break;
                    }
                }
            }
        }

        // === Fill-in placeholder translation ===

        // Translate "{0}" to "*" for display, if requested.
        private string? SanitizeFillIn(string? s)
        {
            if (s == null)
                return null;
            return translateFillIn ? s.Replace("{0}", "*") : s;
        }

        // Translate "*" back to "{0}", and null to empty string.
        private string UnsanitizeFillIn(string? s)
        {
            if (s == null)
                return "";
            return translateFillIn ? s.Replace("*", "{0}") : s;
        }

        // === Grid build / read ===

        // Fill the grid with the values in symbolTexts, expanding the currently
        // enabled form dimensions. Mirrors the WinForms UpdateGrid.
        private void UpdateGrid()
        {
            if (symbolTexts == null || language == null || symbolDB == null)
                return;

            bool usePlurals = numberColumnVisible = ShowPluralForms;
            bool useGender = genderColumnVisible = ShowGenderForms;
            bool useCases = caseColumnVisible = ShowCaseForms;

            Rows.Clear();

            int genderIndex = 0, caseIndex = 0;
            bool plural = false;
            for (; ; ) {
                string gender = useGender ? language.Genders[genderIndex] : "";
                string nounCase = useCases ? language.Cases[caseIndex] : "";
                string text = Symbol.GetBestSymbolText(symbolDB, symbolTexts, language.LangId, plural, gender, nounCase);
                Rows.Add(new SymbolTextRow(plural, gender, nounCase, SanitizeFillIn(text) ?? ""));

                if (useCases && caseIndex < language.Cases.Length - 1) {
                    caseIndex += 1;
                }
                else if (useGender && genderIndex < language.Genders.Length - 1) {
                    genderIndex += 1;      // go to next gender
                    caseIndex = 0;
                }
                else if (usePlurals && !plural) {
                    genderIndex = 0;       // go to next number
                    caseIndex = 0;
                    plural = true;
                }
                else {
                    break;                 // done.
                }
            }

            // The View follows these to show / hide the corresponding columns.
            OnPropertyChanged(nameof(NumberColumnVisible));
            OnPropertyChanged(nameof(GenderColumnVisible));
            OnPropertyChanged(nameof(CaseColumnVisible));
        }

        // Read the values in the grid back into symbolTexts. Mirrors the WinForms
        // ReadGrid. Uses the column-visible fields (not the check-box state) so
        // that, on a check-box toggle, it reads the grid in its current form.
        private void ReadGrid()
        {
            if (language == null)
                return;

            List<SymbolText> symtexts = new List<SymbolText>();

            foreach (SymbolTextRow row in Rows) {
                SymbolText text = new SymbolText();
                text.Lang = language.LangId;
                text.Text = UnsanitizeFillIn(row.Text);

                if (genderColumnVisible)
                    text.Gender = row.Gender;
                else if (ShowGenderList)
                    text.Gender = SelectedGender ?? "";
                else
                    text.Gender = "";

                text.Plural = numberColumnVisible && row.Plural;

                if (caseColumnVisible)
                    text.Case = row.NounCase;

                if (ShowCaseList) {
                    if (SelectedCaseChoice == null || SelectedCaseChoice.IsUnchanged)
                        text.CaseOfModified = "";
                    else
                        text.CaseOfModified = SelectedCaseChoice.Value;
                }

                symtexts.Add(text);
            }

            symbolTexts = symtexts;
        }

        // === OK validation ===

        /// <summary>
        /// When translating fill-in placeholders, every text must contain a "*"
        /// to mark where the modified object is placed. Returns the first row
        /// that is missing one, or null if all rows are valid.
        /// </summary>
        public SymbolTextRow? FindRowMissingStar()
        {
            if (!translateFillIn)
                return null;

            foreach (SymbolTextRow row in Rows) {
                if (!row.Text.Contains("*"))
                    return row;
            }
            return null;
        }
    }

    /// <summary>
    /// One row of the symbol-text grid: a single grammatical form combination
    /// (singular/plural, gender, case) and its editable text.
    /// </summary>
    public partial class SymbolTextRow : ObservableObject
    {
        /// <summary>True for the plural form, false for singular.</summary>
        public bool Plural { get; }

        /// <summary>The gender name for this row (language data), or "".</summary>
        public string Gender { get; }

        /// <summary>The case name for this row (language data), or "".</summary>
        public string NounCase { get; }

        /// <summary>The editable description text (with "*" fill-in markers shown).</summary>
        [ObservableProperty]
        private string text;

        public SymbolTextRow(bool plural, string gender, string nounCase, string text)
        {
            Plural = plural;
            Gender = gender;
            NounCase = nounCase;
            this.text = text;
        }
    }

    /// <summary>
    /// One option in the "case of modified symbol" chooser. The first option is
    /// the "(unchanged)" sentinel (its display text comes from the View); the
    /// rest carry a case name from the language.
    /// </summary>
    public class CaseChoiceItem
    {
        /// <summary>The case name, or "" for the "(unchanged)" option.</summary>
        public string Value { get; }

        /// <summary>True for the leading "(unchanged)" option.</summary>
        public bool IsUnchanged { get; }

        public CaseChoiceItem(string value, bool isUnchanged)
        {
            Value = value;
            IsUnchanged = isUnchanged;
        }

        // Fallback display (used if no item template is applied). The View shows
        // the localized "(unchanged)" text for the sentinel via a template.
        public override string ToString() => Value;
    }
}
