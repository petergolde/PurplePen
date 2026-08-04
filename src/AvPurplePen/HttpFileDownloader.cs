// HttpFileDownloader.cs
//
// The real IFileDownloader used by the updater: downloads over HTTP, reporting
// progress as it goes. CoreUpdater takes this as an interface so that its own
// tests can run without a network; this is the implementation the application
// actually uses.

using PurplePen;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AvPurplePen
{
    /// <summary>
    /// Downloads files over HTTP for <see cref="CoreUpdater"/>, reporting progress against the
    /// response's Content-Length.
    /// </summary>
    public class HttpFileDownloader : IFileDownloader
    {
        /// <summary>
        /// Size of the buffer used to copy the response body. Also decides how often progress is
        /// reported, since one report is made per buffer written.
        /// </summary>
        private const int bufferSize = 65536;

        private readonly IHttpClientFactory httpClientFactory;

        /// <summary>
        /// Initializes a downloader that obtains HTTP clients from the application's shared client
        /// factory, so downloads get the same resilience policy as every other request the
        /// application makes.
        /// </summary>
        /// <param name="httpClientFactory">The factory used to create HTTP clients.</param>
        public HttpFileDownloader(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        /// <summary>
        /// Downloads the contents of a URL into a stream, reporting progress and honouring
        /// cancellation. The stream is written from its current position and is not closed.
        /// </summary>
        /// <param name="url">What to download.</param>
        /// <param name="destinationStream">Where to write it.</param>
        /// <param name="progress">
        /// Receives the fraction downloaded, from 0.0 to 1.0, or null when the server didn't say how
        /// big the file is. May itself be null when the caller doesn't want progress.
        /// </param>
        /// <param name="cancellationToken">Cancels the download.</param>
        /// <returns>A task that completes when the whole body has been written.</returns>
        /// <exception cref="HttpRequestException">The request failed, or returned an error status.</exception>
        /// <exception cref="OperationCanceledException">The download was cancelled.</exception>
        public async Task DownloadFile(string url, Stream destinationStream, IProgress<double?> progress, CancellationToken cancellationToken)
        {
            using HttpClient client = httpClientFactory.CreateClient();

            if (progress != null)
                progress.Report(0);

            // ResponseHeadersRead so the body is streamed rather than buffered in memory first --
            // an installer can be a hundred megabytes, and progress would otherwise jump from
            // nothing to everything.
            using HttpResponseMessage response =
                await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();

            // Absent or zero for a chunked response, in which case there is no way to know how far
            // along the download is and progress is reported as null throughout. Unwrapped into a
            // plain long here so the rest of the method doesn't have to keep re-testing HasValue.
            long? contentLength = response.Content.Headers.ContentLength;
            bool lengthKnown = contentLength.HasValue && contentLength.Value > 0;
            long totalBytes = lengthKnown ? contentLength!.Value : 0;

            if (progress != null)
                progress.Report(lengthKnown ? 0 : (double?)null);

            using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            byte[] buffer = new byte[bufferSize];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0) {
                await destinationStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalRead += bytesRead;

                if (progress != null)
                    progress.Report(lengthKnown ? ((double)totalRead / totalBytes) : (double?)null);
            }

            // A server that reported a length but sent less than that has given us a truncated file.
            // Saying so here is better than letting it surface later as a hash mismatch, which
            // suggests a corrupt or tampered-with download rather than an interrupted one.
            if (lengthKnown && totalRead < totalBytes) {
                throw new IOException(
                    string.Format("The download ended early: expected {0} bytes but received {1}.", totalBytes, totalRead));
            }

            if (progress != null)
                progress.Report(1);
        }
    }
}
