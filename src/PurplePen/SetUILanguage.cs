using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using System.Diagnostics;

namespace PurplePen
{
    public partial class SetUILanguage: OkCancelDialog
    {
        public SetUILanguage()
        {
            InitializeComponent();

            InitLanguages();
        }

        public CultureInfo Culture
        {
            set
            {
                languageListBox.SelectedItem = value;
            }

            get
            {
                return (CultureInfo) languageListBox.SelectedItem;
            }
        }

        void InitLanguages()
        {
            // Search all sub-directories of program directory to find ones that are named like a language.
            Uri uri = new Uri(typeof(SetUILanguage).Assembly.CodeBase);
            string baseDirectory = Path.GetDirectoryName(uri.LocalPath);

            // Look through subdirectories to find languages we have.
            foreach (string subdir in Directory.GetDirectories(baseDirectory)) {
                string langName = Path.GetFileName(subdir);

                if (IsValidCultureName(langName)) {
                    CultureInfo cultureInfo = CultureInfo.GetCultureInfo(langName);
                    languageListBox.Items.Add(CultureInfo.GetCultureInfo(langName));
                    //Debug.WriteLine("Found language: " + langName + " - " + cultureInfo.NativeName + " / " + cultureInfo.EnglishName);
                }
            }

            // Add english also.
            languageListBox.Items.Add(CultureInfo.GetCultureInfo("en"));
        }

        bool IsValidCultureName(string cultureName)
        {
            try {
                CultureInfo.GetCultureInfo(cultureName);
            }
            catch (Exception) {
                return false;
            }

            return true;
        }

        private void languageListBox_Format(object sender, ListControlConvertEventArgs e)
        {
            CultureInfo culture = (CultureInfo) e.ListItem;
            e.Value = culture.TextInfo.ToTitleCase(culture.NativeName);
        }
    }
}
