//#define SHOWDEBUGOUTPUT

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
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
    public class CachedDrawing: IAvaloniaDrawing
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

        public CachedDrawing(IThreadsafeSkiaDrawing drawing)
        {
            this.underlyingDrawing = drawing;
            this.underlyingDrawing.DrawingChanged += OnUnderlyingDrawing;
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
        public void Draw(DrawingContext drawingContext, Rect rectToDraw, PixelSize pixelSize, Matrix transformWorldToPixel)
        {
            DebugPrint($"Draw: Rect:{rectToDraw} PixelSize:{pixelSize}");

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

                // Create an enclosing render if we don't have one or the current one doesn't enclose well.
                if (enclosingRender == null || !enclosingRender.rect.Contains(rectToDraw.Inflate(new Thickness(rectToDraw.Width / 2, rectToDraw.Height / 2))) ||
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

            // Start a new detailed render for the exactly bounds and resolution requested.
            BeginRender(rectToDraw, pixelSize, detailedRenders, "detailed");

            // If there is no full render, or it is out of date, create one.
            if (fullRender == null || fullRender.drawingVersion != drawingVersion) {
                // The size of the full render has a longer side of "FullRenderSize"
                SKRect fullBounds = underlyingDrawing.Bounds;
                PixelSize fullPixelSize;
                if (fullBounds.Width > fullBounds.Height) {
                    fullPixelSize = new PixelSize(FullRenderSize, (int)(FullRenderSize * fullBounds.Height / fullBounds.Width));
                }
                else {
                    fullPixelSize = new PixelSize((int)(FullRenderSize * fullBounds.Width / fullBounds.Height), FullRenderSize);
                }

                BeginRender(Conv.ToAvRect(fullBounds), fullPixelSize, fullRenders, "full");
            }

            DebugPrint($"End Draw: Rect:{rectToDraw} PixelSize:{pixelSize}");
        }

        private InProgressRender? FindCompletedRender(List<InProgressRender> currentRenders)
        {
            return currentRenders.FirstOrDefault(render => render.IsCompleted);
        }

        // Start a new render of the drawing at the resolution requested.
        // Cancel any in-progress renders that are still going on, as well as pruning
        // completed renders.
        private void BeginRender(Rect rectToDraw, PixelSize pixelSize, List<InProgressRender> currentRenders, string type)
        {
            // Prune any completed renders that are no longer needed.
            PruneCompletedRenders(currentRenders);

            // Cancel any in-progress renders that are still going on.
            CancelInProgressRenders(currentRenders);

            // Start a new detailed render for the exactly bounds and resolution requested.
            InProgressRender? newRender = null;
            if (pixelSize.Width > 0 && pixelSize.Height > 0) {
                newRender = new InProgressRender(type, underlyingDrawing, drawingVersion, renderVersion, rectToDraw, pixelSize, NotifyNewDrawingAvailable);
                currentRenders.Add(newRender);
            }
            ++renderVersion;

            if (newRender != null) {
                DebugPrint("Beginning render: " + newRender.ToString());
            }
        }

        private void NotifyNewDrawingAvailable()
        {
            if (DrawingChanged != null && !availableDrawingPosted) {
                availableDrawingPosted = true;  // Prevent multiple posts until the event is handled.

                Dispatcher.UIThread.Post(() => {
                    // NewDrawingAvailable could have become null between the check and the Post,
                    // so check again.
                    availableDrawingPosted = false;
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
                if (currentRenders[i].task.IsCompleted && currentRenders[i].renderVersion != latestVersion) {
                    currentRenders[i].Dispose();
                    currentRenders.RemoveAt(i);
                }
            }
        }

        // Cancel and prune all in-progress renders in the given list.
        private void CancelInProgressRenders(List<InProgressRender> currentRenders)
        {
            for (int i = currentRenders.Count - 1; i >= 0; --i) {
                if (currentRenders[i].InProgress) {
                    currentRenders[i].CancelDrawing();
                    if (currentRenders[i].IsCompleted) {
                        currentRenders[i].Dispose();
                        currentRenders.RemoveAt(i);
                    }
                }
            }
        }

        // The underlying drawing has changed.
        private void OnUnderlyingDrawing(object? sender, EventArgs e)
        {
            ++drawingVersion;
            NotifyNewDrawingAvailable();
        }



        private void DebugPrint(string message)
        {
#if SHOWDEBUGOUTPUT
            Debug.WriteLine("--------------------------------------------");
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

            return builder.ToString();
        }

        private class InProgressRender: IDisposable
        {
            public readonly string type;                           // type for debugging purposes
            public readonly int drawingVersion;                    // version of the drawing used to draw.
            public readonly int renderVersion;                     // increments every render.
            public readonly Rect rect;                             // Rectangle being drawn.
            public readonly PixelSize pixelSize;                   // Pixel size of the drawing.
            public readonly Task<WriteableBitmapTracker> task;     // Task that will return the bitmap when done.
            public readonly CancellationTokenSource cancelSource;  // Source for canceling the task.

            public InProgressRender(string type, IThreadsafeSkiaDrawing drawing, int drawingVersion, int renderVersion, Rect rectToDraw, PixelSize pixelSize, Action? onCompleted)
            {
                this.type = type;
                this.drawingVersion = drawingVersion;
                this.renderVersion = renderVersion;
                this.rect = rectToDraw;
                this.pixelSize = pixelSize;
                this.cancelSource = new CancellationTokenSource();

                CancellationToken cancelToken = cancelSource.Token;

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

                        if (onCompleted != null)
                            onCompleted();
                    }, cancelToken);
                }, cancelToken);
            }

            public bool IsCompleted => task.Status == TaskStatus.RanToCompletion;

            public bool InProgress => !task.IsCompleted;  // Not completed, faulted, or canceled.

            public bool IsCancelled => task.IsCanceled;

            public void CancelDrawing()
            {
                if (!IsCompleted)
                    cancelSource.Cancel();
            }

            public void Draw(DrawingContext drawingContext, Rect clipRect)
            {
                if (IsCompleted && clipRect.Intersects(rect)) {
                    using (var state = drawingContext.PushClip(clipRect)) {
                        drawingContext.DrawImage(task.Result.Bitmap, rect);
                    }
                }
            }

            public override string ToString()
            {
                return $"Type:{type} Ver:{renderVersion} Rect:({rect.Left:0.##},{rect.Top:0.##})-({rect.Right:0.##},{rect.Bottom:0.##}) Size:{pixelSize} Status:{task.Status}";
            }

            public void SaveAsPng(string filePath)
            {
                // Open a file stream to write the PNG file
                using (var stream = System.IO.File.OpenWrite(filePath)) {
                    task.Result.Bitmap.Save(stream);
                }
            }

            public void Dispose()
            {
                if (task != null && task.IsCompleted) {
                    if (task.IsCompletedSuccessfully) {
                        WriteableBitmapTracker bitmapTracker = task.Result;
                        bitmapTracker?.Dispose();
                    }
                    task.Dispose();
                }
            }
        }
    }

}
