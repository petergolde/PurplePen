// IWebsiteLauncher.cs
//
// Abstraction for launching a web browser to a specific URL.
// Lives in PurplePenCore so it can be accessed via Services.WebsiteLauncher.
// The implementation lives in the View layer (AvPurplePen) so that
// ViewModels and core code remain free of UI dependencies.

using System.Threading.Tasks;

namespace PurplePen
{
    /// <summary>
    /// Service for opening a URL in the user's default web browser.
    /// </summary>
    public interface IWebsiteLauncher
    {
        /// <summary>
        /// Launches the user's web browser, navigating to the given URL.
        /// </summary>
        /// <param name="url">The URL to navigate to.</param>
        /// <returns>A task that completes when the launch has been attempted.</returns>
        Task ShowWebsite(string url);
    }
}
