using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace PurplePen
{
    partial class EnterSymbolText: OkCancelDialog
    {
        List<SymbolText> symbolTexts;
        SymbolLanguage symLanguage;
        bool showGenderList;
        bool translateFillIn;

        public EnterSymbolText()
        {
            InitializeComponent();
        }

        public List<SymbolText> SymbolTexts
        {
            get
            {
                ReadGrid();
                return symbolTexts;
            }
            set
            {
                symbolTexts = value;
                UpdateGrid();

                if (showGenderList) {
                    foreach (SymbolText symtext in symbolTexts)
                        if (!string.IsNullOrEmpty(symtext.Gender)) {
                            comboBoxGenderChooser.Text = symtext.Gender;
                            break;
                        }
                }
            }
        }

        public void SetLanguage(SymbolLanguage symLanguage)
        {
            this.symLanguage = symLanguage;

            if (symLanguage.Genders != null && symLanguage.Genders.Length > 0) 
                comboBoxGenderChooser.Items.AddRange(symLanguage.Genders);
        }

        // Translate {0} to * if desired.
        string SanitizeFillIn(string s)
        {
            if (s == null)
                return null;

            if (translateFillIn)
                return s.Replace("{0}", "*");
            else
                return s;
        }

        // Translate * to {0} if desired, and conversion null to empty string
        string UnsanitizeFillIn(string s)
        {
            if (s == null)
                return "";

            if (translateFillIn)
                return s.Replace("*", "{0}");
            else
                return s;
        }

        // Fill the grid with the values in symbolTexts. 
        void UpdateGrid()
        {
            bool usePlurals = numberColumn.Visible = checkBoxPlural.Checked;
            bool useGender = genderColumn.Visible = checkBoxGender.Checked;

            dataGridView.Rows.Clear();

            int genderIndex = 0;
            bool plural = false;
            for (; ; ) {
                string gender = useGender ? symLanguage.Genders[genderIndex] : "";
                string text = Symbol.GetBestSymbolText(symbolTexts, symLanguage.LangId, plural, gender);
                int index = dataGridView.Rows.Add();
                dataGridView[0, index].Value = plural ? MiscText.Plural : MiscText.Singular;
                dataGridView[1, index].Value = gender;
                dataGridView[2, index].Value = SanitizeFillIn(text);

                if (useGender && genderIndex < symLanguage.Genders.Length - 1) {
                    genderIndex += 1;      // go to next gender
                }
                else if (usePlurals && !plural) {
                    genderIndex = 0;     // go to next number
                    plural = true;
                }
                else {
                    break;  // done.
                }
            }

            dataGridView.AutoResizeColumns();
            dataGridView.CurrentCell = dataGridView[2, 0];
        }

        // Read the values in the grid into symbolTexts.
        void ReadGrid()
        {
            List<SymbolText> symtexts = new List<SymbolText>();

            foreach (DataGridViewRow row in dataGridView.Rows) {
                SymbolText text = new SymbolText();
                text.Lang = symLanguage.LangId;
                text.Text = UnsanitizeFillIn((string) row.Cells[2].Value);

                if (genderColumn.Visible)
                    text.Gender = (string) row.Cells[1].Value;
                else if (showGenderList)
                    text.Gender = comboBoxGenderChooser.Text;
                else
                    text.Gender = "";

                if (numberColumn.Visible)
                    text.Plural = (string) row.Cells[0].Value == MiscText.Plural;
                else
                    text.Plural = false;

                symtexts.Add(text);
            }

            symbolTexts = symtexts;
        }

        public void SetAllowableForms(bool allowPluralForms, bool showPluralForms, bool allowGenderForms, bool showGenderForms, bool showGenderList, bool translateFillIn)
        {
            checkBoxPlural.Visible = allowPluralForms;
            checkBoxPlural.Checked = allowPluralForms && showPluralForms;
            checkBoxGender.Visible = allowGenderForms;
            checkBoxGender.Checked = allowGenderForms && showGenderForms;
            labelGenderChooser.Visible = comboBoxGenderChooser.Visible = this.showGenderList = showGenderList;
            this.translateFillIn = translateFillIn;
        }

        private void checkBoxPluralOrGender_CheckedChanged(object sender, EventArgs e)
        {
            if (symbolTexts != null) {
                ReadGrid();
                UpdateGrid();
            }
        }

        private void EnterSymbolText_Shown(object sender, EventArgs e)
        {
            dataGridView.Focus();
        }

        // Show an error message.
        void ErrorMessage(string message)
        {
            ((MainFrame) (Owner.Owner)).ErrorMessage(message);
        }

        private void dataGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (translateFillIn && e.ColumnIndex == 2) {
                string text = e.FormattedValue.ToString();

                if (! text.Contains("*")) {
                    ErrorMessage(MiscText.RequireStar);
                    e.Cancel = true;
                }
            }

        }
    }
}
