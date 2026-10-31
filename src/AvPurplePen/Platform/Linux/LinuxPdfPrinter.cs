// LinuxPdfPrinter.cs
//
// Prints PDF files on Linux through the XDG Desktop Portal (org.freedesktop.portal.Print).
// The portal shows the desktop's own print dialog (printer selection and options) and then
// spools the PDF to the chosen printer. The Print method expects its content to be a PDF,
// so the file is passed to the portal as-is, with no conversion.
// Based on the PdfTest sample's PdfPrinter.
//
// Spec: https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Print.html
//
// Only compiled for the plain net10.0 target framework (see AvPurplePen.csproj).

using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Tmds.DBus.Protocol;

namespace AvPurplePen.Platform.Linux
{
    /// <summary>
    /// Prints PDF files through the XDG Desktop Portal's Print interface.
    /// </summary>
    internal static class LinuxPdfPrinter
    {
        private const string PortalDestination = "org.freedesktop.portal.Desktop";
        private const string PortalPath = "/org/freedesktop/portal/desktop";
        private const string PrintInterface = "org.freedesktop.portal.Print";
        private const string RequestInterface = "org.freedesktop.portal.Request";

        // Portal Request.Response codes.
        private const uint ResponseSuccess = 0;
        private const uint ResponseCancelled = 1;

        // How long to wait for the session bus and the portal when checking whether the portal is available.
        private static readonly TimeSpan AvailabilityTimeout = TimeSpan.FromSeconds(5);

        // Writes the arguments of a portal method call. options already contains the handle_token,
        // and the delegate adds any other options before writing it.
        private delegate void WriteArgs(ref MessageWriter writer, Dictionary<string, VariantValue> options);

        /// <summary>
        /// Returns whether the Print portal can be used: there is a session bus, the portal service is
        /// running (or can be started by D-Bus activation), and one of its backends implements Print.
        /// Never throws. Use this to decide on a fallback before calling PrintPdfAsync; a failure from
        /// PrintPdfAsync itself means printing was attempted and failed, and shouldn't trigger a fallback.
        /// </summary>
        public static async Task<bool> IsAvailableAsync()
        {
            string? address = DBusAddress.Session;
            if (string.IsNullOrEmpty(address))
                return false;

            try {
                using (DBusConnection connection = new DBusConnection(address)) {
                    await connection.ConnectAsync().AsTask().WaitAsync(AvailabilityTimeout);

                    // Reading the interface's version property fails if the portal service doesn't exist
                    // or no configured backend implements Print, in which case the interface isn't exported.
                    MessageBuffer request;
                    MessageWriter writer = connection.GetMessageWriter();
                    try {
                        writer.WriteMethodCallHeader(PortalDestination, PortalPath, "org.freedesktop.DBus.Properties", "Get", "ss", MessageFlags.None);
                        writer.WriteString(PrintInterface);
                        writer.WriteString("version");
                        request = writer.CreateMessage();
                    }
                    finally {
                        writer.Dispose();
                    }

                    await connection.CallMethodAsync(request).WaitAsync(AvailabilityTimeout);
                    return true;
                }
            }
            catch (Exception) {
                return false;
            }
        }

        /// <summary>
        /// Shows the desktop's print dialog, then prints the PDF if the user confirms.
        /// </summary>
        /// <param name="pdfPath">The PDF file to print. It can be deleted as soon as this method completes.</param>
        /// <param name="parent">The window to attach the print dialog to, or null for none.</param>
        /// <param name="jobTitle">The print job title, or null to use the file name.</param>
        /// <param name="acceptLabel">The label of the print dialog's button that prints.</param>
        /// <param name="initialSettings">Settings to pre-fill the dialog with, using GtkPrintSettings keys
        /// (e.g. "n-copies", "duplex"). Keys prefixed with "cups-" are passed through to CUPS as job options
        /// without the prefix, so "cups-print-scaling" = "none" becomes the IPP option print-scaling=none.</param>
        /// <returns>True if the job was sent to the printer, false if the user cancelled.</returns>
        /// <exception cref="Exception">The file can't be opened, the portal can't be reached, or the portal reports a failure.</exception>
        public static async Task<bool> PrintPdfAsync(string pdfPath, TopLevel? parent, string? jobTitle, string acceptLabel,
            IReadOnlyDictionary<string, string>? initialSettings)
        {
            // Open the file first so a bad path fails before any dialog is shown. The portal receives a file
            // descriptor, not the path, and keeps its own copy of it, so the file can be deleted once we return.
            using (SafeFileHandle fileHandle = File.OpenHandle(pdfPath, FileMode.Open, FileAccess.Read)) {
                string title = jobTitle ?? Path.GetFileName(pdfPath);
                string parentWindow = GetParentWindowIdentifier(parent);

                string address = DBusAddress.Session ?? throw new InvalidOperationException("No D-Bus session bus is available.");

                // Uses its own connection rather than the shared DBusConnection.Session, because the request path
                // is built from the connection's unique bus name, which shared auto-connect connections don't expose.
                using (DBusConnection connection = new DBusConnection(address)) {
                    await connection.ConnectAsync();

                    Dictionary<string, VariantValue> settingsArg = new Dictionary<string, VariantValue>();
                    if (initialSettings != null) {
                        foreach (KeyValuePair<string, string> setting in initialSettings)
                            settingsArg[setting.Key] = VariantValue.String(setting.Value);
                    }

                    // Step 1: PreparePrint shows the print dialog and returns a token for the chosen settings.
                    (uint prepareResponse, Dictionary<string, VariantValue> prepareResults) = await CallPortalRequestAsync(connection, "PreparePrint", "ssa{sv}a{sv}a{sv}",
                        (ref MessageWriter writer, Dictionary<string, VariantValue> options) => {
                            options["modal"] = VariantValue.Bool(true);
                            options["accept_label"] = VariantValue.String(acceptLabel);

                            writer.WriteString(parentWindow);
                            writer.WriteString(title);
                            writer.WriteDictionary(settingsArg);
                            writer.WriteDictionary(new Dictionary<string, VariantValue>()); // page_setup
                            writer.WriteDictionary(options);
                        });

                    if (prepareResponse == ResponseCancelled)
                        return false;
                    if (prepareResponse != ResponseSuccess)
                        throw new InvalidOperationException($"Print dialog failed (portal response {prepareResponse}).");

                    uint token = prepareResults["token"].GetUInt32();

                    // Step 2: Print sends the PDF, using the settings captured by the token.
                    (uint printResponse, _) = await CallPortalRequestAsync(connection, "Print", "ssha{sv}",
                        (ref MessageWriter writer, Dictionary<string, VariantValue> options) => {
                            options["token"] = VariantValue.UInt32(token);
                            options["modal"] = VariantValue.Bool(true);

                            writer.WriteString(parentWindow);
                            writer.WriteString(title);
                            writer.WriteHandle(fileHandle);
                            writer.WriteDictionary(options);
                        });

                    if (printResponse == ResponseCancelled)
                        return false;
                    if (printResponse != ResponseSuccess)
                        throw new InvalidOperationException($"Printing failed (portal response {printResponse}).");

                    return true;
                }
            }
        }

        // Calls a portal method that returns a Request object, and waits for that request's Response signal.
        // Returns the response code and the results.
        private static async Task<(uint Response, Dictionary<string, VariantValue> Results)> CallPortalRequestAsync(
            DBusConnection connection, string method, string signature, WriteArgs writeArgs)
        {
            // The portal creates the Request object at a path derived from our unique bus name and
            // handle_token, so we can subscribe to its Response signal before making the call.
            string handleToken = "PurplePen_" + Guid.NewGuid().ToString("N");
            string sender = connection.UniqueName!.TrimStart(':').Replace('.', '_');
            string requestPath = $"{PortalPath}/request/{sender}/{handleToken}";

            TaskCompletionSource<(uint, Dictionary<string, VariantValue>)> responseSource = new TaskCompletionSource<(uint, Dictionary<string, VariantValue>)>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            using (IDisposable watcher = await connection.WatchSignalAsync(PortalDestination, requestPath, RequestInterface, "Response",
                (Message message, object? _) => {
                    Reader reader = message.GetBodyReader();
                    uint response = reader.ReadUInt32();
                    Dictionary<string, VariantValue> results = reader.ReadDictionaryOfStringToVariantValue();
                    return (response, results);
                },
                (Notification<(uint, Dictionary<string, VariantValue>)> notification) => {
                    if (notification.HasValue)
                        responseSource.TrySetResult(notification.Value);
                    else if (notification.Exception != null)
                        responseSource.TrySetException(notification.Exception);
                    else if (notification.IsCompletion)
                        responseSource.TrySetException(new InvalidOperationException("Portal request ended without a response."));
                },
                ObserverFlags.EmitAll, emitOnCapturedContext: false, state: null)) {

                MessageBuffer request;
                MessageWriter writer = connection.GetMessageWriter();
                try {
                    writer.WriteMethodCallHeader(PortalDestination, PortalPath, PrintInterface, method, signature, MessageFlags.None);
                    Dictionary<string, VariantValue> options = new Dictionary<string, VariantValue> { ["handle_token"] = VariantValue.String(handleToken) };
                    writeArgs(ref writer, options);
                    request = writer.CreateMessage();
                }
                finally {
                    writer.Dispose();
                }

                string returnedPath = await connection.CallMethodAsync(request,
                    (Message message, object? _) => message.GetBodyReader().ReadObjectPathAsString(), null);

                // Portals older than version 0.9 ignore handle_token and use a different path.
                if (returnedPath != requestPath)
                    throw new InvalidOperationException($"Unexpected portal request path '{returnedPath}'.");

                return await responseSource.Task;
            }
        }

        // Get the portal parent window identifier: "x11:<XID in hex>" on X11 (including XWayland). A native
        // Wayland parent would need an xdg-foreign exported handle, which Avalonia doesn't provide, so no
        // parent is passed there, and the dialog isn't attached to the window.
        private static string GetParentWindowIdentifier(TopLevel? parent)
        {
            IPlatformHandle? handle = parent?.TryGetPlatformHandle();
            if (handle != null && handle.HandleDescriptor == "XID")
                return $"x11:{handle.Handle:x}";
            return string.Empty;
        }
    }
}
