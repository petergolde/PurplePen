using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.IO;

namespace AvPurplePen
{
    /// <summary>
    /// Central configuration for the native web view environment. Every NativeWebView
    /// in the application must be passed to <see cref="Configure"/> before it is
    /// attached, because the platform defaults are wrong for Purple Pen on both
    /// Windows and Linux. Avalonia has no application-wide hook for this -- the
    /// EnvironmentRequested event is per-control -- so the settings live here and each
    /// web view subscribes individually.
    /// </summary>
    public static class WebViewEnvironment
    {
        // Subfolders of the user's local application data holding the WebView2 user data
        // folder. LocalApplicationData rather than ApplicationData (which holds
        // PurplePenSettings.json): this is a browser cache that can grow to tens of
        // megabytes, so it must not roam.
        private const string applicationFolderName = "PurplePen";
        private const string userDataFolderName = "WebView2";

        /// <summary>
        /// Applies Purple Pen's web view environment settings to a web view. Must be
        /// called before the control is attached to the visual tree, so the constructor
        /// of the containing dialog is the right place.
        /// </summary>
        /// <param name="webView">The web view to configure.</param>
        public static void Configure(NativeWebView webView)
        {
            webView.EnvironmentRequested += (sender, args) =>
            {
                if (args is WindowsWebView2EnvironmentRequestedEventArgs webView2) {
                    // WebView2 defaults its user data folder to "<path to exe>.WebView2", which
                    // is inside Program Files for an installed copy and so is not writable by a
                    // normal user -- environment creation then fails with E_ACCESSDENIED. Point
                    // it at local application data instead. See
                    // https://learn.microsoft.com/microsoft-edge/webview2/concepts/user-data-folder
                    webView2.UserDataFolder = GetWebView2UserDataFolder();
                }
                else if (args is LinuxWpeWebViewEnvironmentRequestedEventArgs wpe) {
                    // On Linux, NativeWebView's default WPE WebKit backend does not render
                    // content; ask for WebKitGTK instead, which Print relies on there.
                    wpe.PreferWebKitGtkInstead = true;
                }
            };
        }

        /// <summary>
        /// Returns the directory WebView2 should use for its user data folder, creating
        /// it if it does not already exist.
        /// </summary>
        private static string GetWebView2UserDataFolder()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                         applicationFolderName, userDataFolderName);
            Directory.CreateDirectory(folder);
            return folder;
        }
    }
}
