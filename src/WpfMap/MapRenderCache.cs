using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using PurplePen.MapModel;

using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Threading;

using Dr = System.Drawing;
using RectangleF = System.Drawing.RectangleF;
using Bitmap = System.Drawing.Bitmap;
using Graphics = System.Drawing.Graphics;


namespace WpfMap
{
    class MapRenderCache
    {
        private Map map;
        private RectangleF mapBounds;
        private double fullMapPixelSize;
        private CacheLevel fullMapLevel = new CacheLevel();
        private CacheLevel mediumLevel = new CacheLevel();
        private CacheLevel detailLevel = new CacheLevel();

        // Number of pixels to use for full-map caches.
        private const int ENTIRE_MAP_PIXELS = 5000000;
        private const double MEDIUM_LEVEL_RATIO = 3.0;      // ratio of medium level pixel size to full map pixel size.

        public MapRenderCache(Map map) {
            this.map = map;

            // Determine bounds of map, and pixel size to use.
            using (map.Read())
                mapBounds = map.Bounds;
            double ratio = mapBounds.Width / mapBounds.Height;
            fullMapPixelSize = mapBounds.Height / Math.Sqrt((ENTIRE_MAP_PIXELS / ratio));

            //fullMapLevel.QueueUpdate(new RenderParameters(map, mapBounds.ToRect(), fullMapPixelSize), null);
        }

        public void Render(DrawingContext dc, Rect visibleRect, double pixelSize) {
            RenderParameters parameters = new RenderParameters(map, visibleRect, pixelSize);

            if (detailLevel.CachedParameters != null && detailLevel.CachedParameters.Equals(parameters)) {
                // The current detail map is perfect.
                detailLevel.Render(dc);
            }
            else if (pixelSize >= fullMapPixelSize) {
                // the full map is the best we can do at this pixel size.
                fullMapLevel.Render(dc);
            }
            else {
                Geometry clipArea = new RectangleGeometry(visibleRect);

                // Use detail map, the medium detail, then full map, in that order.
                // Use the detail map if any portion is useful.
                clipArea = RenderCachedLevel(dc, detailLevel, clipArea);
                clipArea = RenderCachedLevel(dc, mediumLevel, clipArea);
                clipArea = RenderCachedLevel(dc, fullMapLevel, clipArea);
            }
        }

        // Render a given cache level if it usefully contributes to rendering into the given clipArea. Only render into the clip area.
        // Returns an updated clip area with the rendered section excluded.
        private Geometry RenderCachedLevel(DrawingContext dc, CacheLevel cachedLevel, Geometry clipArea) {
            if (cachedLevel.CachedParameters != null && !clipArea.IsEmpty() && clipArea.Bounds.IntersectsWith(cachedLevel.CachedParameters.VisibleRect)) {
                dc.PushClip(clipArea);
                cachedLevel.Render(dc);
                dc.Pop();

                return Geometry.Combine(clipArea, new RectangleGeometry(cachedLevel.CachedParameters.VisibleRect), GeometryCombineMode.Exclude, null);
            }
            else {
                return clipArea;
            }
        }

        // Request the given area to be cached. Returns true if the cache is already up to date or being created.
        // Returns false if a new task was start to cache the area. In this case, the given callback is called back (on the current SynchronizationContext)
        // when complete.
        public bool CacheArea(Rect visibleRect, double pixelSize, Action callbackOnCompletion) {
            if (pixelSize < fullMapPixelSize) {
                bool returnValue = detailLevel.QueueUpdate(new RenderParameters(map, visibleRect, pixelSize), callbackOnCompletion);
                if (pixelSize < (fullMapPixelSize / MEDIUM_LEVEL_RATIO)) {
                    // The map is detailed enough that we might want to cache a medium detail map.
                    RenderParameters cachedParameters = mediumLevel.CachedParameters, inProgressParameters = mediumLevel.ParametersInProgress;
                    if ((cachedParameters == null || (! cachedParameters.VisibleRect.Contains(visibleRect))) &&
                        (inProgressParameters == null || (!inProgressParameters.VisibleRect.Contains(visibleRect)))) 
                    {
                        // The medium map level doesn't contain the current rect, nor is it building a reasonable rect, so build a new one.
                        double mediumPixelSize = fullMapPixelSize/ MEDIUM_LEVEL_RATIO;
                        Point mediumCenterPoint = new Point((visibleRect.Left + visibleRect.Right) / 2, (visibleRect.Top + visibleRect.Bottom) / 2);
                        double mediumWidth = mapBounds.Width / MEDIUM_LEVEL_RATIO, mediumHeight = mapBounds.Height / MEDIUM_LEVEL_RATIO;
                        Rect mediumRect = new Rect(mediumCenterPoint.X - (mediumWidth / 2), mediumCenterPoint.Y - (mediumHeight / 2), mediumWidth, mediumHeight);

                        mediumLevel.QueueUpdate(new RenderParameters(map, mediumRect, mediumPixelSize), null);
                    }
                }

                return returnValue;
            }

            else
                return fullMapLevel.QueueUpdate(new RenderParameters(map, mapBounds.ToRect(), fullMapPixelSize), callbackOnCompletion);        // full map already is detailed enough.
        }


        // Copy a System.Drawing.Bitmap to a BitmapSource.
        static private BitmapSource CopyBitmapToBitmapSource(Bitmap bm) {
            Debug.Assert(bm.PixelFormat == Dr.Imaging.PixelFormat.Format32bppPArgb);

            int width = bm.Width, height = bm.Height;

            Dr.Imaging.BitmapData bmData = bm.LockBits(new Dr.Rectangle(0, 0, width, height), Dr.Imaging.ImageLockMode.ReadOnly, Dr.Imaging.PixelFormat.Format32bppPArgb);

            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Pbgra32, null, bmData.Scan0, bmData.Height * bmData.Stride, bmData.Stride);
            bm.UnlockBits(bmData);
            bitmap.Freeze();
            return bitmap;
        }

        // Create a BitmapSource by rendering the map with the given render parameters.
        static BitmapSource CreateBitmap(RenderParameters parameters, CancellationToken cancellationToken) {
            // Determine the size of the bitmap we need.
            int height = (int)Math.Round(parameters.VisibleRect.Height / parameters.PixelSize);
            int width = (int)Math.Round(parameters.VisibleRect.Width / parameters.PixelSize);

            // Create transformation matrix from map coords to the bitmap coords
            Point midpoint = new Point((width-1) / 2.0, (height-1) / 2.0);
            float scaleFactorX = (float)(width / parameters.VisibleRect.Width);
            float scaleFactorY = (float)(height / parameters.VisibleRect.Height);
            Point centerPoint = new Point((parameters.VisibleRect.Left + parameters.VisibleRect.Right) / 2, (parameters.VisibleRect.Top + parameters.VisibleRect.Bottom) / 2);
            Dr.Drawing2D.Matrix matrix = new Dr.Drawing2D.Matrix();
            matrix.Translate((float)midpoint.X, (float)midpoint.Y, Dr.Drawing2D.MatrixOrder.Prepend);
            matrix.Scale(scaleFactorX, scaleFactorY, Dr.Drawing2D.MatrixOrder.Prepend);  
            matrix.Translate((float)(-centerPoint.X), (float)(-centerPoint.Y), Dr.Drawing2D.MatrixOrder.Prepend);

            // Get the render options.
            PurplePen.MapModel.RenderOptions renderOpts = new PurplePen.MapModel.RenderOptions();
            renderOpts.usePatternBitmaps = true;
            renderOpts.minResolution = (float)parameters.PixelSize;

            // Create a bitmap of the map, appropriately transformed.
            BitmapSource resultBitmap;
            using (Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb)) {
                using (Graphics g = Graphics.FromImage(bitmap)) {
                    g.SmoothingMode = Dr.Drawing2D.SmoothingMode.AntiAlias;
                    g.Clear(Dr.Color.Transparent);
                    g.Transform = matrix;

                    using (parameters.Map.Read())
                        parameters.Map.Draw(new GDIPlus_GraphicsTarget(g), parameters.VisibleRect.ToRectangleF(), renderOpts, () => cancellationToken.ThrowIfCancellationRequested());
                }

                // Copy the bitmap into WPF format.
                cancellationToken.ThrowIfCancellationRequested();
                resultBitmap = CopyBitmapToBitmapSource(bitmap);
            }
            cancellationToken.ThrowIfCancellationRequested();

            return resultBitmap;
        }

        private class CacheLevel
        {
            private BitmapSource currentBestCache;
            private RenderParameters currentBestParameters;

            // The following represent a background task that is completing.
            private Task bgTask;
            private RenderParameters bgRenderParameters;
            private CancellationTokenSource bgCancellationSource;

            public RenderParameters CachedParameters {
                get { return currentBestParameters; }
            }

            public RenderParameters ParametersInProgress {
                get { return bgRenderParameters; }
            }

            public void Render(DrawingContext dc) {
                if (currentBestParameters != null)
                    dc.DrawImage(currentBestCache, currentBestParameters.VisibleRect);
            }

            private void CancelBackgroundTask()
            {
                if (bgCancellationSource != null) {
                    bgCancellationSource.Cancel();
                    bgTask = null;
                    bgCancellationSource = null;
                    bgRenderParameters = null;
                }
            }

            public bool QueueUpdate(RenderParameters parameters, Action callback) {
                if (currentBestParameters != null && currentBestParameters.Equals(parameters))
                    return true;       // nothing to do.
                if (bgCancellationSource != null && bgRenderParameters.Equals(parameters))
                    return true;       // nothing to do; we've already queue a request for these parameters.

                // If we have already scheduled a background task to update this level, cancel it because we don't want it any more.
                CancelBackgroundTask();

                // Record parameters and cancellation source for the new task.
                bgRenderParameters = parameters;
                bgCancellationSource = new CancellationTokenSource();

                // Create a task than renders the new bitmap, then post back to the 
                // UI thread to fill in the result and call the callback.
                SynchronizationContext context = SynchronizationContext.Current;
                CancellationToken cancelToken = bgCancellationSource.Token;
                bgTask = Task.Factory.StartNew(() => {
                    BitmapSource bitmap = CreateBitmap(parameters, cancelToken);
                    cancelToken.ThrowIfCancellationRequested();
                    context.Post(o => 
                        {
                            // This code is run back on the UI thread, so we don't have to worry about races.
                            if (! cancelToken.IsCancellationRequested) {
                                bgCancellationSource = null;
                                bgTask = null;
                                bgRenderParameters = null;

                                currentBestParameters = parameters;
                                currentBestCache = bitmap;

                                if (callback != null)
                                    callback();
                            }
                        },
                        null);
                }, cancelToken);

                return true;
            }
        }

        class RenderParameters
        {
            public readonly Map Map;
            public readonly Rect VisibleRect;
            public readonly double PixelSize;

            public RenderParameters(Map map, Rect visibleRect, double pixelSize) {
                this.Map = map;
                this.VisibleRect = visibleRect;
                this.PixelSize = pixelSize;
            }

            public override bool Equals(object o) {
                if (o is RenderParameters) {
                    RenderParameters other = (RenderParameters)o;
                    return (other.Map == this.Map && other.VisibleRect == this.VisibleRect && other.PixelSize == this.PixelSize);
                }
                else {
                    return false;
                }
            }

            public override int GetHashCode() {
                return VisibleRect.GetHashCode() ^ PixelSize.GetHashCode() ^ Map.GetHashCode();
            }
        }
    }
}
