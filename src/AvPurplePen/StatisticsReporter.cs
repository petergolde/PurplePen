// StatisticsReporter.cs
//
// Reports anonymous application-invocation statistics to the Purple Pen
// monitoring service without delaying application startup.

using PurplePen;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AvPurplePen
{
    /// <summary>
    /// Reports anonymous information about a Purple Pen application invocation.
    /// </summary>
    public class StatisticsReporter
    {
        private const string statisticsEndpoint = "http://monitor.purple-pen.org/api/Invocation";
        private readonly IHttpClientFactory httpClientFactory;

        /// <summary>
        /// Initializes a statistics reporter that obtains HTTP clients from the
        /// application's shared client factory.
        /// </summary>
        /// <param name="httpClientFactory">The factory used to create HTTP clients for reporting.</param>
        public StatisticsReporter(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        /// <summary>
        /// Starts a background task that collects and reports invocation statistics.
        /// This method returns immediately and never displays an error to the user.
        /// </summary>
        public void ReportStatistics()
        {
            // Collection is intentionally performed inside Task.Run as some system
            // properties can involve operating-system calls. The HTTP request is also
            // made there so reporting cannot delay the calling thread during startup.
            _ = Task.Run(CollectAndReportStatisticsAsync);
        }

        /// <summary>
        /// Collects the invocation statistics and sends them to the monitoring service.
        /// This method is run on a background thread by <see cref="ReportStatistics"/>.
        /// </summary>
        /// <returns>A task that completes after the report attempt finishes.</returns>
        private async Task CollectAndReportStatisticsAsync()
        {
            // Use the configured UI language when available. An empty setting
            // means the application is following the operating-system culture.
            string uiLanguage = UserSettings.Current.UILanguage;
            if (string.IsNullOrEmpty(uiLanguage))
                uiLanguage = CultureInfo.CurrentUICulture.Name;

            string versionString = VersionNumber.Current;
#if MSSTORE
            versionString += "S";   // Add S to indicate the Microsoft Store version.
#endif

            // Report both architectures when the process architecture differs from
            // the operating system, such as an x64 process running on ARM64.
            string architecture;
            if (RuntimeInformation.OSArchitecture == RuntimeInformation.ProcessArchitecture)
                architecture = RuntimeInformation.OSArchitecture.ToString();
            else
                architecture = RuntimeInformation.ProcessArchitecture + " on " + RuntimeInformation.OSArchitecture;

            // Preserve the payload used by the legacy updater so the monitoring
            // service receives exactly the fields and values it already expects.
            StatisticsPayload payload = new StatisticsPayload {
                Version = versionString,
                Locale = CultureInfo.CurrentCulture.Name,
                TimeZone = TimeZoneInfo.Local.StandardName,
                UILang = uiLanguage,
                ClientId = UserSettings.Current.ClientId.ToString(),
                OSVersion = GetOSVersion(),
                Framework = RuntimeInformation.FrameworkDescription,
                RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier,
                Architecture = architecture
            };

            try {
                // PostAsJsonAsync handles JSON escaping, UTF-8 encoding, and the
                // application/json content type.
                using HttpClient client = httpClientFactory.CreateClient();
                await client.PostAsJsonAsync(statisticsEndpoint, payload).ConfigureAwait(false);
            }
            catch (Exception) {
                // Statistics are nonessential, so ignore exceptions. We are using resiliance on 
                // our HttpClient already, which is the best we can do. If the statistics aren't recorded,
                // no big deal. For example, the user might have no internet connection.
            }
        }

        /// <summary>
        /// Gets a description of the current operating system and its version.
        /// </summary>
        /// <returns>A platform-appropriate operating-system description.</returns>
        private static string GetOSVersion()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetWindowsVersion();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GetMacVersion();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return GetLinuxVersion();

            return RuntimeInformation.OSDescription;
        }

        /// <summary>
        /// Gets a descriptive Windows version string, including OS architecture.
        /// This implementation is copied from CrashReporter.NET so AvPurplePen does
        /// not need to reference the legacy crash-reporting project.
        /// </summary>
        /// <returns>A description of the detected Windows version.</returns>
        private static string GetWindowsVersion()
        {
            string osArchitecture;
            Version windowsVersion = Environment.OSVersion.Version;

            try {
                osArchitecture = IsOS64Bit() ? "64" : "32";
            }
            catch (Exception) {
                // Architecture detection is useful but not essential to the report.
                osArchitecture = "Undetermined";
            }

            switch (windowsVersion.Major) {
                case 5:
                    switch (windowsVersion.Minor) {
                        case 0:
                            return string.Format("Windows 2000 {0} {1} Version {2}",
                                                 Environment.OSVersion.ServicePack, osArchitecture,
                                                 windowsVersion);
                        case 1:
                            return string.Format("Windows XP {0} {1} Version {2}",
                                                 Environment.OSVersion.ServicePack, osArchitecture,
                                                 windowsVersion);
                        case 2:
                            return string.Format(
                                "Windows XP x64 Professional Edition / Windows Server 2003 {0} {1} Version {2}",
                                Environment.OSVersion.ServicePack, osArchitecture, windowsVersion);
                    }
                    break;

                case 6:
                    switch (windowsVersion.Minor) {
                        case 0:
                            return string.Format("Windows Vista {0} {1} bit Version {2}",
                                                 Environment.OSVersion.ServicePack, osArchitecture,
                                                 windowsVersion);
                        case 1:
                            return string.Format("Windows 7 {0} {1} bit Version {2}",
                                                 Environment.OSVersion.ServicePack, osArchitecture,
                                                 windowsVersion);
                        case 2:
                            return string.Format("Windows 8 {0} {1} bit Version {2}",
                                                 Environment.OSVersion.ServicePack, osArchitecture,
                                                 windowsVersion);
                        case 3:
                            return string.Format("Windows 8.1 {0} {1} bit Version {2}",
                                                 Environment.OSVersion.ServicePack, osArchitecture,
                                                 windowsVersion);
                    }
                    break;

                case 10:
                    if (windowsVersion.Minor == 0) {
                        return string.Format("Windows 10 {0} {1} bit Version {2}",
                                             Environment.OSVersion.ServicePack, osArchitecture,
                                             windowsVersion);
                    }
                    break;
            }

            return string.Format("Unknown {0} bit Version {1}", osArchitecture, windowsVersion);
        }

        /// <summary>
        /// Gets a macOS version description.
        /// </summary>
        /// <returns>The macOS name followed by its operating-system version.</returns>
        private static string GetMacVersion()
        {
            return "MacOS " + Environment.OSVersion.Version;
        }

        /// <summary>
        /// Gets a Linux version description.
        /// </summary>
        /// <returns>The Linux name followed by the runtime's operating-system description.</returns>
        private static string GetLinuxVersion()
        {
            return "Linux: " + RuntimeInformation.OSDescription;
        }

        /// <summary>
        /// Determines whether the operating system is 64-bit.
        /// </summary>
        /// <returns><see langword="true"/> for a 64-bit operating system; otherwise, <see langword="false"/>.</returns>
        private static bool IsOS64Bit()
        {
            return Environment.Is64BitOperatingSystem;
        }

        /// <summary>
        /// Defines the JSON payload accepted by the invocation-statistics endpoint.
        /// </summary>
        private sealed class StatisticsPayload
        {
            [JsonPropertyName("Version")]
            public string Version { get; init; } = "";

            [JsonPropertyName("Locale")]
            public string Locale { get; init; } = "";

            [JsonPropertyName("TimeZone")]
            public string TimeZone { get; init; } = "";

            [JsonPropertyName("UILang")]
            public string UILang { get; init; } = "";

            [JsonPropertyName("ClientId")]
            public string ClientId { get; init; } = "";

            [JsonPropertyName("OSVersion")]
            public string OSVersion { get; init; } = "";

            [JsonPropertyName("Framework")]
            public string Framework { get; init; } = "";

            [JsonPropertyName("RuntimeIdentifier")]
            public string RuntimeIdentifier { get; init; } = "";

            [JsonPropertyName("Architecture")]
            public string Architecture { get; init; } = "";
        }
    }
}
