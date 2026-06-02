// CustomSymbolTextDialogViewModel.cs
//
// ViewModel for the "Customize Description Text" dialog (and its alternate
// "localization tool" mode). The dialog lets the user pick a description symbol
// from a list and override the textual description used for it, in a chosen
// language. In localization-tool mode (used by the internal "add translated
// texts" command) it additionally lets the user edit the symbol's name and
// shows every symbol rather than just the customizable description columns.
//
// Migrated from WinForms PurplePen/CustomSymbolText.cs.
//
// As with the other settings-style dialogs the caller configures the VM through
// an object initializer and reads results back after OK:
//   1. UseAsLocalizeTool  - which mode (set before SymbolDB).
//   2. SymbolDB           - triggers building the symbol + language lists.
//   3. CustomSymbolTexts / CustomSymbolKey - the dictionaries to edit in place.
//   4. LangId             - selects the starting language.
// After OK the caller reads CustomSymbolTexts, CustomSymbolKey, SymbolNames,
// LangId and UseAsDefaultLanguage.
//
// All localized UI chrome (labels, buttons) lives in the View; the strings this
// VM exposes (symbol names, symbol description texts) are data, not UI text.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PurplePen.Graphics2D;
using PurplePen.MapModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// ViewModel for the CustomSymbolText dialog. Handles all of the dialog's
    /// logic: building the symbol/language lists, tracking which symbol is
    /// selected, and editing the custom-text / custom-name dictionaries.
    /// </summary>
    public partial class CustomSymbolTextDialogViewModel : ViewModelBase
    {
        // Pixel size at which each symbol image is rendered. The list shows it
        // at a smaller logical size, so rendering larger keeps it crisp.
        private const int SymbolImagePixelSize = 44;

        // === Inputs set by the caller ===

        private SymbolDB? symbolDB;

        /// <summary>
        /// The symbol database. Setting it (re)builds the language and symbol
        /// lists, so <see cref="UseAsLocalizeTool"/> must be assigned first.
        /// </summary>
        public SymbolDB? SymbolDB {
            get => symbolDB;
            set { symbolDB = value; Initialize(); }
        }

        /// <summary>
        /// True when used as the internal localization tool: every symbol is
        /// listed, the symbol-name editor is shown, and the customize-only
        /// controls (reset, show-key, default-language) are hidden.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowSymbolNameEditor))]
        [NotifyPropertyChangedFor(nameof(ShowCustomizeControls))]
        private bool useAsLocalizeTool;

        // The custom-text dictionaries. These are edited in place and read back
        // by the caller, so the getter returns the same instance the caller
        // passed in.
        private Dictionary<string, List<SymbolText>> customSymbolText = new Dictionary<string, List<SymbolText>>();
        private Dictionary<string, bool> customSymbolKey = new Dictionary<string, bool>();

        /// <summary>Maps symbol IDs to the custom description text (per language).</summary>
        public Dictionary<string, List<SymbolText>> CustomSymbolTexts {
            get => customSymbolText;
            set {
                customSymbolText = value;
                if (SelectedSymbol != null)
                    UpdateControlsFromId(SelectedSymbol.Id);
                RefreshSymbolList();
            }
        }

        /// <summary>Maps symbol IDs to whether to show the meaning ("key") below the symbol.</summary>
        public Dictionary<string, bool> CustomSymbolKey {
            get => customSymbolKey;
            set {
                customSymbolKey = value;
                if (SelectedSymbol != null)
                    UpdateControlsFromId(SelectedSymbol.Id);
            }
        }

        /// <summary>
        /// Maps symbol IDs to custom symbol names (per language). Only filled in
        /// localization-tool mode; read back by the caller after OK.
        /// </summary>
        public Dictionary<string, List<SymbolText>> SymbolNames { get; } = new Dictionary<string, List<SymbolText>>();

        /// <summary>
        /// Whether the chosen language should become the default for new events.
        /// Read back by the caller after OK (customize mode only).
        /// </summary>
        [ObservableProperty]
        private bool useAsDefaultLanguage;

        // === UI state ===

        /// <summary>The languages available for selection, sorted by name.</summary>
        public ObservableCollection<SymbolLanguageItem> Languages { get; } = new ObservableCollection<SymbolLanguageItem>();

        /// <summary>The currently selected language.</summary>
        [ObservableProperty]
        private SymbolLanguageItem? selectedLanguage;

        /// <summary>The symbols shown in the list.</summary>
        public ObservableCollection<CustomSymbolItemViewModel> Symbols { get; } = new ObservableCollection<CustomSymbolItemViewModel>();

        /// <summary>The currently selected symbol.</summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ChangeTextCommand))]
        private CustomSymbolItemViewModel? selectedSymbol;

        // Right-hand detail pane state.

        /// <summary>True if the selected symbol has customized text in the current language (drives the red "Customized text:" label vs. "Standard text:").</summary>
        [ObservableProperty]
        private bool isTextCustomizedForSelected;

        /// <summary>The (read-only) current description text shown for the selected symbol.</summary>
        [ObservableProperty]
        private string currentText = "";

        /// <summary>Whether the "Reset to standard" button is enabled.</summary>
        [ObservableProperty]
        private bool resetEnabled;

        /// <summary>Whether the "Show meaning" checkbox is enabled.</summary>
        [ObservableProperty]
        private bool showKeyEnabled;

        /// <summary>State of the "Show meaning below symbolic descriptions" checkbox.</summary>
        [ObservableProperty]
        private bool showKeyChecked;

        /// <summary>The editable symbol name (localization-tool mode only).</summary>
        [ObservableProperty]
        private string symbolNameText = "";

        // === Computed visibility (mode-dependent) ===

        /// <summary>Show the symbol-name label and text box (localization tool only).</summary>
        public bool ShowSymbolNameEditor => UseAsLocalizeTool;

        /// <summary>Show the reset/show-key/default-language controls (customize mode only).</summary>
        public bool ShowCustomizeControls => !UseAsLocalizeTool;

        /// <summary>
        /// The id of the currently selected language. Lets the caller seed and
        /// read back the language by its id rather than the wrapper object.
        /// </summary>
        public string LangId {
            get => SelectedLanguage?.LangId ?? "en";
            set {
                SymbolLanguageItem? match = Languages.FirstOrDefault(lang => lang.LangId == value);
                if (match != null)
                    SelectedLanguage = match;
            }
        }

        // === Initialization ===

        // Builds the language and symbol lists once the SymbolDB is known.
        private void Initialize()
        {
            Languages.Clear();
            Symbols.Clear();

            if (SymbolDB == null)
                return;

            BuildLanguages();
            BuildSymbols();

            if (Symbols.Count > 0)
                SelectedSymbol = Symbols[0];
        }

        // Fill the language combo with all languages, sorted by name.
        private void BuildLanguages()
        {
            List<SymbolLanguage> languages = new List<SymbolLanguage>(SymbolDB!.AllLanguages);
            languages.Sort((lang1, lang2) => string.Compare(lang1.Name, lang2.Name, StringComparison.CurrentCultureIgnoreCase));

            foreach (SymbolLanguage lang in languages)
                Languages.Add(new SymbolLanguageItem(lang.Name, lang.LangId));

            if (Languages.Count > 0)
                SelectedLanguage = Languages[0];
        }

        // Fill the symbol list. In customize mode the special 6.1/6.2 symbols come
        // first, followed by the customizable description columns (D, E, H). In
        // localization-tool mode every symbol is shown.
        private void BuildSymbols()
        {
            List<Symbol> chosen = new List<Symbol>();

            if (!UseAsLocalizeTool) {
                chosen.Add(SymbolDB!["6.1"]);
                chosen.Add(SymbolDB!["6.2"]);
            }

            foreach (Symbol symbol in SymbolDB!.AllSymbols) {
                bool isCustomizableColumn = (symbol.Kind == 'D' || symbol.Kind == 'E' || symbol.Kind == 'H') &&
                                            symbol.Id != "6.1" && symbol.Id != "6.2";
                if (isCustomizableColumn || UseAsLocalizeTool)
                    chosen.Add(symbol);
            }

            foreach (Symbol symbol in chosen) {
                IGraphicsBitmap image = RenderSymbolImage(symbol);
                Symbols.Add(new CustomSymbolItemViewModel(symbol, this, image));
            }
        }

        // Render a symbol to a small black-on-transparent bitmap for the list.
        private static IGraphicsBitmap RenderSymbolImage(Symbol symbol)
        {
            using (IBitmapGraphicsTarget grTarget = Services.BitmapGraphicsTargetProvider.CreateBitmapGraphicsTarget(
                       SymbolImagePixelSize, SymbolImagePixelSize, CmykColor.FromCmyka(0, 0, 0, 0, 0), DefaultColorConverter.Instance)) {
                grTarget.PushAntiAliasing(true);
                symbol.Draw(grTarget, CmykColor.FromColor(Color.Black), new RectangleF(0, 0, SymbolImagePixelSize, SymbolImagePixelSize));
                return grTarget.FinishBitmap();
            }
        }

        // === Selection / language change handlers ===

        partial void OnSelectedSymbolChanged(CustomSymbolItemViewModel? oldValue, CustomSymbolItemViewModel? newValue)
        {
            // Transfer the edits made for the previously-selected symbol, then
            // load the controls for the newly-selected one.
            if (oldValue != null)
                UpdateDataFromControls(oldValue.Id);
            if (newValue != null)
                UpdateControlsFromId(newValue.Id);
        }

        partial void OnSelectedLanguageChanged(SymbolLanguageItem? oldValue, SymbolLanguageItem? newValue)
        {
            OnPropertyChanged(nameof(LangId));

            // NOTE: deliberately does NOT save the in-progress edits first
            // (mirrors the WinForms behavior): LangId has already changed, so
            // saving here would file the edits under the wrong language.
            if (SelectedSymbol != null)
                UpdateControlsFromId(SelectedSymbol.Id);

            RefreshSymbolList();
        }

        // Recompute each list item's name and customized state for the current language.
        private void RefreshSymbolList()
        {
            foreach (CustomSymbolItemViewModel item in Symbols)
                item.Refresh();
        }

        // === Customization queries ===

        /// <summary>Is the text customized, in the current language, for this id?</summary>
        public bool IsTextCustomized(string id)
        {
            string langId = LangId;
            return customSymbolText.ContainsKey(id) && customSymbolText[id].Exists(symtext => symtext.Lang == langId);
        }

        // Is the name customized, in the current language, for this id?
        private bool IsNameCustomized(string id)
        {
            string langId = LangId;
            return SymbolNames.ContainsKey(id) && SymbolNames[id].Exists(symtext => symtext.Lang == langId);
        }

        // === Detail pane <-> data transfer ===

        // Update the detail pane controls from the data for a given symbol id.
        private void UpdateControlsFromId(string id)
        {
            if (id == null || SymbolDB == null)
                return;

            if (IsTextCustomized(id)) {
                // Uses custom text.
                IsTextCustomizedForSelected = true;
                ResetEnabled = true;
                SetCurrentTextFrom(customSymbolText[id]);
                ShowKeyEnabled = true;
                ShowKeyChecked = customSymbolKey.ContainsKey(id) ? customSymbolKey[id] : false;
            }
            else {
                // Just uses standard text.
                IsTextCustomizedForSelected = false;
                ResetEnabled = false;
                SetCurrentTextFrom(SymbolDB[id].SymbolTexts);
                ShowKeyChecked = false;
                ShowKeyEnabled = false;
            }

            if (UseAsLocalizeTool) {
                if (IsNameCustomized(id))
                    SymbolNameText = SymbolNames[id].Find(symtext => symtext.Lang == LangId)!.Text;
                else
                    SymbolNameText = SymbolDB[id].GetName(LangId) ?? "";
            }
        }

        // Fill CurrentText with the texts for the current language, joined with "/".
        private void SetCurrentTextFrom(List<SymbolText> texts)
        {
            string langId = LangId;

            List<string> parts = new List<string>();
            foreach (SymbolText symtext in texts) {
                if (symtext.Lang == langId && !parts.Contains(symtext.Text)) {
                    string s = symtext.Text;
                    if (!UseAsLocalizeTool)
                        s = s.Replace("{0}", "*");
                    parts.Add(s);
                }
            }

            CurrentText = string.Join("/", parts);
        }

        // Save the detail pane controls into the data for a given symbol id.
        private void UpdateDataFromControls(string id)
        {
            if (ShowKeyEnabled)
                customSymbolKey[id] = ShowKeyChecked;

            if (UseAsLocalizeTool) {
                SymbolText symText = new SymbolText { Lang = LangId, Text = SymbolNameText };
                if (!SymbolNames.ContainsKey(id))
                    SymbolNames[id] = new List<SymbolText>();
                List<SymbolText> list = SymbolNames[id];
                int index = list.FindIndex(st => st.Lang == LangId);
                if (index >= 0)
                    list[index] = symText;
                else
                    list.Add(symText);
            }
        }

        /// <summary>
        /// Commit the edits for the currently selected symbol. Called by the
        /// View's OK handler before closing.
        /// </summary>
        public void CommitCurrentEdits()
        {
            if (SelectedSymbol != null)
                UpdateDataFromControls(SelectedSymbol.Id);
        }

        // === Commands ===

        // Reset to standard: remove the custom text for the current language.
        [RelayCommand]
        private void ResetToStandard()
        {
            if (SelectedSymbol == null || !customSymbolText.ContainsKey(SelectedSymbol.Id))
                return;

            string id = SelectedSymbol.Id;
            string langId = LangId;

            List<SymbolText> symtexts = customSymbolText[id].FindAll(symtext => symtext.Lang != langId);
            if (symtexts.Count == 0) {
                customSymbolText.Remove(id);
                customSymbolKey.Remove(id);
            }
            else {
                customSymbolText[id] = symtexts;
            }

            UpdateControlsFromId(id);
            RefreshSymbolList();
        }

        // Change text: opens the EnterSymbolText sub-dialog.
        [RelayCommand(CanExecute = nameof(CanChangeText))]
        private void ChangeText()
        {
            // Commit current edits before opening the sub-dialog.
            CommitCurrentEdits();

#if PORTING
            // TODO: Port the EnterSymbolText dialog and wire it up here.
            //
            // The WinForms implementation (CustomSymbolText.buttonChangeText_Click)
            // opens EnterSymbolText configured for the selected symbol's current
            // language, seeded with the current SymbolTexts (custom if any, else
            // the standard ones, filtered to the current language). It computes
            // the allowable grammatical forms (plural/gender/case for nouns vs.
            // modifiers) from the symbol Kind and the SymbolLanguage flags, then
            // on OK writes the returned texts back into customSymbolText[id]
            // (retaining other languages), sets customSymbolKey[id], and calls
            // UpdateControlsFromId(id) / RefreshSymbolList().
#endif
        }

        private bool CanChangeText() => SelectedSymbol != null;
    }

    /// <summary>
    /// A language choice for the language combo box. ToString returns the
    /// display name so the ComboBox renders it without an item template.
    /// </summary>
    public class SymbolLanguageItem
    {
        public string Name { get; }
        public string LangId { get; }

        public SymbolLanguageItem(string name, string langId)
        {
            Name = name;
            LangId = langId;
        }

        public override string ToString() => Name;
    }

    /// <summary>
    /// One symbol in the dialog's list: its rendered image, display name in the
    /// current language, and whether it has been customized (shown in red).
    /// </summary>
    public partial class CustomSymbolItemViewModel : ObservableObject
    {
        private readonly Symbol symbol;
        private readonly CustomSymbolTextDialogViewModel parent;

        /// <summary>The symbol's id (e.g. "6.1").</summary>
        public string Id => symbol.Id;

        /// <summary>The underlying symbol.</summary>
        public Symbol Symbol => symbol;

        /// <summary>The rendered symbol image (black on transparent), or null if none.</summary>
        public IGraphicsBitmap? SymbolImage { get; }

        /// <summary>The symbol's name in the current display language.</summary>
        [ObservableProperty]
        private string name = "";

        /// <summary>True if customized in the current language (drives red text).</summary>
        [ObservableProperty]
        private bool isCustomized;

        public CustomSymbolItemViewModel(Symbol symbol, CustomSymbolTextDialogViewModel parent, IGraphicsBitmap? image)
        {
            this.symbol = symbol;
            this.parent = parent;
            this.SymbolImage = image;
            Refresh();
        }

        /// <summary>Recompute the name and customized state for the current language.</summary>
        public void Refresh()
        {
            // The list shows English when customizing text, otherwise the chosen language.
            string displayLang = parent.UseAsLocalizeTool ? "en" : parent.LangId;
            Name = symbol.GetName(displayLang) ?? symbol.Id;
            IsCustomized = parent.IsTextCustomized(symbol.Id);
        }
    }
}
