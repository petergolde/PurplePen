// WebsiteLauncherService.cs
//
// Implementation of IWebsiteLauncher for Avalonia.
// Uses the current top-level window's Launcher to open the URL in the
// user's default web browser.

using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using PurplePen;

namespace AvPurplePen
{
    /// <summary>
    /// Opens URLs in the user's default web browser via the Avalonia
    /// top-level Launcher.
    /// </summary>
    public class WebsiteLauncherService : IWebsiteLauncher
    {
        /// <summary>
        /// The top-level window used to access the platform Launcher. Resolved
        /// dynamically from the desktop lifetime (rather than captured once)
        /// because the main window changes when the welcome screen hands off to
        /// the real main window.
        /// </summary>
        private static TopLevel RootTopLevel
        {
            get {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                        && desktop.MainWindow != null) {
                    return desktop.MainWindow;
                }
                throw new InvalidOperationException("WebsiteLauncherService requires a desktop main window to launch a website.");
            }
        }

        /// <summary>
        /// Launches the user's web browser, navigating to the given URL.
        /// </summary>
        /// <param name="url">The URL to navigate to.</param>
        /// <returns>A task that completes when the launch has been attempted.</returns>
        public async Task ShowWebsite(string url)
        {
            await RootTopLevel.Launcher.LaunchUriAsync(new Uri(url));
        }
    }
}
