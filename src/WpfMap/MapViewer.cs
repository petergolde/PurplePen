using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using PurplePen.MapModel;

using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Threading;

namespace WpfMap
{
    class MapViewer: FrameworkElement
    {
        class CachedMapArea
        {
            public RectangleF portion;       // portion of the map that is cached.
            public double pixelSize;           // pixel size at which it is rendered
            public bool isUpToDate;       // if true, the bitmap is up to date.
            public RenderTargetBitmap bitmap;   // bitmap with the cached section.
        }

        private Map map;
        private PanAndZoom panAndZoom;

        // Cached sections of the map. If non null, these are fully filled in.
        // wholeMap has the entire map, while detail map has a more detailed section.
        CachedMapArea wholeMap = new CachedMapArea();
        CachedMapArea detailMap = new CachedMapArea();

        DispatcherTimer timer;

        public int ENTIRE_MAP_PIXELS = 5000000;

        public Map Map
        {
            get { return map; }

            set
            {
                map = value;

                // cached parts are not up to date.
                wholeMap.isUpToDate = false;
                detailMap.isUpToDate = false;

                InvalidateVisual();
            }
        }

        public MapViewer()
        {
            // Setup the timer so it pauses 200ms before refreshing the detail map.
            timer = new DispatcherTimer(new TimeSpan(0, 0, 0, 0, 50), DispatcherPriority.Background, new EventHandler(CacheDetailMap), Dispatcher);
            timer.Stop();
        }

        public PanAndZoom PanAndZoomControl
        {
            get
            {
                return panAndZoom;
            }
            set
            {
                if (panAndZoom != null)
                    panAndZoom.VisibleChanged -= new RoutedEventHandler(panAndZoom_VisibleChanged);

                panAndZoom = value;
                panAndZoom.VisibleChanged += new RoutedEventHandler(panAndZoom_VisibleChanged);
            }
        }

        void  panAndZoom_VisibleChanged(object sender, RoutedEventArgs e)
        {
            InvalidateVisual();
        }

        // Cache the entire map in a bitmap that is about ENTIRE_MAP_PIXELS in size.
        void CacheEntireMap()
        {
            RectangleF bounds;

            // Determine bounds of map, and pixel size to use.
            using (map.Read())
                bounds = map.Bounds;
            double ratio = bounds.Width / bounds.Height;
            double pixelSize = bounds.Height / Math.Sqrt((ENTIRE_MAP_PIXELS / ratio));

            wholeMap.portion = bounds;
            wholeMap.pixelSize = pixelSize;
            wholeMap.isUpToDate = false;
            CreateBitmap(wholeMap);
        }

        // Cache the visible portion of the map. Called from the timer.
        void CacheDetailMap(object sender, EventArgs eventArgs)
        {
            timer.Stop();

            // Get the portion of the map to cache. This the currently visible area.
            detailMap.portion = (RectangleF) panAndZoom.VisibleRect;
            detailMap.pixelSize = panAndZoom.PixelSize;
            detailMap.isUpToDate = false;

            // Create it.
            CreateBitmap(detailMap);

            // Invalidate the visual to draw the detail map on the screen.
            InvalidateVisual();
        }

        // Fill in the bitmap member of a cached map area with a bitmap, created from the current map. 
        void CreateBitmap(CachedMapArea mapArea)
        {
            // Determine the size of the bitmap we need.
            int height = (int) Math.Ceiling(mapArea.portion.Height / mapArea.pixelSize);
            int width = (int) Math.Ceiling(mapArea.portion.Width / mapArea.pixelSize);

            // Create transformation matrix from map coords to the bitmap coords
            Matrix matrix = Matrix.Identity;
            matrix.ScalePrepend(1 / mapArea.pixelSize, 1 / mapArea.pixelSize);
            matrix.TranslatePrepend(- mapArea.portion.X, - mapArea.portion.Y);

            // Get the render options.
            PurplePen.MapModel.RenderOptions renderOpts = new PurplePen.MapModel.RenderOptions();
            renderOpts.usePatternBitmaps = false;
            renderOpts.minResolution = (float) mapArea.pixelSize;

            // Create a visual of the map, appropriately transformed
            DrawingVisual visual = new DrawingVisual();
            DrawingContext dc = visual.RenderOpen();
            dc.PushTransform(new MatrixTransform(matrix));
            using (map.Read())
                map.Draw(dc, mapArea.portion, renderOpts);
            dc.Close();

            // Draw it into a new bitmap.
            if (mapArea.bitmap != null && mapArea.bitmap.PixelWidth >= width && mapArea.bitmap.PixelHeight >= height)
                mapArea.bitmap.Clear();  // reuse the old bitmap again by clearing it.
            else
                mapArea.bitmap = new RenderTargetBitmap(width, height, 96.0, 96.0, PixelFormats.Pbgra32);
            mapArea.bitmap.Render(visual);
            
            // Mark as up to date.
            mapArea.isUpToDate = true;
        }

        // Render a cached bitmap to a drawing context.
        void RenderMapArea(DrawingContext dc, CachedMapArea mapArea)
        {
            dc.DrawImage(mapArea.bitmap, new Rect(mapArea.portion.X, mapArea.portion.Y, 
                                                               mapArea.pixelSize * mapArea.bitmap.Width, mapArea.pixelSize * mapArea.bitmap.Height));
        }

        // Render the map.
        protected override void OnRender(DrawingContext dc)
        {
            if (map == null)
                return;

            RectangleF visiblePortion = (RectangleF) panAndZoom.VisibleRect; 
            double pixelSize = panAndZoom.PixelSize;

            if (detailMap.isUpToDate && detailMap.portion == visiblePortion && detailMap.pixelSize == pixelSize) {
                // The current detail map is perfect. Use it only, no need to refresh it.
                RenderMapArea(dc, detailMap);
            }
            else {
                // Use the detail map if any portion is useful.
                Geometry clipArea = null;
                if (detailMap.isUpToDate && visiblePortion.IntersectsWith(detailMap.portion) && pixelSize < wholeMap.pixelSize) {
                    RenderMapArea(dc, detailMap);

                    // Exclude the detail map area, so we only draw the whole map around it.
                    RectangleGeometry visibleGeometry = new RectangleGeometry(visiblePortion);
                    RectangleGeometry detailMapGeometry = new RectangleGeometry(detailMap.portion);
                    clipArea = Geometry.Combine(visibleGeometry, detailMapGeometry, GeometryCombineMode.Exclude, null);
                }

                // Use the whole map, unless the cliparea is empty.
                if (clipArea == null || ! clipArea.IsEmpty()) {
                    if (!wholeMap.isUpToDate)
                        CacheEntireMap();

                    if (clipArea != null)
                        dc.PushClip(clipArea);
                    RenderMapArea(dc, wholeMap);
                    if (clipArea != null)
                        dc.Pop();
                }

                if (pixelSize < wholeMap.pixelSize) {
                    // We could use a more detailed map. Create one in the background.
                    timer.Stop();
                    timer.Start();
                }
            }
        }
    }
}
