// UILanguageService.cs
//
// Avalonia implementation of IUILanguage. Sets the thread's CurrentUICulture
// and refreshes all LocalizedString bindings via LocalizedStringManager.

using PurplePen;
using System;
using System.Globalization;
using System.Threading;

namespace AvPurplePen
{
    /// <summary>
    /// Manages the UI language for the Avalonia application.
    /// Setting <see cref="LanguageCode"/> updates the current thread's UI culture
    /// and notifies <see cref="LocalizedStringManager"/> to refresh all localized bindings.
    /// </summary>
    public class UILanguageService : IUILanguage
    {
        /// <summary>
        /// Gets or sets the current UI language code.
        /// Setting this changes CurrentUICulture and refreshes all localized UI strings.
        /// </summary>
        public string LanguageCode
        {
            get => CultureInfo.CurrentUICulture.Name;
            set
            {
                CultureInfo newCulture = new CultureInfo(value);
                Thread.CurrentThread.CurrentUICulture = newCulture;
                CultureInfo.DefaultThreadCurrentUICulture = newCulture;

                // Save the new language code to user settings if it has changed.
                string oldSettingsLanguage = UserSettings.Current.UILanguage;
                if (oldSettingsLanguage != newCulture.Name) {
                    UserSettings.Current.UILanguage = newCulture.Name;
                    UserSettings.Current.Save();
                }

                LocalizedStringManager.Instance.NotifyLanguageChanged();

                // Refreshing the bindings is not enough for the macOS menu bar: its top-level
                // titles are only re-read when the menu is re-exported. No-op elsewhere.
                MacMenuUtilities.RefreshMenuBar();
            }
        }
    }
}
