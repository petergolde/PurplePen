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
    class CachedViewer: FrameworkElement
    {
        private IRenderable renderSource;
        private PanAndZoom panAndZoom;
        private RenderCache renderCache;

        public IRenderable RenderSource
        {
            get { return renderSource; }

            set
            {
                renderSource = value;
                renderCache = new RenderCache(renderSource);

                InvalidateVisual();
            }
        }

        public CachedViewer()
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

        // Render the source.
        protected override void OnRender(DrawingContext drawingContext)
        {
            if (renderSource == null)
                return;

            Rect visiblePortion = panAndZoom.VisibleRect; 
            double pixelSize = panAndZoom.PixelSize;

            // Render the best we can now.
            renderCache.Render(drawingContext, visiblePortion, pixelSize);

            // Ask the render cache to cache the current visible area as best we can, and invalidate when done.
            renderCache.CacheArea(visiblePortion, pixelSize, () => { InvalidateVisual(); });
        }
    }
}
