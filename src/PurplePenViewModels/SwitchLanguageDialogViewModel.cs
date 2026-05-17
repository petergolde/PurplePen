// SwitchLanguageViewModel.cs
//
// ViewModel for the language-switching dialog. Holds the currently selected
// language code and a list of available languages to choose from.
// Each language is represented by a LanguageItem with a code (e.g. "fr")
// and a display name (e.g. "Français").

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PurplePen.ViewModels
{
    /// <summary>
    /// Represents a single language choice in the language list.
    /// </summary>
    public class LanguageItem
    {
        /// <summary>
        /// The culture code, e.g. "en", "fr", "pt-BR".
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// The display name shown in the list, e.g. "English", "Français".
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Creates a new LanguageItem with the given culture code and display name.
        /// </summary>
        /// <param name="code">Culture code such as "en" or "pt-BR".</param>
        /// <param name="displayName">Human-readable language name.</param>
        public LanguageItem(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }

        /// <summary>
        /// Returns the display name for use in list controls.
        /// </summary>
        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// ViewModel for the Switch Language dialog. Contains the list of
    /// available languages and tracks which one is currently selected.
    /// </summary>
    public partial class SwitchLanguageDialogViewModel : ViewModelBase
    {
        /// <summary>
        /// The list of languages available for selection.
        /// </summary>
        public ObservableCollection<LanguageItem> AvailableLanguages { get; }

        /// <summary>
        /// The currently selected language item in the list.
        /// Bound two-way to the ListBox's SelectedItem.
        /// </summary>
        [ObservableProperty]
        private LanguageItem? selectedLanguage;

        /// <summary>
        /// Parameterless constructor for the Avalonia designer.
        /// Populates the ViewModel with sample data so the previewer
        /// can render the dialog with realistic content.
        /// </summary>
        public SwitchLanguageDialogViewModel()
            : this("en", CreateDefaultLanguages())
        {
        }

        /// <summary>
        /// Creates a new SwitchLanguageViewModel with the specified current
        /// language and list of available languages.
        /// </summary>
        /// <param name="currentLanguageCode">The language code currently in use, e.g. "en".</param>
        /// <param name="availableLanguages">The list of languages to offer.</param>
        public SwitchLanguageDialogViewModel(string currentLanguageCode, ObservableCollection<LanguageItem> availableLanguages)
        {
            AvailableLanguages = availableLanguages;

            // Select the item matching the current language code.
            // First try exact match, then fall back to matching first two characters.
            foreach (LanguageItem item in AvailableLanguages) {
                if (string.Equals(item.Code, currentLanguageCode, System.StringComparison.OrdinalIgnoreCase)) {
                    SelectedLanguage = item;
                    break;
                }
            }

            if (SelectedLanguage == null && currentLanguageCode.Length > 2) {
                string prefix = currentLanguageCode.Substring(0, 2);
                foreach (LanguageItem item in AvailableLanguages) {
                    if (item.Code.Length >= 2 && string.Equals(item.Code.Substring(0, 2), prefix, System.StringComparison.OrdinalIgnoreCase)) {
                        SelectedLanguage = item;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Discovers available languages by scanning subdirectories of the application
        /// directory for satellite resource assemblies (directories named with valid culture codes).
        /// Always includes English, which has no satellite directory.
        /// </summary>
        /// <returns>A collection of discovered LanguageItems, sorted by display name.</returns>
        public static ObservableCollection<LanguageItem> CreateDefaultLanguages()
        {
            ObservableCollection<LanguageItem> languages = new ObservableCollection<LanguageItem>();

            try {
                string baseDirectory = AppContext.BaseDirectory;

                foreach (string subdir in Directory.GetDirectories(baseDirectory)) {
                    string dirName = Path.GetFileName(subdir);

                    if (IsValidCultureName(dirName)) {
                        CultureInfo culture = CultureInfo.GetCultureInfo(dirName);
                        string displayName = culture.TextInfo.ToTitleCase(culture.NativeName);
                        languages.Add(new LanguageItem(dirName, displayName));
                    }
                }
            }
            catch (Exception) {
                // If directory scanning fails (e.g. in designer), fall through to add English only.
            }

            // Always include English, which doesn't have a satellite resource directory.
            languages.Add(new LanguageItem("en", "English"));

            // Sort alphabetically by display name.
            ObservableCollection<LanguageItem> sorted = new ObservableCollection<LanguageItem>();
            foreach (LanguageItem item in languages) {
                int insertIndex = 0;
                while (insertIndex < sorted.Count &&
                       string.Compare(sorted[insertIndex].DisplayName, item.DisplayName, StringComparison.CurrentCulture) < 0) {
                    insertIndex++;
                }
                sorted.Insert(insertIndex, item);
            }

            return sorted;
        }

        /// <summary>
        /// Checks whether the given string is a valid culture name.
        /// </summary>
        /// <param name="cultureName">The string to test.</param>
        /// <returns>True if the string is a recognized culture name.</returns>
        private static bool IsValidCultureName(string cultureName)
        {
            try {
                CultureInfo.GetCultureInfo(cultureName);
                return true;
            }
            catch (Exception) {
                return false;
            }
        }

        /// <summary>
        /// Creates test data for design-time and prototyping.
        /// </summary>
        /// <returns>A SwitchLanguageDialogViewModel populated with discovered languages.</returns>
        public static SwitchLanguageDialogViewModel CreateTestData()
        {
            return new SwitchLanguageDialogViewModel("en", CreateDefaultLanguages());
        }
    }
}
