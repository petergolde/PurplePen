using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

namespace PurplePen.DebugUI
{
    partial class NewLanguage: Form
    {
        SymbolDB symbolDB;

        public NewLanguage()
        {
            InitializeComponent();
        }

        public NewLanguage(SymbolDB symbolDB): this()
        {
            this.symbolDB = symbolDB;
        }

        public string LangId
        {
            get { return ((CultureInfo) (languageComboBox.SelectedItem)).Name; }
        }

        public string LanguageName
        {
            get { return textBoxLanguageName.Text; }
        }

        public bool PluralNouns
        {
            get { return pluralNounCheckBox.Checked; }
        }

        public bool PluralModifiers
        {
            get { return PluralNouns && pluralAdjectiveCheckBox.Checked; }
        }

        public bool GenderModifiers
        {
            get { return genderAdjectiveCheckBox.Checked; }
        }

        public string Genders
        {
            get { return gendersTextBox.Text; }
        }

        public bool CaseModifiers
        {
            get { return caseModifiersCheckBox.Checked; }
        }

        public string Cases
        {
            get { return casesTextBox.Text;  }
        }

        public string CopyFromLangId
        {
            get { return (string) copyFromComboBox.SelectedItem; }
        }


        private void NewLanguage_Load(object sender, EventArgs e)
        {
            foreach (CultureInfo cultureInfo in CultureInfo.GetCultures(CultureTypes.AllCultures)) {
                languageComboBox.Items.Add(cultureInfo);
            }

            pluralNounCheckBox_CheckedChanged(this, EventArgs.Empty);
            genderAdjectiveCheckBox_CheckedChanged(this, EventArgs.Empty);
            caseModifiersCheckBox_CheckedChanged(this, EventArgs.Empty);

            foreach (SymbolLanguage lang in symbolDB.AllLanguages) {
                copyFromComboBox.Items.Add(lang.LangId);
            }
            copyFromComboBox.SelectedItem = "en";
        }

        private void languageComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBoxLanguageName.Text = ((CultureInfo) (languageComboBox.SelectedItem)).NativeName;
        }

        private void pluralNounCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            pluralAdjectiveCheckBox.Enabled = pluralNounCheckBox.Checked;
            pluralAdjectiveCheckBox.Checked = false;
        }

        private void genderAdjectiveCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            gendersLabel.Enabled = gendersTextBox.Enabled = genderAdjectiveCheckBox.Checked;
        }

        private void caseModifiersCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            casesLabel.Enabled = casesTextBox.Enabled = caseModifiersCheckBox.Checked;
        }
    }
}
