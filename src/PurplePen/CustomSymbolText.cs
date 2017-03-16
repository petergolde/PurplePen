/* Copyright (c) 2007, Peter Golde
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
using System.ComponentModel;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    public partial class CustomSymbolText: OkCancelDialog
    {
        SymbolDB symbolDB;
        bool useAsLocalizeTool;           // If true, this dialog is being used as a localization tool, not an end-use customization.
        Dictionary<string, List<SymbolText>> customSymbolText = new Dictionary<string,List<SymbolText>>();  // maps symbol IDs to custom symbol text.
        Dictionary<string, bool> customSymbolKey = new Dictionary<string,bool>();   // maps symbol IDs to whether to display key for this custom symbol
        Dictionary<string, List<SymbolText>> symbolNames = new Dictionary<string, List<SymbolText>>();  // maps symbol IDs to symbol names/languages.
        List<SymbolLanguage> languages;

        string selectedId;


        internal CustomSymbolText(SymbolDB symbolDB, bool useAsLocalizeTool): this()
        {
            this.symbolDB = symbolDB;
            this.useAsLocalizeTool = useAsLocalizeTool;
            FillListBox();
            FillLanguages();
            listBoxSymbols.SelectedIndex = 0;

            if (useAsLocalizeTool) {
                labelSymbolName.Visible = textBoxSymbolName.Visible = true;
                buttonDefault.Visible = checkBoxShowKey.Visible = false;
                checkBoxDefaultLanguage.Visible = false;
            }
        }

        public CustomSymbolText()
        {
            InitializeComponent();
        }

        public void SetCustomSymbolDictionaries(Dictionary<string, List<SymbolText>> customSymbolText, Dictionary<string, bool> customSymbolKey)
        {
            this.customSymbolText = customSymbolText;
            this.customSymbolKey = customSymbolKey;

            if (selectedId != null)
                UpdateControlsFromId(selectedId);
        }

        public Dictionary<string, List<SymbolText>> CustomSymbolTexts
        {
            get
            {
                return customSymbolText;
            }
        }

        public Dictionary<string, List<SymbolText>> SymbolNames
        {
            get
            {
                return symbolNames;
            }
        }

        // Get the language id of the selected language.
        public string LangId
        {
            get
            {
                return languages[comboBoxLanguage.SelectedIndex].LangId;
            }
            set
            {
                int index = languages.FindIndex(lang => (lang.LangId == value));
                if (index >= 0)
                    comboBoxLanguage.SelectedIndex = index;
            }
        }

        public bool UseAsDefaultLanguage
        {
            get { return checkBoxDefaultLanguage.Checked; }
            set { checkBoxDefaultLanguage.Checked = value; }
        }

        // Fill the list box with the ids of the symbols we allow customizing.
        // When used as a localization tool, ALL the symbols are shown.
        void FillListBox()
        {
            if (!useAsLocalizeTool) {
                listBoxSymbols.Items.Add("6.1");            // add special items first.
                listBoxSymbols.Items.Add("6.2");
            }

            // Then all other symbols for columns D, E, H.
            foreach (Symbol symbol in symbolDB.AllSymbols) {
                if ((symbol.Kind == 'D' || symbol.Kind == 'E' || symbol.Kind == 'H') && !(symbol.Id == "6.1" || symbol.Id == "6.2") || useAsLocalizeTool) {
                    listBoxSymbols.Items.Add(symbol.Id);
                }
            }
        }

        void FillLanguages()
        {
            languages = new List<SymbolLanguage>(symbolDB.AllLanguages);
            languages.Sort((lang1, lang2) => string.Compare(lang1.Name, lang2.Name, StringComparison.CurrentCultureIgnoreCase));

            foreach (SymbolLanguage lang in languages) {
                comboBoxLanguage.Items.Add(lang.Name);
            }
            comboBoxLanguage.SelectedIndex = 0;
        }

        // Set the text box with a bunch of text, filtered to the current language.
        void SetTextBox(List<SymbolText> texts)
        {
            string langId = LangId;

            List<string> l = new List<string>();
            foreach (SymbolText symtext in texts) {
                if (symtext.Lang == langId && !l.Contains(symtext.Text)) {
                    string s = symtext.Text;
                    if (!useAsLocalizeTool)
                        s = s.Replace("{0}", "*");
                    l.Add(s);
                }
            }
            
            StringBuilder builder = new StringBuilder();
            foreach (string s in l) {
                if (builder.Length > 0)
                    builder.Append("/");
                builder.Append(s);
            }

            textBoxCurrent.Text = builder.ToString();
        }

        // Is the text customized, IN THE CURRENT LANGUAGE, for this id.
        bool TextIsCustomized(string id)
        {
            string langId = LangId;

            return (customSymbolText.ContainsKey(id) && customSymbolText[id].Exists(symtext => (symtext.Lang == langId)));
        }

        // Is the name customized, IN THE CURRENT LANGUAGE, for this id.
        bool NameIsCustomized(string id)
        {
            string langId = LangId;

            return (symbolNames.ContainsKey(id) && symbolNames[id].Exists(symtext => (symtext.Lang == langId)));
        }

        // Update the right hand side controls from a given item.
        void UpdateControlsFromId(string id)
        {
            if (id == null)
                return;

            Symbol symbol = symbolDB[id];

            if (TextIsCustomized(id)) {
                // Uses custom text.
                labelCustomizedText.Visible = true;
                labelStandardText.Visible = false;
                buttonDefault.Enabled = true;
                SetTextBox(customSymbolText[id]);
                checkBoxShowKey.Enabled = true;
                checkBoxShowKey.Checked = customSymbolKey.ContainsKey(id) ? customSymbolKey[id] : false;
            }
            else {
                // Just uses standard text.
                labelCustomizedText.Visible = false;
                labelStandardText.Visible = true;
                buttonDefault.Enabled = false;
                SetTextBox(symbolDB[id].SymbolTexts);
                checkBoxShowKey.Checked = false;
                checkBoxShowKey.Enabled = false;
            }

            if (useAsLocalizeTool) {
                if (NameIsCustomized(id)) {
                    // uses custom name
                    textBoxSymbolName.Text = symbolNames[id].Find(symtext => (symtext.Lang == LangId)).Text;
                }
                else {
                    // uses standard text.
                    textBoxSymbolName.Text = symbolDB[id].GetName(LangId);
                }
            }
        }

        // Update the data from the current state of the right hand side controls
        void UpdateDataFromControls(string id)
        {
            if (checkBoxShowKey.Enabled) 
                customSymbolKey[id] = checkBoxShowKey.Checked;

            if (useAsLocalizeTool) {
                SymbolText symText = new SymbolText();
                symText.Lang = LangId;
                symText.Text = textBoxSymbolName.Text;
                if (!symbolNames.ContainsKey(id)) 
                    symbolNames[id] = new List<SymbolText>();
                List<SymbolText> list = symbolNames[id];
                int index = list.FindIndex(st => (st.Lang == LangId));
                if (index >= 0)
                    list[index] = symText;
                else
                    list.Add(symText);
            }
        }

        private void listBoxSymbols_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();

            if (e.Index >= 0) {
                string id = (string) listBoxSymbols.Items[e.Index];

                // Get bounds of where to draw the symbol and its text.
                RectangleF symbolGraphicsBounds = e.Bounds;
                symbolGraphicsBounds.Width = symbolGraphicsBounds.Height;

                RectangleF textBounds = e.Bounds;
                textBounds.X += symbolGraphicsBounds.Width + 4;

                // Get color to draw with. Draw customize ones in red when not selected.
                Color foreColor;
                if ((e.State & DrawItemState.Selected) == 0 && TextIsCustomized(id))
                    foreColor = Color.Red;
                else
                    foreColor = e.ForeColor;

                // Draw the symbol.
                SmoothingMode oldSmoothing = e.Graphics.SmoothingMode;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Symbol symbol = symbolDB[id];
                symbol.Draw(e.Graphics, foreColor, symbolGraphicsBounds);

                e.Graphics.SmoothingMode = oldSmoothing;

                // Draw the text. Use English if we're customizing text, otherwise use the set language.
                string langId = useAsLocalizeTool ? "en" : LangId;
                string text = symbol.GetName(langId);

                StringFormat stringFormat = new StringFormat(StringFormat.GenericDefault);
                stringFormat.Alignment = StringAlignment.Near;
                stringFormat.LineAlignment = StringAlignment.Center;
                e.Graphics.DrawString(text, e.Font, new SolidBrush(foreColor), textBounds, stringFormat);
            }

            e.DrawFocusRectangle();
        }

        private void listBoxSymbols_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Transfer the old data
            if (selectedId != null)
                UpdateDataFromControls(selectedId);

            selectedId = (string) listBoxSymbols.SelectedItem;
            UpdateControlsFromId(selectedId);
        }

        protected override bool OkButtonClicked()
        {
            if (selectedId != null)
                UpdateDataFromControls(selectedId);

            return true;
        }

        private void buttonDefault_Click(object sender, EventArgs e)
        {
            string langId = LangId;

            // Remove all custom text with the current language.
            List<SymbolText> symtexts = customSymbolText[selectedId];
            symtexts = symtexts.FindAll(symtext => (symtext.Lang != langId));
            if (symtexts.Count == 0) {
                customSymbolText.Remove(selectedId);
                customSymbolKey.Remove(selectedId);
            }
            else {
                customSymbolText[selectedId] = symtexts;
            }

            UpdateControlsFromId(selectedId);
            listBoxSymbols.Refresh();
        }

        private SymbolLanguage CurrentLanguage()
        {
            string langId = LangId;
            SymbolLanguage language = null;
            foreach (SymbolLanguage symLanguage in symbolDB.AllLanguages) {
                if (symLanguage.LangId == langId) {
                    language = symLanguage;
                }
            }

            return language;
        }

        private void buttonChangeText_Click(object sender, EventArgs e)
        {
            if (selectedId != null)
                UpdateDataFromControls(selectedId);

            EnterSymbolText dialog = new EnterSymbolText(symbolDB);

            char kind = symbolDB[selectedId].Kind; 

            SymbolLanguage language = CurrentLanguage();
            dialog.SetLanguage(language);

            List<SymbolText> symTexts;
            if (TextIsCustomized(selectedId))  // UNDONE: this isn't right!
                symTexts = customSymbolText[selectedId];
            else
                symTexts = symbolDB[selectedId].SymbolTexts;

            // Only consider the current language.
            symTexts = symTexts.FindAll(symtext => (symtext.Lang == language.LangId));

            bool hasPlural = false, hasGender = false, hasCase = false;
            foreach (SymbolText symtext in symTexts) {
                if (symtext.Plural)
                    hasPlural = true;
                if (!string.IsNullOrEmpty(symtext.Gender))
                    hasGender = true;
                if (!string.IsNullOrEmpty(symtext.Case))
                    hasCase = true;
            }

            bool isModifier = (kind == 'E' || kind=='C' || kind == 'G' || kind == 'F') && selectedId != "11.15" && !selectedId.StartsWith("10.", StringComparison.InvariantCulture);   // column C, E, F, G, but not between/crossing/junction
            bool isNoun = (kind == 'D' || selectedId == "11.15" || selectedId.StartsWith("10.", StringComparison.InvariantCulture));  // column D or between/junction/crossing

            // Note that between/junction/crossing can both modify case of somthing inside, and have it's own case.
            bool canHaveCase = isNoun;
            bool canModifyCase = (kind=='F' || kind == 'E' || kind=='C' || kind == 'G');


            dialog.SetAllowableForms((isNoun && language.PluralNouns) || (kind=='E' && language.PluralModifiers), hasPlural,
                                                       isModifier && language.GenderModifiers, hasGender,
                                                       canHaveCase && language.CaseModifiers, hasCase,
                                                       isNoun && language.GenderModifiers,
                                                       canModifyCase && language.CaseModifiers,
                                                       !useAsLocalizeTool && isModifier);     

            dialog.SymbolTexts = symTexts;

            if (dialog.ShowDialog(this) == DialogResult.OK) {
                symTexts = dialog.SymbolTexts;

                // retain custom texts from other languages, if any
                if (customSymbolText.ContainsKey(selectedId))  
                    symTexts.AddRange(customSymbolText[selectedId].FindAll(symtext => (symtext.Lang != language.LangId)));

                customSymbolText[selectedId] = symTexts;
                customSymbolKey[selectedId] = checkBoxShowKey.Checked;
                UpdateControlsFromId(selectedId);
            }

            dialog.Dispose();
        }

        private void comboBoxLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateControlsFromId(selectedId);
            listBoxSymbols.Refresh();
        }

        private void CustomSymbolText_Load(object sender, EventArgs e)
        {
            listBoxSymbols.Height = this.ClientSize.Height - listBoxSymbols.Top - 25;
        }
    }
}