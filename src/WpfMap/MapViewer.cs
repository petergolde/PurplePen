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
        private Map map;
        private PanAndZoom panAndZoom;
        private MapRenderCache renderCache;

        public Map Map
        {
            get { return map; }

            set
            {
                map = value;
                renderCache = new MapRenderCache(map);

                InvalidateVisual();
            }
        }

        public MapViewer()
        {
            System.Windows.Media.RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.Linear);
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

        // Render the map.
        protected override void OnRender(DrawingContext dc)
        {
            if (map == null)
                return;

            Rect visiblePortion = panAndZoom.VisibleRect; 
            double pixelSize = panAndZoom.PixelSize;

            // Render the best we can now.
            renderCache.Render(dc, visiblePortion, pixelSize);

            // Ask the render cache to cache the current visible area as best we can, and invalidate when done.
            renderCache.CacheArea(visiblePortion, pixelSize, () => { InvalidateVisual(); });
        }
    }
}
