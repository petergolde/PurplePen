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
        private CacheLevel fullMapLevel = new CacheLevel();
        private CacheLevel detailLevel = new CacheLevel();

        // Number of pixels to use for full-map caches.
        private const int ENTIRE_MAP_PIXELS = 5000000;

        public MapRenderCache(Map map) {
            this.map = map;

            RectangleF bounds;

            // Determine bounds of map, and pixel size to use.
            using (map.Read())
                bounds = map.Bounds;
            double ratio = bounds.Width / bounds.Height;
            double pixelSize = bounds.Height / Math.Sqrt((ENTIRE_MAP_PIXELS / ratio));

            fullMapLevel.QueueUpdate(new RenderParameters(map, bounds.ToRect(), pixelSize), null);
        }

        public void Render(DrawingContext dc, Rect visibleRect, double pixelSize) {
            RenderParameters parameters = new RenderParameters(map, visibleRect, pixelSize);

            if (detailLevel.CachedParameters != null && detailLevel.CachedParameters.Equals(parameters)) {
                // The current detail map is perfect.
                detailLevel.Render(dc);
            }
            else {
                // Use the detail map if any portion is useful.
                Geometry clipArea = null;
                if (detailLevel.CachedParameters != null && visibleRect.IntersectsWith(detailLevel.CachedParameters.VisibleRect) && pixelSize < fullMapLevel.CachedParameters.PixelSize) {
                    detailLevel.Render(dc);

                    // Exclude the detail map area, so we only draw the whole map around it.
                    RectangleGeometry visibleGeometry = new RectangleGeometry(visibleRect);
                    RectangleGeometry detailMapGeometry = new RectangleGeometry(detailLevel.CachedParameters.VisibleRect);
                    clipArea = Geometry.Combine(visibleGeometry, detailMapGeometry, GeometryCombineMode.Exclude, null);
                }

                // Use the whole map, unless the cliparea is empty.
                if (fullMapLevel.CachedParameters != null && (clipArea == null || !clipArea.IsEmpty())) {
                    if (clipArea != null)
                        dc.PushClip(clipArea);
                    fullMapLevel.Render(dc);
                    if (clipArea != null)
                        dc.Pop();
                }
            }
        }

        // Request the given area to be cached. Returns true if the cache is already up to date or being created.
        // Returns false if a new task was start to cache the area. In this case, the given callback is called back (on the current SynchronizationContext)
        // when complete.
        public bool CacheArea(Rect visibleRect, double pixelSize, Action callbackOnCompletion) {
            if (fullMapLevel.CachedParameters == null || pixelSize < fullMapLevel.CachedParameters.PixelSize)
                return detailLevel.QueueUpdate(new RenderParameters(map, visibleRect, pixelSize), callbackOnCompletion);
            else
                return true;        // full map already is detailed enough.
        }


        // Copy a System.Draw.Bitmap to a BitmapSource.
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
            int height = (int)Math.Ceiling(parameters.VisibleRect.Height / parameters.PixelSize);
            int width = (int)Math.Ceiling(parameters.VisibleRect.Width / parameters.PixelSize);

            // Create transformation matrix from map coords to the bitmap coords
            Point midpoint = new Point(width / 2.0F, height / 2.0F);
            float scaleFactor = (float)(width / parameters.VisibleRect.Width);
            Point centerPoint = new Point((parameters.VisibleRect.Left + parameters.VisibleRect.Right) / 2, (parameters.VisibleRect.Top + parameters.VisibleRect.Bottom) / 2);
            Dr.Drawing2D.Matrix matrix = new Dr.Drawing2D.Matrix();
            matrix.Translate((float)midpoint.X, (float)midpoint.Y, Dr.Drawing2D.MatrixOrder.Prepend);
            matrix.Scale(scaleFactor, scaleFactor, Dr.Drawing2D.MatrixOrder.Prepend);  // y scale is negative to get to cartesian orientation.
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

            public void Render(DrawingContext dc) {
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
