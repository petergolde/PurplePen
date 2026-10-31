//#define SHOWDEBUGOUTPUT

using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace AvUtil
{
    // Caches a drawing so it appears fully crisp at all resolutions. Calls a drawing function
    // that draws in the background, and caches the result. When the drawing is requested, it
    // first draws anything that is available, then starts a new render of the drawing at the
    // resolution requested and raised an event when the full resolution drawing is available.
    //
    // Adapts an IThreadsafeSkiaDrawing to an IAvaloniaDrawing by caching the result of the drawing function in a bitmap.
    //
    // The cached bitmaps are only released by Dispose, so the owner must Dispose a CachedDrawing when it
    // is no longer used (e.g., when replacing it with a new drawing, or when the control showing it goes away).
    public class CachedDrawing: IAvaloniaDrawing, IDisposable
    {
        private const int FullRenderSize = 1300;  // Size of the full render along the longest dimension.

        private readonly IThreadsafeSkiaDrawing underlyingDrawing;

        // List of all the renders that are in progress or complete that render the detailed
        // version of the drawing at the resolution requested.
        private List<InProgressRender> detailedRenders = new List<InProgressRender>();

        // List of all the renders that are in progress or complete that render 3x version of the
        // drawing at the 1/3 resolution requested.
        private List<InProgressRender> enclosingRenders = new List<InProgressRender>();

        // List of all the renders that are in progress or complete that render the full
        // drawing at a pre-defined resolution (FullRenderSize)
        private List<InProgressRender> fullRenders = new List<InProgressRender>();

        // The drawingVersion is incremented every time the underlying drawing
        // changes, and tracks which version of the underlying drawing was drawn.
        private int drawingVersion = 1;

        // The renderVersion is incremented every render and is used to find the latest
        // rendering.
        private int renderVersion = 1;

        // Keep track if an available drawing event has been posted to the UI thread,
        // so we don't post it multiple times.
        private volatile bool availableDrawingPosted = false;

        // True once Dispose has been called.
        private volatile bool disposed = false;

        public CachedDrawing(IThreadsafeSkiaDrawing drawing)
        {
            this.underlyingDrawing = drawing;
            this.underlyingDrawing.DrawingChanged += OnUnderlyingDrawingChanged;
        }

        // The event is raised when a new drawing is available. This occurs either:
        // 1. When the underlying drawing has changed, and a new render of the change
        //    is available.
        // 2. When a request for a drawing has been made, and the drawing is not already
        //    available at the resolution requested, the event is raised when the drawing
        //    is available.
        //
        // The event is always raised on the UI thread via a Post.
        public event EventHandler? DrawingChanged;

        public Rect Bounds => Conv.ToAvRect(underlyingDrawing.Bounds);

        // The passed in DrawingContext is assumed to be initialized with a transform
        // that maps "rectToDraw" to the visible surface. The best cached drawing that 
        // is available is drawn to that rectangle. If the best cached drawing is not the
        // full rectangle at the resolution requested, then a drawing of the full rectangle
        // is initiated on a background task, and the DrawingChanged event is raised
        // (on the UI thread) when the full drawing is available.
        public void Draw(DrawingContext drawingContext, Rect rectToDraw, PixelSize pixelSize, Matrix transformWorldToPixel, double layoutScale)
        {
            DebugPrint($"Draw: Rect:{rectToDraw} PixelSize:{pixelSize}");

            if (disposed)
                return;

            // Nothing is visible in an empty rectangle or pixel size, and rendering one would divide by zero
            // when creating the transform.
            if (IsEmptyRequest(rectToDraw, pixelSize))
                return;

            // Prune any completed renders that are no longer needed.
            PruneCompletedRenders(detailedRenders);
            PruneCompletedRenders(enclosingRenders);
            PruneCompletedRenders(fullRenders);

            InProgressRender? detailedRender = FindCompletedRender(detailedRenders);
            InProgressRender? enclosingRender = FindCompletedRender(enclosingRenders);
            InProgressRender? fullRender = FindCompletedRender(fullRenders);

            // If we have a detailed render that exactly matches the resolution requested, use it and 
            // we are done.
            if (detailedRender != null && detailedRender.drawingVersion == drawingVersion && 
                detailedRender.pixelSize == pixelSize && detailedRender.rect == rectToDraw) 
            {
                detailedRender.Draw(drawingContext, rectToDraw);
                DebugPrint("Found detailed render that matches exactly.");

                // Create an enclosing render if we don't have one, the current one is out of date, or the current one
                // doesn't enclose well. (BeginRender does nothing if a matching enclosing render is already in progress.)
                if (enclosingRender == null || enclosingRender.drawingVersion != drawingVersion ||
                    !enclosingRender.rect.Contains(rectToDraw.Inflate(new Thickness(rectToDraw.Width / 2, rectToDraw.Height / 2))) ||
                    enclosingRender.rect.Width > 5 * rectToDraw.Width ||
                    enclosingRender.rect.Height > 5 * rectToDraw.Height) 
                {
                    // The enclosing render is for a rectangle that is 3x the size of the current rectangle, centered on it, but
                    // with the same pixel dimensions (so 1/3 the resolution).
                    Rect enclosingRect = rectToDraw.Inflate(new Thickness(rectToDraw.Width, rectToDraw.Height));
                    BeginRender(enclosingRect, pixelSize, enclosingRenders, "enclosing");
                }

                return;
            }

            // If we have a detailed render that fully encloses the resolution requested, use only it.
            if (detailedRender != null && detailedRender.rect.Contains(rectToDraw)) {
                detailedRender.Draw(drawingContext, rectToDraw);
            }
            else {
                // Otherwise, draw the full render, then the enclosing, then detailed render on top.
                if (fullRender == null || !fullRender.rect.Contains(rectToDraw)) {
                    // Draw white background to fill the whole rectangle.
                    drawingContext.FillRectangle(Brushes.White, rectToDraw);    
                }

                if (enclosingRender == null || !enclosingRender.rect.Contains(rectToDraw)) {
                    // Only draw the full if the enclosing doesn't enclose.
                    fullRender?.Draw(drawingContext, rectToDraw);
                }
                enclosingRender?.Draw(drawingContext, rectToDraw);
                detailedRender?.Draw(drawingContext, rectToDraw);
            }

            // Start a new detailed render for the exactly bounds and resolution requested
            // (unless one is already in progress).
            BeginRender(rectToDraw, pixelSize, detailedRenders, "detailed");

            // If there is no full render, or it is out of date, create one (unless one is already in progress).
            if (fullRender == null || fullRender.drawingVersion != drawingVersion) {
                BeginFullRender();
            }

            DebugPrint($"End Draw: Rect:{rectToDraw} PixelSize:{pixelSize}");
        }

        // Request a render of the whole drawing, with its longer side FullRenderSize pixels (unless a matching
        // render is already pending, in progress, or completed). Does nothing if the drawing's bounds are empty
        // or not finite, since there is nothing to render.
        private void BeginFullRender()
        {
            SKRect fullBounds = underlyingDrawing.Bounds;
            if (!float.IsFinite(fullBounds.Width) || !float.IsFinite(fullBounds.Height) || fullBounds.Width <= 0 || fullBounds.Height <= 0)
                return;

            // The size of the full render has a longer side of "FullRenderSize". Each side is at least 1 pixel, so
            // that a very long, thin drawing still gets rendered.
            PixelSize fullPixelSize;
            if (fullBounds.Width > fullBounds.Height) {
                fullPixelSize = new PixelSize(FullRenderSize, Math.Max(1, (int)(FullRenderSize * fullBounds.Height / fullBounds.Width)));
            }
            else {
                fullPixelSize = new PixelSize(Math.Max(1, (int)(FullRenderSize * fullBounds.Width / fullBounds.Height)), FullRenderSize);
            }

            BeginRender(Conv.ToAvRect(fullBounds), fullPixelSize, fullRenders, "full");
        }

        // True if a request to draw or render the given rectangle at the given pixel size would have nothing in it:
        // the rectangle or pixel size is empty, or the rectangle is not finite.
        //   rect: the rectangle requested.
        //   pixelSize: the pixel size requested.
        private static bool IsEmptyRequest(Rect rect, PixelSize pixelSize)
        {
            return !double.IsFinite(rect.Width) || !double.IsFinite(rect.Height) || rect.Width <= 0 || rect.Height <= 0 ||
                   pixelSize.Width <= 0 || pixelSize.Height <= 0;
        }

        private InProgressRender? FindCompletedRender(List<InProgressRender> currentRenders)
        {
            return currentRenders.FirstOrDefault(render => render.IsCompleted);
        }

        // Find a render in the list of the given rectangle, pixel size, and drawing version that either has completed
        // successfully, or is still running or pending and has not been canceled. Returns null if there is no such render.
        //   currentRenders: list of renders to search.
        //   rect: the rectangle the render must be of.
        //   pixelSize: the pixel size the render must be of.
        //   version: the drawing version the render must be of.
        private static InProgressRender? FindMatchingRender(List<InProgressRender> currentRenders, Rect rect, PixelSize pixelSize, int version)
        {
            return currentRenders.FirstOrDefault(render => render.drawingVersion == version && render.rect == rect && render.pixelSize == pixelSize &&
                                                           (render.IsCompleted || ((render.InProgress || render.IsPending) && !render.CancelRequested)));
        }

        // Find the most recently requested render in the list, whatever its state (pending, in progress, completed, canceled, or faulted).
        // This is the best indication of the rectangle and pixel size most recently wanted. Returns null if the list is empty.
        //   currentRenders: list of renders to search.
        private static InProgressRender? FindLatestRender(List<InProgressRender> currentRenders)
        {
            return currentRenders.MaxBy(render => render.renderVersion);
        }

        // Request a new render of the drawing at the resolution requested, unless a matching
        // render (same rectangle, pixel size and drawing version) is already pending, in progress, or completed.
        // Cancels any other in-progress render, discards any other pending render, and prunes completed renders.
        //
        // At most one render in each list is running at a time, so that cancelled renders that haven't noticed
        // their cancellation yet don't pile up (each with its own bitmap). If a render is still running (perhaps
        // because it was just cancelled), the new render is left pending in the list, and is started by
        // StartPendingRender when the running render finishes.
        //   rectToDraw: the rectangle to render.
        //   pixelSize: the pixel size to render it at.
        //   currentRenders: the list of renders (detailed, enclosing, or full) that the render belongs to.
        //   type: type of the render, for debugging.
        private void BeginRender(Rect rectToDraw, PixelSize pixelSize, List<InProgressRender> currentRenders, string type)
        {
            // Prune any completed renders that are no longer needed.
            PruneCompletedRenders(currentRenders);

            // If the render requested is already pending, in progress or completed, there is nothing to do.
            InProgressRender? matchingRender = FindMatchingRender(currentRenders, rectToDraw, pixelSize, drawingVersion);
            if (matchingRender != null) {
                InProgressRender.RecordReused(type);
                return;
            }

            // Cancel any in-progress render that is still going on, and discard any pending render.
            CancelInProgressRenders(currentRenders);

            // Create a new render for the exact bounds and resolution requested. Start it now if nothing else in
            // the list is running; otherwise it stays pending until the running render finishes.
            InProgressRender? newRender = null;
            if (!IsEmptyRequest(rectToDraw, pixelSize)) {
                newRender = new InProgressRender(type, underlyingDrawing, drawingVersion, renderVersion, rectToDraw, pixelSize,
                                                 NotifyNewDrawingAvailable, () => OnRenderFinished(currentRenders));
                currentRenders.Add(newRender);

                if (!currentRenders.Any(render => render.InProgress)) {
                    newRender.Start();
                }
            }
            ++renderVersion;

            if (newRender != null) {
                DebugPrint((newRender.IsPending ? "Queued render: " : "Beginning render: ") + newRender.ToString());
            }
        }

        // Called on a thread pool thread whenever a render task finishes, in any state (completed, canceled, or faulted).
        // Always posts StartPendingRender to the UI thread, so that a pending render in the list is never left waiting.
        // (This must not be combined with NotifyNewDrawingAvailable, which doesn't always post.)
        //   currentRenders: the list of renders that the finished render belongs to.
        private void OnRenderFinished(List<InProgressRender> currentRenders)
        {
            Dispatcher.UIThread.Post(() => StartPendingRender(currentRenders), DispatcherPriority.Normal);
        }

        // Called on the UI thread after a render in the list has finished. If nothing in the list is running,
        // start the pending render, if there is one. It is harmless if this is called when it isn't needed: if
        // another render has been started in the meantime, that render's finishing will call this again.
        //   currentRenders: the list of renders that the finished render belongs to.
        private void StartPendingRender(List<InProgressRender> currentRenders)
        {
            if (disposed)
                return;

            if (currentRenders.Any(render => render.InProgress))
                return;

            InProgressRender? pendingRender = currentRenders.FirstOrDefault(render => render.IsPending);
            if (pendingRender != null) {
                pendingRender.Start();
                DebugPrint("Beginning pending render: " + pendingRender.ToString());
            }
        }

        private void NotifyNewDrawingAvailable()
        {
            if (DrawingChanged != null && !availableDrawingPosted && !disposed) {
                availableDrawingPosted = true;  // Prevent multiple posts until the event is handled.

                Dispatcher.UIThread.Post(() => {
                    // NewDrawingAvailable could have become null (or this disposed) between the check and the Post,
                    // so check again.
                    availableDrawingPosted = false;
                    if (!disposed)
                        DrawingChanged?.Invoke(this, EventArgs.Empty);
                }, DispatcherPriority.Render);
            }
        }

        // Prune all completed 
        private void PruneCompletedRenders(List<InProgressRender> currentRenders)
        {
            int latestVersion = (from render in currentRenders
                                 where render.IsCompleted
                                 select render.renderVersion).DefaultIfEmpty(0).Max();

            for (int i = currentRenders.Count - 1; i >= 0; --i) {
                if (currentRenders[i].IsFinished && currentRenders[i].renderVersion != latestVersion) {
                    currentRenders[i].Dispose();
                    currentRenders.RemoveAt(i);
                }
            }
        }

        // Cancel all in-progress renders in the given list (pruning any that finish), and discard all pending renders.
        private void CancelInProgressRenders(List<InProgressRender> currentRenders)
        {
            for (int i = currentRenders.Count - 1; i >= 0; --i) {
                if (currentRenders[i].IsPending) {
                    // Never started, so it has no bitmap and nothing to wait for.
                    currentRenders[i].Dispose();
                    currentRenders.RemoveAt(i);
                }
                else if (currentRenders[i].InProgress) {
                    currentRenders[i].CancelDrawing();
                    if (currentRenders[i].IsCompleted) {
                        currentRenders[i].Dispose();
                        currentRenders.RemoveAt(i);
                    }
                }
            }
        }

        // The underlying drawing has changed. Must be raised on the UI thread, because it changes the render
        // lists that Draw uses.
        private void OnUnderlyingDrawingChanged(object? sender, EventArgs e)
        {
            if (!Dispatcher.UIThread.CheckAccess()) {
                Debug.WriteLine($"CachedDrawing: DrawingChanged raised on a non-UI thread (thread {Environment.CurrentManagedThreadId}) at:{Environment.NewLine}{Environment.StackTrace}");
                Dispatcher.UIThread.VerifyAccess();  // throws InvalidOperationException.
            }

            ++drawingVersion;
            NotifyNewDrawingAvailable();

            DebugPrint($"***** Underlying drawing changed: new drawingVersion: {drawingVersion}");

            // Prune any completed renders that are no longer needed.
            PruneCompletedRenders(detailedRenders);
            PruneCompletedRenders(enclosingRenders);
            PruneCompletedRenders(fullRenders);

            // Begin redrawing renders now, so they are ready as soon as possible.
            // Use the sizes/locations most recently requested for the detailed and enclosing
            // (which may still be in progress), and start a new full.

            InProgressRender? detailedRender = FindLatestRender(detailedRenders);
            if (detailedRender != null) {
                BeginRender(detailedRender.rect, detailedRender.pixelSize, detailedRenders, "detailed");
            }

            InProgressRender? enclosingRender = FindLatestRender(enclosingRenders);
            if (enclosingRender != null) {
                BeginRender(enclosingRender.rect, enclosingRender.pixelSize, enclosingRenders, "enclosing");
            }

            // Only begin a new full render if we have had one before. Otherwise we might not ever need one,
            // for example, we never get a draw.
            if (fullRenders.Count > 0) {
                BeginFullRender();
            }
        }

        // Release all the cached bitmaps, and cancel any renders in progress (their bitmaps are
        // released when they finish). Must be called on the UI thread.
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            underlyingDrawing.DrawingChanged -= OnUnderlyingDrawingChanged;

            DisposeAllRenders(detailedRenders);
            DisposeAllRenders(enclosingRenders);
            DisposeAllRenders(fullRenders);
        }

        // Cancel and dispose all renders in the given list, and empty the list.
        private static void DisposeAllRenders(List<InProgressRender> currentRenders)
        {
            foreach (InProgressRender render in currentRenders) {
                render.CancelDrawing();
                render.DisposeWhenDone();
            }
            currentRenders.Clear();
        }



        private void DebugPrint(string message)
        {
#if SHOWDEBUGOUTPUT
            Debug.WriteLine($"--------------{this.GetHashCode().ToString("X")}------------------------------");
            Debug.WriteLine(message);
            Debug.WriteLine(this.ToString());
#endif
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("Detailed Renders:");
            foreach (InProgressRender render in detailedRenders) {
                builder.AppendLine("  " + render.ToString());
            }

            builder.AppendLine("Enclosing Renders:");
            foreach (InProgressRender render in enclosingRenders) {
                builder.AppendLine("  " + render.ToString());
            }

            builder.AppendLine("Full Renders:");
            foreach (InProgressRender render in fullRenders) {
                builder.AppendLine("  " + render.ToString());
            }

#if SHOWDEBUGOUTPUT
            builder.AppendLine("Render counts (all CachedDrawings):");
            builder.Append(InProgressRender.StatsString());
#endif

            return builder.ToString();
        }

        private class InProgressRender: IDisposable
        {
            public readonly string type;                           // type for debugging purposes
            public readonly int drawingVersion;                    // version of the drawing used to draw.
            public readonly int renderVersion;                     // increments every render.
            public readonly Rect rect;                             // Rectangle being drawn.
            public readonly PixelSize pixelSize;                   // Pixel size of the drawing.
            public readonly CancellationTokenSource cancelSource;  // Source for canceling the task.
            private readonly IThreadsafeSkiaDrawing drawing;       // The drawing to render.
            private readonly Action? onCompleted;                  // Called (on a thread pool thread) when the render completes successfully.
            private readonly Action? onFinished;                   // Called (on a thread pool thread) when the render finishes in any state.
            private Task<WriteableBitmapTracker>? task;            // Task that will return the bitmap when done. Null until Start is called (pending).
            private bool dropped;                                  // True if disposed before being started; it can then never be started.

            // Create a render. It is pending (does not start drawing) until Start is called.
            //   type: type of the render, for debugging.
            //   drawing: the drawing to render.
            //   drawingVersion: version of the drawing being rendered.
            //   renderVersion: increments every render.
            //   rectToDraw: the rectangle to render.
            //   pixelSize: the pixel size to render at.
            //   onCompleted: called on a thread pool thread after the render completes successfully.
            //   onFinished: called on a thread pool thread after the render finishes in any state (after onCompleted).
            public InProgressRender(string type, IThreadsafeSkiaDrawing drawing, int drawingVersion, int renderVersion, Rect rectToDraw, PixelSize pixelSize,
                                    Action? onCompleted, Action? onFinished)
            {
                this.type = type;
                this.drawing = drawing;
                this.drawingVersion = drawingVersion;
                this.renderVersion = renderVersion;
                this.rect = rectToDraw;
                this.pixelSize = pixelSize;
                this.onCompleted = onCompleted;
                this.onFinished = onFinished;
                this.cancelSource = new CancellationTokenSource();

                RecordStat(type, counts => ++counts.requested);
            }

            // Start the render on a background task. Does nothing if it has already been started, or was disposed before
            // being started. Must be called on the UI thread.
            public void Start()
            {
                if (task != null || dropped)
                    return;

                IThreadsafeSkiaDrawing drawing = this.drawing;
                Rect rectToDraw = this.rect;
                PixelSize pixelSize = this.pixelSize;
                CancellationToken cancelToken = cancelSource.Token;

                RecordStat(type, counts => ++counts.started);

                // All canvas operations (Concat, Clear, Draw) must happen on the same thread.
                // SKCanvas is not thread-safe, so we wrap everything in a single Task.Run
                // and use the synchronous DrawToBitmap.
                this.task = Task.Run(() => {
                    cancelToken.ThrowIfCancellationRequested();

                    return SkiaWriteableBitmapUtil.DrawToBitmap(pixelSize, (canvas, token) => {
                        token.ThrowIfCancellationRequested();

                        RectangleF destRect = new RectangleF(0, 0, pixelSize.Width, pixelSize.Height);
                        SKMatrix transformation = GeometryUtil.CreateRectangleTransform(Conv.ToSKRect(rectToDraw), Conv.ToSKRect(destRect));
                        canvas.Concat(transformation);

                        drawing.ThreadsafeDraw(canvas,
                                               new SKRect((float)rectToDraw.Left, (float)rectToDraw.Top, (float)rectToDraw.Right, (float)rectToDraw.Bottom),
                                               new SKSizeI(pixelSize.Width, pixelSize.Height),
                                               token);
                        token.ThrowIfCancellationRequested();
                    }, cancelToken);
                }, cancelToken);

                // Notify completion only once the task has actually finished, so that IsCompleted is true
                // (and the bitmap is unlocked) by the time anyone responds to the notification.
                this.task.ContinueWith(OnTaskFinished, TaskScheduler.Default);
            }

            // Called on a thread pool thread when the render task has finished, in any state.
            // Calls onCompleted if the task ran to completion, then always calls onFinished.
            //   t: the finished task.
            private void OnTaskFinished(Task<WriteableBitmapTracker> t)
            {
                try {
                    ReportTaskFinished(t);
                }
                finally {
                    // Always called, so that a pending render waiting on this one is started.
                    if (onFinished != null)
                        onFinished();
                }
            }

            // Record statistics on how the render task finished, and call onCompleted if it ran to completion.
            //   t: the finished task.
            private void ReportTaskFinished(Task<WriteableBitmapTracker> t)
            {
                if (t.Status == TaskStatus.RanToCompletion) {
                    RecordStat(type, counts => ++counts.completed);
#if SHOWDEBUGOUTPUT
                    Debug.WriteLine("Completed render: " + ToString());
#endif
                    if (onCompleted != null)
                        onCompleted();
                }
                else if (t.IsCanceled) {
                    RecordStat(type, counts => ++counts.cancelled);
                }
                else if (t.IsFaulted) {
                    // Reading t.Exception observes the exception, so it doesn't become an UnobservedTaskException.
                    RecordStat(type, counts => ++counts.faulted);
                    Debug.WriteLine($"CachedDrawing: {type} render failed: {t.Exception}");
                }
            }

            // True if the render has completed successfully, so the bitmap is available.
            public bool IsCompleted => task != null && task.Status == TaskStatus.RanToCompletion;

            // True if the render has been started and is still running (not completed, faulted, or canceled).
            public bool InProgress => task != null && !task.IsCompleted;

            // True if the render has not been started yet.
            public bool IsPending => task == null;

            // True if the render has been started and has finished, in any state (completed, faulted, or canceled).
            public bool IsFinished => task != null && task.IsCompleted;

            public bool IsCancelled => task != null && task.IsCanceled;

            // True if cancellation of this render has been requested (it may still be running until it notices).
            public bool CancelRequested => cancelSource.IsCancellationRequested;

            // Counts of renders of one type, for debugging. RecordStat and RecordReused are [Conditional], so the calls
            // to them (including creating their lambda arguments) are compiled only when SHOWDEBUGOUTPUT is defined.
            private class RenderCounts
            {
                public int requested, started, completed, cancelled, faulted, dropped, notRestarted;
            }

            // Counts of renders by type, across all CachedDrawings, for debugging. Protected by locking the dictionary.
            private static readonly Dictionary<string, RenderCounts> renderStats = new Dictionary<string, RenderCounts>();

            // Update the render counts for the given render type.
            //   type: the type of render.
            //   update: updates the counts.
            [Conditional("SHOWDEBUGOUTPUT")]
            private static void RecordStat(string type, Action<RenderCounts> update)
            {
                lock (renderStats) {
                    if (!renderStats.TryGetValue(type, out RenderCounts? counts)) {
                        counts = new RenderCounts();
                        renderStats.Add(type, counts);
                    }
                    update(counts);
                }
            }

            // Record that a render of the given type was requested but not started, because a matching render
            // was already in progress or completed.
            //   type: the type of render.
            [Conditional("SHOWDEBUGOUTPUT")]
            public static void RecordReused(string type)
            {
                RecordStat(type, counts => ++counts.notRestarted);
            }

            // Get a description of the render counts, for debugging.
            public static string StatsString()
            {
                StringBuilder builder = new StringBuilder();
                lock (renderStats) {
                    foreach (KeyValuePair<string, RenderCounts> pair in renderStats.OrderBy(pair => pair.Key)) {
                        RenderCounts counts = pair.Value;
                        builder.AppendLine($"  {pair.Key}: requested:{counts.requested} started:{counts.started} completed:{counts.completed} cancelled:{counts.cancelled} " +
                                           $"faulted:{counts.faulted} running:{counts.started - counts.completed - counts.cancelled - counts.faulted} " +
                                           $"dropped before start:{counts.dropped} pending:{counts.requested - counts.started - counts.dropped} " +
                                           $"not restarted:{counts.notRestarted}");
                    }
                }
                return builder.ToString();
            }

            public void CancelDrawing()
            {
                if (!IsCompleted && !dropped)
                    cancelSource.Cancel();
            }

            public void Draw(DrawingContext drawingContext, Rect clipRect)
            {
                if (IsCompleted && clipRect.Intersects(rect)) {
                    using (var state = drawingContext.PushClip(clipRect)) {
                        drawingContext.DrawImage(task!.Result.Bitmap, rect);
                    }
                }
            }

            public override string ToString()
            {
                string status = (task == null) ? "Pending" : task.Status.ToString();
                return $"Type:{type} Ver:{renderVersion} DrawVer:{drawingVersion} Rect:({rect.Left:0.##},{rect.Top:0.##})-({rect.Right:0.##},{rect.Bottom:0.##}) Size:{pixelSize} Status:{status}";
            }

            // Dispose the render now if it is done or was never started, otherwise as soon as the task finishes (a canceled
            // task can still run to completion if it was past its last cancellation check).
            public void DisposeWhenDone()
            {
                if (task == null || task.IsCompleted)
                    Dispose();
                else
                    task.ContinueWith(_ => Dispose(), TaskScheduler.Default);
            }

            // Dispose the bitmap, if the task is done. Does nothing if the task is still running.
            // A render that was never started is dropped, and can't be started afterward.
            public void Dispose()
            {
                if (task == null) {
                    // Never started. Make sure it can't be started later.
                    if (!dropped) {
                        dropped = true;
                        RecordStat(type, counts => ++counts.dropped);
                        cancelSource.Dispose();
                    }
                }
                else if (task.IsCompleted) {
                    if (task.IsCompletedSuccessfully) {
                        WriteableBitmapTracker bitmapTracker = task.Result;
                        bitmapTracker?.Dispose();
                    }
                    task.Dispose();
                    cancelSource.Dispose();
                }
            }
        }
    }

}
