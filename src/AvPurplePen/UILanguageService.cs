// UILanguageService.cs
//
// Avalonia implementation of IUILanguage. Sets the thread's CurrentUICulture
// and refreshes all LocalizedString bindings via LocalizedStringManager.

using PurplePen;
using System;
using System.Globalization;

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

                // Only DefaultThreadCurrentUICulture is set here, deliberately. Do NOT add
                // "CultureInfo.CurrentUICulture = ..." or "Thread.CurrentThread.CurrentUICulture = ..."
                // (the latter just forwards to the former): that value lives in an AsyncLocal, i.e. in
                // the ExecutionContext. This setter is normally reached from an async continuation
                // (ShowSwitchLanguageDialog assigns LanguageCode after awaiting the dialog), and an
                // AsyncLocal written inside a continuation is restored -- thrown away -- as soon as that
                // continuation returns. The language would appear to change (bindings refreshed below
                // still see it) and then silently revert.
                //
                // DefaultThreadCurrentUICulture is a plain static consulted by the CurrentUICulture
                // getter whenever no thread-level override exists, so it sticks, and it applies to
                // background threads too. That is why Program.InitUILanguage must not set a
                // thread-level culture either: doing so would shadow this permanently.
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
