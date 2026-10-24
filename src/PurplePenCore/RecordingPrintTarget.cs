using PurplePen.Graphics2D;
using System;
using System.Collections.Generic;
using System.Threading;

namespace PurplePen
{
    // A printing target that records each printed page instead of outputting it. Each recorded page can
    // then be drawn (played back) onto any graphics target, any number of times. This is used for print
    // preview: print once to this target, then play back each page whenever it needs to be displayed.
    //
    // Pages may be drawn concurrently from multiple threads (e.g., background rendering threads).
    // Dispose() releases the recordings. If any page is being drawn at the time, the release is deferred
    // until that drawing finishes. Once Dispose() has been called, drawing a page does nothing.
    public sealed class RecordingPrintTarget : IPrintingTarget, IDisposable
    {
        private readonly float dpi;
        private readonly IGraphicsBitmapLoader bitmapLoader;   // null to use Services.BitmapLoader.
        private readonly List<RecordedPrintPage> pages = new List<RecordedPrintPage>();
        private string documentTitle = "";

        private readonly object syncLock = new object();
        private int activeDrawCount = 0;          // Number of page drawings currently in progress.
        private bool disposeRequested = false;    // Has Dispose been called?
        private bool recordingsDisposed = false;  // Have the recordings actually been disposed?

        // Create a new recording print target.
        // dpi: the printer resolution to report to the printing code, in dots per inch. Some printing code
        //      (e.g., map rendering) varies its output by resolution, so this should match the resolution
        //      of the real output being previewed. The default matches PDF output.
        public RecordingPrintTarget(float dpi = PdfPrintTarget.PdfDpi)
            : this(null, dpi)
        {
        }

        // Create a new recording print target that uses a specific bitmap loader for the page recordings.
        // bitmapLoader: loads recorded bitmaps when pages are drawn; if null, Services.BitmapLoader is used. It must
        //               create bitmaps that the targets the pages are drawn onto can draw (e.g., a SkiaBitmapGraphicsLoader
        //               when drawing onto Skia graphics targets). A mismatch causes bitmaps to be omitted
        //               (silently in Release builds).
        // dpi: the printer resolution to report to the printing code, in dots per inch (see the other constructor).
        public RecordingPrintTarget(IGraphicsBitmapLoader bitmapLoader, float dpi = PdfPrintTarget.PdfDpi)
        {
            this.bitmapLoader = bitmapLoader;
            this.dpi = dpi;
        }

        // The document title passed when printing started.
        public string DocumentTitle {
            get { return documentTitle; }
        }

        // The recorded pages, in the order printed.
        public IReadOnlyList<RecordedPrintPage> Pages {
            get { return pages; }
        }

        // Called at the beginning of printing.
        // documentTitle: the title of the document.
        // pageCount: the number of pages that will be printed.
        public void StartPrinting(string documentTitle, int pageCount)
        {
            CheckNotDisposed();
            this.documentTitle = documentTitle ?? "";
        }

        // Get the resolution that the printing code should assume, in dots per inch.
        public float GetPrinterDpi()
        {
            return dpi;
        }

        // Called at the end of printing. Nothing to do.
        public void EndPrinting()
        {
        }

        // Record a page.
        // pageNumber: the page number, starting at 1.
        // paperSize: the paper size of the page.
        // drawPage: draws the page onto the given graphics target, in hundredths of an inch from the top-left corner of the paper.
        public void PrintPage(int pageNumber, PrintingPaperSize paperSize, Action<IGraphicsTarget> drawPage)
        {
            CheckNotDisposed();

            RecordingGraphicsTarget recording = new RecordingGraphicsTarget(bitmapLoader ?? Services.BitmapLoader);
            try {
                drawPage(recording);
                recording.EndRecording();
            }
            catch {
                recording.Dispose();
                throw;
            }

            pages.Add(new RecordedPrintPage(this, pageNumber, paperSize, recording));
        }

        // Play back a recording onto a target, unless this object has been disposed.
        // recording: the recording of the page to play back.
        // target: the graphics target to play back onto.
        // cancellationToken: cancels the playback (an OperationCanceledException is thrown).
        // Returns true if the page was drawn, false if this object has been disposed.
        internal bool PlaybackPage(RecordingGraphicsTarget recording, IGraphicsTarget target, CancellationToken cancellationToken)
        {
            lock (syncLock) {
                if (disposeRequested)
                    return false;
                ++activeDrawCount;
            }

            try {
                recording.Playback(target, cancellationToken);
                return true;
            }
            finally {
                lock (syncLock) {
                    --activeDrawCount;
                    if (activeDrawCount == 0 && disposeRequested)
                        DisposeRecordings();
                }
            }
        }

        // Release the recorded pages. If a page is currently being drawn, the release happens when the drawing finishes.
        public void Dispose()
        {
            lock (syncLock) {
                if (disposeRequested)
                    return;

                disposeRequested = true;
                if (activeDrawCount == 0)
                    DisposeRecordings();
            }
        }

        // Dispose all the recordings. Must be called with syncLock held.
        private void DisposeRecordings()
        {
            if (recordingsDisposed)
                return;

            recordingsDisposed = true;
            foreach (RecordedPrintPage page in pages)
                page.DisposeRecording();
        }

        // Throw if Dispose has been called.
        private void CheckNotDisposed()
        {
            lock (syncLock) {
                if (disposeRequested)
                    throw new ObjectDisposedException(nameof(RecordingPrintTarget));
            }
        }
    }

    // A single page recorded by a RecordingPrintTarget.
    public sealed class RecordedPrintPage
    {
        private readonly RecordingPrintTarget owner;
        private readonly RecordingGraphicsTarget recording;

        // Create a recorded page.
        // owner: the RecordingPrintTarget that recorded the page, which controls the lifetime of the recording.
        // pageNumber: the page number, starting at 1.
        // paperSize: the paper size of the page.
        // recording: the recording of the page; must have ended recording.
        internal RecordedPrintPage(RecordingPrintTarget owner, int pageNumber, PrintingPaperSize paperSize, RecordingGraphicsTarget recording)
        {
            this.owner = owner;
            this.recording = recording;
            PageNumber = pageNumber;
            PaperSize = paperSize;
        }

        // The page number, starting at 1.
        public int PageNumber { get; }

        // The paper size (and thus orientation) of the page.
        public PrintingPaperSize PaperSize { get; }

        // Draw the page onto a graphics target. The coordinates are the same as when the page was printed: hundredths
        // of an inch from the top-left corner of the paper. The paper itself is not drawn; the caller should fill the
        // background with white first if needed. May be called concurrently from multiple threads onto different targets.
        // target: the graphics target to draw onto.
        // cancellationToken: cancels the drawing (an OperationCanceledException is thrown).
        // Returns true if the page was drawn, false if the owning RecordingPrintTarget has been disposed.
        public bool Draw(IGraphicsTarget target, CancellationToken cancellationToken = default)
        {
            return owner.PlaybackPage(recording, target, cancellationToken);
        }

        // Dispose the recording. Called only by the owning RecordingPrintTarget.
        internal void DisposeRecording()
        {
            recording.Dispose();
        }
    }
}
