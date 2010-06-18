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

using Dr = System.Drawing;
using RectangleF = System.Drawing.RectangleF;
using Bitmap = System.Drawing.Bitmap;
using Graphics = System.Drawing.Graphics;

namespace WpfMap
{
    class MapViewer: FrameworkElement
    {
        class CachedMapArea
        {
            public Rect portion;       // portion of the map that is cached.
            public double pixelSize;           // pixel size at which it is rendered
            public bool isUpToDate;       // if true, the bitmap is up to date.
            public BitmapSource bitmap;   // bitmap with the cached section.
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

            wholeMap.portion = bounds.ToRect();
            wholeMap.pixelSize = pixelSize;
            wholeMap.isUpToDate = false;
            CreateBitmap(wholeMap);
        }

        // Cache the visible portion of the map. Called from the timer.
        void CacheDetailMap(object sender, EventArgs eventArgs)
        {
            timer.Stop();

            // Get the portion of the map to cache. This the currently visible area.
            detailMap.portion =  panAndZoom.VisibleRect;
            detailMap.pixelSize = panAndZoom.PixelSize;
            detailMap.isUpToDate = false;

            // Create it.
            CreateBitmap(detailMap);

            // Invalidate the visual to draw the detail map on the screen.
            InvalidateVisual();
        }

        BitmapSource CopyBitmapToBitmapSource(Bitmap bm) {
            Debug.Assert(bm.PixelFormat == Dr.Imaging.PixelFormat.Format32bppPArgb);

            int width = bm.Width, height = bm.Height;

            Dr.Imaging.BitmapData bmData = bm.LockBits(new Dr.Rectangle(0, 0, width, height), Dr.Imaging.ImageLockMode.ReadOnly, Dr.Imaging.PixelFormat.Format32bppPArgb);

            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Pbgra32, null, bmData.Scan0, bmData.Height * bmData.Stride, bmData.Stride);
            bm.UnlockBits(bmData);
            return bitmap;
        }


        // Fill in the bitmap member of a cached map area with a bitmap, created from the current map. 
        void CreateBitmap(CachedMapArea mapArea)
        {
            // Determine the size of the bitmap we need.
            int height = (int) Math.Ceiling(mapArea.portion.Height / mapArea.pixelSize);
            int width = (int) Math.Ceiling(mapArea.portion.Width / mapArea.pixelSize);

            // Create transformation matrix from map coords to the bitmap coords
            Point midpoint = new Point(width / 2.0F, height / 2.0F);
            float scaleFactor = (float)(width / mapArea.portion.Width);
            Point centerPoint = new Point((mapArea.portion.Left + mapArea.portion.Right) / 2, (mapArea.portion.Top + mapArea.portion.Bottom) / 2);
            Dr.Drawing2D.Matrix matrix = new Dr.Drawing2D.Matrix();
            matrix.Translate((float) midpoint.X, (float) midpoint.Y, Dr.Drawing2D.MatrixOrder.Prepend);
            matrix.Scale(scaleFactor, scaleFactor, Dr.Drawing2D.MatrixOrder.Prepend);  // y scale is negative to get to cartesian orientation.
            matrix.Translate((float)(-centerPoint.X), (float)(-centerPoint.Y), Dr.Drawing2D.MatrixOrder.Prepend);

            // Get the render options.
            PurplePen.MapModel.RenderOptions renderOpts = new PurplePen.MapModel.RenderOptions();
            renderOpts.usePatternBitmaps = true;
            renderOpts.minResolution = (float) mapArea.pixelSize;

            // Create a bitmap of the map, appropriately transformed.
            Bitmap bitmapNew = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(bitmapNew)) {
                g.Clear(Dr.Color.Transparent);
                g.Transform = matrix;

                using (map.Read())
                    map.Draw(new GDIPlus_GraphicsTarget(g), mapArea.portion.ToRectangleF(), renderOpts);
            }

            bitmapNew.Save(@"C:\Users\Peter\Documents\PurplePen\newmapmodel\src\WpfMap\TestResults\output.png", Dr.Imaging.ImageFormat.Png);

            // Copy the bitmap into WPF format.
            mapArea.bitmap = CopyBitmapToBitmapSource(bitmapNew);
            bitmapNew.Dispose();
            
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

            Rect visiblePortion = panAndZoom.VisibleRect; 
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
