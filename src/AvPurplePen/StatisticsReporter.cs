// StatisticsReporter.cs
//
// Reports anonymous application-invocation statistics, and unhandled-exception
// reports, to the Purple Pen monitoring service without delaying application
// startup.

using PurplePen;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AvPurplePen
{
    /// <summary>
    /// Reports anonymous information about a Purple Pen application invocation,
    /// and about unhandled exceptions when the crash handler asks it to.
    /// </summary>
    public class StatisticsReporter
    {
        private const string statisticsEndpoint = "http://monitor.purple-pen.org/api/Invocation";
        private const string exceptionEndpoint = "http://monitor.purple-pen.org/api/Exception";

        /// <summary>
        /// The most exception reports to send in one run of the application. This bounds how
        /// much traffic a badly misbehaving session can generate: a fault that repeats on every
        /// frame could otherwise produce reports indefinitely.
        /// </summary>
        private const int maxExceptionReportsPerSession = 10;

        private readonly IHttpClientFactory httpClientFactory;

        // How many exception reports have been sent this run. This class is registered as a
        // singleton, so the instance field is effectively per-process. Updated with Interlocked
        // because reports can be requested from the UI thread, a background thread, and the
        // finalizer thread (unobserved task exceptions).
        private int exceptionReportsSent;

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
            MachineInformation payload = GetMachineInformation();

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
        /// Reports an unhandled exception to the monitoring service, completing when the
        /// attempt has finished. Unlike <see cref="ReportStatistics"/>, which is fire and
        /// forget, this returns a task, because the crash handler has to be able to wait
        /// for the report to be sent before terminating the process on the Restart path.
        /// This method never throws: reporting a crash must not be able to cause one.
        /// </summary>
        /// <param name="exception">The unhandled exception being reported.</param>
        /// <param name="userDescription">
        /// What the user typed about what they were doing when the error occurred. May be
        /// null or empty if the user chose not to say anything. This is the only part of the
        /// report that isn't derivable from the exception itself, and so it is the most
        /// valuable part of it.
        /// </param>
        /// <param name="cancellationToken">Cancels the outstanding HTTP request.</param>
        /// <returns>
        /// True if the monitoring service accepted the report; false on any failure, and false
        /// once this run has already sent <see cref="maxExceptionReportsPerSession"/> reports.
        /// </returns>
        public async Task<bool> ReportExceptionAsync(Exception exception,
                                                    string userDescription,
                                                    CancellationToken cancellationToken = default)
        {
            if (exception == null)
                return false;

            // The per-session limit is enforced here rather than in the callers, so that it
            // covers every path that can send a report, including any added later. Attempts are
            // counted rather than successes: a failed send still costs the service a request
            // (several, given the client's retry policy), which is what the limit exists to bound.
            if (Interlocked.Increment(ref exceptionReportsSent) > maxExceptionReportsPerSession)
                return false;

            try {
                ExceptionReport payload = BuildExceptionReport(exception, userDescription);

                // PostAsJsonAsync handles JSON escaping, UTF-8 encoding, and the
                // application/json content type.
                using HttpClient client = httpClientFactory.CreateClient();
                HttpResponseMessage response =
                    await client.PostAsJsonAsync(exceptionEndpoint, payload, cancellationToken).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception) {
                // No internet connection, no service, a cancellation, or a serialization
                // problem. A lost crash report is unfortunate but is never worth throwing
                // a second exception out of the crash handler over.
                return false;
            }
        }

        /// <summary>
        /// Builds the payload sent to the exception-reporting endpoint.
        /// </summary>
        /// <param name="exception">The unhandled exception being reported.</param>
        /// <param name="userDescription">What the user typed; may be null or empty.</param>
        /// <returns>The report to serialize as JSON.</returns>
        private static ExceptionReport BuildExceptionReport(Exception exception, string userDescription)
        {
            return new ExceptionReport {
                // GetType().FullName rather than Name, so that two same-named exception
                // types from different namespaces can be told apart on the server.
                ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                Message = exception.Message ?? "",

                // ToString() includes the message, the stack trace, and the full chain of
                // inner exceptions -- everything needed to diagnose the problem by hand.
                ExceptionText = exception.ToString(),

                // Sent separately as well, so the server can index and group on it without
                // having to parse it back out of ExceptionText.
                StackTrace = exception.StackTrace ?? "",
                HResult = exception.HResult,
                UserDescription = userDescription ?? "",
                MachineInformation = GetMachineInformation()
            };
        }

        private static MachineInformation GetMachineInformation()
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

            // Preserve the machineInformation used by the legacy updater so the monitoring
            // service receives exactly the fields and values it already expects.
            MachineInformation machineInformation = new MachineInformation {
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

            return machineInformation;
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
        /// Defines the JSON machineInformation accepted by the invocation-statistics endpoint.
        /// </summary>
        private sealed class MachineInformation
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

        /// <summary>
        /// Defines the JSON payload accepted by the exception-reporting endpoint. The
        /// MachineInformation sub-object is exactly the payload that the invocation
        /// endpoint already receives, so the monitoring service can share its parsing
        /// between the two kinds of report.
        /// </summary>
        private sealed class ExceptionReport
        {
            [JsonPropertyName("ExceptionType")]
            public string ExceptionType { get; init; } = "";

            [JsonPropertyName("Message")]
            public string Message { get; init; } = "";

            [JsonPropertyName("ExceptionText")]
            public string ExceptionText { get; init; } = "";

            [JsonPropertyName("StackTrace")]
            public string StackTrace { get; init; } = "";

            [JsonPropertyName("HResult")]
            public int HResult { get; init; }

            /// <summary>
            /// What the user typed into the crash dialog describing what they were doing.
            /// Empty if the user chose not to say anything.
            /// </summary>
            [JsonPropertyName("UserDescription")]
            public string UserDescription { get; init; } = "";

            [JsonPropertyName("MachineInformation")]
            public MachineInformation MachineInformation { get; init; } = new MachineInformation();
        }
    }
}
