using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

namespace PurplePen.DebugUI
{
    public partial class NewLanguage: Form
    {
        public NewLanguage()
        {
            InitializeComponent();
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



        private void NewLanguage_Load(object sender, EventArgs e)
        {
            foreach (CultureInfo cultureInfo in CultureInfo.GetCultures(CultureTypes.AllCultures)) {
                languageComboBox.Items.Add(cultureInfo);
            }

            pluralNounCheckBox_CheckedChanged(this, EventArgs.Empty);
            genderAdjectiveCheckBox_CheckedChanged(this, EventArgs.Empty);
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
    }
}
