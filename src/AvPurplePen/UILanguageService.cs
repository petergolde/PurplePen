// UILanguageService.cs
//
// Avalonia implementation of IUILanguage. Sets the thread's CurrentUICulture
// and refreshes all LocalizedString bindings via LocalizedStringManager.

using Avalonia.Threading;
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
        /// Setting this saves the choice immediately, then applies the culture change on the
        /// next dispatcher turn -- see the comment in the setter for why the delay is required.
        /// </summary>
        public string LanguageCode
        {
            get => CultureInfo.CurrentUICulture.Name;
            set
            {
                CultureInfo newCulture = new CultureInfo(value);

                // Save the new language code to user settings if it has changed. This does not
                // depend on the culture actually being applied, so do it up front.
                string oldSettingsLanguage = UserSettings.Current.UILanguage;
                if (oldSettingsLanguage != newCulture.Name) {
                    UserSettings.Current.UILanguage = newCulture.Name;
                    UserSettings.Current.Save();
                }

                // The culture change MUST happen as its own dispatcher operation. It cannot be
                // done inline here, and the reason is subtle enough to be worth spelling out.
                //
                // CurrentUICulture lives in an AsyncLocal, i.e. in the ExecutionContext, so a
                // write to it is undone as soon as the ExecutionContext.Run scope it was made in
                // exits. Avalonia 12.1's CulturePreservingExecutionContext exists to defeat that:
                // DispatcherOperation.Execute reads the culture back out after the operation's
                // callback returns and re-applies it to the thread *outside* the ExecutionContext
                // scope, so the write survives into later operations.
                //
                // But that read-back only sees the culture as it stands when the operation's
                // top-level callback returns. This setter is normally reached from the
                // continuation of "await ShowDialogAsync(...)", and when a task completes on the
                // UI thread with the same SynchronizationContext the await captured, .NET runs
                // that continuation INLINE -- inside a nested ExecutionContext.Run -- rather than
                // posting it. The nested scope reverts our write before Avalonia's read-back
                // happens, so the language would visibly change (bindings refreshed in the same
                // scope still see it) and then silently revert.
                //
                // Posting makes the change the top-level callback of its own DispatcherOperation,
                // which is exactly what the read-back inspects. As a bonus, this also makes the
                // setter safe to call from any thread.
                Dispatcher.UIThread.Post(() => ApplyCulture(newCulture));
            }
        }

        /// <summary>
        /// Applies the culture and refreshes all localized UI. Must run as the top-level callback
        /// of a dispatcher operation -- see the comment in the LanguageCode setter.
        /// </summary>
        /// <param name="newCulture">The culture to switch the user interface to.</param>
        private static void ApplyCulture(CultureInfo newCulture)
        {
            CultureInfo.CurrentUICulture = newCulture;

            // Also set the app-domain default, which covers background threads that never run a
            // dispatcher operation. On the UI thread it is shadowed: the first dispatcher
            // operation materializes the ambient culture into a thread-level override.
            CultureInfo.DefaultThreadCurrentUICulture = newCulture;

            LocalizedStringManager.Instance.NotifyLanguageChanged();

            // Refreshing the bindings is not enough for the macOS menu bar: its top-level
            // titles are only re-read when the menu is re-exported. No-op elsewhere.
            MacMenuUtilities.RefreshMenuBar();
        }
    }
}
